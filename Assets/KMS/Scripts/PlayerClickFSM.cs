using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

namespace ClickMoveExample
{
    /// <summary>
    /// 클릭 이동 플레이어 (FSM Owner)
    ///  - 우클릭 : 이동 (누르고 있으면 커서를 따라감). 적을 클릭해도 이동만 한다.
    ///  - 좌클릭 : 클릭한 지면 위치로 물체를 던짐
    ///  - Space  : 커서 방향으로 회피
    /// 이동은 NavMeshAgent가 담당하므로 CharacterController는 사용하지 않는다.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class PlayerClickFSM : MonoBehaviour, IDamageable
    {
        [Header("이동 (NavMeshAgent)")]
        public float moveSpeed = 5f;
        public float acceleration = 60f;          // 기본값 8은 액션 게임엔 너무 굼뜸
        public float rotateSpeed = 900f;          // 초당 회전 각도 (회전은 코드가 직접 처리)
        public float stoppingDistance = 0.1f;
        public float holdRepathInterval = 0.1f;   // 우클릭 홀드 시 목적지 갱신 간격
        public float navSampleRadius = 2f;        // 클릭 지점을 NavMesh 위로 보정하는 범위
        [Tooltip("마우스 레이캐스트 대상. 바닥 레이어만 지정하는 것을 권장")]
        public LayerMask groundLayer = ~0;
        [Tooltip("선택: 클릭 위치에 표시할 오브젝트 (파티클 등, 씬에 하나 두고 재사용)")]
        public GameObject moveMarker;

        [Header("공격 (던지기)")]
        public ThrownProjectile projectilePrefab;
        public Transform throwOrigin;             // 손 위치. 비우면 머리 높이에서 던짐
        public float attackDuration = 0.5f;       // 던지기 상태 전체 길이
        public float throwReleaseTime = 0.2f;     // 손에서 떠나는 시점
        public float attackCancelTime = 0.3f;     // 이후 회피/재공격/이동으로 캔슬 가능 (releaseTime보다 커야 함)
        public float maxThrowRange = 10f;
        public float throwHorizontalSpeed = 12f;  // 수평 비행 속도 (멀수록 오래 날아감)
        public float minFlightTime = 0.35f;       // 가까이 던질 때 최소 체공 시간 (포물선 높이)
        public int attackDamage = 10;
        public LayerMask enemyLayer;

        [Header("회피")]
        public float dodgeSpeed = 12f;
        public float dodgeDuration = 0.35f;
        public float dodgeInvincibleTime = 0.25f;
        public float dodgeCooldown = 0.5f;

        [Header("피격 / 체력")]
        public int maxHp = 100;
        public float hitStunDuration = 0.4f;
        public float knockbackSpeed = 6f;

        [Header("입력")]
        public float inputBufferTime = 0.15f;

        [Header("디버그")]
        [SerializeField] PlayerStateType currentStateView;

        public static readonly int LocomotionHash = Animator.StringToHash("Locomotion");
        public static readonly int AttackHash = Animator.StringToHash("Attack");
        public static readonly int DodgeHash = Animator.StringToHash("Dodge");
        public static readonly int HitHash = Animator.StringToHash("Hit");
        public static readonly int DieHash = Animator.StringToHash("Die");
        static readonly int SpeedParam = Animator.StringToHash("Speed");

        StateMachine<PlayerStateType, PlayerClickFSM> fsm;
        NavMeshAgent agent;
        Animator anim;
        Camera cam;

        // ───── 상태들이 읽어 가는 데이터 ─────
        public int Hp { get; private set; }
        public PlayerStateType CurrentStateType => fsm.CurrentStateType;
        public PlayerStateType PreviousStateType => fsm.PreviousStateType;
        public bool IsDead => fsm.HasState && fsm.CurrentStateType == PlayerStateType.Dead;
        public Vector3 LastHitSourcePosition { get; private set; }
        public Vector3 AttackTarget { get; private set; }   // ConsumeAttack 성공 시 설정
        public bool HasPendingMove { get; private set; }     // 아직 처리되지 않은 우클릭 이동 명령

        Vector3 pendingDestination;
        Vector3 bufferedAttackTarget;
        float attackPressedAt = float.NegativeInfinity;
        float dodgePressedAt = float.NegativeInfinity;
        float lastDodgeAt = float.NegativeInfinity;
        float nextHoldRepathAt = float.NegativeInfinity;

        void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            anim = GetComponentInChildren<Animator>();
            cam = Camera.main;
            Hp = maxHp;

            agent.speed = moveSpeed;
            agent.acceleration = acceleration;
            agent.stoppingDistance = stoppingDistance;
            agent.updateRotation = false;   // 던질 때 즉시 회전해야 하므로 회전은 직접 처리
            agent.autoBraking = true;

            fsm = new StateMachine<PlayerStateType, PlayerClickFSM>(this);
            fsm.AddState(new PlayerIdleState());
            fsm.AddState(new PlayerMoveState());
            fsm.AddState(new PlayerAttackState());
            fsm.AddState(new PlayerDodgeState());
            fsm.AddState(new PlayerHitState());
            fsm.AddState(new PlayerDeadState());
            fsm.ValidateAllRegistered();
        }

