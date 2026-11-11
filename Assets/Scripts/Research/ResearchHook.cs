using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class ResearchHook : MonoBehaviour
{
    public float captureRadius = 1.2f;
    public Vector3 couplingOffset = new Vector3(0f, -0.4f, 0f);
    public Load Attached { get; private set; }
    public Vector3 CouplingPoint => transform.TransformPoint(couplingOffset);
    private FixedJoint joint;
    private float nextAction;

    public bool ToggleAttachment()
    {
        var session = ExperimentSession.Instance;
        if (session == null || !session.IsRunning || Time.unscaledTime < nextAction) return false;
        nextAction = Time.unscaledTime + 0.25f;
        if (Attached != null) { Release(); return true; }
        if (session.Run.Phase == ResearchPhase.Return)
        { session.RecordEvent("coupling_ignored_during_return", "HOME", "Park the empty hook; no coupling required"); return false; }
        Load load = session.ActiveLoad;
        if (load == null || load.IsPlaced || Vector3.Distance(CouplingPoint, load.LiftPoint) > captureRadius)
        {
            float distance = load != null ? Vector3.Distance(CouplingPoint, load.LiftPoint) : 0;
            session.RecordEvent("coupling_miss", load != null ? load.loadId : "none", distance);
            FindFirstObjectByType<ResearchGuide>()?.NotifyCouplingMiss(distance);
            return false;
        }
        var body = load.Body;
        if (body == null) return false;
        joint = gameObject.AddComponent<FixedJoint>();
        joint.connectedBody = body;
        joint.autoConfigureConnectedAnchor = false;
        joint.anchor = couplingOffset;
        joint.connectedAnchor = body.transform.InverseTransformPoint(load.LiftPoint);
        joint.enableCollision = false;
        joint.breakForce = Mathf.Infinity;
        joint.breakTorque = Mathf.Infinity;
        Attached = load;
        load.IsAttached = true;
        session.OnAcquired(load);
        return true;
    }

    public void Release(bool record = true)
    {
        if (joint != null) { joint.connectedBody = null; Destroy(joint); joint = null; }
        var load = Attached;
        Attached = null;
        if (load == null) return;
        load.IsAttached = false;
        if (record) ExperimentSession.Instance?.OnReleased(load);
    }
    private void OnDisable() => Release(false);
}
