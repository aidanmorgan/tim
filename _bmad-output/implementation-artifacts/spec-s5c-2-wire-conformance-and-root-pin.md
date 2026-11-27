---
title: 'One schema file is the ABI: TypeScript generated in every build graph, pinned at the root and proven to agree with C# (S5c-2)'
type: 'refactor'
created: '2026-11-26'
status: 'draft'
baseline_revision: 'captured at dispatch (the main that contains S5c-1)'
review_loop_iteration: 0
followup_review_recommended: false
context:
  - '{project-root}/schemas/README.md'
  - '{project-root}/docs/testing-guidance.md'
  - '{project-root}/docs/delivery-workflow.md'
  - '{project-root}/_bmad-output/implementation-artifacts/spec-s5c-one-wire-schema-file.md'
  - '{project-root}/_bmad-output/implementation-artifacts/spec-s5b-generate-typescript-wire-in-build.md'
  - '{project-root}/docs/planning/elements/readiness/owner-decisions-2026-11-22.md'
warnings: []
deferred: []
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
- **Cross-language conformance.** A required test runs both directions, for every root, every command and response kind, and every observation payload arm: a strict-TypeScript node script, compiled by the pinned `tsc` in the normal build, decodes and re-encodes buffers that the C# codecs wrote, and the C# production codecs decode what TypeScript wrote. It asserts decoded semantic fields and checks that both sides refuse malformed frames.
- **One TypeScript frame check.** `tools/runtime/wire-frame.ts` holds the one TypeScript frame check and identifier conversion; `workshop-wire.ts`'s own are deleted (H-S3).
- **Runtime pin.** The exact `flatbuffers` 25.2.10 pin moves to the root `package.json` and lock, reversing S5b's coordinator decision B (24p ruling (d); D6).
- **The runner** stops reading the deleted schema record and checks staleness against the files that now declare generation (A6-1).

## Boundaries & Constraints

**Always:**
- **One schema path, defined once.** Each generation step, C# and TypeScript, passes exactly `schemas/workshop_wire.fbs` to the pinned `flatc`. The path is defined once, as a property in `tools/runtime/FlatcCompiler.targets` read by both steps; the vendored project's `WireSchema` (`CuriousContraptions.FlatBuffers.csproj:43`) takes it, and no second `WireSchema` include is declared in a file every project imports, which would pass the file twice and trip S2 (audit 6, D3).
- **TypeScript identifiers come from generated code.** TypeScript takes an identifier only from the generated `WireRootIdentifier`, through exactly one conversion in `tools/runtime/wire-frame.ts`. It is total over `uint32`: any value, member or not, maps to its four little-endian bytes as a four-character identifier. No hand TypeScript in `tools/runtime/` or `tools/e2e/workshop-wire.ts` holds a current root identifier literal after this slice.
- **Every TypeScript decode checks the frame first.** There is exactly one TypeScript frame check, in `tools/runtime/wire-frame.ts`, compiled by the runtime TypeScript chain and imported by both the conformance script and `tools/e2e/workshop-wire.ts`. The size prefix must cover the buffer exactly, then the identifier must be the expected root's. It throws a plain `Error` whose message names the failed check: shorter than its size prefix, truncated, trailing bytes, or wrong identifier. The conformance test matches each message to the `WireFaultReason` C# gives for the same buffer (24p ruling (a)); that mapping lives in the C# test, so TypeScript holds no mirror of `WireFaultReason`.
- **No TypeScript verifier.** The pinned compiler writes none: 0 of the 137 generated TypeScript files mention one, and the `flatbuffers@25.2.10` package has none. TypeScript therefore decodes only what passes the frame check. **Evidence limit:** until the first production TypeScript table decoder provides TypeScript verification without hand code (H-R1, `ENGINE-WIRE-ABI`), a malformed table that passes the size and identifier checks is refused only on the C# side.
- **The conformance check is a required test, not a browser gate.** AGENTS.md says "the game system's tests are the C# solution's test projects and the node harnesses for game runtime code", so a node harness over the generated runtime bindings is a required system test. Only Chrome/Playwright suites and the tests of their toolchain are deferred.
  - The script is strict TypeScript (`strict: true`, no `any`), compiled by the pinned `tsc` 5.9.3 (root `package.json:10`) in the normal build and run with the pinned Node 26.12.1 against the compiled output. It imports `flatbuffers` from the root `node_modules`.
  - The test asserts decoded semantic fields with exact values: the wire carries values exactly, so exact equality is a defined-interface invariant. It never compares bytes or hashes.
