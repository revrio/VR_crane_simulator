using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum PortLayout { Accessible,Stacked,Priority,StackPractice }
[Serializable] public class PortSlotSpec
{
    public string id,area,supportCargoId="";
    public Vector3 position;
    public int tier;
    public Vector2 footprint;
}
[Serializable] public class PortMoveSpec
{
    public string id,cargoId,destinationId,kind,note;
}
[Serializable] public class PortShipmentPlan
{
    public string layoutId,label;
    public string[] cargoIds,initialSlotIds;
    public PortSlotSpec[] slots;
    public PortMoveSpec[] moves;
    public PortSlotSpec Slot(string id)=>slots.FirstOrDefault(s=>s.id==id);
    public PortSlotSpec Initial(string cargoId)=>Slot(initialSlotIds[Array.IndexOf(cargoIds,cargoId)]);
    public static PortShipmentPlan Create(PortScenario config,PortLayout layout)
    {
        var result=new PortShipmentPlan();var slots=new List<PortSlotSpec>();var moves=new List<PortMoveSpec>();
        if(!config.multipleCargo)
        {
            result.layoutId="SINGLE";result.label="One-container chain";result.cargoIds=new[]{"C1"};result.initialSlotIds=new[]{"Y-C1"};
            slots.Add(Slot("Y-C1","Yard",config.yard,0,""));slots.Add(Slot("SHIP-01","Ship",config.ship,0,""));
            moves.Add(Move("C1","SHIP-01","Ship","Load C1 aboard."));
        }
        else
        {
            result.layoutId=new[]{"A","B","C","D"}[(int)layout];
            result.label=new[]{"Accessible staging","Compact stacks","Priority retrieval","Build a stack"}[(int)layout];
            result.cargoIds=Enumerable.Range(1,5).Select(i=>"C"+i).ToArray();result.initialSlotIds=result.cargoIds.Select(id=>"Y-"+id).ToArray();
            var points=new[]{new Vector3(-7.5f,1.3f,-12),new Vector3(0,1.3f,-12),new Vector3(7.5f,1.3f,-12),new Vector3(-3.5f,1.3f,-5),new Vector3(3.5f,1.3f,-5)};
            string[] support={"","","","",""};
            if(layout==PortLayout.Stacked)
            {
                points=new[]{new Vector3(-6,6.5f,-12),new Vector3(-6,3.9f,-12),new Vector3(-6,1.3f,-12),new Vector3(6,3.9f,-12),new Vector3(6,1.3f,-12)};
                support=new[]{"C2","C3","","C5",""};
            }
            else if(layout==PortLayout.Priority)
            {
                points=new[]{new Vector3(8,3.9f,-12),new Vector3(-8,3.9f,-12),new Vector3(0,1.3f,-5),new Vector3(8,1.3f,-12),new Vector3(-8,1.3f,-12)};
                support=new[]{"C4","C5","","",""};
            }
            for(int i=0;i<5;i++)slots.Add(Slot("Y-"+result.cargoIds[i],"Yard",points[i],Mathf.RoundToInt((points[i].y-1.3f)/2.6f),support[i]));
            slots.Add(Slot("TEMP-01","Temp",new Vector3(0,1.3f,layout==PortLayout.Priority?-12:-4),0,""));
            int[] order=layout==PortLayout.Priority?new[]{5,1,3,2,4}:layout==PortLayout.StackPractice?new[]{2,1,3,4,5}:new[]{1,2,3,4,5};
            if(layout==PortLayout.Priority)moves.Add(Move("C2","TEMP-01","Rehandle","C5 is blocked by C2. Move C2 to TEMP-01 first."));
            if(layout==PortLayout.StackPractice)
            {
                slots.Add(Slot("Y-STACK","Yard",points[0]+Vector3.up*2.6f,1,"C1"));
                moves.Add(Move("C2","Y-STACK","Stack","Build a two-high stack: place C2 on C1. Then unstack it for the ship."));
            }
            for(int i=0;i<5;i++)
            {
                int column=i<3?i:i-3,tier=i<3?0:1;
                string supportId=tier==0?"":"C"+order[column];
                string slot="SHIP-"+(column+1).ToString("00")+(tier==0?"":"-T2");
                slots.Add(Slot(slot,"Ship",new Vector3((column-1)*7.5f,4.3f+tier*2.6f,16),tier,supportId));
                moves.Add(Move("C"+order[i],slot,"Ship",tier>0?$"Stack C{order[i]} on {supportId} aboard the ship.":$"Load C{order[i]} into {slot}."));
            }
        }
        for(int i=0;i<moves.Count;i++)moves[i].id="M"+(i+1).ToString("000");
        result.slots=slots.ToArray();result.moves=moves.ToArray();return result;
    }
    static PortSlotSpec Slot(string id,string area,Vector3 p,int tier,string support)=>new PortSlotSpec{id=id,area=area,position=p,tier=tier,supportCargoId=support,footprint=tier==0?new Vector2(7.5f,3.7f):new Vector2(6.2f,2.6f)};
    static PortMoveSpec Move(string cargo,string target,string kind,string note)=>new PortMoveSpec{cargoId=cargo,destinationId=target,kind=kind,note=note};

    // Simulate slot occupancy before play so impossible orders cannot deadlock a mission.
    public string Validate(PortScenario config)
    {
        if(cargoIds==null||cargoIds.Length==0||cargoIds.Distinct().Count()!=cargoIds.Length)return "Cargo IDs must be nonempty and unique.";
        if(initialSlotIds.Length!=cargoIds.Length||slots.Select(s=>s.id).Distinct().Count()!=slots.Length)return "Invalid slot identities.";
        foreach(var slot in slots)
            if(slot.position.x<config.gantryLimits.x||slot.position.x>config.gantryLimits.y||slot.position.z<config.trolleyLimits.x||slot.position.z>config.trolleyLimits.y||slot.position.y+1.75f>config.trolleyHeight-3)
                return "Unreachable slot: "+slot.id;
        var occupied=cargoIds.Select((id,i)=>new{id,slot=initialSlotIds[i]}).ToDictionary(x=>x.id,x=>x.slot);
        foreach(var move in moves)
        {
            if(!occupied.ContainsKey(move.cargoId)||Slot(move.destinationId)==null)return "Move references missing cargo or target.";
            if(occupied.Any(x=>x.Key!=move.cargoId&&Slot(x.Value)?.supportCargoId==move.cargoId))return "Blocked pickup in "+move.id;
            var dest=Slot(move.destinationId);
            if(occupied.Any(x=>x.Key!=move.cargoId&&x.Value==dest.id))return "Occupied target: "+dest.id;
            if(!string.IsNullOrEmpty(dest.supportCargoId))
            {
                if(!occupied.TryGetValue(dest.supportCargoId,out var supportSlot))return "Missing support cargo.";
                var below=Slot(supportSlot);
                if(below==null||Vector3.Distance(below.position,dest.position-Vector3.up*config.cargoSize.y)>.05f)return "Support is not positioned below "+dest.id;
            }
            occupied[move.cargoId]=dest.id;
        }
        if(occupied.Values.Any(s=>Slot(s).area!="Ship"))return "Manifest leaves cargo outside the ship.";
        return null;
    }
}
