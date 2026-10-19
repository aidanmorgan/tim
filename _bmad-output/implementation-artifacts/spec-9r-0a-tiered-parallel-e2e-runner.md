---
title: 'Story 9R.0a: Tiered and Parallel E2E Runner'
type: 'tooling'
created: '2026-10-19'
status: 'ready-for-dev'
route: 'dispatch'
review_loop_iteration: 1
context:
  - '{project-root}/AGENTS.md'
  - '{project-root}/_bmad-output/planning-artifacts/epics.md#story-9r0a-tiered-and-parallel-e2e-runner'
  - '{project-root}/_bmad-output/planning-artifacts/autonomous-development-guide.md#verification-tiers'
  - '{project-root}/tools/e2e/README.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Every Chrome gate today is a hand-written zsh script, and each one runs all `tools/e2e/*.test.ts` files one after another. The Story 9.4b candidate-5 cumulative run took 26 min 17 s for 33 files and 140 tests (`evidence-9-4b/candidate-5/cumulative-run1/started` 05:44:15Z, `finished` 06:10:32Z). The scripts also have structural problems:
- They hard-code the npx-cache tsx path and port 8060.
- They count results with `grep`.
- They check bundle identity by hand against a bundle published outside the gate.
- Nothing selects suites by impact.
- Nothing declares which suites may share the machine.
- **A live test-integrity hole:** a Simulation debt fault stops the world (`CCGPU_DEBT_FAULT`), but nothing fails the suite. The line is page console output: the driver never records it, and it never reaches TAP. A suite whose remaining assertions are negative ("never struck", "never samples", "goal stays unsolved") passes vacuously on a stopped world.

**Approach:** Add one checked-in C# runner, `tools/E2eRunner`, with a typed suite catalogue. The runner:
- runs one runner at a time, system-wide;
- publishes the bundle itself from a stamped source tree;
- runs each suite file in its own Node process with its own Chrome;
- runs every suite alone by default, and lets a suite share the machine with N − 1 others only after it has qualified through a recorded calibration;
- computes the Tier 2 selection from a changed-file set;
- runs the Tier 3 checkpoint in a separate worktree.

The driver turns every debt fault into a close finding.

## Boundaries & Constraints

**Always:**
- **Granularity.** One suite file is one Node process (`node --test --test-concurrency=1`) and one Chrome (`chromium.launch` in `WorkshopDriver.launch`, which gives a fresh temporary profile). Files are scheduled whole and never split by test: cat-023b test 4 loads the IndexedDB slot that test 1 saved.
- **A file passes only if all of these hold:**
  - exit code 0;
  - a complete TAP plan, with `# tests` > 0, `# fail 0`, `# cancelled 0`, `# skipped 0` and `# todo 0`;
  - no `not ok` line;
  - `debtFaults == 0`.

  The exit code is authoritative, because a close finding raised in a root hook (`tools/workshop-close-findings.mjs`) leaves every test line `ok` but exits 1.
- **SerialAlone is the default.** A suite is `Parallel` only through a checked-in qualification row whose suite-file SHA-256 matches the current file. An edit to a Parallel suite returns it to SerialAlone until it requalifies. clock-master, cat-017 and cat-023b are never Parallel (owner decision, 19 Oct 2026).
- **One runner at a time, system-wide.** Every runner command that launches Chrome or builds holds one exclusive lock file in the shared git directory for its whole duration. Reviewers run Chrome only through the runner.
- **The runner publishes the bundle it tests**, from the source tree whose digest it stamps into the run. It never runs an externally published bundle.
- **Every run gets a fresh output directory.** It must not already exist, and evidence is never overwritten.
- **Closed sets are C# enums end to end:** suite, timing, impact kind, workshop area, verdict, phase and verb. Strings are converted only at these boundaries: file names, TAP, JSON, git output and CLI arguments. Unknown values are rejected.
- **The project builds under the Story 9R.0 C# static gate** with no suppressions, no catch-all boundaries, typed `catch` clauses only, and `IDisposable` leases for every process, watcher, lock stream and `HttpClient`.
- **One preview URL.** `E2E_PREVIEW_URL` is required. There are no per-suite variables and no 8060 default.

