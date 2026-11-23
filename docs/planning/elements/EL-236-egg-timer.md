# EL-236 · Egg timer — named-identity spec

**Current delivery (21 November 2026):** [EL-236 ledger entry](../invest/named-elements.md#element-236), ledgered 21 November 2026; no earlier spec and no legacy to harvest. Code is cited as `path:line` in checkout `spec-drafts` (published main `8d1d68aa`); abstraction files are cited by file name and live under `CuriousContraptions.Abstractions*/`. Values are f32; **proposed** values are starting values ([D1](../invest/delivery-batches.md#3-family-level-decisions)). Tests are native; browser qualification is pending.

## 1. Identity and disposition

| Field | Value |
| --- | --- |
| ID / name / type | EL-236 · Egg timer · Control |
| Requirement anchor | [todo-213](../requirements.md#todo-213) (`sequence-task-362`) |
| Ledger entry / owner | [element-236](../invest/named-elements.md#element-236); slice `named-el-236`, no earlier S owner |
| Disposition | **To deliver as a single element, after the engine programme (owner decision 3a) and the rearming-timer row of `ENGINE-SIGNAL-NODES`** (a rearming timer that emits an elapsed activation; section 3). Run "timed and stored drives" in delivery batches 1b; the engine gap moves it to 1c. Checked against Duplicate closure with the Delay box (CAT-022): it is **not** a duplicate, because it differs in a parameter range (to 60 s against 12 s), in mode (it rearms; the Delay latches until Reset) and in its countdown dial. |
| Planned package | id `egg_timer`, directory `elements/EggTimer/` |
| Related | [CAT-022 Delay](CAT-022-delay.md) (shape), [CAT-033 Hold timer](CAT-033-hold_timer.md), [TH-34 Timed toaster ejector](TH-34-timed-toaster-ejector.md) |
| Status | Not started. Blocked on the engine programme (owner decision 3a); then `ENGINE-SIGNAL-NODES` (its rearming-timer row, engine design audit §6 item 11, which names this part) and `ENGINE-RUN-LENGTH` (owner decision 13) for any level whose delay with its outcome runs past the default 30 s Run. |

## 2. Declaration

Follows `elements/DelayBox/DelayBoxElementDeclaration.cs:10`.

- **Interfaces.** `ISignalElement` (`SignalContracts.cs:8`), `ISceneGeometryElement` (`SceneGeometry.cs:10`).
- **Body 0 (static).** A sphere of radius 0.35 m on a base 0.5 × 0.1 × 0.5 m. Restitution 0, friction 0.3.
- **Signal node.** `SignalTimerDescriptor("signal", "delay_seconds", TimerCompletionPolicy.Rearm, TimerBoundary.BeforePhysics, TimerElapsedSignal.Activation)` (`SignalContractValues.cs:49`). The Delay box differs only in `Latch`. **Today's validator refuses this combination** (`CuriousContraptions.Bundle/SignalBundleValidation.cs:157`: `Rearm` is admitted only with `TimerElapsedSignal.None` and `TimerBoundary.BeforeNetworks`); its admission is the rearming-timer row of `ENGINE-SIGNAL-NODES` (section 3).
- **Event.** `SimulationEventKind.TriggerActivated` on the node, as the Delay box (`DelayBoxElementDeclaration.cs:50`); it drives the ring cue.

| Parameter | Type, unit | Range | Default | Editor | Reason |
| --- | --- | --- | --- | --- | --- |
| `delay_seconds` | Real, s | 0–60 | 5 | Reject, build-only | A kitchen-timer scale, five times the Delay's 12 s ceiling; seconds (D10). Zero is admitted, because the requirement's zero delay behaves as authored (behaviour item 5); the delivered timer admits positive durations only (`CuriousContraptions.Bundle/SignalBundleValidation.cs:155`; `engine/gpu/ActivationTimers.cs:10`), which the rearming-timer row of `ENGINE-SIGNAL-NODES` widens (section 3) |

| Port | Domain | Direction, role, kind | Position (m) |
| --- | --- | --- | --- |
| `ActivationIn` | Signal | Input, ActivationIn, Pulse | (−0.4, 0, 0) |
| `ActivationOut` | Signal | Output, ActivationOut, Pulse | (0.4, 0, 0) |

- **Stores, supply.** None; like the Delay it needs no power.
- **Presentation.** Dial hand rotation from `PresentationDriver.CommittedTimerRemaining`, which is the remaining **fraction** of the duration, 1 to 0 (`engine/gpu/WorkshopDeclaredPresentation.cs:470`–`:472`): one full sweep per countdown, whatever the duration. The authored seconds are read from the selected-part panel; no numerals are printed. Indicator colour from `CommittedTimerPhase` (ready slate, counting ochre, finished gold); a 0.2 s ring wobble on the `TriggerActivated` event.
- **Palette.** Control, "Egg timer"; order 404 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)).

### Art and solidity

| Node | Mesh | Class |
| --- | --- | --- |
| Egg body | Sphere 0.35 m (drawn very slightly taller by its texture only), cream `#fff8e9` (owner decision 4) | Solid, sphere collider |
| Base | Box 0.5 × 0.1 × 0.5 m, navy `#1c2836` | Solid, box collider |
| Dial scale | Unnumbered tick marks printed on the sphere | Decorative: printed flat |
| Dial hand | Thin needle within 2 mm of the surface | Decorative: moved by a binding |
| Indicator | Flush disc on the base | Decorative: printed flat |

Icon: an egg outline with a dial arc and tick marks. It differs from the Delay box (a square housing with one sweep hand) by the egg shape.

### Behaviour and numbers

1. A pulse on `ActivationIn` starts the countdown from `delay_seconds`. When it reaches zero the timer emits one pulse on `ActivationOut`, rings, and is ready again.
2. A pulse while counting is ignored: it neither restarts nor extends the count.
3. Pausing the game pauses the count; it resumes without replaying or losing a pulse. Reset returns it to ready with the authored duration.
4. Tick order: boundary `BeforePhysics`, as the Delay box: the elapsed pulse is delivered before the physics of the tick in which it is due.
5. Zero delay is a setting. With `delay_seconds` 0 the timer adds no countdown: it is due at the first tick boundary after its start pulse, where it emits its one pulse on `ActivationOut`, rings and is ready again. A start pulse timed inside tick n (a contact during its substeps) therefore leaves at the boundary that starts tick n + 1 (the `BeforePhysics` boundary, item 4), the boundary at which a timer started then would begin to count; a start pulse that itself arrives on a tick boundary (another timer's elapse) leaves at the next boundary, never in the routing pass that started it, so no chain or loop of zero-delay timers can emit twice in one tick (the delivered routing loop relies on positive durations to exclude instantaneous timer cycles, `engine/gpu/ActivationNetwork.cs:474`). The rule is one for every timer: it is due at the later of its start boundary plus its duration in ticks and the first tick boundary strictly after its start pulse; only a zero duration ever meets the second bound.

## 3. Engine capabilities

| Capability | Mark | Symbol | Test |
| --- | --- | --- | --- |
| Declared timer that emits an activation on elapse | present | `SignalContractValues.cs:49` | `WorkshopDelayTests.RealContactChainedTimersFireAtThirtySixTicksRegardlessOfEntityOrder` |
| Deadline boundaries in f32 seconds | present | same | `WorkshopDelayTests.CanonicalF32ProductCeilingPreservesDeadlineBoundaries`; `ActivationTimerTests.FractionalStartCeilingAndExactDeadlineEmitDistinctTimerOccurrence` |
| Busy inputs neither restart nor re-emit | present | `SignalContractValues.cs:9` | `ActivationTimerTests.BusyAndFinishedInputsDoNotRestartOrReemit` |
| Rearm on expiry | present | `SignalContractValues.cs:9` | `HoldTimerTests.BusyInputsDoNotExtendAndExpiryRearmsWithoutAnyOutput` |
| Pause, Step and Reset of a pending timed request | present | `ActivationTimerValues.cs:12` | `CannonIntegrationTests.PauseRetainsTheClockRequestUntilStepAndResumeDoesNotReplayIt`; `WorkshopDelayTests.RealContactNonHalfDurationSurvivesSaveLoadAndResetExactly` |
| Remaining-time and phase presentation drivers | present | `ElementPresentation.cs:18`; the remaining value is a fraction of the duration (`engine/gpu/WorkshopDeclaredPresentation.cs:470`–`:472`) | `HoldTimerTests.DueDelayClosesContactBeforeSameTickConsumerIntegrationAndSaveResetIsExact` (reads); the sweep mapping is declaration data |
| **A rearming timer that emits an elapsed activation, at the `BeforePhysics` boundary** | **narrower** | refused today: `Rearm` requires `ElapsedSignal.None` and `BeforeNetworks`, `Latch` requires `Activation` and `BeforePhysics` (`CuriousContraptions.Bundle/SignalBundleValidation.cs:157`–`:158`) | Owner: `ENGINE-SIGNAL-NODES`, its rearming-timer row (owner decision 3a; engine design audit §6 item 11), stated for any package: "a declared timer may combine either completion policy with either elapsed signal; a rearming timer emits one activation on elapse and is ready again; busy inputs neither restart nor re-emit" |
| A 60 s duration (delivered timers stop at 12 s) | present, untested | `SignalBundleValidation.cs:155` | none; the slice adds `EggTimerTests.TheLongestDelayStillFiresOnItsDeadline` |
| **A zero-duration timer, due at the first tick boundary strictly after its start pulse** | **missing** | refused today: `ActivationTimerTransitions.Duration` throws for a duration of 0 s or less (`engine/gpu/ActivationTimers.cs:10`) and the validator admits positive durations only (`CuriousContraptions.Bundle/SignalBundleValidation.cs:155`) | Owner: `ENGINE-SIGNAL-NODES`, its rearming-timer row, stated for any package: "a timer's duration may be zero; every timer is due at the later of its start boundary plus its duration in ticks and the first tick boundary strictly after its start pulse, so no timer elapses in the routing pass that started it" |
| A level Run longer than 30 s | missing | the Run is 30 s (`CadenceValues.cs:77`) | Owner: `ENGINE-RUN-LENGTH` (owner decisions 13, 17) |

Counts: 6 present, 1 untested, 1 narrower, 2 missing. The narrower row and the zero-duration row need `ENGINE-SIGNAL-NODES` before this element, and any level that lets a long delay play out needs `ENGINE-RUN-LENGTH`; none is worked around.

### Engine conformance (owner addendum, 22 November 2026)

- **Declaration data only; generic laws.** One static body, one timer node, one event and two signal ports: the Delay box's declaration with a rearming policy. Counting, ignoring busy pulses, emitting and rearming are the timer rule; pause and Reset are the engine's tick and restore rules. The narrower capability is stated for any package (a rearming timer with an elapsed activation), owned by `ENGINE-SIGNAL-NODES`. No element code, callback or engine branch.
- **Three layers.** Colliders (physics): 1 sphere and 1 box as hidden `col_` proxies. Visuals: the art nodes, none a collider. Animation: the hand's rotation, the indicator colour and the ring wobble are bindings from the committed timer fraction, phase and event; they are evaluated off the physics thread and write nothing back. What moves each art node: egg body, base, dial scale and indicator nothing (static body 0; the indicator's colour is a binding); the dial hand its rotation binding.
- **Flat state.** 1 body; 2 colliders (1 sphere, 1 box); 0 joints; 0 drives; 0 sensors; 1 signal node; 0 stores; 3 bindings.
- **Performance.** A static body with two primitives and one timer row; no per-tick work beyond the timer table.
- **One-way flow, typing, tests.** Physics and signals → animation → renderer; nothing writes back. Closed sets are enums, ids typed, values f32. Every acceptance row is a scenario on the production simulation.

## 4. Sources and legacy

- **Requirement.** [todo-213](../requirements.md#todo-213): "Add a readable countdown actuator. Test zero delay, pause/Reset and ordered goals; keep author configuration separate from difficulty assistance."
- **Campaign.** Chapter 10 "Oddly Satisfying" (91–100), lesson "A good egg timer"; coverage row "… timed ejector/toaster, egg timer, …", prerequisites "familiar control contracts from 21–40". Owner decision 24e moves the first use to level **28** in chapter 3, after the Delay and Hold timer ([per-level allocation](readiness/campaign-coverage.md#6-per-level-allocation-and-progression-check-owner-decisions-24e-and-24f)).
- **Legacy.** None.
- **Files harvested.** None.

## 5. Acceptance outline

Tolerances: this is a timer, so the elapsed time from the accepted pulse to the output pulse is asserted to ±1 tick at the selected cadence; counts of output pulses are exact. E9 asserts its tick exactly: the next-boundary rule is the feature, and the defect it excludes (an output in the same routing pass as its start) is one tick away.

| Row | Given / When | Then | Control and boundary | Planned test |
| --- | --- | --- | --- | --- |
| E1 | Impact switch → egg timer (5 s) → Lamp latch | The lamp turns on 5 s after the impact | Control: no impact, never | `EggTimerTests.ItFiresAfterItsDelay` |
| E2 | A second pulse at 2 s; a third after the ring | One output for the first two; a second output 5 s after the third | — | `EggTimerTests.ItRingsOnceAndRearms` |
| E3 | `delay_seconds` −0.1, 60.1, NaN | Refused with a typed reason; the instance is unchanged | Boundary: 0 and 60 admitted; 60 fires on its deadline (the 60 s case runs past the default Run: its fixture declares a Run length of 70 s, `ENGINE-RUN-LENGTH`, within the 300 s maximum of owner decision 17) | `EggTimerTests.NegativeAndOutOfRangeDelaysAreRefused`; `EggTimerTests.TheLongestDelayStillFiresOnItsDeadline` |
| E4 | Pause at 2 s for any time, then resume | The output comes 3 s of game time after resuming | Control: no pause | `EggTimerTests.PauseHoldsTheCountdown` |
| E5 | Reset at 2 s; Save, Load | Ready, authored duration, wires restored; no stale output on the next Run | — | `EggTimerTests.ResetAndReloadRestoreTheAuthoredTimer` |
| E6 | Two egg timers, 2 s and 4 s, started together, each releasing a ball to its own Receiver | The 2 s ball is captured before the 4 s ball | Control: durations swapped, the order swaps | `EggTimerTests.TwoTimersOrderTwoDeliveries` |
| E7 | An electrical wire into `ActivationIn`; a Pressure plate level into `ActivationIn` | Refused with a typed reason | — | `EggTimerTests.WrongKindWiresAreRefused` |
| E8 | A level that locks the egg timer (`"locked"` in `content/puzzles.json` locks a part, not one parameter) | The player cannot edit its delay; hints and assistance never change it | Control: unlocked, the edit is admitted | `EggTimerTests.ALockedTimerIsNotEditable` |
| E9 | Zero delay (requirement: "test zero delay"). Impact switch → egg timer (`delay_seconds` 0) → Lamp latch; and egg timer A (1 s) → egg timer B (0 s) → Lamp latch, A started by one pulse | The first lamp turns on at the first tick boundary after the impact, from exactly one output pulse and one ring; B's one pulse leaves at the boundary after A's elapse, one tick later, not in the same tick | Control: `delay_seconds` 0.1 in place of 0, the output 12 ticks later at 120 Hz | `EggTimerTests.ZeroDelayFiresAtTheNextTickBoundary` |

E6 proves the causal order the requirement's "ordered goals" needs. Scoring an ordered-events goal is `engine-goal-ordered-events` (`ENGINE-GOALS`); that slice adds the goal row with this timer as its fixture.

**Integration.** Impact switch, Ball detector, Lamp, Latch, Counter, Powered gate, Cannon, Wound spring (delivered); TH-34 toaster ejector and EL-122 ordered-events goal (later).

**Coverage binding (planned, not yet live).** Published main binds a named-identity package through a final section of its spec that `tools/Coverage/NamedPackageSources.cs` reads (pattern: EL-239 Baseball); that section is not written here, because the reader parses every `EL-*.md` that carries its heading. It is written in the syntax current on main when this identity's slice starts. Planned values: Package `egg_timer`; Named identity EL-236; Acceptance todo-213 and sequence-task-362; owner slice `named-el-236`; Mode records `Configuration=Adjustable`. A slice-owned package is admitted only once main's reader admits one (the coverage follow-on story S7, which owner decision 15 also has widen the reader; EL-223 is paused, owner decision 24g). A level that places it also needs S8.

**Delivery.** One element slice. Prerequisites, in order: blocked on the engine programme (owner decision 3a); then `ENGINE-SIGNAL-NODES` (rearming-timer row, with zero durations) and `ENGINE-RUN-LENGTH` (for E3's 60 s case and any long-delay level); the coverage reader admitting a slice-owned package; S3 if published (S2 is published at `25df3cd5`; no test-project list edit). Files: `elements/EggTimer/` (project, declaration and `.uid`, `egg_timer.tscn`, icon and import), `CuriousContraptions.tests/EggTimerTests.cs`, and the usual hand-edited lists until S3. Review tier: engine (one untested capability). **Disqualified if** a 60 s deadline loses precision at the fastest cadence.

## 6. Open questions

1. *Zero delay.* Decided by the requirement, whose criterion is kept whole: [todo-213](../requirements.md#todo-213) asks for zero delay to be tested, and the ledger's summary of it is that zero delay behaves as authored. So zero is admitted and adds no countdown; the elapsed pulse leaves at the first tick boundary after the start pulse (behaviour item 5; row E9). The earlier 0.1 s floor, which refused zero, is withdrawn; the ledger entry's sentence recording that floor is for the coordinator to correct.
2. *Does it start by itself at Run?* Resolved by D19: no; it starts on a pulse, like every timer.
3. *Is it a duplicate of the Delay box?* Resolved in section 1: no.
4. *A seconds scale on the dial.* Resolved by the coordinator's decision of 22 November 2026: the dial is declared against what the driver is, a fraction of the duration, so it sweeps once per countdown and carries no numerals. A scale in seconds would need a presentation mapping scaled by a declared parameter; it is not planned.

Needs the owner: nothing.
