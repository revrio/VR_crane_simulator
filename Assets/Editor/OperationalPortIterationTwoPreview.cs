#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class OperationalPortIterationTwoPreview
{
    static int step;
    static double ready;
    public static void BuildAndRun(){OperationalPortBuilder.Build();Run();}
    public static void Run()
    {
        EditorSceneManager.OpenScene(OperationalPortBuilder.ScenePath);
        if(UnityEngine.XR.Management.XRGeneralSettings.Instance!=null)UnityEngine.XR.Management.XRGeneralSettings.Instance.InitManagerOnStart=false;
        SessionState.SetBool("PortOperationalTwoPreview",true);EditorApplication.EnterPlaymode();
    }
    [InitializeOnLoadMethod] static void Restore(){if(SessionState.GetBool("PortOperationalTwoPreview",false))EditorApplication.update+=Tick;}
    static void Tick()
    {
        if(!EditorApplication.isPlaying)return;
        var d=UnityEngine.Object.FindFirstObjectByType<PortDirector>();if(d==null)return;
        try
        {
            if(step==0){d.SelectLayout(PortLayout.Accessible);d.view.Select(0);ready=EditorApplication.timeSinceStartup+.6;step=1;return;}
            if(EditorApplication.timeSinceStartup<ready)return;
            if(step<=4)
            {
                OperationalPortValidation.Capture("operational-i2-layout-"+d.Plan.layoutId);
                ResearchValidation.Check(d.Cargoes.Length==5&&d.Cargoes.AllActive(),"all five source containers are visible");
                if(step<4){d.SelectLayout((PortLayout)step);d.view.Select(0);}
                else{d.SelectLayout(PortLayout.Stacked);d.view.Cabin();}
                ready=EditorApplication.timeSinceStartup+.6;step++;return;
            }
            var maya=UnityEngine.Object.FindFirstObjectByType<PortMaya>();var point=d.view.cameraMain.WorldToViewportPoint(maya.head.position);
            ResearchValidation.Check(point.z>0&&point.x>0&&point.x<1&&point.y>0&&point.y<1,"Maya visible from operational cabin");
            ResearchValidation.Check(d.view.loadCamera.enabled&&d.view.loadCamera.targetTexture!=null,"cabin retains live load feed");
            RenderPipeline.SubmitRenderRequest(d.view.loadCamera,new UniversalRenderPipeline.SingleCameraRequest{destination=d.view.loadCamera.targetTexture});
            OperationalPortValidation.Capture("operational-i2-cabin");
            Debug.Log("OPERATIONAL_ITERATION2_PREVIEW_PASSED four layouts, readable console, cabin Maya and live feed.");
            SessionState.SetBool("PortOperationalTwoPreview",false);EditorApplication.update-=Tick;EditorApplication.Exit(0);
        }
        catch(Exception e){Debug.LogException(e);SessionState.SetBool("PortOperationalTwoPreview",false);EditorApplication.Exit(1);}
    }
    static bool AllActive(this PortCargo[] cargo){foreach(var item in cargo)if(!item.gameObject.activeSelf)return false;return true;}
}
#endif
