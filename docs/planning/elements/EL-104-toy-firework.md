# EL-104 · Toy firework — named-identity spec

**Current delivery (24 October 2026):** [EL-104 delivery slice](../invest/named-elements.md#element-104). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

**Build-ready revision, 22 November 2026 (checkout `8d1d68aa`).** Every section is written against current code; a `path:line` citation without a commit is in this checkout, and `path@a6c914e:Lx-Ly` resolves with `git show a6c914e:<path>`. Values marked **starting value** are adopted under decision D1 of [delivery batches](../invest/delivery-batches.md#3-family-level-decisions). All game values are canonical f32. No number here was simulated; each is a hand calculation shown beside it.

**Safety boundary.** An abstract toy: a declared energy budget, a declared force and an authored visual. No real pyrotechnic composition, quantity, fusing or construction is modelled or described (requirement row).

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name / type | EL-104 · Toy firework · Specialist |
| Anchor | [requirements.md#element-104](../requirements.md#element-104); [named entry](../invest/named-elements.md#element-104); scope index [todo-227](../requirements.md#todo-227) |
| Proof owner | S377 |
| Disposition | To deliver as a single element, second of the jet run after [EL-092 Toy rocket](EL-092-toy-rocket.md). Not a Family member with it: the firework adds a timer node and an output port, which is behaviour, not a value. Not a duplicate. |
| Planned package | id `toy_firework`, directory `elements/ToyFirework/`, class `ToyFireworkElementDeclaration` |
| Refines / extends | No CAT spec. Jet of EL-092; timer of the delivered Delay box ([CAT-022](CAT-022-delay.md)) |
| Status | not started; blocked on the engine programme (owner decision 3a), then ENGINE-REACTION-THRUST, then S7 |

## 2. Declaration

The package implements `IEnergyStoreElement` and `IAirflowSourceElement` (`CuriousContraptions.Abstractions.Mechanical/MechanicalContracts.cs:35`, `:40`), `ISignalElement` (`CuriousContraptions.Abstractions.Signal/SignalContracts.cs:8`) and `ISceneGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`). `KinematicMode.Dynamic`, `SupportedDomains = Mechanical | Signal`. Footprint: 1 body, no constraints.

### Body and colliders

One dynamic body, long axis local +Y, nozzle at −Y. Mass 0.2 kg (**starting value**: half the rocket). Mass frame: centre at the origin; principal moments of a solid cylinder of radius 0.08 m and length 0.6 m: axial ½·0.2·0.08² = 0.00064 kg·m², transverse 0.2·(3·0.08² + 0.6²)/12 = 0.00632 kg·m². Gravity (0, −9.81, 0); mass-frame `linearDrag` 0.04 /s (`CuriousContraptions.Abstractions/MassFrameValues.cs:39`). Material: restitution 0.2, friction 0.4, rolling resistance 0, bounce threshold 0.1 m/s.

| Collider | Shape | Dimensions (m) | Offset |
| --- | --- | --- | --- |
| Tube | Cylinder about +Y | radius 0.08, half-length 0.30 | origin |
| Head | Sphere | radius 0.12 | (0, 0.34, 0) |
| Fins A | Box | 0.30 × 0.12 × 0.02 | (0, −0.24, 0) |
| Fins B | Box | 0.02 × 0.12 × 0.30 | (0, −0.24, 0) |

Four colliders, as the rocket and for the same reason. The head is wider than the tube: that bulb is its silhouette.

### Store, jet and signal nodes

- **Store** `1`: capacity and initial energy both the parameter `charge`; charge power 0 (`EnergyStoreDescriptor`, `CuriousContraptions.Abstractions/EnergyStoreValues.cs:73`).
- **Jet** `1` (`AirflowSourceDescriptor`, `CuriousContraptions.Abstractions/AirflowValues.cs:45`): frame body 0, origin (0, −0.30, 0), axis (0, −1, 0), reach 0.6 m, terminal radius 0.15 m, flow speed 16 m/s, maximum force the parameter `thrust`, maximum power `thrust` × 16 m/s; one `StoredAirflowInput` on store 1 with `Enable = (lit, ignited)`.
- **Ambient reaction declared on the jet (missing capability, section 3):** thrust is the jet's own rated force reacting on the body when nothing is in the cone, as [EL-092](EL-092-toy-rocket.md).
- **Signal nodes:** `SignalMemoryDescriptor` `ignited`; `SignalTimerDescriptor` `fuse` (`CuriousContraptions.Abstractions/SignalContractValues.cs:49`) with duration parameter `burst_delay`, `TimerCompletionPolicy.Latch`, `TimerBoundary.BeforePhysics`, `TimerElapsedSignal.Activation`. Edge: `ignited` Output → `fuse` Input. Port bindings: `ActivationIn` → `ignited` Set; `ActivationOut` → `fuse` Output.

### Ports

| `PortId` | Domain | Direction | Role | Kind | Local position |
| --- | --- | --- | --- | --- | --- |
| `ActivationIn` | Signal | Input | `ActivationIn` | Pulse | tube front, (0, −0.10, 0.08), normal +Z |
| `ActivationOut` | Signal | Output | `ActivationOut` | Pulse | tube front, (0, 0.10, 0.08), normal +Z |

### Parameters

| Key | Kind | Unit | Default | Min | Max | Editor policy | Reason |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `thrust` | Real | Newton | 4 | 1 | 6 | Reject | 20 m/s² on 0.2 kg: a quick climb |
| `charge` | Real | Joule | 32 | 8 | 80 | Reject | a 0.6 s burn and a 2.3 m apex at defaults |
| `burst_delay` | Real | Second | 1.0 | 0.2 | 5 | Reject | the default flight's apex time; seconds of game time (D10) |
| `lit` | Choice (`Unlit`, `Lit`) | None | 0 (`Unlit`) | 0 | 1 | Reject | a binary Build-only choice, as the Fan's `powered` (`CuriousContraptions.Bundle/AirflowDomain.cs:122`–`:125`); `Lit` starts the burn at Run, and the burst still waits for a pulse |

### Metadata and bindings

Category `Motion`, display name "Toy firework", palette order 174 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)), colour (0.97, 0.80, 0.32). Presentation: `Visual/Plume` `ScaleY` on `PresentationDriver.CommittedFlowSpeed`; `Visual/Burst` `Visible` on `PresentationDriver.CommittedTimerPhase` of `fuse` with state values (hidden while idle and counting, shown when finished); `Visual/Head` albedo (three channel bindings) on the same driver, cream before, slate `#556573` after.

