/// <summary>대상을 제자리에 묶어 이동·구르기를 막는다. 공격(포션 투척)은 허용한다.</summary>
// 주의: 같은 Entity 값을 바꾸는 다른 상태와 겹치면, 먼저 풀린 쪽의 OnRemove가 기본값으로 되돌려 남은 상태의 효과도 풀린다.
public sealed class BindBehavior : IStatusBehavior
{
    public void OnRegistered(StatusEffect effect)
    {
        Entity target = effect.Target;
        if (target == null)
        {
            return;
        }

        target.CanMove = false;
        target.CanDodge = false;
        target.MoveSpeedRatio = 0f;
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
        target.CanDodge = true;
        target.MoveSpeedRatio = 1f;
    }
}
