#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEngine;

// Editor-only physical smoke test: excluded from player builds and never added to the saved scene.
public class ResearchSmokeDriver : MonoBehaviour
{
    private ExperimentSession session;
    private double started;
    private int condition;
    private int liftTask = -1;
    private float pauseUntil;
    private double pausedSeconds;
    private bool testingPause;
    private float nextTrace;
    private bool capturedPreview;
    private int verifiedReturnTask = -1;

    private void Start()
    {
        try
        {
            session = ExperimentSession.Instance;
            FindFirstObjectByType<ResearchCraneInput>().enabled = false;
            session.dataDirectoryOverride = "/tmp/vr-research-physical-smoke";
            session.participantId = "AUTOMATED_SMOKE";
            started = Time.realtimeSinceStartupAsDouble;
            session.SelectLayout(session.layoutA);
            var validationLoad = session.loads[0];
            var validationTarget = session.targets[0];
            validationLoad.transform.position = validationTarget.transform.position;
            validationLoad.IsAttached = true;
            Check(!validationTarget.ValidPlacement(validationLoad), "attached load cannot count");
            Check(validationTarget.HorizontalFootprintFits(validationLoad), "release guidance uses the actual bay footprint");
            Check(validationTarget.PlacementIssue(validationLoad).Contains("release"), "attached landing explains release action");
            validationLoad.IsAttached = false;
            Check(validationTarget.ValidPlacement(validationLoad), "released aligned stationary load accepted");
            validationLoad.transform.position += Vector3.up;
            Check(!validationTarget.ValidPlacement(validationLoad), "suspended load cannot count as landed");
            validationLoad.transform.position = validationTarget.transform.position;
            validationLoad.transform.rotation = Quaternion.Euler(15, 0, 0);
            Check(!validationTarget.ValidPlacement(validationLoad), "tilted load rejected");
            validationLoad.transform.rotation = Quaternion.identity;
            validationLoad.transform.position += Vector3.right * 4;
            Check(!validationTarget.ValidPlacement(validationLoad), "partially outside footprint rejected");
            Check(validationTarget.PlacementIssue(validationLoad).Contains("outside"), "rejected placement gives correction");
            var hookPosition = session.hook.transform.position;
            session.hook.transform.position = session.homeMarker.position + new Vector3(1.5f, 1.5f, 0);
            Check(session.HomePositionReady, "HOME accepts horizontal/height tolerances independently");
            session.hook.transform.position += Vector3.up * 3;
            Check(!session.HomePositionReady, "HOME rejects wrong height");
            session.hook.transform.position = hookPosition;
            session.ResetStation();
            var panel = FindObjectsByType<ResearchPanel>(FindObjectsSortMode.None).First(p => p.GetComponentInParent<Canvas>().renderMode != RenderMode.WorldSpace);
            panel.participant.text = "AUTOMATED_SMOKE";
            panel.practice.onClick.Invoke();
            Check(session.Run != null && session.Practice, "practice button starts a tagged run");
            Check(!session.SelectLayout(session.layoutB), "layout cannot change during active run");
            Check(!session.SelectLayout(session.layoutC), "stack layout cannot change during active run");
            session.TogglePause(); pausedSeconds = session.Run.ActiveSeconds;
            testingPause = true; pauseUntil = Time.realtimeSinceStartup + 0.25f;
        }
        catch (Exception ex) { Fail(ex); }
    }

