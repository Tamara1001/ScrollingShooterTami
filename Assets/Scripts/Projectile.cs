using UnityEngine;

public class Projectile : MonoBehaviour
{
    [Header("Projectile Settings")]
    [Tooltip("How fast the projectile travels along its move direction.")]
    [SerializeField] private float speed = 50f;
    [Tooltip("How long (in seconds) before the projectile is automatically destroyed to prevent memory leaks.")]
    [SerializeField] private float lifetime = 3f;

    [Header("Ownership & Direction")]
    [Tooltip("The direction this projectile will travel in world space. Default is Vector3.forward (positive Z) for player bullets.")]
    public Vector3 moveDirection = Vector3.forward;
    [Tooltip("True if this bullet was fired by the player. Used by collision logic to determine what it can damage.")]
    public bool isPlayerOwned = true;

    [Header("Combat")]
    [Tooltip("How much damage this projectile deals on impact.")]
    public int damage = 1;

    private void Start()
    {
        // Automatically destroy this game object after 'lifetime' seconds
        Destroy(gameObject, lifetime);
    }

    private void Update()
    {
        // Travel along the assigned direction. Normalized so diagonal directions
        // don't produce faster movement.
        transform.position += moveDirection.normalized * (speed * Time.deltaTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isPlayerOwned)
        {
            // Player bullets damage Enemies and Asteroids
            if (other.CompareTag("Enemy"))
            {
                Enemy enemy = other.GetComponent<Enemy>();
                if (enemy != null)
                    enemy.TakeDamage(damage);

                Destroy(gameObject);
            }
            else if (other.CompareTag("Asteroid"))
            {
                Asteroid asteroid = other.GetComponent<Asteroid>();
                if (asteroid != null)
                    asteroid.TakeDamage(damage);

                Destroy(gameObject);
            }
        }
        else
        {
            // Enemy bullets damage the Player
            if (other.CompareTag("Player"))
            {
                PlayerStats stats = other.GetComponent<PlayerStats>();
                if (stats != null)
                    stats.TakeDamage(damage);

                Destroy(gameObject);
            }
        }
    }
}
