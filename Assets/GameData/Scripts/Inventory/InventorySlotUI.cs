using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Pomunculus.Items
{
    public class InventorySlotUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text amountText;

        private ItemTooltipUI tooltip;
        private string itemName;
        private string description;
        private int itemAmount;

        private bool hasItem;
        private bool isHovered;
        private Vector2 pointerPosition;

        private PotionData potionData;
        private PotionSlotSelector potionSlotSelector;

        public void ShowItem(Sprite sprite, int amount, string name, string desc, ItemTooltipUI itemTooltip)
        {
            tooltip = itemTooltip;
            itemName = name;
            description = desc;
            itemAmount = amount;
            hasItem = true;

            icon.sprite = sprite;
            icon.preserveAspect = true;
            icon.gameObject.SetActive(true);

            amountText.text = amount.ToString();
            amountText.gameObject.SetActive(true);

            if (isHovered)
                ShowTooltip();
        }

        public void ShowEmpty()
        {
            potionData = null;
            if (tooltip != null)
                tooltip.HideFor(this);

            hasItem = false;

            icon.sprite = null;
            icon.gameObject.SetActive(false);

            amountText.text = "";
            amountText.gameObject.SetActive(false);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            isHovered = true;
            pointerPosition = eventData.position;

            ShowTooltip();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            isHovered = false;

            if (tooltip != null)
                tooltip.HideFor(this);
        }

        private void ShowTooltip()
        {
            if (!hasItem || tooltip == null)
                return;
            if (potionSlotSelector != null && potionSlotSelector.IsSelecting)
                return;

            tooltip.Show(this, itemName, description, icon.sprite, itemAmount, pointerPosition);
        }

        private void OnDisable()
        {
            isHovered = false;

            if (tooltip != null)
                tooltip.HideFor(this);
        }
        public void SetPotionSelection(PotionData potion, PotionSlotSelector selector)
        {
            potionData = potion;
            potionSlotSelector = selector;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            if (!hasItem || potionData == null || potionSlotSelector == null)
                return;

            potionSlotSelector.BeginSelection(potionData);
        }
    }
}