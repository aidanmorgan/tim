# Workshop browser tests

Every suite runs with installed Chrome against the production preview: the bundle `dotnet publish CuriousContraptions.web` writes (no `PlaytestDiagnostics` property: every suite runs only against the production build; the PlaytestDiagnostics variant adds diagnostic recording and is never a gate). The read-only observations the suites decode (`CCGPU` acknowledgements, `CCGPU_RETAINED` energy and event commits, `CCGPU_CLOCK_RECALIBRATED` and `CCGPU_CLOCK_DERIVATION` telemetry, the `globalThis.Workshop*` read hooks) ship in the production build until Story 9A.24 replaces them with the typed observation port. Read-only observations supplement actual UI construction; they are not test control hooks.

## Running the suites: `tools/E2eRunner`

The runner (Story 9R.0a) publishes the production bundle itself (`dotnet publish CuriousContraptions.web`, logged to `<D>/publish.log`), then runs each `tools/e2e/*.test.ts` file in its own `node --test` process with its own Chrome, against its own preview on a free port, and writes one summary. It never retries a failed file into green.

```sh
dotnet run --project tools/E2eRunner -- full --out <D>                 # Tier 3: every suite
dotnet run --project tools/E2eRunner -- select --diff <base>           # print the Tier 2 selection, one reason per suite
dotnet run --project tools/E2eRunner -- run --diff <base> --out <D>    # run the Tier 2 selection (the full run when Mode: Full)
dotnet run --project tools/E2eRunner -- run --changed <list> --out <D> # the same for an explicit list of repository paths
dotnet run --project tools/E2eRunner -- calibrate --out <D>
```

- **Tier 2 selection.** A changed element selects its own suites, the suites that place it, the primary suite of each port partner and the smoke pair, each with a one-line reason. A changed element that no suite declares as its Primary is a typed `NoPrimarySuite` error. An unknown or shared change (the solver, workers, clock, wire, driver, runner, build files, the central interaction files, any unmatched path) selects the full run.
- **One suite.** `run --changed <list>` with a list holding the suite's own path (for example `tools/e2e/cat-049.test.ts`) runs that suite and the smoke pair `engine-core-2b1` and `engine-core-2a1`.
- **Output.** `<D>` must not exist: every run writes a fresh directory with `<suite>.tap`, `<suite>.log`, `<suite>.derivations.jsonl`, `preview.log`, `summary.txt` and `summary.json`. Exit codes: 0 Pass, 1 Fail, 2 any other verdict (`Incomplete`, `IdentityChanged`, `Aborted`) or a refusal.
- **Pass rule.** A file passes only when its exit code is 0, its TAP plan is complete, it ran at least one test, and it has no failed, cancelled, skipped or todo test and no `not ok` line. The exit code is authoritative: a session close finding raised in a hook fails the file even when its test lines are `ok`. A file that declares no test is `NoTests`. A file over its 15-minute wall limit is killed with its whole process tree (`TimedOut`), and the runner checks that no process it held survives.
- **Console lines.** `node --test` forwards each suite's console output into its TAP file as `#` comments, so the runner counts from the `.tap`; the `.log` holds the node runner's own output.
- **Identity.** The runner records the source identity (HEAD, the porcelain status digest, the digest of `tools/e2e/**`, `tools/*.mjs` and `tools/E2eRunner/**`) and the bundle identity: the SHA-256 of the sorted `path<TAB>sha256` manifest of every file under `CuriousContraptions.web/AppBundle`. It refuses to start unless every served file is byte-identical to the disk (dot-files are listed as `NotServed`). A file watcher on the bundle stops the run at the first change, and the end-of-run re-hash, second served check and suite-source digest are authoritative: any difference is `IdentityChanged`.
- **Serial by default; parallel workers.** Every suite runs alone unless the catalogue marks it `Parallel`. `SerialAlone` suites run first, one at a time (Phase S); the Parallel ones then run on N workers, longest first when `--durations <summary.json>` names an earlier run (Phase P). N is the calibrated `MeasuredWorkers.Current` (`tools/E2eRunner/MeasuredWorkers.cs`, N = 2 on the 16-CPU development machine); with nothing calibrated every run is serial, a machine with a different CPU or memory fingerprint refuses parallel runs until it is recalibrated, and `--workers 1` is always allowed.
- **One runner at a time.** Every Chrome verb holds an exclusive lock on `<git common dir>/e2e-runner.lock`, shared by every worktree; a second runner is refused (`RunnerBusy`), so a SerialAlone file never shares the machine with another runner's Chrome. Before each SerialAlone file the runner waits up to 10 minutes for any foreign headless Chrome (another agent's ad-hoc run) to finish, then reports `Incomplete(ForeignChrome)` with the pids; it never kills foreign processes. It also samples the 1-minute load average before and after each SerialAlone file: above the calibrated `IdleLoad + 2` the observation is `Contended` and the file runs once more when the machine is quiet; both records are kept, and a contended failure is still a failure.

