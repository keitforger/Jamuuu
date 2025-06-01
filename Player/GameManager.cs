using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Sistem penyimpanan data game yang disederhanakan
public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    public Text moneyText;
    private DataUI uiData;
    private string uiSaveName = "dataui";

    public DataGame gameData; // public supaya bisa diakses luar
    private string saveName = "datagame";
    private JamuSystem jamuSystem; // Reference to JamuSystem
    public TaskData taskData = new TaskData();

    [Header("Game Over UI")]
    public GameObject gameOverPanel;
    private CanvasGroup gameOverCanvasGroup;

    // [Header("Bootstrap Prefabs")]
    // public GameObject jamuSystemPrefab; // <-- DIHAPUS

    void Awake()
    {
        // Singleton pattern
        if (instance == null)
        {
            instance = this;
            // Hapus bootstrap jamuSystemPrefab!
            // if (JamuSystem.Instance == null)
            // {
            //     if (jamuSystemPrefab != null)
            //     {
            //         GameObject jamu = Instantiate(jamuSystemPrefab);
            //         DontDestroyOnLoad(jamu);
            //         Debug.Log("✅ JamuSystem di-bootstrap lewat GameManager.");
            //     }
            //     else
            //     {
            //         Debug.LogError("❌ jamuSystemPrefab belum diassign di GameManager!");
            //     }
            // }
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
            Destroy(gameObject);

        // Load data game yang tersimpan
        LoadGameData();

        // Initialize JamuSystem
        jamuSystem = FindAnyObjectByType<JamuSystem>();
        if (jamuSystem != null)
        {
            jamuSystem.LoadDatabase(); // Load the jamu database
        }
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "MainScene 1" || scene.name == "MainScene" || scene.name == "GameplayScene")
        {
            // Assign coin
            var coinObj = GameObject.FindGameObjectWithTag("koin");
            if (coinObj != null)
                moneyText = coinObj.GetComponent<Text>();
            else
                Debug.LogWarning("GameManager: Tidak menemukan GameObject 'coin' di scene!");

            // Assign GameOver panel
            var goPanel = GameObject.FindGameObjectWithTag("gameover");
            if (goPanel != null)
                gameOverPanel = goPanel;
            else
                Debug.LogWarning("GameManager: Tidak menemukan GameObject 'gameover' di scene!");

            if (gameOverPanel != null)
            {
                gameOverCanvasGroup = gameOverPanel.GetComponent<CanvasGroup>();
                if (gameOverCanvasGroup == null)
                    gameOverCanvasGroup = gameOverPanel.AddComponent<CanvasGroup>();

                // Hide panel visually and block interaction in the beginning
                gameOverCanvasGroup.alpha = 0f;
                gameOverCanvasGroup.interactable = false;
                gameOverCanvasGroup.blocksRaycasts = false;
            }

            UpdateMoneyDisplay();
        }
        else
        {
            moneyText = null;
            gameOverPanel = null;
        }
    }

    void Start()
    {
        // Update tampilan uang di awal
        UpdateMoneyDisplay();
    }

    void Update()
    {
        CheckGameOverCondition();
    }

    void CheckGameOverCondition()
    {
        bool isInventoryKosong = true;
        if (gameData.barang != null && gameData.barang.Count > 0)
        {
            foreach (var item in gameData.barang)
            {
                if (item != null && item.jumlah > 0)
                {
                    isInventoryKosong = false;
                    break;
                }
            }
        }

        if (isInventoryKosong && gameData.koin <= 0)
        {
            GameOver();
        }
    }

    void GameOver()
    {
        // Hanya panggil sekali
        if (gameOverPanel != null && (gameOverCanvasGroup == null || gameOverCanvasGroup.alpha == 0f))
        {
            // Show visually, enable interaction
            if (gameOverCanvasGroup == null)
                gameOverCanvasGroup = gameOverPanel.GetComponent<CanvasGroup>();

            if (gameOverCanvasGroup != null)
            {
                gameOverCanvasGroup.alpha = 1f;
                gameOverCanvasGroup.interactable = true;
                gameOverCanvasGroup.blocksRaycasts = true;
            }

            Debug.Log("GAME OVER: Inventory dan uang kosong.");
            var player = FindAnyObjectByType<PlayerMovement>();
            if (player != null)
                player.canMove = false;
        }
    }

    // Memuat data game dari PlayerPrefs
    void LoadGameData()
    {
        gameData = ManagerPP<DataGame>.Get(saveName);
        if (gameData == null) gameData = new DataGame(); // anti null

        uiData = ManagerPP<DataUI>.Get(uiSaveName);
        if (uiData == null) uiData = new DataUI();

        Debug.Log("Data loaded: " + gameData.koin + " koin, Level: " + gameData.playerLevel);
    }

    // Menyimpan data game ke PlayerPrefs
    public void SaveGameData()
    {
        ManagerPP<DataGame>.Set(saveName, gameData);
        Debug.Log("Data saved: " + gameData.koin + " koin, Level: " + gameData.playerLevel);
    }

    public void CreateNewGameData()
    {
        gameData = new DataGame(); // Buat data baru (otomatis nilai default)
        SaveGameData();
        Debug.Log("DataGame baru dibuat.");
    }

    public void SaveUIData(float currentTime, int queueCount)
    {
        uiData.currentTime = currentTime;
        uiData.queueCount = queueCount;
        ManagerPP<DataUI>.Set(uiSaveName, uiData);
    }

    public DataUI LoadUIData()
    {
        return uiData;
    }

    public void SaveInventory(List<Item> items)
    {
        gameData.barang = items;
        SaveGameData();
    }

    // Tambahkan metode untuk memuat inventory
    public List<Item> LoadInventory()
    {
        return gameData.barang;
    }

    // Menambahkan uang/koin ke dompet pemain
    public void AddMoney(int amount)
    {
        gameData.koin += amount;
        UpdateMoneyDisplay();
        SaveGameData();
    }

    // Mengurangi uang/koin dari dompet pemain
    public bool SpendMoney(int amount)
    {
        if (gameData.koin >= amount)
        {
            gameData.koin -= amount;
            UpdateMoneyDisplay();
            SaveGameData();
            return true;
        }
        return false;
    }

    // Mendapatkan jumlah uang/koin saat ini
    public int GetMoney()
    {
        return gameData.koin;
    }

    // Update tampilan uang di UI
    void UpdateMoneyDisplay()
    {
        if (moneyText != null)
            moneyText.text = gameData.koin.ToString();
    }

    // Mengelola barang/item dalam inventory
    public Item GetItem(int index)
    {
        if (index >= 0 && index < gameData.barang.Count)
            return gameData.barang[index];
        return null;
    }

    public void SetItem(int index, Item item)
    {
        if (index >= 0 && index < gameData.barang.Count)
        {
            gameData.barang[index] = item;
            SaveGameData();
        }
    }

    // Dipanggil ketika aplikasi ditutup
    void OnApplicationQuit()
    {
        SaveGameData();
        if (jamuSystem != null)
        {
            jamuSystem.SaveDatabase(); // Save the jamu database
        }
    }

    public bool HasWatchedCutscene()
    {
        return gameData.hasWatchedCutscene;
    }

    // Menetapkan status cutscene sebagai sudah ditonton
    public void SetCutsceneWatched()
    {
        gameData.hasWatchedCutscene = true;
        SaveGameData();
        Debug.Log("Cutscene status ditandai sebagai sudah ditonton");
    }

    // Untuk testing - reset status cutscene
    public void ResetCutsceneStatus()
    {
        gameData.hasWatchedCutscene = false;
        SaveGameData();
        Debug.Log("Cutscene status di-reset");
    }

    // === MODIFIKASI: getter dan setter TaskData agar TaskManager bisa akses ===
    public TaskData GetTaskData()
    {
        if (gameData.taskData == null)
            gameData.taskData = new TaskData();
        return gameData.taskData;
    }

    public void SetTaskData(TaskData data)
    {
        gameData.taskData = data;
        SaveGameData();
    }

    // Methods untuk Level System
    public void OnJamuCraftedAndAddedToAlmanac(string jamuName, bool isNewDiscovery = false)
    {
        // Sudah tidak perlu panggil LevelManager di sini
    }
}

// Helper class untuk menyimpan dan memuat data (dari ManagerPP.cs)
public static class ManagerPP<T>
{
    public static void Set(string namaPP, T dtg)
    {
        string json = JsonUtility.ToJson(dtg);
        PlayerPrefs.SetString(namaPP, json);
        PlayerPrefs.Save();
    }

    public static T Get(string namaPP)
    {
        string json = PlayerPrefs.GetString(namaPP, "{}");
        T dtg = JsonUtility.FromJson<T>(json);
        return dtg;
    }
}

// Data class untuk menyimpan informasi game (dari DataGame.cs)
[System.Serializable]
public class DataGame
{
    public int koin = 500;
    public List<Item> barang = new List<Item>();
    public List<string> discoveredAlmanacItems = new List<string>();
    public bool hasWatchedCutscene = false;  // Status apakah cutscene sudah pernah ditonton

    // Level System Data
    public int playerLevel = 1;
    public int playerExp = 0;

    // Task/tugas
    public TaskData taskData = new TaskData();
}

[System.Serializable]
public class DataUI
{
    public float currentTime = 8f; // misalnya jam 8 pagi default
    public int queueCount = 0;
}