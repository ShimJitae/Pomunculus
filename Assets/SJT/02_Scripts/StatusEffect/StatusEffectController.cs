using System;
using System.Collections.Generic;
using UnityEngine;

public class StatusEffectController : MonoBehaviour
{
    [SerializeField] private Entity entity; // 같은 오브젝트에 있는 Entity를 참조.
    private List<StatusEffect> activeStatusEffects;
    [SerializeField] private int activeStatusEffectsCapacity = 20; // 초기 상태 효과 갯수

    /// <summary>새 상태가 걸린 직후 발생한다. 연출 등 로직 밖의 반응에 사용한다.</summary>
    public event Action<StatusEffect> D_EffectRegistered;

    /// <summary>이미 걸린 상태가 다시 걸려 중첩된 직후 발생한다.</summary>
    public event Action<StatusEffect> D_EffectReapplied;

    /// <summary>상태의 발동 주기가 돌아 Tick이 실행된 직후 발생한다.</summary>
    public event Action<StatusEffect> D_EffectTicked;

    /// <summary>상태가 만료되어 해제된 직후, 풀에 반납되기 전에 발생한다. 대상 파괴로 인한 정리에서는 발생하지 않는다.</summary>
    public event Action<StatusEffect> D_EffectRemoved;

    /// <summary>현재 활성 상태 목록을 반환한다. 읽기 전용이며 Awake 전에는 null이다.</summary>
    public IReadOnlyList<StatusEffect> ActiveStatusEffects => activeStatusEffects;

    private void Awake()
    {
        // 초기 상태 효과 갯수. activeStatusEffectsCapacity 크기로 설정.
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
                D_EffectRemoved?.Invoke(effect); // 풀 반납(Reset) 전에 알려야 구독자가 데이터를 읽을 수 있다.
                LogStatus("종료", effect); // 풀 반납(Reset) 전에 기록해야 데이터를 읽을 수 있다.
                StatusEffectPool.Return(effect);
                continue;
            }

            if (effect.Tick(Time.deltaTime))
            {
                D_EffectTicked?.Invoke(effect);
                LogStatus("틱", effect);
            }
        }
    }

    /// <summary>대상이 파괴될 때 남은 상태를 해제하고 실행 객체를 풀에 반납한다.</summary>
    private void OnDestroy()
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

    public void RegisterStatusEffect(StatusEffectType seType)
    {
        if (entity == null || seType == StatusEffectType.None)
        {
            return;
        }

        for (int i = 0; i < activeStatusEffects.Count; i++)
        {
            if (activeStatusEffects[i].Type == seType)
            {
                activeStatusEffects[i].Reapply();
                RecalculateModifiers();
                D_EffectReapplied?.Invoke(activeStatusEffects[i]);
                LogStatus("중첩", activeStatusEffects[i]);
                return;
            }
        }

        StatusEffect statusEffect = StatusEffectPool.Get();

        if (!statusEffect.Initialize(seType, entity))
        {
            StatusEffectPool.Return(statusEffect);
            return;
        }

        statusEffect.Register();
        activeStatusEffects.Add(statusEffect);
        RecalculateModifiers();
        D_EffectRegistered?.Invoke(statusEffect);
        LogStatus("걸림", statusEffect);
    }

    /// <summary>
    /// 대상의 상태이상 수치를 기본값으로 되돌린 뒤 활성 상태 전체의 몫을 다시 반영한다.
    /// 같은 수치를 쓰는 상태가 겹쳐도 먼저 끝난 쪽이 남은 쪽의 효과를 풀지 않게 한다.
    /// 상태가 걸리거나·중첩되거나·끝날 때만 호출한다.
    /// </summary>
    private void RecalculateModifiers()
    {
        if (entity == null)
        {
            return;
        }

        // 상태이상 수치 7개를 기본값으로 되돌린 뒤, 남아 있는 상태의 몫만 다시 반영한다.
        entity.ResetStatusModifiers();

        for (int i = 0; i < activeStatusEffects.Count; i++)
        {
            activeStatusEffects[i].ApplyModifier();
        }
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
}
