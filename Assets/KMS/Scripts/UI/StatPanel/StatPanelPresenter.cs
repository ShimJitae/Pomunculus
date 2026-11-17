using System;

public class StatPanelPresenter : IDisposable
{
    // Model
    private readonly PlayerStats stats;

    // View
    private readonly IStatPanelView view;

    public StatPanelPresenter(PlayerStats stats, IStatPanelView view)
    {
        this.stats = stats;
        this.view = view;

        stats.OnHpChanged += view.SetHp;
        stats.OnStaminaChanged += view.SetStamina;
        stats.OnShieldChanged += view.SetShield;

        view.SetHp(stats.CurrentHp, stats.MaxHp);
        view.SetStamina(stats.CurrentStamina, stats.MaxStamina);
        view.SetShield(stats.CurrentShield);
    }

    public void Dispose()
    {
        stats.OnHpChanged -= view.SetHp;
        stats.OnStaminaChanged -= view.SetStamina;
        stats.OnShieldChanged -= view.SetShield;
    }
}
