using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RangePopup : MonoBehaviour
{
    [Tooltip("Assign the Image (or any UI GameObject) on the Canvas to enable when player enters the trigger.")]
    public GameObject popupImageObject;

    [Tooltip("Player tag to check for. Default is 'Player'.")]
    public string playerTag = "Player";

    void Start()
    {
        if (popupImageObject != null)
            popupImageObject.SetActive(false);
    }

    // Support both 3D and 2D triggers by delegating to the same handlers
    private void OnTriggerEnter(Collider other)
    {
        HandleEnter(other.gameObject);
    }

    private void OnTriggerExit(Collider other)
    {
        HandleExit(other.gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        HandleEnter(other.gameObject);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        HandleExit(other.gameObject);
    }

    private void HandleEnter(GameObject other)
    {
        if (other.CompareTag(playerTag))
        {
            if (popupImageObject != null)
                popupImageObject.SetActive(true);
            else
                Debug.LogWarning("RangePopup: popupImageObject is not assigned.");
        }
    }

    private void HandleExit(GameObject other)
    {
        if (other.CompareTag(playerTag))
        {
            if (popupImageObject != null)
                popupImageObject.SetActive(false);
        }
    }
}
