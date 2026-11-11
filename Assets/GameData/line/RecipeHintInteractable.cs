using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class RecipeHintInteractable : MonoBehaviour
{
    [SerializeField] private Line note;

    private readonly HashSet<Collider> nearbyColliders = new HashSet<Collider>();
    private RecipeHintUI hintUI;

    private void Start()
    {
        hintUI = FindFirstObjectByType<RecipeHintUI>();
    }
    private void OnTriggerEnter(Collider other)
    {
        PlayerFSM player = other.GetComponentInParent<PlayerFSM>();
        bool isLocalPlayer = player != null
            ? player.IsMine
            : other.CompareTag("Player") ||
              (other.attachedRigidbody != null && other.attachedRigidbody.CompareTag("Player"));
        if (!isLocalPlayer)
            return;
        nearbyColliders.Add(other);
        if (hintUI == null)
            hintUI = FindFirstObjectByType<RecipeHintUI>();

        if (hintUI != null)
            hintUI.SetInteractionText(this, true);
    }
    private void OnTriggerExit(Collider other)
    {
        if (nearbyColliders.Remove(other) && nearbyColliders.Count == 0 && hintUI != null)
            hintUI.SetInteractionText(this, false);
    }
    private void OnDisable()
    {
        nearbyColliders.Clear();

        if (hintUI != null)
            hintUI.SetInteractionText(this, false);
    }
    private void Update()
    {
        if (nearbyColliders.Count == 0 || Keyboard.current?.eKey.wasPressedThisFrame != true)
            return;

        foreach (Collider other in nearbyColliders)
        {
            if (other == null || !other.enabled || !other.gameObject.activeInHierarchy)
                continue;
            PlayerFSM player = other.GetComponentInParent<PlayerFSM>();
            if (player != null)
            {
                if (!player.IsMine)
                    continue;
            }
            else if (!other.CompareTag("Player") &&
                     (other.attachedRigidbody == null || !other.attachedRigidbody.CompareTag("Player")))
            {
                continue;
            }
            if (hintUI == null)
                hintUI = FindFirstObjectByType<RecipeHintUI>();
            if (hintUI != null)
            {
                if (hintUI.IsOpen)
                    hintUI.Close();
                else
                    hintUI.Open(note);
            }
            break;
        }
    }
}