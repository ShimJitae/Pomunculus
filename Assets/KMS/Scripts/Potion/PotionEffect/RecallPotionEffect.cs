// 맞은 플레이어를 지정한 지역의 플레이어 스폰 위치로 돌려보낸다 (분신·적·채집물은 무시)
using System;
using UnityEngine;

[Serializable]
public class RecallPotionEffect : PotionEffect
{
    [Tooltip("돌려보낼 지역. 그 지역의 플레이어 스폰 위치(SpawnPointRegistry)로 이동한다")]
    [SerializeField] private RegionType destinationRegion = RegionType.Forest;

    // 맞은 플레이어의 소유자 화면에서만 불린다 (PotionTargeting.HasAuthority)
    public override void Apply(in PotionHitContext context)
    {
        if (context.Target is not PlayerFSM player || player.IsDead) return;

        Transform spawnPoint = SpawnPointRegistry.Instance != null
            ? SpawnPointRegistry.Instance.GetPlayerSpawnPoint(destinationRegion)
            : null;
        if (spawnPoint == null)
        {
            Debug.LogWarning($"[RecallPotionEffect] {destinationRegion} 지역의 플레이어 스폰 위치가 없습니다.");
            return;
        }

        player.Teleport(spawnPoint.position);
    }
}
