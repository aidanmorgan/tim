# EL-102 · Large-bore ball pipe — named-identity spec

**Current delivery (24 October 2026):** [EL-102 delivery slice](../invest/named-elements.md#element-102). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

**Build-ready revision, 22 November 2026 (checkout `8d1d68aa`).** Sections 1–3, 5 and 6 are written against current code; a `path:line` citation without a commit is in this checkout, and `path@a6c914e:Lstart-Lend` resolves with `git show a6c914e:<path>`. Values marked **starting value** are adopted under decision D1 of [delivery batches](../invest/delivery-batches.md#3-family-level-decisions). Game values are canonical f32.

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name / type | EL-102 · Large-bore ball pipe · Specialist |
| Anchor | [requirements.md#element-102](../requirements.md#element-102) ([sequence-task-655](../requirements.md#sequence-task-655)); [named entry](../invest/named-elements.md#element-102); scope index [todo-227](../requirements.md#todo-227) |
| Proof owner | S362 |
| Disposition | To deliver as a single element. It is a variant of the Clear pipe, not a duplicate: its bore, shell, collars and mouth bore are different declared values, and its acceptance (larger cargo passes, a mismatched join does not form) is its own. It is the only large-bore part today, so there is no Family slice to join |
| Planned package | id `pipe_large`, directory `elements/PipeLarge/`, class `PipeLargeElementDeclaration` |
| Refines / extends | [CAT-048 Clear pipe](CAT-048-pipe.md) (`elements/Pipe/`) |
| Status | not started; blocked on the engine programme (owner decision 3a), then S7. It needs no further engine story |

## 2. Declaration

Follow the Clear pipe exactly in shape (`elements/Pipe/PipeElementDeclaration.cs:13`): the package implements `IMechanicalElement` (`CuriousContraptions.Abstractions.Mechanical/MechanicalContracts.cs:7`), `ISceneGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`) and `IParameterizedGeometryElement` (`SceneGeometry.cs:29`). `KinematicMode.Static`, `SupportedDomains = Mechanical`, `PayloadPolicy.None`, zero mass frame. Footprint: 1 body, 0 moving, 0 constraints. No ports, stores, sensors, signal or supply nodes.

### Body and colliders

One static body, tube axis along local +X, dimensions in metres. Every radius is the Clear pipe's value × 1.0 / 0.65 (the bore ratio), rounded to the centimetre.

| Collider | Kind (`ColliderShapeKind`, `CuriousContraptions.Abstractions/ColliderDescriptor.cs:8`) | Large bore | Clear pipe (for comparison) |
| --- | --- | --- | --- |
| Shell | Tube | inner radius 1.00, outer radius 1.08, half-length `length`/2 + 0.14 | 0.65, 0.70, `length`/2 + 0.09 (`PipeElementDeclaration.cs:9`–`:12`, `:52`) |
| Collar, start | Torus, full sweep | centreline radius 1.10, tube radius 0.10 (spans 1.00 to 1.20), at x = −`length`/2 | centreline 0.715, tube 0.065 |
| Collar, end | Torus, full sweep | as the start collar, at x = +`length`/2 | as above |

Material for all three: restitution 0.15, friction 0.3, rolling resistance 0, bounce threshold 0.1 m/s, the pipe family's (`PipeElementDeclaration.cs:62`). Collider bindings are the Clear pipe's three (`:50`–`:55`) with the mouth reach 0.14 in place of 0.09.

**Mouths.** Two `TubeMouthDescriptor` rows (`SceneGeometry.cs:39`), `TubeEnd.Start` and `TubeEnd.End`, each with `BoreRadius` 1.0, 0.14 m beyond its collar, bound to `length` as the Clear pipe's (`PipeElementDeclaration.cs:46`–`:47`). `TubeEnd` is today's mouth identity; `ENGINE-CAPABILITY-INSTANCES` replaces it with declared snap sockets and the package is written in the form that story leaves.

### Parameter

| Name | Type | Unit | Default | Min | Max | Editor policy | Run edit |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `length` | Real | Metre | 3.6 | 2 | 8 | Reject | BuildOnly |

Default and maximum are the Clear pipe's (`PipeElementDeclaration.cs:39`). The minimum is 2 m (**starting value**): at 1 m the two 0.2 m collars and the 0.28 m of mouth reach would leave 0.6 m of visible shell on a tube 2.4 m across.

### Metadata and bindings

Category `Motion`, display name "Large clear pipe", palette order 112 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)), colour (0.4, 0.72, 0.79). Presentation: the Clear pipe's three parameter bindings (shell `ScaleY`, two collar `LocalX`; `PipeElementDeclaration.cs:22`–`:24`) and its resize sizing along X with the large dimensions. No presentation driver reads run state.

