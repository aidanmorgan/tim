---
title: "CI harness determinism: two node harnesses that raced the runner"
status: done
followup_review_recommended: true
baseline: e344b19c
---

# Outcome

CI run 40045841714 (main 7143a874, GitHub ubuntu x64 runner, Node 26.12.1) failed in two game-runtime node harnesses ([excerpt](evidence-ci-harness-determinism/ci-run-40045841714-failures.log)). Each test asserted an outcome that depended on wall-clock scheduling.

Both tests, and the observation harness's two absence checks, now reach their assertions through the event that decides them:
- an awaited reply wait;
- a deterministic in-thread clock master;
- an explicit quiescence point.

No assertion is weakened, nothing is skipped, and no game code changes. The worker.js and workshop-client.js edits listed under Mutation controls were temporary and are reverted.

# Root causes

## `workshop-client-lifecycle` test 15: "a clock reply landing after the client failed is never delivered"

CI recorded `expected 1, actual 0` at "control: an answered probe is delivered".

The client's `probe` waits for its reply through the real ring's `Atomics.waitAsync`. The harness answered the probe synchronously (`serveRequests` stores Replied and notifies), then slept 20 ms (`settle`) and assumed delivery had happened.

The notify does not resolve the wait in the same turn; the wait's promise settles through a later task on the event loop. Nothing orders that task before the 20 ms timer, so a slow or loaded process can run the timer first, and the assertion then reads 0. This is analysis of the mechanism, and the reproduction is consistent with it: 10/10 runs failed under emulated linux/amd64 with Node 26.12.1, `--cpus=1` and 6 busy loops ([log](evidence-ci-harness-determinism/before-amd64-loaded.log)).

## `workshop-observation` test 1: "capture mode 1: …"

CI recorded `deepStrictEqual [missed, adopted]`, expected `[0, 1]`, actual `[1, 1]` ([excerpt](evidence-ci-harness-determinism/ci-run-40045841714-failures.log)).

The Simulation worker's running derivation check makes blocking `Atomics.wait` exchanges. Production's profile gives the exchanges of one check a single shared 2 ms budget (`WorkshopClockDerivation.ExchangeTimeoutMilliseconds`). A real stand-in master thread answered them. When that thread was not scheduled within the budget, the worker correctly counted a missed check. The test asserted that no check ever misses, so its outcome depended on thread scheduling.

# Fix

## Client lifecycle (tests 15 and 16)

The harness injects `requestReply`, already a context binding of the vm-loaded client, as a recording wrapper around the real ring function. `h.replyWaitsSettled()` loops until every recorded wait has settled, including any started while settling, and returns their outcomes. The client attaches its continuation to each wait before the harness does, so once a wait has settled the client has handled it.

`assertOneAnsweredWait` then asserts three things:
- exactly one wait was pending;
- it settled fulfilled;
- it settled with a non-null reply: the master's answer, not the 250 ms release, which is null.

Only after that is the absence asserted: the failed or disposed client did not deliver the reply. The 20 ms `settle` helper is deleted.

## Observation test 1: deterministic clock master, production budget

The harness keeps production's profile `[2, 100, 4]`.

**The stand-in master.** The worker's clock-master context is now the in-thread `clockMaster()` stand-in, with no thread.
- The worker reads the shipped `clock-ring.js`, evaluated in the worker's context, whose `Atomics.wait` lets the stand-in master serve every request already posted in the ring before the shipped wait runs.
- The shipped wait then finds the reply and returns at once, so an answered exchange never depends on scheduling.
- The stand-in answers the bootstrap handshake as `clock-master.js` does.
- It records every ring wait's timeout.

**The shared budget.** Test 1's stub native clock advances 0.25 ms per read. The test asserts the recorded wait timeouts exactly:
- 16 startup exchanges at 2 ms;
- the first running exchange at 2 ms;
- its retry at 2 − 3 × 0.25 = 1.25 ms, which is what is left of the check's shared budget after its three clock reads.

The startup-exchange assertion is re-derived for an advancing clock: `after ≥ before`, the master reading, epoch 0.

**Slow-master control (new).** The master answers every startup exchange and then no running one. The test asserts:
- `missed > 0` and no completed check;
- no failure envelope;
- every missed check turn still beats the heartbeat.

Then the master serves the stale request, and the test asserts that a later check completes, the heartbeat keeps advancing, and there is still no failure.

