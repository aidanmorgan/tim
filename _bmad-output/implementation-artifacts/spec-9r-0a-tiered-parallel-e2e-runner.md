---
title: 'Story 9R.0a: Tiered and Parallel E2E Runner'
type: 'tooling'
created: '2026-10-19'
status: 'ready-for-dev'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/AGENTS.md'
  - '{project-root}/_bmad-output/planning-artifacts/epics.md#story-9r0a-tiered-and-parallel-e2e-runner'
  - '{project-root}/_bmad-output/planning-artifacts/autonomous-development-guide.md#verification-tiers'
  - '{project-root}/tools/e2e/README.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Every Chrome gate today is a hand-written zsh script, and each one runs all `tools/e2e/*.test.ts` files one after another. The Story 9.4b candidate-5 cumulative run took 26 min 17 s for 33 files and 140 tests (`evidence-9-4b/candidate-5/cumulative-run1/started` 05:44:15Z, `finished` 06:10:32Z). The scripts also have structural problems:
- They hard-code the npx-cache tsx path (`/Users/aidan/.npm/_npx/fd45a72a…/tsx/dist/loader.mjs`) and port 8060.
- They count results with `grep`.
- They check bundle identity by hand: `served-identity.txt` covers 8 files, and the tree-change diff is optional.
- Nothing selects suites by impact.
- Nothing declares which suites are timing-sensitive.

**Approach:** Add one checked-in C# runner, `tools/E2eRunner`, with a typed suite catalogue. The runner:
- runs each suite file in its own Node process with its own Chrome;
- runs the timing-sensitive suites alone and the rest on N measured workers;
- computes the Tier 2 selection from a changed-file set;
- pins the served bundle identity for the whole run;
- can run the Tier 3 checkpoint in a separate worktree on its own port.

A Tier 1 test fails when a suite measures time, or injects stalls or load, without being declared.

## Boundaries & Constraints

**Always:**
- **Granularity.** One suite file is one Node process (`node --test --test-concurrency=1`) and one Chrome (`chromium.launch` in `WorkshopDriver.launch`, which gives a fresh temporary profile). Files are scheduled whole and never split by test: cat-023b test 4 loads the IndexedDB slot that test 1 saved.
- **A file passes only if all of these hold:** exit code 0; the TAP plan is complete; `# tests` > 0; `# fail 0`; `# cancelled 0`; and no `not ok` line. The exit code is authoritative, because a close finding raised in a root hook (`tools/workshop-close-findings.mjs`) leaves every test line `ok` but exits 1.
- **Timing-sensitive suites run alone.** No other runner-managed Chrome or build may be active on the machine, including the background checkpoint's.
- **Every run gets a fresh output directory.** It must not already exist, and evidence is never overwritten.
- **Closed sets are C# enums end to end:** suite, timing class, timing reason, impact kind, verdict and phase. Strings are converted only at these boundaries: file names, TAP, JSON, git output and CLI arguments. Unknown values are rejected.
- **One current path.** One preview-URL variable replaces the five per-suite ones. There are no aliases, and there is no 8060 default.

**Never:**
- A filename heuristic for timing sensitivity.
- Retrying into green. A failed file is reported once, with its artifacts. A contended observation is invalid, never silently replaced.
- Sharing one browser process between workers.
- Running against a bundle whose served identity differs from the on-disk identity recorded at start.
- A compatibility wrapper for the old gate scripts.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|---|---|---|---|
| Full run | `full --out D` | Phase S: SerialAlone files one at a time. Phase P: Parallel files on N workers. Writes `D/<suite>.tap`, `D/<suite>.log`, `D/summary.txt` and `D/summary.json` | Verdict `Pass` only when every file passes |
| Undeclared suite | `tools/e2e/x.test.ts` has no catalogue entry | The runner refuses to start and names the file | Exit 2, no Chrome launched |
| Detector hit on a Parallel suite | Suite contains `while (performance.now() < end) {}` | Tier 1 test `TimingDeclarationTests` fails, naming the file, line and rule | N/A |
| Close finding in an `after` hook | All tests `ok`, exit 1 | File `Fail`, reported against that suite only | Other files unaffected |
| Zero tests | Import error, so TAP shows `# tests 0` | File `Fail` (`NoTests`) | — |
| Hang | File exceeds its wall limit | Process tree killed, file `TimedOut` | Other workers continue |
| Seeded failure | One Parallel suite has one assertion flipped | Only that suite and test are `not ok`; every other file's result equals the unseeded run | — |
| Bundle changes mid-run | A file under `AppBundle/` changes | Scheduling stops, running trees are killed, verdict `IdentityChanged` | Exit 2; no Pass is possible |
| Served ≠ disk at start | Another server on the port, or a stale bundle | Refuses to start | Exit 2 |
| Suite source changes mid-run | `tools/e2e/**` or `tools/*.mjs` digest differs between start and end | Verdict `IdentityChanged` | Exit 2 |
| Foreign headless Chrome before Phase S | Another agent's ad-hoc run | Phase S waits up to 10 min, then the verdict is `Incomplete(ForeignChrome)` with the PIDs | Never kills foreign processes |
| Tier 2, element change | `elements/Counter/CounterElementDeclaration.cs` | Own suite, suites that place `counter`, port-partner primary suites, and the smoke pair, each with a one-line reason | — |
| Tier 2, shared or unknown | `engine/gpu/**`, workers, clock, wire, driver, runner, an `AllInteraction` file, or an unmatched path | `Mode: Full`, with the path and rule as the reason | — |
| Tier 2, Workshop area | `ui/WorkshopBenchPlacement.cs` | The suites declaring `BenchPlacement`, plus the smoke pair | A file not in the area table is `Unknown`, so Full |
| Tier 2, docs only | `docs/**`, `*.md` | The smoke pair only | — |
| Checkpoint | `checkpoint --commit <sha> --out D` | Fresh detached worktree → publish → that worktree's runner runs `full` on a free port | A missing runner at that commit fails with a typed message |

### Decisions

- **D1. The runner is written in C#, as `tools/E2eRunner` plus `tools/E2eRunner.Tests`, both in `CuriousContraptions.slnx`.** Reasons:
  - AGENTS.md says "Use C# wherever possible", and closed sets must be enums end to end.
  - Every sibling tool is C#: Preview, Coverage, Ownership, LifecycleContract, WireContract and BundleElements.
  - The repo has no TypeScript compiler. tsx strips types without checking them, so a "typed" TypeScript declaration would be typed in name only. The C# declaration is compiled under the Story 9R.0 analyzer gate (`TreatWarningsAsErrors`).
  - `dotnet test CuriousContraptions.slnx` already runs in Tier 1, so the detector test runs in Tier 1 automatically.
  - The project links `engine/PartId.cs` (`Compile Include … Link`, as `tools/Coverage` links Ownership files) and references `CuriousContraptions.Abstractions` for `SimulationDomain`. The test project pins the same xunit versions as `Coverage.Tests`.
