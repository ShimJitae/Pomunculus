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
        [SerializeField] private PotionDatabase potionDatabase;

        [Header("위에서부터 레시피 4행")]
        [SerializeField] private RecipeRow[] rows = new RecipeRow[4];

        [Header("공통 UI")]
        [SerializeField] private Sprite unknownIcon;
        [SerializeField] private TMP_Text pageNumberText;
        [SerializeField] private Button backButton;
        [SerializeField] private Button nextButton;

        private const int RowsPerPage = 4;
        private int currentPage;
        private RecipeJournal journal;   // 내 플레이어의 발견 레시피 목록. LocalPlayerUIBinder가 연결

        private void OnEnable()
        {
            Refresh();
        }
        private void OnDestroy()
        {
            if (journal != null)
                journal.OnChanged -= HandleJournalChanged;
        }
        // 내 플레이어의 발견 레시피 목록을 연결한다. 다시 호출하면 이전 연결을 끊고 새로 묶는다
        public void Bind(RecipeJournal newJournal)
        {
            if (journal != null)
                journal.OnChanged -= HandleJournalChanged;

            journal = newJournal;

            if (journal != null)
                journal.OnChanged += HandleJournalChanged;

            HandleJournalChanged();
        }
        // 발견 목록이 바뀌면 열려 있을 때만 다시 그린다. 닫혀 있으면 OnEnable에서 그린다
        private void HandleJournalChanged()
        {
            if (isActiveAndEnabled)
                Refresh();
        }
        public void Refresh()
        {
            List<PotionData> recipes = potionDatabase.Potions
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
                bool discovered = IsDiscovered(potion.Id);

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
        // 제작으로 발견한 레시피를 내 플레이어 목록에 추가한다. 화면 갱신은 OnChanged로 처리
        public void DiscoverPotion(int potionId)
        {
            journal?.Discover(potionId);
        }
        // 내 플레이어가 이미 발견한 레시피인지 확인한다. 아직 연결 전이면 false
        public bool IsDiscovered(int potionId)
        {
            return journal != null && journal.IsDiscovered(potionId);
        }
        private static string Number(float value)
        {
            return value == 0 ? "-" : value.ToString("0.##", CultureInfo.InvariantCulture);
        }
    }
}