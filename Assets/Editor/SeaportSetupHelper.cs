// SeaportSetupHelper.cs  (Editor Only)
// Unity menu tools for setting up the seaport research demo.
// Access via: VR Research Demo > ... in the Unity menu bar.
//
// USAGE FLOW:
//   1. Open an existing Level scene (e.g., Level1.unity).
//   2. Run  "VR Research Demo > 1. Add LayoutManager to Scene"
//   3. Run  "VR Research Demo > 2. Create Layout A Config"
//   4. Duplicate the scene, rename to Level2 (or SeaportLayout_B).
//   5. Open Level2, run  "VR Research Demo > 3. Apply Layout B Positions"
//   6. Run  "VR Research Demo > 4. Add NPC Placeholder to Scene" in both scenes.
//   7. Run  "VR Research Demo > 5. Add ResearchHUD Canvas to Scene" in both scenes.
//   8. Wire references in the Inspector as prompted.

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using Unity.XR.CoreUtils;
using TMPro;

public static class SeaportSetupHelper
{
    private const string MenuRoot = "VR Research Demo/";

    // ──────────────────────────────────────────────────────────────────────
    // 1. ADD LAYOUT MANAGER
    // ──────────────────────────────────────────────────────────────────────

    [MenuItem(MenuRoot + "1. Add LayoutManager to Scene")]
    static void AddLayoutManager()
    {
        if (FindOrWarn<LayoutManager>("LayoutManager")) return;

        GameObject go = new GameObject("LayoutManager");
        go.AddComponent<LayoutManager>();
        Undo.RegisterCreatedObjectUndo(go, "Add LayoutManager");
        Selection.activeGameObject = go;

        Debug.Log("[SeaportSetup] LayoutManager added. " +
                  "Drag your TargetArea and Load transforms into it in the Inspector, " +
                  "then assign the LayoutConfig ScriptableObjects.");
    }

    // ──────────────────────────────────────────────────────────────────────
    // 2. CREATE LAYOUT CONFIGS
    // ──────────────────────────────────────────────────────────────────────

    [MenuItem(MenuRoot + "2a. Create Layout A Config Asset")]
    static void CreateLayoutA()
    {
        CreateLayoutConfigAsset(
            fileName:    "LayoutA_WideBay",
            layoutName:  "Layout A — Wide Bay",
            description: "Containers spread across the wide dock bay. Low spatial pressure, low mental load. " +
                         "Baseline condition for time-study and fatigue measurement.",
            difficulty:  2,
            // Example positions — adjust these in the Inspector to match your scene geometry
            targetPositions: new Vector3[]
            {
                new Vector3(-12f, 0f, 10f),
                new Vector3(  0f, 0f, 10f),
                new Vector3( 12f, 0f, 10f)
            },
            loadSpawnPositions: new Vector3[]
            {
                new Vector3(-12f, 2f, -8f),
                new Vector3(  0f, 2f, -8f),
                new Vector3( 12f, 2f, -8f)
            }
        );
    }

    [MenuItem(MenuRoot + "2b. Create Layout B Config Asset")]
    static void CreateLayoutB()
    {
        CreateLayoutConfigAsset(
            fileName:    "LayoutB_NarrowChannel",
            layoutName:  "Layout B — Narrow Channel",
            description: "Containers clustered along a narrow quay channel. Higher spatial demands, " +
                         "increased working memory load. Compare operator fatigue with Layout A.",
            difficulty:  4,
            targetPositions: new Vector3[]
            {
                new Vector3(-4f, 0f, 14f),
                new Vector3( 0f, 0f, 14f),
                new Vector3( 4f, 0f, 14f)
            },
            loadSpawnPositions: new Vector3[]
            {
                new Vector3(-4f, 2f, -4f),
                new Vector3( 0f, 2f, -4f),
                new Vector3( 4f, 2f, -4f)
            }
        );
    }

    static void CreateLayoutConfigAsset(
        string fileName, string layoutName, string description,
        int difficulty, Vector3[] targetPositions, Vector3[] loadSpawnPositions)
    {
        string folder = "Assets/Research/Layouts";
        EnsureFolder(folder);

        string fullPath = $"{folder}/{fileName}.asset";
        if (AssetDatabase.LoadAssetAtPath<LayoutConfig>(fullPath) != null)
        {
            Debug.LogWarning($"[SeaportSetup] Config already exists at {fullPath}. Delete it first to recreate.");
            return;
        }

        var config = ScriptableObject.CreateInstance<LayoutConfig>();
        config.layoutName        = layoutName;
        config.description       = description;
        config.difficultyRating  = difficulty;
        config.targetPositions   = targetPositions;
        config.loadSpawnPositions = loadSpawnPositions;

        AssetDatabase.CreateAsset(config, fullPath);
        AssetDatabase.SaveAssets();
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = config;

        Debug.Log($"[SeaportSetup] Created '{layoutName}' config at {fullPath}. " +
                  "Update the positions in the Inspector to match your scene.");
    }

    // ──────────────────────────────────────────────────────────────────────
    // 3. ADD NPC PLACEHOLDER
    // ──────────────────────────────────────────────────────────────────────

