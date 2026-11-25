// 맞은 채집물의 드롭 수량을 배수만큼 늘리고, 옵션에 따라 바로 채집한다
// 같은 포션에 피해 효과가 있으면 이 효과를 목록 앞에 둬야 한다 (피해가 먼저 채집하면 배수가 안 걸림)
using System;
using UnityEngine;

[Serializable]
public class GatherBoostPotionEffect : PotionEffect
{
    [Tooltip("드롭되는 재료 수량 배수")]
    [Min(1)][SerializeField] private int yieldMultiplier = 2;
    [Tooltip("켜면 맞은 채집물을 바로 채집한다. 끄면 배수만 걸어 두고 나중에 채집될 때 적용된다")]
    [SerializeField] private bool harvestInstantly = true;

    // 채집물은 방장 화면에서만 불린다 (드롭 생성도 방장만 가능)
    public override void Apply(in PotionHitContext context)
    {
        Entity target = context.Target;
        if (target == null || target.CheckDie() || !target.TryGetComponent(out ItemDropper dropper)) return;

        dropper.ApplyYieldMultiplier(yieldMultiplier);
        if (harvestInstantly) target.KillEntity();
    }
}
