using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ZonePotionEffect : PotionEffect
{
    // 조준 원이 장판 크기를 보여 주도록 장판 반경을 알려 준다
    

    [SerializeField] private PotionZone zonePrefab;
    [Min(0.1f)][SerializeField] private float radius = 2f;
    public override float AreaRadius => radius;
    [Min(0.1f)][SerializeField] private float duration = 4f;
    [Min(0.1f)][SerializeField] private float tickInterval = 0.5f;

    // 직렬화 클래스 생성자에서는 LayerMask.GetMask를 쓸 수 없어 인덱스로 둔다. 6 = Ground
    [Tooltip("장판을 붙일 바닥 레이어")]
    [SerializeField] private LayerMask groundLayers = 1 << 6;

    private const float GroundProbeHeight = 0.5f;     // 착탄 지점보다 이만큼 위에서 아래로 쏜다 (바닥에 닿은 경우 대비)
    private const float GroundSearchDistance = 5f;    // 착탄 지점 아래로 바닥을 찾을 최대 거리


    [Tooltip("장판 안 대상에게 틱마다 줄 효과")]
    [PotionEffectPicker(allowZone: false)]
    [SerializeReference] private List<PotionEffect> tickEffects = new List<PotionEffect>();

    // 착탄 지점에 장판을 하나 깐다
    public override void OnImpact(in PotionImpactContext context)
    {
        if (zonePrefab == null) return;
        Vector3 spawnPoint = FindGroundPoint(context.ImpactPoint);
        PotionZone zone = UnityEngine.Object.Instantiate(zonePrefab, spawnPoint, Quaternion.identity);
        zone.Initialize(radius, duration, tickInterval, tickEffects, context.Thrower, context.HitLayers);
    }

    // 착탄 지점 바로 아래 바닥 위치. 바닥을 못 찾으면 착탄 지점 그대로
    private Vector3 FindGroundPoint(Vector3 impactPoint)
    {
        Vector3 origin = impactPoint + Vector3.up * GroundProbeHeight;
        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, GroundProbeHeight + GroundSearchDistance,
                groundLayers, QueryTriggerInteraction.Ignore))
            return hit.point;

        return impactPoint;
    }

    public override void Apply(in PotionHitContext context) { }
}