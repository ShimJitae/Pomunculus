using UnityEngine;

public sealed class StatusEffect
{
    private StatusEffectData statusEffectData;
    private IStatusBehavior statusBehavior;
    private Entity target;
    private int stackCount;
    private float remainingDuration;
    private float tickTimer;

    public StatusEffect()
    {
        Reset();
    }

    // 초기 상태 등록 시 초기화
    public bool Initialize(StatusEffectType seType, Entity target)
    {
        Reset();

        if (target == null || seType == StatusEffectType.None)
        {
            return false;
        }

        StatusEffectManager manager = StatusEffectManager.Instance;
        if (manager == null)
        {
#if UNITY_EDITOR
            Debug.LogError("[StatusEffect] StatusEffectManager가 준비되지 않았습니다.", target);
#endif
            return false;
        }

        statusEffectData = manager.GetSOData(seType);
        statusBehavior = manager.GetStatusBehavior(seType);

        if (statusEffectData == null || statusBehavior == null)
        {
            return false;
        }

        if (statusEffectData.MaxStackCount < 1)
        {
#if UNITY_EDITOR
            Debug.LogError("[StatusEffect] 최대 중첩 수는 1 이상이어야 합니다.", statusEffectData);
#endif
            return false;
        }

        this.target = target;
        stackCount = 1;
        tickTimer = 0f;
        return true;
    }

    /// <summary>활성 목록에 등록된 상태의 최초 동작을 실행한다.</summary>
    public void Register()
    {
        statusBehavior.OnRegistered(this);
        remainingDuration = statusEffectData.Duration;
    }

    // 상태이상 풀에 반납하기 전에 SO 참조·중첩·타이머 등을 초기화
    public void Reset()
    {
        statusEffectData = null;
        statusBehavior = null;
        target = null;
        stackCount = 0;
        remainingDuration = 0f;
        tickTimer = 0f;
    }

    // 이미 걸린 상태의 중첩
    public void Reapply()
    {
        if (statusEffectData == null)
        {
#if UNITY_EDITOR
            Debug.LogError("[StatusEffect] 초기화 되어 있지 않은 상태에서 중첩을 호출했습니다.");
#endif
            return;
        }

        // 최대 중첩 수를 초과할 경우, MaxStackCount로 갱신
        if (stackCount + 1 > statusEffectData.MaxStackCount)
        {
            stackCount = statusEffectData.MaxStackCount;
        }
        else
        {
            stackCount++;
        }

        statusBehavior.OnReapply(this);
        remainingDuration = statusEffectData.Duration;
    }

    /// <summary>
    /// 상태 효과의 Tick 주기를 계산. 
    /// </summary>
    /// <param name="deltaTime">경과 시간</param>
    /// <returns>Tick 진행 여부</returns>
    public bool Tick(float deltaTime)
    {
        if (statusEffectData == null)
        {
#if UNITY_EDITOR
            Debug.LogError("[StatusEffect] 초기화 되어 있지 않은 상태에서 중첩을 호출했습니다.");
#endif
            return false;
        }

        tickTimer += deltaTime;
        if (tickTimer >= statusEffectData.TickSec)
        {
            tickTimer = 0f;
            statusBehavior.OnTick(this);
            return true;
        }

        return false;
    }

    /// <summary>
    /// 상태 이상의 지속 시간을 계산
    /// </summary>
    /// <param name="deltaTime">경과 시간</param>
    /// <returns>지속 시간 종료 여부</returns>
    public bool AdvanceTime(float deltaTime)
    {
        if (statusEffectData == null)
        {
#if UNITY_EDITOR
            Debug.LogError("[StatusEffect] 초기화 되어 있지 않은 상태에서 중첩을 호출했습니다.");
#endif
            return false;
        }

        remainingDuration -= deltaTime;
        if (remainingDuration <= 0f)
        {
            // 상태 효과 종료
            return true;
        }

        return false;
    }

    /// <summary>상태가 적용한 효과를 해제한다.</summary>
    public void Remove()
    {
        statusBehavior.OnRemove(this);
    }

    public StatusEffectType Type => statusEffectData.StatusEffectType; // 현재 상태의 종류이다. 

    /// <summary>상태 효과가 적용되는 대상을 반환한다.</summary>
    public Entity Target => target;

    /// <summary>중첩 하나당 한 번의 주기 발동에 적용할 피해량을 반환한다.</summary>
    public int TickDamagePerStack => statusEffectData.TickDamagePerStack;

    public float TickSec => statusEffectData.TickSec;
    public int StackCount => stackCount;
}
