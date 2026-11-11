using System;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;
using Object = UnityEngine.Object;

// 지역별 채집물/일반 몬스터 소환 관리 (방장 전용)
// 플레이어가 지역에 들어오면 SpawnPointRegistry의 포인트에 오브젝트를 소환하고,
// 지역에 플레이어가 한 명도 없게 되면 상태만 기록에 남기고 디스폰한다. 다시 들어오면 기록대로 소환
// 기록(WorldData.RegionGathers / RegionMobs)은 날짜가 바뀌기 전까지 유지되므로, 채집하거나 처치해서 사라진 자리는 다음 날까지 비어 있다
public class RegionSpawner : Singleton<RegionSpawner>
{
    public RegionSpawner()
    {
        m_categories = new[]
        {
            new Category
            {
                GetPoints = (registry, region) => registry.GetGatherMobPoints(region),
                GetRecords = data => data.RegionGathers,
                Pick = region => m_table.PickGather(region),
            },
            new Category
            {
                GetPoints = (registry, region) => registry.GetNormalMobPoints(region),
                GetRecords = data => data.RegionMobs,
                Pick = region => m_table.PickMob(region),
            },
        };
    }

    // WorldManager.Init에서 호출
    public void Init()
    {
        m_table = Resources.Load<RegionSpawnTable>("RegionSpawnTable");
        if (m_table == null) Debug.LogWarning("[RegionSpawner] Resources/RegionSpawnTable 이 없어 지역 오브젝트를 소환하지 않음");
        m_emptyTime.Clear();
        m_checkTimer = 0f;
    }
    // WorldManager.Release에서 호출. 월드 오브젝트는 WorldManager가 정리하고, 기록은 WorldData에 저장된 상태로 남는다
    public void Release()
    {
        m_table = null;
        m_emptyTime.Clear();
    }

    // GameManager.Update에서 매 프레임 호출
    public void Update(float deltaTime)
    {
        var world = WorldManager.Instance;
        if (m_table == null || !IsMaster || !world.IsReady || world.CurWorldData == null) return;
        var registry = SpawnPointRegistry.Instance;
        if (registry == null) return;

        m_checkTimer += deltaTime;
        if (m_checkTimer < CheckInterval) return;
        float elapsed = m_checkTimer;
        m_checkTimer = 0f;

        CollectOccupied();
        var data = world.CurWorldData;
        foreach (RegionType region in Regions)
        {
            if (m_occupied.Contains(region))
            {
                m_emptyTime.Remove(region);
                foreach (var cat in m_categories)
                {
                    var points = cat.GetPoints(registry, region);
                    if (points.Count == 0) continue;
                    var rec = GetRecord(cat, region, points);
                    RefreshDay(cat, region, rec, points);
                    if (!rec.Active) Activate(rec, points);
                }
                continue;
            }

            // 비어 있는 지역의 날짜 갱신은 다음 입장 때 처리
            if (!HasActive(data, region)) continue;
            m_emptyTime.TryGetValue(region, out float empty);
            empty += elapsed;
            m_emptyTime[region] = empty;
            if (empty < m_table.DespawnDelay) continue;
            m_emptyTime.Remove(region);
            foreach (var cat in m_categories)
            {
                if (cat.GetRecords(data).TryGetValue(region, out var rec) && rec.Active) Deactivate(rec);
            }
        }
    }

    // 플레이어가 한 명이라도 있는 지역. 원격 플레이어도 방장 쪽에서 동기화된 위치로 PlayerRegionTracker가 갱신됨
    private void CollectOccupied()
    {
        m_occupied.Clear();
        foreach (var obj in WorldManager.Instance.Objects.GetList())
        {
            if (obj is not IPlayerObj || !IsAlive(obj)) continue;
            if (!obj.gameObject.TryGetComponent(out PlayerRegionTracker tracker)) continue;
            if (tracker.CurrentRegion != RegionType.None) m_occupied.Add(tracker.CurrentRegion);
        }
    }

    private bool HasActive(WorldData data, RegionType region)
    {
        foreach (var cat in m_categories)
        {
            if (cat.GetRecords(data).TryGetValue(region, out var rec) && rec.Active) return true;
        }
        return false;
    }

