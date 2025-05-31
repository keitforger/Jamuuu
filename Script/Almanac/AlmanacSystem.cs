using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using System.Collections;

public class AlmanacSystem : MonoBehaviour
{
    public static AlmanacSystem Instance { get; private set; }
    public static event System.Action<AlmanacSystem> OnAlmanacEnabled;

    public JamuSystem jamuSystem;

    [Header("Database Reference")]
    public JamuDatabase jamuDatabase;

    private HashSet<string> discoveredJamus = new HashSet<string>();

    [System.Serializable]
    public class AlmanacItemData
    {
        public string nama;
        [TextArea] public string manfaat;
        public string tipe; // "Rempah" atau "Jamu"
        public Sprite gambar;
        public bool ditemukan = false;
    }

    private Dictionary<string, AlmanacItemData> discoveredItems = new Dictionary<string, AlmanacItemData>();

    [Header("UI Almanac")]
    public GameObject almanacPanel;
    public Button btnTutup;

    [Header("Tabs")]
    public Button btnRempahTab;
    public Button btnJamuTab;

    [Header("Content")]
    public Transform entriesContainerRempah;
    public Transform entriesContainerJamu;

    private Transform entriesContainer; // Container for item buttons (Grid Layout Group)
    public GameObject itemButtonPrefab; // Prefab for each item button

    [Header("Navigation")]
    public Button btnNext;
    public Button btnPrev;
    public int itemsPerPage = 6;

    [Header("Detail Panel")]
    public GameObject detailPanel;
    public Image detailImage;
    public Text detailNamaText;
    public Text detailManfaatText;

    // Runtime variables
    private int currentPage = 0;
    private string currentFilter = "Rempah"; // "Rempah" atau "Jamu"
    private List<AlmanacItemData> filteredItems = new List<AlmanacItemData>();
    private List<GameObject> instantiatedButtons = new List<GameObject>();
    private bool isAlmanacDataRefreshed = false;
    private Coroutine detailImageAnimationCoroutine;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        if (jamuSystem == null)
        {
            jamuSystem = FindAnyObjectByType<JamuSystem>();
            if (jamuSystem == null)
            {
                Debug.LogWarning("JamuSystem reference not found! Will try again later.");
            }
            else
            {
                Debug.Log("JamuSystem reference found");
            }
        }

        InitUI();
        SyncDiscoveredFromDataGame();

        if (discoveredItems.Count == 0)
        {
            Debug.Log("No items found in almanac, refreshing from JamuSystem");
            RefreshAlmanacDataFromJamuSystem();
            SyncDiscoveredFromDataGame();
        }

