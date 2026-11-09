using Photon.Pun;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent (typeof(Enemy))]
public class EnemySkill : MonoBehaviour
{
    // 패턴 선택 순간에 고정한 공격 기준값 (범위 표시와 판정이 함께 사용)
    public EnemyPatternDataSO CurrentAttackPattern { get; private set; }
    public Vector3 AttackOrigin { get; private set; }
    public Quaternion AttackRotation { get; private set; }

    [SerializeField] private LayerMask hitLayers;      // Player 레이어만 체크
    [SerializeField] private float hitHeight = 2f;     // Rectangle 판정 높이 (공통)
    [SerializeField] private EnemySkillPattern skillPattern = new();   // Enemy에서 이동

    private readonly Collider[] hitColliders = new Collider[16];
    private readonly HashSet<IDamageable> alreadyHitTargets = new();

    private Enemy enemy;
    private NavMeshAgent agent;
    private EnemyAttackIndicator attackIndicator;


    private void Awake()
    {
        CacheComponents();
    }

    // Enemy.OnEnable 마지막에 호출 (방장만). BG 그래프 지정 후라 사거리 설정이 가능
    public void Initialize()
    {
        CacheComponents();

        IReadOnlyList<EnemyPatternDataSO> enemyPatterns = DataDBManager.Instance.PatternDatabase
            .GetPatternsByEnemyId(enemy.Data.GetEnemyId());

        skillPattern.LoadPatternList(enemyPatterns, enemy.Data.GetNormalAttackPattern());
        CurrentAttackPattern = null;

        ApplyNextPatternRange();
    }

    private void CacheComponents()
    {
        if (enemy == null)
            enemy = GetComponent<Enemy>();

        if (agent == null)
            agent = GetComponent<NavMeshAgent>();

        if (attackIndicator == null)
            attackIndicator = GetComponentInChildren<EnemyAttackIndicator>();
    }

    // BG SelectAttackPattern 노드에서 호출 (방장만). 실패하면 null
    public EnemyPatternDataSO BeginAttack()
    {
        if (!enemy.CanDoAttack)
            return null;

        EnemyPatternDataSO currentPattern = skillPattern.UseSkill();

        if (currentPattern == null)
            return null;

        ApplyNextPatternRange();   // 미리 뽑힌 다음 패턴 기준으로 사거리 갱신

        // 표시와 판정이 함께 쓸 기준값 고정 (LookAt 1회 직후라 방향이 확정된 상태)
        GameObject attackTarget = enemy.Target;
        bool isTargetAnchor = currentPattern.AttackArea.anchor == AttackAreaAnchor.Target && attackTarget != null;

        CurrentAttackPattern = currentPattern;
        AttackOrigin = isTargetAnchor ? attackTarget.transform.position : transform.position;
        AttackRotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);

        ShowTelegraph();
        enemy.SendShowTelegraph(currentPattern.PatternId, AttackOrigin, AttackRotation.eulerAngles.y);

