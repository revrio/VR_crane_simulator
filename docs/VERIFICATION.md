# Local verification

## Operational iteration 3 — 5 November 2026

All tests run on this Linux desktop in Unity 6000.0.26f1, without a headset. The following fresh processes exit 0:

- `/tmp/port-iteration3-final-core.log`: all four staging/rehandle/build-stack layouts with the controlled aisle crossing; 22 physical moves, final park, conserved identities, real support/landing, held trolley boundaries, gated road traffic, actual actor/crane motion, and healthy reconciled op3 records.
- `/tmp/port-iteration3-drill.log`: five deliveries after an actual scripted restricted-zone entry; attached cargo/traffic freeze, operational hold time included, software pause excluded, early acknowledgement while paused ignored, actual worker retreat and explicit resume.
- `/tmp/port-iteration3-full-intake.log`: five persistent truck/reachstacker deliveries into the three/two-high yard stacks, stop/resume during parked-truck intake, then five player deliveries with normal crossing.
- `/tmp/port-iteration3-separated.log`: six-move priority rehandle with perimeter/road pedestrians and traffic but no scheduled aisle crossing.
- `/tmp/port-iteration3-contacts-recovery.log`: ordinary physics/collider fixtures distinguish near clearance from contact; initial scrape and later force escalation stay one compound episode; a fast barrier strike produces strong classification/effects; an actual moving capsule-worker contact causes protected hold, physical retreat, explicit resume and preserved aborted/reset records. No synthetic Collision or direct severity injection is used.
- `/tmp/port-iteration3-final-preview.log`: final scene regeneration; all five camera views, both rear trucks in overview, four workers/two trucks/two background crane sets, actual motion, cabin Maya/live camera, welcome/stop/resume speech playback, protected-stop alarm, actor freeze and actual world-space particle emission at the contact point. Also checks intake stop records have no unstarted operator move ID. Final captures are `docs/images/operational-i3-*.png` and were visually inspected.

The normal handling suite totals **38 accepted moves across seven completed runs**. Those normal runs have zero contact and near-miss episodes; the dedicated contact fixtures deliberately generate incidents. The analyzer retains seven task groups and 38 move groups with zero phase/move/delay reconciliation warnings. Seven Python tests cover old/new schema grouping, aborted exclusions, traffic separation, incident types and the rule that delay subsets do not add extra elapsed time.

The first attempts exposed a Unity native-material initialization requirement, overly broad near-miss labels around normal seating/departure, and cleanup calls on destroyed audio/hook objects. These are corrected in the final source. The driver now uses a physically gentle final hoist approach; full-speed landing can correctly trigger the configured impact stop. The crossing starts on the first ship-bound acquisition, including after a yard-only rehandle. Prototype worker/truck routes are deterministic; meaningful dynamic actors and foreground barriers have colliders. Hinterland buildings are distant visual scenery.

Final Linux build succeeds cleanly in `/tmp/port-iteration3-final-build.log` (exit 0; `Build Finished, Result: Success`, `RESEARCH_PLAYER_BUILT`). The `PortLab Operations` player opens at native 3456 × 2160 fullscreen. A real packaged-keyboard check selected intrusion traffic, started staged practice, opened V overview, latched X stop, acknowledged/resumed with E and entered the cabin; saved run/view/stop events confirm each action (`/tmp/port-iteration3-player.log`). Both externally driven sessions are explicitly flagged automated practice. Normal window close exits 0, preserves aborted records and produces no managed runtime exceptions. The actual client window was inspected (`/tmp/port-iteration3-player.png`). Both Unity and the player are closed. The portable source ZIP is refreshed and CRC checked.

Prior operational scenes and the v6 research scene were compared byte-for-byte to the preceding source ZIP and are unchanged. Their packaged players are retained separately. All new verification runs are accelerated automated **practice data**, not human time-study/fatigue findings. Contact speed/impulse, bounds-based clearance, scripted routes, protected freezes and anchored accepted supports are simulator conventions. Actual headset performance/controller checks and the final participant protocol remain outstanding.


