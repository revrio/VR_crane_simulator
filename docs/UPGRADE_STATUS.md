# Guided upgrade status — 5 November 2026

## Operational terminal — iteration 3

Implemented in `Assets/Scenes/OperationalPort.unity`: four articulated workers; marked side lanes; controlled cargo-aisle and rear-road crossings; two gated road trucks; barriers, parked trailer and stored-freight colliders; connected terminal hinterland/warehouses; two animated neighbouring gantries; a fifth overview view (V); full-width spreader collision shapes; grouped contact and clearance episodes; contact-local flecks/halo/clank and requested controller haptics; protected hold with acknowledgement, worker clearance and explicit resume. Eight new offline Maya clips use the same Piper/LJSpeech female voice. Op3 records add traffic policy, incident episodes, acknowledgement timing and delay subsets. The first/second operational scenes and v6 research scene are preserved byte-for-byte. Second player backup: `Builds/OperationalIteration2Linux`, launched with `tools/run_desktop_demo.sh --iteration2`.

Completed physics verification: all four layouts with controlled crossings (22 moves, `/tmp/port-iteration3-final-core.log`), actual intrusion/attached-load hold and recovery (5 moves, `/tmp/port-iteration3-drill.log`), five-truck stacked intake including a stop during truck parking (5 moves, `/tmp/port-iteration3-full-intake.log`), and separated traffic with priority retrieval (6 moves, `/tmp/port-iteration3-separated.log`). All exit 0: **38 accepted moves across seven completed practice runs**. The clean normal-handling runs have zero contacts and zero near misses. `/tmp/port-iteration3-contacts-recovery.log` independently passes real barrier clearance, initial scrape, compound-contact dedup/escalation, fast strong impact, actual worker contact, clearance/resume and reset/export preservation (exit 0).

`/tmp/port-iteration3-final-preview.log` regenerates the final scene and passes all five views, actual worker/truck/crane motion, rear trucks in overview, cabin/live feed, offline welcome/stop/resume speech, warning tone, frozen actors and contact-local particles that actually emit/simulate (exit 0). It also checks an intake stop is not attributed to an operator move that has not started. Final arena/cabin/stop screenshots were inspected. The last visual update connects the quay to terminal land/access roads/warehouses; interactive cargo slots and crane limits are unchanged.

The analysis of the seven complete verification runs produces seven separate task groups and 38 move groups with zero reconciliation warnings. Seven Python analysis tests and launcher shell checks pass. All timing samples are automated practice checks, not participant findings. Final Linux build succeeds cleanly in `/tmp/port-iteration3-final-build.log` (exit 0; `Build Finished, Result: Success`, `RESEARCH_PLAYER_BUILT`). The `PortLab Operations` player opens at native 3456 × 2160 fullscreen. A real packaged-keyboard check selected intrusion traffic, started staged practice, opened V overview, latched X stop, acknowledged/resumed with E and entered the cabin; saved run/view/stop events confirm each action (`/tmp/port-iteration3-player.log`). Both externally driven sessions are explicitly flagged automated practice. Normal window close exits 0, preserves aborted records and produces no managed runtime exceptions. The actual client window was inspected (`/tmp/port-iteration3-player.png`). Both Unity and the player are closed. The portable source ZIP is refreshed and CRC checked. Read `OPERATIONAL_ITERATION_3.md` for controls and data definitions.

Remaining work is final study/ratings/analysis presentation and actual Windows headset/controller/performance validation. Protected stops and anchored supports are simulation assistance; no real braking, injury, stack-collapse or industrial certification claims are made.

## Operational terminal — iteration 2

Implemented in `Assets/Scenes/OperationalPort.unity`: five persistent C1–C5 bodies; four selectable flat/stacked/priority/build-stack tasks; immediate staged practice or five bottom-first truck/reachstacker deliveries; guided yard stacking/unstacking and temporary rehandling; supported two-tier ship stow; one final park; op2 per-move records with separate IDs for repeated handling. Source/destination markers, Maya captions, input, pause/reset, wrong-release recovery and existing cabin/live feed are integrated. Settled accepted supports are anchored; this assistance is recorded and documented in `OPERATIONAL_ITERATION_2.md`.

