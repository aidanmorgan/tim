# CAT-017 · clock — declaration readiness spec

Story 7.0 Batch B declaration spec (CAT-017-D). Legacy citations are pinned to `a6c914e` and resolve with `git show a6c914e:<path> | sed -n 'start,endp'`. Requirement row: [CAT-017](../requirements.md#current-cat-017). Supplied-network facts (N1–N24) are in [CAT-005](CAT-005-battery.md#4-legacy-harvest).

## 1. Identity

| Item | Value |
| --- | --- |
| CAT ID / kind | CAT-017 · `clock` ("Repeating clock") |
| Requirement anchor | [current-cat-017](../requirements.md#current-cat-017); retained behaviour [todo-273](../requirements.md#todo-273) |
| Mapped identities | none owned. Related but distinct: [EL-025 Tipping-bucket water clock](../invest/named-elements.md#element-025), [EL-057 Escapement](../invest/named-elements.md#element-057). No legacy implementation links them; their requirement rows stand as the source. |
| Roadmap story | 9.3 "System Clock Periodic Pulse Generator" (Epic 9) |
| Status | not started |

## 2. Declaration

| Item | Declaration (value · source) |
| --- | --- |
| Bodies and shapes | Static root box 1.4 × 1.7 × 0.6 m at the origin; foot box 1.6 × 0.2 × 0.85 m centred at y −0.95 (`parts/ClockPart.cs@a6c914e:L59-L60`; legacy `AddBox` = collision box, see [CAT-005 §2](CAT-005-battery.md#2-declaration)). |
| Mass and material | Static. Legacy static default material restitution 1, bounce threshold 0.1 m/s, friction 0.3 (`reference/cpu/MachinePart.cs@a6c914e:L236-L236`). |
| Constraints and joints | none (the pendulum is cosmetic, not a joint). |
| Typed sockets and ports | `PowerIn` Electrical Input at (−0.78, 0, 0); `ActivationOut` Activation Output at (0.78, 0, 0) (`parts/ClockPart.cs@a6c914e:L41-L45`). Current `WorkshopSocket` already has both values (`engine/gpu/WorkshopConnections.cs@a6c914e:L9-L9`). |
| Sensors and activation | A powered periodic source: runs only while `PowerIn` is available; emits one activation (Trigger) on each pulse tick through `ActivationOut` (`parts/ClockPart.cs@a6c914e:L24-L25`, `parts/ClockPart.cs@a6c914e:L40-L40`, `parts/ClockPart.cs@a6c914e:L52-L55`). No activation input. |
| Work and energy stores | none; consumes binary supply availability only. |
| Parameters | `interval_seconds`: float seconds, range 0.1–12 inclusive, default 1.0 (`parts/ClockPart.cs@a6c914e:L46-L51`, `parts/catalog/clock.tres@a6c914e:L14-L14`). Discrete interval = ceil(seconds / tick) ticks (`parts/ClockPart.cs@a6c914e:L21-L21`, `engine/SceneOscillatorDeclaration.cs@a6c914e:L10-L18`). Carry forward as f32 seconds with a typed parameter id; the current `DelayDuration` stores binary16 (`engine/gpu/WorkshopDelay.cs@a6c914e:L6-L33`), which is listed as remaining f32 migration (`docs/gpu-f32-physics.md@a6c914e:L96-L96`) and must not be copied. |
| Cosmetic curves and UI bindings | (1) Pendulum: rotation about Z follows target = 0.42 · sin(2π · progress) rad, presentation-clock exponential follow with rate 30, range ±0.42, initial 0; progress = committed oscillator progress fraction (`parts/ClockPart.cs@a6c914e:L26-L32`; formula `CuriousContraptions.tests/ClockPendulumTests.cs@a6c914e:L20-L25`). (2) Pulse lamp: slate `#556573` → gold `#f7cb52` per pulse, 0.25 s linear decay, overlapping pulses combine by maximum, essential (deferred until visible), event-timed, retains its first frame, capacity 64 queued pulses per lamp (`parts/ClockPart.cs@a6c914e:L35-L39`). Current cosmetic sources lack an oscillator-progress and oscillator-pulse source (`engine/gpu/WorkshopCosmetic.cs@a6c914e:L7-L7`). |
| Art | Housing 1.4 × 1.7 × 0.6 in `#e8b764` (catalogue colour 0.91, 0.72, 0.39); navy `#293954` foot; cream `#fff8e9` face 1.14 × 1.44 × 0.04 at z 0.32; slate `#556573` pendulum recess 0.84 × 1.1 × 0.02 at (0, −0.06, 0.35); pendulum pivot at (0, 0.4, 0.39) carrying a cream rod 0.045 × 0.7 × 0.035 (centre 0.35 below the pivot) and a gold bob r 0.15 at 0.7 below; navy pivot cap r 0.055 at (0, 0.4, 0.44); pulse lamp r 0.065 at (0, 0.63, 0.39); gold port spheres r 0.075 (`parts/ClockPart.cs@a6c914e:L56-L70`). Selection ring 1.1 (`parts/ClockPart.cs@a6c914e:L58-L58`). Toolbox icon (current, survives): `ui/WorkshopIcons.cs@a6c914e:L58-L58`. Design: "Repeating clock" row of [DESIGN.md · Motion and state feedback](../../../DESIGN.md#motion-and-state-feedback). |
| Catalogue / inventory | Id `clock`, Title "Repeating clock", Category Control, colour (0.91, 0.72, 0.39), Parameters `{"interval_seconds": 1.0}`; Description: "Connect electricity to start the pendulum. Sends one trigger each second, starting after a full interval. Losing power resets its timing; reconnecting begins a fresh interval." (`parts/catalog/clock.tres@a6c914e:L8-L14`). No level places or grants it. |

## 3. Engine capabilities

Families ([map row](../general-engine-element-map.md)): AnimationEvaluation, AnimationLifecycle, ContactImpulse, EnvironmentState, FiniteLedger, GeometryQuery, RigidBodyDynamics, SignalPropagation, SlidingFriction, TimedCommand (P0-022/023 shared evaluator/feedback registration).

**Exists now**
- Discrete activation network with integer-boundary timer occurrences, fan-out to lamps and timers, and Reset to a clear state: `engine/gpu/ActivationNetwork.cs@a6c914e:L70-L231`, `engine/gpu/ActivationTimers.cs@a6c914e:L66-L113`.
- Activation sockets and connection storage: `engine/gpu/WorkshopConnections.cs@a6c914e:L8-L13`.
- Cadence-to-ticks conversion with ceiling (Delay): `engine/gpu/WorkshopDelay.cs@a6c914e:L16-L33`.
- Note: `ActivationTime`, `ActivationLatch` and the cosmetic curve record store times and durations as binary16 (`Half`); activation/timer phases and cosmetic durations are listed as remaining f32 migration (`docs/gpu-f32-physics.md@a6c914e:L96-L98`). Extending them for the clock inherits that debt (open question 3).

**Missing**
- A repeating source rule. The current network lets each source emit at most once per world: a second emission from a latched source throws "Duplicate source emission" (`engine/gpu/ActivationNetwork.cs@a6c914e:L199-L208`). A clock emits on every pulse, so it needs a new rule — Story 9.3.
- A powered periodic oscillator node kind (Stopped/Running, due tick, pulse count, last pulse tick, progress) whose pulses enter the activation network as source occurrences — Story 9.3.
- Its power input from the supplied network — Story 8.1 (CAT-005), which now precedes 9.3 (open question 1).
- Cosmetic feedback sources for oscillator progress (pendulum) and per-pulse impulses with max overlap, plus a bounded pending-pulse queue with whole-tick saturation (C26–C28) — Story 9.3.

**Element dependencies**: Battery (CAT-005) to run at all; a downstream consumer that can observe repeated pulses. The Signal lamp (CAT-035) cannot: its node latches on its first input and ignores later ones (`engine/gpu/ActivationNetwork.cs@a6c914e:L213-L216`). Counter (CAT-020, Story 9.2) counts every pulse (C17).

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
| C11 | Every tick samples every power input once: unpowered → Stopped, due −1 (no pulse even at the deadline); newly powered → Running, due = tick + interval (no pulse); running at due → pulse, count + 1, last = tick, due += interval. Simultaneous pulses are emitted in identity order. | `engine/SimulationOscillators.cs@a6c914e:L83-L109` | carry forward |
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

1. **Power.** The legacy clock and the requirement need electrical supply to run; after the 9 Oct 2026 reorder the supply network (Story 8.1) precedes Story 9.3 (Epic 9). The Story 9.3 AC does not mention power. Whether 9.3 requires the Story 8.1 supply, or ships an unpowered/always-running mode the requirement does not define: unspecified — owner decision.
2. **Pulse-queue bound values.** The requirement already demands atomic saturation handling (C26–C28 carry it forward); only the bounds are open. The legacy used 64 queued pulses per pulse lamp (`parts/ClockPart.cs@a6c914e:L35-L37`) and 4096 pending pulse occurrences per world (`reference/cpu/MachineWorld.cs@a6c914e:L23-L23`). The per-lamp and per-world bound values in the worker design: unspecified — owner decision.
3. **binary16 activation and cosmetic lanes.** The activation network, timers and cosmetic curves the clock extends still hold binary16 values (`docs/gpu-f32-physics.md@a6c914e:L96-L98`, remaining f32 migration). Whether Story 9.3 migrates those lanes to f32 before extending them, or extends them as they are and leaves migration to the scheduled f32 work: unspecified — owner decision.
