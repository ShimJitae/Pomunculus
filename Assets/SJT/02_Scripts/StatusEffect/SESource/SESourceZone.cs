using UnityEngine;

/// <summary>콜라이더 범위 안에 있는 동안 고정 간격으로 상태이상을 건다. 콜라이더 모양은 자유.</summary>
public sealed class SESourceZone : SESource
{
    protected override float GetInterval(Entity target) => data.Interval;
}
