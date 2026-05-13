using UnityEngine;

/// <summary>
/// Central Singleton that owns game state, runtime stats, and PlayerPrefs persistence.
/// Access from any script via GameManager.Instance.
/// </summary>
public class GameManager : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // Singleton
    // -------------------------------------------------------------------------

    public static GameManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Explicitly set the initial state here in Awake so it is guaranteed to be
        // MainMenu before any other script's Start() reads it.
        CurrentState = GameState.MainMenu;
    }

    // -------------------------------------------------------------------------
    // State Enum
    // -------------------------------------------------------------------------

    public enum GameState { MainMenu, Playing, GameOver, Win }

    // -------------------------------------------------------------------------
    // Inspector Fields
    // -------------------------------------------------------------------------

    [Header("Win Condition")]
    [Tooltip("How many seconds the player must survive to trigger the Win state.")]
    [SerializeField] private float winDuration = 180f;

    [Header("Scoring")]
    [Tooltip("Points awarded for each enemy destroyed.")]
    [SerializeField] private int pointsPerEnemy = 100;
    [Tooltip("Points awarded for each asteroid destroyed.")]
    [SerializeField] private int pointsPerAsteroid = 25;

    [Header("References")]
    [Tooltip("Drag the PlayerShip GameObject here. Auto-found via FindFirstObjectByType if left empty.")]
    [SerializeField] private PlayerStats playerStats;

    // -------------------------------------------------------------------------
    // Public Runtime State
    // -------------------------------------------------------------------------

    public GameState CurrentState   { get; private set; } = GameState.MainMenu;
    public int       CurrentScore   { get; private set; }
    public float     CurrentTime    { get; private set; }
    public int       EnemiesDefeated { get; private set; }

    // -------------------------------------------------------------------------
    // PlayerPrefs Keys
    // -------------------------------------------------------------------------

    private const string PREF_BEST_SCORE = "BestScore";
    private const string PREF_BEST_TIME  = "BestTime";

    public int   BestScore => PlayerPrefs.GetInt(PREF_BEST_SCORE, 0);
    public float BestTime  => PlayerPrefs.GetFloat(PREF_BEST_TIME, 0f);

    // -------------------------------------------------------------------------
    // Unity Lifecycle
    // -------------------------------------------------------------------------

    private void Start()
    {
        // Cache the PlayerStats reference. Inspector assignment is preferred;
        // fallback to FindFirstObjectByType only on first boot.
        if (playerStats == null)
        {
            playerStats = FindFirstObjectByType<PlayerStats>();
            if (playerStats == null)
                Debug.LogWarning("[GameManager] PlayerStats not found! Assign it in the Inspector.");
        }

        // All Awake() methods across all scripts have now completed.
        // UIManager.Awake() has already hidden every panel.
        // We can safely call ShowPanel() here — it will not be overwritten.
        UIManager.Instance?.ShowPanel(CurrentState);
    }

    private void Update()
    {
        if (CurrentState != GameState.Playing) return;

        CurrentTime += Time.deltaTime;

        // Win Condition: player survived the full duration
        if (CurrentTime >= winDuration)
        {
            TriggerWin();
        }
    }

    // -------------------------------------------------------------------------
    // Public Game Flow Methods
    // -------------------------------------------------------------------------

    /// <summary>Transitions to the Playing state from the Main Menu.</summary>
    public void StartGame()
    {
        CurrentScore    = 0;
        CurrentTime     = 0f;
        EnemiesDefeated = 0;

        WaveManager.Instance?.ResetWaves();

        playerStats?.ResetStats(); // ResetStats() also calls SetActive(true)

        SetState(GameState.Playing);
    }

    /// <summary>
    /// Full board reset called by the Retry button from a Game Over or Win screen.
    /// Executes the reset in a strict order to prevent any single-frame state glitches.
    /// </summary>
    public void RestartGame()
    {
        // Step 1: Reset runtime counters FIRST, before anything reads them
        CurrentScore    = 0;
        CurrentTime     = 0f;
        EnemiesDefeated = 0;

        // Step 2: Clear all live hazards from the board.
        //         ResetWaves() is called while state is still GameOver/Win,
        //         so the spawn loops see a non-Playing state and don't fire.
        WaveManager.Instance?.ResetWaves();

        // Step 3: Restore the player — resets health/energy, re-centers, re-activates.
        playerStats?.ResetStats();

        // Step 4: Flip the state to Playing (WaveManager loops now unblock next frame)
        CurrentState = GameState.Playing;

        // Step 5: Show the HUD panel
        UIManager.Instance?.ShowPanel(GameState.Playing);

        // Step 6: Force an immediate HUD flush so bars/score don't show stale values
        //         for even one frame before PlayerStats.Update() kicks in.
        if (playerStats != null)
        {
            UIManager.Instance?.UpdateHUD(
                playerStats.CurrentHealth, playerStats.MaxHealth,
                playerStats.CurrentEnergy, playerStats.MaxEnergy,
                CurrentScore,
                CurrentTime,
                EnemiesDefeated
            );
        }

        Debug.Log("[GameManager] Game restarted cleanly.");
    }

    /// <summary>Returns to the Main Menu state and clears the field.</summary>
    public void ReturnToMenu()
    {
        WaveManager.Instance?.ResetWaves();
        SetState(GameState.MainMenu);
    }

    // -------------------------------------------------------------------------
    // Score & Stats
    // -------------------------------------------------------------------------

    /// <summary>Adds to the current score. Pass GameManager.Instance.pointsPerEnemy etc. from enemy/asteroid scripts.</summary>
    public void AddScore(int amount)
    {
        if (CurrentState != GameState.Playing) return;
        CurrentScore += amount;
        UIManager.Instance?.RefreshScoreText(CurrentScore);
    }

    public void RegisterEnemyKill()
    {
        if (CurrentState != GameState.Playing) return;
        EnemiesDefeated++;
        AddScore(pointsPerEnemy);
    }

    public void RegisterAsteroidKill()
    {
        if (CurrentState != GameState.Playing) return;
        AddScore(pointsPerAsteroid);
    }

    // -------------------------------------------------------------------------
    // End Conditions
    // -------------------------------------------------------------------------

    /// <summary>Called by PlayerStats when the player's health reaches zero.</summary>
    public void TriggerGameOver()
    {
        if (CurrentState != GameState.Playing) return;

        SaveBestStats();
        SetState(GameState.GameOver);
    }

    private void TriggerWin()
    {
        SaveBestStats();
        SetState(GameState.Win);
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private void SetState(GameState newState)
    {
        CurrentState = newState;
        UIManager.Instance?.ShowPanel(newState);

        if (newState == GameState.GameOver || newState == GameState.Win)
        {
            UIManager.Instance?.UpdateEndScreens(CurrentScore, CurrentTime, EnemiesDefeated, BestScore, BestTime);
        }
    }

    private void SaveBestStats()
    {
        if (CurrentScore > BestScore)
            PlayerPrefs.SetInt(PREF_BEST_SCORE, CurrentScore);

        if (CurrentTime > BestTime)
            PlayerPrefs.SetFloat(PREF_BEST_TIME, CurrentTime);

        PlayerPrefs.Save();
    }
}
