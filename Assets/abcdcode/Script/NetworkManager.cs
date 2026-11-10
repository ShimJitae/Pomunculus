using System;
using System.Collections.Generic;
using System.Linq;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum NetworkState
{
    Offline,            // 연결 안 됨
    Connecting,         // 최초 접속 ~ 마스터 서버 접속
    ConnectedToMaster,  // 마스터 서버에 접속했지만 로비 밖 (대기)
    JoiningLobby,       // 로비 진입 중
    InLobby,            // 로비 (대기)
    JoiningRoom,        // 룸 생성/참가 요청 ~ 입장 완료
    LoadingScene,       // 룸 안에서 동기화된 씬 로딩 중
    InRoom,             // 룸 (대기)
    LeavingRoom,        // 룸 퇴장 ~ 로비 복귀
}

public class NetworkManager : Singleton<NetworkManager>, IConnectionCallbacks, ILobbyCallbacks, IMatchmakingCallbacks, IInRoomCallbacks
{
    // 연결이 끊긴 뒤 다시 호출하면 재접속
    public void Init()
    {
        if (!m_initialized)
        {
            PhotonNetwork.AddCallbackTarget(this);
            SceneManager.sceneLoaded += OnSceneLoaded;
            m_initialized = true;
        }
        // 방 밖에서 설정하면 입장할 때 같이 전송되므로, 다른 플레이어가 ActorNumber로 UID를 바로 찾을 수 있음
        PhotonNetwork.LocalPlayer.SetCustomProperties(new Hashtable { { UidProperty, GameManager.Instance.LocalUserProfile.UID } });
        if (PhotonNetwork.IsConnected) return;
        PhotonNetwork.GameVersion = $"v{GameManager.Instance.Version}";
        PhotonNetwork.NickName = GameManager.Instance.LocalUserProfile.UserName;
        PhotonNetwork.AutomaticallySyncScene = true;
        State = PhotonNetwork.ConnectUsingSettings() ? NetworkState.Connecting : NetworkState.Offline;
    }
    public void Release()
    {
        PhotonNetwork.RemoveCallbackTarget(this);
        SceneManager.sceneLoaded -= OnSceneLoaded;
        m_initialized = false;
    }

    #region 상태
    public NetworkState State
    {
        get => m_state;
        private set
        {
            if (m_state == value) return;
            m_state = value;
            OnStateChangedEvent?.Invoke(value);
        }
    }
    // 로딩 UI 표시 여부
    public bool IsBusy => State is NetworkState.Connecting or NetworkState.JoiningLobby or NetworkState.JoiningRoom or NetworkState.LoadingScene or NetworkState.LeavingRoom;
    // 진행 문구를 세세하게 보여줄 때 보조로 사용
    public ClientState DetailState => PhotonNetwork.NetworkClientState;
    #endregion

    #region 로비
    // 마스터 서버 접속이 끝나면 자동으로 로비에 들어감. 수동으로 다시 들어가야 할 때 호출
    public bool JoinLobby()
    {
        if (!PhotonNetwork.IsConnectedAndReady)
        {
            Debug.LogWarning("[Network] 마스터 서버에 연결되지 않아 로비에 접속할 수 없음");
            return false;
        }
        if (PhotonNetwork.InLobby) return true;
        if (!PhotonNetwork.JoinLobby()) return false;
        // 룸에서 나오는 중이면 "로비 복귀 중" 상태를 유지
        if (State != NetworkState.LeavingRoom) State = NetworkState.JoiningLobby;
        return true;
    }
    public void LeaveLobby()
    {
        if (PhotonNetwork.InLobby) PhotonNetwork.LeaveLobby();
    }
    #endregion

    #region 룸 생성
    public bool CreateRoom(string roomName, int maxPlayers = 4, bool isVisible = true, Hashtable customProperties = null)
    {
        if (!CanMatchmaking()) return false;
        var options = new RoomOptions
        {
            MaxPlayers = maxPlayers,
            IsVisible = isVisible,
            IsOpen = true,
            CustomRoomProperties = customProperties,
            // 로비의 룸 목록에서 볼 수 있도록 커스텀 프로퍼티 키를 모두 노출
            CustomRoomPropertiesForLobby = customProperties?.Keys.Select(k => k.ToString()).ToArray()
        };
        // roomName이 비어 있으면 서버가 이름을 자동 생성
        return BeginJoinRoom(PhotonNetwork.CreateRoom(string.IsNullOrEmpty(roomName) ? null : roomName, options));
    }
    #endregion

