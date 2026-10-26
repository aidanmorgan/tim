---
title: 'Local tests are authoritative; GitHub builds and deploys'
type: 'chore'
created: '2026-10-26'
status: 'done'
baseline_revision: 133859b4dfac2066a941a9a2833f08d97834abbd
review_loop_iteration: 0
followup_review_recommended: false
context: [AGENTS.md, docs/testing-guidance.md]
warnings: []
deferred: []
---

<intent-contract>

## Intent

**Problem:** The owner says: “we dont need to run the ci tests on github, local tests are enough”. GitHub currently repeats the locally verified game and tool suites, delaying publication.

**Approach:** Make local required runs and normal hooks the test authority. Remove GitHub test execution and its test-only provisioning; retain production compilation, publishing, artifact checks and deployment.

## Boundaries & Constraints

**Always:** All required local game/tool tests execute with zero failures/skips, excluding only the already explicit browser deferral. Retain normal hooks, independent Auto review, compiler warnings-as-errors, production build and exact-SHA artifact/deployment verification. Apply this newest owner direction immediately to pending publication. Preserve every gameplay requirement and the next warmup-removal task.

**Never:** Disable or skip individual game tests, introduce conditional CI/local behavior in tests, remove production build/deployment, modify game runtime, create a new test runner/platform, run browser/mutation tests or publish locally. No fallback/compatibility.

</intent-contract>

## Code Map

- Review repair: `CSharpStaticGateTests.cs` now requires matching compiler logs and deletes the no-log/evaluated-MSBuild alternatives; retain all compiler-input and expected-compilation checks. `docs/delivery-workflow.md` owns the complete local prerequisites, five-log capture and full-suite commands; `docs/testing-guidance.md` links that contract. AGENTS distinguishes full local verification from supplemental hooks.

- `tools/E2eRunner/BrowserDeferral.cs`, `tools/E2eRunner/Program.cs`: rename the CI-specific filter API and its callers without an alias; retain the external `deferred-filter` command and exact exclusions.
- `.githooks/pre-commit`: correct the stale CI-test comment only; retain every hook operation.
- `docs/testing-guidance.md`: correct the integration-test execution location to local required runs, preserving all behavioral requirements.
- `.github/workflows/pages.yml`: remove Test C# game/tools and setup-node used only by those tests. Preserve pinned .NET/WASM setup, builds, production publish, static files, upload and deploy. Correct comments that claim remote test inspection.
- `tools/E2eRunner.Tests/BrowserDeferralTests.cs`: CiFilterIsGeneratedFromTheListAndExcludesExactlyItsMethods mixes obsolete workflow assertions with useful actual CLI filter behavior. Keep the real generator execution and independently enumerated exact deferrals; remove obsolete workflow expectations.
- `CuriousContraptions.tests/CSharpStaticGateTests.cs`: CiKeepsTheSameRestoreAndBuilds has one CSHARP_STATIC_GATE_BINLOGS inline row belonging only to removed test step. Remove that obsolete row; preserve build/trigger/main-only assertions and all local hook checks.
- `AGENTS.md`, `docs/delivery-workflow.md`: record owner's local-test authority, including terminal build/artifact/deploy checks without remote test totals. These direct owner rule updates are explicitly authorized; no extra approval.
- `TODO.md`, `_bmad-output/implementation-artifacts/sprint-status.yaml`, this spec: concise current result/next check.
- `spec-game-grade-float-comparisons.md`: existing immutable float intent and evidence remain; append short publication-policy transition only. Float commit133859b4 has full local4409/0/0, hooks124/0/0, independent SnapshotApproval. Remote run40311722182 was cancelled by owner-directed policy change before publication; do not claim a remote test/deployment Pass.

## Tasks & Acceptance

**Execution:**
- [x] Remove remote test steps/provisioning and adjust the two affected local guards, preserving their remaining behavioral checks.
- [x] Update current owner rules and brief; keep original requirements and historical receipts intact.
- [x] Verify local affected/full checks and independent Auto review; establish applicability of unchanged float runtime proof to unchanged relevant inputs.
- [ ] Run normal hooks, commit, obtain exact-SHA SnapshotApproval, publish and verify exact artifact/deployment.

