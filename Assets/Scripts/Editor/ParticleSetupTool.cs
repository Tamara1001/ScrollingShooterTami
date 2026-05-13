// ParticleSetupTool.cs
// Place this file inside:  Assets/Scripts/Editor/
//
// Unity 6, Universal Render Pipeline (URP)
// Adds a menu item: Tools > Setup Neon Particles
//
// What it does automatically:
//   1. Creates Assets/Prefabs/Particles/ folder.
//   2. Generates two Particle System Prefabs (Explosion_Cyan, Explosion_Magenta)
//      with low-poly cube mesh renderers, HDR neon materials, and StopAction = Destroy.

using UnityEditor;
using UnityEngine;

public static class ParticleSetupTool
{
    // -------------------------------------------------------------------------
    // Paths
    // -------------------------------------------------------------------------

    private const string PREFABS_FOLDER    = "Assets/Prefabs";
    private const string PARTICLES_FOLDER  = "Assets/Prefabs/Particles";
    private const string CYAN_MAT_PATH     = "Assets/Materials/Neon/NeonCyan.mat";
    private const string MAGENTA_MAT_PATH  = "Assets/Materials/Neon/NeonMagenta.mat";

    // -------------------------------------------------------------------------
    // Menu Entry
    // -------------------------------------------------------------------------

    [MenuItem("Tools/Setup Neon Particles")]
    public static void SetupNeonParticles()
    {
        // Pre-flight: verify that the neon materials exist
        Material cyanMat    = AssetDatabase.LoadAssetAtPath<Material>(CYAN_MAT_PATH);
        Material magentaMat = AssetDatabase.LoadAssetAtPath<Material>(MAGENTA_MAT_PATH);

        if (cyanMat == null || magentaMat == null)
        {
            EditorUtility.DisplayDialog(
                "Missing Neon Materials",
                "Could not find the Neon materials.\n\n" +
                "Please run  Tools > Setup Neon Visuals  first to generate them, " +
                "then try again.",
                "OK");
            return;
        }

        // Ensure folders exist
        EnsureFolderExists(PREFABS_FOLDER,   "Prefabs");
        EnsureFolderExists(PARTICLES_FOLDER, "Particles");

        // Retrieve Unity's built-in Cube mesh
        Mesh cubeMesh = GetBuiltInCubeMesh();

        // Create both prefabs
        CreateExplosionPrefab("Explosion_Cyan",    cyanMat,    cubeMesh);
        CreateExplosionPrefab("Explosion_Magenta", magentaMat, cubeMesh);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[ParticleSetupTool] ✓ Neon particle prefabs created in Assets/Prefabs/Particles/");
        EditorUtility.DisplayDialog(
            "Neon Particles Setup Complete",
            "✓ Explosion_Cyan.prefab\n" +
            "✓ Explosion_Magenta.prefab\n\n" +
            "Both saved to  Assets/Prefabs/Particles/\n\n" +
            "Assign them to the 'Explosion Prefab' field on your Enemy and Asteroid components.",
            "Got it!");
    }

    // =========================================================================
    // Prefab Builder
    // =========================================================================

