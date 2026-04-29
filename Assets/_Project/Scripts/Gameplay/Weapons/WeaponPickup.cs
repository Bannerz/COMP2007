using UnityEngine;

[RequireComponent(typeof(SwordWeapon))]
public class WeaponPickup : MonoBehaviour
{
    [SerializeField] private bool pickupOnTouch = true;

    private SwordWeapon weapon;
    private bool pickedUp;

    private void Awake()
    {
        weapon = GetComponent<SwordWeapon>();
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

        pickedUp = true;
        enabled = false;
    }
}
