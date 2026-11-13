using System.Collections.Generic;
using UnityEngine;

namespace Pomunculus.Items
{
    public class Inventory : MonoBehaviour
    {
        [SerializeField] private List<InventorySlot> slots = new List<InventorySlot>();
        public List<InventorySlot> Slots => slots;
        public event System.Action Changed;
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
    }
}