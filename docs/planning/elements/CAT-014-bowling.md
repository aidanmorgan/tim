# CAT-014 · bowling (Bowling ball) — declaration readiness spec

**Status (22 November 2026, checkout `8d1d68aa`):** delivered as package `bowling` (`elements/BowlingBall/`). Section 2 is the delivered declaration and section 3 the capability probe of that date. Native proof is current; browser qualification is pending until every named element passes. Citations pinned `@a6c914e` in section 4 are source facts, not instructions. No visible-solid defect is open: the art is one plain sphere equal to its collider. Every residual slice (the tests-only matched-Basketball lever control and EL-188's buoyancy row) is blocked on the engine programme (owner decision 3a, 22 November 2026); the part's own engine gaps are owned by `ENGINE-ISLANDS-SLEEP` (a resting ball is still stepped) and `ENGINE-CAPABILITY-INSTANCES` (the payload target filter, audit E12).

## 1. Identity

| Field | Value |
| --- | --- |
| CAT ID / kind | CAT-014 · `bowling` (display name Bowling ball) |
| Requirement | [CAT-014](../requirements.md#current-cat-014) |
| Mapped identities | EL-188 Bowling ball (refines this part; §8). Related: EL-091 Cannonball (separate heavy sphere, own spec). |
| Disposition | **Delivered.** Package id `bowling`, directory `elements/BowlingBall/`. Slice records: [Story 6.1](../../../_bmad-output/implementation-artifacts/spec-6-1-bowling-ball-dynamic-sphere.md), [9A-PKG-1](../../../_bmad-output/implementation-artifacts/review-9a-pkg-1.md), [C-Balls](../../../_bmad-output/implementation-artifacts/review-c-balls.md). |
| Roadmap | Stories 6.1, 6.1b (drag and rolling resistance), 6.1c (f32 velocity); Story 9A.31 |
| Levels | one Playable lesson places it, `burst_of_motion`; `power_trip` and `cold_start` are Planned (`content/puzzles.json`). See [CAT-014-I consumers](../invest/current-consumers.md#cat-014-i). |

## 2. Declaration

Delivered declaration: `elements/BowlingBall/BowlingBallElementDeclaration.cs`; scene `elements/BowlingBall/bowling.tscn`. Interfaces (`BowlingBallElementDeclaration.cs:12`): `IMechanicalElement`, `ISceneGeometryElement`, `IAmbientLoadElement`, `IAirflowReceiverElement` (the same four as the Basketball; symbols cited in [CAT-001 §2](CAT-001-ball.md#2-declaration)). It has the Basketball's declaration shape and differs only in declared values and in having no seam.

| Item | Value and source |
| --- | --- |
| Body | One dynamic body (`KinematicMode.Dynamic`, `:48`; footprint 1 body, 1 moving, no constraints, `:28`). A zero-parameter generic instance. |
| Collider | Baked from the scene (`Colliders => []`, `:36`): one sphere, radius 0.28 m (`bowling.tscn:12`–`13`, node `col_0_0`, `:22`). |
| Mass and moments | 4 kg; centre of mass at the origin; principal moments 0.12544 kg·m² on each axis (0.4 × 4 × 0.28²); identity principal frame; gravity (0, −9.81, 0) m/s² (`:53`–`59`). |
| Material (f32) | restitution 0.14, friction 0.3, rolling resistance 0.03, bounce threshold 0.1 m/s (`:40`). Owner decision 9 October 2026: game scale, smaller than the Basketball. |
| Ambient load | Linear drag 0.04 1/s on body 0, zero world acceleration, no gas inventory (`:61`–`62`): not buoyant. The compiled drag follows the scene's atmosphere preset: 0.04 1/s in Standard, 0 in Vacuum (`BalloonTests.ExistingBallsHaveAmbientDragOnlyInStandardAndRestoreTheirEnvironment`, `CuriousContraptions.tests/BalloonTests.cs:167`, row `bowling`). The mass frame's own `linearDrag` is 0 (`:58`). |
| Airflow receiver | One receiver on collider 0, `AirflowSampling.ColliderNormalized`, four samples at (0, ±0.5, 0) and (0, 0, ±0.5) of the collider, weight 0.25 each (`:32`–`35`). |
| Payload | `PayloadPolicy.CaptureTarget` (`:51`). |
| Parameters | none (`Parameters: []`, `:26`). No editor panel; a record carrying any parameter or material is refused. |
| Ports, events, stores, signal/supply/electrical nodes, anchors | none (`Ports`, `:30`; `Events`, `:44`; `AnchorPoints`, `:64`; `SupportedDomains` is Mechanical only, `:46`). |
| Presentation | Pick radius 0.4 m, `PresentationAxes.None`, no bindings of any kind (`:14`–`15`). |
| Catalogue | id `bowling`, title Bowling ball, category Motion, palette order 10, colour (0.27, 0.39, 0.61) = Bowling ball `#45639c` (`DESIGN.md:225`), icon `res://elements/BowlingBall/icon.svg`, scene `res://elements/BowlingBall/bowling.tscn` (`:17`–`26`). |

At a glance: the only delivered ball with no seam or band, plain blue. The absence of a band separates it from the Basketball and from every banded ball of the presets family; it is also smaller than the Basketball (0.56 m against 0.68 m across).

## 3. Engine capabilities

Probe of 22 November 2026. Each row cites the symbol and an existing test, or the one owning story.

| Capability | Status | Symbol | Test or owner |
| --- | --- | --- | --- |
| Dynamic sphere contact with restitution | present | `WorkshopCollisionKernels.CollideSpherePlane`, `CollideBoxSphere` (`CuriousContraptions.Simulation/WorkshopCollisionKernels.cs:325`, `:349`) | `WorkshopPhysicsEngineTests.DroppedBowlingBallBarelyReboundsAndRestsAtItsOwnRadius` |
| Rolling resistance and ambient drag | present | `CuriousContraptions.Simulation/WorkshopConstraintSolver.cs:231`; `ApplyAmbientLoads` (`CuriousContraptions.Simulation/WorkshopPhysicsEngine.Gas.cs:95`) | `WorkshopBowlingTests.DeclaredDragAndRollingResistanceCompileIntoTheBodyAndMaterialRecordsAndOnlyBallsCarryACoefficient`; `WorkshopPhysicsLawTests.EachBallRollingAt0Point7DeceleratesAtTheRollingLawAndRestsWithoutBackwardTravel` |
| Mass-dependent momentum transfer | present | the shared contact solver (`WorkshopConstraintSolver.cs`) | `WorkshopPhysicsEngineTests.RollingStrikeTopplesTheDominoOnlyWithTheHeavierMass`; `TheTwoLaneSetupSeparatesTheBallKindsAndBothStruckBallsComeToRest` |
| Payload policy read by other parts' sensors | present | `DeclaredPhysics.IsPayload` (`engine/gpu/DeclaredPhysics.cs:144`) | `PayloadInstanceTests.AReceiverCapturesEitherKindAndAShiftedMissNever` |
| Loading a hinged beam by impact | present | `CuriousContraptions.Simulation/RevoluteConstraintSolver.cs` | `ImpactLeverTests.AlignedBowlingBallImpact_FlingsPayloadUpward` |
| Airflow reception at collider samples | present | `GatherAirflowBranches` (`CuriousContraptions.Simulation/WorkshopPhysicsEngine.Airflow.cs:69`) | `FanGuidanceTests.ElectricalConeGuidesFallingTennisToReceiverWithMatchedControls`, row `Control.Heavy`: a Bowling ball in the stream that delivers a Tennis ball is not delivered. The row asserts the outcome, not the work received. |
| Ambient drag scaled by the scene's atmosphere preset; Reset and save restore the preset | present | `ApplyAmbientLoads`; `AtmospherePreset` (`CuriousContraptions.Abstractions/AtmosphereBoundary.cs:6`) | `BalloonTests.ExistingBallsHaveAmbientDragOnlyInStandardAndRestoreTheirEnvironment` (drag 0.04 in Standard, 0 in Vacuum; body drag stays 0) |
| Pressures between the two presets, or a pressure that varies in space | narrower | the atmosphere is one of two scene presets | `ENGINE-FIELD-FORCE` (scene atmosphere; [EL-125](EL-125-authored-atmosphere-preset.md) is the identity that needs it); this part's row does not require it |
| Buoyancy in a liquid | missing | the declared ambient acceleration is zero | [ENGINE-LIQUID](../../../_bmad-output/planning-artifacts/epics.md#story-engine-liquid-conserved-liquid-inventory-head-driven-flow-and-immersion); D2: this ball sinks |

Counts: 7 present, 0 untested, 1 narrower, 1 missing.

### Engine conformance

The package is declaration data and art. It has no script, callback, update loop or state machine, and no engine code names it: the audit's search of `engine/`, the `CuriousContraptions.*` engine projects and `ui/` for the id, class name and lesson ids found no literal and no branch. One refactor is owed by a shared contract the part uses, not by the part: its `PayloadPolicy.CaptureTarget` (`:51`) is the product role that `BodyTargetKind.AllPayloads` builds into the target filter (audit E12); `ENGINE-CAPABILITY-INSTANCES` replaces it with a declared participation mask.

**Compiled counts.** Bodies 1 (dynamic). Colliders 1: 1 sphere. Joints 0. Drives 0. Sensors 0. Airflow receivers 1 (4 samples). Ambient loads 1. Domain nodes 0. Stores 0. Presentation bindings 0. Collider parameter bindings 0. All fixed at compile time.

**Layers.**

| Art node (`bowling.tscn`) | Mesh | Classification | Collider | Moved by |
| --- | --- | --- | --- | --- |
| `Visual/Visual_Sphere` (`:19`) | sphere, radius 0.28 m | solid | `col_0_0`, sphere 0.28 m | body 0 pose |

No presentation binding exists, so nothing is animated. The hand table agrees (`CuriousContraptions.tests/VisibleSolidColliderTests.cs:110`).

**Generic laws.**

| Behaviour | Law | Declared values |
| --- | --- | --- |
| Falls | gravity on a dynamic body | 4 kg; (0, −9.81, 0) m/s² |
| Barely rebounds, then rests | contact with restitution (pair value is the product) and a bounce threshold | 0.14; 0.1 m/s |
| Rolls, slows and stops | Coulomb friction and rolling resistance at each contact | 0.3; 0.03 |
| Slows in flight | ambient linear drag, scaled by the scene's atmosphere preset | 0.04 1/s in Standard; 0 in Vacuum |
| Topples a Domino or tips a beam that a Basketball does not | contact impulse between bodies; the difference is the declared mass | 4 kg; moments 0.12544 kg·m² |
| Is captured or sensed | other parts' declared sensors admit bodies whose payload policy is `CaptureTarget` | the policy |
| Is hardly moved by a stream that carries a light ball | airflow transfer at the receiver's samples, acting on the declared mass | four samples of weight 0.25; 4 kg |

**Collider count.** One, the minimum.

**Performance fitness.** One primitive on one body. Tunnelling is excluded by the engine's speculative sphere contact (`AdvanceSpeculativeSphereContact`, `WorkshopCollisionKernels.cs:1940`) and the committed speed limit of 64 m/s (`docs/gpu-f32-physics.md:370`), proven for both ball kinds at 40 m/s by `PayloadInstanceTests.APayloadDrivenIntoTheBenchAtFortyMetresPerSecondNeverTunnels`. The part has no sensor or region. Engine gap, not an element fault: island sleeping is not implemented (`docs/gpu-f32-physics.md:273` specifies it; see [CAT-001 §3](CAT-001-ball.md#engine-conformance)); a resting body is still stepped. Owned by `ENGINE-ISLANDS-SLEEP`.

## 4. Legacy harvest

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| 1 | Bowling joins ball and tennis in the isolated drop calibration: first impact at √(2h/g) ± 0.02 s, rebound height ratio bounce² ± 0.015, identical replay after Reset. | `CuriousContraptions.tests/PhysicsCalibrationTests.cs@a6c914e:L15-L67` | carry forward as acceptance facts. |
| 2 | Bowling bounces on the bench, never sinks below the surface by more than 0.001 m and settles at rest (≤ 0.05 m/s). | `CuriousContraptions.tests/FloorTests.cs@a6c914e:L11-L48` | carry forward. |
| 3 | Every ball-family catalogue entry (ball, tennis, bowling, balloon) must carry every ball parameter; removing one rejects configuration instead of defaulting; Reset restores the saved construction. | `CuriousContraptions.tests/RequiredPhysicsParameterTests.cs@a6c914e:L23-L80` | the "no silent default" rule is carried: the package declares every value, and a record carrying a material or a parameter is refused. The string-keyed parameter dictionary is not carried. |
| 4 | The legacy ball script used one shared parameter set (Radius, Mass, Bounce, Buoyancy, Drag) for every ball kind. | `reference/cpu/BallPart.cs@a6c914e:L4-L21` | carried as one declaration shape with each kind's values in its own package; the Bowling ball takes no Basketball value (requirement). Buoyancy waits for ENGINE-LIQUID. |
| 5 | In the legacy domino module the Bowling ball is the striker at (−2.4, 3, 0) above the first of four dominoes spaced 0.8 m. | `tools/Campaign/Program.cs@a6c914e:L21-L32` | not carried as level data: the current `domino_effect` in `content/puzzles.json` strikes with a Basketball. The Bowling ball's Domino loading is proven by `RollingStrikeTopplesTheDominoOnlyWithTheHeavierMass`. |
| 6 | Bowling is the falling trigger for switch-gated modules: at (−5.8, 2.4, 0) above a switch at (−5.8, 1.6, 0), and at (−2, 5, 0) above the switch in switched_motor. | `tools/Campaign/Program.cs@a6c914e:L64-L91`; `tools/Campaign/Program.cs@a6c914e:L133-L149` | carry forward as level data for the Planned lessons; no Playable lesson holds it today (integration with CAT-063). |

Files harvested:
- `CuriousContraptions.tests/PhysicsCalibrationTests.cs`
- `CuriousContraptions.tests/FloorTests.cs`
- `CuriousContraptions.tests/RequiredPhysicsParameterTests.cs`
- `reference/cpu/BallPart.cs`
- `tools/Campaign/Program.cs`

## 5. Acceptance outline

Acceptance: [CAT-014](../requirements.md#current-cat-014). Every row runs the production simulation; none depends on a ball-specific code path.

| Given / When / Then | Test (exists unless marked planned) | Control and boundary |
| --- | --- | --- |
| Dropped on the bench it barely rebounds and rests at 0.28 m | `WorkshopPhysicsEngineTests.DroppedBowlingBallBarelyReboundsAndRestsAtItsOwnRadius`; `DroppedFromThreeMetresEachBallReboundsToESquaredHOfItsDeclaredBounce` | the Basketball in the same theory rebounds to its own e²·h |
| It does not inherit Basketball values | `WorkshopBowlingTests.BothBallsCompileAsDynamicSpheresWithTheirDeclaredMaterialsAndSolidInertia`; `PayloadInstanceTests.BothKindsCompileTheOwnerDeclaredMaterialsOnOneBench` | both kinds on one bench |
| It topples a Domino that a matched Basketball leaves standing | `WorkshopPhysicsEngineTests.RollingStrikeTopplesTheDominoOnlyWithTheHeavierMass`; `TheTwoLaneSetupSeparatesTheBallKindsAndBothStruckBallsComeToRest` | mass-only theory rows |
| It loads a lever | `ImpactLeverTests.AlignedBowlingBallImpact_FlingsPayloadUpward` | `DepthMissControl_PayloadRemainsUndisturbed`; `PivotHitControl_PayloadRemainsUndisturbed` |
| Visible size agrees with collision | `BasketballResourceTests.TheBundleAndSceneUseDeclaredNumericMaterials`; `VisibleSolidColliderTests.EverySolidArtPartHasItsColliderAndNoColliderReachesBeyondTheArt` | the mesh radius is the authored collider radius |
| A Receiver captures it | `PayloadInstanceTests.AReceiverCapturesEitherKindAndAShiftedMissNever` | the shifted miss never captures |
| A stream that delivers a light ball does not deliver it | `FanGuidanceTests.ElectricalConeGuidesFallingTennisToReceiverWithMatchedControls` (`Control.Heavy`) | `Control.Supplied`: the Tennis ball is delivered |
| Run/Reset, save/load, parameter refusal | `PayloadInstanceTests.BothKindsFallAgainAfterExactResetAndReload`; `ABareGenericPayloadRecordLoadsAndAnyParameterIsRefused`; `WorkshopBowlingTests.SaveRoundTripsBothKindsAsMaterialFreeRecordsAndRefusesAForgedMaterialOrParameter` | forged material refused |
| Wrong-kind connection | the part has no port; a port-free part refusing a wire is shown by `WorkshopWallTests.WallPopulationPuzzleAndForeignPortsReject` | — |
| Its lesson solves | `LaunchLessonSolveTests.AuthoredLessonSolvesAndOneMissingCausePreventsTheGoal` (`burst_of_motion`, at 60, 120 and 240 Hz) | one missing cause leaves it unsolved |

Residuals:

- **Lever loading against a matched Basketball** (requirement: "Domino/lever loading against a matched Basketball control"). The existing lever test drops a Bowling ball and flings a Basketball; no test drops a Basketball as the striker. Planned `ImpactLeverTests.ABasketballStrikerFlingsThePayloadLowerThanTheBowlingBall`: the scene of `AlignedBowlingBallImpact_FlingsPayloadUpward` (lever at (0, 3, 0), Basketball payload at (1.2, 3.46, 0), striker released at (−1.2, 6, 0)), run once with the 4 kg Bowling ball and once with a 1 kg Basketball as striker, 2 s each. Then the payload's highest point with the Bowling striker exceeds that with the Basketball striker by at least 0.3 m (about one ball radius, so a player sees it). Boundary: two runs with the same striker give highest points within 0.05 m. Both tolerances are chosen from part size, not from a run (audit L3), and are confirmed when the test is written. Tests-only; blocked on the engine programme (owner decision 3a).
- **Buoyancy**: waits for ENGINE-LIQUID (the sinking half of D2's lesson); owned by EL-188's slice.
- Browser proof is pending post-catalogue (`tools/e2e/cat-014.test.ts` is deferred with the browser phase).

Consistency with delivered parts: 0.56 m across is below the Cannon's 0.6 m minimum, so a Bowling ball is Unseated there by geometry (decided in [CAT-016](CAT-016-cannon.md), no identity exception); it passes the pipe bore, 1.3 m across; 4 kg presses the default 0.5 kg Pressure plate. Four balls are delivered: Basketball, Bowling ball, Tennis ball and, on published main, the Baseball (EL-239).

## 6. Open questions

Resolved:

- Named-goal use (EL-188 residual): a goal names a body by identity, and `PayloadInstanceTests.AReceiverCapturesEitherKindAndAShiftedMissNever` captures either kind. No decision is needed.
- The heavy airflow control is the existing `Control.Heavy` row; no further Fan row is planned for this part (no uncovered failure).
- Decided under the standing delegation: no proud band is added. Reason: the banded-ball decision of 21 November 2026 applies to the ball-presets family, and the plain surface is what tells this ball from the others.

**Needs the owner:** nothing.

## 7. Integration

- **Collaborators, all delivered.** Domino, Receiver, Impact lever, Impact switch, Pressure plate, Basketball (control), Bumper, Fan (heavy control), pipe family.
- **Campaign.** `burst_of_motion` (Playable); first use 1–10 in the [element coverage ledger](../requirements.md#campaign-element-coverage) (physical presets row). `power_trip` and `cold_start` are Planned.
- **Coverage binding.** Catalogue key `bowling` in `docs/coverage/catalogue-elements.json`.
- **Lesson fixture.** None needed (S8 not required).

## 8. Delivery

- **Disposition.** Delivered. No slice is owed for the part.
- **Residual work, in order.** Every residual slice is blocked on the engine programme (owner decision 3a, 22 November 2026), which precedes every further element; the part's own gaps are owned by `ENGINE-ISLANDS-SLEEP` and `ENGINE-CAPABILITY-INSTANCES`. Within it: (1) the matched-Basketball lever control: a tests-only patch, data-only review tier. (2) EL-188's buoyancy row, after ENGINE-LIQUID.
- **EL-188 planning check** (delivery-batches 2c: lever loading, pressure modes, named goal body). Lever loading and named-goal capture are delivered and tested; pressure modes are delivered as drag scaled by the Standard or Vacuum preset. EL-188 stays open for the matched lever control and the pressure and buoyancy row; it is not closed as a duplicate before then.
- **Family note.** The ball-presets family (`spec-family-ball-presets.md`) follows this declaration shape. Palette orders 1 (Basketball), 10 (this part) and 68 (Baseball, published main) are taken.
- **Disqualifiers.** A change to the declared mass, radius or material; a Cannon window change that would seat a 0.56 m ball.
