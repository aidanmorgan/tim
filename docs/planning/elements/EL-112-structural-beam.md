# EL-112 · Structural beam — named-identity readiness spec

Story 7.0 readiness spec ([spec](../../../_bmad-output/implementation-artifacts/spec-7-0-element-implementation-readiness.md)). Citations are `path@a6c914e:Lstart-Lend`; current-tree citations use the same baseline. Values marked **proposed** have no legacy or requirement source; each carries a one-line justification and the owner may revise it.

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name / type | EL-112 · Structural beam · Construction |
| Anchor | [requirements.md#element-112](../requirements.md#element-112); [named-elements entry](../invest/named-elements.md#element-112); umbrella [gap-01](../requirements.md#gap-01) (index only) |
| Related identities | [EL-113 Structural brace](EL-113-structural-brace.md) (same frame lesson); [EL-114 Placeable pivot](EL-114-placeable-pivot.md) and [EL-159](EL-159-tension-limited-connector.md)/[160](EL-160-shear-limited-connector.md)/[161](EL-161-bending-limited-connector.md) connectors (what a beam end attaches to). No CAT refines it; the nearest catalogue box geometry is [CAT-066 Wall](CAT-066-wall.md) (static) and the [CAT-034 Impact lever](CAT-034-impact_lever.md) beam (dynamic box on a hinge). |
| Roadmap story | Unscheduled. Campaign: introduction 4 "Brace Yourself", practice 5, reuse 18, 65, 138 ([gap-01](../requirements.md#gap-01)). |
| Status | Not started. No `WorkshopPartKind` member (`engine/gpu/WorkshopPartKind.cs@a6c914e:L3-L3`). |

## 2. Declaration

The requirements row names no variants; there is one mode.

- **Bodies and shapes.** One dynamic homogeneous box, the only dynamic shapes admitted being one sphere or one box per body (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L226-L231`). Local X is the beam axis.
  - **Proposed** default length 2.0 m, cross-section 0.2 m (Y) × 0.3 m (Z) — two bays of a 1 m frame on the 16.8 × 9.8 m bench (`DESIGN.md@a6c914e:L195-L195`), slimmer than the 0.24 m lever beam.
- **Mass and material.**
  - **Proposed** linear density 0.75 kg/m (1.5 kg at 2 m) — between the 1 kg Basketball and the 2 kg lever beam, so a 4 kg Bowling ball visibly loads it.
  - Inertia: the homogeneous box law already compiled by `RigidMassProperties.Compile` (`engine/gpu/RigidMassProperties.cs@a6c914e:L26-L38`).
  - **Proposed** contact material: friction 0.5, restitution 0.1, bounce threshold 0.1 m/s, rolling resistance 0 — matte ceramic/wood that does not bounce cargo; boxes declare zero rolling resistance ([f32 materials row](../../gpu-f32-physics.md)).
- **Constraints.** None of its own. A beam is held only by an explicit connector at an end socket: [EL-114](EL-114-placeable-pivot.md) (pin) or EL-159/160/161 (strength-limited). No implicit world anchor ([gap-01 integration](../requirements.md#gap-01)).
- **Typed ports.** Two structural attachment sockets, `EndA` at (−L/2, 0, 0) and `EndB` at (+L/2, 0, 0) in part space.
  - **Proposed** new closed enum member `WorkshopConnectionDomain.Structural` and sockets `StructuralEndA`/`StructuralEndB` — the current domains are only Activation and Electrical (`engine/gpu/WorkshopConnections.cs@a6c914e:L8-L11`).
  - **Proposed** two rope sockets, `TieA` at (−L/2, 0, 0) and `TieB` at (+L/2, 0, 0), rope domain (added by Story 10.2) — distinct typed identities beside the structural sockets let a Weight (EL-204) hang from a beam end on a straight, collinear load path, which the EL-159 and EL-160 tension and axial controls use.
- **Sensors and activation.** None.
- **Work and energy stores.** None. It stores only gravitational potential through the shared rigid body.
- **Parameters.**

  | Name | Type | Range | Default | Unit | Source |
  | --- | --- | --- | --- | --- | --- |
  | `length` | `Metres` (f32) | 0.5–4.0 | 2.0 | m | **proposed** — the Ramp's admitted length tops out at 4 m (`engine/gpu/WorkshopInstances.cs@a6c914e:L22-L31`) |

- **Cosmetic curves and UI bindings.** No curve; the artwork follows the committed pose. Resize along local X with the existing resize mode, as the Ramp does (`parts/RampPart.cs@a6c914e:L13-L13`).
- **Art.** Chamfered cream ceramic beam with warm-wood inset faces, navy joint marks and gold connection collars ([gap-01 visual style](../requirements.md#gap-01)). Tokens: cream `#fff8e9`, wood `#c28f52`, navy `#293954`, gold `#f7cb52` (`DESIGN.md@a6c914e:L147-L154`, `DESIGN.md@a6c914e:L169-L169`).
- **Catalogue and inventory.** **Proposed** id `structural_beam`, title "Structural beam", category Structure (as Ramp and Wall, `parts/catalog/wall.tres@a6c914e:L15-L18`). `WorkshopPartKind.StructuralBeam` appended as the last Free palette row (`engine/gpu/WorkshopInventory.cs@a6c914e:L47-L60`).

## 3. Engine capabilities

Families ([element map row](../general-engine-element-map.md), [binding](../../coverage/engine/element-02.json)): ContactImpulse, EnvironmentState, FiniteLedger, GeometryQuery, JointConstraint, RigidBodyDynamics, SlidingFriction, StateTransaction.

- **Exists now.** Dynamic box body, box–box/plane/sphere manifolds, Coulomb friction, gravity and Run/Reset (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L120`; `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L205-L229`). Gravity is a per-body vector (`CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L1271-L1279`).
- **Missing.**
  - JointConstraint. No joint exists in `engine/gpu`. Story 6.4 adds the first (prismatic) and Story 10.3 the revolute hinge ([epics](../../../_bmad-output/planning-artifacts/epics.md)). A rigid (weld) joint kind is in no story; owner S326 ([element map](../general-engine-element-map.md)).
  - Structural connection domain and sockets: S257 typed-port decisions ([decisions](../invest/decisions.md#s257)) have no structural row; owner S326. The rope sockets `TieA`/`TieB` wait for the Story 10.2 rope domain.
  - Capacity: 16 dynamic bodies per workbench (`engine/gpu/PhysicsBodyReadSet.cs@a6c914e:L30-L30`) bounds frame size.
- **Dependencies.** At least one connector (EL-114 or EL-159–161) or the bench to support it; EL-113 for the braced-frame integration.

## 4. Sources and legacy

- **Requirements.** Row: "Bounded rigid member transfers load through declared attachment sockets"; outcome "Unsupported member falls rather than floating at authored endpoints" ([element-112](../requirements.md#element-112)). Umbrella integration: a braced frame supports the load while an unbraced one deflects; no implicit world anchor; moving a member updates real contact and connected mass ([gap-01](../requirements.md#gap-01)).
- **Audit.** "Typed endpoints/materials and shared rigid assemblies. Compare a supported triangular frame with an unbraced control" (`docs/physics-puzzle-gap-audit.md@a6c914e:L80-L80`).
- **Decisions.** None bounded yet; [S635 environment](../invest/decisions.md#s635) governs gravity.
- **Legacy.** No beam part, level or test exists. Relevant joint-framework facts:

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| 1 | Legacy joint kinds were BallSocket, Hinge and Slider; there was no fixed (weld) kind. | `engine/physics/JointEquations.cs@a6c914e:L5-L5` | Carry forward as a recorded gap: a rigid beam joint needs a new kind. |
| 2 | A joint requires two distinct bodies, at least one dynamic, and an explicit connected-body collision policy. | `engine/physics/PhysicsJoint.cs@a6c914e:L16-L16`, `engine/physics/PhysicsJoint.cs@a6c914e:L33-L48` | Carry forward. |
| 3 | A collision-disabled body still receives gravity and joint reactions. | `CuriousContraptions.tests/PhysicsColliderUpdateTests.cs@a6c914e:L134-L148` | Carry forward as an acceptance fact. |
| 4 | Joint solve on a CPU `PhysicsWorld` with 1e-7 projection tolerances. | `CuriousContraptions.tests/JointRangeTests.cs@a6c914e:L48-L72` | Do not carry forward: CPU solver path and proof-grade tolerance. |

Files consulted: `engine/physics/JointEquations.cs`, `engine/physics/PhysicsJoint.cs`, `CuriousContraptions.tests/PhysicsColliderUpdateTests.cs`, `CuriousContraptions.tests/JointRangeTests.cs`.

## 5. Acceptance outline

Point of truth: [element-112](../requirements.md#element-112) and the [gap-01 integration](../requirements.md#gap-01).

- **Chrome recipe.** Select Structural beam in the palette, click the bench to place it, use move mode to lift a second beam above the bench, resize one to 3 m.
- **Positive.** A beam pinned at both ends to fixed supports carries a Basketball dropped on it; the reaction holds within the contact-slop envelope.
- **Negative or control.** The lifted, unattached beam falls on Run and rests on the bench; removing one support connector lets that end drop.
- **Boundaries.** Lengths 0.5 and 4.0 m admitted; 0.49 and 4.01 m rejected without changing the construction; a 4 kg Bowling ball on a 4 m beam stays within the game-grade envelope ([envelope](../../gpu-f32-physics.md#game-grade-envelope)).
- **Run/Reset.** Reset restores exact pose and connections; no residual velocity.
- **Save/Load.** Length, pose and attachments survive save, reload and Load.
- **Integrations.** Braced-frame integration task [sequence-task-534](../requirements.md#sequence-task-534) (gap-01, shared with EL-113); load-limited joints [sequence-task-538](../requirements.md#sequence-task-538) (gap-05); interaction rows IX-01 contact impulse ([interaction-01](../requirements.md#interaction-01)), IX-02 sliding friction ([interaction-02](../requirements.md#interaction-02)) and IX-03 joint constraint ([interaction-03](../requirements.md#interaction-03)). Campaign first use: GAP-01 row of the [campaign allocation](../requirements.md#campaign-gap-allocation) — introduction 4, practice 5, reuse 18, 65, 138.

## 6. Open questions

1. Which bodies a beam end may attach to: the bench, a Wall, another beam, or only an EL-114/159–161 connector. Unspecified — owner decision.
2. Whether a rigid weld joint kind is adopted, or every beam joint is a pin and rigidity comes only from triangulation (EL-113).
3. Whether cross-section and mass are configurable or derived from length only.
4. Structural port domain naming and capacity (sockets per end), and whether beams carry rope ties.
