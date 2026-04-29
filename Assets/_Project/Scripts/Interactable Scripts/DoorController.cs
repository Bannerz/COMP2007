using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DoorController : MonoBehaviour
{

    public Animator animator;
    public AudioSource audioSource;

    [Header("Settings")]
    public string openAnimationName = "OpenDoor";
    public AudioClip openingSound;

    private bool isOpened = false;
    private bool playerInRange = false;
    // Start is called before the first frame update
   public void InteractWithDoor() {
        // First interaction - open the door
        if (!isOpened) {
            OpenDoor();
        }
    }

     private void OpenDoor() {
        isOpened = true;
        
        // Play the opening animation
        if (animator != null) {
            animator.SetTrigger(openAnimationName);
        }
        
        // Play the opening sound
        if (audioSource != null && openingSound != null) {
            audioSource.PlayOneShot(openingSound);
        }
        
    }

    // Update is called once per frame
    void Update()
    {
        if (playerInRange && Input.GetKeyDown(KeyCode.E)) {
            InteractWithDoor();
        }
    }

    void OnTriggerEnter(Collider other) {
        if (other.CompareTag("Player")) {
            playerInRange = true;
        }
    }

    void OnTriggerExit(Collider other) {
        if (other.CompareTag("Player")) {
            playerInRange = false;
        }
    }
}
