# EL-071 · Straight metal ball pipe declaration readiness spec

**Current delivery (24 October 2026):** [EL-071 delivery slice](../invest/named-elements.md#element-071). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Story 7.0 named-identity spec. Baseline commit `a6c914e`. Legacy citations use `path@a6c914e:Lstart-Lend` and stay retrievable from git history after the Epic 7 purge. Values marked **proposed** are design values the owner may revise. Sections 1 to 3 and 5 to 8 were rewritten on 21 November 2026 against the current code (published main `8d1d68aa`); citations without `@a6c914e` are `path:line` in that checkout.

## 1. Identity

| Field | Value |
| --- | --- |
| ID | EL-071 |
| Name | Straight metal ball pipe |
| Type | Mechanical |
| Requirement anchor | [element-071](../requirements.md#element-071); scope index [todo-199](../requirements.md#todo-199) |
| Named entry | [named-elements.md#element-071](../invest/named-elements.md#element-071); proof owner S360. The entry also retains [CAT-048 acceptance](CAT-048-pipe.md) |
| Disposition | To deliver as a single element. It is a variant of the delivered Clear pipe in declared values and art only (D12), not a duplicate: its contact material, opacity, id and catalogue entry differ |
| Planned package | id `metal_pipe`, directory `elements/MetalPipe/`, class `MetalPipeElementDeclaration` |
| Coverage binding | Planned, not yet live (section 7); blocked on S7. No CAT number |
| delivery-batches class | [Section 2d](../invest/delivery-batches.md#2-duplicate-closures) (never a closure); in no family or run of section 1, so a single element. **Confirmed by code:** every capability the pipe needs is delivered and tested through the Clear pipe (section 3) |
| CAT spec refined | [CAT-048 pipe](CAT-048-pipe.md), delivered as `elements/Pipe/`. The bore conflict the earlier draft raised is closed by D12: the metal pipe keeps the 1.3 m bore and joins clear mouths |
| Related identities | [EL-072 curved metal ball pipe](EL-072-curved-metal-ball-pipe.md) and [EL-242 45° metal ball pipe bend](EL-242-45-metal-ball-pipe-bend.md) (same bore and material, join at seams; the Metal pipe snap rows against both bends run in this slice, coordinator decision 41); [CAT-049](CAT-049-pipe_bend_45.md) and [CAT-050](CAT-050-pipe_bend_90.md) clear bends. [EL-058 size grate](EL-058-size-grate.md) no longer shares a gauge with this pipe (the 0.62 m gauge is withdrawn) |
| Status | Not started; blocked on the engine programme (owner decision 3a), relying most on ENGINE-SPATIAL-QUERIES (hollow contact candidates and the segment cast that airflow and light use), ENGINE-CAPABILITY-INSTANCES (`TubeEnd` snapping) and ENGINE-ISLANDS-SLEEP (a jammed ball at rest); then S7. Row A11 also waits for ENGINE-OPTICAL |

## 2. Declaration

The declaration is the Clear pipe's, `elements/Pipe/PipeElementDeclaration.cs:13`, with different metadata, material values and art. Nothing else changes.

- **Interfaces.** `IMechanicalElement` (`CuriousContraptions.Abstractions.Mechanical/MechanicalContracts.cs:7`), `ISceneGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`), `IParameterizedGeometryElement` (`SceneGeometry.cs:29`).
- **Body.** One static body (`KinematicMode.Static`, mass 0, no gravity), footprint 1 body, 0 moving, as `PipeElementDeclaration.cs:42` and `:69`–`:79`.
- **Colliders** (from the scene, as `elements/Pipe/pipe.tscn:53`–`:67`): a Tube shell along local X, outer radius 0.70 m, wall 0.05 m, so bore radius 0.65 m, length `length` + 0.18 m (it reaches both mouths); and one full Torus collar at each end of the length, centreline radius 0.715 m, tube radius 0.065 m (0.65 to 0.78 m).
- **Collider and mouth bindings.** Identical to `PipeElementDeclaration.cs:46`–`:55`: shell half-length = 0.5·`length` + 0.09; collar offsets = ∓0.5·`length`; mouths at ∓(0.5·`length` + 0.09) on X, facing outward, bore radius 0.65 m (`pipe.tscn:47`, `:51`).
- **Material** (`MaterialProperties` order restitution, friction, rolling resistance, bounce threshold, `CuriousContraptions.Abstractions/ColliderDescriptor.cs:21`): `new(0.3f, 0.2f, 0f, 0.1f)`. The Clear pipe is `new(0.15f, 0.3f, 0f, 0.1f)` (`PipeElementDeclaration.cs:62`).
- **Stores, sensors, events.** None.
- **Ports.** None. Ball routing is contact; the two tube mouths are build-time snap points, not connections.

| Parameter | Kind | Unit | Default | Min | Max | Editor policy | Run edit |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `length` | Real | Metre | 3.6 | 1 | 8 | Reject (the descriptor default, as the Clear pipe) | BuildOnly |

- **Presentation.** As `PipeElementDeclaration.cs:21`–`:26`: pick radius 2.2 m, resize axis X bound to `length`; `Visual/Visual_Shell` ScaleY ← `length`·(1/3.78) + 0.18/3.78; `Visual/Visual_Collar_Start` LocalX ← −0.5·`length`; `Visual/Visual_Collar_End` LocalX ← +0.5·`length`. No run-time driver.
- **Metadata.** Id `metal_pipe`, display name "Metal pipe", category `ElementCategory.Motion`, palette order 141 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)), colour pale grey `(0.80, 0.85, 0.875)` `#ccd9df`, description "An opaque steel tube. Balls roll through the bore under gravity; you cannot see them until they leave."

### Art and solidity

| Node | Mesh and dimensions | Class |
| --- | --- | --- |
| `Visual/Visual_Shell` | Uncapped cylinder, radius 0.70 m, height 3.78 m at the default length, opaque (alpha 1), matte and non-metallic (metallic 0, roughness 0.4–0.7, `DESIGN.md:79`) | Hollow, collider `col_shell` (Tube) |
| `Visual/Visual_Collar_Start`, `Visual/Visual_Collar_End` | Torus, inner radius 0.65 m, outer 0.78 m | Hollow, colliders `col_collar_start`, `col_collar_end` (Torus) |
| `Visual/Visual_Collar_Start/Band`, `Visual/Visual_Collar_End/Band` | Uncapped cylinder, radius 0.702 m, 0.06 m long, centred 0.10 m inboard of its collar; a child of the collar node, so it follows the length | Decorative: printed flat, 0.002 m proud of the shell, inside the 0.005 m printed-flat bound of [spec-engine-art-solidity](../../../_bmad-output/implementation-artifacts/spec-engine-art-solidity.md) |

Colours: shell pale grey `#ccd9df` ("metal details use pale grey", `DESIGN.md:248`), collars cream `#fff8e9` (the Clear pipe's, `pipe.tscn:10`), bands navy `#1c2836` (`DESIGN.md:80`). Icon: a solid grey tube with two navy bands and a ball half entered at one mouth. **At a glance** against the Clear pipe: the shell is opaque, so the cargo and the far wall are hidden, and it carries a dark band beside each collar; the clear shell shows both and has no band. No viewing slot is drawn: a drawn opening over a solid collider would break visible = solid.

### Behaviour and numbers

- **Law.** Gravity and contact only. Pair laws are product restitution and geometric-mean friction (`docs/gpu-f32-physics.md:151`). No transport force, no logical transfer between mouths.
- **Bore 0.65 m radius.** Passes the Basketball (0.35 m with its band, `elements/Ball/ball.tscn:18`), the Bowling ball (0.28 m, `elements/BowlingBall/bowling.tscn:9`) and the default Tennis ball (0.25 m, `elements/TennisBall/TennisBallElementDeclaration.cs:18`). Kept equal to the clear family so mouths snap (bores must be equal within 1e-6 m, `CuriousContraptions.tests/TubeMouthSnapTests.cs:269`).
- **Oversized cargo.** Any sphere with radius above 0.65 m meets the mouth rim and stops. The control ball is a Tennis ball with `radius` 0.75 m (admitted range 1/16 to 16 m). This is D12; the code confirms it: the same geometry already stops a 0.75 m sphere at the Clear pipe's mouth (`CuriousContraptions.tests/WorkshopPhysicsLawHollowTests.cs:618`).
- **Restitution 0.3** (starting value, D1). A Basketball (0.55) rebounds from the steel at 0.165 of its approach speed, twice the clear shell's 0.0825, so cargo rattles audibly more. **Friction 0.2** (starting value): pair friction with a ball (0.3) is 0.245 against the clear pipe's 0.30, so a ball that enters sliding takes longer to grip. A ball already rolling accelerates at (5/7)·g·(sin θ − C_rr) in both pipes; the metal pipe is not a faster conveyor.
- **Length 1 to 8 m, default 3.6 m.** Same as the Clear pipe, so the scene geometry and bindings are reused unchanged and a metal pipe swaps for a clear one without re-fitting a route. (The earlier draft proposed 3 m; decided now for that reason.)
- **Tick order.** No signal, optical or acoustic read.

## 3. Engine capabilities (capability probe)

| Capability | Status | Symbol | Exercising test |
| --- | --- | --- | --- |
| Static Tube and Torus colliders extracted from a scene | *present* | `ColliderShapeKind` `CuriousContraptions.Abstractions/PhysicsDeclarationValues.cs:12`; `elements/Pipe/pipe.tscn:53` | `CuriousContraptions.tests/StraightPipeTests.cs:57`, `:94` |
| Length parameter bound to collider, mouths and art | *present* | `ColliderParameterBinding` `SceneGeometry.cs:27`; `TubeMouthParameterBinding` `:35` | `CuriousContraptions.tests/HollowElementTests.cs:299` (minimum, default, maximum length), `:139` (invalid length refused) |
| A fitting sphere passes the bore and an oversized one jams at the mouth, through real geometry | *present* | Hollow narrowphase, `CuriousContraptions.Simulation/WorkshopCollisionKernels.cs:1996` | `WorkshopPhysicsLawHollowTests.cs:618` (delivered pipe walls, radius 0.34 passes, 0.75 stops), `:216`, `:822` (64 m/s axial launch) |
| Tube mouths that snap only on equal bores | *present* | `TubeMouthDescriptor` `SceneGeometry.cs:39` | `TubeMouthSnapTests.cs:184`, `:269`, `:405` (a ball crosses each snapped joint) |
| Per-package static contact material | *present* | `SceneBodyGeometry` `SceneGeometry.cs:8` | `CuriousContraptions.tests/VisibleSolidColliderTests.cs:725` (rebound at the product restitution, pipe shell row `:647`); `WorkshopPhysicsLawHollowTests.cs:687` |
| A ball whose radius is a parameter (the oversized control) | *present* | `TennisBallElementDeclaration.cs:18`, `:33`–`:38` | `CuriousContraptions.tests/TennisBallTests.cs:29`, `:137` |
| Reset and save/load of a hollow passage and its cargo | *present* | — | `HollowElementTests.cs:86`; `StraightPipeTests.cs:119` |

Nothing is missing, narrower or untested. The earlier draft's "Missing" list (hollow colliders, mouth snapping) is delivered. Occlusion is a retained CAT-048 control. Air occlusion by a curved solid wall is present (`CuriousContraptions.tests/FanGuidanceTests.cs:45`) and is row A10; once ENGINE-SPATIAL-QUERIES is delivered, airflow line of sight uses its segment cast (optical convention 5, [EL-176 section 10](EL-176-red-laser-emitter.md#10-optical-conventions-owned-here-every-optical-spec-cites-this-section)). Light occlusion waits for [`ENGINE-OPTICAL`](../../../_bmad-output/planning-artifacts/epics.md#story-engine-optical-beam-emission-transport-surface-interaction-and-reception) and is later-partner row A11. Under optical convention 6 every participating collider occludes and the default response is Absorb: this package declares no optical response, so its Tube and Torus colliders absorb, while the Clear pipe's clear shell collider must declare Transmit (1, 1, 1) as a criterion of ENGINE-OPTICAL (its opaque cream collars keep the default Absorb).

**Element dependencies.** None undelivered. Collaborators: Basketball, Bowling ball, Tennis ball, Clear pipe, both clear bends, Funnel, Powered gate, Ball detector (all delivered, the tube parts listed at `TubeMouthSnapTests.cs:176`).

### Engine conformance

- **Declaration data only.** One static body with hollow colliders from the scene, one material and one length parameter. It is the Clear pipe's declaration with other values; no transport force, logical transfer, callback or engine branch.
- **Generic laws.** Rolling, jamming and rebound are the hollow contact law with product restitution and geometric-mean friction; mouths are build-time snap data.
- **Layers.** Colliders: 1 Tube and 2 Torus (hidden `col_` proxies). Visuals: opaque shell, collars and printed bands, none used as a collider. Animation: none; nothing moves any art node during a Run; shell and collars (with their bands) follow `length` through Build-time parameter bindings. Visible = solid: shell and collars each have their hollow collider; the bands are printed flat, and no viewing slot is drawn.
- **One-way flow and typing.** Nothing reads art. `length` is a typed f32 parameter in metres.
- **Flat state.** 1 static body, 3 colliders, 0 joints, 0 drives, 0 sensors, 0 domain nodes, 0 stores, 3 collider and mouth parameter bindings and 3 presentation parameter bindings; 2 tube mouths.
- **Performance.** Three primitive hollow colliders, the Clear pipe's count. Static. Fast cargo is kept in by the engine's general substeps and margins (`docs/gpu-f32-physics.md:396`). No island sleeping exists today; a jammed or resting ball costs nothing only once ENGINE-ISLANDS-SLEEP (owner decision 3a) is delivered, ahead of this part.
- **Tests are scenarios.** Rows A1 to A11 run the production simulation with delivered balls and tube parts.

## 4. Sources and legacy

- Requirements row: "Oversized cargo jams rather than traversing a logical connection." No variants.

| # | Fact | Citation | Disposition | Reason |
| --- | --- | --- | --- | --- |
| 1 | Clear pipe: bore radius 0.65 (diameter 1.3), length 1–8 m default 3.6, profile middle radius 0.70, end radius 0.78, end half-width 0.09; only length may change. | `engine/gpu/WorkshopPipe.cs@a6c914e:L5-L28` | carry forward (proportions and length rule) | Pattern for the metal pipe; the historical file was excluded from builds and is removed by Story 7.4. |
| 2 | A hollow tube keeps its reported minimum bore clearance (≥ 0.6475 m for r 0.65) so a ball passes through the open bore. | `CuriousContraptions.tests/HollowGeometryTests.cs@a6c914e:L209-L213` | carry forward (behaviour); do not carry forward the surface-error certificate | Pass/jam is clearance against the ball radius; proof-grade bounds are not acceptance. |
| 3 | Catalogue length stored as half-precision bits (`LengthBits = 17203`, i.e. 3.6). | `parts/catalog/pipe.tres@a6c914e:L8-L10` | do not carry forward the bits | f32 contract: carry the decimal meaning only. |
| 4 | Mouth record: id, position, outward normal, bore radius. | `engine/TubeMouth.cs@a6c914e:L9-L9` | carry forward | Seam snapping identity. |
| 5 | "Gravity carries balls through the open bore; tilt it downhill to guide them." | `parts/catalog/pipe.tres@a6c914e:L17-L17` | carry forward | No transport boost: gravity and contact only. |
| 6 | A 0.34 m-radius ball at 40 m/s passes the clear bore; a 0.8 m-radius ball at 4 m/s is stopped at the mouth. | `CuriousContraptions.tests/PipeTests.cs@a6c914e:L55-L80` | carry forward (acceptance pattern) | The legacy oversize control used a non-catalogue 0.8 m ball; with a 1.3 m bore no catalogue ball is oversized, which is why this spec proposes the narrower gauge. |

**Files harvested:** `engine/gpu/WorkshopPipe.cs`, `engine/gpu/AnnularProfile.cs` (both build-excluded at a6c914e), `parts/catalog/pipe.tres`, `parts/PipePart.cs` (checked; harvested by CAT-048), `engine/TubeMouth.cs`, `CuriousContraptions.tests/HollowGeometryTests.cs`, `CuriousContraptions.tests/PipeTests.cs`, `CuriousContraptions.csproj`. Searched for metal pipe: no hit.

## 5. Acceptance outline

Requirement row: [element-071](../requirements.md#element-071). Fact 6's closing remark ("proposes the narrower gauge") is superseded by D12.

- **Chrome UI recipe (pending, post-catalogue).** Place a Ramp feeding the mouth of a Metal pipe tilted 10° downhill and a Receiver at the far end; release a Bowling ball, then a Tennis ball whose radius is set to 0.75 m. Snap a Clear pipe to the outlet. Verify placement, length and the snapped joint through real UI controls.

All tests below are planned, in `CuriousContraptions.tests/MetalPipeTests.cs` unless named otherwise. "S4 row n" is the planned conformance theory ([contract](../../../_bmad-output/implementation-artifacts/spec-s4-catalogue-conformance-suite.md)); until it is delivered the package test named in the row is written.

| # | Given / when / then | Planned test | Control | Boundary | Tolerance and stall guard |
| --- | --- | --- | --- | --- | --- |
| A1 | Given a metal pipe tilted 10° downhill and a ball released 0.4 m inside the upper mouth, when Run advances, then the ball leaves the lower mouth | `MetalPipeTests.EveryDeliveredBallRollsThroughTheBoreAndLeavesTheFarMouth` (Basketball, Bowling ball, Tennis ball) | A level pipe: the ball never leaves | `length` 1, 3.6 and 8 m | Exit observed within 12 s of game time (an 8 m pipe takes about 4.2 s); while inside, the ball centre stays within bore radius − the ball's sphere radius (0.34, 0.28 or 0.25 m; not the Basketball's 0.35 m band) + 0.005 m of the axis, so a ball that leaks through the wall fails |
| A2 | Given an upright metal pipe and a Tennis ball with `radius` 0.75 m dropped 0.5 m above its mouth, when Run advances for 3 s, then the ball never crosses the mouth plane and comes to rest on the rim | `MetalPipeTests.ABallWiderThanTheBoreJamsAtTheMouthAndANarrowerOnePasses` | `radius` 0.60 m passes and leaves the far mouth | 0.66 m jams; 0.61 m passes, clearing the 0.65 m bore by 0.04 m | The jammed ball's centre stays at least √(r² − 0.65²) − 0.01 m outside the mouth plane (0.364 m for r 0.75, 0.105 m for r 0.66; a seated sphere's lowest point is 0.38 to 0.55 m inside the bore, so the mouth plane is no bound on it), and its speed over the last 0.5 s is under 0.05 m/s: a jam that creeps would eventually traverse |
| A3 | Given the package, then it declares no port, and a construction that connects any port of it is refused | `MetalPipeTests.ThePipeDeclaresNoPortsAndRefusesAForeignConnection` (pattern `WorkshopWallTests.cs:141`) | A valid construction with the pipe unconnected is accepted | — | Exact: typed refusal `InvalidConnection` |
| A4 | Given a placed Clear pipe, bend, Funnel outlet, Powered gate or Ball detector, when a metal pipe is offered 0.1 m off and 5° turned from its free mouth, then it snaps; and a ball crosses a metal-to-clear joint without losing forward progress | `MetalPipeTests.ItsMouthsSnapToEveryDeliveredTubeMouthAndABallCrossesTheJoint`; also one row added to `TubeMouthSnapTests.cs:176` | The Funnel's wide inlet (bore 1.3 m radius) does not snap | Offset at the capture distance, as `TubeMouthSnapTests.cs:141` | Snap is exact pose equality of the joined mouths (authored state); the ball leaves the far mouth within the 12 s guard and never rests in the joint (speed under 0.05 m/s for 0.5 s with its centre within 0.2 m of the joint plane fails); no every-tick speed is asserted |
| A5 | Given a Basketball dropped 0.5 m onto the top of a level metal pipe, then it rebounds at 0.165 of its approach speed | `MetalPipeTests.ABallDroppedOnTheShellReboundsAtTheSteelRestitution` | The Clear pipe's 0.0825 (delivered, `VisibleSolidColliderTests.cs:725`) | — | Ratio within ±0.03; the two materials are 0.0825 apart, so a pipe that silently used clear-pipe values fails |
| A6 | Given the package scene, then the shell material is opaque, every art node is classified once and each hollow node has its collider at `length` 1, 3.6 and 8 m | `MetalPipeArtTests.TheShellIsOpaqueAndEveryArtNodeHasItsClass`; one entry in the table at `VisibleSolidColliderTests.cs:153` | — | The parameter bounds, taken by that theory at `:227` | Exact alpha 1; art-to-collider agreement within the suite's existing bound |
| A7 | Run, Reset and save/load restore the pipe, its length and the ball | S4 rows 1 and 3, else `MetalPipeTests.ResetAndSaveLoadRestoreThePipeItsLengthAndTheBall` (pattern `HollowElementTests.cs:86`) | — | Length at both limits | Authored state exact |
| A8 | A length below 1, above 8 or non-finite is refused atomically and the prior construction still runs | S4 row 2, else `MetalPipeTests.AnInvalidLengthIsRefusedWithoutChangingTheConstruction` (pattern `HollowElementTests.cs:139`) | 1 and 8 m are admitted | The next f32 below 1 and above 8 | Exact: typed refusal |
| A9 | Given a Bowling ball launched at 20 m/s at the side of the shell, then it rebounds and never enters the bore | `MetalPipeTests.AFastBallStrikingTheShellNeverEntersTheBore` | The same launch along the axis into the mouth passes through | 8 m/s | The ball centre never comes nearer the axis than 0.70 m + 0.28 m − 0.01 m |
| A10 | **Air occlusion (retained CAT-048 control).** Given a Fan blowing across a metal pipe at a Tennis ball behind it, then the ball is not pushed | `MetalPipeTests.TheShellBlocksAFansStream` (pattern `CuriousContraptions.tests/FanGuidanceTests.cs:45`) | Pipe removed: the ball is pushed | — | Ball displacement under 0.02 m in 3 s against more than 0.3 m in the control |
| A11 | **Light occlusion (later partner, after `ENGINE-OPTICAL`).** A collimated beam aimed across the metal shell at a receiver behind it: the shell's colliders take the default Absorb response, so the receiver's committed reading is zero (optical conventions 6 and 8, [EL-176 section 10](EL-176-red-laser-emitter.md#10-optical-conventions-owned-here-every-optical-spec-cites-this-section)) | `MetalPipeTests.TheOpaqueShellBlocksABeamTheClearShellPasses` | The Clear pipe, whose shell declares Transmit (1, 1, 1): the receiver reads the beam's power | — | The optical tolerances (coordinator decision 21): reading within ±2 % of the emitted power in the control and zero here, after a 1 s guard; the identity stays open for this row until it runs |

The retained CAT-048 acceptance is rows A1, A4, A7 and A8 run on this package, with its occlusion controls in A10 and A11. No arrival tick, trajectory or hash is asserted.

## 6. Open questions

**Resolved**

1. Bore conflict with CAT-048: **D12.** The 1.3 m bore is kept and the metal pipe joins clear mouths. Code confirms the oversized control needs no new body (`TennisBallElementDeclaration.cs:18`; `WorkshopPhysicsLawHollowTests.cs:618`).
2. Separate catalogue part or a material variant: **D8 and D12.** Its own package and catalogue entry; a variant in declared values and art.
3. Viewing slots: **decided now, none.** Visible = solid forbids a drawn opening over a solid shell, and a real slot would let small cargo escape; hiding the cargo is the part's stated difference (D12).
4. Default length: **decided now, 3.6 m**, so the Clear pipe's scene and bindings are reused unchanged.
5. Whether the retained CAT-048 `length` parameter survives D13: **yes.** The ledger entry retains CAT-048 acceptance, which names the size parameter, so D13's exception applies.

**Needs the owner**

None.

## 7. Integration

- **Collaborators.** All delivered: the three balls, Clear pipe, both clear bends, Funnel, Powered gate, Ball detector, Receiver, Ramp. EL-072 and EL-242 (same batch, the Family slice "Metal bends", not delivered) join by the same mouths; neither depends on the other, and the snap rows of this pipe against both bends run in this slice.
- **Campaign.** Ball straight pipe, first use levels 21–30, reuse 41–50, 111–120 and 136–150 (`docs/planning/invest/campaign-reservations.md:73`). That reservation is met by the clear pipe, so owner decision 24c places the metal pipes in the light-interruption lesson at **52** (chapter 6), where their opacity is the lesson: introduction 52, practice 54, reuse 142 ([campaign coverage additions](../requirements.md#campaign-coverage-additions); brief in [campaign coverage](readiness/campaign-coverage.md#4-lesson-briefs)). This slice authors no level. A level that places it as a fixture before it is playable needs the lesson-fixture story (S8).

### Coverage binding (planned, not yet live)

Published main binds a named-identity package through a final section of the identity's spec headed "Package source binding", read by `tools/Coverage/NamedPackageSources.cs` (on `origin/main`, `25df3cd5`; pattern: EL-239 Baseball). The section is not written here: the reader parses every EL spec that carries that heading and refuses one whose preconditions fail. **Blocked on the coverage follow-on story S7.**

| Field | Planned value |
| --- | --- |
| Package | `metal_pipe` |
| Named identity | EL-071, `../invest/named-elements.md#element-071` |
| Acceptance | `todo-199` and `sequence-task-624` |
| Decision | S360; `scope-corrections.md` has no `s360` block |
| Mode records | `Configuration=Adjustable` (it declares a parameter; confirmed at slice planning) |

What it lacks today: an `s360` anchor block in `docs/planning/invest/scope-corrections.md`, and its todo-199 anchor is a scope index, not immediately followed by its sequence-task-624 anchor in `requirements.md`. S7 widens the reader (owner decision 15), including for companion packages and TH, RAD and GAP identities.

## 8. Delivery

- **Slice shape.** One element slice. Run it immediately before or after EL-072 so one planner reuses this probe (delivery-batches 1b, run "Metal ball pipes").
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), which includes ENGINE-SPATIAL-QUERIES, ENGINE-CAPABILITY-INSTANCES and ENGINE-ISLANDS-SLEEP; then S7 (the coverage follow-on, which widens the reader, owner decision 15). Row A11 alone also waits for ENGINE-OPTICAL. No other element.
- **Files added.** `elements/MetalPipe/MetalPipe.csproj`, `MetalPipeElementDeclaration.cs`, `metal_pipe.tscn`, `icon.svg`; `CuriousContraptions.tests/MetalPipeTests.cs`, `MetalPipeArtTests.cs`.
- **Files edited.** One row in `TubeMouthSnapTests.cs:176` and one entry in `VisibleSolidColliderTests.cs:153`; until S3, `CuriousContraptions.slnx:35`, the trim list at `CSharpStaticGateTests.cs:687`–`:703` and the parameter and placement switches at `PackageRuntimeParityTests.cs:483` and `:641` (generic list `:653`); the binding section of section 7; this spec and `TODO.md`.
- **Review tier.** Data-only once S3 has published: every capability is present with a test and the owned diff is declarations, art, tests and list lines. Before S3 it is engine tier for the placement switch alone, the reason recorded in `spec-family-ball-presets.md:124`.
- **Disqualifiers (the slice's probe re-checks).** The clear family's bore is still 0.65 m at every mouth; scene extraction does not depend on the shell material being transparent; the printed bands pass the art-solidity declaration that `spec-engine-art-solidity.md` introduces; the coverage follow-on story S7 has published.
