---
title: 'One schema file is the ABI: both bindings generated from it and proven to agree (S5c)'
type: 'refactor'
created: '2026-11-25'
status: 'draft'
baseline_revision: 'captured at dispatch (the main that contains S5b)'
review_loop_iteration: 0
followup_review_recommended: false
context:
  - '{project-root}/schemas/README.md'
  - '{project-root}/docs/testing-guidance.md'
  - '{project-root}/docs/delivery-workflow.md'
  - '{project-root}/_bmad-output/implementation-artifacts/spec-s5-generate-wire-accessors-in-build.md'
  - '{project-root}/_bmad-output/implementation-artifacts/spec-s5b-generate-typescript-wire-in-build.md'
  - '{project-root}/docs/planning/elements/readiness/owner-decisions-2026-11-22.md'
warnings: ['oversized']
deferred: []
---

<intent-contract>

## Intent

**Problem:** Owner decision 24p (25 November 2026): "we need the flatbuffers to have one spec as a file, that the dotnet and typescript bindings are generated from, this is the foundation of the ABI between the systems and threads and needs to be resillient".

Today the FlatBuffers ABI is two files:
- `schemas/workshop_wire.fbs`, with 127 declarations;
- `schemas/workshop_authoring.fbs`, with 9 declarations and `include "workshop_wire.fbs"` at `:1`.

There are four roots: `WorkshopCommand` (`CWCM`), `WorkshopResponse` (`CWRM`), `WorkshopObservationBatch` (`CCWO`) and `SavedConstruction` (`CCWS`). The schema declares only one `file_identifier` (`workshop_authoring.fbs:95`). The other three identifiers exist only as hand-written C# and TypeScript literals, and in a comment at `workshop_wire.fbs:348-351`.

The schema is also restated by hand in several places:
- **Unknown-field refusal** uses C# vtable field counts and slot numbers. It covers 6 of the 79 tables (`engine/gpu/WorkshopWireFlatBuffers.cs:45-91`; `engine/gpu/WorkshopAuthoringFlatBuffers.cs:34-102`).
- **`SNAPSHOT_SCHEMA`** is a full hand copy of both files (`tools/e2e/run-reset-snapshot.ts:13-146`), with a hand decoder at `:182-293`. It is stale: the deferred `tools/e2e/unit/run-reset-snapshot.test.ts` fails 23 of its 24 cases (S5b review finding BH3-4).
- **The authored-state dump** lists a partial set of fields by hand (`tools/e2e/workshop-wire.ts:12812-12871`).
- **Field lists** are pinned by `CuriousContraptions.tests/WorkshopFormatTests.cs:97-123`.
- **Literal slot numbers** are used to build or read tables in about fifteen test files.

No required run executes the generated TypeScript (S5b's stated residual), so nothing proves that the C# and TypeScript bindings agree.

**Approach:**
- **One file.** The two files merge into one, `schemas/workshop_wire.fbs`, and `workshop_authoring.fbs` is deleted.
  - The file declares the four root identifiers as schema data: an enum whose values are each identifier's four bytes read as a little-endian `uint32`. Both bindings are generated from it.
  - It marks the authoring-only declarations with a declared attribute. That mark replaces the file boundary that kept authored records out of the runtime roots.
- **Unknown-field refusal** becomes one rule in the vendored C# FlatBuffers verifier. The rule is derived from the generated `Verify` code and covers every table. The hand counts are deleted.
- **Hand restatements.** Every restatement in the inventory below is deleted or derived from generated code or compiler output. Anything that cannot be derived before the programme gets a row with an owner.
- **Cross-language conformance.** A required test runs both directions, for every root, every command and response kind, and every observation payload arm:
  - a strict-TypeScript node script, compiled by the pinned `tsc` in the normal build, decodes and re-encodes buffers that the C# codecs wrote;
  - the C# production codecs then decode what TypeScript wrote.

  The test asserts decoded semantic fields and checks that both sides refuse malformed frames. It is part of the required full round.
- **Runtime pin.** A required test now type-checks and runs the generated TypeScript, so the exact `flatbuffers` 25.2.10 pin moves to the root `package.json` and lock. This reverses S5b's coordinator decision B (owner decision 24p, ruling (d)).

## Boundaries & Constraints

**Always:**
- **Exactly one schema file.**
  - `schemas/` holds exactly one `.fbs` file, `schemas/workshop_wire.fbs`. It declares every table, struct and enum, and every type the four roots reach. It has no `include`.
  - It has exactly one `root_type` and one `file_identifier`: `root_type SavedConstruction; file_identifier "CCWS";`. The save is the root that lives in files.
  - The C# generation step and the TypeScript generation step each pass exactly this file to the pinned `flatc`.
- **Root identifiers are schema data.**
  - The file declares `enum WireRootIdentifier : uint32` with one member per root. Each member is named exactly as its root table. Its value is the root's four identifier bytes read as a little-endian `uint32`:
    - `WorkshopCommand = 0x4D435743` ("CWCM")
    - `WorkshopResponse = 0x4D525743` ("CWRM")
    - `WorkshopObservationBatch = 0x4F574343` ("CCWO")
    - `SavedConstruction = 0x53574343` ("CCWS")

    The values are in ascending order, and each is commented with its ASCII text.
  - C# and TypeScript take an identifier only from generated code: the generated `WireRootIdentifier`, or the root API that `root_type SavedConstruction` emits (`SavedConstruction.FinishSizePrefixedSavedConstructionBuffer`, `SavedConstructionBufferHasIdentifier`). The main assembly's save encoder uses that finisher, and S3 proves that its identifier equals the member.
  - Each language has exactly one conversion function. It is total over `uint32`: any `WireRootIdentifier` value, whether a member or not, maps to its four little-endian bytes as a four-character identifier.
    - C#: internal, beside Core's frame boundary. It yields the `string` that the vendored `FinishSizePrefixed` and `VerifyBuffer` take (`CuriousContraptions.FlatBuffers/FlatBufferBuilder.cs:1016`, `FlatBufferVerify.cs:781`), so no call site converts a second time.
    - TypeScript: in `tools/runtime/wire-frame.ts`.
  - No C# or TypeScript source outside generated code holds a literal for any of the four current root identifiers, in code or in a comment. The only exceptions are the historical-fixture regions on S11's allow-list. Comments and messages that name one today are reworded to name the enum member: `CuriousContraptions.tests/SaveFixture.cs:7`, `engine/gpu/WorkshopSaveCodec.cs:13`, `tools/e2e/workshop-driver.ts:145` and `:935`, `tools/e2e/run-reset-snapshot.ts:321`, and `tools/e2e/workshop-wire.ts:12799`. S11 scans the tracked C#, TypeScript and MSBuild sources, excluding `_bmad-output/` and `docs/`, which hold historical evidence such as `_bmad-output/implementation-artifacts/evidence-9r-0c/`.
  - C# test attributes that need an identifier take the enum member, which is a constant. The mixed theory at `WorkshopSaveTests.cs:398-401` takes a `WireRootIdentifier` parameter: the member for the current root, and `(WireRootIdentifier)Foreign` for the foreign word `"XXXX"`, where `Foreign` is a named `uint` constant. Both go through the one conversion. `WorkshopSaveRejectionTests.cs:42`'s optional parameter defaults to `WireRootIdentifier.SavedConstruction` and is converted at its one use.
  - Deliberately foreign identifiers in negative tests stay as test data: `"CCWC"` and `"CCWR"` at `CuriousContraptions.tests/ClutchGeneralityTests.cs:443`, and the foreign word at `WorkshopSaveTests.cs:398`, now passed as `(WireRootIdentifier)Foreign`.
  - The pinned compiler 25.2.10 was run on a probe of this declaration in planning (`.tools/s5c-probe/`, ignored), and the planning audit recompiled it. It generated `public enum WireRootIdentifier : uint` and `export enum WireRootIdentifier` with these values.
- **Authoring declarations are marked.**
  - The file declares `attribute "authoring";` and marks exactly the nine declarations that `workshop_authoring.fbs` holds today: `CadenceSettingsRecord`, `ConnectionRecord`, `AssistanceKnotRecord`, `ParameterRecord`, `InstanceRecord`, `IntegrityGoalRecord`, `PuzzleRecord`, `ConstructionRecord` and `SavedConstruction` (`workshop_authoring.fbs:4`, `:13`, `:25`, `:39`, `:44`, `:57`, `:62`, `:79`, `:88`).
  - The pinned compiler accepts the attribute on tables and structs and emits nothing for it (planning probe; audit 1).
  - No runtime root may reach a marked declaration (row S9).
- **One derived rule refuses unknown fields, in the vendored verifier.**
  - `Verifier` records, for each table being verified, the highest vtable slot that the generated `Verify` for that table checked. `VerifyTableEnd` (`CuriousContraptions.FlatBuffers/FlatBufferVerify.cs:462-466`) then refuses the table if its vtable is longer than that slot. Tables nest: a child is verified inside its parent's field call, so the record is kept per table (a stack).
  - Every generated `Verify` checks every field of its table, and the last slot it checks is `4 + 2 * (n - 1)` for n fields (planning audit 1, measured over all 79 generated tables, 0 mismatches). The schema has no `union`, no `deprecated` field and no `id:` attribute. The rule therefore covers every table at every depth, and no count is written by hand.
  - A vtable that is one entry longer than its table, with that extra entry zero, is also refused. This is a behaviour change from today's save rule, which refused only a present extra field (`engine/gpu/WorkshopAuthoringFlatBuffers.cs:98-99`). It refuses no valid buffer: the C# and TypeScript builders trim trailing zero entries.
  - It is a recorded patch to the vendored runtime, like Story 9A.45's (`CuriousContraptions.tests/VendoredFlatBuffersIdentityTests.cs:5-12`). The `FlatBufferVerify.cs` pin (`:23`) and `THIRD-PARTY-NOTICES.md` are updated with it.
  - A refused buffer is `WireFaultReason.VerifierRejected` at the wire boundary and `WorkshopSaveRejection.VerifierRejected` on the save path (`engine/gpu/WorkshopSaveCodec.cs:59-69` maps it). A refused save changes nothing: the codec refuses before any domain construction (`WorkshopSaveCodec.cs:48-55`).
- **Every decode checks the frame first, on both sides.** The size prefix must cover the buffer exactly, and then the identifier must be the expected root's.
  - **C#** then runs the generated verifier, with the derived rule, and decodes once (`WorkshopWireFlatBuffers.OpenFrame`, `engine/gpu/WorkshopWireFlatBuffers.cs:25-56`).
  - **TypeScript** has exactly one frame check, in `tools/runtime/wire-frame.ts`. It is compiled by the runtime TypeScript chain and imported by both the conformance script and `tools/e2e/workshop-wire.ts`. It throws a plain `Error` whose message names the failed check: shorter than its size prefix, truncated, trailing bytes, or wrong identifier. The conformance test matches each message to the `WireFaultReason` that C# gives for the same buffer (owner decision 24p, ruling (a)). That mapping lives in the C# test, so TypeScript holds no mirror of `WireFaultReason`.
  - The pinned compiler writes no TypeScript verifier: 0 of the 137 generated TypeScript files mention one, and the `flatbuffers@25.2.10` package has none. TypeScript therefore decodes only what passes the frame check.
  - **Evidence limit:** until the first production TypeScript table decoder provides TypeScript verification without hand code (inventory row H-R1, owned by `ENGINE-WIRE-ABI`), a malformed table that passes the size and identifier checks is refused only on the C# side.
- **The conformance check is a required test, not a browser gate.** AGENTS.md says "the game system's tests are the C# solution's test projects and the node harnesses for game runtime code". A node harness over the generated runtime bindings is therefore a required system test. Only Chrome/Playwright suites and the tests of their toolchain are deferred.
  - The script is strict TypeScript (`strict: true`, no `any`). It is compiled by the pinned `tsc` 5.9.3 (root `package.json:10`) in the normal build and run with the pinned Node 26.12.1 against the compiled output. It imports `flatbuffers` from the root `node_modules`.
  - The test asserts decoded semantic fields with exact values. The wire carries values exactly, so exact equality is a defined-interface invariant. The test never compares bytes or hashes.
- **The generated TypeScript is compiled in every build graph, after generation.** One runtime TypeScript compile (`npm run build:runtime`, `tools/runtime/RuntimeTypeScript.targets:29-33`) compiles the generated module, `wire-frame.ts` and the conformance script.
  - That compile runs in the `CuriousContraptions.Abstractions` instance (`:23`, `:29-30`) in every configuration.
  - TypeScript generation therefore joins the same chain, before the compile, and reads the one schema file. It replaces S5b's Debug-only `GenerateWireTypeScript` in `CuriousContraptions.FlatBuffers` (decision D3; task 1 is its gate).
  - Generation stages its output in the building instance's own intermediate directory. It moves the per-type files into `tools/e2e/.wire/` first and the entry module last, so an interrupted run never leaves a partial set behind an entry module.
  - Two writers never overlap. Each local graph has one Abstractions instance. In CI's graph, the Release and Debug Abstractions instances are reached from different targets of the one web project (reference resolution, then `BuildWorkshopEditorMetadata`), so they run one after the other. Task 1(c) records that ordering.
  - Inputs and Outputs follow S5b's pattern. A separate target writes an intermediate list stamp with `WriteOnlyWhenDifferent` and registers it in `FileWrites`, so Clean deletes it and every `--no-incremental` capture build generates exactly once. The generation target's Inputs are:
    - that stamp;
    - the schema file;
    - the shared compiler targets file;
    - the file that declares the generation target (`$(MSBuildThisFileFullPath)`), so that a change to the generation command regenerates.

    Its only Output is the entry module, so an unchanged tree does not regenerate.
  - The shared compiler targets file is imported once. `tools/runtime/RuntimeTypeScript.targets` imports it, and every project, the vendored one included, already receives that file through `Directory.Build.targets:41`. The vendored project therefore does not import it again, which avoids the MSB4011 duplicate-import warning. `PinnedImports` records the one importer.
  - S5's compiler provisioning target serves both generation steps. The compiler cache is still needed only when generating (S5's offline rule).
