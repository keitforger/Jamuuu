using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine.SceneManagement;

[System.Serializable]
public class JamuOrder
{
    [Header("Basic Order Info")]
    public string orderID;
    public string requestedJamuName;
    public string customerName;
    public float orderTime;

    [Header("Order Settings")]
    public int baseReward = 50;
    public bool isUrgent = false;
    public float timeLimit = 120f; // 2 minutes default

    [Header("Status")]
    public OrderStatus status = OrderStatus.Pending;

    // Simple constructor
    public JamuOrder(string jamuName, string customer = "Customer")
    {
        orderID = System.Guid.NewGuid().ToString().Substring(0, 6);
        requestedJamuName = jamuName;
        customerName = customer;
        orderTime = Time.time;

        // Random chance for urgent order
        if (Random.value < 0.3f) // 30% chance
        {
            isUrgent = true;
            timeLimit = 60f; // 1 minute for urgent
            baseReward = Mathf.RoundToInt(baseReward * 1.5f);
        }
    }

    public bool IsExpired()
    {
        return (Time.time - orderTime) > timeLimit;
    }

    public float GetRemainingTime()
    {
        return Mathf.Max(0, timeLimit - (Time.time - orderTime));
    }

    public string GetOrderDescription()
    {
        string desc = $"{customerName} wants: {requestedJamuName}";
        if (isUrgent) desc += "\n⚡ URGENT!";
        return desc;
    }

    public Color GetOrderColor()
    {
        if (isUrgent) return Color.red;
        if (GetRemainingTime() < 30f) return Color.yellow;
        return Color.white;
    }
}

[System.Serializable]
public enum OrderStatus
{
    Pending,
    InProgress,
    Completed,
    Failed,
    Expired
}

public class OrderManager : MonoBehaviour
{
    public static OrderManager Instance { get; private set; }

    [Header("Order Settings")]
    public int maxActiveOrders = 3;
    public float orderSpawnInterval = 30f; // New order every 30 seconds
    public List<JamuOrder> activeOrders = new List<JamuOrder>();

    [Header("Customer Names")]
    public NPCSpawner npcSpawner;

    public System.Action<JamuOrder> OnOrderStatusChanged;
    public System.Action<JamuOrder> OnOrderProgressStarted;
    public System.Action<JamuOrder> OnOrderCancelled;
    public System.Action<JamuOrder> OnOrderCompleted;
    public System.Action<JamuOrder> OnOrderAdded;
    public System.Action<JamuOrder> OnOrderExpired;

