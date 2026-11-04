/// <summary>대상을 무적으로 만든다. 실제 피해 무효는 피해 처리 쪽이 IsInvincible을 읽어야 한다.</summary>
// 주의: 같은 Entity 값을 바꾸는 다른 상태와 겹치면, 먼저 풀린 쪽의 OnRemove가 기본값으로 되돌려 남은 상태의 효과도 풀린다.
public sealed class InvincibleBehavior : IStatusBehavior
{
    public void OnRegistered(StatusEffect effect)
    {
        Entity target = effect.Target;
        if (target == null)
        {
            return;
        }

        target.IsInvincible = true;
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

        target.IsInvincible = false;
    }
}
