using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static UnityEditor.Progress;

public class Inventory : MonoBehaviour
{
    public static Inventory Instance { get; private set; } // Singleton Instance
    public static event System.Action<Inventory> OnInventoryEnabled;

    [SerializeField] private GameObject inventoryPanel;
    [SerializeField] GameObject itemSlotPrefab;
    [SerializeField] Transform isiTasParent;
    private List<GameObject> isiTas = new List<GameObject>(); // Slot untuk barang di inventory
    [SerializeField] Image preview; // Preview untuk barang yang akan dijual
    [SerializeField] Text txtHarga, txtKoin; // UI untuk harga dan koin
    [SerializeField] GameObject imgKoin;
    [SerializeField] Text txtHargaInventory;
    [SerializeField] GameObject btnJual; // Tombol jual

    DataGame dtg;

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

    void Start()
    {
        RefreshData();
        GenerateIsiTas();
        tampilIsiTas();

        if (txtHargaInventory != null)
        {
            txtHargaInventory.gameObject.SetActive(false);
        }
        if (imgKoin != null)
        {
            imgKoin.SetActive(false);
        }
        gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        OnInventoryEnabled?.Invoke(this);
    }

    private BaseJamuItem GetBaseItem(string itemName)
    {
        var jamuSystem = JamuSystem.Instance;
        if (jamuSystem == null) return null;

        // Cek apakah item ini adalah bahan, benih, atau jamu
        BaseJamuItem item = jamuSystem.GetBahan(itemName);
        if (item != null) return item;

        item = jamuSystem.GetBenih(itemName);
        if (item != null) return item;

        item = jamuSystem.GetResepJamu(itemName);
        return item;
    }

    public static bool InstanceHasBenih()
    {
        if (Instance == null) return false;

        var data = GameManager.instance.gameData;
        foreach (var item in data.barang)
        {
            if (item != null && item.jumlah > 0 && JamuSystem.Instance.GetBenih(item.nama) != null)
                return true;
        }
        return false;
    }

    public static bool InstanceHasBahan()
    {
        if (Instance == null) return false;

        var data = GameManager.instance.gameData;
        foreach (var item in data.barang)
        {
            if (item != null && item.jumlah > 0 && JamuSystem.Instance.GetBahan(item.nama) != null)
                return true;
        }
        return false;
    }

    public void RefreshData()
    {
        dtg = GameManager.instance.gameData;
        if (dtg == null)
        {
            dtg = new DataGame();
            dtg.koin = 10000; // Starting coins
            dtg.barang = new List<Item>();
            GameManager.instance.gameData = dtg;
            GameManager.instance.SaveGameData();
        }
    }

    public void show()
    {
        RefreshData();
        this.gameObject.SetActive(true);
        tampilIsiTas();
        preview.sprite = null;
        preview.color = new Color(1, 1, 1, 0);
        btnJual.SetActive(false);
        namaItemDipilih = "";
    }

    public void hide()
    {
        this.gameObject.SetActive(false);
    }

    // Fix for Inventory.cs - Update RefreshInventory method
    public void RefreshInventory()
    {
        RefreshData();
        GenerateIsiTas(); // Add this line to regenerate the inventory slots
        tampilIsiTas();   // Then update their display
    }

    public void GenerateIsiTas()
    {
        foreach (Transform child in isiTasParent)
        {
            Destroy(child.gameObject);
        }
        isiTas.Clear();

        RefreshData();

        foreach (Item item in dtg.barang)
        {
            if (item != null && item.jumlah > 0)
            {
                BaseJamuItem baseItem = GetBaseItem(item.nama);
                if (baseItem == null) continue;

                GameObject slot = Instantiate(itemSlotPrefab, isiTasParent);
                isiTas.Add(slot);

                Image itemImage = slot.transform.GetChild(0).GetComponent<Image>();
                Text itemCountText = slot.transform.GetChild(1).GetComponent<Text>();

                if (itemImage != null && itemCountText != null)
                {
                    itemImage.sprite = baseItem.itemSprite;
                    itemCountText.text = item.jumlah.ToString();

                    Button slotButton = slot.transform.GetChild(0).GetComponent<Button>();
                    if (slotButton != null)
                    {
                        Item copiedItem = new Item
                        {
                            nama = item.nama,
                            jumlah = item.jumlah
                        };

                        slotButton.onClick.RemoveAllListeners();
                        slotButton.onClick.AddListener(() =>
                        {
                            BarangBelanjaDijual(copiedItem);
                        });
                    }
                }
            }
        }
    }

