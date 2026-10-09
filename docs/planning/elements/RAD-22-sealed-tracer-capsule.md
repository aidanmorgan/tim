# RAD-22 · Sealed tracer capsule — element readiness spec

Story 7.0 Batch O named-identity spec. Legacy citations are pinned to `a6c914e` and resolve with `git show a6c914e:<path> | sed -n 'start,endp'`. Requirement row: [radiation-22](../requirements.md#radiation-22) (P2 potential). A **marked mobile sealed toy source** tracked as a discrete body — not dissolved radioactive fluid; game units only.

## 1. Identity

| Item | Value |
| --- | --- |
| ID / name | RAD-22 · Sealed tracer capsule |
| Type | Radiation + Gravity (dynamic ball-compatible photon source) |
| Anchor | [requirements.md#radiation-22](../requirements.md#radiation-22); [named-elements.md#radiation-22](../invest/named-elements.md#radiation-22) |
| Related identities | Receivers [RAD-08](RAD-08-radiation-rate-meter.md); ball transport [CAT-048 Pipe](CAT-048-pipe.md), [CAT-054 Ramp](CAT-054-ramp.md), [CAT-019 Conveyor](CAT-019-conveyor.md); branching via [EL-163 Powered ball diverter](../invest/named-elements.md#element-163); counting via [CAT-020 Counter](CAT-020-counter.md); ball scale from [CAT-064 Tennis](CAT-064-tennis.md). No CAT spec refined. |
| Proof owner | S633 |
| Roadmap story | unscheduled; campaign 135 (research slots 110, 125) |
| Status | not started |

## 2. Declaration

The requirement fixes: a marked mobile capsule travels through existing ball-compatible transport and is sensed through its enclosure; two detectors observe it in causal order and drive a gate/counter; a stationary capsule cannot repeatedly count as new arrivals; branches move one capsule without copying it. Values are proposals unless cited.

- **Bodies and shapes.** One dynamic sphere of radius 0.25 m (proposed: the Tennis-ball radius, so it fits every ball pipe, whose bore radius is 0.65 m, and every receiver; sphere admission 1/16–16 m, `engine/gpu/PhysicsDeclarations.cs@a6c914e:L89-L120`).
- **Mass and material.** 0.6 kg; restitution 0.3; friction 0.3; rolling resistance 0.03; linear drag 0.04 1/s (proposed: a sealed ceramic shell between the Tennis ball and the Bowling ball; drag 0.04 and friction 0.3 match the current ball materials, `engine/gpu/WorkshopConstruction.cs@a6c914e:L50-L51`). Declared through the ball material record, not a per-element law.
- **Constraints.** none.
- **Typed ports.** none.
- **Sensors and activation.** none on the capsule. The capsule's source follows its committed body pose; meters sample it at substep endpoints, so fast passes are not missed.
- **Source declaration.** `RadiationKind.Gamma`, `PhotonEnergyBand.Medium`, isotropic, A = 8 game units/s, centre-sampled under the [shared rate law](RAD-08-radiation-rate-meter.md#2-declaration) (proposed: half the gamma capsule, so a default meter switches on only when the tracer passes within ≈ 1.41 m and releases beyond ≈ 1.63 m; a pipe's thin wall barely attenuates it). Steady within a Run (RadioactiveDecay steady case).
- **Work and energy stores.** Ordinary kinetic/potential energy of a rigid ball; emitted-quanta ledger.
- **Parameters.** none.
- **Cosmetic curves and UI bindings.** Field overlay follows the body (research row); selected-only.
- **Art.** Cream `#fff8e9` sphere with a gold `#f7cb52` band and an engraved γ mark, navy `#293954` band seams (proposed: "visibly marked" by shape; reads as a ball but not as a plain Basketball).
- **Catalogue and inventory.** Id `sealed_tracer_capsule`, title "Tracer capsule", category `Radiation` (proposed: snake_case id; [grouped menu](../requirements.md#palette-type-groups)).

**Variants.** None in the row.

## 3. Engine capabilities

Families from the [map row](../general-engine-element-map.md) and [binding](../../coverage/engine/radiation-01.json):

| Family | Status | Basis or builder |
| --- | --- | --- |
| FiniteLedger | missing | unscheduled (S010-D); emitted-quanta ledger; the only current finite store is bumper contact work, `engine/gpu/ContactWorkDeclaration.cs@a6c914e:L5-L9` |
| FluidAdvection | missing — and not applicable | unscheduled (S418-D); the map's composition note says this inherited family does not apply to discrete sealed cargo (open question 3) |
| GeometryQuery | exists now for contact only; missing for radiation paths | BVH `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L74-L204` and narrowphase `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L325-L620` (the coverage JSON's `engine/gpu/physics.wgsl` basis is not in the tree); paths from a moving source: unscheduled (S606-D) |
| IonizingTransport | missing | unscheduled (S606-D/F); a moving source bound to a body pose |
| RadioactiveDecay | missing | unscheduled (S607-D/F); steady case |
| StateTransaction | exists now | `engine/gpu/WorkshopSimulation.cs@a6c914e:L44-L161`; capsule pose and velocity restore with Reset |
| TopologyTransaction | exists now for construction admission only | `engine/gpu/WorkshopSimulation.cs@a6c914e:L113-L151`; no in-Run topology change needed — branches move the one body |

The map composition adds RigidBodyDynamics, ContactImpulse and SlidingFriction for ball transport: all exist now (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L42-L120`, the `BallMaterial` record `engine/gpu/WorkshopConstruction.cs@a6c914e:L38-L77`, and `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L623-L726`). Rising-edge counting at meters needs RAD-08 contacts (Story 8.1) and CAT-020 (Story 9.2).

**Dependencies.** RAD-08 (two meters); CAT-048 pipe or other ball transport; a diverter (EL-163) and counter or gate; CAT-005 Battery.

## 4. Sources and legacy

- [radiation-22](../requirements.md#radiation-22): "Two detectors observe the capsule in causal order and drive a gate/counter; a stationary capsule cannot repeatedly count as new arrivals. Branches move one capsule without copying it."
- [Named entry](../invest/named-elements.md#radiation-22), owner S633; map composition note (discrete sealed cargo, not dissolved tracer); binding `radiation-01.json`.
- Research row ("meters along opaque transport sequence a diverter") and slot 110 "The Traveling Beacon".
- **Legacy.** No tracer part. Hits are coverage snapshots only, e.g. `reference/P0-022-before/docs/coverage/engine/capabilities-02.json@a6c914e:L15446-L15449`, plus the aggregate task lists — no element knowledge; do not carry forward.

**Files harvested** (all "no element knowledge"):
- `reference/P0-022-before/docs/coverage/engine/capabilities-02.json`
- `reference/P0-022-before/docs/coverage/engine/task-002.json`
- `reference/P0-022-before/docs/coverage/engine/task-003.json`
- `reference/P0-022-before/docs/coverage/engine/task-013.json`

## 5. Acceptance outline

- **Construction (actual Chrome UI).** Drop the capsule into a pipe run; place two supplied Rate meters beside the pipe 0.8 m away at points A then B; meter A drives a counter, meter B drives a powered diverter that sends the capsule to one branch.
- **Positive.** As the capsule passes, A reads ≈ 12 (above On), then falls below Off; then B switches on: counter = 1; diverter acts after A, in causal order; the single capsule exits one branch.
- **Negative / controls.** A capsule stuck beside meter A keeps the contact closed and counts once only. Wrong branch: meter on the other branch never fires. A Basketball through the same route triggers nothing. Only one capsule body exists after the branch.
- **Boundaries.** Fast pass (≈ 5 m/s) still registers at both meters; capsule stopping exactly at the Off distance.
- **Run/Reset.** Capsule pose and velocity restore; counter clears.
- **Save/Load.** Pose round-trips.
- **Integrations.** No row-level cross-element task names the tracer; it integrates through interaction processes [IX-15 Ionizing transport](../requirements.md#interaction-15), [IX-16 Radioactive decay](../requirements.md#interaction-16) (steady case), [IX-01 Contact impulse](../requirements.md#interaction-01) and [IX-02 Sliding friction](../requirements.md#interaction-02) (ball-compatible transport); family tasks [radiation-foundations](../requirements.md#radiation-foundations), [radiation-proof](../requirements.md#radiation-proof) and [radiation-campaign](../requirements.md#radiation-campaign); campaign first use in levels 131–135 ([campaign element coverage](../requirements.md#campaign-element-coverage); chapter 14 of the [campaign plan](../requirements.md#campaign-plan), tracer at 135), reuse 136–150.

## 6. Open questions

1. Proposed size, mass and activity need S606-D; whether the capsule should share Tennis-ball material exactly — owner decision.
2. Should arrival be sensed by rate meters (contact edges) or a dedicated passage sensor that reads radiation — owner decision.
3. Confirm the coverage binding drops FluidAdvection and adds RigidBodyDynamics, ContactImpulse and SlidingFriction, as the map composition note says — owner of the map.
