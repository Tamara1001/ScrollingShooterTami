using System.Collections;
using UnityEngine;

/// <summary>
/// Briefly swaps the renderer's material to a flash material, then restores it.
/// Attach to any GameObject that needs a hit-flash reaction.
/// Call Flash() from TakeDamage() to trigger the effect.
/// </summary>
[RequireComponent(typeof(MeshRenderer))]
public class DamageFlash : MonoBehaviour
{
    [Header("Flash Settings")]
    [Tooltip("The material swapped in for the flash frame. Use NeonWhite.mat for a universal bright flash.")]
    [SerializeField] private Material flashMaterial;

    [Tooltip("How long (seconds) the flash material stays visible before reverting.")]
    [SerializeField] private float flashDuration = 0.08f;

    // -------------------------------------------------------------------------
    // Private State
    // -------------------------------------------------------------------------

    private MeshRenderer   meshRenderer;
    private Material[]     originalMaterials;   // cached on Awake — supports multi-material meshes
    private Coroutine      activeFlash;

    // -------------------------------------------------------------------------
    // Unity Lifecycle
    // -------------------------------------------------------------------------

    private void Awake()
    {
        meshRenderer      = GetComponent<MeshRenderer>();
        // Cache a copy of the original materials array (NOT sharedMaterials, to avoid
        // modifying the asset on disk or affecting other instances of the same prefab)
        originalMaterials = meshRenderer.materials;
    }

    // -------------------------------------------------------------------------
    // Public API
    // -------------------------------------------------------------------------

    /// <summary>
    /// Triggers a single hit-flash. Safe to call mid-flash — the previous
    /// flash is cancelled cleanly and a fresh one starts immediately.
    /// </summary>
    public void Flash()
    {
        if (flashMaterial == null)
        {
            Debug.LogWarning($"[DamageFlash] No flash material assigned on {gameObject.name}.");
            return;
        }

        // Cancel any running flash so we don't stack coroutines
        if (activeFlash != null)
            StopCoroutine(activeFlash);

        activeFlash = StartCoroutine(FlashRoutine());
    }

    // -------------------------------------------------------------------------
    // Coroutine
    // -------------------------------------------------------------------------

    private IEnumerator FlashRoutine()
    {
        // Build a new materials array where every slot is the flash material.
        // This handles multi-material meshes (e.g., a ship with cockpit + hull).
        Material[] flashMats = new Material[originalMaterials.Length];
        for (int i = 0; i < flashMats.Length; i++)
            flashMats[i] = flashMaterial;

        meshRenderer.materials = flashMats;

        yield return new WaitForSeconds(flashDuration);

        // Restore all original materials
        meshRenderer.materials = originalMaterials;
        activeFlash = null;
    }

    // -------------------------------------------------------------------------
    // Cleanup — ensure original materials are restored if the object is disabled
    // (e.g., player dies while flashing)
    // -------------------------------------------------------------------------

    private void OnDisable()
    {
        if (activeFlash != null)
        {
            StopCoroutine(activeFlash);
            activeFlash = null;
        }

        // Only restore if the renderer is still valid
        if (meshRenderer != null && originalMaterials != null)
            meshRenderer.materials = originalMaterials;
    }
}