### Art and solidity

| Node | Mesh | Class |
| --- | --- | --- |
| `Visual/Visual_Shell` | clear cylinder, outer radius 1.08, `#66b8c9` at alpha 0.16 (`DESIGN.md:240`) | hollow, the Tube collider |
| `Visual/Visual_Collar_Start`, `Visual/Visual_Collar_End` | cream `#fff8e9` torus, inner 1.00, outer 1.20 | solid, one Torus collider each |
| `Visual/Visual_Band_Start`, `Visual/Visual_Band_End` | navy `#293954` stripe printed on each collar's outer face, flush | decorative: printed flat |

Icon: the pipe icon drawn fatter, with a large ball inside and a double ring at each end. It differs from the Clear pipe at a glance by its diameter (2.4 m across its collars against 1.56 m) and the navy band on each collar, not by colour.

### Behaviour and numbers

Contact, friction and restitution against the hollow Tube carry cargo through the bore under gravity; nothing boosts or teleports it. What passes is decided by collision with the declared colliders, and what joins is decided by the declared mouth bore.

| Cargo | Across | Clear pipe (bore 1.30 across) | Large pipe (bore 2.00 across) |
| --- | --- | --- | --- |
| Tennis ball, default radius 0.25 | 0.50 | passes, clearance 0.80 | passes, clearance 1.50 |
| Bowling ball, radius 0.28 | 0.56 | passes | passes |
| Basketball, radius 0.34 | 0.68 | passes, clearance 0.62 | passes, clearance 1.32 |
| Tennis ball with `radius` 0.8 (`elements/TennisBall/TennisBallElementDeclaration.cs:18` admits 1/16 to 16) | 1.60 | **blocked** at the collar: 1.60 > 1.30 | **passes**, clearance 0.40 |
| Tennis ball with `radius` 1.1 | 2.20 | blocked | **blocked** at the collar: 2.20 > 2.00 |

A ball of radius r rolling inside rests with its centre (1.0 − r) from the axis: 0.20 m for the 0.8 m ball. A blocked ball rests in the collar's opening on the torus, its centre outside the mouth plane. The planned Cannonball (EL-091) and Programmable box (EL-080) are checked against both bores in their own specs.

**Joining.** Mouths snap only when their bores agree within 1e-6 m (`engine/TubeMouthSnap.cs:62`; `CuriousContraptions.Abstractions/PhysicalTolerance.cs:39`). A large mouth (bore radius 1.0) therefore snaps to another large mouth and never to a 0.65 mouth of the Clear pipe, either bend, the Funnel outlet or the Powered gate, nor to the Funnel's wide inlet, whose declared bore radius is 1.3 (`CuriousContraptions.tests/TubeMouthSnapTests.cs:17`). A mismatched pair is simply not a snap candidate: the part stays where the player put it and the construction is unchanged. No typed refusal exists for it and none is needed, because free placement of two unsnapped tubes is legal. There is no adapter part.

## 3. Engine capabilities

Map row families: ContactImpulse, EnvironmentState, FiniteLedger, GeometryQuery, JointConstraint, RigidBodyDynamics, SlidingFriction (navigation only under D14; the part has no joint and no ledger).

