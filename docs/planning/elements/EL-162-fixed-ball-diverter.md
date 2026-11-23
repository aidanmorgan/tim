# EL-162 · Fixed ball diverter named-identity spec

**Current delivery (24 October 2026):** [EL-162 delivery slice](../invest/named-elements.md#element-162). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Story 7.0 named-identity spec (Batch J). Baseline commit `a6c914e`; every `path@a6c914e:Lstart-Lend` citation resolves with `git show a6c914e:<path> | sed -n 'start,endp'`. All values are canonical IEEE-754 f32 game values inside the [game-grade envelope](../../gpu-f32-physics.md#game-grade-envelope).

**Build-ready revision, 21 November 2026 (checkout `8d1d68aa`).** Sections 1–3, 5 and 6 were rewritten against current code; a `path:line` citation without a commit is in this checkout. Section 4 is the unchanged harvest. Values marked **starting value** are adopted under decision D1 of [delivery batches](../invest/delivery-batches.md#3-family-level-decisions).

## 1. Identity

| Field | Value |
| --- | --- |
| ID | EL-162 |
| Name | Fixed ball diverter |
| Type | Mechanical |
| Anchor | [requirements.md#element-162](../requirements.md#element-162); [named entry](../invest/named-elements.md#element-162); scope index [todo-246](../requirements.md#todo-246) |
| Proof owner | S373 |
| Disposition | To deliver as a single element, first of the diverter run (EL-162, then EL-163, then EL-164). The three share junction geometry but differ in ports, bodies and behaviour, so they are a run of three slices, not a Family slice, and none duplicates another. |
| Planned package | id `diverter_fixed`, directory `elements/DiverterFixed/`, class `DiverterFixedElementDeclaration` |
| Refines / extends | No CAT spec. Routes cargo between ramps ([CAT-054](CAT-054-ramp.md)) and receivers ([CAT-004](CAT-004-basket.md)) |
| Status | Not started; blocked on the engine programme (owner decision 3a), relying most on ENGINE-SPATIAL-QUERIES (the persistent broadphase its static boxes join) and ENGINE-ISLANDS-SLEEP; then S7. ENGINE-PARAMETER-BOUND-FRAMES (Choice clause) later turns `blade` into a two-value Choice (section 6). |

## 2. Declaration

Follow the Pipe's parameter-bound static geometry (`elements/Pipe/PipeElementDeclaration.cs:50`–`:54`). The package implements `IMechanicalElement` (`CuriousContraptions.Abstractions.Mechanical/MechanicalContracts.cs:7`), `ISceneGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`) and `IParameterizedGeometryElement` (`SceneGeometry.cs:29`). `KinematicMode.Static`, `PayloadPolicy.None`, zero mass frame, no ports, events, stores or joints. Footprint: 1 body, 0 moving.

### Shared junction frame (also used by EL-163 and EL-164)

Local axes: flow along +Z, lateral X, up Y; floor top at y = 0. Boxes are full extents in metres; walls are 0.5 high and 0.06 thick.

| Collider | Shape and place |
| --- | --- |
| Inlet floor | box 1.2 × 0.1 × 1.0, z from −1.0 to 0 |
| Delta and lane floor | box 2.4 × 0.1 × 1.6, z from 0 to 1.6 |
| Inlet side walls (2) | boxes 1.0 long at x = ±0.63 |
| Delta walls (2) | boxes 0.85 long, yawed ±45°, from (±0.63, 0) to (±1.23, 0.6) |
| Lane outer walls (2) | boxes 1.0 long at x = ±1.23, z from 0.6 to 1.6 |
| Divider | box 1.0 long at x = 0, z from 0.6 to 1.6 |

The inlet is one 1.2 m lane; it widens over 0.6 m into two parallel 1.17 m lanes, **lane A** on +X and **lane B** on −X. Parallel lanes keep every floor and lane wall axis-aligned; the earlier ±30° branches are dropped (section 6).

### Blade

A wedge of two boxes, each 0.85 × 0.5 × 0.06, yawed +45° and −45°, joined at an upstream apex at z = 0 and ending at z = 0.6. Its lateral position is the part's one parameter. With the apex against the −X inlet wall, one arm runs from that wall to the divider nose and seals lane B, and the other lies along the −X delta wall: every ball is turned into lane A. The mirror pose opens lane B. The open path between the sealing arm and the opposite delta wall is 0.79 m, 0.11 m more than the Basketball's 0.68 m. A ball can only push the wedge toward the wall it already rests on.

The wedge colliders are static colliders of the same body, moved by `ColliderParameterBinding` on `GeometryComponent.OffsetX` (`SceneGeometry.cs:24`–`:27`): offset = 1 × `blade` ± 0.3 for the two arms.

### Parameters

| Name | Type | Unit | Default | Min | Max | Editor policy | Run edit |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `blade` | Real | Metre | −0.6 | −0.6 | 0.6 | Reject | BuildOnly |

−0.6 opens lane A, +0.6 opens lane B. A collider binding accepts a Real parameter only (`CuriousContraptions.Bundle/BundleValidator.cs:419`), so the selection is a position, not a Choice. An intermediate value blocks both lanes, a visible jam: the open path beside the wedge is (0.63 − `blade`)·cos 45° − 0.06 m toward lane A and (0.63 + `blade`)·cos 45° − 0.06 m toward lane B, and both are under the Basketball's 0.68 m whenever |`blade`| is below about 0.42 m (0.39 m of path each side at `blade` 0). The wedge does not split a stream.

### Material, metadata and bindings

Restitution 0.1, friction 0.3, rolling resistance 0, bounce threshold 0.1 m/s (**starting value**: the gate's dead surface, `elements/PoweredGate/PoweredGateElementDeclaration.cs:47`, so a turned ball does not hop a 0.5 m wall). Category `Motion`, display name "Ball diverter", palette order 113 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)), colour (0.76, 0.56, 0.32). One parameter presentation binding moves the wedge art: `Visual/Wedge`, `PresentationProperty.LocalX`, parameter `blade`, multiplier 1 (the Pipe's collar bindings, `PipeElementDeclaration.cs:22`–`:24`).

### Art and solidity

| Node | Mesh | Class |
| --- | --- | --- |
| `Visual/Floor`, `Visual/Walls`, `Visual/Divider` | boxes matching the table above, Ramp colour `#c28f52` | solid, one collider each |
| `Visual/Wedge` | two gold `#f7cb52` boxes with a navy `#293954` cap box 0.1 × 0.04 × 0.1 on the apex | solid: both arms have colliders; the cap is covered by a third bound box collider of its size |
| `Visual/LaneMarks` | one cream stripe printed on lane A's floor and two on lane B's | decorative: printed flat |

Icon: a Y seen from above with a gold chevron closing one arm. It differs from EL-163 by having no motor housing or sockets, and from EL-164 by having no exit hoops.

### Behaviour and numbers

Contact and friction only. The junction is built level in its own frame; a Basketball needs about 1.1 m/s at the inlet to cross 2.6 m against its 0.035 rolling resistance, so lessons feed it from a ramp or place it with a fall. Lane width 1.17 m passes every delivered ball. A fast ball (8 m/s, the Bumper's target) meets a 45° face of restitution 0.1 and is turned, not thrown over the 0.5 m wall.

## 3. Engine capabilities

Map row families: ContactImpulse, EnvironmentState, FiniteLedger, FiniteWorkActuation, GeometryQuery, JointConstraint, RigidBodyDynamics, SignalPropagation, StateTransaction; the map notes that the fixed blade uses geometry and contact only.

| Capability | Status | Evidence |
| --- | --- | --- |
| Static body with several box colliders, some yawed, extracted from the scene | present | `CuriousContraptions.Abstractions/ColliderDescriptor.cs:40`; `PoweredGateTests.PoweredGateScene_ExtractsItsSleeveCollarsFrameSocketsAndThreePortLocators` (`CuriousContraptions.tests/PoweredGateTests.cs:56`) |
| A collider offset bound to a Real parameter | present | `SceneGeometry.cs:27`; `PipeElementDeclaration.cs:53`; `WorkshopPipeLengthTests.TheShellTubeSpansTheLengthToBothMouthsAndTheCollarsFollowItsEnds` (`CuriousContraptions.tests/WorkshopPipeLengthTests.cs:54`) |
| Art moved by the same parameter | present | `PipeElementDeclaration.cs:22`; `WorkshopPipeLengthTests.ArtworkAndPickRadiusFollowTheLength` (`WorkshopPipeLengthTests.cs:139`) |
| Parameter survives save, wire and refusal | present | `WorkshopPipeLengthTests.TheSaveCodecRoundTripsTheLength` (`:227`), `LengthsOutsideOneToEightMetresAreRejected` (`:34`) |

Not used, recorded so nobody plans on it: a collider pose selected by a Choice parameter does not exist today (the binding is Real-only; its owner is ENGINE-PARAMETER-BOUND-FRAMES, Choice clause), and neither does a parameter-rotated collider (`GeometryComponent` has extents, offsets and radius only). The design above needs neither. Nothing is missing; the stale harvest row asking for "a typed selection field in the construction record" is superseded by the generic parameter.

## 4. Sources and legacy

- Requirement: "Player-set blade routes actual balls into one chosen branch"; outcome "Wrong branch remains unavailable without physical leakage" ([element-162](../requirements.md#element-162)).
- Integration task under [todo-246](../requirements.md#todo-246): "A visible Y-junction selection sends the next ball down the chosen physical branch. Teach fixed selection first." Campaign: first use 21–30, reuse 41–50, 111–120, 136–150 ([campaign-element-coverage](../requirements.md#campaign-element-coverage)). Decision S257 applies to the inherited SignalPropagation only ([decisions](../invest/decisions.md#s257)).

| # | Fact | Citation | Disposition | Reason |
| --- | --- | --- | --- | --- |
| 1 | Gold blade art on the powered gate | `parts/PoweredGatePart.cs@a6c914e:L46-L47` | carry forward (palette) | Blade colour identity. |
| 2 | Accepted: a closed gate blocks the ball and only an open gate lets it through, also when the part is rotated (20°, 30°, 40°) | `CuriousContraptions.tests/PoweredGateTests.cs@a6c914e:L152-L178` | carry forward (behaviour) | A sealing blade, including rotated placement. |

No legacy diverter exists.

**Files harvested:**
- `parts/PoweredGatePart.cs` (blade art)
- `parts/PipeBendPart.cs` (material reference)
- `CuriousContraptions.tests/PoweredGateTests.cs`
- Searched with no hit for `diverter` or `junction`: `parts/`, `engine/` (including `engine/physics/` and `engine/bridge/`), `CuriousContraptions.tests/`, `content/puzzles.json`, `tools/`, `reference/`, `diagnostics/`.

## 5. Acceptance outline

Requirement row: [element-162](../requirements.md#element-162). Planned test class `DiverterFixedTests`. A ball "takes lane A" when its centre passes z = 1.6 with x > 0; "never enters lane B" means no committed sample has z > 0.7 and x < 0.

| # | Given / When / Then | Planned test | Control | Boundary and tolerance |
| --- | --- | --- | --- | --- |
| A1 | Given `blade` −0.6 and a Basketball rolled in at 2 m/s, when Run, then it takes lane A and never enters lane B; with +0.6 the lanes swap | `TheBladeSendsTheBallDownTheLaneItOpens` (theory over both ends and Basketball, Bowling ball) | the opposite blade end sends the same ball down the other lane | guard 10 s; lane membership is a sign test with the 0.06 m divider between |
| A1b | Given `blade` 0 and a Basketball rolled in at 2 m/s, when Run for 10 s, then it enters neither lane and comes to rest in the delta | `AHalfSetBladeBlocksBothLanes` | A1 | `blade` −0.3 also blocks the Basketball (path 0.60 m) |
| A2 | Given two balls 1 s apart, when Run, then both take the open lane | `TwoBallsInSuccessionBothTakeTheOpenLane` | A1 | exact count 2 and 0 |
| A3 | Given a Basketball at 8 m/s, when it strikes the wedge, then it never enters the closed lane and never clears the walls | `AFastBallCannotForceTheClosedLane` | A1 at 2 m/s | centre height stays below 0.5 + 0.34 m |
| A4 | Given the part yawed 30°, when Run, then A1 holds in the part's frame | `ARotatedJunctionRoutesTheSameWay` | — | — |
| A5 | Given `blade` −0.61, 0.61 or NaN, then the edit is refused and the construction is unchanged | `BladeOutsideItsRangeIsRefused` | −0.6 and 0.6 are admitted | exact limits; S4 row 2 replaces this test when it lands |
| A6 | Save and load keep `blade`; Run then Reset leaves every body as authored | `SaveLoadKeepsTheBladeAndResetChangesNothing` | — | generic rows: `CatalogueIntegrationTests.EveryAdmittedDeclarationRetainsAuthoredStateAcrossRunResetSaveLoad`, S4 rows 1 and 3 |
| A7 | Every drawn solid has its collider at `blade` −0.6, 0 and 0.6 | `VisibleSolidColliderTests` row for the package | — | 0.005 m fit, the test's existing allowance |

No ports, so no connection refusal applies.

### Integration

- Fed by ramps (CAT-054) and pipes; delivers to receivers (CAT-004) and baskets; all delivered. Followed by EL-163 and EL-164, which reuse this frame.
- Campaign: chapter 3 "Pipe Dreams", levels 21–30, "Y-diverter modes" (`docs/planning/requirements.md:2288`); reuse 41–50, 111–120, 136–150 (`:2398`). The todo-246 task "teach fixed selection first" is a level using only this part and delivered parts.
- Coverage binding: a final package-source-binding section in this spec (Package, Named identity, Acceptance, Decision, Mode records), as EL-239 on published main; blocked on S7 (section 7). A level that locks the blade as a fixture needs the lesson-fixture story.

### Delivery

- **Shape.** Single element slice, first of the diverter run. Delivery batches lists it in 1b "Static geometry"; the probe question recorded there ("a player-set blade angle that moves one collider") is answered here by a bound offset instead of an angle.
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), which includes ENGINE-SPATIAL-QUERIES and ENGINE-ISLANDS-SLEEP; then S7 (the coverage follow-on, which widens the reader, owner decision 15). S2–S4 optional. ENGINE-PARAMETER-BOUND-FRAMES (Choice clause) is not a prerequisite of this slice (section 6, item 5).
- **Files.** `elements/DiverterFixed/` (project, declaration, `diverter_fixed.tscn`, `icon.svg`); `CuriousContraptions.tests/DiverterFixedTests.cs` (compiled by the test project's one `*.cs` rule); one `CuriousContraptions.slnx` line; the binding section of section 7 once S7 admits it; until S3, the trim list and placement switch.
- **Review tier.** Data-only: every capability is present with a cited test and the diff is package, tests and list lines.
- **Disqualifiers the probe re-checks.** A bound offset that does not move a yawed box as expected; a Basketball catching between the sealing arm and the delta wall (then widen the lanes, a number change); a ball climbing the wedge at speed.

## 6. Open questions

All resolved.

1. **Open channel, not a tube junction** (D19): the row names a blade, the lesson needs the blade visible, and open lanes accept ramps directly.
2. **Parallel lanes replace the ±30° branches**, and the pivoting blade becomes a sliding wedge. Reason: a static collider can be moved by a bound offset but not rotated by a parameter, and the same wedge on a slider is the delivered supplied-slider capability for EL-163 and EL-164. Decided under the standing delegation.
3. **`branch` Choice becomes `blade` Real**, for the reason in section 2.
4. **Lane width 1.2 m and no built-in fall**: starting values under D1.
5. **The lane choice stays a Real for now** (coordinator decision under the owner's standing delegation, 22 November 2026). Reason: a collider binding refuses a Choice today (`BundleValidator.cs:419`), and waiting for one would block EL-162 to EL-164 on an engine slice for no gameplay gain; the half-set jam is honest and visible. **Deferred work, named:** a general capability by which a Choice parameter binds a collider component through one declared value per choice. Its owner is ENGINE-PARAMETER-BOUND-FRAMES, Choice clause (coordinator decision 16; `readiness/batch-l.md` section 4 item 4, which names EL-162 among its consumers). When it exists, `blade` is replaced forward by a two-value Choice in the same change as its saves and tests; no second parameter is kept.

Needs the owner: none.

## Engine conformance (22 November 2026)

Checked against the owner addendum of 22 November 2026 (AGENTS.md element-free engine gate and Tri-Graph rule; `_bmad/custom/element-free-engine-design.md`; `docs/gpu-f32-physics.md`).

1. **Declaration data only.** One static body of boxes, one Real parameter bound to three collider offsets and one art offset. No solver branch, callback or engine test on this package.
2. **Generic laws.** Contact, friction, restitution and gravity. A ball is routed because a 45° face is in its way, not because the part assigns it a lane.
3. **Three layers.** Colliders: twelve boxes on the static body (two floors, six walls, the divider, two wedge arms and the apex cap), the last three moved by `ColliderParameterBinding` at build time. Visuals: floor, walls, divider, wedge; lane marks printed flat; none is a collider, and each visible solid has its box. Animation: none during Run. `Visual/Wedge` is placed by a parameter presentation binding on the same `blade` value that places its colliders, so art and collider cannot disagree; it does not move in Run.
4. **Flow and typing.** No ports. `blade` is an f32 Real in metres. The two-way lane choice is a closed set carried as a Real: a recorded deviation from "closed sets are enums", accepted for now by the decision in section 6, with its owner, ENGINE-PARAMETER-BOUND-FRAMES (Choice clause), named.
5. **Flat state.** Bodies 1 (static); colliders 12 (boxes); joints 0; drives 0; sensors 0; domain nodes 0; stores 0; presentation bindings 1 (parameter).
6. **Performance.** Twelve static boxes against the Receiver's five: two lanes, a delta and a wedge, each a visible solid. All static, none cosmetic. No island sleeping exists today; a resting ball in a lane costs nothing only once ENGINE-ISLANDS-SLEEP (owner decision 3a) is delivered, ahead of this part.
7. **Tests are scenarios.** Rows A1 to A7 run the production simulation; none needs a code path of this part.

**Verdict: conforms, with one recorded deviation (point 4, the lane choice as a Real).**

## 7. Coverage binding (planned, not yet live)

Published main binds a named-identity package through a final section of the identity's spec headed "Package source binding", read by `tools/Coverage/NamedPackageSources.cs` (pattern: EL-239 Baseball on published main). The section is not written here: the reader parses every spec that carries that heading and fails the whole coverage run when a field does not resolve. **Blocked on the coverage follow-on story S7.** S7 widens the reader (owner decision 15), including for companion packages and TH, RAD and GAP identities.

| Field | Planned value |
| --- | --- |
| Package | `diverter_fixed` |
| Named identity | EL-162, `../invest/named-elements.md#element-162` |
| Acceptance | `todo-246`; no sequence task yet |
| Decision | S373, `../invest/scope-corrections.md#s373` |
| Mode records | `Configuration=Adjustable` (`blade`) |

What it lacks today: `scope-corrections.md` has no `s373` block on published main; `todo-246` is a scope index shared by EL-162, EL-163 and EL-164 with no sequence-task anchor directly after it, which the reader requires; and one task source can bind only one package.
