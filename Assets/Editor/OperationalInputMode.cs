using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class OperationalInputMode
{
    const string Desktop="Tools/Operational Port/Input Mode/Desktop Keyboard";
    const string Simulator="Tools/Operational Port/Input Mode/Simulated VR Controllers";
    const string SimulatorPath="Assets/Samples/XR Interaction Toolkit/3.1.2/XR Device Simulator/XRDeviceSimulator/XR Device Simulator.prefab";

    [MenuItem(Desktop)] static void UseDesktop()=>SetMode(false);
    [MenuItem(Simulator)] static void UseSimulator()=>SetMode(true);
    [MenuItem(Desktop,true)] static bool DesktopCheck(){Menu.SetChecked(Desktop,!PortView.SimulatedVR);return !EditorApplication.isPlayingOrWillChangePlaymode;}
    [MenuItem(Simulator,true)] static bool SimulatorCheck(){Menu.SetChecked(Simulator,PortView.SimulatedVR);return !EditorApplication.isPlayingOrWillChangePlaymode;}

    static void SetMode(bool simulated)
    {
        EditorPrefs.SetBool("PortLab.SimulatedVR",simulated);
        var settings=AssetDatabase.LoadAssetAtPath<ScriptableObject>("Assets/XRI/Settings/Resources/XRDeviceSimulatorSettings.asset");
        if(settings!=null)
        {
            var serialized=new SerializedObject(settings);
            serialized.FindProperty("m_AutomaticallyInstantiateSimulatorPrefab").boolValue=true;
            serialized.FindProperty("m_AutomaticallyInstantiateInEditorOnly").boolValue=true;
            serialized.FindProperty("m_UseClassic").boolValue=true;
            // This field references the simulator component, not its prefab root.
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(SimulatorPath);
            if(prefab!=null)serialized.FindProperty("m_SimulatorPrefab").objectReferenceValue=prefab.GetComponent<UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation.XRDeviceSimulator>();
            serialized.ApplyModifiedProperties();AssetDatabase.SaveAssets();
        }
        Debug.Log(simulated?"[OperationalPort] Simulated VR selected. Press Play, click Start staged practice in the crane test panel. Hold Shift + WASD for left stick; Space + W/S for hoist; Space + B for lock. F8 switches controller positioning. Thumbstick mode is automatic; do not use 1/2/3 or Escape.":"[OperationalPort] Desktop keyboard selected. Press Play, then P for staged practice.");
    }
}
