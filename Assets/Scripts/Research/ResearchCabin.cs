using UnityEngine;
using UnityEngine.UI;

[DefaultExecutionOrder(50)]
public class ResearchCabin : MonoBehaviour
{
    public ExperimentSession session;
    public ResearchOperatorView view;
    public Transform eye, leftStick, rightStick;
    public Camera loadCamera;
    public RawImage loadScreen;
    public RenderTexture LoadTexture { get; private set; }
    private Quaternion leftRest, rightRest;

    private void Awake() => SyncFrame();
    private void Start()
    {
        SyncFrame(); view.worldPanel.transform.SetParent(transform, true); leftRest = leftStick.localRotation; rightRest = rightStick.localRotation;
        LoadTexture = new RenderTexture(640, 360, 16) { name = "Cabin live load camera" };
        LoadTexture.Create(); loadCamera.targetTexture = LoadTexture; loadScreen.texture = LoadTexture;
    }
    public void SyncFrame()
    {
        if (session == null || session.crane == null) return;
        var crane = session.crane;
        Vector3 direction = crane.car.position - crane.craneRotationPivot.position; direction.y = 0; direction.Normalize();
        Vector3 position = crane.craneRotationPivot.position + direction * 4;
        position.y = crane.craneRotationPivot.position.y - 5;
        transform.SetPositionAndRotation(position, Quaternion.LookRotation(direction));
    }
    private void Update()
    {
        SyncFrame();
        if (loadCamera == null) return;
        Vector3 hook = session.hook.transform.position;
        Vector3 position = hook + Vector3.up * 12;
        // Screen-up points away from the cabin, matching W and the overhead view.
        loadCamera.transform.SetPositionAndRotation(position, Quaternion.Euler(90, transform.eulerAngles.y, 0));
        bool cabinVisible = view.HeadsetRunning || view.IsCabinView;
        loadCamera.enabled = cabinVisible;
        if (LoadTexture == null) return;
        Vector3 command = session.IsRunning ? session.crane.Command : Vector3.zero;
        leftStick.localRotation = leftRest * Quaternion.Euler(command.y * -18, 0, command.x * -18);
        rightStick.localRotation = rightRest * Quaternion.Euler(command.z * 18, 0, 0);
    }
    private void OnDestroy()
    {
        if (LoadTexture == null) return;
        if (loadCamera != null) loadCamera.targetTexture = null;
        LoadTexture.Release(); Destroy(LoadTexture);
    }
}
