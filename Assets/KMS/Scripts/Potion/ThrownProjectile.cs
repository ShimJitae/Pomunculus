using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 포물선으로 날아가 무언가에 닿으면 주변에 피해를 주고 사라지는 투사체.
/// 프리팹 구성: Rigidbody(Use Gravity 켜기) + Collider + 이 스크립트
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class ThrownProjectile : MonoBehaviour
{
    [Tooltip("착탄 지점 주변 피해 반경. 0이면 직접 맞은 대상에게만 피해")]
    public float impactRadius = 1.5f;
    public float maxLifeTime = 5f;
    public GameObject impactEffectPrefab;

    Rigidbody rb;
    int damage;
    LayerMask damageLayer;
    bool hasImpacted;

    readonly Collider[] hitBuffer = new Collider[16];
    readonly HashSet<IDamageable> hitTargets = new HashSet<IDamageable>();

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = true;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;   // 빠른 투사체 관통 방지
    }

    public void Launch(Vector3 velocity, int damage, LayerMask damageLayer, GameObject thrower)
    {
        this.damage = damage;
        this.damageLayer = damageLayer;

        // 던진 사람 몸에 바로 부딪히지 않도록 충돌 무시
        Collider[] myColliders = GetComponentsInChildren<Collider>();
        foreach (Collider other in thrower.GetComponentsInChildren<Collider>())
            foreach (Collider mine in myColliders)
                Physics.IgnoreCollision(mine, other);

        rb.linearVelocity = velocity;

        Destroy(gameObject, maxLifeTime);   // 맵 밖으로 떨어진 경우 대비
    }

    void OnCollisionEnter(Collision collision)
    {
        if (hasImpacted) return;
        hasImpacted = true;

        Vector3 point = collision.contactCount > 0 ? collision.GetContact(0).point : transform.position;
        Impact(point, collision.collider);
    }

    void Impact(Vector3 point, Collider directHit)
    {
        hitTargets.Clear();

        // 직접 맞은 대상
        if (IsInDamageLayer(directHit.gameObject))
        {
            var direct = directHit.GetComponentInParent<IDamageable>();
            if (direct != null) hitTargets.Add(direct);
        }

        // 범위 피해
        if (impactRadius > 0f)
        {
            int count = Physics.OverlapSphereNonAlloc(point, impactRadius, hitBuffer, damageLayer);
            for (int i = 0; i < count; i++)
            {
                var target = hitBuffer[i].GetComponentInParent<IDamageable>();
                if (target != null) hitTargets.Add(target);
            }
        }

        foreach (IDamageable target in hitTargets)
            target.TakeDamage(damage, point);   // 넉백 방향은 착탄 지점 기준

        if (impactEffectPrefab != null)
            Instantiate(impactEffectPrefab, point, Quaternion.identity);

        Destroy(gameObject);
    }

    bool IsInDamageLayer(GameObject go) => (damageLayer.value & (1 << go.layer)) != 0;

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, impactRadius);
    }
}