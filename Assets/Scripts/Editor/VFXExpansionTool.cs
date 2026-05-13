// VFXExpansionTool.cs
// Place this file inside:  Assets/Scripts/Editor/
//
// Unity 6, Universal Render Pipeline (URP)
// Adds a menu item: Tools > Generate Missing VFX
//
// What it does automatically:
//   1. Creates NeonGrey material at Assets/Materials/Neon/NeonGrey.mat
//   2. Creates Explosion_Grey.prefab  (uses NeonGrey)
//   3. Creates Explosion_White.prefab (uses existing NeonWhite)

using UnityEditor;
using UnityEngine;

public static class VFXExpansionTool
{
    // -------------------------------------------------------------------------
    // Paths
    // -------------------------------------------------------------------------

    private const string NEON_FOLDER       = "Assets/Materials/Neon";
    private const string PARTICLES_FOLDER  = "Assets/Prefabs/Particles";
    private const string PREFABS_FOLDER    = "Assets/Prefabs";

    private const string GREY_MAT_PATH     = "Assets/Materials/Neon/NeonGrey.mat";
    private const string WHITE_MAT_PATH    = "Assets/Materials/Neon/NeonWhite.mat";
    private const string URP_UNLIT_SHADER  = "Universal Render Pipeline/Unlit";

    // -------------------------------------------------------------------------
    // Menu Entry
    // -------------------------------------------------------------------------

    [MenuItem("Tools/Generate Missing VFX")]
    public static void GenerateMissingVFX()
    {
        // Ensure folder hierarchy
        EnsureFolderExists("Assets/Materials", "Materials");
        EnsureFolderExists(NEON_FOLDER,        "Neon");
        EnsureFolderExists(PREFABS_FOLDER,     "Prefabs");
        EnsureFolderExists(PARTICLES_FOLDER,   "Particles");

        // ---- Step 1: Create NeonGrey material ----
        Material greyMat = CreateNeonMaterial(
            name:            "NeonGrey",
            assetPath:       GREY_MAT_PATH,
            baseColor:       new Color(0.55f, 0.55f, 0.55f),   // mid-grey
            emissionColor:   new Color(0.55f, 0.55f, 0.55f) * 1.5f);  // low-intensity

        // ---- Step 2: Load existing NeonWhite ----
        Material whiteMat = AssetDatabase.LoadAssetAtPath<Material>(WHITE_MAT_PATH);
        if (whiteMat == null)
        {
            Debug.LogError(
                "[VFXExpansionTool] NeonWhite.mat not found at " + WHITE_MAT_PATH + ". " +
                "Run  Tools > Setup Neon Visuals  first, then try again.");
            return;
        }

        // ---- Step 3: Get the built-in Cube mesh ----
        Mesh cubeMesh = GetBuiltInCubeMesh();

        // ---- Step 4: Create explosion prefabs ----
        CreateExplosionPrefab("Explosion_Grey",  greyMat,  cubeMesh);
        CreateExplosionPrefab("Explosion_White", whiteMat, cubeMesh);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[VFXExpansionTool] ✓ NeonGrey material + Grey/White explosion prefabs created.");
        EditorUtility.DisplayDialog(
            "Missing VFX Generated",
            "✓ Assets/Materials/Neon/NeonGrey.mat\n" +
            "✓ Assets/Prefabs/Particles/Explosion_Grey.prefab\n" +
            "✓ Assets/Prefabs/Particles/Explosion_White.prefab\n\n" +
            "Assign Explosion_White to the PlayerStats 'Explosion Prefab' field.",
            "Got it!");
    }

    // =========================================================================
    // Material Creator
    // =========================================================================

    private static Material CreateNeonMaterial(string name, string assetPath,
                                                Color baseColor, Color emissionColor)
    {
        Shader shader = Shader.Find(URP_UNLIT_SHADER);
        if (shader == null)
        {
            Debug.LogError("[VFXExpansionTool] URP Unlit shader not found. Is URP installed?");
            return null;
        }

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

        mat.SetColor("_BaseColor", baseColor);
        mat.EnableKeyword("_EMISSION");
        mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        mat.SetColor("_EmissionColor", emissionColor);

        EditorUtility.SetDirty(mat);
        Debug.Log($"[VFXExpansionTool]   Material saved: {assetPath}");
        return mat;
    }

