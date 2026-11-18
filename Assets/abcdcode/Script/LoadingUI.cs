using Photon.Pun;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 씬 로딩 화면. 씬 이동은 LoadingUI.LoadScene으로 비동기 로딩하고, 로딩 중에만 화면을 켜서 진행도를 표시
// PUN의 PhotonNetwork.LoadLevel(방장의 씬 전환, 참가 시 자동 씬 동기화)도 감지해서 같은 방식으로 표시
// 이 컴포넌트가 붙은 오브젝트는 항상 켜두고(Update와 Instance 검색을 위해) 자식 오브젝트만 켜고 끔
public class LoadingUI : SingletonBehavior<LoadingUI>
{
    // 비동기로 씬 이동. LoadingUI가 없는 씬(에디터에서 월드 씬부터 시작 등)에서도 로딩 화면 없이 이동은 됨
    // 이미 로딩 중이면 무시 (OnLeftRoom 등에서 같은 씬 이동이 중복 요청되는 경우)
    public static void LoadScene(string sceneName)
    {
        if (IsLoading)
        {
            Debug.LogWarning($"[LoadingUI] 이미 씬 로딩 중이라 {sceneName} 이동 요청 무시");
            return;
        }
        s_operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
    }
    // 직접 요청한 로딩이나 PUN의 LoadLevel 로딩이 진행 중인지
    public static bool IsLoading => (s_operation != null && !s_operation.isDone) || IsPhotonLoading;
    // PUN은 LoadLevel 동안 메시지 큐를 멈추고 씬 로드가 끝나면 다시 켬
    private static bool IsPhotonLoading => !PhotonNetwork.IsMessageQueueRunning;

    public override void Awake()
    {
        base.Awake();
        if (Instance != this) return; // 씬을 옮기며 생긴 중복 LoadingUI는 파괴되므로 초기화하지 않음
        DontDestroyOnLoad(gameObject);
        SetVisible(false);
    }
    private void Update()
    {
        bool loading = IsLoading;
        if (loading != m_visible) SetVisible(loading);
        if (!loading) return;
        m_loadingBar.fillAmount = GetProgress();
    }

    // AsyncOperation.progress는 로드가 끝나면 0.9에서 멈추고 씬 활성화가 끝나야 1이 되므로 0.9를 가득 찬 것으로 봄
    private static float GetProgress()
    {
        float progress = s_operation != null && !s_operation.isDone ? s_operation.progress : PhotonNetwork.LevelLoadingProgress;
        return Mathf.Clamp01(progress / 0.9f);
    }
    private void SetVisible(bool visible)
    {
        m_visible = visible;
        if (visible) m_loadingBar.fillAmount = 0f;
        foreach (Transform child in transform) child.gameObject.SetActive(visible);
    }

    [SerializeField] private Image m_loadingBar;
    private bool m_visible = true; // 시작 시 SetVisible(false)가 확실히 적용되도록
    private static AsyncOperation s_operation;
}
