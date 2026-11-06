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
        private const int MaxMaterialCount = 3;
        [Header("투입한 재료 아이콘")]
        [SerializeField] private Image[] insertedMaterialIcons = new Image[3];
        private readonly List<MaterialData> insertedMaterials = new List<MaterialData>();
        private void OnEnable()
        {
            RefreshUI();
        }

        public void AddMaterial(MaterialData material)
        {
            if (inventory == null || material == null)
                return;

            // 최대 3개까지만 투입
            if (insertedMaterials.Count >= MaxMaterialCount)
                return;

            if (!inventory.RemoveItem(material.Id, 1))
                return;

            physicsElement += material.PhysicsElement;
            fireElement += material.FireElement;
            coldElement += material.ColdElement;
            windElement += material.WindElement;

            insertedMaterials.Add(material);
            insertedMaterialCount = insertedMaterials.Count;

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

            insertedMaterials.Clear();

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
            exitButton.interactable = true;

            for (int i = 0; i < insertedMaterialIcons.Length && i < MaxMaterialCount; i++)
            {
                Image icon = insertedMaterialIcons[i];

                if (icon == null)
                    continue;

                bool hasMaterial = i < insertedMaterials.Count;

                icon.sprite = hasMaterial ? insertedMaterials[i].Icon : null;
                icon.preserveAspect = true;
                icon.enabled = hasMaterial && icon.sprite != null;
            }
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
            if (insertedMaterials.Count > 0 && inventory == null)
            {
                Debug.LogWarning("재료를 반환할 인벤토리가 연결되지 않았습니다.", this);
                return;
            }
            List<MaterialData> materialsToReturn = new List<MaterialData>(insertedMaterials);
            insertedMaterials.Clear();
            insertedMaterialCount = 0;
            physicsElement = 0;
            fireElement = 0;
            coldElement = 0;
            windElement = 0;
            foreach (MaterialData material in materialsToReturn)
            {
                inventory.AddItem(material.Id, 1);
            }
            RefreshUI();
            gameObject.SetActive(false);
        }
        public void CancelMaterial(int index)
        {
            if (inventory == null)
                return;
            if (index < 0 || index >= insertedMaterials.Count)
                return;
            MaterialData material = insertedMaterials[index];
            physicsElement -= material.PhysicsElement;
            fireElement -= material.FireElement;
            coldElement -= material.ColdElement;
            windElement -= material.WindElement;
            insertedMaterials.RemoveAt(index);
            insertedMaterialCount = insertedMaterials.Count;
            if (insertedMaterialCount == 0)
            {
                physicsElement = 0;
                fireElement = 0;
                coldElement = 0;
                windElement = 0;
            }
            inventory.AddItem(material.Id, 1);
            RefreshUI();
        }
    }
}