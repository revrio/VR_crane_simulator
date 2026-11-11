#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class OperationalPortIterationThreeValidation
{
    public static void BuildAndRun(){OperationalPortBuilder.Build();Run();}
    public static void Run()
    {
        EditorSceneManager.OpenScene(OperationalPortBuilder.ScenePath);var d=UnityEngine.Object.FindFirstObjectByType<PortDirector>();
        ResearchValidation.Check(d.config.operationalTraffic&&d.allCargo.Length==5,"five-box operational arena");
        ResearchValidation.Check(d.traffic.Workers.Length==4&&d.traffic.trucks.Length==2&&d.traffic.backgroundCranes.Length==2,"complete worker/traffic/crane set");
        ResearchValidation.Check(d.crane.hookBody.GetComponents<Collider>().Length>=5,"full width physical spreader");
        ResearchValidation.Check(d.traffic.Workers.All(w=>w.shape!=null&&w.body.isKinematic),"physical non-ragdoll worker obstacles");
        ResearchValidation.Check(d.safety.obstacles.Any(o=>o.Id=="INSPECTION-BARRIER"),"inspectable physical barrier");
        var maya=UnityEngine.Object.FindFirstObjectByType<PortMaya>();ResearchValidation.Check(maya.voiceClips.All(c=>c!=null),"all guidance clips resolve");
        foreach(PortLayout layout in Enum.GetValues(typeof(PortLayout)))ResearchValidation.Check(PortShipmentPlan.Create(d.config,layout).Validate(d.config)==null,"layout reachable: "+layout);
        using(var log=new PortRunRecorder("/tmp/port-iteration3-clock","test",true,10,"Desktop",5,"A","Staged","ControlledCrossing"))
        {
            var p=PortShipmentPlan.Create(d.config,PortLayout.Accessible);log.BeginMove(p.moves[0],p.Initial("C1"),p.Slot("SHIP-01"));log.Phase="Transfer";log.DelayTag="ProtectedStop";log.Advance(12);
            log.Paused=true;log.Advance(100);log.Paused=false;log.DelayTag="CrossingWait";log.Advance(103);
            ResearchValidation.Check(Math.Abs(log.Seconds-5)<.001&&Math.Abs(log.Delays.Values.Sum()-5)<.001&&Math.Abs(log.CurrentMove.Total-5)<.001,"pause-excluding delay subsets reconcile");
        }
        if(UnityEngine.XR.Management.XRGeneralSettings.Instance!=null)UnityEngine.XR.Management.XRGeneralSettings.Instance.InitManagerOnStart=false;
        SessionState.SetBool("PortIterationThreeTest",true);EditorApplication.EnterPlaymode();
    }
    [InitializeOnLoadMethod] static void Restore(){if(SessionState.GetBool("PortIterationThreeTest",false))EditorApplication.update+=StartDriver;}
    static void StartDriver()
    {
        if(!EditorApplication.isPlaying||UnityEngine.Object.FindFirstObjectByType<PortDirector>()==null)return;
        SessionState.SetBool("PortIterationThreeTest",false);EditorApplication.update-=StartDriver;
        if(Array.Exists(Environment.GetCommandLineArgs(),a=>a=="-port3contacts"))new GameObject("Physical contact verification").AddComponent<PortContactSmoke>();
        else new GameObject("Traffic and operational verification").AddComponent<PortIterationThreeSmoke>();
    }
}
#endif
