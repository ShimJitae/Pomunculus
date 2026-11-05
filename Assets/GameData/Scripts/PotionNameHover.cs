using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Pomunculus.Items
{
    public class PotionNameHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private TMP_Text nameText;

        private PotionData potionData;
        private bool discovered;

        public void SetPotion(PotionData potion, bool isDiscovered)
        {
            potionData = potion;
            discovered = isDiscovered;

            if (nameText != null)
            {
                nameText.text = discovered && potionData != null ? potionData.ItemName : "미발견 포션";
                nameText.gameObject.SetActive(false);
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (nameText != null && potionData != null)
                nameText.gameObject.SetActive(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            Hide();
        }

        private void OnDisable()
        {
            Hide();
        }

        private void Hide()
        {
            if (nameText != null)
                nameText.gameObject.SetActive(false);
        }
    }
}