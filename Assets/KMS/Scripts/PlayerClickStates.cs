using UnityEngine;

namespace ClickMoveExample
{
    public enum PlayerStateType
    {
        Idle,
        Move,
        Attack,   // 마우스 위치로 던지기
        Dodge,
        Hit,
        Dead
    }

    /// <summary>클릭 이동 플레이어 상태의 공통 부모</summary>
    public abstract class PlayerStateBase : IState<PlayerStateType, PlayerClickFSM>
    {
        public abstract PlayerStateType StateType { get; }

        protected float elapsed;

        public virtual bool IsInvincible(PlayerClickFSM p) => false;

        public virtual void Enter(PlayerClickFSM p) => elapsed = 0f;
        public virtual void Update(PlayerClickFSM p) => elapsed += Time.deltaTime;
        public virtual void Exit(PlayerClickFSM p) { }

        /// <summary>회피 → 던지기 순으로 입력 확인. 전이했으면 true</summary>
        protected bool TryStartAction(PlayerClickFSM p)
        {
            if (p.ConsumeDodge()) { p.SetState(PlayerStateType.Dodge); return true; }
            if (p.ConsumeAttack()) { p.SetState(PlayerStateType.Attack); return true; }
            return false;
        }

        /// <summary>
        /// 대기 중인 우클릭 명령이 있으면 그 위치로 이동 시작.
        /// 이미 Move 상태라면 목적지만 갱신된다 (같은 상태로의 전이는 무시되므로).
        /// </summary>
        protected bool TryStartMove(PlayerClickFSM p)
        {
            if (!p.ConsumeMoveCommand(out Vector3 destination)) return false;
            p.MoveTo(destination);
            p.SetState(PlayerStateType.Move);
            return true;
        }

        protected void ReturnToIdleOrMove(PlayerClickFSM p)
        {
            if (!TryStartMove(p)) p.SetState(PlayerStateType.Idle);
        }
    }

    // ─────────────────────────────────────────────
    // Idle : 정지
    // ─────────────────────────────────────────────
    public class PlayerIdleState : PlayerStateBase
    {
        public override PlayerStateType StateType => PlayerStateType.Idle;

        public override void Enter(PlayerClickFSM p)
        {
            base.Enter(p);
            p.StopMoving();
            p.PlayAnim(PlayerClickFSM.LocomotionHash);
        }

        public override void Update(PlayerClickFSM p)
        {
            base.Update(p);

            if (TryStartAction(p)) return;
            if (TryStartMove(p)) return;

            p.SetAnimSpeed(0f);
        }
    }

    // ─────────────────────────────────────────────
    // Move : 우클릭한 지점으로 NavMesh 경로 이동
    // ─────────────────────────────────────────────
    public class PlayerMoveState : PlayerStateBase
    {
        public override PlayerStateType StateType => PlayerStateType.Move;

        public override void Enter(PlayerClickFSM p)
        {
            base.Enter(p);
            p.PlayAnim(PlayerClickFSM.LocomotionHash);
            // 목적지 설정(MoveTo)은 이 상태로 오기 직전에 이미 호출됨
        }

        public override void Update(PlayerClickFSM p)
        {
            base.Update(p);

            if (TryStartAction(p)) return;
            TryStartMove(p);   // 이동 중 다시 우클릭하면 목적지만 갱신

            p.FaceMoveDirection();
            p.SetAnimSpeed(p.NormalizedSpeed);

            if (p.HasArrived) p.SetState(PlayerStateType.Idle);
        }
    }

    // ─────────────────────────────────────────────
    // Attack : 클릭한 위치로 던지기
    //   - 멈춰서 목표 방향으로 즉시 회전 → releaseTime에 발사
    //   - cancelTime 이후 회피 / 연속 던지기 / 우클릭 이동으로 캔슬
    //   - 이동 중에 던졌다면 끝난 뒤 원래 목적지로 이동 재개 (옵션)
    // ─────────────────────────────────────────────
    public class PlayerAttackState : PlayerStateBase
    {
        public override PlayerStateType StateType => PlayerStateType.Attack;

        Vector3 target;
        bool released;
        Vector3 resumeDestination;

