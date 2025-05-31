using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System;
using System.Collections;
public enum CraftingPanelType
    {
    CombinePanel,
    NPCCraftingPanel
    }
// Unified panel that can function as either a CombinePanel or NPCCraftingPanel
public class CraftingManager : MonoBehaviour, ICraftingPanel
{
    [Header("Panel Configuration")]
    public CraftingPanelType panelType = CraftingPanelType.CombinePanel;
    [Header("Common UI Elements")]
    public GameObject[] slotPanelCombine;
    [SerializeField] private Button buatJamuButton;
    [SerializeField] private Image hasilJamuImage;
    [SerializeField] private Text namaJamuText;
    [Header("Bahan and Slots")]
    [SerializeField] private GameObject slotBahanPrefab;
    [SerializeField] private Transform slotBahanContainer;
    [SerializeField] private Transform slotCombineContainer;
    [Header("Scroll Settings")]
    [SerializeField] private ScrollRect slotBahanScrollView;
    [SerializeField] private float dragThreshold = 10f;
    [SerializeField] private float scrollInactiveTime = 0.2f;
    private bool isScrolling = false;
    private Vector2 lastScrollPosition;
    private Coroutine checkScrollCoroutine;
    [Header("NPC Mode Only")]
    [SerializeField] private Button kasihNPCButton;
    [SerializeField] private Button closePanelButton;
    [Header("Crafting Panels")]
    [SerializeField] private RectTransform combinePanel;
    [SerializeField] private RectTransform npcCraftingPanel;
    [Header("Slot References")]
    [SerializeField] private List<RectTransform> combineSlots = new List<RectTransform>();
    [SerializeField] private List<RectTransform> npcCraftingSlots = new List<RectTransform>();
    [Header("Debugging")]
    [SerializeField] private bool debugMode = false;
    private Canvas combinePanelCanvas;
    private Canvas npcCraftingPanelCanvas;
    private JamuCraftingIntegration jamuIntegration;
    private JamuNPC currentNPC;
    private int selectedBahanIndex = -1;
    private List<string> bahanDipakai = new List<string>();
    private List<ResepJamu> daftarResep;
    private List<int> bahanItemIndices = new List<int>();
    private List<GameObject> bahanSlots = new List<GameObject>();

    private GameObject selectedBahan = null;
    private ResepJamu currentCraftedJamu = null;
    private JamuGagal currentJamuGagal = null;
    [SerializeField] public Transform dragLayer;

