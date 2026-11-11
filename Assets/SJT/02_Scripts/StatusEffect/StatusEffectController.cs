using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(StatusEffectVisualizer))]
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

    /// <summary>Tick이 실행된 직후 발생한다. 걸린 직후의 즉시 틱에도 발생하며, 한 프레임에 밀린 틱이 여러 번 실행돼도 한 번만 발생한다.</summary>
    public event Action<StatusEffect> OnEffectTicked;

    /// <summary>
    /// 시간 만료, 중첩 소진, 저항 부여로 상태가 해제된 직후, 풀에 반납하기 전에 발생한다.
    /// 대상 파괴로 인한 정리에서는 발생하지 않는다.
    /// </summary>
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
        modifiers = StatusModifiers.Default;
    }

    private void Update()
    {
        for (int i = activeStatusEffects.Count - 1; i >= 0; i--)
        {
            StatusEffect effect = activeStatusEffects[i];

            // 틱을 만료보다 먼저 처리해야 마지막 틱이 만료에 묻히지 않는다.
            if (effect.Tick(Time.deltaTime))
            {
                OnEffectTicked?.Invoke(effect);
                LogStatus("틱", effect);
            }

            if (effect.AdvanceTime(Time.deltaTime))
            {
                RemoveEffectAt(i);
            }
        }
    }

    /// <summary>
    /// 상태이상 바깥(포션·지형·디버그 등)에서 상태를 걸 때 호출한다.
    /// 이 호출은 한 컴퓨터에서만 일어나므로, 멀티에서는 모든 컴퓨터에 알려 각자 ApplyStatusEffectLocal을 실행하게 한다.
    /// IStatusBehavior 안(틱 등)은 모든 컴퓨터에서 똑같이 실행되므로 이 함수 대신 ApplyStatusEffectLocal을 쓴다.
    /// </summary>
    public void RegisterStatusEffect(StatusEffectType seType)
    {
        if (entity == null || seType == StatusEffectType.None)
        {
            return;
        }

        // TODO: Entity에 SendRegisterStatusEffect가 추가되면 entity.SendRegisterStatusEffect(seType)로 교체
        ApplyStatusEffectLocal(seType);
    }

    /// <summary>
    /// 이 컴퓨터에만 상태를 건다. 네트워크 수신과 IStatusBehavior 안의 파생 등록에서 호출한다.
    /// </summary>
    public StatusEffect ApplyStatusEffectLocal(StatusEffectType seType)
    {
        if (entity == null || seType == StatusEffectType.None)
        {
            return null;
        }

        if (TryBlockStatusEffect(seType))
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
        RemoveBlockedEffects(statusEffect);
        RecalculateModifiers();
        OnEffectRegistered?.Invoke(statusEffect);
        LogStatus("걸림", statusEffect);

        // 첫 틱은 걸리자마자 실행한다. 복원은 진행 중이던 틱 타이머를 이어받으므로 실행하지 않는다.
        if (statusEffect.Tick(0f))
        {
            OnEffectTicked?.Invoke(statusEffect);
            LogStatus("틱", statusEffect);
        }
        return statusEffect;
    }

    private void RemoveBlockedEffects(StatusEffect resistance)
    {
        StatusEffectData blocked = resistance.Data.BlockedStatusEffect;
        if (blocked == null || resistance.RemainingDuration <= 0f)
        {
            return;
        }

        for (int i = activeStatusEffects.Count - 1; i >= 0; i--)
        {
            StatusEffect active = activeStatusEffects[i];
            if (active != resistance && active.Type == blocked.StatusEffectType)
            {
                RemoveEffectAt(i);
            }
        }
    }

    private bool TryBlockStatusEffect(StatusEffectType incomingType)
    {
        for (int i = 0; i < activeStatusEffects.Count; i++)
        {
            StatusEffect resistance = activeStatusEffects[i];
            StatusEffectData blocked = resistance.Data.BlockedStatusEffect;
            if (blocked == null || blocked.StatusEffectType != incomingType ||
                resistance.StackCount <= 0 || resistance.RemainingDuration <= 0f)
            {
                continue;
            }

            resistance.StackCount--;
            if (resistance.StackCount == 0)
            {
                RemoveEffectAt(i);
            }
            else
            {
                RecalculateModifiers();
            }

            return true;
        }

        return false;
    }

    private void RemoveEffectAt(int index)
    {
        StatusEffect effect = activeStatusEffects[index];
        activeStatusEffects.RemoveAt(index);
        effect.Remove();
        RecalculateModifiers();
        // 상태 데이터는 풀에 반납하기 전에 구독자와 로그에 전달한다.
        OnEffectRemoved?.Invoke(effect);
        LogStatus("종료", effect);
        StatusEffectPool.Return(effect);
    }

    /// <summary>
    /// 기본값에서 시작해 활성 상태 전체의 몫을 다시 반영한다.
    /// 같은 수치를 쓰는 상태가 겹쳐도 먼저 끝난 쪽이 남은 쪽의 효과를 풀지 않게 한다.
    /// 상태 등록·중첩 변경·해제·복원 때 호출하며, 값이 달라졌을 때만 OnModifiersChanged를 발생시킨다.
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
