using Photon.Pun;
using UnityEngine;

namespace Pomunculus.Items
{
    public class LocalPlayerUIBinder : MonoBehaviour
    {
        [Header("연결할 UI")]
        [SerializeField] private InventoryUI inventoryUI;
        [SerializeField] private CraftingMaterialListUI materialListUI;
        [SerializeField] private PotionCrafting potionCrafting;
        [SerializeField] private PotionSlotSelector potionSlotSelector;
        private WorldManager world;
        private void OnEnable()
        {
            world = WorldManager.Instance;
            // 앞으로 생성되는 플레이어의 알림 받기
            world.OnSpawnedEvent += OnWorldObjSpawned;
            // 이미 생성된 내 플레이어가 있다면 연결
            foreach (IWorldObj obj in world.Objects.GetList())
            {
                if (TryBindPlayer(obj))
                    break;
            }
        }
        private void OnDisable()
        {
            if (world != null)
                world.OnSpawnedEvent -= OnWorldObjSpawned;
            world = null;
        }
        private void OnWorldObjSpawned(IWorldObj obj)
        {
            TryBindPlayer(obj);
        }

        private bool TryBindPlayer(IWorldObj obj)
        {
            // 플레이어인지 확인
            if (obj is not IPlayerObj player)
                return false;
            // 내 소유의 플레이어인지 확인
            if (PhotonNetwork.LocalPlayer == null)
                return false;
            if (player.Owner != PhotonNetwork.LocalPlayer.ActorNumber)
                return false;
            // 플레이어에게 붙어 있는 인벤토리 가져오기
            Inventory inventory = obj.gameObject.GetComponent<Inventory>();
            if (inventory == null)
            {
                Debug.LogWarning("내 플레이어에 Inventory가 없습니다.", obj.gameObject);
                return false;
            }
            BindInventory(inventory);
            return true;
        }
        private void BindInventory(Inventory inventory)
        {
            // 제작 기능에서 사용할 인벤토리 연결
            if (potionCrafting != null)
                potionCrafting.inventory = inventory;
            // 인벤토리 창 연결
            if (inventoryUI != null)
            {
                bool wasEnabled = inventoryUI.enabled;

                inventoryUI.enabled = false;
                inventoryUI.inventory = inventory;
                inventoryUI.enabled = wasEnabled;
            }
            // 제작 재료 목록 연결
            if (materialListUI != null)
            {
                bool wasEnabled = materialListUI.enabled;
                materialListUI.enabled = false;
                materialListUI.inventory = inventory;
                materialListUI.enabled = wasEnabled;
            }
            // 포션 단축 슬롯 연결
            if (potionSlotSelector != null)
            {
                bool wasEnabled = potionSlotSelector.enabled;
                potionSlotSelector.enabled = false;
                potionSlotSelector.inventory = inventory;
                potionSlotSelector.enabled = wasEnabled;
            }
        }
    }
}