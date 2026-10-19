# Review: Story 9R.0b, AOT-Compiled WASM SIMD Workers (Murdoch-6, round 1)

**Terminal verdict for round 1: PASS (scoped).** Every story AC is met at `7a8a48e4`. No blocking finding is open, and nothing I found is a player-visible regression. Everything below is filed.

- **Candidate:** `5397956e` (feat) and `7a8a48e4` (docs and evidence) on base `385c3802`, in worktree `agent-ac38d5d5435e6a458`.
- **Reviewer:** Murdoch-6, independent. I edited no deliverable. The only edits were temporary mutants in a scratch worktree, which has been removed.
- **Blocking rule (owner, 19 Oct 2026):** at most 2 rounds. Only a breach of the story's ACs, or a player-visible regression it introduces, blocks.
- **Scope:** this verdict covers the two worker publishes, the guard, and the evidence. It is not a global or runtime qualification claim; formal performance stays at P0-034.
- **Not reviewed:** after `7a8a48e4`, the implementer's worktree has uncommitted edits: `WorkerAot.targets`, the Simulation csproj, `Checks.cs` and `Program.cs`, plus new files `SkipReport.cs` and `SkipReportTests.cs`. They are not part of this candidate. If they are committed, they form a new candidate for round 2.
- **Raw evidence:** `_bmad-output/implementation-artifacts/evidence-9r-0b-review-murdoch6/`, which holds `tier1/`, `tier2/`, `red/`, `aot-coverage/` and `probes/`.

## Story AC matrix

| AC | Evidence (independent unless marked reused) | Status |
|---|---|---|
| 1. Both workers are AOT-compiled with SIMD, with no interpreter-only engine path, proven by build output and bundle inspection | **My clean publish** (Tier 2 run below) printed:<ul><li>`AOTMode=LLVMOnly` for both workers;</li><li>`WORKER_AOT_MODULES` with 4 engine modules per worker;</li><li>`WORKER_AOT_SIMD_INSTRUCTIONS=66` (WorkshopPhysicsEngine 3, PrismaticConstraintSolver 55, SoftConstraint 8);</li><li>`WORKER_AOT_HOST_FILES=142`.</li></ul>**My coverage probe** (`probes/AotCoverage.cs`) checked every CuriousContraptions.* assembly in both workers. Each method with an IL body is either in Mono's compiled-methods token list or matches a defined symbol in `llvm-nm` of its `.o`/`aot-instances.dll.o` by name substring. An overload or lambda symbol could satisfy a needle, so the full-run pass is what backs this result:<ul><li>Simulation: 5200 = 5082 + 118. All 118 non-strip-eligible methods have symbols.</li><li>Animation.Worker: 4951 = 4875 + 76.</li><li>FlatBuffers: 153 = 138 + 14 + 1. The 1 is ``Offset`1::.ctor``, which compiles as `Offset_1_T_GSHAREDVT__ctor_int` (gsharedvt serves any T).</li><li>Animation and Abstractions: complete.</li><li>**0 unresolved.**</li></ul>**Generic call sites:**<ul><li>`Find<T>` has exactly 4 instantiations in source (Ball, Receiver, Switch, Lamp), and all 4 have `aot_instances_*` symbols.</li><li>`ByteBuffer.ArraySize<T>` has `byte` and `Offset<InstanceRecord>` instances.</li></ul>**Red mutants:** see "Red proofs" below; all killed | Met |
| 2. The build fails explicitly without the AOT toolchain, with no silent fallback | <ul><li>An empty-emsdk override fails at evaluation with MSB4184 (implementer red; the log is read).</li><li>A missing workload fails with the SDK's NETSDK1147. That is documented SDK behaviour and was not demonstrated, because the workload cannot be uninstalled here.</li><li>Silent-skip paths: my reds R1, R2, R4 and R5 fail with `WorkerAot (a)`, `(b)` and `(c)`.</li></ul>No fallback flag or condition exists in the diff | Met (the error-text weakness is filed as F8) |
| 3. Tick telemetry before and after, for cat-023b and the largest Free-workshop fixture | I re-ran `telemetry/tick-summary.mjs` on the raw captures and reproduced the spec's table exactly:<ul><li>cat-023b: 3.910 / 6.520 / 22.240 ms → 0.540 / 0.740 / 0.985 ms;</li><li>cat-020: 2.435 / 5.845 / 30.750 ms → 0.455 / 0.630 / 1.010 ms.</li></ul>The captures are reused, not recaptured. This AC is advisory | Met (fixture choice filed as F9) |
| 4. `dotnet test` 100%, and one full runner run 100% against the AOT preview | **Tier 1 on clean `7a8a48e4`:** WorkerAotGuard.Tests 17/17, Coverage.Tests 78/78, CuriousContraptions.tests 1692/1692, E2eRunner.Tests 156/156.<br>**Full run reused:** `full-run/summary.json`, run `20261019T161820Z-76508`. Pass, 34 files, 166/166, bundle `bab39c89…` at start and end, `sourceHead` `5397956e`.<br>**Identity:** `git diff --name-only 5397956e..7a8a48e4` touches only `_bmad-output/`. The run's status digest `078af40d…` is not the empty-status hash, so the tree was dirty during the run. An mtime scan of the implementer worktree over 00:16–01:30 finds no tracked, non-ignored source file outside `_bmad-output/` modified in that window; only Godot `.import` and `.godot` churn appears, and `assets/` is identical to `5397956e`. The dirt was therefore evidence only, and the reuse holds. The committed `full-run/publish.log` carries the guard lines itself (SIMD 66, HOST_FILES 142, LLVMOnly ×2) | Met |
| 5. Bundle size and first-load time are recorded, and DESIGN.md states the change | <ul><li>DESIGN.md:23 has the line.</li><li>My publish matches it: Simulation `dotnet.native.wasm` 10,406,654 B (br 2,977,879), Animation 9,989,386 B (br 2,849,498), AppBundle 146,748 KB.</li><li>The raw `.wasm` sizes are byte-identical to the implementer's; the brotli outputs differ by about 1 KB.</li></ul> | Met |

