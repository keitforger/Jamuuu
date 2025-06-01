using UnityEngine;

public static class PanelScalingUtils
{
    // Calculate scale factor between two transforms for 2D
    public static float CalculateScaleFactor(Transform source, Transform destination)
    {
        // Use lossyScale to consider the real-world scale of the objects
        Vector3 sourceScale = source.lossyScale;
        Vector3 destinationScale = destination.lossyScale;

        // Calculate scale factor for both X and Y dimensions
        float scaleFactorX = destinationScale.x / sourceScale.x;
        float scaleFactorY = destinationScale.y / sourceScale.y;

        // Use the smaller scale to preserve aspect ratio
        return Mathf.Min(scaleFactorX, scaleFactorY);
    }

    // Adjust a game object's scale when moving between panels - optimized for 2D
    public static void AdjustScaleForPanel(GameObject item, Transform sourcePanel, Transform destinationPanel)
    {
        if (item == null || sourcePanel == null || destinationPanel == null)
        {
            Debug.LogWarning("AdjustScaleForPanel: Missing references!");
            return;
        }

        // Selalu hitung scaleFactor (walaupun root canvas sama)
        float scaleFactor = CalculateScaleFactor(sourcePanel, destinationPanel);
        item.transform.localScale = new Vector3(
            item.transform.localScale.x * scaleFactor,
            item.transform.localScale.y * scaleFactor,
            item.transform.localScale.z
        );

        Debug.Log($"[AdjustScaleForPanel] {item.name} scaleFactor = {scaleFactor}, from {sourcePanel.name} to {destinationPanel.name}");
    }


    // Find the root canvas for a transform
    public static Canvas FindRootCanvas(Transform transform)
    {
        if (transform == null)
        {
            Debug.LogWarning("FindRootCanvas: Transform is null!");
            return null;
        }

        Canvas canvas = transform.GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            Debug.LogWarning("FindRootCanvas: No Canvas found!");
            return null;
        }

        while (canvas.transform.parent != null)
        {
            Canvas parentCanvas = canvas.transform.parent.GetComponentInParent<Canvas>();
            if (parentCanvas == null)
                break;
            canvas = parentCanvas;
        }

        return canvas;
    }
}