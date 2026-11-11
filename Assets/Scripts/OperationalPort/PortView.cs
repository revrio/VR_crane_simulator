using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.XR;

[DefaultExecutionOrder(300)]
public class PortView : MonoBehaviour
{
    public PortDirector director;
    public Camera cameraMain,loadCamera;
    public XROrigin origin;
    public Transform cabin,eye;
    public Canvas desktopPanel,worldPanel;
    public RawImage loadScreen;
    public bool HeadsetRunning {get;private set;}
    public int Mode {get;private set;}
    public int MonitorMode {get;private set;}
    public string MonitorName=>new[]{"OVERHEAD LOAD","LOAD FOLLOW","DESTINATION","PORT OVERVIEW"}[MonitorMode];
    public static bool SimulatedVR {
        get {
#if UNITY_EDITOR
            return UnityEditor.EditorPrefs.GetBool("PortLab.SimulatedVR",true);
#else
            return false;
#endif
        }
    }
    public string ModeName=>new[]{"TERMINAL / INTAKE","LOAD FOLLOW","OVERHEAD ALIGNMENT","INSIDE CABIN","PORT ARENA OVERVIEW"}[Mode];
    public int ModeCount=>director.traffic!=null?5:4;
    readonly List<XRDisplaySubsystem> displays=new List<XRDisplaySubsystem>();
    RenderTexture loadTexture;
#if UNITY_EDITOR
    UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation.XRDeviceSimulator craneSimulator;
    bool simulatorPositionMode;
    public bool SimulatorPositionMode=>simulatorPositionMode;
#endif
    float yaw,pitch=23,orbit,zoom=24,elevation=45;
    void Start()
    {
        if(director.GetComponent<PortLayoutFeedback>()==null)director.gameObject.AddComponent<PortLayoutFeedback>().director=director;
        if(director.GetComponent<PortScenery>()==null)director.gameObject.AddComponent<PortScenery>().director=director;
        ResolveHandsOrigin();
        ConfigureSeatedXR();
        if(!Application.isEditor&&Screen.fullScreen&&!System.Array.Exists(System.Environment.GetCommandLineArgs(),a=>a=="-screen-width"))
        {var r=Screen.currentResolution;Screen.SetResolution(r.width,r.height,FullScreenMode.FullScreenWindow);}
        // The desktop camera renders into a cropped Game-view viewport. On OpenGL,
        // URP's MSAA back-buffer resolve is invalid for that path and leaves the
        // camera output black, so keep MSAA for the headset camera only.
        cameraMain.allowMSAA=false;
        loadCamera.allowMSAA=false;
        cameraMain.stereoTargetEye=StereoTargetEyeMask.None;
        loadCamera.stereoTargetEye=StereoTargetEyeMask.None;
        var spectatorData=cameraMain.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
        var feedData=loadCamera.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
        if(spectatorData!=null)spectatorData.allowXRRendering=false;
        if(feedData!=null)feedData.allowXRRendering=false;
#if UNITY_EDITOR_LINUX
        // Unity 6's OpenGL Game view cannot resolve this XR camera's 4x MSAA
        // surface. Keep device builds unchanged and use a single-sample target
        // only while simulating XR in the Linux Editor.
        origin.Camera.allowMSAA=false;
#endif
        var descriptor=new RenderTextureDescriptor(640,360,RenderTextureFormat.ARGB32,16)
        {
            msaaSamples=1,
            useMipMap=false,
            autoGenerateMips=false
        };
        loadTexture=new RenderTexture(descriptor){name="Operational live load camera"};loadTexture.Create();loadCamera.targetTexture=loadTexture;loadScreen.texture=loadTexture;
        DisableEditorSimulatorsForDesktop();
        ApplyDisplayState(IsXRViewRunning());
        Select(3);
    }
    void LateUpdate()
    {
        ConfigureSimulatorInput();
        bool running=IsXRViewRunning();
        if(running!=HeadsetRunning)ApplyDisplayState(running);
        cabin.position=new Vector3(director.crane.GantryX,15,-22);
        bool cabinVisible=true;
        if(loadCamera.gameObject.activeSelf!=cabinVisible)loadCamera.gameObject.SetActive(cabinVisible);
        loadCamera.enabled=cabinVisible;
        UpdateMonitorPose();
        if(HeadsetRunning)return;
        cameraMain.rect=new Rect(0,0,Mathf.Max(.4f,1-560f*desktopPanel.scaleFactor/Screen.width),1);
        if(Mouse.current!=null)
        {
            if(Mouse.current.rightButton.isPressed)
            {
                Vector2 delta=Mouse.current.delta.ReadValue();
                if(Mode==3){yaw=Mathf.Clamp(yaw+delta.x*.15f,-150,150);pitch=Mathf.Clamp(pitch-delta.y*.15f,-40,80);}
                else{orbit+=delta.x*.15f;elevation=Mathf.Clamp(elevation-delta.y*.12f,25,75);}
            }
            if(Mouse.current.position.ReadValue().x<cameraMain.rect.width*Screen.width)zoom=Mathf.Clamp(zoom-Mouse.current.scroll.ReadValue().y*.02f,12,38);
        }
        GetPose(out Vector3 position,out Quaternion rotation);
        cameraMain.transform.position=Mode==3?position:Vector3.Lerp(cameraMain.transform.position,position,1-Mathf.Exp(-5*Time.unscaledDeltaTime));
        cameraMain.transform.rotation=rotation;
    }
    void ResolveHandsOrigin()
    {
        var origins=FindObjectsByType<XROrigin>(FindObjectsInactive.Include,FindObjectsSortMode.None);
        foreach(var candidate in origins)
            if(candidate.name.Contains("XR Origin Hands"))
            {
                origin=candidate;
                return;
            }
    }
    void ConfigureSeatedXR()
    {
        if(origin==null)return;

        // This is a seated crane station. The sample Hands rig also ships with
        // free locomotion, turn, jump and gravity providers. Leaving those on
        // makes the whole operator rig fall and also consumes the thumbsticks
        // that are reserved for the crane controls.
        Transform locomotion=origin.transform.Find("Locomotion");
        if(locomotion!=null)locomotion.gameObject.SetActive(false);

        // Keep the operational console just below eye height on the operator's
        // right. The scene can be rebuilt or migrated without making the menu
        // disappear below the seated headset pose.
        worldPanel.transform.SetParent(cabin,false);
        worldPanel.transform.localPosition=new Vector3(1.6f,1.3f,1f);
        worldPanel.transform.localRotation=Quaternion.Euler(0,45,0);
        worldPanel.transform.localScale=Vector3.one*.0013f;
        worldPanel.worldCamera=origin.Camera;
    }
    bool IsXRViewRunning()
    {
        // The editor is the desktop test harness by default. XRI's device
        // simulator registers a synthetic HMD even though there is no headset
        // display; treating that input device as a display routed WASD into the
        // simulated rig and hid the desktop controls. Pass -portlab-editor-xr
        // only when an editor XR preview is explicitly wanted.
        if(Application.isEditor)return SimulatedVR;

        SubsystemManager.GetSubsystems(displays);
        return displays.Exists(d=>d.running);
    }
    void ConfigureSimulatorInput()
    {
#if UNITY_EDITOR
        if(!SimulatedVR)return;
        if(craneSimulator==null)craneSimulator=FindFirstObjectByType<UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation.XRDeviceSimulator>();
        if(craneSimulator==null)return;
        if(Keyboard.current!=null&&Keyboard.current.f8Key.wasPressedThisFrame)simulatorPositionMode=!simulatorPositionMode;
        // XRI's 1/2/3 keys select mutually exclusive targets. In particular,
        // switching Position off selects None, not Primary2DAxis. Assign an
        // explicit target every frame so crane testing has a predictable mode.
        craneSimulator.axis2DTargets=simulatorPositionMode
            ? UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation.XRDeviceSimulator.Axis2DTargets.Position
            : UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation.XRDeviceSimulator.Axis2DTargets.Primary2DAxis;
        // Escape stops selecting simulated devices and can release Game-view
        // focus. It is not needed by this test harness.
        craneSimulator.stopManipulationAction?.action.Disable();
#endif
    }
#if UNITY_EDITOR
    void OnGUI()
    {
        if(!SimulatedVR)return;
        GUILayout.BeginArea(new Rect(10,10,520,150),GUI.skin.box);
        GUILayout.Label("XR SIMULATOR CONTROLS");
        GUILayout.Label("Hold LEFT SHIFT + A/D: gantry | + W/S: trolley");
        GUILayout.Label("Hold SPACE + W/S: hoist | SPACE + G: precision");
        GUILayout.Label("SPACE + B: lock / release when touching or near the target load");
        GUILayout.Label("Point controller ray + LEFT CLICK: cabin menu");
        GUILayout.Label(simulatorPositionMode?"F8: return to crane controls (currently positioning controllers)":"F8: position controllers | H: controller / hand mode");
        GUILayout.EndArea();
    }
#endif
    void DisableEditorSimulatorsForDesktop()
    {
#if UNITY_EDITOR
        if(SimulatedVR)return;
        foreach(var component in Resources.FindObjectsOfTypeAll<MonoBehaviour>())
        {
            string type=component.GetType().Name;
            if(component.gameObject.scene.IsValid()&&(type.StartsWith("XRDeviceSimulator")||type.StartsWith("XRInteractionSimulator")))
                component.gameObject.SetActive(false);
        }
        Debug.Log("[OperationalPort] Desktop test mode active: WASD/arrow controls enabled; XR simulator input disabled.");
#endif
    }
    void ApplyDisplayState(bool running)
    {
        HeadsetRunning=running;
        // These cameras are stored inactive in the scene so they cannot race
        // during scene load. A Camera component cannot render (and its audio
        // listener cannot run) while its GameObject is inactive, so activate
        // the selected camera object before switching its components.
        if(running&&!origin.gameObject.activeSelf)origin.gameObject.SetActive(true);
        if(!origin.Camera.gameObject.activeSelf)origin.Camera.gameObject.SetActive(true);
        cameraMain.gameObject.SetActive(!running);
        origin.Camera.enabled=running;cameraMain.enabled=!running;
        origin.Camera.GetComponent<AudioListener>().enabled=running;cameraMain.GetComponent<AudioListener>().enabled=!running;
        worldPanel.worldCamera=origin.Camera;
        desktopPanel.gameObject.SetActive(!running);worldPanel.gameObject.SetActive(running);
        if(running){origin.transform.SetParent(cabin,true);Recenter();}
    }
    public void GetPose(out Vector3 position,out Quaternion rotation)
    {
        Vector3 hook=director.crane.hookBody.position;
        if(Mode==0)
        {
            bool watchingIntake=director.Active&&!director.CanOperate;
            Vector3 focus=watchingIntake?director.cargo.position:new Vector3(0,3,director.config.multipleCargo?2:-2);
            position=focus+(director.config.multipleCargo&&!watchingIntake?new Vector3(32,24,-18):new Vector3(26,18,-26));rotation=Quaternion.LookRotation(focus-position);
        }
        else if(Mode==1)
        {
            Vector3 focus=hook;focus.y=Mathf.Lerp(1,hook.y,.65f);rotation=Quaternion.Euler(elevation,orbit,0);position=focus-rotation*Vector3.forward*zoom;
        }
        else if(Mode==2){position=hook+Vector3.up*24;rotation=Quaternion.Euler(90,0,0);}
        else if(Mode==3){position=eye.position;rotation=Quaternion.Euler(pitch,yaw,0);}
        else{rotation=Quaternion.Euler(elevation*.75f,-50+orbit,0);position=new Vector3(0,5,-6)-rotation*Vector3.forward*(zoom*4);}
    }
    public void Select(int mode)
    {
        Mode=mode;yaw=0;pitch=23;orbit=0;
        cameraMain.fieldOfView=mode==3?78:mode==4?76:58;
        cameraMain.cullingMask=~(1<<28);if(mode==3)cameraMain.cullingMask|=1<<28;
        if(mode==2)cameraMain.cullingMask&=~(1<<26);else cameraMain.cullingMask|=1<<26;
        GetPose(out var p,out var r);cameraMain.transform.SetPositionAndRotation(p,r);
        director.Log?.Event("view",ModeName);
    }
    void UpdateMonitorPose()
    {
        Vector3 hook=director.crane.hookBody.position;
        loadCamera.orthographic=MonitorMode==0||MonitorMode==2;
        loadCamera.orthographicSize=MonitorMode==2?10:7;
        loadCamera.fieldOfView=65;loadCamera.farClipPlane=1200;
        if(MonitorMode==0)loadCamera.transform.SetPositionAndRotation(hook+Vector3.up*12,Quaternion.Euler(90,0,0));
        else if(MonitorMode==1)
        {
            Vector3 position=hook+new Vector3(16,14,-18);
            loadCamera.transform.SetPositionAndRotation(position,Quaternion.LookRotation(hook-position));
        }
        else if(MonitorMode==2)
        {
            Vector3 target=director.TargetSlot!=null?director.TargetSlot.position:director.config.ship;
            loadCamera.transform.SetPositionAndRotation(target+Vector3.up*30,Quaternion.Euler(90,0,0));
        }
        else
        {
            Vector3 focus=new Vector3(0,2,-6),position=focus+new Vector3(65,70,-85);
            loadCamera.transform.SetPositionAndRotation(position,Quaternion.LookRotation(focus-position));
        }
    }
    public void Next()
    {
        MonitorMode=(MonitorMode+1)%4;UpdateMonitorPose();
        director.Log?.Event("monitor_view",MonitorName);
    }
    public void Cabin(){if(HeadsetRunning)Recenter();else Select(3);}
    public void OperatorHandoff(){}
    public void Recenter()
    {
        if(HeadsetRunning)
        {
            if(director.CanOperate&&director.Active)return;
            float yawOffset=eye.eulerAngles.y-origin.Camera.transform.eulerAngles.y;
            origin.transform.RotateAround(origin.Camera.transform.position,Vector3.up,yawOffset);origin.transform.position+=eye.position-origin.Camera.transform.position;
        }
        else Select(Mode);
    }
    void OnDestroy(){if(loadTexture==null)return;if(loadCamera!=null)loadCamera.targetTexture=null;loadTexture.Release();Destroy(loadTexture);}
}
