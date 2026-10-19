---
title: 'Story 9R.0b: AOT-Compiled WASM SIMD Workers'
type: 'engine'
created: '2026-10-19'
status: 'review'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/AGENTS.md'
  - '{project-root}/_bmad-output/planning-artifacts/epics.md#story-9r0b-aot-compiled-wasm-simd-workers'
  - '{project-root}/docs/gpu-f32-physics.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Both worker projects set `WasmEnableSIMD` but not `RunAOTCompilation`, so the shipped engine is .NET IL executed by the Mono WebAssembly interpreter. Story 9.4b measured physics ticks at 3–4 ms in Chrome, against about 0.08 ms native. *(Corrected by the design review, 19 Oct 2026: the Release publish already relinks each worker's `dotnet.native.wasm`, because `PublishTrimmed` sets `WasmBuildNative`; the earlier sentence "the worker bundles ship the runtime pack's prebuilt `dotnet.native.wasm` unchanged" was wrong. The relinked module still carries no AOT code: it is the interpreter plus the runtime.)*

**Approach:**
- Turn on Mono AOT, with SIMD and IL stripping, in both worker publishes.
- Add one shared post-publish guard that fails the publish unless:
  - every engine method was AOT-compiled;
  - engine IL was stripped;
  - the native module was relinked;
  - the Simulation's compiled physics code contains wasm SIMD instructions.
- Prove the change with ONE full runner run and before/after tick telemetry.

## Boundaries & Constraints

- **Always:**
  - Change only the two worker publishes. The pre-commit hook and the slnx build are untouched: `WasmBuildOnlyAfterPublish=true` keeps AOT out of Build.
  - A missing toolchain fails the publish with an explicit error.
  - Keep the existing trimming settings and `TrimmerRootAssembly`.
- **Never:**
  - an interpreter fallback flag;
  - a condition that disables AOT;
  - a per-method opt-out;
  - a CPU fallback path;
  - a guard that warns instead of failing.

## Changes

1. **Both worker projects.** In `CuriousContraptions.Simulation/CuriousContraptions.Simulation.csproj` and `CuriousContraptions.Animation.Worker/CuriousContraptions.Animation.Worker.csproj`, in the existing trimmed-publish `PropertyGroup`:
   ```xml
   <RunAOTCompilation>true</RunAOTCompilation>
   <WasmStripILAfterAOT>true</WasmStripILAfterAOT>
   <WasmEnableSIMD>true</WasmEnableSIMD>   <!-- already present; kept -->
   ```
   Then add `<Import Project="../CuriousContraptions.web/WorkerAot.targets" />`. In the Simulation csproj, also replace the misleading comment "SIMD stays on for wasm-simd128 physics" with one that names AOT.