**Acceptance Criteria:**
- C1: Given push/PR/main workflow inputs, when the workflow is inspected/executed, then no game/tool test runner executes on GitHub, while existing builds and main-only deployment remain.
- C2: Given the browser deferral command, when invoked locally, then its output excludes exactly the named deferred methods; all required local game tests remain runnable.
- C3: Given local verification and normal hooks, when this candidate is checked, then required checks pass with zero skipped game tests and no source/runtime changes outside this closure.
- C4: Given the newest owner policy, when reading current guidance and terminal evidence, then local test authority is unambiguous; production publish/artifact/deployment remains required and the cancelled run is not represented as a Pass.

## Spec Change Log

## Review Triage Log


### 2026-10-26 — Review pass 0

Five Auto layers launched before triage: local_test_blind_r0, local_test_edge_r0, local_test_verification_r0, local_test_intent_r0, local_test_forward_r0. Capacity required two waves. Edge returned no findings; Verification found no gaps.
- verdicts: 12 findings — high 1, medium 4, low 2, false 5, maybe-false 0
- findings:
  - `[medium]` `[patch]` B1: Removing the executable remote test inventory leaves current local guidance without a complete reproducible invocation. Document the full solution command and matching compiler evidence; preserve all five native projects and existing browser exclusions.
  - `[low]` `[patch]` B2: The edited rule still claims full test coverage in normal hooks, but the hook deliberately runs only static/UI checks. Correct the current owner-policy wording: a full local round plus supplemental scoped hooks, both required. This direct owner-rule correction is authorized by the user's current instruction and complete-current-guidance requirement.
  - `[medium]` `[patch]` B3: Removed Node provisioning carried the only local-relevant exact version pin. Document Node26.12.1 and its version check with the local prerequisites; coordinator confirmed installed v26.12.1. No installation framework is needed.
  - `[medium]` `[patch]` B4: Local generator behavior is covered, but the new authoritative invocation is not recorded as a reproducible contract. Same root cause as B1; include generated-filter consumption in the exact local command. Actual root round77326 used that filter, but was subsequently cancelled for changed review inputs.
  - `[low]` `[patch]` B5: GitHub binary logs exist only in runner temporary storage, so “retains” can imply persistence beyond the job. Correct the comment; do not introduce artifact machinery.
  - `[false]` `[reject]` B6: Auto intentionally excludes the current spec from the common diff and supplies it only to Edge. The spec exists in the candidate and was read by coordinator and Edge; it is not missing from the delivered change.
  - `[false]` `[reject]` I1: Workflow and current policy directly change test execution/acceptance surfaces as requested. This descriptive alignment is not a divergence.
  - `[false]` `[reject]` I2: The owner's general statement while waiting on GitHub tests selects local authority for current and future work; no one-time qualifier exists. Removing redundant execution implements that direction without disabling tests locally.
  - `[false]` `[reject]` I3: Actual GitHub build-only execution is a publication-dependent check, explicitly pending. Local configuration/CLI checks and YAML inspection do not claim deployed success; no remote test run is required under the new policy.
  - `[medium]` `[patch]` I4: Same local invocation-contract gap as B1/B4; record exact full local execution and generated-filter consumption.
  - `[false]` `[reject]` I5: Pending full verification and publication are correctly labelled; the change does not claim terminal completion.
  - `[high]` `[patch]` F1: Confirmed retained fallback in CSharpStaticGateTests: missing CSHARP_STATIC_GATE_BINLOGS chooses evaluated MSBuild items and empty CscRuns instead of actual compiler inputs/expected log coverage. Delete that alternative and require matching logs explicitly. Owner forward-only gate forbids deferral despite pre-existing origin.

Patch groups: F1 high; B1/B4/I4 medium (with related B2 wording correction); B3 medium; B5 low. Same implementer owns repairs. Further five-layer review recommended because the compiler-input proof path changes. No findings deferred. Required local round77326 was cancelled with exit1 after the new source-boundary decision, not reported as passing; final matching verification follows repairs.


### 2026-10-26 — Follow-up review pass 1

