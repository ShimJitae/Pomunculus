using UnityEngine;

// 포션 슬롯 View 인터페이스. 장착 슬롯 Model 확정 시 시그니처를 조정한다.
public interface IPotionSlotView
{
    public void SetSlotIcon(int slotIndex, Sprite icon);
    public void SetSelectedSlot(int slotIndex);
}
