# EL-187 · Basketball — named-identity readiness spec

Story 7.0 readiness spec ([spec](../../../_bmad-output/implementation-artifacts/spec-7-0-element-implementation-readiness.md)). Citations are `path@a6c914e:Lstart-Lend`. Every value below is sourced; this identity has no proposed values.

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name / type | EL-187 · Basketball · Gravity |
| Anchor | [requirements.md#element-187](../requirements.md#element-187); [named-elements entry](../invest/named-elements.md#element-187); scope source [campaign element coverage](../requirements.md#campaign-element-coverage) |
| Refines | [CAT-001 ball](CAT-001-ball.md) ([requirement](../requirements.md#current-cat-001)). Contrasts: [EL-188 Bowling ball](EL-188-bowling-ball.md), [EL-189 Tennis ball](EL-189-tennis-ball.md). |
| Roadmap story | Delivered: CAT-001-I, Epics 1–2 ([roadmap](../invest/vertical-delivery.md)); remaining modes (buoyancy/pressure) wait for Story 12.1 and [EL-125](EL-125-authored-atmosphere-preset.md). |
| Status | delivered; remaining items in §5 |

## 2. Declaration

The CAT-001 configuration lists radius, mass, bounce, drag and buoyancy as typed material fields with no enumerated selector ([CAT-001](../requirements.md#current-cat-001)); there is one mode.

- **Bodies and shapes.** One dynamic sphere: `WorkshopBall` with `Kind = Basketball` (`engine/gpu/WorkshopConstruction.cs@a6c914e:L67-L77`), compiled to one dynamic body, one material and one sphere collider (`engine/gpu/WorkshopPhysicsCompiler.cs@a6c914e:L32-L42`). Radius 0.34 m.
- **Mass and material.** `BallMaterial.For(Basketball)` (`engine/gpu/WorkshopConstruction.cs@a6c914e:L45-L53`), matching `parts/catalog/ball.tres@a6c914e:L8-L17` bit for bit:

  | Field | Value |
  | --- | --- |
  | Radius | 0.34 m |
  | Mass | 1 kg |
  | Bounce (restitution) | 0.55 |
  | Drag | 0.04 1/s |
  | Buoyancy | 0 |
  | Friction | 0.3 |
  | Bounce threshold | 0.1 m/s |
  | Rolling resistance | 0.035 |

  Inertia: solid sphere 0.4 m r² (`engine/gpu/RigidMassProperties.cs@a6c914e:L34-L38`). Admission rejects any other material for the kind (`engine/gpu/WorkshopConstruction.cs@a6c914e:L54-L62`).
- **Constraints.** None.
- **Typed ports.** None (`engine/gpu/WorkshopConnections.cs@a6c914e:L24-L24`).
- **Sensors and activation.** None owned; it is the named body for Receiver capture (`engine/gpu/WorkshopPuzzle.cs@a6c914e:L65-L73`), Switch triggers and Bumper work.
- **Work and energy stores.** None; kinetic and potential energy only.
- **Parameters.** None exposed: material is fixed per kind. Position and rotation via the move gizmo; authored puzzles lock the ball (`engine/gpu/WorkshopPuzzle.cs@a6c914e:L119-L119`).
- **Cosmetic curves and UI bindings.** None (`engine/gpu/WorkshopConstruction.cs@a6c914e:L70-L70`); the stripe follows the committed pose.
- **Art.** Sphere at the declared radius plus a stripe ring tilted 35°, darkened 40% (`parts/BallPart.cs@a6c914e:L9-L17`). Palette `#f57d38` (`DESIGN.md@a6c914e:L165-L165`).
- **Catalogue and inventory.** Id `ball`, title "Basketball", category Motion, description "A lively, medium-weight ball. Rolls, falls, and transfers momentum." (`parts/catalog/ball.tres@a6c914e:L19-L27`). First Free palette row (`engine/gpu/WorkshopInventory.cs@a6c914e:L50-L51`).

## 3. Engine capabilities

Families ([element map row](../general-engine-element-map.md), [binding](../../coverage/engine/element-02.json)): ContactImpulse, EnvironmentState, FiniteLedger, GeometryQuery, JointConstraint, RigidBodyDynamics, SlidingFriction, StateTransaction.

- **Exists now.** Every dry-mode family: dynamic sphere, contact with product restitution and geometric-mean friction (`CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L622-L656`), rolling resistance (`CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L721-L765`), gravity and drag (`CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L1271-L1279`), velocity envelope clamps (`CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L208-L220`).
- **Missing.** Buoyancy and pressure (EnvironmentState): Story 12.1 and EL-125. JointConstraint applies only through tether/rope consumers (Story 10.2).
- **Dependencies.** None for the dry mode.

## 4. Sources and legacy

- **Requirements.** Row: "Spherical cargo uses declared mass, radius and compliant contact properties"; outcome "Equal drop height produces reproducible material-dependent rebound" ([element-187](../requirements.md#element-187)). CAT-001: rest, boundary and two-height controls; ramp/pipe/Receiver integrations; stripes from committed pose ([CAT-001](../requirements.md#current-cat-001)).

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| 1 | Catalogue stores the material as binary16 bit patterns (RadiusBits 13681 = 0.34, …). | `parts/catalog/ball.tres@a6c914e:L8-L17` | Carry forward the decimal values; do not carry forward binary16 storage (f32 contract, [gpu-f32-physics](../../gpu-f32-physics.md)). |
| 2 | Isolated drop at zero pressure: first impact at √(2h/g) ± 0.02 s; rebound height ratio bounce² ± 0.015 (0.3025 for 0.55); identical replay after Restore. | `CuriousContraptions.tests/PhysicsCalibrationTests.cs@a6c914e:L14-L81` | Carry forward as the "reproducible material-dependent rebound" acceptance. |
| 3 | Dropped on the bench it bounces, never sinks more than 1 mm, settles below 0.05 m/s and never escapes. | `CuriousContraptions.tests/FloorTests.cs@a6c914e:L26-L51` | Carry forward. |
| 4 | Legacy drag scaled by world pressure. | `reference/cpu/MachineWorld.cs@a6c914e:L877-L883` | Carry forward only through EL-125. |

Files consulted: `parts/catalog/ball.tres`, `CuriousContraptions.tests/PhysicsCalibrationTests.cs`, `CuriousContraptions.tests/FloorTests.cs`, `reference/cpu/MachineWorld.cs`.

## 5. Acceptance outline

Point of truth: [element-187](../requirements.md#element-187), [CAT-001](../requirements.md#current-cat-001). Existing Chrome proof: `tools/e2e/engine-core-2a1.test.ts@a6c914e:L50-L160` (single and dual ball, Save/Load).

- **Chrome recipe.** Free Workshop: select Basketball in the palette, click the bench, lift with the move gizmo to two heights in two lanes.
- **Positive.** Equal drop heights give equal, repeatable rebounds near 0.30 of the fall height; the higher drop rebounds proportionally higher.
- **Negative or control.** A Bowling ball dropped beside it from the same height rebounds lower (0.14² ≈ 0.02) — material, not identity, decides; an empty construction Run shows no body.
- **Boundaries.** Rests within 1 mm of the bench; speeds above 64 m/s clamp; material other than the declared one is rejected.
- **Run/Reset.** Reset restores identity rotation and exact position; a second Run replays the committed states.
- **Save/Load.** Kind and pose survive save, reload and Load.
- **Integrations.** Contact/cargo connection audit [sequence-task-285](../requirements.md#sequence-task-285) (balls of every type, with ramps, walls, pipes and the basket); interaction rows IX-01 contact impulse ([interaction-01](../requirements.md#interaction-01)) and IX-02 sliding friction ([interaction-02](../requirements.md#interaction-02)); later IX-10 buoyancy ([interaction-10](../requirements.md#interaction-10)) and IX-11 aerodynamic drag ([interaction-11](../requirements.md#interaction-11)) through EL-125. Campaign first use: the "Basketball, bowling, tennis …" row of the [campaign element coverage](../requirements.md#campaign-element-coverage) — first use 1–10; combination and spaced reuse 21–30, 41–50, 91–100, 136–150.
- **Remaining items.** Buoyancy and pressure modes (Story 12.1, [EL-125](EL-125-authored-atmosphere-preset.md)); the CAT-001 R-N3 Fail retained by the requirement row; the material-as-configuration question (Open question 1).

## 6. Open questions

1. CAT-001 lists the material fields as configuration; the engine fixes them per kind. Whether they become authored parameters. Unspecified — owner decision.
2. R-N3 Fail is retained by the requirement row; its resolution owner is not named here.
