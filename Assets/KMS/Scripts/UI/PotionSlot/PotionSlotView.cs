using Pomunculus.Items;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PotionSlotView : MonoBehaviour, IPotionSlotView
{
    [Header("포션 그림")]
    [SerializeField] private Image[] potionIcon = new Image[PotionLoadout.SlotCount];

    [SerializeField] private PotionSlotIconClickHandler[] iconClickHandlers = new PotionSlotIconClickHandler[PotionLoadout.SlotCount];


    [Header("남은 수량")]
    [SerializeField] private GameObject[] countBadges = new GameObject[PotionLoadout.SlotCount];
    [SerializeField] private TMP_Text[] amountTexts = new TMP_Text[PotionLoadout.SlotCount];
    [Header("선택 프레임")]
    [SerializeField] private Image selectedFrame;
    [SerializeField] private Button[] equipButtons = new Button[PotionLoadout.SlotCount];

    private readonly bool[] slotHasPotion = new bool[PotionLoadout.SlotCount];
    private bool isEquipMode;

    public event Action<int> OnEquipButtonClicked;
    public event Action<int> OnSlotRightClicked;

    private void Awake()
    {
        for (int i = 0; i < equipButtons.Length; i++)
        {
            int index = i;
            equipButtons[i].onClick.AddListener(() => OnEquipButtonClicked?.Invoke(index));
        }

        for (int i = 0; i < iconClickHandlers.Length; i++)
        {
            if (iconClickHandlers[i] != null)
                iconClickHandlers[i].Initialize(i, HandleIconRightClicked);
        }
    }

    public void SetSlotIcon(int slotIndex, PotionData data)
    {
        if (slotIndex < 0 || slotIndex >= potionIcon.Length)
            return;
        Image image = potionIcon[slotIndex];
        if (image == null)
        {
            return;
        }
            
        Sprite icon = data != null ? data.Icon : null;
        image.sprite = icon;
        image.enabled = icon != null;
    }

    public void SetSlotCount(int slotIndex, int amount, bool visible)
    {
        if (slotIndex < 0 || slotIndex >= amountTexts.Length)
            return;

        slotHasPotion[slotIndex] = visible;

        TMP_Text text = amountTexts[slotIndex];
        if (text != null)
        {
            text.text = visible ? amount.ToString() : "";
            text.enabled = visible;
        }

        UpdateCountBadge(slotIndex);
    }


    // 포션이 장착돼 있고 장착 모드가 아닐 때만 수량 뱃지를 보여준다
    private void UpdateCountBadge(int slotIndex)
    {
        GameObject badge = countBadges[slotIndex];
        if (badge == null)
            return;

        badge.SetActive(slotHasPotion[slotIndex] && !isEquipMode);
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
        frame.SetAsFirstSibling(); // 아이콘 아래에 그리기
    }

    public void SetEquipButtonsVisible(bool visible)
    {
        isEquipMode = visible;

        for (int i = 0; i < equipButtons.Length; i++)
        {
            equipButtons[i].gameObject.SetActive(visible);
            UpdateCountBadge(i);
        }
    }

    public void SetUnequipInteractable(bool isOn)
    {
        for (int i = 0; i < potionIcon.Length; i++)
        {
            potionIcon[i].raycastTarget = isOn;
        }
    }

    // 아이콘 우클릭을 Presenter로 전달한다
    private void HandleIconRightClicked(int slotIndex)
    {
        OnSlotRightClicked?.Invoke(slotIndex);
    }
}