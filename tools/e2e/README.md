# Workshop browser tests

Run the suites serially with installed Chrome against the production preview: the bundle `dotnet publish CuriousContraptions.web` writes (no `PlaytestDiagnostics` property: every suite runs only against the production build; the PlaytestDiagnostics variant adds diagnostic recording and is never a gate), served by `dotnet run --project tools/Preview` on port 8060. The read-only observations the suites decode (`CCGPU` acknowledgements, `CCGPU_RETAINED` energy and event commits, `CCGPU_CLOCK_RECALIBRATED` and `CCGPU_CLOCK_DERIVATION` telemetry, the `globalThis.Workshop*` read hooks) ship in the production build until Story 9A.24 replaces them with the typed observation port. TypeScript enums require a TypeScript loader; Node's strip-only invocation is insufficient.

```sh
dotnet publish CuriousContraptions.web
dotnet run --project tools/Preview &        # serves CuriousContraptions.web/AppBundle on :8060
npx --yes --package=tsx@4.23.15 tsx --test --test-concurrency=1 --test-timeout=150000 tools/e2e/*.test.ts
kill %1                                     # stop the preview (or Ctrl+C when it runs in the foreground)
```

With tsx 4.23.15 already installed, `node --import <tsx>/dist/loader.mjs --test --test-concurrency=1 --test-timeout=150000 tools/e2e/*.test.ts` is the equivalent local invocation. Read-only observations supplement actual UI construction; they are not test control hooks.

Every suite fails when its session recalibrates a clock mapping: `WorkshopDriver.close()` records a finding for any `CCGPU_CLOCK_RECALIBRATED` record, because a recalibration is the counted continue path of a clock lapse, never normal operation. Only a test that deliberately starves a requester context or skews the master launches with `allowClockRecalibrations: true` and asserts the typed records itself (`clock-master.test.ts`). `close()` also records a finding for any `CCGPU_CLOCK_REPLY_REJECTED` record (a malformed clock reply), with no opt-out, and for a browser that fails to close. `close()` never throws, because suites close in `finally`: the root `afterEach`/`after` hooks of `tools/workshop-close-findings.mjs` fail the test (or the file, for a close in a suite's `after` hook) with every finding, and a test that already failed keeps its own error. Count a file's result by its exit code, not only its test lines. A suite's `after` hook closes with `closeAfterSuite()`, which fails that suite directly. A `CCGPU_CLOCK_RENEWED_LATE` record (a mapping that lapsed only because the requester's own event loop stalled, renewed by a consistent fresh exchange) is counted in `clockLateRenewals` and reported at close ("Session clock late renewals"), never a failure. The driver keeps the Simulation's typed `CCGPU_CLOCK_DERIVATION` records in `clockDerivations` for evidence. `clock-master.test.ts` writes its retired-session screenshot to `CLOCK_EVIDENCE_SCREENSHOT`, by default `_bmad-output/implementation-artifacts/evidence-9-4b/simulation-unresponsive.png`, never the repository root.

The cumulative serial run of every suite is the regression baseline each story keeps. Its latest recorded result and identities are in the story's evidence; a suite is not part of a passing baseline until such a run is recorded.

Suite-specific preview and evidence variables (all optional; each defaults to the 8060 preview):

- `BUMPER_PREVIEW_URL` for `cat-015a`, `cat-015a-sidekick` and `cat-015b`. CAT015b creates a unique evidence directory inside `.anvil`; an explicit `CAT015B_EVIDENCE_DIR` must name a new directory there. Existing directories reject rather than overwrite screenshots.
- `BATTERY_PREVIEW_URL` for `cat-005`, which runs against the production preview like every other suite.
- `PRELOAD_PREVIEW_URL` and `PRELOAD_EVIDENCE_DIR` for `cat-062b`. The evidence directory must be a new path inside `.anvil`; an existing directory rejects to retain earlier screenshots. Tests use actual placement, rotation and settings controls, read-only saved construction/committed poses, physical energy and matched zero/heading controls.
- `SPRING_PREVIEW_URL` and a new `SPRING_EVIDENCE_DIR` inside `.anvil` for the Springboard UI cases in `cat-062`.
- `CLOCK_PREVIEW_URL` for `clock-master.test.ts`, the permanent injected-load regression of the off-thread clock master (Story 9.4b).

The shipped C# physics engine's numerical laws are stepped natively in `CuriousContraptions.tests` (`WorkshopPhysicsEngineTests` and the `WorkshopPhysicsLaw*Tests` files): most step `WorkshopPhysicsEngine` directly on the admitted world bytes, and the scene tests run the production `WorkshopSimulation` through the worker's `BrowserGpuTransport`. They run in `dotnet test CuriousContraptions.slnx`. The Node harnesses of the browser scripts run with `node --experimental-vm-modules --test tools/*.test.mjs`.