        almanacPanel.SetActive(false);
        detailPanel.SetActive(false);
    }

    private void SyncDiscoveredFromDataGame()
    {
        // Sync semua item dari JamuSystem dulu
        RefreshAlmanacDataFromJamuSystem();
        // Sinkron status ditemukan dari DataGame
        var dataGame = GameManager.instance?.gameData;
        if (dataGame == null) return;
        foreach (var key in discoveredItems.Keys)
        {
            discoveredItems[key].ditemukan = dataGame.discoveredAlmanacItems.Contains(key);
        }
    }

    private void MarkDiscovered(string itemName)
    {
        var dataGame = GameManager.instance?.gameData;
        if (dataGame == null) return;
        if (!dataGame.discoveredAlmanacItems.Contains(itemName))
        {
            dataGame.discoveredAlmanacItems.Add(itemName);
            GameManager.instance.SaveGameData();
        }
        if (discoveredItems.TryGetValue(itemName, out var di))
        {
            di.ditemukan = true;
        }
    }
    private void OnEnable()
    {
        OnAlmanacEnabled?.Invoke(this);
    }

    private void InitUI()
    {
        btnTutup.onClick.AddListener(CloseAlmanac);
        btnRempahTab.onClick.AddListener(() => SetFilter("Rempah"));
        btnJamuTab.onClick.AddListener(() => SetFilter("Jamu"));
        btnNext.onClick.AddListener(NextPage);
        btnPrev.onClick.AddListener(PrevPage);
        UpdateTabVisuals("Rempah");
    }

    public void OpenAlmanac()
    {
        StartCoroutine(OpenAlmanacCoroutine());
    }

    private IEnumerator OpenAlmanacCoroutine()
    {
        // Tunggu JamuSystem dan database siap
        while (jamuSystem == null || jamuSystem.jamuDatabase == null)
        {
            jamuSystem = FindAnyObjectByType<JamuSystem>();
            yield return null;
        }

        RefreshAlmanacDataFromJamuSystem();
        SyncDiscoveredFromDataGame();
        // Tambahan ini: Paksa refresh dulu sebelum buka tab awal
        AlmanacSystem.Instance.SetFilter("Jamu");
        AlmanacSystem.Instance.SetFilter("Rempah");


        almanacPanel.SetActive(true);
        currentPage = 0;
        SetFilter("Rempah");
    }

    public void CloseAlmanac()
    {
        almanacPanel.SetActive(false);
        CloseDetailPanel();
    }

    private void CloseDetailPanel()
    {
        if (detailImageAnimationCoroutine != null)
        {
            StopCoroutine(detailImageAnimationCoroutine);
            detailImageAnimationCoroutine = null;
        }
        if (detailImage != null)
            detailImage.transform.localScale = Vector3.one;
        detailPanel.SetActive(false);
    }

   public void SetFilter(string tipe)
{
    currentFilter = tipe;
    currentPage = 0;
    UpdateTabVisuals(tipe);

    SetEntriesContainerByType(tipe);   // AKTIFKAN CONTAINER DULU
    RefreshItemDisplay();              // BARU REFRESH ISI

    if (detailPanel.activeSelf)
    {
        CloseDetailPanelAnimation();
    }
}

    private void SetEntriesContainerByType(string tipe)
    {
        if (tipe == "Rempah")
        {
            entriesContainerRempah.gameObject.SetActive(true);
            entriesContainerJamu.gameObject.SetActive(false);
            entriesContainer = entriesContainerRempah;
        }
        else if (tipe == "Jamu")
        {
            entriesContainerRempah.gameObject.SetActive(false);
            entriesContainerJamu.gameObject.SetActive(true);
            entriesContainer = entriesContainerJamu;
        }
    }

    void UpdateTabVisuals(string selectedTab)
    {
        Color activeColor = new Color(1f, 0.8f, 0.4f);
        Color inactiveColor = new Color(0.8f, 0.6f, 0.4f);

        if (btnRempahTab.GetComponent<Image>() != null)
            btnRempahTab.GetComponent<Image>().color = selectedTab == "Rempah" ? activeColor : inactiveColor;
        if (btnJamuTab.GetComponent<Image>() != null)
            btnJamuTab.GetComponent<Image>().color = selectedTab == "Jamu" ? activeColor : inactiveColor;
    }

    void RefreshItemDisplay()
    {
        foreach (var button in instantiatedButtons)
            Destroy(button);
        instantiatedButtons.Clear();

        filteredItems.Clear();
        foreach (var item in discoveredItems.Values)
            if (item.tipe == currentFilter && item.ditemukan)
                filteredItems.Add(item);

        int totalPages = Mathf.CeilToInt((float)filteredItems.Count / itemsPerPage);
        if (currentPage >= totalPages && totalPages > 0)
            currentPage = totalPages - 1;

        btnNext.interactable = (currentPage < totalPages - 1);
        btnPrev.interactable = (currentPage > 0);

        int startIndex = currentPage * itemsPerPage;
        int itemsToShow = Mathf.Min(itemsPerPage, filteredItems.Count - startIndex);

        for (int i = 0; i < itemsToShow; i++)
        {
            int itemIndex = startIndex + i;
            if (itemIndex < filteredItems.Count)
                CreateItemButton(filteredItems[itemIndex]);
        }
    }

    void CreateItemButton(AlmanacSystem.AlmanacItemData item)
    {
        GameObject buttonObj = Instantiate(itemButtonPrefab, entriesContainer);
        instantiatedButtons.Add(buttonObj);

        AlmanacItemButton itemButton = buttonObj.GetComponent<AlmanacItemButton>();
        if (itemButton == null)
            itemButton = buttonObj.AddComponent<AlmanacItemButton>();

        // SIMPAN DATA SAJA, TIDAK LANGSUNG SET VISUAL
        itemButton.CacheItemData(item);

        // Event click tetap ditambahkan
        Button btn = buttonObj.GetComponent<Button>();
        if (btn == null)
            btn = buttonObj.AddComponent<Button>();

        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() => ShowItemDetail(item));
    }


    public void ShowItemDetail(AlmanacItemData item)
    {
        detailPanel.SetActive(true);
        detailImage.sprite = item.gambar;
        detailImage.preserveAspect = true;
        detailNamaText.text = item.nama;
        detailManfaatText.text = item.manfaat;
        StartDetailPanelAnimation();
    }

    private void StartDetailPanelAnimation()
    {
        detailPanel.transform.localScale = Vector3.zero;
        StartCoroutine(ScaleAnimation(detailPanel.transform, Vector3.one, 0.3f));
        if (detailImageAnimationCoroutine != null)
            StopCoroutine(detailImageAnimationCoroutine);
        detailImageAnimationCoroutine = StartCoroutine(PulsateAnimation(detailImage.transform, 1.05f, 0.5f));
    }

    private System.Collections.IEnumerator ScaleAnimation(Transform target, Vector3 targetScale, float duration)
    {
        Vector3 startScale = target.localScale;
        float time = 0;
        while (time < duration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / duration);
            float c1 = 1.70158f;
            float c3 = c1 + 1;
            float progress = 1 + c3 * Mathf.Pow(t - 1, 3) + c1 * Mathf.Pow(t - 1, 2);
            target.localScale = Vector3.Lerp(startScale, targetScale, progress);
            yield return null;
        }
        target.localScale = targetScale;
    }

    private System.Collections.IEnumerator PulsateAnimation(Transform target, float maxScale, float duration)
    {
        Vector3 baseScale = Vector3.one;
        Vector3 targetScale = new Vector3(maxScale, maxScale, 1);
        while (true)
        {
            float time = 0;
            while (time < duration)
            {
                time += Time.deltaTime;
                float t = Mathf.SmoothStep(0, 1, Mathf.Clamp01(time / duration));
                target.localScale = Vector3.Lerp(baseScale, targetScale, t);
                yield return null;
            }
            time = 0;
            while (time < duration)
            {
                time += Time.deltaTime;
                float t = Mathf.SmoothStep(0, 1, Mathf.Clamp01(time / duration));
                target.localScale = Vector3.Lerp(targetScale, baseScale, t);
                yield return null;
            }
        }
    }

    private void CloseDetailPanelAnimation()
    {
        if (!detailPanel.activeSelf) return;
        StartCoroutine(ScaleAnimation(detailPanel.transform, Vector3.zero, 0.3f, onComplete: () =>
        {
            detailPanel.SetActive(false);
        }));
        if (detailImageAnimationCoroutine != null)
        {
            StopCoroutine(detailImageAnimationCoroutine);
            detailImageAnimationCoroutine = null;
        }
        if (detailImage != null)
            detailImage.transform.localScale = Vector3.one;
    }

    private System.Collections.IEnumerator ScaleAnimation(Transform target, Vector3 targetScale, float duration, System.Action onComplete = null)
    {
        Vector3 startScale = target.localScale;
        float time = 0;
        while (time < duration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / duration);
            float c1 = 1.70158f;
            float c3 = c1 + 1;
            float progress = 1 + c3 * Mathf.Pow(t - 1, 3) + c1 * Mathf.Pow(t - 1, 2);
            target.localScale = Vector3.Lerp(startScale, targetScale, progress);
            yield return null;
        }
        target.localScale = targetScale;
        onComplete?.Invoke();
    }

    void NextPage()
    {
        currentPage++;
        RefreshItemDisplay();
    }

    void PrevPage()
    {
        if (currentPage > 0)
        {
            currentPage--;
            RefreshItemDisplay();
        }
    }

    private void RefreshAlmanacDataFromJamuSystem()
    {
        if (jamuSystem == null || jamuSystem.jamuDatabase == null)
        {
            if (isAlmanacDataRefreshed) return;
            isAlmanacDataRefreshed = true;
            Debug.LogWarning("Cannot refresh almanac - JamuSystem or database not found!");
            jamuSystem = FindAnyObjectByType<JamuSystem>();
            if (jamuSystem == null || jamuSystem.jamuDatabase == null)
            {
                Debug.LogError("JamuSystem could not be found in the scene.");
                return;
            }
        }
        // Process Bahans (Rempah)
        foreach (var bahan in jamuSystem.jamuDatabase.bahans)
        {
            if (!discoveredItems.ContainsKey(bahan.itemName))
            {
                AlmanacItemData newItem = new AlmanacItemData
                {
                    nama = bahan.itemName,
                    manfaat = bahan.description,
                    tipe = "Rempah",
                    gambar = bahan.itemSprite,
                    ditemukan = false
                };
                discoveredItems.Add(bahan.itemName, newItem);
            }
        }
        // Process Jamu recipes
        foreach (var resep in jamuSystem.jamuDatabase.resepJamus)
        {
            if (!discoveredItems.ContainsKey(resep.jamuName))
            {
                AlmanacItemData newItem = new AlmanacItemData
                {
                    nama = resep.jamuName,
                    manfaat = resep.description,
                    tipe = "Jamu",
                    gambar = resep.jamuSprite,
                    ditemukan = false
                };
                discoveredItems.Add(resep.jamuName, newItem);
            }
        }
        // Process Jamu Gagal
        foreach (var jamuGagal in jamuSystem.jamuDatabase.jamuGagalList)
        {
            if (!discoveredItems.ContainsKey(jamuGagal.itemName))
            {
                AlmanacItemData newItem = new AlmanacItemData
                {
                    nama = jamuGagal.itemName,
                    manfaat = jamuGagal.description,
                    tipe = "Jamu",
                    gambar = jamuGagal.itemSprite,
                    ditemukan = false
                };
                discoveredItems.Add(jamuGagal.itemName, newItem);
            }
        }
    }

    // Penemuan Rempah
    public void AddItemToAlmanac(string namaItem)
    {
        if (!discoveredItems.ContainsKey(namaItem) && jamuSystem != null)
        {
            BahanItem bahan = jamuSystem.GetBahan(namaItem);
            if (bahan != null)
            {
                AlmanacItemData newItem = new AlmanacItemData
                {
                    nama = bahan.itemName,
                    manfaat = bahan.description,
                    tipe = "Rempah",
                    gambar = bahan.itemSprite,
                    ditemukan = true
                };
                discoveredItems.Add(namaItem, newItem);
                MarkDiscovered(namaItem);
                ShowDiscoveryPopup(newItem);
                return;
            }
        }
        if (discoveredItems.TryGetValue(namaItem, out AlmanacItemData item))
        {
            if (!item.ditemukan)
            {
                item.ditemukan = true;
                MarkDiscovered(namaItem);
                Debug.Log($"Item {namaItem} ditambahkan ke almanac!");
                ShowDiscoveryPopup(item);
            }
            return;
        }
        Debug.LogWarning($"Item {namaItem} tidak ditemukan di JamuSystem");
    }

    // Penemuan Jamu
    public bool DiscoverJamu(string jamuName)
    {
        Debug.Log($"Attempting to discover jamu: {jamuName}");

        if (jamuSystem == null)
        {
            jamuSystem = FindAnyObjectByType<JamuSystem>();
            if (jamuSystem == null)
            {
                Debug.LogError("JamuSystem not found when trying to discover jamu.");
                return false;
            }
        }
        if (discoveredItems.Count == 0)
            RefreshAlmanacDataFromJamuSystem();

        ResepJamu resep = jamuSystem.GetResepJamu(jamuName);
        if (resep == null)
        {
            Debug.LogWarning($"Jamu {jamuName} not found in database.");
            return false;
        }
        if (!discoveredItems.ContainsKey(jamuName))
        {
            AlmanacItemData newItem = new AlmanacItemData
            {
                nama = resep.jamuName,
                manfaat = resep.description,
                tipe = "Jamu",
                gambar = resep.jamuSprite,
                ditemukan = true
            };
            discoveredItems.Add(jamuName, newItem);
            MarkDiscovered(jamuName);
            ShowDiscoveryPopup(newItem);
            Debug.Log($"Jamu {jamuName} added to almanac as new item!");
            return true;
        }
        else if (!discoveredItems[jamuName].ditemukan)
        {
            discoveredItems[jamuName].ditemukan = true;
            MarkDiscovered(jamuName);
            ShowDiscoveryPopup(discoveredItems[jamuName]);
            Debug.Log($"Jamu {jamuName} marked as discovered in almanac!");
            return true;
        }
        Debug.Log($"Jamu {jamuName} was already in almanac.");
        return false;
    }

    // Penemuan Jamu Gagal
    public bool DiscoverJamuGagal(string jamuGagalName)
    {
        Debug.Log($"Attempting to discover jamu gagal: {jamuGagalName}");

        if (jamuSystem == null)
        {
            jamuSystem = FindAnyObjectByType<JamuSystem>();
            if (jamuSystem == null)
            {
                Debug.LogError("JamuSystem not found when trying to discover jamu gagal.");
                return false;
            }
        }
        if (discoveredItems.Count == 0)
            RefreshAlmanacDataFromJamuSystem();

        JamuGagal jamuGagal = jamuSystem.jamuDatabase.jamuGagalList.Find(j => j.itemName == jamuGagalName);
        if (jamuGagal == null)
        {
            Debug.LogWarning($"Jamu Gagal {jamuGagalName} not found in database.");
            return false;
        }
        if (!discoveredItems.ContainsKey(jamuGagalName))
        {
            AlmanacItemData newItem = new AlmanacItemData
            {
                nama = jamuGagal.itemName,
                manfaat = jamuGagal.description,
                tipe = "Jamu",
                gambar = jamuGagal.itemSprite,
                ditemukan = true
            };
            discoveredItems.Add(jamuGagalName, newItem);
            MarkDiscovered(jamuGagalName);
            ShowDiscoveryPopup(newItem);
            Debug.Log($"Jamu Gagal {jamuGagalName} added to almanac as new item!");
            return true;
        }
        else if (!discoveredItems[jamuGagalName].ditemukan)
        {
            discoveredItems[jamuGagalName].ditemukan = true;
            MarkDiscovered(jamuGagalName);
            ShowDiscoveryPopup(discoveredItems[jamuGagalName]);
            Debug.Log($"Jamu Gagal {jamuGagalName} marked as discovered in almanac!");
            return true;
        }
        Debug.Log($"Jamu Gagal {jamuGagalName} was already in almanac.");
        return false;
    }

    public bool IsNewlyDiscovered(string jamuName)
    {
        if (string.IsNullOrEmpty(jamuName)) return false;
        if (discoveredJamus.Contains(jamuName))
            return false;
        else
        {
            discoveredJamus.Add(jamuName);
            return true;
        }
    }
    void ShowDiscoveryPopup(AlmanacItemData item)
    {
        Debug.Log($"New item discovered: {item.nama}");
        // TODO: Show UI popup
    }

    public void UnlockAllItems()
    {
        var dataGame = GameManager.instance?.gameData;
        foreach (var item in discoveredItems.Values)
        {
            item.ditemukan = true;
            if (dataGame != null && !dataGame.discoveredAlmanacItems.Contains(item.nama))
                dataGame.discoveredAlmanacItems.Add(item.nama);
        }
        GameManager.instance?.SaveGameData();
        if (almanacPanel.activeSelf)
            RefreshItemDisplay();
        Debug.Log("All almanac items unlocked!");
    }

    public bool HasDiscovered(string jamuName)
    {
        var dataGame = GameManager.instance?.gameData;
        return discoveredItems.ContainsKey(jamuName) && dataGame != null && dataGame.discoveredAlmanacItems.Contains(jamuName);
    }

    [ContextMenu("Debug Almanac Status")]
    public void DebugAlmanacStatus()
    {
        Debug.Log("=== ALMANAC DEBUG STATUS ===");
        Debug.Log($"Total items in almanac: {discoveredItems.Count}");

        if (jamuSystem?.jamuDatabase?.resepJamus != null)
        {
            Debug.Log($"Total resep in database: {jamuSystem.jamuDatabase.resepJamus.Count}");
            foreach (var resep in jamuSystem.jamuDatabase.resepJamus)
            {
                bool discovered = HasDiscovered(resep.jamuName);
                Debug.Log($"Jamu: {resep.jamuName} - Discovered: {discovered}");
            }
        }
        Debug.Log("--- DISCOVERED ITEMS ---");
        foreach (var item in discoveredItems)
        {
            if (item.Value.ditemukan)
            {
                Debug.Log($"✓ {item.Key} ({item.Value.tipe})");
            }
        }
    }

    [ContextMenu("Unlock First Jamu")]
    public void UnlockFirstJamuForTesting()
    {
        if (jamuSystem?.jamuDatabase?.resepJamus != null && jamuSystem.jamuDatabase.resepJamus.Count > 0)
        {
            string firstJamuName = jamuSystem.jamuDatabase.resepJamus[0].jamuName;
            DiscoverJamu(firstJamuName);
            Debug.Log($"Manually unlocked: {firstJamuName}");
        }
    }

    [ContextMenu("Test JamuSystem Connection")]
    public void TestJamuSystemConnection()
    {
        Debug.Log("=== JAMUSYSTEM CONNECTION TEST ===");
        if (jamuSystem == null)
        {
            Debug.LogError("JamuSystem is NULL!");
            jamuSystem = FindAnyObjectByType<JamuSystem>();
            if (jamuSystem != null)
            {
                Debug.Log("JamuSystem found via FindAnyObjectByType");
            }
            else
            {
                Debug.LogError("JamuSystem not found in scene!");
                return;
            }
        }
        if (jamuSystem.jamuDatabase == null)
        {
            Debug.LogError("JamuDatabase is NULL!");
            return;
        }
        Debug.Log($"JamuSystem connected: {jamuSystem.name}");
        Debug.Log($"Database has {jamuSystem.jamuDatabase.resepJamus.Count} recipes");
        Debug.Log($"Database has {jamuSystem.jamuDatabase.bahans.Count} bahans");
    }
}