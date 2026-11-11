# Historical pause checkpoint — crane details and stacked Layout C

**Resumed and implemented as v6 on 4 November 2026.** The physical and rendered checks have passed. See `UPGRADE_STATUS.md` and `VERIFICATION.md` for current release status. The original pause record below is historical and does not describe the current source/scene.

Paused at the user's explicit request on 4 November 2026. Resume only when requested. No new build or packaging has been done for this work. The last working player and source ZIP are still the verified **v5 animated-harbor release**.

## Authorized requests

- Cabin view: hook/cable appears to hang from empty air because the entire original crane is hidden in this view. Show an overhead boom and trolley that visibly support the cable.
- Add extra container stacks in the open port space near the crane.
- Give C1/C2/C3 realistic shipping-container details rather than plain box appearance.
- Improve the hook/spreader and hoist cable visually.
- Add a third layout with stacked containers. Stated implementation assumption: **C1 on top, C2 in the middle, C3 at the bottom**, picked top-down and delivered to separate ground-level bays. No user objection before pause.

User also asked why HOME is necessary. Explained that it provides a common endpoint for the four-element timed cycle, and that real operators would often return toward the next pickup. **No change to HOME behavior was requested or made.**

## Partial source edits already made (not regenerated/tested/built)

- `ExperimentSession.cs`: optional `layoutC`, default order ABC, A/B/C ready message, `SelectNextLayout()` follows selected condition order.
- `ResearchPanel.cs`: optional C button, C heading, supported orders AB/BA/ABC/ACB/BAC/BCA/CAB/CBA, uses selected order in exports, cycles next layout and blocks C changes during active runs.
- `ResearchGuideConsole.cs`: order cycling uses the order list instead of toggling AB/BA.
- `ResearchExperienceBuilder.cs`: three layout buttons, compact condition-order labels, removed the old six plain decorative freight cubes. **No Layout C asset or new detail geometry exists yet.**
- `ResearchGuide.cs`: welcome caption for C explains top-down unloading.
- `ResearchOperatorView.cs`: layer 27 reserved for a new cabin-visible boom assembly; enabled in the cabin/XR and hidden in external views. Original layer 29 still hidden in cabin/overhead.
- `ResearchCabinBuilder.cs`: load-camera mask excludes layer 27 to keep the feed unobstructed.

An invocation of a not-yet-created `ResearchPortDetailBuilder.Apply` was removed at pause so it does not leave an unresolved type reference. Add it back after implementing that class. The saved scene is still v5 and has not been regenerated with these partial source changes. Do not describe the requested features as complete.

## Intended next implementation

1. Add an editor builder (`ResearchPortDetailBuilder`) and a runtime visual component (`ResearchRigVisual` or similar).
2. Create a cabin-only overhead steel boom/girder and rail structure on layer 27, aligned with the actual crane slew and trolley range. Keep its relationship to the existing crane/car positions coherent. Avoid bringing the original near-camera geometry back into the cabin. Show the actual trolley, hoist sheaves and cable attachment overhead. Exterior views can retain the original boom.
3. Replace the visible placeholder spreader with a yellow container lifting frame, corner twistlocks, central hoist/sheave head, mechanical details and hazard markings. Four geometric steel hoist cables should join trolley and head block and follow the existing hook movement in LateUpdate. Keep the existing spring-rope physics, coupling point/radius and input behavior.
4. Replace task-container visuals with corrugated steel sides/roof, edge rails/posts, end doors, hinges/locking bars, corner castings, serial/owner markings, subtle weathered paint and clear C1/C2/C3 labels. Preserve the existing **3.4 × 1.3 × 1.6 m collider/validation footprint** unless intentionally updating all related geometry and tests. Keep added decoration within that footprint. Reuse shared combined meshes/materials for performance; avoid adding physics colliders to visual details.
5. Add roughly two dozen decorative stack positions around quay edges/behind the crane, with varied heights and colors. Keep the active A/B/C pickup/delivery sweep and HOME free. Decorative objects should have no colliders or experiment telemetry effects. Prefer shared meshes/instanced materials.
6. Create `Assets/Research/Layouts/LayoutC.asset`, assign `session.layoutC`. Three sources share x/z with centers separated vertically by 1.3 m: top C1, middle C2, bottom C3. Bottom center = deck top 31.2 + 0.65. Use a reachable existing pickup radius (~22 m). Ground-level receiving bays can use the B arrangement. Preserve actual rigidbody stacking; confirm stability when BeginRun unfreezes all bodies. Layout C changes pickup height/source arrangement, so describe it as a stacked-pickup condition separately from the A/B receiving-layout comparison.
7. Version the final integrated scene separately (e.g. `wsd-detailed-port-v6`) and update setup label. Initialize the three-condition order in the new generated scene, while keeping AB/BA available for the original comparison.

## Required verification before release

- Update `ResearchSmokeDriver` for **three layouts / nine physical deliveries**, including C top-down acquisition, real release/dwell, HOME progression, pause/reset/CSV. It currently only runs A/B and its crane-layer assertion still assumes no replacement cabin boom; update the assertion to require layer 27 visible and layer 29 hidden in the cabin.
- Check that Layout C is selectable via its serialized UI button, resets to the three-high stack, cannot switch during a run, and exports layout C and selected order. Verify next-layout order cycling.
- Confirm container detailing does not change collider bounds or introduce protrusions inconsistent with target validation.
- Render cabin view with supporting boom/trolley/cables visible and no near-camera obstruction; render all three layouts, overhead and close-up container/spreader detail. Extend the preview helpers as needed.
- Existing `ResearchViewValidation.Run` checks four views × four headings. Keep passing camera direction/framing behavior.
- `tools/analyze_sessions.py` already groups arbitrary layout IDs in summaries; its paired-comparison output specifically compares A/B. Decide whether a separate C comparison output is useful; do not quietly reinterpret A/B results. Record C as its own condition.
- Build Linux in a **fresh editor process** after Play-mode tests. Tests should exit Unity directly, avoiding the known Linux Unity native crash when leaving Play mode and building in the same process.
- Refresh README, DEMO_GUIDE, UPGRADE_STATUS, VERIFICATION and the source ZIP only after results are real. Retain prior user sessions and original uncommitted work/LFS restorations.

## Environment and commands

Project: `/home/revrio/vr/vr-crane-simulator`

Unity: `/home/revrio/Unity/Hub/Editor/6000.0.26f1/Editor/Unity`

Graphics-enabled batch example: `Unity -batchmode -force-vulkan -projectPath /home/revrio/vr/vr-crane-simulator -executeMethod ResearchCabinPreview.BuildAndRun -logFile /tmp/vr-details-v6-preview.log`

Physical test: `ResearchValidation.Run` (exits itself).

Build: `Unity -batchmode -quit -force-vulkan -projectPath /home/revrio/vr/vr-crane-simulator -executeMethod ResearchBuilds.Linux -logFile /tmp/vr-details-v6-build.log`

Package: `python3 tools/package_project.py`

Run last working player: `tools/run_desktop_demo.sh`

The user previously requested closing Unity after AFK testing, then later repeatedly requested opening the player. During this paused work, no Unity test/build was launched and no player was closed. Do not infer that a player should be restarted merely from resuming code work.

No headset is available here. Windows/VR lab validation remains outstanding. The v5 harbor rendering and player already passed their checks; avoid redoing unrelated work.
