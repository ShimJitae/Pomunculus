using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pomunculus.Items
{
    public class PotionCraftHistoryUI : MonoBehaviour
    {
        [Header("제작 기록")]
        [SerializeField] private PotionCraftHistory craftHistory;

        [Header("표시할 줄 — 위에서부터")]
        [SerializeField]
        private CraftHistoryRowUI[] rows
            = new CraftHistoryRowUI[3];

        [Header("페이지 버튼")]
        [SerializeField] private Button backButton;
        [SerializeField] private Button nextButton;
        [SerializeField] private TMP_Text pageText;

        [Header("선택한 포션 그림")]
        [SerializeField] private PotionDatabase potionDatabase;
        [SerializeField] private Image selectedPotionImage;

        [Header("연결할 제작창")]
        [SerializeField] private PotionCrafting potionCrafting;
         
        private int selectedPotionId = -1;
        private int currentPage;

        private void OnEnable()
        {
            Refresh();
        }

        // 포션 선택 버튼에서 호출
        public void SelectPotion(int potionId)
        {
            selectedPotionId = potionId;
            currentPage = 0;

            PotionData potion = potionDatabase != null
                ? potionDatabase.Find(potionId)
                : null;

            if (selectedPotionImage != null && potion != null)
            {
                selectedPotionImage.sprite = potion.Icon;
                selectedPotionImage.preserveAspect = true;
            }

            Refresh();
        }

        public void NextPage()
        {
            if (currentPage >= GetPageCount() - 1)
                return;

            currentPage++;
            Refresh();
        }

        public void PreviousPage()
        {
            if (currentPage <= 0)
                return;

            currentPage--;
            Refresh();
        }

        private int GetRecordCount()
        {
            if (craftHistory == null || selectedPotionId < 0)
                return 0;

            return craftHistory.GetCombinationCount(selectedPotionId);
        }

        private int GetPageCount()
        {
            if (rows == null || rows.Length == 0)
                return 1;

            return Mathf.Max(
                1,
                Mathf.CeilToInt((float)GetRecordCount() / rows.Length));
        }

        private void Refresh()
        {
            int recordCount = GetRecordCount();
            int pageCount = GetPageCount();

            currentPage = Mathf.Clamp(currentPage, 0, pageCount - 1);

            if (rows != null)
            {
                for (int i = 0; i < rows.Length; i++)
                {
                    if (rows[i] == null)
                        continue;

                    int index = currentPage * rows.Length + i;

                    if (index < recordCount)
                    {
                        rows[i].Show(
                            craftHistory.GetCombination(
                                selectedPotionId, index));
                    }
                    else
                    {
                        rows[i].Hide();
                    }
                }
            }

            if (pageText != null)
                pageText.text = $"{currentPage + 1} / {pageCount}";

            if (backButton != null)
                backButton.interactable = currentPage > 0;

            if (nextButton != null)
                nextButton.interactable = currentPage < pageCount - 1;
        }
        public void Close()
        {
            gameObject.SetActive(false);
        }
        public void ApplyRecordedCombination(int rowIndex)
        {
            if (potionCrafting == null || craftHistory == null ||
                rows == null || rowIndex < 0 || rowIndex >= rows.Length)
                return;

            // 현재 페이지에서 클릭한 줄의 기록 번호
            int recordIndex = currentPage * rows.Length + rowIndex;

            var materialIds = craftHistory.GetCombination(
                selectedPotionId, recordIndex);

            if (!potionCrafting.TryLoadMaterials(materialIds))
                return;

            potionCrafting.gameObject.SetActive(true);
            Close();
        }
    }
}