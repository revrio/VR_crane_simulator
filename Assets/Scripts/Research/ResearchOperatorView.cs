using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;

public class ResearchOperatorView : MonoBehaviour
{
    public XROrigin origin;
    public Camera desktopCamera;
    public Transform operatorEye, overviewEye;
    public ResearchCabin cabin;
    public bool IsCabinView => mode == 3;
    public Canvas desktopPanel, worldPanel;
    public bool HeadsetRunning { get; private set; }
    public string ViewName => mode == 0 ? "CABIN-SIDE FOLLOW" : mode == 1 ? "OVERHEAD ALIGNMENT" : mode == 2 ? "QUAY OVERVIEW" : "INSIDE CABIN [C]";
    private readonly List<XRDisplaySubsystem> displays = new List<XRDisplaySubsystem>();
    private int mode;
    private bool initialized;
    private float orbit, elevation = 50, distance = 22;
    private float cabinYaw, cabinPitch = 18;
    private void Start()
    {
        // Unity remembers the last windowed test resolution. Use the monitor's native
        // resolution for fullscreen unless an explicit size was requested on the CLI.
        if (!Application.isEditor && Screen.fullScreen && !System.Array.Exists(System.Environment.GetCommandLineArgs(), a => a == "-screen-width"))
        {
            var native = Screen.currentResolution;
            Screen.SetResolution(native.width, native.height, FullScreenMode.FullScreenWindow);
        }
        RefreshMode(); PositionCamera(true);
    }
    private void LateUpdate()
    {
        RefreshMode();
        if (HeadsetRunning) return;
        // Match the fixed-width console at any monitor aspect ratio.
        float workWidth = Mathf.Max(.35f, 1f - 560f * desktopPanel.scaleFactor / Screen.width);
        desktopCamera.rect = new Rect(0, 0, workWidth, 1);
        var controls = desktopPanel.transform.Find("Hold controls");
        if (controls != null) controls.localScale = Vector3.one * Mathf.Min(1, (Screen.width / desktopPanel.scaleFactor - 608f) / 1260f);
        if (Mouse.current != null)
        {
            if (Mouse.current.rightButton.isPressed)
            {
                Vector2 delta = Mouse.current.delta.ReadValue();
                if (IsCabinView) { cabinYaw = Mathf.Clamp(cabinYaw + delta.x * .16f, -160, 160); cabinPitch = Mathf.Clamp(cabinPitch - delta.y * .16f, -35, 70); }
                else { orbit += delta.x * .18f; elevation = Mathf.Clamp(elevation - delta.y * .15f, 20, 78); }
            }
            if (Mouse.current.position.ReadValue().x < Screen.width * desktopCamera.rect.width)
                distance = Mathf.Clamp(distance - Mouse.current.scroll.ReadValue().y * .018f, 8, 32);
        }
        PositionCamera(false);
    }
    private void PositionCamera(bool snap)
    {
        var s = ExperimentSession.Instance;
        if (s == null || desktopCamera == null) return;
        if (mode == 1 || IsCabinView) desktopCamera.cullingMask &= ~(1 << 29); else desktopCamera.cullingMask |= 1 << 29;
        if (IsCabinView) desktopCamera.cullingMask |= 1 << 27; else desktopCamera.cullingMask &= ~(1 << 27);
        if (IsCabinView) desktopCamera.cullingMask |= 1 << 28; else desktopCamera.cullingMask &= ~(1 << 28);
        desktopCamera.fieldOfView = IsCabinView ? 78 : 54;
        float deckHeight = s.targets != null && s.targets.Length > 0 ? s.targets[0].transform.position.y - .65f : 31.2f;
        GetDesktopPose(s.crane, FramingPoint(s, s.hook.transform.position), deckHeight, out Vector3 position, out Quaternion rotation);
        float t = snap ? 1 : 1 - Mathf.Exp(-8 * Time.unscaledDeltaTime);
        desktopCamera.transform.position = IsCabinView ? position : Vector3.Lerp(desktopCamera.transform.position, position, t);
        desktopCamera.transform.rotation = rotation;
    }
    public Vector3 FramingPoint(ExperimentSession session, Vector3 hookPosition)
    {
        if (mode != 0 || session.loads == null || session.loads.Length == 0) return hookPosition;
        Vector3 goal = session.loads[0].transform.position;
        if (session.Run != null && session.Run.Current != null)
            goal = session.Run.Phase == ResearchPhase.Return ? session.homeMarker.position
                : session.hook.Attached != null ? session.ActiveTarget.transform.position : session.ActiveLoad.transform.position;
        Vector3 offset = goal - hookPosition; offset.y = 0;
        // Keep the current goal in view from the cabin side without zooming far out.
        return hookPosition + Vector3.ClampMagnitude(offset * .4f, 5f);
    }

