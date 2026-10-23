# Review: Story 9.4c Latch-Fixture Deferred Integrations

Independent adversarial review (pre-merge). The reviewer did not write the candidate and edited no candidate file.

- **Candidate:** worktree `story-9-4c`, base `55ee05b9`, uncommitted. `LatchSupplyFixtureTests.cs` sha256 `f9b1ec7f…ee206f`; the other identities are in `evidence-9-4c-review/identity.txt`.
- **Verdict: Incomplete.** The native proof passes and discriminates. One small documentation finding (F1) blocks it. After F1 is resolved and re-reviewed, the verdict becomes Pass-pending-merge. That is a pre-merge verdict only: no SnapshotApproval, because main will move (C-Bumper+Spring, OR) and must be merged first.
- **Browser qualification:** pending, by recipe only (post-catalogue E2E).

## Commands and results (evidence-9-4c-review/)

| Check | Result | Log |
|---|---|---|
| `dotnet build CuriousContraptions.slnx -c Debug` | 0 warnings, 0 errors | build.log |
| `dotnet test CuriousContraptions.slnx -c Debug --no-build --no-restore` | tests 2288, E2eRunner 189, Coverage 77, BundleElements 145, WorkerAotGuard 17; 0 failed | test-full.log |
| CI "Test C# tools" step (pages.yml:51-64) under `bash -e` | 77 / 145 / 17 / 174 passed, exit 0 | ci-tools-step.log |
| `git diff --check` (new file intent-added, index restored afterwards) | exit 0 | diff-check.log |
| Throwaway-worktree baseline (fixture filter) | 10/10 passed | mutation-R0-baseline.log |

## Mutations (throwaway worktree, each reverted; worktree removed and pruned)

