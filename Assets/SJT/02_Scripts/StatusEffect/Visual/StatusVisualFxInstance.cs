using UnityEngine;

/// <summary>
/// StatusVisualFxPool이 만든 파티클 인스턴스에 런타임으로 붙는 반납 표식이다.
/// Entity가 아닌 연출 오브젝트에만 붙으며, 직접 프리팹에 추가하지 않는다.
/// </summary>
[DisallowMultipleComponent]
public sealed class StatusVisualFxInstance : MonoBehaviour
{
    private ParticleSystem prefab;
    private ParticleSystem system;

    /// <summary>이 인스턴스를 만든 원본 프리팹을 반환한다. 풀의 키로 사용한다.</summary>
    public ParticleSystem Prefab => prefab;

    /// <summary>이 인스턴스의 루트 ParticleSystem을 반환한다.</summary>
    public ParticleSystem Particles => system;

    /// <summary>원본 프리팹과 인스턴스의 ParticleSystem을 연결한다. 풀이 생성 직후 한 번 호출한다.</summary>
    public void Initialize(ParticleSystem prefab, ParticleSystem system)
    {
        this.prefab = prefab;
        this.system = system;
    }

    /// <summary>Stop Action이 Callback인 파티클의 재생이 끝나면 Unity가 호출한다. 풀로 돌아간다.</summary>
    private void OnParticleSystemStopped()
    {
        StatusVisualFxPool.Return(this);
    }
}
