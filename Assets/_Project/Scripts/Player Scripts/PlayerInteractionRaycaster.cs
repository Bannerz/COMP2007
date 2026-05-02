using UnityEngine;

public class PlayerInteractionRaycaster : MonoBehaviour
{
    [Header("Raycast")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private float interactionDistance = 3f;
    [SerializeField] private LayerMask interactableLayers = ~0;
    [SerializeField] private QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Collide;

    [Header("Input")]
    [SerializeField] private KeyCode interactKey = KeyCode.E;

    [Header("UI")]
    [SerializeField] private GameObject interactPrompt;

    private IInteractable currentInteractable;
    private IInteractablePrompt currentPrompt;

    private void Awake()
    {
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }

        SetGlobalPromptVisible(false);
    }

    private void Update()
    {
        FindInteractable();

        if (currentInteractable != null && Input.GetKeyDown(interactKey))
        {
            currentInteractable.Interact();
        }

        SetGlobalPromptVisible(currentInteractable != null);
    }

    private void FindInteractable()
    {
        IInteractable foundInteractable = null;
        IInteractablePrompt foundPrompt = null;

        if (playerCamera != null)
        {
            Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, interactionDistance, interactableLayers, triggerInteraction))
            {
                foundInteractable = hit.collider.GetComponentInParent<IInteractable>();

                if (foundInteractable != null && IsAvailable(foundInteractable))
                {
                    foundPrompt = hit.collider.GetComponentInParent<IInteractablePrompt>();
                }
                else
                {
                    foundInteractable = null;
                }
            }
        }

        if (!ReferenceEquals(foundInteractable, currentInteractable))
        {
            SetCurrentPromptVisible(false);

            currentInteractable = foundInteractable;
            currentPrompt = foundPrompt;

            SetCurrentPromptVisible(true);
        }
    }

    private void SetCurrentPromptVisible(bool visible)
    {
        if (currentPrompt != null)
        {
            currentPrompt.SetPromptVisible(visible);
        }
    }

    private bool IsAvailable(IInteractable interactable)
    {
        IInteractionAvailability availability = interactable as IInteractionAvailability;
        return availability == null || availability.CanInteract;
    }

    private void OnDisable()
    {
        SetCurrentPromptVisible(false);
        SetGlobalPromptVisible(false);
        currentInteractable = null;
        currentPrompt = null;
    }

    private void SetGlobalPromptVisible(bool visible)
    {
        if (interactPrompt != null && interactPrompt.activeSelf != visible)
        {
            interactPrompt.SetActive(visible);
        }
    }
}
