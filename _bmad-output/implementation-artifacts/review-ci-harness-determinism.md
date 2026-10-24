---
title: "Independent follow-up review: CI harness determinism"
slice: spec-ci-harness-determinism.md
candidate: 4f2ad2af2b4a98c91ccfffac4c6fde051e02780d (tree 2dbf18f5e043f5959a2b4a086c03b3518bba1915)
base: e344b19c2dee4e3c139a7730de43fd40b39d4dff
reviewer: independent follow-up reviewer (did not author the slice; changed no test or source file)
date: 2026-10-24
verdict: Terminal Pass (scoped). SnapshotApproval was granted for 4f2ad2af (tree 2dbf18f5). Publication checks 1-4 were verified at merge 9536e2ff.
---

# Scope

This is the follow-up review the spec's Auto Run Result asked for. It covers:
- the re-derived design of observation test 1;
- the two medium patch groups: the lifecycle reply waits, and the `manualTurns` caps with due-time ordering;
- the new slow-master control;
- the no-game-code claim;
- the mutation evidence.

Environment: Node v26.12.1, Darwin arm64, worktree clean at 4f2ad2af.

My raw outputs are in [evidence-ci-harness-determinism/review/](evidence-ci-harness-determinism/review/). I made the mutations only in a scratch extract of the tip (`git archive 4f2ad2af`), never in the worktree.

# Criterion and impact matrix

| # | Criterion | Expected (derived) | Actual | Result |
|---|---|---|---|---|
| 1 | Observation test 1 does not depend on thread scheduling | No second thread answers the ring, so the outcome is fixed by program order | See note 1a | Pass |
| 1b | The stand-in exercises the shipped code, not a reimplementation | The shipped `clock-ring.js` and `worker.js` paths run | See note 1b | Pass |
| 1c | The slow-master control proves counted misses and a session that continues | See note 1c | Mutations M3 and R1 kill it | Pass |
| 1d | Shared-budget assertions | 16 startup waits at 2 ms, then the first running exchange at 2 ms | See note 1d | Pass |
| 2 | Lifecycle `replyWaitsSettled` / `assertOneAnsweredWait` are not vacuous | See note 2 | R2 and M5 | Pass |
| 3 | `manualTurns` caps and due-time ordering | See note 3 | Matches | Pass |
| 4 | No game code changed | Diff outside `tools/` and `_bmad-output/` is empty | See note 4 | Pass |
| 5 | Mutations M1–M5 recorded, two re-run at the tip | See note 5 | M4 and M3 reproduce | Pass |
| 6 | Affected tests and the full suite | All pass, nothing skipped | See note 6 | Pass |
| 7 | No coverage lost by dropping the real master thread | See note 7 | See note 7 | Pass |

## Notes

**1a. Scheduling independence.**

The context's `Atomics` is `Object.create(Atomics, { wait })`. Before the real `Atomics.wait` runs, the overridden `wait` serves every request already posted on the session ring with the shipped `serveRequests`.

`deriveExchange` posts Requested and rings the doorbell before it waits. So the real wait sees Replied and returns `'not-equal'`, which is a valid production path (a master that answered before the wait). The slow path is also deterministic: it is a real 2 ms timeout with no other answerer, then `'timed-out'`.

Wall-clock time still paces when checks run (the real 100 ms `setInterval`, and `until` polling). It never decides an asserted value.

**1b. Shipped code paths.**

- `worker.js` is loaded unchanged as a `SourceTextModule`.
- `clock-ring.js` is now evaluated in the worker's context from its shipped source (it was previously a SyntheticModule re-export), so its `deriveExchange`, `qualifyDerivation`, `writeDerivation` and `beat` run as shipped.
- The master side calls the shipped `serveRequests`.
- Only `clock-master.js`'s bootstrap handshake is mimicked. The old stand-in thread also bypassed `clock-master.js`.

**1c. Slow-master control.**

Expected: `missed > 0`, `checks == 0`, no failure, `beats >= missed`. After a late `serve()`, a check completes, the beats advance, and there is still no failure.