`/tmp/port-iteration2-physical.log` passed 27 physical moves: all four staged presets plus five-truck stacked intake (exit 0). It covers persistent body identity, move ordering, blocked lower pickups, actual yard/ship stacking, rehandle, reset/partial export, attached-load pause, premature release/recovery, final park, move/phase time reconciliation and healthy op2 CSV. `/tmp/port-iteration2-legacy-regression.log` passes the original one-container physical chain and guards (exit 0). The copied first scene, its original config and the v6 research scene match the previous source ZIP byte-for-byte. First player backup: `Builds/OperationalIteration1Linux`; launch with `tools/run_desktop_demo.sh --iteration1`.

Additional full-intake A passed in `/tmp/port-iteration2-additional-intakes.log`; that long editor batch then hit a native Mono SIGSEGV during C's last transfer, with no failed task assertion. Fresh isolated C and D sessions both pass and exit cleanly in `/tmp/port-iteration2-priority-intake.log` and `/tmp/port-iteration2-stack-intake.log`. In total, eight completed practice runs cover all four presets in both intake modes: **44 accepted physical moves**. Analysis retains eight separate task groups and 44 move groups with zero reconciliation warnings; aborted and the unfinished crash run remain separate. Five Python analysis tests and launcher shell checks pass.

`/tmp/port-iteration2-release-preview.log` regenerates the final scene and passes all four preset previews plus cabin/Maya/live-load-feed rendering (exit 0). Images `docs/images/operational-i2-layout-*.png` and `operational-i2-cabin.png` were inspected. Final Linux build succeeds in `/tmp/port-iteration2-final-build.log` (exit 0; `Build Finished, Result: Success`, `RESEARCH_PLAYER_BUILT`). The standalone `PortLab Operations` player opens at native 3456 × 2160 fullscreen. An actual desktop keyboard smoke check selects layout C, starts its five-container staged task and enters the cabin, confirmed by saved run/view events and inspection of the captured client window (`/tmp/port-iteration2-player.png`). Normal window close exits 0, preserves interrupted records and produces no managed runtime exceptions (`/tmp/port-iteration2-player.log`). Those externally driven practice checks were moved intact into `/tmp/port-iteration2-native-input` so they are not mixed with user sessions. Both Unity and the player are closed. The portable source ZIP is regenerated and CRC checked.

Next iteration remains moving workers, active traffic/crossing conflicts and richer collision feedback. Do not claim those features or actual headset verification are complete.

## Operational terminal — iteration 1 (historical release)

The user authorised implementation in parts. First part is implemented in **`Assets/Scenes/OperationalPort.unity`**: one visible truck delivery, parked-trailer handoff to a reachstacker, curved carry into the yard, physical release/machine clearance, player gantry-crane pickup, real ship-deck placement and one final parking operation. One persistent C1 rigidbody is used through the entire chain. Includes Maya/captions/action speech, live load feed, four views, keyboard/mouse/XR input path, vehicle/crane motors, lock/contact/success sounds, pause/reset/close handling and separate `op1` practice exports. Read `OPERATIONAL_ITERATION_1.md` for controls and launch instructions.

`/tmp/port-iteration1-final-validation.log` passed the complete physics chain, pause, reset/aborted export preservation, wrong/unsupported release recovery, full-footprint/height rejection, ship support/dwell, final park, persistent cargo identity and phase-clock reconciliation (exit 0). A camera disposal-order exception was found on test shutdown and fixed. `/tmp/port-iteration1-final-preview.log` then passed cabin/Maya/live-feed rendering and clean exit without that exception (exit 0). Final render captures are `docs/images/operational-*.png`. Python baseline-analysis tests and launcher shell checks pass. V6's saved scene was checked byte-for-byte against its prior source archive and is preserved.

The release scene also moves decorative stored freight clear of truck and handler routes; the updated scene/cabin passed `/tmp/port-iteration1-release-preview.log` (exit 0). Linux release build succeeded in `/tmp/port-iteration1-release-build.log` (`Build Finished, Result: Success`, `RESEARCH_PLAYER_BUILT Builds/OperationalLinux/PortLabOperations.x86_64`, exit 0). The independent `PortLab Operations` player starts fullscreen at native 3456 × 2160 without managed startup exceptions (`/tmp/port-iteration1-player.log`). The source ZIP is refreshed after these documentation updates. The general desktop launcher now selects the operational player when present; `--research` explicitly selects v6. The full five-container, worker/crossing, rehandling and broader incident scope remains in `OPERATIONAL_PORT_UPGRADE_PLAN.md`; do not claim those later iterations are implemented.

