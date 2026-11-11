#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class OperationalPortPreview
{
    static int step;
    static double ready;
    public static void BuildAndRun(){OperationalPortBuilder.Build();Run();}
    public static void Run()
    {
        EditorSceneManager.OpenScene(OperationalPortBuilder.ScenePath);
        if(UnityEngine.XR.Management.XRGeneralSettings.Instance!=null)UnityEngine.XR.Management.XRGeneralSettings.Instance.InitManagerOnStart=false;
        SessionState.SetBool("PortOperationalPreview",true);EditorApplication.EnterPlaymode();
    }
    [InitializeOnLoadMethod]
    static void Restore(){if(SessionState.GetBool("PortOperationalPreview",false))EditorApplication.update+=Tick;}
    static void Tick()
    {
        if(!EditorApplication.isPlaying)return;
        var d=UnityEngine.Object.FindFirstObjectByType<PortDirector>();if(d==null)return;
        try
        {
            if(step==0){d.view.Cabin();ready=EditorApplication.timeSinceStartup+1;step=1;return;}
            if(EditorApplication.timeSinceStartup<ready)return;
            if(step==1)
            {
                var maya=UnityEngine.Object.FindFirstObjectByType<PortMaya>();var point=d.view.cameraMain.WorldToViewportPoint(maya.head.position);
                ResearchValidation.Check(point.z>0&&point.x>0&&point.x<1&&point.y>0&&point.y<1,"Maya visible in operational cabin");
                ResearchValidation.Check(d.view.loadCamera.enabled&&d.view.loadCamera.targetTexture!=null,"operational cabin live feed");
                RenderPipeline.SubmitRenderRequest(d.view.loadCamera,new UniversalRenderPipeline.SingleCameraRequest{destination=d.view.loadCamera.targetTexture});
                OperationalPortValidation.Capture("operational-cabin");d.view.Select(0);ready=EditorApplication.timeSinceStartup+.5;step=2;return;
            }
            OperationalPortValidation.Capture("operational-ready");
            Debug.Log("OPERATIONAL_ITERATION1_CABIN_PREVIEW_PASSED");SessionState.SetBool("PortOperationalPreview",false);EditorApplication.update-=Tick;EditorApplication.Exit(0);
        }
        catch(Exception e){Debug.LogException(e);SessionState.SetBool("PortOperationalPreview",false);EditorApplication.Exit(1);}
    }
}
#endif
