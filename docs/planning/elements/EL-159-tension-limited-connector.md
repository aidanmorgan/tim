# EL-159 · Tension-limited connector — named-identity readiness spec

Story 7.0 readiness spec ([spec](../../../_bmad-output/implementation-artifacts/spec-7-0-element-implementation-readiness.md)). Citations are `path@a6c914e:Lstart-Lend`. Values marked **proposed** have no legacy or requirement source; each carries a one-line justification and the owner may revise it.

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name / type | EL-159 · Tension-limited connector · Construction |
| Anchor | [requirements.md#element-159](../requirements.md#element-159); [named-elements entry](../invest/named-elements.md#element-159); umbrella [gap-05](../requirements.md#gap-05) (index only) |
| Related identities | Siblings [EL-160 Shear-limited](EL-160-shear-limited-connector.md) and [EL-161 Bending-limited](EL-161-bending-limited-connector.md) (same collar, different failure mode); joins [EL-112](EL-112-structural-beam.md)/[EL-113](EL-113-structural-brace.md) members; loaded by a [EL-204 Weight](EL-204-weight.md); extends releasable bridges (EL-186). No CAT refines it. |
| Roadmap story | Unscheduled. Campaign: introduction 43 "The Last Straw", practice 44, reuse 65, 96, 139 ([gap-05](../requirements.md#gap-05)). |
| Status | Not started. No joint or fracture capability in `engine/gpu` (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L163-L192`). |

## 2. Declaration

The requirements row names no variants; this identity is the axial-tension failure mode of the gap-05 family.

- **Bodies and shapes.** No body of its own: a joint plus a cosmetic collar at the joined sockets.
  - **Proposed** collar art 0.3 m long, radius 0.12 m — visibly larger than a beam's 0.2 × 0.3 m section so the stress cue reads.
- **Mass and material.** None; the joined members carry mass.
- **Constraints.** One rigid joint holding the two sockets coincident (point constraint on translation). Its axis is the line from member A's socket into member B (the connector's "axial" direction).
  - **Proposed** the connector is a pin (rotation free) — tension needs no moment capacity; the bending-limited sibling carries the rigid case.
  - **Proposed** connected-body collision Disabled for the joined pair — consistent with EL-114.
- **Typed ports.** `ConnectorA`, `ConnectorB`, domain `Structural` (**proposed** in [EL-112](EL-112-structural-beam.md)).
- **Sensors and activation.** A load monitor on the joint's reaction.
  - Axial tension T = component of the joint reaction force along the connector axis that pulls the sockets apart; compression is not counted.
  - **Proposed** evaluation: the reaction impulse summed over the four 480 Hz substeps of one 120 Hz tick, divided by the tick (1/120 s), so one-substep solver spikes cannot break a joint — numerical residuals clamp or continue, never fault ([envelope](../../gpu-f32-physics.md#game-grade-envelope)).
  - The connector breaks at the first tick with T > `tensile_strength`. "Subthreshold tension retains the joint" ([element-159](../requirements.md#element-159)).
- **Work and energy stores.** None; a break releases no stored energy (no spring).
- **Break transaction.** The joint row is removed atomically at the tick boundary (TopologyTransaction); the members keep their velocities; each keeps its half-collar as art. No new bodies, so "debris remains bounded" ([gap-05](../requirements.md#gap-05)).
- **Parameters.**

  | Name | Type | Range | Default | Unit | Source |
  | --- | --- | --- | --- | --- | --- |
  | `tensile_strength` | f32 | 5–200 | 60 | N | **proposed** — a 2 m hanging beam (1.5 kg) carrying a 4 kg Weight statically pulls (1.5 + 4) × 9.81 = 53.9 N, which holds with 6.1 N margin; any drop that snatches the load (fact below) exceeds it; 200 N stays well inside the envelope loads |

- **Cosmetic curves and UI bindings.** Selected-only stress indicator: segmented navy witness marks fill toward the gold load band ← committed T / strength; shape and scale, not colour alone. A break opens a real visible gap ([gap-05 visual style](../requirements.md#gap-05)). Strength chosen by a stepped selector of **proposed** presets 20 / 60 / 120 N — a numeric inspector is not allowed.
- **Art.** Cream collar `#fff8e9`, gold load band `#f7cb52`, navy witness marks `#293954` (`DESIGN.md@a6c914e:L147-L154`).
- **Catalogue and inventory.** **Proposed** id `tension_connector`, title "Tension connector", category Structure; appended last in the Free palette (`engine/gpu/WorkshopInventory.cs@a6c914e:L47-L60`).

## 3. Engine capabilities

Families ([element map row](../general-engine-element-map.md), [binding](../../coverage/engine/element-02.json)): EnvironmentState, FiniteLedger, GeometryQuery, JointConstraint, RigidBodyDynamics, StateTransaction, StructuralFracture, TopologyTransaction.

- **Exists now.** Committed per-tick reads and atomic Run/Reset of the document (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L163-L192`).
- **Missing.**
  - Joint rows (Story 10.3 hinge, [epics](../../../_bmad-output/planning-artifacts/epics.md)) and a per-joint reaction read-back: owner S680.
  - StructuralFracture / TopologyTransaction (joint removal mid-Run): decision owner S543 "fracture" row, named next S563 ([decisions](../invest/decisions.md#s543)); no story schedules it.
  - Rope domain and the beam's rope socket `TieB` (proposed in EL-112) for the Weight load: Story 10.2.
- **Dependencies.** A structural member (EL-112) and a fixed overhead support; a Weight (EL-204) on a rope (EL-205) as the load.

## 4. Sources and legacy

- **Requirements.** Row: "Declared tensile strength governs failure under axial load"; outcome "Subthreshold tension retains the joint" ([element-159](../requirements.md#element-159)). Integration: the same assembly survives below threshold and breaks above it; loss changes topology and load paths; debris bounded; Reset restores the exact pre-break graph ([gap-05](../requirements.md#gap-05)).
- **Audit.** "explicit finite breaking threshold and readable stress cue ... restore topology on Reset. Extends existing releasable bridges rather than replacing them" (`docs/physics-puzzle-gap-audit.md@a6c914e:L84-L84`).
- **Legacy.** No strength-limited joint existed. Topology facts that carry over:

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| 1 | Joint edits were explicit Attach/Replace/Detach; no upsert. | `engine/physics/PhysicsJointChange.cs@a6c914e:L5-L28` | Carry forward Detach as the break. |
| 2 | An impact-scheduled change released a rope while a missed impact left the load held. | `CuriousContraptions.tests/PhysicsImpactJointTests.cs@a6c914e:L84-L104` | Carry forward the positive/control shape. |
| 3 | Removing a joint released the load without replacing world or body. | `CuriousContraptions.tests/PhysicsJointUpdateTests.cs@a6c914e:L52-L69` | Carry forward. |
| 4 | A failed step restored already-committed joint and collider changes. | `CuriousContraptions.tests/PhysicsImpactJointTests.cs@a6c914e:L35-L54` | Carry forward atomicity; the CPU rollback mechanism does not carry forward. |

Files consulted: `engine/physics/PhysicsJointChange.cs`, `CuriousContraptions.tests/PhysicsImpactJointTests.cs`, `CuriousContraptions.tests/PhysicsJointUpdateTests.cs`.

## 5. Acceptance outline

Point of truth: [element-159](../requirements.md#element-159) and the [gap-05 integration](../requirements.md#gap-05).

- **Chrome recipe.** Hang a 2 m Structural beam (1.5 kg) vertically from a fixed overhead support through a Tension connector (60 N) on its upper structural socket `EndA`; tie a rope from the beam's lower-end rope socket `TieB` at (+L/2, 0, 0) to a Weight. The load path is collinear, so the connector sees pure tension.
- **Negative or control (static placements).** A 1 kg Weight hanging taut at Run start: T = (1.5 + 1) × 9.81 = 24.5 N, holds. A 4 kg Weight hanging taut: T = (1.5 + 4) × 9.81 = 53.9 N, holds for the whole Run.
- **Positive (drop).** The 4 kg Weight tied with 0.5 m of slack and released from the tie height: it falls 0.5 m to 3.13 m/s, and the rope arrests 4 × 3.13 = 12.5 N·s. Even spread over 0.1 s that adds 125 N, so T ≥ 53.9 + 125 ≈ 179 N > 60; the connector breaks and the beam and Weight fall; a real gap opens.
- **Boundaries.** Strength 5 and 200 N admitted, outside rejected; a compressive reaction of any size (native check) never breaks it.
- **Run/Reset.** Reset restores the exact pre-break joint graph and poses.
- **Save/Load.** Strength preset and attachment survive save and Load; broken state is never saved.
- **Integrations.** Load-limited connector integration task [sequence-task-538](../requirements.md#sequence-task-538) (gap-05: survives below threshold, breaks above, topology and load paths change, debris bounded, Reset restores the pre-break graph); rope/weight connection audit [sequence-task-275](../requirements.md#sequence-task-275) for the Weight load path; interaction rows IX-03 joint constraint ([interaction-03](../requirements.md#interaction-03)), IX-04 tension transmission ([interaction-04](../requirements.md#interaction-04)) and IX-37 structural fracture ([interaction-37](../requirements.md#interaction-37)). Campaign first use: GAP-05 row of the [campaign allocation](../requirements.md#campaign-gap-allocation) — introduction 43, practice 44, reuse 65, 96, 139.

## 6. Open questions

1. Load measure: tick-averaged reaction (proposed) versus peak substep impulse. Unspecified — owner decision.
2. Pin (proposed) or rigid joint for the tension connector.
3. Strength presets (proposed 20/60/120 N) versus a continuous range.
