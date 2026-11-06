using Photon.Pun;
using System;
using Unity.Behavior;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(BehaviorGraphAgent)), RequireComponent(typeof(NavMeshAgent))]
public class Enemy : Entity, IWorldRPC
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
        if (!IsMaster)
            return;

        if (IsDamageBlock || damage <= 0)
            return;

        CurrentHp -=  Mathf.CeilToInt(damage * DamageTakenRatio);
        OnHpChanged?.Invoke(CurrentHp, MaxHp);
        SendHpSync();

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

    //    transform.position += (transform.position - sourcePosition).norm * knockbackDistance;
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
        
        if(!IsMaster)
            WorldManager.Instance.Despawn(ID);
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
        SaveData enemyHpData = new SaveData();
        enemyHpData.SetData(CurrentHp);
        _data.Child["enemyHp"] = enemyHpData;

        base.SaveData(_data);
    }

    public override void LoadData(SaveData _data)
    {
        base.LoadData(_data);

        if (!_data.Child.TryGetValue("enemyHp", out SaveData enemyHpData))
            return;

        ApplyHp(enemyHpData.GetData<int>());
    }

    #region 네트워크
    private bool IsMaster => !PhotonNetwork.InRoom || PhotonNetwork.IsMasterClient;

    private enum EnemyRPCType : byte
    {
        SyncHp, // 방장 > 다른 플레이어 : 확정된 HP
    }


    // 방장이 확정한 HP를 다른 플레이어에게 전송
    private void SendHpSync()
    {
        if (!IsMaster)
            return;

        PacketData packet = new();
        packet.Write((byte)EnemyRPCType.SyncHp);
        packet.Write(CurrentHp);
        this.SendRPC(packet, WorldRPCTarget.Others);   // 확장 메서드라 this. 필요. 방장은 이미 적용했으므로 나머지에게만
    }

    // WorldManager가 ID로 찾아서 호출
    public void ReceiveRPC(PacketData packet)
    {
        EnemyRPCType rpcType = (EnemyRPCType)packet.ReadByte();

        switch (rpcType)
        {
            case EnemyRPCType.SyncHp:
                ApplyHp(packet.ReadInt());
                break;
        }
    }

    // 방장이 확정한 HP나 저장된 HP를 적용 (드롭·파괴는 하지 않음)
    private void ApplyHp(int hp)
    {
        CurrentHp = hp;
        isDie = CurrentHp <= 0;

        behaviorGraphAgent.SetVariableValue("IsDie", isDie);
        OnHpChanged?.Invoke(CurrentHp, MaxHp);
    }
    #endregion
}
