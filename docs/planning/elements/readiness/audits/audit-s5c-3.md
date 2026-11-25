# Planning audit: `spec-s5c-one-wire-schema-file.md` (S5c), third pass

Audited 25 November 2026, read-only, planning phase. Checkout `.claude/worktrees/plan-s5c-one-wire-schema` (branch `plan-s5c-one-wire-schema`, HEAD `51d46f7b`, uncommitted planning edits: the untracked contract and audits 1 and 2, and eight edited planning documents). The S5b candidate in `.claude/worktrees/slice-s5b` was read for context only. Policy was read from this checkout: `AGENTS.md` (forward-only review gate, testing rules), owner decision 24p and coordinator rulings (a) to (f) (`owner-decisions-2026-11-22.md`), `_bmad/custom/element-free-engine-design.md`, `_bmad/custom/element-free-engine-review.md`, `docs/testing-guidance.md`, and the maintainability and coupling gate (`tools/anvil/VerifyPolicy.cs`, rule family E9), which AC4 invokes.

Scope:
- the repaired contract, all sections;
- the repairs of audit 2 findings A2-1 to A2-10;
- the planning-document diff (`git diff`): the C36 rewrite in `engine-stories-index.md`; `delivery-batches.md` §0, S5b and S5c; `epics.md` ENGINE-WIRE-ABI and ENGINE-CAPABILITY-INSTANCES; 24p; TODO, sprint key and OS-01.

Every `path:line` that the audit-2 repairs added or changed was re-read at `51d46f7b`. Every implementer and caller touched by D7 was measured with `rg`. No probe was compiled and no gate was run in this pass.

## Verdict: Fail

Nine audit 2 findings are closed and one is partly closed. One new high finding remains open:
- **A3-1.** D7's replacement path is unimplementable under AC4. The main assembly calling a public member of `CuriousContraptions.Core` is model coupling to a volatile engine project, which the coupling gate refuses. That rule is the reason `ValidateSaveFrame` sits on the Abstractions interface today.

The other new findings are low.

## Audit 2 dispositions

| Finding | Disposition | Evidence |
|---|---|---|
| A2-1 (medium) D7 needs the reserved Abstractions-to-FlatBuffers reference | **Partly closed; reopened as A3-1 (high)** | The reserved reference is avoided: D7 says "Abstractions gains no project reference". The interface part is feasible: `ICommandCodec` (`RuntimeCodecs.cs:7-21`) has one implementer, `CoreCommandCodec` (`CoreCommandCodec.cs:7`), and no test fake (`rg ICommandCodec`). No consumer reads `CommandIdentifier`, `ResponseIdentifier` or `ObservationIdentifier` through the interface. `ValidateSaveFrame`'s one caller is `WorkshopAuthoringFlatBuffers.cs:36`. The replacement does not work: the main assembly would call Core's `WorkshopWireFlatBuffers.OpenFrame` and Core's conversion directly, and the coupling gate fails that (A3-1). Audit 2's repair recommended this path and missed the gate. |
| A2-2 (medium) hand-walk test, whole-file allow-list, inverting assertion | Closed (low residual A3-3) | H-T4 now reads as the code does: `DeclaredOrientationTests.cs:137-138` is the slot comment; `:148-151` is `OldLayoutSaves.Layout(9, 7, 16, 2)` and its two refusals; `:169-180` lies inside `CurrentRecordsHoldNothingBeyondTheTableAndTheWalkReachesTheGeneratedInstances` and calls `HoldsFieldBeyondTheInstanceTable` and the slot constants. That control is deleted, with its cover named. S11 and H-T3 now limit the allow-list to marked regions. `CompoundMassAndRollingTests.cs:898-900` is in D5, and it reads as cited (comment `:898-899`, `Assert.True(...VerifyBuffer...)` `:900`). The dispatch `rg` now names the deleted symbols. |
| A2-3 (medium) S10 blind to companions | Closed, but depends on A3-1 | S10 forbids `<Name>Verify` and the other emitted companions, and uses a semantic model built from the replayed `csc` command lines. That is feasible: `CSharpStaticGateTests.MatchingCompilations()` (`:371-380`) already builds a `CSharpCompilation` from each replayed run's sources, references and parse options, and the test project references Roslyn (`CuriousContraptions.tests.csproj:41-42`). The controls include `SavedConstructionVerify.Verify` in Core and a `using` alias. Today the only Core source that names an authoring type or companion is `CoreCommandCodec.cs:9` (`rg` over every `CoreRuntimeSource`). The A3-1 repair may need to admit that one reference; if so, S10 and its control change with it. |
| A2-4 (low) stamp as Input and Output | Closed (low residual A3-4) | A separate target writes a `WriteOnlyWhenDifferent` stamp registered in `FileWrites`. The stamp is an Input only, and the entry module is the only Output. |
| A2-5 (low) concurrency wording | Closed | D3 and Always now rest on the one-writer ordering, recorded by task 1(c). Task 1(c) also has to establish exactly one generation per local graph, `web.binlog` included. If it cannot, it returns to planning. |
| A2-6 (low) static-gate pins | Closed (low residual A3-5) | The owned scope names `:116`, `PinnedTargets` (`:1335-1364`) and `PinnedImports` (`:1368-1377`); all read as cited. |
| A2-7 (low) S5b build tests | Closed (low residual A3-6) | Each S5b case is disposed: deleted, merged into S13, moved to the Abstractions-copy fixture, or its assertions removed. All the named tests exist in `slice-s5b` `WorkshopWireBuildTests.cs` (`:76`, `:113`, `:180`, `:205`, `:271`, `:291`, `:306`, `:325`) and `WorkshopWireTypeScriptTests.cs` (`:28`, `:270`, `:361`). The Release rationale is corrected. |
| A2-8 (low) comments and the mixed theory | Closed (low residual A3-7) | S11 scans comments. Four comments are disposed: `SaveFixture.cs:7`, `WorkshopSaveCodec.cs:13`, `workshop-driver.ts:145` and `:935`, all read as cited at `51d46f7b`. The theory at `WorkshopSaveTests.cs:398-401` takes a `uint` parameter. That compiles, because an enum cast is a constant expression, and tests are outside the coupling gate. |
| A2-9 (low) planning documents | Closed (low residual A3-8) | `delivery-batches.md` S5c says "rows S1 to S14". S5b's "Remains after it" sends the wire enums to `ENGINE-WIRE-ABI`. C36 now states one rule ("every closed set has exactly one declaration"), consistent with `epics.md` ENGINE-WIRE-ABI's Enums bullet and ENGINE-CAPABILITY-INSTANCES' enum sentence. |
| A2-10 (low) completeness | Closed | `SourceIdentity.cs` is in the deferred-runner scope. `WireSchemaTextTests.EveryTableFieldCountEqualsItsGeneratedAddMethods` is named and in the filter. `TypeCheckBoundaryTests` is in the filter, and `tools/E2eRunner.Tests` is in `CuriousContraptions.slnx:90`. D8 names `WorkshopCompiledScene` as the class that compares by reference. |