## Detailed port and stacked C — v6

The paused follow-up was resumed. Implemented a cabin-visible boom/rails and moving trolley supporting four steel hoist cables, a detailed container spreader/sheave head with twistlocks/hazard markings, corrugated task containers with door hardware/corner castings/IDs/weathered paint, and 40 decorative freight containers around the quay edges/behind the crane. Visual details preserve the original task collider/validation footprint. HOME's floating height marker is now a ring so it does not obscure the spreader; its endpoint/tolerances and progression rules are unchanged.

Layout C is a physical three-high pickup stack: C1 top, C2 middle, C3 bottom, with separate ground receiving bays at the B positions. Added the C button and keyboard 3; the order selector supports six ABC orders plus AB/BA, and Next layout follows that order. C changes source arrangement/height, so document it separately from the controlled A/B receiving-layout comparison. Version: `wsd-detailed-port-v6`.

`/tmp/vr-details-v6-physical.log`: pure checks and all nine physical deliveries across A/B/C passed, including stable top-down pickups, release/dwell, HOME progression, runtime C button, selected-order cycling, CSV and interrupted-run preservation (exit 0). `/tmp/vr-details-v6-final-preview.log`: all four camera directions at four headings, cabin-visible boom, preserved collision footprint, scenery without colliders, readable detailed-container/spreader/cabin captures and animated water passed (exit 0). Analysis of the automated exports retains three groups with three cycles each and zero reconciliation warnings. These are automated practice data, not participant findings. Python unit tests pass.

Linux release build succeeded (`/tmp/vr-details-v6-build.log`, exit 0, `Build Finished, Result: Success`, `RESEARCH_PLAYER_BUILT`). The independent player starts fullscreen at native 3456 × 2160 with no managed startup exceptions (`/tmp/vr-details-v6-player.log`). The portable project archive is regenerated from this release. `PORT_DETAIL_LAYOUT_C_CHECKPOINT.md` is the historical handoff, superseded by this status.

## Current work — harbor v5, 4 November 2026

User requested removal of the duplicate menu in the cabin and a livelier ocean. Removed the entire cabin instrument/menu screen, keeping the useful live load-camera monitor and existing right console (world-space console in XR). Added an extended ocean mesh with gentle shader-driven swell, crossed moving ripples, sun highlights and horizon reflection; a blue sky with drifting procedural clouds and haze; and a slow background tugboat with no physics colliders. Scenery is identical across A/B. Version `wsd-living-harbor-v5` separates new sessions from earlier pilots.

Graphics-enabled Unity checks passed in `/tmp/vr-harbor-v5-final-preview.log` (exit 0): all four camera views at four headings, no duplicate cabin menu, retained desktop sidebar/live load feed, Maya visible from the seat, supported water/sky shaders, scenery without physics colliders, and visible water changes between two rendered frames. Captures: `docs/images/operator-cabin.png` and `docs/images/harbor-atmosphere.png`. The user's subsequent requests to start the simulator supersede the older AFK close instruction in the historical notes.

Linux v5 release build succeeded (exit 0; `/tmp/vr-harbor-v5-build.log`, `Build Finished, Result: Success`, `RESEARCH_PLAYER_BUILT`). The source archive is regenerated from this version. Previous participant sessions are retained.

The user resumed the interaction plan and specifically reported that landing/releasing did not start job 2.

Implemented in source and generated scene:
- Maya: visible stylized animated female port instructor with 13 offline speech clips, captions, repeat/mute, contextual alignment and height guidance.
- Follow / overhead / overview cameras, zoom/orbit, responsive viewport and controls for 16:10 as well as 16:9; overhead hides crane structure to prevent boom occlusion.
- Faster crane, Shift / right-grip precision, on-screen hold controls, quiet movement-dependent motor and harbor ambience.
- Clear quay HOME pad and height beacon; measured runs remain separate from practice, device mode and build version.
- `wsd-guided-v3`: footprint-based landing prompts match actual acceptance. Explicit reasons for rejected placements. Research validation directly checks the known active load rather than depending only on trigger callbacks. A stationary suspended container cannot count as landed.
- HOME uses separately displayed 2 m horizontal / +/-2 m height limits, speed <=1 m/s and 0.5 s dwell. Space during RETURN is harmless. Job 2 begins automatically after HOME acceptance.
- Hook/load damping 1.2 / 0.5 for better settling. Actual settings are recorded in setup.json.