        public override void Enter(PlayerClickFSM p)
        {
            base.Enter(p);
            target = p.AttackTarget;
            released = false;

            p.StopMoving();
            p.RotateTowards(target - p.transform.position, instant: true);
            p.PlayAnim(PlayerClickFSM.AttackHash, restart: true);
        }

        public override void Update(PlayerClickFSM p)
        {
            base.Update(p);

            // 1) 발사 (한 번만)
            if (!released && elapsed >= p.throwReleaseTime)
            {
                p.ThrowProjectile(target);
                released = true;
            }

            // 2) 캔슬 구간
            if (elapsed >= p.attackCancelTime)
            {
                if (p.ConsumeDodge()) { p.SetState(PlayerStateType.Dodge); return; }
                if (p.ConsumeAttack()) { p.SetState(PlayerStateType.Attack, allowReenter: true); return; }
                if (TryStartMove(p)) return;   // 우클릭으로 후딜레이 캔슬
            }

            // 3) 종료
            if (elapsed >= p.attackDuration)
            {
                if (TryStartMove(p)) return;

                p.SetState(PlayerStateType.Idle);
            }
        }
    }

    // ─────────────────────────────────────────────
    // Dodge : 마우스 커서 방향으로 회피 (초반 무적)
    // ─────────────────────────────────────────────
    public class PlayerDodgeState : PlayerStateBase
    {
        public override PlayerStateType StateType => PlayerStateType.Dodge;

        Vector3 dir;

        public override bool IsInvincible(PlayerClickFSM p) => elapsed <= p.dodgeInvincibleTime;

        public override void Enter(PlayerClickFSM p)
        {
            base.Enter(p);
            p.StopMoving();

            dir = p.transform.forward;
            if (p.TryGetMouseWorldPoint(out Vector3 point))
            {
                Vector3 toCursor = point - p.transform.position;
                toCursor.y = 0f;
                if (toCursor.sqrMagnitude > 0.01f) dir = toCursor.normalized;
            }

            p.RotateTowards(dir, instant: true);
            p.MarkDodgeUsed();
            p.PlayAnim(PlayerClickFSM.DodgeHash, restart: true);
        }

        public override void Update(PlayerClickFSM p)
        {
            base.Update(p);

            p.Displace(dir * p.dodgeSpeed);

            if (elapsed >= p.dodgeDuration) ReturnToIdleOrMove(p);
        }
    }

    // ─────────────────────────────────────────────
    // Hit : 경직 + 넉백. 경직 중 누른 우클릭은 경직이 끝나면 실행됨
    // ─────────────────────────────────────────────
    public class PlayerHitState : PlayerStateBase
    {
        public override PlayerStateType StateType => PlayerStateType.Hit;

        Vector3 knockDir;

        public override void Enter(PlayerClickFSM p)
        {
            base.Enter(p);
            p.StopMoving();

            Vector3 v = p.transform.position - p.LastHitSourcePosition;
            v.y = 0f;
            knockDir = v.sqrMagnitude > 0.0001f ? v.normalized : -p.transform.forward;

            p.RotateTowards(-knockDir, instant: true);
            p.PlayAnim(PlayerClickFSM.HitHash, restart: true);
        }

        public override void Update(PlayerClickFSM p)
        {
            base.Update(p);

            float t = Mathf.Clamp01(elapsed / p.hitStunDuration);
            p.Displace(knockDir * p.knockbackSpeed * (1f - t));

            if (elapsed >= p.hitStunDuration) ReturnToIdleOrMove(p);
        }
    }

    // ─────────────────────────────────────────────
    // Dead : Agent를 꺼서 다른 유닛을 밀어내지 않게 함
    // ─────────────────────────────────────────────
    public class PlayerDeadState : PlayerStateBase
    {
        public override PlayerStateType StateType => PlayerStateType.Dead;

        public override bool IsInvincible(PlayerClickFSM p) => true;

        public override void Enter(PlayerClickFSM p)
        {
            base.Enter(p);
            p.SetAgentEnabled(false);
            p.PlayAnim(PlayerClickFSM.DieHash);
        }
    }
}