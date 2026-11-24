// 착탄 지점에 무언가를 만드는 효과(장판, 오브젝트, 소환)의 공통 기반. 대상별 적용은 하지 않는다
using System;
using UnityEngine;

[Serializable]
public abstract class SpawnPotionEffect : PotionEffect
{
    // 직렬화 클래스 생성자에서는 LayerMask.GetMask를 쓸 수 없어 인덱스로 둔다. 6 = Ground
    [Tooltip("생성물을 붙일 바닥 레이어")]
    [SerializeField] private LayerMask groundLayers = 1 << 6;

    private const float GroundProbeHeight = 0.5f;     // 착탄 지점보다 이만큼 위에서 아래로 쏜다 (바닥에 닿은 경우 대비)
    private const float GroundSearchDistance = 5f;    // 착탄 지점 아래로 바닥을 찾을 최대 거리

    // 생성 효과는 착탄 시 OnImpact에서만 동작한다
    public sealed override void Apply(in PotionHitContext context) { }

    // 기준 지점 바로 아래 바닥 위치. 바닥을 못 찾으면 기준 지점 그대로
    protected Vector3 FindGroundPoint(Vector3 point)
    {
        Vector3 origin = point + Vector3.up * GroundProbeHeight;
        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, GroundProbeHeight + GroundSearchDistance,
                groundLayers, QueryTriggerInteraction.Ignore))
            return hit.point;

        return point;
    }

    /// <summary>
    /// 생성 위치 목록. 1개면 중심, 여러 개면 반경 위에 같은 간격으로 둘러 놓는다.
    /// 모든 클라이언트가 같은 위치를 얻도록 랜덤은 쓰지 않는다. 각 위치는 바닥에 붙인다.
    /// </summary>
    protected Vector3[] GetSpawnPoints(Vector3 center, int count, float spreadRadius)
    {
        count = Mathf.Max(1, count);
        Vector3[] points = new Vector3[count];

        for (int i = 0; i < count; i++)
        {
            Vector3 point = center;
            if (count > 1 && spreadRadius > 0f)
            {
                float angle = 2f * Mathf.PI * i / count;
                point += new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * spreadRadius;
            }
            points[i] = FindGroundPoint(point);
        }
        return points;
    }

    // 던진 사람에서 착탄 지점을 바라보는 수평 회전. 방향을 알 수 없으면 기본 회전
    protected static Quaternion GetThrowDirectionRotation(in PotionImpactContext context)
    {
        if (context.Thrower == null) return Quaternion.identity;

        Vector3 direction = context.ImpactPoint - context.Thrower.transform.position;
        direction.y = 0f;
        return direction.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(direction) : Quaternion.identity;
    }
}
