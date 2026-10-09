# RAD-06 · Powered radiation shutter — element readiness spec

Story 7.0 Batch O named-identity spec. Legacy citations are pinned to `a6c914e` and resolve with `git show a6c914e:<path> | sed -n 'start,endp'`. Requirement row: [radiation-06](../requirements.md#radiation-06) (P1 potential). Toy shielding in game units; never real-world shielding guidance.

## 1. Identity

| Item | Value |
| --- | --- |
| ID / name | RAD-06 · Powered radiation shutter |
| Type | Radiation (supplied actuator + moving shield) |
| Anchor | [requirements.md#radiation-06](../requirements.md#radiation-06); [named-elements.md#radiation-06](../invest/named-elements.md#radiation-06) |
| Related identities | Blade material from [EL-128 Dense shield](EL-128-dense-shield.md); sources [RAD-01](RAD-01-gamma-source-capsule.md), [RAD-02](RAD-02-powered-x-ray-emitter.md); [RAD-08](RAD-08-radiation-rate-meter.md), [RAD-09](RAD-09-integrating-dosimeter.md). Mechanical analogues [CAT-007 beam shutter](CAT-007-beam_shutter.md) (optical, not refined here) and [CAT-039 linear pusher](CAT-039-linear_pusher.md) (finite-work actuator). No CAT spec refined. |
| Proof owner | S613 |
| Roadmap story | unscheduled; campaign 105, 110 (research slots 80, 85) |
| Status | not started |

## 2. Declaration

The requirement fixes: a supplied actuator moves a dense blade through the field; radiation follows actual blade position and thickness, including partial travel and jams; power loss cannot teleport the blade closed. Values are proposals.

- **Bodies and shapes.** Slide coordinate s runs along local X with the window centre at s = 0.
  - Frame: static body, box colliders forming a housing 1.40 (s from −1.05 to +0.35) × 0.80 × 0.20 m with a 0.50 × 0.50 m open window at s ∈ [−0.25, +0.25] and a pocket for the retracted blade on the −s side (proposed: Battery-scale height; the window is the controllable exposure interval).
  - Blade: dynamic box 0.60 × 0.60 × 0.10 m (proposed: covers the window with 0.05 m overlap at each edge; Thick-dense thickness so a closed blade drops a Gamma capsule at 1 m below Off).
- **Mass and material.** Blade 4.5 kg (proposed: the EL-128 toy density applied to the blade volume); `RadiationMaterialKind.Dense` (μ 64/20/10 per metre, stopping factor 2000; see EL-128). Frame static, zero mass, static default contact material (`engine/gpu/WorkshopPhysicsCompiler.cs@a6c914e:L109-L110`); blade contact restitution 0.1, friction 0.4 (proposed: a sliding metal-like blade).
- **Constraints.** Prismatic slider joint, blade to frame, along s; travel p from 0 (Open: blade spans s ∈ [−1.00, −0.40]) to 0.70 m (Closed: blade spans [−0.30, +0.30]); leading edge e = −0.40 + p (proposed: travel equals blade width plus overlap).
- **Typed ports.** `PowerIn` (Electrical, Input; existing socket, `engine/gpu/WorkshopConnections.cs@a6c914e:L8-L12`) and `CommandIn` (Electrical signal, Input; new socket, proposed: true = close, false or unconnected = open; signal never powers the motor).
- **Sensors and activation.** Committed blade position is the only state; no endpoint teleport. Actuator: bounded force 40 N and speed limit per parameter, active only while `PowerIn` is available (proposed: 40 N moves 4.5 kg briskly but stalls against a fixed obstruction instead of passing through it). Unpowered: zero actuator force; the blade stays where friction holds it.
- **Work and energy stores.** Actuator work debited from the electrical supply once finite electrical energy exists (FiniteWorkActuation; no store in the part).
- **Parameters.** Travel speed f32, 0.1–1.0 m/s, default 0.5 m/s (proposed: a 1.4 s close is visible and teaches latency, lesson 110 "Close at the Mark").
- **Cosmetic curves and UI bindings.** Blade pose ← committed slider pose (research row); a position scale on the frame.
- **Art.** Cream `#fff8e9` frame, navy `#45639c` blade with three grooves (dense cue), gold `#f7cb52` actuator rod, navy `#293954` base (proposed: matches EL-128's shape cue).
- **Catalogue and inventory.** Id `radiation_shutter`, title "Radiation shutter", category `Radiation` (proposed: snake_case id; [grouped menu](../requirements.md#palette-type-groups)).

**Variants.** None in the row; partial travel and jams are boundary states, each separately proven.

## 3. Engine capabilities

Families from the [map row](../general-engine-element-map.md) and [binding](../../coverage/engine/radiation-01.json):

| Family | Status | Basis or builder |
| --- | --- | --- |
| ElectricalPower | missing | Story 8.1; the `Electrical` domain enum exists, `engine/gpu/WorkshopConnections.cs@a6c914e:L8-L12` |
| EnvironmentState | exists now | gravity lane of `RigidBodyDeclaration`, `engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L86` |
| FiniteLedger | missing | unscheduled (S010-D); the only current finite store is bumper contact work, `engine/gpu/ContactWorkDeclaration.cs@a6c914e:L5-L9` |
| FiniteWorkActuation | missing | Story 11.3 (bounded slider drive, reused by 8.2 and 13.3) |
| GeometryQuery | exists now for contact only; missing for radiation paths | BVH `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L74-L204` and narrowphase `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L325-L620` (the coverage JSON's `engine/gpu/physics.wgsl` basis is not in the tree); moving-blade path sampling: unscheduled (S606-D) |
| IonizingTransport | missing | unscheduled (S606-D/F); samples the blade at substep endpoints |
| JointConstraint | missing | Story 6.4 (prismatic slider with limits) |
| RigidBodyDynamics | exists now | bodies `engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L86`; integration `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L216-L285` |
| StateTransaction | exists now | `engine/gpu/WorkshopSimulation.cs@a6c914e:L44-L161`; blade pose restores with Reset |

The `CommandIn` signal needs SignalPropagation (missing; Story 8.1).

**Dependencies.** CAT-005 Battery; a command source (switch, clock or RAD-09 contact); a source and RAD-08/RAD-09.

## 4. Sources and legacy

- [radiation-06](../requirements.md#radiation-06): "Closed blade reduces exposure below the taught target threshold; an obstructed blade leaks according to geometry. Power loss cannot teleport the blade closed."
- [Named entry](../invest/named-elements.md#radiation-06), owner S613; map row (S605, S257); binding `radiation-01.json`.
- [S605](../invest/decisions.md#s605) photon row; research row (blade pose ← committed slider; "a jammed blade stays open"; slots 80 "Closing Time", 85).
- **Legacy.** No radiation shutter part, test or level. Hits are coverage snapshots only: older binding copy `reference/P0-022-before/docs/coverage/engine/task-017.json@a6c914e:L1804-L1838`, identical to current, and aggregate relation lists — no element knowledge; do not carry forward.

**Files harvested** (all "no element knowledge"):
- `reference/P0-022-before/docs/coverage/engine/task-017.json`
- `reference/P0-022-before/docs/coverage/engine/task-002.json`
- `reference/P0-022-before/docs/coverage/engine/task-003.json`
- `reference/P0-022-before/docs/coverage/engine/task-013.json`
- `reference/P0-022-before/docs/coverage/engine/capabilities-02.json`

## 5. Acceptance outline

Layout: Gamma capsule; shutter window centred on the axis 0.50 m in front of it; photon Rate meter face 1.00 m from the capsule. Under the [shared rate law](RAD-08-radiation-rate-meter.md#2-declaration) the 9 receiver-sample paths cross the window plane at s ∈ {−0.05, 0, +0.05} (three rows of three); a row is covered when the blade's leading edge e ≥ its s.

| Blade state | Covered rows | Mean T (Medium, Thick dense 0.135) | Reading |
| --- | --- | --- | --- |
| Open (p = 0, e = −0.40) | 0 of 3 | 1.0 | ≈ 15.8, on |
| Partial (p = 0.35, e = −0.05) | 1 of 3 | (3 × 0.135 + 6) / 9 = 0.712 | ≈ 11.2, on |
| Jammed (p = 0.425, e = +0.025) | 2 of 3 | (6 × 0.135 + 3) / 9 = 0.424 | ≈ 6.7, on |
| Closed (p = 0.70) | 3 of 3 | 0.135 | ≈ 2.1, below Off 3 |

- **Construction (actual Chrome UI).** Build the layout through palette and gizmo; Battery → `PowerIn`; a switch/clock signal → `CommandIn`; meter `Supply` → Powered gate.
- **Positive.** Command close: blade travels 1.4 s and the reading falls to ≈ 2.1, below Off, when fully closed.
- **Negative / controls.** Jam: a Wall block in the slot stops the blade at e = +0.025 m; the reading stays ≈ 6.7, above the target. The leak follows the sampled paths through the uncovered strip (1 of 3 rows), not the uncovered area fraction (0.45). Close command with supply removed: the blade does not move. Power cut mid-travel: blade stops at its partial pose.
- **Boundaries.** Leading edge exactly on a sample row (counted covered); speed at both range ends.
- **Run/Reset.** Blade returns to its authored position (Open by default) and zero velocity.
- **Save/Load.** Speed, pose and wires round-trip.
- **Integrations.** No row-level cross-element task names the shutter; it integrates through interaction processes [IX-15 Ionizing transport](../requirements.md#interaction-15), [IX-03 Joint constraint](../requirements.md#interaction-03), [IX-06 Electrical power transfer](../requirements.md#interaction-06) and [IX-07 Signal propagation](../requirements.md#interaction-07); supplied-port permutations in [connection permutations](../requirements.md#connection-permutations); family tasks [radiation-foundations](../requirements.md#radiation-foundations), [radiation-proof](../requirements.md#radiation-proof) and [radiation-campaign](../requirements.md#radiation-campaign); campaign first use in levels 101–110 ([campaign element coverage](../requirements.md#campaign-element-coverage); chapter 11 of the [campaign plan](../requirements.md#campaign-plan)), reuse 111–120, 124–130 and 136–150, including the dosimeter-feedback lesson with RAD-09.

## 6. Open questions

1. Does the shutter default to Open or Closed when unpowered at Run start — owner decision.
2. Should the blade be spring-returned (fail-safe) instead of friction-held — owner decision; the requirement only forbids teleporting.
3. Actuator force and speed need the FiniteWorkActuation decision (S149-D); the sampled leak law (3 × 3 receiver samples) is owner question S606-D.
