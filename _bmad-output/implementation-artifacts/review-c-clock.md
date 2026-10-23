---
title: "Review — C-Clock (9A.42 + 9R.10), independent adversarial review"
candidate: "uncommitted worktree story-c-clock on base 14658883; tracked diff excluding _bmad-output and TODO.md (git diff HEAD -- . ':(exclude)_bmad-output' ':(exclude)TODO.md') sha256 781d8eddc18b84b4168525ed94931400720f55acf56370a2c35542eb84b28cd6 at the start and at the end of review; ClockGuardTests.cs 364021b3…, DeclaredOscillatorTests.cs 7aa5e0f7… (= evidence-c-clock/candidate/identity.txt)"
verdict: "First pass Incomplete (B1); after the fixes (tracked diff 7e19246c…) scoped native Pass with SnapshotApproval on base 14658883 (= origin/main). Browser qualification pending post-catalogue E2E."
date: 2026-10-23
---

# Verdict

**Incomplete.** The conversion is correct and I reproduced it independently on the production C# simulation:

- traces, lessons and placed solutions are byte-identical to the record path;
- the suite-first `ClockGuardTests` passes unchanged on the record path (56/56) and on the candidate;
- every base-encoder old save is refused typed at both boundaries, or (pipe, latch) decodes to the identical instance;
- the Known list shrinks by exactly the 30 Clock rows;
- production deletion greps are 0;
- the wire regenerates byte-identically with pinned flatc 25.2.10;
- the full suite, CI tools step, Release workers, strict tsc and the snapshot unit are green.

One D2 criterion has only vacuous proof in the shipped suite:

- **B1.** `ClockGuardTests.TwoSourcesOnOneClockAreRefused` never reaches the oscillator arm's own "more than one upstream source" refusal (`ActivationNetwork.cs:867`). It wires two batteries straight to the clock's PowerIn, and `WorkshopActivationCompiler.Compile` → `construction.Validate()` → `WorkshopConnections` (`:98`) already refuses that with the same `PowerInputSupplied` reason.
  - My mutant **R8** deletes the arm's refusal (it takes `sources[0]`) and survives the whole affected suite (359 + 167).
  - The arm is reachable only through a switched contact, whose PowerIn admits two suppliers. My probe — two batteries → latch PowerIn → latch Supply → clock PowerIn — is refused by the candidate and accepted by R8 (killed).

The behaviour is correct; the regression protection the spec claims ("two sources: refused") is missing. The finding goes back to the implementation owner.

- Nothing here is a browser, release, global-audit or runtime qualification.
- I edited no source, test, tool or content file. I wrote only this record and `evidence-c-clock-review/`.
- All probes ran in throwaway worktrees (`scratchpad/review-clock/wt-base`, `wt-mut`), both removed and pruned. Their sources are archived in `evidence-c-clock-review/probe/`.

# Checks run

There was one matching Debug build. Every test ran with `--no-build --no-restore`.