- **D2. Each worker is a separate process with a separate Chrome, all sharing one stateless preview.**
  - Browser contexts inside one shared browser are rejected. They share a GPU process and renderer scheduling, and one crash would take down every worker. The driver also owns `chromium.launch`.
  - `tools/Preview` is a read-only static file server that strips `If-None-Match` and `If-Modified-Since`, so concurrent clients cannot share server state.
  - IndexedDB lives in each Chrome's temporary profile, so it is isolated per process.
- **D3. The simulation cannot silently slow down under contention, so no runtime rate guard is added.**
  - `WorkshopTickLoop` (`engine/gpu/WorkshopTickLoop.cs:31-71`) catches up to the master clock's due tick. Debt beyond 100 ms stops the world, with `CCGPU_DEBT_FAULT` logged at `CuriousContraptions.Simulation/Program.cs:331`.
  - So inside a wall-clock window, contention shows up only as read latency (≤ 100 ms), fewer distinct samples, or an explicit debt fault. It never shows up as quietly less simulated time.
  - Two consequences:
    - Suites that assert a sample or publication count over a wall window are timing-sensitive (criterion T5 below).
    - The runner counts `CCGPU_DEBT_FAULT` per file. Calibration requires zero debt faults at the chosen N.
- **D4. The serial criterion.** A suite is `SerialAlone` if, anywhere in its local import closure (see the `TimingDetector.cs` task), it meets any of these:
  - **T1, InjectsStall:** a busy-wait loop (rule R1).
  - **T2, InjectsLoad:** it spawns processes or threads (R3).
  - **T3, TampersClock:** it reassigns or redefines a clock (R2).
  - **T4, MeasuresWallTime:** it stores, returns, logs or asserts a wall-clock difference (R4).
  - **T5, AssertsWallClockRate:** it asserts a lower bound on the count of samples, publications or reads collected over a wall-clock window. A presence check is not T5: it asserts that each of a known set of targets published at least once, so its bound equals the number of targets and does not grow with window length. For example, `cat-002.test.ts:87` checks `>= 2` for the detector and the lamp. anim-1c's `>= 3` samples of one target is T5.
  - **T6, OwnerNamed:** clock-master, cat-017 and cat-023b (owner decision, 19 Oct 2026).

  Rules R0–R4 are detected mechanically. T5 is declared, with a justification string and a line reference, and is checked empirically during calibration.
- **D5. Provisional classification, verified line by line in the implementer's audit table (Implementation Notes).**
  - **SerialAlone:**
    - clock-master: T1, T3, T4, T6.
    - cat-017: T4, T5 (`samples.length >= 20/30`), T6.
    - cat-023b: T6. Its 4000 ms negative windows are owner-named.
    - cat-037: T4. It asserts `s.t < 2200` and `s.t < 400` (`:290,395,420`).
    - cat-014: T4 (`:159` paces by elapsed wall time).
    - anim-1a: T5 (`>= 15` samples in 500 ms, `:155,159`).
    - anim-1b: T5 (`>= 10` samples over a 1 s delay, `:100`).
    - anim-1c: T5 (`>= 3` samples in 0.5 s, `:83`).
    - cat-023a: T5 (`>= 10` reads in a second, `:133`).
  - **Parallel:** every other suite, after this story deletes the dead wall-clock `t` and `time` sample fields (see Deletes). That leaves 25 of 34 files.
  - Serial sum ≈ 413 s and parallel sum ≈ 1,163 s (candidate-5 per-file durations; `palette-placement` is new and unmeasured).
  - Expected full run at N ≈ 5 with contention ≈ 413/60 + max(140 × 1.3, 1163 × 1.3 / 5) / 60 ≈ 12 min, against 26 min serially. Rewriting serial suites to measure simulated time is out of scope; it is recorded as a follow-up for the owning stories.
- **D6. Connected suites come from typed per-suite declarations. Bundle ports only name partners.**
  - The bundle's port declarations carry only domain and direction, so port compatibility is dense. Measured on `parts/elements.bundle.json`, the number of partners per element is:

    | Element | Partners |
    |---|---|
    | counter, latch | 9 |
    | battery, pressure_plate, lamp | 7 |
    | clock | 5 |
    | bumper, powered_gate | 4 |
    | domino, ball_detector | 3 |
    | ball, ramp, wall | 0 |

  - Selecting every suite that places any partner would therefore collapse Tier 2 into Tier 3 for logic elements. Instead:
    - **integration** = suites whose declared `Places` contains E;
    - **connected** = the *primary* suite of each port partner of E, at most one per partner.
  - Parts without packages (pipe, pipe_bend_45, pipe_bend_90, funnel, delay, switch, basket, bowling, spring) have no port declarations. Their selection is the own suite plus the suites that place them. Their behaviour lives under `engine/**`, which selects the full run anyway.
- **D7. Bundle identity covers the full tree.**
  - Identity is the SHA-256 of the sorted `relative-path\tsha256` manifest of every file under `CuriousContraptions.web/AppBundle`.
  - "Served" means every manifest path fetched from the runner's own preview has the same hash. Dot-files are listed as `NotServed`, because `PhysicalFileProvider` excludes them.
  - A `FileSystemWatcher` on the bundle aborts the run on any event. The end-of-run re-hash plus a second served check is authoritative.
  - Source identity is also recorded: HEAD, porcelain status digest, and a digest of `tools/e2e/**`, `tools/*.mjs` and `tools/E2eRunner/**`.
- **D8. A cross-worktree Chrome slot budget.**
  - The lock directory is `$(git rev-parse --git-common-dir)/e2e-runner/`, which every worktree of the repo shares.
  - It holds K = measured N slot files plus one `serial-intent` file. Each is held with an exclusive OS file lock, which the kernel releases if the process dies.
  - A Parallel file or a checkpoint `dotnet publish` holds one slot.
  - A SerialAlone file first takes `serial-intent`, which stops new slot grants to every runner, then all K slots.
  - So a background checkpoint can never overlap a foreground timing suite, and the machine never runs more than N runner Chromes.
  - Tier 1 commands run outside the runner (`dotnet build/test`) are not governed by the lock. Instead, the 1-minute load average is sampled before and after each SerialAlone file. If it exceeds the measured idle load plus 2, the observation is `Contended`: the file is re-run once the machine is quiet, and both records are kept. A contended **failure** is still a finding for the reviewer to triage; it is never erased.