    // Look outward along the boom, as the cabin operator does. This keeps W at the
    // top of the overhead view and D to the right, regardless of the crane's heading.
    public void GetDesktopPose(CraneController crane, Vector3 hookPosition, float deckHeight,
        out Vector3 position, out Quaternion rotation)
    {
        Vector3 outward = crane.car.position - crane.craneRotationPivot.position;
        outward.y = 0;
        if (outward.sqrMagnitude < .001f) outward = crane.craneRotationPivot.forward;
        float cabinYaw = Mathf.Atan2(outward.x, outward.z) * Mathf.Rad2Deg;
        Vector3 focus = hookPosition;
        if (mode == 3 && cabin != null)
        {
            position = operatorEye.position;
            rotation = cabin.transform.rotation * Quaternion.Euler(cabinPitch, this.cabinYaw, 0);
        }
        else if (mode == 1)
        {
            position = new Vector3(focus.x, Mathf.Max(focus.y + 14, deckHeight + 21), focus.z);
            rotation = Quaternion.Euler(90, cabinYaw, 0);
        }
        else if (mode == 2)
        {
            focus = crane.craneRotationPivot.position + outward.normalized * 14;
            focus.y = deckHeight;
            rotation = Quaternion.Euler(55, cabinYaw, 0);
            position = focus - rotation * Vector3.forward * 48;
        }
        else
        {
            focus.y = Mathf.Lerp(deckHeight + .8f, focus.y, .35f);
            rotation = Quaternion.Euler(elevation, cabinYaw + orbit, 0);
            position = focus - rotation * Vector3.forward * distance;
        }
    }

    private void RefreshMode()
    {
        SubsystemManager.GetSubsystems(displays);
        bool running = displays.Exists(d => d.running);
        if (initialized && running == HeadsetRunning) return;
        initialized = true; HeadsetRunning = running;
        if (desktopCamera != null) desktopCamera.enabled = !running;
        if (origin != null && origin.Camera != null)
        {
            origin.Camera.enabled = running;
            var listener = origin.Camera.GetComponent<AudioListener>(); if (listener != null) listener.enabled = running;
        }
        if (desktopCamera != null && desktopCamera.TryGetComponent<AudioListener>(out var dl)) dl.enabled = !running;
        if (desktopPanel != null) desktopPanel.enabled = !running;
        if (worldPanel != null) worldPanel.enabled = running;
        if (running)
        {
            if (cabin != null) origin.transform.SetParent(cabin.transform, true);
            origin.Camera.cullingMask = (origin.Camera.cullingMask | (1 << 27) | (1 << 28)) & ~(1 << 29) & ~(1 << 30);
            Recenter();
        }
    }
    public void Recenter()
    {
        if (HeadsetRunning && origin != null && origin.Camera != null)
        {
            if (ExperimentSession.Instance != null && ExperimentSession.Instance.IsActive) return;
            float offset = operatorEye.eulerAngles.y - origin.Camera.transform.eulerAngles.y;
            origin.transform.RotateAround(origin.Camera.transform.position, Vector3.up, offset);
            origin.transform.position += operatorEye.position - origin.Camera.transform.position;
        }
        else
        {
            if (!IsCabinView) { mode = 0; orbit = 0; elevation = 50; distance = 22; }
            cabinYaw = 0; cabinPitch = 18; PositionCamera(true);
        }
    }
    public void ToggleDesktopView() { SelectDesktopView((mode + 1) % (cabin != null ? 4 : 3)); }
    public void EnterCabin() { if (cabin != null && !HeadsetRunning) SelectDesktopView(3); }
    public void SelectDesktopView(int next)
    {
        mode = next; cabinYaw = 0; cabinPitch = 18;
        ExperimentSession.Instance?.RecordEvent("desktop_view", ViewName);
        PositionCamera(true);
    }
}