**until().** Its deadline counts at least 2000 attempts of at least 1 ms each. A module-level assertion pins it above ten check intervals with their full budgets. Blocking waits share its thread, so they only stretch it.

## Absence checks (observation tests 9 and 10)

These are "a failed session resumes no later turn" and "disposal cancels the timed and the queued turn". `manualTurns()` replaces the worker's scheduler-turn `MessageChannel` and timers, the only uses of either in worker.js.
- `pump()` delivers posted turn messages in order. A closed receiving port drops them, as a MessagePort does.
- `quiesce()` pumps, then fires every live timer in due-time order on a virtual clock, pumping after each.
- Both are capped at 1000 steps with labelled assertions, so a regression fails instead of hanging.
- The absence is asserted only after `quiesce()`.

# Mutation controls

Each mutation was applied alone, temporarily, and reverted ([log](evidence-ci-harness-determinism/mutations.log), with each command and diff):

| Mutation | Failing tests |
|---|---|
| M1: the scheduler-turn failure catch no longer disposes the session | test 9 (`resumed` gains 8) |
| M2: the `dispose` import does nothing | tests 1, 2 and 7 (they dispose during their flows); test 10 (`resumed` gains 3 and 2) |
| M3: a missed running check faults the session instead of being counted | test 3, the slow-master control: no check is ever counted as missed |
| M4: a check's retry gets the whole budget again | tests 1 and 2 (recorded retry timeout 2 instead of 1.25) |
| M5 (workshop-client.js): a reply landing after failure or disposal is delivered | lifecycle tests 15 and 16 |

# Evidence (`evidence-ci-harness-determinism/`)

Acceptance follows docs/testing-guidance.md "Stress and randomised tests" (main 860c3cb1). The proof is the root-cause analysis, the event-driven waits and quiescence points, and the mutation controls.

| File | What it records |
|---|---|
| [ci-run-40045841714-failures.log](evidence-ci-harness-determinism/ci-run-40045841714-failures.log) | The two failing test points from CI |
| [before-amd64-loaded.log](evidence-ci-harness-determinism/before-amd64-loaded.log) | Diagnosis only. Before the fix: lifecycle failed 10/10 at test 15; observation 0/10 |
| [stress-macos-50.log](evidence-ci-harness-determinism/stress-macos-50.log), [stress-amd64-loaded.log](evidence-ci-harness-determinism/stress-amd64-loaded.log) | Diagnosis only, not acceptance evidence. Repeat runs of an earlier candidate (the reverted 1000 ms budget), all clean. They do not cover the current harness |
| [mutations.log](evidence-ci-harness-determinism/mutations.log) | M1 to M5 above |
| [node-harness-tests.log](evidence-ci-harness-determinism/node-harness-tests.log) | `NodeHarnessTests` and `BrowserDeferralTests` through `dotnet test` on the current harness: 14/14, Skipped 0 |
| `git diff --check` | clean |

# Residuals

The remaining harness sleeps are recorded in deferred-work.md, owned by the JS-to-TypeScript conversion slice:
- `workshop-animation-clock`'s `settle`;
- the 5 ms retry in `workshop-clock-ring`;
- `until` polling.

# Tasks

- [x] Root-cause both failures and reproduce the lifecycle failure on linux/amd64.
- [x] Lifecycle: event-driven reply waits with asserted outcomes.
- [x] Observation test 1: production's 2 ms budget, a deterministic master, the shared budget exercised, and a slow-master control.
- [x] Observation absence checks: capped, due-time `manualTurns` quiescence.
- [x] Mutation controls M1 to M5; `NodeHarnessTests` and `BrowserDeferralTests` 14/14; `git diff --check` clean.
- [x] Add a deferred-work.md entry for the remaining harness sleeps.
- [ ] Independent review, then publication.

## Review Triage Log