## Operational iteration 2 — 4 November 2026

`OperationalPortIterationTwoValidation.Run` passes in `/tmp/port-iteration2-physical.log` (exit 0): four staged layouts plus five-truck stacked intake, **27 accepted physical moves** using ordinary crane input commands and coupling/release actions. It checks five persistent cargo identities, top-down access rules, guided manual yard stacking/unstacking, temporary blocker rehandle, two-tier ship loading, final parking, attached-load pause, reset/partial exports, premature drop and physical recovery, unique move IDs and reconciled phase/move clocks. Scenario preflight rejects a blocked priority pickup and an upper-tier stow ordered before its supporting base.

All remaining full-intake modes have also completed: A in `/tmp/port-iteration2-additional-intakes.log`, C in `/tmp/port-iteration2-priority-intake.log` and D in `/tmp/port-iteration2-stack-intake.log`. This brings the coverage to **44 completed moves across eight complete runs**, with all four layouts checked in both staged and five-truck intake modes. C/D isolated processes exit 0. The earlier long additional-intake batch ended in a native Mono SIGSEGV during C's last transfer; no managed exception or failed task assertion preceded it. That unfinished run is retained and excluded from completed observations. The isolated reruns pass; the native failure's cause is not established.

`/tmp/port-iteration2-legacy-regression.log` exits 0 after the original one-container chain and its support, footprint, recovery, pause, reset and clock guards. The copied first scene (`OperationalPortIteration1.unity`), original `IterationOne.asset` and `SeaportResearch.unity` match their preceding source ZIP byte-for-byte. The prior operational Linux player is preserved separately.

`/tmp/port-iteration2-release-preview.log` passes the regenerated final scene's four layout previews, cabin/Maya visibility, live camera rendering and clean shutdown. Final layout/cabin screenshots were inspected. A camera framing adjustment clears the yard from the foreground gantry leg, and hold-control captions fit on one line. Labels face the viewing camera.

`python3 -m unittest discover -s tools -p 'test*.py'` passes five tests. The op2 analyzer keeps the eight task groups separate, distinguishes aborted/unfinished runs and reports 44 completed move groups with zero clock-reconciliation warnings. JSON snapshots parse and retain the actual slot plan, five cargo IDs and anchoring assistance policy. All these sessions are accelerated automated **practice checks**, not participant findings.

Final Linux build succeeds in `/tmp/port-iteration2-final-build.log` (exit 0; `Build Finished, Result: Success`, `RESEARCH_PLAYER_BUILT`). The standalone `PortLab Operations` player opens at native 3456 × 2160 fullscreen. An actual desktop keyboard smoke check selects layout C, starts its five-container staged task and enters the cabin, confirmed by saved run/view events and inspection of the captured client window (`/tmp/port-iteration2-player.png`). Normal window close exits 0, preserves interrupted records and produces no managed runtime exceptions (`/tmp/port-iteration2-player.log`). Those externally driven practice checks were moved intact into `/tmp/port-iteration2-native-input` so they are not mixed with user sessions. Both Unity and the player are closed. The portable source ZIP is regenerated and CRC checked.

Accepted, settled supports are anchored until their permitted pickup. No free stack-collapse dynamics, moving worker crossings, active traffic conflicts, advanced impact effects or hardware headset verification is claimed in this part.

## Operational iteration 1 — 4 November 2026

The new `OperationalPort` scene has a single-container truck → reachstacker → yard → player crane → ship workflow. `OperationalPortValidation.Run` passed in `/tmp/port-iteration1-final-validation.log` (exit 0; `OPERATIONAL_ITERATION1_PHYSICAL_PASSED`). It checked:

