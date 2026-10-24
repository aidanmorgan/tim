---
title: "Independent adversarial review: CI no-skip, explicit browser deferral list"
spec: spec-ci-no-skip.md
baseline: 54e19147 (origin/main at review time)
reviewer: independent adversarial reviewer (did not implement; edited no source, test, tool or workflow file)
verdict: Pass (pre-publication); terminal publication checks pending
---

# Scope and rules applied

AGENTS.md at 54e19147 sets the rules this review applies:
- **No skipped tests.** It is scoped to the game system: the C# solution test projects and the node harnesses for game runtime code. Browser/E2E suites, vendored BMAD tests and Anvil are outside the system proof.
- **The browser exception.** Disabled tests must be explicit, kept on one list, and carry the re-enable gate and owner.
- **Tests assert behaviour, not hashes.**

Coordinator rulings accepted as given:
- The 15 `ToolchainMissing` E2eRunner methods are toolchain tests.
- The pre-commit hook's static-gate selection is a scoped local check.
- The owner's directive pins CI Node at the latest stable release, 26.12.1.

The review ran in two rounds:
1. **The first candidate.** I paused it at the owner's rescope. Its partial `full-slnx-test.log` is not evidence and has been deleted.
2. **The revised candidate.** All results below come from this round. During it, the Node pin was changed from 20.19.5 to 26.12.1 (`pages.yml` only, 08:26). Every check below that reads the workflow ran after that change.

# Criterion / impact matrix

| # | Criterion | Result | Evidence |
|---|---|---|---|
| 1 | The deferral list is the single source. CI's only filter is generated from it by `deferred-filter`, and no hand-written filter remains. | Pass | Diff: the `toolchain=` list is deleted, and the step uses `deferred=$(dotnet run … deferred-filter)`. [macos-ci-step-node26.log](evidence-ci-no-skip-review/macos-ci-step-node26.log) prints the 15 generated terms. |
| 1a | Mutation: a hand-written `FullyQualifiedName` filter is added to the workflow. | Caught. `CiFilterIsGeneratedFromTheListAndExcludesExactlyItsMethods` fails (expected 2 `--filter` splits, got 3). | [mutations.log](evidence-ci-no-skip-review/mutations.log) M3 |
| 1b | Mutation: an unlisted test file is added outside `tools/` (`CuriousContraptions.web/wwwroot/zz-review.test.js`), and an unlisted `test_zz.py` is added. | Both caught by `EveryOtherTestFileOfTheSystemRunsOrIsDeferred`. | mutations.log M1, M2 |
| 1c | Mutation: the ChromeSuites entry is removed from the list. | Caught. `EveryOtherTestFileOfTheSystemRunsOrIsDeferred` fails and names the `tools/e2e/*.test.ts` suites. The file was reverted (SHA256 identical before and after) and rebuilt, and the CI step was re-run after the rebuild: exit 0, same counts. Removing an E2eRunner or NodeTest entry re-enables that test by design, through the generated filter or a NodeHarnessTests row. The re-enabled test then runs and fails loudly, so it is never silently filtered. | [mutation-remove-entry.log](evidence-ci-no-skip-review/mutation-remove-entry.log), [macos-ci-step-node26-after-m6.log](evidence-ci-no-skip-review/macos-ci-step-node26-after-m6.log) |
| 2 | NodeHarnessTests runs every game runtime harness, and fails on a skipped, todo, cancelled or failed test, or on a file with no tests. | Pass | M4: an emptied harness fails with `NoTests`. M5: `{ skip: true }` fails with `SkippedTests`. The implementer's earlier todo control also failed. |
| 2a | Earlier finding 4: a file reporting zero tests passed. | Fixed. The waiver is removed. The two script-style harnesses are wrapped in node:test, with identical assertion counts (7/7 and 68/68) and a whitespace-only diff apart from the wrappers. | `git diff -w` reviewed |
| 2b | Earlier finding 5: Node 20 cancelled 9 clock-ring tests. | Fixed in the harness, so Node was not to blame. A pending `Atomics.waitAsync` does not keep a Node process alive. A ref'd keep-alive cleared in `after` fixes it, and worker `error`/`messageerror` handlers turn a silent cancellation into `not ok`. The implementer's control shows `ERR_MODULE_NOT_FOUND`. The game code and assertions are unchanged. | spec; evidence-ci-no-skip/negative-control-worker-error.log, harnesses-node-versions.log |
| 2c | The two newly deferred harnesses really are browser-suite support. | Pass. `tools/workshop-close-findings.mjs` is imported only by `tools/e2e/workshop-driver.ts` and a runner fixture. `tools/workshop-console.mjs` is imported only by `tools/e2e/*.test.ts`. Neither is in `CuriousContraptions.web/wwwroot` or the workers. All 9 remaining harnesses exercise wwwroot/worker runtime code. | `git grep` importer census |
| 3 | Earlier finding 3: `Processes.Run` had no timeout. | Fixed. A wall limit (10 minutes by default, 5 minutes for harnesses) kills the whole process tree and raises the typed `ProcessTimedOut`. `ProcessesTests` covers exit code and outputs, and that the grandchild is gone. | full run; control run in mutations.log |
| 4 | Repository-wide scan for skips and exclusions. | Pass | `rg` with explicit paths. The only skip markers are the vendored BMAD retrospective `skipif`s, outside the system proof. The only other filter is the pre-commit scoped selection (ruled). All 5 test projects run in CI. `OutsideTheSystemProof` names `.agents/`, `.claude/`, `_bmad/`, `tools/anvil/`, `_bmad-output/` and the runner Fixtures, each with a reason. |
| 5 | CI workflow correctness on Ubuntu. | Pass | `setup-node@v4` pins exactly 26.12.1, before both test steps. The Python, uv and pytest steps are gone. The step runs under bash `-e`, so a generator failure aborts it. The generator prints the filter only, and BrowserDeferralTests runs it and asserts exact stdout, so a banner would fail. There is no `launchSettings.json`. The only network work is the existing installs plus the Node download. |
| 6 | Local full run reports Skipped 0 in every project. | Pass | [full-slnx-test-r2.log](evidence-ci-no-skip-review/full-slnx-test-r2.log): WorkerAotGuard 17, BundleElements 191, Coverage 77, E2eRunner 207 and CuriousContraptions.tests 2928 passed, all with Skipped 0. Exit 0 in 8 m 37 s. |
| 7 | Exact CI tools step under bash on Node 26.12.1. | Pass. Step exit 0. Coverage 77, BundleElements 191, WorkerAotGuard 17 and E2eRunner 191 (= 207 − 16 deferred cases), all with Skipped 0. | macos-ci-step-node26.log. The step was extracted verbatim from the YAML after the pin change. |
| 7a | The game runtime harnesses on Linux Node 26.12.1. | Pass. All 9 files exit 0, with 0 fail, cancelled, skipped or todo, run as non-root over a read-only mount. | [linux-node26-harnesses.log](evidence-ci-no-skip-review/linux-node26-harnesses.log) |
| 8 | `git diff --check` | Clean, and the new untracked sources have no trailing whitespace. | [diff-check.log](evidence-ci-no-skip-review/diff-check.log) |
| 9 | No new output-hash assertions. | Pass. BrowserDeferralTests uses term and string equality, with no digests. | inspection |

