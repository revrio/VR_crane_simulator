# Seaport crane simulator: work study and ergonomics project

Reviewed 2 November 2026. First lab demo: Wednesday, 4 November 2026. Final submission: the following lab; exact date not specified.


Implementation update: the research demo is now in `Assets/Scenes/SeaportResearch.unity`. Read `DEMO_GUIDE.md` and `VERIFICATION.md` for its current behavior and test results. The repository audit below records the state before implementation.

4 November v6 extension: A/B retain the receiving-layout comparison. Optional C uses one three-high pickup stack (C1, C2, C3 from top to bottom) and the B receiving bays. Treat C as a separate source-arrangement/height condition when interpreting hoist travel, pickup and cycle time. The selector supports all six ABC orders as well as AB/BA; Next layout follows the selected order. Detailed freight/hoist visuals and decorative yard stacks are shared across conditions. HOME still defines the end of the timed return element.

## Recommended project

**Title: Evaluating container-handling layouts using a VR crane simulator.**

**Purpose:** compare two container-handling layouts to identify changes in operation time, unnecessary crane movement, placement quality, and operator workload. Use a repeatable virtual task to demonstrate work measurement and method improvement.

The existing rotating-boom crane can support this purpose. Present it as a simplified dockside cargo-handling simulation. Its mechanics are not those of a full ship-to-shore gantry crane; rebuilding the crane is outside the first demo's scope.

The supplied plan has useful environment and UI ideas, but needs a stronger experiment, reliable task completion, and earlier hardware testing. Prioritize a measured working task over additional scenery, stacking, or character animation.

## What the repository actually contains

This was a static inspection of scripts, prefab/scene serialization, package settings, and existing local changes. Unity Play mode, a player build, and headset operation have **not** been tested during this review.

| Area | Evidence and assessment |
|---|---|
| Unity | Project version is `6000.0.26f1`; that editor is installed locally. Keep the project on this version for the demo. |
| XR | Manifest includes XRI 3.1.2, OpenXR 1.14.1, Oculus XR 4.3.0, and Input System 1.13.1. The serialized Android and Standalone loader lists reference OpenXR. Installed packages do not establish headset compatibility by themselves. |
| Core crane | `CraneController`, `Rope`, left/right lever scripts, and `CraneModel.prefab` implement trolley, hoist, slew, and spring-force rope behavior. Full pickup, transfer, and release reliability remains a runtime acceptance test. |
| Port assets | `industry.blend`, ship/dock scene objects, water assets, CCTV, and several existing audio clips are available. Avoid scheduling these as entirely new asset work. |
| Existing game | Menu and three level files exist. README's `Assets/Scenes/MainSimulation` path does not. |
| Research additions | Local modifications already add penalties and summary CSV to `LevelManager`. Untracked layout, HUD, NPC, and editor helper scripts already exist. Preserve this work. |
| Integration | Main Menu/Level1/Level2/level3 scene serialization contains none of the new layout/HUD/NPC components; those scripts also lack `.meta` files. No layout config `.asset` files were found. Source files alone are not an integrated feature. |
| Assistant | NPC behavior and an animator creation helper exist; no character model or finished assistant prefab was found. |
| Desktop simulator | Auto-instantiation is already enabled, with editor-only enabled, but its prefab reference is null. The installed XRI loader explicitly returns without instantiating when this reference is missing. |

### Fixes before collecting measurements

