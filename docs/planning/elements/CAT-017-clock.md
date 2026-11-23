# CAT-017 · clock — declaration readiness spec

**Current delivery (24 October 2026):** [CAT-017 delivery owner](../../../_bmad-output/planning-artifacts/epics.md#catalogue-slice-ownership). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Story 7.0 Batch B declaration spec (CAT-017-D). Legacy citations are pinned to `a6c914e` and resolve with `git show a6c914e:<path> | sed -n 'start,endp'`. Requirement row: [CAT-017](../requirements.md#current-cat-017). Supplied-network facts (N1–N24) are in [CAT-005](CAT-005-battery.md#4-legacy-harvest).

**Colliders (owner, 25 October 2026, "visible = solid"):** every part a player sees as solid collides with its true shape. Since [`elements-visible-solid-sweep`](../../../TODO.md) the parts that stand proud of the housing collide with their drawn primitives, after the housing and foot boxes: the cream face panel as a 1.14 × 1.44 × 0.04 box at z 0.32, the indicator as a sphere r 0.065 at (0, 0.63, 0.39), the two sockets as spheres r 0.075 at (∓0.78, 0, 0), and the pendulum recess, 2 cm proud of the face, as a 0.84 × 1.1 × 0.02 box at (0, −0.06, 0.35) (`elements/SystemClock/clock.tscn`; `VisibleSolidColliderTests` checks them against the art and drops a ball onto the face; `ClockTests`). The pendulum swings with the clock and does not collide. Placement bounds every collider (`PlacementPose`), so the sockets now count toward the bench edge: the clock reaches x ±0.855 against the foot's ±0.8, and z +0.495 (the pivot cap; derived from the collider list, not from a test) against the foot's +0.425. The pivot cap (sphere r 0.055 at (0, 0.4, 0.44)) collides since `engine-multi-collider-dynamic` (published `28a2d302`), through the hidden proxy `col_pivotcap` (`elements/SystemClock/clock.tscn:174–177`); it is the clock's eighth collider.

## 1. Identity

| Item | Value |
| --- | --- |
| CAT ID / kind | CAT-017 · `clock` ("Repeating clock") |
| Requirement anchor | [current-cat-017](../requirements.md#current-cat-017); retained behaviour [todo-273](../requirements.md#todo-273) |
| Mapped identities | none owned. Related but distinct: [EL-025 Tipping-bucket water clock](../invest/named-elements.md#element-025), [EL-057 Escapement](../invest/named-elements.md#element-057). No legacy implementation links them; their requirement rows stand as the source. |
| Roadmap story | 9.3 "System Clock Periodic Pulse Generator" (Epic 9) Current [CAT-017 delivery owner](../../../_bmad-output/planning-artifacts/epics.md#catalogue-slice-ownership). |
| Status | **Delivered** (22 November 2026 check): package `elements/SystemClock`, id `clock`; Story 9.3, generic instance by C-Clock (native Pass `ae3cac`). Sprint key `9-3-system-clock-periodic-pulse-generator` stays in-progress only for its Chrome clause. Residuals: [section 7](#7-delivered-state-integration-and-residuals-22-november-2026). Every residual slice is blocked on the engine programme (owner decision 3a, 22 November 2026); the part needs no specific engine story of its own. |

## 2. Declaration

The delivered declaration is `elements/SystemClock/SystemClockElementDeclaration.cs` with the scene `elements/SystemClock/clock.tscn` (read on `8d1d68aa`). It implements `ISignalElement`, `ISupplyElement` and `ISceneGeometryElement` (`:10`). The legacy sources of the carried values are in section 4.

**Body and material.** One static body with no mass and no joint (`:36`). The pendulum is art, not a body. Material: restitution 1, friction 0.3, rolling resistance 0, bounce threshold 0.1 m/s (`:49`), all f32.

**Colliders (physics graph).** Eight, baked in scene order from hidden proxies. Physics reads no art.

| # | Shape | Size (m) | Centre (m) |
|---|---|---|---|
| 0 | box | 1.4 × 1.7 × 0.6 | origin |
| 1 | box | 1.6 × 0.2 × 0.85 | (0, −0.95, 0) |
| 2 | box | 1.14 × 1.44 × 0.04 | (0, 0, 0.32) |
| 3 | sphere | r 0.065 | (0, 0.63, 0.39) |
| 4–5 | sphere | r 0.075 | (−0.78, 0, 0), (0.78, 0, 0) |
| 6 | box | 0.84 × 1.1 × 0.02 | (0, −0.06, 0.35) |
| 7 | sphere | r 0.055 | (0, 0.4, 0.44), proxy `col_pivotcap` (`clock.tscn:174–177`) |

Placement bounds every collider, so the clock reaches x ±0.855 and z +0.495 (the pivot cap) at the bench edge.

**Visuals (rendering graph).** No art mesh is used as a collider.

| Art node | Mesh and colour | Solidity | Moved by |
|---|---|---|---|
| `Visual/Housing` | box 1.4 × 1.7 × 0.6 m, ochre `#e8b764` | solid, collider 0 | nothing |
| `Visual/Foot` | box 1.6 × 0.2 × 0.85 m, navy `#293954` | solid, collider 1 | nothing |
| `Visual/Face` | box 1.14 × 1.44 × 0.04 m, cream `#fff8e9` | solid, collider 2 | nothing |
| `Visual/Visual_Indicator` | sphere r 0.065 m, slate flashing gold | solid, collider 3 | nothing (colour binding only) |
| `Visual/Visual_Socket_PowerIn`, `Visual_Socket_ActivationOut` | spheres r 0.075 m, gold | solid, colliders 4–5 | nothing |
| `Visual/Recess` | box 0.84 × 1.1 × 0.02 m, slate `#556573` | solid, collider 6 | nothing |
| `Visual/PivotCap` | sphere r 0.055 m, navy | solid, collider 7 | nothing |
| `Visual/PendulumPivot/Rod`, `Bob` | rod 0.045 × 0.7 × 0.035 m and gold bob r 0.15 m, hung from the pivot at (0, 0.4, 0.39) | decorative: moved by a binding. The bob reaches z 0.54 and takes no collider, so a ball passes through it | the pendulum binding (animation graph) |

Selection radius 1.1 (`:12`). Icon `elements/SystemClock/icon.svg`. At a glance it is the tall ochre case with a swinging pendulum in a slate recess; no other delivered part has a pendulum.

**Animation (animation graph).** Four presentation bindings (`:14–17`). They read the committed oscillator row of node `signal` and write nothing back.

| Target | Property | Driver | Values |
|---|---|---|---|
| `Visual/PendulumPivot` | rotation about Z | `CommittedOscillatorPhase` | sine mapping to ±0.42 rad, exponential follow at rate 30 |
| `Visual/Visual_Indicator` | albedo red, green, blue | `CommittedOscillatorPulse` | slate to gold per pulse, linear decay 0.25 s, overlaps combine by maximum, deferred until visible, event-timed |

**Ports** (`:40–41`).

| `PortId` | Domain | Direction | Role | Signal kind | Position (m) | Facing |
|---|---|---|---|---|---|---|
| `PowerIn` | Electrical | Input | PowerIn | — | (−0.78, 0, 0) | −X |
| `ActivationOut` | Signal | Output | ActivationOut | Pulse | (0.78, 0, 0) | +X |

**Parameter** (`:32–33`).

| Name | Kind | Unit | Minimum | Maximum | Default | Editor policy | Run edit |
|---|---|---|---|---|---|---|---|
| `interval_seconds` | Real | second | 0.1 | 12 | 1 | Clamp | Build-only |

The interval compiles to ceil(seconds × rate) whole ticks: 0.1 s gives 6, 12 or 24 ticks and 12 s gives 720, 1,440 or 2,880 ticks at 60, 120 or 240 Hz.

**Signal node, supply node and event.** One signal node `signal`, a `SignalOscillatorDescriptor` on `interval_seconds` with `PowerIn` as its supply input (`:58`); `ActivationOut` binds to its output (`:60–63`). One supply node `consumer`, a `SupplyConsumerDescriptor` on `PowerIn` (`:65–67`). Event `TriggerActivated` (`:52`). `ElectricalNodes` holds one row with the `Consumer` role (`:69–72`), which the oscillator rule requires (`SignalBundleValidation.cs:167`); its four rating values are unread (section 7). The bundle validator refuses a second node-selecting capability, an interval that is not Real seconds with a positive minimum, a missing Pulse `ActivationOut`, any `ActivationIn`, a missing `PowerIn` consumer route, and a non-static frame.

**Compiled counts per instance.** 1 static body; 8 colliders (4 boxes, 4 spheres); 0 joints; 0 drives; 0 sensors; 1 signal node with one committed memory row; 1 supply node; 0 stores; 2 ports; 4 bindings. All are fixed at compile.

**Catalogue.** Id `clock`, title "Repeating clock", category Control, palette order 23, colour (0.91, 0.72, 0.39), description "Connect electricity to start the pendulum. Sends one trigger each second, starting after a full interval. Losing power resets its timing; reconnecting begins a fresh interval." (`:21–28`). Coverage key `clock`.

## 3. Engine capabilities and conformance

Map families, for navigation only (D14): AnimationEvaluation, AnimationLifecycle, ContactImpulse, EnvironmentState, FiniteLedger, GeometryQuery, RigidBodyDynamics, SignalPropagation, SlidingFriction, TimedCommand.

**Generic laws.** The package holds no script, callback, update loop or state machine.

| Behaviour | General law | Declared values |
|---|---|---|
| A body rests on it or rebounds from it | rigid contact with the product pair law for restitution, and friction | eight colliders; the material |
| It pulses once per interval while supplied | signal network oscillator rule: a node whose supply input is available runs; newly supplied, it is due one full interval later; at its due tick it emits one pulse occurrence and is due again; unsupplied, it is stopped and due nothing. No start-up pulse and no catch-up | `interval_seconds`; the `PowerIn` supply input |
| It needs exactly one upstream source | supply network consumer rule: a consumer with two suppliers is refused | the `consumer` node |
| Pendulum and flash | presentation bindings on the committed oscillator row | the four bindings |

**Capability probe.** 6 present, 1 present but untested, 0 narrower, 0 missing.

| Capability | Status | Symbol | Test |
|---|---|---|---|
| Powered periodic source | present | `SignalOscillatorDescriptor` (`CuriousContraptions.Abstractions/SignalContractValues.cs:61`) | `ClockGuardTests.AOneSecondClockPulsesFiveTimesInFiveSecondsAtEachWholeSecondAndTheCounterCountsEach`; `ClockTests.ActivationNetwork_PowerLossAtDeadlineAndRestoration` |
| Interval in seconds compiled to whole ticks at each cadence | present | the same descriptor | `ClockGuardTests.TheCompiledIntervalIsTheCeilingOfTheF32ProductAtTheBoundaries`; `ClockGuardTests.TheCountIsTheFloorOfTheTickOverTheIntervalAtEveryCadence` |
| One upstream supply; none means stopped | present | `SupplyConsumerDescriptor` (`CuriousContraptions.Abstractions/SupplyContractValues.cs:42`) | `ClockGuardTests.TwoSourcesOnOneClockAreRefused`; `ClockGuardTests.AnUnsuppliedClockStaysStoppedAndEmitsNothing` |
| Parameter refusal | present | `ParameterDescriptor` range | `ClockGuardTests.AnIntervalOutsideItsAdmittedRangeIsRefusedAtConstruction`; `DeclaredOscillatorTests.AnIntervalOutsideItsRangeIsRefusedAtBothBoundaries` |
| Pause, Reset, save and load | present | committed oscillator row | `ClockGuardTests.APauseAddsNoPulseAndNoPhaseAndResumeContinuesTheSpacing`; `ClockGuardTests.ResetStopsTheClockAndTheNextRunPulsesFirstAtOneSecond`; `ClockGuardTests.SaveAndLoadPreserveThePulseSchedule` |
| Pendulum and flash presentation | present | `PresentationDriver.CommittedOscillatorPhase`, `CommittedOscillatorPulse` | `ClockGuardTests.ThePendulumFollowsTheCommittedPhase`; `ClockGuardTests.TheFaceFlashesOncePerCommittedPulse`; `ClockGuardTests.ResetReturnsThePendulumToRestAndTheFaceToIdle` |
| Pulse-feedback saturation (harvest C26 to C29) | present, untested | hidden catch-up bound of 64 impulses per lamp | No native test; it stays at the named qualification gate (`_bmad-output/implementation-artifacts/spec-c-clock.md:62`) |

These tests assert exact pulse ticks. That is allowed here because the element is a timer and its interval is the feature.

**Conformance (owner addendum, 22 November 2026): conforms with one fix.**
- Declaration data only: holds. A search of `engine`, `ui` and every non-test `CuriousContraptions.*` project for the package id, class and namespace found no reference (`docs/planning/elements/readiness/audits/elements-batch-b.md` §5).
- Three layers: hold. Every static proud solid has its own collider; the pendulum is moved by a binding and is decorative; the indicator's binding changes colour only.
- One-way flow, typing and flat state: hold, with the counts in section 2. The pendulum reads committed phase; no pulse or sensor depends on the drawn pendulum.
- Fix: the unread rating values of `ElectricalNodes` are a required forward-only refactor (section 7).

**Performance fitness.** The body is static and is never integrated. Eight colliders against the two of the original housing has one reason: visible = solid. The face, the recess (2 cm proud of the face), the indicator, the two sockets and the pivot cap are each drawn standing clear of the housing, so each has its own primitive. No collider is cosmetic, and the pendulum, which is cosmetic, has none. The cost for many instances is the recorded, unmeasured engine gap "Per-instance collider growth from visible = solid" (`_bmad-output/implementation-artifacts/deferred-work.md:177–179`), owned by the performance qualification gate P0-034. The oscillator costs one node row per tick. The authorities state no per-part budget, so none is claimed here.

**Dependencies.** Battery to run; a Counter to observe repeated pulses (a Signal lamp latches on the first pulse). All are delivered.

## 4. Legacy harvest

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| C1 | Powered periodic trigger: no startup pulse, no catch-up; loss of power resets phase. | `parts/ClockPart.cs@a6c914e:L8-L8` | carry forward |
| C2 | Readable state: phase, due tick, pulse count, last pulse tick, interval, interval ticks = ceil(interval / tick), progress fraction. | `parts/ClockPart.cs@a6c914e:L13-L23` | carry forward |
| C3 | Declaration: one oscillator keyed to the part, interval seconds, power input socket `PowerIn`; progress published as a scalar observation. | `parts/ClockPart.cs@a6c914e:L24-L28` | carry forward |
| C4 | Pendulum and pulse-lamp bindings as in section 2. | `parts/ClockPart.cs@a6c914e:L29-L39` | carry forward the curve data; current values stored as f32 |
| C5 | Ports `PowerIn` and `ActivationOut`; the clock can send activation. | `parts/ClockPart.cs@a6c914e:L40-L45` | carry forward |
| C6 | Interval must be finite and within 0.1–12 s, else the part is rejected. | `parts/ClockPart.cs@a6c914e:L46-L51` | carry forward |
| C7 | The clock is active exactly on ticks where it is running and its last pulse tick equals the current tick. | `parts/ClockPart.cs@a6c914e:L52-L55` | carry forward the rule; do not carry forward the per-part `PreparePhysics` update loop |
| C8 | Geometry and art as in section 2. | `parts/ClockPart.cs@a6c914e:L56-L70` | carry forward |
| C9 | Oscillator declaration requires a declared electrical input port and a finite positive interval; ticks = ceil(seconds / tick). | `engine/SceneOscillatorDeclaration.cs@a6c914e:L8-L18` | carry forward |
| C10 | Oscillator states `Stopped`/`Running`; state = (phase, due tick, pulse count, last pulse tick); initial Stopped, due −1, count 0, last −1. | `engine/SimulationOscillators.cs@a6c914e:L16-L22` and `engine/SimulationOscillators.cs@a6c914e:L54-L60` | carry forward |
| C11 | Every tick samples every power input once: unpowered → Stopped, due −1 (no pulse even at the deadline); newly powered → Running, due = tick + interval (no pulse); running at due → pulse, count + 1, last = tick, due += interval. Simultaneous pulses are emitted in identity order. **Current (since 9R.1):** the committed oscillator row keeps no last-pulse time (`ActivationMemory.Validate` requires `Last` default); a pulse's time is its occurrence's physical boundary (tick × substeps), held in the clock's latched `ActivationOut` (first pulse) and in each downstream Counter's `Last`. | `engine/SimulationOscillators.cs@a6c914e:L83-L109` | carry forward |
| C12 | Progress = 0 when stopped, else 1 − (due − tick) / interval. | `engine/SimulationOscillators.cs@a6c914e:L65-L74` | carry forward |
| C13 | Power at ticks 0–1, lost at the deadline tick 2: no pulse; restored at 4 → due 6; pulses at 6 and 8 (interval 2). | `CuriousContraptions.tests/SimulationOscillatorsTests.cs@a6c914e:L5-L21` | carry forward (acceptance fact) |
| C14 | Simultaneous pulses use identity order; snapshots replay exactly. | `CuriousContraptions.tests/SimulationOscillatorsTests.cs@a6c914e:L23-L34` | carry forward |
| C15 | Zero interval, duplicate ids and negative first tick reject; tick overflow rejects atomically for all oscillators. | `CuriousContraptions.tests/SimulationOscillatorsTests.cs@a6c914e:L36-L58` | carry forward |
| C16 | Parameter wire name is `interval_seconds`; an unknown parameter enum value rejects. | `CuriousContraptions.tests/ClockTests.cs@a6c914e:L9-L35` and `engine/MachineData.cs@a6c914e:L82-L82` | carry forward at the serialization boundary |
| C17 | For 0.1, 1 and 12 s in either entity order: after each tick, pulse count = tick / interval ticks; a clock→counter activation wire counts every pulse; the clock is active only when tick > 0 and tick is a multiple of the interval; progress stays within [0, 1]; Reset gives Stopped, count 0, due −1 with the authored interval. Battery→clock and clock→counter connect without explicit sockets. | `CuriousContraptions.tests/ClockTests.cs@a6c914e:L37-L80` | carry forward |
| C18 | No supply for 150 ticks: no pulses. Power restored at tick t: due = t + interval; no pulse until due; one pulse at due. Power loss: inactive, due −1, progress 0. | `CuriousContraptions.tests/ClockTests.cs@a6c914e:L82-L115` | carry forward |
| C19 | Intervals 0, −1, 0.01, 13, NaN and +∞ are rejected and the part is not added. | `CuriousContraptions.tests/ClockTests.cs@a6c914e:L117-L135` | carry forward |
| C20 | Pendulum follows only committed progress: paused frames ease toward the committed target (1 − e^(−3) after 0.1 s at rate 30); a failed tick leaves the target unchanged; a hidden clock does not animate and catches up from its held angle when shown; power loss sets target 0 and the pendulum eases to rest; Reset/save restore the baseline transform. | `CuriousContraptions.tests/ClockPendulumTests.cs@a6c914e:L27-L80` | carry forward behaviour; do not carry forward the CPU transaction/failure-injection mechanism |
| C21 | A failed pulse tick publishes nothing; skipped presentation frames retain every pulse occurrence through Reset and save. | `CuriousContraptions.tests/ClockPulseFeedbackTests.cs@a6c914e:L62-L99` | carry forward behaviour |
| C22 | Reset cancels accepted and queued pulse occurrences; the restored lamp is slate. | `CuriousContraptions.tests/ClockPulseFeedbackTests.cs@a6c914e:L197-L218` | carry forward |
| C23 | Progress publication: 0 when unpowered, 0 at a pulse boundary, rises between pulses, unchanged while paused, 0 right after power is restored. | `CuriousContraptions.tests/OscillatorObservationTests.cs@a6c914e:L72-L117` | carry forward |
| C24 | Diagnostic wire names `stopped`/`running` (exact case). | `CuriousContraptions.tests/TimerDiagnosticTests.cs@a6c914e:L7-L31` | carry forward at the diagnostics boundary only |
| C25 | Catalogue entry and scene. | `parts/catalog/clock.tres@a6c914e:L1-L14` and `parts/scenes/clock.tscn@a6c914e:L1-L6` | carry forward the data |
| C26 | Fan-out admission is atomic and hidden essential feedback keeps backpressure: a clock driving two pulse lamps (capacities 2 and 1) that fires 4 pulses while hidden and paused admits a pulse to the lamps only when every lamp can take it — each lamp holds 1 accepted plus 1 pending and the other 3 pulses wait in the world queue; hidden frames consume none; once visible both lamps show gold and drain in order until all 4 pulses are accepted and completed by both lamps. | `CuriousContraptions.tests/ClockPulseFeedbackTests.cs@a6c914e:L100-L124` | carry forward behaviour; do not carry forward the CPU event-stream mechanism |
| C27 | The world pending-pulse cap rejects the whole tick: with 128 one-tick oscillators filling the world queue to its cap, the next tick throws and changes nothing (stream stamp, first queued pulse, queue count, oscillator state and body states unchanged); after presentation drains the queue an exact retry commits one more pulse per oscillator, and pulse sequence numbers continue from the earlier ones (next sequence = cap + 1). | `CuriousContraptions.tests/ClockPulseFeedbackTests.cs@a6c914e:L125-L147` | carry forward behaviour; do not carry forward the CPU stream/transaction mechanism |
| C28 | An admission check that reports `CapacityExhausted` (or rejects an out-of-order sequence) changes nothing: the accepted count stays as it was. | `CuriousContraptions.tests/ClockPulseFeedbackTests.cs@a6c914e:L148-L160` | carry forward |
| C29 | The legacy world cap on pending pulse occurrences was 4096 (`MaximumPendingOscillatorEvents`). | `reference/cpu/MachineWorld.cs@a6c914e:L23-L23` | carry forward as the legacy value only; the bound is open question 2 |
| C30 | A pulse with the wrong stream, generation, revision, source, tick or sequence cannot acknowledge or admit feedback: the existing occurrence remains pending (1), accepted count remains 0 and the lamp stays at its declared initial colour. | `CuriousContraptions.tests/ClockPulseFeedbackTests.cs@a6c914e:L162-L195` | carry forward atomic rejection and unchanged feedback; do not carry forward the CPU event-stream adapter or its exception mechanism as a worker tick fault |

`reference/P0-022-before/docs/coverage/engine/*.json` list `clock` only as a catalogue id: no element knowledge. `engine/bridge/CommittedDisplayClock.cs` is a presentation clock, not this element.

### Files harvested

- `parts/ClockPart.cs`
- `parts/catalog/clock.tres`
- `parts/scenes/clock.tscn`
- `engine/SceneOscillatorDeclaration.cs`
- `engine/SimulationOscillators.cs`
- `engine/MachineData.cs`
- `reference/cpu/MachinePart.cs`
- `reference/cpu/MachineWorld.cs`
- `CuriousContraptions.tests/ClockTests.cs`
- `CuriousContraptions.tests/ClockPendulumTests.cs`
- `CuriousContraptions.tests/ClockPulseFeedbackTests.cs`
- `CuriousContraptions.tests/SimulationOscillatorsTests.cs`
- `CuriousContraptions.tests/OscillatorObservationTests.cs`
- `CuriousContraptions.tests/TimerDiagnosticTests.cs`

## 5. Acceptance outline

Authority: [CAT-017](../requirements.md#current-cat-017), [todo-273](../requirements.md#todo-273); Story 9.3 adds `tools/e2e/cat-017.test.ts`.

- **Construction (Chrome UI).** Place Battery, Repeating clock and Counter (target 5); Connect battery→clock (Supply→PowerIn) and clock→counter (ActivationOut→ActivationIn) through the contextual UI. Do not use a Signal lamp as the pulse observer: it latches on its first input, so it cannot show repeated pulses (section 3).
- **Positive.** Interval 1.0 s: first trigger after one full powered interval, then one per interval. Over 5.0 s the clock's own pulse lamp flashes five times and the counter's dots reach 5 at 1, 2, 3, 4 and 5 s (the Story 9.3 AC: 5 pulses in 5.0 s); the pendulum swings once per interval.
- **Negative / controls.** No supply: no pulses, no dots (C18); no startup pulse at power-on; power loss exactly at a deadline suppresses that pulse (C13).
- **Boundaries.** 0.1 s and 12 s intervals; out-of-range values rejected (C19); restoration starts a full new interval with no catch-up; pulse-queue saturation rejects a whole tick and an exact retry keeps earlier pulses (C27).
- **Run/Reset.** Pause halts emission; Reset gives Stopped, count 0, phase 0, slate lamp and cancels queued pulses (C22).
- **Save/Load.** `interval_seconds` round-trips; run counts are not saved.
- **Integrations.** Clock→Counter counts every pulse (C17); hidden-frame pulse feedback shows on the first visible frame (C26).

## 6. Open questions

1. **Power — decided.** The clock runs only while its `PowerIn` route has exactly one upstream source (more than one is refused, `PowerInputSupplied`); unsupplied it stays Stopped and emits nothing. There is no unpowered or always-running mode (implemented by Story 9.3 and kept by C-Clock's declared capability).
2. **Pulse-queue bound values.** The requirement already demands atomic saturation handling (C26–C28 carry it forward); only the bounds are open. The legacy used 64 queued pulses per pulse lamp (`parts/ClockPart.cs@a6c914e:L35-L37`) and 4096 pending pulse occurrences per world (`reference/cpu/MachineWorld.cs@a6c914e:L23-L23`). **Resolved (D1 and D19 of [delivery batches](../invest/delivery-batches.md#3-family-level-decisions); C-Clock):** the flash is driven by the committed oscillator row, one impulse per committed pulse, and hidden catch-up stays at or below 64 impulses per lamp, the legacy value (`_bmad-output/implementation-artifacts/spec-c-clock.md:62`). No per-world ceiling is set as an owner value. Saturation proof (C26 to C29) stays at its named later gate.
3. **f32 activation and cosmetic lanes (resolved).** Story 9A.44 migrated the activation network, timers and cosmetic curves the clock extends to f32; no migration choice remains.

## 7. Delivered state, integration and residuals (22 November 2026)

Checked against `elements/SystemClock`, its scene and its tests on published main `8d1d68aa`, and against `origin/main` `25df3cd5`, which changes none of them. Nothing was built or run.

**Disposition.** Delivered CAT part: package `elements/SystemClock`, id `clock`, display name "Repeating clock", category Control, palette order 23 (`elements/SystemClock/SystemClockElementDeclaration.cs:21–26`). No slice is planned for this identity. It owns no named identity; EL-025 and EL-057 are separate elements. Every residual below is blocked on the engine programme (owner decision 3a, 22 November 2026), which precedes every further element.

**Integration.**
- Delivered collaborators: Battery (supply) and Counter (counts every pulse). A Signal lamp latches on the first pulse, so it cannot show repetition.
- [Current consumers](../invest/current-consumers.md#cat-017-i) says the clock "drives Signal lamp after a full interval and repeats". Section 5 and the delivered tests use the Counter as the repeat observer; the browser recipe should follow section 5.
- Campaign: no level in `content/puzzles.json` places it.
- Coverage binding: catalogue key `clock` in `docs/coverage/catalogue-elements.json`.

**Residuals.**
- The saturation item in the probe.
- The clock's interval editor clamps, while the Hold timer and the Delay box refuse an out-of-range entry for the same 0.1–12 s range (`elements/HoldTimer/HoldTimerElementDeclaration.cs:21`, `elements/DelayBox/DelayBoxElementDeclaration.cs:31`). Decided under the standing delegation: leave as delivered; it is an editor convenience with no effect on a Run.
- **Required forward-only refactor: unread ratings.** `ElectricalNodes` declares 12 V, 10 Ω and 50 W (`SystemClockElementDeclaration.cs:69–72`). The four rating values are only checked finite (`CuriousContraptions.Bundle/BundleValidator.cs:873–876`) and are unread; the oscillator rule requires this row's `Consumer` role (`SignalBundleValidation.cs:167`). Removing the record re-keys that check to the declared `SupplyConsumerDescriptor`; the deferred-work text is in batch B readiness.
- Requirement row CAT-017 stays unchecked until the post-catalogue browser phase.
