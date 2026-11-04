using UnityEngine;

/// <summary>대상을 조금 느리게 한다. 화상과의 상쇄는 아직 구현하지 않았다.</summary>
public sealed class WetBehavior : IStatusBehavior
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

    // 이동속도 배율 × (1 − 감속률), 중첩 무관.
    public void ApplyModifier(StatusEffect effect)
    {
        Entity target = effect.Target;
        if (target == null)
        {
            return;
        }

        target.MoveSpeedRatio *= Mathf.Max(0f, 1f - effect.ValuePerStack);
    }
}
