# EL-092 · Toy rocket — named-identity spec

**Current delivery (24 October 2026):** [EL-092 delivery slice](../invest/named-elements.md#element-092). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

**Build-ready revision, 22 November 2026 (checkout `8d1d68aa`).** Every section is written against current code; a `path:line` citation without a commit is in this checkout, and `path@a6c914e:Lx-Ly` resolves with `git show a6c914e:<path>`. Values marked **starting value** are adopted under decision D1 of [delivery batches](../invest/delivery-batches.md#3-family-level-decisions). All game values are canonical f32. No number here was simulated; each is a hand calculation shown beside it.

**Safety boundary.** An abstract toy. Its charge is a declared energy budget in joules and a declared force in newtons; no real propellant, formulation, quantity or construction is modelled or described (requirement row).

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name / type | EL-092 · Toy rocket · Specialist |
| Anchor | [requirements.md#element-092](../requirements.md#element-092); [named entry](../invest/named-elements.md#element-092); scope index [todo-197](../requirements.md#todo-197) |
| Proof owner | S662 |
| Disposition | To deliver as a single element, first of the jet run (EL-092, then [EL-104](EL-104-toy-firework.md), then [EL-105](EL-105-toy-missile.md)). Not a Family member: the three differ in ports, signal nodes and sensors, not only in values. Not a duplicate of any part. |
| Planned package | id `toy_rocket`, directory `elements/ToyRocket/`, class `ToyRocketElementDeclaration` |
| Refines / extends | No CAT spec. Its store-paid, latch-enabled jet is the delivered Fan's self-contained mode (`elements/Fan/FanElementDeclaration.cs:42`–`:55`, `:69`–`:71`) |
| Status | not started; blocked on the engine programme (owner decision 3a), then ENGINE-REACTION-THRUST, then S7 |

## 2. Declaration

The package implements `IEnergyStoreElement` (`CuriousContraptions.Abstractions.Mechanical/MechanicalContracts.cs:35`), `IAirflowSourceElement` (`:40`), `ISignalElement` (`CuriousContraptions.Abstractions.Signal/SignalContracts.cs:8`) and `ISceneGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`). `KinematicMode.Dynamic`, `SupportedDomains = Mechanical | Signal`. Footprint: 1 body, 0 moving bodies, no constraints.

### Body and colliders

One dynamic body, long axis local +Y, nozzle at −Y. Mass 0.4 kg (**starting value**: a Tennis ball is 0.35 kg; propellant mass is not tracked). Declared mass frame: centre of mass at the origin; principal moments of a solid cylinder of radius 0.12 m and length 0.9 m: axial ½·0.4·0.12² = 0.00288 kg·m², transverse 0.4·(3·0.12² + 0.9²)/12 = 0.0284 kg·m². Gravity (0, −9.81, 0); mass-frame `linearDrag` 0.04 /s (`CuriousContraptions.Abstractions/MassFrameValues.cs:39`), the value the delivered balls declare as an ambient load (`elements/Ball/BallElementDeclaration.cs:59`). Material for every collider: restitution 0.2, friction 0.4, rolling resistance 0, bounce threshold 0.1 m/s (**starting values**: a cardboard tube).

| Collider | Shape | Dimensions (m) | Offset |
| --- | --- | --- | --- |
| Tube | Cylinder about +Y | radius 0.12, half-length 0.45 | origin |
| Nose | Sphere | radius 0.12 | (0, 0.45, 0) |
| Fins A | Box | 0.44 × 0.18 × 0.02 | (0, −0.36, 0) |
| Fins B | Box | 0.02 × 0.18 × 0.44 | (0, −0.36, 0) |

Four colliders against the Tennis ball's two. Reason: the nose and the four fins are proud visible solids, and the crossed fin pair gives a 0.44 m base so the rocket stands on a bench.

### Store, jet and signal nodes

- **Store** `1` (`EnergyStoreDescriptor`, `CuriousContraptions.Abstractions/EnergyStoreValues.cs:73`): capacity and initial energy both the parameter `charge`; charge power 0. It starts full and nothing refills it, exactly as the Fan's own store (`FanElementDeclaration.cs:42`–`:44`).
- **Jet** `1` (`AirflowSourceDescriptor`, `CuriousContraptions.Abstractions/AirflowValues.cs:45`): frame body 0, origin (0, −0.45, 0), axis (0, −1, 0), reach 1.0 m, terminal radius 0.4 m, flow speed 16 m/s, maximum force the parameter `thrust`, maximum power `thrust` × 16 m/s (force × flow speed, the Fan's convention, `FanElementDeclaration.cs:49`–`:51`); internal occluders: its own four colliders; one `StoredAirflowInput` on store 1 with `Enable = (lit, ignited)`.
- **Ambient reaction declared on the jet (missing capability, section 3):** the share of the jet's rated force not delivered to a body in the cone reacts on the rocket. There is no separate thrust value: thrust is the jet's own maximum force.
- **Signal nodes:** one `SignalMemoryDescriptor` `ignited` (`CuriousContraptions.Abstractions/SignalContractValues.cs:64`); port binding `ActivationIn` → `ignited`, `SignalTerminal.Set`. Nothing is wired to its Reset terminal, so it stays set for the Run.

### Ports

| `PortId` | Domain | Direction | Role | Kind | Local position |
| --- | --- | --- | --- | --- | --- |
| `ActivationIn` | Signal | Input | `ActivationIn` | Pulse | tube front, (0, −0.15, 0.12), normal +Z |

It matches the `ActivationOut` (Signal, Pulse) of the Impact switch (`elements/ImpactSwitch/ImpactSwitchElementDeclaration.cs:40`) and of [EL-094](EL-094-detonation-plunger.md).

### Parameters

| Key | Kind | Unit | Default | Min | Max | Editor policy | Reason |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `thrust` | Real | Newton | 6 | 1 | 10 | Reject | 6 N on 0.4 kg is 15 m/s²: a net 5.2 m/s² climb |
| `charge` | Real | Joule | 100 | 20 | 150 | Reject | about a 1.2 s burn and a 3.4 m apex at defaults |
| `lit` | Choice (`Unlit`, `Lit`) | None | 0 (`Unlit`) | 0 | 1 | Reject | a binary Build-only choice, as the Fan's `powered`: bundle validation refuses any other form of initial state (`CuriousContraptions.Bundle/AirflowDomain.cs:122`–`:125`); `Lit` starts the burn at Run |

All Build-only (D11).

### Metadata and bindings

Category `Motion`, display name "Toy rocket", palette order 172 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)), colour (0.87, 0.44, 0.35). Presentation: `Visual/Plume` `ScaleY` on `PresentationDriver.CommittedFlowSpeed` (0 at rest, 1 at 16 m/s); `Visual/FuelBand` `ScaleY` on `PresentationDriver.CommittedStoredFraction`, `ClampToEndpoints`, store 1 (the Cannon's charge-bar binding, `elements/Cannon/CannonElementDeclaration.cs:55`–`:57`). The root visual follows the body pose.

### Art and solidity

| Node | Mesh | Class | Moved by |
| --- | --- | --- | --- |
| `Visual/Tube` | cream `#fff8e9` cylinder | solid, Tube collider | body pose |
| `Visual/Nose` | coral `#de7058` dome | solid, Nose collider | body pose |
| `Visual/Fins` | four coral fins as two crossed plates | solid, Fins A and B | body pose |
| `Visual/FuelBand` | navy `#293954` band, flush with the tube | decorative: flush, scaled by a binding | binding |
| `Visual/Plume` | gold `#f7cb52` cone below the nozzle | decorative: glow, scaled by a binding | binding |

Icon: a navy upright rocket with a gold flame. At a glance it differs from the Toy firework by its four fins and plain nose (the firework has a stick and a star cap) and from the Toy missile by standing upright with no eyes.

### Behaviour and numbers

Laws: rigid-body dynamics and contact; the store-paid conical airflow law; the ambient reaction of that jet on its own body (missing); the signal network's memory node.

- **Ignition.** A pulse on `ActivationIn` sets `ignited`; signal routing is same-tick and the jet pays from the physics step that follows the commit. No row times this to a tick.
- **Thrust.** While enabled and the store can pay, the rocket is pushed along its own +Y at the nozzle point, which lies on the mass axis, so thrust adds no torque. With forward speed v and flow speed u = 16 m/s the force is `thrust` × (1 − v/u), so the rocket can never outrun its own jet, and the store pays force × u: `thrust` × 16 × (1 − v/16) watts. In the story's words: the reaction impulse on the body, at the nozzle and opposite the exhaust axis, is (granted limit − impulse granted to exposed receivers) × pressure scale × clamp((flow speed + origin velocity · axis) ÷ flow speed, 0, 1), and the work paid is that impulse × flow speed. Receiver candidates and occlusion come from ENGINE-SPATIAL-QUERIES.
- **Burn at defaults, vertical, free.** dv/dt = a₀ − k·v with a₀ = 6/0.4 − 9.81 = 5.19 m/s² and k = 6/(0.4 × 16) = 0.94 /s, so v(t) = 5.54 × (1 − e^(−0.94 t)) m/s and h(t) = 5.54 × (t − (1 − e^(−0.94 t))/0.94). Energy paid E(t) = 96·t − 6·h(t). At t = 1.2 s: v = 3.7 m/s, h = 2.7 m, E = 115 − 16 = 99 J, so the 100 J store lasts about 1.2 s. Coast 3.7²/(2·9.81) = 0.7 m; apex about 3.4 m above the start. Drag at 0.04 /s removes under 4 % of speed per second and is ignored.
- **Extremes.** `thrust` 10 N with `charge` 150 J: a₀ = 15.2 m/s², k = 1.56 /s, terminal 9.7 m/s; E(t) = 160·t − 10·h(t) reaches 150 J near t = 1.5 s with h = 8.9 m and v = 8.8 m/s; apex about 12.8 m. Speed can never pass the 16 m/s flow speed, under the 64 m/s linear envelope (`CuriousContraptions.Abstractions/CanonicalBodyValues.cs:16`). `thrust` under the weight (0.4 × 9.81 = 3.92 N) never lifts the rocket; at 3 N the store drains at 3 × 16 = 48 W, 100/48 = 2.1 s on the bench.
- **Spent.** An empty store pays nothing, so the force is zero from the next substep; nothing recharges it; Reset restores the full store.
- **Vacuum.** The reaction scales with the atmosphere's pressure scale: in a Vacuum atmosphere this toy air jet gives no thrust and pays nothing.
- **Exhaust.** A body inside the cone takes its share of the same rated force, and the rocket receives the equal and opposite of that share as delivered today. Geometry of row A6: a lying rocket rests on two fin tips (0.22 × sin 45° = 0.156 m) and its nose (0.12 m), so its axis is about 0.15 m above the bench; a Tennis ball's centre is at 0.25 m. At 0.5 m behind the nozzle the cone's radius is 0.4 × 0.5 = 0.2 m, so the ball's two side samples (0.125 m off its centre, 0.16 m from the axis) and its bottom sample are inside the cone and its top sample is not. The row asserts only that the ball is pushed away.
- **Fit.** 0.24 m across the tube and 0.44 m across the fins: it passes the pipe bore (1.3 m across); it is outside the Cannon's 0.6–0.84 m seating window (`CannonElementDeclaration.cs:32`) and is not a cannon payload. Its 3.9 N weight is below a default 0.5 kg Pressure plate's 4.9 N.
- **Tunnelling.** At no more than the 16 m/s flow speed the rocket moves 0.033 m per 480 Hz substep, about a quarter of its radius; the engine's velocity-fattened bounds and speculative contacts ([solver model](../../gpu-f32-physics.md), `docs/gpu-f32-physics.md:195`, `:258`) cover it with no per-part check.

## 3. Engine capabilities

Map row families (navigation only, D14): ChemicalReaction, EnvironmentState, FiniteLedger, FiniteWorkActuation, GeometryQuery, RigidBodyDynamics, StateTransaction.

| Capability | Status | Evidence |
| --- | --- | --- |
| Finite store, full at Run, paying a conical airflow source | present | `EnergyStoreValues.cs:73`, `AirflowValues.cs:36`; `AirflowRuntimeTests.FiniteConeTransfersRatedForceAccordingToMass` (`CuriousContraptions.tests/AirflowRuntimeTests.cs:52`) and `FanIntegrationTests.ActualHubDoesNotOccludeThePaidConeButOffAndVacuumDoNotTransfer` (`CuriousContraptions.tests/FanIntegrationTests.cs:514`) |
| A store emptied by its jets stops them | present | `AirflowRuntimeTests.DifferentFlowSpeedsShareOneFiniteStoreInJoules` (`AirflowRuntimeTests.cs:82`) |
| Jet enabled by a parameter and a signal memory set from `ActivationIn` | present | declared by the Fan (`FanElementDeclaration.cs:53`, `:69`–`:71`); `FanGuidanceTests.ColdStartRequiresTheConnectedSwitchOccurrenceBeforeAnyTransfer` (`CuriousContraptions.tests/FanGuidanceTests.cs:63`): no flow before the wired switch occurrence, flow after it, none when unwired |
| Declared mass frame independent of several colliders on a free body | present | `CompoundMassAndRollingTests.CollisionChildrenDoNotChangeDeclaredFreeFlight` (`CuriousContraptions.tests/CompoundMassAndRollingTests.cs:283`) |
| A dynamic source body receives the paired force and moment of what its jet delivers to an exposed receiver | present | `AirflowRuntimeTests.MovingNozzleReceivesPairedForceAndMoment` (`CuriousContraptions.tests/AirflowGeneralityTests.cs:189`): nozzle and cargo momenta sum to zero |
| An airflow source, store, memory node and Signal port declared on a **dynamic** package | present, untested | the law scene's nozzle is dynamic (`AirflowGeneralityTests.cs:192`); every delivered package with a source sits on a static base |
| Store-fraction and flow-speed presentation drivers | present | `ElementPresentation.cs:18`–`:37`; the store fraction mapping is asserted in `CannonIntegrationTests.PhysicalLoadChargesAndFiresOnlyWithItsSeparateSupply` (`CuriousContraptions.tests/CannonIntegrationTests.cs:550`) |
| **A dynamic source body pushed by its jet when nothing is in the cone (ambient reaction)** | missing | a branch exists only per exposed receiver sample (`CuriousContraptions.Simulation/WorkshopPhysicsEngine.Airflow.cs:105`–`:130`); a jet into open air applies nothing and pays nothing, and gas outflow is tested as thrust-free (`GasRuntimeTests.FiniteOutflowChangesOneMassAndShapeWithoutThrustOrChangingDrySpinInertia`, `CuriousContraptions.tests/GasRuntimeTests.cs:151`). Owner: **[ENGINE-REACTION-THRUST](../../../_bmad-output/planning-artifacts/epics.md#story-engine-reaction-thrust-airflow-source-reaction-converging-flow-and-ambient-angular-drag)** (`engine-reaction-thrust`). The axial gas chamber of `ENGINE-GAS-NETWORK` is a gas chamber on a slider and does not cover it |

Counts: 6 present, 1 present-untested, 0 narrower, 1 missing. Nothing needs element-specific engine code.

## 4. Sources and legacy

- Requirement: "Finite stored propellant/energy produces bounded thrust and exhaust"; outcome "Spent rocket produces no further thrust; abstract toy parameters only" ([element-092](../requirements.md#element-092)).
- No legacy rocket exists (`git grep -i rocket` at `a6c914e` finds nothing). The legacy gas-nozzle facts below are background for the story, not a model to rebuild.

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| L1 | A closed or balanced nozzle has zero mass flow and zero thrust | `CuriousContraptions.tests/ConvergingGasNozzleTests.cs@a6c914e:L64-L74` | carry forward as "no payable input, no thrust" |
| L2 | Discharge is finite; zero change gives zero transit | `CuriousContraptions.tests/GasNozzleTransitTests.cs@a6c914e:L89-L103` | carry forward as "spent produces nothing"; do not carry forward the f64 quadrature |
| L3 | A store debit never exceeds stored energy | `engine/physics/PhysicsEnergyStore.cs@a6c914e:L47-L52` | carry forward (the current ledger enforces it) |

**Files harvested:** `CuriousContraptions.tests/ConvergingGasNozzleTests.cs`, `engine/physics/GasNozzleTransit.cs`, `CuriousContraptions.tests/GasNozzleTransitTests.cs`, `engine/physics/PhysicsEnergyStore.cs`.

## 5. Acceptance outline

Requirement row: [element-092](../requirements.md#element-092). Planned test class `ToyRocketTests`; every row runs the production simulation through the integration harness. Heights are measured from the start pose.

| # | Given / When / Then | Planned test | Control | Boundary and tolerance |
| --- | --- | --- | --- | --- |
| A1 | Given an upright rocket wired to an Impact switch, when a ball strikes the switch, then the rocket climbs, coasts and falls back | `APulseIgnitesTheRocketAndItClimbsOnItsStore` | A2 | apex between 2.4 m and 4.4 m (hand value 3.4 m; the band is what a level needs: over a 2 m wall, under a 5 m ceiling); 6 s guard |
| A2 | Given no pulse, then the rocket never moves and its store stays full | `WithoutAPulseTheRocketStaysPut` | A1 | displacement under 0.02 m over 3 s |
| A3 | Given burnout, then the store is empty, the rocket is in free fall, and a second pulse changes nothing | `ASpentRocketGivesNoFurtherThrust` | A1's burn | store at most 0.01 J; vertical acceleration within 0.5 m/s² of −9.81 over 0.25 s of coast; no upward speed gain after the second pulse |
| A4 | Given a burn, then the store's debit is never less than the rocket's gain in kinetic plus potential energy | `TheStorePaysForEverythingTheRocketGains` | — | gain ≤ debit at every committed sample; no negative balance |
| A5 | Given `thrust` 3 N, then the rocket never leaves the bench and the store empties; given a Vacuum atmosphere at 6 N, then it never moves and the store stays full | `ThrustBelowWeightOrInVacuumDoesNotLift` | `thrust` 6 N in the Standard atmosphere lifts | lift-off means rising more than 0.05 m; at 3 N empty within 1.7–2.5 s (hand 2.1 s) |
| A6 | Given a rocket lying nose against a Wall with a Tennis ball 0.5 m behind the nozzle, when lit, then the ball is pushed away | `TheExhaustPushesABallBehindTheNozzle` | a Wall between nozzle and ball: the ball does not move | ball travels over 0.5 m; with the control Wall, no motion |
| A6b | Given a free flight, then the rocket's speed never exceeds the flow speed | `TheRocketCannotOutrunItsJet` | — | speed under 16 m/s at every committed sample, with `thrust` 10 N and `charge` 150 J |
| A7 | Given a Wall 1 m above the nose, then contact stops the climb and the store still empties | `ACeilingStopsTheClimbByContact` | A1 | nose overlap at most 0.02 m |
| A8 | Given `thrust` 11 N or `charge` 0 J, then the edit is refused and nothing changes | `OutOfRangeValuesAreRefused` | defaults accepted | typed refusal |
| A9 | Given a Run, then Reset restores pose, a full store and a clear latch; save and load keep parameters and the wire | `ResetAndSaveLoadRestoreTheRocket` | — | also the generic `CatalogueIntegrationTests.EveryAdmittedDeclarationRetainsAuthoredStateAcrossRunResetSaveLoad` (`CuriousContraptions.tests/CatalogueIntegrationTests.cs:273`) |
| A10 | Given a Battery `Supply` offered to `ActivationIn`, then the connection is refused | covered generically by `BundlePortsTests.AWrongDomainOrOutputToOutputLinkIsStillRefused` (`CuriousContraptions.tests/BundlePortsTests.cs:262`) | a Signal output is accepted | typed refusal |

### Integration

- Ignited by CAT-063 Impact switch, CAT-023 Domino, CAT-052 Pressure plate (all delivered) or [EL-094](EL-094-detonation-plunger.md). Exhaust targets: any delivered ball. Blocked by CAT-066 Wall.
- Campaign: chapter 10 "Oddly Satisfying", levels 91–100, "toy rocket/ignition/destruction variants" (`docs/planning/requirements.md:2295`); first use 91–100, reuse 136–150 (`:2411`; `docs/planning/invest/campaign-reservations.md:360`).
- A level that fixes a rocket in place as authored scenery needs the lesson-fixture story (S8); a rocket in the player's inventory does not.

### Delivery

- **Shape.** Single element slice.
- **Prerequisites, in order.** The engine programme (owner decision 3a), of which this part leans on ENGINE-SPATIAL-QUERIES (airflow candidates and occlusion) and ENGINE-ISLANDS-SLEEP; then ENGINE-REACTION-THRUST; then the coverage follow-on story S7 (section 7). S3 optional. Port roles, node arity, target masks and mouth identities are written in the form ENGINE-CAPABILITY-INSTANCES leaves; the names here are today's.
- **Files.** `elements/ToyRocket/` (project, declaration, `toy_rocket.tscn`, `icon.svg`); `CuriousContraptions.tests/ToyRocketTests.cs`; one `CuriousContraptions.slnx` line; until S3, the trim list and placement switch (`docs/planning/invest/delivery-batches.md`, S3).
- **Review tier.** Engine tier (one present-untested row).
- **Disqualifiers the probe re-checks.** An airflow source refused on a dynamic base; a latch that the port cannot set; the reaction story published with a law other than the one in EL-090 section 3 (then every number under Behaviour is redone).

## 6. Open questions

All resolved.

1. **Mass stays constant** while the charge burns (D1, D19): the toy is an energy budget, not a propellant mass.
2. **No heat from the exhaust** (D19): no requirement or lesson uses it; a thermal mode would be a later criterion under ENGINE-THERMAL.
3. **The exhaust push is in the first slice**: the requirement names "thrust and exhaust", and the jet is the delivered airflow law.
4. **Parameters are `thrust` and `charge`**, not a total impulse: the unit set has newtons and joules and no newton-second (`CuriousContraptions.Abstractions/ParameterDescriptor.cs:15`).
5. **`lit` is a two-option Choice** (audit M1, 22 November 2026): the only form of initial enable state that bundle validation admits.

Needs the owner: none.

## Engine conformance (22 November 2026)

Checked against the owner addendum of 22 November 2026 (AGENTS.md element-free engine gate and Tri-Graph rule; `_bmad/custom/element-free-engine-design.md`; `docs/gpu-f32-physics.md`).

1. **Declaration data only.** One body, one store, one jet, one memory node, one port, three parameters, two bindings. No callback, burn loop or engine test on this package.
2. **Generic laws.** Ignition is the signal network setting a memory node. Thrust is the ambient reaction of a declared jet's own rated force, a capability any package may declare, missing today and owned by one element-free story. Exhaust is the conical airflow law. "Spent" is an empty finite store.
3. **Three layers.** Colliders: one cylinder, one sphere, two boxes on the one body. Visuals: tube, nose and fins follow the body pose; band and plume are separate decorative meshes. Animation: two bindings read committed store fraction and flow speed and write nothing back.
4. **Flow and typing.** One typed Signal input; parameters are typed with units; no strings in domain logic.
5. **Flat state.** Bodies 1 (dynamic); colliders 4 (cylinder 1, sphere 1, boxes 2); joints 0; drives 0; airflow sources 1 (declaring the ambient reaction); sensors 0; signal nodes 1; stores 1; presentation bindings 2.
6. **Performance.** Four primitives on one body; the jet's cone is a bounded volume 1 m long. Tunnelling is excluded by the engine's fattened bounds and speculative contacts. No sleeping exists today: resting cost is owned by ENGINE-ISLANDS-SLEEP, and the airflow pass that scales with the scene by ENGINE-SPATIAL-QUERIES (owner decision 3a). This part does not design around either.
7. **Tests are scenarios.** Every row runs the production simulation; none needs a code path of this part.

**Verdict: conforms** (one general capability missing, with one owner; the audit fixes of 22 November 2026 are applied).

## 7. Coverage binding (planned, not yet live)

Published main binds a named-identity package through a final section of its spec headed "Package source binding", read by `tools/Coverage/NamedPackageSources.cs` (pattern: EL-239 Baseball). The section is not written here: the reader parses every spec carrying that heading and fails the whole run when a field does not resolve. **Blocked on the coverage follow-on story S7.**

| Field | Planned value |
| --- | --- |
| Package | `toy_rocket` |
| Named identity | EL-092, `../invest/named-elements.md#element-092` |
| Acceptance | `todo-197`, `sequence-task-645` |
| Decision | S662 |
| Mode records | `Configuration=Fixed` |

What it lacks today: `docs/planning/invest/scope-corrections.md` has no `s662` block; `todo-197` is a scope index shared by EL-091 to EL-095 (`docs/planning/requirements.md:4100`) and is not co-anchored with this identity's own task, `sequence-task-645`, which sits beside `element-092` (`docs/planning/requirements.md:5133`).
