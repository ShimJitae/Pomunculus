using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 로컬 플레이어 주변 화면 사각형을 기준으로 장애물의 가림 여부를 2단계로 판정한다. 씬에 하나 배치한다.
/// 1단계: 사각형으로 만든 카메라→플레이어 절두체와 Occluder AABB를 TestPlanesAABB로 비교해 후보를 고른다.
/// 2단계: 같은 사각형의 격자 지점에서 카메라로 레이를 쏴 후보 콜라이더에 맞는지 확인한다.
/// </summary>
public sealed class OccluderDetector : MonoBehaviour
{
    private const float c_rayLength = 10f;   // 평면을 만들 두 번째 점까지의 거리. 결과에는 영향 없음

    private static OccluderTarget localTarget;

    [Tooltip("가린 장애물에 씌울 투명 머티리얼 (PP_Standard_Material을 복제해 Surface Type을 Transparent로 바꾼 것)")]
    [SerializeField] private Material fadeMaterial;
    [Tooltip("플레이어 주변 화면 사각형의 반 크기. 화면 높이 대비 비율 (0.12 = 12%)")]
    [SerializeField] private float halfSize = 0.12f;
    [Tooltip("정밀 판정 레이 격자 수 (N×N). 2 = 사각형 꼭짓점 4줄. 플레이 시작 시 한 번 읽는다")]
    [SerializeField, Min(2)] private int gridResolution = 2;

    private readonly Plane[] planes = new Plane[5];
    private Camera cam;
    private bool wasDetecting;

    private Vector2[] gridOffsets;     // 사각형 안의 격자 상대 위치(-1 ~ +1). Start에서 한 번 만든다
    private Ray[] sampleRays;          // 격자 지점 → 카메라 레이
    private float[] sampleDistances;   // 각 레이의 길이

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
        gridOffsets = BuildGridOffsets(gridResolution);
        sampleRays = new Ray[gridOffsets.Length];
        sampleDistances = new float[gridOffsets.Length];
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

        Vector3 playerCenter = localTarget.CenterPosition;
        BuildPlanes(playerCenter);
        BuildSampleRays(playerCenter);   // planes[4]를 쓰므로 BuildPlanes 다음에 호출한다
        wasDetecting = true;

        IReadOnlyList<Occluder> occluders = Occluder.All;
        for (int i = 0; i < occluders.Count; i++)
        {
            Occluder occluder = occluders[i];
            bool hit = occluder.gameObject.activeInHierarchy
                    && GeometryUtility.TestPlanesAABB(planes, occluder.Bounds)   // 1단계
                    && BlocksAnySample(occluder);                                // 2단계
            occluder.SetOccluding(hit, fadeMaterial);
        }
    }

    /// <summary>사각형을 N×N으로 나눈 지점들의 상대 위치를 만든다. (-1,-1) = 왼쪽 아래, (1,1) = 오른쪽 위</summary>
    private static Vector2[] BuildGridOffsets(int n)
    {
        Vector2[] offsets = new Vector2[n * n];
        for (int y = 0; y < n; y++)
        {
            for (int x = 0; x < n; x++)
            {
                offsets[y * n + x] = new Vector2(x / (n - 1f) * 2f - 1f, y / (n - 1f) * 2f - 1f);
            }
        }
        return offsets;
    }

    /// <summary>
    /// 미리 정한 격자 지점마다 플레이어 깊이 평면과의 교점을 구하고,
    /// 그 점에서 카메라 쪽으로 되돌아가는 레이를 만든다
    /// </summary>
    private void BuildSampleRays(Vector3 playerCenter)
    {
        Vector3 vp = cam.WorldToViewportPoint(playerCenter);
        float halfX = halfSize / cam.aspect;
        float halfY = halfSize;

        for (int i = 0; i < gridOffsets.Length; i++)
        {
            Vector2 offset = gridOffsets[i];
            Ray camRay = cam.ViewportPointToRay(new Vector3(vp.x + offset.x * halfX, vp.y + offset.y * halfY, 0f));
            planes[4].Raycast(camRay, out float distance);

            sampleRays[i] = new Ray(camRay.GetPoint(distance), -camRay.direction);
            sampleDistances[i] = distance;
        }
    }

    /// <summary>격자 레이 중 하나라도 이 물체에 맞으면 가린 것으로 본다</summary>
    private bool BlocksAnySample(Occluder occluder)
    {
        for (int i = 0; i < sampleRays.Length; i++)
        {
            if (occluder.Blocks(sampleRays[i], sampleDistances[i]))
            {
                return true;
            }
        }
        return false;
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