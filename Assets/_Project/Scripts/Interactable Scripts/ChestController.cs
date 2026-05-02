using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ChestController : MonoBehaviour, IInteractable, IInteractablePrompt, IInteractionAvailability {
    
    [Header("References")]
    public Animator animator;
    public AudioSource audioSource;
    public ParticleSystem particleSystem;
    public GameObject coinsMesh;
    public Light chestLight;
    [Header("UI")]
    public Image interactPrompt;
    
    [Header("Settings")]
    public string openAnimationName = "Open";
    public AudioClip openingSound;
    public AudioClip particleSound;
    public AudioClip[] coinCollectSounds;
    public float interactionDistance = 2f;
    public float particleDelayTime = 2f;
    public bool handleOwnInteraction = false;
    
    [Header("Coins")]
    public int coinCount = 10;
    
    private bool isOpened = false;
    private bool isLooted = false;
    private bool playerInRange = false;

    public bool CanInteract => !isLooted;
    
    void Start() {
        // Ensure light is off when chest is closed
        if (chestLight != null) {
            chestLight.enabled = false;
        }

        SetPromptVisible(false);
    }
    
    public void InteractWithChest() {
        // First interaction - open the chest
        if (!isOpened) {
            OpenChest();
        }
        // Second interaction - loot the chest
        else if (isOpened && !isLooted) {
            LootChest();
        }
    }

    // IInteractable implemention called by PlayerController when player presses Interact.
    public void Interact()
    {
        InteractWithChest();
    }
    
    private void OpenChest() {
        isOpened = true;
        
        // Turn on the light with delay
        if (chestLight != null) {
            StartCoroutine(TurnOnLightWithDelay(2f));
        }
        
        // Play the opening animation
        if (animator != null) {
            animator.SetTrigger(openAnimationName);
        }
        
        // Play the opening sound
        if (audioSource != null && openingSound != null) {
            audioSource.PlayOneShot(openingSound);
        }
        
        // Play the particle system with delay
        if (particleSystem != null) {
            StartCoroutine(PlayParticlesWithDelay(particleDelayTime));
        }
    }
    
    private void LootChest() {
        isLooted = true;
        
        // Turn off the light
        if (chestLight != null) {
            chestLight.enabled = false;
        }
        
        // Hide the coins mesh
        if (coinsMesh != null) {
            coinsMesh.SetActive(false);
        }
        
        // Stop the particle system
        if (particleSystem != null) {
            particleSystem.Stop();
        }
        
        // Play a random coin collect sound
        if (audioSource != null && coinCollectSounds.Length > 0) {
            AudioClip randomCoinSound = coinCollectSounds[Random.Range(0, coinCollectSounds.Length)];
            audioSource.PlayOneShot(randomCoinSound);
        }
        
        GameStatsUI.Instance?.AddGold(coinCount);
        GameStatsUI.Instance?.ChestLooted();
        Debug.Log($"Collected {coinCount} coins!");
    }
    
    private IEnumerator TurnOnLightWithDelay(float delay) {
        yield return new WaitForSeconds(delay);
        if (chestLight != null) {
            chestLight.enabled = true;
        }
    }
    
    private IEnumerator PlayParticlesWithDelay(float delay) {
        yield return new WaitForSeconds(delay);
        particleSystem.Play();
        
        // Play the particle sound
        if (audioSource != null && particleSound != null) {
            audioSource.PlayOneShot(particleSound);
        }
    }

    void Update() {
        if (handleOwnInteraction && playerInRange && Input.GetKeyDown(KeyCode.E)) {
            InteractWithChest();
        }
    }

    void OnTriggerEnter(Collider other) {
        if (other.CompareTag("Player")) {
            playerInRange = true;
            if (handleOwnInteraction) SetPromptVisible(true);
        }
    }

    void OnTriggerExit(Collider other) {
        if (other.CompareTag("Player")) {
            playerInRange = false;
            if (handleOwnInteraction) SetPromptVisible(false);
        }
    }

    public void SetPromptVisible(bool visible) {
        if (interactPrompt != null && interactPrompt.gameObject.activeSelf != visible) {
            interactPrompt.gameObject.SetActive(visible);
        }
    }

}