Citations checked in this pass and correct at `51d46f7b`:
- `RuntimeCodecs.cs:9`, `:15-16`, `:19`.
- `CoreCommandCodec.cs:9`, `:15-16`, `:19`.
- `WorkshopWireFlatBuffers.cs:18-19`, `:25` (internal), `:25-56`, `:45-53`, `:58-91`, `:104-109`.
- `WorkshopAuthoringFlatBuffers.cs:34-74`, `:36`, `:76-102`.
- `WorkshopSaveCodec.cs:13`, `:17`, `:29`, `:48-55`, `:52-54`, `:59-69`, `:61`.
- `DeclaredOrientationTests.cs:137-138`, `:148-151`, `:169-180`.
- `CompoundMassAndRollingTests.cs:898-900`.
- `CSharpStaticGateTests.cs:116`, `:1335-1364`, `:1368-1377`.
- `SaveFixture.cs:7`, `:22`.
- `tools/e2e/workshop-driver.ts:145`, `:935` (at `51d46f7b`; S5b moves them, and the contract re-reads every citation at dispatch).
- `WorkshopSaveTests.cs:398-401`.
- `FlatBufferVerify.cs:445-458`, `:462-466`.
- `VendoredFlatBuffersIdentityTests.cs:5-12`, `:23`.

The `WireRootIdentifier` values are correct: each is its four identifier bytes read little-endian, and they ascend. `0x53574343` also matches the retired magic word at `WorkshopFormatTests.cs:176`.

## New findings

### A3-1 (high) D7's direct call from the main assembly into Core fails AC4's coupling gate; the save path needs an Abstractions-typed route

Evidence:
- D7 and the Code Map (`RuntimeCodecs.cs` bullet) delete `ValidateSaveFrame` from `ICommandCodec`. They make `WorkshopWireFlatBuffers.OpenFrame` and the member-to-identifier conversion public in Core, "and the main assembly's save codec calls them directly". Always puts the C# conversion "beside Core's frame boundary". The main assembly also needs an identifier to encode: `WorkshopSaveCodec.cs:29` calls `FinishSizePrefixed(..., Identifier)`.
- `WorkshopWireFlatBuffers.cs` and `.Observation.cs` compile only into Core (`RuntimeSources.props:145-146`). `CuriousContraptions.csproj` compiles `engine/gpu/*.cs` minus `@(CoreRuntimeSource)` (`:64-67`).
- The coupling gate (`VerifyPolicy.cs:1523-1537`, `:1556-1569`, `:1647-1654`, `:1710-1725`) works as follows:
  - It reads every type and member reference in the IL of each production project.
  - A reference to a public type of a non-contract project has `Strength.Model`.
  - `CuriousContraptions.csproj` and `CuriousContraptions.Core.csproj` are both `Kind.Engine`, which is volatile.
  - `Judge` returns `Rule.UnbalancedModel` for any Model dependency on a volatile project.
  - The source states that changing a declared kind to make a dependency pass "weakens the gate (E9: Fail)".
