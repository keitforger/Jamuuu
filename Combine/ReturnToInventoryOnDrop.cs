using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ReturnToInventoryOnDrop : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public SlotBahan originalBahan;
    private RectTransform rectTransform;
    private Canvas canvas;
    private CanvasGroup canvasGroup;
    private Transform originalParent;
    private Vector3 originalLocalPosition;
    private Vector3 originalLocalScale;
    public Transform dragLayer;
    private static bool isGlobalDragLocked = false;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        canvas = GetComponentInParent<Canvas>();

        // Store original transform data
        originalParent = transform.parent;
        originalLocalPosition = rectTransform.localPosition;
        originalLocalScale = transform.localScale;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (isGlobalDragLocked) return;
        canvasGroup.blocksRaycasts = false;

        // Pindahkan ke dragLayer agar posisi mouse sesuai (penting!)
        if (dragLayer != null)
        {
            transform.SetParent(dragLayer);
        }

        Transform slotForCombine = originalBahan.transform.parent?.Find("Slot_For_Combine");
        if (slotForCombine != null)
        {
            Image img = slotForCombine.GetComponent<Image>();
            if (img != null)
            {
                Color c = img.color;
                c.a = 0.3f; // Transparan saat item sedang didrag keluar
                img.color = c;
            }
        }

    }


    public void OnDrag(PointerEventData eventData)
    {
        if (isGlobalDragLocked) return;
        Vector2 pos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.transform as RectTransform,
            eventData.position,
            canvas.worldCamera,
            out pos);
        rectTransform.localPosition = pos;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (isGlobalDragLocked) return;
        if (canvasGroup != null)
            canvasGroup.blocksRaycasts = true;

        GameObject target = eventData.pointerCurrentRaycast.gameObject;
        SlotBahan targetBahan = target?.GetComponent<SlotBahan>();

        if (targetBahan != null && targetBahan == originalBahan)
        {
            // Item dikembalikan ke slot bahan asli

            // Handle scale adjustment jika diperlukan
            ICraftingPanel craftingPanel = PanelDetector.FindCraftingPanel(gameObject);
            if (craftingPanel != null && craftingPanel.NeedsScaleAdjustment)
            {
                // Adjust scale dari SlotCombine ke SlotBahan
                Transform sourcePanel = transform.GetComponentInParent<Canvas>().transform;
                Transform destPanel = targetBahan.transform.GetComponentInParent<Canvas>().transform;

                if (sourcePanel != destPanel)
                {
                    PanelScalingUtils.AdjustScaleForPanel(gameObject, sourcePanel, destPanel);
                }
            }

            originalBahan.IncreaseVisualQuantityForCombine();
            originalBahan.UpdateQuantityDisplay();

            Transform slotForCombine = originalBahan.transform.parent?.Find("Slot_For_Combine");
            if (slotForCombine != null)
            {
                Image img = slotForCombine.GetComponent<Image>();
                if (img != null)
                {
                    Color c = img.color;
                    c.a = 0f;
                    img.color = c;
                }
            }

            // Hancurkan visual clone
            Destroy(gameObject);


            Transform parentSlot = transform.parent; // Slot_For_Combine
            if (parentSlot != null && parentSlot.parent != null)
            {
                SlotCombine slotCombine = parentSlot.parent.GetComponent<SlotCombine>();
                if (slotCombine != null)
                {
                    slotCombine.ClearIfEmpty(); // ✅ panggil di sini
                }
            }

            // Hapus bahan dari slot combine
            SlotCombine parentSlotCombine = GetComponentInParent<SlotCombine>();
            if (parentSlotCombine != null)
            {
                parentSlotCombine.SetBahan(null); // Hapus referensi bahan
            }


        }
        else
        {
            // Salah drop — kembalikan ke posisi semula di slot combine
            transform.SetParent(originalParent);
            rectTransform.localPosition = originalLocalPosition;
            transform.localScale = originalLocalScale;
        }
    }

    public static void SetGlobalDragLock(bool locked)
    {
        isGlobalDragLocked = locked;
    }
}
