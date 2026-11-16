using UnityEngine;

/// <summary>
/// 활성화된 동안 CullingManager에 자신을 등록해, 화면 안에 있을 때만 CullingTick을 받는 컴포넌트의 부모.
/// 자식은 등록·해제를 신경 쓰지 않고 판정용 구와 CullingTick만 구현한다.
/// </summary>
public abstract class CullingTickBehaviour : MonoBehaviour, ICullingTickable
{
    [Tooltip("위치가 바뀌는 오브젝트면 체크한다. 매 프레임 화면 판정용 구를 다시 계산한다")]
    [SerializeField] private bool isMoving;

    // CullingTick은 CullingManager만 호출하도록 인터페이스 멤버를 명시적으로 구현하고 자식에는 protected로만 연다
    bool ICullingTickable.IsMovingObject => isMoving;

    protected virtual void OnEnable()
    {
        CullingManager.Register(this);
    }

    protected virtual void OnDisable()
    {
        CullingManager.Unregister(this);
    }

    BoundingSphere ICullingTickable.GetCullingSphere() => GetCullingSphere();

    void ICullingTickable.CullingTick(float now) => CullingTick(now);

    /// <summary>화면 안 판정에 쓸 월드 공간의 구. IsMovingObject가 꺼져 있으면 등록할 때 한 번만 호출된다</summary>
    protected abstract BoundingSphere GetCullingSphere();

    /// <summary>화면 안에 있는 동안 매 프레임 호출된다. Update 대신 쓴다</summary>
    protected abstract void CullingTick(float now);
}
