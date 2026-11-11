# Run the work-study demo

Open this project in **Unity 6000.0.26f1** and open `Assets/Scenes/SeaportResearch.unity`. Press Play. This is also the first enabled build scene. The existing Menu/Level scenes remain in the build list.

The research scene contains a dockside rotating-boom crane, a three-container job manifest, three layouts, desktop and VR controls, an experiment panel, and CSV recording. A and B compare receiving-bay arrangements; C adds a top-down stacked pickup condition. It is a simplified cargo-handling prototype, not a validated model of an industrial crane.

## Without a headset

On this Linux computer, this v6 research baseline can be launched with `tools/run_desktop_demo.sh --research`. The general launcher prefers the new operational scene when its player exists; see `OPERATIONAL_ITERATION_1.md` for that version's different workflow/gantry controls. Both use Vulkan because the Intel/OpenGL path produced render-surface errors here. The Unity editor remains available for editing and Play-mode testing.

The desktop camera starts close to the hook, looking outward from the cabin side. The external views use the boom direction: W extends away from the cabin/up the overhead view, S retracts toward it/down the view, and A/D turn left/right. The views follow the crane heading; F resets a manually orbited camera. The follow view frames the hook and current goal together, including the first pickup. Maya, the instructor NPC in the cabin, speaks each next step and shows captions and alignment guidance. Use the dashboard on the right with the mouse. Select a layout, enter an anonymous participant ID, select the assigned condition order, and set the three pre-run discomfort sliders. Click **Practice** first. Practice exports are explicitly tagged and should be excluded from the study.

| Control | Action |
|---|---|
| A / D | Slew the boom |
| W / S | Move trolley out / in |
| Up / Down arrows | Raise / lower spreader |
| Space | Couple to the active container or release it |
| Tab | Cycle cabin-side follow / overhead / overview / seated cabin |
| C / Cabin button | Enter the seated cabin directly |
| Scroll wheel | Zoom the follow view |
| Shift | Hold for 30% precision speed |
| H / M | Repeat instruction / toggle voice |
| Hold right mouse and drag | Orbit the follow view; look around inside the cabin |
| F | Reset desktop camera; recenter VR before a run |
| 1 / 2 / 3 | Select layout A / B / stacked C, between runs |
| P | Start practice |
| Enter | Start measurement |
| Escape | Pause / resume |
| R | Reset station; saves an active run as interrupted |

You can also hold the on-screen motion buttons and click Lock / Release. Text entry suppresses crane keyboard shortcuts. A practice or measured run must be started before the crane will move. Camera movement and keyboard controls are desktop fallbacks, rather than simulated tracked hands.

## One complete job

1. Identify the current job, **C1**, **C2**, or **C3**. Number and color both identify the container and its bay.
2. Move the spreader above the assigned load. Lower it until the coupling point is near the container top, then press Space/right A. Coupling is allowed only for the current load within the capture radius.
3. Raise the load clear of the ground. Slew and move the trolley to its receiving bay, then lower it carefully.
4. Release the container inside the bay. It must be wholly inside the footprint, upright/aligned, landed (within 0.20 m of the deck reference), released, and nearly stationary for 1.5 seconds. The instructor reports whether it is outside, too high, tilted, rotated or still moving. An attached load cannot count.
5. Wait for **DELIVERY ACCEPTED**, then move the empty spreader to the turquoise **HOME** pad. Match the floating ring's height and release the controls. The HOME distance/height readout shows when you are in range; after 0.5 s settled, **job 2 starts automatically**. Space is only for coupling/release, not advancing jobs. HOME is the common endpoint for the timed return element.
6. After all three jobs, set the same discomfort sliders to the post-run values and click **Save post-run ratings**. Select the next layout explicitly. There is no forced scene transition.

Impacts above the configured 2 m/s relative-speed threshold and entries into a wrong bay are separate metrics. High command input is recorded as an input event; it is not called a safety violation. Collision thresholds and target tolerances are prototype measurement definitions.

## In the lab with Quest Link

The supplied photos appear to show Quest 3. Use the lab's compatible Windows PC and wired Quest Link first.

Take `Builds/PortLab_Unity_Project.zip` and extract the whole project on the lab PC. It contains the scene, controller rig, input actions, crane-control scripts, XR settings, assets, and pinned package manifest/lockfile. Open the extracted `PortLab_Unity_Project` folder in Unity Hub; the lab PC needs internet access on first import to download the declared Unity packages. The Linux player is for desktop use on this machine; use the Unity project for the Windows headset demo.

**XR Hands is included** (`com.unity.xr.hands` 1.5.1), with hand samples and hand-tracking features. **Crane operation uses physical controllers**: the research scene reads both controllers' sticks and the right primary/grip buttons, while the XR Interaction Toolkit rig supplies tracked controller visuals and trigger-driven UI rays. Bare-hand pinch/grab gestures do not operate the crane, and the cabin joystick handles are input indicators. No additional hand-tracking code is needed to use the lab's physical controllers.

