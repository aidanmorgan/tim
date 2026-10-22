# Independent review: C-Ramp+Receiver

## SnapshotApproval — 22 October 2026

Approved tree `c72dce7cd28eb494f098b906b599b6679a61d865`, binary diff SHA256 `5b8b23bf71385c96f5cfd995a37f15d1775ef9dc5d1101613e942503c2f2d355`, base `e036b7f9eb55afe55d818c18c10578f1b3b5a610`. Independent8ea31b verifies exact identity, no unstaged changes and clean staged whitespace. This receipt may accompany the snapshot.

| Criterion / impact | Review and applicable evidence |
| --- | --- |
| Current design/deletion | Actual cutover9810f3/c41e3f removes Ramp/Receiver records, settings/factories/accessor, compiler/CaptureInstance/whitelist and scalar wire paths. Package-owned typed residence/guide declarations and generic f32 parameters replace them. Capacity recognizes capability. Known shrinks56 entries, no addition or alias. |
| Numeric preservation | Generic authoring/defaults/save retain f32; non-Half inputs are tested directly. Only existing common sensor/guide/collider ABI and AssistanceKnot values retain documented9A44 Half boundaries, including existing upperY addition round point. No old record or compatibility helper is retained to satisfy tests. |
| Historical/content evidence | Prechange6fb3a2:85/85. Immutable36lesson/9A16 fixtures unchanged. Independent8a54c7 verifies fifty Planned rows unchanged. Expected-side normalization62d462 compares actual generic instances directly and separately asserts actual compiled sensor/guide regions, participation, speed/dwell/support against historical values. Current defaults and non-Half save controls independently establish restored authoring precision. |
| Ramp behavior and resize | Real long/short roll/runout and beside/depth controls7ab5c9:1/1. Native generic X/Z resize reaches .125/4, clamps overshoot without ending gesture, Cancel restores and accepted edit has one Undo. Explicit invalid/missing/nonfinite vectors reject. ea23ac:5/5 includes resize, exact f32 save and corrected historical observations. |
| Receiver behavior | Declared guide rotation/descending versus ascending/outside, bounded acceleration and fixed frame; participation/dwell4f10d1:7/7. Speed/fast-pass and partial-residence reset/reentrya63e8d:4/4; source8ea31b shows body motion changes only, sensor state never forged. Existing shifted lesson capture/miss and named-body/foreign-sensor/epoch goal controls retained. |
| Lifecycle/integration | Existing actual engine/JoinedPipe/Springboard/geometry690677:84/84; save/wire/parity/lesson01f886:290/291, sole historical representation failure corrected by ea23ac. Receiver configured-placement forgery remains typed rejection. Isolation/capacity/capture animation and Reset585436:89/89. No invented291/291 aggregate. |
| Admission/build closure | Missing/null participation reference now rejected by capability validation; fixed region malformed controls moved to package declaration boundary while parameter/forged-save controls remain. Bundlefdf67e:35/35. Schema5c2301:24/24, strictTSfa8027, Release worker193245 clean4.34s. Matching final test builda0a917 clean4.19s; original compile/TS/asset failures retained in spec. |
| Warmup and limits | Independent733ab0 confirms redundant Ramp has no unique five-tick warmup behavior; removal preserves Initialize/Construct/Run5/Reset and borrowed-device ownership, without a new resource or hidden identity. Current moving-frame/nudging limitations and common9A44 remain named later scope. |

Resolved same-owner review findings: participation reference admission; historical normalization and f32 expectation separation; redundant warmup identity; capacity declaration ownership; missing speed/reentry causal controls. No unresolved scoped unintended regression identified. Source/test applicability is bounded to the affected closure; no global or browser qualification claimed.

## Story6.5d addendum: 9R.12 and 9R.13

Required-now native Receiver capture/miss/named goal and Ramp roll/runout/sidefall/resize-clamp behavior, exact save/Reset and meaningful rejection controls are covered above. The prechange record-path baseline and post-conversion native proofs are distinct and retained. Actual Chrome cat004/cat054 and affected counterpart suites remain queued for post-catalogue qualification under latest owner instruction; this addendum does not claim those browser suites ran. C# UNIT tests are primary; integration supplemental.

