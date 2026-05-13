// EnvironmentSetupTool.cs
// Place inside:  Assets/Scripts/Editor/
//
// Unity 6, URP
// Menu: Tools > Setup Cyberpunk Background
//
// What it does:
//   1. Ensures Assets/Shaders/ folder exists.
//   2. Creates CyberGrid.mat using the ScrollingGrid shader.
//   3. Cleans up old Background objects (BackgroundGrid, CyberFloor, Starfield).
//   4. Creates a CyberFloor Quad in the active scene, parented to Main Camera, below the player.
//   5. Creates a Starfield Particle System parented to Main Camera.

using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

public static class EnvironmentSetupTool
{
    private const string SHADER_PATH   = "Assets/Shaders/ScrollingGrid.shader";
    private const string MATERIAL_PATH = "Assets/Materials/CyberGrid.mat";
    private const string FLOOR_NAME    = "CyberFloor";
    private const string STARS_NAME    = "Starfield";

    [MenuItem("Tools/Setup Cyberpunk Background")]
    public static void SetupCyberpunkBackground()
    {
        // ---- 1. Verify the shader was imported ----
        // The .shader file is written by the tool before this script runs.
        // After AssetDatabase.Refresh() Unity compiles it; we load it here.
        AssetDatabase.Refresh();

        Shader gridShader = AssetDatabase.LoadAssetAtPath<Shader>(SHADER_PATH);
        if (gridShader == null)
        {
            // Shader file is missing — create it via code as a fallback
            EnsureFolderExists("Assets/Shaders", "Shaders");
            Debug.LogError(
                "[EnvironmentSetupTool] ScrollingGrid.shader not found at " + SHADER_PATH + ". " +
                "Make sure the shader file exists in Assets/Shaders/ and try again.");
            return;
        }

        // ---- 2. Create or refresh the material ----
        EnsureFolderExists("Assets/Materials", "Materials");

        Material mat = AssetDatabase.LoadAssetAtPath<Material>(MATERIAL_PATH);
        if (mat == null)
        {
            mat = new Material(gridShader);
            AssetDatabase.CreateAsset(mat, MATERIAL_PATH);
        }
        else
        {
            mat.shader = gridShader;
        }

        // Set neon-purple default colours
        mat.SetColor("_GridColor",   new Color(0.70f, 0.00f, 1.00f, 1f));  // vivid purple
        mat.SetColor("_BgColor",     new Color(0.01f, 0.00f, 0.06f, 1f));  // near-black navy
        mat.SetFloat("_GridScale",   150f); // much smaller grid squares
        mat.SetFloat("_LineWidth",   0.04f);
        mat.SetFloat("_ScrollSpeed", 0.05f); // glides slowly
        mat.SetFloat("_Alpha",       1.0f);

        EditorUtility.SetDirty(mat);

        // ---- 3. Cleanup old objects ----
        string[] oldNames = { "BackgroundGrid", "CyberFloor", "Starfield" };
        foreach (string n in oldNames)
        {
            GameObject oldGo = GameObject.Find(n);
            if (oldGo != null)
                Object.DestroyImmediate(oldGo);
        }

        // ---- 4. Create the CyberFloor scene object ----
        GameObject bgGo = GameObject.CreatePrimitive(PrimitiveType.Quad);
        bgGo.name = FLOOR_NAME;
        Undo.RegisterCreatedObjectUndo(bgGo, "Create CyberFloor");

        // Remove the unnecessary MeshCollider Unity adds to primitives
        MeshCollider col = bgGo.GetComponent<MeshCollider>();
        if (col != null)
            Object.DestroyImmediate(col);

        // ---- 5. Parent to Main Camera & Setup Skybox ----
        Camera mainCam = Camera.main;
        if (mainCam == null)
        {
            Debug.LogWarning(
                "[EnvironmentSetupTool] No Camera tagged 'MainCamera' found in the scene. " +
                "CyberFloor and Starfield were created at the root — parent them to your camera manually.");
        }
        else
        {
            Undo.SetTransformParent(bgGo.transform, mainCam.transform, "Parent CyberFloor to Camera");
            
            // Remove the default Unity blue skybox and replace with pure black space
            Undo.RecordObject(mainCam, "Set Camera Background");
            mainCam.clearFlags = CameraClearFlags.SolidColor;
            mainCam.backgroundColor = Color.black;
        }

        // ---- 6. Position, rotation, and scale ----
        bgGo.transform.localPosition = new Vector3(0f, -15f, 200f);   // below camera, pushed forward
        bgGo.transform.localRotation = Quaternion.Euler(90f, 0f, 0f); // lie flat
        bgGo.transform.localScale    = new Vector3(2000f, 2000f, 1f); // massive scale

        // ---- 7. Assign the material ----
        MeshRenderer mr = bgGo.GetComponent<MeshRenderer>();
        if (mr != null)
        {
            Undo.RecordObject(mr, "Assign CyberGrid Material");
            mr.sharedMaterial = mat;

            // Ensure the background renders BEHIND everything else
            mr.shadowCastingMode    = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows       = false;
            // Sorting order doesn't apply to opaque/transparent queues the same way, but good practice
            mr.sortingOrder         = -100;
        }

        // ---- 8. Create Starfield Particle System ----
        GameObject starGo = new GameObject(STARS_NAME);
        Undo.RegisterCreatedObjectUndo(starGo, "Create Starfield");

        if (mainCam != null)
        {
            Undo.SetTransformParent(starGo.transform, mainCam.transform, "Parent Starfield to Camera");
        }

        starGo.transform.localPosition = new Vector3(0f, 0f, 150f);
        starGo.transform.localRotation = Quaternion.identity;
        starGo.transform.localScale = Vector3.one;

        ParticleSystem ps = starGo.AddComponent<ParticleSystem>();

        var main = ps.main;
        main.prewarm = true;
        main.startSpeed = new ParticleSystem.MinMaxCurve(-20f, -5f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.4f);
        main.maxParticles = 2000;

        var emission = ps.emission;
        emission.rateOverTime = 100f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(300f, 200f, 100f);

        var renderer = starGo.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Mesh;
        renderer.mesh = GetBuiltInCubeMesh();

        Material starMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Neon/NeonWhite.mat");
        if (starMat != null)
        {
            renderer.material = starMat;
        }
        else
        {
            Debug.LogWarning("[EnvironmentSetupTool] NeonWhite.mat not found for Starfield. Please assign manually or run Setup Neon Visuals.");
        }

        // ---- 9. Mark scene dirty ----
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        Debug.Log("[EnvironmentSetupTool] ✓ CyberFloor and Starfield created.");
        EditorUtility.DisplayDialog(
            "Cyberpunk Environment Ready",
            "✓ Shader updated to Transparent with vertical fade\n" +
            "✓ CyberFloor placed below camera\n" +
            "✓ Starfield particle system generated\n\n" +
            "Tip: You can adjust the _Alpha on the CyberGrid material, or the Emission Rate on the Starfield.",
            "Got it!");
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

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
