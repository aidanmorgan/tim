---
title: "CI no-skip: explicit browser deferral list; every game-system test in a required run"
status: in-review
baseline: 54e19147
---

# Outcome

The slice applies three owner rules in AGENTS.md, as of main 54e19147:
- no skipped tests;
- the browser exception;
- tests assert behaviour, not hashes.

The scope rule limits them to the game system's tests: the C# solution's test projects and the node harnesses for game runtime code.

What the slice does:

- **One deferral list.** Browser/E2E tests and their toolchain tests are disabled only through `tools/E2eRunner/BrowserDeferral.cs`. Each entry names the test, its reason, the re-enable gate (`ReEnableGate.PostCatalogueBrowserPhase`) and the owner.
- **One generated CI filter.** The hand-written `FullyQualifiedName` list in `.github/workflows/pages.yml` is deleted. The workflow's only filter comes from `dotnet run --project tools/E2eRunner --no-build -c Debug -- deferred-filter`.
- **List and filter are checked.** `BrowserDeferralTests` asserts that the list and the filter are equal: term equality, no hashes.
- **The game runtime harnesses run.** These are the 9 `tools/*.test.mjs` files for game runtime code: workers, rings, client lifecycle, isolation and save storage. They were in no required run. `NodeHarnessTests` now runs them in `tools/E2eRunner.Tests`, under a 5-minute wall limit. Each must declare at least one test and have none failed, cancelled, skipped or todo.
- **Only Node is provisioned.** CI provisions no browser toolchain and no Python. It pins Node at exactly 26.12.1 with `actions/setup-node@v4`, for EventRingProducerTests and NodeHarnessTests. This follows an owner directive: use the latest stable release, which is 26.12.1 (21 October 2026). The latest LTS is 24.22.1.

Out of scope, per the owner's scope rule and a coordinator decision:
- browser/E2E suites;
- vendored BMAD tooling and its tests;
- Anvil (`tools/anvil`, including VerifyPolicy and its fixtures).

`BrowserDeferralTests.OutsideTheSystemProof` names these areas: `.agents/`, `.claude/`, `_bmad/`, `tools/anvil/`, plus `_bmad-output/` (evidence) and `tools/E2eRunner.Tests/Fixtures/` (runner inputs).

# Classification

## The 15 formerly filtered E2eRunner.Tests methods

[Evidence](evidence-ci-no-skip/classify-empty-home.log): `dotnet test tools/E2eRunner.Tests` with an empty HOME (no npx cache).

- Result: 174 passed, 16 failed. The 16 failures are the 15 methods; one Theory has 2 rows.
- Every failure is `ToolchainMissing`: 13 on tsx 4.23.15 and 3 on TypeScript 6.0.3.
- None launches Chrome.

All 15 test the Chrome suites' toolchain, so they are deferred:

| Methods | Count | Need |
|---|---|---|
| TypeCheckTests | 2 | tsc |
| SuiteProcessTests | 8 of 12 | the suites' tsx loader |
| IdentityTests | 5 of 9 | the runner engine on the tsx loader |

Provisioning tsx alone may not be enough. The C-Ramp record found 6 failures on Linux even with the npx cache.

## The node test files

| File | Disposition |
|---|---|
| 9 harnesses of game runtime code (`CuriousContraptions.web/wwwroot`, Simulation and Animation workers) | Run by `NodeHarnessTests` |
| `tools/workshop-close-findings.test.mjs` | Deferred. It tests the Chrome suites' close-finding module, through node:test's own hook reporting. |
| `tools/workshop-console.test.mjs` | Deferred. It tests the Chrome suites' console classifier. |
| `tools/e2e/unit/run-reset-snapshot.test.ts` | Deferred. It needs tsx and flatbuffers. |
| `tools/e2e/*.test.ts` | Deferred. These are the Chrome suites. |

Node 22.23.4 fails one close-findings test, because a suite after-hook error does not fail the file. That is a node:test reporting difference in a browser-toolchain test.

