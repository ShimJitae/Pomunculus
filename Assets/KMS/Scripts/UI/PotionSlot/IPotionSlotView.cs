using Pomunculus.Items;
using System;

// 포션 슬롯 View 인터페이스. 장착 슬롯 Model 확정 시 시그니처를 조정한다.
public interface IPotionSlotView
{
    public void SetSlotIcon(int slotIndex, PotionData potion);
    public void SetSelectedSlot(int slotIndex);
    public void SetSlotCount(int slotIndex, int amount, bool visible);
    public void SetEquipButtonsVisible(bool visible);

    public event Action<int> OnEquipButtonClicked;
}
