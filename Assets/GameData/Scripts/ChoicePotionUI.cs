using System.Collections.Generic;
using UnityEngine;

namespace Pomunculus.Items
{
    public class ChoicePotionUI : MonoBehaviour
    {
        [Header("데이터")]
        [SerializeField] private PotionDatabase potionDatabase;
        [SerializeField] private RecipeBookUI recipeBook;

        [Header("목록")]
        [SerializeField] private Transform content;
        [SerializeField] private ChoicePotionSlotUI slotPrefab;

        [Header("선택 결과를 전달할 기록창")]
        [SerializeField] private PotionCraftHistoryUI historyUI;
        [Header("선택창 전체 배경")]
        [SerializeField] private GameObject choicePotionBackground;
        [Header("포션 툴팁")]
        [SerializeField] private ItemTooltipUI tooltip;

        private readonly List<ChoicePotionSlotUI> slots = new List<ChoicePotionSlotUI>();

        private void OnEnable()
        {
            Refresh();
        }

        public void Open()
        {
            bool wasVisible = isActiveAndEnabled;

            gameObject.SetActive(true);

            if (choicePotionBackground != null)
                choicePotionBackground.SetActive(true);
            if (wasVisible)
                Refresh();
        }
        public void Close()
        {
            if (choicePotionBackground != null)
                choicePotionBackground.SetActive(false);
            else
                gameObject.SetActive(false);
        }

        private void Refresh()
        {
            foreach (ChoicePotionSlotUI slot in slots)
            {
                if (slot == null)
                    continue;

                slot.gameObject.SetActive(false);
                Destroy(slot.gameObject);
            }

            slots.Clear();

            if (potionDatabase == null || recipeBook == null ||
                content == null || slotPrefab == null)
                return;

            List<PotionData> discovered = new List<PotionData>();

            foreach (PotionData potion in potionDatabase.Potions)
            {
                if (potion == null || potion.Id == 10001)
                    continue;

                if (recipeBook.IsDiscovered(potion.Id))
                    discovered.Add(potion);
            }

            discovered.Sort((a, b) => a.Id.CompareTo(b.Id));

            foreach (PotionData potion in discovered)
            {
                ChoicePotionSlotUI slot =
                    Instantiate(slotPrefab, content);

                slot.Setup(potion, SelectPotion, tooltip);
                slot.gameObject.SetActive(true);
                slots.Add(slot);
            }
        }

        private void SelectPotion(PotionData potion)
        {
            if (historyUI == null)
                return;

            historyUI.gameObject.SetActive(true);
            historyUI.SelectPotion(potion.Id);
            Close();
        }
    }
}