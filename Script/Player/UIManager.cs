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
    public Color nightColor = new Color(0.05f, 0.05f, 0.2f, 0.7f); // Warna overlay malam (bisa diatur di Inspector)

    [Header("Transition Time")]
    public float dayStart = 8f;   // Jam 8 pagi mulai terang
    public float nightStart = 18f; // Jam 6 sore mulai gelap
    public float transitionDuration = 2f; // Jam durasi gradasi (misal 2 jam)

    // === Tambahan untuk fitur tidur ===
    [Header("Sleep Settings")]
    public GameObject player;              // Drag MC/player ke sini (atau cari dengan tag "Player")
    public Transform spawnPointOutside;    // Drag spawn point depan rumah
    public float sleepNightHour = 18f;     // Jam mulai bisa tidur (6 malam)
    public float sleepMorningHour = 8f;    // Jam bangun pagi (8 pagi)
    public float fadeDuration = 1.0f;      // Durasi fade in/out

    public GameObject inventoryGO, almanacGO;
    private NPCSpawner npcSpawner;
    private GameManager gameManager;
    private GameObject[] queueSlots = new GameObject[3];


    // ======== Tambahan FLAG untuk sleep transition ========
    private bool isSleepingTransition = false;

    private void Awake()
    {
        FindReferences();
        var uiData = GameManager.instance.LoadUIData();
        UpdateTimeDisplay(uiData.currentTime);

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

        // Initially disable the blocker
        blockerPanel.SetActive(false);

        // --- Cari player otomatis jika belum di-assign di inspector ---
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
        // Auto-assign inventoryGO dan almanacGO jika belum
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
        // Ganti "MainMenu" sesuai nama scene main menu kamu
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

        // Cegah update overlay malam ketika transisi sleep
        if (!isSleepingTransition)
            UpdateDayNightIndicator(currentTime);
    }

    void UpdateDayNightIndicator(float currentTime)
    {
        if (dayNightIndicator != null)
        {
            float hour = currentTime;
            Color resultColor = nightColor;
            resultColor.a = 0f; // Default transparan (siang)

            float morningTransitionStart = dayStart - transitionDuration;
            if (morningTransitionStart < 0f) morningTransitionStart += 24f;
            float nightTransitionEnd = nightStart + transitionDuration;
            if (nightTransitionEnd >= 24f) nightTransitionEnd -= 24f;

            // Gradasi malam ke siang (pagi)
            if (
                (morningTransitionStart < dayStart && hour >= morningTransitionStart && hour < dayStart) ||
                (morningTransitionStart > dayStart && (hour >= morningTransitionStart || hour < dayStart))
            )
            {
                float t;
                if (morningTransitionStart < dayStart)
                    t = (hour - morningTransitionStart) / transitionDuration;
                else // wrap around tengah malam
                    t = ((hour + 24f - morningTransitionStart) % 24f) / transitionDuration;
                resultColor.a = Mathf.Lerp(nightColor.a, 0f, t);
            }
            // Siang: dayStart <= jam < nightStart (wrap around juga)
            else if (dayStart <= nightStart
                ? hour >= dayStart && hour < nightStart
                : hour >= dayStart || hour < nightStart)
            {
                resultColor.a = 0f;
            }
            // Gradasi siang ke malam (sore)
            else if (
                (nightStart < nightTransitionEnd && hour >= nightStart && hour < nightTransitionEnd) ||
                (nightStart > nightTransitionEnd && (hour >= nightStart || hour < nightTransitionEnd))
            )
            {
                float t;
                if (nightStart < nightTransitionEnd)
                    t = (hour - nightStart) / transitionDuration;
                else // wrap around tengah malam
                    t = ((hour + 24f - nightStart) % 24f) / transitionDuration;
                resultColor.a = Mathf.Lerp(0f, nightColor.a, t);
            }
            // Malam penuh
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
            npcSpawner.SetDayTime(); // Pastikan SetDayTime set jam ke pagi (8.0)
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
        // Jangan setActive(false)!
        // Setelah fade out, overlay malam di-update di luar coroutine (setelah flag isSleepingTransition = false)
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
        // Create a new GameObject for the blocker panel
        blockerPanel = new GameObject("BlockerPanel");
        blockerPanel.transform.SetParent(transform);

        // Add a Canvas component
        Canvas blockerCanvas = blockerPanel.AddComponent<Canvas>();
        blockerCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        blockerCanvas.sortingOrder = 100; // High enough to be above most UI but below panels

        // Add a Canvas Group to handle blocking raycasts
        CanvasGroup canvasGroup = blockerPanel.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0.5f; // Semi-transparent
        canvasGroup.interactable = false; // No direct interaction with the blocker
        canvasGroup.blocksRaycasts = true; // Block raycasts to elements behind

        // Add Image component to visually block the screen
        Image blockerImage = blockerPanel.AddComponent<Image>();
        blockerImage.color = new Color(0, 0, 0, 0.5f); // Semi-transparent black

        // Set the RectTransform to cover the entire screen
        RectTransform rectTransform = blockerPanel.GetComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }

    // Open a panel and automatically close all others
    public void TogglePanel(GameObject panelToOpen)
    {
        foreach (var panel in modalPanels)
        {
            if (panel != panelToOpen)
            {
                panel.SetActive(false);
            }
        }

        bool isCurrentlyActive = panelToOpen.activeSelf;

        if (!isCurrentlyActive)
        {
            panelToOpen.SetActive(true);
            panelToOpen.transform.SetAsLastSibling();
            activePanel = panelToOpen;
            ShowBlocker(); // Jika kamu masih pakai blocker, atau bisa dihapus
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

            // Add panel state monitor to track open/close state
            PanelStateMonitor monitor = panel.GetComponent<PanelStateMonitor>();
            if (monitor == null)
            {
                monitor = panel.AddComponent<PanelStateMonitor>();
                monitor.Initialize(this);
            }

            // Make sure panel is initially closed
            panel.SetActive(false);
        }
    }

    public void BlockGameInput()
    {
        blockerPanel.SetActive(true);
    }

    public void UnblockGameInput()
    {
        blockerPanel.SetActive(false);
    }


    // Open a specific panel and close any other open panels
    public void OpenPanel(GameObject panel)
    {
        // If trying to open the same panel that's already active, do nothing
        if (panel == activePanel && panel.activeSelf)
            return;

        // Close currently active panel if there is one
        if (activePanel != null && activePanel != panel)
        {
            activePanel.SetActive(false);
        }

        // Set and activate the new panel
        activePanel = panel;
        panel.SetActive(true);

        // Ensure the panel is at the front
        panel.transform.SetAsLastSibling();

        // Show blocker behind the panel
        ShowBlocker();
    }

    // Close the currently active panel
    public void CloseActivePanel()
    {
        if (activePanel != null)
        {
            activePanel.SetActive(false);
            activePanel = null;
            HideBlocker();
        }
    }

    // Close a specific panel
    public void ClosePanel(GameObject panel)
    {
        if (panel == activePanel)
        {
            panel.SetActive(false);
            activePanel = null;
            HideBlocker();
        }
    }

    // Show the blocker panel
    private void ShowBlocker()
    {
        if (blockerPanel != null)
        {
            blockerPanel.SetActive(true);
            blockerPanel.transform.SetSiblingIndex(transform.childCount - 2); // Just below the active panel
        }
    }

    // Hide the blocker panel
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
        // If a different panel opens than our tracked active panel
        if (activePanel != null && activePanel != panel)
        {
            // Close the previously active panel
            activePanel.SetActive(false);
        }

        activePanel = panel;
        panel.transform.SetAsLastSibling();
        ShowBlocker();
    }

    // Called by the PanelStateMonitor when a panel is disabled
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