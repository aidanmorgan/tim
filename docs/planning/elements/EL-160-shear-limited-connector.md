# EL-160 · Shear-limited connector — named-identity readiness spec

Story 7.0 readiness spec ([spec](../../../_bmad-output/implementation-artifacts/spec-7-0-element-implementation-readiness.md)). Citations are `path@a6c914e:Lstart-Lend`. Values marked **proposed** have no legacy or requirement source; each carries a one-line justification and the owner may revise it.

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name / type | EL-160 · Shear-limited connector · Construction |
| Anchor | [requirements.md#element-160](../requirements.md#element-160); [named-elements entry](../invest/named-elements.md#element-160); umbrella [gap-05](../requirements.md#gap-05) (index only) |
| Related identities | Siblings [EL-159 Tension-limited](EL-159-tension-limited-connector.md) and [EL-161 Bending-limited](EL-161-bending-limited-connector.md); joins [EL-112](EL-112-structural-beam.md) members. No CAT refines it. |
| Roadmap story | Unscheduled. Campaign: introduction 43 "The Last Straw", practice 44, reuse 65, 96, 139 ([gap-05](../requirements.md#gap-05)). |
| Status | Not started (no joint or fracture capability, `engine/gpu/PhysicsDeclarations.cs@a6c914e:L163-L192`). |

## 2. Declaration

The requirements row names no variants; this identity is the transverse-shear failure mode of the gap-05 family. Joint, collar, ports, break transaction and stress cue are shared with [EL-159](EL-159-tension-limited-connector.md); only the measured load and its threshold differ.

- **Bodies and shapes.** No body; joint plus collar art (**proposed** 0.3 m × radius 0.12 m, as EL-159 — one collar kit for the family).
- **Mass and material.** None.
- **Constraints.** One pin joint at the coincident sockets (**proposed** pin, as EL-159 — shear needs no moment capacity), connected-body collision Disabled (**proposed**, as EL-114).
- **Typed ports.** `ConnectorA`, `ConnectorB`, domain `Structural` (**proposed** in [EL-112](EL-112-structural-beam.md)).
- **Sensors and activation.**
  - Shear V = magnitude of the joint reaction force component perpendicular to the connector axis. The axial component (tension or compression) is excluded: "Pure supported axial load does not silently count as shear" ([element-160](../requirements.md#element-160)).
  - Tick-averaged reaction as EL-159 (**proposed** four-substep average over the 120 Hz tick).
  - Breaks at the first tick with V > `shear_strength`.
- **Work and energy stores.** None.
- **Break transaction.** Atomic joint removal at the tick boundary; no new bodies (as EL-159).
- **Parameters.**

  | Name | Type | Range | Default | Unit | Source |
  | --- | --- | --- | --- | --- | --- |
  | `shear_strength` | f32 | 5–200 | 60 | N | **proposed** — a 4 kg Bowling ball resting right beside the connector on a 2 m horizontal beam (1.5 kg) puts at most 39.2 N (ball) + 7.4 N (half the beam) = 46.6 N of shear on it, which holds with 13.4 N margin; a dropped ball adds an impact load far above 60 N (section 5) |

  **Proposed** stepped presets 20 / 60 / 120 N — same three-step selector as EL-159.
- **Cosmetic curves and UI bindings.** Selected-only witness marks ← committed V / strength; segmented marks run across the collar (transverse) to distinguish shear from EL-159's along-axis marks by shape ([gap-05 visual style](../requirements.md#gap-05)).
- **Art.** Cream collar `#fff8e9`, gold load band `#f7cb52`, navy transverse witness marks `#293954` (`DESIGN.md@a6c914e:L147-L154`).
- **Catalogue and inventory.** **Proposed** id `shear_connector`, title "Shear connector", category Structure; appended last in the Free palette (`engine/gpu/WorkshopInventory.cs@a6c914e:L47-L60`).

## 3. Engine capabilities

Families ([element map row](../general-engine-element-map.md), [binding](../../coverage/engine/element-02.json)): EnvironmentState, FiniteLedger, GeometryQuery, JointConstraint, RigidBodyDynamics, StateTransaction, StructuralFracture, TopologyTransaction.

- **Exists now.** Atomic Run/Reset of the physics document (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L163-L192`).
- **Missing.** Joint rows (Story 10.3, [epics](../../../_bmad-output/planning-artifacts/epics.md)); per-joint reaction decomposed into axial and transverse components; fracture topology change: S543 fracture row, S563 ([decisions](../invest/decisions.md#s543)); owner S681. The axial control also needs the beam's rope socket `TieB` (EL-112, Story 10.2).
- **Dependencies.** Two members (EL-112), a support (EL-114 Pivot or a Wall), a load (Bowling ball; Weight EL-204 for the axial control).

## 4. Sources and legacy

- **Requirements.** Row: "Declared transverse strength governs failure under shear load"; outcome "Pure supported axial load does not silently count as shear" ([element-160](../requirements.md#element-160)). Integration as [gap-05](../requirements.md#gap-05).
- **Audit.** "measurable load, explicit finite breaking threshold" (`docs/physics-puzzle-gap-audit.md@a6c914e:L84-L84`).
- **Legacy.** No strength-limited joint existed. Relevant facts:

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| 1 | Radial and axial impulses through a hinge are carried by its bilateral rows (they cannot rotate it), so the reaction contains separable axial and transverse parts. | `CuriousContraptions.tests/SharedHingeMechanicsTests.cs@a6c914e:L61-L70` | Carry forward: the reaction is the measured quantity. |
| 2 | Explicit Detach as a topology edit. | `engine/physics/PhysicsJointChange.cs@a6c914e:L5-L28` | Carry forward as the break. |

Files consulted: `CuriousContraptions.tests/SharedHingeMechanicsTests.cs`, `engine/physics/PhysicsJointChange.cs`.

## 5. Acceptance outline

Point of truth: [element-160](../requirements.md#element-160) and the [gap-05 integration](../requirements.md#gap-05).

- **Chrome recipe.** A horizontal 2 m Structural beam (1.5 kg) joined at one end by a Shear connector (60 N) to a fixed upright support, the far end resting on a Wall; place or drop loads near the connector.
- **Positive.** A Bowling ball dropped from 1 m (4.43 m/s) beside the connector: the beam arrests 4 × 4.43 = 17.7 N·s; even spread over 0.1 s that is 177 N of shear > 60, so the connector breaks; the beam end drops and the load path changes.
- **Negative or control.** The same Bowling ball placed resting beside the connector: V ≤ 46.6 N < 60, holds. Placed at mid-span: V ≈ 19.6 + 7.4 = 27.0 N, holds.
- **Axial control.** Rig of [EL-159](EL-159-tension-limited-connector.md) with this Shear connector: a 4 kg Weight snatched through 0.5 m of slack loads the connector with ≥ 179 N of pure axial tension and ≈ 0 shear; it does not break.
- **Boundaries.** Strength 5 and 200 N admitted, outside rejected.
- **Run/Reset.** Reset restores the exact pre-break graph.
- **Save/Load.** Strength preset and attachments survive save and Load.
- **Integrations.** Load-limited connector integration task [sequence-task-538](../requirements.md#sequence-task-538) (gap-05: survives below threshold, breaks above, topology and load paths change, Reset restores the pre-break graph); interaction rows IX-01 contact impulse ([interaction-01](../requirements.md#interaction-01)) for the dropped load, IX-03 joint constraint ([interaction-03](../requirements.md#interaction-03)) and IX-37 structural fracture ([interaction-37](../requirements.md#interaction-37)). Campaign first use: GAP-05 row of the [campaign allocation](../requirements.md#campaign-gap-allocation) — introduction 43, practice 44, reuse 65, 96, 139.

## 6. Open questions

1. Whether one connector may combine limits (tension and shear) or each part has exactly one failure mode (proposed one). Unspecified — owner decision.
2. Shear presets (proposed 20/60/120 N).
