using System;
using System.Collections.Generic;

public class PlayerStats
{
    private readonly PlayerFSM owner;
    private readonly PlayerDataSO data;

    // HP
    public int CurrentHp => owner.CurrentHp;
    public int MaxHp => data.GetMaxHp();
    public event Action<int, int> OnHpChanged;

    // Stamina
    private int currentStamina;
    private float regenProgress;
    private float regenDelayRemaining;   // 0이 되면 회복 시작

    public int CurrentStamina => currentStamina;
    public int MaxStamina => data.MaxStamina;
    public event Action<int, int> OnStaminaChanged;
    public bool CanUseStamina => CurrentStamina >= 1;

    // Shield
    private struct ShieldChunk { public int Amount; public float RemainingTime; }
    private readonly List<ShieldChunk> shieldChunks = new();
    private int currentShield;
    public int CurrentShield => currentShield;
    public event Action<int> OnShieldChanged;

    // Speed
    public float MoveSpeed => data.GetSpeed() * owner.MoveSpeedRatio;

    // 소유자와 기본 데이터를 받아 스태미나를 최대로 채운 상태로 시작한다.
    public PlayerStats(PlayerFSM owner, PlayerDataSO data)
    {
        this.owner = owner;
        this.data = data;
        currentStamina = data.MaxStamina;
    }

    public void NotifyHpChanged()
    {
        OnHpChanged?.Invoke(CurrentHp, MaxHp);
    }

    // 실드 추가
    public void AddShield(int amount, float duration)
    {
        ShieldChunk chunk = new ShieldChunk();
        chunk.Amount = amount;
        chunk.RemainingTime = duration;

        shieldChunks.Add(chunk);
        SetShield(SumShieldChunks());
    }

    // 실드 초기화(부활, 죽음)
    public void ClearShield()
    {
        shieldChunks.Clear();
        SetShield(0);
    }

    // 화면 동기용 합계 표시
    public void ApplySyncedShield(int value)
    {
        SetShield(value);
    }

    // 시간 지나면 제거
    private void TickShield(float deltaTime)
    {
        if (shieldChunks.Count == 0) return;

        bool expired = false;
        for (int i = shieldChunks.Count - 1; i >= 0; i--)
        {
            ShieldChunk chunk = shieldChunks[i];
            chunk.RemainingTime -= deltaTime;

            if( chunk.RemainingTime <= 0f)
            {
                shieldChunks.RemoveAt(i);
                expired = true;
            }
            else
            {
                shieldChunks[i] = chunk;
            }
                
        }

        if (expired)
            SetShield(SumShieldChunks());
    }

    private int SumShieldChunks()
    {
        int sum = 0;
        foreach (ShieldChunk chunk in shieldChunks) sum += chunk.Amount;
        return sum;
    }

    private void SetShield(int value)
    {
        if (currentShield == value) return;
        currentShield = value;
        OnShieldChanged?.Invoke(currentShield);
    }


    // 행동 시 스태미나 소모. 모아 둔 회복량도 버리고 회복 지연을 다시 건다.
    public void SpendStamina(int cost)
    {
        SetStamina(currentStamina - cost);
        regenProgress = 0f;
        regenDelayRemaining = data.StaminaRegenDelay;
    }

    // 스태미나 자연 회복
    public void Tick(float deltaTime)
    {
        TickShield(deltaTime);

        if (owner.IsDead) return;
        if (currentStamina >= MaxStamina)
        {
            regenProgress = 0f;
            return;
        }

        if (regenDelayRemaining > 0f)
        {
            regenDelayRemaining -= deltaTime;
            if (regenDelayRemaining > 0f) return;

            deltaTime = -regenDelayRemaining;
            regenDelayRemaining = 0f;
        }

        regenProgress += data.StaminaRegenPerSecond * deltaTime;
        int wholeAmount = (int)regenProgress;
        if (wholeAmount <= 0) return;

        regenProgress -= wholeAmount;
        SetStamina(currentStamina + wholeAmount);
    }

    public void RestoreAll()
    {
        regenProgress = 0f;
        regenDelayRemaining = 0f;
        SetStamina(MaxStamina);
        NotifyHpChanged();
    }

    public void LoadStamina(int value)
    {
        SetStamina(value);
    }

    private void SetStamina(int value)
    {
        int clamped = Math.Clamp(value, 0, MaxStamina);
        if (clamped == CurrentStamina) return;

        currentStamina = clamped;
        OnStaminaChanged?.Invoke(CurrentStamina, MaxStamina);
    }
}
