using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.XR;
using CommonUsages = UnityEngine.XR.CommonUsages;

[DefaultExecutionOrder(200)]
public class ResearchCraneInput : MonoBehaviour
{
    public ExperimentSession session;
    public ResearchOperatorView view;
    public Vector3 PointerCommand;
    private bool previousPrimary, previousPrecision;

    private void Update()
    {
        var keyboard = Keyboard.current;
        var selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
        bool typing = selected != null && selected.GetComponentInParent<TMP_InputField>() != null;
        if (!typing && keyboard != null)
        {
            if (keyboard.digit1Key.wasPressedThisFrame) session.SelectLayout(session.layoutA);
            if (keyboard.digit2Key.wasPressedThisFrame) session.SelectLayout(session.layoutB);
            if (keyboard.digit3Key.wasPressedThisFrame) session.SelectLayout(session.layoutC);
            if (keyboard.enterKey.wasPressedThisFrame) session.BeginRun(false);
            if (keyboard.pKey.wasPressedThisFrame) session.BeginRun(true);
            if (keyboard.escapeKey.wasPressedThisFrame) session.TogglePause();
            if (keyboard.rKey.wasPressedThisFrame) session.ResetStation();
            if (keyboard.fKey.wasPressedThisFrame) view.Recenter();
            if (keyboard.cKey.wasPressedThisFrame) view.EnterCabin();
            if (keyboard.tabKey.wasPressedThisFrame) view.ToggleDesktopView();
            if (keyboard.spaceKey.wasPressedThisFrame) session.hook.ToggleAttachment();
        }
        var right = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
        right.TryGetFeatureValue(CommonUsages.primaryButton, out bool primary);
        if (primary && !previousPrimary && session.IsRunning) session.hook.ToggleAttachment();
        previousPrimary = primary;
        if (!session.IsRunning || typing) { PointerCommand = Vector3.zero; session.crane.StopCommands(); previousPrecision = false; return; }
        float slew = PointerCommand.x, trolley = PointerCommand.y, hoist = PointerCommand.z;
        if (view.HeadsetRunning)
        {
            var left = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
            left.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 leftAxis);
            right.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 rightAxis);
            slew = leftAxis.x; trolley = leftAxis.y; hoist = -rightAxis.y;
        }
        else if (keyboard != null)
        {
            slew += (keyboard.dKey.isPressed ? 1 : 0) - (keyboard.aKey.isPressed ? 1 : 0);
            trolley += (keyboard.wKey.isPressed ? 1 : 0) - (keyboard.sKey.isPressed ? 1 : 0);
            hoist += (keyboard.downArrowKey.isPressed ? 1 : 0) - (keyboard.upArrowKey.isPressed ? 1 : 0);
        }
        right.TryGetFeatureValue(CommonUsages.gripButton, out bool grip);
        bool precise = view.HeadsetRunning ? grip : keyboard != null && keyboard.leftShiftKey.isPressed;
        if (precise != previousPrecision) session.RecordEvent("precision_mode", "controls", precise);
        previousPrecision = precise;
        float precision = precise ? .3f : 1f;
        session.crane.CraneRotation(Mathf.Abs(slew) > 0.15f ? slew * precision : 0);
        session.crane.MoveCar(Mathf.Abs(trolley) > 0.15f ? trolley * precision : 0);
        session.crane.MoveHookY(Mathf.Abs(hoist) > 0.15f ? hoist * precision : 0);
    }
    private void OnApplicationFocus(bool focused) { if (!focused) { PointerCommand = Vector3.zero; session?.crane?.StopCommands(); } }
    private void OnDisable() { PointerCommand = Vector3.zero; if (session != null && session.crane != null) session.crane.StopCommands(); }
}
