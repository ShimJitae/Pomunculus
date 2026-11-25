// 바닥에 깔려 일정 시간 동안 범위 안 대상에게 주기적으로 효과를 준다
using System.Collections.Generic;
using UnityEngine;

public class PotionZone : MonoBehaviour
{
    private readonly Collider[] hitBuffer = new Collider[16];
    private readonly HashSet<IDamageable> zoneTargets = new HashSet<IDamageable>();

    private IReadOnlyList<PotionEffect> tickEffects;
    private float radius;
    private float tickInterval;
    private float nextTickTime;
    private LayerMask hitLayers;
    private GameObject thrower;
    private bool isBurst;                  // 지연 폭발: 한 번 터지고 사라진다
    private GameObject burstEffectPrefab;
    private float burstEffectLifetime;

    [Header("범위 표시")]
    [Tooltip("장판 범위 테두리. Loop 켜고 Use World Space는 끈다")]
    [SerializeField] private LineRenderer rangeCircle;
    [SerializeField] private int circleSegmentCount = 40;
    [SerializeField] private float circleHeightOffset = 0.05f;   // 바닥에 묻히지 않게 살짝 띄움

    // 장판 설정을 받고 duration 뒤에 사라지도록 예약한다
    public void Initialize(float radius, float duration, float tickInterval,
        IReadOnlyList<PotionEffect> tickEffects, GameObject thrower, LayerMask hitLayers)
    {
        this.radius = radius;
        this.tickInterval = tickInterval;
        this.tickEffects = tickEffects;
        this.thrower = thrower;
        this.hitLayers = hitLayers;
        nextTickTime = Time.time;   // 깔리자마자 1회
        Destroy(gameObject, duration);
        DrawRangeCircle();
    }

    /// <summary>
    /// 지연 폭발로 쓴다. 범위 원을 바로 그려 경고로 보여 주고, delay 뒤 한 번만 효과를 준 뒤
    /// 터지는 이펙트(선택)를 남기고 사라진다.
    /// </summary>
    public void InitializeBurst(float radius, float delay, IReadOnlyList<PotionEffect> burstEffects,
        GameObject burstEffectPrefab, float burstEffectLifetime, GameObject thrower, LayerMask hitLayers)
    {
        this.radius = radius;
        tickEffects = burstEffects;
        this.burstEffectPrefab = burstEffectPrefab;
        this.burstEffectLifetime = burstEffectLifetime;
        this.thrower = thrower;
        this.hitLayers = hitLayers;
        isBurst = true;
        nextTickTime = Time.time + delay;
        DrawRangeCircle();
    }

    void Update()
    {
        if (tickEffects == null || Time.time < nextTickTime) return;
        nextTickTime = Time.time + tickInterval;
        ApplyTick();

        if (isBurst) Burst();
    }

    // 지연 폭발이 터진 뒤 이펙트를 남기고 장판을 없앤다
    private void Burst()
    {
        tickEffects = null;
        if (burstEffectPrefab != null)
        {
            GameObject effect = Instantiate(burstEffectPrefab, transform.position, Quaternion.identity);
            Destroy(effect, burstEffectLifetime);
        }
        Destroy(gameObject);
    }

    // 범위 안 대상을 모아 결정권이 있는 대상에게만 효과를 준다
    private void ApplyTick()
    {
        zoneTargets.Clear();
        int count = Physics.OverlapSphereNonAlloc(transform.position, radius, hitBuffer, hitLayers);
        for (int i = 0; i < count; i++)
        {
            IDamageable target = hitBuffer[i].GetComponentInParent<IDamageable>();
            if (target != null) zoneTargets.Add(target);
        }

        foreach (IDamageable target in zoneTargets)
        {
            Entity entity = target as Entity;
            if (!PotionTargeting.HasAuthority(entity)) continue;

            PotionTargetFlags relation = PotionTargeting.GetRelation(entity, thrower);
            PotionHitContext context = new PotionHitContext(entity, target, transform.position, thrower);
            foreach (PotionEffect effect in tickEffects)
            {
                if (effect != null && effect.CanAffect(relation))
                    effect.Apply(context);
            }
        }
    }

    // 실제 판정 반경과 같은 크기로 범위 원을 그린다
    private void DrawRangeCircle()
    {
        if (rangeCircle == null) return;

        rangeCircle.useWorldSpace = false;   // 장판 위치 기준 로컬 좌표로 그린다
        rangeCircle.loop = true;
        rangeCircle.positionCount = circleSegmentCount;
        for (int i = 0; i < circleSegmentCount; i++)
        {
            float angle = 2f * Mathf.PI * i / circleSegmentCount;
            rangeCircle.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, circleHeightOffset, Mathf.Sin(angle) * radius));
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}