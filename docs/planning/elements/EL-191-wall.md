# EL-191 · Wall — named-identity readiness spec

Story 7.0 readiness spec ([spec](../../../_bmad-output/implementation-artifacts/spec-7-0-element-implementation-readiness.md)). Citations are `path@a6c914e:Lstart-Lend`. Every value below is sourced; this identity has no proposed values.

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name / type | EL-191 · Wall · Gravity |
| Anchor | [requirements.md#element-191](../requirements.md#element-191); [named-elements entry](../invest/named-elements.md#element-191); scope source [campaign element coverage](../requirements.md#campaign-element-coverage) |
| Refines | [CAT-066 wall](CAT-066-wall.md) ([requirement](../requirements.md#current-cat-066)). Neighbour: [EL-190 Ramp](EL-190-ramp.md) (same static box path). |
| Roadmap story | Delivered: Stories 1.4 and 1.5 ([epics](../../../_bmad-output/planning-artifacts/epics.md)). |
| Status | Partial: body contact, rotation and resize delivered; air/sound/light occlusion and the solar_shadow fixture remain open with their families (Epics 12–14). |

## 2. Declaration

Configuration: width, height, thickness, no enumerated selector ([CAT-066](../requirements.md#current-cat-066)); one mode.

- **Bodies and shapes.** One static box (`engine/gpu/WorkshopPhysicsCompiler.cs@a6c914e:L83-L101`). `WallDimensions` stores full lengths in local axes (`engine/gpu/WorkshopWall.cs@a6c914e:L6-L16`); the compiler halves each into the box half-extents (`engine/gpu/WorkshopPhysicsCompiler.cs@a6c914e:L95-L96`).
  - Full width × height × thickness: default 3 × 2 × 0.25 m; minimum 0.4 × 0.4 × 0.12 m; maximum 8 × 6 × 2 m (`engine/gpu/WorkshopWall.cs@a6c914e:L8-L10`).
  - The collision box is the only blocking geometry; the UI pick radius (half the dimension diagonal, `parts/WallPart.cs@a6c914e:L26-L26`) never collides: "invisible silhouette bounds do not block it" ([element-191](../requirements.md#element-191)).
- **Mass and material.** Static. Restitution 1, threshold 0.1 m/s, friction 0.3, rolling resistance 0 (`engine/gpu/WorkshopPhysicsCompiler.cs@a6c914e:L90-L90`).
- **Constraints.** None.
- **Typed ports.** None (`engine/gpu/WorkshopConnections.cs@a6c914e:L24-L24`).
- **Sensors and activation.** None.
- **Work and energy stores.** None.
- **Parameters.**

  | Name | Type | Range | Default | Unit | Source |
  | --- | --- | --- | --- | --- | --- |
  | `width` | `Metres` | 0.4–8 | 3 | m | `engine/gpu/WorkshopWall.cs@a6c914e:L8-L10`; `parts/catalog/wall.tres@a6c914e:L7-L11` |
  | `height` | `Metres` | 0.4–6 | 2 | m | same |
  | `thickness` | `Metres` | 0.12–2 | 0.25 | m | same |

  The pointer-input boundary clamps finite drags into range before conversion and rejects non-finite input (`engine/gpu/WorkshopWall.cs@a6c914e:L17-L27`).
- **Cosmetic curves and UI bindings.** None (`engine/gpu/WorkshopWall.cs@a6c914e:L34-L34`). Three local-axis resize handles (`parts/WallPart.cs@a6c914e:L10-L10`); visual scale follows the canonical dimensions (`parts/WallPart.cs@a6c914e:L21-L27`).
- **Art.** Wood panel with two cream side strips `#f9e8c9` (`parts/WallPart.cs@a6c914e:L15-L17`); palette `#c28f52` (`DESIGN.md@a6c914e:L178-L178`).
- **Catalogue and inventory.** Id `wall`, title "Wall", category Structure, "A solid panel. Move, rotate or stretch its three local dimensions to guide and contain balls." (`parts/catalog/wall.tres@a6c914e:L13-L21`). Free palette row 5 (`engine/gpu/WorkshopInventory.cs@a6c914e:L55-L55`).

## 3. Engine capabilities

Families ([element map row](../general-engine-element-map.md), [binding](../../coverage/engine/element-02.json)): ContactImpulse, EnvironmentState, FiniteLedger, GeometryQuery, JointConstraint, RigidBodyDynamics, SlidingFriction, StateTransaction.

- **Exists now.** Static box contact with speculative margins against tunnelling through barriers at least 1 cm thick at up to 64 m/s ([envelope](../../gpu-f32-physics.md#game-grade-envelope); `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L208-L210`).
- **Missing.** Optical, acoustic and airflow occlusion by the same committed box: Epics 13, 14 and 12 respectively ([epics](../../../_bmad-output/planning-artifacts/epics.md)).
- **Dependencies.** None for body blocking.

## 4. Sources and legacy

- **Requirements.** Row: "Rigid barrier blocks bodies through actual transformed geometry"; outcome "A geometric gap allows passage; invisible silhouette bounds do not block it" ([element-191](../requirements.md#element-191)). CAT-066: limits and invalid dimensions, Cancel/one-gesture Undo, saved properties; occlusion controls distinct.

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| 1 | Catalogue dimensions WidthBits 16896 = 3, HeightBits 16384 = 2, ThicknessBits 13312 = 0.25 (binary16). | `parts/catalog/wall.tres@a6c914e:L7-L11` | Carry forward values; not binary16 storage. |
| 2 | Lessons: `wall_return` (wall returns a ball via a bumper), `wall_and_bumper`, `solar_shadow` (wall shades a solar panel). | `content/puzzles.json@a6c914e:L2867-L3144`, `content/puzzles.json@a6c914e:L3145-L3423`, `content/puzzles.json@a6c914e:L6226-L6574` | Carry forward; solar_shadow waits for Epic 13. |

Files consulted: `parts/catalog/wall.tres`, `parts/WallPart.cs`, `content/puzzles.json`.

## 5. Acceptance outline

Point of truth: [element-191](../requirements.md#element-191), [CAT-066](../requirements.md#current-cat-066). Existing Chrome proof: `tools/e2e/engine-core-2a3.test.ts@a6c914e:L62-L182` (deflection and resize).

- **Chrome recipe.** Free Workshop: place two Walls side by side with the move gizmo, leaving a gap; roll a Basketball off a Ramp toward the gap.
- **Positive.** A gap wider than the ball's 0.68 m diameter lets it pass; a solid wall in its path deflects it.
- **Negative or control.** Narrow the gap below the diameter by resizing a wall: the ball is blocked. Aim the ball through the space inside one wall's pick radius but outside its box: it passes untouched.
- **Boundaries.** Minimum and maximum dimensions admitted; drags beyond them clamp; non-finite input rejected.
- **Run/Reset.** Reset restores ball and wall exactly.
- **Save/Load.** Dimensions and pose survive save and Load.
- **Integrations.** Contact/cargo connection audit [sequence-task-285](../requirements.md#sequence-task-285) (ramps/walls with balls of every type); CAT-066 retained behaviour [todo-162](../requirements.md#todo-162) (resize, Undo, occlusion fixtures); interaction row IX-01 contact impulse ([interaction-01](../requirements.md#interaction-01)); later occlusion through IX-13 optical transport ([interaction-13](../requirements.md#interaction-13)) and IX-12 acoustic propagation ([interaction-12](../requirements.md#interaction-12)). Campaign first use: the "ramp/wall/material variants" entry of the first row of the [campaign element coverage](../requirements.md#campaign-element-coverage) — first use 1–10; combination and spaced reuse 21–30, 41–50, 91–100, 136–150.

## 6. Open questions

1. CAT-066 asks to "prove limits/invalid dimensions"; the input boundary clamps rather than rejects. Confirm clamping is the intended UI behaviour. Unspecified — owner decision.
