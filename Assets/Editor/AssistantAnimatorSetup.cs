// AssistantAnimatorSetup.cs  (Editor Only)
// Creates the Animator Controller for the female assistant NPC.
// Run via: VR Research Demo > NPC > Create Assistant Animator Controller

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

public static class AssistantAnimatorSetup
{
    private const string MenuRoot = "VR Research Demo/NPC/";

    [MenuItem(MenuRoot + "Create Assistant Animator Controller")]
    static void CreateAnimatorController()
    {
        string folder = "Assets/Characters/FemaleAssistant";
        if (!AssetDatabase.IsValidFolder(folder))
            AssetDatabase.CreateFolder("Assets/Characters", "FemaleAssistant");

        string path = $"{folder}/AssistantAnimator.controller";

        // Avoid overwriting an existing controller
        if (AssetDatabase.LoadAssetAtPath<AnimatorController>(path) != null)
        {
            Debug.LogWarning($"[AssistantAnimator] Controller already exists at {path}.");
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            return;
        }

        // Create controller
        var controller = AnimatorController.CreateAnimatorControllerAtPath(path);

        // Add parameters
        controller.AddParameter("Wave",  AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Point", AnimatorControllerParameterType.Trigger);

        // Root state machine
        var rootSM = controller.layers[0].stateMachine;

        // ── States ──────────────────────────────────────────────────────
        // Idle (default, will be overridden when Mixamo clips are imported)
        AnimatorState idle = rootSM.AddState("Idle");
        idle.motion = null;   // assign Idle.anim in Inspector after Mixamo import
        idle.speed  = 1f;
        rootSM.defaultState = idle;

        // Wave
        AnimatorState wave = rootSM.AddState("Wave");
        wave.motion = null;   // assign Wave.anim in Inspector
        wave.speed  = 1f;

        // Point
        AnimatorState point = rootSM.AddState("Point");
        point.motion = null;  // assign Point.anim in Inspector
        point.speed  = 1f;

        // ── Transitions ─────────────────────────────────────────────────
        // Idle → Wave
        var idleToWave = idle.AddTransition(wave);
        idleToWave.AddCondition(AnimatorConditionMode.If, 0, "Wave");
        idleToWave.hasExitTime = false;
        idleToWave.duration    = 0.15f;

        // Wave → Idle (exit time = end of clip)
        var waveToIdle = wave.AddTransition(idle);
        waveToIdle.hasExitTime = true;
        waveToIdle.exitTime    = 1f;  // 100% through the clip
        waveToIdle.duration    = 0.25f;

        // Idle → Point
        var idleToPoint = idle.AddTransition(point);
        idleToPoint.AddCondition(AnimatorConditionMode.If, 0, "Point");
        idleToPoint.hasExitTime = false;
        idleToPoint.duration    = 0.15f;

        // Point → Idle
        var pointToIdle = point.AddTransition(idle);
        pointToIdle.hasExitTime = true;
        pointToIdle.exitTime    = 1f;
        pointToIdle.duration    = 0.25f;

        AssetDatabase.SaveAssets();
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = controller;

        EditorUtility.DisplayDialog(
            "Animator Controller Created",
            "AssistantAnimator.controller created at:\n" +
            "Assets/Characters/FemaleAssistant/\n\n" +
            "NEXT: Assign your Mixamo animation clips to the states:\n" +
            "  • Idle state  → Idle.anim\n" +
            "  • Wave state  → Waving.anim\n" +
            "  • Point state → PointingForward.anim\n\n" +
            "Then drag this controller onto the NPC's Animator component.",
            "Done"
        );

        Debug.Log($"[AssistantAnimator] Controller created at {path}.");
    }

    [MenuItem(MenuRoot + "Show Mixamo Download Guide")]
    static void ShowMixamoGuide()
    {
        EditorUtility.DisplayDialog(
            "Mixamo Character Download Guide",

            "How to get a free female character + animations:\n\n" +

            "1. Go to https://www.mixamo.com (free Adobe account needed)\n\n" +

            "2. In CHARACTERS tab, search for a female character.\n" +
            "   Recommended: 'Sophie', 'Kachujin', 'Arissa', 'Lola'\n\n" +

            "3. Click 'USE THIS CHARACTER'\n\n" +

            "4. In ANIMATIONS tab, search & download each:\n" +
            "   a. Search 'Idle' → Select 'Idle' → Download\n" +
            "      Format: FBX for Unity, With Skin, 30fps\n" +
            "   b. Search 'Wave' → Select 'Waving' → Download\n" +
            "      Format: FBX for Unity, WITHOUT Skin, 30fps\n" +
            "   c. Search 'Point' → 'Pointing Forward' → Download\n" +
            "      Format: FBX for Unity, WITHOUT Skin, 30fps\n\n" +

            "5. Import all FBX files into:\n" +
            "   Assets/Characters/FemaleAssistant/\n\n" +

            "6. Set Rig type to 'Humanoid' on each import.\n\n" +

            "7. Run 'Create Assistant Animator Controller' menu item.\n\n" +

            "8. Assign clips to Animator states in the controller.\n\n" +

            "9. Create a Prefab with the character + Animator + NPC_ProximityTrigger.",

            "Got it!"
        );
    }
}
#endif
