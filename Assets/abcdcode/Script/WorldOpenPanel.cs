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
        //if (string.IsNullOrWhiteSpace(m_roomNameInput.text)) m_roomNameInput.text = $"Room{Random.Range(1000, 10000)}";

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
        bool inRoom = PhotonNetwork.InRoom;
        m_isPrivateToggle.interactable = !busy && !inRoom;
        m_openRoomButton.interactable = !busy && !inRoom;
        // 방을 연 뒤에는 친구에게 알려줄 룸 코드를 표시
        string code = inRoom ? PhotonNetwork.CurrentRoom.Name : "";
        if (m_roomNameField != null && m_roomNameField.text != code) m_roomNameField.text = code;
        if (m_messageText != null && m_messageText.text != (m_message ?? "")) m_messageText.text = m_message ?? "";
    }
    #endregion
    private void OnLeftRoom() => LoadingUI.LoadScene(GameManager.TitleSceneName);
    #region 멀티 전환
    public void OnClickOpenRoom()
    {
        if (PhotonNetwork.InRoom) return;
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
    // 룸 이름은 랜덤 룸 코드. 비공개방(IsVisible = false)은 로비 목록에 안 뜨고 룸 코드로만 참가 가능
    // 검색 목록에는 룸 코드 대신 커스텀 프로퍼티로 넘긴 월드 이름을 표시
    private void CreateRoom()
    {
        m_pendingCreateRoom = false;
        bool isPrivate = m_isPrivateToggle.isOn; // 접속을 거쳐 늦게 호출되어도 토글 값을 따르도록 여기서 읽음
        var props = new ExitGames.Client.Photon.Hashtable { [NetworkManager.WorldNameProperty] = GetCurrentWorldName() };
        if (!NetworkManager.Instance.CreateRoom(GenerateRoomCode(), MaxPlayers, !isPrivate, props)) m_message = "방 생성 요청 실패";
    }
    private static string GetCurrentWorldName()
    {
        int slotId = GameManager.Instance.SelectedWorldSlot;
        var slot = WorldSlotLoader.Instance.SaveInfo.Slots.Find(s => s.SlotId == slotId);
        return string.IsNullOrWhiteSpace(slot?.WorldName) ? "World" : slot.WorldName;
    }
    // 헷갈리는 문자(0/O, 1/I/L)를 뺀 대문자+숫자 조합. 32^6 가지라 충돌 가능성은 매우 낮음
    private static string GenerateRoomCode()
    {
        const string chars = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";
        var bytes = System.Guid.NewGuid().ToByteArray();
        var code = new char[RoomCodeLength];
        for (int i = 0; i < RoomCodeLength; i++) code[i] = chars[bytes[i] % chars.Length];
        return new string(code);
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
    [SerializeField] private Toggle m_isPrivateToggle;
    //[SerializeField] private TMP_InputField m_roomNameInput;
    [SerializeField] private TMP_InputField m_roomNameField; // 방을 연 뒤 룸 코드 표시. 없어도 됨
    [SerializeField] private Button m_openRoomButton; // 접속 중이거나 이미 방 안이면 비활성화
    [SerializeField] private TMP_Text m_messageText; // 실패 메시지 표시용. 없어도 됨

    private const int MaxPlayers = 4;
    private const int RoomCodeLength = 6;

    private string m_message;
    private bool m_pendingCreateRoom;
}
