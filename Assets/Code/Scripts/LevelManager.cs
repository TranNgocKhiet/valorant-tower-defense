using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LevelManager : MonoBehaviour
{
    public static LevelManager main;

    [Header("UI References")]
    public TextMeshProUGUI endText;
    public Button nextLevelButton;
    public GameObject rewardDisplayObject;
    public TextMeshProUGUI amountText;
    public TextMeshProUGUI waveText;

    [Header("Multiple Paths Setup")]
    public EnemyPath[] enemyPaths;

    [Header("Attributes")]
    public int totalWaves = 5;

    [Header("Menu panel")]
    public GameObject endGamePanel;
    public GameObject pauseMenuPanel;

    [Header("Level Rewards")]
    public int radianiteReward = 100;

    [Header("UI Popups")]
    public TextMeshProUGUI rewardPopupText;
    [Tooltip("Starting currency for level 1. Carried-over currency is used from level 2 onwards.")]
    [SerializeField] private int defaultCurrency = 300;

    public int currency;
    public int levelHealth;
    private int currentWave = 0;
    private bool isGameOver = false;

    private void Awake()
    {
        if (main == null)
        {
            main = this;
        }
    }

    private void Start()
    {
        Time.timeScale = 1f;

        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
        if (endGamePanel != null) endGamePanel.SetActive(false);

        currency = PlayerPrefs.HasKey("LevelCurrency")
            ? PlayerPrefs.GetInt("LevelCurrency")
            : defaultCurrency;
        endGamePanel.SetActive(false);
    }

    public EnemyPath GetPath(int waveNumber)
    {
        currentWave = waveNumber;
        UpdateWaveUI();

        if (currentWave > totalWaves)
        {
            WinLevel();
            return null;
        }

        return enemyPaths[Random.Range(0, enemyPaths.Length)];
    }

    private void UpdateWaveUI()
    {
        if (waveText != null)
        {
            waveText.text = currentWave + " / " + totalWaves;
        }
    }

    public void DecreaseHealth(int amount)
    {
        if (isGameOver) return;

        levelHealth -= amount;
        if (levelHealth <= 0)
        {
            LoseLevel();
        }
    }

    public void WinLevel()
    {
        isGameOver = true;
        Time.timeScale = 0f;
        endGamePanel.SetActive(true);
        endText.text = "VICTORY";
        endText.color = Color.cyan;
        nextLevelButton.interactable = true;
        
        PlayerPrefs.SetInt("LevelCurrency", currency);
        PlayerPrefs.Save();
        
        GiveLevelReward();
    }

    private void GiveLevelReward()
    {
        // 1. Calculate and update local Radianite
        int currentBalance = PlayerPrefs.GetInt("TotalRadianite", 0);
        currentBalance += radianiteReward;
        PlayerPrefs.SetInt("TotalRadianite", currentBalance);
        PlayerPrefs.Save();

        // 2. Display the Reward in the UI
        if (rewardDisplayObject != null)
        {
            amountText.text = "+" + radianiteReward + " RADIANITE";
            rewardDisplayObject.SetActive(true);
        }

        // 3. Sync everything to AWS DynamoDB
        // We pass the new balance and the next level index
        int nextLevelIndex = SceneManager.GetActiveScene().buildIndex + 1;

        CloudDataManager cloudManager = FindObjectOfType<CloudDataManager>();
        if (cloudManager != null)
        {
            cloudManager.SaveGameProgress(currentBalance, nextLevelIndex);
        }
        else
        {
            Debug.LogWarning("CloudDataManager not found! Progress not synced to AWS.");
        }
    }

    void LoseLevel()
    {
        isGameOver = true;
        Time.timeScale = 0f;
        endGamePanel.SetActive(true);
        endText.text = "DEFEAT";
        endText.color = Color.red;
        nextLevelButton.interactable = false;
    }

    public void IncreaseCurrency(int amount)
    {
        currency += amount;
    }

    public bool SpendCurrency(int amount)
    {
        if (amount <= currency)
        {
            currency -= amount;
            return true;
        }
        else
        {
            Debug.Log("Not enough currency!");
            return false;
        }

    }

    public void Retry()
    {
        PlayerPrefs.DeleteKey("LevelCurrency");
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void GoToLevelSelect()
    {
        PlayerPrefs.DeleteKey("LevelCurrency");
        SceneManager.LoadScene("LevelChooseScene");
    }
    public void NextLevel() => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);

}

[System.Serializable]
public class EnemyPath
{
    public Transform startPoint;
    public Transform[] waypoints;
}
