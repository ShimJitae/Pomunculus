using System;
using System.Collections.Generic;
using System.Linq;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using Object = UnityEngine.Object;

// 월드 오브젝트의 생성/파괴/저장/입장 동기화 담당
// 결정권은 방장(MasterClient)에게 있고, 다른 플레이어는 방장이 보낸 데이터를 그대로 따라간다
public class WorldManager : Singleton<WorldManager>
{
    public void Init()
    {
        if (m_initialized) return;
        Debug.Log($"WorldManager Init");
        m_catalog = Resources.Load<NetworkPrefabCatalog>("NetworkPrefabCatalog");
        m_prefabs = m_catalog.Entries.ToDictionary(x => x.name);
        RaiseEventManager.Instance.RegisterEvent((int)WorldEventID.RequestSnapshot, OnRequestSnapshot);
        RaiseEventManager.Instance.RegisterEvent((int)WorldEventID.WorldSnapshot, OnReceiveSnapshot);
        RaiseEventManager.Instance.RegisterEvent((int)WorldEventID.SpawnObj, OnReceiveSpawn);
        RaiseEventManager.Instance.RegisterEvent((int)WorldEventID.DespawnObj, OnReceiveDespawn);
        RaiseEventManager.Instance.RegisterEvent((int)WorldEventID.WorldRPC, RPCHelper.OnReceive);
        RaiseEventManager.Instance.RegisterEvent((int)WorldEventID.SpawnRequest, OnReceiveSpawnRequest);
        m_initialized = true;
    }
    public void Release()
    {
        Debug.Log($"WorldManager Release");
        RaiseEventManager.Instance.UnRegisterEvent((int)WorldEventID.RequestSnapshot);
        RaiseEventManager.Instance.UnRegisterEvent((int)WorldEventID.WorldSnapshot);
        RaiseEventManager.Instance.UnRegisterEvent((int)WorldEventID.SpawnObj);
        RaiseEventManager.Instance.UnRegisterEvent((int)WorldEventID.DespawnObj);
        RaiseEventManager.Instance.UnRegisterEvent((int)WorldEventID.WorldRPC);
        RaiseEventManager.Instance.UnRegisterEvent((int)WorldEventID.SpawnRequest);
        ClearWorld();
        CurSlot = NoSlot;
        m_initialized = false;
    }
    public WorldData CurWorldData{get;set;}
    public ObjContainer<IWorldObj> Objects{get;} = new();
    // 스냅샷 적용 전에는 false. 방장은 항상 true
    public bool IsReady{get;private set;}

    #region 저장/불러오기 (방장 전용)
    // 불러온 월드의 슬롯. 멀티로 참여한 월드는 NoSlot이라 저장하지 않음
    public int CurSlot{get;private set;} = NoSlot;
    public const int NoSlot = -1;
    private static string WorldFileName(int slotindex) => $"WorldDat{slotindex}";

    // 방장이 게임 시작 시 한 번 호출. 파일이 없으면 새 월드
    public void LoadWorld(int slotindex)
    {
        ClearWorld();
        CurSlot = slotindex;
        CurWorldData = SaveManager.Instance.Load(WorldFileName(slotindex))?.GetData<WorldData>() ?? new WorldData();
        foreach (var d in CurWorldData.Entities) SpawnLocal(d);
        // NextId가 없던 이전 세이브도 있으므로, 불러온 오브젝트 ID와 겹치지 않게 최대 ID + 1 이상으로 맞춤
        int maxId = CurWorldData.Entities.Count > 0 ? CurWorldData.Entities.Max(x => x.ID) : -1;
        IdManager.Instance.SetLastIndex(Math.Max(CurWorldData.NextId, maxId + 1));
        IsReady = true;
    }
    // 주기적으로 / 종료 시 방장이 호출
    // 방을 나간 직후엔 IsMaster가 항상 true이므로 참여한 월드인지는 CurSlot으로 거름
    public void SaveWorld(int slotindex)
    {
        if (CurSlot == NoSlot || !IsMaster) return;
        CaptureWorldData();
        SaveManager.Instance.Save(WorldFileName(slotindex), SaveData.CreateSaveData(CurWorldData));
        Debug.Log($"[WorldManager] 월드 저장 : {WorldFileName(slotindex)}");
    }
    public void CaptureWorldData()
    {
        CurWorldData ??= new WorldData();
        CurWorldData.NextId = IdManager.Instance.Current;
        // 플레이어는 UID 기준으로 따로 저장하고, 휘발성 오브젝트(ITMPWorldObj)는 저장하지 않음
        CurWorldData.SaveEntities(Objects.GetList().Where(x => x is not IPlayerObj and not ITMPWorldObj));
        foreach (var pair in m_players) CurWorldData.SavePlayer(pair.Key, pair.Value);
    }
    #endregion