- AC4 requires that gate's "violations are 0". The rule family is an owner rule (6 November 2026).
- This is why the save frame check goes through `ICommandCodec` today. Main reaches Core only through Abstractions interfaces, and loads the provider by reflection (`engine/RuntimeProviders.cs:13-19`). I searched (`rg -w`) all 67 main-assembly sources (`engine/gpu/*.cs` minus `@(CoreRuntimeSource)`, `engine/*.cs`, `ui/*.cs`, `GameBootstrap.cs`) for the names of the 115 public types declared in Core-only sources. There are two hits, and neither leaves an IL reference: the reflection string at `RuntimeProviders.cs:17`, and a constant-folded enum member at `BrowserWorkshopClient.cs:115` (`(int)NativeClockProfile.IsolatedWitnessedPerformanceNow`). The commit that added `ValidateSaveFrame` (`a3b50d0d`) records the design rule ("no ByteBuffer/verifier delegate crosses General") and a gate result of "intrusive=0; violations=0". No gate run was observed in this pass.
- Consequence of the deletion as written: `WorkshopSaveCodec.Decode`, `DecodeStored` and `Open` take `ICommandCodec codec` (`:35`, `:48`, `:59`) only to call `ValidateSaveFrame`. After D7 that parameter is dead. The contract does not say whether it is deleted, which would change about 334 call sites in about 97 test files and `ui/Workshop.cs`, none of them listed. If it is kept unused, that is a retained parameter that serves only old callers, which the forward-only gate fails.

Repair. Choose one path and restate D7, the Code Map, Always (conversion location), S10 and Design Notes 5 to match.

- **(1) Keep the save frame check behind the Abstractions interface.**
  - Delete only the three identifier properties from `ICommandCodec`, and keep `ValidateSaveFrame(ReadOnlySpan<byte>)`. It names no FlatBuffers type.
  - Core's implementation passes `WireRootIdentifier.SavedConstruction` and `SavedConstructionVerify.Verify` to its one internal boundary.
  - Main encodes the save with the generated root finisher, `SavedConstruction.FinishSizePrefixedSavedConstructionBuffer`. The compiler emits it for the file's `root_type`; it was observed in the last committed generated source, `git show a41139a0^:engine/gpu/WorkshopAuthoringGenerated.cs:392`. It lives in the FlatBuffers wire contract, which the gate rates Contract strength. Amend "C# takes an identifier only from the generated `WireRootIdentifier`" to admit the generated root API, and keep S3's check that the `file_identifier` equals the member.
  - S10 then admits exactly one companion reference, `SavedConstructionVerify` in `CoreCommandCodec`. It is verification code that yields no authored instance. Record why. Its control becomes any other companion or accessor of a marked declaration in Core, or a second site naming `SavedConstructionVerify`.
  - `WorkshopSaveCodec`'s `codec` parameter stays used.
- **(2) A coordinator ruling** that Abstractions references `CuriousContraptions.FlatBuffers` in S5c, so that `ICommandCodec` can carry the enum and the verify delegate. This amends ruling (f)'s reservation to `ENGINE-WIRE-ABI` and D3's premise ("no build-order relation"). Record it in 24p.

Not acceptable:
- changing `CouplingGate.Declared` (E9 Fail);
- a second frame check or verifier invocation in the main assembly (forward-only: a second rule for the same behaviour);
- keeping a dead `codec` parameter.

### A3-2 (low) S11's test-table boundary still says "file"

Row S11 and H-T3 make the allow-list marked regions, "regions, not whole files". The acceptance-to-test table's S11 boundary still reads "an allow-listed historical-fixture file is admitted".

Repair: "a marked historical-fixture region is admitted, and a restatement outside its markers in the same file is refused".

### A3-3 (low) H-T4's deletion boundary

H-T4 cites `:169-180` and says "Control deleted". The test spans `DeclaredOrientationTests.cs:157-181`, and its first part (`:160-167`) asserts save round-trip equality and command owners. State whether the whole method is deleted and name the cover for `:166-167`, or keep those lines.

