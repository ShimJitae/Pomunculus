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
        private bool pausedEscPanel;

        public GameObject CraftingInteractionText => craftingInteractionText;

        private void Update()
        {
            RefreshEscPanel();
            Keyboard keyboard = Keyboard.current;

            if (keyboard == null)
                return;

            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                PopupUIManager manager = PopupUIManager.Instance;
                if (manager == null)
                    return;
                PopupUI top = manager.TopPopup;
                if (top == null)
                    return;
                // 제작창이면 재료를 반환하고 닫기
                if (top.gameObject == craftingUI)
                    CloseCraftingUI();
                else
                    manager.TryCloseTop();

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
        private void OnDisable()
        {
            // OpenUI가 꺼지면 잠깐 껐던 ESCPanel 복원
            if (pausedEscPanel && escPanel != null)
                escPanel.enabled = true;

            pausedEscPanel = false;
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
        private void RefreshEscPanel()
        {
            if (escPanel == null)
                escPanel = FindFirstObjectByType<ESCPanel>(FindObjectsInactive.Include);
            if (escPanel == null)
                return;
            PopupUIManager manager = PopupUIManager.Instance;
            bool hasPopup = manager != null && manager.HasOpenPopup;
            if (hasPopup)
            {
                if (escPanel.enabled)
                {
                    escPanel.enabled = false;
                    pausedEscPanel = true;
                }
            }
            else if (pausedEscPanel)
            {
                escPanel.enabled = true;
                pausedEscPanel = false;
            }
        }

    }
}