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
    private struct ShieldChunk { public int Amount; public float ExpireTime; }
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

    // 실드 추가. 끝나는 시각이 빠른 순서가 유지되도록 알맞은 자리에 끼워 넣는다.
    public void AddShield(int amount, float duration)
    {
        ShieldChunk chunk = new ShieldChunk();
        chunk.Amount = amount;
        chunk.ExpireTime = UnityEngine.Time.time + duration;

        int insertIndex = shieldChunks.FindIndex(existing => existing.ExpireTime > chunk.ExpireTime);
        if (insertIndex < 0) shieldChunks.Add(chunk);
        else shieldChunks.Insert(insertIndex, chunk);

        SetShield(SumShieldChunks());
    }

    // 보호막으로 피해를 흡수하고, 흡수하지 못한 남은 피해를 돌려준다.
    // shieldChunks는 끝나는 시각이 빠른 순서로 유지되므로 앞에서부터 깎는다.
    public int AbsorbDamage(int damage)
    {
        if (damage <= 0 || shieldChunks.Count == 0) return damage;

        int remainingDamage = damage;
        for (int i = 0; i < shieldChunks.Count && remainingDamage > 0; i++)
        {
            ShieldChunk chunk = shieldChunks[i];
            int absorbed = Math.Min(chunk.Amount, remainingDamage);
            chunk.Amount -= absorbed;
            remainingDamage -= absorbed;
            shieldChunks[i] = chunk;
        }

        shieldChunks.RemoveAll(chunk => chunk.Amount <= 0);
        SetShield(SumShieldChunks());
        return remainingDamage;
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

    // 끝나는 시각이 지난 보호막을 앞에서부터 제거한다. 정렬되어 있으므로 맨 앞이 안 끝났으면 멈춘다.
    private void TickShield()
    {
        int expiredCount = 0;
        float now = UnityEngine.Time.time;
        while (expiredCount < shieldChunks.Count && shieldChunks[expiredCount].ExpireTime <= now)
            expiredCount++;

        if (expiredCount == 0) return;

        shieldChunks.RemoveRange(0, expiredCount);
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
        TickShield();

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
