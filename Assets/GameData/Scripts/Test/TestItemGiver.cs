using UnityEngine;

namespace Pomunculus.Items
{
    [RequireComponent(typeof(Inventory))]
    public class TestItemGiver : MonoBehaviour
    {
        private const int TestPotionFirstId = 19001;   // Assets/KMS/Test/Potions 테스트 포션 ID 범위
        private const int TestPotionLastId = 19015;
        private const int TestPotionAmount = 50;

        [Header("추가할 아이템")]
        [SerializeField] private int itemId = 25001;

        [Min(1)]
        [SerializeField] private int amount = 1;
        private void Start()
        {
            ChatManager.Instance.OnCommandEvent += OnCommand;
        }
        private void OnDestroy()
        {
            if (ChatManager.Instance != null)
                ChatManager.Instance.OnCommandEvent -= OnCommand;
        }

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
        [ContextMenu("모든 포션 50개씩 추가")]
        public void GiveAllPotion()
        {
            if (!Application.isPlaying)
            {
                Debug.Log("플레이 모드에서 사용해 주세요.");
                return;
            }

            Inventory inventory = GetComponent<Inventory>();

            for (int id = 10001; id <= 10023; id++)
            {
                inventory.AddItem(id, 50);
            }

            Debug.Log("모든 포션 22종을 각각 50개씩 추가했습니다.", this);
        }

        // 효과 확인용 테스트 포션(19001~19007)을 50개씩 추가한다
        [ContextMenu("테스트 포션 50개씩 추가")]
        public void GiveTestPotions()
        {
            if (!Application.isPlaying)
            {
                Debug.Log("플레이 모드에서 사용해 주세요.");
                return;
            }

            Inventory inventory = GetComponent<Inventory>();

            for (int id = TestPotionFirstId; id <= TestPotionLastId; id++)
            {
                inventory.AddItem(id, TestPotionAmount);
            }

            Debug.Log($"테스트 포션 {TestPotionLastId - TestPotionFirstId + 1}종을 각각 {TestPotionAmount}개씩 추가했습니다.", this);
        }
        // 채팅 명령으로 아이템을 지급한다. ChatManager는 첫 단어만 command로, 나머지는 args로 나눠 주므로 다시 이어 붙여 비교한다
        private void OnCommand(string command, string[] args)
        {
            PlayerFSM player = GetComponentInParent<PlayerFSM>();
            if (player == null || !player.IsMine)
                return;

            string fullCommand = args.Length > 0 ? $"{command} {string.Join(" ", args).ToLowerInvariant()}" : command;
            if (fullCommand == "give all materials" || fullCommand == "wofy")
            {
                GiveAllMaterials();
            }
            else if (fullCommand == "give all potion" || fullCommand == "vhtus")
            {
                GiveAllPotion();
            }
            else if (fullCommand == "give test potion" || fullCommand == "testpotion")
            {
                GiveTestPotions();
            }
        }
    }

}