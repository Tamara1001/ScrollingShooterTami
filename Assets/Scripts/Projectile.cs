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
    [Tooltip("True if this bullet was fired by the player. Used by collision scripts to determine what it can damage.")]
    public bool isPlayerOwned = true;

    private void Start()
    {
        // Automatically destroy this game object after 'lifetime' seconds
        Destroy(gameObject, lifetime);
    }

    private void Update()
    {
        // Travel along the assigned direction. The direction is normalized so
        // that diagonal directions don't produce faster movement.
        transform.position += moveDirection.normalized * (speed * Time.deltaTime);
    }
}
