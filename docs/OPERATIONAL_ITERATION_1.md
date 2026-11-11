# Operational terminal — iteration 1

Historical first release: its scene is now `Assets/Scenes/OperationalPortIteration1.unity` and its Linux player launches with `tools/run_desktop_demo.sh --iteration1`. The current `OperationalPort.unity` and default operational player are iteration 2; see `OPERATIONAL_ITERATION_2.md`.

Preserved as a separate scene, `Assets/Scenes/OperationalPortIteration1.unity`. This iteration establishes **one complete truck → reachstacker → yard → player crane → ship → final park chain** with one persistent C1 container. The earlier three-container A/B/C research scene remains available as `SeaportResearch.unity`.

## Start it

Open a terminal and run:

```bash
/home/revrio/vr/vr-crane-simulator/tools/run_desktop_demo.sh --iteration1
```

The usual launcher selects the latest operational player. To open the older research player explicitly:

```bash
/home/revrio/vr/vr-crane-simulator/tools/run_desktop_demo.sh --research
```

In Unity **6000.0.26f1**, open `Assets/Scenes/OperationalPortIteration1.unity` and press Play.

## What to do

1. Press **P / Practice**. The truck arrives carrying C1 and stops at the transfer stand.
2. Watch the reachstacker align, lift C1 off the trailer, carry it along a curved route, lower it into the yard, release and reverse clear. The truck leaves empty. Intake takes roughly 30–35 seconds at normal speed; the crane is intentionally locked during it. The same container stays visible throughout.
3. At **YOUR TURN / PICKUP**, the desktop changes to load-follow view. Lower the empty spreader with **Down**; use the horizontal-gap/height readout to align over C1. Press **Space** when Maya says ready to lock.
4. Raise with **Up** until the cargo clears the quay and ship rail. Use **W** toward the ship, **S** toward the yard, and **A/D** along the quay. Use **Tab** for overhead alignment and **Shift** for slower movement.
5. Centre the full container footprint in **SHIP-01**, lower gently onto the actual ship deck, and release with **Space**. The container must be aligned, supported, released and nearly still for 1.2 seconds. Unsupported or outside-slot releases do not count; reattach and correct them.
6. After **DELIVERY ACCEPTED**, return the empty spreader toward the turquoise parking ring with S, and match its marked **10 m** height. The parking-gap readout guides this final movement. Parking occurs once at the end of the shipment.
7. The completed report shows intake time, operator time and contact episodes. **R** resets the entire chain; another Practice run creates a separate export.

## Controls

| Input | Action |
|---|---|
| P / Enter | Start a practice chain |
| A / D | Move the gantry along the quay |
| W / S | Move trolley shipward / yardward |
| Up / Down | Raise / lower spreader |
| Space | Lock / release C1 |
| Shift | 30% precision speed |
| Tab | Terminal → load follow → overhead → cabin |
| C | Cabin directly |
| Right mouse drag | Orbit external view / look around cabin |
| Mouse wheel | Follow-view zoom |
| F | Reset view / recenter headset before operating |
| Esc | Pause/resume the whole chain |
| R | Reset; preserve an active run as interrupted |
| M | Toggle Maya's voice |

On-screen hold buttons also move the crane. These are gantry controls: A/D does not slew the old tower-crane boom. In VR, left stick X moves along the quay, left stick Y moves the trolley, right stick Y hoists, right A couples/releases, and right grip provides precision. Use controller rays for the console buttons. Actual headset/controller verification remains a lab task.

## Implemented in this part

- A separate, reachable quay/ship geometry with a gantry, trolley, four visible cables and a detailed container spreader.
- A truck with visible cargo, parking/braking wait, empty departure and turning wheels.
- A reachstacker with extending/lifting boom, rotator/spreader, forward curved carry and reverse clearance route.
- Explicit cargo custody at every handoff; one C1 rigidbody, no duplicate replacement boxes.
- Physical crane acquisition, lift, sway, release, supported ship placement, failed-release recovery and final parking.
- Maya NPC, stage-specific captions, reused offline female action-coaching clips, live load monitor, four desktop views and VR input/view path.
- Movement-dependent vehicle/crane motors, lock/impact/success sounds and basic cargo contact warnings/logging.
- Pause/reset/close handling and separate operational CSV exports.

Vehicle shapes and motion are a procedural first-iteration model. NPC intake uses controlled kinematic custody; player handling uses rigidbody/joint/rope physics. The visible four cables share one spring-rope force model. This release is a functional workflow prototype, not a calibrated industrial crane model.

## Data

All iteration-one runs are explicitly **practice**. They are stored under `Application.persistentDataPath/OperationalSessions`, separate from the v6 research records. The in-app folder button opens the actual run folder when the operation is stopped or complete.

- `runs.csv`: schema `op1`, build/version, practice and automated flags, input mode, cargo count, active unpaused duration and status.
- `phase_times.csv`: exclusive truck, handler and operator-stage durations. Their sum reconciles to active run time.
- `events.csv`: stage transitions, custody handoffs, coupling/release, contact episodes, pause/resume, abort and completion.
- `scenario.json`: geometry, reach bounds, speeds, target and route settings.

Operational waiting is part of this practice chain's elapsed time; software pause is excluded. Accelerated automated test durations are not participant findings. The original research schemas and analysis continue to belong to the separate v6 scene.

## Verification and rebuild

`OperationalPortValidation.Run` exercises the chain in Play mode. `BuildAndRun` regenerates it first. The test pauses intake, resets after unloading, checks interrupted-data preservation, performs physical pickup, deliberately releases outside the ship, recovers, lands/releases on the actual deck, parks and verifies CSV/clock reconciliation. It also checks suspended/rotated-outside placements and persistent cargo identity. Automated exports use `/tmp/port-iteration1-physical` with `automated=true`.

`OperationalPortPreview.Run` checks and renders the cabin, Maya and live load feed. Images are in `docs/images/operational-*.png`.

Editor menu: **VR Research Demo → Build Operational Iteration One** regenerates this scene from the saved v6 assets. **Build Operational Linux** produces `Builds/OperationalLinux/PortLabOperations.x86_64`. Windows has a corresponding menu entry but needs the matching Windows Build Support module, which is not installed here. Build in a fresh editor process after Play tests.

## Next iteration

Extend this proven chain to four/five cargo identities and a move/dependency graph, multiple yard layouts, stack placement/removal, a temporary rehandle slot and elevated ship tiers. Later parts add walking workers/crossings, additional traffic/cranes and richer incident effects. These are described in `OPERATIONAL_PORT_UPGRADE_PLAN.md`; they are not claimed as part of this first iteration.
