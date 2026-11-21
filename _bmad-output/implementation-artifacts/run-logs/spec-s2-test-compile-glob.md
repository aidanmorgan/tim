# Run log: S2 test project compile list becomes a glob

Append-only. Contract: `_bmad-output/implementation-artifacts/spec-s2-test-compile-glob.md`.

## Handled owner messages

- 21 November 2026: "make all of the changes you have identified" (after the second proposal's structural stories were queued). Handled by starting S2 as the first structural slice.

## E1 Session environment check (21 November 2026)

- Host default `node --version` is v26.13.1 (Homebrew), not the pinned v26.12.1. Fixed for this slice by putting `/private/tmp/node-v26.12.1-darwin-arm64/bin` first on `PATH`, the install earlier slices used: `node --version` = v26.12.1.
- `/private/tmp/tim-dotnet-10.0.403/dotnet --version` = 10.0.403, matching `global.json`; `dotnet workload list` shows `wasm-tools`.
- `--bmad-check --route Auto`: structural findings 0; three `when` conditions reported for the tiered layers, read against protocol Review tiers.
- Restore runs with the owner's first build.

## E2 Planning

- Route: freeform intent from `docs/planning/invest/delivery-batches.md` S2. Tree clean at baseline `7c019d64ee54a80bc0d31b82c90533985c2748ba`.
- Carry-forward read: `deferred-work.md` lines 258 and 264 (S2 pointer and the registration-derivation correction). No unresolved dependency.
- In flight elsewhere: Bellows 12.4 and registration-derivation. This slice was started on the owner's instruction above while they run; publication is serialised and the later publisher merges main once.

## E3 Planning audit

- Planning auditor `a5936bbe302620268` (fresh context, no history). Pass 1 Incomplete: F1 M1 expected count wrong for both methods (package compile-in items), F2 the `EnableDefaultCompileItems` option contradicted the Never clause and M3; F3 untracked test file is silent in the hook snapshot, F4 probe cost, F5 doc wording. Contract amended. Pass 2 Scoped Pass, with N1 (name the baseline total's source) and N2 (probe first) adopted in Verification.

## E4 Implementation owner

- Owner `a5f2fbba28f1455a5`, launched fresh with no history, inherited model. Review fixes return to this owner.

## E5 Implementation: baseline capture and probes (21 November 2026)

All commands by the owner, from the checkout root, with the contract's toolchain (`node` v26.12.1, `dotnet` 10.0.403, `wasm-tools` listed). Raw output is under `.tools/s2-evidence/` (ignored); compiler logs are under `/private/tmp/claude-501/-Users-aidan-dev-personal-tim/37b1f527-b6f7-4082-bfd2-686d62e814b3/scratchpad/` in `s2-baseline-logs`, `s2-probe-logs` and `s2-candidate-logs`.

- Capability probe checked against the code before the first edit: nothing pins the explicit list; `CSharpStaticGateTests.RepositoryFiles` lists tracked and untracked non-ignored files, so an untracked probe is seen. No plain test file name appears in any table outside the project file. No missing capability.
- Baseline, before any project edit: restore, then the playtest and solution builds with `--no-incremental` and `-bl:` into `s2-baseline-logs`. Both succeeded, 0 warnings. `baseline-build.log`.
- Baseline `dotnet test CuriousContraptions.slnx -c Debug -f net10.0 --no-build --no-restore --list-tests --filter <deferred filter>`: 7028 listed names. `baseline-list-tests.txt`.
- Edit: the 236 local `Compile` items replaced by `<Compile Include="*.cs" />`; `EnableDefaultCompileItems` stays false; the two linked items stay.
- Probes (M2, M3): `CuriousContraptions.tests/S2ProbeTopLevelTests.cs` and `CuriousContraptions.tests/ProbeOutsideRule/S2ProbeOutsideRuleTests.cs`, one passing fact each, no project edit. Five builds into `s2-probe-logs`, 0 warnings (`probe-build.log`). `dotnet test CuriousContraptions.tests/CuriousContraptions.tests.csproj --no-build --filter "FullyQualifiedName~S2Probe|FullyQualifiedName~CSharpStaticGateTests"`: 94 total, 93 passed, 1 failed. The top-level fact passed. The one failure is `EveryRepositoryCSharpFileIsCompiledByAProject`, "1 C# static gate violation(s): CuriousContraptions.tests/ProbeOutsideRule/S2ProbeOutsideRuleTests.cs: compiled by no project". The subdirectory fact was not discovered. `probe-test.log`.
- Both probe files and the probe directory deleted; `git status --short` then showed only owned paths.

## E6 M1 source-list comparison

- Method: each binlog replayed to text with `dotnet msbuild <log> -noconlog -flp:logfile=...;verbosity=normal`; the single `Csc` command line whose output is `obj/Debug/net10.0/CuriousContraptions.tests.dll` split into arguments, and those ending `.cs` kept. Repository sources are the arguments that are neither absolute nor under `obj/`, written relative to the repository root.
- Result, identical for `solution.binlog` and `playtest.binlog`: baseline 266 arguments, candidate 266, no difference in either direction; 238 repository sources on both sides, no difference (236 under `CuriousContraptions.tests/`, plus `CuriousContraptions.Animation.Worker/RuntimeProviders.cs` and `CuriousContraptions.web/RuntimeProviders.cs`). The candidate's 236 equal the directory listing. Files: `{baseline,candidate}-{solution,playtest}-sources-{full,repo}.txt`, `m1-*.diff` (all empty).

## E7 Candidate compiler logs

- After the probes were deleted: the five compiler-log builds of `docs/delivery-workflow.md#local-test-authority` into `s2-candidate-logs` (playtest, solution, web, `CheckStagedTests.cs`, `VerifyPolicy.cs`), all succeeded with 0 warnings. `candidate-build.log`. These are the logs every later check used.

## E8 Maintainability gate and registration check

- `dotnet run --file tools/anvil/VerifyPolicy.cs -- --maintainability --project-root "$PWD"`: exit 0. Both canary `PASS` lines present; `Coupling dependencies: contract=4633, model=4, intrusive=0; violations=0`; `violations=0 (metrics=0, coupling=0). PASS`. `maintainability-gate.log`.
- Registration check (the filter in `docs/delivery-workflow.md#new-package-registration-check`) with `CSHARP_STATIC_GATE_BINLOGS` set to `s2-candidate-logs`: 716 passed, 0 failed, `Skipped: 0` in all five projects (17, 33, 190, 85, 391). `registration-check.log`.
- Candidate `--list-tests` with the same filter as the baseline: 7028 names, identical to the baseline's as a sorted list. `candidate-list-tests.txt`, `list-tests.diff` (empty).

## E9 Whitespace, scope and self-check

- `git status --short` showed no untracked `CuriousContraptions.tests/*.cs`. `git add -N` on the contract and this run log, then `git diff --check 7c019d64ee54a80bc0d31b82c90533985c2748ba`: clean.
- `git diff --stat` against the baseline: the test project file (9 insertions and 239 deletions across all five files), `_bmad-output/implementation-artifacts/deferred-work.md`, `_bmad/custom/bmad-loop-protocol.md`, `docs/delivery-workflow.md`, `docs/planning/invest/delivery-batches.md`, plus the contract and this log. No test or production source.
- Self-check of the full owned diff: no needless branch, duplicate rule or unrelated churn. Notes: a comment inside the old list ("The shipped worker's physics engine and transport, stepped natively (Story 9.4b)") was removed with the entries it annotated; `deferred-work.md` line 262 "None has started" became "None has started except S2"; the delivery-batches S2 story gained a Status line and its "Now" bullet became "Before (baseline `7c019d64`)". `TODO.md` was not edited: it still describes Windmill, and its new content is the review outcome, which the coordinator owns.

## E10 Full local round

- `caffeinate -i dotnet test CuriousContraptions.slnx -c Debug -f net10.0 --no-build --no-restore --nologo -noAutoResponse --filter <deferred filter> -- RunConfiguration.TreatNoTestsAsError=true`, with `CSHARP_STATIC_GATE_BINLOGS` set to `s2-candidate-logs`: exit 0. WorkerAotGuard.Tests 17, Coverage.Tests 80, BundleElements.Tests 285, E2eRunner.Tests 244, CuriousContraptions.tests 6606; total 7232, 0 failed, `Skipped: 0` in each. `full-round.log`.
- Baseline total: the contract names the baseline's `--list-tests` count as the comparison. That count is 7028 on both sides, while the round reports 7232; listing and running count differently, and the cause of the 204 difference was not investigated (it is the same on the baseline's listing, so it does not come from this change). The like-for-like comparisons are the identical listed names (E8) and the round total equal to the 7232 recorded for the Windmill candidate in `TODO.md`, whose test sources this baseline contains.

## E11 Review pass 1 repair (21 November 2026)

Owner `a5f2fbba28f1455a5`, re-engaged. Raw output under `.tools/s2-evidence/`. No full round was run; the second round belongs to the final candidate.

- Intent-to-add checked by running it (git 2.56.1): with `git add -N` on a 105-byte top-level probe, `git checkout-index --all --prefix=<dir>/` exits 0 and writes the path as a 0-byte file (index entry `e69de29b…`, the empty blob). The file is therefore present but empty in the hook's snapshot, not absent: it compiles there with none of its tests. `git ls-files --cached` and `git diff-files --name-only --diff-filter=A` both list it. `hook-ita-checkout-index.txt`.
- `.githooks/pre-commit`: one guard before the snapshot is made. It lists the `*.cs` files directly in `CuriousContraptions.tests/` on disk, takes the index entries with content (cached paths minus intent-to-add paths), and refuses the commit, naming each file on disk that is not among them. No option, environment switch or bypass.
- Positive: `CSHARP_GATE_STAGED_HOOK=1 sh .githooks/pre-commit` with an untracked probe (`S2HookProbeUntracked.cs`) and an intent-to-add probe (`S2HookProbeIntentToAdd.cs`): exit 1 at the guard, both named, nothing else named. `hook-guard-positive.txt`.
- Control: both probes deleted and the intent-to-add entry removed from the index; the guard's lines, taken verbatim from the hook by line range and run under `set -eu`: exit 0, 236 top-level files, none reported. `hook-guard-control.txt`. Limit: the control ran the guard's lines, not the whole hook, because the rest of the hook is the snapshot build and its tests; an ignored-file probe was not run separately (the guard reads the directory, not git's untracked list, so ignore rules cannot hide a file from it).
- Not changed: `CSharpStaticGateTests.TheHookKeepsEachProtection` pins hook fragments but test sources are outside this slice's scope, so no test pins the new guard. `CSharpStaticGateTests` rerun against the E7 logs after the hook edit: 93 passed, 0 failed, `Skipped: 0`. `repair-static-gate.log`.
- `docs/delivery-workflow.md` registration paragraph: a new test file must be staged with `git add` and the hook refuses otherwise; `git add -N` serves `git diff --check` alone; a new test file still gets the registration check; a deleted test file is no longer refused by the build and is caught as a deleted path; "failed on these" now names the hand-maintained tables.
- `docs/planning/invest/delivery-batches.md` S2 "Before" bullet: past tense, with `EnableDefaultCompileItems` stays false in the present. `TODO.md`: replaced with the current S2 brief.
- Contract: two hook-guard rows in the acceptance-to-test table; the executor line no longer says `TODO.md` is left to the coordinator.
- `dotnet run --file tools/anvil/VerifyPolicy.cs -- --bmad-check --project-root "$PWD" --route Auto`: exit 0, `Auto: structural findings=0`, the same three `when` conditions reported for review. `bmad-check-repair.log`.
- `git diff --check 7c019d64ee54a80bc0d31b82c90533985c2748ba`: clean.

## E12 Hook guard pin (21 November 2026)

Owner `a5f2fbba28f1455a5`, re-engaged, under the contract's Spec Change Log reading of the Never clause. Raw output under `.tools/s2-evidence/`. No full round was run.

- `CuriousContraptions.tests/CSharpStaticGateTests.cs`: one `InlineData` added to `TheHookKeepsEachProtection`, in the style of the existing fragments. It pins the guard's ten lines from `tests_on_disk=` to `fi`, including the two comments between them and the refusal message. Nothing else in any test source changed. After E11 the hook's lead comment had been corrected (an intent-to-add file is present but empty in the snapshot); the guard's code lines were not changed by that.
- Five compiler-log builds, `--no-incremental`, into `s2-candidate2-logs` beside the earlier log folders: all succeeded, 0 warnings. `candidate2-build.log`. These replace the E7 logs.
- `dotnet test CuriousContraptions.tests/CuriousContraptions.tests.csproj -c Debug --no-build --no-restore --filter "FullyQualifiedName~CSharpStaticGateTests"` against those logs: 94 total, 94 passed, including the new fragment's case. `repair2-static-gate.log`.
- Hook-guard control repeated on the lines as they now stand, no probes: exit 0, 236 top-level files, none reported. `hook-guard-control-2.txt`. The positive was not repeated: the guard's code lines are those of E11.
- Maintainability gate: exit 0, both canary `PASS` lines, `Coupling dependencies: contract=4633, model=4, intrusive=0; violations=0`, `violations=0 (metrics=0, coupling=0). PASS`. `maintainability-gate-2.log`.
- `git diff --check 7c019d64ee54a80bc0d31b82c90533985c2748ba`: clean.

## E13 Review pass 2 amendment: staged-source rule in the policy tool (21 November 2026)

Owner `a5f2fbba28f1455a5`, re-engaged. Raw output under `.tools/s2-evidence/`; compiler logs in `s2-candidate3-logs` beside the earlier log folders. The E11 shell guard and the E12 pin are superseded and removed.

- Returned to planning before the first edit: the no-argument harness in `tools/anvil/VerifyPolicy.cs` runs `CheckStagedTests.cs` inside a disposable repository where the tool's own copy is an untracked C# file, and its "empty index" case leaves `SampleTests.cs` untracked, so the new rule would have refused every case. The coordinator added the harness to scope with the planning auditor's B1 fix.
- `tools/anvil/CheckStagedTests.cs`: `StagedSourceRule.Decide` makes the two new-file queries and the staged query, and refuses when a query fails or a new C# file is untracked or intent-to-add; it runs before the staged-source early return. The real queries run with the repository top level as working directory. Nine fixtures run first on every invocation against a fake that answers only the three exact argument lists, held as independent literals. Limit: the fake models git's answers; the upper-case, ignored and non-C# fixtures prove the exact queries and that the rule adds no filter of its own, and git's own behaviour for those cases is shown by the real-tree run below.
- `tools/anvil/VerifyPolicy.cs`, no-argument harness only: the tool is copied to a second temporary directory outside the disposable repository and run from there; `EmptyRepository` empties the index and deletes both sample files at the start of every case and before "empty index"; one new case; count `cases.Length + 7`. The registry file stays inside the disposable repository.
- `.githooks/pre-commit` and `CuriousContraptions.tests/CSharpStaticGateTests.cs`: `git diff 7c019d64ee54a80bc0d31b82c90533985c2748ba --stat` on the two paths is empty. `a3-baseline-equal.txt` (0 bytes).
- `tools/anvil/README.md`: new "Staged-source rule" section; "Verified on" restated from the rerun (Anvil 0.14.3-alpha.6, .NET SDK 10.0.403, 22 checks). `docs/delivery-workflow.md`: the staging sentence names the policy tool, every new C# file, and who replaces intent-to-add with `git add`.
- Five compiler-log builds, `--no-incremental`, into `s2-candidate3-logs`: all succeeded, 0 warnings. `candidate3-build.log`.
- Maintainability gate: exit 0, both canary `PASS` lines, `Coupling dependencies: contract=4633, model=4, intrusive=0; violations=0`, `violations=0 (metrics=0, coupling=0). PASS`. `maintainability-gate-3.log`.
- Registration check on those logs: 716 passed, 0 failed, `Skipped: 0` in all five projects (17, 33, 190, 85, 391). `registration-check-3.log`.
- `dotnet run --file tools/anvil/VerifyPolicy.cs` (no arguments): exit 0, 22 `PASS` lines including "empty index" and "empty index with an unstaged new C# file refused"; `Policy fixtures: Passed: 22, Failed: 0, Skipped: 0`. `verify-policy-harness.log`.
- `dotnet run --file tools/anvil/VerifyPolicy.cs -- --bmad-check --self-test --project-root "$PWD"`: exit 0, `Fixtures: Passed: 37, Failed: 0, Skipped: 0`. `bmad-self-test-3.log`. `--bmad-check --route Auto`: exit 0, structural findings 0. `bmad-check-3.log`.
- Real-tree positive, nothing staged (`git diff --cached --name-only` empty): probes `CuriousContraptions.tests/S2RuleProbeUntracked.cs` (untracked), `tools/Coverage.Tests/S2RuleProbeIntentToAdd.cs` (`git add -N`), `CuriousContraptions.tests/S2RULEPROBEUPPER.CS` (untracked), `.tools/s2-evidence/S2RuleProbeIgnored.cs` (ignored directory) and `CuriousContraptions.tests/S2RuleProbeNotCSharp.cs.txt`. `dotnet run --file tools/anvil/CheckStagedTests.cs`: fixtures 9 passed, then exit 1 naming exactly the first three. `rule-positive.txt`.
- Real-tree control: all five probes deleted and the intent-to-add entry removed; fixtures 9 passed, exit 0. `rule-control.txt`.
- `git diff --check 7c019d64ee54a80bc0d31b82c90533985c2748ba`: clean. Index before the round: only the intent-to-add entries for the contract and this log. `git diff --stat`: eleven paths, all in the owned scope list; no test, engine or element source. `diff-stat-3.txt`.
- Full local round under `caffeinate -i` on the E13 logs: exit 0. WorkerAotGuard.Tests 17, Coverage.Tests 80, BundleElements.Tests 285, E2eRunner.Tests 244, CuriousContraptions.tests 6606; total 7232, 0 failed, `Skipped: 0` in each. `full-round-3.log`.
- Not run: the whole pre-commit hook on this candidate (it runs the staged copy of itself and the tool from an index snapshot, so its first run is the candidate commit); `--list-tests` was not repeated, since no test source differs from the baseline.
- After the round, only this log, the contract and `TODO.md` were edited.

## E14 Review pass 3 repair: when the staged-source rule runs, and what proves it (21 November 2026)

Owner `a5f2fbba28f1455a5`, re-engaged, implementing Spec Change Log entry 3 and the patch rows of review pass 3. Raw output under `.tools/s2-evidence/`; compiler logs in `s2-candidate5-logs` beside the earlier log folders.

- `tools/anvil/CheckStagedTests.cs`: the intent-to-add query and the staged query run on every commit; the untracked query and the ignored-files query (`ls-files --others --ignored --exclude-standard --directory`, entries ending in `/` not refused) run only when a scanned source is staged. Message: stage or move outside the tree, do not delete a file you do not own. Seven in-tool cases; one self-check line, `CheckStagedTests: the staged-source rule held its 7 self-check cases.` Extension lookup is case-insensitive. The skip scan moved into its own member.
- Anvil comment (BH8): checked by running Anvil 0.14.3-alpha.6 with the custom registry on the same skipped test as `Sample.cs` and `sample.ts`: 0 findings for the C# file, 1 error for the TypeScript file. The comment's claim still holds; it now names the checked version. `anvil-csharp-dispatch.txt`.
- `tools/anvil/VerifyPolicy.cs`, no-argument harness only: ten real-git staged-source cases and the `.CS` skip-scan case in `StagedSourceHarness`; ignore rules in the disposable repository's `.git/info/exclude`; per-case reset empties the index and the working tree apart from `.git`, `.git-hooks` and `tools`; the generated hook quotes the tool path safely; cleanup reports a failed delete, fails the run, and still removes the other directory. The earlier single "empty index with an unstaged new C# file refused" case is replaced, because that commit now passes by design (case "an untracked C# file passes when no source is staged").
- Documents: `tools/anvil/README.md` (when the rule fires, the hook's real order, the cheap check command, counts, "Verified on" restated: Anvil 0.14.3-alpha.6, git 2.56.1, .NET SDK 10.0.403, 32 checks); `docs/delivery-workflow.md` (the conditional rule, the remaining hazard, no standing check for a deleted test file); `deferred-work.md` ("S1 is in flight in another session and S2 is delivered as noted below; S3 to S6 have not started").
- First attempt at the gate on this candidate: FAIL, `FAIL MI 18 tools/anvil/VerifyPolicy.cs:12 Program` (`maintainability-gate-4.log`, on the `s2-candidate4-logs` builds). Cause: the harness's helpers were top-level local functions, which count toward `Program`. Repair: the process runner, per-case reset, shell quoting and cleanup moved into a `Harness` class and `StagedSourceHarness` calls it directly. No threshold, exclusion or suppression changed. A check run straight after the refactor passed (`maintainability-gate-4b.log`).
- Final candidate, in handoff order. Five compiler-log builds, `--no-incremental`, into `s2-candidate5-logs`: all succeeded, 0 warnings. `candidate5-build.log`.
- Maintainability gate: exit 0, both canary `PASS` lines, `Coupling dependencies: contract=4633, model=4, intrusive=0; violations=0`, `violations=0 (metrics=0, coupling=0). PASS`. `maintainability-gate-5.log`.
- Registration check on those logs: 716 passed, 0 failed, `Skipped: 0` in all five projects. `registration-check-5.log`.
- `dotnet run --file tools/anvil/VerifyPolicy.cs` (no arguments): exit 0, 32 `PASS` lines, `Policy fixtures: Passed: 32, Failed: 0, Skipped: 0`. `verify-policy-harness-5.log`. `--bmad-check --self-test`: 37 passed. `bmad-self-test-5.log`. `--bmad-check --route Auto`: structural findings 0. `bmad-check-5.log`.
- Real tree, probes `CuriousContraptions.tests/S2RuleProbeUntracked.cs` (untracked) and `tools/Coverage.Tests/S2RuleProbeIntentToAdd.cs` (`git add -N`). Nothing staged: exit 1 naming only the intent-to-add probe (`rule5-nothing-staged.txt`). One source staged (`tools/anvil/CheckStagedTests.cs`, staged for the run and unstaged after it): exit 1 naming both (`rule5-source-staged.txt`). Probes deleted, intent-to-add entry removed, the same source staged: self-check, skip scan with 0 findings, exit 0 (`rule5-control.txt`); unstaged again.
- `git diff --check 7c019d64ee54a80bc0d31b82c90533985c2748ba`: clean. `git diff --stat` on `.githooks/pre-commit` and `CuriousContraptions.tests/CSharpStaticGateTests.cs`: empty (`baseline-equal-5.txt`, 0 bytes). Eleven changed paths, all in the owned scope list (`diff-stat-5.txt`). Index before the round: only the intent-to-add entries for the contract and this log.
- Full local round under `caffeinate -i` on the E14 logs: exit 0. WorkerAotGuard.Tests 17, Coverage.Tests 80, BundleElements.Tests 285, E2eRunner.Tests 244, CuriousContraptions.tests 6606; total 7232, 0 failed, `Skipped: 0` in each. `full-round-5.log`.
- Not run: the whole pre-commit hook on this candidate; `--list-tests` (no test source differs from the baseline). The mechanical renames that followed the `Harness` move (call sites to `Harness.Execute` and the like) were written without a separate validation call each; the moved code itself was validated.
- After the round, only this log, the contract and `TODO.md` were edited.

## E15 Planning verdict on Spec Change Log entry 3 (21 November 2026)

- Planning auditor `a5936bbe302620268`, terminal verdict on entry 3 as amended with (g) and the pass-4 refinement of (a): Scoped Pass, relayed by the coordinator with four conditions. (1) A deleted path is never skip-scanned or read with `git show`: the scan set keeps its `ACMR` query and only the trigger uses a query that includes deletions. (2) The trigger query carries `--no-renames`. (3) A case proves that a staged deletion triggers the queries. (4) A case proves that a staged build input triggers them. How each is met is in E16.

## E16 Review pass 4 patches and the merge attempt (21 November 2026)

Owner `a5f2fbba28f1455a5`, re-engaged. Raw output under `.tools/s2-evidence/`; compiler logs in `s2-candidate6-logs` beside the earlier log folders.

- `tools/anvil/CheckStagedTests.cs`. Conditions 1 and 2: the scan set is still `diff --cached --name-only --diff-filter=ACMR -z`; a separate trigger query, `diff --cached --name-only --no-renames --diff-filter=ACMRD -z`, decides whether the commit stages source, which now means any path the tool scans or a build input (`.csproj`, `.props`, `.targets`, `.slnx`, any case). BH2: the message names `git add -f` for a file ignored by name. BH8: a failed query refuses with `git <arguments>: <git's error text>`; git's standard error is captured. Thirteen in-tool cases: the six earlier decisions, a staged deletion (condition 3), a staged build input (condition 4), and one case per failing query (BH6; five, because the staged question is now two queries), each asserting the exact refusal text.
- `tools/anvil/VerifyPolicy.cs`, no-argument harness only. BH5: a refusal's indented lines are compared with the expected set exactly. Three more real-git cases: an untracked C# file beside a staged deletion (the base commit is made with plumbing, so no hook runs or is bypassed), beside a staged `App.csproj`, and EC3. Each case starts on a branch that is never created, so no case inherits a commit. EC5: `.git/info` is created before the exclude file is written.
- EC3 result: a C# file ignored by name that is the only content of an untracked directory is listed by git as `fresh-dir/IgnoredByName.cs`, not as `fresh-dir/`, and is refused. No repair was needed; the case stays as proof.
- Documents. `tools/anvil/README.md`: what counts as staged source, `git add -f`, the hazard wording, a failed query, thirteen in-tool cases, "Verified on" restated (35 checks, thirteen real-git cases). `docs/delivery-workflow.md`: the same hazard wording, and the no-argument harness as a required command whenever a file under `tools/anvil/` changes (VG1). `_bmad/custom/bmad-loop-protocol.md` handoff step 1: the same command. `TODO.md` restated (IA8).
- Five compiler-log builds, `--no-incremental`, into `s2-candidate6-logs`: all succeeded, 0 warnings. `candidate6-build.log`.
- Maintainability gate: exit 0, both canary `PASS` lines, `Coupling dependencies: contract=4633, model=4, intrusive=0; violations=0`, `violations=0 (metrics=0, coupling=0). PASS`. `maintainability-gate-6.log`.
- Registration check on those logs: 716 passed, 0 failed, `Skipped: 0` in all five projects. `registration-check-6.log`.
- `dotnet run --file tools/anvil/VerifyPolicy.cs` (no arguments): exit 0, 35 `PASS` lines, `Policy fixtures: Passed: 35, Failed: 0, Skipped: 0`. `verify-policy-harness-6.log`. `--bmad-check --self-test`: 37 passed. `bmad-self-test-6.log`. `--bmad-check --route Auto`: structural findings 0. `bmad-check-6.log`.
- Real tree, same two probes as E14. Nothing staged: 13 self-check cases held, exit 1 naming only the intent-to-add probe (`rule6-nothing-staged.txt`). One source staged and unstaged after the run: exit 1 naming both (`rule6-source-staged.txt`). Probes deleted, the same source staged: exit 0, skip scan with 0 errors (`rule6-control.txt`, `rule6-control-tool-output.txt`); unstaged again.
- `git diff --check 7c019d64ee54a80bc0d31b82c90533985c2748ba`: clean. `.githooks/pre-commit` and `CuriousContraptions.tests/CSharpStaticGateTests.cs` against the baseline: empty diff (`baseline-equal-6.txt`, 0 bytes). Eleven changed paths, all in the owned scope list (`diff-stat-6.txt`).
- Merge. `git fetch origin`: `origin/main` is `8d1d68aa262bb3f89ee9d073bbbaf3a83b9f9322`; the merge base is this branch's HEAD `7c019d64`, so main is a fast-forward of it. Main changes 18 paths; three are also changed here: `TODO.md`, `_bmad-output/implementation-artifacts/deferred-work.md`, `docs/delivery-workflow.md` (`main-changed-paths.txt`). Main does not change the test project file. `git merge --no-commit origin/main`: exit 1, "error: Your local changes to the following files would be overwritten by merge: TODO.md, _bmad-output/implementation-artifacts/deferred-work.md, docs/delivery-workflow.md. Please commit your changes or stash them before you merge. Aborting" (`merge-attempt.txt`). HEAD and the working tree are unchanged. Stopped there as instructed; the coordinator decides the route.
- Not run: the full local round on this candidate, because the candidate that review and acceptance rely on is the merged one; main's expected total and the listed-names comparison, which need main's test sources; the whole pre-commit hook.
- Validation note: two one-word edits ("seven" to "thirteen" in the README, the `Placement` enum member) were written without their own validation call.

## E17 Merge of published main and the handoff order on the merged candidate (21 November 2026)

Owner `a5f2fbba28f1455a5`, re-engaged, on the coordinator's route. Raw output under `.tools/s2-evidence/` (merge files in its `merge/` folder); compiler logs in `s2-main-logs` and `s2-candidate7-logs` beside the earlier log folders.

- Identities: original baseline `7c019d64ee54a80bc0d31b82c90533985c2748ba` (the contract's `baseline_revision`, unchanged); new comparison base and HEAD `8d1d68aa262bb3f89ee9d073bbbaf3a83b9f9322`. The candidate now includes main.
- Safeguards: `git diff 7c019d64… --output=` patches for `TODO.md`, `deferred-work.md` and `docs/delivery-workflow.md`, the whole candidate diff (`whole-candidate.patch`, 1398 lines) and plain copies of the three files, all in `merge/`.
- `git checkout HEAD -- TODO.md _bmad-output/implementation-artifacts/deferred-work.md docs/delivery-workflow.md`, then `git merge --ff-only origin/main`: exit 0, HEAD `8d1d68aa…` (`merge/merge-ff.txt`). The other eight owned paths have the same SHA-256 before and after (`merge/other-paths-before.sha`).
- Re-applied by hand onto main's text. `docs/delivery-workflow.md`: main's sentence now reads "the explicit Compile lists of the projects that have them remain required", followed by the test project's rule, the staging rule, the remaining hazard and the deleted-file note; main's static-gate clause gains "such as one in a subdirectory of the test project"; the `tools/anvil/` harness command is restated in local verification. The slice's earlier sentence about the hand-maintained tables is dropped, because main removed that history sentence. `deferred-work.md`: main's S1 outcome untouched; "S2 is delivered as noted below; S3 to S6 have not started" and the S2 pointer marked delivered. `TODO.md`: this slice's brief, carrying forward main's one pending item for its other owner.
- Does main affect this slice's code: main changes none of the test project file, `tools/anvil/`, `.githooks/pre-commit`, `CSharpStaticGateTests.cs`, `_bmad/custom/` or `delivery-batches.md`. Main's test project file is byte-identical to the original baseline's, and its directory holds the same 236 C# file names (six modified, none added or removed).
- M1 on the new basis. Main's explicit-list project file was checked out over the candidate's (`git checkout HEAD -- CuriousContraptions.tests/CuriousContraptions.tests.csproj`), the playtest and solution builds made into `s2-main-logs` (0 warnings, `main-build.log`) and `--list-tests` run (7042 names, `main-list-tests.txt`); the candidate file was then written back and compared byte-for-byte with its saved copy (`merge/tests.csproj.candidate`). Against the merged candidate's logs: 266 `Csc` `.cs` arguments and 238 repository sources on both sides, no difference in either direction in either log; the candidate's 236 equal the directory listing. `{main,merged}-{solution,playtest}-sources-{full,repo}.txt`, `m1b-*.diff` (empty).
- Five compiler-log builds of the merged candidate, `--no-incremental`, into `s2-candidate7-logs`: all succeeded, 0 warnings. `candidate7-build.log`.
- Listed names on the merged candidate: 7042, identical to main's explicit-list build. `merged-list-tests.txt`, `list-tests-main-vs-merged.diff` (empty).
- Maintainability gate: exit 0, both canary `PASS` lines, `Coupling dependencies: contract=4633, model=4, intrusive=0; violations=0`, `violations=0 (metrics=0, coupling=0). PASS`. `maintainability-gate-7.log`.
- Registration check with main's filter (18 classes) on those logs: 730 passed, 0 failed, `Skipped: 0` in all five projects (17, 33, 190, 97, 393); 730 is main's recorded figure. `registration-check-7.log`.
- `dotnet run --file tools/anvil/VerifyPolicy.cs` (no arguments): exit 0, 35 `PASS` lines. `verify-policy-harness-7.log`. `--bmad-check --self-test`: 37 passed. `bmad-self-test-7.log`. `--bmad-check --route Auto`: structural findings 0. `bmad-check-7.log`.
- Real tree, same two probes. Nothing staged: exit 1 naming only the intent-to-add probe (`rule7-nothing-staged.txt`). One source staged and unstaged after: exit 1 naming both (`rule7-source-staged.txt`). Probes deleted, same source staged: exit 0, 0 errors (`rule7-control.txt`); unstaged again.
- `git diff --check 8d1d68aa…`: clean. `.githooks/pre-commit` and `CuriousContraptions.tests/CSharpStaticGateTests.cs` against main: empty diff (`baseline-equal-7.txt`, 0 bytes). `git diff --stat 8d1d68aa…` before the round: ten paths, all owned (`diff-stat-7.txt`); `TODO.md` became the eleventh after the round.
- Full local round under `caffeinate -i` on the E17 logs: exit 0. WorkerAotGuard.Tests 17, Coverage.Tests 80, BundleElements.Tests 285, E2eRunner.Tests 256, CuriousContraptions.tests 6608; total 7246, 0 failed, `Skipped: 0` in each. 7246 is main's recorded full round. `full-round-7.log`.
- Not run: the whole pre-commit hook on this candidate. Noted, not changed: `docs/planning/invest/delivery-batches.md` still heads S1 "in flight", which main did not update and this slice does not own beyond its S2 story.
- After the round, only this log, the contract and `TODO.md` were edited.

## E18 Review pass 5 patches and the second full round on the final candidate (21 November 2026)

Owner `a5f2fbba28f1455a5`, re-engaged. Raw output under `.tools/s2-evidence/`; compiler logs in `s2-candidate9-logs` beside the earlier log folders. Comparison base: published main `8d1d68aa262bb3f89ee9d073bbbaf3a83b9f9322`.

- `tools/anvil/CheckStagedTests.cs`. VG4/EC2: the commit counts as staging source when a staged path has a scanned extension or one of `.csproj`, `.props`, `.targets`, `.slnx`, `.sln`, `.rsp`, `.editorconfig`, `.json`, `.tscn`. BH2/EC1: the trigger query's filter is `ACMRTD`; the scan set stays `ACMR`. BH1/EC3: a failed `rev-parse --show-toplevel` refuses with the query and git's error, exit 1; run outside any repository it printed "git rev-parse --show-toplevel: fatal: not a git repository (or any of the parent directories): .git" (`rule8-outside-repository.txt`). EC4: the refused names are printed between the lines `Not staged:` and `End of the list.`. VG3: one more in-tool case (a staged change beside a staged source deletion passes and only the change is in the scan set). Fourteen in-tool cases.
- `tools/anvil/VerifyPolicy.cs`, no-argument harness only. Eleven more real-git cases, twenty-four in all: a real git failure (VG1: the index is overwritten after staging; exit non-zero, "a git query failed", `git diff-files -z --name-only --diff-filter=A` named); a committed source renamed to a non-source name beside an untracked C# file (VG2; refused only because of `--no-renames`); a staged source deletion alone passes (VG3); a type change, made by replacing a committed file with a symbolic link (BH2); staged `.props`, `.slnx`, `.json` and `.ts` files (VG4); three ignored shapes (BH5). The documents-only pass now stages `.md`, `.yaml` and `.toml`. VG6: a pass also requires the self-check line. EC4: the harness reads only the lines between the tool's two delimiters. EFE4: the space after the comma is restored at the ten call sites. The per-case reset removes a damaged index.
- BH5 result: none of the three shapes escapes. Git lists `fresh-dir/IgnoredByName.cs` beside an ignored `fresh-dir/build.log`, `fresh-dir/deeper/IgnoredByName.cs`, and `ScratchNotes.cs` matched by `Scratch*.cs`, each as a file; each is refused. No repair was needed.
- Documents. One sentence for the harness requirement, word for word in `docs/delivery-workflow.md`, `_bmad/custom/bmad-loop-protocol.md` handoff step 1 and `tools/anvil/README.md`, in force from this slice's publication (BH4). README and `docs/delivery-workflow.md` name the trigger set and the residual; README restates "Verified on" (46 checks, twenty-four real-git cases). `docs/planning/invest/delivery-batches.md`: S1 heading and paragraph say published (`06ac9aa2`, merged as `8d1d68aa`) (EFE1); no document links to the old heading's anchor. Contract (EFE2): Code Map baseline line, the M1 command as run in E17, `git diff --check` against main, the refinement line's set, counts. `deferred-work.md`: the residual entry.
- Five compiler-log builds, `--no-incremental`: first into `s2-candidate8-logs` (0 warnings). The gate run that followed was refused: "playtest build failed (exit 1)", nine `MSB4166: Child node … exited prematurely` errors, no compiler error and no failure file left behind (`maintainability-gate-8.log`). Cause unknown; other sessions were building on this machine. Repeated once: exit 0 (`maintainability-gate-8b.log`). Because a refused run can leave the root game project part-built, the five builds were made again into `s2-candidate9-logs` (0 warnings, `candidate9-build.log`); every later check used those.
- Maintainability gate on the final sources: exit 0, both canary `PASS` lines, `Coupling dependencies: contract=4633, model=4, intrusive=0; violations=0`, `violations=0 (metrics=0, coupling=0). PASS`. `maintainability-gate-8b.log`.
- Registration check with main's filter: 730 passed, 0 failed, `Skipped: 0` in all five projects. `registration-check-9.log`.
- `dotnet run --file tools/anvil/VerifyPolicy.cs` (no arguments): exit 0, 46 `PASS` lines, `Policy fixtures: Passed: 46, Failed: 0, Skipped: 0`. `verify-policy-harness-9.log`. `--bmad-check --self-test`: 37 passed. `bmad-self-test-9.log`. `--bmad-check --route Auto`: structural findings 0. `bmad-check-9.log`.
- Real tree, same two probes. Nothing staged: 14 self-check cases held, exit 1 listing only the intent-to-add probe (`rule9-nothing-staged.txt`). One source staged and unstaged after: exit 1 listing both (`rule9-source-staged.txt`). Probes deleted, same source staged: exit 0 (`rule9-control.txt`, `rule9-control-tool-output.txt`); unstaged again.
- `git diff --check 8d1d68aa…`: clean. `.githooks/pre-commit` and `CuriousContraptions.tests/CSharpStaticGateTests.cs` against main: empty diff (`baseline-equal-9.txt`, 0 bytes). Eleven changed paths, all owned (`diff-stat-9.txt`).
- Second full local round under `caffeinate -i`, on the final candidate and the `s2-candidate9-logs` logs: exit 0. WorkerAotGuard.Tests 17, Coverage.Tests 80, BundleElements.Tests 285, E2eRunner.Tests 256, CuriousContraptions.tests 6608; total 7246, 0 failed, `Skipped: 0` in each. `full-round-9.log`.
- Not run: the whole pre-commit hook on this candidate; `--list-tests` (no test source changed since E17).
- After the round, only this log, the contract and `TODO.md` were edited.

## Close note (written before the candidate commit)

- Finding classes that recurred across this slice's passes: a replacement control proven by its own text or by a fake rather than by execution against the real thing (pass 2 pin, pass 3 fixtures, pass 4 failed-query and exact-name checks, pass 5 real failure, rename and trigger rows); a rule wider or narrower than the hazard it serves (every commit refused in pass 3, deletions and build inputs missed in pass 4, further inputs in pass 5); documents and the contract left describing a superseded design after a redesign (passes 1, 3, 4 and 5).
- Capability found late: deleting the explicit `Compile` list also deleted an unstated commit-path protection. With the list, an unstaged new test file made the hook's index-snapshot build fail; with the glob it does not. Planning did not see it; review pass 1 did. Its replacement was designed three times: a shell guard in the hook (pass 1, wrong as designed), a rule in the policy tool on every commit (pass 2, refused unrelated commits), and the present rule, conditional on staged source (pass 3, refined in passes 4 and 5).
- Unresolved, stated in `deferred-work.md` ("Staged-source rule residual"): a wholly untracked new C# file in a commit that stages none of the trigger extensions is not caught by the hook; no standing check replaces the build's refusal of a deleted or lost test file.
- Cause unknown: one maintainability-gate run was refused when MSBuild worker nodes exited prematurely (E18); it passed when repeated and was not investigated further. The 204-result difference between `--list-tests` names and round results is the same on both sides of every comparison and was not investigated.
- Not exercised: the whole pre-commit hook has not run on any version of this candidate; its first run is the candidate commit.
- For the role card, one line: when a slice deletes a hand-maintained list or step, name every check that failed because of it, on the build path and on the commit path, before calling the deletion free.

## E19 Review pass 6 patches and the full round on the final candidate (21 November 2026)

Owner `a5f2fbba28f1455a5`, re-engaged. Raw output under `.tools/s2-evidence/`; compiler logs in `s2-candidate10-logs` beside the earlier log folders. Comparison base: published main `8d1d68aa262bb3f89ee9d073bbbaf3a83b9f9322`.

- `tools/anvil/CheckStagedTests.cs`. EC1: the scan query's filter is `ACMRT`, so a staged type change is skip-scanned. VG2/BH3/EFE1: one class, `SourcePaths`, holds the scanned extensions and the other source-commit inputs; `Decide` reads it directly and takes no predicate, so the fourteen in-tool cases run the production predicates. The self-check line also prints the set: "A commit stages source when it stages a path ending in one of: .cjs .cs .csproj .editorconfig .js .json .jsx .mjs .props .rsp .sln .slnx .targets .ts .tscn .tsx" (16 members). EC2: a git that cannot be started is a refusal ("git could not be started: …") through the same message as any failed query.
- `tools/anvil/VerifyPolicy.cs`, no-argument harness only. Derived rows: the harness reads that printed set from a run and makes one refusal row per member, the bare extension being the staged file's whole name (so `.editorconfig` is a real dot-file); a new member gets a row and a removed member lowers the count. EC1: a committed symbolic link replaced by a regular `Linked.cs` carrying a skip is blocked with `TEST-001`. VG1/BH1/EFE2: the tool run outside any repository must exit non-zero with "a git query failed", `git rev-parse --show-toplevel` and "not a git repository". EC2: the hook run with the child's `PATH` reduced to the dotnet directory and `/bin` must exit non-zero with the same header and query and "git could not be started"; it was practical, so it is a harness case. VG3/BH6: the real failing-query case also requires git's own text, confirmed on this git (2.56.1) as "fatal: .git/index: index file smaller than expected". BH4/BH5: the deletion case is labelled "a staged deletion of a source with no new file passes" and the `Holds` comment says only what is asserted.
- Documents. BH7: in `_bmad/custom/bmad-loop-protocol.md` the harness requirement is handoff item 2, "Policy-tool harness", with its own effective date, and item 1 is main's text again; the later items are renumbered 3 to 6. In `docs/delivery-workflow.md` it is its own paragraph after the paragraph that explains the solution command. The same three sentences are in `tools/anvil/README.md`, whose "Verified on" paragraph is restated from this run. `deferred-work.md`: the four deferred rows (EC3, EC4, BH10, EFE-o2) beside the residual, and the two cause-unknown items (EFE3). Contract: the same four rows in the frontmatter `deferred:` list; the frontmatter parses as YAML with four entries of keys summary, evidence, location, severity (`frontmatter-yaml.txt`).
- Five compiler-log builds, `--no-incremental`, into `s2-candidate10-logs`: all succeeded, 0 warnings. `candidate10-build.log`.
- Maintainability gate, after those builds: exit 0, both canary `PASS` lines, `Coupling dependencies: contract=4633, model=4, intrusive=0; violations=0`, `violations=0 (metrics=0, coupling=0). PASS`. `maintainability-gate-10.log`.
- Registration check with main's filter: 730 passed, 0 failed, `Skipped: 0` in all five projects. `registration-check-10.log`.
- `dotnet run --file tools/anvil/VerifyPolicy.cs` (no arguments): exit 0, 65 `PASS` lines, `Policy fixtures: Passed: 65, Failed: 0, Skipped: 0`: 21 skip-rule checks and 44 staged-source checks (24 written cases, 16 derived rows, 2 skip scans, 2 unusable-git refusals). `verify-policy-harness-10.log`. `--bmad-check --self-test`: 37 passed. `bmad-self-test-10.log`. `--bmad-check --route Auto`: structural findings 0. `bmad-check-10.log`.
- Real tree, same two probes. Nothing staged: 14 self-check cases held, exit 1 listing only the intent-to-add probe (`rule10-nothing-staged.txt`). One source staged and unstaged after: exit 1 listing both (`rule10-source-staged.txt`). Probes deleted, same source staged: exit 0 (`rule10-control.txt`, `rule10-control-tool-output.txt`); unstaged again.
- `git diff --check 8d1d68aa…`: clean. `.githooks/pre-commit` and `CuriousContraptions.tests/CSharpStaticGateTests.cs` against main: empty diff (`baseline-equal-10.txt`, 0 bytes). Eleven changed paths, all owned (`diff-stat-10.txt`).
- Full local round under `caffeinate -i`, on the final candidate and the `s2-candidate10-logs` logs: exit 0. WorkerAotGuard.Tests 17, Coverage.Tests 80, BundleElements.Tests 285, E2eRunner.Tests 256, CuriousContraptions.tests 6608; total 7246, 0 failed, `Skipped: 0` in each. `full-round-10.log`.
- Not run: the whole pre-commit hook on this candidate; `--list-tests` (no test source changed since E17).
- The session was interrupted by a usage limit after the round was started; on resuming, the round's log showed a completed result and the worktree was as left (HEAD `8d1d68aa…`, eleven owned paths, nothing staged with content). After the round, only this log, the contract and `TODO.md` were edited.

## Close note, additions after review pass 6

- Recurrence from an earlier slice (EFE4). The 7–21 November review that produced the role cards names weak or missing tests as a recurring class, and that class recurred here: a control proven by its own text, by a fake, or by a row that did not exist. The other class this slice met, a protection removed without a named replacement, is not known from an earlier slice; it is new, and the role-card line proposed above is for it. No other recurrence from an earlier slice is known.
- Deferred under Re-review scope at pass 6, each in the contract's `deferred:` list and in `deferred-work.md`: EC3 (amend), EC4 (merge through the hook), BH10 (hand-copied counts), EFE-o2 (pre-existing pass cases assert exit 0 only).
- The two cause-unknown items of the close note are now also in `deferred-work.md` with source, evidence and owner.

## R1 Review pass 1 finding rows (moved verbatim from the contract's Review Triage Log at pass 7)

  - `[high]` `[patch]` BH1 unstaged new test file passes the hook and is missing from the commit; `git add -N` is not a remedy — confirmed at `.githooks/pre-commit:23`; hook guard added, doc corrected.
  - `[false]` `[reject]` BH2 no check for the story's acceptance in the diff — the comparison is executed evidence in run log E6 and E10, which the informed auditor checked against raw files; the reviewer asked for no new guard.
  - `[medium]` `[patch]` BH3 ignored or stray top-level `.cs` is compiled locally — same root cause as BH1; the guard reads the directory, so ignore rules cannot hide a file.
  - `[low]` `[patch]` BH4 mixed tenses in the delivery-batches "Before" bullet — reworded.
  - `[low]` `[patch]` BH5 "failed on these" antecedent — reworded.
  - `[medium]` `[patch]` EC1 untracked top-level file counted by the local round — BH1 root cause.
  - `[medium]` `[patch]` EC2 intent-to-add file at commit — BH1 root cause; owner's run shows the snapshot holds it as an empty file.
  - `[low]` `[patch]` EC3 a deleted or lost test file no longer fails the build — doc now says it shows as a deleted path under the AGENTS.md deletion rule; the handoff comparison of listed names and total stays.
  - `[low]` `[patch]` EC4 hidden or sidecar `*.cs` — covered by the guard (not in the index, so refused); no `Exclude` added, which would be a second membership condition.
  - `[medium]` `[patch]` EC5 deletion of the list removed commit-completeness — BH1 root cause.
  - `[medium]` `[patch]` EC6 the `git add -N` claim — BH1 root cause; sentence corrected.
  - `[low]` `[patch]` EC7 delivery-workflow dropped "test file" as a registration trigger while protocol and card keep it — paragraph now says the check still applies.
  - `[medium]` `[patch]` VG1 broken-verification gap on the commit path — BH1 root cause; filed disposition patch.
  - `[low]` `[patch]` VG2 `git add -N` may not do what the sentence says — BH1 root cause.
  - `[false]` `[reject]` IA1 acceptance evidence is outside the blind diff — true by design of the lens; evidence is in E6 and E10.
  - `[false]` `[reject]` IA2 nothing asserts the rule stays a glob — no test pinned the list, so the story's clause had nothing to rewrite; a subdirectory or uncompiled file is refused by the existing guard and a duplicate entry by the compiler.
  - `[false]` `[reject]` IA3 membership is whatever is on disk — descriptive; the harm it implies is BH1, repaired there.
  - `[false]` `[reject]` IA4 new procedural rules beyond the story — the sentence is replaced by the hook guard's rule, needed to keep the story's outcome true at commit.
  - `[low]` `[patch]` IA5 partial doc alignment across the two policy documents — EC7 root cause.
  - `[false]` `[reject]` IA6 record wording says "Delivered" inside the candidate — the status line states what this candidate does and makes no approval or publication claim; tense fixed under BH4.
  - `[low]` `[patch]` EFE1 `TODO.md` in the owned scope but unchanged — replaced with the current brief.
  - `[medium]` `[patch]` EFE2 `git add -N` wording overstates what it secures — BH1 root cause.
  - `[low]` `[patch]` EFE3 contract lines stale (baseline-total comparison, policy delta) — amended in Tasks, Code Map and Verification.

## R2 Review pass 2 finding rows (moved verbatim from the contract's Review Triage Log at pass 7)

  - `[medium]` `[bad_spec]` VG1 the guard's refusal is proven only by a text pin and one manual run — amendment: rule moves to the policy tool with executed fixtures.
  - `[low]` `[bad_spec]` VG2 the handoff step and the new sentence leave the commit refused until `git add` — amendment: Tasks names who stages and when.
  - `[medium]` `[bad_spec]` VG3 four other test projects compile by default glob and have the same hazard — pre-existing; amendment: the rule covers every non-ignored C# file.
  - `[low]` `[bad_spec]` BH1 a tracked test file with unstaged edits passes while the wording says "with content" — amendment: the rule and its wording cover new files only and say so; unstaged edits were never refused before this slice.
  - `[medium]` `[bad_spec]` BH2 text pin includes comments and proves presence only — amendment: pin removed, fixtures execute the rule.
  - `[low]` `[bad_spec]` BH3 the guard refuses unrelated commits and offers only add or delete — intended, because the local round compiles the file; amendment: message adds the third remedy.
  - `[low]` `[bad_spec]` BH4 path quoting mismatch falsely refuses a staged non-ASCII name — amendment: NUL-separated git output, with a fixture.
  - `[low]` `[bad_spec]` BH5 case-sensitive `find` against a case-insensitive build glob — amendment: case-insensitive pathspec, with a fixture.
  - `[low]` `[bad_spec]` BH6 neither document says who replaces intent-to-add with `git add` — amendment: Tasks wording for `docs/delivery-workflow.md`.
  - `[low]` `[bad_spec]` EC1 upper-case extension — BH5 root cause.
  - `[low]` `[bad_spec]` EC2 non-ASCII staged name refused — BH4 root cause.
  - `[low]` `[bad_spec]` EC3 tracked file with unstaged edits — BH1 root cause.
  - `[medium]` `[bad_spec]` EC4 other glob-compiled directories — VG3 root cause.
  - `[low]` `[bad_spec]` EC5 contract Tasks still prescribed `git add -N` — amended.
  - `[medium]` `[bad_spec]` EC6 the pin contradicts "no test source changed" — amendment: pin removed.
  - `[medium]` `[bad_spec]` EFE1 the recorded reading covered the Never clause only; the pin also broke the Always total and two acceptance rows — amendment: pin removed, reading superseded.
  - `[low]` `[bad_spec]` EFE2 contract line 74 stale — amended.
  - `[low]` `[patch]` EFE3 triage log cited run log E12 for pass-1 detail — citation corrected.
  - `[false]` `[reject]` EFE4 ignored and sidecar rows rest on reasoning — true of the pass-1 guard, which is removed; the new rule's definition is non-ignored files, the same set the static gate uses, and each case has a fixture.
  - `[false]` `[reject]` EFE5 case and quoting suspicions — not separate defects: confirmed and routed under BH4 and BH5.
  - `[false]` `[reject]` EFE6 pin is lexical and over-wide — not a separate defect: BH2 root cause, pin removed.

## R3 Review pass 3 finding rows (moved verbatim from the contract's Review Triage Log at pass 7)

  - `[false]` `[reject]` IA1 build-surface acceptance has no new executable check in the diff — executed evidence is run log E5, E6 and E13, checked by the informed auditor against raw files.
  - `[false]` `[reject]` IA2 `tools/anvil` files are outside the verbatim ownership sentence — expansion is the repair closure of confirmed pass-1 and pass-2 findings, recorded in the Spec Change Log; the Final diff scope check judges necessity.
  - `[medium]` `[bad_spec]` IA3 rule scope is wider than the slice subject and runs on every commit — BH1 root cause.
  - `[false]` `[reject]` IA4 carried: no committed test asserts the glob rule — same claim as pass-1 IA2, code unchanged there.
  - `[low]` `[bad_spec]` IA5 a protection lost (deleted test file) that the story does not mention — BH6 root cause.
  - `[false]` `[reject]` IA6 handoff wording change and narrative figures — the staging sentence is the rule's documentation; the README figures restate executed runs (E13).
  - `[medium]` `[bad_spec]` VG1 intent-to-add refusal is proven only against a fake git — amendment (d): real-git harness cases.
  - `[low]` `[patch]` VG2 the harness is built, not run, by the hook and CI — pre-existing; README now names the no-argument run in Verification and the amendment keeps the rule's own decision checked on every invocation.
  - `[medium]` `[bad_spec]` BH1 the rule refuses every commit in a checkout with unrelated untracked C# files — amendment (a) and (b).
  - `[medium]` `[bad_spec]` BH2 most fixtures exercise the fake — amendment (c) and (d).
  - `[low]` `[bad_spec]` BH3 one real-git case only — VG1 root cause.
  - `[low]` `[bad_spec]` BH4 self-test in the hook path prints a test-style receipt — amendment (f); running the decision's own cases on every invocation is kept, as the gate's canaries do.
  - `[low]` `[bad_spec]` BH5 documents do not say the rule fires after the snapshot builds — amendment, Documents.
  - `[low]` `[bad_spec]` BH6 no standing check replaces the build's refusal of a deleted test file — amendment, Documents: stated as intended, covered by the diff and the deletion rule.
  - `[false]` `[reject]` BH7 carried: nothing asserts the membership rule — pass-1 IA2.
  - `[low]` `[patch]` BH8 record inconsistencies (`deferred-work.md` "None has started except S2" while S1 is in flight; a tool comment naming Anvil 0.12 beside "Verified on 0.14.3") — owner corrects both; the "Delivered" wording is carried from pass-1 IA6.
  - `[low]` `[patch]` EC1 staged file with a non-lowercase extension is never skip-scanned (ordinal lookup, pre-existing) — amendment (e).
  - `[low]` `[bad_spec]` EC2 an ignored yet glob-compiled C# file — amendment (g): the ignored-files query refuses an individually ignored C# file when a source is staged.
  - `[low]` `[bad_spec]` EC3 git older than 2.28 reports intent-to-add differently — VG1 root cause: the real-git case fails on such a git instead of passing silently; no version branch.
  - `[low]` `[patch]` EC4 a temporary path with a single quote breaks the harness's generated hook — quote it safely.
  - `[low]` `[patch]` EC5 a throwing delete in the harness `finally` leaks the tool directory and masks the first failure — nest the cleanup.
  - `[low]` `[bad_spec]` EFE1 real-git proof covers untracked only — VG1 root cause.
  - `[low]` `[patch]` EFE2 contract cited `.githooks/pre-commit:66` — corrected to `:48`.
  - `[low]` `[patch]` EFE3 Verification said rows were pending while the table recorded Pass — owner aligns paragraph and table at handoff.
  - `[false]` `[reject]` EFE4 README "C# static gate" order sentence — not separate: BH5 root cause, repaired there.

## R4 Review pass 4 finding rows (moved verbatim from the contract's Review Triage Log at pass 7)

  - `[false]` `[reject]` IA1 carried: acceptance lives at the build surface, new tests at the commit surface — pass-3 IA1.
  - `[false]` `[reject]` IA2 carried: ownership boundary — pass-3 IA2.
  - `[false]` `[reject]` IA3 rule is repository-wide while the story concerns one project — intended since pass 2 (VG3: other projects compile by glob); it now runs only beside staged source.
  - `[false]` `[reject]` IA4 changes with no anchor in the intent (case-insensitive lookup, harness class, README order, Anvil version) — each is the recorded repair of a confirmed row (pass-3 EC1, the gate failure in E14, BH5, BH8).
  - `[false]` `[reject]` IA5 carried: a protection traded, not only removed — pass-3 BH6, stated in the documents.
  - `[false]` `[reject]` IA6 equivalence holds for top-level files only — that is the rule (matrix M3).
  - `[false]` `[reject]` IA7 carried: story text edited to Status and Before — pass-1 IA6.
  - `[low]` `[patch]` IA8 `TODO.md` still says "pass-3 candidate" — owner restates it for the final candidate.
  - `[medium]` `[patch]` BH1 a staged deletion does not count as staged source, so a hand rename loses the test — product defect against (a); deletions and build inputs now trigger the queries.
  - `[low]` `[patch]` BH2 an ignored-by-name file has no workable remedy in the message — message names `git add -f`; the real ignore rules were checked (no tracked pattern matches a `.cs` file).
  - `[false]` `[reject]` BH3 README hook order reversed with no hook change — the informed auditor compared the README with `.githooks/pre-commit` lines 23–58: the new text is the correct one, the old was wrong.
  - `[false]` `[reject]` BH4 carried: no standing check replaces the build's refusal of a lost test file — pass-3 BH6; the handoff comparison of listed names and total is the control.
  - `[low]` `[patch]` BH5 harness comment says "names exactly" while the code checks presence — compare the listed names with the expected set.
  - `[low]` `[patch]` BH6 only the untracked query is ever failed — one case per failing query.
  - `[false]` `[reject]` BH7 carried: git older than 2.28 — pass-3 EC3; the harness is now a required command (VG1).
  - `[low]` `[patch]` BH8 a git failure is reported without its cause — message names the failing query and git's error text.
  - `[false]` `[reject]` BH9 carried: "Delivered" wording inside the candidate — pass-1 IA6.
  - `[medium]` `[patch]` VG1 the real-git harness is run by no normal verification path — it becomes a named required command whenever `tools/anvil/*.cs` changes, in `docs/delivery-workflow.md` and the protocol's handoff order.
  - `[false]` `[reject]` VG2 carried: loss of a test file fails nothing — pass-3 BH6; filed disposition defer.
  - `[low]` `[patch]` EC1 staged deletion — BH1 root cause.
  - `[medium]` `[patch]` EC2 a commit staging only build inputs beside an untracked C# file — BH1 root cause; build inputs trigger the queries.
  - `[maybe-false]` `[patch]` EC3 an ignored-by-name file that is the sole content of an untracked directory may collapse to `dir/` and escape — settled by a real-git case; repaired if it escapes.
  - `[false]` `[reject]` EC4 carried: hidden or sidecar `*.cs` — pass-1 EC4.
  - `[low]` `[patch]` EC5 a repository created without `.git/info` breaks the harness — create the directory first.
  - `[low]` `[patch]` EC6–EC10 five claim rows: the contract's Tasks text still described the pass-2 design (documents-only commit checked, three remedies, two queries, nine fixtures, "every new C# file") — Tasks and Code Map lines now mark that text superseded and point to entry 3.
  - `[low]` `[patch]` EFE1 entry 3 had no terminal planning verdict — recorded in run log E15.
  - `[low]` `[patch]` EFE2 Code Map and Tasks contradicted the candidate — EC6 root cause.
  - `[low]` `[patch]` EFE3 failed-query cases and the "names exactly" comment — BH5 and BH6 root causes.
  - `[low]` `[patch]` EFE4 staged deletion and the hazard wording — BH1 root cause; the documents say deletions and build inputs count as staged source.

## R5 Review pass 5 finding rows (moved verbatim from the contract's Review Triage Log at pass 7)

  - `[medium]` `[patch]` VG1 the tool's real git-failure path is executed by no check — one real-git harness case with a failing query.
  - `[low]` `[patch]` VG2 `--no-renames` has no behavioural check — a real-git rename case.
  - `[low]` `[patch]` VG3 "deleted paths are never scanned" is observed by no case — an in-tool case and a real-git case where a staged source deletion alone passes.
  - `[low]` `[patch]` VG4 three of four build inputs and every non-C# scanned kind are untested as triggers — harness rows for each trigger extension.
  - `[false]` `[reject]` VG5 carried: the harness runs only by hand — pass-4 VG1, repaired there as a required command.
  - `[low]` `[patch]` VG6 a passing harness case asserts only exit 0 — it also asserts the self-check line, so the rule is shown to have run.
  - `[low]` `[patch]` BH1 a failed `rev-parse` discards git's error — the refusal carries the query and error text like the others.
  - `[low]` `[patch]` BH2 "any status" omits type changes — the trigger filter adds `T`, with a case.
  - `[low]` `[patch]` BH3 production trigger predicate, rename and real failure untested — VG1, VG2 and VG4 root causes.
  - `[low]` `[patch]` BH4 the harness requirement is worded three ways and its effective date is unclear — one wording ("whenever a file under `tools/anvil/` changes"), its own sentence, in force from this slice's publication.
  - `[maybe-false]` `[patch]` BH5 other ignored-file shapes may collapse to a directory entry — settled by real-git cases (an ignored C# file beside another ignored file in an untracked directory; nested two levels; matched by a glob pattern); repaired if one escapes.
  - `[low]` `[patch]` EC1 type change — BH2 root cause.
  - `[low]` `[patch]` EC2 a commit staging only other build or content inputs (`.json`, `.tscn`, `.editorconfig`, `.rsp`, `.sln`) beside an untracked C# file passes — those extensions join the trigger set; the documents name the set.
  - `[low]` `[patch]` EC3 `rev-parse` error — BH1 root cause.
  - `[low]` `[patch]` EC4 the harness reads any indented output line after the header as a refused name — it reads only the lines the tool prints as names, delimited by the tool.
  - `[low]` `[patch]` EFE1 `delivery-batches.md` still heads S1 "in flight" in a section this slice edits — heading and paragraph state that S1 is published.
  - `[low]` `[patch]` EFE2 the contract's Verification and Code Map still use the original baseline in three places — aligned to main as the comparison base, with `baseline_revision` named as the original.
  - `[low]` `[patch]` EFE3 "any status" wording — BH2 root cause.
  - `[low]` `[patch]` EFE4 ten call sites lost a space after a comma in the harness rename — restored.

## E20 Review pass 7 patches and the full round on the final candidate (21 November 2026)

Owner `a5f2fbba28f1455a5`, re-engaged. Raw output under `.tools/s2-evidence/`; compiler logs in `s2-candidate11-logs` beside the earlier log folders. Comparison base: published main `8d1d68aa262bb3f89ee9d073bbbaf3a83b9f9322`.

- No-git case (BH1, EC1, VG1, EFE1). The child's `PATH` is a harness-made temporary directory holding only a symbolic link to the resolved `dotnet` executable (the first `dotnet` on the harness's `PATH`, followed through links to its final target), plus `/bin` when `/bin` has no git; an inherited `DOTNET_ROOT` still reaches the child. If git resolves on that `PATH` the case stops with "the no-git case cannot run: git still resolves at …". Verified in both orders: pinned toolchain first (`dotnet` at `/private/tmp/tim-dotnet-10.0.403/dotnet`, git elsewhere) and `/opt/homebrew/bin` first with `DOTNET_ROOT` unset (`dotnet` and `git` both links in that directory): 67 of 67 each. `/opt/homebrew/bin/dotnet` ran the harness without any other problem; it selects SDK 10.0.403 through `global.json`. `verify-policy-harness-11-pinned.log`, `verify-policy-harness-11-homebrew.log`.
- Child environment (EC4, BH2b, VG3). `Harness.Execute` removes `GIT_DIR`, `GIT_WORK_TREE` and `GIT_INDEX_FILE` and sets `LC_ALL=C` for every child. Outside-repository case (BH2a, EC2): a fresh temporary directory with `GIT_CEILING_DIRECTORIES` set to its parent.
- Trigger coverage (BH3, VG2, EFE2). The harness holds the sixteen documented extensions as its own literal and requires the set the tool prints to equal it (one counted check) before making one refusal row per member. In the tool, the in-tool cases state literal expected scan paths; no expectation is computed with the production predicate.
- README opening paragraph (BH4, EFE3): added, copied, modified, renamed and type-changed source files; a staged path whose content git cannot show is refused and named.
- Gitlink (EC3). The skip scan no longer throws when `git show :<path>` fails: it prints "the staged content of <path> could not be read (git show :<path> failed), so the commit is refused." and exits 1. A harness case stages a gitlink at `Module.cs` (`git update-index --add --cacheinfo 160000,…`) and requires that text and no "Unhandled exception".
- Records. `deferred-work.md`: one entry for the pass-7 deferred candidates; the contract's `deferred:` list has the matching fifth item and parses as YAML (`frontmatter-yaml-11.txt`). The finding rows of triage passes 1 to 5 moved from the contract to run-log entries R1 to R5: 117 rows, compared line for line with a copy taken before the move (`triage-rows-before-move.txt`, `triage-rows-move.diff` empty). Each pass keeps its heading, verdict line, layer line and one line citing its entry; pass 1's layer line lost the sentence saying its rows had no run-log entry. The contract is under 8,000 words after the move (`wc -w`; the count is in the owner's report). `TODO.md` restated.
- Five compiler-log builds, `--no-incremental`, into `s2-candidate11-logs`: all succeeded, 0 warnings. `candidate11-build.log`.
- Maintainability gate, after those builds: exit 0, both canary `PASS` lines, `Coupling dependencies: contract=4633, model=4, intrusive=0; violations=0`, `violations=0 (metrics=0, coupling=0). PASS`. `maintainability-gate-11.log`.
- Registration check with main's filter: 730 passed, 0 failed, `Skipped: 0` in all five projects. `registration-check-11.log`.
- No-argument harness, both orders: exit 0, 67 `PASS` lines, `Policy fixtures: Passed: 67, Failed: 0, Skipped: 0`: 21 skip-rule checks and 46 staged-source checks (24 written cases, 1 set comparison, 16 member rows, 3 skip scans, 2 unusable-git refusals). `--bmad-check --self-test`: 37 passed (`bmad-self-test-11.log`). `--bmad-check --route Auto`: structural findings 0 (`bmad-check-11.log`).
- Real tree, same two probes. Nothing staged: 14 self-check cases held, exit 1 listing only the intent-to-add probe (`rule11-nothing-staged.txt`). One source staged and unstaged after: exit 1 listing both (`rule11-source-staged.txt`). Probes deleted, same source staged: exit 0 (`rule11-control.txt`, `rule11-control-tool-output.txt`); unstaged again.
- `git diff --check 8d1d68aa…`: clean. `.githooks/pre-commit` and `CuriousContraptions.tests/CSharpStaticGateTests.cs` against main: empty diff (`baseline-equal-11.txt`, 0 bytes). Eleven changed paths, all owned (`diff-stat-11.txt`).
- Full local round under `caffeinate -i`, on the final candidate and the `s2-candidate11-logs` logs: exit 0. WorkerAotGuard.Tests 17, Coverage.Tests 80, BundleElements.Tests 285, E2eRunner.Tests 256, CuriousContraptions.tests 6608; total 7246, 0 failed, `Skipped: 0` in each. `full-round-11.log`.
- Not run: the whole pre-commit hook on this candidate; `--list-tests` (no test source changed since E17).
- Validation note: the five contract edits that removed the moved rows, and the run-log append of those rows, were validated by a preview of their headings, not line by line; the rows are text already in the working tree.
- After the round, only this log, the contract and `TODO.md` were edited.

## Close note, addition after review pass 7

- Deferred at pass 7, one entry in `deferred-work.md` and one item in the contract's `deferred:` list: the five candidates named in the pass-7 triage log. Nothing else is unresolved; the residual, the four pass-6 rows and the two cause-unknown items stand as recorded above.

## E21 Second merge of published main and the handoff order on the twice-merged candidate (21 November 2026)

Owner `a5f2fbba28f1455a5`, re-engaged, on the coordinator's route. Raw output under `.tools/s2-evidence/` (merge files in its `merge2/` folder); compiler logs in `s2-main2-logs` and `s2-candidate12-logs` beside the earlier log folders. The slice has now merged main twice, because two other slices (registration derivation, then EL-239 Baseball) published during its review.

- Identities: original baseline `7c019d64ee54a80bc0d31b82c90533985c2748ba` (the contract's `baseline_revision`, unchanged); comparison base from E17 to E20 `8d1d68aa262bb3f89ee9d073bbbaf3a83b9f9322`; new comparison base and HEAD `fa86969f1866368ae44da7d20f885453045a599a`.
- Overlap: main changes 32 paths; four are owned here: `TODO.md`, `_bmad-output/implementation-artifacts/deferred-work.md`, `docs/planning/invest/delivery-batches.md` and `CuriousContraptions.tests/CuriousContraptions.tests.csproj` (`merge2/overlap.txt`). Main also changes `CuriousContraptions.tests/CSharpStaticGateTests.cs` (one line for the Baseball project), which this slice does not own or change.
- Safeguards: per-file patches and plain copies of the four paths and the whole candidate diff (1893 lines) in `merge2/`. `git checkout HEAD --` on the four paths only, then `git merge --ff-only origin/main`: exit 0, HEAD `fa86969f…` (`merge2/merge-ff.txt`). The seven non-overlapping owned paths have the same SHA-256 before and after (`merge2/other-paths-before.sha`).
- Re-applied by hand onto main's text. `deferred-work.md`: main's two new Baseball entries at the top are untouched; this slice's sentence, S2 pointer and four sections are back, and from the "Structural stories queued" heading to the end the file equals the pre-merge candidate. `delivery-batches.md`: main's two Baseball edits kept; this slice's four changed lines are the same as before the merge. `TODO.md`: this slice's brief, carrying forward main's two still-open items for their owners. The test project file: main's explicit list with its new `BaseballTests.cs` line replaced by the one rule; the result equals the pre-merge candidate file byte for byte.
- M1 on the new basis. Main's explicit-list project file, as it stood after the fast-forward, was built on the merged sources into `s2-main2-logs` (0 warnings, `main2-build.log`) and its tests listed (7090 names, `main2-list-tests.txt`) before the rule was written back. Against the candidate's logs: 267 `Csc` `.cs` arguments and 239 repository sources (237 local + 2 linked) on both sides, no difference in either direction in either log; the candidate's 237 equal the directory listing. `{main2,merged2}-{solution,playtest}-sources-{full,repo}.txt`, `m1c-*.diff` (empty).
- Five compiler-log builds of the twice-merged candidate, `--no-incremental`, into `s2-candidate12-logs`: all succeeded, 0 warnings. `candidate12-build.log`.
- Listed names on the candidate: 7090, identical to main's explicit-list build. `merged2-list-tests.txt`, `list-tests-main2-vs-merged2.diff` (empty).
- Maintainability gate, after those builds: exit 0, both canary `PASS` lines, `Coupling dependencies: contract=4671, model=4, intrusive=0; violations=0` (main's own figure), `violations=0 (metrics=0, coupling=0). PASS`. `maintainability-gate-12.log`.
- Registration check. With the filter in `docs/delivery-workflow.md` (18 classes): 767 passed, 0 failed, `Skipped: 0` in all five projects (17, 67, 192, 97, 394). `registration-check-12.log`. Main's recorded 798 was made with that filter plus `CurrentCapabilityInputsTests` and `WorkshopIntegrationStartupTests` (Baseball run log, entries 6 and 9), which main did not add to the document; run that way here: 798 passed, 0 failed, `Skipped: 0` (17, 192, 83, 97, 409). `registration-check-12-expanded.log`. The document's filter is not changed here: it is main's text and outside this slice's edits.
- No-argument harness, both `PATH` orders: 67 of 67 each. `verify-policy-harness-12-pinned.log`, `verify-policy-harness-12-homebrew.log`. `--bmad-check --self-test`: 37 passed (`bmad-self-test-12.log`). `--bmad-check --route Auto`: structural findings 0 (`bmad-check-12.log`). Frontmatter parses as YAML with five `deferred` entries (`frontmatter-yaml-12.txt`).
- Real tree, same two probes. Nothing staged: exit 1 listing only the intent-to-add probe (`rule12-nothing-staged.txt`). One source staged and unstaged after: exit 1 listing both (`rule12-source-staged.txt`). Probes deleted, same source staged: exit 0 (`rule12-control.txt`); unstaged again.
- `git diff --check fa86969f…`: clean. `.githooks/pre-commit` and `CuriousContraptions.tests/CSharpStaticGateTests.cs` against main: empty diff (`baseline-equal-12.txt`, 0 bytes). Changed paths before the round: ten, all owned (`diff-stat-12.txt`); `TODO.md` became the eleventh after the round.
- Full local round under `caffeinate -i` on the E21 logs: exit 0. WorkerAotGuard.Tests 17, BundleElements.Tests 287, Coverage.Tests 115, E2eRunner.Tests 256, CuriousContraptions.tests 6619; total 7294, 0 failed, `Skipped: 0` in each; 7294 is main's recorded full round, with the same per-project figures. `full-round-12.log`.
- Not run: the whole pre-commit hook on this candidate.
- After the round, only this log, the contract and `TODO.md` were edited.

## E22 Review pass 8 patches; verification blocked by a damaged toolchain (22 November 2026)

Owner `a5f2fbba28f1455a5`, re-engaged. `git fetch origin` first: `origin/main` is still `fa86969f1866368ae44da7d20f885453045a599a`.

- Patches made. `tools/anvil/VerifyPolicy.cs`: `StagedSourceHarness.UnusableGit` removes both temporary directories through `Harness.Remove` (both attempted; a failed cleanup fails the run and does not replace the case's own failure); `PathWithoutGit` makes the resolved `dotnet` path absolute before linking and no longer has the unreachable "git still resolves" guard. `tools/anvil/README.md`: the no-git case fails for want of the refusal text if git starts. `deferred-work.md` and the contract's `deferred:` list: the pass-8 deferred candidates (frontmatter parses as YAML with six entries, `frontmatter-yaml-13.txt`). Nothing else in the tool files changed.
- Blocker. The pinned toolchain under `/private/tmp` was damaged between E21 and this entry: `/private/tmp/tim-dotnet-10.0.403` now holds 427 files and 1,331 empty directories, with directory times of 22 November 00:00, and `/private/tmp/node-v26.12.1-darwin-arm64` holds 2 files. Every `dotnet build` and `dotnet run` with that SDK now stops at start-up with "Could not load file or assembly 'Microsoft.AspNetCore.DeveloperCertificates.XPlat, Version=10.0.14.0'" (`candidate13-build.log`, `maintainability-gate-13.log`). The cause is consistent with the operating system's periodic clearing of old files under `/private/tmp`; it was not investigated further. The Homebrew `dotnet` has SDK 10.0.403 but lists no `wasm-tools` workload, so it is not a like-for-like replacement for the five capture builds.
- Not run on the pass-8 candidate, for that reason: the five compiler-log builds, the maintainability gate, the registration check, the tool's cases, the harness, `--bmad-check`, the real-tree runs and the full local round. The last complete evidence is E21, on the candidate before these three harness edits. The compiler logs of E21 (`s2-candidate12-logs`) also live under `/private/tmp` and may be cleared the same way.
- Owner and retry condition: the coordinator or host owner re-provisions .NET SDK 10.0.403 with `wasm-tools` and Node 26.12.1 (outside `/private/tmp`, or refreshed there) and names the paths; the owner then runs the full handoff order on this candidate. The owner did not download or install anything.

### E22 continued: toolchain restored, handoff order on the final candidate (22 November 2026)

- Toolchain. The old `/private/tmp` installs were cleared by the system's temporary-file cleanup. The coordinator re-provisioned on 22 November 2026 with the official install script, `dotnet workload install wasm-tools --version 10.0.403` and the nodejs.org archive: `DOTNET_ROOT=$HOME/.local/share/tim-toolchain/dotnet-10.0.403`, Node at `$HOME/.local/share/tim-toolchain/node-v26.12.1-darwin-arm64/bin`. Session environment check: `node --version` v26.12.1, `dotnet --version` 10.0.403, `wasm-tools` listed, `dotnet restore CuriousContraptions.slnx` exit 0 (`environment-check-13.txt`). Compiler logs now go to `$HOME/.local/share/tim-toolchain/logs/s2-13`, outside the checkout and outside the temporary folder.
- `git fetch origin` at the start: `origin/main` still `fa86969f1866368ae44da7d20f885453045a599a`.
- One more harness edit, found by the first capture build: the analyzer refused the cleanup flag assigned in the `finally` of `UnusableGit` (`IDE0059`, twice, with `var cleaned = false` and with `bool cleaned`). The two `Harness.Remove` results are now collected in a list and the run fails if either is false; behaviour as specified, no suppression. Third build clean.
- Five compiler-log builds, `--no-incremental`: all succeeded, 0 warnings. `candidate13-build.log`.
- Maintainability gate, after those builds: exit 0, both canary `PASS` lines, `Coupling dependencies: contract=4671, model=4, intrusive=0; violations=0`, `violations=0 (metrics=0, coupling=0). PASS`. `maintainability-gate-13.log`.
- Registration check: documented filter 767 passed, 0 failed, `Skipped: 0` (17, 67, 192, 97, 394), `registration-check-13.log`; with main's two appended classes 798 passed, 0 failed, `Skipped: 0` (17, 192, 83, 97, 409), `registration-check-13-expanded.log`.
- No-argument harness: 67 of 67 with the pinned toolchain first on `PATH` and 67 of 67 with `/opt/homebrew/bin` first and `DOTNET_ROOT` unset. `verify-policy-harness-13-pinned.log`, `verify-policy-harness-13-homebrew.log`. `--bmad-check --self-test`: 37 passed (`bmad-self-test-13.log`). `--bmad-check --route Auto`: structural findings 0 (`bmad-check-13.log`).
- Real tree, same two probes. Nothing staged: exit 1 listing only the intent-to-add probe (`rule13-nothing-staged.txt`). One source staged and unstaged after: exit 1 listing both (`rule13-source-staged.txt`). Probes deleted, same source staged: 14 self-check cases held, exit 0 (`rule13-control.txt`, `rule13-control-tool-output.txt`); unstaged again.
- `git diff --check fa86969f…`: clean. `.githooks/pre-commit` and `CuriousContraptions.tests/CSharpStaticGateTests.cs` against main: empty diff (`baseline-equal-13.txt`, 0 bytes). Eleven changed paths, all owned (`diff-stat-13.txt`). Frontmatter parses as YAML with six `deferred` entries (`frontmatter-yaml-13.txt`).
- Full local round under `caffeinate -i` on those logs: exit 0. WorkerAotGuard.Tests 17, BundleElements.Tests 287, Coverage.Tests 115, E2eRunner.Tests 256, CuriousContraptions.tests 6619; total 7294, 0 failed, `Skipped: 0` in each. `full-round-13.log`.
- Records after the round: the contract's toolchain line, table rows, Final diff scope line and Auto Run Result; `TODO.md` restated for the commit; this entry. `git fetch origin` at the end: `origin/main` = `fa86969f1866368ae44da7d20f885453045a599a`, as at the start.
- Not run: the whole pre-commit hook on this candidate; `--list-tests` and the source-list comparison (no test source or project file changed since E21).

## Close note, addition after review pass 8

- Deferred at pass 8: one entry in `deferred-work.md` and one item in the contract's `deferred:` list (eight candidates). Late environment finding: a toolchain and compiler logs kept under `/private/tmp` were cleared by the system during the slice; both now live under `$HOME/.local/share/tim-toolchain`. Nothing else is unresolved beyond what is recorded above.

## E23 Record edits after review pass 9 (22 November 2026)

- Record edits only, after the pass-9 review: the contract's pass-9 triage entry, Final diff scope line, `status: 'done'` and Auto Run Result; the pass-9 additions to the pass-8 deferred entry; `TODO.md` (nine review passes); the fetch result in E22. No source changed after `maintainability-gate-13.log` and `full-round-13.log`.
