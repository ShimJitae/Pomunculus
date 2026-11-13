using Pomunculus.Items;
using System;

// 포션 장착 슬롯 Model ↔ PotionSlotView 연결 (뼈대).
public class PotionSlotPresenter : IDisposable
{
    private readonly Inventory inventory;

    // Model
    private readonly PotionLoadout model;

    // View
    private readonly IPotionSlotView view;

    public PotionSlotPresenter(PotionLoadout model, Inventory inventory, IPotionSlotView view)
    {
        this.model = model;
        this.inventory = inventory;
        this.view = view;

        model.OnSelectChanged += view.SetSelectedSlot;

        model.OnEquipChanged += HandleEquipChanged;
        inventory.Changed += RefreshAllSlots;

        model.OnEquipModeChanged += view.SetEquipButtonsVisible;
        view.OnEquipButtonClicked += HandleEquipButtonClicked;
        // 첫 갱신
        RefreshAllSlots();
        view.SetEquipButtonsVisible(model.IsEquipping);
        view.SetSelectedSlot(model.SelectedIndex);
    }

    // 슬롯 하나의 아이콘과 수량을 Model/Inventory 기준으로 다시 그린다
    private void RefreshSlot(int slotIndex)
    {
        PotionData potion = model.GetSlot(slotIndex);
        int amount = potion != null ? inventory.GetItemCount(potion.Id) : 0;

        view.SetSlotIcon(slotIndex, potion);
        view.SetSlotCount(slotIndex, amount, potion != null);
    }

    // 모든 슬롯을 다시 그린다
    private void RefreshAllSlots()
    {
        for (int i = 0; i < PotionLoadout.SlotCount; i++)
            RefreshSlot(i);
    }

    // 장착이 바뀐 슬롯만 다시 그린다 (이벤트 시그니처 (int, PotionData)에 맞추기 위한 래퍼)
    private void HandleEquipChanged(int slotIndex, PotionData potion)
    {
        RefreshSlot(slotIndex);
    }

    // "+" 클릭 시 대기 중인 포션을 해당 슬롯에 장착
    private void HandleEquipButtonClicked(int slotIndex)
    {
        model.EquipToSlot(slotIndex);
    }

    // 구독 해제 (Model 연결 후 이벤트 해제 코드를 넣는다)
    public void Dispose()
    {
        model.OnEquipChanged -= HandleEquipChanged;
        model.OnSelectChanged -= view.SetSelectedSlot;
        model.OnEquipModeChanged -= view.SetEquipButtonsVisible;
        inventory.Changed -= RefreshAllSlots;
        view.OnEquipButtonClicked -= HandleEquipButtonClicked;
    }
}
