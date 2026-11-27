# Planning audit: `spec-s5c-one-wire-schema-file.md` (S5c-1), seventh pass (bounded re-audit after the split)

Audited 26 November 2026, read-only, planning phase. Checkout `.claude/worktrees/slice-s5c`, branch `slice-s5c`, HEAD `39aaba70` (published `origin/main`, containing S5b), with the uncommitted planning edits of run log E2 and E4 (`git status`: 12 modified files, 6 untracked contracts, run logs and `audit-s5c-6.md`). The S5c-1 contract is 5,483 words (`wc -w`). Policy read from this checkout only: `AGENTS.md`; `_bmad/custom/role-cards.md` (Informed auditors); `_bmad/custom/bmad-loop-protocol.md` (Capability probe, Review tiers, Contract budget, Reviewer duties, Triage); `_bmad/custom/project-engineering-contract.md`, `element-free-engine-design.md`, `element-free-engine-review.md`; `docs/testing-guidance.md`; owner decision 24p and rulings (a) to (f) (`owner-decisions-2026-11-22.md:86-92`); `audit-s5c-6.md` (and `audit-s5c-5.md` for history); the three S5c contracts; the S5c-1 run log (handled messages, E1, E2, E4).

Scope (coordinator brief): (1) S5c-1 readiness; (2) closure of the audit 6 repairs S5c-1 owns; (3) the split's integrity; (4) S5c-1 with S5b's TypeScript generation left in place; (5) the planner's four flagged items; (6) the line-local planning-record edits. S5c-2 and S5c-3 were read only for placement. Settled design was not reopened except where now false against `39aaba70`.

