using UnityEngine;
using System.Linq;

public enum PortTrafficMode { Separated,ControlledCrossing,IntrusionDrill }
[DefaultExecutionOrder(-80)]
public class PortTraffic : MonoBehaviour
{
    public PortDirector director;
    public PortWorker crew,east,west,roadWorker;
    public PortTrafficVehicle[] trucks;
    public PortAmbientCrane[] backgroundCranes;
    public Renderer craneSignal,roadSignal;
    public AudioSource reverseSignal;
    public PortTrafficMode mode=PortTrafficMode.ControlledCrossing;
    public bool CrossingActive {get;private set;}
    public bool RoadCrossingActive {get;private set;}
    public bool CraneWaiting {get;private set;}
    public bool CrossingDone {get;private set;}
    public bool IntrusionTriggered {get;private set;}
    public float RunningSeconds {get;private set;}
    public PortWorker[] Workers=>new[]{crew,east,west,roadWorker};
    public string ModeLabel=>mode==PortTrafficMode.Separated?"SEPARATED":mode==PortTrafficMode.ControlledCrossing?"CROSSING":"INTRUSION DRILL";
    public string Status=>CraneWaiting?"W1 crossing / crane interlock":CrossingActive?"W1 crossing / stop line active":RoadCrossingActive?"Road crossing / trucks waiting":mode==PortTrafficMode.IntrusionDrill&&!IntrusionTriggered?"Hazard drill armed":"Marked routes / traffic running";
    public string WaitInstruction=>$"W1 is crossing the marked aisle. The trolley stops at the hold line. Wait about {Vector3.Distance(crew.body.position,new Vector3(18,0,0))/director.config.workerSpeed:F0}s for green; you may raise the load while waiting.";
    bool crossingRequested,roadRequested,roadReturning,eastReturning,westReturning,crewReturning;
    float nextRoadRequest;
    MaterialPropertyBlock color;
    public void ResetTraffic()
    {
        mode=PortTrafficMode.Separated;
        SetWaiting(false);CrossingActive=RoadCrossingActive=CrossingDone=IntrusionTriggered=crossingRequested=roadRequested=false;
        RunningSeconds=0;nextRoadRequest=4;roadReturning=eastReturning=westReturning=crewReturning=false;
        crew.ResetAt(new Vector3(-18,0,mode==PortTrafficMode.IntrusionDrill?-4:0));east.ResetAt(new Vector3(22,0,-17));west.ResetAt(new Vector3(-22,0,-17));roadWorker.ResetAt(new Vector3(-27,0,-45.5f));
        foreach(var truck in trucks)truck.ResetVehicle();foreach(var crane in backgroundCranes)crane.ResetMotion();
        Signal(craneSignal,false);Signal(roadSignal,false);if(reverseSignal!=null)reverseSignal.Stop();
    }
    public bool SelectMode(PortTrafficMode value)
    {
        if(director.Active){director.Tell("Reset or finish before changing the traffic scenario.");return false;}
        mode=value;director.ResetOperation();return true;
    }
    public void NextMode()=>SelectMode((PortTrafficMode)(((int)mode+1)%3));
    void FixedUpdate()
    {
        if(!director.Active||director.Paused){Silence();return;}
        float dt=Time.fixedDeltaTime;
        RunningSeconds+=dt;
        Patrol(east,new Vector3(22,0,3),ref eastReturning,dt);Patrol(west,new Vector3(-22,0,3),ref westReturning,dt);
        if(mode==PortTrafficMode.ControlledCrossing&&!CrossingDone)
        {
            if(!crossingRequested&&director.Attached&&director.CurrentMove?.kind=="Ship"){crossingRequested=true;director.Log?.Event("crossing_request","W1","cargo aisle");}
            if(crossingRequested&&!CrossingActive&&CraneAisleClear())
            {CrossingActive=true;director.Log?.Event("crossing_granted","W1","crane interlock armed");}
            if(CrossingActive)
            {
                crew.Step(new Vector3(18,0,0),director.config.workerSpeed,dt);
                if(crew.At(new Vector3(18,0,0)))
                {CrossingActive=false;CrossingDone=true;SetWaiting(false);director.Log?.Event("crossing_clear","W1");}
            }
        }
        else if(mode==PortTrafficMode.Separated)Patrol(crew,new Vector3(-18,0,3),ref crewReturning,dt);
        else if(CrossingDone)
        {
            Vector3 goal=crewReturning?new Vector3(18,0,0):new Vector3(18,0,3);
            crew.Step(goal,director.config.workerSpeed*.65f,dt);if(crew.At(goal))crewReturning=!crewReturning;
        }
        if(!roadRequested&&!RoadCrossingActive&&RunningSeconds>=nextRoadRequest){roadRequested=true;director.Log?.Event("crossing_request","W3","rear road");}
        if(roadRequested&&!RoadCrossingActive&&RoadClear())
        {RoadCrossingActive=true;director.Log?.Event("crossing_granted","W3","trucks held at stop lines");}
        if(RoadCrossingActive)
        {
            Vector3 goal=new Vector3(-27,0,roadReturning?-45.5f:-30);
            roadWorker.Step(goal,director.config.workerSpeed,dt);
            if(roadWorker.At(goal)){RoadCrossingActive=roadRequested=false;roadReturning=!roadReturning;nextRoadRequest=RunningSeconds+10;director.Log?.Event("crossing_clear","W3");}
        }
        foreach(var truck in trucks)truck.Step(director.config.trafficSpeed,dt,roadRequested||RoadCrossingActive);
        foreach(var crane in backgroundCranes)crane.Step(dt);
        Signal(craneSignal,CrossingActive);Signal(roadSignal,roadRequested||RoadCrossingActive);
    }
    void Update()
    {
        bool reversing=director.Active&&!director.Paused&&director.Stage==PortStage.HandlerClear;
        if(reverseSignal!=null){if(reversing&&!reverseSignal.isPlaying)reverseSignal.Play();if(!reversing)reverseSignal.Stop();}
    }
    bool CraneAisleClear()
    {
        if(Mathf.Abs(director.crane.TrolleyZ)<8||Mathf.Abs(director.crane.hookBody.position.z)<8)return false;
        return !director.Attached||Mathf.Abs(director.cargo.position.z)>8;
    }
    bool RoadClear()
    {
        if(!director.CanOperate)return false;
        foreach(var truck in trucks)if(truck.body.position.x>-36&&truck.body.position.x<-18)return false;
        return true;
    }
    void Patrol(PortWorker worker,Vector3 far,ref bool returning,float dt)
    {
        Vector3 goal=returning?worker.Home:far;worker.Step(goal,director.config.workerSpeed*.65f,dt);if(worker.At(goal))returning=!returning;
    }
    public float ClampTrolley(float proposed,float command)
    {
        if(!CrossingActive)return proposed;
        float current=director.crane.TrolleyZ,b=director.config.crossingBoundary,allowed=proposed;
        if(current<=-b&&proposed>-b)allowed=-b;
        if(current>=b&&proposed<b)allowed=b;
        if(Mathf.Abs(allowed-proposed)>.0001f)SetWaiting(true);
        else if(CraneWaiting&&((current<0&&command<0)||(current>0&&command>0)))SetWaiting(false);
        return allowed;
    }
    void SetWaiting(bool value)
    {
        if(CraneWaiting==value)return;
        director.Log?.Advance(Time.realtimeSinceStartupAsDouble);CraneWaiting=value;
        if(director.Log!=null)director.Log.DelayTag=value?"CrossingWait":"";
        director.Log?.Event(value?"crane_wait_start":"crane_wait_end","W1");
    }
    public void ClearWait()=>SetWaiting(false);
    void Signal(Renderer lamp,bool stop)
    {
        if(lamp==null)return;if(color==null)color=new MaterialPropertyBlock();color.SetColor("_BaseColor",stop?new Color(1,.12f,.035f):new Color(.08f,.85f,.35f));lamp.SetPropertyBlock(color);
    }
    void Silence(){foreach(var truck in trucks)truck.Silence();if(reverseSignal!=null)reverseSignal.Stop();}
    public string Snapshot()=>JsonUtility.ToJson(new TrafficSetup{mode=mode.ToString(),crewStart=crew.Home,roadCrossingX=-27,roadStopWest=-36,roadStopEast=-18,firstRoadRequest=4,roadCycleWait=10,firstCargoCrossing="first ship-bound acquisition; grant only after actual crane clearance",workers=Workers.Select(w=>new ActorSetup{id=w.workerId,start=w.Home,height=w.shape.height,radius=w.shape.radius}).ToArray(),trucks=trucks.Select(t=>new ActorSetup{id=t.vehicleId,start=new Vector3(t.startX,0,t.laneZ),direction=t.direction}).ToArray(),physics=new PhysicsSetup{fixedStep=Time.fixedDeltaTime,gravity=Physics.gravity,hookMass=director.crane.hookBody.mass,cargoMass=director.cargo.mass,ropeStiffness=director.crane.rope.stiffness,ropeDamping=director.crane.rope.damping},obstacles=director.safety.obstacles.Select(o=>new ObstacleSetup{id=o.Id,kind=o.kind.ToString(),center=o.shape.bounds.center,size=o.shape.bounds.size}).ToArray()});
    [System.Serializable] class ActorSetup{public string id;public Vector3 start;public float height,radius,direction;}
    [System.Serializable] class ObstacleSetup{public string id,kind;public Vector3 center,size;}
    [System.Serializable] class PhysicsSetup{public float fixedStep,hookMass,cargoMass,ropeStiffness,ropeDamping;public Vector3 gravity;}
    [System.Serializable] class TrafficSetup{public string mode,firstCargoCrossing;public Vector3 crewStart;public float roadCrossingX,roadStopWest,roadStopEast,firstRoadRequest,roadCycleWait;public ActorSetup[] workers,trucks;public ObstacleSetup[] obstacles;public PhysicsSetup physics;}
}
