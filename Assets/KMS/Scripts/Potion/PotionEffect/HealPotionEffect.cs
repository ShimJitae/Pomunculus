using System;
using UnityEngine;

[Serializable]
public class HealPotionEffect : PotionEffect
{
    [Min(0)]
    [SerializeField] private int amount = 30;

    // 회복 가능한 대상만 회복시킨다
    public override void Apply(in PotionHitContext context)
    {
        if (context.Target is IHealable healable)
            healable.Heal(amount);
    }
}
