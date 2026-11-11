using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[DefaultExecutionOrder(-200)]
public class PortSafety : MonoBehaviour
{
    public PortDirector director;
    public PortObstacle[] obstacles;
    public PortImpactFeedback feedback;
    public int ContactCount {get;private set;}
    public int NearMissCount {get;private set;}
    public string ClearanceText {get;private set;}="";
    class Episode
    {
        public string key;public double first,last;
        public float speed,impulse;public Vector3 point;public bool raised,contact,near;
        public PortRunRecorder.IncidentRecord record;
    }
    readonly Dictionary<string,Episode> contacts=new Dictionary<string,Episode>(),near=new Dictionary<string,Episode>();
    double running;
    Collider[] hookShapes;
    public void ResetSafety()
    {
        contacts.Clear();near.Clear();ClearanceText="";
        ContactCount=NearMissCount=0;running=0;if(feedback!=null)feedback.ResetEffects();
        hookShapes=director.crane.hookBody.GetComponents<Collider>();
    }
    void FixedUpdate()
    {
        if(!director.Active||director.Paused)return;
        running+=Time.fixedDeltaTime;
        if(!director.CanOperate)return;
        CheckClearance();
        foreach(var item in contacts.Values.ToArray())if(running-item.last>.25){Close(item,"cleared");contacts.Remove(item.key);}
    }
    void Update()
    {
        if(director.Log==null)return;
        director.Log.Advance(Time.realtimeSinceStartupAsDouble);
        foreach(var item in director.Log.Incidents)if(item.outcome=="open")item.end=director.Log.Seconds;
    }
    public void Contact(Collision collision,Rigidbody source,string sourceId)
    {
        if(!director.Active||director.Paused||!director.CanOperate||source==null)return;
        bool hook=source==director.crane.hookBody;
        if(!hook&&(source!=director.cargo||director.Custody!=CargoCustody.Crane&&director.Custody!=CargoCustody.Free))return;
        var obstacle=collision.collider.GetComponentInParent<PortObstacle>();
        string target=obstacle!=null?obstacle.Id:collision.collider.name;
        if(collision.rigidbody==director.crane.hookBody||collision.rigidbody==source)return;
        if(hook&&director.Attached&&collision.rigidbody==director.cargo)return;
        float speed=collision.relativeVelocity.magnitude;
        bool support=obstacle!=null&&(obstacle.kind==PortObstacleKind.Deck||obstacle.kind==PortObstacleKind.Cargo);
        bool gentle=!hook&&support&&collision.contactCount>0&&collision.GetContact(0).normal.y>.65f&&speed<.8f&&Vector3.Angle(source.transform.up,Vector3.up)<7;
        Vector3 coupling=director.crane.CouplingPoint-director.CargoTop;
        bool roofApproach=hook&&collision.rigidbody==director.cargo&&new Vector2(coupling.x,coupling.z).magnitude<.65f&&collision.contactCount>0&&collision.GetContact(0).normal.y>.65f&&speed<.8f;
        if(gentle||roofApproach)return;
        string rig=director.Attached?director.CargoId:sourceId,key=rig+"|"+target;
        if(!contacts.TryGetValue(key,out var item)){item=new Episode{key=key,first=running};contacts.Add(key,item);}
        item.last=running;item.speed=Mathf.Max(item.speed,speed);item.impulse=Mathf.Max(item.impulse,collision.impulse.magnitude);
        item.point=collision.contactCount>0?collision.GetContact(0).point:collision.collider.ClosestPoint(source.position);
        bool person=obstacle!=null&&obstacle.kind==PortObstacleKind.Worker;
        if(item.record==null&&(speed>=.12f||running-item.first>=.3||person))
        {
            ContactCount++;item.record=Record(rig,target,"Contact",person?"PersonContact":item.speed>=director.config.strongImpactSpeed?"Strong":"Scrape",item.point);
            director.Log?.Event("contact_start",target,item.record.id);feedback?.Impact(item.point,item.speed>=director.config.strongImpactSpeed||person);
            director.Tell("Contact with "+target+". Reposition gently.");
        }
        if(item.record!=null){item.record.peakSpeed=item.speed;item.record.peakImpulse=item.impulse;}
        if(near.TryGetValue(key,out var close)){close.contact=true;}
    }
    void CheckClearance()
    {
        ClearanceText="";float smallest=float.PositiveInfinity;
        bool loaded=director.Attached||director.Custody==CargoCustody.Free;
        Collider source=loaded?director.CurrentCargo.footprint:director.crane.hookBody.GetComponent<Collider>();
        Bounds sourceBounds=loaded?source.bounds:HookBounds();
        foreach(var obstacle in obstacles)
        {
            if(obstacle==null||obstacle.shape==null||!obstacle.gameObject.activeInHierarchy||obstacle.kind==PortObstacleKind.Deck)continue;
            if(source==obstacle.shape||obstacle.shape.attachedRigidbody==source.attachedRigidbody)continue;
            if(IntendedStackSupport(obstacle))continue;
            if(!loaded&&obstacle.kind==PortObstacleKind.Cargo)
            {Vector3 delta=director.crane.CouplingPoint-(obstacle.shape.bounds.center+Vector3.up*director.config.cargoSize.y*.5f);if(new Vector2(delta.x,delta.z).magnitude<.8f&&delta.y>=-.3f)continue;}
            float gap=Gap(sourceBounds,obstacle.shape.bounds),threshold=obstacle.kind==PortObstacleKind.Worker?director.config.personClearance:director.config.nearClearance;
            string rig=director.Attached?director.CargoId:source==director.CurrentCargo.footprint?director.CargoId:"SPREADER",key=rig+"|"+obstacle.Id;
            if(gap<threshold)
            {
                if(gap<smallest){smallest=gap;ClearanceText=$"CLEARANCE {gap:F2} m / {obstacle.Id}. Slow down and leave space.";}
                if(!near.TryGetValue(key,out var e)){e=new Episode{key=key,first=running,record=Record(rig,obstacle.Id,"Clearance","NearMiss",obstacle.shape.ClosestPoint(sourceBounds.center),false)};near.Add(key,e);}
                e.last=running;
                if(contacts.TryGetValue(key,out var contact)&&contact.record!=null)e.contact=true;
            }
        }
        foreach(var e in near.Values.ToArray())if(running-e.last>.3)
        {
            if(!e.contact){e.record.type="NearMiss";e.record.end=director.Log?.Seconds??0;e.record.outcome="cleared";director.Log?.Incidents.Add(e.record);NearMissCount++;director.Log?.Event("near_miss",e.record.target,e.record.id);}
            near.Remove(e.key);
        }
    }
    bool IntendedStackSupport(PortObstacle obstacle)
    {
        if(director.Custody!=CargoCustody.Crane&&director.Custody!=CargoCustody.Free||obstacle.kind!=PortObstacleKind.Cargo)return false;
        var delta=director.cargo.position-obstacle.shape.bounds.center;
        return delta.y>=director.config.cargoSize.y-.25f&&new Vector2(delta.x,delta.z).magnitude<.8f;
    }
    Bounds HookBounds()
    {
        if(hookShapes==null||hookShapes.Length==0)hookShapes=director.crane.hookBody.GetComponents<Collider>();
        Bounds bounds=hookShapes[0].bounds;for(int i=1;i<hookShapes.Length;i++)bounds.Encapsulate(hookShapes[i].bounds);return bounds;
    }
    public static float Gap(Bounds a,Bounds b)
    {
        Vector3 delta=new Vector3(Mathf.Max(0,Mathf.Max(a.min.x-b.max.x,b.min.x-a.max.x)),Mathf.Max(0,Mathf.Max(a.min.y-b.max.y,b.min.y-a.max.y)),Mathf.Max(0,Mathf.Max(a.min.z-b.max.z,b.min.z-a.max.z)));
        return delta.magnitude;
    }
    PortRunRecorder.IncidentRecord Record(string source,string target,string type,string severity,Vector3 point,bool add=true)
    {
        var row=new PortRunRecorder.IncidentRecord{id=Guid.NewGuid().ToString("N").Substring(0,12),moveId=director.Log?.CurrentMove?.id??"",source=source,target=target,type=type,severity=severity,start=director.Log?.Seconds??0,end=director.Log?.Seconds??0,x=point.x,y=point.y,z=point.z};
        if(add)director.Log?.Incidents.Add(row);return row;
    }
    void Close(Episode item,string outcome)
    {
        if(item.record==null)return;item.record.end=director.Log?.Seconds??0;item.record.outcome=outcome;director.Log?.Event("contact_end",item.record.target,item.record.id);
    }
    public void EndRun(string outcome)
    {
        director.Log?.Advance(Time.realtimeSinceStartupAsDouble);
        foreach(var item in contacts.Values)Close(item,outcome);contacts.Clear();
        foreach(var item in near.Values)if(!item.contact){item.record.end=director.Log?.Seconds??0;item.record.outcome=outcome;director.Log?.Incidents.Add(item.record);}near.Clear();
    }
}
