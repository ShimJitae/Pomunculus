using TMPro;
using UnityEngine;
using System.Collections.Generic;
using Pomunculus.Items;

public class RecipeHintUI : MonoBehaviour
{
    [SerializeField] private GameObject hintPanel;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text hintText;
    [SerializeField] private GameObject InteractionText;
    [SerializeField] private RecipeBookUI recipeBook;

    public bool IsOpen => hintPanel != null && hintPanel.activeSelf;
    private void Start()
    {
        if (recipeBook == null)
        {
            recipeBook = FindFirstObjectByType<RecipeBookUI>(
                FindObjectsInactive.Include);
        }
    }
    public void Open(Line note, PotionData potion)
    {
        if (note == null || note.lines == null || note.lines.Length == 0)
            return;
        if (hintPanel == null)
            return;
        if (!hintPanel.TryGetComponent<PopupUI>(out var popup))
            return;
        titleText.text = note.SpeakerName;
        hintText.text = note.lines[0];
        popup.Open();
        // 힌트를 열었으면 해당 포션의 레시피 공개
        if (hintPanel.activeInHierarchy &&
            recipeBook != null && potion != null)
        {
            recipeBook.DiscoverPotion(potion.Id);
        }
    }
    private readonly HashSet<RecipeHintInteractable> nearbyHints = new HashSet<RecipeHintInteractable>();

    public void SetInteractionText(RecipeHintInteractable hint, bool isNearby)
    {
        if (isNearby)
            nearbyHints.Add(hint);
        else
            nearbyHints.Remove(hint);

        if (InteractionText != null)
            InteractionText.SetActive(nearbyHints.Count > 0);
    }

    public void Close()
    {
        if (hintPanel == null)
            return;
        if (hintPanel.TryGetComponent<PopupUI>(out var popup))
            popup.Close();
    }
}