1. Open the exact Unity version and this scene on the lab PC. Make sure the PC's Meta Link software and active OpenXR runtime work with the headset.
2. Check **Project Settings > XR Plug-in Management > Standalone**: OpenXR must be enabled. Oculus Touch, Meta Touch Plus and Meta Touch Pro interaction profiles are enabled for PC playback; Oculus Touch and Meta Touch Plus are also enabled for Android. The [Unity Touch Plus profile documentation](https://docs.unity.cn/Packages/com.unity.xr.openxr%401.14/api/UnityEngine.XR.OpenXR.Features.Interactions.MetaQuestTouchPlusControllerProfile.html) describes the included OpenXR feature.
3. Enter Link on the headset, then run the scene. The view changes from the desktop camera to the tracked headset when an XR display starts.
4. Sit in the agreed physical operator position, then use **Calibrate** before beginning a run. Keep this seating/control setup consistent between conditions.
5. Left thumbstick controls slew/trolley. Right thumbstick raises/lowers the spreader. **Right A** couples/releases; hold **right grip** for precision. Point a controller ray at the console and use the trigger for UI buttons.
6. Verify the actual controllers, recentering, console readability, mirrored display, and a complete job before the professor's demo. These require a real headset and are not established by desktop testing.

The automatic XRI keyboard hand simulator is off to avoid conflicts with the dedicated desktop controls. Its prefab reference is wired; it can be enabled explicitly in XRI settings for debugging the original scenes.

This installed OpenXR version does not support Linux VR. Local Linux tests use the desktop path. A Windows player requires the matching Windows Build Support module; this machine currently has Linux/Mac support rather than Windows/Android. A Quest APK requires Android Build Support and a device development setup. Neither is required for editor playback over Link on the lab Windows PC.

## Data and interpretation

Each start creates a unique folder under `Application.persistentDataPath/ResearchSessions`. Click **CSV folder** after a run to open its files on the PC. Unity also logs the exact path in the Console. On Windows the Unity persistent-data root is normally inside the user's AppData/LocalLow company/product folder.

- `runs.csv`: anonymous participant, layout, assigned order, practice flag, input mode, version, active time, and outcome.
- `tasks.csv`: one row per started job, four exclusive phase times, placement/cycle time, handling events, movement totals, and completion/interruption status.
- `events.csv`: timestamped cue, phase, coupling, release, impact, wrong-bay, pause, and completion events.
- `ratings.csv`: custom before/after neck, shoulder, and hand discomfort. These are not a validated fatigue index.
- `setup.json`: source/target geometry, crane speeds, target thresholds, station description, device/runtime, and experiment version.

Snapshots are checkpointed during a run and at important events. Reset, abort, timeout, and application shutdown preserve a partial task. Paused time is excluded. Focus loss pauses an active interactive run. Export errors appear on the dashboard and block starting a measurement if the initial save fails.

The four elements are **pickup** (cue to acquisition), **transfer** (acquisition to destination approach), **placement** (approach through release/stable dwell), and **return** (accepted placement to HOME). Axes can move concurrently within an element. Real-time operator clocks and fixed-timestep simulated motion are different quantities; automated accelerated tests must never be used as participant research data.

For the final study, balance A→B and B→A across participants; practice separately; compare each participant's own results. Report completion and errors alongside timing. The order selector records the assigned order; the experimenter chooses the appropriate first/second layout. It does not automatically randomize participants.

For all three conditions, **Swap order** cycles the six ABC permutations as well as the original AB/BA options. **Next layout** follows the selected sequence. Layout C places C1 on top of C2 on top of C3; unload in that order into separate ground-level bays, whose positions match B. The stack uses real cargo rigidbodies and the original collision boxes. Maya's lift guidance accounts for the taller pickup. C changes source arrangement and height, so interpret it as a separate handling-method condition; its difference from B is not solely a receiving-bay-spacing effect. Keep controls, guidance and view policy consistent.

Use the detailed `WSD_ERGONOMICS_PLAN.md` for the protocol, work-sampling constraints, normal/standard-time formulas, and PMTS/Hick's-law extensions. Ratings, allowances, and simulator cycle times do not establish an industrial production standard.

Summarize collected data with the standard-library Python tool:

```bash
python3 tools/analyze_sessions.py /path/to/ResearchSessions --output /path/to/analysis
```

It writes a run inventory, participant/layout summaries (including C), paired A/B comparisons, and a short report. Practice runs are excluded by default; aborted/incomplete runs remain in the inventory. Desktop and XR data, and different build versions, stay separate. Do not pool earlier pilots with `wsd-detailed-port-v6`. The normal/standard-time helper rates only the supplied manual portion and requires an explicit allowance; it does not infer a rating or allowance from game scores.

## Rebuild and verify

The saved scene and configs are ready to open. To regenerate the authored station, use **VR Research Demo > Build Research Station**. This intentionally replaces the generated research scene from the original Level1 baseline. Keep manual scene edits separately if you want to preserve them. After rebuilding the baseline, run **VR Research Demo > Upgrade Guided Experience** to apply Maya, the guided UI, updated controls, cameras and layouts. Use that upgrade command directly to refresh the existing research scene; it replaces its generated guided elements.

If the checkout contains Git LFS pointer files rather than FBX/WAV binaries, restore the upstream assets using Git LFS or `python3 tools/restore_lfs_assets.py`. The helper verifies each download against the original pointer's SHA-256. Blender is needed only to regenerate/import the original `.blend` source; the research builder can bake its mesh references into portable Unity assets.

Automated Unity checks are available through `ResearchValidation.Run` in batch mode. They test the run clock, phase transitions, CSV quoting/locale/failure handling, rotation integration, physical handling in all three layouts, placement rules, UI start/layout selection, reset, and abort. Test exports go into `/tmp/vr-research-*` and are tagged as automated practice.

Maya is a stylized full-body instructor NPC standing in the cabin, with a greeting wave, head tracking, task-pointing gestures, bundled female speech, contextual captions, repeat and mute. The separate Hick/RT exercise remains a later extension. Keep the guidance policy consistent between layouts. Voice provenance is in `VOICE_ASSETS.md`.

A rendered automated-practice preview is in `docs/images/research-station-play.png`. See `VERIFICATION.md` for checks completed locally and the remaining lab tests.

## Cabin/NPC v4 measurement settings

Slew 19 degrees/s; trolley local speed .105; hoist 4 m/s; desktop Shift or VR right grip provides 30% precision. Pickup radius 1.35 m; target footprint 5.6 × 3.8 m; released stable dwell 1.5 s; HOME accepts up to 2 m horizontal distance and +/-2 m height error, then requires 0.5 s below 1 m/s. Hook/load linear damping is 1.2 / 0.5. Both layouts use the same settings. A spreads receiving bays around a 22 m radius, while B groups them at an 18 m radius: this is a complete receiving-layout comparison, not a pure distance-only manipulation. `setup.json` records actual positions/settings, starting voice state and precision multiplier; events record voice/precision changes.


## If a job does not advance

- **PLACEMENT**: the delivery has not yet been accepted. Read Maya's correction, reattach if necessary, and center the entire container in its matching bay. A partly overlapping container is not a completed delivery. Wait for the 1.5 s verification countdown.
- **RETURN / DELIVERY ACCEPTED**: the container is done. Move the empty hook to HOME; match the beacon height and let it settle. No further Space press is needed. HOME distance and height error appear on the console.
- **PAUSED**: returning to chat pauses the timer. Resume with Esc or the console button.

Overhead alignment hides the crane structure to avoid viewing from inside the boom. Follow and overview retain the crane. Desktop viewport and hold controls adapt to the monitor aspect ratio.


## Seated cabin and Maya

Press **C**, click **Cabin**, or cycle with Tab to the fourth view. The chair, armrest joysticks, window frames and open lower viewing area form a simplified operator station. Right mouse drag looks down, sideways or behind toward the seat; **F** restores the forward look. Use Tab to return to the external views. Existing movement controls remain available in the cabin.

The left monitor shows a live overhead load-camera feed with screen-up aligned outward along the boom. The windscreen beside it stays clear. Use the existing right sidebar for guidance and run controls; in VR that console appears to the operator's right. The joystick handles visibly tilt with input; they are indicators for the existing keyboard/controller axes, rather than grab-driven virtual joysticks.

Maya stands at the front-left of the station. She greets the operator, looks toward the seated viewer and points toward the pickup/bay/HOME while speaking. **H / Ask Maya** repeats her current instruction; **M** toggles voice. The old fixed portrait is replaced by the Cabin button. Captions and speech continue in the external views.

VR uses the seated cabin eye when calibrated. The XR origin is parented to the cabin so the virtual station follows crane slewing while physical head tracking remains active. Calibrate while seated before a run. Actual headset/controller comfort and tracked-ray buttons still require the lab hardware.

The cabin shows a boom with running rails, moving trolley, four steel hoist cables, sheave head and yellow twistlock spreader. Task containers have corrugated panels/roofs, door frames, locking bars, corner castings, painted IDs and weathering. Their original 3.4 × 1.3 × 1.6 m collision/validation footprint is retained. Forty decorative freight containers occupy quay edges and the area behind the crane; the active pickup/receiving area stays clear. These details add no physics colliders or new coupling requirements.

Cabin/NPC geometry and behavior are the same across layouts. The harbor has animated waves and ripple highlights, drifting clouds, a blue hazy horizon, and a slowly moving background tug. Scenery uses the same settings across layouts. View changes are logged; the v6 build ID separates these sessions from earlier pilots. The cabin and lifting assembly are course-prototype models, not dimensionally verified manufacturer's reproductions.

For the layout experiment, use the same permitted-view policy in A and B. Treat a cabin-versus-external-view comparison as a separate condition, and use the logged view changes when interpreting timings.
