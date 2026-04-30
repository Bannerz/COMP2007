using TMPro;
using UnityEngine;

public class GameStatsUI : MonoBehaviour
{
    public static GameStatsUI Instance { get; private set; }

    [Header("Text")]
    [SerializeField] private TMP_Text goldLabel;
    [SerializeField] private TMP_Text goldCount;
    [SerializeField] private TMP_Text enemiesLabel;
    [SerializeField] private TMP_Text enemyCount;

    [Header("Labels")]
    [SerializeField] private string goldLabelText = "Gold";
    [SerializeField] private string enemiesLabelText = "Enemies";

    private int currentGold;
    private int remainingEnemies;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        remainingEnemies = FindObjectsOfType<EnemyHealth>().Length;
        RefreshUI();
    }

    public void AddGold(int amount)
    {
        if (amount <= 0)
            return;

        currentGold += amount;
        RefreshUI();
    }

    public void EnemyKilled()
    {
        remainingEnemies = Mathf.Max(0, remainingEnemies - 1);
        RefreshUI();
    }

    private void RefreshUI()
    {
        if (goldLabel != null)
            goldLabel.text = goldLabelText;

        if (goldCount != null)
            goldCount.text = currentGold.ToString();

        if (enemiesLabel != null)
            enemiesLabel.text = enemiesLabelText;

        if (enemyCount != null)
            enemyCount.text = remainingEnemies.ToString();
    }
}
