# Operational terminal — iteration 3

Open `Assets/Scenes/OperationalPort.unity` in Unity 6000.0.26f1 and press Play, or use `tools/run_operational_demo.sh` for the latest Linux desktop player. The four five-container layouts and staged/full-truck intake options from iteration 2 remain. See `OPERATIONAL_ITERATION_2.md` for their move orders.

## Start and choose traffic

Choose a layout with **1–4**. Before starting, click **TRAFFIC** or press **T** to cycle:

| Traffic mode | Behaviour |
| --- | --- |
| Separated | Workers patrol marked side lanes. Rear-road trucks obey a separate pedestrian crossing. No scheduled crossing of the lifting aisle. |
| Crossing (default) | W1 requests the cargo aisle on the first ship-bound lift. Permission requires actual crane clearance. Red means the trolley is held at its marked line; wait for the worker and green signal. |
| Intrusion drill | An explicitly abnormal exercise: W1 enters the restricted lifting area once the first load is raised. A protected stop holds the load and requires acknowledgement, worker clearance and explicit resume. |

**P / Staged practice** begins with all five containers already arranged. **I / Truck intake** watches all five truck/reachstacker deliveries first. During intake the rear-road worker waits for the unloading route to clear before receiving permission. Traffic policy and layout stay fixed until reset/completion.

Four animated workers, two circulating rear-road freight trucks, a parked inspection trailer, barriers, stored freight and two moving neighbouring gantry cranes fill the wider terminal. Access roads and logistics warehouses connect the quay to a larger terminal hinterland. Side lanes separate routine pedestrians from lifting. The rear-road trucks wait behind stop lines while W3 crosses. Neighbour cranes are background hoist cycles; they do not add consignments to your five-container task.

## Controls and views

- **A/D:** gantry along the quay; **W/S:** trolley shipward/yardward; **Up/Down:** hoist; **Shift:** fine control; **Space:** lock/release.
- **Tab:** cycle views; **C:** cabin; **V:** wide port overview for inspecting the arena. Right mouse drag looks/orbits in supported external views and the cabin; scroll changes external-view zoom.
- **X:** protected stop. **E:** acknowledge, then resume once the area is clear. These also have large console buttons.
- **Esc:** software pause/resume. **R:** reset, preserving partial data. **M:** Maya voice toggle.
- VR input path: left stick X/Y gantry/trolley; right stick Y hoist; right A lock/release; right grip fine control; left Y protected stop, left X acknowledgement/resume. Console buttons can also be selected with a controller ray. Actual headset verification is still required.

Maya's captions name the current task and waiting reason. New offline speech covers intake, crossing, protected stop, worker clearance, resume and shipment completion. Warning sounds and machine motors follow operational state; an interlocked trolley should not sound as if it is moving.

Raise the loaded spreader to the indicated 13 m transit height before crossing stacks. Slow the hoist with Shift before contacting the deck or a stack. An actual fast landing can trigger an impact stop; target alignment alone does not make a hard landing gentle.

## Crossing and protected-stop recovery

A normal aisle crossing clamps trolley travel at the marked hold lines. The operator can raise the load while waiting. The crossing ends automatically when W1 clears; the task then continues. Crew movements are triggered by operations, not random encounters.

A strong structure strike, worker contact, dangerously close worker or intrusion can latch a **PROTECTED STOP**. The simulator freezes the crane and cargo at their current poses while preserving the attachment. Follow the console:

1. Release movement controls and inspect the warning.
2. Press **E / ACKNOWLEDGE**.
3. If a worker is involved, wait while that worker walks out. The crane, cargo and other traffic remain held. Early resume is ignored.
4. When **READY TO RESUME** appears, inspect the load/path and press **E / RESUME** again.
5. Back away gently from a structure and continue the assigned move. The cargo is not snapped to a safe target or counted as delivered.

Manual X uses the same acknowledgement/resume process. Reset is always available. A protected operational hold remains inside active elapsed time; Esc pause is excluded, including a pause during recovery.

## Contact feedback and records

A slow scrape produces a contact-local metal sound, small paint/dust flecks and a brief location halo. Strong contact also adds a cabin beacon, warning tone and protected hold. Controller pulses are requested where supported. Headset view motion is not shaken. People remain upright; there are no injury animations or damage predictions.

Contact episodes group multiple colliders on the same rig/obstacle, and can escalate from a scrape to a strong impact. Near-clearance episodes are distinct from physical contact. Gentle supported seating and vertical pickup/departure are filtered from routine warnings. Placement acceptance still separately checks the full footprint, orientation, real support and stability.

New sessions use **schema op3**:

| File | Contents |
| --- | --- |
| `runs.csv` | Build/layout/intake/input/traffic policy, practice and automated flags, active time and status |
| `moves.csv` | Same unique move and cargo identities as op2, plus crossing-wait and protected-stop subsets |
| `phase_times.csv` | Exclusive task phases; these sum to active elapsed time |
| `delays.csv` | Crossing/interlock and protected-stop time; these are subsets of phase/move time, not extra elapsed time |
| `incidents.csv` | Episode ID, move, source/target, category/severity, start/end/duration, peak relative speed/impulse, contact point, outcome and acknowledgement/resume timing |
| `events.csv` | Crossing requests/grants/clearance, waits, stops, acknowledgements/resume, and the existing task/custody events |
| `scenario.json` | Cargo plan, assistance, traffic mode, schedules and thresholds |

Use **Open operational CSV folder** after a run. Analyze copied sessions with:

```bash
python3 tools/analyze_operational_sessions.py /path/to/OperationalSessions --output /path/to/results
```

The analyzer keeps traffic policies, layouts, input modes, intake modes, versions and automated/practice work separate. Completed-time summaries exclude aborted/unfinished sessions. Delay subsets must not be added a second time. Acknowledgement latency is a simulator response measure; it includes reading/inspection and is not a calibrated pure reaction-time experiment. Contact speed/impulse are simulator proxies, not injury severity or real crane damage estimates.

All operational sessions remain practice data. Workload/comfort ratings, final participant protocol and actual headset performance are further work; time or contact counts alone do not measure physiological fatigue.

## Simulation limits

Traffic and workers use repeatable scripted routes and simple colliders. Truck visits re-enter at outer gates after leaving the arena. Protected stops freeze the simulator rather than modelling real mechanical braking, and accepted supports remain anchored until pickup. The intrusion is a labelled exercise, not the normal operating rule. No industrial training certification or free stack-collapse model is claimed. The preserved first/second operational scenes and v6 research scene remain available; use `tools/run_desktop_demo.sh --iteration1`, `--iteration2` or `--research` for their previous Linux players.

## Verification

The four layouts, controlled/separated/intrusion scenarios and full truck intake completed **38 physical moves** across seven runs. Separate real-collider tests pass scrape/strong impact/person contact, deduplication, effects and recovery. All five views, cabin/live feed and offline safety speech were rendered/checked. Seven analysis tests pass, and the completed verification exports produce zero timing-reconciliation warnings. See `VERIFICATION.md` for exact scope, logs and limits. All these samples are automated practice data.

The Linux release build and packaged keyboard/start/stop/acknowledge/resume/view/normal-close checks also pass. The app opens fullscreen at the machine's native resolution; Unity and the player are closed after verification. Copy the refreshed `Builds/PortLab_Unity_Project.zip` to the lab PC for Unity/Windows/VR testing.
