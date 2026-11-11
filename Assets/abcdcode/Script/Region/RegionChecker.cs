using System.Collections;
using Photon.Pun;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 내 플레이어의 지역이 바뀌면 화면 정중앙에 지역 이름을 잠깐 띄운다
// 씬 아무 오브젝트에 붙이면 되고, m_text를 비워두면 표시용 캔버스를 직접 만든다
// 임시로 화산 지역 화상 처리도 여기서 한다
public class RegionChecker : MonoBehaviour
{
    private void Awake()
    {
        if (m_text == null) CreateDefaultUI();
        if (m_canvasGroup == null) m_canvasGroup = m_text.GetComponentInParent<CanvasGroup>();
        if (m_canvasGroup == null) m_canvasGroup = m_text.gameObject.AddComponent<CanvasGroup>();
        m_canvasGroup.alpha = 0f;
        m_canvasGroup.blocksRaycasts = false;
    }

    private void Start()
    {
        WorldManager.Instance.OnSpawnedEvent += OnWorldObjSpawned;
        // 이미 생성되어 있는 내 플레이어가 있으면 바로 연결
        foreach (var obj in WorldManager.Instance.Objects.GetList()) OnWorldObjSpawned(obj);
    }

    private void OnDestroy()
    {
        if (WorldManager.Instance != null) WorldManager.Instance.OnSpawnedEvent -= OnWorldObjSpawned;
        Bind(null);
    }

    private void OnWorldObjSpawned(IWorldObj obj)
    {
        if (obj is not IPlayerObj p || p.Owner != PhotonNetwork.LocalPlayer.ActorNumber) return;
        // 보통은 GameManager가 먼저 붙여두지만, 순서가 바뀌어도 동작하도록 없으면 추가
        if (!obj.gameObject.TryGetComponent(out PlayerRegionTracker tracker))
            tracker = obj.gameObject.AddComponent<PlayerRegionTracker>();
        Bind(tracker);
    }

    private void Bind(PlayerRegionTracker tracker)
    {
        if (m_tracker == tracker) return;
        if (m_tracker != null) m_tracker.OnRegionChanged -= OnRegionChanged;
        m_tracker = tracker;
        m_statusEffects = null;
        if (m_tracker == null) return;
        m_statusEffects = m_tracker.GetComponent<StatusEffectController>();
        m_tracker.OnRegionChanged += OnRegionChanged;
        // 이미 어떤 지역 안에 있는 상태로 연결된 경우
        if (m_tracker.CurrentRegion != RegionType.None) Show(m_tracker.CurrentRegion);
    }

    private void OnRegionChanged(RegionType prev, RegionType next)
    {
        // 지역 밖으로 나간 경우(None)는 표시하지 않음
        if (next == RegionType.None) return;
        Show(next);
    }

    #region 지역 효과 (임시)
    // TODO 지역 효과가 정리되면 별도 클래스로 이동
    private void Update()
    {
        if (m_tracker == null || m_statusEffects == null) return;
        if (m_tracker.CurrentRegion == RegionType.Volcano) KeepBurn();
    }

    // 화산 안에 있는 동안 화상이 끊기지 않게 유지한다.
    // RegisterStatusEffect를 다시 부르면 중첩이 올라가므로, 이미 걸려 있으면 지속 시간만 채운다.
    // 화산을 나가면 남은 지속 시간만큼 화상이 이어지다 끝난다
    private void KeepBurn()
    {
        foreach (var effect in m_statusEffects.ActiveStatusEffects)
        {
            if (effect.Type != StatusEffectType.Burn) continue;
            effect.RemainingDuration = effect.Data.Duration;
            return;
        }
        m_statusEffects.RegisterStatusEffect(StatusEffectType.Burn);
    }
    #endregion

    private void Show(RegionType region)
    {
        m_text.text = GetDisplayName(region);
        if (m_routine != null) StopCoroutine(m_routine);
        m_routine = StartCoroutine(ShowRoutine());
    }

    private IEnumerator ShowRoutine()
    {
        yield return Fade(m_canvasGroup.alpha, 1f, m_fadeTime);
        yield return new WaitForSecondsRealtime(m_showTime);
        yield return Fade(1f, 0f, m_fadeTime);
        m_routine = null;
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
        {
            m_canvasGroup.alpha = Mathf.Lerp(from, to, t / duration);
            yield return null;
        }
        m_canvasGroup.alpha = to;
    }

    private static string GetDisplayName(RegionType region) => region switch
    {
        RegionType.Forest => "숲",
        RegionType.Volcano => "화산",
        RegionType.SnowyMountain => "설산",
        RegionType.Swamp => "독늪",
        _ => region.ToString(),
    };

    // 테스트용 기본 UI. 정식 UI가 생기면 m_text에 연결해서 사용
    private void CreateDefaultUI()
    {
        var canvasObj = new GameObject("RegionCheckerCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(CanvasGroup));
        canvasObj.transform.SetParent(transform, false);
        var canvas = canvasObj.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var scaler = canvasObj.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        m_canvasGroup = canvasObj.GetComponent<CanvasGroup>();

        var textObj = new GameObject("RegionName", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObj.transform.SetParent(canvasObj.transform, false);
        var rect = (RectTransform)textObj.transform;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(1200f, 200f);

        m_text = textObj.GetComponent<TextMeshProUGUI>();
        if (m_font != null) m_text.font = m_font;
        else Debug.LogWarning("[RegionChecker] m_font가 비어 있어 한글이 표시되지 않을 수 있음 (Maplestory Bold SDF 권장)", this);
        m_text.fontSize = 96f;
        m_text.alignment = TextAlignmentOptions.Center;
        m_text.raycastTarget = false;
    }

    [SerializeField] private TMP_Text m_text;
    [SerializeField] private CanvasGroup m_canvasGroup;
    [SerializeField] private TMP_FontAsset m_font;
    [SerializeField] private float m_fadeTime = 0.5f;
    [SerializeField] private float m_showTime = 2f;

    private PlayerRegionTracker m_tracker;
    private StatusEffectController m_statusEffects;
    private Coroutine m_routine;
}
