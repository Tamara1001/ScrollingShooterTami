using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerShooting : MonoBehaviour
{
    [Header("Shooting Setup")]
    [Tooltip("The prefab to instantiate when firing.")]
    [SerializeField] private GameObject projectilePrefab;
    [Tooltip("The transform from which projectiles will be spawned (usually the tip of the ship).")]
    [SerializeField] private Transform firePoint;

    [Header("Primary Fire Settings")]
    [Tooltip("Time in seconds between consecutive shots.")]
    [SerializeField] private float fireRate = 0.15f;

    [Header("Input Setup")]
    [Tooltip("Reference to the Input Action configured for continuous primary fire.")]
    [SerializeField] private InputActionReference primaryFireAction;
    [Tooltip("Reference to the Input Action configured for a single secondary fire press.")]
    [SerializeField] private InputActionReference secondaryFireAction;

    private float nextFireTime;

    private void OnEnable()
    {
        if (primaryFireAction != null)
        {
            primaryFireAction.action.Enable();
        }
        else
        {
            Debug.LogWarning("Primary Fire Action is not assigned in PlayerShooting!");
        }

        if (secondaryFireAction != null)
        {
            secondaryFireAction.action.Enable();
            // Subscribe to the performed event to detect exactly when the button is pressed
            secondaryFireAction.action.performed += OnSecondaryFirePerformed;
        }
        else
        {
            Debug.LogWarning("Secondary Fire Action is not assigned in PlayerShooting!");
        }
    }

    private void OnDisable()
    {
        if (primaryFireAction != null)
        {
            primaryFireAction.action.Disable();
        }

        if (secondaryFireAction != null)
        {
            secondaryFireAction.action.performed -= OnSecondaryFirePerformed;
            secondaryFireAction.action.Disable();
        }
    }

    private void Update()
    {
        // We use IsPressed() to cleanly check if the button is currently being held down
        if (primaryFireAction != null && primaryFireAction.action.IsPressed())
        {
            if (Time.time >= nextFireTime)
            {
                FirePrimary();
            }
        }
    }

    private void FirePrimary()
    {
        // Set the time for the next allowed shot
        nextFireTime = Time.time + fireRate;

        // Instantiate the projectile
        if (projectilePrefab != null && firePoint != null)
        {
            Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);
        }
        else
        {
            Debug.LogWarning("Projectile Prefab or Fire Point is missing in PlayerShooting!");
        }
    }

    private void OnSecondaryFirePerformed(InputAction.CallbackContext context)
    {
        Debug.Log("Secondary Fire (Lasso/Swarm) Activated");
    }
}
