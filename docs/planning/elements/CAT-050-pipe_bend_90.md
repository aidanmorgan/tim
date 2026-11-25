# CAT-050 · pipe_bend_90 (90° pipe bend) — declaration readiness spec

**Status (22 November 2026).** Delivered: package `elements/PipeBend90`, with true hollow colliders published at `eacf561d` by [`engine-hollow-shapes`](../../../_bmad-output/planning-artifacts/epics.md#story-engine-hollow-shapes). Section 2 is read from the package; section 3 is the capability probe of this date. Owed: two native theory rows (Story 9R.23), rails and double-sided glass (Story 9R.19), light (ENGINE-OPTICAL) and placement assistance (Story 15.0). Every residual slice is blocked on the engine programme (owner decision 3a, 22 November 2026); the part's engine gaps are owned by `ENGINE-OPTICAL` (its clear shell and stubs declare Transmit (1, 1, 1), EL-176 section 10) and `ENGINE-CAPABILITY-INSTANCES` (mouth snapping as declared snap sockets, audit E5). Browser qualification is pending until the post-catalogue phase. Citations ending `@a6c914e` in section 4 describe the legacy source, not current code.

## 1. Identity

| Field | Value |
| --- | --- |
| CAT ID / kind | CAT-050 · `pipe_bend_90` (display name 90° pipe bend) |
| Requirement | [CAT-050](../requirements.md#current-cat-050) |
| Owner key | [CAT-050 delivery owner](../../../_bmad-output/planning-artifacts/epics.md#catalogue-slice-ownership) |
| Disposition | Delivered: package id `pipe_bend_90`, directory `elements/PipeBend90`. A distinct element from the 45° bend; no 45° proof closes it. |
| Mapped identities | EL-072 Curved metal ball pipe (shared with CAT-049) and its 45° sibling EL-242 are separate variants with their own entries (decision D12), not duplicates. Related, own spec: EL-010 90-degree water elbow (liquid). |
| Roadmap | Story 6.10 (delivered; two clause proofs owned by Story 9R.23). |
| Levels | Locked fixture in `joined_pipe`; inventory in `quarter_bend`; see [CAT-050-I consumers](../invest/current-consumers.md#cat-050-i). |
| Mode | The requirement's `TubeAngle=Degrees90` is this package itself (`sweep_degrees = 90`, `elements/PipeBend90/pipe_bend_90.tscn:142`). There is no selector. |

## 2. Declaration

Delivered declaration. `D:` is `elements/PipeBend90/PipeBend90ElementDeclaration.cs`; `S:` is `elements/PipeBend90/pipe_bend_90.tscn`. It differs from the 45° bend (CAT-049) only in sweep, arc centre, mouth poses, chord count, palette order and description.

| Item | Delivered value and source |
| --- | --- |
| Interfaces | `IMechanicalElement`, `ISceneGeometryElement` `D:11`. `SupportedDomains` is Mechanical only `D:40`. |
| Catalogue | Id `pipe_bend_90`, title 90° pipe bend, category Motion, palette order 18, colour (0.4, 0.72, 0.79) `D:16-25`. |
| Body | One static body (`KinematicMode.Static` `D:42`); footprint 1 body, 0 moving, 0 constraints `D:27`; mass 0 and zero moments `D:46-52`; `PayloadPolicy.None` `D:44`; no anchor points `D:54`. |
| Colliders | Baked from hidden scene proxies (the declaration states none), all on the one body. `col_arc`: Torus segment, sweep 90°, centreline radius 2.4 m, tube outer radius 0.7 m, wall 0.05 m (bore radius 0.65 m), arc centre (−1.697056, −1.697056, 0) `S:32-34`, `S:137-143`. `col_stub_start`, `col_stub_end`: Tubes 0.09 m long, outer 0.7 m, wall 0.05 m, from each collar to its mouth `S:22-26`, `S:145-155`. `col_collar_start`, `col_collar_end`: full Torus, inner 0.65 m, outer 0.78 m `S:36-39`, `S:157-164`. |
| Material | Restitution 0.15, friction 0.3, rolling resistance 0, bounce threshold 0.1 m/s `D:33-36`. |
| Parameters, ports, stores, events, domain nodes | None `D:25`, `D:29`, `D:38`. Fixed geometry: no resize axis `D:13`. |
| Tube mouths | Start at (−1.787056, 0.702944, 0) facing −X; End at (0.702944, −1.787056, 0) facing −Y; bore radius 0.65 m each `S:129-135`. The mouth rule is the family's (CAT-048 section 2): 0.09 m beyond the collar, equal bore. |
| Presentation | No binding of any kind `D:13-14`. Pick radius 2.2 m. |
| Art | `Visual/Visual_Shell/Visual_Shell_Seg_0` to `_15`: sixteen open cylinders, radius 0.7 m, 0.26 m long, cyan at alpha 0.16, laid as chords of the arc (sagitta 3.5 mm) `S:12-20`, `S:49-110`. `Visual_Stub_Start`, `Visual_Stub_End`: open cylinders, radius 0.7 m, 0.09 m `S:113-118`. `Visual_Collar_Start`, `Visual_Collar_End`: cream tori 0.65 to 0.78 m `S:121-126`. No rails yet (section 6). Told from the 45° bend by its quarter-circle sweep and its own icon (`elements/PipeBend90/icon.svg`). |

### 2.1 Engine conformance

The package is declaration data and a scene. It has no script, callback or update loop, and a search of `engine/`, the `CuriousContraptions.*` engine projects and `ui/` finds no code that names `pipe_bend_90` or its class (audit of 22 November, section 5; repeated for this repair). One refactor is owed by a shared contract the part uses, not by the part: its two mouths are the tube family's fixed `TubeEnd { Start, End }` recipe with engine snap constants (audit E5); `ENGINE-CAPABILITY-INSTANCES` makes them a declared list of snap sockets.

**Compiled counts.** Bodies 1 (static). Colliders 5: 1 Torus segment, 2 Tube, 2 Torus. Joints 0. Drives 0. Sensors 0. Domain nodes 0. Stores 0. Bindings 0. All counts are fixed at compile time.

**Why five colliders.** Visible = solid, one collider per drawn solid, as for the 45° bend: the arc between the collars, a stub carrying the wall 9 cm to each mouth plane, and a collar standing 0.08 m proud at each end.

| Art node | Classification | Moved by |
| --- | --- | --- |
| `Visual/Visual_Shell/Visual_Shell_Seg_*` | Arc chords of `col_arc` (`CuriousContraptions.tests/VisibleSolidColliderTests.cs:156-157`) | Nothing |
| `Visual/Visual_Shell/Visual_Stub_*` | Hollow; match the stub Tubes | Nothing |
| `Visual/Visual_Collar_Start`, `_End` | Hollow; match the collar tori | Nothing |

| Behaviour | Generic law | Declared values |
| --- | --- | --- |
| A falling ball turns through a right angle | Gravity plus contact normal rows against the Torus segment's inner surface; nothing is declared that adds speed | Centreline 2.4 m, sweep 90°, bore radius 0.65 m |
| The ball slides or rolls on the wall | Pair friction, the geometric mean of the two materials (`CuriousContraptions.Simulation/WorkshopConstraintSolver.cs:119`) | Friction 0.3; rolling resistance 0 |
| A rim or sidewall strike barely rebounds | Pair restitution, the product of the two materials (`WorkshopConstraintSolver.cs:117`) | Restitution 0.15; threshold 0.1 m/s |
| A payload wider than the bore stops at the mouth | Contact against the stub rim and the collar Torus | Bore radius 0.65 m |
| Tubes join | Authoring-time mouth query over declared mouths; it writes a pose, never a constraint | Mouth poses and bore |
| Air does not cross the shell | Airflow occlusion by solid colliders | The same five colliders |
| Light crosses the shell and stops at the collars | Optical transport with a declared surface response per collider: missing, [ENGINE-OPTICAL](../../../_bmad-output/planning-artifacts/epics.md#story-engine-optical-beam-emission-transport-surface-interaction-and-reception) | The shell and both stubs declare Transmit (1, 1, 1), a criterion of ENGINE-OPTICAL (EL-176 section 10, item 6); the collars keep the default Absorb |

**Performance fitness.** The body is static with no sensor or field region. Five primitive hollow colliders replace the deleted octagonal box ring. Tunnelling through the 5 cm wall is excluded by the engine's general means, speculative contacts at 480 Hz substeps (`docs/gpu-f32-physics.md:299-306`; envelope `:375`). Nothing costs work in proportion to the scene. Engine gap: optical response (ENGINE-OPTICAL).

## 3. Engine capabilities

Families from the [element map row](../general-engine-element-map.md): ContactImpulse, EnvironmentState, FiniteLedger, GeometryQuery, RigidBodyDynamics, SlidingFriction.

Capability probe, 22 November 2026. Tests are in `CuriousContraptions.tests/`. Counts: 5 present, 2 present but untested, 0 narrower, 2 missing.

| Capability | Status | Evidence or owner |
| --- | --- | --- |
| Torus-segment wall, Tube stubs and Torus collars: a ball rides the outer wall; a fitting payload passes and an oversized one stops; a rim strike returns no more than the pair restitution | present | `PipeBend90Tests.PipeBend90Scene_ExtractsItsTorusArcStubsCollarsAndTwoMouthPortLocators`; `HollowElementTests.ABallDroppedIntoABendRidesItsOuterWallAndLeavesAtTheDerivedSpeed` (90); `WorkshopPhysicsLawHollowTests.ActualPackageWallsPassAFittingPayloadAndStopAnOversizedPayload` (`Bend90` rows), `.ZeroRestitutionPackageControlDoesNotReturnImpactEnergy` (a drop on this package's collar: the rim and sidewall proof) |
| Typed mouths; straight-to-bend snap with a continuous seam | present | `TubeMouthSnapTests.APipeSnapsOntoTheEndMouthOfEveryTubePart`, `.ABallCrossesASnappedJointWithoutSnagging` (row `StraightToBend90`); `JoinedPipeTests.SnappedAndAuthoredPipesLeaveRoomForTheBallAtBothMouths` |
| Compile, capacity, save and load | present | `PipeBend90Tests.WorkshopPhysicsCompiler_CompilesPipeBend90IntoStaticBodyWithItsHollowColliders`, `.WorkbenchCapacity_AdmitsPipeBendsWithoutColliderLimit`, `.SaveCodec_RoundTripsPipeBend90AndPreservesDeclaredColliders` |
| Level `joined_pipe` played: the snapped pipe delivers, an unsnapped one does not, the locked bend is required, Reset restores | present | `JoinedPipeTests.TheSnappedTwoMetrePipeDeliversTheBallAndResetRestoresTheBench`, `.AnUnsnappedPipeLeavesTheGoalUnsolved`, `.ALockedFixedBendIsRequired`; `LaunchLessonSolveTests.AuthoredLessonSolvesAndOneMissingCausePreventsTheGoal` |
| Level `quarter_bend`: authored content, validation, precision knots, and the level played to its goal with the bend removed as control | present | `PipeBend90Tests.QuarterBend_AuthoredSettingsAndFixturesMatchPuzzlesJson`, `.QuarterBend_ValidateEnforcesFixturesAndInventory`, `.QuarterBend_PrecisionEvaluatesAssistanceAndRoundtripsSaveCodec`; `LaunchLessonSolveTests.AuthoredLessonSolvesAndOneMissingCausePreventsTheGoal` (`quarter_bend` is in `Lessons`; 60, 120 and 240 Hz; Reset; save and load replay) |
| Bend-to-bend passage into this bend | present, untested | The joint theory plays `StraightToBend90` and `Bend45ToBend45` only; planned row in section 5 |
| Air blocked by the shell only when it is in the stream | present, untested | `FanGuidanceTests.TransparentCurvedAndTaperedSolidWallsOccludeOnlyWhenInTheStream` places the 45° bend and the Funnel, not this package; planned rows in section 5 |
| Optical clarity of the shell, opaque collars | missing | [ENGINE-OPTICAL](../../../_bmad-output/planning-artifacts/epics.md#story-engine-optical-beam-emission-transport-surface-interaction-and-reception): Transmit (1, 1, 1) on the shell and stubs is a criterion of that story (EL-176 section 10, item 6) |
| Placement nudging: Forgiving and Balanced succeed where Precise fails | missing | [Story 15.0](../../../_bmad-output/planning-artifacts/epics.md#story-150-physical-placement-nudging--difficulty-assistance) |

Depends on the Clear pipe's bore, collar and mouth rule (CAT-048).

## 4. Legacy harvest

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| 1 | A ball at 6 or 40 m/s crosses a joined seam from a straight pipe or from another 90° bend into a 90° bend rotated (20, 30, 40): each step ≤ speed × tick + 0.002 m, speed ≤ speed + 0.002, exit velocity within cos > 0.8 of the outlet normal. | `CuriousContraptions.tests/PipeBendTests.cs@a6c914e:L41-L85` | carry forward (continuous turned passage, straight/bend joins, high speed). The straight join is delivered; the bend-into-bend join is the planned row of section 5; speed is covered by the family's law tests. |
| 2 | joined_pipe with the straight pipe moved in depth: error 0.48 is won at precision 0 and 0.45, lost at 1; error 0.58 is won at 0, lost at 0.45 and 1; when won the assistance corrected depth to 0, otherwise the error remains (no editor snap during Run). | `CuriousContraptions.tests/PipeBendTests.cs@a6c914e:L87-L114` | carry forward (declared depth-error Forgiving/Balanced success versus Precise failure); owed with Story 15.0. |
| 3 | A length-2 straight pipe at −45° joined to the fixed bend at (1, 3.5, 0), −45°: a ball dropped 1.6 m above the pipe entry reaches y ≤ 1.2 more than 0.3 m past the bend outlet, moving within cos > 0.9 of the outlet normal. | `CuriousContraptions.tests/PipeBendTests.cs@a6c914e:L116-L143` | carry forward (joined_pipe fixture); delivered (`JoinedPipeTests`). |
| 4 | Zero-gravity 6 m/s turn through the 90° bend in three orientations, no added energy, one bend proxy and two collars, exact Reset. | `CuriousContraptions.tests/PipeBendTests.cs@a6c914e:L145-L193` | carry forward; delivered. The collider inventory is the delivered five (section 2). |
| 5 | The 90° bend's End mouth snaps a straight pipe, a 45° bend and another 90° bend within 1e−4 m. | `CuriousContraptions.tests/PipeBendTests.cs@a6c914e:L195-L221` | carry forward; delivered for the pipe; a bend joined to this bend is the planned row of section 5. |
| 6 | Gravity alone carries a ball dropped 1.6 m above the inlet of a bend at (0, 4, 0), −90°, to y ≤ 1.2 at x < −0.5. | `CuriousContraptions.tests/PipeBendTests.cs@a6c914e:L223-L243` | carry forward; delivered. |
| 7 | quarter_bend with the bend 0.48 m off in depth is won at precision 0 and 0.45 (depth corrected to 0) and lost at 1 (no Precise alternative, unlike the 45° bend). | `CuriousContraptions.tests/PipeBendTests.cs@a6c914e:L245-L271` | carry forward; owed with Story 15.0. |
| 8 | Layouts: quarter_bend receiver at (−5.5, 0.6, 0); bend_1 at (0, 4, 0), −90°; ball 1.6 m above the computed inlet. joined_pipe: ball (−1.308, 8.402, 0), fixed bend (1, 3.5, 0) at −45°, receiver (−0.2, 0.6, 0), solution pipe length 2 at (−0.537, 6.031, 0), −45°; "a vertical fall misses the receiver". | `tools/Campaign/Program.cs@a6c914e:L290-L320` | carry forward; delivered in `content/puzzles.json`, with one pose re-authored for the smooth hollow route: joined_pipe's receiver at (0.2, 0.6, 0). |
| 9 | Bend and joined_pipe straight-pipe assistance windows: 0.6 / 0.5 m and 5° / 2° with equal caps. | `tools/Campaign/Program.cs@a6c914e:L564-L573` | carry forward the values (Story 15.0). |
| 10 | Bends are never optically opaque; walls were CPU convex segments within 0.005 m. | `engine/SceneCollisionGeometry.cs@a6c914e:L76-L76`; `engine/physics/HollowGeometry.cs@a6c914e:L109-L146` | carry forward the transparency fact (ENGINE-OPTICAL); do not carry forward the CPU segmentation or its 0.005 m bound. |
| 11 | A 90° bend rotated (20, 35, 10): a ray from 2 m outside the mid-sweep shell blocks air at 0.7–1.4 m but passes light for the full 4 m. | `CuriousContraptions.tests/WindChimeTests.cs@a6c914e:L220-L237` | carry forward. The air control is the planned rows of section 5; light is owed with ENGINE-OPTICAL. |
| 12 | quarter_bend with its authored solution at precision 0 is won: the ball is captured by the Receiver within 1,200 ticks. | `CuriousContraptions.tests/FlightCampaignTests.cs@a6c914e:L15-L40` | carry forward the win; delivered by `LaunchLessonSolveTests`. The 1,200-tick bound is not carried: a lesson deadline is a stall guard (game-fidelity policy). |

Files harvested:
- `parts/PipeBendPart.cs`
- `parts/PipeArt.cs`
- `parts/scenes/pipe_bend_90.tscn`
- `parts/catalog/pipe_bend_90.tres`
- `engine/BendProxy.cs`
- `engine/MachineData.cs` (TubeBendAngle)
- `engine/TubeMouth.cs`
- `engine/physics/HollowGeometry.cs`
- `engine/SceneCollisionGeometry.cs`
- `CuriousContraptions.tests/PipeBendTests.cs`
- `CuriousContraptions.tests/FlightCampaignTests.cs`
- `CuriousContraptions.tests/WindChimeTests.cs` (L220-L237)
- `tools/Campaign/Program.cs`

## 5. Acceptance outline

Acceptance: [CAT-050](../requirements.md#current-cat-050) and [retained behaviour](../requirements.md#todo-250). Every row runs the production simulation or the production authoring query on this declaration; none needs a code path that exists only for this bend, and none is taken from the 45° bend.

| Given / When / Then | Proof | Control and boundary |
| --- | --- | --- |
| Given `quarter_bend` with its authored solution, when Run, then the ball turns through the right angle and is captured | Existing: `LaunchLessonSolveTests.AuthoredLessonSolvesAndOneMissingCausePreventsTheGoal` | Control: the bend removed, the goal is not reached. Three cadences; Reset; save and load replay |
| Given `joined_pipe`, when the 2 m pipe is snapped to the fixed bend and Run, then the ball is delivered | Existing: `JoinedPipeTests.TheSnappedTwoMetrePipeDeliversTheBallAndResetRestoresTheBench` | Controls: `.AnUnsnappedPipeLeavesTheGoalUnsolved` (the missing connection), `.ALockedFixedBendIsRequired` |
| A dropped ball rides the outer wall and leaves at the speed gravity gives it | Existing: `HollowElementTests.ABallDroppedIntoABendRidesItsOuterWallAndLeavesAtTheDerivedSpeed` (90) | No added energy |
| A fitting payload passes; an oversized one stops at the mouth | Existing: `ActualPackageWallsPassAFittingPayloadAndStopAnOversizedPayload` (`Bend90` rows) | 0.34 m and 0.75 m law-scene spheres against the package colliders |
| A rim or sidewall strike stays outside and returns no more than the pair restitution | Existing: `ZeroRestitutionPackageControlDoesNotReturnImpactEnergy` (`Bend90`) | Control: restitution 0 returns at most g/480 |
| A straight pipe snaps to the bend and a ball crosses the joint without a step | Existing: `ABallCrossesASnappedJointWithoutSnagging` (`StraightToBend90`) | Reset restores the bench |
| **Planned row.** Given a 45° bend with this bend offered off and turned at its end mouth, when the query snaps it and a ball is dropped in, then the ball crosses both bends | A `Bend45ToBend90` member of `Joint` and its row in `TubeMouthSnapTests.ABallCrossesASnappedJointWithoutSnagging` | The theory's own assertions: at least 0.05 m/s on every tick inside the route, progress along the route chord never falling back by more than 1 mm, Reset restoring the bench. Control: the missed connection of `joined_pipe` above |
| **Planned rows.** Given a Fan and this bend in or out of its stream, then airflow work on the ball is zero behind the shell and positive with the bend out of the stream | A `Bend90` member of `HollowObstacle` and its two rows in `FanGuidanceTests.TransparentCurvedAndTaperedSolidWallsOccludeOnlyWhenInTheStream` | In stream: work equal to 0. Out of stream: work above 0. No tolerance is needed: an occluded stream does no work |
| Save and load keep the part and its colliders | Existing: `SaveCodec_RoundTripsPipeBend90AndPreservesDeclaredColliders` | Decoded values |
| A wrong-kind connection is impossible | The declaration has no port (`D:29`); the generic port-admission tests cover refusal | — |
| Light crosses the shell and stops at the collars | Owed with ENGINE-OPTICAL and Story 13.1: a beam through the shell reaches a detector | Control: a beam on a collar does not reach it; a ball inside the bore blocks it. Optical tolerances per coordinator decision 21 (±2 % power, 1 s guard, 0.5° direction, ±0.02 m end points) |
| A pipe or bend 0.48 m off in depth wins at Forgiving and Balanced and loses at Precise; 0.58 m wins at Forgiving only | Owed with Story 15.0 | — |

Distinctness: own package, own icon, matching collars; rails with Story 9R.19. Queued for the post-catalogue Chrome phase: construction in `quarter_bend` and the resize-and-snap gesture of `joined_pipe`.

## 6. Open questions

Resolved:

- **Oversized payload (D12).** A ball's declared `radius` may exceed the bore; the delivered proof is the 0.75 m law-scene sphere against the package colliders.
- **Played lesson (coordinator, 22 November 2026).** The played run of `quarter_bend` is present in `LaunchLessonSolveTests`; the earlier planned `PipeBend90Tests.QuarterBend_TheAuthoredSolutionDeliversTheBallAndWithoutTheBendItDoesNot` is deleted as a duplicate.
- **Two residual proofs (decided, no owner question).** Bend-to-bend into this bend and air occlusion by this package have no test of their own, and the requirement forbids closing them from the 45° bend. Each is one added case of an existing theory, not a new test.
- **Rails (coordinator, 22 November 2026; standing delegation).** The requirement says "matching collars/rails" and `DESIGN.md:357` asks for thin navy rails. Geometry as CAT-048 section 6: navy `#293954` cylinders of radius 0.018 m centred at radial distance 0.675 m, wholly inside the 0.65 to 0.70 m glass wall; decorative, enclosed, no collider. Here each of the two rails is sixteen chords, one per shell chord: the same transform as `Visual_Shell_Seg_n`, moved ±0.675 m along local Z, 0.26 m long. `VisibleSolidColliderTests` lists the thirty-two nodes as decorative. If Story 9R.19 replaces the shell chords with one swept mesh, each rail becomes one swept arc at the same radius and offset.
- **Story 9R.23 as native proofs (author decision, kept).** Rotated, oversize and rim controls are proven by the law tests on this package's colliders; the Chrome proof is queued, not dropped.
- **Placement nudging.** Story 15.0 owns the assistance criteria of Story 6.10 after all named intrinsic implementations and before catalogue-native-closure.

Needs the owner: nothing.

## 7. Integration

- **Collaborators.** Clear pipe, 45° bend, Funnel outlet, Ball detector and Powered gate (equal 0.65 m mouths); the four delivered balls (Basketball, Bowling ball, Tennis ball, Baseball), all narrower than the bore, 1.3 m across; Receiver; Fan. All delivered. Light sources are not delivered.
- **Campaign.** `quarter_bend` and `joined_pipe` in `content/puzzles.json`; first use 21–30 in the [element coverage ledger](../requirements.md#campaign-element-coverage) and chapter 3 "Pipe Dreams" of the [150-level progression](../requirements.md#sequence-task-315). Contact and cargo audit: [sequence-task-285](../requirements.md#sequence-task-285); [IX-01 contact impulse](../requirements.md#interaction-01).
- **Coverage binding.** Catalogue key `pipe_bend_90` in `docs/coverage/catalogue-elements.json`.
- **Lesson fixture.** Not needed: `joined_pipe` places the delivered part as a locked fixture.

## 8. Delivery

- **Records.** `review-6-10-pipe-bend-90.md`, `review-9a-pkg-2.md`, `review-9r-6-typed-mouth-snapping.md`, `review-9r-7-joined-pipe-level-playable.md` and `spec-engine-hollow-shapes.md` under `_bmad-output/implementation-artifacts/`. `sprint-status.yaml` holds 6.10 as in-progress.
- **Residual slices, in order.** Every residual slice is blocked on the engine programme (owner decision 3a, 22 November 2026), which precedes every further element.
  1. Story 9R.23, tests only: the two planned theory cases of section 5 (`CuriousContraptions.tests/TubeMouthSnapTests.cs`, `CuriousContraptions.tests/FanGuidanceTests.cs`), then the dated Pass addendum for Story 6.10. All six review layers: two probed capabilities have no exercising test yet, so the slice is not data-only.
  2. Story 9R.19: thirty-two rail nodes, the `VisibleSolidColliderTests` row and double-sided glass. Files: `elements/PipeBend90/pipe_bend_90.tscn`, `CuriousContraptions.tests/VisibleSolidColliderTests.cs`. Data-only tier.
  3. Light, after ENGINE-OPTICAL, with Story 13.1.
  4. Depth-error results in Story 15.0.
- **Disqualifiers.** A planned case failing through the delivered package (stop and plan a repair). Proof taken from the 45° bend. A rail drawn proud of the wall. Any change to the bore radius 0.65 m, the mouth rule or the 2.4 m centreline, which `joined_pipe` and `quarter_bend` depend on.
