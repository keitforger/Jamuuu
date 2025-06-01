using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using TMPro;
using UnityEngine.SceneManagement;

/// <summary>
/// Manages UI functionality including modal behavior for panels by blocking interactions
/// with background elements when any UI panel is open
/// </summary>
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }
    [SerializeField] public List<GameObject> modalPanels = new List<GameObject>();
    private GameObject activePanel;
    [SerializeField] private GameObject blockerPanel;
    private EventSystem eventSystem;

    [Header("Time Display")]
    public TextMeshProUGUI timeText;
    public Image dayNightIndicator;

    [Header("Night Overlay Color Settings")]
    public Color nightColor = new Color(0.05f, 0.05f, 0.2f, 0.7f);

    [Header("Transition Time")]
    public float dayStart = 8f;
    public float nightStart = 18f;
    public float transitionDuration = 2f;

    [Header("Sleep Settings")]
    public GameObject player;
    public Transform spawnPointOutside;
    public float sleepNightHour = 18f;
    public float sleepMorningHour = 8f;
    public float fadeDuration = 1.0f;

    public GameObject inventoryGO, almanacGO;
    private NPCSpawner npcSpawner;
    private GameManager gameManager;
    private GameObject[] queueSlots = new GameObject[3];

    private bool isSleepingTransition = false;

    private void Awake()
    {
        FindReferences();
        var uiData = GameManager.instance?.LoadUIData();
        if (uiData != null)
        {
            UpdateTimeDisplay(uiData.currentTime);
        }
        else
        {
            Debug.LogWarning("UIManager: uiData dari LoadUIData() kosong!");
            UpdateTimeDisplay(6f);
        }

        SubscribeToEvents();

        // Singleton setup
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Get event system reference
        eventSystem = EventSystem.current;
        if (eventSystem == null)
        {
            Debug.LogWarning("No EventSystem found in the scene. Creating one.");
            eventSystem = new GameObject("EventSystem").AddComponent<EventSystem>();
            eventSystem.gameObject.AddComponent<StandaloneInputModule>();
        }

        // Create blocker panel if it doesn't exist
        if (blockerPanel == null)
            CreateBlockerPanel();

        blockerPanel.SetActive(false);

        if (player == null)
        {
            var obj = GameObject.FindGameObjectWithTag("Player");
            if (obj != null) player = obj;
        }

        var canvas = GetComponentInChildren<Canvas>();
        modalPanels.Clear();
        var game1 = GameObject.Find("Game1");
        if (game1 != null)
        {
            foreach (var panel in FindAllChildrenWithTag(game1.transform, "ModalPanel"))
                modalPanels.Add(panel);
        }
        if (inventoryGO == null)
            inventoryGO = canvas.transform.Find("PanelInventory")?.gameObject;
        if (almanacGO == null)
            almanacGO = canvas.transform.Find("PanelAlmanac")?.gameObject;
    }

    void OnDestroy()
    {
        if (npcSpawner != null)
            npcSpawner.OnTimeChanged -= UpdateTimeDisplay;
    }

    private IEnumerable<GameObject> FindAllChildrenWithTag(Transform parent, string tag)
    {
        foreach (Transform child in parent)
        {
            if (child.gameObject.CompareTag(tag))
                yield return child.gameObject;
            foreach (var go in FindAllChildrenWithTag(child, tag))
                yield return go;
        }
    }

    void FindReferences()
    {
        npcSpawner = FindAnyObjectByType<NPCSpawner>();
        gameManager = FindAnyObjectByType<GameManager>();

        if (npcSpawner == null)
            Debug.LogError("NPCSpawner not found!");

        if (gameManager == null)
            Debug.LogWarning("GameManager not found, will try alternative money system");
    }

    void SubscribeToEvents()
    {
        if (npcSpawner != null)
            npcSpawner.OnTimeChanged += UpdateTimeDisplay;
    }

    void Update()
    {
        if (npcSpawner != null)
        {
            int queueCount = npcSpawner.GetQueueCount();
            GameManager.instance.SaveUIData(npcSpawner.GetCurrentTime(), queueCount);
        }
    }
    public void BackToMainMenu()
    {
        SceneManager.LoadScene("GameLauncher");
    }

    public void OnOpenInventoryButton()
    {
        inventoryGO.SetActive(true);
    }
    public void OnOpenAlmanacButton()
    {
        almanacGO.SetActive(true);
    }

    void UpdateTimeDisplay(float currentTime)
    {
        if (timeText != null)
        {
            int hours = Mathf.FloorToInt(currentTime);
            int minutes = Mathf.FloorToInt((currentTime - hours) * 60);

            string period = hours >= 12 ? "PM" : "AM";
            int displayHours = hours > 12 ? hours - 12 : (hours == 0 ? 12 : hours);

            timeText.text = string.Format("{0:00}:{1:00} {2}", displayHours, minutes, period);
        }

        if (!isSleepingTransition)
            UpdateDayNightIndicator(currentTime);
    }

    void UpdateDayNightIndicator(float currentTime)
    {
        if (dayNightIndicator != null)
        {
            float hour = currentTime;
            Color resultColor = nightColor;
            resultColor.a = 0f;

            float morningTransitionStart = dayStart - transitionDuration;
            if (morningTransitionStart < 0f) morningTransitionStart += 24f;
            float nightTransitionEnd = nightStart + transitionDuration;
            if (nightTransitionEnd >= 24f) nightTransitionEnd -= 24f;

            if (
                (morningTransitionStart < dayStart && hour >= morningTransitionStart && hour < dayStart) ||
                (morningTransitionStart > dayStart && (hour >= morningTransitionStart || hour < dayStart))
            )
            {
                float t;
                if (morningTransitionStart < dayStart)
                    t = (hour - morningTransitionStart) / transitionDuration;
                else
                    t = ((hour + 24f - morningTransitionStart) % 24f) / transitionDuration;
                resultColor.a = Mathf.Lerp(nightColor.a, 0f, t);
            }
            else if (dayStart <= nightStart
                ? hour >= dayStart && hour < nightStart
                : hour >= dayStart || hour < nightStart)
            {
                resultColor.a = 0f;
            }
            else if (
                (nightStart < nightTransitionEnd && hour >= nightStart && hour < nightTransitionEnd) ||
                (nightStart > nightTransitionEnd && (hour >= nightStart || hour < nightTransitionEnd))
            )
            {
                float t;
                if (nightStart < nightTransitionEnd)
                    t = (hour - nightStart) / transitionDuration;
                else
                    t = ((hour + 24f - nightStart) % 24f) / transitionDuration;
                resultColor.a = Mathf.Lerp(0f, nightColor.a, t);
            }
            else
            {
                resultColor.a = nightColor.a;
            }

            dayNightIndicator.color = resultColor;
        }
    }

    public void TrySleepAndSkipNight()
    {
        if (npcSpawner != null)
        {
            float jam = npcSpawner.GetCurrentTime();
            if (jam >= sleepNightHour)
            {
                StartCoroutine(SleepAndSkipNightCoroutine());
            }
        }
    }

    private System.Collections.IEnumerator SleepAndSkipNightCoroutine()
    {
        isSleepingTransition = true;
        yield return StartCoroutine(FadeInRoutine());

        if (npcSpawner != null)
        {
            npcSpawner.SetDayTime();
        }
        else
        {
            Debug.LogWarning("NPCSpawner not found in UIManager.");
        }

        if (player != null && spawnPointOutside != null)
        {
            player.transform.position = spawnPointOutside.position;
        }

        yield return StartCoroutine(FadeOutRoutine());

        isSleepingTransition = false;

        if (npcSpawner != null)
            UpdateDayNightIndicator(npcSpawner.GetCurrentTime());
    }

    public System.Collections.IEnumerator FadeInRoutine()
    {
        if (dayNightIndicator == null)
            yield break;

        dayNightIndicator.gameObject.SetActive(true);
        Color c = dayNightIndicator.color;
        float startAlpha = c.a;
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            c.a = Mathf.Lerp(startAlpha, 1f, t / fadeDuration);
            dayNightIndicator.color = c;
            yield return null;
        }
        c.a = 1f;
        dayNightIndicator.color = c;
    }

    public System.Collections.IEnumerator FadeOutRoutine()
    {
        if (dayNightIndicator == null)
            yield break;

        Color c = Color.black;
        c.a = 1f;
        dayNightIndicator.color = c;

        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            c.a = Mathf.Lerp(1f, 0f, t / fadeDuration);
            dayNightIndicator.color = c;
            yield return null;
        }
        c.a = 0f;
        dayNightIndicator.color = c;
    }

    public void ForceUpdateTimeDisplay()
    {
        if (npcSpawner != null)
            UpdateTimeDisplay(npcSpawner.GetCurrentTime());
    }

    public void ShowMessage(string message, float duration = 3f)
    {
        StartCoroutine(ShowTemporaryMessage(message, duration));
    }

    System.Collections.IEnumerator ShowTemporaryMessage(string message, float duration)
    {
        Debug.Log("UI Message: " + message);
        yield return new WaitForSeconds(duration);
    }

    private void CreateBlockerPanel()
    {
        blockerPanel = new GameObject("BlockerPanel");
        // Don't set parent here; parent will be set dynamically when needed
        // Initially inactive
        blockerPanel.SetActive(false);

        // Add a Canvas Group for raycast blocking
        CanvasGroup canvasGroup = blockerPanel.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0.5f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = true;

        // Add Image for visual
        Image blockerImage = blockerPanel.AddComponent<Image>();
        blockerImage.color = new Color(0, 0, 0, 0.5f);

        // Add RectTransform and fill parent when parent is set
        RectTransform rectTransform = blockerPanel.GetComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }

    // Open a panel and automatically close all others
    public void TogglePanel(GameObject panelToOpen)
    {
        modalPanels.RemoveAll(panel => panel == null);

        foreach (var panel in modalPanels)
        {
            if (panel != panelToOpen && panel != null)
            {
                panel.SetActive(false);
            }
        }

        if (panelToOpen == null)
        {
            Debug.LogWarning("Panel yang ingin dibuka sudah dihancurkan.");
            return;
        }

        bool isCurrentlyActive = panelToOpen.activeSelf;

        if (!isCurrentlyActive)
        {
            panelToOpen.SetActive(true);
            panelToOpen.transform.SetAsLastSibling();
            activePanel = panelToOpen;
            ShowBlockerUnderPanel(panelToOpen);
        }
        else
        {
            panelToOpen.SetActive(false);
            activePanel = null;
            HideBlocker();
        }
    }

    // Register a panel to be managed by UIManager
    public void RegisterPanel(GameObject panel)
    {
        if (!modalPanels.Contains(panel))
        {
            modalPanels.Add(panel);

            PanelStateMonitor monitor = panel.GetComponent<PanelStateMonitor>();
            if (monitor == null)
            {
                monitor = panel.AddComponent<PanelStateMonitor>();
                monitor.Initialize(this);
            }
            panel.SetActive(false);
        }
    }

    public void BlockGameInput()
    {
        if (activePanel != null)
            ShowBlockerUnderPanel(activePanel);
        else
            blockerPanel.SetActive(true);
    }

    public void UnblockGameInput()
    {
        blockerPanel.SetActive(false);
    }

    // Open a specific panel and close any other open panels
    public void OpenPanel(GameObject panel)
    {
        if (panel == activePanel && panel.activeSelf)
            return;

        if (activePanel != null && activePanel != panel)
        {
            activePanel.SetActive(false);
        }

        activePanel = panel;
        panel.SetActive(true);
        panel.transform.SetAsLastSibling();
        ShowBlockerUnderPanel(panel);
    }

    public void CloseActivePanel()
    {
        if (activePanel != null)
        {
            activePanel.SetActive(false);
            activePanel = null;
            HideBlocker();
        }
    }

    public void ClosePanel(GameObject panel)
    {
        if (panel == activePanel)
        {
            panel.SetActive(false);
            activePanel = null;
            HideBlocker();
        }
    }

    /// <summary>
    /// Show blockerPanel as sibling just below the given modal panel in the same parent,
    /// regardless of canvas hierarchy.
    /// </summary>
    private void ShowBlockerUnderPanel(GameObject modalPanel)
    {
        if (blockerPanel == null || modalPanel == null) return;

        // Move blockerPanel to the same parent as modalPanel
        var modalParent = modalPanel.transform.parent;
        blockerPanel.transform.SetParent(modalParent, false);

        // Cek apakah parent-nya punya Canvas
        bool hasCanvas = false;
        Transform t = modalParent;
        while (t != null)
        {
            if (t.GetComponent<Canvas>() != null)
            {
                hasCanvas = true;
                break;
            }
            t = t.parent;
        }

        int modalIndex = modalPanel.transform.GetSiblingIndex();

        if (hasCanvas)
        {
            // Di dalam canvas, blocker di atas panel
            blockerPanel.transform.SetSiblingIndex(modalIndex + 1);
        }
        else
        {
            // Di luar canvas, blocker di bawah panel
            blockerPanel.transform.SetSiblingIndex(modalIndex);
        }

        // Ensure size fills parent
        RectTransform blockerRect = blockerPanel.GetComponent<RectTransform>();
        if (blockerRect != null)
        {
            blockerRect.anchorMin = Vector2.zero;
            blockerRect.anchorMax = Vector2.one;
            blockerRect.offsetMin = Vector2.zero;
            blockerRect.offsetMax = Vector2.zero;
        }

        // Buat transparan dan klik tembus
        CanvasGroup cg = blockerPanel.GetComponent<CanvasGroup>();
        if (cg != null)
        {
            cg.alpha = 0f;              // Tidak kelihatan sama sekali
            cg.blocksRaycasts = false;  // Klik tembus ke belakang
        }

        // Jika ada image, pastikan juga alpha-nya 0
        Image img = blockerPanel.GetComponent<Image>();
        if (img != null)
        {
            Color c = img.color;
            c.a = 0f;
            img.color = c;
        }

        blockerPanel.SetActive(true);
    }

    private void HideBlocker()
    {
        if (blockerPanel != null)
        {
            blockerPanel.SetActive(false);
        }
    }

    // Called by the PanelStateMonitor when a panel is enabled
    public void OnPanelOpened(GameObject panel)
    {
        if (activePanel != null && activePanel != panel)
        {
            activePanel.SetActive(false);
        }

        activePanel = panel;
        panel.transform.SetAsLastSibling();
        ShowBlockerUnderPanel(panel);
    }

    public void OnPanelClosed(GameObject panel)
    {
        if (panel == activePanel)
        {
            activePanel = null;
            HideBlocker();
        }
    }
}

// Helper component to monitor panel state changes
public class PanelStateMonitor : MonoBehaviour
{
    private UIManager uiManager;

    public void Initialize(UIManager manager)
    {
        uiManager = manager;
    }

    private void OnEnable()
    {
        if (uiManager != null)
        {
            uiManager.OnPanelOpened(gameObject);
        }
    }

    private void OnDisable()
    {
        if (uiManager != null)
        {
            uiManager.OnPanelClosed(gameObject);
        }
    }
}