    private float lastOrderTime;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        lastOrderTime = Time.time;
    }

    void Update()
    {
        // Auto-generate orders
        if (Time.time - lastOrderTime > orderSpawnInterval && activeOrders.Count < maxActiveOrders)
        {
            GenerateRandomOrder();
            lastOrderTime = Time.time;
        }

        // Check for expired orders
        CheckExpiredOrders();
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        AutoAssignNPCSpawner();
    }

    void AutoAssignNPCSpawner()
    {
        if (npcSpawner == null)
        {
            npcSpawner = FindAnyObjectByType<NPCSpawner>();
            if (npcSpawner == null)
                Debug.LogError("OrderManager: NPCSpawner not found in scene!");
            else
                Debug.Log("OrderManager: NPCSpawner auto-assigned.");
        }
    }
    void GenerateRandomOrder()
    {
        var jamuSystem = JamuSystem.Instance;
        var almanac = AlmanacSystem.Instance;
        if (jamuSystem == null || almanac == null) return;

        // Pilih NPC random
        var npcList = npcSpawner.npcDatabase.npcList;
        if (npcList.Count == 0) return;
        var npc = npcList[Random.Range(0, npcList.Count)];

        // Daftar preferred jamu NPC yang sudah ditemukan di Almanac
        var possibleJamuNames = npc.preferredJamuNames
            .Where(jamuName => almanac.HasDiscovered(jamuName))
            .ToList();

        if (possibleJamuNames.Count == 0) return; // NPC ini belum tahu jamu apa pun

        // Pilih jamu dari preferred jamu
        var selectedJamuName = possibleJamuNames[Random.Range(0, possibleJamuNames.Count)];

        var order = new JamuOrder(selectedJamuName, npc.npcName);
        AddOrder(order);
    }

    public bool AddOrder(JamuOrder order)
    {
        if (activeOrders.Count >= maxActiveOrders)
        {
            Debug.LogWarning("Cannot add order: Maximum active orders reached");
            return false;
        }

        activeOrders.Add(order);
        OnOrderAdded?.Invoke(order);

        Debug.Log($"New Order: {order.GetOrderDescription()}");
        return true;
    }

    public void SetOrderInProgress(string jamuName)
    {
        JamuOrder order = activeOrders.FirstOrDefault(o =>
            o.requestedJamuName == jamuName && o.status == OrderStatus.Pending);

        if (order != null)
        {
            order.status = OrderStatus.InProgress;
            Debug.Log($"Order status changed to InProgress: {order.orderID}");

            // Optional: Trigger UI update
            OnOrderStatusChanged?.Invoke(order);
        }
    }

    public bool TryCompleteOrder(string jamuName)
    {
        // Cari order yang pending atau in progress
        JamuOrder matchingOrder = activeOrders.FirstOrDefault(order =>
            order.requestedJamuName == jamuName &&
            (order.status == OrderStatus.Pending || order.status == OrderStatus.InProgress));

        if (matchingOrder != null)
        {
            // Set status to InProgress first (jika masih pending)
            if (matchingOrder.status == OrderStatus.Pending)
            {
                matchingOrder.status = OrderStatus.InProgress;
                Debug.Log($"Order {matchingOrder.orderID} set to InProgress");
            }

            // Complete the order
            CompleteOrder(matchingOrder, true);
            Debug.Log($"Order {matchingOrder.orderID} completed for jamu: {jamuName}");
            return true;
        }

        Debug.Log($"No matching order found for: {jamuName}");
        return false;
    }

    public void TrackOrderProgress(string jamuName)
    {
        JamuOrder order = activeOrders.FirstOrDefault(o =>
            o.requestedJamuName == jamuName && o.status == OrderStatus.Pending);

        if (order != null)
        {
            order.status = OrderStatus.InProgress;
            Debug.Log($"Started working on order: {order.GetOrderDescription()}");

            // Optional: Show progress indicator
            OnOrderProgressStarted?.Invoke(order);
        }
    }

    public bool HasOrderForJamu(string jamuName)
    {
        return activeOrders.Any(order =>
            order.requestedJamuName == jamuName &&
            (order.status == OrderStatus.Pending || order.status == OrderStatus.InProgress));
    }

    // 5. Method untuk mendapatkan order berdasarkan status
    public List<JamuOrder> GetOrdersByStatus(OrderStatus status)
    {
        return activeOrders.Where(order => order.status == status).ToList();
    }

    // 6. Method untuk mendapatkan count order berdasarkan status
    public int GetOrderCountByStatus(OrderStatus status)
    {
        return activeOrders.Count(order => order.status == status);
    }

    // 7. Method untuk cancel order
    public bool CancelOrder(string orderID)
    {
        JamuOrder order = activeOrders.FirstOrDefault(o => o.orderID == orderID);
        if (order != null)
        {
            order.status = OrderStatus.Failed;
            activeOrders.Remove(order);
            Debug.Log($"Order cancelled: {orderID}");

            // Optional: Trigger penalty atau notification
            OnOrderCancelled?.Invoke(order);
            return true;
        }
        return false;
    }

    // 8. Method untuk mendapatkan order yang paling urgent
    public JamuOrder GetMostUrgentOrder()
    {
        return activeOrders
            .Where(o => o.status == OrderStatus.Pending || o.status == OrderStatus.InProgress)
            .OrderByDescending(o => o.isUrgent)
            .ThenBy(o => o.GetRemainingTime())
            .FirstOrDefault();
    }

    // 9. Method untuk mendapatkan semua order dari NPC tertentu
    public List<JamuOrder> GetOrdersFromCustomer(string customerName)
    {
        return activeOrders.Where(o => o.customerName == customerName).ToList();
    }

    // 12. Method untuk mengecek apakah order hampir expired
    public List<JamuOrder> GetExpiringOrders(float thresholdMinutes = 2f)
    {
        return activeOrders.Where(order =>
            (order.status == OrderStatus.Pending || order.status == OrderStatus.InProgress) &&
            order.GetRemainingTime() <= thresholdMinutes * 60f).ToList();
    }

    // 13. Method untuk auto-fail expired orders
    public void CheckAndFailExpiredOrders()
    {
        var expiredOrders = activeOrders.Where(order =>
            (order.status == OrderStatus.Pending || order.status == OrderStatus.InProgress) &&
            order.GetRemainingTime() <= 0).ToList();

        foreach (var order in expiredOrders)
        {
            order.status = OrderStatus.Failed;
            Debug.Log($"Order expired: {order.orderID} - {order.requestedJamuName}");

            // Optional: Apply penalty or consequences
            OnOrderExpired?.Invoke(order);
        }

        // Remove expired orders
        activeOrders.RemoveAll(o => expiredOrders.Contains(o));
    }

    // 15. Method untuk statistik
    public OrderStatistics GetOrderStatistics()
    {
        return new OrderStatistics
        {
            TotalOrders = activeOrders.Count,
            PendingOrders = GetOrderCountByStatus(OrderStatus.Pending),
            InProgressOrders = GetOrderCountByStatus(OrderStatus.InProgress),
            CompletedOrders = GetOrderCountByStatus(OrderStatus.Completed),
            FailedOrders = GetOrderCountByStatus(OrderStatus.Failed)
        };
    }

    public bool TryFailOrderWithJamuGagal(string customerName)
    {
        JamuOrder matchingOrder = activeOrders.FirstOrDefault(order =>
            order.customerName == customerName &&
            (order.status == OrderStatus.Pending || order.status == OrderStatus.InProgress));

        if (matchingOrder != null)
        {
            matchingOrder.status = OrderStatus.Failed;
            activeOrders.Remove(matchingOrder);

            Debug.Log($"Order gagal karena Jamu Gagal diberikan ke: {customerName}");

            OnOrderStatusChanged?.Invoke(matchingOrder);
            OnOrderCancelled?.Invoke(matchingOrder); // Optional
            return true;
        }

        return false;
    }

    public void CompleteOrder(JamuOrder order, bool isCorrect)
    {
        if (!activeOrders.Contains(order)) return;

        order.status = isCorrect ? OrderStatus.Completed : OrderStatus.Failed;

        if (isCorrect)
        {
            // Calculate reward with time bonus
            int finalReward = order.baseReward;
            float remainingTime = order.GetRemainingTime();

            if (remainingTime > order.timeLimit * 0.5f) // Completed in first half of time
            {
                finalReward = Mathf.RoundToInt(finalReward * 1.2f); // 20% bonus
            }

            // Give reward to player (integrate with your currency system)
            GameManager.instance?.AddMoney(finalReward);

            Debug.Log($"Order completed! Reward: {finalReward}");
        }

        activeOrders.Remove(order);
        OnOrderCompleted?.Invoke(order);
    }

    void CheckExpiredOrders()
    {
        List<JamuOrder> expiredOrders = new List<JamuOrder>();

        foreach (var order in activeOrders)
        {
            if (order.IsExpired())
            {
                expiredOrders.Add(order);
            }
        }

        foreach (var expiredOrder in expiredOrders)
        {
            expiredOrder.status = OrderStatus.Expired;
            activeOrders.Remove(expiredOrder);
            OnOrderExpired?.Invoke(expiredOrder);

            Debug.Log($"Order expired: {expiredOrder.orderID}");
        }
    }

    // Manual order creation for testing
    [ContextMenu("Generate Test Order")]
    public void GenerateTestOrder()
    {
        GenerateRandomOrder();
    }

    public void ClearAllOrders()
    {
        activeOrders.Clear();
        Debug.Log("All orders cleared");
    }

    // Get orders for UI display
    public List<JamuOrder> GetActiveOrders()
    {
        return new List<JamuOrder>(activeOrders);
    }

    // ====== METHODS ADDED FROM ORDERMANAGER1 ======
    /// <summary>
    /// Get all orders that are currently expired (status Expired)
    /// </summary>
    public List<JamuOrder> GetAllExpiredOrders()
    {
        return activeOrders.Where(order => order.status == OrderStatus.Expired).ToList();
    }

    /// <summary>
    /// Get all orders that are currently completed (status Completed)
    /// </summary>
    public List<JamuOrder> GetAllCompletedOrders()
    {
        return activeOrders.Where(order => order.status == OrderStatus.Completed).ToList();
    }

    /// <summary>
    /// Get all orders that are currently failed (status Failed)
    /// </summary>
    public List<JamuOrder> GetAllFailedOrders()
    {
        return activeOrders.Where(order => order.status == OrderStatus.Failed).ToList();
    }

    /// <summary>
    /// Get all orders that are currently in progress (status InProgress)
    /// </summary>
    public List<JamuOrder> GetAllInProgressOrders()
    {
        return activeOrders.Where(order => order.status == OrderStatus.InProgress).ToList();
    }

    /// <summary>
    /// Get all orders that are currently pending (status Pending)
    /// </summary>
    public List<JamuOrder> GetAllPendingOrders()
    {
        return activeOrders.Where(order => order.status == OrderStatus.Pending).ToList();
    }

    /// <summary>
    /// Get the current number of active (not completed, not failed, not expired) orders
    /// </summary>
    public int GetActiveOrderCount()
    {
        return activeOrders.Count(order => order.status == OrderStatus.Pending || order.status == OrderStatus.InProgress);
    }
    // =================================================
}