| Check | Result | Receipt |
|---|---|---|
| Candidate identity, start and end | Tracked diff `781d8edd…` at both; new test files match the implementer's hashes; `ClockGuardTests.cs` byte-equals `baseline/ClockGuardTests-base.cs.txt` | `identity.txt` |
| `dotnet build CuriousContraptions.slnx -c Debug` | 0 warnings, 0 errors, rc 0 | `build.log` |
| `dotnet test CuriousContraptions.slnx -c Debug --no-build --no-restore --blame-crash` (bash) | 17 / 167 / 77 / 189 / 2624; 0 failed, 0 skipped, 0 duplicate-ID notices, rc 0 | `full-test.log` |
| CI "Test C# tools" (`pages.yml` 51–64 verbatim, bash) | 77 / 167 / 17 / 174, rc 0 | `ci-tools.log` |
| Release `CuriousContraptions.Simulation` and `CuriousContraptions.Animation.Worker` (`--no-incremental`) | 0 warnings, rc 0 / 0 | `worker-release.txt` |
| Strict tsc 6.0.3 (`tools/e2e/tsconfig.json --strict --noEmit`); tsx snapshot unit | rc 0; 24/24, rc 0 | `ts-checks.txt` |
| `git diff --check HEAD`; `--no-index --check` for the 3 new files | clean | `diff-check.txt` |
| `anvil_check` (21 changed and new source, test and TS files) | 0 findings | (tool result, this review) |
| Wire regeneration: pinned flatc 25.2.10 (sha256 `f004709c…`) over the candidate schema | C#: identical except the pre-existing blank line 10 (also present at base); TS: generated prefix (2253 lines) of `workshop-wire.ts` identical | `wire-regen.txt` |
| Deletion greps (whole tree except `_bmad-output`/`docs`) | `WorkshopInput.Clock`, `*IntervalSeconds`, `clock_tick`, `AddThreshold`/`AuthoredScalar.Threshold`: 0 code hits. The remaining hits are TODO prose, a `PlacedParts` doc comment, `ElementIsolationTests` scanner data, the unrelated `ClockInterval` clock protocol (`WorkshopClockMapping`) and unrelated `Threshold` members (contact work, orientation, presentation). `"Threshold"` in `content/` is 0. `WorkshopClock.cs(.uid)` is absent | `deletion-greps.txt` |
| Known rows (JSON comparison) | 166 → 136: 30 removed, all `Owner: Clock`, 0 added; the 2 `PartId.Clock` FinalIsolation rows kept; the `Generic` section is unchanged | `known-rows.txt` |
| Handler floor 16 → 15 | The test's own `AsyncVoid` regex over `ui/*.cs` matches 16 at base and 15 at candidate, so the floor tracks exactly the deleted `ApplyClockInterval` | `handler-floor.txt` |
| Suite-first at base | `ClockGuardTests` (same bytes) on 14658883 + `baseline/test-only.diff` (record-path `PlacedParts.Clock`): 56/56 | `suite-first-base-rerun.txt` |
| Identity re-run | The archived probe compiled into my own base and candidate throwaway worktrees: 7 traces, 12×3 lessons, 12×3 placed solutions and the exhaustive parity receipt are identical base vs candidate, and each is identical to the implementer's `baseline/capture` and `candidate/capture` | `identity-compare.txt` |
| Old-save bytes regenerated at base by the production encoders (fresh directory) | 70/70 identical to `baseline/old-saves.sha256`, which is what the `DeclaredOscillatorTests` sha256 pins rest on | `old-saves-regenerated-at-base.txt` |
| Old saves at base | All 70 files LOAD on 14658883 (clock as `WorkshopClock`, counter as `WorkshopCounter`, …) | `base-old-saves-loaded.txt` |
| Old saves at candidate (own probe) | 66 typed refusals (33 save, 33 wire); 0 untyped; 4 loaded: pipe and latch at both boundaries, each decoded instance string-identical to base | `candidate-old-saves-outcomes.txt`, `pipe-latch-meaning.txt` |
| Full-engine cadence probe (own) | Full production `WorkshopSimulation` at Hz60 / Hz120 / Hz240 for 0.1, 1 and 12 s (Hz120 at 1 s), up to the run tick limit (≤ 7199 ticks): the count is ⌊tick/interval⌋ at every tick, with no drift; the counter's first input is at `interval × PhysicalStepsPerCommit` | `probes-candidate.txt`, `probe/ReviewerCadenceProbe.cs.txt` |
| Reviewer mutations R1–R8 | See below | `mutations.log` |
| Carried C-Plate terminal receipt | `cplate-terminal-receipt.patch` (`fc8a9722…`) applies to HEAD copies. `review-c-plate.md`, `review-9-1-pressure-plate.md` and `review-6-5f…md` are byte-identical to HEAD + patch. `sprint-status.yaml` differs from HEAD + patch only by C-Clock's three lines (343/345/347). `evidence-c-plate-review/terminal/` is identical (9 files) to `carry/cplate-terminal-evidence` | `carry-integrity.txt` |

**Mutations** (each applied alone and restored; affected classes: Clock*, DeclaredOscillator, DeclaredOrientation, WorkshopSaveRejection, PayloadInstance, WorkshopWireFlatBuffers, DeclaredPresentation, PalettePlacement, plus BundleElements.Tests). The control is 359/359 + 167/167.