1. **Resolve the `XROrigin` references.** `NPC_ProximityTrigger.cs:57` and `SeaportSetupHelper.cs` use `XROrigin` with the XRI namespace imported. The installed package defines it in `Unity.XR.CoreUtils`. Add the appropriate import in both files. This is a source-level compilation blocker identified statically, rather than a captured Unity compiler result.
2. **Validate the build scene list.** `EditorBuildSettings.asset:18` lists `Assets/Scenes/Level3.unity`, but the actual filename is `level3.unity`. Correct through Unity and verify builds, particularly on this case-sensitive Linux filesystem. Menu/level navigation also needs a complete run-through.
3. **Fix frame-dependent slew.** `CraneController.cs:74–80` initializes smoothing state to zero on every call and multiplies by delta time again for rotation. In the usual unclamped range, rotation per call is proportional to delta time squared. Consequently different frame rates change the task. Persist smoothing state and apply a consistent simulation update; compare identical inputs over the same duration at multiple render rates. Do not claim validated crane dynamics from the spring model.
4. **Define valid placement.** `TargetArea.cs:17–50` accepts a matching-color collider remaining in a trigger for two seconds. It does not check full containment, orientation, release, or low velocity. Add explicit acceptance conditions and one completion per unique load/target. Handle multiple colliders and target exit/re-entry without duplicate countdowns. Confirm acquisition/release behavior first; no explicit custom attach/detach state machine was found in the inspected application scripts.
5. **Replace the penalty definition.** `CraneController.cs:83–95` counts input magnitude at or above 0.8. It measures high command input, not a collision, overspeed, or unsafe maneuver. The shared flag can also alternate when two control axes call the method with different magnitudes. If retained, label this metric accurately. Log real collision and wrong-target events separately, with contact-pair deduplication and clear thresholds. `Load.cs` currently only plays impact sounds.
6. **Add an explicit session lifecycle.** `LevelManager.cs:72–77` times immediately from scene update, including preparation. Start after calibration and a participant-ready action. Support practice, measured running, pause, completion, timeout, and abort. The current five-second scene transition at `LevelManager.cs:203` should become an experimenter-controlled next step.
7. **Make data export usable for an experiment.** Current CSV is a completed-level summary, lacking participant, run, element, and task IDs. It does not preserve interrupted work. Use invariant numeric formatting, CSV escaping, a schema version, and stable IDs. Save at task completion and pause/abort, not only successful level completion. Guard essential UI/audio references so a missing presentation component cannot prevent saving.
8. **Make layout initialization deterministic.** `LayoutManager.Awake` may run before `LevelManager.Awake`, losing the chosen layout label. Initialize in a defined sequence. Validate array lengths and unique load-target mappings. A repeat must reset targets, timer, crane pose, rope state, attached loads, velocities, and event state; repositioning containers alone is insufficient.
9. **Verify control lifecycle.** Lever scripts add callbacks in `Start` and again on subsequent `OnEnable`; `OnDisable` assumes initialization. Left-lever motion selection can retain its original axis until the active coroutine stops. Test re-enabling controls and switching axes. Releasing one lever must not restore locomotion while the other remains held. Remove per-frame logging from measured runs.

## First experiment: method and time study

**Question:** does consolidating receiving bays within a smaller operating sector reduce container transfer time and unnecessary crane motion, while maintaining placement quality?

Use one seaport scene with two configuration assets. The existing `LayoutConfig` approach is a useful starting point; duplicated environment scenes would increase integration work and make settings drift harder to detect.

| Property | Layout A: dispersed receiving bays | Layout B: consolidated receiving bays |
|---|---|---|
| Source loads | Same three source positions | Same three source positions |
| Receiving positions | Distributed across a wider reachable slew sector | Grouped within a smaller reachable slew sector |
| Loads and target rules | Same size, mass, IDs, tolerances, and dwell time | Same |
| Job sequence | Same ordered manifest | Same |
| Controls and crane settings | Identical | Identical |
| Intended change | Baseline handling geometry | Reduced travel/slew where geometry permits |

Choose positions after measuring the crane's actual reach and scene scale. All destinations must be reachable, visible enough to complete, non-overlapping, and supported by physical surfaces. Existing helper coordinates are examples, not a tested layout. Record source/destination coordinates and required angular/radial travel for the report.

This is a comparison of two work-system arrangements. Travel distance changes deliberately; do not attribute the whole time difference to cognitive load. Do not assume that wide spacing is easier: it can require longer travel and greater head turning. Keep target precision unchanged for this experiment. Stacking adds another factor and should wait.

### Standard task and event boundaries

Display a job manifest using both numbers and colors. Work one assigned load at a time. Use the same start crane pose and pickup/drop sequence in both conditions.

| Element | Start | End |
|---|---|---|
| Locate and approach | Job cue shown | Hook enters the pickup alignment region |
| Align and acquire | Pickup alignment region entered | Load attachment confirmed |
| Lift clear | Attachment confirmed | Load reaches the defined clearance height |
| Transfer | Clearance height reached | Load enters the destination approach region |
| Lower and position | Destination approach region entered | Release accepted within target tolerance |
| Verify placement | Accepted release | Load satisfies stability/dwell criteria |
| Return/reset | Stable placement | Hook reaches the defined home state |

For a minimum demo, group the first two elements as **pickup**, then log **transfer**, **placement**, and **return**. Keep these state durations mutually exclusive. Crane axes may operate together inside a state; separate axis-active times may overlap and must not be added as if they were exclusive elements. Record correction attempts as events inside the current element.

A proposed placement rule is correct load ID, center and footprint within tolerance, orientation within tolerance, released attachment, and low linear/angular speed continuously for a defined dwell. Tune and freeze the numerical thresholds during the pilot. These are simulator acceptance rules, not industrial safety standards.

