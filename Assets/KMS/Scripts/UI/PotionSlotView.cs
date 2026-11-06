using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PotionSlotView : MonoBehaviour, IPotionSlotView
{
    [Header("포션 그림")]
    [SerializeField] private Image[] potionIcon = new Image[3];

    [Header("남은 수량")]
    [SerializeField] private TMP_Text[] amountTexts = new TMP_Text[3];

    public void SetSlotIcon(int slotIndex, Sprite icon)
    {
        if (slotIndex < 0 || slotIndex >= potionIcon.Length)
            return;

        Image image = potionIcon[slotIndex];

        if (image == null)
            return;

        image.sprite = icon;
        image.preserveAspect = true;
        image.enabled = icon != null;
    }

    public void SetSlotCount(int slotIndex, int amount, bool visible)
    {
        if (slotIndex < 0 || slotIndex >= amountTexts.Length)
            return;

        TMP_Text text = amountTexts[slotIndex];

        if (text == null)
            return;

        text.text = visible ? amount.ToString() : "";
        text.enabled = visible;
    }

    public void SetSelectedSlot(int slotIndex)
    {
    }
}