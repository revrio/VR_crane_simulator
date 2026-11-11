#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEngine;

public class PortIterationThreeSmoke : MonoBehaviour
{
    PortDirector d;
    string kind;
    int run,previousMove=-1,moves;
    int[] identities;
    double started;
    bool lifted;
    PortStage previous;
    float traceAt;
    void Start()
    {
        try
        {
            d=FindFirstObjectByType<PortDirector>();FindFirstObjectByType<PortInput>().disabledForTest=true;
            d.outputOverride="/tmp/port-iteration3-physical";d.automated=true;d.VoiceMuted=true;
            identities=d.Cargoes.Select(c=>c.body.GetInstanceID()).ToArray();
            kind=Environment.GetCommandLineArgs().FirstOrDefault(a=>a.StartsWith("-port3case="))?.Substring(11)??"core";
            StartRun();
        }catch(Exception e){Fail(e);}
    }
    void StartRun()
    {
        var mode=kind=="drill"?PortTrafficMode.IntrusionDrill:kind=="separated"?PortTrafficMode.Separated:PortTrafficMode.ControlledCrossing;
        d.traffic.SelectMode(mode);d.SelectLayout(kind=="core"?(PortLayout)run:kind=="full"?PortLayout.Stacked:kind=="separated"?PortLayout.Priority:PortLayout.Accessible);
        started=Time.realtimeSinceStartupAsDouble;previousMove=-1;lifted=false;
        if(kind=="full")d.BeginFullIntake();else d.Begin();Time.timeScale=5;
        Check(!d.traffic.SelectMode(PortTrafficMode.Separated),"traffic policy locked during active run");
        Check(d.Log.Schema=="op3","new scene uses op3 exports");
    }
    void Update()
    {
        try
        {
            if(Time.realtimeSinceStartupAsDouble-started>210)throw new Exception("Timeout "+Trace());
            Check(d.Cargoes.Select(c=>c.body.GetInstanceID()).SequenceEqual(identities),"five cargo identities conserved");
            if(d.Stage!=previous){Debug.Log("PORT_I3_STAGE "+Trace());previous=d.Stage;}
            if(Time.unscaledTime>traceAt){Debug.Log("PORT_I3_TRACE "+Trace());traceAt=Time.unscaledTime+7;}
            if(d.Stage==PortStage.Complete){FinishRun();return;}
            if(d.MoveIndex!=previousMove){previousMove=d.MoveIndex;lifted=false;}
            if(d.CanOperate&&!d.Paused)Drive();
            if(d.traffic.RoadCrossingActive)
                foreach(var truck in d.traffic.trucks)
                    Check(truck.body.position.x<=-35.8f||truck.body.position.x>=-18.2f,"road vehicles stay behind pedestrian stop lines");
            if(d.traffic.CrossingActive)
                Check(Mathf.Abs(d.crane.TrolleyZ)>=d.config.crossingBoundary-.01f,"trolley respects controlled crossing boundary");
        }catch(Exception e){Fail(e);}
    }
    void FinishRun()
    {
        int expected=d.Layout==PortLayout.Priority||d.Layout==PortLayout.StackPractice?6:5;
        Check(d.Shipped==5&&d.Log.Moves.Count==expected&&d.Log.Moves.All(m=>m.outcome=="completed"),"complete planned five-container shipment");
        Check(Math.Abs(d.Log.Seconds-d.Log.Phases.Values.Sum())<.002,"exclusive phase clock reconciles");
        Check(d.Log.Delays.Values.Sum()<=d.Log.Seconds+.002,"delay tags are subsets rather than extra elapsed time");
        foreach(var move in d.Log.Moves)Check(move.crossingWait<=move.Total+.02,"move delay subsets reconcile");
        Check(d.Log.Error==null,"op3 exports remain healthy");
        Check(d.traffic.east.Travelled>1&&d.traffic.trucks.Any(t=>t.Distance>2)&&d.traffic.backgroundCranes.All(c=>c.MotionSeconds>0),"walkers, traffic and background cranes actually move");
        Check(d.safety.ContactCount==0,"gentle valid landings do not count as contacts");
        Debug.Log("PORT_I3_RUN_PASSED case="+kind+" layout="+d.Plan.layoutId+" moves="+expected+" path="+d.Log.DirectoryPath);moves+=expected;
        run++;if(kind=="core"&&run<4){StartRun();return;}
        Debug.Log("OPERATIONAL_ITERATION3_PHYSICAL_PASSED case="+kind+" moves="+moves);Time.timeScale=1;UnityEditor.EditorApplication.Exit(0);enabled=false;
    }
    void Drive()
    {
        var crane=d.crane;Vector3 target;
        if(d.Stage==PortStage.Park)target=d.config.park;
        else if(!d.Attached)
        {
            if(d.Stage==PortStage.Verification&&d.PlacementIssue()==null){crane.Command=Vector3.zero;return;}
            target=d.CargoTop+Vector3.up*.45f;
            if(Vector3.ProjectOnPlane(crane.hookBody.position-target,Vector3.up).magnitude>.10f)target.y=d.TransitHeight;
            else if(Vector3.Distance(crane.CouplingPoint,d.CargoTop)<.14f&&crane.hookBody.linearVelocity.magnitude<.45f)d.ToggleAttachment();
        }
        else if(!lifted)
        {
            target=new Vector3(d.cargo.position.x,d.TransitHeight,d.cargo.position.z);
            if(crane.hookBody.position.y>d.TransitHeight-.12f)lifted=true;
        }
        else
        {
            target=d.TargetSlot.position+Vector3.up*(d.config.cargoSize.y*.5f+.45f);
            if(Vector3.ProjectOnPlane(crane.hookBody.position-target,Vector3.up).magnitude>.025f)target.y=d.TransitHeight;
            else if(Mathf.Abs(d.cargo.position.y-d.TargetSlot.position.y)<.075f&&d.cargo.linearVelocity.magnitude<.25f)d.ToggleAttachment();
        }
        float ropeTarget=d.config.trolleyHeight-target.y-.035f;
        crane.Command=new Vector3(Mathf.Clamp((target.x-crane.GantryX)*1.5f,-1,1),Mathf.Clamp((target.z-crane.TrolleyZ)*1.4f,-1,1),Mathf.Clamp((ropeTarget-crane.rope.maxLength)*1.3f,-1,1));
        // Use the same gentle hoist approach recommended to operators; a full-speed deck strike is a real impact.
        if(target.y<crane.hookBody.position.y&&crane.hookBody.position.y-target.y<3)crane.Command.z=Mathf.Min(crane.Command.z,.15f);
    }
    string Trace()=>$"case={kind} run={run} layout={d.Plan.layoutId} stage={d.Stage} move={d.MoveIndex} crossing={d.traffic.CrossingActive}/{d.traffic.CraneWaiting} cargo={d.CargoId}:{d.cargo.position} hook={d.crane.hookBody.position} contacts={d.safety.ContactCount} near={d.safety.NearMissCount}";
    static void Check(bool value,string text){if(!value)throw new Exception("PORT_I3_CHECK_FAILED: "+text);}
    void Fail(Exception e){Debug.LogException(e);Time.timeScale=1;UnityEditor.EditorApplication.Exit(1);enabled=false;}
}
#endif