- **The generated TypeScript is compiled in every build graph, after generation.** One runtime TypeScript compile (`npm run build:runtime`, `tools/runtime/RuntimeTypeScript.targets:29-33`) compiles the generated module, `wire-frame.ts` and the conformance script, in the Abstractions instance (`:23`, `:29-30`) in every configuration.
  - TypeScript generation joins that chain, before the compile, and reads the one schema file (D3). It stages its output in the building instance's own intermediate directory, moves the per-type files into `tools/e2e/.wire/` first and the entry module last, so an interrupted run never leaves a partial set behind an entry module.
  - Two writers never overlap. Each local graph has one Abstractions instance. In CI's graph the Release and Debug Abstractions instances are reached from different targets of the one web project (reference resolution, then `BuildWorkshopEditorMetadata`), so they run one after the other; task 1(c) records that ordering.
  - Inputs and Outputs follow S5b's pattern. A separate target writes an intermediate list stamp with `WriteOnlyWhenDifferent` and registers it in `FileWrites`, so Clean deletes it and every `--no-incremental` capture build generates exactly once. The generation target's Inputs are that stamp, the schema file, `tools/runtime/FlatcCompiler.targets` and `$(MSBuildThisFileFullPath)`, so a change to the generation command regenerates. Its only Output is the entry module; no schema record is written (A6-1).
  - The stray-output and missing-entry refusals move with the target (D10).
  - `tools/runtime/FlatcCompiler.targets` is imported once, by `tools/runtime/RuntimeTypeScript.targets`, which every project receives through `Directory.Build.targets:41`, the vendored one included; the vendored project adds no import (no MSB4011). S5's compiler provisioning serves both steps, and the compiler cache is still needed only when generating.
- **The TypeScript runtime is pinned once, at the root.** `flatbuffers` is exactly `25.2.10` in the root `package.json` `devDependencies` and the root `package-lock.json`. `tools/e2e/package.json` and `tools/e2e/package-lock.json` are deleted. No tsconfig maps `flatbuffers` to a path.
- **Forward-only.** No second generation step, schema record, frame check or pin; no fallback; S5b's Debug-only rule and its include ordering are deleted, not kept beside the new chain.

**Never:**
- A hand-written TypeScript verifier, any other new generator, a TypeScript post-processor script, a hand mirror of `WireFaultReason` in TypeScript, or a second TypeScript frame check.
- Running or gating the browser toolchain (`tsx`, Playwright) in a required run, or the deferred `tools/e2e/unit/run-reset-snapshot.test.ts`. The `BrowserDeferral` list gains, loses and re-gates no entry; only reason texts that name the `tools/e2e` manifest change.
- Editing lane 3's runner files: `tools/E2eRunner/RunnerError.cs`, `ProcessTable.cs`, `SuiteProcess.cs`, `PreviewHost.cs`, `Scheduler.cs`, `tools/E2eRunner.Tests/ProcessesTests.cs`, `SchedulerTests.cs`.
- Hashes or byte comparisons of encoded output as acceptance; mutants; a local Release worker build or web publish.
- Any schema change.

## I/O & Edge-Case Matrix