| Capability | Status | Evidence |
| --- | --- | --- |
| Static body compiled with a Tube and two Torus colliders and the pipe material | present | `ColliderDescriptor.cs:8`; `StraightPipeTests.WorkshopPhysicsCompiler_CompilesPipeIntoStaticBodyWithItsThreeHollowColliders` (`CuriousContraptions.tests/StraightPipeTests.cs:94`) |
| A ball rolls through a hollow Tube on its wall at every length | present | `HollowElementTests.ABallRollsThroughAStraightPipeOfEveryLengthOnItsWallWithoutCatching` (`CuriousContraptions.tests/HollowElementTests.cs:299`) |
| Placement refuses a body overlapping the hollow wall while bore and outside stay clear | present | `HollowElementTests.AHollowWallOverlapIsRefusedAtomicallyWhileBoreAndOutsideRemainClear` (`HollowElementTests.cs:164`) |
| Mouths carry a bore and snap only to an equal bore | present | `TubeMouthDescriptor` (`SceneGeometry.cs:39`); `TubeMouthSnapTests.BoresMustBeEqualWithin1EMinus6Metres` (`TubeMouthSnapTests.cs:269`), built on a synthetic two-mouth tube with an arbitrary bore |
| Length bound to the shell half-length, collar offsets and mouth positions | present | `PipeElementDeclaration.cs:46`–`:55`; `HollowElementTests.cs:299` runs lengths 1, 3.6 and 8 |
| A Tube of inner radius 1.0 and a Torus of centreline 1.10 on a placed part | present, untested | every delivered tube part has bore radius 0.65; no admission bound on Tube or Torus radius was found in `CuriousContraptions.Bundle/BundleValidator.cs`, and no test compiles a wider one |
| Workbench placement, capture distance and resize handles for a part 2.4 m across | present, untested | snap capture distance is a constant 0.45 m (`engine/TubeMouthSnap.cs:24`), independent of bore |

Nothing is missing. Contact candidates for the wide Tube come from the broadphase that `ENGINE-SPATIAL-QUERIES` (owner decision 3a) makes persistent; the part asks nothing of it beyond what every collider does.

## 4. Sources and legacy

