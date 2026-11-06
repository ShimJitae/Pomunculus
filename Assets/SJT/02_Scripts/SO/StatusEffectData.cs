using UnityEngine;

[CreateAssetMenu(fileName = "StatusEffectData", menuName = "Scriptable Objects/StatusEffectData")]
public class StatusEffectData : ScriptableObject
{
    [Tooltip("상태를 구분하는 고유 ID")]
    [SerializeField] private int statusId;

    [Tooltip("상태 효과의 카테고리. Burn, Freeze 등")]
    [SerializeField] private StatusEffectType statusEffectType;

    [Tooltip("상태의 분류. 버프·디버프·저주 중 하나로 지정")]
    [SerializeField] private EffectCategory effectCategory;

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

    [Min(0f)]
    [Tooltip("상태별 수치. 냉기: 스택당 감속률(0.1 = 10%), 독: 스택당 회복 감소율, 점액·젖음: 감속률(스택 무관), " +
             "취약: 스택당 받는 피해 증가율, 헤이스트: 스택당 이동속도 증가율. 기절·빙결·구속·석화·무적은 사용하지 않음")]
    [SerializeField] private float valuePerStack;

    [Header("연출")]
    [Tooltip("이 상태의 공용 Feel 연출 프리팹. 비우면 상태 로직만 실행")]
    [SerializeField] private StatusEffectFeedback feedbackPrefab;

    [Tooltip("HUD 상태이상 패널에 표시할 아이콘")]
    [SerializeField] private Sprite icon;

    public int StatusId => statusId;
    public StatusEffectType StatusEffectType => statusEffectType;

    /// <summary>상태의 분류(버프·디버프·저주)를 반환한다.</summary>
    public EffectCategory EffectCategory => effectCategory;
    public string StateName => stateName;
    public int MaxStackCount => maxStackCount;

    public float Duration => duration;

    public float TickSec => tickSec;

    /// <summary>중첩 하나당 한 번의 주기 발동에 적용할 피해량을 반환한다.</summary>
    public int TickDamagePerStack => tickDamagePerStack;

    /// <summary>상태별 수치를 반환한다. 의미는 상태 종류마다 다르다.</summary>
    public float ValuePerStack => valuePerStack;

    /// <summary>상태별로 대상 아래에 생성할 공용 Feel 연출 프리팹을 반환한다.</summary>
    public StatusEffectFeedback FeedbackPrefab => feedbackPrefab;

    /// <summary>상태별로 띄울 아이콘 이미지를 반환한다.</summary>
    public Sprite Icon => icon;
}
