using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SESourceData", menuName = "Scriptable Objects/SESource/SESourceData")]
public class SESourceData : ScriptableObject
{
    [Tooltip("범위 안 대상에게 걸 상태이상 목록. 부여할 때마다 모두 건다")]
    [SerializeField] private List<StatusEffectType> statusTypeList = new();

    [Min(0.05f)]
    [Tooltip("부여 간격(초). Proximity에서는 가장자리 간격")]
    [SerializeField] private float interval = 2f;

    public IReadOnlyList<StatusEffectType> StatusTypeList => statusTypeList;
    public float Interval => interval;
}
