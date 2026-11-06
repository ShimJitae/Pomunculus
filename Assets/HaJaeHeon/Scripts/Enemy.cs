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

    [SerializeField] private BehaviorGraphAgent behaviorGraphAgent;
    [SerializeField] private GameObject target;
    [SerializeField] private EnemyDetector detector;
    [SerializeField] private float knockbackDistance;

    private bool IsDamageBlock => IsInvincible || CheckDie();
    private NavMeshAgent agent;
    private Animator animator;

    protected EnemyDataSO enemySO
    {
        get { return so as EnemyDataSO; }
        private set { so = value; }
    }

    protected override void Awake()
    {
        base.Awake();

        behaviorGraphAgent = GetComponent<BehaviorGraphAgent>();
        detector = GetComponentInChildren<EnemyDetector>();
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
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

        base.OnEnable();
        
        detector.SetRadius(enemySO.GetDetectRange());
        detector.OnPlayersChanged += RefreshTarget;

        OnHpChanged?.Invoke(CurrentHp, MaxHp);
    }

    protected override void OnDisable()
    {
        base.OnDisable();

        detector.OnPlayersChanged -= RefreshTarget;
    }

    private void Start()
    {
        float attackRange = enemySO.GetAttackRange();
        float bodyRadius = agent.radius;   // Navigate가 더하는 몸 반지름
        float chaseMargin = 0.2f;                                  // 경계 오차 여유

        behaviorGraphAgent.SetVariableValue("AttackRange", attackRange);
        behaviorGraphAgent.SetVariableValue("ChaseDistance", Mathf.Max(0.1f, attackRange - bodyRadius - chaseMargin));
    }

    public override void TakeDamage(int damage)
    {
        if (IsDamageBlock || damage <= 0)
            return;

        CurrentHp -=  Mathf.CeilToInt(damage * DamageTakenRatio);
        OnHpChanged?.Invoke(CurrentHp, MaxHp);

        if (CurrentHp <= 0)
        {
            isDie = true;

            OnDie();
        }
    }

    //public override void TakeDamage(int damage, Vector3 sourcePosition)
    //{
    //    if (isDamageBlock || damage <= 0)
    //        return;

    //    CurrentHp -= damage;

    //    if (CurrentHp <= 0)
    //    {
    //        isDie = true;

    //        if (TryGetComponent<ItemDropper>(out ItemDropper dropper))
    //            dropper.DropItems();

    //        OnDie();
    //    }
    //
    //    OnHpChanged?.Invoke(currentHp, MaxHp);

    //    transform.position += (transform.position - sourcePosition) * knockbackDistance;
    //}

    protected override void OnStatusModifiersChanged(StatusModifiers modifiers)
    {
        base.OnStatusModifiersChanged(modifiers);


        float currentMoveSpeed = CanMove ? enemySO.GetSpeed() * MoveSpeedRatio : 0f;

        behaviorGraphAgent.SetVariableValue("MoveSpeed", currentMoveSpeed);
        agent.speed = currentMoveSpeed;
        animator.SetFloat("MoveAnimSpeed", CanMove ? MoveSpeedRatio : 0f);
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

    public override void SaveData(SaveData _data)
    {
        base.SaveData(_data);
    }

    public override void LoadData(SaveData _data)
    {
        base.LoadData(_data);
    }
}