    private void Update()
    {
        try
        {
            if (testingPause)
            {
                if (Time.realtimeSinceStartup < pauseUntil) return;
                Check(Math.Abs(session.Run.ActiveSeconds - pausedSeconds) < 0.01, "paused runtime clock frozen");
                var guide = FindFirstObjectByType<ResearchGuide>();
                Check(guide != null && guide.Cue == "paused", "guide explains paused state");
                var console = FindObjectsByType<ResearchGuideConsole>(FindObjectsSortMode.None).First(c => c.desktopCouple != null);
                bool muted = guide.VoiceMuted; console.voice.onClick.Invoke();
                Check(guide.VoiceMuted != muted, "serialized console mute button bound at runtime");
                console.voice.onClick.Invoke(); console.repeat.onClick.Invoke();
                string cameraMode = console.panel.view.ViewName; console.changeView.onClick.Invoke();
                Check(console.panel.view.ViewName != cameraMode, "serialized camera button changes view");
                console.panel.view.Recenter();
                Check(guide.voiceClips.Length == 13 && guide.voiceClips.All(c => c != null && c.length > 0), "all offline guide clips present");
                var cabin = FindFirstObjectByType<ResearchCabin>();
                Check(cabin != null && cabin.LoadTexture != null && cabin.loadScreen.texture == cabin.LoadTexture, "live cabin monitor initialized");
                Check(FindFirstObjectByType<ResearchInstructor>() != null && guide.portraits.Length == 0, "Maya is a world NPC rather than a portrait");
                console.cabinView.onClick.Invoke();
                Check(console.panel.view.IsCabinView && (console.panel.view.desktopCamera.cullingMask & (1 << 28)) != 0, "cabin button reveals interior and NPC");
                Check((console.panel.view.desktopCamera.cullingMask & (1 << 29)) == 0, "original crane mesh cannot obscure cabin interior");
                Check((console.panel.view.desktopCamera.cullingMask & (1 << 27)) != 0, "replacement boom visible from cabin");
                Type.GetType("ResearchPreviewCapture, Assembly-CSharp-Editor")?.GetMethod("CaptureCurrent")?.Invoke(null, null);
                File.Copy("docs/images/research-station.png", "docs/images/operator-cabin.png", true);
                console.panel.view.ToggleDesktopView();
                Check(!console.panel.view.IsCabinView, "fourth view returns to follow without resetting run");
                Check(session.homeMarker.position.y > 31.2f && session.homeMarker.position.y < 40, "home beacon within accessible work height");
                session.TogglePause(); testingPause = false;
                Time.timeScale = 6;
            }
            if (Time.realtimeSinceStartupAsDouble - started > 240) throw new Exception("Physical run timed out. " + Trace());
            if (Time.realtimeSinceStartup >= nextTrace) { Debug.Log("RESEARCH_SMOKE " + Trace()); nextTrace = Time.realtimeSinceStartup + 3; }
            if (!capturedPreview && Time.realtimeSinceStartupAsDouble - started > 2)
            {
                Type.GetType("ResearchPreviewCapture, Assembly-CSharp-Editor")?.GetMethod("CaptureCurrent")?.Invoke(null, null);
                capturedPreview = true;
            }
            if (session.Run.State == ResearchRunState.Aborted) throw new Exception("Run aborted: " + session.Message);
            if (session.Run.State == ResearchRunState.Completed)
            {
                Check(session.Run.Tasks.Count == 3 && session.PlacedCount == 3, "all unique loads placed");
                Check(session.Run.Tasks.All(t => t.outcome == "completed" && t.TotalSeconds > 0), "all full cycles recorded");
                Check(Math.Abs(session.Run.Tasks.Sum(t => t.TotalSeconds) - session.Run.ActiveSeconds) < 0.05, "phase and run totals reconcile");
                Check(session.Logger.Healthy, "physical run CSV saved");
                Check(File.ReadAllLines(Path.Combine(session.DataPath, "tasks.csv")).Length == 4, "three task rows exported");
                session.SavePostRatings(1, 2, 1);
                if (condition < 2)
                {
                    condition++;
                    if (condition == 1) session.SelectLayout(session.layoutB);
                    else
                    {
                        var panel = FindObjectsByType<ResearchPanel>(FindObjectsSortMode.None).First(p => p.layoutC != null && p.GetComponentInParent<Canvas>().renderMode != RenderMode.WorldSpace);
                        panel.layoutC.onClick.Invoke();
                        Check(session.CurrentLayoutId == "C", "stack layout button bound at runtime");
                        Check(Mathf.Abs(session.loads[0].transform.position.y-session.loads[2].transform.position.y-2.6f)<.001f,"C1 starts two levels above C3");
                        Check(Vector3.ProjectOnPlane(session.loads[0].transform.position-session.loads[2].transform.position,Vector3.up).magnitude<.001f,"C sources are one vertical stack");
                    }
                    Check(session.PlacedCount == 0 && session.targets.All(t => !t.Completed), "reset clears placement state");
                    Check(session.BeginRun(true), "begin practice "+session.CurrentLayoutId); liftTask = -1; Time.timeScale = 6; return;
                }
                Check(File.ReadAllText(Path.Combine(session.DataPath,"setup.json")).Contains("\"layoutId\": \"C\""),"stack condition exported as C");
                session.conditionOrder="CAB";session.SelectNextLayout();Check(session.CurrentLayoutId=="A","next layout follows selected CAB order");
                session.SelectNextLayout();Check(session.CurrentLayoutId=="B","order continues to B");
                session.SelectNextLayout();Check(session.CurrentLayoutId=="C","order wraps to C");
                // A further run verifies that resetting saves a partial record instead of discarding it.
                session.ResetStation(); session.BeginRun(true); session.Abort("smoke_abort");
                Check(File.ReadAllText(Path.Combine(session.DataPath, "tasks.csv")).Contains("smoke_abort"), "aborted physical run preserved");
                Debug.Log("RESEARCH_PHYSICAL_SMOKE_PASSED: three layouts, nine actual lifts including top-down stack, release/dwell, return, pause/reset, CSV and abort.");
                Time.timeScale = 1; UnityEditor.EditorApplication.Exit(0); enabled = false; return;
            }
            Drive();
        }
        catch (Exception ex) { Fail(ex); }
    }
    private void Drive()
    {
        var crane = session.crane;
        var hook = session.hook;
        var load = session.ActiveLoad;
        var target = session.ActiveTarget;
        Vector3 desired;
        if (session.Run.Phase == ResearchPhase.Return)
        {
            int key = condition * 3 + session.Run.Current.task;
            if (verifiedReturnTask != key)
            {
                verifiedReturnTask = key;
                Check(!session.hook.ToggleAttachment(), "Space during return does not reattach or block progression");
                Check(session.ActiveLoad.IsPlaced, "return begins only after delivery accepted");
            }
            desired = session.homeMarker.position;
        }
        else if (hook.Attached == null && !load.IsPlaced)
        {
            if (session.Run.Phase == ResearchPhase.Pickup)
            {
                desired = load.LiftPoint - hook.couplingOffset;
                Vector3 horizontal = hook.transform.position - desired; horizontal.y = 0;
                // Travel over the pickup at clearance height before lowering.
                if (horizontal.magnitude > 0.5f) desired.y = 38.5f;
                else if (Vector3.Distance(hook.CouplingPoint, load.LiftPoint) < 0.6f && hook.GetComponent<Rigidbody>().linearVelocity.magnitude < 0.7f)
                {
                    if(session.CurrentLayoutId=="C")
                        foreach(var remaining in session.loads.Where(l=>l!=load && !l.IsPlaced))
                            Check(remaining.transform.position.y<load.transform.position.y-.8f,"stack unloads only the top remaining container");
                    hook.ToggleAttachment();
                }
            }
            else { crane.StopCommands(); return; } // Waiting for released load to satisfy the dwell.
        }
        else
        {
            if (liftTask != session.Run.Current.task)
            {
                desired = load.LiftPoint - hook.couplingOffset;
                desired.y = 38.5f;
                if (hook.transform.position.y >= 38f) liftTask = session.Run.Current.task;
            }
            else
            {
                desired = target.transform.position + Vector3.up * 0.95f;
                Vector3 horizontal = hook.transform.position - desired; horizontal.y = 0;
                if (horizontal.magnitude > 0.25f) desired.y = 38.5f;
                else if (load.transform.position.y <= target.transform.position.y + 0.12f && load.Body.linearVelocity.magnitude < 0.55f)
                {
                    Check(!target.ValidPlacement(load), "attached load rejected by validator");
                    hook.ToggleAttachment();
                }
            }
        }
        Vector3 from = crane.car.position - crane.craneRotationPivot.position;
        Vector3 to = desired - crane.craneRotationPivot.position; from.y = to.y = 0;
        float angularError = Vector3.SignedAngle(from, to, Vector3.up);
        float localX = crane.car.parent.InverseTransformPoint(desired).x;
        float trolleyError = crane.car.localPosition.x - localX;
        float ropeLength = crane.car.position.y - desired.y - 0.04f;
        crane.CraneRotation(Mathf.Clamp(angularError * 0.15f, -1, 1));
        crane.MoveCar(Mathf.Clamp(trolleyError * 12f, -1, 1));
        crane.MoveHookY(Mathf.Clamp((ropeLength - crane.hookRope.maxLength) * 1.2f, -1, 1));
    }
    private string Trace() => session == null || session.Run == null ? "waiting" :
        $"layout={session.CurrentLayoutId} task={session.Run.Current.task} phase={session.Run.Phase} hook={session.hook.transform.position} load={session.ActiveLoad.transform.position} attached={session.hook.Attached != null} stable={session.ActiveTarget.StableSeconds:F2}";
    private static void Check(bool condition, string message) { if (!condition) throw new Exception("RESEARCH_PHYSICAL_CHECK_FAILED: " + message); }
    private void Fail(Exception ex) { Debug.LogException(ex); Time.timeScale = 1; enabled = false; UnityEditor.EditorApplication.Exit(1); }
}
#endif
