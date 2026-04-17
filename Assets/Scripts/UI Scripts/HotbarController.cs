using System;
using UnityEngine;
using UnityEngine.UI;

public class HotbarController : MonoBehaviour
{
    [Header("Slot UI (9 images in order)")]
    [SerializeField] private Image[] slotImages = new Image[9];

    [Header("Slot Sprites")]
    [SerializeField] private Sprite leftSprite;
    [SerializeField] private Sprite leftSelectedSprite;
    [SerializeField] private Sprite middleSprite;
    [SerializeField] private Sprite middleSelectedSprite;
    [SerializeField] private Sprite rightSprite;
    [SerializeField] private Sprite rightSelectedSprite;

    [Header("Optional Item Objects (9 max)")]
    [SerializeField] private GameObject[] slotItems = new GameObject[9];
    [SerializeField] private bool activateOnlySelectedItem = true;

    [Header("Input")]
    [SerializeField] private bool allowScrollWrap = true;

    public int SelectedSlotIndex { get; private set; }
    public event Action<int> OnSelectedSlotChanged;

    private const int SlotCount = 9;

    private void Awake()
    {
        if (slotImages == null || slotImages.Length != SlotCount)
        {
            Debug.LogWarning("HotbarController expects exactly 9 slot images.");
        }

        SetSelectedSlot(0, true);
    }

    private void Update()
    {
        if (PauseMenu.isPaused)
            return;

        if (TryGetNumberKeySelection(out int numberKeySlot))
        {
            SetSelectedSlot(numberKeySlot);
        }

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.01f)
        {
            int direction = scroll > 0f ? -1 : 1;
            int next = SelectedSlotIndex + direction;

            if (allowScrollWrap)
            {
                if (next < 0) next = SlotCount - 1;
                if (next >= SlotCount) next = 0;
            }
            else
            {
                next = Mathf.Clamp(next, 0, SlotCount - 1);
            }

            SetSelectedSlot(next);
        }
    }

    public void SetSelectedSlot(int index, bool force = false)
    {
        index = Mathf.Clamp(index, 0, SlotCount - 1);

        if (!force && index == SelectedSlotIndex)
            return;

        SelectedSlotIndex = index;
        RefreshSlotSprites();
        RefreshActiveItems();
        OnSelectedSlotChanged?.Invoke(SelectedSlotIndex);
    }

    private bool TryGetNumberKeySelection(out int slotIndex)
    {
        if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
        {
            slotIndex = 0;
            return true;
        }
        if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2))
        {
            slotIndex = 1;
            return true;
        }
        if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3))
        {
            slotIndex = 2;
            return true;
        }
        if (Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.Keypad4))
        {
            slotIndex = 3;
            return true;
        }
        if (Input.GetKeyDown(KeyCode.Alpha5) || Input.GetKeyDown(KeyCode.Keypad5))
        {
            slotIndex = 4;
            return true;
        }
        if (Input.GetKeyDown(KeyCode.Alpha6) || Input.GetKeyDown(KeyCode.Keypad6))
        {
            slotIndex = 5;
            return true;
        }
        if (Input.GetKeyDown(KeyCode.Alpha7) || Input.GetKeyDown(KeyCode.Keypad7))
        {
            slotIndex = 6;
            return true;
        }
        if (Input.GetKeyDown(KeyCode.Alpha8) || Input.GetKeyDown(KeyCode.Keypad8))
        {
            slotIndex = 7;
            return true;
        }
        if (Input.GetKeyDown(KeyCode.Alpha9) || Input.GetKeyDown(KeyCode.Keypad9))
        {
            slotIndex = 8;
            return true;
        }

        slotIndex = -1;
        return false;
    }

    private void RefreshSlotSprites()
    {
        if (slotImages == null)
            return;

        for (int i = 0; i < SlotCount; i++)
        {
            if (i >= slotImages.Length || slotImages[i] == null)
                continue;

            bool isSelected = i == SelectedSlotIndex;
            slotImages[i].sprite = GetSpriteForSlot(i, isSelected);
        }
    }

    private Sprite GetSpriteForSlot(int slotIndex, bool selected)
    {
        bool isLeft = slotIndex == 0;
        bool isRight = slotIndex == SlotCount - 1;

        if (isLeft)
            return selected ? leftSelectedSprite : leftSprite;

        if (isRight)
            return selected ? rightSelectedSprite : rightSprite;

        return selected ? middleSelectedSprite : middleSprite;
    }

    private void RefreshActiveItems()
    {
        if (!activateOnlySelectedItem || slotItems == null)
            return;

        for (int i = 0; i < slotItems.Length; i++)
        {
            if (slotItems[i] == null)
                continue;

            slotItems[i].SetActive(i == SelectedSlotIndex);
        }
    }
}