[System.Serializable]
public struct OrderStatistics
{
    public int TotalOrders;
    public int PendingOrders;
    public int InProgressOrders;
    public int CompletedOrders;
    public int FailedOrders;
}

// Simple extension to JamuSystem for order integration
public static class JamuSystemOrderExtension
{
    /// <summary>
    /// Create jamu and try to complete any matching orders
    /// </summary>
    public static bool CreateJamuAndCompleteOrder(this JamuSystem jamuSystem, string jamuName)
    {
        // First create the jamu normally
        bool jamuCreated = jamuSystem.CreateJamu(jamuName);

        if (jamuCreated)
        {
            // Try to complete matching order (hanya jika ada OrderManager)
            if (OrderManager.Instance != null)
            {
                // Set order to InProgress first if it exists
                OrderManager.Instance.TrackOrderProgress(jamuName);

                // Then try to complete it
                bool orderCompleted = OrderManager.Instance.TryCompleteOrder(jamuName);

                if (orderCompleted)
                {
                    Debug.Log($"Jamu created and order completed for: {jamuName}");
                }
                else
                {
                    Debug.Log($"Jamu created but no matching order for: {jamuName}");
                }

                return orderCompleted; // Return true only if order was completed
            }
        }

        return jamuCreated;
    }

    /// <summary>
    /// Create jamu from combination and try to complete orders
    /// </summary>
    public static bool CreateJamuFromCombinationAndCompleteOrder(this JamuSystem jamuSystem, string[] bahanKombinasi)
    {
        // Check if combination makes a valid recipe
        if (jamuSystem.IsValidRecipe(bahanKombinasi))
        {
            // Find the matching recipe
            foreach (ResepJamu recipe in jamuSystem.jamuDatabase.resepJamus)
            {
                if (recipe.bahanResep != null && bahanKombinasi != null &&
                    recipe.bahanResep.Length == bahanKombinasi.Length &&
                    recipe.bahanResep.All(bahan => bahanKombinasi.Contains(bahan)) &&
                    bahanKombinasi.All(bahan => recipe.bahanResep.Contains(bahan)))
                {
                    return jamuSystem.CreateJamuAndCompleteOrder(recipe.jamuName);
                }
            }
        }
        else
        {
            // Create failed jamu (no order completion for failed jamu)
            return jamuSystem.CreateSingleJamuGagal(bahanKombinasi);
        }

        return false;
    }
}

