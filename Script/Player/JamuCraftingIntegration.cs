using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Unified integration system that handles Jamu crafting, inventory management, 
/// level progression, and almanac entries
/// </summary>
public class JamuCraftingIntegration : MonoBehaviour
{
    public static JamuCraftingIntegration Instance { get; private set; }

    [Header("System References")]
    [SerializeField] public JamuSystem jamuSystem;
    [SerializeField] public Inventory inventory;
    [SerializeField] public AlmanacSystem almanacSystem;

    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = true;

    // Cache for converted items
    private Dictionary<string, Item> convertedBahanItems = new Dictionary<string, Item>();
    private Dictionary<string, Item> convertedJamuItems = new Dictionary<string, Item>();

    // Tracking collections
    private HashSet<string> craftedJamuHistory = new HashSet<string>();
    private HashSet<string> discoveredAlmanacEntries = new HashSet<string>();

    // Panel registration
    private List<ICraftingPanel> registeredPanels = new List<ICraftingPanel>();

    #region Singleton & Initialization

    void Awake()
    {
        // Singleton setup
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

        FindSystemReferences();
    }

    void Start()
    {
        LoadCraftingHistory();
        CacheItemsIfPossible();

        if (enableDebugLogs)
        {
            Debug.Log($"JamuCraftingIntegration initialized:");
            Debug.Log($"- JamuSystem: {(jamuSystem != null ? "Found" : "Missing")}");
            Debug.Log($"- Inventory: {(inventory != null ? "Found" : "Missing")}");
            Debug.Log($"- AlmanacSystem: {(almanacSystem != null ? "Found" : "Missing")}");
        }
    }

    void LateUpdate()
    {
        // Auto-assign Inventory jika null
        if (inventory == null)
        {
            inventory = FindAnyObjectByType<Inventory>();
            if (enableDebugLogs && inventory != null)
                Debug.Log("Auto-assigned Inventory in JamuCraftingIntegration.");
        }
        // Auto-assign AlmanacSystem jika null
        if (almanacSystem == null)
        {
            almanacSystem = FindAnyObjectByType<AlmanacSystem>();
            if (enableDebugLogs && almanacSystem != null)
                Debug.Log("Auto-assigned AlmanacSystem in JamuCraftingIntegration.");
        }
    }

    void OnEnable()
    {
        Inventory.OnInventoryEnabled += OnInventoryEnabled;
        AlmanacSystem.OnAlmanacEnabled += OnAlmanacEnabled;
    }
    void OnDisable()
    {
        Inventory.OnInventoryEnabled -= OnInventoryEnabled;
        AlmanacSystem.OnAlmanacEnabled -= OnAlmanacEnabled;
    }

    private void OnInventoryEnabled(Inventory inv)
    {
        inventory = inv;
        if (enableDebugLogs) Debug.Log("Inventory auto-assigned via event!");
    }
    private void OnAlmanacEnabled(AlmanacSystem alm)
    {
        almanacSystem = alm;
        if (enableDebugLogs) Debug.Log("AlmanacSystem auto-assigned via event!");
    }

    private void FindSystemReferences()
    {
        if (jamuSystem == null)
            jamuSystem = FindAnyObjectByType<JamuSystem>();

        if (inventory == null)
            inventory = FindAnyObjectByType<Inventory>();

        if (almanacSystem == null)
            almanacSystem = FindAnyObjectByType<AlmanacSystem>();
    }

    #endregion

    #region Panel Registration

    public void RegisterCraftingPanel(ICraftingPanel panel)
    {
        if (panel != null && !registeredPanels.Contains(panel))
        {
            registeredPanels.Add(panel);
            if (enableDebugLogs)
                Debug.Log($"Crafting panel registered: {panel}");
        }
    }

    public void UnregisterCraftingPanel(ICraftingPanel panel)
    {
        if (panel != null && registeredPanels.Contains(panel))
        {
            registeredPanels.Remove(panel);
            if (enableDebugLogs)
                Debug.Log($"Crafting panel unregistered: {panel}");
        }
    }

    #endregion

    #region Data Persistence

    private void LoadCraftingHistory()
    {
        if (GameManager.instance?.gameData != null)
        {
            // Load discovered almanac items
            foreach (string item in GameManager.instance.gameData.discoveredAlmanacItems)
            {
                discoveredAlmanacEntries.Add(item);
            }

            if (enableDebugLogs)
                Debug.Log($"Loaded {discoveredAlmanacEntries.Count} almanac entries from save data");
        }
    }

