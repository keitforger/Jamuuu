using UnityEngine;
using UnityEngine.UI;

public class PlantingSystem : MonoBehaviour
{
    public static PlantingSystem Instance;

    public static bool isPlantingPanelActive = false;
    private SoilTile currentSoil;

    [Header("UI References")]
    public GameObject panelPilihBibit;
    public Transform isiPanel;
    public GameObject tombolSeedPrefab;
    public Button tombolClose;

    [Header("Position Offsets")]
    [SerializeField] private Vector3 panelOffset = new Vector3(0, 0, 0);
    [SerializeField] private Vector2 tombolCloseMargin = new Vector2(20, 20); // margin dari pojok kanan atas panel (pixel)

    public bool sudahTanam = false;

    private void Awake()
    {
        Instance = this;

        if (panelPilihBibit == null)
            panelPilihBibit = transform.Find("panelPilihBibit")?.gameObject;

        if (tombolClose == null)
            tombolClose = transform.Find("tombolClose")?.GetComponent<Button>();

        if (tombolClose != null)
        {
            tombolClose.onClick.RemoveAllListeners();
            tombolClose.onClick.AddListener(() => CloseSeedPanel());
        }
        else
        {
            Debug.LogError("tombolClose tidak ditemukan/assign di inspector!");
        }

        if (panelPilihBibit == null) Debug.LogError("panelPilihBibit tidak ditemukan/assign di inspector!");
        if (isiPanel == null) Debug.LogError("isiPanel tidak diassign di inspector!");
        if (tombolSeedPrefab == null) Debug.LogError("tombolSeedPrefab tidak diassign di inspector!");

        if (UIManager.Instance != null && panelPilihBibit != null)
            UIManager.Instance.RegisterPanel(panelPilihBibit);

    }

    public void CloseSeedPanel()
    {
        if (panelPilihBibit != null) panelPilihBibit.SetActive(false);
        if (tombolClose != null) tombolClose.gameObject.SetActive(false);
        isPlantingPanelActive = false;
        if (UIManager.Instance != null) UIManager.Instance.UnblockGameInput();
        Debug.Log("Panel pilih bibit dan tombol close telah ditutup.");
    }