        void Start() => SetState(PlayerStateType.Idle);

        void Update()
        {
            ReadInput();
            fsm.Update();
            currentStateView = fsm.CurrentStateType;
        }

        public void SetState(PlayerStateType next, bool allowReenter = false)
            => fsm.ChangeState(next, allowReenter);

        // ═══════════════════════ 입력 ═══════════════════════

        void ReadInput()
        {
            Mouse mouse = Mouse.current;
            Keyboard keyboard = Keyboard.current;
            if (mouse == null) return;   // 마우스가 없는 환경 (모바일 등)
 
            // 우클릭: 이동 명령. 누르고 있으면 일정 간격으로 목적지 갱신
            bool rightDown = mouse.rightButton.wasPressedThisFrame;
            bool rightHeld = mouse.rightButton.isPressed;
            if (rightDown || (rightHeld && Time.time >= nextHoldRepathAt))
            {
                nextHoldRepathAt = Time.time + holdRepathInterval;
                if (TryGetNavPointUnderMouse(out Vector3 dest))
                {
                    pendingDestination = dest;
                    HasPendingMove = true;
                    if (rightDown) ShowMoveMarker(dest);
                }
            }
 
            // 좌클릭: 던지기. 클릭한 순간의 마우스 위치를 목표로 저장
            if (mouse.leftButton.wasPressedThisFrame && TryGetMouseWorldPoint(out Vector3 target))
            {
                attackPressedAt = Time.time;
                bufferedAttackTarget = target;
            }
 
            if (keyboard == null) return;
 
            if (keyboard.spaceKey.wasPressedThisFrame) dodgePressedAt = Time.time;
 
#if UNITY_EDITOR
            if (keyboard.hKey.wasPressedThisFrame) TakeDamage(10, transform.position + transform.forward);
            if (keyboard.rKey.wasPressedThisFrame && IsDead) Revive();
#endif
        }

        /// <summary>
        /// 처리 대기 중인 이동 명령을 꺼낸다.
        /// 공격·피격 중에 누른 우클릭은 사라지지 않고 남아 있다가, 상태가 끝나면 실행된다.
        /// </summary>
        public bool ConsumeMoveCommand(out Vector3 destination)
        {
            destination = pendingDestination;
            if (!HasPendingMove) return false;
            HasPendingMove = false;
            return true;
        }

        public bool ConsumeAttack()
        {
            if (Time.time - attackPressedAt > inputBufferTime) return false;
            attackPressedAt = float.NegativeInfinity;
            AttackTarget = ClampToThrowRange(bufferedAttackTarget);
            return true;
        }

        public bool ConsumeDodge()
        {
            if (Time.time - lastDodgeAt < dodgeCooldown) return false;
            if (Time.time - dodgePressedAt > inputBufferTime) return false;
            dodgePressedAt = float.NegativeInfinity;
            return true;
        }

        public void MarkDodgeUsed() => lastDodgeAt = Time.time;

        // ═══════════════════════ NavMeshAgent 제어 ═══════════════════════

        bool AgentReady => agent.enabled && agent.isOnNavMesh;

        public void MoveTo(Vector3 destination)
        {
            if (!AgentReady) return;
            agent.isStopped = false;
            agent.SetDestination(destination);
        }

        public void StopMoving()
        {
            if (!AgentReady) return;
            agent.isStopped = true;
            agent.ResetPath();
            agent.velocity = Vector3.zero;   // 관성으로 미끄러지지 않게
        }

        /// <summary>경로 계산이 끝났고 목적지에 도착했는지</summary>
        public bool HasArrived =>
            !AgentReady ||
            (!agent.pathPending &&
             (!agent.hasPath || agent.remainingDistance <= agent.stoppingDistance));

        /// <summary>현재 이동 중인 목적지 (던진 뒤 이동 재개용)</summary>
        public bool TryGetCurrentDestination(out Vector3 destination)
        {
            destination = default;
            if (!AgentReady || (!agent.hasPath && !agent.pathPending)) return false;
            destination = agent.destination;
            return true;
        }

        /// <summary>회피·넉백처럼 경로와 무관한 이동. NavMesh 밖으로는 밀려나지 않는다.</summary>
        public void Displace(Vector3 velocity)
        {
            if (AgentReady) agent.Move(velocity * Time.deltaTime);
        }

        public void FaceMoveDirection() => RotateTowards(agent.desiredVelocity);

