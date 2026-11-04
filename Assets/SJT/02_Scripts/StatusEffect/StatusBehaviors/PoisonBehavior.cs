using UnityEngine;

/// <summary>중첩마다 대상이 받는 회복량을 줄인다. 지속 피해는 주지 않는다.</summary>
public sealed class PoisonBehavior : IStatusBehavior
{
    public void OnRegistered(StatusEffect effect)
    {
        Apply(effect);
    }

    public void OnTick(StatusEffect effect)
    {
    }

    /// <summary>중첩이 바뀌었으므로 회복 감소를 다시 계산한다.</summary>
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

        target.HealReceivedRatio = 1f;
    }

    // 회복량 = 1 − 감소율 × 중첩. 0 미만은 0으로 맞춘다.
    private static void Apply(StatusEffect effect)
    {
        Entity target = effect.Target;
        if (target == null)
        {
            return;
        }

        target.HealReceivedRatio = Mathf.Max(0f, 1f - effect.ValuePerStack * effect.StackCount);
    }
}
