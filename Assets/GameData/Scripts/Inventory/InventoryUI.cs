using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Pomunculus.Items
{
    // IRightClickConsumer : 인벤토리 창 위에서 우클릭해도 플레이어가 이동하지 않게 한다 (PlayerInputReader가 확인)
    public class InventoryUI : MonoBehaviour, IRightClickConsumer
    {
        private enum InventoryTab
        {
            All,
            Material,
            Potion
        }
        private class DisplayItem
        {
            public PotionData Potion;
            public int Id;
            public string Name;
            public string Description;
            public Sprite Icon;
            public int Amount;
            public float Physics;
            public float Fire;
            public float Cold;
            public float Wind;
        }
        public Inventory inventory;
        [Header("아이템 툴팁")]
        [SerializeField] private ItemTooltipUI tooltip;

        [Header("전체 아이템 데이터")]
        [SerializeField] private MaterialDatabase materialDatabase;
        [SerializeField] private PotionDatabase potionDatabase;

        [Header("슬롯 생성")]
        [SerializeField] private Transform content;
        [SerializeField] private InventorySlotUI slotPrefab;
        [Header("스크롤")]
        [SerializeField] private ScrollRect scrollRect;
        private const int MinimumSlotCount = 36;
        private const int ColumnCount = 6;
        private readonly List<InventorySlotUI> slotViews = new List<InventorySlotUI>();
        [Header("포션 슬롯 등록")]
        [SerializeField] private PotionSlotSelector potionSlotSelector;
        [Header("사운드")]
        [SerializeField] private AudioSO inventoryCloseSound;


        public event Action<bool> OnOpenChanged;
        public bool IsOpen => isActiveAndEnabled;

        private InventoryTab currentTab = InventoryTab.All;

        private void OnEnable()
        {
            if (inventory == null)
                return;

            inventory.Changed += Refresh;
            Refresh();
            OnOpenChanged?.Invoke(true);
        }

        private void OnDisable()
        {
            if (inventory != null)
                inventory.Changed -= Refresh;

            if (tooltip != null)
                tooltip.Hide();

            if (potionSlotSelector != null)
                potionSlotSelector.CancelSelection();

            OnOpenChanged?.Invoke(false);

            // 실제로 패널이 꺼졌을 때 닫기 소리 재생
            if (Application.isPlaying &&
                !gameObject.activeInHierarchy &&
                AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySound(inventoryCloseSound);
            }
        }

        public void Refresh()
        {
            if (inventory == null)
                return;

            List<DisplayItem> items = GetDisplayItems();

            int rowCount = Mathf.CeilToInt(items.Count / (float)ColumnCount);
            int requiredSlots = Mathf.Max(MinimumSlotCount, rowCount * ColumnCount);

            while (slotViews.Count < requiredSlots)
            {
                InventorySlotUI view = Instantiate(slotPrefab, content);
                view.OnDropRequested += HandleDropRequested;
                slotViews.Add(view);
            }

            for (int i = 0; i < slotViews.Count; i++)
            {
                InventorySlotUI view = slotViews[i];
                bool visible = i < requiredSlots;

                view.gameObject.SetActive(visible);

                if (!visible)
                    continue;

                if (i < items.Count)
                {
                    view.ShowItem(items[i].Id, items[i].Icon, items[i].Amount, items[i].Name, items[i].Description, tooltip, 
                        items[i].Physics, items[i].Fire, items[i].Cold, items[i].Wind);
                    view.SetPotionSelection(items[i].Potion, potionSlotSelector);
                }
                else
                {
                    view.ShowEmpty();
                }
            }
        }
        private List<DisplayItem> GetDisplayItems()
        {
            List<DisplayItem> result = new List<DisplayItem>();

            if (currentTab != InventoryTab.Potion)
            {
                foreach (MaterialData material in materialDatabase.Materials)
                {
                    if (material == null)
                        continue;

                    int amount = inventory.GetItemCount(material.Id);

                    if (amount <= 0)
                        continue;

                    result.Add(new DisplayItem
                    {
                        Id = material.Id,
                        Icon = material.Icon,
                        Amount = amount,
                        Name = material.ItemName,
                        Description = material.Description,
                        Physics = material.PhysicsElement,
                        Fire = material.FireElement,
                        Cold = material.ColdElement,
                        Wind = material.WindElement
                    });
                }
            }
            if (currentTab != InventoryTab.Material)
            {
                foreach (PotionData potion in potionDatabase.Potions)
                {
                    if (potion == null)
                        continue;

                    int amount = inventory.GetItemCount(potion.Id);

                    if (amount <= 0)
                        continue;

                    result.Add(new DisplayItem
                    {
                        Id = potion.Id,
                        Name = potion.ItemName,
                        Description = potion.Description,
                        Icon = potion.Icon,
                        Amount = amount,
                        Potion = potion,
                        Physics = potion.PhysicsElement,
                        Fire = potion.FireElement,
                        Cold = potion.ColdElement,
                        Wind = potion.WindElement
                    });
                }
            }

            result.Sort((a, b) => a.Id.CompareTo(b.Id));

            return result;
        }

        // 슬롯 우클릭 시 아이템을 바닥에 버린다. dropAll이면 보유한 전부, 아니면 1개. 화면은 Inventory.Changed로 갱신된다
        private void HandleDropRequested(int itemId, bool dropAll)
        {
            if (inventory == null)
                return;

            int amount = dropAll ? inventory.GetItemCount(itemId) : 1;
            inventory.DropItem(itemId, amount);
        }

        public void ShowAll()
        {
            ChangeTab(InventoryTab.All);
        }

        public void ShowMaterials()
        {
            ChangeTab(InventoryTab.Material);
        }

        public void ShowPotions()
        {
            ChangeTab(InventoryTab.Potion);
        }

        private void ChangeTab(InventoryTab tab)
        {
            currentTab = tab;
            Refresh();

            Canvas.ForceUpdateCanvases();
            scrollRect.verticalNormalizedPosition = 1f;
        }
        public void Close()
        {
            gameObject.SetActive(false);
        }
        public void SetPotionSlotSelector(PotionSlotSelector selector)
        {
            potionSlotSelector = selector;
        }
    }
}