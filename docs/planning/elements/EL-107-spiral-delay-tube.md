# EL-107 · Spiral gravity-delay tube named-identity spec

**Current delivery (24 October 2026):** [EL-107 delivery slice](../invest/named-elements.md#element-107). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Story 7.0 named-identity spec (Batch J). Baseline commit `a6c914e`; every `path@a6c914e:Lstart-Lend` citation resolves with `git show a6c914e:<path> | sed -n 'start,endp'`. All values are canonical IEEE-754 f32 game values ([numeric authority](../../gpu-f32-physics.md#numeric-representation-and-precision)) inside the [game-grade envelope](../../gpu-f32-physics.md#game-grade-envelope).

**Build-ready revision, 21 November 2026 (checkout `8d1d68aa`).** Sections 1–3, 5 and 6 were rewritten against current code; a `path:line` citation without a commit is in this checkout. Section 4 is the unchanged harvest. Values marked **starting value** are adopted under decision D1 of [delivery batches](../invest/delivery-batches.md#3-family-level-decisions).

## 1. Identity

| Field | Value |
| --- | --- |
| ID | EL-107 |
| Name | Spiral gravity-delay tube |
| Type | Mechanical |
| Anchor | [requirements.md#element-107](../requirements.md#element-107); [named entry](../invest/named-elements.md#element-107); scope index [todo-248](../requirements.md#todo-248) |
| Proof owner | S380 |
| Disposition | To deliver as a single element. Not a duplicate: no delivered part delays a ball by path length. Not a family member: its siblings under todo-248 (EL-108, EL-109) share no declaration. |
| Planned package | id `spiral_tube`, directory `elements/SpiralTube/`, class `SpiralTubeElementDeclaration` |
| Refines / extends | No CAT spec. Extends the clear-tube kit: [CAT-048 pipe](CAT-048-pipe.md), [CAT-049 45° bend](CAT-049-pipe_bend_45.md), [CAT-050 90° bend](CAT-050-pipe_bend_90.md) |
| Related | EL-108 Powered airlift, EL-109 Speed-sensitive trapdoor (same scope index) |
| Status | Not started; blocked on the engine programme (owner decision 3a), relying most on ENGINE-SPATIAL-QUERIES and ENGINE-CAPABILITY-INSTANCES (`TubeEnd` snapping); then S7. |

## 2. Declaration

Follow the delivered 90° bend (`elements/PipeBend90/PipeBend90ElementDeclaration.cs:11`): the package implements `IMechanicalElement` (`CuriousContraptions.Abstractions.Mechanical/MechanicalContracts.cs:7`) and `ISceneGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`), with `KinematicMode.Static`, `PayloadPolicy.None`, a zero constant mass frame, no ports, no events, no stores and `SupportedDomains = Mechanical`. Footprint: 1 body, 0 moving, 0 constraints.

### Bodies and colliders

One static body. The spiral is **four half-turn torus arcs**, not a helix: the engine has a planar torus segment (`ColliderShapeKind.Torus`, `CuriousContraptions.Abstractions/PhysicsDeclarationValues.cs:12`) whose sweep may be up to π (`:191`), and no helical shape. Each arc lies in a plane tilted ±14.48° about the in-plane axis through the arc's centre and midpoint (perpendicular to the diameter joining its ends), alternately one way and the other; arc n's centre is 0.8 m below arc n−1's. One end of each arc rises 0.4 m and the other falls 0.4 m, the tangent at both ends is parallel to the tilt axis and so stays horizontal, and the tube descends by the same amount on each half turn. (A tilt about the diameter joining the ends, as first drafted, would leave both ends level.)

| Collider (scene node) | Kind | Dimensions (m) |
| --- | --- | --- |
| `col_arc_1` … `col_arc_4` | Torus, `metadata/sweep_degrees = 180`, `metadata/wall_thickness = 0.05` | centreline radius 1.6; bore radius 0.65; outer radius 0.70; plane tilt ±14.48° (asin 0.8/3.2); each arc drops 0.8 |
| `col_stub_start`, `col_stub_end` | Tube | inner 0.65, outer 0.70, length 0.09, from each end arc to its mouth |
| `col_collar_start`, `col_collar_end` | Torus ring, the bend's `Mesh_Collar` (`elements/PipeBend90/pipe_bend_90.tscn:36`–`:39`) | inner radius 0.65, outer radius 0.78 |

Scene encoding is the bend's: hidden `col_*` meshes with sweep and wall metadata (`elements/PipeBend90/pipe_bend_90.tscn:136`–`:142`, read by `tools/BundleElements/SceneGeometryExtractor.cs:229`), and two mouth locators `Loc_Port_mouth_start` / `Loc_Port_mouth_end` with `metadata/bore_radius = 0.65` (`pipe_bend_90.tscn:128`).

Resulting part: two full turns, 3.2 m total drop, 1.6 m between coils (wall-to-wall gap 0.20 m; the 0.78 m collars sit only at the mouths and clear the neighbouring coil by 0.12 m), footprint 4.6 × 4.6 m, centreline path 20.1 m. `Inlet` is at the top and `Outlet` 3.2 m directly below it; both are horizontal and face opposite ways, so each joins a level pipe.

- Centreline radius 1.6 m, **starting value**: the 2.4 m of the delivered bends would give a 6.2 m footprint on a 16.8 × 9.8 m bench; the torus only requires the centreline radius to exceed the tube's outer radius.
- Two turns, fixed (decision in section 6).

### Material

Wall restitution 0.15, friction 0.3, rolling resistance 0, bounce threshold 0.1 m/s: the delivered bend material (`PipeBend90ElementDeclaration.cs:35`). Loss comes from the ball's own rolling resistance (Basketball 0.035, `elements/Ball/BallElementDeclaration.cs:38`; Bowling ball 0.03, `elements/BowlingBall/BowlingBallElementDeclaration.cs:40`).

### Parameters, ports, stores, bindings

None. Metadata: category `Motion`, display name "Spiral delay tube", palette order 118 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)), colour (0.4, 0.72, 0.79). No presentation binding.

