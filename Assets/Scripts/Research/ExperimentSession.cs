using System;
using System.Linq;
using UnityEngine;
using UnityEngine.XR;

[DefaultExecutionOrder(100)]
public class ExperimentSession : MonoBehaviour
{
    public static ExperimentSession Instance { get; private set; }
    public LayoutManager layoutManager;
    public LayoutConfig layoutA, layoutB, layoutC;
    public CraneController crane;
    public ResearchHook hook;
    public Load[] loads;
    public TargetArea[] targets;
    public Transform[] resetTransforms;
    public Rigidbody[] resetBodies;
    public Transform homeMarker;
    public float homeTolerance = 1f;
    public float homeHeightTolerance = 2f;
    public float homeDwellSeconds = .5f;
    public float HomeStableSeconds { get; private set; }
    public float HomeHorizontalDistance { get { var d = hook.transform.position - homeMarker.position; d.y = 0; return d.magnitude; } }
    public float HomeHeightDifference => Mathf.Abs(hook.transform.position.y - homeMarker.position.y);
    public bool HomePositionReady => HomeHorizontalDistance <= homeTolerance && HomeHeightDifference <= homeHeightTolerance;
    public float approachRadius = 3f;
    public float runTimeoutSeconds = 900f;
    public string participantId = "P001";
    public string conditionOrder = "ABC";
    public string experimentVersion = "wsd-demo-v1";
    public string setupLabel = "Seated operator station; calibrate before starting";
    [NonSerialized] public string dataDirectoryOverride;
    public float neckBefore, shoulderBefore, handBefore;
    public ResearchRun Run { get; private set; }
    public ResearchCsv Logger { get; private set; }
    public bool IsRunning => Run != null && Run.State == ResearchRunState.Running;
    public bool IsActive => Run != null && (Run.State == ResearchRunState.Running || Run.State == ResearchRunState.Paused);
    public bool Practice { get; private set; }
    public Load ActiveLoad => Run?.Current == null || loads == null ? null : loads[Mathf.Clamp(Run.Current.task - 1, 0, loads.Length - 1)];
    public TargetArea ActiveTarget => Run?.Current == null || targets == null ? null : targets[Mathf.Clamp(Run.Current.task - 1, 0, targets.Length - 1)];
    public string Message { get; private set; } = "Choose a layout, calibrate the view, then start practice or measurement.";
    public string DataPath => Logger?.DirectoryPath ?? System.IO.Path.Combine(Application.persistentDataPath, "ResearchSessions");
    public int PlacedCount => loads == null ? 0 : loads.Count(l => l != null && l.IsPlaced);
    public string CurrentLayout => layoutManager != null ? layoutManager.GetActiveLayoutName() : "Unknown";
    public string CurrentLayoutId => layoutManager != null && layoutManager.ActiveLayout != null ? layoutManager.ActiveLayout.layoutId : "unknown";
    public float TransferClearanceHeight => Mathf.Max(34.3f,
        layoutManager?.ActiveLayout?.loadSpawnPositions != null && layoutManager.ActiveLayout.loadSpawnPositions.Length > 0
            ? layoutManager.ActiveLayout.loadSpawnPositions.Max(p => p.y) + 1 : 34.3f);
    private Vector3[] savedPositions;
    private Quaternion[] savedRotations;
    private float initialRopeLength;
    private double nextCheckpoint;
    private float startSlew, startTrolley, startHoist;
    private Vector3 previousLoadPosition;
    private bool captured, closing;
    private static double Now => Time.realtimeSinceStartupAsDouble;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        CaptureResetState();
        FreezeBodies(true);
        Message = "Choose A, B or stacked C. Calibrate while seated. Practice runs are saved separately.";
    }

    public void CaptureResetState()
    {
        resetTransforms = resetTransforms ?? Array.Empty<Transform>();
        resetBodies = resetBodies ?? Array.Empty<Rigidbody>();
        savedPositions = resetTransforms.Select(t => t.localPosition).ToArray();
        savedRotations = resetTransforms.Select(t => t.localRotation).ToArray();
        initialRopeLength = crane != null && crane.hookRope != null ? crane.hookRope.maxLength : 5f;
        captured = true;
    }

    public bool SelectLayout(LayoutConfig config)
    {
        if (IsActive || config == null || layoutManager == null) return false;
        if (!layoutManager.ApplyLayout(config)) { Message = "Layout references are invalid. See the Unity console."; return false; }
        ResetStation();
        return true;
    }

    public void SelectNextLayout()
    {
        string sequence = string.IsNullOrEmpty(conditionOrder) ? "ABC" : conditionOrder;
        int current = sequence.IndexOf(CurrentLayoutId, StringComparison.Ordinal);
        char next = sequence[(current + 1) % sequence.Length];
        SelectLayout(next == 'C' ? layoutC : next == 'B' ? layoutB : layoutA);
    }

    public void ResetStation()
    {
        if (!captured) CaptureResetState();
        if (IsActive) Abort("reset");
        hook?.Release(false);
        Time.timeScale = 1f;
        FreezeBodies(true);
        for (int i = 0; i < resetTransforms.Length; i++)
        {
            resetTransforms[i].localPosition = savedPositions[i];
            resetTransforms[i].localRotation = savedRotations[i];
        }
        if (crane != null)
        {
            crane.ResetTravel();
            if (crane.hookRope != null) crane.hookRope.maxLength = initialRopeLength;
        }
        foreach (var load in loads) load.ResetResearchState();
        foreach (var target in targets) target.ResetResearchState();
        layoutManager.ApplyLayout(layoutManager.ActiveLayout != null ? layoutManager.ActiveLayout : layoutA);
        Physics.SyncTransforms();
        Run = null; HomeStableSeconds = 0;
        Message = "Station reset. Choose practice or start a measured run.";
    }

    public bool BeginRun(bool practice)
    {
        if (IsActive) return false;
        if (string.IsNullOrWhiteSpace(participantId)) { Message = "Enter an anonymous participant ID first."; return false; }
        if (loads == null || targets == null || loads.Length == 0 || loads.Length != targets.Length || crane == null || hook == null || homeMarker == null)
        { Message = "Incomplete station references. Run scene validation."; return false; }
        for (int i = 0; i < loads.Length; i++)
            if (loads[i] == null || targets[i] == null || loads[i].Body == null || string.IsNullOrEmpty(loads[i].loadId) || targets[i].requiredLoadId != loads[i].loadId)
            { Message = "Invalid load/target job mapping."; return false; }
        if (loads.Select(l => l.loadId).Distinct().Count() != loads.Length || targets.Select(t => t.targetId).Distinct().Count() != targets.Length)
        { Message = "Load and target IDs must be unique."; return false; }
        ResetStation();
        Logger?.Dispose();
        Practice = practice;
        double now = Now;
        Run = new ResearchRun(now);
        Run.BeginTask(loads[0].loadId, targets[0].targetId, now);
        var operatorView = FindFirstObjectByType<ResearchOperatorView>();
        Logger = new ResearchCsv(string.IsNullOrEmpty(dataDirectoryOverride) ? System.IO.Path.Combine(Application.persistentDataPath, "ResearchSessions") : dataDirectoryOverride, Run,
            participantId.Trim(), CurrentLayoutId, conditionOrder, practice, operatorView != null && operatorView.HeadsetRunning ? "XR controllers" : "Desktop keyboard", Application.version + "/" + experimentVersion + "/cabin-view-v1");
        Debug.Log("[Research] Session data: " + Logger.DirectoryPath);
        Logger.WriteSetup(JsonUtility.ToJson(new SetupSnapshot {
            experimentVersion = experimentVersion, layoutId = CurrentLayoutId, layoutName = CurrentLayout,
            sourcePositions = loads.Select(l => l.transform.position).ToArray(),
            targetPositions = targets.Select(t => t.transform.position).ToArray(),
            slewDegreesPerSecond = crane.rotationSpeed, trolleyLocalSpeed = crane.carSpeed,
            hoistMetresPerSecond = crane.hookSpeed, placementDwellSeconds = targets[0].countdownTime,
            targetSize = targets[0].GetComponent<BoxCollider>().size, angleTolerance = targets[0].angleTolerance,
            maximumLinearSpeed = targets[0].maximumLinearSpeed, maximumAngularSpeed = targets[0].maximumAngularSpeed,
            maximumTilt = targets[0].maximumTilt, positionTolerance = targets[0].positionTolerance,
            homeTolerance = homeTolerance, homeHeightTolerance = homeHeightTolerance, homeDwellSeconds = homeDwellSeconds,
            hookDamping = hook.GetComponent<Rigidbody>().linearDamping, loadDamping = loads[0].Body.linearDamping,
            approachRadius = approachRadius, setup = setupLabel, suggestedClearanceHeight = TransferClearanceHeight,
            guideVoiceMuted = FindFirstObjectByType<ResearchGuide>()?.VoiceMuted ?? true,
            couplingRadius = hook.captureRadius, precisionMultiplier = .3f, cameraReference = "four-views-plus-seated-cabin-npc-v1",
            unityVersion = Application.unityVersion, device = SystemInfo.deviceModel, runtime = XRSettings.loadedDeviceName
        }, true));
        Logger.Rating(Run, "neck_discomfort", "before", neckBefore);
        Logger.Rating(Run, "shoulder_discomfort", "before", shoulderBefore);
        Logger.Rating(Run, "hand_discomfort", "before", handBefore);
        RecordEvent("run_start", CurrentLayoutId, practice ? "practice" : "measured");
        BeginTaskTelemetry();
        Checkpoint();
        if (!Logger.Healthy)
        {
            Abort("export_failed");
            Message = "Cannot start measurement: export failed. " + Logger.Error;
            return false;
        }
        FreezeBodies(false);
        Message = "Pick up " + ActiveLoad.loadId + ". Match its number and color to the receiving bay.";
        nextCheckpoint = now + 2;
        return true;
    }

    private void Update()
    {
        if (!IsRunning) return;
        double now = Now;
        Run.Advance(now);
        if (Run.ActiveSeconds >= runTimeoutSeconds) { Abort("timeout"); return; }
        if (Run.Phase == ResearchPhase.Transfer && ActiveLoad != null && ActiveTarget != null)
        {
            Vector3 offset = ActiveLoad.transform.position - ActiveTarget.transform.position;
            offset.y = 0;
            if (offset.magnitude <= approachRadius) ChangePhase(ResearchPhase.Placement);
        }
        if (Run.Phase == ResearchPhase.Return)
        {
            bool parked = hook.Attached == null && HomePositionReady && hook.GetComponent<Rigidbody>().linearVelocity.magnitude <= 1f;
            HomeStableSeconds = parked ? HomeStableSeconds + Time.deltaTime : 0;
            if (HomeStableSeconds >= homeDwellSeconds) FinishTask();
        }
        else HomeStableSeconds = 0;
        if (now >= nextCheckpoint) { Checkpoint(); nextCheckpoint = now + 2; }
    }

    private void FixedUpdate()
    {
        if (!IsRunning || ActiveLoad == null) return;
        Vector3 position = ActiveLoad.transform.position;
        Run.Current.loadPathMetres += Vector3.Distance(position, previousLoadPosition);
        previousLoadPosition = position;
        UpdateTravel();
    }

    private void BeginTaskTelemetry()
    {
        startSlew = crane.AbsoluteSlewDegrees; startTrolley = crane.TrolleyTravelMetres; startHoist = crane.HoistTravelMetres;
        previousLoadPosition = ActiveLoad.transform.position;
        RecordEvent("job_cue", ActiveLoad.loadId);
    }
    private void UpdateTravel()
    {
        if (Run?.Current == null) return;
        Run.Current.slewDegrees = crane.AbsoluteSlewDegrees - startSlew;
        Run.Current.trolleyMetres = crane.TrolleyTravelMetres - startTrolley;
        Run.Current.hoistMetres = crane.HoistTravelMetres - startHoist;
    }
    private void ChangePhase(ResearchPhase phase)
    {
        if (!Run.SetPhase(phase, Now)) return;
        RecordEvent("phase", ActiveLoad.loadId, phase);
        Checkpoint();
    }
    public void OnAcquired(Load load)
    {
        if (!IsRunning || load != ActiveLoad) return;
        RecordEvent("acquired", load.loadId);
        if (Run.Phase == ResearchPhase.Pickup) ChangePhase(ResearchPhase.Transfer);
        Message = "Lift clear and transfer " + load.loadId + " to " + ActiveTarget.targetId + ".";
    }
    public void OnReleased(Load load)
    {
        if (!IsRunning || load != ActiveLoad) return;
        RecordEvent("released", load.loadId);
        Message = "Let the load settle inside the matching bay. It must be released, aligned, and still.";
        Checkpoint();
    }
    public bool OnPlaced(Load load, TargetArea target)
    {
        if (!IsRunning || load != ActiveLoad || target != ActiveTarget || load.IsAttached || load.IsPlaced) return false;
        if (Run.Phase == ResearchPhase.Transfer) ChangePhase(ResearchPhase.Placement);
        if (Run.Phase != ResearchPhase.Placement) return false;
        RecordEvent("placement_accepted", load.loadId);
        ChangePhase(ResearchPhase.Return);
        Message = "Placement accepted. Return the empty spreader to the HOME marker.";
        return true;
    }
    private void FinishTask()
    {
        double now = Now;
        UpdateTravel();
        if (!Run.CompleteTask(now)) return;
        RecordEvent("task_completed", ActiveLoad.loadId);
        Checkpoint();
        if (Run.Tasks.Count == loads.Length)
        {
            Run.Finish(now);
            crane.StopCommands();
            FreezeBodies(true);
            RecordEvent("run_completed", CurrentLayoutId);
            Checkpoint();
            Message = "Run complete. Save post-run discomfort ratings, then reset or choose the next layout.";
        }
        else
        {
            int index = Run.Tasks.Count;
            Run.BeginTask(loads[index].loadId, targets[index].targetId, Now);
            BeginTaskTelemetry();
            Message = "Next job: pick up " + ActiveLoad.loadId + ".";
            Checkpoint();
        }
    }
    public void RecordCollision(Load load, float speed)
    {
        if (!IsRunning || load != ActiveLoad) return;
        Run.Current.collisions++;
        RecordEvent("load_impact_above_2_mps", load.loadId, speed);
        Checkpoint();
    }
    public void RecordWrongTarget(Load load, TargetArea target)
    {
        if (!IsRunning || load != ActiveLoad || target == ActiveTarget) return;
        Run.Current.wrongTargets++;
        RecordEvent("wrong_target_entry", target.targetId);
        Checkpoint();
    }
    public void RecordEvent(string type, string objectId, object value = null)
    {
        if (Run == null) return;
        Run.Advance(Now);
        if (type == "high_command_input" && IsRunning && Run.Current != null) Run.Current.highInputs++;
        Logger?.Event(Run, type, objectId, value);
    }
    public void TogglePause()
    {
        if (Run == null) return;
        if (Run.State == ResearchRunState.Running)
        {
            Run.Pause(Now); crane.StopCommands(); Time.timeScale = 0;
            RecordEvent("paused", "operator"); Message = "Paused. Resume when ready; paused time is excluded.";
        }
        else if (Run.State == ResearchRunState.Paused)
        {
            Run.Resume(Now); Time.timeScale = 1;
            RecordEvent("resumed", "operator"); Message = "Resumed. Continue the current job.";
        }
        Checkpoint();
    }
    public void Abort(string reason = "operator_abort")
    {
        if (!IsActive) return;
        UpdateTravel(); Run.Abort(Now, reason); crane.StopCommands();
        hook.Release(false); FreezeBodies(true); Time.timeScale = 1;
        RecordEvent("run_aborted", reason); Checkpoint(); Message = "Run stopped: " + reason + ". Partial data saved.";
    }
    public void SavePostRatings(float neck, float shoulder, float hand)
    {
        if (Run == null || IsActive || Logger == null) return;
        Logger.Rating(Run, "neck_discomfort", "after", neck);
        Logger.Rating(Run, "shoulder_discomfort", "after", shoulder);
        Logger.Rating(Run, "hand_discomfort", "after", hand);
        Message = Logger.Healthy ? "Post-run ratings saved." : "Rating export failed: " + Logger.Error;
    }
    private void Checkpoint() { UpdateTravel(); Logger?.Checkpoint(Run); }
    private void FreezeBodies(bool frozen)
    {
        foreach (var body in resetBodies ?? Array.Empty<Rigidbody>())
        {
            if (body == null) continue;
            if (!body.isKinematic) { body.linearVelocity = Vector3.zero; body.angularVelocity = Vector3.zero; }
            body.isKinematic = frozen;
        }
    }
    private void OnApplicationFocus(bool focused) { if (!Application.isBatchMode && !focused && IsRunning) TogglePause(); }
    private void OnApplicationPause(bool paused) { if (paused && IsRunning) TogglePause(); }
    private void OnApplicationQuit() => Close();
    private void OnDestroy() { Close(); if (Instance == this) Instance = null; }
    private void Close()
    {
        if (closing) return;
        closing = true;
        if (IsActive) Abort("application_closed");
        Logger?.Dispose(); Time.timeScale = 1;
    }

    [Serializable] private class SetupSnapshot
    {
        public string experimentVersion, layoutId, layoutName, setup, unityVersion, device, runtime;
        public bool guideVoiceMuted;
        public string cameraReference;
        public float couplingRadius, precisionMultiplier, homeHeightTolerance, homeDwellSeconds, hookDamping, loadDamping, suggestedClearanceHeight;
        public Vector3[] sourcePositions, targetPositions;
        public Vector3 targetSize;
        public float slewDegreesPerSecond, trolleyLocalSpeed, hoistMetresPerSecond, placementDwellSeconds;
        public float angleTolerance, maximumLinearSpeed, maximumAngularSpeed, maximumTilt, positionTolerance, homeTolerance, approachRadius;
    }
}
