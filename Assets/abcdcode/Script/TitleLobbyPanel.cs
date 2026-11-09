using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 타이틀의 멀티 플레이 로비 패널. 기존 NetTestMono(IMGUI)의 방 참가 기능을 Canvas로 옮긴 것
// 씬에 꺼진 상태로 배치해두고 TitleTestUI가 Open으로 켬. 처음 배치는 메뉴 Tools/Title/Build Lobby Panel 사용
// 폰트(Magical Neverland)에 한글이 없으므로 표시 문구는 영어
public class TitleLobbyPanel : MonoBehaviour
{
    public void Open()
    {
        gameObject.SetActive(true); // 처음 켜질 때 Awake에서 초기화
        transform.SetAsLastSibling(); // 타이틀 버튼보다 위에 그려서 클릭을 막음
        m_message = null;
        var network = NetworkManager.Instance;
        if (network.State == NetworkState.Offline) network.Init();
        RefreshRoomList();
    }
    // 접속은 유지. 다시 열면 바로 로비 상태로 이어짐
    public void Close()
    {
        gameObject.SetActive(false);
    }

    #region Unity
    private void Awake()
    {
        m_roomButtonTemplate.gameObject.SetActive(false);
        m_joinButton.onClick.AddListener(OnClickJoinByName);
        m_joinRandomButton.onClick.AddListener(OnClickJoinRandom);
        m_backButton.onClick.AddListener(Close);

        var network = NetworkManager.Instance;
        network.OnCreateRoomFailedEvent += OnRoomFailed;
        network.OnJoinRoomFailedEvent += OnRoomFailed;
        network.OnJoinedRoomEvent += OnJoinedRoom;
        network.OnDisconnectedEvent += OnDisconnected;
        network.OnRoomListUpdateEvent += OnRoomListUpdate;
    }
    // NetworkManager는 씬 이동 후에도 남아 있으므로 반드시 구독 해제. 한 번도 켜지지 않았으면 Awake처럼 호출되지 않음
    private void OnDestroy()
    {
        var network = NetworkManager.Instance;
        network.OnCreateRoomFailedEvent -= OnRoomFailed;
        network.OnJoinRoomFailedEvent -= OnRoomFailed;
        network.OnJoinedRoomEvent -= OnJoinedRoom;
        network.OnDisconnectedEvent -= OnDisconnected;
        network.OnRoomListUpdateEvent -= OnRoomListUpdate;
    }
    private void Update()
    {
        var network = NetworkManager.Instance;
        m_lobbyGroup.interactable = network.State == NetworkState.InLobby;

        string status = $"{network.State}{(network.IsBusy ? " (Loading...)" : "")}";
        if (network.State == NetworkState.LoadingScene) status += $"  {PhotonNetwork.LevelLoadingProgress:P0}";
        if (PhotonNetwork.InRoom) status += $"\nRoom : {PhotonNetwork.CurrentRoom.Name}";
        if (m_statusText.text != status) m_statusText.text = status;
        if (m_messageText.text != (m_message ?? "")) m_messageText.text = m_message ?? "";
    }
    #endregion

    #region 네트워크 이벤트
    private void OnRoomFailed(short returnCode, string message) => m_message = $"Failed({returnCode}) : {message}";
    private void OnJoinedRoom() => m_message = null;
    private void OnDisconnected(DisconnectCause cause) => m_message = $"Disconnected : {cause}";
    private void OnRoomListUpdate(IReadOnlyCollection<RoomInfo> rooms)
    {
        if (gameObject.activeInHierarchy) RefreshRoomList();
    }
    #endregion

    #region 버튼 동작
    private void OnClickJoinByName()
    {
        if (string.IsNullOrWhiteSpace(m_roomNameInput.text)) return;
        m_message = null;
        NetworkManager.Instance.JoinRoom(m_roomNameInput.text.Trim());
    }
    private void OnClickJoinRandom()
    {
        m_message = null;
        NetworkManager.Instance.JoinRandomRoom();
    }
    private void OnClickJoinRoom(string roomName)
    {
        m_message = null;
        NetworkManager.Instance.JoinRoom(roomName);
    }
    #endregion

    private void RefreshRoomList()
    {
        foreach (var b in m_roomButtons) Destroy(b.gameObject);
        m_roomButtons.Clear();

        var rooms = NetworkManager.Instance.RoomList;
        m_roomListHeader.text = $"Rooms ({rooms.Count})";
        m_emptyText.gameObject.SetActive(rooms.Count == 0);
        foreach (var room in rooms)
        {
            bool full = room.MaxPlayers != 0 && room.PlayerCount >= room.MaxPlayers;
            string roomName = room.Name;
            var button = Instantiate(m_roomButtonTemplate, m_roomButtonTemplate.transform.parent);
            button.name = roomName;
            button.GetComponentInChildren<TMP_Text>().text =
                $"{room.Name}  {room.PlayerCount}/{(room.MaxPlayers == 0 ? "-" : room.MaxPlayers.ToString())}{(room.IsOpen ? "" : "  [Closed]")}";
            button.interactable = room.IsOpen && !full;
            button.onClick.AddListener(() => OnClickJoinRoom(roomName));
            button.gameObject.SetActive(true);
            m_roomButtons.Add(button);
        }
    }

    [SerializeField] private TMP_Text m_statusText;
    [SerializeField] private TMP_Text m_messageText;
    [SerializeField] private CanvasGroup m_lobbyGroup; // 로비에 있을 때만 조작 가능한 영역
    [SerializeField] private TMP_InputField m_roomNameInput;
    [SerializeField] private Button m_joinButton;
    [SerializeField] private Button m_joinRandomButton;
    [SerializeField] private TMP_Text m_roomListHeader;
    [SerializeField] private TMP_Text m_emptyText;
    [SerializeField] private Button m_roomButtonTemplate; // 방 목록 항목. 같은 부모(스크롤 Content) 아래에 복제됨
    [SerializeField] private Button m_backButton;

    private readonly List<Button> m_roomButtons = new();
    private string m_message;
}
