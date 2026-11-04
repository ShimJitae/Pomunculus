/// <summary>대상을 돌로 굳혀 이동·공격·구르기를 막고 무적으로 만든다.</summary>
// 주의: 같은 Entity 값을 바꾸는 다른 상태와 겹치면, 먼저 풀린 쪽의 OnRemove가 기본값으로 되돌려 남은 상태의 효과도 풀린다.
public sealed class PetrifyBehavior : IStatusBehavior
{
    public void OnRegistered(StatusEffect effect)
    {
        Entity target = effect.Target;
        if (target == null)
        {
            return;
        }

        target.CanMove = false;
        target.CanAttack = false;
        target.CanDodge = false;
        target.MoveSpeedRatio = 0f;
        target.IsInvincible = true;
    }

    public void OnTick(StatusEffect effect)
    {
    }

    public void OnReapply(StatusEffect effect)
    {
    }

    public void OnRemove(StatusEffect effect)
    {
        Entity target = effect.Target;
        if (target == null)
        {
            return;
        }

        target.CanMove = true;
        target.CanAttack = true;
        target.CanDodge = true;
        target.MoveSpeedRatio = 1f;
        target.IsInvincible = false;
    }
}
