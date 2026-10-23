---
title: "Independent review: 9A.23 event ring consumers and 9A.25b one-shot bindings on the ring"
candidate: uncommitted working tree, worktree story-9a-23-25b, base f5ffdb4ee14a2fd575d726688c897cbf3816d456
reviewer: independent adversarial reviewer (did not implement; changed no source, test, tool or content file)
date: 2026-10-24
verdict: FAIL
---

# Verdict: FAIL

The shipped shared reader and the shipped Simulation writer deliver torn event records under concurrency on this machine (arm64). This is the slice's own PRIMARY acceptance: "Every delivered record is byte-equal to its source, and none is torn" over at least 10^6 records.

- It is a known violation, not missing proof.
- No SnapshotApproval was given, and nothing was staged or committed.
- The other checked criteria hold (see the matrix).

# Findings

## F1 (Blocking). The seqlock lets torn records through on arm64

The writer and reader use these accesses:

- **Writer** (`CuriousContraptions.Simulation/wwwroot/worker.js` `writeEventRing`):
  1. `Atomics.store(word, 0)`;
  2. a plain `target.set(payload)`;
  3. `Atomics.store(word, sequence)`.
- **Reader** (`CuriousContraptions.web/wwwroot/event-ring-cursor.js` `EventRingCursor.read`):
  1. `Atomics.load` before;
  2. a plain `bytes.slice` copy;
  3. `Atomics.load` after.

**Likely cause (inferred, codegen not confirmed):** the payload accesses are plain, so under the JS memory model they race with the sequence words. On arm64 they may be reordered past the seq-cst sequence accesses. The probe below is the evidence.

**Raw results** (unmodified candidate code, `tools/workshop-event-ring-cursor.test.mjs`):

- **First repeats:** 1 failure in 3 runs (`cursor-stress-repeats.txt`).
- **20-run loop:** 1 failure. Run 16 delivered 3 torn records at capacity 4 (`cursor-run-16.log`).
- **Isolated probe, 40 runs per variant** (`seqlock-probe.txt`, scratch copies of the three shipped files):

  | Variant | Failed runs | Torn records per failure (all at capacity 4) |
  |---|---|---|
  | Shipped code | 5 of 40 | 1, 4, 2, 3, 4 |
  | Reader copies the payload with `Atomics.load` per 8-byte word | 0 of 40 | — |
  | Writer stores the payload with `Atomics.store` per word | 0 of 40 | — |

**What this means:**

- The implementer's 109/109 pass and its "the fence is proven by test (F5)" claim come from lucky runs. The test is flaky against the shipped code.
- The reviewer's own second-fence mutant (the after-load replaced by the before value) failed **10 of 10** runs, with 88, 86, 102, 79, 98, 94, 89, 102, 90 and 94 torn records (`mutations/rv-js-second-fence-removed.txt`). The fences cut the rate from about 100 torn records per run to an intermittent 1–4, so they help but are not sufficient.

**Production impact.** A torn copy can occur when a reader catches up about one lap behind, for example after a stall or a hidden tab. The lap jump lands on the oldest slot, which is the next one the writer reuses. The torn copy then either:

- carries mixed fields, which `SimulationEventRecord.Read` refuses. The refusal throws inside `EventRecords`, then `fail()` faults the Animation worker; on the main thread it throws inside `TryUiFrame`; or
- delivers a wrong owner, kind or time under a valid sequence.

**Required fix:**

- Make the payload accesses atomic per word on **both** sides: `Atomics.store` in the writer and `Atomics.load` in the reader.
  - This changes the shipped 9A.22 writer, which is in scope because this slice's acceptance names that writer.
  - A one-sided change is still a race under the JS memory model. Its 0-of-40 probe result may only reflect a narrower window.
- Keep the before and after sequence checks.
- Add an amplified capacity-4 stress variant, with more records and readers per run, that fails on the shipped code in nearly every run. The fix must pass it in every run of at least 40 repeats, with the raw counts recorded.
- The C# twins (`EventRingBuffer.Append` and `EventRingConsumer`) have the same plain-copy shape but are single-threaded test references. Note this; no change is required.

**Owner:** the implementation owner.

## F2 (Medium, must be fixed with F1). One D5 clause is untested

D5 says a ring older than the installed epoch is ignored. The JS epochs test checks that a newer ring is held, and that an older installation does not read a newer ring. It never checks the case where the ring is older and the installation newer.

