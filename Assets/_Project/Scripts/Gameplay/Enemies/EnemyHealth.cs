using UnityEngine;
using UnityEngine.AI;

public class EnemyHealth : MonoBehaviour, IDamageable
{
    [SerializeField] private float maxHealth = 30f;
    [SerializeField] private bool destroyOnDeath = true;
    [SerializeField] private float destroyDelay = 3f;
    [SerializeField] private Animator animator;
    [SerializeField] private string deathTrigger = "Die";
    [SerializeField] private bool disableCollidersOnDeath = true;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip[] hitSounds;
    [SerializeField] private AudioClip[] deathSounds;
    [SerializeField] private float hitVolume = 1f;
    [SerializeField] private float deathVolume = 1f;
    [SerializeField] private Vector2 pitchRange = new Vector2(0.95f, 1.05f);

    public float CurrentHealth { get; private set; }

    private void Awake()
    {
        CurrentHealth = maxHealth;

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
    }

    public void TakeDamage(float amount)
    {
        if (amount <= 0f || CurrentHealth <= 0f)
        {
            return;
        }

        CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);

        if (CurrentHealth <= 0f)
        {
            Die();
        }
        else
        {
            PlayRandomSound(hitSounds, hitVolume);
        }
    }

    private void Die()
    {
        PlayRandomSound(deathSounds, deathVolume);
        animator?.SetTrigger(deathTrigger);

        SimpleEnemyAI enemyAI = GetComponent<SimpleEnemyAI>();
        if (enemyAI != null)
        {
            enemyAI.StopEnemy();
        }

        NavMeshAgent navMeshAgent = GetComponent<NavMeshAgent>();
        if (navMeshAgent != null)
        {
            navMeshAgent.enabled = false;
        }

        if (disableCollidersOnDeath)
        {
            foreach (Collider enemyCollider in GetComponentsInChildren<Collider>())
            {
                enemyCollider.enabled = false;
            }
        }

        if (destroyOnDeath)
        {
            Destroy(gameObject, destroyDelay);
        }
        else
        {
            enabled = false;
        }
    }

    private void PlayRandomSound(AudioClip[] clips, float volume)
    {
        if (clips == null || clips.Length == 0)
        {
            return;
        }

        AudioClip clip = clips[Random.Range(0, clips.Length)];
        if (clip == null)
        {
            return;
        }

        if (audioSource == null)
        {
            AudioSource.PlayClipAtPoint(clip, transform.position, volume);
            return;
        }

        audioSource.pitch = Random.Range(pitchRange.x, pitchRange.y);
        audioSource.PlayOneShot(clip, volume);
    }
}
