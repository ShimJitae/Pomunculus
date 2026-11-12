using System.Collections.Generic;
using System.Dynamic;
using UnityEngine;

public class SkillParticlePool : SingletonBehavior<SkillParticlePool>
{
    [SerializeField, Min(1)] private int defaultPoolCount = 3;

    private readonly Dictionary<GameObject, List<GameObject>> instancesByPrefab = new();

    public void Prewarm(GameObject particlePrefab, int prewarmCount = 0)
    {
        if (particlePrefab == null)
            return;

        if (particlePrefab.TryGetComponent(out ParticleSystem rootParticle) && rootParticle.main.stopAction != ParticleSystemStopAction.Disable)
        {
            Debug.LogWarning($"{particlePrefab.name}: 루트 파티클 Stop Action이 Disable이 아니라 풀로 돌아오지 않습니다.");
        }

        int targetCount = prewarmCount > 0 ? prewarmCount : defaultPoolCount;
        List<GameObject> instances = GetInstanceList(particlePrefab);

        while (instances.Count < targetCount)
            instances.Add(CreateInstance(particlePrefab));
    }

    public void Spawn(GameObject particlePrefab, Vector3 spawnPosition, Quaternion spawnRotation, Vector3 spawnScale)
    {
        List<GameObject> instances = GetInstanceList(particlePrefab);
        GameObject spawnInstance = null;

        foreach(GameObject pooledInstance in instances)
        {
            if(pooledInstance != null && !pooledInstance.activeSelf)
            {
                spawnInstance = pooledInstance;
                break;
            }
        }

        if(spawnInstance == null)
        {
            spawnInstance = CreateInstance(particlePrefab);
            instances.Add(spawnInstance);
        }

        // 위치를 먼저 잡고 켠다 (EnemyProjectile.OnEnable이 이 위치에서 출발)
        spawnInstance.transform.SetPositionAndRotation(spawnPosition, spawnRotation);
        spawnInstance.transform.localScale = spawnScale;
        spawnInstance.SetActive(true);
    }

    private List<GameObject> GetInstanceList(GameObject particlePrefab)
    {
        if(!instancesByPrefab.TryGetValue(particlePrefab, out List<GameObject> instances))
        {
            instances = new List<GameObject>();
            instancesByPrefab.Add(particlePrefab, instances);
        }
        return instances;
    }

    // 항상 꺼진 상태로 만들어 둔다
    private GameObject CreateInstance(GameObject particlePrefab)
    {
        GameObject newInstance = Instantiate(particlePrefab, transform);
        newInstance.SetActive(false);
        return newInstance;
    }
}
