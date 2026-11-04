using UnityEngine;

/// <summary>중첩마다 대상이 받는 회복량을 줄인다. 지속 피해는 주지 않는다.</summary>
public sealed class PoisonBehavior : IStatusBehavior
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

    // 회복량 배율 × (1 − 감소율 × 중첩). 0 미만은 0으로 맞춘다.
    public void ApplyModifier(StatusEffect effect)
    {
        Entity target = effect.Target;
        if (target == null)
        {
            return;
        }

        target.HealReceivedRatio *= Mathf.Max(0f, 1f - effect.ValuePerStack * effect.StackCount);
    }
}
