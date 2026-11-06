using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class StatusEffectController : MonoBehaviour, ISavable
{
    [SerializeField] private Entity entity; // 같은 오브젝트에 있는 Entity를 참조.
    private List<StatusEffect> activeStatusEffects;
    public List<StatusEffect> ActiveStatusEffects { get => activeStatusEffects; set => activeStatusEffects = value; }
    [SerializeField] private int activeStatusEffectsCapacity = 20; // 초기 상태 효과 갯수

    /// <summary>새 상태가 걸린 직후 발생한다. 연출 등 로직 밖의 반응에 사용한다.</summary>
    public event Action<StatusEffect> OnEffectRegistered;

    /// <summary>이미 걸린 상태가 다시 걸려 중첩된 직후 발생한다.</summary>
    public event Action<StatusEffect> OnEffectReapplied;

    /// <summary>상태의 발동 주기가 돌아 Tick이 실행된 직후 발생한다.</summary>
    public event Action<StatusEffect> OnEffectTicked;

    /// <summary>상태가 만료되어 해제된 직후, 풀에 반납되기 전에 발생한다. 대상 파괴로 인한 정리에서는 발생하지 않는다.</summary>
    public event Action<StatusEffect> OnEffectRemoved;

    private StatusModifiers modifiers = StatusModifiers.Default;

    /// <summary>활성 상태 전체가 적용한 현재 수치이다. 늦게 구독한 쪽이 초기값을 맞출 때 읽는다.</summary>
    public StatusModifiers Modifiers => modifiers;

    /// <summary>
    /// 상태이상 수치가 실제로 바뀐 직후 발생한다. 걸림·중첩·만료·복원 중 값이 달라질 때만 호출된다.
    /// 수치를 바꾸지 않는 상태(예: 화상)가 걸리거나 끝날 때는 발생하지 않는다.
    /// </summary>
    public event Action<StatusModifiers> OnModifiersChanged;

    private void OnEnable()
    {
        // 초기 상태 효과 갯수. activeStatusEffectsCapacity 크기로 설정.
        if (activeStatusEffects == null)
            activeStatusEffects = new List<StatusEffect>(activeStatusEffectsCapacity);

        if (entity == null)
        {
            if (gameObject.TryGetComponent(out Entity _entity))
            {
                entity = _entity;
            }
            else
            {
#if UNITY_EDITOR
                Debug.LogError("[StatusEffectController] Entity가 할당되지 않았습니다.", this);
#endif
            }
        }
    }

    /// <summary>대상이 파괴될 때 남은 상태를 해제하고 실행 객체를 풀에 반납한다.</summary>
    private void OnDisable()
    {
        if (activeStatusEffects == null)
        {
            return;
        }

        for (int i = activeStatusEffects.Count - 1; i >= 0; i--)
        {
            StatusEffect effect = activeStatusEffects[i];
            activeStatusEffects.RemoveAt(i);
            effect.Remove();
            StatusEffectPool.Return(effect);
        }
    }

    private void Update()
    {
        for (int i = activeStatusEffects.Count - 1; i >= 0; i--)
        {
            StatusEffect effect = activeStatusEffects[i];

            bool isExpired = effect.AdvanceTime(Time.deltaTime);
            if (isExpired)
            {
                activeStatusEffects.RemoveAt(i);
                effect.Remove();
                RecalculateModifiers(); // 목록에서 뺀 뒤 계산해야 남은 상태의 효과만 반영된다.
                OnEffectRemoved?.Invoke(effect); // 풀 반납(Reset) 전에 알려야 구독자가 데이터를 읽을 수 있다.
                LogStatus("종료", effect); // 풀 반납(Reset) 전에 기록해야 데이터를 읽을 수 있다.
                StatusEffectPool.Return(effect);
                continue;
            }

            if (effect.Tick(Time.deltaTime))
            {
                OnEffectTicked?.Invoke(effect);
                LogStatus("틱", effect);
            }
        }
    }

    public StatusEffect RegisterStatusEffect(StatusEffectType seType)
    {
        if (entity == null || seType == StatusEffectType.None)
        {
            return null;
        }

        for (int i = 0; i < activeStatusEffects.Count; i++)
        {
            if (activeStatusEffects[i].Type == seType)
            {
                activeStatusEffects[i].Reapply();
                RecalculateModifiers();
                OnEffectReapplied?.Invoke(activeStatusEffects[i]);
                LogStatus("중첩", activeStatusEffects[i]);
                return activeStatusEffects[i];
            }
        }

        StatusEffect statusEffect = StatusEffectPool.Get();

        if (!statusEffect.Initialize(seType, entity))
        {
            StatusEffectPool.Return(statusEffect);
            return null;
        }

        statusEffect.Register();
        activeStatusEffects.Add(statusEffect);
        RecalculateModifiers();
        OnEffectRegistered?.Invoke(statusEffect);
        LogStatus("걸림", statusEffect);
        return statusEffect;
    }

    /// <summary>
    /// 기본값에서 시작해 활성 상태 전체의 몫을 다시 반영한다.
    /// 같은 수치를 쓰는 상태가 겹쳐도 먼저 끝난 쪽이 남은 쪽의 효과를 풀지 않게 한다.
    /// 상태가 걸리거나·중첩되거나·끝날 때만 호출하며, 값이 달라졌을 때만 OnModifiersChanged를 발생시킨다.
    /// </summary>
    private void RecalculateModifiers()
    {
        StatusModifiers next = StatusModifiers.Default;

        for (int i = 0; i < activeStatusEffects.Count; i++)
        {
            activeStatusEffects[i].ApplyModifier(ref next);
        }

        if (next == modifiers)
        {
            return;
        }

        modifiers = next;
        OnModifiersChanged?.Invoke(modifiers);
    }

    /// <summary>상태 단계와 대상의 상태이상 수치를 기록한다. 빌드에서는 호출 자체가 제거된다.</summary>
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    private void LogStatus(string phase, StatusEffect effect)
    {
#if UNITY_EDITOR
        if (!logStatusEffects || entity == null)
        {
            return;
        }

        Debug.Log($"[SE:{phase}] {name} | {effect.Type} x{effect.StackCount} 남은 {effect.RemainingDuration:F2}s | " +
                  $"Move {entity.MoveSpeedRatio:F2} Heal {entity.HealReceivedRatio:F2} Dmg {entity.DamageTakenRatio:F2} | " +
                  $"CanMove {entity.CanMove} Atk {entity.CanAttack} Dodge {entity.CanDodge} Inv {entity.IsInvincible} | " +
                  $"HP {entity.CurrentHp}", this);
#endif
    }

#if UNITY_EDITOR
    [Header("디버그")]
    [Tooltip("걸림·중첩·틱·종료 시점에 콘솔 로그를 남긴다")]
    [SerializeField] private bool logStatusEffects = true;
    [SerializeField] private StatusEffectType debugStatusEffectType = StatusEffectType.Burn;

    [ContextMenu("디버그/선택한 상태이상 걸기")]
    private void DebugRegisterStatusEffect()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[StatusEffectController] 플레이 모드에서만 실행할 수 있습니다.", this);
            return;
        }

        RegisterStatusEffect(debugStatusEffectType);
    }