    private void SaveCraftingHistory()
    {
        if (GameManager.instance?.gameData != null)
        {
            GameManager.instance.gameData.discoveredAlmanacItems.Clear();
            GameManager.instance.gameData.discoveredAlmanacItems.AddRange(discoveredAlmanacEntries);
            GameManager.instance.SaveGameData();

            if (enableDebugLogs)
                Debug.Log($"Saved {discoveredAlmanacEntries.Count} almanac entries to save data");
        }
    }

    #endregion

    #region Item Caching & Conversion

    private void CacheItemsIfPossible()
    {
        if (jamuSystem?.jamuDatabase != null)
        {
            CacheJamuItems();
        }
    }

    /// <summary>
    /// Cache all jamu items for quicker conversion
    /// </summary>
    private void CacheJamuItems()
    {
        // Cache bahan items
        foreach (var bahan in jamuSystem.jamuDatabase.bahans)
        {
            Item item = new Item
            {
                nama = bahan.itemName,
                gambar = bahan.itemSprite,
                harga = bahan.itemValue,
                jumlah = 0
            };
            convertedBahanItems[bahan.itemName] = item;
        }

        // Cache jamu recipes
        foreach (var jamu in jamuSystem.jamuDatabase.resepJamus)
        {
            Item item = new Item
            {
                nama = jamu.jamuName,
                gambar = jamu.jamuSprite,
                harga = jamu.jamuValue,
                jumlah = 0
            };
            convertedJamuItems[jamu.jamuName] = item;
        }

        if (enableDebugLogs)
            Debug.Log($"Cached {convertedBahanItems.Count} bahan items and {convertedJamuItems.Count} jamu items");
    }

    /// <summary>
    /// Convert a Bahan Item from JamuSystem to the game's Item system
    /// </summary>
    public Item ConvertBahanToItem(BahanItem bahan)
    {
        if (bahan == null) return null;

        if (convertedBahanItems.TryGetValue(bahan.itemName, out Item cachedItem))
        {
            return new Item
            {
                nama = cachedItem.nama,
                gambar = cachedItem.gambar,
                harga = cachedItem.harga,
                jumlah = 1
            };
        }

        // Create new item if not cached
        Item item = new Item
        {
            nama = bahan.itemName,
            gambar = bahan.itemSprite,
            harga = bahan.itemValue,
            jumlah = 1
        };

        convertedBahanItems[bahan.itemName] = item;
        return item;
    }

    /// <summary>
    /// Convert a Jamu item to the game's Item system
    /// </summary>
    public Item ConvertJamuToItem(ResepJamu jamu)
    {
        if (jamu == null) return null;

        if (convertedJamuItems.TryGetValue(jamu.jamuName, out Item cachedItem))
        {
            return new Item
            {
                nama = cachedItem.nama,
                gambar = cachedItem.gambar,
                harga = cachedItem.harga,
                jumlah = 1
            };
        }

        // Create new item if not cached
        Item item = new Item
        {
            nama = jamu.jamuName,
            gambar = jamu.jamuSprite,
            harga = jamu.jamuValue,
            jumlah = 1
        };

        convertedJamuItems[jamu.jamuName] = item;
        return item;
    }

    #endregion

    #region Inventory Management