        return currentPattern;
    }

    // 그로기·사망 시 진행 중인 공격 취소 (로컬 처리만. 전송은 Enemy가 담당)
    public void CancelAttack()
    {
        CurrentAttackPattern = null;

        if (attackIndicator != null)
            attackIndicator.Hide();
    }

    // 다른 클라이언트: 방장이 보낸 공격 정보로 표시
    public void ShowRemoteTelegraph(EnemyPatternDataSO patternData, Vector3 origin, Quaternion rotation)
    {
        CurrentAttackPattern = patternData;
        AttackOrigin = origin;
        AttackRotation = rotation;

        ShowTelegraph();
    }

    private void ShowTelegraph()
    {
        if (attackIndicator == null || CurrentAttackPattern.AttackArea.shape == AttackAreaShape.None)
            return;

        attackIndicator.Show(CurrentAttackPattern.AttackArea, AttackOrigin, AttackRotation, CurrentAttackPattern.Telegraph);
    }

    // 미리 정해진 다음 패턴의 사용 사거리로 BG 사거리를 맞춤
    private void ApplyNextPatternRange()
    {
        EnemyPatternDataSO nextPattern = skillPattern.NextPattern;

        float useRange = nextPattern != null && nextPattern.UseRange > 0f
            ? nextPattern.UseRange
            : enemy.Data.GetAttackRange();

        // 인식 범위를 넘으면 의미가 없으므로 제한 (큰 값 = 인식 범위 전체)
        float attackRange = Mathf.Min(useRange, enemy.Data.GetDetectRange());
        float chaseMargin = 0.2f;   // 경계 오차 여유

        enemy.BehaviorGraphAgent.SetVariableValue("AttackRange", attackRange);
        enemy.BehaviorGraphAgent.SetVariableValue("ChaseDistance", Mathf.Max(0.1f, attackRange - agent.radius - chaseMargin));
    }

    // 애니메이션 이벤트에서 호출 (Function: OnAttackHit, Float: 피해 배율 %)
    public void OnAttackHit(float damagePercent)
    {
        // Animator는 모든 클라이언트에서 재생되므로, 표시 숨김은 판정 권한과 상관없이 모두 처리
        if (attackIndicator != null)
            attackIndicator.Hide();

        if (PhotonNetwork.InRoom && !PhotonNetwork.IsMasterClient)
            return;

        if (!enemy.CanDoAttack)
            return;

        if (CurrentAttackPattern == null || CurrentAttackPattern.AttackArea.shape == AttackAreaShape.None)
            return;

        AttackAreaData area = CurrentAttackPattern.AttackArea;
        Vector3 attackForward = AttackRotation * Vector3.forward;
        Vector3 areaCenter = AttackOrigin + AttackRotation * area.offset;

        int hitCount = OverlapArea(area, areaCenter, attackForward);
        int damage = Mathf.RoundToInt(enemy.Data.GetAtk() * damagePercent / 100f);
        alreadyHitTargets.Clear();

        for (int index = 0; index < hitCount; index++)
        {
            IDamageable damageTarget = hitColliders[index].GetComponentInParent<IDamageable>();
            if (damageTarget is not Component targetComponent) continue;          // 못 찾음
            if (targetComponent.gameObject == gameObject) continue;               // 자기 자신 제외
            if (!alreadyHitTargets.Add(damageTarget)) continue;                   // 중복 타격 방지

            if (area.shape == AttackAreaShape.Sector)
            {
                Vector3 directionToTarget = targetComponent.transform.position - areaCenter;
                directionToTarget.y = 0f;

                if (Vector3.Angle(attackForward, directionToTarget) > area.angle * 0.5f)
                    continue;
            }

            damageTarget.TakeDamage(damage, AttackOrigin);
        }
    }

    private int OverlapArea(AttackAreaData area, Vector3 areaCenter, Vector3 attackForward)
    {
        if (area.shape == AttackAreaShape.Rectangle)
        {
            Vector3 boxCenter = areaCenter + attackForward * (area.length * 0.5f);
            Vector3 halfExtents = new Vector3(area.width * 0.5f, hitHeight * 0.5f, area.length * 0.5f);

            return Physics.OverlapBoxNonAlloc(boxCenter, halfExtents, hitColliders, AttackRotation, hitLayers, QueryTriggerInteraction.Ignore);
        }

        return Physics.OverlapSphereNonAlloc(areaCenter, area.radius, hitColliders, hitLayers, QueryTriggerInteraction.Ignore);
    }

#if UNITY_EDITOR
    // 플레이 중 선택하면 마지막 공격의 판정 범위를 표시
    private void OnDrawGizmosSelected()
    {
        if (CurrentAttackPattern == null)
            return;

        AttackAreaData area = CurrentAttackPattern.AttackArea;
        Vector3 attackForward = AttackRotation * Vector3.forward;
        Vector3 areaCenter = AttackOrigin + AttackRotation * area.offset;

        Gizmos.color = Color.red;

        switch (area.shape)
        {
            case AttackAreaShape.Circle:
                Gizmos.DrawWireSphere(areaCenter, area.radius);
                break;

            case AttackAreaShape.Sector:
                Gizmos.DrawWireSphere(areaCenter, area.radius);
                Vector3 leftEdge = Quaternion.Euler(0f, -area.angle * 0.5f, 0f) * attackForward;
                Vector3 rightEdge = Quaternion.Euler(0f, area.angle * 0.5f, 0f) * attackForward;
                Gizmos.DrawLine(areaCenter, areaCenter + leftEdge * area.radius);
                Gizmos.DrawLine(areaCenter, areaCenter + rightEdge * area.radius);
                break;

            case AttackAreaShape.Rectangle:
                Gizmos.matrix = Matrix4x4.TRS(areaCenter + attackForward * (area.length * 0.5f), AttackRotation, Vector3.one);
                Gizmos.DrawWireCube(Vector3.zero, new Vector3(area.width, hitHeight, area.length));
                break;
        }
    }
#endif
}
