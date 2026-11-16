using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 등록된 ICullingTickable 중 메인 카메라 화면 안에 있는 대상에게만 매 프레임 CullingTick을 호출한다.
/// 사용하는 씬마다 미리 배치하며, 씬과 함께 생성·파괴된다. 한 씬에 둘 이상이면 먼저 깨어난 쪽만 남는다.
/// </summary>
public sealed class CullingManager : MonoBehaviour
{
    private const int c_initialCapacity = 32;

#if UNITY_EDITOR
    [Tooltip("켜면 대상이 화면에 들어오거나 나갈 때 콘솔에 로그를 남긴다 (에디터 전용)")]
    [SerializeField] private bool logVisibility;
#endif

    private static CullingManager instance;
    public static CullingManager Instance => instance;

    // targets[i]와 spheres[i]는 항상 같은 대상을 가리킨다
    private readonly List<ICullingTickable> targets = new();
    private readonly Dictionary<ICullingTickable, int> indexOf = new();
    // 이번 프레임에 Tick할 대상. 순회 중 등록·해제가 일어나도 대상마다 프레임당 한 번만 호출되게 한다
    private readonly List<ICullingTickable> tickBuffer = new();
    private BoundingSphere[] spheres = new BoundingSphere[c_initialCapacity];
    private CullingGroup group;
    private Camera targetCamera;

    private void Awake()
    {
        // 씬에 매니저가 둘 이상이면 먼저 깨어난 쪽만 쓴다
        if (instance != null && instance != this)
        {
            // Destroy는 프레임 끝에 처리되므로, 그 전에 CullingGroup 없이 Update가 돌지 않도록 먼저 끈다
            enabled = false;
            Destroy(gameObject);
            return;
        }
        instance = this;

        group = new CullingGroup();
        group.SetBoundingSpheres(spheres);
        group.SetBoundingSphereCount(0);
#if UNITY_EDITOR
        group.onStateChanged = LogVisibilityChanged;
#endif
    }

    private void Start()
    {
        targetCamera = Camera.main;
        if (targetCamera != null)
        {
            group.targetCamera = targetCamera;
        }
#if UNITY_EDITOR
        else
        {
            Debug.LogWarning("[CullingManager] 메인 카메라가 씬에 없습니다. 화면 판정 없이 모든 대상을 갱신합니다", this);
        }
#endif
    }

    private void Update()
    {
        // 움직이는 대상만 구를 다시 계산한다. 고정 대상은 등록할 때 계산한 값을 그대로 쓴다
        for (int i = 0; i < targets.Count; i++)
        {
            if (targets[i].IsMovingObject)
            {
                spheres[i] = targets[i].GetCullingSphere();
            }
        }

        // 화면 판정은 직전 렌더링의 컬링 결과다. 카메라가 없어 판정할 수 없으면 모두 갱신한다
        bool hasCamera = targetCamera != null;
        tickBuffer.Clear();
        for (int i = 0; i < targets.Count; i++)
        {
            if (!hasCamera || group.IsVisible(i))
            {
                tickBuffer.Add(targets[i]);
            }
        }

        // CullingTick 도중 등록·해제로 배열 번호가 바뀌어도 영향이 없도록 미리 모은 목록으로 호출한다
        float now = Time.time;
        for (int i = 0; i < tickBuffer.Count; i++)
        {
            ICullingTickable target = tickBuffer[i];
            // 앞선 대상의 CullingTick 때문에 이번 프레임에 해제된 대상은 건너뛴다
            if (indexOf.ContainsKey(target))
            {
                target.CullingTick(now);
            }
        }
        tickBuffer.Clear();
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }

        // CullingGroup은 네이티브 메모리를 쓰므로 직접 해제한다
        group?.Dispose();
        group = null;
    }

    /// <summary>대상을 등록한다. 이미 등록된 대상이면 무시한다</summary>
    public void Register(ICullingTickable target)
    {
        if (target == null)
        {
            return;
        }

        Add(target);
    }

    /// <summary>대상의 등록을 해제한다. 등록되지 않은 대상이면 무시한다. 매니저가 이미 파괴됐는지는 호출하는 쪽이 확인한다</summary>
    public void Unregister(ICullingTickable target)
    {
        if (target == null)
        {
            return;
        }

        Remove(target);
    }

    /// <summary>대상을 배열 끝에 추가하고 등록 시점의 판정용 구를 계산한다</summary>
    private void Add(ICullingTickable target)
    {
        if (indexOf.ContainsKey(target))
        {
            return;
        }

        if (targets.Count == spheres.Length)
        {
            Array.Resize(ref spheres, spheres.Length * 2);
            group.SetBoundingSpheres(spheres); // 배열이 바뀌었으므로 다시 알려준다
        }

        int index = targets.Count;
        targets.Add(target);
        spheres[index] = target.GetCullingSphere();
        indexOf[target] = index;
        group.SetBoundingSphereCount(targets.Count);
    }

    /// <summary>대상을 배열에서 뺀다. 맨 끝 대상을 빈자리로 옮기며, 구도 함께 옮기므로 고정 대상의 구가 유지된다</summary>
    private void Remove(ICullingTickable target)
    {
        if (!indexOf.TryGetValue(target, out int index))
        {
            return;
        }

        int last = targets.Count - 1;
        if (index != last)
        {
            ICullingTickable moved = targets[last];
            targets[index] = moved;
            spheres[index] = spheres[last];
            indexOf[moved] = index;
        }

        targets.RemoveAt(last);
        indexOf.Remove(target);
        group.SetBoundingSphereCount(targets.Count);
    }

#if UNITY_EDITOR
    /// <summary>화면 진입·이탈을 콘솔에 남긴다. 상태가 바뀔 때만 호출되므로 매 프레임 찍히지 않는다</summary>
    private void LogVisibilityChanged(CullingGroupEvent e)
    {
        if (!logVisibility || e.index >= targets.Count)
        {
            return;
        }

        UnityEngine.Object context = targets[e.index] as UnityEngine.Object;
        string state = e.hasBecomeVisible ? "화면 진입" : "화면 이탈";
        Debug.Log($"[CullingManager] {(context != null ? context.name : "(파괴됨)")} {state}", context);
    }
#endif
}
