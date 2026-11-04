using UnityEngine;

/// <summary>대상을 크게 느리게 하고 구르기를 막는다.</summary>
public sealed class SlimeBehavior : IStatusBehavior
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

    // 이동속도 배율 × (1 − 감속률), 중첩 무관. 구르기 불가.
    public void ApplyModifier(StatusEffect effect)
    {
        Entity target = effect.Target;
        if (target == null)
        {
            return;
        }

        target.MoveSpeedRatio *= Mathf.Max(0f, 1f - effect.ValuePerStack);
        target.CanDodge = false;
    }
}
