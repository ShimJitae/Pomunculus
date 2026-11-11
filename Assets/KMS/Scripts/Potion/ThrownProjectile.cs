using Photon.Pun;
using Pomunculus.Items;
using System.Collections.Generic;
using UnityEngine;

// 포물선으로 날아가 무언가에 닿으면 주변에 피해를 주고 사라지는 포션.
[RequireComponent(typeof(Rigidbody))]
public class ThrownProjectile : MonoBehaviour
{
    [Tooltip("착탄 지점 주변 피해 반경. 0이면 직접 맞은 대상에게만 피해")]
    public float ImpactRadius = 1.5f;
    public float MaxLifeTime = 5f;
    [Tooltip("착탄 지점에 생성할 이펙트")]
    [SerializeField] private GameObject impactEffectPrefab;

    [Tooltip("생성한 착탄 이펙트 유지시간")]
    [SerializeField] private float impactEffectLifeTime = 1f;

    private Rigidbody rb;
    private int fallbackDamage;          // 효과가 없는 포션(빈 병)의 기본 피해
    private LayerMask hitLayers;
    private GameObject thrower;
    private PotionData potion;
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

    // 미리보기용: 충돌 구의 반지름과, 피벗에서 구 중심까지의 오프셋(회전 없는 상태 기준)
    public bool TryGetCollisionSphere(out Vector3 centerOffset, out float radius)
    {
        SphereCollider sphere = GetComponentInChildren<SphereCollider>();
        if (sphere == null)
        {
            centerOffset = Vector3.zero;
            radius = 0f;
            return false;
        }

        Vector3 scale = sphere.transform.lossyScale;
        radius = sphere.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
        centerOffset = sphere.transform.TransformPoint(sphere.center) - transform.position;
        return true;
    }

    // 초기 속도와 이 투사체에만 적용할 중력을 받아 발사
    public void Launch(Vector3 velocity, Vector3 gravity, LayerMask hitLayers, GameObject thrower, PotionData potion, int fallbackDamage)
    {
        this.hitLayers = hitLayers;
        this.gravity = gravity;
        this.thrower = thrower;
        this.potion = potion;
        this.fallbackDamage = fallbackDamage;

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
        CollectTargets(point, directHit);

        foreach (IDamageable damageable in hitTargets)
            ApplyEffects(damageable, point);

        // 착탄 이펙트 생성 후 재생이 끝날 시간에 맞춰 삭제 (기존 그대로)
        if (impactEffectPrefab != null)
        {
            GameObject impactEffect = Instantiate(impactEffectPrefab, point, Quaternion.identity);
            Destroy(impactEffect, impactEffectLifeTime);
        }

        Destroy(gameObject);
    }

    // 직접 맞은 대상과 착탄 반경 안의 대상을 중복 없이 모은다
    void CollectTargets(Vector3 point, Collider directHit)
    {
        hitTargets.Clear();

        if (IsInHitLayer(directHit.gameObject))
        {
            IDamageable direct = directHit.GetComponentInParent<IDamageable>();
            if (direct != null) hitTargets.Add(direct);
        }

        if (ImpactRadius <= 0f) return;

        int count = Physics.OverlapSphereNonAlloc(point, ImpactRadius, hitBuffer, hitLayers);
        for (int i = 0; i < count; i++)
        {
            IDamageable target = hitBuffer[i].GetComponentInParent<IDamageable>();
            if (target != null) hitTargets.Add(target);
        }
    }

    // 대상과의 관계에 맞는 효과만 순서대로 적용한다. 효과가 없으면 적에게 기본 피해
    void ApplyEffects(IDamageable damageable, Vector3 point)
    {
        Entity entity = damageable as Entity;
        if (!HasAuthority(entity)) return;   // 결정권자가 아니면 연출만 하고 효과는 건너뛴다
        PotionTargetFlags relation = GetRelation(entity);

        if (potion == null || potion.Effects.Count == 0)
        {
            if (relation == PotionTargetFlags.Enemy)
                damageable.TakeDamage(fallbackDamage, point);
            return;
        }

        PotionHitContext context = new PotionHitContext(entity, damageable, point, thrower);
        foreach (PotionEffect effect in potion.Effects)
        {
            if (effect != null && effect.CanAffect(relation))
                effect.Apply(context);
        }
    }

    // 이 클라이언트가 대상의 결정권자인지 확인한다 (적 = 방장, 플레이어 = 본인)
    bool HasAuthority(Entity entity)
    {
        if (!PhotonNetwork.InRoom) return true;
        if (entity is PlayerFSM player) return player.IsMine;
        return PhotonNetwork.IsMasterClient;
    }

    // 플레이어(던진 사람 포함)는 아군, 그 외는 적으로 본다
    PotionTargetFlags GetRelation(Entity entity)
        => entity is PlayerFSM ? PotionTargetFlags.Ally : PotionTargetFlags.Enemy;

    bool IsInHitLayer(GameObject go) => (hitLayers.value & (1 << go.layer)) != 0;

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, ImpactRadius);
    }
}
