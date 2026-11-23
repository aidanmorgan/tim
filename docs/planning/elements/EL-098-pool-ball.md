# EL-098 · Pool ball — named-identity spec

**Current delivery (24 October 2026):** [EL-098 delivery slice](../invest/named-elements.md#element-098). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

**Build-ready revision, 22 November 2026 (checkout `8d1d68aa`), repaired the same day against the independent audit and the owner decisions of 22 November 2026.** A `path:line` citation without a commit is in this checkout; `path@a6c914e:L…` resolves in git history. Values marked **starting value** are adopted under decision D1 of [delivery batches](../invest/delivery-batches.md#3-family-level-decisions). Game values are canonical f32 ([numeric contract](../../gpu-f32-physics.md#precision-status)).

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name / type | EL-098 · Pool ball · Specialist |
| Anchors | [requirements.md#element-098](../requirements.md#element-098) (task `sequence-task-651`); [named entry](../invest/named-elements.md#element-098); scope index [todo-227](../requirements.md#todo-227) |
| Proof owner | S668 |
| Disposition | To deliver as a single element in the delivered zero-parameter ball shape. [Delivery batches 1b](../invest/delivery-batches.md#1b-runs-each-its-own-slice-planning-reference-only) lists it in the "Programmable parts" run with EL-076; EL-076 waits for two further engine stories, so this identity is delivered on its own. It differs from every delivered ball in radius, mass, bounce, friction and rolling resistance, so it is not a duplicate. |
| Planned package | id `pool_ball`, directory `elements/PoolBall/`, class `PoolBallElementDeclaration` |
| Refines / extends | The ball shape of [CAT-001 Basketball](CAT-001-ball.md) |
| Status | not started; blocked on the engine programme (owner decision 3a), then S7 |

## 2. Declaration

Follow the Bowling ball (`elements/BowlingBall/BowlingBallElementDeclaration.cs:12`). The package implements `IMechanicalElement` (`CuriousContraptions.Abstractions.Mechanical/MechanicalContracts.cs:7`), `ISceneGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`), `IAmbientLoadElement` (`MechanicalContracts.cs:124`) and `IAirflowReceiverElement` (`MechanicalContracts.cs:45`). `KinematicMode.Dynamic`, `SupportedDomains = Mechanical`, `PayloadPolicy.CaptureTarget`, footprint one body, one moving. Expression-bodied constant members only.

"Planar-table constraints" in the requirement are met physically: the ball rolls under world gravity on a flat bed (the bench, or a Wall laid flat) between Wall cushions. No joint or guide holds it to a plane; it can be knocked off the table, which is a puzzle outcome, not a defect.

### Body, collider and material

| Field | Value | Reason |
| --- | --- | --- |
| Sphere collider `col_0_0` (hidden) | radius 0.20 m | **starting value**: the smallest ball; 0.40 m across drops through the 0.46 m pocket of [EL-099](EL-099-pool-pocket.md) with 0.03 m each side |
| Mass | 0.4 kg | **starting value**: denser than the Tennis ball; under the Pressure plate's 0.5 kg default so one ball does not press it and two do. (The earlier 0.5 kg sat exactly on that threshold.) |
| Principal moments | 0.0064 kg·m² on each axis | solid sphere, 0.4 × 0.4 × 0.20² |
| Restitution | 0.9 | **starting value**: resin balls are very elastic; the highest ball value |
| Friction | 0.2 | **starting value**: polished; below the shared 0.3 |
| Rolling resistance | 0.01 | **starting value**: under a third of the Basketball's 0.035, so it rolls far |
| Bounce threshold | 0.1 m/s | the shared ball value |
| Gravity | (0, −9.81, 0) m/s² | world gravity |
| Ambient load | linear drag 0.04 1/s, no buoyancy | as `BowlingBallElementDeclaration.cs:61`–`:62` |
| Airflow receiver | four normalised samples of weight 0.25 | as `BowlingBallElementDeclaration.cs:32`–`:35` |

One collider. No joints, drives, stores, sensors, ports, events or parameters.

### Metadata and bindings

Category `Motion`; display name "Pool ball"; description "A small, hard, lively ball. Passes on nearly all its speed and rolls a long way."; colour (255, 248, 233)/255; pick radius 0.3; palette order 103 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)). `Parameters: []`. No presentation bindings.

### Art and solidity

Owner decision, 22 November 2026 (decision 11): the Pool ball has a flat mark, flush, with no band collider; this is an exception to the banded-ball rule for this ball only. No delivered package uses a texture, so the mark is made from geometry: the ball is drawn as two hemisphere meshes of the collider's radius, one cream and one navy, meeting on a great circle. Nothing is proud of the sphere and nothing is printed.

| Node | Mesh | Class | Moved by |
| --- | --- | --- | --- |
| `Visual/Visual_HalfCream` | hemisphere, radius 0.20, cream `#fff8e9`, satin | solid: together the two halves are the sphere of `col_0_0` | body pose |
| `Visual/Visual_HalfNavy` | hemisphere, radius 0.20, navy `#293954`, satin | solid: as above | body pose |

The two-tone ball turns with the body, so rolling and spin are visible. Icon: a disc split cream and navy. At a glance it differs from the Soccer ball and Baseball by having no raised band and by being the smallest ball, and from the Bowling ball by its split body.

Why flat (the audit's check, kept here): a band proud by h meets the bed at θ = acos(R ÷ (R + h)) ahead of the contact point. For R 0.20 m and h 0.01 m at 2.7 m/s, θ = 17.8°, the band strikes at 0.86 m/s and the ball hops about 9 cm and loses roughly 39 % of its spin, up to twice per 1.26 m. Rows A3 to A6 could not be met with a band.

### Behaviour and numbers

Laws: gravity, contact with restitution (the product of the two colliders' values, `CuriousContraptions.Simulation/WorkshopConstraintSolver.cs:117`), Coulomb friction (geometric mean, `:119`), rolling resistance (a bounded resisting torque of coefficient × arm, `:218`–`:231`), ambient drag.

- **Ball on ball.** Restitution 0.9 × 0.9 = 0.81. In a head-on hit between equal masses the struck ball leaves with (1 + 0.81) ÷ 2 = 0.905 of the striker's speed and the striker keeps 0.095. Two Basketballs: 0.55² = 0.30, so the struck ball gets 0.65 and the striker keeps 0.35. These are the speeds at separation.
- **Slide, then roll.** A struck ball leaves sliding, without spin. Bed friction √(0.2 × 0.3) = 0.245 slows it at 0.245 × 9.81 = 2.4 m/s² while spinning it up, until it rolls at 5/7 of its separation speed. Struck at 2 m/s it separates at 1.81 m/s and rolls at 1.29 m/s after 0.21 s. The striker, which arrived rolling, keeps its topspin and speeds up again after the hit (to about (5/7) × 0.095 v + (2/7) v = 0.35 v). So a speed read even 0.1 s after contact is not the collision result; rows A3 and A4 read at separation.
- **Cushion.** A Wall declares restitution 1 (`elements/Wall/WallElementDeclaration.cs:51`), so the normal speed at separation from a cushion is 0.9 of the speed before.
- **Drop.** Fall fraction 0.9² = 0.81 before drag; drag at 0.04 1/s over a 0.75 s fall removes about 3 % of speed each way, giving about 0.76.
- **Friction.** Ball on ball √(0.2 × 0.2) = 0.2; ball on a Wall bed 0.245. Ball-to-ball friction is therefore already the lower of the two through the one general rule; per-pair materials do not exist and are not needed.
- **Rolling.** The engine bounds a resisting torque of rolling resistance × normal force × radius, which for a solid sphere decelerates it at coefficient × g ÷ 1.4. With drag, a ball rolling at 2.7 m/s slows at about 0.01 × 9.81 ÷ 1.4 + 0.04 × 2.7 = 0.07 + 0.11 = 0.18 m/s², so after 6 m it still moves at about √(2.7² − 2 × 0.18 × 6) = 2.3 m/s. A Basketball slows at 0.035 × 9.81 ÷ 1.4 + 0.11 = 0.35 m/s² and is at about √(2.7² − 2 × 0.35 × 6) = 1.75 m/s after 6 m.
- **With the cue.** [EL-097](EL-097-pool-cue.md) at its default 3 m/s sends this ball away at about 2.7 m/s at separation and about 1.9 m/s once rolling.
- **Fit.** 0.40 m: drops into the Pool pocket, passes the pipe bore (1.3 m across), does not seat in the Toy cannon (window 0.6–0.84 m, `elements/Cannon/CannonElementDeclaration.cs:32`), and is below the 0.62 m the Hopper feeds singly. A default Bumper (32 J preload toward 8 m/s) brings it to 8 m/s with 12.8 J.

### Variant B (no-gravity table)

Owner decision, 21 November 2026: the no-gravity pool-table variant is decided at slice planning. This spec does not adopt or drop it. If the slice adopts it, the requirement fixes its form: "a separate explicitly fictional field rule", that is, a declared bounded gravity region of [EL-089 Gravity-effect pad](EL-089-gravity-effect-pad.md) (the uniform-field law owned by `ENGINE-FIELD-FORCE`) enclosing the table, labelled as fiction; the ball's declaration does not change and carries no gravity override. Variant A above is complete without it.

## 3. Engine capabilities

Map row families: ContactImpulse, EnvironmentState, FiniteLedger, GeometryQuery, JointConstraint (unused), RigidBodyDynamics, SlidingFriction. Tests marked † were opened for this revision.

| Capability | Status | Evidence |
| --- | --- | --- |
| Dynamic sphere with declared mass, moments, material and gravity | present | `BowlingBallElementDeclaration.cs:40`–`:59`; `WorkshopBowlingTests.BothBallsCompileAsDynamicSpheresWithTheirDeclaredMaterialsAndSolidInertia` † (`CuriousContraptions.tests/WorkshopBowlingTests.cs:40`) |
| Bounce by declared restitution; settle on the bench | present | `WorkshopConstraintSolver.cs:117`; `TennisBallTests.TennisReboundsAboveMatchedHeavierBallsAndSettlesOnTheBench` † (`CuriousContraptions.tests/TennisBallTests.cs:84`) |
| Momentum transfer between two balls by mass | present | `TennisBallTests.ConfiguredMassChangesMomentumTransferredToAnEqualRadiusPayload` (`TennisBallTests.cs:118`; body not opened) |
| Zero-parameter instance; forged parameter refused | present | `PayloadInstanceTests.ABareGenericPayloadRecordLoadsAndAnyParameterIsRefused` † (`CuriousContraptions.tests/PayloadInstanceTests.cs:384`) |
| Run, Reset, save, load; capture by a residence sensor | present | `PayloadInstanceTests.BothKindsFallAgainAfterExactResetAndReload` † (`:249`); `AReceiverCapturesEitherKindAndAShiftedMissNever` † (`:236`) |
| A 0.20 m sphere at 0.9 restitution in repeated ball-to-ball and cushion hits | present, untested | the solver has no radius or restitution special case, but no delivered test strikes two high-restitution balls together |
| Coverage admits a named-identity package | narrower | owner: coverage follow-on story S7 (section 7) |

Nothing is missing. Per-pair friction is not needed (Friction above). Variant B, if adopted, adds `ENGINE-FIELD-FORCE` as a prerequisite of that variant only. A rack of balls at rest costs solver time until `ENGINE-ISLANDS-SLEEP` lands; the 8-point restitution cut that story deletes (`WorkshopConstraintSolver.cs:685`–`:689`) could affect a many-ball break, which the slice's probe checks.

## 4. Sources and legacy

- Requirement: "Authored ball material and planar-table constraints define rolling behaviour"; outcome "No-gravity historical variant, if adopted, requires a separate explicitly fictional field rule" ([element-098](../requirements.md#element-098)).
- A search at `a6c914e` for "pool", "billiard" and "cue" found nothing.

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| L1 | Isolated drop: rebound ratio is bounce² | `CuriousContraptions.tests/PhysicsCalibrationTests.cs@a6c914e:L14-L67` | carry forward as row A2 with a game-grade band |
| L2 | Legacy test worlds could set gravity 0 | `CuriousContraptions.tests/PipeTests.cs@a6c914e:L65-L70` | do not carry forward as a ball setting; variant B uses only a declared field |
| L3 | Legacy ball parameters were Radius, Mass, Bounce, Buoyancy, Drag | `reference/cpu/BallPart.cs@a6c914e:L4-L13` | carry forward as fixed declared values |

**Files harvested:** `CuriousContraptions.tests/PhysicsCalibrationTests.cs`, `CuriousContraptions.tests/PipeTests.cs` (gravity set-up only), `reference/cpu/BallPart.cs`.

## 5. Acceptance outline

Requirement row: [element-098](../requirements.md#element-098). Planned test class `PoolBallTests`. The table in these rows is a Wall laid flat as the bed with Walls as cushions. "At separation" means the first committed tick after the pair stops touching, found within 2 ticks of the last contact tick.

| # | Given / When / Then | Planned test | Control | Boundary and tolerance |
| --- | --- | --- | --- | --- |
| A1 | Given the palette entry, when compiled, then one dynamic sphere with the section 2 values and no parameters | `ThePoolBallCompilesItsDeclaredSphereAndMaterial` | — | authored constants compared exactly |
| A2 | Given a Pool ball and a Basketball released with centres 3 m above the bench, then the Pool ball's fall fraction is 0.68–0.84 and exceeds the Basketball's by at least 0.3; both rest | `ItReboundsFarHigherThanABasketballAndRests` | the Basketball | predicted 0.76 against 0.28; 0.3 is over 0.8 m of peak height; rest within 3600 ticks as a stall guard (a lively ball bounces many times) |
| A3 | Given one Pool ball rolling at 2–3 m/s head-on into a resting one on the bed, then at separation the struck ball has 0.80–0.95 of the incoming speed and the striker at most 0.20 | `AHeadOnHitPassesOnNearlyAllItsSpeed` | two Basketballs in the same construction: the striker has at least 0.25 at separation | predicted 0.905 and 0.095 (Basketballs 0.35); read at separation because the slide changes both speeds within a tenth of a second |
| A4 | Given a Pool ball rolling square into a Wall cushion, then at separation its normal speed is 0.80–0.92 of the incoming speed | `ACushionReturnsItNearlyAsFast` | a Bowling ball has under 0.3 | predicted 0.9 and 0.14 |
| A5 | Given a Pool ball and a Basketball released from the same height on the same Ramp onto a level bed, each passing a first line at 2.5–3 m/s, then between that line and a second one 6 m on the Pool ball loses at least 0.2 m/s less than the Basketball and is still faster than 1.5 m/s | `ItRollsFurtherThanABasketball` | the Basketball | predicted losses 0.4 and 0.95 m/s (difference 0.55) and a final 2.3 m/s; both balls are already rolling at the first line, so no slide enters |
| A6 | Given a Pool ball at rest on the bed beside a moving one that misses it, then it moves less than 1 cm in 600 ticks | `AnUnstruckBallStaysWhereItIs` | A3 | 1 cm is under 3 % of its width |
| A7 | Given one ball on a default Pressure plate, then the plate stays off; two switch it | `OneBallLeavesADefaultPlateOffAndTwoSwitchIt` | two balls | minimum set to 0.4 kg: one ball switches it (inclusive) |
| A8 | Given Run, Reset, save and load, then pose and kind are restored and the second Run repeats A3's outcome | `ResetSaveAndLoadRestoreItAndTheShotRepeats` | — | a save naming `pool_ball` with any parameter is refused atomically (typed `OutOfRange`) |
| A9 | Given the package in the bundle, then registry, palette, save and worker admission accept it; the two hemisphere meshes are classified solid against the one sphere collider and nothing is drawn beyond 0.20 m | rows added to `PackageRuntimeParityTests`, `VisibleSolidColliderTests`, `BasketballResourceTests` | — | — |

"Planar-table constraints define rolling behaviour" is A3–A6: on a bed between cushions the ball's motion follows from its declared material alone. No ports, so no connection refusal applies; parameter refusal is in A8. Generic lifecycle: `CatalogueIntegrationTests.EveryAdmittedDeclarationRetainsAuthoredStateAcrossRunResetSaveLoad` (`CuriousContraptions.tests/CatalogueIntegrationTests.cs:273`). Retained CAT-001 acceptance (gravity, rolling, bounce, momentum, rest): A2–A6. The cue and pocket rows that use this ball are EL-097 A1–A2 and EL-099 A1–A4; they run in those slices.

### Integration

- Works with Wall and bench as bed and cushions, Bumper, Ramp, Receiver, Pressure plate, Ball detector (all delivered); EL-097 Pool cue and EL-099 Pool pocket (planned after it).
- Campaign: the coverage ledger has no pool-table row; [todo-227](../requirements.md#todo-227) says its identities keep "the original priority/adoption status and campaign reservations" (`docs/planning/requirements.md:6391`) without naming levels. Owner decision 24c places it in *Bank the Bounce* at **8** (chapter 1), on the normal gravity table (coordinator decision 35: the pool lessons use the normal table; variant B is decided at slice planning): introduction 8, practice 10, reuse 49 and 141 ([campaign coverage additions](../requirements.md#campaign-coverage-additions); brief in [campaign coverage](readiness/campaign-coverage.md#4-lesson-briefs)).
- A level that places a named-package part needs the lesson-fixture story S8 (the Coverage fixture rule on main, `tools/Coverage/CurrentCatalogue.cs:116`–`:120`).

### Delivery

- **Shape.** Single element, first of the three pool identities (ball, then cue, then pocket).
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a, 22 November 2026), which precedes every further element; no specific engine story of its own for variant A; then S7.
- **Form.** Port roles, node arity, target masks and mouth identities are written in the form `ENGINE-CAPABILITY-INSTANCES` leaves; the names here are today's (this part declares only `PayloadPolicy` and an airflow receiver).
- **Files.** `elements/PoolBall/` (project, declaration and `.uid`, `pool_ball.tscn` with the two hemisphere meshes and one hidden sphere collider, `icon.svg` and its import file; no texture file); `CuriousContraptions.tests/PoolBallTests.cs`; one `CuriousContraptions.slnx` line; the hand lists a ball package still edits (those named under "Direct consumers" in the ball-presets Family contract that S3 has not removed); the ledger row; the binding section once S7 admits it.
- **Review tier.** Engine tier: it has a *present, untested* row.
- **Disqualifiers the probe re-checks.** Two 0.9-restitution balls that gain energy or jitter in a head-on hit; a 0.20 m ball that fails a delivered small-collider admission bound; a bed made from a flat Wall that a rolling ball catches on; a visible-solid check that cannot classify two meshes against one collider.

## 6. Open questions

All resolved.

1. **Planar constraint:** table contact, no planar joint. Reason: contact gives rolling, cushion and jump behaviour from general laws; `IPlanarGuideElement` (`MechanicalContracts.cs:30`) is a bounded lateral guide used by the Receiver, not a plane constraint, and nothing in the requirement needs one. Decided under the standing delegation.
2. **Variant B:** decided at slice planning (owner, 21 November 2026; coordinator decision 35 keeps it there and gives the pool lessons the normal table); its form is fixed above. It is not an owner item.
3. **Ball-to-ball friction lower than ball-to-cloth:** already true through the geometric-mean rule; no per-pair material.
4. **No `number` parameter** (D13, D19): no requirement or lesson uses it. Levels tell balls apart by instance.
5. **Flat mark, no band collider:** owner decision, 22 November 2026 (decision 11), for this ball only. The mechanism is two hemisphere meshes, chosen here because no package has a textured material.
6. **Values:** starting values (D1).

Needs the owner: none.

## Engine conformance (22 November 2026)

1. **Declaration data only.** One body, one collider, a material, a mass frame, an ambient load and an airflow receiver. No constraint to a plane, no callback, no engine test on this package.
2. **Generic laws.** Gravity, contact with restitution, friction, rolling resistance, drag. "Pool" behaviour is those laws with these values on a flat bed.
3. **Three layers.** Colliders: one hidden sphere. Visuals: two hemisphere meshes that together are that sphere; nothing proud, nothing printed. Animation: none; the meshes follow the body pose.
4. **Flow and typing.** No ports, parameters or events; f32 constants.
5. **Flat state.** Bodies 1 (dynamic); colliders 1 (sphere); joints 0; drives 0; sensors 0; domain nodes 0; stores 0; airflow receivers 1; ambient loads 1; presentation bindings 0.
6. **Performance.** One primitive, fewer than the Basketball's two. A rack of fifteen is fifteen spheres in one island; nothing sleeps today, and the rack will sleep when `ENGINE-ISLANDS-SLEEP` lands. Its speeds (under 8 m/s) are far below the 40 m/s of the delivered no-tunnelling test (`PayloadInstanceTests.cs:209`), though that test uses larger balls; the probe re-checks 0.20 m.
7. **Tests are scenarios.** Every row runs the production simulation with delivered parts.

**Verdict: conforms** (the audit's fixes of 22 November 2026 are applied: the mark's mechanism, the owner decision, the read point of the collision rows and the rolling model).

## 7. Coverage binding (planned, not yet live)

Published main binds a named-identity package through a final section of the identity's spec headed "Package source binding", read by `tools/Coverage/NamedPackageSources.cs` (pattern: EL-239 Baseball). The section is not written here: the reader parses every spec that carries that heading and fails the coverage run when a field does not resolve. **Blocked on the coverage follow-on story S7.**

| Field | Planned value |
| --- | --- |
| Package | `pool_ball` |
| Named identity | EL-098, `../invest/named-elements.md#element-098` |
| Acceptance | `todo-227`, `sequence-task-651` |
| Decision | S668, `../invest/scope-corrections.md#s668` |
| Mode records | `Configuration=Fixed` (variant A); a second record only if the slice adopts variant B |

Its sequence task exists: `sequence-task-651` stands beside the `element-098` anchor (`docs/planning/requirements.md:5179`–`:5181`). What the reader lacks today: `scope-corrections.md` has no `s668` block; and the reader wants the todo and the task adjacent in one row, while `todo-227` is a scope index shared by thirteen identities (`:6389`) apart from the task.