    #region 플레이어
    // 플레이어 오브젝트가 생성되면 Owner(ActorNumber)로 UID를 찾아 등록. 방장/수신 쪽 모두 호출됨
    // 방장은 전파 전에 해당 UID의 저장 데이터를 적용하므로, 다른 플레이어에게는 불러온 상태로 전송됨
    private void RegisterPlayer(IPlayerObj player)
    {
        string uid = GetUid(player.Owner);
        if (uid == null)
        {
            Debug.LogWarning($"[WorldManager] UID를 찾을 수 없는 플레이어 : Owner {player.Owner}");
            return;
        }
        m_players[uid] = player;
        if (!IsMaster) return;
        // 저장된 Owner는 이전 세션의 ActorNumber이므로 불러온 뒤 현재 값으로 되돌림
        int owner = player.Owner;
        if (CurWorldData?.LoadPlayer(uid, player) == true) player.Owner = owner;
    }
    private void UnregisterPlayer(IWorldObj player)
    {
        // 나간 플레이어는 Room에서 이미 빠져 UID를 못 찾을 수 있으므로 등록된 값으로 찾음
        string uid = m_players.FirstOrDefault(x => x.Value == player).Key;
        if (uid == null) return;
        if (IsMaster) CurWorldData?.SavePlayer(uid, player); // 나간 플레이어 데이터 보존
        m_players.Remove(uid);
    }
    // 오프라인이면 본인뿐이므로 로컬 UID
    private static string GetUid(int actor)
    {
        if (!PhotonNetwork.InRoom) return GameManager.Instance.LocalUserProfile.UID;
        return PhotonNetwork.CurrentRoom.GetPlayer(actor)?.CustomProperties[NetworkManager.UidProperty] as string;
    }
    public PlayerFSM GetLocalPlayer()
    {
        return GetPlayer(GameManager.Instance.LocalUserProfile.UID);
    }
    public PlayerFSM GetPlayer(string uid)
    {
        return (PlayerFSM)m_players[uid];
    }
    #endregion

