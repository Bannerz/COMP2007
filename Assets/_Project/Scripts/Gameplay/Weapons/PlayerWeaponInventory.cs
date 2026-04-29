using UnityEngine;

public class PlayerWeaponInventory : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private HotbarController hotbar;
    [SerializeField] private Transform weaponHolder;
    [SerializeField] private Transform attackOrigin;

    [Header("Input")]
    [SerializeField] private KeyCode attackKey = KeyCode.Mouse0;
    [SerializeField] private int swordSlotIndex = 1;

    [Header("Generated Holder")]
    [SerializeField] private Vector3 holderLocalPosition = new Vector3(0.45f, -0.35f, 0.75f);
    [SerializeField] private Vector3 holderLocalEulerAngles = new Vector3(0f, 0f, 0f);

    private void Awake()
    {
        if (hotbar == null)
            hotbar = FindObjectOfType<HotbarController>();

        EnsureWeaponHolder();
    }

    private void Update()
    {
        if (PauseMenu.isPaused)
            return;

        if (Input.GetKeyDown(attackKey))
            TryAttackWithSelectedWeapon();
    }

    public bool TryPickupWeapon(SwordWeapon weapon)
    {
        if (weapon == null || hotbar == null)
            return false;

        EnsureWeaponHolder();

        int selectedSlotIndex = swordSlotIndex;
        bool addedToPreferredSlot = hotbar.TrySetSlotItem(swordSlotIndex, weapon.gameObject);
        bool addedToAnySlot = addedToPreferredSlot || hotbar.TryAddSlotItem(weapon.gameObject, out selectedSlotIndex);

        if (!addedToAnySlot)
        {
            Debug.Log("Hotbar is full. Could not pick up weapon.");
            return false;
        }

        weapon.EquipTo(weaponHolder, transform);
        hotbar.SetSelectedSlot(selectedSlotIndex);
        return true;
    }

    private void TryAttackWithSelectedWeapon()
    {
        if (hotbar == null)
            return;

        GameObject selectedItem = hotbar.GetSlotItem(hotbar.SelectedSlotIndex);
        if (selectedItem == null || !selectedItem.activeInHierarchy)
            return;

        SwordWeapon weapon = selectedItem.GetComponent<SwordWeapon>();
        if (weapon == null)
            return;

        Transform origin = attackOrigin != null ? attackOrigin : GetFallbackAttackOrigin();
        weapon.TryAttack(origin, transform);
    }

    private void EnsureWeaponHolder()
    {
        if (weaponHolder != null)
        {
            if (attackOrigin == null)
                attackOrigin = weaponHolder;

            return;
        }

        Transform parent = GetFallbackAttackOrigin();
        GameObject holder = new GameObject("Weapon Holder");
        weaponHolder = holder.transform;
        weaponHolder.SetParent(parent, false);
        weaponHolder.localPosition = holderLocalPosition;
        weaponHolder.localEulerAngles = holderLocalEulerAngles;

        if (attackOrigin == null)
            attackOrigin = parent;
    }

    private Transform GetFallbackAttackOrigin()
    {
        Camera playerCamera = GetComponentInChildren<Camera>();
        if (playerCamera != null)
            return playerCamera.transform;

        if (Camera.main != null)
            return Camera.main.transform;

        return transform;
    }
}
