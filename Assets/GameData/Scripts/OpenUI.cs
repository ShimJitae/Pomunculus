using UnityEngine;
using UnityEngine.InputSystem;

namespace Pomunculus.Items
{
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

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;

            if (keyboard == null)
                return;
            if (recipeBookKey != Key.None && keyboard[recipeBookKey].wasPressedThisFrame)
                ToggleRecipeBook();

            if (craftingKey != Key.None && keyboard[craftingKey].wasPressedThisFrame)
                ToggleCraftingUI();

            if (inventoryKey != Key.None && keyboard[inventoryKey].wasPressedThisFrame)
                ToggleInventoryUI();
        }

        public void ToggleRecipeBook()
        {
            if (recipeBook != null)
                recipeBook.SetActive(!recipeBook.activeSelf);
        }

        public void ToggleCraftingUI()
        {
            if (craftingUI == null)
                return;
            if (!craftingUI.activeSelf)
            {
                craftingUI.SetActive(true);
                return;
            }
            if (craftingUI.TryGetComponent<PotionCrafting>(out var crafting))
                crafting.Close();
        }

        public void ToggleInventoryUI()
        {
            if (inventoryUI != null)
                inventoryUI.SetActive(!inventoryUI.activeSelf);
        }


    }
}