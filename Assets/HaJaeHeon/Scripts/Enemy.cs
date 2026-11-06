using System;
using Unity.Behavior;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(BehaviorGraphAgent)), RequireComponent(typeof(NavMeshAgent))]
public class Enemy : Entity
{
    public BehaviorGraphAgent BehaviorGraphAgent => behaviorGraphAgent;
    public GameObject Target => target;
    public bool CanDoAttack => !isDie && CanAttack;
    public EnemyDataSO Data => enemySO;

    public event Action<int, int> OnHpChanged; // (현재HP / 최대HP)

    public int MaxHp => so != null ? so.GetMaxHp() : 0;

    public int PresentHp
    {
        get { return currentHp; }
        private set
        {
            currentHp = Mathf.Clamp(value, 0, MaxHp);
            OnHpChanged?.Invoke(currentHp, MaxHp);
        }
    }

    [SerializeField] private BehaviorGraphAgent behaviorGraphAgent;
    [SerializeField] private GameObject target;
    [SerializeField] private EnemyDetector detector;

    protected EnemyDataSO enemySO
    {
        get { return so as EnemyDataSO; }
        private set { so = value; }
    }

    protected override void Awake()
    {
        behaviorGraphAgent = GetComponent<BehaviorGraphAgent>();
        detector = GetComponentInChildren<EnemyDetector>();
    }


    protected override void OnEnable()
    {
        if(enemySO == null)
        {
            Debug.Log("SO 데이터가 연결되어있지 않습니다.");
            gameObject.SetActive(false);
            return;
        }

        behaviorGraphAgent.Graph = enemySO.GetBehaviorGraph();
        
        CurrentHp = enemySO.GetHp();
        moveSpeed = enemySO.GetSpeed();
        
        isDie = CurrentHp <= 0;
        detector.SetRadius(enemySO.GetDetectRange());
        detector.OnPlayersChanged += RefreshTarget;
    }

    private void OnDisable()
    {
        detector.OnPlayersChanged -= RefreshTarget;
    }

    private void Start()
    {
        float attackRange = enemySO.GetAttackRange();
        float bodyRadius = GetComponent<NavMeshAgent>().radius;   // Navigate가 더하는 몸 반지름
        float chaseMargin = 0.2f;                                  // 경계 오차 여유

        behaviorGraphAgent.SetVariableValue("MoveSpeed", enemySO.GetSpeed() * MoveSpeedRatio);
        behaviorGraphAgent.SetVariableValue("AttackRange", attackRange);
        behaviorGraphAgent.SetVariableValue("ChaseDistance", Mathf.Max(0.1f, attackRange - bodyRadius - chaseMargin));
    }

    protected override void OnDie()
    {
        behaviorGraphAgent.SetVariableValue<bool>("IsDie", true);

        DataDBManager.Instance.SpawnDropItem(GetId(), transform.position);

        Destroy(gameObject, 1f);
    }

    public void SetTarget(GameObject player)
    {
        target = player;
        behaviorGraphAgent.SetVariableValue<GameObject>("Target", player);
        behaviorGraphAgent.SetVariableValue("HasTarget", player != null);
    }

    public void RefreshTarget()
    {
        if (target != null && target.activeInHierarchy && detector.Contains(target))
            return;

        SetTarget(detector.GetNearestPlayer());
    }

    protected override int GetId()
    {
        return enemySO.GetEnemyId();
    }
}
