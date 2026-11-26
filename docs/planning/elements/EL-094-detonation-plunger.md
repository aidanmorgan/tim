# EL-094 · Detonation plunger — named-identity spec

**Current delivery (24 October 2026):** [EL-094 delivery slice](../invest/named-elements.md#element-094). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

**Build-ready revision, 22 November 2026 (checkout `8d1d68aa`).** Every section is written against current code; a `path:line` citation without a commit is in this checkout, and `path@a6c914e:Lx-Ly` resolves with `git show a6c914e:<path>`. Values marked **starting value** are adopted under decision D1 of [delivery batches](../invest/delivery-batches.md#3-family-level-decisions). All game values are canonical f32. No number here was simulated; each is a hand calculation shown beside it.

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name / type | EL-094 · Detonation plunger · Specialist |
| Anchor | [requirements.md#element-094](../requirements.md#element-094); [named entry](../invest/named-elements.md#element-094); scope index [todo-197](../requirements.md#todo-197) |
| Proof owner | S664 |
| Disposition | To deliver as a single element. Not a duplicate of the Impact switch (CAT-063) or the Pressure plate (CAT-052): it fires on a completed stroke of a sprung handle, not on approach speed or resting mass, and it passes no electrical supply. The ledger's retained CAT-063 and CAT-023 acceptance is the shared "one command after actual motion" rule, kept in rows A1–A5. |
| Planned package | id `plunger`, directory `elements/Plunger/`, class `PlungerElementDeclaration` |
| Refines / extends | Sprung slider of the Springboard (CAT-062); activation output of [CAT-063 Impact switch](CAT-063-switch.md) |
| Status | not started; blocked on the engine programme (owner decision 3a), then ENGINE-SIGNAL-NODES (threshold over a committed scalar), then S7 |

## 2. Declaration

The package implements `ISliderElement` (`CuriousContraptions.Abstractions.Mechanical/MechanicalContracts.cs:15`), `ISignalElement` (`CuriousContraptions.Abstractions.Signal/SignalContracts.cs:8`) and `ISceneGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`). `KinematicMode.Static` base, `SupportedDomains = Mechanical | Signal`. Footprint: 2 bodies, 1 moving, 1 prismatic constraint. The plunger declares no store, no supply graph and no electrical node: it owns no energy.

### Bodies and colliders

Part origin on the bench under the box centre. All static colliders use the shared static material; the handle uses restitution 0.1, friction 0.4, rolling resistance 0, bounce threshold 0.1 m/s.

| Body | Collider | Shape | Dimensions (m) | Centre |
| --- | --- | --- | --- | --- |
| Base (static) | Box | Box | 0.7 × 0.5 × 0.5 | (0, 0.25, 0) |
| Base | Socket | Sphere | radius 0.05 | (0.35, 0.25, 0) |
| Handle (on the slider) | Plate | Box | 0.6 × 0.08 × 0.4 | (0, 0.86, 0) |
| Handle | Shaft | Box | 0.08 × 0.50 × 0.08 | (0, 0.57, 0) |

Handle mass 0.3 kg (**starting value**); principal moments of a 0.6 × 0.68 × 0.4 box of that mass: (0.0156, 0.0130, 0.0206) kg·m²; gravity (0, −9.81, 0). The plate is a flat pad, not a bar, so a ball can sit on it. Its underside is 0.32 m above the box top at rest and 0.02 m above it at full stroke, so nothing is pinched. The shaft's lower end is at y = 0.32 at rest and 0.02 at full stroke: it stays inside the box.

### Slider

One `PrismaticSliderDescriptor` (`CuriousContraptions.Abstractions/MechanicalContractValues.cs:104`) along the base's +Y: lower limit −0.30 m, upper limit 0; stiffness 80 N/m; damping 8 N·s/m; compression 0; `BodiesCollide` false (the shaft passes into the box); no drive. All fixed, **starting values**.

### Threshold node on the stroke (capability narrower than needed, section 3)

One threshold node over a committed scalar, the slider coordinate: it publishes one occurrence when inward travel reaches 0.27 m (90 % of the stroke) and re-arms at a separate level, when travel falls back under 0.10 m. It is the Pulse source node `fire`, routed in the tick it is published; port binding `ActivationOut` → `fire`, `SignalTerminal.Output`.

### Ports

| `PortId` | Domain | Direction | Role | Kind | Local position |
| --- | --- | --- | --- | --- | --- |
| `ActivationOut` | Signal | Output | `ActivationOut` | Pulse | box side, (0.35, 0.25, 0), normal +X |

This is the Impact switch's output in every field (`elements/ImpactSwitch/ImpactSwitchElementDeclaration.cs:40`). It connects to any `ActivationIn` (Signal, Input, Pulse): [EL-093](EL-093-toy-dynamite-charge.md), [EL-092](EL-092-toy-rocket.md), [EL-104](EL-104-toy-firework.md), [EL-105](EL-105-toy-missile.md), [EL-095](EL-095-toy-revolver.md) and the delivered Cannon and Fan. Both ends agree in domain, role pair and kind.

### Parameters

None. The fire point is fixed per kind (D13).

### Metadata and bindings

Category `Control`, display name "Plunger box", palette order 437 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)), colour (0.16, 0.22, 0.33). Events: the stroke occurrence the engine story adds. Presentation: body binding `Visual/Handle` → `PackageBodyIndex(1)`; `Visual/Lamp` albedo (three channel bindings) on `PresentationDriver.CommittedActivation` of `fire`, slate `#556573` to gold `#f7cb52`.