- Healthy export and Practice-button start; the same cargo instance and continuous pose at each custody handoff.
- Pausing freezes the truck and excludes the pause from the monotonic clock.
- A reset after NPC unloading saves an interrupted record and restores the loaded truck, handler and ownership.
- Real rigidbody/joint pickup, lift and travel; deliberate early release outside the ship; physical reattachment/recovery.
- Rejection of suspended cargo and a rotated footprint outside the outlined slot.
- Supported, released, stable landing on the actual ship-deck collider; final empty-spreader parking; preserved completed export and reconciled exclusive phase totals.

The test exposed a camera cleanup-order exception on editor shutdown after the assertions passed. The null guard is fixed; `/tmp/port-iteration1-final-preview.log` then passed the cabin/Maya/live-load-camera render and clean exit (`OPERATIONAL_ITERATION1_CABIN_PREVIEW_PASSED`, exit 0). Captures `operational-intake.png`, `operational-ship.png`, `operational-complete.png`, `operational-cabin.png` and `operational-ready.png` were inspected. All test runs are accelerated, labelled automated practice, and are not participant findings.

`SeaportResearch.unity` was compared with the previous v6 source archive and remains byte-identical. The operational scene has no legacy `ExperimentSession` singleton. Baseline Python analysis tests and shell syntax checks pass. The editor enabled the existing Meta Touch Plus interaction profiles while importing; actual HMD tracking, controller mappings, world-space UI and frame-time/comfort tests remain for the lab. The operational Linux build temporarily removes unsupported Linux XR loaders and restores the source configuration, as the v6 build helper does.

This first part does not include five-cargo missions, walking workers, crossings, rehandling or rich contact particles. Its figures and machinery are procedural models; NPC intake is controlled kinematic motion, while student handling uses the spring-rope rigidbody simulation. New records use a separate `op1` practice schema/root.

The final scene moves stored decorative freight outside the truck/handler routes. Its release preview passed (`/tmp/port-iteration1-release-preview.log`). The separate Linux release build succeeded (`/tmp/port-iteration1-release-build.log`, exit 0, `RESEARCH_PLAYER_BUILT`). Native Vulkan startup is fullscreen at 3456 × 2160 without managed startup exceptions (`/tmp/port-iteration1-player.log`). The operational player uses the separate product title/data root `PortLab Operations`. Original player settings and fixed timestep are restored after authoring/building. The portable source archive is regenerated and CRC checked.

## Detailed port / stacked C v6 — 4 November 2026

- Graphics-enabled Play checks passed in `/tmp/vr-details-v6-final-preview.log` (exit 0). The cabin camera sees the supporting boom, trolley and four cables. Maya, the single load monitor and sidebar remain visible. Camera direction checks pass for four views and four headings. Cargo detail mesh fits the original validated 3.4 × 1.3 × 1.6 m bounds; each task load retains its one original collider, while the new rig and decorative freight have none.
- Nine actual physics-driven acquisitions, transfers, releases/stable placements and HOME returns passed across A/B/C in `/tmp/vr-details-v6-physical.log` (exit 0). C's three rigidbodies remain stacked until unloaded top-down. The C UI button, active-run layout lockout, three-condition order/Next layout behavior, setup export, pause/reset and preserved abort are checked.
- Automated-practice export analysis produced three condition groups, each with three completed cycles, and zero duration-reconciliation warnings. C is retained as a separate summary; paired output remains specifically A/B. Python analysis unit tests pass. Automated times are technical checks, not research findings.
- Visually inspected final captures: `docs/images/operator-cabin.png`, `stacked-layout-c.png`, `spreader-detail.png`, `detailed-port.png`. Existing animated-water frame comparison also passes.

The rig is a simplified visual lifting assembly driven by the existing single-rope physics and coupling rules. Four independently simulated hoist ropes or industrial-scale crane certification are not implemented. HMD rendering, tracked input and actual lab performance/comfort remain hardware checks.

