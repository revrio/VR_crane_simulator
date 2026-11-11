#if UNITY_EDITOR
using System.IO;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class ResearchPreviewCapture
{
    public static void BuildAndCapture()
    {
        ResearchDemoBuilder.Build();
        Run();
        ResearchBuilds.Linux();
    }
    public static void Run()
    {
        EditorSceneManager.OpenScene(ResearchDemoBuilder.ScenePath);
        CaptureCurrent();
    }
    public static void CaptureCurrent()
    {
        var view = Object.FindFirstObjectByType<ResearchOperatorView>();
        var camera = view.desktopCamera;
        var previousRect = camera.rect;
        var previousMode = view.desktopPanel.renderMode;
        float previousPlane = view.desktopPanel.planeDistance;
        bool worldActive = view.worldPanel.gameObject.activeSelf;
        var previousPosition = camera.transform.position;
        var previousRotation = camera.transform.rotation;
        if (!Application.isPlaying)
        {
            var session = Object.FindFirstObjectByType<ExperimentSession>();
            view.GetDesktopPose(session.crane, view.FramingPoint(session, session.hook.transform.position), 31.2f, out var position, out var rotation);
            camera.transform.SetPositionAndRotation(position, rotation);
        }
        var guide = Object.FindFirstObjectByType<ResearchGuide>();
        if (guide != null && guide.portraitCamera != null && guide.portraitCamera.targetTexture != null)
            RenderPipeline.SubmitRenderRequest(guide.portraitCamera, new UniversalRenderPipeline.SingleCameraRequest { destination = guide.portraitCamera.targetTexture });
        var cabin = Object.FindFirstObjectByType<ResearchCabin>();
        if (cabin != null && cabin.LoadTexture != null)
        {
            bool enabled = cabin.loadCamera.enabled; cabin.loadCamera.enabled = true;
            RenderPipeline.SubmitRenderRequest(cabin.loadCamera, new UniversalRenderPipeline.SingleCameraRequest { destination = cabin.LoadTexture });
            cabin.loadCamera.enabled = enabled;
        }
        camera.rect = new Rect(0, 0, 1, 1);
        view.worldPanel.gameObject.SetActive(false);
        view.desktopPanel.renderMode = RenderMode.ScreenSpaceCamera;
        view.desktopPanel.worldCamera = camera;
        view.desktopPanel.planeDistance = camera.nearClipPlane + .01f;
        var target = new RenderTexture(1920, 1080, 24, RenderTextureFormat.ARGB32);
        target.Create();
        camera.targetTexture = target;
        Canvas.ForceUpdateCanvases();
        foreach (var text in view.desktopPanel.GetComponentsInChildren<TMPro.TextMeshProUGUI>()) text.ForceMeshUpdate(true, true);
        Canvas.ForceUpdateCanvases();
        var request = new UniversalRenderPipeline.SingleCameraRequest { destination = target };
        RenderPipeline.SubmitRenderRequest(camera, request);
        var previous = RenderTexture.active; RenderTexture.active = target;
        var image = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
        image.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0); image.Apply();
        Directory.CreateDirectory("docs/images");
        File.WriteAllBytes("docs/images/research-station.png", image.EncodeToPNG());
        RenderTexture.active = previous; target.Release();
        camera.targetTexture = null;
        camera.rect = previousRect;
        camera.transform.SetPositionAndRotation(previousPosition, previousRotation);
        view.desktopPanel.renderMode = previousMode;
        view.desktopPanel.planeDistance = previousPlane;
        view.worldPanel.gameObject.SetActive(worldActive);
        Object.DestroyImmediate(image); Object.DestroyImmediate(target);
        Debug.Log("RESEARCH_PREVIEW_CAPTURED");
    }
}
#endif
