using UnityEngine;

public class GerobakUnlockController : MonoBehaviour
{
    public SpriteRenderer gerobakSpriteRenderer;
    public GameObject lockIcon;
    public GameObject npcTrigger;

    public Color lockedColor = new Color(0.2f, 0.2f, 0.2f, 1f);
    private Color originalColor;

    void Awake()
    {
        if (gerobakSpriteRenderer != null)
            originalColor = gerobakSpriteRenderer.color;
    }

    public void SetGerobakState(bool isLocked)
    {
        if (gerobakSpriteRenderer != null)
            gerobakSpriteRenderer.color = isLocked ? lockedColor : originalColor;

        if (lockIcon != null)
            lockIcon.SetActive(isLocked);

        if (npcTrigger != null)
            npcTrigger.SetActive(!isLocked);
    }
}