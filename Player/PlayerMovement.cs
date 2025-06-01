using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem; // Untuk Input System baru

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

    // Enable/disable camera bounds clamp
    public bool useCameraBounds = true;

    public FixedJoystick joystick; // drag dari inspector jika ada joystick

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (joystick == null)
        {
            joystick = FindAnyObjectByType<FixedJoystick>();
            if (joystick == null)
                Debug.LogWarning("FixedJoystick not found in scene! Pastikan Joystick ada di scene.");
        }
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
        if (!canMove)
        {
            movement = Vector2.zero;
            if (animator != null)
            {
                animator.SetFloat("Horizontal", 0);
                animator.SetFloat("Vertical", 0);
                animator.SetFloat("Speed", 0);
            }
            return;
        }

        // --- Prioritas: Pakai Joystick kalau ada ---
        if (joystick != null)
        {
            movement.x = joystick.Horizontal;
            movement.y = joystick.Vertical;
        }
        else if (Keyboard.current != null)
        {
            // --- Input System baru ---
            movement = Vector2.zero;
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
                movement.x = -1;
            else if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
                movement.x = 1;

            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed)
                movement.y = 1;
            else if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed)
                movement.y = -1;
        }
        else
        {
            // --- Fallback: Input lama ---
            movement.x = Input.GetAxisRaw("Horizontal");
            movement.y = Input.GetAxisRaw("Vertical");
        }

        if (animator != null)
        {
            animator.SetFloat("Horizontal", movement.x);
            animator.SetFloat("Vertical", movement.y);
            animator.SetFloat("Speed", movement.sqrMagnitude);
        }
    }

    void FixedUpdate()
    {
        rb.MovePosition(rb.position + movement * moveSpeed * Time.fixedDeltaTime);

        if (myCamera != null)
        {
            Vector3 desiredPosition = transform.position + offset;
            Vector3 smoothedPosition = Vector3.Lerp(myCamera.transform.position, desiredPosition, smoothSpeed);

            if (useCameraBounds)
            {
                smoothedPosition.x = Mathf.Clamp(smoothedPosition.x, minCameraPos.x, maxCameraPos.x);
                smoothedPosition.y = Mathf.Clamp(smoothedPosition.y, minCameraPos.y, maxCameraPos.y);
            }
            smoothedPosition.z = -10f;
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
        useCameraBounds = false; // Matikan clamp sementara saat teleport

        // Pindahkan pemain
        transform.position = targetPosition;

        // Update camera settings
        if (myCamera != null)
        {
            myCamera.orthographicSize = newCameraSize;

            // Langsung pindahkan kamera ke posisi baru (tanpa clamp)
            Vector3 instantCamPos = targetPosition + offset;
            instantCamPos.z = -10f;
            myCamera.transform.position = instantCamPos;

            // Update batas kamera untuk map baru
            minCameraPos = newMinCameraPos;
            maxCameraPos = newMaxCameraPos;
        }

        useCameraBounds = true; // Aktifkan lagi clamp setelah teleport
    }

    public void TeleportWithFade(Vector3 targetPosition, float newCameraSize, Vector2 newMinCameraPos, Vector2 newMaxCameraPos)
    {
        // Tidak perlu fade, langsung teleport
        TeleportPlayer(targetPosition, newCameraSize, newMinCameraPos, newMaxCameraPos);
    }
}