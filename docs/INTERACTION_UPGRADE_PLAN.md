# PortLab interaction upgrade — plan and continuation handoff

Saved: 3 November 2026, Asia/Kolkata. **Resumed at the user's request.** See `docs/UPGRADE_STATUS.md` for the current implementation, verification and remaining work. The historical plan below records the original handoff; do not treat its old build/verification status as current.

## Goal and user feedback

A working seaport crane VR simulator for a Work System Design & Ergonomics lab. First demo was described as day after tomorrow on Nov 2 (Nov 4); final submission/presentation the following lab. Lab has Windows PCs and headsets/controllers; supplied photos look like Quest 3, not formally confirmed. Local Linux Unity is available without a headset.

Course topics: direct time study, PMTS/MTS, normal/standard time, work sampling, keyboard optimization, Hick's law, index of difficulty, reaction time. Preserve credible research data, but make the demonstration understandable and enjoyable without referring to chat.

User's latest feedback:
- Simulator feels stuck, slow, dead and insufficiently interactive.
- Default camera is too distant to operate accurately; other views also need improvement.
- Layouts are not easy to understand or interesting.
- HOME looks like a tiny dot in the sea; its purpose is unclear.
- Constant machinery audio is monotonous.
- Wants the female assistant originally discussed, visibly guiding and speaking the next step in the application.

## Intended experience

Start with a clear welcome from **Maya**, a professional female port instructor. A prominent Practice button begins the task. Maya explains one action at a time, while the scene highlights the actual pickup/drop target and reports alignment and height. The player can rotate, move, lower, attach, lift, carry and deliver with responsive controls. Every success/miss has understandable feedback. The next instruction always appears in the simulator.

Keep practice and measured runs explicitly separate. Use identical speeds, target tolerances, guidance and controls across the two measured layouts. Do not introduce unlogged automation, automatic snapping or physics shortcuts that invalidate measurements.

## Changes to finish, in priority order

### 1. Guided task loop and recovery
- Visible assistant portrait with subtle head/hand/mouth animation and bundled offline female speech.
- Guidance states: welcome → align → lower → attach → lift clear → carry → lower at bay → release → settle → return/next job → completion.
- Handle pause, reset, failed coupling, premature release, and misplaced loads with actionable guidance.
- Show live horizontal distance, height difference, current job and delivered count. Ideally show explicit left/right and inward/outward corrections, not just a distance.
- Repeat instruction and voice mute buttons; H repeats, M toggles voice on desktop.
- Debounce proximity speech and avoid repeating long messages or allowing stale instructions to finish long after a state change.
- Keep full captions when muted. Test all buttons after serialization into a player build.
- Consider a short practice-only tutorial progression that starts close to the first load. Do not silently auto-start measured recording.

### 2. Cameras and clear spatial feedback
- Default close follow-hook camera, stable in world orientation (not spinning with crane).
- Tab cycles follow, overhead alignment, full quay overview.
- Scroll zoom, RMB orbit; F resets the desktop view.
- Ground projection under hook; pulsing active-goal ring; readable floating label; ground route to destination.
- Make the target visible or its direction obvious when outside the follow view. A small overview map is an optional improvement if the route/labels alone are insufficient.
- Check the crane structure does not obscure the camera and that UI does not hide the working area.
- VR remains tracked; never apply desktop follow-camera motion to the real headset. Verify seated station visibility in the lab.

### 3. HOME and job transitions
- Replace tiny airborne dot with a large turquoise pad on the quay, a height beacon, and readable HOME / EMPTY HOOK label.
- Explain that parking the empty hook completes the return work element and starts the next job.
- Show distance and height for parking, plus visible success feedback.
- Current draft keeps the return element and increases tolerance from 1 m to 1.8 m. Test whether this is genuinely easy for a novice.
- If return still feels pointless, consider separate simplified practice flow, but retain a declared, consistent cycle boundary for measured runs.

