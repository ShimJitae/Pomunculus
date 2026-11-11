using System;
using System.Collections.Generic;
using UnityEngine;

// 지역별 스폰 위치를 모아 두는 컴포넌트. World 루트에 붙인다.
// 일반 몹·채집물: 루트 Transform을 연결하면 그 직속 자식(Point_N, Gather_N)이 스폰 위치가 된다.
// 보스·플레이어: 위치 Transform 하나를 연결한다. 보스가 없는 지역은 비워 둔다.
[DisallowMultipleComponent]
public class SpawnPointRegistry : MonoBehaviour
{
    public static SpawnPointRegistry Instance { get; private set; }

    [Header("숲")]
    [SerializeField] private Transform forestNormalMobs;
    [SerializeField] private Transform forestGathers;
    [SerializeField] private Transform forestPlayerSpawnPoint;

    [Header("화산")]
    [SerializeField] private Transform volcanoNormalMobs;
    [SerializeField] private Transform volcanoGathers;
    [SerializeField] private Transform volcanoBoss;
    [SerializeField] private Transform volcanoPlayerSpawnPoint;

    [Header("설산")]
    [SerializeField] private Transform snowyNormalMobs;
    [SerializeField] private Transform snowyGathers;
    [SerializeField] private Transform snowyBoss;
    [SerializeField] private Transform snowyPlayerSpawnPoint;

    [Header("독늪")]
    [SerializeField] private Transform swampNormalMobs;
    [SerializeField] private Transform swampGathers;
    [SerializeField] private Transform swampBoss;
    [SerializeField] private Transform swampPlayerSpawnPoint;

    private readonly Dictionary<RegionType, List<Transform>> normalDic = new();
    private readonly Dictionary<RegionType, List<Transform>> gatherDic = new();
    private readonly Dictionary<RegionType, Transform> bossDic = new();
    private readonly Dictionary<RegionType, Transform> spawnPointDic = new();

    // 지역의 일반 몹 스폰 위치들. 없으면 빈 리스트
    public IReadOnlyList<Transform> GetNormalMobPoints(RegionType region)
    {
        return normalDic.TryGetValue(region, out var list) ? list : Array.Empty<Transform>();
    }

    // 지역의 채집물 위치들. 없으면 빈 리스트
    public IReadOnlyList<Transform> GetGatherMobPoints(RegionType region)
    {
        return gatherDic.TryGetValue(region, out var list) ? list : Array.Empty<Transform>();
    }

    // 지역의 보스 스폰 위치. 보스가 없는 지역이면 null
    public Transform GetBossPoint(RegionType region)
    {
        return bossDic.TryGetValue(region, out var point) ? point : null;
    }

    // 지역의 플레이어 스폰 위치. 없으면 null
    public Transform GetPlayerSpawnPoint(RegionType region)
    {
        return spawnPointDic.TryGetValue(region, out var point) ? point : null;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[SpawnPointRegistry] 씬에 두 개 이상 있음. 나중에 생성된 것으로 교체", this);
        }
        Instance = this;
        Build();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Build()
    {
        normalDic.Clear();
        gatherDic.Clear();
        bossDic.Clear();
        spawnPointDic.Clear();

        AddNormalMob(RegionType.Forest, forestNormalMobs);
        AddNormalMob(RegionType.Volcano, volcanoNormalMobs);
        AddNormalMob(RegionType.SnowyMountain, snowyNormalMobs);
        AddNormalMob(RegionType.Swamp, swampNormalMobs);

        AddGather(RegionType.Forest, forestGathers);
        AddGather(RegionType.Volcano, volcanoGathers);
        AddGather(RegionType.SnowyMountain, snowyGathers);
        AddGather(RegionType.Swamp, swampGathers);

        AddBoss(RegionType.Volcano, volcanoBoss);
        AddBoss(RegionType.SnowyMountain, snowyBoss);
        AddBoss(RegionType.Swamp, swampBoss);

        AddPlayerSpawnPoint(RegionType.Volcano, volcanoPlayerSpawnPoint);
        AddPlayerSpawnPoint(RegionType.SnowyMountain, snowyPlayerSpawnPoint);
        AddPlayerSpawnPoint(RegionType.Swamp, swampPlayerSpawnPoint);
        AddPlayerSpawnPoint(RegionType.Forest, forestPlayerSpawnPoint);
    }

    private void AddNormalMob(RegionType region, Transform root)
    {
        if (root == null) return;
        var list = new List<Transform>(root.childCount);
        foreach (Transform point in root)
        {
            list.Add(point); // 직속 자식만
        }
        normalDic[region] = list;
    }

    private void AddGather(RegionType region, Transform root)
    {
        if (root == null) return;
        var list = new List<Transform>(root.childCount);
        foreach (Transform point in root)
        {
            list.Add(point); // 직속 자식만
        }
        gatherDic[region] = list;
    }

    private void AddBoss(RegionType region, Transform point)
    {
        if (point == null) return;
        bossDic[region] = point;
    }

    private void AddPlayerSpawnPoint(RegionType region, Transform point)
    {
        if (point == null) return;
        spawnPointDic[region] = point;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        DrawPointList(forestNormalMobs);
        DrawPointList(volcanoNormalMobs);
        DrawPointList(snowyNormalMobs);
        DrawPointList(swampNormalMobs);

        Gizmos.color = Color.blue;
        DrawPointList(forestGathers);
        DrawPointList(volcanoGathers);
        DrawPointList(snowyGathers);
        DrawPointList(swampGathers);

        Gizmos.color = Color.red;
        DrawPoint(volcanoBoss);
        DrawPoint(snowyBoss);
        DrawPoint(swampBoss);

        Gizmos.color = Color.green;
        DrawPoint(forestPlayerSpawnPoint);
        DrawPoint(volcanoPlayerSpawnPoint);
        DrawPoint(snowyPlayerSpawnPoint);
        DrawPoint(swampPlayerSpawnPoint);
    }

    private static void DrawPointList(Transform root)
    {
        if (root == null) return;
        foreach (Transform point in root)
        {
            Gizmos.DrawWireSphere(point.position, 0.5f);
        }
    }

    private static void DrawPoint(Transform point)
    {
        if (point == null)
        {
            return;
        }
        Gizmos.DrawWireSphere(point.position, 1.5f);
    }
}
