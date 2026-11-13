using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 타이틀 테스트 UI (Canvas)
// 게임 시작 : 접속 없이 TestScene으로 이동 (오프라인)
// 멀티 플레이 시작 : 로비 패널을 띄우고 로비 접속. 타이틀에서는 방 생성 없이 참가만 가능
public class TitleTestUI : MonoBehaviour
{
    private void Awake()
    {
        m_lobbyPanel.gameObject.SetActive(false);
        //m_startGameButton.onClick.AddListener(OnClickStartGame);
        m_findRoomButton.onClick.AddListener(OnClickFindRoom);

        // 저장된 월드가 없으면 이어하기 숨김
        m_continueGameButton.gameObject.SetActive(WorldManager.Instance.HasWorld(GameManager.TempWorldSlot));
    }
    private void Start()
    {
        NetworkManager.Instance.OnJoinedRoomEvent += OnJoinedRoom;
    }
    private void OnDestroy()
    {
        NetworkManager.Instance.OnJoinedRoomEvent -= OnJoinedRoom;

        //SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    public void OnClickStartGame()
    {
        SceneManager.LoadScene(GameManager.TestSceneName);
    }
    public void OnClickNewGame()
    {
        // 기존 월드를 지우면 StartWorldOffline -> LoadWorld에서 새 월드로 시작됨
        WorldManager.Instance.DeleteWorld(GameManager.TempWorldSlot);
        SceneManager.LoadScene(GameManager.TestSceneName);
    }
    
    private void OnClickFindRoom()
    {
        m_lobbyPanel.Open(); // 오프라인이면 여기서 접속 시작
    }

    // 보통은 방장이 방을 만들 때 씬이 방 속성에 기록되어 PUN이 TestScene으로 자동 이동시킴
    // 기록이 없는 방이면 직접 이동
    private void OnJoinedRoom()
    {
        if (NetworkManager.Instance.State == NetworkState.InRoom && SceneManager.GetActiveScene().name != GameManager.TestSceneName)
            SceneManager.LoadScene(GameManager.TestSceneName);
    }

    [SerializeField] private Button m_startGameButton;
    [SerializeField] private Button m_continueGameButton;
    [SerializeField] private Button m_findRoomButton;
    [SerializeField] private TitleLobbyPanel m_lobbyPanel;
}
