/// <summary>대상을 돌로 굳혀 이동·공격·구르기를 막고 무적으로 만든다.</summary>
public sealed class PetrifyBehavior : IStatusBehavior
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

    public void ApplyModifier(StatusEffect effect)
    {
        Entity target = effect.Target;
        if (target == null)
        {
            return;
        }

        target.CanMove = false;
        target.CanAttack = false;
        target.CanDodge = false;
        target.MoveSpeedRatio = 0f;
        target.IsInvincible = true;
    }
}
