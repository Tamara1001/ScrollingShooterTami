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

    private float forwardSpeed;
    private Vector3 driftVelocity;
    private Vector3 spinVelocity;
    private int currentHealth;

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
    }

    private void Die()
    {
        // Award score via GameManager
        GameManager.Instance?.RegisterAsteroidKill();

        // TODO: Spawn debris VFX, play impact sound
        Destroy(gameObject);
    }
}
