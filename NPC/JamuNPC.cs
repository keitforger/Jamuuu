using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Linq;
using TMPro;
using System.Collections.Generic;

public class JamuNPC : MonoBehaviour
{
    [Header("NPC Data")]
    public NPCData npcData;

    [Header("Components")]
    public Image requestIcon;
    public Canvas requestCanvas;
    public CraftingManager craftingManager;
    public SpriteRenderer npcSpriteRenderer;

    [Header("Queue System")]
    public int queuePosition = -1;
    public bool isInQueue = false;
    public Transform assignedQueuePosition;

    [Header("Interaction")]
    public float interactionDistance = 2f;

    [Header("Timeout Settings")]
    public float waitTimeout = 30f; // seconds, set as needed (or get from order)
    private float waitTimer = 0f;
    private bool waitingForService = false;

    // Private variables
    private ResepJamu requestedJamuResep;
    private Transform player;
    private bool canInteract = false;
    private bool hasReachedDestination = false;
    private bool isGoingHome = false;
    private bool isMovingToQueue = false;
    private Transform assignedHomePosition;
    private Color originalColor;
    private int rewardAmount;
    public Sprite correctIcon;
    public Sprite wrongIcon;

    // Sprite animation system
    private float animationTimer = 0f;
    public float animationFrameRate = 0.15f; // seconds per frame
    private int currentFrame = 0;

    // Tambahan untuk toggle crafting panel
    private bool isCraftingPanelOpen = false;

    // State management
    public enum NPCState
    {
        MovingToQueue,
        InQueue,
        Interacting,
        GoingHome,
        AtHome
    }
    public NPCState currentState = NPCState.MovingToQueue;

    public enum Direction { Down, Up, Left, Right }
    private Direction lastDirection = Direction.Down;

    void Start()
    {
        InitializeNPC();
        FindReferences();
    }

    void InitializeNPC()
    {
        if (npcData != null)
        {
            // Set sprite dan warna
            if (npcSpriteRenderer != null)
            {
                npcSpriteRenderer.sprite = npcData.idleSprite != null ? npcData.idleSprite : npcData.npcSprite;
                npcSpriteRenderer.color = npcData.npcColor;
                originalColor = npcData.npcColor;
            }

            // Generate jamu request dari preferensi saja, jika ada
            CreateSmartJamuRequest();

            // Set reward sesuai value jamu dari database
            rewardAmount = (requestedJamuResep != null) ? requestedJamuResep.jamuValue : 10;
        }
        else
        {
            Debug.LogError("NPCData tidak di-assign untuk " + gameObject.name);
        }
    }

    void FindReferences()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (player == null)
        {
            Debug.LogError("Player tidak ditemukan!");
        }

        if (craftingManager == null)
        {
            craftingManager = FindAnyObjectByType<CraftingManager>();
        }

