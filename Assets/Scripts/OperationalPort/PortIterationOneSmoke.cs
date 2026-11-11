#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class PortIterationOneSmoke : MonoBehaviour
{
    PortDirector d;
    int cargoId,run,mode;
    double started,pausedAt,pausedSeconds;
    Vector3 pausedTruck;
    bool pauseChecked,releasedWrong,lifted,capturedIntake,capturedShip;
    float nextTrace,releaseTime;
    PortStage previous;
    void Start()
    {
        try
        {
            d=FindFirstObjectByType<PortDirector>();FindFirstObjectByType<PortInput>().disabledForTest=true;
            d.outputOverride="/tmp/port-iteration1-physical";d.automated=true;cargoId=d.cargo.GetInstanceID();
            started=Time.realtimeSinceStartupAsDouble;
            Vector3 initial=d.cargo.position;Quaternion rotation=d.cargo.rotation;
            d.cargo.position=d.config.ship+Vector3.forward*.6f;d.cargo.rotation=Quaternion.Euler(0,7,0);
            Check(d.PlacementIssue()!=null,"rotated footprint outside ship slot rejected");
            d.cargo.position=d.config.ship+Vector3.up;d.cargo.rotation=Quaternion.identity;
            Check(d.PlacementIssue()!=null,"suspended cargo cannot count as ship placement");
            d.cargo.position=initial;d.cargo.rotation=rotation;
            d.CustodyChanged+=(owner,before)=>Check(Vector3.Distance(before,d.cargo.position)<.05f,"custody handoff preserves physical identity and pose");
            var panel=FindObjectsByType<PortPanel>(FindObjectsSortMode.None).First(p=>p.GetComponentInParent<Canvas>().renderMode!=RenderMode.WorldSpace);
            panel.start.onClick.Invoke();Check(d.Stage==PortStage.TruckArrival&&d.Log.Error==null,"practice UI starts operational intake with healthy export");
            Time.timeScale=4;
        }catch(Exception e){Fail(e);}
    }
    void Update()
    {
        try
        {
            if(Time.realtimeSinceStartupAsDouble-started>180)throw new Exception("Operational test timed out: "+Trace());
            Check(d.cargo.GetInstanceID()==cargoId,"one persistent cargo through entire chain");
            if(d.Stage!=previous){Debug.Log("PORT_SMOKE_STAGE "+Trace());previous=d.Stage;}
            if(Time.unscaledTime>nextTrace){Debug.Log("PORT_SMOKE "+Trace());nextTrace=Time.unscaledTime+4;}
            if(!pauseChecked&&d.Stage==PortStage.TruckArrival)
            {
                if(mode==0){d.TogglePause();pausedAt=Time.realtimeSinceStartupAsDouble;pausedSeconds=d.Log.Seconds;pausedTruck=d.vehicles.truck.position;mode=1;return;}
                if(Time.realtimeSinceStartupAsDouble-pausedAt<.3)return;
                Check(Math.Abs(d.Log.Seconds-pausedSeconds)<.01&&Vector3.Distance(pausedTruck,d.vehicles.truck.position)<.001f,"pause freezes truck and clock");
                d.TogglePause();pauseChecked=true;
            }
            if(!capturedIntake&&d.Stage==PortStage.HandlerCarry){OperationalPortValidationCapture("operational-intake");capturedIntake=true;}
            if(d.Stage==PortStage.Pickup&&run==0)
            {
                Check(d.Custody==CargoCustody.Yard&&Vector3.Distance(d.cargo.position,d.config.yard)<.15f,"actual yard release and settling");
                string path=d.Log.DirectoryPath;d.ResetOperation();
                Check(File.ReadAllText(Path.Combine(path,"runs.csv")).Contains("Aborted"),"reset preserves interrupted intake");
                Check(d.Custody==CargoCustody.Truck&&d.cargo.isKinematic&&d.Stage==PortStage.Idle,"reset restores loaded truck and actor ownership");
                run=1;d.Begin();Time.timeScale=4;return;
            }
            if(d.Stage==PortStage.Complete)
            {
                Check(d.Custody==CargoCustody.Ship&&d.cargo.isKinematic,"cargo remains aboard after complete");
                Check(Math.Abs(d.Log.Seconds-d.Log.Phases.Values.Sum())<.01,"exclusive phase durations reconcile");
                Check(File.ReadAllText(Path.Combine(d.Log.DirectoryPath,"runs.csv")).Contains("Completed"),"completed operational export");
                Check(releasedWrong,"premature release was tested and recovered");
                Check(d.Log.Error==null,"exports stay healthy");
                d.view.Select(0);OperationalPortValidationCapture("operational-complete");
                Debug.Log("OPERATIONAL_ITERATION1_PHYSICAL_PASSED: visible truck and handler custody, pause/reset/abort, actual crane pickup and ship landing, failed release recovery, final park, CSV.");
                Time.timeScale=1;UnityEditor.EditorApplication.Exit(0);enabled=false;return;
            }
            if(d.CanOperate&&!d.Paused)Drive();
        }catch(Exception e){Fail(e);}
    }
    void Drive()
    {
        var crane=d.crane;Vector3 target;
        if(d.Stage==PortStage.Park)target=d.config.park;
        else if(!d.Attached)
        {
            if(d.Stage==PortStage.Verification&&d.PlacementIssue()==null){crane.Command=Vector3.zero;return;}
            if(d.Stage==PortStage.Verification&&releasedWrong&&Time.unscaledTime-releaseTime<.6f){crane.Command=Vector3.zero;return;}
            target=d.CargoTop+Vector3.up*.45f;
            if(Vector3.ProjectOnPlane(crane.hookBody.position-target,Vector3.up).magnitude>.25f)target.y=10;
            else if(Vector3.Distance(crane.CouplingPoint,d.CargoTop)<.18f&&crane.hookBody.linearVelocity.magnitude<.45f)
            {
                d.ToggleAttachment();
                if(d.Attached)Check(d.Custody==CargoCustody.Crane,"acquired cargo belongs to crane");
            }
        }
        else
        {
            if(!lifted)
            {
                target=new Vector3(d.cargo.position.x,10,d.cargo.position.z);
                if(crane.hookBody.position.y>9.7f)
                {
                    if(!releasedWrong)
                    {
                        d.ToggleAttachment();releasedWrong=true;releaseTime=Time.unscaledTime;
                        Check(d.Custody==CargoCustody.Free&&d.PlacementIssue()!=null,"unsupported wrong-area release rejected");
                    }
                    else lifted=true;
                }
            }
            else
            {
                target=d.config.ship+Vector3.up*(d.config.cargoSize.y*.5f+.45f);
                if(Vector3.ProjectOnPlane(crane.hookBody.position-target,Vector3.up).magnitude>.14f)target.y=10;
                else if(Mathf.Abs(d.cargo.position.y-d.config.ship.y)<.10f&&d.cargo.linearVelocity.magnitude<.35f)
                {
                    if(!capturedShip){d.view.Select(0);OperationalPortValidationCapture("operational-ship");capturedShip=true;}
                    d.ToggleAttachment();
                }
            }
        }
        float ropeTarget=d.config.trolleyHeight-target.y-.035f;
        crane.Command=new Vector3(Mathf.Clamp((target.x-crane.GantryX)*1.5f,-1,1),Mathf.Clamp((target.z-crane.TrolleyZ)*1.4f,-1,1),Mathf.Clamp((ropeTarget-crane.rope.maxLength)*1.3f,-1,1));
    }
    static void OperationalPortValidationCapture(string name)=>Type.GetType("OperationalPortValidation, Assembly-CSharp-Editor")?.GetMethod("Capture")?.Invoke(null,new object[]{name});
    string Trace()=>$"stage={d.Stage} owner={d.Custody} cargo={d.cargo.position} hook={d.crane.hookBody.position} attached={d.Attached} stable={d.StableSeconds:F2}";
    static void Check(bool value,string message){if(!value)throw new Exception("PORT_CHECK_FAILED: "+message);}
    void Fail(Exception e){Debug.LogException(e);Time.timeScale=1;UnityEditor.EditorApplication.Exit(1);enabled=false;}
}
#endif
