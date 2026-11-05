using UnityEngine;
using UnityEngine.UI;

// 장착된 포션 슬롯을 그리는 View.
public class PotionSlotView : MonoBehaviour, IPotionSlotView
{
    [SerializeField] private Image[] potionIcon;

    // 포션 장착 시(인벤토리에서 슬롯으로) 슬롯 아이콘 갱신. icon이 null이면 빈 슬롯
    public void SetSlotIcon(int slotIndex, Sprite icon)
    {
        if (slotIndex < 0 || slotIndex >= potionIcon.Length) return;

        potionIcon[slotIndex].sprite = icon;
        potionIcon[slotIndex].enabled = icon != null;
    }

    // 포션 변경 시(마우스 휠 ~ 지정키) 선택 슬롯 표시
    public void SetSelectedSlot(int slotIndex)
    {
        // TODO: 선택 테두리/강조 표현이 정해지면 구현
    }
}
