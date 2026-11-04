using UnityEngine;

/// <summary>대상을 크게 느리게 하고 구르기를 막는다.</summary>
// 주의: 같은 Entity 값을 바꾸는 다른 상태와 겹치면, 먼저 풀린 쪽의 OnRemove가 기본값으로 되돌려 남은 상태의 효과도 풀린다.
public sealed class SlimeBehavior : IStatusBehavior
{
    /// <summary>이동속도 = 1 − 감속률(중첩 무관), 구르기 불가.</summary>
    public void OnRegistered(StatusEffect effect)
    {
        Entity target = effect.Target;
        if (target == null)
        {
            return;
        }

        target.MoveSpeedRatio = Mathf.Max(0f, 1f - effect.ValuePerStack);
        target.CanDodge = false;
    }

    public void OnTick(StatusEffect effect)
    {
    }

    public void OnReapply(StatusEffect effect)
    {
    }

    public void OnRemove(StatusEffect effect)
    {
        Entity target = effect.Target;
        if (target == null)
        {
            return;
        }

        target.MoveSpeedRatio = 1f;
        target.CanDodge = true;
    }
}