[CreateAssetMenu(fileName = "NPCDatabase", menuName = "Jamu Game/NPC Database")]
public class NPCDatabase : ScriptableObject
{
    public List<NPCData> npcList = new List<NPCData>();
}

[System.Serializable]
public class NPCData
{
    public string npcName;
    public Sprite npcSprite;
    public Color npcColor = Color.white;

    [Header("Home/House Settings")]
    public Transform homeTransform; // The house or home position for this NPC
    public Transform spawnTransform; // (Optional) The spawn position for this NPC
    public string homeName; // (Optional) For display or logic

    [Header("Behavior")]
    public float moveSpeed = 2f;
    public float homeSpeed = 3f;
    public int minReward = 40;
    public int maxReward = 100;

    [Header("Sprite Animations")]
    public Sprite[] walkDownSprites;
    public Sprite[] walkUpSprites;
    public Sprite[] walkLeftSprites;
    public Sprite[] walkRightSprites;
    public Sprite idleSprite; // Single sprite for idle ("stop") state

    [Header("Personality")]
    [TextArea(2, 5)] public string greeting;
    [TextArea(2, 5)] public string thankYouMessage;
    [TextArea(2, 5)] public string disappointedMessage;

    [Header("Preferences")]
    public List<string> preferredJamuNames;
    public float preferenceWeight = 0.7f;
}

