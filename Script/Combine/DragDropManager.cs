using UnityEngine;
using System.Collections.Generic;

public class DragDropManager : MonoBehaviour
{
    private static DragDropManager instance;
    public static DragDropManager Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject go = new GameObject("DragDropManager");
                instance = go.AddComponent<DragDropManager>();
                DontDestroyOnLoad(go);
            }
            return instance;
        }
    }
    [SerializeField] private bool isDragDropLocked = false;
    private List<SlotBahan> allSlotBahan = new List<SlotBahan>();
    private List<SlotCombine> allSlotCombine = new List<SlotCombine>();

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    public void RegisterSlotBahan(SlotBahan slot)
    {
        if (!allSlotBahan.Contains(slot))
        {
            allSlotBahan.Add(slot);
            SlotBahan.SetGlobalDragLock(isDragDropLocked);
        }
    }

    public void UnregisterSlotBahan(SlotBahan slot)
    {
        allSlotBahan.Remove(slot);
    }

    public void RegisterSlotCombine(SlotCombine slot)
    {
        if (!allSlotCombine.Contains(slot))
        {
            allSlotCombine.Add(slot);
            SlotCombine.SetGlobalDropLock(isDragDropLocked);
        }
    }

    public void UnregisterSlotCombine(SlotCombine slot)
    {
        allSlotCombine.Remove(slot);
    }

    public void LockAllDragDrop()
    {
        isDragDropLocked = true;
        Debug.Log("[DragDropManager] Locking all drag and drop operations");

        // Lock all SlotBahan (static call)
        SlotBahan.SetGlobalDragLock(true);

        // Lock all SlotCombine (static call)
        SlotCombine.SetGlobalDropLock(true);

        // Cancel any ongoing drag operations
        CancelAllActiveDrags();
    }

    public void UnlockAllDragDrop()
    {
        isDragDropLocked = false;
        Debug.Log("[DragDropManager] Unlocking all drag and drop operations");

        // Unlock all SlotBahan (static call)
        SlotBahan.SetGlobalDragLock(false);

        // Unlock all SlotCombine (static call)
        SlotCombine.SetGlobalDropLock(false);
    }

    private void CancelAllActiveDrags()
    {
        // Force cancel any active drag operations
        foreach (SlotBahan slot in allSlotBahan)
        {
            if (slot != null)
            {
                slot.CancelActiveDrag();
            }
        }
    }

    public bool IsDragDropLocked()
    {
        return isDragDropLocked;
    }

    // Clean up null references
    void Update()
    {
        allSlotBahan.RemoveAll(slot => slot == null);
        allSlotCombine.RemoveAll(slot => slot == null);
    }
}