    private bool isDraggingLocked = false;
    public Transform DragLayer
 {
 get => dragLayer;
 set => dragLayer = value;
 }
 public bool NeedsScaleAdjustment => panelType == CraftingPanelType.CombinePanel;
 private void Awake()
 {
 FindCanvasReferences();
 }
 void Start()
 {
 SetupScrollHandling();
 if (buatJamuButton != null)
 {
 buatJamuButton.onClick.AddListener(BuatJamu);
 }
 if (panelType == CraftingPanelType.CombinePanel)
 {
 InitializeAsCombinePanel();
 }
 else
 {
 InitializeAsNPCCraftingPanel();
 }
 InitializeSlots();
 LoadRecipes();
 foreach (GameObject slot in slotPanelCombine)
 {
 Transform slotForCombine = slot.transform.Find("Slot_For_Combine");
 if (slotForCombine != null)
 {
 Image img = slotForCombine.GetComponent<Image>();
 if (img != null)
 {
 img.sprite = null;
 img.color = new Color(1f, 1f, 1f, 0f);
 }
 }
 else
 {
 Image img = slot.transform.GetChild(0).GetComponent<Image>();
 if (img != null)
 {
 img.sprite = null;
 img.color = new Color(1f, 1f, 1f, 0f);
 }
 }
 }
 if (debugMode)
 {
 LogPanelInformation();
 }
 }
 private void SetupScrollHandling()
 {
 if (slotBahanScrollView != null)
 {
 slotBahanScrollView.onValueChanged.AddListener(OnScrollChanged);
 FixScrollViewContentVisibility();
 }
 }
 private void OnScrollChanged(Vector2 scrollPos)
 {
 if (!isScrolling)
 {
 isScrolling = true;
 SetSlotDragEnabled(false);
 }
 lastScrollPosition = scrollPos;
 if (checkScrollCoroutine != null)
 {
 StopCoroutine(checkScrollCoroutine);
 }
 checkScrollCoroutine = StartCoroutine(CheckScrollStopped());
 }
 private IEnumerator CheckScrollStopped()
 {
 Vector2 prevPosition = lastScrollPosition;
 yield return new WaitForSeconds(scrollInactiveTime);
 if (Vector2.Distance(prevPosition, lastScrollPosition) < dragThreshold)
 {
 isScrolling = false;
 SetSlotDragEnabled(true);
 }
 }
 private void SetSlotDragEnabled(bool enabled)
 {
 foreach (GameObject slot in bahanSlots)
 {
 if (slot != null)
 {
 SlotBahan slotBahan = slot.GetComponentInChildren<SlotBahan>();
 if (slotBahan != null)
 {
 slotBahan.enabled = enabled;
 }
 }
 }
 }
 private void FixScrollViewContentVisibility()
 {
 if (slotBahanScrollView == null || slotBahanScrollView.content == null) return;
 Canvas.ForceUpdateCanvases();
 LayoutRebuilder.ForceRebuildLayoutImmediate(slotBahanScrollView.content);
 ContentSizeFitter sizeFitter = slotBahanScrollView.content.GetComponent<ContentSizeFitter>();
 if (sizeFitter != null)
 {
 sizeFitter.SetLayoutVertical();
 sizeFitter.SetLayoutHorizontal();
 }
 VerticalLayoutGroup vertLayout =
slotBahanScrollView.content.GetComponent<VerticalLayoutGroup>();
 if (vertLayout != null)
 {
 vertLayout.CalculateLayoutInputVertical();
 vertLayout.SetLayoutVertical();
 }
 slotBahanScrollView.normalizedPosition = new Vector2(0, 1);
 }
 private void FindCanvasReferences()
 {
 if (combinePanel != null)
 {
 combinePanelCanvas = PanelScalingUtils.FindRootCanvas(combinePanel);
 }
 if (npcCraftingPanel != null)
 {
 npcCraftingPanelCanvas = PanelScalingUtils.FindRootCanvas(npcCraftingPanel);
 }
 }
 private void LogPanelInformation()
 {
 if (combinePanelCanvas != null)
 {
 Debug.Log($"CombinePanel Canvas: {combinePanelCanvas.name}, Render Mode:{combinePanelCanvas.renderMode}");
 }
 else
 {
 Debug.LogWarning("CombinePanel Canvas not found!");
 }
 if (npcCraftingPanelCanvas != null)
 {
 Debug.Log($"NPCCraftingPanel Canvas: {npcCraftingPanelCanvas.name}, Render Mode:{npcCraftingPanelCanvas.renderMode}");
 }
 else
 {
 Debug.LogWarning("NPCCraftingPanel Canvas not found!");
 }
 }
 public void MoveItemBetweenPanels(GameObject craftingItem, RectTransform sourceSlot,
RectTransform destinationSlot)
 {
 if (craftingItem == null || sourceSlot == null || destinationSlot == null)
 {
 Debug.LogError("MoveItemBetweenPanels: Missing parameters!");
 return;
 }
 SlotBahan draggedBahan = craftingItem.GetComponent<SlotBahan>();
 if (draggedBahan == null) return;
 SlotCombine targetSlot = destinationSlot.GetComponent<SlotCombine>();
 if (targetSlot == null) return;
 targetSlot.SetBahan(draggedBahan.GetBahan());
 draggedBahan.StoreCurrentAsLastValid();
 Transform originalParent = craftingItem.transform.parent;
 Vector2 worldPos = craftingItem.transform.position;
 craftingItem.transform.SetParent(destinationSlot);
 craftingItem.transform.position = new Vector2(worldPos.x, worldPos.y);
 craftingItem.transform.localPosition = Vector2.zero;
 AdjustItemScaling(craftingItem, originalParent, destinationSlot);
 if (debugMode)
 {
 Debug.Log($"Moved {craftingItem.name} from {sourceSlot.name} to {destinationSlot.name}");
 }
 }
 private void AdjustItemScaling(GameObject item, Transform sourceParent, Transform
destinationParent)
 {
 if (item == null || sourceParent == null || destinationParent == null)
 {
 Debug.LogWarning("AdjustItemScaling: Missing parameters!");
 return;
 }
 float scaleFactor = PanelScalingUtils.CalculateScaleFactor(sourceParent, destinationParent);
 item.transform.localScale = new Vector3(
 item.transform.localScale.x * scaleFactor,
 item.transform.localScale.y * scaleFactor,
 item.transform.localScale.z
 );
 if (debugMode)
 {
 Debug.Log($"[AdjustItemScaling] Applied scaleFactor = {scaleFactor} from {sourceParent.name} to {destinationParent.name}");
 }
 }
 public RectTransform GetCombineSlot(int index)
 {
 if (index >= 0 && index < combineSlots.Count)
 {
 return combineSlots[index];
 }
 return null;
 }
 public RectTransform GetNPCCraftingSlot(int index)
 {
 if (index >= 0 && index < npcCraftingSlots.Count)
 {
 return npcCraftingSlots[index];
 }
 return null;
 }
 public void TransferToCombinePanel(GameObject craftingItem, int sourceSlotIndex, int destSlotIndex)
 {
 RectTransform sourceSlot = GetNPCCraftingSlot(sourceSlotIndex);
 RectTransform destSlot = GetCombineSlot(destSlotIndex);
 if (sourceSlot != null && destSlot != null)
 {
 MoveItemBetweenPanels(craftingItem, sourceSlot, destSlot);
 }
 else
 {
 Debug.LogError($"Cannot find slots: sourceIndex={sourceSlotIndex}, destIndex={destSlotIndex}");
 }
 }
 public void TransferToNPCCraftingPanel(GameObject craftingItem, int sourceSlotIndex, int
destSlotIndex)
 {
 RectTransform sourceSlot = GetCombineSlot(sourceSlotIndex);
 RectTransform destSlot = GetNPCCraftingSlot(destSlotIndex);
 if (sourceSlot != null && destSlot != null)
 {
 MoveItemBetweenPanels(craftingItem, sourceSlot, destSlot);
 }
 else
 {
 Debug.LogError($"Cannot find slots: sourceIndex={sourceSlotIndex}, destIndex={destSlotIndex}");
 }
 }
 void OnEnable()
 {
        if (JamuSystem.Instance != null && JamuSystem.Instance.jamuDatabase != null)
        {
            TampilkanDariInventory();
        }
        try
 {
 TampilkanDariInventory();
 FixScrollViewContentVisibility();
 isScrolling = false;
 SetSlotDragEnabled(true);
 ResetSlotCombine();
 if (hasilJamuImage != null)
 {
 hasilJamuImage.gameObject.SetActive(false);
 }
 if (namaJamuText != null)
 {
 namaJamuText.gameObject.SetActive(false);
 }
            if (panelType == CraftingPanelType.NPCCraftingPanel)
            {
                ResetNPCCraftingPanel();
                transform.localScale = Vector3.one;
            }
            else
            {
                ResetSlotCombine();
            }
        }
 catch (System.Exception e)
 {
 Debug.LogError($"Error in OnEnable: {e.Message}\n{e.StackTrace}");
 }
 }
 private void InitializeAsCombinePanel()
 {
 jamuIntegration = JamuCraftingIntegration.Instance;
 if (jamuIntegration != null)
 {
 jamuIntegration.RegisterCraftingPanel(this);
 }
 if (kasihNPCButton != null)
 kasihNPCButton.gameObject.SetActive(false);
 if (closePanelButton != null)
 closePanelButton.gameObject.SetActive(false);
 }
 private void InitializeAsNPCCraftingPanel()
 {
 if (kasihNPCButton != null)
 kasihNPCButton.onClick.AddListener(KasihJamuKeNPC);
 if (closePanelButton != null)
 closePanelButton.onClick.AddListener(ClosePanel);
 gameObject.SetActive(false);
 }
 // Method to connect with NPC (only used in NPC mode)
 public void Initialize(JamuNPC npc)
 {
 if (panelType != CraftingPanelType.NPCCraftingPanel)
 {
 Debug.LogWarning("Attempted to initialize with NPC but panel is not in NPC mode!");
 return;
 }
 currentNPC = npc;
 Debug.Log("UnifiedCraftingPanel terhubung dengan NPC: " + npc.name);
 gameObject.SetActive(true);
 try
 {
 ResetSlotCombine();
 TampilkanDariInventory();
 }
 catch (System.Exception e)
 {
 Debug.LogError($"Error in Initialize: {e.Message}\n{e.StackTrace}");
 }
 }
 /// <summary>
 /// Method to safely close the panel (can be called from NPC or close button)
 /// </summary>
 public void ClosePanel()
 {
 gameObject.SetActive(false);
 if (currentNPC != null)
 {
 currentNPC.NotifyCraftingPanelClosed();
 }
 // Jangan reset currentNPC = null di sini!
 }
 // ... [rest of file remains unchanged] ...
 private bool IsValidSprite(Sprite sprite)
 {
 // Check if the sprite reference is valid
 if (sprite == null)
 return false;
 try
 {
 // Accessing a property to verify it's not just a null reference but actually valid
 var _ = sprite.rect;
 return true;
 }
 catch (System.Exception)
 {
 return false;
 }
 }
 // Add this helper method to safely set sprite on images
 private void SafeSetSlotSprite(Image targetImage, Sprite sprite)
 {
 if (targetImage == null)
 return;
 if (IsValidSprite(sprite))
 {
 targetImage.sprite = sprite;
 targetImage.color = Color.white;
 }
 else
 {
 targetImage.sprite = null;
 targetImage.color = new Color(1f, 1f, 1f, 0f);
 Debug.LogWarning("Attempted to set invalid sprite to a slot");
 }
 }
 private void LoadRecipes()
 {
 // Get the JamuDatabase instance
 if (JamuSystem.Instance != null && JamuSystem.Instance.jamuDatabase != null)
 {
 daftarResep = JamuSystem.Instance.jamuDatabase.resepJamus;
 }
 else
 {
 Debug.LogWarning("JamuDatabase is not set in JamuSystem.");
 daftarResep = new List<ResepJamu>();
 }
 }
 private string GetBahanFromDatabase(string bahanName)
 {
 var jamuDb = JamuSystem.Instance?.jamuDatabase;
 if (jamuDb != null)
 {
 BahanItem bahanItem = jamuDb.GetBahan(bahanName);
 if (bahanItem != null)
 {
 return bahanItem.itemName;
 }
 }
 return null;
 }
 public void ResetSlotCombine()
 {
 if (slotPanelCombine == null || slotPanelCombine.Length == 0)
 {
 Debug.LogWarning("No slot panels available to reset.");
 return;
 }
        EnableDragAfterCombine();
        foreach (GameObject slot in slotPanelCombine)
 {
 if (slot == null) continue;
 SlotCombine combineData = slot.GetComponent<SlotCombine>();
 if (combineData != null)
 {
 combineData.ClearSlot();
                var slotBahan = slot.GetComponentInChildren<SlotBahan>();
                if (slotBahan != null)
                {
                    slotBahan.UpdateQuantityDisplay();
                }
            }
 // Handle different structure based on panel type
 if (panelType == CraftingPanelType.CombinePanel)
 {
 Transform slotForCombineTransform = slot.transform.Find("Slot_For_Combine");
 if (slotForCombineTransform != null)
 {
 foreach (Transform child in slotForCombineTransform)
 {
 Destroy(child.gameObject);
 }
 Image img = slotForCombineTransform.GetComponent<Image>();
 if (img != null)
 {
 img.sprite = null;
 img.color = new Color(1f, 1f, 1f, 0f);
 }
 }
 }
 else
 {
 // NPCCraftingPanel structure
 Image img = slot.transform.GetChild(0).GetComponent<Image>();
 if (img != null)
 {
 img.sprite = null;
 img.color = new Color(1f, 1f, 1f, 0f);
 }
 }
 }
 bahanDipakai.Clear();
 if (hasilJamuImage != null)
 {
 hasilJamuImage.gameObject.SetActive(false);
 }
 if (namaJamuText != null)
 {
 namaJamuText.gameObject.SetActive(false);
 }
        var dtg = GameManager.instance.gameData;
        if (dtg != null)
 {
            GameManager.instance.SaveGameData();
            Debug.Log("Data inventory saved after resetting slots");
 }
        if (Inventory.Instance != null)
        {
            Inventory.Instance.RefreshInventory();
        }
    }
 public void TampilkanDariInventory()
 {
        var dtg = GameManager.instance.gameData;
        if (dtg == null)
 {
 Debug.LogError("TampilkanDariInventory: DataGame is null!");
 return;
 }
 if (JamuSystem.Instance == null || JamuSystem.Instance.jamuDatabase == null)
 {
 Debug.LogError("TampilkanDariInventory: JamuSystem or jamuDatabase is null!");
 return;
 }
 if (JamuCraftingIntegration.Instance == null)
 {
 Debug.LogError("TampilkanDariInventory: JamuIntegration is null!");
 return;
 }
 // Clear existing slots
 foreach (GameObject slot in bahanSlots)
 {
 if (slot != null)
 Destroy(slot);
 }
 bahanSlots.Clear();
 bahanItemIndices.Clear();
 // Get available bahans
 List<string> bahanTersedia = JamuCraftingIntegration.Instance.GetAvailableBahanNames();
 Debug.Log($"Available bahans: {string.Join(", ", bahanTersedia)}");
 // Create new slots from inventory
 int createdSlots = 0;
 for (int i = 0; i < dtg.barang.Count; i++)
 {
 var item = dtg.barang[i];
 if (item != null && item.jumlah > 0 && bahanTersedia.Contains(item.nama))
 {
 // Create the slot GameObject
 GameObject slot = Instantiate(slotBahanPrefab, slotBahanContainer);
 SlotBahan sb = slot.GetComponentInChildren<SlotBahan>();
 if (sb != null)
 {
 sb.dragLayer = dragLayer;
 }
 bahanSlots.Add(slot);
 bahanItemIndices.Add(i);
 // Set up the visual components
 Image img = slot.transform.GetChild(0).GetComponent<Image>();
 Text txt = slot.transform.GetChild(1).GetComponent<Text>();
 BahanItem bahan = JamuSystem.Instance.jamuDatabase.GetBahan(item.nama);
 if (bahan != null)
 {
 // Critical Fix: Check if sprites are valid before assigning
 Sprite spriteToUse = null;
 spriteToUse = bahan.itemSprite;
 // Check if sprite is valid before using it
 if (IsValidSprite(spriteToUse))
 {
 img.sprite = spriteToUse;
 img.color = Color.white;
 }
 else
 {
 Debug.LogWarning($"Invalid sprite for bahan: {item.nama}, using fallback");
 img.sprite = null; // Or use a fallback sprite
 img.color = new Color(0.5f, 0.5f, 0.5f, 0.5f); // Visual indication of missing sprite
 }
 txt.text = item.jumlah.ToString();
 // Set up the SlotBahan component
 SlotBahan slotBahan = slot.GetComponentInChildren<SlotBahan>();
 if (slotBahan != null)
 {
 slotBahan.SetBahan(bahan);
 createdSlots++;
 }
 // Add click handler for direct selection
 Button slotButton = slot.GetComponent<Button>();
 if (slotButton != null)
 {
 string namaBahan = item.nama;
 slotButton.onClick.RemoveAllListeners();
 slotButton.onClick.AddListener(() => PilihBahan(namaBahan));
 }
 }
 }
 }
 Debug.Log($"Created {createdSlots} bahan slots from inventory");
 if (slotBahanScrollView != null)
 {
 slotBahanScrollView.normalizedPosition = new Vector2(0, 1);
 }
 }
 public void PilihBahan(string namaBahan)
 {
 var dtg = GameManager.instance.gameData;
 if (dtg == null || string.IsNullOrEmpty(namaBahan)) return;
 int inventoryIndex = dtg.barang.FindIndex(b => b != null && b.nama == namaBahan && b.jumlah >
0);
 if (inventoryIndex < 0)
 {
 Debug.LogWarning("Bahan tidak ditemukan atau jumlah habis: " + namaBahan);
 return;
 }
 if (selectedBahanIndex != inventoryIndex)
 {
 selectedBahanIndex = inventoryIndex;
 Debug.Log("Bahan dipilih: " + namaBahan);
 }
 else
 {
 Debug.Log("Bahan yang sama dipilih kembali: " + namaBahan);
 }
 }
    public void TempatkanBahanKeSlotCombine(int index)
    {
        var dtg = GameManager.instance.gameData;
        if (selectedBahanIndex == -1 || dtg == null) return;
        if (index < 0 || index >= slotPanelCombine.Length) return;
        string bahanName = dtg.barang[selectedBahanIndex].nama;
        BahanItem bahan = JamuSystem.Instance.jamuDatabase.GetBahan(bahanName);
        if (bahan == null)
        {
            Debug.LogError($"BahanItem not found for: {bahanName}");
            return;
        }

        if (panelType == CraftingPanelType.CombinePanel)
        {

            Transform slotForCombineTransform = slotPanelCombine[index].transform.Find("Slot_For_Combine");
            if (slotForCombineTransform == null)
            {
                Debug.LogError($"Slot {index} does not have a Slot_For_Combine transform.");
                return;
            }
            foreach (Transform child in slotForCombineTransform)
            {
                Destroy(child.gameObject);
            }
            SlotCombine slotData = slotPanelCombine[index].GetComponent<SlotCombine>();
            if (slotData != null)
            {
                slotData.SetBahan(bahan);
                Debug.Log($"Setting slot {index} with Bahan: {bahan.itemName}");
            }
            Image targetImage = slotForCombineTransform.GetComponent<Image>();
            if (targetImage != null)
            {
                targetImage.sprite = bahan.itemSprite;
                targetImage.color = Color.white;
            }
        }
        else
        {
            // NPCCraftingPanel style
            SlotCombine slotData = slotPanelCombine[index].GetComponent<SlotCombine>();
            if (slotData != null)
            {
                slotData.SetBahan(bahan);
            }
            Image targetImage = slotPanelCombine[index].transform.GetChild(0).GetComponent<Image>();
            if (targetImage != null)
            {
                targetImage.sprite = dtg.barang[selectedBahanIndex].gambar;
                targetImage.color = Color.white;
                float scaleRatio = PanelScalingUtils.CalculateScaleFactor(slotBahanContainer, slotCombineContainer);
                targetImage.transform.localScale = Vector3.one * scaleRatio;
            }
        }
        // Hanya kurangi bahan dari inventory di CombinePanel
        if (panelType == CraftingPanelType.CombinePanel)
        {
            selectedBahanIndex = -1;
        }
        else
        {
            // Untuk NPCCraftingPanel, jangan kurangi bahan di sini
            selectedBahanIndex = -1;
        }
    }
    public void BuatJamu()
    {
        SlotBahan.SetGlobalDragLock(true);
        SlotCombine.SetGlobalDropLock(true);
        ReturnToInventoryOnDrop.SetGlobalDragLock(true);
        DisableDragDuringCombine();

        if (JamuSystem.Instance == null || JamuSystem.Instance.jamuDatabase == null)
        {
            Debug.LogError("JamuSystem atau jamuDatabase tidak tersedia!");
            UnlockDragDrop();
            EnableDragAfterCombine();
            return;
        }

        // Log slot status
        Debug.Log("==== Slot Status ====");
        for (int i = 0; i < slotPanelCombine.Length; i++)
        {
            SlotCombine combineData = slotPanelCombine[i].GetComponent<SlotCombine>();
            BahanItem bahan = combineData?.GetBahan();
            Debug.Log($"Slot {i}: {(bahan != null ? bahan.itemName : "NULL")}");
        }

        // Clear and collect ingredients from slots
        bahanDipakai.Clear();
        foreach (GameObject slot in slotPanelCombine)
        {
            SlotCombine combineData = slot.GetComponent<SlotCombine>();
            BahanItem bahan = combineData?.GetBahan();
            Transform slotForCombine = slot.transform.Find("Slot_For_Combine");
            if (slotForCombine != null && slotForCombine.childCount == 0)
            {
                continue;
            }
            if (bahan != null)
            {
                bahanDipakai.Add(bahan.itemName);
                Debug.Log($"Bahan digunakan: {bahan.itemName}");
            }
        }

        if (bahanDipakai.Count == 0)
        {
            Debug.LogWarning("Tidak ada bahan yang digunakan untuk membuat jamu.");
            UnlockDragDrop();
            EnableDragAfterCombine();
            return;
        }

        // Find matching recipe
        ResepJamu resepBerhasil = null;
        Debug.Log($"Checking {daftarResep.Count} recipes with {bahanDipakai.Count} ingredients used");
        foreach (ResepJamu resep in daftarResep)
        {
            if (resep.bahanResep.Length != bahanDipakai.Count)
            {
                Debug.Log($"Skipping recipe {resep.jamuName}: ingredient count mismatch ({resep.bahanResep.Length} vs {bahanDipakai.Count})");
                continue;
            }
            List<string> sortedRecipeIngredients = new List<string>(resep.bahanResep);
            sortedRecipeIngredients.Sort();
            List<string> sortedBahanDipakai = new List<string>(bahanDipakai);
            sortedBahanDipakai.Sort();
            bool semuaBahanCocok = sortedRecipeIngredients.SequenceEqual(sortedBahanDipakai);
            Debug.Log($"Recipe {resep.jamuName} matches: {semuaBahanCocok}");
            if (semuaBahanCocok)
            {
                resepBerhasil = resep;
                break;
            }
        }

        // === KURANGI BAHAN DARI INVENTORY DI SINI ===
        var dtg = GameManager.instance.gameData;
        if (dtg != null)
        {
            foreach (string namaBahan in bahanDipakai)
            {
                var item = dtg.barang.FirstOrDefault(i => i != null && i.nama == namaBahan);
                if (item != null && item.jumlah > 0)
                {
                    item.jumlah--;
                    if (item.jumlah <= 0)
                    {
                        item.nama = "";
                        item.gambar = null;
                        item.jumlah = 0;
                    }
                }
            }
            
                GameManager.instance.SaveGameData(); // Save yang benar
            if (panelType == CraftingPanelType.NPCCraftingPanel)
            {
                GameManager.instance?.SaveGameData();
            }
            Debug.Log("Inventory bahan dikurangi dan data disimpan ke DataGame.");
        }
        Inventory.Instance?.RefreshInventory();

        // Show result UI
        if (hasilJamuImage != null) hasilJamuImage.gameObject.SetActive(true);
        if (namaJamuText != null) namaJamuText.gameObject.SetActive(true);

        if (resepBerhasil != null)
        {
            currentCraftedJamu = resepBerhasil;
            hasilJamuImage.sprite = resepBerhasil.jamuSprite;
            namaJamuText.text = resepBerhasil.jamuName;
            Debug.Log($"Jamu berhasil dibuat: {resepBerhasil.jamuName}");

            if (panelType == CraftingPanelType.CombinePanel)
            {
                if (jamuIntegration != null)
                {
                    jamuIntegration.AddJamuToInventory(resepBerhasil);
                }
                else
                {
                    Debug.LogError("JamuIntegration tidak tersedia.");
                    TambahJamuKeInventory(resepBerhasil);
                }
                if (AlmanacSystem.Instance != null)
                {
                    AlmanacSystem.Instance.DiscoverJamu(resepBerhasil.jamuName);
                }
            }
            TaskManager.Instance?.OnUjiResepJamu(resepBerhasil.jamuName);

            if (AlmanacSystem.Instance != null)
            {
                bool isBaru = AlmanacSystem.Instance.IsNewlyDiscovered(resepBerhasil.jamuName);
                if (isBaru && resepBerhasil.jamuName.ToLower().Contains("wedang jahe"))
                {
                    TaskManager.Instance?.OnUjiResepWedangJahe(resepBerhasil.jamuName);
                    Debug.Log($"Trigger: Buka resep Wedang Jahe ({resepBerhasil.jamuName})!");
                }
            }
        }
        else
        {
            currentCraftedJamu = null;
            JamuGagal jamuGagal = GetOrCreateSingleFailedJamu();
            if (jamuGagal != null)
            {
                currentJamuGagal = jamuGagal;
                hasilJamuImage.sprite = jamuGagal.itemSprite;
                namaJamuText.text = jamuGagal.itemName;
                if (AlmanacSystem.Instance != null)
                {
                    AlmanacSystem.Instance.DiscoverJamuGagal(jamuGagal.itemName);
                }
            }
            else
            {
                hasilJamuImage.sprite = JamuSystem.Instance.jamuDatabase.defaultFailedJamuSprite;
                namaJamuText.text = "Jamu tidak diketahui!";
            }
        }

        if (panelType == CraftingPanelType.CombinePanel)
        {
            StartCoroutine(HandleCombineReset());
            Invoke("ResetSlotCombineDelayed", 5.0f);
        }
        CraftingManagerHelper.TutorialCraftingBerhasil = true;
        ResetAllSlotBahanVisual();
    }
    private void UnlockDragDrop()
    {
        SlotBahan.SetGlobalDragLock(false);
        SlotCombine.SetGlobalDropLock(false);
        ReturnToInventoryOnDrop.SetGlobalDragLock(false);
        Debug.Log("[CraftingPanel] Drag drop unlocked");
    }
    private IEnumerator HandleCombineReset()
    {
        yield return new WaitForSeconds(5.0f);

        // Panggil UnlockDragDrop untuk membuka kembali drag drop functionality
        UnlockDragDrop();

        EnableDragAfterCombine();
        CompleteReset();
        yield return new WaitForSeconds(0.1f);
        TampilkanDariInventory();
    }