## Publication-dependent checks

SnapshotApproval authorizes normal protected commit/push of this exact candidate plus receipt. Terminal scoped Pass requires normal hooks, independent source/main/remote equivalence, exact-SHA CI native/build success, Pages artifact association and successful deployment. Preserve failed attempts and return concrete failures to this same owner. No redundant local campaign or routine Chrome merely for publication; carry terminal receipt into the next normal slice.

## Terminal scoped Pass — 22 October 2026

Independent2636d4 verified remote main `9b324bbd6a161858b9342562cd0a572d67f9e62a` and source8665e68 share tree `95a3ebda12b491588c40cc4a7872526258518954`; only the authorized review receipt differs from the approved snapshot. Normal protected hooks591514 passed124/124.

Independenta6ec45 verifies CI39697329074 completed successfully on that exact SHA, native2138/2138 with zero failed/skipped in1m14s. Pages artifact12392320102 belongs to the same run/SHA,79267732 bytes, digest `sha256:04ace9de6df95f7ddbf9ff58f25870cfbbe29defde2b363f2511b461112592b8`. Deployment7240389509 identifies the same SHA and succeeded at2026-10-22T00:14:21Z for https://aidanmorgan.github.io/tim/.

Verdict: scoped Pass for required-now C-Ramp+Receiver generic conversion, native behavior and publication, including the native6.5d/9R.12/9R.13 addendum. Original local failures and their corrections remain retained in the spec; browser qualification and named moving-frame/nudging/common9A44 limits are not claimed complete. Carry this terminal receipt into the next normal publication; no standalone administrative commit, Chrome run or duplicate local campaign is required.

## Corrective forward fix — independent review, 22 October 2026: Incomplete

Candidate: base `8665e68b`, uncommitted tracked diff sha256 `ad1c9261…9dc3` plus untracked `tools/E2eRunner/LessonFixtures.cs` sha256 `b348e930…b9bc`. Raw outputs: [evidence-c-ramp-corrective-review](evidence-c-ramp-corrective-review/). No SnapshotApproval; nothing staged.

| Criterion / impact | Command / check | Result |
|---|---|---|
| Matching build | `dotnet build CuriousContraptions.slnx -c Debug` | 0 warnings/0 errors |
| Full solution | `dotnet test CuriousContraptions.slnx -c Debug --no-build --no-restore` | native 2142, E2eRunner 189, Coverage 77, BundleElements 141, WorkerAotGuard 17 = 2566/2566 |
| Exact new CI tool step (`bash -e`) | four `dotnet test tools/… -f net10.0` incl. filter | 77/141/17/165, exit 0 |
| Two-shape lessons reader | `PuzzleContentReader.ReadIndex` admits Planned (indexed, unconstructed) and Playable (`PuzzleDefinitionReader` requires `placements`); content is 50 Planned/`parts`, 12 Playable/`placements`, none mixed | current schema, not a fallback; unknown/mixed/missing rejected (tests) |
| domino_effect | immutable `lessons-484ec38.json`: ball, domino, lamp at all three precisions; current placements ball/domino/lamp locked; pre-2f5650b6 JSON `bowling` was a stale descriptor | correction true; FIX-014-002 retirement forced by exact membership; no current tool reads `docs/coverage/engine` |
| Expectations | 302→303 / 1471→1472 (+lamp), admitted 22→24 (24 packages), `PortSocket` 5 = FirstIn | justified by content/enum, not tuned |
| Bundle | HEAD worktree `dotnet build tools/BundleElements` vs candidate | both `65964d95…2785`, byte-identical |
| Wire TS | schemas/README recipe with flatc 25.12.19 | candidate region identical; HEAD differs at line 1202 |
| Mutations | M1 `capture_margin`→`Fixed(.02)`; M2 Ramp X-extent bound to `width`; M3 editor `parameters.Validate` removed | M1, M2 killed; M3 survived (atomic refusal still held by construction validation) |
| Selector consequence | `E2eRunner select --changed` both_gate / lamp | NoPrimarySuite exit 2 (coordinator-accepted); lamp selects clock-master via domino_effect |
| `git diff --check` | tracked diff | clean (untracked file to be checked with `--cached` after staging) |

