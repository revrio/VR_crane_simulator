using UnityEngine;

// Visual rig only. Rope and ResearchHook retain ownership of physics/coupling.
[DefaultExecutionOrder(150)]
public class ResearchRigVisual : MonoBehaviour
{
    public ExperimentSession session;
    public Transform boom, trolley, head;
    public Transform[] cables;
    public Renderer lockLamp;
    private MaterialPropertyBlock lampProperties;

    private void LateUpdate() => SyncVisuals();

    public void SyncVisuals()
    {
        if (session == null || session.crane == null) return;
        var crane = session.crane;
        Vector3 forward = crane.car.position - crane.craneRotationPivot.position;
        forward.y = 0; forward.Normalize();
        Quaternion heading = Quaternion.LookRotation(forward);
        Vector3 boomPosition = crane.craneRotationPivot.position;
        boomPosition.y = crane.car.position.y + .9f;
        boom.SetPositionAndRotation(boomPosition, heading);
        trolley.SetPositionAndRotation(crane.car.position, heading);
        // Task containers retain world alignment; the spreader follows their frame.
        head.SetPositionAndRotation(session.hook.transform.position, session.hook.transform.rotation);
        for (int i = 0; i < cables.Length; i++)
        {
            float side = i < 2 ? -1 : 1, fore = i % 2 == 0 ? -1 : 1;
            Vector3 top = trolley.TransformPoint(new Vector3(side * .56f, -.12f, fore * .28f));
            Vector3 bottom = head.TransformPoint(new Vector3(side * .28f, .94f, fore * .18f));
            Vector3 delta = top - bottom;
            cables[i].SetPositionAndRotation((top + bottom) * .5f, Quaternion.FromToRotation(Vector3.up, delta));
            cables[i].localScale = new Vector3(.032f, delta.magnitude * .5f, .032f);
        }
        if (lockLamp != null)
        {
            lampProperties ??= new MaterialPropertyBlock();
            lampProperties.SetColor("_BaseColor", session.hook.Attached != null ? new Color(.08f,.9f,.32f) : new Color(1,.25f,.06f));
            lockLamp.SetPropertyBlock(lampProperties);
        }
    }
}
