using UnityEngine;

/// <summary>콜라이더 범위 안에 있는 동안 고정 간격으로 상태이상을 건다. 콜라이더 모양은 자유.</summary>
public sealed class SESourceZone : SESource
{
    [SerializeField] private SESourceData data;
    [Tooltip("대상이 장판에 들어올 때 재생할 파티클")]
    [SerializeField] private ParticleSystem entryEffect;

    protected override SESourceData Data => data;
    protected override float GetInterval(Entity target) => data.Interval;

    protected override void OnTargetEntered(Entity entity)
    {
        if (entryEffect == null || data == null || !isActiveAndEnabled ||
            entity == null || !entity.isActiveAndEnabled || entity.CheckDie())
        {
            return;
        }

        entryEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        entryEffect.Play(true);
    }
}