Five layers: local_test_blind_r1, local_test_edge_r1, local_test_verification_r1, local_test_intent_r1, local_test_forward_r1. All launched before triage; Edge returned no findings; Forward confirmed the log fallback removed and no violation.
- verdicts: 13 findings — high 0, medium 2, low 1, false 10, maybe-false 0
- findings:
  - `[low]` `[patch]` B1: TODO/sprint still show pre-repair94/0/0. Refresh current views with log-backed and final focused evidence, retaining prior results as historical.
  - `[false]` `[reject]` B2: Compiler-input and expected-compilation facts are complementary checks within the required static-gate class. An empty log directory fails EveryProjectReportsTheSourcesItCompiled through ExpectedLogCompiles; selecting one unrelated assertion cannot establish full gate Pass. No actual required-run acceptance of empty logs exists.
  - `[medium]` `[patch]` B3: Missing configuration refusal needs committed negative coverage; group absent/empty/value cases with V1. Existing filesystem enumeration visibly rejects nonexistent directories and existing expected-inventory checks reject missing logs/compiles; no new filesystem framework or Cartesian fixture suite is needed.
  - `[false]` `[reject]` B4: Current guidance explicitly limits log reuse to unchanged relevant source/generated/dependency/configuration/compiler inputs. The slice records actual matching capture commands and frozen candidate; independent review establishes applicability. It does not claim filenames prove freshness, and owner light-evidence rules do not require a new content-hash manifest.
  - `[false]` `[reject]` B5: The r0 invocation-contract gap is repaired: current guidance records the complete solution command consuming the real generated filter. Root executes that exact contract; no new test orchestrator or implementation-mirroring assertion is required for the documentation correction.
  - `[false]` `[reject]` B6: Applying the existing fully qualified E2eRunner.Tests exclusions to the solution was already the authoritative local full-run pattern before this change. It changes no filter entries/operator/runner semantics or other-project namespace; required full project totals provide execution evidence. The API rename does not introduce runner selection behavior.
  - `[false]` `[reject]` B7: No remote test/provisioning steps remain in the sole workflow; coordinator parsed YAML and inspected every retained command. Actual build-only execution is the terminal publication check. Another source-text assertion is unnecessary for this reversible configuration deletion.
  - `[medium]` `[patch]` V1: Pre-verified regression gap: valid-log runs alone do not fail if absent/empty configuration refusal is weakened. Add tests of the same required-directory validation used by the real static gate. No environmental mutation or alternate path.
  - `[false]` `[reject]` I1: The additional local prerequisite resolves the confirmed high forward-only violation required by the owner's standing rule, not an arbitrary strengthening of the new request. Existing hooks already supplied the same five compiler logs.
  - `[false]` `[reject]` I2: carried r0 I2 — the owner's general direction selects permanent local authority immediately, without a one-time qualifier.
  - `[false]` `[reject]` I3: carried r0 I3 — YAML inspection covers configuration now; actual remote build-only publication remains explicitly pending, never represented as proven by local text assertions.
  - `[false]` `[reject]` I4: The complete invocation is documented and executed by coordinator; the CLI unit test intentionally covers its own interface, not the whole test runner. No claim that it alone proves full verification exists.
  - `[false]` `[reject]` I5: carried r0 I5 — pending full/terminal checks are honestly stated, not a completed-result claim.

One medium patch group B3/V1 and one low B1; no high. Further full five-layer review recommended: false. Retained Verification reviewer confirms the final affected repair. Round30683 was cancelled with exit1 before the new test inputs; final full verification will use refreshed matching logs. No findings deferred.

## Verification

Implementer: incremental affected solution/test builds; focused BrowserDeferralTests and CSharpStaticGateTests with --no-build --no-restore, no full run or commit. Coordinator: one final full native solution run using the existing generated BrowserDeferral filter, zero failures/skips; parse workflow YAML and inspect retained triggers/dependencies/main-only deployment. No new implementation-mirroring tests. Then normal hooks, Auto five layers, exact committed-SHA approval and build/deployment-only GitHub publication. No remote test totals are required.

The new policy affects test execution location, not game behavior. The float runtime and its local proof remain unchanged; its deployment receipt will identify the later workflow-policy SHA that contains it.


### Implementation verification — 26 October 2026

Removed both GitHub test steps and their Node provisioning; all production build/publish commands, static checks and main-only artifact/deploy conditions remain. Removed the obsolete remote-binlog test row and remote-filter assertions; the actual filter CLI, exact deferral enumeration and local hook guards remain. Renamed `CiFilter` to `LocalFilter` in its complete caller closure without an alias. No game runtime, float-policy/test inputs, hook operations or deferral entries changed. Legacy deleted: remote test execution and its CI-only expectations/name; no compatibility path introduced. All writes passed Anvil validation.