    #region 생성/파괴 (방장이 호출하면 다른 플레이어에게 전파)
    // onInit : 다른 플레이어에게 전파되기 전에 초기 상태를 설정할 때 사용 (SaveData에 담겨 같이 전송됨)
    public IWorldObj Spawn(string prefabId, Vector3 position, Quaternion rotation, Action<IWorldObj> onInit = null)
    {
        if (!IsMaster)
        {
            Debug.LogWarning("[WorldManager] 방장만 월드 오브젝트를 생성할 수 있음");
            return null;
        }
        var obj = Instantiate(IdManager.Instance.Next, prefabId, position, rotation);
        if (obj == null) return null;
        onInit?.Invoke(obj);
        if (obj is IPlayerObj player) RegisterPlayer(player);
        Objects.Add(obj);
        OnSpawnedEvent?.Invoke(obj);
        if (!PhotonNetwork.InRoom) return obj; // 오프라인이면 전파할 대상 없음

        PacketData p = new();
        p.Write((int)WorldEventID.SpawnObj);
        WorldObjSaveData.Create(obj).Write(p);
        RaiseEventManager.Instance.SendPacket(p, new() { Receivers = ReceiverGroup.Others }, SendOptions.SendReliable);
        Debug.Log($"방장 소환 명령 : {prefabId}");
        return obj;
    }
    // 방장이 아닌 플레이어도 호출 가능한 생성 요청. 방장이 대신 Spawn하므로 결과는 OnSpawnedEvent로 받는다
    // 람다는 전송할 수 없으므로 초기 상태는 state(SaveData)로 넘기고, 방장 쪽에서 LoadData로 적용됨
    // 방장이 호출하면 바로 Spawn
    public void RequestSpawn(string prefabId, Vector3 position, Quaternion rotation, SaveData state = null)
    {
        if (IsMaster)
        {
            Spawn(prefabId, position, rotation, state != null ? obj => obj.LoadData(state) : null);
            return;
        }
        PacketData p = new();
        p.Write((int)WorldEventID.SpawnRequest);
        p.Write(prefabId);
        p.Write(position);
        p.Write(rotation.eulerAngles);
        p.WriteSaveData(state);
        RaiseEventManager.Instance.SendPacket(p, new() { Receivers = ReceiverGroup.MasterClient }, SendOptions.SendReliable);
    }
    public void Despawn(int id)
    {
        if (!IsMaster) return;
        DespawnLocal(id);
        if (!PhotonNetwork.InRoom) return;

        PacketData p = new();
        p.Write((int)WorldEventID.DespawnObj);
        p.Write(id);
        RaiseEventManager.Instance.SendPacket(p, new() { Receivers = ReceiverGroup.Others }, SendOptions.SendReliable);
    }
    public IWorldObj Find(int id) => Objects.GetList().Find(x => x.ID == id);
    // 로컬에 오브젝트가 생성되고 초기 상태(소유자 등)까지 적용된 뒤 호출. 방장/수신 쪽 모두 호출됨
    public event Action<IWorldObj> OnSpawnedEvent;
    #endregion

    #region 입장 동기화
    // 입장한 플레이어가 씬 로딩 후 호출. 방장에게 스냅샷 요청
    public void RequestSnapshot()
    {
        if (IsMaster)
        {
            IsReady = true;
            return;
        }
        IsReady = false;
        ClearWorld();
        CurSlot = NoSlot; // 남의 월드에 참여했으므로 저장 대상 아님
        PacketData p = new();
        p.Write((int)WorldEventID.RequestSnapshot);
        RaiseEventManager.Instance.SendPacket(p, new() { Receivers = ReceiverGroup.MasterClient }, SendOptions.SendReliable);
    }
    private void OnRequestSnapshot(EventData e, PacketData p)
    {
        if (!IsMaster) return;
        CurWorldData ??= new WorldData();
        // 저장과 달리 플레이어/휘발성 오브젝트까지 현재 월드의 모든 오브젝트를 보냄
        // 입장한 플레이어 본인의 저장 데이터는 방장이 그 플레이어를 생성할 때 적용됨
        var snapshot = new WorldSnapshot()
        {
            Day = CurWorldData.Day,
            Time = CurWorldData.Time,
            Entities = Objects.GetList().Select(WorldObjSaveData.Create).ToList()
        };

        PacketData r = new();
        r.Write((int)WorldEventID.WorldSnapshot);
        snapshot.Write(r);
        RaiseEventManager.Instance.SendPacket(r, new() { TargetActors = new[] { e.Sender } }, SendOptions.SendReliable);
    }
    private void OnReceiveSnapshot(EventData e, PacketData p)
    {
        if (IsMaster) return;
        var snapshot = WorldSnapshot.Read(p);

        ClearWorld();
        CurWorldData = new WorldData() { Day = snapshot.Day, Time = snapshot.Time };
        foreach (var d in snapshot.Entities) SpawnLocal(d);

        IsReady = true;
        // 스냅샷 전에 도착한 이벤트 처리
        while (m_pendingEvents.Count > 0) m_pendingEvents.Dequeue()();
    }
    #endregion

