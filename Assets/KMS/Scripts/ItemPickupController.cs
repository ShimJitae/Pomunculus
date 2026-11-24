using Pomunculus.Items;
using System;
using System.Collections.Generic;
using UnityEngine;

// 주변에 존재하는 아이템 감지
public class ItemPickupController : MonoBehaviour
{
    [Header("감지")]
    public float radius = 1.5f;
    [Tooltip("주울 수 있는 오브젝트만 들어 있는 레이어를 지정")]
    public LayerMask pickupLayer;
    public float scanInterval = 0.1f;

    // 주울 아이템 및 포션 -> TODO 이 후 각 스크립트에 IPickupable 같은 인터페이스로 관리하기
    public event Action<WorldItemPickup> OnItemPickedUp;

    readonly Collider[] buffer = new Collider[16];
    private PlayerFSM owner;
    private float nextScanAt;
    private readonly HashSet<Component> detectedThisScan = new HashSet<Component>();

    private void Awake()
    {
        owner = GetComponent<PlayerFSM>();
    }

    void Update()
    {

        if (owner == null || !owner.IsMine || owner.IsDead)
            return;

        if (Time.time >= nextScanAt)
        {
            nextScanAt = Time.time + scanInterval;
            Scan();
        }
    }

    void Scan()
    {
        int count = Physics.OverlapSphereNonAlloc(
            transform.position, radius, buffer, pickupLayer, QueryTriggerInteraction.Collide);

        detectedThisScan.Clear();

        for (int i = 0; i < count; i++)
        {
            Collider hitCollider = buffer[i];
            WorldItemPickup item = hitCollider.GetComponentInParent<WorldItemPickup>();
            if (item != null && detectedThisScan.Add(item))
                PickupItem(item);
        }
    }

    // 아이템을 화면에서 숨기고 방장에게 획득을 요청한다 (지급은 PlayerFSM.GrantItem RPC로 받음)
    private void PickupItem(WorldItemPickup item)
    {
        if (item.ItemId <= 0)
        {
            Debug.LogWarning("아이템 데이터가 비어 있는 픽업", item);
            return;
        }
        item.gameObject.SetActive(false);   // 다음 스캔에서 다시 요청하지 않도록
        item.RequestPickup(owner.Owner);
        OnItemPickedUp?.Invoke(item);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
