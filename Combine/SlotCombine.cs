using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SlotCombine : MonoBehaviour, IDropHandler
{
    public int slotIndex;
    private ICraftingPanel craftingPanel;
    private Image currentImage;
    private BahanItem currentBahan;
    private static bool isGlobalDropLocked = false;

    void Start()
    {
        craftingPanel = PanelDetector.FindCraftingPanel(gameObject);
    }

    private void UpdateVisualState()
    {
        Transform slotForCombine = transform.Find("Slot_For_Combine");
        if (slotForCombine == null && transform.parent != null)
            slotForCombine = transform.parent.Find("Slot_For_Combine");

        if (slotForCombine != null)
        {
            Image slotImage = slotForCombine.GetComponent<Image>();
            if (slotImage != null)
            {
                if (currentBahan != null)
                {
                    slotImage.sprite = currentBahan.itemSprite;
                    slotImage.color = Color.white;
                }
                else
                {
                    slotImage.sprite = null;
                    slotImage.color = new Color(1f, 1f, 1f, 0f);
                }
            }
        }
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (isGlobalDropLocked)
        {
            Debug.Log("[SlotCombine] Drop blocked - Global lock active");
            return;
        }

        GameObject draggedObject = eventData.pointerDrag;
        if (draggedObject == null) return;

        SlotBahan draggedBahan = draggedObject.GetComponent<SlotBahan>();
        if (draggedBahan == null) return;

        // TAMBAHAN: Cek quantity dari data game langsung
        var dtg = GameManager.instance.gameData;
        if (dtg == null)
        {
            Debug.Log("[SlotCombine] Drop blocked - No data game");
            return;
        }

        // Cari index inventory dari SlotBahan yang di-drag
        BahanItem bahan = draggedBahan.GetBahan();
        if (bahan == null)
        {
            Debug.Log("[SlotCombine] Drop blocked - No bahan data");
            return;
        }

        // Cari quantity real dari inventory
        int inventoryIndex = -1;
        for (int i = 0; i < dtg.barang.Count; i++)
        {
            if (dtg.barang[i] != null && dtg.barang[i].nama == bahan.itemName)
            {
                inventoryIndex = i;
                break;
            }
        }

        if (inventoryIndex < 0 || dtg.barang[inventoryIndex].jumlah <= 0)
        {
            Debug.Log("[SlotCombine] Drop blocked - Item quantity is 0 or not found. Index: " + inventoryIndex +
                      ", Quantity: " + (inventoryIndex >= 0 ? dtg.barang[inventoryIndex].jumlah : -1));
            return;
        }

        Transform slotForCombine = transform.Find("Slot_For_Combine");
        if (slotForCombine == null) return;

        // Jangan terima jika sudah ada isi
        if (slotForCombine.childCount > 0) return;

        // Set bahan ke slot combine
        SetBahan(bahan);

        draggedBahan.ReduceVisualQuantityForCombine();
        draggedBahan.UpdateQuantityDisplay();

        // Buat visual clone untuk slot combine
        GameObject visual = new GameObject("VisualClone");
        Image image = visual.AddComponent<Image>();
        image.sprite = bahan.itemSprite;

        // Set alpha penuh untuk visual di slot combine
        Color visualColor = image.color;
        visualColor.a = 1f;
        image.color = visualColor;

        visual.transform.SetParent(slotForCombine, false);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localScale = Vector3.one;

        // Tambahkan CanvasGroup agar bisa di-drag
        CanvasGroup cg = visual.AddComponent<CanvasGroup>();
        cg.blocksRaycasts = true;
        cg.alpha = 1f; // Alpha penuh

        // Tambahkan komponen drag balik dengan pengaturan skala
        var returnComponent = visual.AddComponent<ReturnToInventoryOnDrop>();
        returnComponent.originalBahan = draggedBahan;
        returnComponent.dragLayer = draggedBahan.dragLayer;

        SlotBahan sb = visual.GetComponent<SlotBahan>();
        if (sb != null)
        {
            sb.LockDragging(true); // 👈 Kunci agar clone tidak bisa didrag ulang
        }

        // Update slot bahan asli
        draggedBahan.droppedOnValidSlot = true;
        draggedBahan.OnItemPlacedToSlotCombine();
        draggedBahan.UpdateDraggableState();
        draggedBahan.UpdateVisualState(false); // Reset ke normal state
        draggedBahan.ReturnToOriginalPosition();

        // Update tampilan SlotCombine
        UpdateVisualState();
        SetSlotForCombineAlpha(0f);
    }

    private void SetSlotForCombineAlpha(float alpha)
    {
        Transform slotForCombine = transform.Find("Slot_For_Combine");
        if (slotForCombine != null)
        {
            Image img = slotForCombine.GetComponent<Image>();
            if (img != null)
            {
                Color c = img.color;
                c.a = alpha;
                img.color = c;
            }
        }
    }

    public void ClearIfEmpty()
    {
        Transform slotForCombine = transform.Find("Slot_For_Combine");
        if (slotForCombine != null && slotForCombine.childCount == 0)
        {
            currentBahan = null;
            if (currentImage != null)
            {
                currentImage.sprite = null;
                currentImage.color = new Color(1f, 1f, 1f, 0f);
            }
        }
    }
    public static void SetGlobalDropLock(bool locked)
    {
        isGlobalDropLocked = locked;
        Debug.Log("[SlotCombine] Global drop lock set to: " + locked);
    }

    public void SetCurrentImage(Image image)
    {
        currentImage = image;
    }
    public Image GetCurrentImage()
    {
        return currentImage;
    }
    public void SetBahan(BahanItem bahan)
    {
        currentBahan = bahan;
        UpdateVisualState();
    }
    public BahanItem GetBahan()
    {
        return currentBahan;
    }
    public void ClearSlot()
    {
        currentBahan = null;
        currentImage = null;
        Transform slotForCombine = transform.Find("Slot_For_Combine");
        if (slotForCombine != null)
        {
            Image img = slotForCombine.GetComponent<Image>();
            if (img != null)
            {
                img.sprite = null;
                img.color = new Color(1f, 1f, 1f, 0f);
            }
            foreach (Transform child in slotForCombine)
                Destroy(child.gameObject);
        }
    }
}