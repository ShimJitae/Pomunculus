using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Pomunculus.Items
{
    public class Inventory : MonoBehaviour
    {
        [SerializeField] private List<InventorySlot> slots = new List<InventorySlot>();
        public List<InventorySlot> Slots => slots;
        public event System.Action Changed;

        [Header("드롭")]
        [SerializeField] private float dropScatterRadius = 0.7f;   // 사망 시 전체 드롭을 흩뿌릴 반경
        [Tooltip("착지 지점을 바닥 높이에 맞출 때 사용할 지면 레이어. 비워 두면 PlayerInputReader의 GroundLayer를 사용")]
        [SerializeField] private LayerMask groundLayers;
        [SerializeField, Min(0f)] private float dropStartHeight = 1f;        // 아이템이 튀어나오는 높이 (플레이어 발 기준)
        [SerializeField, Min(0f)] private float dropDistance = 1.3f;         // 우클릭으로 버릴 때 착지 거리
        [SerializeField, Min(0f)] private float dropArcHeight = 1f;          // 포물선 최고점 높이
        [SerializeField, Min(0.01f)] private float dropFlightDuration = 0.45f;   // 튀어나가서 착지할 때까지 걸리는 시간
        [Tooltip("버린 사람이 그 아이템을 다시 주울 수 있을 때까지의 시간(초)")]
        [SerializeField, Min(0f)] private float dropperPickupDelay = 2f;
        [SerializeField] private AudioSO dropSound;                           // 우클릭으로 버릴 때 재생 (없으면 무음)

        private PlayerFSM ownerPlayer;   // 이 인벤토리를 가진 플레이어 (사망 여부·ActorNumber·위치 기준)

        private void Awake()
        {
            ownerPlayer = GetComponentInParent<PlayerFSM>();
        }

        // 아이템이 튀어나오는 기준 위치. 플레이어가 없으면 이 오브젝트 위치
        private Transform DropOrigin => ownerPlayer != null ? ownerPlayer.transform : transform;

        public void AddItem(int itemId, int amount = 1)
        {
            if (itemId <= 0 || amount <= 0)
                return;
            foreach (InventorySlot slot in slots)
            {
                if (slot.ItemId == itemId)
                {
                    slot.Amount += amount;
                    Changed?.Invoke();
                    return;
                }
            }
            slots.Add(new InventorySlot(itemId, amount));
            Changed?.Invoke();
        }
        public int GetItemCount(int itemId)
        {
            foreach (InventorySlot slot in slots)
            {
                if (slot.ItemId == itemId)
                    return slot.Amount;
            }

            return 0;
        }
        public bool RemoveItem(int itemId, int amount = 1)
        {
            if (amount <= 0)
                return false;

            for (int i = 0; i < slots.Count; i++)
            {
                InventorySlot slot = slots[i];

                if (slot.ItemId != itemId)
                    continue;

                if (slot.Amount < amount)
                    return false;

                slot.Amount -= amount;

                if (slot.Amount == 0)
                    slots.RemoveAt(i);

                Changed?.Invoke();
                return true;
            }
            return false;
        }

        /// <summary>
        /// 아이템을 amount개 바닥에 버린다 (인벤토리 우클릭).
        /// 버린 수량은 픽업 오브젝트 하나에 담기고, 플레이어 정면 쪽으로 튀어나간다. 성공하면 true
        /// </summary>
        public bool DropItem(int itemId, int amount)
        {
            if (amount <= 0 || GetItemCount(itemId) < amount)
                return false;
            if (ownerPlayer != null && ownerPlayer.IsDead)
                return false;

            GameObject prefab = FindDropPrefab(itemId);
            if (prefab == null)
            {
                Debug.LogWarning($"[Inventory] 드롭 프리팹이 없는 아이템 : {itemId}");
                return false;
            }

            if (!RemoveItem(itemId, amount))
                return false;

            float distance = Random.Range(dropDistance * 0.6f, dropDistance);
            SpawnDroppedItem(prefab, amount, GetRandomDropDirection(), distance);

            if (dropSound != null && AudioManager.Instance != null)
                AudioManager.Instance.PlaySound(dropSound, DropOrigin.position);
            return true;
        }

        // 인벤토리의 모든 아이템을 종류별로 하나씩 떨어트리고, 떨어트린 슬롯만 인벤토리에서 지운다 (사망 시)
        [ContextMenu("버리기")]
        public void DropAllItems()
        {
            if (slots.Count == 0) return;

            int totalCount = slots.Count;

            bool removeAny = false;
            for (int i = slots.Count - 1; i >= 0; i--)
            {
                if (!TryDropSlot(slots[i], GetRingDropDirection(i, totalCount)))
                    continue;

                slots.RemoveAt(i);
                removeAny = true;
            }

            if (removeAny)
                Changed?.Invoke();
        }

        // 슬롯 하나를 드롭 오브젝트 하나로 direction 방향에 떨어트린다. 성공하면 true
        private bool TryDropSlot(InventorySlot slot, Vector3 direction)
        {
            GameObject prefab = FindDropPrefab(slot.ItemId);
            if (prefab == null)
            {
                Debug.LogWarning($"[Inventory] 드롭 프리팹이 없는 아이템 : {slot.ItemId}");
                return false;
            }

            SpawnDroppedItem(prefab, slot.Amount, direction, dropScatterRadius);
            return true;
        }

        /// <summary>
        /// 플레이어 몸에서 생성해 착지 지점까지 포물선으로 튀어나가게 한다.
        /// 궤적·수량·버린 사람 정보는 SaveData에 담겨 방장을 거쳐 모든 화면에 같은 연출로 전파된다
        /// </summary>
        private void SpawnDroppedItem(GameObject prefab, int amount, Vector3 direction, float distance)
        {
            Vector3 origin = DropOrigin.position;
            Vector3 startPosition = origin + Vector3.up * dropStartHeight;
            Vector3 landingPoint = GetLandingPoint(origin, direction, distance);
            int dropperActor = ownerPlayer != null ? ownerPlayer.Owner : 0;

            SaveData saveData = WorldItemPickup.CreateDropData(
                amount, landingPoint, dropFlightDuration, dropArcHeight, dropperActor, dropperPickupDelay);
            WorldManager.Instance.RequestSpawn(prefab.name, startPosition, Quaternion.identity, saveData);
        }

        // 플레이어 둘레 360° 중 무작위 수평 방향을 고른다
        private Vector3 GetRandomDropDirection()
        {
            float angle = Random.Range(0f, 360f);
            return Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
        }

        // index번째 아이템을 플레이어 둘레에 같은 각도 간격으로 배치할 방향
        private static Vector3 GetRingDropDirection(int index, int totalCount)
        {
            float angle = (360f / totalCount) * index * Mathf.Deg2Rad;
            return new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
        }

        /// <summary>
        /// origin에서 direction으로 distance만큼 떨어진 착지 지점을 구한다.
        /// 중간에 NavMesh 경계(벽)가 있으면 그 앞까지만 가고, 최종 높이는 아래 지면에 맞춘다
        /// </summary>
        private Vector3 GetLandingPoint(Vector3 origin, Vector3 direction, float distance)
        {
            Vector3 target = origin + direction * distance;

            if (NavMesh.SamplePosition(origin, out NavMeshHit originHit, 1f, NavMesh.AllAreas) &&
                NavMesh.Raycast(originHit.position, target, out NavMeshHit wallHit, NavMesh.AllAreas))
            {
                target = wallHit.position;
            }

            if (Physics.Raycast(target + Vector3.up * 2f, Vector3.down, out RaycastHit groundHit, 10f,
                    GetGroundLayers(), QueryTriggerInteraction.Ignore))
            {
                target = groundHit.point;
            }

            return target;
        }

        // 착지 높이를 맞출 지면 레이어. 인스펙터 값이 비어 있으면 플레이어 입력의 GroundLayer, 그것도 없으면 전체
        private LayerMask GetGroundLayers()
        {
            if (groundLayers.value != 0)
                return groundLayers;
            if (ownerPlayer != null && ownerPlayer.Input != null && ownerPlayer.Input.GroundLayer.value != 0)
                return ownerPlayer.Input.GroundLayer;
            return ~0;
        }

        // 아이템 ID로 바닥에 떨어트릴 프리팹을 찾는다. 재료 → 포션 순으로 조회하고, 없으면 null
        public static GameObject FindDropPrefab(int itemId)
        {
            MaterialData material = MaterialDatabase.Instance.Find(itemId);
            if (material != null)
                return material.Prefab;
            PotionData potion = PotionDatabase.Instance.Find(itemId);
            if (potion != null)
                return potion.PickupPrefab.gameObject;
            return null;
        }
    }
}