    [MenuItem(MenuRoot + "3. Add NPC Placeholder to Scene")]
    static void AddNPCPlaceholder()
    {
        // Create a capsule as a stand-in until the Mixamo FBX is imported
        GameObject npc = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        npc.name = "FemaleAssistant_PLACEHOLDER";
        npc.transform.localScale = new Vector3(0.45f, 0.85f, 0.45f); // roughly female proportions

        // Give it a green material to stand out
        var renderer = npc.GetComponent<Renderer>();
        if (renderer != null)
        {
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = new Color(0.2f, 0.8f, 0.4f);
            renderer.sharedMaterial = mat;
        }

        // Add the trigger script
        var trigger = npc.AddComponent<NPC_ProximityTrigger>();
        trigger.enableIdleBob = true;

        // Try to position near the XR Origin (player start)
        var xrOrigin = Object.FindAnyObjectByType<XROrigin>();
        if (xrOrigin != null)
        {
            Vector3 playerPos = xrOrigin.transform.position;
            // Place 1.5 m to the left of and 0.85 m (half capsule height offset) above ground
            npc.transform.position = playerPos + xrOrigin.transform.right * -1.5f;
        }

        Undo.RegisterCreatedObjectUndo(npc, "Add NPC Placeholder");
        Selection.activeGameObject = npc;

        EditorUtility.DisplayDialog(
            "NPC Placeholder Added",
            "A capsule placeholder has been added.\n\n" +
            "NEXT STEPS:\n" +
            "1. Download a female character + Idle + Wave animations from Mixamo.com (free)\n" +
            "2. Import the FBX into Assets/Characters/FemaleAssistant/\n" +
            "3. Create an Animator Controller with states: Idle (default, loop) → Wave (Trigger)\n" +
            "4. Replace this capsule with the character prefab\n" +
            "5. Assign the Animator to NPC_ProximityTrigger.animator",
            "OK");
    }

    // ──────────────────────────────────────────────────────────────────────
    // 4. ADD RESEARCH HUD CANVAS
    // ──────────────────────────────────────────────────────────────────────

    [MenuItem(MenuRoot + "4. Add ResearchHUD Canvas to Scene")]
    static void AddResearchHUDCanvas()
    {
        if (FindOrWarn<ResearchHUD>("ResearchHUD")) return;

        // Create World Space Canvas
        GameObject canvasGO = new GameObject("ResearchHUD_Canvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;

        var canvasScaler = canvasGO.AddComponent<UnityEngine.UI.CanvasScaler>();
        canvasScaler.dynamicPixelsPerUnit = 100f;
        canvasGO.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        // Size of the panel
        var rt = canvasGO.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0.5f, 0.35f);  // 50cm × 35cm in world-space
        canvasGO.transform.localScale = Vector3.one; // Scale is controlled by sizeDelta

        // Try to position in front of the player near the crane controls
        var xrOrigin = Object.FindAnyObjectByType<XROrigin>();
        if (xrOrigin != null)
        {
            Vector3 pos = xrOrigin.transform.position + xrOrigin.transform.forward * 0.8f + Vector3.up * 1.5f;
            canvasGO.transform.position = pos;
            canvasGO.transform.rotation = Quaternion.LookRotation(canvasGO.transform.position - xrOrigin.Camera.transform.position);
        }

        // Add background panel
        GameObject bg = new GameObject("Background");
        bg.transform.SetParent(canvasGO.transform, false);
        var img = bg.AddComponent<UnityEngine.UI.Image>();
        img.color = new Color(0.05f, 0.08f, 0.15f, 0.88f);
        var bgRt = bg.GetComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = bgRt.offsetMax = Vector2.zero;

        // Add TMP text rows
        var hud = canvasGO.AddComponent<ResearchHUD>();
        hud.layoutNameText = AddHUDText(canvasGO, "LayoutName", "Layout A", new Vector2(0f, 0.72f), 0.07f, Color.cyan);
        hud.timerText      = AddHUDText(canvasGO, "Timer",      "00:00",    new Vector2(0f, 0.48f), 0.12f, Color.white);
        hud.placedText     = AddHUDText(canvasGO, "Placed",     "0 / 0",    new Vector2(0f, 0.24f), 0.07f, Color.white);
        hud.penaltyText    = AddHUDText(canvasGO, "Penalties",  "Penalties: 0", new Vector2(0f, 0.04f), 0.065f, Color.white);

        Undo.RegisterCreatedObjectUndo(canvasGO, "Add ResearchHUD Canvas");
        Selection.activeGameObject = canvasGO;

        Debug.Log("[SeaportSetup] ResearchHUD Canvas added. " +
                  "Move it in the scene to the crane cabin wall or operator panel position.");
    }

