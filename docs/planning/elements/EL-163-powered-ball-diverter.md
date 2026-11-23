# EL-163 · Powered ball diverter named-identity spec

**Current delivery (24 October 2026):** [EL-163 delivery slice](../invest/named-elements.md#element-163). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Story 7.0 named-identity spec (Batch J). Baseline commit `a6c914e`; every `path@a6c914e:Lstart-Lend` citation resolves with `git show a6c914e:<path> | sed -n 'start,endp'`. All values are canonical IEEE-754 f32 game values inside the [game-grade envelope](../../gpu-f32-physics.md#game-grade-envelope).

**Build-ready revision, 21 November 2026 (checkout `8d1d68aa`).** Sections 1–3, 5 and 6 were rewritten against current code; a `path:line` citation without a commit is in this checkout. Section 4 is the unchanged harvest. Values marked **starting value** are adopted under decision D1 of [delivery batches](../invest/delivery-batches.md#3-family-level-decisions).

## 1. Identity

| Field | Value |
| --- | --- |
| ID | EL-163 |
| Name | Powered ball diverter |
| Type | Mechanical |
| Anchor | [requirements.md#element-163](../requirements.md#element-163); [named entry](../invest/named-elements.md#element-163); scope index [todo-246](../requirements.md#todo-246) |
| Proof owner | S374 |
| Disposition | To deliver as a single element, second of the diverter run, after [EL-162](EL-162-fixed-ball-diverter.md) and before [EL-164](EL-164-alternating-ball-diverter.md). Not a duplicate of EL-162 (it adds a moving body, three ports and a drive) and not a Family member (the difference is behaviour, not declared values). |
| Planned package | id `diverter_powered`, directory `elements/DiverterPowered/`, class `DiverterPoweredElementDeclaration` |
| Refines / extends | No CAT spec. Junction frame of EL-162; drive pattern of [CAT-039 linear pusher](CAT-039-linear_pusher.md); supply from [CAT-005 battery](CAT-005-battery.md) |
| Status | Not started; blocked on the engine programme (owner decision 3a), relying most on ENGINE-SUPPLY-DEMAND (the supplied directional drive) and ENGINE-ISLANDS-SLEEP; then EL-162; then S7. |

## 2. Declaration

Follow the Linear pusher (`elements/LinearPusher/LinearPusherElementDeclaration.cs:8`): the package implements `ISliderElement` (`CuriousContraptions.Abstractions.Mechanical/MechanicalContracts.cs:15`), `ISupplyElement` (`CuriousContraptions.Abstractions.Electrical/ElectricalContracts.cs:11`) and `ISceneGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`). `KinematicMode.Static` base, `SupportedDomains = Mechanical | Electrical`. Footprint: 2 bodies, 1 moving, 1 prismatic constraint.

### Bodies and colliders

- **Base (static).** The junction frame of [EL-162 section 2](EL-162-fixed-ball-diverter.md#shared-junction-frame-also-used-by-el-163-and-el-164): one 1.2 m inlet lane opening over a 0.6 m delta into lane A (+X) and lane B (−X), plus a bridge box 2.6 × 0.08 × 0.2 centred (0, 0.94, 0.3), spanning the delta with its underside at y = 0.9 (0.4 m above the walls, clear of every ball); two bridge posts 0.08 × 1.0 × 0.08 centred (±1.26, 0.40, 0.3), standing beside the floor from its underside to the bridge; and a drive housing box 0.5 × 0.3 × 0.4 centred (0, 1.13, 0.3) on the bridge.
- **Wedge (dynamic, on the slider).** The EL-162 wedge: two boxes 0.85 × 0.5 × 0.06 yawed ±45° meeting at an upstream apex, with the apex cap box. Mass 0.5 kg (**starting value**: the pusher head's mass, `LinearPusherElementDeclaration.cs:33`); principal moments of a 1.2 × 0.5 × 0.6 box of that mass; gravity (0, −9.81, 0). Material: restitution 0.1, friction 0.3, rolling resistance 0, bounce threshold 0.1 m/s, as the EL-162 wedge.

### Slider and drive

One `PrismaticSliderDescriptor` (`CuriousContraptions.Abstractions/MechanicalContractValues.cs:104`) along the part's +X: lower limit 0 (apex at x = −0.6, lane A open), upper limit 1.2 (apex at x = +0.6, lane B open); no spring, no damping; `BodiesCollide` false. Its drive (`PrismaticDriveDescriptor`, `MechanicalContractValues.cs:89`) is the pusher's shape (`LinearPusherElementDeclaration.cs:34`–`:38`):

| Field | Value | Reason |
| --- | --- | --- |
| Target | 1.2 m | full stroke |
| MaxSpeed | 3 m/s | with the profile below the stroke takes about 0.5 s: quicker than balls 1.5 s apart, slow enough to see |
| StoppingAcceleration | 12 m/s² | the pusher's default |
| MaxEffort | 40 N | a 1 kg ball pinned against a wall stalls it; it cannot crush through |
| MaxPower | 120 W | effort × speed |
| Selection | `PrismaticTargetSelection.Directional` | separate commands for each end |
| Inactive | `PrismaticInactiveResponse.SelfLock` | no command, both commands or lost power hold the wedge where it is |
| Positive / Negative | supply nodes `positive`, `negative` | |

All drive values are fixed, **starting values**; the part has no parameters (section 6).

### Ports

| `PortId` | Domain | Direction | Role | Glyph | Local position |
| --- | --- | --- | --- | --- | --- |
| `PowerIn` | Electrical | Input | `PowerIn` | Power | housing front face, (−0.25, 1.13, 0.1), normal −Z |
| `SelectAIn` | Electrical | Input | `FirstIn` | First | housing front face, (0, 1.13, 0.1), normal −Z |
| `SelectBIn` | Electrical | Input | `SecondIn` | Second | housing front face, (0.25, 1.13, 0.1), normal −Z |

The commands are electrical inputs, exactly as the pusher's `ExtendIn` and `RetractIn` (`LinearPusherElementDeclaration.cs:45`–`:46`): a player wires a battery to `PowerIn` and a switched supply (Impact switch, State latch, Pressure plate, Counter or Hold timer `Supply`) to a select input. The earlier draft's Activation-domain inputs are dropped (section 6). Supply graph: the pusher's (`LinearPusherElementDeclaration.cs:52`–`:66`) without its two endpoint contacts and outputs. Electrical nodes, the pusher's (`LinearPusherElementDeclaration.cs:51`): `motor`, `ElectricalRole.Consumer`, 12 V, 24 Ω, 0 F, `MaxPower` 400 W; and `distribution`, `ElectricalRole.LogicGate`, 12 V, 0.05 Ω, 0 F, 400 W.

### Metadata and bindings

Category `Motion`, display name "Powered diverter", palette order 115 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)), colour (0.76, 0.56, 0.32). Presentation: body binding `Visual/Wedge` → `PackageBodyIndex(1)`; indicator `Visual/Indicator` on `PresentationDriver.CommittedPrismaticPhase` with state values (held slate `#556573`, moving gold `#f7cb52`, blocked amber `#e8b764`), the pusher's binding in form (`LinearPusherElementDeclaration.cs:72`–`:76`); the three colours are this part's declared values.

### Art and solidity

| Node | Mesh | Class |
| --- | --- | --- |
| Junction floor, walls, divider | as EL-162, Ramp colour `#c28f52` | solid, one collider each |
| `Visual/Wedge` | gold arms and navy cap | solid, colliders on the moving body |
| `Visual/Bridge`, `Visual/Posts`, `Visual/Housing` | navy `#293954` boxes | solid, one collider each |
| `Visual/Indicator` | sphere radius 0.06 on the housing | solid, sphere collider (as the pusher's indicator) |
| `Visual/Sockets` | three socket spheres | solid, sphere colliders |
| `Visual/Rod` | cream rod from housing to wedge, stretched by a span binding | decorative: moved by a binding |
| `Visual/LaneMarks` | one and two printed stripes | decorative: printed flat |

Icon: the EL-162 Y with a small motor block and a double-headed arrow over the chevron. It differs from EL-162 by the bridge, housing and sockets, and from EL-164 by having sockets for commands and no exit hoops.

### Behaviour and numbers

The drive moves the wedge toward the commanded end while that command and `PowerIn` are both supplied, within its speed, effort and power; otherwise the wedge is self-locked. Supply reaches the drive in the electrical phase and the wedge moves in the physics step of the same tick ([solver model](../../gpu-f32-physics.md#solver-model)). A stroke takes about 0.5 s, so a ball already past the apex keeps its lane: switching is never instant. A ball pinned between the wedge and a wall needs more than 40 N to move, so the drive reports Blocked and resumes when the ball leaves. Motor electrical drain is deferred with the Motor's and has its own owner; this part's supply is on or off.

## 3. Engine capabilities

Map row families: ContactImpulse, EnvironmentState, FiniteLedger, FiniteWorkActuation, GeometryQuery, JointConstraint, RigidBodyDynamics, SignalPropagation, StateTransaction.

| Capability | Status | Evidence |
| --- | --- | --- |
| Prismatic slider with limits on a static base | present | `MechanicalContractValues.cs:104`; `SliderInstanceTests.ThePlateHoldsItsLowerAndUpperStops` (`CuriousContraptions.tests/SliderInstanceTests.cs:61`) |
| Supplied directional drive with separate command inputs | present | `MechanicalContractValues.cs:84`–`:98`; `LinearPusherTests.IndependentElectricalInputsHoldWithoutDeliveringWork` (`CuriousContraptions.tests/LinearPusherTests.cs:38`) |
| Self-lock on lost power, resume from the held position | present | `LinearPusherTests.LosingPowerLocksTheIntermediatePositionAndRetractResumesFromIt` (`LinearPusherTests.cs:104`) |
| Blocked phase against an obstruction | present | `LinearPusherTests.ObstructionStopsTheHeadAndReportsBlockedWhileAnAdjacentWallDoesNot` (`LinearPusherTests.cs:125`) |
| One source feeding power and a command | present | `LinearPusherTests.OneSourceCanSupplyBothPowerAndCommandWithAnUnwiredCommandControl` (`LinearPusherTests.cs:249`) |
| Phase-driven indicator binding | present | `LinearPusherTests.PackageInstallationRoundTripsItsDriveExtrusionAndPhaseBinding` (`LinearPusherTests.cs:17`) |
| A slider's moving body carrying **two yawed box colliders** | present, untested | multi-collider moving bodies are admitted (`SliderBodyDescriptor`, `MechanicalContractValues.cs:75`–`:77`), but the delivered slider bodies (gate blade, pusher head, springboard plate) each carry one |

Nothing is missing. The revolute alternatives the earlier draft assumed (a hinge servo with Hold/Lower/Upper, a position drive) do not exist and belong to ENGINE-JOINT-KINDS (revolute position drive with rest window); this design does not use them.

## 4. Sources and legacy

- Requirement: "Finite-work actuator changes a routing blade after a command"; outcome "Missing supply or a jam prevents instant route switching" ([element-163](../requirements.md#element-163)).
- Integration under [todo-246](../requirements.md#todo-246): powered mode is added and proved after fixed selection.

| # | Fact | Citation | Disposition | Reason |
| --- | --- | --- | --- | --- |
| 1 | Servo modes Hold, Lower, Upper | `engine/physics/PhysicsServo.cs@a6c914e:L9-L9` | carry forward (closed set) | Commanded end-stop motion. |
| 2 | Accepted: ramped command speed (max 2, acceleration 4 in the fixture), reversal and Hold locking the joint at its current coordinate with no further work; replay identical | `CuriousContraptions.tests/PhysicsServoTests.cs@a6c914e:L20-L67` | carry forward (behaviour) | Finite, rate-limited blade motion. |
| 3 | Accepted: supply is finite; blade kinetic energy ≤ supplied work; a direct motor command cannot bypass the servo | `CuriousContraptions.tests/PhysicsServoTests.cs@a6c914e:L69-L90` | carry forward | No free switching energy. |
| 4 | Gate states Closed, Opening, Open, Closing, Blocked; Blocked when speed < 1e-5 between end stops | `engine/SlidingBlade.cs@a6c914e:L7-L7`, `engine/SlidingBlade.cs@a6c914e:L49-L53` | carry forward (states), do not carry forward (1e-5 literal) | Jam is observable state; tolerance is game-grade. |
| 5 | Powered controller drives toward the stroke at min(max speed, √(2·a·distance)), effort 60 N, power 120 W, per-step `Prepare` | `engine/SlidingBlade.cs@a6c914e:L36-L47` | carry forward (profile), do not carry forward (per-step loop) | Per-element update loops are banned. |
| 6 | Accepted: a closing blade meeting moving cargo stops on it; Reset replays | `CuriousContraptions.tests/PoweredGateTests.cs@a6c914e:L179-L241` | carry forward | Jam behaviour. |

No legacy diverter exists. Rows 1–3 describe the behaviour to keep (hold, ramped motion, no free energy); the current mechanism that delivers it is the prismatic directional drive, not a hinge servo.

**Files harvested:**
- `engine/physics/PhysicsServo.cs`
- `engine/SlidingBlade.cs`
- `parts/PoweredGatePart.cs` (blocked indicator)
- `parts/ImpactLeverPart.cs` (hinge pattern)
- `parts/LatchPart.cs` (typed command sockets)
- `CuriousContraptions.tests/PhysicsServoTests.cs`
- `CuriousContraptions.tests/PoweredGateTests.cs`
- Checked, no extra diverter knowledge: `engine/SceneServoDeclaration.cs` (servo record: joint, maximum speed, acceleration, effort, power).
- Searched with no hit for `diverter`: `parts/`, `engine/` (including `engine/physics/` and `engine/bridge/`), `CuriousContraptions.tests/`, `content/puzzles.json`, `tools/`, `reference/`, `diagnostics/`.

## 5. Acceptance outline

Requirement row: [element-163](../requirements.md#element-163). Planned test class `DiverterPoweredTests`. Lane membership is defined as in EL-162; `q` is the committed slider coordinate.

| # | Given / When / Then | Planned test | Control | Boundary and tolerance |
| --- | --- | --- | --- | --- |
| A1 | Given a battery on `PowerIn` and a switched supply on `SelectBIn`, when the switch closes and a second ball follows 2 s later, then the first ball took lane A and the second takes lane B | `ACommandWithSupplyMovesTheWedgeAndTheNextBallTakesTheOtherLane` | with no command the second ball also takes lane A | q reaches at least 1.15 m within a 3 s guard |
| A2 | Given each combination of power and command present or absent, then the wedge moves only with both | `TheWedgeMovesOnlyWithPowerAndACommand` (theory, four rows) | the row with both | unmoved means q stays below 0.01 m for 3 s |
| A3 | Given a command, then 0.1 s later the wedge has not crossed the centre, and a ball that passed the apex before the command keeps lane A | `SwitchingTakesTimeAndABallAlreadyPastKeepsItsLane` | A1 | q(0.1 s) < 0.6 m, which the 3 m/s limit implies with margin |
| A4 | Given a Basketball pinned between wedge and wall, when commanded, then the drive reports Blocked, the ball is not penetrated, and when the ball leaves the stroke completes | `APinnedBallBlocksTheWedgeUntilItLeaves` | an adjacent wall alone does not block | overlap of ball and wedge at most 0.02 m |
| A5 | Given power removed mid-stroke, then the wedge holds its position against a ball rolling into it | `LosingPowerLocksTheWedgeWhereItIs` | — | drift under 0.02 m over 3 s |
| A6 | Given both commands supplied, then the wedge holds | `BothCommandsTogetherHold` | either command alone moves it | — |
| A7 | Given a Signal output offered to `SelectBIn`, then the connection is refused | `ASignalOutputCannotCommandTheDiverter` | an electrical `Supply` output is accepted | typed refusal; S4 row 5 covers it generically when it lands |
| A8 | Reset returns q to 0 and clears the drive; save and load keep all three connections | `ResetAndSaveLoadRestoreTheWedgeAndItsWires` | — | generic rows: `CatalogueIntegrationTests.EveryAdmittedDeclarationRetainsAuthoredStateAcrossRunResetSaveLoad`, S4 rows 1 and 3 |

No parameters, so no parameter refusal applies.

### Integration

- Commanded by CAT-063 Impact switch, CAT-037 State latch, CAT-052 Pressure plate, CAT-020 Counter or CAT-033 Hold timer through their `Supply` outputs; supplied by CAT-005 Battery. All delivered.
- Campaign: chapter 3 "Pipe Dreams", levels 21–30, "Y-diverter modes" and the closing "feeder/timed-diverter machine" (`docs/planning/requirements.md:2288`); reuse 41–50, 111–120, 136–150 (`:2398`).
- Coverage binding: a final package-source-binding section in this spec, as EL-239 on published main; blocked on S7 (section 7).

### Delivery

- **Shape.** Single element slice after EL-162. Delivery batches does not classify it; it belongs in 1b "Supplied sliders".
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), which includes ENGINE-SUPPLY-DEMAND (the drive's work debited from its source) and ENGINE-ISLANDS-SLEEP; then EL-162 (todo-246 teaches fixed selection first and this slice reuses its scene frame); then S7 (the coverage follow-on, which widens the reader, owner decision 15). S2–S4 optional.
- **Files.** `elements/DiverterPowered/` (project, declaration, `diverter_powered.tscn`, `icon.svg`); `CuriousContraptions.tests/DiverterPoweredTests.cs` (compiled by the test project's one `*.cs` rule); one `CuriousContraptions.slnx` line; the binding section of section 7 once S7 admits it; until S3, the trim list and placement switch.
- **Review tier.** Engine tier because of the one *present, untested* row; data-only if the probe finds an exercising test for a multi-collider slider body.
- **Disqualifiers the probe re-checks.** A slider body that admits only one collider; a self-locked wedge that a rolling ball can push; a supply graph that needs the endpoint contacts to compile.

## 6. Open questions

All resolved.

1. **Two select inputs, not a toggle**, and they are electrical. Reason: the delivered directional drive takes two electrical command nodes, every delivered sensor and memory part offers a switched `Supply`, and a signal-commanded position drive does not exist. Decided under the standing delegation.
2. **An unsupplied wedge holds its pose** (self-lock), as the pusher does; no return spring.
3. **`initial_branch` dropped** (D13, D19): the wedge starts at lane A. A level that needs the other start sends a command first.
4. **No end-position outputs** (D19): no requirement or lesson uses them; they can be added from the pusher's pattern when one does.
5. **Drive values**: starting values under D1.
6. **Bridge, posts, housing, ports and electrical node values written out** (22 November 2026, audit M11); starting values under D1.

Needs the owner: none.

## Engine conformance (22 November 2026)

Checked against the owner addendum of 22 November 2026 (AGENTS.md element-free engine gate and Tri-Graph rule; `_bmad/custom/element-free-engine-design.md`; `docs/gpu-f32-physics.md`).

1. **Declaration data only.** A static frame, one slider body with a declared directional drive, a supply graph, three typed ports and presentation bindings. No solver branch, callback or engine test on this package.
2. **Generic laws.** The prismatic drive law (target, speed, stopping acceleration, effort and power bounds, self-lock when inactive) enabled by the supply-network rule, exactly as the Linear pusher declares it; contact and friction for routing and for a pinned ball stalling the drive.
3. **Three layers.** Colliders: twenty primitives. Static body: nine frame boxes as EL-162, bridge, two posts, housing, an indicator sphere and three socket spheres. Wedge body: two arm boxes and the cap box. Visuals: one mesh per collider; lane marks printed flat. Animation: `Visual/Indicator` recoloured from the committed prismatic phase and `Visual/Rod` stretched by a span binding; both read committed state and push nothing. `Visual/Wedge` follows body 1's pose.
4. **Flow and typing.** Three typed Electrical inputs with roles `PowerIn`, `FirstIn`, `SecondIn`; closed sets (`PrismaticTargetSelection`, `PrismaticInactiveResponse`) are enums; no parameters.
5. **Flat state.** Bodies 2 (1 static, 1 dynamic); colliders 20 (boxes 16, spheres 4); joints 1 prismatic; drives 1; sensors 0; domain nodes: 2 electrical and the pusher's supply-graph nodes less its two endpoint contacts; stores 0; presentation bindings 3 (body, phase colour, span).
6. **Performance.** Twenty colliders against the Linear pusher's handful. Reason: the junction itself is nine visible walls and floors, and the bridge, posts and housing are visible solids. Seventeen are static. No island sleeping exists today; a resting self-locked wedge costs nothing only once ENGINE-ISLANDS-SLEEP (owner decision 3a) is delivered, ahead of this part. The rod has no collider or joint.
7. **Tests are scenarios.** The acceptance rows run the production simulation with delivered supplies; none needs a code path of this part.

**Verdict: conforms.**

## 7. Coverage binding (planned, not yet live)

Published main binds a named-identity package through a final section of the identity's spec headed "Package source binding", read by `tools/Coverage/NamedPackageSources.cs` (pattern: EL-239 Baseball on published main). The section is not written here: the reader parses every spec that carries that heading and fails the whole coverage run when a field does not resolve. **Blocked on the coverage follow-on story S7.** S7 widens the reader (owner decision 15), including for companion packages and TH, RAD and GAP identities.

| Field | Planned value |
| --- | --- |
| Package | `diverter_powered` |
| Named identity | EL-163, `../invest/named-elements.md#element-163` |
| Acceptance | `todo-246`; no sequence task yet |
| Decision | S374, `../invest/scope-corrections.md#s374` |
| Mode records | `Configuration=Fixed` |

What it lacks today: `scope-corrections.md` has no `s374` block on published main; `todo-246` is a scope index shared by EL-162, EL-163 and EL-164 with no sequence-task anchor directly after it, which the reader requires; and one task source can bind only one package.
