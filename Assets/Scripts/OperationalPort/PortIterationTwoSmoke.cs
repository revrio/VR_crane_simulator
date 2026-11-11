#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEngine;

// Drives ordinary crane commands and Space actions. Cargo is never moved by the test after launch.
public class PortIterationTwoSmoke : MonoBehaviour
{
    PortDirector d;
    int run,previousMove=-1,totalMoves,pauseStep,endRun=5;
    int[] identities;
    double started,pauseAt,pauseClock;
    Vector3 pauseCargo;
    bool lifted,wrongReleased,recovered,stackChecked,capturedShip,additionalIntakes;
    float releasedAt,nextTrace;
    PortStage previous;
    void Start()
    {
        try
        {
            d=FindFirstObjectByType<PortDirector>();FindFirstObjectByType<PortInput>().disabledForTest=true;
            d.outputOverride="/tmp/port-iteration2-physical";d.automated=true;identities=d.Cargoes.Select(c=>c.body.GetInstanceID()).ToArray();
            // Interrupted partial moves remain reviewable; reset restores the selected layout.
            d.SelectLayout(PortLayout.Priority);d.Begin();string path=d.Log.DirectoryPath;
            Check(!d.SelectLayout(PortLayout.Accessible),"layout cannot change during a shipment");d.ResetOperation();
            Check(File.ReadAllText(Path.Combine(path,"runs.csv")).Contains("Aborted"),"reset preserves aborted run");
            Check(File.ReadAllText(Path.Combine(path,"moves.csv")).Contains("aborted"),"reset preserves partial move");
            Check(d.Cargoes.All(c=>c.slotId==d.Plan.Initial(c.cargoId).id),"reset restores slot occupancy");
            additionalIntakes=Array.Exists(Environment.GetCommandLineArgs(),a=>a=="-portAdditionalIntakes");
            if(additionalIntakes){run=5;endRun=8;}
            string only=Environment.GetCommandLineArgs().FirstOrDefault(a=>a.StartsWith("-portIntake="));
            if(only!=null)
            {
                var selected=(PortLayout)Enum.Parse(typeof(PortLayout),only.Substring("-portIntake=".Length));
                run=selected==PortLayout.Stacked?4:selected==PortLayout.Accessible?5:selected==PortLayout.Priority?6:7;
                endRun=run+1;additionalIntakes=true;
            }
            BeginRun();
        }catch(Exception e){Fail(e);}
    }
    void BeginRun()
    {
        var layout=run==4?PortLayout.Stacked:run==5?PortLayout.Accessible:run==6?PortLayout.Priority:run==7?PortLayout.StackPractice:(PortLayout)run;
        d.SelectLayout(layout);
        started=Time.realtimeSinceStartupAsDouble;previousMove=-1;lifted=false;capturedShip=false;
        if(run==1||run==2)
        {
            var bottom=d.Cargoes.First(c=>c.cargoId==(run==1?"C3":"C5"));
            Check(d.Blocker(bottom)!=null&&d.PickupIssue(bottom).Contains("blocked"),"bottom container cannot be acquired through its blocker");
        }
        if(run>=4)d.BeginFullIntake();else d.Begin();Time.timeScale=6;
        d.view.Select(0);Capture("operational-i2-layout-"+d.Plan.layoutId+(run==4?"-intake":""));
    }
    void Update()
    {
        try
        {
            if(Time.realtimeSinceStartupAsDouble-started>240)throw new Exception("Timed out: "+Trace()+" issue="+d.PlacementIssue());
            Check(d.Cargoes.Select(c=>c.body.GetInstanceID()).SequenceEqual(identities),"all five persistent bodies retain identity");
            if(d.Stage!=previous){Debug.Log("PORT_I2_STAGE "+Trace());previous=d.Stage;}
            if(Time.unscaledTime>nextTrace){Debug.Log("PORT_I2_TRACE "+Trace()+" issue="+(d.Stage==PortStage.Verification?d.PlacementIssue():""));nextTrace=Time.unscaledTime+5;}
            if(pauseStep<2&&run==0&&d.Attached)
            {
                if(pauseStep==0){d.TogglePause();pauseAt=Time.realtimeSinceStartupAsDouble;pauseClock=d.Log.Seconds;pauseCargo=d.cargo.position;pauseStep=1;return;}
                if(Time.realtimeSinceStartupAsDouble-pauseAt<.35)return;
                Check(Math.Abs(d.Log.Seconds-pauseClock)<.001&&Vector3.Distance(pauseCargo,d.cargo.position)<.001,"pause freezes attached load and move clock");d.TogglePause();pauseStep=2;
            }
            if(d.MoveIndex!=previousMove)
            {
                previousMove=d.MoveIndex;lifted=false;
                if(run==3&&d.MoveIndex==1)
                {
                    var c1=d.Cargoes.First(c=>c.cargoId=="C1");Check(d.Blocker(c1)?.cargoId=="C2","manual yard stack blocks its lower container");
                    stackChecked=true;d.view.Select(0);Capture("operational-i2-yard-stack");
                }
                if(run==2&&d.MoveIndex==1){Check(d.Cargoes.First(c=>c.cargoId=="C2").slotId=="TEMP-01","priority blocker stored in temporary bay");Capture("operational-i2-rehandle");}
            }
            if(d.Stage==PortStage.Complete){FinishRun();return;}
            if(d.CanOperate&&!d.Paused)Drive();
        }catch(Exception e){Fail(e);}
    }
    void FinishRun()
    {
        int expected=d.Layout==PortLayout.Priority||d.Layout==PortLayout.StackPractice?6:5;
        Check(d.Shipped==5&&d.MoveIndex==expected,"all five delivered and expected moves accepted");
        Check(d.Log.Moves.Count==expected&&d.Log.Moves.All(m=>m.outcome=="completed"),"move outcomes exported once each");
        Check(d.Log.Moves.Select(m=>m.id).Distinct().Count()==expected,"move IDs remain unique when a cargo is moved twice");
        Check(Math.Abs(d.Log.Seconds-d.Log.Phases.Values.Sum())<.001,"phase totals reconcile to active clock");
        double manual=d.Log.Phases.Where(p=>new[]{"Reposition","Pickup","Transfer","Verification"}.Contains(p.Key)).Sum(p=>p.Value);
        Check(Math.Abs(manual-d.Log.Moves.Sum(m=>m.Total))<.15,"per-move times reconcile to manual phases");
        Check(d.Log.Error==null,"exports remain healthy");
        string rows=File.ReadAllText(Path.Combine(d.Log.DirectoryPath,"runs.csv"));Check(rows.Contains("op2")&&rows.Contains("Completed"),"completed op2 run saved");
        if(run>=4)Check(File.ReadAllLines(Path.Combine(d.Log.DirectoryPath,"events.csv")).Count(l=>l.Contains(",truck_visit,"))==5,"full intake contains five actual truck visits");
        totalMoves+=expected;d.view.Select(0);Capture("operational-i2-complete-"+d.Plan.layoutId);
        Debug.Log("PORT_I2_RUN_PASSED layout="+d.Plan.layoutId+" intake="+d.FullIntake+" moves="+expected+" path="+d.Log.DirectoryPath);
        run++;
        if(run<endRun){BeginRun();return;}
        if(additionalIntakes)
        {
            Debug.Log("OPERATIONAL_ITERATION2_ADDITIONAL_INTAKES_PASSED moves="+totalMoves+" full truck intake and complete loading; five visits per run.");
            Time.timeScale=1;UnityEditor.EditorApplication.Exit(0);enabled=false;return;
        }
        Check(wrongReleased&&recovered&&stackChecked&&pauseStep==2,"recovery, manual stack and pause exercised");
        Debug.Log("OPERATIONAL_ITERATION2_PHYSICAL_PASSED moves="+totalMoves+" four staged layouts + full five-truck stacked intake; blocked retrieval, real tier support, reset/abort, pause, wrong release recovery, final park and op2 CSV.");
        Time.timeScale=1;UnityEditor.EditorApplication.Exit(0);enabled=false;
    }
    void Drive()
    {
        var crane=d.crane;Vector3 target;
        if(d.Stage==PortStage.Park)target=d.config.park;
        else if(!d.Attached)
        {
            if(d.Stage==PortStage.Verification&&d.PlacementIssue()==null){crane.Command=Vector3.zero;return;}
            if(d.Stage==PortStage.Verification&&wrongReleased&&!recovered&&Time.unscaledTime-releasedAt<.7f){crane.Command=Vector3.zero;return;}
            target=d.CargoTop+Vector3.up*.45f;
            float horizontal=Vector3.ProjectOnPlane(crane.hookBody.position-target,Vector3.up).magnitude;
            if(horizontal>.10f)target.y=d.TransitHeight;
            else if(Vector3.Distance(crane.CouplingPoint,d.CargoTop)<.14f&&crane.hookBody.linearVelocity.magnitude<.45f)
            {
                d.ToggleAttachment();if(d.Attached&&wrongReleased&&!recovered)recovered=true;
            }
        }
        else if(!lifted)
        {
            target=new Vector3(d.cargo.position.x,d.TransitHeight,d.cargo.position.z);
            if(crane.hookBody.position.y>d.TransitHeight-.12f)
            {
                if(run==0&&!wrongReleased)
                {
                    d.ToggleAttachment();wrongReleased=true;releasedAt=Time.unscaledTime;
                    Check(d.Custody==CargoCustody.Free&&d.PlacementIssue()!=null,"unsupported release rejected");
                }
                else lifted=true;
            }
        }
        else
        {
            target=d.TargetSlot.position+Vector3.up*(d.config.cargoSize.y*.5f+.45f);
            float horizontal=Vector3.ProjectOnPlane(crane.hookBody.position-target,Vector3.up).magnitude;
            if(horizontal>.025f)target.y=d.TransitHeight;
            else if(Mathf.Abs(d.cargo.position.y-d.TargetSlot.position.y)<.075f&&d.cargo.linearVelocity.magnitude<.25f)
            {
                if(!capturedShip&&d.TargetSlot.tier==1){d.view.Select(0);Capture("operational-i2-tier-"+d.Plan.layoutId);capturedShip=true;}
                d.ToggleAttachment();
            }
        }
        float ropeTarget=d.config.trolleyHeight-target.y-.035f;
        crane.Command=new Vector3(Mathf.Clamp((target.x-crane.GantryX)*1.5f,-1,1),Mathf.Clamp((target.z-crane.TrolleyZ)*1.4f,-1,1),Mathf.Clamp((ropeTarget-crane.rope.maxLength)*1.3f,-1,1));
    }
    void Capture(string name){if(!additionalIntakes)Type.GetType("OperationalPortValidation, Assembly-CSharp-Editor")?.GetMethod("Capture")?.Invoke(null,new object[]{name});}
    string Trace()=>$"run={run} layout={d.Plan.layoutId} stage={d.Stage} move={d.MoveIndex} cargo={d.CargoId}:{d.cargo.position} hook={d.crane.hookBody.position} owner={d.Custody} attached={d.Attached} stable={d.StableSeconds:F2}";
    static void Check(bool value,string message){if(!value)throw new Exception("PORT_I2_CHECK_FAILED: "+message);}
    void Fail(Exception e){Debug.LogException(e);Time.timeScale=1;UnityEditor.EditorApplication.Exit(1);enabled=false;}
}
#endif
