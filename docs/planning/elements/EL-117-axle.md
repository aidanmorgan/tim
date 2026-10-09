# EL-117 · Axle — named-identity readiness spec

Story 7.0 readiness spec ([spec](../../../_bmad-output/implementation-artifacts/spec-7-0-element-implementation-readiness.md)). Citations are `path@a6c914e:Lstart-Lend`. Values marked **proposed** have no legacy or requirement source; each carries a one-line justification and the owner may revise it.

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name / type | EL-117 · Axle · Construction |
| Anchor | [requirements.md#element-117](../requirements.md#element-117); [named-elements entry](../invest/named-elements.md#element-117); umbrella [gap-03](../requirements.md#gap-03) (index only) |
| Related identities | [EL-116 Passive wheel](EL-116-passive-wheel.md) (what it carries); [EL-114 Placeable pivot](EL-114-placeable-pivot.md) (same revolute row); [GAP-04 Driven wheel](GAP-04-driven-wheel.md) (supplied torque on the same axle). No CAT refines it. |
| Roadmap story | Unscheduled. Campaign: introduction 18 "Wheel Meet Again", practice 19, reuse 37, 68, 138 ([gap-03](../requirements.md#gap-03)). |
| Status | Not started. No `WorkshopPartKind` member (`engine/gpu/WorkshopPartKind.cs@a6c914e:L3-L3`). |

## 2. Declaration

Two variants: the outcome "Locked axle prevents the formerly permitted rotation" ([element-117](../requirements.md#element-117)) requires a free and a locked mode, and the integration says "Compare locked/free axle modes separately if both are supported" ([gap-03](../requirements.md#gap-03)).

- **Bodies and shapes.** No body of its own. The axle is one revolute joint row from the wheel hub to a chassis socket, plus a cosmetic shaft.
  - **Proposed** cosmetic shaft length 0.4 m, radius 0.04 m — spans a 0.15 m wheel and a 0.2 m beam with visible clearance.
- **Mass and material.** None (massless joint); the wheel and chassis carry mass.
- **Constraints.** One revolute row, free axis along the wheel axis (part-local Z), anchors at the wheel centre and the chassis socket.
  - **Proposed** connected-body collision Disabled between wheel and chassis — the hub overlaps the chassis at the shaft.
  - **Proposed** joint softness as EL-114 (60 Hz, ζ 10, `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L222-L223`) — one tune for all TGS Soft rows.
- **Typed ports.** `AxleWheel` (to the EL-116 `Hub`) and `AxleChassis` (to a structural socket on EL-112 or another chassis member), domain `Structural` (**proposed** in [EL-112](EL-112-structural-beam.md)).
- **Sensors and activation.** None.
- **Work and energy stores.** None; a free axle transmits no torque about its axis.
- **Variant `Free`.** The wheel spins freely about the axle; translation and the two other rotations are constrained (legacy hinge: one free angular freedom, `CuriousContraptions.tests/JointConstraintTests.cs@a6c914e:L21-L32`).
- **Variant `Locked`.** Zero-width travel range: the formerly free rotation is removed and the wheel is welded to the chassis, so on a ramp it slides on friction rather than rolls (legacy: zero-width range locks, interior motion is not locked, `CuriousContraptions.tests/JointRangeTests.cs@a6c914e:L89-L102`).
- **Parameters.**

  | Name | Type | Range | Default | Unit | Source |
  | --- | --- | --- | --- | --- | --- |
  | `mode` | enum `AxleMode { Free, Locked }` | closed set | `Free` | — | variants from the requirement rows above |

- **Cosmetic curves and UI bindings.** None; the shaft follows the committed wheel pose. A navy lock collar appears only in `Locked` (shape cue, not colour only).
- **Art.** Gold shaft collar `#f7cb52`, navy lock collar `#293954`, cream bearing `#fff8e9` ([gap-03 visual style](../requirements.md#gap-03); `DESIGN.md@a6c914e:L147-L154`).
- **Catalogue and inventory.** **Proposed** id `axle`, title "Axle", category Structure; `WorkshopPartKind.Axle` appended last in the Free palette (`engine/gpu/WorkshopInventory.cs@a6c914e:L47-L60`). Mode toggled by a contextual two-state control on the selected axle.

## 3. Engine capabilities

Families ([element map row](../general-engine-element-map.md), [binding](../../coverage/engine/element-02.json)): ContactImpulse, EnvironmentState, FiniteLedger, GeometryQuery, JointConstraint, RigidBodyDynamics, SlidingFriction, StateTransaction.

- **Exists now.** Dynamic bodies, friction and rolling resistance (`CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L701-L765`).
- **Missing.** Revolute joint row between two dynamic bodies, and a zero-width (locked) range: Story 10.3 builds the hinge with stops for a fixture ([epics](../../../_bmad-output/planning-artifacts/epics.md)); dynamic–dynamic use and the lock are unscheduled, owner S331. The wheel's cylinder collider is missing (see [EL-116](EL-116-passive-wheel.md)).
- **Dependencies.** EL-116 Passive wheel and a chassis member (EL-112).

## 4. Sources and legacy

- **Requirements.** Row: "Rotating support couples a wheel to the chassis with declared freedom"; outcome "Locked axle prevents the formerly permitted rotation" ([element-117](../requirements.md#element-117)). Integration: loaded chassis rolls downhill, slips under insufficient traction, stops against a block; lifted wheels cannot support it ([gap-03](../requirements.md#gap-03)).
- **Audit.** "Compare roll, slip and blocked wheel; no scripted travel path" (`docs/physics-puzzle-gap-audit.md@a6c914e:L82-L82`).
- **Legacy** (joint framework; no axle part existed):

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| 1 | A hinge has exactly one free angular degree of freedom. | `CuriousContraptions.tests/JointConstraintTests.cs@a6c914e:L21-L32` | Carry forward (Free). |
| 2 | Interior motion is not locked; a zero-width range locks the joint. | `CuriousContraptions.tests/JointRangeTests.cs@a6c914e:L89-L102` | Carry forward (Locked). |
| 3 | A zero-width range was compiled as an extra bilateral row in one block. | `engine/physics/PhysicsJoint.cs@a6c914e:L163-L170` | Carry forward the idea (lock = bilateral angular row); the CPU block solver does not carry forward. |
| 4 | Runtime latch held, released and restored both constraint versions. | `CuriousContraptions.tests/PhysicsJointUpdateTests.cs@a6c914e:L18-L50` | Do not carry forward now: the axle mode is construction-time, not switched during a Run. |

Files consulted: `CuriousContraptions.tests/JointConstraintTests.cs`, `CuriousContraptions.tests/JointRangeTests.cs`, `CuriousContraptions.tests/PhysicsJointUpdateTests.cs`, `engine/physics/PhysicsJoint.cs`.

## 5. Acceptance outline

Point of truth: [element-117](../requirements.md#element-117) and the [gap-03 integration](../requirements.md#gap-03).

- **Chrome recipe.** Build a chassis: one Structural beam, two Wheels, two Axles connected beam-to-hub; place it at the top of a tilted Ramp; add a Bowling ball on the beam as load.
- **Positive (Free).** The loaded chassis rolls down the ramp, wheels spinning, and stops against a Wall at the bottom.
- **Positive (Locked).** Set both axles to Locked: the wheels do not spin; the chassis slides only if the slope exceeds the friction angle, otherwise stays put.
- **Negative or control.** Lift the chassis so the wheels hang clear: it cannot be supported by the wheels and falls; a detached axle leaves its wheel rolling away alone.
- **Boundaries.** Mode outside the enum rejected at the save/UI boundary; an axle to a second hub on the same wheel refused.
- **Run/Reset.** Reset restores mode, attachments and exact poses.
- **Save/Load.** Mode and attachments survive save and Load.
- **Integrations.** Wheel/axle integration task [sequence-task-536](../requirements.md#sequence-task-536) (gap-03, including the separate locked/free axle comparison); interaction rows IX-03 joint constraint ([interaction-03](../requirements.md#interaction-03)) and IX-02 sliding friction ([interaction-02](../requirements.md#interaction-02)) for the locked-axle slide. Campaign first use: GAP-03 row of the [campaign allocation](../requirements.md#campaign-gap-allocation) — introduction 18, practice 19, reuse 37, 68, 138.

## 6. Open questions

1. Whether the mode may change during a Run (a brake), or only between Runs. Unspecified — owner decision.
2. Whether an axle may also carry a free-spinning non-wheel body (a rotor), overlapping EL-114.