### Art and solidity

| Node | Mesh | Class |
| --- | --- | --- |
| `Visual/Visual_Shell_1..4` | four half-torus shells, clear pipe `#66b8c9`, alpha 0.16 | hollow, with `col_arc_n` |
| `Visual/Visual_Collar_Start/End` | cream `#fff8e9` torus collars | solid, with `col_collar_*` |
| `Visual/Visual_Stub_Start/End` | clear tube stubs | hollow, with `col_stub_*` |
| `Visual/Visual_Rings` | three navy `#293954` index bands printed on the shell, one per half-turn joint | decorative: printed flat, flush with the shell |

No rails or brackets are drawn, so no proud solid lacks a collider. Icon: a two-coil spring outline seen from the side with a ball at the top mouth. At a glance it differs from the bends by its stacked coils and index bands, not by colour.

### Behaviour and numbers

Gravity, rolling contact and rolling resistance only. For a rolling solid sphere the mean acceleration along the path is g·(Δh/L − Crr)/1.4. With Δh/L = 3.2/20.1 = 0.159 and Crr 0.035 the Basketball averages about 0.87 m/s², so from a slow entry it takes roughly 6–8 s and leaves at about 5–6 m/s; the same drop in free fall takes 0.81 s. A faster entry shortens the transit, which is the required outcome. The slope is zero at each joint and below the Basketball's rolling resistance for about 0.2 m either side, so a ball entering slower than about 0.35 m/s stops inside the inlet: it stays visible in the tube and nothing moves it. Exit speed is below the Bumper's 8 m/s target and the bore radius is the kit's 0.65 m, so every delivered ball (radius 0.25–0.34 m) passes and a ball whose `radius` is set above 0.65 m cannot enter (decision D12).

## 3. Engine capabilities

Map row families: ContactImpulse, EnvironmentState, FiniteLedger, GeometryQuery, JointConstraint, RigidBodyDynamics, SlidingFriction, StateTransaction ([map row](../general-engine-element-map.md); navigation only under D14).