## Coordinator design-review requirements

| Requirement | Finding | Status |
|---|---|---|
| The guard's AOT evidence is real (`driver-gen.c` registration, `.dll.o` link inputs, red when AOT is off) | See `WorkerAot.targets` `VerifyWorkerAotCompile` and `VerifyWorkerAotLink`.<ul><li>R1: `-p:RunAOTCompilation=false` fails `WorkerAot (a)`.</li><li>R2: with (a) mutated off, the same publish fails `WorkerAot (b): this publish did not AOT-compile the worker`.</li><li>Implementer reds: (b) "driver-gen.c is missing" when `_WasmShouldAOT` is bypassed; the prebuilt pack module fails the module-name check</li></ul> | Met |
| The guard runs after `WasmTriggerPublishApp` and checks the final `_framework` folders, including `.br` and `.gz` | <ul><li>`VerifyWorkerAot` runs AfterTargets `WasmTriggerPublishApp`.</li><li>The host check runs after `TwoDogWebPrecompressBundle`: `HostCopies` compares bytes and decompresses each sibling. My CI-equivalent publish shows 142 files.</li><li>Stale-`.br` red: the implementer's `red-host-stale-br.log` (read, not re-run)</li></ul> | Met (M2 and L4 filed) |
| `LLVMOnly`, or else the skipped-method proof | `LLVMOnly` and `EE_MODE_LLVMONLY` are asserted. R4: `-p:AOTMode=LLVMOnlyInterp` with (a) off fails `WorkerAot (c)`. The coverage probe additionally shows no uncompiled engine method | Met |
| Per-function f32x4 arithmetic counts above zero, measured on `.o` | My green run shows 66 per function (`worker-aot-simd.txt`). R3: `WasmEnableSIMD=false` with (a) and the `mattr` check both mutated off gives 0 in all three types and fails `WorkerAot (d)` | Met (thin coverage filed as F7) |
| IL actually stripped | R5: `WasmStripILAfterAOT=false` with (a) off fails `WorkerAot (c) … still carries its IL` for all 4 engine assemblies | Met |
| Every cross-thread float comparison audited, none comparing bits | **The spec's audit is inaccurate.** `engine/MachineWorld.Gpu.cs:311-318` (main thread, interpreted) recompiles the scene with `WorkshopPhysicsCompiler.Compile`. Then `PhysicsBodyReadSet.ValidateScene:84-91` compares the worker's tick-0 `Local`, `Velocity` and `Rotation` **bits** against it. For the Springboard plate, those values come from `Vector3.Transform(v, rotation)`, computed independently on each side (`WorkshopPhysicsCompiler.cs:163-169`). The full analysis and its rating are F1 | Not as claimed; **filed (F1), not blocking** |
| Debug publish documented as unsupported | <ul><li>`CuriousContraptions.web/Directory.Build.props` comment.</li><li>`red-debug-publish.log`: "AOT is not supported in debug configuration".</li><li>No script runs a Debug worker publish; the one `Configuration=Debug` build is the editor-metadata build, which is unrelated</li></ul> | Met |
| CI timeout justified | `timeout-minutes: 30` is unchanged.<ul><li>The local clean host publish took 107 s, as the runner's publish step (16 cores).</li><li>The CI runner has 4 vCPU, and the CI step time is unmeasured.</li><li>`llvm-objdump` is present in the osx-arm64 Emscripten pack; for the linux-x64 pack this is inferred, not verified</li></ul> | Reasoned, not measured; **filed (F5)** |
| The tick-time claim is backed by telemetry | Reproduced from the raw captures (AC3) | Met |

