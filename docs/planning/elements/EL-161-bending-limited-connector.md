# EL-161 · Bending-limited connector — named-identity readiness spec

Story 7.0 readiness spec ([spec](../../../_bmad-output/implementation-artifacts/spec-7-0-element-implementation-readiness.md)). Citations are `path@a6c914e:Lstart-Lend`. Values marked **proposed** have no legacy or requirement source; each carries a one-line justification and the owner may revise it.

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name / type | EL-161 · Bending-limited connector · Construction |
| Anchor | [requirements.md#element-161](../requirements.md#element-161); [named-elements entry](../invest/named-elements.md#element-161); umbrella [gap-05](../requirements.md#gap-05) (index only) |
| Related identities | Siblings [EL-159 Tension-limited](EL-159-tension-limited-connector.md) and [EL-160 Shear-limited](EL-160-shear-limited-connector.md); the rigid (weld) joint it needs is the open question of [EL-112](EL-112-structural-beam.md). No CAT refines it. |
| Roadmap story | Unscheduled. Campaign: introduction 43 "The Last Straw", practice 44, reuse 65, 96, 139 ([gap-05](../requirements.md#gap-05)). |
| Status | Not started (no joint or fracture capability, `engine/gpu/PhysicsDeclarations.cs@a6c914e:L163-L192`). |

## 2. Declaration

The requirements row names no variants; this identity is the bending-moment failure mode of the gap-05 family. Collar, ports, break transaction and stress cue are shared with [EL-159](EL-159-tension-limited-connector.md). Unlike its siblings it must transmit moment, so its joint is rigid.

- **Bodies and shapes.** No body; collar art (**proposed** 0.3 m × radius 0.12 m, as EL-159).
- **Mass and material.** None.
- **Constraints.** One rigid (weld) joint: three translational and three rotational rows holding the two sockets' frames together. The legacy kinds (BallSocket, Hinge, Slider) had no weld (`engine/physics/JointEquations.cs@a6c914e:L5-L5`); a locked hinge (zero-width range, `CuriousContraptions.tests/JointRangeTests.cs@a6c914e:L89-L102`) plus its two bilateral angular rows is equivalent. Connected-body collision Disabled (**proposed**, as EL-114).
- **Typed ports.** `ConnectorA`, `ConnectorB`, domain `Structural` (**proposed** in [EL-112](EL-112-structural-beam.md)).
- **Sensors and activation.**
  - Bending moment M = magnitude of the joint's reaction torque about the two axes perpendicular to the connector axis; torsion about the axis is excluded.
  - Tick-averaged as EL-159 (**proposed** four-substep average over the 120 Hz tick).
  - Breaks at the first tick with M > `moment_capacity`. Display animation never drives it: "Subthreshold moment retains the joint regardless of display animation" ([element-161](../requirements.md#element-161)) — the stress cue reads the committed moment, it does not feed back.
- **Work and energy stores.** None.
- **Break transaction.** Atomic joint removal at the tick boundary; no new bodies (as EL-159).
- **Parameters.**

  | Name | Type | Range | Default | Unit | Source |
  | --- | --- | --- | --- | --- | --- |
  | `moment_capacity` | f32 | 2–100 | 45 | N·m | **proposed** — the 2 m cantilever beam (1.5 kg at the proposed EL-112 density) contributes 1.5 × 9.81 × 1.0 = 14.7 N·m; adding a 4 kg Bowling ball resting 0.5 m out (19.6 N·m) gives 34.3 N·m, which holds with 10.7 N·m margin; the ball at 1 m (39.2 N·m) gives 53.9 N·m, which breaks with 8.9 N·m excess — a clean two-position lesson |

  **Proposed** stepped presets 15 / 45 / 100 N·m — same three-step selector shape as EL-159; 15 N·m is just above an unloaded 2 m cantilever (14.7 N·m), and 100 N·m (the range maximum) holds the Bowling ball anywhere on a 2 m beam, including the tip (4 × 9.81 × 2.0 + 14.7 = 93.2 N·m).
- **Cosmetic curves and UI bindings.** Selected-only witness marks arranged as an arc on the collar's top face ← committed M / capacity; a break opens a real visible gap ([gap-05 visual style](../requirements.md#gap-05)).
- **Art.** Cream collar `#fff8e9`, gold load band `#f7cb52`, navy arc witness marks `#293954` (`DESIGN.md@a6c914e:L147-L154`).
- **Catalogue and inventory.** **Proposed** id `bending_connector`, title "Bending connector", category Structure; appended last in the Free palette (`engine/gpu/WorkshopInventory.cs@a6c914e:L47-L60`).

## 3. Engine capabilities

Families ([element map row](../general-engine-element-map.md), [binding](../../coverage/engine/element-02.json)): EnvironmentState, FiniteLedger, GeometryQuery, JointConstraint, RigidBodyDynamics, StateTransaction, StructuralFracture, TopologyTransaction.

- **Exists now.** Atomic Run/Reset of the physics document (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L163-L192`).
- **Missing.**
  - A rigid weld joint with angular rows: no story builds it; Story 10.3's hinge with a zero-width range is the nearest ([epics](../../../_bmad-output/planning-artifacts/epics.md)). Owner S682.
  - Reaction torque read-back and the fracture topology change: S543 fracture row, S563 ([decisions](../invest/decisions.md#s543)).
- **Dependencies.** A fixed support (Wall or Pivot plinth) and a member (EL-112).

## 4. Sources and legacy

- **Requirements.** Row: "Declared moment capacity governs failure under bending"; outcome "Subthreshold moment retains the joint regardless of display animation" ([element-161](../requirements.md#element-161)). Integration as [gap-05](../requirements.md#gap-05).
- **Audit.** "explicit finite breaking threshold and readable stress cue" (`docs/physics-puzzle-gap-audit.md@a6c914e:L84-L84`).

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| 1 | No weld kind among legacy joints. | `engine/physics/JointEquations.cs@a6c914e:L5-L5` | Carry forward as a recorded gap. |
| 2 | A zero-width travel range locks the remaining freedom. | `CuriousContraptions.tests/JointRangeTests.cs@a6c914e:L89-L102` | Carry forward as one route to a weld. |
| 3 | Balanced loads cancel and an off-centre load produces rotation (moment) about a hinge. | `CuriousContraptions.tests/SharedHingeMechanicsTests.cs@a6c914e:L47-L59` | Carry forward: moment arm determines load, so load position must matter in acceptance. |

Files consulted: `engine/physics/JointEquations.cs`, `CuriousContraptions.tests/JointRangeTests.cs`, `CuriousContraptions.tests/SharedHingeMechanicsTests.cs`.

## 5. Acceptance outline

Point of truth: [element-161](../requirements.md#element-161) and the [gap-05 integration](../requirements.md#gap-05).

- **Chrome recipe.** Attach a 2 m Structural beam horizontally to a Wall through a Bending connector (45 N·m), forming a cantilever; place a Bowling ball resting on the beam with the move gizmo (placed, not dropped).
- **Positive.** Ball resting 1 m from the connector: total moment 14.7 + 39.2 = 53.9 N·m > 45; the connector breaks, the beam swings down and falls.
- **Negative or control.** Ball resting 0.5 m from the connector: 14.7 + 19.6 = 34.3 N·m < 45; the cantilever holds for the whole Run. Selecting the connector (stress cue visible) does not change the outcome.
- **Boundaries.** Capacity 2 and 100 N·m admitted, outside rejected; pure torsion about the beam axis does not break it.
- **Run/Reset.** Reset restores the exact pre-break graph.
- **Save/Load.** Capacity preset and attachments survive save and Load.
- **Integrations.** Load-limited connector integration task [sequence-task-538](../requirements.md#sequence-task-538) (gap-05: the same supported assembly survives below threshold and breaks above it; Reset restores the pre-break graph); interaction rows IX-03 joint constraint ([interaction-03](../requirements.md#interaction-03)) and IX-37 structural fracture ([interaction-37](../requirements.md#interaction-37)). Campaign first use: GAP-05 row of the [campaign allocation](../requirements.md#campaign-gap-allocation) — introduction 43, practice 44, reuse 65, 96, 139.

## 6. Open questions

1. Whether the rigid weld is a new joint kind or a locked hinge pair. Unspecified — owner decision.
2. Whether torsion gets its own limit or is never a failure mode.
3. Moment presets (proposed 15/45/100 N·m).
