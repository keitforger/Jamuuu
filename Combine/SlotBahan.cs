using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SlotBahan : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    private ICraftingPanel craftingPanel;
    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;
    private Canvas rootCanvas;
    private Camera canvasCamera;
    private Transform originalParent;
    private Vector2 originalPosition;
    private float originalScale;
    public bool droppedOnValidSlot = false;
    [SerializeField] public Transform dragLayer;
    private int inventoryIndex = -1;
    private Text quantityText;
    private Image slotImage;
    private CanvasGroup slotCanvasGroup;
    private BahanItem currentBahan;
    // For drag cloning
    private GameObject dragClone;
    private CanvasGroup dragCloneCanvasGroup;
    private Image dragCloneImage;
    private Transform lastValidParent;
    private Vector2 lastValidPosition;
    private float lastValidScale;
    // Add these variables at the top of the SlotBahan class
    private bool isDragLocked = false;
    private bool isCombining = false;
    private bool isDraggable = true;
    private int currentQuantity = 0;
    private float normalAlpha = 1f;
    private float transparentAlpha = 0.3f;
    private static bool isGlobalDragLocked = false;
    private bool isCurrentlyDragging = false;
    private int visualQuantity = -1;
    [HideInInspector] public bool isDragClone = false;


    void Awake()
    {
        if (isDragClone) return;
        if (gameObject.name.Contains("(Clone)"))
            return;

        Transform parent = transform.parent;
        if (parent != null)
        {
            Transform textSibling = parent.Find("Text");
            if (textSibling != null)
                quantityText = textSibling.GetComponent<Text>();
        }
        if (quantityText == null)
            Debug.LogError("quantityText NOT FOUND for " + gameObject.name);
        else
            Debug.Log("quantityText FOUND for " + gameObject.name + " : " + quantityText.text);
        rectTransform = GetComponent<RectTransform>();
        originalParent = transform.parent;
        originalPosition = rectTransform.localPosition;
        originalScale = transform.localScale.x;
        rootCanvas = GetComponentInParent<Canvas>();
        while (rootCanvas != null && rootCanvas.transform.parent != null && rootCanvas.transform.parent.GetComponentInParent<Canvas>() != null)
            rootCanvas = rootCanvas.transform.parent.GetComponentInParent<Canvas>();
        canvasCamera = rootCanvas != null ? rootCanvas.worldCamera : null;
    }

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
        slotImage = GetComponent<Image>();
        slotCanvasGroup = GetComponent<CanvasGroup>();
        if (slotCanvasGroup == null)
            slotCanvasGroup = gameObject.AddComponent<CanvasGroup>();
        craftingPanel = PanelDetector.FindCraftingPanel(gameObject);
        FindInventoryIndex();
        UpdateQuantityDisplay();
    }

    private void FindInventoryIndex()
    {
        if (currentBahan == null) return;
        var dtg = GameManager.instance.gameData;
        if (dtg == null) return;
        inventoryIndex = -1;
        for (int i = 0; i < dtg.barang.Count; i++)
        {
            if (dtg.barang[i] != null && dtg.barang[i].nama == currentBahan.itemName)
            {
                inventoryIndex = i;
                break;
            }
        }
    }

    public void UpdateDraggableState()
    {
        var dtg = GameManager.instance.gameData;
        if (dtg != null && inventoryIndex >= 0)
        {
            currentQuantity = dtg.barang[inventoryIndex].jumlah;
            isDraggable = GetDisplayQuantity() > 0 && !isDragLocked;

            if (currentQuantity <= 0 && isCurrentlyDragging)
            {
                CancelActiveDrag();
            }
            UpdateVisualState(false);
        }
    }

    public void UpdateVisualState(bool isDragging)
    {
        if (slotImage != null)
        {
            Color c = slotImage.color;
            if (GetDisplayQuantity() <= 0)
            {
                c.a = transparentAlpha;
                isDraggable = false;
            }
            else if (isDragging)
            {
                c.a = transparentAlpha;
            }
            else
            {
                c.a = normalAlpha;
            }
            slotImage.color = c;
        }
    }

    private int GetDisplayQuantity()
    {
        if (visualQuantity >= 0) return visualQuantity;
        var dtg = GameManager.instance.gameData;
        if (dtg != null && inventoryIndex >= 0 && inventoryIndex < dtg.barang.Count)
            return dtg.barang[inventoryIndex].jumlah;
        return 0;
    }

    public void ReduceVisualQuantityForCombine()
    {
        if (visualQuantity < 0)
            visualQuantity = GetDisplayQuantity();
        if (visualQuantity > 0)
            visualQuantity--;
        UpdateQuantityDisplay();
    }

    public void ResetVisualQuantity()
    {
        visualQuantity = -1;
        UpdateQuantityDisplay();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        var dtg = GameManager.instance.gameData;
        if (dtg != null && inventoryIndex >= 0 && inventoryIndex < dtg.barang.Count)
        {
            currentQuantity = dtg.barang[inventoryIndex].jumlah;
        }
        else
        {
            currentQuantity = 0;
        }

        if (isGlobalDragLocked || !isDraggable || currentQuantity <= 0)
        {
            Debug.Log("[SlotBahan] Drag blocked - Global lock: " + isGlobalDragLocked);
            return;
        }

        if (dtg == null || inventoryIndex < 0 || dtg.barang[inventoryIndex].jumlah <= 0)
            return;

        isCurrentlyDragging = true;
        UpdateVisualState(true);

        // Buat dragClone dengan alpha penuh
        if (dragClone != null) Destroy(dragClone);

        // Tentukan parent untuk drag clone - gunakan dragLayer jika ada
        Transform dragParent = dragLayer != null ? dragLayer : rootCanvas.transform;
        dragClone = Instantiate(gameObject, dragParent);
        dragClone.GetComponent<SlotBahan>().isDragClone = true;

        var cloneText = dragClone.transform.parent.Find("Text");
        if (cloneText != null)
            cloneText.gameObject.SetActive(false);


        // Setup dragClone dengan alpha penuh
        dragCloneCanvasGroup = dragClone.GetComponent<CanvasGroup>();
        if (dragCloneCanvasGroup == null)
            dragCloneCanvasGroup = dragClone.AddComponent<CanvasGroup>();
        dragCloneCanvasGroup.blocksRaycasts = false;
        dragCloneCanvasGroup.alpha = normalAlpha; // Alpha penuh untuk clone

        // Set alpha penuh untuk image di drag clone
        Image dragCloneImg = dragClone.GetComponent<Image>();
        if (dragCloneImg != null)
        {
            Color cloneColor = dragCloneImg.color;
            cloneColor.a = normalAlpha;
            dragCloneImg.color = cloneColor;
        }

        // Adjust scale untuk drag clone
        AdjustDragCloneScale();

        originalParent = transform.parent;
        originalPosition = rectTransform.localPosition;
        originalScale = transform.localScale.x;
        canvasGroup.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        var dtg =   GameManager.instance.gameData;
        if (dtg != null && inventoryIndex >= 0 && inventoryIndex < dtg.barang.Count)
        {
            int realTimeQuantity = dtg.barang[inventoryIndex].jumlah;
            if (realTimeQuantity <= 0)
            {
                Debug.Log("[SlotBahan] Canceling drag - quantity became 0 during drag");
                CancelActiveDrag();
                return;
            }
        }

        if (dragClone != null)
        {
            RectTransform dragCloneRect = dragClone.GetComponent<RectTransform>();
            Vector3 currentPointerWorldPosition;
            if (canvasCamera != null && RectTransformUtility.ScreenPointToWorldPointInRectangle(
                    dragCloneRect,
                    eventData.position,
                    canvasCamera,
                    out currentPointerWorldPosition))
            {
                dragCloneRect.position = currentPointerWorldPosition;
            }
            else
            {
                dragCloneRect.position = Input.mousePosition;
            }
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // Hapus drag clone
        if (dragClone != null)
        {
            Destroy(dragClone);
            dragClone = null;
        }

        // Reset visual state - kembalikan alpha ke normal
        UpdateVisualState(false);
        canvasGroup.blocksRaycasts = true;

        var dtg = GameManager.instance.gameData;
        if (!droppedOnValidSlot && dtg != null && inventoryIndex >= 0)
        {
            // Item dikembalikan ke inventory jika tidak di-drop ke slot yang valid
            // (tidak perlu menambah quantity karena tidak dikurangi saat drag)
            UpdateQuantityDisplay();
        }

        ReturnToOriginalPosition();
        droppedOnValidSlot = false;
        isCurrentlyDragging = false;
    }

    public void OnItemReturnedFromSlotCombine()
    {
        var dtg = GameManager.instance.gameData;
        if (dtg == null || inventoryIndex < 0) return;

        dtg.barang[inventoryIndex].jumlah++;
        GameManager.instance.SaveGameData();

        UpdateQuantityDisplay();
        UpdateDraggableState();
    }

    public void LockDragging(bool locked)
    {
        isDragLocked = locked;
        Debug.Log("SlotBahan " + gameObject.name + " drag lock: " + locked);
        SetDraggable(!locked);
    }
    public void SetDraggable(bool state)
    {
        isDraggable = state;
        if (slotCanvasGroup != null)
        {
            slotCanvasGroup.interactable = state;
            slotCanvasGroup.blocksRaycasts = state;
        }
    }

    // Drop handler: menerima drop dari clone di slot combine ATAU dari draglayer
    public void OnDrop(PointerEventData eventData)
    {
        GameObject draggedObject = eventData.pointerDrag;
        if (draggedObject == null) return;

        SlotBahan draggedSlotBahan = draggedObject.GetComponent<SlotBahan>();
        if (draggedSlotBahan == null || draggedSlotBahan == this) return;

        var parentName = draggedObject.transform.parent?.name ?? "";
        Debug.Log($"Dropped from: {parentName}"); // tambahkan ini
        if (draggedObject.GetComponent<SlotBahan>() == null) return;

        // Handle item return to inventory
        var dtg = GameManager.instance.gameData;
        if (dtg != null && inventoryIndex >= 0)
        {
            dtg.barang[inventoryIndex].jumlah += 1;
            GameManager.instance.SaveGameData();
            UpdateQuantityDisplay();
            UpdateDraggableState();
        }

        // Destroy dragged clone
        Destroy(draggedObject);
    }

    public void ReduceQuantityForCombine()
    {
        var dtg = GameManager.instance.gameData;
        if (dtg == null || inventoryIndex < 0) return;

        dtg.barang[inventoryIndex].jumlah--;
        if (dtg.barang[inventoryIndex].jumlah < 0)
            dtg.barang[inventoryIndex].jumlah = 0;

        GameManager.instance.SaveGameData();

        UpdateQuantityDisplay();
    }

    public void PreviewReduceQuantityForCombine()
    {
        // Dipanggil hanya untuk update visual tanpa sentuh data utama
        if (visualQuantity > 0)
            visualQuantity--;
        UpdateQuantityDisplay();
    }

    public void OnItemPlacedToSlotCombine()
    {
        UpdateQuantityDisplay();
        UpdateVisualState(false);
    }

    public void UpdateQuantityDisplay()
    {
        int display = GetDisplayQuantity();
        if (quantityText != null)
            quantityText.text = display.ToString();

        if (display > 0)
        {
            ShowSlot();
            SetDraggable(!isDragLocked);
            isDraggable = true;
        }
        else
        {
            HideSlot();
            SetDraggable(false);
            isDraggable = false;
        }

        UpdateVisualState(false);
    }

    public void IncreaseVisualQuantityForCombine()
    {
        if (visualQuantity < 0)
            visualQuantity = GetDisplayQuantity();
        visualQuantity++;
        UpdateQuantityDisplay();
    }

    public void HideSlot()
    {
        if (slotCanvasGroup != null)
        {
            slotCanvasGroup.alpha = 1f; // Canvas group tetap alpha 1
            slotCanvasGroup.interactable = false;
            slotCanvasGroup.blocksRaycasts = false;
        }
        if (slotImage != null)
        {
            var tempColor = slotImage.color;
            tempColor.a = transparentAlpha; // Image jadi transparan
            slotImage.color = tempColor;
        }
        isDraggable = false;
    }

    public void ShowSlot()
    {
        if (slotCanvasGroup != null)
        {
            slotCanvasGroup.alpha = 1f;
            slotCanvasGroup.interactable = true;
            slotCanvasGroup.blocksRaycasts = true;
        }
        if (slotImage != null)
        {
            var tempColor = slotImage.color;
            tempColor.a = normalAlpha; // Image dengan alpha penuh
            slotImage.color = tempColor;
        }
        isDraggable = true;
    }

    private float GetCurrentPanelScaleFactor()
    {
        // Cari CombinePanel (scale 0.51) atau NPCCraftingPanel (scale 1.0)
        Transform current = transform.parent;
        while (current != null)
        {
            if (current.name.Contains("CombinePanel") || current.name.Contains("PanelCombine"))
            {
                return current.localScale.x; // Biasanya 0.51
            }
            if (current.name.Contains("NPCCraftingPanel") || current.name.Contains("CraftingPanel"))
            {
                return current.localScale.x; // Biasanya 1.0
            }
            current = current.parent;
        }
        return 1f; // Default scale
    }

    private void AdjustDragCloneScale()
{
    if (dragClone == null) return;
    
    float currentPanelScale = GetCurrentPanelScaleFactor();
    float targetScale = 1f; // Target scale untuk drag layer (normal size)
    float scaleFactor = targetScale / currentPanelScale;
    
    Debug.Log($"[AdjustDragCloneScale] Current panel scale: {currentPanelScale}, Target: {targetScale}, Factor: {scaleFactor}");
    
    dragClone.transform.localScale = new Vector3(
        dragClone.transform.localScale.x * scaleFactor,
        dragClone.transform.localScale.y * scaleFactor,
        dragClone.transform.localScale.z
    );
}

    public void StoreCurrentAsLastValid()
    {
        lastValidParent = transform.parent;
        lastValidPosition = rectTransform.localPosition;
        lastValidScale = transform.localScale.x;
    }

    public static void SetGlobalDragLock(bool locked)
    {
        isGlobalDragLocked = locked;
        Debug.Log("[SlotBahan] Global drag lock set to: " + locked);
    }

    public void CancelActiveDrag()
    {
        if (isCurrentlyDragging && dragClone != null)
        {
            // Hapus drag clone
            Destroy(dragClone);
            dragClone = null;

            // Reset visual state
            UpdateVisualState(false);
            canvasGroup.blocksRaycasts = true;
            isCurrentlyDragging = false;

            // Kembalikan ke posisi semula
            ReturnToOriginalPosition();
        }
    }

    public void ReturnToOriginalPosition()
    {
        if (originalParent != null)
        {
            transform.SetParent(originalParent);
            rectTransform.localPosition = originalPosition;
            transform.localScale = new Vector3(originalScale, originalScale, 1f);
        }
    }
    public void SetBahan(BahanItem bahan)
    {
        if (currentBahan == bahan) return;
        currentBahan = bahan;
        FindInventoryIndex();

    }
    public BahanItem GetBahan()
    {
        return currentBahan;
    }
}