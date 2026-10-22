# Independent review: C-Balls (9A.30 + 9A.31 + 9R.11)

Reviewer: independent adversarial reviewer (did not implement). Date: 22 October 2026. Candidate: base `59ecb70b` -> WIP `41e3381e` (tree `e4e06518`) -> merge `0795c235` (tree `38860f7f`; parents `41e3381e`, main `b35be7f4`) + uncommitted delta (carried 9.7a terminal receipt and evidence, four planned-doc edits, spec, merged evidence, this review). Raw outputs: [evidence-c-balls-review/](evidence-c-balls-review/).

## Verdict: Pass (scoped, native required-now). SnapshotApproval below.

No blocking finding. Browser/Chrome qualification (9R.11 `cat-001`, and the 9A.30/9A.31 Chrome ACs) stays pending for the post-catalogue E2E phase under the 21 Oct policy and is not claimed. No global, performance or runtime qualification is claimed.

## Commands run by the reviewer (this worktree, merged candidate)

| Check | Command | Result | Raw |
|---|---|---|---|
| Debug build | `dotnet build CuriousContraptions.slnx -c Debug` | 0 warnings, 0 errors | `build.txt` |
| Full solution | `dotnet test CuriousContraptions.slnx -c Debug --no-build --no-restore` | WorkerAotGuard 17/17, BundleElements 143/143, Coverage 77/77, E2eRunner 189/189, CuriousContraptions.tests 2245/2245 = 2671/2671 | `full-test.txt` |
| CI "Test C# tools", verbatim step body under `bash -e` | `ci-tools-step.sh.txt` | Coverage 77, BundleElements 143, WorkerAotGuard 17, E2eRunner 174 (15 toolchain excluded); exit 0 | `ci-tools.txt` |
| Release worker | `dotnet build CuriousContraptions.Simulation -c Release --no-incremental` | 0/0 | `worker-release.txt` |
| Strict TS | TypeScript 6.0.3 `tsc --noEmit --project tools/e2e/tsconfig.json --typeRoots <@types/node 26.0.1>` (`strict: true`) | exit 0 | `tsc.txt` |
| Snapshot schema unit | tsx 4.23.15 `--test tools/e2e/unit/run-reset-snapshot.test.ts` | 24/24 | `ts-snapshot-unit.txt` |
| Whitespace | `git diff --check` (worktree) | clean; vs base only raw captured evidence logs/patches (verbatim tool output) | `diff-check.txt` |

## Criterion / impact matrix