# Node 20 and the clock-ring harness (reviewer finding)

**Symptom.** Under Node 20.19.5, 9 of 12 clock-ring tests reported "cancelled": "Promise resolution is still pending but the event loop has already resolved" ([log before the fix](evidence-ci-no-skip/node20-clock-ring-control.log)).

**Cause.** The tests await `Atomics.waitAsync` replies with nothing else keeping the process alive, since the master workers are unref'd.
- A pending `Atomics.waitAsync` is never a ref'd handle in Node. The [minimal repro](evidence-ci-no-skip/waitasync-liveness.log) exits before resolving on 20.19.5, 22.23.4 and 26.12.1 alike.
- Node 26's test runner merely happened to keep these tests' loop open.
- A page's event loop always runs the wait to its timeout or answer. Neither the game code nor the assertions are at fault.
- Proof: with an extra ref'd interval, Node 20 passes 12/12.

**Fix, in the harness.**
- A ref'd keep-alive interval, cleared in a root `after` hook. The harness now holds the loop open the way a page does.
- `watch(worker)` adds `error`/`messageerror` handlers:
  - a start still waiting for its worker rejects at once;
  - `stop(worker)` rethrows a later failure after terminating the worker.
  - Control ([log](evidence-ci-no-skip/negative-control-worker-error.log)): a mutated master import now reports `not ok` with `ERR_MODULE_NOT_FOUND` instead of "cancelled". The mutation was reverted.

**Result.** All 9 game runtime harnesses pass on Node 20.19.5, 22.23.4 and 26.12.1 ([log](evidence-ci-no-skip/harnesses-node-versions.log)). Node 20 has no genuine platform limitation here. CI pins the latest stable release, 26.12.1, by owner directive, not to work around a limitation.

**What the target runtime semantics need.** `SharedArrayBuffer`, `Atomics.wait`/`waitAsync` and `worker_threads`, plus `vm.SourceTextModule` under `--experimental-vm-modules`. Node 20, 22 and 26 all provide these.

# Other reviewer findings

- **`Processes.Run` had no overall timeout.** It now has a wall limit: 10 minutes by default, and 5 minutes for the harnesses.
  - A command past the limit is killed with its whole process tree (`Kill(entireProcessTree: true)`) and refused as `RunnerError.ProcessTimedOut`. So is one whose output stays open past the limit.
  - `ProcessesTests` covers it: a `sleep` grandchild is gone after the refusal (on Linux, a `/proc` zombie counts as gone), and a finished command returns its exit code and both outputs.
- **A harness file reporting zero tests passed.** The waiver is removed, and every harness must declare a test.
  - The two script-style harnesses now declare node:test tests.
    - `workshop-animation-output` is one test. The change is whitespace-only apart from the wrapper.
    - `workshop-observation`'s ten existing blocks became ten tests.
    - No assertion changed.
  - Control: an emptied harness fails `HarnessPasses` with `NoTests`.

# Repository scan

Command: `rg -n --hidden` with an explicit path and `timeout`, excluding bin, obj, .git, `_bmad-output` and node_modules.

Patterns: `Skip =`, `Explicit`, `.skip(`, `.todo(`, `--filter`, `Trait(`, runsettings, `TestCaseFilter`, `skipif`, `mark.skip`, `pytest.skip`, `xfail`.

| Finding | Disposition |
|---|---|
| CI E2eRunner.Tests filter | **Fixed:** generated and checked. |
| Game runtime node harnesses in no required run | **Fixed:** `NodeHarnessTests`. |
| BMAD skill pytest suites, including 2 `skipif(geteuid()==0)` per copy in retrospective; Anvil fixtures | **Out of scope** under the owner's rule. Not run, not provisioned. Named in `OutsideTheSystemProof`. |
| `.githooks/pre-commit` positive selection of the static-gate tests | **Accepted by the coordinator as already ruled:** a scoped selection. CI runs all 2928. Not changed. |
| C# tests: no `Skip`, `Explicit` or traits; no runsettings | Clean. |

