# EL-188 · Bowling ball — named-identity readiness spec

Story 7.0 readiness spec ([spec](../../../_bmad-output/implementation-artifacts/spec-7-0-element-implementation-readiness.md)). Citations are `path@a6c914e:Lstart-Lend`. Every value below is sourced; this identity has no proposed values.

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name / type | EL-188 · Bowling ball · Gravity |
| Anchor | [requirements.md#element-188](../requirements.md#element-188); [named-elements entry](../invest/named-elements.md#element-188); scope source [campaign element coverage](../requirements.md#campaign-element-coverage) |
| Refines | [CAT-014 bowling](CAT-014-bowling.md) ([requirement](../requirements.md#current-cat-014)). Contrasts: [EL-187 Basketball](EL-187-basketball.md), [EL-189 Tennis ball](EL-189-tennis-ball.md). |
| Roadmap story | Delivered: Story 6.1 (0.38 m original), re-tuned to 0.28 m with drag and rolling resistance in Stories 6.1b/6.1c ([epics](../../../_bmad-output/planning-artifacts/epics.md)). Lever loading waits for Story 10.3. |
| Status | delivered; remaining items in §5 |

## 2. Declaration

The CAT-014 configuration lists mass, bounce, radius, buoyancy and drag with no enumerated selector; there is one mode.

- **Bodies and shapes.** One dynamic sphere, the same `WorkshopBall` record with `Kind = BowlingBall` (`engine/gpu/WorkshopConstruction.cs@a6c914e:L67-L77`; `engine/gpu/WorkshopPartKind.cs@a6c914e:L3-L3`), compiled by the same path as every ball (`engine/gpu/WorkshopPhysicsCompiler.cs@a6c914e:L32-L42`). Radius 0.28 m.
- **Mass and material.** `BallMaterial.For(BowlingBall)` (`engine/gpu/WorkshopConstruction.cs@a6c914e:L45-L53`), equal to `parts/catalog/bowling.tres@a6c914e:L8-L17`:

  | Field | Value |
  | --- | --- |
  | Radius | 0.28 m |
  | Mass | 4 kg |
  | Bounce | 0.14 |
  | Drag | 0.04 1/s |
  | Buoyancy | 0 |
  | Friction | 0.3 |
  | Bounce threshold | 0.1 m/s |
  | Rolling resistance | 0.03 |

  Inertia 0.4 m r² (`engine/gpu/RigidMassProperties.cs@a6c914e:L34-L38`). Owner decision 9 Oct 2026: 0.28 m / 4 kg, smaller than the Basketball ([CAT-014](../requirements.md#current-cat-014)).
- **Constraints.** None.
- **Typed ports.** None (`engine/gpu/WorkshopConnections.cs@a6c914e:L24-L24`).
- **Sensors and activation.** None owned. Authored puzzles admit only the named Basketball (`engine/gpu/WorkshopPuzzle.cs@a6c914e:L65-L73`), so the Bowling ball is Free-play only today.
- **Work and energy stores.** None. "Its visual size alone cannot supply added work" ([element-188](../requirements.md#element-188)): work delivered equals its own kinetic and potential energy from its declared 4 kg.
- **Parameters.** None exposed; material fixed per kind (`engine/gpu/WorkshopConstruction.cs@a6c914e:L54-L62`).
- **Cosmetic curves and UI bindings.** None; stripe follows the committed pose.
- **Art.** Shared ball art at the declared radius (`parts/BallPart.cs@a6c914e:L9-L17`). Palette `#45639c` (`DESIGN.md@a6c914e:L166-L166`).
- **Catalogue and inventory.** Id `bowling`, title "Bowling ball", category Motion, description "A heavy, low-bounce ball: four times the Basketball's mass and smaller." (`parts/catalog/bowling.tres@a6c914e:L19-L27`). Last Free palette row (`engine/gpu/WorkshopInventory.cs@a6c914e:L60-L60`).

## 3. Engine capabilities

Families ([element map row](../general-engine-element-map.md), [binding](../../coverage/engine/element-02.json)): ContactImpulse, EnvironmentState, FiniteLedger, GeometryQuery, JointConstraint, RigidBodyDynamics, SlidingFriction, StateTransaction.

- **Exists now.** As EL-187: sphere contact, friction, rolling resistance, drag, gravity (`CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L622-L765`, `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L1271-L1279`).
- **Missing.** Lever loading needs the hinge (Story 10.3); rope loading the rope row (Story 10.2); pressure modes EL-125 / Story 12.1. Multi-ball authored puzzles (`engine/gpu/WorkshopPuzzle.cs@a6c914e:L65-L73`).
- **Dependencies.** Domino (CAT-023) for the delivered momentum control; Impact lever (CAT-034) for the open lever mode.

## 4. Sources and legacy

- **Requirements.** Row: "Heavy spherical cargo loads supports according to actual mass"; outcome "Its visual size alone cannot supply added work" ([element-188](../requirements.md#element-188)). CAT-014: workbench rest/bounce, ball/box momentum transfer and Domino/lever loading against a matched Basketball control.

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| 1 | Material bits: RadiusBits 13435 = 0.28, MassBits 17408 = 4, BounceBits 12411 = 0.14, RollingResistanceBits 10158 = 0.03. | `parts/catalog/bowling.tres@a6c914e:L8-L17` | Carry forward decimal values; binary16 storage does not carry forward. |
| 2 | Drop calibration and bench settle as EL-187, run for bowling too. | `CuriousContraptions.tests/PhysicsCalibrationTests.cs@a6c914e:L14-L81`; `CuriousContraptions.tests/FloorTests.cs@a6c914e:L26-L51` | Carry forward. |
| 3 | Legacy levels used the Bowling ball as the heavy trigger in 30 placements (e.g. `power_trip`, `domino_effect`). | `content/puzzles.json@a6c914e:L276-L494`, `content/puzzles.json@a6c914e:L1129-L1528` | Carry forward as lesson references (content stays after Epic 7). |

Files consulted: `parts/catalog/bowling.tres`, `CuriousContraptions.tests/PhysicsCalibrationTests.cs`, `CuriousContraptions.tests/FloorTests.cs`, `content/puzzles.json`.

## 5. Acceptance outline

Point of truth: [element-188](../requirements.md#element-188), [CAT-014](../requirements.md#current-cat-014). Existing Chrome proof: the four `cat-014` tests — two-lane Domino topple against a matched Basketball, drop/rebound and rest with bit-for-bit Reset replay, Receiver capture, and Save/reload/Load (`tools/e2e/cat-014.test.ts@a6c914e:L187-L316`).

- **Chrome recipe.** Free Workshop: place a Domino in each of two lanes, a Basketball above one and a Bowling ball above the other, lowered with the move gizmo to the same height.
- **Positive.** The Bowling ball topples its Domino; both balls rest on the bench at their radii.
- **Negative or control.** The Basketball lane with identical geometry leaves its Domino standing (or pushes it less); mass-only controls belong in native checks. The Bowling ball rebounds far lower than the Basketball from equal height.
- **Boundaries.** Rest within 1 mm of the bench; only the declared material is admitted.
- **Run/Reset.** Reset restores exact poses of balls and Dominoes.
- **Save/Load.** Kind survives save, reload and Load.
- **Integrations.** Contact/cargo connection audit [sequence-task-285](../requirements.md#sequence-task-285) (balls of every type, domino, ramps/walls, basket, impact lever); interaction rows IX-01 contact impulse ([interaction-01](../requirements.md#interaction-01)) and IX-02 sliding friction ([interaction-02](../requirements.md#interaction-02)); lever loading later uses IX-03 joint constraint ([interaction-03](../requirements.md#interaction-03)). Campaign first use: the "Basketball, bowling, tennis …" row of the [campaign element coverage](../requirements.md#campaign-element-coverage) — first use 1–10; combination and spaced reuse 21–30, 41–50, 91–100, 136–150.
- **Remaining items.** Lever loading (heavier arm wins) after Story 10.3; pressure modes (Story 12.1, EL-125); use as a named goal body in authored puzzles (Open question 2).

## 6. Open questions

1. Whether CAT-014's listed configuration fields become authored parameters. Unspecified — owner decision.
2. Whether authored puzzles admit the Bowling ball as a named goal body (today only the Basketball).
