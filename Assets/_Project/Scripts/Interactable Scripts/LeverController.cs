using UnityEngine;
using UnityEngine.UI;

public class LeverController : MonoBehaviour, IInteractable, IInteractablePrompt
{
    [Header("References")]
    [SerializeField] private PortcullisController portcullis;
    [SerializeField] private Animator leverAnimator;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private Image interactPrompt;

    [Header("Animation")]
    [SerializeField] private string toggleTriggerName = "Toggle";
    [SerializeField] private string isOnParameterName = "IsOn";

    [Header("Audio")]
    [SerializeField] private AudioClip leverSound;

    [Header("Interaction")]
    [SerializeField] private Transform player;
    [SerializeField] private float interactionRange = 2.5f;
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private bool ignoreInputWhilePortcullisMoves = true;
    [SerializeField] private bool handleOwnInteraction = false;

    private bool playerInRange;
    private bool isOn;
    private Collider[] leverColliders;

    private void Awake()
    {
        if (leverAnimator == null)
        {
            leverAnimator = GetComponent<Animator>();
        }

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        leverColliders = GetComponentsInChildren<Collider>();
    }

    private void Start()
    {
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

        if (portcullis != null)
        {
            isOn = portcullis.IsOpen;
        }

        SetAnimatorBoolIfPresent(isOnParameterName, isOn);
    }

    private void Update()
    {
        if (!handleOwnInteraction)
        {
            return;
        }

        bool canPrompt = playerInRange || IsPlayerWithinRange();
        SetPromptVisible(canPrompt);

        if (canPrompt && Input.GetKeyDown(interactKey))
        {
            Interact();
        }
    }

    public void Interact()
    {
        if (portcullis == null)
        {
            Debug.LogWarning($"{nameof(LeverController)} on {name} has no portcullis assigned.", this);
            return;
        }

        if (ignoreInputWhilePortcullisMoves && portcullis.IsMoving)
        {
            return;
        }

        isOn = !isOn;
        portcullis.Toggle();
        PlayLeverFeedback();
    }

    private void PlayLeverFeedback()
    {
        if (leverAnimator != null)
        {
            SetAnimatorBoolIfPresent(isOnParameterName, isOn);
            SetAnimatorTriggerIfPresent(toggleTriggerName);
        }

        if (audioSource != null && leverSound != null)
        {
            audioSource.PlayOneShot(leverSound);
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

        if (leverColliders != null && leverColliders.Length > 0)
        {
            foreach (Collider leverCollider in leverColliders)
            {
                if (leverCollider == null)
                {
                    continue;
                }

                Vector3 closestPoint = leverCollider.ClosestPoint(player.position);
                if (Vector3.Distance(closestPoint, player.position) <= interactionRange)
                {
                    return true;
                }
            }
        }

        return Vector3.Distance(transform.position, player.position) <= interactionRange;
    }

    private void SetAnimatorBoolIfPresent(string parameterName, bool value)
    {
        if (leverAnimator == null || string.IsNullOrWhiteSpace(parameterName))
        {
            return;
        }

        foreach (AnimatorControllerParameter parameter in leverAnimator.parameters)
        {
            if (parameter.type == AnimatorControllerParameterType.Bool && parameter.name == parameterName)
            {
                leverAnimator.SetBool(parameterName, value);
                return;
            }
        }
    }

    private void SetAnimatorTriggerIfPresent(string parameterName)
    {
        if (leverAnimator == null || string.IsNullOrWhiteSpace(parameterName))
        {
            return;
        }

        foreach (AnimatorControllerParameter parameter in leverAnimator.parameters)
        {
            if (parameter.type == AnimatorControllerParameterType.Trigger && parameter.name == parameterName)
            {
                leverAnimator.SetTrigger(parameterName);
                return;
            }
        }
    }
}
