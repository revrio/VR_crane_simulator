#if UNITY_EDITOR
using System;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ResearchValidation
{
    public static void PureChecks()
    {
        var run = new ResearchRun(100);
        run.BeginTask("C1", "BAY1", 100);
        run.Advance(110);
        Check(run.SetPhase(ResearchPhase.Transfer, 110), "pickup transition");
        Check(!run.SetPhase(ResearchPhase.Return, 111), "cannot skip placement");
        run.Pause(115); run.Advance(200); run.Resume(205); run.Advance(210);
        Check(Math.Abs(run.ActiveSeconds - 20) < 1e-8, "paused time excluded");
        run.SetPhase(ResearchPhase.Placement, 210); run.Advance(214);
        Check(!run.CompleteTask(214), "cannot finish before return");
        run.SetPhase(ResearchPhase.Return, 214); run.CompleteTask(220); run.Finish(220);
        Check(run.State == ResearchRunState.Completed && Math.Abs(run.Current.TotalSeconds - 30) < 1e-8, "exclusive phase accounting");
        Check(!run.CompleteTask(222), "completion deduplicated");
        var chained = new ResearchRun(0); chained.BeginTask("C1", "BAY1", 0);
        chained.SetPhase(ResearchPhase.Transfer, 2); chained.SetPhase(ResearchPhase.Placement, 4);
        chained.SetPhase(ResearchPhase.Return, 6); chained.CompleteTask(10); chained.Advance(20);
        chained.BeginTask("C2", "BAY2", 30); chained.Advance(35);
        Check(chained.ActiveSeconds == 15, "between-job checkpoint time is not assigned to a work element");
        var partial = new ResearchRun(0); partial.BeginTask("C2", "BAY2", 0); partial.Abort(4, "timeout");
        Check(partial.Current.outcome == "timeout" && partial.ActiveSeconds == 4, "partial timeout preserved");
        partial.Advance(10); Check(partial.ActiveSeconds == 4, "finished clock stops");
        CultureInfo previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
            Check(ResearchCsv.Cell(1.25) == "1.25", "invariant decimals");
            Check(ResearchCsv.Cell("A,\"B\"") == "\"A,\"\"B\"\"\"", "CSV quoting");
            Check(ResearchCsv.Cell("=SUM(A1)").StartsWith("\t"), "spreadsheet-safe participant IDs");
            using (var csv = new ResearchCsv("/tmp/vr-research-validation", partial, "P,01", "B", "BA", true, "automated test", "test"))
            {
                csv.Event(partial, "abort", "C2"); csv.Checkpoint(partial); csv.Checkpoint(partial);
                Check(csv.Healthy, "CSV export successful");
                Check(File.ReadAllLines(Path.Combine(csv.DirectoryPath, "tasks.csv")).Length == 2, "checkpoint replaces rather than duplicates rows");
                Check(File.ReadAllText(Path.Combine(csv.DirectoryPath, "tasks.csv")).Contains("timeout"), "partial task exported");
            }
            using (var csv = new ResearchCsv("/proc/not-a-writable-research-folder", partial, "test", "A", "AB", true, "test", "test"))
                Check(!csv.Healthy && !string.IsNullOrEmpty(csv.Error), "export failure surfaced");
        }
        finally { CultureInfo.CurrentCulture = previous; }
        float slow = SlewAtStep(1f / 30f, 300);
        float fast = SlewAtStep(1f / 90f, 900);
        Check(Mathf.Abs(slow - fast) < 0.2f, "slew stable across timestep rates");
        Debug.Log("RESEARCH_PURE_CHECKS_PASSED");
    }

    private static float SlewAtStep(float step, int ticks)
    {
        var obj = new GameObject("slew regression check"); var crane = obj.AddComponent<CraneController>();
        crane.craneRotationPivot = obj.transform; crane.rotationSpeed = 12; crane.rotationSmoothness = 4;
        crane.CraneRotation(0.5f);
        for (int i = 0; i < ticks; i++) crane.Simulate(step);
        float result = crane.AbsoluteSlewDegrees;
        UnityEngine.Object.DestroyImmediate(obj); return result;
    }
    public static void Check(bool condition, string message)
    { if (!condition) throw new Exception("RESEARCH_CHECK_FAILED: " + message); }

    public static void Run()
    {
        try
        {
            PureChecks();
            EditorSceneManager.OpenScene(ResearchDemoBuilder.ScenePath);
            // The test uses keyboard controls. This installed OpenXR package does not support Linux.
            // Change only the in-memory test settings, leaving the lab's Windows XR asset unchanged.
            if (Application.platform == RuntimePlatform.LinuxEditor && UnityEngine.XR.Management.XRGeneralSettings.Instance != null)
            {
                SessionState.SetBool("ResearchOriginalXrInit", UnityEngine.XR.Management.XRGeneralSettings.Instance.InitManagerOnStart);
                UnityEngine.XR.Management.XRGeneralSettings.Instance.InitManagerOnStart = false;
            }
            SessionState.SetBool("ResearchSmokePending", true);
            EditorApplication.EnterPlaymode();
        }
        catch (Exception ex) { Debug.LogException(ex); EditorApplication.Exit(1); }
    }
    [InitializeOnLoadMethod]
    private static void ResumeAfterReload()
    {
        EditorApplication.update -= StartDriver;
        if (SessionState.GetBool("ResearchSmokePending", false)) EditorApplication.update += StartDriver;
    }
    private static void StartDriver()
    {
        if (!EditorApplication.isPlaying || ExperimentSession.Instance == null) return;
        EditorApplication.update -= StartDriver;
        SessionState.SetBool("ResearchSmokePending", false);
        new GameObject("Automated research verification").AddComponent<ResearchSmokeDriver>();
    }
}
#endif