    static TextMeshProUGUI AddHUDText(GameObject parent, string name, string defaultText,
                                       Vector2 anchorCenter, float heightFraction, Color color)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);

        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = defaultText;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontSize = 0.045f;  // world-space units
        tmp.enableAutoSizing = true;
        tmp.fontSizeMin = 0.01f;
        tmp.fontSizeMax = 0.06f;

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.05f, anchorCenter.y);
        rt.anchorMax = new Vector2(0.95f, anchorCenter.y + heightFraction);
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        return tmp;
    }

    // ──────────────────────────────────────────────────────────────────────
    // 5. WATER PLANE SETUP
    // ──────────────────────────────────────────────────────────────────────

    [MenuItem(MenuRoot + "5. Add Seaport Water Plane")]
    static void AddWaterPlane()
    {
        GameObject waterGO = GameObject.CreatePrimitive(PrimitiveType.Plane);
        waterGO.name = "SeaportWater";
        waterGO.transform.localScale = new Vector3(20f, 1f, 20f); // 200m × 200m plane

        // Try to position below the dock level
        waterGO.transform.position = new Vector3(0f, -1f, 0f);

        // Check for Simple Water Shader material
        string waterMatPath = "Assets/IgniteCoders/Simple Water Shader/Resources";
        string[] waterMats = AssetDatabase.FindAssets("t:Material", new[] { waterMatPath });
        if (waterMats.Length > 0)
        {
            string matPath = AssetDatabase.GUIDToAssetPath(waterMats[0]);
            var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat != null)
                waterGO.GetComponent<Renderer>().sharedMaterial = mat;
            Debug.Log($"[SeaportSetup] Applied water material from: {matPath}");
        }
        else
        {
            Debug.Log("[SeaportSetup] Water material not found at expected path. " +
                      "Manually assign a material from IgniteCoders/Simple Water Shader.");
        }

        Undo.RegisterCreatedObjectUndo(waterGO, "Add Seaport Water Plane");
        Selection.activeGameObject = waterGO;
    }

    // ──────────────────────────────────────────────────────────────────────
    // 6. MENU SCENE LAYOUT PANEL GUIDE
    // ──────────────────────────────────────────────────────────────────────

    [MenuItem(MenuRoot + "6. Show Menu Scene Setup Guide")]
    static void ShowMenuGuide()
    {
        EditorUtility.DisplayDialog(
            "Menu Scene — Layout Selector Setup",

            "To add layout selection buttons to your Menu scene:\n\n" +

            "STEP 1 — Add a World Space Canvas (or Screen Space) to the Menu scene.\n\n" +

            "STEP 2 — Create 2 Buttons on it:\n" +
            "  • 'Layout A — Wide Bay'\n" +
            "  • 'Layout B — Narrow Channel'\n\n" +

            "STEP 3 — On each Button GameObject:\n" +
            "  • Add the MenuLayoutButton component\n" +
            "  • Drag the matching LayoutConfig ScriptableObject into the 'Layout Config' field\n" +
            "    (These are in Assets/Research/Layouts/)\n" +
            "  • Set Target Scene Name = the name of your seaport scene (e.g. 'Level1')\n\n" +

            "STEP 4 — Make sure your seaport scene has a LayoutManager with\n" +
            "the same LayoutConfig assigned as 'Default Layout'.\n\n" +

            "The LayoutManager reads LayoutSelector.Selected at Awake() and\n" +
            "repositions TargetAreas + Load containers automatically.",

            "Got it!"
        );
    }

    // ──────────────────────────────────────────────────────────────────────
    // 7. VALIDATE SCENE SETUP
    // ──────────────────────────────────────────────────────────────────────

    [MenuItem(MenuRoot + "7. Validate Current Scene Setup")]
    static void ValidateScene()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("=== VR Research Demo Scene Validation ===\n");

        Check<LevelManager>(sb,   "LevelManager");
        Check<LayoutManager>(sb,  "LayoutManager");
        Check<NPC_ProximityTrigger>(sb, "NPC_ProximityTrigger (FemaleAssistant)");
        Check<ResearchHUD>(sb,    "ResearchHUD Canvas");
        Check<XROrigin>(sb,       "XR Origin (Player)");

        // Check water plane
        bool waterFound = GameObject.Find("SeaportWater") != null;
        sb.AppendLine($"  [{(waterFound ? "OK" : "MISSING")}] Seaport Water Plane");

        Debug.Log(sb.ToString());
        EditorUtility.DisplayDialog("Scene Validation", sb.ToString(), "Close");
    }

    // ──────────────────────────────────────────────────────────────────────
    // HELPERS
    // ──────────────────────────────────────────────────────────────────────

    static bool FindOrWarn<T>(string label) where T : Object
    {
        var existing = Object.FindAnyObjectByType<T>();
        if (existing != null)
        {
            Debug.LogWarning($"[SeaportSetup] {label} already exists in scene.");
            return true;
        }
        return false;
    }

    static void Check<T>(System.Text.StringBuilder sb, string label) where T : Object
    {
        bool found = Object.FindAnyObjectByType<T>() != null;
        sb.AppendLine($"  [{(found ? "OK" : "MISSING")}] {label}");
    }

    static void EnsureFolder(string path)
    {
        string[] parts = path.Split('/');
        string current = parts[0]; // "Assets"
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }
}
#endif
