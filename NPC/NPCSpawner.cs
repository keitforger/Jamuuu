using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class NPCSpawner : MonoBehaviour
{
    [Header("NPC Setup")]
    public GameObject npcPrefab;
    public NPCDatabase npcDatabase;
    public CraftingManager craftingManager;
    public Transform[] spawnPoints;

    [Header("Queue System")]
    public Transform[] queuePositions = new Transform[3]; // Max 3 NPCs in queue
    public float queueMoveSpeed = 2f;
    private List<JamuNPC> npcQueue = new List<JamuNPC>();

    [Header("Spawning Settings")]
    public float initialSpawnDelay = 3f;
    public float minSpawnInterval = 15f;
    public float maxSpawnInterval = 30f;
    public int maxConcurrentNPCs = 3;

    [Header("Day/Night Cycle")]
    public float dayDuration = 300f; // 5 minutes = 1 day
    public float nightStartHour = 18f; // 6 PM
    public float nightEndHour = 6f; // 6 AM
    private float currentTime = 8f; // Start at 8 AM
    private bool isNightTime = false;
    private bool isShopClosed = false; // NEW: Flag untuk menandai toko tutup (jam 6)

    [Header("Home System")]
    public Transform[] homePositions = new Transform[3];

    [Header("Events")]
    public System.Action<float> OnTimeChanged; // Event for UI updates

    private Coroutine spawnCoroutine;

    [Header("Spawn Trigger Area (2D)")]
    [Tooltip("Drag area trigger GameObject (with Collider2D & isTrigger) ke sini")]
    public Collider2D areaTrigger2D;
    [Tooltip("Tag untuk player")]
    public string playerTag = "Player";
    public bool spawnOnlyWhenPlayerInArea = false;

    private bool isPlayerInSpawnArea = false;
    private bool spawningActive = true;

    void Awake()
    {
        // Auto assign craftingManager jika belum di inspector
        if (craftingManager == null)
        {
            // Cari Game1 di scene
            GameObject game1 = GameObject.Find("Game1");
            if (game1 != null)
            {
                // Cari Canvas di bawah Game1
                var canvas = game1.transform.Find("Canvas");
                if (canvas != null)
                {
                    // Cari NPCCraftingPanel di dalam Canvas (langsung atau rekursif)
                    var craftingPanel = FindChildRecursive(canvas, "NPCCraftingPanel");
                    if (craftingPanel != null)
                    {
                        craftingManager = craftingPanel.GetComponent<CraftingManager>();
                        if (craftingManager == null)
                            Debug.LogWarning("NPCCraftingPanel ditemukan, tapi tidak ada CraftingManager!");
                    }
                    else
                    {
                        Debug.LogWarning("NPCCraftingPanel tidak ditemukan di dalam Canvas!");
                    }
                }
            }
        }
    }

    void Start()
    {
        ValidateReferences();
        spawningActive = !spawnOnlyWhenPlayerInArea; // Hanya aktif jika tidak pakai area
        if (spawningActive) StartSpawning();
    }

    Transform FindChildRecursive(Transform parent, string childName)
    {
        foreach (Transform child in parent)
        {
            if (child.name == childName)
                return child;
            var result = FindChildRecursive(child, childName);
            if (result != null)
                return result;
        }
        return null;
    }

    void ValidateReferences()
    {
        if (npcPrefab == null)
        {
            Debug.LogError("NPC Prefab is missing!");
            return;
        }

        if (npcDatabase.npcList == null || npcDatabase.npcList.Count == 0)
        {
            Debug.LogError("No NPC Data assigned!");
            return;
        }

        if (craftingManager == null)
        {
            Debug.LogError("CraftingManager reference is missing!");
            return;
        }

        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogError("No spawn points assigned!");
            return;
        }

        if (queuePositions == null || queuePositions.Length == 0)
        {
            Debug.LogError("No queue positions assigned!");
            return;
        }

        if (spawnOnlyWhenPlayerInArea && areaTrigger2D == null)
        {
            Debug.LogError("Area Trigger 2D belum di-assign!");
            return;
        }
    }

    void OnEnable()
    {
        // Daftarkan events trigger 2D jika field diisi
        if (spawnOnlyWhenPlayerInArea && areaTrigger2D != null)
        {
            // Gunakan komponen helper untuk relay event trigger ke spawner ini
            var relay = areaTrigger2D.GetComponent<NPCSpawnArea2DRelay>();
            if (relay == null)
                relay = areaTrigger2D.gameObject.AddComponent<NPCSpawnArea2DRelay>();
            relay.spawner = this;
        }
    }

    void StartSpawning()
    {
        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
        }
        if (spawningActive)
            spawnCoroutine = StartCoroutine(SpawnRoutine());
    }

    void Update()
    {
        UpdateDayNightCycle();
    }

    void UpdateDayNightCycle()
    {
        float oldTime = currentTime;
        currentTime += (24f / dayDuration) * Time.deltaTime;

        if (currentTime >= 24f)
        {
            currentTime = 0f;
        }

        // Notify UI about time change
        OnTimeChanged?.Invoke(currentTime);

        // Check for night time
        bool wasNight = isNightTime;
        isNightTime = (currentTime >= nightStartHour || currentTime < nightEndHour);

        // MODIFIKASI: Toko tutup jam 6, order terakhir, NPC tetap dilayani sampai habis, tidak ada NPC baru
        if (!isShopClosed && isNightTime)
        {
            isShopClosed = true;
            Debug.Log($"Toko tutup! (Jam {currentTime:0.00})");
            // Tidak usah SendAllNPCsHome();
            // Cukup hentikan spawn, NPC yang sudah ada tetap dilayani
        }
        // Jika pagi, reset flag
        if (isShopClosed && !isNightTime)
        {
            isShopClosed = false;
        }
    }

    IEnumerator SpawnRoutine()
    {
        yield return new WaitForSeconds(initialSpawnDelay);

        while (true)
        {
            if (CanSpawnNPC())
            {
                SpawnRandomNPC();
            }

            float waitTime = Random.Range(minSpawnInterval, maxSpawnInterval);
            yield return new WaitForSeconds(waitTime);

            // Stop spawning if player leaves area (if using spawnOnlyWhenPlayerInArea)
            if (spawnOnlyWhenPlayerInArea && !isPlayerInSpawnArea)
            {
                spawnCoroutine = null;
                yield break;
            }
        }
    }

    bool CanSpawnNPC()
    {
        if (spawnOnlyWhenPlayerInArea && !isPlayerInSpawnArea)
            return false;

        // MODIFIKASI: Jangan spawn jika toko sudah tutup (jam 6 ke atas)
        if (isShopClosed)
            return false;

        return !isNightTime &&
               npcQueue.Count < maxConcurrentNPCs &&
               npcQueue.Count < queuePositions.Length;
    }

    void SpawnRandomNPC()
    {
        // Choose random spawn point
        Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];

        var list = npcDatabase.npcList;
        NPCData selected = list[Random.Range(0, list.Count)];

        // Instantiate NPC
        GameObject npcObject = Instantiate(npcPrefab, spawnPoint.position, Quaternion.identity);
        JamuNPC npc = npcObject.GetComponent<JamuNPC>();

        if (npc != null)
        {
            // Configure NPC
            npc.npcData = selected;
            npc.craftingManager = craftingManager;

            // Add to queue
            AddNPCToQueue(npc);

            Debug.Log($"Spawned NPC: {selected.npcName} at position {npcQueue.Count - 1}");
        }
        else
        {
            Debug.LogError("Spawned NPC doesn't have JamuNPC component!");
            Destroy(npcObject);
        }
    }

    public void SpawnNPCWithOrder(NPCData npcData, JamuOrder order)
    {
        Transform spawnPoint = spawnPoints[UnityEngine.Random.Range(0, spawnPoints.Length)];
        GameObject npcObject = Instantiate(npcPrefab, spawnPoint.position, Quaternion.identity);
        JamuNPC npc = npcObject.GetComponent<JamuNPC>();
        npc.npcData = npcData;
        npc.craftingManager = craftingManager;

        // Saat spawn NPC dengan order
        var resep = JamuSystem.Instance.jamuDatabase.resepJamus
            .FirstOrDefault(j => j.jamuName == order.requestedJamuName);

        npc.SetRequestedJamu(resep, order);

        AddNPCToQueue(npc);

        Debug.Log($"Spawned NPC (with order): {npcData.npcName} - {order.requestedJamuName}");
    }

    void AddNPCToQueue(JamuNPC npc)
    {
        int queueIndex = npcQueue.Count;

        if (queueIndex < queuePositions.Length)
        {
            npcQueue.Add(npc);

            npc.SetQueuePosition(queueIndex, queuePositions[queueIndex]);

            if (queueIndex < homePositions.Length)
            {
                npc.SetHomePosition(homePositions[queueIndex]);
            }

            Debug.Log($"Added {npc.GetNPCName()} to queue at position {queueIndex}");
        }
        else
        {
            Debug.LogWarning("Queue is full! Cannot add more NPCs.");
            Destroy(npc.gameObject);
            return;
        }

        // Perbaikan: Ambil order dan pasang reward secara akurat
        var order = OrderManager.Instance?.activeOrders
            .FirstOrDefault(o => o.customerName == npc.npcData.npcName && o.status == OrderStatus.Pending);

        if (order != null)
        {
            var resep = JamuSystem.Instance.jamuDatabase.resepJamus
                .FirstOrDefault(j => j.jamuName == order.requestedJamuName);
            npc.SetRequestedJamu(resep, order);
        }
    }


    // --- MODIFIED LOGIC START HERE ---
    // Ini dipanggil dari JamuNPC ketika jamu berhasil/failed, untuk mulai proses pulang
    public void OnNPCCompleted(JamuNPC completedNPC)
    {
        // TIDAK LANGSUNG geser queue di sini!
        // Tunggu sampai NPC benar-benar hilang lewat OnNPCActuallyGone
    }

    // Panggil fungsi ini dari JamuNPC saat NPC sudah benar-benar hilang/destroy (setelah move home)
    public void OnNPCActuallyGone(JamuNPC goneNPC)
    {
        int goneIdx = npcQueue.IndexOf(goneNPC);
        if (goneIdx != -1)
        {
            npcQueue.RemoveAt(goneIdx);
            // Sekarang baru geser queue
            UpdateQueuePositions();
            Debug.Log($"NPC {goneNPC.GetNPCName()} actually gone. Queue updated.");
        }
    }
    // --- MODIFIED LOGIC END HERE ---

    void UpdateQueuePositions()
    {
        for (int i = 0; i < npcQueue.Count; i++)
        {
            if (npcQueue[i] != null && i < queuePositions.Length)
            {
                npcQueue[i].MoveUpInQueue(i, queuePositions[i]);
            }
        }
    }

    // SendAllNPCsHome tetap bisa dipakai untuk force send di context menu/test/manual
    void SendAllNPCsHome()
    {
        Debug.Log("Night time! Sending all NPCs home.");

        List<JamuNPC> npcsToRemove = new List<JamuNPC>(npcQueue);

        foreach (var npc in npcsToRemove)
        {
            if (npc != null)
            {
                // FAIL the order for this NPC if still pending/in progress
                OrderManager.Instance?.TryFailOrderWithJamuGagal(npc.GetNPCName());
                npc.GoHome();
            }
        }

        // Clear queue after a delay
        StartCoroutine(ClearQueueAfterDelay());
    }

    IEnumerator ClearQueueAfterDelay()
    {
        yield return new WaitForSeconds(2f);
        npcQueue.Clear();
        Debug.Log("Queue cleared for night time.");
    }

    // Manual testing methods
    [ContextMenu("Force Spawn NPC")]
    public void ForceSpawnNPC()
    {
        if (CanSpawnNPC())
        {
            SpawnRandomNPC();
        }
        else
        {
            Debug.Log("Cannot spawn NPC right now.");
        }
    }

    [ContextMenu("Send NPCs Home")]
    public void ForceSendNPCsHome()
    {
        SendAllNPCsHome();
    }

    [ContextMenu("Set Day Time")]
    public void SetDayTime()
    {
        currentTime = 8f; // Noon
        isNightTime = false;
        isShopClosed = false;
    }

    [ContextMenu("Set Night Time")]
    public void SetNightTime()
    {
        currentTime = 20f; // 8 PM
        isNightTime = true;
        isShopClosed = true;
        SendAllNPCsHome();
    }

    // Getters for UI
    public float GetCurrentTime() => currentTime;
    public bool IsNight() => isNightTime;
    public int GetQueueCount() => npcQueue.Count;

    // Dipanggil dari relay helper saat player masuk/keluar area trigger 2D
    public void SetPlayerInArea(bool inArea)
    {
        isPlayerInSpawnArea = inArea;
        spawningActive = inArea;
        if (inArea && spawnCoroutine == null)
        {
            StartSpawning();
        }
        else if (!inArea && spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
            spawnCoroutine = null;
        }
    }

    // ===== METHODS FROM NPCSpawner1 added for feature parity =====

    // Returns the current list of NPCs in queue (for UI, debug, etc)
    public List<JamuNPC> GetCurrentNPCQueue()
    {
        return new List<JamuNPC>(npcQueue);
    }

    // Returns the maximum concurrent NPCs allowed in the queue
    public int GetMaxConcurrentNPCs()
    {
        return maxConcurrentNPCs;
    }

    // Returns the maximum number of queue slots
    public int GetMaxQueueSlots()
    {
        return queuePositions.Length;
    }
}

// Helper supaya trigger 2D bisa relay ke NPCSpawner, drag gameobject area ke field areaTrigger2D di inspector
public class NPCSpawnArea2DRelay : MonoBehaviour
{
    [HideInInspector] public NPCSpawner spawner;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (spawner == null) return;
        if (spawner.spawnOnlyWhenPlayerInArea && other.CompareTag(spawner.playerTag))
        {
            spawner.SetPlayerInArea(true);
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (spawner == null) return;
        if (spawner.spawnOnlyWhenPlayerInArea && other.CompareTag(spawner.playerTag))
        {
            spawner.SetPlayerInArea(false);
        }
    }
}