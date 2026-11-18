using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

namespace Pomunculus.Items
{
    public class CraftingMaterialListUI : MonoBehaviour
    {
        [System.Serializable]
        public class MaterialSlotView
        {
            public Button Button;
            public Image Icon;
            public TMP_Text AmountText;
        }
        public Inventory inventory;
        [Header("포션 제작")]
        [SerializeField] private PotionCrafting crafting;

        [Header("전체 재료 데이터")]
        [SerializeField] private MaterialDatabase materialDatabase;

        [Header("왼쪽 위부터 재료 칸 6개")]
        [SerializeField] private MaterialSlotView[] slotViews = new MaterialSlotView[6];

        [Header("페이지 UI")]
        [SerializeField] private Button backButton;
        [SerializeField] private Button nextButton;
        [SerializeField] private TMP_Text pageText;
        private const int SlotsPerPage = 6;
        private readonly MaterialData[] displayedMaterials = new MaterialData[SlotsPerPage];
 
        private int currentPage;
        [Header("아이템 툴팁")]
        [SerializeField] private ItemTooltipUI tooltip;
        private int hoveredSlotIndex = -1;
        private void Awake()
        {
            for (int i = 0; i < SlotsPerPage; i++)
            {
                int slotIndex = i;
                slotViews[i].Button.onClick.AddListener(() => OnMaterialClicked(slotIndex));
            }
        }

        private void OnEnable()
        {
            if (inventory == null)
            {
                Debug.LogWarning("플레이어 인벤토리를 연결해 주세요.", this);
                return;
            }

            inventory.Changed += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            if (inventory != null)
                inventory.Changed -= Refresh;

            HideMaterialTooltip();
        }

        private void OnMaterialClicked(int slotIndex)
        {
            MaterialData material = displayedMaterials[slotIndex];

            if (crafting == null || material == null)
                return;

            crafting.AddMaterial(material);
        }

        public void Refresh()
        {
            if (inventory == null)
                return;

            List<MaterialData> ownedMaterials = materialDatabase.Materials
                .Where(material => material != null && inventory.GetItemCount(material.Id) > 0)
                .OrderBy(material => material.Id)
                .ToList();

            int pageCount = Mathf.Max(1, Mathf.CeilToInt(ownedMaterials.Count / (float)SlotsPerPage));
            currentPage = Mathf.Clamp(currentPage, 0, pageCount - 1);

            for (int i = 0; i < SlotsPerPage; i++)
            {
                MaterialSlotView view = slotViews[i];

                int index = currentPage * SlotsPerPage + i;
                bool hasMaterial = index < ownedMaterials.Count;

                displayedMaterials[i] = hasMaterial ? ownedMaterials[index] : null;

                view.Button.gameObject.SetActive(hasMaterial);

                if (!hasMaterial)
                    continue;

                MaterialData material = displayedMaterials[i];

                view.Icon.sprite = material.Icon;
                view.Icon.preserveAspect = true;
                view.AmountText.text = inventory.GetItemCount(material.Id).ToString();
            }

            pageText.text = $"{currentPage + 1} / {pageCount}";
            backButton.interactable = currentPage > 0;
            nextButton.interactable = currentPage < pageCount - 1;
            RefreshMaterialTooltip();
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
        public void ShowMaterialTooltip(int slotIndex)
        {
            hoveredSlotIndex = slotIndex;
            RefreshMaterialTooltip();
        }

        public void HideMaterialTooltip()
        {
            hoveredSlotIndex = -1;

            if (tooltip != null)
                tooltip.HideFor(this);
        }
        private void RefreshMaterialTooltip()
        {
            if (tooltip == null)
                return;
            if (inventory == null ||
                hoveredSlotIndex < 0 ||
                hoveredSlotIndex >= displayedMaterials.Length)
            {
                tooltip.HideFor(this);
                return;
            }
            MaterialData material = displayedMaterials[hoveredSlotIndex];
            if (material == null || Mouse.current == null)
            {
                tooltip.HideFor(this);
                return;
            }

            tooltip.Show(this,
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
    }
}