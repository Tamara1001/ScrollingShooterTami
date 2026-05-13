using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Owns all UI panel references and routes display calls from GameManager.
/// </summary>
public class UIManager : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // Singleton
    // -------------------------------------------------------------------------

    public static UIManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Hide all panels immediately in Awake so this is guaranteed to run
        // before ANY script's Start() — including GameManager.Start() which
        // will call ShowPanel() to reveal the correct one.
        SetAllPanelsInactive();
    }

    // -------------------------------------------------------------------------
    // Panel References
    // -------------------------------------------------------------------------

    [Header("Panels (assign all four root panel GameObjects)")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject hudPanel;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private GameObject winPanel;

    // -------------------------------------------------------------------------
    // HUD References
    // -------------------------------------------------------------------------

    [Header("HUD Elements")]
    [Tooltip("Slider used as the Health bar. Set Max Value to player's Max Health.")]
    [SerializeField] private Slider healthSlider;
    [Tooltip("Slider used as the Energy bar. Set Max Value to player's Max Energy.")]
    [SerializeField] private Slider energySlider;
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI timeText;
    [SerializeField] private TextMeshProUGUI hudEnemiesText;

    // -------------------------------------------------------------------------
    // Game Over Panel References
    // -------------------------------------------------------------------------

    [Header("Game Over Panel")]
    [SerializeField] private TextMeshProUGUI goScoreText;
    [SerializeField] private TextMeshProUGUI goTimeText;
    [SerializeField] private TextMeshProUGUI goEnemiesText;
    [SerializeField] private TextMeshProUGUI goBestScoreText;
    [SerializeField] private TextMeshProUGUI goBestTimeText;

    // -------------------------------------------------------------------------
    // Win Panel References
    // -------------------------------------------------------------------------

    [Header("Win Panel")]
    [SerializeField] private TextMeshProUGUI winScoreText;
    [SerializeField] private TextMeshProUGUI winTimeText;
    [SerializeField] private TextMeshProUGUI winEnemiesText;
    [SerializeField] private TextMeshProUGUI winBestScoreText;
    [SerializeField] private TextMeshProUGUI winBestTimeText;

    // -------------------------------------------------------------------------
    // Main Menu References
    // -------------------------------------------------------------------------

    [Header("Main Menu")]
    [SerializeField] private TextMeshProUGUI menuBestScoreText;
    [SerializeField] private TextMeshProUGUI menuBestTimeText;

    // -------------------------------------------------------------------------
    // Unity Lifecycle
    // -------------------------------------------------------------------------

    // Start() intentionally omitted — panel initialization is handled in Awake()
    // and the first ShowPanel() call comes from GameManager.Start().

    // -------------------------------------------------------------------------
    // Panel Management
    // -------------------------------------------------------------------------

    /// <summary>Shows the panel that matches the given game state, hides all others.</summary>
    public void ShowPanel(GameManager.GameState state)
    {
        SetAllPanelsInactive();

        switch (state)
        {
            case GameManager.GameState.MainMenu:
                mainMenuPanel?.SetActive(true);
                RefreshMainMenuBestStats();
                break;

            case GameManager.GameState.Playing:
                hudPanel?.SetActive(true);
                break;

            case GameManager.GameState.GameOver:
                gameOverPanel?.SetActive(true);
                break;

            case GameManager.GameState.Win:
                winPanel?.SetActive(true);
                break;
        }
    }

    private void SetAllPanelsInactive()
    {
        mainMenuPanel?.SetActive(false);
        hudPanel?.SetActive(false);
        gameOverPanel?.SetActive(false);
        winPanel?.SetActive(false);
    }

    // -------------------------------------------------------------------------
    // HUD Updates
    // -------------------------------------------------------------------------

    /// <summary>Call this every frame (or on change) from PlayerStats to keep HUD current.</summary>
    public void UpdateHUD(float currentHealth, float maxHealth, float currentEnergy, float maxEnergy, int score, float time, int enemiesDefeated)
    {
        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealth;
            healthSlider.value    = currentHealth;
        }

        if (energySlider != null)
        {
            energySlider.maxValue = maxEnergy;
            energySlider.value    = currentEnergy;
        }

        if (scoreText != null)
            scoreText.text = $"SCORE  {score:N0}";

        if (timeText != null)
            timeText.text = FormatTime(time);

        if (hudEnemiesText != null)
            hudEnemiesText.text = $"ENEMIES  {enemiesDefeated}";
    }

    /// <summary>Refreshes only the score text (called by GameManager.AddScore for efficiency).</summary>
    public void RefreshScoreText(int score)
    {
        if (scoreText != null)
            scoreText.text = $"SCORE  {score:N0}";
    }

    // -------------------------------------------------------------------------
    // End Screen Updates
    // -------------------------------------------------------------------------

    /// <summary>Populates both the Game Over and Win panels with the session stats.</summary>
    public void UpdateEndScreens(int score, float time, int enemiesDefeated, int bestScore, float bestTime)
    {
        string scoreStr   = score.ToString("N0");
        string timeStr    = FormatTime(time);
        string enemyStr   = enemiesDefeated.ToString();
        string bScoreStr  = bestScore.ToString("N0");
        string bTimeStr   = FormatTime(bestTime);

        // Game Over panel
        SetText(goScoreText,   $"Score: {scoreStr}");
        SetText(goTimeText,    $"Time Survived: {timeStr}");
        SetText(goEnemiesText, $"Enemies Defeated: {enemyStr}");
        SetText(goBestScoreText, $"Best Score: {bScoreStr}");
        SetText(goBestTimeText,  $"Best Time: {bTimeStr}");

        // Win panel
        SetText(winScoreText,   $"Score: {scoreStr}");
        SetText(winTimeText,    $"Time: {timeStr}");
        SetText(winEnemiesText, $"Enemies Defeated: {enemyStr}");
        SetText(winBestScoreText, $"Best Score: {bScoreStr}");
        SetText(winBestTimeText,  $"Best Time: {bTimeStr}");
    }

    private void RefreshMainMenuBestStats()
    {
        if (GameManager.Instance == null) return;
        SetText(menuBestScoreText, $"Best Score: {GameManager.Instance.BestScore:N0}");
        SetText(menuBestTimeText,  $"Best Time:  {FormatTime(GameManager.Instance.BestTime)}");
    }

    // -------------------------------------------------------------------------
    // Button Callbacks (wire these up to UI Button OnClick events in the Inspector)
    // -------------------------------------------------------------------------

    public void OnClickPlay()
    {
        GameManager.Instance?.StartGame();
    }

    public void OnClickMenu()
    {
        GameManager.Instance?.ReturnToMenu();
    }

    public void OnClickRestart()
    {
        GameManager.Instance?.RestartGame();
    }

    // -------------------------------------------------------------------------
    // Utility
    // -------------------------------------------------------------------------

    private static void SetText(TextMeshProUGUI tmp, string value)
    {
        if (tmp != null) tmp.text = value;
    }

    /// <summary>Converts a raw float of seconds into a MM:SS display string.</summary>
    private static string FormatTime(float seconds)
    {
        int m = Mathf.FloorToInt(seconds / 60f);
        int s = Mathf.FloorToInt(seconds % 60f);
        return $"{m:00}:{s:00}";
    }
}