Define cycle time as job cue through return home. Also retain cue-to-stable-placement time so handling time and return time can be reported separately.

### Practical participant protocol

For Wednesday, demonstrate one complete cycle in each layout and show the exported element times. One operator's demonstration is not evidence of a general layout advantage.

For the final submission, aim for a small exploratory pilot of approximately 6–10 volunteers if lab time allows; this is a feasibility target, not a statistical power calculation.

1. Record anonymous participant ID, prior VR/crane experience, seated/standing setup, headset, runtime, and build ID.
2. Give identical instructions, calibrate viewpoint and reachable controls, and practice on a neutral layout until the participant understands acquisition, movement, release, and reset. Exclude practice from measured results.
3. Balance order: half do A then B, half B then A. Keep order in the data. Use two three-container runs per condition if practical: six transfers per layout per participant.
4. Allow a break between conditions. Keep guidance, audio, control placement, and any assistant behavior constant. Record tracking failures, assistance, and interruptions.
5. Record errors and aborted runs as well as successful completions. Do not silently remove slow or unsuccessful runs. Agree a timeout after the pilot.
6. Collect workload/discomfort immediately after each condition, then display the summary and explicitly advance.

Report within-participant differences. Repeated transfers from one person are not independent participants. Present completion rate and errors alongside mean/median time. For successful repeated cycles, show variability and sample count. Small convenience-sample findings apply to this prototype and task.

## Mapping to your syllabus

| Course topic | What this project can show | Priority |
|---|---|---|
| Direct time study (DTS) | Timestamped work elements; observed cycle and element times; repetitions and variability | First demo |
| Method/motion study | Load path, accumulated absolute slew, trolley travel, hoist travel, corrections, and a process chart | First demo, with richer plots for final |
| Normal and standard time | A transparent worked calculation from observed elements, with declared rating/allowance assumptions | First demo/report |
| Work sampling | Random observations of defined task/control states and estimated state proportions | Final, if enough exposure is available |
| PMTS / motion-time systems | A small manual control sequence analyzed with the actual tables and conventions taught in class | Optional report extension |
| Hick's law and RT | Separate controlled choice-response trials in a dock control panel | Optional final extension |
| Index of difficulty, if this is what “ID” means in your course | Separate controller-pointing task with controlled target distance/width | Optional final extension |
| Keyboard optimization | Spatial compatibility and frequency-based arrangement of a control panel are relevant analogies; do not label joystick movement a keyboard optimization experiment | Optional discussion |

### Calculations that are defensible

**Observed time:** report the mean or median of repeated measured cycle times, plus element means and variability. Preserve abnormal-event flags and explain any exclusions. Productive throughput can be expressed as accepted containers / measured active minutes; identify whether return time and corrections are included.

**Normal/basic time:** for rateable manual elements, `NT = observed time × rating / 100`. Do not rate fixed machine travel or mandatory dwell as if it were manual effort. Separate human-controlled work from fixed-duration machine/process components; overlapping components require a timeline rather than simple addition.

**Standard time:** if allowance `A` is a fraction of total standard time, `ST = NT / (1 − A)`. If the course instead defines an allowance added as a fraction of normal time, use `ST = NT × (1 + A)`. State the denominator/convention. Do not mix them.

Illustration only, **not measured project data**: sequential manual observed time 40 s at an assumed 100% rating plus 20 s of fixed process time gives NT = 60 s. Under a deliberately simplified whole-cycle allowance of 15% of standard time, ST = 60 / 0.85 = 70.59 s. A more detailed course calculation may apply relaxation allowance only to eligible work and treat process waiting separately. State this explicitly. Novice VR performance and an assumed rating do not establish a production standard for real crane operators. A fatigue rating does not automatically determine an allowance percentage.

**Motion comparison:** use accumulated absolute slew angle (not net end-minus-start angle), absolute trolley/hoist travel, and world-space load-path length. Keep units consistent and integrate at a fixed sampling rate. Treat small physics/tracking jitter consistently; do not claim every small fluctuation is unnecessary operator motion.

**PMTS:** break down an actual human sequence such as reach to control, grasp, actuate, and release using the system taught in class. Use its tables and applicability rules. Where the chosen system uses TMU, 1 TMU = 0.036 s. Do not invent motion codes or predict the complete suspended-load transfer from hand-motion tables; machine travel, visual judgment, sway correction, and the VR interface need separate treatment.

