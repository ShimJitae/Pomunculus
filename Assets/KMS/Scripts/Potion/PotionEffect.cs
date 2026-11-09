// 모든 포션 효과의 기반. PotionData 안에 SerializeReference로 저장된다
using System;
using UnityEngine;

[Serializable]
public abstract class PotionEffect
{
    [Tooltip("이 효과가 적용될 대상")]
    [SerializeField] private PotionTargetFlags affectedTargets = PotionTargetFlags.Enemy;

    // 이 대상 관계에 효과를 적용할 수 있는지 확인한다
    public bool CanAffect(PotionTargetFlags relation) => (affectedTargets & relation) != 0;

    // 대상 하나에 효과를 적용한다
    public abstract void Apply(in PotionHitContext context);
}