    private void DisableDragDuringCombine()
    {
        isDraggingLocked = true;
        foreach (GameObject slot in slotPanelCombine)
        {
            var slotBahan = slot.GetComponentInChildren<SlotBahan>();
            if (slotBahan != null)
            {
                slotBahan.LockDragging(true);
            }
        }
    }

    // Tambahkan method ini untuk enable kembali drag setelah combine selesai
    private void EnableDragAfterCombine()
    {
        isDraggingLocked = false;
        foreach (GameObject slot in slotPanelCombine)
        {
            var slotBahan = slot.GetComponentInChildren<SlotBahan>();
            if (slotBahan != null)
            {
                slotBahan.LockDragging(false);
            }
        }
    }

    private void ShowOrderCompletedNotification(string jamuName)
 {
 Debug.Log($" Order Completed: {jamuName}!");
 // Optional: tambahkan notifikasi UI kamu di sini
 }
 // Helper method to get or create the single failed jamu
 private JamuGagal GetOrCreateSingleFailedJamu()
 {
 if (JamuSystem.Instance == null || JamuSystem.Instance.jamuDatabase == null)
 {
 Debug.LogError("JamuSystem atau jamuDatabase tidak tersedia untuk membuat jamu gagal!");
 return null;
 }
 JamuDatabase database = JamuSystem.Instance.jamuDatabase;
 // Look for the single failed jamu (should be named "Jamu Gagal" or similar)
 const string FAILED_JAMU_NAME = "Jamu Gagal";
 JamuGagal failedJamu = database.jamuGagalList.Find(j => j.itemName == FAILED_JAMU_NAME);
 if (failedJamu != null)
 {
 Debug.Log($"Found existing failed jamu: {failedJamu.itemName}");
 return failedJamu;
 }
 // Create the single failed jamu entry if it doesn't exist
 JamuGagal newFailedJamu = new JamuGagal
 {
 itemName = FAILED_JAMU_NAME,
 itemSprite = database.defaultFailedJamuSprite,
 itemValue = 5, // Low value for failed jamu
 description = "Jamu yang gagal dibuat karena kombinasi bahan yang tidak tepat. Meskipun tidakberhasil, tetap bisa dipelajari untuk referensi di masa depan.",
 bahanPenyusun = new string[0], // Empty array since this represents all failed combinations
 tanggalDibuat = System.DateTime.Now
 };
 // Add to database
 database.jamuGagalList.Add(newFailedJamu);
 Debug.Log($"Created single failed jamu: {newFailedJamu.itemName}");
 // Save the database if needed (depending on your save system)
 if (JamuSystem.Instance != null)
 {
 JamuSystem.Instance.SaveDatabase();
 }
 return newFailedJamu;
 }
 private void InitializeSlots()
 {
 for (int i = 0; i < slotPanelCombine.Length; i++)
 {
 SlotCombine slotComponent = slotPanelCombine[i].GetComponent<SlotCombine>();
 if (slotComponent == null)
 {
 slotComponent = slotPanelCombine[i].AddComponent<SlotCombine>();
 }
 slotComponent.slotIndex = i;
 }
 Debug.Log($"Initialized {slotPanelCombine.Length} combine slots");
 }
 // Method to return the current crafted Jamu
 public ResepJamu GetCurrentCraftedJamu()
 {
 return currentCraftedJamu;
 }
    // NPC specific method
    public void KasihJamuKeNPC()
    {
        if (panelType == CraftingPanelType.NPCCraftingPanel)
        {
            ResetNPCCraftingPanel();
        }
        else
        {
            ResetSlotCombine();
        }

        if (currentCraftedJamu == null && currentJamuGagal == null)
        {
            Debug.LogWarning("Belum membuat jamu apapun untuk diberikan!");
            return;
        }

        if (currentNPC != null)
        {
            if (currentCraftedJamu != null)
            {
                currentNPC.GiveJamuToNPC(currentCraftedJamu);
                TaskManager.Instance?.OnJualJamu(currentCraftedJamu.jamuName, 1);

            }
            else if (currentJamuGagal != null)
            {
                currentNPC.GiveJamuGagalToNPC(currentJamuGagal);
                Debug.Log($"❌Jamu Gagal '{currentJamuGagal.itemName}' diberikan ke NPC.");

                if (OrderManager.Instance != null)
                {
                    bool failed = OrderManager.Instance.TryFailOrderWithJamuGagal(currentNPC.npcData.npcName);
                    if (failed)
                    {
                        Debug.Log($"Order untuk {currentNPC.npcData.npcName} ditandai gagal karena diberikan Jamu Gagal.");
                    }
                }

                GameManager.instance?.SaveGameData();
                Inventory.Instance?.RefreshInventory();
            }

            currentCraftedJamu = null;
            currentJamuGagal = null;
            ResetSlotCombine();
            UnlockDragDrop();
            EnableDragAfterCombine();
            gameObject.SetActive(false);
        }
        else
        {
            Debug.LogError("Tidak ada NPC yang sedang terhubung!");
        }

    }
    public bool HasMatchingOrder(List<string> ingredients)
    {
        if (panelType != CraftingPanelType.NPCCraftingPanel || OrderManager.Instance == null)
        return false;
        // Cek apakah ada resep yang cocok dengan ingredients
        foreach (ResepJamu resep in daftarResep)
        {
            if (resep.bahanResep.Length != ingredients.Count) continue;
            List<string> sortedRecipeIngredients = new List<string>(resep.bahanResep);
            sortedRecipeIngredients.Sort();
            List<string> sortedIngredients = new List<string>(ingredients);
            sortedIngredients.Sort();
            if (sortedRecipeIngredients.SequenceEqual(sortedIngredients))
            {
            // Cek apakah ada order untuk jamu ini
            return OrderManager.Instance.HasOrderForJamu(resep.jamuName);
            }
        }
        return false;
    }
 public string GetOrderHint()
 {
 if (panelType != CraftingPanelType.NPCCraftingPanel || OrderManager.Instance == null)
 return "";
 var pendingOrders = OrderManager.Instance.GetOrdersByStatus(OrderStatus.Pending);
 if (pendingOrders.Count > 0)
 {
 var order = pendingOrders[0]; // Ambil order pertama sebagai hint
 return $"Order tersedia: {order.requestedJamuName} untuk {order.customerName}";
 }
 return "Tidak ada order yang tersedia saat ini.";
 }
 // 7. Method untuk tracking progress order saat mulai crafting (khusus NPC)
 private void OnStartCrafting()
 {
 if (panelType == CraftingPanelType.NPCCraftingPanel && OrderManager.Instance != null)
 {
 // Cek apakah ingredients yang digunakan bisa membuat jamu yang ada ordernya
 if (HasMatchingOrder(bahanDipakai))
 {
 Debug.Log("Player sedang membuat jamu yang memiliki order!");
 // Set order status ke InProgress jika ada
 foreach (ResepJamu resep in daftarResep)
 {
 if (resep.bahanResep.Length != bahanDipakai.Count) continue;
 List<string> sortedRecipeIngredients = new List<string>(resep.bahanResep);
 sortedRecipeIngredients.Sort();
 List<string> sortedBahanDipakai = new List<string>(bahanDipakai);
 sortedBahanDipakai.Sort();
 if (sortedRecipeIngredients.SequenceEqual(sortedBahanDipakai))
 {
 OrderManager.Instance.TrackOrderProgress(resep.jamuName);
 break;
 }
 }
 }
 }
 }
 // Method for resetting with delay (for CombinePanel)
 private void ResetSlotCombineDelayed()
 {
 CompleteReset();
 StartCoroutine(UpdateInventoryAfterDelay(0.1f));
 }
 // Method for thorough reset
 public void CompleteReset()
 {
 Debug.Log("Starting thorough reset of the combine panel...");
        EnableDragAfterCombine();

        foreach (GameObject slot in slotPanelCombine)
 {
 if (slot == null) continue;
 // Reset data di komponen SlotCombine
 SlotCombine slotData = slot.GetComponent<SlotCombine>();
 if (slotData != null)
 {
 slotData.SetBahan(null);
                var slotBahan = slot.GetComponentInChildren<SlotBahan>();
                if (slotBahan != null)
                {
                    slotBahan.UpdateQuantityDisplay();
                }
            }
 if (panelType == CraftingPanelType.CombinePanel)
 {
 // Hapus semua isi dari Slot_For_Combine
 Transform slotForCombine = slot.transform.Find("Slot_For_Combine");
 if (slotForCombine != null)
 {
 foreach (Transform child in slotForCombine)
 {
 Destroy(child.gameObject);
 }
 // Reset image background-nya
 Image slotImage = slotForCombine.GetComponent<Image>();
 if (slotImage != null)
 {
 slotImage.sprite = null;
 slotImage.color = new Color(1f, 1f, 1f, 0f);
 }
 }
 }
 else
 {
 // NPCCraftingPanel structure
 Image img = slot.transform.GetChild(0).GetComponent<Image>();
 if (img != null)
 {
 img.sprite = null;
 img.color = new Color(1f, 1f, 1f, 0f);
 }
 }
 }
 // Reset data penggunaan bahan
 bahanDipakai.Clear();
 // Reset UI hasil jamu
 if (hasilJamuImage != null)
 {
 hasilJamuImage.gameObject.SetActive(false);
 }
 if (namaJamuText != null)
 {
 namaJamuText.text = string.Empty;
 namaJamuText.gameObject.SetActive(false);
 }
 Debug.Log("Thorough reset completed successfully.");
        if (Inventory.Instance != null)
        {
            Inventory.Instance.RefreshInventory();

        }
        ResetAllSlotBahanVisual();
    }
 private IEnumerator UpdateInventoryAfterDelay(float delay)
 {
 yield return new WaitForSeconds(delay);
 TampilkanDariInventory();
 Debug.Log("Inventory display updated after reset.");
 }
 private void TambahJamuKeInventory(ResepJamu resep)
 {
 var dtg = GameManager.instance.gameData;
 if (dtg == null) return;
 bool jamuDitambahkan = false;
 for (int i = 0; i < dtg.barang.Count; i++)
 {
 if (dtg.barang[i] == null)
 {
 // Implementasi penambahan jamu baru sesuai sistem Anda
 jamuDitambahkan = true;
 break;
 }
 else if (dtg.barang[i].gambar != null && dtg.barang[i].gambar.name == resep.jamuSprite.name)
 {
 dtg.barang[i].jumlah++;
 jamuDitambahkan = true;
 break;
     }
 }
         if (!jamuDitambahkan)
         {
        Debug.LogWarning("Inventory penuh, tidak bisa menambah jamu!");
        }
    }
    private void ResetNPCCraftingPanel()
    {
        // Reset semua slot combine
        foreach (GameObject slot in slotPanelCombine)
        {
            if (slot == null) continue;
            SlotCombine slotData = slot.GetComponent<SlotCombine>();
            if (slotData != null)
            {
                slotData.SetBahan(null);
                var slotBahan = slot.GetComponentInChildren<SlotBahan>();
                if (slotBahan != null)
                {
                    slotBahan.UpdateQuantityDisplay();
                }
            }
            // NPCCraftingPanel structure
            Image img = slot.transform.GetChild(0).GetComponent<Image>();
            if (img != null)
            {
                img.sprite = null;
                img.color = new Color(1f, 1f, 1f, 0f);
            }
        }

        // Reset data penggunaan bahan
        bahanDipakai.Clear();

        // Jangan sembunyikan hasilJamuImage dan namaJamuText di NPC mode

        // Refresh inventory dan panel bahan
        Inventory.Instance?.RefreshInventory();
        TampilkanDariInventory();
    }
    private void ResetAllSlotBahanVisual()
    {
        foreach (GameObject slot in slotPanelCombine)
        {
            if (slot == null) continue;
            var slotBahan = slot.GetComponentInChildren<SlotBahan>();
            if (slotBahan != null)
            {
                slotBahan.ResetVisualQuantity();
            }
        }
    }
}