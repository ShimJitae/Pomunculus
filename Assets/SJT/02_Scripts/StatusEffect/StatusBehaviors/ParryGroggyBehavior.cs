public class ParryGroggyBehavior : IStatusBehavior
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
        modifiers.DamageTakenRatio *= 1f + effect.ValuePerStack;
    }
}
