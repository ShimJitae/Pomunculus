using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 사망 시 뜨는 부활 창. 카운트다운이 끝나거나 버튼을 누르면 부활을 요청한다. Model(PlayerFSM)은 RespawnUIPresenter가 연결한다.
public class RespawnUIView : MonoBehaviour, IRespawnUIView
{
    [SerializeField] private GameObject root;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TMP_Text countdownText;
    [SerializeField] private Button respawnButton;
    [SerializeField, Min(0f)] private float fadeDuration = 0.3f;

    public event Action OnRespawnRequested;

    private bool isCounting;
    private bool hasRequested;
    private float remainingTime;
    private float fadeElapsed;
    private int shownSeconds = -1;

    // 버튼 클릭을 부활 요청으로 연결한다
    private void Awake()
    {
        respawnButton.onClick.AddListener(RequestRespawn);
    }

    private void OnDestroy()
    {
        respawnButton.onClick.RemoveListener(RequestRespawn);
    }

    // 창을 열고 카운트다운을 시작한다
    public void Show(float respawnDelay)
    {
        remainingTime = Mathf.Max(0f, respawnDelay);
        isCounting = true;
        hasRequested = false;
        fadeElapsed = 0f;
        shownSeconds = -1;

        if (canvasGroup != null) canvasGroup.alpha = fadeDuration > 0f ? 0f : 1f;
        root.SetActive(true);
        RefreshCountdownText();
    }

    // 창을 닫고 카운트다운을 멈춘다
    public void Hide()
    {
        isCounting = false;
        root.SetActive(false);
    }

    // 페이드인과 카운트다운을 진행하고, 0초가 되면 부활을 한 번 요청한다
    private void Update()
    {
        if (!isCounting) return;

        if (canvasGroup != null && fadeElapsed < fadeDuration)
        {
            fadeElapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Clamp01(fadeElapsed / fadeDuration);
        }

        remainingTime -= Time.deltaTime;
        RefreshCountdownText();

        if (remainingTime <= 0f) RequestRespawn();
    }

    // 남은 시간을 올림한 초 단위로 표시한다 (같은 숫자면 갱신하지 않음)
    private void RefreshCountdownText()
    {
        int seconds = Mathf.Max(0, Mathf.CeilToInt(remainingTime));
        if (seconds == shownSeconds) return;

        shownSeconds = seconds;
        countdownText.text = seconds.ToString();
    }

    // 창이 열려 있는 동안 부활 요청을 한 번만 보낸다
    private void RequestRespawn()
    {
        if (!isCounting || hasRequested) return;

        hasRequested = true;
        OnRespawnRequested?.Invoke();
    }
}
