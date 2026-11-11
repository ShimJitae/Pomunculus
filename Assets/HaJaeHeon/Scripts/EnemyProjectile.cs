using Photon.Pun;
using UnityEngine;

// 스킬 파티클 프리팹 안에서 앞으로 날아가다 처음 닿은 플레이어에게 피해를 주는 투사체.
// EnemySkill이 스킬 파티클을 켤 때마다(풀 재사용 포함) OnEnable에서 출발 위치부터 다시 날아간다.
// 이동·판정은 모든 클라이언트에서 같이 돌고, 피해는 방장만 준다.
public class EnemyProjectile : MonoBehaviour
{
    [Header("이동")]
    [SerializeField] private Vector3 localMoveAxis = Vector3.forward;  // 프리팹 기준 진행 방향 (Vefects 이펙트는 +X)
    [SerializeField, Min(0f)] private float moveSpeed = 25f;
    [SerializeField, Min(0f)] private float maxDistance = 8f;          // 패턴 SO 공격 범위 length와 맞춘다

    [Header("판정")]
    [SerializeField, Min(0f)] private float hitRadius = 0.5f;          // 패턴 SO 공격 범위 width의 절반과 맞춘다
    [SerializeField, Min(0)] private int damage = 10;
    [SerializeField] private LayerMask hitLayers;                      // 피해 대상 (Player)
    [SerializeField] private LayerMask blockLayers;                    // 닿으면 피해 없이 터지는 레이어 (벽)

    [Header("이펙트")]
    [SerializeField] private GameObject projectileVisual;              // 날아가는 모델 (이 오브젝트의 자식). 맞으면 끄고 발사 때 켠다
    [SerializeField] private ParticleSystem projectileEffect;          // 날아가는 파티클 (선택)
    [SerializeField] private ParticleSystem impactEffect;              // 터지는 이펙트 (이 오브젝트 밖, 루트의 자식)

    private readonly RaycastHit[] targetHits = new RaycastHit[8];

    private Vector3 startLocalPosition;
    private Vector3 moveDirection;
    private Vector3 sourcePosition;
    private float remainingDistance;
    private bool isFlying;

    private void Awake()
    {
        startLocalPosition = transform.localPosition;
    }

    // 스킬 파티클이 켜질 때마다 출발 위치에서 다시 발사
    private void OnEnable()
    {
        transform.localPosition = startLocalPosition;
        moveDirection = transform.TransformDirection(localMoveAxis).normalized;
        sourcePosition = transform.position;
        remainingDistance = maxDistance;
        isFlying = true;

        if (projectileVisual != null)
            projectileVisual.SetActive(true);
    }

    private void Update()
    {
        if (!isFlying)
            return;

        Vector3 currentPosition = transform.position;
        float stepDistance = Mathf.Min(moveSpeed * Time.deltaTime, remainingDistance);

        // 벽은 가는 선으로 검사 (판정 반경이 커도 바닥에 걸리지 않게)
        float blockDistance = float.MaxValue;
        if (blockLayers != 0 && Physics.Raycast(currentPosition, moveDirection, out RaycastHit blockHit,
                stepDistance, blockLayers, QueryTriggerInteraction.Ignore))
        {
            blockDistance = blockHit.distance;
        }

        // 플레이어는 이번 프레임 이동 구간 전체를 구로 쓸어서 검사 (빨라도 건너뛰지 않음)
        IDamageable hitTarget = FindNearestTarget(currentPosition, stepDistance, out float targetDistance);

        if (hitTarget != null && targetDistance <= blockDistance)
        {
            if (!PhotonNetwork.InRoom || PhotonNetwork.IsMasterClient)
                hitTarget.TakeDamage(damage, sourcePosition);

            Finish(currentPosition + moveDirection * targetDistance);
            return;
        }

        if (blockDistance < float.MaxValue)
        {
            Finish(currentPosition + moveDirection * blockDistance);
            return;
        }

        transform.position = currentPosition + moveDirection * stepDistance;
        remainingDistance -= stepDistance;

        if (remainingDistance <= 0f)
            Finish(transform.position);
    }

    private IDamageable FindNearestTarget(Vector3 startPosition, float stepDistance, out float nearestDistance)
    {
        nearestDistance = float.MaxValue;
        IDamageable nearestTarget = null;

        int hitCount = Physics.SphereCastNonAlloc(startPosition, hitRadius, moveDirection, targetHits,
            stepDistance, hitLayers, QueryTriggerInteraction.Ignore);

        for (int index = 0; index < hitCount; index++)
        {
            RaycastHit targetHit = targetHits[index];
            if (targetHit.distance >= nearestDistance) continue;

            IDamageable damageTarget = targetHit.collider.GetComponentInParent<IDamageable>();
            if (damageTarget == null) continue;

            nearestTarget = damageTarget;
            nearestDistance = targetHit.distance;
        }

        return nearestTarget;
    }

    // 모델·파티클을 지우고 그 자리에서 터짐. 루트는 파티클이 끝나면 Stop Action(Disable)으로 꺼진다
    private void Finish(Vector3 impactPosition)
    {
        isFlying = false;
        transform.position = impactPosition;

        if (projectileVisual != null)
            projectileVisual.SetActive(false);

        if (projectileEffect != null)
            projectileEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        if (impactEffect != null)
        {
            impactEffect.transform.position = impactPosition;
            impactEffect.Play(true);
        }
    }
}
