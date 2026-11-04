using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pomunculus.Items
{
    public class RecipeBookUI : MonoBehaviour
    {
        [System.Serializable]
        public class RecipeRow
        {
            public TMP_Text PhysicsText;
            public TMP_Text FireText;
            public TMP_Text ColdText;
            public TMP_Text WindText;
            public Image EqualsImage;
            public Image PotionImage;

            public void SetVisible(bool visible)
            {
                PhysicsText.gameObject.SetActive(visible);
                FireText.gameObject.SetActive(visible);
                ColdText.gameObject.SetActive(visible);
                WindText.gameObject.SetActive(visible);
                EqualsImage.gameObject.SetActive(visible);
                PotionImage.gameObject.SetActive(visible);
            }
        }

        [Header("포션 데이터")]
        [SerializeField] private List<PotionData> potions = new List<PotionData>();

        [Header("위에서부터 레시피 4행")]
        [SerializeField] private RecipeRow[] rows = new RecipeRow[4];

        [Header("공통 UI")]
        [SerializeField] private Sprite unknownIcon;
        [SerializeField] private TMP_Text pageNumberText;
        [SerializeField] private Button backButton;
        [SerializeField] private Button nextButton;

        [Header("발견한 포션 ID - 현재는 확인용")]
        [SerializeField] private List<int> discoveredPotionIds = new List<int>();

        private const int RowsPerPage = 4;
        private int currentPage;

        private void OnEnable()
        {
            Refresh();
        }
        public void Refresh()
        {
            List<PotionData> recipes = potions
                .Where(potion => potion != null && potion.Id != 10001)
                .OrderBy(potion => potion.Id)
                .ToList();

            int pageCount = Mathf.Max(1, Mathf.CeilToInt(recipes.Count / (float)RowsPerPage));
            currentPage = Mathf.Clamp(currentPage, 0, pageCount - 1);

            for (int i = 0; i < RowsPerPage; i++)
            {
                RecipeRow row = rows[i];
                int index = currentPage * RowsPerPage + i;
                bool hasRecipe = index < recipes.Count;

                row.SetVisible(hasRecipe);

                if (!hasRecipe)
                    continue;

                PotionData potion = recipes[index];
                bool discovered = discoveredPotionIds.Contains(potion.Id);

                row.PhysicsText.text = discovered ? Number(potion.PhysicsElement) : "?";
                row.FireText.text = discovered ? Number(potion.FireElement) : "?";
                row.ColdText.text = discovered ? Number(potion.ColdElement) : "?";
                row.WindText.text = discovered ? Number(potion.WindElement) : "?";
                row.PotionImage.sprite = discovered ? potion.Icon : unknownIcon;
                row.PotionImage.preserveAspect = true;
                if (row.PotionImage.TryGetComponent(out PotionNameHover hover))
                    hover.SetPotion(potion, discovered);
            }

            pageNumberText.text = $"{currentPage + 1} / {pageCount}";
            backButton.interactable = currentPage > 0;
            nextButton.interactable = currentPage < pageCount - 1;
        }
        public void NextPage()
        {
            currentPage++;
            Refresh();
        }
        public void PreviousPage()
        {
            currentPage--;
            Refresh();
        }
        public void Close()
        {
            gameObject.SetActive(false);
        }
        public void DiscoverPotion(int potionId)
        {
            if (potionId == 10001 || discoveredPotionIds.Contains(potionId))
                return;
            discoveredPotionIds.Add(potionId);
            Refresh();
        }
        private static string Number(float value)
        {
            return value == 0 ? "-" : value.ToString("0.##", CultureInfo.InvariantCulture);
        }
    }
}