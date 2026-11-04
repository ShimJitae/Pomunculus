/// <summary>대상을 제자리에 묶어 이동·구르기를 막는다. 공격(포션 투척)은 허용한다.</summary>
public sealed class BindBehavior : IStatusBehavior
{
    public void OnRegistered(StatusEffect effect)
    {
    }

    public void OnTick(StatusEffect effect)
    {
    }

    public void OnReapply(StatusEffect effect)
    {
    }

    public void OnRemove(StatusEffect effect)
    {
    }

    public void ApplyModifier(StatusEffect effect)
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
}
