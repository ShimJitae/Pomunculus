// 던진 사람을 착탄 지점 근처의 걸을 수 있는 위치로 순간이동시킨다
using System;
using UnityEngine;
using UnityEngine.AI;

[Serializable]
public class TeleportPotionEffect : PotionEffect
{
    [Tooltip("착탄 지점에서 걸을 수 있는 위치를 찾을 최대 거리. 못 찾으면 이동하지 않는다")]
    [Min(0.1f)][SerializeField] private float navMeshSearchDistance = 3f;

    // 대상마다 적용하는 효과는 없다 (던진 사람만 움직인다)
    public override void Apply(in PotionHitContext context) { }

    // 모든 화면에서 불리지만, 던진 플레이어의 소유자 화면에서만 옮긴다
    public override void OnImpact(in PotionImpactContext context)
    {
        if (context.Thrower == null || !context.Thrower.TryGetComponent(out PlayerFSM thrower)) return;
        if (!thrower.IsMine || thrower.IsDead) return;

        // 벽·적 위에 맞아도 근처 바닥으로 내린다
        if (!NavMesh.SamplePosition(context.ImpactPoint, out NavMeshHit hit, navMeshSearchDistance, NavMesh.AllAreas)) return;
        thrower.Teleport(hit.position);
    }
}
