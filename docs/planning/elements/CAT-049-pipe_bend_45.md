# CAT-049 · pipe_bend_45 (45° pipe bend) — declaration readiness spec

**Status (22 November 2026).** Delivered: package `elements/PipeBend45`, with true hollow colliders published at `eacf561d` by [`engine-hollow-shapes`](../../../_bmad-output/planning-artifacts/epics.md#story-engine-hollow-shapes). Section 2 is read from the package; section 3 is the capability probe of this date. Owed: one native control (Story 9R.23), rails and double-sided glass (Story 9R.19), light (ENGINE-OPTICAL) and placement assistance (Story 15.0). Every residual slice is blocked on the engine programme (owner decision 3a, 22 November 2026); the part's engine gaps are owned by `ENGINE-OPTICAL` (its clear shell and stubs declare Transmit (1, 1, 1), EL-176 section 10) and `ENGINE-CAPABILITY-INSTANCES` (mouth snapping as declared snap sockets, audit E5). Browser qualification is pending until the post-catalogue phase. Citations ending `@a6c914e` in section 4 describe the legacy source, not current code.

## 1. Identity

| Field | Value |
| --- | --- |
| CAT ID / kind | CAT-049 · `pipe_bend_45` (display name 45° pipe bend) |
| Requirement | [CAT-049](../requirements.md#current-cat-049) |
| Owner key | [CAT-049 delivery owner](../../../_bmad-output/planning-artifacts/epics.md#catalogue-slice-ownership) |
| Disposition | Delivered: package id `pipe_bend_45`, directory `elements/PipeBend45`. A distinct element from the 90° bend; no 90° proof stands in for it. |
| Mapped identities | EL-242 45° metal ball pipe bend (ledgered 23 November 2026, coordinator decision 27; it carries this spec's acceptance for the metal variant) and EL-072 Curved metal ball pipe (shared with CAT-050) are separate variants with their own entries (decision D12), not duplicates. Related, own specs: EL-009 45-degree water elbow (liquid), EL-107 Spiral gravity-delay tube. |
| Roadmap | Story 6.9 (delivered; one clause proof owned by Story 9R.23). |
| Levels | Inventory in `gentle_bend`; see [CAT-049-I consumers](../invest/current-consumers.md#cat-049-i). |
| Mode | The requirement's `TubeAngle=Degrees45` is this package itself (`sweep_degrees = 45`, `elements/PipeBend45/pipe_bend_45.tscn:110`). There is no selector. |

## 2. Declaration

Delivered declaration. `D:` is `elements/PipeBend45/PipeBend45ElementDeclaration.cs`; `S:` is `elements/PipeBend45/pipe_bend_45.tscn`. It differs from the 90° bend (CAT-050) only in sweep, arc centre, mouth poses, chord count, palette order and description.

| Item | Delivered value and source |
| --- | --- |
| Interfaces | `IMechanicalElement`, `ISceneGeometryElement` `D:11`. `SupportedDomains` is Mechanical only `D:40`. |
| Catalogue | Id `pipe_bend_45`, title 45° pipe bend, category Motion, palette order 17, colour (0.4, 0.72, 0.79) `D:16-25`. |
| Body | One static body (`KinematicMode.Static` `D:42`); footprint 1 body, 0 moving, 0 constraints `D:27`; mass 0 and zero moments `D:46-52`; `PayloadPolicy.None` `D:44`; no anchor points `D:54`. |
| Colliders | Baked from hidden scene proxies (`Colliders => []` `D:31`), all on the one body. `col_arc`: Torus segment, sweep 45°, centreline radius 2.4 m, tube outer radius 0.7 m, wall 0.05 m (bore radius 0.65 m), arc centre (−0.91844, −2.217311, 0) `S:32-34`, `S:105-111`. `col_stub_start`, `col_stub_end`: Tubes 0.09 m long, outer 0.7 m, wall 0.05 m, from each collar to its mouth `S:22-26`, `S:113-124`. `col_collar_start`, `col_collar_end`: full Torus, inner 0.65 m, outer 0.78 m `S:36-39`, `S:126-135`. |
| Material | Restitution 0.15, friction 0.3, rolling resistance 0, bounce threshold 0.1 m/s `D:33-36`. |
| Parameters, ports, stores, events, domain nodes | None `D:25`, `D:29`, `D:38`. Fixed geometry: no resize axis `D:13`. |
| Tube mouths | Start at (−1.008440, 0.182689, 0) facing −X; End at (0.842256, −0.583894, 0) facing (0.7071, −0.7071, 0); bore radius 0.65 m each `S:97-103`. The mouth rule is the family's (CAT-048 section 2): 0.09 m beyond the collar, equal bore. |
| Presentation | No binding of any kind `D:13-14`. Pick radius 2.2 m. |
| Art | `Visual/Visual_Shell/Visual_Shell_Seg_0` to `_7`: eight open cylinders, radius 0.7 m, 0.26 m long, cyan at alpha 0.16, laid as chords of the arc (sagitta 3.5 mm) `S:12-20`, `S:49-79`. `Visual_Stub_Start`, `Visual_Stub_End`: open cylinders, radius 0.7 m, 0.09 m `S:81-87`. `Visual_Collar_Start`, `Visual_Collar_End`: cream tori 0.65 to 0.78 m `S:89-95`. No rails yet (section 6). Told from the 90° bend by its sweep and its own icon (`elements/PipeBend45/icon.svg`). |

### 2.1 Engine conformance

The package is declaration data and a scene. It has no script, callback or update loop, and a search of `engine/`, the `CuriousContraptions.*` engine projects and `ui/` finds no code that names `pipe_bend_45` or its class (audit of 22 November, section 5; repeated for this repair). One refactor is owed by a shared contract the part uses, not by the part: its two mouths are the tube family's fixed `TubeEnd { Start, End }` recipe with engine snap constants (audit E5); `ENGINE-CAPABILITY-INSTANCES` makes them a declared list of snap sockets.

**Compiled counts.** Bodies 1 (static). Colliders 5: 1 Torus segment, 2 Tube, 2 Torus. Joints 0. Drives 0. Sensors 0. Domain nodes 0. Stores 0. Bindings 0. All counts are fixed at compile time.

**Why five colliders.** Visible = solid, one collider per drawn solid. The arc is the wall between the collars. Each stub is the drawn 9 cm of straight glass that carries the wall to the mouth plane, so a snapped neighbour's bore meets it without a gap. Each collar stands 0.08 m proud of the shell. The Clear pipe needs no stubs because its one Tube already reaches both mouths.

| Art node | Classification | Moved by |
| --- | --- | --- |
| `Visual/Visual_Shell/Visual_Shell_Seg_*` | Arc chords of `col_arc` (`CuriousContraptions.tests/VisibleSolidColliderTests.cs:136-137`) | Nothing |
| `Visual/Visual_Shell/Visual_Stub_*` | Hollow; match the stub Tubes | Nothing |
| `Visual/Visual_Collar_Start`, `_End` | Hollow; match the collar tori | Nothing |

| Behaviour | Generic law | Declared values |
| --- | --- | --- |
| A falling ball turns through the curve | Gravity plus contact normal rows against the Torus segment's inner surface; nothing is declared that adds speed | Centreline 2.4 m, sweep 45°, bore radius 0.65 m |
| The ball slides or rolls on the wall | Pair friction, the geometric mean of the two materials (`CuriousContraptions.Simulation/WorkshopConstraintSolver.cs:119`) | Friction 0.3; rolling resistance 0 |
| A rim or sidewall strike barely rebounds | Pair restitution, the product of the two materials (`WorkshopConstraintSolver.cs:117`) | Restitution 0.15; threshold 0.1 m/s |
| A payload wider than the bore stops at the mouth | Contact against the stub rim and the collar Torus | Bore radius 0.65 m |
| Tubes join | Authoring-time mouth query over declared mouths; it writes a pose, never a constraint | Mouth poses and bore |
| Air does not cross the shell | Airflow occlusion by solid colliders | The same five colliders |
| Light crosses the shell and stops at the collars | Optical transport with a declared surface response per collider: missing, [ENGINE-OPTICAL](../../../_bmad-output/planning-artifacts/epics.md#story-engine-optical-beam-emission-transport-surface-interaction-and-reception) | The shell and both stubs declare Transmit (1, 1, 1), a criterion of ENGINE-OPTICAL (EL-176 section 10, item 6); the collars keep the default Absorb |

**Performance fitness.** The body is static with no sensor or field region. Five primitive hollow colliders replace the deleted octagonal box ring. Tunnelling through the 5 cm wall is excluded by the engine's general means, speculative contacts at 480 Hz substeps (`docs/gpu-f32-physics.md:299-306`; envelope `:375`). Nothing costs work in proportion to the scene. Engine gap: optical response (ENGINE-OPTICAL).

## 3. Engine capabilities

Families from the [element map row](../general-engine-element-map.md): ContactImpulse, EnvironmentState, FiniteLedger, GeometryQuery, RigidBodyDynamics, SlidingFriction.

Capability probe, 22 November 2026. Tests are in `CuriousContraptions.tests/`. Counts: 7 present, 1 present but untested, 0 narrower, 2 missing.

| Capability | Status | Evidence or owner |
| --- | --- | --- |
| Torus-segment wall, Tube stubs and Torus collars: a ball rides the outer wall; a fitting payload passes and an oversized one stops; a rim strike returns no more than the pair restitution | present | `PipeBend45Tests.PipeBend45Scene_ExtractsItsTorusArcStubsCollarsAndTwoMouthPortLocators`; `HollowElementTests.ABallDroppedIntoABendRidesItsOuterWallAndLeavesAtTheDerivedSpeed`; `WorkshopPhysicsLawHollowTests.ActualPackageWallsPassAFittingPayloadAndStopAnOversizedPayload` (0.34 m passes, 0.75 m stops), `.ZeroRestitutionPackageControlDoesNotReturnImpactEnergy` (a drop on this package's collar: the rim and sidewall proof) |
| Typed mouths; straight-to-bend snap with a continuous seam | present | `TubeMouthSnapTests.APipeSnapsOntoTheEndMouthOfEveryTubePart`, `.ABallCrossesASnappedJointWithoutSnagging` (row `StraightToBend45`); `HollowElementTests.ABallCrossesASnappedStraightOrFunnelToBendJointWithoutAStep` |
| Bend-to-bend snap and turned passage | present | `TubeMouthSnapTests.ABallCrossesASnappedJointWithoutSnagging` (row `Bend45ToBend45`: the second bend offered off and turned, snapped by the query, and the ball crosses both) |
| Compile, capacity, save and load | present | `PipeBend45Tests.WorkshopPhysicsCompiler_CompilesPipeBend45IntoStaticBodyWithItsHollowColliders`, `.WorkbenchCapacity_AdmitsPipeBendsWithoutColliderLimit`, `.SaveCodec_RoundTripsPipeBend45AndPreservesDeclaredColliders` |
| Level `gentle_bend`: authored content, validation, precision knots | present | `PipeBend45Tests.GentleBend_AuthoredSettingsAndFixturesMatchPuzzlesJson`, `.GentleBend_ValidateEnforcesFixturesAndInventory`, `.GentleBend_PrecisionEvaluatesAssistanceAndRoundtripsSaveCodec` |
| `gentle_bend` played to its goal; without the bend the goal is not reached; Reset and save/load replay | present | `LaunchLessonSolveTests.AuthoredLessonSolvesAndOneMissingCausePreventsTheGoal` (`gentle_bend` is in `Lessons`; 60, 120 and 240 Hz; the control removes the bend, the only inventory part) |
| Air blocked by the curved shell only when it is in the stream | present | `FanGuidanceTests.TransparentCurvedAndTaperedSolidWallsOccludeOnlyWhenInTheStream` (rows `Bend`, this package) |
| Missed connection: a bend offered outside the capture distance is not joined, and the ball does not pass through it | present, untested | The snap refusal is proven on pipes (`TubeMouthSnapTests.OnlyAMouthWithinTheCaptureDistanceSnaps`); no played control places this package unjoined. Planned in section 5 |
| Optical clarity of the shell, opaque collars | missing | [ENGINE-OPTICAL](../../../_bmad-output/planning-artifacts/epics.md#story-engine-optical-beam-emission-transport-surface-interaction-and-reception): Transmit (1, 1, 1) on the shell and stubs is a criterion of that story (EL-176 section 10, item 6) |
| Placement nudging: the authored assistance controls and the valid Precise alternative route | missing | [Story 15.0](../../../_bmad-output/planning-artifacts/epics.md#story-150-physical-placement-nudging--difficulty-assistance) |

Depends on the Clear pipe's bore, collar and mouth rule (CAT-048).

## 4. Legacy harvest

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| 1 | With zero gravity, a ball entering 0.8 m before the inlet at 6 m/s turns through the 45° bend in orientations (0,0,0), (30,40,50) and (0,90,90): steps < 0.051 m, speed ≤ 6.001 m/s, exit velocity within cos > 0.8 of the outlet normal; the bend has no boxes, one bend proxy and two collar tubes; Reset restores the ball's initial pose and velocity. | `CuriousContraptions.tests/PipeBendTests.cs@a6c914e:L145-L193` | carry forward (rotated passage, no added energy); delivered by the tests of section 3. The collider inventory is the delivered five (section 2), not the legacy proxy and collar tubes. |
| 2 | The 45° bend's End mouth snaps a straight pipe, another 45° bend and a 90° bend placed 0.1 m off: mouths coincide within 1e−4 m. | `CuriousContraptions.tests/PipeBendTests.cs@a6c914e:L195-L221` | carry forward (straight-to-bend and bend-to-bend); delivered for the pipe and a second 45° bend. The 45°-to-90° joint is a planned row in CAT-050 section 5. |
| 3 | Gravity alone carries a ball dropped 1.6 m above the inlet of a bend at (0, 4, 0) rotated −90° down to y ≤ 1.2 at x < −0.5. | `CuriousContraptions.tests/PipeBendTests.cs@a6c914e:L223-L243` | carry forward; delivered. |
| 4 | gentle_bend with the bend moved 0.48 m in depth is won at precision 0, 0.45 and also 1 (an actual valid Precise route); at precision < 1 the assistance corrects the depth to 0, at 1 it stays 0.48. | `CuriousContraptions.tests/PipeBendTests.cs@a6c914e:L245-L271` | carry forward (Precise alternative route; authored assistance controls); owed with Story 15.0. |
| 5 | Level layout: inlet x = 2.4·(1 − cos(angle/2)), inlet y = 4 + 2.4·sin(angle/2) + 0.09; ball at the inlet 1.6 m higher; receiver at (−2, 0.6, 0) for 45°; bend_1 at (0, 4, 0) rotated −90° about Z. | `tools/Campaign/Program.cs@a6c914e:L290-L306` | carry forward; delivered unchanged in `content/puzzles.json` (`gentle_bend`: ball (0.1827, 6.6084, 0)). |
| 6 | Bend assistance window: position 0.6 / 0.5 m and rotation 5° / 2° with equal correction caps at precision 0 / 0.45; curved outlets amplify residual lateral error, so a more distant bend is never moved. | `tools/Campaign/Program.cs@a6c914e:L564-L573` | carry forward the values and rule (Story 15.0); do not carry forward the string `part.Kind` test. |
| 7 | Test fixtures map an enum (Straight, Bend45, Bend90) to catalogue strings and reject undefined values. | `CuriousContraptions.tests/PipeBendTests.cs@a6c914e:L11-L39` | carry forward the typed boundary rule only. |
| 8 | Bend walls were built as rounded convex segments within 0.005 m surface error; bends are never optically opaque. | `engine/physics/HollowGeometry.cs@a6c914e:L109-L146`; `engine/SceneCollisionGeometry.cs@a6c914e:L76-L76` | carry forward the transparency fact (ENGINE-OPTICAL); do not carry forward the CPU segmentation or its 0.005 m bound. |
| 9 | A 45° bend rotated (20, 35, 10): a ray from 2 m outside the mid-sweep shell blocks air at 0.7–1.4 m but passes light for the full 4 m. | `CuriousContraptions.tests/WindChimeTests.cs@a6c914e:L220-L237` | carry forward. The air control is delivered (`FanGuidanceTests`); light is owed with ENGINE-OPTICAL. |

Files harvested:
- `parts/PipeBendPart.cs`
- `parts/PipeArt.cs` (shared tube mesh)
- `parts/scenes/pipe_bend_45.tscn`
- `parts/catalog/pipe_bend_45.tres`
- `engine/BendProxy.cs`
- `engine/MachineData.cs` (TubeBendAngle)
- `engine/TubeMouth.cs`
- `engine/physics/HollowGeometry.cs`
- `engine/SceneCollisionGeometry.cs`
- `CuriousContraptions.tests/PipeBendTests.cs`
- `CuriousContraptions.tests/WindChimeTests.cs` (L220-L237)
- `tools/Campaign/Program.cs`

## 5. Acceptance outline

Acceptance: [CAT-049](../requirements.md#current-cat-049) and [retained behaviour](../requirements.md#todo-250). Every row runs the production simulation or the production authoring query on this declaration; none needs a code path that exists only for this bend, and none is taken from the 90° bend.

| Given / When / Then | Proof | Control and boundary |
| --- | --- | --- |
| Given `gentle_bend` with its authored solution, when Run, then the ball turns through the curve and is captured | Existing: `LaunchLessonSolveTests.AuthoredLessonSolvesAndOneMissingCausePreventsTheGoal` | Control: the bend removed, the goal is not reached. Three cadences; Reset; save and load replay |
| A dropped ball rides the outer wall and leaves at the speed gravity gives it | Existing: `HollowElementTests.ABallDroppedIntoABendRidesItsOuterWallAndLeavesAtTheDerivedSpeed` (45) | No added energy |
| A fitting payload passes; an oversized one stops at the mouth | Existing: `ActualPackageWallsPassAFittingPayloadAndStopAnOversizedPayload` (`Bend45` rows) | 0.34 m and 0.75 m law-scene spheres against the package colliders |
| A rim or sidewall strike stays outside and returns no more than the pair restitution | Existing: `ZeroRestitutionPackageControlDoesNotReturnImpactEnergy` (`Bend45`) | Control: restitution 0 returns at most g/480 |
| Straight-to-bend and bend-to-bend joints snap, and a ball crosses each without a step | Existing: `ABallCrossesASnappedJointWithoutSnagging` (`StraightToBend45`, `Bend45ToBend45`); `ABallCrossesASnappedStraightOrFunnelToBendJointWithoutAStep` | Second part offered 0.15 m off and turned 4°; Reset restores the bench |
| **Planned.** Given a vertical pipe and a 45° bend offered 0.72 m to the side of the pipe's lower mouth, when placed and Run, then the query returns no snap, the ball strikes the bend's collar and never leaves through the bend's far mouth | `TubeMouthSnapTests.AnUnsnappedBend45DoesNotReceiveTheBall` | Control: the `StraightToBend45` row of the existing theory. Boundary: 0.72 m, beyond the 0.45 m capture and beyond the 0.65 m bore radius. A 3 s stall guard; the assertion is the side of the far mouth plane, no float tolerance |
| Save and load keep the part and its colliders | Existing: `SaveCodec_RoundTripsPipeBend45AndPreservesDeclaredColliders` | Decoded values |
| A wrong-kind connection is impossible | The declaration has no port (`D:29`); the generic port-admission tests cover refusal | — |
| Air is blocked only when the shell is in the stream | Existing: `FanGuidanceTests.TransparentCurvedAndTaperedSolidWallsOccludeOnlyWhenInTheStream` | In stream and out of stream |
| Light crosses the shell and stops at the collars | Owed with ENGINE-OPTICAL and Story 13.1: a beam through the shell reaches a detector | Control: a beam on a collar does not reach it; a ball inside the bore blocks it. Optical tolerances per coordinator decision 21 (±2 % power, 1 s guard, 0.5° direction, ±0.02 m end points) |
| A bend 0.48 m off in depth is corrected at Forgiving and Balanced, and a valid Precise route exists | Owed with Story 15.0 | — |

Distinctness: own package, own icon, own collars; rails with Story 9R.19. Queued for the post-catalogue Chrome phase: construction through the palette and rotation in `gentle_bend`.

## 6. Open questions

Resolved:

- **Oversized payload (D12).** A ball's declared `radius` may exceed the bore; the delivered proof is the 0.75 m law-scene sphere against the package colliders.
- **Played lesson (coordinator, 22 November 2026).** The played run of `gentle_bend` is present in `LaunchLessonSolveTests`; the earlier planned `PipeBend45Tests.GentleBend_TheAuthoredSolutionDeliversTheBallAndWithoutTheBendItDoesNot` is deleted as a duplicate.
- **Bend-to-bend (this repair).** The `Bend45ToBend45` row already plays the joint, so the earlier planned `HollowElementTests.ABallCrossesASnappedBendToBendJointWithoutAStep` is deleted. Its control, the missed connection, is kept as the one planned test of section 5, because the requirement names it and no played control places this package unjoined.
- **Rails (coordinator, 22 November 2026; standing delegation).** The requirement says "preserve its own icon/rails/collars" and `DESIGN.md:357` asks for thin navy rails. Geometry as CAT-048 section 6: navy `#293954` cylinders of radius 0.018 m centred at radial distance 0.675 m, wholly inside the 0.65 to 0.70 m glass wall; decorative, enclosed, no collider. Here each of the two rails is eight chords, one per shell chord: the same transform as `Visual_Shell_Seg_n`, moved ±0.675 m along local Z, 0.26 m long. At z = ±0.675 m a rail lies at the centreline's 2.4 m bend radius, so its chords are the shell's. `VisibleSolidColliderTests` lists the sixteen nodes as decorative. If Story 9R.19 replaces the shell chords with one swept mesh, each rail becomes one swept arc at the same radius and offset.
- **Story 9R.23 as native proofs (author decision, kept).** Rim, oversize and sidewall are proven by the law tests on this package's colliders; the Chrome proof is queued, not dropped.
- **Placement nudging.** Story 15.0 owns the assistance criteria of Story 6.9, including the valid Precise alternative route, after all named intrinsic implementations and before catalogue-native-closure.

Needs the owner: nothing.

## 7. Integration

- **Collaborators.** Clear pipe, 90° bend, Funnel outlet, Ball detector and Powered gate (equal 0.65 m mouths); the four delivered balls (Basketball, Bowling ball, Tennis ball, Baseball), all narrower than the bore, 1.3 m across; Receiver; Fan. All delivered. Light sources are not delivered.
- **Campaign.** `gentle_bend` in `content/puzzles.json`; first use 21–30 in the [element coverage ledger](../requirements.md#campaign-element-coverage) and chapter 3 "Pipe Dreams" of the [150-level progression](../requirements.md#sequence-task-315). Contact and cargo audit: [sequence-task-285](../requirements.md#sequence-task-285); [IX-01 contact impulse](../requirements.md#interaction-01).
- **Coverage binding.** Catalogue key `pipe_bend_45` in `docs/coverage/catalogue-elements.json`.
- **Lesson fixture.** Not needed: `gentle_bend` places the delivered part.

## 8. Delivery

- **Records.** `review-6-9-pipe-bend-45.md`, `review-9a-pkg-2.md`, `review-9r-6-typed-mouth-snapping.md` and `spec-engine-hollow-shapes.md` under `_bmad-output/implementation-artifacts/`. `sprint-status.yaml` holds 6.9 as in-progress.
- **Residual slices, in order.** Every residual slice is blocked on the engine programme (owner decision 3a, 22 November 2026), which precedes every further element.
  1. Story 9R.23, tests only: the planned control of section 5, then the dated Pass addendum for Story 6.9. All six review layers: the probed capability has no exercising test yet, so the slice is not data-only.
  2. Story 9R.19: sixteen rail nodes, the `VisibleSolidColliderTests` row and double-sided glass. Files: `elements/PipeBend45/pipe_bend_45.tscn`, `CuriousContraptions.tests/VisibleSolidColliderTests.cs`. Data-only tier.
  3. Light, after ENGINE-OPTICAL, with Story 13.1.
  4. Assistance in Story 15.0.
- **Disqualifiers.** The planned control failing through the delivered package (stop and plan a repair). Proof taken from the 90° bend. A rail drawn proud of the wall. Any change to the bore radius 0.65 m, the mouth rule or the 2.4 m centreline, which `gentle_bend` depends on.
