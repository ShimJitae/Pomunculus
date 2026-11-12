using Pomunculus.Items;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PotionSlotView : MonoBehaviour, IPotionSlotView
{
    [Header("포션 그림")]
    [SerializeField] private Image[] potionIcon = new Image[PotionLoadout.SlotCount];
    [Header("남은 수량")]
    [SerializeField] private GameObject[] countBadges = new GameObject[PotionLoadout.SlotCount];
    [SerializeField] private TMP_Text[] amountTexts = new TMP_Text[PotionLoadout.SlotCount];
    [Header("선택 프레임")]
    [SerializeField] private Image selectedFrame;
    [SerializeField] private Button[] equipButtons = new Button[PotionLoadout.SlotCount];

    public event Action<int> OnEquipButtonClicked;

    private void Awake()
    {
        for(int i = 0; i < equipButtons.Length; i++)
        {
            int index = i;
            equipButtons[i].onClick.AddListener(() => OnEquipButtonClicked?.Invoke(index));
        }
        SetSelectedSlot(0);
    }

    public void SetSlotIcon(int slotIndex, PotionData data)
    {
        if (slotIndex < 0 || slotIndex >= potionIcon.Length)
            return;
        Image image = potionIcon[slotIndex];
        if (image == null)
            return;

        Sprite icon = data != null ? data.Icon : null;
        image.sprite = icon;
        image.enabled = icon != null;
    }

    public void SetSlotCount(int slotIndex, int amount, bool visible)
    {
        if (slotIndex < 0 || slotIndex >= amountTexts.Length)
            return;

        TMP_Text text = amountTexts[slotIndex];

        if (text == null)
            return;
        countBadges[slotIndex].SetActive(amount > 0);
        text.text = visible ? amount.ToString() : "";
        text.enabled = visible;
    }

    // 선택된 슬롯 위로 선택 프레임을 옮긴다
    public void SetSelectedSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= potionIcon.Length || selectedFrame == null)
            return;

        Image icon = potionIcon[slotIndex];
        if (icon == null)
            return;

        // 셀(PotionIcon_N)의 자식으로 붙여 레이아웃이 끝난 뒤에도 같이 움직이게 한다
        RectTransform frame = selectedFrame.rectTransform;
        frame.SetParent(icon.transform.parent, false);
        frame.anchorMin = frame.anchorMax = new Vector2(0.5f, 0.5f);
        frame.anchoredPosition = Vector2.zero;
        frame.SetAsLastSibling(); // 아이콘 위에 그리기 (아이콘 아래가 좋으면 SetAsFirstSibling)
    }

    public void SetEquipButtonsVisible(bool visible)
    {
        for(int i = 0; i < equipButtons.Length; i++)
        {
            equipButtons[i].gameObject.SetActive(visible);
        }
    }
}