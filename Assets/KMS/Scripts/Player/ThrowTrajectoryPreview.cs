using Pomunculus.Items;
using UnityEngine;

/// <summary>
/// 발사 위치와 목표 지점을 받아 포션 궤적과 착탄 범위 원을 그린다.
/// 플레이어(ThrowAimIndicator)와 분신(PlayerClone)이 같이 쓴다. 언제 보일지는 쓰는 쪽이 정한다.
/// </summary>
public class ThrowTrajectoryPreview : MonoBehaviour
{
    [SerializeField] private LineRenderer trajectoryLine;
    [SerializeField] private LineRenderer rangeCircle;          // loop = true
    [SerializeField] private int trajectorySegmentCount = 30;
    [SerializeField] private int circleSegmentCount = 40;
    [Tooltip("궤적이 멈추는 지면·벽. 플레이어 레이어는 제외")]
    [SerializeField] private LayerMask obstacleLayers = (1 << 0) | (1 << 6);   // Default + Ground
    [SerializeField] private float circleHeightOffset = 0.05f;  // 바닥에 묻히지 않게 살짝 띄움

    void Awake() => Hide();

    // thrower의 궤적 계산으로 origin → goal 포물선과 실제 착탄 지점의 범위 원을 그린다
    public void Show(PlayerThrower thrower, Vector3 origin, Vector3 goal, PotionData potion)
    {
        if (thrower == null || trajectoryLine == null || rangeCircle == null) return;

        ThrownProjectile prefab = thrower.GetProjectilePrefab(potion);
        Vector3 impactPoint = DrawTrajectory(thrower, origin, goal, prefab);
        DrawRangeCircle(impactPoint, GetRangeRadius(potion, prefab));
        SetVisible(true);
    }

    // 궤적과 범위 원을 숨긴다
    public void Hide() => SetVisible(false);

    // 투사체와 같은 크기의 구를 궤적을 따라 밀어 보며, 먼저 닿는 곳에서 끊는다. 실제 착탄 지점을 돌려준다
    private Vector3 DrawTrajectory(PlayerThrower thrower, Vector3 origin, Vector3 goal, ThrownProjectile prefab)
    {
        Vector3 centerOffset = Vector3.zero;
        float radius = 0f;
        if (prefab != null) prefab.TryGetCollisionSphere(out centerOffset, out radius);

        Vector3 velocity = thrower.CalculateLaunchVelocity(origin, goal, out float flightTime, out Vector3 gravity);

        trajectoryLine.positionCount = trajectorySegmentCount + 1;
        trajectoryLine.SetPosition(0, origin);
        Vector3 previous = origin;

        for (int i = 1; i <= trajectorySegmentCount; i++)
        {
            float time = flightTime * i / trajectorySegmentCount;
            Vector3 next = origin + velocity * time + 0.5f * time * time * gravity;

            Vector3 step = next - previous;
            float distance = step.magnitude;
            Vector3 direction = step / distance;

            // 실제 충돌 구는 피벗에서 centerOffset만큼 떨어진 곳에 있다
            if (Physics.SphereCast(previous + centerOffset, radius, direction, out RaycastHit hit,
                                   distance, obstacleLayers, QueryTriggerInteraction.Ignore))
            {
                trajectoryLine.SetPosition(i, previous + direction * hit.distance);   // 포션이 멈추는 위치
                trajectoryLine.positionCount = i + 1;

                // 시작부터 겹쳐 있으면 hit.point가 (0,0,0)으로 나온다
                return hit.distance > 0f ? hit.point : previous;
            }

            trajectoryLine.SetPosition(i, next);
            previous = next;
        }
        return goal;
    }

    // 착탄 지점을 중심으로 피격 반경 원을 그린다
    private void DrawRangeCircle(Vector3 center, float radius)
    {
        rangeCircle.positionCount = circleSegmentCount;
        for (int i = 0; i < circleSegmentCount; i++)
        {
            float angle = 2f * Mathf.PI * i / circleSegmentCount;
            Vector3 offset = new Vector3(Mathf.Cos(angle) * radius, circleHeightOffset, Mathf.Sin(angle) * radius);
            rangeCircle.SetPosition(i, center + offset);
        }
    }

    // 투사체 착탄 반경과 효과 범위(장판 등) 중 가장 큰 값
    private static float GetRangeRadius(PotionData potion, ThrownProjectile prefab)
    {
        float radius = prefab != null ? prefab.ImpactRadius : 0f;
        if (potion == null) return radius;

        foreach (PotionEffect effect in potion.Effects)
        {
            if (effect != null)
                radius = Mathf.Max(radius, effect.AreaRadius);
        }
        return radius;
    }

    private void SetVisible(bool visible)
    {
        if (trajectoryLine != null) trajectoryLine.enabled = visible;
        if (rangeCircle != null) rangeCircle.enabled = visible;
    }
}
