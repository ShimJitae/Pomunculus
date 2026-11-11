using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Pomunculus.Items
{
    public class OpenCraftingUI : MonoBehaviour
    {

        [SerializeField] private OpenUI openUI;
        [SerializeField] private GameObject interactionText;
        private readonly HashSet<Collider> nearbyColliders = new HashSet<Collider>();

        private void OnTriggerEnter(Collider other)
        {
            if (!IsLocalPlayer(other))
                return;

            nearbyColliders.Add(other);

            if (interactionText != null)
                interactionText.SetActive(true);
        }

        private void OnTriggerExit(Collider other)
        {
            if (!nearbyColliders.Remove(other) || nearbyColliders.Count > 0)
                return;

            if (interactionText != null)
                interactionText.SetActive(false);

            if (openUI != null)
                openUI.CloseCraftingUI();
        }
        private void Start()
        {
            if (openUI == null)
                openUI = FindFirstObjectByType<OpenUI>();

            if (interactionText == null && openUI != null)
                interactionText = openUI.CraftingInteractionText;
        }

        private void Update()
        {
            if (nearbyColliders.Count == 0 || openUI == null)
                return;

            if (Keyboard.current?.eKey.wasPressedThisFrame == true)
                openUI.ToggleCraftingUI();
        }

        private void OnDisable()
        {
            nearbyColliders.Clear();

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
}