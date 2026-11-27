---
title: 'One schema file is the ABI: the merged schema, root identifiers as schema data and the derived C# refusal (S5c-1)'
type: 'refactor'
created: '2026-11-25'
status: 'done'
baseline_revision: '39aaba70ce15650a9ca351a0666a68830dcc3e1a'
review_loop_iteration: 0
followup_review_recommended: false
context:
  - '{project-root}/schemas/README.md'
  - '{project-root}/docs/testing-guidance.md'
  - '{project-root}/docs/delivery-workflow.md'
  - '{project-root}/_bmad-output/implementation-artifacts/spec-s5-generate-wire-accessors-in-build.md'
  - '{project-root}/_bmad-output/implementation-artifacts/spec-s5b-generate-typescript-wire-in-build.md'
  - '{project-root}/docs/planning/elements/readiness/owner-decisions-2026-11-22.md'
warnings: []
deferred:
  - summary: >-
      BH1-1: the browser-deferred tools/e2e/unit/run-reset-snapshot.test.ts reads the deleted schemas/workshop_authoring.fbs.
    evidence: |-
      Review pass 1. S5c-1 touches no tools/e2e file (coordinator ruling, 26 November 2026); the test is on the BrowserDeferral list and already failed 23 of 24 cases (BH3-4). Owner S5c-3 (inventory row H-S2).
    location: >-
      tools/e2e/unit/run-reset-snapshot.test.ts:17
    severity: low
  - summary: >-
      BH1-2 and VG1-1: tools/e2e/workshop-wire.ts keeps hand identifier literals and its own frame check, unchecked against the schema's WireRootIdentifier.
    evidence: |-
      Review pass 1. Code this slice does not change (coordinator dispatch rule); owner S5c-2 (inventory row H-S3, wire-frame.ts, and row S3's conformance check).
    location: >-
      tools/e2e/workshop-wire.ts:14-16
    severity: low
  - summary: >-
      BH2-1 (runner part) and ECH2-1: the e2e runner's schema-record check advises a rebuild for a second schemas/*.fbs file, which no build clears now that schemas.txt always names the one file.
    evidence: |-
      Review pass 2. Runner code this slice does not change (coordinator ruling: no runner file in S5c-1); S1 refuses a second schema file in the full round. Owner S5c-2 (A6-1 deletes the record and this comparison); tools/e2e/workshop-wire.ts:2 likewise S5c-2 (A6-3 (d)).
    location: >-
      tools/E2eRunner/Toolchain.cs:74-96
    severity: low
  - summary: >-
      P5-5: schemas/README.md and the comment on attribute "authoring" state only that no runtime root reaches a marked declaration, not the closure rule S9 enforces (an unmarked declaration may not reference a marked one).
    evidence: |-
      Review pass 5 (element-free auditor). Documentation improvement outside the repair diff that breaks no rule (Re-review scope); S9's refusal message states the rule when hit. Owner S5c-2, which rewrites the README's TypeScript section and touches the schema comments.
    location: >-
      schemas/README.md; schemas/workshop_wire.fbs (attribute "authoring")
    severity: low
---

<intent-contract>

## Intent

**Problem:** Owner decision 24p (25 November 2026): "we need the flatbuffers to have one spec as a file, that the dotnet and typescript bindings are generated from, this is the foundation of the ABI between the systems and threads and needs to be resillient".

Today the FlatBuffers ABI is two files:
- `schemas/workshop_wire.fbs`, with 127 declarations;
- `schemas/workshop_authoring.fbs`, with 9 declarations and `include "workshop_wire.fbs"` at `:1`.

There are four roots: `WorkshopCommand` (`CWCM`), `WorkshopResponse` (`CWRM`), `WorkshopObservationBatch` (`CCWO`) and `SavedConstruction` (`CCWS`). The schema declares only one `file_identifier` (`workshop_authoring.fbs:95`). The other three identifiers exist only as hand-written C# and TypeScript literals, and in a comment at `workshop_wire.fbs:348-351`. Unknown-field refusal uses hand C# vtable field counts and slot numbers on 6 of the 79 tables (`engine/gpu/WorkshopWireFlatBuffers.cs:45-91`; `engine/gpu/WorkshopAuthoringFlatBuffers.cs:34-102`).

**Split (coordinator ruling, 26 November 2026).** S5c is delivered as three slices in lane 2, one after another, each from the main that contains the one before:
- **S5c-1** (this contract): the one schema file, root identifiers as schema data, the authoring mark, the derived C# refusal and the C# codecs. Rows S1, S8, S9 and S10.
- **S5c-2** ([contract](spec-s5c-2-wire-conformance-and-root-pin.md)): TypeScript generation in the runtime chain, the root `flatbuffers` pin and the cross-language conformance test. Rows S2 to S7, S12 and S13.
- **S5c-3** ([contract](spec-s5c-3-remove-hand-schema-restatements.md)): the remaining hand restatements. Rows S11 and S14.

Row and inventory IDs (S1 to S14, H-*) are shared by the three contracts. Run log E4 maps every row, inventory item and audit 6 repair to its part.

**Approach:**
- **One file.** The two files merge into `schemas/workshop_wire.fbs`, and `workshop_authoring.fbs` is deleted. The file declares the four root identifiers as an enum whose values are each identifier's four bytes read as a little-endian `uint32`, and marks the authoring-only declarations with a declared attribute. That mark replaces the file boundary that kept authored records out of the runtime roots.
- **Unknown-field refusal** becomes one rule in the vendored C# FlatBuffers verifier, derived from the generated `Verify` code, for every table. The hand counts are deleted (H-C1, H-C2, H-C4).
- **C# identifiers** come only from generated code (H-C3). `ICommandCodec` loses its three identifier properties and keeps `ValidateSaveFrame` (D7).
- **One schema reader.** The test helper `WireSchemaText` reads the file under a closed grammar, and `WireSchemaLayout` derives slots from it.
- **Guards:** one file (S1), no runtime root reaching an authoring declaration (S9), and no worker compile referencing one (S10).
- **Both generation steps pass exactly `schemas/workshop_wire.fbs`** (coordinator ruling on FO1-3, 26 November 2026, correcting the lane planner's earlier wording here and at D9). `WireSchema` is that one item, not a glob; the `OrderWireSchemas` target (its `flatc -M` parsing, include ordering and file-name check) and `ListWireSchemas` with its stamp are deleted whole: a multi-file path beside S1 would be a second rule. S5b's TypeScript generation otherwise stays in the vendored project until S5c-2 (`GenerateWireTypeScript`, Debug-only), writing `tools/e2e/.wire/schemas.txt` with the one name for the runner; no runner file changes. S1 refuses any second schema file.

## Boundaries & Constraints

**Always:**
- **Exactly one schema file.**
  - `schemas/` holds exactly one `.fbs` file, `schemas/workshop_wire.fbs`. It declares every table, struct and enum, and every type the four roots reach. It has no `include`.
  - It has exactly one `root_type` and one `file_identifier`: `root_type SavedConstruction; file_identifier "CCWS";`. The save is the root that lives in files.
  - The C# generation step passes `@(WireSchema)` (`CuriousContraptions.FlatBuffers/CuriousContraptions.FlatBuffers.csproj:43`), the one item `../schemas/workshop_wire.fbs` (FO1-3 ruling). S5c-2 defines that path once in the shared targets file and proves it from the logs (S2).
- **Root identifiers are schema data.**
  - The file declares `enum WireRootIdentifier : uint32` with one member per root. Each member is named exactly as its root table. Its value is the root's four identifier bytes read as a little-endian `uint32`:
    - `WorkshopCommand = 0x4D435743` ("CWCM")
    - `WorkshopResponse = 0x4D525743` ("CWRM")
    - `WorkshopObservationBatch = 0x4F574343` ("CCWO")
    - `SavedConstruction = 0x53574343` ("CCWS")

    The values are in ascending order, and each is commented with its ASCII text.
  - C# takes an identifier only from generated code: the generated `WireRootIdentifier`, or the root API that `root_type SavedConstruction` emits (`SavedConstruction.FinishSizePrefixedSavedConstructionBuffer`, `SavedConstructionBufferHasIdentifier`). The main assembly's save encoder uses that finisher.
  - C# has exactly one conversion function, internal, beside Core's frame boundary. It is total over `uint32`: any `WireRootIdentifier` value, whether a member or not, maps to its four little-endian bytes as a four-character identifier. It yields the `string` that the vendored `FinishSizePrefixed` and `VerifyBuffer` take (`CuriousContraptions.FlatBuffers/FlatBufferBuilder.cs:1016`, `FlatBufferVerify.cs:781`), so no call site converts a second time. The TypeScript conversion is S5c-2's.
  - No C# source outside generated code holds a literal for any of the four current root identifiers, in code, except the historical-fixture regions (H-T3, H-T4) and the foreign test data below. The comments this slice's files hold that name one are reworded to name the enum member: `CuriousContraptions.tests/SaveFixture.cs:7` and `engine/gpu/WorkshopSaveCodec.cs:13`.
  - **Evidence limit:** the TypeScript identifier literals (`tools/e2e/workshop-wire.ts:14-16`, `:79`; `tools/e2e/workshop-driver.ts:139`, `:929`; `tools/e2e/run-reset-snapshot.ts:321`; `tools/e2e/unit/run-reset-snapshot.test.ts:86`) stay until S5c-2 and S5c-3. The tree-wide literal guard, comments included, is S11 (S5c-3). `tools/e2e/unit/run-reset-snapshot.test.ts:17-18` reads the deleted `schemas/workshop_authoring.fbs` until S5c-3 (H-S2). The test is browser-deferred and is not run.
  - C# test attributes that need an identifier take the enum member, which is a constant. The mixed theory at `WorkshopSaveTests.cs:398-401` takes a `WireRootIdentifier` parameter: the member for the current root, and `(WireRootIdentifier)Foreign` for the foreign word `"XXXX"`, where `Foreign` is a named `uint` constant. Both go through the one conversion. `WorkshopSaveRejectionTests.cs:42`'s optional parameter defaults to `WireRootIdentifier.SavedConstruction` and is converted at its one use.
  - Deliberately foreign identifiers in negative tests stay as test data: `"CCWC"` and `"CCWR"` at `CuriousContraptions.tests/ClutchGeneralityTests.cs:443`, and the foreign word at `WorkshopSaveTests.cs:398`.
  - The pinned compiler 25.2.10 was run on a probe of this declaration in planning, and audit 1 recompiled it: it generated `public enum WireRootIdentifier : uint` and `export enum WireRootIdentifier` with these values.
- **Authoring declarations are marked.**
  - The file declares `attribute "authoring";` and marks exactly the nine declarations that `workshop_authoring.fbs` holds today: `CadenceSettingsRecord`, `ConnectionRecord`, `AssistanceKnotRecord`, `ParameterRecord`, `InstanceRecord`, `IntegrityGoalRecord`, `PuzzleRecord`, `ConstructionRecord` and `SavedConstruction` (`workshop_authoring.fbs:4`, `:13`, `:25`, `:39`, `:44`, `:57`, `:62`, `:79`, `:88`).
  - The pinned compiler accepts the attribute on tables and structs and emits nothing for it (planning probe; audit 1).
  - No runtime root may reach a marked declaration (row S9).
- **One derived rule refuses unknown fields, in the vendored verifier.**
  - `Verifier` records, for each table being verified, the highest vtable offset passed to every per-field `Verify*` method of that table, both offsets of `VerifyUnion` and `VerifyVectorOfUnion` included (`FlatBufferVerify.cs:658`, `:696`). `VerifyTableEnd` (`:462-466`) then refuses the table if its vtable is longer. Tables nest, so the record is kept per table (a stack), and it resets in `VerifyBuffer`.
  - Every generated `Verify` checks every field of its table, with the last slot at `4 + 2 * (n - 1)` for n fields (audit 1, all 79 tables, 0 mismatches). That premise holds because the schema has no `union`, no `deprecated` field and no `id:` attribute, which `WireSchemaText` refuses (below). The rule therefore covers every table at every depth, and no count is written by hand.
  - A vtable one entry longer than its table, with that extra entry zero, is also refused. This changes today's save rule, which refused only a present extra field (`engine/gpu/WorkshopAuthoringFlatBuffers.cs:98-99`). It refuses no valid buffer: the C# and TypeScript builders trim trailing zero entries.
  - It is a recorded patch to the vendored runtime, like Story 9A.45's. `THIRD-PARTY-NOTICES.md` records the rule as a modification of `FlatBufferVerify.cs`: the per-table slot record, the refusal in `VerifyTableEnd`, and the reason. It states that any runtime upgrade re-applies this and Story 9A.45's patches, and that the direct verifier tests fail if one is dropped. The vendored project's header (`CuriousContraptions.FlatBuffers.csproj:7-9`) and `VendoredFlatBuffersIdentityTests.cs:5-12` say the same (A6-5 (1)).
  - Apache-2.0 §4(b) (`CuriousContraptions.FlatBuffers/LICENSE:98-99`; A6-6, a pre-existing defect fixed here): `FlatBufferVerify.cs` and `Table.cs` each carry, directly after the Google licence header, the notice "Modified by the Curious Contraptions project: <the file's changes: Story 9A.45's checked vtable positions (both files); S5c's unknown-field rule (FlatBufferVerify.cs)>. See THIRD-PARTY-NOTICES.md."
  - A refused buffer is `WireFaultReason.VerifierRejected` at the wire boundary and `WorkshopSaveRejection.VerifierRejected` on the save path (`engine/gpu/WorkshopSaveCodec.cs:59-69` maps it). A refused save changes nothing: the codec refuses before any domain construction (`WorkshopSaveCodec.cs:48-55`).
- **Every C# decode checks the frame first:** the size prefix must cover the buffer exactly, then the identifier must be the expected root's, then the generated verifier runs with the derived rule, and the buffer is decoded once (`WorkshopWireFlatBuffers.OpenFrame`, `engine/gpu/WorkshopWireFlatBuffers.cs:25-56`). The save path reaches that boundary only through `ICommandCodec.ValidateSaveFrame` (D7).
- **The worker boundary holds after the merge.** No worker receives authored instances (`schemas/README.md:3`). Assembly ownership enforces this in three ways:
  - `WorkshopAuthoringFlatBuffers`, `WorkshopSaveCodec` and `WorkshopSavePrevalidation` compile only into the main assembly. `CuriousContraptions.csproj` compiles `engine/gpu/*.cs` minus `@(CoreRuntimeSource)` (`CuriousContraptions.Core/RuntimeSources.props`).
  - No runtime root reaches a declaration marked `authoring` (row S9).
  - No compile of `CuriousContraptions.Core`, `CuriousContraptions.Simulation` or `CuriousContraptions.Animation.Worker` references a marked declaration, or one of its generated companions, as a type (row S10), with one exception: `SavedConstructionVerify` in Core's `ValidateSaveFrame` (`CuriousContraptions.Core/CoreCommandCodec.cs:9`), which verifies a save frame for the main assembly through `ICommandCodec` and yields no authored instance (D7). Abstractions gains no project reference.
- **One schema reader.** `WireSchemaText` (test helper) is the only parser of the schema file. Tree-wide source scans that read `.fbs` as text (`WorkshopFormatTests.cs:91`, `HalfPrecisionGuardTests.cs:23`, `TypedTransportCommandTests.cs:393`) are not schema readers. It accepts exactly this subset and refuses anything else by name and line:
  - Comments: `//` to end of line (`///` included); `/*` is refused.
  - Top level: exactly one `namespace CuriousContraptions.Wire;`; exactly one `attribute "authoring";`; `enum Name : <integer scalar> { Member [= <decimal or 0x integer>], … }`; `struct Name [(authoring)] { field: <scalar or declared struct or enum>; … }`; `table Name [(authoring)] { field: <type> [= <decimal float | null>] [(required)]; … }`; exactly one `root_type Name;`; exactly one `file_identifier "XXXX";`. A member without a value takes the previous member's value plus one, and the first takes 0, as the compiler assigns.
  - A type is a scalar name, `string`, a declared name, or `[` one of those `]`. A field's only attribute is `required`, and a declaration's only attribute is `authoring`. The scalar names are exactly `bool int uint ulong ubyte uint8 int32 uint32 int64 uint64 float double`; an integer scalar is one of these other than `bool`, `float` and `double`. The scalar, default and enum-value sets are exactly those the merged file uses (A7-1, closed to the measured inventory, run log E5); a schema that needs another extends the grammar in its own slice.
  - Refused: `include`, `union`, `rpc_service`, `file_extension`, any other attribute (`deprecated`, `id:`, `key`, `nested_flatbuffer`, `force_align`, `bit_flags`, …; field names `id` and `key` are admitted), fixed arrays `[T:n]`, a second namespace, and a type name that resolves to no declaration. A `union`, `deprecated` or `id:` refusal names the construct and its line; `WireSchemaLayout`, S9 and the `Verify` coverage premise rely on it, so it is made once, in the parser, not in generation (A6-5 (3)).
  - `CSharpStaticGateTests.SchemaTypesIn`, `BundleElementIdTests` (`:209-215`), `WorkshopFormatTests` (`:20-38`), `WireSchemaLayout`, S1 and S9 read only its model.
  - `WireSchemaLayout` returns a field's slot from its position in the declaration.
  - The one copied-tree build case whose schema text leaves the closed grammar (the stray-output case: a second namespace) asserts on its refusal line, never through `WireSchemaText`; every other build case reads one closed-grammar file.
- **Forward-only.** There is no format version (`schemas/README.md:17-18`), no old-schema path, no second schema file, no alias type or re-export, and no fallback decoder. The merge moves declarations without renaming or reordering any field.

**Never:**
- A second `.fbs` file anywhere that the build reads, an `include`, or a generated schema fragment.
- A field renamed, reordered or removed, or a change to the layout of any table or struct. The schema changes only in these ways: the two files merge; the `WireRootIdentifier` enum is added; the `authoring` attribute and its marks are added; `root_type WorkshopResponse` (`workshop_wire.fbs:1122`) is deleted, because no caller uses a root-only generated API (audit 1); the identifier comment at `:348-351` is deleted, because the enum carries its content.
- Any file under `tools/E2eRunner/`, `tools/E2eRunner.Tests/` or `tools/e2e/` (coordinator ruling, 26 November 2026), or any change to S5b's TypeScript generation beyond the FO1-3 ruling (Always, "Both generation steps"): `WireSchema` becomes the one item, `OrderWireSchemas` and `ListWireSchemas` are deleted, and `GenerateWireTypeScript` passes `@(WireSchema)`, records its one name and takes the C# generation stamp as an Input in place of the deleted list stamp (coordinator acceptance, run log E11); its Debug-only condition, staging, annotation normalisation, output refusals and Outputs are unchanged.
- Running or gating the browser toolchain (`tsx`, Playwright) in a required run, or the deferred `tools/e2e/unit/run-reset-snapshot.test.ts`. The `BrowserDeferral` list is unchanged.
- Hashes or byte comparisons of encoded output as acceptance; mutants; a local Release worker build or web publish.
- Moving the thread-boundary layouts here (pose ring, clock and event rings, schedule controls, animation control and output, the physics GPU ABI records). They stay with `ENGINE-WIRE-ABI`, which declares them in this same file (owner decision 24p).

## I/O & Edge-Case Matrix

| Row | Input / State | Expected Output / Behavior | Error Handling |
|-----|--------------|---------------------------|----------------|
| S1 | The candidate tree | `schemas/` holds exactly one `.fbs` file. It has no `include` and exactly one `root_type` and one `file_identifier`. Every root named by `WireRootIdentifier` is a table the file declares | The guard names each extra file or line |
| S8 | One field appended beyond the schema: at each of the four roots; in a nested vector table that no hand check covered before (`CompiledSceneRecord` rows); in a save's `InstanceRecord`. Boundary: one extra vtable entry that is zero | Each is refused with `VerifierRejected`; a save gets `WorkshopSaveRejection.VerifierRejected`. Control: a table with its last schema field set is admitted | The refusal carries `VerifierRejected` and the root's identifier; each test case names its table |
| S9 | The schema text | No runtime root (`WorkshopCommand`, `WorkshopResponse`, `WorkshopObservationBatch`) reaches a declaration marked `authoring` through any field. `SavedConstruction` and `InstanceRecord` are marked. The guard follows the parser's resolved field types (a vector's element type included): every runtime root is unmarked, and every declaration that references a marked one is itself marked, so no path from a root can reach a marked declaration (coordinator correction after review pass 4, run log E15); an unresolved name cannot occur (the parser refuses it). Evidence limit: opaque byte vectors (`motion`, `response`, `record`, `probe`, `peer`, `pending`; `workshop_wire.fbs:306`, `:357-361`, `:378-379`) are not visible to this guard (H-W3) | Names the declaration and field that cross from unmarked to marked (for a root's own field, the root and that field) |
| S10 | The C# compiles of Core, Simulation and Animation.Worker, replayed from the binary logs | No type symbol that a compile references binds to a forbidden type: each declaration marked `authoring`; its generated companions (`<Name>Verify`, and any other type the pinned compiler emits for it, measured in task 1(a)); `WorkshopAuthoringFlatBuffers`, `WorkshopSaveCodec` and `WorkshopSavePrevalidation`. The replay extends the gate's one semantic seam, `CSharpStaticGateTests.MatchingCompilations` (`:370-382`; `CSharpCommandLineParser` over the gate's one command-line split), with no second replay or tokenizer, runs the generators from its analyzer references through `CSharpGeneratorDriver`, and refuses (naming project, file and diagnostic) any replayed compile whose model then reports an error, so a binding gap cannot hide a reference. It inspects every type symbol bound in every syntax tree, generator output included: type syntax, member access, object creation, `typeof`, `nameof`, attribute arguments and generic arguments; XML doc `cref`s are not inspected (stated limit). The enum member `WireRootIdentifier.SavedConstruction` is not a type and does not trip it. Exactly one reference is admitted, `SavedConstructionVerify` in `CoreCommandCodec.ValidateSaveFrame` (D7), as one source location (file and span) deduplicated across the replayed compiles; a second distinct location in any compile fails | Names the project, file and type |

</intent-contract>

## Code Map

Baseline `39aaba70`, the published main that contains S5b (coordinator decision 38); scope guards compare against it. Every `path:line` below was re-read at `39aaba70` by `audit-s5c-6.md`, whose citation corrections (A6-11) are applied.

**Status:** done (`final8` 7457/0/0, `Skipped: 0`; registration 828/0/0; maintainability PASS; pass 7 clean; Final diff scope Pass, `fa6f33a1`, run log E18). Implementation owner: fresh non-fork subagent `ad7ec364d6b3a6c36` (inherited model). S5c-1 runs now in lane 2, after S5b and before S5c-2, S5c-3 and S6 (owner decision 24p; coordinator ruling, 26 November 2026).

**Planning verdict:** Scoped Pass, 26 November 2026 (`audit-s5c-8.md`, bounded check of audit 7's repairs); its four low findings are repaired here. History in the Spec Change Log and run log E2, E4 and E5.

**Review tier:** engine (schema, a vendored runtime patch, wire codecs, build tests). **Shape:** one structural slice, not a Family slice or closure review.

Owned scope (the owned scope list):
- **`schemas/workshop_wire.fbs`**: gains the nine authoring declarations from `workshop_authoring.fbs:4-92`, after the wire declarations, unchanged apart from their `(authoring)` mark; gains `attribute "authoring";` and `enum WireRootIdentifier : uint32`; loses `root_type WorkshopResponse` (`:1122`) and the identifier comment (`:348-351`); ends with `root_type SavedConstruction; file_identifier "CCWS";`. `schemas/workshop_authoring.fbs` is deleted.
- **`CuriousContraptions.FlatBuffers/FlatBufferVerify.cs`** (`VerifyTableStart` `:445-458`, the per-field `Verify*` methods, `VerifyTableEnd` `:462-466`, `VerifyBuffer` `:781`): the derived rule and the §4(b) notice. **`Table.cs`**: the notice only.
- **`THIRD-PARTY-NOTICES.md`**: the modification record and the re-apply-on-upgrade instruction.
- **`CuriousContraptions.FlatBuffers/CuriousContraptions.FlatBuffers.csproj`**: the header's re-apply sentence (`:7-9`) and the FO1-3 changes. `WireSchema` (`:44`) is the one item `../schemas/workshop_wire.fbs`. `OrderWireSchemas` (`:212-276`) is deleted whole, its file-name check included. `ListWireSchemas`, its `DependsOnTargets` uses (`:128`, `:174`), the `WireSchemas.list` Inputs (`:129`, `:175`) and its `FileWrites` entry (`:148`) are deleted: with one literal item the list cannot change except through the project file, which is already an Input. `GenerateWireTypeScript` passes `@(WireSchema)` and writes `schemas.txt` from `@(WireSchema->'%(Filename)%(Extension)')`; its Inputs take the C# generation stamp `$(IntermediateOutputPath)WireAccessors/.generated` in place of the deleted list stamp, so a no-incremental build still regenerates (coordinator acceptance, run log E11). The comments at `:5`, `:125-127` and `:152-169` are reworded to describe one file; the comment `:119-120` is deleted with `ListWireSchemas` (project-file lines are the pre-ruling candidate tree's; baseline is one line earlier from `:44`).
- **`CuriousContraptions.tests/VendoredFlatBuffersIdentityTests.cs`**: re-pins `:23` and `:27` (the `Table.cs` pin changes for its notice only); the summary `:5-12` says both files carry the notice and an upgrade re-applies the patches. `CSharpStaticGateTests.VendoredFiles` takes `Pinned` (`:118`), so it needs no separate edit.
- **`CuriousContraptions.tests/CSharpStaticGateTests.cs`**: the vendored project file's pin (`:116`), because its header changes; `SchemaTypesIn` (`:506-512`) reads `WireSchemaText`'s model.
  - `TheGeneratedWireRuleAdmitsOnlySchemaTypes` (`:987-1034`): its synthetic schema becomes a complete text of the closed grammar (`namespace CuriousContraptions.Wire;`, `attribute "authoring";`, the enum, struct and table, `root_type Holder; file_identifier "TEST";`). The regex reader `SchemaDeclaration` (`:504`), which `WireSchemaText` replaces, is deleted. The `UnionVerify` case and `GeneratedWireCase.UnionVerify` are deleted, and with them `SchemaKind.Union` (`:502`), the union arm of the Verify-class rule (`:550-553`) and "or union" in its texts (`:516`, `:543`, `:999-1001`) and the union sentence of the theory summary (`:987-989`). `schemas/README.md:36` says "tables". `WireSchemaText` has no fragment mode (A7-2).
  - S10 takes each run's parsed arguments from the seam and runs `CSharpGeneratorDriver` over their `AnalyzerReferences` itself; `MatchingCompilations`' output to its existing callers (`GameFloatGuardTests.cs:131`) is unchanged (A7-5).
- **C# codecs** (H-C1 to H-C4):
  - `engine/gpu/WorkshopWireFlatBuffers.cs`: `:18-19` (the identifier constants) deleted, and the production uses (`:45`, `:53`, `:152`, `:158`, `:567`, `:582`) take the converted member; `:45-53` (`fieldCount` and the `ValidateBindingShapes` call) and `:58-91` (`ValidateBindingShapes`, `TableTarget`) deleted; `:104-109` `IsIdentifier` compares against the converted member.
  - `engine/gpu/WorkshopWireFlatBuffers.Observation.cs:11` (`ObservationIdentifier`, used at `:25`, `:34`): the same.
  - `engine/gpu/WorkshopAuthoringFlatBuffers.cs`: `:34-74` (`OpenFrame`, `CurrentPuzzleTable`, `Reference`) and `:76-102` (`InstanceRecordFields`, `SavedConstructionSlot`, `ConstructionInstancesSlot`, `HoldsFieldBeyondTheInstanceTable`) deleted; the verifier rule and the schema's `(required)` on `assistance` cover them.
  - `engine/gpu/WorkshopSaveCodec.cs`: `:17` (`Identifier`) deleted; `Encode` (`:29`) finishes with `SavedConstruction.FinishSizePrefixedSavedConstructionBuffer`; `Open` (`:59-61`) calls `codec.ValidateSaveFrame(bytes)` and then reads the root from the size-stripped buffer, the work of the deleted `OpenFrame`, moved here; `:52-54` deleted; the `codec` parameters (`:35`, `:48`, `:59`) stay used; the summary `:44-47` and the comment `:13` are reworded.
  - `CuriousContraptions.Abstractions/RuntimeCodecs.cs:15-16`, `:19` and `CuriousContraptions.Core/CoreCommandCodec.cs:15-16`, `:19`: the three identifier properties are deleted from `ICommandCodec` and its one implementer; no consumer reads them (audits 2 and 3, `rg`). `ValidateSaveFrame(ReadOnlySpan<byte>)` (`RuntimeCodecs.cs:9`) stays; Core's implementation (`CoreCommandCodec.cs:9`) passes `WireRootIdentifier.SavedConstruction` and `SavedConstructionVerify.Verify` to its one internal frame boundary (D7).
- **New test files:** `WireSchemaText.cs` (the reader and `WireSchemaLayout`); `WireSchemaTextTests.cs`; `WireDecodeTests.cs` (S8); `VendoredFlatBuffersVerifierTests.cs` (A6-5 (2)); `WireSchemaGuards.cs` (S1, S9, S10; S5c-2 adds S2 and S12, S5c-3 adds S11).
- **Tests that S5c-1's changes break, repaired here** (dependency test of the coordinator ruling; each would fail to compile or fail its assertion otherwise):
  - `WorkshopFormatTests.cs`: `:30-31` reads the one file through `WireSchemaText`; `:96-123` `TheVersionlessRecordsDeclareExactlyTheirFields` (H-T1, it reads the deleted file at `:99-100`) is deleted, its cover under Protections; `:137` and `:180` (inside the historical region `:160-181`; A6-3 (k)) take the converted `WireRootIdentifier.SavedConstruction`.
  - `BundleElementIdTests.cs:209-215` reads `WireSchemaText`'s model.
  - `DeclaredOrientationTests.cs` (H-T4): the slot comment `:137-138` is deleted; the walk `:168-180` is deleted (it calls the deleted `HoldsFieldBeyondTheInstanceTable`), covered by S8's control (current records are admitted) and the existing save and command round trips; the round-trip and owner assertions `:157-167` stay, renamed `CurrentRecordsRoundTripThroughTheSaveAndTheCommand`; `:140` takes D5's reason; `:154-156` is reworded; the historical layout `:148-151` stays (its markers are S5c-3's).
  - Callers of the deleted `WorkshopAuthoringFlatBuffers.OpenFrame`: `F32SaveBoundaryTests.cs:12` (with its `"CCWS"` literal; the message at `:48` is derived from the member) and `GoalSignalSelectorTests.cs:81`.
  - Callers of `WorkshopSaveCodec.Identifier`: `WorkshopSaveRejectionTests.cs:42`; `CompoundMassAndRollingTests.cs:896`, `:900`; `WorkshopWireFlatBuffersTests.cs:43`, `:46`; `WorkshopSaveTests.cs:399-401`; `WorkshopFormatTests.cs:137`, `:180`; `SaveFixture.cs:22` (and its comment `:7`).
  - Callers of `CommandIdentifier`, `ResponseIdentifier` and `ObservationIdentifier`: `CompiledSceneBoundaryTests.cs:199`, `:615`; `RunControlTableTests.cs:411`; `TypedTransportCommandTests.cs:360`; `WireFaultTests.cs:207`, `:214`; `WorkshopIntegrationHarnessTests.cs:143`; `WorkshopObservationTests.cs:400`, `:555`; `WorkshopWireTests.cs:760`; `OldLayoutSaves.cs:45` (the identifier only, inside its historical region). Re-measured at dispatch with `rg -n "CommandIdentifier|ResponseIdentifier|ObservationIdentifier|WorkshopSaveCodec.Identifier"`.
  - The tests D5 measures.
  - `WorkshopWireBuildTests.cs` (A6-3 (j)): the edits at `:68` (S5) and `:82` (S5b T2) and the touches at `:99` (S5) and `:123` (S5b T3) are retargeted to `schemas/workshop_wire.fbs`, each edit anchored on `table SavedConstruction (authoring) {`. `ASchemaSetWithoutOneRootReachingEverySchemaRefusesTheBuild` (`:205-296`) and `ADeletedSchemaFileLeavesNoGeneratedSource` (`:334-354`) are deleted with `OrderWireSchemas`, and so is their helper `MisnamedSchemas`; S1 covers one file (FO1-3 ruling; audit 9). The stray-output case is kept as its own `[Fact]` on the one file: after a first build, a second `namespace S5bOther;` with one table is appended to `schemas/workshop_wire.fbs`; the build is refused on the line "wrote TypeScript outside the wire namespace's module", naming `s5b-other`; the previous TypeScript output's write times are unchanged. It asserts on the refusal line, never through `WireSchemaText`; `RefusalLine` stays for it. Every other case runs on the one closed-grammar file. The comments at `:79` and `:185`, the summary `:11` and the comment `:310-311` are reworded: the one schema file, and the space-path case proving the quoted full paths of both generation commands (A7-3; A9-9).
- **Unchanged and re-verified by the bounded re-audit:** `WorkshopWireTypeScriptTests` (T1 reads `SchemaTypesIn`; its schema-record check holds with one file) and the e2e runner's `RequireGeneratedWireModule` (`tools/E2eRunner/Toolchain.cs:74-100`) with its tests.
- **`schemas/README.md`**: `:1-9` (one file, the identifier enum, the authoring mark), `:36` ("tables", A7-2), `:48-52` (the derived refusal replaces "the hand vtable field counts") and the `WorkshopFormatTests` clause of `:90-91`. In the TypeScript section, the sentences that describe `OrderWireSchemas` (the `-M` order, the root and include refusals, the file-name and include-spelling rules, "the list of schema files") are reworded here to the one schema file (FO1-3); the rest of that section, `:41`, `:64` and `:69-71` are S5c-2's; the `SNAPSHOT_SCHEMA` clause is S5c-3's.
- **Planning records:** this contract and its run log; `TODO.md`; `sprint-status.yaml` (key `s5c-one-wire-schema-file`); `docs/planning/invest/delivery-batches.md` (S5c); and the planning edits of run log E2 and E4 that this candidate carries: `owner-decisions-2026-11-22.md`, `epics.md`, `completion-ledger.md`, `engine-stories-index.md`, `named-elements.md`, `vertical-delivery.md`, `deferred-work.md`, the S5b run log (E37), the S5c-2 and S5c-3 contracts and run logs, and `audits/audit-s5c-6.md`, `audit-s5c-7.md`, `audit-s5c-8.md`, `audit-s5c-9.md` and `audit-s5c-10.md` (A7-9; P5-4).
- **Current documents citing the deleted file** are repointed to `schemas/workshop_wire.fbs` (`PuzzleRecord`, marked `(authoring)`), with line numbers re-measured after the merge by the implementer: `EL-124-authored-gravity-preset.md:127`, `:128`; `EL-125-authored-atmosphere-preset.md:130`; `completion-ledger.md:305`, `:309`; `delivery-batches.md:361`. `delivery-batches.md:295` and `:314` are history and stay (A7-8).

Checked and unchanged:
- `engine/gpu/WorkshopWireFlatBuffers.Observation.cs` arm switches (`:79-160`, `:199-215`) map domain values to generated types (coordinator decision 38).
- `engine/gpu/WorkshopAuthoringFlatBuffers.cs:230-231` (`KnotsPerProfile`, `AssistanceKnotCount`) is a domain rule; its schema comment (`workshop_authoring.fbs:23-24`) moves with the merge.
- `tools/WireContract/` and `tools/LifecycleContract/` are separate JSON design fixtures.
- `BundleElementIdTests.cs:213` names two fields in an assertion, not a layout. `HalfPrecisionGuardTests.cs:53` and `TypedTransportCommandTests.cs:397` already name `schemas/workshop_wire.fbs`.

**Existing coverage of the C# conversion and `ValidateSaveFrame`** (row S3 is S5c-2's):
- `WireFaultTests.AnotherRootIsAnUnknownIdentifier` (each root, saves included, decoded with another root's identifier) and `WireFaultTests.DamagedBufferIsATypedFaultNamingItsReason` (the WrongIdentifier case for every message, Save included).
- Every save round trip, for example `WorkshopSaveTests.CanonicalBitsAndAllocatorRoundTripWithoutLiveState`: the generated finisher writes the `file_identifier`, and `ValidateSaveFrame` admits it only if it equals the converted member, which proves the byte order.
- `WorkshopSaveTests.UnknownHeaderCannotBeLoaded` and `ClutchGeneralityTests.ObsoleteRuntimeBindingFramesAndUndefinedCurrentFieldsRefuseAtomically` (foreign identifiers refused). Limit: `"XXXX"` is byte-symmetric, so it proves the conversion is total, not that it is little-endian.

### Hand-restatement inventory (S5c-1 rows)

Measured at `51d46f7b` and re-read at `39aaba70` (audit 6). The full table, with the S5c-2 and S5c-3 rows and the rows owned elsewhere, is in the S5c-3 contract.

| Row | Where | What it restates | S5c-1 action |
|---|---|---|---|
| H-C1 | `engine/gpu/WorkshopWireFlatBuffers.cs:45-53`, `:58-91` | Command and response field counts (12, 41), scene ordinals (11, 27), presentation field 29, track and binding counts (17, 20), field 5 | Deleted; verifier rule |
| H-C2 | `engine/gpu/WorkshopAuthoringFlatBuffers.cs:34-102` | Puzzle vtable bounds (10, 14), saved-construction and instances slots, `InstanceRecordFields = 7` | Deleted; verifier rule plus `(required)` |
| H-C3 | `engine/gpu/WorkshopWireFlatBuffers.cs:18-19`, `WorkshopWireFlatBuffers.Observation.cs:11`, `WorkshopSaveCodec.cs:17`, `CuriousContraptions.Core/CoreCommandCodec.cs:9`; tests: `F32SaveBoundaryTests.cs:12` | Root identifiers `CWCM`, `CWRM`, `CCWO`, `CCWS` | Derived from `WireRootIdentifier` |
| H-C4 | `engine/gpu/WorkshopSaveCodec.cs:52-54` | The instance-record field bound, through H-C2 | Deleted |
| H-T1 | `CuriousContraptions.tests/WorkshopFormatTests.cs:96-123` | Field lists of eight records and the settings creator's arity | Deleted (it reads the deleted file); cover under Protections |
| H-T4 | `DeclaredOrientationTests.cs:137-138`, `:148-151`, `:157-181` | A slot comment, a historical layout, and a walk calling the deleted `HoldsFieldBeyondTheInstanceTable` | Comment deleted; walk deleted, test renamed (owned scope); historical layout kept, marked by S5c-3 |

### Capability probe

No solver law, worker message content or shared presentation contract changes. The wire ABI changes only by declaration (the identifier enum and the authoring mark); no encoded field moves.
- **Present:** the pinned compiler and C# generation (S5; `WorkshopWireBuildTests`); TypeScript generation (S5b; `WorkshopWireTypeScriptTests`); the C# verifier and frame boundary (`WorkshopWireFlatBuffers.cs:25-56`; `WireFaultTests`); the `csc` replay (`CSharpStaticGateTests.MatchingCompilations`; `GameFloatGuardTests`).
- **Missing, added here:** the one-file schema; root identifiers as schema data; a derived unknown-field rule for every table; schema-level enforcement of the worker boundary.
- **Missing, owned elsewhere:** cross-language conformance (S5c-2); a TypeScript verifier (H-R1, `ENGINE-WIRE-ABI`). No required-now owner decision is open.

### Decisions

- **D1, file name.** The merged file keeps the name `schemas/workshop_wire.fbs`: "wire" is the ABI and most readers already name it. The authoring declarations follow the wire declarations and use only earlier wire structs (audit 1).
- **D2, identifiers (24p ruling (b)).** `WireRootIdentifier : uint32`, member name = root table, value = the identifier bytes read little-endian; the one `file_identifier` must equal its member. Rejected alternatives: run log E4.
- **D5, refusal reason.** An unknown field is a verifier refusal: wire refusals become `VerifierRejected` (were `OutOfRange`, `WorkshopWireFlatBuffers.cs:51`, `:73`, `:78`), and so does the save's (`WorkshopSaveCodec.cs:53`). Measured at dispatch and disposed: tests expecting `OutOfRange` for an undefined field (among them `DeclaredOrientationTests.ACurrentSaveRefusesTheFirstUnknownInstanceFieldAndItsValidControlLoads`, `:140`); committed fixtures whose reason may change (`CuriousContraptions.tests/Fixtures/pre-launch/`, `pre-source-store/`, `save-f1ad3bd1-authored-puzzle.bin`); `CompoundMassAndRollingTests.cs:898-900`, whose "the vendored verifier deliberately permits unknown fields" inverts; and every hit of `rg -n "HoldsFieldBeyondTheInstanceTable|SavedConstructionSlot|ConstructionInstancesSlot|InstanceRecordFields|ValidateBindingShapes|TableTarget|ValidateSaveFrame"`.
- **D7, the save frame check stays behind `ICommandCodec` (audits 2 and 3).** The main assembly reaches Core only through Abstractions interfaces (`engine/RuntimeProviders.cs:13-19`), and the coupling gate (`tools/anvil/VerifyPolicy.cs`, rule family E9) refuses a main-to-Core model dependency; so only the identifier properties leave, and S10 admits exactly the one `SavedConstructionVerify` site. Alternatives: run log E4.
- **D9, S5b's TypeScript generation stays in the vendored project until S5c-2, on the one schema file (coordinator ruling on FO1-3, 26 November 2026).** `OrderWireSchemas`, `ListWireSchemas` and the glob are deleted here, because a multi-file path beside S1 is a second rule; S5c-2 moves generation into the Abstractions chain with its replacement.

### Protections this change removes, and what replaces them

- **The include direction** is replaced by S9 and S10.
- **The hand field counts on 6 tables** are replaced by the verifier rule on every table (S8 and the direct verifier tests).
- **`TheVersionlessRecordsDeclareExactlyTheirFields`.** Its version-field intent is covered by `NoWireOrSaveRecordDeclaresAFormatVersion` (`WorkshopFormatTests.cs:20-38`); its "any re-added field" intent is withdrawn as a hand restatement under 24p. **Evidence limit:** its slot-drift cover between the languages (S4, S5, S14) arrives with S5c-2 and S5c-3; until then both bindings are generated from the one file, but no test compares them.
- **Residuals:** no TypeScript verifier (H-R1); S9 does not see inside opaque byte vectors (H-W3); TypeScript identifier literals remain until S5c-2 and S5c-3.

### Concurrency

- S5c-1 touches no file under `tools/E2eRunner/`, `tools/E2eRunner.Tests/` or `tools/e2e/`, so it is disjoint from lane 3's E2E runner process lifecycle slice (owner decision 24m). The runner files A6-2 names (`Toolchain.cs`, `SourceIdentity.cs`, `IdentityTests.cs`, `TypeCheckBoundaryTests.cs`) are S5c-2's, which starts from a main that contains lane 3's slice (coordinator ruling, 26 November 2026).
- Shared with S5 and S5b: the vendored project file (header and the FO1-3 changes), `WorkshopWireBuildTests.cs`, `CSharpStaticGateTests.cs` and `schemas/README.md`.
- Shared documents (owner decision 24m): `TODO.md`, `deferred-work.md`, `delivery-batches.md`, `completion-ledger.md` and `sprint-status.yaml`; line-local edits only.
- The test files above overlap lane 1's collider repairs only if those repairs edit the same tests; checked at dispatch, and an overlap serialises the two slices.
- New files are staged with `git add` before the candidate commit.

## Tasks & Acceptance

**Execution:**
1. Hard gate, in `.tools/s5c-evidence/`, with the pinned cached compiler. If any check fails, stop and return to planning.
   - **(a) Merge.** Merge the files in a scratch copy and run `flatc --csharp` and plain `flatc --ts` (as S5b runs it) on it. Record that both succeed, that the C# types equal the baseline's generated set plus `WireRootIdentifier`, that the TypeScript entry module re-exports every declaration, and each generated companion of a marked declaration (S10's forbidden set).
   - **(b) Verify coverage.** Repeat audit 1's check that every generated `Verify` checks every field, with the last slot at `4 + 2 * (n - 1)`.
   - **(c) Replay inputs.** From the binary logs of a baseline round, confirm that each replayed `csc` command line carries its references, defines and analyzer references, which S10's semantic model and generator run need.
2. **Schema:** the merge, `WireRootIdentifier`, the `authoring` marks and the deletions. Row S1.
3. **Vendored verifier:** the derived rule, the notices, the pins and `VendoredFlatBuffersVerifierTests`. Row S8.
4. **C# codecs:** H-C1 to H-C4, identifiers from the enum, D5 and D7. Row S8 (`WireDecodeTests`).
5. **Reader and guards:** `WireSchemaText`, `WireSchemaLayout`, `WireSchemaTextTests`, and `WireSchemaGuards` for S1, S9 and S10; the existing readers moved onto it.
6. **Repairs and records:** the broken tests listed in the Code Map, the build-test retargets, the README sections and the planning records; close S5c-1 with its numbers.

**Acceptance Criteria:**
- AC1: Given the candidate, when `git ls-files schemas` is listed, then it shows only `schemas/README.md` and `schemas/workshop_wire.fbs`, and S1 passes.
- AC2: Given the full local round, when it completes, then failures are 0 and skipped are 0 in every project; the total is the baseline (7429) plus the new tests minus the deleted ones, with the delta measured at dispatch; and the run log names each added and deleted test with the row or test that covers it.
- AC3: Given the inventory rows H-C1 to H-C4, H-T1 and H-T4, when the slice closes, then `rg -n "\b(ValidateBindingShapes|TableTarget|CurrentPuzzleTable|InstanceRecordFields|SavedConstructionSlot|ConstructionInstancesSlot|HoldsFieldBeyondTheInstanceTable|CommandIdentifier|ResponseIdentifier|ObservationIdentifier|TheVersionlessRecordsDeclareExactlyTheirFields|CurrentRecordsHoldNothingBeyondTheTableAndTheWalkReachesTheGeneratedInstances)\b|WorkshopAuthoringFlatBuffers\.OpenFrame|WorkshopSaveCodec\.Identifier|workshop_authoring" --glob '*.cs' --glob '*.ts' --glob '*.fbs' --glob '*.csproj' --glob '*.props' --glob '*.targets' --glob '!_bmad-output/**' --glob '!.tools/**' --glob '!tools/e2e/unit/run-reset-snapshot.test.ts'` prints nothing (A7-7), and S5b's TypeScript generation, `WorkshopWireTypeScriptTests`, the TypeScript cases of `WorkshopWireBuildTests` and the e2e runner's tests pass with one schema file and no `include`.
- AC4: Given the maintainability gate, when it runs, then both canaries pass and violations are 0.

## Spec Change Log

- 25 November 2026, planning audits 1 to 4 (`docs/planning/elements/readiness/audits/audit-s5c-1.md` to `-4.md`, Fail) and the bounded fifth check (`audit-s5c-5.md`, Scoped Pass): A1-1 to A5-3 repaired (run log E4).
- 26 November 2026, dispatch re-audit (`audit-s5c-6.md`, Fail: A6-1 and A6-2 medium, A6-3 to A6-11 low). A6-9 applied (run log E2). Here: A6-3 (j) and (k), A6-5, A6-6, A6-8's `WireSchemaText`, S9 and S10 bullets, A6-11; A6-2 by sequencing. The rest is in S5c-2 and S5c-3 (run log E4).
- 26 November 2026, split into S5c-1, S5c-2 and S5c-3 (coordinator ruling; A6-10). This contract is S5c-1; the moved text and the mapping are in run log E4.
- 26 November 2026, bounded re-audit (`audit-s5c-7.md`, Fail: A7-1 and A7-2 medium, A7-3 to A7-9 low): all nine repaired here; A7-1's closed sets narrowed to the measured inventory (coordinator instruction; run log E5).
- 26 November 2026, bounded check (`audit-s5c-8.md`, **Scoped Pass**; the narrowing accepted): A8-1 to A8-4 (low) repaired here (run log E6).
- 26 November 2026, review pass 1 FO1-3 (high, intent gap): coordinator ruling, option A. Amended Always `:59`, `:67`, the `WorkshopWireBuildTests.cs` bullet and D9: both generation steps pass exactly `schemas/workshop_wire.fbs`; `OrderWireSchemas`' `-M` parsing and include ordering and the two multi-file build cases are deleted (S1 covers them). Known-bad state avoided: a build that admits multi-file sets beside S1's one-file rule. KEEP: everything else in the pass-1 candidate (run log E7), S5b's TypeScript generation in the vendored project (Debug-only, `schemas.txt` with one name), no runner file. Run log E9.
- 26 November 2026, bounded check of the FO1-3 amendment (`audit-s5c-9.md`, Fail: A9-1 to A9-5 medium, A9-6 to A9-10 low): all repaired here; `OrderWireSchemas` and `ListWireSchemas` are deleted whole, the stray-output case is kept on the one file, Never and owned scope state the FO1-3 changes. Run log E9.
- 26 November 2026, repair acceptances (run log E11): the C# generation stamp as `GenerateWireTypeScript`'s Input in place of the deleted list stamp (Never and the vendored-project bullet name it); the `schemas/README.md` sentences that described `OrderWireSchemas` are owned and reworded here (README bullet).
- 26 November 2026, review pass 5 P5-1: intent row S9's description and Error Handling corrected to the closure rule that review pass 4 (BH4-1) made the one S9 rule; same guarantee (no runtime root reaches a marked declaration), stronger check. Coordinator correction, run log E15; ratified by the top-level coordinator as a coordinator ruling, 27 November 2026, beside the FO1-3 ruling (run log E17).
- 27 November 2026, top-level coordinator rulings after review pass 6 (run log E17): the parser review oracle and its routing (Verification, Acceptance-to-test table preface); `WireDecodeTests.AddOne` becomes a switch expression if that compiles under warnings-as-errors with no fallback arm, otherwise it keeps the `UnreachableException` arm with a corrected comment (outcome recorded in E17b).

## Review Triage Log

Full rows of passes 1 to 6: run log E18 (contract budget).

- **2026-11-26 — Review pass 1 (E8, 16 rows):** high 3, medium 0, low 7, false 6; FO1-3 ruled option A (E9), 6 patched (E10), 3 deferred to S5c-2/S5c-3 (`deferred:`).
- **2026-11-26 — Review pass 2 (E11, 9 rows):** high 1, medium 0, low 7, false 1; FO2-1 and 6 patched (E12), runner/TS items deferred to S5c-2.
- **2026-11-26 — Review pass 3 (E15, 7 rows):** high 0, medium 0, low 7; repaired in E13, E14.
- **2026-11-26 — Review pass 4 (E15, 2 rows):** high 0, medium 0, low 2; BH4-1 and records repaired in E14; BH4-2 deferred to S5c-2.
- **2026-11-26 — Review pass 5 (E15, 5 rows):** high 0, medium 0, low 5; P5-1 to P5-4 repaired (E15, E15b), P5-5 deferred to S5c-2. Final diff scope (`a88dd6a445869024f`) Incomplete pending P5-1 to P5-4.
- **2026-11-26 — Review pass 6 (E16, 4 rows + 1 intent_gap):** held on EFE6-F6-1 (S9 intent row) and parser narrowing; resolved by 27 Nov coordinator rulings (E17) and repaired in E17b.
- **2026-11-27 — Review pass 7 (E18, `final8`):** 0 findings — Blind Hunter (`e490dbb2`), Verification Gap (`fb8e4777`), Forward-Only (`37f5095f`), Element-Free Engine (`fa6f33a1`, **Scoped Pass**). Final diff scope (`fa6f33a1`): **Pass**.

## Design Notes

The eight design-record questions (`_bmad/custom/element-free-engine-design.md`, section 6):

1. **Delivered:** one declaration of the FlatBuffers ABI, with roots and identifiers as schema data and unknown fields refused at every table. No gameplay, solver or presentation behaviour changes.
2. **Reusable:** one verifier rule for every table; one C# identifier conversion.
3. **Declared:** the schema file only; authoring-only declarations carry a schema mark.
4. **Execution:** schema → `flatc` → compile → frame check → C# verifier → decode once.
5. **Ownership:** the main assembly owns the authoring codecs and atomic save refusal; Core owns the frame boundary and runtime codecs, reached for saves only through `ICommandCodec.ValidateSaveFrame` (D7).
6. **Generality:** no element identity, capability kind or table name is special-cased; slots come from declaration order, checked against the generated `Add*` counts.
7. **Deleted:** `workshop_authoring.fbs` and its `include`; the codecs' hand counts and slots; the C# identifier constants and the three `ICommandCodec` identifier properties; `TheVersionlessRecordsDeclareExactlyTheirFields`; the H-T4 walk.
8. **Uncertain:** task 1's gates; D5's measured list; H-R1 (`ENGINE-WIRE-ABI`). Nothing required now is open.

Element-free rules D1 (roots by enum member), D4 (refuse before decode) and D5 (one current representation) apply; D6 and D7 are unaffected. **Architecture guards:** S1, S9 and S10 read source, schema text or logs (`docs/testing-guidance.md`, "Architecture guards"); they live in `WireSchemaGuards`, each with a positive control.

## Verification

Executor: the implementation owner.

Environment: `PATH=~/.local/share/tim-toolchain/dotnet-10.0.403:~/.local/share/tim-toolchain/node-v26.12.1-darwin-arm64/bin:$PATH` and `DOTNET_ROOT=~/.local/share/tim-toolchain/dotnet-10.0.403`. Record the session environment check in the run log.

**Commands:**
- The five compiler-log capture commands (`docs/delivery-workflow.md:38-53`). Expected: every build succeeds and generates from the one file.
- `dotnet run --file tools/anvil/VerifyPolicy.cs -- --maintainability --project-root .` Expected: AC4.
- The registration check filter. Expected: 0 failed, 0 skipped (test files are added).
- The scoped run of S5c-1's test classes, with `CSHARP_STATIC_GATE_BINLOGS` set. Expected: 0 failed, 0 skipped.

  `dotnet test CuriousContraptions.slnx -c Debug -f net10.0 --no-build --no-restore --nologo -noAutoResponse --filter "FullyQualifiedName~WireDecodeTests|FullyQualifiedName~WireSchemaGuards|FullyQualifiedName~WireSchemaTextTests|FullyQualifiedName~VendoredFlatBuffersVerifierTests|FullyQualifiedName~VendoredFlatBuffersIdentityTests|FullyQualifiedName~CSharpStaticGateTests|FullyQualifiedName~WorkshopWireBuildTests|FullyQualifiedName~WorkshopWireTypeScriptTests|FullyQualifiedName~WorkshopFormatTests|FullyQualifiedName~WorkshopSaveRejectionTests|FullyQualifiedName~WorkshopSaveTests|FullyQualifiedName~WireFaultTests|FullyQualifiedName~F32SaveBoundaryTests|FullyQualifiedName~GoalSignalSelectorTests|FullyQualifiedName~BundleElementIdTests|FullyQualifiedName~DeclaredOrientationTests|FullyQualifiedName~CompoundMassAndRollingTests|FullyQualifiedName~ClutchGeneralityTests|FullyQualifiedName~WorkshopWireFlatBuffersTests"`
- `git add -N` on the new files, then `git diff --check 39aaba70`. Expected: clean.
- One full local round. Expected: AC2.

### Acceptance-to-test table

No tolerance applies: every assertion is a name, a count, a presence or a typed refusal.

**Parser review oracle (coordinator ruling, 27 November 2026).** The grammar is closed by construction: anything it does not accept is refused. `OnlyTheClosedGrammarIsAccepted` has exactly one refusal row per refusal this contract names (Always, "One schema reader": `/*`, `include`, `union`, `rpc_service`, `file_extension`, each named attribute, fixed arrays, a second namespace, an unresolved type name, a scalar outside the list, an attribute on an enum, a non-integer enum type, a default outside the grammar), and S1 has one wrong-underlying-type `WireRootIdentifier` control; that list is the fixed review oracle. Routing: the parser accepting a form outside the grammar is a defect fixed in this slice; a grammar form without its own row that the parser already refuses is not a defect; a request to extend the grammar goes to `ENGINE-WIRE-ABI` (the grammar-extension owner) unless it breaks a hard rule.

| Rows | Named discriminating test | Control and boundary | Execution |
|---|---|---|---|
| S1 | `WireSchemaGuards.TheSchemaIsOneFileWithOneRootTypeAndNoInclude` | Control: a synthetic `schemas/` with a second `.fbs`, and one with an `include`, are each refused, naming them. Boundary: the real `schemas/`, which also holds `README.md`, is admitted; a synthetic schema whose `WireRootIdentifier` names a member that is not a declared table is refused, naming it; a `WireRootIdentifier` of another underlying type (`: uint8`) is refused ("declares no enum WireRootIdentifier : uint32") | passed, `final8` (run log E17b, E18) |
| S8 | `WireDecodeTests.AnAppendedFieldIsRefusedAtEveryRootInANestedTableAndInASave` | Control: a table with its last schema field set is admitted. Boundary: one extra zero vtable entry is refused | passed, `final8` (run log E17b, E18) |
| S8 (verifier) | `VendoredFlatBuffersVerifierTests`, testing `Verifier` directly with generated `<Name>Verify.Verify` actions of small schema tables, slots from `WireSchemaLayout`, no codec | Positive: a vtable of exactly the table's fields, and a shorter one (trailing absent fields), are admitted. Control: one appended non-zero entry, and one appended zero entry, are refused. Boundary: a child table with an appended field inside an exact parent is refused; a parent whose appended field follows a fully verified child is refused (the stack does not let the child's record stand for the parent); a table reached through a vector of tables is refused; one `Verifier` reused for an admitted buffer after a refused one admits it. `CompoundMassAndRollingTests.cs:898-900`'s inversion stays | passed, `final8` (run log E14, E18) |
| S9 | `WireSchemaGuards.NoRuntimeRootReachesAnAuthoringDeclaration`, one rule closed under reference: every declaration that references a declaration marked `authoring` (a field's type or a vector's element type) is itself marked, and every `WireRootIdentifier` runtime root is unmarked | Control: a runtime root whose field names a marked table (`WorkshopCommand.scene: ConstructionRecord`), an unmarked table holding a marked field (`Holder.record: InstanceRecord`), and one holding a vector of them (`Holder.records: [InstanceRecord]`), are each refused, naming `Declaration.field`. Boundary: the real file, whose runtime roots and marked declarations share the unmarked struct `Vec3f` (`WorkshopResponse.bodies` → `BodyRead.local`; `InstanceRecord.local`), is admitted | passed, `final8` (run log E14, E18) |
| S10 | `WireSchemaGuards.NoWorkerCompileReferencesAnAuthoringDeclaration` (semantic model of each replayed compile) | Control: each synthetic compile is refused: a Simulation source with a variable of type `InstanceRecord`; a second Core site naming `SavedConstructionVerify`; a Core source naming `InstanceRecordVerify`; a source reaching a marked type through a `using` alias; a compile whose model reports an error; a marked type bound without being named (`UnnamedTargetTypedCreation`, `UnnamedLambdaParameter`, `ContainingTypeArgument`, `UnnamedFunctionPointer`, `UnnamedFunctionPointerReturn`); the admitted text in a Simulation compile (`AdmittedSiteOutsideCore`). Boundary: the one admitted reference and `WireRootIdentifier.SavedConstruction` are admitted | passed, `final8` (run log E14, E18) |
| — | `WireSchemaTextTests.EveryTableFieldCountEqualsItsGeneratedAddMethods` | Control: a synthetic schema text with one field more than its generated type is refused, naming the table | passed, `final8` (run log E14, E18) |
| — | `WireSchemaTextTests.OnlyTheClosedGrammarIsAccepted` | Control: one synthetic text per refusal the "One schema reader" bullets name (the parser review oracle), each refused with its exact line: `/*`, `include`, `union`, `rpc_service`, `file_extension`, the attributes `deprecated`, `id:`, `key`, `nested_flatbuffer`, `force_align` and `bit_flags`, a fixed array `[float:3]`, a second namespace, a scalar outside the list (`int16`, which is also the unresolved-type-name row: one refusal), an attribute on an enum (`authoring`), an enum over `float`, and a table default outside the grammar (`= 5`). Boundary: the real file is admitted; an enum whose members have no values is admitted, with values 0, 1, … | passed, `final8` (run log E17b, E18) |
| C# identifiers | Existing: `WireFaultTests.AnotherRootIsAnUnknownIdentifier`, `WireFaultTests.DamagedBufferIsATypedFaultNamingItsReason`, `WorkshopSaveTests.CanonicalBitsAndAllocatorRoundTripWithoutLiveState`, `WorkshopSaveTests.UnknownHeaderCannotBeLoaded` | Their controls (an undamaged buffer decodes; a foreign identifier is refused) | passed, `final8` (run log E14, E18) |
| FO1-3 | `WorkshopWireBuildTests` (one-file cases) and the kept stray-output case | Control: the clean copy builds a complete module set from the one file. Boundary: a second namespace is refused with the previous output unchanged | passed, `final8` (run log E14, E18) |
| AC1 to AC4 | `git ls-files`, the full round, the maintainability gate | The baseline round is recorded at the start | passed, `final8` (run log E18) |

## Auto Run Result

- **Summary.** `schemas/workshop_wire.fbs` is the single FlatBuffers schema (`schemas/workshop_authoring.fbs` deleted); `WireRootIdentifier : uint32` and `(authoring)` are schema data; `FlatBufferVerify`/`Table` derive unknown-field refusal across all 79 tables; C# codecs and `WorkshopSaveCodec` use generated types/verifier; `OrderWireSchemas` and `ListWireSchemas` deleted (FO1-3); `WireSchemaText` and `WireSchemaGuards` enforce S1, S9, S10, and the closed-grammar oracle. Run log E7–E18.
- **Files.** 71 owned paths on baseline `39aaba70` (Final diff scope **Pass**, `fa6f33a1`, run log E18).
- **Review.** Engine tier, 7 passes (16, 9, 7, 2, 5, 4+1, 0 rows); Forward-Only clean on pass 7; `followup_review_recommended: false` (pass 7 patched 0 findings).
- **Verification.** `final8` (run log E18): 5 capture builds 0 warnings/errors; maintainability gate PASS (`violations=0`, both canaries `PASS`, `Coupling dependencies: contract=4627, model=4, intrusive=0`); registration 828/0/0; full local round **7457/0/0, `Skipped: 0`** (17 + 329 + 132 + 260 + 6719).
- **Residual risks.** Cross-language conformance and TypeScript hand-restatement cleanup belong to S5c-2 and S5c-3; browser qualification remains deferred in `BrowserDeferral.cs`.
