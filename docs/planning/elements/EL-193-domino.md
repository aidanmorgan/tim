# EL-193 · Domino named-identity spec

**Current delivery (24 October 2026):** [EL-193 delivery slice](../invest/named-elements.md#element-193). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Story 7.0 named-identity spec (Batch J). Harvest citations are `path@a6c914e:Lstart-Lend`. **Build-ready revision, 21 November 2026:** sections 1–3, 5 and 6 were verified against checkout `8d1d68aa`; citations of the form `path:line` are in that checkout, and test files are under `CuriousContraptions.tests/`. All values are canonical IEEE-754 f32 game values inside the [game-grade envelope](../../gpu-f32-physics.md#game-grade-envelope).

## 1. Identity

| Field | Value |
| --- | --- |
| ID | EL-193 |
| Name | Domino |
| Type | Mechanical |
| Anchor | [requirements.md#element-193](../requirements.md#element-193); [named entry](../invest/named-elements.md#element-193); scope [campaign-element-coverage](../requirements.md#campaign-element-coverage) |
| Proof owner | S299 |
| Refines / extends | Refines [CAT-023 domino](CAT-023-domino.md) ([requirement](../requirements.md#current-cat-023)) |
| Related | CAT-035 Lamp (the `domino_effect` goal), CAT-014 Bowling ball (loading control), EL-205 Rope and EL-209 Fan (domino lanes in composite levels) |
| Delivered part | Package `domino`, directory `elements/Domino` (Stories 5.1 and 5.2; converted to a generic declared instance by Story 9A.32, commit `495f0081`) |
| Disposition | **Refinement with one undelivered residual; not a duplicate closure today.** The identity adds nothing over CAT-023 except what CAT-023 itself still owes: the offset centre of mass. That delta is delivered by the existing story **CAT-023c** (`cat-023c-offset-centre-of-mass`, [its story in `epics.md`](../../../_bmad-output/planning-artifacts/epics.md#cat-023c-domino-offset-centre-of-mass-and-presentation-origin)). EL-193 is then closed as a Duplicate closure by acceptance rows inside that slice. It gets no package, palette or catalogue entry of its own |
| Status | Delivered except the offset centre of mass, which CAT-023c delivers; EL-193 closes by rows inside CAT-023c. Closure review not yet run; blocked on the engine programme (owner decision 3a), which comes first and re-qualifies the delivered package (relying most on ENGINE-ISLANDS-SLEEP). Browser qualification pending |

**What EL-193 adds over the delivered part.** The ledger sentence is "Finite rigid body tips through real contact, mass and centre of gravity"; the exact source outcome is "Separated neighbour does not fall from a scripted propagation event". Contact tipping, mass and the separated-neighbour control are delivered and tested (section 5). "Centre of gravity" is delivered only for a centre at the box centre: the package declares `centreOfMass: Vector3.Zero` (`elements/Domino/DominoElementDeclaration.cs:49`).

## 2. Declaration

Delivered declaration, read from `elements/Domino/DominoElementDeclaration.cs`. The only planned change is the centre of mass.

- **Interfaces.** `IMechanicalElement` (`CuriousContraptions.Abstractions.Mechanical/MechanicalContracts.cs:7`), `ISignalElement` (`CuriousContraptions.Abstractions.Signal/SignalContracts.cs:8`), `ISceneGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`) (`:9`).
- **Body.** One dynamic body: the box, half extents 0.125 × 0.55 × 0.325 m (full 0.25 × 1.1 × 0.65 m), and two pip colliders on its face (`col_visual_pip1`, `col_visual_pip2`, `elements/Domino/domino.tscn:46`–`:53`), baked from the scene; body origin at the box centre. Footprint: 1 body, 1 moving (`:25`).
- **Mass.** 0.4 kg; principal moments of the homogeneous box, m(b² + c²)/3 on half extents: (0.0544, 0.0162, 0.0424) kg·m² about x, y, z (`:47`–`:53`); no linear drag; gravity (0, −9.81, 0) m/s².
- **Material.** Restitution 0.05, friction 0.6, rolling resistance 0, bounce threshold 0.1 m/s (`:36`).
- **Centre of mass (approved by owner decision 8; delivered by CAT-023c).** Local offset (0, −0.05, 0) m, a fixed declared value with no parameter. The principal moments keep the homogeneous values: the parallel-axis difference m·d² is 0.001 kg·m², 2.4 % of the 0.0424 kg·m² centroidal moment about the tipping direction, and no gameplay depends on it. Reason for the value: it raises the static tipping angle about a bottom long edge from atan(0.125/0.55) = 12.8° to atan(0.125/0.50) = 14.0°, so a standing tile forgives a slightly harder nudge, while every delivered cascade strike (which rotates the tile well past 30°) still topples it.
- **Parameters.** None (`:23`). Size and mass are fixed per kind (decision D13).
- **Ports.** `PortId.ActivationOut`, role `ActivationOut`, domain Signal, output, pulse, at (0, 0.55, 0) facing +Y (`:29`). No input.
- **Signal.** One `SignalOrientationDescriptor` on body 0 with threshold 45° (`:59`), bound to the port (`:63`); event `TriggerActivated` (`:39`).
- **Stores, constraints, presentation bindings.** None (`:11`–`:12`). Presentation scale 0.6.
- **Palette.** Category Motion, order 9, id `domino`, title "Domino" (`:15`–`:20`).

### Art and solidity

Classified in `VisibleSolidColliderTests.cs:133`: `Visual/Visual_Body` (box, solid, its own collider) and the two pips `Visual/Visual_Pip1`, `Visual/Visual_Pip2` (listed solid with the body). Tile colour `#e8d4a6`, pips `#384757` ([DESIGN colour system](../../../DESIGN.md#colour-system)). No visible-solid defect is listed for this package in `spec-engine-art-solidity.md` or `spec-elements-visible-solid-remainder.md`. The tile is told from the Wall by its two pips and upright slab proportion, not by colour.

CAT-023c must keep the art on the collider: the body origin stays at the box centre and only the mass centre moves, so no mesh moves in the scene.

## 3. Engine capabilities

Map families: ContactImpulse, EnvironmentState, FiniteLedger, GeometryQuery, JointConstraint, RigidBodyDynamics, SlidingFriction, StateTransaction ([map row](../general-engine-element-map.md)). The map row is navigation (decision D14); the probe below is the authority.

| Capability | Status | Evidence |
| --- | --- | --- |
| Dynamic box with a declared mass frame | present | `MassFrameDeclaration.Constant` (`CuriousContraptions.Abstractions/MassFrameValues.cs:41`); `WorkshopDominoTests.DominoCompilesAsOneDynamicBoxWithDeclaredMassMaterialAndGravity` (`WorkshopDominoTests.cs:16`) |
| Box–box contact, friction and toppling | present | `DominoGuardTests.TheWiredLessonCascadeTopplesByContactFiresOnceAtFortyFiveDegreesAndSolvesOnce` (`DominoGuardTests.cs:62`) |
| Orientation-threshold signal node | present | `SignalOrientationDescriptor` (`CuriousContraptions.Abstractions/SignalContractValues.cs:56`); `DeclaredOrientationTests.TheDeclaredThresholdSelectsTheSensorAndNodeOfAWiredTile` (`DeclaredOrientationTests.cs:23`) |
| A centre of mass off the body origin, in free flight | present | `MassFrameDeclaration.CentreOfMass` (`MassFrameValues.cs:38`), read by the solver (`CuriousContraptions.Simulation/WorkshopPhysicsEngine.cs:457`); `WorkshopPhysicsLawTests.ABodyWhoseCentreOfMassSitsOffItsOriginSpinsAboutThatCentreInFreeFlight` (`WorkshopPhysicsLawTests.cs:1403`) |
| A declared centre of mass carried from the package declaration to the compiled body record | present, untested | `MassFrameDeclaration.CentreOfMass` to `LocalCentreOfMass` | no delivered package declares a non-zero centre; `WorkshopDominoTests.cs:46` asserts `Assert.Equal(default, properties.LocalCentreOfMass)` and must be re-authored by CAT-023c (row A2b) |
| A centre of mass off the body origin, in resting and tipping contact | present, untested | Same symbols; no test loads an offset body through a contact, and no delivered package declares a non-zero centre |
| Motion and presentation samples of an offset body | narrower | The worker writes the body origin into the lanes the host reads as the centre-of-mass cell; they agree only while the offset is zero (`_bmad-output/implementation-artifacts/deferred-work.md:107`). Owned by CAT-023c |

Nothing here needs element-specific engine code. CAT-023c is an engine-tier slice because of the last two rows.

**Dependencies.** None outstanding for the delivered behaviour.

## 4. Sources and legacy

- Requirement: "Finite rigid body tips through real contact, mass and centre of gravity"; outcome "Separated neighbour does not fall from a scripted propagation event" ([element-193](../requirements.md#element-193)).
- CAT-023 row: offset centre of mass, 45° output, input rejected, grounded close-gap chain versus separated gap ([current-cat-023](../requirements.md#current-cat-023)).

| # | Fact | Citation | Disposition | Reason |
| --- | --- | --- | --- | --- |
| 1 | Tile artwork only; the shared solver owns standing and toppling | `parts/DominoPart.cs@a6c914e:L6-L19` | carry forward (art) | Declaration-only element. |
| 2 | Accepted: wiring into a domino, foreign inventory, exhausted tiles and changed fixtures reject | `CuriousContraptions.tests/DominoEffectTests.cs@a6c914e:L60-L79` | carry forward | Input rejection. |
| 3 | Accepted: the goal solves only when the end domino and the lamp are both latched | `CuriousContraptions.tests/DominoEffectTests.cs@a6c914e:L80-L96` | carry forward | Real propagation required. |
| 4 | Level `domino_effect`: "Fill the gap with four dominoes"; inventory domino 4 | `content/puzzles.json@a6c914e:L1129-L1528` | carry forward | First lesson. |

**Files harvested:**
- `parts/DominoPart.cs`
- `parts/catalog/domino.tres`
- `CuriousContraptions.tests/DominoEffectTests.cs`
- `content/puzzles.json` (level `domino_effect`)
- The full domino harvest, including the composite domino lanes of `tools/Campaign/Program.cs`, belongs to [CAT-023](CAT-023-domino.md) (Batch A). Searched with no further EL-193 knowledge: `engine/physics/`, `engine/bridge/`, `reference/`, `diagnostics/`.

## 5. Acceptance outline

Follow [element-193](../requirements.md#element-193) and [current-cat-023](../requirements.md#current-cat-023). The Chrome recipe (open `domino_effect`, place four dominoes at close gaps with the move gizmo, Run) stays queued for the post-catalogue phase.

### Executed today (the closure check for everything except the offset)

| Criterion | Test |
| --- | --- |
| Tips and propagates by real contact; fires once at 45°; goal solves once | `DominoGuardTests.cs:62` |
| **Exact source outcome:** a separated neighbour does not fall | `DominoGuardTests.AGapBreaksTheChainSoTheEndStandsAndTheLampStaysDark` (`:95`) |
| Standing stability and the tipping boundary | `AnUprightWiredTileStandsAndNeverFires` (`:128`); `ATileTenDegreesOffUprightRocksBackSilentlyAndThirtyDegreesTopplesAndFires` (`:140`) |
| Wrong-kind connection refused at edit and save | `AnActivationInputIntoADominoIsRefusedAtTheEditAndSaveBoundaries` (`:161`) |
| No parameter is authorable (parameter refusal) | `WorkshopDominoTests.TheDominoHasNoAuthorableParameterAndDeclaresActivationOutOnly` (`WorkshopDominoTests.cs:93`); save refuses any typed field (`:68`) |
| Run/Reset; Save/Load | `DominoGuardTests.cs:193`; `:226` |
| Lesson `domino_effect` with the Lamp | `DominoEffectTests.cs:84`; `LaunchLessonSolveTests.cs:111` |

### Planned in CAT-023c

| # | Given / When / Then | Test | Control | Boundary |
| --- | --- | --- | --- | --- |
| A1 | Given two declared qualification boxes of the tile's size, one with the centre lowered 0.15 m (tipping angle 17.4°) and one homogeneous (12.8°), when each is released at rest tilted 15° about a bottom long edge on the bench, then the lowered one returns upright and the homogeneous one falls past 45° | `WorkshopPhysicsLawTests.ALoweredCentreOfMassRaisesTheTippingAngleOfABoxInContact` | The homogeneous box is the one-cause control | Both stand at 10° and both fall at 20°. Margins of 2.2° or more keep contact compliance from deciding the result. Stall guard 5 s of game time |
| A2 | Given the delivered tile with its declared (0, −0.05, 0) offset, when the delivered cascade, gap, upright, 10° and 30° cases run, then each has its delivered outcome | The existing `DominoGuardTests` cases above, unchanged | Gap control (`:95`) | 10° and 30° (`:140`); both are outside the 12.8°–14.0° band the offset moves |
| A2b | Given the stock tile, when compiled, then its `LocalCentreOfMass` is (0, −0.05, 0) and its principal moments are the declared homogeneous values | `WorkshopDominoTests.DominoCompilesAsOneDynamicBoxWithDeclaredMassMaterialAndGravity` (`CuriousContraptions.tests/WorkshopDominoTests.cs:16`; its assertion at `:46` is re-authored, so the existing tests are not all unchanged) | the box collider's pose stays identity | this is the only row that fails if the declared offset is ignored: −0.05 m moves the tipping angle by 1.2°, which no scene test resolves at a 1° allowance |
| A3 | Given a falling offset tile, when presentation samples its motion, then the drawn tile stays on its collider | `DominoGuardTests.TheDrawnTileStaysOnItsColliderWhileItFalls`: the presented origin is within 0.005 m of the committed body origin at every sampled tick | The zero-offset fixture gives the same bound | A double or missing subtraction of the offset shows as 0.05 m, ten times the allowance |
| A3b | Given an end-of-substep motion sample observed at its declared ordinal, then it neither leads physics by a substep nor lags it (CAT-023c's third criterion, [its story in `epics.md`](../../../_bmad-output/planning-artifacts/epics.md#cat-023c-domino-offset-centre-of-mass-and-presentation-origin)) | owned by the CAT-023c slice contract, which names the test; this spec only requires that the row exists there | the zero-offset fixture | — |
| A4 | Given a non-finite centre of mass in a declaration, when the bundle is built, then it is refused with a typed reason and nothing changes | Generic mass-frame refusal case beside `RigidMassPropertiesTests` | The finite declaration is admitted | each component at ±16 m is admitted and just beyond is refused (`CuriousContraptions.Bundle/MassFrameDomain.cs:18`–`:19`); NaN is refused; zero is admitted |
| A5 | Given the changed tile, when Run, Reset and Save/Load are exercised, then authored poses return and the wired cascade reloads | `DominoGuardTests.cs:193`, `:226`, unchanged | — | — |
| A6 | Given a Bowling ball and a Basketball striking a tile at the same speed, then the delivered loading contrast still holds | The loading cases in `WorkshopPhysicsEngineTests` (`CuriousContraptions.tests/WorkshopPhysicsEngineTests.cs:710`, `:438`) | Basketball | — |

No arrival tick, trajectory or hash is asserted. Angles are compared with a 1° allowance chosen from the 2.2° margin.

### Integration

- **Collaborators.** Lamp, Bowling ball, Ball, Impact switch, Fan, Springboard, Conveyor: all delivered.
- **Campaign.** Row 1 (domino): first use 1–10, reuse 21–30, 41–50, 91–100, 136–150 ([campaign-element-coverage](../requirements.md#campaign-element-coverage)). In `content/puzzles.json` the Domino is in level 6 `domino_effect` (Playable; plan level 4, re-authored without the lamp, its goal a Captured goal: the last domino knocks a ball into a Receiving basket, coordinator decision 41). The authored planned levels 33, 35, 38, 46, 47, 48, 50, 51, 55, 56 and 57 that placed it were removed under owner decision 24d (they repeated this mechanic; [campaign coverage section 5](readiness/campaign-coverage.md#5-removed-and-converted-levels-owner-decision-24d)); the kept level 58 (in its converted form) has several goals and waits for `ENGINE-GOALS`.
- **Coverage binding.** Catalogue key `domino` (CAT-023). EL-193 has no package of its own and so no package source binding.
- **Lesson fixtures.** Level 6 already authors its locked end tile; the planned levels are authored when proven and need the lesson-fixture story (S8) only if they lock tiles in a way level 6 does not.

### Delivery

- **Slice.** CAT-023c, a single engine-tier slice (all six review layers). EL-193's closure rows are A2 and the "Executed today" table; the ledger row then cites `domino`, CAT-023c and those tests.
- **Prerequisites.** Blocked on the engine programme (owner decision 3a), which comes first; the closure review then re-runs its rows on the re-qualified package. Then CAT-023c, inside which EL-193 closes. It must be serialised with any slice that edits `elements/Domino` or the motion-read path.
- **Files.** `elements/Domino/DominoElementDeclaration.cs` (line 49); the worker's motion-piece writer and `PhysicsMotionRead.TrySample` callers; tests named above, including `CuriousContraptions.tests/WorkshopDominoTests.cs` (the compiled-centre assertion at `:46`); `docs/planning/elements/CAT-023-domino.md` (its line 88 still says "unspecified — owner decision"); the CAT-023 requirement delivery note; this spec and the ledger row. No registration, palette or bundle list changes: the package exists.
- **What would disqualify the plan (the slice's probe re-checks).** (1) Contact rows that do not take lever arms from the committed centre of mass: then A1 fails for an engine reason and the slice's engine scope grows. (2) `domino_effect` or any `LaunchLessonSolveTests` route no longer solving with the offset: the offset is reduced, never the lesson's stall guard stretched. (3) A second consumer of the body-origin-as-centre assumption beyond the motion pieces.

## 6. Open questions

None open.

1. Offset value and direction: **approved** by owner decision 8 (22 November 2026): (0, −0.05, 0) m. The moments stay homogeneous (decided here under the standing delegation: the difference is 2.4 % and unobservable in play).
2. Resizable dominoes: **resolved** by D13: size is fixed per kind; the row names no size parameter.
3. How the offset is proven: **decided by the coordinator under the owner's standing delegation (22 November 2026)**: by a compiled-value test (row A2b). Reason: the −0.05 m offset moves the tipping angle by only 1.2°, below what a scene test with a 1° allowance can show, so the value is asserted where it is exact, and the −0.15 m qualification pair of A1 proves the law in contact.

## Engine conformance (22 November 2026)

Checked against the owner addendum of 22 November 2026, with the package opened (`elements/Domino/`) and the engine projects searched for the package id and name.

1. **Declaration data only.** One dynamic body, a declared mass frame, a declared orientation-threshold signal node and one output port. The engine search found no code that names the package; the word appears once, in a comment (`CuriousContraptions.Abstractions/OrientationSensorDeclarationValues.cs:11`, "Domino: 45°"), which should be reworded element-free when that file is next touched.
2. **Generic laws.** Rigid-body contact, friction and toppling about an edge; the orientation-threshold sensor rule that fires its pulse; the declared centre of mass in the mass frame. A separated neighbour stays up because nothing touches it: there is no propagation event.
3. **Three layers.** Colliders: the box and two pip proxies (`col_visual_pip1`, `col_visual_pip2`) on the one body. Visuals: the tile and its pips; none is a collider. Animation: none. The art follows the body's pose, and CAT-023c makes the presented origin and the centre of mass agree for an offset body (row A3).
4. **Flow and typing.** One Signal-domain `ActivationOut` pulse port; no parameters; f32 values.
5. **Flat state.** Bodies 1 (dynamic); colliders 3 (box 1, pips 2); joints 0; drives 0; sensors 1 (orientation threshold); signal nodes 1; stores 0; presentation bindings 0 beyond the body pose.
6. **Performance.** Three primitive colliders on one dynamic body. No island sleeping exists today; a resting tile costs nothing only once ENGINE-ISLANDS-SLEEP (owner decision 3a) is delivered, ahead of this part. A long cascade of mostly resting tiles is the case that story is for.
7. **Tests are scenarios.** The delivered and planned rows run the production simulation; A2b reads the compiled declaration through the defined compile interface.

**Verdict: conforms (one engine comment to reword).**
