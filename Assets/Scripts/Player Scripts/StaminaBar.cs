using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StaminaBar : MonoBehaviour {
    
    [Header("References")]
    public Slider staminaSlider;
    public PlayerController playerController;
    
    void Update() {
        if (playerController != null && staminaSlider != null) {
            // Update slider value based on player's current stamina
            staminaSlider.value = playerController.currentStamina / playerController.maxStamina;
        }
    }
}
