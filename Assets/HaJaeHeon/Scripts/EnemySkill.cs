using Photon.Pun;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent (typeof(Enemy))]
public class EnemySkill : MonoBehaviour
{
    [SerializeField] private LayerMask hitLayers;          // Player 레이어만 체크
    [SerializeField] private float hitRadius = 1.5f;
    [SerializeField] private float hitForwardOffset = 1f;  // 몸 앞쪽에 판정 중심
    [SerializeField] private float hitAboveOffset = 1f; // 몸 위 쪽  판정 중심
    [SerializeField] private float hitAngle = 120f;

    private readonly Collider[] hitColliders = new Collider[16];
    private readonly HashSet<IDamageable> alreadyHitTargets = new();

    private Enemy enemy;


    private void Awake()
    {
        enemy = GetComponent<Enemy>();
    }
    // 애니메이션 이벤트에서 호출 (Function 이름: OnAttackHit)
    // Int 파라미터로 패턴별 배율 등을 넘길 수 있음 (Attack1=100, Attack3=150 처럼 %)
    public void OnAttackHit(float damagePercent)
    {
        if (PhotonNetwork.InRoom && !PhotonNetwork.IsMasterClient)
            return;

        if (!enemy.CanDoAttack) return;

        int hitCount = Physics.OverlapSphereNonAlloc(
            GetHitCenter(), hitRadius, hitColliders, hitLayers, QueryTriggerInteraction.Ignore);

        int damage = Mathf.RoundToInt(enemy.Data.GetAtk() * damagePercent / 100f);
        alreadyHitTargets.Clear();

        for (int index = 0; index < hitCount; index++)
        {
            IDamageable damageTarget = hitColliders[index].GetComponentInParent<IDamageable>();
            if (damageTarget is not Component targetComponent) continue;          // 못 찾음
            if (targetComponent.gameObject == gameObject) continue;               // 자기 자신 제외
            if (!alreadyHitTargets.Add(damageTarget)) continue;                   // 중복 타격 방지

            Vector3 directionToTarget = targetComponent.transform.position - transform.position;
            directionToTarget.y = 0f;
            if (Vector3.Angle(transform.forward, directionToTarget) > hitAngle * 0.5f) continue;

            damageTarget.TakeDamage(damage, transform.position);
        }
    }

    private Vector3 GetHitCenter()
    {
        Vector3 hitCenter = transform.position + (transform.forward * hitForwardOffset) + (transform.up * hitAboveOffset);

        return hitCenter;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()   // 판정 범위 확인용
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(GetHitCenter(), hitRadius);
    }
#endif
}