**Build.** One Debug build of the solution: 0 warnings, 0 errors ([build-r2.log](evidence-ci-no-skip-review/build-r2.log)). After the build, only `pages.yml` changed. It is not a compile input; it is read at run time, and the CI step and control runs above re-read it after the change.

# Findings

No blocking findings.

Non-blocking (Low):
- **L1.** `Processes.Run`'s "exited but its output stayed open" path raises `ProcessTimedOut` without killing the descendant that holds the pipes. The parent is gone by then, so the tree kill no longer reaches it. A leak risk only.
- **L2.** The coverage scan recognises test files by name (`*.test.{mjs,ts,js}` and `test_*.py`). A new C# test project added to the solution but not to the CI "Test C#" steps would not be detected. This gap predates the slice.
- **L3.** The deferral reason for `workshop-close-findings` also notes that the harness fails on Node 22.23.4. It is legitimately deferred as browser-suite support, so this needs a look at re-enable time, not now.
- **L4.** I did not rerun the full Linux CI tools step myself. My Linux evidence is the harnesses on Node 26.12.1 (7a). The implementer reran the step in Docker on Node 26.12.1 after the pin change (`evidence-ci-no-skip/docker-ci-step.log`, 08:28:54): exit 0, E2eRunner 191. Two items remain for the first real CI run:
  - "Test C# game" cannot be reproduced on arm64, because of the Godot import.
  - `ClockRingScriptTests` and `EventRingProducerTests` both start Node in that step, but the workflow comment names only `EventRingProducerTests`.

# Verdict

**Pass** for every pre-publication criterion of this candidate, within the scope of this slice. This is not global, browser or release qualification. Browser qualification stays pending until the post-catalogue browser phase.

# SnapshotApproval

The exact candidate is approved for a normal commit, a non-force push to main, and deployment.

| Item | Value |
|---|---|
| Base | HEAD = origin/main = `54e19147525d40b815f2c1243f92fa9cb2e28c7b`, checked immediately before staging |
| Staged paths | 31: 9 modified sources and docs, 4 new sources, the spec, 9 implementer evidence logs, 8 reviewer evidence logs |
| `git write-tree` | `114e9710e1e7510f2b7f23665e111927ba21df69` |
| SHA256 of `git diff --cached --binary HEAD` | `558d13bc5dcb07d6322413da4fdc71fe5c958078ee7af4fd3151bbcf2510ae2d` |

**What the identities cover.**
- They exclude this review record. It is staged afterwards and is not self-hashed.
- The committed tree must equal the tree above plus this record only.
- Any other change to a staged path invalidates this approval.

# Publication-dependent checks