- **The TypeScript runtime is pinned once, at the root.** `flatbuffers` is exactly `25.2.10` in the root `package.json` `devDependencies` and the root `package-lock.json`. `tools/e2e/package.json` and `tools/e2e/package-lock.json` (from S5b) are deleted. No tsconfig maps `flatbuffers` to a path.
- **The worker boundary holds after the merge.** No worker receives authored instances (`schemas/README.md:3`). Assembly ownership enforces this in three ways:
  - `WorkshopAuthoringFlatBuffers`, `WorkshopSaveCodec` and `WorkshopSavePrevalidation` compile only into the main assembly. `CuriousContraptions.csproj` compiles `engine/gpu/*.cs` minus `@(CoreRuntimeSource)` (`CuriousContraptions.Core/RuntimeSources.props`).
  - No runtime root reaches a declaration marked `authoring` (row S9).
  - No compile of `CuriousContraptions.Core`, `CuriousContraptions.Simulation` or `CuriousContraptions.Animation.Worker` references a marked declaration, or one of its generated companions, as a type (row S10), with one exception.
    - The exception is `SavedConstructionVerify` in Core's `ValidateSaveFrame` (`CuriousContraptions.Core/CoreCommandCodec.cs:9`). It verifies a save frame for the main assembly through `ICommandCodec` and yields no authored instance.
    - The save frame check stays behind the Abstractions interface because the main assembly reaches Core only through Abstractions interfaces. The coupling gate (`tools/anvil/VerifyPolicy.cs`, rule family E9) refuses a model dependency from the main assembly on Core (D7).
    - Abstractions gains no project reference.
- **Wire enums keep one source in the schema.** Owner decision 24p, ruling (f), makes the schema enum the one source for closed sets that cross the wire. S5c does not move enum ownership; `ENGINE-WIRE-ABI` does. Until then, an interim test (row S14) proves that each of the seven C# domain mirrors has the same member names and values as its generated enum.
- **Forward-only.** There is no format version (`schemas/README.md:17-18`), no old-schema path, no second schema file, no alias type or re-export, and no fallback decoder. The merge moves declarations without renaming or reordering any field.

**Never:**
- A second `.fbs` file anywhere that the build reads, an `include`, or a generated schema fragment.
- A field renamed, reordered or removed, or a change to the layout of any table or struct. The schema changes only in these ways:
  - the two files merge;
  - the `WireRootIdentifier` enum is added;
  - the `authoring` attribute and its marks are added;
  - `root_type WorkshopResponse` (`workshop_wire.fbs:1122`) is deleted, because no caller uses a root-only generated API (audit 1);
  - the identifier comment at `:348-351` is deleted, because the enum now carries its content.
- A hand-written TypeScript verifier, any other new generator, a TypeScript post-processor script, a hand mirror of `WireFaultReason` in TypeScript, or a second TypeScript frame check.
- Running or gating the browser toolchain (`tsx`, Playwright) in a required run, or the deferred `tools/e2e/unit/run-reset-snapshot.test.ts`. The `BrowserDeferral` list gains, loses and re-gates no entry. Only reason texts that name the `tools/e2e` manifest change.
- Hashes or byte comparisons of encoded output as acceptance; mutants; a local Release worker build or web publish.
- Moving the thread-boundary layouts here (pose ring, clock and event rings, schedule controls, animation control and output, the physics GPU ABI records). They stay with `ENGINE-WIRE-ABI`, which declares them in this same file (owner decision 24p).

## I/O & Edge-Case Matrix

