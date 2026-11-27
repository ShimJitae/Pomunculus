using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class StatusEffectIconView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Image border;

    [Header("상태이상별 테두리 색")]
    [SerializeField] private Color buffColor = Color.green;
    [SerializeField] private Color debuffColor = Color.red;
    [SerializeField] private Color curseColor = new Color(0.6f, 0.2f, 0.8f);
    [SerializeField] private Color defaultColor = Color.white;


    [SerializeField] private Image iconImage;          // Icon
    [SerializeField] private GameObject stackBadge;    // StackBadge
    [SerializeField] private TMP_Text stackText;       // StackText
    [SerializeField] private Image durationOverlay;    // DurationOverlay (Image Type = Filled)
    [SerializeField] private TMP_Text durationText;    // DurationText

    private StatusEffectTooltip tooltip;
    private StatusEffectData data;      // 풀링되는 StatusEffect 대신 바뀌지 않는 SO만 보관

    private int stackCount;
    private float remainingDuration;
    private float totalDuration;
    private int displayedSeconds = -1;

    public Color BorderColor => border.color;
    public int StackCount => stackCount;
    public float RemainingDuration => remainingDuration;

    // 분류에 맞는 테두리 색 반환
    private Color GetBorderColor(EffectCategory category)
    {
        switch (category)
        {
            case EffectCategory.Buff: return buffColor;
            case EffectCategory.Debuff: return debuffColor;
            case EffectCategory.Curse: return curseColor;
            default: return defaultColor;
        }
    }

    // 아이콘 처음 생성 시 초기화
    public void Setup(StatusEffectTooltip tooltip, StatusEffectData data, int stackCount, float remainingDuration)
    {
        this.tooltip = tooltip;
        this.data = data;

        iconImage.sprite = data.Icon;
        iconImage.enabled = data.Icon != null;   // 아이콘이 비어 있으면 흰 사각형 방지
        border.color = GetBorderColor(data.EffectCategory);
        Refresh(stackCount, remainingDuration, data.Duration);
    }

    public void Refresh(int stackCount, float remainingDuration, float totalDuration)
    {
        this.stackCount = stackCount;
        this.remainingDuration = remainingDuration;
        this.totalDuration = totalDuration;
        displayedSeconds = -1;
        UpdateStackDisplay(stackCount);
    }

    // 남은 시간을 줄이고, 표시할 초가 바뀌었을 때만 UI 갱신
    private void Update()
    {
        remainingDuration = Mathf.Max(0f, remainingDuration - Time.deltaTime);
        UpdateOverlay();

        int seconds = Mathf.CeilToInt(remainingDuration);
        if (seconds == displayedSeconds) return;

        displayedSeconds = seconds;
        UpdateDurationDisplay();
    }

    private void UpdateStackDisplay(int stackCount)
    {
        stackBadge.SetActive(stackCount > 1);
        stackText.SetText("{0}", stackCount);
    }

    // 남은 초 텍스트 (정수)
    private void UpdateDurationDisplay()
    {
        durationText.SetText("{0}", displayedSeconds);
    }

    private void UpdateOverlay()
    {
        if (totalDuration <= 0f)
        {
            durationOverlay.fillAmount = 0f;
            return;
        }

        durationOverlay.fillAmount = 1f - remainingDuration / totalDuration;
    }

    // 아이콘에 마우스가 들어오면 툴팁을 띄운다
    public void OnPointerEnter(PointerEventData eventData)
    {
        tooltip.Show(this, data.Icon, data.StateName, data.EffectCategory, data.Description, eventData.position);
    }

    // 아이콘에서 마우스가 나가면 툴팁을 닫는다
    public void OnPointerExit(PointerEventData eventData)
    {
        tooltip.HideFor(this);
    }
}
