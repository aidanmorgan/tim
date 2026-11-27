---
title: 'One schema file is the ABI: TypeScript generated in every build graph, pinned at the root and proven to agree with C# (S5c-2)'
type: 'refactor'
created: '2026-11-26'
status: 'done'
baseline_revision: 'cb29a0763d2bba289e5018b82698e3a7f86478a3'
review_loop_iteration: 3
followup_review_recommended: false
context:
  - '{project-root}/_bmad/custom/element-free-engine-design.md'
  - '{project-root}/_bmad/custom/element-free-engine-review.md'
  - '{project-root}/schemas/README.md'
  - '{project-root}/docs/testing-guidance.md'
  - '{project-root}/docs/delivery-workflow.md'
  - '{project-root}/_bmad-output/implementation-artifacts/spec-s5c-one-wire-schema-file.md'
  - '{project-root}/_bmad-output/implementation-artifacts/spec-s5b-generate-typescript-wire-in-build.md'
  - '{project-root}/docs/planning/elements/readiness/owner-decisions-2026-11-22.md'
warnings: []
deferred:
  - summary: >-
      Add extra populated TypeScript airflow response coverage at the applicable later qualification gate.
    evidence: |-
      E14 VG3-3. The writer and its empty baseline fixture are outside the pass-two repair and direct effects. The selected-field matrix and native airflow codec proof still apply.
    location: >-
      tools/runtime/wire-conformance.ts
    severity: low
---

<intent-contract>

## Intent