These are required for terminal Pass. Artifact upload and deploy run only on `refs/heads/main`, so a branch run cannot satisfy them.

1. **Commit identity.** The pushed commit's parent is `54e19147`. Its tree minus `review-ci-no-skip.md` equals `114e9710…`, and origin/main equals the pushed commit.
2. **The first real CI run of that commit on main passes every step**, including the new ones:
   - setup-node resolves exactly v26.12.1.
   - "C# static gate" passes.
   - "Test C# game" passes on Linux x64 with Skipped 0. It includes the Node-starting `EventRingProducerTests` and `ClockRingScriptTests`, which cannot be reproduced locally.
   - "Test C# tools" passes:
     - Coverage 77, BundleElements 191 and WorkerAotGuard 17.
     - E2eRunner 191, with the 15-term generated filter.
     - The 9 NodeHarnessTests rows pass.
     - Skipped 0 in every project.
   - The production publish and the static deployment check pass.
3. **Pages.** `upload-pages-artifact` uploads the bundle, and `deploy-pages` succeeds for that run. The served Pages origin loads the new build: its `index.html` and `_framework/dotnet.js` are served from that deployment.

Until these checks are verified, the terminal verdict stays Incomplete.

# Addendum, 2026-10-24: terminal publication result (append-only)

This was recorded by the independent follow-up reviewer of the forward-fix slice "CI harness determinism". The text above is unchanged. Raw outputs are in the determinism slice's `_bmad-output/implementation-artifacts/evidence-ci-harness-determinism/terminal/`, in its worktree and on main after 9536e2ff.

## 1. Commit identity: Pass

| Item | Value |
|---|---|
| Merge commit | `7143a874d8e334911dd0dfaf38f5db788c405d1b` |
| Parents | `54e19147` (approved base) and `87d27e12` (slice commit) |
| Tree | `6970535d…` |

`git diff --name-only 114e9710 7143a874^{tree}` lists only `review-ci-no-skip.md`. Raw output: `no-skip-identity.txt`.

## 2. First CI run on main: failed, fixed forward

Run [40045841714](https://github.com/aidanmorgan/tim/actions/runs/40045841714), head `7143a874`, concluded `failure`:
- the **Test C# tools** step failed;
- publish, static check and Pages upload were skipped;
- deploy was skipped.

The cause was two game-runtime node harnesses whose assertions depended on wall-clock and thread scheduling:
- `workshop-client-lifecycle` test 15;
- `workshop-observation` test 1.

This slice newly ran them in CI. That is the no-skip rule working as intended, not a defect in this slice's own change. Raw output: `failed-run-40045841714-jobs.json`, and the determinism slice's `ci-run-40045841714-failures.log`.

**Forward fix.** "CI harness determinism", commit `4f2ad2af2b4a98c91ccfffac4c6fde051e02780d`, independently reviewed in `review-ci-harness-determinism.md`.
- It was merged `--no-ff` as `9536e2ff70bfc0fe157a27bf1f7436727ea05dd0`, with parents `860c3cb1` and `4f2ad2af`, and tree `12dd70ded8f4894c758b0131a13fddb283f23e8c`.
- Between `7143a874` and `9536e2ff`, the only non-documentation files that changed are the two harness files (`git diff --name-only 7143a874 9536e2ff`). This slice's workflow, `E2eRunner` deferral list and test projects are unchanged.

## 3. Green run: Pass

Run [40052056113](https://github.com/aidanmorgan/tim/actions/runs/40052056113), head `9536e2ff`, concluded `success`, with every step of both jobs successful:
- setup-node resolved `node: v26.12.1`.
- C# static gate passed.
- **Test C# game**: CuriousContraptions.tests 2928/2928, Skipped 0.
- **Test C# tools**, Skipped 0 in every project:
  - Coverage 77/77;
  - BundleElements 191/191;
  - WorkerAotGuard 17/17;
  - E2eRunner 191/191 under the generated deferral filter, the same 191 that I reproduced locally.
- Publish and the static deployment check passed.

Raw output: `run.json` and `run.log`.

## 4. Pages: Pass

| Item | Value |
|---|---|
| Artifact | `github-pages`, id 12544457446, head `9536e2ff`, digest `sha256:9b88d4ef…` |
| Deployment | 7292816973, sha `9536e2ff`, state `success`, at https://aidanmorgan.github.io/tim/ |

It is the latest `github-pages` deployment. `index.html`, `404.html`, `clock-ring.js`, `native-clock.js`, `godot.wasm`, `godot.pck` and `_framework/dotnet.js` are served with HTTP 200, and each is byte-equal (SHA-256) to the same file in the downloaded artifact. Raw output: `served-vs-artifact.txt`.

These are the slice's first exact-SHA green CI and deployment. They run on the forward-fix merge, which contains this slice unchanged.

## Terminal verdict

**Pass.** This covers the slice scope and publication only. It is not global, browser or release qualification. Browser qualification stays pending until the post-catalogue browser phase.
