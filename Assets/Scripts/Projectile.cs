using UnityEngine;

public class Projectile : MonoBehaviour
{
    [Header("Projectile Settings")]
    [Tooltip("How fast the projectile travels forward along the global Z-axis.")]
    [SerializeField] private float speed = 50f;
    [Tooltip("How long (in seconds) before the projectile is automatically destroyed to prevent memory leaks.")]
    [SerializeField] private float lifetime = 3f;

    private void Start()
    {
        // Automatically destroy the game object after 'lifetime' seconds
        Destroy(gameObject, lifetime);
    }

    private void Update()
    {
        // Move forward along the global Z axis.
        // In an on-rails shooter, the camera looks down the positive Z axis.
        transform.position += Vector3.forward * (speed * Time.deltaTime);
    }
}
