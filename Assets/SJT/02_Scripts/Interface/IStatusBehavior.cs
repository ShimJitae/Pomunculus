public interface IStatusBehavior
{
    void OnApply(StatusEffect effect);
    void OnTick(StatusEffect effect);
    void OnStackChanged(StatusEffect effect);
    void OnRemove(StatusEffect effect);
}