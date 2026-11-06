using Pomunculus.Items;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// PlayerThrower가 던질 포물선 궤적을 LineRenderer로 미리 그려 주는 테스트용 스크립트.
/// Target은 마우스 아래 지면을 따라다니고(followMouse), PlayerThrower 값을 바꾸면 궤적이 바로 갱신된다.
/// Space를 누르면 실제 투사체를 던져 궤적과 비교할 수 있다.
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class ParabolaPreview : MonoBehaviour
{
    [SerializeField] private PlayerThrower thrower;
    [SerializeField] private Transform target;
    [SerializeField] private int segmentCount = 30;
    [SerializeField] private bool applyThrowRange = true;   // 게임과 같이 최대 사거리로 목표를 제한
    [SerializeField] private bool followMouse = true;       // 끄면 Scene 뷰에서 Target을 직접 옮긴다
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private PotionData potion;

    private LineRenderer lineRenderer;
    private Camera mainCamera;
    private Vector3 goal;
    

    void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.useWorldSpace = true;
        mainCamera = Camera.main;
    }

    // 매 프레임 Target 위치와 궤적을 갱신하고, Space 입력 시 실제로 던진다
    void Update()
    {
        if (thrower == null || target == null) return;

        if (followMouse && TryGetMouseWorldPoint(out Vector3 mousePoint))
            target.position = mousePoint;

        goal = applyThrowRange ? thrower.ClampToThrowRange(target.position) : target.position;
        DrawTrajectory();

        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            thrower.ThrowProjectile(goal, potion);
    }

    // 마우스 아래 지면 위치. 지면이 없으면 현재 Target 높이의 평면으로 대체
    bool TryGetMouseWorldPoint(out Vector3 point)
    {
        point = default;
        if (mainCamera == null || Mouse.current == null) return false;

        Ray ray = mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());

        if (Physics.Raycast(ray, out RaycastHit hit, 500f, groundLayer, QueryTriggerInteraction.Ignore))
        {
            point = hit.point;
            return true;
        }

        var plane = new Plane(Vector3.up, target.position);
        if (!plane.Raycast(ray, out float enter)) return false;
        point = ray.GetPoint(enter);
        return true;
    }

    // 발사 위치부터 착지까지 등가속도 운동 공식으로 점을 찍어 선을 그린다
    void DrawTrajectory()
    {
        Vector3 origin = thrower.GetThrowOrigin();
        Vector3 velocity = thrower.CalculateLaunchVelocity(origin, goal, out float flightTime, out Vector3 gravity);

        int pointCount = Mathf.Max(2, segmentCount + 1);
        lineRenderer.positionCount = pointCount;
        for (int i = 0; i < pointCount; i++)
        {
            float time = flightTime * i / (pointCount - 1);
            lineRenderer.SetPosition(i, origin + velocity * time + 0.5f * time * time * gravity);
        }
    }

    // 사거리 제한이 적용된 실제 착지 지점 표시
    void OnDrawGizmos()
    {
        if (!Application.isPlaying || target == null) return;

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(goal, 0.3f);
    }
}