    void tampilIsiTas()
    {
        RefreshData();

        List<Item> validItems = new List<Item>();
        foreach (Item item in dtg.barang)
        {
            if (item != null && item.jumlah > 0 && GetBaseItem(item.nama) != null)
            {
                validItems.Add(item);
            }
        }

        for (int i = 0; i < isiTas.Count && i < validItems.Count; i++)
        {
            Image myItem = isiTas[i].transform.GetChild(0).GetComponent<Image>();
            Text myText = isiTas[i].transform.GetChild(1).GetComponent<Text>();

            BaseJamuItem baseItem = GetBaseItem(validItems[i].nama);
            if (baseItem != null)
            {
                myItem.sprite = baseItem.itemSprite;
                myText.text = validItems[i].jumlah.ToString();
                myItem.color = new Color(1f, 1f, 1f, 1f);
            }
        }

        if (validItems.Count == 0 && isiTas.Count > 0)
        {
            Image myItem = isiTas[0].transform.GetChild(0).GetComponent<Image>();
            Text myText = isiTas[0].transform.GetChild(1).GetComponent<Text>();

            myItem.sprite = null;
            myItem.color = new Color(1f, 1f, 1f, 0f);
            myText.text = "";
        }

        txtKoin.text = dtg.koin.ToString();
    }

    string namaItemDipilih = "";
    int hargajual = 2;

    public void BarangBelanjaDijual(Item item)
    {
        BaseJamuItem baseItem = GetBaseItem(item.nama);
        if (item == null || baseItem == null || baseItem.itemSprite == null)
        {
            Debug.LogError("Item tidak valid.");
            btnJual.SetActive(false);
            txtHargaInventory.gameObject.SetActive(false);
            imgKoin.SetActive(false); // Nonaktifkan imgKoin
            return;
        }

        preview.sprite = baseItem.itemSprite;
        preview.color = new Color(1f, 1f, 1f, 1f);

        namaItemDipilih = item.nama;

        hargajual = baseItem.itemValue / 2;
        if (hargajual < 1) hargajual = 1;

        txtHarga.text = hargajual.ToString();
        btnJual.SetActive(true);
        txtHargaInventory.gameObject.SetActive(true);
        imgKoin.SetActive(true); // Aktifkan imgKoin
    }

    public void Jual()
    {
        if (string.IsNullOrEmpty(namaItemDipilih))
        {
            Debug.LogError("Tidak ada item yang dipilih untuk dijual.");
            return;
        }

        bool itemSold = false;

        for (int i = 0; i < dtg.barang.Count; i++)
        {
            Item item = dtg.barang[i];
            if (item != null && item.nama == namaItemDipilih)
            {
                item.jumlah--;

                if (item.jumlah <= 0)
                {
                    dtg.barang[i] = new Item();
                }

                GameManager.instance.AddMoney(hargajual); // Tambahkan koin ke GameManager
                itemSold = true;
                GameManager.instance.SaveInventory(dtg.barang); // Simpan inventory ke GameManager
                GameManager.instance.SaveGameData(); // Simpan data game utama (tambah ini)
                break;
            }
        }

        if (itemSold)
        {
            GenerateIsiTas();
            tampilIsiTas();

            preview.sprite = null;
            preview.color = new Color(1f, 1f, 1f, 0f);
            btnJual.SetActive(false);
            txtHargaInventory.gameObject.SetActive(false); // Nonaktifkan txtKoin
            imgKoin.SetActive(false); // Nonaktifkan imgKoin
            namaItemDipilih = "";

            Debug.Log("Item berhasil dijual.");
        }
        else
        {
            Debug.LogError("Gagal menjual item. Item tidak ditemukan.");
        }
    }

    public void TambahItemHasilPanen(BahanItem hasilPanen)
    {
        RefreshData();

        bool isAlreadyExists = false;
        int emptySlot = -1;

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

            if (dtg.barang[i].gambar != null && dtg.barang[i].nama == hasilPanen.itemName)
            {
                dtg.barang[i].jumlah += 1;
                isAlreadyExists = true;
                break;
            }
        }

        if (!isAlreadyExists && emptySlot != -1)
        {
            dtg.barang[emptySlot].gambar = hasilPanen.itemSprite; // Assign the sprite
            dtg.barang[emptySlot].nama = hasilPanen.itemName; // Assign the item name
            dtg.barang[emptySlot].jumlah = 1; // Set initial quantity
        }

        GameManager.instance.SaveGameData(); // Save to data utama
        tampilIsiTas(); // Update inventory display

