using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// ESC 메뉴의 방 열기 패널. WorldTestUI의 멀티 전환(접속 -> 로비 -> 방 생성. 월드는 그대로 유지)을 Canvas로 옮긴 것
// 씬에 꺼진 상태로 배치해두고 멀티플레이 버튼의 onClick에 Open, 방 열기 버튼에 OnClickOpenRoom, 닫기 버튼에 Close를 연결
public class WorldOpenPanel : MonoBehaviour
{
    // 접속 중이어도 패널만 닫음. 진행 중인 방 생성 요청은 그대로 진행됨
    public void Close()
    {
        gameObject.SetActive(false);
    }
    
    #region Unity
    private void Awake()
    {
        if (string.IsNullOrWhiteSpace(m_roomNameInput.text)) m_roomNameInput.text = $"Room{Random.Range(1000, 10000)}";

        var network = NetworkManager.Instance;
        network.OnJoinedLobbyEvent += OnJoinedLobby;
        network.OnJoinedRoomEvent += OnJoinedRoom;
        network.OnCreateRoomFailedEvent += OnRoomFailed;
        network.OnDisconnectedEvent += OnDisconnected;
        network.OnLeftRoomEvent += OnLeftRoom;
    }
    // NetworkManager는 씬 이동 후에도 남아 있으므로 반드시 구독 해제. 한 번도 켜지지 않았으면 Awake처럼 호출되지 않음
    private void OnDestroy()
    {
        var network = NetworkManager.Instance;
        network.OnJoinedLobbyEvent -= OnJoinedLobby;
        network.OnJoinedRoomEvent -= OnJoinedRoom;
        network.OnCreateRoomFailedEvent -= OnRoomFailed;
        network.OnDisconnectedEvent -= OnDisconnected;
        network.OnLeftRoomEvent -= OnLeftRoom;
    }
    private void Update()
    {
        bool busy = m_pendingCreateRoom || NetworkManager.Instance.IsBusy;
        m_roomNameInput.interactable = !busy;
        m_openRoomButton.interactable = !busy;
        if (m_messageText != null && m_messageText.text != (m_message ?? "")) m_messageText.text = m_message ?? "";
    }
    #endregion
    private void OnLeftRoom() => SceneManager.LoadScene(GameManager.TitleSceneName);
    #region 멀티 전환
    public void OnClickOpenRoom()
    {
        if (string.IsNullOrWhiteSpace(m_roomNameInput.text)) return;
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
        if (!NetworkManager.Instance.CreateRoom(m_roomNameInput.text.Trim())) m_message = "방 생성 요청 실패";
    }
    private void OnJoinedRoom()
    {
        m_message = null;
        Close();
    }
    private void OnRoomFailed(short returnCode, string message) => m_message = $"방 생성 실패({returnCode}) : {message}";
    private void OnDisconnected(DisconnectCause cause)
    {
        m_pendingCreateRoom = false;
        m_message = $"연결 끊김 : {cause}";
    }
    #endregion

    [SerializeField] private TMP_InputField m_roomNameInput;
    [SerializeField] private Button m_openRoomButton; // 접속 중에는 비활성화
    [SerializeField] private TMP_Text m_messageText; // 실패 메시지 표시용. 없어도 됨

    private string m_message;
    private bool m_pendingCreateRoom;
}