    private static void CreateExplosionPrefab(string prefabName, Material material, Mesh cubeMesh)
    {
        string prefabPath = $"{PARTICLES_FOLDER}/{prefabName}.prefab";

        // ---- Create a temporary scene object to configure ----
        GameObject go = new GameObject(prefabName);
        ParticleSystem ps = go.AddComponent<ParticleSystem>();

        // Stop the preview so we don't accidentally start the system mid-setup
        ps.Stop();

        // ---- Main Module ----------------------------------------
        var main = ps.main;
        main.duration        = 1.0f;
        main.loop            = false;
        main.playOnAwake     = true;
        main.stopAction      = ParticleSystemStopAction.Destroy;  // self-cleans from scene

        // Start Lifetime: random between 0.4 and 0.8
        main.startLifetime   = new ParticleSystem.MinMaxCurve(0.4f, 0.8f);

        // Start Speed: random between 15 and 25
        main.startSpeed      = new ParticleSystem.MinMaxCurve(15f, 25f);

        // Start Size: random between 0.5 and 1.5
        main.startSize       = new ParticleSystem.MinMaxCurve(0.5f, 1.5f);

        // Start Color: use the material's base color so it matches
        main.startColor      = material.GetColor("_BaseColor");

        // Gravity: slight negative pull gives a nice "scatter & fall" feel
        main.gravityModifier = new ParticleSystem.MinMaxCurve(0.3f);

        // ---- Emission Module ------------------------------------
        var emission = ps.emission;
        emission.enabled         = true;
        emission.rateOverTime    = 0;           // no continuous stream

        // Single burst of 15–25 particles at time 0.
        // Unity 6: use the (time, count) constructor then set min/max directly on the struct.
        var burst = new ParticleSystem.Burst(0f, 15, 25);
        burst.cycleCount     = 1;
        burst.repeatInterval = 0.01f;
        emission.SetBursts(new ParticleSystem.Burst[] { burst });

        // ---- Shape Module ---------------------------------------
        var shape = ps.shape;
        shape.enabled       = true;
        shape.shapeType     = ParticleSystemShapeType.Sphere;
        shape.radius        = 0.5f;

        // ---- Size Over Lifetime (1 → 0 fade-out) ---------------
        var sol = ps.sizeOverLifetime;
        sol.enabled = true;

        // Create an AnimationCurve that goes from 1 at t=0 down to 0 at t=1
        AnimationCurve shrinkCurve = new AnimationCurve(
            new Keyframe(0f, 1f),
            new Keyframe(1f, 0f));
        shrinkCurve.SmoothTangents(0, 0f);
        shrinkCurve.SmoothTangents(1, 0f);

        sol.size = new ParticleSystem.MinMaxCurve(1f, shrinkCurve);

        // ---- Renderer Module (Cube Mesh) ------------------------
        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode   = ParticleSystemRenderMode.Mesh;
        renderer.mesh         = cubeMesh;
        renderer.material     = material;

        // Random rotation on spawn + rotation speed makes cubes tumble nicely
        var rot = ps.rotationOverLifetime;
        rot.enabled = true;
        rot.x = new ParticleSystem.MinMaxCurve(-180f * Mathf.Deg2Rad, 180f * Mathf.Deg2Rad);
        rot.y = new ParticleSystem.MinMaxCurve(-180f * Mathf.Deg2Rad, 180f * Mathf.Deg2Rad);
        rot.z = new ParticleSystem.MinMaxCurve(-180f * Mathf.Deg2Rad, 180f * Mathf.Deg2Rad);
        rot.separateAxes = true;

        // ---- Save as Prefab then destroy the temp scene object --
        PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
        Object.DestroyImmediate(go);

        Debug.Log($"[ParticleSetupTool]   Prefab saved: {prefabPath}");
    }

    // =========================================================================
    // Helpers
    // =========================================================================

    /// <summary>
    /// Loads Unity's built-in Cube primitive mesh from the default resources.
    /// </summary>
    private static Mesh GetBuiltInCubeMesh()
    {
        // The cleanest way to get the built-in cube mesh in any Unity version:
        // create a temporary primitive, grab its mesh, then destroy it.
        GameObject tempCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Mesh mesh = tempCube.GetComponent<MeshFilter>().sharedMesh;
        Object.DestroyImmediate(tempCube);
        return mesh;
    }

    private static void EnsureFolderExists(string fullPath, string folderName)
    {
        if (!AssetDatabase.IsValidFolder(fullPath))
        {
            string parent = fullPath.Substring(0, fullPath.Length - folderName.Length - 1);
            AssetDatabase.CreateFolder(parent, folderName);
            Debug.Log($"[ParticleSetupTool]   Created folder: {fullPath}");
        }
    }
}
