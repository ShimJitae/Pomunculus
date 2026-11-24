using UnityEngine;

public enum PickupCategory { Material, Potion }

// 토스트 한 장에 표시할 값
public readonly struct PickupToastData
{
    public readonly string ItemName;
    public readonly int Amount;
    public readonly Sprite Icon;
    public readonly PickupCategory Category;

    // 생성자
    public PickupToastData(string itemName, int amout, Sprite icon, PickupCategory category)
    {
        ItemName = itemName;
        Amount = amout;
        Icon = icon;
        Category = category;
    }
}