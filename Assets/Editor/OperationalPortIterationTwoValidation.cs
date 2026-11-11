#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class OperationalPortIterationTwoValidation
{
    public static void Run()
    {
        EditorSceneManager.OpenScene(OperationalPortBuilder.ScenePath);
        var d=UnityEngine.Object.FindFirstObjectByType<PortDirector>();
        ResearchValidation.Check(d.allCargo.Length==5,"five distinct shipment containers");
        foreach(PortLayout layout in Enum.GetValues(typeof(PortLayout)))
            ResearchValidation.Check(PortShipmentPlan.Create(d.config,layout).Validate(d.config)==null,"reachable and supported move graph: "+layout);
        var broken=PortShipmentPlan.Create(d.config,PortLayout.Priority);broken.moves=broken.moves.Skip(1).ToArray();
        ResearchValidation.Check(broken.Validate(d.config)?.Contains("Blocked")==true,"priority retrieval requires blocker rehandle");
        broken=PortShipmentPlan.Create(d.config,PortLayout.Accessible);
        var temp=broken.moves[0];broken.moves[0]=broken.moves[3];broken.moves[3]=temp;
        ResearchValidation.Check(broken.Validate(d.config)?.Contains("Support")==true,"upper-tier destination needs its bottom container first");
        using(var log=new PortRunRecorder("/tmp/port-iteration2-clock","test",true,10,"Desktop",5,"C","Staged"))
        {
            var plan=PortShipmentPlan.Create(d.config,PortLayout.Priority);
            log.BeginMove(plan.moves[0],plan.Initial("C2"),plan.Slot("TEMP-01"));log.Phase="Pickup";log.Advance(12);
            log.Paused=true;log.Advance(99);log.Paused=false;log.Phase="Transfer";log.Advance(102);log.Advance(101);
            ResearchValidation.Check(Math.Abs(log.Seconds-5)<.00001&&Math.Abs(log.CurrentMove.Total-5)<.00001,"move clock excludes pause and rejects backward time");
        }
        if(UnityEngine.XR.Management.XRGeneralSettings.Instance!=null)UnityEngine.XR.Management.XRGeneralSettings.Instance.InitManagerOnStart=false;
        SessionState.SetBool("PortIterationTwoTest",true);EditorApplication.EnterPlaymode();
    }
    [InitializeOnLoadMethod] static void Restore(){if(SessionState.GetBool("PortIterationTwoTest",false))EditorApplication.update+=StartDriver;}
    static void StartDriver()
    {
        if(!EditorApplication.isPlaying||UnityEngine.Object.FindFirstObjectByType<PortDirector>()==null)return;
        SessionState.SetBool("PortIterationTwoTest",false);EditorApplication.update-=StartDriver;
        new GameObject("Five container physical verification").AddComponent<PortIterationTwoSmoke>();
    }
}
#endif
