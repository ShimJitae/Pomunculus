using Photon.Pun;
using UnityEngine;

namespace Pomunculus.Items
{
    /// <summary>
    /// 인벤토리 UI(PotionSlotSelector)에서 장착한 포션과 선택 슬롯을 로컬 플레이어의 PotionLoadout에 옮겨 담는다.
    /// PotionSlotSelector에는 변경 이벤트가 없어서 매 프레임 슬롯을 비교하고, 달라졌을 때만 반영한다.
    /// </summary>
    public class PotionLoadoutSync : MonoBehaviour
    {
        [SerializeField] private PotionSlotSelector potionSlotSelector;
        [Tooltip("네트워크 스폰이 없는 테스트 씬에서 쓸 플레이어. 로컬 플레이어가 스폰되면 그쪽을 우선한다")]
        [SerializeField] private PlayerFSM fallbackPlayer;

        private PotionLoadout loadout;
        private WorldManager world;

        // 스폰 알림을 구독하고, 이미 있는 로컬 플레이어를 찾는다
        private void OnEnable()
        {
            world = WorldManager.Instance;
            if (world != null)
            {
                world.OnSpawnedEvent += OnWorldObjSpawned;
                foreach (IWorldObj obj in world.Objects.GetList())
                {
                    if (TryBindPlayer(obj))
                        break;
                }
            }

            if (loadout == null && fallbackPlayer != null)
                loadout = fallbackPlayer.GetComponent<PotionLoadout>();
        }

        // 스폰 알림 구독을 해제한다
        private void OnDisable()
        {
            if (world != null)
                world.OnSpawnedEvent -= OnWorldObjSpawned;
            world = null;
        }

        // 새로 스폰된 오브젝트가 내 플레이어면 연결한다
        private void OnWorldObjSpawned(IWorldObj obj)
        {
            TryBindPlayer(obj);
        }

        // 내 소유 플레이어의 PotionLoadout을 연결한다. 연결했으면 true
        private bool TryBindPlayer(IWorldObj obj)
        {
            if (obj is not IPlayerObj player)
                return false;
            if (PhotonNetwork.LocalPlayer == null || player.Owner != PhotonNetwork.LocalPlayer.ActorNumber)
                return false;

            PotionLoadout playerLoadout = obj.gameObject.GetComponent<PotionLoadout>();
            if (playerLoadout == null)
                return false;

            loadout = playerLoadout;
            return true;
        }

        // UI의 장착 슬롯과 선택 슬롯을 PotionLoadout에 반영한다 (UI 입력 처리가 끝난 뒤)
        private void LateUpdate()
        {
            if (potionSlotSelector == null || loadout == null)
                return;

            for (int slotIndex = 0; slotIndex < PotionLoadout.SlotCount; slotIndex++)
            {
                PotionData registeredPotion = potionSlotSelector.GetRegisteredPotion(slotIndex);
                if (registeredPotion != loadout.GetSlot(slotIndex))
                    loadout.SetSlot(slotIndex, registeredPotion);
            }

            // UI가 아직 아무 슬롯도 고르지 않았으면(-1) Loadout의 현재 선택을 유지한다
            int activeSlotIndex = potionSlotSelector.ActiveSlotIndex;
            if (activeSlotIndex >= 0)
                loadout.SelectSlot(activeSlotIndex);
        }
    }
}
