#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class OperationalPortIterationThreePreview
{
    static int step;
    static double ready;
    static Vector3 worker,truck;
    public static void BuildAndRun(){OperationalPortBuilder.Build();Run();}
    public static void Run()
    {
        EditorSceneManager.OpenScene(OperationalPortBuilder.ScenePath);
        if(UnityEngine.XR.Management.XRGeneralSettings.Instance!=null)UnityEngine.XR.Management.XRGeneralSettings.Instance.InitManagerOnStart=false;
        SessionState.SetBool("PortThreePreview",true);EditorApplication.EnterPlaymode();
    }
    [InitializeOnLoadMethod] static void Restore(){if(SessionState.GetBool("PortThreePreview",false))EditorApplication.update+=Tick;}
    static void Tick()
    {
        if(!EditorApplication.isPlaying)return;var d=UnityEngine.Object.FindFirstObjectByType<PortDirector>();if(d==null)return;
        try
        {
            if(step==0)
            {d.outputOverride="/tmp/port-iteration3-preview";d.automated=true;d.SelectLayout(PortLayout.Stacked);d.BeginFullIntake();d.ResetOperation();d.view.Select(4);ready=EditorApplication.timeSinceStartup+1;step=1;return;}
            if(EditorApplication.timeSinceStartup<ready)return;
            if(step==1)
            {
                ResearchValidation.Check(d.voice.clip!=null&&d.voice.clip.name=="welcome"&&d.voice.isPlaying,"offline operational welcome plays");
                d.view.Select(4);OperationalPortValidation.Capture("operational-i3-ready");d.Begin();d.view.Select(4);ready=EditorApplication.timeSinceStartup+5;step=2;return;
            }
            if(step==2)
            {
                ResearchValidation.Check(d.traffic.east.Travelled>2&&d.traffic.trucks[0].Distance>10&&d.traffic.backgroundCranes[0].MotionSeconds>4,"arena actors visibly animate");
                foreach(var actor in d.traffic.trucks){var p=d.view.cameraMain.WorldToViewportPoint(actor.body.position+Vector3.up*2);ResearchValidation.Check(p.z>0&&p.x>0&&p.x<1&&p.y>0&&p.y<1,"overview shows rear truck "+actor.vehicleId);}
                OperationalPortValidation.Capture("operational-i3-arena");d.view.Select(0);ready=EditorApplication.timeSinceStartup+.6;step=3;return;
            }
            if(step==3){OperationalPortValidation.Capture("operational-i3-terminal");d.view.Select(2);ready=EditorApplication.timeSinceStartup+.6;step=4;return;}
            if(step==4){OperationalPortValidation.Capture("operational-i3-overhead");d.view.Cabin();ready=EditorApplication.timeSinceStartup+.8;step=5;return;}
            if(step==5)
            {
                var maya=UnityEngine.Object.FindFirstObjectByType<PortMaya>();var p=d.view.cameraMain.WorldToViewportPoint(maya.head.position);
                ResearchValidation.Check(p.z>0&&p.x>0&&p.x<1&&p.y>0&&p.y<1,"Maya remains visible in cabin");
                ResearchValidation.Check(d.view.loadCamera.enabled,"cabin live feed retained");
                RenderPipeline.SubmitRenderRequest(d.view.loadCamera,new UniversalRenderPipeline.SingleCameraRequest{destination=d.view.loadCamera.targetTexture});
                OperationalPortValidation.Capture("operational-i3-cabin");worker=d.traffic.east.body.position;truck=d.traffic.trucks[0].body.position;ready=EditorApplication.timeSinceStartup+.9;step=6;return;
            }
            if(step==6)
            {
                ResearchValidation.Check(Vector3.Distance(worker,d.traffic.east.body.position)>.002f,"workers keep moving during operation");
                OperationalPortValidation.Capture("operational-i3-contact-flow");ready=EditorApplication.timeSinceStartup+.9;step=7;return;
            }
            if(step==7)
            {
                
                Vector3 point=d.crane.hookBody.position;d.safety.feedback.Impact(point,true);
                ResearchValidation.Check(d.safety.feedback.dust.isPlaying&&d.safety.feedback.dust.particleCount>0,"contact flecks actually emit and simulate");
                var particles=new ParticleSystem.Particle[96];int count=d.safety.feedback.dust.GetParticles(particles);
                ResearchValidation.Check(count>0&&Vector3.Distance(particles[0].position,point)<.5f,"effect originates at contact point");
                d.ResetOperation();Debug.Log("OPERATIONAL_ITERATION3_PREVIEW_PASSED five views, moving actors, readable console, cabin/live feed, speech and contact-local particles.");
                SessionState.SetBool("PortThreePreview",false);EditorApplication.update-=Tick;EditorApplication.Exit(0);
            }
        }catch(Exception e){Debug.LogException(e);SessionState.SetBool("PortThreePreview",false);EditorApplication.Exit(1);}
    }
}
#endif
