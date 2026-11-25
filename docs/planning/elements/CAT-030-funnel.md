# CAT-030 · funnel — declaration readiness spec

**Status (22 November 2026).** Delivered: package `elements/Funnel`, with true hollow colliders published at `eacf561d` by [`engine-hollow-shapes`](../../../_bmad-output/planning-artifacts/epics.md#story-engine-hollow-shapes). Section 2 is read from the package; section 3 is the capability probe of this date. Owed: one native proof (Story 9R.23), rails and double-sided glass (Story 9R.19) and light (ENGINE-OPTICAL); CAT-030 stays open until light is proven. Every residual slice is blocked on the engine programme (owner decision 3a, 22 November 2026); the part's engine gaps are owned by `ENGINE-OPTICAL` (its clear shell and stubs declare Transmit (1, 1, 1), EL-176 section 10) and `ENGINE-CAPABILITY-INSTANCES` (mouth snapping as declared snap sockets, audit E5). Browser qualification is pending until the post-catalogue phase. Citations ending `@a6c914e` in section 4 describe the legacy source, not current code.

## 1. Identity

| Field | Value |
| --- | --- |
| CAT ID / kind | CAT-030 · `funnel` (display name Funnel) |
| Requirement | [CAT-030](../requirements.md#current-cat-030) |
| Owner key | [CAT-030 delivery owner](../../../_bmad-output/planning-artifacts/epics.md#catalogue-slice-ownership) |
| Disposition | Delivered: package id `funnel`, directory `elements/Funnel`. |
| Mapped identities | None directly. EL-005 Liquid funnel is a separate liquid element. |
| Roadmap | Story 6.11 (delivered; one clause proof owned by Story 9R.23). |
| Levels | None in `content/puzzles.json`; Free workshop only. See [CAT-030-I consumers](../invest/current-consumers.md#cat-030-i). |

## 2. Declaration

Delivered declaration. `D:` is `elements/Funnel/FunnelElementDeclaration.cs`; `S:` is `elements/Funnel/funnel.tscn`. The part lies along local X with the wide inlet at −X.

| Item | Delivered value and source |
| --- | --- |
| Interfaces | `IMechanicalElement`, `ISceneGeometryElement` `D:13`. `SupportedDomains` is Mechanical only `D:42`. |
| Catalogue | Id `funnel`, title Funnel, category Motion, palette order 19, colour (0.4, 0.72, 0.79) `D:18-27`. |
| Body | One static body (`KinematicMode.Static` `D:44`); footprint 1 body, 0 moving, 0 constraints `D:29`; mass 0 and zero moments `D:48-54`; `PayloadPolicy.None` `D:46`; no anchor points `D:56`. |
| Colliders | Baked from hidden scene proxies (`Colliders => []` `D:33`), all on the one body. `col_shell`: Frustum 1.8 m long, outer radius 1.35 m at the inlet to 0.7 m at the outlet, radial wall 0.05 m (bore radii 1.3 m and 0.65 m) `S:12-16`, `S:88-92`. `col_stub_start`: Tube 0.09 m long, outer 1.35 m, wall 0.05 m; `col_stub_end`: Tube 0.09 m long, outer 0.7 m, wall 0.05 m `S:22-36`, `S:94-104`. `col_collar_start`: full Torus, inner 1.3 m, outer 1.43 m, at x = −0.9 m; `col_collar_end`: full Torus, inner 0.65 m, outer 0.78 m, at x = +0.9 m `S:42-52`, `S:106-114`. |
| Material | Restitution 0.15, friction 0.3, rolling resistance 0, bounce threshold 0.1 m/s `D:35-38`. |
| Parameters, ports, stores, events, domain nodes | None `D:27`, `D:31`, `D:40`. Fixed geometry: no resize axis `D:15`. |
| Tube mouths | Start (inlet) at (−0.99, 0, 0) facing −X, bore radius 1.3 m; End (outlet) at (0.99, 0, 0) facing +X, bore radius 0.65 m `S:80-86`. Each lies 0.09 m beyond its collar, the family's mouth rule (CAT-048 section 2). Only the outlet equals the family bore, so only the outlet snaps. |
| Presentation | No binding of any kind `D:15-16`. Pick radius 1.75 m. |
| Art | `Visual/Visual_Shell`: open tapered cylinder, radius 1.35 m at the inlet to 0.7 m at the outlet, 1.8 m, 64 segments, cyan at alpha 0.16 `S:12-20`, `S:60-62`. `Visual_Stub_Start`, `Visual_Stub_End`: open cylinders 0.09 m long at radii 1.35 m and 0.7 m `S:64-70`. `Visual_Collar_Start`: cream torus 1.3 to 1.43 m; `Visual_Collar_End`: cream torus 0.65 to 0.78 m `S:72-78`. No rails yet (section 6). Told from the Clear pipe by its taper and its wide inlet collar. |

### 2.1 Engine conformance

The package is declaration data and a scene. It has no script, callback or update loop, and a search of `engine/`, the `CuriousContraptions.*` engine projects and `ui/` finds no code that names `funnel` or its class (audit of 22 November, section 5; repeated for this repair). One refactor is owed by a shared contract the part uses, not by the part: its two mouths are the tube family's fixed `TubeEnd { Start, End }` recipe with engine snap constants (audit E5); `ENGINE-CAPABILITY-INSTANCES` makes them a declared list of snap sockets.

**Compiled counts.** Bodies 1 (static). Colliders 5: 1 Frustum, 2 Tube, 2 Torus. Joints 0. Drives 0. Sensors 0. Domain nodes 0. Stores 0. Bindings 0. All counts are fixed at compile time.

**Why five colliders.** Visible = solid, one collider per drawn solid: the tapered wall between the collars, a stub carrying the wall 9 cm to each mouth plane (so a snapped pipe's bore meets the outlet without a gap), and a collar standing 0.08 m proud at each end.

| Art node | Classification | Moved by |
| --- | --- | --- |
| `Visual/Visual_Shell` | Hollow; matches `col_shell` (`CuriousContraptions.tests/VisibleSolidColliderTests.cs:158-159`) | Nothing |
| `Visual/Visual_Stub_Start`, `_End` | Hollow; match the stub Tubes | Nothing |
| `Visual/Visual_Collar_Start`, `_End` | Hollow; match the collar tori | Nothing |

| Behaviour | Generic law | Declared values |
| --- | --- | --- |
| A ball dropped anywhere in the inlet is gathered to the outlet | Gravity plus contact normal rows against the Frustum's sloping inner surface; no attraction, force or transport is declared | Bore radius 1.3 m narrowing to 0.65 m over 1.8 m |
| The ball slides, rolls or circles on the wall | Pair friction, the geometric mean of the two materials (`CuriousContraptions.Simulation/WorkshopConstraintSolver.cs:119`) | Friction 0.3; rolling resistance 0 |
| A rim or outside strike barely rebounds | Pair restitution, the product of the two materials (`WorkshopConstraintSolver.cs:117`) | Restitution 0.15; threshold 0.1 m/s |
| A payload wider than the outlet stops in the throat | Contact against the Frustum wall and the outlet stub | Outlet bore radius 0.65 m |
| A pipe joins the outlet and not the inlet | Authoring-time mouth query: bores must be equal | Mouth bores 0.65 m and 1.3 m |
| Air does not cross the shell | Airflow occlusion by solid colliders | The same five colliders |
| Light crosses the wall and stops at the collars | Optical transport with a declared surface response per collider (EL-176 section 10): missing, [ENGINE-OPTICAL](../../../_bmad-output/planning-artifacts/epics.md#story-engine-optical-beam-emission-transport-surface-interaction-and-reception) | `col_shell` and both stubs declare Transmit (1, 1, 1), a criterion of ENGINE-OPTICAL (EL-176 section 10, item 6); the collars keep the default Absorb |

**Performance fitness.** The body is static with no sensor or field region. Five primitive hollow colliders replace the deleted box ring. Tunnelling through the 5 cm wall is excluded by the engine's general means, speculative contacts at 480 Hz substeps (`docs/gpu-f32-physics.md:299-306`; envelope `:375`). Nothing costs work in proportion to the scene. Engine gap: optical response (ENGINE-OPTICAL).

## 3. Engine capabilities

Families from the [element map row](../general-engine-element-map.md): ContactImpulse, EnvironmentState, FiniteLedger, GeometryQuery, RigidBodyDynamics, SlidingFriction.

Capability probe, 22 November 2026. Tests are in `CuriousContraptions.tests/`. Counts: 6 present, 1 present but untested, 0 narrower, 1 missing.

| Capability | Status | Evidence or owner |
| --- | --- | --- |
| Frustum wall, stubs and collars: an off-centre inlet drop leaves through the throat inside the wall; a tangential release circles the wall and exits; Reset and save/load replay | present | `FunnelTests.FunnelScene_ExtractsItsFrustumStubsCollarsAndTwoMouthPortLocators`; `HollowElementTests.ABallDroppedIntoTheFunnelsInletLeavesThroughItsThroatInsideTheWall` (0.6 m off the axis), `.ATangentialFunnelReleaseTravelsAroundTheWallAndExitsWithFullSphereClearance` |
| Rim, end-face and outside contact | present | `WorkshopPhysicsLawHollowTests.SpheresMeetTheFrustumEndFaceAndBothRims`, `.GravityPressesSpheresOntoFrustumFacesAndRimsWithinTheRestingEnvelope`, `.ZeroRestitutionPackageControlDoesNotReturnImpactEnergy` (a drop on this package's collar) |
| A fitting payload passes; an oversized one stops at the outlet | present | `WorkshopPhysicsLawHollowTests.ActualPackageWallsPassAFittingPayloadAndStopAnOversizedPayload` (`Funnel` rows, the package turned −90°); `FunnelTests.BoreGeometry_AdmitsBallRadiusUnder65cm_AndBlocksOversizedBallAtOutlet` |
| Outlet-only snapping; continuous joint to a pipe and to a bend | present | `TubeMouthSnapTests.ThePipeSnapsToTheFunnelOutletButNotItsWideInlet`, `.ABallCrossesASnappedJointWithoutSnagging` (row `FunnelOutletToPipe`); `HollowElementTests.ABallCrossesASnappedStraightOrFunnelToBendJointWithoutAStep` |
| Compile, capacity, save and load | present | `FunnelTests.WorkshopPhysicsCompiler_CompilesFunnelIntoStaticBodyWithItsHollowColliders`, `.WorkbenchCapacity_AdmitsFunnelsWithoutColliderLimit`, `.SaveCodec_RoundTripsFunnelAndPreservesDeclaredColliders` |
| Air blocked by the tapered shell only when it is in the stream | present | `FanGuidanceTests.TransparentCurvedAndTaperedSolidWallsOccludeOnlyWhenInTheStream` (rows `Funnel`) |
| High-speed passage through a funnel turned off the world axes | present, untested | The 64 m/s law test (`FastAxialLaunchPassesOnlyTheBoreOrClearAir`) uses a Tube, not a Frustum; planned in section 5 |
| Light through the clear wall, opaque collars | missing | [ENGINE-OPTICAL](../../../_bmad-output/planning-artifacts/epics.md#story-engine-optical-beam-emission-transport-surface-interaction-and-reception): Transmit (1, 1, 1) on the shell and stubs is a criterion of that story (EL-176 section 10, item 6); proof with Story 13.1 |

Depends on the Clear pipe's bore, collar and mouth rule (CAT-048) and on the bends for the bend join.

## 4. Legacy harvest

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| 1 | The funnel is passive: the sloping shell redirects actual sphere contacts. | `parts/FunnelPart.cs@a6c914e:L6-L6` | carry forward; delivered: the declaration has no force, drive or store. |
| 2 | Inlet pointing up (funnel at (0, 5, 0) rotated −90°), balls dropped from (0, 8, 0) and with ±0.75 m offset all exit through the narrow mouth: crossing local x 1.34 within 0.36 m of the axis; no step jumps 0.34 m or more; Reset restores the drop position. | `CuriousContraptions.tests/FunnelTests.cs@a6c914e:L29-L61` | carry forward (inlet/wall/outlet passage); delivered with a 0.6 m off-axis drop, Reset and a save/load replay. The legacy 0.75 m offset and step bound are not carried as numbers (section 6). |
| 3 | With zero gravity, a centred ball at 4 m/s (unrotated) or 40 m/s (rotated 20/30/40°) travels from local x −2 to 1.99–2.34 m with speed unchanged ± 0.003 m/s. | `CuriousContraptions.tests/FunnelTests.cs@a6c914e:L63-L82` | carry forward (high-speed and rotated payload; no energy added); the planned test of section 5. |
| 4 | A funnel at (0, 7, 0) feeding a straight pipe at (0, 4.12, 0), both rotated −90°: a ball dropped 0.75 m off axis exits the pipe below y 1.8 within 0.4 m of the axis. | `CuriousContraptions.tests/FunnelTests.cs@a6c914e:L84-L110` | carry forward (straight join); delivered by the `FunnelOutletToPipe` row. |
| 5 | A pipe at (2.95, 5, 0) snaps to the outlet; the same pipe at (−2.95, 5, 0) does not snap to the wide inlet; the inlet mouth radius equals 1.3. | `CuriousContraptions.tests/FunnelTests.cs@a6c914e:L112-L126` | carry forward (compatible tube mouths; wrong-bore rejection); delivered. |
| 6 | Light along z through the glass is unobstructed for 6 m; a ball dropped onto the outside at 4 m/s never overlaps the shell and gains no speed. | `CuriousContraptions.tests/FunnelTests.cs@a6c914e:L128-L151` | carry forward. The solid shell is delivered (law tests of section 3); light is owed with ENGINE-OPTICAL. |
| 7 | Occupancy of a frustum: a box in the bore, inlet hole, outlet hole or beyond the end is clear; boxes in the shell, on the inlet or outlet rim, or surrounding it overlap; invariant under rigid transforms. | `CuriousContraptions.tests/FrustumBoxIntersectionTests.cs@a6c914e:L8-L39` | carry forward (rim and opening geometry); the contact side is delivered (`WorkshopPhysicsLawHollowTests.BoxAndCylinderMeetTubeAndFrustumEndFaces`). |
| 8 | The frustum wall was a ring of convex segments within 0.005 m surface error; frustums are never optically opaque (collars are opaque tubes). | `engine/physics/HollowGeometry.cs@a6c914e:L83-L108`; `engine/SceneCollisionGeometry.cs@a6c914e:L75-L77` | carry forward the opacity facts (ENGINE-OPTICAL); do not carry forward the CPU segmentation or its 0.005 m bound. |
| 9 | Test fixtures use an enum role → catalogue string boundary that rejects undefined roles. | `CuriousContraptions.tests/FunnelTests.cs@a6c914e:L11-L21` | carry forward the typed boundary rule only. |
| 10 | Solid shell blocks air: a funnel rotated (20, 35, 10) blocks an air trace from 2 m outside its wall at 0.7–1.4 m while light passes the full 4 m. The same fixture is captured for the fan in CAT-028 (`WindChimeTests.cs` L212-L237). | `CuriousContraptions.tests/WindChimeTests.cs@a6c914e:L220-L237` | carry forward. The air control is delivered (`FanGuidanceTests`); the light control is owed with ENGINE-OPTICAL. |

Files harvested:
- `parts/FunnelPart.cs`
- `parts/PipeArt.cs` (frustum mesh)
- `parts/scenes/funnel.tscn` (script binding only)
- `parts/catalog/funnel.tres`
- `engine/FrustumProxy.cs`
- `engine/TubeMouth.cs`
- `engine/physics/HollowGeometry.cs`
- `engine/SceneCollisionGeometry.cs`
- `CuriousContraptions.tests/FunnelTests.cs`
- `CuriousContraptions.tests/FrustumBoxIntersectionTests.cs`
- `CuriousContraptions.tests/WindChimeTests.cs` (L220-L237; cross-reference to CAT-028)

## 5. Acceptance outline

Acceptance: [CAT-030](../requirements.md#current-cat-030) and [retained behaviour](../requirements.md#todo-242). Every row runs the production simulation or the production authoring query on this declaration; none needs a code path that exists only for the funnel.

| Given / When / Then | Proof | Control and boundary |
| --- | --- | --- |
| Given a funnel with its inlet up and a ball dropped off the axis, when Run, then the ball leaves through the throat and its centre never passes the wall | Existing: `HollowElementTests.ABallDroppedIntoTheFunnelsInletLeavesThroughItsThroatInsideTheWall` | 0.6 m off the axis; Reset; save and load replay |
| A tangential release circles the wall and exits with full clearance | Existing: `.ATangentialFunnelReleaseTravelsAroundTheWallAndExitsWithFullSphereClearance` | No attraction: the route is contact only |
| Rim, end-face and outside strikes stay outside and return no more than the pair restitution | Existing: the law tests of section 3 | Control: restitution 0 returns at most g/480 |
| A fitting payload passes; an oversized one stops at the outlet | Existing: `ActualPackageWallsPassAFittingPayloadAndStopAnOversizedPayload` (`Funnel`) | 0.34 m and 0.75 m law-scene spheres against the package colliders |
| A pipe snaps to the outlet only; a ball crosses the outlet joint into a pipe or a bend without a step | Existing: `ThePipeSnapsToTheFunnelOutletButNotItsWideInlet`; `ABallCrossesASnappedJointWithoutSnagging` (`FunnelOutletToPipe`); `ABallCrossesASnappedStraightOrFunnelToBendJointWithoutAStep` | Control: the same pipe offered to the 1.3 m inlet does not snap |
| **Planned.** Given the package colliders turned off the world axes, no gravity, and a 0.34 m sphere launched at 40 m/s along the axis from before the inlet, then it leaves beyond the outlet mouth plane, stays on the axis and gains no speed | `WorkshopPhysicsLawHollowTests.AFastAxialLaunchCrossesTheRotatedFunnelPackage` | Control: a 0.75 m sphere on the same launch ends short of the outlet mouth plane (no tunnelling through the throat). Boundary: 40 m/s, the legacy high-speed value, inside the 64 m/s envelope. Lateral offset within the file's existing `Resting + SurfaceError`; final speed not above the launch speed |
| Save and load keep the part and its colliders | Existing: `SaveCodec_RoundTripsFunnelAndPreservesDeclaredColliders` | Decoded values |
| A wrong-kind connection is impossible | The declaration has no port (`D:31`); the generic port-admission tests cover refusal | — |
| Air is blocked only when the shell is in the stream | Existing: `FanGuidanceTests.TransparentCurvedAndTaperedSolidWallsOccludeOnlyWhenInTheStream` | In stream and out of stream |
| Light crosses the clear wall and stops at the collars | Owed with ENGINE-OPTICAL and Story 13.1: a beam through the shell reaches a detector | Control: a beam on a collar does not reach it; a ball inside the shell blocks it. Optical tolerances per coordinator decision 21 (±2 % power, 1 s guard, 0.5° direction, ±0.02 m end points) |

Mesh and contact geometry match: `VisibleSolidColliderTests` (hollow rows). Queued for the post-catalogue Chrome phase: Free-workshop construction with a pipe and with a bend snapped to the outlet.

## 6. Open questions

Resolved:

- **Campaign level (D19).** No level is authored here; the campaign level slices own first use. A level is content, not element acceptance, and the requirement row names none.
- **Oversized payload (D12).** A ball's declared `radius` may exceed the outlet bore; the delivered proof is the 0.75 m law-scene sphere against the package colliders.
- **Off-centre drop (this repair).** The delivered drop is 0.6 m off the axis; the legacy 0.75 m is not re-proven. Both land on the sloping wall of a 1.3 m inlet, so no gameplay outcome is uncovered (game-fidelity policy: no rerun without an uncovered failure).
- **Rails (coordinator, 22 November 2026; standing delegation).** `DESIGN.md:353` asks for "two thin navy rails". Geometry as CAT-048 section 6: navy `#293954` cylinders of radius 0.018 m inside the glass wall; decorative, enclosed, no collider. Here the two rails run along the taper at mid-wall, from (−0.9, 0, ±1.325) to (0.9, 0, ±0.675) m: 1.914 m long, inclined 19.9° to the axis. The wall is 0.047 m thick across the rail, so the 0.036 m rail is enclosed. `VisibleSolidColliderTests` lists them as decorative.
- **Story 9R.19 clauses already met.** The glass narrows from the wide inlet to the outlet (`S:12-16`, `S:60-62`), and `VisibleSolidColliderTests` checks that art and collider radii agree. The story's "48 sides" is superseded by the delivered 64 segments, which the family's chord bound needs; the batch summary proposes the wording.
- **Story 9R.23 as native proofs (author decision, kept).** Rim, oversize, off-centre, mesh match and the bend join are proven natively; the fast rotated entry is the planned test. The Chrome proof is queued, not dropped.

Needs the owner: nothing.

## 7. Integration

- **Collaborators.** Clear pipe, both bends, Ball detector and Powered gate at the outlet (0.65 m); the four delivered balls (Basketball, Bowling ball, Tennis ball, Baseball), any of which the 2.6 m inlet takes and the 1.3 m outlet passes; Fan. All delivered. Light sources are not delivered.
- **Campaign.** No authored level places it; chapter 3 "Pipe Dreams" teaches it at plan level 23 in the [244-level progression](../requirements.md#sequence-task-315). A level that places it uses the delivered package. A level that offers it from the player's inventory needs no lesson-fixture story; a level that locks it as a fixture adds a `FIX-` row to the catalogue fixture table ([current consumers](../invest/current-consumers.md), read by `tools/Coverage/CurrentCatalogue.cs:100`), and needs [S8](../invest/delivery-batches.md#s8-lesson-fixtures-for-any-admitted-package) only for per-parameter or connection locks.
- **Coverage binding.** Catalogue key `funnel` in `docs/coverage/catalogue-elements.json`.

## 8. Delivery

- **Records.** `review-6-11-funnel.md`, `review-9a-pkg-2.md` and `spec-engine-hollow-shapes.md` under `_bmad-output/implementation-artifacts/`. `sprint-status.yaml` holds 6.11 as in-progress, owned by 9R.23.
- **Residual slices, in order.** Every residual slice is blocked on the engine programme (owner decision 3a, 22 November 2026), which precedes every further element.
  1. Story 9R.23, tests only: the planned test of section 5 (`CuriousContraptions.tests/WorkshopPhysicsLawHollowTests.cs`), then the dated Pass addendum for Story 6.11. All six review layers: the probed capability has no exercising test yet, so the slice is not data-only.
  2. Story 9R.19: two rail nodes, the `VisibleSolidColliderTests` row and double-sided glass. Files: `elements/Funnel/funnel.tscn`, `CuriousContraptions.tests/VisibleSolidColliderTests.cs`. Data-only tier.
  3. Light, after ENGINE-OPTICAL, with Story 13.1.
- **Disqualifiers.** The planned test failing through the delivered package (stop and plan a repair). A rail drawn proud of the wall. Any change to either bore or to the mouth rule.
