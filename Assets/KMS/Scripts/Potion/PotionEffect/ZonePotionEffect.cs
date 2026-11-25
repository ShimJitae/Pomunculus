using System;
using System.Collections.Generic;
using UnityEngine;

// 착탄 지점에 장판을 깔아 범위 안 대상에게 주기적으로 효과를 준다
[Serializable]
public class ZonePotionEffect : SpawnPotionEffect
{
    [SerializeField] private PotionZone zonePrefab;
    [Min(0.1f)][SerializeField] private float radius = 2f;
    [Min(0.1f)][SerializeField] private float duration = 4f;
    [Min(0.1f)][SerializeField] private float tickInterval = 0.5f;

    [Tooltip("장판 안 대상에게 틱마다 줄 효과")]
    [PotionEffectPicker(allowSpawn: false)]
    [SerializeReference] private List<PotionEffect> tickEffects = new List<PotionEffect>();

    // 조준 원이 장판 크기를 보여 주도록 장판 반경을 알려 준다
    public override float AreaRadius => radius;

    public IReadOnlyList<PotionEffect> TickEffects => tickEffects;

    // 착탄 지점에 장판을 하나 깐다
    public override void OnImpact(in PotionImpactContext context)
    {
        if (zonePrefab == null) return;
        Vector3 spawnPoint = FindGroundPoint(context.ImpactPoint);
        PotionZone zone = UnityEngine.Object.Instantiate(zonePrefab, spawnPoint, Quaternion.identity);
        zone.Initialize(radius, duration, tickInterval, tickEffects, context.Thrower, context.HitLayers);
    }
}
