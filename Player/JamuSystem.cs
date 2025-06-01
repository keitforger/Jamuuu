using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

/// <summary>
/// Comprehensive jamu system that manages bahans, benihs, and jamu recipes
/// </summary>
public class JamuSystem : MonoBehaviour
{
    public static JamuSystem Instance { get; private set; }

    // Reference to the scriptable object database
    public JamuDatabase jamuDatabase;

    // Reference to the AlmanacSystem for adding discovered items
    private AlmanacSystem almanacSystem;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("✅ JamuSystem aktif: " + gameObject.name);

            // FORCE LOAD dari Resources meskipun sudah di Inspector
            if (jamuDatabase == null)
            {
                jamuDatabase = Resources.Load<JamuDatabase>("eJamuuu/JamuDatabase");
            }
        }
        else if (Instance != this)
        {
            Debug.LogWarning("⚠️ Duplikat JamuSystem ditemukan, menghancurkan: " + gameObject.name);
            Destroy(gameObject);
        }
    }

    public void LoadDatabase()
    {
        // Load the database from PlayerPrefs or directly from asset
        if (jamuDatabase == null)
        {
            // Attempt to load from resources or a specific path
            jamuDatabase = Resources.Load<JamuDatabase>("eJamuuu/JamuDatabase");
            if (jamuDatabase == null)
            {
                Debug.LogError("Failed to load JamuDatabase.");
            }
        }
    }

    public void SaveDatabase()
    {
        // Save the database to PlayerPrefs
        string json = JsonUtility.ToJson(jamuDatabase);
        PlayerPrefs.SetString("JamuDatabase", json);
        PlayerPrefs.Save();
        Debug.Log("Jamu Database saved.");
    }

    void Start()
    {
        // Hanya cari AlmanacSystem di scene yang membutuhkannya (misal MainScene1)
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "MainScene1")
        {
            almanacSystem = AlmanacSystem.Instance;
            if (almanacSystem == null)
            {
                Debug.LogWarning("AlmanacSystem not found in the scene. Jamus won't be added to almanac automatically.");
            }
        }
    }

    /// <summary>
    /// Get a bahan item by its name
    /// </summary>
    public BahanItem GetBahan(string bahanName)
    {
        return jamuDatabase.GetBahan(bahanName);
    }

    /// <summary>
    /// Get a benih item by its name
    /// </summary>
    public BenihItem GetBenih(string benihName)
    {
        return jamuDatabase.GetBenih(benihName);
    }

    /// <summary>
    /// Get a jamu recipe by its name
    /// </summary>
    public ResepJamu GetResepJamu(string jamuName)
    {
        return jamuDatabase.GetResepJamu(jamuName);
    }

    /// <summary>
    /// Check if the player has all ingredients for a jamu recipe
    /// </summary>
    public bool HasIngredientsForJamu(string jamuName)
    {
        ResepJamu recipe = GetResepJamu(jamuName);
        if (recipe == null) return false;

        // Check if player has all ingredients in the inventory
        // This requires integration with your inventory system
        GameManager gameManager = GameManager.instance;
        Dictionary<string, int> playerBahans = new Dictionary<string, int>();

        // Count all bahans in player inventory
        for (int i = 0; i < 15; i++)
        {
            Item item = gameManager.GetItem(i);
            if (item != null && item.jumlah > 0)
            {
                // Check if the item is a bahan
                BahanItem bahan = jamuDatabase.GetBahan(item.nama);
                if (bahan != null)
                {
                    if (playerBahans.ContainsKey(item.nama))
                        playerBahans[item.nama] += item.jumlah;
                    else
                        playerBahans[item.nama] = item.jumlah;
                }
            }
        }

        // Check if player has all needed ingredients
        foreach (string ingredient in recipe.bahanResep)
        {
            if (!playerBahans.ContainsKey(ingredient) || playerBahans[ingredient] < 1)
                return false;
        }

        return true;
    }

    /// <summary>
    /// Create a jamu from a recipe (consume ingredients and add jamu to inventory)
    /// </summary>
    public bool CreateJamu(string jamuName)
    {
        if (!HasIngredientsForJamu(jamuName)) return false;

        ResepJamu recipe = GetResepJamu(jamuName);
        GameManager gameManager = GameManager.instance;

        // Consume each ingredient
        foreach (string ingredient in recipe.bahanResep)
        {
            bool foundIngredient = false;

            // Find and remove one of each ingredient
            for (int i = 0; i < 15; i++)
            {
                Item item = gameManager.GetItem(i);
                if (item != null && item.nama == ingredient && item.jumlah > 0)
                {
                    item.jumlah--;
                    gameManager.SetItem(i, item);
                    foundIngredient = true;
                    break;
                }
            }

            if (!foundIngredient) return false; // Safeguard
        }

        // Create jamu item
        Item jamuItem = new Item
        {
            nama = recipe.jamuName,
            gambar = recipe.jamuSprite,
            harga = recipe.jamuValue,
            jumlah = 1
        };

        // Find empty slot or matching jamu to add to
        bool added = false;
        for (int i = 0; i < 15; i++)
        {
            Item slot = gameManager.GetItem(i);

            // If slot is empty or contains same jamu
            if (slot == null || (slot.nama == jamuName && slot.jumlah > 0))
            {
                if (slot == null)
                {
                    gameManager.SetItem(i, jamuItem);
                }
                else
                {
                    slot.jumlah++;
                    gameManager.SetItem(i, slot);
                }
                added = true;
                break;
            }
        }

        // If jamu was successfully created and added to inventory, 
        // also add it to the almanac
        if (added)
        {
            // Add to almanac if almanacSystem exists
            if (almanacSystem != null)
            {
                almanacSystem.DiscoverJamu(jamuName);
            }
            else
            {
                // Try to find almanacSystem again if it wasn't found earlier
                almanacSystem = AlmanacSystem.Instance;
                if (almanacSystem != null)
                {
                    almanacSystem.DiscoverJamu(jamuName);
                }
                else
                {
                    Debug.LogWarning($"AlmanacSystem not found, couldn't add {jamuName} to almanac");
                }
            }
            return true;
        }
        return added;
    }

    // Tambahkan method baru di JamuSystem class
    /// <summary>
    /// Cek apakah kombinasi bahan membentuk resep yang valid
    /// </summary>
    public bool IsValidRecipe(string[] bahanKombinasi)
    {
        if (jamuDatabase == null || jamuDatabase.resepJamus == null) return false;

        foreach (ResepJamu recipe in jamuDatabase.resepJamus)
        {
            if (recipe.bahanResep != null && bahanKombinasi != null &&
                recipe.bahanResep.Length == bahanKombinasi.Length &&
                recipe.bahanResep.All(bahan => bahanKombinasi.Contains(bahan)) &&
                bahanKombinasi.All(bahan => recipe.bahanResep.Contains(bahan)))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Buat jamu gagal dari kombinasi bahan yang tidak valid
    /// </summary>
    public bool CreateSingleJamuGagal(string[] bahanKombinasi)
    {
        // Cek apakah player punya semua bahan
        if (!HasAllBahan(bahanKombinasi)) return false;

        GameManager gameManager = GameManager.instance;

        // Konsumsi bahan-bahan
        foreach (string bahan in bahanKombinasi)
        {
            bool foundIngredient = false;
            for (int i = 0; i < 15; i++)
            {
                Item item = gameManager.GetItem(i);
                if (item != null && item.nama == bahan && item.jumlah > 0)
                {
                    item.jumlah--;
                    gameManager.SetItem(i, item);
                    foundIngredient = true;
                    break;
                }
            }
            if (!foundIngredient) return false;
        }

        // Get or create the single failed jamu
        const string FAILED_JAMU_NAME = "Jamu Gagal";
        JamuGagal failedJamu = jamuDatabase.jamuGagalList.Find(j => j.itemName == FAILED_JAMU_NAME);

        if (failedJamu == null)
        {
            // Create the single failed jamu if it doesn't exist
            failedJamu = new JamuGagal
            {
                itemName = FAILED_JAMU_NAME,
                itemSprite = jamuDatabase.defaultFailedJamuSprite,
                itemValue = 5,
                description = "Jamu yang gagal dibuat karena kombinasi bahan yang tidak tepat. Meskipun tidak berhasil, tetap bisa dipelajari untuk referensi di masa depan.",
                bahanPenyusun = new string[0], // Empty since this represents all failed combinations
                tanggalDibuat = DateTime.Now
            };
            jamuDatabase.jamuGagalList.Add(failedJamu);
        }

        // Buat item jamu gagal
        Item jamuGagalItem = new Item
        {
            nama = failedJamu.itemName,
            gambar = failedJamu.itemSprite,
            harga = failedJamu.itemValue,
            jumlah = 1
        };

        // Tambahkan ke inventory
        bool added = false;
        for (int i = 0; i < 15; i++)
        {
            Item slot = gameManager.GetItem(i);
            if (slot == null || (slot.nama == failedJamu.itemName && slot.jumlah > 0))
            {
                if (slot == null)
                {
                    gameManager.SetItem(i, jamuGagalItem);
                }
                else
                {
                    slot.jumlah++;
                    gameManager.SetItem(i, slot);
                }
                added = true;
                break;
            }
        }

        // Tambahkan ke almanac jika berhasil
        if (added && almanacSystem != null)
        {
            almanacSystem.DiscoverJamuGagal(failedJamu.itemName);
        }

        return added;
    }

    /// <summary>
    /// Cek apakah player memiliki semua bahan dalam kombinasi
    /// </summary>
    private bool HasAllBahan(string[] bahanKombinasi)
    {
        GameManager gameManager = GameManager.instance;
        Dictionary<string, int> playerBahans = new Dictionary<string, int>();

        // Hitung semua bahan di inventory
        for (int i = 0; i < 15; i++)
        {
            Item item = gameManager.GetItem(i);
            if (item != null && item.jumlah > 0)
            {
                BahanItem bahan = jamuDatabase.GetBahan(item.nama);
                if (bahan != null)
                {
                    if (playerBahans.ContainsKey(item.nama))
                        playerBahans[item.nama] += item.jumlah;
                    else
                        playerBahans[item.nama] = item.jumlah;
                }
            }
        }

        // Cek apakah semua bahan tersedia
        foreach (string bahan in bahanKombinasi)
        {
            if (!playerBahans.ContainsKey(bahan) || playerBahans[bahan] < 1)
                return false;
        }

        return true;
    }

    /// <summary>
    /// Method utama untuk crafting - coba buat jamu valid dulu, kalau gagal buat jamu gagal
    /// </summary>
    public bool AttemptCrafting(string[] bahanKombinasi)
    {
        // Cek apakah kombinasi membentuk resep valid
        if (IsValidRecipe(bahanKombinasi))
        {
            // Cari resep yang cocok
            foreach (ResepJamu recipe in jamuDatabase.resepJamus)
            {
                if (recipe.bahanResep != null && bahanKombinasi != null &&
                    recipe.bahanResep.Length == bahanKombinasi.Length &&
                    recipe.bahanResep.All(bahan => bahanKombinasi.Contains(bahan)) &&
                    bahanKombinasi.All(bahan => recipe.bahanResep.Contains(bahan)))
                {
                    return CreateJamu(recipe.jamuName);
                }
            }
        }
        else
        {
            // Kombinasi tidak valid, buat jamu gagal tunggal
            return CreateSingleJamuGagal(bahanKombinasi);
        }

        return false;
    }

    /// <summary>
    /// Method wrapper untuk membuat jamu dari kombinasi bahan
    /// </summary>
    public bool CreateJamuFromCombination(string[] bahanKombinasi)
    {
        return AttemptCrafting(bahanKombinasi);
    }
}

/// <summary>
/// Base class for all jamu items
/// </summary>
[Serializable]
public abstract class BaseJamuItem
{
    public string itemName;
    public Sprite itemSprite;
    public int itemValue;  // Market price/value

    [TextArea(2, 5)]
    public string description;
}

/// <summary>
/// Represents a harvested bahan item
/// </summary>
[Serializable]
public class BahanItem : BaseJamuItem
{
    public enum JenisBahan
    {
        Alami,
        NonAlami
    }

    public JenisBahan jenisBahan = JenisBahan.Alami;
}

/// <summary>
/// Represents a benih that can be planted to grow into a bahan
/// </summary>
[Serializable]
public class BenihItem : BaseJamuItem
{
    public Sprite[] growthStages;  // Sprite for each growth stage
    public float growthTime = 1f;  // Time between stages (in seconds)
    public string producesBahanName; // simpan nama bahan saja
}

/// <summary>
/// Represents a jamu recipe
/// </summary>
[Serializable]
public class ResepJamu : BaseJamuItem
{
    public string jamuName => itemName;     // Name of the jamu
    public Sprite jamuSprite => itemSprite; // Result jamu sprite
    public int jamuValue => itemValue;      // Value of the resulting jamu

    public string[] bahanResep;  // Array of bahan names needed for this recipe

}

[Serializable]
public class JamuGagal : BaseJamuItem
{
    public string[] bahanPenyusun;  // Bahan-bahan yang digunakan untuk membuat jamu gagal ini
    public DateTime tanggalDibuat;  // Kapan jamu gagal ini dibuat
}

/// <summary>
/// The main database that holds all bahans, benihs, and resep jamus
/// </summary>
[CreateAssetMenu(fileName = "JamuDatabase", menuName = "Jamu System/Jamu Database")]
public class JamuDatabase : ScriptableObject
{
    [Header("Bahans")]
    public List<BahanItem> bahans = new List<BahanItem>();

    [Header("Benihs")]
    public List<BenihItem> benihs = new List<BenihItem>();

    [Header("Resep Jamu")]
    public List<ResepJamu> resepJamus = new List<ResepJamu>();

    [Header("Jamu Gagal")]
    public List<JamuGagal> jamuGagalList = new List<JamuGagal>();

    [Header("Default Sprites")]
    public Sprite defaultFailedJamuSprite; // Sprite default untuk jamu gagal

    // Helper methods to get items by name
    public BahanItem GetBahan(string bahanName)
    {
        return bahans.Find(h => h.itemName == bahanName);
    }

    public BenihItem GetBenih(string benihName)
    {
        return benihs.Find(s => s.itemName == benihName);
    }

    public ResepJamu GetResepJamu(string jamuName)
    {
        return resepJamus.Find(j => j.jamuName == jamuName);
    }

    public JamuGagal GetJamuGagal(string[] bahanKombinasi)
    {
        return jamuGagalList.Find(j =>
            j.bahanPenyusun != null &&
            bahanKombinasi != null &&
            j.bahanPenyusun.Length == bahanKombinasi.Length &&
            j.bahanPenyusun.All(bahan => bahanKombinasi.Contains(bahan)) &&
            bahanKombinasi.All(bahan => j.bahanPenyusun.Contains(bahan))
        );
    }

    // Add this method to JamuDatabase class
    public BahanItem FindBahanBySprite(Sprite sprite)
    {
        if (sprite == null) return null;

        foreach (BahanItem bahan in bahans)
        {
            if (bahan != null && bahan.itemSprite != null &&
                bahan.itemSprite.name == sprite.name)
            {
                return bahan;
            }
        }

        Debug.LogWarning($"Could not find BahanItem with sprite: {sprite.name}");
        return null;
    }
}