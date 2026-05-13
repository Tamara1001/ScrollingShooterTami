using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaveManager : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // Singleton
    // -------------------------------------------------------------------------

    public static WaveManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

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

    // Tracks all live enemies so we can count and clean them up without FindObjectsByType.
    private readonly List<GameObject> activeEnemies   = new List<GameObject>();
    // Tracks all live asteroids so ResetWaves() can destroy them cleanly.
    private readonly List<GameObject> activeAsteroids = new List<GameObject>();

    private Coroutine enemySpawnCoroutine;
    private Coroutine asteroidSpawnCoroutine;

    // Separate elapsed time tracker so difficulty resets properly on ResetWaves()
    private float playTime = 0f;

    // -------------------------------------------------------------------------
    // Unity Lifecycle
    // -------------------------------------------------------------------------

    private void Start()
    {
        if (enemyPrefabs.Count == 0)
            Debug.LogWarning("[WaveManager] No enemy prefabs assigned!");

        if (asteroidPrefabs.Count == 0)
            Debug.LogWarning("[WaveManager] No asteroid prefabs assigned!");

        // Start both loops — they will idle until GameState == Playing
        enemySpawnCoroutine   = StartCoroutine(EnemySpawnLoop());
        asteroidSpawnCoroutine = StartCoroutine(AsteroidSpawnLoop());
    }

    private void Update()
    {
        // Only advance the difficulty timer while actually playing
        if (GameManager.Instance?.CurrentState == GameManager.GameState.Playing)
        {
            playTime += Time.deltaTime;
        }
    }

    // -------------------------------------------------------------------------
    // Public API
    // -------------------------------------------------------------------------

    /// <summary>
    /// Destroys every tracked enemy and asteroid in the scene and resets spawn timers.
    /// Call this from GameManager.StartGame() before transitioning to the Playing state.
    /// </summary>
    public void ResetWaves()
    {
        // 1. Stop the running spawn coroutines so nothing new is spawned mid-reset
        if (enemySpawnCoroutine   != null) StopCoroutine(enemySpawnCoroutine);
        if (asteroidSpawnCoroutine != null) StopCoroutine(asteroidSpawnCoroutine);
        enemySpawnCoroutine   = null;
        asteroidSpawnCoroutine = null;

        // 2. Destroy every enemy in our tracked list
        foreach (GameObject enemy in activeEnemies)
        {
            if (enemy != null) Destroy(enemy);
        }
        activeEnemies.Clear();

        // 3. Destroy every asteroid in our tracked list
        foreach (GameObject asteroid in activeAsteroids)
        {
            if (asteroid != null) Destroy(asteroid);
        }
        activeAsteroids.Clear();

        // 4. Safety sweep: catch any hazards that exist in the scene but were
        //    never added to our lists (e.g. manually placed in editor, or spawned
        //    by a code path we missed).
        foreach (GameObject stray in GameObject.FindGameObjectsWithTag("Enemy"))
            Destroy(stray);

        foreach (GameObject stray in GameObject.FindGameObjectsWithTag("Asteroid"))
            Destroy(stray);

        // 5. Reset the difficulty timer so the next round starts at initial difficulty
        playTime = 0f;

        // 6. Restart the loops — they will immediately idle until GameState == Playing
        enemySpawnCoroutine   = StartCoroutine(EnemySpawnLoop());
        asteroidSpawnCoroutine = StartCoroutine(AsteroidSpawnLoop());

        Debug.Log("[WaveManager] Board cleared and spawn loops restarted.");
    }

    // -------------------------------------------------------------------------
    // Spawn Loops
    // -------------------------------------------------------------------------

    /// <summary>Continuously spawns enemies with a dynamically adjusted interval.</summary>
    private IEnumerator EnemySpawnLoop()
    {
        while (true)
        {
            // Idle until we are in the Playing state — this is the core state guard
            if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameManager.GameState.Playing)
            {
                yield return null; // wait one frame and check again
                continue;
            }

            float interval = GetCurrentEnemyInterval();
            yield return new WaitForSeconds(interval);

            // Re-check after waiting — state may have changed (e.g., player died mid-interval)
            if (GameManager.Instance?.CurrentState != GameManager.GameState.Playing)
                continue;

            // Clean up destroyed entries before checking the count
            activeEnemies.RemoveAll(e => e == null);

            if (activeEnemies.Count < maxActiveEnemies)
            {
                SpawnEnemy();
            }
            else
            {
                // Too crowded — brief pause before checking again
                yield return new WaitForSeconds(1f);
            }
        }
    }

    /// <summary>Continuously spawns asteroids with a dynamically adjusted interval.</summary>
    private IEnumerator AsteroidSpawnLoop()
    {
        while (true)
        {
            // Idle until we are in the Playing state
            if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameManager.GameState.Playing)
            {
                yield return null;
                continue;
            }

            float interval = GetCurrentAsteroidInterval();
            yield return new WaitForSeconds(interval);

            // Re-check after waiting
            if (GameManager.Instance?.CurrentState != GameManager.GameState.Playing)
                continue;

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
        Vector3 spawnPos  = GetRandomSpawnPosition();

        GameObject enemy = Instantiate(prefab, spawnPos, Quaternion.identity);
        activeEnemies.Add(enemy);
    }

    private void SpawnAsteroid()
    {
        if (asteroidPrefabs.Count == 0) return;

        GameObject prefab   = asteroidPrefabs[Random.Range(0, asteroidPrefabs.Count)];
        Vector3 spawnPos    = GetRandomSpawnPosition();

        GameObject asteroid = Instantiate(prefab, spawnPos, Random.rotation);
        activeAsteroids.Add(asteroid);
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
    /// Uses playTime (our own counter) instead of Time.timeSinceLevelLoad so
    /// difficulty resets correctly when ResetWaves() is called.
    /// </summary>
    private float GetCurrentEnemyInterval()
    {
        float t = Mathf.Clamp01(playTime / difficultyRampDuration);
        return Mathf.Lerp(initialEnemySpawnInterval, minEnemySpawnInterval, t);
    }

    /// <summary>Calculates the current asteroid spawn interval based on time played.</summary>
    private float GetCurrentAsteroidInterval()
    {
        float t = Mathf.Clamp01(playTime / difficultyRampDuration);
        return Mathf.Lerp(initialAsteroidSpawnInterval, minAsteroidSpawnInterval, t);
    }

    // -------------------------------------------------------------------------
    // Debug Gizmos (visible in Scene view only)
    // -------------------------------------------------------------------------

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Vector3 center = new Vector3(0f, 0f, spawnZDistance);
        Vector3 size   = new Vector3(spawnRangeX * 2f, spawnRangeY * 2f, 0.5f);
        Gizmos.DrawWireCube(center, size);
    }
}
