using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("How fast the ship moves in the X and Y axes.")]
    [SerializeField] private float moveSpeed = 20f;
    [Tooltip("How smoothly the ship reaches its target position. Lower values are more responsive.")]
    [SerializeField] private float moveSmoothTime = 0.05f;

    [Header("Boundaries")]
    [Tooltip("Minimum and Maximum X positions (Left/Right)")]
    [SerializeField] private Vector2 xBounds = new Vector2(-15f, 15f);
    [Tooltip("Minimum and Maximum Y positions (Down/Up)")]
    [SerializeField] private Vector2 yBounds = new Vector2(-8f, 8f);

    [Header("Visual Tilt (Roll)")]
    [Tooltip("The maximum angle the ship will tilt on the Z-axis when moving left or right.")]
    [SerializeField] private float maxTiltAngle = 30f;
    [Tooltip("How smoothly the ship rolls into the tilt and back to zero.")]
    [SerializeField] private float tiltSmoothSpeed = 8f;

    [Header("Input Setup")]
    [Tooltip("Reference to the Input Action configured for moving the ship (Vector2).")]
    [SerializeField] private InputActionReference moveAction;

    private Vector2 currentInput;
    private Vector3 currentVelocity;
    private Vector3 targetPosition;

    private void Awake()
    {
        // Initialize target position to our starting position so we don't snap to origin
        targetPosition = transform.position;
    }

    private void OnEnable()
    {
        if (moveAction != null)
        {
            moveAction.action.Enable();
        }
        else
        {
            Debug.LogWarning("Move Action is not assigned in the PlayerMovement script!");
        }
    }

    private void OnDisable()
    {
        if (moveAction != null)
        {
            moveAction.action.Disable();
        }
    }

    private void Update()
    {
        // Block all input processing if the game is not in the Playing state
        if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameManager.GameState.Playing)
            return;

        ReadInput();
        MoveShip();
        ApplyTilt();
    }

    private void ReadInput()
    {
        if (moveAction != null)
        {
            currentInput = moveAction.action.ReadValue<Vector2>();
        }
    }

    private void MoveShip()
    {
        // Calculate the raw target position based on input
        Vector3 inputTranslation = new Vector3(currentInput.x, currentInput.y, 0f) * (moveSpeed * Time.deltaTime);
        targetPosition += inputTranslation;

        // Clamp the target position within our defined screen boundaries
        targetPosition.x = Mathf.Clamp(targetPosition.x, xBounds.x, xBounds.y);
        targetPosition.y = Mathf.Clamp(targetPosition.y, yBounds.x, yBounds.y);
        
        // Z-axis is locked for the player, keep it at its initial value or 0
        targetPosition.z = transform.position.z;

        // Smoothly move the ship towards the target position for that "tight but smooth" feel
        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref currentVelocity, moveSmoothTime);
    }

    private void ApplyTilt()
    {
        // In Unity, a negative rotation on the Z axis rolls the object to the right.
        // Therefore, if input is positive (moving right), we want a negative Z angle.
        float targetZRotation = currentInput.x * -maxTiltAngle;

        // Create the target quaternion (we keep X and Y rotation at 0)
        Quaternion targetRotation = Quaternion.Euler(0f, 0f, targetZRotation);

        // Smoothly interpolate the current rotation towards the target tilt rotation
        transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRotation, Time.deltaTime * tiltSmoothSpeed);
    }
}
