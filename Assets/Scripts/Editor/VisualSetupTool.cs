// VisualSetupTool.cs
// Place this file inside:  Assets/Scripts/Editor/
//
// Unity 6, Universal Render Pipeline (URP)
// Adds a menu item: Tools > Setup Neon Visuals
//
// What it does automatically:
//   1. Creates three HDR-emissive URP Unlit materials (Cyan, Magenta, White).
//   2. Adds a Global Post-Processing Volume to the active scene with
//      Bloom, Vignette, and Chromatic Aberration configured for a neon look.

using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class VisualSetupTool
{
    // -------------------------------------------------------------------------
    // Paths
    // -------------------------------------------------------------------------

    private const string MATERIALS_FOLDER = "Assets/Materials";
    private const string NEON_FOLDER      = "Assets/Materials/Neon";
    private const string SETTINGS_FOLDER  = "Assets/Settings";
    private const string PROFILE_PATH     = "Assets/Settings/NeonPostProcessProfile.asset";

    // URP Unlit shader name (works in URP 14+ / Unity 6)
    private const string URP_UNLIT_SHADER = "Universal Render Pipeline/Unlit";

    // -------------------------------------------------------------------------
    // Menu Entry
    // -------------------------------------------------------------------------

    [MenuItem("Tools/Setup Neon Visuals")]
    public static void SetupNeonVisuals()
    {
        // Run both tasks and show a single dialog at the end
        CreateNeonMaterials();
        SetupPostProcessingVolume();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[VisualSetupTool] ✓ Neon materials and Post-Processing Volume created successfully.");
        EditorUtility.DisplayDialog(
            "Neon Visuals Setup Complete",
            "✓ 3 HDR Neon materials saved to Assets/Materials/Neon/\n" +
            "✓ Global Post-Processing Volume added to the active scene\n" +
            "✓ VolumeProfile saved to Assets/Settings/NeonPostProcessProfile.asset\n\n" +
            "Tip: Make sure your Camera has 'Post Processing' enabled in its inspector.",
            "Got it!"
        );
    }

    // =========================================================================
    // TASK 1 — MATERIALS
    // =========================================================================

    private static void CreateNeonMaterials()
    {
        // Ensure the folder hierarchy exists
        EnsureFolderExists(MATERIALS_FOLDER, "Materials");
        EnsureFolderExists(NEON_FOLDER,      "Neon");

        Shader unlitShader = Shader.Find(URP_UNLIT_SHADER);
        if (unlitShader == null)
        {
            Debug.LogError(
                "[VisualSetupTool] Could not find shader '" + URP_UNLIT_SHADER + "'. " +
                "Make sure Universal Render Pipeline is installed and active.");
            return;
        }

        // Emission intensity multiplier (HDR). Value of 4 = very bright neon glow
        const float emissionIntensity = 4f;

        CreateNeonMaterial(unlitShader, "NeonCyan",
            baseColor:      Color.cyan,
            emissionColor:  Color.cyan * emissionIntensity);

        CreateNeonMaterial(unlitShader, "NeonMagenta",
            baseColor:      Color.magenta,
            emissionColor:  Color.magenta * emissionIntensity);

        CreateNeonMaterial(unlitShader, "NeonWhite",
            baseColor:      Color.white,
            emissionColor:  Color.white * emissionIntensity);
    }

    /// <summary>
    /// Creates (or overwrites) a single URP Unlit material with HDR emission at the Neon folder.
    /// </summary>
    private static void CreateNeonMaterial(Shader shader, string materialName, Color baseColor, Color emissionColor)
    {
        string assetPath = $"{NEON_FOLDER}/{materialName}.mat";

        // If a material already exists at this path, reuse it; otherwise create fresh
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
        if (mat == null)
        {
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, assetPath);
        }
        else
        {
            mat.shader = shader;
        }

        // Base (albedo) colour
        mat.SetColor("_BaseColor", baseColor);

        // Enable Emission keyword and set HDR colour
        // The URP Unlit shader exposes "_EmissionColor" and the keyword "EMISSION"
        mat.EnableKeyword("_EMISSION");
        mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        mat.SetColor("_EmissionColor", emissionColor);

        EditorUtility.SetDirty(mat);
        Debug.Log($"[VisualSetupTool]   Material created: {assetPath}");
    }

    // =========================================================================
    // TASK 2 — POST-PROCESSING VOLUME
    // =========================================================================

    private static void SetupPostProcessingVolume()
    {
        EnsureFolderExists(SETTINGS_FOLDER, "Settings");

        // ----- VolumeProfile Asset -----
        VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(PROFILE_PATH);
        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, PROFILE_PATH);
        }

        // Add overrides (returns existing component if already present)
        ConfigureBloom(profile);
        ConfigureVignette(profile);
        ConfigureChromaticAberration(profile);

        EditorUtility.SetDirty(profile);

        // ----- Scene Volume GameObject -----
        // Reuse an existing one if it's already there
        const string volumeGoName = "Global Post-Processing Volume";
        GameObject volumeGo = GameObject.Find(volumeGoName);
        if (volumeGo == null)
        {
            volumeGo = new GameObject(volumeGoName);
            Undo.RegisterCreatedObjectUndo(volumeGo, "Create Post-Processing Volume");
        }

        Volume volume = volumeGo.GetComponent<Volume>();
        if (volume == null)
            volume = Undo.AddComponent<Volume>(volumeGo);

        volume.isGlobal  = true;
        volume.priority  = 1f;
        volume.profile   = profile;

        // Mark the scene dirty so the user is prompted to save
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        Debug.Log($"[VisualSetupTool]   Volume profile saved: {PROFILE_PATH}");
        Debug.Log($"[VisualSetupTool]   Scene object created: '{volumeGoName}'");
    }

    // ---- Individual Override Configurators ----

    private static void ConfigureBloom(VolumeProfile profile)
    {
        if (!profile.TryGet<Bloom>(out Bloom bloom))
            bloom = profile.Add<Bloom>(overrides: true);

        bloom.active           = true;
        bloom.threshold.value  = 0.9f;
        bloom.threshold.overrideState = true;
        bloom.intensity.value  = 2.5f;
        bloom.intensity.overrideState = true;
    }

    private static void ConfigureVignette(VolumeProfile profile)
    {
        if (!profile.TryGet<Vignette>(out Vignette vignette))
            vignette = profile.Add<Vignette>(overrides: true);

        vignette.active            = true;
        vignette.intensity.value   = 0.35f;
        vignette.intensity.overrideState = true;
        vignette.smoothness.value  = 0.4f;
        vignette.smoothness.overrideState = true;
    }

    private static void ConfigureChromaticAberration(VolumeProfile profile)
    {
        if (!profile.TryGet<ChromaticAberration>(out ChromaticAberration ca))
            ca = profile.Add<ChromaticAberration>(overrides: true);

        ca.active           = true;
        ca.intensity.value  = 0.15f;
        ca.intensity.overrideState = true;
    }

    // =========================================================================
    // Helpers
    // =========================================================================

    /// <summary>
    /// Creates a subfolder inside <paramref name="parentPath"/> if it does not exist.
    /// Uses AssetDatabase so Unity correctly generates .meta files.
    /// </summary>
    private static void EnsureFolderExists(string fullPath, string folderName)
    {
        if (!AssetDatabase.IsValidFolder(fullPath))
        {
            // Derive parent by stripping the last segment
            string parent = fullPath.Substring(0, fullPath.Length - folderName.Length - 1);
            AssetDatabase.CreateFolder(parent, folderName);
            Debug.Log($"[VisualSetupTool]   Created folder: {fullPath}");
        }
    }
}
