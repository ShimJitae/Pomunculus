// 착탄 시 대상과 상관없이 한 번 넘기는 정보
using UnityEngine;

public readonly struct PotionImpactContext
{
    public readonly Vector3 ImpactPoint;
    public readonly GameObject Thrower;
    public readonly LayerMask HitLayers;

    public PotionImpactContext(Vector3 impactPoint, GameObject thrower, LayerMask hitLayers)
    {
        ImpactPoint = impactPoint;
        Thrower = thrower;
        HitLayers = hitLayers;
    }
}