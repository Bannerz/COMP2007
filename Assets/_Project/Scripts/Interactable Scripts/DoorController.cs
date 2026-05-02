using UnityEngine;
using UnityEngine.UI;

public class DoorController : MonoBehaviour, IInteractable, IInteractablePrompt
{
    [Header("References")]
    public Animator animator;
    public AudioSource audioSource;
    public Image interactPrompt;

    [Header("Animation")]
    public string openAnimationName = "OpenDoor";
    public string closeAnimationName = "CloseDoor";
    public string isOpenParameterName = "IsOpen";

    [Header("Audio")]
    public AudioClip openingSound;
    public AudioClip closingSound;

    [Header("Interaction")]
    public Transform player;
    public float interactionRange = 2.5f;
    public KeyCode interactKey = KeyCode.E;
    public float interactionCooldown = 0.25f;
    public bool logInteractions = false;
    public bool handleOwnInteraction = false;

    [Header("State")]
    public bool startsOpen = false;

    private bool isOpen;
    private bool playerInRange;
    private float nextInteractTime;
    private Collider[] doorColliders;

    private void Awake()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        doorColliders = GetComponentsInChildren<Collider>();
    }

    private void Start()
    {
        isOpen = startsOpen;

        if (interactPrompt != null)
        {
            SetPromptVisible(false);
        }

        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
            {
                player = playerObject.transform;
            }
        }

        SetAnimatorBoolIfPresent(isOpenParameterName, isOpen);
    }

    private void Update()
    {
        if (!handleOwnInteraction)
        {
            return;
        }

        bool canPrompt = playerInRange || IsPlayerWithinRange();
        SetPromptVisible(canPrompt);

        if (CanInteract() && canPrompt && Input.GetKeyDown(interactKey))
        {
            Interact();
        }
    }

    public void Interact()
    {
        ToggleDoor();
    }

    public void InteractWithDoor()
    {
        ToggleDoor();
    }

    public void OpenDoor()
    {
        SetOpen(true);
    }

    public void CloseDoor()
    {
        SetOpen(false);
    }

    public void ToggleDoor()
    {
        SetOpen(!isOpen);
    }

    private void SetOpen(bool open)
    {
        nextInteractTime = Time.time + interactionCooldown;

        if (isOpen == open)
        {
            return;
        }

        isOpen = open;
        if (logInteractions)
        {
            Debug.Log($"{name} door is now {(isOpen ? "open" : "closed")}.", this);
        }

        PlayDoorAnimation(open);
        PlaySound(open ? openingSound : closingSound);
    }

    private void PlayDoorAnimation(bool open)
    {
        if (animator == null)
        {
            Debug.LogWarning($"{nameof(DoorController)} on {name} has no Animator assigned.", this);
            return;
        }

        SetAnimatorBoolIfPresent(isOpenParameterName, isOpen);

        SetAnimatorTriggerIfPresent(open ? openAnimationName : closeAnimationName);
    }

    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!handleOwnInteraction)
        {
            return;
        }

        if (other.CompareTag("Player"))
        {
            playerInRange = true;

            if (interactPrompt != null)
            {
                SetPromptVisible(true);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!handleOwnInteraction)
        {
            return;
        }

        if (other.CompareTag("Player"))
        {
            playerInRange = false;

            if (interactPrompt != null)
            {
                SetPromptVisible(IsPlayerWithinRange());
            }
        }
    }

    public void SetPromptVisible(bool visible)
    {
        if (interactPrompt != null && interactPrompt.gameObject.activeSelf != visible)
        {
            interactPrompt.gameObject.SetActive(visible);
        }
    }

    private bool IsPlayerWithinRange()
    {
        if (player == null)
        {
            return false;
        }

        if (doorColliders != null && doorColliders.Length > 0)
        {
            foreach (Collider doorCollider in doorColliders)
            {
                if (doorCollider == null)
                {
                    continue;
                }

                Vector3 closestPoint = doorCollider.ClosestPoint(player.position);
                if (Vector3.Distance(closestPoint, player.position) <= interactionRange)
                {
                    return true;
                }
            }
        }

        return Vector3.Distance(transform.position, player.position) <= interactionRange;
    }

    private bool CanInteract()
    {
        return Time.time >= nextInteractTime;
    }

    private void SetAnimatorBoolIfPresent(string parameterName, bool value)
    {
        if (animator == null || string.IsNullOrEmpty(parameterName))
        {
            return;
        }

        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.type == AnimatorControllerParameterType.Bool && parameter.name == parameterName)
            {
                animator.SetBool(parameterName, value);
                if (logInteractions)
                {
                    Debug.Log($"{name} set Animator bool {parameterName} to {value}.", this);
                }
                return;
            }
        }

        if (logInteractions)
        {
            Debug.LogWarning($"{name} Animator does not have a bool parameter named {parameterName}.", this);
        }
    }

    private bool SetAnimatorTriggerIfPresent(string parameterName)
    {
        if (animator == null || string.IsNullOrEmpty(parameterName))
        {
            return false;
        }

        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.type == AnimatorControllerParameterType.Trigger && parameter.name == parameterName)
            {
                animator.SetTrigger(parameterName);
                return true;
            }
        }

        return false;
    }
}
