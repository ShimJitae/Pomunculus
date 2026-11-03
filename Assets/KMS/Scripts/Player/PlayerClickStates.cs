using UnityEngine;

public enum PlayerStateType
{
    Idle,
    Move,
    Attack,   // 마우스 위치로 던지기
    Dodge,
    Hit,
    Dead
}

// 클릭 이동 플레이어 상태의 공통 부모
public abstract class PlayerStateBase : IState<PlayerStateType, PlayerClickFSM>
{
    public abstract PlayerStateType StateType { get; }

    protected float elapsed;

    public virtual bool IsInvincible(PlayerClickFSM player) => false;

    public virtual void Enter(PlayerClickFSM player) => elapsed = 0f;
    public virtual void Update(PlayerClickFSM player) => elapsed += Time.deltaTime;
    public virtual void Exit(PlayerClickFSM player) { }

    // 회피 → 던지기 순으로 입력 확인. 전이했으면 true
    protected bool TryStartAction(PlayerClickFSM player)
    {
        if (player.ConsumeDodge()) { player.SetState(PlayerStateType.Dodge); return true; }
        if (player.ConsumeAttack()) { player.SetState(PlayerStateType.Attack); return true; }
        return false;
    }

    /// <summary>
    /// 대기 중인 우클릭 명령이 있으면 그 위치로 이동 시작.
    /// 이미 Move 상태라면 목적지만 갱신된다 (같은 상태로의 전이는 무시되므로).
    /// </summary>
    protected bool TryStartMove(PlayerClickFSM player)
    {
        if (!player.Input.ConsumeMoveCommand(out Vector3 destination)) return false;
        player.Motor.MoveTo(destination);
        player.SetState(PlayerStateType.Move);
        return true;
    }

    protected void ReturnToIdleOrMove(PlayerClickFSM player)
    {
        if (!TryStartMove(player)) player.SetState(PlayerStateType.Idle);
    }

    // 상호작용 입력이 있으면 주변 대상과 상호작용. 상태 전환은 하지 않는다 (Idle·Move에서만 호출)
    protected void TryInteract(PlayerClickFSM player)
    {
        if (player.Input.ConsumeInteract() && player.Interactor != null)
            player.Interactor.Interact();
    }
}

// ─────────────────────────────────────────────
// Idle : 정지
// ─────────────────────────────────────────────
public class PlayerIdleState : PlayerStateBase
{
    public override PlayerStateType StateType => PlayerStateType.Idle;

    public override void Enter(PlayerClickFSM player)
    {
        base.Enter(player);
        player.Motor.StopMoving();
        player.PlayAnim(PlayerClickFSM.LocomotionHash);
    }

    public override void Update(PlayerClickFSM player)
    {
        base.Update(player);

        TryInteract(player);
        if (TryStartAction(player)) return;
        if (TryStartMove(player)) return;

        player.SetAnimSpeed(0f);
    }
}

// ─────────────────────────────────────────────
// Move : 우클릭한 지점으로 NavMesh 경로 이동
// ─────────────────────────────────────────────
public class PlayerMoveState : PlayerStateBase
{
    public override PlayerStateType StateType => PlayerStateType.Move;

    public override void Enter(PlayerClickFSM player)
    {
        base.Enter(player);
        player.PlayAnim(PlayerClickFSM.LocomotionHash);
        // 목적지 설정(MoveTo)은 이 상태로 오기 직전에 이미 호출됨
    }

