using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class OpenTutorialUI : MonoBehaviour
{
    [Header("튜토리얼")]
    [SerializeField] private TutorialUI tutorialUI;

    [Header("E키 안내")]
    [SerializeField] private GameObject interactionText;

    private readonly HashSet<Collider> nearbyColliders = new HashSet<Collider>();
    private bool openedHere;

    private void Start()
    {
        if (tutorialUI == null)
            tutorialUI = FindFirstObjectByType<TutorialUI>(FindObjectsInactive.Include);

        if (interactionText == null && tutorialUI != null)
            interactionText = tutorialUI.InteractionText;

        if (interactionText != null)
            interactionText.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsLocalPlayer(other))
            return;

        nearbyColliders.Add(other);
        RefreshInteractionText();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!nearbyColliders.Remove(other))
            return;

        if (nearbyColliders.Count > 0)
            return;

        CloseTutorial();
        RefreshInteractionText();
    }

    private void Update()
    {
        if (nearbyColliders.Count == 0 || tutorialUI == null)
            return;
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
            return;
        if (keyboard.eKey.wasPressedThisFrame)
        {
            if (tutorialUI.gameObject.activeSelf)
            {
                CloseTutorial();
            }
            else
            {
                openedHere = true;
                tutorialUI.Open();
            }
        }
        RefreshInteractionText();
    }

    private void CloseTutorial()
    {
        if (openedHere && tutorialUI != null)
            tutorialUI.Close();

        openedHere = false;
    }

    private void RefreshInteractionText()
    {
        if (interactionText == null)
            return;

        bool isOpen = tutorialUI != null && tutorialUI.gameObject.activeSelf;
        interactionText.SetActive(nearbyColliders.Count > 0 && !isOpen);
    }

    private void OnDisable()
    {
        nearbyColliders.Clear();
        CloseTutorial();

        if (interactionText != null)
            interactionText.SetActive(false);
    }

    private bool IsLocalPlayer(Collider other)
    {
        PlayerFSM player = other.GetComponentInParent<PlayerFSM>();

        if (player != null)
            return player.IsMine;

        return other.CompareTag("Player") ||
               (other.attachedRigidbody != null &&
                other.attachedRigidbody.CompareTag("Player"));
    }
}