| # | Criterion | Independent check | Result |
|---|---|---|---|
| 1 | `IsPayload` derivation correct for every package; Domino excluded; ordering preserved | Predicate evaluated over all 25 bundle entries (`payload-predicate-bundle.txt`): payload set = {ball, bowling} exactly = the deleted `WorkshopBall` set; Domino (dynamic box), Bumper (static sphere), slider bodies excluded; Mechanical-null parts false. Orderings read in code: capture targets ascending id (unchanged), telemetry lowest id (unchanged), named body/presence gate = first in instance order (old `Find<WorkshopBall>` is first-in-order). Mutants M1 (any dynamic), M2 (no sphere check), M3 (no dynamic check), M9 (capture order), M10 (telemetry order), M11 (no lesson payload) all killed (7/7/7/1/1/6 failures). M4 (drop slider clause) survives: equivalent for every current package (Low L1) | Pass |
| 2 | Save guard: forged typed fields refused; old shifted saves refused, never misread | M5 (guard off) 56 failures; M6 (TargetCount omitted) 3; M7 (save prevalidation guard only) 60; M8 (command-decode guard only) 54: both paths independently covered. Old-layout probe: 18 saves produced by the base (59ecb70b) production encoder, decoded by the candidate save boundary (`old-save-probe/`): ball, bowling, spring (default and custom), battery (default and custom), bumper, domino, pipe, pressure plate, clock, counter, default ramp each refused with a typed `WorkshopSaveRejection` (OutOfRange/VerifierRejected/MissingField). Lamp, latch, ball_detector, funnel, pipe_bend_45 load unchanged: they carry no field at slot 6 or later, so the layout and meaning are identical (not a misread). The spec's claim is correct and in fact broader than stated | Pass |
| 3 | Wire field removal and regenerated code match the generator | flatc 25.2.10 per schemas/README (`wire-regeneration.txt`): TS generated part byte-identical; C# differs only by the header blank line, which is already absent at base (pre-existing). Snapshot schema listing 24/24 | Pass |
| 4 | Behavior preservation | Golden fixtures `compiled-geometry-9a16.json` and `lessons-484ec38.json` unchanged vs base (empty stat). Lesson-golden expected-side normalisation reviewed: it asserts each historical ball material equals the compiled body/collider/material rows (radius, mass, drag, bounce, friction, threshold, rolling; buoyancy 0) before removing record-only fields; pose/identity still compared. Independent solved-world probe (`solved-world-probe/`), identical source at base and candidate: first_principles (2 Ramps), spring_forward (Springboard), bumper_sidekick (Bumper), domino_effect (4 Dominoes) with content solution poses, 3 precisions: admitted world bytes, every committed body read over 1200 ticks and Reset state byte-identical 12/12; first_principles and spring_forward actually capture (latched reads > 800); bumper_sidekick/domino_effect cited as equivalence only. Implementer's 36/36 unsolved-world probe accepted as consistent. Missing native solve-runs for 10 lessons judged non-blocking: compiled inputs are byte-identical and the only changed code on that path (payload selection, capture targets, capacity, named body) is proven identical for the current bundle (L5) | Pass |
| 5 | Known rows: exactly 50 removed, none added; deleted symbols gone | `git diff b35be7f4 HEAD` Known list: 50 `-`, 0 `+` (Basketball 44, Bowling 4, Warmup 2); 0 rows remain owned by Basketball/Bowling. `rg -w "WorkshopBall|BallMaterial|CaptureBall"` 0 production hits (tests: only the detector's own name sample); `WorkshopInput.(Ball|Basketball)`, `Definition.Ball`, `construction.Ball`, `.Ball!/?` 0 hits anywhere; no `Radius/Drag/Buoyancy` in content; no `Add(Radius|Drag|Buoyancy)` callers (`deletion-greps.txt`, `known-rows.txt`) | Pass |
| 6 | Warmup ball removal safe | The exact candidate warmup construction (battery, plate, powered gate, two wires) validates, compiles, runs 5 real ticks and Resets exactly through the production simulation (`warmup-probe.txt`; two failed attempts were the reviewer's own wrong assertions, retained). No dynamic body is warmed any more: possible first-Run hitch belongs to the performance gate (L4) | Pass |
| 7 | Ratchet scanning ignored `CuriousContraptions.web/AppBundle` | `ElementIsolationTests.Outputs` excludes bin/obj/.godot/.git and node_modules only; `**/AppBundle/` is gitignored; CI publishes after tests, so CI is unaffected; no AppBundle present for this review. Classified pre-existing local hazard (L3) | Non-blocking |
| 8 | Merge brings only main's changes plus resolutions | `merge-audit.txt`: merge-base(41e3381e, b35be7f4) = 59ecb70b; per-file patch-ids equal on both sides except `BasketballResourceTests.cs` (main's `electrical_nand` registry line on top of the candidate's rewrite), `ElectricalNandTests.cs` (two `WorkshopInput.Basketball` -> `WorkshopInput.Generic(PartId.Basketball, ...)`, the only semantic resolution) and `TODO.md` (textual: both slices' status kept) | Pass |
| 9 | Uncommitted delta | Carried 9.7a receipt: `review-9-7a-electrical-nand.md` is +24/-0 vs merge and byte-identical to the story-9-7a worktree copy; `evidence-9-7a-review/terminal/` identical (`diff -r`); sprint-status changes only the 9-7a line (copied verbatim). Planned-doc edits (CAT-064, EL-189, EL-208, vertical-delivery CAT-014 row) replace stale `WorkshopBall`/`BallMaterial` engine descriptions with the package/payload path; legacy-harvest facts unchanged | Pass |
| 10 | Closed sets / no fallback / no migration | Typed refusals are enums (`WorkshopSaveRejection`, `WireFaultReason`); no deprecated slot, alias, migration or bridge; old saves refused | Pass |

## Mutation receipts (disposable worktree at 0795c235, full native project each)

M0 unmutated control 2246/2246 (incl. reviewer probe). Diffs and outputs in `evidence-c-balls-review/mutations/`. Killed: M1, M2, M3, M5, M6, M7, M8, M9, M10, M11. Survived: M4 (slider clause), equivalent for the current bundle.

## Findings (none blocking)

- **L1 (Low) Untested payload slider clause.** Criterion: tests discriminate the predicate. Risk: the `Sliders.Count: 0` clause is unobservable (M4 survives); the spec calls it part of the "existing" rule, but the deleted `PartRegistry.Ball` had no slider clause. Not blocking: no current package is a dynamic body with sliders, so payload membership is identical to the deleted record set. Smallest resolving check: delete the clause, or add a synthetic-entry test, when the first dynamic slider package (or the next touch of `DeclaredPhysics`) lands.
- **L2 (Info) Old-save refusal is broader than the spec states.** Every shifted typed record, not only ball/bowling/domino/pipe/ramp, is refused with a typed reason (criterion 2). No action.
- **L3 (Low, pre-existing) AppBundle ratchet hazard.** Owner: isolation-ratchet maintainers; one-line exclusion outside this slice. Publication check: no `CuriousContraptions.web/AppBundle` present when hooks run.
- **L4 (Low) Warmup no longer exercises a dynamic body.** Owner: performance qualification gate.
- **L5 (Low) Native lesson solve-runs.** Only joined_pipe and spring_forward have native goal solve-runs; the rest rely on byte-identical equivalence (criterion 4). Browser lesson solves stay queued for post-catalogue E2E.
- **L6 (Process, disclosed) Tool use.** The implementer disclosed `sed -i`, `python3` and `patch` file edits and some Anvil validations after the write; content was reviewed in the diff. The reviewer used `sed -i` once per disposable worktree on a throwaway `.csproj` include and `python3 -I` read-only over JSON; no deliverable was edited by shell.
- **L7 (Cosmetic)** carried sprint-status comment reads `Passb35be7` (verbatim from the 9.7a worktree).
- **L8 (Spec wording)** Ordered tasks say mark `9r-11` done at publication; Current state says it stays backlog. Resolution below: `9r-11` stays backlog (Chrome ACs pending), and no sprint-status change is made in the publication commit.

## Story 6.5d addendum: 9R.11

Required-now native Basketball proof is covered above and in the spec evidence: fall/bounce/rest at 0.34 m, resting no-drift control, Receiver capture of either ball and shifted-miss control, exact Run/Reset and Save/Load of both kinds, and first_principles reaching the Receiver with the content Ramp solution in the real simulation (solved-world probe, latched captures, identical to the record path). Actual Chrome `tools/e2e/cat-001.test.ts` and the counterpart suites (`cat-014`, `cat-015a`, `cat-052`, `cat-005`, `cat-002`, `cat-023a`, `engine-core-2a1`..`2c`) remain queued for post-catalogue qualification; this addendum does not claim any browser suite ran. C# UNIT tests are primary; integration supplemental.

## SnapshotApproval - 22 October 2026

Approved staged tree `42279728750828a97b50c3a10fa11e4e8ead697f`; SHA256 of `git diff --cached --binary 0795c235` = `ded086b48da3006ec040a115af1bc22e17238e497a70cdc564397cd17d9355ed` (72 paths: 65 added, 7 modified; slice delta = carried 9.7a terminal receipt/evidence, four planned docs, spec, sprint-status 9-7a line, merged evidence, this review and its evidence). Parent chain: `59ecb70b` (base) -> `41e3381e` (WIP candidate) -> `0795c235` (merge; parents `41e3381e`, main `b35be7f4`) -> staged delta. Every pre-publication criterion above holds for this exact snapshot: one matching 0-warning Debug build, full slnx 2671/2671, the verbatim CI tools step, Release worker, strict TS, snapshot schema, worktree whitespace clean, `git diff --cached --check` clean outside raw evidence. Only this SnapshotApproval and Publication-dependent sections are added to this file after that identity; no other path changes.

## Publication-dependent checks

SnapshotApproval authorizes the implementer to commit exactly this snapshot (plus these two appended sections) on `story-c-balls` with normal hooks (no `--no-verify`), merge `--no-ff` to `main` and push. Do not change `sprint-status.yaml` in the publication commit: `9a-c-balls-conversion`, `9a-30`, `9a-31` move to done in the terminal receipt carried into the next normal slice (as 9.7a's was here; the isolation ratchet then requires zero Basketball/Bowling rows, which holds), and `9r-11` stays backlog until its Chrome ACs run. Ensure no `CuriousContraptions.web/AppBundle` exists in the worktree when hooks run (L3). Terminal scoped Pass requires, independently verified:
- tree equivalence: the story commit tree equals the approved staged tree except this review file; the merge commit on main and `origin/main` carry that tree with main's then-current changes only;
- normal hooks passing on commit and merge;
- exact-SHA CI success on the pushed main SHA, including "C# static gate", "Test C# game" (native count, zero failed/skipped) and "Test C# tools" (Coverage, BundleElements, WorkerAotGuard, E2eRunner 174, zero failed/skipped), then publish and static-deployment checks;
- the Pages artifact associated with that run and SHA;
- a successful `github-pages` deployment of that SHA.

Return concrete failures to the same implementation owner. No routine Chrome run or duplicate local campaign is required for publication; browser qualification remains pending.

## Terminal publication verification — 22 October 2026

Raw outputs: [evidence-c-balls-review/terminal/](evidence-c-balls-review/terminal/).

| Check | Evidence | Result |
|---|---|---|
| Identities | `trees.txt`, `ls-remote.txt` | Slice commit `e3769cc16d99336ee40acc96cdf5ebdffbeb57bc` (tree `3b7d3c8fa6c89339157bcf802cb7daedab9874ad`, parent `0795c235`); merge `3fce7a034b328be0a79f834a2a70a4d049372a4b` (`--no-ff`, parents `b35be7f4`, `e3769cc1`, same tree `3b7d3c8f`); `git ls-remote origin refs/heads/main` = `3fce7a03` |
| Tree equivalence | `git diff-tree` | Approved tree `42279728` vs published tree: only `review-c-balls.md` (the authorized receipt). Staged T2 `3b7d3c8f` = slice tree = merge tree = origin/main tree (b35be7f4 is an ancestor of the slice, so the merge adds nothing) |
| Hooks | `hook-rerun.log` | Implementer reports the hook passed on the slice commit; no hook ran on the clean merge. Re-run on the exact published tree (HEAD `e3769cc1`, index tree `3b7d3c8f`, nothing staged): `sh .githooks/pre-commit` exit 0; solution, playtest and web gate builds 0 warnings/0 errors; static-gate and UI-failure tests 124/124; staged-test check and anvil hook passed |
| Exact-SHA CI | `run.json`, `jobs.json`, `jobs-summary.txt`, `build-job.log` | Run 39712582817 "Build and deploy game", push on main, head SHA `3fce7a03`, attempt 1, completed/success. Build job 119607139032 success, every step success (C# static gate 3× 0 warnings/0 errors; Test C# game; Test C# tools; publish; static deployment; upload-pages-artifact). Native: CuriousContraptions.tests 2245/2245, 0 failed/skipped. Tools step: Coverage 77, BundleElements 143, WorkerAotGuard 17, E2eRunner 174, all 0 failed/skipped |
| Pages artifact | `artifacts.json`, `deploy-job.log` | Artifact 12398102440 `github-pages`, 79471119 bytes, digest `sha256:3d53aa0bfa3ee7bfb2400220d68fc11cbf80d9668e3ee4a40d4ff4ea76585a6e`, workflow run 39712582817, head SHA `3fce7a03`, not expired; deploy job fetched artifact id 12398102440 |
| Deployment | `deployment.json`, `deployment-statuses.json`, `recent-deployments.txt`, `site-head.txt` | Deployment 7242501369, SHA `3fce7a03`, environment github-pages, waiting → queued → in_progress → success at 2026-10-22T02:53:26Z, log = deploy job 119611109038 of the same run; it is the latest github-pages deployment; https://aidanmorgan.github.io/tim/ returns HTTP 200, last-modified 02:53:23 GMT |
| Status flip | `status-flip-ratchet.txt` | After the coordinator-authorized flip of `9a-c-balls-conversion`, `9a-30`, `9a-31` to done (9r-11 stays backlog), the sprint-status readers ElementIsolationTests + WorkshopPhysicsLawTests pass 93/93 on the unchanged build: no Basketball/Bowling Known row remains |

Verdict: **terminal scoped Pass** for required-now C-Balls (9A.30 + 9A.31) generic conversion, native behavior and publication, including the native 6.5d/9R.11 addendum. Browser qualification (`cat-001` and counterpart suites) remains pending for the post-catalogue E2E phase; no global, performance or runtime qualification is claimed. Findings L1–L8 stay as recorded (non-blocking). Carry this receipt and the sprint-status flip into the next normal publication; no standalone administrative commit is required.
