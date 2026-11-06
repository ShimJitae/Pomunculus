using Photon.Pun;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 클릭 이동 플레이어 (FSM Owner)
///  - 우클릭 : 이동 (누르고 있으면 커서를 따라감). 적을 클릭해도 이동만 한다.
///  - 좌클릭 : 클릭한 지면 위치로 물체를 던짐
///  - Space  : 커서 방향으로 회피
/// 입력·이동·던지기·체력은 각 컴포넌트가 담당하고, 이 클래스는 FSM 구동과 상태 간 공유 데이터만 맡는다.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(PlayerInputReader), typeof(PlayerMotor), typeof(PlayerThrower))]
public class PlayerFSM : Entity, IPlayerObj
{
    [SerializeField] private PlayerTuningSO tuning;
    public PlayerTuningSO Tuning => tuning;
    public PlayerDataSO Data => so as PlayerDataSO;

    [Header("위치")]
    [SerializeField] private Transform spawnPos;
    private Vector3 startPos;
    [Header("디버그")]
    [SerializeField] private PlayerStateType currentStateView;

    public static readonly int LocomotionHash = Animator.StringToHash("Locomotion");
    public static readonly int AttackHash = Animator.StringToHash("Attack");
    public static readonly int DodgeHash = Animator.StringToHash("Dodge");
    public static readonly int HitHash = Animator.StringToHash("Hit");
    public static readonly int DieHash = Animator.StringToHash("Die");
    public static readonly int SpeedParam = Animator.StringToHash("Speed");

    private StateMachine<PlayerStateType, PlayerFSM> fsm;
    private Animator anim;

    // ───── 네트워크 시 사용하는 컴포넌트 ─────
    private WorldTransformSync worldTransform;
    private WorldAnimatorSync worldAnimator;

    // ───── 상태들이 사용하는 컴포넌트 ─────
    public PlayerStats Stats { get; private set; }
    public PlayerInputReader Input { get; private set; }
    public PlayerMotor Motor { get; private set; }
    public PlayerThrower Thrower { get; private set; }
    public PlayerInteractor Interactor { get; private set; }   // 없을 수 있음 (null 허용)

    // ───── 상태들이 읽어 가는 데이터 ─────
    public PlayerStateType CurrentStateType => fsm.CurrentStateType;
    public PlayerStateType PreviousStateType => fsm.PreviousStateType;
    public bool IsDead => CheckDie();
    public Vector3 LastHitSourcePosition { get; private set; }
    public Vector3 AttackTarget { get; private set; }   // ConsumeAttack 성공 시 설정

    private float lastDodgeAt = float.NegativeInfinity;

    private bool IsDamageBlocked =>
    IsInvincible || CheckDie() ||
    (fsm.CurrentState is PlayerStateBase state && state.IsInvincible(this));

    public int Owner { get; set; }
    public bool IsMine => !PhotonNetwork.InRoom ||
                      (PhotonNetwork.LocalPlayer != null && PhotonNetwork.LocalPlayer.ActorNumber == Owner);
    protected override void Awake()
    {
        base.Awake();

        if (Data == null) Debug.LogError("[PlayerFSM] so 슬롯에 PlayerDataSO가 필요합니다.", this);
        if (tuning == null) Debug.LogError("[PlayerFSM] tuning 슬롯이 비어 있습니다.", this);

        worldTransform = GetComponent<WorldTransformSync>();
        worldAnimator = GetComponent<WorldAnimatorSync>();

        anim = GetComponentInChildren<Animator>();
        Input = GetComponent<PlayerInputReader>();
        Motor = GetComponent<PlayerMotor>();
        Thrower = GetComponent<PlayerThrower>();
        Interactor = GetComponent<PlayerInteractor>();

        Motor.Initialize(Tuning, Data.GetSpeed());
        Stats = new PlayerStats(this, Data);
        startPos = transform.position;

        fsm = new StateMachine<PlayerStateType, PlayerFSM>(this);
        fsm.AddState(new PlayerIdleState());
        fsm.AddState(new PlayerMoveState());
        fsm.AddState(new PlayerAttackState());
        fsm.AddState(new PlayerDodgeState());
        fsm.AddState(new PlayerHitState());
        fsm.AddState(new PlayerDeadState());
        fsm.ValidateAllRegistered();
    }


    // ══════════════════GameManager 관리 ═══════════════════════
    // 오브젝트 소환 (Awake 초기화)를 여기서
    // id -> 월드 매니저쪽에서 지정해주는 아이디
    // prefabId -> 프리팹 이름이 곧 아이디
    public override void InitWorldObj(int id, string prefabId)
    {
        //ID = id;
        //PrefabId = prefabId;
        SetOwner(Owner);
    }

    // ActorNumber 가져오기
    public void SetOwner(int actor)
    {
        Owner = actor;
        if (worldTransform != null) worldTransform.IsOwner = () => IsMine;
        if (worldAnimator != null) worldAnimator.IsOwner = () => IsMine;
        Motor.SetAgentEnabled(IsMine);

        // TODO 다른 플레이어의 포션색상 다르게 하기?
        //if (TryGetComponent(out Renderer r)) r.material.color = IsMine ? Color.green : Color.red;
    }


