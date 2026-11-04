/// <summary>중첩마다 대상의 이동속도를 높인다.</summary>
public sealed class HasteBehavior : IStatusBehavior
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

    // 이동속도 배율 × (1 + 증가율 × 중첩).
    public void ApplyModifier(StatusEffect effect)
    {
        Entity target = effect.Target;
        if (target == null)
        {
            return;
        }

        target.MoveSpeedRatio *= 1f + effect.ValuePerStack * effect.StackCount;
    }
}
