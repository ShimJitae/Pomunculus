using UnityEngine;
using UnityEngine.InputSystem;

namespace Pomunculus.Items
{
    public class PotionSlotSelector : MonoBehaviour
    {
        [Header("플레이어 인벤토리")]
        [SerializeField] private Inventory inventory;

        [Header("선택 화면")]
        [SerializeField] private GameObject selectPotion;
        [SerializeField] private ItemTooltipUI tooltip;

        [Header("왼쪽 포션 슬롯")]
        [SerializeField] private PotionSlotView slotView;

        [Header("등록된 포션")]
        [SerializeField] private PotionData[] registeredPotions = new PotionData[3];

        private PotionData selectedPotion;

        public bool IsSelecting => selectedPotion != null;

        private void Start()
        {
            CancelSelection();

            for (int i = 0; i < registeredPotions.Length; i++)
            {
                PotionData potion = registeredPotions[i];
                slotView.SetSlotIcon(i, potion != null ? potion.Icon : null);
            }
        }

        private void Update()
        {
            if (!IsSelecting || Keyboard.current == null)
                return;

            if (Keyboard.current.escapeKey.wasPressedThisFrame)
                CancelSelection();
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
                slotView.SetSlotIcon(slotIndex, null);
                return;
            }

            if (inventory == null || inventory.GetItemCount(selectedPotion.Id) <= 0)
            {
                CancelSelection();
                return;
            }

            registeredPotions[slotIndex] = selectedPotion;
            slotView.SetSlotIcon(slotIndex, selectedPotion.Icon);

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
            CancelSelection();
        }
    }
}