- **D9. N is measured by `calibrate`, which is run once and its result recorded.**
  - The parallel set runs at N ∈ {1, 2, 4, 6, 8}, capped at ⌊logical CPUs / 2⌋ and by memory.
  - Each trial records wall time, per-file duration against N = 1, total `CCGPU_DEBT_FAULT`, peak summed RSS of the runner's process trees (sampled every 2 s) and pass/fail.
  - N is the largest value that meets all of these:
    - 100% pass;
    - zero debt faults;
    - peak RSS ≤ 50% of physical memory;
    - no file slower than 2× its N = 1 duration;
    - at least 10% wall-time gain over the next lower N.
  - The result is checked in as the typed constant `MeasuredWorkers` (`tools/E2eRunner/MeasuredWorkers.cs`). It holds `Workers`, `LogicalCpus`, `MemoryBytes`, `ChromeVersion`, `Date` and the evidence path.
  - On a machine whose CPU or memory fingerprint differs, the runner refuses `Parallel` until it is recalibrated. `--workers 1` is always allowed.
  - The N = 1 trial plus Phase S is the like-for-like serial baseline at the same identity. The historical baseline is 26 min 17 s.
- **D10. One preview URL.**
  - `WorkshopDriver.launch` reads the required `E2E_PREVIEW_URL` and fails fast if it is missing.
  - The `baseUrl` option, the five per-suite URL variables and every `8060` default are deleted. A suite can therefore never silently hit another agent's stale 8060 preview.
  - The runner starts and owns its own preview on a free port: `dotnet run --project tools/Preview -- <bundle> <port>`. If the port is lost to a race, it retries with a new port, up to 3 times. It verifies that it is talking to its own process using the served-identity check.
- **D11. The checkpoint runs the runner at the checkpoint's own commit.** The suite catalogue must match that commit's `tools/e2e/`. The outer `checkpoint` verb therefore only does the following:
  1. creates `.claude/worktrees/e2e-checkpoint-<sha12>` with `git worktree add --detach`, refusing if the path already exists;
  2. runs the README's `dotnet publish CuriousContraptions.web` there, holding one slot, and **releases that slot before step 3**, because the inner runner's Phase S needs all K slots and would otherwise deadlock;
  3. runs `dotnet run --project tools/E2eRunner -- full --out <D>` inside that worktree, holding no slot itself;
  4. copies the worktree's `.anvil/e2e-runs/<runId>/` into `D/anvil/` before any removal. cat-015b, cat-062 and cat-062b must keep their evidence directories inside their own working directory's `.anvil`.

  `D` is an absolute path outside the worktree. On Pass the worktree is removed after step 4; on Fail it is kept for diagnosis, and its path is reported. A commit that does not contain `tools/E2eRunner` fails with `CheckpointUnsupportedCommit`.

**Deletes** (the legacy this story removes):
- the scratch per-candidate gate scripts that serialise every file (Code Map, "Legacy scripts");
- the five per-suite preview-URL variables, `PLAYWRIGHT_BASE_URL`, the driver's `baseUrl` option and every `8060` default;
- clock-master's screenshot default inside committed evidence;
- the dead wall-clock `t` and `time` sample fields in the Parallel suites;
- every "all suites serially" and "cumulative twice" wording in active docs (Code Map, "Active wording to change").

The tracked copies in `evidence-9-4b/` stay unless OD1 decides otherwise.

</frozen-after-approval>

## Code Map

Line references are from worktree HEAD `d87164dc` (Story 9R.0c), which has 34 suite files.

- **`tools/e2e/workshop-driver.ts`:**
  - `launch` (`:356-385`): `baseUrl` default `'http://127.0.0.1:8060/'` at `:357`; Chrome launch flags at `:363-369`; a non-persistent `newContext` at `:370`.
  - `shutdown` (`:410-430`): the `CLOCK_DERIVATION_LOG` append.
  - `run`, `reset` and `holdUntil` (`:845-866`): deadline arithmetic, `deadline - Date.now()`, which is not R4.
  - Playwright comes from the global `@playwright/mcp` install (`:11-12`).
- **Per-suite preview variables:**
  - `cat-015a.test.ts:7` and `cat-015a-sidekick.test.ts:6`: `BUMPER_PREVIEW_URL`.
  - `cat-015b.test.ts:13`: `BUMPER_PREVIEW_URL`; `:24-26` defines the evidence directory inside `.anvil`, created with `mkdirSync`.
  - `cat-005.test.ts:9`: `BATTERY_PREVIEW_URL` with a `PLAYWRIGHT_BASE_URL` fallback.
  - `cat-062.test.ts:10-11`: `SPRING_PREVIEW_URL` and `SPRING_EVIDENCE_DIR`.
  - `cat-062b.test.ts:13-16`: `PRELOAD_*`.
  - `clock-master.test.ts:13`: `CLOCK_PREVIEW_URL`; `:15` defaults the screenshot into the committed `evidence-9-4b/` directory.
- **Detector ground truth:**
  - R1 and R2 in `clock-master.test.ts:41,59,265,344,348`.
  - R4 hits:
    - cat-017 `:183,226,305`;
    - cat-037 `:180`;
    - cat-014 `:159`;
    - clock-master `:91,93,212,214`;
    - the dead sample fields in cat-015a `:85`, cat-048a `:63,115,169,210`, cat-020 `:207,275,342,419`, cat-030 `:128,195,259`, cat-051 `:119,182,233,293`, cat-052 `:162,206,252,309` and engine-core-2a2 `:86`.
- **Parts mapping:**
  - `engine/PartId.cs`: the static members, e.g. `Basketball = "ball"` and `Receiver = "basket"`.
  - The driver's `selectTool` union (`workshop-driver.ts:574`). Its tool kinds `basketball` and `receiver` differ from the PartId values.
  - `elements/*/` (13 packages). `tools/BundleElements/Program.cs` writes `parts/elements.bundle.json`, which includes `GeneratedAt`.
  - `parts/catalog/*.tres` and `content/puzzles.json` (levels with `id` and `parts`).
- **Preview:** `tools/Preview/Program.cs` takes `[directory [port [base-path/]]]` and binds `127.0.0.1:{port}`. It is not in the slnx.
- **Contention signals:** `engine/gpu/WorkshopTickLoop.cs` and `CuriousContraptions.Simulation/Program.cs:325-336` (`CCGPU_DEBT_FAULT`).
- **Legacy scripts:**
  - Scratch, in the session scratchpad `…/scratchpad/`: `gates/gate-cumulative.sh`, `gate-cumulative-tap.sh`, `gate-candidate-4-all.sh`, `gate-candidate-5-all.sh`, `gate-candidate-6-tier2.sh`, `gate-suite-n.sh`, `gate-clock-master.sh` and `gate-023b.sh`; the root `gate-cumulative.sh` and `gate-023b.sh`; and `murdoch2-9-4b/run-chrome-gates*.sh`.
  - Tracked in `evidence-9-4b/`: `diagnosis/probes/gate-cumulative.sh` and `review*/mutations/run-chrome-gates*.sh` (see Open decisions OD1).
