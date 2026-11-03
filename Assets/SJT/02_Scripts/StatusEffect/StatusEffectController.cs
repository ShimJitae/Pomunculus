using System.Collections.Generic;
using UnityEngine;

public class StatusEffectController : MonoBehaviour
{
    [SerializeField] private Entity entity; // 같은 오브젝트에 있는 Entity를 참조.
    private List<StatusEffect> activeStatusEffects;
    [SerializeField] private int activeStatusEffectsCapacity = 20; // 초기 상태 효과 갯수

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
            bool isExpired = activeStatusEffects[i].AdvanceTime(Time.deltaTime);
            if (isExpired)
            {
                StatusEffect effect = activeStatusEffects[i];

                activeStatusEffects.RemoveAt(i);
                effect.Remove();
                StatusEffectPool.Return(effect);
                continue;
            }

            activeStatusEffects[i].Tick(Time.deltaTime);
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
    }
}
