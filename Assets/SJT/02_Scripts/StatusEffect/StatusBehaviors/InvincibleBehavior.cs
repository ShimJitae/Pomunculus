/// <summary>대상을 무적으로 만든다. 실제 피해 무효는 피해 처리 쪽이 IsInvincible을 읽어야 한다.</summary>
public sealed class InvincibleBehavior : IStatusBehavior
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

        target.IsInvincible = true;
    }
}
