# EL-114 · Placeable pivot — named-identity readiness spec

Story 7.0 readiness spec ([spec](../../../_bmad-output/implementation-artifacts/spec-7-0-element-implementation-readiness.md)). Citations are `path@a6c914e:Lstart-Lend`. Values marked **proposed** have no legacy or requirement source; each carries a one-line justification and the owner may revise it.

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name / type | EL-114 · Placeable pivot · Construction |
| Anchor | [requirements.md#element-114](../requirements.md#element-114); [named-elements entry](../invest/named-elements.md#element-114); umbrella [gap-02](../requirements.md#gap-02) (index only) |
| Related identities | [EL-115 Linkage connector](EL-115-linkage-connector.md) (same lesson); [EL-112](EL-112-structural-beam.md)/[EL-113](EL-113-structural-brace.md) (frame corners); [EL-117 Axle](EL-117-axle.md) (the same revolute row on a wheel). It reuses the hinge the [CAT-034 Impact lever](CAT-034-impact_lever.md) introduces; no CAT refines it. |
| Roadmap story | Unscheduled. Campaign: introduction 9 "Joint Effort", practice 10, reuse 38, 46, 138 ([gap-02](../requirements.md#gap-02)). |
| Status | Not started. No `WorkshopPartKind` member (`engine/gpu/WorkshopPartKind.cs@a6c914e:L3-L3`). |

## 2. Declaration

Two variants from the gap-02 integration: "A free pivot rotates and a limited pivot stops at its actual bound" ([gap-02](../requirements.md#gap-02)).

- **Bodies and shapes.** A static bearing plinth plus one revolute joint row. The plinth is a static box (static bodies need zero mass, velocity and gravity, `engine/gpu/PhysicsDeclarations.cs@a6c914e:L70-L77`).
  - **Proposed** plinth 0.4 × 0.3 × 0.4 m, pin axis at its top centre, 0.25 m above the plinth base — large enough to grip but smaller than a Switch housing (1.1 × 0.25 × 1.0 m).
  - A pivot may instead join two dynamic members with no plinth (frame corner). Then it declares only the joint row and a gold pin artwork.
- **Mass and material.** Plinth static; **proposed** material friction 0.5, restitution 0.1 — same structural material as EL-112.
- **Constraints.** One revolute (hinge) row: body A is the rotating member; body B is the plinth or a second member. Free axis is part-local Z, the legacy hinge convention (`CuriousContraptions.tests/JointConstraintTests.cs@a6c914e:L21-L32`).
  - Connected-body collision: **proposed** Disabled for the joined pair — members overlap at the pin; legacy made the policy explicit (`engine/physics/PhysicsJoint.cs@a6c914e:L16-L16`).
  - Joint softness: **proposed** reuse the contact soft parameters, 60 Hz and damping ratio 10 (`CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L222-L223`) — one stiffness tune for every TGS Soft row.
- **Typed ports.** Two structural sockets, `PivotA` (rotating member) and `PivotB` (support), domain `Structural` (**proposed** in [EL-112](EL-112-structural-beam.md)). An unconnected `PivotA` declares no joint.
- **Sensors and activation.** None.
- **Work and energy stores.** None; the joint does no work.
- **Variant `Free`.** No travel limit; full rotation. Parameter `limit_mode = Free`.
- **Variant `Limited`.** Lower and upper angle stops, unilateral, inactive in the interior (`CuriousContraptions.tests/JointConstraintTests.cs@a6c914e:L73-L85`). Stops dissipate outward momentum (`CuriousContraptions.tests/SharedHingeMechanicsTests.cs@a6c914e:L108-L127`) and a resting stop acts as an immovable surface (`CuriousContraptions.tests/SharedHingeMechanicsTests.cs@a6c914e:L154-L164`).
- **Parameters.**

  | Name | Type | Range | Default | Unit | Source |
  | --- | --- | --- | --- | --- | --- |
  | `limit_mode` | enum `PivotLimitMode { Free, Limited }` | closed set | `Free` | — | variants from [gap-02](../requirements.md#gap-02) |
  | `lower_angle` | f32 | −π < lower ≤ upper < π (Limited only) | −π/4 | rad | range: legacy hinge branch (`engine/physics/PhysicsJoint.cs@a6c914e:L137-L139`); default **proposed** — a 90° swing reads clearly on a lever |
  | `upper_angle` | f32 | as above | +π/4 | rad | as above |

- **Cosmetic curves and UI bindings.** Moving art follows the solved joint pose; no curve. A selected-only arc shows the actual limits ([gap-02 visual style](../requirements.md#gap-02)). Angles set by a rotate-gizmo drag on the arc handles, not a numeric inspector.
- **Art.** Cream bearing plinth `#fff8e9`, gold visible pin `#f7cb52`, navy axis notch `#293954` ([gap-02](../requirements.md#gap-02); `DESIGN.md@a6c914e:L147-L154`).
- **Catalogue and inventory.** **Proposed** id `pivot`, title "Pivot", category Structure; `WorkshopPartKind.Pivot` appended last in the Free palette (`engine/gpu/WorkshopInventory.cs@a6c914e:L47-L60`).

## 3. Engine capabilities

Families ([element map row](../general-engine-element-map.md), [binding](../../coverage/engine/element-02.json)): ContactImpulse, EnvironmentState, FiniteLedger, GeometryQuery, JointConstraint, RigidBodyDynamics, SlidingFriction, StateTransaction.

- **Exists now.** Static box plinth, dynamic members, contact and gravity (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L120`). TGS Soft contact rows and soft parameters (`CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L205-L240`).
- **Missing.** Revolute joint row with travel limits: Story 10.3 builds it for CAT-034 ([epics](../../../_bmad-output/planning-artifacts/epics.md)). The placeable part, its sockets and the structural domain: unscheduled, owner S328.
- **Dependencies.** Story 10.3 hinge row; a dynamic member (EL-112 beam or EL-115 link) to rotate.

## 4. Sources and legacy

- **Requirements.** Row: "Axis joint constrains translation while allowing declared rotation"; outcome "Off-axis load is solved by the shared joint; no animation path" ([element-114](../requirements.md#element-114)). Integration: connected lever transfers motion, detached control does not; Reset restores attachments, limits and transforms ([gap-02](../requirements.md#gap-02)).
- **Audit.** "Build on shared joint constraints; bounded attachments and visible motion limits ... Kernel hinges alone do not close this task" (`docs/physics-puzzle-gap-audit.md@a6c914e:L81-L81`).
- **Legacy** (joint framework; no placeable pivot part existed):

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| 1 | Hinge pendulum keeps its pivot (anchor point velocity zero), gains no energy, and replays exactly after Restore. | `CuriousContraptions.tests/PhysicsJointTests.cs@a6c914e:L66-L91` | Carry forward as acceptance. |
| 2 | Radial, axial and pivot-through impulses cannot rotate a hinge. | `CuriousContraptions.tests/SharedHingeMechanicsTests.cs@a6c914e:L61-L70` | Carry forward: "off-axis load is solved by the shared joint". |
| 3 | Balanced loads cancel; an off-centre load rotates the hinge. | `CuriousContraptions.tests/SharedHingeMechanicsTests.cs@a6c914e:L47-L59` | Carry forward. |
| 4 | Stops arrest high-speed travel and release inward motion. | `CuriousContraptions.tests/JointRangeTests.cs@a6c914e:L48-L72` | Carry forward (Limited variant); its 1e-7 tolerance is proof-grade, do not carry forward. |
| 5 | Hinge range must lie strictly inside (−π, π); invalid ranges reject. | `engine/physics/PhysicsJoint.cs@a6c914e:L130-L141`; `CuriousContraptions.tests/JointRangeTests.cs@a6c914e:L175-L187` | Carry forward. |
| 6 | Initial twist is measured against the authored frame, not re-zeroed. | `CuriousContraptions.tests/SceneJointDeclarationTests.cs@a6c914e:L186-L202` | Carry forward. |
| 7 | Direction policy (Both/Positive/Negative ratchet). | `engine/physics/PhysicsJoint.cs@a6c914e:L117-L117` | Do not carry forward here: a ratchet is a separate element, not a pivot mode. |

Files consulted: `CuriousContraptions.tests/PhysicsJointTests.cs`, `CuriousContraptions.tests/SharedHingeMechanicsTests.cs`, `CuriousContraptions.tests/JointRangeTests.cs`, `CuriousContraptions.tests/JointConstraintTests.cs`, `CuriousContraptions.tests/SceneJointDeclarationTests.cs`, `engine/physics/PhysicsJoint.cs`.

## 5. Acceptance outline

Point of truth: [element-114](../requirements.md#element-114) and the [gap-02 integration](../requirements.md#gap-02).

- **Chrome recipe.** Place a Pivot on the bench, place a Structural beam across it, connect beam to `PivotA` with the connection tool; set Limited and drag the arc handles.
- **Positive (Free).** A Bowling ball dropped on one arm rotates the beam about the pin; the pin stays put.
- **Positive (Limited).** The same drop stops the arm at the upper bound; the beam rests on the stop.
- **Negative or control.** Detach the beam: it falls off the plinth; a ball dropped directly over the pin produces no rotation.
- **Boundaries.** Limits at ±(π − 0.01) admitted; ±π rejected; lower > upper rejected without mutation.
- **Run/Reset.** Reset restores attachment, mode, limits and exact pose.
- **Save/Load.** Mode, limits and attachment survive save and Load.
- **Integrations.** Pivot/linkage integration task [sequence-task-535](../requirements.md#sequence-task-535) (gap-02: connected lever transfers motion, detached control does not, free versus limited pivot, Reset restores attachments and limits); interaction rows IX-03 joint constraint ([interaction-03](../requirements.md#interaction-03)) and IX-01 contact impulse ([interaction-01](../requirements.md#interaction-01)). Campaign first use: GAP-02 row of the [campaign allocation](../requirements.md#campaign-gap-allocation) — introduction 9, practice 10, reuse 38, 46, 138.

## 6. Open questions

1. Whether the plinth may also be mounted on a Wall or beam (moving support), or only on the bench. Unspecified — owner decision.
2. Whether joint softness gets its own declared parameters or always reuses the contact tune.
3. Whether stops carry a restitution (legacy stops were fully dissipative).