    #region 수신
    private void OnReceiveSpawn(EventData e, PacketData p)
    {
        if (IsMaster) return;
        var d = WorldObjSaveData.Read(p);
        Debug.Log($"소환 명령 : {d.PrefabId}");
        RunOrQueue(() => SpawnLocal(d));
    }
    private void OnReceiveSpawnRequest(EventData e, PacketData p)
    {
        if (!IsMaster) return;
        string prefabId = p;
        Vector3 position = p;
        Vector3 rotation = p;
        var state = p.ReadSaveData();
        RequestSpawn(prefabId, position, Quaternion.Euler(rotation), state);
    }
    private void OnReceiveDespawn(EventData e, PacketData p)
    {
        if (IsMaster) return;
        int id = p;
        RunOrQueue(() => DespawnLocal(id));
    }
    // 스냅샷 적용 전이면 쌓아뒀다가 적용 후 실행
    public void RunOrQueue(Action action)
    {
        if (IsReady) action();
        else m_pendingEvents.Enqueue(action);
    }
    #endregion

    #region 로컬 처리
    // 저장/수신 데이터로 오브젝트를 로컬에 재구성
    private void SpawnLocal(WorldObjSaveData d)
    {
        if (Find(d.ID) != null) return; // 스냅샷과 이벤트가 겹친 경우
        var obj = Instantiate(d.ID, d.PrefabId, d.Position, Quaternion.Euler(d.Rotation));
        if (obj == null) return;
        // Instantiate 시점에 OnEnable이 먼저 돌기 때문에 그 뒤에 상태를 덮어씀
        obj.LoadData(d.State);
        if (obj is IPlayerObj player) RegisterPlayer(player);
        Objects.Add(obj);
        OnSpawnedEvent?.Invoke(obj);
    }
    private void DespawnLocal(int id)
    {
        var obj = Find(id);
        if (obj == null) return;
        if (obj is IPlayerObj) UnregisterPlayer(obj);
        Objects.Remove(obj);
        Object.Destroy(obj.gameObject);
    }
    // Photon을 거치지 않는 로컬 생성. PhotonView가 붙어있어도 ViewID가 없으므로 RPC/PhotonView 동기화는 동작하지 않음
    private IWorldObj Instantiate(int id, string prefabId, Vector3 position, Quaternion rotation)
    {
        if (!m_prefabs.TryGetValue(prefabId, out var prefab))
        {
            Debug.LogError($"[WorldManager] 카탈로그에 없는 프리팹 : {prefabId}");
            return null;
        }
        var go = Object.Instantiate(prefab, position, rotation);
        if (!go.TryGetComponent(out IWorldObj obj))
        {
            Debug.LogError($"[WorldManager] IWorldObj가 없는 프리팹 : {prefabId}");
            Object.Destroy(go);
            return null;
        }
        obj.ID = id;
        obj.PrefabId = prefabId;
        obj.InitWorldObj(id, prefabId);
        return obj;
    }
    private void ClearWorld()
    {
        foreach (var obj in Objects.GetList())
        {
            try{
            if (obj.gameObject != null) Object.Destroy(obj.gameObject);
            }catch(Exception e)
            {
                Debug.Log("ClearWorld Error");
            }
        }
            
        Objects.Clear();
        m_players.Clear();
        m_pendingEvents.Clear();
        IsReady = false;
    }
    #endregion

    private bool IsMaster => !PhotonNetwork.InRoom || PhotonNetwork.IsMasterClient; // 오프라인이면 본인이 방장

    private NetworkPrefabCatalog m_catalog;
    private Dictionary<string, GameObject> m_prefabs;
    private readonly Dictionary<string, IWorldObj> m_players = new();
    private readonly Queue<Action> m_pendingEvents = new();
    private bool m_initialized;
}
