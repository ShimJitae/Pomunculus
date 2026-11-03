using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 포물선으로 날아가 무언가에 닿으면 주변에 피해를 주고 사라지는 투사체.
/// 프리팹 구성: Rigidbody + Collider + 이 스크립트.
/// 중력은 Launch에서 받은 값을 직접 적용하므로 Rigidbody의 Use Gravity는 자동으로 꺼진다.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class ThrownProjectile : MonoBehaviour
{
    [Tooltip("착탄 지점 주변 피해 반경. 0이면 직접 맞은 대상에게만 피해")]
    public float ImpactRadius = 1.5f;
    public float MaxLifeTime = 5f;
    public GameObject ImpactEffectPrefab;

    private Rigidbody rb;
    private int damage;
    private LayerMask damageLayer;
    private Vector3 gravity;
    private bool hasImpacted;

    private readonly Collider[] hitBuffer = new Collider[16];
    private readonly HashSet<IDamageable> hitTargets = new HashSet<IDamageable>();

    [Header("회전 (던질 때 한 번만 랜덤으로 정함)")]
    public Vector2 spinSpeedRange = new Vector2(540f, 900f);
    [Range(0f, 1f)] public float spinRandomness = 0.3f;
    public bool randomSpinDirection = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;   // 투사체마다 다른 중력을 FixedUpdate에서 직접 적용
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;   // 빠른 투사체 관통 방지

    }

    // 궤적 모양을 유지하도록 계산된 중력을 매 물리 프레임 적용
    void FixedUpdate()
    {
        rb.AddForce(gravity, ForceMode.Acceleration);
    }

    // 초기 속도와 이 투사체에만 적용할 중력을 받아 발사
    public void Launch(Vector3 velocity, Vector3 gravity, int damage, LayerMask damageLayer, GameObject thrower)
    {
        this.damage = damage;
        this.damageLayer = damageLayer;
        this.gravity = gravity;

        // 던진 사람 몸에 바로 부딪히지 않도록 충돌 무시
        Collider[] myColliders = GetComponentsInChildren<Collider>();
        foreach (Collider other in thrower.GetComponentsInChildren<Collider>())
            foreach (Collider mine in myColliders)
                Physics.IgnoreCollision(mine, other);

        rb.linearVelocity = velocity;

        ApplyRandomSpin(velocity);

        Destroy(gameObject, MaxLifeTime);   // 맵 밖으로 떨어진 경우 대비
    }

    void ApplyRandomSpin(Vector3 velocity)
    {
        Vector3 tumbleAxis = Vector3.Cross(Vector3.up, velocity);
        tumbleAxis = tumbleAxis.sqrMagnitude > 0.0001f
            ? tumbleAxis.normalized
            : Random.onUnitSphere;

        Vector3 axis = Vector3.Slerp(tumbleAxis, Random.onUnitSphere, spinRandomness).normalized;
        if (randomSpinDirection && Random.value < 0.5f) axis = -axis;

        float speedRad = Random.Range(spinSpeedRange.x, spinSpeedRange.y) * Mathf.Deg2Rad;

        // 기본 최대 회전 속도(7 rad/s ≈ 초당 400도)에 막히지 않도록 한도를 올림
        rb.maxAngularVelocity = Mathf.Max(rb.maxAngularVelocity, speedRad);
        rb.angularVelocity = axis * speedRad;
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
        if (ImpactRadius > 0f)
        {
            int count = Physics.OverlapSphereNonAlloc(point, ImpactRadius, hitBuffer, damageLayer);
            for (int i = 0; i < count; i++)
            {
                var target = hitBuffer[i].GetComponentInParent<IDamageable>();
                if (target != null) hitTargets.Add(target);
            }
        }

        foreach (IDamageable target in hitTargets)
            target.TakeDamage(damage, point);   // 넉백 방향은 착탄 지점 기준

        if (ImpactEffectPrefab != null)
            Instantiate(ImpactEffectPrefab, point, Quaternion.identity);

        Destroy(gameObject);
    }

    bool IsInDamageLayer(GameObject go) => (damageLayer.value & (1 << go.layer)) != 0;

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, ImpactRadius);
    }
}
