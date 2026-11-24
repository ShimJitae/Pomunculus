// 착탄 지점에 아무 프리팹(장애물, 이펙트, 분신 등)을 만든다. 모든 클라이언트가 각자 로컬로 생성한다
using System;
using UnityEngine;

[Serializable]
public class SpawnObjectPotionEffect : SpawnPotionEffect
{
    [Tooltip("생성할 프리팹. 네트워크 동기화 없이 각 클라이언트에 로컬로 만든다")]
    [SerializeField] private GameObject prefab;
    [Min(1)][SerializeField] private int count = 1;
    [Tooltip("2개 이상일 때 착탄 지점을 중심으로 둘러 놓을 반경")]
    [Min(0f)][SerializeField] private float spreadRadius = 0f;
    [Tooltip("이 시간 뒤 삭제. 0이면 남겨 둔다")]
    [Min(0f)][SerializeField] private float lifetime = 5f;
    [Tooltip("켜면 던진 사람 → 착탄 지점 방향을 바라보게 회전")]
    [SerializeField] private bool alignToThrowDirection;

    // 조준 원에 배치 범위를 보여 준다
    public override float AreaRadius => count > 1 ? spreadRadius : 0f;

    // 이 효과가 T 컴포넌트를 가진 프리팹을 만드는지 확인한다 (분신이 분신을 만드는 것 방지 등)
    public bool SpawnsComponent<T>() => prefab != null && prefab.GetComponentInChildren<T>(true) != null;

    // 배치 위치마다 프리팹을 만들고 던진 사람 정보를 넘긴다
    public override void OnImpact(in PotionImpactContext context)
    {
        if (prefab == null) return;

        Quaternion rotation = alignToThrowDirection ? GetThrowDirectionRotation(context) : prefab.transform.rotation;
        foreach (Vector3 point in GetSpawnPoints(context.ImpactPoint, count, spreadRadius))
        {
            GameObject spawned = UnityEngine.Object.Instantiate(prefab, point, rotation);
            foreach (IPotionSpawned receiver in spawned.GetComponentsInChildren<IPotionSpawned>(true))
                receiver.OnSpawnedByPotion(context);

            if (lifetime > 0f) UnityEngine.Object.Destroy(spawned, lifetime);
        }
    }
}
