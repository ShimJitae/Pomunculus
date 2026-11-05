using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 상태이상 연출용 일회성 파티클을 프리팹별로 재사용하는 전역 풀이다.
/// 수명: 첫 Play 호출 시 프리팹별 큐를 만들고, 재생이 끝난 인스턴스는 비활성화해 큐에 돌려받는다.
/// 인스턴스는 씬 루트에 생성되므로 씬 전환 시 함께 파괴되며, 파괴된 항목은 꺼낼 때 건너뛴다.
/// 플레이 모드 진입 시 큐를 비워 Domain Reload를 끈 설정에서도 이전 세션의 참조가 남지 않게 한다.
/// </summary>
public static class StatusVisualFxPool
{
    private static readonly Dictionary<ParticleSystem, Queue<StatusVisualFxInstance>> pools = new Dictionary<ParticleSystem, Queue<StatusVisualFxInstance>>();

    /// <summary>프리팹의 인스턴스를 풀에서 꺼내거나 새로 만들어 지정 위치에서 한 번 재생한다.</summary>
    public static void Play(ParticleSystem prefab, Vector3 position)
    {
        if (prefab == null)
        {
            return;
        }

        StatusVisualFxInstance instance = Get(prefab);
        if (instance == null)
        {
            instance = Create(prefab);
        }

        Transform instanceTransform = instance.transform;
        instanceTransform.position = position;
        instance.gameObject.SetActive(true);

        ParticleSystem system = instance.Particles;
        system.Clear(true);
        system.Play(true);
    }

    /// <summary>재생이 끝난 인스턴스를 비활성화하고 풀에 돌려받는다.</summary>
    public static void Return(StatusVisualFxInstance instance)
    {
        if (instance == null)
        {
            return;
        }

        instance.gameObject.SetActive(false);

        if (instance.Prefab == null || !pools.TryGetValue(instance.Prefab, out Queue<StatusVisualFxInstance> queue))
        {
            Object.Destroy(instance.gameObject);
            return;
        }

        queue.Enqueue(instance);
    }

    /// <summary>프리팹의 큐에서 살아 있는 인스턴스를 꺼낸다. 없으면 null을 반환한다.</summary>
    private static StatusVisualFxInstance Get(ParticleSystem prefab)
    {
        if (!pools.TryGetValue(prefab, out Queue<StatusVisualFxInstance> queue))
        {
            pools.Add(prefab, new Queue<StatusVisualFxInstance>());
            return null;
        }

        while (queue.Count > 0)
        {
            StatusVisualFxInstance candidate = queue.Dequeue();
            if (candidate != null)
            {
                return candidate;
            }
        }

        return null;
    }

    /// <summary>새 인스턴스를 만들고 재생이 끝나면 풀로 돌아오도록 설정한다.</summary>
    private static StatusVisualFxInstance Create(ParticleSystem prefab)
    {
        ParticleSystem system = Object.Instantiate(prefab);

        // 원본 프리팹은 건드리지 않고 인스턴스만 일회성 재생용으로 맞춘다.
        // Looping이 남아 있으면 재생이 끝나지 않아 풀로 돌아오지 못하고, 자식의 Disable·Destroy 동작은 재사용 시 자식 연출을 없앤다.
        ParticleSystem[] systems = system.GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < systems.Length; i++)
        {
            ParticleSystem.MainModule childMain = systems[i].main;
#if UNITY_EDITOR
            if (childMain.loop)
            {
                Debug.LogWarning($"[StatusVisualFxPool] {prefab.name}의 {systems[i].name}에 Looping이 켜져 있어 인스턴스에서 끕니다.", prefab);
            }
#endif
            childMain.loop = false;
            childMain.stopAction = ParticleSystemStopAction.None;
        }

        // 재생이 끝나면 OnParticleSystemStopped가 호출되어 풀로 돌아온다.
        ParticleSystem.MainModule main = system.main;
        main.stopAction = ParticleSystemStopAction.Callback;

        StatusVisualFxInstance instance = system.gameObject.AddComponent<StatusVisualFxInstance>();
        instance.Initialize(prefab, system);
        return instance;
    }

    /// <summary>플레이 모드 진입 시 이전 세션의 큐를 비운다.</summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetPools()
    {
        pools.Clear();
    }
}
