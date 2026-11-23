using UnityEngine;
using UnityEngine.InputSystem;

namespace Pomunculus.Items
{
    [DefaultExecutionOrder(-1000)]
    public class OpenUI : MonoBehaviour
    {
        [Header("레시피북")]
        [SerializeField] private GameObject recipeBook;
        [SerializeField] private Key recipeBookKey = Key.R;
        [Header("제작창")]
        [SerializeField] private GameObject craftingUI;
        [SerializeField] private Key craftingKey = Key.C;

        [Header("인벤토리")]
        [SerializeField] private GameObject inventoryUI;
        [SerializeField] private Key inventoryKey = Key.I;
        [Header("사운드")]
        [SerializeField] private AudioSO inventoryOpenSound;
        [SerializeField] private AudioSO CreatingOpenSound;

        [Header("텍스트")]
        [SerializeField] private GameObject craftingInteractionText;
        private ESCPanel escPanel;

        public GameObject CraftingInteractionText => craftingInteractionText;

        // ESC 입력은 OpenUI가 대신 처리하므로 ESCPanel의 자체 ESC 입력을 끈다
        private void OnEnable()
        {
            TakeOverEscapeKey();
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;

            if (keyboard == null)
                return;

            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                HandleEscapeKey();
                return;
            }
            if (recipeBookKey != Key.None &&
                keyboard[recipeBookKey].wasPressedThisFrame)
            {
                ToggleRecipeBook();
            }

            if (craftingKey != Key.None &&
                keyboard[craftingKey].wasPressedThisFrame)
            {
                ToggleCraftingUI();
            }

            if (inventoryKey != Key.None &&
                keyboard[inventoryKey].wasPressedThisFrame)
            {
                ToggleInventoryUI();
            }
        }
        // OpenUI가 꺼지면 ESCPanel이 다시 ESC를 직접 처리한다
        private void OnDisable()
        {
            if (escPanel != null)
                escPanel.HandlesEscapeKey = true;
        }

        // ESC 하나로 맨 위 팝업을 닫고, 열린 팝업이 없으면 ESC 패널을 연다
        private void HandleEscapeKey()
        {
            // ESCPanel이 OpenUI보다 늦게 생겼어도 이번 프레임에 같이 처리하지 않게 먼저 넘겨받는다
            TakeOverEscapeKey();

            // 채팅 중의 Esc는 채팅창을 닫는 데 쓰이므로 무시
            if (ChatUI.IsTyping)
                return;

            PopupUIManager manager = PopupUIManager.Instance;
            PopupUI top = manager != null ? manager.TopPopup : null;

            if (top != null)
            {
                // 제작창이면 재료를 반환하고 닫기
                if (top.gameObject == craftingUI)
                    CloseCraftingUI();
                else
                    manager.TryCloseTop();

                return;
            }

            if (escPanel != null)
                escPanel.Toggle();
        }

        // 씬의 ESCPanel을 찾아 자체 ESC 입력을 끈다
        private void TakeOverEscapeKey()
        {
            if (escPanel == null)
                escPanel = FindFirstObjectByType<ESCPanel>(FindObjectsInactive.Include);

            if (escPanel != null)
                escPanel.HandlesEscapeKey = false;
        }

        public void ToggleRecipeBook()
        {
            if (recipeBook == null)
                return;
            if (!recipeBook.TryGetComponent<PopupUI>(out var popup))
                return;
            bool wasOpen = popup.IsOpen;
            popup.Toggle();
            if (!wasOpen && popup.IsOpen && AudioManager.Instance != null)
                AudioManager.Instance.PlaySound(inventoryOpenSound);
        }
        public void ToggleInventoryUI()
        {
            if (inventoryUI == null)
                return;
            if (!inventoryUI.TryGetComponent<PopupUI>(out var popup))
                return;
            bool wasOpen = popup.IsOpen;
            popup.Toggle();
            if (!wasOpen && popup.IsOpen && AudioManager.Instance != null)
                AudioManager.Instance.PlaySound(inventoryOpenSound);
        }

        public void ToggleCraftingUI()
        {
            if (craftingUI == null)
                return;
            // 이미 열려 있으면 재료를 반환하고 닫기
            if (craftingUI.activeSelf)
            {
                CloseCraftingUI();
                return;
            }
            // 닫혀 있으면 매니저를 통해 열기
            if (!craftingUI.TryGetComponent<PopupUI>(out var popup))
                return;
            bool wasOpen = popup.IsOpen;
            popup.Open();
            if (!wasOpen && popup.IsOpen && AudioManager.Instance != null)
                AudioManager.Instance.PlaySound(CreatingOpenSound);
        }

        public void CloseCraftingUI()
        {
            if (craftingUI == null || !craftingUI.activeSelf)
                return;

            if (!craftingUI.TryGetComponent<PotionCrafting>(out var crafting))
                return;

            // 기존 함수가 재료를 반환하고 창을 끔
            crafting.Close();

            // 실제로 닫혔으면 매니저에서도 정리
            if (!craftingUI.activeSelf &&
                craftingUI.TryGetComponent<PopupUI>(out var popup))
            {
                popup.Close();
            }
        }
    }
}