**Never:**
- Any source-scanning timing heuristic. The serial default plus calibration replaces it.
- Retrying into green. A failed file is reported once, with its artifacts.
- Two runners running Chrome or building at once.
- `--workers` above the measured N.
- Sharing one browser process between workers.
- Running against a bundle whose identity changed, or whose source tree changed, during the run.
- A compatibility wrapper for the old gate scripts.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|---|---|---|---|
| Full run | `full --out D` | Takes the lock, stamps the source, publishes, serves, then runs Phase S (SerialAlone files one at a time) and Phase P (qualified Parallel files on N workers). Writes `D/<suite>.tap`, `D/<suite>.log`, `D/summary.txt` and `D/summary.json` | Verdict `Pass` only when every file passes |
| Lock held | Another runner is active | Waits, logging the holder's PID and its `--out` | Never runs concurrently |
| Undeclared suite | `tools/e2e/x.test.ts` has no catalogue entry | Refuses to start and names the file | Exit 2, no Chrome launched |
| Parallel suite edited | Suite SHA-256 ≠ its qualification row | Tier 1 `CatalogueTests` fails. The runner treats the suite as SerialAlone and says so in the summary | — |
| Debt fault | `CCGPU_DEBT_FAULT` on the page | Driver close finding (no opt-out), printed to stderr. `debtFaults` ≥ 1, so the file is `Fail` | — |
| Close finding in an `after` hook | All tests `ok`, exit 1 | File `Fail`, reported against that suite only | — |
| Skipped or todo test | `# skipped 1` or `# todo 1` | File `Fail` (`Skipped`) | — |
| Zero tests | Import error, so TAP shows `# tests 0` | File `Fail` (`NoTests`) | — |
| Hang outside a test | Module-level or `before`-hook hang | Wall limit reached; process tree killed and verified; file `TimedOut` | Other workers continue |
| Hang inside a test | A test exceeds its `timeout` | Node cancels it; `# cancelled 1`; file `Fail` (`Cancelled`) | — |
| Seeded failure | One Parallel suite has one assertion flipped | Only that suite and test are `not ok`; every other file's result equals the unseeded run | — |
| Source changes mid-run | Digest of the tracked and untracked source list differs at the end | Verdict `IdentityChanged` | Exit 2 |
| Bundle changes mid-run | Watcher event or error under `AppBundle/`, or the end re-hash differs | Scheduling stops, running trees are killed, verdict `IdentityChanged` | Exit 2 |
| SIGINT or SIGTERM | Operator stops the runner | Every ledgered process is killed and verified; summary `Aborted` | Exit 2 |
| Stale ledger at startup | A previous runner died | Its live ledgered processes (PID plus start time) are killed, verified and reported, and the ledger is deleted | — |
| Tier 2, element change | `elements/Counter/…` | Own suite, suites whose declared or level-derived parts contain `counter`, port-partner primary suites, and the smoke pair, each with a one-line reason | A missing Primary suite is a typed `NoPrimarySuite` error (exit 2) |
| Tier 2, Workshop area | `ui/WorkshopBenchPlacement.cs` | Suites declaring `BenchPlacement`, plus the smoke pair | — |
| Tier 2, Full | Guide engine list, `AllInteraction` file, non-`.cs` file under `ui/`, `engine/` or `scenes/`, unmapped `.tscn`, or unknown path | `Mode: Full`, with the path and rule as the reason | — |
| Tier 2, node unit | `tools/e2e/unit/**` | `NodeUnit`: Tier 1 node-unit tier, no Chrome suite beyond the smoke pair | — |
| Checkpoint | `checkpoint --commit <sha> --out D` | Fresh detached worktree. Builds the runner and Preview there under the lock, then that worktree's runner runs `full` (publishing inside its own lock) | A commit without `tools/E2eRunner` gives typed `CheckpointUnsupportedCommit` |

### Decisions

- **D1. The runner is C#, as `tools/E2eRunner` plus `tools/E2eRunner.Tests`, both in `CuriousContraptions.slnx`** (coordinator decision, recorded in epics.md).
  - AGENTS.md says to use C# wherever possible. Closed sets are enums.
  - Every sibling tool is C#.
  - tsx does not type-check TypeScript.
  - The projects are compiled under the Story 9R.0 gate. The test project is added to `XunitProjects` in `CuriousContraptions.tests/CSharpStaticGateTests.cs:139-140`. The runner adds nothing to `CatchAllBoundaries` (`:37`).
  - The runner links `engine/PartId.cs` (`Compile Include … Link`, as Coverage links Ownership files) and references `CuriousContraptions.Abstractions` for `SimulationDomain`. The test project uses the same xunit pins as `Coverage.Tests`.
- **D2. Each worker is a separate process with a separate Chrome, all sharing one stateless preview.**
  - Contexts inside one shared browser would share a GPU process and scheduling, and one crash would take down every worker.
  - `tools/Preview` is a read-only static server that strips conditional request headers.
  - IndexedDB lives in each Chrome's temporary profile.
- **D3. Debt faults are visible, so no rate guard is needed.**
  - `WorkshopTickLoop` (`engine/gpu/WorkshopTickLoop.cs`) catches up to the master clock's due tick. Debt beyond 100 ms stops the world, with `CCGPU_DEBT_FAULT` at `CuriousContraptions.Simulation/Program.cs:331`.
  - So contention appears only as read latency (≤ 100 ms), fewer distinct samples, or a debt fault.
  - Change: `WorkshopDriver.observeConsole` (`tools/e2e/workshop-driver.ts:387`) records every `CCGPU_DEBT_FAULT` line and prints `SIMULATION DEBT FAULT (fails this suite at close): <line>` to stderr. `shutdown` (`:442`) returns one finding per session that recorded any. There is no launch option to allow them.
  - The runner counts the stderr marker per file (`debtFaults`). The pass rule requires 0.
- **D4. Timing classification: SerialAlone by default, Parallel by calibration.** This deletes the earlier lexical detector (R0–R4) entirely.
  - **The declaration:** `enum SuiteTiming { SerialAlone = 1, Parallel = 2 }` on each catalogue row. Parallel is legal only with a row in `ParallelQualifications.cs`: `(SuiteId, string SuiteSha256, int Workers, string Evidence)`.
  - **The test that catches an undeclared timing suite (AC2):** `CatalogueTests` fails when:
    - a Parallel row has no qualification;
    - a qualification's SHA-256 differs from the current suite file;
    - a qualification's `Workers` ≠ `MeasuredWorkers.Workers`;
    - an owner-named suite is Parallel.

    A suite that starts measuring time or injecting load must change its file, so it falls back to SerialAlone until it requalifies. A new suite starts SerialAlone.
  - **Candidates for calibration:** every suite except the owner-named three. Recommended exclusions, which simply stay SerialAlone with a one-line catalogue reason: cat-037 (asserts `s.t < 400` and `< 2200`, `:290,395,420`) and cat-014 (`:159` paces assertions by elapsed wall time).
