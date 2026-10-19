---
title: 'Story 9R.10: System Clock pulses, Pause and Reset proven in Chrome (reopened Story 9.3)'
type: 'bugfix'
created: '2026-10-18'
status: 'ready-for-dev'
route: ''
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/planning-artifacts/epics.md#story-9r10-system-clock-pulses-pause-and-reset-proven-in-chrome-reopened-story-93'
  - '{project-root}/docs/planning/elements/CAT-017-clock.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem (report #10):**
- The 5-pulse AC is proven only natively (`ClockTests.cs:189-236`).
- The Chrome Pause check (`cat-017.test.ts:171-190`) watches a ball, and the Reset check (`:195-199`) watches the blade.
- There are no native pause or reset facts.

Investigation shows the AC is unobservable and the state unsafe:
- The oscillator's phase, due tick, pulse count and last pulse tick are private mutable arrays on `ActivationNetwork` (`:91-93`, cleared in `Clear()` `:158-166`, advanced at `:353-410`).
- The oscillator's committed latch keeps only its first pulse (`:436-437`).
- `Advance` stages on the committed network instance (`WorkshopGpuDevice.cs:82`, `:151-152`), and `Discard` and the catch path (`:167-176`, `:192-200`) never restore those arrays. A discarded or retried tick therefore advances the clock twice.

**Approach:**
- Move the oscillator state into the committed activation checkpoint as a compiled Run-time row per Clock: Running/Stopped, due tick, pulse count, last pulse tick. Carry it in `WorkshopRead`, the wire and `workshop-wire.ts`.
- `Consume` becomes a pure function of (committed checkpoint, inputs).
- Then prove 5 pulses, Pause and Reset in Chrome from Pause and Step ack reads, and add native pause and reset facts.

## Boundaries & Constraints

**Always:**
- Common 9R rules:
  - Chrome proof uses actual UI controls through `workshop-driver.ts`.
  - Committed reads arrive only on command acks, so tick-exact facts come from Pause, Step and Resume ack reads. Every command response carries `Simulation.Committed` (`CuriousContraptions.Simulation/Program.cs:267`), including Resume and Step.
  - Exact Run/Reset uses the Story 9R.1 snapshot. Its contract: the tick-0 read from the last Construct or Load ack plus the authored bytes. Bodies, captures, activations, timers, electrical rows and the new oscillator rows are deep-equal after Reset.
- Pulse k of a 1.0 s clock is due at tick 120·k at 120 Hz (`ClockTests.cs:225-226`). The first pulse comes after one full powered interval, with no startup pulse.
- The clock runs only while its PowerIn supply is available (`ActivationNetwork.cs:357-374`).

**Never:**
- Don't count lamp samples as pulses: the lamp is sticky (CAT-017 §5).
- No ball-only or blade-only proxies.
- No wall-clock timing assertions on pulse spacing.
- No hidden mutable network state after this story, for the oscillator or the counter.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| 5 s | supplied clock, 1.0 s, paused at tick T in [600, 719] | count 5; last pulse ticks seen 120, 240, 360, 480, 600 | N/A |
| Pause | Pause ack at T, wait 2.0 s wall time, Resume ack | identical tick, count and due tick; next pulse due at the same tick | N/A |
| Reset | after pulses, Reset then Run | snapshot equal; first pulse at tick 120; count 1 | N/A |
| Unsupplied | clock with no battery wire | Stopped, count 0 for ≥ 3 s | N/A |
| Discarded tick | native: `Consume` twice from the same committed checkpoint | identical outputs; exactly one pulse at its due tick | N/A |

</frozen-after-approval>

## Code Map

- `engine/gpu/ActivationNetwork.cs:86-116`, `:158-166`, `:214-245`, `:350-412`, `:430-437` -- oscillator arrays, `Clear`, getters, emission, latch.
- `engine/gpu/ActivationNetwork.cs:77` -- `ActivationCheckpoint(Activations, Timers)` gains the oscillator rows.
- `CuriousContraptions.Simulation/WorkshopGpuDevice.cs:76-200` -- Advance, Stage, Commit, Discard.
- `engine/gpu/WorkshopRead.cs`, `WorkshopWireFlatBuffers.cs`, the FlatBuffers schema, `tools/e2e/workshop-wire.ts` -- the read rows.
- `engine/gpu/WorkshopSimulation.cs:420-440` -- the admission check of zero state.
- `ui/WorkshopConfiguration.cs:30-60` -- the Clock interval editor.
- `CuriousContraptions.tests/ClockTests.cs:188-336`; `tools/e2e/cat-017.test.ts:171-199` -- the checks to replace.

## Tasks & Acceptance

**Execution:**
- [ ] `CuriousContraptions.tests/ClockTests.cs`, red first:
  - a pause fact: no `Consume` while paused leaves the rows equal, and resume continues at the next due tick;
  - a reset fact: admission `Clear` gives Stopped, count 0 and due −1, then the first pulse at 120;
  - a discarded-tick fact at `ActivationNetwork` level: calling `Consume` twice from the same committed checkpoint and inputs gives identical output and leaves no state behind (oscillator and counter);
  - the rows are part of the checkpoint;
  - a wire round trip.