    // =========================================================================
    // Prefab Builder  — identical parameters to ParticleSetupTool
    // =========================================================================

    private static void CreateExplosionPrefab(string prefabName, Material material, Mesh cubeMesh)
    {
        if (material == null)
        {
            Debug.LogWarning($"[VFXExpansionTool] Skipping {prefabName} — material is null.");
            return;
        }

        string prefabPath = $"{PARTICLES_FOLDER}/{prefabName}.prefab";

        GameObject go = new GameObject(prefabName);
        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ps.Stop();

        // ---- Main Module ----------------------------------------
        var main             = ps.main;
        main.duration        = 1.0f;
        main.loop            = false;
        main.playOnAwake     = true;
        main.stopAction      = ParticleSystemStopAction.Destroy;
        main.startLifetime   = new ParticleSystem.MinMaxCurve(0.4f, 0.8f);
        main.startSpeed      = new ParticleSystem.MinMaxCurve(15f, 25f);
        main.startSize       = new ParticleSystem.MinMaxCurve(0.5f, 1.5f);
        main.startColor      = material.GetColor("_BaseColor");
        main.gravityModifier = new ParticleSystem.MinMaxCurve(0.3f);

        // ---- Emission Module ------------------------------------
        var emission          = ps.emission;
        emission.enabled      = true;
        emission.rateOverTime = 0;

        var burst             = new ParticleSystem.Burst(0f, 15, 25);
        burst.cycleCount      = 1;
        burst.repeatInterval  = 0.01f;
        emission.SetBursts(new ParticleSystem.Burst[] { burst });

        // ---- Shape Module ---------------------------------------
        var shape        = ps.shape;
        shape.enabled    = true;
        shape.shapeType  = ParticleSystemShapeType.Sphere;
        shape.radius     = 0.5f;

        // ---- Size Over Lifetime (1 → 0) -------------------------
        var sol     = ps.sizeOverLifetime;
        sol.enabled = true;
        AnimationCurve shrinkCurve = new AnimationCurve(
            new Keyframe(0f, 1f),
            new Keyframe(1f, 0f));
        shrinkCurve.SmoothTangents(0, 0f);
        shrinkCurve.SmoothTangents(1, 0f);
        sol.size = new ParticleSystem.MinMaxCurve(1f, shrinkCurve);

        // ---- Rotation Over Lifetime (tumbling cubes) ------------
        var rot          = ps.rotationOverLifetime;
        rot.enabled      = true;
        rot.separateAxes = true;
        rot.x = new ParticleSystem.MinMaxCurve(-180f * Mathf.Deg2Rad, 180f * Mathf.Deg2Rad);
        rot.y = new ParticleSystem.MinMaxCurve(-180f * Mathf.Deg2Rad, 180f * Mathf.Deg2Rad);
        rot.z = new ParticleSystem.MinMaxCurve(-180f * Mathf.Deg2Rad, 180f * Mathf.Deg2Rad);

        // ---- Renderer Module ------------------------------------
        var rend        = go.GetComponent<ParticleSystemRenderer>();
        rend.renderMode = ParticleSystemRenderMode.Mesh;
        rend.mesh       = cubeMesh;
        rend.material   = material;

        PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
        Object.DestroyImmediate(go);

        Debug.Log($"[VFXExpansionTool]   Prefab saved: {prefabPath}");
    }

    // =========================================================================
    // Helpers
    // =========================================================================

    private static Mesh GetBuiltInCubeMesh()
    {
        GameObject temp = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Mesh mesh = temp.GetComponent<MeshFilter>().sharedMesh;
        Object.DestroyImmediate(temp);
        return mesh;
    }

    private static void EnsureFolderExists(string fullPath, string folderName)
    {
        if (!AssetDatabase.IsValidFolder(fullPath))
        {
            string parent = fullPath.Substring(0, fullPath.Length - folderName.Length - 1);
            AssetDatabase.CreateFolder(parent, folderName);
        }
    }
}
