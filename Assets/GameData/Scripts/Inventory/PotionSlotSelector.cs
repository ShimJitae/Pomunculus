using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Pomunculus.Items
{
    // 인벤토리에서 고른 포션의 장착 대기를 PotionLoadout에 시작/취소하고, 선택 화면(어두운 패널)을 켜고 끈다.
    public class PotionSlotSelector : MonoBehaviour
    {
        // [MVP 삭제] Bind()로 주입받도록 변경
        // public Inventory inventory;
        private Inventory inventory;
        private PotionLoadout loadout;

        [Header("선택 화면")]
        [SerializeField] private GameObject selectPotion;
        [SerializeField] private ItemTooltipUI tooltip;

        // [MVP 삭제] 슬롯 표시는 HUD의 PotionSlotPresenter가 담당
        // [Header("포션 슬롯")]
        // [SerializeField] private PotionSlotView slotView;

        // [MVP 삭제] 장착/선택 데이터는 PotionLoadout(Model)이 담당
        // [Header("등록된 포션")]
        // [SerializeField] private PotionData[] registeredPotions = new PotionData[3];
        // public int ActiveSlotIndex { get; private set; } = -1;
        // private PotionData selectedPotion;
        // public PotionData selectedPotionData => GetRegisteredPotion(ActiveSlotIndex);

        // [MVP 삭제] 선택 상태는 PotionLoadout의 OnEquipModeChanged로 추적
        // public bool IsSelecting => selectedPotion != null;
        private bool isSelecting;
        public bool IsSelecting => isSelecting;

        // 로컬 플레이어의 인벤토리와 포션 장착 Model을 연결한다
        public void Bind(Inventory inventory, PotionLoadout loadout)
        {
            if (isActiveAndEnabled)
                UnsubscribeLoadout();

            this.inventory = inventory;
            this.loadout = loadout;

            if (isActiveAndEnabled)
                SubscribeLoadout();
        }

        private void OnEnable()
        {
            // [MVP 삭제] 수량 갱신은 PotionSlotPresenter가 담당
            // if (inventory != null)
            //     inventory.Changed += RefreshSlots;
            SubscribeLoadout();

            CancelSelection();
            // [MVP 삭제]
            // RefreshSlots();
            // SelectActiveSlot(ActiveSlotIndex);
        }

        // [MVP 삭제] 숫자키 슬롯 선택은 PlayerInputReader → PlayerFSM → PotionLoadout.SelectSlot이 담당
        // private void SelectActiveSlot(int slotIndex)
        // {
        //     ActiveSlotIndex = slotIndex;
        //     if (slotView != null)
        //         slotView.SetSelectedSlot(slotIndex);
        // }

        // 인벤토리에서 클릭한 포션으로 장착 대기를 시작한다
        public void BeginSelection(PotionData potion)
        {
            if (potion == null || inventory == null || loadout == null)
                return;

            if (inventory.GetItemCount(potion.Id) <= 0)
                return;

            // [MVP 삭제] 대기 포션은 PotionLoadout이 보관
            // selectedPotion = potion;

            if (tooltip != null)
                tooltip.Hide();

            // [MVP 삭제] 패널은 HandleEquipModeChanged에서 켠다
            // selectPotion.SetActive(true);
            loadout.BeginEquip(potion);
        }

        // [MVP 삭제] 장착은 HUD의 "+" 버튼 → PotionSlotPresenter → PotionLoadout.EquipToSlot이 담당
        // public void RegisterPotion(int slotIndex)
        // {
        //     if (slotIndex < 0 || slotIndex >= registeredPotions.Length)
        //         return;
        //
        //     if (!IsSelecting)
        //     {
        //         registeredPotions[slotIndex] = null;
        //         RefreshSlots();
        //         return;
        //     }
        //
        //     if (inventory == null || inventory.GetItemCount(selectedPotion.Id) <= 0)
        //     {
        //         CancelSelection();
        //         return;
        //     }
        //
        //     registeredPotions[slotIndex] = selectedPotion;
        //
        //     RefreshSlots();
        //     CancelSelection();
        // }

        // [MVP 삭제] 장착된 포션은 PotionLoadout.GetSlot으로 조회
        // public PotionData GetRegisteredPotion(int slotIndex)
        // {
        //     if (slotIndex < 0 || slotIndex >= registeredPotions.Length)
        //         return null;
        //
        //     return registeredPotions[slotIndex];
        // }

        // 장착 대기를 취소하고 선택 화면을 닫는다
        public void CancelSelection()
        {
            // [MVP 삭제]
            // selectedPotion = null;
            if (loadout != null)
                loadout.CancelEquip();

            HandleEquipModeChanged(false);
        }

        private void OnDisable()
        {
            // [MVP 삭제]
            // if (inventory != null)
            //     inventory.Changed -= RefreshSlots;

            CancelSelection();
            UnsubscribeLoadout();
        }

        // [MVP 삭제] 슬롯 아이콘/수량 표시는 PotionSlotPresenter가 담당
        // private void RefreshSlots()
        // {
        //     if (slotView == null)
        //         return;
        //
        //     for (int i = 0; i < registeredPotions.Length; i++)
        //     {
        //         PotionData potion = registeredPotions[i];
        //
        //         int amount = potion != null && inventory != null
        //             ? inventory.GetItemCount(potion.Id)
        //             : 0;
        //
        //         slotView.SetSlotIcon(i, potion);
        //         slotView.SetSlotCount(i, amount, potion != null);
        //     }
        // }

        // 장착 대기 상태가 바뀌면 선택 상태와 선택 화면을 맞춘다
        private void HandleEquipModeChanged(bool isEquipping)
        {
            isSelecting = isEquipping;

            if (selectPotion != null)
                selectPotion.SetActive(isEquipping);
        }

        // PotionLoadout의 장착 대기 이벤트를 구독한다
        private void SubscribeLoadout()
        {
            if (loadout != null)
                loadout.OnEquipModeChanged += HandleEquipModeChanged;
        }

        // PotionLoadout의 장착 대기 이벤트 구독을 해제한다
        private void UnsubscribeLoadout()
        {
            if (loadout != null)
                loadout.OnEquipModeChanged -= HandleEquipModeChanged;
        }
    }
}
