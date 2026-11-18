using UnityEngine;

public class ShieldPotionEffect : PotionEffect
{
    [Min(0)]
    [SerializeField] private int amount = 30;
    [Min(0.1f)]
    [SerializeField] private float duration = 10f;
    // 보호막을 받을 수 있는 대상에게만 보호막을 준다
    public override void Apply(in PotionHitContext context)
    {
        if (context.Target is IShieldable shieldable)
            shieldable.GetShield(amount, duration);
    }
}
