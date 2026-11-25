# EL-191 · Wall — named-identity readiness spec

**Current delivery (24 October 2026):** [EL-191 delivery slice](../invest/named-elements.md#element-191). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Rewritten on 22 November 2026 as one current text, checked against `elements/Wall/` and its tests in checkout `8d1d68aa`. Citations without `@` are `path:line` in that checkout; `path@a6c914e` citations appear only in section 4 and are source facts. The delivered declaration is recorded once, in [CAT-066](CAT-066-wall.md); this spec states what the named identity adds and what remains open.

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name / type | EL-191 · Wall · Gravity |
| Anchor | [requirements.md#element-191](../requirements.md#element-191) (task `sequence-task-744`); [named-elements entry](../invest/named-elements.md#element-191); scope source [campaign element coverage](../requirements.md#campaign-element-coverage) |
| Owner key / slice | S298; `named-el-191` |
| Refines | [CAT-066 wall](CAT-066-wall.md) ([requirement](../requirements.md#current-cat-066)). Neighbour: [EL-190 Ramp](EL-190-ramp.md). Related barriers with their own specs: EL-073, EL-074, EL-050, TH-09 |
| Row and outcome | "Rigid barrier blocks bodies through actual transformed geometry." Outcome: "A geometric gap allows passage; invisible silhouette bounds do not block it." |
| Status | Delivered through package `wall`; the gap half of the outcome has no executed test, and light and sound occlusion wait for their engines (Disposition). Further work is blocked on the engine programme (owner decision 3a), in particular `ENGINE-SPATIAL-QUERIES`, and runs after it (owner decision 22) |

### Disposition

**Refinement of the delivered package `wall`** (`elements/Wall/`, palette order 5, `WallElementDeclaration.cs:23`). No package, catalogue entry or palette order of its own, and no declaration delta. It stays open on:

1. **The gap** (its own outcome). Blocking is executed; no delivered test sends a ball just past the wall's edge. Planned, tests only (row A2).
2. **CAT-066 residuals the ledger entry retains:** resize refused during Run and the minimum thickness at the speed limit (tests only), then **light occlusion** (missing; [ENGINE-OPTICAL](../../../_bmad-output/planning-artifacts/epics.md#story-engine-optical-beam-emission-transport-surface-interaction-and-reception)) and **sound occlusion** (missing; [Story 14.0 ENGINE-ACOUSTIC](../../../_bmad-output/planning-artifacts/epics.md#story-140-acoustic-propagation-occlusion--reception-engine-engine-acoustic)).

Its own row (bodies blocked by transformed geometry, a gap passes) closes with row A2; the ledger entry is ticked only when the occlusion rows also pass, because it retains the CAT-066 acceptance.

Nothing is deliverable now: every step is **blocked on the engine programme (owner decision 3a)**. The tests-only rows A2, A6 and A7 run after that programme (owner decision 22, 23 November 2026).

## 2. Declaration

Delivered and unchanged by this identity; every value and line is in [CAT-066 §2](CAT-066-wall.md#2-declaration). In short: one static body at any 3D pose; one box collider with half extents (width/2, height/2, thickness/2) m, sized by three collider bindings; parameters `width` (default 3 m, 0.4 to 8), `height` (default 2 m, 0.4 to 6) and `thickness` (default 0.25 m, 0.12 to 2), build-only and refused outside the range (`elements/Wall/WallElementDeclaration.cs:28`–`:30`); no port, sensor, store or event.

**Material, as declared data.** Restitution 1 (neutral under the product pair law), friction 0.3, rolling resistance 0, bounce threshold 0.1 m/s (`WallElementDeclaration.cs:51`), the four fields of `ContactMaterialDeclaration` (`CuriousContraptions.Abstractions/PhysicsDeclarationValues.cs:133`–`:143`). The Wall declares nothing optical or acoustic, and the material model has no optical, acoustic, thermal, density or strength property today. How a solid blocks light and sound is decided once by ENGINE-OPTICAL and Story 14.0 for every package: either any enabled collider occludes, as it already does for air, or a declared material value does. Neither may be a rule that names the Wall.

### Art and solidity

`Visual/Visual_Body` is the box, drawn at 0.998 of the thickness; collider `col_0_0` is the full box, so the collider is at most 2 mm thicker per face than the art at the 2 m maximum, a stated fit (`CuriousContraptions.tests/VisibleSolidColliderTests.cs:136`, `Fit: .002f`). `Visual_StripeLeft` and `Visual_StripeRight` are printed flat on the faces: decorative, flush, nothing proud. Nothing moves during Run; three bindings size the art in Build. `spec-engine-art-solidity.md` lists no finding and keeps the declared fit (its D4). There is no invisible bound: the pick radius (1.5 m) is a selection aid and takes no part in physics. At a glance: a tall plain tan panel (`#c28f52`) with two flush cream stripes.

## 3. Engine capabilities

### Capability probe

Probe of 22 November 2026. "Opened" means the test body was read for this spec; the others are cited from CAT-066's probe and confirmed by name.

| Capability | Status | Symbol | Test |
| --- | --- | --- | --- |
| Static box contact at any rotation, without tunnelling up to the 64 m/s limit | present | `WorkshopCollisionKernels.CollideBoxSphere`, `AdvanceSpeculativeSphereContact` (`CuriousContraptions.Simulation/WorkshopCollisionKernels.cs:349`, `:1940`) | `WorkshopWallTests.RealSimulationRotatedWallBlocksMovingBallWhileAbsentWallDoesNot` (`CuriousContraptions.tests/WorkshopWallTests.cs:88`, opened): yaw 0°, 45°, 90° at 4 m/s and 45° at 64 m/s |
| Product-law rebound off a static face | present | the shared contact solver (`CuriousContraptions.Simulation/WorkshopConstraintSolver.cs`) | `VisibleSolidColliderTests.ABallDroppedOntoAStaticWallReboundsAtItsOwnRestitution` (`CuriousContraptions.tests/VisibleSolidColliderTests.cs:860`) |
| Three declared parameters size the collider | present | `ColliderParameterBinding` (applied at `CuriousContraptions.Bundle/BundleGeometry.cs:28`) | `WorkshopWallTests.ThreeCanonicalDimensionsPoseAndIdentityRoundTripAtDefaultAndBothLimits` |
| Art and projection follow the parameters and pose | present | `PresentationParameterBinding`, `PresentationSizing` (`CuriousContraptions.Abstractions/ElementPresentation.cs:119`, `:123`) | `WorkshopWallTests.ActualResourceThreeAxesArtworkAndProjectionFollowCanonicalFullPose` |
| Air occlusion by any enabled collider on the sample line | present | `CuriousContraptions.Simulation/WorkshopPhysicsEngine.Airflow.cs:151` | `FanGuidanceTests.ElectricalConeGuidesFallingTennisToReceiverWithMatchedControls` (`CuriousContraptions.tests/FanGuidanceTests.cs:124`, opened), row `Control.SolidWall` |
| Light occlusion | missing | no optical contract exists | ENGINE-OPTICAL, taking its occluder candidates from `ENGINE-SPATIAL-QUERIES` |
| Sound occlusion | missing | no acoustic contract exists | Story 14.0 (ENGINE-ACOUSTIC), taking its occluder candidates from `ENGINE-SPATIAL-QUERIES` |

Counts: 5 present, 0 untested, 0 narrower, 2 missing.

## 4. Sources and legacy

- **Requirements.** Row and outcome as in section 1. CAT-066: limits and invalid dimensions, Cancel and one-gesture Undo, saved properties; occlusion controls distinct.

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| 1 | Catalogue dimensions WidthBits 16896 = 3, HeightBits 16384 = 2, ThicknessBits 13312 = 0.25 (half-precision). | `parts/catalog/wall.tres@a6c914e:L7-L11` | The values are the delivered f32 defaults; half-precision storage is not carried. |
| 2 | Lessons: `wall_return` (a wall returns a ball via a bumper), `wall_and_bumper`, `solar_shadow` (a wall shades a solar panel). | `content/puzzles.json@a6c914e:L2867-L3144`, `content/puzzles.json@a6c914e:L3145-L3423`, `content/puzzles.json@a6c914e:L6226-L6574` | `wall_and_bumper` is Playable; `wall_return` is in the Early level proof list; `solar_shadow` waits for ENGINE-OPTICAL, the Flashlight and the Solar panel. |

Files consulted: `parts/catalog/wall.tres`, `parts/WallPart.cs`, `content/puzzles.json`.

## 5. Acceptance outline

Point of truth: [element-191](../requirements.md#element-191) and [CAT-066](../requirements.md#current-cat-066). Chrome proof is pending post-catalogue.

### Behaviour and numbers

- **Blocking.** In the delivered test a 3 × 6 × 0.25 m wall stands at (0, 3, 0) and a Basketball starts 2 m in front of it, moving along the wall's normal. With the wall its furthest travel along the normal stays in front of the face; without it the ball crosses the plane.
- **The gap (row A2).** The wall's edge is at x = 1.5 m. A ball launched from x = 1.95 m has its seam (0.35 m from its centre) at x = 1.60 m, 0.10 m clear of the edge, and must pass. From x = 1.0 m the ball overlaps the face by 1.5 − (1.0 − 0.35) = 0.85 m and must be blocked. This case shows the box's edge is where blocking stops, but the passing ball is 1.95 m from the wall's centre line, outside the 1.5 m pick radius, so on its own it does not show that a silhouette bound is harmless.
- **Inside the silhouette (row A2's second case).** Turn the same 3 × 6 × 0.25 m wall 45° about Y. Its outward face normal is n = (0.707, 0, 0.707) and the direction along its face is t = (0.707, 0, −0.707); the front face is the plane n·p = 0.125 m. Its axis-aligned bounding box reaches 1.5 × 0.707 + 0.125 × 0.707 = 1.149 m in x and in z. A Basketball is sent along the face on the line n·p = 0.125 + 0.6 = 0.725 m, so its seam (0.35 m) clears the face by 0.25 m: from 0.725·n − 1.5·t = (−0.548, y, 1.573) at 4·t = (2.83, 0, −2.83) m/s for 90 ticks (0.75 s, 3 m), ending near (1.573, y, −0.548). At mid-run its centre is at (0.513, y, 0.513): inside the bounding box by 1.149 − 0.513 − 0.35 = 0.29 m even at its seam, and 0.725 m from the wall's centre line, inside the pick radius. A bound drawn round the silhouette, the bounding box or the pick radius would deflect it; the real box does not touch it.
- **Thin wall at the speed limit.** At 64 m/s a ball travels 64 / 480 = 0.133 m in one 480 Hz substep, more than the 0.12 m minimum thickness, so stopping it depends on the engine's speculative contact; the delivered 64 m/s row uses the 0.25 m default.
- **Fit with delivered parts.** The 0.4 m minimum height exceeds the radius of every delivered ball (largest 0.35 m at the Basketball's seam), so a wall standing on the bench always blocks a rolling ball.

### Acceptance rows

| # | Given / When / Then | Test | Control and boundary | State |
| --- | --- | --- | --- | --- |
| A1 | A rotated wall blocks a moving ball | `WorkshopWallTests.RealSimulationRotatedWallBlocksMovingBallWhileAbsentWallDoesNot` | the same shot with no wall crosses; three yaws; 64 m/s; 90 ticks | executed |
| A2 | A ball passing beside the wall's edge is not blocked, and one inside the edge is; a ball passing 0.6 m in front of a wall turned 45° is not deflected | planned `WorkshopWallTests.ABallPassingBesideTheWallEdgeIsNotBlockedAndOneInsideTheEdgeIs` (defined in [CAT-066 §5](CAT-066-wall.md#5-acceptance-outline): the A1 scene at yaw 0, 4 m/s along +z from z = −2, 90 ticks; from x = 1.95 the furthest z exceeds 0.3), with one added case in the same theory: the wall turned 45° and the ball sent along its face on the line 0.6 m in front of it (geometry above); over 90 ticks its distance from the face plane stays within 0.05 m of 0.725 m and it travels more than 2.8 m along the face | from x = 1.0 the ball is blocked, and the 45° row of A1 blocks a ball aimed at the same face. Boundary: the edge-passing ball's sideways deflection stays under 0.05 m. 0.05 m is a seventh of the ball's radius and a fifth of the 0.25 m clearance, so a graze by any oversized bound fails | planned, tests only; blocked on the engine programme (owner decision 3a) unless the owner lets tests-only patches run first |
| A3 | Dimensions, pose and identity round-trip at the default and both limits; invalid dimensions are refused atomically | `WorkshopWallTests.ThreeCanonicalDimensionsPoseAndIdentityRoundTripAtDefaultAndBothLimits`; `InvalidSubmittedParametersRejectAtomicallyWhileExactF32ValuesRoundTrip` | 0.4 × 0.4 × 0.12 and 8 × 6 × 2 | executed |
| A4 | Reset and save/load restore the wall and its contact | `WorkshopWallTests.RealSimulationResetAndSaveLoadRestoreGenericWallAndItsContact` | — | executed |
| A5 | A wire into a wall is refused | `WorkshopWallTests.WallPopulationPuzzleAndForeignPortsReject` | two walls in free play are admitted | executed |
| A6 | A resize while running or paused is refused and applies after Reset | planned `WorkshopWallTests.AResizeWhileRunningOrPausedIsRefusedAndTheSameEditAppliesAfterReset` (CAT-066 §5) | the edit in Build applies at once | planned, tests only |
| A7 | A minimum-thickness wall stops a ball at 64 m/s | planned `WorkshopWallTests.AMinimumThicknessWallStopsABallAtTheSpeedLimit` (CAT-066 §5: furthest z below −0.2 with the wall, above 0.3 without) | no wall | planned, tests only |
| A8 | A wall across an air stream shelters a ball | `FanGuidanceTests.ElectricalConeGuidesFallingTennisToReceiverWithMatchedControls`, `Control.SolidWall` | `Control.Supplied`: delivered with no wall; 10 s stall guard | executed |
| A9 | A wall between a light source and a panel keeps the consumer off; removing it turns the consumer on | planned inside the first optical element slice after ENGINE-OPTICAL (`solar_shadow` scene: a 3 × 3 × 0.3 wall) | the wall moved out of the beam | blocked on ENGINE-OPTICAL |
| A10 | A wall between a Bell and a Sound meter keeps the meter below threshold | planned inside the first acoustic element slice after Story 14.0 | no wall: above threshold | blocked on Story 14.0 |
| A11 | `wall_and_bumper` solves, and not without the wall | `LaunchLessonSolveTests.AuthoredLessonSolvesAndOneMissingCausePreventsTheGoal` | 60, 120 and 240 Hz | executed |

Tick order for A9 and A10 follows `docs/gpu-f32-physics.md` (single physics tick pipeline): optical and acoustic fields are evaluated in Phase 2, before the mechanical substeps of Phase 3, and receiver readings are read by the electrical solve of the next tick (decision D7). The wall's pose is static, so the order has no visible effect here.

### Integration

- **Collaborators.** Delivered: every ball kind, Bumper, Receiver, Fan, Balloon (a wall is its ceiling). Not delivered: Flashlight (CAT-029), Solar panel (CAT-059), Bell (CAT-009), Sound meter (CAT-060); Bellows (12.4) is in flight.
- **Campaign.** `wall_and_bumper` (Playable); `wall_return`, `solar_shadow` (Planned). First use 1–10. Teaching task [sequence-task-304](../requirements.md#sequence-task-304), [todo-163](../requirements.md#todo-163).
- **Coverage binding.** The catalogue key `wall` in `docs/coverage/catalogue-elements.json`. A refinement has no named-package binding of its own.
- **Lesson fixture (S8).** Not needed.

### Delivery

- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), in particular `ENGINE-SPATIAL-QUERIES` (which replaces the broadphase these rows exercise), `ENGINE-SENSOR-VOLUMES` and `ENGINE-ISLANDS-SLEEP`; ENGINE-OPTICAL and Story 14.0 (both with candidates from `ENGINE-SPATIAL-QUERIES`) for rows A9 and A10 only.

1. Rows A2, A6 and A7: one tests-only patch from CAT-066's residual list; data-only review tier. It runs after the engine programme (owner decision 22, 23 November 2026), against those stories as delivered.
2. Row A9 inside the first optical element slice; row A10 inside the first acoustic element slice.
3. The ledger entry then cites `wall` and rows A1 to A11.

Disqualifiers: either engine story needing Wall-specific code or a second occlusion rule beside its general one; a change to the bounds; removing the declared fit without redrawing the body at full thickness.

## 6. Open questions

Resolved:

- **Limits and invalid dimensions.** A submitted or saved out-of-range dimension is refused (the delivered `Reject` policy, row A3); only an interactive handle drag stops at the bound (CAT-066 §6, standing delegation). One rule for saves, commands and lessons.
- **How the Wall occludes light and sound.** Decided once, element-free, by ENGINE-OPTICAL and Story 14.0 at planning (D16). The Wall blocks; the Acoustic screen (EL-050) attenuates and does not reflect (owner, 21 November 2026).
- **Tests-only patches before the engine programme.** They wait for it (owner decision 22, 23 November 2026): rows A2, A6 and A7 run after the programme.

**Needs the owner.** None.

## Engine conformance (22 November 2026)

Against the owner's addendum of 22 November 2026, `AGENTS.md` (element-free engine gate, Tri-Graph) and `_bmad/custom/element-free-engine-design.md`.

1. **Declaration data only.** One declaration class, one scene, one icon. A search of `engine/`, `CuriousContraptions.Simulation`, `CuriousContraptions.Bundle` and the abstractions projects for the id `wall` and the class name found no code that names the part; the word "wall" appears there only for the wall of a hollow collider and for wall-clock time. No refactor is owed.
2. **Generic laws.** Blocks and deflects: contact of a dynamic body with a static box at the placed pose. Rebound: the ball's own restitution under the product law. Shelters from a stream: airflow occlusion by any enabled collider. Blocks light and sound (planned): the general occlusion rule of each engine story.
3. **Three layers.** Colliders: one hidden box. Visuals: the body and two flat printed stripes. Animation: none; three build-time sizing bindings.
4. **One-way flow and typing.** Three typed f32 parameters; nothing reads art.
5. **Flat state.** Bodies 1 (static); colliders 1 (box); joints, drives, sensors, domain nodes, stores and ports 0; collider bindings 3; presentation bindings 3 (build-time).
6. **Performance.** One static box, the minimum; never integrated. Tunnelling is excluded by the engine's speculative contact and speed limit. The airflow occlusion pass that reads this collider walks every collider per sample line (`WorkshopPhysicsEngine.Airflow.cs:151`); that scene-proportional pass is an engine gap owned by `ENGINE-SPATIAL-QUERIES` in the engine programme (owner decision 3a), and the optical and acoustic occlusion rules take their candidates from the same query interface instead of adding another scan. It is not designed around in the package.
7. **Tests are scenarios.** Every row runs the production simulation.

**Verdict: Conforms**; blocked on the engine programme (owner decision 3a).
