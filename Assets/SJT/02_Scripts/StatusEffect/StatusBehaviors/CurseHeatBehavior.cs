/// <summary>저주가 틱할 때 대상에게 화상 상태를 한 번 부여한다.</summary>
public sealed class CurseHeatBehavior : IStatusBehavior
{
    public void OnRegistered(StatusEffect effect)
    {
    }

    public void OnTick(StatusEffect effect)
    {
        Entity target = effect.Target;
        if (target == null || target.CheckDie())
        {
            return;
        }

        if (target.TryGetComponent(out StatusEffectController controller))
        {
            controller.RegisterStatusEffect(StatusEffectType.Burn);
        }
    }

    public void OnReapply(StatusEffect effect)
    {
    }

    public void OnRemove(StatusEffect effect)
    {
    }

    public void ApplyModifier(StatusEffect effect, ref StatusModifiers modifiers)
    {
    }
}