### Art and solidity

| Node | Mesh | Class | Moved by |
| --- | --- | --- | --- |
| `Visual/Box` | navy `#293954` box, cream `#fff8e9` panel lines and a coral `#f06e54` band printed flat | solid, Box collider | nothing |
| `Visual/Socket` | gold sphere | solid, Socket collider | nothing |
| `Visual/Handle` | gold `#f7cb52` plate on a cream shaft | solid, Plate and Shaft colliders | body pose |
| `Visual/Lamp` | disc printed flat on the box front | decorative: printed flat, recoloured by a binding | binding |

No cable is drawn by the part; the connection is the delivered thin signal wire, which stays a non-solid drawing (owner decision, 22 November 2026). Icon: a navy box with a raised gold T. It differs from the Impact switch by the tall handle on a shaft (the switch is a low button) and from the Pressure plate by its narrow box.

### Behaviour and numbers

Laws: contact, the prismatic joint with its declared spring, damper and stops, and the signal network. Nothing fires on a timer or a command.

- **Rest.** The handle sags 0.3 × 9.81/80 = 0.037 m on its spring.
- **Static fire point.** Holding the handle at 0.27 m takes 80 × 0.27 = 21.6 N, less the handle's own 2.94 N: 18.7 N, a resting 1.9 kg.
- **A resting Basketball (1 kg) does not fire.** Equilibrium (9.81 + 2.94)/80 = 0.159 m. Released touching the plate it moves 0.122 m to equilibrium; damping ratio 8/(2√(80 × 1.3)) = 0.39 gives a 26 % overshoot, 0.032 m, so it peaks near 0.19 m, 0.08 m short.
- **A resting Weight or Bowling ball (4 kg) fires.** Equilibrium (39.2 + 2.94)/80 = 0.53 m, beyond the 0.30 m stop: the handle bottoms out. A 3 kg Weight: 0.40 m, also bottoms out.
- **A dropped Basketball fires.** From 1.0 m it arrives at 4.4 m/s; ball and handle move on together at 4.4 × 1/1.3 = 3.4 m/s, 7.5 J. The fire point is 0.23 m below the rest position, about 0.085 s away at that speed. Over that travel the spring stores 2.9 J and gravity returns 3.0 J, which cancel; the damper takes at most 8 × 3.4 × 0.23 = 6.3 J (its force at the starting speed over the whole travel, an upper bound). At least 1.2 J is left at the fire point, so it fires. Re-arming at 0.10 m gives one pulse per strike.
- **No energy passes on.** The pulse is a signal occurrence. The spring's energy returns to the handle and the ball; a consumer spends only its own store.
- **Tick order.** The observer reads the committed coordinate; the pulse is routed in the tick it is published (signal routing is same-tick) and the consumer acts in the physics step that follows. No row times this to a tick.
- **Fit.** The 0.6 × 0.4 m plate catches every delivered ball (the Basketball is 0.68 m across and rests on it).

