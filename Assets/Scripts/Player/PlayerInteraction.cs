using UnityEngine;
using Mirror;

namespace ZombieSurvival.Player
{
    public class PlayerInteraction : NetworkBehaviour
    {
        [Header("Interaction Settings")]
        [SerializeField] private float interactionRange = 3f;
        [SerializeField] private LayerMask interactableMask;
        [SerializeField] private KeyCode interactKey = KeyCode.E;

        private Camera playerCamera;
        private IInteractable currentInteractable;

        public IInteractable CurrentInteractable => currentInteractable;
        public bool HasInteractable => currentInteractable != null;

        private void Start()
        {
            if (isLocalPlayer)
            {
                playerCamera = Camera.main;
            }
        }

        private void Update()
        {
            if (!isLocalPlayer) return;

            CheckForInteractable();

            if (currentInteractable != null && Input.GetKeyDown(interactKey))
            {
                currentInteractable.Interact(gameObject);
            }
        }

        private void CheckForInteractable()
        {
            if (playerCamera == null) return;

            Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);

            if (Physics.Raycast(ray, out RaycastHit hit, interactionRange, interactableMask))
            {
                var interactable = hit.collider.GetComponent<IInteractable>();
                if (interactable != null && interactable.CanInteract)
                {
                    currentInteractable = interactable;
                    return;
                }
            }

            currentInteractable = null;
        }
    }

    public interface IInteractable
    {
        string InteractionPrompt { get; }
        bool CanInteract { get; }
        void Interact(GameObject interactor);
    }
}
