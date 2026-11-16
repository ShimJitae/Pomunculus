using Photon.Pun;
using System;
using System.Collections;
using Unity.Behavior;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(BehaviorGraphAgent)), RequireComponent(typeof(NavMeshAgent)), RequireComponent(typeof(EnemySkill)), RequireComponent(typeof(EnemyMovement))]
public class Enemy : Entity, IWorldRPC, IHealable
{
    public BehaviorGraphAgent BehaviorGraphAgent => behaviorGraphAgent;
    public GameObject Target => target;
    public bool CanDoAttack => !isDie && CanAttack;
    public EnemyDataSO Data => EnemySo;

    public event Action<int, int> OnHpChanged; // (현재HP / 최대HP)

    public int MaxHp => so != null ? so.GetMaxHp() : 0;

    [SerializeField] private BehaviorGraphAgent behaviorGraphAgent;
    [SerializeField] private GameObject target;
    [SerializeField] private EnemyDetector detector;
    [SerializeField] private float knockbackDistance;

    private bool IsDamageBlock => IsInvincible || CheckDie();
    private NavMeshAgent agent;
    private Animator animator;
    private EnemyMovement movement;
    private EnemySkill enemySkill;

    private float groggyHpRatio = 0.5f;
    private bool hasGroggyTriggered;

