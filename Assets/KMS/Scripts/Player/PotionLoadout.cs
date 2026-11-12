using System;
using UnityEngine;


namespace Pomunculus.Items
{
    public enum EquipMode
    {
        PendingPotion,
        IsEquipping
    }

    public class PotionLoadout : MonoBehaviour
    {
        public const int SlotCount = 3;

        [SerializeField] private PotionData[] slots = new PotionData[SlotCount];
        private int selectedIndex;

        public EquipMode EquipMode;

        public event Action<int> OnSelectChanged; // 슬롯 선택 변경시
        public event Action<int, PotionData> OnEquipChanged; // 포션 장착 및 해제시
        public event Action<bool> OnEquipModeChanged;

        public PotionData PendingPotion { get; private set; }
        public bool IsEquipping => PendingPotion != null;

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
            OnEquipChanged?.Invoke(slotIndex, potion);
        }

        // 던질 슬롯을 바꾼다
        public void SelectSlot(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= SlotCount || slotIndex == selectedIndex) return;
            selectedIndex = slotIndex;
            OnSelectChanged?.Invoke(selectedIndex);
        }

        // 인벤토리에서 고른 포션으로 장착 대기를 시작한다
        public void BeginEquip(PotionData potion)
        {
            if (potion == null) return;
            PendingPotion = potion;
            OnEquipModeChanged?.Invoke(true);
        }

        // 대기 중인 포션을 슬롯에 장착하고 대기를 끝낸다
        public void EquipToSlot(int slotIndex)
        {
            if (!IsEquipping) return;
            SetSlot(slotIndex, PendingPotion);
            CancelEquip();
        }

        // 장착 대기를 취소한다
        public void CancelEquip()
        {
            if (!IsEquipping) return;
            PendingPotion = null;
            OnEquipModeChanged?.Invoke(false);
        }
    }
}