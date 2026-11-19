using System.Collections.Generic;
using UnityEngine;

namespace Pomunculus.Items
{
    public class Inventory : MonoBehaviour
    {
        [SerializeField] private List<InventorySlot> slots = new List<InventorySlot>();
        public List<InventorySlot> Slots => slots;
        public event System.Action Changed;

        [Header("드롭")]
        [SerializeField] private float dropScatterRadius = 0.7f;   // 드롭 위치를 흩뿌릴 반경
        [SerializeField] private float dropHeight = 0.5f;          // 바닥에서 띄울 높이


        public void AddItem(int itemId, int amount = 1)
        {
            if (itemId <= 0 || amount <= 0)
                return;
            foreach (InventorySlot slot in slots)
            {
                if (slot.ItemId == itemId)
                {
                    slot.Amount += amount;
                    Changed?.Invoke();
                    return;
                }
            }
            slots.Add(new InventorySlot(itemId, amount));
            Changed?.Invoke();
        }
        public int GetItemCount(int itemId)
        {
            foreach (InventorySlot slot in slots)
            {
                if (slot.ItemId == itemId)
                    return slot.Amount;
            }

            return 0;
        }
        public bool RemoveItem(int itemId, int amount = 1)
        {
            if (amount <= 0)
                return false;

            for (int i = 0; i < slots.Count; i++)
            {
                InventorySlot slot = slots[i];

                if (slot.ItemId != itemId)
                    continue;

                if (slot.Amount < amount)
                    return false;

                slot.Amount -= amount;

                if (slot.Amount == 0)
                    slots.RemoveAt(i);

                Changed?.Invoke();
                return true;
            }
            return false;
        }

        // 인벤토리의 모든 아이템을 종류별로 하나씩 떨어트리고, 떨어트린 슬롯만 인벤토리에서 지운다
        [ContextMenu("버리기")]
        public void DropAllItems()
        {
            if (slots.Count == 0) return;

            int totalCount = slots.Count;

            bool removeAny = false;
            for (int i = slots.Count - 1; i >= 0; i--)
            {
                Vector3 dropPos = GetDropPosition(i, totalCount);

                if (!TryDropItem(slots[i], dropPos))
                    continue;

                slots.RemoveAt(i);
                removeAny = true;
            }

            if (removeAny)
                Changed?.Invoke();
        }

        // 슬롯 하나를 드롭 오브젝트 하나로 떨어트린다. 수량은 SaveData에 담아 넘긴다. 성공하면 true
        private bool TryDropItem(InventorySlot slot, Vector3 position)
        {
            GameObject prefab = FindDropPrefab(slot.ItemId);
            if (prefab == null)
            {
                Debug.LogWarning($"[Inventory] 드롭 프리팹이 없는 아이템 : {slot.ItemId}");
                return false;
            }

            var saveData = SaveData.CreateSaveData(slot.Amount);
            WorldManager.Instance.RequestSpawn(prefab.name, position, Quaternion.identity, saveData);
            return true;
        }

        //// 플레이어 주변에 겹치지 않게 흩뿌린 드롭 위치를 구한다
        //private Vector3 GetDropPosition()
        //{
        //    Vector2 circle = Random.insideUnitCircle * dropScatterRadius;
        //    Vector3 offset = new Vector3(circle.x, 0f, circle.y);
        //    return transform.position + offset + Vector3.up * dropHeight;
        //}

        // index번째 아이템을 플레이어 둘레에 같은 각도 간격으로 배치한다
        private Vector3 GetDropPosition(int index, int totalCount)
        {
            float angle = (360f / totalCount) * index * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * dropScatterRadius;
            return transform.position + offset + Vector3.up * dropHeight;
        }

        // 아이템 ID로 바닥에 떨어트릴 프리팹을 찾는다. 재료 → 포션 순으로 조회하고, 없으면 null
        public static GameObject FindDropPrefab(int itemId)
        {
            MaterialData material = MaterialDatabase.Instance.Find(itemId);
            if (material != null)
                return material.Prefab;
            PotionData potion = PotionDatabase.Instance.Find(itemId);
            if (potion != null)
                return potion.PickupPrefab.gameObject;
            return null;
        }
    }
}