    protected EnemyDataSO EnemySo
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
        movement = GetComponent<EnemyMovement>();
        enemySkill = GetComponentInChildren<EnemySkill>();
    }

    // 적의 행동 그래프와 전투 상태를 초기화하고 실행 권한에 따라 감지와 공격 준비를 수행
    protected override void OnEnable()
    {
        if(EnemySo == null)
        {
            Debug.Log("SO 데이터가 연결되어있지 않습니다.");
            gameObject.SetActive(false);
            return;
        }
        movement.InitSpawnCenter(transform.position);
        
        behaviorGraphAgent.Graph = EnemySo.GetBehaviorGraph();
        behaviorGraphAgent.SetVariableValue("SpawnPosition", movement.SpawnCenter);

        base.OnEnable();
        OnHpChanged?.Invoke(CurrentHp, MaxHp);

        ApplyAuthority(IsMaster);

        hasGroggyTriggered = false;
        behaviorGraphAgent.SetVariableValue("IsGroggy", false);

        enemySkill.PrewarmParticles();

        // 방장만 범위 감지를 함, 공격 패턴 로드
        if (!IsMaster)
            return;

        detector.SetRadius(EnemySo.GetDetectRange());
        detector.OnPlayersChanged += RefreshTarget;

        enemySkill.Initialize();
    }

    protected override void OnDisable()
    {
        base.OnDisable();

        detector.OnPlayersChanged -= RefreshTarget;

        enemySkill.CancelAttack();
    }

    //private void Start()
    //{
    //    float attackRange = enemySO.GetAttackRange();
    //    float bodyRadius = agent.radius;   // Navigate가 더하는 몸 반지름
    //    float chaseMargin = 0.2f;                                  // 경계 오차 여유

    //    behaviorGraphAgent.SetVariableValue("AttackRange", attackRange);
    //    behaviorGraphAgent.SetVariableValue("ChaseDistance", Mathf.Max(0.1f, attackRange - bodyRadius - chaseMargin));
    //}

    // 실행 권한이 있는 경우 피해 배율을 적용하고 HP 동기화 및ㅊ 사망, 그로기 판정을 수행
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
        else
        {
            TryEnterGroggy();
        }
    }

    public void Heal(int amount)
    {
        if (!IsMaster || amount <= 0)
            return;

        CurrentHp += amount;
        OnHpChanged?.Invoke(CurrentHp, MaxHp);
        SendHpSync();
    }

    [ContextMenu("HP 회복")]
    public void Heal10()
    {
        Heal(10);
    }

    // 활성화 후 최초로 체력 비율이 기준 이하가 되면 그로기 상태가 되어 공격이 끊기고 피격 모션 재생
    private void TryEnterGroggy()
    {
        if (hasGroggyTriggered || CurrentHp > MaxHp * groggyHpRatio)
            return;

        hasGroggyTriggered = true;
        behaviorGraphAgent.SetVariableValue("IsGroggy", true);
        CancelCurrentAttack();
    }

    // 넉백을 포함한 takeDamage
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

    // 상태 이상에 변화가 생기면 호출됨
    protected override void OnStatusModifiersChanged(StatusModifiers modifiers)
    {
        base.OnStatusModifiersChanged(modifiers);


        float currentMoveSpeed = CanMove ? EnemySo.GetSpeed() * MoveSpeedRatio : 0f;

        behaviorGraphAgent.SetVariableValue("MoveSpeed", currentMoveSpeed);
        agent.speed = currentMoveSpeed; 
        // 기절 0 / 구속 1 / 그외 >> 이동 배율대로
        float animationSpeedRatio = !CanAttack ? 0f : (CanMove ? MoveSpeedRatio : 1f);
        animator.SetFloat("MoveAnimSpeed", animationSpeedRatio);

        bool isStunned = !CanAttack;
        behaviorGraphAgent.SetVariableValue("IsStunned", isStunned);

        if (isStunned && IsMaster && enemySkill.IsAttackActive)
            CancelCurrentAttack();
    }

    // 죽었을 시 아이템 스폰 and 적 사라지기 (하던 행동 끊김)
    protected override void OnDie()
    {
        behaviorGraphAgent.SetVariableValue<bool>("IsDie", true);
        CancelCurrentAttack();

        // 동기화 시 이 부분 삭제 필요
        DataDBManager.Instance.SpawnDropItem(GetId(), transform.position);

        if (IsMaster)
        {
            StartCoroutine(WaitDieAnimation());
        }
    }

    // 몬스터 죽고 1초 대기
    private IEnumerator WaitDieAnimation()
    {
        yield return new WaitForSeconds(1f);

        WorldManager.Instance.Despawn(ID);
    }

    // 타겟 참조와 BG의 Target 및 HasTarget 값을 함께 갱신
    public void SetTarget(GameObject player)
    {
        target = player;
        behaviorGraphAgent.SetVariableValue<GameObject>("Target", player);
        behaviorGraphAgent.SetVariableValue("HasTarget", player != null);
    }

    // 기존 타겟이 유효하면 유지, 유효하지 않을 때만 감지 범위의 가장 가까운 플레이어를 선택(A를 타겟으로 잡고 있다가 
    //  >> 타겟이 멀어지거나 죽어서 사라지면 다시 범위 내의 플레이어를 타겟으로 설정
    public void RefreshTarget()
    {
        if (target != null && target.activeInHierarchy && detector.Contains(target))
            return;

        SetTarget(detector.GetNearestPlayer());
    }

    protected override int GetId()
    {
        return EnemySo.GetEnemyId();
    }

    public override void SaveData(SaveData saveData)
    {
        SaveData enemyHpData = new SaveData();
        enemyHpData.SetData(CurrentHp);
        saveData.Child["enemyHp"] = enemyHpData;

        base.SaveData(saveData);
    }

    // 데이터 로드
    public override void LoadData(SaveData _data)
    {
        base.LoadData(_data);

        if (!_data.Child.TryGetValue("enemyHp", out SaveData enemyHpData))
            return;

        ApplyHp(enemyHpData.GetData<int>());
    }

    // 마스터가 넘어갔을 시 그 마스터에게만 동작 권한 넘기기용 함수
    // 실행 권한에 따라 BG, agent, detector 오브젝트를 켜거나 끈다.
    private void ApplyAuthority(bool isAuthority)
    {
        behaviorGraphAgent.enabled = isAuthority;
        agent.enabled = isAuthority;
        detector.gameObject.SetActive(isAuthority);
    }

    #region 네트워크
    // 방 밖에서는 로컬 실행을 허용하고, 방 안에서는 마스터 클라이언트만 실행 권한을 가진다.
    private bool IsMaster => !PhotonNetwork.InRoom || PhotonNetwork.IsMasterClient;

    private enum EnemyRPCType : byte
    {
        SyncHp,           // 방장 > 다른 플레이어 : 확정된 HP
        ShowTelegraph,    // 방장 > 다른 플레이어 : 공격 범위 표시
        CancelTelegraph,  // 방장 > 다른 플레이어 : 공격 취소 (그로기, 사망)
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

    // EnemySkill.BeginAttack에서 호출 (방장만)
    public void SendShowTelegraph(int patternId, Vector3 origin, float yaw)
    {
        if (!IsMaster)
            return;

        PacketData packet = new();
        packet.Write((byte)EnemyRPCType.ShowTelegraph);
        packet.Write(patternId);
        packet.Write(origin);
        packet.Write(yaw);
        this.SendRPC(packet, WorldRPCTarget.Others);
    }

    // 그로기,사망 시 진행 중인 공격 취소. 방장이면 다른 플레이어에게도 전달
    private void CancelCurrentAttack()
    {
        enemySkill.CancelAttack();

        if (!IsMaster)
            return;

        PacketData packet = new();
        packet.Write((byte)EnemyRPCType.CancelTelegraph);
        this.SendRPC(packet, WorldRPCTarget.Others);
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
            case EnemyRPCType.ShowTelegraph:
                ReceiveShowTelegraph(packet);
                break;

            case EnemyRPCType.CancelTelegraph:
                enemySkill.CancelAttack();
                break;
        }
    }

    // 피격 장판 생성 RPC Receive
    private void ReceiveShowTelegraph(PacketData packet)
    {
        int patternId = packet.ReadInt();
        Vector3 origin = packet.ReadVector3();
        float yaw = packet.ReadFloat();

        EnemyPatternDataSO patternData = DataDBManager.Instance.PatternDatabase.TryGetDataSO(patternId);

        if (patternData == null)
            return;

        enemySkill.ShowRemoteTelegraph(patternData, origin, Quaternion.Euler(0f, yaw, 0f));
    }

    // 방장이 확정한 HP나 저장된 HP를 적용 (드롭·파괴는 하지 않음)
    private void ApplyHp(int hp)
    {
        CurrentHp = hp;
        isDie = CurrentHp <= 0;

        if (CurrentHp <= MaxHp * groggyHpRatio)
            hasGroggyTriggered = true;

        behaviorGraphAgent.SetVariableValue("IsDie", isDie);

        if (isDie)
            enemySkill.CancelAttack();

        OnHpChanged?.Invoke(CurrentHp, MaxHp);
    }
    #endregion
}
