# CAT-014 · bowling (Bowling ball) — declaration readiness spec

## 1. Identity

| Field | Value |
| --- | --- |
| CAT ID / kind | CAT-014 · `bowling` (display name Bowling ball) |
| Requirement | [CAT-014](../requirements.md#current-cat-014) |
| Mapped identities | EL-188 Bowling ball. Related: EL-091 Cannonball (separate heavy sphere, own spec). |
| Roadmap | Story 6.1 (delivered, terminal scoped Pass 9 Oct 2026); Stories 6.1b (drag and rolling resistance), 6.1c (f32 velocity) |
| Status | delivered; remaining items in §5 |
| Levels | placed in power_trip, domino_effect and others; see [CAT-014-I consumers](../invest/current-consumers.md#cat-014-i) |

## 2. Declaration

| Item | Value and source |
| --- | --- |
| Body and shape | The generic `WorkshopBall` with `Kind = BowlingBall`: one dynamic homogeneous sphere, radius 0.28 m `engine/gpu/WorkshopConstruction.cs@a6c914e:L45-L53`, `engine/gpu/WorkshopConstruction.cs@a6c914e:L67-L77`; compiled like every ball `engine/gpu/WorkshopPhysicsCompiler.cs@a6c914e:L32-L42`. |
| Mass and material | 4 kg; bounce 0.14; drag 0.04 1/s; buoyancy 0; friction 0.3; bounce threshold 0.1 m/s; rolling resistance 0.03 `engine/gpu/WorkshopConstruction.cs@a6c914e:L45-L53`. Owner decision 9 Oct 2026: game-scale, smaller than the 0.34 m Basketball. |
| Constraints and joints | none. |
| Sockets and ports | none `engine/gpu/WorkshopConnections.cs@a6c914e:L18-L26`. |
| Sensors and activation | none owned; a named target like every ball. |
| Work and energy stores | none. |
| Parameters | none authorable; material validated bit-exactly per kind `engine/gpu/WorkshopConstruction.cs@a6c914e:L54-L62`. |
| Cosmetic curves and UI bindings | none. |
| Art | Shared `parts/BallPart.cs@a6c914e:L9-L17` reading the declared radius (stripe ring half thickness 0.014); scene `parts/scenes/ball.tscn`. Palette Bowling ball `#45639c` (0.27, 0.39, 0.61) `DESIGN.md@a6c914e:L166-L166`. Icon `ui/WorkshopIcons.cs@a6c914e:L41-L41`. |
| Catalogue and inventory | `parts/catalog/bowling.tres@a6c914e:L8-L27`: id `bowling`, title Bowling ball, category Motion, same typed material bits. |

Current tests: `CuriousContraptions.tests/WorkshopBowlingTests.cs`, `RigidMassPropertiesTests.cs`, `WorkbenchCapacityTests.cs`, `BasketballResourceTests.cs`; Node harness `tools/workshop-rigid-body.test.mjs` (mass-only controls); Chrome `tools/e2e/cat-014.test.ts` (two-lane Domino strike, bench rest/bounce, Receiver capture, Save/Load).

## 3. Engine capabilities

Families from the [element map row](../general-engine-element-map.md): AerodynamicDrag, Buoyancy, ContactImpulse, EnvironmentState, FiniteLedger, GeometryQuery, RigidBodyDynamics, SlidingFriction.

| Exists now | Reference |
| --- | --- |
| RigidBodyDynamics, ContactImpulse, SlidingFriction, rolling resistance, declared drag | `CuriousContraptions.Simulation/wwwroot/worker.js`; Stories 1.1–1.5, 6.1, 6.1b |
| GeometryQuery, FiniteLedger | `engine/gpu/WorkshopPoseRing.cs`, `engine/gpu/PhysicsMotionRead.cs` |

| Missing | Story that builds it |
| --- | --- |
| Buoyancy and atmosphere (declared zero today) | Epic 12 |
| Lever loading integration | Story 10.3 (CAT-034 Impact lever) |
| f32 declarations: the shared `BallMaterial` lanes, pose and the `bowling.tres` `*Bits` fields are still binary16 (`Half`) | Remaining f32 migration ([f32 migration status](../../gpu-f32-physics.md#f32-migration-status)) |

Dependencies: none. Shares CAT-001's ball record; differs only by declared material.

## 4. Legacy harvest

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| 1 | Bowling joins ball and tennis in the isolated drop calibration: first impact at √(2h/g) ± 0.02 s, rebound height ratio bounce² ± 0.015, identical replay after Reset. | `CuriousContraptions.tests/PhysicsCalibrationTests.cs@a6c914e:L15-L67` | carry forward as acceptance facts. |
| 2 | Bowling bounces on the bench, never sinks below the surface by more than 0.001 m and settles at rest (≤ 0.05 m/s). | `CuriousContraptions.tests/FloorTests.cs@a6c914e:L11-L48` | carry forward. |
| 3 | Every ball-family catalogue entry (ball, tennis, bowling, balloon) must carry every ball parameter; removing one rejects configuration instead of defaulting; Reset restores the saved construction. | `CuriousContraptions.tests/RequiredPhysicsParameterTests.cs@a6c914e:L23-L80` | carry forward the "no silent default" rule (current: bit-exact resource capture). Do not carry forward the string-keyed parameter dictionary. |
| 4 | The legacy ball script used one shared parameter set (Radius, Mass, Bounce, Buoyancy, Drag) for every ball kind. | `reference/cpu/BallPart.cs@a6c914e:L4-L21` | carry forward the shared field set; Bowling must not inherit Basketball defaults (requirement). |
| 5 | In the legacy domino module the Bowling ball is the striker at (−2.4, 3, 0) above the first of four dominoes spaced 0.8 m. | `tools/Campaign/Program.cs@a6c914e:L21-L32` | carry forward as level data (shared with CAT-023; content is in `content/puzzles.json` domino_effect). |
| 6 | Bowling is the falling trigger for switch-gated modules: at (−5.8, 2.4, 0) above a switch at (−5.8, 1.6, 0), and at (−2, 5, 0) above the switch in switched_motor. | `tools/Campaign/Program.cs@a6c914e:L64-L91`; `tools/Campaign/Program.cs@a6c914e:L133-L149` | carry forward as level data (integrations with CAT-063). |

Files harvested:
- `CuriousContraptions.tests/PhysicsCalibrationTests.cs`
- `CuriousContraptions.tests/FloorTests.cs`
- `CuriousContraptions.tests/RequiredPhysicsParameterTests.cs`
- `reference/cpu/BallPart.cs`
- `tools/Campaign/Program.cs`

## 5. Acceptance outline

Acceptance: [CAT-014](../requirements.md#current-cat-014) and its delivery note.

- **Construction.** Free workshop → place Bowling ball and a matched Basketball over identical stock Dominoes → Run (proven by `tools/e2e/cat-014.test.ts`).
- **Positive.** Bowling topples its Domino past 60°; rests at its 0.28 m radius with low rebound; captured by a Receiver.
- **Control.** The identically released Basketball leaves its Domino below 20°; mass-only isolation in the Node harness.
- **Run/Reset, Save/Load.** Both kinds restored; second Run reproduces the lanes.
- **Integrations.** Contact and cargo connection audit [sequence-task-285](../requirements.md#sequence-task-285) (balls of every type); [IX-01 contact impulse](../requirements.md#interaction-01) and [IX-11 aerodynamic drag](../requirements.md#interaction-11); campaign first use 1–10 in the [element coverage ledger](../requirements.md#campaign-element-coverage) (physical presets row). Partners: Domino (CAT-023), Receiver (CAT-004), Impact lever (CAT-034, Story 10.3).
- **Remaining (unmet now).** Lever loading against a matched control (Story 10.3); Domino offset centre of mass is CAT-023's item; buoyancy/atmosphere integrations at Epic 12; power_trip and the other bowling levels are not yet admitted puzzles.

## 6. Open questions

none.
