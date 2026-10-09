# EL-115 · Linkage connector — named-identity readiness spec

Story 7.0 readiness spec ([spec](../../../_bmad-output/implementation-artifacts/spec-7-0-element-implementation-readiness.md)). Citations are `path@a6c914e:Lstart-Lend`. Values marked **proposed** have no legacy or requirement source; each carries a one-line justification and the owner may revise it.

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name / type | EL-115 · Linkage connector · Construction |
| Anchor | [requirements.md#element-115](../requirements.md#element-115); [named-elements entry](../invest/named-elements.md#element-115); umbrella [gap-02](../requirements.md#gap-02) (index only) |
| Related identities | [EL-114 Placeable pivot](EL-114-placeable-pivot.md) (same lesson); contrast with the tension-only rope [EL-205](../invest/named-elements.md#element-205) and [CAT-058 Rope anchor](CAT-058-rope_anchor.md): a link pushes and pulls. No CAT refines it. |
| Roadmap story | Unscheduled. Campaign: introduction 9 "Joint Effort", practice 10, reuse 38, 46, 138 ([gap-02](../requirements.md#gap-02)). |
| Status | Not started. No `WorkshopPartKind` member (`engine/gpu/WorkshopPartKind.cs@a6c914e:L3-L3`). |

## 2. Declaration

The requirements row names no variants; there is one mode. A linkage is a finite rigid rod with a pin at each end; it transmits both push and pull between two declared attachment points.

- **Bodies and shapes.** One dynamic homogeneous box rod (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L226-L231`), local X along the rod.
  - **Proposed** default length 1.5 m, cross-section 0.08 × 0.08 m — reaches across a lever-and-crank pair on the bench; thin enough to read as a link, above the 1/1024 m half-extent floor (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L105-L110`).
- **Mass and material.**
  - **Proposed** mass 0.2 kg at any length — a light link that does not dominate the bodies it couples (Basketball 1 kg).
  - **Proposed** material friction 0.4, restitution 0.1, threshold 0.1 m/s, rolling 0 — a slim ceramic rod.
- **Constraints.** Two revolute pin rows, one per end, each joining the rod to the attached body at a declared local anchor. Pins rotate about part-local Z (planar linkage).
  - **Proposed** connected-body collision Disabled for each pinned pair — the rod end overlaps the attached body at the pin.
  - A detached end declares no row; the rod then hangs from or falls off the remaining pin.
- **Typed ports.** `LinkEndA` and `LinkEndB` at (±L/2, 0, 0), domain `Structural` (**proposed** in [EL-112](EL-112-structural-beam.md); current domains `engine/gpu/WorkshopConnections.cs@a6c914e:L8-L11`). Each end attaches to one structural socket of a dynamic or static body.
- **Sensors and activation.** None.
- **Work and energy stores.** None; the link transmits work, it does not store or create it.
- **Parameters.**

  | Name | Type | Range | Default | Unit | Source |
  | --- | --- | --- | --- | --- | --- |
  | `length` | `Metres` (f32) | 0.3–4.0 | 1.5 | m | **proposed** — shortest link that still clears two pin collars; longest equals the beam |

- **Cosmetic curves and UI bindings.** None; art follows the committed pose. Length changes with the resize mode along local X.
- **Art.** Cream rod `#fff8e9`, gold end pins `#f7cb52`, navy end notches `#293954` ([gap-02 visual style](../requirements.md#gap-02); `DESIGN.md@a6c914e:L147-L154`).
- **Catalogue and inventory.** **Proposed** id `linkage`, title "Linkage", category Structure; `WorkshopPartKind.Linkage` appended last in the Free palette (`engine/gpu/WorkshopInventory.cs@a6c914e:L47-L60`).

## 3. Engine capabilities

Families ([element map row](../general-engine-element-map.md), [binding](../../coverage/engine/element-02.json)): ContactImpulse, EnvironmentState, FiniteLedger, GeometryQuery, JointConstraint, RigidBodyDynamics, SlidingFriction, StateTransaction.

- **Exists now.** Dynamic box rod, contact and gravity (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L120`; `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L1271-L1279`).
- **Missing.**
  - Revolute pin rows: Story 10.3 ([epics](../../../_bmad-output/planning-artifacts/epics.md)). A pin between two dynamic bodies (not body-to-fixture) is not exercised by any story; owner S329.
  - Structural sockets and domain: owner S329.
- **Dependencies.** Story 10.3 hinge row; EL-114 Pivot for a driven-lever demonstration; a driver such as a Bowling ball drop or, later, a Motor (CAT-042, Story 11.1).

## 4. Sources and legacy

- **Requirements.** Row: "Finite-length member couples declared body attachment points"; outcome "Disconnected endpoint cannot transmit motion" ([element-115](../requirements.md#element-115)). Integration: "A connected lever transfers motion; a detached control does not" ([gap-02](../requirements.md#gap-02)).
- **Audit.** "Demonstrate a driven linkage and disconnected control" (`docs/physics-puzzle-gap-audit.md@a6c914e:L81-L81`).
- **Legacy** (joint framework; no linkage part existed):

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| 1 | A ball-socket removes anchor velocity but leaves spin; it locks translation and leaves three rotational freedoms. | `CuriousContraptions.tests/JointConstraintTests.cs@a6c914e:L14-L20`; `CuriousContraptions.tests/PhysicsJointTests.cs@a6c914e:L174-L182` | Carry forward as the anchor-row behaviour expected of each link end (planar pin is the hinge restriction of it). |
| 2 | A rope only tensions and never pushes; slack is free. | `CuriousContraptions.tests/JointConstraintTests.cs@a6c914e:L86-L97`; `CuriousContraptions.tests/PhysicsJointTests.cs@a6c914e:L184-L195` | Carry forward as the contrast control: a link must push where a rope cannot. |
| 3 | Removing a constraint at runtime releases the load without replacing world or body. | `CuriousContraptions.tests/PhysicsJointUpdateTests.cs@a6c914e:L52-L69` | Carry forward: a detached end stops transmitting immediately. |
| 4 | A runtime constraint cannot snap an off-axis body into place. | `CuriousContraptions.tests/PhysicsJointUpdateTests.cs@a6c914e:L71-L80` | Carry forward: a link whose length does not match the socket distance is refused at connection, not snapped. |
| 5 | Joint edits were Attach/Replace/Detach with no upsert. | `engine/physics/PhysicsJointChange.cs@a6c914e:L5-L28` | Carry forward the explicit edit kinds; the CPU world mutation path does not carry forward. |

Files consulted: `CuriousContraptions.tests/JointConstraintTests.cs`, `CuriousContraptions.tests/PhysicsJointTests.cs`, `CuriousContraptions.tests/PhysicsJointUpdateTests.cs`, `engine/physics/PhysicsJointChange.cs`.

## 5. Acceptance outline

Point of truth: [element-115](../requirements.md#element-115) and the [gap-02 integration](../requirements.md#gap-02).

- **Chrome recipe.** Two Pivots each carrying a Structural beam; place a Linkage between the beams' far ends and connect both ends with the connection tool.
- **Positive.** A Bowling ball dropped on beam 1 rotates it; the link pushes beam 2, which rotates in the same sense.
- **Negative or control.** Disconnect `LinkEndB`: beam 1 still rotates, beam 2 stays still. Swap the link for a slack rope: lifting beam 1's end does not push beam 2.
- **Boundaries.** Lengths 0.3 and 4.0 m admitted; a length that differs from the socket distance by more than 0.05 m (**proposed** tolerance — larger than the 0.5 mm contact slop, smaller than a pin collar) is refused at connection without mutation.
- **Run/Reset.** Reset restores both attachments and exact poses.
- **Save/Load.** Length and attachments survive save and Load.
- **Integrations.** Pivot/linkage integration task [sequence-task-535](../requirements.md#sequence-task-535) (gap-02: a connected lever transfers motion, a detached control does not); interaction rows IX-03 joint constraint ([interaction-03](../requirements.md#interaction-03)), IX-01 contact impulse ([interaction-01](../requirements.md#interaction-01)) and, for the slack-rope contrast, IX-04 tension transmission ([interaction-04](../requirements.md#interaction-04)). Campaign first use: GAP-02 row of the [campaign allocation](../requirements.md#campaign-gap-allocation) — introduction 9, practice 10, reuse 38 (driven linkage), 46, 138.

## 6. Open questions

1. Whether a link is a massed rod body (proposed) or a massless distance constraint. Unspecified — owner decision.
2. Connection tolerance between rod length and socket distance (proposed 0.05 m), or whether connecting resizes the rod to fit.
3. Whether out-of-plane (ball-socket) link ends are supported, or only planar pins.