**Problem:** Owner decision 24p (25 November 2026) asks for one schema file "that the dotnet and typescript bindings are generated from", proven to agree. After S5c-1 ([contract](spec-s5c-one-wire-schema-file.md)) the ABI is one file with its root identifiers as schema data, and C# takes them from generated code. Three gaps remain:
- S5b generates TypeScript only in the Debug `CuriousContraptions.FlatBuffers` instance, which has no build-order relation to the runtime TypeScript compile in the `CuriousContraptions.Abstractions` instance, so the generated module is missing or stale for that compile, including in CI's Release graph.
- No required run executes the generated TypeScript (S5b's stated residual), so nothing proves that the C# and TypeScript bindings agree. The `flatbuffers` 25.2.10 pin lives in `tools/e2e/package.json` (S5b coordinator decision B).
- TypeScript keeps hand identifier constants and its own frame check (`tools/e2e/workshop-wire.ts:14-16`, `:20-29`; H-S3).

This is the second of the three S5c slices (coordinator ruling, 26 November 2026); S5c-3 ([contract](spec-s5c-3-remove-hand-schema-restatements.md)) follows.

**Approach:**
- **Generation moves.** TypeScript generation joins the Abstractions runtime chain before the runtime compile, reads the one schema file with `--gen-object-api`, and S5b's vendored-project target, its staging properties and the schema record `schemas.txt` are deleted (D3).
- **One compiler definition.** The compiler pin, provisioning and the schema path move to one shared file, `tools/runtime/FlatcCompiler.targets`, read by both generation steps.
- **Cross-language conformance.** A required test runs both directions, for every root, every command and response kind, and every observation payload arm: a strict-TypeScript node script, compiled by the pinned `tsc` in the normal build, decodes and re-encodes buffers that the C# codecs wrote, and the C# production codecs decode what TypeScript wrote. It asserts decoded semantic fields and checks that both sides refuse malformed frames. For the response root, the generated typed builder retains source motion vector presence (D11).
- **One TypeScript frame check.** `tools/runtime/wire-frame.ts` holds the one TypeScript frame check and identifier conversion; `workshop-wire.ts`'s own are deleted (H-S3).
- **Runtime pin.** The exact `flatbuffers` 25.2.10 pin moves to the root `package.json` and lock, reversing S5b's coordinator decision B (24p ruling (d); D6).
- **The runner** stops reading the deleted schema record and checks staleness against the files that now declare generation (A6-1).

## Boundaries & Constraints

**Always:**
- **One schema path, defined once.** Each generation step, C# and TypeScript, passes exactly `schemas/workshop_wire.fbs` to the pinned `flatc`. The path is defined once, as a property in `tools/runtime/FlatcCompiler.targets` read by both steps; the vendored project's `WireSchema` (`CuriousContraptions.FlatBuffers.csproj:44`) takes it, and no second `WireSchema` include is declared in a file every project imports, which would pass the file twice and trip S2 (audit 6, D3).
- **TypeScript identifiers come from generated code.** TypeScript takes an identifier only from the generated `WireRootIdentifier`, through exactly one conversion in `tools/runtime/wire-frame.ts`. It is total over `uint32`: any value, member or not, maps to its four little-endian bytes as a four-character identifier. No hand TypeScript in `tools/runtime/` or `tools/e2e/workshop-wire.ts` holds a current root identifier literal after this slice.
- **Every TypeScript decode checks the frame first.** There is exactly one TypeScript frame check, in `tools/runtime/wire-frame.ts`, compiled by the runtime TypeScript chain and imported by both the conformance script and `tools/e2e/workshop-wire.ts`. The size prefix must cover the buffer exactly, then the identifier must be the expected root's. It throws a plain `Error` whose message names the failed check: shorter than its size prefix, truncated, trailing bytes, or wrong identifier. The conformance test matches each message to the `WireFaultReason` C# gives for the same buffer (24p ruling (a)); that mapping lives in the C# test, so TypeScript holds no mirror of `WireFaultReason`.
- **No TypeScript verifier.** The pinned compiler writes none: 0 of the 137 generated TypeScript files mention one, and the `flatbuffers@25.2.10` package has none. TypeScript therefore decodes only what passes the frame check. **Evidence limit:** until the first production TypeScript table decoder provides TypeScript verification without hand code (H-R1, `ENGINE-WIRE-ABI`), a malformed table that passes the size and identifier checks is refused only on the C# side.
- **The conformance check is a required test, not a browser gate.** AGENTS.md says "the game system's tests are the C# solution's test projects and the node harnesses for game runtime code", so a node harness over the generated runtime bindings is a required system test. Only Chrome/Playwright suites and the tests of their toolchain are deferred.
  - The script is strict TypeScript (`strict: true`, no `any`), compiled by the pinned `tsc` 5.9.3 (root `package.json:10`) in the normal build and run with the pinned Node 26.12.1 against the compiled output. It imports `flatbuffers` from the root `node_modules`.
  - The test asserts decoded semantic fields with exact values: the wire carries values exactly, so exact equality is a defined-interface invariant. It never compares bytes or hashes.
- **The generated TypeScript is compiled in every build graph, after generation.** One runtime TypeScript compile (the direct pinned local tsc invocation owned by Abstractions) compiles the generated module, `wire-frame.ts` and the conformance script, in the Abstractions instance (`:23`, `:29-30`) in every configuration.
  - TypeScript generation joins that chain, before the compile, and reads the one schema file (D3). It stages its output in the building instance's own intermediate directory, moves the per-type files into `tools/e2e/.wire/` first and the entry module last, so an interrupted run never leaves a partial set behind an entry module.
  - Two writers never overlap within a supported graph, including Clean and publication of generated files. Each clean local capture executes the generator exactly once, completing before the first actual runtime tsc. Later Abstractions target visits reuse fresh shared output. Task 1(c) derives the ordered Release publish branches from the host and worker project sources. Exact Release instance reuse remains unobserved.
  - A separate target writes one configuration-neutral input stamp per Abstractions artifact base with `WriteOnlyWhenDifferent`. Clean explicitly deletes that stamp so every `--no-incremental` capture generates exactly once. Generation Inputs are the stamp, the schema, `tools/runtime/FlatcCompiler.targets` and `$(MSBuildThisFileFullPath)`. Its sole Output is the entry module (A6-1).
  - The stray-output and missing-entry refusals move with the target (D10).
  - `tools/runtime/FlatcCompiler.targets` is imported once, by `tools/runtime/RuntimeTypeScript.targets`, which every project receives through `Directory.Build.targets:41`, the vendored one included; the vendored project adds no import (no MSB4011). S5's compiler provisioning serves both steps, and the compiler cache is still needed only when generating.
- **The TypeScript runtime is pinned once, at the root.** `flatbuffers` is exactly `25.2.10` in the root `package.json` `devDependencies` and the root `package-lock.json`. `tools/e2e/package.json` and `tools/e2e/package-lock.json` are deleted. No tsconfig maps `flatbuffers` to a path.
- **Forward-only.** No second generation step, schema record, frame check or pin; no fallback; S5b's Debug-only rule and its include ordering are deleted, not kept beside the new chain.

**Never:**
- A hand-written TypeScript verifier, any other new generator, a TypeScript post-processor script, a hand mirror of `WireFaultReason` in TypeScript, or a second TypeScript frame check.
- Running or gating the browser toolchain (`tsx`, Playwright) in a required run, or the deferred `tools/e2e/unit/run-reset-snapshot.test.ts`. The `BrowserDeferral` list gains, loses and re-gates no entry; only reason texts that name the `tools/e2e` manifest change.
- Editing lane 3's runner files: `tools/E2eRunner/RunnerError.cs`, `ProcessTable.cs`, `SuiteProcess.cs`, `PreviewHost.cs`, `Scheduler.cs`, `tools/E2eRunner.Tests/ProcessesTests.cs`, `SchedulerTests.cs`.
- Hashes or byte comparisons of encoded output as acceptance; mutants; a local Release worker build or web publish.
- Any schema change (except S5c-1 deferral P5-5's comment-only closure clarification on `attribute "authoring"` at `schemas/workshop_wire.fbs:3`).

## I/O & Edge-Case Matrix

| Row | Input / State | Expected Output / Behavior | Error Handling |
|-----|--------------|---------------------------|----------------|
| S2 | The binary logs of the round (`solution.binlog`, `playtest.binlog`, `web.binlog`) | Every C# and TypeScript `flatc` invocation names exactly the one schema file. One actual TypeScript invocation runs in Abstractions per clean graph. Its generation completes before runtime tsc, and writers including Clean do not overlap. Every npm Exec in the matching runtime graphs must match the canonical restore command, owner and interval. Compiler path comparisons preserve literal-dollar quoting. Target visits with skipped bodies do not count as invocations | Names the log and the offending input, count, order or overlap |
| S3 | The generated `WireRootIdentifier` in C#, and the same members read through the TypeScript script | Four members. Each name is a table that the production codecs decode as a root. The values are distinct, and each decodes to four printable ASCII bytes. C# and TypeScript give the same four bytes per member. The `SavedConstruction` member equals the schema's `file_identifier`. A save the codec wrote, which the generated finisher frames with the `file_identifier`, is admitted by `ICommandCodec.ValidateSaveFrame`, which checks against the converted `WireRootIdentifier.SavedConstruction`. That save, re-finished by the generated builder with another member's converted identifier, is refused with `UnknownIdentifier`. No frame bytes are read by the test | Names the member and the refusal |
| S4 | The production codecs encode one value of each root: every value of the encoder's command-kind enum (`Enum.GetValues<WorkshopCommandKind>()`, 14 today), every value of its response-kind enum (2), every non-None `ObservationPayload` arm (11), and one save | The script frame-checks each buffer with the identifier from the generated `WireRootIdentifier`, decodes it with the generated accessors and reports the unpacked object. Named fields equal the encoded domain values exactly: kind, session bytes, sequence, a nested table's identity, a vector's length, an f32 and a `uint64` | Any frame refusal or missing field fails, naming the root and the case |
| S5 | Every S4 sample re-encoded through generated TypeScript bindings, unchanged and with one named field changed. Use generated object packing for command, observation and save roots. Use the generated typed response builder and preserve source motion vector presence, including a legal present control (D11) | C# admits each re-encoding and compares the full domain value field by field, with collections compared by element. The edited result differs only in the selected field. Response motion stays absent or present respectively | A C# refusal names its `WireFaultReason` |
| S6 | Malformed frames made from each root's valid buffer: shorter than the size prefix, wrong identifier (another root's), truncated by one byte, one trailing byte | C# refuses with `Truncated`, `UnknownIdentifier`, `Truncated` and `TrailingBytes` respectively; a save gets the matching `WorkshopSaveRejection`. TypeScript refuses the same buffer, and its message names the failed check the C# test maps to that reason | The test names the case if either side admits the buffer or the checks differ |
| S7 | A buffer whose root table offset points outside the buffer; a save whose required field `PuzzleRecord.assistance` is absent | C# refuses with `VerifierRejected`; a save gets `WorkshopSaveRejection.VerifierRejected`. TypeScript: no assertion (evidence limit, see Always) | C# names the reason |
| S12 | The root and `tools/e2e` manifests and tsconfigs | The root `package.json` has `flatbuffers` exactly `25.2.10` in `devDependencies`, locked at `25.2.10`, equal to `<FlatcVersion>` in `tools/runtime/FlatcCompiler.targets`. `tools/e2e/package.json` and its lock are absent. No tsconfig maps `flatbuffers` | Names the file and value |
| S13 | A field added to the schema in a copied tree, then incremental generation | The C# accessor exists (S5's vendored-project fixture) and the TypeScript accessor exists (the generation target on an Abstractions copy, task 1(e)). Control: both absent before the edit | Build error names the file |

</intent-contract>

## Code Map

Baseline: `cb29a0763d2bba289e5018b82698e3a7f86478a3` (the main that contains S5c-1 and lane 3's E2E runner process lifecycle slice `36219d3a`). Citations below were re-verified at `cb29a076`.

**Status:** The integrated candidate's full owned scope passed independent review (E48–E49). Matching local verification passed 7767/0/0, including 98 wire conformance cases, with both maintainability canaries and zero violations (E47, E50).

**Planning verdict:** D12 Scoped Pass in E16, E19 and E31. Independent engine Scoped Pass on the recovered plan and D3 correction (`ctx_780e686f41d7`, `msg_587c7da46d81`). See run log E4. D8/D11 Scoped Pass (`ctx_3a8bbef93d6b`, `msg_c1a53adb48a3`, run log E7).

**Implementation owner:** fresh context-free Orca dispatch `ctx_bb1927a75a82` (task `task_a1620fff9ff9`, kept-open terminal `term_b7b9f95f-368e-4b50-b1b7-a5bc83dd50c8`) replaces the unavailable original owner, whose ID is absent from the native transfer record. Retain this dispatch for all repairs.

**Review tier:** engine (build configuration, TypeScript, the vendored project, runner tools, tests). **Shape:** one structural slice. No named element is affected. **Checkout:** `slice-s5c-2`. Transfer and policy scope in E2.

Owned scope:
- **`CuriousContraptions.FlatBuffers/CuriousContraptions.FlatBuffers.csproj`** (A6-3 (a)):
  - `WireSchema` (`:44`, the one item S5c-1 left, FO1-3 ruling) takes the one path from `FlatcCompiler.targets`; `GenerateWireAccessors` (`:122-128`) passes it, and its Inputs (`:123`) gain the shared file, so a compiler pin change still regenerates C#. (`ListWireSchemas` was deleted by S5c-1, FO1-3 ruling.)
  - Deleted: the comment `:146-163`, `WireTypeScriptDirectory` and `_WireTypeScriptStaging` (`:164-167`), `GenerateWireTypeScript` (`:168-197`, the Debug-only condition at `:168`), `RemoveWireTypeScriptStaging` (`:199-201`) (`OrderWireSchemas` was deleted by S5c-1; FO1-3 ruling).
  - Provisioning moves to `tools/runtime/FlatcCompiler.targets`: `ProvisionFlatBuffersCompiler` (`:58-113`), `DeleteFlatcTemporary` (`:115-117`) and the properties they read (`:24-42`). The comments that describe provisioning here (`:2-10`, `:56-57`, `:119-121`) are reworded.
- **`tools/runtime/FlatcCompiler.targets`** (new): the compiler properties, digests, provisioning targets and the schema path property.
- **`tools/runtime/RuntimeTypeScript.targets`** (`:23-52`): the stamp target, the generation target (`--ts --gen-object-api` on the one file, into `tools/e2e/.wire/`, with S5b's annotation normalisation and the moved stray and missing-entry refusals) and its staging-cleanup `OnError` target, before `BuildRuntimeTypeScript`, whose Inputs gain the generated entry module.
- **`tsconfig.runtime.json`** (E33): remove the duplicate `outDir`. The guarded MSBuild output is passed to the sole tsc command and supplies the compiled import and shadow paths.
- **E33 direct consumers:** `FlatcCompiler.targets` guards the cold archive and extraction paths and escapes compiler command paths. `RuntimeTypeScript.targets` and the vendored C# project invoke the escaped paths. `WireSchemaGuards.cs` replays executed arguments and owning project evaluations. `WorkshopWireBuildTests.cs` covers cold refusal, literal-dollar paths and Clean bookkeeping. `CSharpStaticGateTests.cs` pins the revised targets. `SourceIdentity.cs` records nested retired npm manifest presence, with a native control in `IdentityTests.cs`. E35 extends the replay to every npm Exec and raw compiler quoting, adds a literal-dollar runtime copy and verifies Git-listed retired paths, physical case aliases and adjacent paths.
- **`tools/runtime/wire-frame.ts`** (new): the one TypeScript frame check and the one `WireRootIdentifier`-to-bytes conversion; it imports the generated module.
- **`tools/runtime/wire-conformance.ts`** (new): reads JSON on stdin and writes JSON on stdout, like `tools/runtime/event-roundtrip.ts` (driven from `CuriousContraptions.tests/EventRingConsumerTests.cs:761-773`). It imports the generated entry module, `wire-frame.ts` and `flatbuffers`; looks up a root class by its `WireRootIdentifier` member name; frame-checks the buffer; then either decodes and unpacks, or unpacks, sets one named field if asked, packs and finishes size-prefixed. It reports every JavaScript `number` as its IEEE-754 binary64 bit pattern in hex (exact for every f32, integer and double, and it keeps -0) and every `bigint` as a decimal string; the C# side widens its expected values to binary64. D11 uses typed generated response accessors and builder methods for the conformance fixtures. No hand slot, field layout or identifier data.
- **`CuriousContraptions.tests/WireConformanceTests.cs`** (new): rows S3 to S7, driving the script with node.
- **`CuriousContraptions.tests/WireSchemaGuards.cs`**: adds S2 (`WireSchemaGuards.cs:896` calls the internal `CSharpStaticGateTests.ReplayBuildLog`) and S12 (S5b's T7 inverted, reading `<FlatcVersion>` from `tools/runtime/FlatcCompiler.targets`; A6-3 (f)).
- **`CuriousContraptions.tests/CSharpStaticGateTests.cs`**: the vendored project file's pin (`:116`); `PinnedTargets` (`:1336-1365`) for the changed `BuildRuntimeTypeScript`, the moved generation, stamp, explicit Clean and staging-cleanup targets (A6-3 (b)), and `tools/runtime/FlatcCompiler.targets:ProvisionFlatBuffersCompiler` and `:DeleteFlatcTemporary` with their `DownloadFile` and `Exec`; `PinnedImports` (`:1369-1378`) for the shared file's one importer.
- **`CuriousContraptions.tests/WorkshopWireTypeScriptTests.cs`** keeps T1's exact generated accessor/object companion and enum shapes, with T5's existing lexer. Remove the record guard and superseded T7/T8 in favor of S12 and S2 (E1, E8).
- **`CuriousContraptions.tests/WorkshopWireBuildTests.cs`** (S5 and S5b cases, as S5c-1 left them):
  - **Deleted:** (`ASchemaSetWithoutOneRootReachingEverySchemaRefusesTheBuild` and `ADeletedSchemaFileLeavesNoGeneratedSource` are already deleted by S5c-1, FO1-3 ruling); `AReleaseBuildWritesNoTypeScript` (its behaviour is removed: Release graphs now generate); T3's schema-record case (`:156-159`; A6-1).
  - **Merged into S13:** `AnAddedSchemaFieldIsATypeScriptAccessorAfterAnIncrementalBuild`.
  - **Moved to the Abstractions-copy fixture (task 1(e)):** `AnUnchangedSchemaDoesNotRegenerateTypeScript` (steps rewritten for the intermediate staging folder), `ARemovedSchemaTypeLeavesNoTypeScript` (asserting the new declaration form, A6-4), `ACheckoutPathWithASpaceBuildsACompleteModuleSet` (the command's quoting still matters), and the stray-output refusal (D10), S5c-1's one-file case with a second namespace appended.
  - **Assertions removed:** the "refused build wrote no TypeScript" assertions in `APinThatTheCachedCompilerDoesNotMatchRefusesTheBuild` and the span-define cases, which would be vacuous; the pin-mismatch case is repeated once on the Abstractions-copy fixture, asserting that no TypeScript is written. The pin-mismatch controls supply a regular invalid file at the canonical cache executable path in both fixtures (E28).
  - `SharedBuildFiles` (`:40-41`) gains `tools/runtime/FlatcCompiler.targets`.
- **`tools/E2eRunner.Tests/NodeHarnessTests.cs`** (E46): the single added repair-closure path. The shared harness execution supplies the emitted JavaScript basename to `TapResult` and exercises a compiled zero-test refusal and a compiled one-test pass.
- **`tools/E2eRunner.Tests/TimingDetectorTests.cs:129-159`** (T9): kept unchanged; the closure follows relative imports (`tools/E2eRunner/TimingDetector.cs:271-296`), so `workshop-wire.ts` importing `../runtime/wire-frame.ts` resolves (A6-3 (i)).
- **The e2e runner** (A6-1; deferred-runner scope, which the required round tests through `tools/E2eRunner.Tests`):
  - `tools/E2eRunner/Toolchain.cs` invokes the shared Abstractions admission target with read-only MSBuild result reporting. The runner maps refusals to ToolchainMissing and carries the returned relative generated paths in the resolved toolchain (D12).
  - `tools/E2eRunner/SourceIdentity.cs` includes both root npm manifests, `tools/runtime/wire-frame.ts` and the exact wire sources admitted by the shared target. The same list is used at start and finish (A6-1, D12).
  - `tools/E2eRunner/BrowserDeferral.cs`: the reason texts at `:18-20` (phrase at `:19`) and `:44-45` name the root manifest.
  - `tools/E2eRunner.Tests/TypeCheckBoundaryTests.cs`: `TheE2eFlatBuffersRuntimeMustBeThePinnedVersionInstalledInToolsE2e` (`:31-92`) becomes the root-installed check, its "copy above `tools/e2e`" control (`:60-61`) inverting to "a copy only in `tools/e2e/node_modules`"; in `TheGeneratedWireEntryModuleMustExistBeforeSuitesRun` (`:129-221`; constants `:230-232`) the record cases (`:160-174`, `:202-214`) are deleted, the project-file cases (`:155-159`, `:182-200`) retargeted to the two declaring files, and the expected texts follow the remedy.
  - `tools/E2eRunner.Tests/IdentityTests.cs` `TheSuiteDigestCoversTheGeneratedWireModulesAndNoOtherIgnoredFile` (`:613-644`): the record step (`:623-624`, `:636-638`) is replaced by a control that a change to `tools/runtime/wire-frame.ts` changes the digest.
  - `tools/E2eRunner.Tests/E2eRunner.Tests.csproj:21-22` (the reference to the vendored project "to build the generated TypeScript wire modules") is deleted; the Abstractions reference (`:19-20`) now generates `.wire/` (A6-3 (c)).
- **`tools/e2e/workshop-wire.ts`** (H-S3, placed here by the forward-only rule; run log E1): the identifier constants (`:14-16`) and `openFrame` (`:20-29`) are deleted; the decoders (`:32`, `:74`, `:78`) and the mock finishers (`:316`, `:379`) take `wire-frame.ts` and the generated `WireRootIdentifier`; the `CCWS` message (`:79`) names the member; the header `:1-4` and `:13` are reworded (A6-3 (d)). The authored-state dump and mocks are otherwise S5c-3's (H-S4, H-S5).
- **Runtime pin:** the root `package.json` and `package-lock.json` gain `flatbuffers` at exactly `25.2.10`, written by npm with the pinned Node; `tools/e2e/package.json` and `tools/e2e/package-lock.json` are deleted; `tools/e2e/README.md` `:68` and `:82` name the root manifest.
- **`schemas/README.md`** and **`schemas/workshop_wire.fbs:3`** (P5-5): `schemas/README.md:8-12` and the `attribute "authoring"` comment at `schemas/workshop_wire.fbs:3` state the closure rule S9 enforces (every declaration that references a marked declaration is itself marked); `schemas/README.md:51` (digests pinned in `tools/runtime/FlatcCompiler.targets`) and the TypeScript section (`:64-90`, including `:64` and `:69-71`) describe the generation chain, the object API, the root pin and the conformance test.
- **`_bmad/custom/bmad-loop-protocol.md:251`** names the Abstractions instance as the writer of `tools/e2e/.wire/` (A6-3 (e); a shared document under 24m, line-local).
- **`tools/E2eRunner/Program.cs`** (D12): carry the admitted generated module path list into the existing source identity calls. Do not change the process lifecycle.
- **Planning records:** this contract, its run log and [Task 1 probe transcript](run-logs/s5c-2-task-1-probes.md); `TODO.md`; `sprint-status.yaml` (key `s5c-2-wire-conformance-and-root-pin`); `delivery-batches.md` (S5c); `deferred-work.md` (closes S5b's carried BH2 pin item and S5c-1's three S5c-2-owned deferrals H-S3, A6-1, P5-5).

Inventory row owned here: **H-S3** (`tools/e2e/workshop-wire.ts:14-16`, `:20-29`: identifier literals and a second frame check; deleted, `wire-frame.ts` replaces them). The full inventory is in the S5c-3 contract.

### Capability probe

No solver law, worker message content, wire field or shared presentation contract changes. The independent planning scout's cited sources and consumers are in run log E3.
- **Present:** the one schema and generated C# root identifiers, the pinned C# generation, strict runtime TypeScript compile and compiled Node harness execution. Exercise points include `WireSchemaGuards.TheSchemaIsOneFileWithOneRootTypeAndNoInclude`, `WireFaultTests.AnotherRootIsAnUnknownIdentifier`, `WorkshopWireBuildTests.ACleanCopyGeneratesEverySchemaTypeAndCompiles` and `NodeHarnessTests.HarnessPasses`.
- **Narrower at baseline:** the Debug-only TypeScript generation exercised by `WorkshopWireTypeScriptTests.TheTypeScriptStepRunsExactlyOncePerBuildGraph`. The shared TypeScript frame check, bidirectional required test and root pin belong here.
- **Direct consumers:** the owned Code Map below and above describes the build, guards, runner and fixture changes. The 27 consumers of the generated module keep its current path. No package or player interaction is affected.
- **Missing, owned elsewhere:** the TypeScript table verifier and shared-memory ABI layouts (`ENGINE-WIRE-ABI`), and S5c-3's remaining hand restatements. No required-now owner decision is open. D3 has a contract-preserving default that requires independent planning approval.

### Decisions

- **D3, generation chain (Task 1).** Abstractions generates ahead of runtime compilation. Use one configuration-neutral freshness stamp per artifact base, explicit Clean ownership and per-instance staging. One actual generator invocation per clean local capture, ordered before the runtime tsc body, with zero overlapping writers.
- **D3 graph clarification.** The measurements supersede E1's two-instance estimate. One writer means execution, and a later target visit may reuse fresh output. Web Release ordering is source-derived across five callers with nested publish instance reuse unobserved. See run log E3–E4.
- **D4, object API for TypeScript only.** `--gen-object-api` is passed only to the TypeScript step (confirmed at 25.2.10 under strict `tsc` 5.9.3; audit 6 probe adds no `any`), giving the conformance round trip, and S5c-3's snapshot, dump and mocks, a surface that covers every field with no hand names. The C# surface is unchanged.
- **D6, pin at the root (24p ruling (d); reverses coordinator decision B).** The conformance script and `wire-frame.ts` are compiled in every graph and a required test runs them, so the runtime belongs in the root manifest, with one pin. Reasoning: run log E1.
- **D8, the S5 comparison.** Compare decoded collections and fields with strict equivalence or domain equality for scene-free values. For responses, separate `Read.Motion` before domain equality because it otherwise compares encoded motion bytes. Compare presence, decoded motion metadata and the committed body sample instead (E5).
- **D10, S5b's two output refusals move with the target (A6-3 (a)).** The stray-output and missing-entry refusals (`CuriousContraptions.FlatBuffers.csproj:173-179`) cost two lines and protect the set the entry module names; their stray case moves to the Abstractions-copy fixture.
- **D11, response motion presence.** Use one generated typed response-root encoder in the conformance harness. Read `motionArray()` and call generated `createMotionVector` and `addMotion` only for a present vector. The schema and production C# codec stay intact.
- **D11 proof.** Use generated object packing for other roots and nested response values. Exercise the existing acknowledgement and admission read plus a motion-present committed read from `WireFaultTests.PhysicalRead()`, unchanged and edited. Compare semantic response fields, presence, count, substeps, first/last ordinals and sampled pose through `IPhysicsMotionRead.TrySample` (E5).
- **D12, shared admission.** The existing Abstractions targets are the sole validators. Refuse retired files by presence before any output writer. After the build's normal restore/generation, validate the current runtime and source set. The runner calls the same read-only targets, returns admitted paths and maps errors to its typed toolchain refusal (E14–E15).
- **D12, canonical inputs.** Match the compiler pin, root lock/manifest and loadable FlatBuffers inside the root installation. Derive the complete object-API export/leaf set from the schema. Use the same paths for tsc inputs/outputs, source identity at both ends and build-owned compiled leaf cleanup on compile or reuse (E14–E15).
- **D12, physical admission.** Prove physical repository, package, manifest and compiler identity before opening the installation. Resolve and compare the runtime file first, then load it from the admitted installation. Use the pinned Node CLI's filesystem and resolver operations (E18).
- **D12, sole compiler entry.** Delete the independently invocable npm compile script. Only the Abstractions target invokes the pinned root tsc CLI. S2 observes that actual direct command and process interval, including compiled output reconciliation and no overlap (E18).
- **D12, shared output preflight.** Use the one schema declaration inventory and physical path guard for both language projections. Check expected path kinds, ancestors and stamps before standalone generation, runtime writes, Clean and read-only admission. Preserve physical SDK artifacts paths and offline direct generation, with lossless path handling and exact output identities. Actual paths, ordering and proof are in E30–E31.

### Protections this change removes, and what replaces them

- **S5b's include ordering and its refusals** are replaced by S1 (one file, no include) and S2 (each run reads exactly that file).
- **S5b's Debug-only writer guard** is replaced by the Abstractions chain, shared freshness and the observed or source-derived ordering in Task 1(c).
- **The schema record `schemas.txt`** is deleted with its runner check; with one file the presence and staleness checks remain.
- **Residuals:** the e2e module graph is still type-checked only in the browser phase (`tools/E2eRunner/TypeCheck.cs`), while the generated module, `wire-frame.ts` and the conformance script are type-checked in every build; there is no TypeScript verifier (H-R1); CI's build graph is not observed locally (D3).

### Concurrency

- Baseline includes the published lane 3 runner prerequisite. Keep its process-handling files outside scope (Never and E2).
- Shared with S5, S5b and S5c-1: the vendored project file, `WorkshopWireBuildTests.cs`, `CSharpStaticGateTests.cs`, `schemas/README.md` and the `tools/e2e` files.
- Shared documents (owner decision 24m): `TODO.md`, `deferred-work.md`, `delivery-batches.md`, `completion-ledger.md`, `sprint-status.yaml` and `_bmad/custom/bmad-loop-protocol.md`; line-local edits only.
- New files are staged with `git add` before the candidate commit.

## Tasks & Acceptance

**Execution:**
1. Hard gate, in `.tools/s5c-evidence/`, with the pinned cached compiler and the pinned Node. The planning probes and baseline replay passed (E3–E4). Matching candidate replay and fixture execution complete the gate before handoff. A disproven design assumption returns to planning.
   - **(a) Object API.** Run `flatc --ts --gen-object-api` on the one file and record that the entry module re-exports every declaration in the A6-4 form.
   - **(b) Compile and import.** Compile the generated TypeScript, a stub of `wire-frame.ts` and a stub of the conformance script under `tsconfig.runtime.json`'s options with the pinned `tsc`, and import them from the compiled JS with `flatbuffers` 25.2.10 resolved from the root.
   - **(c) Build graphs.** From the three binary logs of a baseline round, record per graph which project instances run `RestoreRuntimeTypeScript`, `BuildRuntimeTypeScript` and `GenerateWireTypeScript`. Derive the intended CI branches from project references, host MSBuild calls and the pinned WASM SDK nested publish target. Record serial call order and distinguish the five source paths from the unobserved exact instance count. Confirm from matching candidate logs that D3 performs exactly one generation before runtime compilation with no overlapping writes in each clean local graph, `web.binlog` included (the web graph's `BuildWorkshopEditorMetadata` call passes `Configuration=Debug` and removes `RuntimeIdentifier;SelfContained`, so whether it reuses the outer instance is measured, not assumed).
   - **(e) Build-test fixture** (lettered (e) as row S13 cites it). Define the copied-tree fixture that invokes the moved generation target on an Abstractions copy, offline (generation needs only the compiler cache). Invoke the generation target with MSBuild directly and exercise explicit Clean before regeneration. Record the files it copies and its run time against the full round.
2. **Build:** `FlatcCompiler.targets`, the moved generation, the vendored-project deletions, the pins in the static gate. Rows S2 and S13.
3. **Runtime pin:** the root manifest and lock, the `tools/e2e` manifest deletion. Row S12.
4. **Conformance:** `wire-frame.ts`, `wire-conformance.ts`, `WireConformanceTests.cs`, H-S3. Rows S3 to S7.
5. **Runner and records:** A6-1's runner edits, the build-test dispositions, P5-5 (`schemas/README.md:8-12` and `schemas/workshop_wire.fbs:3` comment), the README sections, `bmad-loop-protocol.md:251` and the planning records; close S5c-2 with its numbers.

**Acceptance Criteria:**
- AC1: Given the candidate, when the guards run, then S2, S12 and S13 pass, and `tools/e2e/package.json` and `tools/e2e/.wire/schemas.txt` are absent after a build.
- AC2: Given the full local round, when it completes, then failures are 0 and skipped are 0 in every project, the total is the baseline plus the new tests minus the deleted ones (delta measured at dispatch), and the run log names each added and deleted test with its cover.
- AC3: Given the hand TypeScript sources outside S5c-3's H-S1 hand decoder (`tools/e2e/run-reset-snapshot.ts:183-293`, whose `rootPosition` at `:290-293` reads `SIZE_PREFIX_LENGTH` and which S5c-3 deletes), when they are searched, then only `tools/runtime/wire-frame.ts` calls `__has_identifier` or reads `SIZE_PREFIX_LENGTH`, and H-S3's constants are gone.
- AC4: Given the maintainability gate, when it runs, then both canaries pass and violations are 0.

## Spec Change Log

- 26 November 2026: created from S5c and audits 1–6 (E1).
- 27 November 2026: dispatch re-audit on the original baseline. Updated row ownership (E2–E4).
- 28 November 2026: D12 scoped shared-admission recovery (E14–E16).

## Implementation Notes

- Retained handoff inspected the owned diff and preserved T5's lexer and controls (E8).
- Keep the carried owned changes that meet D3 and S2–S13, including the workflow correction (E3–E4).
- Keep Baseball shared entries at the one root-held 24m integration boundary (E2).
- E36 checks the full owned scope against the baseline and removes the partial npm command interpreter. The repair uses the existing replay, copied runtime fixture and suite identity path selection. The shared MSBuild admission and runner lifecycle remain the same.
- E38 reuses the existing executable scan and physical entry distinction. Only the executable search skips leading assignments. Suite identity compares the complete immediate manifest segment in canonical Unicode and uses the original Git spelling for physical lookup.

## Review Triage Log

### 27 November 2026 — Review pass 1

- Verdicts: 22 findings, high 9, medium 12, low 0, false 1, maybe-false 0. One descriptive Intent Alignment report. See run log E10 for the verified findings and routes.
- Retained owner repair `ctx_f3b09a213f4b` returned the seven fixes and matching receipts (E11). Baseline, original cases, the motion oracle, D3 writer ordering and the line-local workflow correction were kept. Independent re-review is pending.

### 27 November 2026 — Review pass 2

- Verdicts: 18 findings, high 5, medium 11, low 0, false 2, maybe-false 0. See run log E12 for individual dispositions.
- Repair remains with the retained owner. Intent Alignment was inactive under the engine review cadence.

### 28 November 2026 — Review pass 3

- Verdicts: 16 findings, high 6, medium 9, low 1, false 0, maybe-false 0. One quality coverage item is deferred under the third-pass rule. See E14.
- Repeated module and package admission defects require a bounded planning correction. Preserve D3, D8, D11 and all existing executed case coverage.

### 28 November 2026 — Restarted engine review

- Verdicts: 12 findings, high 4, medium 7, low 0, false 1, maybe-false 0. Intent Alignment described the binding proof as aligned. The E17 receipts cover this handoff. See E18 for dispositions.
- Preserve the single shared admission mechanism, D3/D8/D11 and the full matrix. Return ordering and the single compiler entry to bounded planning. Final full and hook receipts are due after the reviewed integration boundary.

### 28 November 2026 — Restarted E18 review

- Verdicts: 12 findings, high 6, medium 6, low 0, false 0, maybe-false 0. Ten patches and two already assigned browser-deferred H-S2 items. See E21.
- Intent Alignment confirms generated binding interoperability. The retained owner addresses the affected current paths, with final full and hook proof still due.

### 28 November 2026 — Affected re-review after E21

- Verdicts: 13 findings, high 2, medium 9, low 0, false 2, maybe-false 0. The engine and forward-only layers returned no new findings. See E23.
- Repair the current source/build and runner transport cases through the existing D12 mechanism. Do not reopen the accepted TypeScript verifier boundary.

### 28 November 2026 — Affected re-review after E23

- Verdicts: 16 findings, high 8, medium 8, low 0, false 0, maybe-false 0. All are current writer/pin or direct repair effects. The engine layer returned Scoped Pass. See E25.
- Apply the existing ownership and S2 capture rules across the complete affected writer set. Retain isolated artifacts-path support.

## Design Notes

1. **Delivered:** both bindings generated from the one file in every build graph, and a required test proving they agree in both directions. No gameplay, solver or presentation change.
2. **Reusable:** one TypeScript frame check and identifier conversion; one compiler definition for both steps.
3. **Declared:** the schema file; the conformance script finds root classes by the generated enum's member names.
4. **Execution:** schema → `flatc` (C# in the vendored project; TypeScript in the runtime chain) → compile → frame check → decode.
5. **Ownership:** the runtime TypeScript chain owns TypeScript generation and compilation; Core owns the C# frame boundary.
6. **Generality:** S4 and S5 run over every command kind, response kind and payload arm, with C#-derived expectations; the harness uses the generated response builder for presence-sensitive motion (D11).
7. **Deleted:** S5b's vendored-project TypeScript target (`OrderWireSchemas` and the include ordering were deleted by S5c-1, FO1-3 ruling), Debug-only guard, schema record and `tools/e2e` manifest; `workshop-wire.ts`'s identifier constants and frame check.
8. **Uncertain:** task 1's gates (CI graph included); H-R1. Nothing required now is open.

**Architecture guards.** S2 and S12 read logs and manifests; they live in `WireSchemaGuards`, each with a positive control.

### Pass 8: E26 affected review

- Verdicts: eight finding groups, high five and medium three. Repeat Blind and Edge reports on the same restore capture are grouped. Both reviewers replayed the E24 active restore through E26 (1/0/0). Scope, reviewers and routes in E27.

### Pass 9: E28 affected review

- Verdicts: seven finding groups, high two and medium five. Independent replay corroborates E24's active npm command and E28's matching scoped evidence. Actual standalone writing paths, layout proof and remaining repairs in E29.

### Pass 10: E32 restarted engine review

- Verdicts: nine finding groups, high three and medium six. Intent Alignment found no competing intent requiring a change. See E33 for receipts and patch scope, including the sole tsc destination setting in tsconfig.runtime.json.

### Pass 11: E34 affected review

- Verdicts: five finding groups, high one and medium four. The cold receiving, sole output and SDK Clean repairs passed their affected checks. E35 records source-layer identities, the restore replay progress trigger and remaining bounded patches.

### Pass 12: E36 affected review

- Verdicts: three finding groups, high one and medium two. Two lenses reported scoped clean results. Confirmed current alias behavior and two test controls go back to the retained owner (E37).

### Pass 13: E38 affected review

- Verdicts: no findings. Five independent reports and matching E38 receipts passed (E39). The single integration and final full local and normal-hook checks follow.

### Pass 14: E43 scope-repair review

- E42's C# visibility and schema freshness corrections passed. Two affected prose findings and staged review input pointers remain. E44 records the three independent reports, applicable capture reuse and exact fixes.

### Pass 15: E45 prose review

- E45's source and replay references passed. VG15-1/EFE15-1 confirmed a required compiled Node harness proof defect and EFE15-2 corrected wording (E46). The owned test closure adds NodeHarnessTests.cs.

### Pass 16: E47 emitted harness review

- No findings. All five applicable layers and E47's matching scoped checks passed (E48). The final scope and single full merged round follow.

## Verification

The retained implementation owner ran the original handoff (E8–E9), repairs (E11 and E13), D12 (E17) and its ordering and compiler-entry correction (E20). E47 records the latest matching configurations, maintainability command, registration and affected scopes, raw receipts and invalidating inputs. E49 records independent final scope. The coordinator's one required merged round used those matching captures and passed in E50.

**Diagnostic result.** E5 records the initial failed S5 fixture and presence cases. E8–E9 record their passing matching replacement.

**S5 return to planning.** The generated object packer normalized missing motion to a present empty vector, which production C# refused. The approved typed builder then admitted both original motion-absent cases and the committed-motion control in the matching affected and full runs (E8–E9).

The pinned SDK and Node environment and exact commands are recorded in E2, E8 and E13.

**Commands:**
- Five matching captures under the local-test authority. E47 refreshes the Solution and Playtest captures and retains E41's applicable web and file-tool receipts.
- `dotnet run --file tools/anvil/VerifyPolicy.cs -- --maintainability --project-root .` Expected: AC4.
- The registration check filter. Expected: 0 failed, 0 skipped (test files are added).
- The affected run uses `CSHARP_STATIC_GATE_BINLOGS` and the following scope intersected with the generated browser deferral. Expected: 0 failed, 0 skipped.

  `dotnet test CuriousContraptions.slnx -c Debug -f net10.0 --no-build --no-restore --nologo -noAutoResponse --filter "FullyQualifiedName~WireConformanceTests|FullyQualifiedName~WireSchemaGuards|FullyQualifiedName~WorkshopWireBuildTests|FullyQualifiedName~WorkshopWireTypeScriptTests|FullyQualifiedName~CSharpStaticGateTests|FullyQualifiedName~TimingDetectorTests|FullyQualifiedName~TypeCheckBoundaryTests|FullyQualifiedName~IdentityTests"`
- `git add -N` on the new files, then `git diff --check <baseline_revision>`. Expected: clean.
- One full local round. Expected: AC2.

E43 executes the current registration filter, including `CSharpStaticGateTests`, and the named `WireSchemaGuards` replay, C# freshness, `BrowserDeferralTests` and `ArchitectureDocsConsistencyTests` checks. The README wording is checked directly against the browser-deferral scope and the C# generation inputs. The final full merged round remains with the coordinator.

### Acceptance-to-test table

E50 reran the required solution tests on the integrated candidate using the five matching compiler captures. Row-specific E8–E47 receipts retain the original controls and evidence provenance, including the E24 active restore's separate historical replay.

Decoded integers and canonical wire f32 fields are exact by the wire interface. The committed-motion sample uses the existing contact-separation allowance in metres after converting the local displacement from cell units (E6). The tolerance detects a wrongly decoded sampled position. Rotation is the fixture's exact authored identity.

| Rows | Named discriminating test | Control and boundary | Execution |
|---|---|---|---|
| S2 | `WireSchemaGuards.EveryCompilerRunReadsOnlyTheOneSchemaAndTypeScriptGeneratesOncePerGraph`; `WorkshopWireBuildTests.AGlobalWireSchemaPathCannotSelectAnotherSchema` | Refuse foreign or extra flatc or tsc owners and schema inputs, duplicate or missing generators or compilers, early tsc, and overlapping writers. Match the exact direct local tsc invocation and observe compilation and emitted reconciliation intervals. Both copied builds keep the canonical schema under a global override. An executing compiler, tsc or restore has an observed command and writer interval. An overlapping skipped visit contributes no writer. C# accessors have their own interval in their disjoint output tree. The reader matches observed Flatc executable and schema args and normalized destinations against the owning evaluation’s intermediate path. It matches tsc’s actual `--outDir` against the guarded runtime output. Same-basename foreign executable and destination controls are refused. The replay preserves whether compiler path arguments allow dollar expansion. The same normalized schema and tsc output are refused when their raw quotes allow expansion. Every npm executable and every Exec in the canonical restore target reaches exact owner, argv and interval checks. Foreign `npm add` and ci or install after separated `--script-shell` or `--logs-dir` values are refused. Leading shell assignments are skipped only to locate the executable. The same replay observes and refuses foreign `CI=true npm ci` and refuses the prefixed canonical options even in the canonical owner. The exact canonical restore still passes. Existing value options and active, skipped, missing-owner and wrong-command controls remain. Canonical active restore must finish before both TS generation and tsc. | E38, `affected-game.log`, `graph-order.log` and three matching graph binlogs. `active-restore-e24.log` qualifies the original E24 active restore. E43 confirms the replay and C# static gate on the final matching captures |
| S3 | `WireConformanceTests.EveryRootIdentifierIsGeneratedSchemaDataBothSidesAgreeOn` | Control: a buffer finished with another member's identifier is refused by both sides. Boundary: a finisher-written save is admitted by `ValidateSaveFrame`, which fails if the `file_identifier` differs from the converted `SavedConstruction` member; the same save finished with another member's identifier is refused | E34, `affected-game.log` |
| S4 | `WireConformanceTests.TypeScriptDecodesEveryCommandCSharpEncoded`, `WireConformanceTests.TypeScriptDecodesEveryResponseCSharpEncoded`, `WireConformanceTests.TypeScriptDecodesEveryObservationArmCSharpEncoded`, `WireConformanceTests.TypeScriptDecodesTheSaveCSharpEncoded` | A wrong expected field names the mismatch. Check admitted signed zero, maximum uint64, adjacent identities and exact negative signed WideOffset values beyond 2^53. Check distinct cadence and projection, acknowledgement outcome and reason, populated energy rows and nested Transfer reads decoded through the generated response harness. The generated TS response accessor distinguishes absent motion from a present vector of the expected shape and decodes exact, distinct publication IDs at 2^53 and 2^53 + 1. The moving response carries a legal reached coordinate-travel row. The authored save control checks puzzle identity, placement, precision, goal kind, and both connection endpoints and sockets. The admission response checks the populated slack rope row and its attachments. TypeScript reads the committed authority revision directly. The wide Exact command revision above 2^53 and the canonical zero Any revision decode with their respective kinds. The accumulating and reached coordinate rows have distinct valid owners and thresholds, and the accumulating travel differs from its threshold. | E34, `affected-game.log` |
| S5 | `WireConformanceTests.CSharpDecodesEveryCommandTypeScriptEncoded`, `WireConformanceTests.CSharpDecodesEveryResponseTypeScriptEncoded`, `WireConformanceTests.CSharpDecodesEveryObservationArmTypeScriptEncoded`, `WireConformanceTests.CSharpDecodesTheSaveTypeScriptEncoded`, `WireConformanceTests.TheTypeScriptHarnessExplicitlyRefusesAMotionEdit`, `WireConformanceTests.CSharpRefusesANonzeroAnyRevisionEncodedByTypeScript` | Check unchanged and edited values, including signed-zero commands and save coordinates, both acknowledgement outcomes, exact signed WideOffset values, and populated energy, contact loads, electrical, scene and memory. A named cadence edit preserves projection. D8 keeps the present and absent motion cases, the full decoded window, the owned-body sample with the metre contact-separation allowance, and the committed-endpoint refusal. The harness explicitly refuses edits to response motion. The moving case preserves the populated coordinate-travel row, wide publication identity and its owned half-substep motion. The metre tolerance distinguishes integrated movement from a stationary origin. The unchanged and edited authored saves preserve their puzzle and connections. The unchanged and cadence-edited admission response preserves the populated rope row. The admitted Any command retains its canonical zero revision. Setting a wide nonzero revision in the generated TypeScript command is refused by the production C# decoder with the expected revision error. Both coordinate-travel rows survive unchanged and cadence-edited repacks. | E34, `affected-game.log` |
| S6 | `WireConformanceTests.MalformedFramesAreRefusedOnBothSidesForTheSameCheck` | Six faults per root. The current-length incomplete identifier is UnknownIdentifier and the high-bit unsigned prefix is Truncated in both runtimes | E34, `affected-game.log` |
| S7 | `WireConformanceTests.VerifierFailuresAreRefusedInCSharp` | Production C# decodes valid command, response, observation and authored save controls. Out-of-range table offsets on the three runtime roots and absent required assistance on the save are refused as VerifierRejected. TypeScript retains the S6 frame-only boundary. | E34, `affected-game.log` |
| S12 | `WireSchemaGuards.TheFlatBuffersRuntimeIsPinnedAtTheRoot`, `WorkshopWireBuildTests.AFlatBuffersShadowRefusesRuntimeCompileAdmission`, `WorkshopWireBuildTests.ACurrentNpmMarkerStillRequiresThePinnedRootFlatBuffersInstall`, `TypeCheckBoundaryTests.TheE2eFlatBuffersRuntimeMustBeThePinnedVersionInstalledAtTheRoot`, `WorkshopWireBuildTests.ALinkedNodeModulesRootRefusesStaleRestoreWithoutTouchingItsTarget`, `WorkshopWireBuildTests.BothGeneratorsUseTheSharedCompilerPinAndReceivingGuard`, `WorkshopWireBuildTests.ADirectoryAtThePinnedCompilerLeafRefusesBeforeDownload`, `WorkshopWireBuildTests.ALinkedDownloadWorkspaceRefusesWithAValidCanonicalCompiler`, `WorkshopWireBuildTests.AnUnsupportedBuildHostRefusesProvisioning`, `WorkshopWireBuildTests.ABadDigestAtTheCanonicalCompilerLeafRefusesGeneration`, `WorkshopWireBuildTests.ADirectoryAtTheNpmMarkerRefusesBeforeRestoreFreshness`, `WorkshopWireBuildTests.ColdCompilerWorkspaceRefusesWrongKindsBeforeDownload` | Current-marker admission requires the installed runtime and both root pins to match flatc. Missing, mismatched, shadowed, unloadable and resolved-outside-root installations are refused. The valid root alone is admitted. The package, installed manifest and local compiler are checked for physical ownership before reads or execution. The resolved runtime entry is checked before import. Foreign and broken links distinguish the early refusals from later load failures. The filesystem walk rejects foreign-linked implementation files in both pinned package trees before any metadata read or execution. A stale-marker build refuses a linked node_modules root before npm restore and preserves the external fixture. Both generation entry points retain the host, asset, version, derived compiler path and archive and binary digests under global override and refuse a linked receiving directory while preserving its sentinel. The configured cache refuses a directory at the executable leaf and a linked download workspace independently, preserving sentinels. A copied host-selection input exercises the unchanged selection table and refusal. Both generators refuse a bad digest on a regular file at the pinned path while preserving that file. The marker itself must be absent or a physical file. The wrong-kind marker control preserves its sentinel before restore freshness. Both cold entry points refuse wrong-kind archive, extraction folder and extracted compiler paths before download while preserving each sentinel and the empty cache. | E34, affected game and runner receipts |
| S13 | `WorkshopWireBuildTests.AnAddedSchemaFieldIsAnAccessorInBothLanguagesAfterAnIncrementalBuild`, `WorkshopWireBuildTests.AnUnchangedSchemaDoesNotRegenerateTypeScript`, `WorkshopWireBuildTests.AMissingCompiledWireLeafRegeneratesAnOtherwiseCurrentRuntime`, `WorkshopWireBuildTests.ARemovedSchemaTypeLeavesNoTypeScript`, `WorkshopWireBuildTests.TypeScriptOutsideTheWireNamespaceRefusesTheBuild`, `WorkshopWireBuildTests.LinkedGeneratedFilesAndDirectoriesRefuseBeforeOfflineGeneration`, `WorkshopWireBuildTests.ALinkedGenerationStampRefusesFreshnessAndClean`, `WorkshopWireBuildTests.LinkedCSharpAccessorOutputRefusesReuseAndClean`, `WorkshopWireBuildTests.LinkedRuntimeOutputsRefuseReuseReconciliationAndClean`, `WorkshopWireBuildTests.WrongKindEmittedWirePathsRefuseReuseAdmissionAndClean`, `WorkshopWireBuildTests.AnUnchangedSchemaAndCompilerSelectionDoNotRegenerate`, `WorkshopWireBuildTests.StandalonePreflightRefusesWrongKindBeforeAnyWrite`, `WorkshopWireBuildTests.StandaloneGeneratorsPreserveSdkArtifactLayoutsWithoutNode`, `WorkshopWireBuildTests.ANonIncrementalBuildRegeneratesAndCompiles`, `WorkshopWireBuildTests.BothOfflineGeneratorsPreserveLiteralDollarArguments`, `WorkshopWireBuildTests.RuntimeCompilerPreservesLiteralDollarCheckout` | Both accessors are absent before the schema edit. Cold Release generates through copied Abstractions and Debug and Release reuse the set. Deleted emitted entry and leaves are restored. A source leaf change recompiles with a current entry. A retired JS leaf is removed after generation and on compile reuse while a current leaf still loads and a neighboring output survives. Wrapped and same-line declarations generate valid modules. A refused staged set leaves the previous published set reusable. The offline generator refuses linked generated leaves, the published directory and linked staging before replacement. Freshness and Clean refuse linked stamps and C# leaves. Runtime compile, direct reconciliation and Clean refuse linked compiled ancestors, retired modules and published JS leaves without changing the sentinel. A directory at an expected emitted leaf and a file at its parent fail in Build, admission, reconciliation and Clean. Restoring the path kind allows the emitted leaf to be rebuilt and loaded. C# reuses unchanged output and regenerates when the schema, `FlatcCompiler.targets` or `RuntimeTypeScript.targets` changes in separate controls. Standalone C# and TS refuse wrong-kind expected leaves and parents, including stamps, without touching sentinels or writing later outputs. Offline positives cover external artifacts, literal-pipe artifacts and checkout paths, and mixed project-local intermediates. Missing C# leaves and the missing TS entry recover in those layouts. SDK Clean records and removes the accessor outputs and stamp and a non-incremental build regenerates them in default, external and mixed layouts. Warm wrong-kind Clean preserves the real TypeScript stamp contents and timestamp. Both offline generators preserve ordinary literal-dollar checkout and intermediate paths. The runtime target compiles from a literal-dollar checkout and Node loads a generated module from its guarded output. | E36, `affected-game.log`. E43 confirms C# freshness, including the host-selection import, in `test-affected-game.log` |
| D12 | `WorkshopWireBuildTests.RetiredArtifactsRefuseBeforeTheGenerationWriter`, `WorkshopWireBuildTests.RuntimeBuildRefusesIncompleteAndStaleGeneratedSourcesWithACurrentEntry`, `WorkshopWireBuildTests.RootRuntimeAdmissionRequiresTheActualLocalCompilerEvenWithTscOnPath`, `WorkshopWireBuildTests.AMissingCompiledWireLeafRegeneratesAnOtherwiseCurrentRuntime`, `TypeCheckBoundaryTests.TheE2eFlatBuffersRuntimeMustBeThePinnedVersionInstalledAtTheRoot`, `TypeCheckBoundaryTests.TheGeneratedWireEntryModuleMustExistBeforeSuitesRun`, `IdentityTests.TheSuiteDigestCoversTheGeneratedWireModulesAndNoOtherIgnoredFile`, `WorkshopWireBuildTests.RuntimePathsAndAdmissionIgnoreGlobalAndInheritedRedirections` | Presence-only refusal precedes generation. The shared target refuses incorrect or unloadable dependency installs, missing or wrong local tsc, stale or unlisted sources, duplicate exports and missing schema exports. Read-only runner refusal preserves inspected artifacts. The admitted paths stay fixed across identity captures, and emitted entry and leaf recovery and reuse cleanup run in the normal build. The same guard refuses a second schema and the generated source check refuses an indented alias. The sole runtime tsc command takes its output from the guarded MSBuild directory. The same directory drives validation, compiled import and shadow paths and publication. Generation-only copies remain offline. Token-based declaration discovery supports normal whitespace. The shared validator checks the complete staged source set before publication and checks the same contract on published sources. The shared physical guard checks only the owned directory chains and generated tree contents. Read-only admission applies the same physical output guard and returns the authoritative item count with the canonical path set. Pure schema path derivation supplies the emitted inventory before freshness or source inspection. Read-only checks the canonical published tree. The generator invokes a staged validation target that takes its path from the isolated intermediate root. Attempted global path and count overrides and an inherited staging path leave the canonical result and decoy unchanged. Both generators use one pure schema declaration inventory and the same physical path guard. The ordinary Build preflights TS publication, staging and stamp before restore. Each guard uses the actual SDK path to select its boundary. The runtime copied positive includes a pipe in the checkout path and emitted filename. The standalone generators and the runtime compiler pass quoted literal-dollar paths. The copied runtime target’s actual compiled output loads in Node. | E36, affected game and runner receipts |
| A6-1 | `TypeCheckBoundaryTests.TheGeneratedWireEntryModuleMustExistBeforeSuitesRun`, `IdentityTests.TheSuiteDigestCoversTheGeneratedWireModulesAndNoOtherIgnoredFile`, `TypeCheckBoundaryTests.MalformedWireAdmissionIdentityIsATypedRefusal`, `TypeCheckBoundaryTests.NoncanonicalWireAdmissionResultIsATypedRefusal`, `TypeCheckBoundaryTests.IncompleteWireAdmissionResultIsATypedRefusal`, `TypeCheckBoundaryTests.TwoCanonicalLeavesCannotStandInForTheGeneratedEntry`, `IdentityTests.RetiredNestedNpmManifestChangesSuiteIdentityByPresence`, `IdentityTests.GitListedRetiredNpmInputsContributePresenceOnly`, `IdentityTests.GitListedChildrenLeaveARetiredDirectoryIdentityUnchanged`, `IdentityTests.GitListedManifestCaseVariantsFollowFilesystemIdentity`, `IdentityTests.AdjacentManifestBackupParticipatesInSuiteIdentity`, `IdentityTests.RetiredManifestAppearingMidRunChangesIdentity` | Read-only admission accepts generated object companions and enums, returns repository-relative paths even when macOS spells the temp directory differently, and refuses missing, stale or retired sources. A missing admitted leaf changes the digest, and both root manifests independently change it. Unlisted generated paths and retired manifest contents stay outside the digest. Each retired nested npm file’s presence independently changes identity even when ignored by Git. Git-listed retired files and descendants of wrong-kind directories contribute only canonical presence. Physical case and canonical Unicode aliases, including `pacKage.json`, use that same record. Adding, editing or removing a Git-listed child of a still-present retired directory preserves identity. The fixtures assert the actual indexed spelling and use physical lookup to distinguish aliases from independent entries. Distinct files on a case-sensitive filesystem and adjacent paths such as `package.json.backup` remain suite sources. Removal restores the absent identity, and a manifest appearing during an existing native run triggers its final source-identity verdict. The real MSBuild result equals the complete generated relative path sequence after sorting. Invalid, missing and null result identities are typed refusals. Removing an admitted leaf changes identity through the same path list used at the start. Transport admission requires Success, one canonical entry and distinct canonical leaf paths. Malformed status, missing and duplicate roots, duplicate leaves and unrelated or nested paths receive ToolchainMissing. The same MSBuild result supplies the authoritative count. Omitted items or a missing, null or invalid count receive ToolchainMissing. A successful result with two distinct canonical leaves and a matching count still requires the canonical entry and reports its absence directly. | E38, `affected-runner.log` |
| AC1 | S2, S12, S13 and the generated-entry fixture | Retired manifests and records are checked by presence. The appropriate admission boundary refuses invalid installations or incomplete, stale or retired generated source. Runtime compilation restores expected JS output and removes retired JS leaves. Shared physical ownership covers freshness, publication, reconciliation and Clean. Wrong-kind owned outputs fail while still present. Missing compiled entry and leaf recovery remains valid. The shared preflight covers both standalone generators, SDK Clean and the root npm marker. Git-listed retired manifest paths and physical aliases contribute canonical presence only to suite identity. | E38, affected game and runner receipts |
| AC2 | Full local solution round. E46 boundary: `NodeHarnessTests.HarnessPasses`, `NodeHarnessTests.EmittedModuleWithoutTestsIsRefused` and `BrowserDeferralTests.EveryOtherTestFileOfTheSystemRunsOrIsDeferred` | Baseline and intentional test addition/deletion counts, zero failures and skips per project. The emitted worker contracts module has exit 0 and a synthetic TAP point, yet receives exactly `NoTests`. The emitted animation harness has one declared test and passes. Deferral checks account for test file routing, and the full round supplies execution evidence. | E9 records 7650/0/0. The repairs add 19 cases (E11), four (E13), three (E17), two (E20), seven (E22), thirteen (E24), twelve (E26), seven (E28), fifteen (E32), twelve (E34), eight (E36), three (E38) and one (E47). E47's `test-affected-runner.log` records the boundary and deferral checks. The matching integrated full run passed 7767/0/0 (E50, `test-solution.log`). |
| AC3 | Shared frame call-site source check including the H-S1 exclusion | Both production TypeScript consumers use the same helper through the generated IDs. The authored-state entry also uses the existing required-construction refusal. | E8, `frame-and-generated-output.log` and E20, `authored-save-typecheck.log` |
| AC4 | `VerifyPolicy --maintainability` | Both canaries, coupling summary and zero gate violations | E47, `maintainability.log` |

## Auto Run Result

S5c-2's one schema feeds both language generators in their owning projects. The required local round executes strict emitted TypeScript with the root FlatBuffers 25.2.10 pin and proves decoded agreement across four roots. The read-only runner uses the same artifact admission, and the side conditions on retired inputs, physical output ownership and emitted Node inputs have independent checks.

The original dispatch baseline remains `cb29a0763d2bba289e5018b82698e3a7f86478a3`. The single integration with published Baseball `b5ce3e0e2b9d998d628a6ea47640e15b182902da` preserved its shared entries. The 35 owned paths passed final scope and simplification (E49). Matching compiler captures, both maintainability canaries, 828 registration, 16 affected runner, and 7767/0/0 full local are in E47 and E50. The normal hooks verify the staged candidate and the independent approver checks the exact committed SHA.

Browser qualification remains assigned to the post-catalogue E2E phase. The production Release worker bundles and web publish were scoped from project sources. Remote build, Pages artifact, deployment and release qualification remain unverified. S5c-3 owns the remaining hand restatements and `ENGINE-WIRE-ABI` owns TypeScript structural verification.
