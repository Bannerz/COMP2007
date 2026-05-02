using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class EndGameTrigger : MonoBehaviour
{
    [Header("End Condition")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private bool requireAllEnemiesKilled = true;

    [Header("End Game")]
    [SerializeField] private GameObject winScreen;
    [SerializeField] private bool pauseGame = true;
    [SerializeField] private bool unlockCursor = true;

    [Header("Optional Scene Load")]
    [SerializeField] private bool loadSceneOnEnd = false;
    [SerializeField] private int sceneBuildIndex = 0;
    [SerializeField] private float sceneLoadDelay = 0f;

    [Header("Feedback")]
    [SerializeField] private bool logBlockedAttempt = true;

    private bool hasEnded;

    private void Awake()
    {
        if (winScreen != null)
        {
            winScreen.SetActive(false);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasEnded || !other.CompareTag(playerTag))
        {
            return;
        }

        if (requireAllEnemiesKilled && !AreAllEnemiesKilled())
        {
            if (logBlockedAttempt)
            {
                int remainingEnemies = GameStatsUI.Instance != null
                    ? GameStatsUI.Instance.RemainingEnemies
                    : CountLivingEnemies();

                Debug.Log($"Exit locked. Defeat all enemies first. Remaining enemies: {remainingEnemies}", this);
            }

            return;
        }

        EndGame();
    }

    private bool AreAllEnemiesKilled()
    {
        if (GameStatsUI.Instance != null)
        {
            return GameStatsUI.Instance.AllEnemiesKilled;
        }

        return CountLivingEnemies() <= 0;
    }

    private int CountLivingEnemies()
    {
        int livingEnemies = 0;
        foreach (EnemyHealth enemy in FindObjectsOfType<EnemyHealth>())
        {
            if (enemy != null && enemy.CurrentHealth > 0f)
            {
                livingEnemies++;
            }
        }

        return livingEnemies;
    }

    private void EndGame()
    {
        hasEnded = true;

        if (winScreen != null)
        {
            winScreen.SetActive(true);
        }

        if (unlockCursor)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        if (pauseGame)
        {
            Time.timeScale = 0f;
        }

        if (loadSceneOnEnd)
        {
            if (sceneLoadDelay <= 0f)
            {
                LoadEndScene();
            }
            else
            {
                StartCoroutine(LoadEndSceneAfterDelay());
            }
        }
    }

    private IEnumerator LoadEndSceneAfterDelay()
    {
        yield return new WaitForSecondsRealtime(sceneLoadDelay);
        LoadEndScene();
    }

    private void LoadEndScene()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(sceneBuildIndex);
    }
}