- Requirement: "Scaled physical bore supports correspondingly larger cargo"; outcome "Too-large cargo remains blocked; size is validated rather than guessed from art" ([element-102](../requirements.md#element-102)).
- Decision D12 keeps metal pipes at the bore 1.3 m across and proves "oversized cargo jams" with a ball whose `radius` is set above the bore; this spec uses the same proof at both bores.
- Legacy search at `a6c914e` for "large bore", "bore" and "oversize" found no large-bore source. [CAT-048](CAT-048-pipe.md) holds the full pipe harvest.

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| L1 | The standard pipe passes a 0.34 m ball; a 0.8 m ball hits the mouth and stays upstream | `CuriousContraptions.tests/PipeTests.cs@a6c914e:L56-L80` | Carry forward: the large bore passes what the standard blocks and still blocks cargo larger than itself |
| L2 | Only pipe length may change; the bore is fixed | `engine/gpu/WorkshopPipe.cs@a6c914e:L21-L27` | Carry forward: the large bore is a separate part, not a resize of the standard bore |
| L3 | Clear shell at alpha 0.16 with cream collars | `parts/PipePart.cs@a6c914e:L15-L30` | Carry forward the look; do not carry forward the adapter or its rails (the delivered pipe draws none) |

**Files harvested:** `CuriousContraptions.tests/PipeTests.cs`, `parts/PipePart.cs`.

## 5. Acceptance outline

Requirement row: [element-102](../requirements.md#element-102). Planned test class `PipeLargeTests`. "Passes" means the ball's centre leaves the far mouth plane; "blocked" means it never crosses the near collar plane.

| # | Given / When / Then | Planned test | Control | Boundary and tolerance |
| --- | --- | --- | --- | --- |
| A1 | Given a large pipe tilted 10° downhill and a Tennis ball with `radius` 0.8 released in its upper mouth, when run, then the ball leaves the lower mouth | `ALargeBallRollsThroughTheLargeBore` | the same ball at the same tilt at a Clear pipe's mouth is blocked (A2) | 10 s stall guard; while between the collar planes the centre is never farther than 0.22 m from the axis (0.20 on the wall plus 0.02; farther would be inside the wall) |
| A2 | Given the same 0.8 m ball rolled at a Clear pipe's mouth, then it never enters | `TheSameBallIsBlockedByTheStandardBore` | a Basketball enters the same pipe | centre stays outside the collar plane for 5 s |
| A3 | Given a Tennis ball with `radius` 1.1 rolled at the large mouth, then it is blocked | `ABallWiderThanTheLargeBoreIsBlocked` | A1 | as A2; the pair 0.8 / 1.1 brackets the 1.0 bore radius with 0.2 m to spare each side, far above contact slop |
| A4 | Given default Basketball, Bowling ball and Tennis ball, then each rolls through at lengths 2, 3.6 and 8 | `DeliveredBallsRollThroughEveryLength` (theory) | — | 15 s stall guard at 8 m; each centre is never farther than (1.0 − r) + 0.02 m from the axis between the collar planes |
| A5 | Given a large pipe moved to within 0.2 m of another large pipe's free mouth, then it snaps and a 0.8 m ball crosses the joint; given the same move toward a Clear pipe's mouth, a bend's or the Funnel's outlet, then no snap is offered and the construction is unchanged | `LargeMouthsJoinOnlyLargeMouths` (theory over the tube parts) | large to large | the ball leaves the second pipe's far mouth within a 15 s guard and is never farther than 0.22 m from either axis |
| A6 | Given the declaration, then both mouths report bore radius 1.0 and the compiled Tube's inner radius is 1.0 and the collars' inner edge is 1.0 | `BoreMouthsAndCollidersAgree` | — | exact declared values (authored data, not simulated) |
| A7 | Length 1.9 and 8.1 are refused and the prior construction stands; Reset restores every cargo pose; save and load keep length and pose | `LengthIsValidatedAndStateRoundTrips` | lengths 2 and 8 are accepted | generic: `HollowElementTests.AnInvalidHollowLengthIsRefusedWithoutChangingThePriorConstruction` pattern, `CatalogueIntegrationTests.EveryAdmittedDeclarationRetainsAuthoredStateAcrossRunResetSaveLoad` (`CatalogueIntegrationTests.cs:273`) |

The part has no ports, so no connection refusal applies. A6 is the "validated rather than guessed from art" criterion: bore, mouth and collider are one declared number checked against each other.

### Integration

- Works with: Tennis ball (resizable; delivered), Basketball and Bowling ball (delivered), Ramp, Wall and Receiver (delivered); the planned Cannonball (EL-091) and Programmable box (EL-080). It joins no delivered tube part.
- Campaign: no chapter row named it; owner decision 24c gives it new level **24a** *Bore Necessities* (chapter 3): introduction 24a, practice 30, reuse 141 ([campaign coverage additions](../requirements.md#campaign-coverage-additions); brief in [campaign coverage](readiness/campaign-coverage.md#4-lesson-briefs)); a level that fixes one in place needs the lesson-fixture story (S8).
- Coverage binding: section 7.

### Delivery

- **Shape.** Single element slice.
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a: `ENGINE-SPATIAL-QUERIES`, `ENGINE-CONTACT-OBSERVATIONS`, `ENGINE-SENSOR-VOLUMES`, `ENGINE-ISLANDS-SLEEP`, `ENGINE-ONE-WAY-INSTALL`, `ENGINE-SUPPLY-DEMAND`, `ENGINE-CAPABILITY-INSTANCES`, `ENGINE-TICK-ALLOCATION`, `ENGINE-SOA-SIMD`); no further engine story; then S7. S3 optional.
- **Form.** Port roles, node arity, target masks and mouth identities are written in the form `ENGINE-CAPABILITY-INSTANCES` leaves; the names here are today's (`TubeEnd`, the tube-mouth binding).
- **Files.** `elements/PipeLarge/` (project, declaration, `pipe_large.tscn`, `icon.svg`); `CuriousContraptions.tests/PipeLargeTests.cs` (compiled by the test project's one `*.cs` rule); one `CuriousContraptions.slnx` line; until S3, the remaining per-package lists.
- **Review tier.** Engine: two rows are *present, untested* (a wide Tube and Torus; placement of a 2.4 m part).
- **Disqualifiers the probe re-checks.** A radius bound that refuses a 1.0 m Tube or a 1.10 m Torus; a tube test (`TubeMouthSnapTests.EveryTubePartDeclaresAStartAndEndMouthWithABoreFacingOutward`, `:87`) or visible-solid test that assumes every bore radius is 0.65 or the Funnel's; placement that cannot seat a part 2.4 m across on the bench.

## 6. Open questions

All resolved.

1. **Bore 2.0 m** (radius 1.0): **starting value** (D1); it passes cargo up to about 1.9 m across.
2. **Mismatched joins**: no snap, no adapter, no typed refusal (decided now; one current path, and free placement stays legal).
3. **No `BoreClass` enum.** The earlier draft's Standard/Large class is dropped: the mouth already carries its bore as a number and the snap rule compares it, so a class would be a second rule for the same behaviour.
4. **Cargo for the proof** is a resized Tennis ball (D12), so the slice does not wait for EL-080.
5. **Navy rails** dropped; the delivered pipe has none.

Needs the owner: none.

## Engine conformance (22 November 2026)

Checked against the owner addendum of 22 November 2026 (AGENTS.md element-free engine gate; `_bmad/custom/element-free-engine-design.md`; `docs/gpu-f32-physics.md`) and owner decision 3a.

1. **Declaration data only.** One static body, three colliders, two mouths, one parameter. No code path of its own.
2. **Generic laws.** Contact, friction and restitution against hollow shapes; the mouth-snap rule compares declared bores.
3. **Three layers.** Colliders: one Tube, two Tori. Visuals: shell and two collars, one mesh per collider, plus two flush printed bands. Animation: none; the three parameter bindings resize art in Build only. No art node moves in Run.
4. **Flow and typing.** One Real parameter in metres; `TubeEnd` and `ColliderShapeKind` are enums; no strings in domain logic.
5. **Flat state.** Bodies 1 (static); colliders 3 (Tube 1, Torus 2); joints 0; drives 0; sensors 0; domain nodes 0; stores 0; presentation bindings 3 (parameter).
6. **Performance.** The Clear pipe's collider count exactly; static. Its contact candidates come from the shared broadphase (`ENGINE-SPATIAL-QUERIES`).
7. **Tests are scenarios.** Every row runs the production simulation or the production snap query with delivered balls; no row asserts a per-tick trajectory.

**Verdict: conforms.**

## 7. Coverage binding (planned, not yet live)

Published main binds a named-identity package through a final section of the identity's spec headed "Package source binding", read by `tools/Coverage/NamedPackageSources.cs` (pattern: EL-239 Baseball). The section is not written here, because the reader parses every spec that carries that heading and fails the whole run when a field does not resolve. **Blocked on the coverage follow-on story S7.**

| Field | Planned value |
| --- | --- |
| Package | `pipe_large` |
| Named identity | EL-102, `../invest/named-elements.md#element-102` |
| Acceptance | `todo-227`, `sequence-task-655` (`docs/planning/requirements.md:5213`) |
| Decision | S362, `../invest/scope-corrections.md#s362` |
| Mode records | `Configuration=Fixed` |

What it lacks today: `scope-corrections.md` has no `s362` block; and `todo-227` is a scope index shared by EL-097 to EL-106 that is not anchored beside `sequence-task-655`, which the reader's task-source rule requires. The sequence task itself exists.