## Attacks

**Can `LLVMOnly` crash on an uncompiled path that the suites never exercise?** Not found.

- **Engine methods.** Every engine method has compiled code (AC1 probe).
- **`JSExport` entry points.** All 30 are compiled. They share one wrapper signature, so the exercised ones prove the invoke path.
- **Reflection.** Worker code has no `MakeGenericType`, `MakeGenericMethod`, `Activator`, `Expression` or reflection invoke (checked with `rg`).
- **Enum helpers.** The `Enum.IsDefined<T>`, `Enum.GetName<T>` and `Enum.GetValues<T>` instantiations are static, so they are AOT instances.
- **Chrome logs.** My Tier 2 run and the full run contain no `llvmonly`, `AOT method not found`, `ExecutionEngineException` or `Attempting to JIT`.
- **Residual risk 1, framework-internal instantiations.** Dynamic generic instantiation inside the framework (for example, comparer creation over engine structs) is not exhaustively enumerated. The covered paths run in every suite.
- **Residual risk 2, worker exceptions.** A worker-side throw and catch under LLVMOnly is not exercised by any suite. cat-005 test 7 and cat-062 test 7 reject in Node or on the main thread. This is F4.

**Do exact Run/Reset and Save/Load stay bit-identical in the AOT build?** Yes, within this coverage:

- **cat-023b, in my Tier 2 run (test 4, Pass).** The end Domino's complete tick-0 worker read equals the authored read byte-for-byte after Save, reload and Load, and again after Reset (`assertEndRestored`, `cat-023b.test.ts:131-145`). Orientation is bit-identity (`:90`, `:140`).
- **Every suite.** Each Run and Reset passes the main-thread tick-0 bit check of all dynamic bodies against the canonical scene (F1). It is an implicit bit-exactness oracle, and it passed in all 166 full-run tests.
- **Broader coverage.** A full-construction snapshot is Story 9R.1's scope.

## Tier 1 (clean `git worktree add --detach … 7a8a48e4`, `murdoch6-9r0b`)

| Check | Result |
|---|---|
| `dotnet build CuriousContraptions.slnx` | 0 Warning(s), 0 Error(s) |
| `dotnet build CuriousContraptions.slnx -p:PlaytestDiagnostics=true` | 0 Warning(s), 0 Error(s) |
| `dotnet test CuriousContraptions.slnx` | 17/17, 78/78, 1692/1692 (including `CSharpStaticGateTests`) and 156/156 |
| `node --experimental-vm-modules --test tools/*.test.mjs` | 82/82, 0 skipped, 0 todo |
| `tools/LifecycleContract`, `tools/WireContract`, `tools/Ownership -- --oracles` | Failures 0; Failures 0; 8 Positive, 37 Rejected (my first Ownership call omitted the operation argument and was rerun) |
| `anvil check --changed` / `anvil check --since 385c3802` | Nothing changed / 2 files (`telemetry/*.mjs`), no warnings. `.targets` and `.csproj` are outside anvil's extensions |
| `git status` before and after | Clean (0 entries) |