V6 release build succeeded in a fresh editor process (`/tmp/vr-details-v6-build.log`, exit 0, `RESEARCH_PLAYER_BUILT`). The independent Vulkan player starts at native 3456 × 2160 fullscreen without managed startup exceptions (`/tmp/vr-details-v6-player.log`). The source archive is refreshed and CRC checked after documentation updates.

## Harbor / clear cabin v5 — 4 November 2026

`ResearchCabinPreview.Run` passed in a graphics-enabled Unity process (exit 0; `/tmp/vr-harbor-v5-final-preview.log`). The duplicate cabin menu is absent; the sidebar, live load camera and Maya remain visible. All four camera directions passed at four crane headings. Both new URP shaders are supported, the harbor scenery has no colliders, and a comparison of two locked-camera frames confirms visible animated water. The saved cabin and harbor screenshots were visually inspected. No crane input, task acceptance or timing rules changed in this update; the earlier six-delivery checks remain the handling validation.

The original native port meshes remain. Water, clouds and the background tug are procedural visuals with no extra downloaded assets. Actual headset rendering and performance still require the lab equipment.

The separate Linux v5 release build succeeded (`/tmp/vr-harbor-v5-build.log`, exit 0, `RESEARCH_PLAYER_BUILT`). Unity reported an unsupported single-pass-instancing variant of the package's `Hidden/CoreSRP/CoreCopy` shader during the Linux build; this did not fail the build. The new harbor shaders passed graphics-enabled rendering separately.

Environment: Linux desktop, Unity **6000.0.26f1**, project OpenXR/XRI package versions preserved. No headset was connected.

## Completed checks

- Compilation of the integrated research scripts in the installed Unity editor.
- Deterministic run-clock tests: pauses excluded, four exclusive phases, invalid transitions rejected, duplicate completion rejected, between-job checkpoint time excluded, and partial timeout retained.
- CSV tests: invariant decimal formatting, quote escaping, safe participant-ID cells, repeated checkpoints without duplicate task rows, partial records, and visible export failure.
- Rotation regression across 30/90 Hz integration steps.
- Unity Play-mode handling in both layouts: **six physical acquisitions, lifts, transfers, releases, stable placements, and returns to HOME** per full smoke-test pass.
- UI practice start; layout changes blocked during a run; reset clears completed targets; an interrupted run remains in its export.
- Placement rejects attached loads, excessive tilt, and load footprints outside the bay. Actual settled/released placements complete after the configured dwell.
- Analysis tests preserve practice/aborted-run distinctions and separate desktop from XR participant comparisons. A sample analysis of automated practice exports produced no duration-reconciliation warnings.
- Rendered Play-mode preview inspected: crane and port meshes, colored containers, numbered/color-coded receiving bays, timer, phase breakdown, handling errors, ratings panel, and export status are visible.
- Original missing Git LFS models/audio restored: **34 files**, each verified against its original SHA-256 and byte size.
- Port source meshes/materials converted into Unity assets for the research scene. The original Blender source remains for editing the original levels.

The automated runs are accelerated **practice tests**, labeled `AUTOMATED_SMOKE`. Their timings are technical test data and must not be presented as participant results.

## Build and editor behavior

The final Linux Mono player was built successfully and started independently with graphics. On this Intel/Mesa laptop, the default OpenGL backend emitted render-surface resolve errors; launching with `-force-vulkan` resolved them. `tools/run_desktop_demo.sh` applies that setting. The Vulkan startup log contains no managed exceptions or render-surface errors. The first attempt using the project's IL2CPP setting was unavailable because that backend is not installed here; the build helper now selects Mono for the demo build and restores the project setting afterward.

One combined test/build attempt triggered a **native Unity crash on leaving Play mode after all physical assertions had passed**. Separate Play-mode checks that exited the editor directly completed successfully. The final build is performed in a fresh editor process. This observation is recorded separately from the simulator's managed-code checks; it does not establish that stopping Play mode is reliable on this editor/OS combination.

Headless player startup can emit unsupported-shader messages when `-nographics` is used. Graphics-enabled editor rendering was checked separately.

