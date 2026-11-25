using System;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;
using Object = UnityEngine.Object;

// 지역별 채집물/일반 몬스터/필드 보스 소환 관리 (방장 전용)
// 플레이어가 지역에 들어오면 SpawnPointRegistry의 포인트에 오브젝트를 소환하고,
// 지역에 플레이어가 한 명도 없게 되면 상태만 기록에 남기고 디스폰한다. 다시 들어오면 기록대로 소환
// 기록(WorldData.RegionGathers / RegionMobs / RegionBosses)은 카테고리의 재소환 주기 동안 유지되므로,
// 채집하거나 처치해서 사라진 자리는 채집물/몬스터는 다음 날까지, 보스는 BossRespawnDays일 뒤까지 비어 있다
public class RegionSpawner : Singleton<RegionSpawner>
{
    public RegionSpawner()
    {
        m_mobCategory = new Category
        {
            GetPoints = (registry, region) => registry.GetNormalMobPoints(region),
            GetRecords = data => data.RegionMobs,
            Pick = region => m_table.PickMob(MobTableRegion(region)),
        };
        m_categories = new[]
        {
            new Category
            {
                GetPoints = (registry, region) => registry.GetGatherMobPoints(region),
                GetRecords = data => data.RegionGathers,
                Pick = region => m_table.PickGather(region),
            },
            m_mobCategory,
            new Category
            {
                GetPoints = (registry, region) => SinglePoint(registry.GetBossPoint(region)),
                GetRecords = data => data.RegionBosses,
                Pick = region => m_table.PickFieldBoss(region),
                RespawnDays = BossRespawnDays,
                KeepUntilConsumed = true,
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
        WorldManager.Instance.OnSeasonChangedEvent -= OnSeasonChanged;
        WorldManager.Instance.OnSeasonChangedEvent += OnSeasonChanged;
        var chat = ChatManager.Instance;
        if (chat != null)
        {
            chat.OnCommandEvent -= OnCommand;
            chat.OnCommandEvent += OnCommand;
        }
    }
    // WorldManager.Release에서 호출. 월드 오브젝트는 WorldManager가 정리하고, 기록은 WorldData에 저장된 상태로 남는다
    public void Release()
    {
        WorldManager.Instance.OnSeasonChangedEvent -= OnSeasonChanged;
        var chat = ChatManager.Instance;
        if (chat != null) chat.OnCommandEvent -= OnCommand;
        m_table = null;
        m_emptyTime.Clear();
    }

    // 계절이 바뀌면 계절마다 몬스터 목록이 달라지는 지역의 일반 몬스터를 전부 정리하고 새 계절 몬스터로 다시 채운다
    // 플레이어가 있는 지역은 바로 다시 소환하고, 비어 있는 지역은 기록만 바꿔 두었다가 다음 입장 때 소환
    private void OnSeasonChanged(Season season)
    {
        var data = WorldManager.Instance.CurWorldData;
        if (m_table == null || !IsMaster || data == null) return;
        var registry = SpawnPointRegistry.Instance;
        foreach (RegionType region in Regions)
        {
            if (!HasSeasonalMobs(region)) continue;
            if (!m_mobCategory.GetRecords(data).TryGetValue(region, out var rec)) continue;
            bool active = rec.Active;
            foreach (var p in rec.Points)
            {
                // 사망 연출 중인 개체도 같이 정리
                if (IsAlive(p.ObjectId)) WorldManager.Instance.Find(p.ObjectId).Despawn();
                p.PrefabId = m_mobCategory.Pick(region);
                p.ObjectId = -1;
                p.Consumed = false;
                p.State = null;
            }
            rec.Active = false;
            rec.Day = data.Day; // 같은 날 RefreshDay에서 다시 고르지 않도록
            if (!active || registry == null) continue;
            var points = m_mobCategory.GetPoints(registry, region);
            if (points.Count == rec.Points.Count) Activate(rec, points);
        }
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
        UpdateFinalBoss(data, registry);
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
                    if (rec.Active) CheckConsumed(rec);
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

    #region 최종보스
    // 결계와 최종보스는 카테고리 기록에 넣지 않으므로 지역이 비거나 날짜가 지나도 디스폰/재소환되지 않는다
    // 결계 : 결계 지역에 플레이어가 처음 들어왔을 때 보스 포인트에 소환. 어떤 이유로든 사라졌으면 다시 세운다
    // 최종보스 : 결계가 풀릴 때 SummonFinalBoss로 소환. 처치되면 기록만 남기고 다시 소환하지 않는다
    private void UpdateFinalBoss(WorldData data, SpawnPointRegistry registry)
    {
        var rec = data.FinalBoss ??= new FinalBossRecord();
        switch (rec.Stage)
        {
            case FinalBossStage.None:
            case FinalBossStage.Barrier:
            {
                if (IsAlive(rec.ObjectId)) return;
                RegionType region = m_table.BarrierRegion;
                if (!m_occupied.Contains(region)) return;
                var point = registry.GetBossPoint(region);
                if (point == null || string.IsNullOrEmpty(m_table.BarrierPrefabId)) return;
                var obj = WorldManager.Instance.Spawn(m_table.BarrierPrefabId, point.position, point.rotation);
                if (obj == null) return;
                rec.ObjectId = obj.ID;
                rec.Stage = FinalBossStage.Barrier;
                break;
            }
            case FinalBossStage.Boss:
            {
                var boss = WorldManager.Instance.Find(rec.ObjectId);
                if (IsAlive(boss) && !(boss is Entity entity && entity.CheckDie())) return;
                // 사망 연출 중인 개체는 스스로 디스폰하므로 기록만 바꾼다
                rec.Stage = FinalBossStage.Defeated;
                rec.ObjectId = -1;
                break;
            }
        }
    }

    // 방장 전용. 해제 조건을 확인한 결계가 호출. 결계 자리에 최종보스를 소환하고 기록을 바꾼다. 결계 디스폰은 호출한 쪽에서
    // 이미 풀렸거나 기록과 다른 결계, 또는 최종보스 소환에 실패하면 false
    public bool SummonFinalBoss(IWorldObj barrier)
    {
        var data = WorldManager.Instance.CurWorldData;
        if (m_table == null || !IsMaster || data == null || barrier == null) return false;
        var rec = data.FinalBoss ??= new FinalBossRecord();
        if (rec.Stage != FinalBossStage.Barrier || rec.ObjectId != barrier.ID) return false;

        var t = barrier.gameObject.transform;
        var boss = WorldManager.Instance.Spawn(m_table.FinalBossPrefabId, t.position, t.rotation);
        if (boss == null)
        {
            Debug.LogError($"[RegionSpawner] 최종보스 소환 실패 : {m_table.FinalBossPrefabId}");
            return false;
        }
        rec.Stage = FinalBossStage.Boss;
        rec.ObjectId = boss.ID;
        return true;
    }
    #endregion

    #region 테스트 명령어
    // 채팅 명령어는 내 클라이언트에서만 실행되므로 방장이 아니어도 사용 가능
    //  /barrieritems [개수] : 결계 해제에 필요한 아이템을 내 인벤토리에 개수만큼(기본 1) 추가
    private void OnCommand(string command, string[] args)
    {
        switch (command)
        {
            case "barrieritems":
                GiveBarrierItems(args);
                break;
        }
    }

    private void GiveBarrierItems(string[] args)
    {
        var chat = ChatManager.Instance;
        int amount = 1;
        if (args.Length > 0 && (!int.TryParse(args[0], out amount) || amount <= 0))
        {
            chat.Notify("사용법 : /barrieritems [개수]");
            return;
        }
        PlayerFSM player = null;
        foreach (var obj in WorldManager.Instance.Objects.GetList())
        {
            if (obj is PlayerFSM p && p != null && p.IsMine) { player = p; break; }
        }
        if (player == null || player.Inventory == null)
        {
            chat.Notify("내 플레이어를 찾을 수 없습니다.");
            return;
        }
        var barrier = FindBarrierPrefab();
        if (barrier == null)
        {
            chat.Notify($"결계 프리팹을 찾을 수 없습니다 : {(m_table != null ? m_table.BarrierPrefabId : "(스폰 테이블 없음)")}");
            return;
        }
        // 인벤토리 소유자 쪽에서 추가하므로 SyncInventory로 방장 사본까지 맞춰진다
        foreach (int itemId in barrier.RequiredItemIds) player.Inventory.AddItem(itemId, amount);
        chat.Notify($"결계 해제 아이템을 {amount}개씩 추가했습니다.");
    }

    // 결계에 설정된 필요 아이템을 그대로 쓰기 위해 카탈로그에서 결계 프리팹을 찾는다
    private Barrier FindBarrierPrefab()
    {
        if (m_table == null) return null;
        var catalog = Resources.Load<NetworkPrefabCatalog>("NetworkPrefabCatalog");
        if (catalog == null) return null;
        foreach (var prefab in catalog.Entries)
        {
            if (prefab != null && prefab.name == m_table.BarrierPrefabId) return prefab.GetComponent<Barrier>();
        }
        return null;
    }
    #endregion

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

    // 나와 있는 동안 사라지거나 죽는 중인 오브젝트를 그 날짜로 기록. 재소환 주기 계산에 쓴다
    // 사망 연출 중인 몬스터는 ObjectId를 남겨 두어 Deactivate에서 같이 정리되게 한다
    private static void CheckConsumed(RegionSpawnRecord rec)
    {
        foreach (var p in rec.Points)
        {
            if (p.Consumed || p.ObjectId == -1) continue;
            var obj = WorldManager.Instance.Find(p.ObjectId);
            if (!IsAlive(obj) || (obj is Entity entity && entity.CheckDie())) MarkConsumed(p);
        }
    }

    // 날짜가 바뀌었으면 재소환 주기가 지난 자리와 보관 중인 상태를 새로 채운다. 지금 나와 있는 오브젝트는 그대로 둠
    private static void RefreshDay(Category cat, RegionType region, RegionSpawnRecord rec, IReadOnlyList<Transform> points)
    {
        int day = WorldManager.Instance.CurWorldData.Day;
        if (rec.Day == day) return;
        rec.Day = day;
        for (int i = 0; i < rec.Points.Count; i++)
        {
            var p = rec.Points[i];
            if (p.Consumed)
            {
                if (day - p.ConsumedDay < cat.RespawnDays) continue;
                // 사망 연출이 하루를 넘길 일은 없지만, 아직 남아 있으면 정리하고 새로 소환
                if (rec.Active && IsAlive(p.ObjectId)) WorldManager.Instance.Find(p.ObjectId).Despawn();
            }
            else
            {
                if (rec.Active && IsAlive(p.ObjectId)) continue;
                // 보스처럼 처치되기 전까지 유지하는 대상은 보관 중인 상태(체력 등)를 그대로 둔다
                if (cat.KeepUntilConsumed && !string.IsNullOrEmpty(p.PrefabId)) continue;
            }
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
                bool dying = p.Consumed || (obj is Entity entity && entity.CheckDie());
                if (!dying)
                {
                    p.State = new SaveData();
                    obj.SaveData(p.State);
                }
                else MarkConsumed(p);
                obj.Despawn();
            }
            else MarkConsumed(p);
            p.ObjectId = -1;
        }
        rec.Active = false;
    }

    private static void MarkConsumed(SpawnPointRecord p)
    {
        if (p.Consumed) return;
        p.Consumed = true;
        p.ConsumedDay = WorldManager.Instance.CurWorldData.Day;
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

    private static bool HasSeasonalMobs(RegionType region) => region == RegionType.Forest;

    // 숲의 일반 몬스터는 계절에 따라 다른 지역의 몬스터 목록에서 고른다 (봄: 숲, 여름: 화산, 가을: 독늪, 겨울: 설산)
    private static RegionType MobTableRegion(RegionType region)
    {
        if (!HasSeasonalMobs(region)) return region;
        return WorldManager.Instance.CurSeason switch
        {
            Season.Summer => RegionType.Volcano,
            Season.Autumn => RegionType.Swamp,
            Season.Winter => RegionType.SnowyMountain,
            _ => RegionType.Forest,
        };
    }

    // 보스 포인트는 지역마다 하나라서 다른 카테고리와 같은 목록 형태로 맞춘다
    private static IReadOnlyList<Transform> SinglePoint(Transform point) =>
        point != null ? new[] { point } : Array.Empty<Transform>();

    // 소환 대상 종류(채집물/몬스터/보스)마다 포인트, 기록, 프리팹 선택, 재소환 규칙이 다르다
    private class Category
    {
        public Func<SpawnPointRegistry, RegionType, IReadOnlyList<Transform>> GetPoints;
        public Func<WorldData, Dictionary<RegionType, RegionSpawnRecord>> GetRecords;
        public Func<RegionType, string> Pick;
        public int RespawnDays = 1; // 사라진 날로부터 이 일수가 지나야 다시 소환
        public bool KeepUntilConsumed; // true면 날짜가 바뀌어도 처치되기 전까지는 같은 개체(보관 상태)를 유지
    }

    private static bool IsMaster => !PhotonNetwork.InRoom || PhotonNetwork.IsMasterClient; // 오프라인이면 본인이 방장
    private static readonly RegionType[] Regions =
        Array.FindAll((RegionType[])Enum.GetValues(typeof(RegionType)), r => r != RegionType.None);
    private const float CheckInterval = 0.25f;
    private const int BossRespawnDays = 10; // 필드 보스는 처치 후 10일이 지나야 다시 소환

    private readonly Category[] m_categories;
    private readonly Category m_mobCategory; // 계절이 바뀔 때 따로 다시 채우는 일반 몬스터
    private RegionSpawnTable m_table;
    private float m_checkTimer;
    private readonly HashSet<RegionType> m_occupied = new();
    private readonly Dictionary<RegionType, float> m_emptyTime = new(); // 지역이 빈 뒤 흐른 시간
}
