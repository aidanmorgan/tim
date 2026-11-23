# EL-097 · Pool cue — named-identity spec

**Current delivery (24 October 2026):** [EL-097 delivery slice](../invest/named-elements.md#element-097). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

**Build-ready revision, 22 November 2026 (checkout `8d1d68aa`), repaired the same day against the independent audit and the owner decisions of 22 November 2026.** A `path:line` citation without a commit is in this checkout; `path@a6c914e:L…` resolves in git history. Values marked **starting value** are adopted under decision D1 of [delivery batches](../invest/delivery-batches.md#3-family-level-decisions). Game values are canonical f32 ([numeric contract](../../gpu-f32-physics.md#precision-status)).

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name / type | EL-097 · Pool cue · Specialist |
| Anchors | [requirements.md#element-097](../requirements.md#element-097) (task `sequence-task-650`); [named entry](../invest/named-elements.md#element-097); scope index [todo-227](../requirements.md#todo-227) |
| Proof owner | S667 |
| Disposition | To deliver as a single element. It uses the declaration shape of the delivered Linear pusher with different declared values, but it is not a duplicate: it has a 0.06 m tip on a long thin stick (the miss control depends on it), a non-zero tip restitution so it strikes rather than shoves, a short fast stroke, one parameter instead of four, and no end-position outputs. |
| Planned package | id `pool_cue`, directory `elements/PoolCue/`, class `PoolCueElementDeclaration` |
| Refines / extends | No CAT spec. Drive pattern of [CAT-039 Linear pusher](CAT-039-linear_pusher.md); supply from [CAT-005 Battery](CAT-005-battery.md) |
| Status | not started; blocked on the engine programme (owner decision 3a; of it, `ENGINE-SUPPLY-DEMAND` and `ENGINE-ISLANDS-SLEEP` bear on this part), then S7 |

## 2. Declaration

The cue is a contact actuator: a supplied slider drives a stick, and work reaches a ball only where the stick's tip actually touches it. There is no "hit this ball" command and nothing writes a velocity onto a target.

Follow the Linear pusher (`elements/LinearPusher/LinearPusherElementDeclaration.cs:8`): the package implements `ISliderElement` (`CuriousContraptions.Abstractions.Mechanical/MechanicalContracts.cs:15`), `ISupplyElement` (`CuriousContraptions.Abstractions.Electrical/ElectricalContracts.cs:11`) and `ISceneGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`). `KinematicMode.Static` base, `SupportedDomains = Mechanical | Electrical`, `PayloadPolicy.None`. Footprint: 2 bodies, 1 moving, 1 prismatic constraint.

The part's origin is on the surface it stands on, under the centre of its rest; +X is the strike direction.

### Bodies and colliders

Every collider is a hidden `col_` node of its own, separate from the drawn mesh it matches. (The pusher this part copies uses some visible meshes as collider proxies, which `ENGINE-CAPABILITY-INSTANCES` will refuse; this part does not copy that.)

- **Rest (static).** A U-shaped bridge: base plate box 0.6 × 0.1 × 0.3 m centred (0, 0.05, 0); two cheek boxes 0.6 × 0.2 × 0.1 m centred (0, 0.2, ±0.1), leaving a 0.10 m groove; one indicator sphere of radius 0.05 m centred (−0.2, 0.35, 0.1) on the +Z cheek. Material: restitution 0, friction 0.3, rolling resistance 0, bounce threshold 0.1 m/s (the pusher's base, `LinearPusherElementDeclaration.cs:27`).
- **Stick (dynamic, on the slider).** One box 1.6 × 0.06 × 0.06 m whose axis is the line y = 0.2, z = 0. At rest it spans x −0.8 to +0.8; the tip is its +X end face. It sits 0.02 m clear of each cheek and 0.07 m above the base plate. Mass 0.5 kg (**starting value**: the pusher head's mass, `:33`); principal moments 0.1067 kg·m² about each transverse axis (m L² ÷ 12 = 0.5 × 1.6² ÷ 12) and 0.0003 kg·m² about its length (m (0.06² + 0.06²) ÷ 12); gravity (0, −9.81, 0). Material: restitution 0.7, friction 0.5 (**starting values**: a leather tip grips and returns most of the hit), rolling resistance 0, bounce threshold 0.1 m/s.

The axis height of 0.2 m is the centre height of a [Pool ball](EL-098-pool-ball.md) standing on the same surface, so the tip meets it square. A larger ball is struck below its centre.

### Slider and drive

One `PrismaticSliderDescriptor` (`CuriousContraptions.Abstractions/MechanicalContractValues.cs:104`) along the part's +X using the pusher's axial frame (`LinearPusherElementDeclaration.cs:10`): lower limit 0 (tip at x = 0.8), upper limit 0.6 (tip at x = 1.4; the stick's rear end is then at x = −0.2, still 0.1 m inside the groove); no spring, no damping; `BodiesCollide` false. Its `PrismaticDriveDescriptor` (`MechanicalContractValues.cs:89`):

| Field | Value | Reason |
| --- | --- | --- |
| Target | 0.6 m | full stroke |
| MaxSpeed | parameter `speed` | the strike speed is the one thing a player tunes |
| StoppingAcceleration | 30 m/s² | reaches 3 m/s in 3² ÷ (2 × 30) = 0.15 m and brakes over the last 0.15 m |
| MaxEffort | 30 N | accelerating the 0.5 kg stick at 30 m/s² takes 15 N (19.9 N when mounted pointing up); a ball pinned against a wall stalls it |
| MaxPower | 120 W | 30 N × 4 m/s at the top speed; exactly the default Battery's power limit (`elements/Battery/BatteryElementDeclaration.cs:44`), 1 J per tick at 120 Hz |
| Selection | `PrismaticTargetSelection.Directional` | separate strike and return commands |
| Inactive | `PrismaticInactiveResponse.SelfLock` | no command, both commands or lost power hold the stick |
| Positive / Negative | supply nodes `positive`, `negative` | |

### Ports and supply

| `PortId` | Domain | Direction | Role (today's name) | Glyph | Local position |
| --- | --- | --- | --- | --- | --- |
| `PowerIn` | Electrical | Input | `PowerIn` | Power | base front face, (−0.2, 0.05, 0.15), normal +Z |
| `StrikeIn` | Electrical | Input | `FirstIn` | Extend | (0, 0.05, 0.15), normal +Z |
| `ReturnIn` | Electrical | Input | `SecondIn` | Retract | (0.2, 0.05, 0.15), normal +Z |

Both commands are electrical inputs, as the pusher's `ExtendIn` and `RetractIn` (`LinearPusherElementDeclaration.cs:45`–`:46`): a Battery feeds `PowerIn` and a switched supply (Impact switch, State latch, Pressure plate, Counter or Hold timer `Supply`) feeds a command. `FirstIn` and `SecondIn` are fixed-slot names that `ENGINE-CAPABILITY-INSTANCES` replaces; the slice writes whatever form that story leaves. Supply graph: the pusher's (`:52`–`:66`) without its two endpoint contacts and outputs: logic node `power`, consumer `motor` with a `SupplyDriveTarget` on prismatic coordinate 0, consumers `positive` and `negative`. Electrical nodes: `motor`, `ElectricalRole.Consumer`, 12 V, 1.2 Ω (12² ÷ 120 W), 0 F, rating 120 W; `distribution`, `ElectricalRole.LogicGate`, 12 V, 0.05 Ω, 0 F, 120 W. The node ratings are declared ratings that nothing reads; the drive's `MaxPower` is the bound, and the debit is the drive's committed work.

### Parameters

| Key | Kind | Unit | Default | Minimum | Maximum | Editor policy |
| --- | --- | --- | --- | --- | --- | --- |
| `speed` | Real | m/s | 3 | 1 | 4 | Reject, Build only |

No stores of its own: the work is paid from the supplying source each tick, within the drive's power bound.

### Metadata and bindings

Category `Power` (as the pusher); display name "Pool cue"; description "A supplied stick that strikes whatever its tip meets. A separate command draws it back."; colour (0.76, 0.56, 0.32); pick radius 1.0; palette order 311 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)). Presentation: body binding `Visual/Stick` → `PackageBodyIndex(1)`; `Visual/Indicator` recoloured from `PresentationDriver.CommittedPrismaticPhase` with the pusher's binding form (`LinearPusherElementDeclaration.cs:72`–`:76`): held slate `#556573`, moving gold `#f7cb52`, blocked amber `#e8b764`.

### Art and solidity

No delivered package uses a texture, so every colour change is its own mesh.

| Node | Mesh | Class | Moved by |
| --- | --- | --- | --- |
| `Visual/Base`, `Visual/CheekA`, `Visual/CheekB` | navy `#293954` boxes of the collider sizes | solid: `col_Base`, `col_CheekA`, `col_CheekB` | nothing (static) |
| `Visual/Indicator` | sphere radius 0.05 | solid: `col_Indicator` | nothing; recoloured by the phase binding |
| `Visual/Stick/Shaft`, `Visual/Stick/Ferrule`, `Visual/Stick/Tip` | three boxes end to end, 1.50, 0.05 and 0.05 m long, all 0.06 × 0.06 m in section: warm wood `#c28f52`, cream `#fff8e9`, navy | solid: together they are the one stick box `col_Stick`; nothing is proud | body 1's pose |
| `Visual/Sockets` | three discs set flush in the base's front face (coplanar with it) | decorative: flush | nothing |

Icon: a slanted stick over a U-shaped rest with a small ball ahead of the tip. It differs from the Linear pusher at a glance by the long thin stick and open bridge in place of a barrel and a round head.

### Behaviour and numbers

Laws: the prismatic drive law (speed, stopping acceleration, effort and power bounds; self-lock when inactive) enabled by the supply-network rule; contact with restitution (product of the two colliders' values, `CuriousContraptions.Simulation/WorkshopConstraintSolver.cs:117`) and friction (geometric mean, `:119`). Supply reaches the drive in the electrical phase and the stick moves in the physics step of the same tick ([solver model](../../gpu-f32-physics.md#solver-model)).

- **Strike window.** At the default 3 m/s the stick is at full speed while the tip has travelled between 0.15 m and 0.45 m. A ball whose near surface is 0.15–0.45 m ahead of the resting tip is struck at full speed; 0.3 m is the recommended placement. Closer, it is hit more slowly; touching the tip, it is pushed. At 4 m/s the window is 0.27–0.33 m; at 1 m/s it is 0.02–0.58 m.
- **Exit speed, Pool ball (0.4 kg, restitution 0.9).** Contact restitution 0.7 × 0.9 = 0.63. Ball speed at separation = (1 + 0.63) × 0.5 ÷ (0.5 + 0.4) × 3 = 0.906 × 3 = 2.72 m/s; the stick drops to (0.5 − 0.63 × 0.4) ÷ 0.9 × 3 = 0.83 m/s and the drive brings it back up. Each further touch adds 0.906 of the closing speed, which is under 1, so the ball can never leave faster than the stick: at most `speed`.
- **Slide, then roll.** The ball leaves sliding. Bed friction √(0.2 × 0.3) = 0.245 slows it at 2.4 m/s² until it rolls at 5/7 of its separation speed: (5/7) × 2.72 = 1.94 m/s, reached after 0.32 s and about 0.75 m. Row A1 reads the ball 0.8 m after the strike, so it reads the rolling speed, 1.94 m/s (0.5 × 0.4 × 1.94² = 0.75 J of translation; 1.5 J at separation).
- **Bounded work.** Per tick the drive can commit at most 120 W ÷ 120 Hz = 1 J; over a whole stroke at most 30 N × 0.6 m = 18 J. A default strike takes about 0.5 × 0.5 × 3² = 2.25 J to bring the stick to speed and about 0.25 × (3² − 0.83²) = 2.1 J to bring it back up after the hit, about 4.3 J in all. Both bounds are properties of the declared drive, not of this part.
- **Supply.** Under `ENGINE-SUPPLY-DEMAND` the source is debited the drive's committed work each tick and an exhausted source opens the route. A source holding 1 J cannot bring the stick to 3 m/s (2.25 J): the drive stops and the stick self-locks short of its stroke.
- **Heavy cargo.** A Bowling ball (4 kg, restitution 0.14): at separation (1 + 0.098) × 0.5 ÷ 4.5 × 3 = 0.37 m/s; the drive then pushes it with up to 30 N (7.5 m/s²) over the remaining stroke of at most 0.45 m, to at most √(0.37² + 2 × 7.5 × 0.45) = 2.6 m/s. It moves by the same contact; because it is pushed rather than struck it may end faster than a struck Pool ball rolls, so no speed comparison is asserted.
- **Miss.** The tip is 0.06 m wide and a Pool ball's radius is 0.20 m, so a ball whose centre is more than 0.23 m off the stick's axis is not touched. The stroke completes and holds; nothing else happens.
- **Tunnelling.** At 4 m/s the stick advances 0.033 m per tick against a 0.40 m ball; excluded by the engine's general contact, with no per-part check.

## 3. Engine capabilities

Map row families: ContactImpulse, EnvironmentState, FiniteLedger, GeometryQuery, JointConstraint, RigidBodyDynamics, SlidingFriction. Every test below was opened for this revision.

| Capability | Status | Evidence |
| --- | --- | --- |
| Supplied directional prismatic drive with separate command inputs; holds without work when a command or power is absent | present | `MechanicalContractValues.cs:84`–`:98`; `LinearPusherTests.IndependentElectricalInputsHoldWithoutDeliveringWork` (`CuriousContraptions.tests/LinearPusherTests.cs:38`) |
| Per-tick drive work bounded by the declared power | present | `LinearPusherTests.ExtensionReportsMotorWorkAndEndpointPowerOnTheNextElectricalTick` (`:52`, asserts tick work ≤ force × speed ÷ 120) |
| A driven head moves cargo it touches and leaves cargo across a lateral gap unmoved, in any orientation | present | `LinearPusherTests.RotatedSphericalHeadPushesCargoAndLateralClearanceMisses` (`:307`) |
| Effort bound decides whether a load moves | present | `LinearPusherTests.VerticalPackageLiftsCargoOnlyWithSufficientDeclaredForce` (`:322`) |
| Save, load, Reset and replay of a driven slider with cargo | present | `LinearPusherTests.CargoTransferResetRestoresEveryAuthoredBodyAndSemanticReplay` (`:333`) |
| A driven slider body striking a resting ball at 3–4 m/s with non-zero restitution | present, untested | the pusher's head declares restitution 0 and its tests push from contact; the contact law is general, but no test strikes at speed |
| Drive work debited from the source; an exhausted source stops the drive | missing | `ENGINE-SUPPLY-DEMAND` (sprint key `engine-supply-drive-demand`), part of the engine programme and ahead of every element; row A11 depends on it |

The power-bound trial that the prismatic drive runs each tick is whole-world today; `ENGINE-ISLANDS-SLEEP` confines drive-budget trials to the island they affect. This part declares one ordinary driven slider and designs around nothing.

## 4. Sources and legacy

- Requirement: "Contact actuator delivers bounded work to an actual ball"; outcome "A miss cannot move its target" ([element-097](../requirements.md#element-097)).
- A search at `a6c914e` for "cue", "pool" and "billiard" found nothing. Pusher and actuator facts apply.

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| L1 | Linear pusher: stroke 2.4, speed 1.5, acceleration 12, force 40; "pushes with limited force and stops at obstacles" | `parts/catalog/linear_pusher.tres@a6c914e:L9-L12` | carry forward the bounded-force rule; the stroke is shortened and the speed raised for a cue |
| L2 | Actuator positive work is bounded by available work | `engine/physics/PoweredImpulse.cs@a6c914e:L7-L26` | carry forward the rule as the drive's power bound and the source debit; do not carry forward the CPU code |
| L3 | Charging accepts min(capacity − energy, power × dt) only while supplied | `engine/physics/PhysicsEnergyStore.cs@a6c914e:L38-L46` | do not carry forward: this design has no store of its own; the drive is paid directly from the supplying source |

**Files harvested:** `parts/catalog/linear_pusher.tres`, `engine/physics/PoweredImpulse.cs`, `engine/physics/PhysicsEnergyStore.cs`.

## 5. Acceptance outline

Requirement row: [element-097](../requirements.md#element-097). Planned test class `PoolCueTests`. Unless stated, a Battery is on `PowerIn`, a switched supply on `StrikeIn`, `speed` is 3 m/s and a Pool ball stands on a Wall bed with its near surface 0.3 m ahead of the tip; `q` is the committed slider coordinate. Until EL-098 is published the rows use a Tennis ball set to radius 0.2 m and mass 0.4 kg (its bounce and friction differ, so A1's lower bound is recomputed for it).

| # | Given / When / Then | Planned test | Control | Boundary and tolerance |
| --- | --- | --- | --- | --- |
| A1 | When the command is supplied, then the tip touches the ball and the ball leaves along +X at 1.6–3.15 m/s | `AnAimedStrikeSendsTheBallAwayNoFasterThanTheStick` | with no command the ball moves less than 1 cm in 3 s | speed read when the ball is 0.5 m past the full-stroke tip, by when it rolls. Predicted 1.94 m/s. Lower bound 0.85 × (5/7) × 0.906 × `speed` = 1.65, the rolling speed after the slide less 15 %; upper bound 1.05 × `speed`, since the ball cannot outrun the stick; 3 s stall guard |
| A2 | Given the ball's centre 0.30 m to the side of the stick's axis (0.07 m clear of the tip), when the command is supplied, then the stroke completes and the ball moves less than 1 cm in 3 s | `AMissCompletesTheStrokeAndLeavesTheBallWhereItIs` | A1 | q reaches at least 0.58 m; at 0.20 m to the side (inside the 0.23 m reach) the ball does move |
| A3 | Given each combination of power and command present or absent, then the stick moves only with both, and the drive reports no work otherwise | `TheStickMovesOnlyWithPowerAndACommand` (theory, four rows) | the row with both | unmoved means q stays under 0.005 m for 3 s |
| A4 | During A1, then no tick commits more than 1 J of drive work, the total over the stroke is at most 18 J, and the ball's kinetic energy at separation is at most 2 J | `TheWorkGivenIsBoundedByTheDeclaredDrive` | — | 1 J = 120 W ÷ 120 Hz with 1 % slack; 2 J = 0.5 × 0.4 × 3.15² |
| A5 | Given `speed` 1 and 4 m/s, then the ball leaves at least twice as fast at 4 as at 1 | `TheSpeedSettingChangesTheShot` | — | predicted ratio 4 (both readings are rolling speeds, so the 5/7 factor cancels); ball placed 0.3 m ahead in both |
| A6 | Given a Bowling ball in place of the Pool ball, then it moves at least 0.5 m and the drive's total work stays within 18 J | `AHeavyBallIsMovedByTheSameContact` | A1 | the claim is that the same contact moves any body; no speed comparison (section 2, Heavy cargo) |
| A7 | Given a Wall 0.3 m ahead of the tip, when commanded, then the drive reports Blocked and the stick does not pass the Wall | `AWallStopsTheStick` | with the Wall removed the stroke completes | overlap of stick and Wall at most 0.02 m |
| A8 | Given `speed` 0.5 or 4.5 in an edit or a save, then it is refused with the typed reason and nothing changes | `ASpeedOutsideItsRangeIsRefused` | 1 and 4 are admitted (inclusive) | — |
| A9 | Given a Signal output offered to `StrikeIn`, then the connection is refused | `ASignalOutputCannotCommandTheCue` | an electrical `Supply` output is accepted | typed refusal; covered generically by S4 when it lands |
| A10 | Given Run, Reset, save and load, then q returns to 0, the ball to its place, the wires and `speed` survive, and a second Run repeats A1 | `ResetSaveAndLoadRestoreTheCueAndTheShotRepeats` | — | generic row: `CatalogueIntegrationTests.EveryAdmittedDeclarationRetainsAuthoredStateAcrossRunResetSaveLoad` |
| A11 | During A1 with a finite Battery, then the Battery's debit is never below the drive's committed work and above it by no more than the ledger residual; and given a Battery holding 1 J, then the drive stops when the source is exhausted, the stick self-locks short of full stroke and the ball is not struck at speed | `TheSourcePaysForTheStrokeAndAnEmptySourceStopsIt` | A1's full Battery completes the stroke | q stays under 0.3 m with the 1 J source (2.25 J is needed to reach 3 m/s); Reset restores the authored charge |

A1 and A4 are "delivers bounded work to an actual ball"; A2 is "a miss cannot move its target"; A11 is the same bound seen from the source.

### Integration

- Supplied by CAT-005 Battery; commanded by Impact switch, State latch, Pressure plate, Counter or Hold timer `Supply` outputs (all delivered). Strikes any ball: EL-098 Pool ball (planned first), Tennis ball, Basketball, Bowling ball, EL-091 Cannonball.
- Campaign: the coverage ledger has no pool-table row; [todo-227](../requirements.md#todo-227) keeps "the original … campaign reservations" without naming levels (`docs/planning/requirements.md:6391`). The nearest row is the launcher row "Pusher, boxing glove, flipper, wound launcher …" with first use in levels 41–50 (`:2401`). Owner decision 24c gives it new level **44a** *On Cue* (chapter 5): introduction 44a, practice 49, reuse 141 ([campaign coverage additions](../requirements.md#campaign-coverage-additions); brief in [campaign coverage](readiness/campaign-coverage.md#4-lesson-briefs)).
- A level that fixes a cue in place needs the lesson-fixture story S8.

### Delivery

- **Shape.** Single element, after EL-098 and EL-099 (both taught at 8; the cue at 44a, owner decision 24a), so its tests strike the real Pool ball.
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a, 22 November 2026), which precedes every further element: of it `ENGINE-SUPPLY-DEMAND` gives row A11 and `ENGINE-ISLANDS-SLEEP` confines the drive's power-bound trial; then S7. EL-098 preferred, not required. S3 and S4 optional.
- **Form.** Port roles, node arity, target masks and mouth identities are written in the form `ENGINE-CAPABILITY-INSTANCES` leaves; the names here are today's (`FirstIn`, `SecondIn` and the two-consumer command shape of the supply graph).
- **Files.** `elements/PoolCue/` (project, declaration and `.uid`, `pool_cue.tscn`, `icon.svg` and its import file); `CuriousContraptions.tests/PoolCueTests.cs`; one `CuriousContraptions.slnx` line; until S3, the hand lists a new package still edits; the ledger row; the binding section once S7 admits it.
- **Review tier.** Engine tier, because of the one *present, untested* row (a strike at speed); data-only if the probe finds an exercising test.
- **Disqualifiers the probe re-checks.** A driven slider whose contact with a resting ball ignores restitution; a supply graph that needs the endpoint contacts to compile; a 1.6 m by 0.06 m moving box that the slider refuses; a drive whose `MaxPower` must be a product of two parameters rather than a fixed value; a visible-solid check that cannot classify three meshes against one collider.

## 6. Open questions

All resolved.

1. **Energy source:** a supplied drive, not a charged store or a spring. Reason: it is the simplest capability that is present and tested, its effort and power bounds are the "bounded work", and a store would add a charge wait with no lesson. Decided under the standing delegation.
2. **Aim:** along the placed pose only; the player turns the part (D19).
3. **Separate strike and return commands**, both electrical, as the pusher; no single pulse input, because a signal-commanded stroke does not exist for a drive and is not needed.
4. **One parameter, `speed`** (D13); stroke, effort, acceleration and power are fixed starting values (D1).
5. **No end-position outputs** (D19).
6. **Sockets flush; indicator solid; colour bands as separate meshes** (visible = solid; no package has a texture).

Needs the owner: none.

## Engine conformance (22 November 2026)

1. **Declaration data only.** A static rest, one slider body with a declared directional drive, a supply graph, three typed ports, one parameter and presentation bindings. No solver branch, callback or engine test on this package.
2. **Generic laws.** The prismatic drive law enabled by the supply-network rule and paid from the source; contact with restitution and friction for the strike; the same contact for a blocked stick. "A miss" is geometry: no contact, no impulse.
3. **Three layers.** Colliders: five hidden `col_` primitives (three static boxes and one static sphere; one box on the stick). Visuals: one mesh per static collider; three end-to-end meshes over the stick's one collider; sockets flush. Animation: the indicator's colour from the committed prismatic phase; it reads committed state and pushes nothing. `Visual/Stick` follows body 1's pose.
4. **Flow and typing.** Three typed Electrical inputs; `PrismaticTargetSelection` and `PrismaticInactiveResponse` are enums; one f32 parameter.
5. **Flat state.** Bodies 2 (1 static, 1 dynamic); colliders 5 (boxes 4, sphere 1); joints 1 prismatic; drives 1; sensors 0; domain nodes: 2 electrical and 4 supply-graph nodes; stores 0; presentation bindings 4 (one body binding, three colour channels of one indicator).
6. **Performance.** Five primitives, about the pusher's count; four are static. Nothing sleeps today; a self-locked stick will sleep with its island when `ENGINE-ISLANDS-SLEEP` lands, and that story also confines the drive's power-bound trial. Nothing is cosmetic-only in physics.
7. **Tests are scenarios.** Every row runs the production simulation with delivered supplies and balls.

**Verdict: conforms** (the audit's fixes of 22 November 2026 are applied: slide-to-roll in the tolerance band, hidden collider proxies, the drain as a real row).

## 7. Coverage binding (planned, not yet live)

Published main binds a named-identity package through a final section of the identity's spec headed "Package source binding", read by `tools/Coverage/NamedPackageSources.cs` (pattern: EL-239 Baseball). The section is not written here: the reader parses every spec that carries that heading and fails the coverage run when a field does not resolve. **Blocked on the coverage follow-on story S7.**

| Field | Planned value |
| --- | --- |
| Package | `pool_cue` |
| Named identity | EL-097, `../invest/named-elements.md#element-097` |
| Acceptance | `todo-227`, `sequence-task-650` |
| Decision | S667, `../invest/scope-corrections.md#s667` |
| Mode records | `Configuration=Fixed` |

Its sequence task exists: `sequence-task-650` stands beside the `element-097` anchor (`docs/planning/requirements.md:5171`–`:5173`). What the reader lacks today: `scope-corrections.md` has no `s667` block; and the reader wants the todo and the task adjacent in one row, while `todo-227` is a scope index shared by thirteen identities (`:6389`) apart from the task.
