# Operational terminal — iteration 2

Historical release: its scene is preserved as `Assets/Scenes/OperationalPortIteration2.unity`; launch its player with `tools/run_desktop_demo.sh --iteration2`. The current operational scene/player is iteration 3; see `OPERATIONAL_ITERATION_3.md`.

Open `Assets/Scenes/OperationalPortIteration2.unity` in Unity **6000.0.26f1**, then press Play. On this Linux machine, run `tools/run_desktop_demo.sh --iteration2` for the preserved desktop player. The first operational scene is preserved as `Assets/Scenes/OperationalPortIteration1.unity`; its previous Linux player is available with `tools/run_desktop_demo.sh --iteration1`. The older three-job research scene/player remains available with `--research`.

To use the Windows lab PC, copy/extract `Builds/PortLab_Unity_Project.zip`, open its project folder in Unity 6000.0.26f1, open the operational scene and press Play. The menu **VR Research Demo → Build Operational Windows** creates a Windows player when the matching build-support module is installed. Connect the headset through the lab's existing PC VR setup; see `DEMO_GUIDE.md` for the earlier Quest Link guidance. The supplied Linux desktop player is for this machine.

## Choose a task

| Key / layout | Starting arrangement | What you do |
| --- | --- | --- |
| 1 / A — Flat | Five accessible containers in two rows | Five loading moves; finish with a two-tier ship stow |
| 2 / B — Stacks | One three-high stack and one two-high stack | Retrieve top-down, lowering the pickup height for each tier; five ship moves |
| 3 / C — Retrieve | C2 blocks priority container C5 | Put C2 in TEMP-01, ship C5 first, then retrieve C2 again later; six moves |
| 4 / D — Build | Five containers initially on the ground | Put C2 on C1, unstack C2 again, then ship all five; six moves |

Choose the layout before starting. **P / Staged practice** starts immediately with the selected arrangement already in the yard. **I / Truck intake** starts five truck visits: each container is unloaded by the reachstacker into its selected source position, including upper tiers. Stack bases arrive first. The same five cargo objects continue through intake, yard, crane and ship; containers are not substituted at the handoffs. Reset before choosing another layout.

## Operating the crane

- **A/D:** move the gantry along the quay. **W/S:** move the trolley shipward/yardward.
- **Up/Down:** raise/lower the spreader. **Shift:** fine control. **Space:** lock/release.
- **Tab:** terminal, load-follow, overhead and cabin views. **C:** cabin. **F:** recenter the current view. Hold the right mouse button to look/orbit; scroll to zoom in the external views.
- **Esc:** pause/resume; freezes actors, physics and the active clock. **R:** reset, preserving partial records. **M:** voice on/off.
- VR input path: left stick X gantry / Y trolley; right stick Y hoist; right A lock/release; right grip fine control. Headset operation still needs a lab hardware check.

Maya's captions name the current cargo, destination and reason for an extra move. Yellow marks the pickup roof; turquoise marks the receiving surface, including the top of a supporting container. The right console shows the planned ship order, current move and alignment/height error. Cargo side/roof IDs distinguish C1–C5.

Align the spreader above the assigned container and lower to its roof. Lock with Space. Raise to the indicated **13 m hook height** before travelling across stacks, then align over the destination and lower slowly. Upper-tier placements need tighter alignment than ground slots. Release and wait for the supported placement to settle and be accepted. The next pickup is assigned automatically. A bottom container cannot be lifted while another accepted container rests on it.

There is **no HOME return between moves**. After all five containers are aboard, park the empty spreader once at the turquoise quay ring, at 10 m height. Completion requires this final parking step.

If released outside its destination, the cargo remains physical and can be picked up again. Acceptance requires the full footprint inside the receiving outline, upright orientation, low movement, the correct support directly underneath and 1.2 seconds of stability. A load hanging above the slot does not count.

## Records for the lab

The console's **Open operational CSV folder** button opens the latest run after it ends. Sessions live in Unity's persistent data folder under `OperationalSessions`. New multi-container sessions use **schema op2** and include:

- `runs.csv`: build, layout, intake mode, input mode, five-container count, total active time, practice/automated flags and status.
- `moves.csv`: unique move ID, cargo ID, source/destination, operation (Ship / Rehandle / Stack), source/target tier (0 = base, 1 = second level, 2 = third level), empty reposition, pickup, transfer and placement times, and outcome. C2 has two separate move IDs when handled twice.
- `phase_times.csv`: all exclusive phase totals, including automated intake, manual handling and final parking.
- `events.csv`: custody changes, acquisitions, releases, accepted placements, coupling misses, contacts, pauses and aborts, with move identity.
- `scenario.json`: the exact slot/move plan and assistance settings.

Analyze a copied session folder with:

```bash
python3 tools/analyze_operational_sessions.py /path/to/OperationalSessions --output /path/to/results
```

The summary keeps build, layout, staged/full intake, input mode, cargo count, practice status and automated status separate. It reports observed durations and checks clock reconciliation. It does not pool an aborted run with completed times.

This iteration records **practice data**. Compare comparable staged runs to explore the time cost of height changes and rehandling. The C/D tasks deliberately require an extra move, so they are different methods/tasks, not a clean layout-only comparison. Full-intake totals include machine waiting and must be kept separate from staged operator times. Simulator time is not an MTM/PMTS table value; normal/standard times still need the course's explicit performance-rating and allowance assumptions. Fatigue is not inferred from elapsed time or collisions.

## Simulation limits and next part

Settled, accepted yard/ship containers are anchored until their permitted pickup. This prevents numerical stack drift during a student demo; placements still land physically and are verified before anchoring. Anchoring is recorded in the scenario settings. Stacks do not simulate collapse or structural damage. Source layouts and move orders are guided presets rather than a free-form terminal editor. Vehicle paths are scripted, and the next truck visit resets at the gate after the preceding unloading sequence clears.

Moving workers, active traffic conflicts and richer collision feedback belong to the next iteration. Current cargo contact warnings, impact sound, motor sounds, Maya and the animated harbor are retained. This is a teaching simulator, not an industrial crane-training model.

## Verification

Eight completed automated practice runs cover every layout with staged practice and full five-truck intake: **44 physical moves**, including the extra yard-stack/rehandle moves. Pause, reset/abort, premature release/recovery, actual receiving support, final park and CSV clock reconciliation pass. The original one-container scene also passes its regression check. All four final layout previews, cabin/Maya/live feed and the packaged desktop's layout/start/cabin/clean-close controls were checked. See `VERIFICATION.md` for logs and the native editor crash encountered during one additional batch; fresh isolated reruns pass. Actual headset checks remain outstanding.