Measurements made: `rg`, `sed -n`, `git diff`, `git show 39aaba70:<contract>` (the pre-split text, compared row by row with the three contracts). One probe with the pinned compiler (`~/Library/Application Support/CuriousContraptions/flatc/25.2.10/Mac.flatc.binary/flatc`, `flatc version 25.2.10`) on a scratch single-file schema `.tools/s5c-evidence/audit7/probe/schemas/workshop_wire.fbs` (Anvil-validated; namespace, `attribute "authoring";`, the four-member `WireRootIdentifier : uint32` with the contract's hex values, three wire tables, one wire struct, one marked struct, two marked tables, `root_type SavedConstruction; file_identifier "CCWS";`): `flatc -M --ts --no-warnings -o d "workshop_wire.fbs"` run in its directory (as `OrderWireSchemas` runs it), `flatc --ts -o ../ts` and `flatc --csharp -o ../cs`. The generated outputs under `audit7/probe/ts/` and `audit7/probe/cs/` were written by the compiler, not through a validated write. No build, test, mutant, browser or maintainability-gate run.

## Verdict: Fail

- Two medium findings are open, both "faithful implementation cannot pass":
  - **A7-1**: the closed grammar of `WireSchemaText` refuses the real schema file (an enum with implicit member values), and its scalar and literal sets are not closed.
  - **A7-2**: moving `SchemaTypesIn` onto `WireSchemaText` breaks `CSharpStaticGateTests.TheGeneratedWireRuleAdmitsOnlySchemaTypes`, which the contract does not list, and leaves the gate's union branch unreachable.
- Seven low findings (A7-3 to A7-9), each with exact repair text.
- Audit 6: everything S5c-1 owns is Closed except A6-8's `WireSchemaText` bullet (Open, A7-1) and A6-8's S10 bullet (Closed in substance, wording repair A7-5).
- The split is intact: every matrix row S1 to S14, every inventory row and every audit 6 repair lands in exactly one contract (H-T4 deliberately in two parts, each owned once).
- S5b's TypeScript generation, its `schemas.txt` record, its tests and the runner's check work with exactly one schema file and no `include` (section "S5b's generation with one file").
- All repairs are contract text; none reopens settled design. A bounded check of A7-1 and A7-2 and a skim of the low repairs suffice before the build dispatch.

This is a planning audit. It is not an implementation pass, and it is not build, browser or release qualification.

## Audit 6 dispositions (what S5c-1 owns)

| Finding | Disposition | Evidence |
|---|---|---|
| A6-2 (sequencing) | **Closed** | Concurrency (`:197-199`): S5c-1 touches no file under `tools/E2eRunner/`, `tools/E2eRunner.Tests/` or `tools/e2e/`; Never (`:98`) forbids it. The runner files are S5c-2's, which starts from the main that contains lane 3's slice (S5c-2 Concurrency). |
| A6-3 (j) | **Closed**, with A7-3 | Code Map `:146` retargets `:68`, `:82`, `:99`, `:123` and rewrites `:205-296` and `:334-354` around probe schemas. A7-3 covers what the rewrite must not call and one case's root. |
| A6-3 (k) | **Closed** | `WorkshopFormatTests.cs:180` is named twice (`:139`, `:143`); re-read: `builder.FinishSizePrefixed(builder.EndTable(), WorkshopSaveCodec.Identifier);` inside the historical region `:160-181`. |
| A6-5 (1) | **Closed** | Always `:77`: `THIRD-PARTY-NOTICES.md`, the vendored header `:7-9` and `VendoredFlatBuffersIdentityTests.cs:5-12` state the re-apply-on-upgrade rule; the header change re-pins `CSharpStaticGateTests.cs:116` (Code Map `:130`; the project file is pinned whole). |
| A6-5 (2) | **Closed** | `VendoredFlatBuffersVerifierTests` is a new file (`:137`) with A6-5's positive, control and boundary cases verbatim in the acceptance-to-test table (`:275`). |
| A6-5 (3) | **Closed** | Always `:74` records the highest offset passed to every per-field method, both union offsets included (`FlatBufferVerify.cs:658`, `:696` re-read: `VerifyUnion`, `VerifyVectorOfUnion`); `:89` makes the `union`/`deprecated`/`id:` refusal once, in the parser; `OnlyTheClosedGrammarIsAccepted` (`:279`) has the four controls and the real-file boundary. |
| A6-6 | **Closed** | Always `:78`; Code Map `:126`, `:129` (re-pins `:23` and `:27`, re-read: the `FlatBufferVerify.cs` and `Table.cs` digests). `LICENSE:98-99` re-read: §4(b). |
| A6-8 `WireSchemaText` | **Open** (A7-1) | The grammar (`:85-92`) is A6-8's text, but it refuses `workshop_wire.fbs:385` and leaves the scalar and literal sets open. |
| A6-8 S9 | **Closed** | Row S9 (`:109`) carries the resolved-type walk and the stated limit. |
| A6-8 S10 | **Closed in substance**; wording A7-5 | Row S10 (`:110`) carries the parser, generator driver, error refusal and inspected forms. It does not name the existing seam it should extend. |
| A6-9 (a) | **Closed** | `owner-decisions-2026-11-22.md`: blank lines now separate 23/24c (`:34-36`), 24c to 24 (`:36-48`), 24g to 24p (`:67-86`) and (f)/38 (`:92-94`); none between 24m and its "Merging back" continuation (`:79-80`). `git diff` shows only blank lines plus the 24p Plan sentence and ruling (e). |
| A6-9 (b) | **Closed** | Ruling (e) (`:91`) reads "Applying 24p ... 24p, the later owner decision, governs". |
| A6-9 (c) | **Closed** | No "planning cap", "four-round" or "sixth planning" text in any S5c contract; run log E4 `:58` carries "The coordinator authorised a bounded fifth check". |
| A6-9 Plan sentence | **Closed** | 24p's Plan sentence (`:86`) names S5c-1, S5c-2 and S5c-3 with working relative links. |
| A6-10 | **Closed**, re-measure after repairs | 5,483 words, under 6,000. This audit's repairs add about 500. After applying them the planner re-measures; if the total exceeds 6,000, the first block to move is the five audit 1 to 5 lines of the Spec Change Log (`:226-230`), collapsed into one line citing run log E4, which already holds them. A record action, not a finding. |
| A6-11 (kept citations) | **Closed** | Every citation S5c-1 introduces or changed was re-read at `39aaba70` and holds (section "Citations spot-checked"). |
| A6-1, A6-3 (a)-(i), A6-4 | Placed in S5c-2 | Not audited for readiness here. |
| A6-7, A6-8 S11 | Placed in S5c-3 | Not audited for readiness here. |

## New findings

### A7-1 (medium, wrong specified behaviour): the closed grammar refuses the real schema file, and its scalar and literal sets are open

Evidence:
- Always `:87` admits only `enum Name : <integer scalar> { Member = <decimal or 0x literal>, … }`. The real file declares `enum ObservationPayload: uint8 { None, Fault, Recalibration, RejectedClock, LateRenewal, WideOffset, Derivation, Deferral, Debt, TickFailure, Transfer, Capture }` (`schemas/workshop_wire.fbs:385`), whose members have no values. A faithful parser refuses the file, so `WireSchemaTextTests.OnlyTheClosedGrammarIsAccepted`'s boundary ("the real file is admitted", `:279`) fails. The implementer would then widen the grammar during the build, which is the S5b open-scanner pattern A6-8 was written to prevent.
- "A FlatBuffers scalar name" (`:88`) is not a closed set. The file uses both spellings: `ulong` (`:542`), `uint` (`:509`) and `ubyte` (`:568`), beside `uint64`, `uint32` and `uint8`.
- "`<literal>`" as a field default (`:87`) is not closed. Measured: the only defaults are 12 `null` and one `0.0`.
- "the only reader of the schema file" (`:85`) is read literally by tree-wide text scans that include `.fbs` (`WorkshopFormatTests.cs:91`, `HalfPrecisionGuardTests.cs:23`, `TypedTransportCommandTests.cs:393`). These scan source text for names; they are not schema parsers.

Repair (in Always, "One schema reader"):
- Replace `:85`'s first sentence with: "`WireSchemaText` (test helper) is the only parser of the schema file. Tree-wide source scans that read `.fbs` as text (`WorkshopFormatTests.cs:91`, `HalfPrecisionGuardTests.cs:23`, `TypedTransportCommandTests.cs:393`) are not schema readers. It accepts exactly this subset and refuses anything else by name and line:"
- In `:87`, replace the enum form with `enum Name : <integer scalar> { Member [= <decimal or 0x integer>], … }`. A member without a value takes the previous member's value plus one, and the first takes 0, as the compiler assigns. Replace the table default with `[= <decimal or 0x integer | decimal float | null | true | false | member of the field's enum>]`.
- Append to `:88`: "The scalar names are exactly `bool byte ubyte short ushort int uint long ulong float double int8 uint8 int16 uint16 int32 uint32 int64 uint64 float32 float64`; an integer scalar is one of these other than `bool`, `float`, `double`, `float32` and `float64`."
- Add to `OnlyTheClosedGrammarIsAccepted`'s boundary (`:279`): "an enum whose members have no values is admitted, with values 0, 1, …".

### A7-2 (medium, wrong specified behaviour/test): `SchemaTypesIn` on the closed grammar breaks a static-gate theory the contract does not list, and leaves the gate's union branch unreachable

Evidence:
- The contract moves `CSharpStaticGateTests.SchemaTypesIn` (`:510-512`) onto `WireSchemaText`'s model (`:90`, `:130`).
- `TheGeneratedWireRuleAdmitsOnlySchemaTypes` (`CSharpStaticGateTests.cs:987-1034`, 11 theory cases) calls `SchemaTypesIn([schemaText])` (`:1031`) on a synthetic fragment. The fragment (`:1004`) has no namespace, no `attribute "authoring";`, no `root_type` or `file_identifier`, and an enum with implicit values. Its `UnionVerify` case (`:1022-1024`) appends `union Choice { Holder }`, which the grammar refuses by design. Every case then throws in the parser.
- With unions refused, `SchemaKind.Union` (`:502`), the union arm of the Verify-class rule (`:550-553`) and "or union" in the rule's texts (`:516`, `:543`, `:999-1001`) become unreachable. `schemas/README.md:36` ("the `Verify` classes the compiler writes for tables and unions") also becomes stale. Run log E4 `:40` treats unreachable branches left in the tree as a Forward-Only finding.
- The Code Map's `CSharpStaticGateTests.cs` bullet (`:130`) names only the pin `:116` and `SchemaTypesIn`. The likeliest unplanned repair is a fragment mode in `WireSchemaText`: a second grammar, against A6-8.

Repair (append to the Code Map's `CSharpStaticGateTests.cs` bullet, `:130`):

> `TheGeneratedWireRuleAdmitsOnlySchemaTypes` (`:987-1034`): its synthetic schema becomes a complete text of the closed grammar (`namespace CuriousContraptions.Wire;`, `attribute "authoring";`, the enum, struct and table, `root_type Holder; file_identifier "TEST";`). The regex reader `SchemaDeclaration` (`:504`), which `WireSchemaText` replaces, is deleted. The `UnionVerify` case and `GeneratedWireCase.UnionVerify` are deleted, and with them `SchemaKind.Union` (`:502`), the union arm of the Verify-class rule (`:550-553`) and "or union" in its texts (`:516`, `:543`, `:999-1001`). `schemas/README.md:36` says "tables". `WireSchemaText` has no fragment mode.

Add `schemas/README.md:36` to the README bullet (`:148`).

### A7-3 (low, test): the `WorkshopWireBuildTests` rewrites: what the multi-file steps may call, and one case's root

Evidence:
- `GeneratedTypeScriptViolations` (`WorkshopWireTypeScriptTests.cs:112-126`, through `SchemaTypes` `:97-98`) and `WireBuildCopy.SchemaTypes()` (`WorkshopWireBuildTests.cs:651-652`) both read the schemas through `SchemaTypesIn`, so after S5c-1 they read through `WireSchemaText`. The steps of `ASchemaSetWithoutOneRootReachingEverySchemaRefusesTheBuild` that hold two files or an `include` call them: the `./workshop_wire.fbs` control (`:283-287`) and the `S5bWarned` control (`:289-295`). Always `:92` forbids this, but the contract does not say that these helpers are `WireSchemaText` readers, or what replaces them.
- `ADeletedSchemaFileLeavesNoGeneratedSource` (`:334-354`) proves that "deleting a whole schema file leaves every remaining input older than the previous output, so only the schema list can show the change". The Code Map rewrite has "`workshop_wire.fbs` as the one root (an include edit prepends a line to it)" (`:146`). If the probe is included by `workshop_wire.fbs`, deleting it also requires editing `workshop_wire.fbs`, which then becomes newer, so the case no longer discriminates the list stamp. The case keeps its property only if the probe includes `workshop_wire.fbs` and is itself the root. `OrderWireSchemas` admits that (the probe is in no other rule; the second `-M` run reaches both), and after the deletion `workshop_wire.fbs` "includes nothing", as T10 says.
- The comment at `:79` ("T2: the field is added to the authoring schema, which reaches the wire schema through its include") goes stale like `:185`, but the contract does not list it.

Repair (replace the second and third sentences of Code Map `:146`):

> `ASchemaSetWithoutOneRootReachingEverySchemaRefusesTheBuild` (`:205-296`) is rewritten around probe schema files in the copied tree, with `workshop_wire.fbs` as the one root (an `include` edit prepends a line to it). `ADeletedSchemaFileLeavesNoGeneratedSource` (`:334-354`) uses a probe that includes `workshop_wire.fbs` and is the root, so deleting it edits no other file. A step that holds more than one schema file or an `include` asserts on refusal lines and on `copy.TypeScriptExports()` and `copy.GeneratedTypeNames()`. It never calls `GeneratedTypeScriptViolations` or `copy.SchemaTypes()`, which read through `WireSchemaText`. They stay on single-file states. The comments at `:79` and `:185` are reworded.

### A7-4 (low, test): S1 and S9 have a control but no boundary case

Evidence: the acceptance-to-test table rows S1 (`:273`) and S9 (`:276`) name controls only. The role card requires one named discriminating test per matrix row "with its control and boundary case".

Repair:
- S1 row: append "Boundary: the real `schemas/`, which also holds `README.md`, is admitted; a synthetic schema whose `WireRootIdentifier` names a member that is not a declared table is refused, naming it."
- S9 row: append "Boundary: a synthetic runtime root that reaches `InstanceRecord` only through a vector's element type (`records: [InstanceRecord]`) is refused, naming the path; the real file, whose runtime roots and marked declarations share the unmarked struct `Vec3f` (`WorkshopResponse.bodies` → `BodyRead.local`; `InstanceRecord.local`), is admitted."

### A7-5 (low, wrong specified behaviour/record): S10 does not name the existing semantic seam, and "not a hand tokenizer" conflicts with it

Evidence:
- The probe cites `CSharpStaticGateTests.ReplayCscRuns` (`:178`), which is `private` (`CSharpStaticGateTests.cs:403`).
- The gate already exposes an internal semantic seam, `MatchingCompilations` (`:370-382`, used by `GameFloatGuardTests.cs:131`). It builds each compilation through `CSharpCommandLineParser.Default.Parse` from tokens split by the gate's own `CommandLineTokens` (`:384-401`). Row S10's "with Roslyn's `CSharpCommandLineParser`, not a hand tokenizer" (`:110`) can be read as requiring a second replay and tokenizer beside it. Replacing `CommandLineTokens` would change the existing gate's parsing, which is outside S5c-1's scope.
- The S10 premise holds: the test project compiles against the SDK's own Roslyn (`CuriousContraptions.tests.csproj:41-42`, `$(MSBuildSDKsPath)/../Roslyn/bincore`), so the SDK's source generators load in the replay.

Repair:
- In row S10, replace "parses each `csc` command line with Roslyn's `CSharpCommandLineParser`, not a hand tokenizer," with: "extends the gate's one semantic seam, `CSharpStaticGateTests.MatchingCompilations` (`:370-382`; `CSharpCommandLineParser` over the gate's one command-line split), with no second replay or tokenizer,".
- In the probe's Present list, replace "`CSharpStaticGateTests.ReplayCscRuns`; the static gate facts" with "`CSharpStaticGateTests.MatchingCompilations`; `GameFloatGuardTests`".
- Add to the `CSharpStaticGateTests.cs` bullet (`:130`): "S10 takes each run's parsed arguments from the seam and runs `CSharpGeneratorDriver` over their `AnalyzerReferences` itself; `MatchingCompilations`' output to its existing callers (`GameFloatGuardTests.cs:131`) is unchanged."

### A7-6 (low, record): S8's Error Handling cell asks the verifier to name the table

Evidence: row S8 (`:108`) has "Names the table" in Error Handling. The vendored `Verifier` returns `bool` and has no message channel. `OpenFrame` (`WorkshopWireFlatBuffers.cs:38-43`) reports "The FlatBuffers verifier refused a {identifier} buffer." A faithful reader could add a table-name channel to the vendored runtime, which is a second patch the contract does not record.

Repair: replace S8's Error Handling cell with "The refusal carries `VerifierRejected` and the root's identifier; each test case names its table".

### A7-7 (low, test/record): AC3's `rg` is not one exact command

Evidence: AC3 (`:221`) says "`rg` finds none of their deleted symbols". The deleted names include generic ones: `Reference`; `OpenFrame`, of which `WorkshopWireFlatBuffers.OpenFrame` stays; and `Identifier`. D5's pattern includes `ValidateSaveFrame`, which stays. Also, `tools/e2e/unit/run-reset-snapshot.test.ts:17-18`, which S5c-1 may not edit, reads `schemas/workshop_authoring.fbs` until S5c-3's H-S2. That test is browser-deferred and already fails 23 of 24 cases (BH3-4). The audit ran the pattern below at `39aaba70`: it hits exactly the files the Code Map names, plus that one deferred test.

Repair: replace AC3's "`rg` finds none of their deleted symbols" with:

> `rg -n "\b(ValidateBindingShapes|TableTarget|CurrentPuzzleTable|InstanceRecordFields|SavedConstructionSlot|ConstructionInstancesSlot|HoldsFieldBeyondTheInstanceTable|CommandIdentifier|ResponseIdentifier|ObservationIdentifier|TheVersionlessRecordsDeclareExactlyTheirFields|CurrentRecordsHoldNothingBeyondTheTableAndTheWalkReachesTheGeneratedInstances)\b|WorkshopAuthoringFlatBuffers\.OpenFrame|WorkshopSaveCodec\.Identifier|workshop_authoring" --glob '*.cs' --glob '*.ts' --glob '*.fbs' --glob '*.csproj' --glob '*.props' --glob '*.targets' --glob '!_bmad-output/**' --glob '!.tools/**' --glob '!tools/e2e/unit/run-reset-snapshot.test.ts'` prints nothing

Append to the Always evidence limit (`:65`): "`tools/e2e/unit/run-reset-snapshot.test.ts:17-18` reads the deleted `schemas/workshop_authoring.fbs` until S5c-3 (H-S2). The test is browser-deferred and is not run."

### A7-8 (low, record): current planning documents still cite the deleted file

Evidence (`rg -n workshop_authoring`, excluding `_bmad-output/`, `.tools/`, build output and audit files): current element and ledger texts direct future slices to edit, or cite lines of, `schemas/workshop_authoring.fbs`:
- `docs/planning/elements/EL-124-authored-gravity-preset.md:127` and `:128`;
- `docs/planning/elements/EL-125-authored-atmosphere-preset.md:130`;
- `docs/planning/elements/readiness/completion-ledger.md:305` and `:309` (`:62-77`);
- `docs/planning/invest/delivery-batches.md:361` (S8's "Now", `:62-77`).

`delivery-batches.md:295` (S5's "Before") and `:314` (S5c's "Now") describe the past and stay. The contract's README and planning-record bullets do not cover these. AGENTS.md requires active docs to be updated together with the change.

Repair: add to the Code Map's planning-records bullet (`:149`): "Current documents citing the deleted file are repointed to `schemas/workshop_wire.fbs` (`PuzzleRecord`, marked `(authoring)`), with line numbers re-measured after the merge: `EL-124-authored-gravity-preset.md:127`, `:128`; `EL-125-authored-atmosphere-preset.md:130`; `completion-ledger.md:305`, `:309`; `delivery-batches.md:361`. `delivery-batches.md:295` and `:314` are history and stay."

### A7-9 (low, record): the planning-records list omits files the candidate will carry, and one acceptance pointer is singular

Evidence:
- The S5c-1 candidate will commit the E2 and E4 planning edits now in the tree: `owner-decisions-2026-11-22.md`, `epics.md`, `completion-ledger.md`, `engine-stories-index.md`, `named-elements.md`, `vertical-delivery.md`, `deferred-work.md`, S5b's run log (E37), the S5c-2 and S5c-3 contracts and run logs, `audit-s5c-6.md` and this audit.
- The Code Map's planning records (`:149`) name only the contract, its run log, `TODO.md`, `sprint-status.yaml` and `delivery-batches.md`. The Final diff scope check would report the rest as unowned.
- `delivery-batches.md:316` reads "**Acceptance.** As in the contract, rows S1 to S14", but there are now three contracts.

Repair:
- Append to `:149`: "and the planning edits of run log E2 and E4 that this candidate carries: `owner-decisions-2026-11-22.md`, `epics.md`, `completion-ledger.md`, `engine-stories-index.md`, `named-elements.md`, `vertical-delivery.md`, `deferred-work.md`, the S5b run log (E37), the S5c-2 and S5c-3 contracts and run logs, and `audits/audit-s5c-6.md` and `audit-s5c-7.md`."
- At `delivery-batches.md:316`, change "As in the contract" to "As in the three contracts".

## S5b's generation with one file (scope item 4)

| Question | Result |
|---|---|
| `OrderWireSchemas` on one file | The probe's `-M` output is one rule, `d/workshop_wire_generated.ts: workshop_wire.fbs`. After the target strips the rule target (`_WireMakeNoise`, `CuriousContraptions.FlatBuffers.csproj:226`), the dependency text is `workshop_wire.fbs`, so `_WireUnknownIncludes` is empty (`:244`). `_WireDependencyNames` is `||workshop_wire.fbs||`. The included-by-another test (`:251`) compares 19 with 19, which is not greater, so the file is the one root and neither no-root nor two-root `Error` fires (`:254-257`). The second `-M` run on the root (`:258`) reaches it, so none is unreached (`:267-270`). `_WireSchemaOrdered` is `[workshop_wire.fbs]`. |
| Plain `--ts` output and T1 | One entry module `curious-contraptions/wire.ts` with one line per declaration in T1's `EntryExport` form (`WorkshopWireTypeScriptTests.cs:105`), including `export { WireRootIdentifier } from './wire/wire-root-identifier.js';`. The type files match `TypeDeclaration` (`:106`): `export enum WireRootIdentifier {`, with values 1296258883, 1297241923, 1331118915 and 1398227779 (= 0x4D435743, 0x4D525743, 0x4F574343, 0x53574343). The `authoring` attribute is not mentioned in any TypeScript or C# output, and there is no stray file. In every generated file (`grep` over `probe/ts/`) the only `any` is the eslint suppression line, which S5b's normalisation removes (`:193`). The probe has no `string` field; the real file's `optionalEncoding?:any` sites are the ones that normalisation already rewrites. |
| C# output | `public enum WireRootIdentifier : uint` with the same values; `SavedConstruction.FinishSizePrefixedSavedConstructionBuffer`, `SavedConstructionBufferHasIdentifier` and `SavedConstructionVerify` exist. The marked tables keep their `Verify` classes (`SavedConstructionVerify`, `PuzzleRecordVerify`); the structs (`ConnectionRecord`, `Vec3f`) have none. |
| `schemas.txt` and its readers | The record becomes `workshop_wire.fbs`, equal to the current `schemas/*.fbs` set, so T1's `SchemaRecordViolations` (`:127-138`) and its renamed-record control (`:59-65`, which replaces `workshop_wire.fbs`) hold, and the runner's `RequireGeneratedWireModule` name-set comparison (`Toolchain.cs:93-96`) passes. The changed vendored project header (`:7-9`) makes the module stale for the runner (`:97-99`) until a Debug build, which regenerates because the project file is a generation Input (`:174`). The removed schema leaves no file behind: `_WireTypeScriptPrevious` is deleted (`:196-199`). |
| Runner tests | `TypeCheckBoundaryTests.TheGeneratedWireEntryModuleMustExistBeforeSuitesRun` (`:137-221`) and `IdentityTests.TheSuiteDigestCoversTheGeneratedWireModulesAndNoOtherIgnoredFile` (`:181-212`) build synthetic trees (`s.fbs`, `t.fbs`, `u.fbs`, `a.fbs`) in temporary directories and read no real schema. They are unaffected. |
| Tests S5c-1 must retarget | `rg -n workshop_authoring` and `table SavedConstruction \{` at `39aaba70`: `WorkshopWireBuildTests.cs:68`, `:82` (anchors), `:99`, `:123` (touches), `:223-244` and `:346` (in the rewritten cases); `WorkshopFormatTests.cs:31`, `:100`; `BundleElementIdTests.cs:209`; `DeclaredOrientationTests.cs:137`; `WorkshopAuthoringFlatBuffers.cs:76`. All are named, except the static-gate theory (A7-2), the comment `:79` (A7-3) and the deferred `run-reset-snapshot.test.ts:17` (A7-7). |
| T1 and the `authoring` attribute | T1's schema-type oracle (`SchemaTypes` → `SchemaTypesIn`) includes `WireRootIdentifier`, and so do the exports, so the sets agree. The attribute emits nothing, so no required test is broken by it. `TheVersionlessRecords…` leaves with H-T1. `BundleElementIdTests` on the whole merged model still finds exactly `part_id` and `inventory_part` (no wire field matches `(^|_)(part|element)(_|$)`). |
| D9, forward-only | **Pass.** The `WireSchema` glob (`:43`) still feeds `OrderWireSchemas`, so its include branches are reachable for the input it accepts, and the rewritten build tests exercise them. They are the one current generation path's input validation, not a fallback, an old-format path or a second rule chosen by condition. S5c-2 deletes them with the glob and the target. **Evidence limit:** S1 is a test-time guard on the tree; until S5c-2, the build itself still accepts a second `.fbs` file or an `include`. |

## Split integrity (scope item 3)

- **Matrix rows.** Pre-split rows (`git show 39aaba70:…spec-s5c-one-wire-schema-file.md`, `:137-150`) compared with the three contracts: S1 and S8 are verbatim in S5c-1. S2 to S7 and S13 are verbatim in S5c-2. S14 is verbatim in S5c-3. S9 and S10 (S5c-1) differ only by A6-8's appended text; S11 (S5c-3) by A6-8's closed list; S12 (S5c-2) by A6-3 (f)'s `<FlatcVersion>` clause. Each row is in exactly one contract, and no row is cut.
- **Inventory.** Pre-split rows H-C1 to H-C4, H-T1 to H-T4, H-S1 to H-S6, H-R1, H-W3, H-W4, H-W6 and H-W7 all appear in S5c-3's full table, each with one slice or owner. S5c-1 owns H-C1 to H-C4, H-T1 and H-T4's comment and walk. S5c-2 owns H-S3. S5c-3 owns H-T2, H-T3, H-T4's region marker, H-S1, H-S2 and H-S4 to H-S6. H-T4 is split on purpose: its walk calls `HoldsFieldBeyondTheInstanceTable`, which S5c-1 deletes. Each part is owned exactly once, and both contracts state the split identically.
- **Audit 6 repairs.**
  - S5c-1: A6-3 (j) and (k), A6-5, A6-6, A6-8's `WireSchemaText`, S9 and S10 bullets, A6-9 and A6-11.
  - S5c-2: A6-1 (runner, T1 record, T3 record case, README `:64`, `:69-71`), A6-3 (a) to (i) (D10 chooses "move with the target"), A6-4 and A6-11.
  - S5c-3: A6-7 (both repairs) and A6-8's S11 bullet.
  - A6-2 appears in the Concurrency sections of S5c-1 and S5c-2 by design (sequencing).
  - A6-10 is the split itself.
  - None is dropped or duplicated.
- **Decisions, tasks and ACs.** D1, D2, D5, D7 and D9 are in S5c-1. D3, D4, D6, D8 and D10 are in S5c-2. D11 and D12 are in S5c-3. Pre-split task 1 (a) to (e) is distributed: (a) plain `--ts`, (b) = old (d), and (c) replay inputs are in S5c-1; (a) object API, (b), (c) and (e) are in S5c-2. The pre-split AC1's S2 clause moved to S5c-2 AC1, and AC3's inventory-owner clause to S5c-3 AC3.

## The planner's four flagged items (scope item 5)

1. **H-S3 in S5c-2: agreed.** S5c-2 introduces `wire-frame.ts` as the one TypeScript frame check. Leaving `workshop-wire.ts:20-29` until S5c-3 would make S5c-2 hold two, which is a Forward-Only Fail. The ruling's own parenthesis for S5c-3 omitted H-S3. S5c-3's inventory and S11 form (5) agree with the placement.
2. **H-T1's interim cover gap as an evidence limit: agreed.** H-T1 must leave with the file it reads. Its version intent stays covered (`WorkshopFormatTests.cs:20-38`). Its "any re-added field" intent is withdrawn under 24p. Its cross-language slot cover arrives with S4, S5 and S14. Between S5c-1 and S5c-2 only browser-deferred code consumes the TypeScript, both bindings come from one file, and no required test or gameplay path depends on the gap. The limit is stated at Protections (`:194`).
3. **24c's blank line: Closed.** `owner-decisions-2026-11-22.md:35` is now blank. `24c.` is not an ordered-list marker, so without it 24c was part of decision 23's paragraph.
4. **Descriptive mentions: agreed,** with one exception. `delivery-batches.md:309`, `:311`, `:325`, `engine-stories-index.md:196`, `:200` and `epics.md:4673`, `:4678`, `:4698`, `:4701` name S5c as the story, which the three slices deliver, and none contradicts the split. The exception is `delivery-batches.md:316` ("As in the contract"), repaired in A7-9.

## Planning-record edits (scope item 6)

Each edit is a single-line replacement, apart from the added lines: `sprint-status.yaml` (two new keys) and the S5b run log (E37, appended). The records are accurate and do not contradict each other or the contracts:
- `TODO.md:11` records the S5b publication receipt (matching run log E37 and S5b's E35 to E37 totals) and the S5c-1 → S5c-2 → S5c-3 chain, with S5c-2 "from the main that contains S5c-1 and lane 3's lifecycle slice".
- `deferred-work.md:309` names owner S5c-2 (BH2, the root pin), and `:310` names owner S5c-3 (BH3-4); both match the contracts.
- `sprint-status.yaml:577-579`: `s5c-one-wire-schema-file` is `in-progress` (S5c-1); the two new keys are `backlog`.
- `epics.md:81` and `:4426` (pre-programme lists) name the three slices. `ENGINE-WIRE-ABI`'s *Depends on* (`:4703`) reads "S5c-1 to S5c-3, ending with S5c-3".
- `completion-ledger.md:128`, OS-01 `:392`, `engine-stories-index.md:5`, `:21`, `named-elements.md:22`, `vertical-delivery.md:9`, `:22` and `delivery-batches.md:13`, `:313` list S5c-1, S5c-2 and S5c-3 in lane 2 order.
- `owner-decisions-2026-11-22.md` is covered under A6-9.

## Citations spot-checked at `39aaba70` (all hold)

- `WorkshopFormatTests.cs:20-38`, `:30-31`, `:96-123` (the method ends at `:125`, the cited range covers its body), `:99-100`, `:137`, `:160-181`, `:176`, `:180`.
- `DeclaredOrientationTests.cs:137-138`, `:140` (`OutOfRange`), `:148-151`, `:154-156`, `:157-181`, `:168-180`.
- `F32SaveBoundaryTests.cs:12` (`"CCWS"`), `:48`; `GoalSignalSelectorTests.cs:81`; `WorkshopSaveRejectionTests.cs:42`; `CompoundMassAndRollingTests.cs:896`, `:898-900`; `WorkshopWireFlatBuffersTests.cs:43`, `:46`; `WorkshopSaveTests.cs:398-401`; `SaveFixture.cs:7`, `:22`; `OldLayoutSaves.cs:45`; `ClutchGeneralityTests.cs:443`; `BundleElementIdTests.cs:209-215`; `HalfPrecisionGuardTests.cs:53`; `TypedTransportCommandTests.cs:397`.
- The identifier caller list (`:144`): the `rg` counts match the named files exactly.
- `WorkshopWireBuildTests.cs:68`, `:82`, `:99`, `:123`, `:185`, `:205-296`, `:334-354`.
- `WorkshopWireFlatBuffers.cs:18-19`, `:25-56`, `:45-53`, `:58-91`, `:104-109`, and the production uses `:152`, `:158`, `:567`, `:582`; `WorkshopWireFlatBuffers.Observation.cs:11`, `:25`, `:34`.
- `WorkshopAuthoringFlatBuffers.cs:34-74`, `:76-102`, `:98-99`. `PuzzleRecord.assistance` is field 11 and `(required)`, so its presence implies a vtable of at least 28 bytes, which covers `CurrentPuzzleTable`'s 24-byte minimum together with the verifier rule's maximum.
- `WorkshopSaveCodec.cs:13`, `:17`, `:29`, `:35`, `:44-47`, `:48-55`, `:52-54`, `:59-69`; `CoreCommandCodec.cs:9`, `:15-16`, `:19`; `RuntimeCodecs.cs:9`, `:15-16`, `:19` (one implementer, `rg ": ICommandCodec"`).
- `FlatBufferVerify.cs:238`, `:445-458`, `:462-466`, `:658`, `:696`, `:781`; `FlatBufferBuilder.cs:1016`; `LICENSE:98-99`; `VendoredFlatBuffersIdentityTests.cs:5-12`, `:23`, `:27`; `CSharpStaticGateTests.cs:116`, `:118`, `:506-512`.
- `CuriousContraptions.FlatBuffers.csproj:7-9`, `:43`; `workshop_wire.fbs:306`, `:348-351`, `:357-361`, `:378-379`, `:1122`; `workshop_authoring.fbs:4`, `:13`, `:25`, `:39`, `:44`, `:57`, `:62`, `:79`, `:88`, `:23-24`, `:95`; `schemas/README.md:1-9`, `:48-52`, `:90-91`.
- `engine/RuntimeProviders.cs:13-19`; `Toolchain.cs:74-100`; the D5 fixtures `Fixtures/pre-launch/`, `pre-source-store/` and `save-f1ad3bd1-authored-puzzle.bin` exist.

## Checks that hold

| Question | Result |
|---|---|
| Budget | 5,483 words (≤ 6,000); re-measure after the repairs (A6-10 disposition). |
| Capability probe | Cited (`:175-180`). Present items carry symbols and tests (S5 and `WorkshopWireBuildTests`; S5b and `WorkshopWireTypeScriptTests`; the frame boundary and `WireFaultTests`; the seam per A7-5). Missing items are owned (here; S5c-2; H-R1 → `ENGINE-WIRE-ABI`). No required-now owner decision is open. |
| Tier | Engine, with its reason (`:122`); correct (schema, vendored runtime, codecs, build tests). |
| Owned paths | Listed one per file in the Code Map, apart from the gaps in A7-2, A7-3, A7-5, A7-8 and A7-9. |
| Grammar against the real file, after A7-1 | The only field attribute is `(required)` (84 times). The defaults are 12 `null` and one `0.0`. There is no `/*`, no `union`, no `deprecated`, no `id:` and no fixed array. `include` is only at `workshop_authoring.fbs:1`, and the second `root_type` only at `workshop_wire.fbs:1122`, both deleted by the merge. |
| S9 and S10 on the real code | They hold today: the wire file cannot name authoring declarations (include direction), and the only reference to a marked declaration or companion in Core, Simulation or Animation.Worker sources is `SavedConstructionVerify` at `CoreCommandCodec.cs:9` (`rg -lw` over the nine names and their `Verify` classes: only main-only files and that one). |
| Generators in the S10 replay | The test project compiles against the SDK's Roslyn (`CuriousContraptions.tests.csproj:41-42`), so the `[JSImport]` generators of Simulation and Animation.Worker load at the same version. |
| Removed root API | No current caller uses `WorkshopResponse`'s root-only API in C# or TypeScript (`rg`: hits only under `_bmad-output/evidence-9r-0c/`). |
| D5 reason targets | `WorkshopSaveRejection.OutOfRange` and `WireFaultReason.OutOfRange` keep production uses (`WorkshopSavePrevalidation.cs`, `WorkshopAuthoringFlatBuffers.cs:187` and others), so no enum member is orphaned. No test matches the deleted messages ("undefined fields", "holds a field the current record"). |
| Forward-only, element-free, no-hash, no-skip, TypeScript rules | No conflict in the planned design. D9 passes (above). The interim TypeScript literals are a stated evidence limit, owned by S5c-2 and S5c-3. |
| Rulings (a) to (f) | Unaffected; ruling (e) now applies 24p (A6-9 (b)). |
