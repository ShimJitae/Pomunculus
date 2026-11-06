using System;
using UnityEngine;

namespace Pomunculus.Items
{
    public class PotionLoadout : MonoBehaviour
    {
        public const int SlotCount = 3;

        [SerializeField] private PotionData[] slots = new PotionData[SlotCount];
        private int selectedIndex;

        public event Action OnChanged;

        public int SelectedIndex => selectedIndex;
        public PotionData SelectedPotion => slots[selectedIndex];

        // 슬롯에 등록된 포션을 가져온다. 범위 밖이면 null
        public PotionData GetSlot(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= SlotCount) return null;
            return slots[slotIndex];
        }

        // 슬롯에 포션을 등록한다. null이면 슬롯을 비운다
        public void SetSlot(int slotIndex, PotionData potion)
        {
            if (slotIndex < 0 || slotIndex >= SlotCount) return;
            slots[slotIndex] = potion;
            OnChanged?.Invoke();
        }

        // 던질 슬롯을 바꾼다
        public void SelectSlot(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= SlotCount || slotIndex == selectedIndex) return;
            selectedIndex = slotIndex;
            OnChanged?.Invoke();
        }
    }
}