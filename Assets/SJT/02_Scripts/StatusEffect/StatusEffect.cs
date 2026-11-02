using UnityEngine;

public sealed class StatusEffect
{
    private StatusEffectData statusEffectData;
    private int stackCount;
    private float remainingDuration;
    private float tickTimer;

    public StatusEffect()
    {
        Reset();
    }

    // 초기 상태 등록 시 초기화
    public void Initialize(StatusEffectType seType)
    {
        statusEffectData = StatusEffectManager.Instance.GetSOData(seType);
        stackCount = 1;
        remainingDuration = statusEffectData.Duration;
        tickTimer = 0f;
    }

    // 상태이상 풀에 반납하기 전에 SO 참조·중첩·타이머 등을 초기화
    public void Reset()
    {
        statusEffectData = null;
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

    public int StatusId => statusEffectData.StatusId;
    public float TickSec => statusEffectData.TickSec;
    public int StackCount => stackCount;
}
