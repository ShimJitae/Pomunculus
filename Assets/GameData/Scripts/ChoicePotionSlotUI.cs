using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Pomunculus.Items
{
    public class ChoicePotionSlotUI : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private Button button;
        [SerializeField] private Image icon;

        private PotionData potion;
        private Action<PotionData> onSelected;
        private ItemTooltipUI tooltip;

        private void Awake()
        {
            button.onClick.AddListener(Select);
        }

        public void Setup(
            PotionData data,
            Action<PotionData> selected,
            ItemTooltipUI itemTooltip)
        {
            potion = data;
            onSelected = selected;
            tooltip = itemTooltip;

            icon.sprite = potion.Icon;
            icon.preserveAspect = true;
            icon.enabled = icon.sprite != null;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (potion == null || tooltip == null)
                return;

            tooltip.Show(
                this,
                potion.ItemName,
                potion.Description,
                potion.Icon,
                0,
                eventData.position,
                potion.PhysicsElement,
                potion.FireElement,
                potion.ColdElement,
                potion.WindElement,
                false);
        }
        public void OnPointerExit(PointerEventData eventData)
        {
            HideTooltip();
        }
        private void Select()
        {
            HideTooltip();

            if (potion != null)
                onSelected?.Invoke(potion);
        }

        private void OnDisable()
        {
            HideTooltip();
        }

        private void HideTooltip()
        {
            if (tooltip != null)
                tooltip.HideFor(this);
        }
    }
}