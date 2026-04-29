using UnityEngine;

[RequireComponent(typeof(SwordWeapon))]
public class WeaponPickup : MonoBehaviour
{
    [SerializeField] private bool pickupOnTouch = true;

    [Header("Idle Animation")]
    [SerializeField] private bool animateWhileWaiting = true;
    [SerializeField] private float spinSpeed = 90f;
    [SerializeField] private float bobHeight = 0.15f;
    [SerializeField] private float bobSpeed = 2f;

    [Header("Glow")]
    [SerializeField] private bool createGlowLight = true;
    [SerializeField] private Color glowColor = new Color(1f, 0.78f, 0.35f);
    [SerializeField] private float glowIntensity = 0.75f;
    [SerializeField] private float glowRange = 2.2f;
    [SerializeField] private float glowPulseAmount = 0.25f;
    [SerializeField] private float glowPulseSpeed = 2f;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip pickupSound;
    [SerializeField] private float pickupVolume = 1f;

    private SwordWeapon weapon;
    private Light glowLight;
    private bool pickedUp;
    private Vector3 startLocalPosition;
    private bool hasStartPosition;

    private void Awake()
    {
        weapon = GetComponent<SwordWeapon>();

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        SetupGlowLight();
    }

    private void OnEnable()
    {
        startLocalPosition = transform.localPosition;
        hasStartPosition = true;

        if (glowLight != null && !pickedUp)
            glowLight.enabled = true;
    }

    private void Update()
    {
        if (pickedUp || !animateWhileWaiting)
            return;

        if (!hasStartPosition)
        {
            startLocalPosition = transform.localPosition;
            hasStartPosition = true;
        }

        transform.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.World);

        float bobOffset = Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        transform.localPosition = startLocalPosition + Vector3.up * bobOffset;

        if (glowLight != null)
        {
            float pulse = 1f + Mathf.Sin(Time.time * glowPulseSpeed) * glowPulseAmount;
            glowLight.intensity = glowIntensity * pulse;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (pickupOnTouch)
            TryPickup(other);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (pickupOnTouch && collision.collider != null)
            TryPickup(collision.collider);
    }

    private void TryPickup(Collider other)
    {
        if (pickedUp || other == null)
            return;

        PlayerWeaponInventory inventory = other.GetComponentInParent<PlayerWeaponInventory>();
        if (inventory == null)
            return;

        if (!inventory.TryPickupWeapon(weapon))
            return;

        PlayPickupSound();

        pickedUp = true;
        if (glowLight != null)
            glowLight.enabled = false;

        enabled = false;
    }

    private void SetupGlowLight()
    {
        if (!createGlowLight)
            return;

        glowLight = GetComponentInChildren<Light>();
        if (glowLight == null)
        {
            GameObject lightObject = new GameObject("Pickup Glow");
            lightObject.transform.SetParent(transform, false);
            lightObject.transform.localPosition = Vector3.zero;
            glowLight = lightObject.AddComponent<Light>();
        }

        glowLight.type = LightType.Point;
        glowLight.color = glowColor;
        glowLight.intensity = glowIntensity;
        glowLight.range = glowRange;
        glowLight.shadows = LightShadows.None;
    }

    private void PlayPickupSound()
    {
        if (pickupSound == null)
            return;

        if (audioSource != null)
        {
            audioSource.PlayOneShot(pickupSound, pickupVolume);
            return;
        }

        AudioSource.PlayClipAtPoint(pickupSound, transform.position, pickupVolume);
    }
}