    #region 룸 탐색
    // 로비에 있는 동안 OnRoomListUpdate로 받은 룸 목록을 캐싱
    public IReadOnlyCollection<RoomInfo> RoomList => m_roomList.Values;
    public RoomInfo FindRoom(string roomName)
    {
        m_roomList.TryGetValue(roomName, out var room);
        return room;
    }
    public List<RoomInfo> FindRooms(Func<RoomInfo, bool> predicate)
    {
        return m_roomList.Values.Where(predicate).ToList();
    }
    // 입장 가능한 룸만
    public List<RoomInfo> FindJoinableRooms()
    {
        return FindRooms(r => r.IsOpen && r.IsVisible && (r.MaxPlayers == 0 || r.PlayerCount < r.MaxPlayers));
    }
    private void UpdateRoomList(List<RoomInfo> roomList)
    {
        foreach (var room in roomList)
        {
            if (room.RemovedFromList) m_roomList.Remove(room.Name);
            else m_roomList[room.Name] = room;
        }
    }
    #endregion

    #region 룸 참가
    public bool JoinRoom(string roomName)
    {
        if (!CanMatchmaking()) return false;
        return BeginJoinRoom(PhotonNetwork.JoinRoom(roomName));
    }
    public bool JoinRandomRoom(Hashtable expectedProperties = null, int expectedMaxPlayers = 0)
    {
        if (!CanMatchmaking()) return false;
        return BeginJoinRoom(PhotonNetwork.JoinRandomRoom(expectedProperties, expectedMaxPlayers));
    }
    // 없으면 만들고 있으면 참가
    public bool JoinOrCreateRoom(string roomName, int maxPlayers = 4)
    {
        if (!CanMatchmaking()) return false;
        var options = new RoomOptions { MaxPlayers = maxPlayers, IsVisible = true, IsOpen = true };
        return BeginJoinRoom(PhotonNetwork.JoinOrCreateRoom(roomName, options, TypedLobby.Default));
    }
    public void LeaveRoom()
    {
        if (!PhotonNetwork.InRoom) return;
        if (PhotonNetwork.LeaveRoom()) State = NetworkState.LeavingRoom;
    }
    private bool BeginJoinRoom(bool requested)
    {
        if (requested) State = NetworkState.JoiningRoom;
        return requested;
    }
    // 룸 생성/참가 실패 시 로비로 복귀
    // 게임 서버에서 실패한 경우 마스터 서버로 돌아오면서 로비 밖이 되므로 다시 로비에 들어가야 함
    private void ReturnToLobby()
    {
        if (PhotonNetwork.InLobby) State = NetworkState.InLobby;
        else if (!JoinLobby()) State = NetworkState.ConnectedToMaster;
    }
    #endregion

