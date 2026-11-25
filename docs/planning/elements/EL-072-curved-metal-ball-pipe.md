# EL-072 · Curved metal ball pipe declaration readiness spec

**Current delivery (24 October 2026):** [EL-072 delivery slice](../invest/named-elements.md#element-072). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Story 7.0 named-identity spec. Baseline commit `a6c914e`. Legacy citations use `path@a6c914e:Lstart-Lend` and stay retrievable from git history after the Epic 7 purge. Values marked **proposed** are design values the owner may revise. Sections 1 to 3 and 5 to 8 were rewritten on 21 November 2026 against the current code (published main `8d1d68aa`); citations without `@a6c914e` are `path:line` in that checkout.

## 1. Identity

| Field | Value |
| --- | --- |
| ID | EL-072 |
| Name | Curved metal ball pipe |
| Type | Mechanical |
| Requirement anchor | [element-072](../requirements.md#element-072); scope index [todo-199](../requirements.md#todo-199) |
| Named entry | [named-elements.md#element-072](../invest/named-elements.md#element-072); proof owner S361, shared with EL-242, which has its own requirement row and sequence task (coordinator decision 24l, 23 November 2026). The entry also retains [CAT-049](CAT-049-pipe_bend_45.md) and [CAT-050](CAT-050-pipe_bend_90.md) acceptance. EL-072 itself holds the 90° bend; the 45° bend and the CAT-049 acceptance belong to EL-242 (coordinator decision 27, 23 November 2026; section 6), whose own spec is [EL-242](EL-242-45-metal-ball-pipe-bend.md) |
| Disposition | To deliver as a single element with one package, the 90° metal bend. It is a variant of the delivered clear 90° bend in declared values and art only (D12), not a duplicate. The 45° metal bend is [EL-242](EL-242-45-metal-ball-pipe-bend.md), its own ledger identity (coordinator decision 27) with its own spec, delivered with this one as the "Metal bends" Family slice |
| Planned package | `metal_pipe_bend_90`, directory `elements/MetalPipeBend90/`, class `MetalPipeBend90ElementDeclaration`. EL-242 declares `metal_pipe_bend_45` in its own spec |
| Coverage binding | Planned, not yet live (section 7), naming `metal_pipe_bend_90` only; blocked on S7 |
| delivery-batches class | [Section 1a](../invest/delivery-batches.md#1a-families-one-slice-each-if-the-probe-passes), family "Metal bends" with EL-242, and 2d (never a closure). **Confirmed by code** for geometry, mouths and material. **Corrected in one respect:** the row's own outcome (fast and queued cargo at seams) has no current test at speed or with more than one ball, so the slice is engine tier (section 3) |
| CAT specs refined | CAT-049 and CAT-050, delivered as `elements/PipeBend45/` and `elements/PipeBend90/`. The bore conflict the earlier draft raised is closed by D12 |
| Related identities | [EL-071 straight metal ball pipe](EL-071-straight-metal-ball-pipe.md) (same bore and material); [CAT-048 pipe](CAT-048-pipe.md) |
| Status | Not started; blocked on the engine programme (owner decision 3a), relying most on ENGINE-SPATIAL-QUERIES (hollow contact candidates and the segment cast that airflow and light use), ENGINE-CAPABILITY-INSTANCES (`TubeEnd` snapping) and ENGINE-ISLANDS-SLEEP (queued or jammed balls at rest); then S7. Row B11 also waits for ENGINE-OPTICAL |

## 2. Declaration

The package is the clear 90° bend's declaration (`elements/PipeBend90/PipeBend90ElementDeclaration.cs:11`) with different metadata, material values and art. The 45° sibling, [EL-242](EL-242-45-metal-ball-pipe-bend.md), is the clear 45° bend's declaration treated the same way and is declared in its own spec; the two differ only as the two clear bends do: sweep angle, the poses that follow from it, id, name and icon. Where this spec cites the clear 45° bend's files, it is because the two clear bends share their geometry and material.

- **Interfaces.** `IMechanicalElement` (`CuriousContraptions.Abstractions.Mechanical/MechanicalContracts.cs:7`) and `ISceneGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`). No parameterised geometry.
- **Body.** One static body, mass 0, footprint 1 body and 0 moving (`PipeBend45ElementDeclaration.cs:27`, `:42`–`:52`).
- **Colliders** (from the scene, as `elements/PipeBend90/pipe_bend_90.tscn:137`–`:155`): one Torus segment, centreline radius 2.4 m, outer tube radius 0.70 m, wall 0.05 m (bore radius 0.65 m), sweep 90° (`pipe_bend_90.tscn:142`); one Tube stub 0.09 m long from each end of the arc to its mouth, same radii; one full Torus collar at each end, centreline radius 0.715 m, tube radius 0.065 m.
- **Mouths.** Start and End, at the outer face of each stub, facing outward along the tangent, bore radius 0.65 m (`pipe_bend_45.tscn:99`, `:103`; `pipe_bend_90.tscn:131`, `:135`).
- **Material** (restitution, friction, rolling resistance, bounce threshold, `CuriousContraptions.Abstractions/ColliderDescriptor.cs:21`): `new(0.3f, 0.2f, 0f, 0.1f)`, equal to EL-071. The clear bends are `new(0.15f, 0.3f, 0f, 0.1f)` (`PipeBend45ElementDeclaration.cs:35`).
- **Parameters, ports, stores, sensors, events.** None. Fixed sweep and radius (D13, and as the clear bends).
- **Presentation.** Pick radius 2.2 m, no resize axis, no bindings (`PipeBend45ElementDeclaration.cs:13`).
- **Metadata.** Category `ElementCategory.Motion`, colour pale grey `(0.80, 0.85, 0.875)` `#ccd9df`, as EL-242.

| Package | Display name | Palette order | Description |
| --- | --- | --- | --- |
| `metal_pipe_bend_90` (EL-072) | "90° metal bend" | 142 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)) | "A 90-degree opaque steel curve. Turns rolling balls through a right angle; you cannot see them inside." |

### Art and solidity

| Node | Mesh and dimensions | Class |
| --- | --- | --- |
| `Visual/Visual_Shell/Visual_Shell_Seg_*` (16) | Uncapped cylinders, radius 0.70 m, 0.26 m long, opaque, matte and non-metallic (metallic 0, roughness 0.4–0.7, `DESIGN.md:79`) | Arc chord: together they draw the Torus segment `col_arc`, as the clear bends are classified at `CuriousContraptions.tests/VisibleSolidColliderTests.cs:154`–`:157` |
| `Visual/Visual_Shell/Visual_Stub_Start`, `Visual_Stub_End` | Uncapped cylinders, radius 0.70 m, 0.09 m long, opaque | Hollow, colliders `col_stub_start`, `col_stub_end` (Tube) |
| `Visual/Visual_Collar_Start`, `Visual/Visual_Collar_End` | Torus, inner radius 0.65 m, outer 0.78 m | Hollow, colliders `col_collar_start`, `col_collar_end` (Torus) |
| `Visual/Visual_Collar_Start/Band`, `Visual/Visual_Collar_End/Band` | Uncapped cylinder, radius 0.702 m, 0.06 m long, centred 0.10 m inboard of its collar along the tangent | Decorative: printed flat. Over its 0.06 m the arc leaves the band's straight line by under 0.004 m, inside the 0.005 m printed-flat bound of [spec-engine-art-solidity](../../../_bmad-output/implementation-artifacts/spec-engine-art-solidity.md) |

Colours as EL-071: shell pale grey `#ccd9df` (`DESIGN.md:248`), collars cream `#fff8e9`, bands navy `#1c2836` (`DESIGN.md:80`). Icons: the solid grey arc of that angle with two navy bands. **At a glance:** against the clear bend of the same angle, the opaque shell hides the cargo and carries a dark band beside each collar; against each other the two metal bends differ by sweep, as the clear bends do.

### Behaviour and numbers

- **Law.** Gravity and contact only; direction changes come from the bore wall. Pair laws are product restitution and geometric-mean friction (`docs/gpu-f32-physics.md:147`). Nothing adds speed: a ball leaves no faster than √(2·g·Δh) above its entry speed.
- **Bore 0.65 m, centreline 2.4 m.** Unchanged from the clear bends, so mouths snap to every delivered tube mouth (equal bores within 1e-6 m, `CuriousContraptions.tests/TubeMouthSnapTests.cs:269`) and the Basketball (0.35 m) turns with 0.30 m of clearance. The earlier draft's 1.2 m centreline belonged to the withdrawn 0.62 m gauge.
- **Material.** Restitution 0.3 and friction 0.2, starting values (D1), equal to EL-071 so a metal route is one material from end to end. Reasons are in EL-071 section 2.
- **Seams.** A seam is two snapped mouths: the stubs of both parts are straight for 0.09 m either side, so the bore is continuous. "Conserved" means every ball that enters a mouth leaves the other mouth exactly once, none passes through a wall and none is duplicated.
- **Fast.** The acceptance speeds are 8 m/s (the Bumper's target speed) and 20 m/s as the boundary. At 20 m/s a Bowling ball riding the outer wall (path radius 2.4 + 0.65 − 0.28 = 2.77 m) carries a centripetal load of about 144 m/s², and the speed is well inside the engine's 64 m/s envelope (`CuriousContraptions.tests/WorkshopPhysicsLawHollowTests.cs:822`). The legacy 40 m/s case (fact 2) is historical evidence, not an obligation.
- **Queued.** Three Bowling balls (0.56 m across) in contact in one bore.
- **Tick order.** No signal, optical or acoustic read.

## 3. Engine capabilities (capability probe)

| Capability | Status | Symbol | Exercising test |
| --- | --- | --- | --- |
| Static Torus segment, Tube stub and Torus collar colliders extracted from a scene | *present* | `ColliderShapeKind` `CuriousContraptions.Abstractions/PhysicsDeclarationValues.cs:12`; `pipe_bend_45.tscn:105` | `CuriousContraptions.tests/PipeBend45Tests.cs:49`, `:90`; `CuriousContraptions.tests/PipeBend90Tests.cs:49`, `:90` |
| A ball rides the bend wall and leaves within the derived speed bounds | *present* | Hollow narrowphase, `CuriousContraptions.Simulation/WorkshopCollisionKernels.cs:1996` | `CuriousContraptions.tests/HollowElementTests.cs:356` (45° and 90°) |
| A ball crosses a stub-to-arc seam and a snapped part-to-part seam without a step | *present* | `TubeMouthDescriptor` `SceneGeometry.cs:39` | `WorkshopPhysicsLawHollowTests.cs:1132`; `HollowElementTests.cs:24`; `TubeMouthSnapTests.cs:405` |
| A fitting sphere passes and an oversized one jams at a bend mouth | *present* | as above | `WorkshopPhysicsLawHollowTests.cs:618` (both delivered bends), `:216` |
| Mouths snap only on equal bores | *present* | `TubeMouthDescriptor` | `TubeMouthSnapTests.cs:184`, `:269` |
| Per-package static contact material | *present* | `SceneBodyGeometry` `SceneGeometry.cs:8` | `CuriousContraptions.tests/VisibleSolidColliderTests.cs:725`; `WorkshopPhysicsLawHollowTests.cs:687` |
| Reset and save/load of a connected hollow passage | *present* | — | `HollowElementTests.cs:86`; `PipeBend90Tests.cs:115` |
| A fast ball (8 to 20 m/s) through a torus segment and its seams | *present, untested* | same narrowphase; the only speed test is a straight Tube (`WorkshopPhysicsLawHollowTests.cs:822`) | none; the bend tests release from rest or 0.5 m. This slice adds the test |
| Several balls in contact inside one bore | *present, untested* | sphere–sphere and sphere–hollow rows are each tested alone | none found in `HollowElementTests.cs`, `TubeMouthSnapTests.cs` or `WorkshopPhysicsLawHollowTests.cs`. This slice adds the test |

Nothing is missing or narrower. The two untested rows are the row's own outcome; if their tests fail, the defect is in the generic hollow contact law and is repaired there, never by element-specific code. Light occlusion (row B11) follows [EL-176 section 10](EL-176-red-laser-emitter.md#10-optical-conventions-owned-here-every-optical-spec-cites-this-section): under convention 6 every participating collider occludes with the default Absorb response, so this package, which declares no optical response, absorbs, while the clear bends' clear shell colliders must declare Transmit (1, 1, 1) as a criterion of ENGINE-OPTICAL (their opaque cream collars keep the default Absorb). Airflow line of sight (row B10) uses ENGINE-SPATIAL-QUERIES' segment cast once delivered (convention 5).

**Element dependencies.** None undelivered. EL-071 is the natural seam partner but the Clear pipe serves for every seam test.

### Engine conformance

- **Declaration data only.** One static body with hollow colliders from the scene and one material. No ports, parameters, callbacks or engine branch on this part; it is the clear bend's declaration with other values.
- **Generic laws.** Turning, seam crossing, queueing and jamming are the hollow contact law with product restitution and geometric-mean friction; nothing transports a ball.
- **Layers.** Colliders: 1 Torus segment, 2 Tube stubs, 2 Torus collars (hidden `col_` proxies). Visuals: the opaque shell segments, stubs, collars and printed bands, none used as a collider. Animation: none; no art node moves. Visible = solid: the shell segments draw the arc collider, stubs and collars have their hollow colliders, and the bands are printed flat.
- **One-way flow and typing.** Nothing reads art. Values are f32; the part declares no string-keyed behaviour.
- **Flat state.** 1 static body, 5 colliders (3 Torus, 2 Tube), 0 joints, 0 drives, 0 sensors, 0 domain nodes, 0 stores, 0 bindings; 2 tube mouths as build-time snap data.
- **Performance.** Five primitive hollow colliders, the same as the delivered clear bend. Fast cargo is kept in by the engine's general 480 Hz substeps and speculative margins (`docs/gpu-f32-physics.md:375`), not by a per-part check. No island sleeping exists today; a queued or jammed ball at rest costs nothing only once ENGINE-ISLANDS-SLEEP (owner decision 3a) is delivered, ahead of this part.
- **Tests are scenarios.** Rows B1 to B11 run the production simulation on placed parts.

## 4. Sources and legacy

- Requirements row: "Fast and queued cargo remain conserved at seams." No variants.

| # | Fact | Citation | Disposition | Reason |
| --- | --- | --- | --- | --- |
| 1 | Clear bend: centreline radius 2.4 m, sweep from the `TubeBendAngle` enum {45, 90}, bend wall radius 0.70 with bore 0.65, collar tubes half-width 0.09 to radius 0.78 at both mouths. | `parts/PipeBendPart.cs@a6c914e:L8-L36`; `engine/MachineData.cs@a6c914e:L8-L8` | carry forward (geometry pattern, scaled) | Shape of a hollow bend. |
| 2 | Fast balls (6 and 40 m/s) cross a straight-to-bend or bend-to-bend seam and exit the outlet; per-tick travel ≤ speed × tick + 0.002 m and speed never exceeds entry + 0.002 m/s. | `CuriousContraptions.tests/PipeBendTests.cs@a6c914e:L40-L85` | carry forward (re-freeze tolerances under f32) | Directly the row's "fast cargo conserved at seams". |
| 3 | A ball at 6 m/s turns through a 45° or 90° bend without added energy (speed ≤ 6.001 m/s) and Reset restores its initial pose and velocity exactly. | `CuriousContraptions.tests/PipeBendTests.cs@a6c914e:L150-L193` | carry forward | Energy bound and Reset. |
| 4 | Bend mouths snap to straight pipes and other bends; a joined gravity route carries the ball across its seam. | `CuriousContraptions.tests/PipeBendTests.cs@a6c914e:L117-L117`, `CuriousContraptions.tests/PipeBendTests.cs@a6c914e:L198-L198` | carry forward | Seam joining. |
| 5 | Hollow compound distinguishes bore, shell exterior and open end. | `CuriousContraptions.tests/HollowSurfaceTests.cs@a6c914e:L61-L71` | carry forward (behaviour) | Real collision surfaces, no transport. |
| 6 | Curve tessellation steps (32 × 48) for collision. | `parts/PipeBendPart.cs@a6c914e:L37-L37` | do not carry forward | Legacy CPU hull approximation; the hollow SDF of Story 6.7 replaces it. |

**Files harvested:** `parts/PipeBendPart.cs`, `parts/catalog/pipe_bend_90.tres` (checked), `engine/MachineData.cs`, `engine/TubeMouth.cs`, `CuriousContraptions.tests/PipeBendTests.cs`, `CuriousContraptions.tests/HollowSurfaceTests.cs`. `CuriousContraptions.tests/TubePlacementSnapTests.cs` checked (snapping, harvested by CAT-049/050).

## 5. Acceptance outline

Requirement row: [element-072](../requirements.md#element-072). Fact 1's "scaled" and fact 2's per-tick travel bound are superseded: the geometry is unscaled (D12) and no per-tick trajectory is asserted (game fidelity policy).

- **Chrome UI recipe (pending, post-catalogue).** Place a Metal pipe tilted downhill, snap a 90° metal bend to its outlet and a second Metal pipe out of the bend into a Receiver; queue three Bowling balls on a Ramp feeding the first pipe. Verify placement and the snapped seams through real UI controls.

All tests are planned, in `CuriousContraptions.tests/MetalPipeBendTests.cs`, each a theory over the package ids the slice delivers (the 90° bend, and the 45° bend of [EL-242](EL-242-45-metal-ball-pipe-bend.md), whose own rows C1 to C11 are in its spec, in the Family slice) unless stated. "S4 row n" is the planned conformance theory; until it is delivered the named package test is written.

| # | Given / when / then | Planned test | Control | Boundary | Tolerance and stall guard |
| --- | --- | --- | --- | --- | --- |
| B1 | Given a bend with its start mouth up and a ball released 0.5 m above it, when Run advances, then the ball leaves the end mouth | `MetalPipeBendTests.ABallDroppedIntoEitherBendLeavesItsFarMouth` (Basketball, Bowling ball) | A bend lying flat and level: the ball does not leave | Both angles | Exit within 8 s of game time; exit speed no greater than √(2·g·Δh) + 0.05 m/s, so a bend that adds energy fails |
| B2 | **Fast cargo at seams.** Given a Clear pipe snapped to each mouth of the bend, the route lying in a horizontal plane, and a Bowling ball entering the first pipe along its axis at 8 m/s, then it crosses both seams and leaves the last mouth, and its centre is never farther than 0.65 − 0.28 + 0.01 m from the route centreline | `MetalPipeBendTests.AFastBallCrossesBothSeamsAndLeavesTheRoute` | The bend moved 1 m out of line (no snap): the ball leaves the first pipe and never enters the bend | 20 m/s | The 0.01 m allowance is the 1 cm barrier bound of the game-grade envelope; a larger excursion is a ball in the wall. Stall guard 5 s |
| B3 | **Queued cargo at seams.** Given the same route tilted 10° and three Bowling balls released in contact in the first pipe, then exactly three balls leave the last mouth, each once, and none is outside the bore between the mouths | `MetalPipeBendTests.ThreeQueuedBallsAllLeaveTheRouteAndNoneLeavesThroughAWall` | The outlet closed by a Wall: no ball leaves and all three are still inside the route when it settles | Balls released 0.6 m apart (not in contact) | Count is exact. Containment as B2. Stall guard 20 s, long enough for the last ball of a queue |
| B4 | Given a bend with its start mouth up and a Tennis ball with `radius` 0.75 m dropped 0.5 m above it, then it jams at the mouth: it comes to rest on the collar, its centre about √((0.75 + 0.065)² − 0.715²) = 0.39 m outside the collar plane, and never leaves the far mouth | `MetalPipeBendTests.ABallWiderThanTheBoreJamsAtTheMouth` | `radius` 0.60 m leaves the far mouth | 0.66 m jams; 0.61 m passes, clearing the 0.65 m bore by 0.04 m | As EL-071 row A2 |
| B5 | Each mouth snaps to a Metal pipe, a Clear pipe and the other metal bend; the Funnel's wide inlet does not snap | `MetalPipeBendTests.BothMouthsSnapToEqualBoresOnly`; two rows added at `TubeMouthSnapTests.cs:176` | Funnel inlet | Capture distance as `TubeMouthSnapTests.cs:141` | Exact joined pose (authored state) |
| B6 | A Basketball dropped 0.5 m onto the outside of the shell rebounds at 0.165 of its approach speed | `MetalPipeBendTests.ABallDroppedOnTheShellReboundsAtTheSteelRestitution` | The clear bend's 0.0825 | — | Ratio within ±0.03 |
| B7 | The shell is opaque, every art node is classified once, and every hollow or arc node has its collider | `MetalPipeBendArtTests.TheShellIsOpaqueAndEveryArtNodeHasItsClass`; two entries beside `VisibleSolidColliderTests.cs:154` | — | — | As that suite |
| B8 | Run, Reset and save/load restore both parts, their snapped poses and the balls | S4 rows 1 and 3, else `MetalPipeBendTests.ResetAndSaveLoadRestoreTheRouteAndItsCargo` (pattern `HollowElementTests.cs:86`) | — | — | Authored state exact |
| B9 | The package declares no port and no parameter; a construction that connects one of its ports or submits a parameter for it is refused | `MetalPipeBendTests.TheBendDeclaresNoPortsOrParametersAndRefusesBoth` | The unconnected part is accepted | — | Exact typed refusal. S4 rows 2 and 5 have nothing to iterate for a part with no parameter or port |
| B10 | **Air occlusion (retained CAT-049/050 control).** Given a Fan blowing across the bend at a Tennis ball behind it, then the ball is not pushed | `MetalPipeBendTests.TheShellBlocksAFansStream` (pattern `CuriousContraptions.tests/FanGuidanceTests.cs:45`) | Bend removed: the ball is pushed | — | Ball displacement under 0.02 m in 3 s against more than 0.3 m in the control |
| B11 | **Light occlusion (later partner, after `ENGINE-OPTICAL`).** A Laser beam aimed across the metal shell at a receiver behind it: the shell's colliders take the default Absorb response, so the receiver's committed reading is zero (optical conventions 6 and 8, [EL-176 section 10](EL-176-red-laser-emitter.md#10-optical-conventions-owned-here-every-optical-spec-cites-this-section)) | `MetalPipeBendTests.TheOpaqueShellBlocksABeamTheClearShellPasses` | The clear bend, whose shell declares Transmit (1, 1, 1): the receiver reads the beam's power | — | The optical tolerances (coordinator decision 21): reading within ±2 % of the emitted power in the control and zero here, after a 1 s guard; the identity stays open for this row until it runs |

The retained CAT-050 acceptance is rows B1, B5 and B8 on the 90° package, with its occlusion controls in B10 and B11. The retained CAT-049 acceptance is EL-242's rows C1, C5 and C8 on the 45° package ([EL-242 spec](EL-242-45-metal-ball-pipe-bend.md)), so nothing is dropped.

## 6. Open questions

**Resolved**

1. Is a 45° metal bend required: **yes, decided now.** The ledger entry retains both CAT-049 and CAT-050 acceptance and the campaign reserves "both bend angles" (`docs/planning/invest/campaign-reservations.md:73`), so D19's test (a requirement or lesson uses it) is met. It is delivered through its own ledger identity, EL-242 (item 4).
2. Centreline radius: **D12.** 2.4 m, the clear bends' value; the 1.2 m proposal went with the 0.62 m gauge.
3. Bore conflict with CAT-049/050: **D12.** 1.3 m bore kept; matches EL-071.
4. One identity, one package, and the 45° metal bend as its own identity: **coordinator decision 27, 23 November 2026.** A 45° bend is a placeable palette part, so it is its own ledger identity, not a companion package of EL-072: owner decision 15 covers only parts that are not placed by themselves. Published main also refuses two bindings for one identity (`tools/Coverage/NamedPackageSources.cs` on `origin/main`: one marker per spec, unique identity, unique task source). EL-072 therefore holds `metal_pipe_bend_90` only. The ledger admitted `metal_pipe_bend_45` as EL-242 on 23 November 2026, and CAT-049's retained acceptance moved with it.

**Needs the owner**

None.

## 7. Integration

- **Collaborators.** Delivered: the three balls, Clear pipe, both clear bends, Funnel, Powered gate, Ball detector, Receiver, Ramp, Wall. Same batch, not delivered: EL-071 (seam partner; not a prerequisite).
- **Campaign.** Both bend angles, first use levels 21–30, reuse 41–50, 111–120 and 136–150 (`campaign-reservations.md:73`). That reservation is met by the clear bends, so owner decision 24c places the metal bends in the light-interruption lesson at **52** (chapter 6), where their opacity is the lesson: introduction 52, practice 54, reuse 142 ([campaign coverage additions](../requirements.md#campaign-coverage-additions); brief in [campaign coverage](readiness/campaign-coverage.md#4-lesson-briefs)). This slice authors no level; a level that places a metal bend before it is playable needs the lesson-fixture story (S8).

### Coverage binding (planned, not yet live)

Published main binds a named-identity package through a final section of the identity's spec headed "Package source binding", read by `tools/Coverage/NamedPackageSources.cs` (on `origin/main`, `25df3cd5`; pattern: EL-239 Baseball). The section is not written here: the reader parses every EL spec that carries that heading and refuses one whose preconditions fail. **Blocked on the coverage follow-on story S7.**

| Field | Planned value |
| --- | --- |
| Package | `metal_pipe_bend_90` (EL-242's spec plans the binding for `metal_pipe_bend_45`) |
| Named identity | EL-072, `../invest/named-elements.md#element-072` |
| Acceptance | `todo-199` and `sequence-task-625` |
| Decision | S361; `scope-corrections.md` has no `s361` block |
| Mode records | `Configuration=Fixed` (the clear 90° bend's catalogue record also carries `TubeAngle=Degrees90`, requirement row [CAT-050](../requirements.md#current-cat-050); whether this binding adds it is confirmed at slice planning) |

What it lacks today: an `s361` anchor block in `docs/planning/invest/scope-corrections.md`, and its todo-199 anchor is a scope index, not immediately followed by its sequence-task-625 anchor in `requirements.md`. S7 widens the reader (owner decision 15), including for companion packages and TH, RAD and GAP identities.

## 8. Delivery

- **Slice shape.** The Family slice "Metal bends", always, never a single-element slice, with [EL-242](EL-242-45-metal-ball-pipe-bend.md) (two identities, each with its own rows and tests, differing only in declared sweep). Run it next to EL-071.
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), which includes ENGINE-SPATIAL-QUERIES, ENGINE-CAPABILITY-INSTANCES and ENGINE-ISLANDS-SLEEP; then S7 (the coverage follow-on, which widens the reader, owner decision 15). Row B11 alone also waits for ENGINE-OPTICAL. No other element.
- **Files added.** `elements/MetalPipeBend90/MetalPipeBend90.csproj`, `MetalPipeBend90ElementDeclaration.cs`, the scene `metal_pipe_bend_90.tscn`, `icon.svg` (EL-242's spec lists its own). Tests: `CuriousContraptions.tests/MetalPipeBendTests.cs`, `MetalPipeBendArtTests.cs`.
- **Files edited.** One row at `TubeMouthSnapTests.cs:176` and one entry beside `VisibleSolidColliderTests.cs:154`; until S3, `CuriousContraptions.slnx`, the trim list at `CSharpStaticGateTests.cs:687`–`:703` and the placement switch at `PackageRuntimeParityTests.cs:641` (generic list `:653`); the binding section of section 7; this spec and `TODO.md`.
- **Review tier.** Engine tier: two capabilities are *present, untested*.
- **Disqualifiers (the slice's probe re-checks).** The clear bends' bore, centreline and mouth poses are unchanged; rows B2 and B3 pass on the clear bends first (if they fail there, the repair is an engine slice that precedes this one); the printed bands pass the art-solidity declaration.
