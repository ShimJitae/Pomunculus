using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.SceneManagement;

// TestScene 테스트 UI (IMGUI)
// 오프라인 : 멀티로 전환(접속 -> 로비 -> 방 생성. 월드는 그대로 유지), 타이틀로
// 온라인 : 방 정보, 플레이어 목록, 방 나가기(-> 타이틀)
public class WorldTestUI : MonoBehaviour
{
    private void Awake()
    {
        m_roomName = $"Room{Random.Range(1000, 10000)}";
    }
    private void Start()
    {
        var network = NetworkManager.Instance;
        network.OnJoinedLobbyEvent += OnJoinedLobby;
        network.OnLeftRoomEvent += OnLeftRoom;
        network.OnCreateRoomFailedEvent += OnRoomFailed;
        network.OnDisconnectedEvent += OnDisconnected;
        // 방을 통해 들어온 경우는 GameManager가 방 입장 흐름으로 처리
        if (!PhotonNetwork.InRoom) GameManager.Instance.StartWorldOffline();
    }
    private void OnDestroy()
    {
        var network = NetworkManager.Instance;
        network.OnJoinedLobbyEvent -= OnJoinedLobby;
        network.OnLeftRoomEvent -= OnLeftRoom;
        network.OnCreateRoomFailedEvent -= OnRoomFailed;
        network.OnDisconnectedEvent -= OnDisconnected;
    }

    #region 멀티 전환
    private void StartMulti()
    {
        m_message = null;
        m_pendingCreateRoom = true;
        var network = NetworkManager.Instance;
        if (network.State == NetworkState.Offline) network.Init(); // 마스터 서버 접속 후 자동으로 로비 입장
        else if (network.State == NetworkState.InLobby) CreateRoom();
    }
    private void OnJoinedLobby()
    {
        if (m_pendingCreateRoom) CreateRoom();
    }
    private void CreateRoom()
    {
        m_pendingCreateRoom = false;
        if (!NetworkManager.Instance.CreateRoom(m_roomName)) m_message = "방 생성 요청 실패";
    }
    private void OnRoomFailed(short returnCode, string message) => m_message = $"방 생성 실패({returnCode}) : {message}";
    private void OnDisconnected(DisconnectCause cause)
    {
        m_pendingCreateRoom = false;
        m_message = $"연결 끊김 : {cause}";
    }
    #endregion

    // 월드 정리는 GameManager.OnLeftRoom에서 처리
    private void OnLeftRoom() => SceneManager.LoadScene(GameManager.TitleSceneName);
    private void GoTitle()
    {
        WorldManager.Instance.Release();
        SceneManager.LoadScene(GameManager.TitleSceneName);
    }

    private void OnGUI()
    {
        float scale = Screen.height / ReferenceHeight;
        GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
        GUILayout.BeginArea(new Rect(10f, 10f, PanelWidth, ReferenceHeight - 20f), GUI.skin.box);

        var network = NetworkManager.Instance;
        GUILayout.Label($"상태 : {network.State}{(network.IsBusy ? "  (로딩 중...)" : "")}");
        GUILayout.Label($"월드 오브젝트 : {WorldManager.Instance.Objects.GetList().Count}개  (준비 : {WorldManager.Instance.IsReady})");
        if (!string.IsNullOrEmpty(m_message)) GUILayout.Label(m_message);
        GUILayout.Space(10f);

        if (PhotonNetwork.InRoom) DrawOnline();
        else DrawOffline();

        GUILayout.EndArea();
    }
    private void DrawOffline()
    {
        var network = NetworkManager.Instance;
        bool busy = m_pendingCreateRoom || network.IsBusy;

        GUI.enabled = !busy;
        GUILayout.Label("── 멀티로 전환 ──");
        GUILayout.BeginHorizontal();
        GUILayout.Label("방 이름", GUILayout.Width(60f));
        m_roomName = GUILayout.TextField(m_roomName);
        GUILayout.EndHorizontal();
        if (GUILayout.Button(busy ? "접속 중..." : "방 만들기")) StartMulti();
        GUILayout.Space(10f);

        if (GUILayout.Button("타이틀로")) GoTitle();
        GUI.enabled = true;
    }
    private void DrawOnline()
    {
        var network = NetworkManager.Instance;
        var room = PhotonNetwork.CurrentRoom;
        GUILayout.Label($"── 방 : {room.Name} ({room.PlayerCount}/{(room.MaxPlayers == 0 ? "-" : room.MaxPlayers.ToString())}) ──");
        foreach (var player in PhotonNetwork.PlayerList)
            GUILayout.Label($"#{player.ActorNumber}  {player.NickName}{(player.IsMasterClient ? "  [방장]" : "")}{(player.IsLocal ? "  (나)" : "")}");
        GUILayout.Space(10f);

        GUI.enabled = network.State == NetworkState.InRoom;
        if (GUILayout.Button("방 나가기 (타이틀로)")) network.LeaveRoom();
        GUI.enabled = true;
    }

    private const float ReferenceHeight = 720f;
    private const float PanelWidth = 360f;
    private string m_roomName;
    private string m_message;
    private bool m_pendingCreateRoom;
}