        Shopp shopPanel = Object.FindFirstObjectByType<Shopp>(FindObjectsInactive.Include);
        if (shopPanel != null)
        {
            shopPanel.RefreshData();
        }
    }

    public void AddItemToInventory(Item itemToAdd)
    {
        RefreshData();
        bool sudahAda = false;

        // Cek apakah item dengan nama yang sama sudah ada di inventory
        for (int i = 0; i < dtg.barang.Count; i++)
        {
            if (dtg.barang[i] != null && dtg.barang[i].gambar != null &&
                dtg.barang[i].nama == itemToAdd.nama)
            {
                dtg.barang[i].jumlah += itemToAdd.jumlah;
                sudahAda = true;
                break;
            }
        }

        // Jika belum ada, tambahkan ke slot kosong pertama
        if (!sudahAda)
        {
            for (int i = 0; i < dtg.barang.Count; i++)
            {
                if (dtg.barang[i] == null)
                {
                    dtg.barang[i] = new Item();
                }

                if (dtg.barang[i].gambar == null || dtg.barang[i].jumlah <= 0)
                {
                    dtg.barang[i] = new Item
                    {
                        nama = itemToAdd.nama,
                        gambar = itemToAdd.gambar,
                        harga = itemToAdd.harga,
                        jumlah = itemToAdd.jumlah
                    };
                    break;
                }
            }
        }

        GameManager.instance.SaveGameData();
        // Regenerate inventory dengan slot dinamis
        GenerateIsiTas();
        tampilIsiTas();
    }

    // Fungsi untuk memindahkan item dari inventory ke combine panel
    public void MoveItemToCombinePanel(int inventoryIndex, Slot combineSlot)
    {
        // Pastikan item ada di inventory
        if (dtg.barang[inventoryIndex].gambar != null)
        {
            // Ambil item dari inventory
            Item item = dtg.barang[inventoryIndex];

            // Cek apakah slot combine sudah kosong
            if (combineSlot.transform.childCount == 0)
            {
                // Buat item baru di slot combine panel
                GameObject newItem = new GameObject(item.nama);
                Image itemImage = newItem.AddComponent<Image>();
                itemImage.sprite = item.gambar;

                newItem.transform.SetParent(combineSlot.transform);
                newItem.transform.localPosition = Vector3.zero;

                // Tambahkan komponen DragDropItem
                DragDropItem dragItem = newItem.AddComponent<DragDropItem>();
                dragItem.Item = item;

                // Simpan parent slot sebagai originalParent
                dragItem.originalParent = combineSlot.transform;

                // Kurangi jumlah di inventory
                dtg.barang[inventoryIndex].jumlah -= 1;

                // Jika jumlah item di inventory menjadi 0, reset item
                if (dtg.barang[inventoryIndex].jumlah <= 0)
                {
                    dtg.barang[inventoryIndex] = new Item(); // Reset item
                }

                GameManager.instance.SaveGameData();
                RefreshInventory();  // Update tampilan inventory setelah perubahan
            }
        }
    }

    // Fungsi untuk mengembalikan item dari combine panel ke inventory
    public void ReturnItemToInventory(Slot combineSlot, int inventoryIndex)
    {
        // Cek apakah slot combine tidak kosong dan item yang ada di combine panel
        if (combineSlot.transform.childCount > 0)
        {
            // Ambil item dari slot combine
            GameObject itemObject = combineSlot.transform.GetChild(0).gameObject;
            DragDropItem dragItem = itemObject.GetComponent<DragDropItem>();
            Item item = dragItem.Item;

            // Tambahkan kembali item ke inventory
            bool added = false;
            for (int i = 0; i < dtg.barang.Count; i++)
            {
                if (dtg.barang[i].gambar != null && dtg.barang[i].gambar.name == item.gambar.name)
                {
                    dtg.barang[i].jumlah += 1;
                    added = true;
                    break;
                }
            }

            // Jika item belum ada, cari slot kosong di inventory
            if (!added)
            {
                for (int i = 0; i < dtg.barang.Count; i++)
                {
                    if (dtg.barang[i].gambar == null)
                    {
                        dtg.barang[i] = item;
                        dtg.barang[i].jumlah = 1;
                        break;
                    }
                }
            }

            // Reset item di combine slot
            Destroy(itemObject);

            // Update inventory dan data game
            GameManager.instance.SaveGameData();
            RefreshInventory(); // Update tampilan inventory setelah perubahan
        }
    }
}