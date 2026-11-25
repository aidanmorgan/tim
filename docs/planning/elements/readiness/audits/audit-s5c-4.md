# Planning audit: `spec-s5c-one-wire-schema-file.md` (S5c), fourth pass

Audited 25 November 2026, read-only, planning phase. Checkout `.claude/worktrees/plan-s5c-one-wire-schema` (branch `plan-s5c-one-wire-schema`, HEAD `51d46f7b`, uncommitted planning edits: the untracked contract and audits 1 to 3, and eight edited planning documents). The S5b candidate in `.claude/worktrees/slice-s5b` was read for context only. Policy was read from this checkout: `AGENTS.md` (forward-only review gate, testing rules), owner decision 24p and coordinator rulings (a) to (f) (`owner-decisions-2026-11-22.md`), `_bmad/custom/element-free-engine-design.md`, `_bmad/custom/element-free-engine-review.md`, `docs/testing-guidance.md`, and the maintainability and coupling gate (`tools/anvil/VerifyPolicy.cs`, rule family E9), which AC4 invokes.

Scope:
- the repaired contract, all sections, with D7 checked against every place it is restated;
- the repairs of audit 3 findings A3-1 to A3-8;
- every `path:line` that those repairs added or changed, re-read at `51d46f7b`;
- the C36 sub-points in `engine-stories-index.md`.

One probe was run, in the ignored `.tools/s5c-audit4/`. A minimal schema, `probe.fbs`, was written after anvil validation. It holds `attribute "authoring"`, the four-member `WireRootIdentifier`, three runtime root tables and `SavedConstruction (authoring)`, and ends with `root_type SavedConstruction; file_identifier "CCWS";`. The pinned compiler (`flatc version 25.2.10`, from the per-user cache) ran `--csharp` and `--ts --gen-object-api` on it, writing into `.tools/s5c-audit4/cs/` and `ts/`. The maintainability gate was not run: run on today's tree, it says nothing about the post-change state. Only `probe.fbs` went through anvil validation. The compiler wrote its own output files under `.tools/s5c-audit4/`, and those were not validated one by one.

## Verdict: Fail

- All eight audit 3 findings are closed. A3-1 is closed on evidence: the finisher exists, the gate rates the new reference as Contract, there is no second frame check, there is no dead parameter, and every restatement agrees.
- One new medium finding is open, A4-2. The A3-1 repair turned S3's `file_identifier` check into an assertion that cannot fail, and D2 and Always name that check as the proof.
- Four new findings are low. Under the review repair policy they are fixed in the contract before dispatch, or by the implementation owner at dispatch, and then re-reviewed.

This is a scoped planning pass. It is not an implementation pass, and it is not build, browser or release qualification.

## Audit 3 dispositions

