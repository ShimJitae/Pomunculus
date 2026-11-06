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

    public void ApplyModifier(StatusEffect effect, ref StatusModifiers modifiers)
    {
        modifiers.CanMove = false;
        modifiers.CanAttack = false;
        modifiers.CanDodge = false;
        modifiers.MoveSpeedRatio = 0f;
        modifiers.IsInvincible = true;
    }
}
