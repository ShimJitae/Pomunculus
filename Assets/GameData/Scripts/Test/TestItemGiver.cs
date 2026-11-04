using UnityEngine;

namespace Pomunculus.Items
{
    [RequireComponent(typeof(Inventory))]
    public class TestItemGiver : MonoBehaviour
    {
        [Header("추가할 아이템")]
        [SerializeField] private int itemId = 25001;

        [Min(1)]
        [SerializeField] private int amount = 1;

        [ContextMenu("아이템 추가")]
        public void GiveItem()
        {
            if (!Application.isPlaying)
            {
                Debug.Log("플레이 모드에서 사용해 주세요.");
                return;
            }

            if (itemId <= 0 || amount <= 0)
            {
                Debug.LogWarning("아이템 ID와 수량은 양수로 입력해 주세요.");
                return;
            }

            Inventory inventory = GetComponent<Inventory>();
            inventory.AddItem(itemId, amount);

            Debug.Log($"아이템 추가: ID {itemId}, 수량 {amount}, 현재 보유량 {inventory.GetItemCount(itemId)}");
        }
        [ContextMenu("모든 재료 50개씩 추가")]
        public void GiveAllMaterials()
        {
            if (!Application.isPlaying)
            {
                Debug.Log("플레이 모드에서 사용해 주세요.");
                return;
            }

            Inventory inventory = GetComponent<Inventory>();

            for (int id = 25001; id <= 25041; id++)
            {
                inventory.AddItem(id, 50);
            }

            Debug.Log("모든 재료 41종을 각각 50개씩 추가했습니다.", this);
        }
    }

}