## Tier 2 (runner, my own publish)

Command: `dotnet run --project tools/E2eRunner -- run --changed tier2/changed.txt`. The changed list held `cat-023b.test.ts` and `clock-master.test.ts`.

- **The run:** `20261019T165250Z-41361`. **Pass**: 4 files, 19/19 tests:
  - clock-master 9/9, with its one expected injected debt fault;
  - cat-023b 4/4;
  - engine-core-2a1 5/5;
  - engine-core-2b1 1/1.
- **Identity:**
  - `sourceHead` `7a8a48e4`;
  - `sourceStatusDigest` `e3b0c442…`, which is the empty-status hash, so the tree was clean;
  - suite `940c8df9…` and runner `0fb80f87…`, the same as the full run;
  - bundle `81abcec6…` at start and end, with both served checks passing.
- **Publish:** 107.0 s. The guard tool's `obj/` and `bin/` were moved aside first, and it built from scratch (`WorkerAotGuard -> …/bin/Release/…`). L6 did not reproduce.

## Red proofs (temporary mutants in a scratch worktree `murdoch6-red`, Simulation worker publish, now removed)

| # | Targets | Arguments | Result |
|---|---|---|---|
| R0 | unmodified (`4c91373b…`, the same hash as the implementer's reds) | none | Pass, SIMD 66 |
| R1 | unmodified | `RunAOTCompilation=false` | **Killed:** `WorkerAot (a)` |
| R2 | (a) off (`7bf6a91f…`) | `RunAOTCompilation=false` | **Killed:** `WorkerAot (b)`, did not AOT-compile |
| R4 | (a) off | `AOTMode=LLVMOnlyInterp` | **Killed:** `WorkerAot (c)`, AOTMode |
| R5 | (a) off | `WasmStripILAfterAOT=false` | **Killed:** `WorkerAot (c)`, still carries its IL (×4) |
| R3 | (a) and the `mattr` check off (`eabd3dd9…`) | `WasmEnableSIMD=false` | **Killed:** `WorkerAot (d)`, 0 f32x4 in each of the 3 types |

## Lens findings (forwarded by the coordinator), verified

- **M1: `print-skipped` only logs, and the guard does not fail on a skip.** True as code. Today the skips are 1414 shared-generic canonical forms, and the coverage probe finds no engine method without compiled code. This is no breach and no regression. **Filed (F2):** the guard should enforce the probe's rule, so a future uncompiled method fails the publish instead of throwing at runtime.
- **M2: the nested checks hang off SDK-private targets, and nothing checks that they ran.** True. If `_WasmAotCompileApp` or `_BrowserWasmLinkDotNet` is renamed, or `WasmBuildingForNestedPublish` changes, (c) and (d) stop silently. (a), the outer native-module check and the host check still run. Today all of them run; their lines appear in my publish. The spec's design-review row that says it "fails loudly" is wrong. **Filed (F3):** the outer target should require the nested reports.
- **M3: the guard's tests are not run by CI or the hook.** True. `.githooks/pre-commit:52` and `pages.yml` run only `CuriousContraptions.tests`. That is the same pre-existing gap as Coverage.Tests and E2eRunner.Tests. The guard itself runs in the CI publish. **Filed (F6).**
- **L4: `HostCopies` ignores extra stale files.** True. Files left over in `simulation/` or `animation/` are not checked. The game does not load unlisted files. **Filed (F10).**
- **L5: `OwnerType` assumes a single-segment namespace.** True. A deeper namespace gives a count of 0, so (d) fails closed. **Filed (F11).**
- **L6: Restore then Build reuses one evaluation.** Not reproduced: the guard built from a moved-aside clean state. Latent only, and the project has no packages. **Filed (F11).**

## Filed (non-blocking); owner: a new back-of-queue story unless stated

1. **F1, cross-thread bitwise tick-0 check.**
   - **The comparison.** `MachineWorld.ValidateWorkshopRead` (`MachineWorld.Gpu.cs:311-318`) runs on the interpreted main thread. It compiles its own scene and compares bits against the AOT worker's tick-0 read. The Springboard plate pose is `Vector3.Transform` on both sides.
   - **Why it is not a known violation:**
     - **IL:** the Mono CoreLib IL of `Vector4.Transform(Vector4, Quaternion)` is `Quaternion` multiply ×2, which is lane-wise `Vector128` multiply and `MultiplyAddEstimate` (`probes/IlCalls.cs`). On the interpreter side, `Single.MultiplyAddEstimate` is a 6-byte unfused multiply-then-add.
     - **Compiled code:** in the shipped AOT objects, the compiled `corlib_System_Numerics_Quaternion_op_Multiply` body has 7 `f32x4.mul`, 3 `f32x4.add`, 8 `v128.const`, 2 `v128.load`, 1 `v128.store` and no call (`llvm-objdump -d`). `System.Private.CoreLib.dll.o`, `CuriousContraptions.Simulation.dll.o`, `System.Numerics.Vectors.dll.o` and `aot-instances.dll.o` contain no `fma` or `relaxed_madd` instruction and no undefined `fma` symbol (`llvm-nm -u`).
     - **Conclusion:** both sides run the same unfused IEEE operation sequence.
     - **Empirically:** `WorkshopInput.Position` keeps `Local` as the raw f32 remainder, with no snapping (`WorkshopConstruction.cs:314-320`). So the tilted Springboards in cat-062 and cat-062b, which pass this check in the AOT full run, are real bit-identity evidence.
   - **Actions:**
     - Correct the spec's audit (design review #6).
     - Either validate against worker-echoed values, or add a native and Chrome property test over the reachable rotations.
   - **Priority:** front of the filed list. A mismatch would make Run fail for the player.
2. **F2 (M1):** the guard should fail on any engine method without compiled code. The implementer's uncommitted `SkipReport.cs` appears to start this; it is unreviewed.
3. **F3 (M2):** require the nested guard reports from the outer target, and correct the spec's "fails loudly" row.
4. **F4:** no seeded worker-side throw and catch runs in Chrome under LLVMOnly. Add one probe.
5. **F5:** record the first CI publish-step duration and peak memory on push, then reassess `timeout-minutes: 30`. Confirm `llvm-objdump` is in the linux-x64 Emscripten pack.
6. **F6 (M3):** run `WorkerAotGuard.Tests` in the hook and CI, along with the other tool test projects.
7. **F7:** WorkshopPhysicsEngine has only 3 f32x4 operations, all in `EmitContactEvents`, and the solver core is mostly scalar. That is not an AC matter; it is performance work for P0-034.
8. **F8:** a missing toolchain fails with an SDK path error (MSB4184) or NETSDK1147, not a message that names `wasm-tools`. Consider a guard pre-check with a named message.
9. **F9:** "largest Free-workshop fixture" rests on measured tick cost. cat-030 has 19 file-wide `placeOnCanvas` calls against cat-020's 18, which contradicts the spec's "most placed parts" sentence. cat-030 has no AOT capture.
10. **F10 (L4):** the host check ignores extra stale files in the worker folders.
11. **F11 (L5, L6):** `OwnerType` namespace shape; the guard's Restore and Build evaluation reuse.
12. **F12, uncommitted evidence.** These are untracked in the implementer worktree:
    - `evidence-9r-0b/aot-coverage/`;
    - `after/publish-fresh.log`;
    - `red-green/red-toolchain-empty-emsdk-dir.*`.

    `red-toolchain-no-aot-cross-compiler.log` was present earlier and has since disappeared. Commit what is cited, or drop the citations.

## Verdict

**PASS (scoped), round 1.**

- **ACs:** all five are met at `7a8a48e4`.
- **Coordinator requirements:** all verified, except the cross-thread audit, whose claim is wrong. That comparison is analysed and empirically holding, and it is filed as F1.
- **Regressions:** none player-visible.
- **Records to commit:** the coordinator should commit this record together with `evidence-9r-0b-review-murdoch6/`, both untracked in the main checkout.
- **Worktree:** the clean review worktree `.claude/worktrees/murdoch6-9r0b` is kept for a possible round 2 and can be removed afterwards.
