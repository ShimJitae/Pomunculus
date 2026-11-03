using System.Collections.Generic;
using UnityEngine;

public class StatusEffectController : MonoBehaviour
{
    private List<StatusEffect> activeStatusEffects;
    [SerializeField] private int activeStatusEffectsCapacity = 20; // 초기 상태 효과 갯수
    [SerializeField] private Entity entity; // 상태 효과를 적용할 엔티티

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
                // 상태 지속 시간이 끝나면 풀에 반납 및 리스트에서 제거
                StatusEffectPool.Return(activeStatusEffects[i]);
                activeStatusEffects.RemoveAt(i);
                continue;
            }

            bool isActiveTick = activeStatusEffects[i].Tick(Time.deltaTime);
            if (isActiveTick)
            {
                // 상태 효과 발동
            }
        }
    }

    public void RegisterStatusEffect(StatusEffectType seType)
    {
        StatusEffect statusEffect = StatusEffectPool.Get();
        statusEffect.Initialize(seType);
        activeStatusEffects.Add(statusEffect);
    }
}