### Art and solidity

| Node | Mesh | Class | Moved by |
| --- | --- | --- | --- |
| `Visual/Tube` | cream `#fff8e9` tube with gold spiral stripes printed flat | solid, Tube collider | body pose |
| `Visual/Head` | cream bulb with a navy `#293954` star printed flat | solid, Head collider | body pose |
| `Visual/Fins` | navy fins as two crossed plates | solid, Fins A and B | body pose |
| `Visual/Plume` | gold `#f7cb52` cone | decorative: glow, scaled by a binding | binding |
| `Visual/Burst` | gold star-shaped spray around the head, hidden until the burst | decorative: shown by a binding | binding |

Icon: a navy bulb-headed tube under a gold starburst. It differs from the Toy rocket by the bulb head and shorter body, and from the Toy missile by standing upright.

### Behaviour and numbers

Laws: as EL-092 (store-paid jet, its reaction, signal memory), plus the signal network's timer node.

- **Ignition and fuse.** A pulse sets `ignited`, which enables the jet and starts `fuse`. Signal routing is same-tick. When `burst_delay` has elapsed the timer publishes one activation: `ActivationOut` pulses and the burst art appears, wherever the body is. The burst applies no force and spends no energy: it is the "authored visual/event release" of the requirement.
- **Burn at defaults, vertical, free.** The force is `thrust` × (1 − v/16) and the store pays force × 16 m/s. dv/dt = a₀ − k·v with a₀ = 4/0.2 − 9.81 = 10.19 m/s² and k = 4/(0.2 × 16) = 1.25 /s: v(t) = 8.15 × (1 − e^(−1.25 t)), h(t) = 8.15 × (t − (1 − e^(−1.25 t))/1.25), E(t) = 64·t − 4·h(t). At t = 0.59 s: v = 4.3 m/s, h = 1.4 m, E = 37.8 − 5.6 = 32 J. Coast 4.3²/19.62 = 0.9 m in 0.43 s; apex about 2.3 m at about 1.0 s, which is the default `burst_delay`.
- **Extremes.** `thrust` 6 N, `charge` 80 J: a₀ = 20.2 m/s², k = 1.88 /s, terminal 10.8 m/s; E(t) = 96·t − 6·h(t) reaches 80 J near t = 1.5 s with h = 10.8 m and v = 10.2 m/s; apex about 16 m. Speed never passes the 16 m/s flow speed, under the 64 m/s linear envelope (`CuriousContraptions.Abstractions/CanonicalBodyValues.cs:16`). `thrust` under the 1.96 N weight never lifts it (at 1 N the store drains at 16 W, in 2 s); the fuse still runs. In a Vacuum atmosphere there is no thrust and no debit; the fuse still runs.
- **Exhaust.** The 0.6 m cone (terminal radius 0.15 m) pushes a body directly below the nozzle with its share of the rated force; no acceptance row depends on the exhaust reaching a body. Candidates and occlusion come from ENGINE-SPATIAL-QUERIES.
- **Spent.** The store empties and stays empty; the timer completes with the Latch policy and neither restarts nor re-emits on a later input; the memory is already set. A second pulse changes nothing until Reset.
- **Fit and tunnelling.** 0.24 m across the head and 0.30 m across the fins: it passes the pipe bore (1.3 m across) and is not a Cannon payload. At most 16 m/s is 0.033 m per substep against a 0.08 m tube radius; the engine's fattened bounds and speculative contacts cover it (`docs/gpu-f32-physics.md:195`, `:258`).

