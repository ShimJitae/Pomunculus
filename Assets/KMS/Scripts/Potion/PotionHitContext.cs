// 효과 하나에 넘기는 착탄 정보
using UnityEngine;

public readonly struct PotionHitContext
{
    public readonly Entity Target;            // Entity가 아닌 대상(Health 등)이면 null
    public readonly IDamageable Damageable;
    public readonly Vector3 ImpactPoint;
    public readonly GameObject Thrower;

    public PotionHitContext(Entity target, IDamageable damageable, Vector3 impactPoint, GameObject thrower)
    {
        Target = target;
        Damageable = damageable;
        ImpactPoint = impactPoint;
        Thrower = thrower;
    }
}