| Row | Input / State | Expected Output / Behavior | Error Handling |
|-----|--------------|---------------------------|----------------|
| S1 | The candidate tree | `schemas/` holds exactly one `.fbs` file. It has no `include` and exactly one `root_type` and one `file_identifier`. Every root named by `WireRootIdentifier` is a table the file declares | The guard names each extra file or line |
| S2 | The binary logs of the round (`solution.binlog`, `playtest.binlog`, `web.binlog`) | Every `flatc` invocation, C# and TypeScript, names exactly the one schema file as input. TypeScript generation starts exactly once per graph, in the Abstractions instance | Names the log, the invocation and the extra input or count |
| S3 | The generated `WireRootIdentifier` in C#, and the same members read through the TypeScript script | Four members. Each name is a table that the production codecs decode as a root. The values are distinct, and each decodes to four printable ASCII bytes. C# and TypeScript give the same four bytes per member. The `SavedConstruction` member equals the schema's `file_identifier`. A save the codec wrote, which the generated finisher frames with the `file_identifier`, is admitted by `ICommandCodec.ValidateSaveFrame`, which checks against the converted `WireRootIdentifier.SavedConstruction`. That save, re-finished by the generated builder with another member's converted identifier, is refused with `UnknownIdentifier`. No frame bytes are read by the test | Names the member and the refusal |
| S4 | The production codecs encode one value of each root: every value of the encoder's command-kind enum (`Enum.GetValues<WorkshopCommandKind>()`, 14 today), every value of its response-kind enum (2), every non-None `ObservationPayload` arm (11), and one save | The script frame-checks each buffer with the identifier from the generated `WireRootIdentifier`, decodes it with the generated accessors and reports the unpacked object. Named fields equal the encoded domain values exactly: kind, session bytes, sequence, a nested table's identity, a vector's length, an f32 and a `uint64` | Any frame refusal or missing field fails, naming the root and the case |
| S5 | Each S4 buffer re-encoded by TypeScript with the generated object API (unpack, then pack with `finishSizePrefixed` and the generated identifier), once unchanged and once with one named field set to a new value | The C# production decoder admits it. The decoded domain value deep-equals the original, field by field, with collections compared by element. In the set case it differs exactly in that field | A C# refusal names its `WireFaultReason` |
| S6 | Malformed frames made from each root's valid buffer: shorter than the size prefix, wrong identifier (another root's), truncated by one byte, one trailing byte | C# refuses with `Truncated`, `UnknownIdentifier`, `Truncated` and `TrailingBytes` respectively; a save gets the matching `WorkshopSaveRejection`. TypeScript refuses the same buffer, and its message names the failed check the C# test maps to that reason | The test names the case if either side admits the buffer or the checks differ |
| S7 | A buffer whose root table offset points outside the buffer; a save whose required field `PuzzleRecord.assistance` is absent | C# refuses with `VerifierRejected`; a save gets `WorkshopSaveRejection.VerifierRejected`. TypeScript: no assertion (evidence limit, see Always) | C# names the reason |
| S8 | One field appended beyond the schema: at each of the four roots; in a nested vector table that no hand check covered before (`CompiledSceneRecord` rows); in a save's `InstanceRecord`. Boundary: one extra vtable entry that is zero | Each is refused with `VerifierRejected`; a save gets `WorkshopSaveRejection.VerifierRejected`. Control: a table with its last schema field set is admitted | Names the table |
| S9 | The schema text | No runtime root (`WorkshopCommand`, `WorkshopResponse`, `WorkshopObservationBatch`) reaches a declaration marked `authoring` through any field. `SavedConstruction` and `InstanceRecord` are marked. Evidence limit: opaque byte vectors (`motion`, `response`, `record`, `probe`, `peer`, `pending`; `workshop_wire.fbs:306`, `:357-361`, `:378-379`) are not visible to this walk (H-W3) | Names the root and the path to the marked declaration |
| S10 | The C# compiles of Core, Simulation and Animation.Worker, replayed from the binary logs | No type symbol that a compile references binds to a forbidden type. The forbidden set is: each declaration marked `authoring`; its generated companions (`<Name>Verify`, and any other type the pinned compiler emits for it, measured in task 1(a)); `WorkshopAuthoringFlatBuffers`, `WorkshopSaveCodec` and `WorkshopSavePrevalidation`. Symbols come from a Roslyn semantic model of the replayed compile, using the sources, references and defines on its `csc` command line, so aliases and `var` are resolved. The enum member `WireRootIdentifier.SavedConstruction` is not a type and does not trip it. Exactly one reference is admitted, `SavedConstructionVerify` in `CoreCommandCodec.ValidateSaveFrame` (D7). The admission is one source location (file and span), deduplicated across the replayed compiles; a second distinct location in any compile fails | Names the project, file and type |
| S11 | The tree, code and comments | No inventoried hand restatement remains. No `SNAPSHOT_SCHEMA`, no hand field-name or slot list, and no current root identifier literal (in code or in a comment), outside the allow-list. The allow-list is: generated code, the vendored runtime, `WireSchemaLayout`, and the marked historical-fixture regions named in H-T3 and H-T4 (regions, not whole files). No numeric literal for a slot or field count passed to `StartTable`, an `Add*` slot argument or `__offset`, and no vtable arithmetic, outside the same allow-list. Exactly one TypeScript frame check. No `TheVersionlessRecordsDeclareExactlyTheirFields` | The guard names file and line |
| S12 | The root and `tools/e2e` manifests and tsconfigs | The root `package.json` has `flatbuffers` exactly `25.2.10` in `devDependencies`, locked at `25.2.10`. `tools/e2e/package.json` and its lock are absent. No tsconfig maps `flatbuffers` | Names the file and value |
| S13 | A field added to the schema in a copied tree, then incremental generation | The C# accessor exists (S5's vendored-project fixture) and the TypeScript accessor exists (the generation target on an Abstractions copy, task 1(e)). Control: both absent before the edit | Build error names the file |
| S14 | The seven C# domain mirrors of wire enums (interim, until `ENGINE-WIRE-ABI`): `WorkshopCommandKind`, `WorkshopResponseKind`, `WorkshopSimulationPhase`, `WorkshopCommandOutcome`, `ElectricalEnable`, `RunControlLane`, `RunControlValueKind` | Each has the same member names and numeric values as its generated `CuriousContraptions.Wire` enum | Names the pair, member and values |

</intent-contract>

## Code Map

Baseline: captured at dispatch (coordinator decision 38): the published main that contains S5b. Scope guards compare against it. The contract was drafted on `51d46f7b`, and every `path:line` below was re-read there; planning audit 1 re-checked them.

S5b (lane 2, in flight; contract `spec-s5b-generate-typescript-wire-in-build.md`) moves several citations:
- It deletes `tools/e2e/workshop-wire.ts:1-12731`, so the helper lines cited here shift up by 12,720.
- It adds `GenerateWireTypeScript` to the vendored project.
- It adds `tools/e2e/package.json`, `tools/e2e/package-lock.json`, `WorkshopWireTypeScriptTests.cs` and the `Toolchain.Resolve` runtime check.
- It edits some of the e2e suites named below.

Re-read every citation at dispatch.

**Status:** draft. Runs in lane 2 after S5b and before S6 (owner decision 24p, 25 November 2026). Prerequisite: S5b published.

**Planning verdict:** Scoped Pass, 25 November 2026, on the bounded fifth check that the coordinator authorised (`audit-s5c-5.md`). Audits 1 to 4 failed, and their findings are repaired (`docs/planning/elements/readiness/audits/audit-s5c-1.md` to `audit-s5c-4.md`). The fifth check's three low findings are repaired and await re-check at the dispatch re-audit. That re-audit is mandatory before the build and runs against the main that contains S5b.

**Review tier:** engine (wire codecs, a vendored runtime patch, build configuration, TypeScript, tests).

**Delivery shape:** one structural slice. If the planning step measures it as oversized, it may be split at dispatch along task boundaries: schema and C# refusal; conformance harness and pin; hand-restatement removal in tests and tools. Each part keeps the rows it owns.

Owned scope:
- **`schemas/workshop_wire.fbs`.**
  - Gains the nine authoring declarations from `workshop_authoring.fbs:4-92`, placed after the wire declarations and unchanged apart from their `(authoring)` mark.
  - Gains `attribute "authoring";` and `enum WireRootIdentifier : uint32`.
  - Loses `root_type WorkshopResponse` (`:1122`) and the identifier comment (`:348-351`).
  - Ends with `root_type SavedConstruction; file_identifier "CCWS";`.

  `schemas/workshop_authoring.fbs` is deleted.
- **`CuriousContraptions.FlatBuffers/CuriousContraptions.FlatBuffers.csproj`.**
  - `WireSchema` (`:43`) becomes the one file, and `GenerateWireAccessors` (`:127-133`) passes it.
  - S5b's `GenerateWireTypeScript` is deleted from here, with its include-ordering logic and its two `Error` branches.
  - Provisioning moves to one shared targets file outside the vendored directory: `ProvisionFlatBuffersCompiler` (`:57`), `DeleteFlatcTemporary` (`:114`) and the compiler properties they read (`:23-41`: `FlatcVersion`, `FlatcCacheRoot`, `FlatcAsset`, the two digests, `FlatcCacheDirectory`, `FlatcPath`). `GenerateWireAccessors`' Inputs (`:128`) gain the shared file, so a compiler pin change still regenerates C#. The pin-mismatch build cases edit the digest in the shared file, on both fixtures. Only `tools/runtime/RuntimeTypeScript.targets` imports it. Every project receives that file through `Directory.Build.targets:41`, this one included, so both generation steps see one provisioning target.
  - The static gate's build-logic pins are updated in `CuriousContraptions.tests/CSharpStaticGateTests.cs`:
    - the vendored project file's pin (`:116`);
    - `PinnedTargets` (`:1335-1364`), for the changed `BuildRuntimeTypeScript`, the moved generation target and stamp target, and the shared provisioning targets file with its `DownloadFile` and `Exec`;
    - `PinnedImports` (`:1368-1377`), for the shared file's one importer, `tools/runtime/RuntimeTypeScript.targets`. The vendored project adds no import.
- **`CuriousContraptions.FlatBuffers/FlatBufferVerify.cs`** (`VerifyTableStart` `:445-458`, the per-field `Verify*` methods, `VerifyTableEnd` `:462-466`) gets the derived unknown-field rule. `CuriousContraptions.tests/VendoredFlatBuffersIdentityTests.cs` (`:5-12` summary, `:23` pin) and `THIRD-PARTY-NOTICES.md` (the bounded patch) are updated with it.
- **`tools/runtime/RuntimeTypeScript.targets`** (`:23-40`). TypeScript generation joins the Abstractions-guarded chain before `BuildRuntimeTypeScript`, with the staging, Inputs and Outputs stated under Always. It runs `--ts --gen-object-api` on the one file, writes into `tools/e2e/.wire/`, and applies S5b's annotation normalisation. The generated entry module joins `BuildRuntimeTypeScript`'s Inputs.
- **`tools/runtime/wire-frame.ts`** (new). The one TypeScript frame check and the one `WireRootIdentifier`-to-bytes conversion. It imports the generated module.
- **`tools/runtime/wire-conformance.ts`** (new). A script that reads JSON on stdin and writes JSON on stdout, like `tools/runtime/event-roundtrip.ts`, which is driven from `CuriousContraptions.tests/EventRingConsumerTests.cs:761-773`.
  - It imports the generated entry module, `wire-frame.ts` and `flatbuffers`.
  - It looks up a root class by its `WireRootIdentifier` member name, frame-checks the buffer, then does one of: decode and unpack; or unpack, set one named field if asked, pack and finish size-prefixed.
  - It reports every JavaScript `number` as its IEEE-754 binary64 bit pattern in hex. That is exact for every f32, integer and double, and it keeps -0. It reports every `bigint` as a decimal string. The C# side widens its expected values to binary64 to compare.
  - It holds no field, type or slot name of its own.