public enum JamuType { Kunyit, Jahe, Temulawak, Kencur, Beras }

[CustomEditor(typeof(NPCDatabase))]
public class NPCDatabaseEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        NPCDatabase db = (NPCDatabase)target;

        EditorGUILayout.LabelField("NPC Database", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        for (int i = 0; i < db.npcList.Count; i++)
        {
            var npc = db.npcList[i];
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"NPC {i + 1}", EditorStyles.boldLabel);

            if (GUILayout.Button("Remove", GUILayout.Width(70)))
            {
                db.npcList.RemoveAt(i);
                EditorUtility.SetDirty(target);
                GUIUtility.ExitGUI();
                return;
            }
            EditorGUILayout.EndHorizontal();

            npc.npcName = EditorGUILayout.TextField("Name", npc.npcName);
            npc.npcSprite = (Sprite)EditorGUILayout.ObjectField("Sprite", npc.npcSprite, typeof(Sprite), false);
            npc.homeTransform = (Transform)EditorGUILayout.ObjectField("Home Transform", npc.homeTransform, typeof(Transform), true);
            npc.spawnTransform = (Transform)EditorGUILayout.ObjectField("Spawn Transform", npc.spawnTransform, typeof(Transform), true);
            npc.homeName = EditorGUILayout.TextField("Home Name", npc.homeName);
            SerializedProperty walkDown = serializedObject.FindProperty("npcList").GetArrayElementAtIndex(i).FindPropertyRelative("walkDownSprites");
            EditorGUILayout.PropertyField(walkDown, new GUIContent("Walk Down Sprites"), true);
            SerializedProperty walkUp = serializedObject.FindProperty("npcList").GetArrayElementAtIndex(i).FindPropertyRelative("walkUpSprites");
            EditorGUILayout.PropertyField(walkUp, new GUIContent("Walk Up Sprites"), true);
            SerializedProperty walkLeft = serializedObject.FindProperty("npcList").GetArrayElementAtIndex(i).FindPropertyRelative("walkLeftSprites");
            EditorGUILayout.PropertyField(walkLeft, new GUIContent("Walk Left Sprites"), true);
            SerializedProperty walkRight = serializedObject.FindProperty("npcList").GetArrayElementAtIndex(i).FindPropertyRelative("walkRightSprites");
            EditorGUILayout.PropertyField(walkRight, new GUIContent("Walk Right Sprites"), true);
            npc.idleSprite = (Sprite)EditorGUILayout.ObjectField("Idle Sprite", npc.idleSprite, typeof(Sprite), false);
            npc.npcColor = EditorGUILayout.ColorField("Color", npc.npcColor);
            npc.moveSpeed = EditorGUILayout.FloatField("Move Speed", npc.moveSpeed);
            npc.homeSpeed = EditorGUILayout.FloatField("Home Speed", npc.homeSpeed);
            npc.minReward = EditorGUILayout.IntField("Min Reward", npc.minReward);
            npc.maxReward = EditorGUILayout.IntField("Max Reward", npc.maxReward);
            npc.greeting = EditorGUILayout.TextArea(npc.greeting, GUILayout.Height(40));
            npc.thankYouMessage = EditorGUILayout.TextArea(npc.thankYouMessage, GUILayout.Height(40));
            npc.disappointedMessage = EditorGUILayout.TextArea(npc.disappointedMessage, GUILayout.Height(40));
            npc.preferenceWeight = EditorGUILayout.Slider("Preference Weight", npc.preferenceWeight, 0f, 1f);

            SerializedProperty enumArray = serializedObject.FindProperty("npcList").GetArrayElementAtIndex(i).FindPropertyRelative("preferredJamuNames");
            EditorGUILayout.PropertyField(enumArray, new GUIContent("Preferred Jamu Names"), true);

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }

        if (GUILayout.Button("Add New NPC"))
        {
            db.npcList.Add(new NPCData());
        }

        serializedObject.ApplyModifiedProperties();
    }
}