using System.Collections.Generic;
using UnityEngine;

/// <summary>한 번 부여할 때 걸 상태이상과 쌓을 중첩 수</summary>
[System.Serializable]
public struct SEApplyEntry
{
    public StatusEffectType type;
    [Min(1)] public int stackCount;
}

[CreateAssetMenu(fileName = "SESourceData", menuName = "Scriptable Objects/SESource/SESourceData")]
public class SESourceData : ScriptableObject
{
    [Tooltip("범위 안 대상에게 걸 상태이상과 한 번에 쌓을 중첩 수. 부여할 때마다 모두 건다")]
    [SerializeField] private List<SEApplyEntry> applyList = new();

    [Min(0.05f)]
    [Tooltip("부여 간격(초). Proximity에서는 가장자리 간격")]
    [SerializeField] private float interval = 2f;

    [Tooltip("이 레이어의 Entity에게는 상태이상을 걸지 않는다")]
    [SerializeField] private LayerMask ignoreLayers;

    public IReadOnlyList<SEApplyEntry> ApplyList => applyList;
    public float Interval => interval;
    public LayerMask IgnoreLayers => ignoreLayers;
}
