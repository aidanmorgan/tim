# Review: Story 9R.0a, Tiered and Parallel E2E Runner (Murdoch-5)

**Terminal verdict: PASS (scoped). No blocking finding is open. Everything filed is non-blocking.**

- **Candidate:** `d8e071f9` (the B1 fix) on `f79705aa`, on base `c854b4d5`.
- **Reviewer:** Murdoch-5, the only reviewer, in one final round (owner rules, 19 Oct 2026).
- **Blocking rule:** only two kinds of finding block: a breach of the ACs as the coordinator cut them, or a player-visible regression. Everything else is filed, by default to Story F.1.
- **Player-visible regression:** none is possible. The `c854b4d5..d8e071f9` diff touches no shipped code:
  - `tools/`;
  - the slnx;
  - `CSharpStaticGateTests`' `XunitProjects`;
  - docs, the spec and evidence.
- **Scope:** scoped to the runner. This is not a global or runtime qualification claim.
- **Raw evidence** (copied from Murdoch-5's scratch): `_bmad-output/implementation-artifacts/evidence-9r-0a-review-murdoch5/`.

## Cut-scope criteria matrix

| # | Criterion (coordinator cut) | Evidence | Status |
|---|---|---|---|
| 1 | Typed C# runner, serial by default, Parallel only where the catalogue marks it, backed by calibration | `SuiteCatalogue.cs`: 9 SerialAlone and 25 Parallel suites. `Scheduler.RunAsync` finishes Phase S before Phase P. `MeasuredWorkers.Resolve`: uncalibrated → 1, a different fingerprint refuses, and `--workers` above N refuses. The 25 Parallel rows are set-equal to the 25 files of calibration trial-1 and trial-2 (both Pass, 0 debt faults, slowest file 1.01×). In the run of record, each Parallel file is ≤ 1.01× its N = 1 duration. Mutant M05 (serial in Phase P) is recorded | Met |
| 2 | `CCGPU_DEBT_FAULT` close finding with no opt-out, plus a typed expected-finding assertion | See "Debt faults" below | Met |
| 3 | Tier 2 selection: unknown changes select Full; a changed element with no Primary is a typed error; integration and connected suites | Unknown and unmatched paths are Full (`ImpactRules.UnknownRule`; 15 Full rows in `SelectionTests`). `NoPrimarySuite` is thrown for the changed element (4 rows). B1 was found and is fixed (below) | Met at `d8e071f9` |
| 4 | Calibration plus one parallel full run | `evidence-9r-0a/calibration/calibration.md` gives N = 2; N = 4, 6 and 8 are rejected (debt fault, failures, > 2×). The run of record is checked under "Full run reused" below | Met |
| 5 | A skipped or todo test fails the run | `TapResult.Failures` adds `SkippedTests` and `TodoTests`. Real node v26.12.0 TAP for `test.skip`, `test.todo`, `{skip:true}` and `t.skip()` gives `# skipped 3` / `# todo 1` with exit 0, which matches the parser's regex. The Murdoch-5 skip mutant is killed (below) | Met |
| 6 | The runner publishes the bundle and records its identity | `Program.PublishAsync` treats a failed publish as a typed refusal. `RunEngine` starts the watcher first, then records the manifest identity, the served check and the suite-source digest at start and end, and verdicts any mismatch `IdentityChanged`. Shown live in the Tier 2 run below | Met |
| 7 | Analyzer gate | No `#pragma`, `SuppressMessage`, `NoWarn`, `GlobalSuppressions` or `.editorconfig` override under `tools/E2eRunner*` (checked with `rg`). All 25 catch clauses are typed. Both projects are in the slnx, the test project is in `XunitProjects`, and `CatchAllBoundaries` is unchanged. Tier 1 is clean (below) | Met |

## Runtime phase (after CHROME CLEAR) on a clean `git worktree add --detach … d8e071f9`

### Tier 1

| Check | Result |
|---|---|
| `dotnet build CuriousContraptions.slnx` | 0 Warning(s), 0 Error(s) |
| `dotnet build CuriousContraptions.slnx -p:PlaytestDiagnostics=true` | 0 Warning(s), 0 Error(s) |
| `dotnet test CuriousContraptions.slnx` | Coverage.Tests 78/78; CuriousContraptions.tests 1692/1692, including `CSharpStaticGateTests`; E2eRunner.Tests 156/156. Exit 0 |
| `node --experimental-vm-modules --test tools/*.test.mjs` | 82/82, with 0 skipped and 0 todo |

The logs are in `tier1/`.

### B1 reproduction and fix

- **Method:** `select --changed` on a list holding `parts/catalog/bowling.tres`.
- **At `f79705aa`:** the selection was cat-014, cat-005, cat-015a, cat-052, palette-placement and the smoke pair. **cat-023b and clock-master were absent: reproduced.**
- **At `d8e071f9`:** the same selection plus `clock-master` and `cat-023b`, each with the reason "integration: level domino_effect places bowling". **Fixed.**
- The new `SelectionTests.PartPreplacedByADeclaredLevelSelectsThatLevelsSuites` passes inside the 156.

### Murdoch-5 mutants

The diff is `tier1/mutants.diff`. Both mutants were reverted, and `git status` was clean afterwards.

| Mutant | Change | Result |
|---|---|---|
| Skip | Delete `if (tap.Skipped > 0) failures.Add(SkippedTests)` | **Killed.** `SuiteProcessTests.SkippedOrTodoTestFailsTheFile("skipped")` fails (155/156) |
| O1 (expected fault silences all) | `sessionFaultFindings`: `fault !== expected` becomes `expected === undefined` | **Killed.** 2 of 14 harness tests fail: "a different fault than the declared one is a finding…" and "a declared fault does not excuse another fault that also occurred" |
| G1/G2 (analyzer gate) | In `tools/E2eRunner/RunSummary.cs`, add an unused private field and `try { … } catch (Exception) { }` | **Killed.** The runner build fails with 4 errors: CA1823, CS0169 and IDE0051 for the field, and CA1031 for the catch-all. `CSharpStaticGateTests.NoCatchBlockIsEmpty` fails (88/89) and names `tools/E2eRunner/RunSummary.cs:59`. The diff and logs are in `gate-mutant/`. Reverted, and the worktree was removed |

### Tier 2 run through the runner

Command: `run --changed [tools/e2e/palette-placement.test.ts] --out …`.

- **Clean worktree inside the repo:** `tier2-palette-placement/`, run `20261019T150935Z-59341`.
  - **Pass:** 3 files, 31/31 tests (palette-placement 25, engine-core-2a1 5, engine-core-2b1 1), 0 debt faults, 73.5 s.
  - **Selection:** palette-placement "suite: … changed", plus the smoke pair.
  - **Parallelism:** N = 2, with slots 0 and 1 overlapping.
  - **Identity:**
    - `sourceHead` is `d8e071f9`.
    - `sourceStatusDigest` is `e3b0c442…`, the empty-status hash, so the tree was clean.
    - Suite `940c8df9…` and runner `0fb80f87…` match Murdoch-5's recomputation from disk.
    - Bundle `e88c33db…` is the same at start and end, and both served checks pass.
- **Unplanned negative control:** `tier2-negative-outside-repo/`.
  - The first attempt ran from a clean worktree outside the repository directory. Every file failed with `Cannot find module 'flatbuffers'`, because the untracked repo-root `node_modules` was not reachable from there.
  - The runner reported this correctly: `Fail`, with `NonZeroExit,NoTests,FailedTests,NotOkLine` on each file and exit 1.
  - This is environmental, not a candidate defect. It is filed as F13.

### Debt faults (criterion 2)

- **The finding.** The driver records every `CCGPU_DEBT_FAULT`, and `shutdown` turns them into a session-fault finding. The only exception is `closeExpecting(SessionFault)`, which:
  - excuses only the declared kind;
  - requires that kind to have occurred.
- **Chrome mutants** (Amelia-4, `evidence-9r-0a/seeded/`):
  - **undeclared:** clock-master fails with debtFaults=1 and "The simulation stopped on 1 debt fault(s)";
  - **absent:** clock-master fails with "expected to raise DebtFault, but it did not".
- **Run of record `clock-master.tap`:** the one debt fault comes from test 5, the new 600 ms Running block. Test 4, the 4000 ms spin, shows only `SimulationUnresponsive`.
- **The AC's premise.** The AC assumed the 4000 ms spin would raise a debt fault. Measurement shows it does not, so the spin needs no exception. That is a wording deviation, not a breach.

### Full run reused: `evidence-9r-0a/parallel-full-run-head` (run `20261019T143851Z-3607`)

- **The run:**
  - Pass: 34 files, 166/166.
  - 1085.3 s at N = 2, against a historical serial baseline of 26 min 17 s.
  - Bundle `d580844d…` at start and end, with both served checks passing.
- **Identity, recorded vs recomputed by Murdoch-5 from `git ls-tree f79705aa` and from disk:**
  - `sourceHead` is `f79705aa`;
  - suite `f387e494…` matches;
  - runner `feee69be…` matches.
- **The committed copy.** `git show d8e071f9:…/parallel-full-run-head/summary.txt` has the same run id, `sourceHead`, suite, runner and bundle values, and the same 34/166 totals.
- **Applicability to `d8e071f9`.** The fix changes only `Selection.cs`, `SelectionTests.cs`, the spec and evidence. The `full` verb never calls `Selector`, so the run of record still applies (coordinator decision).
  - The reuse rests on that source argument alone, not on bundle identity. Publishes are not reproducible across runs (F14).
- **The 1065 s pre-commit run is not candidate evidence.** `evidence-9r-0a/parallel-full-run` ran on a dirty `c854b4d5` with a different `tools/` tree (suite `f1604e51…`).

## Findings

### Blocking

- **B1, resolved at `d8e071f9`.** Tier 2 integration ignored the parts that a suite's declared levels pre-place (spec D6/A30/M9). The case was `domino_effect`'s locked bowling ball, which dropped cat-023b and clock-master.
  - Reproduced at `f79705aa` and verified fixed at `d8e071f9` (above).
  - Amelia-4's B1 mutant is recorded as killed. Murdoch-5 saw the new row pass and did not rerun that mutant.

### Filed (non-blocking), owner Story F.1 unless stated

1. **F1: no requalification pin.** There is no `ParallelQualifications.cs` digest pin (spec D4). An edited or new suite can be marked Parallel with no calibration; the only guard is the lexical detector test.
2. **F2: lexical timing detector kept.** `TimingDetector.cs` keeps the lexical heuristic, which the spec says "Never" to, as the Parallel guard. The Spec Change Log does not record this deviation from the frozen intent. Owner: F.1, or the coordinator to ratify.
3. **F3: the runner's pass rule ignores the debt-fault counter.** `TapResult.Failures` does not use `debtFaults` (the `pass.test.ts` fixture passes with one), so enforcement is driver-only. Spec D3 said "the pass rule requires 0".
4. **F4: `closeExpecting` is not an exact count.** It excuses ≥ 1 of the declared fault rather than exactly one. Clock-master asserts its own count, but nothing pins call sites or the count.
5. **F5: weak source identity.** Non-`tools/` sources are identified only by HEAD plus a porcelain status digest, which covers names, not content. A dirty product file edited mid-run goes undetected.
6. **F6: `--changed` diffs against HEAD.** A committed edit to `content/puzzles.json` or `parts/elements.bundle.json` selects only the smoke pair. The DoD path (`--diff <base>`) is unaffected.
7. **F7: partners without a Primary are skipped.** A port partner with no Primary suite is skipped silently. The spec wanted `NoPrimarySuite`; the epics cut text narrowed it to the changed element.
8. **F8: no `NodeUnit` kind.** `tools/e2e/unit/**` resolves to Full(Unknown). That over-selects, which is safe.
9. **F9: skip and todo tests use synthetic TAP.** The real format was checked manually (criterion 5).
10. **F10: seeded-failure and seeded-hang runs.** The AC5 seeded-failure and seeded-hang Chrome runs are already filed to F.1 by the coordinator.
11. **F11: the spec still has superseded text.** D8 slots, D11 checkpoint, AC6, the D5 2N rule and its lowest-projected-wall choice remain. F.1 should own the rewrite.
12. **F12: the B1 fix's level-parts reader is unguarded.** `ImpactContext.ReadLevelParts` parses `content/puzzles.json` without the typed-catch fallback `Levels()` has. With an unparsable file, an element change throws an unhandled `JsonException` instead of selecting Full. It fails loudly, never silently. The fix also moved the `Levels` doc comment onto the `_levelParts` field.
13. **F13: a worktree outside the repo cannot resolve Node modules.** Suites depend on an untracked repo-root `node_modules` (`flatbuffers`). The runner's toolchain check does not detect a missing module before Chrome runs, so a clean worktree outside the repository fails every file. That is loud, not silent.

14. **F14: publishes are not reproducible.** Two publishes of the same clean `d8e071f9` gave different bundle identities: `eec53721…` from outside the repo and `e88c33db…` from inside it. Bundle identity therefore proves consistency within one run only. It cannot anchor evidence reuse across runs or candidates.

Two items from the provisional record are closed: the uncommitted evidence (now committed in `d8e071f9`) and the scratch scripts (coordinator decision).

## Verdict

**PASS (scoped).** Every cut-scope criterion is met at `d8e071f9`. The one blocking finding (B1) is fixed and independently verified. No player-visible regression is possible from this diff. F1–F14 are non-blocking follow-ups. The coordinator should commit this record together with `evidence-9r-0a-review-murdoch5/`, which is untracked in the main checkout.
