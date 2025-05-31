using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class Shopp : MonoBehaviour
{
    [SerializeField]
    Image[] etalase;

    [SerializeField]
    Image preview;
    [SerializeField]
    Text txtHarga, txtKoin, txtNamaBarang;
    [SerializeField]
    GameObject btnBeli;

    private JamuCraftingIntegration jamuIntegration;
    private List<Item> shopItems = new List<Item>(); // All shop items come from JamuSystem database

    [SerializeField]
    private bool includeBahans = true; // Whether to include jamu bahan (ingredients) in shop

    [SerializeField]
    private bool includeBenihs = true; // Whether to include jamu benih (seeds) in shop

    [SerializeField]
    private bool includeJamus = false; // Whether to include crafted jamu in shop

    int page = 0;

    DataGame dtg;

    void Start()
    {
        SetPreviewAlpha(0f);

        jamuIntegration = JamuCraftingIntegration.Instance;
        if (jamuIntegration == null)
        {
            Debug.LogError("JamuIntegration belum ditemukan!");
        }
        else if (jamuIntegration.jamuSystem == null || jamuIntegration.jamuSystem.jamuDatabase == null)
        {
            Debug.LogError("JamuSystem atau JamuDatabase belum ditemukan!");
        }

        RefreshData();
        LoadShopItems();
        tampil();
        UpdateKoinDisplay();
        gameObject.SetActive(false);
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

        if (jamuIntegration == null || jamuIntegration.jamuSystem == null ||
            jamuIntegration.jamuSystem.jamuDatabase == null)
        {
            Debug.LogError("Tidak bisa memuat item shop: JamuSystem tidak tersedia");
            return;
        }

        var jamuDB = jamuIntegration.jamuSystem.jamuDatabase;

        if (includeBahans && jamuDB.bahans != null)
        {
            foreach (BahanItem bahan in jamuDB.bahans)
            {
                if (bahan.jenisBahan == BahanItem.JenisBahan.NonAlami)
                {
                    Item shopItem = jamuIntegration.ConvertBahanToItem(bahan);
                    if (shopItem != null)
                    {
                        shopItems.Add(shopItem);
                    }
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

                shopItems.Add(shopItem);
            }
        }

        Debug.Log($"Loaded {shopItems.Count} items into shop from JamuSystem database");
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
                {
                    dtg.barang[i] = new Item();
                }
            }
            GameManager.instance.SaveGameData();
        }
    }

    public void show()
    {
        RefreshData();
        LoadShopItems();
        this.gameObject.SetActive(true);
        tampil();
        UpdateKoinDisplay();
    }

    public void hide() { this.gameObject.SetActive(false); }

    public void next()
    {
        page++;
        if (page * etalase.Length >= shopItems.Count)
        {
            page--;
        }
        tampil();
    }

    public void prev()
    {
        page--;
        if (page < 0)
        {
            page = 0;
        }
        tampil();
    }

    void tampil()
    {
        for (int i = 0; i < etalase.Length; i++)
        {
            int index = i + etalase.Length * page;
            if (index < shopItems.Count)
            {
                etalase[i].sprite = shopItems[index].gambar;
                etalase[i].color = new Color(1, 1, 1, 1);

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
                etalase[i].GetComponent<Button>().onClick.RemoveAllListeners();
            }
        }
    }

    Sprite temp;
    int hargabeli = 1;
    string currentItemName = "";

    public void BarangBelanjaDibeli(Item item)
    {
        temp = item.gambar;
        preview.sprite = temp;
        SetPreviewAlpha(1f);
        hargabeli = item.harga;
        currentItemName = item.nama;
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

        // Periksa apakah item sudah ada atau cari slot kosong
        for (int i = 0; i < dtg.barang.Count; i++)
        {
            if (dtg.barang[i] == null)
            {
                dtg.barang[i] = new Item();
            }

            if (dtg.barang[i].gambar == null && emptySlot == -1)
            {
                emptySlot = i;
            }
            else if (dtg.barang[i].gambar != null && dtg.barang[i].nama == currentItemName)
            {
                dtg.barang[i].jumlah += 1;
                GameManager.instance.SpendMoney(hargabeli);
                isSudahAda = true;

                // ... dalam Shopp.cs method Beli()
                if (TaskManager.Instance != null)
                {
                    TaskManager.Instance.OnItemBought(currentItemName, 1);
                }

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

            // TASK INTEGRATION: Beritahu TaskManager tentang pembelian
            if (TaskManager.Instance != null)
            {
                TaskManager.Instance.OnItemBought(currentItemName, 1);
            }
            TryAddBahanToAlmanac(currentItemName);
        }
        else if (!isSudahAda)
        {
            Debug.Log("Inventory penuh, tidak bisa membeli barang baru!");
            return;
        }

        GameManager.instance.SaveInventory(dtg.barang);
        UpdateKoinDisplay();

        // Perbarui inventory jika panel inventory aktif
        Inventory invPanel = Object.FindFirstObjectByType<Inventory>(FindObjectsInactive.Include);
        if (invPanel != null)
        {
            invPanel.RefreshInventory();
        }
    }

    void UpdateKoinDisplay()
    {
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
}