using System;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.SceneManagement;
public class GameManager : SingletonBehavior<GameManager>
{
    public override void Awake()
    {
        base.Awake();
        if (Instance != this) return; // 씬을 옮기며 생긴 중복 GameManager는 파괴되므로 초기화하지 않음
        DontDestroyOnLoad(gameObject);
        UserProfileInit();
        // 오프라인으로 시작할 수 있도록 자동 접속하지 않음. 접속은 멀티 플레이를 시작할 때 NetworkManager.Init으로
        PhotonNetwork.PrefabPool = new NetworkPrefabPool(Resources.Load<NetworkPrefabCatalog>("NetworkPrefabCatalog"));
        PlayerSpawnRegister();
    }
    private void OnDestroy()
    {
        if (m_playerSpawnRegistered) PlayerSpawnUnregister();
    }

    #region 플레이어 생성
    // 오프라인 : 월드 씬에 들어오면 WorldTestUI가 StartWorldOffline 호출 -> 내 플레이어 생성
    // 온라인 : 방에 들어와 월드 씬 로딩이 끝나면 자기 소유의 플레이어를 하나 생성 (생성 자체는 방장이 처리)
    // 플레이어가 방을 나가면 방장이 그 플레이어 소유의 오브젝트를 제거
    public const string TitleSceneName = "abcdcode_TitleTestScene";
    public const string TestSceneName = "abcdcode_TestScene";

