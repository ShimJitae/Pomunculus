using System;

public class PlayerStats
{
    private readonly PlayerFSM owner;
    private readonly PlayerDataSO data;

    // HP
    public int CurrentHp;
    public int MaxHp;
    public event Action<int, int> OnHpChanged;

    // Stamina
    private int currentStamina;
    private float regenDelayRemaining;   // 0이 되면 회복 시작

    public int CurrentStamina => currentStamina;
    public int MaxStamina => data.Stamina;
    public event Action<int, int> OnStaminaChanged;
    public bool CanUseStamina => CurrentStamina >= 1;

    // Speed
    public float MoveSpeed;


    public void SpendStamina(int cost)
    {
        SetStamina(currentStamina - cost);
        regenDelayRemaining = data.StaminaRegenDelay;
    }

    public void Tick(float deltaTime)
    {
        if (owner.IsDead) return;
        if (currentStamina >= MaxStamina) return;

        if (regenDelayRemaining > 0f)
        {
            regenDelayRemaining -= deltaTime;
            if (regenDelayRemaining > 0f) return;

            // 지연이 끝난 프레임에서는 남은 시간만큼만 회복
            deltaTime = -regenDelayRemaining;
            regenDelayRemaining = 0f;
        }

        SetStamina(currentStamina + (int)(data.StaminaRegenPerSecond * deltaTime));
    }

    public void RestoreAll()
    {
        regenDelayRemaining = 0f;
        SetStamina(MaxStamina);
        //NotifyHpChanged();
    }

    public void SetStamina(int value)
    {
        int clamped = Math.Clamp(value, 0, MaxStamina);
        if (clamped == CurrentStamina) return;

        currentStamina = clamped;
        OnStaminaChanged?.Invoke(CurrentStamina, MaxStamina);
    }
}