### 2026-10-24 — Review pass
- verdicts: 30 findings (intent-alignment 5 divergences, edge-case 9, blind 16, verification-gap 0) — high 0, medium 3 groups, low 4 groups, false 0, maybe-false 0
- findings:
  - `[medium]` `[patch]` pump()/quiesce() have unbounded synchronous loops, so a regression hangs the test instead of failing it (edge 1, 2; blind 1). Fix: iteration caps with labelled assertions.
  - `[low]` `[patch]` quiesce orders timers by raw delay, not due time, and the comment claims "earliest first" (edge 3; blind 2). Fix: virtual now plus due-time ordering.
  - `[medium]` `[patch]` lifecycle absence checks are vacuous. replyWaitsSettled discards outcomes and never asserts that a wait was started or settled fulfilled with a non-null reply; waits started while settling are missed; the 250 ms fallback hides wall-clock time (edge 6, 7; blind 3, 4). Fix: return outcomes, loop until drained, assert exactly one wait was pending and fulfilled non-null, then dropped.
  - `[medium]` `[bad_spec]` observation test 1 widens the exchange budget from 2 ms to 1000 ms instead of waiting on the deciding event. It now proves "missed 0" under a harness-only budget, leaves the shared-budget path unexercised (nativeNow is fixed), has no slow-master control, misstates production's budget in its comments, and leaves the until() deadline shorter than the blocking wait (intent divergence 1, 2; edge 4, 5, 8, 9; blind 5, 6, 7, 8, 15). Spec amended: keep production's 2 ms; make the stand-in master's answer deterministic (answered synchronously before the worker's wait, or a deterministic responder); add a slow-master control asserting missed > 0 with the session continuing; advance the stub clock so the shared budget is exercised.
  - `[low]` `[patch]` evidence hygiene: unrecorded claims, M2 table vs log mismatch, before/after under different load, filtered logs without command or identity (intent divergence 5; blind 9, 10, 11, 12). Fix: keep only recorded claims and correct the M2 table. Per the new guidance, repeat-N stress logs are diagnosis only.
  - `[low]` `[patch]` a transient CI run ID is in a source comment (blind 16). Fix: state the invariant instead.
  - `[low]` `[patch]` residual harness sleeps have no owner (blind 14; intent divergence 4). Fix: a deferred-work.md entry owned by the TypeScript conversion slice, which rewrites the harnesses.
  - `[low]` `[reject]` acceptance lacks a green CI run (blind 13; intent divergence 2). The publication terminal checks already require exact-SHA CI, so the fix would only edit this spec.
  - `[low]` `[reject]` the scope covers tests beyond the two that failed (intent divergence 4). That follows the coordinator's direction under testing-guidance rule 7.

## Spec Change Log

- 2026-10-24, review pass 1 (bad_spec, observation test 1). Amended the Fix for observation test 1: production's 2 ms budget is kept, and the stand-in master's answer is made deterministic, so the outcome no longer depends on thread scheduling. A slow-master control is added. Avoided known-bad state: a widened wall-clock deadline (1000 ms) that changes what the assertion proves. KEEP: the lifecycle event-driven reply waits (strengthened per patch), manualTurns with deterministic quiescence for tests 8 and 9 (with caps and due-time order), and no game code changes.

## Auto Run Result

- **Summary:** the two CI-failing game node harnesses are now deterministic. Lifecycle reply waits are awaited, and their outcomes asserted, in place of sleeps. The observation harness runs under production's 2 ms shared budget with an in-thread deterministic stand-in master and an advancing stub clock. A slow-master control proves that missed checks are counted while the session continues. The absence checks run at a bounded, due-time-ordered quiescence point. No game code changed.
- **Files:**
  - tools/workshop-client-lifecycle.test.mjs: settled outcomes asserted for reply waits.
  - tools/workshop-observation.test.mjs: deterministic master, shared-budget assertions, slow-master test, manualTurns with caps and a virtual clock.
  - deferred-work.md: remaining harness sleeps, owned by the TypeScript conversion slice.
  - Spec and evidence.
- **Review:** 30 findings. 5 grouped entries were patched: 1 was a bad_spec amendment re-derived through the same implementer, and the patch groups held 2 medium and 3 low. 2 were rejected (CI green is covered by the publication terminal check; the scope follows coordinator direction under guidance rule 7). Nothing was deferred beyond the owned harness-sleeps entry.
- **Follow-up review recommended: true.** Two medium patch groups and a re-derived test-1 design have not been independently re-reviewed. Patched by verdict: medium 2, low 3.
- **Verification (coordinator):**
  - one Debug slnx build, 0 warnings;
  - E2eRunner NodeHarnessTests + BrowserDeferralTests 14/14, Skipped 0;
  - `git diff --check` clean.
- **Residual risks:** this fix has had no real ubuntu CI run yet; the first run after merge is the acceptance check.
