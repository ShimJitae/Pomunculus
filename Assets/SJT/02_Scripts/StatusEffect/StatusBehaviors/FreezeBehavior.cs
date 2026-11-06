/// <summary>대상을 얼려 완전히 멈춘다. 냉기 최대 중첩에서의 발동은 아직 구현하지 않았다.</summary>
public sealed class FreezeBehavior : IStatusBehavior
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
    }
}
