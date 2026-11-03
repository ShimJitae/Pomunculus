using UnityEngine;

[CreateAssetMenu(fileName = "StatusEffectData", menuName = "Scriptable Objects/StatusEffectData")]
public class StatusEffectData : ScriptableObject
{
    [Tooltip("상태를 구분하는 고유 ID")]
    [SerializeField] private int statusId;

    [Tooltip("상태 효과의 카테고리. Burn, Freeze 등")]
    [SerializeField] private StatusEffectType statusEffectType;

    [Tooltip("화상, 냉기 등 화면에 표시할 상태 이름")]
    [SerializeField] private string stateName;

    [Min(1)]
    [Tooltip("최대 중첩 수. 1 이상으로 설정")]
    [SerializeField] private int maxStackCount = 1;

    [Min(0f)]
    [Tooltip("상태 효과의 총 지속 시간. 단위는 초")]
    [SerializeField] private float duration = 1f;

    [Min(0f)]
    [Tooltip("상태 효과의 발동 간격. 단위는 초")]
    [SerializeField] private float tickSec = 0.2f;

    [Min(0)]
    [Tooltip("중첩 하나당 한 번의 주기 발동에 적용할 피해량. 0이면 피해를 주지 않음")]
    [SerializeField] private int tickDamagePerStack;

    public int StatusId => statusId;
    public StatusEffectType StatusEffectType => statusEffectType;
    public string StateName => stateName;
    public int MaxStackCount => maxStackCount;

    public float Duration => duration;

    public float TickSec => tickSec;

    /// <summary>중첩 하나당 한 번의 주기 발동에 적용할 피해량을 반환한다.</summary>
    public int TickDamagePerStack => tickDamagePerStack;
}
