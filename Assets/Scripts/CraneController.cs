using UnityEngine;

public class CraneController : MonoBehaviour
{
    public Transform hook;
    public float hookSpeed = 2f;
    public Transform car;
    public float carSpeed = 0.1f;
    public Transform boomStartCorner;
    public Transform boomEndCorner;
    public Rope hookRope;
    public Transform craneRotationPivot;
    [Tooltip("Maximum slew speed in degrees per second.")]
    public float rotationSpeed = 12f;
    public float rotationSmoothness = 5f;
    public float minimumRopeLength = 0.5f;
    public float maximumRopeLength = 23f;
    public GameObject dangerPanel;
    public AudioClip DangerPanelAudioClip;
    public bool requireResearchRun;

    public Vector3 Command { get; private set; }
    public float AbsoluteSlewDegrees { get; private set; }
    public float TrolleyTravelMetres { get; private set; }
    public float HoistTravelMetres { get; private set; }
    private float slewVelocity;
    private bool highInput;

    public void MoveHookY(float input) => Command = new Vector3(Command.x, Command.y, Mathf.Clamp(input, -1f, 1f));
    public void MoveCar(float input) => Command = new Vector3(Command.x, Mathf.Clamp(input, -1f, 1f), Command.z);
    public void CraneRotation(float input) => Command = new Vector3(Mathf.Clamp(input, -1f, 1f), Command.y, Command.z);
    public void StopCommands() { Command = Vector3.zero; slewVelocity = 0f; }
    public void ResetTravel() { AbsoluteSlewDegrees = TrolleyTravelMetres = HoistTravelMetres = 0f; StopCommands(); }

    private void FixedUpdate()
    {
        if (requireResearchRun && (ExperimentSession.Instance == null || !ExperimentSession.Instance.IsRunning))
        {
            StopCommands();
            SetHighInput(false);
            return;
        }
        Simulate(Time.fixedDeltaTime);
    }

    public void Simulate(float deltaTime)
    {
        if (deltaTime <= 0f) return;
        if (hookRope != null)
        {
            float previous = hookRope.maxLength;
            hookRope.maxLength = Mathf.Clamp(previous + Command.z * hookSpeed * deltaTime, minimumRopeLength, maximumRopeLength);
            HoistTravelMetres += Mathf.Abs(hookRope.maxLength - previous);
        }
        if (car != null && boomStartCorner != null && boomEndCorner != null)
        {
            Vector3 previous = car.position;
            Vector3 local = car.localPosition;
            local.x = Mathf.Clamp(local.x - Command.y * carSpeed * deltaTime,
                Mathf.Min(boomStartCorner.localPosition.x, boomEndCorner.localPosition.x),
                Mathf.Max(boomStartCorner.localPosition.x, boomEndCorner.localPosition.x));
            car.localPosition = local;
            TrolleyTravelMetres += Vector3.Distance(previous, car.position);
        }
        if (craneRotationPivot != null)
        {
            // Persistent exponential smoothing; motion uses a single delta-time factor.
            slewVelocity = Mathf.Lerp(slewVelocity, Command.x * rotationSpeed, 1f - Mathf.Exp(-rotationSmoothness * deltaTime));
            float angle = slewVelocity * deltaTime;
            craneRotationPivot.Rotate(0f, angle, 0f, Space.World);
            AbsoluteSlewDegrees += Mathf.Abs(angle);
        }
        SetHighInput(Mathf.Max(Mathf.Abs(Command.x), Mathf.Abs(Command.y), Mathf.Abs(Command.z)) >= 0.8f);
    }

    private void SetHighInput(bool value)
    {
        if (value == highInput) return;
        highInput = value;
        if (dangerPanel != null) dangerPanel.SetActive(value);
        if (value) ExperimentSession.Instance?.RecordEvent("high_command_input", "crane", 1);
    }

    private void OnDisable() => StopCommands();
}
