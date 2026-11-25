using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Pomunculus.Items
{
    public class InventorySlotUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text amountText;

        // 우클릭으로 버리기를 요청 (itemId, 전부 버릴지). Alt를 누른 채 우클릭하면 전부
        public event Action<int, bool> OnDropRequested;

        private ItemTooltipUI tooltip;
        private int itemId;
        private string itemName;
        private string description;
        private int itemAmount;
        private float physicsElement;
        private float fireElement;
        private float coldElement;
        private float windElement;

        private bool hasItem;
        private bool isHovered;
        private Vector2 pointerPosition;

        private PotionData potionData;
        private PotionSlotSelector potionSlotSelector;

        public void ShowItem(int id, Sprite sprite, int amount, string name, string desc, ItemTooltipUI itemTooltip,
            float physics, float fire, float cold, float wind)
        {
            itemId = id;
            tooltip = itemTooltip;
            itemName = name;
            description = desc;
            itemAmount = amount;
            hasItem = true;
            physicsElement = physics;
            fireElement = fire;
            coldElement = cold;
            windElement = wind;

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

            itemId = 0;
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

            tooltip.Show(this, itemName, description, icon.sprite, itemAmount, pointerPosition,
                physicsElement, fireElement, coldElement, windElement);
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
            if (eventData.button == PointerEventData.InputButton.Right)
            {
                RequestDrop();
                return;
            }

            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            if (!hasItem || potionData == null || potionSlotSelector == null)
                return;

            potionSlotSelector.BeginSelection(potionData);
        }

        // 우클릭한 슬롯의 아이템 버리기를 요청한다. Alt를 누르고 있으면 전부, 아니면 1개. 포션 장착 선택 중에는 무시
        private void RequestDrop()
        {
            if (!hasItem || itemId <= 0)
                return;
            if (potionSlotSelector != null && potionSlotSelector.IsSelecting)
                return;

            bool dropAll = Keyboard.current != null && Keyboard.current.altKey.isPressed;
            OnDropRequested?.Invoke(itemId, dropAll);
        }
    }
}