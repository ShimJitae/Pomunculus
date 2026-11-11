using UnityEngine;

/// <summary>중심에 가까울수록 짧은 간격으로 플레이어에게 상태이상을 건다. 범위는 SO의 Radius를 따른다.</summary>
[RequireComponent(typeof(SphereCollider))]
public sealed class SESourceProximity : SESource
{
    [SerializeField] private SESourceProximityData data;

    protected override SESourceData Data => data;

    protected override void Awake()
    {
        base.Awake();
        if (data != null) GetComponent<SphereCollider>().radius = data.Radius; // 스케일 1 기준
    }

    protected override float GetInterval(Entity target)
    {
        float distance = Vector3.Distance(transform.position, target.transform.position);
        float distanceRatio = Mathf.Clamp01(distance / data.Radius);
        return Mathf.Lerp(data.MinInterval, data.Interval, distanceRatio);
    }

    // 선택하지 않아도 씬 뷰에 감지 범위를 항상 표시한다
    private void OnDrawGizmos()
    {
        if (data == null) return;
        Gizmos.color = new Color(0.6f, 0.2f, 1f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, data.Radius);
    }
}
