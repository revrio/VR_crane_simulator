using UnityEngine;

// Original stylized character; deterministic idle, greeting and task-pointing gestures.
public class ResearchInstructor : MonoBehaviour
{
    public ResearchGuide guide;
    public ResearchCabin cabin;
    public Transform head, arm, mouth;
    private Quaternion armRest;
    private float greetingUntil;
    private bool wasVisible;
    private void Start() { armRest = arm.localRotation; greetingUntil = Time.unscaledTime + 9; }
    private void LateUpdate()
    {
        bool visible = guide.view.HeadsetRunning || guide.view.IsCabinView;
        if (visible && !wasVisible) greetingUntil = Time.unscaledTime + 5;
        wasVisible = visible;
        var viewer = guide.view.HeadsetRunning ? guide.view.origin.Camera.transform : cabin.eye;
        Vector3 localViewer = transform.InverseTransformPoint(viewer.position) - head.localPosition;
        // The character is modelled facing local -Z.
        Quaternion look = Quaternion.LookRotation(-localViewer.normalized);
        var angles = look.eulerAngles;
        float yaw = Mathf.Clamp(Mathf.DeltaAngle(0, angles.y), -45, 45);
        float pitch = Mathf.Clamp(Mathf.DeltaAngle(0, angles.x), -15, 20);
        head.localRotation = Quaternion.Slerp(head.localRotation, Quaternion.Euler(pitch, yaw, Mathf.Sin(Time.unscaledTime) * 1.5f), 1 - Mathf.Exp(-5 * Time.unscaledDeltaTime));
        Quaternion gesture = armRest;
        if (Time.unscaledTime < greetingUntil && cabin.session.Run == null)
            gesture = Quaternion.Euler(0, 0, -145 + Mathf.Sin(Time.unscaledTime * 5) * 12);
        else if (guide.IsSpeaking && cabin.session.IsRunning)
        {
            var session = cabin.session;
            Vector3 goal = session.Run.Phase == ResearchPhase.Return ? session.homeMarker.position
                : session.hook.Attached != null ? session.ActiveTarget.transform.position : session.ActiveLoad.transform.position;
            Vector3 direction = arm.parent.InverseTransformDirection(goal - arm.position).normalized;
            gesture = Quaternion.FromToRotation(Vector3.down, direction);
        }
        arm.localRotation = Quaternion.Slerp(arm.localRotation, gesture, 1 - Mathf.Exp(-6 * Time.unscaledDeltaTime));
        float speech = guide.IsSpeaking ? .014f + .012f * Mathf.Sin(Time.unscaledTime * 16) : 0;
        mouth.localScale = new Vector3(.10f, .012f + speech, .018f);
    }
}
