using System.Collections;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public bool canMove = true;
    public static PlayerMovement Instance;
    public float moveSpeed = 5f;
    public Rigidbody2D rb;
    Vector2 movement;
    public Animator animator;
    public Camera myCamera;

    // Camera following parameters
    public float smoothSpeed = 0.125f;
    public Vector3 offset = new Vector3(0, 0, -10);

    // Camera position limits
    public Vector2 minCameraPos;
    public Vector2 maxCameraPos;
    private Vector3 startPosition;
    public FixedJoystick joystick; // drag dari inspector

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }
    void Start()
    {
        startPosition = transform.position;
    }

    public void ReturnToStart(float originalCameraSize, Vector2 originalMinCameraPos, Vector2 originalMaxCameraPos)
    {
        TeleportPlayer(startPosition, originalCameraSize, originalMinCameraPos, originalMaxCameraPos);
    }

    void Update()
    {
        if (!canMove) return;
        if (joystick != null)
        {
            movement.x = joystick.Horizontal;
            movement.y = joystick.Vertical;
        }
        else
        {
            movement.x = Input.GetAxisRaw("Horizontal");
            movement.y = Input.GetAxisRaw("Vertical");
        }

        animator.SetFloat("Horizontal", movement.x);
        animator.SetFloat("Vertical", movement.y);
        animator.SetFloat("Speed", movement.sqrMagnitude);
    }

    void FixedUpdate()
    {
        rb.MovePosition(rb.position + movement * moveSpeed * Time.fixedDeltaTime);

        if (myCamera != null)
        {
            Vector3 desiredPosition = transform.position + offset;
            Vector3 smoothedPosition = Vector3.Lerp(myCamera.transform.position, desiredPosition, smoothSpeed);

            smoothedPosition.x = Mathf.Clamp(smoothedPosition.x, minCameraPos.x, maxCameraPos.x);
            smoothedPosition.y = Mathf.Clamp(smoothedPosition.y, minCameraPos.y, maxCameraPos.y);

            myCamera.transform.position = smoothedPosition;
        }
    }

    /// <summary>
    /// Teleport player to target position, update camera size and camera bounds.
    /// </summary>
    /// <param name="targetPosition">Tujuan teleport.</param>
    /// <param name="newCameraSize">Ukuran kamera baru.</param>
    /// <param name="newMinCameraPos">Batas minimum kamera (x,y).</param>
    /// <param name="newMaxCameraPos">Batas maksimum kamera (x,y).</param>
    public void TeleportPlayer(Vector3 targetPosition, float newCameraSize, Vector2 newMinCameraPos, Vector2 newMaxCameraPos)
    {
        // Pindahkan pemain
        transform.position = targetPosition;

        // Update camera settings
        if (myCamera != null)
        {
            myCamera.orthographicSize = newCameraSize;

            minCameraPos = newMinCameraPos;
            maxCameraPos = newMaxCameraPos;

            // Langsung pindahkan kamera ke posisi yang benar
            Vector3 instantCamPos = targetPosition + offset;
            instantCamPos.x = Mathf.Clamp(instantCamPos.x, minCameraPos.x, maxCameraPos.x);
            instantCamPos.y = Mathf.Clamp(instantCamPos.y, minCameraPos.y, maxCameraPos.y);

            myCamera.transform.position = instantCamPos;
        }
    }

    public void TeleportWithFade(Vector3 targetPosition, float newCameraSize, Vector2 newMinCameraPos, Vector2 newMaxCameraPos)
    {
        // Tidak perlu fade, langsung teleport
        TeleportPlayer(targetPosition, newCameraSize, newMinCameraPos, newMaxCameraPos);
    }
}
