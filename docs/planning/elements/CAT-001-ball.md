# CAT-001 · ball (Basketball) — declaration readiness spec

**Status (22 November 2026, checkout `8d1d68aa`):** delivered as package `ball` (`elements/Ball/`). Section 2 is the delivered declaration and section 3 the capability probe of that date. Native proof is current; browser qualification is pending until every named element passes. Citations pinned `@a6c914e` in section 4 are source facts, not instructions. No visible-solid defect is open: the seam stands 1 cm proud and has its own torus collider. Every residual slice (the tests-only two-height control and EL-187's buoyancy row) is blocked on the engine programme (owner decision 3a, 22 November 2026); the part's own engine gaps are owned by `ENGINE-ISLANDS-SLEEP` (a resting ball is still stepped) and `ENGINE-CAPABILITY-INSTANCES` (the payload target filter, audit E12).

## 1. Identity

| Field | Value |
| --- | --- |
| CAT ID / kind | CAT-001 · `ball` (display name Basketball) |
| Requirement | [CAT-001](../requirements.md#current-cat-001) |
| Mapped identities | EL-187 Basketball (refines this part; its residual is in §8). Separate identities with their own specs: EL-076 Programmable ball, EL-077 Soccer ball, EL-091 Cannonball, EL-098 Pool ball, and EL-239 Baseball, which is delivered on published main (`origin/main:elements/Baseball/`, id `baseball`, palette order 68; its values are the repair's, 0.15 kg and radius 0.26 m with one proud band, coordinator decision 36; published main declares 0.65 kg and 0.25 m until the repair publishes). |
| Disposition | **Delivered.** Package id `ball`, directory `elements/Ball/`. Slice records: [C-Balls](../../../_bmad-output/implementation-artifacts/spec-c-balls.md) ([review](../../../_bmad-output/implementation-artifacts/review-c-balls.md)) for the generic instance; [engine-multi-collider-dynamic](../../../_bmad-output/implementation-artifacts/spec-engine-multi-collider-dynamic.md) for the seam collider. |
| Roadmap | [CAT-001-I](../invest/vertical-delivery.md#ball-drop); Epics 1–2; Story 9A.30 |
| Levels | 14 of the 15 Playable lessons place it (all except `burst_of_motion`; counted from `content/puzzles.json`). See [CAT-001-I consumers](../invest/current-consumers.md#cat-001-i). |

## 2. Declaration

Delivered declaration: `elements/Ball/BallElementDeclaration.cs`; scene `elements/Ball/ball.tscn`. Interfaces (`BallElementDeclaration.cs:9`): `IMechanicalElement` (`CuriousContraptions.Abstractions.Mechanical/MechanicalContracts.cs:7`), `ISceneGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`), `IAmbientLoadElement` (`MechanicalContracts.cs:124`), `IAirflowReceiverElement` (`MechanicalContracts.cs:45`).

| Item | Value and source |
| --- | --- |
| Body | One dynamic body (`KinematicMode.Dynamic`, `:46`; footprint 1 body, 1 moving, no constraints, `:25`). A zero-parameter generic instance. |
| Colliders | Baked from the scene (`Colliders => []`, `:34`). Collider 0: sphere, radius 0.34 m (`ball.tscn:20`–`21`, node `col_0_0`, `:34`). Collider 1: torus, centreline radius 0.336 m, tube radius 0.014 m (inner 0.322, outer 0.35, `ball.tscn:17`–`18`), turned about Z by quaternion (0, 0, −0.3007058, 0.95371695) (node `col_visual_stripe`, `:40`–`43`). Both are on body 0 and share its material. |
| Mass and moments | 1 kg; centre of mass at the origin; principal moments 0.04624 kg·m² on each axis (solid sphere, 0.4 × 1 × 0.34²); identity principal frame; gravity (0, −9.81, 0) m/s² (`:50`–`56`). The seam adds no mass or inertia. |
| Material (f32) | restitution 0.55, friction 0.3, rolling resistance 0.035, bounce threshold 0.1 m/s (`:38`; field order of `MaterialProperties`, `CuriousContraptions.Abstractions/ColliderDescriptor.cs:21`). Owner decision 9 October 2026. |
| Ambient load | Linear drag 0.04 1/s on body 0, zero world acceleration, no gas inventory (`:58`–`59`): not buoyant. The compiled drag follows the scene's atmosphere preset: 0.04 1/s in Standard, 0 in Vacuum (`BalloonTests.ExistingBallsHaveAmbientDragOnlyInStandardAndRestoreTheirEnvironment`, `CuriousContraptions.tests/BalloonTests.cs:171`). The mass frame's own `linearDrag` is 0 (`:55`), so drag is applied once. |
| Airflow receiver | One receiver on collider 0, `AirflowSampling.ColliderNormalized`, four samples at (0, ±0.5, 0) and (0, 0, ±0.5) of the collider, weight 0.25 each (`:30`–`33`). |
| Payload | `PayloadPolicy.CaptureTarget` (`:48`), read by `DeclaredPhysics.IsPayload` (`engine/gpu/DeclaredPhysics.cs:144`). |
| Parameters | none (`Parameters: []`, `:23`). No editor panel; a record carrying any parameter or material is refused. |
| Ports, events, stores, signal/supply/electrical nodes, anchors | none (`Ports`, `:27`; `Events`, `:42`; `AnchorPoints`, `:61`; `SupportedDomains` is Mechanical only, `:44`). |
| Presentation | Pick radius 0.46 m, `PresentationAxes.None`, no bindings of any kind (`:11`–`12`). |
| Catalogue | id `ball`, title Basketball, category Motion, palette order 1, colour (0.96, 0.49, 0.22) = Ball `#f57d38` (`DESIGN.md:224`), icon `res://elements/Ball/icon.svg`, scene `res://elements/Ball/ball.tscn` (`:14`–`23`). |

At a glance: the only ball with one dark tilted seam on orange. It is the largest delivered ball (0.68 m across, 0.70 m at the seam); the Bowling ball (0.56 m) is plain blue with no seam.

## 3. Engine capabilities

Probe of 22 November 2026. Each row cites the symbol and an existing test, or the one owning story.

| Capability | Status | Symbol | Test or owner |
| --- | --- | --- | --- |
| Dynamic sphere against plane, sphere and box, with restitution | present | `WorkshopCollisionKernels.CollideSpherePlane`, `CollideSphereSphere`, `CollideBoxSphere` (`CuriousContraptions.Simulation/WorkshopCollisionKernels.cs:325`, `:335`, `:349`) | `WorkshopPhysicsEngineTests.DroppedBasketballBouncesLowerEachTimeAndRestsOnTheBench`; `DroppedFromThreeMetresEachBallReboundsToESquaredHOfItsDeclaredBounce` |
| Speculative contact for a fast sphere | present | `AdvanceSpeculativeSphereContact` (`WorkshopCollisionKernels.cs:1940`) | `PayloadInstanceTests.APayloadDrivenIntoTheBenchAtFortyMetresPerSecondNeverTunnels` |
| Rolling resistance at each contact | present | `CuriousContraptions.Simulation/WorkshopConstraintSolver.cs:231` | `WorkshopPhysicsLawTests.EachBallRollingAt0Point7DeceleratesAtTheRollingLawAndRestsWithoutBackwardTravel` |
| Declared linear drag as an ambient load | present | `WorkshopPhysicsEngine.ApplyAmbientLoads` (`CuriousContraptions.Simulation/WorkshopPhysicsEngine.Gas.cs:95`) | `WorkshopPhysicsEngineTests.FreeFlightDragChangesOneSecondSpeedWithoutDampingSpin` |
| Second (torus) collider on a dynamic body, mass as declared | present | `WorkshopHollowKernels` (`CuriousContraptions.Simulation/WorkshopHollowKernels.cs:13`) | `BasketballResourceTests.TheBundleAndSceneUseDeclaredNumericMaterials`; `CompoundMassAndRollingTests.BasketballSeamMustClearBeforeRearmingAndStaticPlaneReadsAreExcluded` |
| Payload policy read by other parts' sensors and guides | present | `DeclaredPhysics.IsPayload` (`engine/gpu/DeclaredPhysics.cs:144`) | `PayloadInstanceTests.AReceiverCapturesEitherKindAndAShiftedMissNever` |
| Airflow reception at collider samples | present | `GatherAirflowBranches` (`CuriousContraptions.Simulation/WorkshopPhysicsEngine.Airflow.cs:69`) | `FanGuidanceTests.ExistingPayloadsReceivePaidLateralGuidanceOnlyWithEnabledFlow` |
| Material-free save record; forged material or parameter refused | present | `engine/gpu/WorkshopSavePrevalidation.cs` | `WorkshopBowlingTests.SaveRoundTripsBothKindsAsMaterialFreeRecordsAndRefusesAForgedMaterialOrParameter` |
| Ambient drag scaled by the scene's atmosphere preset; Reset and save restore the preset | present | `ApplyAmbientLoads`; `AtmospherePreset` (`CuriousContraptions.Abstractions/AtmosphereBoundary.cs:6`) | `BalloonTests.ExistingBallsHaveAmbientDragOnlyInStandardAndRestoreTheirEnvironment` (drag 0.04 in Standard, 0 in Vacuum; body drag stays 0) |
| Pressures between the two presets, or a pressure that varies in space | narrower | the atmosphere is one of two scene presets | `ENGINE-FIELD-FORCE` (scene atmosphere; [EL-125](EL-125-authored-atmosphere-preset.md) is the identity that needs it); this part's row does not require it |
| Buoyancy in a liquid | missing | the declared ambient acceleration is zero | [ENGINE-LIQUID](../../../_bmad-output/planning-artifacts/epics.md#story-engine-liquid-conserved-liquid-inventory-head-driven-flow-and-immersion); D2 sets 16 kg/m³ so this ball floats |

Counts: 9 present, 0 untested, 1 narrower, 1 missing.

### Engine conformance

The package is declaration data and art. It has no script, callback, update loop or state machine, and no engine code names it: the audit's search of `engine/`, the `CuriousContraptions.*` engine projects and `ui/` for the id, class name and lesson ids found no literal and no branch. One refactor is owed by a shared contract the part uses, not by the part: its `PayloadPolicy.CaptureTarget` (`:48`) is the product role that `BodyTargetKind.AllPayloads` builds into the target filter (audit E12); `ENGINE-CAPABILITY-INSTANCES` replaces it with a declared participation mask.

**Compiled counts.** Bodies 1 (dynamic). Colliders 2: 1 sphere, 1 torus. Joints 0. Drives 0. Sensors 0. Airflow receivers 1 (4 samples). Ambient loads 1. Domain nodes 0. Stores 0. Presentation bindings 0. Collider parameter bindings 0. All fixed at compile time.

**Layers.**

| Art node (`ball.tscn`) | Mesh | Classification | Collider | Moved by |
| --- | --- | --- | --- | --- |
| `Visual/Visual_Sphere` (`:27`) | sphere, radius 0.34 m | solid | `col_0_0`, sphere 0.34 m | body 0 pose |
| `Visual/Visual_Stripe` (`:30`) | torus, inner 0.322, outer 0.35 m, darker orange (0.576, 0.294, 0.132) | hollow (torus), 1 cm proud | `col_visual_stripe`, the same mesh and pose | body 0 pose |

No presentation binding exists, so nothing is animated; the art only follows the committed body pose. The hand table agrees (`CuriousContraptions.tests/VisibleSolidColliderTests.cs:109`).

**Generic laws.**

| Behaviour | Law | Declared values |
| --- | --- | --- |
| Falls | gravity on a dynamic body | 1 kg; (0, −9.81, 0) m/s² |
| Bounces lower each time, then rests | contact with restitution (pair value is the product) and a bounce threshold | 0.55; 0.1 m/s |
| Rolls, slows and stops | Coulomb friction and rolling resistance at each contact | 0.3; 0.035 |
| Slows in flight | ambient linear drag, scaled by the scene's atmosphere preset | 0.04 1/s in Standard; 0 in Vacuum |
| Passes momentum to what it strikes | contact impulse between bodies | mass 1 kg; moments 0.04624 kg·m² |
| Rides over its seam | contact on the torus collider of the same body | centreline 0.336 m, tube 0.014 m |
| Is captured, sensed or guided | other parts' declared sensors and guides admit bodies whose payload policy is `CaptureTarget` | the policy |
| Is pushed by a stream | airflow transfer at the receiver's samples | four samples of weight 0.25 |

**Collider count above one.** The torus exists because the seam is a visible solid standing 1 cm proud (visible = solid; rolling over a 1 cm band is accepted, owner, 21 November 2026). It is not cosmetic: it changes contact.

**Performance fitness.** Two primitives on one body. Tunnelling is excluded by the engine's speculative sphere contact and the committed speed limit of 64 m/s (`docs/gpu-f32-physics.md:370`), proven at 40 m/s; nothing in the package checks for it. The part has no sensor or region and asks for no per-tick work beyond its own body and contacts. Engine gap, not an element fault: island sleeping is not implemented (`docs/gpu-f32-physics.md:273` specifies it; no sleep state exists under `CuriousContraptions.Simulation` or `engine/gpu`); a resting body is still stepped. Owned by `ENGINE-ISLANDS-SLEEP`.

## 4. Legacy harvest

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| 1 | The legacy ball declared five authorable fields Radius, Mass, Bounce, Buoyancy, Drag; a ball with Buoyancy > 0 drew a short cream tether line below it. | `reference/cpu/BallPart.cs@a6c914e:L4-L28` | Radius, mass, bounce and drag are carried as fixed package values, not authorable fields (C-Balls). Buoyancy waits for ENGINE-LIQUID. The tether art is not carried: this ball is not buoyant. The string-keyed parameter dictionary and the CPU `BodyDynamics.SolidSphere` path are not carried. |
| 2 | An isolated drop from y = 6 at zero pressure reaches first impact at √(2h/g) ± 0.02 s and rebounds to a height ratio of bounce² ± 0.015; samples replay identically after Reset; the ball stays at x = z = 0. | `CuriousContraptions.tests/PhysicsCalibrationTests.cs@a6c914e:L24-L67` | carry forward as acceptance facts (game-grade tolerances); applies to ball, bowling and tennis. |
| 3 | A ball bounces on the visible bench, never penetrates the surface by more than 0.001 m, and settles with rest height within +0.002 m and speed ≤ 0.05 m/s. | `CuriousContraptions.tests/FloorTests.cs@a6c914e:L27-L48` | carry forward. |
| 4 | A 40 m/s downward ball is stopped by the bench and rebounds; Reset restores the start position and initial velocity; the bench is not a saved part. | `CuriousContraptions.tests/FloorTests.cs@a6c914e:L69-L85` | carry forward. |
| 5 | The legacy bench was finite: deck half extents 8.4 × 0.03 × 4.9 m at y −0.49 (surface −0.46), base 8.5 × 0.175 × 5 m at y −0.7. A ball placed at x = 10 or z = 6 falls past it, is hidden and records an Escaped event. | `reference/cpu/Workbench.cs@a6c914e:L6-L12`; `CuriousContraptions.tests/FloorTests.cs@a6c914e:L53-L67` | surface height carried forward (`engine/gpu/WorkshopConstruction.cs:16`, −0.46 m). Finite edges and the Escaped event are not carried (§6). |
| 6 | Depth separates collisions: a ramp at z = −2 does not alter a ball falling at z = 2; positions and velocities match a ball-only run every tick. | `CuriousContraptions.tests/BasicTests.cs@a6c914e:L204-L234` | carry forward as "a depth miss does not deflect" (within a tolerance, not per-tick identity). |
| 7 | A decoy ball captured by the Receiver records a Captured event but does not satisfy a goal naming another ball. | `CuriousContraptions.tests/BasicTests.cs@a6c914e:L236-L258` | carry forward (shared with CAT-004). |
| 8 | Linear drag only removes kinetic energy and replays exactly; invalid drag rates reject. | `CuriousContraptions.tests/BodyDragTests.cs@a6c914e:L12-L48` | the behaviour is carried as the declared ambient load. This part has no drag parameter, so there is no rate to reject. The CPU `BodyDragLoad` midpoint integration is not carried. |
| 9 | The legacy ball accepted a per-instance `radius` property (0.34 and 0.8 used in pipe tests). | `CuriousContraptions.tests/PipeTests.cs@a6c914e:L56-L70` | not carried: this part has no parameter. An oversize payload is the Tennis ball with its `radius` parameter set (D12). |
| 10 | The CAT-001-I r1 candidate is preserved as failed on R-N3 with a later hint regression; its native checks did not exercise the hint semantics. | `reference/CAT-001-I-r1/README.md@a6c914e:L1-L17` | not carried: R-N3 is retired (owner decision 8, 22 November 2026); its definition no longer exists and no test derives from it (§6). |

Files harvested:
- `reference/cpu/BallPart.cs`
- `reference/cpu/Workbench.cs`
- `reference/CAT-001-I-r1/` (README.md; ui/*.cs, TODO.md, csproj and artifacts/*.dll hold no element knowledge)
- `CuriousContraptions.tests/PhysicsCalibrationTests.cs`
- `CuriousContraptions.tests/FloorTests.cs`
- `CuriousContraptions.tests/BasicTests.cs`
- `CuriousContraptions.tests/BodyDragTests.cs`
- `CuriousContraptions.tests/PipeTests.cs`

## 5. Acceptance outline

Acceptance: [CAT-001](../requirements.md#current-cat-001) and [simulation and element acceptance](../requirements.md#accept-simulation). Every row runs the production simulation; none depends on a ball-specific code path.

| Given / When / Then | Test (exists unless marked planned) | Control and boundary |
| --- | --- | --- |
| A Basketball dropped on the bench bounces lower each time and rests at its radius | `WorkshopPhysicsEngineTests.DroppedBasketballBouncesLowerEachTimeAndRestsOnTheBench`; `DroppedFromThreeMetresEachBallReboundsToESquaredHOfItsDeclaredBounce` | the Bowling ball in the same theory; `WorkshopPhysicsLawRestitutionTests.TheBounceThresholdIsStrict` |
| It rolls, slows by the rolling law and stops without reversing | `WorkshopPhysicsLawTests.EachBallRollingAt0Point7DeceleratesAtTheRollingLawAndRestsWithoutBackwardTravel`; `WorkshopPhysicsEngineTests.RollingBallComesToRestAtItsRadiusAndNeverReverses` | `WithZeroDeclaredDragAndCoefficientARollingBallKeepsItsSpeed` |
| Momentum transfers to a Domino or a second ball | `WorkshopPhysicsEngineTests.RollingStrikeTopplesTheDominoOnlyWithTheHeavierMass`; `WorkshopPhysicsLawTests.ARollingBowlingBallPushingARestingBasketballBringsBothToRestTogether` | the lighter ball leaves the Domino standing |
| Rest, and the highest and lowest legal placement | `PayloadInstanceTests.APayloadRestingOnTheBenchStaysWithoutDrift`; `PlacementBoundariesBuildAndCompileAndBeyondIsRefused` | beyond the boundary is refused |
| A 40 m/s impact does not tunnel | `PayloadInstanceTests.APayloadDrivenIntoTheBenchAtFortyMetresPerSecondNeverTunnels` | both ball kinds |
| Run/Reset and save/load restore kind and pose | `PayloadInstanceTests.BothKindsFallAgainAfterExactResetAndReload` | `PreConversionRecordsAreRefusedWithTheTypedSaveRefusal` |
| Parameter refusal | `PayloadInstanceTests.ABareGenericPayloadRecordLoadsAndAnyParameterIsRefused` | the bare record loads |
| Wrong-kind connection | the part has no port; a port-free part refusing a wire is shown by `WorkshopWallTests.WallPopulationPuzzleAndForeignPortsReject` | — |
| The seam is solid and follows the committed pose | `BasketballResourceTests.TheBundleAndSceneUseDeclaredNumericMaterials`; `VisibleSolidColliderTests.EverySolidArtPartHasItsColliderAndNoColliderReachesBeyondTheArt` | a collider with no art fails the same theory |
| Collaborators: Ramp, Receiver, pipe family, Fan | `WorkshopPhysicsEngineTests.DeclaredRampLengthChangesRealDownhillRunoutWhileSideAndDepthMissesDoNotDeflect`; `PayloadInstanceTests.AReceiverCapturesEitherKindAndAShiftedMissNever`; `LaunchLessonSolveTests.AuthoredLessonSolvesAndOneMissingCausePreventsTheGoal` (`clear_pipe`, `gentle_bend`, `quarter_bend`, `joined_pipe`); `FanGuidanceTests.ExistingPayloadsReceivePaidLateralGuidanceOnlyWithEnabledFlow` | one missing cause per lesson; flow disabled |

Residuals:

- **Two-height control** (requirement: "two-height controls"). Planned `PayloadInstanceTests.TwoDropHeightsGiveTheExpectedImpactOrderAndHigherFirstRebound`: two Basketballs released at rest in one Run, centres at (−1, 2, 0) and (1, 4, 0). Then the lower one touches the bench first, and the first apex of the higher one exceeds the other's by at least 0.3 m (about one ball radius, so a player sees it; e²h predicts 0.64 m and 1.25 m above rest). Control: both at y = 2 give apexes within 0.05 m of each other. Stall guard 10 s of game time. Both tolerances are chosen from part size, not from a run (audit L3), and are confirmed when the test is written. Tests-only; blocked on the engine programme (owner decision 3a).
- **Buoyancy** (the requirement lists the field). Waits for ENGINE-LIQUID; the row "a Basketball released under a 16 kg/m³ liquid surface rises and a Bowling ball sinks" is owned by EL-187's slice.
- Browser proof of every row is pending post-catalogue (`9r-11-cat-001-basketball-suite`, backlog).

Consistency with delivered parts: 0.68 m across (0.70 m at the seam) fits the Cannon's 0.6–0.84 m window and the pipe bore, 1.3 m across, and fits the Receiver's 1.32 m sensed interior; 1 kg presses the default 0.5 kg Pressure plate. Four balls are delivered: Basketball, Bowling ball, Tennis ball and, on published main, the Baseball.

## 6. Open questions

Resolved:

- No material field is editable per instance; each kind's material is its package declaration (C-Balls, 22 October 2026).
- The workbench stays an unbounded plane and no Escaped event is added (standing delegation; D15). Reason: no requirement row or Playable lesson uses a bench edge, and a finite bench would add a failure the player cannot see coming.
- Oversize payload: the Tennis ball's `radius` parameter (D12).

- **R-N3 retired (owner decision 8, 22 November 2026).** Its definition was deleted before `a6c914e` and no test can be derived from it. The clause "retain R-N3 Fail until independently resolved" is struck from the CAT-001 row (the `requirements.md` edit belongs to that file's owner); nothing in this spec or in EL-187 waits on it.

**Needs the owner:** nothing.

## 7. Integration

- **Collaborators, all delivered.** Receiver, Ramp, Wall, Domino, Bumper, Springboard, Impact switch, Pipe, both bends, Funnel, Ball detector, Pressure plate, Fan, Impact lever, Cannon, Trampoline, Conveyor.
- **Campaign.** 14 Playable lessons; first use 1–10 in the [element coverage ledger](../requirements.md#campaign-element-coverage) (physical presets row).
- **Coverage binding.** Catalogue key `ball` in `docs/coverage/catalogue-elements.json`. EL-187 is a refinement and gets no package.
- **Lesson fixture.** None needed (S8 not required).

## 8. Delivery

- **Disposition.** Delivered. No slice is owed for the part.
- **Residual work, in order.** Every residual slice is blocked on the engine programme (owner decision 3a, 22 November 2026), which precedes every further element; the part's own gaps are owned by `ENGINE-ISLANDS-SLEEP` and `ENGINE-CAPABILITY-INSTANCES`. Within it: (1) the two-height control: a tests-only patch, data-only review tier. (2) EL-187's buoyancy row, after ENGINE-LIQUID, inside EL-187's slice. (3) Browser suite 9R.11, post-catalogue.
- **EL-187 planning check** (delivery-batches 2c: buoyancy and pressure modes; its R-N3 item is retired by owner decision 8). Its contact outcome ("equal drop height produces reproducible material-dependent rebound") is proven by `DroppedFromThreeMetresEachBallReboundsToESquaredHOfItsDeclaredBounce`. Pressure modes are delivered as drag scaled by the Standard or Vacuum preset. EL-187 stays open for the two-height control and the buoyancy row; it is not closed as a duplicate before both are settled.
- **Disqualifiers.** A change to the declared material, radius or seam geometry (re-run §5); the Baseball repair or the ball-presets family changing a shared payload rule; ENGINE-LIQUID publishing with a density other than D2's.