Manual runs outside the runner need the preview URL: `E2E_PREVIEW_URL` is required by `WorkshopDriver.launch`, with no default port, so a suite can never silently test another agent's stale preview:

```sh
dotnet run --project tools/Preview -- CuriousContraptions.web/AppBundle 8123 &
E2E_PREVIEW_URL=http://127.0.0.1:8123/ CLOCK_EVIDENCE_SCREENSHOT=/tmp/clock-master.png \
  node --import <tsx 4.23.15>/dist/loader.mjs --test --test-concurrency=1 --test-timeout=150000 tools/e2e/<suite>.test.ts
```

The runner gives every suite an explicit environment: `PATH`, `HOME` and `TMPDIR` from the shell, `E2E_PREVIEW_URL`, a per-suite `CLOCK_DERIVATION_LOG`, `CLOCK_EVIDENCE_SCREENSHOT` in the run's output directory, and new `CAT015B_EVIDENCE_DIR`, `PRELOAD_EVIDENCE_DIR` and `SPRING_EVIDENCE_DIR` directories under `.anvil/e2e-runs/<runId>/` (the suites create the leaf and refuse an existing one). Every other variable of the shell (`E2E_SLOWMO`, `E2E_HEADED` and the rest) is dropped.

## The timing catalogue and declaring a suite

`tools/E2eRunner/SuiteCatalogue.cs` declares every suite file: its `SuiteId`, its timing (`Parallel` or `SerialAlone`) with typed reasons, its primary element, the parts it places, the levels it selects, the domains it wires and the Workshop areas it exercises. A new suite needs a `SuiteId` member, its `FileName` arm and its declaration; the runner refuses to start while any `tools/e2e/*.test.ts` file is undeclared. `TimingDeclarationTests` (in `dotnet test`) checks, on the real tree:

- the catalogue and the suite files are a bijection;
- no Parallel suite, or any module it imports, has a timing-detector hit: R1 a busy-wait loop (a wall-clock condition with no `await` in the loop), R2 a reassigned or redefined clock or `page.clock`/`context.clock`, R3 a child process or thread, R4 a stored, returned, logged or asserted wall-clock difference (`now() - x` that is not a direct operand of `<`, `<=`, `>` or `>=`);
- no suite passes page code as a string (R0), which cannot be scanned;
- every SerialAlone suite carries a justified reason with a line, and every detector rule it hits is declared; a rate assertion (a lower bound on samples over a wall-clock window) and the owner-named suites are declared reasons;
- each literal `selectTool`, `stackUnderLiftedBall` and `selectLevel` call is declared in `Places` or `Levels`, and each driver call that exercises a Workshop area is declared in `Areas`.

| SerialAlone suite | Reasons |
|---|---|
| clock-master | injects stalls, tampers with clocks, measures wall time; owner-named |
| cat-017 | measures wall time; asserts a wall-clock sample rate; owner-named |
| cat-023b | owner-named (4000 ms negative windows) |
| cat-037 | measures wall time (`s.t < 2200`, `s.t < 400`) |
| cat-014 | measures wall time (paces by elapsed wall time) |
| anim-1a, anim-1b, anim-1c, cat-023a | assert a wall-clock sample rate |
| capacity-9r | owner-named (coordinator, 20 Oct 2026): a new suite starts SerialAlone until calibration qualifies it; the 2,305-collider scene debt-faulted before Story 9R.2's broadphase fix |
| cat-062 | owner-named (coordinator, 20 Oct 2026): test 4 timed out with no failed assertion on a Parallel worker under N=2 AOT workers (`_bmad-output/implementation-artifacts/evidence-9r-3/full-run-1`); Parallel returns only through Story F.1's recalibration |
| cat-051-own-supply | owner-named (coordinator, 20 Oct 2026, Story 9R.3): a new suite starts SerialAlone until calibration qualifies it; it samples blade and ball poses over wall-clock windows after each in-run supply toggle |
| cat-020 | owner-named (coordinator, 20 Oct 2026): a boot-time clock recalibration under N=2 AOT workers (`_bmad-output/implementation-artifacts/evidence-9r-1/full-run-parallel`); Parallel returns only through Story F.1's recalibration |