    public override void Update(PlayerClickFSM player)
    {
        base.Update(player);

        TryInteract(player);
        if (TryStartAction(player)) return;
        TryStartMove(player);   // 이동 중 다시 우클릭하면 목적지만 갱신

        player.Motor.FaceMoveDirection();
        player.SetAnimSpeed(player.Motor.NormalizedSpeed);

        if (player.Motor.HasArrived) player.SetState(PlayerStateType.Idle);
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

    private Vector3 target;
    private bool released;
    private Vector3 resumeDestination;

    public override void Enter(PlayerClickFSM player)
    {
        base.Enter(player);
        target = player.AttackTarget;
        released = false;

        player.Motor.StopMoving();
        player.Motor.RotateTowards(target - player.transform.position, instant: true);
        player.PlayAnim(PlayerClickFSM.AttackHash, restart: true);
    }

    public override void Update(PlayerClickFSM player)
    {
        base.Update(player);

        // 1) 발사 (한 번만)
        if (!released && elapsed >= player.ThrowReleaseTime)
        {
            player.Thrower.ThrowProjectile(target);
            released = true;
        }

        // 2) 캔슬 구간
        if (elapsed >= player.AttackCancelTime)
        {
            if (player.ConsumeDodge()) { player.SetState(PlayerStateType.Dodge); return; }
            if (player.ConsumeAttack()) { player.SetState(PlayerStateType.Attack, allowReenter: true); return; }
            if (TryStartMove(player)) return;   // 우클릭으로 후딜레이 캔슬
        }

        // 3) 종료
        if (elapsed >= player.AttackDuration)
        {
            if (TryStartMove(player)) return;

            player.SetState(PlayerStateType.Idle);
        }
    }
}

// ─────────────────────────────────────────────
// Dodge : 마우스 커서 방향으로 회피 (초반 무적)
// ─────────────────────────────────────────────
public class PlayerDodgeState : PlayerStateBase
{
    public override PlayerStateType StateType => PlayerStateType.Dodge;

    private Vector3 dir;

    public override bool IsInvincible(PlayerClickFSM player) => elapsed <= player.DodgeInvincibleTime;

    public override void Enter(PlayerClickFSM player)
    {
        base.Enter(player);
        player.Motor.StopMoving();

        dir = player.transform.forward;
        if (player.Input.TryGetMouseWorldPoint(out Vector3 point))
        {
            Vector3 toCursor = point - player.transform.position;
            toCursor.y = 0f;
            if (toCursor.sqrMagnitude > 0.01f) dir = toCursor.normalized;
        }

        player.Motor.RotateTowards(dir, instant: true);
        player.MarkDodgeUsed();
        player.PlayAnim(PlayerClickFSM.DodgeHash, restart: true);
    }

    public override void Update(PlayerClickFSM player)
    {
        base.Update(player);

        player.Motor.Displace(dir * player.DodgeSpeed);

        if (elapsed >= player.DodgeDuration) ReturnToIdleOrMove(player);
    }
}

// ─────────────────────────────────────────────
// Hit : 경직 + 넉백. 경직 중 누른 우클릭은 경직이 끝나면 실행됨
// ─────────────────────────────────────────────
public class PlayerHitState : PlayerStateBase
{
    public override PlayerStateType StateType => PlayerStateType.Hit;

    private Vector3 knockDir;

    public override void Enter(PlayerClickFSM player)
    {
        base.Enter(player);
        player.Motor.StopMoving();

        Vector3 v = player.transform.position - player.LastHitSourcePosition;
        v.y = 0f;
        knockDir = v.sqrMagnitude > 0.0001f ? v.normalized : -player.transform.forward;

        player.Motor.RotateTowards(-knockDir, instant: true);
        player.PlayAnim(PlayerClickFSM.HitHash, restart: true);
    }

    public override void Update(PlayerClickFSM player)
    {
        base.Update(player);

        float t = Mathf.Clamp01(elapsed / player.HitStunDuration);
        player.Motor.Displace(knockDir * player.KnockbackSpeed * (1f - t));

        if (elapsed >= player.HitStunDuration) ReturnToIdleOrMove(player);
    }
}

// ─────────────────────────────────────────────
// Dead : Agent를 꺼서 다른 유닛을 밀어내지 않게 함
// ─────────────────────────────────────────────
public class PlayerDeadState : PlayerStateBase
{
    public override PlayerStateType StateType => PlayerStateType.Dead;

    public override bool IsInvincible(PlayerClickFSM player) => true;

    public override void Enter(PlayerClickFSM player)
    {
        base.Enter(player);
        player.Motor.SetAgentEnabled(false);
        player.PlayAnim(PlayerClickFSM.DieHash);
    }
}
