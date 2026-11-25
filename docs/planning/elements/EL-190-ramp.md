# EL-190 · Ramp — named-identity readiness spec

**Current delivery (24 October 2026):** [EL-190 delivery slice](../invest/named-elements.md#element-190). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Rewritten on 22 November 2026 as one current text, checked against `elements/Ramp/` and its tests in checkout `8d1d68aa`. Citations without `@` are `path:line` in that checkout; `path@a6c914e` citations appear only in section 4 and are source facts. The delivered declaration is recorded once, in [CAT-054](CAT-054-ramp.md); this spec states what the named identity adds and what remains open.

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name / type | EL-190 · Ramp · Gravity |
| Anchor | [requirements.md#element-190](../requirements.md#element-190) (task `sequence-task-743`); [named-elements entry](../invest/named-elements.md#element-190); scope source [campaign element coverage](../requirements.md#campaign-element-coverage) |
| Owner key / slice | S293; `named-el-190` |
| Refines | [CAT-054 ramp](CAT-054-ramp.md) ([requirement](../requirements.md#current-cat-054)). Neighbours: [EL-191 Wall](EL-191-wall.md), [EL-192 Receiving basket](EL-192-receiving-basket.md) |
| Row and outcome | "Inclined rigid support guides motion through contact and gravity." Outcome: "Wrong-facing slope cannot accelerate cargo uphill." |
| Status | Delivered through package `ramp`; the identity's own outcome has no executed test yet (Disposition). Further work is blocked on the engine programme (owner decision 3a), in particular `ENGINE-SPATIAL-QUERIES`, `ENGINE-SENSOR-VOLUMES` and `ENGINE-ISLANDS-SLEEP`, and runs after it (owner decision 22) |

### Disposition

**Refinement of the delivered package `ramp`** (`elements/Ramp/`, palette order 8, `RampElementDeclaration.cs:22`). No package, catalogue entry or palette order of its own, and no declaration delta. It is not closed yet for two reasons:

1. **Its own outcome is not executed.** No delivered test releases a ball at the low end of a slope and shows it does not climb. The test is planned (row A2); it needs no new capability.
2. **CAT-054 residuals the ledger entry retains:** the misaligned-ramp control with a precise alternative solution, the minimum length, and bounded placement assistance. Assistance is missing; owner [Story 15.0](../../../_bmad-output/planning-artifacts/epics.md#story-150-physical-placement-nudging--difficulty-assistance).

After rows A2 to A4 pass, the identity has no delta and no open row of its own; the ledger entry can then cite `ramp` and those tests, with the assistance row recorded as Story 15.0's.

Nothing is deliverable now: every step is **blocked on the engine programme (owner decision 3a)**. The tests-only rows A2 to A4 run after that programme (owner decision 22, 23 November 2026).

## 2. Declaration

Delivered and unchanged by this identity; every value and line is in [CAT-054 §2](CAT-054-ramp.md#2-declaration). In short: one static body at any 3D pose; three box colliders, the slab (length/2, 0.09, width/2) m and two rails 0.055 m square in section standing 0.0475 m proud, all sized by eight collider bindings; parameters `length` (default 3 m, 0.125 to 4) and `width` (default 1.3 m, 0.125 to 4), build-only and refused outside the range (`elements/Ramp/RampElementDeclaration.cs:27`–`:28`); no port, sensor, store or event.

**Material, as declared data.** Restitution 1, friction 0.3, rolling resistance 0, bounce threshold 0.1 m/s (`RampElementDeclaration.cs:56`), the four fields of `ContactMaterialDeclaration` (`CuriousContraptions.Abstractions/PhysicsDeclarationValues.cs:133`–`:143`). Restitution 1 is the neutral value under the product pair law, so the ball's own bounce decides the rebound. The Ramp declares no `MaterialParameterBinding`: its friction is not player-editable.

### Art and solidity

`Visual/Visual_Body` (slab, collider `col_0_0`) and `Visual/Visual_RailA`, `Visual_RailB` (rails, colliders `col_0_1`, `col_0_2`): all solid, none moved during Run; the two presentation bindings size the art in Build from the same parameters that size the colliders. The hand table agrees (`CuriousContraptions.tests/VisibleSolidColliderTests.cs:86`); `spec-engine-art-solidity.md` lists no finding. At a glance: a thin tan slab (`#c28f52`) with two raised cream rails; the Wall shares the colour but is a tall panel with flush stripes and no rails.

## 3. Engine capabilities

### Capability probe

Probe of 22 November 2026. "Opened" means the test body was read for this spec; the others are cited from CAT-054's probe and confirmed by name.

| Capability | Status | Symbol | Test |
| --- | --- | --- | --- |
| Static box against a sphere at any pose; gravity acts on the ball only | present | `WorkshopCollisionKernels.CollideBoxSphere` (`CuriousContraptions.Simulation/WorkshopCollisionKernels.cs:349`) | `WorkshopPhysicsEngineTests.DeclaredRampLengthChangesRealDownhillRunoutWhileSideAndDepthMissesDoNotDeflect` (`CuriousContraptions.tests/WorkshopPhysicsEngineTests.cs:17`, opened) |
| Friction and rolling on an inclined face | present | `CuriousContraptions.Simulation/WorkshopFrictionSolver.cs`; rolling resistance at `CuriousContraptions.Simulation/WorkshopConstraintSolver.cs:231` | `VisibleSolidColliderTests.ABallRolledAtTheDelayHousingIsStoppedAtItsFace` (`CuriousContraptions.tests/VisibleSolidColliderTests.cs:1086`, opened): a Basketball released on a `ramp` tilted 5° rolls down the slab and stops within 5 mm of the predicted point. Sliding impact on an inclined face is a separate law test, `WorkshopPhysicsLawRestitutionTests.ABallOnAnInclinedFaceLeavesWithTheCoulombSlidingReflection` |
| A declared parameter sizes and offsets colliders | present | `ColliderParameterBinding` (applied at `CuriousContraptions.Bundle/BundleGeometry.cs:28`) | `VisibleSolidColliderTests.AFastBallCrossingARailAtEitherWidthBoundGoesOverItNeverThrough` |
| Parameter-sized art and selection ring | present | `PresentationParameterBinding` (`CuriousContraptions.Abstractions/ElementPresentation.cs:119`) | `WorkshopWallTests.ExistingRampSelectionRingTracksResizeAndCanonicalRestore` |
| Bounded placement assistance before Run | missing | the lesson's `AssistanceProfile` is stored and round-trips (`engine/gpu/WorkshopPuzzle.cs:38`); nothing moves a part | Story 15.0. Its text names CAT-049 and CAT-050 only; Batch A records the gap. It must be a general capability over any placed part, never a ramp rule |

Counts: 4 present, 0 untested, 0 narrower, 1 missing.

## 4. Sources and legacy

- **Requirements.** Row and outcome as in section 1. CAT-054: two-ramp delivery with missing, misaligned and alternative-solution controls; mesh, contact and projections agree.

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| 1 | first_principles: Basketball locked at (−4, 6.5, 0), Receiver at (2.5, 0.9, 0), inventory 2 ramps, captured goal. | `content/puzzles.json@a6c914e:L2-L275`; `engine/gpu/WorkshopPuzzle.cs@a6c914e:L119-L134` | Current: the Playable lesson of row A1. |
| 2 | Ramp used in 36 legacy placements across delivery lessons. | `content/puzzles.json@a6c914e:L8205-L8673` (`two_deliveries`), `content/puzzles.json@a6c914e:L13930-L14460` (`deep_routes`) | Lesson references for Early level proof and Epic 15. |
| 3 | Legacy resize took a free float vector and converted to half-precision. | `parts/RampPart.cs@a6c914e:L27-L31` | Not carried: two typed f32 parameters. |

Files consulted: `content/puzzles.json`, `parts/RampPart.cs`, `parts/catalog/ramp.tres`.

## 5. Acceptance outline

Point of truth: [element-190](../requirements.md#element-190) and [CAT-054](../requirements.md#current-cat-054). Chrome proof is pending post-catalogue.

### Behaviour and numbers

- **No uphill gain.** The slab is static and applies only contact force, which does no work on a ball that does not penetrate; friction and rolling resistance only remove energy. A ball released at rest at height y₀ therefore has kinetic plus potential energy at most m·g·y₀ and can never be above y₀. Row A2's 0.02 m allowance is 6% of the Basketball's 0.34 m radius: below what a player would see as a climb, and above the solver's contact slop.
- **Arriving uphill (row A2's boundary).** A ball that reaches the low end already moving up the slope at v = 1 m/s can climb, but only by the energy it brings. Rolling without slip it carries 0.7·m·v² (0.5 translation plus 0.2 rotation for a solid sphere), so it rises at most 0.7 × 1² / 9.81 = 0.071 m; launched without spin it carries only 0.5·m·v², 0.5 / 9.81 = 0.051 m, and loses part of that while friction sets it rolling. The row therefore bounds the rise by 0.071 m plus the same 0.02 m allowance, 0.091 m, and asks for at least 0.01 m so the ball is seen to have gone uphill at all. Along the 20° slab 0.071 m of height is 0.071 / sin 20° = 0.21 m of travel, well inside the 3 m slab.
- **Downhill.** In the delivered test the ramp is turned −20° about Z (the quaternion's half-angle is π/18). A sphere rolling without slip accelerates at (5/7)·g·(sin θ − c), with c the ball's rolling resistance 0.035, the form the delivered rail test states for a 5° slope (`VisibleSolidColliderTests.cs:1081`): (5/7) × 9.81 × (0.342 − 0.035) = 2.15 m/s². Over the 1.2 m of slab below the release point of row A2's control that gives √(2 × 2.15 × 1.2) = 2.3 m/s and a descent of 1.2 × sin 20° = 0.41 m, so the control's 0.3 m bound is met with margin. The slope form of rolling resistance was not checked against the solver; the row asserts descent, not this speed.
- **Rails.** By energy a rolling ball needs 0.7·v² = g·h to climb the 0.0475 m rail: v = √(9.81 × 0.0475 / 0.7) = 0.82 m/s. Slower balls are held, faster ones go over, none goes through.
- **Fit with delivered parts.** The rails' inner faces are 1.141 m apart at the default width, which carries all four delivered balls (the widest is the Basketball, 0.70 m at its seam).

### Acceptance rows

| # | Given / When / Then | Test | Control and boundary | State |
| --- | --- | --- | --- | --- |
| A1 | A ball on a tilted ramp runs downhill and ends lower; a longer ramp gives a longer run-out | `WorkshopPhysicsEngineTests.DeclaredRampLengthChangesRealDownhillRunoutWhileSideAndDepthMissesDoNotDeflect` | a ball beside the ramp or behind it in depth moves less than 0.05 m; lengths 1 m and 4 m; 3 s | executed |
| A2 | A ball released at rest at the low end of a slope never gains height | planned `WorkshopPhysicsEngineTests.ABallReleasedAtTheLowEndOfARampNeverGainsHeight` (defined in [CAT-054 §5](CAT-054-ramp.md#5-acceptance-outline): default ramp at (0, 2.5, 0) turned −20°, Basketball 0.002 m above the slab at local (1.2, 0.432, 0); over 5 s its centre never rises more than 0.02 m above release) | control: released at local (−1.2, 0.432, 0) it descends at least 0.3 m. Boundary: the same ball at the same place given 1 m/s up the slope, world velocity (−0.940, 0.342, 0) m/s, rises at least 0.01 m and never more than 0.091 m above release over 5 s (0.7·v²/g = 0.071 m plus 0.02 m), so the slope returns no more height than the arriving ball brought | planned, tests only; blocked on the engine programme (owner decision 3a) unless the owner lets tests-only patches run first |
| A3 | first_principles stays unsolved with one ramp misaligned and solves with an exactly placed alternative | planned `LaunchLessonSolveTests.FirstPrinciplesStaysUnsolvedWithAMisalignedRampAndSolvesWithAPreciseAlternative` (three rows, defined in CAT-054 §5; 10 s stall guard) | the authored pair at precision 1 is the baseline row | planned, tests only |
| A4 | A minimum-length ramp still deflects a ball downhill | planned `WorkshopPhysicsEngineTests.ABallDroppedOnAMinimumLengthRampIsDeflectedDownhillAndADepthMissIsNot` (`length` 0.125 m; travel towards +x above 0.2 m in 3 s) | the same drop with the ramp at z = 2 | planned, tests only |
| A5 | Two ramps deliver the ball and the lesson solves | `LaunchLessonSolveTests.AuthoredLessonSolvesAndOneMissingCausePreventsTheGoal` (`first_principles`) | one ramp removed; 60, 120 and 240 Hz | executed |
| A6 | Dimensions persist per instance; rails hold at both width bounds; parameter refusal | `WorkshopSaveTests.TwoRampInstancesKeepDistinctCanonicalDimensionsThroughSaveAndCompile`; `VisibleSolidColliderTests.AFastBallCrossingARailAtEitherWidthBoundGoesOverItNeverThrough`; `WorkshopSaveRejectionTests.PreValidationAdmitsExactlyWhatTheDomainAdmits` | widths 0.125 m and 4 m | executed |
| A7 | Run, Reset and reload with non-default dimensions | `WorkshopPhysicsEngineTests.NonHalfRampAndReceiverParametersRunCaptureResetAndReloadExactly` | — | executed |
| A8 | Inside the assistance window a placed ramp is corrected by no more than the knot's cap before Run; outside it, and at precision 1, the transform is unchanged | rows planned inside Story 15.0 | no correction during Run | blocked on Story 15.0 |

Wrong-kind connection: the part has no port (`WorkshopWallTests.WallPopulationPuzzleAndForeignPortsReject` shows a port-free part refusing a wire).

### Integration

- **Collaborators, all delivered:** Basketball, Bowling ball, Tennis ball, Baseball (published main), Receiver, Domino, Delay box. Interaction rows [IX-01](../requirements.md#interaction-01), [IX-02](../requirements.md#interaction-02); audit [sequence-task-285](../requirements.md#sequence-task-285).
- **Campaign.** `first_principles` (Playable, level 1); first use 1–10; reuse 21–30, 41–50, 91–100, 136–150.
- **Coverage binding.** The catalogue key `ramp` in `docs/coverage/catalogue-elements.json`. A refinement has no named-package binding of its own.
- **Lesson fixture (S8).** Not needed.

### Delivery

- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), in particular `ENGINE-SPATIAL-QUERIES` (the broadphase the contact rows exercise), `ENGINE-SENSOR-VOLUMES` (row A3 solves through a Receiver's residence sensor) and `ENGINE-ISLANDS-SLEEP` (resting balls); Story 15.0 for row A8 only.

1. Rows A2, A3 and A4: one tests-only patch from CAT-054's residual list; data-only review tier. It runs after the engine programme (owner decision 22, 23 November 2026), against those stories as delivered. If the baseline row of A3 fails, the defect is in the authored content or the Receiver's precise knot and is repaired there; the test is not loosened.
2. Row A8 inside Story 15.0.
3. The ledger entry then cites `ramp` and rows A1 to A7.

Disqualifiers: Story 15.0 choosing a correction during Run or a ramp-specific rule; a change to the rail bindings or bounds.

## 6. Open questions

Resolved:

- **Ramp friction as a material choice.** It stays the declared 0.3 with no parameter (decided now, standing delegation; D13). Reason: no requirement row or lesson asks for a slippery or grippy ramp. A surface coating is [EL-118](EL-118-coating-station.md)'s subject and would act through a general material capability on any body, not through a Ramp parameter.
- **Bounds 0.125 to 4 m** stand (D1, D19); the Wall, up to 8 m, is the long barrier.
- **"Precise alternative"** is an exactly placed second solution at precision 1 (CAT-054 §6).
- **Tests-only patches before the engine programme.** They wait for it (owner decision 22, 23 November 2026): rows A2, A3 and A4 run after the programme.

**Needs the owner.** None.

## Engine conformance (22 November 2026)

Against the owner's addendum of 22 November 2026, `AGENTS.md` (element-free engine gate, Tri-Graph) and `_bmad/custom/element-free-engine-design.md`.

1. **Declaration data only.** One declaration class, one scene, one icon. A search of `engine/`, `CuriousContraptions.Simulation`, `CuriousContraptions.Bundle` and the abstractions projects for the id `ramp` and the class name found no code that names it. No refactor is owed.
2. **Generic laws.** Guides a ball downhill: contact of a dynamic sphere with a static box at the placed pose, with gravity on the ball. Rolls rather than sticks: Coulomb friction and the ball's rolling resistance. Holds a slow ball: contact with the rail boxes. Nothing is "when a ball touches the ramp, do Y".
3. **Three layers.** Colliders: three hidden boxes. Visuals: slab and two rails. Animation: none; the two bindings are build-time sizing.
4. **One-way flow and typing.** Two typed f32 parameters; nothing reads art.
5. **Flat state.** Bodies 1 (static); colliders 3 (boxes); joints, drives, sensors, domain nodes, stores and ports 0; collider bindings 8; presentation bindings 2 (build-time).
6. **Performance.** A static body, never integrated. Three boxes against the Wall's one, because each rail is a visible solid and also holds a slow ball on the slab. A fast ball cannot pass through slab or rail (speculative contact; proven at both width bounds).
7. **Tests are scenarios.** Every row runs the production simulation.

**Verdict: Conforms**; blocked on the engine programme (owner decision 3a). The package is one static body, so island sleeping (`ENGINE-ISLANDS-SLEEP`) changes nothing in it; the balls that rest on it are the bodies that story affects.
