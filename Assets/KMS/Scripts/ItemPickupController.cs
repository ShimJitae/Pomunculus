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
    public event Action<MaterialPickup> OnMaterialPickedUp;
    public event Action<PotionPickup> OnPotionPickedUp;

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
        if (owner != null && !owner.IsMine)
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

            MaterialPickup material = hitCollider.GetComponentInParent<MaterialPickup>();
            if (material != null)
            {
                if (detectedThisScan.Add(material))
                    PickupMaterial(material);
                continue;
            }

            PotionPickup potion = hitCollider.GetComponentInParent<PotionPickup>();
            if (potion != null && detectedThisScan.Add(potion))
                PickupPotion(potion);
        }

        foreach(Component component in detectedThisScan)
        {
            Debug.Log($"{component} 감지");
        }
    }

    // 재료를 화면에서 숨기고 방장에게 획득을 요청한다 (지급은 PlayerFSM.GrantItem RPC로 받음)
    private void PickupMaterial(MaterialPickup material)
    {
        if (material.Material == null)
        {
            Debug.LogWarning("MaterialData가 비어 있는 재료", material);
            return;
        }
        material.gameObject.SetActive(false);   // 다음 스캔에서 다시 요청하지 않도록
        material.RequestPickup(owner.Owner);
        OnMaterialPickedUp?.Invoke(material);   // 현재 구독자 없음. 지급 확정 시점이 필요하면 GrantItem 쪽으로 옮길 것
    }

    // 포션을 인벤토리에 넣고 필드에서 제거한다 (포션은 항상 로컬 오브젝트)
    private void PickupPotion(PotionPickup potion)
    {
        if (potion.Data == null) { Debug.LogWarning("PotionData가 비어 있는 포션", potion); return; }

        owner.Inventory.AddItem(potion.Data.Id, 1);
        RemoveFromField(potion.gameObject);
        OnPotionPickedUp?.Invoke(potion);
    }

    // 로컬 아이템을 다음 스캔에서 다시 감지되지 않게 즉시 끄고 삭제한다
    private void RemoveFromField(GameObject item)
    {
        item.SetActive(false);
        Destroy(item);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
