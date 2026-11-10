using System;
using UnityEngine;

[Serializable]
public class StatusPotionEffect : PotionEffect
{
    [Tooltip("걸 상태이상. 지속시간·틱 피해는 StatusEffectData에서 정한다")]
    [SerializeField] private StatusEffectType statusType = StatusEffectType.Burn;

    // 기존 상태이상 시스템으로 상태를 건다
    public override void Apply(in PotionHitContext context)
    {
        Entity target = context.Target;
        if (target == null || target.CheckDie()) return;

        if (target.TryGetComponent(out StatusEffectController controller))
            controller.RegisterStatusEffect(statusType);
    }
}