    public void StartPlanting(SoilTile soil)
    {
        if (isPlantingPanelActive) return;

        currentSoil = soil;

        if (panelPilihBibit == null || tombolClose == null)
        {
            Debug.LogError("panelPilihBibit atau tombolClose null di StartPlanting");
            return;
        }

        // Tempatkan panel di world position (atau overlay/screen point)
        Vector3 worldPos = soil.transform.position + panelOffset;
        panelPilihBibit.transform.position = worldPos;
        panelPilihBibit.transform.rotation = Quaternion.LookRotation(Camera.main.transform.forward);

        // ====== POSISI TOMBOL X (SUPAYA SELALU DI POJOK KANAN ATAS PANEL) ======
        RectTransform panelRect = panelPilihBibit.GetComponent<RectTransform>();
        RectTransform closeRect = tombolClose.GetComponent<RectTransform>();
        Canvas canvas = panelPilihBibit.GetComponentInParent<Canvas>();
        if (panelRect != null && closeRect != null && canvas != null)
        {
            // Ambil world space pojok kanan atas panel
            Vector3[] corners = new Vector3[4];
            panelRect.GetWorldCorners(corners);
            Vector3 worldTopRight = corners[2];

            // Konversi ke screen point
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(Camera.main, worldTopRight);
            // Offset pixel ke kiri & bawah
            screenPoint.x -= tombolCloseMargin.x;
            screenPoint.y -= tombolCloseMargin.y;

            // Konversi ke local point canvas (anchoredPosition)
            Vector2 localPoint;
            RectTransform canvasRect = canvas.GetComponent<RectTransform>();
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint,
                canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : Camera.main, out localPoint))
            {
                closeRect.anchoredPosition = localPoint;
            }
        }

        panelPilihBibit.SetActive(true);
        tombolClose.gameObject.SetActive(true);

        isPlantingPanelActive = true;

        ShowSeedChoice();

        if (UIManager.Instance != null) UIManager.Instance.BlockGameInput();
    }

    void ShowSeedChoice()
    {
        if (isiPanel == null) { Debug.LogError("isiPanel null!"); return; }
        if (JamuSystem.Instance == null || JamuSystem.Instance.jamuDatabase == null) { Debug.LogError("JamuSystem/jamuDatabase null!"); return; }
        if (tombolSeedPrefab == null) { Debug.LogError("tombolSeedPrefab null!"); return; }

        foreach (Transform child in isiPanel)
        {
            Destroy(child.gameObject);
        }

        var benihList = JamuSystem.Instance.jamuDatabase.benihs;

        // Ambil data dari GameManager (bukan ManagerPP!) sesuai permintaan user
        var data = GameManager.instance != null ? GameManager.instance.gameData : null;
        if (data == null || data.barang == null)
        {
            Debug.LogError("gameData/barang null di ShowSeedChoice!");
            return;
        }

        foreach (BenihItem benih in benihList)
        {
            foreach (Item item in data.barang)
            {
                if (item != null && item.jumlah > 0 && item.nama == benih.itemName)
                {
                    GameObject tombol = Instantiate(tombolSeedPrefab, isiPanel);
                    var img = tombol.GetComponentInChildren<Image>();
                    var txt = tombol.GetComponentInChildren<Text>();
                    var btn = tombol.GetComponent<Button>();

                    if (img != null) img.sprite = benih.itemSprite;
                    if (txt != null) txt.text = item.jumlah.ToString();
                    if (btn != null)
                    {
                        string namaSeed = benih.itemName;
                        btn.onClick.AddListener(() =>
                        {
                            TanamBibit(namaSeed);
                            if (tombolClose != null) tombolClose.gameObject.SetActive(false);
                            if (panelPilihBibit != null) panelPilihBibit.SetActive(false);
                            isPlantingPanelActive = false;
                            if (UIManager.Instance != null) UIManager.Instance.UnblockGameInput();
                        });
                    }
                    else
                    {
                        Debug.LogError("Prefab tombol tidak memiliki komponen Button!");
                    }
                }
            }
        }

        if (panelPilihBibit != null) panelPilihBibit.SetActive(true);
    }

    public void TogglePanelBibit()
    {
        if (panelPilihBibit == null || tombolClose == null) return;
        bool aktif = panelPilihBibit.activeSelf;
        panelPilihBibit.SetActive(!aktif);
        tombolClose.gameObject.SetActive(!aktif);
        isPlantingPanelActive = !aktif;
    }

    void TanamBibit(string namaSeed)
    {
        if (currentSoil == null)
        {
            Debug.LogError("currentSoil null saat TanamBibit! Tidak bisa tanam.");
            return;
        }

        var data = GameManager.instance != null ? GameManager.instance.gameData : null;
        if (data == null || data.barang == null)
        {
            Debug.LogError("gameData/barang null di TanamBibit!");
            return;
        }

        BenihItem seed = JamuSystem.Instance.GetBenih(namaSeed);

        if (seed == null)
        {
            Debug.LogError($"Benih {namaSeed} tidak ditemukan di database.");
            return;
        }

        for (int i = 0; i < data.barang.Count; i++)
        {
            if (data.barang[i] != null && data.barang[i].nama == namaSeed)
            {
                if (data.barang[i].jumlah > 0)
                {
                    data.barang[i].jumlah--;

                    if (data.barang[i].jumlah <= 0)
                    {
                        data.barang[i] = new Item();
                    }

                    sudahTanam = true;
                    if (GameManager.instance != null)
                        GameManager.instance.SaveGameData();
                    currentSoil.Plant(seed);
                    Inventory.Instance.RefreshInventory();

                    if (TaskManager.Instance != null)
                        TaskManager.Instance.OnSeedPlanted(namaSeed);

                    isPlantingPanelActive = false;

                    return;
                }
            }
        }

        Debug.LogWarning("Benih tidak tersedia di inventory.");
    }

    public static bool InstanceTanamSelesai()
    {
        return Instance != null && Instance.sudahTanam;
    }
}