        requestCanvas?.gameObject.SetActive(false);
    }

    void Update()
    {
        switch (currentState)
        {
            case NPCState.MovingToQueue:
                HandleMovingToQueue();
                break;
            case NPCState.InQueue:
                HandleInQueue();
                HandleWaitTimeout();
                break;
            case NPCState.Interacting:
                // Handled by interaction system
                break;
            case NPCState.GoingHome:
                // Handled by coroutine
                break;
        }
    }

    void HandleWaitTimeout()
    {
        if (queuePosition == 0 && waitingForService)
        {
            waitTimer += Time.deltaTime;
            if (waitTimer >= waitTimeout)
            {
                Debug.Log($"{npcData.npcName} waited too long and is leaving!");
                waitingForService = false;
                HandleTimeoutLeave();
            }
        }
    }

    void HandleTimeoutLeave()
    {
        // Fail the order for this NPC if applicable
        OrderManager.Instance?.TryFailOrderWithJamuGagal(npcData.npcName);

        // Could play a disappointed animation/effect here
        ShowFailureEffect();

        // Notify spawner and go home
        var spawner = FindAnyObjectByType<NPCSpawner>();
        spawner?.OnNPCCompleted(this);

        StartCoroutine(DelayedGoHome());
    }

    void HandleMovingToQueue()
    {
        if (assignedQueuePosition != null)
        {
            Vector3 target = assignedQueuePosition.position;
            float speed = npcData.moveSpeed * Time.deltaTime;

            // Move in X first, then Y
            Vector3 current = transform.position;
            Vector2 moveDir = Vector2.zero;

            if (Mathf.Abs(current.x - target.x) > 0.05f)
            {
                float newX = Mathf.MoveTowards(current.x, target.x, speed);
                transform.position = new Vector3(newX, current.y, current.z);
                moveDir = new Vector2(target.x - current.x, 0);
            }
            else if (Mathf.Abs(current.y - target.y) > 0.05f)
            {
                float newY = Mathf.MoveTowards(current.y, target.y, speed);
                transform.position = new Vector3(target.x, newY, current.z);
                moveDir = new Vector2(0, target.y - current.y);
            }
            else
            {
                currentState = NPCState.InQueue;
                hasReachedDestination = true;
                SetIdleSprite();
                OnReachedQueue();
                return;
            }

            UpdateSpriteAnimation(moveDir);
        }
    }

    void UpdateSpriteAnimation(Vector2 moveDir)
    {
        Sprite[] sprites = null;
        Direction dir = lastDirection;

        if (moveDir.x > 0.1f)
        {
            sprites = npcData.walkRightSprites;
            dir = Direction.Right;
        }
        else if (moveDir.x < -0.1f)
        {
            sprites = npcData.walkLeftSprites;
            dir = Direction.Left;
        }
        else if (moveDir.y > 0.1f)
        {
            sprites = npcData.walkUpSprites;
            dir = Direction.Up;
        }
        else if (moveDir.y < -0.1f)
        {
            sprites = npcData.walkDownSprites;
            dir = Direction.Down;
        }

        if (sprites != null && sprites.Length > 0)
        {
            animationTimer += Time.deltaTime;
            if (animationTimer >= animationFrameRate)
            {
                animationTimer = 0f;
                currentFrame = (currentFrame + 1) % sprites.Length;
            }
            npcSpriteRenderer.sprite = sprites[currentFrame];
            lastDirection = dir;
        }
        else
        {
            SetIdleSprite();
        }
    }

    void SetIdleSprite()
    {
        if (npcData.idleSprite != null)
        {
            npcSpriteRenderer.sprite = npcData.idleSprite;
        }
        else
        {
            switch (lastDirection)
            {
                case Direction.Right:
                    npcSpriteRenderer.sprite = npcData.walkRightSprites.Length > 0 ? npcData.walkRightSprites[0] : null;
                    break;
                case Direction.Left:
                    npcSpriteRenderer.sprite = npcData.walkLeftSprites.Length > 0 ? npcData.walkLeftSprites[0] : null;
                    break;
                case Direction.Up:
                    npcSpriteRenderer.sprite = npcData.walkUpSprites.Length > 0 ? npcData.walkUpSprites[0] : null;
                    break;
                case Direction.Down:
                default:
                    npcSpriteRenderer.sprite = npcData.walkDownSprites.Length > 0 ? npcData.walkDownSprites[0] : null;
                    break;
            }
        }
        animationTimer = 0f;
        currentFrame = 0;
    }

    void HandleInQueue()
    {
        if (queuePosition == 0 && !requestCanvas.gameObject.activeSelf)
        {
            ShowRequestIcon();
        }
        if (player != null)
        {
            float distance = Vector2.Distance(transform.position, player.position);
            canInteract = (distance <= interactionDistance); // For interaction, not for bubble
        }
    }

    void CheckPlayerInteraction()
    {
        if (player == null) return;

        float distance = Vector2.Distance(transform.position, player.position);

        if (distance <= interactionDistance)
        {
            if (!canInteract)
            {
                canInteract = true;
                ShowRequestIcon();
            }
        }
        else
        {
            if (canInteract)
            {
                canInteract = false;
                HideRequestIcon();
            }
        }
    }

    public void SetQueuePosition(int position, Transform queueTransform)
    {
        queuePosition = position;
        assignedQueuePosition = queueTransform;
        isInQueue = true;
        currentState = NPCState.MovingToQueue;
    }

    void OnReachedQueue()
    {
        SetIdleSprite();
        if (queuePosition == 0)
        {
            ShowRequestIcon();

            // Try to get the order time limit for this NPC's request
            var order = OrderManager.Instance?.GetOrdersFromCustomer(npcData.npcName)
                        .FirstOrDefault(o => o.requestedJamuName == requestedJamuResep.jamuName);
            if (order != null)
            {
                waitTimeout = order.timeLimit;
            }

            StartWaitTimer();
        }
        else
        {
            waitingForService = false;
        }
    }

    void StartWaitTimer()
    {
        waitTimer = 0f;
        waitingForService = true;
    }

    public void MoveUpInQueue(int newPosition, Transform newQueueTransform)
    {
        queuePosition = newPosition;
        assignedQueuePosition = newQueueTransform;
        currentState = NPCState.MovingToQueue;

        if (newPosition == 0)
        {
            canInteract = true;
        }
    }

    void CreateSmartJamuRequest()
    {
        if (JamuSystem.Instance?.jamuDatabase?.resepJamus == null ||
            JamuSystem.Instance.jamuDatabase.resepJamus.Count == 0)
        {
            Debug.LogError("Database jamu tidak tersedia!");
            return;
        }

        var jamuDb = JamuSystem.Instance.jamuDatabase;
        var almanac = AlmanacSystem.Instance;

        var discoveredJamus = jamuDb.resepJamus
            .Where(jamu => almanac != null
                           && almanac.HasDiscovered(jamu.jamuName)
                           && !jamu.jamuName.Trim().ToLower().Contains("gagal"))
            .ToList();

        List<ResepJamu> candidateJamus;

        if (npcData.preferredJamuNames != null && npcData.preferredJamuNames.Count > 0)
        {
            candidateJamus = discoveredJamus
                .Where(jamu => npcData.preferredJamuNames
                    .Any(pref => jamu.jamuName.Trim().ToLower() == pref.Trim().ToLower()))
                .ToList();
        }
        else
        {
            candidateJamus = discoveredJamus;
        }

        if (candidateJamus.Count > 0)
        {
            requestedJamuResep = candidateJamus[Random.Range(0, candidateJamus.Count)];
        }
        else
        {
            requestedJamuResep = jamuDb.resepJamus[Random.Range(0, jamuDb.resepJamus.Count)];
            Debug.LogWarning($"[JamuNPC] {npcData.npcName}: Tidak ada jamu eligible, fallback random.");
        }

        if (requestIcon != null && requestedJamuResep != null)
        {
            requestIcon.sprite = requestedJamuResep.jamuSprite;
        }
    }

    public void GiveJamuGagalToNPC(JamuGagal jamuGagal)
    {
        Debug.Log($"{npcData.npcName} menerima jamu gagal: {jamuGagal.itemName}");

        HandleWrongJamu(); // Beri reaksi kecewa
        OrderManager.Instance?.TryFailOrderWithJamuGagal(npcData.npcName); // Gagalkan order

        var spawner = FindAnyObjectByType<NPCSpawner>();
        spawner?.OnNPCCompleted(this);

        StartCoroutine(DelayedGoHome());
    }

    void ShowRequestIcon()
    {
        requestCanvas?.gameObject.SetActive(true);
    }

    void HideRequestIcon()
    {
        requestCanvas?.gameObject.SetActive(false);
    }

    public void OnNPCClicked()
    {
        Debug.Log($"OnNPCClicked: isCraftingPanelOpen={isCraftingPanelOpen}");
        if (IsInteractable())
        {
            if (isCraftingPanelOpen)
            {
                Debug.Log("OnNPCClicked: HideCraftingPanel dipanggil");
                HideCraftingPanel();
            }
            else
            {
                Debug.Log("OnNPCClicked: ShowCraftingPanel dipanggil");
                ShowCraftingPanel();
            }
        }
    }

    void ShowCraftingPanel()
    {
        if (craftingManager != null && npcData != null)
        {
            craftingManager.gameObject.SetActive(true);
            craftingManager.Initialize(this);
            isCraftingPanelOpen = true;
            Debug.Log("ShowCraftingPanel selesai, isCraftingPanelOpen=true");
        }
    }

    public void HideCraftingPanel()
    {
        if (craftingManager != null)
        {
            craftingManager.ClosePanel();
        }
        isCraftingPanelOpen = false;
        Debug.Log("HideCraftingPanel selesai, isCraftingPanelOpen=false");
    }

    public void NotifyCraftingPanelClosed()
    {
        isCraftingPanelOpen = false;
        Debug.Log("NotifyCraftingPanelClosed: isCraftingPanelOpen=false");
    }

    public void SetRequestedJamu(ResepJamu resep, JamuOrder order = null)
    {
        this.requestedJamuResep = resep;
        if (requestIcon != null && resep != null)
            requestIcon.sprite = resep.jamuSprite;
        if (order != null)
        {
            rewardAmount = order.baseReward;
        }
        else
        {
            rewardAmount = (resep != null) ? resep.jamuValue : 10;
        }
    }

    public void GiveJamuToNPC(ResepJamu givenJamuResep)
    {
        bool isCorrectJamu = (givenJamuResep != null && requestedJamuResep != null &&
                             givenJamuResep.jamuName == requestedJamuResep.jamuName);

        if (isCorrectJamu)
        {
            HandleCorrectJamu();
        }
        else
        {
            HandleWrongJamu();
        }

        var spawner = FindAnyObjectByType<NPCSpawner>();
        spawner?.OnNPCCompleted(this);

        StartCoroutine(DelayedGoHome());
    }

    void HandleCorrectJamu()
    {
        var gameManager = FindAnyObjectByType<GameManager>();
        if (gameManager != null)
        {
            gameManager.AddMoney(rewardAmount);
        }
        else
        {
            var dtg = ManagerPP<DataGame>.Get("datagame");
            if (dtg != null)
            {
                dtg.koin += rewardAmount;
                ManagerPP<DataGame>.Set("datagame", dtg);
            }
        }

        Debug.Log(npcData.npcName + ": " + npcData.thankYouMessage);
        ShowSuccessEffect();

        if (requestIcon != null && correctIcon != null)
            requestIcon.sprite = correctIcon;
    }

    void HandleWrongJamu()
    {
        Debug.Log(npcData.npcName + ": " + npcData.disappointedMessage);

        if (npcSpriteRenderer != null)
        {
            npcSpriteRenderer.color = Color.Lerp(originalColor, Color.red, 0.3f);
        }

        ShowFailureEffect();
        if (requestIcon != null && wrongIcon != null)
            requestIcon.sprite = wrongIcon;
    }

    void ShowSuccessEffect()
    {
        Debug.Log("Success effect for " + npcData.npcName);
    }

    void ShowFailureEffect()
    {
        Debug.Log("Failure effect for " + npcData.npcName);
    }

    IEnumerator DelayedGoHome()
    {
        HideCraftingPanel();
        yield return new WaitForSeconds(2f);
        GoHome();
    }

    public void GoHome()
    {
        if (isGoingHome) return;

        currentState = NPCState.GoingHome;
        isGoingHome = true;
        canInteract = false;
        HideRequestIcon();
        HideCraftingPanel();

        if (assignedHomePosition == null)
        {
            CreateDefaultHomePosition();
        }

        StartCoroutine(MoveToHome());
    }

    public void SetHomePosition(Transform homePos)
    {
        assignedHomePosition = homePos;
    }

    void CreateDefaultHomePosition()
    {
        GameObject homePoint = new GameObject("HomePosition_" + npcData.npcName);
        homePoint.transform.position = new Vector3(transform.position.x - 15f, transform.position.y, 0);
        assignedHomePosition = homePoint.transform;
    }

    IEnumerator MoveToHome()
    {
        while (assignedHomePosition != null &&
               Vector2.Distance(transform.position, assignedHomePosition.position) > 0.1f)
        {
            Vector3 target = assignedHomePosition.position;
            float speed = npcData.homeSpeed * Time.deltaTime;

            Vector3 current = transform.position;
            Vector2 moveDir = Vector2.zero;
            if (Mathf.Abs(current.x - target.x) > 0.05f)
            {
                float newX = Mathf.MoveTowards(current.x, target.x, speed);
                transform.position = new Vector3(newX, current.y, current.z);
                moveDir = new Vector2(target.x - current.x, 0);
            }
            else if (Mathf.Abs(current.y - target.y) > 0.05f)
            {
                float newY = Mathf.MoveTowards(current.y, target.y, speed);
                transform.position = new Vector3(target.x, newY, current.z);
                moveDir = new Vector2(0, target.y - current.y);
            }
            else
            {
                break;
            }
            UpdateSpriteAnimation(moveDir);
            yield return null;
        }

        if (assignedHomePosition != null && assignedHomePosition.name.StartsWith("HomePosition_"))
        {
            Destroy(assignedHomePosition.gameObject);
        }

        currentState = NPCState.AtHome;
        SetIdleSprite();

        var spawner = FindAnyObjectByType<NPCSpawner>();
        if (spawner != null)
        {
            Debug.Log($"[JamuNPC] {npcData.npcName} memanggil OnNPCActuallyGone");
            spawner.OnNPCActuallyGone(this);
        }

        Destroy(gameObject);
    }

    // Getter methods
    public string GetNPCName() => npcData?.npcName ?? "Unknown";
    public int GetQueuePosition() => queuePosition;
    public bool IsInteractable() => canInteract && currentState == NPCState.InQueue;
}