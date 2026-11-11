using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.XR.Management;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features.Interactions;

// Checks that can be performed before taking the APK to the lab.
public class QuestCranePreflight : IPreprocessBuildWithReport
{
    [InitializeOnLoadMethod]
    static void ScheduleRequestedCheck()
    {
        if(!File.Exists("/tmp/portlab-request-quest-check"))return;
        EditorApplication.update+=CheckWhenReady;
    }
    static void CheckWhenReady()
    {
        if(EditorApplication.isCompiling)return;
        if(EditorApplication.isPlaying){EditorApplication.ExitPlaymode();return;}
        if(EditorApplication.isPlayingOrWillChangePlaymode)return;
        EditorApplication.update-=CheckWhenReady;
        File.Delete("/tmp/portlab-request-quest-check");
        try{Run();}catch(Exception e){Debug.LogException(e);}
    }
    public int callbackOrder=>0;
    public void OnPreprocessBuild(BuildReport report)
    {
        if(report.summary.platform==BuildTarget.Android)ValidateSettings();
    }

    [MenuItem("Tools/Operational Port/Verify Quest 2 Settings and XR Inputs")]
    public static void Run()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play Mode before running the isolated input check.");
        try
        {
            ValidateSettings();VerifyInputs();
            const string result="QUEST_PREFLIGHT_PASSED: Android settings, imported XR action asset, separate left/right sticks, right A lock press/release and right grip precision. Physical headset/runtime still requires device verification.";
            File.WriteAllText("/tmp/portlab-quest-preflight.txt",result);Debug.Log(result);
        }
        catch(Exception e){File.WriteAllText("/tmp/portlab-quest-preflight.txt",e.ToString());throw;}
    }

    static void Require(bool condition,string message)
    {if(!condition)throw new BuildFailedException("Quest crane preflight: "+message);}

    static void ValidateSettings()
    {
        Require(PlayerSettings.Android.targetArchitectures==AndroidArchitecture.ARM64,"use ARM64 only");
        Require(PlayerSettings.GetScriptingBackend(NamedBuildTarget.Android)==ScriptingImplementation.IL2CPP,"use IL2CPP");
        var xr=XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(BuildTargetGroup.Android);
        Require(xr!=null&&xr.InitManagerOnStart&&xr.Manager.activeLoaders.Any(l=>l!=null&&l.GetType().Name=="OpenXRLoader"),"enable Android OpenXR initialization");
        var openxr=OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Android);
        Require(openxr!=null&&openxr.GetFeature<OculusTouchControllerProfile>()!=null&&openxr.GetFeature<OculusTouchControllerProfile>().enabled,"enable Oculus Touch controller profile");
        var asset=AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/Resources/PortCraneControls.inputactions");
        Require(asset!=null,"crane input asset did not import");
        foreach(var name in new[]{"Left stick","Right stick","Lock","Precision"})Require(asset.FindAction("Crane/"+name)!=null,"missing action "+name);
        string scene=File.ReadAllText("Assets/Scenes/OperationalPort.unity");
        Require(scene.Contains("guid: 5802329ee0b04ce99c201f96cce01b90"),"scene must reference the crane input asset at startup");
        Require(!EditorBuildSettings.scenes.Where(s=>s.enabled).Any(s=>s.path!="Assets/Scenes/OperationalPort.unity"),"only enable the current OperationalPort scene for the APK");
    }

    static void VerifyInputs()
    {
        XRSimulatedController left=null,right=null;InputActionAsset asset=null;
        // Input System 1.13 suppresses Dynamic updates outside Play Mode.
        // Editor updates also explicitly skip InputAction state monitors. Scope
        // the package's test override to this synchronous check and restore it
        // even on failure; no project input settings are changed.
        var managerField=typeof(InputSystem).GetField("s_Manager",BindingFlags.Static|BindingFlags.NonPublic);
        object manager=managerField?.GetValue(null);
        var updateOverride=manager?.GetType().GetProperty("runPlayerUpdatesInEditMode",BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance);
        Require(updateOverride!=null,"this Input System version does not expose the Edit Mode test update override");
        bool previousOverride=(bool)updateOverride.GetValue(manager);
        try
        {
            updateOverride.SetValue(manager,true);
            left=InputSystem.AddDevice<XRSimulatedController>();right=InputSystem.AddDevice<XRSimulatedController>();
            InputSystem.SetDeviceUsage(left,UnityEngine.InputSystem.CommonUsages.LeftHand);
            InputSystem.SetDeviceUsage(right,UnityEngine.InputSystem.CommonUsages.RightHand);
            asset=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/Resources/PortCraneControls.inputactions"));
            asset.devices=new UnityEngine.InputSystem.InputDevice[]{left,right};asset.Enable();
            InputSystem.QueueStateEvent(left,new XRSimulatedControllerState{primary2DAxis=new Vector2(1,-1).normalized});
            InputSystem.QueueStateEvent(right,new XRSimulatedControllerState{primary2DAxis=Vector2.up}.WithButton(ControllerButton.PrimaryButton).WithButton(ControllerButton.GripButton));
            UpdateInput(InputUpdateType.Dynamic);
            var leftAction=asset.FindAction("Crane/Left stick");
            Require(Vector2.Distance(leftAction.ReadValue<Vector2>(),new Vector2(1,-1).normalized)<.02f,"left thumbstick binding failed: value="+leftAction.ReadValue<Vector2>()+", device="+left.primary2DAxis.ReadValue()+", controls="+string.Join(", ",leftAction.controls.Select(c=>c.path)));
            Require(asset.FindAction("Crane/Right stick").ReadValue<Vector2>().y>.95f,"right thumbstick binding failed");
            Require(asset.FindAction("Crane/Lock").IsPressed(),"right A lock binding failed");
            Require(asset.FindAction("Crane/Precision").IsPressed(),"right grip precision binding failed");
            InputSystem.QueueStateEvent(right,new XRSimulatedControllerState());InputSystem.QueueStateEvent(left,new XRSimulatedControllerState());UpdateInput(InputUpdateType.Dynamic);
            Require(!asset.FindAction("Crane/Lock").IsPressed()&&!asset.FindAction("Crane/Precision").IsPressed(),"button release failed");
            Require(asset.FindAction("Crane/Left stick").ReadValue<Vector2>()==Vector2.zero&&asset.FindAction("Crane/Right stick").ReadValue<Vector2>()==Vector2.zero,"stick release failed");
        }
        finally
        {
            if(asset!=null){asset.Disable();UnityEngine.Object.DestroyImmediate(asset);}
            if(left!=null)InputSystem.RemoveDevice(left);if(right!=null)InputSystem.RemoveDevice(right);
            updateOverride.SetValue(manager,previousOverride);
            UpdateInput(InputUpdateType.Editor);
        }
    }
    static void UpdateInput(InputUpdateType type)
    {
        var method=typeof(InputSystem).GetMethod("Update",BindingFlags.Static|BindingFlags.NonPublic,null,new[]{typeof(InputUpdateType)},null);
        Require(method!=null,"typed Input System test update unavailable");
        method.Invoke(null,new object[]{type});
    }
}
