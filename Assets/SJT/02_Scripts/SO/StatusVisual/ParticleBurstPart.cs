using UnityEngine;

/// <summary>
/// 지정한 시점마다 파티클을 한 번 재생하는 누적형 파츠이다. 화상의 불꽃 튀기, 냉기의 눈 결정 띄우기 등에 사용한다.
/// 파티클은 대상 위치에서 월드 공간에 재생되며 풀에서 재사용된다.
/// </summary>
[CreateAssetMenu(fileName = "ParticleBurstPart", menuName = "Scriptable Objects/StatusVisual/ParticleBurstPart")]
public sealed class ParticleBurstPart : StatusVisualPart
{
    [Tooltip("재생할 파티클 프리팹. 루트에 ParticleSystem이 있어야 함. 일회성으로 재생하도록 인스턴스의 Looping은 자동으로 꺼짐")]
    [SerializeField] private ParticleSystem effectPrefab;

    [Tooltip("재생 위치 보정값. 대상의 연출 기준 위치(Anchor) 기준, 월드 단위")]
    [SerializeField] private Vector3 offset;

    [Tooltip("상태가 처음 걸릴 때 재생")]
    [SerializeField] private bool playOnStart;

    [Tooltip("상태가 중첩될 때 재생")]
    [SerializeField] private bool playOnReapply;

    [Tooltip("발동 주기(틱)마다 재생")]
    [SerializeField] private bool playOnTick = true;

    /// <summary>설정에 따라 걸림 시 파티클을 재생한다.</summary>
    public override void OnStart(StatusEffectVisualizer host, StatusEffect effect)
    {
        if (playOnStart)
        {
            Play(host);
        }
    }

    /// <summary>설정에 따라 중첩 시 파티클을 재생한다.</summary>
    public override void OnReapply(StatusEffectVisualizer host, StatusEffect effect)
    {
        if (playOnReapply)
        {
            Play(host);
        }
    }

    /// <summary>설정에 따라 틱마다 파티클을 재생한다.</summary>
    public override void OnTick(StatusEffectVisualizer host, StatusEffect effect)
    {
        if (playOnTick)
        {
            Play(host);
        }
    }

    /// <summary>대상의 연출 기준 위치에서 파티클을 한 번 재생한다.</summary>
    private void Play(StatusEffectVisualizer host)
    {
        if (effectPrefab == null)
        {
#if UNITY_EDITOR
            Debug.LogWarning("[ParticleBurstPart] 재생할 파티클 프리팹이 지정되지 않았습니다.", this);
#endif
            return;
        }

        StatusVisualFxPool.Play(effectPrefab, host.Anchor.position + offset);
    }
}
