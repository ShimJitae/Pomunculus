using System.Collections.Generic;
using UnityEngine;

public class StatusEffectController : MonoBehaviour
{
    private List<StatusEffect> activeStatusEffects;
    [SerializeField] private int activeStatusEffectsCapacity = 20; // 초기 상태 효과 갯수

    private void Awake()
    {
        // 초기 상태 효과 갯수. activeStatusEffectsCapacity 크기로 설정.
        activeStatusEffects = new List<StatusEffect>(activeStatusEffectsCapacity);
    }

    private void Update()
    {
        for (int i = activeStatusEffects.Count - 1; i >= 0; i--)
        {
            bool isActiveTick = activeStatusEffects[i].Tick(Time.deltaTime);
            if (isActiveTick)
            {

            }

            bool isExpired = activeStatusEffects[i].AdvanceTime(Time.deltaTime);
            if (isExpired)
            {
                // 상태 지속 시간이 끝나면 풀에 반납 및 리스트에서 제거
                StatusEffectPool.Return(activeStatusEffects[i]);
                activeStatusEffects.RemoveAt(i);
            }
        }
    }

    public void RegisterStatusEffect(StatusEffectType seType)
    {
        // StatusEffect statusEffect = StatusEffectPool.Get();
        // statusEffect.Initialize(seType);
        // activeStatusEffects.Add(statusEffect);
    }
}
