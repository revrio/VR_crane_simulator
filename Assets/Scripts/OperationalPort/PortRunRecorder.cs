using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

// Operational exports are separate from schema-1 three-delivery research records.
public sealed class PortRunRecorder : IDisposable
{
    [Serializable] public class MoveRecord
    {
        public string id,cargo,source,target,kind,outcome="in_progress";
        public int sourceTier,targetTier;
        public double reposition,pickup,transfer,placement;
        public double crossingWait;
        public double Total=>reposition+pickup+transfer+placement;
    }
    public readonly string Id=Guid.NewGuid().ToString("N"), DirectoryPath;
    public readonly Dictionary<string,double> Phases=new Dictionary<string,double>();
    public readonly Dictionary<string,double> Delays=new Dictionary<string,double>();
    public string DelayTag="";
    [Serializable] public class IncidentRecord
    {
        public string id,moveId,source,target,type,severity,outcome="open";
        public double start,end,ackLatency=-1,resumeAt=-1;
        public float peakSpeed,peakImpulse,x,y,z;
    }
    public readonly List<IncidentRecord> Incidents=new List<IncidentRecord>();
    public double Seconds { get; private set; }
    public bool Paused, Running=true;
    public string Phase="TruckArrival", Error;
    public string Status="Running";
    public readonly System.Collections.Generic.List<MoveRecord> Moves=new System.Collections.Generic.List<MoveRecord>();
    public MoveRecord CurrentMove {get;private set;}
    readonly string version;
    readonly bool automated;
    readonly string inputMode;
    readonly int boxes;
    readonly string layout,intake,schema,traffic;
    public string Schema=>schema;
    StreamWriter events;
    double last;
    public PortRunRecorder(string root,string build,bool test,double now,string input="Desktop",int count=1,string preset="SINGLE",string intakeMode="FullIntake",string trafficMode=null)
    {
        version=build;automated=test;last=now;inputMode=input;boxes=count;layout=preset;intake=intakeMode;traffic=trafficMode;schema=trafficMode!=null?"op3":count>1?"op2":"op1";
        DirectoryPath=Path.Combine(root,DateTime.UtcNow.ToString("yyyyMMdd_HHmmss")+"_"+Id);
        try
        {
            Directory.CreateDirectory(DirectoryPath);
            events=new StreamWriter(Path.Combine(DirectoryPath,"events.csv"),false,new UTF8Encoding(false)){AutoFlush=true};
            events.WriteLine(schema!="op1"?"schema_version,run_id,move_id,active_seconds,phase,event_type,object_id,value":"schema_version,run_id,active_seconds,phase,event_type,object_id,value");
            Save();
        }
        catch(Exception e){Error=e.Message;}
    }
    public void Advance(double now)
    {
        if(double.IsNaN(now)||double.IsInfinity(now)||now<last)return;
        double dt=now-last;last=now;
        if(!Running||Paused)return;
        Seconds+=dt;Phases[Phase]=(Phases.TryGetValue(Phase,out double value)?value:0)+dt;
        if(!string.IsNullOrEmpty(DelayTag))
        {
            Delays[DelayTag]=(Delays.TryGetValue(DelayTag,out double delay)?delay:0)+dt;
            if(CurrentMove!=null){if(DelayTag=="CrossingWait")CurrentMove.crossingWait+=dt;}
        }
        if(CurrentMove!=null&&CurrentMove.outcome=="in_progress")
            switch(Phase){case "Reposition":CurrentMove.reposition+=dt;break;case "Pickup":CurrentMove.pickup+=dt;break;case "Transfer":CurrentMove.transfer+=dt;break;case "Verification":CurrentMove.placement+=dt;break;}
    }
    public void BeginMove(PortMoveSpec move,PortSlotSpec source,PortSlotSpec target)
    {
        CurrentMove=new MoveRecord{id=move.id,cargo=move.cargoId,source=source?.id??"RECOVERY",target=target.id,kind=move.kind,sourceTier=source?.tier??0,targetTier=target.tier};Moves.Add(CurrentMove);
        Event("move_start",move.cargoId,move.kind+" → "+target.id);
    }
    public void FinishMove(){if(CurrentMove!=null)CurrentMove.outcome="completed";Event("move_complete",CurrentMove?.cargo,CurrentMove?.id);CurrentMove=null;}
    public void InterruptMove(){if(CurrentMove!=null)CurrentMove.outcome="aborted";}
    public void Event(string type,string obj,object value=null)
    {
        try{events?.WriteLine(schema!="op1"?Row(schema,Id,CurrentMove?.id??"",Seconds,Phase,type,obj,value):Row(schema,Id,Seconds,Phase,type,obj,value));}catch(Exception e){Error=e.Message;}
    }
    public void Save()
    {
        try
        {
            Write("runs.csv",schema=="op3"?"schema_version,build_id,run_id,practice,automated,input_mode,container_count,layout_id,intake_mode,active_seconds,status,traffic_mode\n"+Row(schema,version,Id,true,automated,inputMode,boxes,layout,intake,Seconds,Status,traffic)+"\n":schema=="op2"?"schema_version,build_id,run_id,practice,automated,input_mode,container_count,layout_id,intake_mode,active_seconds,status\n"+Row(schema,version,Id,true,automated,inputMode,boxes,layout,intake,Seconds,Status)+"\n":"schema_version,build_id,run_id,practice,automated,input_mode,container_count,active_seconds,status\n"+Row(schema,version,Id,true,automated,inputMode,boxes,Seconds,Status)+"\n");
            var rows=new StringBuilder("schema_version,run_id,phase,seconds\n");
            foreach(var phase in Phases)rows.AppendLine(Row(schema,Id,phase.Key,phase.Value));
            Write("phase_times.csv",rows.ToString());
            if(schema!="op1")
            {
                var moves=new StringBuilder("schema_version,run_id,move_id,cargo_id,source_slot,target_slot,operation,source_tier,target_tier,reposition_s,pickup_s,transfer_s,placement_s,move_s,outcome"+(schema=="op3"?",crossing_wait_s":"")+"\n");
                foreach(var move in Moves){string row=Row(schema,Id,move.id,move.cargo,move.source,move.target,move.kind,move.sourceTier,move.targetTier,move.reposition,move.pickup,move.transfer,move.placement,move.Total,move.outcome);moves.AppendLine(row+(schema=="op3"?","+Row(move.crossingWait):""));}
                Write("moves.csv",moves.ToString());
            }
            if(schema=="op3")
            {
                var delays=new StringBuilder("schema_version,run_id,cause,seconds\n");foreach(var delay in Delays)delays.AppendLine(Row(schema,Id,delay.Key,delay.Value));Write("delays.csv",delays.ToString());
                var incidents=new StringBuilder("schema_version,run_id,episode_id,move_id,source_id,target_id,type,severity,start_s,end_s,duration_s,peak_relative_speed_mps,peak_impulse_ns,x,y,z,outcome,ack_latency_s,resume_s\n");
                foreach(var item in Incidents)incidents.AppendLine(Row(schema,Id,item.id,item.moveId,item.source,item.target,item.type,item.severity,item.start,item.end,Math.Max(0,item.end-item.start),item.peakSpeed,item.peakImpulse,item.x,item.y,item.z,item.outcome,item.ackLatency<0?null:(object)item.ackLatency,item.resumeAt<0?null:(object)item.resumeAt));
                Write("incidents.csv",incidents.ToString());
            }
        }catch(Exception e){Error=e.Message;}
    }
    public void Setup(string json){try{Write("scenario.json",json);}catch(Exception e){Error=e.Message;}}
    void Write(string name,string text){string path=Path.Combine(DirectoryPath,name);File.WriteAllText(path+".tmp",text,new UTF8Encoding(false));File.Copy(path+".tmp",path,true);File.Delete(path+".tmp");}
    static string Row(params object[] values)=>string.Join(",",values.Select(ResearchCsv.Cell));
    public void Dispose(){Save();events?.Dispose();events=null;}
}