| Capability | Status | Evidence |
| --- | --- | --- |
| Static hollow torus segment with bore and wall | present | `PhysicsDeclarationValues.cs:12`; `PipeBend90Tests.WorkshopPhysicsCompiler_CompilesPipeBend90IntoStaticBodyWithItsHollowColliders` (`CuriousContraptions.tests/PipeBend90Tests.cs:90`) |
| A ball rolling through a torus bore with rolling loss | present | `HollowElementTests.ABallDroppedIntoABendRidesItsOuterWallAndLeavesAtTheDerivedSpeed` (`CuriousContraptions.tests/HollowElementTests.cs:356`) |
| Several hollow colliders in tilted local frames on one static body, a ball riding the bore floor with gravity near the torus axis | present, untested | bend scene (`pipe_bend_90.tscn:136`); `HollowBoundsTests.NearAxisFramesKeepTheirTransverseSurfaceExtent` (`CuriousContraptions.tests/HollowBoundsTests.cs:11`) is a bounds test, and the only traversal test (`HollowElementTests.cs:356`) keeps gravity in the torus plane |
| Tube mouths that snap to the pipe kit, step-free joint | present | `TubeMouthSnapTests.APipeSnapsOntoTheEndMouthOfEveryTubePart` (`CuriousContraptions.tests/TubeMouthSnapTests.cs:184`); `HollowElementTests.ABallCrossesASnappedStraightOrFunnelToBendJointWithoutAStep` (`HollowElementTests.cs:24`) |
| Hollow passage through Reset and save/load | present | `HollowElementTests.AConnectedHollowPassageRestoresConstructionAndRepeatsAfterResetAndSaveLoad` (`HollowElementTests.cs:86`) |
| A **half-turn** (π) sweep carrying a rolling ball | present, untested | admitted (`PhysicsDeclarationValues.cs:191`) and its bounds are tested (`HollowBoundsTests.PartialTorusBoundsContainEndpointsAndInteriorExtrema`, `HollowBoundsTests.cs:60`, sweep π), but ball traversal is tested only at 45° and 90° |

Counts: 4 present, 2 present-untested, 0 narrower, 0 missing. Nothing needs element-specific engine code. The stale harvest rows ("Sphere, Box and Plane only", "hollow helical collider missing") are superseded by this table.

## 4. Sources and legacy

