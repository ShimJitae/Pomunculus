using Pomunculus.Items;
using System;
using UnityEngine;

public class PickupUIPresenter : IDisposable
{
    // Model
    private readonly PlayerFSM player;

    // View
    private readonly IPickupView view;

    public PickupUIPresenter(PlayerFSM player, IPickupView view)
    {
        this.player = player;
        this.view = view;

        player.OnItemGranted += HandleItemGranted;
    }

    // 방장이 지급을 확정한 아이템을 표시용 데이터로 바꿔 View에 넘긴다
    private void HandleItemGranted(int itemId, int amount)
    {
        view.ShowPickup(CreateToastData(itemId, amount));
    }

    // 아이템 ID로 재료 → 포션 순으로 데이터를 찾아 이름·아이콘·분류를 채운다
    private static PickupToastData CreateToastData(int itemId, int amount)
    {
        MaterialData material = MaterialDatabase.Instance.Find(itemId);
        if (material != null)
            return new PickupToastData(material.ItemName, amount, material.Icon, PickupCategory.Material);

        PotionData potion = PotionDatabase.Instance.Find(itemId);
        if (potion != null)
            return new PickupToastData(potion.ItemName, amount, potion.Icon, PickupCategory.Potion);

        Debug.LogWarning($"[PickupUIPresenter] DB에 없는 아이템 Id : {itemId}");
        return new PickupToastData($"아이템 {itemId}", amount, null, PickupCategory.Material);
    }

    // Model 구독을 해제한다
    public void Dispose()
    {
        player.OnItemGranted -= HandleItemGranted;
    }
}
