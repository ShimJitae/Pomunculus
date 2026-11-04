/// <summary>중첩마다 대상의 이동속도를 높인다.</summary>
// 주의: 같은 Entity 값을 바꾸는 다른 상태와 겹치면, 먼저 풀린 쪽의 OnRemove가 기본값으로 되돌려 남은 상태의 효과도 풀린다.
public sealed class HasteBehavior : IStatusBehavior
{
    public void OnRegistered(StatusEffect effect)
    {
        Apply(effect);
    }

    public void OnTick(StatusEffect effect)
    {
    }

    /// <summary>중첩이 바뀌었으므로 가속을 다시 계산한다.</summary>
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

        target.MoveSpeedRatio = 1f;
    }

    // 이동속도 = 1 + 증가율 × 중첩.
    private static void Apply(StatusEffect effect)
    {
        Entity target = effect.Target;
        if (target == null)
        {
            return;
        }

        target.MoveSpeedRatio = 1f + effect.ValuePerStack * effect.StackCount;
    }
}
