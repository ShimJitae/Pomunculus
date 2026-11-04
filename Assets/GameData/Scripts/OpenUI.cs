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
        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
                return;
            if (recipeBookKey != Key.None && keyboard[recipeBookKey].wasPressedThisFrame)
                OpenRecipeBook();
            if (craftingKey != Key.None && keyboard[craftingKey].wasPressedThisFrame)
                OpenCraftingUI();
        }
        public void OpenRecipeBook()
        {
            if (recipeBook != null)
                recipeBook.SetActive(true);
        }
        public void OpenCraftingUI()
        {
            if (craftingUI != null)
                craftingUI.SetActive(true);
        }
    }
}