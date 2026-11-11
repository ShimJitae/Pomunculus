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

    public float Radius => radius;
    public float MinInterval => Mathf.Min(minInterval, Interval);
}
