using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Pomunculus.Items
{
    public class PotionSlotSelector : MonoBehaviour
    {
        public Inventory inventory;
        [Header("선택 화면")]
        [SerializeField] private GameObject selectPotion;
        [SerializeField] private ItemTooltipUI tooltip;

        [Header("포션 슬롯")]
        [SerializeField] private PotionSlotView slotView;

        [Header("등록된 포션")]
        [SerializeField] private PotionData[] registeredPotions = new PotionData[3];
        public int ActiveSlotIndex { get; private set; } = -1;
        private PotionData selectedPotion;
        public PotionData selectedPotionData => GetRegisteredPotion(ActiveSlotIndex);

        public bool IsSelecting => selectedPotion != null;
        private void OnEnable()
        {
            if (inventory != null)
                inventory.Changed += RefreshSlots;

            CancelSelection();
            RefreshSlots();
            SelectActiveSlot(ActiveSlotIndex);
        }
        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
                return;
            if (IsSelecting)
            {
                if (keyboard.escapeKey.wasPressedThisFrame)
                    CancelSelection();
                return;
            }
            if (keyboard.digit1Key.wasPressedThisFrame)
            {
                SelectActiveSlot(0);
            }
            else if (keyboard.digit2Key.wasPressedThisFrame)
            {
                SelectActiveSlot(1);
            }
            else if (keyboard.digit3Key.wasPressedThisFrame)
            {
                SelectActiveSlot(2);
            }
        }
        private void SelectActiveSlot(int slotIndex)
        {
            ActiveSlotIndex = slotIndex;
            if (slotView != null)
                slotView.SetSelectedSlot(slotIndex);
        }
        public void BeginSelection(PotionData potion)
        {
            if (potion == null || inventory == null)
                return;

            if (inventory.GetItemCount(potion.Id) <= 0)
                return;

            selectedPotion = potion;

            if (tooltip != null)
                tooltip.Hide();

            selectPotion.SetActive(true);
        }

        public void RegisterPotion(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= registeredPotions.Length)
                return;

            if (!IsSelecting)
            {
                registeredPotions[slotIndex] = null;
                RefreshSlots();
                return;
            }

            if (inventory == null || inventory.GetItemCount(selectedPotion.Id) <= 0)
            {
                CancelSelection();
                return;
            }

            registeredPotions[slotIndex] = selectedPotion;

            RefreshSlots();
            CancelSelection();
        }

        public PotionData GetRegisteredPotion(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= registeredPotions.Length)
                return null;

            return registeredPotions[slotIndex];
        }

        public void CancelSelection()
        {
            selectedPotion = null;

            if (selectPotion != null)
                selectPotion.SetActive(false);
        }

        private void OnDisable()
        {
            if (inventory != null)
                inventory.Changed -= RefreshSlots;

            CancelSelection();
        }
        private void RefreshSlots()
        {
            if (slotView == null)
                return;

            for (int i = 0; i < registeredPotions.Length; i++)
            {
                PotionData potion = registeredPotions[i];

                int amount = potion != null && inventory != null
                    ? inventory.GetItemCount(potion.Id)
                    : 0;

                slotView.SetSlotIcon(i, potion != null ? potion.Icon : null);
                slotView.SetSlotCount(i, amount, potion != null);
            }
        }
    }
}