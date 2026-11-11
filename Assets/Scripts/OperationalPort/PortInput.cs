using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.XR;
using CommonUsages=UnityEngine.XR.CommonUsages;

public class PortInput : MonoBehaviour
{
    public PortDirector director;
    public Vector3 PointerCommand;
    public bool disabledForTest;
    public InputActionAsset craneActions;
    public bool ControllersReady {get;private set;}
    public Vector2 LeftStickValue {get;private set;}
    public Vector2 RightStickValue {get;private set;}
    public string InputBlockReason {get;private set;}
    bool lastA;
    bool controllerStatusLogged;
    InputAction leftStick,rightStick,attach,precision;

    void OnEnable()
    {
        if(leftStick==null)
        {
            if(craneActions==null)craneActions=Resources.Load<InputActionAsset>("PortCraneControls");
            if(craneActions==null){Debug.LogError("Missing PortCraneControls input asset; crane input cannot start.");enabled=false;return;}
            // Use an imported asset so OpenXR sees the bindings at startup.
            craneActions=Instantiate(craneActions);
            leftStick=craneActions.FindAction("Crane/Left stick",true);
            rightStick=craneActions.FindAction("Crane/Right stick",true);
            attach=craneActions.FindAction("Crane/Lock",true);
            precision=craneActions.FindAction("Crane/Precision",true);
        }
        SetActionsEnabled(true);
    }


    void Update()
    {
        if(disabledForTest)return;
        var selected=EventSystem.current?.currentSelectedGameObject;
        bool typing=selected!=null&&selected.GetComponentInParent<TMP_InputField>()!=null;
        var keyboard=Keyboard.current;
        if(keyboard!=null&&!typing&&!PortView.SimulatedVR)
        {
            if(keyboard.pKey.wasPressedThisFrame||keyboard.enterKey.wasPressedThisFrame)director.Begin();
            if(keyboard.rKey.wasPressedThisFrame)director.ResetOperation();
            if(keyboard.escapeKey.wasPressedThisFrame)director.TogglePause();
            if(keyboard.spaceKey.wasPressedThisFrame)director.ToggleAttachment();
            if(keyboard.tabKey.wasPressedThisFrame)director.view.Next();
            if(keyboard.cKey.wasPressedThisFrame)director.view.Cabin();
            if(keyboard.fKey.wasPressedThisFrame)director.view.Recenter();
            if(keyboard.mKey.wasPressedThisFrame)director.ToggleVoice();
            if(keyboard.digit1Key.wasPressedThisFrame)director.SelectLayout(PortLayout.Accessible);
            if(keyboard.digit2Key.wasPressedThisFrame)director.SelectLayout(PortLayout.Stacked);
            if(keyboard.digit3Key.wasPressedThisFrame)director.SelectLayout(PortLayout.Priority);
            if(keyboard.digit4Key.wasPressedThisFrame)director.SelectLayout(PortLayout.StackPractice);
        }
        var right=InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
        var left=InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
        right.TryGetFeatureValue(CommonUsages.primaryButton,out bool legacyA);
        bool xr=director.view.HeadsetRunning;
        bool ready=left.isValid&&right.isValid||leftStick.controls.Count>0&&rightStick.controls.Count>0;
        if(xr&&(ready!=ControllersReady||!controllerStatusLogged))
        {
            if(ready)Debug.Log("[OperationalPort] Quest controllers detected; crane input is active.");
            else Debug.LogWarning("[OperationalPort] XR display is running, but left/right controllers are not available yet. Wake both controllers and press a button.");
            controllerStatusLogged=true;
        }
        ControllersReady=xr&&ready;
        bool a=xr&&(attach.WasPressedThisFrame()||legacyA&&!lastA);
        if(a)director.ToggleAttachment();
        lastA=legacyA;
        LeftStickValue=leftStick.ReadValue<Vector2>();RightStickValue=rightStick.ReadValue<Vector2>();
        InputBlockReason=!director.CanOperate?"Select a layout and start":director.Paused?"Practice paused: click Resume":typing?"Text field has keyboard focus":"READY";
        if(!director.CanOperate||director.Paused||typing){director.crane.Command=PointerCommand=Vector3.zero;return;}
        Vector3 command=PointerCommand;bool precise=false;
        if(xr)
        {
            Vector2 l=leftStick.ReadValue<Vector2>(),r=rightStick.ReadValue<Vector2>();
            left.TryGetFeatureValue(CommonUsages.primary2DAxis,out Vector2 legacyLeft);
            right.TryGetFeatureValue(CommonUsages.primary2DAxis,out Vector2 legacyRight);
            if(leftStick.controls.Count==0)l=legacyLeft;
            if(rightStick.controls.Count==0)r=legacyRight;
#if UNITY_EDITOR
            if(PortView.SimulatedVR)
            {
                // XRI retains stick state when manipulation ends. Require the
                // modifier AND a held direction so release always stops motion.
                bool rightHeld=keyboard!=null&&keyboard.spaceKey.isPressed;
                bool leftHeld=keyboard!=null&&keyboard.leftShiftKey.isPressed;
                bool axisHeld=keyboard!=null&&(keyboard.wKey.isPressed||keyboard.sKey.isPressed||keyboard.aKey.isPressed||keyboard.dKey.isPressed);
                if(!rightHeld||!axisHeld)r=Vector2.zero;
                if(!leftHeld||!axisHeld)l=Vector2.zero;
                if(director.view.SimulatorPositionMode)l=r=Vector2.zero;
            }
#endif
            right.TryGetFeatureValue(CommonUsages.gripButton,out bool legacyPrecision);
            precise=precision.IsPressed()||legacyPrecision;
            command=new Vector3(l.x,l.y,-r.y);
            command.z=Mathf.Sign(command.z)*Mathf.Pow(Mathf.Abs(command.z),1.5f)*.6f;
            command=new Vector3(Mathf.Abs(command.x)<.15f?0:command.x,Mathf.Abs(command.y)<.15f?0:command.y,Mathf.Abs(command.z)<.15f?0:command.z);
        }
        else if(keyboard!=null)
        {
            command+=new Vector3((keyboard.dKey.isPressed?1:0)-(keyboard.aKey.isPressed?1:0),(keyboard.wKey.isPressed?1:0)-(keyboard.sKey.isPressed?1:0),(keyboard.downArrowKey.isPressed?1:0)-(keyboard.upArrowKey.isPressed?1:0));
            precise=keyboard.leftShiftKey.isPressed;
        }
        director.crane.Command=Vector3.ClampMagnitude(command,1.7321f)*(precise?.3f:1);
    }
    void OnDisable()
    {
        SetActionsEnabled(false);ControllersReady=false;controllerStatusLogged=false;
        PointerCommand=Vector3.zero;if(director!=null&&director.crane!=null)director.crane.Command=Vector3.zero;
    }
    void SetActionsEnabled(bool enabled)
    {
        InputAction[] actions={leftStick,rightStick,attach,precision};
        foreach(var action in actions)if(action!=null){if(enabled)action.Enable();else action.Disable();}
    }
    void OnDestroy(){if(craneActions!=null){craneActions.Disable();Destroy(craneActions);}}
    void OnApplicationFocus(bool focus){if(!focus){PointerCommand=Vector3.zero;director.crane.Command=Vector3.zero;}}
}
