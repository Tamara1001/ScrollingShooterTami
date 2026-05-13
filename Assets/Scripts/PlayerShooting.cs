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

    [Header("References")]
    [Tooltip("Reference to the PlayerStats component on this ship. Used to check overheat and consume energy.")]
    [SerializeField] private PlayerStats playerStats;

    private float nextFireTime;

    private void Awake()
    {
        // Auto-find PlayerStats on the same GameObject if not set in the Inspector
        if (playerStats == null)
        {
            playerStats = GetComponent<PlayerStats>();
            if (playerStats == null)
                Debug.LogWarning("[PlayerShooting] PlayerStats component not found! Energy/Overheat checks will be skipped.");
        }
    }

    private void OnEnable()
    {
        if (primaryFireAction != null)
        {
            primaryFireAction.action.Enable();
        }
        else
        {
            Debug.LogWarning("[PlayerShooting] Primary Fire Action is not assigned!");
        }

        if (secondaryFireAction != null)
        {
            secondaryFireAction.action.Enable();
            secondaryFireAction.action.performed += OnSecondaryFirePerformed;
        }
        else
        {
            Debug.LogWarning("[PlayerShooting] Secondary Fire Action is not assigned!");
        }
    }

    private void OnDisable()
    {
        if (primaryFireAction != null)
            primaryFireAction.action.Disable();

        if (secondaryFireAction != null)
        {
            secondaryFireAction.action.performed -= OnSecondaryFirePerformed;
            secondaryFireAction.action.Disable();
        }
    }

    private void Update()
    {
        // Block all firing if the game is not in the Playing state
        if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameManager.GameState.Playing)
            return;

        if (primaryFireAction != null && primaryFireAction.action.IsPressed())
        {
            if (Time.time >= nextFireTime)
            {
                TryFirePrimary();
            }
        }
    }

    // -------------------------------------------------------------------------
    // Fire Logic
    // -------------------------------------------------------------------------

    private void TryFirePrimary()
    {
        // Gate 1: Overheat check — if the player is overheated, block entirely
        if (playerStats != null && playerStats.IsOverheated)
            return;

        // Gate 2: Energy check — ConsumeEnergy returns false if there's not enough
        if (playerStats != null && !playerStats.ConsumeEnergy())
            return;

        FirePrimary();
    }

    private void FirePrimary()
    {
        nextFireTime = Time.time + fireRate;

        if (projectilePrefab != null && firePoint != null)
        {
            Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);
        }
        else
        {
            Debug.LogWarning("[PlayerShooting] Projectile Prefab or Fire Point is missing!");
        }
    }

    private void OnSecondaryFirePerformed(InputAction.CallbackContext context)
    {
        // Gate: Block secondary fire during overheat as well
        if (playerStats != null && playerStats.IsOverheated)
        {
            Debug.Log("[PlayerShooting] Secondary fire blocked — ship is overheated.");
            return;
        }

        // Optionally consume a chunk of energy for secondary fire too.
        // For now we just check the state and log — implementation TBD.
        Debug.Log("Secondary Fire (Lasso/Swarm) Activated");
    }
}