- Requirement: "Long physical descent delays arrival through travel distance and contact"; outcome "Changing speed changes transit time; no precise hidden timer" ([element-107](../requirements.md#element-107)).
- Scope record [todo-248](../requirements.md#todo-248) is navigation only; campaign coverage reserves spiral delay for first use 21–30, specialist routes 91–100, reuse 41–50, 111–120 and 136–150 ([campaign-element-coverage](../requirements.md#campaign-element-coverage)).
- Decisions: none bounded for this element in [decisions.md](../invest/decisions.md); the map's decision column is "Source D owner".

| # | Fact | Citation | Disposition | Reason |
| --- | --- | --- | --- | --- |
| 1 | Standard bore 0.65 m, wall 0.70 m, collars 0.78 m × 0.09 m | `engine/gpu/WorkshopPipe.cs@a6c914e:L8-L19` | carry forward | Mouths must match every other tube. |
| 2 | Bend centreline radius 2.4 m; bend material (0.15, 0.1, 0.3) | `parts/PipeBendPart.cs@a6c914e:L9-L12` | carry forward | Curvature and material of the existing curved tube. |
| 3 | Bend collider is a hollow torus segment built per-part (`Bends.Add`) | `parts/PipeBendPart.cs@a6c914e:L30-L30` | do not carry forward | Per-part collider assembly; the helix must be a generic declared shape. |

No legacy spiral, helix or delay-tube implementation exists.

**Files harvested:**
- `parts/PipeBendPart.cs`
- `parts/PipePart.cs` (tube art)
- `parts/PoweredGatePart.cs` (tube mouth contract)
- `parts/catalog/conveyor.tres` (catalogue field format only)
- Searched with no hit for `spiral`, `helix` or `delay tube`: `parts/`, `engine/` (including `engine/physics/` and `engine/bridge/`), `CuriousContraptions.tests/`, `content/puzzles.json`, `tools/`, `reference/`, `diagnostics/`.

## 5. Acceptance outline

Requirement row: [element-107](../requirements.md#element-107). Planned test class `SpiralTubeTests`, run through the Workshop integration harness with the real simulation. Stall guards are game seconds.

| # | Given / When / Then | Planned test | Control | Boundary and tolerance |
| --- | --- | --- | --- | --- |
| A1 | Given a Basketball fed into `Inlet` at 1 m/s, when Run, then it leaves `Outlet` and its transit is at least three times the free-fall time for 3.2 m | `ABallLeavesTheOutletMuchLaterThanTheSameDropInFreeFall` | the same ball released beside the tube reaches outlet height in about 0.81 s | factor 3 against an expected factor near 8: a delay a player cannot miss; guard 30 s |
| A2 | Given entry at 1 m/s and at 4 m/s, when Run, then the faster ball's transit is shorter by at least 0.1 s | `AFasterEntryArrivesSooner` | the same ball released beside the tube with 1 and with 4 m/s of horizontal speed reaches outlet height at the same time (0.81 s, within 0.02 s): only the path through the tube turns entry speed into transit time | 0.1 s is 12 ticks at 120 Hz, the least a player can see |
| A3 | Given entry at 0.1 m/s, when Run for 20 s, then the ball is still inside the first arc and never left the bore | `ABallEnteringTooSlowlyStopsInsideTheTube` | A1 | centre stays within bore radius − ball radius + 0.02 m of the centreline |
| A4 | Given a Basketball and a Bowling ball, when each traverses, then neither leaves the bore and both exit | `EveryDeliveredBallStaysInTheBoreAndExits` | a ball with `radius` 0.7 m does not enter | same 0.02 m bore allowance |
| A5 | Given pipes snapped to both mouths, when a ball crosses each joint, then its height changes by no more than 0.02 m across the joint | `BothMouthsJoinThePipeKitWithoutAStep` | an unsnapped pipe 0.3 m away leaves a gap the ball falls through | the detector collar (CAT-002) on the outlet pipe fires once per ball |
| A6 | Run, Reset, save and load restore the construction and repeat A1's outcome | `ResetAndSaveLoadRepeatTheDescent` | — | generic rows: `CatalogueIntegrationTests.EveryAdmittedDeclarationRetainsAuthoredStateAcrossRunResetSaveLoad` and S4 rows 1 and 3 |

No ports and no parameters, so the wrong-kind connection and parameter refusals do not apply; S4 row 4 and `VisibleSolidColliderTests` cover visible-solid agreement. No arrival tick is asserted: the element is not a timer.

### Integration

- Works with CAT-048, CAT-049, CAT-050 and CAT-030 (delivered), CAT-002 Ball detector (delivered) for an observable arrival, and every delivered ball.
- Campaign: chapter 3 "Pipe Dreams" (levels 21–30) introduces the pipe kit (`docs/planning/requirements.md:2288`); the coverage row places spiral delay at specialist routes 91–100 with reuse in 41–50, 111–120 and 136–150 (`:2398`). Owner decision 24e moves the first use to level **28b** in chapter 3 ([per-level allocation](readiness/campaign-coverage.md#6-per-level-allocation-and-progression-check-owner-decisions-24e-and-24f)).
- Coverage binding: a final package-source-binding section in this spec (Package, Named identity, Acceptance, Decision, Mode records), as EL-239 on published main; blocked on S7 (section 7). A level that places it as a locked fixture needs the lesson-fixture story.

### Delivery

- **Shape.** Single element slice. It is not classified in delivery batches section 1; it belongs in 1b beside the metal ball pipes.
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), which includes ENGINE-SPATIAL-QUERIES and ENGINE-CAPABILITY-INSTANCES (`TubeEnd` snapping); then S7 (the coverage follow-on, which widens the reader, owner decision 15). S2–S4 reduce the slice but do not block it.
- **Files.** `elements/SpiralTube/` (`SpiralTube.csproj`, `SpiralTubeElementDeclaration.cs`, `spiral_tube.tscn`, `icon.svg`); `CuriousContraptions.tests/SpiralTubeTests.cs` (compiled by the test project's one `*.cs` rule); one line in `CuriousContraptions.slnx`; the id added to the tube-part rows of `TubeMouthSnapTests`; the binding section of section 7 once S7 admits it. Until S3 lands: the trim-analysis list and the placement switch. Registry, bundle, palette, capacity, coverage membership and impact tables are derived (`spec-registration-derivation.md`).
- **Review tier.** Engine tier, because of the two *present, untested* rows. If the planning probe finds or adds a traversal test of a tilted arc at sweep π first, the slice is data-only.
- **Disqualifiers the probe re-checks.** A half-turn arc that a rolling ball cannot traverse (a seam at 180°); a ball stalling at a joint when fed at 1 m/s (then raise the tilt, not the engine); the placement bounds refusing a 4.6 m part on lesson benches.

## 6. Open questions

All resolved.

1. **Variants.** The campaign row's "spiral delay … variants" are not separate identities; the ledger names one. Resolved by D8.
2. **Turns and handedness parameters dropped.** Colliders are baked from the scene and no capability changes a collider count or mirrors a scene from a parameter; D13 fixes size per kind unless the row names a size parameter. Two turns, one handedness. Decided under the standing delegation.
3. **Radius and pitch.** 1.6 m and 1.6 m per turn, starting values under D1, with the reason in section 2.
4. **Index rings.** Yes, printed flat (D9): they let a player count half turns.

Needs the owner: none.

## Engine conformance (22 November 2026)

Checked against the owner addendum of 22 November 2026 (AGENTS.md element-free engine gate and Tri-Graph rule; `_bmad/custom/element-free-engine-design.md`; `docs/gpu-f32-physics.md`).

1. **Declaration data only.** A static body of scene-declared hollow colliders and its art. No solver branch, callback, script or engine test on this package.
2. **Generic laws.** Contact, friction, rolling resistance and gravity. The delay is a consequence of a 20.1 m bore path; nothing times the ball.
3. **Three layers.** Colliders: eight hidden `col_` proxies on one static body (four Torus arcs, two Tube stubs, two Torus collar rings), the pipe kit's material; physics reads none of the art. Visuals: the clear tube, collars and flat-printed index rings; none is a collider and each proud solid has its proxy. Animation: none; no art node moves.
4. **Flow and typing.** No ports, parameters, events or stores; nothing is read back from presentation. Values are f32.
5. **Flat state.** Bodies 1 (static); colliders 8 (Torus 6, Tube 2); joints 0; drives 0; sensors 0; domain nodes 0; stores 0; presentation bindings 0.
6. **Performance.** Eight colliders against the 90° bend's five. Reason: the admitted torus segment is planar with a sweep of at most π, so two turns need four arcs; a general helical or longer-sweep hollow shape does not exist and is not requested. No collider is cosmetic; the body is static. A fast ball is kept in the bore by the engine's general continuous material query, not by a check in this part; the persistent, velocity-fattened broadphase that the static tree joins is ENGINE-SPATIAL-QUERIES (today's tree is rebuilt every substep and fattened by the maximum speed). No island sleeping exists today; a resting ball stalled in the bore costs nothing only once ENGINE-ISLANDS-SLEEP (owner decision 3a) is delivered, ahead of this part.
7. **Tests are scenarios.** Rows A1 to A6 run the production simulation on this declaration and need no code path of its own.

**Verdict: conforms.**

## 7. Coverage binding (planned, not yet live)

Published main binds a named-identity package through a final section of the identity's spec headed "Package source binding", read by `tools/Coverage/NamedPackageSources.cs` (pattern: EL-239 Baseball on published main). The section is not written here: the reader parses every spec that carries that heading and fails the whole coverage run when a field does not resolve. **Blocked on the coverage follow-on story S7.** S7 widens the reader (owner decision 15), including for companion packages and TH, RAD and GAP identities.

| Field | Planned value |
| --- | --- |
| Package | `spiral_tube` |
| Named identity | EL-107, `../invest/named-elements.md#element-107` |
| Acceptance | `todo-248`; no sequence task yet |
| Decision | S380, `../invest/scope-corrections.md#s380` |
| Mode records | `Configuration=Fixed` |

What it lacks today: `scope-corrections.md` has no `s380` block on published main; `todo-248` is a scope index shared by EL-107, EL-108 and EL-109 with no sequence-task anchor directly after it, which the reader requires; and one task source can bind only one package.
