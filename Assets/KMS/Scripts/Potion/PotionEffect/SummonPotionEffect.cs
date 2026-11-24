// 착탄 지점에 월드 오브젝트(몬스터 등)를 소환한다. 방장만 WorldManager로 생성해 모두에게 전파한다
using Photon.Pun;
using System;
using UnityEngine;
using UnityEngine.AI;

[Serializable]
public class SummonPotionEffect : SpawnPotionEffect
{
    private const float NavMeshSearchDistance = 2f;   // 배치 지점 주변에서 NavMesh를 찾을 최대 거리

    [Tooltip("소환할 프리팹. NetworkPrefabCatalog에 등록된 IWorldObj 프리팹이어야 한다 (플레이어 불가)")]
    [SerializeField] private GameObject monsterPrefab;
    [Min(1)][SerializeField] private int count = 1;
    [Tooltip("2마리 이상일 때 착탄 지점을 중심으로 둘러 놓을 반경")]
    [Min(0f)][SerializeField] private float spreadRadius = 1.5f;
    [Tooltip("이 시간 뒤 제거. 0이면 소환한 플레이어가 나갈 때까지 남는다")]
    [Min(0f)][SerializeField] private float lifetime = 0f;

    // 조준 원에 배치 범위를 보여 준다
    public override float AreaRadius => count > 1 ? spreadRadius : 0f;

    // 방장만 배치 지점마다 소환한다 (OnImpact는 모든 클라이언트에서 불린다)
    public override void OnImpact(in PotionImpactContext context)
    {
        if (PhotonNetwork.InRoom && !PhotonNetwork.IsMasterClient) return;
        if (!CanSummon()) return;

        Quaternion rotation = GetThrowDirectionRotation(context);
        PotionImpactContext impact = context;   // in 매개변수는 람다에서 쓸 수 없어 복사한다
        foreach (Vector3 point in GetSpawnPoints(context.ImpactPoint, count, spreadRadius))
        {
            if (!NavMesh.SamplePosition(point, out NavMeshHit hit, NavMeshSearchDistance, NavMesh.AllAreas)) continue;

            WorldManager.Instance.Spawn(monsterPrefab.name, hit.position, rotation,
                summoned => InitializeSummoned(summoned, impact));
        }
    }

    // 이 효과가 T 컴포넌트를 가진 프리팹을 소환하는지 확인한다 (분신이 분신을 만드는 것 방지 등)
    public bool SpawnsComponent<T>() => monsterPrefab != null && monsterPrefab.GetComponentInChildren<T>(true) != null;

    // 다른 화면에 전파되기 전에 소환물에 던진 사람 정보와 수명 타이머를 넣는다 (방장에서만 실행)
    private void InitializeSummoned(IWorldObj summoned, PotionImpactContext impact)
    {
        if (summoned == null) return;

        foreach (IPotionSpawned receiver in summoned.gameObject.GetComponentsInChildren<IPotionSpawned>(true))
            receiver.OnSpawnedByPotion(impact);

        summoned.gameObject.AddComponent<PotionSummonLifetime>().Initialize(lifetime, impact.Thrower);
    }

    // 소환 가능한 프리팹인지 확인하고, 아니면 이유를 경고로 남긴다
    private bool CanSummon()
    {
        if (monsterPrefab == null) return false;

        if (monsterPrefab.GetComponent<IPlayerObj>() != null)
        {
            Debug.LogWarning($"[SummonPotionEffect] 플레이어 프리팹은 소환할 수 없습니다: {monsterPrefab.name}. 분신은 PlayerClone 프리팹을 소환하세요.");
            return false;
        }
        if (monsterPrefab.GetComponent<IWorldObj>() == null)
        {
            Debug.LogWarning($"[SummonPotionEffect] IWorldObj가 없는 프리팹입니다: {monsterPrefab.name}");
            return false;
        }
        return true;
    }

}
