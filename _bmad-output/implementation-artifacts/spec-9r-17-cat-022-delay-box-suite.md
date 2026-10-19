---
title: 'Story 9R.17: Delay Box suite cat-022 (reopened Story 6.5e)'
type: 'feature'
created: '2026-10-18'
status: 'ready-for-dev'
route: ''
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/planning-artifacts/epics.md#story-9r17-delay-box-suite-cat-022-reopened-story-65e'
  - '{project-root}/docs/planning/elements/CAT-022-delay.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem (report #8):** `tools/e2e/cat-022.test.ts` never existed. The closest proof, `anim-1b.test.ts:105-108`, checks that the lamp waits "roughly" 1 s from animation wall timestamps (≥ 800 ms). That is not "exactly the declared duration within one tick". The Delay → Powered Gate refusal is proven only natively (Story 8.2).

**Approach:** Add one suite following CAT-022 §5:
- `delayed_signal` with the drawer-placed Delay wired switch → delay → lamp through the real Connect controls, its duration set with the existing Delay editor;
- tick-exact timing from committed latch and timer rows at Pause acks;
- the before-deadline negative;
- the free-workshop Delay → Powered Gate refusal (the no-matching-socket path);
- the 0.1 s and 12 s endpoints;
- exact Reset and Save/Load.

## Boundaries & Constraints

**Always:**
- Common 9R rules:
  - Chrome proof uses actual UI controls through `workshop-driver.ts`.
  - Committed reads arrive only on command acks, so timing comes from Pause and Step ack reads, never wall time.
  - Exact Run/Reset uses the Story 9R.1 snapshot. Its contract: the tick-0 read from the last Construct or Load ack plus the authored bytes. Every body's cell, local, rotation, com, velocity and angularVelocity, plus the captures, activations, timers and electrical rows, are deep-equal after Reset (model: `cat-062b.test.ts:226-231`).
  - Negatives in authored levels run at Precise.
- Duration is 0.1–12 s. The due offset is `DelayDuration.Ticks(cadence)`, the exact ceiling of seconds × rate on the stored value (`engine/gpu/WorkshopDelay.cs:6-34`). It is Ready → Counting → Finished, and inputs are ignored once Counting or Finished.
- The `delayed_signal` fixtures stay as authored (`DelayedSignal.Validate`, `engine/gpu/DelayedSignal.cs:39-57`): switch (−3, 1, 0), trigger ball (−3, 4, 0), lamp (3, 1, 0), goal ActivatedAfter(lamp, switch, 1 s).

**Never:**
- No wall-clock timing assertions.
- No setters, imported solutions or numeric placement.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| 1 s | Delay set to 1.0 via the editor | timer `StartedTick` = ceiling tick of the switch occurrence; `DueTick` = `StartedTick` + `Ticks(1.0)` (= 120 at 120 Hz); lamp `eventOrdinal` = `DueTick` × substeps; 1 solve | N/A |
| Before | Pause at a tick < due | Delay timer Counting; lamp Clear | N/A |
| 0.1 s / 12 s | editor endpoints | offset = `Ticks(0.1)` and `Ticks(12)`; the 12 s case runs ≥ 12.5 s | N/A |
| Repeated input | second switch impact during Counting (native if not reachable) | same due tick | N/A |
| Gate link | free workshop: Delay "Connect ActivationOut" → click Powered Gate | no choice offered; no command; bytes unchanged | no-matching-socket path |
| Out-of-range text | 13, 0, NaN typed in the SpinBox | committed duration never leaves 0.1–12 | SpinBox clamp / `FromInput` reject |

</frozen-after-approval>

## Code Map

- `engine/gpu/WorkshopDelay.cs:6-34` -- bounds, `FromInput` and `Ticks`.
- `engine/gpu/ActivationTimers.cs`; `engine/gpu/ActivationNetwork.cs:345-352`, `:415-445` -- timer completion and the lamp latch from `TimerElapsed`.
- `engine/gpu/DelayedSignal.cs:6-57` -- lesson Create and Validate.
- `ui/WorkshopConfiguration.cs:16-27`, `:62-77` -- the existing Delay editor (SpinBox `DelaySeconds`, "Apply duration") and `ApplyDelayDuration`.
- `ui/WorkshopConnections.cs:77-95` -- "These parts have no matching sockets" (no choices for Delay → Gate PowerIn).
- `tools/e2e/anim-1b.test.ts:86-92` -- the `delayed_signal` placement and connect recipe through `connectActivation` panels `locked` and `delay`.
- `tools/e2e/workshop-wire.ts:496-530` -- `ActivationRecord` (`eventOrdinal`, `phase`) and `timers()`.

## Tasks & Acceptance

**Execution:**
- [ ] `tools/e2e/workshop-driver.ts`:
  - `setDelayDuration(seconds)`, which types into the real SpinBox and presses "Apply duration";
  - reuse `readCommittedAtPause`, `connectFrom` and `expectNoCommand` (shared stories).