2. **`CuriousContraptions.web/WorkerAot.targets`**, new and shared. It defines target `VerifyWorkerAot`, which runs `AfterTargets="Publish"`, and fails with `<Error>` when any of these holds:
   - **(a)** `RunAOTCompilation`, `WasmEnableSIMD` or `WasmStripILAfterAOT` is not `true`.
   - **(b)** The published `_framework/dotnet.native.wasm` has the same SHA-256 as the runtime pack's prebuilt `dotnet.native.wasm` (from the `Microsoft.NETCore.App.Runtime.Mono.browser-wasm` pack). A match means no AOT relink happened.
   - **(c)** The AOT compile reports a skipped (uncompiled) method in a `CuriousContraptions.*` namespace. Use Mono's skipped-method report, through the AOT task's extra-options hook. Confirm the property name in the Red step. If the SDK exposes none, parse the AOT task's per-method skip messages from the build log. Skipped framework methods are listed in the build output and recorded, not failed.
   - **(d)** Simulation only: the engine's AOT object (`CuriousContraptions.Simulation.dll.o` or `.bc` in the AOT intermediate directory; confirm the path) is missing, or has no `f32x4.`, `i32x4.` or `v128.` instruction in functions whose symbol names `WorkshopPhysicsEngine`, `PrismaticConstraintSolver` or `SoftConstraint`. This is checked with the emscripten pack's `llvm-objdump -d` (`~/.dotnet/packs/Microsoft.NET.Runtime.Emscripten.3.1.56.Sdk.osx-arm64/10.0.12/tools/bin/`, resolved through the SDK's emscripten path property, not hard-coded). The count is written to the build output as `WORKER_AOT_SIMD_INSTRUCTIONS=<n>`.

   (b), (c) and (d) together are the bundle inspection the AC asks for:
   - stripped IL means the interpreter cannot run engine methods;
   - no skipped engine methods means none fall back to it;
   - the relinked native module and its SIMD count show the compiled code ships.
3. **`CuriousContraptions.web/Program.cs` / the 2dog boot:** no change. The Mono runtime loads AOT code automatically.
4. **DESIGN.md** gets one line, plus the release-notes entry: first-load size and time before and after, as the AC requires.

## Toolchain

- `dotnet workload list` (19 Oct 2026) shows `wasm-tools` 10.0.112/10.0.100 on SDK 10.0.401.
- The installed packs (`~/.dotnet/packs`) include:
  - `Microsoft.NET.Runtime.Emscripten.3.1.56.{Sdk,Node,Python,Cache}.osx-arm64` (emcc, `llvm-objdump`, `wasm-opt`);
  - `Microsoft.NETCore.App.Runtime.AOT.osx-arm64.Cross.browser-wasm` (the Mono AOT cross-compiler).
- CI (`.github/workflows/pages.yml:20-21`) already installs `wasm-tools`.
- Nothing new is installed. A missing workload already fails the SDK with NETSDK1147 when `RunAOTCompilation` is true. The guard's (a) and (b) catch any path that silently skips AOT.

## Determinism (f32)

- **Unchanged:**
  - Native goldens and scene tests (`CuriousContraptions.tests`, run on CoreCLR) are not affected.
  - Inside the browser, exact Run/Reset, replay and Save/Load compare one engine with itself, so they stay bit-identical.
  - Saves store authored values only.
  - Wasm float semantics are deterministic across browsers (NaN payloads aside), which preserves `docs/gpu-f32-physics.md`'s cross-browser replay claim.
- **What may change, once:** browser physics results relative to the interpreter era, in the last bits.
  - `Vector3.Dot`, `Cross` and `Transform`, and `Vector128.Min` and `Max`, may lower to SIMD with a different reduction order or NaN and −0 handling.
  - `MathF.Sin`, `Cos`, `Exp` and `Pow` call the same emscripten libm in both modes.
  - LLVM does not contract into FMA without fast-math, and base wasm SIMD has no FMA.
- **The rule:** no checked-in Chrome assertion pins an interpreter-derived physics value. A grep shows only the animation endpoint `valBits === 15360` (half 1.0), which does not change.
  - The implementer re-greps `tools/e2e` for hex or bit literals and exact float equality on physics reads.
  - Any hit is re-derived in the AOT build, with a one-line justification in the review. Its tolerance is not widened.

## Build-time impact

- **Pre-commit hook and CI gate builds:** none. They build and never publish.
- **CI publish (`pages.yml:45`, `-c Release`) and every `tools/E2eRunner` publish:** AOT compile and native relink for two workers. Record the publish time before and after, taken from the runner's `publish.log` line and the CI step time.
- **Expected:** minutes, and more memory. If one runner publish exceeds 10 min, stop and raise OD1.

## Verification

- **Tier 1:**
  - `dotnet build CuriousContraptions.slnx` with 0 warnings under the 9R.0 gate;
  - `dotnet test CuriousContraptions.slnx` at 100%;
  - the Node harnesses and the contract tools.
- **Red proofs for the guard.** Each is a temporary property or edit, recorded and reverted:
  - `-p:RunAOTCompilation=false` fails (a); with (a) temporarily removed, it fails (b).
  - An emscripten path override pointing at an empty directory fails the publish explicitly.
  - One engine method made AOT-unfriendly (for example a `MakeGenericMethod` call) fails (c).
  - `-p:WasmEnableSIMD=false`, with (a) temporarily removed, fails (d).
- **Tier 3, a material engine change:** ONE full run, `dotnet run --project tools/E2eRunner -- full --out <evidence>/full`, at 100%. Every suite is reported with no skips, zero debt faults, and bundle and source identity recorded.
- **Telemetry (advisory):** tick time for the cat-023b wired chain and the largest Free-workshop fixture, before (main `385c3802`) and after. Report p50, p95 and max of `CCGPU_TICK_TIMES` (PLAYTEST build; see OD2). Also record bundle size and first-load time to `ready`.

## Deletes

- Interpreter execution of every engine method in both workers. The prebuilt runtime-pack `dotnet.native.wasm` is no longer shipped in the worker bundles.
- The misleading Simulation csproj comment.
- Any active-doc wording that says the workers run in the interpreter, or that SIMD is on without AOT. Historical evidence keeps its wording.
- Nothing else. No fallback is added that would need deleting later.

## Acceptance → proof

| AC | Proof |
|---|---|
| Both workers AOT with SIMD; no interpreter-only engine path, proven by build output and bundle inspection | `VerifyWorkerAot` (a)–(d) passes on the production publish; its output lines are recorded; red proofs |
| Missing toolchain fails explicitly | NETSDK1147 or the emscripten-path red proof; (b) catches any silent skip |
| Tick telemetry before and after | p50, p95 and max table for both scenes |
| `dotnet test` 100%; one full runner run 100% | Tier 1 logs; runner `summary.json` with verdict `Pass` |
| Bundle size and first-load time recorded; DESIGN.md or release notes updated | Size and time table; the doc line |

</frozen-after-approval>

## As built (19–20 Oct 2026, Amelia-5; supersedes Changes 1–2 where they differ, per the design review below)

- **Mode.** Both workers set `RunAOTCompilation`, `AOTMode=LLVMOnly`, `WasmStripILAfterAOT` and `WasmEnableSIMD`. `LLVMOnly` makes `driver-gen.c` define `EE_MODE_LLVMONLY`, so the runtime runs `MONO_AOT_MODE_LLVMONLY`: there is no interpreter execution mode. A method that was not compiled throws ("AOT method not found in llvmonly mode") instead of being interpreted. The SDK also refuses any forced-interpreter assembly under LLVMOnly (red proof below).
- **Guard.** `CuriousContraptions.web/WorkerAot.targets`, imported by both workers (`WorkerAotRole=Worker`) and the web host (`WorkerAotRole=Host`); the byte, disassembly and copy inspections are `tools/WorkerAotGuard` (C#, in the slnx under the 9R.0 gate, 17 unit tests in `tools/WorkerAotGuard.Tests`). Targets, all pinned in `CSharpStaticGateTests`:
  - `VerifyWorkerAotProperties` (BeforeTargets `WasmTriggerPublishApp`): **(a)** the four properties.
  - `VerifyWorkerAotCompile` (AfterTargets `_WasmAotCompileApp`, nested publish): **(b)** the publish AOT-compiled; `driver-gen.c` registers `mono_aot_module_<asm>_info` for every `CuriousContraptions.*` assembly (FlatBuffers included); **(c)** `AOTMode` is LLVMOnly and `driver-gen.c` selects `EE_MODE_LLVMONLY`; no forced-interpreter assembly; each engine assembly is the IL-stripped copy and differs from its AOT input.
  - `VerifyWorkerAotLink` (AfterTargets `_BrowserWasmLinkDotNet`, nested publish): **(b)** each `<asm>.dll.o` is a native link input; **(d)** Simulation: `llvm-objdump -d` of `CuriousContraptions.Simulation.dll.o` has f32x4 arithmetic (add, sub, mul, div, min, max, sqrt) in each of `WorkshopPhysicsEngine`, `PrismaticConstraintSolver` and `SoftConstraint` (per-function counts written to `worker-aot-simd.txt`); every worker: the AOT compile was given `mattr=simd`.
  - `VerifyWorkerAot` (AfterTargets `WasmTriggerPublishApp`): **(b)** the shipped `_framework/dotnet.native.wasm` is byte-identical to this publish's link output and carries every engine AOT module name.
  - `VerifyWorkerAotHostBundle` (host, AfterTargets `TwoDogWebPrecompressBundle`): the host's final `simulation/` and `animation/` files are byte-identical to the verified worker bundles, and every `.br`/`.gz` sibling decompresses to its file.
- **Skip report.** `print-skipped` is added to the AOT arguments; Mono's skip lines (shared-generic canonical forms, `gshared failure`) appear in the detailed log of a non-cached compile. They are not failures under LLVMOnly: their concrete instances are compiled into `aot-instances` (checked: `WorkshopInstances.Find<WorkshopBall|Lamp|Receiver|Switch>`, FlatBuffers `Put/Add/ToArray<byte>`, `Offset<InstanceRecord>`), and a missing instance throws when called.
- **Debug publish is unsupported:** the SDK fails it with "AOT is not supported in debug configuration" (recorded); `CuriousContraptions.web/Directory.Build.props` says so.

## Design review (adversarial, 10 min)

| Attack | Handling |
|---|---|
| Mono on wasm is mixed-mode: methods AOT cannot compile silently run in the interpreter | Guard (c) fails the publish on any skipped engine method, and stripped IL (a) means a skipped method would have nothing to run |
| AOT on, but the bundle still ships the prebuilt runtime (no relink) | Guard (b) compares hashes against the runtime pack |
| SIMD in the binary comes from the C runtime, not the engine | (d) counts only engine-named functions in the engine's own AOT object |
| AOT needs `JSExport`/`JSImport` reflection | They are source-generated interop, AOT-compatible by design; `TrimmerRootAssembly` stays. The full run exercises every entry point |
| FlatBuffers reflection or generics | The vendored project is `IsTrimmable` with trim warnings as errors (Story 9.4b); any generic AOT cannot compile shows up in (c) |
| `Enum.ToString`/`IsDefined` in boundary code | Metadata survives IL stripping; the full run covers these paths |
| f32 results differ from the interpreter era | Accepted, once (see Determinism); in-browser determinism unaffected; grep rule for pinned values |
| NaN or −0 semantics of `Vector128.Min/Max` change | Same managed semantics; the game-grade envelope clamps; Run/Reset compares within one engine |
| A Debug publish skips AOT | The properties are unconditional, and the guard runs on every publish configuration |
| The guard breaks on an SDK update (paths, property names) | It fails loudly. Paths come from SDK properties; the Red step confirms names |
| Publish time slows every Tier 2 runner run | Measured and recorded; OD1 above 10 min |
| Larger first load on mobile | Recorded; DESIGN.md line; brotli assets already ship |
| AOT changes timing enough to change timing-suite behaviour | The one full run covers it; clock-master stays run-alone |
| Telemetry needs the PLAYTEST build, but gates are production-only | Telemetry is advisory and never a gate (OD2) |

## Design review round 2 (19 Oct 2026, forwarded by the coordinator): dispositions

| # | Finding | Disposition |
|---|---|---|
| 1 | Guard (b) is vacuous: Release already relinks | **Fixed.** Measured: the 385c3802 worker modules hash differently from the pack's prebuilt one and carry no `CuriousContraptions` string. (b) is now AOT-only evidence: `driver-gen.c` registration of every engine module, each `<asm>.dll.o` in `@(_WasmNativeFileForLinking)`, the shipped module identical to the link output and carrying every module name. Red: `RunAOTCompilation=false` with (a) disabled fails (b); with that check also disabled, `driver-gen.c is missing` (b) catches it; the pack's prebuilt module fails the module-name check. Problem statement corrected. |
| 2 | The guard runs too early; check host folders and siblings | **Fixed.** Nested-publish targets after `_WasmAotCompileApp` and `_BrowserWasmLinkDotNet`; outer after `WasmTriggerPublishApp`; host after `TwoDogWebPrecompressBundle` compares every worker file and each `.br`/`.gz`. Every red ran on a clean `for-publish` directory (moved aside). Red: a planted stale `.br` fails the host check; a planted prebuilt module in the worker bundle fails (b) on an incremental host publish. |
| 3 | Skipped methods are still interpreted under LLVMOnlyInterp | **LLVMOnly adopted** (coordinator's first choice): it builds, cat-023b passes, and the full run is the proof. No interpreter mode exists, so the skip-fail rule (the coordinator's fallback) does not apply. (c) asserts the mode (`AOTMode` and `EE_MODE_LLVMONLY`) and that `_AOT_InternalForceInterpretAssemblies` is empty; the SDK itself rejects a forced-interpreter assembly under LLVMOnly. The `MONO_LOG_MASK=aot` runtime proof was for the fallback branch and is not run. |
| 4 | Red for (c) with a real skip construct | Under LLVMOnly a skipped method cannot run interpreted, so the reds for (c) are the ways to get interpretation back: `AOTMode=LLVMOnlyInterp` (fails (c) mode; with that check disabled, the `EE_MODE_LLVMONLY` check fails; with both disabled the publish passes, proving they are load-bearing), a forced-interpreter assembly (SDK error), and `WasmStripILAfterAOT=false` (fails the stripped check; with it disabled, passes). Every failure text is the guard's `WorkerAot (x):`. |
| 5 | SIMD proof on `.o`; per-function counts; real IL-stripped check | **Fixed.** `.o` disassembly, f32x4 add/sub/mul/div/min/max/sqrt, counted per function of each named type (exact type segment: `SoftConstraintCoefficients` is not `SoftConstraint`), each type must be > 0: WorkshopPhysicsEngine 3, PrismaticConstraintSolver 55, SoftConstraint 8 (66 total; `evidence-9r-0b/simd/worker-aot-simd.txt`). Red: `WasmEnableSIMD=false` gives 0 in each type and fails (d). **Stripped is not smaller:** ILStrip rewrites compiled bodies in place, so the stripped assemblies are the same size (Simulation 874,496 B, 265,304 B rewritten). The check is that the linked engine assembly is the `stripped/` copy and differs from its AOT input. 118 Simulation methods (try/catch, async state machines and similar) are not strip-eligible and keep their IL bytes; each sampled one has a compiled symbol in the `.o`. The no-interpreter guarantee comes from `EE_MODE_LLVMONLY`, not from stripping, so Change 2's sentence "stripped IL means the interpreter cannot run engine methods" is superseded. |
| 6 | Determinism: main thread interpreted, workers AOT | **Audited; no fix needed.** The tick-0 bitwise admission (`PhysicsBodyReadSet.ValidateScene`: `F32Bits`/`HalfBits`/`HasSameBits`) and `ContactWork` `Remaining.HasSameBits(InitialEnergy)` run in `WorkshopSimulation`, which only the Simulation worker instantiates: worker against its own compiled scene. Main-thread checks (`BrowserWorkshopClient.*`) compare worker samples with worker-originated echoes (`HalfBits` on event phases and pulse durations), compare energies by order against authored values copied into the main-compiled scene (Bumper preload; `Capacity × InitialFraction` is one correctly rounded multiply), and evaluate goals in integer fixed point. None compares bits of a value both sides compute independently. `Vector128.Min/Max` in `SoftConstraint.Solve4` lowers to compare and `bitselect` sequences, not `f32x4.pmin/pmax`, so the managed NaN and −0 semantics are kept. `tools/e2e` re-grep: only animation endpoint `valBits === 15360` (Half 1.0). |
| 7 | Debug publish unsupported | **Done.** Recorded SDK error; `CuriousContraptions.web/Directory.Build.props` comment fixed. |
| 8 | CI publish time and `timeout-minutes` | Local clean publish measured (see Evidence). The CI step time cannot be measured without a push (this story never pushes), so `timeout-minutes: 30` stays; the local AOT publish is well under it. The runner's calibrated N and durations were measured under the interpreter; recalibration is filed to Story F.1 unless the full run fails. |
| 9 | Animation worker SIMD | Its SIMD proof is the recorded `mattr=simd` argument (`WORKER_AOT_COMPILE … arguments=…;mattr=simd`), enforced by (d) for every worker. `Math.FusedMultiplyAdd` on doubles lowers to emscripten `fma` in both modes. |

## Evidence

All under `_bmad-output/implementation-artifacts/evidence-9r-0b/`: `before/` (385c3802 publish log and tick captures), `red-green/` (every red, mutant and green publish log with its summary), `simd/` (per-function counts, both `driver-gen.c`), `telemetry/` (capture and summary scripts, OD2: manual, advisory), `after/`, `tier1/` and `full-run/`.

**Tick telemetry (advisory, production bundles, `tools/Preview` + `node --test` with `telemetry/tick-capture.mjs`; ms per physics tick from `CCGPU_TICK_TIMES`, first Run of each suite).** The largest Free-workshop fixture is cat-020's wired construction (Counter, Powered Gate, Ball Detector, Basketball, Battery and Lamp, with activation and electrical wiring): the most placed parts of any Free-workshop suite (18 placements), and the slowest interpreter ticks among the Free-workshop suites measured (cat-030, cat-020, cat-002, cat-005; `before/ticks-candidates.jsonl`). Every capture shares one fixed-size construction encoding (5,768 B), so that field does not rank scene size.

| Scene | Before 385c3802 (interpreter) p50 / p95 / max | After 5397956e (LLVMOnly AOT) p50 / p95 / max |
|---|---|---|
| cat-023b wired domino chain (355–361 ticks) | 3.910 / 6.520 / 22.240 | 0.540 / 0.740 / 0.985 |
| cat-020 Free-workshop fixture (434–436 ticks) | 2.435 / 5.845 / 30.750 | 0.455 / 0.630 / 1.010 |

**Bundle and first load.** Worker `dotnet.native.wasm`: Simulation 1,486,004 B → 10,406,654 B (brotli 561,356 → 2,976,935), Animation 1,486,038 B → 9,989,386 B (brotli 2,847,936 after). Whole AppBundle including `.br`/`.gz` siblings: 120,944 KB → 146,744 KB. Driver launch to `ready` (local Preview, uncompressed): 4,133–4,367 ms before, 4,239–4,273 ms after.

**Publish time.** Clean host publish 67 s before (fresh worktree) and 117 s after (worker and host `obj`/`bin` moved aside); runner publish 33.7 s (9R.0a full run) and 78.1 s (this full run). Below the 10-minute OD1 threshold.

**Full run.** `full-run/summary.json`: verdict Pass on `5397956e`, 34 files, 166/166, 0 skipped or todo, bundle identity unchanged start to end, 1,060.6 s. clock-master's single debt fault is its injected-load proof (identical counts to the 9R.0a baseline run).

## Open decisions

- **OD1. Publish time.** If one runner publish exceeds 10 min, the owner chooses whether the runner may reuse an identical-source publish. Recommendation: decide only if it is measured above the threshold.
- **OD2. Tick telemetry route.** `CCGPU_TICK_TIMES` exists only in the `PlaytestDiagnostics` build, and Story 9R.0a says Chrome runs only through the runner, which publishes production only. Recommendation: allow one recorded manual PLAYTEST publish plus the two suites, run through `tools/Preview` and `node --test`, labelled telemetry and never a gate, for this story only. The alternative is a runner `--playtest` option with a non-Pass `Telemetry` verdict. *(Coordinator: allowed. As run: `CCGPU_TICK_TIMES_*` is printed outside `#if PLAYTEST` (`WorkshopTrace.FlushSealed`), so the captures came from the production bundle itself, through `tools/Preview` and `node --test` with `telemetry/tick-capture.mjs` preloaded; no PLAYTEST publish was needed.)*