Fresh worktree build attempt `dotnet build CuriousContraptions.slnx -c Debug --no-restore --nologo -v quiet -m:1` (80308) failed only because NuGet assets were absent (NETSDK1004, 55 errors). `dotnet restore CuriousContraptions.slnx --nologo --disable-parallel -v quiet` (84731) exited 0. The matching incremental Debug solution build with the same build command (61342) passed with **0 warnings, 0 errors, 30.39 s**.

Focused checks reused that build:
- `dotnet test CuriousContraptions.tests/CuriousContraptions.tests.csproj -c Debug -f net10.0 --no-build --no-restore --nologo -v minimal --filter 'FullyQualifiedName~CSharpStaticGateTests'` (86503): **89 passed, 0 failed, 0 skipped**, 6 s.
- `dotnet test tools/E2eRunner.Tests/E2eRunner.Tests.csproj -c Debug -f net10.0 --no-build --no-restore --nologo -v minimal --filter 'FullyQualifiedName~BrowserDeferralTests'` (7159): **5 passed, 0 failed, 0 skipped**, 850 ms. This executes the real `deferred-filter` CLI and checks its exact exclusions.
- `git diff --check` passed. Scoped retired-name/policy search found no remaining `CiFilter` or obsolete remote-test claim in the changed code/policies.

Coordinator checks remain pending: one full native round, workflow YAML inspection, independent Auto review, normal hooks, exact committed-SHA approval and production build/artifact/deployment verification. No local Release worker build, web publish, browser or mutation tests were run. Float runtime proof remains applicable to unchanged relevant inputs, subject to independent closure review; this focused result is not publication or browser qualification.

### Review repair in progress

The original focused94/0/0 result predates the mandatory-log repair. Coordinator full native77326 was cancelled (exit1, SIGINT PID97110) before these input changes; it is not a passing result. New focused static-gate verification will use all five matching compiler-proof logs. Full native verification, normal hooks and independent review remain coordinator-owned.

### Mandatory-log repair evidence — 26 October 2026

Removed the no-log empty-CscRuns branch, evaluated-MSBuild source alternative, its helper and unused JSON import. `CSHARP_STATIC_GATE_BINLOGS` is now required with a visible error; all expected-compilation, PLAYTEST define and actual compiler-input checks remain. Local verification docs specify exact Node 26.12.1 validation, .NET/WASM prerequisites, the five named captures and full solution command. AGENTS separates the full solution round from supplemental hooks. The workflow accurately calls its logs runner-temporary, not uploaded artifacts. Source is frozen after the passing affected run below; documentation-only evidence does not invalidate these compiler inputs.

Environment: SDK **10.0.402**; `dotnet workload list` reported installed `wasm-tools` manifest **10.0.112/10.0.100**, workload version **10.0.401**; `node --version` returned **v26.12.1**. The existing compiler provenance pins passed. Complete compiler-proof captures, all exit0, in **`/tmp/tim-local-gate.UEl4SQ`** (physical `/private/tmp/tim-local-gate.UEl4SQ`):

```sh
dotnet build CuriousContraptions.slnx -c Debug --nologo --no-restore --no-incremental -p:PlaytestDiagnostics=true -noAutoResponse -bl:/tmp/tim-local-gate.UEl4SQ/playtest.binlog -v quiet -m:1
dotnet build CuriousContraptions.slnx -c Debug --nologo --no-restore --no-incremental -noAutoResponse -bl:/tmp/tim-local-gate.UEl4SQ/solution.binlog -v quiet -m:1
dotnet restore CuriousContraptions.web/CuriousContraptions.web.csproj --nologo --disable-parallel -noAutoResponse -v quiet
dotnet build CuriousContraptions.web/CuriousContraptions.web.csproj -c Debug --nologo --no-restore --no-incremental -p:WasmBuildOnlyAfterPublish=true -p:TwoDogAutoImport=false -noAutoResponse -bl:/tmp/tim-local-gate.UEl4SQ/web.binlog -v quiet -m:1
dotnet build tools/anvil/CheckStagedTests.cs --nologo --no-incremental -bl:/tmp/tim-local-gate.UEl4SQ/check-staged-tests.binlog -v quiet
dotnet build tools/anvil/VerifyPolicy.cs --nologo --no-incremental -bl:/tmp/tim-local-gate.UEl4SQ/verify-policy.binlog -v quiet
```