### 4. Controls, pace and feedback
- Draft speeds: slew 19 deg/s (was 12), trolley local speed .105 (was .07), hoist 4 m/s (was 2.8); smoothness 7 (was 4).
- Shift precision at 30% speed on desktop. Ensure a comparable usable precision method for controllers if needed.
- Holdable on-screen motion buttons and a Lock / Release button, in addition to keyboard/controller support.
- Hook coupling radius draft 1.35 m. Avoid invisible failures: show ready-to-lock status and why an attempt fails.
- Check pointer release/focus loss clears held commands; no movement should resume unexpectedly after pausing.
- Sound for successful attachment/delivery, clear completed count, settling progress.

### 5. Layouts and scene readability
- Both layouts use the same sources, loads and validation rules.
- Draft A: receiving bays at angles 35/58/81 degrees, radius 22 m relative to original boom direction.
- Draft B: angles 28/45/62 degrees, radius 18 m. This is a combined receiving-layout comparison, not an isolated distance-only experiment.
- Target footprints widened to 5.6 × 3.8 m (was 4.6 × 2.8), with floor visuals matching collider dimensions.
- Preserve colored/numbered container-to-bay matching. Check bay overlap, spacing, ground height and accessible crane travel.
- Decorative stacked freight adds context without obstructing work. Avoid spending remaining demo time on elaborate scenery.

### 6. Audio and assistant presentation
- Remove old global looping AudioManager from research scene only.
- Motor plays quietly in response to actual commands; changes pitch with movement and ducks during speech. Subtle harbor ambience.
- Current assistant is an original procedural stylized 3D bust with hard hat, safety vest, hair bun and tablet, shown in desktop and VR consoles. It is not a realistic imported animated human.
- Inspect the portrait visually. Improve its proportions/lighting if needed; do not claim realism or lip synchronization (mouth movement is approximate).
- Voice generated offline using Piper. Final intended model: `en_US-ljspeech-high`, female, model card lists public-domain training dataset. Only generated WAV clips ship, not model/runtime.
- Record voice provenance, transcript and generator instructions in docs. Sources:
  - https://huggingface.co/rhasspy/piper-voices/raw/main/en/en_US/ljspeech/high/MODEL_CARD
  - https://keithito.com/LJ-Speech-Dataset/
- Initial test clips used lessac; overwrite ALL 13 clips with ljspeech before release. Regeneration subsequently completed successfully for all 13 clips (exit 0). Reimport them into Unity before building.

### 7. Research integrity and final packaging
- Version new runs `wsd-guided-v2`; keep old data untouched and do not combine old/new mechanics as one condition.
- Log updated settings and assistance configuration (including voice enabled/muted and precision use if used as an experimental variable).
- Main experiment: within-person layout A/B comparison, counterbalanced order, practice first, matched tasks; report cycle/element times and handling errors.
- Self-reported discomfort is not a validated physiological fatigue measurement. PMTS is a separate analytic estimate, not automatic inference from simulator timings. Hick/Fitts studies require controlled tasks and should remain optional rather than delaying this demo.
- Update guide, verification notes, README, screenshots and portable project ZIP after checks pass.

## What is already done versus unverified

### Previously completed, tested baseline
- Scene: `Assets/Scenes/SeaportResearch.unity`; original levels retained.
- Working crane physics, matching deliveries, four exclusive timing phases, A/B layouts, practice/measured tags, pause/abort/reset, CSV export, custom discomfort ratings.
- Baseline passed deterministic checks plus six physical pickups/deliveries across A and B in Unity Play mode.
- Linux player: `Builds/Linux/PortLab.x86_64`.
- Launcher: `tools/run_desktop_demo.sh`, Vulkan + fullscreen.
- Source ZIP: `Builds/PortLab_Unity_Project.zip`.
- **Player and ZIP are still the old baseline; they do NOT contain the interaction upgrade.**

