#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using TMPro;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

public static class ResearchDemoBuilder
{
    public const string ScenePath = "Assets/Scenes/SeaportResearch.unity";
    private const string Folder = "Assets/Research";
    private static readonly Color Navy = new Color(0.035f, 0.065f, 0.09f);
    private static readonly Color Slate = new Color(0.09f, 0.14f, 0.18f);
    private static readonly Color Gold = new Color(1f, 0.68f, 0.28f);
    private static readonly Color Teal = new Color(0.24f, 0.78f, 0.72f);
    private static Material cargoRed, cargoBlue, cargoYellow, yellow, dark, white;

    public static void RepairAssetsAndBuild()
    {
        AssetDatabase.ImportAsset("Assets/Models/industry/industry.blend", ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
        Build();
    }

    [MenuItem("VR Research Demo/Build Research Station")]
    public static void Build()
    {
        if (!Application.isBatchMode && File.Exists(ScenePath) &&
            !EditorUtility.DisplayDialog("Rebuild generated station?", "This regenerates SeaportResearch from Level1. Manual changes to the research scene will be replaced.", "Rebuild", "Cancel")) return;
        Directory.CreateDirectory(Folder + "/Layouts");
        Directory.CreateDirectory(Folder + "/Materials");
        AssetDatabase.Refresh();
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/Level1.unity");
        // Retain the imported port and working XR rig; original scenes are never saved over.
        var keep = new[] { "CraneModel", "industry", "XR Origin (XR Rig)", "XR Interaction Manager", "EventSystem", "PlayerRestrictor", "Directional Light", "AudioManager" };
        foreach (var root in scene.GetRootGameObjects())
            if (!keep.Contains(root.name)) UnityEngine.Object.DestroyImmediate(root);
        var industry = GameObject.Find("industry");
        if (industry != null)
        {
            if (PrefabUtility.IsPartOfPrefabInstance(industry)) PrefabUtility.UnpackPrefabInstance(industry, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            industry.transform.position += new Vector3(0, -1f, -72f);
            foreach (var collider in industry.GetComponentsInChildren<Collider>()) collider.enabled = false;
        }
        foreach (var canvas in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None)) UnityEngine.Object.DestroyImmediate(canvas.gameObject);
        foreach (var simulator in UnityEngine.Object.FindObjectsByType<UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation.XRDeviceSimulator>(FindObjectsSortMode.None))
            UnityEngine.Object.DestroyImmediate(simulator.gameObject);

        cargoRed = Material("Cargo_Coral", new Color(0.72f, 0.19f, 0.12f));
        cargoBlue = Material("Cargo_Teal", new Color(0.045f, 0.42f, 0.49f));
        cargoYellow = Material("Cargo_Ochre", new Color(0.83f, 0.56f, 0.13f));
        yellow = Material("Safety_Yellow", Gold);
        dark = Material("Structure_Dark", Slate);
        white = Material("Markings_White", new Color(0.84f, 0.9f, 0.88f));
        var deck = Material("Quay_Concrete", new Color(0.25f, 0.3f, 0.31f));
        var water = Material("Harbor_Water", new Color(0.035f, 0.22f, 0.3f));

        var crane = UnityEngine.Object.FindFirstObjectByType<CraneController>();
        if (crane == null) throw new InvalidOperationException("Level1 must contain its CraneController.");
        GameObject craneRoot = crane.transform.root.gameObject;
        if (PrefabUtility.IsPartOfPrefabInstance(craneRoot)) PrefabUtility.UnpackPrefabInstance(craneRoot, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        foreach (var rope in craneRoot.GetComponentsInChildren<Rope>(true)) rope.enabled = false;
        foreach (var body in craneRoot.GetComponentsInChildren<Rigidbody>(true)) body.isKinematic = true;
        foreach (var collider in craneRoot.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
        foreach (var lever in craneRoot.GetComponentsInChildren<LeftLever>(true)) lever.enabled = false;
        foreach (var lever in craneRoot.GetComponentsInChildren<RightLever>(true)) lever.enabled = false;
        foreach (var behaviour in craneRoot.GetComponentsInChildren<MonoBehaviour>(true))
            if (behaviour != crane) behaviour.enabled = false;
        foreach (var camera in craneRoot.GetComponentsInChildren<Camera>(true)) camera.gameObject.SetActive(false);
        foreach (var t in craneRoot.GetComponentsInChildren<Transform>(true))
            if (t.name == "hook" || t.name == "Hook (1)") t.gameObject.SetActive(false);
        if (crane.dangerPanel != null) crane.dangerPanel.SetActive(false);
        crane.dangerPanel = null;
        crane.rotationSpeed = 12f; crane.rotationSmoothness = 4f;
        crane.carSpeed = 0.07f; crane.hookSpeed = 2.8f;
        crane.minimumRopeLength = 2f; crane.maximumRopeLength = 27f; crane.requireResearchRun = true;

        Vector3 pivot = crane.craneRotationPivot.position;
        Vector3 direction = crane.car.position - pivot; direction.y = 0; direction.Normalize();
        const float deckTop = 31.2f;
        Vector3 yardCenter = new Vector3(pivot.x + 10f, deckTop - 0.5f, pivot.z);
        var yard = new GameObject("Research Quay");
        Primitive("Quay deck", PrimitiveType.Cube, yardCenter, new Vector3(76, 1, 76), deck, yard.transform, true);
        Primitive("Harbor", PrimitiveType.Cube, new Vector3(25, 23.4f, 0), new Vector3(280, 0.15f, 250), water, null, false);
        for (int i = -3; i <= 3; i++)
            Primitive("Quay lane stripe", PrimitiveType.Cube, new Vector3(yardCenter.x + i * 8, deckTop + 0.012f, yardCenter.z), new Vector3(0.055f, 0.02f, 74), white, yard.transform, false);
        for (int i = 0; i < 18; i++)
            Primitive("Quay edge marking", PrimitiveType.Cube, new Vector3(yardCenter.x - 36 + i * 4f, deckTop + 0.02f, yardCenter.z - 37), new Vector3(1.8f, 0.025f, 0.65f), i % 2 == 0 ? yellow : dark, yard.transform, false);

        var station = new GameObject("Research Experiment");
        var session = station.AddComponent<ExperimentSession>();
        var layouts = station.AddComponent<LayoutManager>();
        session.crane = crane; session.layoutManager = layouts;
        var hookObject = Primitive("Research spreader", PrimitiveType.Cube, crane.car.position - Vector3.up * 8,
            new Vector3(1.6f, 0.45f, 0.8f), yellow, null, true);
        // Keep a unit-scale physics root so joint anchors and load tolerances are in metres.
        var hookRoot = new GameObject("Spreader physics"); hookRoot.transform.position = hookObject.transform.position;
        hookObject.transform.SetParent(hookRoot.transform, true);
        var hookBody = hookRoot.AddComponent<Rigidbody>(); hookBody.mass = 6;
        hookBody.linearDamping = 0.3f; hookBody.angularDamping = 3f;
        hookBody.constraints = RigidbodyConstraints.FreezeRotation;
        hookBody.interpolation = RigidbodyInterpolation.Interpolate;
        hookBody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        var hook = hookRoot.AddComponent<ResearchHook>(); hook.couplingOffset = new Vector3(0, -0.3f, 0); hook.captureRadius = 1.1f;
        var ropeComponent = hookRoot.AddComponent<Rope>(); ropeComponent.basePoint = crane.car; ropeComponent.hook = hookBody;
        ropeComponent.maxLength = 8; ropeComponent.stiffness = 1600; ropeComponent.damping = 150;
        var ropeRenderer = hookRoot.AddComponent<LineRenderer>(); ropeRenderer.positionCount = 2;
        ropeRenderer.startWidth = ropeRenderer.endWidth = 0.055f; ropeRenderer.sharedMaterial = dark;
        ropeComponent.ropeRenderer = ropeRenderer;
        crane.hook = hookRoot.transform; crane.hookRope = ropeComponent; session.hook = hook;
        var home = new GameObject("HOME return position"); home.transform.position = hookRoot.transform.position; session.homeMarker = home.transform;
        var homeVisual = Primitive("Home beacon", PrimitiveType.Sphere, home.transform.position, Vector3.one * 0.4f, white, home.transform, false);
        Label("HOME", home.transform, new Vector3(0, 0.7f, 0), 1.8f, 0.6f, 0.32f, Color.white, Quaternion.identity);

        var loads = new Load[3]; var targets = new TargetArea[3];
        var sources = new Vector3[3];
        var mats = new[] { cargoRed, cargoBlue, cargoYellow };
        var names = new[] { "Coral", "Teal", "Ochre" };
        for (int i = 0; i < 3; i++)
        {
            sources[i] = Polar(pivot, direction, -30 + 20 * i, 22, deckTop + 0.7f);
            var cargo = new GameObject("Cargo " + (i + 1)); cargo.transform.position = sources[i];
            Primitive("Container body", PrimitiveType.Cube, cargo.transform.position, new Vector3(3.4f, 1.3f, 1.6f), mats[i], cargo.transform, true);
            for (int rib = 0; rib < 13; rib++)
                Primitive("Corrugation", PrimitiveType.Cube, cargo.transform.position + new Vector3(-1.56f + rib * 0.26f, 0, 0), new Vector3(0.055f, 1.2f, 1.64f), mats[i], cargo.transform, false);
            var body = cargo.AddComponent<Rigidbody>(); body.mass = 4; body.linearDamping = 0.2f; body.angularDamping = 1f;
            body.interpolation = RigidbodyInterpolation.Interpolate; body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            var load = cargo.AddComponent<Load>(); load.loadId = "C" + (i + 1); load.containerColor = names[i]; load.liftOffset = new Vector3(0, 0.7f, 0);
            Label(load.loadId, cargo.transform, new Vector3(0, 0.665f, 0), 2.5f, 1.1f, 0.8f, Color.white, Quaternion.Euler(90, 0, 0));
            Label(load.loadId, cargo.transform, new Vector3(0, 0, -0.815f), 2.5f, 1f, 0.65f, Color.white, Quaternion.identity);
            loads[i] = load;
            var targetObject = new GameObject("Receiving bay " + (i + 1));
            var box = targetObject.AddComponent<BoxCollider>(); box.isTrigger = true; box.size = new Vector3(4.6f, 2, 2.8f);
            var target = targetObject.AddComponent<TargetArea>(); target.targetId = "BAY" + (i + 1); target.requiredLoadId = load.loadId;
            target.requiredColor = names[i]; target.researchValidation = true; target.loadHalfExtents = new Vector3(1.7f, 0.65f, 0.8f);
            target.positionTolerance = 0.1f; target.countdownTime = 1.5f;
            Color bayColor = Color.Lerp(mats[i].color, Color.black, 0.5f); bayColor.a = 1;
            var bayMaterial = Material("Bay_" + names[i], bayColor);
            Primitive("Bay floor marking", PrimitiveType.Cube, new Vector3(0, -0.635f, 0), new Vector3(4.6f, 0.02f, 2.8f), bayMaterial, targetObject.transform, false, true);
            for (int side = -1; side <= 1; side += 2)
            {
                Primitive("Bay border", PrimitiveType.Cube, new Vector3(side * 2.3f, -0.60f, 0), new Vector3(0.12f, 0.04f, 2.8f), mats[i], targetObject.transform, false, true);
                Primitive("Bay border", PrimitiveType.Cube, new Vector3(0, -0.60f, side * 1.4f), new Vector3(4.6f, 0.04f, 0.12f), mats[i], targetObject.transform, false, true);
            }
            Label("BAY " + (i + 1), targetObject.transform, new Vector3(0, -0.615f, 0), 3.4f, 1f, 0.65f, Color.white, Quaternion.Euler(90, 0, 0));
            targets[i] = target;
        }
        session.loads = loads; session.targets = targets;
        layouts.targetAreaTransforms = targets.Select(t => t.transform).ToArray();
        layouts.loadTransforms = loads.Select(l => l.transform).ToArray(); layouts.loadRigidbodies = loads.Select(l => l.Body).ToArray();
        session.layoutA = Config("A", "Dispersed receiving bays", sources, Enumerable.Range(0, 3).Select(i => Polar(pivot, direction, 65 + 25 * i, 22, deckTop + 0.65f)).ToArray());
        session.layoutB = Config("B", "Consolidated receiving bays", sources, Enumerable.Range(0, 3).Select(i => Polar(pivot, direction, 35 + 15 * i, 22, deckTop + 0.65f)).ToArray());
        layouts.defaultLayout = session.layoutA; LayoutSelector.SetLayout(session.layoutA); layouts.ApplyLayout(session.layoutA);
        session.resetTransforms = new[] { crane.craneRotationPivot, crane.car, hookRoot.transform }.Concat(loads.Select(l => l.transform)).ToArray();
        session.resetBodies = new[] { hookBody }.Concat(loads.Select(l => l.Body)).ToArray();
        foreach (var body in session.resetBodies) body.isKinematic = true;

        var origin = UnityEngine.Object.FindFirstObjectByType<XROrigin>();
        if (origin == null) throw new InvalidOperationException("Level1 XR Origin was not found.");
        var locomotion = origin.transform.Find("Locomotion"); if (locomotion != null) locomotion.gameObject.SetActive(false);
        var view = station.AddComponent<ResearchOperatorView>(); view.origin = origin;
        var operatorEye = new GameObject("Calibrated operator eye");
        operatorEye.transform.position = new Vector3(pivot.x + 1f, 49.5f, pivot.z - 2.5f);
        operatorEye.transform.rotation = Quaternion.LookRotation(new Vector3(direction.x, -0.5f, direction.z));
        view.operatorEye = operatorEye.transform;
        var overview = new GameObject("Desktop overview eye"); overview.transform.position = new Vector3(65, 65, 95);
        overview.transform.LookAt(new Vector3(29, 33, 47)); view.overviewEye = overview.transform;
        var desktop = new GameObject("Desktop spectator camera"); var desktopCamera = desktop.AddComponent<Camera>(); desktop.AddComponent<AudioListener>();
        desktopCamera.fieldOfView = 58; desktopCamera.nearClipPlane = 0.1f; desktopCamera.farClipPlane = 450;
        desktopCamera.rect = new Rect(0, 0, 0.71f, 1);
        desktopCamera.transform.SetPositionAndRotation(overview.transform.position, overview.transform.rotation);
        desktopCamera.backgroundColor = new Color(0.12f, 0.24f, 0.32f); desktopCamera.clearFlags = CameraClearFlags.Skybox;
        view.desktopCamera = desktopCamera;
        var input = station.AddComponent<ResearchCraneInput>(); input.session = session; input.view = view;
        origin.transform.position += operatorEye.transform.position - origin.Camera.transform.position;
        origin.transform.rotation = Quaternion.Euler(0, operatorEye.transform.eulerAngles.y, 0);
        origin.Camera.nearClipPlane = 0.05f; origin.Camera.farClipPlane = 450;
        foreach (var camera in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
            if (camera != desktopCamera && camera != origin.Camera) camera.enabled = false;

        var desktopCanvas = Panel(session, view, false); view.desktopPanel = desktopCanvas;
        var worldCanvas = Panel(session, view, true); view.worldPanel = worldCanvas;
        worldCanvas.worldCamera = origin.Camera;
        Vector3 forward = Quaternion.Euler(0, operatorEye.transform.eulerAngles.y, 0) * Vector3.forward;
        Vector3 right = Quaternion.Euler(0, operatorEye.transform.eulerAngles.y, 0) * Vector3.right;
        worldCanvas.transform.position = operatorEye.transform.position + forward * 1.65f + right * 1.05f;
        worldCanvas.transform.rotation = Quaternion.LookRotation(worldCanvas.transform.position - operatorEye.transform.position);
        worldCanvas.transform.localScale = Vector3.one * 0.0012f;

        var eventSystem = UnityEngine.Object.FindFirstObjectByType<EventSystem>();
        if (eventSystem == null) eventSystem = new GameObject("EventSystem").AddComponent<EventSystem>();
        foreach (var module in eventSystem.GetComponents<BaseInputModule>()) UnityEngine.Object.DestroyImmediate(module);
        eventSystem.gameObject.AddComponent<XRUIInputModule>();
        var light = GameObject.Find("Directional Light").GetComponent<Light>();
        foreach (var other in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None)) if (other != light) other.enabled = false;
        light.enabled = true; light.type = LightType.Directional; light.color = Color.white;
        light.transform.rotation = Quaternion.Euler(45, -35, 0); light.intensity = 2.2f; light.shadows = LightShadows.Soft;
        light.cullingMask = -1; RenderSettings.sun = light;
        RenderSettings.ambientMode = AmbientMode.Flat; RenderSettings.ambientLight = new Color(0.6f, 0.68f, 0.75f);
        var ambient = new SphericalHarmonicsL2(); ambient.AddAmbientLight(new Color(0.35f, 0.4f, 0.45f)); RenderSettings.ambientProbe = ambient;
        string skyPath = Folder + "/Materials/HarborSky.mat";
        var sky = AssetDatabase.LoadAssetAtPath<Material>(skyPath);
        if (sky == null) { sky = new Material(Shader.Find("Skybox/Procedural")); AssetDatabase.CreateAsset(sky, skyPath); }
        sky.SetColor("_SkyTint", new Color(0.6f, 0.75f, 0.9f)); sky.SetColor("_GroundColor", new Color(0.15f, 0.3f, 0.35f));
        sky.SetFloat("_Exposure", 1.2f); sky.SetFloat("_AtmosphereThickness", 0.8f); RenderSettings.skybox = sky;
        RenderSettings.fog = true; RenderSettings.fogColor = new Color(0.44f, 0.59f, 0.64f); RenderSettings.fogMode = FogMode.Linear; RenderSettings.fogStartDistance = 90; RenderSettings.fogEndDistance = 350;
        ConfigureSimulator();
        BakeBlenderMeshes(scene.GetRootGameObjects());
        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) }
            .Concat(EditorBuildSettings.scenes.Where(s => s.path != ScenePath)).ToArray();
        AssetDatabase.SaveAssets();
        Debug.Log("RESEARCH_STATION_BUILT " + ScenePath);
    }

    private static Vector3 Polar(Vector3 pivot, Vector3 direction, float angle, float radius, float y)
    { Vector3 point = pivot + Quaternion.Euler(0, angle, 0) * direction * radius; point.y = y; return point; }
    private static LayoutConfig Config(string id, string name, Vector3[] sources, Vector3[] targets)
    {
        string path = Folder + "/Layouts/Layout" + id + ".asset";
        var config = AssetDatabase.LoadAssetAtPath<LayoutConfig>(path);
        if (config == null) { config = ScriptableObject.CreateInstance<LayoutConfig>(); AssetDatabase.CreateAsset(config, path); }
        config.layoutId = id; config.layoutName = "Layout " + id + " - " + name;
        config.description = "Same sources, loads, target precision and crane settings. Only receiving positions change.";
        config.targetPositions = targets; config.loadSpawnPositions = sources; config.loadYawDegrees = new float[3]; config.targetYawDegrees = new float[3];
        EditorUtility.SetDirty(config); return config;
    }
    private static Material Material(string name, Color color)
    {
        string path = Folder + "/Materials/" + name + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null) { material = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(material, path); }
        material.color = color; material.SetFloat("_Smoothness", 0.22f); EditorUtility.SetDirty(material); return material;
    }
    private static GameObject Primitive(string name, PrimitiveType type, Vector3 position, Vector3 scale, Material material, Transform parent, bool collision, bool local = false)
    {
        var obj = GameObject.CreatePrimitive(type); obj.name = name;
        if (parent != null) obj.transform.SetParent(parent, false);
        if (local) obj.transform.localPosition = position; else obj.transform.position = position;
        obj.transform.localScale = scale; obj.GetComponent<Renderer>().sharedMaterial = material;
        if (!collision) UnityEngine.Object.DestroyImmediate(obj.GetComponent<Collider>());
        return obj;
    }
    private static void Label(string text, Transform parent, Vector3 position, float width, float height, float font, Color color, Quaternion rotation)
    {
        var obj = new GameObject(text + " label"); obj.transform.SetParent(parent, false); obj.transform.localPosition = position; obj.transform.localRotation = rotation;
        var tmp = obj.AddComponent<TextMeshPro>(); tmp.text = text; tmp.fontSize = font * 10f; tmp.alignment = TextAlignmentOptions.Center; tmp.color = color;
        tmp.rectTransform.sizeDelta = new Vector2(width, height); tmp.font = TMP_Settings.defaultFontAsset;
    }
    private static void ConfigureSimulator()
    {
        var settings = new SerializedObject(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>("Assets/XRI/Settings/Resources/XRDeviceSimulatorSettings.asset"));
        settings.FindProperty("m_SimulatorPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Samples/XR Interaction Toolkit/3.1.1/XR Device Simulator/XRInteractionSimulator/XR Interaction Simulator.prefab");
        // The research scene has direct desktop controls. Opt into fake tracked hands explicitly for old scenes.
        settings.FindProperty("m_AutomaticallyInstantiateSimulatorPrefab").boolValue = false;
        settings.FindProperty("m_AutomaticallyInstantiateInEditorOnly").boolValue = true;
        settings.ApplyModifiedPropertiesWithoutUndo();
    }