Sessions: playtest1787 **0 warnings/errors,30.06s**; solution29276 **0 warnings/errors,31.02s**; web restore17760 exit0; web67291 **0 warnings/errors,13.68s**; file builds35056 and55613 exit0 with no diagnostics. These are complete compiler-proof captures, not a change to the ordinary incremental development loop. No Release worker build, web publish or full suite ran in this repair.

Focused required check:
`CSHARP_STATIC_GATE_BINLOGS=/tmp/tim-local-gate.UEl4SQ dotnet test CuriousContraptions.tests/CuriousContraptions.tests.csproj -c Debug -f net10.0 --no-build --no-restore --nologo -v minimal --filter 'FullyQualifiedName~CSharpStaticGateTests'`
Session2984: **89 passed,0 failed,0 skipped**,5s. This includes `EveryProjectReportsTheSourcesItCompiled`, proving every expected Csc run is present in its named log, plus all actual compiler-input checks. The coordinator can reuse these matching binaries and logs for the full native round without another build unless relevant inputs change.

Negative provisioning control (not a required passing run):
`env -u CSHARP_STATIC_GATE_BINLOGS dotnet test CuriousContraptions.tests/CuriousContraptions.tests.csproj -c Debug -f net10.0 --no-build --no-restore --nologo -v minimal --filter 'FullyQualifiedName~EveryProjectReportsTheSourcesItCompiled'`
Session27766 intentionally exited1: **0 passed,1 failed,0 skipped** with `CSHARP_STATIC_GATE_BINLOGS is required. Capture the five matching compiler-proof logs described in docs/delivery-workflow.md#local-test-authority.` No missing-log pass or alternate source mode remains. `git diff --check` passed. Full native verification, five-layer follow-up, normal hooks and exact-SHA publication remain coordinator-owned.

### Final configuration regression coverage — 26 October 2026

Added three committed cases in `CSharpStaticGateTests`: absent and empty log-directory configuration must throw `InvalidOperationException` with the mandatory-configuration reason; a nonempty literal directory must be returned unchanged. They exercise the same private `RequireBinlogDirectory` validation used by `CscRuns` through `BinlogDirectory`. No environment mutation, subprocess harness, public API or fallback was added. All real log parsing and inventory checks remain.

Only this test source changed. Refreshed the two affected complete compiler captures in `/tmp/tim-local-gate.UEl4SQ`; reused unchanged `web.binlog`, `check-staged-tests.binlog` and `verify-policy.binlog` from the previous capture because their sources, project references, generated inputs, dependencies and build configuration are unchanged. The web host references the game project, not the test project; the file-based apps have no reference to it.

Commands:
```sh
dotnet build CuriousContraptions.slnx -c Debug --nologo --no-restore --no-incremental -p:PlaytestDiagnostics=true -noAutoResponse -bl:/tmp/tim-local-gate.UEl4SQ/playtest.binlog -v quiet -m:1
dotnet build CuriousContraptions.slnx -c Debug --nologo --no-restore --no-incremental -noAutoResponse -bl:/tmp/tim-local-gate.UEl4SQ/solution.binlog -v quiet -m:1
CSHARP_STATIC_GATE_BINLOGS=/tmp/tim-local-gate.UEl4SQ dotnet test CuriousContraptions.tests/CuriousContraptions.tests.csproj -c Debug -f net10.0 --no-build --no-restore --nologo -v minimal --filter 'FullyQualifiedName~CSharpStaticGateTests'
```

PLAYTEST28748 and solution13911 passed with **0 warnings/errors**,30.19s and30.25s respectively. Affected session34128 passed **92/0/0**,5s, including all required-log controls and all actual compiler-input/expected-compilation checks. Source is frozen; reuse this complete matching log set and binaries for coordinator verification. TODO and sprint now label pre-repair94/0/0 as historical, retain mandatory-log89/0/0, and report final92/0/0 accurately. `git diff --check` passed.

