using UnityEngine;
using System.Collections;

public class SoilTile : MonoBehaviour
{
    public SpriteRenderer spriteRenderer;

    private BenihItem benihItem;
    private bool isPlanted = false;
    private int currentStage = -1;
    private float timer = 0f;

    private void OnMouseDown()
    {

        if (!isPlanted)
        {
            if (PlantingSystem.Instance != null)
                PlantingSystem.Instance.StartPlanting(this);
            else
                Debug.LogError("PlantingSystem instance is null.");
        }
        else if (benihItem != null && currentStage >= benihItem.growthStages.Length - 1)
        {
            Harvest();
        }
        else
        {
            Debug.Log("Tanaman belum siap dipanen.");
        }
    }

    public void Plant(BenihItem benih)
    {
        benihItem = JamuSystem.Instance.GetBenih(benih.itemName);
        if (benihItem == null)
        {
            Debug.LogError("BenihItem tidak ditemukan di JamuSystem: " + benih.itemName);
            return;
        }

        isPlanted = true;
        currentStage = 0;
        timer = 0f;
        if (spriteRenderer != null && benihItem.growthStages.Length > 0)
            spriteRenderer.sprite = benihItem.growthStages[currentStage];

        StartCoroutine(Grow());
    }

    IEnumerator Grow()
    {
        while (currentStage < benihItem.growthStages.Length - 1)
        {
            yield return new WaitForSeconds(benihItem.growthTime);
            currentStage++;
            spriteRenderer.sprite = benihItem.growthStages[currentStage];
        }
    }

    public void Harvest()
    {
        if (!isPlanted || benihItem == null || currentStage < benihItem.growthStages.Length - 1)
            return;

        BahanItem hasil = benihItem.producesBahan;

        if (hasil != null)
        {
            BahanItem bahanDatabase = JamuSystem.Instance.GetBahan(hasil.itemName);
            if (bahanDatabase != null)
            {
                // Tambahkan hasil panen ke inventory lewat GameManager.instance.gameData
                var data = GameManager.instance.gameData;
                bool ditambahkan = false;

                for (int i = 0; i < data.barang.Count; i++)
                {
                    var item = data.barang[i];
                    if (item != null && item.nama == bahanDatabase.itemName)
                    {
                        item.jumlah++;
                        ditambahkan = true;
                        break;
                    }
                }
                if (!ditambahkan)
                {
                    // Tambahkan ke slot kosong
                    for (int i = 0; i < data.barang.Count; i++)
                    {
                        if (data.barang[i] == null || string.IsNullOrEmpty(data.barang[i].nama))
                        {
                            data.barang[i] = new Item
                            {
                                nama = bahanDatabase.itemName,
                                gambar = bahanDatabase.itemSprite,
                                harga = bahanDatabase.itemValue,
                                jumlah = 1
                            };
                            ditambahkan = true;
                            break;
                        }
                    }
                }
                GameManager.instance.SaveGameData();

                Inventory.Instance.RefreshInventory();

                if (TaskManager.Instance != null)
                    TaskManager.Instance.OnHarvested(bahanDatabase.itemName);

                if (AlmanacSystem.Instance != null)
                {
                    AlmanacSystem.Instance.AddItemToAlmanac(bahanDatabase.itemName);
                }

                Debug.Log($"Panen berhasil: {bahanDatabase.itemName} masuk ke inventory dan disimpan ke GameManager.");
            }
            else
            {
                Debug.LogError($"Bahan dengan nama {hasil.itemName} tidak ditemukan di database JamuSystem.");
            }
        }
        else
        {
            Debug.LogWarning("Benih ini tidak menghasilkan bahan saat dipanen.");
        }

        ResetSoil();
    }

    public void ResetSoil()
    {
        isPlanted = false;
        benihItem = null;
        currentStage = -1;
        timer = 0f;
        spriteRenderer.sprite = null;
    }
}