| Finding | Disposition | Evidence |
|---|---|---|
| A3-1 (high): D7's main-to-Core call fails the coupling gate | **Closed** | See "A3-1 verification" below. |
| A3-2 (low): S11's test-table boundary said "file" | Closed | The acceptance-to-test table's S11 row (contract `:509`) now says that a marked historical-fixture region is admitted, and that a restatement outside its markers in the same file is refused. |
| A3-3 (low): H-T4's deletion boundary | Closed (low residual in A4-5) | H-T4 (`:288`) deletes only the walk (`:168-180`). It keeps `:157-167`, renamed `CurrentRecordsRoundTripThroughTheSaveAndTheCommand`, and names S8's control and S4/S5 as the walk's cover. Checked against `DeclaredOrientationTests.cs`: the `[Fact]` is at `:157`, the round trip at `:166`, the owners at `:167`, the `Open` call at `:168`, `HoldsFieldBeyondTheInstanceTable` at `:169`, the vtable walk at `:170-180`, and the closing brace at `:181`. D5 (`:342`) names `ACurrentSaveRefusesTheFirstUnknownInstanceFieldAndItsValidControlLoads` (`:140`), which expects `WorkshopSaveRejection.OutOfRange` at `:141`. |
| A3-4 (low): the generation Inputs lacked the declaring file | Closed (C# counterpart in A4-1) | Always (`:100-104`) lists the stamp, the schema, the shared compiler targets file and `$(MSBuildThisFileFullPath)`. The existing `BuildRuntimeTypeScript` already uses the same idiom (`RuntimeTypeScript.targets:29-31`). |
| A3-5 (low): duplicate import into the vendored project | Closed | Always (`:107`) and the Code Map (`:183`, `:187`) import the shared file only from `tools/runtime/RuntimeTypeScript.targets`. `Directory.Build.targets:41` is `<Import Project="tools/runtime/RuntimeTypeScript.targets" />`, without a condition. `CuriousContraptions.FlatBuffers/` holds a `Directory.Build.props` but no `Directory.Build.targets`, so the root targets file reaches it. `PinnedImports` records the one importer. |
| A3-6 (low): S5 build cases that edit the deleted file | Closed (low residual in A4-5) | Code Map `:247` retargets `WorkshopWireBuildTests.cs:61` (edit) and `:77` (touch) to `workshop_wire.fbs`. It also adds a dispatch `rg -n workshop_authoring` for every other S5 or S5b case, which catches S5b's `:68`, `:82`, `:99`, `:122`, `:216-237` and `:328`. `:112` is disposed as `ADeletedSchemaFileLeavesNoGeneratedSource`. |
| A3-7 (low): identifier literals in strings | Closed | Always (`:73`) rewords `run-reset-snapshot.ts:321` and `workshop-wire.ts:12799` (both re-read; each holds "CCWS" in an error message). It states S11's roots: tracked C#, TypeScript and MSBuild sources, excluding `_bmad-output/` and `docs/`. Always `:71` says the C# conversion yields the `string` that `FlatBufferBuilder.FinishSizePrefixed(int, string)` (`FlatBufferBuilder.cs:1016`) and `Verifier.VerifyBuffer(string, bool, VerifyTableAction)` (`FlatBufferVerify.cs:781`) take; both re-read. An `rg` of the tracked sources for the four identifiers and their `uint32` values, outside the deleted `workshop-wire.ts` ranges, finds only sites that the contract disposes or allow-lists. |
| A3-8 (low): C36 formatting | Closed | `engine-stories-index.md:197-200` are indented two spaces under C36 (`:196`). C37 (`:201`) is a sibling again. |

### A3-1 verification

**The repaired D7.**
- `ICommandCodec.ValidateSaveFrame(ReadOnlySpan<byte>)` stays (`RuntimeCodecs.cs:9`).
- Only `CommandIdentifier`, `ResponseIdentifier` and `ObservationIdentifier` are deleted (`:15-16`, `:19`).
- Core's implementation (`CoreCommandCodec.cs:9`) passes `WireRootIdentifier.SavedConstruction` and `SavedConstructionVerify.Verify` to the one `WorkshopWireFlatBuffers.OpenFrame`.
- The main assembly encodes with `SavedConstruction.FinishSizePrefixedSavedConstructionBuffer`, and decodes with `codec.ValidateSaveFrame`, then strips the size prefix, then calls `GetRootAsSavedConstruction`.

**The finisher exists.**
- In the probe, the pinned compiler emitted `public static void FinishSizePrefixedSavedConstructionBuffer(FlatBufferBuilder builder, Offset<CuriousContraptions.Wire.SavedConstruction> offset) { builder.FinishSizePrefixed(offset.Value, "CCWS"); }` (`.tools/s5c-audit4/cs/CuriousContraptions/Wire/SavedConstruction.cs:40`). It also emitted `SavedConstructionBufferHasIdentifier` (`:19`) and `SavedConstructionVerify` (`:44`).
- The probe's `WorkshopResponse.cs` has no `Finish*Buffer`. That confirms the finisher follows `root_type` alone. After the merge, no generated runtime root carries one, and no caller needs one: the only `finish*WorkshopResponseBuffer` text is `tools/e2e/workshop-wire.ts:1950-1954`, inside the flatc copy that S5b deletes.
- Probe limit: the probe is a minimal schema, not the merged file. The corroboration on the real authoring tables is the pinned compiler's real build output in the S5b checkout, `slice-s5b/CuriousContraptions.FlatBuffers/obj/Debug/net10.0/WireAccessors/CuriousContraptions/Wire/SavedConstruction.cs:44`, which is the same method. The planning audit 1 output agrees (`.tools/s5c-audit/cs/CuriousContraptions/Wire/SavedConstruction.cs:44`). Task 1(a) compiles the real merged file at dispatch.

**The coupling gate admits it.**
- The generated accessors compile into `CuriousContraptions.FlatBuffers`: `GenerateWireAccessors` writes into that project's intermediate `WireAccessors/` (`CuriousContraptions.FlatBuffers.csproj:127-133`).
- That project is declared `Kind.WireContract` (`VerifyPolicy.cs:1564`). `ExposedStrength` rates a public reference into it as `Strength.Contract` (`:1725`). `Judge` returns `UnbalancedModel` only for a Model dependency on a volatile project (`:1652`, with `Volatile` at `:1606`).
- The main assembly already holds this edge kind:
  - it references the project (`CuriousContraptions.csproj:26`);
  - it already calls the generated `SavedConstruction` members (`WorkshopSaveCodec.cs:26-28`, `GetRootAsSavedConstruction` at `:61`) and the vendored `FlatBufferBuilder` and `ByteBufferUtil` (`:24`, `WorkshopAuthoringFlatBuffers.cs:37`).
- `ICommandCodec` and `WireFaultException` are in `CuriousContraptions.Abstractions` (`Kind.Abstraction`). The main assembly gains no reference to Core.

**No second frame check, no dead parameter.**
- The main assembly keeps no size, identifier or verifier check. `WorkshopAuthoringFlatBuffers.OpenFrame` and its `CurrentPuzzleTable` are deleted (`:34-74`). The required `assistance` vector is already `(required)` (`workshop_authoring.fbs:74`), so the generated verifier covers it.
- What remains on the main side is the prefix strip and the root read, which is not a check.
- `WorkshopSaveCodec`'s `codec` parameters (`:35`, `:48`, `:59`) stay used through `Open` → `ValidateSaveFrame`.
- The `identifier` parameter of the deleted `WorkshopAuthoringFlatBuffers.OpenFrame` goes with the method. Core's `OpenFrame` keeps its `identifier` and `verify`, and both stay used (`IsIdentifier`, `VerifyBuffer`, messages).
- Today's identifier switch in Core's `OpenFrame` (`WorkshopWireFlatBuffers.cs:45`) is deleted with H-C1, so no branch on the identifier remains.

**Consistent everywhere.** Each of these states the same design:
- Always: `:69` (the generated root-finisher exception), `:71` (the conversion beside Core's boundary) and `:113-116` (the S10 exception and why the check stays behind the interface; Abstractions gains no reference);
- the Code Map: `:211-219`;
- D7 (`:347-356`);
- row S10 (`:146`) and its test row (`:508`);
- Design Notes 5 (`:457`);
- H-C3 (`:283`).

No planning document outside the contract mentions `ValidateSaveFrame`, `OpenFrame` or `ICommandCodec` (`rg` over `docs/planning`, `TODO.md`, `epics.md` and `sprint-status.yaml`, excluding the S5c audits). Ruling (f)'s reservation of the Abstractions-to-FlatBuffers reference to `ENGINE-WIRE-ABI` is untouched. The Spec Change Log's A2-1 entry records the superseded design as history, and its A3-1 entry records the repair.

**S10's one exception is precise.** `CoreCommandCodec.cs` compiles only into Core (`RuntimeSources.props:42`; `Core.csproj:13`). Simulation removes `@(CoreRuntimeSource)` (`Simulation.csproj:46`). Today, `CoreCommandCodec.cs:9` is the only Core source that names a marked declaration or companion (audit 3, `rg`).

## New findings

### A4-1 (low): the compiler properties and their incremental input must move with the provisioning targets

Evidence:
- The Code Map (`:183`) moves `ProvisionFlatBuffersCompiler` (`:57`) and `DeleteFlatcTemporary` (`:114`) to the shared file. The compiler properties stay in the vendored project, unmentioned: `FlatcVersion`, `FlatcCacheRoot`, `FlatcAsset`, the two digests, `FlatcCacheDirectory` and `FlatcPath` (`CuriousContraptions.FlatBuffers.csproj:23-41`).
- The Abstractions instance's TypeScript generation needs them. Without them, provisioning stops at its `FlatcAsset` error (`:58`).
- Once the properties move, `GenerateWireAccessors`' Inputs (`:128`: the schemas, the list and `$(MSBuildProjectFullPath)`) no longer see a compiler pin change. That is the C# side of A3-4.
- `APinThatTheCachedCompilerDoesNotMatchRefusesTheBuild` edits the digest inside the vendored project file (`WorkshopWireBuildTests.cs:120-127`; `RequireCachedCompiler` at `:396-400` reads the properties by evaluating that project). The contract repeats that case on the Abstractions-copy fixture, but it does not move the vendored case's edit target.

Repair:
- State that `:23-41` move into the shared file with the two targets.
- Add the shared file to `GenerateWireAccessors`' Inputs.
- Retarget the pin-mismatch case's `Edit` (both fixtures) to the shared file. Keep the property evaluation working from each project.

### A4-2 (medium): S3's `file_identifier` boundary can no longer fail

Grade basis: this is a planned assertion that the testing hard rule forbids (`docs/testing-guidance.md:74`, "tests that cannot fail"). D2 (`:326`, "which S3 checks") and Always (`:69`, "S3 proves that its identifier equals the member") name it as the proof of ruling (b)'s invariant. Incidental cover by other tests does not lower the grade, because no named row owns that cover.

Evidence:
- S3 (`:139`) proves that the `SavedConstruction` member equals the schema's `file_identifier`. It does so by asserting that the generated `SavedConstructionBufferHasIdentifier` returns true for a save the codec wrote.
- After the A3-1 repair, the codec writes that save with `FinishSizePrefixedSavedConstructionBuffer`. That method and `SavedConstructionBufferHasIdentifier` both embed the same `file_identifier` literal (probe `SavedConstruction.cs:19`, `:40`). The assertion therefore holds for any `file_identifier`, which makes it a test that cannot fail (`docs/testing-guidance.md:41`, `:74`).
- The invariant itself is still protected. Every C# save decode compares the finisher's bytes with the converted member in Core's `OpenFrame`, and S4's save case frame-checks with the member on the TypeScript side. Coverage is not lost; only the named assertion is vacuous.

Repair: restate S3's boundary as an assertion that can fail. Two forms work:
- the four identifier bytes of a codec-written save (offset `SizePrefixLength + 4`) equal the one conversion of `WireRootIdentifier.SavedConstruction`; or
- a save finished by the generated finisher is admitted by `ICommandCodec.ValidateSaveFrame`, and the same save re-stamped with another member is refused.

Update the S3 test row's boundary to match.

### A4-3 (low): the conversion's domain and the remaining constant contexts

Evidence:
- The mixed theory (`WorkshopSaveTests.cs:398-401`) takes a `uint`: the member for the current root, and a named constant for the foreign `"XXXX"` (Always `:74`). The foreign word is not a `WireRootIdentifier` member. If the one C# conversion accepts only members, the test needs a second conversion, which Always (`:70`) forbids.
- `WorkshopSaveRejectionTests.cs:42` uses `WorkshopSaveCodec.Identifier` as an optional-parameter default (`string Identifier = WorkshopSaveCodec.Identifier`). The Code Map lists the line but gives no treatment, and an enum member is not a `string` constant.

Repair:
- State that the one conversion is total over `uint32`: any `WireRootIdentifier` value, member or not, to its four little-endian bytes. The theory then passes `(WireRootIdentifier)Foreign`.
- Give `:42` the theory's treatment: a `WireRootIdentifier` parameter defaulting to the member, converted at its one use.

### A4-4 (low): S10's "exactly one" needs a counting rule

Evidence:
- S10 replays the C# compiles from three binary logs (`solution.binlog`, `playtest.binlog`, `web.binlog`). Core is compiled in more than one of them.
- "Exactly one reference is admitted" and its control "a second site naming `SavedConstructionVerify`" (`:146`, `:508`) do not say whether the same source location in several replayed compiles counts once.

Repair: define the admitted reference as one source location (file and span), deduplicated across the replayed compiles. A second distinct location fails, in any compile.

### A4-5 (low): polish

Each item takes one line to fix:
- **Stale comments the deletion leaves behind.** S11 does not catch these.
  - `DeclaredOrientationTests.cs:154-156` ("Controls and the pinned walk … the slots the rule walks reach the same instance tables") describes the deleted walk.
  - `WorkshopSaveCodec.cs:44-47` describes "the refusal of any instance record field beyond the current table" after `:52-54`, which is deleted.

  Add both to the Code Map.
- **`WorkshopWireBuildTests.cs:61` edit anchor.** It is `"table SavedConstruction {\n"`. That text no longer exists once the declaration carries `(authoring)`, so the retargeted case must use the marked text.
- **`OldLayoutSaves.cs:45`.** It uses `WorkshopWireFlatBuffers.CommandIdentifier` inside the allow-listed historical region (`:11-80`). It is missing from the Code Map's caller list. The dispatch `rg` catches it; name it.
- **Two `WorkshopSaveCodec.cs` bullets.** The Code Map has them at `:211-214` and `:216-219`; merge them.
- **Design Notes 7** (`:464`) omits the three deleted `ICommandCodec` properties.
- **Always `:69`.** It limits the generated-root-API exception to "the main assembly's save encoder". Tests may also use the generated finisher and `SavedConstructionBufferHasIdentifier`, which are generated code. Say "generated code, including the root API that `root_type` emits".

## Checks that hold

| Question | Result |
|---|---|
| Rulings (a) to (f) | Complied with. Ruling (b): the save finisher's identifier is the schema's `file_identifier`, which is schema data generated on both sides. Ruling (f)'s reservation is kept, because Abstractions gains no reference. |
| Forward-only | No format version, alias, fallback decoder, second schema, second frame check, retained deprecated item or dead parameter. The one save frame check is Core's `OpenFrame`, reached through the existing interface. The deleted `WorkshopResponse` `root_type` leaves no caller of a root-only API. |
| Coupling gate (AC4) | The plan adds no Model dependency on a volatile project. Main-assembly references stay within Abstractions and the WireContract assembly. Not run in this pass. |
| Testing guidance and hashes | Exact equality of decoded values is a defined-interface invariant. No hashes, mutants, skips or browser gating. Guards are named, each with a positive control. Deleted tests name their cover. Open: S3's `file_identifier` assertion cannot fail (A4-2, medium). |
| Element-free design | No element identity, capability kind or table name selects behaviour. `ValidateSaveFrame` is a root-level frame contract. The eight design-record questions are answered. |
| Other TypeScript frame checks | `rg` over `CuriousContraptions.web/wwwroot` and `tools/runtime` finds no FlatBuffers frame check. The `getUint32(0, true)` hits are a clock-peer layout (`workshop-client.ts:231`, H-W7 territory) and two non-wire test readers. S11's "exactly one TypeScript frame check" is achievable. |
| Planning edits | Unchanged since audit 3, and consistent with the contract. C36's indentation is repaired. |
