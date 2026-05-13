using System.Collections;
using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // Inspector Fields
    // -------------------------------------------------------------------------

    [Header("Health")]
    [Tooltip("Maximum hit points the player can have.")]
    [SerializeField] private int maxHealth = 5;

    [Header("Energy")]
    [Tooltip("Maximum energy the player can have.")]
    [SerializeField] private float maxEnergy = 100f;
    [Tooltip("How much energy regenerates per second when not in overheat.")]
    [SerializeField] private float energyRegenRate = 15f;
    [Tooltip("Energy consumed every time the player fires one shot.")]
    [SerializeField] private float energyCostPerShot = 8f;

    [Header("Overheat")]
    [Tooltip("How many seconds the player cannot fire after the energy bar hits zero.")]
    [SerializeField] private float overheatPenaltyDuration = 3f;

    // -------------------------------------------------------------------------
    // Public Read-Only State (for UI, etc.)
    // -------------------------------------------------------------------------

    public int   CurrentHealth  { get; private set; }
    public float CurrentEnergy  { get; private set; }
    public float MaxHealth      => maxHealth;
    public float MaxEnergy      => maxEnergy;

    /// <summary>True while the player is in the overheat cooldown and cannot fire.</summary>
    public bool IsOverheated    { get; private set; }

    // -------------------------------------------------------------------------
    // Unity Lifecycle
    // -------------------------------------------------------------------------

    private void Awake()
    {
        CurrentHealth = maxHealth;
        CurrentEnergy = maxEnergy;
        IsOverheated  = false;
    }

    private void Update()
    {
        RegenerateEnergy();

        // Push live stats to the HUD every frame
        if (GameManager.Instance?.CurrentState == GameManager.GameState.Playing)
        {
            UIManager.Instance?.UpdateHUD(
                CurrentHealth, maxHealth,
                CurrentEnergy, maxEnergy,
                GameManager.Instance.CurrentScore,
                GameManager.Instance.CurrentTime
            );
        }
    }

    // -------------------------------------------------------------------------
    // Energy
    // -------------------------------------------------------------------------

    private void RegenerateEnergy()
    {
        // Only regen when not overheated and not already at max
        if (!IsOverheated && CurrentEnergy < maxEnergy)
        {
            CurrentEnergy = Mathf.Min(CurrentEnergy + energyRegenRate * Time.deltaTime, maxEnergy);
        }
    }

    /// <summary>
    /// Attempts to consume energy for a single shot.
    /// Returns TRUE if the shot is allowed, FALSE if there is not enough energy.
    /// Triggers an Overheat state if energy reaches zero.
    /// </summary>
    public bool ConsumeEnergy()
    {
        // Block any new shot attempts while overheated
        if (IsOverheated)
            return false;

        if (CurrentEnergy <= 0f)
        {
            StartCoroutine(OverheatRoutine());
            return false;
        }

        CurrentEnergy = Mathf.Max(0f, CurrentEnergy - energyCostPerShot);

        // If draining this shot emptied the bar, trigger overheat immediately
        if (CurrentEnergy <= 0f)
        {
            StartCoroutine(OverheatRoutine());
        }

        // The shot itself was still allowed — energy was above zero when we started
        return true;
    }

    private IEnumerator OverheatRoutine()
    {
        IsOverheated = true;
        Debug.Log("[PlayerStats] OVERHEAT! Cannot fire for " + overheatPenaltyDuration + " seconds.");

        yield return new WaitForSeconds(overheatPenaltyDuration);

        IsOverheated = false;
        Debug.Log("[PlayerStats] Overheat cleared. Firing restored.");
    }

    // -------------------------------------------------------------------------
    // Health
    // -------------------------------------------------------------------------

    /// <summary>Applies damage to the player. Logs death when health reaches zero.</summary>
    public void TakeDamage(int amount)
    {
        if (amount <= 0) return;

        CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
        Debug.Log($"[PlayerStats] Player took {amount} damage. Health: {CurrentHealth}/{maxHealth}");

        if (CurrentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        // Notify the GameManager — it will handle the panel transition
        Debug.Log("[PlayerStats] Player has died.");
        GameManager.Instance?.TriggerGameOver();
        gameObject.SetActive(false);
    }

    /// <summary>
    /// Fully resets the player for a new round:
    /// restores health/energy, clears overheat, re-centers the ship, and re-activates it.
    /// Called by GameManager before transitioning to Playing state.
    /// </summary>
    public void ResetStats()
    {
        // Cancel any running coroutines (e.g. OverheatRoutine) so they
        // don't carry stale state into the new round.
        StopAllCoroutines();

        // Restore stats to full
        CurrentHealth = maxHealth;
        CurrentEnergy = maxEnergy;
        IsOverheated  = false;

        // Re-center the ship so it spawns in a predictable position
        transform.localPosition = Vector3.zero;

        // Re-activate the GameObject (was disabled by Die())
        gameObject.SetActive(true);

        Debug.Log("[PlayerStats] Player fully reset and respawned.");
    }
}
