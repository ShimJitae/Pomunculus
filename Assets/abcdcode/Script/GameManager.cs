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
        if (m_runWorldSyncTest) TestRegister();
    }
    private void OnDestroy()
    {
        if (m_testRegistered) TestUnregister();
    }

    #region 월드 동기화 테스트
    // 오프라인 : TestScene에 들어오면 WorldTestUI가 TestStartOffline 호출 -> 내 큐브 생성
    // 온라인 : 방에 들어와 TestScene 로딩이 끝나면 자기 소유의 TestCube를 하나 생성 (생성 자체는 방장이 처리)
    // 나중에 들어온 플레이어는 WorldManager 스냅샷으로 기존 큐브를 받아 재구성
    public const string TitleSceneName = "abcdcode_TitleTestScene";
    public const string TestSceneName = "abcdcode_TestScene";

    private void TestRegister()
    {
        NetworkManager.Instance.OnJoinedRoomEvent += TestOnJoinedRoom;
        NetworkManager.Instance.OnLeftRoomEvent += TestOnLeftRoom;
        NetworkManager.Instance.OnStateChangedEvent += TestOnStateChanged;
        SceneManager.sceneLoaded += TestOnSceneLoaded;
        m_testRegistered = true;
    }
    private void TestUnregister()
    {
        NetworkManager.Instance.OnJoinedRoomEvent -= TestOnJoinedRoom;
        NetworkManager.Instance.OnLeftRoomEvent -= TestOnLeftRoom;
        NetworkManager.Instance.OnStateChangedEvent -= TestOnStateChanged;
        SceneManager.sceneLoaded -= TestOnSceneLoaded;
        m_testRegistered = false;
    }
    private void TestOnJoinedRoom()
    {
        // 타이틀에서 참가하면 방장 씬(TestScene)으로 로딩이 끝난 뒤 시작해야 생성한 오브젝트가 씬 전환에 날아가지 않음
        m_testPending = true;
        TryTestStart(SceneManager.GetActiveScene().name);
    }
    private void TestOnStateChanged(NetworkState state) => TryTestStart(SceneManager.GetActiveScene().name);
    private void TestOnSceneLoaded(Scene scene, LoadSceneMode mode) => TryTestStart(scene.name);
    private void TryTestStart(string sceneName)
    {
        if (!m_testPending || NetworkManager.Instance.State != NetworkState.InRoom || sceneName != TestSceneName) return;
        TestStart();
    }
    // TestScene에 오프라인으로 들어왔을 때 호출. 오프라인은 방장 취급
    public void TestStartOffline()
    {
        if (PhotonNetwork.InRoom) return;
        WorldManager.Instance.Init();
        WorldManager.Instance.RequestSnapshot(); // 방장이면 바로 준비 완료
        Debug.Log("[Test] 오프라인 시작");
        TestSpawnCube(PhotonNetwork.LocalPlayer.ActorNumber);
    }
    // 큐브 추가 생성. 방장(오프라인 포함)은 직접, 그 외는 방장에게 요청
    public void TestRequestSpawn()
    {
        if (!PhotonNetwork.InRoom || PhotonNetwork.IsMasterClient)
        {
            TestSpawnCube(PhotonNetwork.LocalPlayer.ActorNumber);
            return;
        }
        PacketData p = new();
        p.Write((int)WorldEventID.TestSpawnRequest);
        RaiseEventManager.Instance.SendPacket(p, new() { Receivers = ReceiverGroup.MasterClient }, SendOptions.SendReliable);
    }
    private void TestOnLeftRoom()
    {
        m_testPending = false;
        RaiseEventManager.Instance.UnRegisterEvent((int)WorldEventID.TestSpawnRequest);
        WorldManager.Instance.Release(); // 월드 오브젝트 정리
    }
    private void TestStart()
    {
        m_testPending = false;
        WorldManager.Instance.Init();
        RaiseEventManager.Instance.RegisterEvent((int)WorldEventID.TestSpawnRequest, TestOnSpawnRequest);

        // 방장은 바로 준비 완료, 그 외는 방장에게 스냅샷 요청
        WorldManager.Instance.RequestSnapshot();
        Debug.Log($"[Test] 입장 완료. 방장 여부 : {PhotonNetwork.IsMasterClient}, ActorNumber : {PhotonNetwork.LocalPlayer.ActorNumber}");

        if (PhotonNetwork.IsMasterClient)
        {
            // 오프라인에서 방을 만든 경우 : 오프라인 때 ActorNumber(-1)로 소유하던 큐브를 방에서 받은 번호로 옮김
            int myActor = PhotonNetwork.LocalPlayer.ActorNumber;
            bool hasMine = false;
            foreach (var obj in WorldManager.Instance.Objects.GetList())
            {
                if (obj is not TestWorldObject t) continue;
                if (t.Owner == OfflineActor) t.SetOwner(myActor);
                if (t.Owner == myActor) hasMine = true;
            }
            if (!hasMine) TestSpawnCube(myActor);
        }
        else
        {
            // 스냅샷 요청 다음에 보내므로 방장은 스냅샷 -> 생성 순서로 처리함
            TestRequestSpawn();
        }
    }
    private void TestOnSpawnRequest(EventData e, PacketData p)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        TestSpawnCube(e.Sender);
    }
    private void TestSpawnCube(int ownerActor)
    {
        int count = WorldManager.Instance.Objects.GetList().Count;
        var pos = new Vector3(count % 5 * 1.5f - 3f, 0.5f, count / 5 * 1.5f); // 생성 순서대로 격자 배치
        var obj = WorldManager.Instance.Spawn(m_testPrefabId, pos, Quaternion.identity,
            o => { if (o is IPlayerObj t) t.Owner = ownerActor; });
        Debug.Log($"[Test] TestCube 생성 (ID : {obj?.ID}, 소유자 : {ownerActor}, 월드 오브젝트 수 : {WorldManager.Instance.Objects.GetList().Count})");
    }

    [ContextMenu("테스트/월드 오브젝트 목록 출력")]
    private void TestLogWorldObjects()
    {
        foreach (var obj in WorldManager.Instance.Objects.GetList())
        {
            var owner = obj is TestWorldObject t ? t.Owner : -1;
            Debug.Log($"[Test] ID : {obj.ID}, Prefab : {obj.PrefabId}, 소유자 : {owner}, 위치 : {obj.gameObject.transform.position}");
        }
    }

    [Header("월드 동기화 테스트")]
    [SerializeField] private bool m_runWorldSyncTest = true;
    [SerializeField] private string m_testPrefabId = "TestCube";
    private bool m_testPending;
    private bool m_testRegistered;
    private const int OfflineActor = -1; // 방 밖에서의 PhotonNetwork.LocalPlayer.ActorNumber
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