| Row | Input / State | Expected Output / Behavior | Error Handling |
|-----|--------------|---------------------------|----------------|
| S2 | The binary logs of the round (`solution.binlog`, `playtest.binlog`, `web.binlog`) | Every `flatc` invocation, C# and TypeScript, names exactly the one schema file as input. TypeScript generation starts exactly once per graph, in the Abstractions instance | Names the log, the invocation and the extra input or count |
| S3 | The generated `WireRootIdentifier` in C#, and the same members read through the TypeScript script | Four members. Each name is a table that the production codecs decode as a root. The values are distinct, and each decodes to four printable ASCII bytes. C# and TypeScript give the same four bytes per member. The `SavedConstruction` member equals the schema's `file_identifier`. A save the codec wrote, which the generated finisher frames with the `file_identifier`, is admitted by `ICommandCodec.ValidateSaveFrame`, which checks against the converted `WireRootIdentifier.SavedConstruction`. That save, re-finished by the generated builder with another member's converted identifier, is refused with `UnknownIdentifier`. No frame bytes are read by the test | Names the member and the refusal |
| S4 | The production codecs encode one value of each root: every value of the encoder's command-kind enum (`Enum.GetValues<WorkshopCommandKind>()`, 14 today), every value of its response-kind enum (2), every non-None `ObservationPayload` arm (11), and one save | The script frame-checks each buffer with the identifier from the generated `WireRootIdentifier`, decodes it with the generated accessors and reports the unpacked object. Named fields equal the encoded domain values exactly: kind, session bytes, sequence, a nested table's identity, a vector's length, an f32 and a `uint64` | Any frame refusal or missing field fails, naming the root and the case |
| S5 | Each S4 buffer re-encoded by TypeScript with the generated object API (unpack, then pack with `finishSizePrefixed` and the generated identifier), once unchanged and once with one named field set to a new value | The C# production decoder admits it. The decoded domain value deep-equals the original, field by field, with collections compared by element. In the set case it differs exactly in that field | A C# refusal names its `WireFaultReason` |
| S6 | Malformed frames made from each root's valid buffer: shorter than the size prefix, wrong identifier (another root's), truncated by one byte, one trailing byte | C# refuses with `Truncated`, `UnknownIdentifier`, `Truncated` and `TrailingBytes` respectively; a save gets the matching `WorkshopSaveRejection`. TypeScript refuses the same buffer, and its message names the failed check the C# test maps to that reason | The test names the case if either side admits the buffer or the checks differ |
| S7 | A buffer whose root table offset points outside the buffer; a save whose required field `PuzzleRecord.assistance` is absent | C# refuses with `VerifierRejected`; a save gets `WorkshopSaveRejection.VerifierRejected`. TypeScript: no assertion (evidence limit, see Always) | C# names the reason |
| S12 | The root and `tools/e2e` manifests and tsconfigs | The root `package.json` has `flatbuffers` exactly `25.2.10` in `devDependencies`, locked at `25.2.10`, equal to `<FlatcVersion>` in `tools/runtime/FlatcCompiler.targets`. `tools/e2e/package.json` and its lock are absent. No tsconfig maps `flatbuffers` | Names the file and value |
| S13 | A field added to the schema in a copied tree, then incremental generation | The C# accessor exists (S5's vendored-project fixture) and the TypeScript accessor exists (the generation target on an Abstractions copy, task 1(e)). Control: both absent before the edit | Build error names the file |

</intent-contract>

## Code Map

Baseline: captured at dispatch, the main that contains S5c-1 and lane 3's E2E runner process lifecycle slice. Citations below were read at `39aaba70` (`audit-s5c-6.md`, A6-11 applied); S5c-1 and lane 3 move some of them, so the dispatch re-audit re-reads each one.

**Status:** draft. Runs in lane 2 after S5c-1 publishes, and after lane 3's lifecycle slice publishes; S5c-3 follows (coordinator ruling, 26 November 2026).

**Planning verdict:** not yet audited as its own contract. Its content comes from the S5c contract and audits 1 to 6 (run log E1). A bounded re-audit at its own dispatch, from the main that contains S5c-1 and lane 3's slice, is mandatory before the build.

**Review tier:** engine (build configuration, TypeScript, the vendored project, runner tools, tests). **Shape:** one structural slice.

Owned scope:
- **`CuriousContraptions.FlatBuffers/CuriousContraptions.FlatBuffers.csproj`** (A6-3 (a)):
  - `WireSchema` (`:43`, the one item S5c-1 left, FO1-3 ruling) takes the one path from `FlatcCompiler.targets`; `GenerateWireAccessors` (`:127-133`) passes it, and its Inputs (`:128`) gain the shared file, so a compiler pin change still regenerates C#. (`ListWireSchemas` was deleted by S5c-1, FO1-3 ruling.)
  - Deleted: the comment `:151-168`, `WireTypeScriptDirectory` and `_WireTypeScriptStaging` (`:169-172`), `GenerateWireTypeScript` (`:173-205`, the Debug-only condition at `:173`), `RemoveWireTypeScriptStaging` (`:207-209`) (`OrderWireSchemas` was deleted by S5c-1; FO1-3 ruling). Lines re-measured at dispatch.
  - Provisioning moves to `tools/runtime/FlatcCompiler.targets`: `ProvisionFlatBuffersCompiler` (`:57`), `DeleteFlatcTemporary` (`:114`) and the properties they read (`:23-41`). The comments that describe provisioning here (`:5-8`, `:55-56`, `:124-126`) are reworded.
