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

    // SO 생성자에서는 LayerMask.GetMask를 쓸 수 없어 인덱스로 둔다. 3 = Player
    [Tooltip("상태이상을 걸 Entity의 레이어. 이 밖의 레이어와는 트리거 이벤트도 생기지 않는다")]
    [SerializeField] private LayerMask targetLayers = 1 << 3;

    public IReadOnlyList<SEApplyEntry> ApplyList => applyList;
    public float Interval => interval;
    public LayerMask TargetLayers => targetLayers;
}