## 3. Engine capabilities

Map row families (navigation only, D14): ChemicalReaction, FiniteLedger, FiniteWorkActuation, RigidBodyDynamics, SignalPropagation, StateTransaction, TimedCommand.

| Capability | Status | Evidence |
| --- | --- | --- |
| Finite store, full at Run, paying a conical airflow source until empty | present | `AirflowRuntimeTests.FiniteConeTransfersRatedForceAccordingToMass` and `.DifferentFlowSpeedsShareOneFiniteStoreInJoules` (`CuriousContraptions.tests/AirflowRuntimeTests.cs:52`, `:82`); `FanIntegrationTests.ActualHubDoesNotOccludeThePaidConeButOffAndVacuumDoNotTransfer` (`CuriousContraptions.tests/FanIntegrationTests.cs:514`) |
| A timer that completes once and ignores busy and later inputs | present | `ActivationTimerTests.BusyAndFinishedInputsDoNotRestartOrReemit` (`CuriousContraptions.tests/ActivationTimerTests.cs:73`) |
| Declared mass frame independent of several colliders | present | `CompoundMassAndRollingTests.CollisionChildrenDoNotChangeDeclaredFreeFlight` (`CuriousContraptions.tests/CompoundMassAndRollingTests.cs:283`) |
| Jet enabled by a signal memory set from `ActivationIn` | present | declared by the Fan (`elements/Fan/FanElementDeclaration.cs:53`, `:69`–`:71`); `FanGuidanceTests.ColdStartRequiresTheConnectedSwitchOccurrenceBeforeAnyTransfer` (`CuriousContraptions.tests/FanGuidanceTests.cs:63`) |
| A memory node's output starting a timer inside one package | present, untested | `SignalEdge` (`SignalContractValues.cs:16`); no delivered package declares this edge |
| A dynamic source body receives the paired force and moment of what its jet delivers to an exposed receiver | present | `AirflowRuntimeTests.MovingNozzleReceivesPairedForceAndMoment` (`CuriousContraptions.tests/AirflowGeneralityTests.cs:189`) |
| An airflow source, store and signal nodes declared on a **dynamic** package | present, untested | the law scene's nozzle is dynamic (`AirflowGeneralityTests.cs:192`); every delivered source sits on a static base |
| Art shown from the committed timer phase | present, untested | `PresentationDriver.CommittedTimerPhase` (`CuriousContraptions.Abstractions/ElementPresentation.cs:18`–`:37`); no test body opened |
| **A dynamic source body pushed by its jet when nothing is in the cone (ambient reaction)** | missing | as [EL-092 section 3](EL-092-toy-rocket.md#3-engine-capabilities). Owner: **ENGINE-REACTION-THRUST** (`engine-reaction-thrust`); its text is in [EL-090 section 3](EL-090-steerable-blimp.md#3-engine-capabilities) and the Batch L summary |

Counts: 5 present, 3 present-untested, 0 narrower, 1 missing. Nothing needs element-specific engine code. No blast capability is used.

## 4. Sources and legacy

- Requirement: "Finite abstract toy charge creates bounded flight and an authored visual/event release"; outcome "Spent charge does not retrigger; no real pyrotechnic instructions" ([element-104](../requirements.md#element-104)).
- No legacy firework exists (`git grep -i` for "firework", "pyro" at `a6c914e` finds nothing).

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| L1 | A presentation pulse defers until visible | `parts/ClockPart.cs@a6c914e:L35-L39` | carry forward as a presentation rule of the animation worker; the burst art here is a held state, so nothing is lost off-screen |
| L2 | A store debit never exceeds stored energy | `engine/physics/PhysicsEnergyStore.cs@a6c914e:L47-L52` | carry forward |

**Files harvested:** `parts/ClockPart.cs`, `engine/physics/PhysicsEnergyStore.cs`.

## 5. Acceptance outline

Requirement row: [element-104](../requirements.md#element-104). Planned test class `ToyFireworkTests`. Heights are measured from the start pose.

| # | Given / When / Then | Planned test | Control | Boundary and tolerance |
| --- | --- | --- | --- | --- |
| A1 | Given an upright firework and a pulse, then it climbs, coasts and falls | `APulseStartsABoundedFlight` | A3 | apex 1.6–3.1 m (hand 2.3 m; clears a 1.5 m wall, stays under a 4 m ceiling); 6 s guard |
| A2 | Given the same pulse and a Signal lamp on `ActivationOut`, then one burst occurrence is published `burst_delay` after ignition and the lamp lights | `TheBurstIsOneTimedEvent` | A3 | 1.0 s ± 0.05 s (an authored duration: this part is a timer); exactly one occurrence |
| A3 | Given no pulse, then no flight and no burst | `WithoutAPulseNothingHappens` | A1 | displacement under 0.02 m and zero occurrences over 3 s |
| A4 | Given a burst, when a second pulse arrives, then no thrust and no second burst | `ASpentFireworkDoesNotRetrigger` | A2 | occurrence count stays 1; store stays at most 0.01 J; no upward speed gain |
| A5 | Given a Tennis ball resting 0.4 m beside the firework's burst point, then the burst does not move it | `TheBurstAddsNoMomentum` | — | ball speed under 0.01 m/s across the burst tick |
| A6 | Given a Wall 0.5 m above the head, then the climb stops by contact and the burst still fires on time | `ABlockedFireworkStillBurstsOnTime` | A2 | as A2; head overlap at most 0.02 m |
| A7 | Given `thrust` 1 N, then it never lifts and the burst still fires | `ThrustBelowWeightDoesNotLift` | A1 | rise under 0.05 m |
| A8 | Given a burn, then the store's debit is never less than the gain in kinetic plus potential energy | `TheStorePaysForTheFlight` | — | gain ≤ debit at every committed sample |
| A9 | Given `burst_delay` 0.1 s or `charge` 100 J, then the edit is refused | `OutOfRangeValuesAreRefused` | defaults accepted | typed refusal |
| A10 | Reset restores pose, a full store, a clear latch and an idle timer; save and load keep parameters and both wires | `ResetAndSaveLoadRestoreTheFirework` | — | also `CatalogueIntegrationTests.EveryAdmittedDeclarationRetainsAuthoredStateAcrossRunResetSaveLoad` (`CuriousContraptions.tests/CatalogueIntegrationTests.cs:273`) |
| A11 | A Battery `Supply` offered to `ActivationIn`, or `ActivationOut` offered to another output, is refused | covered generically by `BundlePortsTests.AWrongDomainOrOutputToOutputLinkIsStillRefused` (`CuriousContraptions.tests/BundlePortsTests.cs:262`) | Signal pairs are accepted | typed refusal |

### Integration

- Ignited by CAT-063 Impact switch, CAT-023 Domino, CAT-017 Clock (delivered) or [EL-094](EL-094-detonation-plunger.md). Its burst pulse drives the delivered Signal lamp, Counter, State latch and Cannon.
- Campaign: no ledger row names the firework. It falls under chapter 10's "toy rocket/ignition/destruction variants" (`docs/planning/requirements.md:2295`); owner decision 24c gives it new level **96a** *Fuse Box* (chapter 10): introduction 96a, practice 99, reuse 148 ([campaign coverage additions](../requirements.md#campaign-coverage-additions); brief in [campaign coverage](readiness/campaign-coverage.md#4-lesson-briefs)).
- A level that fixes a firework as authored scenery needs S8.

### Delivery

- **Shape.** Single element slice after EL-092.
- **Prerequisites, in order.** The engine programme (owner decision 3a), of which this part leans on ENGINE-SPATIAL-QUERIES and ENGINE-ISLANDS-SLEEP; then ENGINE-REACTION-THRUST; then EL-092 (same jet, proven first); then S7 (section 7). S3 optional. Port roles, node arity, target masks and mouth identities are written in the form ENGINE-CAPABILITY-INSTANCES leaves; the names here are today's.
- **Files.** `elements/ToyFirework/` (project, declaration, `toy_firework.tscn`, `icon.svg`); `CuriousContraptions.tests/ToyFireworkTests.cs`; one `CuriousContraptions.slnx` line; until S3, the trim list and placement switch.
- **Review tier.** Engine tier (three present-untested rows).
- **Disqualifiers the probe re-checks.** A memory output that cannot start a timer inside one package; a timer that restarts on a second input; an airflow source refused on a dynamic base.

## 6. Open questions

All resolved.

1. **The burst is not an optical source** (D19): no requirement or lesson uses it; it can gain a declared emitter under ENGINE-OPTICAL when one does.
2. **The burst pushes nothing** (decided now): the requirement asks for a visual or event release; an impulse is EL-093's behaviour, and leaving it out removes the blast dependency.
3. **Burst at a fixed delay after ignition**, not at the apex (D10, D19): a timer node is delivered; an apex detector is not.
4. **One burst colour, gold** (D13): the earlier colour choice is dropped because no row names it.

Needs the owner: none.

## Engine conformance (22 November 2026)

Checked against the owner addendum of 22 November 2026 (AGENTS.md element-free engine gate and Tri-Graph rule; `_bmad/custom/element-free-engine-design.md`; `docs/gpu-f32-physics.md`).

1. **Declaration data only.** One body, one store, one jet, a memory node, a timer node, two ports, four parameters and bindings. No flight or burst code.
2. **Generic laws.** Flight is the ambient reaction of a declared jet's own rated force (one element-free story); the burst is the signal network's timer elapsing; "spent" is an empty store and a completed latch-policy timer.
3. **Three layers.** Colliders: cylinder, sphere, two boxes on the one body. Visuals: tube, head and fins follow the body pose. Animation: plume, burst and head colour read committed flow speed and timer phase and write nothing back.
4. **Flow and typing.** Two typed Signal ports; the timer policies are enums; durations are seconds.
5. **Flat state.** Bodies 1 (dynamic); colliders 4 (cylinder 1, sphere 1, boxes 2); joints 0; drives 0; airflow sources 1 (declaring the ambient reaction); sensors 0; signal nodes 2 with 1 edge; stores 1; presentation bindings 5.
6. **Performance.** Four primitives on one body; a 0.6 m bounded cone. No sleeping exists today: resting cost is owned by ENGINE-ISLANDS-SLEEP, and the airflow pass that scales with the scene by ENGINE-SPATIAL-QUERIES (owner decision 3a). This part does not design around either.
7. **Tests are scenarios.** Every row runs the production simulation.

**Verdict: conforms** (one general capability missing, with one owner; the audit fixes of 22 November 2026 are applied).

## 7. Coverage binding (planned, not yet live)

Published main binds a named-identity package through a final section of its spec headed "Package source binding", read by `tools/Coverage/NamedPackageSources.cs` (pattern: EL-239 Baseball). The section is not written here: the reader parses every spec carrying that heading and fails the whole run when a field does not resolve. **Blocked on the coverage follow-on story S7.**

| Field | Planned value |
| --- | --- |
| Package | `toy_firework` |
| Named identity | EL-104, `../invest/named-elements.md#element-104` |
| Acceptance | `todo-227`, `sequence-task-657` |
| Decision | S377 |
| Mode records | `Configuration=Fixed` |

What it lacks today: `docs/planning/invest/scope-corrections.md` has no `s377` block; `todo-227` is a scope index shared by thirteen identities (`docs/planning/requirements.md:6389`–`:6391`) and is not co-anchored with this identity's own task, `sequence-task-657`, which sits beside `element-104` (`docs/planning/requirements.md:5229`).