Earlier v2 tests passed six physical deliveries across A/B and a Linux player build. Hands-on keyboard checks exercised pickup, precision, lift, carry, release and rejected-placement feedback. Those checks found overhead obstruction and misleading near-bay lift prompts; both are fixed in v3 source.

V3 is implemented and validated. The final Linux player and portable source ZIP are rebuilt from this version. Headset/controller validation still requires the lab hardware.


V3 physical validation completed successfully (exit 0): `/tmp/vr-guided-v3-validation.log`, markers `RESEARCH_PURE_CHECKS_PASSED` and `RESEARCH_PHYSICAL_SMOKE_PASSED`. Both A/B advanced through all three tasks with physical release/dwell and HOME return. Targeted regression checks cover the user's stuck-progression report. Python analysis tests also pass and different build versions are not pooled.


Final release build: `/tmp/vr-guided-v3-release-build.log` contains `Build Finished, Result: Success` and `RESEARCH_PLAYER_BUILT`. Final player: `Builds/Linux/PortLab.x86_64`; startup log `/tmp/vr-guided-v3-release-player.log` confirms native 3456 x 2160 fullscreen after overriding the previous 1600 x 900 window preference. No managed startup exceptions were found. The app is open; user practice data is preserved.

Portable project: `Builds/PortLab_Unity_Project.zip`, recreated with `python3 tools/package_project.py` and CRC verified. It includes current source, scene, voice assets, settings, guides and analysis tools, with no Library/cache or old player build. The README and DEMO_GUIDE describe v3 and the release -> acceptance -> HOME -> next-job sequence. Optional separate Hick/RT tasks and real headset validation remain outside this desktop upgrade.


Camera orientation correction: desktop views now face outward along the boom from the cabin side. Overhead screen-up means outward (W); screen-down means inward (S); A/D turn left/right. The overview uses the same reference. `ResearchViewValidation.Run` passed actual crane-motion projection checks for both axes, all three views and four headings (0/90/180/270 degrees), log `/tmp/vr-cabin-view-validation.log`. Camera reference `cabinside-boom-relative-v1` is saved in setup metadata and `/cabin-view-v1` is appended to the build ID for analysis grouping.

Cabin-view release built successfully: `/tmp/vr-cabin-view-build.log`, `RESEARCH_PLAYER_BUILT`. The fullscreen player was restarted with this camera correction. Source ZIP is refreshed from the same version.

Final cabin-side framing also includes the active goal, and a projection assertion verifies the first pickup remains on screen. Latest validation/build log: `/tmp/vr-cabin-framing-build.log`.

Final cabin direction/framing validation and player build both succeeded in `/tmp/vr-cabin-framing-build.log`. The restarted fullscreen player uses that build; the portable project ZIP was refreshed afterward.


## Cabin and NPC v4

Implemented the requested fourth, seated cabin view (**C**, Cabin button or Tab). Includes seat/backrest/armrests, animated joystick indicators, window/open lower viewing area, live load camera and interactive instrument screen. Maya is now a full-body stylized NPC beside the operator, with greeting, head-look, task-pointing and approximate mouth animation. The fixed portrait is removed. Headset calibration uses the cabin eye; the tracked rig follows the virtual cabin frame during slew.

Validation: `/tmp/vr-cabin-npc-validation.log` passed pure checks, all six physical deliveries/returns and cabin UI/monitor/NPC initialization checks (exit 0). `/tmp/vr-cabin-npc-final-preview.log` passed camera-direction checks in all four views at four headings, actual seated NPC visibility, live camera/instrument state and a rendered cabin capture (exit 0). The rendered image is `docs/images/operator-cabin.png`. Python analysis checks pass. The controls and monitor layout are simplified course-demo assets; real headset testing remains for the lab.

The user asked that Unity be closed after testing while they are AFK. Do not leave the editor or standalone simulator running after packaging. Use `tools/run_desktop_demo.sh` when they return.

V4 release build succeeded (`/tmp/vr-cabin-npc-release-build.log`, `RESEARCH_PLAYER_BUILT`). The player assembly includes the cabin and instructor components. Final project ZIP is regenerated after verification; Unity Editor and the simulator are closed after testing at the user's request.