Also note in D5's list that `ACurrentSaveRefusesTheFirstUnknownInstanceFieldAndItsValidControlLoads` (`:140`) expects `OutOfRange`. The dispatch measurement catches it.

### A3-4 (low) The generation target's Inputs omit the file that declares it

Always lists the stamp, the schema and "the shared compiler targets file", which is the provisioning file. A change to the generation command itself (for example `--gen-object-api`) lives in the file that declares the generation target (`RuntimeTypeScript.targets` or wherever the target is placed). Such a change would not regenerate on an incremental build. S5b used `$(MSBuildProjectFullPath)` for this.

Repair: add `$(MSBuildThisFileFullPath)` of the declaring file to the Inputs.

### A3-5 (low) The shared provisioning file would be imported twice into the vendored project

`Directory.Build.targets` imports `tools/runtime/RuntimeTypeScript.targets` into every project (`PinnedImports`, `CSharpStaticGateTests.cs:1370`). That includes `CuriousContraptions.FlatBuffers`, whose folder has only `Directory.Build.props`. If `RuntimeTypeScript.targets` imports the shared file and the vendored project imports it as well, MSBuild warns MSB4011 and ignores the duplicate import.

Repair: import the shared file once, through the chain the vendored project already receives, or state why the explicit import is needed. Keep the `PinnedImports` entries consistent with that choice.

### A3-6 (low) S5's C# build tests that edit the deleted file

On this baseline, `WorkshopWireBuildTests.cs:61`, `:77` and `:112` edit, touch or delete `schemas/workshop_authoring.fbs`. The contract disposes `ADeletedSchemaFileLeavesNoGeneratedSource` (`:112`) and merges the accessor case into S13. The `:77` case (and any other S5 case naming the file after S5b) is not listed.

Repair: add these cases to the Code Map, retargeted to `workshop_wire.fbs`.

### A3-7 (low) Identifier literals in code strings not disposed

S11 forbids current identifiers outside the allow-list, in code or comments. Three are not disposed:
- `tools/e2e/run-reset-snapshot.ts:321`: an error message containing "CCWS", outside the deleted ranges;
- `tools/e2e/workshop-wire.ts:12799`: "a CCWS construction save ...", outside H-S3;
- `_bmad-output/implementation-artifacts/evidence-9r-0c/implementer/tscheck/tools/e2e/workshop-driver.ts:173`: a historical evidence copy.

Repair:
- Reword the first two to name the enum member.
- State S11's scanned roots. The inventory excluded `_bmad-output/` and `docs/`; S11 should exclude them too, or allow-list the evidence copy.
- Say that the C# conversion yields the `string` that the vendored `FinishSizePrefixed` and `VerifyBuffer` take (`FlatBufferBuilder.cs:1016`, `FlatBufferVerify.cs:781`), so that no call site converts a second time.

### A3-8 (low) C36's new sub-points are formatted as sibling entries

In `engine-stories-index.md`, C36's four new bullets ("Wire closed sets", "`ENGINE-WIRE-ABI`", "Every other closed set", "Until then") are not indented. They read as entries beside C35 and C37, not as parts of C36.

Repair: indent them under C36.

## Checks that hold

| Question | Result |
|---|---|
| Rulings (a) to (f) | Complied with, apart from the choice A3-1 forces. Repair path (2) of A3-1 would need ruling (f) amended. |
| Forward-only | No format version, alias, fallback decoder, second schema, second frame check or retained deprecated item in the plan as drafted. The open forward-only risk is the dead `codec` parameter (A3-1). |
| Testing guidance and hashes | Exact equality of decoded values is justified as a defined-interface invariant. No hashes, mutants, skips or browser gating. Guards are named, each with a positive control. Deleted tests name their cover. |
| Element-free design | No element identity, capability kind or table name selects behaviour. The eight design-record questions are answered. |
| Derived verifier rule | Sound. The generated `Verify` methods call `VerifyTableStart`, the field checks and `VerifyTableEnd` in sequence, and a nested table is checked inside its parent's field call, so a per-table stack is needed and sufficient. A short-circuited failure refuses the whole buffer. |
| S10 semantic model | Feasible with the existing `MatchingCompilations()` replay. |
| D3 one-writer and once-per-graph | Rests on task 1(c), a hard gate that returns to planning on failure. That is acceptable for planning. |
| Planning edits | TODO lane 2, the sprint key, `delivery-batches.md` §0, S5b and S5c, the engine-stories gate, row 8a and C36, `epics.md` (ENGINE-WIRE-ABI Contract, Enums, Deletes and Depends on; ENGINE-CAPABILITY-INSTANCES), `vertical-delivery.md`, OS-01 and 24p agree with the contract and with each other, apart from A3-8. |
