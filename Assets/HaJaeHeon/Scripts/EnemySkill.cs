using Photon.Pun;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(Enemy))]
public class EnemySkill : MonoBehaviour
{
    // 패턴 선택 순간에 고정한 공격 기준값 (범위 표시와 판정이 함께 사용)
    public EnemyPatternDataSO CurrentAttackPattern { get; private set; }
    public Vector3 AttackOrigin { get; private set; }
    public Quaternion AttackRotation { get; private set; }
    public bool IsAttackActive => isAttackActive;


    [SerializeField] private LayerMask hitLayers;      // Player 레이어만 체크
    [SerializeField] private float hitHeight = 2f;     // Rectangle 판정 높이 (공통)
    [SerializeField] private EnemySkillPattern skillPattern = new();   // Enemy에서 이동

    
    private readonly Collider[] hitColliders = new Collider[16]; // 범위 검색 결과를 재사용하는 버퍼이며 한 번에 최대 16개의 콜라이더를 받는다. 추후 변경 할 수 도 있음
    private readonly HashSet<IDamageable> alreadyHitTargets = new(); // 하나의 대상에 여러 콜라이더가 있어도 한 번의 판정에서는 피해를 한 번만 적용한다.
    private bool isAttackActive;
    private float hitAcceptStartTime; // 이 시각 전의 판정 이벤트는 이전 공격 모션의 잔여 이벤트로 보고 무시
    private readonly Dictionary<GameObject, List<GameObject>> spawnPools = new();   // 프리팹별 소환 인스턴스 (꺼진 것을 재사용)

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

        SetAttackActive(false);

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

        // 표시와 판정이 함께 쓸 기준값 고정 (회전 노드 직후라 방향이 확정된 상태)
        GameObject attackTarget = enemy.Target;
        bool isTargetAnchor = currentPattern.AttackArea.anchor == AttackAreaAnchor.Target && attackTarget != null;

        if (isAttackActive)
            Debug.Log($"{name} >> 이전 공격의 OnAttackHit 이벤트 없음, 애니메이션 이벤트 확인 필요");

        SetAttackActive(true);

        if(agent.enabled && agent.isOnNavMesh)
        {
            agent.ResetPath();
            agent.velocity = Vector3.zero;
        }

        hitAcceptStartTime = Time.time + currentPattern.Telegraph;

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

        if (isAttackActive)
            SetAttackActive(false);

