using TMPro;
using UnityEngine;
using System.Collections.Generic;

public class RecipeHintUI : MonoBehaviour
{
    [SerializeField] private GameObject hintPanel;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text hintText;
    [SerializeField] private GameObject InteractionText;
    public bool IsOpen => hintPanel != null && hintPanel.activeSelf;

    public void Open(Line note)
    {
        if (note == null || note.lines == null || note.lines.Length == 0)
            return;
        if (hintPanel == null)
            return;

        titleText.text = note.SpeakerName;
        hintText.text = note.lines[0];
        if (hintPanel.TryGetComponent<PopupUI>(out var popup))
            popup.Open();
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