- **R1.** Validator admits an `ActivationIn`. Killed (BundleValidatorTests).
- **R2.** Pulse collider is `PartIdentities + 2`. Killed (3: the 5-pulse guard and the exact node ×2).
- **R3.** `CarriesTypedField` ignores `target_count`. Killed (at least 12 failures shown — old clock saves, typed-field rows, `DeclaredSetting`; the runner truncates output at 12 lines).
- **R4.** Validator admits a zero minimum. Killed.
- **R5.** Interval compiled at fixed Hz120. Killed (at least 12 failures shown, all 60/240 Hz `TheCompiledIntervalIsTheCeiling…` pins; output truncated at 12 lines).
- **R6.** Length typed by value, not by bits (`-0f` admitted). **Survives**. The same mutant also survives at base 14658883 (304/304 on the save/wire classes), so this is a pre-existing gap: L1, non-blocking.
- **R7.** Validator admits a Dynamic body. Killed.
- **R8.** Oscillator arm's `> 1` source refusal removed. **Survives the shipped suite.** Killed only by my latch-routed probe → **B1**.

# Criterion and impact matrix

| # | Criterion (source) | Independent evidence | Verdict |
|---|---|---|---|
| 1 | Declared oscillator capability, generic, no PartId (C-Clock; 9A.42; spec D1) | `SignalOscillatorDescriptor`/`IOscillatorElement`, `SignalPayload.Oscillator` round-trips; the compile arm selects on `WorkshopGenericInstance` + `Signal?.Oscillator`; the validator has seven-way exclusivity, Real Second with finite min > 0, Pulse out/no in, PowerIn + Consumer, Static/no slider/collider, and committed-oscillator binding coupling (R1, R4, R7 killed; the implementer's M4/M5) | Pass |
| 2 | Interval compile and parity; f32 lanes (D2) | `Duration` = `MathF.Ceiling(f32·rate)`. The exhaustive 57,881,396 f32 × 3 cadences parity receipt re-run is byte-identical; ±1-ulp pins at 0.1/1/12 × 3 cadences; R5 killed | Pass |
| 3 | More than one source refused, `PowerInputSupplied` (D2; CAT-017 §6 Q1) | Behaviour correct (own latch probe). The shipped test is refused upstream by construction validation and does not reach the arm; R8 survives | **Incomplete (B1)** |
| 4 | Period 5 pulses in 5.0 s at 120·k, no startup pulse, no drift at all cadences (9.3; 9A.42 AC1; 9R.10 AC1; C11, C12) | `ClockGuardTests` 5-pulse and floor matrix (production compile + Consume, 7200 ticks × 3 cadences × 3 intervals); my full-engine probe at Hz60/240 (above). Occurrences: an `OscillatorTick` latch from the clock's first collider at ordinal 480, held; the counter counts each | Pass |
| 5 | Interval 0.1–12 literal boundaries, Clamp, Build-only (D3; C19) | Construction refuses `0.1 − 1 ulp`, `12 + 1 ulp`, 0, −1, 0.01, 13, NaN and +∞. The save boundary refuses NonFinite/OutOfRange and the wire refuses OutOfRange; 0.1 and 12 load bit-exact. Editor (real buttons): 13 → 12, 0.05 → 0.1; NaN/±Infinity/text/empty make no change, command or Undo; Cancel; an unchanged Apply adds no Undo; Undo ×3 restores 1.0; Running/Paused/locked refused; a move keeps the interval | Pass |
| 6 | Pause adds nothing, resume continues; Reset → Stopped/0, first pulse at 1.0 s, snapshot equal (9R.10 AC2, AC3; 9A.42 AC2) | `ClockGuardTests` pause (240 refused Advances, `AssertSameState`, tick-identical to an unpaused run) and reset (built == reset, replay identical) — run by me at base and candidate | Pass |
| 7 | Unsupplied control; deadline loss and restore (9R.10 AC5; C13, C16) | Unwired and disabled-battery controls stay Stopped with 0 pulses; loss at tick 120 suppresses the pulse; restore gives a fresh full interval | Pass |
| 8 | Save/load exact; counts not saved (9A.42 AC3; C22) | 0.1/1/12 equal at both boundaries; the built read is Stopped/0; the run replays bit for bit | Pass |
| 9 | Pendulum and per-pulse flash from committed phase and pulse occurrences through declared bindings; held while paused; rest on Reset (9R.10 AC6; coordinator decision 18 Oct; C20–C23) | Pendulum = `−.42 + .84·(.5 + .5 sin 2π·phase)` at 6 ticks; Stopped → 0 rad; a converged sample is unchanged with no new read (D4's meaning). The flash is lit on exactly 120…600, idle between. Reset read → 0 rad, idle, and the stale projection refused. Hidden catch-up, skipped first frame and Reset cancel: `DeclaredPresentationTests.CommittedClockRetains…` and `SameWorldPrepareCommit…`, now on the generic clock. After save/load there is no dedicated presentation test; this is accepted because loaded reads are bit-identical (criterion 8) and the evaluator is a pure function of committed reads | Pass (Chrome pixel/binding reads pending) |
| 10 | "Native `ClockTests` gain pause and reset facts" (9R.10) | Met by the sibling `ClockGuardTests` (suite-first, run on both paths). The AC asks for native facts, not a class name; accepted | Pass |
| 11 | Wire `threshold` removal, regeneration, 10 → 9 fields, trailing-field literal slot (D5) | Regeneration is identical (above); the `DeclaredOrientationTests` boundary is now literally slot 9 (`Layout(10, 8, …)`); the implementer's M7 (9 → 10) was killed. R3 killed | Pass |
| 12 | Old saves at both boundaries; nothing misread (D5) | See checks: the base bytes regenerate and load at base; the candidate gives 66 typed refusals and 0 untyped; pipe/latch decode identically (their slots did not move; the threshold was unused by both). Counter targets 1–9 are refused typed (`OldLayoutSaves` breadth; 3, 4 and 8 are also pinned base-encoder bytes). Expected-change sites (`DeclaredContactLoadTests`, `DeclaredOrientationTests`, `PayloadInstanceTests`, `WorkshopSaveRejectionTests`, `WorkshopWireFlatBuffersTests`, `ConstructionDecodeBoundaryTests`, `ClockTests`) each re-derive a typed refusal from the new layout; none weakens a refusal into a load | Pass |
| 13 | Deletions in slice, no fallback/alias/migration; Known only shrinks; the ratchet passes (C-Clock deletes and AC; AGENTS) | See checks. The ratchet is `ElementIsolationTests`, which is green in the full suite. There is no compatibility path; old saves are refused, not migrated | Pass |
| 14 | Identity (traces, nodes, lessons, placed solutions; compiled-geometry clock row) | Own re-run identical; `PackageRuntimeParityTests` passes on the untouched `compiled-geometry-9a16.json` | Pass |
| 15 | Docs/status | CAT-017 §2/§3 (capability), the superseded binary16 note, §6 Q1 decided; isolation A3/A5/A6/A11 row; TODO; sprint lines `review`; `9r-10` stays backlog with its native note | Pass (see L2) |

# Rulings on the implementer's decisions

1. **The dropped `clock_tick` trigger:** confirmed. Nothing reads declared triggers; the bundle identity change is recorded; this follows the C-Plate precedent.
2. **Oscillator `Last`:** confirmed. `ActivationMemory.Validate` has required `Last == default` for an oscillator since 9R.1, and base behaves the same (identical traces). The pulse time is observable in the held latch and in downstream counters' `Last`. See L2 for the CAT-017 C11 wording.
3. **9R.10 pause/reset facts in `ClockGuardTests`:** accepted (criterion 10).
4. **Cadence matrix:** sufficient as proof of the law; it runs the real network and kills R5. The stated reason is **inaccurate**: the production `WorkshopSimulation` runs Hz60 and Hz240 (my probe). Only the shared `WorkshopPhysicsEngineTests.Run` helper is fixed at 120 Hz. Correct the sentence in the spec (L3). Not blocking.
5. **`PackageRuntimeParityTests:373` retarget:** accepted. The compiled `IntervalTicks` is asserted exactly elsewhere (R5 and the implementer's M3 are killed).
6. **NaN NonFinite (save) / OutOfRange (command):** accepted. This is the shared generic rule (C-Plate precedent), and both are typed.
7. **Handler floor 16 → 15:** confirmed forced (`handler-floor.txt`). It is not a weakened gate.
8. **Old-save expectation changes:** confirmed attributed one by one; each remains a typed refusal at both boundaries (criterion 12).
9. **`9r-10` stays backlog:** accepted until the terminal Pass, as in the 9r-9/9r-16 precedent.

# Findings

- **B1 (blocking → Incomplete).** Criterion: D2 "more than one source is refused, `PowerInputSupplied`" and spec "two sources: refused".
  - Risk: if the arm's check regresses, a clock fed through a switched contact from two sources silently takes the lowest-id source as its power source, and no test fails.
  - Why it blocks: AGENTS ("unknown coverage or missing proof is Incomplete") and the C-Plate B2 precedent. The shipped proof is vacuous for the converted line.
  - Smallest resolving check: see below.
- **L1 (Low, non-blocking, pre-existing).** No test pins that a `-0f` Length is a typed field (R6 survives at base and candidate). The implementer may add a `-0f` row to the typed-field theory while editing; not required.
- **L2 (Low, non-blocking).** CAT-017 C11 still says "last = tick" for the oscillator row. That has not been true since 9R.1; the implementer's correction is in the spec only. One sentence in CAT-017 §4 C11 (or §2) recording where the pulse time lives would keep the current document complete.
- **L3 (Low, non-blocking).** The spec's decision 4 rationale says "the engine harness admits Hz120 only". Correct it as in ruling 4.

# Smallest resolving check (for re-review)

1. In `DeclaredOscillatorTests` (not `ClockGuardTests`, whose suite-first identity must stay; if it goes there, re-run it at base), add:
   - a fact with two batteries → latch PowerIn → latch Supply → clock PowerIn, where `Compile` throws `PowerInputSupplied`;
   - a one-battery control that compiles with `PowerSource` = that battery.

   `evidence-c-clock-review/probe/ReviewerTwoSourceProbe.cs.txt` is a working shape. Fix the comment or name of `TwoSourcesOnOneClockAreRefused` so it no longer claims to prove the oscillator arm (it proves the connection rule).
2. Show R8 killed by the shipped suite.
3. Optional: L1–L3.
4. Then:
   - a new tracked-diff hash;
   - one Debug build;
   - full `CuriousContraptions.tests` (slnx if production changed);
   - R8 re-run;
   - SnapshotApproval if `origin/main` is still `14658883`.
5. The Story 9.3 native-only addendum (C-Clock AC "the review appends the addendum that re-verifies Story 9.3") is appended with the terminal receipt.

# Process

- Reviewer probes were compiled only into the throwaway worktrees.
- `python3 -I` was used read-only for the Known-row JSON comparison.
- One combined carry-integrity command was refused by the permission layer (it used `rm -rf`/`git init`); it was re-run with `patch` on copies.
- The base `ProbeClockCapture.OldSaves` xUnit result was Failed only because its report directory did not exist; all 70 files had been written, and the byte comparison is the receipt.

# Re-review of the fixes (23 Oct 2026)

**Candidate:** tracked diff `7e19246c26eb1c03b38fe01734eaaeb391abfebfd4bcb88acca83b1271ad9550`.
- Against the reviewed `781d8edd…` candidate, the only tracked change is `docs/planning/elements/CAT-017-clock.md`: the L2 C11 note.
- Also changed: the untracked `DeclaredOscillatorTests.cs` (now `4f22025e…`) and the spec (the L3 correction to decision 4).
- `ClockGuardTests.cs` is unchanged (`364021b3…`, the suite-first identity).
- No production, tool or TS source changed, so the first pass's Release worker, CI tools, tsc, snapshot-unit, wire and identity receipts still apply.

**B1: resolved.**
- The new theory `DeclaredOscillatorTests.TwoUpstreamSourcesThroughALatchAreRefusedByTheOscillatorItself` builds the latch-routed bench, asserts `bench.Validate()` admits it, and asserts the oscillator arm's own reason and message ("This source-accounted input requires one upstream source.").
- Its one-battery control compiles with that battery as `PowerSource`.
- **R8**, run by me in a fresh throwaway copy of `7e19246c…`, is now killed by the shipped suite: `twoBatteries: True` [FAIL], 1/363.
- The existing direct-wire test's role is stated in the new test's comment; `ClockGuardTests` is left unchanged by design.

**L1: resolved.** `ANegativeZeroLengthIsATypedFieldOnAGenericRecord` stores −0 with `ForceDefaults` (bits `0x80000000` asserted) and expects a typed OutOfRange refusal at both boundaries, with a +0 load control. **R6** is now killed: `negative: True` [FAIL], 1/363.

**L2 and L3: resolved** (CAT-017 C11 "Current (since 9R.1)" note; spec decision 4 correction).

**Checks** (`evidence-c-clock-review/rereview/`):

| Check | Result |
|---|---|
| `dotnet build CuriousContraptions.slnx -c Debug --no-incremental` | 0 warnings, 0 errors, rc 0 |
| `dotnet test CuriousContraptions.slnx -c Debug --no-build --no-restore --blame-crash` (bash) | 17 / 77 / 167 / 189 / 2628, 0 failed, 0 skipped, 0 duplicate-ID notices, rc 0 |
| Affected-class control and mutations | 363/363 + 167/167; R8 and R6 each killed; both restored (`mutations.log`); throwaway worktree removed and pruned |
| Carried C-Plate terminal receipt | Still applies to HEAD copies. The three review files are byte-identical to HEAD + patch; `sprint-status.yaml` differs only by C-Clock's three lines; `evidence-c-plate-review/terminal/` is identical (`carry-integrity.txt`) |

**Verdict: Pass (scoped, native).**
- Every criterion in the matrix is Pass, with zero unresolved findings.
- This is no browser, release, global-audit or runtime qualification.
- Browser cases stay pending for post-catalogue E2E (spec "Pending browser cases").

# SnapshotApproval (23 Oct 2026)

- `origin/main` after `git fetch` is `146588838b97d9634406c47f9e383b3dd24f8fa6`, which equals this worktree's `HEAD` (the candidate's base).
- I staged with `git add -A`, 247 paths:
  - the slice: production, tests, tools, schema, content, docs, `TODO.md`, the spec, `sprint-status.yaml`, `evidence-c-clock/`, `evidence-c-clock-review/` and this record;
  - the carried C-Plate terminal receipt: the `review-c-plate.md` terminal section, the `review-9-1…` and `review-6-5f…` addenda, the C-Plate sprint lines, and `evidence-c-plate-review/terminal/`, byte-checked against the carry.
- The staged paths include no scratch, build output or compiled probe source; reviewer probes appear only as `.txt` under `evidence-c-clock-review/probe/`.
- `git diff --cached --check HEAD` has findings only in verbatim raw logs and diffs under `_bmad-output/` (build logs, recorded diffs, the carried CI log). There are none in source, test, tool, schema, content or doc files.
- Pre-append staged tree: `c18f7c57b3889cb4a270442b24ca5dda10fe6007`.
- `git diff --cached --binary HEAD` sha256: `ba33a0a8f9f4c8d2d13d7600534753c294f7cf173a6312adad4f07e71f2ef032`.
- Appending this section and re-staging changes only this file. The final staged tree is reported to the coordinator.
- Every pre-publication criterion above is verified for this exact candidate (tracked diff `7e19246c…`).

**Publication-dependent checks** (for the reviewer after commit/merge/push):
1. The slice commit's tree equals the approved final staged tree. The merge into `main` (or the fast-forward) is pushed, and `origin/main` agrees with `git ls-remote`.
2. The pre-commit hook (C# static gate and anvil) passes on the exact snapshot, with its log kept.
3. The exact-SHA Pages CI run succeeds: C# tests, the "Test C# tools" step, the production Release publish and the static-deployment checks.
4. The deployed artifact and the Pages deployment reference that SHA.
5. After publication, `sprint-status` is updated:
   - `9a-c-clock-conversion` and `9a-42` → done;
   - `9r-10` → done with a native scoped Pass; Chrome stays post-catalogue, as with 9r-15;
   - `9-3` stays in-progress, with the native-only addendum appended to `review-9-3-system-clock.md` (the C-Clock AC);
   - browser qualification stays pending.
