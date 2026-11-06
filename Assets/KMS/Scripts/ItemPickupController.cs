using Photon.Pun;
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
    private Inventory inventory;
    private float nextScanAt;
    private readonly HashSet<Component> detectedThisScan = new HashSet<Component>();

    private const int LocalOnlyId = -1;

    private void Awake()
    {
        owner = GetComponent<PlayerFSM>();
        inventory = GetComponent<Inventory>();
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

    // 재료를 인벤토리에 넣고 필드에서 제거한다
    private void PickupMaterial(MaterialPickup material)
    {
        if (material.Material == null)
        {
            Debug.LogWarning("MaterialData가 비어 있는 재료", material);
            return;
        }
        if (!CanRemoveFromField(material.ID)) return;   // 먼저 확인하고 나서 추가 (추가만 되고 삭제 안 되는 경우 방지)

        inventory.AddItem(material.Material.Id, material.Amount);
        RemoveFromField(material.gameObject, material.ID);
        OnMaterialPickedUp?.Invoke(material);
    }

    // 포션을 인벤토리에 넣고 필드에서 제거한다 (포션은 항상 로컬 오브젝트)
    private void PickupPotion(PotionPickup potion)
    {
        if (potion.Data == null) { Debug.LogWarning("PotionData가 비어 있는 포션", potion); return; }

        inventory.AddItem(potion.Data.Id, 1);
        RemoveFromField(potion.gameObject, LocalOnlyId);
        OnPotionPickedUp?.Invoke(potion);
    }

    // WorldManager에 등록된 아이템은 방장만 지울 수 있다
    private bool CanRemoveFromField(int worldObjectId)
    {
        if (worldObjectId == LocalOnlyId) return true;
        return !PhotonNetwork.InRoom || PhotonNetwork.IsMasterClient;
    }

    // 다음 스캔에서 다시 감지되지 않게 즉시 끄고 삭제한다
    private void RemoveFromField(GameObject item, int worldObjectId)
    {
        item.SetActive(false);
        if (worldObjectId == LocalOnlyId) Destroy(item);
        else WorldManager.Instance.Despawn(worldObjectId);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