- M3 (a miss throws instead of being counted) fails it at "counted as missed".
- My R1 (a miss stops further checks) also fails it.

**1d. Shared-budget arithmetic.**

The retry gets `2 − (read₄ − read₁)`. `read₁` is `started`, and the 3 steps that follow are `before`, `after` and the `remaining` read itself. So the retry gets `2 − 0.75 = 1.25`. That is exact in binary, and `ExchangeTimeoutMilliseconds = 2` matches `engine/gpu/WorkshopClockDerivation.cs:44`.

M4 fails tests 1 and 2 with `+2 −1.25`.

**2. Lifecycle reply waits.**

The wrapper returns the same promise the client attaches `.then` to. Promise reactions run in registration order, so the client's handler runs before `allSettled` resolves.

Expected: exactly one wait, fulfilled, non-null.

- R2 (remove `serveRing()` in test 16) fails after 255 ms with "after disposal: the master answered it". This proves the non-null assertion separates the master's answer from the 250 ms release.
- M5 fails tests 15 and 16.

**3. `manualTurns`.**

`worker.js` uses `MessageChannel` and `setTimeout` only for scheduler turns (lines 10 and 253).

- `pump` and `quiesce` are each capped at 1000 steps, with labelled assertions.
- Timers fire in (due, id) order on a virtual clock.
- A closed port drops deliveries.

M1 and M2 fail tests 9 and 10.

**4. No game code changed.**

