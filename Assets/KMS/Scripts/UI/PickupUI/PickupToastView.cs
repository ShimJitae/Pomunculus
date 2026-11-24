using DG.Tweening;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PickupToastView : MonoBehaviour
{
    [SerializeField] private LayoutElement slotLayout;      // 루트
    [SerializeField] private RectTransform body;
    [SerializeField] private CanvasGroup bodyGroup;
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text categoryText, itemNameText, amountText;
    [SerializeField] private RectTransform shine;
    [SerializeField] private Image shineImage;
    [SerializeField] private Color materialColor = new Color(0.37f, 0.43f, 0.15f);
    [SerializeField] private Color potionColor = new Color(0.54f, 0.25f, 0.49f);

    [Header("슬롯")]
    [SerializeField] private float slotHeight = 86;

    [Header("등장")]
    [SerializeField] private float enterOffsetX = 60f;      // 오른쪽에서 밀려 들어오기 시작하는 거리
    [SerializeField] private float expand = 0.15f;
    [SerializeField] private float enter = 0.22f;

    [Header("아이콘 튐")]
    [SerializeField] private float iconPopDelay = 0.06f;
    [SerializeField] private float iconPopDuration = 0.3f;
    [SerializeField] private float iconStartScale = 0.6f;

    [Header("반짝임")]
    [SerializeField] private Color shineColor = new Color(1f, 0.98f, 0.88f);
    [SerializeField] private float shineDelay = 0.13f;
    [SerializeField] private float shineDuration = 0.45f;
    [SerializeField] private float shineFadeInDuration = 0.18f;   // 나머지 시간 동안 다시 사라진다
    [SerializeField, Range(0f, 1f)] private float shinePeakAlpha = 0.85f;
    [SerializeField] private float shineEdgePadding = 40f;        // 양피지 양 끝 바깥에서 출발/도착하는 거리

    [Header("유지 / 퇴장")]
    [SerializeField] private float hold = 2.4f;
    [SerializeField] private float exitOffsetX = 40f;       // 오른쪽으로 빠져나가는 거리
    [SerializeField] private float exit = 0.3f;
    [SerializeField] private float collapse = 0.18f;

    public bool IsLeaving { get; private set; }
    private Sequence sequence;
    private event Action<PickupToastView> onFinished;

    // 내용을 채우고 등장 → 유지 → 퇴장 애니메이션을 시작한다
    public void Play(PickupToastData data, Action<PickupToastView> onFinished)
    {
        this.onFinished = onFinished;
        IsLeaving = false;
        sequence?.Kill();

        itemNameText.text = data.ItemName;
        amountText.text = $"×{data.Amount}";
        icon.sprite = data.Icon;
        icon.enabled = data.Icon != null;   // 아이콘이 없으면 흰 사각형이 뜨니까 숨김
        categoryText.text = data.Category == PickupCategory.Potion ? "포션" : "재료";
        categoryText.color = data.Category == PickupCategory.Potion ? potionColor : materialColor;

        slotLayout.preferredHeight = 0;
        body.anchoredPosition = new Vector2(enterOffsetX, 0);
        bodyGroup.alpha = 0f;
        icon.rectTransform.localScale = Vector3.one * iconStartScale;

        float travelWidth = ((RectTransform)shine.parent).rect.width;
        shine.anchoredPosition = new Vector2(-shineEdgePadding, 0f);   // 왼쪽 밖
        shineImage.color = new Color(shineColor.r, shineColor.g, shineColor.b, 0f);

        float shineFadeOutDuration = Mathf.Max(0f, shineDuration - shineFadeInDuration);

        sequence = DOTween.Sequence()
            .SetUpdate(true)
            .SetLink(gameObject)
            .Append(TweenSlotHeight(slotHeight, expand).SetEase(Ease.OutQuad))
            .Join(body.DOAnchorPosX(0, enter).SetEase(Ease.OutCubic))      // 0.00초 시작 (높이와 동시에)
            .Join(bodyGroup.DOFade(1, enter))     // 0.00초 시작 (역시 동시에)
            .Insert(iconPopDelay, icon.rectTransform.DOScale(1f, iconPopDuration).SetEase(Ease.OutBack))
            .Insert(shineDelay, shine.DOAnchorPosX(travelWidth + shineEdgePadding, shineDuration).SetEase(Ease.Linear))
            .Insert(shineDelay, shineImage.DOFade(shinePeakAlpha, shineFadeInDuration))
            .Insert(shineDelay + shineFadeInDuration, shineImage.DOFade(0f, shineFadeOutDuration))
            .InsertCallback(enter + hold, Dismiss);
    }

    // 남은 유지 시간을 건너뛰고 퇴장(슬라이드 아웃 + 페이드) 후 슬롯을 접는다
    public void Dismiss()
    {
        if (IsLeaving) return;
        IsLeaving = true;

        sequence?.Kill();
        sequence = DOTween.Sequence()
            .SetUpdate(true)
            .SetLink(gameObject)
            .Append(body.DOAnchorPosX(exitOffsetX, exit).SetEase(Ease.InQuad))
            .Join(bodyGroup.DOFade(0f, exit))
            .Append(TweenSlotHeight(0f, collapse).SetEase(Ease.InOutQuad))
            .OnComplete(() => onFinished?.Invoke(this));
    }

    // 풀에 돌려보내기 전에 진행 중인 트윈과 상태를 정리한다
    public void ResetForPool()
    {
        sequence?.Kill();
        sequence = null;
        IsLeaving = false;
        onFinished = null;
    }

    // 비활성화되면 남은 트윈을 멈춘다
    private void OnDisable()
    {
        sequence?.Kill();
    }

    // 슬롯 높이를 목표값까지 바꾸는 트윈을 만든다
    private Tween TweenSlotHeight(float target, float duration)
    {
        return DOTween.To(() => slotLayout.preferredHeight, h => slotLayout.preferredHeight = h, target, duration);
    }
}