**Work sampling:** define mutually exclusive observable categories first, for example active crane command, no command while load is settling, and no command during alignment/decision. Intent such as “searching” cannot be inferred just from zero joystick input. Use randomly timed observations rather than calling a continuous 10 Hz trace work sampling. `state proportion = observations in state / valid observations`. A rough binomial planning expression is `n ≈ z² p(1−p)/e²`; at p = 0.5, 95% confidence, and ±10 percentage points, n is about 97 independent observations. Samples close together within a short session are correlated, so a brief demo cannot promise that precision. Continuous telemetry can separately provide exact logged state-duration fractions.

### Workload, discomfort, and fatigue

NASA-TLX provides subjective workload ratings across six dimensions. If used, retain the official questions and anchors; Raw TLX is the unweighted average of the six consistently oriented ratings. Workload is not a direct fatigue measurement. The NASA manual and Hart's review describe the instrument and its raw variant. [NASA TLX resources](https://www.nasa.gov/human-systems-integration-division/nasa-task-load-index-tlx/), [Hart, 2006](https://humanfactors.arc.nasa.gov/groups/tlx/downloads/HFES_2006_Paper.pdf).

For this schedule, a simple before/after self-reported discomfort rating for neck, shoulders, and hands is also practical. Label a custom 0–10 questionnaire as custom; do not call it Borg or a validated fatigue index. Record exposure duration. Equal job counts can produce unequal exposure times between layouts. Early-versus-late performance changes are exploratory because learning, order, and task geometry also influence time.

Headset/controller tracking can describe head orientation, hand reach, and input activity. It does not directly provide eye gaze, muscle fatigue, complete body posture, or valid RULA/REBA scores. A virtual container's mass is not a physical load on the participant's arms. Stop a session if the participant reports discomfort or sickness; preserve the abort record.

### Optional final extension: decision and pointing trials

If the main simulator is reliable, add **one** small independent cabin-panel exercise.