Coordinator full30683 was cancelled (exit1, SIGINT knownPID10700) before these edits; no Pass is claimed. No full suite, commit, push, Release worker build, web publish, browser or mutation run was performed by this repair. Full local verification, retained reviewer confirmation, normal hooks, exact-SHA approval and publication remain pending.

## AutoRunResult — local Auto done; publication pending

**Summary:** GitHub no longer executes game/tool suites or provisions their test-only Node runtime. Local full-solution verification is authoritative, with supplemental normal hooks, mandatory matching compiler logs and the unchanged explicit browser deferral. Production compilation, publish, static checks and main-only Pages artifact/deployment remain. All accepted review repairs are complete; `deferred: []`, `followup_review_recommended: false`. Frontmatter `done` records local Auto implementation/review/verification completion. Normal hooks, exact-SHA SnapshotApproval and terminal publication remain pending; no terminal publication Pass is claimed.

**File closure:** `.github/workflows/pages.yml`; `.githooks/pre-commit` (comment only); `CuriousContraptions.tests/CSharpStaticGateTests.cs`; `tools/E2eRunner.Tests/BrowserDeferralTests.cs`; `tools/E2eRunner/BrowserDeferral.cs`; `tools/E2eRunner/Program.cs`; `AGENTS.md`; `docs/delivery-workflow.md`; `docs/testing-guidance.md`; `TODO.md`; `_bmad-output/implementation-artifacts/sprint-status.yaml`; this spec; and the append-only publication-policy transition in `spec-game-grade-float-comparisons.md`. No game runtime, float numeric policy, float tests, hook operations or browser-deferral entries changed.

**Review breakdown:**

| Round | High | Medium | Low | False | Total | Accepted repairs |
| --- | ---: | ---: | ---: | ---: | ---: | --- |
| r0 | 1 | 4 | 2 | 5 | 12 | F1; B1/B4/I4 invocation group; B2 hook wording; B3 Node prerequisite; B5 temporary-log comment — all resolved |
| r1 | 0 | 2 | 1 | 10 | 13 | B3/V1 committed configuration controls; B1 current evidence — all resolved |

Rejected r0 IDs **B6, I1, I2, I3, I5** and their individual reasons are retained in [Review pass 0](#2026-10-26--review-pass-0). Rejected r1 IDs **B2, B4, B5, B6, B7, I1, I2, I3, I4, I5** and their individual reasons are retained in [Follow-up review pass 1](#2026-10-26--follow-up-review-pass-1). No finding is deferred, and no unresolved accepted repair remains. Each round ran all five Auto layers; there is no additional duplicate review system.

**Retained repair verdict:** the retained Verification reviewer confirmed V1 resolved and no introduced defect by inspecting the actual same-helper path: `CscRuns` → `BinlogDirectory` → `RequireBinlogDirectory`, with committed absent/empty refusal and valid-value tests. The follow-up Forward-Only Auditor confirmed the former no-log alternative removed and no forward-only violation. No further five-layer review is recommended. These are local review conclusions, not publication approval.

**Verification state:** final affected static guards passed **92/0/0** (34128), after matching PLAYTEST28748 and solution13911 captures with zero warnings/errors. Unchanged web/file-app compiler logs remain applicable as documented above. Earlier94/0/0 and89/0/0 results retain their original input scopes. Final full native session **5326 exited0: 4,411 passed,0 failed,0 skipped**, using `CSHARP_STATIC_GATE_BINLOGS=/tmp/tim-local-gate.UEl4SQ` and the real generated deferral filter. Totals: game **3,919/9m6s**, Coverage **77/655ms**, E2eRunner **192/12s**, WorkerAotGuard **17/44ms**, Bundle **206/335ms**. All five projects reported zero failures/skips. Source remained unchanged from matching solution13911 and PLAYTEST28748 captures; unchanged web/file-app logs were reused. Cancelled rounds77326 and30683 remain non-passing historical attempts.

**Remaining acceptance:** capture the frozen staged candidate, run normal hook checks, commit and obtain independent exact-SHA SnapshotApproval, then publish and verify the production build, Pages artifact and deployment for that SHA. No commit or publication is claimed here. Source remains frozen so matching binaries/logs can be reused; browser qualification stays pending until the post-catalogue phase. Synthetic warm-up removal remains the next owner-requested task.
