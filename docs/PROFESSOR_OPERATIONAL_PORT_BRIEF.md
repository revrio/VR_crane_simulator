# Proposal: operational VR container-terminal simulator

**Work System Design & Ergonomics lab · 4 November 2026**  
**Proposed upgrade; current working baseline is v6.**

**Implementation update:** iteration 1 now provides one complete truck–reachstacker–yard–player-crane–ship chain in a separate scene, with physics handling, guidance and practice logging. See `OPERATIONAL_ITERATION_1.md` for the working slice. The five-container and moving-worker features below remain the full proposal.

**Purpose:** investigate how container staging, stacking order and traffic organisation change shipment time, crane movement, waiting, rehandling, errors and operator workload.

The present prototype proves crane control, physical cargo handling, three layouts, in-app instruction and data recording. The next version will connect these mechanics into a complete terminal process.

## The proposed demonstration

**Truck arrival → machine-assisted unloading → yard stacking → crane retrieval/rehandling → ship loading at different heights → performance report.**

Five truck visits each bring one numbered container. A reachstacker visibly removes the load and places it in the configured buffer. The student then loads the vessel in manifest order. A buried container requires moving its blocker to a temporary slot. Ship destinations include lower and upper tiers, requiring controlled raising, clearance, alignment and lowering.

A gantry crane spanning the transfer area and vessel provides a coherent operating geometry. The original slewing-crane scene remains available as a baseline. Cargo has one continuous identity through all handoffs; it does not disappear or teleport between actors.

## What makes the port operational

| Addition | Purpose |
|---|---|
| Truck and reachstacker | Visible arrival, queue, parking, unloading and handoff |
| Banksman and walking workers | Signals, controlled crossings, clearance decisions and reaction-time events |
| Obstacles and restricted areas | Require path/height planning, stopping and recovery |
| Neighbouring working crane | Recognisable parallel terminal activity |
| Configurable four/five active containers | Practice and complete shipments, rather than fixed three-job scripts |
| Real stacking and rehandling | Place on another container, unload top-down, retrieve blocked cargo and reuse temporary positions |
| Collision feedback | Local sound/effect, warning and haptic feedback; protected stop for person contact; separate near-miss records |
| Maya and spatial audio | Context instructions, machine sounds, clearance calls and recovery guidance |

The normal environment uses organised vehicle and pedestrian routes. Hazard intrusions are explicit training events. Workers are not simply spawned beneath suspended loads to increase difficulty. General layout principles are informed by [HSE's ports transport guidance](https://www.hse.gov.uk/ports/transport.htm); equipment roles are informed by [Liebherr's container-stacking overview](https://www.liebherr.com/en-ca/maritime-cranes/products/port-equipment/areas-of-application-in-ports/stacking-4674146).

## Layouts and operating decisions

- **A — accessible staging:** five ground-level containers and direct vessel loading.
- **B — compact stacking:** three-high and two-high stacks, with compatible top-down retrieval.
- **C — priority retrieval:** a required lower container is blocked; temporarily relocate the blocker before completing the shipment.

Free-stacking practice and a longer receive-and-stack mode reuse these systems. In the latter, the student performs both truck-to-yard and yard-to-ship handling. The new workflow returns the empty spreader toward the next pickup and parks only after the shipment; repeated fixed-HOME returns remain in the original baseline experiment.

## Relevance to the course

The simulator supports direct time study of acquisition, hoisting, transfer, placement and repositioning; process analysis of waiting and rehandling; work sampling; and explicit normal/standard-time calculations. Additional controlled exercises can measure warning-response time, choice complexity and control/target difficulty.

For the main experiment, compare layouts with the same cargo count, crane settings, ship destinations, guidance and traffic policy. Balance order and use participant-level comparisons. Report shipment time, movement, rehandles, delays and errors together. Collect before/after discomfort or workload ratings with exposure duration; identify self-report measures clearly.

## Development and evidence

Build in this order: reachable terminal geometry → one complete truck-to-ship chain → five-container stacking/retrieval → people/traffic/incidents → guidance/data → desktop and actual-headset validation.

**Planning estimate:** approximately 22–39 focused development hours for the core, subject to usable licensed assets and lab access. A one-day milestone would demonstrate a smaller working slice, not the whole proposal.

Acceptance requires complete shipments without lost cargo, valid elevated placements, repeatable actor events, recoverable collisions, correct resets, reconciled timing and tested headset/controller operation. Deliver the scene, source project, scenario definitions, process chart, raw logs and experiment protocol. This is an educational operational simulator; industrial certification and full-scale port optimisation are outside its scope.

Full implementation and validation detail: [Operational port upgrade plan](OPERATIONAL_PORT_UPGRADE_PLAN.md).
