using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pomunculus.Items
{
    public class PotionCrafting : MonoBehaviour
    {
        [Header("플레이어 인벤토리")]
        [SerializeField] private Inventory inventory;
        [Header("전체 포션 데이터")]
        [SerializeField] private List<PotionData> potions = new List<PotionData>();
        [Header("실패했을 때 지급할 포션")]
        [SerializeField] private PotionData failurePotion;
        [Header("레시피북")]
        [SerializeField] private RecipeBookUI recipeBook;
        [Header("제작 버튼")]
        [SerializeField] private Button craftButton;
        [Header("돌아가기 버튼")]
        [SerializeField] private Button exitButton;
        [Header("현재 기운 표시")]
        [SerializeField] private TMP_Text physicsText;
        [SerializeField] private TMP_Text fireText;
        [SerializeField] private TMP_Text coldText;
        [SerializeField] private TMP_Text windText;
        [Header("현재 들어간 기운")]
        [SerializeField] private float physicsElement;
        [SerializeField] private float fireElement;
        [SerializeField] private float coldElement;
        [SerializeField] private float windElement;
        [Header("투입한 재료 개수")]
        [SerializeField] private int insertedMaterialCount;
        private const int ResultAmount = 3;
        private const int EmptyBottleId = 10001;
        private void OnEnable()
        {
            RefreshUI();
        }

        public void AddMaterial(MaterialData material)
        {
            if (inventory == null || material == null)
                return;

            if (!inventory.RemoveItem(material.Id, 1))
                return;

            physicsElement += material.PhysicsElement;
            fireElement += material.FireElement;
            coldElement += material.ColdElement;
            windElement += material.WindElement;

            insertedMaterialCount++;

            RefreshUI();
        }

        public void Craft()
        {
            if (inventory == null || insertedMaterialCount <= 0)
                return;

            PotionData matchedPotion = FindMatchingPotion();
            bool succeeded = matchedPotion != null;

            PotionData resultPotion = succeeded ? matchedPotion : failurePotion;

            if (resultPotion == null)
            {
                Debug.LogWarning("실패했을 때 지급할 물 병 데이터를 연결해 주세요.", this);
                return;
            }
            inventory.AddItem(resultPotion.Id, ResultAmount);
            physicsElement = 0;
            fireElement = 0;
            coldElement = 0;
            windElement = 0;
            insertedMaterialCount = 0;

            RefreshUI();
            if (succeeded && recipeBook != null)
                recipeBook.DiscoverPotion(resultPotion.Id);

            Debug.Log($"제작 결과: {resultPotion.ItemName} × {ResultAmount}", this);
        }

        private PotionData FindMatchingPotion()
        {
            foreach (PotionData potion in potions)
            {
                if (potion == null || potion.Id == EmptyBottleId)
                    continue;

                if (!SameElement(physicsElement, potion.PhysicsElement))
                    continue;

                if (!SameElement(fireElement, potion.FireElement))
                    continue;

                if (!SameElement(coldElement, potion.ColdElement))
                    continue;

                if (!SameElement(windElement, potion.WindElement))
                    continue;

                return potion;
            }

            return null;
        }

        private bool SameElement(float current, float required)
        {
            return Mathf.Abs(current - required) < 0.0001f;
        }

        private void RefreshUI()
        {
            physicsText.text = $"× {physicsElement:0.##}";
            fireText.text = $"× {fireElement:0.##}";
            coldText.text = $"× {coldElement:0.##}";
            windText.text = $"× {windElement:0.##}";

            craftButton.interactable = inventory != null && insertedMaterialCount > 0;
            exitButton.interactable = !HasRemainingElements();
        }
        private bool HasRemainingElements()
        {
            return !SameElement(physicsElement, 0f)
                || !SameElement(fireElement, 0f)
                || !SameElement(coldElement, 0f)
                || !SameElement(windElement, 0f);
        }
        public void Close()
        {
            if (HasRemainingElements())
                return;

            gameObject.SetActive(false);
        }
    }
}