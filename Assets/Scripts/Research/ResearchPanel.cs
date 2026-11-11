using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ResearchPanel : MonoBehaviour
{
    public static readonly string[] Orders = { "AB", "BA", "ABC", "ACB", "BAC", "BCA", "CAB", "CBA" };
    public ExperimentSession session;
    public ResearchOperatorView view;
    public TextMeshProUGUI heading, state, timer, job, guidance, results, export;
    public TMP_InputField participant;
    public TMP_Dropdown order;
    public Slider[] discomfort;
    public TextMeshProUGUI[] discomfortLabels;
    public Button layoutA, layoutB, layoutC, practice, start, pause, reset, recenter, saveRatings, next, openData;
    private float nextRefresh;
    private void Start()
    {
        participant.text = session.participantId;
        participant.onValueChanged.AddListener(value => { if (!session.IsActive) session.participantId = value; });
        order.SetValueWithoutNotify(Mathf.Max(0, Array.IndexOf(Orders, session.conditionOrder)));
        order.onValueChanged.AddListener(value => { if (!session.IsActive) session.conditionOrder = Orders[value]; });
        discomfort[0].onValueChanged.AddListener(value => { if (!session.IsActive) session.neckBefore = value; });
        discomfort[1].onValueChanged.AddListener(value => { if (!session.IsActive) session.shoulderBefore = value; });
        discomfort[2].onValueChanged.AddListener(value => { if (!session.IsActive) session.handBefore = value; });
        layoutA.onClick.AddListener(() => session.SelectLayout(session.layoutA));
        layoutB.onClick.AddListener(() => session.SelectLayout(session.layoutB));
        if (layoutC != null) layoutC.onClick.AddListener(() => session.SelectLayout(session.layoutC));
        practice.onClick.AddListener(() => Begin(true));
        start.onClick.AddListener(() => Begin(false));
        pause.onClick.AddListener(session.TogglePause);
        reset.onClick.AddListener(session.ResetStation);
        recenter.onClick.AddListener(view.Recenter);
        openData.onClick.AddListener(() => Application.OpenURL(new Uri(session.DataPath + System.IO.Path.DirectorySeparatorChar).AbsoluteUri));
        saveRatings.onClick.AddListener(() => session.SavePostRatings(discomfort[0].value, discomfort[1].value, discomfort[2].value));
        next.onClick.AddListener(session.SelectNextLayout);
    }
    private void Begin(bool isPractice)
    {
        session.participantId = participant.text;
        session.conditionOrder = Orders[order.value];
        session.neckBefore = discomfort[0].value;
        session.shoulderBefore = discomfort[1].value;
        session.handBefore = discomfort[2].value;
        session.BeginRun(isPractice);
    }
    private void Update()
    {
        if (Time.unscaledTime < nextRefresh) return;
        nextRefresh = Time.unscaledTime + 0.1f;
        bool active = session.IsActive;
        var run = session.Run;
        heading.text = session.CurrentLayoutId == "C" ? "C / STACKED PICKUP" : session.CurrentLayoutId == "A" ? "A / DISPERSED BAYS" : "B / CONSOLIDATED BAYS";
        order.SetValueWithoutNotify(Mathf.Max(0, Array.IndexOf(Orders, session.conditionOrder)));
        state.text = run == null ? "READY FOR CALIBRATION" : (session.Practice ? "PRACTICE / " : "MEASURED / ") + run.State.ToString().ToUpperInvariant();
        double seconds = run?.ActiveSeconds ?? 0;
        timer.text = $"{(int)seconds / 60:00}:{seconds % 60:00.0}";
        job.text = run?.Current == null ? "3 CONTAINERS / 4 WORK ELEMENTS" : $"JOB {run.Current.task} / {session.loads.Length}   /   {run.Phase.ToString().ToUpperInvariant()}";
        guidance.text = session.Message;
        if (run?.Current != null)
        {
            var t = run.Current;
            results.text = $"PICKUP  {t.pickupSeconds:F1}s     TRANSFER  {t.transferSeconds:F1}s\nPLACEMENT  {t.placementSeconds:F1}s     RETURN  {t.returnSeconds:F1}s\nIMPACTS  {run.Tasks.Sum(x => x.collisions)}    WRONG BAYS  {run.Tasks.Sum(x => x.wrongTargets)}";
            if (run.State == ResearchRunState.Completed)
                results.text += $"\nMEAN CYCLE  {run.Tasks.Average(x => x.TotalSeconds):F1}s  /  PLACED {session.PlacedCount}";
        }
        else results.text = "Element times and handling errors appear here.\nHigh joystick input is logged separately from impacts.";
        bool failed = session.Logger != null && !session.Logger.Healthy;
        export.text = failed ? "EXPORT FAILED: " + session.Logger.Error : session.Logger == null ? "CSV checkpoints are saved every 2 seconds during a run." :
            run == null ? "PREVIOUS RUN SAVED / Open CSV folder" : "CSV SAVED / " + (session.Practice ? "PRACTICE" : "MEASURED") + " / " + run.Id.Substring(0, 8);
        export.color = failed ? new Color(1, 0.35f, 0.25f) : new Color(0.45f, 0.82f, 0.75f);
        layoutA.interactable = layoutB.interactable = practice.interactable = start.interactable = recenter.interactable = next.interactable = !active;
        if (layoutC != null) layoutC.interactable = !active;
        participant.interactable = order.interactable = !active;
        pause.interactable = active;
        pause.GetComponentInChildren<TextMeshProUGUI>().text = run != null && run.State == ResearchRunState.Paused ? "Resume [Esc]" : "Pause [Esc]";
        saveRatings.interactable = run != null && !active;
        openData.interactable = !active && session.Logger != null && Application.platform != RuntimePlatform.Android;
        for (int i = 0; i < discomfort.Length; i++)
        {
            discomfort[i].interactable = !active;
            discomfortLabels[i].text = new[] { "Neck", "Shoulders", "Hands" }[i] + "  " + discomfort[i].value.ToString("0");
        }
    }
}
