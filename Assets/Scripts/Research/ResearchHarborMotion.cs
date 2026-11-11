using UnityEngine;

// Background scenery only: no colliders, crane commands or experiment state.
public class ResearchHarborMotion : MonoBehaviour
{
    public Transform tug;
    public Vector3 routeCenter, routeRight, routeForward;
    public const float WaterLevel = 23.475f;

    private void Update() => PoseAt(Time.time);

    public void PoseAt(float seconds)
    {
        if (tug == null) return;
        float phase = .6f + seconds * .009f;
        Vector3 position = routeCenter + routeRight * (Mathf.Sin(phase) * 95) + routeForward * (Mathf.Cos(phase) * 24);
        position.y = WaterLevel + Mathf.Sin(seconds * .62f) * .10f;
        Vector3 tangent = routeRight * (Mathf.Cos(phase) * 95) - routeForward * (Mathf.Sin(phase) * 24);
        tug.SetPositionAndRotation(position, Quaternion.LookRotation(tangent) * Quaternion.Euler(Mathf.Sin(seconds*.47f)*.5f,0,Mathf.Sin(seconds*.62f)*1.2f));
    }
}
