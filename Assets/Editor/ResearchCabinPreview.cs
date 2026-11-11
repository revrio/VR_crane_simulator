#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class ResearchCabinPreview
{
    static double readyAt;
    static bool entered;
    static Texture2D waterBefore;
    static Camera harborCamera;
    public static void BuildAndRun() { ResearchCabinBuilder.Build(); Run(); }
    public static void Run()
    {
        ResearchViewValidation.Run();
        if (UnityEngine.XR.Management.XRGeneralSettings.Instance != null)
            UnityEngine.XR.Management.XRGeneralSettings.Instance.InitManagerOnStart = false;
        SessionState.SetBool("ResearchCabinPreviewPending", true);
        EditorApplication.EnterPlaymode();
    }
    [InitializeOnLoadMethod]
    static void Restore()
    {
        if (SessionState.GetBool("ResearchCabinPreviewPending", false)) EditorApplication.update += Tick;
    }
    static void Tick()
    {
        if (!EditorApplication.isPlaying || ExperimentSession.Instance == null) return;
        try
        {
            var view = UnityEngine.Object.FindFirstObjectByType<ResearchOperatorView>();
            if (!entered)
            {
                if (view.cabin.LoadTexture == null) return;
                view.EnterCabin(); entered = true; readyAt = EditorApplication.timeSinceStartup + 2; return;
            }
            if (EditorApplication.timeSinceStartup < readyAt) return;
            if (waterBefore != null)
            {
                var after = CaptureHarbor();
                var a = waterBefore.GetPixels32(); var b = after.GetPixels32();
                int movingPixels = 0;
                // Bottom-middle of this locked camera is open water, outside the tug route.
                for (int y=30;y<230;y++) for(int x=250;x<750;x++)
                {
                    int i=y*1024+x;
                    if(Math.Abs(a[i].r-b[i].r)+Math.Abs(a[i].g-b[i].g)+Math.Abs(a[i].b-b[i].b)>6) movingPixels++;
                }
                ResearchValidation.Check(movingPixels>1000,"water visibly changes between rendered frames: "+movingPixels);
                File.WriteAllBytes("docs/images/harbor-atmosphere.png",after.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(waterBefore); UnityEngine.Object.DestroyImmediate(after);
                Debug.Log("RESEARCH_HARBOR_ANIMATION_PREVIEW_PASSED");
                SessionState.SetBool("ResearchCabinPreviewPending", false);
                EditorApplication.update -= Tick;
                EditorApplication.Exit(0);
                return;
            }
            ResearchValidation.Check(view.cabin.loadCamera.enabled, "live load monitor visible");
            ResearchValidation.Check(view.cabin.transform.Find("Cabin instrument panel") == null, "duplicate cabin menu removed");
            ResearchValidation.Check(view.desktopPanel.enabled && !view.worldPanel.enabled, "single desktop sidebar remains available");
            var npc = UnityEngine.Object.FindFirstObjectByType<ResearchInstructor>();
            var face = view.desktopCamera.WorldToViewportPoint(npc.head.position);
            ResearchValidation.Check(face.z > 0 && face.x > 0 && face.x < 1 && face.y > 0 && face.y < 1, "Maya visible from operator seat");
            ResearchPreviewCapture.CaptureCurrent();
            File.Copy("docs/images/research-station.png", "docs/images/operator-cabin.png", true);
            Debug.Log("RESEARCH_CABIN_NPC_PREVIEW_PASSED");
            ResearchPortDetailPreview.CheckAndCapture(view);
            ResearchValidation.Check(Shader.Find("PortLab/Harbor Water").isSupported && Shader.Find("PortLab/Harbor Sky").isSupported,"harbor shaders supported");
            var motion=UnityEngine.Object.FindFirstObjectByType<ResearchHarborMotion>();
            ResearchValidation.Check(motion!=null && motion.GetComponentsInChildren<Collider>().Length==0,"harbor scenery has no physics colliders");
            harborCamera=new GameObject("Harbor visual check camera").AddComponent<Camera>();harborCamera.enabled=false;
            harborCamera.fieldOfView=60;harborCamera.farClipPlane=1200;harborCamera.cullingMask=~((1<<28)|(1<<29)|(1<<30));
            var p=ExperimentSession.Instance.crane.craneRotationPivot.position;
            harborCamera.transform.position=p+motion.routeForward*45+Vector3.up*10;
            harborCamera.transform.LookAt(p+motion.routeForward*220+Vector3.down*5);
            waterBefore=CaptureHarbor();readyAt=EditorApplication.timeSinceStartup+3;
        }
        catch (Exception e)
        {
            Debug.LogException(e); SessionState.SetBool("ResearchCabinPreviewPending", false); EditorApplication.Exit(1);
        }
    }
    static Texture2D CaptureHarbor()
    {
        var target=new RenderTexture(1024,576,24);target.Create();
        UnityEngine.Rendering.RenderPipeline.SubmitRenderRequest(harborCamera,new UnityEngine.Rendering.Universal.UniversalRenderPipeline.SingleCameraRequest {destination=target});
        var previous=RenderTexture.active;RenderTexture.active=target;
        var image=new Texture2D(1024,576,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1024,576),0,0);image.Apply();
        RenderTexture.active=previous;target.Release();UnityEngine.Object.DestroyImmediate(target);return image;
    }
}
#endif
