using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 로컬 플레이어 주변 화면 영역으로 카메라→플레이어 작은 절두체를 만들고,
/// 매 프레임 Occluder들과 겹치는지 TestPlanesAABB로 검사해 가림 여부를 알려준다. 씬에 하나 배치한다.
/// </summary>
public sealed class OccluderDetector : MonoBehaviour
{
    private const float c_rayLength = 10f;   // 평면을 만들 두 번째 점까지의 거리. 결과에는 영향 없음

    private static OccluderTarget localTarget;

    [Tooltip("가린 장애물에 씌울 투명 머티리얼 (PP_Standard_Material을 복제해 Surface Type을 Transparent로 바꾼 것)")]
    [SerializeField] private Material fadeMaterial;
    [Tooltip("플레이어 주변 화면 사각형의 반 크기. 화면 높이 대비 비율 (0.12 = 12%)")]
    [SerializeField] private float halfSize = 0.12f;

    private readonly Plane[] planes = new Plane[5];
    private Camera cam;
    private bool wasDetecting;

    /// <summary>로컬 플레이어가 자신을 검사 기준으로 지정한다</summary>
    public static void SetTarget(OccluderTarget target) => localTarget = target;

    /// <summary>지정했던 플레이어가 꺼질 때 기준을 비운다. 다른 대상이 지정돼 있으면 건드리지 않는다</summary>
    public static void ClearTarget(OccluderTarget target)
    {
        if (localTarget == target)
        {
            localTarget = null;
        }
    }

    private void Start()
    {
        cam = Camera.main;
    }

    private void Update()
    {
        if (cam == null || localTarget == null)
        {
            // 기준이 사라지면(사망·퇴장) 반투명했던 장애물을 되돌린다
            if (wasDetecting)
            {
                ClearAll();
            }
            return;
        }

        BuildPlanes(localTarget.CenterPosition);
        wasDetecting = true;

        IReadOnlyList<Occluder> occluders = Occluder.All;
        for (int i = 0; i < occluders.Count; i++)
        {
            Occluder occluder = occluders[i];
            bool hit = occluder.gameObject.activeInHierarchy
                    && GeometryUtility.TestPlanesAABB(planes, occluder.Bounds);
            occluder.SetOccluding(hit, fadeMaterial);
        }
    }

    /// <summary>플레이어 주변 화면 사각형을 지나는 평면 4장과 플레이어 깊이 평면 1장을 만든다</summary>
    private void BuildPlanes(Vector3 playerCenter)
    {
        Vector3 vp = cam.WorldToViewportPoint(playerCenter);
        float halfX = halfSize / cam.aspect;   // 가로가 길어지지 않게 화면 비율을 맞춘다
        float halfY = halfSize;

        Ray bottomLeft = cam.ViewportPointToRay(new Vector3(vp.x - halfX, vp.y - halfY, 0f));
        Ray bottomRight = cam.ViewportPointToRay(new Vector3(vp.x + halfX, vp.y - halfY, 0f));
        Ray topLeft = cam.ViewportPointToRay(new Vector3(vp.x - halfX, vp.y + halfY, 0f));
        Ray topRight = cam.ViewportPointToRay(new Vector3(vp.x + halfX, vp.y + halfY, 0f));
        Vector3 inside = cam.ViewportPointToRay(new Vector3(vp.x, vp.y, 0f)).GetPoint(c_rayLength);

        planes[0] = MakePlane(topLeft, bottomLeft, inside);          // 왼쪽
        planes[1] = MakePlane(bottomRight, topRight, inside);        // 오른쪽
        planes[2] = MakePlane(bottomLeft, bottomRight, inside);      // 아래
        planes[3] = MakePlane(topRight, topLeft, inside);            // 위
        planes[4] = new Plane(-cam.transform.forward, playerCenter); // 플레이어 뒤쪽 제외
    }

    // 두 레이가 지나는 평면을 만들고 법선이 안쪽(inside)을 향하게 맞춘다.
    // 레이의 시작점을 쓰므로 원근·직교 카메라 모두에서 동작한다
    private static Plane MakePlane(Ray a, Ray b, Vector3 inside)
    {
        Plane plane = new Plane(a.origin, a.GetPoint(c_rayLength), b.GetPoint(c_rayLength));
        if (!plane.GetSide(inside))
        {
            plane.Flip();
        }
        return plane;
    }

    private void ClearAll()
    {
        IReadOnlyList<Occluder> occluders = Occluder.All;
        for (int i = 0; i < occluders.Count; i++)
        {
            occluders[i].SetOccluding(false, fadeMaterial);
        }
        wasDetecting = false;
    }
}