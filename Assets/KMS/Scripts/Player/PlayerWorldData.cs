// 월드 저장·입장 동기화 때 주고받는 플레이어 상태
using Pomunculus.Items;
using System.Collections.Generic;

public class PlayerWorldData
{
    public int Owner { get; set; }
    public int CurrentHp { get; set; }
    public int CurrentStamina { get; set; }

    // TODO 인벤토리 데이터, 알아낸 레시피 기록 등 저장할 내역들 추가해야함
    public List<InventorySlot> Item { get; set; }
}