    private RegionSpawnRecord GetRecord(Category cat, RegionType region, IReadOnlyList<Transform> points)
    {
        var data = WorldManager.Instance.CurWorldData;
        var records = cat.GetRecords(data);
        if (records.TryGetValue(region, out var rec) && rec.Points.Count == points.Count) return rec;
        // 맵 수정으로 포인트 수가 달라졌으면 기존 기록은 버리고 새로 시작
        if (rec != null) Deactivate(rec);
        rec = new RegionSpawnRecord { Day = data.Day };
        for (int i = 0; i < points.Count; i++) rec.Points.Add(new SpawnPointRecord { PrefabId = cat.Pick(region) });
        records[region] = rec;
        return rec;
    }

    // 날짜가 바뀌었으면 사라진 자리와 보관 중인 상태를 새로 채운다. 지금 나와 있는 오브젝트는 그대로 둠
    private static void RefreshDay(Category cat, RegionType region, RegionSpawnRecord rec, IReadOnlyList<Transform> points)
    {
        int day = WorldManager.Instance.CurWorldData.Day;
        if (rec.Day == day) return;
        rec.Day = day;
        for (int i = 0; i < rec.Points.Count; i++)
        {
            var p = rec.Points[i];
            if (rec.Active && IsAlive(p.ObjectId)) continue;
            p.PrefabId = cat.Pick(region);
            p.ObjectId = -1;
            p.Consumed = false;
            p.State = null;
            if (rec.Active) SpawnPoint(p, points[i]);
        }
    }

    // 사라지지 않은 포인트를 전부 소환. 보관한 상태가 있으면 적용
    private static void Activate(RegionSpawnRecord rec, IReadOnlyList<Transform> points)
    {
        for (int i = 0; i < rec.Points.Count; i++)
        {
            var p = rec.Points[i];
            if (p.Consumed || IsAlive(p.ObjectId)) continue;
            SpawnPoint(p, points[i]);
        }
        rec.Active = true;
    }

    // 나와 있는 오브젝트의 상태를 보관하고 디스폰. 이미 사라졌거나 죽는 중이면 사라진 것으로 기록
    private static void Deactivate(RegionSpawnRecord rec)
    {
        foreach (var p in rec.Points)
        {
            if (p.ObjectId == -1) continue;
            var obj = WorldManager.Instance.Find(p.ObjectId);
            if (IsAlive(obj))
            {
                // 몬스터는 사망 연출 동안 남아 있다가 스스로 디스폰하므로 여기서 같이 정리
                bool dying = obj is Entity entity && entity.CheckDie();
                if (!dying)
                {
                    p.State = new SaveData();
                    obj.SaveData(p.State);
                }
                p.Consumed = dying;
                obj.Despawn();
            }
            else p.Consumed = true;
            p.ObjectId = -1;
        }
        rec.Active = false;
    }

    // 몬스터가 돌아다녔더라도 다시 소환할 때는 원래 포인트에서
    private static void SpawnPoint(SpawnPointRecord p, Transform point)
    {
        if (string.IsNullOrEmpty(p.PrefabId) || point == null) return;
        var state = p.State;
        var obj = WorldManager.Instance.Spawn(p.PrefabId, point.position, point.rotation,
            state != null ? o => o.LoadData(state) : null);
        p.ObjectId = obj?.ID ?? -1;
        p.State = null;
    }

    private static bool IsAlive(int id) => id != -1 && IsAlive(WorldManager.Instance.Find(id));
    // 채집물이 WorldManager를 거치지 않고 Destroy되는 경우가 있어 목록에 남아 있어도 파괴 여부를 확인
    private static bool IsAlive(IWorldObj obj) => obj is Object o ? o != null : obj != null;

    // 소환 대상 종류(채집물/몬스터)마다 포인트, 기록, 프리팹 선택만 다르다
    private class Category
    {
        public Func<SpawnPointRegistry, RegionType, IReadOnlyList<Transform>> GetPoints;
        public Func<WorldData, Dictionary<RegionType, RegionSpawnRecord>> GetRecords;
        public Func<RegionType, string> Pick;
    }

    private static bool IsMaster => !PhotonNetwork.InRoom || PhotonNetwork.IsMasterClient; // 오프라인이면 본인이 방장
    private static readonly RegionType[] Regions =
        Array.FindAll((RegionType[])Enum.GetValues(typeof(RegionType)), r => r != RegionType.None);
    private const float CheckInterval = 0.25f;

    private readonly Category[] m_categories;
    private RegionSpawnTable m_table;
    private float m_checkTimer;
    private readonly HashSet<RegionType> m_occupied = new();
    private readonly Dictionary<RegionType, float> m_emptyTime = new(); // 지역이 빈 뒤 흐른 시간
}