    #region 씬 동기화
    // 마스터 클라이언트가 씬을 바꿀 때 사용. AutomaticallySyncScene에 의해 다른 클라이언트도 같은 씬을 로딩함
    // SceneManager.LoadScene을 직접 쓰면 룸 프로퍼티가 갱신되지 않아 나중에 들어온 유저가 씬을 따라오지 못함
    public void LoadLevel(string sceneName)
    {
        if (!PhotonNetwork.InRoom || !PhotonNetwork.IsMasterClient)
        {
            Debug.LogWarning("[Network] 룸의 마스터 클라이언트만 씬을 전환할 수 있음");
            return;
        }
        State = NetworkState.LoadingScene;
        PhotonNetwork.LoadLevel(sceneName);
    }
    // PUN 내부의 LoadLevelIfSynced와 같은 조건으로, 룸 프로퍼티에 기록된 씬으로 이동해야 하는지 판단
    private bool NeedsSceneSync()
    {
        if (!PhotonNetwork.AutomaticallySyncScene || PhotonNetwork.IsMasterClient || PhotonNetwork.CurrentRoom == null) return false;
        if (!PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(CurrentSceneProperty, out var sceneId)) return false;
        var activeScene = SceneManager.GetActiveScene();
        return sceneId switch
        {
            int buildIndex => activeScene.buildIndex != buildIndex,
            string sceneName => activeScene.name != sceneName,
            _ => false
        };
    }
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (State == NetworkState.LoadingScene && PhotonNetwork.InRoom) State = NetworkState.InRoom;
    }
    #endregion

    private bool CanMatchmaking()
    {
        if (!PhotonNetwork.IsConnectedAndReady || PhotonNetwork.InRoom || IsBusy)
        {
            Debug.LogWarning("[Network] 매치메이킹을 할 수 없는 상태");
            return false;
        }
        return true;
    }

    #region IConnectionCallbacks
    public void OnConnected() { }
    public void OnConnectedToMaster()
    {
        Debug.Log("[Network] 마스터 서버 접속");
        OnConnectedToMasterEvent?.Invoke();
        // 로비 진입 요청에 실패했을 때만 대기 상태로 (중간에 Busy가 풀리는 것 방지)
        if (!JoinLobby()) State = NetworkState.ConnectedToMaster;
    }
    public void OnDisconnected(DisconnectCause cause)
    {
        Debug.LogWarning($"[Network] 연결 끊김 : {cause}");
        m_roomList.Clear();
        State = NetworkState.Offline;
        OnDisconnectedEvent?.Invoke(cause);
    }
    public void OnRegionListReceived(RegionHandler regionHandler) { }
    public void OnCustomAuthenticationResponse(Dictionary<string, object> data) { }
    public void OnCustomAuthenticationFailed(string debugMessage) { }
    #endregion

    #region ILobbyCallbacks
    public void OnJoinedLobby()
    {
        Debug.Log("[Network] 로비 접속");
        m_roomList.Clear();
        State = NetworkState.InLobby;
        OnJoinedLobbyEvent?.Invoke();
    }
    public void OnLeftLobby()
    {
        m_roomList.Clear();
        State = NetworkState.ConnectedToMaster;
    }
    public void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        UpdateRoomList(roomList);
        OnRoomListUpdateEvent?.Invoke(RoomList);
    }
    public void OnLobbyStatisticsUpdate(List<TypedLobbyInfo> lobbyStatistics) { }
    #endregion

    #region IMatchmakingCallbacks
    public void OnFriendListUpdate(List<FriendInfo> friendList) { }
    public void OnCreatedRoom()
    {
        Debug.Log($"[Network] 룸 생성 : {PhotonNetwork.CurrentRoom.Name}");
    }
    public void OnCreateRoomFailed(short returnCode, string message)
    {
        Debug.LogWarning($"[Network] 룸 생성 실패({returnCode}) : {message}");
        ReturnToLobby();
        OnCreateRoomFailedEvent?.Invoke(returnCode, message);
    }
    public void OnJoinedRoom()
    {
        Debug.Log($"[Network] 룸 참가 : {PhotonNetwork.CurrentRoom.Name}");
        // 룸에 들어가면 로비를 벗어나므로 캐시된 목록은 더 이상 갱신되지 않음
        m_roomList.Clear();
        // 이 콜백 직후 PUN이 마스터의 씬으로 로딩을 시작하므로, 로딩이 끝날 때까지 LoadingScene 유지
        State = NeedsSceneSync() ? NetworkState.LoadingScene : NetworkState.InRoom;
        OnJoinedRoomEvent?.Invoke();
    }
    public void OnJoinRoomFailed(short returnCode, string message)
    {
        Debug.LogWarning($"[Network] 룸 참가 실패({returnCode}) : {message}");
        ReturnToLobby();
        OnJoinRoomFailedEvent?.Invoke(returnCode, message);
    }
    public void OnJoinRandomFailed(short returnCode, string message)
    {
        Debug.LogWarning($"[Network] 랜덤 룸 참가 실패({returnCode}) : {message}");
        ReturnToLobby();
        OnJoinRoomFailedEvent?.Invoke(returnCode, message);
    }
    // 룸 퇴장 후 로비 복귀까지 LeavingRoom 상태를 유지하고, OnJoinedLobby에서 InLobby로 바뀜
    public void OnLeftRoom()
    {
        Debug.Log("[Network] 룸 퇴장");
        OnLeftRoomEvent?.Invoke();
    }
    #endregion

    #region IInRoomCallbacks
    public void OnPlayerEnteredRoom(Player newPlayer) { }
    public void OnPlayerLeftRoom(Player otherPlayer)
    {
        Debug.Log($"[Network] 플레이어 퇴장 : #{otherPlayer.ActorNumber} {otherPlayer.NickName}");
        OnPlayerLeftRoomEvent?.Invoke(otherPlayer);
    }
    public void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
    {
        // 룸 안에서 마스터가 씬을 바꾼 경우
        if (propertiesThatChanged.ContainsKey(CurrentSceneProperty) && NeedsSceneSync()) State = NetworkState.LoadingScene;
    }
    public void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps) { }
    public void OnMasterClientSwitched(Player newMasterClient) { }
    #endregion

    public event Action<NetworkState> OnStateChangedEvent;
    public event Action OnConnectedToMasterEvent;
    public event Action<DisconnectCause> OnDisconnectedEvent;
    public event Action OnJoinedLobbyEvent;
    public event Action<IReadOnlyCollection<RoomInfo>> OnRoomListUpdateEvent;
    public event Action<short, string> OnCreateRoomFailedEvent;
    public event Action OnJoinedRoomEvent;
    public event Action<short, string> OnJoinRoomFailedEvent;
    public event Action OnLeftRoomEvent;
    public event Action<Player> OnPlayerLeftRoomEvent;

    // PhotonNetwork.CurrentSceneProperty(internal)와 같은 키
    private const string CurrentSceneProperty = "curScn";
    // 플레이어 커스텀 프로퍼티. UserProfile.UID
    public const string UidProperty = "uid";
    private bool m_initialized;
    private NetworkState m_state = NetworkState.Offline;
    private readonly Dictionary<string, RoomInfo> m_roomList = new();
}