#endif

    #region 다른 플레이어가 월드에 들어왔을 때, 보낼 상태이상 데이터
    public void SaveData(SaveData _data)
    {
        PacketData d = new();
        List<int> type = new List<int>();
        List<int> stackCount = new List<int>();
        List<float> duration = new List<float>();
        List<float> tickTimer = new List<float>();
        foreach (var a in activeStatusEffects)
        {
            type.Add((int)a.Type);
            stackCount.Add(a.StackCount);
            duration.Add(a.RemainingDuration);
            tickTimer.Add(a.TickTimer);
        }
        d.WriteList(type, d.Write);
        d.WriteList(stackCount, d.Write);
        d.WriteList(duration, d.Write);
        d.WriteList(tickTimer, d.Write);
        d.Save();
        _data.SetData(d.GetData());
    }

    public void LoadData(SaveData _data)
    {
        var p = new PacketData(_data.GetData<byte[]>());
        List<int> type = p;
        List<int> stackCount = p;
        List<float> duration = p;
        List<float> tickTimer = p;
        int count = Mathf.Min(type.Count, Mathf.Min(stackCount.Count, duration.Count));
        for (int i = 0; i < count; i++)
        {
            RestoreStatusEffect((StatusEffectType)type[i], stackCount[i], duration[i], tickTimer[i]);
        }
    }

    private void RestoreStatusEffect(StatusEffectType type, int stack, float remaining, float tickTimer)
    {
        StatusEffect effect = StatusEffectPool.Get();
        if (!effect.Initialize(type, entity))
        {
            StatusEffectPool.Return(effect); // StatusEffectManager가 아직 준비되지 않았을 때 등
            return;
        }

        effect.Register();                        // OnRegistered 1회 실행. 남은 시간이 SO 값으로 초기화된다
        effect.StackCount = Mathf.Max(1, stack);  // 그래서 Register 다음에 덮어쓴다
        effect.RemainingDuration = remaining;
        effect.TickTimer = tickTimer;

        activeStatusEffects.Add(effect);
        RecalculateModifiers();                   // 중첩 수가 반영된 수치로 다시 계산
        OnEffectRegistered?.Invoke(effect);       // 연출은 상태마다 한 번만
        LogStatus("복원", effect);
    }
    #endregion
}
