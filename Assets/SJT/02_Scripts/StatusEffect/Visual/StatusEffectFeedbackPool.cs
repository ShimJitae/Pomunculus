using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public sealed class StatusEffectFeedbackPool : MonoBehaviour
{
    [SerializeField] private Transform inactiveRoot;

    private readonly Dictionary<StatusEffectFeedback, ObjectPool<StatusEffectFeedback>> pools = new();

    public static StatusEffectFeedbackPool Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public StatusEffectFeedback Get(StatusEffectFeedback prefab)
    {
        if (!pools.TryGetValue(prefab, out ObjectPool<StatusEffectFeedback> pool))
        {
            pool = new ObjectPool<StatusEffectFeedback>(
                createFunc: () =>
                {
                    StatusEffectFeedback instance = Instantiate(prefab, inactiveRoot, false);
                    instance.gameObject.SetActive(false);
                    return instance;
                },
                actionOnRelease: instance =>
                {
                    instance.gameObject.SetActive(false);
                    instance.transform.SetParent(inactiveRoot, false);
                },
                actionOnDestroy: instance => Destroy(instance.gameObject));

            pools.Add(prefab, pool);
        }

        return pool.Get();
    }

    public void Return(StatusEffectFeedback prefab, StatusEffectFeedback instance)
    {
        pools[prefab].Release(instance);
    }
}