    private void PlayerSpawnRegister()
    {
        NetworkManager.Instance.OnJoinedRoomEvent += OnJoinedRoom;
        NetworkManager.Instance.OnLeftRoomEvent += OnLeftRoom;
        NetworkManager.Instance.OnPlayerLeftRoomEvent += OnPlayerLeftRoom;
        NetworkManager.Instance.OnStateChangedEvent += OnNetworkStateChanged;
        SceneManager.sceneLoaded += OnSceneLoaded;
        m_playerSpawnRegistered = true;
    }
    private void PlayerSpawnUnregister()
    {
        NetworkManager.Instance.OnJoinedRoomEvent -= OnJoinedRoom;
        NetworkManager.Instance.OnLeftRoomEvent -= OnLeftRoom;
        NetworkManager.Instance.OnPlayerLeftRoomEvent -= OnPlayerLeftRoom;
        NetworkManager.Instance.OnStateChangedEvent -= OnNetworkStateChanged;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        m_playerSpawnRegistered = false;
    }
    private void OnJoinedRoom()
    {
        // 타이틀에서 참가하면 방장 씬으로 로딩이 끝난 뒤 시작해야 생성한 오브젝트가 씬 전환에 날아가지 않음
        m_worldPending = true;
        TryStartWorld(SceneManager.GetActiveScene().name);
    }
    private void OnNetworkStateChanged(NetworkState state) => TryStartWorld(SceneManager.GetActiveScene().name);
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) => TryStartWorld(scene.name);
    private void TryStartWorld(string sceneName)
    {
        if (!m_worldPending || NetworkManager.Instance.State != NetworkState.InRoom || sceneName != TestSceneName) return;
        StartWorldOnline();
    }
    // 월드 씬에 오프라인으로 들어왔을 때 호출. 오프라인은 방장 취급
    public void StartWorldOffline()
    {
        if (PhotonNetwork.InRoom) return;
        WorldManager.Instance.Init();
        WorldManager.Instance.RequestSnapshot(); // 방장이면 바로 준비 완료
        Debug.Log("[GameManager] 오프라인 월드 시작");
        SpawnPlayer(PhotonNetwork.LocalPlayer.ActorNumber);
    }
    private void StartWorldOnline()
    {
        m_worldPending = false;
        WorldManager.Instance.Init();
        RaiseEventManager.Instance.RegisterEvent((int)WorldEventID.PlayerSpawnRequest, OnPlayerSpawnRequest);

        // 방장은 바로 준비 완료, 그 외는 방장에게 스냅샷 요청
        WorldManager.Instance.RequestSnapshot();
        Debug.Log($"[GameManager] 월드 입장. 방장 여부 : {PhotonNetwork.IsMasterClient}, ActorNumber : {PhotonNetwork.LocalPlayer.ActorNumber}");

        if (PhotonNetwork.IsMasterClient)
        {
            // 오프라인에서 방을 만든 경우 : 오프라인 때 ActorNumber(-1)로 소유하던 플레이어를 방에서 받은 번호로 옮김
            int myActor = PhotonNetwork.LocalPlayer.ActorNumber;
            bool hasMine = false;
            foreach (var obj in WorldManager.Instance.Objects.GetList())
            {
                if (obj is not IPlayerObj p) continue;
                if (p.Owner == OfflineActor) p.Owner = myActor;
                if (p.Owner == myActor) hasMine = true;
            }
            if (!hasMine) SpawnPlayer(myActor);
        }
        else
        {
            // 스냅샷 요청 다음에 보내므로 방장은 스냅샷 -> 생성 순서로 처리함
            PacketData p = new();
            p.Write((int)WorldEventID.PlayerSpawnRequest);
            RaiseEventManager.Instance.SendPacket(p, new() { Receivers = ReceiverGroup.MasterClient }, SendOptions.SendReliable);
        }
    }
    private void OnPlayerSpawnRequest(EventData e, PacketData p)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        SpawnPlayer(e.Sender);
    }
    private void SpawnPlayer(int ownerActor)
    {
        var obj = WorldManager.Instance.Spawn(PlayerPrefabId, PlayerSpawnPosition, Quaternion.identity,
            o => { if (o is IPlayerObj p) p.Owner = ownerActor; });
        Debug.Log($"[GameManager] 플레이어 생성 (ID : {obj?.ID}, 소유자 : {ownerActor})");
    }
    // 다른 플레이어가 나가면 방장이 그 플레이어 소유의 오브젝트를 제거
    private void OnPlayerLeftRoom(Player player)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        var leftObjs = WorldManager.Instance.Objects.GetList()
            .FindAll(o => o is IPlayerObj p && p.Owner == player.ActorNumber);
        foreach (var obj in leftObjs) WorldManager.Instance.Despawn(obj.ID);
    }
    // 내가 나가면 월드 전체 정리
    private void OnLeftRoom()
    {
        m_worldPending = false;
        RaiseEventManager.Instance.UnRegisterEvent((int)WorldEventID.PlayerSpawnRequest);
        WorldManager.Instance.Release();
    }

    private const string PlayerPrefabId = "Player";
    private static readonly Vector3 PlayerSpawnPosition = new(0f, 1f, 0f);
    private const int OfflineActor = -1; // 방 밖에서의 PhotonNetwork.LocalPlayer.ActorNumber
    private bool m_worldPending;
    private bool m_playerSpawnRegistered;
    #endregion
    public void UserProfileInit()
    {
        if(SaveManager.Instance.Load(UserProfile) == null)
        {
            var uid = Guid.NewGuid().ToString();
            string userName = "Sans";
            LocalUserProfile = new()
            {
                UID = uid,
                UserName = userName
            };
            //SaveProfile();
            /*
            d.Child["uid"] = SaveData.CreateSaveData(uid);
            d.Child["userName"] = SaveData.CreateSaveData(userName);
            */
        }
        else
        {
            LocalUserProfile = SaveManager.Instance.Load(UserProfile).GetData<UserProfile>();
        }
    }
    public void SaveProfile()
    {
        var d = SaveData.CreateSaveData(LocalUserProfile);
        SaveManager.Instance.Save(UserProfile,d);
    }

    [SerializeField]private int m_version;
    private UserProfile m_profile;
    public UserProfile LocalUserProfile
    {
        get
        {

            return m_profile.Copy();
        }
        private set
        {
            m_profile = value;
            SaveProfile();
        }
    }
    public int Version => m_version;
    public const string UserProfile = "UserProfile";
}
public class UserProfile
{
    public string UID{get;set;}
    public string UserName{get;set;}
    public UserProfile Copy()
    {
        return new()
        {
            UID = this.UID,
            UserName = this.UserName
        };
    }
}