I ran `git diff e344b19c..4f2ad2af --stat -- . ':(exclude)tools' ':(exclude)_bmad-output' ':(exclude)**/deferred-work.md'`. It printed nothing, with rc 0. (The spec's `':!_bmad-output'` form errors under zsh.)

**5. Mutations M1–M5.**

All five are in [mutations.log](evidence-ci-harness-determinism/mutations.log). I re-ran two at the tip:
- M4: tests 1 and 2 fail, `2` vs `1.25` ([m4.tap](evidence-ci-harness-determinism/review/m4.tap));
- M3: test 3 fails ([m3.tap](evidence-ci-harness-determinism/review/m3.tap)).

My own extra controls: [r1.tap](evidence-ci-harness-determinism/review/r1.tap) and [r2.tap](evidence-ci-harness-determinism/review/r2.tap).

**6. Test runs.**

| Run | Result |
|---|---|
| Debug build | 0 warnings |
| `NodeHarnessTests` and `BrowserDeferralTests` | 14/14, Skipped 0 |
| Direct TAP, observation harness | 11/11 |
| Direct TAP, lifecycle harness | 39/39 |
| Full slnx, `--no-build --no-restore` | 2928 + 207 + 191 + 77 + 17 passed, 0 failed, 0 skipped, 8 m 14 s |

**7. Coverage of the real master thread.**

The real cross-thread `'ok'` wake and the master's `awaitRequest` doorbell loop are still exercised by real worker threads in `tools/workshop-clock-ring.test.mjs`:
- lines 79–87 and 125–142, `deriveExchange`;
- lines 252–263, `qualifyDerivation` with a stalled master.

# Commands

All run from the worktree root.

```
dotnet build CuriousContraptions.slnx -c Debug                                   # build.log
dotnet test tools/E2eRunner.Tests -c Debug --no-build --no-restore \
  --filter "FullyQualifiedName~NodeHarnessTests|FullyQualifiedName~BrowserDeferralTests"  # node-harness.log
node --experimental-vm-modules --test --test-reporter=tap tools/workshop-observation.test.mjs     # obs-tip.tap
node --experimental-vm-modules --test --test-reporter=tap tools/workshop-client-lifecycle.test.mjs # lc-tip.tap
dotnet test CuriousContraptions.slnx -c Debug --no-build --no-restore            # full-slnx.log (no --blame-crash)
```

The mutation runs used the same `node` command in the scratch extract, after an unmutated control run passed 11/11 and 39/39.

# Findings

None are blocking.

- **L1 (low): stale evidence file.** [mutation-absence-checks.log](evidence-ci-harness-determinism/mutation-absence-checks.log) comes from an earlier candidate: 10 tests, the old test numbering, and a duplicated `not ok 9`. The spec's evidence table does not list it. Label it diagnosis-only or remove it in a later evidence edit. It does not affect acceptance, because mutations.log and my tip re-runs cover the same mutations.
- **L2 (low): evidence identity.** `mutations.log` and `node-harness-tests.log` name "e344b19c plus uncommitted changes", not 4f2ad2af. My re-runs at the tip establish the tip identity for M3, M4 and the suites.
- **L3 (low): wording.** Test 1's comment lists the three reads as "its start, then the first exchange's before and after". The three steps elapsed are actually `before`, `after` and the `remaining` read. The arithmetic and the assertion are correct.
- **L4 (low, covered): wall-clock pacing.** The new slow-master test is paced by the real check interval and `until` polling, but its outcome does not depend on that time. The residual is already owned in deferred-work.md (observation `until`, owned by the TypeScript conversion slice), so it is not a new violation.
- **L5 (informational): latent stand-in limit.** The stand-in master's `serve()` passes empty peers. It would throw if a Browser or Animation probe were ever posted on that ring. No current test does this.

# Verdict

| | |
|---|---|
| SnapshotApproval | Granted for 4f2ad2af, tree 2dbf18f5 |
| Pre-publication criteria | All pass (rows 1–7). There are no unresolved unintended regressions in the required-now scope. Findings L1–L5 are non-blocking. |
| Implementation verdict | Pending. It becomes Pass when publication-dependent checks 1–4 below are verified. |

Browser and release qualification are not claimed. The fix's first real ubuntu x64 CI run (check 2) is its real acceptance.

# SnapshotApproval

| | |
|---|---|
| Candidate | commit 4f2ad2af2b4a98c91ccfffac4c6fde051e02780d, tree 2dbf18f5e043f5959a2b4a086c03b3518bba1915 |
| Target | `--no-ff` merge into current main, origin/main 860c3cb15f1635821ec8d09f2c68ad88e4c2e550 |

**Main has moved, with no overlap.** `git log e344b19c..origin/main` lists d94e9208 and 860c3cb1. They touch `TODO.md`, `_bmad-output/planning-artifacts/epics.md` and `docs/testing-guidance.md`, and none of those files is in this slice (`comm -12` of the two file lists is empty). e344b19c is an ancestor of origin/main. `docs/testing-guidance.md` gains the "Stress and randomised tests" section, which the spec already follows: repeat-N logs are labelled diagnosis only.

**Expected merge tree.** `git merge-tree --write-tree origin/main 4f2ad2af` gives **12dd70ded8f4894c758b0131a13fddb283f23e8c**:
- `git diff --name-only origin/main 12dd70de` lists exactly the 11 slice files;
- `git diff --name-only 4f2ad2af 12dd70de` lists exactly main's 3 files.

**Pre-publication criteria verified:** rows 1–7 above.

**Approved content.** The approval covers the 11 slice paths at 4f2ad2af. This review record and `evidence-ci-harness-determinism/review/**` are review-only. They are excluded from the candidate and are not inputs to its approval.

If main moves again before the merge, or the merge tree differs from 12dd70de in anything other than those review-only paths, this approval no longer applies to the result; re-check the overlap.

## Publication-dependent checks

These are pending, for the reviewer after the merge and push.

1. `git diff --name-only 12dd70ded8f4894c758b0131a13fddb283f23e8c <merge>^{tree}` lists only the review-only paths, or nothing if they are committed separately. Its parents are origin/main 860c3cb1 (or the main it was verified against) and 4f2ad2af.
2. Exact-SHA GitHub Actions CI on the merge commit is green on every step, explicitly including **Test C# tools** on the ubuntu x64 runner. That run is the first real-runner acceptance of this fix (the spec's residual risk). A failure in `workshop-client-lifecycle` test 15, or in observation tests 1, 3, 9 or 10, reopens this slice.
3. The Pages artifact was built from that exact SHA.
4. The deployment is complete, and the deployed identity matches that SHA.

Also record the review-only evidence (this file and `evidence-ci-harness-determinism/review/`), which is uncommitted in the worktree. Committing it is the implementer's or coordinator's call; I committed nothing.

# Terminal publication checks (2026-10-24)

Raw outputs are in [evidence-ci-harness-determinism/terminal/](evidence-ci-harness-determinism/terminal/).

| # | Check | Actual | Result |
|---|---|---|---|
| 1 | Tree equivalence | See note 1 | Pass |
| 2 | Exact-SHA CI | See note 2 | Pass |
| 3 | Pages artifact | See note 3 | Pass |
| 4 | Deployment and serving | See note 4 | Pass |

**1. Tree equivalence.**
- Merge `9536e2ff70bfc0fe157a27bf1f7436727ea05dd0` has tree `12dd70ded8f4894c758b0131a13fddb283f23e8c`, which equals the expected merge tree.
- `git diff --name-only 12dd70de 9536e2ff^{tree}` is empty.
- Its parents are `860c3cb1` and `4f2ad2af`.
- origin/main is `9536e2ff`.

The review-only paths were not committed. Raw output: `tree.txt`.

**2. Exact-SHA CI.** Run [40052056113](https://github.com/aidanmorgan/tim/actions/runs/40052056113), workflow "Build and deploy game", head `9536e2ff`, concluded `success`. Every step of the build and deploy jobs succeeded.
- setup-node resolved `node: v26.12.1`, linux-x64.
- C# static gate passed.
- **Test C# game**: 2928/2928, Skipped 0.
- **Test C# tools**, Skipped 0 in every project:
  - Coverage 77/77;
  - BundleElements 191/191;
  - WorkerAotGuard 17/17;
  - E2eRunner 191/191 under the generated deferral filter.

The E2eRunner count matches the local filtered run of 191/191 (`local-filtered-e2erunner.log`). The 15-term filter removes 16 test cases (207 unfiltered, 191 filtered), all of them listed browser-toolchain tests (`deferred-filter.txt`). `NodeHarnessTests` stays in the run.

This is the first real ubuntu x64 run of the fixed harnesses. Lifecycle test 15 and observation test 1 passed. Raw output: `run.json` and `run.log`.

**3. Pages artifact.**
- Artifact `github-pages`, id 12544457446, 80,271,764 bytes, not expired.
- Its `workflow_run.head_sha` is `9536e2ff`, and its digest is `sha256:9b88d4efe14076bcffba6dc7473a4431bd25d8252d81386f75b897397239adae`.
- The downloaded `artifact.tar` has SHA-256 `0b22d0f1…ec3b`.

Raw output: `artifacts.json`.

**4. Deployment and serving.**
- Deployment 7292816973 is environment `github-pages`, sha `9536e2ff`, ref main.
- Its statuses run waiting, queued, in_progress, then `success` at 02:22:38Z, with URL https://aidanmorgan.github.io/tim/.
- It is the latest `github-pages` deployment.
- These files are served with HTTP 200, and each is byte-equal (SHA-256) to the same file in the downloaded artifact: `index.html`, `404.html`, `clock-ring.js`, `native-clock.js`, `godot.wasm`, `godot.pck` and `_framework/dotnet.js`.
- The served `clock-ring.js` also equals `9536e2ff:CuriousContraptions.web/wwwroot/clock-ring.js`.

Raw output: `deployments.json`, `deployment-7292816973-statuses.json`, `latest-deployments.json`, `served-vs-artifact.txt` and `index-headers.txt`.

## Terminal verdict

**Pass**, within the slice scope and publication only. This is not global, browser or release qualification. Browser qualification stays pending until the post-catalogue browser phase. Low findings L1–L5 remain recorded and are non-blocking.

This run also closes the earlier "CI no-skip" slice. Its first run on main, [40045841714](https://github.com/aidanmorgan/tim/actions/runs/40045841714) at `7143a874`, failed at Test C# tools and was fixed forward by this slice. I recorded an append-only addendum in that slice's `review-ci-no-skip.md`.
