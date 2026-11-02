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
        for (int i = 0; i < activeStatusEffects.Count; i++)
        {
            activeStatusEffects[i].Tick(Time.deltaTime);
        }
    }
}