    // 데이터 저장 및 불어오기 위치값은 굳이 안넣어도 됨 이미 알아하는중
    // 데이터 저장
    // 상시 저장 알아한다.
    // 소유권? 인벤토리 데이터, 알아낸 레시피 기록 - 이외에도 필요한거 생각해보기

    // 현재 상태를 묶어서 저장 데이터에 담는다.
    public override void SaveData(SaveData _data)
    {
        var s = new SaveData();
        base.SaveData(s); // Entity 내부 SaveData 불러오기 -> 상태이상 데이터와 플레이어의 데이터 구분
        var state = new PlayerWorldData
        {
            Owner = Owner,
            CurrentHp = CurrentHp,
            CurrentStamina = Stats.CurrentStamina,
        };
        _data.SetData(state);
        _data.Child["b"] = s;
    }

    // 저장 데이터에서 상태를 꺼내 적용한다.
    public override void LoadData(SaveData _data)
    {
        base.LoadData(_data.Child["b"]); // Entity 내부 데이터(지금은 상태이상 값) 불러오기

        if (_data?.Data == null) return; // 처음 생성됐거나 예전 세이브라 데이터가 없을 때

        var state = _data.GetData<PlayerWorldData>();
        SetOwner(state.Owner);
        CurrentHp = state.CurrentHp;
        isDie = CurrentHp <= 0;
        Stats.LoadStamina(state.CurrentStamina);
        Stats.NotifyHpChanged();
    }

    void Start() => SetState(IsDead ? PlayerStateType.Dead : PlayerStateType.Idle);

    void Update()
    {
        if (!IsMine) return;

        Input.ReadInput();
#if UNITY_EDITOR
        ReadDebugKeys();
#endif
        fsm.Update();
        Stats.Tick(Time.deltaTime);
        Motor.SetMoveSpeed(Stats.MoveSpeed);
        currentStateView = fsm.CurrentStateType;
    }

    public void SetState(PlayerStateType next, bool allowReenter = false)
        => fsm.ChangeState(next, allowReenter);

    // ═══════════════════════ 입력 ═══════════════════════

#if UNITY_EDITOR
    // 에디터 테스트용: H = 10 피해, R = 사망 상태에서 부활
    void ReadDebugKeys()
    {
        if (Input.IsDebugDamagePressed()) TakeDamage(10, transform.position + transform.forward);
        if (Input.IsDebugRevivePressed() && IsDead) Revive();
    }
#endif

    // 받는 피해 비율을 적용해 피해를 입고, HP가 바뀌었으면 알린다.
    public override void TakeDamage(int damage)
    {
        if (IsDamageBlocked) return;

        int hpBefore = CurrentHp;
        base.TakeDamage(Mathf.RoundToInt(damage * DamageTakenRatio));
        if (CurrentHp != hpBefore) Stats.NotifyHpChanged();
    }

    // 투사체·공격 피해. 맞은 위치로 넉백 + 경직
    public void TakeDamage(int damage, Vector3 sourcePosition)
    {
        int hpBefore = CurrentHp;
        TakeDamage(damage);
        if (CurrentHp < hpBefore && !CheckDie())
        {
            LastHitSourcePosition = sourcePosition;
            SetState(PlayerStateType.Hit, allowReenter: true);
        }
    }

    protected override void OnDie() => SetState(PlayerStateType.Dead);

    public bool ConsumeAttack()
    {
        if (!CanAttack || !Stats.CanUseStamina) return false;
        if (!Input.ConsumeAttack(out Vector3 target)) return false;

        AttackTarget = Thrower.ClampToThrowRange(target);
        Stats.SpendStamina(Data.ThrowStaminaCost);
        return true;
    }

    // 쿨다운 중이면 입력을 소비하지 않고 버퍼에 남겨 둔다
    public bool ConsumeDodge()
    {
        if (!CanDodge || !Stats.CanUseStamina) return false;
        if (Time.time - lastDodgeAt < tuning.DodgeCooldown) return false;
        if (!Input.ConsumeDodge()) return false;

        Stats.SpendStamina(Data.DodgeStaminaCost);
        return true;
    }

    public void MarkDodgeUsed() => lastDodgeAt = Time.time;

    public void Revive()
    {
        CurrentHp = so.GetMaxHp();
        isDie = false;
        Stats.RestoreAll();
        Input.ClearMoveCommand();
        if (!Motor.AgentEnabled) Motor.SetAgentEnabled(true);
        if (spawnPos == null)
            transform.position = startPos;
        else
            transform.position = spawnPos.position;
        SetState(PlayerStateType.Idle);
    }

    // ═══════════════════════ 애니메이션 ═══════════════════════

    public void PlayAnim(int stateHash, bool restart = false, float fade = 0.08f)
    {
        if (anim == null) return;
        if (restart) { anim.Play(stateHash, 0, 0f); return; }

        int playing = anim.IsInTransition(0)
            ? anim.GetNextAnimatorStateInfo(0).shortNameHash
            : anim.GetCurrentAnimatorStateInfo(0).shortNameHash;
        if (playing != stateHash) anim.CrossFadeInFixedTime(stateHash, fade);
    }

    public void SetAnimSpeed(float value)
    {
        if (anim != null) anim.SetFloat(SpeedParam, value, 0.1f, Time.deltaTime);
    }
}
