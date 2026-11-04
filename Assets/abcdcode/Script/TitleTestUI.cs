using UnityEngine;
using UnityEngine.SceneManagement;

// 타이틀 테스트 UI (IMGUI)
// 게임 시작 : 접속 없이 TestScene으로 이동 (오프라인)
// 멀티 플레이 시작 : 같은 오브젝트의 NetTestMono를 켜서 로비 접속. 방 생성은 NetTestMono에서 막아둠
public class TitleTestUI : MonoBehaviour
{
    private void Awake()
    {
        m_netTest = GetComponent<NetTestMono>();
        if (m_netTest != null) m_netTest.enabled = false;
    }
    private void Start()
    {
        NetworkManager.Instance.OnJoinedRoomEvent += OnJoinedRoom;
    }
    private void OnDestroy()
    {
        NetworkManager.Instance.OnJoinedRoomEvent -= OnJoinedRoom;
    }

    // 보통은 방장이 방을 만들 때 씬이 방 속성에 기록되어 PUN이 TestScene으로 자동 이동시킴
    // 기록이 없는 방이면 직접 이동
    private void OnJoinedRoom()
    {
        if (NetworkManager.Instance.State == NetworkState.InRoom && SceneManager.GetActiveScene().name != GameManager.TestSceneName)
            SceneManager.LoadScene(GameManager.TestSceneName);
    }

    private void OnGUI()
    {
        if (m_netTest != null && m_netTest.enabled) return; // 멀티 시작 후에는 NetTestMono UI만 표시

        float scale = Screen.height / ReferenceHeight;
        GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
        float width = Screen.width / scale;
        GUILayout.BeginArea(new Rect((width - PanelWidth) * 0.5f, 250f, PanelWidth, 200f), GUI.skin.box);
        GUILayout.Label("── 타이틀 ──");
        if (GUILayout.Button("게임 시작 (싱글)", GUILayout.Height(50f)))
        {
            SceneManager.LoadScene(GameManager.TestSceneName);
        }
        GUI.enabled = m_netTest != null;
        if (GUILayout.Button("멀티 플레이 시작", GUILayout.Height(50f)))
        {
            m_netTest.enabled = true; // NetTestMono.Start에서 접속 시작
        }
        GUI.enabled = true;
        GUILayout.EndArea();
    }

    private const float ReferenceHeight = 720f;
    private const float PanelWidth = 300f;
    private NetTestMono m_netTest;
}