Findings:

1. **Medium, blocking (CI step accuracy/coverage).** The class-level filter excludes 24 tests, but with an empty HOME (no npx/tsx/TypeScript cache) 9 of them pass: `IdentityTests.ManifestIsDeterministicAndListsDotFilesAsNotServed`, `PreviewServesTheBundleAndAnotherBundleIsAMismatch`, `PortHeldByAForeignListenerRefusesAfterThreeAttempts`, `LostPortIsRetriedOnANewPort`; `SuiteProcessTests.TruncatedPlanIsIncomplete`, `MissingTsxIsATypedRefusal`, `CleanTapWithNonZeroExitIsAFailure`, both `SkippedOrTodoTestFailsTheFile` cases. Control: the other 15 fail. This matches the implementer's own Linux run (15 failures unfiltered without the npx cache). The YAML comment and spec claim that all 24 drive the browser toolchain are false, and runnable tests are left out of the gap this step closes. Resolve: exclude only the 15 toolchain tests (method-level filter or relocation), correct the comment and spec count; re-check the exact CI step gives 174/174 for E2eRunner and the excluded remainder all fail under empty HOME.
2. **Low, non-blocking.** M3 survives: the Ramp editor test proves atomic refusal, not the typed OutOfRange path.
3. **Low, non-blocking.** `LessonAvailability` duplicates the engine `PuzzleAvailability` at a tool boundary that does not reference the engine; acceptable.

Verdict: Incomplete. Return finding 1 to transport_implementer; re-review the fix only (filter, comment, spec) plus `git diff --cached --check` at staging.

## Corrective forward fix — SnapshotApproval, 22 October 2026

Approved staged tree `361912e59b3c245db8bb16099f8f60049bfd84d7`, binary staged diff SHA256 `13837fbc8ca31fab09a99706ac9e62071347bc1a1fe0e2d80d1ca1e7aeefb04a`, base `8665e68bf018122887c85dd7e61c792e587d77e1` (51 paths: 23 slice files incl. `tools/E2eRunner/LessonFixtures.cs`, plus the review evidence). Only this receipt and its review-only evidence (`rereview/approved-identity.txt`, `diff-cached-check*.log`) are added afterwards. Raw outputs: [evidence-c-ramp-corrective-review/rereview](evidence-c-ramp-corrective-review/rereview/).

| Criterion / impact | Command / check | Result |
|---|---|---|
| Findings re-review: changed inputs | pre-staging tracked diff `3bb360de…b0b7`; only `pages.yml`, `PalettePlacementTests.cs`, spec/TODO text changed since the Incomplete review | affected closure re-checked below; earlier bundle/wire/reader/domino/M1/M2 proof unchanged in applicability |
| Matching build | `dotnet build CuriousContraptions.slnx -c Debug` | 0 warnings/0 errors (up to date with the changed test) |
| Full solution | `dotnet test CuriousContraptions.slnx -c Debug --no-build --no-restore` | 2142 + 189 + 77 + 141 + 17 = 2566/2566 |
| Exact CI step, extracted verbatim from YAML, `bash -e` | `rereview/ci-step-extracted.sh` | Coverage 77, BundleElements 141, WorkerAotGuard 17, E2eRunner 174; exit 0 |
| Finding 1: exclusion is exactly the toolchain set | YAML `toolchain` list vs the 15 empty-HOME failures; the 15 alone and the other 174 under `HOME=$(mktemp -d)` | lists identical; 15/15 fail; 174/174 pass |
| Finding 2: editor discrimination | M3 again (editor `parameters.Validate` removed) in a disposable worktree | killed: expected "Invalid declared parameter value.", actual "Unexpected fault (ArgumentException)…"; unmutated passes in slnx |
| Whitespace | `git diff --cached --check` | slice files clean; only raw, unaltered review logs carry tool-emitted trailing blanks (no hook enforces this; prior raw evidence logs do the same) |

