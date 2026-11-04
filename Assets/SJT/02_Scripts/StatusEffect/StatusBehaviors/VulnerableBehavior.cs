/// <summary>중첩마다 대상이 받는 피해 배율을 높인다. 실제 피해 반영은 피해 처리 쪽이 배율을 읽어야 한다.</summary>
public sealed class VulnerableBehavior : IStatusBehavior
{
    public void OnRegistered(StatusEffect effect)
    {
        Apply(effect);
    }

    public void OnTick(StatusEffect effect)
    {
    }

    /// <summary>중첩이 바뀌었으므로 피해 배율을 다시 계산한다.</summary>
    public void OnReapply(StatusEffect effect)
    {
        Apply(effect);
    }

    public void OnRemove(StatusEffect effect)
    {
        Entity target = effect.Target;
        if (target == null)
        {
            return;
        }

        target.DamageTakenRatio = 1f;
    }

    // 받는 피해 배율 = 1 + 증가율 × 중첩.
    private static void Apply(StatusEffect effect)
    {
        Entity target = effect.Target;
        if (target == null)
        {
            return;
        }

        target.DamageTakenRatio = 1f + effect.ValuePerStack * effect.StackCount;
    }
}
