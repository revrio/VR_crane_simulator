using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class TargetArea : MonoBehaviour
{
    public string requiredColor;
    public float countdownTime = 2f;
    public string targetId;
    public string requiredLoadId;
    public bool researchValidation;
    public Vector3 loadHalfExtents = new Vector3(1.5f, 0.6f, 0.7f);
    public float positionTolerance = 0.2f;
    public float angleTolerance = 12f;
    public float maximumLinearSpeed = 0.2f;
    public float maximumAngularSpeed = 0.15f;
    public float maximumTilt = 8f;
    public bool Completed { get; private set; }
    public float StableSeconds { get; private set; }
    private readonly HashSet<Collider> overlaps = new HashSet<Collider>();
    private readonly Dictionary<Load, HashSet<Collider>> wrongEntries = new Dictionary<Load, HashSet<Collider>>();
    private BoxCollider zone;
    private Load candidate;
    private void Awake() { zone = GetComponent<BoxCollider>(); zone.isTrigger = true; }

    public void ResetResearchState() { Completed = false; StableSeconds = 0; candidate = null; overlaps.Clear(); wrongEntries.Clear(); }

    private void OnTriggerEnter(Collider other)
    {
        var load = other.attachedRigidbody != null ? other.attachedRigidbody.GetComponent<Load>() : other.GetComponentInParent<Load>();
        if (load == null) return;
        bool matches = string.IsNullOrEmpty(requiredLoadId) ? load.containerColor == requiredColor : load.loadId == requiredLoadId;
        if (!matches)
        {
            if (!wrongEntries.TryGetValue(load, out var contacts))
            {
                contacts = new HashSet<Collider>(); wrongEntries.Add(load, contacts);
                ExperimentSession.Instance?.RecordWrongTarget(load, this);
            }
            contacts.Add(other);
            return;
        }
        if (candidate == null || candidate == load) { candidate = load; overlaps.Add(other); }
    }

    private void OnTriggerExit(Collider other)
    {
        overlaps.Remove(other);
        if (overlaps.Count == 0) { candidate = null; StableSeconds = 0; }
        var load = other.GetComponentInParent<Load>();
        if (load != null && wrongEntries.TryGetValue(load, out var contacts))
        {
            contacts.Remove(other); if (contacts.Count == 0) wrongEntries.Remove(load);
        }
    }

    public bool HorizontalFootprintFits(Load load)
    {
        if (zone == null) zone = GetComponent<BoxCollider>();
        if (load == null) return false;
        Vector3 half = zone.size * .5f + Vector3.one * positionTolerance;
        for (int x = -1; x <= 1; x += 2)
            for (int y = -1; y <= 1; y += 2)
                for (int z = -1; z <= 1; z += 2)
                {
                    Vector3 corner = load.transform.TransformPoint(Vector3.Scale(loadHalfExtents, new Vector3(x, y, z)));
                    Vector3 local = transform.InverseTransformPoint(corner) - zone.center;
                    if (Mathf.Abs(local.x) > half.x || Mathf.Abs(local.z) > half.z) return false;
                }
        return true;
    }

    // This is also the UI's explanation: never leave a failed delivery silently waiting.
    public string PlacementIssue(Load load, bool requireReleased = true)
    {
        if (zone == null) zone = GetComponent<BoxCollider>();
        if (load == null || load.Body == null) return "No container detected";
        if (!string.IsNullOrEmpty(requiredLoadId) && requiredLoadId != load.loadId) return "Wrong container for this bay";
        if (load.IsPlaced) return "Delivery already accepted";
        if (!HorizontalFootprintFits(load)) return "Container extends outside the bay — center the whole container";
        if (Vector3.Angle(load.transform.up, Vector3.up) > maximumTilt) return "Container is tilted — lift and set it down upright";
        if (Mathf.Abs(Mathf.DeltaAngle(load.transform.eulerAngles.y, transform.eulerAngles.y)) > angleTolerance) return "Container is rotated — align it with the bay";
        // Require actual landing, not a stationary container suspended inside the trigger volume.
        float localHeight = transform.InverseTransformPoint(load.transform.position).y - zone.center.y;
        if (Mathf.Abs(localHeight) > .20f) return "Container is above the deck — lower it gently";
        if (requireReleased && load.IsAttached) return "Ready to release — press Space / right A";
        if (load.Body.linearVelocity.magnitude > maximumLinearSpeed || load.Body.angularVelocity.magnitude > maximumAngularSpeed)
            return "Container is moving — let it settle";
        return null;
    }

    public bool ValidPlacement(Load load) => PlacementIssue(load) == null;

    private void FixedUpdate()
    {
        if (Completed) return;
        // Research jobs have a known active load. Validate directly so missed trigger callbacks
        // (reset/reload or several colliders leaving together) cannot strand a landed delivery.
        if (researchValidation)
        {
            var active = ExperimentSession.Instance;
            candidate = active != null && active.IsRunning && active.ActiveTarget == this ? active.ActiveLoad : null;
        }
        if (candidate == null) { StableSeconds = 0; return; }
        if (researchValidation)
        {
            var session = ExperimentSession.Instance;
            if (session == null || !session.IsRunning || session.ActiveLoad != candidate || !ValidPlacement(candidate)) { StableSeconds = 0; return; }
        }
        StableSeconds += Time.fixedDeltaTime;
        if (StableSeconds < countdownTime) return;
        if (researchValidation)
        {
            if (!ExperimentSession.Instance.OnPlaced(candidate, this)) return;
            candidate.IsPlaced = true;
        }
        else LevelManager.Instance?.NotifyTargetFilled(this);
        Completed = true;
    }
}
