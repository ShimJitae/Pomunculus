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
[RequireComponent(typeof(Health))]
public class PlayerFSM : MonoBehaviour
{
    [Header("공격 (던지기)")]
    public float AttackDuration = 0.5f;       // 던지기 상태 전체 길이
    public float ThrowReleaseTime = 0.2f;     // 손에서 떠나는 시점
    public float AttackCancelTime = 0.3f;     // 이후 회피/재공격/이동으로 캔슬 가능 (releaseTime보다 커야 함)

    [Header("회피")]
    public float DodgeSpeed = 7f;
    public float DodgeDuration = 0.35f;
    public float DodgeInvincibleTime = 0.25f;
    public float DodgeCooldown = 0.5f;

    [Header("피격")]
    public float HitStunDuration = 0.4f;
    public float KnockbackSpeed = 0f;

    [Header("위치")]
    public Transform SpawnPos;

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

    // ───── 상태들이 사용하는 컴포넌트 ─────
    public PlayerInputReader Input { get; private set; }
    public PlayerMotor Motor { get; private set; }
    public PlayerThrower Thrower { get; private set; }
    public Health Health { get; private set; }
    public PlayerInteractor Interactor { get; private set; }   // 없을 수 있음 (null 허용)

    // ───── 상태들이 읽어 가는 데이터 ─────
    public PlayerStateType CurrentStateType => fsm.CurrentStateType;
    public PlayerStateType PreviousStateType => fsm.PreviousStateType;
    public bool IsDead => fsm.HasState && fsm.CurrentStateType == PlayerStateType.Dead;
    public Vector3 LastHitSourcePosition { get; private set; }
    public Vector3 AttackTarget { get; private set; }   // ConsumeAttack 성공 시 설정

    private float lastDodgeAt = float.NegativeInfinity;

    void Awake()
    {
        anim = GetComponentInChildren<Animator>();
        Input = GetComponent<PlayerInputReader>();
        Motor = GetComponent<PlayerMotor>();
        Thrower = GetComponent<PlayerThrower>();
        Health = GetComponent<Health>();
        Interactor = GetComponent<PlayerInteractor>();

        Health.IsInvincible = () =>
            IsDead || (fsm.CurrentState is PlayerStateBase state && state.IsInvincible(this));
        Health.OnDamaged += HandleDamaged;
        Health.OnDied += HandleDied;

        fsm = new StateMachine<PlayerStateType, PlayerFSM>(this);
        fsm.AddState(new PlayerIdleState());
        fsm.AddState(new PlayerMoveState());
        fsm.AddState(new PlayerAttackState());
        fsm.AddState(new PlayerDodgeState());
        fsm.AddState(new PlayerHitState());
        fsm.AddState(new PlayerDeadState());
        fsm.ValidateAllRegistered();
    }

    void OnDestroy()
    {
        if (Health == null) return;
        Health.OnDamaged -= HandleDamaged;
        Health.OnDied -= HandleDied;
    }

    void Start() => SetState(PlayerStateType.Idle);

    void Update()
    {
        Input.ReadInput();
#if UNITY_EDITOR
        ReadDebugKeys();
#endif
        fsm.Update();
        currentStateView = fsm.CurrentStateType;
    }

    public void SetState(PlayerStateType next, bool allowReenter = false)
        => fsm.ChangeState(next, allowReenter);

    // ═══════════════════════ 입력 ═══════════════════════

#if UNITY_EDITOR
    // 에디터 테스트용: H = 10 피해, R = 사망 상태에서 부활
    void ReadDebugKeys()
    {
        if (Input.IsDebugDamagePressed()) Health.TakeDamage(10, transform.position + transform.forward);
        if (Input.IsDebugRevivePressed() && IsDead) Revive();
    }
#endif

    public bool ConsumeAttack()
    {
        if (!Input.ConsumeAttack(out Vector3 target)) return false;
        AttackTarget = Thrower.ClampToThrowRange(target);
        return true;
    }

    // 쿨다운 중이면 입력을 소비하지 않고 버퍼에 남겨 둔다
    public bool ConsumeDodge()
    {
        if (Time.time - lastDodgeAt < DodgeCooldown) return false;
        return Input.ConsumeDodge();
    }

    public void MarkDodgeUsed() => lastDodgeAt = Time.time;

    // ═══════════════════════ 피격 / 사망 ═══════════════════════

    void HandleDamaged(Vector3 sourcePosition)
    {
        LastHitSourcePosition = sourcePosition;
        SetState(PlayerStateType.Hit, allowReenter: true);
    }

    void HandleDied() => SetState(PlayerStateType.Dead);

    public void Revive()
    {
        Health.ResetHp();
        Input.ClearMoveCommand();
        if (!Motor.AgentEnabled) Motor.SetAgentEnabled(true);
        transform.position = SpawnPos.position;
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