- **Mutant:** `this.epoch !== admitted` changed to `this.epoch > admitted`, so a reader whose ring is older than the installation reads it anyway.
- **Result:** it **survived**, 6/6 (`mutations/rv-js-older-ring-read.log`).
- **Why it matters:** this order is reachable when Commit(N+1) reaches a consumer before the N+1 ring does. Under the mutant, the C# side would throw "Foreign event ring epoch" in `AcceptEvents` and fault the worker.
- **Fix:** add a test that attaches the epoch-7 ring, then calls `read(8n)`, and expects nothing delivered and the cursor unchanged.

## F3 (Low). A formatting regression

In the `workshop-client.js` `create()` diff, two statements were merged onto one line with a run of spaces: `client.unwatchIsolation = …;        await new Promise(…`. `git diff --check` does not catch this.

## F4 (Low, process; for the coordinator). Mandatory gates were broken

The spec's disclosure says some pre-write `anvil_validate_write` calls were skipped, and some writes used heredoc or python3. This breaks a mandatory gate, and a reviewer cannot waive it.

- **Reviewer's after-the-fact check:** `anvil_check` (antipattern) over all 83 changed or untracked source, test and tool files in the final tree found 0 warnings.
- **Requirement:** the fix round must use Anvil before each write.
- **Reviewer's own gaps, disclosed:**
  - The first write of this record had no prior `anvil_validate_write`. It was checked afterwards with `anvil_check` (0 findings). Later edits were validated with `anvil_apply_patch` first.
  - The reviewer's mutants were applied with `sed -i` and `perl -0pi`, only in throwaway copies (a scratch worktree, since removed, and scratchpad file copies). The candidate tree was not touched.

## Observations (not blocking)

- **Wire regeneration (D9a).**
  - Pinned flatc 25.2.10 (`flatc --version`) reproduces the TS generated part byte for byte.
  - The C# output differs only by one leading blank line, from the `schemas/README.md` pipeline. That pipeline produces the same one-line difference against the base f5ffdb4e file, so the README's "byte for byte" claim was already inaccurate at base (`flatc-regen.txt`).
- **Pulse visibility mutant.** Changing the Event pulse from `AdvanceWhileHidden` to `DeferUntilVisible` survived (`mutations/rv-pulse-defer-hidden-*`). It is observationally equivalent under `EventTime` timing: a pending pulse starts at `OccurredAt` when it is shown.
- **PumpGoal is not natively exercised.** The `BrowserWorkshopClient.PumpGoal` wiring (JSImport `readEvents`) is browser-only. Its logic is reproduced natively by the authored-goal equivalence test. Its browser case is pending.
- **Goal coverage.** 12 authored puzzles have goals; the equivalence test runs the 6 that have authored solutions. The other 6 are all Captured goals, which wake structurally on their own sensor frame's Captured record, the same edge the evaluator reads.
- **Cadence (advisor question 1).** `Configure` is the only same-epoch re-admission with a new capacity, and it runs only in Building. No record exists in that epoch, and the non-retained Prepare builds a fresh evaluator (`_lastSequence` 0). The duplicate-sequence throw is not reachable.

# Criterion / impact matrix

