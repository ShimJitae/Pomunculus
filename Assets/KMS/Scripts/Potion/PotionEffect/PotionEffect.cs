// 모든 포션 효과의 기반. PotionData 안에 SerializeReference로 저장된다
using System;
using UnityEngine;

[Serializable]
public abstract class PotionEffect
{
    [Tooltip("이 효과가 적용될 대상 (Ally = 다른 플레이어, Self = 던진 사람, Gatherable = 채집물)")]
    [SerializeField] private PotionTargetFlags affectedTargets = PotionTargetFlags.Enemy;
    // 조준 원에 표시할 효과 범위. 범위가 없는 효과는 0
    public virtual float AreaRadius => 0f;

    // 이 대상 관계에 효과를 적용할 수 있는지 확인한다
    public bool CanAffect(PotionTargetFlags relation) => (affectedTargets & relation) != 0;

    // 대상 하나에 효과를 적용한다
    public abstract void Apply(in PotionHitContext context);

    // 착탄 시 대상과 상관없이 모든 클라이언트에서 한 번 호출된다 (장판 생성 등)
    public virtual void OnImpact(in PotionImpactContext context) { }
}