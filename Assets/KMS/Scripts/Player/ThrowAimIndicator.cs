using Pomunculus.Items;
using UnityEngine;

// Shift(Aim)를 누르는 동안 포션 궤적과 착탄 범위를 내 화면에만 표시한다
public class ThrowAimIndicator : MonoBehaviour
{
    [SerializeField] private LineRenderer trajectoryLine;
    [SerializeField] private LineRenderer rangeCircle;          // loop = true
    [SerializeField] private int trajectorySegmentCount = 30;
    [SerializeField] private int circleSegmentCount = 40;
    [SerializeField] private LayerMask obstacleLayers;          // 지면·벽. 플레이어 레이어는 제외
    [SerializeField] private float circleHeightOffset = 0.05f;  // 바닥에 묻히지 않게 살짝 띄움

    private PlayerFSM player;

    void Awake()
    {
        player = GetComponent<PlayerFSM>();
        SetVisible(false);
    }

    // 상태·입력이 이번 프레임에 반영된 뒤 그리도록 LateUpdate에서 갱신한다
    void LateUpdate()
    {
        if (!player.IsMine || player.IsDead || !player.Input.IsAimHeld
            || !player.Input.TryGetMouseWorldPoint(out Vector3 mousePoint))
        {
            SetVisible(false);
            return;
        }

        Vector3 goal = player.Thrower.ClampToThrowRange(mousePoint);
        Vector3 impactPoint = DrawTrajectory(goal);
        DrawRangeCircle(impactPoint, GetImpactRadius());
        SetVisible(true);
    }

    // 포물선을 그리다가 장애물에 먼저 닿으면 거기서 끊는다. 실제 착탄 지점을 돌려준다
    Vector3 DrawTrajectory(Vector3 goal)
    {
        Vector3 origin = player.Thrower.GetThrowOrigin();
        Vector3 velocity = player.Thrower.CalculateLaunchVelocity(origin, goal, out float flightTime, out Vector3 gravity);

        trajectoryLine.positionCount = trajectorySegmentCount + 1;
        trajectoryLine.SetPosition(0, origin);
        Vector3 previous = origin;

        for (int i = 1; i <= trajectorySegmentCount; i++)
        {
            float time = flightTime * i / trajectorySegmentCount;
            Vector3 next = origin + velocity * time + 0.5f * time * time * gravity;

            if (Physics.Linecast(previous, next, out RaycastHit hit, obstacleLayers, QueryTriggerInteraction.Ignore))
            {
                trajectoryLine.SetPosition(i, hit.point);
                trajectoryLine.positionCount = i + 1;
                return hit.point;
            }

            trajectoryLine.SetPosition(i, next);
            previous = next;
        }
        return goal;
    }

    // 착탄 지점을 중심으로 피격 반경 원을 그린다
    void DrawRangeCircle(Vector3 center, float radius)
    {
        rangeCircle.positionCount = circleSegmentCount;
        for (int i = 0; i < circleSegmentCount; i++)
        {
            float angle = 2f * Mathf.PI * i / circleSegmentCount;
            Vector3 offset = new Vector3(Mathf.Cos(angle) * radius, circleHeightOffset, Mathf.Sin(angle) * radius);
            rangeCircle.SetPosition(i, center + offset);
        }
    }

    // 지금 던질 투사체 프리팹의 피격 반경
    float GetImpactRadius()
    {
        ThrownProjectile prefab = player.Thrower.GetProjectilePrefab(player.GetThrowablePotion());
        return prefab != null ? prefab.ImpactRadius : 0f;
    }

    void SetVisible(bool visible)
    {
        trajectoryLine.enabled = visible;
        rangeCircle.enabled = visible;
    }
}