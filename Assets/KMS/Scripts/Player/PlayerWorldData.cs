// 월드 저장·입장 동기화 때 주고받는 플레이어 상태
using Pomunculus.Items;
using System.Collections.Generic;

public class PlayerWorldData
{
    public int Owner { get; set; }
    public int CurrentHp { get; set; }
    public int CurrentStamina { get; set; }

    public List<InventorySlot> Item { get; set; }
    public int[] PotionSlotIds { get; set; }
    public int SelectedPotionSlot { get; set; }
    public List<int> DiscoveredPotionIds { get; set; }
    public Dictionary<int, List<List<int>>> CraftHistory { get; set; }
}