### New draft changes already on disk
- Replaced `Assets/Scripts/Research/ResearchOperatorView.cs`: follow/overhead/overview, zoom/orbit, desktop reset; XR mode handling retained.
- Modified `ResearchCraneInput.cs`: pointer-held commands and Shift precision.
- New `ResearchHoldControl.cs`: pointer-down/up/exit motion control.
- New `ResearchGuide.cs`: state-based captions/voice, guidance rings, route, metrics, reactive audio, avatar animation and portrait RenderTexture.
- New `ResearchGuideConsole.cs`: runtime binding for repeat/mute/swap-order/view/coupling UI. Important: editor-only `AddListener` callbacks do not persist; this component was added to solve that.
- New `Assets/Editor/ResearchExperienceBuilder.cs`: upgrades the existing saved research scene, creates avatar/UI, moves HOME, adjusts speeds/layouts/tolerances, removes old looping audio; `BuildPlayer()` additionally builds Linux.
- New `tools/generate_guide_voice.py`, `Assets/Research/Voice/*.wav`, transcript JSON.
- `ResearchExperienceBuilder.Build` ran successfully once, log `/tmp/vr-guided-build-scene.log`, marker `RESEARCH_GUIDED_EXPERIENCE_BUILT`, exit 0. This proves initial compilation/scene generation only.
- **No new physical smoke test, rendered inspection, player build or interaction test has yet been completed. Do not describe the draft as validated.**

## Concrete continuation steps

1. Read this file and inspect current diff; preserve user changes and earlier baseline fixes.
2. Confirm voice generation finished and all 13 WAVs use ljspeech. Re-run generator if uncertain, then save provenance.
3. Review draft code for correctness before launching Unity:
   - Clear `ResearchCraneInput.PointerCommand` on focus loss/disable/pause.
   - Guide says “white cross”/“dotted route” but draft renders a ring/solid line: align wording or visuals.
   - Prevent misleading recovery directions immediately after a normal release while container settles.
   - Speech interruption/prioritization: do not leave old instructions playing during a different operation.
   - Assistant UI currently replaces original full panels. Ensure all `ResearchPanel` references and runtime button bindings exist.
   - New order dropdown is intentionally disabled with a separate swap-order button; verify caption/event/condition-order synchronization.
   - `ResearchExperienceBuilder.Build` must be safe when rerun (removes prior guided root and Hold controls).
   - Original `ResearchDemoBuilder.Build` regenerates baseline; either wire upgrade into the intended rebuild workflow or clearly document using Upgrade afterward.
   - Avatar portrait camera is in layer 30 at y=-200; verify URP lighting and camera output. Main cameras exclude its layer.
4. Run Unity compile/scene upgrade after final code/voice corrections.
5. Run `ResearchValidation.Run` in a separate Unity process; physical smoke driver uses actual crane/rope motion and tests six deliveries across layouts. Increased speeds might require test-driver gain adjustments without changing player physics.
6. Add/check targeted tests for guide transitions and serialized button functionality. Avoid large unrelated testing work.
7. Build Linux in a separate process, never immediately after exiting Play in the same editor process (known native crash).
8. Gracefully close the old running app, launch updated fullscreen Vulkan player. Visually inspect welcome, portrait, all three cameras, labels/HOME, controls and UI at actual resolution. Play at least one manual job without referring to chat; test speech mute/repeat, pause/focus loss, premature release/recovery, reset.
9. Fix usability issues found; confirm CSV/checkpoint/phase behavior still works.
10. Update screenshots/docs and recreate portable source ZIP; run ZIP CRC check and Python analysis tests. Leave improved app open only after it is verified.

## Commands and environment

Project: `/home/revrio/vr/vr-crane-simulator`
Unity: `/home/revrio/Unity/Hub/Editor/6000.0.26f1/Editor/Unity`
Use EXACT version 6000.0.26f1. Another editor is installed but is not used.

