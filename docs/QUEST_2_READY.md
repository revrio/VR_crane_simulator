# Current port scenario

Choose one of four layouts, then Start operation. Five containers are staged in the yard. Background trucks, workers and neighbouring cranes follow fixed routes; no incoming-truck mode or traffic customization is offered. Impacts keep their sound/particle feedback and contact records, but never freeze the crane/cargo or require acknowledgement. Pause and Reset are still manual controls.

## Controls

Desktop: A/D gantry, W/S trolley, arrows hoist, Space lock/release, Shift precision. P starts; 1–4 select layouts. Tab cycles the cabin monitor feed; C selects the cabin. The operator remains seated when switching monitor feeds.

Simulated VR: select Tools > Operational Port > Input Mode > Simulated VR Controllers, enter Play, and use the controller ray and trigger to select a layout and Start operation on the cabin console. Hold Left Shift + WASD for the left stick; Space + W/S for the right stick; Space + B for right A; Space + G for right grip. F8 switches controller positioning; leave it in crane mode for operation. The top-left overlay contains control instructions only. No headset is needed.

Quest 2: left thumbstick X gantry, Y trolley; right thumbstick Y hoist; right A lock/release; right grip precision. Locking always requires a button press (Right A, desktop Space, or simulator Space + B). Contact with the requested, unblocked container qualifies for locking regardless of calculated centre/roof gap; otherwise the tolerance is 1.2 m horizontally and 0.9 m vertically. Contact alone never locks. The console shows directional distances in metres. Use controller ray + trigger for layouts, Start, Pause, Reset and Monitor. Monitor cycles overhead, load follow, destination and port overview feeds without moving the operator. Recenter seated view aligns the operator before starting.

## Before the lab

Stop Play Mode and run Tools > Operational Port > Verify Quest 2 Settings and XR Inputs. The check validates ARM64/IL2CPP/OpenXR/Oculus Touch configuration and injects simulated left/right controller states into the imported crane input asset. It checks sticks, right A, grip and release-to-neutral. Results are written to `/tmp/portlab-quest-preflight.txt`. Android builds also run the settings portion before packaging.

Crane actions live in Assets/Resources/PortCraneControls.inputactions and are referenced by the scene so OpenXR can discover bindings at startup. The Android target SDK is pinned to 34. Quest Pro targeting is disabled because OpenXR 1.14's manifest builder otherwise adds required eye tracking, which Quest 2 does not have. The actual headset camera uses XR; spectator and load-feed cameras do not.

Build the APK before visiting the lab. A headset connection is not required for building. Keep this exact APK for installation and record its file hash if comparing results. In the lab, install it and test head tracking, both controller rays/trigger, layout selection, Start, both sticks, right grip, right A attachment/release, Pause/Resume and Reset. For attachment, align with the requested container and match roof height first; incorrect alignment intentionally refuses locking and displays the gap.

Compilation and simulation cannot establish that a particular headset's OpenXR session, firmware, focus, permissions and tracking behave correctly. The first device smoke test is the remaining verification boundary; a settings/input preflight pass must not be described as a headset pass.

The older operational iteration documents describe historical scenarios and stop drills. They do not describe the current UI or behavior.

## Maya, feedback and scenery update

Maya's speech is loaded from `Assets/Resources/PortVoice`. Common guidance avoids key names; coupling, release, lowering/precision and pause cues have Desktop, Simulator and Quest variants. Quest uses right A/right grip/right stick, Simulator uses Space+B/Space+G/Space+S, and Desktop uses Space/Left Shift/Down Arrow. Speech and captions both select the current mode. The recordings use the same offline LJ Speech/Piper voice as the original Maya clips. Reproduce with `tools/generate_port_mode_voice.py MODEL.onnx` in a Piper environment; no model or synthesizer runs inside the game.

After final parking completes a layout, the existing cabin/desktop console shows seven 0–10 sliders: head turning effort, neck movement/posture effort, shoulder/arm reaching effort, neck discomfort, shoulder discomfort, hand/wrist discomfort and eye strain. Mouse dragging and XR ray dragging use the existing canvas; +/− and explicit 0/None buttons offer alternatives. Every item starts unanswered. Save requires explicit ratings; Skip is available. Draft responses are checkpointed on each adjustment. The file `layout_feedback.csv` is saved alongside the same run's `runs.csv`, with run/layout/input-mode identifiers and a draft/submitted/skipped status. These are exploratory post-task self-reports, not measured joint angles or a validated clinical WSD assessment. Existing task timing is stopped before the survey.

Scenery is built as a render-only child with combined meshes: reshaped feeder hull and deck details, two background cargo ships, outer-yard freight stacks, light masts, fencing, road paint and stored equipment. No new colliders, rigidbodies, PortCargo objects, task slots or handling paths are added. The original gameplay deck/hull colliders remain intact. Actual Quest performance still needs checking.

The editor menu **Tools > Operational Port > Validate Scenery and Feedback** runs the added visual/export checks and captures overview, cabin and survey previews. It temporarily uses Desktop mode, writes a synthetic run only under `/tmp/portlab-feedback-validation`, and restores the previous input mode.

## Handling feedback

The cyan pickup outline follows the current cargo in every layout. The old static yard outline is hidden, and the home/parking markers appear only in the final parking stage. Receiving outlines remain at their destination slots.

Placement allows 0.65 m overhang beyond the marked footprint, 0.4 m height error, up to 12 degrees tilt and 15 degrees yaw, and moderate settling velocity. An upper-tier support may be offset up to 1.2 m from its planned position, but the container still needs actual physical support. Unsupported or wrong-slot cargo is not accepted.

XR hoisting has a gentler response curve and ramps into motion. In laptop simulation, releasing either the direction keys or the controller modifier forces that controller's crane command to neutral, avoiding XRI's retained-axis state. Physical load sway can continue briefly after stopping the winch.

Maya selects alignment, lowering, transfer and landing speech from task state. Each accepted placement interrupts obsolete guidance with a short Delivery accepted clip, before continuing with the next task's instructions.
