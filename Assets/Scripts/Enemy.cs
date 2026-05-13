using System.Collections;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // Enums
    // -------------------------------------------------------------------------

    public enum EnemyColor { Cyan, Magenta }
    public enum EnemyType  { Fast, Normal, Tough }

    // -------------------------------------------------------------------------
    // Inspector Fields
    // -------------------------------------------------------------------------

    [Header("Identity")]
    [SerializeField] private EnemyColor enemyColor = EnemyColor.Cyan;
    [SerializeField] private EnemyType  enemyType  = EnemyType.Normal;

    [Header("Stats")]
    [Tooltip("Hit points before the enemy is destroyed.")]
    [SerializeField] private int health = 3;

    [Header("Phase 1 – Hover & Attack")]
    [Tooltip("How long the enemy stays in hover/attack phase before fleeing (seconds).")]
    [SerializeField] private float hoverDuration = 10f;
    [Tooltip("The Z distance from origin at which the enemy hovers while attacking.")]
    [SerializeField] private float hoverZPosition = 20f;
    [Tooltip("How fast the enemy smoothly aligns its X/Y to the player (units/sec).")]
    [SerializeField] private float alignmentSpeed = 4f;
    [Tooltip("How close the enemy's XY position must be to the player's to be considered 'aligned' and start shooting.")]
    [SerializeField] private float alignmentThreshold = 1.5f;

    [Header("Phase 2 – Flee")]
    [Tooltip("Speed at which the enemy flies forward (negative Z) during the flee phase.")]
    [SerializeField] private float fleeSpeed = 18f;
    [Tooltip("Z position behind which the enemy is destroyed during the flee phase.")]
    [SerializeField] private float destroyBehindZ = -15f;

    [Header("Shooting")]
    [Tooltip("The projectile prefab fired by this enemy.")]
    [SerializeField] private GameObject projectilePrefab;
    [Tooltip("The transform from which enemy projectiles are spawned.")]
    [SerializeField] private Transform firePoint;
    [Tooltip("Material applied to each enemy bullet on spawn. Use NeonMagenta or NeonCyan.")]
    [SerializeField] private Material bulletMaterial;
    [Tooltip("Number of shots per burst.")]
    [SerializeField] private int burstSize = 5;
    [Tooltip("Time between individual shots within a burst (seconds).")]
    [SerializeField] private float timeBetweenShots = 0.12f;
    [Tooltip("Cooldown (seconds) before the next burst can start after one finishes.")]
    [SerializeField] private float reloadCooldown = 3f;

    [Header("VFX & Feedback")]
    [Tooltip("Explosion particle prefab spawned on death. Use Explosion_Cyan or Explosion_Magenta.")]
    [SerializeField] private GameObject explosionPrefab;
    [Tooltip("Duration of the camera shake triggered on enemy death.")]
    [SerializeField] private float shakeDuration  = 0.25f;
    [Tooltip("Magnitude (world-unit displacement) of the camera shake on enemy death.")]
    [SerializeField] private float shakeMagnitude = 0.3f;

    [Header("Highlighting & Collision")]
    [Tooltip("Material used when the player's laser sight targets this enemy.")]
    [SerializeField] private Material targetedMaterial;
    [Tooltip("Damage dealt to the player if they crash into this enemy.")]
    [SerializeField] private int crashDamage = 1;

    [Header("References")]
    [Tooltip("The player's Transform – used to calculate alignment.")]
    [SerializeField] private Transform playerTransform;

    // -------------------------------------------------------------------------
    // Private State
    // -------------------------------------------------------------------------

    private bool      isInFleePhase     = false;
    private bool      isShooting        = false;
    private int       currentBurstCount  = 0;
    private Coroutine shootingCoroutine;

    // Cached reference — avoids GetComponent every TakeDamage call
    private DamageFlash damageFlash;
    private Material    originalMaterial;
    private MeshRenderer meshRenderer;

    // -------------------------------------------------------------------------
    // Unity Lifecycle
    // -------------------------------------------------------------------------

    private void Awake()
    {
        // Cache the DamageFlash component (may be null if not added to this prefab)
        damageFlash = GetComponent<DamageFlash>();

        meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer != null)
        {
            originalMaterial = meshRenderer.sharedMaterial;
        }
    }

    private void Start()
    {
        if (playerTransform == null)
        {
            // Fallback: try to find the player by tag (assign 'Player' tag on the ship)
            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null)
                playerTransform = playerObj.transform;
            else
                Debug.LogWarning($"[Enemy] No player Transform assigned and no 'Player' tag found on {gameObject.name}.");
        }

        // Apply stat modifiers based on EnemyType
        ApplyTypeModifiers();

        // Lock the enemy at its designated hover Z depth
        Vector3 startPos = transform.position;
        startPos.z = hoverZPosition;
        transform.position = startPos;

        // Begin the phase timer
        StartCoroutine(HoverPhaseTimer());
    }

    private void Update()
    {
        if (isInFleePhase)
        {
            RunFleePhase();
        }
        else
        {
            RunHoverPhase();
        }
    }

    // -------------------------------------------------------------------------
    // Collision & Highlighting
    // -------------------------------------------------------------------------

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerStats playerStats = other.GetComponent<PlayerStats>();
            if (playerStats != null)
            {
                playerStats.TakeDamage(crashDamage);
                Die(); // Destroy self after crashing into player
            }
        }
    }

    public void SetTargeted(bool isTargeted)
    {
        if (meshRenderer != null && targetedMaterial != null && originalMaterial != null)
        {
            meshRenderer.sharedMaterial = isTargeted ? targetedMaterial : originalMaterial;
        }
    }

    // -------------------------------------------------------------------------
    // Phase Logic
    // -------------------------------------------------------------------------

    /// <summary>Phase 1: Smoothly align to the player's XY and shoot when aligned.</summary>
    private void RunHoverPhase()
    {
        if (playerTransform == null) return;

        // Build the target position: match the player's XY, but keep our hover Z
        Vector3 target = new Vector3(playerTransform.position.x, playerTransform.position.y, transform.position.z);

        // Smoothly interpolate towards that target
        transform.position = Vector3.MoveTowards(transform.position, target, alignmentSpeed * Time.deltaTime);

        // Check if we are close enough to shoot
        float xyDistance = Vector2.Distance(
            new Vector2(transform.position.x, transform.position.y),
            new Vector2(playerTransform.position.x, playerTransform.position.y)
        );

        bool isAligned = xyDistance <= alignmentThreshold;
        if (isAligned && !isShooting)
        {
            shootingCoroutine = StartCoroutine(ShootingRoutine());
        }
    }

    /// <summary>Phase 2: Fly straight towards and past the player, then self-destruct.</summary>
    private void RunFleePhase()
    {
        transform.position += Vector3.back * (fleeSpeed * Time.deltaTime);

        if (transform.position.z < destroyBehindZ)
        {
            Destroy(gameObject);
        }
    }

    // -------------------------------------------------------------------------
    // Coroutines
    // -------------------------------------------------------------------------

    /// <summary>Waits for hoverDuration then triggers the flee phase.</summary>
    private IEnumerator HoverPhaseTimer()
    {
        yield return new WaitForSeconds(hoverDuration);
        isInFleePhase = true;
        // Stop any in-progress burst immediately using the stored reference
        if (shootingCoroutine != null)
        {
            StopCoroutine(shootingCoroutine);
            shootingCoroutine = null;
        }
        isShooting = false;
    }

    /// <summary>Fires a full burst of shots, waits for the reload cooldown, then loops.</summary>
    private IEnumerator ShootingRoutine()
    {
        isShooting       = true;
        shootingCoroutine = null; // Will be set by the caller

        while (!isInFleePhase)
        {
            // Fire the burst
            for (int i = 0; i < burstSize; i++)
            {
                if (isInFleePhase) break; // Abort mid-burst if we just transitioned

                FireProjectile();
                yield return new WaitForSeconds(timeBetweenShots);
            }

            // Wait for the reload cooldown before starting the next burst
            yield return new WaitForSeconds(reloadCooldown);
        }

        isShooting = false;
    }

    // -------------------------------------------------------------------------
    // Actions
    // -------------------------------------------------------------------------

    private void FireProjectile()
    {
        if (projectilePrefab == null || firePoint == null)
        {
            Debug.LogWarning($"[Enemy] projectilePrefab or firePoint is not set on {gameObject.name}.");
            return;
        }

        // Instantiate the projectile at the fire point with no rotation
        GameObject proj = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);

        // Use the updated Projectile API to set direction, ownership, and colour
        Projectile projScript = proj.GetComponent<Projectile>();
        if (projScript != null)
        {
            projScript.moveDirection = Vector3.back;
            projScript.isPlayerOwned = false;
            projScript.SetMaterial(bulletMaterial);
        }
    }

    /// <summary>Call this from collision/trigger logic to deal damage to this enemy.</summary>
    public void TakeDamage(int damage)
    {
        health -= damage;

        if (health <= 0)
        {
            Die();
        }
        else
        {
            // Still alive — flash to signal the hit without dying
            damageFlash?.Flash();
        }
    }

    private void Die()
    {
        // Award score and increment kill counter via GameManager
        GameManager.Instance?.RegisterEnemyKill();

        // Spawn explosion VFX at this enemy's position.
        // The prefab's StopAction = Destroy, so it cleans itself up automatically.
        if (explosionPrefab != null)
            Instantiate(explosionPrefab, transform.position, Quaternion.identity);

        // Trigger camera shake for impact feel
        CameraShake.Instance?.Shake(shakeDuration, shakeMagnitude);

        Destroy(gameObject);
    }

    // -------------------------------------------------------------------------
    // Type Modifiers
    // -------------------------------------------------------------------------

    /// <summary>Adjusts stats based on EnemyType enum to avoid duplicating inspector values.</summary>
    private void ApplyTypeModifiers()
    {
        switch (enemyType)
        {
            case EnemyType.Fast:
                alignmentSpeed *= 2f;
                fleeSpeed      *= 1.5f;
                health          = Mathf.Max(1, health - 1);
                break;

            case EnemyType.Tough:
                health          = health * 3;
                alignmentSpeed *= 0.6f;
                break;

            case EnemyType.Normal:
            default:
                // Baseline stats — no modification
                break;
        }
    }
}
