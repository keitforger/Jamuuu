using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class Shopp : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image[] etalase;
    [SerializeField] private Text[] txtNamaEtalase; // Drag urut sesuai gambar etalase!
    [SerializeField] private Image preview;
    [SerializeField] public Text txtNamaBarang; // Untuk preview (kanan)
    [SerializeField] private Text txtHarga, txtKoin;
    [SerializeField] private GameObject btnBeli;

    private JamuCraftingIntegration jamuIntegration;

    public class ShopItemData
    {
        public Item item;
        public BaseJamuItem itemData;
    }
    private List<ShopItemData> shopItems = new List<ShopItemData>();

    [Header("Shop Item Filter")]
    [SerializeField] private bool includeBahans = true;
    [SerializeField] private bool includeBenihs = true;
    [SerializeField] private bool includeJamus = false;

    int page = 0;

    DataGame dtg;

    // State barang preview
    Sprite temp;
    int hargabeli = 1;
    string currentItemName = "";
    BaseJamuItem currentItemData = null;

    void Start()
    {
        SetPreviewAlpha(0f);

        jamuIntegration = JamuCraftingIntegration.Instance;
        if (jamuIntegration == null || jamuIntegration.jamuSystem == null || jamuIntegration.jamuSystem.jamuDatabase == null)
        {
            Debug.LogError("JamuIntegration/JamuSystem/JamuDatabase belum ditemukan!");
        }

        RefreshData();
        LoadShopItems();
        tampil();
        UpdateKoinDisplay();
    }

    void SetPreviewAlpha(float alpha)
    {
        Color c = preview.color;
        c.a = alpha;
        preview.color = c;
    }

    private void LoadShopItems()
    {
        shopItems.Clear();
        var jamuDB = jamuIntegration.jamuSystem.jamuDatabase;

        if (includeBahans && jamuDB.bahans != null)
        {
            foreach (BahanItem bahan in jamuDB.bahans)
            {
                if (bahan.jenisBahan == BahanItem.JenisBahan.NonAlami)
                {
                    Item shopItem = jamuIntegration.ConvertBahanToItem(bahan);
                    if (shopItem != null)
                        shopItems.Add(new ShopItemData { item = shopItem, itemData = bahan });
                }
            }
        }
        if (includeBenihs && jamuDB.benihs != null)
        {
            foreach (BenihItem benih in jamuDB.benihs)
            {
                Item shopItem = new Item
                {
                    nama = benih.itemName,
                    gambar = benih.itemSprite,
                    harga = benih.itemValue,
                    jumlah = 1
                };
                shopItems.Add(new ShopItemData { item = shopItem, itemData = benih });
            }
        }
        // Tambahkan jamu jadi jika perlu...
    }

    public void show()
    {
        RefreshData();
        LoadShopItems();
        gameObject.SetActive(true);
        tampil();
        UpdateKoinDisplay();
    }

    public void hide() => gameObject.SetActive(false);

    public void next()
    {
        page++;
        if (page * etalase.Length >= shopItems.Count) page--;
        tampil();
    }

    public void prev()
    {
        page--;
        if (page < 0) page = 0;
        tampil();
    }

    void tampil()
    {
        for (int i = 0; i < etalase.Length; i++)
        {
            int index = i + etalase.Length * page;
            if (index < shopItems.Count)
            {
                var data = shopItems[index];

                etalase[i].sprite = data.item.gambar;
                etalase[i].color = Color.white;

                // Set nama (ambil dari database) — pastikan urutan txtNamaEtalase sama dengan etalase!
                if (txtNamaEtalase != null && i < txtNamaEtalase.Length && txtNamaEtalase[i] != null)
                    txtNamaEtalase[i].text = data.itemData.itemName;
                else
                    Debug.LogWarning("txtNamaEtalase belum diisi dengan benar di Inspector!");

                int capturedIndex = index;
                etalase[i].GetComponent<Button>().onClick.RemoveAllListeners();
                etalase[i].GetComponent<Button>().onClick.AddListener(() =>
                {
                    BarangBelanjaDibeli(shopItems[capturedIndex]);
                });
            }
            else
            {
                etalase[i].sprite = null;
                etalase[i].color = new Color(1, 1, 1, 0);

                // Kosongkan nama jika slot kosong
                if (txtNamaEtalase != null && i < txtNamaEtalase.Length && txtNamaEtalase[i] != null)
                    txtNamaEtalase[i].text = "";
                etalase[i].GetComponent<Button>().onClick.RemoveAllListeners();
            }
        }
    }

    public void BarangBelanjaDibeli(ShopItemData shopItemData)
    {
        temp = shopItemData.item.gambar;
        preview.sprite = temp;
        SetPreviewAlpha(1f);
        hargabeli = shopItemData.item.harga;
        currentItemName = shopItemData.itemData.itemName;
        currentItemData = shopItemData.itemData;

        // Preview — Ambil nama dari database
        if (txtNamaBarang != null)
            txtNamaBarang.text = currentItemName;

        txtHarga.text = hargabeli.ToString();
        btnBeli.SetActive(true);
    }

    public void Beli()
    {
        RefreshData();

        if (dtg.koin < hargabeli)
        {
            Debug.Log("Koin tidak cukup untuk membeli barang ini!");
            return;
        }

        bool isSudahAda = false;
        int emptySlot = -1;

        for (int i = 0; i < dtg.barang.Count; i++)
        {
            if (dtg.barang[i] == null)
                dtg.barang[i] = new Item();

            if (dtg.barang[i].gambar == null && emptySlot == -1)
                emptySlot = i;
            else if (dtg.barang[i].gambar != null && dtg.barang[i].nama == currentItemName)
            {
                dtg.barang[i].jumlah += 1;
                GameManager.instance.SpendMoney(hargabeli);
                isSudahAda = true;
                if (TaskManager.Instance != null)
                    TaskManager.Instance.OnItemBought(currentItemName, 1);
                TryAddBahanToAlmanac(currentItemName);
                break;
            }
        }

        if (!isSudahAda && emptySlot != -1)
        {
            dtg.barang[emptySlot].gambar = temp;
            dtg.barang[emptySlot].nama = currentItemName;
            dtg.barang[emptySlot].harga = hargabeli;
            dtg.barang[emptySlot].jumlah = 1;
            GameManager.instance.SpendMoney(hargabeli);
            if (TaskManager.Instance != null)
                TaskManager.Instance.OnItemBought(currentItemName, 1);
            TryAddBahanToAlmanac(currentItemName);
        }
        else if (!isSudahAda)
        {
            Debug.Log("Inventory penuh, tidak bisa membeli barang baru!");
            return;
        }

        GameManager.instance.SaveInventory(dtg.barang);
        UpdateKoinDisplay();

        Inventory invPanel = Object.FindFirstObjectByType<Inventory>(FindObjectsInactive.Include);
        if (invPanel != null)
            invPanel.RefreshInventory();
    }

    void UpdateKoinDisplay()
    {
        if (txtKoin != null && dtg != null)
            txtKoin.text = dtg.koin.ToString();
    }

    private void TryAddBahanToAlmanac(string itemName)
    {
        if (AlmanacSystem.Instance != null && JamuCraftingIntegration.Instance != null)
        {
            var jamuSystem = JamuCraftingIntegration.Instance.jamuSystem;
            if (jamuSystem != null && jamuSystem.jamuDatabase != null)
            {
                BahanItem bahan = jamuSystem.jamuDatabase.GetBahan(itemName);
                if (bahan != null)
                {
                    AlmanacSystem.Instance.AddItemToAlmanac(itemName);
                }
            }
        }
    }

    public void ToggleBahans(bool include)
    {
        includeBahans = include;
        LoadShopItems();
        page = 0;
        tampil();
    }

    public void ToggleBenihs(bool include)
    {
        includeBenihs = include;
        LoadShopItems();
        page = 0;
        tampil();
    }

    public void RefreshData()
    {
        dtg = GameManager.instance.gameData;
        if (dtg == null)
        {
            dtg = new DataGame();
            dtg.koin = 10000;
            int slotCount = 15;
            for (int i = 0; i < slotCount; i++)
            {
                dtg.barang.Add(new Item());
            }
            GameManager.instance.gameData = dtg;
            GameManager.instance.SaveGameData();
        }
        else
        {
            int slotCount = 15;
            while (dtg.barang.Count < slotCount)
            {
                dtg.barang.Add(new Item());
            }
            for (int i = 0; i < dtg.barang.Count; i++)
            {
                if (dtg.barang[i] == null)
                    dtg.barang[i] = new Item();
            }
            GameManager.instance.SaveGameData();
        }
    }
}