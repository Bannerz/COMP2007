using System.Collections;
using UnityEngine;

public class PortcullisController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private Transform portcullis;
    [SerializeField] private float openHeight = 4.84f;
    [SerializeField] private float moveDuration = 2f;
    [SerializeField] private bool startsOpen = false;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip openSound;
    [SerializeField] private AudioClip closeSound;

    public bool IsOpen { get; private set; }
    public bool IsMoving => moveRoutine != null;

    private Vector3 closedLocalPosition;
    private Coroutine moveRoutine;

    private void Awake()
    {
        if (portcullis == null)
        {
            portcullis = transform;
        }

        closedLocalPosition = portcullis.localPosition;
        IsOpen = startsOpen;

        if (startsOpen)
        {
            portcullis.localPosition = GetOpenPosition();
        }
    }

    public void Toggle()
    {
        SetOpen(!IsOpen);
    }

    public void Open()
    {
        SetOpen(true);
    }

    public void Close()
    {
        SetOpen(false);
    }

    public void SetOpen(bool open)
    {
        if (moveRoutine != null)
        {
            StopCoroutine(moveRoutine);
        }

        IsOpen = open;
        moveRoutine = StartCoroutine(MovePortcullis(open));
    }

    private IEnumerator MovePortcullis(bool open)
    {
        Vector3 startPosition = portcullis.localPosition;
        Vector3 targetPosition = open ? GetOpenPosition() : closedLocalPosition;

        PlaySound(open ? openSound : closeSound);

        if (moveDuration <= 0f)
        {
            portcullis.localPosition = targetPosition;
            moveRoutine = null;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < moveDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / moveDuration);
            t = Mathf.SmoothStep(0f, 1f, t);
            portcullis.localPosition = Vector3.LerpUnclamped(startPosition, targetPosition, t);
            yield return null;
        }

        portcullis.localPosition = targetPosition;
        moveRoutine = null;
    }

    private Vector3 GetOpenPosition()
    {
        return closedLocalPosition + Vector3.up * openHeight;
    }

    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
}