        if (attackIndicator != null)
            attackIndicator.Hide();
    }

    // 다른 클라이언트: 방장이 보낸 공격 정보로 표시
    public void ShowRemoteTelegraph(EnemyPatternDataSO patternData, Vector3 origin, Quaternion rotation)
    {
        CurrentAttackPattern = patternData;
        AttackOrigin = origin;
        AttackRotation = rotation;

        hitAcceptStartTime = Time.time + patternData.Telegraph;

        ShowTelegraph();
    }

    private void ShowTelegraph()
    {
        PlayPatternSound(CurrentAttackPattern.TelegraphSound, transform.position); // 기 모으는 몬스터 위치에서 소리 재생

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

    // 공격 진해 여부를 로컬 상태와 ㅎBG에 함께 반영한다.
    private void SetAttackActive(bool isActive)
    {
        isAttackActive = isActive;
        enemy.BehaviorGraphAgent.SetVariableValue("IsAttackActive", isActive);
    }

    // 범위 표시와 같은 중심 위치 (바닥 높이)
    private Vector3 GetAttackAreaCenter()
    {
        Vector3 areaOffset = CurrentAttackPattern.AttackArea.offset;
        Vector3 flatOffset = new Vector3(areaOffset.x, 0f, areaOffset.z);

        return AttackOrigin + AttackRotation * flatOffset;
    }

    // 범위 크기에 맞춘 소환 스케일. 소환 프리팹은 반지름 1(직사각형은 폭 1·길이 1, +Z 방향) 기준으로 만든다
    private Vector3 GetAttackAreaScale()
    {
        AttackAreaData area = CurrentAttackPattern.AttackArea;

        switch (area.shape)
        {
            case AttackAreaShape.Circle:
            case AttackAreaShape.Sector:
                return area.radius > 0f ? Vector3.one * area.radius : Vector3.one;

            case AttackAreaShape.Rectangle:
                return new Vector3(Mathf.Max(area.width, 0.01f), 1f, Mathf.Max(area.length, 0.01f));

            default:
                return CurrentAttackPattern.SkillParticle.transform.localScale;   // 범위 없음: 프리팹 원래 크기
        }
    }

    // 스킬 파티클 소환: 범위 중심에 패턴별 위치·회전 보정을 더함 (보정은 공격 방향 기준)
    private void SpawnSkillParticle()
    {
        EnemyPatternDataSO attackPattern = CurrentAttackPattern;

        Vector3 spawnPosition = GetAttackAreaCenter() + AttackRotation * attackPattern.SkillParticleOffset;
        Quaternion spawnRotation = AttackRotation * Quaternion.Euler(attackPattern.SkillParticleRotation);
        Vector3 spawnScale = attackPattern.ScaleSkillParticleWithArea
            ? GetAttackAreaScale()
            : attackPattern.SkillParticle.transform.localScale;

        SkillParticlePool.Instance.Spawn(attackPattern.SkillParticle, spawnPosition, spawnRotation, spawnScale);
    }


    // 애니메이션 이벤트에서 호출 (Function: OnAttackHit, Float: 피해 배율 %)
    public void OnAttackHit(float damagePercent)
    {
        // 예고 중에 들어온 이벤트 = 기절로 멈췄다가 다시 재생된 이전 공격 모션의 것 → 표시·판정 모두 건드리지 않음
        if (Time.time < hitAcceptStartTime)
            return;

        // Animator는 모든 클라이언트에서 재생되므로, 표시 숨김은 판정 권한과 상관없이 모두 처리
        if (attackIndicator != null)
            attackIndicator.Hide();

        // 연출 처리 이후 > 방 안에서는 마스터만 공격 판정을 진행한다.
        if (CurrentAttackPattern != null && CurrentAttackPattern.SkillParticle != null)
            SpawnSkillParticle();

        if (CurrentAttackPattern != null)
            PlayPatternSound(CurrentAttackPattern.AttackSound, GetAttackAreaCenter()); // 타격,투사체는 공격 범위 위치에서 소리 재생

        if (PhotonNetwork.InRoom && !PhotonNetwork.IsMasterClient || !isAttackActive)
            return;

        SetAttackActive(false);

        if (!enemy.CanDoAttack || CurrentAttackPattern == null || CurrentAttackPattern.AttackArea.shape == AttackAreaShape.None)
            return;

        AttackAreaData area = CurrentAttackPattern.AttackArea;
        Vector3 attackForward = AttackRotation * Vector3.forward;
        Vector3 areaCenter = AttackOrigin + AttackRotation * area.offset;

        int hitCount = OverlapArea(area, areaCenter, attackForward);
        int damage = Mathf.RoundToInt( CurrentAttackPattern.GetDamageValue() * damagePercent / 100f);
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

            // 맞은 대상 위치에 피격 파티클 (판정이 방장에서만 일어나므로 현재는 방장 화면에만 보임)
            if (CurrentAttackPattern.HitParticle != null)
                SkillParticlePool.Instance.Spawn(CurrentAttackPattern.HitParticle, targetComponent.transform.position, AttackRotation, CurrentAttackPattern.HitParticle.transform.localScale);
        }
    }

    // 사각형은 박스로, 원과 부채꼴은 구로 후보를 수집하며 부채꼴 각도는 호출부에서 추가 검사한다.
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

    // 전용 패턴과 기본 공격에서 사용하는 공격, 피격 파티클을 풀에 미리 준비한다.
    public void PrewarmParticles()
    {
        CacheComponents();

        IReadOnlyList<EnemyPatternDataSO> enemyPatterns = DataDBManager.Instance.PatternDatabase.GetPatternsByEnemyId(enemy.Data.GetEnemyId());

        foreach (EnemyPatternDataSO patternData in enemyPatterns)
        {
            PrewarmPattern(patternData);
        }

        PrewarmPattern(enemy.Data.GetNormalAttackPattern());
    }

    private void PrewarmPattern(EnemyPatternDataSO patternData)
    {
        if (patternData == null)
            return;

        SkillParticlePool.Instance.Prewarm(patternData.SkillParticle);
        SkillParticlePool.Instance.Prewarm(patternData.HitParticle);
    }

    private void PlayPatternSound(AudioSO patternSound, Vector3 soundPosition)
    {
        if (patternSound == null || AudioManager.Instance == null)
            return;

        AudioManager.Instance.PlaySound(patternSound, soundPosition);
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