## Requires the lab

- Quest Link/OpenXR initialization on the actual Windows PC.
- Both tracked controllers and their stick/button mappings.
- Seated calibration, world-space UI ray interaction, console readability, tracking interruptions, and VR comfort.
- A complete human-operated cycle in both layouts and opening the resulting CSV on the lab PC.

Windows and Android player build modules are not installed on this machine. Use the lab's Windows Unity editor for Link playback or the provided Windows-build menu after confirming build support. This is a tested desktop prototype with a VR input/view path, not a completed headset validation or industrial crane certification.

Final portable-scene Play-mode run: deterministic checks and all six physical tasks passed, and the editor exited successfully when the test process quit directly. The separate final release build succeeded.


## Guided v3 verification (3 November 2026)

`/tmp/vr-guided-v3-validation.log` completed with `RESEARCH_PURE_CHECKS_PASSED` and `RESEARCH_PHYSICAL_SMOKE_PASSED` (exit 0). Both layouts completed all three physical deliveries and returns, including job 1 -> job 2 -> job 3. Checks cover the independent HOME height/horizontal tolerances, Space during RETURN, explicit outside-bay diagnosis, rejection of suspended loads, footprint-based release guidance, runtime voice/camera button binding and initialized portraits. Hook/load damping is now 1.2 / 0.5.

Landing validation directly examines the known active load each physics update. Missing trigger callbacks cannot leave an otherwise valid research delivery waiting. Acceptance still requires a matching released, aligned, upright, landed, stationary container with the whole footprint in its bay for 1.5 seconds. Return acceptance requires an empty hook within the displayed horizontal/height limits, <=1 m/s and a 0.5-second dwell. No manual next-job key is required.

Earlier hands-on desktop checks exercised keyboard pickup, precision, lift, carry, release and correction of a rejected placement. Their failures led to the overhead culling fix and corrected near-bay guidance. Final player startup/visual inspection and packaging are recorded in `UPGRADE_STATUS.md`; headset checks remain outstanding.


Final v3 release: `/tmp/vr-guided-v3-release-build.log` reports success. The independent Vulkan player starts at the monitor's native 3456 x 2160 in fullscreen; it no longer retains a 1600 x 900 window-test resolution. The archive is regenerated from v3 source and checked for ZIP CRC errors.


Cabin-side camera correction: `/tmp/vr-cabin-view-validation.log` reports `RESEARCH_CABIN_VIEW_CHECKS_PASSED` (exit 0). The check projects actual controller motion against each view: W/S map to up/down and D/A to right/left at four crane headings. This change concerns desktop camera framing; the earlier six physical deliveries remain the handling/progression check.

Final cabin framing check also verifies that the first pickup lies inside the camera frame. The cabin-side follow angle is steeper and frames the hook/current goal together. `/tmp/vr-cabin-framing-build.log` contains the passing direction/framing checks.


## Cabin / NPC v4

All six physical jobs across A/B passed with the cabin/NPC present (`/tmp/vr-cabin-npc-validation.log`). Additional assertions verify the live RenderTexture, removal of the portrait, visibility layers, cabin button and fourth-view wraparound. `ResearchCabinPreview.BuildAndRun` then passed direction projection checks for all four views/four headings, verified Maya's face inside the seated field of view, checked that the load camera and instrument panel are enabled, and rendered a cabin preview for inspection (`/tmp/vr-cabin-npc-final-preview.log`).

The NPC is an original procedural character with deterministic gestures and prerecorded speech. Joystick geometry follows actual commands; grab-based joystick manipulation is not implemented. Actual HMD tracking, cabin slew comfort and controller-ray UI remain lab checks. Editors exit directly after tests; the standalone simulator is left closed at the user's request.

V4 Linux release build completed successfully (`/tmp/vr-cabin-npc-release-build.log`). `ResearchCabin` and `ResearchInstructor` are present in the player assembly. The built player was briefly started for a startup check and closed afterward.
