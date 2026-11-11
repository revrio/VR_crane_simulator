using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// One instructor drives both desktop and tracked-ray consoles. No network or TTS runtime is required.
public class ResearchGuide : MonoBehaviour
{
    public ExperimentSession session;
    public ResearchOperatorView view;
    public TextMeshProUGUI[] captions, readouts;
    public RawImage[] portraits;
    public Transform avatarHead, avatarHand, avatarMouth;
    public Camera portraitCamera;
    public AudioClip[] voiceClips;
    public string[] voiceKeys;
    public AudioClip motorClip, successClip, ambientClip;
    public LineRenderer objectiveRing, hookRing, route;
    public TextMeshPro objectiveLabel;
    public string Instruction { get; private set; }
    public string Cue { get; private set; }
    public bool VoiceMuted { get; private set; }
    public bool IsSpeaking => voice != null && voice.isPlaying && !VoiceMuted;
    private AudioSource voice, motor, feedback, ambience;
    private RenderTexture portrait;
    private float nextVoice, stableSince, repeatAt;
    private string lastCue, couplingFeedback;
    private float feedbackUntil;
    public void NotifyCouplingMiss(float distance)
    { couplingFeedback = $"Not connected: hook is {distance:F1} m from the lift point. Align the white ring, then lower until READY appears."; feedbackUntil = Time.unscaledTime + 3; }
    private bool wasAttached;
    private int completed;
    private void Start()
    {
        voice = Source(false, .95f); motor = Source(true, 0); feedback = Source(false, .35f); ambience = Source(true, .035f);
        motor.clip = motorClip; if (motor.clip != null) motor.Play();
        ambience.clip = ambientClip; if (ambience.clip != null) ambience.Play();
        if (portraitCamera != null)
        {
            portrait = new RenderTexture(256, 256, 16); portrait.Create(); portraitCamera.targetTexture = portrait;
            foreach (var image in portraits) image.texture = portrait;
        }
    }
    private AudioSource Source(bool loop, float volume)
    {
        var source = gameObject.AddComponent<AudioSource>(); source.playOnAwake = false; source.loop = loop; source.volume = volume; source.spatialBlend = 0; return source;
    }
    public void ToggleVoice() { VoiceMuted = !VoiceMuted; session.RecordEvent("guide_voice_muted", "Maya", VoiceMuted); if (VoiceMuted) voice.Stop(); else Repeat(); }
    public void Repeat() { if (!VoiceMuted && Cue != null) Speak(Cue); }
    private void Speak(string key)
    {
        int i = Array.IndexOf(voiceKeys, key);
        if (i < 0 || i >= voiceClips.Length || voiceClips[i] == null) return;
        voice.Stop(); voice.clip = voiceClips[i]; voice.Play();
        repeatAt = Time.unscaledTime + Mathf.Max(22, voice.clip.length + 12);
    }
    private void Update()
    {
        if (voice == null) return;
        bool typing = UnityEngine.EventSystems.EventSystem.current?.currentSelectedGameObject?.GetComponentInParent<TMP_InputField>() != null;
        if (!typing && Keyboard.current != null)
        {
            if (Keyboard.current.hKey.wasPressedThisFrame) Repeat();
            if (Keyboard.current.mKey.wasPressedThisFrame) ToggleVoice();
        }
        var run = session.Run;
        bool attached = session.hook.Attached != null;
        if ((attached && !wasAttached) || session.PlacedCount > completed)
            if (successClip != null) feedback.PlayOneShot(successClip);
        wasAttached = attached; completed = session.PlacedCount;
        Vector3 goal = session.homeMarker.position;
        string cue, instruction;
        if (run == null)
        { cue = "welcome"; instruction = session.CurrentLayoutId == "C"
            ? "STACKED PICKUP: unload C1 from the top, then C2, then C3. Click PRACTICE; I'll guide each delivery."
            : "I'm Maya, your port instructor. Click PRACTICE to begin. I'll guide every step."; }
        else if (run.State == ResearchRunState.Paused)
        { cue = "paused"; instruction = "PAUSED — click Resume or press Esc. Your task time is stopped."; }
        else if (run.State == ResearchRunState.Completed)
        { cue = "complete"; instruction = "All three deliveries complete! Save your comfort ratings, then compare the other layout."; }
        else if (!session.IsRunning)
        { cue = "stopped"; instruction = "Run stopped and saved. Press P for a fresh practice run."; }
        else
        {
            var load = session.ActiveLoad;
            if (run.Phase == ResearchPhase.Return)
            {
                goal = session.homeMarker.position; cue = "return";
                instruction = session.HomePositionReady
                    ? $"HOME reached! Release the controls and let the hook settle. Next job starts automatically ({session.HomeStableSeconds:F1}/{session.homeDwellSeconds:F1}s)."
                    : "DELIVERY ACCEPTED. Move the EMPTY hook to turquoise HOME and match its height. The next job starts there; no Space press needed.";
            }
            else if (!attached)
            {
                if (run.Phase != ResearchPhase.Pickup && session.ActiveTarget.HorizontalFootprintFits(load))
                { goal = session.ActiveTarget.transform.position + Vector3.up * .95f; cue = "settle"; instruction = session.ActiveTarget.ValidPlacement(load)
                    ? $"Verifying delivery… {session.ActiveTarget.StableSeconds:F1} / {session.ActiveTarget.countdownTime:F1}s. Then return the empty hook HOME."
                    : session.ActiveTarget.PlacementIssue(load) + "."; }
                else
                {
                    goal = load.LiftPoint - session.hook.couplingOffset;
                    float gap = Vector3.Distance(session.hook.CouplingPoint, load.LiftPoint);
                    float horizontal = FlatDistance(session.hook.transform.position, goal);
                    cue = gap <= session.hook.captureRadius ? "couple" : horizontal > .8f ? "align" : "lower";
                    instruction = cue == "couple" ? $"READY: press SPACE / right A to lock onto {load.loadId}." : cue == "align" ?
                        $"Pick up {load.loadId}: A / D rotate, W / S move trolley. Put the hook's white ring over the glowing container." :
                        $"Aligned with {load.loadId}. Hold DOWN to lower the hook. Stop when the instruction turns green.";
                    if (run.Phase != ResearchPhase.Pickup) instruction = cue == "couple" ? "Delivery not accepted. Press SPACE / right A to reattach, then center the container in its bay." : "Delivery not accepted. Align the white ring with the container and lower to reattach.";
                }
            }
            else
            {
                goal = session.ActiveTarget.transform.position + Vector3.up * .95f;
                float horizontal = FlatDistance(session.hook.transform.position, goal);
                bool overBay = session.ActiveTarget.HorizontalFootprintFits(load);
                if (load.transform.position.y < session.TransferClearanceHeight - .3f && horizontal > 3)
                { goal = load.LiftPoint - session.hook.couplingOffset + Vector3.up * Mathf.Max(0, session.TransferClearanceHeight - load.transform.position.y); cue = "lift"; instruction = "Locked! Hold UP to lift clear of the deck and other containers before rotating."; }
                else if (!overBay)
                { cue = "transfer"; instruction = horizontal < 3 ? "Center the WHOLE container inside the bay. Hold SHIFT for fine control; use TAB for overhead alignment." : $"Carry {load.loadId} to {session.ActiveTarget.targetId}. Follow the ground route. TAB: overhead alignment."; }
                else if (load.transform.position.y > session.ActiveTarget.transform.position.y + .20f)
                { cue = "land"; instruction = "Over your bay. Hold DOWN to lower gently; hold SHIFT for fine control."; }
                else
                { cue = "release"; instruction = "READY TO DELIVER: press SPACE / right A to release. Wait for DELIVERY ACCEPTED, then return the empty hook HOME."; }
            }
        }
        if (view.HeadsetRunning)
            instruction = instruction.Replace("A / D rotate, W / S move trolley", "left stick left/right to rotate, up/down for trolley")
                .Replace("Hold DOWN", "Push right stick down").Replace("Hold UP", "Push right stick up")
                .Replace("hold DOWN", "push right stick down").Replace("hold SHIFT", "hold right grip")
                .Replace("TAB: overhead alignment.", "Use the ground ring to align.").Replace("press Esc", "use Resume")
                .Replace("Press P", "Select Practice");
        if (session.IsRunning && run.Phase != ResearchPhase.Return && Time.unscaledTime < feedbackUntil && !attached) instruction = couplingFeedback;
        Cue = cue; Instruction = instruction;
        if (cue != lastCue) { lastCue = cue; stableSince = Time.unscaledTime; nextVoice = Time.unscaledTime + .65f; voice.Stop(); }
        if (!VoiceMuted && Time.unscaledTime >= nextVoice && Time.unscaledTime - stableSince >= .65f)
        {
            // Debounce rapidly changing proximity cues; never queue stale spoken instructions.
            if (!voice.isPlaying) { Speak(cue); nextVoice = float.PositiveInfinity; }
        }
        if (!VoiceMuted && session.IsRunning && !voice.isPlaying && Time.unscaledTime > repeatAt && Time.unscaledTime - stableSince > 22) Speak(cue);
        foreach (var text in captions) { text.text = instruction; text.color = cue == "couple" || cue == "release" ? new Color(.4f, 1, .76f) : Color.white; }
        Vector3 hookPosition = session.hook.transform.position;
        string heightHint = cue == "transfer" ? "KEEP LOAD RAISED" : VerticalHint(hookPosition.y, goal.y);
        string metrics = session.IsRunning ? $"{SteeringHint(goal)}\n{heightHint} | GAP {FlatDistance(hookPosition, goal):F1} m | {session.PlacedCount}/3 DONE" : "A/D ROTATE   W/S TROLLEY   UP/DOWN HOIST\nSPACE LOCK / RELEASE    SHIFT FINE CONTROL";
        if (view.HeadsetRunning && !session.IsRunning) metrics = "LEFT STICK rotate / trolley  |  RIGHT STICK hoist\nRIGHT A lock / release  |  RIGHT GRIP precision";
        if (session.IsRunning && run.Phase == ResearchPhase.Return)
            metrics = $"HOME: {session.HomeHorizontalDistance:F1} m away | HEIGHT +/-{session.HomeHeightDifference:F1} m\n{(session.HomePositionReady ? "POSITION OK — RELEASE CONTROLS" : SteeringHint(goal) + " | " + VerticalHint(hookPosition.y, goal.y))}";
        foreach (var text in readouts) text.text = (view.HeadsetRunning ? "VR / SEATED OPERATOR" : view.ViewName + "  [TAB]") + "\n" + metrics;
        bool show = session.IsRunning;
        objectiveRing.enabled = route.enabled = show;
        objectiveLabel.gameObject.SetActive(show);
        Ring(hookRing, new Vector3(hookPosition.x, 31.28f, hookPosition.z), .65f);
        if (show)
        {
            Ring(objectiveRing, new Vector3(goal.x, 31.30f, goal.z), 2.7f + .1f * Mathf.Sin(Time.unscaledTime * 3));
            objectiveLabel.transform.position = new Vector3(goal.x, 34.5f, goal.z + (!view.HeadsetRunning && view.ViewName == "OVERHEAD ALIGNMENT" ? 3.4f : 0));
            objectiveLabel.text = cue == "lift" ? "LIFT CLEAR" : cue == "settle" ? "VERIFYING DELIVERY" : run.Phase == ResearchPhase.Return ? "PARK EMPTY HOOK" : attached ? session.ActiveTarget.targetId + " / DROP HERE" : session.ActiveLoad.loadId + " / PICK UP";
            var camera = view.HeadsetRunning ? view.origin.Camera : view.desktopCamera;
            objectiveLabel.transform.rotation = camera.transform.rotation;
            route.SetPosition(0, new Vector3(hookPosition.x, 31.26f, hookPosition.z)); route.SetPosition(1, new Vector3(goal.x, 31.26f, goal.z));
        }
        float movement = session.IsRunning ? Mathf.Clamp01(session.crane.Command.magnitude) : 0;
        motor.volume = Mathf.MoveTowards(motor.volume, movement * (voice.isPlaying ? .025f : .10f), Time.unscaledDeltaTime * .15f);
        motor.pitch = Mathf.Lerp(.7f, 1.2f, movement);

    }
    private string SteeringHint(Vector3 goal)
    {
        Vector3 from = session.crane.car.position - session.crane.craneRotationPivot.position;
        Vector3 to = goal - session.crane.craneRotationPivot.position; from.y = to.y = 0;
        float angle = Vector3.SignedAngle(from, to, Vector3.up);
        float radius = to.magnitude - from.magnitude;
        string slew = Mathf.Abs(angle) < 1.5f ? "ROTATION OK" : angle > 0 ? "D / ROTATE RIGHT" : "A / ROTATE LEFT";
        string travel = Mathf.Abs(radius) < .4f ? "REACH OK" : radius > 0 ? "W / OUTWARD" : "S / INWARD";
        if (view.HeadsetRunning) { slew = slew.Replace("D / ", "LEFT STICK: ").Replace("A / ", "LEFT STICK: "); travel = travel.Replace("W / ", "UP: ").Replace("S / ", "DOWN: "); }
        return slew + "  |  " + travel;
    }
    private static string VerticalHint(float current, float desired) => Mathf.Abs(current - desired) < .3f ? "HEIGHT ALIGNED" : current > desired ? $"LOWER {current - desired:F1} m" : $"RAISE {desired - current:F1} m";
    private static float FlatDistance(Vector3 a, Vector3 b) { a.y = b.y = 0; return Vector3.Distance(a,b); }
    private static void Ring(LineRenderer ring, Vector3 center, float radius)
    {
        for (int i = 0; i < ring.positionCount; i++) { float a = i * Mathf.PI * 2 / (ring.positionCount - 1); ring.SetPosition(i, center + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * radius); }
    }
    private void OnDestroy() { if (portrait != null) { portrait.Release(); Destroy(portrait); } }
}
