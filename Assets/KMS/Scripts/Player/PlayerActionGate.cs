using Pomunculus.Items;
using UnityEngine;

/// <summary>
/// 공격(던지기)·회피를 지금 실행할 수 있는지 판정하고, 실행이 확정되면 입력과 스태미나를 소비한다.
/// 상태이상·스태미나·쿨다운·입력 버퍼·장착 포션을 함께 확인한다.
/// </summary>
public class PlayerActionGate
{
    private readonly PlayerFSM player;
    private float lastDodgeAt = float.NegativeInfinity;

    public Vector3 AttackTarget { get; private set; }     // ConsumeAttack 성공 시 설정
    public PotionData AttackPotion { get; private set; }  // null이면 빈 병

    public PlayerActionGate(PlayerFSM player) => this.player = player;

    // 지금 던지면 나갈 포션. 선택한 포션이 인벤토리에 없으면 null(빈 병)
    public PotionData GetThrowablePotion()
    {
        PotionData potion = player.Loadout.SelectedPotion;
        return potion != null && player.Inventory.GetItemCount(potion.Id) > 0 ? potion : null;
    }

    // 던질 수 있으면 목표·포션을 확정하고 스태미나를 쓴다
    public bool ConsumeAttack()
    {
        if (!player.CanAttack || !player.Stats.CanUseStamina) return false;
        if (!player.Input.ConsumeAttack(out Vector3 target)) return false;

        AttackPotion = GetThrowablePotion();   // 이미 수량 확인을 거친 값이라 다시 확인하지 않는다
        AttackTarget = player.Thrower.ClampToThrowRange(target);
        player.Stats.SpendStamina(player.Data.ThrowStaminaCost);
        return true;
    }

    // 쿨다운 중이면 입력을 소비하지 않고 버퍼에 남겨 둔다
    public bool ConsumeDodge()
    {
        if (!player.CanDodge || !player.Stats.CanUseStamina) return false;
        if (Time.time - lastDodgeAt < player.Tuning.DodgeCooldown) return false;
        if (!player.Input.ConsumeDodge()) return false;

        player.Stats.SpendStamina(player.Data.DodgeStaminaCost);
        return true;
    }

    // 회피가 실제로 시작된 시점을 기록해 쿨다운을 잰다
    public void MarkDodgeUsed() => lastDodgeAt = Time.time;
}
