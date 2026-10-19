# Specification: Story 9R.0 Real C# Static Analysis Gate

*Status: Approved (implementer self-approval under the owner's autonomous-continuation authority, 18 Oct 2026). Round 2 fixes Murdoch-3's **Fail** ([review record](review-9r-0-csharp-static-gate.md)). Implemented in worktree branch `worktree-agent-a08aebc94d551b53b` (base `8f682f8`); awaiting re-review and the Chrome checks in section 8.*

## 1. Overview & Context

- **Goal:** make "0 warnings" mean something for C#. Review-9-4 row A2 proved that `anvil check --changed` scans no `.cs`: a probe with two empty catch blocks returned 0 warnings. Every recorded "anvil 0 warnings" claim on C# was vacuous (report #18, X1).
- **Outcome:** every C# project, every configuration and every file-based app builds with the .NET analyzers and warnings as errors.
  - The pre-commit hook builds a from-scratch snapshot of exactly what is staged, then runs a guard test for what the analyzers cannot see: suppressions, widened exemptions and swallowed exceptions.
  - CI repeats the builds.
- **Behaviour:** warning fixes and the gate itself. Runtime changes are limited to error paths, exception logging and resource disposal (section 5). Section 8 lists what a Chrome run must confirm after the rebase.
- **Deletes:**
  - the DoD claim that `anvil check --changed` covers C#. That covers guide DoD item 2 and its pre-commit, reviewer, loop and gate-table steps, epics Global DoD item 2, and the C# half of NFR-06;
  - the root `Directory.Build.props` import of a props file above the repository;
  - the stale uncompiled `CompiledMemoryLayoutTests.cs`, which was written against an API that never existed here and is now rewritten.

## 2. Acceptance Criteria Mapping

| AC (epics.md Story 9R.0) | Delivered by | Evidence |
| :--- | :--- | :--- |
| Analyzers with `AnalysisLevel` recommended plus `TreatWarningsAsErrors` for every production and test project; any disabled rule listed and justified. | `Directory.Build.props` (pinned `10.0-recommended`, hermetic root). `CuriousContraptions.slnx` lists all 33 projects. The web host's own sources compile in the hook and in CI. | `evidence-9r-0/inventory.txt`, `fix/P1-web.log` |
| Every exposed warning is fixed at source; any unavoidable suppression is justified and listed. | Sections 5 and 6. No pragma, `SuppressMessage`, `NoWarn` or nested configuration exists, and `CSharpStaticGateTests` keeps it that way. | `inventory.txt` AFTER, `fix/P4-guard.log` |
| The pre-commit hook runs `dotnet build CuriousContraptions.slnx` with the gate; a commit with a C# warning is refused. | `.githooks/pre-commit`: an index snapshot, built from scratch. | `fix/P1`–`P4` (real `git commit` attempts refused, HEAD unchanged) |
| Both DoD texts say anvil does not analyse C# and name the C# gate. | The guide (DoD 2; pre-commit, reviewer, loop step 4, Murdoch step 3, sequence diagram, gate table) and epics (Global DoD 2, NFR-06). | diff |
| Negative control: an empty `catch {}` and an unused variable fail the build; the unchanged tree is clean. | Probes, since removed. | `negative-control.txt`, `fix/` |
| `dotnet test` passes 100%, and every `tools/e2e` suite passes in one serial Chrome run. | `CuriousContraptions.tests` passes 1016/1016 by default and 1030/1030 with PLAYTEST. `Coverage.Tests` now runs and has 3 pre-existing failures (Story 9.4b, F9). **The Chrome run is not done here** (this agent may not run Chrome). | `verification.txt` |

## 3. Gate Design

- **`Directory.Build.props`:**
  - **Hermetic root.** It no longer imports a `Directory.Build.props` from above the repository. This worktree sits inside the main checkout, and its builds were silently importing that checkout's uncommitted Story 9.4b props, which added an IL2001–IL2125 warnings-as-errors list. A props file above the repository could equally turn the gate off.
  - **Gate block:** `AnalysisLevel=10.0-recommended`, pinned so that a newer SDK cannot add rules silently; `EnforceCodeStyleInBuild`; `Nullable=enable`; `TreatWarningsAsErrors`.
  - **Vendored opt-out:** a vendored directory's own `Directory.Build.props` sets `CSharpStaticGate=false` and then imports this file, which turns off `RunAnalyzers` and `EnforceCodeStyleInBuild` for that project. The 9.4b rebase uses this for `CuriousContraptions.FlatBuffers/`.
- **`.editorconfig`** (root only) raises these rules to warning:
  - CA1031, CA1065, CA1508, CA1849, CA1823 and CA2008;
  - **CA2000 and CA2213**, with precise Godot exclusions. CA2000 excludes `T:Godot.Node`, because the scene tree owns and frees a node after `AddChild`. CA2213 excludes `T:Godot.Node|T:Godot.Resource`: a field holding a node, or an exported or loaded resource shared through Godot's resource cache, must not dispose it.
    - Measured: excluding all of `GodotObject` hid only 2 more sites than `Node` alone. One is `PartDefinition`'s exported `PackedScene` (cache-owned, hence the CA2213 `Resource` exclusion); the other is a test `InputEvent`, now disposed. Every other Godot object (input events, materials created with `new`) stays under CA2000.
    - There is no ownership-transfer option: it would wrongly treat a `GpuDeviceOwnership.Borrowed` simulation as an owner;
  - IDE0051, IDE0052, IDE0059 and IDE0060.

  It also holds the scoped exclusions in section 6.
- **`Directory.Build.targets`:** the 2dog.xunit compile-in sources under `~/.nuget` get CA1711 and CA1001 set to `none`, through a generated per-file globalconfig.
- **What Csc received (round 4; replaces the round-3 `CSharpStaticGate.targets`, its post-compile property read and its manifests, all deleted).** The hook and CI build every entry point with a binary log (`-bl`) and run the guard with `CSHARP_STATIC_GATE_BINLOGS` pointing at them. The guard replays each log with the SDK's own `Microsoft.Build` and reads every `Csc` task run: its command line (`TaskCommandLineEventArgs`) and its `Sources` items. Whatever a property, target, import, response file or command-line switch did on the way, this is what the compiler got. For every run (only `CuriousContraptions.FlatBuffers/CuriousContraptions.FlatBuffers.csproj` is exempt, keyed by exact path):
  - **Compiler:** the SDK's `Roslyn/bincore/csc`, `/noconfig`, no `@` response files.
  - **Every token** is one of that run's sources or a switch on a pinned list of 29 names; an unknown or future switch fails by default.
  - **Diagnostic switches:** `/warnaserror+` and `/warnaserror+:NU1605,SYSLIB0011` present; `/warnaserror-:IL2125` only in `CuriousContraptions.csproj`; no other `/warnaserror` form; `/nowarn:` ⊆ {1701, 1702, 8002, IL2121}; exactly `/warn:10` and `/nullable:enable`; `/features:` and `/langversion:` values pinned; no `/skipanalyzers`, `/ruleset`, `/reportanalyzer`.
  - **Analyzers:** the four .NET analyzer and code-style assemblies present; each analyzer pinned by location: SDK assemblies by file name under the dotnet root, NuGet ones by package id and file name (2dog.godotsharp, microsoft.extensions.logging.abstractions, microsoft.net.illink.tasks, xunit.analyzers).
  - **Analyzer configuration:** the root `.editorconfig` and the SDK's `analysislevel_10_recommended.globalconfig` present; besides those only the compile's own generated MSBuild config and 2dog package-source config (in its `/out:` directory) and an inert `.editorconfig` above the repository. The generated MSBuild config may carry only 33 pinned `build_property` keys, with `EffectiveAnalysisLevelStyle = 10.0`, `EnableCodeStyleSeverity` and `GodotDisabledSourceGenerators` empty, and the analyzer switches (`EnableTrimAnalyzer`, `EnableAotAnalyzer`, `EnableSingleFileAnalyzer`, `EnforceExtendedAnalyzerRules`, `VerifyReference*Compatibility`) only true or unset, and true where pinned (the trim analyzer on every trimmable project, the web host and the file-based apps).
  - **Coverage:** every solution project and both file-based apps must have a logged Csc run, and every logged run must be one of them.
- **`.githooks/pre-commit`:**
  1. The existing staged-skip check.
  2. `git checkout-index --all` into a fresh temp directory, resolved with `pwd -P`. It is outside every checkout, so no props above it apply. The physical path matters: through the macOS `/var` symlink, a solution build resolves paths differently and the `.editorconfig`'s path-relative sections stop matching.
  3. A refusal of any indexed file in a `bin/`, `obj/` or `.godot/` directory (where SDK-generated sources are exempt from the scan).
  4. A sequential restore with `--disable-parallel`, because a fresh tree restored in parallel races on `obj/*.nuget.g.props`.
  5. `--no-incremental` builds with `-noAutoResponse` and a binary log each: the solution (default, then `PlaytestDiagnostics=true`), the web host's own sources (`WasmBuildOnlyAfterPublish=true TwoDogAutoImport=false`: compile only, with no bundle or Godot import), and both `tools/anvil` file-based apps (which reject MSBuild-only switches; any `.rsp` in the repository is refused).
  6. `dotnet test -noAutoResponse` filtered to `CSharpStaticGateTests` and `WorkshopUiFailureTests`, with `CSHARP_STATIC_GATE_BINLOGS` and `TreatNoTestsAsError=true`, so a missing compile or a deleted or renamed guard fails.
  7. `anvil hook pre-commit`.
- **Hook cost:** 37 s wall time per commit, measured (`round4/hook.log`).
- **`CSharpStaticGateTests`** (Roslyn and MSBuild from the SDK). Its inputs are what was compiled: every logged Csc run's Sources (without binary logs, an ordinary `dotnet test` uses MSBuild evaluation of each project's Compile items), plus every repository `.cs` file (`git ls-files` in a checkout; a walk of the snapshot in the hook). It fails on the compiler inputs above, and on:
  - **Compile inputs:** a compiled source outside the repository other than the pinned package sources (`2dog.xunit/`, `xunit.v3.core.mtp-v1/`) and the compile's own generated files; a repository `.cs` file that no compile compiles (F11); a committed file in a `bin/`, `obj/` or `.godot/` directory; any file in the reserved `CuriousContraptions.FlatBuffers/` before Story 9.4b pins it.
  - **Suppression, in every compiled source and repository C# file:** each file is parsed twice, with no symbols and with every symbol it tests plus `DEBUG`, and any branch still inactive is scanned as raw text. It finds `#pragma warning`, `#nullable`, `#line` (round 5; outside the SDK's generated files), the identifiers `SuppressMessage[Attribute]`, `UnconditionalSuppressMessage[Attribute]` and `GeneratedCode[Attribute]` in any spelling (an alias, a qualified name or an attribute suffix), `GlobalSuppressions.cs`, generated-code file names (`*.g.cs`, `*.designer.cs` and the like), and `<auto-generated>` headers outside the flatc output `engine/gpu/WorkshopWireGenerated.cs`. The SDK's and xUnit's own generated files are scanned too; only their generated name and header are allowed.
  - **Build output sources:** a compiled source in a `bin/`, `obj/` or `.godot/` directory must be one of the SDK's, named exactly for that project (`.NETCoreApp,Version=v10.0.AssemblyAttributes.cs`, `<Project>.AssemblyInfo.cs`, `<Project>.GlobalUsings.g.cs`) in its own intermediate directory, holding only usings and assembly attributes; or, in the two xUnit projects only, `XunitAutoGeneratedEntryPoint.cs`/`SelfRegisteredExtensions.cs`, each declaring only its one pinned type with no catch and no object creation. Anything else a target writes there and compiles is refused.
  - **Configuration files:** any `.rsp`, `.globalconfig`, `.ruleset`, `nuget.config`, `Directory.Packages.props` or `.editorconfig` besides the root `.editorconfig`, whose every key is pinned; any `.gitattributes` attribute that hides or rewrites a diff (`-diff`, `diff=`, `binary`, `-text`, `text=`, `filter=`, `merge=`, `linguist-generated|vendored|documentation`).
  - **MSBuild build logic:** every `<Target>` in a repository MSBuild file is pinned by file, name and SHA-256 of its element (10 targets: the package-source exclusion, the worker bundles and the web host's bundle steps); `<UsingTask>`, `<CreateProperty>`, `<CreateItem>` and `<Output>` outside a pinned target are refused; `<Import>` only of the root Directory.Build file (nested) or the pinned web-host props import.
  - **MSBuild lines:** any `NoWarn`, `TreatWarningsAsErrors`, `WarningsNotAsErrors`/`WarningsAsErrors`, `RunAnalyzers*`, `AnalysisLevel*`/`AnalysisMode*`, `EnforceCodeStyleInBuild`, `CSharpStaticGate`, `Analyzer` item, `CscToolPath`/`CscToolExe`, `DisableImplicit*`, `CompilerVisibleProperty`/`CompilerVisibleItemMetadata`, `Features`, `AdditionalFiles`, `Enable*Analyzer`, `Custom{Before,After}*Targets`, `ImportDirectoryBuild*`, `DirectoryBuild*Path`, `OutputItemType="Analyzer"`, a `CoreCompile` target, analyzer-config item, `Nullable` other than `enable`, and similar, outside the pinned lines (the root gate block, the package-source config item, the IL2125 line and the vendored opt-out).
  - **Nested build files:** a nested `Directory.Build.props` or `Directory.Build.targets` that does not import the root one.
  - **Project coverage:** a csproj missing from the slnx.
  - **Hook and CI fragments:** each hook protection above, and CI's restores, logged builds and binlog-checked test run, are asserted present.
  - **Edit validation paths:** 28 pinned UI handler and validator methods, and every method they reach (same-type calls followed transitively, and throw helpers of any type), create no `ArgumentException` in any spelling (qualified, `global::`, a `using` alias, target-typed `new(...)`, `ArgumentException.ThrowIf*`).
  - **Catches:** an empty catch body of any type (including in an `#if` branch), counting a block of only `;` as empty (F18), and any catch-all that neither rethrows, wraps the caught exception into the exception it throws, nor narrows with a pure type-pattern filter (`when (e is A or B)`), unless it sits at a pinned `file:Type.Method` with its exact count.
  - **Containers:** lambdas and local functions are their own containers, so no exemption reaches nested code (F6).
  - **Self-checks:** synthetic negative controls confirm each rule fires.
- **`WorkshopUiFailureTests`:** every `catch` under `ui/` reports through `WorkshopUiFailure.Report`, is a `WorkshopDiagnostics.Fault`, or rethrows; no UI file logs `WorkshopDiagnostics.Rejection` directly. It also proves both classification paths (F7, §10).
- **CI (`pages.yml`):** the same restores and logged builds run before the tests (logs in `$RUNNER_TEMP/csharp-gate-binlogs`), and the tests run with `CSHARP_STATIC_GATE_BINLOGS` set.

## 4. Review Findings and Resolutions

| # | Resolution |
| :-- | :-- |
| F1 | The hook and CI compile `CuriousContraptions.web`'s own sources, and the guard asserts that they do. The new gate immediately found a real CA2000 in `web/Program.cs` (the engine was dropped on the success path); the page now holds it in `Program.Running`. **Proof:** `fix/P1-web.log`, a real commit refused with CA1031, CS0219 and IDE0059. |
| F2 | The hook builds an index snapshot from scratch. **Proof:** `fix/P2-staged.log`, a staged warning behind a clean working copy, refused. `fix/P3-stale.log`, a commit after a warm `-p:TreatWarningsAsErrors=false` build, refused. HEAD did not move in either. |
| F3 | `CSharpStaticGateTests`. **Proof:** `fix/P4.log`, a real commit refused by the guard. `fix/P4-guard.log` lists every vector: `#pragma` (C), `[SuppressMessage]` (D), `GlobalSuppressions.cs` (E), nested `.editorconfig` (F), csproj `NoWarn` (G), csproj `TreatWarningsAsErrors=false` (H), nested `CSharpStaticGate=false` (I) and an `<auto-generated>` header (J). |
| F4 | CA2000 and CA2213 are on, with the precise Godot exclusions from §3. All 37 sites are fixed at source:<br>• **Explicit `GpuDeviceOwnership { Owned, Borrowed }` on `WorkshopSimulation`.** The warm-up borrows the worker's device and is disposed with `await using`, and its disposal never disposes the device. **Test:** `ABorrowedWarmupLeavesTheDeviceToItsOwnerWhichThenInitialisesAFreshSimulation`.<br>• **`WorkshopGpuDevice`'s gate is disposed exactly when it is idle after disposal.** That is at disposal itself, or at the release of the last operation that held or awaited it. `Release` hands the count to a queued waiter, so `CurrentCount == 1` means idle. **Tests:** three `Disposal…Gate…` tests. The queued-waiter test fails on the round-2 commit's naive "dispose on any release after disposal" (`SemaphoreSlim`'s `ObjectDisposedException` instead of the device's).<br>• **`BrowserWorkshopClient.Create`** disposes through `DisposeAsync` on every failure path. A local retire path covers a client that never started.<br>• **Preview:** `using var` for the file provider.<br>• **Web host:** the engine is held in `Program.Running`.<br>• **Tests:** the `SimulationRun` harness constructs and owns its simulation, which owns its device. `WorkshopGpuDeviceTests` owns its device and lends it to a `Borrowed` simulation. The clients use `await using`, `CapturedConsole` keeps only a restore action, and the hint test disposes its `InputEvent`. |
| F5 | Out of scope by coordinator decision: Story 9.4b deletes `tools/workshop-rigid-body.test.mjs`. |
| F6 | The guard pins each boundary method's exact catch-all count and treats lambdas and local functions as separate containers. **Proof:** in `fix/P4-guard.log`, a catch-all lambda inside excluded `ToggleRun` is caught. |
| F7 | Superseded in round 3 (§10): every UI handler classifies through `WorkshopUiFailure.Report`. A declared refusal (`WorkshopEditRejectedException`, `WorkbenchFullException` or an indeterminate transport) is a stdout `CCGPU_UI_REJECTION` with its message as the status; anything else is a whole-exception stderr `CCGPU_UI_HANDLER_EXCEPTION` and an "Unexpected fault" status. `MachineWorld`'s presentation and disposal boundaries log the whole exception through `WorkshopDiagnostics.Fault`. |
| F8 | `SubmitConstruction` is the one boundary for every construction edit, including the place, delete and undo handlers, which are async void with no catch of their own. Every exception goes through `WorkshopUiFailure.Report`: a declared refusal is a rejection, anything else a logged fault with a fault status. Either way the presentation is restored and it returns `false`. It is a pinned CA1031 boundary (count 1). |
| F9 | `Coverage.Tests` references the test SDK, so `dotnet test CuriousContraptions.slnx` runs it. Its 3 pre-existing failures are recorded, not fixed (Story 9.4b). |
| F10 | `WorkshopWireFlatBuffers.DecodeCommand` and `DecodeResponse` are the typed boundary. They translate the reader's `OverflowException` and `IndexOutOfRangeException` into `ArgumentException`, with the cause kept. `WorkshopWireDecodeBoundaryTests` covers every truncation, every single-bit flip, oversized and prefixed buffers, fixed-binary encodings and seeded random buffers, over commands, an acknowledgement and a full physical read. It was red (`OverflowException`) before the boundary existed. The sniff's fallback decoders, `WorkshopWire.DecodeCommand` and `DecodeResponse`, get the same corpus. They already reject only with `ArgumentException`, and the tests pin it. Both sides of the sniff are therefore one typed boundary. |
| F11 | `CompiledMemoryLayoutTests.cs` is rewritten against the real compiled scene (4 tests) and added to the test project. |
| F12 | A superseded `RunLoop` logs its exception (`STALE_RUN_LOOP`). A failed installation that carries a cause logs `INSTALLATION`, and a superseded one logs nothing (**Test:** `AnInstallationFailureCarryingACauseIsLoggedAndASupersededOneIsNot`). The warm-up is a borrowed, disposed simulation. The worker's own `Initialize` admits a fresh world on the device, and every command already discards its candidate in `finally`. |
| F13 | Each boundary sets Faulted (`Failure(...)`) **before** a synchronous diagnostic write. The `await WriteLineAsync` calls are gone. |
| F14 | There is one channel, `WorkshopDiagnostics`:<br>• **Faults** (`CCGPU_<SITE>_EXCEPTION`, written whole) go to stderr.<br>• **UI rejections** (`CCGPU_UI_REJECTION`, an edit the player was refused) go to stdout, because an ordinary refused edit is not a fault.<br>• **`TRACE_FLUSH`** stays on stderr by necessity: stdout is the stream that failed. |
| F15 | `AnalysisLevel` is pinned. CI runs the gate. |
| F16 | The evidence moved to `_bmad-output/implementation-artifacts/evidence-9r-0/`, with the raw before-site lists in `raw/` and the round-2 proofs in `fix/`. |
| F17 | DoD 2 names IL2125 as the one expected warning until the rebase. |
| F18 | Empty catch bodies of any type, and catch-alls with non-type filters, are rejected by the guard. **Proof:** in `fix/P4-guard.log`, A (`catch (FormatException) { }`) and K (`when (e.HResult != 0)`) are caught. |

**Additional defects found and fixed this round:**
- `Program.Schedule` replaced any installation failure with a cause-less `WorkshopInstallationException`. It now carries the cause, and a superseded installation passes through unchanged. `WorkshopSimulation` logs a carried cause as an `INSTALLATION` fault.
- `WorkshopSaveCodec.Decode` dropped the cause of a malformed save. It now keeps it.

## 5. Fixes (round 1 and round 2)

Round 1 fixed 403 unique sites and is unchanged (`inventory.txt`):
- CA1051, which made the Simulation types internal;
- CA1050, the namespaced worker exports;
- CA1859, CA1305, CA1310, CA1512, CA1065, CA2208, CA1508, the renames, and the IDE0059/IDE0060 decisions;
- the CA1031 narrowing: codec sniffing, trace, PLAYTEST diagnostics and the tool harnesses.

Round 2 adds:
- CA2000/CA2213 (35 sites);
- web `Program.cs` CA2000;
- the diagnostic channel (`engine/gpu/WorkshopDiagnostics.cs`, with `FaultSite` as a closed enum);
- the `SubmitConstruction` narrowing;
- the FlatBuffers decode boundary;
- the cause-carrying wraps;
- two redundant `TreatWarningsAsErrors` lines removed from tool csprojs.

## 6. Exclusions (every one scoped, justified and pinned by `CSharpStaticGateTests`)

| Rule | Scope | Justification |
| :--- | :--- | :--- |
| CA1031 | `WorkshopSimulation.Initialize`, `Construct`, `AdvanceCore`, `Reset` | Simulation command fault boundary: any failure faults the simulation (Faulted, then logged whole). |
| CA1031 | `MachineWorld.PresentWorkshopRead`, `DisposeWorkshop` | A presentation failure faults the Workshop. Disposal always completes. Both log whole. |
| CA1031 | Simulation `Program.WarmupSimulation`, `RunLoop` | The warm-up is non-fatal. The run loop retires the worker. Both log whole. |
| CA1031 | Each Workshop async void handler `X` (`_Ready`, `Click`, `DeleteSelected`, `Undo`, `ToggleRun`, `ResetRun`, `Save`, `LoadSave`, `ApplyBatterySettings`, `ApplyBatteryEnable`, `ApplyClockInterval`, `ApplyDelayDuration`, `ApplyBumperStrength`, `ApplySpringboardSettings`, `ChangeConnection`, `Playback`, `CommitSelected`, `SelectModeFromPicker`, `ChangePrecision`), the classifying body `XAsync` of each that has one (`ClickAsync` and `ResetRunAsync` 2 each), `SubmitConstruction` and `HandlerFailed` | Story 9R.0c layering (§13). The handler `X` is exactly one catch-all passing the exception to `HandlerFailed`, which logs it whole (`GD.PushError`) and shows it: an exception escaping async void terminates the browser runtime. `XAsync` classifies its own failures through `WorkshopUiFailure.Report` (a refusal to stdout, a defect whole to stderr) or `WorkshopDiagnostics.Fault`. `WorkshopUiFailureTests` requires each UI catch to report, rethrow, or pass the caught exception itself to `HandlerFailed` or `GD.PushError`. |
| CA1031 | `PalettePlacementTests.EscapeRecorder.Post` (test) | The test synchronization context records every exception escaping an async void handler, so a test can assert none escaped. |
| CA1707 | `CuriousContraptions.tests/**`, `tools/Coverage.Tests/**` | The underscore test names are cited by evidence and `--filter` commands. Production code keeps CA1707. |
| CA2000 | `T:Godot.Node` and derived types | The scene tree owns and frees a node after `AddChild`. |
| CA2213 | `T:Godot.Node`, `T:Godot.Resource` and derived types | A field holding a node, or an exported or loaded resource shared through Godot's resource cache, must not dispose it. |
| CA1711, CA1001 | The 2dog.xunit compile-in sources under the NuGet root only | Third-party package source. |
| Analyzers and code style | `CuriousContraptions.FlatBuffers/` (vendored Google FlatBuffers v25.2.10) | Third-party source kept byte-identical to upstream. Its `Directory.Build.props` sets `CSharpStaticGate=false`; compiler warnings-as-errors and trim analysis still apply. Every file in the directory is pinned by SHA-256 (§13). |

That is 49 sites in 47 methods, including `Workshop.SubmitConstruction` (F8), which returned to the list in round 3 because it now reports defects as faults instead of rethrowing them out of async void handlers. The IL2125 `WarningsNotAsErrors` exemption is gone (§13).

## 7. Findings Routed Elsewhere
- **F1-codec:** the dual wire codec is still sniffed by exception, now typed end to end. Owner: Epic 9A single codec (9A.2/9A.40) with Story 9.4b.
- **F2-contact:** `ReceiveContactSample` has no reversed-publication guard. Owner: the Epic 9A contact-pulse story.
- **F3-coverage:** `Coverage.Tests` fails 3/68, a stale catalogue expectation. Owner: Story 9.4b, per the coordinator. *Resolved by 9.4b: 78/78 after the rebase (§13).*
- **F4-node:** 51/107 node harness tests fail identically on the base; the rigid-body module was recorded in review-9-4. The suite is deleted by 9.4b.

## 8. Chrome Checks Required After the Rebase
1. **Worker and web boot.** Both workers resolve the namespaced exports, and the web host boots through `CuriousContraptions.TwoDogWebBoot` and holds the engine in `Program.Running` (any Run). A `BrowserWorkshopClient` startup failure now retires through `DisposeAsync`'s local path, which also retires the pose history.
2. **Codec boundary.** Run, Pause, Resume, Step, Reset, Save and Load. The sniff and the decoders now reject every malformed buffer with `ArgumentException`.
3. **PLAYTEST trace capture.** The suites that read `CCGPU_TRACE_*` and `CCGPU_TICK_TIMES_*`.
4. **Diagnostic streams.**
   - `CCGPU_ADVANCE_EXCEPTION` moved from stdout to stderr.
   - `FAULT 1:` is now `CCGPU_RUN_LOOP_EXCEPTION` on stderr.
   - `WarmupSimulation non-fatal:` is now `CCGPU_WARMUP_EXCEPTION` on stderr.
   - New stderr lines: `STALE_RUN_LOOP`, `INSTALLATION`, `PRESENTATION`, `DISPOSAL`, `TRACE_FLUSH` and `LATE_DISPOSE`. *Since §13 item 6:* a `PRESENTATION` or `DISPOSAL` failure caused by a transport that has already retired (and reported `CCGPU_TRANSPORT_FAILURE`) is a stdout `CCGPU_PRESENTATION_AFTER_RETIREMENT` or `CCGPU_DISPOSAL_AFTER_RETIREMENT` line instead.
   - New stdout line: `CCGPU_UI_REJECTION`, for declared refusals only; it now names `CuriousContraptions.Gpu.WorkshopEditRejectedException` where it named `System.ArgumentException` (no suite matches on the type). A plain `ArgumentException` reaching a handler, such as `DelayDuration.FromInput` outside its spin-box range, is now a stderr fault.
   - New stderr line: `CCGPU_UI_HANDLER_EXCEPTION`, for any other exception a UI handler catches (round 3). An `InvalidOperationException` reaching a handler, such as `MachineWorld`'s "A Workshop command is pending.", is now a fault: the UI's `_gpuPending` guard should make it unreachable, and the e2e suites will show whether it is.

   Suites such as cat-005 fail on any console error, so confirm that none of these appear on ordinary lifecycle paths. No recorded run in either checkout (`_bmad-output`, `docs`, main `.anvil`) contains `WarmupSimulation non-fatal`, `FAULT 1:` or `CCGPU_ADVANCE_EXCEPTION`, so the streams were chosen by meaning (a caught fault goes to stderr). The first Chrome run decides this for the warm-up in particular.
5. **Faults.** A forced fault, such as a transport timeout, sets Faulted before its log line, and the status text is sensible.
6. **Edits.** Place, delete, undo, rotation-gizmo and configuration edits. Refused edits still return to the prior construction and show their message. A defect inside `SubmitConstruction` now escapes visibly. Load of a corrupted browser save (not base64) is now the save codec's "Unsupported construction save." refusal, not a `FormatException`.
7. **Device lifetime.** Warm-up then Initialize; Reset during Run; scene removal during a GPU operation (the device gate's disposal).
8. **UI.** The labels, rotation rows, `+`/`−` icons and input paths from round 1.
9. **The full serial `tools/e2e/*.test.ts` run** (AC6).

## 9. Rebase Notes (onto Story 9.4b)
- Delete the IL2125 `WarningsNotAsErrors` line in `CuriousContraptions.csproj`, its entry in `CSharpStaticGateTests.MsBuildGateLines`, and `PinnedWarningNotAsError`.
- Add `CuriousContraptions.FlatBuffers/Directory.Build.props` that sets `<CSharpStaticGate>false</CSharpStaticGate>` and imports the parent props, keep its code and Apache-2.0 LICENSE/NOTICE unchanged, and add the project to the slnx. The compiler-input pins already exempt exactly that project path; replace `TheVendoredDirectoryIsAbsentUntilItsFilesArePinned` with a pinned file-hash manifest of the vendored copy.
- 9.4b's main-checkout `Directory.Build.props` adds trim-warnings-as-errors (IL2001–IL2125). Merge it into the root gate block, and in the same change extend the pinned lines and `PinnedWarningsAsErrors` (the exact `/warnaserror+:` list Csc receives).
- If 9.4b adds a project, a compiled source outside a project, an analyzer package, a Csc switch or a target, it must fit the pins (`PackageSourcePrefixes`, `SdkAnalyzers`/`PackageAnalyzers`, `AllowedSwitches`/`PinnedSwitchValues`, `PinnedTargets`), changed in the same diff.
- `CuriousContraptions.slnx`, `epics.md` and the guide changed on both sides, so merge them by hand. Then re-run the F10 fuzz against the vendored reader.

## 10. Round 3 (review 2 Fail: C1–C3)

*Round 4 (§11) replaced items 1 and 2's mechanism (the post-compile targets file and its manifests) with the binary-log check of what Csc received, and narrowed the save-decode catch; the rest stands.*

Round 2 guarded the directory tree, so three real commits got past it: C1 (a `SuppressMessage` alias, an empty typed catch inside `#if DEBUG`, and an `archives/` file with a `#pragma` pulled in by `<Compile Include>`), C2 (`<Analyzer Remove="@(Analyzer)"/>` in a csproj or nested `Directory.Build.targets`) and C3 (a root `Directory.Build.rsp`). Round 3 stops listing vectors and checks what was compiled:

1. **Effective configuration** (closes C2, C3 and every property, item or command-line route to them): `CSharpStaticGate.targets`, injected as a global property, asserts each compile's properties, warning-code sets, analyzers and analyzer configuration against pins (§3). `-noAutoResponse` on every MSBuild entry point, and the guard refuses any `.rsp`.
2. **Compiled sources** (closes C1): each build's manifest lists its `@(Compile)` items; the guard scans each of them and every repository C# file, both preprocessor branches and inactive text, and matches suppression identifiers rather than spellings (§3). Nothing in the repository is skipped by directory any more; only build output directories are, and committing into one is refused.
3. **F11:** every repository `.cs` file is compiled by a project or pinned in `UncompiledSources`.
4. **F7:** a refused edit is a dedicated type. `WorkshopEditRejectedException` (sealed, `engine/gpu/`) carries a closed `WorkshopEditRejection` reason (FixedPart, NotConfigurable, OutOfRange, IdentityExhausted, UnsupportedSave, SaveSettingsMismatch, ConnectionCapacity, InvalidConnection, LatchDrivesLampOnly, LampFollowsOneLatch, PowerInputSupplied, InventoryExceeded, InventoryUnsupported, PuzzleSettings; `None` and undefined values are refused at construction). It is an `ArgumentException` so the decode and admission boundaries that reject invalid input by that type (the codec sniff, the worker's `Construct` → `InvalidConstruction`) keep rejecting it. `WorkshopUiFailure.IsRejection` accepts only it, `WorkbenchFullException` (typed by `WorkbenchTable`) and `WorkshopTransportException`. Every other exception is a defect, `ArgumentException` itself included: a programmer's argument guard, a framework failure such as a duplicate dictionary key, `ArgumentOutOfRangeException`, `InvalidOperationException` and its subtypes.
   - **Converted refusal sites** (every one keeps its message): the UI handlers' refusals in `LoadSave`, `ApplyBatterySettings`, `ApplyClockInterval`, `ApplyDelayDuration`, `ApplyBumperStrength`, `ApplySpringboardSettings`, `CommitSelected` and `SelectModeFromPicker`; `WorkshopConnections.Validate`; `WorkshopPuzzle.Validate` and the eight lesson validators (`SpringboardLessons`, `DelayedSignal`, `BumperSidekick`, `BumperAdvanced`, `DominoEffect`, `ClearPipe`, `GentleBend`, `QuarterBend`); `SpringboardSettings.Validate` (the spring fields are free text); and `WorkshopSaveCodec.Decode`. The two programmer branches in those handlers (an unknown picker index, an unhandled puzzle id) are `UnreachableException`.
   - **Traced and left as argument guards** (the UI control already bounds the value, so reaching them is a defect): `DelayDuration.FromInput` (spin box 0.1–12), `BumperWork.FromInput` (0–20), `ElectricalSourceSettings.Validate` (spin boxes 60–14400, 10–480, 0–1, an option list), `WallDimensions.FromInput` (clamps), and the structural instance, body, rotation and bound validators.
   - **Save decode:** `WorkshopSaveCodec.Decode` turns only the failures malformed bytes produce (`IsMalformedSave`: exactly `ArgumentException` and `ArgumentOutOfRangeException` from the decoders' and validators' declared input rejection, and the FlatBuffers reader's `IndexOutOfRangeException`/`OverflowException`) into `UnsupportedSave`; a rule's own rejection keeps its reason; anything else escapes as a defect. A corpus test (every truncation and bit flip of a save with parts and connections, plus 1,000 seeded random buffers) shows no other type escapes. **Open residual, ruling needed:** because the structural validators that decoding runs are argument guards shared with the edit path, an exact `ArgumentException` or `ArgumentOutOfRangeException` raised anywhere inside decode (a duplicate key included) still becomes "Unsupported construction save." Either accept the stored save as an untrusted-input boundary, or schedule a typed decode rejection (the decoder doing its own typed field checks), which is a design change.
   - **Guard:** `TheEditValidationPathsRefuseOnlyWithTheTypedRejection` pins those 28 handler and validator methods (each must still exist) and refuses any `ArgumentException` creation in them, in any spelling (qualified, `global::`, target-typed `new(...)`, `ArgumentException.ThrowIf*`), with synthetic controls. `Report` writes a refusal to stdout (`CCGPU_UI_REJECTION`) and returns its message; it writes a defect whole to stderr (`CCGPU_UI_HANDLER_EXCEPTION`) and returns "Unexpected fault (Type): message It has been logged." for the status line. `LoadSave` now rejects non-base64 browser storage as the save codec's refusal instead of letting `FormatException` through. `SubmitConstruction` reports every exception and returns `false` (F8), so the async void place, delete and undo handlers, which have no catch of their own, never lose a defect. The rotation gizmo's `ArgumentException` catch always ends the drag and reports; it prefixes "Placement unchanged:" only to a declared refusal, so an out-of-range pose (`ArgumentOutOfRangeException`) is a logged fault. Tests: refusals (3 types), a real connection-capacity refusal reaching `Report` with its reason, undefined reasons refused, defect types (10, `ArgumentException` included), runtime-thrown defects with their stack in the log (6, including a duplicate-key `ArgumentException`), the save corpus and `IsMalformedSave`, and the `ui/` wiring rule. 84 existing exact `Throws<ArgumentException>` assertions on converted sites now expect `WorkshopEditRejectedException`; two theories that mix a rule refusal with a non-finite precision assert the exact type per case.
5. **Real commit proofs.** Each probe below was a real `git commit` through the hook on top of `3a3b5f7`/`b833831` (R3 and R3b on `6ad4342`, which closed them; both compiled with 0 warnings in a plain build first, so each was a live bypass of the earlier name-pattern exemption), run by `run-probe.sh` (in `round3/`), which requires the refusal to be named in the commit's own output, then restores the clean tree. Every one was refused with HEAD unmoved (`round3/*.log`, each ending `moved=no refusal_named=yes`).

| Probe | Vector | Refused by |
| :-- | :-- | :-- |
| C1a | `using SM = …SuppressMessageAttribute` alias | guard: `ProbeAlias.cs:1: SuppressMessageAttribute` |
| C1b | `#if DEBUG` empty `catch (FormatException) { }` | guard: `NoCatchBlockIsEmpty` |
| C1c | `archives/review/Hidden.cs` with `#pragma`, via `<Compile Include>` | guard: `archives/review/Hidden.cs:2: #pragma warning disable` |
| C2a | `<Analyzer Remove="@(Analyzer)"/>` target in `Ball.csproj` | gate targets: `compiled without Microsoft.CodeAnalysis.NetAnalyzers.dll;…` |
| C2b | the same in a nested `Directory.Build.targets` that imports the root | gate targets: same |
| C2c | the same in a nested `Directory.Build.targets` that shadows the root | gate targets: same |
| C3a | root `Directory.Build.rsp` with `-p:TreatWarningsAsErrors=false`, plus an unused local | build: the rsp is ignored, so `CS0219`/`IDE0059` are errors |
| C3b | root `Directory.Build.rsp` narrowing `VSTestTestCaseFilter` to one guard test | guard: all 78 tests ran; `Directory.Build.rsp: response files … are not allowed` |
| P1 | empty typed catch in the web host's `Program.cs` | guard: `CuriousContraptions.web/Program.cs:15 Program.Main` |
| P2 | a warning staged behind a clean working copy | build of the index snapshot: `CS0219`/`IDE0059` |
| P3 | a backdated edit with a warning under warm worktree outputs | `--no-incremental` snapshot build: the warning is an error |
| P4A | empty typed catch | guard: `NoCatchBlockIsEmpty` |
| P4C | `#pragma warning disable CA2000` | guard |
| P4D | `[System.Diagnostics.CodeAnalysis.SuppressMessage]` | guard |
| P4E | `GlobalSuppressions.cs` | guard: `suppression file` |
| P4F | nested `.editorconfig` turning CA2000 off | gate targets: `used unpinned analyzer configuration …/elements/Ball/.editorconfig` |
| P4G | csproj `NoWarn` CA2000 | gate targets: `suppresses CA2000 through NoWarn` |
| P4H | csproj `TreatWarningsAsErrors=false` | gate targets: `compiled with TreatWarningsAsErrors='false'` |
| P4I | nested `Directory.Build.props` with `CSharpStaticGate=false`, not importing the root | gate targets: `compiled with TreatWarningsAsErrors='false'` |
| P4J | `// <auto-generated/>` header | guard: `generated-code header` |
| P4K | catch-all with a non-type filter | guard: `ProbeK.Parse: found 1, pinned 0` |
| P4L | catch-all lambda inside the pinned `ToggleRun` | guard: `ui/Workshop.cs:<lambda>: found 1, pinned 0` (and the `ui/` wiring rule) |
| R1 | `#pragma` in the file-based app `tools/anvil/VerifyPolicy.cs` | guard: `tools/anvil/VerifyPolicy.cs:3: #pragma warning disable CA2000` |
| R2 | force-added `elements/Ball/obj/…/Probe.GlobalUsings.g.cs` with a `#pragma`, Compile-included | hook: `committed files in a bin/, obj/ or .godot/ directory` |
| R3 | a committed `Ball.csproj` target that writes `$(IntermediateOutputPath)Evil.GlobalUsings.g.cs` (a leaking `FileStream`, no pragma) before `CoreCompile` and compiles it | guard: `compiles elements/Ball/obj/…/Evil.GlobalUsings.g.cs from a build output directory` |
| R3b | a committed `Ball.csproj` target that overwrites the SDK's `Ball.GlobalUsings.g.cs` with a class after generation | guard: `Ball.GlobalUsings.g.cs: an SDK-generated name with declarations` |
| F11a | uncompiled `tools/ProbeUncompiled/Hidden.cs` | guard: `compiled by no project` |
| F11b | a test file left out of the tests csproj's explicit Compile list | guard: `compiled by no project` |
| F7 | a `ui/` catch logging `WorkshopDiagnostics.Rejection` and `error.Message` directly | `WorkshopUiFailureTests`: `logs a rejection without WorkshopUiFailure.Report` |
| T1 | weakened `CSharpStaticGate.targets` (TreatWarningsAsErrors check disabled) | guard: `TheGateTargetsEqualTheirPinnedHash` |
| F7c | `ApplyDelayDuration` refusing with `new System.ArgumentException(…)` again (on `facf10c`) | guard: `Workshop.ApplyDelayDuration creates new System.ArgumentException` |
| F7d | `WorkshopConnections.Validate` refusing with a target-typed `ArgumentException refused = new(…)` (on `facf10c`) | guard: `WorkshopConnections.Validate creates new("Connection capacity exceeded.")` |

F7 was re-run on `facf10c` too (refused). CI reproduced in a fresh clone of `facf10c`: exit 0 in 125 s, 1079/1079.

**CI:** reproduced as written in a fresh `git clone` of the branch (git checkout, manifests required): exit 0 in 114 s on `6ad4342`, 1057/1057 tests, publish and bundle checks included (`round3/CI-clone.log`).

**Out of reach by design:** `git commit --no-verify` skips the hook (CI then runs the same gate before deploy), and a commit that edits the guard's own pins at the same time as the code it pins is a reviewed change, not a bypass.

## 11. Round 4 (review 3 Fail: CreateProperty toggles)

Murdoch-3's round-3 commit `a7a6b29` got through: two `Ball.csproj` targets used `CreateProperty` to set `TreatWarningsAsErrors=false` before `CoreCompile` and `true` after it, so the post-compile property read saw `true`. The same worked for `NoWarn`, `WarningsNotAsErrors`, `AnalysisLevel`, `WarningLevel` and `Nullable`. Round 4 closes the class:

1. **Csc's actual inputs** (§3): binary logs from every gate build; every Csc run's command line and Sources checked against pins, every token a source or a pinned switch, analyzers pinned by location, the generated MSBuild config's keys and analyzer switches pinned, every expected compile logged. `CSharpStaticGate.targets`, its import and its manifests are deleted.
2. **Build logic refused** (§3): targets pinned by hash; `UsingTask`, `CreateProperty`, `CreateItem` and `Output` outside them refused; imports pinned.
3. **Diff hiding refused:** `.gitattributes` diff-hiding attributes on any path; also `nuget.config` and `Directory.Packages.props`; the reserved vendored directory must be absent until 9.4b pins it.
4. **Low items:**
   - the `ArgumentException` guard follows same-type calls transitively, throw helpers of any type, and `using` aliases (the reachable `SocketLabel`, `IsDepth` and `IsReady` enum guards became `ArgumentOutOfRangeException`);
   - every typed-refusal assertion (104) goes through `EditRefusal.Is(reason, …)` and asserts its `WorkshopEditRejection` reason;
   - save decode: the reader's `IndexOutOfRangeException`/`OverflowException` count as a malformed save only while reading records (`Read`); during construction validation only exactly `ArgumentException`/`ArgumentOutOfRangeException` (the structural validators' declared input rejection) do; anything else is a fault. The allocator check in `Decode` refuses typed; `Encode`'s stays an invariant `ArgumentException`.
5. **Real commit proofs**: 45 probes on `401528e` (`round4/*.log`, `run-probe.sh`), each refused with HEAD unmoved and its refusal matched in the commit's own output. The round-3 targets-file messages no longer exist, so each earlier probe's expected pattern is the new check's message.

| Probe | Vector | Refused by |
| :-- | :-- | :-- |
| R1-TWAE | `CreateProperty` TreatWarningsAsErrors false before CoreCompile, true after (the round-3 bypass) | Csc inputs: `compiled without /warnaserror+` |
| R1-NoWarn | `CreateProperty` NoWarn += CS0219;IDE0059, restored after | Csc inputs: `/nowarn:CS0219` |
| R1-WarningsNotAsErrors | `CreateProperty` WarningsNotAsErrors += CS0219;IDE0059 | Csc inputs: `/warnaserror-:…` |
| R1-WarningLevel | `CreateProperty` WarningLevel 0 | Csc inputs: `/warn:0 (must be exactly 10)` |
| R1-Nullable | `CreateProperty` Nullable disable | Csc inputs: `/nullable:disable (must be exactly enable)` |
| R1-AnalysisLevel | `CreateProperty` AnalysisLevel none | no effect reaches Csc (its analysis-level config is chosen at evaluation): the warnings stay errors (`CS0219`, `IDE0059`); the target rule also refuses it |
| R4-Features | plain `<Features>run-nullable-analysis=never</Features>` (no target) silencing a CS8602 | Csc inputs: `/features:run-nullable-analysis=never (unpinned value)`; the build itself passed |
| R5 | `.gitattributes`: `CSharpStaticGateTests.cs -diff` | guard: `diff-hiding attributes are not allowed` |
| R6 | repository `nuget.config` with a local feed | guard: `nuget.config: … package configuration … not allowed` |
| R7 | a `#pragma` file in the reserved `CuriousContraptions.FlatBuffers/` | guard: `the vendored directory is reserved` |
| H1b | a same-type helper throwing `ArgumentException`, called from `ApplyClockInterval` | guard: `reaches Workshop.ProbeRefuse, which creates new ArgumentException` |
| H1c | `using AE = System.ArgumentException;` and `throw new AE(…)` | guard: `creates new AE("alias refusal")` |
| R3r | `DefineConstants` + empty catch under that `#if` symbol | guard: `NoCatchBlockIsEmpty` |
| T1 | a change inside a pinned target | guard: `target changed` |
| C2a/b/c | `<Analyzer Remove>` (csproj, importing and shadowing nested targets) | Csc inputs: `compiled without Microsoft.CodeAnalysis.NetAnalyzers.dll` |
| P4F | nested `.editorconfig` | Csc inputs: `unpinned analyzer configuration …/elements/Ball/.editorconfig` |
| P4G | csproj `NoWarn` CA2000 | Csc inputs: `/nowarn:CA2000` |
| P4H, P4I | csproj `TreatWarningsAsErrors=false`; nested props opting out | Csc inputs: `compiled without /warnaserror+` |
| C1a–c, C3a–b, P1–P3, P4A/C/D/E/J/K/L, R1a, R2, R3, R3b, F11a–b, F7, F7c, F7d | every other round-1 to round-3 vector, as in §10 | the same checks as in §10 (F11 now `compiled by no project (add it to one, or delete it)`; F7c/F7d now name the reached method) |

**CI:** reproduced as written in a fresh `git clone` of `401528e`, with the binary logs checked: exit 0 in 114 s, 1091/1091 tests, publish and bundle checks included (`round4/CI-clone.log`).

## 12. Round 5 (review 4 Fail: `#line hidden`)

Roslyn drops every analyzer diagnostic located in a `#line hidden` region (only compiler CS warnings survive), and nothing about the Csc invocation changes, so an undisposed `Timer` wrapped in it committed (`b5984a4`, reset).

1. **`#line` refused:** the source scan refuses every `#line` directive (`LineDirectiveTriviaSyntax`, `LineSpanDirectiveTriviaSyntax`), and `#line` text in inactive `#if` branches, in every scanned source except the SDK's own allowlisted generated files. Vectors: `#line hidden`, a remapped `#line 1 "file"`, a span `#line (1, 1) - (1, 10) "file"`, and an inactive-branch `#line hidden`.
2. **Every gate log required:** each named log (`solution`, `playtest`, `web`, `check-staged-tests`, `verify-policy`) must exist and contain its own compiles, and in `playtest.binlog` the four PLAYTEST projects must compile with the `PLAYTEST` define.
3. **Analyzer packages per project:** each NuGet analyzer is allowed only in the compiles that use it (Godot's generator in the game project, the logging generator in TraceAllocations, ILLink in the trim-analyzed compiles, xUnit's analyzers and suppressors only in the two test projects).
4. **The staged hook:** the hook re-runs its own staged copy (`git show :.githooks/pre-commit`), so an unstaged edit below that block has no effect, and runs `CheckStagedTests` from the snapshot. A working-tree edit of the re-run block itself is the hook's inherent limit; CI and the fragment pins remain the backstop.
5. **Real commit proofs:** 50 probes on `cb58f5f2` (`round5/*.log`), each refused with HEAD unmoved and its refusal matched in the commit's own output: the 45 of round 4, plus L1 (`#line hidden` around an undisposed `Timer`; the build itself passed, refused by `ProbeLine.cs:4: #line hidden`), L2 (`#line hidden` in an inactive branch), A1 (`xunit.analyzers` added to `Ball.csproj`: `unpinned analyzer … (package xunit.analyzers, project elements/Ball/Ball.csproj)`), B1 (a staged hook without the PLAYTEST build: `playtest.binlog: missing`) and H2 (an unstaged working-tree hook edit that exits before the gate, with a warning staged: refused by the staged hook's build).

## 13. Rebase onto Stories 9.4b and 9R.0c (main `d345952a`, then `0673bb92`)

Murdoch-3's round 5 was Pass-pending-Chrome (Lows only). The 20+ story commits were squashed into one (the unsquashed history is kept on branch `story-9r-0-history-5c0a6232`) and rebased onto main.

1. **Merge.** The root `.editorconfig` keeps 9.4b's `root = true` header and its `[CuriousContraptions.FlatBuffers/**.cs] generated_code = true` section above the gate rules. `Directory.Build.props` keeps 9.4b's trim-analysis block (IL2001–IL2125 as errors in Csc, MSBuild and ILLink) beside the gate block. The duplicate Coverage entries in the slnx were removed. Main's new tests were moved to this branch's names (`NativeSceneCollectionDefinition`, `PartInventoryDictionary`, the `GpuDeviceOwnership` argument) and its un-namespaced worker exports in `workshop-animation-clock.test.mjs` were namespaced.
2. **Warnings the gate exposed in the new code, fixed at source:**
   - CA1031, 25 sites. 23 are 9R.0c's handler layering (`X` → `XAsync` → `HandlerFailed`) and 1 is the test's `EscapeRecorder.Post`: each is a deliberate boundary, so each is pinned by name in `.editorconfig` and by count in `CatchAllBoundaries` (§6). The 25th, `WorkshopBenchPlacement.Propose`, was narrowed instead (next item), not pinned. `WorkshopUiFailureTests` now also accepts a catch that passes the caught exception itself to `HandlerFailed` or `GD.PushError`, with 11 synthetic controls (6 rejected: another identifier, a log line without the exception (named and unnamed catch), a warning, a foreign `HandlerFailed`, a status-only catch).
   - `WorkshopBenchPlacement.Propose` caught everything. It now catches only the declared refusals (`WorkshopEditRejectedException` or `WorkbenchFullException`) and reports them through `WorkshopUiFailure.Report` (stdout, not a Godot warning). Any other exception is a defect that reaches the handler boundary (new test `ABenchPlacementDefectIsNotARejectionAndReachesTheHandlerBoundary`). To keep the two refusals the tests drive typed: `CaptureInstance`'s unknown part is `WorkshopEditRejection.UnsupportedPart` (new reason), and the compiler's out-of-domain authored identity is `IdentityExhausted`. `Propose` and `PartIdentities` joined `EditValidationPaths`, and so did `Click`, `DeleteSelected` and `Undo`.
   - CA1849: `ToggleRunAsync` called the async void `ResetRun()`; it now awaits `ResetRunAsync()`.
   - Tests: CA2000 (an `await using` client; a test-owned device borrowed by the simulation), CA1875 (`Regex.Count`), CA1305 and CA1310 (invariant `Int128.Parse`, ordinal `StartsWith`), IDE0051 (an unused `Sensor` overload), CA1861 (a hoisted constant array).
3. **Spec §9 follow-ups.**
   - The IL2125 `WarningsNotAsErrors` line, its `MsBuildGateLines` entry and `PinnedWarningNotAsError` are deleted. `/warnaserror+:` must be exactly one list equal, as a set, to NU1605, SYSLIB0011 and IL2001–IL2125. The trim block's four lines are pinned (the `TrimAnalysisWarningsAsErrors` value in full).
   - `TheVendoredDirectoryIsAbsentUntilItsFilesArePinned` became `TheVendoredDirectoryIsExactlyItsPinnedFiles`: every file in `CuriousContraptions.FlatBuffers/` is pinned by SHA-256 (9.4b's `VendoredFlatBuffersIdentityTests.Pinned` plus `.gdignore`, the csproj and the opt-out `Directory.Build.props`), so the line and build-logic checks skip that directory.
   - CI (`pages.yml`) runs on every branch push and every pull request; only `refs/heads/main` uploads the Pages artifact and deploys, and the `github-pages` concurrency group moved to the deploy job (pinned by `CiKeepsTheSameRestoreAndBuilds` and `OnlyMainDeploys`).
   - **CA2000 and `FileStream` (decision item):** in a clean .NET 10 project (`scratchpad/ca2000fs`), CA2000 never flags `var s = new FileStream(…); return s.Length;` or the inline form under any `dotnet_code_quality.dispose_analysis_kind`. It flags `_ = new FileStream(…)` by default, and `File.OpenRead(…)` only under `AllPaths`/`AllPathsOnlyNotDisposed`, which would add 32 exceptional-path findings in this repository. The setting is left at its default; closing the literal control needs a decision.
4. **9.4b Lows assigned to 9R.0** (`review-9-4b-cumulative-regression-repair.md` rounds 4–5).
   - A too-long stall is now its own cause, `ClockRecalibrationCause.StallPastMappedWidth`, distinct from a genuine renewal disagreement (`MappingLifetime`). Test-first (red 6/59, green 59/59). Mutants killed: merging into `MappingLifetime` (6 failures) and merging into the new cause (4 failures).
   - R5-02: `AnOddWidthOldMappingIsBoundToTheNanosecond` uses a 400,001 ns slot, so the drift-widened width reaches 2,000,001 ns. It kills the 1 ns loosening that survived (1 failure).
   - Stale comments fixed: the `ClockRecalibrationCause` summary, `WorkshopTrace.RecordTickDuration` and the allocation test's two tiering comments now say the cause is open (P0-034 / Story 16.1). `docs/engine-contracts.md` names the new cause.
   - R4-F2 stays a recorded Low.
5. **Evidence** (`evidence-9r-0/rebase/`): the clock red, green and three mutant logs, and the CA2000 control (`ca2000-filestream/`: the source, project and script stored as `.txt` so nothing compiles them, the per-setting result and the repository's `AllPaths` findings). *Process note:* these were copied into the repository from the scratchpad with `cp`, and the three control files renamed with `mv`, rather than written with the Write tool.
6. **Chrome finding, fixed.** The one full serial run on `6c8f7dc3` (served `godot.wasm` `508493f7…`) passed 33 of 34 files. `clock-master` test 4 failed: after the forced liveness fault (`CCGPU_TRANSPORT_FAILURE 3 SimulationUnresponsive`), the presentation that stopped with it and the scene's disposal each logged a second and third stderr fault (`CCGPU_PRESENTATION_EXCEPTION`, `CCGPU_DISPOSAL_EXCEPTION`). This story's round 1 had made those two boundaries log whole to stderr; main logged neither there. Both are consequences of the fault the transport already reported, so they are now `WorkshopDiagnostics.AfterRetirement` lines on stdout (`CCGPU_PRESENTATION_AFTER_RETIREMENT`, `CCGPU_DISPOSAL_AFTER_RETIREMENT`), decided by the same `TransportRetired` rule 9.4b uses to stop UI bindings, and by a `WorkshopTransportException` in a retired state for disposal. A failure on a live transport is still a stderr fault. Test-first: `ARetiredTransportsStoppedPresentationAndDisposalAreNotSecondFaults` (red 2/20, green 20/20) and the control `APresentationOrDisposalFailureOnALiveTransportIsAStderrFault`, which kills a widened presentation rule and a state-blind disposal rule. Re-verified in Chrome on the fixed candidate: `clock-master`, the smoke pair and the checklist probe (`evidence-9r-0/verification.txt`).
7. **anvil:** 0 warnings over every changed file. `docs/engine-contracts.md` line 241 (4,784 bytes, unchanged from main) is longer than anvil's secret-scan line limit, so that one line is not proven clean. The D23 sentence this story extends was split onto its own line so it stays within the limit.
