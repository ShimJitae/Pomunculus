using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 날짜·계절 시계 View. Model(DayClockModel)과는 DayUIPresenter가 연결한다.
// 월드 단위 UI라 플레이어 HUD 바인더 대신 스스로 Presenter를 만든다.
public class DayUI : MonoBehaviour, IDayUIView, IPointerEnterHandler, IPointerExitHandler
{
    private static readonly string[] seasonNames = { "봄", "여름", "가을", "겨울" }; // Season 순서

    [SerializeField]private Image m_clockUI;

    [Header("가운데 숫자판")]
    [SerializeField] private GameObject m_dayGroup;                // 평소: DAY + 생존 일 수
    [SerializeField] private TMP_Text m_dayCountText;
    [SerializeField] private GameObject m_seasonRemainGroup;       // 호버: 다음 계절까지 남은 일 수
    [SerializeField] private TMP_Text m_seasonRemainLabel;
    [SerializeField] private TMP_Text m_seasonRemainCountText;

    [Header("계절 원판")]
    [SerializeField] private RectTransform m_seasonMarkerPivot;    // z 회전으로 테두리 위 계절 마커 이동
    [SerializeField] private GameObject[] m_seasonDims;            // Season 순서(봄, 여름, 가을, 겨울)의 어둡게 덮는 오버레이
    [SerializeField] private Color[] m_seasonColors;               // Season 순서의 계절 색

    [Header("다음 계절 배너")]
    [SerializeField] private Image m_nextSeasonDot;
    [SerializeField] private TMP_Text m_nextSeasonText;

    [Header("호버")]
    [SerializeField] private GameObject m_hoverGlow;

    public event Action<bool> OnHoverChanged;

    private DayClockModel m_clockModel;
    private DayUIPresenter m_presenter;

    // WorldManager를 Model로 감싸 Presenter와 연결한다
    private void Start()
    {
        m_clockModel = new DayClockModel(WorldManager.Instance);
        m_presenter = new DayUIPresenter(m_clockModel, this);
    }

    // 시간은 이벤트 없이 흐르므로 매 프레임 Presenter를 갱신한다
    private void Update()
    {
        m_presenter?.Tick();
    }

    // Presenter와 Model의 구독을 해제한다
    private void OnDestroy()
    {
        m_presenter?.Dispose();
        m_clockModel?.Dispose();
    }

    // 시계(Frame)에 마우스가 올라가면 알린다
    public void OnPointerEnter(PointerEventData eventData) => OnHoverChanged?.Invoke(true);

    // 시계에서 마우스가 벗어나면 알린다
    public void OnPointerExit(PointerEventData eventData) => OnHoverChanged?.Invoke(false);

    // 가운데 숫자판에 생존 일 수를 표시한다
    public void SetDay(int dayNumber)
    {
        m_dayCountText.text = dayNumber.ToString();
    }

    // 하루 진행 링에 오늘 남은 시간 비율을 표시한다
    public void SetDayRemainRatio(float remainRatio)
    {
        m_clockUI.fillAmount = remainRatio;
    }

    // 1년 중 지난 비율만큼 계절 마커를 시계방향으로 돌린다
    public void SetYearProgress(float yearProgress)
    {
        m_seasonMarkerPivot.localEulerAngles = new Vector3(0f, 0f, -yearProgress * 360f);
    }

    // 현재 계절 칸만 밝게 두고 나머지는 어둡게 덮는다
    public void SetCurrentSeason(Season season)
    {
        for (int i = 0; i < m_seasonDims.Length; i++)
        {
            m_seasonDims[i].SetActive(i != (int)season);
        }
    }

    // 다음 계절 배너와 호버용 남은 일 수를 표시한다
    public void SetNextSeason(Season nextSeason, int daysUntilNextSeason)
    {
        string seasonName = seasonNames[(int)nextSeason];

        m_nextSeasonText.text = $"다음 계절 · {seasonName}";
        m_nextSeasonDot.color = m_seasonColors[(int)nextSeason];
        m_seasonRemainLabel.text = $"{seasonName}까지";
        m_seasonRemainCountText.text = $"{daysUntilNextSeason}일";
    }

    // 호버 중에는 가운데에 다음 계절까지 남은 일 수를 보여 주고 테두리를 빛나게 한다
    public void SetHover(bool isHovered)
    {
        m_dayGroup.SetActive(!isHovered);
        m_seasonRemainGroup.SetActive(isHovered);
        m_hoverGlow.SetActive(isHovered);
    }
}
