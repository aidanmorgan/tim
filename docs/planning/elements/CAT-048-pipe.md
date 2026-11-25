# CAT-048 · pipe (Clear pipe) — declaration readiness spec

**Status (22 November 2026).** Delivered: package `elements/Pipe`, with true hollow colliders published at `eacf561d` by [`engine-hollow-shapes`](../../../_bmad-output/planning-artifacts/epics.md#story-engine-hollow-shapes). Section 2 is read from the package; section 3 is the capability probe of this date. Owed: two native proofs (Story 9R.23), rails and double-sided glass (Story 9R.19), light (ENGINE-OPTICAL) and placement assistance (Story 15.0). Every residual slice is blocked on the engine programme (owner decision 3a, 22 November 2026); the part's engine gaps are owned by `ENGINE-OPTICAL` (its clear shell declares Transmit (1, 1, 1), EL-176 section 10) and `ENGINE-CAPABILITY-INSTANCES` (mouth snapping as declared snap sockets, audit E5). Browser qualification is pending until the post-catalogue phase. Citations ending `@a6c914e` in section 4 describe the legacy source, not current code.

## 1. Identity

| Field | Value |
| --- | --- |
| CAT ID / kind | CAT-048 · `pipe` (display name Clear pipe) |
| Requirement | [CAT-048](../requirements.md#current-cat-048) |
| Owner key | [CAT-048 delivery owner](../../../_bmad-output/planning-artifacts/epics.md#catalogue-slice-ownership) |
| Disposition | Delivered: package id `pipe`, directory `elements/Pipe`. |
| Mapped identities | EL-071 Straight metal ball pipe is a separate variant with its own entry (decision D12), not a duplicate. Related, own specs: EL-102 Large-bore ball pipe, EL-103 Accelerator tube, EL-107 Spiral gravity-delay tube, EL-008 Straight water pipe (liquid). |
| Roadmap | Stories 6.6 and 6.7 (delivered; clause proofs owned by Story 9R.23). |
| Levels | Inventory in `clear_pipe` and `joined_pipe`; see [CAT-048-I consumers](../invest/current-consumers.md#cat-048-i). |

## 2. Declaration

Delivered declaration. `D:` is `elements/Pipe/PipeElementDeclaration.cs`; `S:` is `elements/Pipe/pipe.tscn`.

| Item | Delivered value and source |
| --- | --- |
| Interfaces | `IMechanicalElement`, `ISceneGeometryElement`, `IParameterizedGeometryElement` `D:13`. `SupportedDomains` is Mechanical only `D:67`. |
| Catalogue | Id `pipe`, title Clear pipe, category Motion, palette order 15, colour (0.4, 0.72, 0.79) `D:28-36`. |
| Body | One static body (`KinematicMode.Static` `D:69`); footprint 1 body, 0 moving, 0 constraints `D:42`; mass 0 and zero moments `D:73-79`; `PayloadPolicy.None` `D:71`; no anchor points `D:81`. |
| Colliders | Baked from hidden scene proxies (`Colliders => []` `D:57`), all on the one body. `col_shell`: Tube, outer radius 0.7 m, wall 0.05 m (bore radius 0.65 m), length `length` + 0.18 m (3.78 m at the default) `S:12-16`, `S:53-57`. `col_collar_start`, `col_collar_end`: full Torus, inner 0.65 m, outer 0.78 m (centreline 0.715 m, tube 0.065 m), at x = ∓`length`/2 `S:22-25`, `S:59-67`. `ColliderBindings` tie the Tube half-length to `length`/2 + 0.09 m and the collar offsets to ∓`length`/2 `D:50-55`. |
| Material | Restitution 0.15, friction 0.3, rolling resistance 0, bounce threshold 0.1 m/s `D:60-63` (field order `CuriousContraptions.Abstractions/ColliderDescriptor.cs:21-24`). |
| Parameter | `length`: Real, metre, default 3.6, minimum 1, maximum 8 `D:39`; editor policy Reject and BuildOnly, the record defaults (`CuriousContraptions.Abstractions/ParameterDescriptor.cs:60-61`). One resize handle on local X `D:25-26`. |
| Ports, stores, events, domain nodes | None `D:44`, `D:65`. No signal, supply or electrical node. |
| Tube mouths | Start and End at x = ∓(`length`/2 + 0.09 m), bore radius 0.65 m, facing outward: `TubeMouthBindings` `D:46-47`, locators `S:45-51`. A mouth is placement data for the snap query; it is not a port and creates no network edge. |
| Presentation | Three parameter bindings on `length`: `Visual/Visual_Shell` ScaleY (multiplier 1/3.78, offset 0.18/3.78), `Visual/Visual_Collar_Start` LocalX (−0.5), `Visual/Visual_Collar_End` LocalX (+0.5) `D:21-24`. No driver binding, body binding or span. Pick radius 2.2 m `D:21`. |
| Art | `Visual_Shell`: open cylinder, radius 0.7 m, 3.78 m, 64 segments, cyan (0.4, 0.72, 0.79) at alpha 0.16 `S:4-20`, `S:33-35`. `Visual_Collar_Start`, `Visual_Collar_End`: cream tori 0.65 to 0.78 m `S:9-10`, `S:22-27`, `S:37-43`. No rails yet (section 6). |

The four tube parts share one mouth rule: every snapping mouth has bore radius 0.65 m and lies 0.09 m beyond its collar, on the outer collar face, so two snapped bores meet at one plane.

### 2.1 Engine conformance

The package is declaration data, a scene and bindings. It has no script, callback or update loop, and a search of `engine/`, the `CuriousContraptions.*` engine projects and `ui/` finds no code that names `pipe` or its class (audit of 22 November, section 5; repeated for this repair). One refactor is owed by a shared contract the part uses, not by the part: its two mouths are the tube family's fixed `TubeEnd { Start, End }` recipe, and the snap capture constants in `engine/TubeMouthSnap.cs` were taken from this part (audit E5); `ENGINE-CAPABILITY-INSTANCES` makes them a declared list of snap sockets, each with its own capture distance and angle.

**Compiled counts.** Bodies 1 (static). Colliders 3: 1 Tube, 2 Torus. Joints 0. Drives 0. Sensors 0. Domain nodes 0. Stores 0. Presentation bindings 3 (parameter). Build-time bindings: 3 collider, 2 mouth. All counts are fixed at compile time.

**Why three colliders.** Visible = solid: each cream collar stands 0.08 m proud of the shell and a ball can rest on it or strike it, so each has its own Torus. The Tube alone carries the wall to both mouths.

| Art node | Classification | Moved by |
| --- | --- | --- |
| `Visual/Visual_Shell` | Hollow; matches `col_shell` (`CuriousContraptions.tests/VisibleSolidColliderTests.cs:153`) | Nothing during Run. Its length is set at Build by the `length` parameter binding, and `col_shell` takes the same parameter through `ColliderBindings`, so art and collider agree at every length. |
| `Visual/Visual_Collar_Start`, `_End` | Hollow; match the collar tori | Nothing during Run; placed at Build by `length`, as their colliders are. |

No art is moved by an animation binding, and nothing in physics reads art.

| Behaviour | Generic law | Declared values |
| --- | --- | --- |
| A ball rolls down the bore and stays inside | Gravity plus contact normal rows against the Tube's inner surface; no force, drive or transport is declared, so no speed is added | Tube bore radius 0.65 m, wall 0.05 m |
| The ball slides or rolls on the wall | Pair friction, the geometric mean of the two materials (`CuriousContraptions.Simulation/WorkshopConstraintSolver.cs:119`); rolling resistance | Friction 0.3; rolling resistance 0 |
| A dropped ball barely rebounds | Pair restitution, the product of the two materials (`WorkshopConstraintSolver.cs:117`), above the bounce threshold | Restitution 0.15 (a Basketball returns 0.55 × 0.15); threshold 0.1 m/s |
| A payload wider than the bore stops at the mouth | Contact against the Tube end rim and the collar Torus | Bore radius 0.65 m |
| Length changes the part | Parameter-bound collider geometry and mouths | `length` 1 to 8 m |
| Two tubes join | Authoring-time mouth query over declared mouths (0.45 m, 20°, equal bore); it writes a pose, never a constraint | Mouth positions and bore |
| Air does not cross the shell | Airflow occlusion by solid colliders | The same three colliders |
| Light crosses the shell and stops at the collars | Optical transport with a declared surface response per collider: missing, [ENGINE-OPTICAL](../../../_bmad-output/planning-artifacts/epics.md#story-engine-optical-beam-emission-transport-surface-interaction-and-reception) | `col_shell` declares Transmit (1, 1, 1), a criterion of ENGINE-OPTICAL (EL-176 section 10, item 6); the collars keep the default Absorb |

**Performance fitness.** The body is static and has no sensor or field region. Three primitive hollow colliders replace the deleted box ring. Tunnelling through the 5 cm wall is excluded by the engine's general means, speculative contacts at 480 Hz substeps (`docs/gpu-f32-physics.md:299-306`; envelope `:375`: no penetration of a barrier at least 1 cm thick at up to 64 m/s). Nothing costs work in proportion to the scene. Engine gaps: optical response (ENGINE-OPTICAL). Placement assistance is authoring behaviour owned by Story 15.0.

## 3. Engine capabilities

Families from the [element map row](../general-engine-element-map.md): ContactImpulse, EnvironmentState, FiniteLedger, GeometryQuery, RigidBodyDynamics, SlidingFriction.

Capability probe, 22 November 2026. Tests are in `CuriousContraptions.tests/`. Counts: 10 present, 1 present but untested, 0 narrower, 2 missing.

| Capability | Status | Evidence or owner |
| --- | --- | --- |
| Static Tube and Torus contact in the bore, on the rim and on the outer wall; a fitting payload passes and an oversized one stops | present | `StraightPipeTests.WorkshopPhysicsCompiler_CompilesPipeIntoStaticBodyWithItsThreeHollowColliders`; `HollowElementTests.ABallRollsThroughAStraightPipeOfEveryLengthOnItsWallWithoutCatching`; `WorkshopPhysicsLawHollowTests.ActualPackageWallsPassAFittingPayloadAndStopAnOversizedPayload` (0.34 m passes, 0.75 m stops) |
| Entry speed and edge angles (the Story 6.7 clause) | present | `WorkshopPhysicsLawHollowTests.FastAxialLaunchPassesOnlyTheBoreOrClearAir` (the Tube law at 64 m/s, turned off the world axes, with an oversized control); `.ActualPackageWallsPassAFittingPayloadAndStopAnOversizedPayload` (the package colliders, turned −90°); `.ZeroRestitutionPackageControlDoesNotReturnImpactEnergy` (a drop on the package collar) |
| Parameter-bound colliders, mouths and art | present | `D:46-55`; `WorkshopPipeLengthTests.TheShellTubeSpansTheLengthToBothMouthsAndTheCollarsFollowItsEnds`, `.MouthsSitAtTheCollarFacesOfTheLength`, `.ArtworkAndPickRadiusFollowTheLength` |
| Parameter admission and refusal; save and command wire | present | `WorkshopPipeLengthTests.LengthsOutsideOneToEightMetresAreRejected`, `.TheSaveCodecRoundTripsTheLength`, `.TheCommandWireRoundTripsTheLength`, `.ASaveWithoutAnAdmittedLengthIsRejectedNeverDefaulted`; `HollowElementTests.AnInvalidHollowLengthIsRefusedWithoutChangingThePriorConstruction` |
| Typed mouth snapping: 0.45 m and 20° capture, occupied and unequal-bore refusal, locked parts, non-mutating query, shortest arc keeping roll | present | `engine/TubeMouthSnap.cs:35`; `TubeMouthSnapTests.ANewPipeOffsetAndTurnedSnapsOntoAFreeMouthByTheShortestArcAndKeepsItsRoll`, `.OnlyAMouthWithinTheCaptureDistanceSnaps`, `.OnlyAMouthWithinTheCaptureAngleSnaps`, `.AnOccupiedMouthIsNotACandidate`, `.BoresMustBeEqualWithin1EMinus6Metres`, `.ALockedMoverNeverSnapsAndALockedTargetsFreeMouthDoes`, `.TheQueryNeverMutatesTheConstruction` |
| Continuous seam across a snapped joint | present | `TubeMouthSnapTests.ABallCrossesASnappedJointWithoutSnagging`; `HollowElementTests.ABallRollingThroughTwoSnappedPipesKeepsItsHeightAcrossTheJoint` |
| Hollow-wall overlap refused at construction; bore and outside stay clear | present | `HollowElementTests.AHollowWallOverlapIsRefusedAtomicallyWhileBoreAndOutsideRemainClear` |
| Run/Reset and save/load of a connected passage | present | `HollowElementTests.AConnectedHollowPassageRestoresConstructionAndRepeatsAfterResetAndSaveLoad` |
| Air blocked across the shell, open along the bore | present | `FanGuidanceTests.TransparentPipeBlocksAcrossItsShellButAllowsItsOpenBore` |
| `clear_pipe` and `joined_pipe` played to their goals; without the pipe the goal is not reached | present | `LaunchLessonSolveTests.AuthoredLessonSolvesAndOneMissingCausePreventsTheGoal` (60, 120 and 240 Hz; Reset; save and load replay); `JoinedPipeTests.TheSnappedTwoMetrePipeDeliversTheBallAndResetRestoresTheBench`, `.AnUnsnappedPipeLeavesTheGoalUnsolved` |
| Snap and resize refused while Running or Paused | present, untested | `ui/WorkshopGuidance.cs:274` (`MoveSnap` returns no snap unless `CanEdit`); `length` is BuildOnly. No native test exercises either; planned in section 5 |
| Optical transmission through the shell; collar occlusion | missing | [ENGINE-OPTICAL](../../../_bmad-output/planning-artifacts/epics.md#story-engine-optical-beam-emission-transport-surface-interaction-and-reception): Transmit (1, 1, 1) on the shell is a criterion of that story (EL-176 section 10, item 6); proof with Story 13.1 |
| Placement nudging for the depth-error difficulty controls | missing | [Story 15.0](../../../_bmad-output/planning-artifacts/epics.md#story-150-physical-placement-nudging--difficulty-assistance); its text names CAT-049 and CAT-050 only (section 6) |

Depends on a ball. The bends, the Funnel, the Ball detector and the Powered gate reuse the Tube collider kind and the mouth rule.

## 4. Legacy harvest

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| 1 | With zero gravity, a ball started at local (−3, 0.1, 0) moving 4 m/s along the axis of a pipe rotated (0,0,0), (30,40,50) or (0,90,90) travels continuously (< 0.04 m per 1/120 s step) to local x 3 ± 0.01, y 0.1 ± 0.01, speed 4 ± 0.01. | `CuriousContraptions.tests/PipeTests.cs@a6c914e:L11-L42` | carry forward (tilted/rotated passage, no energy change); delivered by the law tests of section 3. |
| 2 | A ball dropped at 8 m/s onto the outer wall stays outside: local y ≥ 0.70 + radius − 0.001. | `CuriousContraptions.tests/PipeTests.cs@a6c914e:L43-L51` | carry forward (sidewall control); delivered. |
| 3 | A 0.34 m ball at 40 m/s passes the bore (x 5 ± 0.01 after 24 steps); a 0.8 m ball at 4 m/s is stopped at the mouth (x < −1.89); speed never increases. | `CuriousContraptions.tests/PipeTests.cs@a6c914e:L56-L80` | carry forward (high-speed and oversize controls); delivered with a law-scene sphere of radius 0.75 m (section 5). The per-instance radius property of the legacy ball is not current. |
| 4 | Light: along the axis through the bore is unobstructed (10 m); at y + 0.7 the collar face blocks at 3.10–3.12 m; across the clear shell sideways is unobstructed. | `CuriousContraptions.tests/PipeTests.cs@a6c914e:L82-L96` | carry forward (collar optical blocking, clear-wall transmission); owed with ENGINE-OPTICAL. |
| 5 | clear_pipe: with the reference pipe moved 0.48 m in depth, the level is won at precision 0 and 0.45 but not 1; with no depth error it is won at all three; Reset replays the same state signature; without the pipe it is never won. | `CuriousContraptions.tests/PipeTests.cs@a6c914e:L98-L138` | carry forward (difficulty controls). The no-error win, the no-pipe control and Reset are delivered (`LaunchLessonSolveTests`), asserted on decoded state, not a signature. The depth-error wins at precision 0 and 0.45 need Story 15.0. |
| 6 | Resizing to 1, 4.2 or 8 sets tube half length length/2, keeps bore 0.65, moves collars to ±length/2, serializes `length`, artwork bounds length + 0.18, collar optical face at 6 − length/2 − 0.09; Reset keeps length and unit scale; a bore change rejects. | `CuriousContraptions.tests/PipeResizeTests.cs@a6c914e:L10-L40` | carry forward; delivered except the optical face (ENGINE-OPTICAL). The delivered Tube reaches the mouths (half length length/2 + 0.09). |
| 7 | Lengths 0, 9 and NaN reject at construction with no part added. | `CuriousContraptions.tests/PipeResizeTests.cs@a6c914e:L42-L57` | carry forward; delivered. |
| 8 | Only the local length handle is interactive (Y and Z disabled); a 0.3-axis drag sets 4.2; Cancel restores 3.6 and half length 1.8. | `CuriousContraptions.tests/PipeResizeTests.cs@a6c914e:L59-L89` | carry forward; one X handle is declared; the gesture, Cancel and Undo are queued Chrome cases. |
| 9 | Snapping a second pipe 0.1 m off and 0.08 rad rotated: the query is non-mutating; applied, mouths coincide within 1e−4 and face opposite (dot < −0.9999); a ball crosses the seam continuously to local x 7 ± 0.01 at 4 ± 0.01 m/s; a joined mouth is no longer a snap candidate; the placement survives Reset. | `CuriousContraptions.tests/TubePlacementSnapTests.cs@a6c914e:L11-L52` | carry forward (continuous seams, occupied mouths); delivered. |
| 10 | Snapping is unavailable while Running and while paused (Running false after Start) and never mutates bodies; it returns after Reset; a part from another world never snaps. | `CuriousContraptions.tests/TubePlacementSnapTests.cs@a6c914e:L54-L85` | carry forward (idle-construction-only, foreign same-ID parts reject); the run-state proof is planned (section 5). The query takes one construction's instances (`engine/TubeMouthSnap.cs:35`), so a part of another construction is never a candidate. |
| 11 | Mouths 0.72 m apart (pipes 4.5 m apart) or facing 40° off do not snap; an occupied mouth and a locked part are not candidates. | `CuriousContraptions.tests/TubePlacementSnapTests.cs@a6c914e:L87-L121` | carry forward (0.45 m / 20° thresholds per DESIGN); delivered. |
| 12 | Resizing is rejected while Running or Paused without changing tubes, saved construction or physics; it applies after Reset. | `CuriousContraptions.tests/ConstructionLifecycleTests.cs@a6c914e:L92-L139` | carry forward; the proof is planned (section 5). |
| 13 | Occupancy of a hollow tube: a box in the bore, in the end hole or beyond the end is clear; a box in the shell, on the annular rim, surrounding the tube or touching the outside overlaps; the result is invariant under a rigid transform. | `CuriousContraptions.tests/TubeBoxIntersectionTests.cs@a6c914e:L8-L38` | carry forward (geometry facts for any part inside a bore, e.g. powered gate blade); delivered as the construction overlap refusal. |
| 14 | Parked GPU profile: lengths 1, 3.6, 8 round-trip; one exposed annular profile per pipe; saved lengths 0, 0.5, 9, NaN, +∞ and non-zero padding reject (Story 9R.7: the save and wire carry no format version, so "previous schemas" are structurally invalid input); a pipe in the first_principles puzzle rejects; maximum population (8 instances incl. a length-8 pipe) fits capacity. | `CuriousContraptions.tests/WorkshopPipeTests.cs@a6c914e:L14-L136` | carry forward the behaviour; do not carry forward the half-precision lanes, the fixed collider ABI bytes or the retired `AnnularFeature` enum. |
| 15 | Hollow walls were built as convex segments with maximum surface error 0.005 m; tube collars carry an opaque flag, bends and frustums are never opaque. | `engine/SceneCollisionGeometry.cs@a6c914e:L37-L38`; `engine/SceneCollisionGeometry.cs@a6c914e:L75-L77`; `engine/physics/HollowGeometry.cs@a6c914e:L80-L108` | carry forward the opacity facts (ENGINE-OPTICAL); do not carry forward the CPU convex segmentation or its 0.005 m bound. |
| 16 | GPU probe fixture matrix for a default pipe and a Basketball: bore-axis fall onto the inner wall; frictionless circumferential motion at constant speed; tilted axial rolling (−0.2 rad); mouth departure to rim/free motion; outer-shell approach hitting the exposed collar shoulder; plane and box rolling controls. | `reference/pipe/Program.cs@a6c914e:L22-L119` | carry forward the fixture matrix; do not carry forward the "shorter trial" subdivision case (CPU-style directed trial) or the WGSL diagnostic shaders in `reference/pipe/` (proof/diagnostic artefacts). |
| 17 | clear_pipe reference: ball (−1.4, 6, 0), receiver (2, 0.6, 0), pipe_1 at (0, 3, 0) rotated −45° about Z, length 3.6. joined_pipe: ball (−1.308, 8.402, 0), fixed bend (1, 3.5, 0) at −45°, receiver (−0.2, 0.6, 0), pipe_1 at (−0.537, 6.031, 0), −45°, length 2. | `tools/Campaign/Program.cs@a6c914e:L279-L320` | carry forward the levels; the delivered `content/puzzles.json` re-authors three poses for the smooth hollow route: clear_pipe's solution pipe at (0.2, 3.1, 0) turned −37.5° and its receiver at (1.7, 0.6, 0); joined_pipe's receiver at (0.2, 0.6, 0). The other poses stand. |
| 18 | joined_pipe's straight pipe uses the narrow bend assistance windows: position 0.6 / 0.5 m and rotation 5° / 2° with equal correction caps at precision 0 / 0.45. | `tools/Campaign/Program.cs@a6c914e:L564-L573` | carry forward the values (Story 15.0); do not carry forward the string `part.Kind` test. |
| 19 | A trampoline rebound traverses the bore only when trampoline and pipe align; a pipe moved 2 m in depth is missed. | `CuriousContraptions.tests/TrampolinePipeTests.cs@a6c914e:L32-L90` | carry forward as an integration scenario owned with CAT-065; no current native test places both. |
| 20 | Workshop UI: the contextual "Resize mode" button is hidden until a pipe (or wall) is placed and selected; pressing it puts the gizmo in resize mode; a Shift-drag of the length handle by half a world unit increases the length by 1 (symmetric about the centre); one "↺ Undo" restores the original length and keeps the part. | `CuriousContraptions.tests/WorkshopInteractionTests.cs@a6c914e:L703-L741` | carry forward (contextual resize, one gesture is one Undo); queued Chrome case. |
| 21 | Workshop UI: with a locked fixed pipe at (−2, 3, 0), placing a new pipe by clicking near (1.9, 3, 0) snaps it to x 1.78 ± 0.001, y 3 ± 0.001 (mouths touching, 3.78 m apart); one "↺ Undo" removes only the new tube and keeps the fixed one. | `CuriousContraptions.tests/WorkshopInteractionTests.cs@a6c914e:L742-L765` | carry forward (snap on placement, one gesture is one Undo); queued Chrome case. |
| 22 | A pipe at (0, 4, 0): light crossing the clear shell sideways is unobstructed (5 m); air crossing it sideways is blocked by the shell (1.1–1.4 m); air along the axis through the bore is open (10 m). | `CuriousContraptions.tests/WindChimeTests.cs@a6c914e:L211-L215` | carry forward. The air controls are delivered (`FanGuidanceTests.TransparentPipeBlocksAcrossItsShellButAllowsItsOpenBore`); light is owed with ENGINE-OPTICAL. The fan/chime fixture itself belongs to CAT-028/CAT-069. |
| 23 | clear_pipe with its authored solution at precision 0 is won: the ball is captured by the Receiver within 1,200 ticks. | `CuriousContraptions.tests/FlightCampaignTests.cs@a6c914e:L15-L40` | carry forward the win; delivered by `LaunchLessonSolveTests`. The 1,200-tick bound is not carried: a lesson deadline is a stall guard (game-fidelity policy). |

Historical files harvested (retrievable at the pinned baseline; not retained implementation prerequisites):
- `engine/gpu/WorkshopPipe.cs` (removed by Story 7.4; Story 6.6 rebuilt fresh)
- `engine/gpu/AnnularProfile.cs` (removed with its retired `AnnularFeature` by Story 7.4)
- `parts/PipePart.cs`
- `parts/PipeArt.cs`
- `parts/scenes/pipe.tscn` (script binding only)
- `parts/catalog/pipe.tres`
- `engine/PipeDimensionsResource.cs`
- `engine/TubeMouth.cs`
- `engine/TubeProxy.cs`
- `engine/SceneCollisionGeometry.cs`
- `engine/physics/HollowGeometry.cs`
- `CuriousContraptions.tests/PipeTests.cs`
- `CuriousContraptions.tests/PipeResizeTests.cs`
- `CuriousContraptions.tests/TubePlacementSnapTests.cs`
- `CuriousContraptions.tests/WorkshopPipeTests.cs`
- `CuriousContraptions.tests/ConstructionLifecycleTests.cs`
- `CuriousContraptions.tests/TubeBoxIntersectionTests.cs`
- `CuriousContraptions.tests/TrampolinePipeTests.cs`
- `CuriousContraptions.tests/FlightCampaignTests.cs`
- `CuriousContraptions.tests/WorkshopInteractionTests.cs` (L703-L765)
- `CuriousContraptions.tests/WindChimeTests.cs` (L211-L215)
- `tools/Campaign/Program.cs`
- `reference/pipe/` (Program.cs; 64 WGSL diagnostics, three inputs JSON files and the csproj hold no further element knowledge)

## 5. Acceptance outline

Acceptance: [CAT-048](../requirements.md#current-cat-048) and [retained behaviour](../requirements.md#todo-125). Every row runs the production simulation or the production authoring query on this declaration; none needs a code path that exists only for the pipe.

| Given / When / Then | Proof | Control and boundary |
| --- | --- | --- |
| Given a tilted pipe under a falling ball, when Run, then the ball travels the bore without catching, added speed or passing through the wall, and reaches the Receiver | Existing: `HollowElementTests.ABallRollsThroughAStraightPipeOfEveryLengthOnItsWallWithoutCatching`; `LaunchLessonSolveTests.AuthoredLessonSolvesAndOneMissingCausePreventsTheGoal` (`clear_pipe`) | Control: without the pipe the goal is not reached. Boundary: lengths 1, 3.6 and 8 m |
| Given a fast or turned entry, then a fitting payload passes and never crosses the wall | Existing: `FastAxialLaunchPassesOnlyTheBoreOrClearAir`; `ActualPackageWallsPassAFittingPayloadAndStopAnOversizedPayload` | Control: a 0.75 m sphere stops at the mouth. The oversize proof uses a law-scene sphere against the package colliders, not a placed ball with `radius` set. Boundary: 64 m/s, the envelope's limit |
| Given a drop on the collar or the outer wall, then the ball stays outside and returns no more than the pair restitution | Existing: `ZeroRestitutionPackageControlDoesNotReturnImpactEnergy`; `HollowElementTests.AHollowWallOverlapIsRefusedAtomicallyWhileBoreAndOutsideRemainClear` | Control: restitution 0 returns at most g/480 |
| Length 1 to 8 m resizes shell, collars, mouths and art together; values outside are refused | Existing: `WorkshopPipeLengthTests` (section 3) | Boundary: 0.999 and 8.001 m, NaN, infinities |
| Mouths snap within 0.45 m and 20°, by the shortest arc keeping roll; occupied, unequal-bore, non-opposed and locked candidates are refused; the query changes nothing | Existing: `TubeMouthSnapTests` (section 3) | Boundary: 0.44 and 0.46 m; 19° and 21°; bore difference 5e−7 and 2e−6 m |
| A ball crosses a snapped joint without a step | Existing: `ABallCrossesASnappedJointWithoutSnagging`; `ABallRollingThroughTwoSnappedPipesKeepsItsHeightAcrossTheJoint` | Control: `JoinedPipeTests.AnUnsnappedPipeLeavesTheGoalUnsolved` |
| Run/Reset and save/load restore length and pose | Existing: `AConnectedHollowPassageRestoresConstructionAndRepeatsAfterResetAndSaveLoad`; `TheSaveCodecRoundTripsTheLength` | Decoded field values, no buffer comparison |
| A wrong-kind connection is impossible | The declaration has no port (`D:44`), so no connection can name the pipe; no test is needed beyond the generic port-admission tests | — |
| Air is blocked across the shell and open along the bore | Existing: `FanGuidanceTests.TransparentPipeBlocksAcrossItsShellButAllowsItsOpenBore` | In stream and out of stream |
| **Planned.** Given a Run in progress or paused, when a pipe is moved near a free mouth, then no snap is offered and the construction is unchanged; after Reset the same move snaps | `TubeMouthSnapTests.ASnapIsRefusedWhileRunningOrPausedAndReturnsAfterReset` | Control: the same move in Build snaps. Boundary: Paused straight after Run. Typed refusal and decoded construction; no float tolerance |
| **Planned.** Given a Run in progress or paused, when `length` is edited, then the edit is refused and the construction is unchanged; it applies after Reset | `WorkshopPipeLengthTests.AResizeIsRefusedWhileRunningOrPausedWithoutChangingTheConstruction` | Control: the same edit in Build applies. Boundary: Paused straight after Run |
| Light: the collar blocks and the shell transmits | Owed with ENGINE-OPTICAL and Story 13.1: a beam through the shell reaches a detector | Control: a beam on a collar does not reach it; a ball inside the bore blocks it. Optical tolerances per coordinator decision 21 (±2 % power, 1 s guard, 0.5° direction, ±0.02 m end points) |
| A pipe 0.48 m off in depth wins at Forgiving and Balanced, not at Precise | Owed with Story 15.0 | — |

Both planned tests must reach the refusal through the production construction-edit interface. Queued for the post-catalogue Chrome phase: construction through the palette, the resize handle, Cancel, one Undo, separation and rejoin. The trampoline scenario (fact 19) is owned with CAT-065.

## 6. Open questions

Resolved:

- **Oversized payload (D12).** A ball's declared `radius` may exceed the bore, so no special part is needed. The delivered proof is the 0.75 m law-scene sphere.
- **Wrong-bore control.** The Funnel's 1.3 m inlet is the unequal bore: `TubeMouthSnapTests.ThePipeSnapsToTheFunnelOutletButNotItsWideInlet`.
- **Fresh build (owner, 10 October 2026).** The twelve parked legacy files were deleted and the part rebuilt on the current engine. Nothing legacy is a prerequisite.
- **Restitution 0.15.** Pair restitution is a product, so 1.0 would be neutral and wrong for this part; `engine-hollow-shapes` set 0.15 on all four tube parts.
- **Rails (coordinator, 22 November 2026; standing delegation).** `DESIGN.md:358` asks for "two thin navy rails" and Story 9R.19 owes them. Each rail is a navy `#293954` cylinder of radius 0.018 m centred at radial distance 0.675 m, so it lies wholly inside the 0.65 to 0.70 m glass wall and is seen through it. It is decorative, enclosed by the shell's hollow collider, and takes no collider. The pipe has two, at z = ±0.675 m, each 3.78 m long at the default and scaled with `length` by a ScaleY binding with the shell's multiplier and offset (bindings become 5). `VisibleSolidColliderTests` lists them as decorative. Reason: a proud rail would be a solid that catches balls for no gameplay purpose; a rail inside the glass is still a rail.
- **Story 9R.23 as native proofs (author decision, kept).** The story's text asks for Chrome proofs, which cannot gate a slice before the catalogue is complete. Its clauses are proven natively: oversize, entry speed and edge angles, rim, sidewall and non-default lengths by the tests of section 3; the two run-state refusals by the planned tests. The Chrome proof is queued, not dropped.
- **Story 15.0 scope.** Its text names CAT-049 and CAT-050. The pipe's depth-error controls in `clear_pipe` and `joined_pipe` are the same mechanism; the batch summary proposes the added sentence.

Needs the owner: nothing.

## 7. Integration

- **Collaborators.** Basketball, Bowling ball, Tennis ball and Baseball: four delivered balls, 0.68, 0.56, 0.5 and 0.54 m across (the Baseball at its band, the repair values of coordinator decision 36), all passing the bore, 1.3 m across (the Baseball is delivered on published main, id `baseball`). Both bends, the Funnel outlet, the Ball detector and the Powered gate have equal 0.65 m mouths. Receiver, Fan (air occlusion) and Trampoline are delivered. Light sources are not delivered.
- **Campaign.** `clear_pipe` and `joined_pipe` in `content/puzzles.json`; chapter 3 "Pipe Dreams" of the [150-level progression](../requirements.md#sequence-task-315).
- **Coverage binding.** Catalogue key `pipe` in `docs/coverage/catalogue-elements.json`.
- **Lesson fixture.** Not needed: both levels place the delivered part.

## 8. Delivery

- **Records.** `review-6-6-straight-pipe.md`, `review-6-7-clear-pipe.md`, `review-9a-pkg-2.md`, `review-9r-6-typed-mouth-snapping.md`, `review-9r-7-joined-pipe-level-playable.md` and `spec-engine-hollow-shapes.md` under `_bmad-output/implementation-artifacts/`. `sprint-status.yaml` holds 6.6 and 6.7 as in-progress, owned by 9R.23.
- **Residual slices, in order.** Every residual slice is blocked on the engine programme (owner decision 3a, 22 November 2026), which precedes every further element.
  1. Story 9R.23, tests only: the two planned tests of section 5, then the dated Pass addenda for Stories 6.6 and 6.7. All six review layers: the probed capability has no exercising test yet, so the slice is not data-only.
  2. Story 9R.19: the two rails (scene nodes, two presentation bindings, the `VisibleSolidColliderTests` row) and double-sided glass. Files: `elements/Pipe/pipe.tscn`, `elements/Pipe/PipeElementDeclaration.cs`, `CuriousContraptions.tests/VisibleSolidColliderTests.cs`, `CuriousContraptions.tests/WorkshopPipeLengthTests.cs`. Data-only tier.
  3. Light, after ENGINE-OPTICAL, with Story 13.1.
  4. Difficulty controls in Story 15.0.
- **Disqualifiers.** A planned test that fails through the delivered package (stop and plan a repair). A run-state refusal that can be reached only through `ui/` code: the slice's probe records it and the proof joins the queued Chrome cases. A rail drawn proud of the wall. Any change to the bore radius 0.65 m or the mouth rule, which every tube part and D12 depend on.
