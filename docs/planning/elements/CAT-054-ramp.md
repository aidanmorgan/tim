# CAT-054 · ramp — declaration readiness spec

## 1. Identity

| Field | Value |
| --- | --- |
| CAT ID / kind | CAT-054 · `ramp` |
| Requirement | [CAT-054](../requirements.md#current-cat-054) |
| Mapped identities | EL-190 Ramp |
| Roadmap | [CAT-054-I · first_principles](../invest/vertical-delivery.md#first-principles); Story 1.4 (Box–Sphere SAT), Story 2.4 (assistance as force regions) |
| Status | delivered for manual placement; physical placement nudging and remaining levels open (§5) |
| Levels | inventory in first_principles and 17 others; see [CAT-054-I consumers](../invest/current-consumers.md#cat-054-i) |

## 2. Declaration

| Item | Value and source |
| --- | --- |
| Body and shape | One static box: half extents (length/2, 0.09, width/2), identity local pose `engine/gpu/WorkshopPhysicsCompiler.cs@a6c914e:L83-L101`; thickness fixed at 0.18 m `engine/gpu/WorkshopInstances.cs@a6c914e:L22-L31`. Arbitrary full pose (`CanonicalRotation`) `engine/gpu/WorkshopInstances.cs@a6c914e:L33-L43`. |
| Material | restitution 1, bounce threshold 0.1 m/s, friction 0.3, rolling resistance 0 `engine/gpu/WorkshopPhysicsCompiler.cs@a6c914e:L87-L90`. |
| Constraints and joints | none (static). During authored assistance the legacy made the ramp kinematic (§4 fact 4). |
| Sockets and ports | none `engine/gpu/WorkshopConnections.cs@a6c914e:L18-L26`. |
| Sensors and activation | none. |
| Work and energy stores | none. |
| Parameters | length 0.125–4 m (default 3), width 0.125–4 m (default 1.3) `engine/gpu/WorkshopInstances.cs@a6c914e:L22-L31`; resource bits `engine/RampDimensionsResource.cs@a6c914e:L8-L18`. first_principles ramp assistance: maximum position correction 0.3 / 0.12 m, rotation 5° / 2° at precision 0 / 0.45, zero at 1 `engine/gpu/WorkshopPuzzle.cs@a6c914e:L111-L114`. |
| Cosmetic curves and UI bindings | none (`CosmeticCurveDeclaration.None`). Resize axes X and Z (length, width) `parts/RampPart.cs@a6c914e:L13-L13`. |
| Art | `parts/RampPart.cs@a6c914e:L14-L40`: slab length × 0.18 × width in the part colour; two cream `#f9e8c9` rails length × 0.055 × 0.055 at y 0.11, z ±0.46 × width; navy `#364354` studs radius 0.035 at (±0.4 × length, 0.12, 0.38 × width); pick radius length/2; resize scales the visual relative to the catalogue default. Scene `parts/scenes/ramp.tscn`. Palette Ramp `#c28f52` `DESIGN.md@a6c914e:L169-L169`. Icon `ui/WorkshopIcons.cs@a6c914e:L43-L43`. |
| Catalogue and inventory | `parts/catalog/ramp.tres@a6c914e:L8-L21`: id `ramp`, title Ramp, category Structure, default dimensions 3 × 1.3. |

Current tests: `CuriousContraptions.tests/WorkshopSimulationTests.cs`, `WorkshopSaveTests.cs`, `WorkshopWallTests.cs` (ramp selection ring follows resize), `WorkbenchCapacityTests.cs`; Chrome `tools/e2e/engine-core-2a3.test.ts`, `engine-core-2b1.test.ts`, `engine-core-2b2.test.ts`, `engine-core-2b3.test.ts`, `engine-core-2b4.test.ts`.

## 3. Engine capabilities

Families from the [element map row](../general-engine-element-map.md): ContactImpulse, EnvironmentState, FiniteLedger, GeometryQuery, RigidBodyDynamics, SlidingFriction.

| Exists now | Reference |
| --- | --- |
| Static box contact with sphere and box, rolling | Story 1.4; `CuriousContraptions.Simulation/wwwroot/worker.js` |
| GeometryQuery (dashed projections, placement) | `ui/PlacementShadows.cs`, `ui/RotationGizmo.cs` |

| Missing | Story that builds it |
| --- | --- |
| Physical placement nudging (prescribed kinematic correction of the ramp with contact) | not scheduled; the roadmap reports it unsupported in the current playable mode ([first_principles](../invest/vertical-delivery.md#first-principles)) |
| Ramp width/length resize handles in the playable UI (X/Z) | already declared; Chrome proof of supported dimensions still required |
| f32 declarations: `RampDimensions`, the fixed thickness, pose, the assistance knots and the `RampDimensionsResource` `*Bits` fields are still binary16 (`Half`) | Remaining f32 migration ([f32 migration status](../../gpu-f32-physics.md#f32-migration-status)) |

Dependencies: none. Consumers: Basketball, Bowling, Domino, Receiver levels.

## 4. Legacy harvest

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| 1 | Placement assistance: a ramp placed 0.2 m and 4° from its authored target (window 0.5 m / 10°, cap 0.25 m / 5°, blend 0.4 s) is not snapped at Run start, then moves monotonically and boundedly (≤ 0.01 m per tick) onto the target; Reset restores the placed pose. | `CuriousContraptions.tests/AssistanceTests.cs@a6c914e:L17-L65` | carry forward the behaviour as the nudging acceptance; the mechanism must be a declared prescribed motion in the shared solver. |
| 2 | Strict precision (1), a placement outside the position window (0.6 m) or outside the orientation window (15°) is untouched; an author cap of 0.15 m stops correction at 0.15 m without changing gravity. | `CuriousContraptions.tests/AssistanceTests.cs@a6c914e:L67-L100` | carry forward. |
| 3 | An exact placement reserves its target slot; another ramp at 0.2 m is not pulled onto it; rotation correction takes the short arc (−179° target from +179° moves ≤ 2.1°). | `CuriousContraptions.tests/AssistanceTests.cs@a6c914e:L101-L135` | carry forward. |
| 4 | Compound rotation assistance uses the geodesic angle window and cap (0.2° cap moves exactly 0.2°); the assisted ramp is Kinematic during correction and Static when strict; its centre and angular velocity stay zero; Reset is exact. | `CuriousContraptions.tests/AuthoredOrientationSceneTests.cs@a6c914e:L80-L118` | carry forward. |
| 5 | The difficulty curve interpolates authored knots linearly (0.8 → 0.2 correction gives 0.5 at precision 0.5); an empty curve gives zero correction. | `CuriousContraptions.tests/AssistanceTests.cs@a6c914e:L171-L182` | carry forward (current `AssistanceProfile.Evaluate`). |
| 6 | Authoring defaults per kind: ramp position step 0.30 m, rotation step 5°, window 3 m / 100° at precision 0, halved/45° at 0.45; blend 0.4 s. | `tools/Campaign/Program.cs@a6c914e:L540-L563` | carry forward the values; do not carry forward the string `part.Kind` switch. |
| 7 | first_principles reference solution: ramps at (−3.4, 4.3, 0) and (−0.7, 2.7, 0), both rotated −20° about Z. | `reference/p054-guide/Program.cs@a6c914e:L24-L32`; `content/puzzles.json@a6c914e:L2-L275` | carry forward (content survives in `content/puzzles.json`). |
| 8 | Ramp catalogue parameters are required; removing one rejects rather than defaulting. | `CuriousContraptions.tests/RequiredPhysicsParameterTests.cs@a6c914e:L47-L80` | carry forward the no-default rule; do not carry forward the string dictionary. |
| 9 | Depth separates collisions: a ramp at z = −2 does not affect a ball at z = 2. | `CuriousContraptions.tests/BasicTests.cs@a6c914e:L204-L234` | carry forward. |
| 10 | Placing a ramp then pressing Escape cancels the move gesture; Ctrl+Z removes the placed ramp; the workshop scene frees cleanly. | `CuriousContraptions.tests/WorkshopLifetimeTests.cs@a6c914e:L55-L78` | carry forward as UI lifecycle acceptance. |

Files harvested:
- `CuriousContraptions.tests/AssistanceTests.cs`
- `CuriousContraptions.tests/AuthoredOrientationSceneTests.cs`
- `CuriousContraptions.tests/RequiredPhysicsParameterTests.cs`
- `CuriousContraptions.tests/BasicTests.cs`
- `CuriousContraptions.tests/WorkshopLifetimeTests.cs`
- `tools/Campaign/Program.cs`
- `reference/p054-guide/Program.cs`
- `reference/p054-contact-replay/` (replay validator and diagnostic shader; no element knowledge)

## 5. Acceptance outline

Acceptance: [CAT-054](../requirements.md#current-cat-054).

- **Construction.** first_principles: select Ramp from inventory, place and rotate two ramps through the gizmo, Run.
- **Positive.** Two-ramp delivery into the Receiver; Captured goal and Solved once.
- **Negative/control.** Missing ramp, misaligned ramp; a valid precise alternative solution.
- **Boundaries.** Supported length/width endpoints through the X/Z resize handles; mesh, contact and dashed projections agree.
- **Run/Reset, Save/Load.** Exact pose and dimensions; Save/Load restores them.
- **Integrations.** Contact and cargo connection audit [sequence-task-285](../requirements.md#sequence-task-285) (ramps/walls); [IX-01 contact impulse](../requirements.md#interaction-01) and [IX-02 sliding friction](../requirements.md#interaction-02); campaign first use 1–10 in the [element coverage ledger](../requirements.md#campaign-element-coverage) (physical presets row: ramp variants). Partners: Receiver (CAT-004) in first_principles, every ball kind (CAT-001, CAT-014, CAT-064).
- **Remaining (unmet now).** Physical placement nudging (facts 1–4) is unsupported; source fixtures and supported dimensions beyond the default are not yet proven in Chrome; the 17 other ramp levels need their companion parts admitted.

## 6. Open questions

- Legacy length/width bounds before the current 0.125–4 m domain are not recorded at `a6c914e`; whether 4 m is the intended authoring maximum: unspecified — owner decision.
