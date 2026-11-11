#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class OperationalPortValidation
{
    public static void BuildAndRun(){OperationalPortBuilder.Build();OperationalPortIterationTwoValidation.Run();}
    public static void Run()=>RunScene("Assets/Scenes/OperationalPortIteration1.unity");
    public static void RunScene(string scene)
    {
        // Real-time phase clock: excluded pause and monotonic input, independent of Unity timeScale.
        using(var run=new PortRunRecorder("/tmp/port-operational-clock-check","test",true,10))
        {
            run.Advance(12);run.Paused=true;run.Advance(100);run.Paused=false;run.Phase="Transfer";run.Advance(103);run.Advance(102);
            ResearchValidation.Check(Math.Abs(run.Seconds-5)<.0001&&Math.Abs(run.Phases.Values.Sum()-5)<.0001,"operational clock reconciles and excludes pause");
        }
        EditorSceneManager.OpenScene(scene);
        var director=UnityEngine.Object.FindFirstObjectByType<PortDirector>();
        ResearchValidation.Check(director.config.yard.x>=director.config.gantryLimits.x&&director.config.ship.z<=director.config.trolleyLimits.y,"source and ship are inside crane reach");
        ResearchValidation.Check(UnityEngine.Object.FindObjectsByType<PortCargoContact>(FindObjectsSortMode.None).Length==1,"exactly one active consignment");
        ResearchValidation.Check(UnityEngine.Object.FindFirstObjectByType<ExperimentSession>()==null,"operational scene independent of legacy research singleton");
        if(UnityEngine.XR.Management.XRGeneralSettings.Instance!=null)UnityEngine.XR.Management.XRGeneralSettings.Instance.InitManagerOnStart=false;
        SessionState.SetBool("PortIterationOneTest",true);EditorApplication.EnterPlaymode();
    }
    [InitializeOnLoadMethod]
    static void Restore(){if(SessionState.GetBool("PortIterationOneTest",false))EditorApplication.update+=StartDriver;}
    static void StartDriver()
    {
        if(!EditorApplication.isPlaying||UnityEngine.Object.FindFirstObjectByType<PortDirector>()==null)return;
        SessionState.SetBool("PortIterationOneTest",false);EditorApplication.update-=StartDriver;
        new GameObject("Operational iteration one verification").AddComponent<PortIterationOneSmoke>();
    }
    public static void Capture(string name)
    {
        var view=UnityEngine.Object.FindFirstObjectByType<PortView>();var camera=view.cameraMain;
        var oldRect=camera.rect;var mode=view.desktopPanel.renderMode;var previousCamera=view.desktopPanel.worldCamera;float plane=view.desktopPanel.planeDistance;
        var target=new RenderTexture(1920,1080,24);target.Create();camera.rect=new Rect(0,0,1,1);camera.targetTexture=target;
        view.desktopPanel.renderMode=RenderMode.ScreenSpaceCamera;view.desktopPanel.worldCamera=camera;view.desktopPanel.planeDistance=camera.nearClipPlane+.01f;
        Canvas.ForceUpdateCanvases();foreach(var text in view.desktopPanel.GetComponentsInChildren<TMPro.TextMeshProUGUI>())text.ForceMeshUpdate();
        RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=target});
        var prev=RenderTexture.active;RenderTexture.active=target;var image=new Texture2D(1920,1080,TextureFormat.RGB24,false);
        image.ReadPixels(new Rect(0,0,1920,1080),0,0);image.Apply();Directory.CreateDirectory("docs/images");File.WriteAllBytes("docs/images/"+name+".png",image.EncodeToPNG());
        RenderTexture.active=prev;target.Release();UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(target);
        camera.targetTexture=null;camera.rect=oldRect;view.desktopPanel.renderMode=mode;view.desktopPanel.worldCamera=previousCamera;view.desktopPanel.planeDistance=plane;
    }
}
#endif
