using Photon.Pun;
using Pomunculus.Items;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[Serializable]
public class KnockbackPotionEffect : PotionEffect
{
    [Tooltip("밀려나는 거리(m). 음수면 기준점 쪽으로 끌어당긴다")]
    [SerializeField] private float distance = 3f;
    [Tooltip("그 거리를 이동하는 데 걸리는 시간")]
    [Min(0.05f)][SerializeField] private float duration = 0.25f;
    [Header("픽업 아이템")]
    [SerializeField] private bool affectPickups = true;
    [SerializeField] private float pickupRadius = 2f;
    [SerializeField] private LayerMask pickupLayers;          // 픽업이 있는 레이어
    [SerializeField] private LayerMask groundLayers = 1 << 6;

    private readonly Collider[] pickupBuffer = new Collider[32];

    // 기준점(착탄 지점 또는 장판 중심)에서 대상을 밀어내거나 끌어당긴다
    public override void Apply(in PotionHitContext context)
    {
        Entity target = context.Target;
        if (target == null || target.CheckDie()) return;
        if (!target.TryGetComponent(out NavMeshAgent _)) return;   // 채집물 등은 제외

        Vector3 away = target.transform.position - context.ImpactPoint;
        away.y = 0f;
        Vector3 direction = away.sqrMagnitude > 0.0001f ? away.normalized : -target.transform.forward;

        // 끌어당길 때는 기준점을 지나치지 않도록 거리를 자른다
        float moveDistance = distance >= 0f ? distance : -Mathf.Min(-distance, away.magnitude);

        if (!target.TryGetComponent(out PotionKnockbackMover mover))
            mover = target.gameObject.AddComponent<PotionKnockbackMover>();
        mover.Push(direction * moveDistance, duration);
    }

    // 방장만 착탄 지점 주변 픽업의 도착 지점을 계산해 전파한다 (OnImpact는 모든 클라이언트에서 불림)
    public override void OnImpact(in PotionImpactContext context)
    {
        if (!affectPickups) return;
        if (PhotonNetwork.InRoom && !PhotonNetwork.IsMasterClient) return;

        int count = Physics.OverlapSphereNonAlloc(context.ImpactPoint, pickupRadius, pickupBuffer, pickupLayers);
        var handled = new HashSet<WorldItemPickup>();
        for (int i = 0; i < count; i++)
        {
            WorldItemPickup pickup = pickupBuffer[i].GetComponentInParent<WorldItemPickup>();
            if (pickup == null || !handled.Add(pickup)) continue;

            Vector3 destination = pickup.transform.position + GetDisplacement(pickup.transform.position, context.ImpactPoint);
            pickup.SendDisplace(SnapToGround(destination), duration);
        }
    }

    // 기준점에서 밀어내거나(양수) 끌어당기는(음수) 이동량. 끌어당길 때는 기준점을 넘지 않는다
    private Vector3 GetDisplacement(Vector3 position, Vector3 center)
    {
        Vector3 away = position - center;
        away.y = 0f;
        if (away.sqrMagnitude < 0.0001f) return Vector3.zero;

        float moveDistance = distance >= 0f ? distance : -Mathf.Min(-distance, away.magnitude);
        return away.normalized * moveDistance;
    }

    // 도착 지점 바로 아래 바닥 위치
    private Vector3 SnapToGround(Vector3 point)
    {
        return Physics.Raycast(point + Vector3.up * 2f, Vector3.down, out RaycastHit hit, 10f, groundLayers)
            ? hit.point : point;
    }
}