    // The portable research scene must not require Blender to be installed on the lab PC.
    private static void BakeBlenderMeshes(GameObject[] roots)
    {
        string path = Folder + "/PortMeshes.asset";
        var existing = (File.Exists(path) ? AssetDatabase.LoadAllAssetsAtPath(path) : Array.Empty<UnityEngine.Object>()).OfType<Mesh>().ToDictionary(m => m.name);
        var copies = new System.Collections.Generic.Dictionary<Mesh, Mesh>();
        Func<Mesh, Mesh> bake = source =>
        {
            if (source == null || !AssetDatabase.GetAssetPath(source).EndsWith(".blend", StringComparison.OrdinalIgnoreCase)) return source;
            if (copies.TryGetValue(source, out var cached)) return cached;
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(source, out string guid, out long id);
            string key = guid + "_" + id;
            if (!existing.TryGetValue(key, out var mesh))
            {
                mesh = UnityEngine.Object.Instantiate(source); mesh.name = key;
                if (!File.Exists(path)) AssetDatabase.CreateAsset(mesh, path); else AssetDatabase.AddObjectToAsset(mesh, path);
            }
            copies[source] = mesh; return mesh;
        };
        foreach (var root in roots)
        {
            foreach (var filter in root.GetComponentsInChildren<MeshFilter>(true)) filter.sharedMesh = bake(filter.sharedMesh);
            foreach (var collider in root.GetComponentsInChildren<MeshCollider>(true)) collider.sharedMesh = bake(collider.sharedMesh);
        }
        string materialPath = Folder + "/PortMaterials.asset";
        var materials = (File.Exists(materialPath) ? AssetDatabase.LoadAllAssetsAtPath(materialPath) : Array.Empty<UnityEngine.Object>()).OfType<Material>().ToDictionary(m => m.name);
        foreach (var root in roots)
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                var slots = renderer.sharedMaterials;
                for (int i = 0; i < slots.Length; i++)
                {
                    var source = slots[i];
                    if (source == null || !AssetDatabase.GetAssetPath(source).EndsWith(".blend", StringComparison.OrdinalIgnoreCase)) continue;
                    AssetDatabase.TryGetGUIDAndLocalFileIdentifier(source, out string guid, out long id);
                    string key = guid + "_" + id;
                    if (!materials.TryGetValue(key, out var material))
                    {
                        material = new Material(source); material.name = key;
                        if (!File.Exists(materialPath)) AssetDatabase.CreateAsset(material, materialPath); else AssetDatabase.AddObjectToAsset(material, materialPath);
                        materials[key] = material;
                    }
                    slots[i] = material;
                }
                renderer.sharedMaterials = slots;
            }
        Debug.Log("RESEARCH_PORTABLE_MESHES " + copies.Count);
    }

    private static Canvas Panel(ExperimentSession session, ResearchOperatorView view, bool world)
    {
        var obj = new GameObject(world ? "VR research console" : "Desktop research dashboard", typeof(RectTransform));
        var canvas = obj.AddComponent<Canvas>(); canvas.renderMode = world ? RenderMode.WorldSpace : RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 20;
        if (world) { obj.GetComponent<RectTransform>().sizeDelta = new Vector2(560, 1080); obj.AddComponent<TrackedDeviceGraphicRaycaster>(); }
        else
        {
            var scaler = obj.AddComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = 1;
            obj.AddComponent<GraphicRaycaster>();
        }
        var panelObject = new GameObject("Console panel", typeof(RectTransform), typeof(Image)); panelObject.transform.SetParent(obj.transform, false);
        var panelRect = panelObject.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(1, 0.5f); panelRect.anchorMax = new Vector2(1, 0.5f); panelRect.pivot = new Vector2(1, 0.5f); panelRect.sizeDelta = new Vector2(560, 1080);
        panelObject.GetComponent<Image>().color = Navy;
        var panel = panelObject.AddComponent<ResearchPanel>(); panel.session = session; panel.view = view;
        Text(panelRect, "PORTLAB  /  WORK SYSTEMS", 28, 22, 500, 30, 19, Gold);
        panel.heading = Text(panelRect, "A / DISPERSED BAYS", 28, 65, 504, 45, 28, Color.white);
        panel.state = Text(panelRect, "READY FOR CALIBRATION", 28, 117, 500, 25, 17, Teal);
        panel.timer = Text(panelRect, "00:00.0", 28, 147, 500, 68, 56, Color.white);
        panel.job = Text(panelRect, "3 CONTAINERS / 4 WORK ELEMENTS", 28, 219, 500, 26, 18, Gold);
        panel.guidance = Text(panelRect, "Choose a layout and calibrate the view before starting.", 28, 255, 500, 68, 21, Color.white);
        panel.layoutA = Button(panelRect, "A / Dispersed", 28, 334, 245, 40, Slate);
        panel.layoutB = Button(panelRect, "B / Consolidated", 283, 334, 249, 40, Slate);
        Text(panelRect, "PARTICIPANT", 28, 386, 225, 20, 15, Teal);
        Text(panelRect, "ASSIGNED ORDER", 285, 386, 240, 20, 15, Teal);
        panel.participant = Input(panelRect, "P001", 28, 410, 245, 38);
        panel.order = Dropdown(panelRect, 283, 410, 249, 38);
        panel.practice = Button(panelRect, "Practice [P]", 28, 465, 245, 44, Slate);
        panel.start = Button(panelRect, "Start measured [Enter]", 283, 465, 249, 44, new Color(0.15f, 0.47f, 0.42f));
        panel.pause = Button(panelRect, "Pause [Esc]", 28, 521, 120, 36, Slate);
        panel.reset = Button(panelRect, "Reset [R]", 156, 521, 110, 36, Slate);
        panel.recenter = Button(panelRect, "Calibrate [F]", 274, 521, 120, 36, Slate);
        panel.openData = Button(panelRect, "CSV folder", 402, 521, 130, 36, Slate);
        panel.results = Text(panelRect, "Element times and errors appear here.", 28, 573, 504, 100, 19, Color.white);
        Text(panelRect, "DISCOMFORT  /  BEFORE START, AFTER FINISH", 28, 686, 504, 24, 16, Gold);
        Text(panelRect, "Custom self-report: 0 none - 10 maximum", 28, 714, 504, 23, 15, new Color(0.64f, 0.72f, 0.76f));
        panel.discomfort = new Slider[3]; panel.discomfortLabels = new TextMeshProUGUI[3];
        for (int i = 0; i < 3; i++)
        {
            panel.discomfortLabels[i] = Text(panelRect, new[] { "Neck  0", "Shoulders  0", "Hands  0" }[i], 28, 748 + i * 32, 160, 26, 18, Color.white);
            panel.discomfort[i] = Slider(panelRect, 194, 752 + i * 32, 330, 22);
        }
        panel.saveRatings = Button(panelRect, "Save post-run ratings", 28, 857, 245, 36, Slate);
        panel.next = Button(panelRect, "Next layout", 283, 857, 249, 36, Slate);
        panel.export = Text(panelRect, "CSV is saved during the run.", 28, 908, 504, 44, 15, Teal);
        Text(panelRect, "DESKTOP  A/D slew - W/S trolley - Up/Down hoist\nSpace couple/release - Tab view - RMB look\nVR  Left stick slew/trolley - Right stick hoist\nRight A couple/release - Trigger selects console", 28, 971, 504, 90, 16, new Color(0.64f, 0.72f, 0.76f));
        return canvas;
    }
    private static RectTransform Rect(Transform parent, string name, float x, float y, float width, float height)
    {
        var obj = new GameObject(name, typeof(RectTransform)); obj.transform.SetParent(parent, false);
        var rect = obj.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = new Vector2(0, 1); rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = new Vector2(x, -y); rect.sizeDelta = new Vector2(width, height); return rect;
    }
    private static TextMeshProUGUI Text(Transform parent, string text, float x, float y, float width, float height, float font, Color color)
    {
        var rect = Rect(parent, text.Split('\n')[0], x, y, width, height);
        var tmp = rect.gameObject.AddComponent<TextMeshProUGUI>(); tmp.text = text; tmp.font = TMP_Settings.defaultFontAsset;
        tmp.fontSize = font; tmp.color = color; tmp.alignment = TextAlignmentOptions.TopLeft;
        tmp.raycastTarget = false; tmp.textWrappingMode = TextWrappingModes.Normal; return tmp;
    }
    private static Button Button(Transform parent, string text, float x, float y, float width, float height, Color color)
    {
        var rect = Rect(parent, text, x, y, width, height); var image = rect.gameObject.AddComponent<Image>(); image.color = color;
        var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = image;
        var label = Text(rect, text, 5, 0, width - 10, height, 17, Color.white); label.alignment = TextAlignmentOptions.Center;
        var colors = button.colors; colors.highlightedColor = new Color(0.8f, 1, 0.95f); colors.pressedColor = Teal; colors.disabledColor = new Color(0.45f, 0.5f, 0.55f, 0.45f); button.colors = colors;
        return button;
    }
    private static TMP_InputField Input(Transform parent, string value, float x, float y, float width, float height)
    {
        var rect = Rect(parent, "Anonymous ID", x, y, width, height); rect.gameObject.AddComponent<Image>().color = Slate;
        var input = rect.gameObject.AddComponent<TMP_InputField>();
        var text = Text(rect, value, 10, 3, width - 20, height - 6, 20, Color.white);
        input.textViewport = rect; input.textComponent = text; input.text = value; input.characterLimit = 32; input.lineType = TMP_InputField.LineType.SingleLine;
        return input;
    }
    private static TMP_Dropdown Dropdown(Transform parent, float x, float y, float width, float height)
    {
        // A single button cycles order, keeping the VR interaction target large and avoiding popup clipping.
        var rect = Rect(parent, "Condition order", x, y, width, height); rect.gameObject.AddComponent<Image>().color = Slate;
        var dropdown = rect.gameObject.AddComponent<TMP_Dropdown>();
        dropdown.captionText = Text(rect, "A then B", 10, 3, width - 20, height - 6, 20, Color.white);
        dropdown.options = new System.Collections.Generic.List<TMP_Dropdown.OptionData> { new TMP_Dropdown.OptionData("A then B"), new TMP_Dropdown.OptionData("B then A") };
        // Provide a real dropdown template for mouse and tracked-ray selection.
        var template = Rect(rect, "Template", 0, height, width, 80); template.gameObject.AddComponent<Image>().color = Slate;
        var scroll = template.gameObject.AddComponent<ScrollRect>(); scroll.horizontal = false;
        var viewport = Rect(template, "Viewport", 0, 0, width, 80); viewport.gameObject.AddComponent<RectMask2D>();
        var content = Rect(viewport, "Content", 0, 0, width, 40); scroll.viewport = viewport; scroll.content = content;
        var item = Rect(content, "Item", 0, 0, width, 40); var toggle = item.gameObject.AddComponent<Toggle>();
        toggle.targetGraphic = item.gameObject.AddComponent<Image>(); toggle.targetGraphic.color = Slate;
        var itemText = Text(item, "A then B", 10, 3, width - 20, 34, 20, Color.white); dropdown.itemText = itemText;
        dropdown.template = template; template.gameObject.SetActive(false); return dropdown;
    }
    private static Slider Slider(Transform parent, float x, float y, float width, float height)
    {
        var rect = Rect(parent, "Discomfort", x, y, width, height); var slider = rect.gameObject.AddComponent<Slider>();
        slider.minValue = 0; slider.maxValue = 10; slider.wholeNumbers = true;
        var background = Rect(rect, "Track", 0, height * 0.4f, width, height * 0.2f); background.gameObject.AddComponent<Image>().color = Slate;
        var handleArea = Rect(rect, "Handle area", 8, 0, width - 16, height);
        var handle = Rect(handleArea, "Handle", 0, 0, 16, height); var image = handle.gameObject.AddComponent<Image>(); image.color = Teal;
        handle.sizeDelta = new Vector2(16, 0); handle.pivot = new Vector2(0.5f, 0.5f);
        slider.handleRect = handle; slider.targetGraphic = image; return slider;
    }
}
#endif
