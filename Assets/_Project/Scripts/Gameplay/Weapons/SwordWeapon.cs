using System;
using System.Collections;
using UnityEngine;

public class SwordWeapon : MonoBehaviour
{
    [Header("Attack")]
    [SerializeField] private float damage = 25f;
    [SerializeField] private float range = 2.2f;
    [SerializeField] private float radius = 0.55f;
    [SerializeField] private float cooldown = 0.65f;
    [SerializeField] private LayerMask attackMask = ~0;

    [Header("Held Pose")]
    [SerializeField] private Vector3 heldLocalPosition = new Vector3(0.38f, -0.34f, 0.72f);
    [SerializeField] private Vector3 heldLocalEulerAngles = new Vector3(18f, 96f, -35f);
    [SerializeField] private Vector3 heldLocalScale = new Vector3(1f, 1f, 1f);

    [Header("Procedural Swing")]
    [SerializeField] private bool useProceduralSwing = true;
    [SerializeField] private float swingForwardTime = 0.12f;
    [SerializeField] private float swingReturnTime = 0.2f;
    [SerializeField] private Vector3 swingLocalPositionOffset = new Vector3(0.05f, -0.02f, 0.18f);
    [SerializeField] private Vector3 swingLocalEulerOffset = new Vector3(25f, -75f, 18f);

    [Header("Feedback")]
    [SerializeField] private Animator animator;
    [SerializeField] private string attackTrigger = "Attack";
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip attackSound;
    [SerializeField] private AudioClip[] swingSounds;
    [SerializeField] private float swingSoundVolume = 1f;
    [SerializeField] private Vector2 swingPitchRange = new Vector2(0.95f, 1.05f);

    private Collider[] weaponColliders;
    private Rigidbody weaponRigidbody;
    private Transform ownerRoot;
    private float nextAttackTime;
    private Coroutine swingRoutine;

    private void Awake()
    {
        weaponColliders = GetComponentsInChildren<Collider>(true);
        weaponRigidbody = GetComponent<Rigidbody>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }

    public void EquipTo(Transform holder, Transform owner)
    {
        ownerRoot = owner;

        if (weaponRigidbody != null)
        {
            weaponRigidbody.velocity = Vector3.zero;
            weaponRigidbody.angularVelocity = Vector3.zero;
            weaponRigidbody.isKinematic = true;
            weaponRigidbody.useGravity = false;
        }

        foreach (Collider col in weaponColliders)
        {
            if (col != null)
                col.enabled = false;
        }

        transform.SetParent(holder, false);
        transform.localPosition = heldLocalPosition;
        transform.localEulerAngles = heldLocalEulerAngles;
        transform.localScale = heldLocalScale;
    }

    public bool TryAttack(Transform attackOrigin, Transform owner)
    {
        if (Time.time < nextAttackTime)
            return false;

        nextAttackTime = Time.time + cooldown;
        ownerRoot = owner != null ? owner : ownerRoot;

        if (animator != null && !string.IsNullOrEmpty(attackTrigger))
            animator.SetTrigger(attackTrigger);

        PlayProceduralSwing();

        PlaySwingSound();

        Transform origin = attackOrigin != null ? attackOrigin : transform;
        RaycastHit[] hits = Physics.SphereCastAll(
            origin.position,
            radius,
            origin.forward,
            range,
            attackMask,
            QueryTriggerInteraction.Ignore);

        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider == null || IsOwnCollider(hit.collider.transform))
                continue;

            IDamageable damageable = hit.collider.GetComponentInParent<IDamageable>();
            if (damageable == null)
                continue;

            damageable.TakeDamage(damage);
            return true;
        }

        return false;
    }

    private void PlayProceduralSwing()
    {
        if (!useProceduralSwing || !gameObject.activeInHierarchy)
            return;

        if (swingRoutine != null)
            StopCoroutine(swingRoutine);

        swingRoutine = StartCoroutine(SwingRoutine());
    }

    private void PlaySwingSound()
    {
        AudioClip clip = GetRandomSwingClip();
        if (clip == null)
            return;

        if (audioSource == null)
        {
            AudioSource.PlayClipAtPoint(clip, transform.position, swingSoundVolume);
            return;
        }

        audioSource.pitch = UnityEngine.Random.Range(swingPitchRange.x, swingPitchRange.y);
        audioSource.PlayOneShot(clip, swingSoundVolume);
    }

    private AudioClip GetRandomSwingClip()
    {
        if (swingSounds != null && swingSounds.Length > 0)
        {
            AudioClip clip = swingSounds[UnityEngine.Random.Range(0, swingSounds.Length)];
            if (clip != null)
                return clip;
        }

        return attackSound;
    }

    private IEnumerator SwingRoutine()
    {
        Quaternion heldRotation = Quaternion.Euler(heldLocalEulerAngles);
        Vector3 swingPosition = heldLocalPosition + swingLocalPositionOffset;
        Quaternion swingRotation = heldRotation * Quaternion.Euler(swingLocalEulerOffset);

        yield return MoveToPose(swingPosition, swingRotation, swingForwardTime);
        yield return MoveToPose(heldLocalPosition, heldRotation, swingReturnTime);

        transform.localPosition = heldLocalPosition;
        transform.localRotation = heldRotation;
        swingRoutine = null;
    }

    private IEnumerator MoveToPose(Vector3 targetPosition, Quaternion targetRotation, float duration)
    {
        Vector3 startPosition = transform.localPosition;
        Quaternion startRotation = transform.localRotation;

        if (duration <= 0f)
        {
            transform.localPosition = targetPosition;
            transform.localRotation = targetRotation;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            t = 1f - Mathf.Pow(1f - t, 3f);

            transform.localPosition = Vector3.Lerp(startPosition, targetPosition, t);
            transform.localRotation = Quaternion.Slerp(startRotation, targetRotation, t);
            yield return null;
        }
    }

    private bool IsOwnCollider(Transform hitTransform)
    {
        if (hitTransform == null)
            return true;

        if (hitTransform == transform || hitTransform.IsChildOf(transform))
            return true;

        return ownerRoot != null && hitTransform.IsChildOf(ownerRoot);
    }

    private void OnDrawGizmosSelected()
    {
        Transform origin = Camera.main != null ? Camera.main.transform : transform;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(origin.position + origin.forward * range, radius);
        Gizmos.DrawLine(origin.position, origin.position + origin.forward * range);
    }
}
