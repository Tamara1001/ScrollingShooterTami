using System.Collections;
using UnityEngine;

/// <summary>
/// Lightweight camera shake system. Attach to the Main Camera.
/// Call from anywhere via CameraShake.Instance.Shake(duration, magnitude).
/// </summary>
public class CameraShake : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // Singleton
    // -------------------------------------------------------------------------

    public static CameraShake Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
    }

    // -------------------------------------------------------------------------
    // State
    // -------------------------------------------------------------------------

    // The resting local position the camera returns to after each shake.
    // Captured fresh at the start of every shake so nested shakes don't drift.
    private Vector3  originalLocalPosition;
    private Coroutine activeShake;

    // -------------------------------------------------------------------------
    // Public API
    // -------------------------------------------------------------------------

    /// <summary>
    /// Starts a camera shake. Safe to call while a shake is already running —
    /// the new call wins (it cancels the current shake cleanly first).
    /// </summary>
    /// <param name="duration">How long the shake lasts in seconds.</param>
    /// <param name="magnitude">Maximum displacement in world units per frame.</param>
    public void Shake(float duration, float magnitude)
    {
        // Cancel any in-progress shake so we don't get competing coroutines
        if (activeShake != null)
        {
            StopCoroutine(activeShake);
            // Snap back immediately before starting the new shake, so
            // originalLocalPosition is captured from a clean baseline.
            transform.localPosition = originalLocalPosition;
        }

        activeShake = StartCoroutine(ShakeRoutine(duration, magnitude));
    }

    // -------------------------------------------------------------------------
    // Coroutine
    // -------------------------------------------------------------------------

    private IEnumerator ShakeRoutine(float duration, float magnitude)
    {
        // Capture the resting position at the START of each shake
        originalLocalPosition = transform.localPosition;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            // Lerp magnitude from full → zero over the duration for a smooth decay
            float currentMagnitude = Mathf.Lerp(magnitude, 0f, elapsed / duration);

            // Offset in a random sphere, projected to XY only (no Z depth jitter)
            Vector3 offset = Random.insideUnitSphere * currentMagnitude;
            offset.z = 0f;  // keep Z locked — camera only shakes laterally

            transform.localPosition = originalLocalPosition + offset;

            elapsed += Time.deltaTime;
            yield return null; // advance one frame
        }

        // Always snap back to the exact resting position when done
        transform.localPosition = originalLocalPosition;
        activeShake = null;
    }
}