- **`CuriousContraptions.tests/WireConformanceTests.cs`** (new): rows S3 to S7, driving the script with node.
- **`CuriousContraptions.tests/WireDecodeTests.cs`** (new): row S8.
- **`CuriousContraptions.tests/WireSchemaGuards.cs`** (new): rows S1, S2, S9, S10, S11 and S12, named as architecture guards. S2 and S10 reuse `CSharpStaticGateTests.ReplayCscRuns`. S12 is S5b's T7, inverted and moved here.
- **`CuriousContraptions.tests/WireEnumMirrorTests.cs`** (new, interim): row S14. `ENGINE-WIRE-ABI` deletes it together with the mirrors.
- **`CuriousContraptions.tests/WireSchemaText.cs`** (new): the one test helper that reads the schema file. It provides declarations, fields in order, marks and references. `CSharpStaticGateTests.SchemaTypesIn` (`:506-512`), `WireSchemaLayout`, S9's reachability walk and `BundleElementIdTests.NoSchemaFieldCarriesANumericPartId` (`:209-215`) all use it.
  - `WireSchemaLayout` returns a field's slot from its position in the declaration. It refuses a table that carries a `deprecated` or `id:` attribute; none exists today (audit 1 measured this).
  - Its control, `WireSchemaTextTests.EveryTableFieldCountEqualsItsGeneratedAddMethods` (in `CuriousContraptions.tests/WireSchemaTextTests.cs`, new): for every table, the field count equals the number of generated `Add*` methods on that table's C# type.
- **`tools/E2eRunner/SourceIdentity.cs`** (S5b's `:36-38`): the e2e source identity also digests `tools/runtime/wire-frame.ts`, because the suites now import it. This is deferred-runner scope; nothing runs it in a required run.
- **C# codecs.**
  - `engine/gpu/WorkshopWireFlatBuffers.cs`:
    - `:18-19`: the identifier constants are deleted, and callers take the `WireRootIdentifier` member.
    - `:45-53` (`fieldCount` and the `ValidateBindingShapes` call) and `:58-91` (`ValidateBindingShapes`, `TableTarget`) are deleted.
    - `:104-109`: `IsIdentifier` compares against the converted member.
  - `engine/gpu/WorkshopWireFlatBuffers.Observation.cs:11` (`ObservationIdentifier`): handled the same way.
  - `engine/gpu/WorkshopAuthoringFlatBuffers.cs`: `:34-74` (`OpenFrame`, `CurrentPuzzleTable`, `Reference`) and `:76-102` (`InstanceRecordFields`, `SavedConstructionSlot`, `ConstructionInstancesSlot`, `HoldsFieldBeyondTheInstanceTable`) are deleted. The verifier rule and the schema's `(required)` on `assistance` cover them.
  - `engine/gpu/WorkshopSaveCodec.cs`:
    - `:17` (`Identifier`) is deleted.
    - Its production uses at `:29` and `:61` become the generated root finisher and `codec.ValidateSaveFrame` (see the `ICommandCodec` bullet below).
    - `:52-54` is deleted.
  - `CuriousContraptions.Abstractions/RuntimeCodecs.cs:15-16`, `:19` and `CuriousContraptions.Core/CoreCommandCodec.cs:15-16`, `:19`: the three identifier properties are deleted from `ICommandCodec` and its one implementer; no consumer reads them through the interface (audits 2 and 3, `rg`). `ValidateSaveFrame(ReadOnlySpan<byte>)` (`RuntimeCodecs.cs:9`) stays on the interface. Core's implementation (`CoreCommandCodec.cs:9`) passes `WireRootIdentifier.SavedConstruction` and `SavedConstructionVerify.Verify` to its one internal frame boundary (D7).
  - `engine/gpu/WorkshopSaveCodec.cs`, in the main assembly:
    - `Open` (`:59-61`) calls `codec.ValidateSaveFrame(bytes)` and then reads the root from the size-stripped buffer, which is the work of the deleted `WorkshopAuthoringFlatBuffers.OpenFrame`, moved here.
    - `Encode` (`:29`) finishes with the generated `SavedConstruction.FinishSizePrefixedSavedConstructionBuffer`.
    - Its `codec` parameters (`:35`, `:48`, `:59`) stay used. Abstractions gains no project reference.
    - Its summary at `:44-47`, which describes the deleted instance-field refusal, is reworded. This bullet and the `WorkshopSaveCodec.cs` bullet above describe one file and are applied together.
  - Stale comments: `DeclaredOrientationTests.cs:154-156` describes the deleted walk and is reworded with H-T4.
- **Test callers of what is deleted.**
  - `WorkshopAuthoringFlatBuffers.OpenFrame`: `F32SaveBoundaryTests.cs:12` (with its `"CCWS"` literal; the message at `:48` is derived from the member) and `GoalSignalSelectorTests.cs:81`.
  - `WorkshopSaveCodec.Identifier`: `WorkshopSaveRejectionTests.cs:42`; `CompoundMassAndRollingTests.cs:896`, `:900`; `WorkshopWireFlatBuffersTests.cs:43`, `:46`; `WorkshopSaveTests.cs:399-401` (attribute arguments, which take the enum member); `WorkshopFormatTests.cs:137`; `SaveFixture.cs:22`.
  - `WorkshopWireFlatBuffers.CommandIdentifier`, `ResponseIdentifier` and `ObservationIdentifier`: `WireFaultTests.cs`, `WorkshopObservationTests.cs`, `WorkshopIntegrationHarnessTests.cs`, `TypedTransportCommandTests.cs`, `CompiledSceneBoundaryTests.cs`, `RunControlTableTests.cs` and `WorkshopWireTests.cs`. The complete list is re-measured at dispatch with `rg -n "CommandIdentifier|ResponseIdentifier|ObservationIdentifier|WorkshopSaveCodec.Identifier"`.
- **TypeScript tools.** These are deferred browser support. `tools/E2eRunner/TypeCheck.cs` type-checks them only in the browser phase.
  - `tools/e2e/workshop-wire.ts`:
    - `:12734-12736` (the identifier constants) and `:12740-12749` (`openFrame`) are deleted. Its decoders import `wire-frame.ts`.
    - `:12812-12871` (`dumpAuthored`, `authoredState`) is derived from the generated object API and flattens every field. This closes the gap the inventory found.
    - `:12874-13101` (the mock encoders) builds through object-API classes with named properties, not positional struct creators.
  - `tools/e2e/run-reset-snapshot.ts`:
    - `:5-6`, `:13-146` (`SNAPSHOT_SCHEMA`), `:156-158`, `:167` and `:182-293` (the hand layout and decoder) are deleted. The snapshot flattens the generated object API's unpacked read and save into `path -> value`.
    - Values are compared with `Object.is`, so -0 still differs from 0 and NaN equals NaN. The raw-bit NaN-payload distinction is dropped, because Run/Reset restoration is judged on decoded values (AGENTS.md game fidelity) and no gameplay outcome depends on a NaN payload.
    - `EXCLUDED_PATHS` (`:150-153`) keeps its intent, in the derived path spelling. `:371` (`MemoryKind`) is row H-W6.
  - `tools/e2e/unit/run-reset-snapshot.test.ts:17-18`, `:83`, `:106-184`: the schema-listing checks are deleted, because nothing is left to compare. Its other cases follow the derived paths. It stays deferred and is not run.
  - The suites' snapshot path strings follow the derived path spelling. They stay unexecuted until the browser phase. Files and lines: `capacity-9r5b.test.ts:109-111`, `cat-020.test.ts:104-106`, `:302`, `cat-050-joined.test.ts:187-293`, `cat-051.test.ts:74`, `cat-051-own-supply.test.ts:250-252`, `cat-052-lamp.test.ts:108-110`, `pkg-3.test.ts:108-110`, `tube-mouths.ts:38-68`, `tube-snapping.test.ts:117`, `:178`.
- **Runtime pin.**
  - The root `package.json` and `package-lock.json` gain `flatbuffers` at exactly `25.2.10`, written by npm with the pinned Node.
  - `tools/e2e/package.json` and `tools/e2e/package-lock.json` are deleted.
  - `tools/E2eRunner/Toolchain.cs`: S5b's runtime check resolves the root `node_modules/flatbuffers`.
  - `tools/E2eRunner/BrowserDeferral.cs`: the reason texts at S5b's `:18` and `:43-45` name the root manifest. `tools/e2e/README.md` changes S5b's two phrases to match.
  - `tools/E2eRunner.Tests/TypeCheckBoundaryTests.cs`: S5b's `TheE2eFlatBuffersRuntimeMustBeThePinnedVersionInstalledInToolsE2e` becomes the root-installed check.
- **Tests that restate the schema** (inventory rows H-T1 to H-T4):
  - `CuriousContraptions.tests/WorkshopFormatTests.cs`:
    - `:30-31` reads the one file.
    - `:96-123` (`TheVersionlessRecordsDeclareExactlyTheirFields`) is deleted. Its intent that "a re-added version field fails" is covered by `NoWireOrSaveRecordDeclaresAFormatVersion` (`:20-38`). Its intent that "a re-added field of any name fails" is withdrawn: under 24p that is a hand restatement of the schema. Slot drift between the two languages is covered by S4, S5 and S14.
    - `:160-181` is a historical-fixture case (H-T3).
  - `BundleElementIdTests.cs:209`, `HalfPrecisionGuardTests.cs:53` and `TypedTransportCommandTests.cs:397` read the one file.
  - S5's `WorkshopWireBuildTests.cs:61` and `:77` edit and touch `schemas/workshop_authoring.fbs`.
    - They are retargeted to `schemas/workshop_wire.fbs`, together with any other S5 or S5b case that names the deleted file after S5b, found at dispatch by `rg -n workshop_authoring`.
    - `:61`'s edit anchor, and that of every other case anchored on `table SavedConstruction {` (S5b's `:82` among them, merged into S13), becomes the marked text `table SavedConstruction (authoring) {`.
  - The shared compiler targets file is `tools/runtime/FlatcCompiler.targets`. The vendored-project fixture's `SharedBuildFiles` (`WorkshopWireBuildTests.cs:31-32`) gains it, so each copied-tree build still finds its import. `schemas/README.md:41` says the digests are pinned in that file. The vendored project's comments that describe provisioning in it (`CuriousContraptions.FlatBuffers.csproj:5-8`, `:55-56`, `:124-126`) are reworded.
  - `OldLayoutSaves.cs:45` uses `WorkshopWireFlatBuffers.CommandIdentifier` inside its historical region. It takes the converted `WireRootIdentifier.WorkshopCommand`.
  - `WorkshopWireBuildTests.cs`, S5's and S5b's cases:
    - **Deleted:**
      - `ASchemaSetWithoutOneRootReachingEverySchemaRefusesTheBuild` and `ADeletedSchemaFileLeavesNoGeneratedSource`: S1 covers them.
      - `AReleaseBuildWritesNoTypeScript`: its behaviour is removed, because Release graphs now generate. S2 reads only the local Debug logs, so it is not cited as cover.
      - S5b's T8 `TheTypeScriptStepRunsExactlyOncePerBuildGraph`: its target moves, and S2 covers the count.
    - **Merged into S13:** `AnAddedSchemaFieldIsATypeScriptAccessorAfterAnIncrementalBuild`.
    - **Moved to the Abstractions-copy fixture (task 1(e)):**
      - `AnUnchangedSchemaDoesNotRegenerateTypeScript`, with its steps rewritten for the staging folder in the intermediate directory.
      - `ARemovedSchemaTypeLeavesNoTypeScript`.
      - `ACheckoutPathWithASpaceBuildsACompleteModuleSet`: the generation command's quoting still matters; the include ordering it guarded is deleted.
    - **Assertions removed:** the "refused build wrote no TypeScript" assertions in `APinThatTheCachedCompilerDoesNotMatchRefusesTheBuild` and the span-define cases. The vendored project no longer writes TypeScript, so they would be vacuous. The pin-mismatch case is repeated once on the Abstractions-copy fixture, asserting that no TypeScript is written.
  - S5b's T1 (`TheGeneratedModuleExportsEverySchemaTypeWithoutAny`) is amended: with `--gen-object-api`, each per-type file also declares `<Name>T`.
  - The test files with literal slots (H-T3) take their slots from `WireSchemaLayout`.
