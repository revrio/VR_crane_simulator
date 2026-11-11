using System;
using System.Collections.Generic;

public enum ResearchRunState { Running, Paused, Completed, Aborted }
public enum ResearchPhase { Pickup, Transfer, Placement, Return }

[Serializable]
public sealed class ResearchTaskRecord
{
    public int task;
    public string loadId;
    public string targetId;
    public string outcome = "in_progress";
    public double pickupSeconds, transferSeconds, placementSeconds, returnSeconds;
    public int collisions, wrongTargets, highInputs;
    public double slewDegrees, trolleyMetres, hoistMetres, loadPathMetres;
    public double TotalSeconds => pickupSeconds + transferSeconds + placementSeconds + returnSeconds;
    public double PlacementTime => pickupSeconds + transferSeconds + placementSeconds;

    public void AddTime(ResearchPhase phase, double seconds)
    {
        switch (phase)
        {
            case ResearchPhase.Pickup: pickupSeconds += seconds; break;
            case ResearchPhase.Transfer: transferSeconds += seconds; break;
            case ResearchPhase.Placement: placementSeconds += seconds; break;
            case ResearchPhase.Return: returnSeconds += seconds; break;
        }
    }
}

// No Unity dependency: a monotonic clock is supplied by the caller and can be tested deterministically.
public sealed class ResearchRun
{
    public readonly string Id = Guid.NewGuid().ToString("N");
    public readonly List<ResearchTaskRecord> Tasks = new List<ResearchTaskRecord>();
    public ResearchRunState State { get; private set; } = ResearchRunState.Running;
    public ResearchPhase Phase { get; private set; }
    public double ActiveSeconds { get; private set; }
    public ResearchTaskRecord Current => Tasks.Count == 0 ? null : Tasks[Tasks.Count - 1];
    private double lastClock;

    public ResearchRun(double now) { lastClock = now; }

    public void Advance(double now)
    {
        if (double.IsNaN(now) || double.IsInfinity(now) || now < lastClock) return;
        double elapsed = now - lastClock;
        lastClock = now;
        if (State != ResearchRunState.Running) return;
        if (Current != null && Current.outcome == "in_progress")
        {
            ActiveSeconds += elapsed;
            Current.AddTime(Phase, elapsed);
        }
    }

    public void BeginTask(string load, string target, double now)
    {
        Advance(now);
        if (State != ResearchRunState.Running || (Current != null && Current.outcome == "in_progress"))
            throw new InvalidOperationException("Finish the current task before starting another.");
        Tasks.Add(new ResearchTaskRecord { task = Tasks.Count + 1, loadId = load, targetId = target });
        Phase = ResearchPhase.Pickup;
    }

    public bool SetPhase(ResearchPhase phase, double now)
    {
        Advance(now);
        if (State != ResearchRunState.Running || Current == null || Current.outcome != "in_progress" || (int)phase != (int)Phase + 1)
            return false;
        Phase = phase;
        return true;
    }

    public bool CompleteTask(double now)
    {
        Advance(now);
        if (State != ResearchRunState.Running || Current == null || Current.outcome != "in_progress" || Phase != ResearchPhase.Return) return false;
        Current.outcome = "completed";
        return true;
    }

    public void Pause(double now) { Advance(now); if (State == ResearchRunState.Running) State = ResearchRunState.Paused; }
    public void Resume(double now) { Advance(now); if (State == ResearchRunState.Paused) State = ResearchRunState.Running; }
    public void Finish(double now)
    {
        Advance(now);
        if (State == ResearchRunState.Running && Current != null && Current.outcome == "completed") State = ResearchRunState.Completed;
    }
    public void Abort(double now, string reason = "aborted")
    {
        Advance(now);
        if (State == ResearchRunState.Completed || State == ResearchRunState.Aborted) return;
        if (Current != null && Current.outcome == "in_progress") Current.outcome = reason;
        State = ResearchRunState.Aborted;
    }
}