# Tasks

- [x] Merge main 8411ba3b, dc0356de and 54e19147 (all fast-forward).
- [x] `BrowserDeferral`, the `deferred-filter` verb (with CommandLineTests), the generated CI filter, and `BrowserDeferralTests`.
- [x] `NodeHarnessTests`, with the wall limit and at least one test per file.
- [x] Clock-ring harness: event-loop and worker-failure fixes, which need no particular Node version.
- [x] `Processes.Run` wall limit with tree kill, and `ProcessesTests`.
- [x] Remove `PythonSkillTests` and the Python/uv/pytest CI steps.
- [x] Pin CI Node at exactly 26.12.1 (owner directive: latest stable).
- [x] Update the tools/e2e README and the TypeCheckTests summary.
- [ ] Independent adversarial review.

# Evidence (`evidence-ci-no-skip/`)

- **[full-slnx-test.log](evidence-ci-no-skip/full-slnx-test.log):** one Debug build (0 warnings), then `dotnet test CuriousContraptions.slnx -c Debug --no-build --no-restore` (no `--blame-crash`). Exit 0, Skipped 0 in every project:

  | Project | Passed |
  |---|---|
  | WorkerAotGuard | 17/17 |
  | Coverage | 77/77 |
  | BundleElements | 191/191 |
  | E2eRunner | 207/207 |
  | CuriousContraptions.tests | 2928/2928 |

  E2eRunner's 207 = 190 + 1 CommandLine row + 5 BrowserDeferralTests + 9 NodeHarnessTests + 2 ProcessesTests. The 15 deferred methods also pass on this workstation, which has the toolchain.
- **[docker-ci-step.log](evidence-ci-no-skip/docker-ci-step.log):** a Linux reproduction of CI. Setup:
  - `ci-sdk-wasm:local`, SDK 10.0.401 with wasm-tools.
  - A non-root user, as on GitHub.
  - Node 26.12.1, read from the workflow's setup-node pin.
  - No npx cache.
  - A fresh git copy, with CI's restore and build. Only the Godot native import fails, because the host is arm64.

  The step script is extracted verbatim from the YAML and run under `bash --noprofile --norc -eo pipefail`. Results:

  | Project | Passed |
  |---|---|
  | Coverage | 77/77 |
  | BundleElements | 191/191 |
  | WorkerAotGuard | 17/17 |
  | E2eRunner | 191/191, which is 207 − 16 deferred cases |

  Skipped 0, step exit 0, and `git status` stays clean.
- **[macos-ci-step.log](evidence-ci-no-skip/macos-ci-step.log):** the same step on macOS (Node 26.12.1) gives the same counts, exit 0.
- **Negative controls:**
  - [negative-controls.log](evidence-ci-no-skip/negative-controls.log): an emptied harness fails with `NoTests`, and an unlisted `CuriousContraptions.Simulation/zz-control.test.ts` fails the coverage check. Both control files were deleted.
  - [negative-control-worker-error.log](evidence-ci-no-skip/negative-control-worker-error.log): the worker-error control above.
- **Node versions:** [harnesses-node-versions.log](evidence-ci-no-skip/harnesses-node-versions.log) and [waitasync-liveness.log](evidence-ci-no-skip/waitasync-liveness.log).
- **`git diff --check`:** clean.

# Residuals

- **CI Node is pinned at exactly 26.12.1 (owner directive).** This is the latest stable release (21 October 2026); the latest LTS is 24.22.1. `actions/setup-node@v4` sets it before the test steps. The harnesses pass on 26.12.1 and also on 20.19.5 and 22.23.4. They were not run on 24.22.1.
- **"Test C# game" is not reproduced on Linux.** It needs the Godot import, which fails on arm64. Its Node use is unchanged by this slice.

# Re-enable

At the post-catalogue browser phase, provision the browser-suite toolchain and delete the entries. Once the list holds no E2eRunner.Tests entry, the generator line and its `--filter` must also go. BrowserDeferralTests fails until they do.
