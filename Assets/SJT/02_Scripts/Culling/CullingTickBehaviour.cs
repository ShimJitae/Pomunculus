using UnityEngine;

/// <summary>
/// 활성화된 동안 CullingManager에 자신을 등록해, 화면 안에 있을 때만 CullingTick을 받는 컴포넌트의 부모.
/// 자식은 등록·해제를 신경 쓰지 않고 판정용 구와 CullingTick만 구현한다.
/// </summary>
public abstract class CullingTickBehaviour : MonoBehaviour, ICullingTickable
{
    [Tooltip("위치가 바뀌는 오브젝트면 체크한다. 매 프레임 화면 판정용 구를 다시 계산한다")]
    [SerializeField] private bool isMoving;

    // 첫 등록이 끝났는지. 씬 로드 직후의 OnEnable은 매니저의 Awake보다 먼저일 수 있어 첫 등록은 Start에서 한다
    private bool hasStarted;

#if UNITY_EDITOR
    // 기즈모 색 판정용. 마지막으로 CullingTick을 받은 프레임
    private int lastTickFrame = -1;
#endif

    // CullingTick은 CullingManager만 호출하도록 인터페이스 멤버를 명시적으로 구현하고 자식에는 protected로만 연다
    bool ICullingTickable.IsMovingObject => isMoving;

    protected virtual void Start()
    {
        hasStarted = true;
        RegisterToManager();
    }

    protected virtual void OnEnable()
    {
        // 첫 활성화는 Start가 맡는다. 이후 재활성화(풀링 등)만 여기서 등록한다
        if (hasStarted)
        {
            RegisterToManager();
        }
    }

    protected virtual void OnDisable()
    {
        // 씬 종료·전환 중에는 매니저가 먼저 파괴될 수 있다
        CullingManager manager = CullingManager.Instance;
        if (manager != null)
        {
            manager.Unregister(this);
        }
    }

    BoundingSphere ICullingTickable.GetCullingSphere() => GetCullingSphere();

    void ICullingTickable.CullingTick(float now)
    {
#if UNITY_EDITOR
        lastTickFrame = Time.frameCount;
#endif
        CullingTick(now);
    }

    /// <summary>
    /// 플레이 중 씬 뷰에 화면 판정용 구를 그린다. 이번 프레임에 CullingTick을 받았으면 초록, 아니면 빨강.
    /// 자식이 기즈모를 추가로 그리려면 override하고 base를 호출한다
    /// </summary>
    protected virtual void OnDrawGizmos()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            return;
        }

        BoundingSphere sphere = GetCullingSphere();
        Gizmos.color = lastTickFrame >= Time.frameCount - 1 ? Color.green : Color.red;
        Gizmos.DrawWireSphere(sphere.position, sphere.radius);
#endif
    }

    /// <summary>씬의 CullingManager에 등록한다. 매니저가 없으면 에디터에서만 경고한다</summary>
    private void RegisterToManager()
    {
        CullingManager manager = CullingManager.Instance;
        if (manager != null)
        {
            manager.Register(this);
        }
#if UNITY_EDITOR
        else
        {
            Debug.LogWarning($"[CullingTickBehaviour] 씬에 CullingManager가 없어 {name}이(가) 갱신되지 않습니다", this);
        }
#endif
    }

    /// <summary>화면 안 판정에 쓸 월드 공간의 구. IsMovingObject가 꺼져 있으면 등록할 때 한 번만 호출된다</summary>
    protected abstract BoundingSphere GetCullingSphere();

    /// <summary>화면 안에 있는 동안 매 프레임 호출된다. Update 대신 쓴다</summary>
    protected abstract void CullingTick(float now);
}
