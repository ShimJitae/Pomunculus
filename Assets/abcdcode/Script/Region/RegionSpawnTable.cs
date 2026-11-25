using System;
using System.Collections.Generic;
using UnityEngine;

// 지역별로 채집/몬스터/보스 포인트에 소환할 프리팹 목록. Resources/RegionSpawnTable 에 두면 RegionSpawner가 불러온다
// 포인트마다 목록에서 하나를 무작위로 골라 하루 동안 유지한다. 필드 보스는 처치될 때까지 유지하고, 처치 후 10일 뒤 다시 고른다
[CreateAssetMenu(menuName = "Pomunculus/RegionSpawnTable")]
public class RegionSpawnTable : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        public RegionType Region;
        public List<string> GatherPrefabIds = new(); // NetworkPrefabCatalog 이름
        public List<string> MobPrefabIds = new();
        public List<string> FieldBossPrefabIds = new(); // 지금은 지역마다 1종이지만 여러 종을 넣으면 무작위로 고른다
    }

    // 지역에 플레이어가 없어진 뒤 디스폰하기까지 기다리는 시간. 경계에서 들락날락할 때 매번 다시 소환되지 않도록
    public float DespawnDelay => m_despawnDelay;

    // 최종보스 : BarrierRegion의 보스 포인트에 결계를 한 번 소환하고, 결계가 풀리면 그 자리에 최종보스를 소환한다
    public RegionType BarrierRegion => m_barrierRegion;
    public string BarrierPrefabId => m_barrierPrefabId;
    public string FinalBossPrefabId => m_finalBossPrefabId;

    // 목록이 비어 있으면 null
    public string PickGather(RegionType region) => Pick(region, e => e.GatherPrefabIds);
    public string PickMob(RegionType region) => Pick(region, e => e.MobPrefabIds);
    public string PickFieldBoss(RegionType region) => Pick(region, e => e.FieldBossPrefabIds);

    private string Pick(RegionType region, Func<Entry, List<string>> selector)
    {
        foreach (var entry in m_entries)
        {
            if (entry.Region != region) continue;
            var ids = selector(entry);
            if (ids.Count == 0) continue;
            return ids[UnityEngine.Random.Range(0, ids.Count)];
        }
        return null;
    }

    [SerializeField] private List<Entry> m_entries = new();
    [SerializeField] private float m_despawnDelay = 3f;

    [Header("최종보스")]
    [SerializeField] private RegionType m_barrierRegion = RegionType.Forest;
    [SerializeField] private string m_barrierPrefabId = "Barrier"; // NetworkPrefabCatalog 이름
    [SerializeField] private string m_finalBossPrefabId = "Enemy_Slime"; // 임시 이름
}