## 3. Engine capabilities

Map row families (navigation only, D14): ContactImpulse, JointConstraint, RigidBodyDynamics, SignalPropagation, StateTransaction. The row's ChemicalReaction belongs to the charge, not to this part.

| Capability | Status | Evidence |
| --- | --- | --- |
| Passive prismatic slider with spring, damping and both stops | present | `MechanicalContractValues.cs:104`; `SliderInstanceTests.ThePlateHoldsItsLowerAndUpperStops` (`CuriousContraptions.tests/SliderInstanceTests.cs:61`) |
| A slider body carrying two box colliders | present, untested | a moving body's colliders are its baked list in `MechanicalPayload.MovingBodyColliders` (`BundleServices.cs:75`); each delivered slider body carries one |
| A Signal pulse output reaching a consumer's input | present | `ContactActivationTests.ActualBaseBoxContactUsesInclusiveNonHalfThresholdAndExactReportedSpeed` (`CuriousContraptions.tests/ContactActivationTests.cs:235`) routes a switch occurrence to a lamp |
| A consumer refusing a command it cannot pay for | present | `CannonIntegrationTests.PhysicalLoadChargesAndFiresOnlyWithItsSeparateSupply` (`CuriousContraptions.tests/CannonIntegrationTests.cs:550`), unpowered case |
| Lamp recoloured from committed activation | present, untested | `PresentationDriver.CommittedActivation` (`CuriousContraptions.Abstractions/ElementPresentation.cs:22`); no test body opened for this driver |
| **A repeatable stroke occurrence on a prismatic coordinate, as a signal source** | narrower | the travel observer takes a revolute coordinate and fires once (`CoordinateTravelDescriptor`, `CuriousContraptions.Abstractions/CoordinateTravelValues.cs:13`); the prismatic endpoint observation closes an electrical supply contact at a limit, not a signal pulse at a threshold (`PrismaticContactControl`, `CuriousContraptions.Abstractions/SignalContactControl.cs:13`); a contact trigger needs a static owner struck by a dynamic target (`ContactActivationTests.TriggerRequiresActualStaticOwnerDynamicTargetAndUniqueOwnership`, `ContactActivationTests.cs:77`, name only). Owner: **ENGINE-SIGNAL-NODES** (owner decision 3a and coordinator decision 16: the threshold over a committed scalar), with the addition in the Batch L summary: the scalar may be a prismatic or revolute coordinate; the threshold declares a separate re-arm level; the occurrence is a Pulse source node routed in the tick it is published |

Counts: 3 present, 2 present-untested, 1 narrower, 0 missing. Nothing needs element-specific engine code.

## 4. Sources and legacy

