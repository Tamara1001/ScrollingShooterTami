using UnityEngine;

public class Asteroid : MonoBehaviour
{
    [Header("Scale Randomization")]
    [Tooltip("Minimum uniform scale applied to the asteroid on spawn.")]
    [SerializeField] private float minScale = 0.5f;
    [Tooltip("Maximum uniform scale applied to the asteroid on spawn.")]
    [SerializeField] private float maxScale = 3f;

    [Header("Speed Randomization")]
    [Tooltip("Minimum forward speed (towards the player, negative Z).")]
    [SerializeField] private float minSpeed = 8f;
    [Tooltip("Maximum forward speed (towards the player, negative Z).")]
    [SerializeField] private float maxSpeed = 22f;

    [Header("Diagonal Drift")]
    [Tooltip("Maximum random drift applied to the X axis per second.")]
    [SerializeField] private float maxDriftX = 1.5f;
    [Tooltip("Maximum random drift applied to the Y axis per second.")]
    [SerializeField] private float maxDriftY = 0.8f;

    [Header("Rotation")]
    [Tooltip("Maximum random spin speed (degrees/sec) on each axis.")]
    [SerializeField] private float maxSpinSpeed = 90f;

    [Header("Cleanup")]
    [Tooltip("Z position threshold behind the player. The asteroid is destroyed once it passes this point.")]
    [SerializeField] private float destroyBehindZ = -15f;

    [Header("Health")]
    [Tooltip("Hit points this asteroid has before it is destroyed by player fire.")]
    [SerializeField] private int maxHealth = 2;

    [Header("Crash Damage")]
    [Tooltip("Damage dealt to the player if they crash into this asteroid.")]
    [SerializeField] private int crashDamage = 1;

    [Header("VFX & Feedback")]
    [Tooltip("Explosion particle prefab spawned when the asteroid is destroyed.")]
    [SerializeField] private GameObject explosionPrefab;
    [Tooltip("Duration of camera shake on asteroid death.")]
    [SerializeField] private float shakeDuration  = 0.15f;
    [Tooltip("Magnitude of camera shake on asteroid death (lighter than enemy shake).")]
    [SerializeField] private float shakeMagnitude = 0.15f;

    private float forwardSpeed;
    private Vector3 driftVelocity;
    private Vector3 spinVelocity;
    private int currentHealth;

    // Cached reference — avoids GetComponent every TakeDamage call
    private DamageFlash damageFlash;

    // -------------------------------------------------------------------------
    // Unity Lifecycle
    // -------------------------------------------------------------------------

    private void Awake()
    {
        damageFlash = GetComponent<DamageFlash>();
    }

    private void Start()
    {
        currentHealth = maxHealth;

        // --- Random Scale ---
        float randomScale = Random.Range(minScale, maxScale);
        transform.localScale = Vector3.one * randomScale;

        // --- Random Speed ---
        forwardSpeed = Random.Range(minSpeed, maxSpeed);

        // --- Random Diagonal Drift ---
        driftVelocity = new Vector3(
            Random.Range(-maxDriftX, maxDriftX),
            Random.Range(-maxDriftY, maxDriftY),
            0f
        );

        // --- Random Spin ---
        spinVelocity = new Vector3(
            Random.Range(-maxSpinSpeed, maxSpinSpeed),
            Random.Range(-maxSpinSpeed, maxSpinSpeed),
            Random.Range(-maxSpinSpeed, maxSpinSpeed)
        );
    }

    private void Update()
    {
        // Move towards the player (negative Z) plus the random lateral drift
        Vector3 movement = Vector3.back * (forwardSpeed * Time.deltaTime);
        movement += driftVelocity * Time.deltaTime;
        transform.position += movement;

        // Apply the random spin for a more natural tumbling look
        transform.Rotate(spinVelocity * Time.deltaTime, Space.World);

        // Self-destruct once the asteroid has passed behind the camera
        if (transform.position.z < destroyBehindZ)
        {
            Destroy(gameObject);
        }
    }

    // -------------------------------------------------------------------------
    // Collision
    // -------------------------------------------------------------------------

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerStats playerStats = other.GetComponent<PlayerStats>();
            if (playerStats != null)
            {
                playerStats.TakeDamage(crashDamage);
                Die(); // Destroy self after crashing into the player
            }
        }
    }

    // -------------------------------------------------------------------------
    // Combat
    // -------------------------------------------------------------------------

    /// <summary>Applies damage to this asteroid. Destroys it when health reaches zero.</summary>
    public void TakeDamage(int amount)
    {
        currentHealth -= amount;

        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            // Still alive — flash to signal the hit
            damageFlash?.Flash();
        }
    }

    private void Die()
    {
        // Award score via GameManager
        GameManager.Instance?.RegisterAsteroidKill();

        // Spawn debris VFX
        if (explosionPrefab != null)
            Instantiate(explosionPrefab, transform.position, Quaternion.identity);

        // Light camera shake — less intense than enemy death
        CameraShake.Instance?.Shake(shakeDuration, shakeMagnitude);

        Destroy(gameObject);
    }
}
