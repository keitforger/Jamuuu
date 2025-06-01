using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance;

    [Header("Level UI")]
    public TMP_Text levelText;
    public TMP_Text expText;
    public Slider expSlider;

    [Header("Level Up Notification")]
    public GameObject levelUpPanel;
    public TMP_Text levelUpText;
    public Button levelUpCloseButton;
    public AudioSource levelUpSound;

    [Header("Level Settings")]
    public int baseExpRequired = 100;
    public float expMultiplier = 1.5f;

    private int currentLevel = 1;
    private int currentExp = 0;
    private int expRequiredForNextLevel;
    public GameObject bajajObject;

    // Tambahkan referensi gerobak unlock controller
    public GerobakUnlockController gerobakUnlockController;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (levelUpCloseButton != null)
            levelUpCloseButton.onClick.AddListener(CloseLevelUpPanel);

        if (levelUpPanel != null)
            levelUpPanel.SetActive(false);
    }

    void Start()
    {
        LoadLevelData();
        UpdateUI();
        TryUnlockGerobak();
        UpdateBajajStatus();
    }

    public void UpdateBajajStatus()
    {
        if (bajajObject != null)
        {
            // Bajaj hanya aktif jika sudah masuk level 4 (level 3 selesai, level 4 belum selesai)
            bool canAccessBajaj = false;
            if (TaskManager.Instance != null)
                canAccessBajaj = TaskManager.Instance.IsLevel4();

            bajajObject.SetActive(canAccessBajaj);
        }
    }

    void LoadLevelData()
    {
        if (GameManager.instance != null && GameManager.instance.gameData != null)
        {
            currentLevel = GameManager.instance.gameData.playerLevel;
            currentExp = GameManager.instance.gameData.playerExp;
        }
        else
        {
            currentLevel = PlayerPrefs.GetInt("PlayerLevel", 1);
            currentExp = PlayerPrefs.GetInt("PlayerExp", 0);
        }
        CalculateExpRequiredForNextLevel();
    }

    void SaveLevelData()
    {
        if (GameManager.instance != null && GameManager.instance.gameData != null)
        {
            GameManager.instance.gameData.playerLevel = currentLevel;
            GameManager.instance.gameData.playerExp = currentExp;
            GameManager.instance.SaveGameData();
        }
        else
        {
            PlayerPrefs.SetInt("PlayerLevel", currentLevel);
            PlayerPrefs.SetInt("PlayerExp", currentExp);
            PlayerPrefs.Save();
        }
    }

    void CalculateExpRequiredForNextLevel()
    {
        expRequiredForNextLevel = Mathf.RoundToInt(baseExpRequired * Mathf.Pow(expMultiplier, currentLevel - 1));
    }

    public void AddExperience(int amount, string reason = "")
    {
        currentExp += amount;
        Debug.Log($"Gained {amount} EXP" + (string.IsNullOrEmpty(reason) ? "" : $" for {reason}"));
        CheckLevelUp();
        UpdateUI();
        SaveLevelData();
    }

    void CheckLevelUp()
    {
        if (currentLevel == 1)
        {
            if (TaskManager.Instance != null &&
                TaskManager.Instance.IsLevel1Complete() &&
                currentExp >= expRequiredForNextLevel)
            {
                LevelUp();
            }
        }
        else
        {
            while (currentExp >= expRequiredForNextLevel)
                LevelUp();
        }
    }

    void LevelUp()
    {
        currentExp -= expRequiredForNextLevel;
        currentLevel++;
        CalculateExpRequiredForNextLevel();
        ShowLevelUpNotification();
        GiveLevelUpRewards();
        UpdateUI();
        SaveLevelData();

        // Cek status gerobak setiap naik level
        TryUnlockGerobak();

        // Update status nenek (TaskManager)
        if (TaskManager.Instance != null)
            TaskManager.Instance.UpdateNenekStatus();

        UpdateBajajStatus();
    }

    public void TryUnlockGerobak()
    {
        if (gerobakUnlockController != null)
        {
            // Unlock hanya jika sudah level 3 atau lebih
            bool isLocked = currentLevel < 3;
            gerobakUnlockController.SetGerobakState(isLocked);
        }
    }

    void ShowLevelUpNotification()
    {
        if (levelUpPanel != null)
        {
            levelUpPanel.SetActive(true);
            if (levelUpText != null)
                levelUpText.text = $"Level Up!\n{currentLevel - 1} → {currentLevel}";
            if (levelUpSound != null)
                levelUpSound.Play();
            StartCoroutine(AutoCloseLevelUpPanel());
        }
    }

    System.Collections.IEnumerator AutoCloseLevelUpPanel()
    {
        yield return new WaitForSeconds(3f);
        if (levelUpPanel != null && levelUpPanel.activeSelf)
        {
            CloseLevelUpPanel();
        }
    }

    void CloseLevelUpPanel()
    {
        if (levelUpPanel != null)
            levelUpPanel.SetActive(false);
    }

    void GiveLevelUpRewards()
    {
        int coinReward = currentLevel * 50;
        if (GameManager.instance != null)
        {
            GameManager.instance.AddMoney(coinReward);
            Debug.Log($"Level up reward: {coinReward} coins!");
        }
    }

    void UpdateUI()
    {
        if (levelText != null)
            levelText.text = $"{currentLevel}";

        if (expText != null)
            expText.text = $"{currentExp}/{expRequiredForNextLevel} EXP";

        if (expSlider != null)
        {
            expSlider.maxValue = expRequiredForNextLevel;
            expSlider.value = currentExp;
        }
    }

    [ContextMenu("Add 50 EXP")]
    public void AddTestExp() => AddExperience(50, "testing");

    [ContextMenu("Reset Level")]
    public void ResetLevel()
    {
        currentLevel = 1;
        currentExp = 0;
        CalculateExpRequiredForNextLevel();
        UpdateUI();
        SaveLevelData();
        Debug.Log("Level reset to 1");
    }

    [ContextMenu("Force Level Check")]
    public void ForceTaskLevelCheck() => CheckLevelUp();

    public int GetCurrentLevel() => currentLevel;
    public int GetCurrentExp() => currentExp;
    public int GetExpRequiredForNextLevel() => expRequiredForNextLevel;
    public float GetExpProgress() => (float)currentExp / expRequiredForNextLevel;
    public bool CanAccessLevel(int requiredLevel) => currentLevel >= requiredLevel;
}