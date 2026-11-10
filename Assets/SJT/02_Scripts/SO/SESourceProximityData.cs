using UnityEngine;

[CreateAssetMenu(fileName = "SESourceProximityData", menuName = "Scriptable Objects/SESource/SESourceProximityData")]
public class SESourceProximityData : SESourceData
{
    [Min(0.1f)]
    [Tooltip("감지 범위. SphereCollider 반경이 이 값으로 맞춰진다")]
    [SerializeField] private float radius = 5f;

    [Min(0.05f)]
    [Tooltip("중심에서의 부여 간격(초). Interval(가장자리)보다 크면 Interval을 쓴다")]
    [SerializeField] private float minInterval = 0.5f;

    [Tooltip("x: 거리 비율(0=중심, 1=가장자리), y: 간격 비율(0=minInterval, 1=Interval)")]
    [SerializeField] private AnimationCurve curve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

    public float Radius => radius;
    public float MinInterval => Mathf.Min(minInterval, Interval);
    public AnimationCurve Curve => curve;
}
