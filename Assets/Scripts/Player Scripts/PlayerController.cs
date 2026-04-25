using UnityEngine;

public class PlayerController : MonoBehaviour, IDamageable
{
    [Header("Health Settings")]
    public float currentHealth = 100f;
    public float maxHealth = 100f;
    public float healthRegenRate = 5f;
    public float healthRegenDelay = 3f;

    [Header("Stamina Settings")]
    public float currentStamina;
    public float maxStamina = 100f;
    public float sprintStaminaDrain = 30f;
    public float climbStaminaDrain = 20f;
    public float staminaRegenRate = 15f;
    public float staminaRegenDelay = 1f;

    private float staminaRegenTimer = 0f;
    private float healthRegenTimer = 0f;
    private bool isSprinting = false;
    private bool isClimbing = false;
    

    private void Start()
    {
        currentStamina = maxStamina;
    }

    private void Update()
    {
        HandleStamina();
        HandleHealthRegen();

        // interaction handled by interactable objects via triggers (e.g., ChestController)
    }


    private void HandleStamina()
    {
        if (isSprinting || isClimbing)
        {
            float drainRate = isClimbing ? climbStaminaDrain : sprintStaminaDrain;
            currentStamina = Mathf.Max(0f, currentStamina - drainRate * Time.deltaTime);
            staminaRegenTimer = staminaRegenDelay;

            if (currentStamina <= 0f)
            {
                isSprinting = false;
                isClimbing = false;
            }
        }
        else
        {
            staminaRegenTimer -= Time.deltaTime;
            if (staminaRegenTimer <= 0f)
                currentStamina = Mathf.Min(maxStamina, currentStamina + staminaRegenRate * Time.deltaTime);
        }
    }

    public bool CanSprint()
    {
        return currentStamina > 0f;
    }

    public bool CanClimb()
    {
        return currentStamina > 0f;
    }

    public void SetSprinting(bool sprinting)
    {
        isSprinting = sprinting && CanSprint();
    }

    public void SetClimbing(bool climbing)
    {
        isClimbing = climbing && CanClimb();
    }

    private void HandleHealthRegen()
    {
        if (currentHealth >= maxHealth) return;

        healthRegenTimer -= Time.deltaTime;
        if (healthRegenTimer <= 0f)
            currentHealth = Mathf.Min(maxHealth, currentHealth + healthRegenRate * Time.deltaTime);
    }

    // Public damage API used by other systems (DamageZone, traps, bullets etc.)
    public void TakeDamage(float amount)
    {
        if (amount <= 0f) return;

        currentHealth = Mathf.Max(0f, currentHealth - amount);
        healthRegenTimer = healthRegenDelay;

        if (currentHealth <= 0f)
        {
            Debug.Log("Player died");
        }
    }
}
