// 착탄 지점에 경고 원을 띄우고, 일정 시간 뒤 범위 안 대상에게 효과를 한 번 준다 (번개 등)
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class DelayedBurstPotionEffect : SpawnPotionEffect
{
    [Tooltip("경고 원을 그릴 장판 프리팹 (PotionZone + 범위 원 LineRenderer)")]
    [SerializeField] private PotionZone markerPrefab;
    [Min(0.1f)][SerializeField] private float radius = 3f;
    [Tooltip("착탄 후 터지기까지 걸리는 시간")]
    [Min(0f)][SerializeField] private float delay = 1f;
    [Tooltip("터질 때 생성할 이펙트 (선택)")]
    [SerializeField] private GameObject burstEffectPrefab;
    [Min(0.1f)][SerializeField] private float burstEffectLifetime = 2f;

    [Tooltip("터질 때 범위 안 대상에게 줄 효과")]
    [PotionEffectPicker(allowSpawn: false)]
    [SerializeReference] private List<PotionEffect> burstEffects = new List<PotionEffect>();

    // 조준 원이 터지는 범위를 보여 주도록 반경을 알려 준다
    public override float AreaRadius => radius;

    // 모든 화면에서 경고 원을 깔고, 효과는 각 대상의 결정권자 화면에서만 적용된다
    public override void OnImpact(in PotionImpactContext context)
    {
        if (markerPrefab == null) return;
        Vector3 spawnPoint = FindGroundPoint(context.ImpactPoint);
        PotionZone marker = UnityEngine.Object.Instantiate(markerPrefab, spawnPoint, Quaternion.identity);
        marker.InitializeBurst(radius, delay, burstEffects, burstEffectPrefab, burstEffectLifetime,
            context.Thrower, context.HitLayers);
    }
}
