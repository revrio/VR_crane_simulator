#if UNITY_EDITOR
using System;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ResearchViewValidation
{
    public static void Run()
    {
        EditorSceneManager.OpenScene(ResearchDemoBuilder.ScenePath);
        var session = UnityEngine.Object.FindFirstObjectByType<ExperimentSession>();
        var view = UnityEngine.Object.FindFirstObjectByType<ResearchOperatorView>();
        var crane = session.crane;
        Quaternion originalHeading = crane.craneRotationPivot.rotation;
        Vector3 carLocal = crane.car.localPosition;
        Vector3 hookOffset = session.hook.transform.position - crane.car.position;
        var camera = view.desktopCamera;
        try
        {
            foreach (float heading in new[] { 0f, 90f, 180f, 270f })
            {
                crane.craneRotationPivot.rotation = Quaternion.AngleAxis(heading, Vector3.up) * originalHeading;
                view.SelectDesktopView(0);
                if (view.cabin != null) view.cabin.SyncFrame();
                for (int mode = 0; mode < 4; mode++)
                {
                    crane.car.localPosition = carLocal;
                    Vector3 probe = crane.car.position + hookOffset;
                    view.GetDesktopPose(crane, view.FramingPoint(session, probe), 31.2f, out var position, out var rotation);
                    camera.transform.SetPositionAndRotation(position, rotation);
                    Vector3 before = camera.WorldToViewportPoint(probe);
                    if (heading == 0 && mode == 0)
                    {
                        var pickup = camera.WorldToViewportPoint(session.loads[0].transform.position);
                        ResearchValidation.Check(pickup.z > 0 && pickup.x > .05f && pickup.x < .95f && pickup.y > .05f && pickup.y < .95f,
                            "first pickup is visible from the cabin side");
                    }
                    // Project actual controller movement against a fixed camera pose.
                    // A tracking camera recenters the hook; the yard moves in the opposite direction.
                    foreach (float sign in new[] { -1f, 1f })
                    {
                        crane.car.localPosition = carLocal;
                        crane.craneRotationPivot.rotation = Quaternion.AngleAxis(heading, Vector3.up) * originalHeading;
                        crane.StopCommands(); crane.MoveCar(sign); crane.Simulate(.2f);
                        var after = camera.WorldToViewportPoint(crane.car.position + hookOffset);
                        ResearchValidation.Check((after.y - before.y) * sign > .0001f, $"W outward/up, S inward/down: heading {heading}, view {mode}");
                        crane.car.localPosition = carLocal;
                        crane.StopCommands(); crane.CraneRotation(sign); crane.Simulate(.2f);
                        after = camera.WorldToViewportPoint(crane.car.position + hookOffset);
                        ResearchValidation.Check((after.x - before.x) * sign > .0001f, $"D right, A left: heading {heading}, view {mode}");
                        crane.craneRotationPivot.rotation = Quaternion.AngleAxis(heading, Vector3.up) * originalHeading;
                    }
                    view.ToggleDesktopView();
                }
            }
            Debug.Log("RESEARCH_CABIN_VIEW_CHECKS_PASSED: actual W/S and A/D motion, all four views, four crane headings.");
        }
        finally
        {
            crane.craneRotationPivot.rotation = originalHeading;
            crane.car.localPosition = carLocal;
            crane.ResetTravel();
        }
    }
    public static void ValidateAndBuild() { Run(); ResearchBuilds.Linux(); }
}
#endif
