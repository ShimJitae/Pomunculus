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
        m_keyDic = new BiDictionary<string, int>();
        m_prefabs = new Dictionary<int, GameObject>();
        NetworkPrefabCatalog.ValidateHashIds(m_catalog.Entries, Debug.LogError);
        foreach(var e in m_catalog.Entries)
        {
            int hashId = NetworkPrefabCatalog.GetHashId(e.name);
            if (m_prefabs.ContainsKey(hashId)) continue; // 충돌은 위에서 로그로 보고됨. 먼저 등록된 쪽 유지
            m_keyDic.Add(e.name, hashId);
            m_prefabs[hashId] = e;
        }
        //m_prefabs = m_catalog.Entries.ToDictionary(x => x.name);
        RaiseEventManager.Instance.RegisterEvent((int)WorldEventID.RequestSnapshot, OnRequestSnapshot);
        RaiseEventManager.Instance.RegisterEvent((int)WorldEventID.WorldSnapshot, OnReceiveSnapshot);
        RaiseEventManager.Instance.RegisterEvent((int)WorldEventID.SpawnObj, OnReceiveSpawn);
        RaiseEventManager.Instance.RegisterEvent((int)WorldEventID.DespawnObj, OnReceiveDespawn);
        RaiseEventManager.Instance.RegisterEvent((int)WorldEventID.WorldRPC, RPCHelper.OnReceive);
        RaiseEventManager.Instance.RegisterEvent((int)WorldEventID.SpawnRequest, OnReceiveSpawnRequest);
        RaiseEventManager.Instance.RegisterEvent((int)WorldEventID.DayChanged, OnReceiveDayChanged);
        RaiseEventManager.Instance.RegisterEvent((int)WorldEventID.PlayerStateSync, OnReceivePlayerState);
        RegionSpawner.Instance.Init();
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
        RaiseEventManager.Instance.UnRegisterEvent((int)WorldEventID.DayChanged);
        RaiseEventManager.Instance.UnRegisterEvent((int)WorldEventID.PlayerStateSync);
        RegionSpawner.Instance.Release();
        ClearWorld();
        CurSlot = NoSlot;
        DayLength = WorldSlotData.DefaultDayTime;
        m_initialized = false;
    }
    public WorldData CurWorldData{get;set;}
    public ObjContainer<IWorldObj> Objects{get;} = new();
    // 스냅샷 적용 전에는 false. 방장은 항상 true
    public bool IsReady{get;private set;}

    #region 시간/날짜
    // 하루 길이(초). 방장은 불러온 슬롯의 WorldSlotData.DayTime, 참여자는 방장이 보낸 스냅샷 값을 쓴다
    public static float DayLength{get;private set;} = WorldSlotData.DefaultDayTime;
    private static float ValidDayLength(float v) => v > 0f ? v : WorldSlotData.DefaultDayTime;
    // 바뀐 Day. 방장/수신 쪽 모두 호출됨
    public event Action<int> OnDayChangedEvent;
    // 바뀐 계절. 날짜가 넘어가며 계절이 바뀔 때 OnDayChangedEvent 다음에 호출. 방장/수신 쪽 모두 호출됨
    // 월드를 불러오거나 스냅샷을 받을 때는 호출되지 않으므로 초기값은 CurSeason으로 확인
    public event Action<Season> OnSeasonChangedEvent;
    public Season CurSeason => CurWorldData?.Season ?? Season.Spring;

    // GameManager.Update에서 매 프레임 호출
    // 시간은 모두가 로컬로 흘려보내고, 날짜가 넘어가는 건 방장만 결정해서 전파한다
    // 방장이 아니면 하루 끝에서 멈춰 방장의 DayChanged를 기다림
    public void Update(float deltaTime)
    {
        if (!IsReady || CurWorldData == null) return;
        CurWorldData.Time += deltaTime;
        if (!IsMaster)
        {
            CurWorldData.Time = Mathf.Min(CurWorldData.Time, DayLength);
            return;
        }
        if (CurWorldData.Time < DayLength) return;

        Season prevSeason = CurWorldData.Season;
        while (CurWorldData.Time >= DayLength)
        {
            CurWorldData.Time -= DayLength;
            CurWorldData.Day++;
        }
        OnDayChangedEvent?.Invoke(CurWorldData.Day);
        if (CurWorldData.Season != prevSeason) OnSeasonChangedEvent?.Invoke(CurWorldData.Season);
        if (!PhotonNetwork.InRoom) return;

        // 남은 Time도 같이 보내서 다른 플레이어의 오차를 날짜마다 맞춤
        PacketData p = new();
        p.Write((int)WorldEventID.DayChanged);
        p.Write(CurWorldData.Day);
        p.Write(CurWorldData.Time);
        RaiseEventManager.Instance.SendPacket(p, new() { Receivers = ReceiverGroup.Others }, SendOptions.SendReliable);
    }
    private void OnReceiveDayChanged(EventData e, PacketData p)
    {
        if (IsMaster) return;
        int day = p;
        float time = p;
        // 스냅샷 전에 보낸 날짜는 스냅샷에 이미 반영되어 오므로 무시
        if (!IsReady || CurWorldData == null || day < CurWorldData.Day) return;
        bool changed = day != CurWorldData.Day;
        Season prevSeason = CurWorldData.Season;
        CurWorldData.Day = day;
        CurWorldData.Time = time;
        if (changed) OnDayChangedEvent?.Invoke(day);
        if (CurWorldData.Season != prevSeason) OnSeasonChangedEvent?.Invoke(CurWorldData.Season);
    }
    #endregion

    #region 저장/불러오기 (방장 전용)
    // 불러온 월드의 슬롯. 멀티로 참여한 월드는 NoSlot이라 저장하지 않음
    public int CurSlot{get;private set;} = NoSlot;
    public const int NoSlot = -1;
    private static string WorldFileName(int slotindex) => $"WorldDat{slotindex}";

    public bool HasWorld(int slotindex) => SaveManager.Instance.Exists(WorldFileName(slotindex));
    // 새 게임 시작 전에 호출. 이후 LoadWorld에서 파일이 없으므로 새 월드로 시작됨
    public void DeleteWorld(int slotindex) => SaveManager.Instance.Delete(WorldFileName(slotindex));

    // 방장이 게임 시작 시 한 번 호출. 파일이 없으면 새 월드
    public void LoadWorld(int slotindex)
    {
        ClearWorld();
        CurSlot = slotindex;
        var slot = WorldSlotLoader.Instance.SaveInfo.Slots.Find(x => x.SlotId == slotindex);
        DayLength = ValidDayLength(slot?.DayTime ?? WorldSlotData.DefaultDayTime);
        if (slot != null && !slot.IsCompatible)
        {
            // 슬롯 목록에서 걸러지므로 정상 경로로는 오지 않음. 이전 버전 세이브를 덮어쓰지 않도록 저장도 막고 빈 월드로 시작
            Debug.LogError($"[WorldManager] 버전이 다른 월드 슬롯 : {slotindex} (슬롯 v{slot.WorldVersion}, 현재 v{GameManager.Instance.Version})");
            CurSlot = NoSlot;
            CurWorldData = new WorldData();
            IsReady = true;
            return;
        }
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
        if (!IsMaster)
        {
            // 인벤토리는 소유자 클라이언트에서만 바뀌므로, 바뀔 때마다 방장에게 보내 방장 쪽 사본/저장 데이터를 맞춤
            // LoadData로 초기 상태를 적용한 뒤 등록되므로 불러올 때의 변경은 전송되지 않음
            if (player.Owner == PhotonNetwork.LocalPlayer.ActorNumber && player is PlayerFSM fsm && fsm.Inventory != null)
                fsm.Inventory.Changed += SendLocalPlayerState;
            return;
        }
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
    // 방장이 아닌 소유자가 호출. 자기 플레이어의 현재 상태를 방장에게 전송
    public void SendLocalPlayerState()
    {
        if (IsMaster || !PhotonNetwork.InRoom) return;
        string uid = GameManager.Instance.LocalUserProfile.UID;
        if (!m_players.TryGetValue(uid, out var player) || player == null) return;
        var state = new SaveData();
        player.SaveData(state);

        PacketData p = new();
        p.Write((int)WorldEventID.PlayerStateSync);
        p.WriteSaveData(state);
        RaiseEventManager.Instance.SendPacket(p, new() { Receivers = ReceiverGroup.MasterClient }, SendOptions.SendReliable);
    }
    // 방장 : 소유자가 보낸 상태를 방장 쪽 사본에 적용. 나갈 때 UnregisterPlayer에서 이 사본으로 저장됨
    private void OnReceivePlayerState(EventData e, PacketData p)
    {
        if (!IsMaster) return;
        var state = p.ReadSaveData();
        string uid = GetUid(e.Sender);
        if (state == null || uid == null || !m_players.TryGetValue(uid, out var player)) return;
        if (player is not IPlayerObj po || po.Owner != e.Sender) return; // 다른 사람의 플레이어는 덮어쓰지 않음
        player.LoadData(state);
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
        => Spawn(NetworkPrefabCatalog.GetHashId(prefabId), position, rotation, onInit);
    // prefabHash : NetworkPrefabCatalog.GetHashId(이름)
    public IWorldObj Spawn(int prefabHash, Vector3 position, Quaternion rotation, Action<IWorldObj> onInit = null)
    {
        if (!IsMaster)
        {
            Debug.LogWarning("[WorldManager] 방장만 월드 오브젝트를 생성할 수 있음");
            return null;
        }
        var obj = Instantiate(IdManager.Instance.Next, prefabHash, position, rotation);
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
        Debug.Log($"방장 소환 명령 : {obj.PrefabId}");
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
            DayLength = DayLength,
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
        DayLength = ValidDayLength(snapshot.DayLength);
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
        Debug.Log($"소환 명령 : {PrefabName(d.PrefabHash)}");
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
        var obj = Instantiate(d.ID, d.PrefabHash, d.Position, Quaternion.Euler(d.Rotation));
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
    private IWorldObj Instantiate(int id, int prefabHash, Vector3 position, Quaternion rotation)
    {
        if (!m_keyDic.TryGetKey(prefabHash, out string prefabId) || !m_prefabs.TryGetValue(prefabHash, out var prefab))
        {
            Debug.LogError($"[WorldManager] 카탈로그에 없는 프리팹 해시 : {prefabHash}");
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
    private BiDictionary<string,int> m_keyDic = new BiDictionary<string, int>();
    private string PrefabName(int prefabHash) => m_keyDic.TryGetKey(prefabHash, out var name) ? name : $"(알 수 없음 {prefabHash})";
    private Dictionary<int, GameObject> m_prefabs;
    private readonly Dictionary<string, IWorldObj> m_players = new();
    private readonly Queue<Action> m_pendingEvents = new();
    private bool m_initialized;
}