        public float NormalizedSpeed =>
            AgentReady ? Mathf.Clamp01(agent.velocity.magnitude / moveSpeed) : 0f;

        public void SetAgentEnabled(bool enabled)
        {
            if (enabled)
            {
                agent.enabled = true;
                agent.Warp(transform.position);
            }
            else
            {
                StopMoving();
                agent.enabled = false;   // 사망 시 다른 Agent를 밀어내지 않도록
            }
        }

        // ═══════════════════════ 마우스 ═══════════════════════

        /// <summary>마우스 아래 지면 위치 (지형 높이 반영). 실패 시 플레이어 높이 평면으로 대체</summary>
        public bool TryGetMouseWorldPoint(out Vector3 point)
        {
            point = default;
            Mouse mouse = Mouse.current;
            if (cam == null || mouse == null) return false;

            Ray ray = cam.ScreenPointToRay(mouse.position.ReadValue());

            if (Physics.Raycast(ray, out RaycastHit hit, 500f, groundLayer, QueryTriggerInteraction.Ignore))
            {
                point = hit.point;
                return true;
            }

            var plane = new Plane(Vector3.up, transform.position);
            if (!plane.Raycast(ray, out float enter)) return false;
            point = ray.GetPoint(enter);
            return true;
        }

        /// <summary>마우스 아래 지점을 갈 수 있는 NavMesh 위치로 보정</summary>
        bool TryGetNavPointUnderMouse(out Vector3 point)
        {
            point = default;
            if (!TryGetMouseWorldPoint(out Vector3 ground)) return false;
            if (!NavMesh.SamplePosition(ground, out NavMeshHit navHit, navSampleRadius, NavMesh.AllAreas))
                return false;

            point = navHit.position;
            return true;
        }

        void ShowMoveMarker(Vector3 position)
        {
            if (moveMarker == null) return;
            moveMarker.transform.position = position;
            moveMarker.SetActive(false);   // 껐다 켜서 파티클 등을 다시 재생
            moveMarker.SetActive(true);
        }

        Vector3 ClampToThrowRange(Vector3 target)
        {
            Vector3 flat = target - transform.position;
            flat.y = 0f;
            if (flat.magnitude <= maxThrowRange) return target;

            Vector3 clamped = transform.position + flat.normalized * maxThrowRange;
            clamped.y = target.y;
            return clamped;
        }

        // ═══════════════════════ 던지기 ═══════════════════════

        /// <summary>목표 지점에 정확히 떨어지는 포물선 속도로 투사체 발사</summary>
        public void ThrowProjectile(Vector3 target)
        {
            if (projectilePrefab == null)
            {
                Debug.LogWarning("[PlayerClickFSM] projectilePrefab이 지정되지 않았습니다.");
                return;
            }

            Vector3 origin = throwOrigin != null
                ? throwOrigin.position
                : transform.position + Vector3.up * 1.5f;

            Vector3 toTarget = target - origin;
            float horizontalDist = new Vector2(toTarget.x, toTarget.z).magnitude;
            float flightTime = Mathf.Max(minFlightTime, horizontalDist / throwHorizontalSpeed);

            // 등가속도 운동: target = origin + v*t + 0.5*g*t²  →  v = (target - origin)/t - 0.5*g*t
            Vector3 velocity = toTarget / flightTime - 0.5f * flightTime * Physics.gravity;

            ThrownProjectile projectile = Instantiate(projectilePrefab, origin, Quaternion.identity);
            projectile.Launch(velocity, attackDamage, enemyLayer, gameObject);
        }

        // ═══════════════════════ 피격 / 사망 ═══════════════════════

        public void TakeDamage(int damage, Vector3 sourcePosition)
        {
            if (IsDead) return;
            if (fsm.CurrentState is PlayerStateBase state && state.IsInvincible(this)) return;

            Hp = Mathf.Max(0, Hp - damage);

            if (Hp == 0)
            {
                SetState(PlayerStateType.Dead);
                return;
            }

            LastHitSourcePosition = sourcePosition;
            SetState(PlayerStateType.Hit, allowReenter: true);
        }

        public void Revive()
        {
            Hp = maxHp;
            HasPendingMove = false;
            if (!agent.enabled) SetAgentEnabled(true);
            SetState(PlayerStateType.Idle);
        }

        // ═══════════════════════ 회전 / 애니메이션 ═══════════════════════

        public void RotateTowards(Vector3 dir, bool instant = false)
        {
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f) return;
            Quaternion target = Quaternion.LookRotation(dir);
            transform.rotation = instant
                ? target
                : Quaternion.RotateTowards(transform.rotation, target, rotateSpeed * Time.deltaTime);
        }

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

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.6f);
            Gizmos.DrawWireSphere(transform.position, maxThrowRange);   // 최대 사거리
        }
    }
}