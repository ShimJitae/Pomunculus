using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Pomunculus.Items
{
    // 인벤토리에서 고른 포션의 장착 대기를 PotionLoadout에 시작/취소하고, 선택 화면(어두운 패널)을 켜고 끈다.
    public class PotionSlotSelector : MonoBehaviour
    {
        private Inventory inventory;
        private PotionLoadout loadout;

        [Header("선택 화면")]
        [SerializeField] private ItemTooltipUI tooltip;

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
            SubscribeLoadout();

            CancelSelection();
        }

        // 인벤토리에서 클릭한 포션으로 장착 대기를 시작한다
        public void BeginSelection(PotionData potion)
        {
            if (potion == null || inventory == null || loadout == null)
                return;

            if (inventory.GetItemCount(potion.Id) <= 0)
                return;

            if (tooltip != null)
                tooltip.Hide();

            loadout.BeginEquip(potion);
        }

        // 장착 대기를 취소하고 선택 화면을 닫는다
        public void CancelSelection()
        {
            if (loadout != null)
                loadout.CancelEquip();

            HandleEquipModeChanged(false);
        }

        private void OnDisable()
        {
            CancelSelection();
            UnsubscribeLoadout();
        }

        // 장착 대기 상태가 바뀌면 선택 상태와 선택 화면을 맞춘다
        private void HandleEquipModeChanged(bool isEquipping)
        {
            isSelecting = isEquipping;
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
