using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaveManager : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // Inspector Fields
    // -------------------------------------------------------------------------

    [Header("Prefabs")]
    [Tooltip("List of all enemy prefabs that can be spawned.")]
    [SerializeField] private List<GameObject> enemyPrefabs = new List<GameObject>();
    [Tooltip("List of all asteroid prefabs that can be spawned.")]
    [SerializeField] private List<GameObject> asteroidPrefabs = new List<GameObject>();

    [Header("Spawn Area")]
    [Tooltip("The Z distance at which enemies and asteroids are spawned (far from the player).")]
    [SerializeField] private float spawnZDistance = 80f;
    [Tooltip("The half-width of the spawn corridor on the X axis (spawns between -X and +X).")]
    [SerializeField] private float spawnRangeX = 18f;
    [Tooltip("The half-height of the spawn corridor on the Y axis (spawns between -Y and +Y).")]
    [SerializeField] private float spawnRangeY = 10f;

    [Header("Difficulty – Spawn Rate")]
    [Tooltip("Time between enemy spawns at the very start of the game (seconds).")]
    [SerializeField] private float initialEnemySpawnInterval = 6f;
    [Tooltip("The fastest the enemy spawn interval can ever get, no matter how long the game runs (seconds).")]
    [SerializeField] private float minEnemySpawnInterval = 1.5f;
    [Tooltip("How many seconds of play it takes to reach the minimum spawn interval (full difficulty).")]
    [SerializeField] private float difficultyRampDuration = 180f;

    [Header("Difficulty – Asteroid Rate")]
    [Tooltip("Time between asteroid spawns at the start of the game (seconds).")]
    [SerializeField] private float initialAsteroidSpawnInterval = 2f;
    [Tooltip("The fastest the asteroid spawn interval can ever get (seconds).")]
    [SerializeField] private float minAsteroidSpawnInterval = 0.4f;

    [Header("Crowding Limit")]
    [Tooltip("Maximum number of enemy GameObjects allowed in the scene at once. Spawning pauses if this is exceeded.")]
    [SerializeField] private int maxActiveEnemies = 8;

    // -------------------------------------------------------------------------
    // Private State
    // -------------------------------------------------------------------------

    // We track live enemies with a list so we can count them accurately
    // (using FindObjectsByType every frame would be expensive).
    private readonly List<GameObject> activeEnemies = new List<GameObject>();

    // -------------------------------------------------------------------------
    // Unity Lifecycle
    // -------------------------------------------------------------------------

    private void Start()
    {
        if (enemyPrefabs.Count == 0)
            Debug.LogWarning("[WaveManager] No enemy prefabs assigned!");

        if (asteroidPrefabs.Count == 0)
            Debug.LogWarning("[WaveManager] No asteroid prefabs assigned!");

        StartCoroutine(EnemySpawnLoop());
        StartCoroutine(AsteroidSpawnLoop());
    }

    // -------------------------------------------------------------------------
    // Spawn Loops
    // -------------------------------------------------------------------------

    /// <summary>Continuously spawns enemies with a dynamically adjusted interval.</summary>
    private IEnumerator EnemySpawnLoop()
    {
        while (true)
        {
            float interval = GetCurrentEnemyInterval();
            yield return new WaitForSeconds(interval);

            // Clean up any destroyed enemies from our tracking list before checking the count
            activeEnemies.RemoveAll(e => e == null);

            if (activeEnemies.Count < maxActiveEnemies)
            {
                SpawnEnemy();
            }
            else
            {
                // Screen is too crowded — wait a short beat before checking again
                // instead of a full interval, so we react quickly when enemies leave
                yield return new WaitForSeconds(1f);
            }
        }
    }

    /// <summary>Continuously spawns asteroids with a dynamically adjusted interval.</summary>
    private IEnumerator AsteroidSpawnLoop()
    {
        while (true)
        {
            float interval = GetCurrentAsteroidInterval();
            yield return new WaitForSeconds(interval);

            SpawnAsteroid();
        }
    }

    // -------------------------------------------------------------------------
    // Spawn Helpers
    // -------------------------------------------------------------------------

    private void SpawnEnemy()
    {
        if (enemyPrefabs.Count == 0) return;

        GameObject prefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Count)];
        Vector3 spawnPos = GetRandomSpawnPosition();

        GameObject enemy = Instantiate(prefab, spawnPos, Quaternion.identity);
        activeEnemies.Add(enemy);
    }

    private void SpawnAsteroid()
    {
        if (asteroidPrefabs.Count == 0) return;

        GameObject prefab = asteroidPrefabs[Random.Range(0, asteroidPrefabs.Count)];
        Vector3 spawnPos = GetRandomSpawnPosition();

        Instantiate(prefab, spawnPos, Random.rotation);
    }

    /// <summary>Returns a random world position within the spawn corridor at the far Z distance.</summary>
    private Vector3 GetRandomSpawnPosition()
    {
        float x = Random.Range(-spawnRangeX, spawnRangeX);
        float y = Random.Range(-spawnRangeY, spawnRangeY);
        return new Vector3(x, y, spawnZDistance);
    }

    // -------------------------------------------------------------------------
    // Dynamic Difficulty
    // -------------------------------------------------------------------------

    /// <summary>
    /// Calculates the current enemy spawn interval based on time played.
    /// Uses a linear lerp from the initial interval down to the minimum,
    /// clamped so it never goes below <see cref="minEnemySpawnInterval"/>.
    /// </summary>
    private float GetCurrentEnemyInterval()
    {
        // t goes from 0.0 (start) to 1.0 (full difficulty reached)
        float t = Mathf.Clamp01(Time.timeSinceLevelLoad / difficultyRampDuration);
        return Mathf.Lerp(initialEnemySpawnInterval, minEnemySpawnInterval, t);
    }

    /// <summary>Calculates the current asteroid spawn interval based on time played.</summary>
    private float GetCurrentAsteroidInterval()
    {
        float t = Mathf.Clamp01(Time.timeSinceLevelLoad / difficultyRampDuration);
        return Mathf.Lerp(initialAsteroidSpawnInterval, minAsteroidSpawnInterval, t);
    }

    // -------------------------------------------------------------------------
    // Debug Gizmos (visible in Scene view, not in Game view)
    // -------------------------------------------------------------------------

    private void OnDrawGizmosSelected()
    {
        // Draw a wire box showing the spawn corridor in the Scene view for easy tuning
        Gizmos.color = Color.yellow;
        Vector3 center = new Vector3(0f, 0f, spawnZDistance);
        Vector3 size   = new Vector3(spawnRangeX * 2f, spawnRangeY * 2f, 0.5f);
        Gizmos.DrawWireCube(center, size);
    }
}