    /// <summary>
    /// Get a list of bahan names that match the current inventory items
    /// </summary>
    public List<string> GetAvailableBahanNames()
    {
        if (jamuSystem?.jamuDatabase == null)
            return new List<string>();

        List<string> result = new List<string>();
        var dtg = ManagerPP<DataGame>.Get("datagame");

        if (dtg == null) return result;

        foreach (Item item in dtg.barang)
        {
            if (item?.gambar != null && item.jumlah > 0)
            {
                BahanItem bahan = jamuSystem.jamuDatabase.GetBahan(item.nama);
                if (bahan != null)
                {
                    result.Add(item.nama);
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Add a jamu to the player's inventory
    /// </summary>
    public bool AddJamuToInventory(ResepJamu jamu)
    {
        if (jamu == null || inventory == null) return false;

        BahanItem bahanProduced = jamuSystem.GetBahan(jamu.jamuName);
        if (bahanProduced != null)
        {
            inventory.TambahItemHasilPanen(bahanProduced);

            if (enableDebugLogs)
                Debug.Log($"Added {jamu.jamuName} to inventory");

            return true;
        }

        return false;
    }

    #endregion

    #region Crafting Logic

    /// <summary>
    /// Check if a jamu can be crafted with the given ingredients
    /// </summary>
    public bool CanCraftJamu(List<string> ingredientNames)
    {
        if (jamuSystem?.jamuDatabase == null || ingredientNames == null)
            return false;

        foreach (ResepJamu recipe in jamuSystem.jamuDatabase.resepJamus)
        {
            if (recipe.bahanResep.Length != ingredientNames.Count)
                continue;

            bool allIngredientsMatch = true;
            foreach (string recipeIngredient in recipe.bahanResep)
            {
                if (!ingredientNames.Contains(recipeIngredient))
                {
                    allIngredientsMatch = false;
                    break;
                }
            }

            if (allIngredientsMatch)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Try to craft a jamu with the given ingredients
    /// </summary>
    public ResepJamu TryCraftJamu(List<string> ingredientNames)
    {
        if (jamuSystem?.jamuDatabase == null || ingredientNames == null)
            return null;

        foreach (ResepJamu recipe in jamuSystem.jamuDatabase.resepJamus)
        {
            if (recipe.bahanResep.Length != ingredientNames.Count)
                continue;

            // Sort both lists for comparison
            List<string> sortedRecipeIngredients = recipe.bahanResep.ToList();
            sortedRecipeIngredients.Sort();

            List<string> sortedIngredients = new List<string>(ingredientNames);
            sortedIngredients.Sort();

            // Check if ingredient lists match
            bool allIngredientsMatch = true;
            for (int i = 0; i < sortedRecipeIngredients.Count; i++)
            {
                if (!sortedIngredients[i].Equals(sortedRecipeIngredients[i]))
                {
                    allIngredientsMatch = false;
                    break;
                }
            }

            if (allIngredientsMatch)
                return recipe;
        }

        return null;
    }

    #endregion

    #region Level & Progress Integration

    /// <summary>
    /// Call this method after successfully crafting jamu
    /// </summary>
    /// <param name="jamuName">Name of the crafted jamu</param>
    /// <param name="jamuData">Optional jamu data</param>
    public void OnJamuCrafted(string jamuName, object jamuData = null)
    {
        bool isNewDiscovery = !craftedJamuHistory.Contains(jamuName);

        // Add to history
        craftedJamuHistory.Add(jamuName);

        // Set tutorial flag
        CraftingManagerHelper.TutorialCraftingBerhasil = true;

        if (enableDebugLogs)
            Debug.Log($"Jamu crafted: {jamuName} (New discovery: {isNewDiscovery})");

        // EXP for crafting handled by LevelManager in OnJamuCrafted, but if your leveling is now only task-based, you may remove this
        // if (LevelManager.Instance != null)
        // {
        //     LevelManager.Instance.OnJamuCrafted(jamuName, isNewDiscovery);
        // }

        // Trigger GameManager method if exists (this only handles EXP if not using LevelManager for EXP)
        if (GameManager.instance != null)
        {
            GameManager.instance.OnJamuCraftedAndAddedToAlmanac(jamuName, isNewDiscovery);
        }
    }

    /// <summary>
    /// Call this method when jamu is added to almanac (NO LONGER NEEDED for EXP/level)
    /// </summary>
    /// <param name="jamuName">Name of jamu added to almanac</param>
    public void OnJamuAddedToAlmanac(string jamuName)
    {
        bool isNewEntry = !discoveredAlmanacEntries.Contains(jamuName);

        if (isNewEntry)
        {
            discoveredAlmanacEntries.Add(jamuName);
            SaveCraftingHistory();

            if (enableDebugLogs)
                Debug.Log($"New almanac entry: {jamuName}");

            // No longer call LevelManager.Instance.OnAlmanacEntryAdded
            // No longer call GameManager.instance.OnAlmanacEntryAdded
        }
    }

    /// <summary>
    /// Call this method when crafting AND adding to almanac happens simultaneously
    /// </summary>
    /// <param name="jamuName">Name of jamu</param>
    public void OnJamuCraftedAndAddedToAlmanac(string jamuName)
    {
        bool isNewAlmanacEntry = !discoveredAlmanacEntries.Contains(jamuName);

        // Process crafting
        OnJamuCrafted(jamuName);

        // Process almanac addition (if it's actually new)
        if (isNewAlmanacEntry)
        {
            OnJamuAddedToAlmanac(jamuName);
        }
    }

    /// <summary>
    /// Complete crafting process: craft jamu, add to inventory, and handle progression
    /// </summary>
    /// <param name="ingredientNames">List of ingredient names used</param>
    /// <returns>The crafted jamu recipe, or null if crafting failed</returns>
    public ResepJamu CompleteCraftingProcess(List<string> ingredientNames)
    {
        ResepJamu craftedJamu = TryCraftJamu(ingredientNames);

        if (craftedJamu != null)
        {
            // Add to inventory
            bool addedToInventory = AddJamuToInventory(craftedJamu);

            if (addedToInventory)
            {
                // Handle progression and almanac
                OnJamuCraftedAndAddedToAlmanac(craftedJamu.jamuName);

                if (enableDebugLogs)
                    Debug.Log($"Successfully completed crafting process for: {craftedJamu.jamuName}");
            }
            else
            {
                if (enableDebugLogs)
                    Debug.LogWarning($"Failed to add {craftedJamu.jamuName} to inventory");
            }
        }
        else
        {
            if (enableDebugLogs)
                Debug.Log($"No matching recipe found for ingredients: {string.Join(", ", ingredientNames)}");
        }

        return craftedJamu;
    }

    /// <summary>
    /// Method for giving bonus EXP for special tasks
    /// </summary>
    /// <param name="taskName">Name of task</param>
    /// <param name="expAmount">Amount of EXP</param>
    public void OnSpecialTaskCompleted(string taskName, int expAmount)
    {
        // No longer call LevelManager.Instance.OnTaskCompleted
        // Use your task system for progression

        if (enableDebugLogs)
            Debug.Log($"Special task completed: {taskName} (+{expAmount} EXP)");
    }

    #endregion

    #region Query Methods

    public bool HasCraftedJamu(string jamuName)
    {
        return craftedJamuHistory.Contains(jamuName);
    }

    public bool HasDiscoveredAlmanacEntry(string entryName)
    {
        return discoveredAlmanacEntries.Contains(entryName);
    }

    public int GetTotalJamuCrafted()
    {
        return craftedJamuHistory.Count;
    }

    public int GetTotalAlmanacEntries()
    {
        return discoveredAlmanacEntries.Count;
    }

    public List<string> GetCraftedJamuList()
    {
        return new List<string>(craftedJamuHistory);
    }

    public List<string> GetDiscoveredAlmanacEntries()
    {
        return new List<string>(discoveredAlmanacEntries);
    }

    #endregion

    #region Debug & Testing

    [ContextMenu("Test Crafting")]
    public void TestCrafting()
    {
        OnJamuCraftedAndAddedToAlmanac("Test Jamu");
    }

    [ContextMenu("Reset Crafting History")]
    public void ResetCraftingHistory()
    {
        craftedJamuHistory.Clear();
        discoveredAlmanacEntries.Clear();
        SaveCraftingHistory();

        if (enableDebugLogs)
            Debug.Log("Crafting history reset");
    }

    [ContextMenu("Log Current Status")]
    public void LogCurrentStatus()
    {
        Debug.Log($"=== Jamu Crafting Integration Status ===");
        Debug.Log($"Total Jamu Crafted: {GetTotalJamuCrafted()}");
        Debug.Log($"Total Almanac Entries: {GetTotalAlmanacEntries()}");
        Debug.Log($"Available Bahan: {GetAvailableBahanNames().Count}");
        Debug.Log($"Registered Panels: {registeredPanels.Count}");
        Debug.Log($"Systems Connected:");
        Debug.Log($"  - JamuSystem: {jamuSystem != null}");
        Debug.Log($"  - Inventory: {inventory != null}");
        Debug.Log($"  - AlmanacSystem: {almanacSystem != null}");
        Debug.Log($"  - LevelManager: {LevelManager.Instance != null}");
        Debug.Log($"  - GameManager: {GameManager.instance != null}");
    }

    #endregion
}

#if UNITY_EDITOR
/// <summary>
/// Editor utility for setting up the integration system
/// </summary>
public class JamuCraftingIntegrationSetup
{
    [UnityEditor.MenuItem("Tools/Jamu System/Setup Jamu Crafting Integration")]
    static void SetupJamuCraftingIntegration()
    {
        // Check if instance already exists
        JamuCraftingIntegration existing = Object.FindAnyObjectByType<JamuCraftingIntegration>();
        if (existing != null)
        {
            UnityEditor.Selection.activeGameObject = existing.gameObject;
            Debug.Log("JamuCraftingIntegration already exists");
            return;
        }

        // Create new integration object
        GameObject integrationObj = new GameObject("JamuCraftingIntegration");
        JamuCraftingIntegration integration = integrationObj.AddComponent<JamuCraftingIntegration>();

        // Find references to existing systems
        integration.jamuSystem = Object.FindAnyObjectByType<JamuSystem>();
        integration.inventory = Object.FindAnyObjectByType<Inventory>();
        integration.almanacSystem = Object.FindAnyObjectByType<AlmanacSystem>();

        UnityEditor.Selection.activeGameObject = integrationObj;
        Debug.Log("JamuCraftingIntegration created and configured");
    }
}
#endif