- **Active wording to change:**
  - `autonomous-development-guide.md:66,134,142,175,212`.
  - `epics.md:51` (NFR-06), `:62` (Global DoD 4) and `:95` (the scope rule's "full serial cumulative Chrome run").
  - `tools/e2e/README.md:3-16`.
  - Stories that are already done keep their historical AC wording.

## Tasks & Acceptance

**Execution** (test first):

- [ ] **`tools/E2eRunner/E2eRunner.csproj`, `tools/E2eRunner.Tests/E2eRunner.Tests.csproj` and the slnx entries** (Editor|* builds them in Debug, as Coverage does).
- [ ] **`SuiteCatalogue.cs`.** The typed declaration:
  - `enum SuiteId` has one member per file.
  - `enum SuiteTiming { Parallel = 1, SerialAlone = 2 }`.
  - `enum TimingReason { InjectsStall = 1, InjectsLoad, TampersClock, MeasuresWallTime, AssertsWallClockRate, OwnerNamed }`.
  - `record SuiteDeclaration(SuiteId Id, SuiteTiming Timing, IReadOnlyList<TimingJustification> Reasons, PartId? Primary, IReadOnlySet<PartId> Places, PlacementScope Scope, IReadOnlySet<LevelId> Levels, IReadOnlySet<SimulationDomain> Wires, IReadOnlySet<WorkshopArea> Areas, bool Smoke)`.
    - `PlacementScope` is `{ Declared, EveryPalettePart }`; palette-placement uses `EveryPalettePart`.
    - `TimingJustification` is `(TimingReason, string Justification, int Line)`.
  - `FileName(SuiteId)` is one switch, at the file-name boundary.
  - `LevelId` is a strongly typed ID, validated against `content/puzzles.json` plus `free_workshop`.
- [ ] **`TimingDetector.cs`.** A lexical TypeScript scanner:
  - It strips comments and string literals, keeps the `${…}` code inside template literals, and recognises regex literals by the preceding token.
  - It scans the suite and its local import closure. Shared modules (`workshop-driver.ts`, `workshop-wire.ts`, `../*.mjs`) are scanned too, and any hit in them is attributed to every suite that imports them.
  - **Rules:**
    - **R0:** `evaluate`, `evaluateHandle`, `addInitScript` or `waitForFunction` called with a string or template first argument, or `new Function` / `eval(`, is an error in every suite, because that code cannot be scanned.
    - **R1:** a `while`, `do` or `for` loop whose condition reads `performance.now()` or `Date.now()` and which has no `await` anywhere in the loop statement (initialiser, condition, update clause or body). For example, `cat-005.test.ts:138` (`for (…; Date.now()<deadline; await driver.page.waitForTimeout(50))`) is a poll loop, not a stall; it is a negative-control case in `TimingDetectorTests`.
    - **R2:** an assignment to, or `Object.defineProperty` on, `performance.now`, `Date.now` or `Date`; or any use of `page.clock` or `context.clock`.
    - **R3:** imports of `node:child_process`, `child_process` or `node:worker_threads`; or `new Worker(` inside page code.
    - **R4:** `(performance|Date).now() - <expr>` that is not a direct operand of `<`, `<=`, `>` or `>=`.
  - Each hit carries the file, line, rule and code excerpt.
- [ ] **`ImpactRules.cs` and `Selection.cs`.** Tier 2 selection:
  - **Input** is either `--diff <base>` or `--changed <file>`.
    - `--diff <base>` combines `git diff --name-status <base>` (working tree, renames giving both paths) with `git ls-files --others --exclude-standard`.
    - `--changed <file>` must contain repo-relative paths; `..`, absolute paths and non-canonical case are rejected.
  - **Path classification.** Each path gets exactly one `ImpactKind`, and a test forbids overlapping rules:
    - **`NoBrowserImpact`:** `docs/**`, `_bmad-output/**`, `_bmad/**`, `.claude/**`, `.agents/**`, root `*.md`, `tools/e2e/README.md`, `CuriousContraptions.tests/**`, `tools/{Coverage,Coverage.Tests,Ownership,anvil,LifecycleContract,WireContract,TraceAllocations}/**`.
    - **`Suite(SuiteId)`:** `tools/e2e/<suite>.test.ts`.
    - **`SharedTestModule(path)`:** any other `tools/e2e/*.ts` or `tools/*.mjs`. It selects the suites whose import closure contains the module. The driver and wire are imported by every suite, so they select `Full(SharedDriverInfrastructure)`.
    - **`Element(PartId)`:**
      - `elements/<Dir>/**`, through the typed `ElementPackages` table (Dir → PartId), which a test checks against the bundle and the directory listing;
      - `parts/catalog/<id>.tres` and `parts/icons/<id>.png`;
      - `parts/scenes/<x>.tscn`, mapped to every PartId whose catalogue resource or bundle `ScenePath` references it.
    - **`ElementBundle`:** `parts/elements.bundle.json`. Base and head are diffed per element; a `GeneratedAt`-only change is `NoBrowserImpact`; a parse failure is `Full`.
    - **`Level(LevelId)`:** `content/puzzles.json`, diffed per level `id` between base and head.
    - **`WorkshopArea(area)`** (OD6): the remaining `engine/*.cs`, `engine/presentation/**`, `ui/**` and `scenes/**` paths.
      - Each file maps to exactly one typed `enum WorkshopArea` (for example `Palette`, `BenchPlacement`, `Gizmo`, `Connections`, `Configuration`, `LevelPicker`, `SaveLoad`, `RunReset`, `Presentation`, `AllInteraction`) through a typed path table. A test checks the table against `git ls-files engine ui scenes`.
      - Each suite declares the areas it exercises (`Areas`). An area change selects the suites that declare it, plus the smoke pair.
      - Files that every interaction passes through map to `AllInteraction`, which means Full: `ui/Workshop.cs` input dispatch, `engine/MachineWorld*.cs` and `engine/WorkshopUiBinding.cs`. A `.cs` file missing from the table is `Unknown`, so Full.
      - This is how Story 9R.0c (`engine/PartRegistry.cs`, `ui/WorkshopBenchPlacement.cs`) gets its Tier 2 selection: palette-placement plus the smoke pair, not Full.
    - **`Full(MaterialEngine | SharedRuntime | RunnerItself | Build | Unknown)`.** These are the guide's Tier 3 triggers:
      - `engine/gpu/**` (solver, compiler, wire, pose ring and schedule);
      - `CuriousContraptions.Simulation/**`, `CuriousContraptions.Animation*/**`, `CuriousContraptions.web/**` (workers, clock master, clock ring, client);
      - `CuriousContraptions.Abstractions*/**` (the declaration contracts the compiler consumes), `CuriousContraptions.FlatBuffers/**`, `schemas/**`;
      - the shared driver and test infrastructure, `tools/BundleElements/**`, `tools/Preview/**`, `tools/E2eRunner*/**`;
      - `*.csproj`, `*.slnx`, `Directory.Build.*`, `global.json`, `project.godot`, `export_presets.cfg`;
      - any unmatched path (`Unknown`).
  - **Selected suites.** Each reason line is `suite<TAB>reason`:
    - own: `Primary == E`;
    - integration: `Places` contains E, or `Scope == EveryPalettePart`;
    - connected: the primary suite of each port partner, with the reason naming the domain and the two ports;
    - level: the suite declares the changed level;
    - shared module: the suite's import closure contains it;
    - smoke: `engine-core-2b1` and `engine-core-2a1`.
  - **`Mode: Full`** whenever any path is `Full`. The output also says whether Tier 3 is required.
- [ ] **`BundleIdentity.cs`, `SourceIdentity.cs` and `PreviewHost.cs`:** the manifest, the served check, the watcher behind an injected interface, and the owned preview on a free port (D7, D10).
- [ ] **`Toolchain.cs`.** Resolves and records the toolchain, and fails fast if any part is missing:
  - tsx `4.23.15`, by scanning `~/.npm/_npx/*/node_modules/tsx/package.json` for that exact version, and failing with the `npx --yes --package=tsx@4.23.15 tsx --version` hint if absent;
  - `/opt/homebrew/lib/node_modules/@playwright/mcp`, and its `playwright` version;
  - `node --version`;
  - the Chrome version, from the Chrome binary's `--version`.
- [ ] **`SuiteProcess.cs`.** Starts one suite process:
  - **Command:** `node --import <tsx loader> --test --test-concurrency=1 --test-timeout=150000 --test-force-exit --test-reporter=tap --test-reporter-destination=<D>/<suite>.tap tools/e2e/<suite>.test.ts`, with stdout and stderr going to `<D>/<suite>.log`.
  - **Environment.** The process gets an explicit environment: PATH, HOME and TMPDIR are inherited, plus:
    - `E2E_PREVIEW_URL`;
    - `CLOCK_DERIVATION_LOG=<D>/<suite>.derivations.jsonl`;
    - `CLOCK_EVIDENCE_SCREENSHOT=<D>/clock-master.simulation-unresponsive.png` (a file path);
    - `CAT015B_EVIDENCE_DIR`, `PRELOAD_EVIDENCE_DIR` and `SPRING_EVIDENCE_DIR`, set to new paths under `.anvil/e2e-runs/<runId>/`. Only the parent directory is created, because suites `mkdir` the leaf themselves.

    Every `E2E_*`, `CLOCK_*` or `*_PREVIEW_URL` variable from the parent shell is dropped.
  - **Wall limit:** 15 min per file. On timeout, the runner calls `Process.Kill(entireProcessTree: true)`, then checks that no descendant survives.
- [ ] **`TapResult.cs`.** Parses the TAP into these per-file columns, which keep the candidate-5 column names:
  - `exit`, `tests`, `pass`, `fail`, `cancelled`, `notok`;
  - `closeFindings`, `unresponsive`, `rejected`, `recal`, `renewedLate`, `lateRenewalsReported`, `wideOffset`;
  - `debtFaults` (new);
  - `durationMs`, `phase`, `slot`, `start` and `end`.
- [ ] **`ChromeSlots.cs` and `Scheduler.cs`:**
  - D8's lock.
  - Phase S runs first: SerialAlone files in catalogue order, each holding all slots, with the foreign-Chrome check and the load sampling.
  - Phase P then runs longest first, using the durations from the last summary passed with `--durations <summary.json>`, or alphabetical order without it.
  - Run order is always Phase S, then Phase P.
- [ ] **`RunSummary.cs`.** The summary records:
  - the run header: runner source digest, source identity, bundle identity before and after, the served check, toolchain versions, N, the machine fingerprint, the selection with reasons, and the wall time;
  - per-file rows;
  - a `Verdict` enum: `Pass`, `Fail`, `Incomplete`, `IdentityChanged` or `Aborted`.

  Exit codes: 0 Pass, 1 Fail, 2 any other verdict.
- [ ] **`Program.cs` verbs.** One subcommand dispatch, with an enum for the verb:
  - `full --out D [--workers k] [--durations S]`
  - `select (--diff B | --changed F)`
  - `run (--diff B | --changed F) --out D`
  - `checkpoint --commit SHA --out D`
  - `calibrate --out D`
- [ ] **`MeasuredWorkers.cs`.** Written from the calibration evidence (D9).
- [ ] **`tools/e2e/workshop-driver.ts`.** `launch` reads the required `E2E_PREVIEW_URL`. Delete the `baseUrl` option and the 8060 default.
- [ ] **The suites:**
  - Delete the per-suite URL constants listed above, and `PLAYWRIGHT_BASE_URL`.
  - `clock-master.test.ts`: `CLOCK_EVIDENCE_SCREENSHOT` becomes required, with no default inside committed evidence.
  - Delete the unused wall-clock `t` and `time` fields in cat-015a, cat-048a, cat-020, cat-030, cat-051, cat-052 and engine-core-2a2. Where a log needs ordering, use the pose `sequence` instead. If the reviewer finds any of these fields read by an assertion, that suite becomes SerialAlone (T4) instead.
- [ ] **Docs:**
  - `tools/e2e/README.md`: the runner invocation for full, Tier 2 and single-suite runs; `E2E_PREVIEW_URL`; the timing catalogue; and how a new suite is declared.
  - The guide and epics lines listed in the Code Map: "Tier 3 full run by `tools/E2eRunner`" replaces "all suites serially" and "cumulative twice".
  - Delete the scratch gate scripts.

**Acceptance Criteria → test plan:**

| AC | Proof | Kind |
|---|---|---|
| **AC1.** One runner, one process per file, per-file TAP, exit code and close findings, one summary. SerialAlone files run alone; Parallel files run on N workers; N is measured and recorded | `SuiteProcessTests` use fixture `.test.ts` files with no browser, under `tools/E2eRunner.Tests/Fixtures/`, run through the same tsx path (a missing tsx is a test failure, not a skip): pass, fail, close finding in `after` (exit 1, all ok), zero tests, hang (timeout and tree kill, where a child `sleep` stands in for Chrome), crash, env scrub. `SchedulerTests` replay a fake clock and assert that no SerialAlone interval overlaps any other interval and that concurrency never exceeds N. `CalibrationTests` cover the N-selection rule on recorded trial tables. Chrome: the `calibrate` evidence and `MeasuredWorkers.cs` | Tier 1 + Chrome |
| **AC2.** A typed timing declaration, and a test that catches an undeclared suite | `TimingDetectorTests`: each rule R0–R4 has a positive case; a negative control (poll loop with `await`; deadline `x - Date.now()`; a rule token inside a comment or string); a template `${}` case; a regex-literal case. `TimingDeclarationTests` on the real tree: (a) catalogue and files are a bijection; (b) a detector hit on a Parallel suite fails, naming the file and line; (c) the three owner-named suites are SerialAlone; (d) every SerialAlone suite without a detector hit carries T5 or T6 with a justification and line; (e) each suite's literal `selectTool`/`selectLevel` calls ⊆ its declared `Places`/`Levels`, through the tool-kind→PartId boundary table; (f) `ElementPackages` matches `elements/*` and the bundle | Tier 1 |
| **AC3.** Tier 2 selection with reasons; unknown or shared → full | `SelectionTests`, table-driven: counter declaration; `parts/scenes/ball.tscn` (shared scene); `content/puzzles.json` with one level changed; `GeneratedAt`-only bundle change; `tools/workshop-console.mjs`; driver; `engine/gpu/X.cs`; `ui/WorkshopBenchPlacement.cs` (area); `ui/Workshop.cs` (AllInteraction); a new `ui/` file missing from the table; unknown root file; docs only; empty set; rename out of `elements/`; deleted suite. Each expects an exact suite set, reason lines and mode | Tier 1 |
| **AC4.** Bundle identity recorded before and after; refuse on change | `BundleIdentityTests`: manifest determinism, dot-file `NotServed`. `PreviewHostTests` (real `tools/Preview`, temp bundle): served ≠ disk refuses; a port held by a foreign listener refuses; a file changed mid-run, with fixture suites that wait, aborts with `IdentityChanged`; a file changed and restored is still `IdentityChanged` (watcher); suite source changed mid-run gives `IdentityChanged` | Tier 1 |
| **AC5.** The parallel full run passes 100% on two consecutive invocations; wall time and serial baseline recorded; a seeded failure is reported against that suite alone | Two `full` runs on one bundle identity, recorded with wall times next to the N = 1 baseline and the historical 26 min 17 s. Seeded run: a temporary one-line `assert.equal(1, 2)` in cat-049 test 2 (Parallel). It shows exactly one `not ok` (cat-049 test 2), cat-049 `Fail`, and every other row identical in pass/fail to the unseeded run. The diff is recorded, reverted, and the source digest is shown restored. A seeded hang (`await new Promise(() => {})` in cat-048b) shows `TimedOut` against that file only | Chrome |
| **AC6.** Tier 3 checkpoint against a committed HEAD in a separate worktree and port, in the background | `checkpoint --commit <candidate commit>` runs while a foreground `run --changed` Tier 2 runs. Evidence: both summaries; the slot-lock timeline showing no foreground SerialAlone file overlapping any checkpoint Chrome or publish; distinct ports; the worktree removed on Pass. `CheckpointTests`: a commit without the runner fails with `CheckpointUnsupportedCommit`; an existing worktree path refuses | Chrome + Tier 1 |
| **AC7.** `dotnet build` 0 warnings, `dotnet test` 100%, Node harnesses pass | Tier 1 logs | Tier 1 |

## Implementation Notes

- Before the green step, record an audit table with one row per suite: the rule hits with lines, the T5 verdict with lines, the final timing, `Primary`, `Places`, `Levels` and `Wires`.
- Confirm that `node --test` forwards suite console lines into the TAP destination as comments. The candidate-5 scripts counted close findings from merged TAP and console output. If they are not forwarded, count from the `.log`, and say which in the README.

## Spec Change Log

Implementation decisions (Amelia-4, 19 Oct 2026). None changes the frozen intent; each fills a gap the implementation met.

- **Rule precedence.** A `Full` rule wins over every other rule, so a build file inside a package (`elements/Counter/Counter.csproj`, `CuriousContraptions.tests/*.csproj`) is `Full(Build)`. Any other overlap is forbidden: `SelectionTests.RuleTableResolvesEveryTrackedPathWithoutOverlap` resolves every tracked path. `docs/**` and root `*.md` do not overlap (root means no `/`).
- **Area table.** Every non-`engine/gpu` file under `engine/`, `ui/` and `scenes/` is in `ImpactRules.AreaFiles`; a `.cs.uid` sidecar follows its source. `AllInteraction` (Full) also covers `engine/MachinePart.cs`, `ui/WorkshopGpu.cs` (command submission), `ui/WorkshopUiFailure.cs` and `scenes/workshop.tscn`. A `Guidance` area holds `ui/WorkshopGuidance.cs` (fine rotate, hints). `FullReason` gains `AllInteraction`, and `tools/{BundleElements,Preview}/**` report `SharedDriverInfrastructure`.
- **Areas mean the area a suite proves.** Every suite selects tools and places parts, so declaring `Palette`/`BenchPlacement` on all of them would turn every area change into Full. Only palette-placement declares them (the 9R.0c selection the spec asks for). The other areas follow the driver calls a suite makes (`DriverVocabulary.MethodAreas`, checked by test (e)) plus raw-click wiring and configuration by review.
- **Content-mapped parts.** A catalogue resource maps by its `Id` line (`springboard.tres` is `spring`); a scene maps to every part whose catalogue `ext_resource` or bundle `ScenePath` names it (`ball.tscn` is ball, balloon and tennis). A path that resolves to no part is `Full(Unknown)`.
- **Removed and empty inputs.** A removed suite file or shared module is `Full(Unknown)` (the catalogue or importers must change with it). An empty change set selects the smoke pair. A `--changed` list diffs content against HEAD.
- **node 26 TAP facts.** Each file's console output is forwarded into its TAP as `#` comments (the runner counts from the `.tap`; the `.log` holds node's own output). A file that declares no test still reports one synthetic test point named after the file, so `tests` excludes that entry before the `NoTests` rule applies. A root-hook failure or a non-zero exit adds a synthetic `not ok`; the exit code stays authoritative, and the clean-TAP-with-exit-1 case is covered by a recorded TAP (M3).
- **tsx.** Two npx caches hold tsx 4.23.15; the runner takes the first by ordinal path and records it.
- **OD7 as implemented.** `tools/workshop-close-findings.mjs` owns the typed `SessionFault` set and `sessionFaultFindings`; the driver now records every `CCGPU_DEBT_FAULT` and fails the session on it, as it already did for rejected replies and undeclared recalibrations. `WorkshopDriver.closeExpecting(fault)` declares the one fault a test provokes and fails at once if it is absent or any other fault occurred. Measured in Chrome: the 4000 ms spin while Running emits no debt fault (the `SimulationUnresponsive` retirement ends the session first), so it stays unchanged in Run and declares nothing. The debt-fault fixture is a new clock-master test: a 600 ms block while Running (inside the 1000 ms liveness bound) takes exactly one declared debt stop.
- **Observed signals.** The driver prints `E2E_OBSERVED <signal> <record>` for debt faults, recalibrations, rejected replies, late renewals, wide startup offsets and `SimulationUnresponsive`, so every column the runner counts exists in every suite's TAP.
- **One more dead field.** `cat-062.test.ts:52` held an unread `time: Date.now() - started` sample field (R4); it is deleted with the listed ones.
- **Scope cut (coordinator, 19 Oct 2026, during implementation).** Removed from this story to one follow-up story: the second parallel proof run, the background checkpoint verb and its worktree, signal handling, and the cross-worktree slot lock (D8's slots and `serial-intent`; replaced by one exclusive `<git common dir>/e2e-runner.lock` held for the whole run, so only one runner runs at a time). Added: suites run serially unless the catalogue marks them Parallel, and an uncalibrated machine runs everything serially; a changed element with no Primary suite is a typed `NoPrimarySuite` error; skipped and todo tests fail the file; the runner publishes `CuriousContraptions.web` itself before it records the bundle identity. The checkpoint and slot-lock text above (D8, D11, AC6) describes the follow-up, not this story. AC5's seeded failure and seeded hang Chrome runs are not on the finish-only list and were not run (pending the coordinator's confirmation); the seed diffs are in `evidence-9r-0a/seeded/`.
- **Preview logging.** The runner starts its preview with `Logging__LogLevel__Default=Warning`, so `preview.log` holds warnings, errors and the start line rather than one line per request.
- **Calibration result.** N = 2 (`calibration/calibration.md`): N = 1 and 2 passed; at N = 4 a debt fault (cat-020) and a clock recalibration (cat-048a) failed two files, N = 6 and 8 failed too. The measured solo load (5.23, the highest 1-minute load after a SerialAlone file ran alone) is `IdleLoad`, so `Contended` means above 7.23.
- **Active wording.** Besides the guide and epics lines in the Code Map, `architecture.md` and `prd.md` carried the same "cumulative ... serially" rule and now name the runner's tiers.

## Review Triage Log

## Design Notes

**Red → green → mutation.**
1. **Red first:** the detector rule tests, the declaration tests against an empty catalogue, the selection table, the TAP verdict fixtures and the identity tests.
2. **Green:** implement them.
3. **Mutants.** Each one must turn a named test red:

| # | Mutant | Killed by |
|---|---|---|
| M1 | Drop R1 | Synthetic busy-loop case |
| M2 | Move cat-037 to `Parallel` | `TimingDeclarationTests` (b) |
| M3 | Pass a file on TAP alone, ignoring exit | `after`-hook close-finding fixture |
| M4 | Treat zero tests as a pass | `NoTests` fixture |
| M5 | Scheduler places SerialAlone in Phase P | Overlap assertion |
| M6 | Selection drops connected partners | Counter row |
| M7 | Unknown path classified `NoBrowserImpact` | Unknown-root-file row |
| M8 | Watcher disabled | Change-and-restore test |
| M9 | End-hash compare removed | Test that changes a bundle file with the injected watcher silenced |
| M10 | Kill node only, not the tree | Orphan check |
| M11 | Environment not scrubbed | Env fixture shows `E2E_SLOWMO` present |
| M12 | SerialAlone without `serial-intent` | Two-runner fixture shows overlap |
| M13 | R4 also flags relational operands | Poll-loop negative control |

**Risks.**
- **CPU contention skewing timing.** Godot WASM, the .NET-interpreted Simulation worker (3–4 ms per tick in Chrome, Story 9.4b) and the Animation worker each take roughly 0.5–1 core per Chrome, plus a GPU process. Mitigations:
  - timing suites run alone under the cross-worktree lock;
  - load sampling marks contended observations invalid;
  - N comes from measurement with a 2× duration cap and zero debt faults;
  - D3 rules out silent simulation slowdown.
- **Godot WASM memory.** Each Chrome holds the Godot heap plus two .NET runtimes, each with its own `SharedArrayBuffer`. Calibration caps N at 50% of RAM by measured peak RSS, so macOS memory compression cannot stall timing work.
- **GPU contention.** N headless Chromes share one GPU. A lost context surfaces as a page error, which every suite already fails on. Calibration catches it at the chosen N.
- **Shared preview state.** The preview is stateless and read-only. Each Chrome profile is temporary. Every evidence path is per run and per suite. The derivation logs are per suite, not shared appends. The only shared mutable state left is the slot lock, which uses kernel locks.
- **Cold start.** N simultaneous cold page loads of a bundle of tens of MB from one Kestrel instance over loopback. `waitForReady` allows 120 s. Calibration records ready time, and the longest-first order staggers starts.
- **Checkpoint publish in a fresh worktree** may need state that is not tracked (for example the Godot import cache). The proof shows a clean-worktree publish. If it fails, it fails fast and typed, with no implicit copying from the main checkout.

## Design review (adversarial, edge-case hunter)

| # | Attack | How the spec handles it |
|---|---|---|
| A1 | Stall code hidden in a string passed to `page.evaluate` | R0 forbids string-form page code in all suites. None exist today |
| A2 | Timing helper moved into the driver or a `.mjs` module | The detector scans the import closure and attributes shared-module hits to every importing suite |
| A3 | Rule token inside a comment or string triggers a false positive | The scanner strips comments and strings; negative-control tests cover this |
| A4 | Code inside a template `${}` evades the scanner | `${}` is scanned as code; a dedicated test covers it |
| A5 | Wall difference rewritten as deadline arithmetic (`REST - (now - t)` → `(t + REST) - now`) to evade R4 | R4 is a tripwire, not a proof. T5 and calibration (2× duration, debt faults) are the empirical backstop. The reviewer audits each rewrite against D4's criterion, not the rule |
| A6 | Rate assertion (T5) undetectable | Declared with a line and justification. Test (d) forbids a SerialAlone suite without a reason, and the audit table is reviewed line by line. anim-1b, anim-1c and cat-023a are classified explicitly |
| A7 | New suite added without a declaration | Bijection test in Tier 1; the runner refuses to start |
| A8 | Suite deleted or renamed but still declared | Same bijection |
| A9 | Two runners (foreground plus checkpoint) exceed N, or overlap a timing suite | The cross-worktree slot lock in the git common dir; `serial-intent` gives writer preference |
| A10 | Runner crash leaves the lock held | Kernel file locks are released on process death |
| A11 | Runner crash orphans Chrome | Pre-flight foreign-Chrome check before Phase S; tree kill on timeout and SIGINT/SIGTERM |
| A12 | Ad-hoc `node --test` by another agent during Phase S | Foreign headless Chrome detection waits, then marks the run `Incomplete`; load sampling also marks `Contended` |
| A13 | Tier 1 `dotnet build` started by the implementer during the checkpoint's Phase S | Load sampling gives `Contended`; the re-run keeps both records; a contended failure is still triaged |
| A14 | Stale 8060 preview from another agent serves an old bundle | No 8060 default; the URL is required; the runner owns its preview and verifies served = disk across the full manifest |
| A15 | Port race between the free-port probe and the preview bind | Retry with a new port up to 3 times; the served-identity check proves which process answered |
| A16 | `dotnet publish` mid-run in the same checkout | Watcher aborts with `IdentityChanged`; the end re-hash is authoritative |
| A17 | Another agent edits `tools/e2e/*.ts` in the main checkout mid-run | Suite source digest compared at start and end gives `IdentityChanged`; recommend running gates from the story's own worktree |
| A18 | Bundle file replaced with identical bytes | Watcher still aborts (conservative). Accepted: false aborts cost a rerun, never a false Pass |
| A19 | A dot-file in the bundle is never served | Listed as `NotServed` in the manifest, not a mismatch |
| A20 | Test-order dependency inside a file (cat-023b test 4 on test 1) | Files are never split; `--test-concurrency=1` within a file |
| A21 | IndexedDB collision between workers | Each Chrome has a temporary profile from `chromium.launch` plus a non-persistent context |
| A22 | Evidence directory collision (cat-062 and cat-062b use `Date.now()` names; clock-master screenshot defaults into committed evidence) | The runner sets every evidence variable to per-run, per-suite paths; the clock-master default is deleted |
| A23 | `CLOCK_DERIVATION_LOG` appended by parallel processes | One file per suite |
| A24 | Leaked `E2E_SLOWMO` or `E2E_HEADED` from the shell changes timing | Explicit environment; scrub test M11 |
| A25 | File passes with all `ok` lines but a close finding in an `after` hook | Exit code authoritative; fixture M3 |
| A26 | Import error gives zero tests and exit 0 | `NoTests` is `Fail` (M4) |
| A27 | Hung file blocks a worker forever | 15-min wall limit with tree kill |
| A28 | Retry into green | No automatic retry of failures. Only a `Contended` observation is re-run, both records are kept, and a contended failure is still a finding |
| A29 | Implementer hands `select` a trimmed `--changed` list | The summary records the changed list and base. The reviewer recomputes with `--diff <base>` on the exact candidate and compares |
| A30 | Generated bundle JSON changes on every build (`GeneratedAt`) and forces Full every time | Per-element diff; a `GeneratedAt`-only change is `NoBrowserImpact` |
| A31 | A scene shared by several parts (`ball.tscn` serves ball, tennis and balloon) | A scene maps to every PartId referencing it |
| A32 | Element directory renamed | Not in `ElementPackages`, so `Unknown` → Full, and test (f) fails |
| A33 | Case-variant or `..` paths on case-insensitive APFS | Rejected at the CLI boundary; `--diff` uses git's spelling |
| A34 | Dense port graph turns Tier 2 into Tier 3 | D6: integration from declared `Places`; connected limited to partners' primary suites. Sizes recorded |
| A35 | `Places` declaration rots | Test (e): literal `selectTool` and `selectLevel` calls ⊆ declared. Variable placements are covered by review and `EveryPalettePart` |
| A36 | Checkpoint commit predates the runner, or has a different suite set | The checkpoint runs that commit's own runner; a missing runner gives typed `CheckpointUnsupportedCommit` |
| A37 | Checkpoint worktree path left over from an earlier crash | Refuses an existing path; the summary names it for manual removal |
| A38 | Checkpoint publish skews foreground timing | Publish holds a slot, so it never overlaps a SerialAlone file |
| A39 | Machine changed (different CPU count or RAM) but stale N is used | Fingerprint mismatch refuses Parallel until recalibrated |
| A40 | Chrome auto-update changes behaviour between runs | Chrome version recorded per run; not enforced (it would make every run brittle). The reviewer compares versions when reusing runs |
| A41 | tsx missing, or a different version, after the npx cache is cleared | Exact-version resolution with a typed failure; no `npx` download mid-run |
| A42 | GPU context loss at high N | Page errors already fail suites; calibration selects an N with zero failures |
| A43 | Simulation slows silently under load and a negative window false-greens | Ruled out by D3: catch-up to the master clock, or an explicit `CCGPU_DEBT_FAULT` |
| A44 | A unit-test `.test.ts` placed in `tools/e2e/` is treated as a Chrome suite | Undeclared, so the runner refuses. Pure unit tests belong outside the top-level glob (see OD2) |
| A45 | Checkpoint deadlock: the outer process holds its publish slot while the inner runner's Phase S waits for all K | D11 step 2 releases the slot before step 3; a two-process fixture test covers it |
| A46 | Checkpoint worktree removal deletes `.anvil` screenshot evidence | D11 step 4 copies it into `D` first |
| A47 | Treating every `ui/**` or `engine/**` edit as Full makes almost every element story a full run | `WorkshopArea` table (OD6); only the guide's Tier 3 subsystems and the `AllInteraction` files are Full |
| A48 | A new `ui/` file is added without an area entry | Missing from the table, so `Unknown` → Full, and the table test fails |
| A49 | Suite `Areas` declaration rots | The reviewer audits it with the suite table. The rows are typed, so a stale area name does not compile |

**Open decisions for the coordinator:**
- **OD1. The tracked evidence scripts in `evidence-9-4b/`.** The story says to delete them. AGENTS.md says historical artifacts stay unchanged as evidence, and Story 9.4b's Pass cites them. Recommendation: delete the scratch copies, keep the tracked evidence copies, and remove the serial wording from active docs.
- **OD2. Story 9R.1's planned `tools/e2e/run-reset-snapshot.test.ts`** would be caught by the Chrome-suite glob. Recommendation: move it to `tools/e2e/unit/` and run it with the Node harnesses, and refresh the 9R.1 spec.
- **OD3. The checkpoint proof needs a commit that already contains the runner.** Recommendation: a local, unpushed candidate commit on a scratch branch, whose identity is recorded. The alternative is to prove AC6 after SnapshotApproval and the commit.
- **OD4. `E2E_PREVIEW_URL` is required, with no 8060 default.** This changes manual single-suite runs (see the README). Recommendation: accept it.
- **OD5. Story 9R.0b's AC asks for "two back-to-back serial Chrome runs".** That conflicts with deleting the "cumulative twice" wording. Recommendation: the epics scope rule maps it to one Tier 3 runner run, plus 9R.0b's own timing telemetry.
- **OD6. "Shared change → full run" needs a boundary.**
  - The story says an unknown or shared change selects the full run.
  - The guide's Tier 3 list is narrower: solver, workers, clock, pose ring, wire/ABI, compiler and shared driver/test infrastructure.
  - Story 9R.0c already treats `engine/PartRegistry.cs` and `ui/**` edits as Tier 2.
  - Recommendation: Full means the guide's list plus `AllInteraction` files and unknown paths. The other `engine/*.cs` and `ui/**` files go through the typed `WorkshopArea` table.
