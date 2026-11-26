using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class StatusEffectTooltip : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text effectNameText;
    [SerializeField] private TMP_Text categoryText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text infoText;

    [Header("위치")]
    [SerializeField] private Vector2 mouseOffset = new Vector2(20, 20);  // 마우스와 툴팁 사이 간격
    [SerializeField] private float edgePadding = 15;                      // 화면 가장자리 여백

    private RectTransform tooltipRect;   // 자기 RectTransform (첫 Show 때 CacheReferences에서 캐싱)
    private RectTransform canvasRect;    // HUD Canvas의 RectTransform, 좌표 변환 기준
    private Canvas rootCanvas;           // GetComponentInParent<Canvas>().rootCanvas

    private StatusEffectIconView owner;  // 툴팁을 띄운 아이콘
    private int displayedSeconds = -1;   // 초가 바뀔 때만 InfoText 갱신
    private int displayedStackCount = -1;

    // 아이콘에 마우스를 올리면 해당 상태이상 정보로 툴팁을 채우고 띄운다
    public void Show(StatusEffectIconView owner, Sprite icon, string stateName, EffectCategory category, string description, Vector2 screenPosition)
    {
        gameObject.SetActive(true);   // 먼저 켜야 아래 Canvas 조회가 정상 동작
        CacheReferences();

        this.owner = owner;

        iconImage.sprite = icon;
        iconImage.enabled = icon != null;
        effectNameText.text = stateName;
        categoryText.text = GetCategoryName(category);
        categoryText.color = owner.BorderColor;
        descriptionText.text = description;

        // 이전 아이콘의 값이 남지 않도록 캐시를 초기화하고 바로 한 번 갱신
        displayedSeconds = -1;
        displayedStackCount = -1;
        RefreshInfo();

        // ContentSizeFitter 높이를 즉시 반영해야 첫 프레임 가장자리 계산이 맞는다
        LayoutRebuilder.ForceRebuildLayoutImmediate(tooltipRect);
        UpdatePosition(screenPosition);
    }

    // 처음 한 번만 RectTransform과 루트 Canvas를 캐싱
    private void CacheReferences()
    {
        if (tooltipRect != null) return;

        tooltipRect = (RectTransform)transform;
        rootCanvas = GetComponentInParent<Canvas>().rootCanvas;   // 중첩 Canvas라 rootCanvas 필수
        canvasRect = (RectTransform)rootCanvas.transform;
    }

    // 분류를 화면에 표시할 한글 이름으로 변환
    private string GetCategoryName(EffectCategory category)
    {
        switch (category)
        {
            case EffectCategory.Buff: return "버프";
            case EffectCategory.Debuff: return "디버프";
            case EffectCategory.Curse: return "저주";
            default: return string.Empty;
        }
    }

    // 남은 시간·스택이 바뀌었을 때만 InfoText 갱신
    private void RefreshInfo()
    {
        int seconds = Mathf.CeilToInt(owner.RemainingDuration);
        int stackCount = owner.StackCount;
        if (seconds == displayedSeconds && stackCount == displayedStackCount) return;

        displayedSeconds = seconds;
        displayedStackCount = stackCount;
        infoText.SetText("중첩 {0} · 남은 시간 {1}초", stackCount, seconds);
    }

    private void UpdatePosition(Vector2 screenPosition)
    {
        Camera camera = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : rootCanvas.worldCamera;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPosition, camera, out Vector2 mousePosition))
            return;

        Rect bounds = canvasRect.rect;

        float width = tooltipRect.rect.width * tooltipRect.localScale.x;
        float height = tooltipRect.rect.height * tooltipRect.localScale.y;

        float x = mousePosition.x + mouseOffset.x;
        float y = mousePosition.y - mouseOffset.y;

        // 오른쪽 공간이 부족하면 마우스 왼쪽으로 이동합니다.
        if (x + width > bounds.xMax - edgePadding)
            x = mousePosition.x - mouseOffset.x - width;

        // 아래쪽 공간이 부족하면 마우스 위쪽으로 이동합니다.
        if (y - height < bounds.yMin + edgePadding)
            y = mousePosition.y + mouseOffset.y + height;

        float minX = bounds.xMin + edgePadding;
        float maxX = bounds.xMax - edgePadding - width;
        float minY = bounds.yMin + edgePadding + height;
        float maxY = bounds.yMax - edgePadding;

        x = maxX >= minX ? Mathf.Clamp(x, minX, maxX) : minX;
        y = maxY >= minY ? Mathf.Clamp(y, minY, maxY) : maxY;

        tooltipRect.localPosition = new Vector3(x, y, 0);
    }

    // 매 프레임 정보 갱신 + 마우스 따라가기. 아이콘이 파괴되면 스스로 닫힌다
    private void LateUpdate()
    {
        if (owner == null)
        {
            Hide();
            return;
        }

        RefreshInfo();

        if (Mouse.current != null)
            UpdatePosition(Mouse.current.position.ReadValue());
    }

    // 자신을 띄운 아이콘이 요청할 때만 닫는다 (다른 아이콘 hover 중 깜빡임 방지)
    public void HideFor(StatusEffectIconView requester)
    {
        if (owner == requester)
            Hide();
    }

    // 툴팁을 끄고 owner 참조를 끊는다
    public void Hide()
    {
        owner = null;
        gameObject.SetActive(false);
    }
}
