using System;
using System.IO;
using System.Linq;
using UnityEngine;

public enum PortStage { Idle,TruckArrival,TruckParking,HandlerApproach,HandlerLift,HandlerCarry,HandlerLower,HandlerClear,Reposition,Pickup,Transfer,Verification,Park,Complete,Aborted }
public enum CargoCustody { Truck,Handler,Yard,Crane,Free,Ship }

[DefaultExecutionOrder(-100)]
public class PortDirector : MonoBehaviour
{
    public PortScenario config;
    public PortCrane crane;
    public PortView view;
    public PortVehicles vehicles;
    public PortSafety safety;
    public PortTraffic traffic;
    public Rigidbody cargo;
    public PortCargo[] allCargo;
    public Collider shipDeck,quayDeck;
    public AudioSource voice,sfx;
    public AudioClip lockSound,impactSound,successSound;
    public PortStage Stage {get;private set;}
    public CargoCustody Custody=>CurrentCargo?.custody??CargoCustody.Truck;
    public PortCargo CurrentCargo=>entities?.FirstOrDefault(c=>c.body==cargo);
    public PortCargo[] Cargoes=>entities??Array.Empty<PortCargo>();
    public PortShipmentPlan Plan {get;private set;}
    public PortLayout Layout {get;private set;}
    public int MoveIndex {get;private set;}
    public int AcceptedDeliveries {get;private set;}
    public PortMoveSpec CurrentMove=>Plan!=null&&MoveIndex<Plan.moves.Length?Plan.moves[MoveIndex]:null;
    public PortSlotSpec TargetSlot=>CurrentMove==null?null:Plan.Slot(CurrentMove.destinationId);
    public int Shipped=>Cargoes.Count(c=>c.custody==CargoCustody.Ship);
    public int ContainerCount=>Plan?.cargoIds.Length??1;
    public bool Paused {get;private set;}
    public bool CanOperate=>Stage==PortStage.Reposition||Stage==PortStage.Pickup||Stage==PortStage.Transfer||Stage==PortStage.Verification||Stage==PortStage.Park;
    public bool Active=>Stage!=PortStage.Idle&&Stage!=PortStage.Complete&&Stage!=PortStage.Aborted;
    public bool Attached=>joint!=null;
    public string Feedback {get;private set;}="";
    public PortRunRecorder Log {get;private set;}
    public string outputOverride;
    public bool automated,VoiceMuted;
    public bool FullIntake {get;private set;}
    public int Contacts=>safety!=null?safety.ContactCount:legacyContacts;
    public float StableSeconds {get;private set;}
    public float TransitHeight=>config.multipleCargo?config.transitHookHeight:10;
    public Vector3 CargoTop=>cargo.transform.TransformPoint(Vector3.up*(config.cargoSize.y*.5f));
    public Vector3 HandlerCargoPoint=>vehicles.handler.position+vehicles.handler.forward*config.handlerReach+Vector3.up*vehicles.gripHeight;
    public Vector3 Goal=>Stage==PortStage.Park?config.park:Attached||Stage==PortStage.Verification?TargetSlot.position+Vector3.up*(config.cargoSize.y*.5f+.45f):CargoTop+Vector3.up*.45f;
    public string CargoId=>CurrentCargo?.cargoId??"C1";
    public string LockControl=>PortView.SimulatedVR?"hold Space + tap B":view.HeadsetRunning?"press right A":"press Space";
    public string RaiseControl=>PortView.SimulatedVR?"Space + W":view.HeadsetRunning?"right stick up":"Up Arrow";
    public string LowerControl=>PortView.SimulatedVR?"Space + S":view.HeadsetRunning?"right stick down":"Down Arrow";
    public string FineControl=>PortView.SimulatedVR?"Space + G":view.HeadsetRunning?"right grip":"Left Shift";
    FixedJoint joint;
    float lastTargetContact=-100;
    Rigidbody contactedCargo;
    public bool ReadyToLock=>CurrentCargo!=null&&PickupIssue(CurrentCargo)==null&&
        ((contactedCargo==cargo&&Time.time-lastTargetContact<.2f)||
        (Vector3.ProjectOnPlane(crane.CouplingPoint-CargoTop,Vector3.up).magnitude<1.2f&&Mathf.Abs(crane.CouplingPoint.y-CargoTop.y)<.9f));
    public void RecordLoadContact(Rigidbody body)
    {
        if(body!=cargo)return;
        contactedCargo=body;lastTargetContact=Time.time;
        // Contact validates a later button press; it must never attach a load.
    }
    public string AlignmentGuidance
    {
        get
        {
            if(!CanOperate)return "Select a layout and start the operation.";
            Vector3 delta=Goal-crane.hookBody.position;
            if(Attached&&TargetSlot!=null)delta=TargetSlot.position-cargo.position;
            if(!Attached&&(Stage==PortStage.Pickup||Stage==PortStage.Reposition))delta=CargoTop-crane.CouplingPoint;
            return $"{(delta.x>=0?"RIGHT":"LEFT")} {Mathf.Abs(delta.x):F1} m   {(delta.z>=0?"SHIPWARD":"YARDWARD")} {Mathf.Abs(delta.z):F1} m\n{(delta.y>=0?"RAISE":"LOWER")} {Mathf.Abs(delta.y):F1} m";
        }
    }
    PortCargo[] entities;
    string[] incomingOrder;
    int incomingIndex;
    Vector3 handlerDrop,handlerExit;
    float stageTime,travel,clearTime,savedScale=1,nextAction,nextSave,nextFeedback,contactCooldown;
    bool truckLeaving,closed;
    int legacyContacts;
    public event Action<CargoCustody,Vector3> CustodyChanged;
    void Start(){if(Array.Exists(Environment.GetCommandLineArgs(),a=>a=="-portlab-automated-check"))automated=true;ResetOperation();}
    void EnsureCargo()
    {
        if(entities!=null)return;
        if(allCargo!=null&&allCargo.Length>0)entities=allCargo;
        else
        {
            var item=cargo.GetComponent<PortCargo>();if(item==null)item=cargo.gameObject.AddComponent<PortCargo>();
            item.cargoId="C1";item.body=cargo;item.footprint=cargo.GetComponent<Collider>();entities=new[]{item};
        }
        if(quayDeck==null)quayDeck=GameObject.Find("Quay deck").GetComponent<Collider>();
    }
    public bool SelectLayout(PortLayout layout)
    {
        if(Active){Tell("Finish or reset the shipment before changing its layout.");return false;}
        Layout=layout;ResetOperation();return true;
    }
    public void Begin()=>BeginOperation(!config.multipleCargo);
    public void BeginFullIntake()=>BeginOperation(true);
    void BeginOperation(bool full)
    {
        if(Active)return;ResetOperation();
        string invalid=Plan.Validate(config);if(invalid!=null){Tell("Scenario unavailable: "+invalid);return;}
        FullIntake=full;
        string root=string.IsNullOrEmpty(outputOverride)?Path.Combine(Application.persistentDataPath,"OperationalSessions"):outputOverride;
        Log=new PortRunRecorder(root,config.version,automated,Time.realtimeSinceStartupAsDouble,view.HeadsetRunning?"XR controllers":"Desktop keyboard/mouse",ContainerCount,Plan.layoutId,full?"FullIntake":"Staged",traffic!=null?traffic.mode.ToString():null);
        Log.Setup("{\"configuration\":"+JsonUtility.ToJson(config,true)+",\"plan\":"+JsonUtility.ToJson(Plan,true)+",\"fullIntake\":"+(full?"true":"false")+",\"anchoredSupports\":"+(config.anchorSettledCargo?"true":"false")+(traffic!=null?",\"traffic\":"+traffic.Snapshot():"")+"}");
        if(Log.Error!=null){Tell("Could not save: "+Log.Error);return;}
        Log.Event("run_start","shipment",full?"truck intake":"explicit staged practice");
        if(full)
        {
            foreach(var item in entities){item.slotId="";item.gameObject.SetActive(false);}
            incomingOrder=Plan.cargoIds.OrderBy(id=>Plan.Initial(id).position.y).ThenBy(id=>id).ToArray();incomingIndex=0;PrepareVisit(true);
        }
        else{StartMove(false);view.OperatorHandoff();}
        Log.Save();Debug.Log("[OperationalPort] Session data: "+Log.DirectoryPath);
    }
    public void ResetOperation()
    {
        contactedCargo=null;lastTargetContact=-100;
        EnsureCargo();if(Active)Abort("reset");Log?.Dispose();Log=null;
        safety?.ResetSafety();Time.timeScale=1;Paused=false;nextAction=0;legacyContacts=0;StableSeconds=0;nextFeedback=contactCooldown=0;
        Detach(false);Stage=PortStage.Idle;stageTime=travel=clearTime=0;truckLeaving=false;Feedback="";MoveIndex=0;AcceptedDeliveries=0;
        Plan=PortShipmentPlan.Create(config,Layout);
        vehicles.truck.SetPositionAndRotation(config.truckStart,Quaternion.Euler(0,90,0));
        vehicles.handler.SetPositionAndRotation(config.handlerStart,Quaternion.identity);vehicles.gripHeight=2.7f;
        foreach(var item in entities)
        {
            item.gameObject.SetActive(true);item.body.isKinematic=true;item.body.rotation=Quaternion.identity;
            item.slotId=Plan.Initial(item.cargoId).id;item.custody=config.multipleCargo?CargoCustody.Yard:CargoCustody.Truck;
            item.body.position=config.multipleCargo?Plan.Initial(item.cargoId).position:vehicles.truck.TransformPoint(config.truckCargoOffset);
        }
        cargo=Entity(Plan.moves[0].cargoId).body;
        crane.ResetCrane();vehicles.SyncRig();traffic?.ResetTraffic();Physics.SyncTransforms();
        if(Application.isPlaying&&!view.HeadsetRunning)view.Select(3);
    }
    PortCargo Entity(string id)=>entities.First(c=>c.cargoId==id);
    void PrepareVisit(bool first)
    {
        cargo=Entity(incomingOrder[incomingIndex]).body;cargo.gameObject.SetActive(true);
        vehicles.truck.SetPositionAndRotation(config.truckStart,Quaternion.Euler(0,90,0));truckLeaving=false;
        if(first)vehicles.handler.SetPositionAndRotation(config.handlerStart,Quaternion.identity);
        vehicles.gripHeight=2.7f;SetCargoPose(vehicles.truck.TransformPoint(config.truckCargoOffset));SetCustody(CargoCustody.Truck);
        var dest=Plan.Initial(CargoId);handlerDrop=new Vector3(dest.position.x,0,dest.position.z-config.handlerReach);
        handlerExit=config.multipleCargo?new Vector3(0,0,-32):config.handlerExit;
        SetStage(PortStage.TruckArrival);Log?.Event("truck_visit",CargoId,incomingIndex+1);
    }
    void Update()
    {
        Log?.Advance(Time.realtimeSinceStartupAsDouble);
        if(Active&&Time.unscaledTime>=nextSave){Log?.Save();nextSave=Time.unscaledTime+2;}
        if(Time.unscaledTime>nextFeedback)Feedback="";
    }
    void FixedUpdate()
    {
        if(!Active||Paused)return;
        float dt=Time.fixedDeltaTime;stageTime+=dt;
        if(truckLeaving)Move(vehicles.truck,config.truckExit,config.truckSpeed,dt);
        switch(Stage)
        {
            case PortStage.TruckArrival:if(Move(vehicles.truck,config.truckStand,config.truckSpeed,dt))SetStage(PortStage.TruckParking);break;
            case PortStage.TruckParking:if(stageTime>1.4f)SetStage(PortStage.HandlerApproach);break;
            case PortStage.HandlerApproach:
                bool arrived=MoveFacing(vehicles.handler,config.handlerPickup,config.handlerSpeed,dt);
                if(arrived&&Quaternion.Angle(vehicles.handler.rotation,Quaternion.identity)<1)
                {vehicles.handler.rotation=Quaternion.identity;SetCustody(CargoCustody.Handler);SetStage(PortStage.HandlerLift);Play(lockSound);}break;
            case PortStage.HandlerLift:
                float height=Mathf.Max(5.4f,Plan.Initial(CargoId).position.y+2.7f);
                vehicles.gripHeight=Mathf.MoveTowards(vehicles.gripHeight,height,dt);
                if(vehicles.gripHeight>=height){truckLeaving=true;SetStage(PortStage.HandlerCarry);}break;
            case PortStage.HandlerCarry:
                travel=Mathf.Min(1,travel+dt*config.handlerSpeed/Mathf.Max(25,Vector3.Distance(config.handlerPickup,handlerDrop)*1.5f));
                FollowCurve(vehicles.handler,config.handlerPickup,config.handlerPickup+Vector3.forward*9.5f,handlerDrop-Vector3.forward*9.5f,handlerDrop,travel,false);
                if(travel>=1)SetStage(PortStage.HandlerLower);break;
            case PortStage.HandlerLower:
                float y=Plan.Initial(CargoId).position.y;vehicles.gripHeight=Mathf.MoveTowards(vehicles.gripHeight,y,dt);
                if(vehicles.gripHeight<=y){SetCustody(CargoCustody.Yard);CurrentCargo.slotId=Plan.Initial(CargoId).id;SetStage(PortStage.HandlerClear);Play(lockSound);}break;
            case PortStage.HandlerClear:
                float safe=Mathf.Max(4.5f,Plan.Initial(CargoId).position.y+1.6f);
                if(vehicles.gripHeight<safe)vehicles.gripHeight=Mathf.MoveTowards(vehicles.gripHeight,safe,dt*1.4f);
                else
                {
                    travel=Mathf.Min(1,travel+dt*config.handlerSpeed/Mathf.Max(26,Vector3.Distance(handlerDrop,handlerExit)*1.5f));
                    FollowCurve(vehicles.handler,handlerDrop,handlerDrop-Vector3.forward*8,handlerExit+Vector3.forward*8,handlerExit,travel,true);
                    if(travel>=1&&cargo.linearVelocity.magnitude<.2f&&Mathf.Abs(cargo.position.y-Plan.Initial(CargoId).position.y)<.15f)
                    {
                        if(config.multipleCargo&&config.anchorSettledCargo)cargo.isKinematic=true;
                        incomingIndex++;
                        if(incomingIndex<incomingOrder.Length)PrepareVisit(false);
                        else{StartMove(false);crane.hookBody.isKinematic=false;view.OperatorHandoff();}
                    }
                }break;
            case PortStage.Reposition:
                Vector3 approach=crane.hookBody.position-CargoTop;approach.y=0;
                if(approach.magnitude<1&&crane.hookBody.position.y>=CargoTop.y+.45f)SetStage(PortStage.Pickup);break;
            case PortStage.Verification:
                StableSeconds=PlacementIssue()==null?StableSeconds+dt:0;
                if(StableSeconds>=config.placementDwell)AcceptMove();break;
            case PortStage.Park:
                bool near=Vector3.Distance(crane.hookBody.position,config.park)<1.4f&&crane.hookBody.linearVelocity.magnitude<.7f;
                clearTime=near?clearTime+dt:0;if(clearTime>.6f)Complete();break;
        }
        if(Custody==CargoCustody.Truck)SetCargoPose(vehicles.truck.TransformPoint(config.truckCargoOffset));
        else if(Custody==CargoCustody.Handler)SetCargoPose(HandlerCargoPoint);
    }
    void StartMove(bool reposition)
    {
        cargo=Entity(CurrentMove.cargoId).body;Log?.Advance(Time.realtimeSinceStartupAsDouble);
        Log?.BeginMove(CurrentMove,Plan.Slot(CurrentCargo.slotId),TargetSlot);
        SetStage(reposition?PortStage.Reposition:PortStage.Pickup);crane.hookBody.isKinematic=false;
    }
    void AcceptMove()
    {
        var destination=TargetSlot;Log?.Advance(Time.realtimeSinceStartupAsDouble);
        CurrentCargo.slotId=destination.id;SetCustody(destination.area=="Ship"?CargoCustody.Ship:CargoCustody.Yard);
        if(config.multipleCargo&&config.anchorSettledCargo)cargo.isKinematic=true;
        string acceptedCargo=CargoId;
        Log?.Event("placement_accepted",CargoId,destination.id);Log?.FinishMove();MoveIndex++;AcceptedDeliveries++;Play(successSound);
        if(MoveIndex>=Plan.moves.Length)SetStage(PortStage.Park);else StartMove(true);
        Tell($"DELIVERY ACCEPTED: {acceptedCargo} placed at {destination.id}. "+(Stage==PortStage.Park?"All containers placed. Park the empty spreader.":$"Next pickup: {CargoId}."));
    }
    void SetCargoPose(Vector3 point){cargo.position=point;cargo.rotation=Quaternion.identity;}
    void SetCustody(CargoCustody owner)
    {
        Vector3 before=cargo.position;CurrentCargo.custody=owner;
        if(!cargo.isKinematic){cargo.linearVelocity=Vector3.zero;cargo.angularVelocity=Vector3.zero;}
        cargo.isKinematic=owner==CargoCustody.Truck||owner==CargoCustody.Handler||owner==CargoCustody.Ship;
        Log?.Event("custody",CargoId,owner);CustodyChanged?.Invoke(owner,before);
    }
    public PortCargo Blocker(PortCargo candidate)=>Cargoes.FirstOrDefault(c=>c!=candidate&&!string.IsNullOrEmpty(c.slotId)&&Plan.Slot(c.slotId)?.supportCargoId==candidate.cargoId);
    public string PickupIssue(PortCargo candidate)
    {
        var above=Blocker(candidate);if(above!=null)return candidate.cargoId+" is blocked by "+above.cargoId+". Remove the top container first.";
        if(CurrentMove==null||candidate.cargoId!=CurrentMove.cargoId)return "Current move: "+CurrentMove?.cargoId+" to "+CurrentMove?.destinationId+".";
        return null;
    }
    public void ToggleAttachment()
    {
        if(!CanOperate||Paused){Tell("Wait for the reachstacker to unload and clear the yard.");return;}
        if(Time.unscaledTime<nextAction)return;nextAction=Time.unscaledTime+.25f;
        if(Attached){Detach(true);return;}
        if(Stage==PortStage.Park){Tell("The shipment is loaded. Park the empty spreader; no lock is needed.");return;}
        string blocked=PickupIssue(CurrentCargo);if(blocked!=null){Tell(blocked);Log?.Event("blocked_pickup",CargoId,blocked);return;}
        Vector3 gap=crane.CouplingPoint-CargoTop;
        if(!ReadyToLock)
        {Tell($"Move onto or close to {CargoId}, then {LockControl} to lock.\n"+AlignmentGuidance);Log?.Event("coupling_miss",CargoId,gap.magnitude);return;}
        cargo.isKinematic=false;CurrentCargo.slotId="";
        joint=crane.hookBody.gameObject.AddComponent<FixedJoint>();joint.connectedBody=cargo;
        joint.anchor=Vector3.zero;joint.autoConfigureConnectedAnchor=true;joint.enableCollision=false;
        SetCustody(CargoCustody.Crane);SetStage(PortStage.Transfer);Play(lockSound);Log?.Event("acquired",CargoId);
    }
    void Detach(bool record)
    {
        if(joint==null)return;joint.connectedBody=null;Destroy(joint);joint=null;
        if(record){SetCustody(CargoCustody.Free);Log?.Event("released",CargoId);Play(lockSound);SetStage(PortStage.Verification);}
    }
    public string PlacementIssue()
    {
        if(Attached)return "Release "+CargoId+" to verify placement.";
        var target=TargetSlot;if(target==null)return "No active receiving slot.";
        if(Cargoes.Any(c=>c!=CurrentCargo&&c.slotId==target.id))return target.id+" is occupied.";
        Vector3 half=config.cargoSize*.5f;
        for(int x=-1;x<=1;x+=2)for(int y=-1;y<=1;y+=2)for(int z=-1;z<=1;z+=2)
        {
            Vector3 corner=cargo.transform.TransformPoint(new Vector3(x*half.x,y*half.y,z*half.z))-target.position;
            if(Mathf.Abs(corner.x)>target.footprint.x*.5f+.65f||Mathf.Abs(corner.z)>target.footprint.y*.5f+.65f)return "Move closer to the centre of "+target.id+".\n"+AlignmentGuidance;
        }
        if(Vector3.Angle(cargo.transform.up,Vector3.up)>12||Mathf.Abs(Mathf.DeltaAngle(0,cargo.rotation.eulerAngles.y))>15)return "Cargo is tilted or rotated. Reposition it upright.";
        if(Mathf.Abs(cargo.position.y-target.position.y)>.4f)return "Match the highlighted support height and lower gently.";
        if(cargo.linearVelocity.magnitude>.4f||cargo.angularVelocity.magnitude>.3f)return "Let "+CargoId+" settle on its support.";
        Collider support=target.area=="Ship"?shipDeck:quayDeck;
        if(!string.IsNullOrEmpty(target.supportCargoId))
        {
            var below=Entity(target.supportCargoId);
            if(string.IsNullOrEmpty(below.slotId)||below.custody==CargoCustody.Crane||Vector3.Distance(below.body.position,target.position-Vector3.up*config.cargoSize.y)>1.2f)
                return "Support "+below.cargoId+" must be correctly placed below this tier.";
            support=below.footprint;
        }
        if(!Physics.Raycast(cargo.position,Vector3.down,out var hit,config.cargoSize.y*.5f+.45f)||hit.collider!=support)return "Cargo needs actual support from "+(target.supportCargoId==""?target.area:target.supportCargoId)+".";
        return null;
    }
    public void TogglePause()
    {
        if(!Active)return;Log?.Advance(Time.realtimeSinceStartupAsDouble);Paused=!Paused;if(Log!=null)Log.Paused=Paused;
        if(Paused){savedScale=Time.timeScale;Time.timeScale=0;crane.Command=Vector3.zero;voice.Stop();sfx.Pause();}else{Time.timeScale=savedScale;sfx.UnPause();}
        Log?.Event(Paused?"pause":"resume","operator");Log?.Save();
    }
    public void ToggleVoice(){VoiceMuted=!VoiceMuted;if(VoiceMuted)voice.Stop();}
    void SetStage(PortStage state)
    {
        Log?.Advance(Time.realtimeSinceStartupAsDouble);Stage=state;stageTime=travel=0;StableSeconds=0;
        if(Log!=null){Log.Phase=state.ToString();Log.Event("stage","terminal",state);Log.Save();}
    }
    void Complete()
    {
        safety?.EndRun("completed");
        SetStage(PortStage.Complete);crane.Command=Vector3.zero;crane.hookBody.isKinematic=true;
        if(Log!=null){Log.Event("shipment_complete","shipment",Shipped);Log.Status="Completed";Log.Running=false;Log.Save();}Play(successSound);
    }
    public void Abort(string reason)
    {
        if(!Active)return;Log?.Advance(Time.realtimeSinceStartupAsDouble);safety?.EndRun("aborted");Detach(false);Stage=PortStage.Aborted;crane.Command=Vector3.zero;
        if(crane!=null&&crane.hookBody!=null)crane.hookBody.isKinematic=true;foreach(var item in Cargoes)if(item!=null&&item.body!=null)item.body.isKinematic=true;
        Paused=false;Time.timeScale=1;
        if(Log!=null){Log.InterruptMove();Log.Event("abort","operation",reason);Log.Status="Aborted";Log.Running=false;Log.Save();}
    }
    public void Contact(Collision collision,PortCargo source=null)
    {
        if(safety!=null){if(source!=null)safety.Contact(collision,source.body,source.cargoId);return;}
        if(source!=null&&source!=CurrentCargo)return;
        if(!Active||!CanOperate||Paused)return;
        float speed=collision.relativeVelocity.magnitude;
        bool landing=collision.contactCount>0&&collision.GetContact(0).normal.y>.65f&&speed<1;
        if(landing||speed<.65f||Time.unscaledTime<contactCooldown)return;
        contactCooldown=Time.unscaledTime+.6f;legacyContacts++;Log?.Event("cargo_contact",collision.collider.name,speed);Play(impactSound);Tell("Contact with "+collision.collider.name+". Stop and reposition gently.");
    }
    public void Tell(string message){Feedback=message;nextFeedback=Time.unscaledTime+4;}
    void Play(AudioClip clip){if(clip!=null&&sfx!=null)sfx.PlayOneShot(clip,.55f);}
    public string Instruction
    {
        get
        {
            if(Paused)return "PAUSED. Select Resume on the console to continue.";
            if(traffic!=null&&traffic.CraneWaiting)return traffic.WaitInstruction;
            if(!string.IsNullOrEmpty(Feedback))return Feedback;
            switch(Stage)
            {
                case PortStage.Idle:return "Choose a layout, then Start operation. Five containers are ready in the yard; load them aboard in the marked order. Maya will guide each move.";
                case PortStage.TruckArrival:return $"Truck visit {incomingIndex+1}/{ContainerCount}: {CargoId} is arriving at the transfer stand.";
                case PortStage.TruckParking:return "The truck is parked and braking. The reachstacker needs clearance before it lifts.";
                case PortStage.HandlerApproach:return "The reachstacker is aligning with "+CargoId+" on the trailer.";
                case PortStage.HandlerLift:return CargoId+" is locked to the handler. Lift clear, then the empty truck departs.";
                case PortStage.HandlerCarry:return "The reachstacker is carrying "+CargoId+" to its yard position. The empty truck is departing.";
                case PortStage.HandlerLower:return "Lowering "+CargoId+" into "+(Plan.Initial(CargoId).tier>0?"the stack":"the yard")+". Wait for release and clearance.";
                case PortStage.HandlerClear:return CargoId+" is supported. The reachstacker is raising its head and reversing clear.";
                case PortStage.Reposition:return $"Placement accepted. Move the EMPTY spreader to {CargoId}; no HOME trip between moves. Raise before crossing stacks. {CurrentMove.note}";
                case PortStage.Pickup:
                    Vector3 gap=crane.CouplingPoint-CargoTop;gap.y=0;
                    if(ReadyToLock)return $"READY TO LOCK {CargoId}: {LockControl}.";
                    if(gap.magnitude>1.2f)return $"Pick up {CargoId}. Move the gantry / trolley toward its outline.\n"+AlignmentGuidance;
                    return $"Aligned over {CargoId}. Lower using {LowerControl}; fine control: {FineControl}.\n"+AlignmentGuidance;
                case PortStage.Transfer:
                    float distance=Vector3.ProjectOnPlane(cargo.position-TargetSlot.position,Vector3.up).magnitude;
                    if(crane.hookBody.position.y<TransitHeight-.3f&&distance>2)return $"Locked. Raise using {RaiseControl} to {TransitHeight:F0} m hook height before travelling.";
                    if(distance>.8f)return $"Carry {CargoId} to {TargetSlot.id}. Use the Monitor button for other views. Fine control: {FineControl}.";
                    return cargo.position.y>TargetSlot.position.y+.4f?"Over "+TargetSlot.id+". Lower gently onto "+(TargetSlot.supportCargoId==""?"the deck":TargetSlot.supportCargoId)+".":$"On the support: {LockControl} to release, then wait for acceptance.";
                case PortStage.Verification:return PlacementIssue()??$"Verifying supported placement: {StableSeconds:F1}/{config.placementDwell:F1}s.";
                case PortStage.Park:return "ALL CARGO LOADED. Return toward the turquoise quay ring and park the empty spreader at 10 m. Only one final parking trip.";
                case PortStage.Complete:return $"Layout complete: {Shipped}/{ContainerCount} aboard in {MoveIndex} moves. Please complete the feedback sliders on the console. Timing and move records are saved.";
                default:return "Operation stopped. Partial data is saved. Reset to restart.";
            }
        }
    }
    public string VoiceKey
    {
        get
        {
            if(Paused)return "paused";
            switch(Stage)
            {
                case PortStage.Idle:return "welcome";
                case PortStage.TruckArrival:return "truck_intake";
                case PortStage.HandlerClear:return "handler_clear";
                case PortStage.Complete:return "shipment_complete";
                case PortStage.Park:return "park";
                case PortStage.Pickup:
                case PortStage.Reposition:
                    if(ReadyToLock)return "couple";
                    return Vector3.ProjectOnPlane(crane.CouplingPoint-CargoTop,Vector3.up).magnitude<1.2f?"lower":"align";
                case PortStage.Transfer:
                    if(crane.hookBody.position.y<TransitHeight-.3f&&Vector3.ProjectOnPlane(cargo.position-TargetSlot.position,Vector3.up).magnitude>2)return "lift";
                    if(Vector3.ProjectOnPlane(cargo.position-TargetSlot.position,Vector3.up).magnitude>1)return "transfer";
                    return cargo.position.y>TargetSlot.position.y+.4f?"land":"release";
                case PortStage.Verification:return "settle";
                default:return "";
            }
        }
    }
    static bool Move(Transform obj,Vector3 goal,float speed,float dt){obj.position=Vector3.MoveTowards(obj.position,goal,speed*dt);return Vector3.Distance(obj.position,goal)<.01f;}
    static bool MoveFacing(Transform obj,Vector3 goal,float speed,float dt)
    {
        Vector3 delta=goal-obj.position;bool arrived=delta.magnitude<.05f;
        Quaternion target=arrived?Quaternion.identity:Quaternion.LookRotation(delta);
        obj.rotation=Quaternion.RotateTowards(obj.rotation,target,90*dt);
        if(!arrived)obj.position=Vector3.MoveTowards(obj.position,goal,speed*dt);return arrived;
    }
    static void FollowCurve(Transform obj,Vector3 a,Vector3 b,Vector3 c,Vector3 d,float t,bool reverse)
    {
        float s=1-t;Vector3 p=s*s*s*a+3*s*s*t*b+3*s*t*t*c+t*t*t*d;
        Vector3 direction=3*s*s*(b-a)+6*s*t*(c-b)+3*t*t*(d-c);if(reverse)direction=-direction;
        obj.SetPositionAndRotation(p,Quaternion.LookRotation(direction));
    }
    void OnApplicationFocus(bool focused){if(!focused&&!Application.isBatchMode&&Active&&!Paused)TogglePause();}
    void OnApplicationPause(bool paused){if(paused&&Active&&!Paused)TogglePause();}
    void OnApplicationQuit()=>Close();void OnDestroy()=>Close();
    void Close(){if(closed)return;closed=true;if(Active)Abort("application_closed");Log?.Dispose();Time.timeScale=1;}
}
