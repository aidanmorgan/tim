# EL-113 · Structural brace — named-identity readiness spec

Story 7.0 readiness spec ([spec](../../../_bmad-output/implementation-artifacts/spec-7-0-element-implementation-readiness.md)). Citations are `path@a6c914e:Lstart-Lend`. Values marked **proposed** have no legacy or requirement source; each carries a one-line justification and the owner may revise it.

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name / type | EL-113 · Structural brace · Construction |
| Anchor | [requirements.md#element-113](../requirements.md#element-113); [named-elements entry](../invest/named-elements.md#element-113); umbrella [gap-01](../requirements.md#gap-01) (index only) |
| Related identities | [EL-112 Structural beam](EL-112-structural-beam.md) (the members it triangulates); [EL-114 Placeable pivot](EL-114-placeable-pivot.md) (pinned frame corners). No CAT refines it. |
| Roadmap story | Unscheduled. Campaign: introduction 4 "Brace Yourself", practice 5, reuse 18, 65, 138 ([gap-01](../requirements.md#gap-01)). |
| Status | Not started. No `WorkshopPartKind` member (`engine/gpu/WorkshopPartKind.cs@a6c914e:L3-L3`). |

## 2. Declaration

The requirements row names no variants; there is one mode. The brace differs from the beam by its role: it joins two members of a frame along a diagonal so that geometry, not a name, removes the frame's racking freedom.

- **Bodies and shapes.** One dynamic homogeneous box (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L226-L231`), local X along the brace.
  - **Proposed** default length 1.414 m, cross-section 0.12 m × 0.2 m — the diagonal of a 1 m square bay built from EL-112 beams, visibly slimmer than a beam.
- **Mass and material.**
  - **Proposed** linear density 0.5 kg/m (0.71 kg at default) — lighter than the beam's proposed 0.75 kg/m so it reads as a secondary member.
  - Inertia by the compiled box law (`engine/gpu/RigidMassProperties.cs@a6c914e:L26-L38`).
  - **Proposed** material as EL-112: friction 0.5, restitution 0.1, threshold 0.1 m/s, rolling 0 — one structural material keeps frame contacts uniform.
- **Constraints.** None of its own. Each end attaches through a pin connector (EL-114) to a beam's structural socket, so a braced rectangle becomes two rigid triangles; with the brace removed the pinned rectangle racks.
- **Typed ports.** Two structural sockets at (±L/2, 0, 0), domain `Structural` (**proposed** in [EL-112](EL-112-structural-beam.md); current domains `engine/gpu/WorkshopConnections.cs@a6c914e:L8-L11`).
  - **Proposed** a brace end may also attach mid-span to a beam at a snapped point every 0.5 m — lets one brace length serve several bay shapes without free-form welding.
- **Sensors and activation.** None.
- **Work and energy stores.** None.
- **Parameters.**

  | Name | Type | Range | Default | Unit | Source |
  | --- | --- | --- | --- | --- | --- |
  | `length` | `Metres` (f32) | 0.5–4.0 | 1.414 | m | **proposed** — same range as EL-112 so any built bay can be braced |

- **Cosmetic curves and UI bindings.** None; art follows the committed pose. Resize along local X.
- **Art.** Same family as the beam ([gap-01 visual style](../requirements.md#gap-01)): cream `#fff8e9` body, wood `#c28f52` inset, navy `#293954` joint marks, gold `#f7cb52` collars (`DESIGN.md@a6c914e:L147-L154`, `DESIGN.md@a6c914e:L169-L169`). A diagonal navy chevron on the inset distinguishes it from a beam without relying on colour alone.
- **Catalogue and inventory.** **Proposed** id `structural_brace`, title "Structural brace", category Structure; `WorkshopPartKind.StructuralBrace` appended last in the Free palette (`engine/gpu/WorkshopInventory.cs@a6c914e:L47-L60`).

## 3. Engine capabilities

Families ([element map row](../general-engine-element-map.md), [binding](../../coverage/engine/element-02.json)): ContactImpulse, EnvironmentState, FiniteLedger, GeometryQuery, JointConstraint, RigidBodyDynamics, SlidingFriction, StateTransaction.

- **Exists now.** Dynamic box, contact, friction and gravity (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L120`; `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L1271-L1279`).
- **Missing.**
  - Revolute pin joints between dynamic bodies: Story 10.3 builds the hinge ([epics](../../../_bmad-output/planning-artifacts/epics.md)); multi-joint closed loops (a triangle is a kinematic loop) are not in any story. Owner S327.
  - Structural sockets and domain (see EL-112), owner S327.
  - Dynamic-body capacity 16 (`engine/gpu/PhysicsBodyReadSet.cs@a6c914e:L30-L30`): a braced bay uses 5 bodies.
- **Dependencies.** EL-112 beams and EL-114 pins; a frame needs at least four beams, four pins and one brace.

## 4. Sources and legacy

- **Requirements.** Row: "Diagonal member constrains frame distortion through actual geometry and load"; outcome "Removing the brace changes stability; no frame-name bonus" ([element-113](../requirements.md#element-113)). Integration: braced frame supports the load, otherwise identical unbraced frame deflects or collapses ([gap-01](../requirements.md#gap-01)).
- **Audit.** "Compare a supported triangular frame with an unbraced control under the same load" (`docs/physics-puzzle-gap-audit.md@a6c914e:L80-L80`).
- **Legacy.** No brace part, level or test exists.

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| 1 | A hinge leaves exactly one free angular degree of freedom between its bodies. | `CuriousContraptions.tests/JointConstraintTests.cs@a6c914e:L21-L32` | Carry forward: pinned frame corners rack until braced. |
| 2 | An off-centre hinge uses parallel-axis inertia through its anchor rows. | `CuriousContraptions.tests/JointConstraintTests.cs@a6c914e:L33-L40` | Carry forward as solver behaviour expected of the shared joint row. |
| 3 | Joints with overlapping connected bodies need an explicit collision policy; a wrong policy can expose an impossible construction. | `CuriousContraptions.tests/PhysicsJointTests.cs@a6c914e:L210-L219` | Carry forward: braced corners disable collision between the joined pair. |

Files consulted: `CuriousContraptions.tests/JointConstraintTests.cs`, `CuriousContraptions.tests/PhysicsJointTests.cs`.

## 5. Acceptance outline

Point of truth: [element-113](../requirements.md#element-113) and the [gap-01 integration](../requirements.md#gap-01).

- **Chrome recipe.** Build a 1 m square bay from four beams joined by four pins, two pins fixed to the bench, then place a brace across the diagonal through palette, move and rotate controls only.
- **Positive.** A 4 kg Bowling ball dropped on the top beam is supported; the bay keeps its shape.
- **Negative or control.** Delete the brace and repeat: the bay racks and collapses under the same load. Renaming or recolouring nothing changes the outcome.
- **Boundaries.** Brace 0.5 and 4.0 m admitted; a brace too short to reach both sockets is refused at connection with the construction unchanged.
- **Run/Reset.** Reset restores frame geometry, brace attachments and zero velocity.
- **Save/Load.** Frame topology and the brace survive save and Load.
- **Integrations.** Braced-frame integration task [sequence-task-534](../requirements.md#sequence-task-534) (gap-01: braced frame holds, identical unbraced frame deflects, no implicit world anchor); interaction rows IX-01 contact impulse ([interaction-01](../requirements.md#interaction-01)), IX-02 sliding friction ([interaction-02](../requirements.md#interaction-02)) and IX-03 joint constraint ([interaction-03](../requirements.md#interaction-03)). Campaign first use: GAP-01 row of the [campaign allocation](../requirements.md#campaign-gap-allocation) — introduction 4, practice 5, reuse 18, 65, 138.

## 6. Open questions

1. Whether brace ends may attach mid-span (proposed 0.5 m snap) or only at beam end sockets. Unspecified — owner decision.
2. Whether the brace is a distinct kind or a beam in a diagonal role (one kind with two catalogue entries would conflict with "no frame-name bonus" only if behaviour differed).
3. Closed kinematic loops: confirm the shared TGS joint rows must solve a triangle loop without drift beyond the contact-slop envelope.