For Hick's law, vary the number of equally likely response alternatives, for example 2, 4, and 8. Keep cue legibility, response mapping, practice, and physical response demands controlled. Record cue onset, first response, correctness, and valid response time; retain errors and timeouts. Fit the course's chosen logarithmic formulation and state it. Do not use complete crane transfer time as reaction time. VR display/input latency means these are application-level response measurements. [Hick, 1952](https://journals.sagepub.com/doi/10.1080/17470215208416600).

If ID means Fitts' index of difficulty, use a separate pointing task with measured amplitude and target width. The original formulation is `ID = log2(2A/W)`; use the formulation taught in class consistently. A crane's inertial, multiaxis suspended-load movement is not automatically a standard Fitts pointing task. [Fitts, 1954](https://doi.org/10.1037/h0055392).

## Implementation structure and data

Retain `CraneController`, `Rope`, and existing XR interactions after fixes. Reuse `LayoutConfig`/`LayoutManager`, expanding configs to include stable IDs, orientations, target tolerances, and a job manifest. Avoid a second competing game/session manager: make `LevelManager` a game-facing adapter while one `ExperimentSession` owns measured state and time.

Recommended additions:

- `ExperimentSession`: participant/run IDs, practice versus measurement, lifecycle, condition order, explicit start/end, monotonic unscaled timing, and active-time pause handling.
- `TaskRecorder`: exclusive element transitions, cycle outcomes, and stable load-target IDs.
- `PlacementValidator`: attachment/release state, tolerances, dwell, and deduplicated completion.
- `ResearchLogger`: escaped, invariant-culture CSV; checkpoints, abort records, and visible export success/failure.
- `CraneTelemetry`: optional fixed-rate samples of crane/load positions, commands, and tracking validity. Keep writes buffered; do not write the filesystem every rendered frame.

Minimum data tables:

| File | Main fields |
|---|---|
| `runs.csv` | schema_version, build_id, run_id, participant_id, layout_id, condition_order, practice, start_utc, active_seconds, status, headset, runtime |
| `tasks.csv` | run_id, task_id, load_id, target_id, each element duration, cue_to_placement_s, full_cycle_s, corrections, collision_count, wrong_target_count, outcome |
| `events.csv` | run_id, task_id, monotonic_time_s, event_type, object_id, relevant value |
| `ratings.csv` | run_id, rating_name, timing/pre_post, value, scale_definition |

Keep raw data and calculated results separate. Derive a small results table and real plots after the pilot: layout versus cycle time, element-time breakdown, and errors. Include paired participant points when available. Do not fill the presentation with invented experimental results.

## VR deployment and presentation

The photos supplied after the initial inspection appear to show a **Meta Quest 3**, based on the three vertical front sensor sections. This is a visual identification; confirm the device name in headset or lab software when available.

For the first demo, prioritize a **Windows PC build with wired Quest Link**, subject to the lab PC's GPU/runtime and Link access. Meta supports running Unity content over Link without creating an APK. Use OpenXR with the correct PC runtime and controller interaction profiles, and check **Standalone** XR settings, since these apply to editor/PC execution. Do not assume Android configuration covers PC playback. [Meta hardware setup](https://developers.meta.com/vr/documentation/unity/unity-env-device-setup/).

Test a standalone Android build later if needed and if the development setup is available. Keep desktop simulator testing as a development aid and fallback; it does not validate headset controls or ergonomics.

Use a stable operator station, preferably seated if the lab permits, with an adjustable viewpoint and comfortable controller reach. Freeze that setup across the layout experiment. Avoid making participants walk to the cabin during measurement. Show a task-focused in-world panel and mirror results on the PC monitor for the professor. A world-space HUD alone does not guarantee a spectator-readable display.

The optional female assistant can be a port supervisor/trainer: welcome the user, explain the controls and manifest, run practice, and introduce each condition. Keep instructions identical. Disable proximity greetings during measured runs so waves/voice do not distract one condition more than another. A static portrait or existing dialogue UI is sufficient for Wednesday; import a character only after the crane, recording, and headset run work.

## Two-stage delivery schedule

### Monday, 2 November: make one reliable measured task

1. Resolve compilation and scene-path issues using the exact editor version. Wire the simulator prefab for local testing.
2. Prove one pickup → transfer → valid release → stable placement → reset cycle. Repair controls/physics before scene decoration.
3. Add explicit start, four timed phases, unique IDs, outcome/error events, and checkpointed CSV.
4. Configure two reachable layouts in one scene. Keep identical target tolerances and crane settings.

### Tuesday, 3 November: prove the lab demo

1. Test on the actual Windows PC and headset early. Verify both controllers, grab/release, recentering, menu navigation, and mirror output.
2. Run both layouts, open the actual CSV outside Unity, and compare timestamps against an independent stopwatch/video segment.
3. Verify no double counting, false attached-load completion, missing aborted runs, or mixed layout IDs. Check crane response across frame rates.
4. Prepare a short slide/report page with aim, controlled variables, task elements, raw data example, and a clearly labeled standard-time calculation.
5. Record a backup video of the tested build. Freeze the demo version. Add limited port dressing/assistant only if everything above passes.

### Wednesday, 4 November: first demo

Show the port and controls, explain the layout question, perform one transfer in each condition, show element times and a real CSV export, then explain the final participant protocol. Treat this as a functional prototype demonstration.

### Following lab: final presentation

Deliver the tested build, two layout definitions, reproducible participant protocol, actual pilot data, analysis of time/motion/quality, normal/standard-time assumptions, and limitations. Add workload ratings, sufficient work-sampling observations, or one choice-response exercise as time permits. Preserve attribution for the original simulator and identify your team's new experimental contributions. README claims MIT licensing but no root LICENSE file was found in this checkout; verify source/asset provenance before packaging the submission.

The original seven-hour estimate is too optimistic for an integrated, hardware-tested research prototype. Reserve the two available days for core operation, instrumentation, and lab testing. Move stacking, extensive scenery, dynamic NPC behavior, and additional experiments after the first demo.

## Acceptance checks

- Project compiles in Unity 6000.0.26f1 and the selected build target runs on the lab headset.
- Both layouts use the same build, controls, task count, physics, and target precision.
- The timer starts on explicit readiness; practice and pauses are identifiable.
- Pickup/release/reset work repeatedly; attached, moving, wrong, or duplicate loads cannot complete a task falsely.
- Collision, wrong-target, high-input, and completion events have separate meanings.
- Logged exclusive phase durations reconcile with active task time within the declared sampling tolerance.
- Repeat/restart clears the whole task state and retains the correct condition ID.
- Partial/aborted sessions survive; export errors are visible; CSV imports with correct columns and decimal values.
- The professor can see the mirrored task and results, and a tested backup recording is available.

Work-study background reference: [ILO, Introduction to Work Study, fourth revised edition](https://www.ilo.org/publications/introduction-work-study-4th-revised-edition). Use your course's actual rating, allowance, PMTS, and experimental conventions for the submitted calculations.


### Guided v3 implementation note

The current generated station includes Maya's offline spoken/captioned instruction and new desktop cameras. Use `DEMO_GUIDE.md` for actual v3 geometry, tolerances and controls; earlier estimates in this planning document are historical. Preserve the return work element: after delivery acceptance, the empty hook parks HOME and the next task begins automatically. Do not combine v1/v2/v3 participant records as though mechanics were unchanged.