- **`schemas/README.md`** (`:3-9`, `:48-53`, `:55-78`) describes the one file, the identifier enum, the authoring mark, the derived refusal, the generation chain and the conformance test. The manual "also update" reminder (`:77-78`) is deleted.
- **Planning records:**
  - `docs/planning/invest/delivery-batches.md` (S5c);
  - `_bmad-output/implementation-artifacts/deferred-work.md` (closes S5b's carried BH2 and BH3-4 items);
  - `TODO.md`;
  - `sprint-status.yaml` (key `s5c-one-wire-schema-file`);
  - this contract and its run log.

Checked and unchanged:
- `engine/gpu/WorkshopWireFlatBuffers.Observation.cs` arm switches (`:79-160`, `:199-215`) map domain values to generated types (coordinator decision 38). They are not restatements.
- `engine/gpu/WorkshopAuthoringFlatBuffers.cs:230-231` (`KnotsPerProfile`, `AssistanceKnotCount`) is a domain rule owned by the codec. The schema comment at `workshop_authoring.fbs:23-24` describes it and moves with the merge. It is not a layout restatement.
- `tools/WireContract/` and `tools/LifecycleContract/` are separate JSON design fixtures that never reference the game (their READMEs).
- `BundleElementIdTests.cs:213` names the two fields that carry element identities. That is a test assertion, not a layout.

### Hand-restatement inventory (owner decision 24p)

Measured at `51d46f7b` with `rg` over C#, TypeScript, MSBuild, tools and tests, excluding `_bmad-output/` and `docs/`; planning audit 1 extended it. S5c deletes or derives each row unless the row names another owner.

| Row | Where | What it restates | S5c action |
|---|---|---|---|
| H-C1 | `engine/gpu/WorkshopWireFlatBuffers.cs:45-53`, `:58-91` | Command and response field counts (12, 41), scene ordinals (11, 27), presentation field 29, track and binding counts (17, 20), field 5 | Deleted; verifier rule |
| H-C2 | `engine/gpu/WorkshopAuthoringFlatBuffers.cs:34-102` | Puzzle vtable bounds (10, 14), saved-construction and instances slots, `InstanceRecordFields = 7` | Deleted; verifier rule plus `(required)` |
| H-C3 | `engine/gpu/WorkshopWireFlatBuffers.cs:18-19`, `WorkshopWireFlatBuffers.Observation.cs:11`, `WorkshopSaveCodec.cs:17`, `CuriousContraptions.Core/CoreCommandCodec.cs:9`; tests: `F32SaveBoundaryTests.cs:12` | Root identifiers `CWCM`, `CWRM`, `CCWO`, `CCWS` | Derived from `WireRootIdentifier` |
| H-C4 | `engine/gpu/WorkshopSaveCodec.cs:52-54` | The instance-record field bound, through H-C2 | Deleted |
| H-T1 | `CuriousContraptions.tests/WorkshopFormatTests.cs:96-123` | Field lists of eight records and the settings creator's arity | Deleted; coverage recorded above |
| H-T2 | `WorkshopSaveRejectionTests.cs:17-20` (`enum Field`, the `InstanceRecord` fields in schema order) | Field order | Derived through `WireSchemaLayout` |
| H-T3 | Literal slots or vtable sizes in: `BodyOwnershipTests.cs`, `ClutchGeneralityTests.cs:378-462`, `CompiledSceneBoundaryTests.cs:89-761`, `CompoundMassAndRollingTests.cs:880-896`, `DrivenSurfaceBindingTests.cs:512-552`, `FanAdmissionTests.cs`, `IndependentSignalCompositionTests.cs:268-312`, `OwnedFiniteWorkTransactionTests.cs:196-198`, `PayloadInstanceTests.cs:270-358`, `RetentionBoundaryTests.cs:117-122`, `:372-397`, `RopeTests.cs`, `RunControlTableTests.cs:102-104`, `TrampolineTests.cs:571-574`, `WindmillAdmissionTests.cs:281-289`, `WorkshopWireTests.cs:918-922`, `MechanicalCoordinateBindingsTests.cs:1011-1044`, `MechanicalLinkMappingTests.cs:144-150`. Historical-fixture regions: `OldLayoutSaves.cs:11-80`, `WorkshopFormatTests.cs:160-181` (the retired versioned save), `DeclaredOscillatorTests.cs:174-177`, `DeclaredOrientationTests.cs:148-151` | Field ordinals, vtable sizes and struct byte offsets | Slots are derived through `WireSchemaLayout`. Struct byte offsets are rewritten through generated builders or accessors. The historical-fixture regions stay as fixed fixtures of refused input. Their slots are the fixture's own data, commented as historical. Each region, not its whole file, is on S11's allow-list as a marked region (`// historical layout: begin` and `// historical layout: end`) |
| H-T4 | `DeclaredOrientationTests.cs:137-138`, `:148-151`, `:157-181` | An `InstanceRecord` slot comment (`:137-138`); a historical layout (`:148-151`); and the test `CurrentRecordsHoldNothingBeyondTheTableAndTheWalkReachesTheGeneratedInstances` (`:157-181`). Its `:168-180` calls the deleted `HoldsFieldBeyondTheInstanceTable` and walks vtables with the deleted slot constants | Comment deleted. Historical layout kept, with only `:148-151` on the allow-list. In the test, the walk (`:168-180`) is deleted, covered by S8's control (current records are admitted) and by S4 and S5 (the generated accessors reach every instance). The round-trip and owner assertions (`:157-167`) stay, renamed `CurrentRecordsRoundTripThroughTheSaveAndTheCommand`. The run log records this |
| H-S1 | `tools/e2e/run-reset-snapshot.ts:13-146`, `:156-167`, `:182-293` | Both schemas in full, scalar sizes, struct layout and a hand decoder | Deleted; derived from the object API |
| H-S2 | `tools/e2e/unit/run-reset-snapshot.test.ts:17-18`, `:83`, `:106-184` | A schema-text parser, listing checks and `'CCWS'` | Deleted; nothing is left to compare |
| H-S3 | `tools/e2e/workshop-wire.ts:12734-12736`, `:12740-12749` | Identifier literals and a second frame check | Deleted; `wire-frame.ts` replaces them |
| H-S4 | `tools/e2e/workshop-wire.ts:12812-12871` | Partial field lists of `InstanceRecord`, `ConnectionRecord` and `PuzzleRecord` | Derived from the object API, covering every field |
| H-S5 | `tools/e2e/workshop-wire.ts:12874-13101` | Positional argument order of struct creators, for example `createBodyRead` with 22 arguments | Object API with named properties |
| H-S6 | The suites' snapshot path strings (owned scope) | Field names as path text | Kept as test assertions, respelled to the derived paths |
| H-R1 | There is no TypeScript FlatBuffers verifier (planning measurement) | Nothing; this is a missing capability | **Owner `ENGINE-WIRE-ABI`.** The first slice that adds a production TypeScript table decoder must provide TypeScript verification without hand code. The candidate to evaluate there is a generic verifier driven by the compiler's binary schema (`flatc --binary --schema`), with accessors generated from upstream `reflection.fbs` vendored under an integrity pin (owner decision 24p, ruling (a)) |
| H-W4 | `CuriousContraptions.Abstractions/WorkshopMessageValues.cs:11-13`, `RuntimeServices.cs:10-11`, `ElectricalSourceValues.cs:36`, `ParameterDescriptor.cs:50`, `RunControlValues.cs:11`, and the numeric casts in `engine/gpu/WorkshopWireFlatBuffers.cs:137`, `:147`, `:166`, `:530-533`, `:586-589`, `:761` | Seven C# enums that mirror schema enums by value | **Owner `ENGINE-WIRE-ABI`.** Under owner decision 24p ruling (f), the schema enum is the one source, and the C# domain uses the generated enum (C36 amended). S5c adds the interim S14 test |
| H-W6 | `CuriousContraptions.web/wwwroot/workshop-observation.ts:21`, `:39-40`, `:47` (wire enums); the rest of `:3-49` and `tools/e2e/run-reset-snapshot.ts:371` (`MemoryKind`) (C#-owned enums) | TypeScript enum mirrors | The wire enums go to **`ENGINE-WIRE-ABI`**, imported from the generated module. The non-wire enums go to **`ENGINE-CAPABILITY-INSTANCES`**, through its C#-to-TypeScript generator |
| H-W7 | `engine/gpu/WorkshopCadence.cs:61-93` and every non-FlatBuffers boundary layout | Hand layouts | **Owner `ENGINE-WIRE-ABI`**, declared in this one file (owner decision 24p) |
| H-W3 | `schemas/workshop_wire.fbs:305-306` (`motion: [uint8]`); `engine/gpu/PhysicsMotionRead.cs:47-52` | A hand layout tunnelled through the schema | **Owner `ENGINE-WIRE-ABI`** |

### Capability probe

No solver law, worker message content or shared presentation contract changes. The wire ABI changes only by declaration (the identifier enum and the authoring mark); no encoded field moves.
- **Present:**
  - the pinned compiler and C# generation (S5);
  - TypeScript generation (S5b);
  - the C# verifier and frame boundary (`WorkshopWireFlatBuffers.cs:25-56`);
  - node harness execution (`tools/E2eRunner.Tests/NodeHarnessTests.cs:12-29`; `EventRingConsumerTests.cs:761-773`).
- **Missing, added here:**
  - the one-file schema;
  - root identifiers as schema data;
  - a derived unknown-field rule for every table;
  - one TypeScript frame check;
  - a required test that runs the generated TypeScript;
  - schema-level enforcement of the worker boundary.
- **Missing, owned elsewhere:** a TypeScript verifier (H-R1).

### Decisions

- **D1, file name (decided).** The merged file keeps the name `schemas/workshop_wire.fbs`, since "wire" is the ABI. Most tests and the README already name it, so this means the fewest path changes. The authoring declarations follow the wire declarations, and they use only wire structs that are declared earlier (audit 1).
- **D2, identifiers (owner decision 24p, ruling (b)).** `WireRootIdentifier : uint32`. Each member name is the root table name, and each value is the four identifier bytes read little-endian. Rejected alternatives:
  - C# constants that TypeScript restates: that is a hand mirror.
  - Two `file_identifier` lines: the compiler allows one per file.
  - A user attribute that carries the text: generated code ignores attributes, so both sides would have to parse the schema text.

  The one remaining `file_identifier` must equal its enum member, which S3 checks.
- **D3, generation chain (decided; task 1 is the hard gate).**
  - **Why generation moves.** The runtime TypeScript compile runs in the Abstractions instance in every configuration (`tools/runtime/RuntimeTypeScript.targets:22-33`). `CuriousContraptions.FlatBuffers` and Abstractions have no build-order relation, and S5b generates TypeScript only in the Debug FlatBuffers instance. Left as it is, the generated module would be missing or stale for that compile, including in CI's Release graph. TypeScript generation therefore moves into the Abstractions chain, ahead of the compile.
  - **One writer per graph.** Locally, the one-writer rule is the chain's existing project-name guard. CI's `dotnet publish` (`.github/workflows/pages.yml:43`) may build Abstractions twice: once in Release as the web host's reference, and once in Debug through `BuildWorkshopEditorMetadata` (`CuriousContraptions.web/CuriousContraptions.web.csproj:47-52`). The runtime compile already runs in each such instance today. The two instances are reached from different targets of the web project, so they run one after the other. Each regenerates the same files from the same compiler and schema, so the later write is identical.
  - **Evidence limit.** Task 1(c) derives CI's instance count from the project references and the web host's MSBuild calls, as S5b's task 2 did, and records that the CI graph is not observed locally.
  - **What is deleted.** S5b's Debug-only guard and its rationale.
  - **Rejected alternatives.**
    - Keeping generation in FlatBuffers with a separate `tsconfig` compiled there. That needs the root `node_modules` before Abstractions' `npm ci`, plus an order edge between the projects.
    - A build-order reference from Abstractions to FlatBuffers. CI builds several FlatBuffers instances.
  - **Output location.** Unchanged at `tools/e2e/.wire/` (S5b D1), so the 27 importers stay the same. `ENGINE-WIRE-ABI` moves it when production scripts import it.
- **D4, object API for TypeScript only (decided).** `--gen-object-api` is passed only to the TypeScript step. The planning probe and audit 1 confirmed `InstanceRecordT` with `unpack()` and `pack(builder)` at 25.2.10, compiling under strict `tsc` 5.9.3. It gives the snapshot, the authored dump, the mocks and the conformance round trip a surface that covers every field with no hand-written names. The C# compile surface is unchanged, and so is the C# static gate.
- **D5, refusal reason (decided).** An unknown field is a verifier refusal.
  - Today's wire refusals become `VerifierRejected`. They were `OutOfRange`, "contains undefined fields" (`WorkshopWireFlatBuffers.cs:51`, `:73`, `:78`).
  - The save path's refusal also becomes `VerifierRejected`. It was `OutOfRange` (`WorkshopSaveCodec.cs:53`).
  - A zero extra vtable entry is now refused (Always).
  - These tests are measured at dispatch:
    - tests that expect `OutOfRange` for an undefined field, among them `DeclaredOrientationTests.ACurrentSaveRefusesTheFirstUnknownInstanceFieldAndItsValidControlLoads` (`:140`);
    - tests whose committed fixtures may change their expected reason (`CuriousContraptions.tests/Fixtures/pre-launch/`, `pre-source-store/`, `save-f1ad3bd1-authored-puzzle.bin`);
    - `CompoundMassAndRollingTests.cs:898-900`, which asserts that "the vendored verifier deliberately permits unknown fields". That assertion inverts: the verifier itself now refuses the buffer, and the test keeps its codec refusal.
  - The dispatch measurement also runs `rg -n "HoldsFieldBeyondTheInstanceTable|SavedConstructionSlot|ConstructionInstancesSlot|InstanceRecordFields|ValidateBindingShapes|TableTarget|ValidateSaveFrame"` and disposes every hit.
- **D6, pin at the root (owner decision 24p, ruling (d); reverses coordinator decision B).** Decision B put the pin in `tools/e2e` because no build graph compiled or ran the generated TypeScript, so a root pin would have been test-only provisioning. S5c's conformance script and `wire-frame.ts` are compiled by the runtime TypeScript compile in every graph, as the existing `tools/runtime` harnesses already are in CI. A required test runs that script. The runtime therefore belongs in the root manifest, with one pin and one source.
- **D7, the save frame check stays behind `ICommandCodec` (decided; audits 2 and 3, A2-1 and A3-1).**
  - **Why it stays.** The main assembly reaches Core only through Abstractions interfaces, and loads Core's provider by reflection (`engine/RuntimeProviders.cs:13-19`). A direct call from the main assembly into a public Core member is a model dependency between two volatile engine projects. The coupling gate (`tools/anvil/VerifyPolicy.cs`, rule family E9) refuses that dependency, so AC4 would fail.
  - **What stays.** `ICommandCodec.ValidateSaveFrame(ReadOnlySpan<byte>)` stays; it names no FlatBuffers type. Core's implementation passes `WireRootIdentifier.SavedConstruction` and `SavedConstructionVerify.Verify` to the one internal frame boundary.
  - **What is deleted.** Only the three unused identifier properties leave the interface. Abstractions needs no reference to `CuriousContraptions.FlatBuffers`, which keeps D3's premise and leaves that reference to `ENGINE-WIRE-ABI`'s planning under ruling (f).
  - **The main assembly's side.** It encodes the save with the generated root finisher, and decodes after `ValidateSaveFrame` with the generated `GetRootAsSavedConstruction`. It holds no frame check of its own and no identifier literal.
  - **The single S10 exception.** S10 admits exactly one companion reference in a worker-included compile: `SavedConstructionVerify` in `CoreCommandCodec.ValidateSaveFrame`. It is verification code that returns nothing and yields no authored instance. Any other companion or accessor of a marked declaration in Core, Simulation or Animation.Worker is refused, and so is a second site naming `SavedConstructionVerify`.
  - **Rejected alternatives.**
    - Changing the coupling gate's declared kinds: an E9 Fail.
    - A second frame check in the main assembly: forward-only, because it is a second rule for the same behaviour.
    - Keeping a `codec` parameter that is no longer used.
- **D8, the S5 comparison (decided).** "Deep-equals" in row S5 is xUnit `Assert.Equivalent(expected, actual, strict: true)`, which compares collections by element and handles the scene graph's by-ref-like properties (xUnit 3.2.2, audit 2). Record `Equals` is not used, because `WorkshopCompiledScene` is a class without `Equals`, so a command or response that carries a scene would compare by reference. Scene-free commands, responses and saves may keep `Assert.Equal`, as existing tests do. No NaN case is used: save prevalidation refuses non-finite values (`engine/gpu/WorkshopSavePrevalidation.cs:104`, `:169`, `:189-192`), and NaN bit patterns are not portable through JavaScript.

### Protections this change removes, and what replaces them

- **The include direction**, which kept authoring tables out of the runtime roots, is replaced by S9 and S10.
- **The hand field counts on 6 tables** are replaced by the verifier rule on every table (S8).
- **`SNAPSHOT_SCHEMA`'s listing test**, deferred and stale, is replaced by derivation, which leaves nothing to list.
- **`TheVersionlessRecordsDeclareExactlyTheirFields`.** The version-field part moves to `NoWireOrSaveRecordDeclaresAFormatVersion`. The "any re-added field" part is withdrawn as a hand restatement.
- **Residuals, stated:**
  - The e2e module graph is still type-checked only in the browser phase (`tools/E2eRunner/TypeCheck.cs`). The generated module, `wire-frame.ts` and the conformance script are type-checked in every build.
  - There is no TypeScript verifier (H-R1).
  - S9 does not see inside opaque byte vectors (H-W3).
  - CI's build graph is not observed locally (D3).

### Concurrency

Runs after S5b publishes, and S6 waits for it (lane 2 runs one slice at a time).

It shares these files with S5 and S5b: the vendored project file, `WorkshopWireBuildTests.cs`, `CSharpStaticGateTests.cs`, `schemas/README.md` and the `tools/e2e` files.

Shared documents (owner decision 24m): `TODO.md`, `deferred-work.md`, `delivery-batches.md`, `completion-ledger.md` and `sprint-status.yaml`.

The H-T3 test files overlap lane 1's collider repairs only if those repairs edit the same tests. Overlap is checked at dispatch, and any overlap serialises the two slices.

New files are staged with `git add` before the candidate commit.

## Tasks & Acceptance

**Execution:**
1. Hard gate, in `.tools/s5c-evidence/`, with the pinned cached compiler and the pinned Node. If any check fails, stop and return to planning.
   - **(a) Merge.** Merge the files in a scratch copy. Run `flatc --csharp` and `flatc --ts --gen-object-api` on it. Record that both succeed, that the C# types equal the baseline's generated set plus `WireRootIdentifier`, and that the TypeScript entry module re-exports every declaration.
   - **(b) Compile and import.** Compile the generated TypeScript, a stub of `wire-frame.ts` and a stub of the conformance script under `tsconfig.runtime.json`'s options with the pinned `tsc`. Import them from the compiled JS with `flatbuffers` 25.2.10 resolved from the root.
   - **(c) Build graphs.** From the three binary logs of a baseline round, record per graph which project instances run `RestoreRuntimeTypeScript`, `BuildRuntimeTypeScript` and `GenerateWireTypeScript`. Derive CI's publish graph from the project references and the web host's MSBuild calls. Record its Abstractions instance count, and confirm those instances run one after the other. Confirm that D3's placement generates exactly once before the compile in each local graph, `web.binlog` included. Confirm that each replayed `csc` command line carries its references and defines, which S10's semantic model needs.
   - **(d) Verify coverage.** Repeat audit 1's check that every generated `Verify` checks every field of its table, with the last slot at `4 + 2 * (n - 1)`.
   - **(e) Build-test fixture.** Define the copied-tree fixture for the TypeScript build cases. It invokes the moved generation target on an Abstractions copy, offline: generation needs only the compiler cache, not `node_modules`. Record the files it copies and its run time against the full round.
2. **Schema and build:** the merge, `WireRootIdentifier`, the `authoring` marks, the vendored project, the shared provisioning targets, TypeScript generation in the runtime chain, the root pin and the deletions. Rows S1, S2, S12 and S13.
3. **C# codecs and the vendored verifier:** the derived rule, identifiers from the enum, deletion of H-C1 to H-C4, and Core's save-frame check (D7). Rows S3 and S6 to S10.
4. **Conformance:** `wire-frame.ts`, `wire-conformance.ts`, `WireConformanceTests.cs` and `WireDecodeTests.cs`. Rows S3 to S8.
5. **Hand restatements:** `WireSchemaText`, `WireSchemaLayout`, H-T1 to H-T4, H-S1 to H-S6, the test callers in the owned scope, and `WireEnumMirrorTests`. Rows S11 and S14.
6. **Documentation:** `schemas/README.md`, `tools/e2e/README.md` and the planning records; close S5c with its numbers.

**Acceptance Criteria:**
- AC1: Given the candidate, when `git ls-files schemas` is listed, then it shows only `schemas/README.md` and `schemas/workshop_wire.fbs`, and S1 and S2 pass.
- AC2: Given the full local round, when it completes, then:
  - failures are 0 and skipped are 0 in every project;
  - the total is the baseline plus the new tests minus the deleted ones, with the expected delta measured at dispatch;
  - the run log names each added and deleted test, with the row or test that covers it.
- AC3: Given the inventory, when the slice closes, then every row marked "Deleted" or "Derived" is gone from the tree (S11), and every row owned elsewhere is cited from its owner story: `ENGINE-WIRE-ABI` for H-R1, H-W3, H-W4, H-W6 (wire enums) and H-W7; `ENGINE-CAPABILITY-INSTANCES` for H-W6 (non-wire enums).
- AC4: Given the maintainability gate, when it runs, then both canaries pass and violations are 0.

## Spec Change Log

- 25 November 2026, planning audit 1 (`docs/planning/elements/readiness/audits/audit-s5c-1.md`, Fail).
  - Repaired:
    - A1-1: one TypeScript frame check, `wire-frame.ts`.
    - A1-2: distinct frame messages matched to `WireFaultReason`; the clause is added to 24p.
    - A1-3: binary64 number reporting; D8; NaN dropped.
    - A1-4: D3's CI graph, staging, and Inputs and Outputs.
    - A1-5: the task 1(e) fixture and the dispositions of S5b's TypeScript build cases.
    - A1-6: S8 at all four roots, the zero-entry boundary, `WireDecodeTests.cs`.
    - A1-7: coordinator ruling (f) in 24p. C36, `ENGINE-WIRE-ABI`'s Enums bullet and `ENGINE-CAPABILITY-INSTANCES`' enum sentence are amended. S14 is added.
    - A1-8: rulings (a) to (f) are in 24p; OS-01's source column is updated.
    - A1-9: the caller and fixture lists; S11's allow-list.
    - A1-10: citations.
    - A1-11: guards named, one schema-text helper, the `WireSchemaLayout` control, the S9 and S10 limits, the withdrawn intent, and the design record.
- 25 November 2026, planning audit 2 (`audit-s5c-2.md`, Fail).
  - Repaired:
    - A2-1: D7 deletes `ValidateSaveFrame` and the identifier properties from `ICommandCodec`; Core's frame boundary and conversion become public; Abstractions gets no reference.
    - A2-2: H-T4 corrected (the walk control is deleted, with its cover named); the allow-list is limited to marked regions; `CompoundMassAndRollingTests.cs:898-900` is added to D5; the dispatch `rg` is extended.
    - A2-3: S10 forbids generated companions and uses a semantic model; new controls added.
    - A2-4: stamp target as an Input only.
    - A2-5: the ordering argument replaces the concurrency claim.
    - A2-6: `PinnedTargets` and `PinnedImports` are in scope.
    - A2-7: S5b build-test dispositions.
    - A2-8: comments are in S11; four comments are disposed; the theory's `uint` parameter.
    - A2-9: the planning documents (S1 to S14, S5b's W6 line, C36's one rule).
    - A2-10: `SourceIdentity.cs`, the named `WireSchemaText` control, the filter, D8's rationale.
- 25 November 2026, planning audit 3 (`audit-s5c-3.md`, Fail).
  - Repaired:
    - A3-1: D7 keeps `ICommandCodec.ValidateSaveFrame` because the coupling gate (E9) refuses a main-to-Core model dependency. Only the identifier properties leave the interface. The main assembly encodes with the generated root finisher. S10 admits exactly the one `SavedConstructionVerify` reference in `CoreCommandCodec.ValidateSaveFrame`.
    - A3-2: S11's boundary is regions.
    - A3-3: H-T4 deletes only the walk; `:140` is added to D5.
    - A3-4: the declaring file is in the generation Inputs.
    - A3-5: the shared file is imported once.
    - A3-6: S5's build cases are retargeted.
    - A3-7: two messages reworded; S11's scanned roots and the conversion's `string` result are stated.
    - A3-8: C36's sub-points indented.
- 25 November 2026, planning audit 4 (`audit-s5c-4.md`, Fail: A4-2 medium, A4-1 and A4-3 to A4-5 low).
  - All five findings were repaired in this contract afterwards:
    - A4-1: the compiler properties move with the provisioning targets; the shared file joins `GenerateWireAccessors`' Inputs; the pin-mismatch cases edit the shared file.
    - A4-2: S3's boundary can fail: it checks the finisher-written bytes against the conversion, and a re-stamped save is refused.
    - A4-3: the conversion is total over `uint32`; the theory and `WorkshopSaveRejectionTests.cs:42` treatments are stated.
    - A4-4: S10 counts one admitted source location across the replayed compiles.
    - A4-5: stale comments, the `:61` anchor, `OldLayoutSaves.cs:45`, Design Notes 7, the Always rewording to admit the generated root API, and the cross-referenced `WorkshopSaveCodec.cs` bullets.
- 25 November 2026, bounded fifth check (`audit-s5c-5.md`, **Scoped Pass**). The coordinator authorised it after the four-round planning cap; it is limited to the audit 4 repairs. All five audit 4 findings are closed.
  - Three new low findings were repaired afterwards:
    - A5-1: the shared file is named `tools/runtime/FlatcCompiler.targets`; the fixture's `SharedBuildFiles`, `schemas/README.md:41` and the vendored project's comments are in scope.
    - A5-2: these log entries; the foreign identifier's spelling at Always; S5b's `:82` anchor.
    - A5-3: S3 asserts admission and refusal through `ValidateSaveFrame` instead of reading frame bytes.
  - These repairs await re-check at the dispatch re-audit (coordinator ruling, 25 November 2026: no sixth planning round).

## Review Triage Log

## Design Notes

The eight design-record questions (`_bmad/custom/element-free-engine-design.md`, section 6):

1. **What behaviour is delivered?** One declaration of the FlatBuffers ABI. Both bindings are generated from it and proven to agree (owner decision 24p). Unknown fields are refused at every table. No gameplay, solver or presentation behaviour changes.
2. **What is reusable?** One verifier rule, derived from the generated `Verify` code, for every table. One conversion from `WireRootIdentifier` to identifier bytes, and one frame check, per language.
3. **What is declared?** The schema file is the only declaration. Roots and identifiers are schema data, and authoring-only declarations carry a schema mark.
4. **How does it execute?** Schema → `flatc` (C# in the vendored project; TypeScript in the runtime chain) → compile → frame check → C# verifier → decode once.
5. **Who owns changes?**
   - The main assembly owns the authoring codecs and atomic save refusal: a refused save mutates nothing (Always).
   - Core owns the frame boundary and the runtime codecs. The main assembly reaches the boundary for saves only through `ICommandCodec.ValidateSaveFrame`, as the coupling gate requires (D7).
   - The runtime TypeScript chain owns TypeScript generation and compilation.
6. **What proves generality?**
   - No element identity, capability kind or table name is special-cased.
   - The conformance script finds root classes by the generated enum's member names.
   - The test helper finds slots in schema declaration order and is checked against the generated `Add*` counts.
   - S4 and S5 run over every command kind, response kind and payload arm, with C#-derived expectations.
7. **What is deleted?** `workshop_authoring.fbs` and its `include`; the hand field counts and slots; the identifier literals; the three identifier properties of `ICommandCodec`; `SNAPSHOT_SCHEMA` and its hand decoder; the second TypeScript frame check; the field-list test; S5b's include ordering, Debug-only guard and `tools/e2e` manifest.
8. **What remains uncertain?**
   - Task 1's gates: the merge, the strict compile, the build graphs (CI included) and the fixture.
   - D5's list of tests and fixtures, measured at dispatch.
   - H-R1 (the TypeScript verifier, `ENGINE-WIRE-ABI`).
   - Enum ownership under ruling (f) (`ENGINE-WIRE-ABI`).

   Nothing required now is open.

Element-free rules D1 (explicit references: roots by enum member), D4 (complete and atomic: refuse before decode) and D5 (one current representation) apply. The laws under D6 and D7 are unaffected.

**Architecture guards.** S1, S2, S9, S10, S11 and S12 read source, schema text or logs, so they are architecture guards (`docs/testing-guidance.md`, "Architecture guards"). They live in one class, `WireSchemaGuards`, and each has a positive control. They protect the invariants that 24p names: one file, one source, one boundary. No behaviour test can see these invariants.

## Verification

Executor: the implementation owner.

Environment: `PATH=~/.local/share/tim-toolchain/dotnet-10.0.403:~/.local/share/tim-toolchain/node-v26.12.1-darwin-arm64/bin:$PATH` and `DOTNET_ROOT=~/.local/share/tim-toolchain/dotnet-10.0.403`. Record the session environment check in the run log.

**Commands:**
- The five compiler-log capture commands (`docs/delivery-workflow.md:38-53`). Expected: every build succeeds, generates from the one file and compiles `.runtime-build/tools/runtime/wire-conformance.js`.
- `dotnet run --file tools/anvil/VerifyPolicy.cs -- --maintainability --project-root .` Expected: AC4.
- The registration check filter. Expected: 0 failed, 0 skipped (test files are added).
- The scoped run below, with `CSHARP_STATIC_GATE_BINLOGS` set. Expected: 0 failed, 0 skipped.

  `dotnet test CuriousContraptions.slnx -c Debug -f net10.0 --no-build --no-restore --nologo -noAutoResponse --filter "FullyQualifiedName~WireConformanceTests|FullyQualifiedName~WireDecodeTests|FullyQualifiedName~WireSchemaGuards|FullyQualifiedName~WireEnumMirrorTests|FullyQualifiedName~WorkshopWireBuildTests|FullyQualifiedName~WorkshopWireTypeScriptTests|FullyQualifiedName~CSharpStaticGateTests|FullyQualifiedName~VendoredFlatBuffersIdentityTests|FullyQualifiedName~WorkshopFormatTests|FullyQualifiedName~WorkshopSaveRejectionTests|FullyQualifiedName~F32SaveBoundaryTests|FullyQualifiedName~BundleElementIdTests|FullyQualifiedName~WireSchemaTextTests|FullyQualifiedName~TypeCheckBoundaryTests|FullyQualifiedName~DeclaredOrientationTests|FullyQualifiedName~CompoundMassAndRollingTests|FullyQualifiedName~WorkshopSaveTests"`
- `git add -N` on the new files, then `git diff --check <baseline_revision>`. Expected: clean.
- One full local round. Expected: AC2.

### Acceptance-to-test table

No tolerance applies. Decoded integers and f32 values must be exactly equal, because the wire carries them exactly (a defined-interface invariant). Every other assertion is a name, a count, a presence or a typed refusal.

| Rows | Named discriminating test | Control and boundary | Execution |
|---|---|---|---|
| S1 | `WireSchemaGuards.TheSchemaIsOneFileWithOneRootTypeAndNoInclude` | Control: a synthetic `schemas/` with a second `.fbs`, and one with an `include`, are each refused, naming them | planned |
| S2 | `WireSchemaGuards.EveryCompilerRunReadsOnlyTheOneSchemaAndTypeScriptGeneratesOncePerGraph` (binary-log replay) | Control: a synthetic invocation naming two inputs is refused, and so are counts of 0 and 2, each naming the log. Boundary: `web.binlog` | planned |
| S3 | `WireConformanceTests.EveryRootIdentifierIsGeneratedSchemaDataBothSidesAgreeOn` | Control: a buffer finished with another member's identifier is refused by both sides. Boundary: a finisher-written save is admitted by `ValidateSaveFrame`, which fails if the `file_identifier` differs from the converted `SavedConstruction` member; the same save finished with another member's identifier is refused | planned |
| S4 | `WireConformanceTests.TypeScriptDecodesEveryRootAndArmCSharpEncoded`, a theory over the roots, the encoder's command-kind values, its response-kind values and the 11 payload arms | Control: a case whose C#-side expected value is altered fails, naming the field. Boundary: -0 on an f32 field the encoder admits (chosen at dispatch), and the `uint64` maximum | planned |
| S5 | `WireConformanceTests.CSharpDecodesEveryRootTypeScriptEncoded` (D8 comparison) | Control: the set-field case differs in exactly that field. Boundary: an empty vector and an absent optional table | planned |
| S6 | `WireConformanceTests.MalformedFramesAreRefusedOnBothSidesForTheSameCheck` | Each of the four faults, for each root, on both sides, with the TypeScript message matched to the C# reason | planned |
| S7 | `WireConformanceTests.VerifierFailuresAreRefusedInCSharp` | Control: the same buffer with the offset in range is admitted | planned |
| S8 | `WireDecodeTests.AnAppendedFieldIsRefusedAtEveryRootInANestedTableAndInASave` | Control: a table with its last schema field set is admitted. Boundary: one extra zero vtable entry is refused | planned |
| S9 | `WireSchemaGuards.NoRuntimeRootReachesAnAuthoringDeclaration` | Control: a synthetic schema whose `WorkshopCommand` names `ConstructionRecord` is refused, naming the path | planned |
| S10 | `WireSchemaGuards.NoWorkerCompileReferencesAnAuthoringDeclaration` (semantic model of each replayed compile) | Control: each of these synthetic compiles is refused: a Simulation source with a variable of type `InstanceRecord`; a second Core site naming `SavedConstructionVerify`, outside `CoreCommandCodec.ValidateSaveFrame`; a Core source naming another companion, such as `InstanceRecordVerify`; a source reaching a marked type through a `using` alias. Boundary: the one admitted reference, in `CoreCommandCodec.ValidateSaveFrame`, and `WireRootIdentifier.SavedConstruction` are admitted | planned |
| S11 | `WireSchemaGuards.NoHandSchemaRestatementRemains` | Control: synthetic sources holding `StartTable(12)`, `"CWCM"`, a `SNAPSHOT_SCHEMA` declaration, a `4 + 2 * slot` expression or a second TypeScript frame check, outside the allow-list, are each refused, naming file and line. Boundary: a marked historical-fixture region is admitted, and a restatement outside its markers in the same file is refused | planned |
| S12 | `WireSchemaGuards.TheFlatBuffersRuntimeIsPinnedAtTheRoot` (S5b's T7, inverted) | Control: a version range, a `tools/e2e/package.json` naming `flatbuffers`, and a `paths` mapping are each refused | planned |
| S13 | `WorkshopWireBuildTests.AnAddedSchemaFieldIsAnAccessorInBothLanguagesAfterAnIncrementalBuild` | Control: the accessor is absent before the edit | planned |
| S14 | `WireEnumMirrorTests.EachDomainMirrorOfAWireEnumMatchesItsGeneratedDeclaration` (interim) | Control: a synthetic pair that differs in one member's value, and one that differs in one name, are each refused | planned |
| — | `WireSchemaTextTests.EveryTableFieldCountEqualsItsGeneratedAddMethods` | Control: a synthetic schema text with one field more than its generated type is refused, naming the table | planned |
| AC1 to AC4 | `git ls-files`, the full round, the maintainability gate | The baseline round is recorded at the start | planned |
