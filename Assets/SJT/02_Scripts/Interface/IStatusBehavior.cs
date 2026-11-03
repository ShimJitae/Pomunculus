public interface IStatusBehavior
{
    void OnRegistered(StatusEffect effect);
    void OnTick(StatusEffect effect);
    void OnReapply(StatusEffect effect);
    void OnRemove(StatusEffect effect);
}