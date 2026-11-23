# CAT-054 · ramp — declaration readiness spec

**Status (22 November 2026, checkout `8d1d68aa`):** delivered as package `ramp` (`elements/Ramp/`) for manual placement. Section 2 is the delivered declaration and section 3 the capability probe of that date. Native proof is current; browser qualification is pending until every named element passes. Citations pinned `@a6c914e` in section 4 are source facts, not instructions. No visible-solid defect is open: the slab and both rails collide as boxes. Every residual slice (the tests-only patch and Story 15.0's assistance rows) is blocked on the engine programme (owner decision 3a, 22 November 2026); the part needs no specific engine story of its own.

## 1. Identity

| Field | Value |
| --- | --- |
| CAT ID / kind | CAT-054 · `ramp` |
| Requirement | [CAT-054](../requirements.md#current-cat-054) |
| Mapped identities | EL-190 Ramp (refines this part; §8) |
| Disposition | **Delivered.** Package id `ramp`, directory `elements/Ramp/`. Slice records: [C-Ramp+Receiver](../../../_bmad-output/implementation-artifacts/spec-c-ramp-receiver.md) ([review](../../../_bmad-output/implementation-artifacts/review-c-ramp-receiver.md)); rails by [elements-visible-solid](../../../_bmad-output/implementation-artifacts/spec-elements-visible-solid.md). |
| Roadmap | [CAT-054-I · first_principles](../invest/vertical-delivery.md#first-principles); Story 1.4; Story 9A.28; assistance by [Story 15.0](../../../_bmad-output/planning-artifacts/epics.md#story-150-physical-placement-nudging--difficulty-assistance) |
| Levels | one Playable lesson, `first_principles` (inventory of two; `content/puzzles.json`). See [CAT-054-I consumers](../invest/current-consumers.md#cat-054-i). |

## 2. Declaration

Delivered declaration: `elements/Ramp/RampElementDeclaration.cs`; scene `elements/Ramp/ramp.tscn`. Interfaces (`RampElementDeclaration.cs:8`): `IMechanicalElement` (`CuriousContraptions.Abstractions.Mechanical/MechanicalContracts.cs:7`), `ISceneGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`), `IParameterizedGeometryElement` (`SceneGeometry.cs:29`).

| Item | Value and source |
| --- | --- |
| Body | One static body (`KinematicMode.Static`, `:63`; footprint 1 body, 0 moving, no constraints, `:31`), at any full 3D pose. Mass frame zero (`:67`–`73`). |
| Colliders | Three boxes baked from the scene (`Colliders => []`, `:52`; `ramp.tscn:18`–`25`, nodes `col_0_0`, `col_0_1`, `col_0_2` at `:42`, `:48`, `:54`) and sized by eight `ColliderBindings` (`:40`–`50`). Collider 0, the slab: half extents (length/2, 0.09, width/2) m at the origin. Colliders 1 and 2, the rails: half extents (length/2, 0.0275, 0.0275 × width/1.3) m at (0, 0.11, ∓0.598 × width/1.3) m. Slab thickness 0.18 m, rail height 0.055 m and rail height offset 0.11 m do not scale. All share the body's one material. |
| Material (f32) | restitution 1, friction 0.3, rolling resistance 0, bounce threshold 0.1 m/s (`:56`). Restitution 1 is the neutral value under the product pair law: the ball's own restitution decides the bounce. |
| Parameters | `length`: Real, metres, default 3, minimum 0.125, maximum 4 (`:27`). `width`: Real, metres, default 1.3, minimum 0.125, maximum 4 (`:28`). Both take the descriptor defaults `ParameterEditorPolicy.Reject` and `ParameterRunEdit.BuildOnly` (`CuriousContraptions.Abstractions/ParameterDescriptor.cs:60`–`61`): an out-of-range or non-finite value is refused, not clamped, and neither changes during Run. |
| Ports, events, stores, sensors, signal/supply/electrical nodes, anchors | none (`Ports`, `:33`; `Events`, `:59`; `AnchorPoints`, `:75`; `TubeMouthBindings`, `:35`; `SupportedDomains` is Mechanical only, `:61`). `PayloadPolicy.None` (`:65`). |
| Presentation | Pick radius 1.5 m; resize axes `PresentationAxes.XZ` (`:10`). Two parameter bindings on node `Visual`: `ScaleX` = `length` × 1/3, `ScaleZ` = `width` × 1/1.3 (`:11`–`12`). Sizing: dimensions (3, 0.18, 1.3) m, radius scale (0.5, 0, 0), `length` on X and `width` on Z (`:13`–`14`). No event or state binding. |
| Catalogue | id `ramp`, title Ramp, category Structure, palette order 8, colour (0.76, 0.56, 0.32) = Ramp `#c28f52` (`DESIGN.md:228`), icon `res://elements/Ramp/icon.svg`, scene `res://elements/Ramp/ramp.tscn` (`:16`–`29`). |
| Lesson data that names the part | `first_principles` carries per-ramp assistance knots at precision 0 / 0.45 / 1 in `content/puzzles.json` (position step 0.3 / 0.12 / 0 m, rotation step 5° / 2° / 0°, windows 3 m and 100°, then 1.5 m and 45°, blend 0.4 s). The data is stored and round-trips; nothing applies it yet (§3). It is lesson content, not part of the package. |

At a glance: a thin slab with two raised cream rails along its long edges. The Wall shares its colour but is a tall panel with two flush cream stripes across its face and no rails.

## 3. Engine capabilities

Probe of 22 November 2026. Each row cites the symbol and an existing test, or the one owning story.

| Capability | Status | Symbol | Test or owner |
| --- | --- | --- | --- |
| Static box against sphere, at any pose | present | `WorkshopCollisionKernels.CollideBoxSphere` (`CuriousContraptions.Simulation/WorkshopCollisionKernels.cs:349`) | `WorkshopPhysicsEngineTests.DeclaredRampLengthChangesRealDownhillRunoutWhileSideAndDepthMissesDoNotDeflect` |
| Sliding and rolling on an inclined face | present | `CuriousContraptions.Simulation/WorkshopFrictionSolver.cs` | `WorkshopPhysicsLawRestitutionTests.ABallOnAnInclinedFaceLeavesWithTheCoulombSlidingReflection` |
| A declared real parameter sizes and offsets colliders | present | `ColliderParameterBinding`, applied at `CuriousContraptions.Bundle/BundleGeometry.cs:28` | `WorkshopSaveTests.TwoRampInstancesKeepDistinctCanonicalDimensionsThroughSaveAndCompile`; `VisibleSolidColliderTests.AFastBallCrossingARailAtEitherWidthBoundGoesOverItNeverThrough` |
| Parameter-sized art, resize axes and selection ring | present | `PresentationParameterBinding`, `PresentationSizing` (`CuriousContraptions.Abstractions/ElementPresentation.cs:119`, `:123`) | `WorkshopWallTests.ExistingRampSelectionRingTracksResizeAndCanonicalRestore` |
| Bounded placement assistance (a placed part eased towards its authored pose before Run) | missing | the lesson's placement `AssistanceProfile` is stored and round-trips (`engine/gpu/WorkshopPuzzle.cs:38`, `:76`); nothing moves a part | [Story 15.0](../../../_bmad-output/planning-artifacts/epics.md#story-150-physical-placement-nudging--difficulty-assistance). Its text names CAT-049 and CAT-050 only; the gap is recorded in the batch summary (audit G2). |

Counts: 4 present, 0 untested, 0 narrower, 1 missing.

### Engine conformance

The package is declaration data and art. It has no script, callback, update loop or state machine, and no engine code names it: the audit's search of `engine/`, the `CuriousContraptions.*` engine projects and `ui/` for the id, class name and lesson ids found no literal and no branch. No residue is recorded against this spec. Assistance, when Story 15.0 delivers it, must be a general capability over any placed part and its lesson profile, never a ramp rule.

**Compiled counts.** Bodies 1 (static). Colliders 3: 3 boxes. Joints 0. Drives 0. Sensors 0. Domain nodes 0. Stores 0. Presentation bindings 2 (both parameter bindings). Collider parameter bindings 8. All fixed at compile time.

**Layers.**

| Art node (`ramp.tscn`) | Mesh | Classification | Collider | Moved by |
| --- | --- | --- | --- | --- |
| `Visual/Visual_Body` (`:31`) | box 3 × 0.18 × 1.3 m, ramp tan | solid | `col_0_0` | nothing during Run |
| `Visual/Visual_RailA` (`:34`) | box 3 × 0.055 × 0.055 m at (0, 0.11, −0.598), cream (0.976, 0.91, 0.788) | solid, 0.0475 m proud of the slab | `col_0_1` | nothing during Run |
| `Visual/Visual_RailB` (`:38`) | the same at z +0.598 | solid, 0.0475 m proud | `col_0_2` | nothing during Run |

The two presentation bindings size the parent `Visual` node from `length` and `width`, which are `BuildOnly`; the eight collider bindings size the three boxes from the same two parameters by the same factors. This is static sizing in Build, not animation: no art moves relative to its collider, and nothing is driven during Run. The hand table agrees (`CuriousContraptions.tests/VisibleSolidColliderTests.cs:71`).

**Generic laws.**

| Behaviour | Law | Declared values |
| --- | --- | --- |
| A ball lands on it and runs downhill | contact of a dynamic sphere with a static box at the placed pose; gravity acts on the ball | slab half extents (length/2, 0.09, width/2); the pose |
| The ball rolls or slides rather than sticking | Coulomb friction; the ball's rolling resistance | friction 0.3; rolling resistance 0 on the ramp |
| The ball bounces by its own liveliness | restitution, pair value the product | 1 (neutral) |
| A ball crossing the slab is held by a rail, or goes over it | contact with the rail boxes | rail half extents and offsets above |
| A longer or wider ramp behaves as drawn | collider and art sized from one parameter | `length`, `width` |

**Collider count above one.** The Wall uses one box. The Ramp uses three because each rail is a visible solid standing 0.0475 m proud of the slab (visible = solid). The rails also do work in play: they keep a slow ball on the slab.

**Performance fitness.** A static body with three boxes; it never moves and needs no solving of its own. A fast ball cannot pass through the slab or a rail: the engine's speculative sphere contact applies to every pair, and `AFastBallCrossingARailAtEitherWidthBoundGoesOverItNeverThrough` shows a rail holding at both width bounds, where the rail is 0.0053 m thick at the 0.125 m minimum. No sensor, no region, nothing proportional to the scene. No engine gap is specific to this part.

## 4. Legacy harvest

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| 1 | Placement assistance: a ramp placed 0.2 m and 4° from its authored target (window 0.5 m / 10°, cap 0.25 m / 5°, blend 0.4 s) is not snapped at Run start, then moves monotonically and boundedly (≤ 0.01 m per tick) onto the target; Reset restores the placed pose. | `CuriousContraptions.tests/AssistanceTests.cs@a6c914e:L17-L65` | the window, cap and Reset restoration are carried as Story 15.0's acceptance. The in-Run motion is not carried: the requirement forbids a separate numerical trajectory, so the correction is applied to the placed transform before Run. |
| 2 | Strict precision (1), a placement outside the position window (0.6 m) or outside the orientation window (15°) is untouched; an author cap of 0.15 m stops correction at 0.15 m without changing gravity. | `CuriousContraptions.tests/AssistanceTests.cs@a6c914e:L67-L100` | carry forward. |
| 3 | An exact placement reserves its target slot; another ramp at 0.2 m is not pulled onto it; rotation correction takes the short arc (−179° target from +179° moves ≤ 2.1°). | `CuriousContraptions.tests/AssistanceTests.cs@a6c914e:L101-L135` | carry forward. |
| 4 | Compound rotation assistance uses the geodesic angle window and cap (0.2° cap moves exactly 0.2°); the assisted ramp is Kinematic during correction and Static when strict; its centre and angular velocity stay zero; Reset is exact. | `CuriousContraptions.tests/AuthoredOrientationSceneTests.cs@a6c914e:L80-L118` | the geodesic window and cap are carried. The Kinematic state is not carried: the delivered ramp is always a static body. |
| 5 | The difficulty curve interpolates authored knots linearly (0.8 → 0.2 correction gives 0.5 at precision 0.5); an empty curve gives zero correction. | `CuriousContraptions.tests/AssistanceTests.cs@a6c914e:L171-L182` | carry forward (current record: `AssistanceProfile`, `engine/gpu/WorkshopPuzzle.cs:38`). |
| 6 | Authoring defaults per kind: ramp position step 0.30 m, rotation step 5°, window 3 m / 100° at precision 0, halved/45° at 0.45; blend 0.4 s. | `tools/Campaign/Program.cs@a6c914e:L540-L563` | the values are carried in `content/puzzles.json`; the string `part.Kind` switch is not carried. |
| 7 | first_principles reference solution: ramps at (−3.4, 4.3, 0) and (−0.7, 2.7, 0), both rotated −20° about Z. | `reference/p054-guide/Program.cs@a6c914e:L24-L32`; `content/puzzles.json@a6c914e:L2-L275` | carried: the current `content/puzzles.json` holds the same two poses. |
| 8 | Ramp catalogue parameters are required; removing one rejects rather than defaulting. | `CuriousContraptions.tests/RequiredPhysicsParameterTests.cs@a6c914e:L47-L80` | carry forward the no-default rule; the string dictionary is not carried. |
| 9 | Depth separates collisions: a ramp at z = −2 does not affect a ball at z = 2. | `CuriousContraptions.tests/BasicTests.cs@a6c914e:L204-L234` | carry forward. |
| 10 | Placing a ramp then pressing Escape cancels the move gesture; Ctrl+Z removes the placed ramp; the workshop scene frees cleanly. | `CuriousContraptions.tests/WorkshopLifetimeTests.cs@a6c914e:L55-L78` | carry forward as UI lifecycle acceptance (browser proof, post-catalogue). |

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

Acceptance: [CAT-054](../requirements.md#current-cat-054). Every row runs the production simulation; none depends on a ramp-specific code path.

| Given / When / Then | Test (exists unless marked planned) | Control and boundary |
| --- | --- | --- |
| A ball on a tilted ramp runs downhill, and a longer ramp gives a longer run-out | `WorkshopPhysicsEngineTests.DeclaredRampLengthChangesRealDownhillRunoutWhileSideAndDepthMissesDoNotDeflect` | a ball beside the ramp, or behind it in depth, is not deflected; lengths 1 m and 4 m (the maximum) |
| first_principles: two ramps deliver the ball and the level solves | `LaunchLessonSolveTests.AuthoredLessonSolvesAndOneMissingCausePreventsTheGoal` (`first_principles`, precision 0.45, at 60, 120 and 240 Hz) | one missing ramp leaves it unsolved |
| Two ramps keep their own dimensions through save and compile | `WorkshopSaveTests.TwoRampInstancesKeepDistinctCanonicalDimensionsThroughSaveAndCompile`; `FirstPrinciplesSettingsAndTwoRampsRoundTripAtEveryAuthoredKnot` | every authored knot |
| Non-default dimensions run, capture, Reset and reload | `WorkshopPhysicsEngineTests.NonHalfRampAndReceiverParametersRunCaptureResetAndReloadExactly` | values that are not exact in half precision |
| Rails are solid at both width bounds | `VisibleSolidColliderTests.ABallRollingAcrossTheRampIsContainedByItsRail`; `AFastBallCrossingARailAtEitherWidthBoundGoesOverItNeverThrough` | widths 0.125 m and 4 m |
| Art, selection ring and collision resize together | `WorkshopWallTests.ExistingRampSelectionRingTracksResizeAndCanonicalRestore`; `VisibleSolidColliderTests.EverySolidArtPartHasItsColliderAndNoColliderReachesBeyondTheArt` | restore after resize |
| Parameter and foreign-record refusal | `WorkshopSaveRejectionTests.PreValidationAdmitsExactlyWhatTheDomainAdmits`; `WorkshopSaveTests.FirstPrinciplesRejectsForeignInstancesAndConnections` | — |
| Wrong-kind connection | the part has no port; a port-free part refusing a wire is shown by `WorkshopWallTests.WallPopulationPuzzleAndForeignPortsReject` | — |

Residuals. The first three are one tests-only patch. Every number below is chosen from the authored content and part sizes, not from a run, and is confirmed when the tests are written.

- **Misaligned-ramp control and precise alternative solution** (requirement). The lesson test removes a ramp; it does not misalign one or prove a second solution, and it plays precision 0.45 only. Planned theory `LaunchLessonSolveTests.FirstPrinciplesStaysUnsolvedWithAMisalignedRampAndSolvesWithAPreciseAlternative`, at 120 Hz with the lesson theory's 10 s stall guard. The fixtures are the authored ones: Basketball at (−4, 6.5, 0), Receiver at (2.5, 0.9, 0). The authored ramps are at (−3.4, 4.3, 0) and (−0.7, 2.7, 0), both turned −20° about Z (quaternion (0, 0, −0.17364818, 0.98480775)), `length` 3, `width` 1.3. Rows:
  1. *Misaligned*, precision 0.45: the first ramp is turned +20° about Z (quaternion (0, 0, +0.17364818, 0.98480775)) at its authored position. Its low end now faces away from the Receiver, so the ball runs off towards −x. Then the level is unsolved at the stall guard.
  2. *Authored, precise*, precision 1: the authored poses. Then the level solves. No native test plays this lesson at precision 1 today, so this row is the baseline for row 3.
  3. *Alternative, precise*, precision 1: both ramps moved 0.125 m (two cells) in depth to z = +0.125, rotations unchanged; ball and Receiver stay at z = 0. Then the level solves. Reason for this pair: the slab is uniform in depth, so the ball's path over it is unchanged, and the ball (0.35 m at the seam) still clears the nearer rail's inner face (0.4455 m from the ball's line) by 0.0955 m. The row shows that the goal follows from contact, not from matching the authored slots.

  If row 2 fails, the defect is in the authored content or the Receiver's Precise knot and is repaired there; the test is not loosened. The interpretation of "precise alternative" as an exactly placed second solution at Precise difficulty is decided in §6.
- **Wrong-facing slope** (EL-190 outcome: "cannot accelerate cargo uphill"). Planned `WorkshopPhysicsEngineTests.ABallReleasedAtTheLowEndOfARampNeverGainsHeight`: default ramp at (0, 2.5, 0) turned −20° about Z, as in the length test. A Basketball is released at rest 0.002 m above the slab at local (1.2, 0.432, 0), which is world (1.275, 2.496, 0), near the low end. Over 5 s its centre never rises more than 0.02 m above its release height (6% of the ball's radius, below what a player would see as a climb). Control: released at local (−1.2, 0.432, 0), world (−0.980, 3.316, 0), it descends at least 0.3 m.
- **Minimum length** (requirement: "supported dimensions"). The existing length test is a Fact with fixed lengths 1 m and 4 m, not a theory. Planned `WorkshopPhysicsEngineTests.ABallDroppedOnAMinimumLengthRampIsDeflectedDownhillAndADepthMissIsNot`: ramp with `length` 0.125 and `width` 1.3 at (0, 2.5, 0) turned −20° about Z; a Basketball released at rest at (0, 4, 0), directly above the stub, so it strikes at about 4.6 m/s; 3 s. Then its travel towards +x exceeds 0.2 m (the bound the existing test uses for its short ramp; the reflection predicts about 2 m/s of horizontal speed). Control: the same drop with the ramp at z = 2 moves the ball less than 0.05 m horizontally. A length below 0.125 is refused by the parameter bound.
- **Bounded assistance** (facts 1 to 6): waits for Story 15.0. Rows planned there: inside the window the placed ramp is corrected by no more than the knot's cap and the level then solves; outside the window, and at precision 1, the transform is unchanged; no correction happens during Run.
- **Source fixtures** beyond `first_principles`: none exists in `content/puzzles.json` today; they arrive with the Planned lessons (Early level proof).
- Browser proof is pending post-catalogue (`9r-13-cat-054-ramp-suite` is in review at native scope).

Consistency with delivered parts: at the default width the rails' inner faces are 1.141 m apart, which carries each of the four delivered balls (Basketball 0.70 m at its seam, Bowling ball 0.56 m, Tennis ball 0.50 m, Baseball 0.54 m at its band, the repair values of coordinator decision 36). At the 0.125 m minimum width no ball fits between the rails, which is legal but unhelpful. By energy, a ball rolling across the slab needs about 0.82 m/s to climb the 0.0475 m rail (0.7 v² = g h); slower balls are held, faster ones go over, and none goes through.

## 6. Open questions

Resolved:

- The 4 m maximum and 0.125 m minimum are the delivered bounds and stand (D1, D19). Reason: no lesson places a ramp longer than its 3 m default, and the Wall (up to 8 m), not the Ramp, is the long barrier.
- Assistance mechanism: a correction of the placed transform before Run, never motion during Run (the requirement row; D15 for legacy facts 1 and 4).
- "Precise alternative-solution control" (decided now, standing delegation): an exactly placed second solution at precision 1, namely the authored pair moved 0.125 m in depth. Reason: it is the one alternative whose success follows from the contact law without a trial run, and it proves the point of the control, that solving is physical and not slot matching. A planner who wants a second route through different slopes authors it as level content, not in this part's tests.

**Needs the owner:** nothing.

## 7. Integration

- **Collaborators, all delivered.** Basketball, Bowling ball, Tennis ball, Baseball (published main), Receiver, Domino, Delay box (rolled against in `VisibleSolidColliderTests`).
- **Campaign.** `first_principles` (Playable, level 1); first use 1–10 in the [element coverage ledger](../requirements.md#campaign-element-coverage) (physical presets row: ramp variants).
- **Coverage binding.** Catalogue key `ramp` in `docs/coverage/catalogue-elements.json`.
- **Lesson fixture.** None needed (S8 not required); a lesson that locks a ramp places the delivered package.

## 8. Delivery

- **Disposition.** Delivered. No slice is owed for the part.
- **Residual work, in order.** Every residual slice is blocked on the engine programme (owner decision 3a, 22 November 2026), which precedes every further element; no specific engine story. (1) The three planned tests (misaligned and alternative, wrong-facing, minimum length): one tests-only patch, data-only review tier. (2) Assistance rows inside Story 15.0, after all named elements. (3) Browser suite 9R.13, post-catalogue.
- **EL-190 planning check** (delivery-batches 2c: supported-dimension controls and remaining fixtures). Width is tested at both bounds and length at its maximum; residual (1) adds the minimum length and EL-190's uphill outcome. EL-190 stays open until (1) passes and Story 15.0's assistance rows pass; it gets no package of its own.
- **Disqualifiers.** Story 15.0 choosing a correction during Run (contradicts the requirement) or a ramp-specific rule (element-free gate); a change to the rail bindings or bounds; a family part (EL-058 Size grate, EL-162 Fixed ball diverter) reusing palette order 8.
