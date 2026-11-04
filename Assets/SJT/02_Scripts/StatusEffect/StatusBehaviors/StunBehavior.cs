/// <summary>대상의 이동·공격·구르기를 모두 막는다.</summary>
public sealed class StunBehavior : IStatusBehavior
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
        target.CanAttack = false;
        target.CanDodge = false;
    }
}