| ID | Mutation | Killed by | Log |
|---|---|---|---|
| R1 | `Resolve` ignores the latch contact | 7/10 fail: Set, both Reset rows, both ball rows, the no-B control and off/on | mutation-R1-contact-ignored.log |
| R2 | Battery enable ignored | Authored-Disabled control and supply off/on | mutation-R2-enable-ignored.log |
| R3 | Latch `ResetIn` ignored | Reset-open, mid-stroke and both ball rows | mutation-R3-latch-reset-ignored.log |
| R4 | Route reads the latch one tick later (engine lag +1) | Set, Reset-open and mid-stroke. Run/Reset also fails, partly because of a stale mutation field | mutation-R4-lag-plus-one.log |
| T0 / T2 | Test-side `SupplyLagTicks` set to 0 / 2 against the real engine | T0: Set, Reset-open, mid-stroke. T2: Set only (the Reset rows' far edge is pinned only at lag 1; R4 covers the engine side) | mutation-T0/T2-*.log |
| R5 | Return spring 14 → 0.001 N/m | 5/10, including Reset-open, mid-stroke and both ball rows | mutation-R5-spring-disabled.log |
| R6 | Blade collider moved 3 m sideways | 5/10. The crush row is killed by its **guard** (the ball has already passed), not by the fact-14 assertions | mutation-R6-blade-collider-removed.log |
| R7 | Spring 3000 N/m | 9/10, by guards and opening (the blade never opens), not by the crush assertions | mutation-R7-crushing-spring.log |
| R8 | Contact softness 60 → 1 Hz (crush proxy) | Only the crush row fails, at its assertion "temporary penetration ≤ 5% of the blade, tick 221". This is the real fact-14 discriminator | mutation-R8-soft-contacts-crush.log |

## Criterion and impact matrix

| Criterion | Evidence | Status |
|---|---|---|
| AC1 Set; N19 no topology change | `SetCloses…`: edges Off@T and On@T+1, first opening tick T+1. `Construction` has value equality (`WorkshopConstruction` record struct, `WorkshopConnections : IEquatable`), compared after every tick and after Reset | Pass. Lag value: see D-T14 |
| AC2 Reset when open; N17; spring return | Latch and lamp clear at T′, unsupplied from T′+1, spring-damper law residual ≤ 0.5 m/s², energy bound, closed within 240 ticks | Pass |
| AC3 mid-stroke; upstream ball; ball partly through (fact 14) | Guards (0.05, 1.50) m never skip; coasting until reversal follows the frozen row "speed strictly decreasing until it reverses"; R8 kills the crush row | Pass. The "State Blocked" part of fact 14 is not proven: F1 |
| AC4 controls | No-B (the ball passes downstream), authored Disabled, off/on with memory and occurrence unchanged | Pass |
| AC5 one battery, one gate | Builder | Pass |
| AC6 Run/Reset and Save/Load | `AssertSameState` and presentation bits Built==Reset, construction equality, tick-for-tick replay after Reset. Real `WorkshopSaveCodec` round trip with byte re-encode equality, five typed connections, replay identical | Pass |
| AC7 Chrome | Superseded in cadence by the 21 Oct policy. Recipes are in the spec and TODO | Pending (recorded) |
| Production path | Real `WorkshopSimulation` on `WorkshopGpuDevice` + `BrowserGpuTransport`. Only the clock and installation doubles are used (the existing `WorkshopPhysicsEngineTests` pattern). No setters; supply changes only through `ResetIn` or the in-run enable | Pass |
| Durability | Committed reads only. The record factories (`WorkshopInput.Battery/Latch/Lamp`) are confined to `Machine` | Pass. See N2 |

## Coordinator decisions evaluated

- **D-T14 (1-tick lag): correct.**
  - In `WorkshopGpuDevice.cs:120-123`, `AdvanceScheduled`, then `Compose`, read the committed checkpoint before the physics step.
  - The strike's edge and latch resolution happen only in `ConsumeScheduled` (`:185`), after the step.
  - `ContactClosed` reads `Activations` (`ActivationNetwork.cs:243-253`). Presentation `SupplyClosed` is that tick's composed input.
  - So T+1 is the earliest possible route change. `BothGateTests:150,154` pins the same lag (120→121, 180→181).
  - The "+2" came from the legacy command queue: `git show a6c914e:CuriousContraptions.tests/LatchTests.cs` L40-45 show `world.Activate(set)`, then Off, `Step`, Off, `Step`, On.
  - The tests pin both sides of the Set edge and of each Reset edge (On@T′ / Off@T′+1).
  - Required record (non-blocking): AC1, the frozen Set row and the TODO "awaiting confirmation" still read +2. Add a dated line recording the coordinator decision, with owner renegotiation of the frozen text still open.
- **D-Blocked residual: observation confirmed; no 9.4c acceptance requires it.**
  - `probe-4-creep.log` shows v = −1.083e-5 m/s, constant from tick 241 to 2400.
  - `PhysicsPresentationRead.cs:29` requires |v| < 1e-5, so the indicator shows "unpowered" instead of "Blocked" (`WorkshopDeclaredPresentation.cs:282`).
  - The frozen row does not require the state, but see F1.

## Findings

**F1 (blocking, documentation and ownership).**
- *Criterion:* current documents must be accurate, and a known defect stays explicit with an owner (AGENTS.md).
- *Problem:* the new CAT-051 §5 status line says fact 14 is "proven natively". Fact 14 (row 14) includes "State Blocked", and that state reads false on this engine. The "assigned to C-Gate" handoff is recorded only in the spec's Implementation Notes: the C-Gate, 9A.19 and 9A.21 ACs and TODO do not mention it. The threshold is also CAT-051 open question 3 (owner).
- *Risk:* C-Gate, or the post-catalogue E2E, inherits an overclaim and loses the defect.
- *Smallest resolving check:*
  - amend the CAT-051 status line to exclude the Blocked classification and link open question 3;
  - add a TODO (or C-Gate) note naming the owner (C-Gate, threshold decision owner Q3);
  - the reviewer re-reads both lines.

**N1 (non-blocking).** D6's mapping, under which "`cat-005-n19`/`cat-051-interrupt` pass" means `LatchSupplyFixtureTests` passes, exists only in this spec. The C-Gate, 9A.39 and 9A.43 ACs do not mention it. Consider a one-line pointer next to those ACs.

**N2 (non-blocking).** C-Lamp+Detector (position 36) deletes `WorkshopInput.Lamp`, so it must also edit `Machine`. D5 lists only C-Gate, 9A.39 and C-Memory.

**N3 (non-blocking).** The gate drive debits 0 battery energy. CAT-005 already lists powered-work verification as outstanding (line 43); 9A.39 or the owner should keep it explicit.

**N4 (non-blocking process).** The implementer self-reported two edits made outside the Write and Edit tools (the probe file, since deleted, and the spec's `status` line). The content is as intended.

## Re-review scope after F1

Documentation only: CAT-051 §5, plus TODO or C-Gate. No runtime input changes, so this native evidence is reused. Before SnapshotApproval, merge main, then run one matching build, the full `--no-build` test run and the fixture filter again.

## Re-review 1 (22 Oct 2026): F1 fixes, documentation only

**Verdict: Pass-pending-merge** (pre-merge). This supersedes the Incomplete verdict above. No SnapshotApproval: main must be merged first, and then the post-merge checks listed above apply.

- **F1: resolved.**
  - The CAT-051 §5 status line now says the "State Blocked" part of fact 14 is **not** proven. It cites the −1.083e-5 m/s residual against the 1e-5 predicate, links `#6-open-questions` (that heading exists), and names the C-Gate assignment.
  - TODO.md records the dated handoff, with the C-Gate implementation owner, the native-proof requirement and OQ3.
  - The epics.md C-Gate section has the same dated coordinator decision.
- **D-T14 record: resolved.** Spec D3 is dated as a coordinator decision under owner delegation and supersedes the "+2" wording in the frozen Set row, AC1 and CAT-037 T14. The AC1 note and the Spec Change Log agree. The frozen block is byte-identical to HEAD: the awk-extracted block hashes to sha256 `47bd45a8…4a83` in both HEAD and the worktree.
- **N1: resolved.** A native-guard pointer line now sits under the suite ACs of 9A.19 (C-Gate), 9A.39 and 9A.43, and keeps the browser suites pending.
- **No runtime input changed.**
  - `LatchSupplyFixtureTests.cs` sha256 is still `f9b1ec7f…ee206f`.
  - `git diff --stat` over engine, elements, Simulation, ui and tests shows only the earlier csproj Compile Include.
  - So the build, test, CI and mutation evidence above is reused unchanged.
  - `git diff --check`: exit 0 (re-run).
- **Residual (non-blocking):**
  - CAT-037 T14 itself is not annotated. D3 and TODO record that it is superseded.
  - N2 and N3 stay open as notes.

## Post-merge review and SnapshotApproval (22 Oct 2026)

**Verdict: Pass. SnapshotApproval is granted** for the exact staged snapshot below. Raw outputs are in `evidence-9-4c-review/post-merge/` and `evidence-9-4c-review/snapshot/`.

### Parent chain

- HEAD is merge `80ffc68589070a6e18839986861f54a358b557ad`, with parents:
  - `5e840026af2f7babf1282655e0bcda2dbeffd566` (WIP 9.4c, parent `55ee05b9`);
  - main `a2eddf811323d95819fef5eeeb50a76a63bf428f` (Merge 9.8a OR, parents `711568c4` and `28a59b71`).
- The merge base is `55ee05b9`.

### Merge integrity

- `5e840026` holds the reviewed candidate:
  - `LatchSupplyFixtureTests.cs` sha256 is `f9b1ec7f…ee206f`, unchanged;
  - the review record includes Re-review 1;
  - `evidence-9-4c-review/` has 16 files.
- The same files changed in `55ee05b9..5e840026` and in `a2eddf81..80ffc685`. Every 9.4c file is byte-identical to its `5e840026` blob, except the three files both sides touched.
- Every main-side file is byte-identical to its `a2eddf81` blob, except the same three files. No main-side deletion was resurrected.
- The three shared files:
  - the csproj is a union (the `LatchSupplyFixtureTests` include sits beside `ElectricalOrTests`);
  - `sprint-status.yaml` takes only the 9.4c `review` flip;
  - `TODO.md` keeps main's receipts and 9.8a state, and keeps the 9.4c in-flight item with the D3 decision and the C-Gate Blocked handoff.
- No 9.4c content was lost.

### Post-merge checks (one build of `80ffc685` plus the documentation-only delta)

| Check | Result | Log |
|---|---|---|
| AppBundle absent | 0 matches | post-merge/appbundle-check.txt |
| `dotnet build CuriousContraptions.slnx -c Debug` | 0 warnings, 0 errors | post-merge/build.log |
| Full `dotnet test … --no-build --no-restore` | tests 2362, E2eRunner 189, BundleElements 148, Coverage 77, WorkerAotGuard 17; 0 failed; exit 0 | post-merge/test-full.log |
| Named `LatchSupplyFixtureTests` | 10/10 | post-merge/test-latch-fixture.log |
| CI "Test C# tools" step under `bash -e` | 77 / 148 / 17 / 174 passed, exit 0 | post-merge/ci-tools-step.log |
| `git diff --check` | Exit 0 on the worktree before staging. `--cached 80ffc685` flags trailing whitespace, but only inside verbatim raw logs (`evidence-c-bumper-spring-review/terminal/build-job.log`, `evidence-9-4c/post-merge/build.log`) and the receipt copy (`snapshot/cbs-receipt-worktree.patch`). Raw evidence is kept unedited, as in 772 committed artifact files at HEAD, and the pre-commit hook has no whitespace gate. No authored file is flagged | post-merge/diff-check.log |

### Mutations on the merged engine (throwaway worktree at `80ffc685`, removed and pruned)

| ID | Mutation | Result | Log |
|---|---|---|---|
| R1 | Supply ignores the latch contact | 7/10 fail | post-merge/mutation-R1-contact-ignored.log |
| R4 | Off-by-one lag | Set, Reset-open, mid-stroke and Run/Reset fail | post-merge/mutation-R4-lag-plus-one.log |
| R8 | Soft contacts (crush) | Only the fact-14 penetration assertion fails | post-merge/mutation-R8-soft-contacts-crush.log |

The guards still discriminate after C-Bumper+Spring and OR.

### Delta review (staged against `80ffc685`)

The delta has 32 files, all under `_bmad-output/implementation-artifacts/`. There are 0 product, test or doc-source files.

- **Spec "Post-merge" section:** its claims match the reviewer's independent results above.
- **`evidence-9-4c/post-merge/`:** the implementer's logs.
- **C-Bumper+Spring terminal receipt:**
  - The `review-c-bumper-spring.md` hunk is byte-identical to `carry/cbs-terminal-receipt.patch`.
  - The `sprint-status.yaml` hunk is identical except for the patch's `index` line (base blob `5e240221` here, `89e89a5b` at the source), because this file also carries the 9.4c `review` line. `snapshot/cbs-receipt-worktree.patch` holds the comparison copy.
  - `evidence-c-bumper-spring-review/terminal/` matches `carry/cbs-terminal-evidence` (`diff -r` identical, 12 files).
  - Only byte identity was checked, as instructed; the receipt's substance belongs to its own reviewer.
- **This reviewer's records:** `evidence-9-4c-review/post-merge/`, `snapshot/` and this section.

### Snapshot identity

- Candidate staged tree before this section was appended: `c26e5c399116e93f6c264d56009481d4fa283955`.
- `git diff --cached --binary 80ffc685 | shasum -a 256` gives `c5af1bc5f47c907cef2d410734712b6056bacaa59707271bd6f474389d277270`.
- The final staged tree is that candidate plus only this appended section of this record. The coordinator's report gives its id.

**Authorization:** the implementer may commit exactly the final staged tree on top of `80ffc685` (normal hooks, no `--no-verify`) and push it to main. Any other change invalidates this approval.

### Pre-push condition

- OR's CI for `a2eddf81` must succeed. At approval it was "Build and deploy game", run 39722366290, `in_progress`.
- `origin/main` was `a2eddf81`. If main moves again, merge it and re-review.

### Publication-dependent checks (the reviewer verifies these after push, before the terminal Pass)

1. `git ls-remote origin main` equals the pushed commit. Its tree equals the approved final tree, and its parents are `80ffc685`.
2. "Build and deploy game" for that exact SHA succeeds: the build job's native and tool totals match the counts above, and AppBundle is produced only in CI.
3. A `github-pages` artifact and a successful deployment exist for that SHA.
4. Browser qualification stays pending (post-catalogue E2E: `cat-005-n19`, `cat-051-interrupt`). This approval is not browser or release qualification.

## Terminal publication verification — 22 October 2026

**Verdict: terminal scoped Pass** (native and publication). Browser qualification remains pending until post-catalogue E2E. This is not release qualification.

Raw outputs: [evidence-9-4c-review/terminal/](evidence-9-4c-review/terminal/).

| Check | Evidence | Result |
|---|---|---|
| Identities | `trees.txt` | `git ls-remote origin main` = `a598a09e2b91d58135b3343a2beeea6b88578013`, a `--no-ff` merge with parents `a2eddf81` and `421dd5325c3ca35156b7531acc1bfd48d42bd86e`. The slice commit `421dd532` has parent `80ffc685`. Both have tree `690739d1e603e7543e346a1411a5d2253f0796a8`, which equals the approved final staged tree |
| Tree equivalence | `diff-tree-approved-vs-slice.txt`, `diff-tree-slice-vs-merge.txt` | The approved candidate tree `c26e5c39` and the published tree differ only in `review-9-4c-latch-fixture-deferred-integrations.md` (the authorized SnapshotApproval append). The slice tree and the merge tree are identical (0 entries) |
| Fixture in tree | `latch-tests-listed.txt` | At `421dd532`, `LatchSupplyFixtureTests.cs` sha256 is `f9b1ec7f…ee206f` and the csproj includes it. `--list-tests` on the matching build lists all 10 cases |
| Hooks | `hook-rerun.log` | With the index tree exactly `690739d1`, `.githooks/pre-commit` re-ran: staged-snapshot gate 124/124, exit 0. The coordinator reports the hook passed on the slice commit; no hook runs on a clean merge |
| Exact-SHA CI | `run.json`, `jobs.json`, `jobs-summary.txt`, `build-job.log`, `build-job-totals.txt` | Run 39723690498 "Build and deploy game", push on main, headSha `a598a09e`, conclusion success. Build job 119642688609 succeeded at every step: static gate, Test C# game, Test C# tools, Publish, Check static deployment and upload-pages-artifact. Totals from the log: CuriousContraptions.tests 2362/2362; tools Coverage 77, BundleElements 148, WorkerAotGuard 17, E2eRunner 174; 0 failed. The CI log prints only per-assembly totals, so the 10 fixture cases are counted inside the 2362 and named in `latch-tests-listed.txt` |
| Pages artifact | `artifacts.json`, `deploy-job.log`, `deploy-job-excerpt.txt` | Artifact 12402396952 `github-pages`, 79459987 bytes, digest `sha256:518372be1400569fefc746d27c806809fd4e015a9183cdf35bee8abc4d79864c`, workflow run 39723690498, head_sha `a598a09e`, not expired. The deploy job 119646642563 fetched `artifact_id` 12402396952 and reported "Reported success!" |
| Deployment | `deployments.txt`, `deployment-statuses.txt` | Deployment 7244087300, environment `github-pages`, sha `a598a09e`. Status history: waiting, queued, in_progress, then success (2026-10-22T04:55:17Z, https://aidanmorgan.github.io/tim/) |

`sprint-status.yaml` is set to `done # scoped native/publication Pass a598a09e; browser pending` in this worktree (uncommitted, as the coordinator instructed).