- [ ] `ActivationNetwork.cs`, `WorkshopGpuDevice.cs` -- delete the four oscillator arrays and the three counter arrays, and the getters that read them (`GetOscillatorPulseCount`, `GetCounterCount` and similar). State flows in and out through the checkpoint, and callers and tests are updated together.
- [ ] Read, wire, schema and `workshop-wire.ts` -- emit the oscillator rows. Admission requires their cleared values.
- [ ] `tools/e2e/workshop-driver.ts`:
  - `readCommittedAtPause()` (shared with 9R.9);
  - a `connectFrom(sourceRow, target, choice)` free-workshop Connect helper (Battery Supply → Clock PowerIn);
  - `setClockInterval(seconds)` through the real editor.
- [ ] `tools/e2e/cat-017.test.ts`:
  - delete the ball-only pause check and the blade-only reset check;
  - add the 5-pulse, Pause, Reset and unsupplied cases from the matrix;
  - keep the existing gate integration cases.

**Acceptance Criteria:**
- Given a battery-supplied Clock at the default 1.0 s built through real UI, when the run is sampled by repeated Pause/Resume acks until a Pause lands in ticks 600–719, then the committed rows show exactly 5 pulses whose last-pulse ticks are 120, 240, 360, 480 and 600. That is 1.0 s spacing at zero-tick error, within the one-tick tolerance.
- Given a paused run, when 2.0 s of wall time pass before Resume, then the Resume ack's tick, count and due tick equal the Pause ack's, and the next pulse lands at the original due tick.
- Given pulses emitted, when Reset, then the read equals the pre-Run snapshot (Stopped, count 0). After a new Run, the first pulse is at tick 120.
- Given native `ClockTests`, then pause, reset and discarded-tick facts exist and pass.
- Given a Clock with no supply wire, when run for ≥ 3 s, then its row stays Stopped with count 0.
- `cat-017` passes 100% serially in Chrome.
- `dotnet build CuriousContraptions.slnx` reports 0 warnings, `dotnet test CuriousContraptions.slnx` passes 100%, and every `tools/e2e/*.test.ts` suite passes 100% in one serial Chrome run against the production preview.

**§5 map (CAT-017):**
- Construction: AC1.
- Positive: AC1. Clock→Counter dots at 5 are Incomplete (Story 9A.41 editor).
- Negative/control: AC5, plus native C13 (power loss at the deadline).
- Boundaries: native `WorkshopClock_ValidationAndTicks`.
- Run/Reset: AC2 and AC3.
- Save/Load: existing `cat-017` test 5 plus native `WireSerialization`.
- Integrations: Clock→Counter→Gate (kept `cat-017` cases). Hidden-frame feedback is Incomplete (no owner).

## Implementation Notes

## Spec Change Log

## Review Triage Log

## Design Notes

**Decisions (owner playability delegation):**
- "Committed activation reads" means the new oscillator row, sampled at Pause and Step acks. Rationale: it is the only committed channel (`MachineWorld.Gpu.cs:197`, `:363`). The event ring is not consumed until Stories 9A.22 and 9A.23.
- Fixing the discarded-tick defect is in scope. Rationale: the epic rule says a defect found by the suite is fixed in the same story, and Pause/Resume exercises the staging path.
- The Counter's `_counterCounts`, `_counterLastTimes` and `_counterEmitted` (`ActivationNetwork.cs:158-161`) have the same hidden-state and discarded-tick defect in the same `Consume`. They move into the same committed checkpoint in this story (a Counter row: count, last trigger time, emitted). Rationale: the defect lives in one function, a half-pure `Consume` would contradict this story's invariant, and the kept cat-017 Clock→Counter→Gate cases run through it. Clock→Counter at target 5 still needs the target editor (Story 9A.41), so the Chrome observer remains the oscillator row.

**§5 items not reachable in UI:** 0.1 s and 12 s intervals, out-of-range intervals and pulse-queue saturation are covered natively (`ClockTests.WorkshopClock_ValidationAndTicks`). The pendulum and the per-pulse lamp flashes (CAT-017 §5 positive) have no scheduled owner. They stay Incomplete and are reported to the coordinator for Story 9R.18's open-clause record; this story does not claim them.

**Test-first plan:** native facts fail on the missing checkpoint rows. Chrome cases fail on the missing `oscillators` accessor. Pause/Resume uses the real menu buttons (`workshop-driver.ts:736-752`).

**Mutation plan:** each change below must fail a test.
- Advance phase while paused (Pause row).
- Skip the admission clear (Reset row).
- Emit a startup pulse at tick 1 (first-pulse tick).
- Restore the private array (discarded-tick fact).
- Pulse while unsupplied (control).

**Impacted suites:** `cat-017`, `cat-020`, `cat-037`, `cat-051`, `ClockTests`, `CounterTests`, `ActivationTimerTests`, `WorkshopWireFlatBuffersTests`, `clock-master.test.ts`.

**Risks:**
- Wall-time scheduling at 120 Hz may make the Pause land outside 600–719. Loop Pause/Resume, or use Step from tick ≥ 590.
- The read schema change ripples to every decoder.

## Verification

**Commands:**
- `dotnet build CuriousContraptions.slnx` -- expected: 0 warnings.
- `dotnet test CuriousContraptions.slnx` -- expected: 100%.
- Serial Chrome run of `tools/e2e/cat-017.test.ts`, then all of `tools/e2e/*.test.ts` against the 8060 production preview -- expected: 100%.
