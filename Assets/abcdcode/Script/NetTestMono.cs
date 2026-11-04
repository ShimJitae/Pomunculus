using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

// NetworkManager 기능 확인용 테스트 UI (IMGUI)
public class NetTestMono : MonoBehaviour
{
    private void Awake()
    {
        m_roomName = $"Room{Random.Range(1000, 10000)}";
    }
    // GameManager.Awake에서 유저 프로파일이 준비된 뒤 접속해야 하므로 Start에서 Init
    private void Start()
    {
        var network = NetworkManager.Instance;
        network.OnCreateRoomFailedEvent += OnRoomFailed;
        network.OnJoinRoomFailedEvent += OnRoomFailed;
        network.OnJoinedRoomEvent += OnJoinedRoom;
        network.OnDisconnectedEvent += OnDisconnected;
        if (network.State == NetworkState.Offline) network.Init();
    }
    // NetworkManager는 씬 이동 후에도 남아 있으므로 반드시 구독 해제
    private void OnDestroy()
    {
        var network = NetworkManager.Instance;
        network.OnCreateRoomFailedEvent -= OnRoomFailed;
        network.OnJoinRoomFailedEvent -= OnRoomFailed;
        network.OnJoinedRoomEvent -= OnJoinedRoom;
        network.OnDisconnectedEvent -= OnDisconnected;
    }

    private void OnRoomFailed(short returnCode, string message) => m_message = $"실패({returnCode}) : {message}";
    private void OnJoinedRoom() => m_message = null;
    private void OnDisconnected(DisconnectCause cause) => m_message = $"연결 끊김 : {cause}";

    private void OnGUI()
    {
        float scale = Screen.height / ReferenceHeight;
        GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
        GUILayout.BeginArea(new Rect(10f, 10f, PanelWidth, ReferenceHeight - 20f), GUI.skin.box);
        DrawStatus();
        // 방에 들어가면 로비 UI(방 생성/목록/참가)는 숨기고 플레이어 목록을 표시
        if (PhotonNetwork.InRoom) DrawRoom();
        else DrawLobby();
        GUILayout.EndArea();
    }

    private void DrawStatus()
    {
        var network = NetworkManager.Instance;
        GUILayout.Label($"상태 : {network.State}{(network.IsBusy ? "  (로딩 중...)" : "")}");
        GUILayout.Label($"세부 상태 : {network.DetailState}");
        if (network.State == NetworkState.LoadingScene) GUILayout.Label($"씬 로딩 : {PhotonNetwork.LevelLoadingProgress:P0}");
        if (!string.IsNullOrEmpty(m_message)) GUILayout.Label(m_message);
        if (network.State == NetworkState.Offline && GUILayout.Button("접속")) network.Init();
        GUILayout.Space(10f);
    }

    private void DrawLobby()
    {
        var network = NetworkManager.Instance;
        GUI.enabled = network.State == NetworkState.InLobby;

        GUILayout.Label("── 방 생성 ──");
        GUILayout.BeginHorizontal();
        GUILayout.Label("이름", GUILayout.Width(40f));
        m_roomName = GUILayout.TextField(m_roomName);
        GUILayout.Label("최대", GUILayout.Width(40f));
        m_maxPlayers = GUILayout.TextField(m_maxPlayers, GUILayout.Width(40f));
        GUILayout.EndHorizontal();
        if (GUILayout.Button("방 생성"))
        {
            m_message = null;
            network.CreateRoom(m_roomName, int.TryParse(m_maxPlayers, out var maxPlayers) ? maxPlayers : 4);
        }
        GUILayout.Space(10f);

        GUILayout.Label("── 방 참가 ──");
        GUILayout.BeginHorizontal();
        m_joinRoomName = GUILayout.TextField(m_joinRoomName);
        if (GUILayout.Button("이름으로 참가", GUILayout.Width(110f)))
        {
            m_message = null;
            network.JoinRoom(m_joinRoomName);
        }
        GUILayout.EndHorizontal();
        if (GUILayout.Button("랜덤 참가"))
        {
            m_message = null;
            network.JoinRandomRoom();
        }
        GUILayout.Space(10f);

        GUILayout.Label($"── 방 목록 ({network.RoomList.Count}) ──");
        m_roomListScroll = GUILayout.BeginScrollView(m_roomListScroll, GUILayout.Height(250f));
        if (network.RoomList.Count == 0) GUILayout.Label("방이 없습니다");
        bool lobbyReady = GUI.enabled;
        foreach (var room in network.RoomList)
        {
            bool full = room.MaxPlayers != 0 && room.PlayerCount >= room.MaxPlayers;
            GUILayout.BeginHorizontal();
            GUILayout.Label($"{room.Name}  {room.PlayerCount}/{(room.MaxPlayers == 0 ? "-" : room.MaxPlayers.ToString())}{(room.IsOpen ? "" : "  [닫힘]")}");
            GUI.enabled = lobbyReady && room.IsOpen && !full;
            if (GUILayout.Button("참가", GUILayout.Width(60f)))
            {
                m_message = null;
                network.JoinRoom(room.Name);
            }
            GUI.enabled = lobbyReady;
            GUILayout.EndHorizontal();
        }
        GUILayout.EndScrollView();

        GUI.enabled = true;
    }

    private void DrawRoom()
    {
        var network = NetworkManager.Instance;
        var room = PhotonNetwork.CurrentRoom;
        GUILayout.Label($"── 방 : {room.Name} ({room.PlayerCount}/{(room.MaxPlayers == 0 ? "-" : room.MaxPlayers.ToString())}) ──");

        GUILayout.Label("플레이어 목록");
        m_playerListScroll = GUILayout.BeginScrollView(m_playerListScroll, GUILayout.Height(250f));
        foreach (var player in PhotonNetwork.PlayerList)
        {
            GUILayout.Label($"#{player.ActorNumber}  {player.NickName}{(player.IsMasterClient ? "  [방장]" : "")}{(player.IsLocal ? "  (나)" : "")}");
        }
        GUILayout.EndScrollView();

        GUI.enabled = network.State == NetworkState.InRoom;
        if (GUILayout.Button("방 나가기")) network.LeaveRoom();
        GUI.enabled = true;
    }

    private const float ReferenceHeight = 720f;
    private const float PanelWidth = 420f;
    private string m_roomName;
    private string m_maxPlayers = "4";
    private string m_joinRoomName = "";
    private string m_message;
    private Vector2 m_roomListScroll;
    private Vector2 m_playerListScroll;
}