- **D5. Calibration (`calibrate --out D`), run once and recorded.**
  1. **Two N = 1 baselines** of every candidate. Each candidate's baseline is the larger of its two durations. The run fails as `Incomplete` if any candidate fails, skips or debt-faults in either baseline.
  2. **Trials.** For N in {2, 4, 6, 8}, capped at ⌊logical CPUs / 2⌋, all candidates run on N workers. A candidate qualifies at N if it has:
     - 100% pass, no skips;
     - zero debt faults;
     - a duration ≤ 2× its baseline.

     Peak summed RSS of the runner's ledgered processes is sampled every 2 s and must stay ≤ 50% of physical memory; otherwise that N is rejected.
  3. **Choose N.** N is the value with the lowest projected full-run wall time: Phase S (SerialAlone durations) plus the measured Phase P wall time. Ties go to the smaller N.
  4. **Oversubscribed control at 2N.** The same candidates run at 2N. This trial must disqualify at least one candidate (fail, debt fault or > 2× duration), which shows the criteria can fail. If it disqualifies none, N was not the knee: set N := 2N within the cap and repeat steps 2–4. If the cap is reached with no disqualification, calibration is `Incomplete` and the result is reported.
  5. **Write the result.** The runner prints the `ParallelQualifications.cs` rows and the `MeasuredWorkers.cs` constant (`Workers`, `LogicalCpus`, `MemoryBytes`, `ChromeVersion`, `Date`, `Evidence`). The implementer checks them in.
  6. **Fingerprint.** On a machine whose CPU or memory fingerprint differs, every suite runs SerialAlone until the machine is recalibrated.
  7. **Single-suite requalification.** `calibrate --suite S` requalifies one edited suite by running it on 1 worker alongside N − 1 qualified Parallel suites. It writes a new row.
- **D6. Tier 2 connected and integration suites come from typed declarations; bundle ports only name partners.**
  - Port compatibility is dense. Measured on `parts/elements.bundle.json`, the number of partners per element is:

    | Element | Partners |
    |---|---|
    | counter, latch | 9 |
    | battery, pressure_plate, lamp | 7 |
    | clock | 5 |
    | bumper, powered_gate | 4 |
    | domino, ball_detector | 3 |
    | ball, ramp, wall | 0 |

  - Therefore:
    - **integration** = suites whose effective parts contain E, where effective parts = declared `Places` ∪ the `parts` of every declared level in `content/puzzles.json`;
    - **connected** = the declared `Primary` suite of each port partner.
  - A changed element or a partner with no Primary suite is a typed `NoPrimarySuite(PartId)` error. The selection fails rather than returning empty.
- **D7. Source and bundle identity: the runner publishes.**
  - **Why publish rather than stamp a digest into the bundle:**
    - Stamping needs an MSBuild target in the product build that writes a file into `AppBundle`. That is production code for a test concern, and it still trusts that the bundle was built from the stamped tree.
    - Publishing inside the run proves provenance by construction and changes no product code.
    - The cost is one publish per run, paid inside the lock.
  - **Source digest:** SHA-256 over `path\tsha256(content)` for every path from `git ls-files --cached --others --exclude-standard`. A deleted tracked file contributes `path\tdeleted`. It is computed before publish and again at the end; any difference gives `IdentityChanged`. HEAD is also recorded.
  - **Bundle identity:**
    - The `FileSystemWatcher` on `CuriousContraptions.web/AppBundle` starts first, before hashing. Any event or watcher `Error` after hashing aborts the run.
    - Identity is the SHA-256 of the sorted `relative-path\tsha256` manifest.
    - "Served" means every manifest path fetched from the runner's preview has the same hash. Dot-files are `NotServed`.
    - The end-of-run re-hash plus a second served check is authoritative.
- **D8. One global runner lock.**
  - The lock is `$(git rev-parse --git-common-dir)/e2e-runner.lock`, an exclusive OS file lock held by a `RunnerLock` lease. The kernel releases it if the process dies.
  - Every verb that builds, publishes or launches Chrome holds it for its whole duration: `full`, `run`, `calibrate` and the checkpoint's build step. `select` does not take it.
  - Tier 1 commands outside the runner are not governed by the lock. The runner records the 1-minute load average at the start and end of each SerialAlone file as evidence only.
  - Before Phase S, headless Chrome processes not in any live ledger refuse the run as `Incomplete(ForeignChrome)`, listing their PIDs.
- **D9. Process ownership.**
  - **Ledger.** Each run keeps `$(git-common-dir)/e2e-runner/ledgers/<runId>.pids`. Every 2 s, and on each file's exit, the runner snapshots `(pid, start time)` of every descendant of each suite process.
  - **After every file:** `ProcessLease.Dispose` calls `Kill(entireProcessTree: true)`, then kills any ledgered pair still alive (PID and start time must both match, so a reused PID is never killed), then verifies. A survivor is `Fail(OrphanSurvived)`.
  - **Startup:** ledgers whose owner runner is dead are reaped the same way, reported, and deleted.
  - **Signals:** SIGINT and SIGTERM (`PosixSignalRegistration`) cancel the run, reap every ledgered process, and write the summary `Aborted` (exit 2).
- **D10. One preview URL** (coordinator decision, recorded in epics.md).
  - `WorkshopDriver.launch` reads the required `E2E_PREVIEW_URL`.
  - The `baseUrl` option, the five per-suite URL variables, `PLAYWRIGHT_BASE_URL` and every `8060` default are deleted.
  - The runner builds `tools/Preview` inside the lock, then starts it with `dotnet run --no-build --project tools/Preview -- <bundle> <port>` on a free port. If the bind fails it retries with a new port, up to 3 times. The served check proves which process answered.
