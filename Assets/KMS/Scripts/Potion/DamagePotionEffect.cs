using System;
using UnityEngine;

[Serializable]
public class DamagePotionEffect : PotionEffect
{
    [Min(0)]
    [SerializeField] private int amount = 5;

    // 착탄 지점 기준으로 피해를 준다 (넉백 방향도 착탄 지점 기준)
    public override void Apply(in PotionHitContext context)
    {
        if (amount <= 0) return;
        context.Damageable?.TakeDamage(amount, context.ImpactPoint);
    }
}

