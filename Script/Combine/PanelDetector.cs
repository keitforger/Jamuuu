// Modified PanelDetector.cs - Helper class to find CraftingManager
using UnityEngine;

public static class PanelDetector
{
    // Updated method to find CraftingManager as ICraftingPanel interface
    public static ICraftingPanel FindCraftingPanel(GameObject gameObject)
    {
        // Try to get the CraftingManager from the object or its parents
        CraftingManager craftingManager = gameObject.GetComponentInParent<CraftingManager>();
        if (craftingManager != null)
        {
            return craftingManager as ICraftingPanel;
        }

        // If not found in immediate parents, search up the hierarchy
        Transform current = gameObject.transform.parent;
        while (current != null)
        {
            craftingManager = current.GetComponent<CraftingManager>();
            if (craftingManager != null)
            {
                return craftingManager as ICraftingPanel;
            }

            current = current.parent;
        }

        // As a fallback, try to search for any ICraftingPanel implementation
        // This maintains compatibility if there are other ICraftingPanel implementers
        ICraftingPanel panel = gameObject.GetComponentInParent<ICraftingPanel>();
        if (panel != null)
        {
            return panel;
        }

        Debug.LogWarning($"No CraftingManager or ICraftingPanel found for {gameObject.name}");
        return null;
    }
}