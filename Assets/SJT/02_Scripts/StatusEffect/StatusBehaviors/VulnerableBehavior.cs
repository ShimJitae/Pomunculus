/// <summary>중첩마다 대상이 받는 피해 배율을 높인다. 실제 피해 반영은 피해 처리 쪽이 배율을 읽어야 한다.</summary>
public sealed class VulnerableBehavior : IStatusBehavior
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

    // 받는 피해 배율 × (1 + 증가율 × 중첩).
    public void ApplyModifier(StatusEffect effect)
    {
        Entity target = effect.Target;
        if (target == null)
        {
            return;
        }

        target.DamageTakenRatio *= 1f + effect.ValuePerStack * effect.StackCount;
    }
}
