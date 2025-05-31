using UnityEngine;

public class TeleportTrigger : MonoBehaviour
{
    [Header("Target Teleport Settings")]
    public Transform targetPoint;
    public float targetCameraSize = 5f;
    public Vector2 newMinCameraPos;
    public Vector2 newMaxCameraPos;

    [Header("Return Settings")]
    public bool isReturnTrigger = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerMovement player = other.GetComponent<PlayerMovement>();
            if (player != null)
            {
                if (isReturnTrigger)
                {
                    // Kembali ke posisi awal player saat scene dimulai
                    player.ReturnToStart(targetCameraSize, newMinCameraPos, newMaxCameraPos);
                }
                else
                {
                    player.TeleportPlayer(targetPoint.position, targetCameraSize, newMinCameraPos, newMaxCameraPos);
                }
            }
        }
    }
}