- Requirement: "Mechanical input emits a typed control command after actual stroke"; outcome "Command supplies no explosion energy of its own" ([element-094](../requirements.md#element-094)).
- `git grep -i plunger` at `a6c914e` finds only the wound-spring launcher's plunger head, a launcher, not a command source.

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| L1 | The wound-spring plunger is a guided body on a frame joint, pushed by payload contact | `CuriousContraptions.tests/ScenePlungerBindingTests.cs@a6c914e:L148-L174` | carry forward only "a guided slider moved by actual contact" |
| L2 | The Domino emits once after turning 45° from its admitted pose; Reset re-arms | `engine/gpu/WorkshopDomino.cs@a6c914e:L25-L26` | carry forward as "fire after actual motion" |

**Files harvested:** `CuriousContraptions.tests/ScenePlungerBindingTests.cs` (checked; no element knowledge).

## 5. Acceptance outline

Requirement row: [element-094](../requirements.md#element-094). Planned test class `PlungerTests`. `q` is the committed inward travel.

| # | Given / When / Then | Planned test | Control | Boundary and tolerance |
| --- | --- | --- | --- | --- |
| A1 | Given a Bowling ball dropped 0.5 m onto the plate and a Signal lamp on `ActivationOut`, then one pulse is published and the lamp lights | `AFullStrokeSendsOneCommand` | A2 | exactly one occurrence while the ball stays on the plate; 3 s guard |
| A2 | Given no load, then no pulse | `AnUnloadedHandleSendsNothing` | A1 | q stays under 0.10 m for 3 s |
| A3 | Given a Basketball released touching the plate, then no pulse; given a Weight with `mass` 3 kg released the same way, then one pulse | `ARestingLightLoadDoesNotFireAndAHeavyOneDoes` | each is the other's control | Basketball: q never reaches 0.25 m (hand peak 0.19 m); Weight: bottoms out |
| A4 | Given a Basketball dropped from 1.0 m, then one pulse | `ADroppedBallFiresByItsMomentum` | A3's resting Basketball | 3 s guard |
| A5 | Given a ball that strikes and rolls off, then a second ball, then two pulses in that order | `EachSeparateStrokeSendsOneCommand` | a held plate sends one | count exactly 2 |
| A6 | Given `ActivationOut` wired to an unpowered Cannon holding a ball, when the plunger fires, then the Cannon refuses and the ball stays | `TheCommandCarriesNoEnergy` | a powered, charged Cannon launches on the same pulse | ball speed under 0.01 m/s in the refused case |
| A7 | Given the plunger wired to [EL-093](EL-093-toy-dynamite-charge.md), then the energy added to the scene is at most the charge's `energy` and equals its store debit | `ThePlungerCommandsTheChargeAndSuppliesNoEnergy` | unwired: the handle moves, nothing releases | lands with the later of the two slices |
| A8 | Given `ActivationOut` offered to an Electrical `PowerIn`, then the connection is refused | covered generically by `BundlePortsTests.AWrongDomainOrOutputToOutputLinkIsStillRefused` (`CuriousContraptions.tests/BundlePortsTests.cs:262`) | an `ActivationIn` is accepted | typed refusal |
| A9 | Reset returns the handle to rest and re-arms the observer; save and load keep the wire | `ResetAndSaveLoadRestoreThePlunger` | — | also `CatalogueIntegrationTests.EveryAdmittedDeclarationRetainsAuthoredStateAcrossRunResetSaveLoad` (`CuriousContraptions.tests/CatalogueIntegrationTests.cs:273`) |

No parameters, so no parameter refusal applies.

### Integration

- Pressed by CAT-014 Bowling ball, CAT-001 Basketball, the Weight (delivered). Commands the delivered Cannon, Fan, Signal lamp, State latch, Counter and Delay box, and the planned EL-092, EL-093, EL-095, EL-104, EL-105.
- Campaign: chapter 10, levels 91–100, "dynamite/plunger abstraction" (`docs/planning/requirements.md:2411`; `:2295`; `docs/planning/invest/campaign-reservations.md:360`); reuse 136–150.
- A level that fixes a plunger as authored scenery needs S8.

### Delivery

- **Shape.** Single element slice; it can precede EL-093 because rows A1–A6 use delivered consumers.
- **Prerequisites, in order.** The engine programme (owner decision 3a), of which this part leans on ENGINE-ISLANDS-SLEEP; then ENGINE-SIGNAL-NODES with the addition above; then S7 (section 7). S3 optional. Port roles, node arity, target masks and mouth identities are written in the form ENGINE-CAPABILITY-INSTANCES leaves; the names here are today's.
- **Files.** `elements/Plunger/` (project, declaration, `plunger.tscn`, `icon.svg`); `CuriousContraptions.tests/PlungerTests.cs`; one `CuriousContraptions.slnx` line; until S3, the trim list and placement switch.
- **Review tier.** Engine tier (two present-untested rows).
- **Disqualifiers the probe re-checks.** A slider body that admits only one collider; a spring that cannot be declared without a preload parameter; the threshold node published without a prismatic coordinate as its scalar or without a separate re-arm level.

## 6. Open questions

All resolved.

1. **Damped spring, no preload** (decided now): a preload would need the spring's rest point outside the travel limits, which no delivered slider declares; damping at 8 N·s/m keeps a resting Basketball 0.08 m short of the fire point.
2. **Mechanical only** (D19): no electrical trigger variant; the row says "mechanical input".
3. **Re-arms when the handle returns** (decided now): a threshold node with a separate re-arm level is one general rule, and the Bellows' stroke count can use the same node.
4. **`fire_fraction` dropped** (D13): fixed at 90 % of the stroke.

Needs the owner: none.

## Engine conformance (22 November 2026)

Checked against the owner addendum of 22 November 2026 (AGENTS.md element-free engine gate and Tri-Graph rule; `_bmad/custom/element-free-engine-design.md`; `docs/gpu-f32-physics.md`).

1. **Declaration data only.** A static box, one sprung slider body, one stroke observer, one signal node, one port and bindings. No handler runs when the handle is pressed.
2. **Generic laws.** The handle moves by contact against a declared spring and damper; the command is a general threshold over a committed scalar (here a prismatic coordinate), owned by ENGINE-SIGNAL-NODES; routing is the signal network.
3. **Three layers.** Colliders: base box and socket sphere (static), plate and shaft boxes (handle). Visuals: one mesh per collider; lamp and band printed flat. Animation: the lamp colour reads committed activation; the handle art follows body 1's pose.
4. **Flow and typing.** One typed Signal output; no parameters; no strings in domain logic.
5. **Flat state.** Bodies 2 (1 static, 1 dynamic); colliders 4 (boxes 3, sphere 1); joints 1 prismatic; drives 0; sensors 0; signal nodes 1 (a threshold node over the slider coordinate); stores 0; presentation bindings 4 (body, three lamp channels).
6. **Performance.** Four primitives. The threshold node reads one committed coordinate. No sleeping exists today: the cost of the handle resting on its spring is owned by ENGINE-ISLANDS-SLEEP (owner decision 3a). This part does not design around it.
7. **Tests are scenarios.** Every row runs the production simulation with delivered loads and consumers.

**Verdict: conforms** (one general capability narrower than needed, owned by ENGINE-SIGNAL-NODES; the audit fixes of 22 November 2026 are applied).

## 7. Coverage binding (planned, not yet live)

Published main binds a named-identity package through a final section of its spec headed "Package source binding", read by `tools/Coverage/NamedPackageSources.cs` (pattern: EL-239 Baseball). The section is not written here: the reader parses every spec carrying that heading and fails the whole run when a field does not resolve. **Blocked on the coverage follow-on story S7.**

| Field | Planned value |
| --- | --- |
| Package | `plunger` |
| Named identity | EL-094, `../invest/named-elements.md#element-094` |
| Acceptance | `todo-197`, `sequence-task-647` |
| Decision | S664 |
| Mode records | `Configuration=Fixed` |

What it lacks today: `docs/planning/invest/scope-corrections.md` has no `s664` block; `todo-197` is a scope index shared by EL-091 to EL-095 (`docs/planning/requirements.md:4100`) and is not co-anchored with this identity's own task, `sequence-task-647`, which sits beside `element-094` (`docs/planning/requirements.md:5149`).
