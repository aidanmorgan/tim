# EL-075 · Toy pulse emitter — named-identity spec

**Current delivery (24 October 2026):** [EL-075 delivery slice](../invest/named-elements.md#element-075). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

**Build-ready revision, 22 November 2026 (checkout `8d1d68aa`), repaired the same day against the independent audit (`docs/planning/elements/readiness/audits/elements-batch-l.md`) and the owner decisions of 22 November 2026.** A `path:line` citation without a commit is in this checkout, and `path@a6c914e:L…` is the legacy harvest. Values marked **starting value** are adopted under decision D1 of [delivery batches](../invest/delivery-batches.md#3-family-level-decisions). Nothing was simulated. Game values are canonical f32.

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name / type | EL-075 · Toy pulse emitter · Specialist |
| Anchor | [requirements.md#element-075](../requirements.md#element-075); [named entry](../invest/named-elements.md#element-075); scope index [todo-214](../requirements.md#todo-214) (shared with TH-34); [sequence-task-628](../requirements.md#sequence-task-628) |
| Proof owner | S645 |
| Disposition | To deliver as a single element. Not a duplicate of the Laser ([CAT-036](CAT-036-laser.md)), which lights continuously once enabled: this part emits a counted burst. The ledger retains CAT-036 acceptance for the shared optical emission. |
| Planned package | id `pulse_emitter`, directory `elements/PulseEmitter/`, class `PulseEmitterElementDeclaration` |
| Refines / extends | CAT-036 Laser (optical source with separate supply and control). Signal composition of the Repeating clock (`elements/SystemClock/SystemClockElementDeclaration.cs:10`), the Counter (`elements/Counter/CounterElementDeclaration.cs:9`) and the Hold timer (`elements/HoldTimer/HoldTimerElementDeclaration.cs:10`) |
| Status | not started; blocked on the engine programme (owner decision 3a), then ENGINE-SIGNAL-NODES (oscillator enable row) and ENGINE-OPTICAL, then S7 |

## 2. Declaration

The package implements `ISignalElement` (`CuriousContraptions.Abstractions.Signal/SignalContracts.cs:8`), `ISupplyElement` (`CuriousContraptions.Abstractions.Electrical/ElectricalContracts.cs:11`), `ISceneGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`) and the emitter contract that ENGINE-OPTICAL introduces. `KinematicMode.Static`, footprint 1 body, nothing moving.

The burst is not coded anywhere. It is three delivered signal nodes and a small supply graph: an oscillator ticks while the control is held and supply is present; a pulse-count node stops it at the authored number; a rearming timer holds the light on for each pulse.

### Body and colliders (one static body)

| Collider | Shape and size (m) | Centre (m) |
| --- | --- | --- |
| housing | box 0.9 × 0.6 × 0.6 | (0, 0.40, 0) |
| foot | box 1.0 × 0.1 × 0.7 | (0, 0.05, 0) |
| lens collar | cylinder radius 0.2, half-length 0.05, axis X | (0.50, 0.40, 0) |
| two sockets | spheres radius 0.075 | at the two port positions |

Material: restitution 1, friction 0.3, rolling resistance 0, bounce threshold 0.1 m/s (the Clock's housing form). The beam leaves the collar's centre at (0.55, 0.40, 0) along +X.

### Ports

| `PortId` | Domain | Direction | Role | Label, glyph | Local position |
| --- | --- | --- | --- | --- | --- |
| `PowerIn` | Electrical | Input | `PowerIn` | Power | (−0.45, 0.40, 0), normal −X |
| `FireIn` | Electrical | Input | `FirstIn` | "Fire", First | (0, 0.70, 0), normal +Y |

The control is an electrical level, as the Linear pusher's commands are (`elements/LinearPusher/LinearPusherElementDeclaration.cs:52`–`:66`): a player wires a Battery to `PowerIn` and any switched supply (Impact switch, State latch, Pressure plate, Counter, Hold timer, a gate) to `FireIn`. "Held input" then has a plain meaning. There is no output port: the pulses are light. The role `FirstIn` and the two-input gates below are today's shapes (see Delivery, Form).

### Parameters

| Key | Type, unit | Minimum | Maximum | Default | Editor policy | Reason |
| --- | --- | --- | --- | --- | --- | --- |
| `pulse_count` | WholeNumber, none | 1 | 9 | 3 | Reject | the Counter's range, so a Counter can confirm a burst |
| `interval_seconds` | Real, second | 0.2 | 12 | 1.0 | Clamp | the Clock's default and upper bound (`SystemClockElementDeclaration.cs:32`); the floor is above the longest flash |
| `pulse_seconds` | Real, second | 0.05 | 0.15 | 0.1 | Reject | 6 to 18 ticks at 120 Hz, default 12, so a receiver reading once per tick sees every flash |

The ranges cannot overlap (0.15 < 0.2), so no cross-parameter rule is needed. All are Build-only; durations are seconds of game time (D10).

### Signal nodes and edges

| Node | Descriptor | Wiring |
| --- | --- | --- |
| `osc` | `SignalOscillatorDescriptor`, interval `interval_seconds` (`CuriousContraptions.Abstractions/SignalContractValues.cs:61`) | enable: the internal supply node `burst` (the ENGINE-SIGNAL-NODES row in section 3) |
| `count` | `SignalPulseCountDescriptor`, target `pulse_count` (`SignalContractValues.cs:63`) | input from `osc` output |
| `flash` | `SignalTimerDescriptor`, duration `pulse_seconds`, `TimerCompletionPolicy.Rearm`, `TimerBoundary.BeforeNetworks`, `TimerElapsedSignal.None` (`SignalContractValues.cs:49`; the Hold timer's form, `HoldTimerElementDeclaration.cs:33`) | input from `osc` output |

### Supply graph

| Node | Descriptor | Fed by |
| --- | --- | --- |
| `power` | logic `Nor` with no conditions: a pass-through that fans out, the pusher's form (`LinearPusherElementDeclaration.cs:53`) | port `PowerIn` |
| `fire` | the same pass-through | port `FireIn` |
| `done` | contact closed while `count` is `SignalContactState.Latched` (the Counter's control, `CounterElementDeclaration.cs:88`) | `fire` |
| `gate` | logic `LogicGateOperation.Xor`: energy from `power`, first condition `fire`, second condition `done` | `power`, `fire`, `done` |
| `burst` | consumer with no targets; its supplied state enables `osc` | `gate` |
| `window` | contact closed while `flash` is `SignalContactState.TimerCounting` (the Hold timer's control, `HoldTimerElementDeclaration.cs:40`) | `power` |
| `emitter` | consumer whose target is the declared optical emitter | `window` |

`done` is powered only while the control is held and the count is complete, so `gate` is on exactly when supply is present, the control is held and the count is not complete. The audit checked this mechanism and found it sound. Electrical node `emitter`: `ElectricalRole.Consumer`, 12 V, 10 Ω, 0 F, `MaxPower` 50 W (the Clock's consumer values, `SystemClockElementDeclaration.cs:71`): a declared rating, read by nothing; no debit is asserted. The emitter is not a drive, so ENGINE-SUPPLY-DEMAND debits nothing for it; it is lit or dark with its route. No stores.

### Optical emitter

One emitter at (0.55, 0.40, 0), direction +X, no divergence, range 16 m, channel power (1.00, 0.78, 0.32), the Laser's amber, which carries all three ordinary colour channels (D18). Units, range and the receiver model are ENGINE-OPTICAL's and are taken from it unchanged: there is one receiver law for all light (owner decision 9, 22 November 2026), and this part shares the one receiver model that Batch K's optical parts use.

### Metadata and bindings

Category `Control`, display name "Toy pulse emitter", palette order 435 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)), colour (0.84, 0.61, 0.28).

| Target node | Property | Driver | Values |
| --- | --- | --- | --- |
| `Visual/Lens` | emission, three channels | `PresentationDriver.CommittedTimerActive` on `flash` | off to `#fff0a5` |
| `Visual/Pip_0` … `Pip_8` | albedo, three channels each | `CommittedCount` on `count`, threshold 1 … 9 | slate `#556573` to gold `#f7cb52` |

The beam itself is drawn by ENGINE-OPTICAL's shared beam presentation from the committed optical state.

### Art and solidity

| Node | Mesh | Class | Moved by |
| --- | --- | --- | --- |
| `Visual/Housing`, `Visual/Foot` | ochre `#d69c47` box, navy `#293954` foot | solid, box colliders | nothing |
| `Visual/Collar` | cream `#fff8e9` ring, the collider's size | solid, cylinder collider | nothing |
| `Visual/Lens` | slate disc set flush inside the collar | decorative: enclosed and flush; glow by binding | nothing |
| `Visual/Pip_0` … `Pip_8` | nine discs printed in a row on the top face | decorative: printed flat | colour by binding |
| `Visual/Sockets` | gold studs | solid, sphere colliders | nothing |

Icon: the housing in profile with three short dashes leaving the lens. It differs from the Laser (one unbroken line) by the dashes and the row of pips.

### Behaviour and numbers

Laws: the oscillator, pulse-count and rearming-timer node rules of the signal network; the supply-network rule with one Xor and two state contacts; ENGINE-OPTICAL emission while the `emitter` consumer is supplied.

**The explicit retrigger rule.** One burst of at most `pulse_count` flashes per Run. While supply is present and the control is held, a flash starts at each full interval. Holding the control longer adds none. Releasing it stops the clock and loses the part-elapsed interval; holding it again resumes toward the same total. After the last flash nothing more is emitted until Reset.

**Default timeline** (count 3, interval 1.0 s, flash 0.1 s), control and supply both present from t = 0: flashes start at 1.0, 2.0 and 3.0 s and each lasts 0.1 s. The third pulse completes the count, which closes `done`, turns `gate` off and stops the oscillator; the third flash still runs its 0.1 s because `window` is fed from `power`, not from `gate`. Total light time 0.3 s.

**Unpowered request.** Without supply `gate` has no energy, the oscillator does not run and the count does not move. A control pressed and released while unpowered leaves no trace, so supply connected later emits nothing. A control still held when supply arrives starts the burst then: the request is present, not remembered.

**Tick order.** Supply is solved in the electrical phase; the oscillator's pulse is routed to `count` and `flash` in the tick it occurs (signal routing is same-tick); the count's latch reaches `gate` through the supply network. The optical solve runs after the last substep of the tick and receivers' readings act from the next tick; a receiver couples to other parts only as observation, then signal node, then signal-state contact (coordinator decision 16, 22 November 2026; D7). The spacing of flashes is a timing feature of this part and is asserted; the one-tick offsets inside a tick are not.

## 3. Engine capabilities

Map row families: ElectricalPower, FiniteLedger, GeometryQuery, OpticalTransport, SignalPropagation, StateTransaction (navigation only, D14).

| Capability | Status | Evidence |
| --- | --- | --- |
| Oscillator that runs only while its supply is effectively powered and restarts a fresh interval | present | `SignalContractValues.cs:61`; `ComposableSignalNodesTests.EffectiveContactAndGatePowerStopBothOscillatorsWithTheBatteryStillEnabled` (`CuriousContraptions.tests/ComposableSignalNodesTests.cs:645`) |
| Oscillator pulses routed inside one package to a pulse-count node that latches at its target | present | `ComposableSignalNodesTests.IndependentNodesRouteCountAndResetThroughProduction` (`ComposableSignalNodesTests.cs:67`); `CounterTests.ActivationNetwork_CountingProgression_SingleEmission_AndSaturation` (`CuriousContraptions.tests/CounterTests.cs:240`) |
| Rearming timer that closes a supply contact for exactly its duration | present | `HoldTimerTests.PhysicalTriggerHoldsExactWindowWithoutGeneratingSupply` (`CuriousContraptions.tests/HoldTimerTests.cs:88`) |
| Supply Xor with separate energy and condition inputs | present | `ElectricalXorTests.SwitchedConditionsAndSeparateSupplyDriveTheActualConsumer` (`CuriousContraptions.tests/ElectricalXorTests.cs:59`) |
| Package-internal supply edges | present | `ComposableSupplyRoutingTests.PackageInternalSupplyEdgesAdmittedByValidationAndCompileToTopology` (`CuriousContraptions.tests/ComposableSupplyRoutingTests.cs:499`) |
| Count-threshold bindings | present | `DeclaredPresentationTests.ClockTicksDrivePendulumPulseAndCounterLamps` (`CuriousContraptions.tests/DeclaredPresentationTests.cs:237`) |
| An oscillator's enable, and a cycle through an oscillator | narrower | the enable must be a public Electrical input port with role `PowerIn` (`CuriousContraptions.Bundle/SignalBundleValidation.cs:162`–`:167`), and a wire loop through a Xor is refused as `NonmonotoneFeedback` (`ElectricalXorTests.ZeroDelayFeedbackThroughTheGateIsRefusedAtomicallyBeforeAnySolve`, `ElectricalXorTests.cs:263`). Owner: ENGINE-SIGNAL-NODES (owner decision 3a and coordinator decision 16), with the audit's row E1: "An oscillator's enable is a declared reference to either a public `PowerIn` port or a supply node of the package's own graph; a cycle that passes through an oscillator (at least one interval) is admitted; zero-delay non-monotone feedback stays refused." |
| Optical emitter, lit while a declared supply consumer is powered; transport, occlusion, reception and beam presentation | **missing** | [ENGINE-OPTICAL](../../../_bmad-output/planning-artifacts/epics.md#story-engine-optical-beam-emission-transport-surface-interaction-and-reception), taking its occlusion candidates from ENGINE-SPATIAL-QUERIES. Its text does not yet say that an emitter is a supply target; proposed addition in the batch summary |

## 4. Sources and legacy

- **Requirement row.** [element-075](../requirements.md#element-075): separate supply and control emit an authored bounded number of pulses; an unpowered request emits none; held input follows the explicit retrigger rule.
- **Research reference.** The article cited by [todo-214](../requirements.md#todo-214); intent, not constants.
- **Legacy search.** `git grep -i` at a6c914e for "pulse emitter", "emitter", "burst" and "pulse" found no pulse-emitter source.

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| L1 | Clock interval is finite and within 0.1–12 s | `parts/ClockPart.cs@a6c914e:L46-L50` | Carry forward the upper bound; the floor here is 0.2 s |
| L2 | Clock: loss of power resets timing; reconnecting begins a fresh interval | `parts/catalog/clock.tres@a6c914e:L11-L11` | Carry forward: it is the delivered oscillator rule and gives the release-and-hold-again behaviour |
| L3 | Counter target is an integer 1–9 | `parts/CounterPart.cs@a6c914e:L39-L41` | Carry forward as the `pulse_count` range |
| L4 | Laser: emission only when enabled and `PowerIn` is powered | `parts/LaserPart.cs@a6c914e:L36-L38` | Carry forward the rule. The laser's Activation trigger is not carried: this control is an electrical level |

**Files harvested:** `parts/ClockPart.cs`, `parts/catalog/clock.tres`, `parts/CounterPart.cs`.

## 5. Acceptance outline

Requirement row: [element-075](../requirements.md#element-075). Planned test class `PulseEmitterTests`. A "flash" is one continuous run of ticks in which the committed emitter state is lit; flashes are counted from that read and, where a receiver is used, from the receiver's committed readings.

| # | Given / When / Then | Planned test | Control | Boundary and tolerance |
| --- | --- | --- | --- | --- |
| A1 | Given a Battery on `PowerIn` and a closed switched supply on `FireIn`, when Run proceeds, then exactly three flashes occur, a receiver 4 m along +X reads three, and nothing more is emitted in the next 10 s | `OneHeldRequestEmitsExactlyTheAuthoredCount` | A2 | starts one interval apart within 1 tick; each flash 12 ticks within 1 |
| A2 | Given the control held and no supply (no battery, disabled, unwired), then no flash occurs and the count stays 0 | `WithoutSupplyNoPulseIsEmitted` (theory) | A1 | 10 s |
| A3 | Given the control pressed and released while unpowered, when supply is then connected, then no flash occurs | `AnUnpoweredRequestIsNotRemembered` | the control held when supply arrives gives three | 10 s after supply |
| A4 | Given the control released after the first flash and held again 2 s later, then two more flashes occur, the first a full interval after the control returns, and the total is three | `ReleasingPausesAndHoldingAgainFinishesTheSameBurst` | A1 | total exactly 3 |
| A5 | Given a completed burst, when the control is released and held again, then no flash occurs; after Reset the burst is available again | `ACompletedBurstDoesNotRepeatUntilReset` | A1 | 10 s |
| A6 | Given a Wall across the beam, then the receiver reads nothing while the emitter still flashes three times and three pips light | `AnOccludedBeamStillCountsItsFlashes` | A1 | — |
| A7 | `pulse_count` 1 and 9 give 1 and 9 flashes; `interval_seconds` 0.2 and `pulse_seconds` 0.15 still give separate flashes | `TheBoundsEmitSeparateCountedFlashes` (theory) | — | at least 5 dark ticks between flashes at the tightest setting |
| A8 | Out-of-range, non-finite and non-whole values are refused at every seam; an interval typed out of range in the editor is clamped | `OutOfRangeParametersAreRefused` (theory) | the bounds are admitted | exact |
| A9 | A Signal output offered to `FireIn` or `PowerIn` is refused | `ASignalOutputCannotFireTheEmitter` | an electrical `Supply` is accepted | typed `InvalidConnection`; S4 covers it generically when it lands |
| A10 | Reset clears the count, the timer and the pips; save and load keep the three parameters and both wires | `ResetAndSaveLoadRestoreTheEmitter` | — | generic row: `CatalogueIntegrationTests.EveryAdmittedDeclarationRetainsAuthoredStateAcrossRunResetSaveLoad` (`CuriousContraptions.tests/CatalogueIntegrationTests.cs:273`) |

Run length (owner decision 13): the default 30 s covers every row at the default interval. A level that teaches `pulse_count` 9 at intervals above 3 s declares a Run length of at least 9 × interval + 5 s; at the 12 s maximum that is 113 s, subject to the maximum ENGINE-RUN-LENGTH sets.

### Integration

- Supply and control sources: Battery and every delivered switched supply. Observers: the optical receivers of Epic 13 (not delivered; they arrive with or after ENGINE-OPTICAL) and, through them, a Counter.
- TH-34 shares the scope index and proves its own interaction.
- Campaign: chapter 10 "Oddly Satisfying", levels 91–100, "phazer-like toy pulse source" (`docs/planning/requirements.md:2410`, chapter row `:2295`); reuse 111–120, 136–150.
- A level that fixes the emitter in place needs the lesson-fixture story S8.

### Delivery

- **Shape.** Single element slice.
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a); then ENGINE-SIGNAL-NODES (the oscillator enable row); ENGINE-OPTICAL; one optical receiver (the first Epic 13 receiver) for rows A1 and A6; then S7.
- **Files.** `elements/PulseEmitter/` (project, declaration, `pulse_emitter.tscn`, `icon.svg`); `CuriousContraptions.tests/PulseEmitterTests.cs`; one `CuriousContraptions.slnx` line; the binding section once S7 admits it; until S3, the remaining per-package lists.
- **Form.** Port roles, node arity, target masks and mouth identities are written in the form ENGINE-CAPABILITY-INSTANCES leaves; the names here are today's. This part uses the role `FirstIn`, a two-input Xor and two condition-less Nor pass-throughs; under an N-input boolean node the same graph is one node computing "power and fire and not done", and the declaration is rewritten to that form in the delivering slice.
- **Review tier.** Engine tier unless the two engine slices publish every row above with tests; then data-only.
- **Disqualifiers the probe re-checks.** Whether a count that latches in a tick opens `gate` before the next oscillator sample (if it does not, the worst case is one extra pulse, and A1 fails visibly); whether ENGINE-SIGNAL-NODES admits the cycle through `osc` as its row E1 says.

## 6. Open questions

All resolved.

1. **Pulse medium: light.** The ledger keeps CAT-036 acceptance and the map row names optical transport, so the pulses are optical; a signal-only burst would drop a retained criterion. Channel content is the Laser's amber (D18).
2. **Retrigger rule:** one burst per Run, as stated in section 2. Decided under the standing delegation: it needs no edge detector and no count reset, it bounds the total at `pulse_count`, and every other latching part also waits for Reset. A repeatable burst can be added when ENGINE-SIGNAL-NODES delivers its count-reset row and a lesson asks for it (D19).
3. **Supply loss mid-burst** pauses the burst; the count is kept and the part-elapsed interval is lost. This is the delivered oscillator rule.
4. **The control is electrical, not a Signal pulse.** A pulse request accepted without supply would be remembered and fired later, against the requirement; a level cannot be.
5. **Values** are starting values (D1); `pulse_power` is not a parameter (D13).

Needs the owner: none.

## Engine conformance (22 November 2026)

Checked against the owner addendum of 22 November 2026 (AGENTS.md element-free engine gate and Tri-Graph rule; `_bmad/custom/element-free-engine-design.md`; `docs/gpu-f32-physics.md`).

1. **Declaration data only.** One static body, three signal nodes, seven supply nodes, one optical emitter, two ports, three parameters and presentation bindings. No burst routine, timer callback or state machine in code; the only memory is the declared count node.
2. **Generic laws.** Oscillator, pulse-count and rearming-timer node rules; the supply network with one Xor; optical emission while supplied. The narrower and the missing capability are stated element-free, each with one owner (ENGINE-SIGNAL-NODES, ENGINE-OPTICAL).
3. **Three layers.** Colliders: two boxes, a cylinder and two spheres. Visuals: one mesh per collider; lens and pips flush or printed. Animation: lens glow from the committed timer state and pip colours from the committed count; the beam is the optical engine's shared presentation. None writes back.
4. **Flow and typing.** Typed Electrical inputs; `LogicGateOperation`, `SignalContactState`, `TimerCompletionPolicy` and `TimerBoundary` are enums; the count is a WholeNumber; durations are seconds.
5. **Flat state.** Bodies 1 (static); colliders 5; joints 0; drives 0; sensors 0; domain nodes: 3 signal, 7 supply, 1 electrical, 1 optical emitter; stores 0; presentation bindings 30 (three lens channels, twenty-seven pip channels).
6. **Performance.** Five static colliders. One emitter with no divergence is one ray per tick while lit, found through ENGINE-SPATIAL-QUERIES within the bounce and segment bounds ENGINE-OPTICAL declares. Nothing scales with the scene.
7. **Tests are scenarios.** Every row runs the production simulation with delivered supplies and an optical receiver.

**Verdict: conforms** (two general capabilities outstanding, each with one owner).

## 7. Coverage binding (planned, not yet live)

Published main binds a named-identity package through a final section of the identity's spec headed "Package source binding", read by `tools/Coverage/NamedPackageSources.cs` (pattern: EL-239 Baseball). The section is not written here: the reader parses every spec that carries that heading and fails the whole coverage run when a field does not resolve. **Blocked on the coverage follow-on story S7.**

| Field | Planned value |
| --- | --- |
| Package | `pulse_emitter` |
| Named identity | EL-075, `../invest/named-elements.md#element-075` |
| Acceptance | `todo-214`, `sequence-task-628` (`docs/planning/requirements.md:4997`) |
| Decision | S645, `../invest/scope-corrections.md#s645` |
| Mode records | `Configuration=Fixed` |

What it lacks today: `scope-corrections.md` has no `s645` block; `todo-214` is a scope index shared with TH-34, and `sequence-task-628` is anchored beside the element row, not beside that todo, which is the pairing the reader expects.
