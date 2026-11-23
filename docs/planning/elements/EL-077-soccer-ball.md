# EL-077 · Soccer ball — named-identity spec

**Current delivery (24 October 2026):** [EL-077 delivery slice](../invest/named-elements.md#element-077). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

**Build-ready revision, 22 November 2026 (checkout `8d1d68aa`), repaired the same day against the independent audit and the owner decisions of 22 November 2026.** Written against current code, the drafted Family contract [spec-family-ball-presets](../../../_bmad-output/implementation-artifacts/spec-family-ball-presets.md) and the owner's engine-conformance addendum. A `path:line` citation without a commit is in this checkout; `path@a6c914e:L…` resolves in git history. Values marked **starting value** are adopted under decision D1 of [delivery batches](../invest/delivery-batches.md#3-family-level-decisions). Game values are canonical f32 ([numeric contract](../../gpu-f32-physics.md#precision-status)).

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name / type | EL-077 · Soccer ball · Specialist |
| Anchors | [requirements.md#element-077](../requirements.md#element-077) (task `sequence-task-630`); [named entry](../invest/named-elements.md#element-077); scope index [todo-217](../requirements.md#todo-217) |
| Proof owner | S647 |
| Disposition | Member of the Family slice **Ball presets** with [EL-091 Cannonball](EL-091-cannonball.md) ([delivery batches 1a](../invest/delivery-batches.md#1a-families-one-slice-each-if-the-probe-passes)). The third family member, EL-239 Baseball, is already published as `baseball`. Not a duplicate of any delivered ball: mass, radius and bounce all differ. |
| Planned package | id `soccer`, directory `elements/SoccerBall/`, class `SoccerBallElementDeclaration` |
| Refines / extends | The ball shape of [CAT-001 Basketball](CAT-001-ball.md); the heavy control is [CAT-014 Bowling ball](CAT-014-bowling.md) |
| Status | not started; blocked on the engine programme (owner decision 3a), then S7 |

## 2. Declaration

Follow the Bowling ball (`elements/BowlingBall/BowlingBallElementDeclaration.cs:12`), the delivered zero-parameter ball. The package implements `IMechanicalElement` (`CuriousContraptions.Abstractions.Mechanical/MechanicalContracts.cs:7`), `ISceneGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`), `IAmbientLoadElement` (`MechanicalContracts.cs:124`) and `IAirflowReceiverElement` (`MechanicalContracts.cs:45`). `KinematicMode.Dynamic`, `SupportedDomains = Mechanical`, `PayloadPolicy.CaptureTarget`, footprint one body, one moving, no constraints. The declaration file holds only expression-bodied members returning constants.

### Body, colliders and material

| Field | Value | Reason |
| --- | --- | --- |
| Sphere collider `col_0_0` | radius 0.32 m | **starting value**: just under the Basketball's 0.34 m; 0.64 m across fits the Toy cannon |
| Band collider `col_visual_band` | torus, centreline radius 0.316 m, tube radius 0.014 m, turned 35° about Z (the Basketball's pose, `elements/Ball/ball.tscn:17`–`:18`) | owner decision, 21 November 2026: each banded ball has one proud band with its own collider |
| Mass | 0.45 kg | **starting value**: below the Pressure plate's default 0.5 kg (`elements/PressurePlate/PressurePlateElementDeclaration.cs:39`), so one ball does not press it |
| Principal moments | 0.018432 kg·m² on each axis | solid sphere, 0.4 × 0.45 × 0.32² |
| Restitution | 0.65 | **starting value**: livelier than the Basketball (0.55), far above the Bowling ball (0.14) |
| Friction, rolling resistance, bounce threshold | 0.3, 0.03, 0.1 m/s | the shared ball values; rolling a little freer than the Basketball |
| Gravity | (0, −9.81, 0) m/s² | world gravity |
| Ambient load | linear drag 0.04 1/s, no buoyancy | as `BowlingBallElementDeclaration.cs:61`–`:62` |
| Airflow receiver | four normalised samples of weight 0.25 | as `BowlingBallElementDeclaration.cs:32`–`:35` |

Both colliders are hidden `col_` nodes, separate from the drawn meshes. No joints, drives, stores, sensors, ports, events or parameters. The widest dimension is 0.66 m across the band (0.32 + 0.01 each side).

### Metadata and bindings

Category `Motion`; display name "Soccer ball"; description "A light, lively ball. Bounces higher and presses less than heavy balls."; colour (255, 248, 233)/255; pick radius 0.44; palette order 102 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)): it lists after every delivered Motion part, the Baseball (68) among them, and after the Programmable ball (101). `Parameters: []`. No presentation bindings: the root visual follows the one body's pose.

### Art and solidity

| Node | Mesh | Class | Moved by |
| --- | --- | --- | --- |
| `Visual/Visual_Sphere` | `SphereMesh` radius 0.32, cream `#fff8e9`, satin | solid: `col_0_0` | body pose |
| `Visual/Visual_Band` | `TorusMesh` inner 0.302, outer 0.33, navy `#293954` | solid: `col_visual_band`, the same torus | body pose |

Nothing is drawn beyond 0.33 m. Icon: a cream disc with one navy pentagon and five short seam strokes. At a glance it differs from the Baseball by size and by the colour of its band (navy; the Baseball's single band is coral), and from the Basketball by its pale body and single band.

### Behaviour and numbers

Laws: gravity, contact with restitution (the solver multiplies the two colliders' restitutions, `CuriousContraptions.Simulation/WorkshopConstraintSolver.cs:117`), Coulomb friction (geometric mean, `:119`), rolling resistance and ambient drag. Benches, Walls and Ramps declare restitution 1 (`elements/Wall/WallElementDeclaration.cs:51`), so the ball's own value sets its bounce.

- **Bounce.** Fall fraction f = (first rebound peak − rest height) ÷ (release height − rest height) = 0.65² = 0.42 before drag; drag at 0.04 1/s over a 0.7 s fall removes about 3 % of speed each way, giving about 0.39. Basketball 0.55² = 0.30; Bowling ball 0.14² = 0.02.
- **Load.** 0.45 kg is under the plate's 0.5 kg default; two balls are 0.9 kg.
- **Cannon.** 0.66 m lies inside the chamber's 0.6–0.84 m window (`elements/Cannon/CannonElementDeclaration.cs:32`). The default 45 J store would give √(2 × 45 ÷ 0.45) = 14.1 m/s, so the actuator's 12 m/s target (`:34`) is what limits it; it carries 0.5 × 0.45 × 12² = 32.4 J.
- **Bumper.** The default Bumper pays toward 8 m/s from a 32 J preload (`elements/Bumper/BumperElementDeclaration.cs:36`, `:45`); 0.5 × 0.45 × 8² = 14.4 J, so the ball reaches the target.
- **Pipe and feeders.** 0.66 m passes the 1.3 m bore and lies inside the 0.62–0.78 m range the Hopper and shuttle feed singly (owner decision, 22 November 2026).
- **Band.** In the authored pose the band is 55° from the resting point, so a ball placed or dropped without spin lands on its sphere. Rolling, it meets the band as a 1 cm step at most twice a turn; the owner accepts this.

## 3. Engine capabilities

Map row families: ContactImpulse, EnvironmentState, FiniteLedger, GeometryQuery, JointConstraint (unused), RigidBodyDynamics, SlidingFriction. The Family contract's probe lists seventeen rows; the ones this identity turns on are below. Tests marked † were opened for this revision; the others are named by the contract's probe and were not re-opened.

| Capability | Status | Evidence |
| --- | --- | --- |
| Dynamic sphere with declared mass, moments, material and gravity | present | `BowlingBallElementDeclaration.cs:40`–`:59`; `WorkshopBowlingTests.BothBallsCompileAsDynamicSpheresWithTheirDeclaredMaterialsAndSolidInertia` † (`CuriousContraptions.tests/WorkshopBowlingTests.cs:40`) |
| Bounce by declared restitution, settle on the bench | present | `WorkshopConstraintSolver.cs:117`; `TennisBallTests.TennisReboundsAboveMatchedHeavierBallsAndSettlesOnTheBench` † (`CuriousContraptions.tests/TennisBallTests.cs:84`) |
| Zero-parameter instance; a forged parameter refused in a save | present | `BowlingBallElementDeclaration.cs:26`; `PayloadInstanceTests.ABareGenericPayloadRecordLoadsAndAnyParameterIsRefused` † (`CuriousContraptions.tests/PayloadInstanceTests.cs:384`) |
| Capture by a Receiver, with a shifted miss | present | `elements/Receiver/ReceiverElementDeclaration.cs:12`; `PayloadInstanceTests.AReceiverCapturesEitherKindAndAShiftedMissNever` † (`PayloadInstanceTests.cs:236`) |
| Run, Reset, save and load of a payload | present | `PayloadInstanceTests.BothKindsFallAgainAfterExactResetAndReload` † (`PayloadInstanceTests.cs:249`) |
| Contact-load threshold | present | `PressurePlateElementDeclaration.cs:39`; `PressurePlateTests.TheInstanceMinimumMassDecidesItsOwnGate` † (`CuriousContraptions.tests/PressurePlateTests.cs:407`) |
| Proud torus band on a dynamic ball, with its collider | present | `elements/Ball/ball.tscn` (`col_visual_stripe`); `CompoundMassAndRollingTests.EveryAssignedVisibleFeatureStopsAProbeAndLeavesAdjacentSpaceEmpty` (`CuriousContraptions.tests/CompoundMassAndRollingTests.cs:403`) |
| Airflow receiver, ambient drag | present | `MechanicalContracts.cs:45`, `:124`; drag asserted in the Bowling test above †; airflow by `FanGuidanceTests.ExistingPayloadsReceivePaidLateralGuidanceOnlyWithEnabledFlow` |
| Coverage admits a named-identity package | narrower | only for an identity with a decision block and a todo co-anchored with its task; owner: coverage follow-on story S7 (section 7) |

Nothing this part declares is missing in the engine, and no element-specific engine code is needed. The collaborators' passes it meets are owned by the engine programme: the Cannon's chamber queries by `ENGINE-SPATIAL-QUERIES`, the plate's and Bumper's contact rows by `ENGINE-CONTACT-OBSERVATIONS`, the Receiver's residence by `ENGINE-SENSOR-VOLUMES`. This part designs around none of them.

## 4. Sources and legacy

- Requirement: "Distinct lightweight ball uses measured authored contact properties"; outcome "Its bounce and load differ reproducibly from a heavy ball" ([element-077](../requirements.md#element-077)).
- A search at `a6c914e` for "soccer" over every Epic 7 deletion path found nothing. The ball facts below apply.

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| L1 | Isolated drop: rebound ratio is bounce² | `CuriousContraptions.tests/PhysicsCalibrationTests.cs@a6c914e:L14-L67` | carry forward as row A2, with a game-grade band instead of ± 0.015 |
| L2 | A dropped ball settles on the bench without sinking | `CuriousContraptions.tests/FloorTests.cs@a6c914e:L26-L48` | carry forward |
| L3 | Plate minimum 0.5 kg: one 0.35 kg ball does not press, two do | `CuriousContraptions.tests/PressurePlateTests.cs@a6c914e:L38-L62` | carry forward as row A3 |
| L4 | Legacy ball material: restitution = bounce, threshold 0.1, friction 0.3 | `reference/cpu/MachinePart.cs@a6c914e:L236-L236` | carry forward the values; do not carry forward the CPU path |

**Files harvested:** `CuriousContraptions.tests/PhysicsCalibrationTests.cs`, `CuriousContraptions.tests/FloorTests.cs`, `CuriousContraptions.tests/PressurePlateTests.cs`, `parts/catalog/pressure_plate.tres`, `reference/cpu/MachinePart.cs`.

## 5. Acceptance outline

Requirement row: [element-077](../requirements.md#element-077). Planned test class `SoccerBallTests`; the rows are SB1–SB5 and F1 of the Family contract, with its tolerances. Drop rows release every ball with its centre 3 m above the bench origin.

| # | Given / When / Then | Planned test | Control | Boundary and tolerance |
| --- | --- | --- | --- | --- |
| A1 | Given the palette entry, when compiled, then one dynamic body with the section 2 values and no parameters | `TheSoccerBallCompilesItsDeclaredSphereBandAndMaterial` | — | declared values are compared exactly: they are authored constants, not simulated floats |
| A2 | Given a Soccer ball, a Basketball and a Bowling ball released from one height, then the Soccer ball's f is 0.36–0.45 and exceeds the Basketball's by at least 0.06; all three rest | `ItReboundsHigherThanTheBasketballAndTheBowlingBallAndRests` | the Bowling ball (heavy) rebounds least | the 0.06 margin is a 16 cm peak difference on a 2.7 m fall, plainly visible; rest within 2400 ticks as a stall guard |
| A3 | Given one Soccer ball on a default plate, then the plate stays off; two balls, or one Bowling ball, switch it | `OneBallLeavesADefaultPlateOffAndTwoOrAHeavyBallSwitchIt` | the Bowling ball | plate minimum set to 0.45 kg: one ball switches it (inclusive) |
| A4 | Given a default-size Ramp at 10° and the ball 1.5 m up the slope, then it rolls off the lower end, travels at least 0.64 m beyond it, and 480 ticks later is slower than when it left | `ItRollsDownARampOverItsBandAndSlowsOnTheBench` | on the level bench it moves less than 1 cm in 600 ticks | distance and slowing only; no smooth-path or rest-height assertion (the band is a 1 cm step) |
| A5 | Given Run, Reset, save and load with a Receiver below, then pose and kind are restored, an aligned ball is captured and a shifted one is not | `ResetSaveAndLoadRestoreItAndAReceiverCapturesIt` | the shifted ball | a save naming `soccer` with any parameter is refused atomically (typed `OutOfRange`) |
| A6 | Given the package in the bundle, then registry, palette, save and worker admission accept `soccer`; every drawn solid has its collider and no collider reaches beyond the art | rows added to `PackageRuntimeParityTests`, `VisibleSolidColliderTests`, `BasketballResourceTests` | — | a duplicate id fails the bundle build |

No ports, so no connection refusal applies. Parameter refusal is the forged-parameter half of A5. Generic lifecycle: `CatalogueIntegrationTests.EveryAdmittedDeclarationRetainsAuthoredStateAcrossRunResetSaveLoad` (`CuriousContraptions.tests/CatalogueIntegrationTests.cs:273`). The retained CAT-001 acceptance (gravity, rolling, bounce, momentum transfer, rest) is covered by A2, A4 and the delivered `TennisBallTests.ConfiguredMassChangesMomentumTransferredToAnEqualRadiusPayload`.

### Integration

- Works with Receiver, Pressure plate, Ramp, Wall, Bumper, Toy cannon, Pipe, Fan (all delivered).
- Campaign: the coverage ledger row "Basketball, bowling, tennis, baseball, soccer and programmable physical presets" reserves first use in levels 1–10, reuse 21–30, 41–50, 91–100, 136–150 (`docs/planning/requirements.md:2396`); chapter 1 "On a Roll".
- A level that places a named-package part is refused by the Coverage fixture rule on main (`tools/Coverage/CurrentCatalogue.cs:116`–`:120`, as the Family contract records), so its first level needs the lesson-fixture story S8.
- Coverage binding: section 7.

### Delivery

- **Shape.** Family slice "Ball presets" with EL-091, one candidate commit and one approval, each identity with its own rows and tests.
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a, 22 November 2026), which precedes every further element; no specific engine story of its own; then S7 (the coverage follow-on).
- **Form.** Port roles, node arity, target masks and mouth identities are written in the form `ENGINE-CAPABILITY-INSTANCES` leaves; the names here are today's (this part declares only `PayloadPolicy` and an airflow receiver).
- **Files.** `elements/SoccerBall/` (project, declaration and `.uid`, `soccer.tscn`, `icon.svg` and its import file); `CuriousContraptions.tests/SoccerBallTests.cs` (the test project compiles every top-level `*.cs` by one rule); one `CuriousContraptions.slnx` line; the hand lists the Family contract names under "Direct consumers" that S3 has not yet removed; the ledger row; the binding section once S7 admits it.
- **Review tier.** Engine tier (all six layers) while the coverage capability is narrower and a shared test helper is edited, as the Family contract decides; data-only is re-judged at replanning.
- **Disqualifiers the probe re-checks.** A chamber window that no longer admits 0.66 m; a visible-solid check that rejects a torus band on a sphere; a narrower coverage reader than section 7 assumes.

## 6. Open questions

All resolved.

1. **Values** (radius, mass, bounce, rolling resistance): the spec's proposals are the starting values (D1). "Measured" in the requirement row is met by fixed authored values proven by A2 and A3.
2. **Airflow:** it responds through the receiver every delivered ball declares (Family contract decision 3).
3. **Fixed preset, no parameters** (D13); configurable balls are the Tennis ball and EL-076.
4. **Art:** one proud navy band with its collider (owner, 21 November 2026); the pentagon pattern appears only in the icon, so nothing printed needs a decision.

Needs the owner: none.

## Engine conformance (22 November 2026)

1. **Declaration data only.** One body, two colliders, a material, a mass frame, an ambient load and an airflow receiver. No solver branch, callback or engine test on this package.
2. **Generic laws.** Gravity, contact with restitution and friction, rolling resistance, ambient drag, the airflow receiver law, the contact-load and capture sensors of other parts. Each acceptance row is a consequence of those laws and the declared values.
3. **Three layers.** Colliders: one sphere and one torus on the body, both hidden `col_` nodes. Visuals: one sphere mesh and one torus mesh, each matching its collider. Animation: none; both art nodes follow the body pose.
4. **Flow and typing.** No ports, parameters or events; values are f32 constants.
5. **Flat state.** Bodies 1 (dynamic); colliders 2 (sphere 1, torus 1); joints 0; drives 0; sensors 0; domain nodes 0; stores 0; airflow receivers 1; ambient loads 1; presentation bindings 0.
6. **Performance.** Two primitives, the Basketball's count. Nothing sleeps today; a ball at rest will sleep with its island when `ENGINE-ISLANDS-SLEEP` lands, and nothing here depends on it. It is 0.64 m across and bounded to cannon and bumper speeds; tunnelling is excluded by the engine's general means, exercised for balls by `PayloadInstanceTests.APayloadDrivenIntoTheBenchAtFortyMetresPerSecondNeverTunnels` (`PayloadInstanceTests.cs:209`).
7. **Tests are scenarios.** Every row runs the production simulation with delivered parts.

**Verdict: conforms** (the audit's fixes of 22 November 2026, the owner-decision 3a wording, are applied).

## 7. Coverage binding (planned, not yet live)

Published main binds a named-identity package through a final section of the identity's spec headed "Package source binding", read by `tools/Coverage/NamedPackageSources.cs` (pattern: EL-239 Baseball). The section is not written here: the reader parses every spec that carries that heading and fails the coverage run when a field does not resolve. **Blocked on the coverage follow-on story S7.**

| Field | Planned value |
| --- | --- |
| Package | `soccer` |
| Named identity | EL-077, `../invest/named-elements.md#element-077` |
| Acceptance | `todo-217`, `sequence-task-630` |
| Decision | S647, `../invest/scope-corrections.md#s647` |
| Mode records | `Configuration=Fixed` |

Its sequence task exists: `sequence-task-630` stands beside the `element-077` anchor (`docs/planning/requirements.md:5011`–`:5013`). What the reader lacks today: `scope-corrections.md` has no `s647` block; and the reader wants the todo and the task adjacent in one row, while `todo-217` is a scope-index row (`:4085`) apart from the task.