Scene upgrade:
```sh
/home/revrio/Unity/Hub/Editor/6000.0.26f1/Editor/Unity -batchmode -nographics -projectPath /home/revrio/vr/vr-crane-simulator -executeMethod ResearchExperienceBuilder.Build -quit -logFile /tmp/vr-guided-build-scene.log
```
Physical verification (do not pass `-quit`, driver exits itself):
```sh
/home/revrio/Unity/Hub/Editor/6000.0.26f1/Editor/Unity -batchmode -force-vulkan -projectPath /home/revrio/vr/vr-crane-simulator -executeMethod ResearchValidation.Run -logFile /tmp/vr-guided-validation.log
```
Build after validation, separate process:
```sh
/home/revrio/Unity/Hub/Editor/6000.0.26f1/Editor/Unity -batchmode -nographics -projectPath /home/revrio/vr/vr-crane-simulator -executeMethod ResearchBuilds.Linux -quit -logFile /tmp/vr-guided-player-build.log
```
Voice generator:
```sh
/tmp/portlab-voice-env/bin/python tools/generate_guide_voice.py /tmp/portlab-voice/ljspeech.onnx
```
Python tests: `python3 -m unittest discover -s tools -p 'test_*.py' -v`.
Unity commands previously required approved sandbox escalation for licensing/display/network.

## Useful continuity details / pitfalls

- Prior active fullscreen app unified exec session was `32077`, window `0x1400008`, title `VRCraneSimulator`, log `/tmp/vr-crane-fullscreen.log`. It may still be open; verify before operating. Do not kill unrelated applications.
- Voice generation ljspeech exec session `50459` finished successfully (exit 0), including `complete` and `stopped`: all 13 final WAV clips are generated. No voice process remains pending.
- The first scene upgrade used earlier voice files; reimport final WAVs before building.
- `ResearchPreviewCapture.CaptureCurrent` forces the old overview camera and manually renders a canvas; adapt it for new views/portrait or inspect actual standalone app screenshots. Do not mistake its old capture framing for new runtime default.
- `ResearchSmokeDriver` speeds time to 6×; speech/UI timing under that automated run is not a manual usability test.
- Unity native crash occurred in the baseline when exiting Play and building in the same process. Separate processes are reliable.
- Linux OpenXR package unsupported; desktop build temporarily disables XR initialization and restores settings. Real Quest/controller checks remain for the lab Windows PC.
- No Windows/Android build module here; source ZIP is intended for lab Unity/Windows Quest Link. Do not promise a tested Windows executable or headset functionality.
- Unity may modify `.vscode/settings.json` solution name and create Assets/Resources/PerformanceTest*.json; review generated noise, preserve user work.
- Earlier 34 FBX/WAV Git LFS pointers were restored to actual verified upstream binaries; these appear modified because git-lfs is absent. Do not revert them.
- Blender source was baked to `Assets/Research/PortMeshes.asset` and `PortMaterials.asset`; research scene is portable without Blender. Rebuilding original Level1-derived base may require `/tmp/blender-2.83.20-linux-x64` on PATH.
- Actual player CSV root: `/home/revrio/.config/unity3d/Negentra/VRCraneSimulator/ResearchSessions`. Preserve existing user runs. Test data goes to `/tmp/vr-research-physical-smoke` tagged AUTOMATED_SMOKE/practice.
- Existing docs: `docs/DEMO_GUIDE.md`, `docs/VERIFICATION.md`, `docs/WSD_ERGONOMICS_PLAN.md`. Update only when new behavior is verified.

## Definition of done

A first-time user can complete a practice delivery using only in-app guidance; assistant is visible and audible; cameras make pickup/placement readable; controls feel responsive; HOME is clearly on the quay and explained; motor stops being monotonous; both layout runs still complete with trustworthy CSV timing. Updated app and source archive must match, and remaining headset validation must be stated honestly.
