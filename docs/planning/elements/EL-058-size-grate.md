# EL-058 · Size grate declaration readiness spec

**Current delivery (24 October 2026):** [EL-058 delivery slice](../invest/named-elements.md#element-058). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Story 7.0 named-identity spec. Baseline commit `a6c914e`. Legacy citations use `path@a6c914e:Lstart-Lend` and stay retrievable from git history after the Epic 7 purge. Values marked **proposed** are design values the owner may revise.

Deepened to build-ready detail on 21 November 2026 against checkout `8d1d68aa`; every `path:line` outside section 4 was read in that checkout. Values are starting values under [D1](../invest/delivery-batches.md#3-family-level-decisions).

## 1. Identity

| Field | Value |
| --- | --- |
| ID | EL-058 |
| Name | Size grate |
| Type | Mechanical |
| Requirement anchor | [element-058](../requirements.md#element-058) (sequence-task-611); scope index [todo-320](../requirements.md#todo-320); integration [sequence-task-371](../requirements.md#sequence-task-371) |
| Named entry | [named-elements.md#element-058](../invest/named-elements.md#element-058); proof owner S367 |
| CAT spec refined or extended | none. Related: [CAT-001 ball](CAT-001-ball.md), [CAT-014 bowling](CAT-014-bowling.md) and the Tennis ball (the sizes it separates), [CAT-004 basket](CAT-004-basket.md) (catch below), [CAT-066 wall](CAT-066-wall.md) (static panel family) |
| Related identities | [EL-059 weight tray](EL-059-weight-tray.md) (sorts by mass), [EL-060 electromagnet](EL-060-electromagnet.md) (sorts by material), [EL-071 straight metal ball pipe](EL-071-straight-metal-ball-pipe.md) (its 0.62 m gauge was withdrawn by D12; no shared value remains) |
| Roadmap story | [EL-058 delivery slice](../invest/named-elements.md#element-058); canonical proof owner and prerequisite decisions are recorded there. |
| Status | Not started; blocked on the engine programme (owner decision 3a), relying most on ENGINE-SPATIAL-QUERIES (contact candidates against its five static boxes), ENGINE-ISLANDS-SLEEP (carried balls at rest) and ENGINE-SENSOR-VOLUMES (the Receiver capture of row G7); then S7. No package exists (`elements/` has no `SizeGrate`). Planning spec at build-ready detail |
| Disposition | To deliver as a single element. Not a duplicate: no delivered part has a size-selecting opening |
| Planned package | id `size_grate`, directory `elements/SizeGrate/`, class `SizeGrateElementDeclaration` |
| Coverage binding | Planned, not yet live (section 7); blocked on S7. No CAT number |
| delivery-batches class | 1b run "Static geometry" ("static boxes and multi-body scene geometry as in Wall, Funnel and Receiver"). **Confirmed** as declarable from data today; **corrected** on tier: the aperture contact it relies on has no exercising test, so the slice is engine tier (section 3) |

## 2. Declaration

Shape followed: `elements/Wall/WallElementDeclaration.cs` (static box body with parameter-bound extents) with the multi-box scene body of `elements/Receiver/ReceiverElementDeclaration.cs` and the per-collider offsets of `elements/Pipe/PipeElementDeclaration.cs:53`–`:54`.

**Interfaces.** `IMechanicalElement` (`CuriousContraptions.Abstractions.Mechanical/MechanicalContracts.cs:7`), `ISceneGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`), `IParameterizedGeometryElement` (`SceneGeometry.cs:29`). `KinematicMode.Static`, `PayloadPolicy.None`, zero mass frame, `SupportedDomains` Mechanical, footprint 1 body, 0 moving.

**Body and colliders.** One static body, five box colliders: a comb of four parallel tines joined by one head rail, giving three slots of clear width `opening`. Local frame: tines run along Z, slots are spaced along X, the top faces lie in the plane y = +0.06. With o = `opening`:

| Collider | Box half extents (m) | Centre (m) |
| --- | --- | --- |
| 0–3 tines | (0.06, 0.06, 1.20) | x = −1.5·o − 0.18, −0.5·o − 0.06, +0.5·o + 0.06, +1.5·o + 0.18; y 0; z 0 |
| 4 head rail | (1.5·o + 0.24, 0.06, 0.06) | (0, 0, −1.26) |

At the default the part is 2.34 m wide and 2.52 m long. The tips at z = +1.2 are open. Bindings are `ColliderParameterBinding` rows (`SceneGeometry.cs:27`): `OffsetX` on colliders 0–3 with (multiplier, offset) = (−1.5, −0.18), (−0.5, −0.06), (0.5, 0.06), (1.5, 0.18), and `HalfExtentX` on collider 4 with (1.5, 0.24). No `TubeMouthBindings`.

**Material** (`MaterialProperties` order restitution, friction, rolling resistance, bounce threshold): (0.3, 0.3, 0, 0.1). A dull bar: an oversize ball settles on the tines instead of bouncing off the part.

**Parameters.**

| Key | Kind | Unit | Default | Min | Max | Editor policy | Run edit |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `opening` | Real | Metre | 0.62 | 0.3 | 1.2 | Reject | BuildOnly |

**Ports, stores, events.** None. Passage is geometry only.

**Presentation.** `PickRadius` 1.8, no resize axes. Parameter bindings (`PresentationParameterBinding`, `CuriousContraptions.Abstractions/ElementPresentation.cs:119`): `LocalX` on `Visual/Tine_0`…`Visual/Tine_3` with the four collider (multiplier, offset) pairs; `ScaleX` on `Visual/HeadRail` (unit-length mesh) with (3, 0.48). No state bindings.

**Palette.** Category `ElementCategory.Structure`, palette order 511 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)), display name "Size grate", colour `#c28f52` (the wall family, `DESIGN.md:237`).

### Art and solidity

| Node | Mesh and dimensions (m) | Class |
| --- | --- | --- |
| `Visual/Tine_0`…`Tine_3` | box 0.12 × 0.12 × 2.4 | solid, colliders 0–3 |
| `Visual/HeadRail` | box (3·o + 0.48) × 0.12 × 0.12 | solid, collider 4 |
| `Visual/GaugeMark` | a ring inside a slot outline, printed flush on the head rail top | decorative: printed flat |

Colours (`DESIGN.md:80`): tines Cream `#fff8e9` (part-body cream, owner decision 4), head rail Navy `#1c2836`, gauge mark Gold `#f7cb52`. The earlier corner studs are dropped: a proud stud needs a collider. Icon: a four-tine comb seen from above with a small ball dropping through a slot and a large ball resting across two tines. At a glance it differs from the Wall by being open bars, not a slab, and from the Ramp by having no surface.

### Behaviour and numbers

- **Law.** Rigid contact only. A sphere of diameter d passes a slot when d < o and is carried on two tines when d > o, sitting √(r² − (o/2)²) above the tine tops. Nothing reads a part id, so identity cannot select passage.
- **Why parallel slots, not the 3 × 3 square grid first proposed.** In a square cell an oversize ball seats on four edges and sags up to its own radius (0.20 m for the Basketball at 0.62 m), so the grate clogs after one ball and cannot be cleared by tilting. On parallel tines the same ball rolls along the bars and leaves over the open tips when the part is pitched, while fitting cargo drops through: the grate sorts a stream. It also needs five colliders instead of eight and binds only the X components that delivered parts already bind.
- **Bar section 0.12 m.** The Wall's minimum thickness (`elements/Wall/WallElementDeclaration.cs:30`), the thinnest static box a delivered part admits.
- **Default 0.62 m against delivered balls.** Tennis ball 0.50 m across (radius default 0.25, `elements/TennisBall/TennisBallElementDeclaration.cs:18`), Bowling ball 0.56 m (`elements/BowlingBall/bowling.tscn:13`), Basketball 0.68 m (`elements/Ball/ball.tscn:21`). Pass and carry cases are judged against a ball's widest collider: the Basketball's band is 0.70 m across (`elements/Ball/ball.tscn:18`); the Tennis ball's seam is printed (owner decision 24i), so its widest collider is its sphere. The default passes the Tennis and Bowling balls with 0.06 m and 0.03 m clearance per side and carries the Basketball with at least 0.03 m interference per side.
- **Range 0.3–1.2 m.** 0.3 m stops every delivered ball; 1.2 m passes all three and stays under the 1.3 m pipe bore (`elements/Pipe/PipeElementDeclaration.cs:9`–`:12`), so a grate never needs to out-gauge a pipe.
- **Feeding speed.** A fitting ball crossing the slots sideways faster than about 1.5 m/s can skip a slot before it has dropped; that is ordinary motion, not a defect. Lessons feed along the slots or from above.

## 3. Engine capabilities (capability probe)

| Capability | Status | Symbol | Exercising test or owner |
| --- | --- | --- | --- |
| Static multi-box body from scene geometry | *present* | `ISceneGeometryElement` `SceneGeometry.cs:10`; Receiver `elements/Receiver/ReceiverElementDeclaration.cs:59` | `CuriousContraptions.tests/CatalogueIntegrationTests.cs:200` (payloads reach or miss the five-wall Receiver) |
| Sphere against a static box, no pass-through at speed | *present* | Wall `WallElementDeclaration.cs:49` | `CuriousContraptions.tests/WorkshopWallTests.cs:88` (4 and 64 m/s) |
| Collider extent and offset bound to a real parameter | *present* | `ColliderParameterBinding` `SceneGeometry.cs:27`; Pipe `PipeElementDeclaration.cs:53` | `CuriousContraptions.tests/WorkshopPipeLengthTests.cs:54`; `CuriousContraptions.tests/WorkshopWallTests.cs:15` |
| Art position and scale bound to a parameter | *present* | `PresentationParameterBinding` `ElementPresentation.cs:119`; Pipe `PipeElementDeclaration.cs:23` | `CuriousContraptions.tests/WorkshopPipeLengthTests.cs:139` |
| Parameter admission and refusal at every seam | *present* | `ParameterDescriptor` `CuriousContraptions.Abstractions/ParameterDescriptor.cs:52` | `CuriousContraptions.tests/WorkshopWallTests.cs:55` |
| Run/Reset and save/load of any placed package | *present* | generic | `CuriousContraptions.tests/CatalogueIntegrationTests.cs:273` |
| A sphere passing between, or carried on the edges of, two parallel static boxes | *present, untested* | generic sphere–box contact | none found (searched test names for edge, gap, slot, aperture, rail). No engine change is expected; the element's own tests are the first to exercise it. Owner if it fails: an engine repair slice, not this package |

Counts: 6 present, 1 present-untested, 0 narrower, 0 missing. No element-specific engine code is needed.

**Element dependencies.** Basketball, Bowling ball, Tennis ball and Receiver, all delivered.

### Engine conformance

- **Declaration data only.** One static body of five boxes and one geometry parameter. No sensor, port, callback or engine branch.
- **Generic laws.** Passing and carrying are rigid contact against the declared geometry; nothing reads a part id, so identity cannot select passage.
- **Layers.** Colliders: 5 static boxes (4 tines, 1 head rail). Visuals: tine and rail boxes and a printed gauge mark, none used as a collider. Animation: none; nothing moves any art node during a Run; art and colliders follow `opening` through Build-time parameter bindings, not a run-time driver. Visible = solid: each tine and the head rail has its own collider; the gauge mark is printed flat.
- **One-way flow and typing.** Nothing reads art. `opening` is a typed f32 parameter in metres.
- **Flat state.** 1 static body, 5 box colliders, 0 joints, 0 drives, 0 sensors, 0 domain nodes, 0 stores, 5 collider parameter bindings and 5 presentation parameter bindings.
- **Performance.** Five boxes, the Receiver's count, in place of the eight a grid would need. Static; no per-tick work beyond contact, whose candidates come from ENGINE-SPATIAL-QUERIES. No island sleeping exists today; a ball resting on the tines costs nothing only once ENGINE-ISLANDS-SLEEP (owner decision 3a) is delivered, ahead of this part.
- **Tests are scenarios.** Rows G1 to G11 run the production simulation with delivered balls.

## 4. Sources and legacy

- Requirements row: "Oversized object remains blocked; identity labels do not select passage." No variants.
- Integration task (todo-320): "Sorting depends on observable size, mass or material" ([requirements.md#todo-320](../requirements.md#todo-320)).
- Research row: "Material/size sorter — aperture geometry … physical apertures sort actual bodies; no hidden ID-based routing" (`docs/component-research.md@a6c914e:L82-L82`).

No legacy grate exists. Shared aperture facts:

| # | Fact | Citation | Disposition | Reason |
| --- | --- | --- | --- | --- |
| 1 | Hollow apertures report a minimum clearance that a passing ball must respect (bore ≥ 0.64 m for the 0.65 m tube). | `CuriousContraptions.tests/HollowGeometryTests.cs@a6c914e:L54-L55` | carry forward (pattern) | Aperture pass/fail is clearance against the body's actual radius. |
| 2 | Hollow contact surfaces were proof-graded with maximum surface error bounds. | `CuriousContraptions.tests/HollowGeometryTests.cs@a6c914e:L209-L213` | do not carry forward | Proof-grade certificate; game-grade envelope applies. |

**Files harvested:** `CuriousContraptions.tests/HollowGeometryTests.cs`. Searched with no hit: `parts/`, `engine/`, `content/puzzles.json`, `tools/Campaign`, `reference/` (terms: grate, sieve, sorter, aperture).

## 5. Acceptance outline

Requirement row: [element-058](../requirements.md#element-058).

- **Chrome UI recipe (pending, post-catalogue).** Place a Size grate pitched 10° with its tips low, a Receiver under the slots and a second Receiver beyond the tips; place a Bowling ball and a Basketball above the head end. Verify placement and `opening` from the committed read. Run: the Bowling ball is captured below, the Basketball beyond the tips.

Planned tests are in `SizeGrateTests` unless named otherwise. "Plane" is the tine-top plane. Stall guards are game seconds converted at the selected cadence.

| Row | Given / when / then | Planned test | Control | Boundary | Tolerance and stall guard |
| --- | --- | --- | --- | --- | --- |
| G1 | A level grate at the default; a Bowling ball released at rest 0.3 m above the middle slot. Then its centre goes at least 0.5 m below the plane | `AFittingBallDropsThroughASlot` | released over a tine centre line it rests on the flat 0.12 m tine top for 5 s and never passes through the bar | — | 0.5 m is two radii, clear of any resting pose; guard 5 s (free fall takes 0.4 s) |
| G2 | The same release with a Basketball. Then it rests on two tines and never goes below the plane | `AnOversizeBallIsCarriedOnTheTines` | G1 | — | centre stays at least 0.10 m above the plane (derived 0.14 m; 0.04 m covers edge-contact slop without admitting a ball that has dropped in); observed for 5 s |
| G3 | Requirement outcome "identity labels do not select passage": two Tennis balls differing only in `radius`, 0.28 and 0.33 m (spheres 0.56 m and 0.66 m across against the 0.62 m opening) | `TheSamePartPassesOrIsCarriedByItsRadiusAlone` | a Bowling ball (other id, 0.28 m) passes like the small Tennis ball | 0.03 m clear per side for the small ball; 0.02 m interference per side for the large ball | as G1 and G2 |
| G4 | `opening` boundaries: 0.52 carries and 0.62 passes the Bowling ball (0.56 m); 0.64 carries and 0.74 passes the Basketball (0.70 m across its band) | `TheOpeningDecidesPassageAtEachBall` | each pair is its own control | at least 0.02 m per side against the widest collider, one hundred times the contact allowance (`PhysicalTolerance.ContactSeparationMetres`) | as G1 and G2 |
| G5 | A grate pitched 10°, a Basketball and a Bowling ball released at the head end. Then the Basketball leaves beyond the tips and the Bowling ball ends below the plane before the tips | `APitchedGrateSortsAStream` | level grate: the Basketball stays on it | — | "beyond" is z > tip + 0.34 m; guard 15 s (derived roll time about 3 s) |
| G6 | A Basketball arriving on the tines at 8 m/s does not pass | `AFastOversizeBallDoesNotPassTheBars` | no grate: it crosses the plane | — | centre never more than 0.1 m below the plane within 3 s |
| G7 | Receiver below: the ball that passes is the captured one | `ThePassedBallIsCapturedBelowAndTheCarriedOneIsNot` | swap the two release points | — | exact: one `Captured` record whose `Other` is the Bowling ball; guard 10 s |
| G8 | Run, Reset, save and load with `opening` 0.9 and a carried ball | `ResetAndSaveLoadRestoreTheOpeningAndTheBalls`; placed alone it is covered by `CatalogueIntegrationTests.cs:273` | — | — | authored state compared as the generic test does |
| G9 | `opening` below 0.3, above 1.2 or non-finite is refused at every seam and changes nothing; 0.3 and 1.2 are admitted | S4 row R2 once delivered, else `AnOutOfRangeOpeningIsRefusedAtEverySeam` (pattern `WeightTests.cs:96`) | bounds admitted | one f32 step outside | exact refusal |
| G10 | A connection naming the grate is refused: it has no ports | S4 row R5 does not apply to a portless package; `AForeignPortIsRefused` (pattern `WorkshopWallTests.cs:141`) | — | — | exact refusal |
| G11 | Tines, head rail and their art follow `opening` at 0.3, 0.62 and 1.2 | `SizeGrateArtTests.BarsAndCollidersFollowTheOpening` | — | both limits | positions within 1 mm, far below any ball clearance |

## 6. Open questions

**Resolved**

1. Is `opening` adjustable? Yes, a declared parameter editable in the Workbench unless a level locks it: D11.
2. Should the grate emit a signal? No: D19 (no requirement row or lesson uses one). The map's SignalPropagation family is navigation only: D14. A passage occurrence is the delivered Ball detector's job.
3. Round holes or square openings? Decided now: parallel slots between tines, because a closed cell clogs on the first oversize ball and tines let it roll off (section 2).
4. Fixed bar thickness or uniform scaling with `opening`? Decided now: fixed 0.12 m, the tested Wall minimum, so a small opening does not thin the bars.

**Needs the owner**

None.

## 7. Integration

- **Collaborators.** Basketball, Bowling ball, Tennis ball, Receiver, Ramp, Funnel and Pipe (all delivered). EL-059 and EL-060 share the [sequence-task-371](../requirements.md#sequence-task-371) sorting task; this spec covers its size clause (G3, G5, G7) and needs neither of them.
- **Campaign.** First use levels 91–100, reuse 111–120 and 136–150 (`docs/planning/invest/campaign-reservations.md:75`). Owner decision 24e moves the first use to level **28a** in chapter 3 ([per-level allocation](readiness/campaign-coverage.md#6-per-level-allocation-and-progression-check-owner-decisions-24e-and-24f)).
- **S8.** A lesson that places the grate needs the lesson-fixture story S8 first.

### Coverage binding (planned, not yet live)

Published main binds a named-identity package through a final section of the identity's spec headed "Package source binding", read by `tools/Coverage/NamedPackageSources.cs` (on `origin/main`, `25df3cd5`; pattern: EL-239 Baseball). The section is not written here: the reader parses every EL spec that carries that heading and refuses one whose preconditions fail. **Blocked on the coverage follow-on story S7.**

| Field | Planned value |
| --- | --- |
| Package | `size_grate` |
| Named identity | EL-058, `../invest/named-elements.md#element-058` |
| Acceptance | `todo-320` and `sequence-task-611` |
| Decision | S367; `scope-corrections.md` has no `s367` block |
| Mode records | `Configuration=Adjustable` (it declares a parameter; confirmed at slice planning) |

What it lacks today: an `s367` anchor block in `docs/planning/invest/scope-corrections.md`, and its todo-320 anchor is a scope index, not immediately followed by its sequence-task-611 anchor in `requirements.md`. S7 widens the reader (owner decision 15), including for companion packages and TH, RAD and GAP identities.

## 8. Delivery

- **Slice shape.** One element, declaration data, art and tests only.
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), which includes ENGINE-SPATIAL-QUERIES, ENGINE-SENSOR-VOLUMES and ENGINE-ISLANDS-SLEEP; then S7 (the coverage follow-on, which widens the reader, owner decision 15). S3 only shortens the edit list. No other element.
- **Files added.** `elements/SizeGrate/SizeGrate.csproj`, `SizeGrateElementDeclaration.cs`, `size_grate.tscn`, `icon.svg`; `CuriousContraptions.tests/SizeGrateTests.cs`, `SizeGrateArtTests.cs`.
- **Files edited.** No test-project edit (published main compiles every top-level `*.cs` by one rule); until S3, `CuriousContraptions.slnx`, the trim list at `CuriousContraptions.tests/CSharpStaticGateTests.cs:703` and the parameter and placement switches at `CuriousContraptions.tests/PackageRuntimeParityTests.cs:483` and `:641` (generic list at `:653`); at delivery the binding section of section 7; this spec and `TODO.md`.
- **Review tier.** Engine tier (all six layers), because of the one *present, untested* row. If the planning scout finds an exercising test for edge-carried spheres, it is data-only.
- **Disqualifiers the probe must re-check.** G2 or G5 fails with plausible causes in contact (a sphere jitters, creeps or tunnels on box edges): stop and raise an engine repair slice; do not add a grate-specific collider or rule. A `ColliderParameterBinding` offset on a non-zero collider index failing to compile in a five-collider static body.
