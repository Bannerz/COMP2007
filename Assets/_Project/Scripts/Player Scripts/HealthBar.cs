using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
      [Header("References")]
    public Slider healthSlider;
    public PlayerController playerController;
    // Update is called once per frame
    void Update()
    {
        if (playerController != null && healthSlider != null)
        {
            float maxH = Mathf.Max(1f, playerController.maxHealth);
            healthSlider.value = Mathf.Clamp01(playerController.currentHealth / maxH);
        }
    }
}