| # | Criterion | Expected | Evidence (this review) | Result |
|---|---|---|---|---|
| 1 | Reader correctness under concurrency: Atomics order, torn detection, laps, epoch | No torn record; delivered + skipped = written | Stress test flaky on the shipped code: 7 failing runs of 63 executions. Lap and epoch-held tests pass. The older-ring clause is untested. | **FAIL (F1, F2)** |
| 1m | Second fence removed | Killed | Reviewer's mutant: 10 of 10 runs failed, 79–102 torn records each. The implementer's mutant had 101 torn. The unmutated code also fails intermittently (1–4 torn), so the kill does not prove the fences sufficient. | see F1 |
| 2 | 16 bindings typed by `SimulationEventKind`, values unchanged | Values identical to base | Diff of the four declarations: only Driver, Event and EventMagnitude changed. The golden test asserts Driver, Kind and Duration per binding. | Pass |
| 2 | 9A.25a goldens, bit for bit | Exact float equality | `PresentationBindingGoldenTests` (exact `Assert.Equal`) passes through `AcceptEvents` → `ApplyDeclaredProperty`; golden arrays are unchanged. | Pass |
| 2 | Cue at the record's physical time; detector re-pulses per Passage | `Ordinal` + `Phase` | `TheCueStartsAtTheRecordsOwnOrdinalAndPhase`, `EachRecordRestarts…(detector: 2 cues)`. Reviewer mutant "oldest reached cue" was **killed** (`ALappedRing…`). | Pass |
| 2 | No duplicate across lap, epoch, pause, hidden catch-up or Reset | One cue per record | `PauseResumeAndStep…NoDuplicate`, `AHiddenPart…`, `ResetRests…`, `ALappedRing…`, `ADelivery…Refused` all pass. | Pass |
| 2 | D3a: Bumper cues only on paid hits | Positive Magnitude ⇔ old `Effect == Paid` | `ContactWorkOccurrence.Validate` enforces Passive ⇔ Debit bits 0 and Paid ⇔ Debit > 0; the record's Magnitude is the hit's Debit. The deleted relay pulsed only on `Effect == Paid` (`git show f5ffdb4e:…ContactWork.cs`). Equivalent. | Pass |
| 3 | Goal wake held until its committed read; no main-thread recomputation | Evaluator only when due | `WorkshopGoalWake` is a pure tick gate; `MachineWorld.Gpu` still evaluates Solved from the committed read. Implementer's wake mutants were killed. | Pass |
| 3 | D8a: ActivatedAfter wakes on any record | Polling-equivalent | `KindOf`: only Rearm timers, passage, sticky and indicator nodes publish nothing. Rearm timers are validated `ElapsedSignal.None`, so they emit no signal. Every latch transition shares a tick with a published source record. `delayed_signal` and `domino_effect` are equivalent to polling. | Pass |
| 4 | D9: occurrence rows device-private; wire regenerated; Impact bytes identical | Same SHA at the **true base** | Throwaway worktree at f5ffdb4e: 3 records, SHA-256 `E54B4A09…853CC8`, equal to the candidate pin. `ContactWorkLedger` is validated on the device, including cooldown. flatc 25.2.10 reproduction as above. | Pass |
| 5 | Deletions have 0 production hits | 0 | `rg` over the tree, excluding `_bmad-output`: only the retirement-guard test and `EngineCapability.ContactImpulse` (an unrelated coverage capability). Known list 94 → 94. | Pass |
| 6 | Old relay ranges in e2e suites | Recorded as pending | 13 suites read the retired ranges: anim-1a/1b/1c, cat-002, cat-005, cat-014, cat-015a, cat-015a-sidekick, cat-015b, cat-020, cat-023b, cat-037, cat-052-lamp. With `cat-062b` goal timing that makes 14. All are in the spec's pending list. None affects native acceptance (E2eRunner native 190/190). | Pass (pending browser) |
| 7 | Lifecycle test fix is test-only and bounded | Harness only | Diff limited to `clientHarness`: it races creation against the first worker, with a 10 s per-test bound. Production is unchanged. 39/39 pass. | Pass |
| 8 | Reviewer mutants | Killed or justified | Oldest-cue: killed. Older-ring JS: **survived (F2)**. Pulse defer-hidden: equivalent. | see F2 |

# Commands and raw results (`evidence-9a-23-25b-review/`)

| Check | Command | Result |
|---|---|---|
| Build | `dotnet build CuriousContraptions.slnx -c Debug` (`build.log`, up to date), then `--no-incremental` (`build-no-incremental.log`) | 0 warnings, 0 errors |
| Full suite | `dotnet test CuriousContraptions.slnx -c Debug --no-build --no-restore` (`test.log`) | 17 / 191 / 77 / 190 / 2877, rc 0 |
| Node harnesses | `node --experimental-vm-modules --test --test-timeout=120000 tools/*.test.mjs` under `timeout 900` (`node-harness.log`) | 109/109, 1.3 s (a passing run; see F1 for the repeats) |
| CI step | pages.yml "Test C# tools" step, verbatim, under bash (`ci-tools.log`) | 77 / 191 / 17 / 174, all rc 0 |
| Release workers | `dotnet build CuriousContraptions.Simulation -c Release` and `CuriousContraptions.Animation.Worker -c Release` | rc 0 / 0, 0 warnings |
| Strict tsc | Pinned TypeScript 6.0.3 with `@types/node` 26.0.1 `--typeRoots` (`tsc.log`) | rc 0 |
| Snapshot schema unit | tsx 4.23.15 loader (`e2e-unit.log`) | 24/24 |
| Whitespace | `git diff --check` and `git diff HEAD --check`; untracked files scanned (`diff-check.txt`) | Clean (F3 is not caught by this) |
| Anvil | `anvil_check` antipattern over 83 files | 0 warnings |

**Hygiene.** The throwaway base worktree was removed (`git worktree list`: none). The scratch seqlock copies live only in the session scratchpad. No process from this review is left running.

# Return to the implementation owner

Fix F1 (atomic payload accesses) and F2 (the older-ring test), and tidy F3. Re-run the stress test at least 40 times with the raw counts, plus the node harnesses and the affected checks, then return the candidate to this reviewer for re-review of the affected scope.