Every other suite is Parallel. Rewriting a SerialAlone suite to measure simulated time instead belongs to the story that owns it.

## Exact Run/Reset snapshots (Story 9R.1)

`tools/e2e/run-reset-snapshot.ts` is the one comparison authority for Reset restoration. `driver.captureSnapshot({ bodies })` saves through the Workshop menu in Build mode and flattens the committed read of the Save acknowledgement the driver consumed, plus the authored construction in the IndexedDB slot, into an ordered `path → value` list: every f32 is its raw IEEE-754 bit pattern, so −0 and every NaN payload are distinct. Only the advancing identity paths `read.sequence`, `read.revision`, `read.publication`, `read.epoch`, `read.capture`, `read.session`, `read.master_generation`, `read.cadence` and `read.projection` are excluded. `bodies` is the bench's committed (dynamic) body count. `driver.expectRunAdvanced(s0)` pauses through the menu and requires the Pause acknowledgement's committed read to be past tick 0 and to differ from S0; `driver.expectResetMatches(s0)` re-snapshots after `reset()`, asserts bit-equality (naming the first differing path and both values) and that no goal solved since the Reset. A Load leg compares with `assertLoadRestores`, which also ignores the reloaded page's restarted `saved.construction.revision`.

Its browserless unit checks live in `tools/e2e/unit/` (outside the runner's `tools/e2e/*.test.ts` catalogue): `node --import <tsx 4.23.15>/dist/loader.mjs --test tools/e2e/unit/run-reset-snapshot.test.ts`.

## Session faults and close findings

Every suite fails when its session raises a fault: `WorkshopDriver.close()` records a finding for each `CCGPU_DEBT_FAULT` (simulation debt beyond the 100 ms bound stopped the world), each `CCGPU_CLOCK_REPLY_REJECTED` (a malformed clock reply) and, unless the probe launched with `allowClockRecalibrations: true`, each `CCGPU_CLOCK_RECALIBRATED` (the counted continue path of a clock lapse, never normal operation), and for a browser that fails to close. A test that deliberately provokes exactly one session fault declares it with `closeExpecting(SessionFault.<fault>)`, which fails the test at once unless that fault occurred and no other did (`clock-master.test.ts`, the 600 ms block while Running). `close()` never throws, because suites close in `finally`: the root `afterEach`/`after` hooks of `tools/workshop-close-findings.mjs` fail the test (or the file, for a close in a suite's `after` hook) with every finding, and a test that already failed keeps its own error. A suite's `after` hook closes with `closeAfterSuite()`, which fails that suite directly. A `CCGPU_CLOCK_RENEWED_LATE` record (a mapping that lapsed only because the requester's own event loop stalled, renewed by a consistent fresh exchange) is counted in `clockLateRenewals` and reported at close ("Session clock late renewals"), never a failure. The driver keeps the Simulation's typed `CCGPU_CLOCK_DERIVATION` records in `clockDerivations` for evidence.

The driver prints `E2E_OBSERVED <signal> <record>` for each debt fault, recalibration, rejected reply, late renewal, wide startup offset and `SimulationUnresponsive` transport failure; the runner counts them per file (`debtFaults`, `recal`, `rejected`, `renewedLate`, `wideOffset`, `unresponsive`), with `closeFindings` and `lateRenewalsReported` from the close-finding and late-renewal report lines.

`clock-master.test.ts` writes its retired-session screenshot to the required `CLOCK_EVIDENCE_SCREENSHOT` file path, which the runner places in the run's output directory: never committed evidence, never the repository root.

The shipped C# physics engine's numerical laws are stepped natively in `CuriousContraptions.tests` (`WorkshopPhysicsEngineTests` and the `WorkshopPhysicsLaw*Tests` files): most step `WorkshopPhysicsEngine` directly on the admitted world bytes, and the scene tests run the production `WorkshopSimulation` through the worker's `BrowserGpuTransport`. They run in `dotnet test CuriousContraptions.slnx`, which also runs the runner's own tests (`tools/E2eRunner.Tests`, browserless fixtures under `Fixtures/`). The Node harnesses of the browser scripts run with `node --experimental-vm-modules --test tools/*.test.mjs`.