- **`tools/runtime/FlatcCompiler.targets`** (new): the compiler properties, digests, provisioning targets and the schema path property.
- **`tools/runtime/RuntimeTypeScript.targets`** (`:23-52`): the stamp target, the generation target (`--ts --gen-object-api` on the one file, into `tools/e2e/.wire/`, with S5b's annotation normalisation and the moved stray and missing-entry refusals) and its staging-cleanup `OnError` target, before `BuildRuntimeTypeScript`, whose Inputs gain the generated entry module.
- **`tools/runtime/wire-frame.ts`** (new): the one TypeScript frame check and the one `WireRootIdentifier`-to-bytes conversion; it imports the generated module.
- **`tools/runtime/wire-conformance.ts`** (new): reads JSON on stdin and writes JSON on stdout, like `tools/runtime/event-roundtrip.ts` (driven from `CuriousContraptions.tests/EventRingConsumerTests.cs:761-773`). It imports the generated entry module, `wire-frame.ts` and `flatbuffers`; looks up a root class by its `WireRootIdentifier` member name; frame-checks the buffer; then either decodes and unpacks, or unpacks, sets one named field if asked, packs and finishes size-prefixed. It reports every JavaScript `number` as its IEEE-754 binary64 bit pattern in hex (exact for every f32, integer and double, and it keeps -0) and every `bigint` as a decimal string; the C# side widens its expected values to binary64. It holds no field, type or slot name of its own.
- **`CuriousContraptions.tests/WireConformanceTests.cs`** (new): rows S3 to S7, driving the script with node.
- **`CuriousContraptions.tests/WireSchemaGuards.cs`**: adds S2 (reusing `CSharpStaticGateTests.ReplayCscRuns`) and S12 (S5b's T7 inverted, reading `<FlatcVersion>` from `tools/runtime/FlatcCompiler.targets`; A6-3 (f)).
- **`CuriousContraptions.tests/CSharpStaticGateTests.cs`**: the vendored project file's pin (`:116`); `PinnedTargets` (`:1335-1364`) for the changed `BuildRuntimeTypeScript`, the moved generation, stamp and staging-cleanup targets (A6-3 (b)), and `tools/runtime/FlatcCompiler.targets:ProvisionFlatBuffersCompiler` and `:DeleteFlatcTemporary` with their `DownloadFile` and `Exec`; `PinnedImports` (`:1368-1377`) for the shared file's one importer.
- **`CuriousContraptions.tests/WorkshopWireTypeScriptTests.cs`**:
  - T1 (`:26-154`) is amended for `--gen-object-api` (A6-4): each entry line exports `<Name>`, plus `<Name>T` for every table and struct; enums export the name alone. Each type file declares `export class <Name> implements flatbuffers.IUnpackableObject<<Name>T> {` and `export class <Name>T implements flatbuffers.IGeneratedObject {` (an enum declares `export enum <Name> {`). `EntryExport` (`:105`), `TypeDeclaration` (`:106`), `EntryExports` (`:100-101`) and their controls (`:43-58`) follow.
  - The schema record (A6-1): `SchemaRecord` (`:19`), `SchemaRecordViolations` (`:127-138`) and their controls (`:59-68`) are deleted.
  - T7 (`:401`) moves to S12 (A6-3 (f)). T8 (`:487-557`, with `GraphLogs`, `Replay`, `Target`, `Task`, `Context`) is deleted; S2 covers its count (A6-3 (g)). The class summary `:7-12` and constants `:21-22` are reworded.
  - T5 (`:156-397`) is kept (A6-3 (h)). Its scope is `tools/e2e/*.ts` and `unit/*.ts`; `tools/runtime/wire-frame.ts` and `wire-conformance.ts` are outside it, covered by "holds no field, type or slot name of its own" here and by S11 (S5c-3).
- **`CuriousContraptions.tests/WorkshopWireBuildTests.cs`** (S5 and S5b cases, as S5c-1 left them):
  - **Deleted:** (`ASchemaSetWithoutOneRootReachingEverySchemaRefusesTheBuild` and `ADeletedSchemaFileLeavesNoGeneratedSource` are already deleted by S5c-1, FO1-3 ruling); `AReleaseBuildWritesNoTypeScript` (its behaviour is removed: Release graphs now generate); T3's schema-record case (`:155-157`; A6-1).
  - **Merged into S13:** `AnAddedSchemaFieldIsATypeScriptAccessorAfterAnIncrementalBuild`.
  - **Moved to the Abstractions-copy fixture (task 1(e)):** `AnUnchangedSchemaDoesNotRegenerateTypeScript` (steps rewritten for the intermediate staging folder), `ARemovedSchemaTypeLeavesNoTypeScript` (asserting the new declaration form, A6-4), `ACheckoutPathWithASpaceBuildsACompleteModuleSet` (the command's quoting still matters), and the stray-output refusal (D10), S5c-1's one-file case with a second namespace appended.
  - **Assertions removed:** the "refused build wrote no TypeScript" assertions in `APinThatTheCachedCompilerDoesNotMatchRefusesTheBuild` and the span-define cases, which would be vacuous; the pin-mismatch case is repeated once on the Abstractions-copy fixture, asserting that no TypeScript is written. The pin-mismatch cases edit the digest in the shared file, on both fixtures.
  - `SharedBuildFiles` (`:37-38`) gains `tools/runtime/FlatcCompiler.targets`.
- **`CuriousContraptions.tests/TimingDetectorTests.cs:129-158`** (T9): kept unchanged; the closure follows relative imports (`TimingDetector.cs:271-296`), so `workshop-wire.ts` importing `../runtime/wire-frame.ts` resolves (A6-3 (i)).
- **The e2e runner** (A6-1; deferred-runner scope, which the required round tests through `tools/E2eRunner.Tests`):
  - `tools/E2eRunner/Toolchain.cs` `RequireGeneratedWireModule` (`:74-100`): `GeneratedWireRecord` (`:14`) and the name-set comparison (`:93-96`) are deleted, since with one schema file there is nothing to compare. The presence check stays. The staleness check compares the entry module with `schemas/workshop_wire.fbs`, `tools/runtime/RuntimeTypeScript.targets` and `tools/runtime/FlatcCompiler.targets`; `WireGeneratingProject` (`:15`) is replaced by those two paths. The remedy texts at `:26`, `:84` and `:99` name "a build of the solution (or of CuriousContraptions.Abstractions)"; the summary `:74-78` is reworded. `RequireE2eRuntime` (`:59-72`) and `PinnedRuntime` (`:110-130`) resolve the root `node_modules/flatbuffers`.
  - `tools/E2eRunner/SourceIdentity.cs` (`IsSuiteSource`, `:37-39`): the suite digest also covers `tools/runtime/wire-frame.ts`, which the suites now import.
  - `tools/E2eRunner/BrowserDeferral.cs`: the reason texts at `:18-20` (phrase at `:19`) and `:44-45` name the root manifest.
  - `tools/E2eRunner.Tests/TypeCheckBoundaryTests.cs`: `TheE2eFlatBuffersRuntimeMustBeThePinnedVersionInstalledInToolsE2e` (`:31-93`) becomes the root-installed check, its "copy above `tools/e2e`" control (`:59-60`) inverting to "a copy only in `tools/e2e/node_modules`"; in `TheGeneratedWireEntryModuleMustExistBeforeSuitesRun` (`:129-221`; constants `:230-232`) the record cases (`:160-174`, `:202-214`) are deleted, the project-file cases (`:155-159`, `:182-200`) retargeted to the two declaring files, and the expected texts follow the remedy.
  - `tools/E2eRunner.Tests/IdentityTests.cs` `TheSuiteDigestCoversTheGeneratedWireModulesAndNoOtherIgnoredFile` (`:176-212`): the record step (`:192-193`, `:203-205`) is replaced by a control that a change to `tools/runtime/wire-frame.ts` changes the digest.
  - `tools/E2eRunner.Tests/E2eRunner.Tests.csproj:21-22` (the reference to the vendored project "to build the generated TypeScript wire modules") is deleted; the Abstractions reference (`:19-20`) now generates `.wire/` (A6-3 (c)).
- **`tools/e2e/workshop-wire.ts`** (H-S3, placed here by the forward-only rule; run log E1): the identifier constants (`:14-16`) and `openFrame` (`:20-29`) are deleted; the decoders (`:32`, `:74`, `:78`) and the mock finishers (`:316`, `:379`) take `wire-frame.ts` and the generated `WireRootIdentifier`; the `CCWS` message (`:79`) names the member; the header `:1-4` and `:13` are reworded (A6-3 (d)). The authored-state dump and mocks are otherwise S5c-3's (H-S4, H-S5).
- **Runtime pin:** the root `package.json` and `package-lock.json` gain `flatbuffers` at exactly `25.2.10`, written by npm with the pinned Node; `tools/e2e/package.json` and `tools/e2e/package-lock.json` are deleted; `tools/e2e/README.md` `:68` and `:82` name the root manifest.
- **`schemas/README.md`**: `:41` (the digests are pinned in `tools/runtime/FlatcCompiler.targets`) and the TypeScript section (`:54-88`, including `:64` and `:69-71`) describe the generation chain, the object API, the root pin and the conformance test.
- **`_bmad/custom/bmad-loop-protocol.md:251`** names the Abstractions instance as the writer of `tools/e2e/.wire/` (A6-3 (e); a shared document under 24m, line-local).
- **Planning records:** this contract and its run log; `TODO.md`; `sprint-status.yaml` (key `s5c-2-wire-conformance-and-root-pin`); `delivery-batches.md` (S5c); `deferred-work.md` (closes S5b's carried BH2 pin item).

Inventory row owned here: **H-S3** (`tools/e2e/workshop-wire.ts:14-16`, `:20-29`: identifier literals and a second frame check; deleted, `wire-frame.ts` replaces them). The full inventory is in the S5c-3 contract.

### Capability probe

No solver law, worker message content, wire field or shared presentation contract changes.
- **Present:** the one schema file and C# identifiers from the enum (S5c-1); C# generation (S5; `WorkshopWireBuildTests`); TypeScript generation (S5b; `WorkshopWireTypeScriptTests`), moved here; the runtime TypeScript compile (`RuntimeTypeScript.targets:23-33`); node harness execution (`tools/E2eRunner.Tests/NodeHarnessTests.cs:12-29`; `EventRingConsumerTests.cs:761-773`).
- **Missing, added here:** TypeScript generation in every build graph; one TypeScript frame check; a required test that runs the generated TypeScript; the root pin.
- **Missing, owned elsewhere:** a TypeScript verifier (H-R1, `ENGINE-WIRE-ABI`). No required-now owner decision is open.

### Decisions

- **D3, generation chain (task 1 is the hard gate).** TypeScript generation moves into the Abstractions chain ahead of the runtime compile, because that compile runs there in every configuration and has no build-order relation to the vendored project. Generation output stays at `tools/e2e/.wire/` (S5b D1), so the importers stay the same. Derivation, CI graph and alternatives: run log E1.
- **D4, object API for TypeScript only.** `--gen-object-api` is passed only to the TypeScript step (confirmed at 25.2.10 under strict `tsc` 5.9.3; audit 6 probe adds no `any`), giving the conformance round trip, and S5c-3's snapshot, dump and mocks, a surface that covers every field with no hand names. The C# surface is unchanged.
- **D6, pin at the root (24p ruling (d); reverses coordinator decision B).** The conformance script and `wire-frame.ts` are compiled in every graph and a required test runs them, so the runtime belongs in the root manifest, with one pin. Reasoning: run log E1.
- **D8, the S5 comparison.** "Deep-equals" in S5 is xUnit `Assert.Equivalent(expected, actual, strict: true)`, because `WorkshopCompiledScene` is a class without `Equals`; scene-free values may keep `Assert.Equal`. No NaN case: save prevalidation refuses non-finite values (`engine/gpu/WorkshopSavePrevalidation.cs:104`, `:169`, `:189-192`).
- **D10, S5b's two output refusals move with the target (A6-3 (a)).** The stray-output and missing-entry refusals (`CuriousContraptions.FlatBuffers.csproj:182-187`) cost two lines and protect the set the entry module names; their stray case moves to the Abstractions-copy fixture.

### Protections this change removes, and what replaces them

- **S5b's include ordering and its refusals** are replaced by S1 (one file, no include) and S2 (each run reads exactly that file).
- **S5b's Debug-only one-writer rule** is replaced by the Abstractions chain's project-name guard and task 1(c)'s ordering.
- **The schema record `schemas.txt`** is deleted with its runner check; with one file the presence and staleness checks remain.
- **Residuals:** the e2e module graph is still type-checked only in the browser phase (`tools/E2eRunner/TypeCheck.cs`), while the generated module, `wire-frame.ts` and the conformance script are type-checked in every build; there is no TypeScript verifier (H-R1); CI's build graph is not observed locally (D3).

### Concurrency

- Lane 3's E2E runner process lifecycle slice (`deferred-work.md`, "E2E runner process lifecycle") may edit `tools/E2eRunner/Toolchain.cs`, `tools/E2eRunner/SourceIdentity.cs`, `tools/E2eRunner.Tests/IdentityTests.cs` and `tools/E2eRunner.Tests/TypeCheckBoundaryTests.cs`, which S5c-2 also edits. S5c-2 edits none of `RunnerError.cs`, `ProcessTable.cs`, `SuiteProcess.cs`, `PreviewHost.cs`, `Scheduler.cs`, `ProcessesTests.cs` or `SchedulerTests.cs`. Under owner decision 24m the two slices may not edit the same file concurrently, so they run one after the other: S5c-2 starts from the main that contains lane 3's slice (coordinator ruling, 26 November 2026; A6-2's repair, by sequencing).
- Shared with S5, S5b and S5c-1: the vendored project file, `WorkshopWireBuildTests.cs`, `CSharpStaticGateTests.cs`, `schemas/README.md` and the `tools/e2e` files.
- Shared documents (owner decision 24m): `TODO.md`, `deferred-work.md`, `delivery-batches.md`, `completion-ledger.md`, `sprint-status.yaml` and `_bmad/custom/bmad-loop-protocol.md`; line-local edits only.
- New files are staged with `git add` before the candidate commit.

## Tasks & Acceptance

**Execution:**
1. Hard gate, in `.tools/s5c-evidence/`, with the pinned cached compiler and the pinned Node. If any check fails, stop and return to planning.
   - **(a) Object API.** Run `flatc --ts --gen-object-api` on the one file and record that the entry module re-exports every declaration in the A6-4 form.
   - **(b) Compile and import.** Compile the generated TypeScript, a stub of `wire-frame.ts` and a stub of the conformance script under `tsconfig.runtime.json`'s options with the pinned `tsc`, and import them from the compiled JS with `flatbuffers` 25.2.10 resolved from the root.
   - **(c) Build graphs.** From the three binary logs of a baseline round, record per graph which project instances run `RestoreRuntimeTypeScript`, `BuildRuntimeTypeScript` and `GenerateWireTypeScript`. Derive CI's publish graph from the project references and the web host's MSBuild calls, record its Abstractions instance count and confirm they run one after the other. Confirm that D3's placement generates exactly once before the compile in each local graph, `web.binlog` included (the web graph's `BuildWorkshopEditorMetadata` call passes `Configuration=Debug` and removes `RuntimeIdentifier;SelfContained`, so whether it reuses the outer instance is measured, not assumed).
   - **(e) Build-test fixture** (lettered (e) as row S13 cites it). Define the copied-tree fixture that invokes the moved generation target on an Abstractions copy, offline (generation needs only the compiler cache). Record the files it copies and its run time against the full round.
2. **Build:** `FlatcCompiler.targets`, the moved generation, the vendored-project deletions, the pins in the static gate. Rows S2 and S13.
3. **Runtime pin:** the root manifest and lock, the `tools/e2e` manifest deletion. Row S12.
4. **Conformance:** `wire-frame.ts`, `wire-conformance.ts`, `WireConformanceTests.cs`, H-S3. Rows S3 to S7.
5. **Runner and records:** A6-1's runner edits, the build-test dispositions, the README sections, `bmad-loop-protocol.md:251` and the planning records; close S5c-2 with its numbers.

**Acceptance Criteria:**
- AC1: Given the candidate, when the guards run, then S2, S12 and S13 pass, and `tools/e2e/package.json` and `tools/e2e/.wire/schemas.txt` are absent after a build.
- AC2: Given the full local round, when it completes, then failures are 0 and skipped are 0 in every project, the total is the baseline plus the new tests minus the deleted ones (delta measured at dispatch), and the run log names each added and deleted test with its cover.
- AC3: Given the hand TypeScript sources outside S5c-3's H-S1 hand decoder (`tools/e2e/run-reset-snapshot.ts:182-293`, whose `rootPosition` at `:289-292` reads `SIZE_PREFIX_LENGTH` and which S5c-3 deletes), when they are searched, then only `tools/runtime/wire-frame.ts` calls `__has_identifier` or reads `SIZE_PREFIX_LENGTH`, and H-S3's constants are gone.
- AC4: Given the maintainability gate, when it runs, then both canaries pass and violations are 0.

## Spec Change Log

- 26 November 2026: created by the S5c split (coordinator ruling; audit 6, A6-10), from the S5c contract and audits 1 to 6. Carries A6-1, A6-3 (a) to (i), A6-4 and A6-11's citations; A6-2 by sequencing; H-S3 placed here (run log E1).

## Review Triage Log

## Design Notes

1. **Delivered:** both bindings generated from the one file in every build graph, and a required test proving they agree in both directions. No gameplay, solver or presentation change.
2. **Reusable:** one TypeScript frame check and identifier conversion; one compiler definition for both steps.
3. **Declared:** the schema file; the conformance script finds root classes by the generated enum's member names.
4. **Execution:** schema → `flatc` (C# in the vendored project; TypeScript in the runtime chain) → compile → frame check → decode.
5. **Ownership:** the runtime TypeScript chain owns TypeScript generation and compilation; Core owns the C# frame boundary.
6. **Generality:** S4 and S5 run over every command kind, response kind and payload arm, with C#-derived expectations; no name is special-cased.
7. **Deleted:** S5b's vendored-project TypeScript target (`OrderWireSchemas` and the include ordering were deleted by S5c-1, FO1-3 ruling), Debug-only guard, schema record and `tools/e2e` manifest; `workshop-wire.ts`'s identifier constants and frame check.
8. **Uncertain:** task 1's gates (CI graph included); H-R1. Nothing required now is open.

**Architecture guards.** S2 and S12 read logs and manifests; they live in `WireSchemaGuards`, each with a positive control.

## Verification

Executor: the implementation owner.

Environment: `PATH=~/.local/share/tim-toolchain/dotnet-10.0.403:~/.local/share/tim-toolchain/node-v26.12.1-darwin-arm64/bin:$PATH` and `DOTNET_ROOT=~/.local/share/tim-toolchain/dotnet-10.0.403`. Record the session environment check in the run log.

**Commands:**
- The five compiler-log capture commands (`docs/delivery-workflow.md:38-53`). Expected: every build succeeds, generates from the one file and compiles `.runtime-build/tools/runtime/wire-conformance.js`.
- `dotnet run --file tools/anvil/VerifyPolicy.cs -- --maintainability --project-root .` Expected: AC4.
- The registration check filter. Expected: 0 failed, 0 skipped (test files are added).
- The scoped run, with `CSHARP_STATIC_GATE_BINLOGS` set. Expected: 0 failed, 0 skipped.

  `dotnet test CuriousContraptions.slnx -c Debug -f net10.0 --no-build --no-restore --nologo -noAutoResponse --filter "FullyQualifiedName~WireConformanceTests|FullyQualifiedName~WireSchemaGuards|FullyQualifiedName~WorkshopWireBuildTests|FullyQualifiedName~WorkshopWireTypeScriptTests|FullyQualifiedName~CSharpStaticGateTests|FullyQualifiedName~TimingDetectorTests|FullyQualifiedName~TypeCheckBoundaryTests|FullyQualifiedName~IdentityTests"`
- `git add -N` on the new files, then `git diff --check <baseline_revision>`. Expected: clean.
- One full local round. Expected: AC2.

### Acceptance-to-test table

No tolerance applies. Decoded integers and f32 values must be exactly equal, because the wire carries them exactly (a defined-interface invariant). Every other assertion is a name, a count, a presence or a typed refusal.

| Rows | Named discriminating test | Control and boundary | Execution |
|---|---|---|---|
| S2 | `WireSchemaGuards.EveryCompilerRunReadsOnlyTheOneSchemaAndTypeScriptGeneratesOncePerGraph` (binary-log replay) | Control: a synthetic invocation naming two inputs is refused, and so are counts of 0 and 2, each naming the log. Boundary: `web.binlog` | planned |
| S3 | `WireConformanceTests.EveryRootIdentifierIsGeneratedSchemaDataBothSidesAgreeOn` | Control: a buffer finished with another member's identifier is refused by both sides. Boundary: a finisher-written save is admitted by `ValidateSaveFrame`, which fails if the `file_identifier` differs from the converted `SavedConstruction` member; the same save finished with another member's identifier is refused | planned |
| S4 | `WireConformanceTests.TypeScriptDecodesEveryRootAndArmCSharpEncoded`, a theory over the roots, the command-kind values, the response-kind values and the 11 payload arms | Control: a case whose C#-side expected value is altered fails, naming the field. Boundary: -0 on an f32 field the encoder admits (chosen at dispatch), and the `uint64` maximum | planned |
| S5 | `WireConformanceTests.CSharpDecodesEveryRootTypeScriptEncoded` (D8 comparison) | Control: the set-field case differs in exactly that field. Boundary: an empty vector and an absent optional table | planned |
| S6 | `WireConformanceTests.MalformedFramesAreRefusedOnBothSidesForTheSameCheck` | Each of the four faults, for each root, on both sides, with the TypeScript message matched to the C# reason | planned |
| S7 | `WireConformanceTests.VerifierFailuresAreRefusedInCSharp` | Control: the same buffer with the offset in range is admitted | planned |
| S12 | `WireSchemaGuards.TheFlatBuffersRuntimeIsPinnedAtTheRoot` (S5b's T7, inverted) | Control: a version range, a `tools/e2e/package.json` naming `flatbuffers`, a version differing from `<FlatcVersion>`, and a `paths` mapping are each refused | planned |
| S13 | `WorkshopWireBuildTests.AnAddedSchemaFieldIsAnAccessorInBothLanguagesAfterAnIncrementalBuild` | Control: the accessor is absent before the edit | planned |
| A6-1 | `TypeCheckBoundaryTests.TheGeneratedWireEntryModuleMustExistBeforeSuitesRun`; `IdentityTests.TheSuiteDigestCoversTheGeneratedWireModulesAndNoOtherIgnoredFile` | Control: a change to either declaring file makes the module stale; a change to `wire-frame.ts` changes the digest | planned |
| AC1 to AC4 | The guards, `rg`, the full round, the maintainability gate | The baseline round is recorded at the start | planned |
