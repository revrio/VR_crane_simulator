using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

public sealed class ResearchCsv : IDisposable
{
    public const string Schema = "1";
    public string DirectoryPath { get; }
    public string Error { get; private set; }
    public bool Healthy => string.IsNullOrEmpty(Error);
    private StreamWriter events;
    private readonly string participant, layout, order, inputMode, startedUtc, build;
    private readonly bool practice;

    public ResearchCsv(string root, ResearchRun run, string participant, string layout, string order, bool practice, string inputMode, string build)
    {
        this.participant = participant; this.layout = layout; this.order = order;
        this.practice = practice; this.inputMode = inputMode; this.build = build;
        startedUtc = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture);
        DirectoryPath = Path.Combine(root, DateTime.UtcNow.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture) + "_" + run.Id);
        try
        {
            Directory.CreateDirectory(DirectoryPath);
            events = new StreamWriter(Path.Combine(DirectoryPath, "events.csv"), false, new UTF8Encoding(false)) { AutoFlush = true };
            events.WriteLine("schema_version,run_id,task_id,active_seconds,event_type,object_id,value");
        }
        catch (Exception ex) { Fail(ex); }
    }

    public static string Cell(object value)
    {
        string text = value is IFormattable formattable ? formattable.ToString(null, CultureInfo.InvariantCulture) : value?.ToString() ?? "";
        // A tab keeps user-supplied IDs from becoming executable spreadsheet formulas.
        if (value is string && text.Length > 0 && "=+-@".IndexOf(text[0]) >= 0) text = "\t" + text;
        return text.IndexOfAny(new[] { ',', '"', '\r', '\n' }) >= 0 ? "\"" + text.Replace("\"", "\"\"") + "\"" : text;
    }
    private static string Row(params object[] values) => string.Join(",", values.Select(Cell));
    private void Fail(Exception ex) { Error = ex.Message; }

    public void Event(ResearchRun run, string type, string objectId, object value = null)
    {
        try { events?.WriteLine(Row(Schema, run.Id, run.Current?.task ?? 0, run.ActiveSeconds, type, objectId, value)); }
        catch (Exception ex) { Fail(ex); }
    }

    public void Checkpoint(ResearchRun run)
    {
        try
        {
            WriteSnapshot("runs.csv", "schema_version,build_id,run_id,participant_id,layout_id,condition_order,practice,input_mode,start_utc,active_seconds,status\n" +
                Row(Schema, build, run.Id, participant, layout, order, practice, inputMode, startedUtc, run.ActiveSeconds, run.State) + "\n");
            var data = new StringBuilder("schema_version,run_id,task_id,load_id,target_id,pickup_s,transfer_s,placement_s,return_s,cue_to_placement_s,cycle_s,collisions,wrong_targets,high_input_events,slew_degrees,trolley_m,hoist_m,load_path_m,outcome\n");
            foreach (var t in run.Tasks)
                data.AppendLine(Row(Schema, run.Id, t.task, t.loadId, t.targetId, t.pickupSeconds, t.transferSeconds,
                    t.placementSeconds, t.returnSeconds, t.PlacementTime, t.TotalSeconds, t.collisions, t.wrongTargets,
                    t.highInputs, t.slewDegrees, t.trolleyMetres, t.hoistMetres, t.loadPathMetres, t.outcome));
            WriteSnapshot("tasks.csv", data.ToString());
        }
        catch (Exception ex) { Fail(ex); }
    }

    public void WriteSetup(string json)
    {
        try { WriteSnapshot("setup.json", json); } catch (Exception ex) { Fail(ex); }
    }

    public void Rating(ResearchRun run, string name, string timing, float value)
    {
        try
        {
            string path = Path.Combine(DirectoryPath, "ratings.csv");
            bool exists = File.Exists(path);
            using (var writer = new StreamWriter(path, true, new UTF8Encoding(false)))
            {
                if (!exists) writer.WriteLine("schema_version,run_id,rating_name,timing,value,scale_definition");
                writer.WriteLine(Row(Schema, run.Id, name, timing, value, "Custom self-report: 0 none, 10 maximum; not a validated fatigue index"));
            }
        }
        catch (Exception ex) { Fail(ex); }
    }

    private void WriteSnapshot(string name, string content)
    {
        string path = Path.Combine(DirectoryPath, name);
        string temporary = path + ".tmp";
        File.WriteAllText(temporary, content, new UTF8Encoding(false));
        if (File.Exists(path)) File.Replace(temporary, path, null); else File.Move(temporary, path);
    }
    public void Dispose() { try { events?.Dispose(); } catch (Exception ex) { Fail(ex); } events = null; }
}