Findings resolved: 1 (CI filter now excludes only the 15 toolchain tests; YAML comment and spec state the 15 and the 9 now running) and 2 (typed OutOfRange status asserted). Finding 3 stays Low and accepted. No unresolved findings within required-now scope.

Non-claims: browser/Chrome qualification stays pending for the post-catalogue E2E phase. The 15 toolchain E2eRunner tests (TypeCheckTests 2, SuiteProcessTests 8, IdentityTests 5) are not run in CI; they ran locally in the slnx run. The selector's NoPrimarySuite for both_gate/hold_timer is an accepted, explicit gap pending their suites. No global audit, performance or runtime qualification is claimed.

## Corrective forward fix — Publication-dependent checks

SnapshotApproval authorizes normal protected commit of exactly this candidate plus this receipt, merge to main and push, with no `--no-verify`. Terminal scoped Pass requires:
- an independent check that the source commit tree, main and remote match the approved tree, differing only by this receipt and its review evidence;
- normal hooks passing;
- exact-SHA CI success, including the native step and the new "Test C# tools" step with E2eRunner 174 and zero failed/skipped;
- the Pages artifact associated with that run/SHA;
- a successful deployment of that SHA.

Return concrete failures to the same owner. No routine Chrome run or duplicate local campaign is needed for publication.

## Corrective forward fix — terminal publication verification, 22 October 2026

Raw outputs: [evidence-c-ramp-corrective-review/terminal](evidence-c-ramp-corrective-review/terminal/).

| Check | Evidence | Result |
|---|---|---|
| Source commit | `0b3607f5cdbe1c0739f690359c296f1b4fc0c1bc`, parent `8665e68b` | tree `4c160f0a63bf189571c85b3b48a3c68c21659a69` = approved+receipt tree |
| Main / remote | `git ls-remote origin refs/heads/main` → `59ecb70b8d161045d08c1f3de802cdad89e23eef` (`--no-ff`, parents `9b324bbd`, `0b3607f5`) | same tree `4c160f0a…` |
| Approved vs published | `git diff --stat 361912e5 4c160f0a` | only the review receipt and three review-only evidence files (47 insertions) |
| CI | run 39704720283, push on main, headSha `59ecb70b…` | completed/success; build job 119581933674 all steps success, incl. "Test C# tools"; deploy job 119586266965 success |
| CI totals from the job log | `gh run view --job 119581933674 --log` | native 2142/2142; tools: Coverage 77, BundleElements 141, WorkerAotGuard 17, E2eRunner 174; zero failed/skipped |
| Pages artifact | `actions/runs/39704720283/artifacts` | `github-pages` 12394544072, run 39704720283, SHA `59ecb70b…`, 79267631 bytes, `sha256:c9793ac881917047d9cdd3034e8f5a973b65f5f59581e2920b314e261c27ac2c`; the deploy log uses the same artifact id |
| Deployment | `deployments?sha=59ecb70b…`, statuses | 7241421567, environment github-pages, ref main; success at 2026-10-22T01:30:39Z for https://aidanmorgan.github.io/tim/ (live page HTTP 200) |

Hooks 124/124 come from the implementer's report and were not independently re-run; the CI static gate and both test steps passed on the exact SHA.

Verdict: terminal scoped Pass for the corrective forward fix. It covers the strengthened Ramp/Receiver native proof, flatc TS parity, the typed lesson fixture readers, the domino_effect fixture correction, the BothGate/HoldTimer package rows, and the CI tool-test step. Non-claims are unchanged:
- browser qualification stays pending for the post-catalogue E2E phase;
- the 15 toolchain E2eRunner tests remain outside CI;
- the selector's NoPrimarySuite for both_gate/hold_timer remains an accepted gap.

Carry this receipt into the next normal publication. No standalone commit is required.
