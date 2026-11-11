using System.Collections.Generic;
using UnityEngine;

public class Load : MonoBehaviour
{
    public string containerColor;
    public AudioClip[] hitSounds;
    public string loadId;
    public Vector3 liftOffset = new Vector3(0, 1, 0);
    public bool IsAttached { get; set; }
    public bool IsPlaced { get; set; }
    public Rigidbody Body => GetComponent<Rigidbody>();
    public Vector3 LiftPoint => transform.TransformPoint(liftOffset);
    private readonly Dictionary<int, double> contacts = new Dictionary<int, double>();

    public void ResetResearchState() { IsAttached = IsPlaced = false; contacts.Clear(); }

    private void OnCollisionEnter(Collision collision)
    {
        float speed = collision.relativeVelocity.magnitude;
        if (speed <= 2f) return;
        if (hitSounds != null && hitSounds.Length > 0 && AudioManagerVR.Instance != null)
            AudioManagerVR.Instance.PlaySFX2D(hitSounds[Random.Range(0, hitSounds.Length)]);
        var session = ExperimentSession.Instance;
        if (session == null || !session.IsRunning || session.ActiveLoad != this) return;
        int id = collision.collider.GetInstanceID();
        double now = Time.realtimeSinceStartupAsDouble;
        if (contacts.TryGetValue(id, out double previous) && now - previous < 0.75) return;
        contacts[id] = now;
        session.RecordCollision(this, speed);
    }
}