- [ ] `tools/e2e/cat-022.test.ts` (new) -- the matrix cases, each from a fresh page. Ordinals come from committed rows. Convert the ordinal unit (tick vs substep) using `ActivationTime`'s declared substeps before comparing to `Ticks`.
- [ ] Defect fixes, only if a case finds one, with a native `WorkshopDelayTests` or `DelayedSignalTests` case first.

**Acceptance Criteria:**
- Given a Delay between an Impact Switch and a Signal lamp, wired through real UI with its duration set by the existing Delay control, when the switch fires, then the lamp lights after exactly `Ticks(duration)` ticks, measured by the derivation rule in `ActivationTimerState.Trigger` and `Complete` (`engine/gpu/ActivationTimers.cs:93-110`):
  - `StartedTick` = `CeilingTick(switch occurrence)`;
  - `DueTick` = `StartedTick` + `Ticks`;
  - the lamp occurrence = `ActivationTime.Boundary(DueTick)`.

  Each value is asserted exactly from the committed timer and latch rows. This is zero error, within the one-tick tolerance. In `delayed_signal`, one solve is printed.
- Given a Pause before the due tick, then the lamp is Clear and the timer is Counting (negative control).
- Given a free-workshop Delay and Powered Gate, when the Delay's ActivationOut is connected to the gate, then the link is refused: no choice is offered, no command is issued and the bytes are unchanged (control).
- Given the 0.1 s and 12 s endpoints, then the offsets match `Ticks` exactly, and out-of-range text never commits.
- Given any case after Run, when Reset, then the read equals the pre-Run snapshot (timer Ready, due −1). When Save, reload and Load, then the duration bits and wires are byte-identical and the timing replays.
- `cat-022` passes 100% serially in Chrome.
- `dotnet build CuriousContraptions.slnx` reports 0 warnings, `dotnet test CuriousContraptions.slnx` passes 100%, and every `tools/e2e/*.test.ts` suite passes 100% in one serial Chrome run against the production preview.

**§5 map (CAT-022):**
- Construction: AC1.
- Positive: AC1. The hand and indicator are covered by `anim-1b`.
- Negative/control: AC2 and AC3. The bypass and missing-link cases are in Design Notes.
- Boundaries: AC4. Invalid durations are native (`WorkshopDelayTests`).
- Run/Reset and Save/Load: AC5.
- Integrations: switch → delay → lamp in `delayed_signal` (AC1). `delayed_solar`, `wind_then_release` and `saved_for_later` are Incomplete until their companion parts land.

## Implementation Notes

## Spec Change Log

## Review Triage Log

## Design Notes

**Decisions (owner playability delegation):**
- "Exactly … within one tick" is asserted as an exact equality of the committed ordinal difference to `Ticks`. Rationale: tick-quantised timers are deterministic, and the tolerance is only a ceiling.
- The gate-link control runs in the free workshop. Rationale: the Powered Gate is free-workshop only (`workshop-driver.ts:502`).
- Out-of-range text is proven as never committed. Rationale: the SpinBox clamps to its 0.1–12 range before `FromInput`, so `FromInput`'s reject is reachable only natively (`WorkshopDelayTests`).

**§5 items not reachable in UI:**
- Repeated same-tick input: native `DelayedSignalTests` and `ActivationTimerTests` (one switch latches once per run).
- Direct early bypass: `DelayedSignal.Validate` admits only the lesson's parts, so a switch → lamp link may be admitted. If it is, prove the goal fails while the Delay still finishes; otherwise use native.
- Hand revolution and indicator colours: `anim-1b`.

**Test-first plan:** the driver helper goes first, verified by a saved duration change. The suite then runs red against the current build. Failures are defect stops.

**Mutation plan:** each change below must fail a case.
- Use floor instead of ceil in `Ticks` (the 1.13 s native case and the 0.1 s case).
- Fire the lamp at Counting start (Before row).
- Allow Activation → PowerIn (Gate link).
- Skip the duration in the save (Save/Load).

**Impacted suites:** `anim-1b`, `anim-1c`, `cat-037`, `cat-051`, `WorkshopDelayTests`, `DelayedSignalTests`, `ActivationTimerTests`.

**Risks:**
- Ordinals are in substeps (`ActivationTime.Boundary` = tick × substeps, `ActivationTimers.cs:26-30`). The switch occurrence sits mid-tick, so a raw lamp-minus-switch ordinal difference is not `Ticks` × substeps. Always use the derivation rule above.
- The 12 s case lengthens the suite, so keep it to one test within the 150 s timeout.

## Verification

**Commands:**
- `dotnet build CuriousContraptions.slnx` -- expected: 0 warnings.
- `dotnet test CuriousContraptions.slnx` -- expected: 100%.
- Serial Chrome run of `tools/e2e/cat-022.test.ts`, then all of `tools/e2e/*.test.ts` against the 8060 production preview -- expected: 100%.
