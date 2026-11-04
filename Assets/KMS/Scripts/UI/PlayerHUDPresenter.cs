using System;

public class PlayerHUDPresenter : IDisposable
{
    // Model
    private readonly PlayerStats stats;

    // View
    private readonly IPlayerHUDView view;

    public PlayerHUDPresenter(PlayerStats stats, IPlayerHUDView view)
    {
        this.stats = stats;
        this.view = view;

        stats.OnHpChanged += view.SetHp;
        stats.OnStaminaChanged += view.SetStamina;

        view.SetHp(stats.CurrentHp, stats.MaxHp);
        view.SetStamina(stats.CurrentStamina, stats.MaxStamina);
    }

    public void Dispose()
    {
        stats.OnHpChanged -= view.SetHp;
        stats.OnStaminaChanged -= view.SetStamina;
    }
}
