# EL-091 · Cannonball — named-identity spec

**Current delivery (24 October 2026):** [EL-091 delivery slice](../invest/named-elements.md#element-091). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

**Build-ready revision, 22 November 2026 (checkout `8d1d68aa`), repaired the same day against the independent audit and the owner decisions of 22 November 2026.** Written against current code, the drafted Family contract [spec-family-ball-presets](../../../_bmad-output/implementation-artifacts/spec-family-ball-presets.md) and the owner's engine-conformance addendum. A `path:line` citation without a commit is in this checkout; `path@a6c914e:L…` resolves in git history. Values marked **starting value** are adopted under decision D1 of [delivery batches](../invest/delivery-batches.md#3-family-level-decisions). Game values are canonical f32 ([numeric contract](../../gpu-f32-physics.md#precision-status)).

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name / type | EL-091 · Cannonball · Specialist |
| Anchors | [requirements.md#element-091](../requirements.md#element-091) (task `sequence-task-644`); [named entry](../invest/named-elements.md#element-091); scope index [todo-197](../requirements.md#todo-197) |
| Proof owner | S661 |
| Disposition | Member of the Family slice **Ball presets** with [EL-077 Soccer ball](EL-077-soccer-ball.md) ([delivery batches 1a](../invest/delivery-batches.md#1a-families-one-slice-each-if-the-probe-passes)); the Baseball (EL-239) is already published. Not a duplicate of the Bowling ball: it is heavier (6 kg against 4 kg), larger (0.32 m against 0.28 m) and, unlike the Bowling ball, seats in the Toy cannon. |
| Planned package | id `cannonball`, directory `elements/Cannonball/`, class `CannonballElementDeclaration` |
| Refines / extends | The ball shape of [CAT-014 Bowling ball](CAT-014-bowling.md); payload for [CAT-016 Toy cannon](CAT-016-cannon.md) and [CAT-015 Bumper](CAT-015-bumper.md) |
| Status | not started; blocked on the engine programme (owner decision 3a), then S7 |

## 2. Declaration

The requirement separates cargo from launcher: the Cannonball declares its own mass and material, and every launcher moves it through generic contact and work. It carries no reference to any launcher and no launcher refers to it.

Follow the Bowling ball (`elements/BowlingBall/BowlingBallElementDeclaration.cs:12`). The package implements `IMechanicalElement` (`CuriousContraptions.Abstractions.Mechanical/MechanicalContracts.cs:7`), `ISceneGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`), `IAmbientLoadElement` (`MechanicalContracts.cs:124`) and `IAirflowReceiverElement` (`MechanicalContracts.cs:45`). `KinematicMode.Dynamic`, `SupportedDomains = Mechanical`, `PayloadPolicy.CaptureTarget`, footprint one body, one moving. The declaration file holds only expression-bodied members returning constants.

### Body, colliders and material

| Field | Value | Reason |
| --- | --- | --- |
| Sphere collider `col_0_0` | radius 0.32 m | **starting value**: 0.66 m across the band seats in the cannon's 0.6–0.84 m window (`elements/Cannon/CannonElementDeclaration.cs:32`) |
| Band collider `col_visual_band` | torus, centreline radius 0.316 m, tube radius 0.014 m, turned 35° about Z (the Basketball's pose, `elements/Ball/ball.tscn:17`–`:18`) | owner decision, 21 November 2026: one proud band with its own collider |
| Mass | 6 kg | **starting value**: "heavy", above the 4 kg Bowling ball; six times the Basketball |
| Principal moments | 0.24576 kg·m² on each axis | solid sphere, 0.4 × 6 × 0.32² |
| Restitution | 0.08 | **starting value**: cast iron barely rebounds |
| Friction, rolling resistance, bounce threshold | 0.3, 0.02, 0.1 m/s | a hard smooth sphere rolls a little further than the Bowling ball (0.03) |
| Gravity | (0, −9.81, 0) m/s² | world gravity |
| Ambient load | linear drag 0.04 1/s, no buoyancy | as `BowlingBallElementDeclaration.cs:61`–`:62` |
| Airflow receiver | four normalised samples of weight 0.25 | as `BowlingBallElementDeclaration.cs:32`–`:35` |

Both colliders are hidden `col_` nodes, separate from the drawn meshes. No joints, drives, stores, sensors, ports, events or parameters.

### Metadata and bindings

Category `Motion`; display name "Cannonball"; description "A heavy iron ball. Launchers give it far less speed than a light ball."; colour (85, 101, 115)/255; pick radius 0.44; palette order 134 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)). `Parameters: []`. No presentation bindings.

### Art and solidity

| Node | Mesh | Class | Moved by |
| --- | --- | --- | --- |
| `Visual/Visual_Sphere` | `SphereMesh` radius 0.32, slate `#556573`, satin | solid: `col_0_0` | body pose |
| `Visual/Visual_Band` | `TorusMesh` inner 0.302, outer 0.33, gold `#f7cb52` | solid: `col_visual_band`, the same torus | body pose |

Nothing is drawn beyond 0.33 m. Icon: a slate disc with a gold band and a short fuse stub (icon only). At a glance it differs from the Bowling ball by its band (the Bowling ball declares none) and its larger size, not by colour alone.

### Behaviour and numbers

Laws: gravity, contact with restitution (product of the two colliders' values, `CuriousContraptions.Simulation/WorkshopConstraintSolver.cs:117`), friction, rolling resistance, ambient drag, and whatever work a launcher's own declared law pays.

- **Toy cannon.** The default 45 J store (`CannonElementDeclaration.cs:17`) gives √(2 × 45 ÷ 6) = 3.87 m/s, below the actuator's 12 m/s target (`:34`), so the store limits it. A 1 kg Basketball leaves at √(2 × 45 ÷ 1) = 9.49 m/s. Ratio 0.41. Kinetic energy 0.5 × 6 × 3.87² = 45 J: never more than the work released.
- **Bumper.** The default Bumper holds 0.5 × 1 × 8² = 32 J and pays toward 8 m/s (`elements/Bumper/BumperElementDeclaration.cs:36`, `:45`). Reaching 8 m/s would need 192 J, so the store limits the Cannonball to about √(2 × 32 ÷ 6) = 3.3 m/s, while the Basketball reaches 8 m/s. Ratio about 0.41.
- **Bounce.** Fall fraction 0.08² = 0.006; the Bowling ball's is 0.02. The 3 cm difference on a 2.5 m fall is not visible, so distinctness from the Bowling ball is proven by launch speed and cannon seating, not by bounce.
- **Fit.** 0.66 m: inside the cannon window, through the pipe bore (1.3 m across), and inside the 0.62–0.78 m range the Hopper and shuttle feed singly (owner decision, 22 November 2026). The Bowling ball (0.56 m) and Tennis ball (0.5 m) never seat in the cannon.
- **Load.** It always presses a Pressure plate (minimum range 0.1–16 kg, default 0.5); no plate row is written (it teaches nothing).

## 3. Engine capabilities

Map row families: ContactImpulse, EnvironmentState, FiniteLedger, GeometryQuery, JointConstraint (unused), RigidBodyDynamics, SlidingFriction. Tests marked † were opened for this revision; the others are named by the Family contract's probe and were not re-opened.

| Capability | Status | Evidence |
| --- | --- | --- |
| Dynamic sphere with declared mass, moments, material and gravity | present | `BowlingBallElementDeclaration.cs:40`–`:59`; `WorkshopBowlingTests.BothBallsCompileAsDynamicSpheresWithTheirDeclaredMaterialsAndSolidInertia` † (`CuriousContraptions.tests/WorkshopBowlingTests.cs:40`) |
| Zero-parameter instance; a forged parameter refused | present | `PayloadInstanceTests.ABareGenericPayloadRecordLoadsAndAnyParameterIsRefused` † (`CuriousContraptions.tests/PayloadInstanceTests.cs:384`) |
| Run, Reset, save and load of a payload | present | `PayloadInstanceTests.BothKindsFallAgainAfterExactResetAndReload` † (`PayloadInstanceTests.cs:249`) |
| Chamber seating by declared width window | present | `CannonElementDeclaration.cs:30`–`:32`; `ChamberOccupancyTests.WidthAndRelativeArrivalAdmissionIncludeTheirDeclaredBoundaries` (`CuriousContraptions.tests/ChamberOccupancyTests.cs:21`) |
| Finite-work launch of a seated payload | present | `CannonElementDeclaration.cs:33`–`:34`; `CannonIntegrationTests.PhysicalLoadChargesAndFiresOnlyWithItsSeparateSupply` (`CuriousContraptions.tests/CannonIntegrationTests.cs:550`) |
| Store-limited contact work | present | `BumperElementDeclaration.cs:39`–`:45`; `WorkshopBumperTests.FiniteBudgetCapsTheNormalBoostAndNeverPaysPastTarget` (`CuriousContraptions.tests/WorkshopBumperTests.cs:360`) |
| Proud torus band on a dynamic ball | present | `elements/Ball/ball.tscn` (`col_visual_stripe`); `CompoundMassAndRollingTests.EveryAssignedVisibleFeatureStopsAProbeAndLeavesAdjacentSpaceEmpty` (`CuriousContraptions.tests/CompoundMassAndRollingTests.cs:403`) |
| Coverage admits a named-identity package | narrower | owner: coverage follow-on story S7 (section 7) |

Nothing this part declares is missing in the engine. The launchers' passes it meets are owned by the engine programme: the Cannon's chamber queries by `ENGINE-SPATIAL-QUERIES` and the Bumper's contact work by `ENGINE-CONTACT-OBSERVATIONS`. This part declares nothing that works around them.

A later identity, EL-227 Magnetic impulse relay, needs a ferrous Cannonball. The responding property is declared on a body by the one rule `ENGINE-FIELD-FORCE` sets; when that story lands, this declaration gains that one declared value in the EL-227 slice. It is not part of this slice and nothing here anticipates it.

## 4. Sources and legacy

- Requirement: "Heavy spherical cargo declares material and mass independently of launcher identity"; outcome "Different launchers move the same body through generic contact and work" ([element-091](../requirements.md#element-091)).
- A search at `a6c914e` for "cannonball" found nothing. The launcher harvest is in [CAT-016](CAT-016-cannon.md).

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| L1 | The cannon seats only payloads 0.6–0.84 m wide | `parts/CannonPart.cs@a6c914e:L44-L45` | carry forward; current value at `CannonElementDeclaration.cs:32` |
| L2 | Radii 0.29 and 0.43 are unseated, keep the charge and are not launched | `CuriousContraptions.tests/CannonTests.cs@a6c914e:L413-L431` | carry forward as the size control of A2 |
| L3 | Release targets an axial speed with impulse limited by stored work (default 45 J) | `parts/CannonPart.cs@a6c914e:L46-L46` | carry forward: 3.87 m/s for 6 kg |
| L4 | A default shot of a 1 kg Basketball gives about 9.5 m/s | `CuriousContraptions.tests/CannonTests.cs@a6c914e:L38-L63` | carry forward as the light-ball comparison |

**Files harvested:** `parts/CannonPart.cs`, `parts/catalog/cannon.tres`, `CuriousContraptions.tests/CannonTests.cs`, `CuriousContraptions.tests/PhysicsCalibrationTests.cs`.

## 5. Acceptance outline

Requirement row: [element-091](../requirements.md#element-091). Planned test class `CannonballTests`; the rows are CB1–CB5 and F1 of the Family contract, with its tolerances.

| # | Given / When / Then | Planned test | Control | Boundary and tolerance |
| --- | --- | --- | --- | --- |
| A1 | Given the palette entry, when compiled, then one dynamic body with the section 2 values and no parameters; dropped from 3 m its fall fraction is at most 0.05 and it rests | `TheCannonballCompilesItsDeclaredValuesAndBarelyRebounds` | — | declared constants compared exactly; 0.05 is eight times the predicted 0.006, a rebound of at most 13 cm |
| A2 | Given a Cannonball in a charged default cannon and a Basketball in an identical one, when both fire, then both were seated and launched, the Cannonball's release speed is 0.30–0.55 of the Basketball's and its kinetic energy does not exceed the work released plus 0.5 J | `TheCannonLaunchesItFarSlowerThanABasketballWithinTheWorkReleased` | an uncharged cannon refuses `Uncharged` and the ball stays seated and still; a Tennis ball and a Bowling ball never seat | predicted ratio 0.41; the band allows for barrel friction on either ball; 0.5 J is 1 % of the store |
| A3 | Given a Cannonball and a Basketball each dropped 2 m onto a default Bumper, then the Bumper pays both, the Cannonball leaves at 2 m/s or more and at most 0.6 of the Basketball's speed | `TheBumperMovesItThroughTheSameContactWorkMoreSlowly` | Bumper strength 0: the Cannonball leaves at 1 m/s or less | predicted 3.3 m/s and ratio 0.41 |
| A4 | Given the Soccer ball's ramp construction (10° Ramp, 1.5 m up the slope), then it rolls off, travels at least 0.64 m beyond the lower end and is slower 480 ticks later | `ItRollsDownARampOverItsBandAndSlowsOnTheBench` | on the level bench it moves less than 1 cm in 600 ticks | distance and slowing only |
| A5 | Given Run, Reset, save and load, then pose and kind are restored and a second Run launches again | `ResetSaveAndLoadRestoreItAndASecondRunLaunchesAgain` | — | a save naming `cannonball` with any parameter is refused atomically |
| A6 | Given the package in the bundle, then registry, palette, save and worker admission accept `cannonball`; each drawn solid has its collider | rows added to `PackageRuntimeParityTests`, `VisibleSolidColliderTests`, `BasketballResourceTests` | — | a duplicate id fails the bundle build |

A2 and A3 together are the requirement's outcome: two different launchers move the same body, each by its own declared law, and neither launcher's code or data names the ball. No ports, so no connection refusal applies; parameter refusal is the forged-parameter half of A5. Generic lifecycle: `CatalogueIntegrationTests.EveryAdmittedDeclarationRetainsAuthoredStateAcrossRunResetSaveLoad` (`CuriousContraptions.tests/CatalogueIntegrationTests.cs:273`). Retained CAT-014 acceptance (gravity, rolling, bounce, rest): A1 and A4.

### Integration

- Works with Toy cannon, Bumper, Springboard, Ramp, Wall, Pipe, Pressure plate, Receiver (delivered); later with EL-095 Toy revolver and EL-097 Pool cue as further launchers (EL-097 row A6 moves a heavy ball) and EL-227 (above).
- Campaign: the coverage ledger has no cannonball row. Its launcher is in the row beginning "Pusher, boxing glove, flipper, wound launcher … cannon" with first use in levels 41–50 and reuse 61–100, 111–120, 136–150 (`docs/planning/requirements.md:2401`). Owner decision 24c places it in the Cannon's loading lesson *Load Before Fire* at **45** (chapter 5): introduction 45, practice 46, reuse 68 and 146 ([campaign coverage additions](../requirements.md#campaign-coverage-additions); brief in [campaign coverage](readiness/campaign-coverage.md#4-lesson-briefs)).
- A level that places a named-package part is refused by the Coverage fixture rule on main (`tools/Coverage/CurrentCatalogue.cs:116`–`:120`), so its first level needs the lesson-fixture story S8.

### Delivery

- **Shape.** Family slice "Ball presets" with EL-077.
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a, 22 November 2026), which precedes every further element; no specific engine story of its own; then S7.
- **Form.** Port roles, node arity, target masks and mouth identities are written in the form `ENGINE-CAPABILITY-INSTANCES` leaves; the names here are today's (this part declares only `PayloadPolicy` and an airflow receiver).
- **Files.** `elements/Cannonball/` (project, declaration and `.uid`, `cannonball.tscn`, `icon.svg` and its import file); `CuriousContraptions.tests/CannonballTests.cs`; one `CuriousContraptions.slnx` line; the hand lists the Family contract names under "Direct consumers" that S3 has not yet removed; the ledger row; the binding section once S7 admits it.
- **Review tier.** Engine tier while the coverage capability is narrower, as the Family contract decides.
- **Disqualifiers the probe re-checks.** A chamber window that no longer admits 0.66 m (the disqualifier named in delivery batches 1a; it passes today); a bumper store too small to move 6 kg at 2 m/s.

## 6. Open questions

All resolved.

1. **Mass 6 kg** is the starting value (D1); the hand calculations above show the cannon and the Bumper both move it at readable speeds.
2. **No Pressure plate role** (D19; Family contract decision 4).
3. **Distinctness from the Bowling ball** is proven by launch speed and cannon seating, not bounce (Family contract decision 6); the earlier control "the Bowling ball rebounds higher" is not an acceptance row.
4. **Art:** one proud gold band with its collider (owner, 21 November 2026); no fuse hole is drawn on the ball.

Needs the owner: none.

## Engine conformance (22 November 2026)

1. **Declaration data only.** One body, two colliders, a material, a mass frame, an ambient load and an airflow receiver. Nothing names a launcher.
2. **Generic laws.** Gravity, contact, friction, rolling resistance and drag for the ball; the chamber-and-finite-work law and the contact-work law belong to the launchers and apply to any body.
3. **Three layers.** Colliders: one sphere and one torus, both hidden `col_` nodes. Visuals: one sphere mesh and one torus mesh matching them. Animation: none; both nodes follow the body pose.
4. **Flow and typing.** No ports, parameters or events; f32 constants.
5. **Flat state.** Bodies 1 (dynamic); colliders 2 (sphere 1, torus 1); joints 0; drives 0; sensors 0; domain nodes 0; stores 0; airflow receivers 1; ambient loads 1; presentation bindings 0.
6. **Performance.** Two primitives, the Basketball's count. Nothing sleeps today; a ball at rest will sleep with its island when `ENGINE-ISLANDS-SLEEP` lands. Its launch speeds (under 4 m/s) are far below the 40 m/s the delivered no-tunnelling test drives a ball at (`PayloadInstanceTests.cs:209`).
7. **Tests are scenarios.** Every row runs the production simulation with delivered launchers.

**Verdict: conforms** (the audit's fixes of 22 November 2026, the owner-decision 3a wording, are applied).

## 7. Coverage binding (planned, not yet live)

Published main binds a named-identity package through a final section of the identity's spec headed "Package source binding", read by `tools/Coverage/NamedPackageSources.cs` (pattern: EL-239 Baseball). The section is not written here: the reader parses every spec that carries that heading and fails the coverage run when a field does not resolve. **Blocked on the coverage follow-on story S7.**

| Field | Planned value |
| --- | --- |
| Package | `cannonball` |
| Named identity | EL-091, `../invest/named-elements.md#element-091` |
| Acceptance | `todo-197`, `sequence-task-644` |
| Decision | S661, `../invest/scope-corrections.md#s661` |
| Mode records | `Configuration=Fixed` |

Its sequence task exists: `sequence-task-644` stands beside the `element-091` anchor (`docs/planning/requirements.md:5123`–`:5125`). What the reader lacks today: `scope-corrections.md` has no `s661` block; and the reader wants the todo and the task adjacent in one row, while `todo-197` is a scope-index row shared by EL-091 to EL-095 (`:4100`) apart from the task.