- **D11. Checkpoint: `checkpoint --commit <sha> --out D`.**
  1. `git worktree add --detach .claude/worktrees/e2e-checkpoint-<sha12> <sha>`, refusing if the path exists.
  2. Under the lock: `dotnet build` of `tools/E2eRunner` and `tools/Preview` in that worktree. Then the lock is released.
  3. Run `dotnet run --no-build --project tools/E2eRunner -- full --out <D>` in the worktree. The inner runner waits for the lock and publishes inside it. Any runner that wins the brief handoff gap simply runs alone first.
  4. Copy the worktree's `.anvil/e2e-runs/<runId>/` into `D/anvil/`. cat-015b, cat-062 and cat-062b must keep their evidence directories inside their own working directory's `.anvil`.
  5. On Pass, `git worktree remove --force` the worktree. On Fail, keep it and report its path.

  The checkpoint is proven against a local, unpushed candidate commit that contains the runner (coordinator decision, recorded in epics.md).
- **D12. Tier 2 path classification** (coordinator decision on the area table, recorded in epics.md). Each path gets exactly one `ImpactKind`, and a test forbids overlapping rules:
  - **`NoBrowserImpact`:** `docs/**`, `_bmad-output/**`, `_bmad/**`, `.claude/**`, `.agents/**`, root `*.md`, `tools/e2e/README.md`, `CuriousContraptions.tests/**`, `tools/{Coverage,Coverage.Tests,Ownership,anvil,LifecycleContract,WireContract,TraceAllocations}/**`.
  - **`NodeUnit`:** `tools/e2e/unit/**`. It runs the Tier 1 node-unit tier, not Full (coordinator decision: Story 9R.1's unit test lives there).
  - **`Suite(SuiteId)`:** `tools/e2e/<suite>.test.ts`.
  - **`SharedTestModule`:** any other `tools/e2e/*.ts` and `tools/*.mjs`. It selects the suites whose import closure contains the module. The driver and wire are imported by every suite, so they select `Full(SharedDriverInfrastructure)`.
  - **`Element(PartId)`:**
    - `elements/<Dir>/**` through the typed `ElementPackages` table;
    - `parts/catalog/<id>.tres`, `parts/icons/<id>.png`;
    - `parts/scenes/<x>.tscn`, mapped to every PartId whose catalogue resource or bundle `ScenePath` references it.

    A `.tscn` that maps to no PartId is `Full(Unknown)`.
  - **`ElementBundle`:** `parts/elements.bundle.json`, diffed per element between base and head. A `GeneratedAt`-only change is `NoBrowserImpact`; a parse failure is Full.
  - **`Level(LevelId)`:** `content/puzzles.json`, diffed per level `id`.
  - **`WorkshopArea(area)`:** only `.cs` files (and their `.cs.uid`) under `engine/` (outside `engine/gpu/`), `ui/` and `scenes/`, through the typed area table. A test checks that table against `git ls-files`.
    - Files every interaction passes through are `AllInteraction`, which means Full: `ui/Workshop.cs`, `engine/MachineWorld*.cs`, `engine/WorkshopUiBinding.cs`, and `scenes/workshop.tscn` (all interaction suites).
    - Any non-`.cs` file under these roots is Full.
  - **`Full`:** the guide's Tier 3 list, which is:
    - `engine/gpu/**`;
    - `CuriousContraptions.Simulation/**`, `CuriousContraptions.Animation*/**`, `CuriousContraptions.web/**`;
    - `CuriousContraptions.Abstractions*/**`, `CuriousContraptions.FlatBuffers/**`, `schemas/**`;
    - the shared driver and test infrastructure, `tools/BundleElements/**`, `tools/Preview/**`, `tools/E2eRunner*/**`;
    - build files;

    plus `AllInteraction` and any unknown path.
  - **Input** is either `--diff <base>` or `--changed <file>`.
    - `--diff <base>` combines `git diff --name-status <base>` (renames give both paths) with untracked files.
    - `--changed <file>` must contain repo-relative paths; `..`, absolute paths and non-canonical case are rejected.

  Every selection includes the smoke pair `engine-core-2b1` and `engine-core-2a1`, and gives one reason line per suite.

**Deletes:**
- the lexical timing detector from the earlier draft;
- the scratch per-candidate gate scripts that serialise every file;
- the five per-suite preview-URL variables, `PLAYWRIGHT_BASE_URL`, the driver's `baseUrl` option and every `8060` default;
- clock-master's screenshot default inside committed evidence;
- every "all suites serially" and "cumulative twice" wording in active docs.

Tracked evidence scripts in `evidence-9-4b/` stay unchanged (coordinator decision, recorded in epics.md).

</frozen-after-approval>

## Code Map

Line references are from main HEAD `c854b4d5`, which has 34 suite files.

- **`tools/e2e/workshop-driver.ts`:**
  - `launch` (`:356-385`), with the 8060 default at `:357`;
  - `observeConsole` (`:387`), where the malformed-reply pattern at `:395-398` is the model for the debt-fault record;
  - `shutdown` (`:442`), where the rejection finding at `:453` is the model;
  - Playwright comes from the global `@playwright/mcp` install (`:11-12`).
- **Per-suite preview variables:**
  - `cat-015a.test.ts:7`, `cat-015a-sidekick.test.ts:6`, `cat-015b.test.ts:13`: `BUMPER_PREVIEW_URL`;
  - `cat-005.test.ts:9`: `BATTERY_PREVIEW_URL` with a `PLAYWRIGHT_BASE_URL` fallback;
  - `cat-062.test.ts:10`: `SPRING_PREVIEW_URL`;
  - `cat-062b.test.ts:13`: `PRELOAD_PREVIEW_URL`;
  - `clock-master.test.ts:13`: `CLOCK_PREVIEW_URL`; `:15` defaults the screenshot into the committed `evidence-9-4b/` directory.
- **Evidence directories that must stay inside the working directory's `.anvil`:** `cat-015b.test.ts:24-26`, `cat-062.test.ts:11`, `cat-062b.test.ts:14-16`.
- **The debt-fault source:** `CuriousContraptions.Simulation/Program.cs:325-336`.
- **Possible deliberate debt fault:** `clock-master.test.ts:206-245` spins the Simulation for 4000 ms while Running (see Open decision OD7).
- **C# static gate:**
  - `.githooks/pre-commit` builds the slnx with binlogs.
  - `CuriousContraptions.tests/CSharpStaticGateTests.cs`: `XunitProjects` at `:139-140`, `CatchAllBoundaries` at `:37`, and the slnx-derived `Projects` at `:250`.
- **Parts mapping:**
  - `engine/PartId.cs`;
  - the driver's `selectTool` union (`:574`), whose tool kinds `basketball` and `receiver` differ from the PartId values `ball` and `basket`;
  - `elements/*/` (13 packages);
  - `parts/elements.bundle.json`, which includes `GeneratedAt`;
  - `parts/catalog/*.tres`;
  - `content/puzzles.json`, whose levels have `id` and `parts`.
- **Preview:** `tools/Preview/Program.cs` takes `[directory [port [base-path/]]]` and is in the slnx.
- **Active wording to change:**
  - `autonomous-development-guide.md:66,134,142,175,212`;
  - `epics.md:51` (NFR-06), `:62` (Global DoD 4) and the scope rule's "full serial cumulative Chrome run";
  - `tools/e2e/README.md:3-16`.

## Tasks & Acceptance

**Execution** (test first):

- [ ] **`tools/E2eRunner/E2eRunner.csproj`, `tools/E2eRunner.Tests/E2eRunner.Tests.csproj` and their slnx entries.** Add the test project to `XunitProjects`.
- [ ] **`SuiteCatalogue.cs`.** Holds:
  - `enum SuiteId`;
  - `enum SuiteTiming`;
  - `record SuiteDeclaration(SuiteId Id, SuiteTiming Timing, string? SerialReason, PartId? Primary, IReadOnlySet<PartId> Places, PlacementScope Scope, IReadOnlySet<LevelId> Levels, IReadOnlySet<SimulationDomain> Wires, IReadOnlySet<WorkshopArea> Areas, bool Smoke)`;
  - `FileName(SuiteId)`;
  - `LevelId` (strongly typed, validated against `content/puzzles.json` plus `free_workshop`).

  `ParallelQualifications.cs` and `MeasuredWorkers.cs` hold the calibration output (D5).
- [ ] **`ImpactRules.cs`, `WorkshopAreas.cs` and `Selection.cs`** (D6, D12).
- [ ] **`SourceIdentity.cs`, `BundleIdentity.cs`, `Publisher.cs` and `PreviewHost.cs`** (D7, D10). The watcher, the preview process and `HttpClient` are leases.
- [ ] **`RunnerLock.cs`** (D8), **`ProcessLease.cs` and `ProcessLedger.cs`** (D9).
- [ ] **`Toolchain.cs`.** Resolves and records the toolchain, failing typed if any part is missing:
  - tsx `4.23.15`, found by scanning `~/.npm/_npx/*/node_modules/tsx/package.json`;
  - `@playwright/mcp` and its `playwright` version;
  - the Node version;
  - the Chrome version.
- [ ] **`SuiteProcess.cs`.** Starts one suite process:
  - **Command:** `node --import <tsx> --test --test-concurrency=1 --test-timeout=150000 --test-force-exit --test-reporter=tap --test-reporter-destination=<D>/<suite>.tap tools/e2e/<suite>.test.ts`, with stdout and stderr going to `<D>/<suite>.log`.
  - **Environment.** An explicit environment: PATH, HOME and TMPDIR are inherited, plus:
    - `E2E_PREVIEW_URL`;
    - `CLOCK_DERIVATION_LOG=<D>/<suite>.derivations.jsonl`;
    - `CLOCK_EVIDENCE_SCREENSHOT=<D>/clock-master.simulation-unresponsive.png`;
    - `CAT015B_EVIDENCE_DIR`, `PRELOAD_EVIDENCE_DIR` and `SPRING_EVIDENCE_DIR`, set to new leaf paths under `.anvil/e2e-runs/<runId>/`.

    Every `E2E_*`, `CLOCK_*` and `*_PREVIEW_URL` variable from the parent shell is dropped.
  - **Wall limit:** 15 min per file.
- [ ] **`TapResult.cs`.** The per-file columns:
  - `exit`, `tests`, `pass`, `fail`, `cancelled`, `skipped`, `todo`, `notok`;
  - `closeFindings`, `debtFaults`, `unresponsive`, `rejected`, `recal`, `renewedLate`, `lateRenewalsReported`, `wideOffset`;
  - `orphans`, `durationMs`, `phase`, `start` and `end`.
- [ ] **`Scheduler.cs`.**
  - Phase S runs first: SerialAlone files in catalogue order.
  - Phase P follows: qualified Parallel files, longest first by their calibration baseline, on N workers.
  - `--workers k` is accepted only when k ≤ N.
- [ ] **`RunSummary.cs`.** The run header records:
  - runner source digest, source digest and HEAD;
  - bundle identity before and after;
  - the served check;
  - toolchain versions, N and the fingerprint;
  - the selection with reasons;
  - wall time and load-average samples.

  It also records per-file rows and a `Verdict` enum: `Pass`, `Fail`, `Incomplete`, `IdentityChanged` or `Aborted`. Exit codes: 0 Pass, 1 Fail, 2 any other verdict.
- [ ] **`Program.cs`.** `enum Verb { Full, Select, Run, Checkpoint, Calibrate }`:
  - `full --out D [--workers k]`
  - `select (--diff B | --changed F)`
  - `run (--diff B | --changed F) --out D`
  - `checkpoint --commit SHA --out D`
  - `calibrate --out D [--suite S]`
- [ ] **`tools/e2e/workshop-driver.ts`.**
  - Require `E2E_PREVIEW_URL`; delete `baseUrl` and 8060.
  - Add the debt-fault record, stderr line and close finding (D3), with no opt-out.
- [ ] **The suites.**
  - Delete the per-suite URL constants and `PLAYWRIGHT_BASE_URL`.
  - `clock-master.test.ts`: `CLOCK_EVIDENCE_SCREENSHOT` is required.
- [ ] **Docs.**
  - `tools/e2e/README.md`:
    - the runner verbs;
    - `E2E_PREVIEW_URL`;
    - "reviewers and implementers run Chrome only through the runner";
    - how a new suite is declared (SerialAlone) and requalified.
  - In the guide and epics lines in the Code Map, "Tier 3 full run by `tools/E2eRunner`" replaces "all suites serially" and "cumulative twice".
  - Delete the scratch gate scripts.

**Acceptance Criteria → test plan:**

| AC | Proof | Kind |
|---|---|---|
| **AC1.** One runner, one process per file, per-file TAP, exit code and close findings, one summary; serial alone, parallel on N, N measured | `SuiteProcessTests` with no-browser fixture `.test.ts` files in `tools/E2eRunner.Tests/Fixtures/`, using the same tsx path (a missing tsx fails, it does not skip). Fixtures: pass; fail; close finding in `after` (exit 1, all ok); zero tests; `test.skip`; `test.todo`; a debt-fault stderr marker with exit 0; module-level hang (`await new Promise(() => {})` at top level, giving `TimedOut` through the wall limit, set low by the test); `before`-hook hang (`TimedOut`); in-test hang with `{ timeout: 200 }` (`Cancelled`); crash; env scrub; a child process that outlives node (ledger reaps it; `OrphanSurvived` if reaping is mutated). `SchedulerTests` on a fake clock: no SerialAlone interval overlaps any other interval, concurrency ≤ N, and `--workers N+1` is refused. `CalibrationTests` cover the D5 rules on recorded trial tables, including a 2N trial that disqualifies none (gives N := 2N) and the cap reached (`Incomplete`). Chrome: `calibrate` evidence plus the checked-in `MeasuredWorkers.cs` and `ParallelQualifications.cs` | Tier 1 + Chrome |
| **AC2.** A typed timing declaration, and a test that catches an undeclared suite | `CatalogueTests`: (a) catalogue and files are a bijection; (b) a Parallel row without a qualification fails; (c) a stale suite SHA-256 fails, shown by a one-byte edit fixture copy; (d) a qualification whose `Workers` ≠ N fails; (e) owner-named suites are never Parallel; (f) each suite's literal `selectTool` and `selectLevel` calls ⊆ its declared `Places` and `Levels` (tool-kind→PartId boundary table); (g) `ElementPackages` matches `elements/*` and the bundle; (h) the area table covers every tracked `.cs` under `engine/` (outside `gpu/`), `ui/` and `scenes/` | Tier 1 |
| **AC3.** Tier 2 selection with reasons; unknown or shared → Full | `SelectionTests`, table-driven, each row with an exact suite set, reason lines and mode: counter declaration (own + integration + partner primaries + level-derived suites + smoke); an element with no Primary (`NoPrimarySuite`); `parts/scenes/ball.tscn` (shared scene); an unmapped `.tscn` (Full); `scenes/workshop.tscn` (Full); `ui/foo.png` (Full); `ui/WorkshopBenchPlacement.cs` (area); `ui/Workshop.cs` (Full); a new `ui/X.cs` missing from the table (Full); `content/puzzles.json` with one level changed; a `GeneratedAt`-only bundle change; `tools/workshop-console.mjs`; driver (Full); `engine/gpu/X.cs` (Full); `tools/e2e/unit/x.test.ts` (`NodeUnit`); an unknown root file (Full); docs only (smoke pair); the empty set (smoke pair); a rename out of `elements/` (Full) | Tier 1 |
| **AC4.** Bundle identity recorded before and after; refuse on change | `SourceIdentityTests`: tracked, untracked and deleted paths; ignored files excluded. `BundleIdentityTests`: manifest determinism; dot-file `NotServed`. `PreviewHostTests` with the real `tools/Preview` and a temp bundle: served ≠ disk refuses; a foreign listener on the port refuses; a mid-run file change aborts with `IdentityChanged`; a change then restore is still `IdentityChanged`; a watcher `Error` aborts; a source change mid-run gives `IdentityChanged`. `PublisherTests`: the run's bundle is the one the runner published, and a pre-existing `AppBundle` is replaced, never reused | Tier 1 |
| **AC5.** The parallel full run passes 100% on two consecutive invocations; wall time and serial baseline recorded; a seeded failure is reported against that suite alone | Two `full` runs on one source digest and bundle identity, with wall times recorded next to the calibration N = 1 baseline and the historical 26 min 17 s. Seeded run: a temporary one-line `assert.equal(1, 2)` in one qualified Parallel suite (for example cat-049 test 2) shows exactly one `not ok`, that file `Fail`, and every other row identical in pass/fail to the unseeded run. The diff is recorded and reverted, and the restored source digest is shown. Seeded debt fault: a temporary test that blocks the Simulation 600 ms while Running (the `blockSimulation` helper pattern in clock-master) makes that file `Fail` with `debtFaults ≥ 1`. It is reverted and recorded | Chrome |
| **AC6.** Tier 3 checkpoint against a committed HEAD in a separate worktree and port, in the background | `checkpoint --commit <local candidate commit>` started while a foreground `run --changed` waits for the lock. Evidence: both summaries; lock acquisition times showing no overlap; distinct ports; `D/anvil/` copied; the worktree removed on Pass. `CheckpointTests`: a commit without the runner gives `CheckpointUnsupportedCommit`; an existing path refuses | Chrome + Tier 1 |
| **AC7.** `dotnet build` 0 warnings under the 9R.0 gate, `dotnet test` 100%, Node harnesses pass | Tier 1 logs, including the pre-commit gate's binlog check covering the two new projects | Tier 1 |

**Process-safety tests.** `SignalTests` start the runner as a child process on a hanging fixture, send SIGTERM (and separately SIGINT), and expect exit 2, summary `Aborted`, and no ledgered process alive. `LedgerTests` write a stale ledger holding a live `sleep` with a dead owner PID; startup reaps it, and a reused PID with a different start time is left alone. `LockTests` start two runner processes on the same git common dir and show that the second starts only after the first exits.

## Implementation Notes

- Before green, record the candidate table: every suite, its N = 1 baselines, and its D4 exclusion reason if any.
- Confirm from the Red run whether clock-master's 4000 ms Running spin emits `CCGPU_DEBT_FAULT`. If it does, stop and raise OD7. Do not add an opt-out.
- Confirm that `node --test` forwards the suite's stderr lines to the TAP destination as comments or only to the `.log`. Count markers from whichever stream carries them, and say which in the README.

## Spec Change Log

- **Iteration 1 (coordinator decisions on the independent design review, 19 Oct 2026):**
  - Deleted the lexical timing detector (R0–R4); SerialAlone is now the default, and Parallel requires calibration with a digest-pinned qualification.
  - Deleted the K-slot protocol and the contended-observation re-run; one global runner lock replaces them.
  - Debt faults became driver close findings.
  - Skipped and todo tests fail.
  - The runner publishes its own bundle from a stamped tree.
  - Added the PID ledger and signal handling.
  - Tier 2 under-selection fixed: unmapped `.tscn` and non-`.cs` files under `ui/`, `engine/` and `scenes/` select Full; `NoPrimarySuite` is an error; selection goes through level parts; `NodeUnit` added.
  - Low fixes: `git ls-files` digest, watcher started first, `worktree remove --force`.

## Review Triage Log

| Finding | Disposition |
|---|---|
| H1. Debt faults are not counted; suites can pass on a stopped world | **Fixed.** Driver close finding with no opt-out, plus a stderr marker; pass rule `debtFaults == 0`; fixture and mutant M3; Chrome seeded proof (AC5). Named in Intent as a live hole in today's suites |
| H2, H3. Findings against the lexical timing detector | **Removed by construction.** Detector deleted; SerialAlone default; Parallel only by calibration with a digest pin (D4, D5) |
| H4. Seeded hang inside a test is caught by node's per-test timeout, not the wall limit | **Fixed.** Module-level and `before`-hook hang fixtures prove `TimedOut`; a separate in-test fixture proves `Cancelled` |
| H5. Findings against the K-slot lock protocol | **Removed by construction.** One global runner lock (D8) |
| M6. Analyzer gate coverage | **Fixed.** Test project added to `XunitProjects`; slnx entries; no suppressions or catch-all boundaries; typed catches; leases (D1) |
| M7. Skipped tests can pass | **Fixed.** `# skipped` or `# todo` > 0 is `Fail` (fixtures, mutant M4) |
| M8. Source identity of the bundle unproven | **Fixed.** The runner stamps the source digest and publishes itself (D7) |
| M9. Orphan processes | **Fixed.** PID ledger with start times, verify after every file, stale-ledger reaping, SIGINT and SIGTERM tests (D9) |
| M10, M11. Findings against the slot protocol and concurrent runners | **Removed by construction.** Global lock; `--workers` ≤ N; reviewers run Chrome only through the runner |
| M12. Tier 2 under-selection | **Fixed.** D6 and D12 rules; `SelectionTests` rows |
| L. Digest over walked directories | **Fixed.** `git ls-files -co --exclude-standard` only |
| L. Watcher started after hashing; watcher errors ignored | **Fixed.** Watcher first; `Error` aborts |
| L. `git worktree remove` fails on build outputs | **Fixed.** `--force` |
| L. `tools/e2e/unit/**` treated as Full | **Fixed.** `NodeUnit` impact |

## Design Notes

**Red → green → mutation.** Red first: catalogue tests, selection table, TAP verdict fixtures, identity tests, ledger, signal and lock tests. Each mutant must turn a named test red:

| # | Mutant | Killed by |
|---|---|---|
| M1 | Pass a file on TAP alone, ignoring exit | `after`-hook fixture |
| M2 | Treat zero tests as a pass | `NoTests` fixture |
| M3 | Drop `debtFaults == 0` from the pass rule | Debt-marker fixture |
| M4 | Ignore `# skipped` and `# todo` | Skip and todo fixtures |
| M5 | Accept a Parallel row with a stale SHA-256 | `CatalogueTests` (c) |
| M6 | Scheduler places a SerialAlone suite in Phase P | Overlap assertion |
| M7 | Accept `--workers` > N | Scheduler refusal test |
| M8 | Selection drops port-partner primaries | Counter row |
| M9 | Selection ignores level parts | Counter level-derived row |
| M10 | `NoPrimarySuite` returns empty | No-Primary row |
| M11 | An unmapped `.tscn` is classified `NoBrowserImpact` | Unmapped `.tscn` row |
| M12 | Watcher disabled | Change-and-restore test |
| M13 | Watcher started after hashing | Change-during-hash test |
| M14 | Ledger not consulted after a file | Outliving-child fixture |
| M15 | No signal handler | `SignalTests` |
| M16 | Lock not taken by `full` | `LockTests` |
| M17 | Driver debt finding gated by an option | Chrome seeded debt fault (AC5) |
| M18 | Calibration accepts a 2N trial with no disqualification | `CalibrationTests` |

**Risks.**
- **CPU contention skewing timing.** Godot WASM, the interpreted Simulation worker (3–4 ms per tick in Chrome) and the Animation worker each take roughly 0.5–1 core per Chrome, plus a GPU process. Mitigations:
  - SerialAlone is the default;
  - Parallel needs a measured 2× bound and zero debt faults at N;
  - the 2N control proves the criteria can trip;
  - debt faults are now failures;
  - the global lock rules out a second runner.

  Tier 1 builds started by hand are not governed by the lock. Load averages are recorded so a reviewer can see them.
- **Godot WASM memory.** Each Chrome holds the Godot heap plus two .NET runtimes. Calibration rejects any N whose peak RSS exceeds 50% of RAM.
- **GPU contention.** A lost context surfaces as a page error, which every suite already fails on.
- **Shared preview state.** The preview is stateless. Each Chrome profile is temporary. Evidence and derivation files are per run and per suite.
- **Per-run publish cost.** One publish per run, inside the lock, in exchange for provenance by construction.
- **Clean-worktree publish** may need untracked state, such as the Godot import cache. It fails typed, with no implicit copying.

## Design review (adversarial, edge-case hunter)

| # | Attack | How the spec handles it |
|---|---|---|
| A1 | A suite starts measuring time or injecting load but keeps running in parallel | Any edit breaks the qualification SHA-256, so the suite returns to SerialAlone until it requalifies (D4) |
| A2 | Classification looks good but is not sensitive | The 2N control must disqualify something, or N is raised (D5) |
| A3 | The N = 1 baseline is noisy | Two baselines; the larger counts; any failure makes calibration `Incomplete` |
| A4 | New suite added | Bijection test; it defaults to SerialAlone |
| A5 | Owner-named suite marked Parallel | `CatalogueTests` (e) |
| A6 | Two runners (foreground plus checkpoint) run Chrome together | The global lock; the checkpoint builds inside it |
| A7 | Runner crash leaves the lock held | Kernel lock release on process death |
| A8 | Runner crash orphans Chrome | Stale-ledger reaping at startup; foreign headless Chrome refuses Phase S |
| A9 | PID reused before reaping | The ledger matches PID and start time |
| A10 | Ctrl-C mid-run | Signal handler reaps and writes `Aborted` |
| A11 | Bundle published from a different tree | The runner publishes from the stamped tree; source digest checked again at the end |
| A12 | Untracked new source file compiled but not digested | `git ls-files --others --exclude-standard` is included |
| A13 | Another agent edits sources mid-run | Source digest mismatch gives `IdentityChanged` |
| A14 | Bundle changes during hashing | The watcher starts before hashing |
| A15 | Watcher buffer overflow | `Error` aborts |
| A16 | Stale 8060 preview from another agent | No 8060; URL required; served check over the full manifest |
| A17 | Port race on preview start | Retry up to 3 times; the served check proves which process answered |
| A18 | Test-order dependency within a file | Files are never split |
| A19 | IndexedDB or evidence-path collisions | Temporary profiles; per-run, per-suite evidence variables; per-suite derivation logs |
| A20 | Leaked `E2E_SLOWMO` from the shell | Explicit environment, with a scrub fixture |
| A21 | All `ok` but a close finding | Exit code authoritative |
| A22 | Zero tests, skipped or todo tests | `Fail` |
| A23 | Hang in a hook or at module level | Wall limit gives `TimedOut` (H4 fixture) |
| A24 | Debt fault on a stopped world lets negative assertions pass | Close finding (H1) |
| A25 | Implementer passes a trimmed `--changed` list | The summary records the list and base; the reviewer recomputes with `--diff` |
| A26 | `GeneratedAt` churn | Per-element diff |
| A27 | Shared scene or unmapped scene | All referencing PartIds, or Full |
| A28 | `scenes/workshop.tscn` or a `ui/` asset change | Full |
| A29 | Element with no suite yet | `NoPrimarySuite` error, never an empty selection |
| A30 | Suite places a part only through an authored level | Effective parts include the level's `parts` |
| A31 | Dense port graph turns Tier 2 into Tier 3 | Integration from declarations; connected limited to partner primaries |
| A32 | Checkpoint commit predates the runner | `CheckpointUnsupportedCommit` |
| A33 | Checkpoint worktree path exists, or removal fails on build outputs | Refuse an existing path; `--force` on removal |
| A34 | Checkpoint removal deletes `.anvil` evidence | Copied into `D/anvil/` first |
| A35 | Machine changed | A fingerprint mismatch makes every suite SerialAlone |
| A36 | tsx missing | Typed failure; no download mid-run |
| A37 | Analyzer gate silently excludes the new projects | slnx `Projects` plus the `XunitProjects` entry; checked by the binlog compile list |

## Open decisions for the coordinator

The coordinator decisions already recorded in `epics.md` are kept: C# runner; `E2E_PREVIEW_URL` required; 9R.1 unit test in `tools/e2e/unit/`; checkpoint proven on a local unpushed commit; area table; 9R.0b as one full runner run; evidence scripts unchanged.

- **OD7 (new). Deliberate debt fault in clock-master.**
  - The test "A Simulation spin beyond the liveness bound…" (`clock-master.test.ts:206-245`) blocks the Simulation for 4000 ms after `run(0)`. It may emit `CCGPU_DEBT_FAULT` when the loop resumes.
  - With no opt-out, that suite would then fail.
  - Recommendation: run the spin in Build mode, as the 600 ms block already does. The liveness fault does not need a Running world, so no debt fault occurs and no exception is needed.
  - If the Running world is essential to that proof, the alternative is a typed exact-count expectation on that one launch, which fails on a missing or an extra fault.
- **OD8 (new). Calibration exclusions.** Recommendation: keep cat-037 and cat-014 SerialAlone without calibrating them, because their assertions compare wall-clock times. Calibration would otherwise likely admit them at low N.
