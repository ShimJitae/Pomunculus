using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

namespace Pomunculus.Items
{
    public class PotionCrafting : MonoBehaviour
    {
        public Inventory inventory;
        [Header("전체 포션 데이터")]
        [SerializeField] private PotionDatabase potionDatabase;
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
        [Header("제작 결과 미리보기")]
        [SerializeField] private Image resultPreviewImage;
        [SerializeField] private Sprite unknownIcon;
        [Header("아이템 툴팁")]
        [SerializeField] private ItemTooltipUI tooltip;
        private int hoveredMaterialIndex = -1;
        private bool isResultHovered;
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
            foreach (PotionData potion in potionDatabase.Potions)
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
            RefreshResultPreview();
            RefreshInsertedMaterialTooltip();
            RefreshResultTooltip();

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
        public void ResetUi()
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
        //결과이미지 보이기
        private void RefreshResultPreview()
        {
            if (resultPreviewImage == null)
                return;

            if (insertedMaterials.Count == 0)
            {
                resultPreviewImage.sprite = null;
                resultPreviewImage.enabled = false;
                return;
            }

            PotionData potion = FindMatchingPotion();

            bool discovered = potion != null
                && recipeBook != null
                && recipeBook.IsDiscovered(potion.Id);

            resultPreviewImage.sprite = discovered ? potion.Icon : unknownIcon;
            resultPreviewImage.preserveAspect = true;
            resultPreviewImage.enabled = resultPreviewImage.sprite != null;
        }
        public void ShowInsertedMaterialTooltip(int index)
        {
            hoveredMaterialIndex = index;
            RefreshInsertedMaterialTooltip();
        }

        public void HideInsertedMaterialTooltip()
        {
            hoveredMaterialIndex = -1;

            if (tooltip != null)
                tooltip.HideFor(this);
        }

        private void RefreshInsertedMaterialTooltip()
        {
            if (tooltip == null)
                return;

            if (hoveredMaterialIndex < 0 ||
                hoveredMaterialIndex >= insertedMaterials.Count ||
                inventory == null ||
                Mouse.current == null)
            {
                tooltip.HideFor(this);
                return;
            }

            MaterialData material = insertedMaterials[hoveredMaterialIndex];

            tooltip.Show(
                this,
                material.ItemName,
                material.Description,
                material.Icon,
                inventory.GetItemCount(material.Id),
                Mouse.current.position.ReadValue(),
                material.PhysicsElement,
                material.FireElement,
                material.ColdElement,
                material.WindElement
            );
        }
        private void OnDisable()
        {
            HideInsertedMaterialTooltip();
            HideResultTooltip();
        }
        public void ShowResultTooltip()
        {
            isResultHovered = true;
            RefreshResultTooltip();
        }

        public void HideResultTooltip()
        {
            isResultHovered = false;

            if (tooltip != null)
                tooltip.HideFor(this);
        }

        private void RefreshResultTooltip()
        {
            if (!isResultHovered || tooltip == null)
                return;

            PotionData potion = insertedMaterials.Count > 0
                ? FindMatchingPotion()
                : null;

            bool discovered = potion != null
                && recipeBook != null
                && recipeBook.IsDiscovered(potion.Id);

            if (!discovered || inventory == null || Mouse.current == null)
            {
                tooltip.HideFor(this);
                return;
            }

            tooltip.Show(
                this,
                potion.ItemName,
                potion.Description,
                potion.Icon,
                inventory.GetItemCount(potion.Id),
                Mouse.current.position.ReadValue(),
                potion.PhysicsElement,
                potion.FireElement,
                potion.ColdElement,
                potion.WindElement
            );
        }
    }
}