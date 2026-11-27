# Planning audit: `spec-s5c-one-wire-schema-file.md` (S5c), sixth pass (mandatory dispatch re-audit)

Audited 26 November 2026, read-only, planning phase. Checkout `.claude/worktrees/slice-s5c`, branch `slice-s5c`, HEAD `39aaba70` (= published `origin/main`, which contains S5b); the only working-tree change is the contract itself (` M _bmad-output/implementation-artifacts/spec-s5c-one-wire-schema-file.md`, 9,841 words, 535 lines). Policy read from this checkout only: `AGENTS.md`; `_bmad/custom/role-cards.md` (Informed auditors); `_bmad/custom/bmad-loop-protocol.md` (Capability probe, Slice record and Contract budget, Reviewer duties, Triage, Maintainability gate); `_bmad/custom/project-engineering-contract.md`, `element-free-engine-design.md`, `element-free-engine-review.md`; `docs/testing-guidance.md`; owner decision 24p and rulings (a) to (f) (`docs/planning/elements/readiness/owner-decisions-2026-11-22.md:71-77`); audits `audit-s5c-1.md` to `-5.md`; the S5b contract and run-log entries E31, E35 to E37; `deferred-work.md:309-311`.

Scope (coordinator ruling: mandatory before the build): (1) every contract `path:line` re-read against `39aaba70`; (2) S5b code S5c must change and the contract does not name; (3) D3's premises against S5b's final code; (4) audit 5's A5-1 to A5-3; (5) the SnapshotApproval notes on the vendored verifier rule; (6) three wording points; (7) scanner spiral prevention; (8) contract budget; (9) anything else now false against `39aaba70`. Settled design outside these points was not reopened.

Measurements made: `rg`, `git diff 51d46f7b 39aaba70`, `sed -n`; one probe with the pinned compiler (`~/Library/Application Support/CuriousContraptions/flatc/25.2.10/Mac.flatc.binary/flatc`, SHA-256 `f004709c…37ac` = the pin, `flatc version 25.2.10`): `flatc --ts --gen-object-api -o .tools/s5c-evidence/audit6/ts-oapi schemas/workshop_wire.fbs schemas/workshop_authoring.fbs`. No build, test, mutant, browser or maintainability-gate run.

## Verdict: Fail

- Two medium findings are open: **A6-1** (the e2e runner's schema record and staleness check in `tools/E2eRunner/Toolchain.cs` are left reading what S5c stops writing) and **A6-2** (the contract's concurrency section omits lane 3's E2E runner process lifecycle slice, which is now dispatchable and shares runner files S5c must edit; owner decision 24m).
- Nine low findings (A6-3 to A6-11), each with exact repair text.
- Audit 5: A5-1 Closed, A5-3 Closed, A5-2 Open in one part (the "four-round planning cap" clause), repaired by A6-9.
- All repairs are contract or record text; none reopens settled design. A bounded re-check of A6-1 and A6-2 (and a skim of the low repairs) suffices before dispatch.

This is a planning audit. It is not an implementation pass, and it is not build, browser or release qualification.

**Lane-3 files flag (prominent).** No S5c change needs `tools/E2eRunner/RunnerError.cs`, `ProcessTable.cs`, `SuiteProcess.cs`, `PreviewHost.cs` or `Scheduler.cs`, nor `ProcessesTests.cs` or `SchedulerTests.cs`: `Toolchain.cs` uses `RunnerError.ToolchainMissing` unchanged. But S5c must edit `tools/E2eRunner/Toolchain.cs`, `tools/E2eRunner/SourceIdentity.cs`, `tools/E2eRunner.Tests/TypeCheckBoundaryTests.cs`, `tools/E2eRunner.Tests/IdentityTests.cs` and `tools/E2eRunner.Tests/E2eRunner.Tests.csproj`, and lane 3's slice owns the preflight's `node` and Chrome start failures (`Toolchain.ResolveParts`, `Toolchain.cs:37-41`), the suite digest's coverage (EFE17-F6, `SourceIdentity`) and deterministic trigger tests on `IdentityTests` dispose paths (`deferred-work.md:311`). See A6-2.

## Audit 5 dispositions

| Finding | Disposition | Evidence |
|---|---|---|
| A5-1 (low): A4-1 move leaves fixture and texts behind | **Closed** (citation drift only) | Code Map `:254` names `tools/runtime/FlatcCompiler.targets`, adds it to the vendored fixture's `SharedBuildFiles`, names `schemas/README.md:41` (re-read: "digests pinned in `CuriousContraptions.FlatBuffers.csproj`") and the vendored comments `:5-8`, `:55-56`, `:124-126` (re-read: all three still describe provisioning in the project; `:55` now reads "Runs only from GenerateWireAccessors and GenerateWireTypeScript"). `SharedBuildFiles` moved from `WorkshopWireBuildTests.cs:31-32` to `:37-38` (A6-11). |
| A5-2 (low): Spec Change Log and spelling records | **Open in part** | Repaired: the A4-5 entry (`:458`) now lists the Always `:69` rewording and the cross-referenced `WorkshopSaveCodec.cs` bullets; `:75` says the foreign word is "now passed as `(WireRootIdentifier)Foreign`"; `:253` applies the marked anchor to every `table SavedConstruction {` case, S5b's `:82` included (re-read: `WorkshopWireBuildTests.cs:68` and `:82` are the only two anchors, `rg -n "table SavedConstruction \{"`). Not repaired: `:459` still asserts "after the four-round planning cap", which no rule in this checkout sets (A5-2's second bullet asked to cite or drop it). Repair: A6-9 (c). |
| A5-3 (low): S3 read frame bytes | **Closed** | Row S3 (`:139`) and its test row (`:522`) now admit the finisher-written save through `ICommandCodec.ValidateSaveFrame` (Core compares against the converted `WireRootIdentifier.SavedConstruction`) and refuse it re-finished with another member's converted identifier; "No frame bytes are read by the test". This can fail when the `file_identifier` differs from the member, and agrees with Always `:94` and Never `:130`. |

## New findings

### A6-1 (medium, wrong specified behaviour): the runner's schema record and staleness check are left reading what S5c stops writing

Evidence:
- S5b's `GenerateWireTypeScript` has **two** Outputs, the entry module and the record `tools/e2e/.wire/schemas.txt` (`CuriousContraptions.FlatBuffers.csproj:174`, written at `:194`, moved at `:201`). The contract's moved target has "only Output ... the entry module" (`:106`) and never mentions the record.
- The file that holds the runner's schema-name and staleness check is **`tools/E2eRunner/Toolchain.cs`**: constants `:13-15` (`GeneratedWireEntry`, `GeneratedWireRecord`, `WireGeneratingProject` = the vendored project file), `RequireGeneratedWireModule` `:74-100` (record read and name-set comparison `:93-96`; staleness against `schemas/*.fbs` and the vendored project file `:97-99`), and the remedy texts "a Debug build of the solution (or of CuriousContraptions.FlatBuffers)" at `:26`, `:84`, `:96`, `:99`. The contract names `Toolchain.cs` only for the runtime check (`:242`).
- Faithful implementation therefore leaves (a) a runner that reads a record no build writes (the read throws, and `Resolve`'s one IO handler at `:23-27` refuses every browser-phase run); (b) a staleness rule keyed to a file that no longer declares generation, so a change to the moved generation command is not seen as stale; (c) wrong remedy text. It also fails the real-tree T1 check, whose `SchemaRecordViolations` (`WorkshopWireTypeScriptTests.cs:127-138`) reports "the schema record schemas.txt is missing", and the contract's T1 amendment (`:267`) does not cover that. Under the forward-only rule, a check for an artifact that no longer exists is a second, dead rule.

Repair (add to the Code Map, "Runtime pin" bullets):

> - `tools/E2eRunner/Toolchain.cs` `RequireGeneratedWireModule` (`:74-100`): the schema record is deleted. With one schema file (S1), a name-set comparison has nothing to compare, so `GeneratedWireRecord` (`:14`) and the comparison (`:93-96`) are deleted, and the generation target writes no `schemas.txt`. The presence check stays. The staleness check compares the entry module with `schemas/workshop_wire.fbs`, `tools/runtime/RuntimeTypeScript.targets` (the file that declares generation) and `tools/runtime/FlatcCompiler.targets`. `WireGeneratingProject` (`:15`) is replaced by those two paths. The remedy texts at `:26`, `:84` and `:99` name "a build of the solution (or of CuriousContraptions.Abstractions)". The summary at `:74-78` is reworded.
> - `tools/E2eRunner.Tests/TypeCheckBoundaryTests.cs` `TheGeneratedWireEntryModuleMustExistBeforeSuitesRun` (`:129-221`; constants `:230-232`): the record cases (`:160-174`, `:202-214`) are deleted, the project-file cases (`:155-159`, `:182-200`) are retargeted to the two declaring files, and the expected texts change with the remedy.
> - `tools/E2eRunner.Tests/IdentityTests.cs` `TheSuiteDigestCoversTheGeneratedWireModulesAndNoOtherIgnoredFile` (`:176-212`): the record step (`:192-193`, `:203-205`) is replaced by a control that a change to `tools/runtime/wire-frame.ts` changes the digest (the `SourceIdentity.cs` change at Code Map `:205`).
> - `WorkshopWireTypeScriptTests` T1: `SchemaRecord` (`:19`), `SchemaRecordViolations` (`:127-138`) and their controls (`:59-68`) are deleted; `WorkshopWireBuildTests` T3's record case (`:155-157`) is deleted; `schemas/README.md:64`, `:69-71` are reworded.

### A6-2 (medium, record/concurrency): lane 3's runner slice shares files S5c must edit, and the contract does not say so

Evidence:
- `TODO.md:12`: "Next in lane 3: the E2E runner process lifecycle repair slice, dispatched by the coordinator straight after S5b publishes". S5b published as `39aaba70` (`TODO.md:11`; run log E37), so that slice can run now, beside S5c.
- `deferred-work.md:311` (its scope): "it shares S5b's files"; "program start failures as typed refusals, including ... the preflight's `node` and Chrome" (`Toolchain.ResolveParts`, `Toolchain.cs:37-41`); "the suite digest's coverage of inputs outside it changed after the preflight (EFE17-F6)" (`SourceIdentity`); deterministic trigger tests for `PreviewHost.Stop` on the dispose paths of three `IdentityTests` facts. `Processes.Run` is in `RunnerError.cs` (`grep -l "static class Processes"`).
- S5c must edit `Toolchain.cs` (Code Map `:242`; A6-1), `SourceIdentity.cs` (`:205`), `TypeCheckBoundaryTests.cs` (`:244`; A6-1), `IdentityTests.cs` (A6-1) and `E2eRunner.Tests.csproj` (A6-3 (c)).
- Owner decision 24m (`owner-decisions-2026-11-22.md:67`): "A lane is allowed only when its owned files are disjoint from every other running lane's code; two slices that edit the same code file are one lane." The contract's Concurrency section (`:379-389`) names S5, S5b and lane 1 only.

Repair (append to Concurrency, and record the coordinator's choice in the run log):

> Lane 3's E2E runner process lifecycle slice (`deferred-work.md`, "E2E runner process lifecycle") may edit `tools/E2eRunner/Toolchain.cs`, `tools/E2eRunner/SourceIdentity.cs`, `tools/E2eRunner.Tests/IdentityTests.cs` and `tools/E2eRunner.Tests/TypeCheckBoundaryTests.cs`, which S5c also edits. S5c edits none of `RunnerError.cs`, `ProcessTable.cs`, `SuiteProcess.cs`, `PreviewHost.cs`, `Scheduler.cs`, `ProcessesTests.cs` or `SchedulerTests.cs`. Under owner decision 24m the two slices may not edit the same file concurrently. At dispatch the coordinator compares the two owned-file lists. Where they overlap, it either runs them one after the other (the second starts from the main that contains the first), or moves S5c's runner edits (A6-1) into whichever slice runs first. The choice is recorded in this contract's run log.

### A6-3 (low, record): S5b code that S5c must change or delete and the contract does not name

Each item gives the location, then what S5c must do.

| # | S5b code at `39aaba70` | S5c action |
|---|---|---|
| a | `CuriousContraptions.FlatBuffers.csproj:151-168` (comment), `:169-172` (`WireTypeScriptDirectory`, `_WireTypeScriptStaging`), `:173-205` (`GenerateWireTypeScript`, `Condition="'$(Configuration)' == 'Debug'"` at `:173`), `:207-209` (`RemoveWireTypeScriptStaging`), `:211-275` (`OrderWireSchemas`: the two `-M` runs `:237`, `:258`; the file-name rule `:229-232`; unknown-include, no-root, two-root and unreached `Error`s `:247-270`) | Delete all from the vendored project. `ListWireSchemas` (`:118-122`) stays for C#. Code Map `:184` says "its include-ordering logic and its two `Error` branches": state explicitly whether the stray-output and missing-entry refusals (`:182-187`) **move with the target** (recommended: they cost two lines and protect the set the entry module names; their test is the stray case `WorkshopWireBuildTests.cs:253`, which then moves to the Abstractions-copy fixture) or are deleted with T1's "no other file" check as their cover. |
| b | The moved chain's own staging-cleanup `OnError` target | Add it to `PinnedTargets` beside the generation and stamp targets (Code Map `:188`), with `DeleteFlatcTemporary` and `ProvisionFlatBuffersCompiler` under `tools/runtime/FlatcCompiler.targets:<name>` keys. |
| c | `tools/E2eRunner.Tests/E2eRunner.Tests.csproj:21-22` (ProjectReference to the vendored project "to build the generated TypeScript wire modules") | Delete. The existing Abstractions reference (`:19-20`) now generates `.wire/`. |
| d | `tools/e2e/workshop-wire.ts:1-4` ("CuriousContraptions.FlatBuffers generates them from schemas/*.fbs") and `:13` | Reword with the H-S3 to H-S5 edits. |
| e | `_bmad/custom/bmad-loop-protocol.md:251` ("its build of the vendored FlatBuffers project does write the checkout's ignored `tools/e2e/.wire/` (S5b)") | Reword to the Abstractions instance (a shared document under 24m). Add it to Planning records. |
| f | T7 reads the compiler version from the vendored project (`WorkshopWireTypeScriptTests.cs:403`) | S12 reads `<FlatcVersion>` from `tools/runtime/FlatcCompiler.targets`. |
| g | T8 is `WorkshopWireTypeScriptTests.cs:487-557` (with `GraphLogs`, `Replay`, `Target`, `Task`, `Context`), not a `WorkshopWireBuildTests` case as Code Map `:260` files it | Delete it there. The class summary `:7-12` and constants `:21-22` are reworded. |
| h | T5 `TheHelpersDeclareNoSchemaTypeAndNoImporterTakesOneFromThem` (`:156-397`) | Keep. Its scope is `tools/e2e/*.ts` and `unit/*.ts` only. State that `tools/runtime/wire-frame.ts` and `wire-conformance.ts` are outside it and are covered by S11 and "holds no field, type or slot name of its own" (Code Map `:197`). |
| i | `TimingDetectorTests.cs:129-158` (T9: `.js` to `.ts` closure) | Keep, unchanged. The closure follows relative imports anywhere (`TimingDetector.cs:271-296`), so `workshop-wire.ts` importing `../runtime/wire-frame.ts` resolves. |
| j | `WorkshopWireBuildTests.cs:99` and `:123` (touch `workshop_authoring.fbs`), `:223-244`, `:346` | `:99` and `:123` are retargeted by the generic `rg` rule (`:252`). `:223-244` and `:346` leave with the deleted cases. Listed so the run log can dispose them by line. |
| k | `CuriousContraptions.tests/WorkshopFormatTests.cs:180` (`WorkshopSaveCodec.Identifier`, inside the historical region `:160-181`) | Not in the caller list (`:226`). It takes the converted `WireRootIdentifier.SavedConstruction`, like `OldLayoutSaves.cs:45`. |

Repair: add rows (a) to (k) to the Code Map with these actions. For (a), choose and state one of the two options.

### A6-4 (low, test): T1's amendment does not match the object-API output

Evidence (probe `.tools/s5c-evidence/audit6/ts-oapi/`): with `--gen-object-api` the entry module line becomes `export { ActivationMemoryRecord, ActivationMemoryRecordT } from './wire/activation-memory-record.js';` (all 140 lines are this form), and the type declaration becomes `export class InstanceRecord implements flatbuffers.IUnpackableObject<InstanceRecordT> {` plus `export class InstanceRecordT implements flatbuffers.IGeneratedObject {` (also for structs: `Vec3f`, `Vec3fT`). `EntryExport` (`WorkshopWireTypeScriptTests.cs:105`, `^export \{ (\w+) \} from …`) and `TypeDeclaration` (`:106`, `^export (?:class|enum) (\w+) \{`) match neither, so T1 reports every schema type as missing. `EntryExports` (`:100-101`) is used by `WorkshopWireBuildTests.cs:666`, and `ARemovedSchemaTypeLeavesNoTypeScript` asserts `export class S5bRemovedProbe {` (`:195`). The object API adds no `any` (probe: only the 10 normalised `optionalEncoding?:any` sites and the suppression comment).

Repair (replace Code Map `:267`):

> S5b's T1 (`TheGeneratedModuleExportsEverySchemaTypeWithoutAny`) is amended for `--gen-object-api`. Each entry line exports `<Name>`, plus `<Name>T` for every table and struct; enums export the name alone. Each type file declares `export class <Name> implements flatbuffers.IUnpackableObject<<Name>T> {` and `export class <Name>T implements flatbuffers.IGeneratedObject {` (an enum declares `export enum <Name> {`). `EntryExport`, `TypeDeclaration`, `EntryExports` and their controls (`:43-58`) follow. The moved `ARemovedSchemaTypeLeavesNoTypeScript` asserts the new declaration form.

### A6-5 (low, wrong specified behaviour/test): the verifier rule's record, its direct proof and its schema premise

Evidence and repairs, by SnapshotApproval note:

1. **Notice and upgrade.** `:85` says the pin and `THIRD-PARTY-NOTICES.md` "are updated with it", but not that an upgrade must re-apply it. `VendoredFlatBuffersIdentityTests.cs:12` says "Upgrade the runtime as a complete pinned set …; retain or supersede this correctness proof", and the vendored project's header (`CuriousContraptions.FlatBuffers.csproj:7-9`) says "Upgrade by replacing every source file from a newer tag". A plain replacement would silently drop the rule. Repair (append to the `:85` bullet):

   > `THIRD-PARTY-NOTICES.md` records the rule as a modification of `FlatBufferVerify.cs`: the per-table slot record, the refusal in `VerifyTableEnd`, and the reason. It states that any runtime upgrade re-applies this and Story 9A.45's patches, and that the direct verifier tests (below) fail if one is dropped. The vendored project's header (`:7-9`) and `VendoredFlatBuffersIdentityTests.cs:5-12` say the same.

2. **Gate exclusion and direct tests.** The vendored directory is outside both gates. Analyzers are off through `CuriousContraptions.FlatBuffers/Directory.Build.props:5` (`CSharpStaticGate=false`) and `.editorconfig` `[CuriousContraptions.FlatBuffers/**.cs] generated_code = true` (pinned at `CSharpStaticGateTests.cs:144-145`). The gate's source and line scans skip `IsVendored` (`CSharpStaticGateTests.cs:239`, used at `:573`, `:597`, `:1092`, `:1314`, `:1391`); its MSBuild files are pinned whole by hash (`:116-117`). The maintainability gate excludes it by the same property (`tools/anvil/VerifyPolicy.cs:1306-1308`). So the new rule's complexity and analyzer findings are never scored, and S8 proves it only through the codecs. Repair (add to the Code Map and the acceptance-to-test table):

   > `CuriousContraptions.tests/VendoredFlatBuffersVerifierTests.cs` (new) tests `Verifier` directly. It uses generated `<Name>Verify.Verify` actions of small schema tables, with slots from `WireSchemaLayout` and no codec. Positive: a vtable of exactly the table's fields, and a shorter one (trailing absent fields), are admitted. Control: one appended non-zero entry, and one appended zero entry, are refused. Boundary: a child table with an appended field inside an exact parent is refused; a parent whose appended field follows a fully verified child is refused (the stack does not let the child's record stand for the parent); a table reached through a vector of tables is refused; and one `Verifier` reused for an admitted buffer after a refused one admits it (the per-table record resets in `VerifyBuffer`). `CompoundMassAndRollingTests.cs:898-900`'s inversion stays.

3. **Union, `deprecated` and `id:`.** The rule itself needs only that the generated `Verify` checks every slot a current writer can write. The pinned compiler verifies every non-deprecated field, a union through both offsets of `VerifyUnion` or `VerifyVectorOfUnion` (`FlatBufferVerify.cs:658`, `:696`). So the rule must record the highest offset passed to every per-field method, both union offsets included. What does assume their absence is `WireSchemaLayout` (slot = declaration position), S9's walk (union arms) and task 1(d)'s `4 + 2 * (n - 1)` measurement. Today only `WireSchemaLayout` refuses `deprecated` and `id:`, and only per table (`:203`). No one place refuses a `union`; T1's entry-export form refuses one only indirectly (`WorkshopWireTypeScriptTests.cs:103-104`), and A6-4 rewrites that form. Repair (replace `:203`'s second sentence, and add to the `:82` bullet):

   > `WireSchemaText` refuses the whole schema, naming the construct and its line, if it declares a `union`, or carries a `deprecated` or `id:` attribute anywhere (closed grammar in A6-8). `WireSchemaLayout`, S9 and the `Verify` coverage premise rely on that refusal, so it is made once, in the parser, not in generation. MSBuild has no parser, and a text check there would be a second scanner. Control, in `WireSchemaTextTests`: three synthetic schema texts, one with each construct, are each refused, naming the construct and line; the real file is admitted.
   >
   > (`:82`) The rule records the highest vtable offset passed to every per-field `Verify*` method of the table being verified, both offsets of `VerifyUnion` and `VerifyVectorOfUnion` included.

### A6-6 (low, product defect, pre-existing): Apache-2.0 §4(b) in-file change notices

Evidence: `CuriousContraptions.FlatBuffers/LICENSE:98-99`, §4(b): "You must cause any modified files to carry prominent notices stating that You changed the files; and". Story 9A.45's patches:
- `FlatBufferVerify.cs` carries one inline comment at one change site (`:238`, "Local correctness patch (9A.45): …"), below the unchanged Google header (`:1-15`). That is not a prominent file-level notice.
- `Table.cs` carries no notice at all (header `:1-15` unchanged; changed lines `:44-45`, `:50-51` unmarked).

`THIRD-PARTY-NOTICES.md:16-21` describes the patches, but §4(b) requires the modified files themselves to carry the notice, so the record alone does not satisfy it. This is a pre-existing confirmed defect.

Repair: S5c edits `FlatBufferVerify.cs`, its pin and the notices anyway, so it adds the notices to both files. Add to the Code Map:

> `CuriousContraptions.FlatBuffers/FlatBufferVerify.cs` and `Table.cs` each gain, directly after the Google licence header, a notice: "Modified by the Curious Contraptions project: <the file's changes: Story 9A.45's checked vtable positions (both files); S5c's unknown-field rule (FlatBufferVerify.cs)>. See THIRD-PARTY-NOTICES.md." `VendoredFlatBuffersIdentityTests.cs` re-pins `:23` and `:27` (the `Table.cs` pin changes for this comment only) and its summary `:5-12` says both files carry the notice. `CSharpStaticGateTests.VendoredFiles` takes `Pinned` (`:118`), so it needs no separate edit.

### A6-7 (low, test/record): snapshot value spellings and BH3-4's fixture

Evidence: the snapshot reports values as strings, an f32 as its bit pattern (`tools/e2e/run-reset-snapshot.ts:210`, `` `0x${…padStart(8,'0')}` ``), and the suites assert them: `cat-050-joined.test.ts:294` `'0x40666666'`, `:188` `'1'` for a boolean, and `cat-020.test.ts:105-107` (`===` against a string). The contract moves values to the object API compared with `Object.is` (`:235`) and says only that suite **path** strings change (`:238`). BH3-4 (`deferred-work.md:310`) also names a second cause: "the test's fixture never sets the required `PuzzleRecord.assistance` field". H-S2 (`:237`, `:298`) does not repair it.

Repair:
- At `:238`, after "follow the derived path spelling", add: ", and their asserted values follow the derived value form that `snapshotValue` returns (stated in `run-reset-snapshot.ts`), for example `cat-050-joined.test.ts:294`'s f32 bit pattern".
- At `:237`, add: "Its fixture sets the required `PuzzleRecord.assistance` (BH3-4's second cause). BH3-4 closes for both causes in the record, and its re-check stays with the browser phase."

### A6-8 (low, test/spec): close the accepted input of each new scanner

S5b's two text scanners reopened for many passes because each repair widened what they accepted. The contract defines none of S5c's four scanners as a closed form.

- **`WireSchemaText`** (`:202-204`): "declarations, fields in order, marks and references". The accepted grammar is not stated, and the real file needs a token-based reader: fields share lines (`workshop_wire.fbs:359-361`, `:381-383`), tables fit on one line (`:356-357`), and comments hold parentheses (`:348-351`). Repair (append to `:202`):

  > It accepts exactly this subset and refuses anything else by name and line. Comments: `//` to end of line (`///` included); `/*` is refused. Top level: exactly one `namespace CuriousContraptions.Wire;`; exactly one `attribute "authoring";`; `enum Name : <integer scalar> { Member = <decimal or 0x literal>, … }`; `struct Name [(authoring)] { field: <scalar or declared struct or enum>; … }`; `table Name [(authoring)] { field: <type> [= <literal | null | true | false | member of the field's enum>] [(required)]; … }`; exactly one `root_type Name;`; exactly one `file_identifier "XXXX";`. A type is a FlatBuffers scalar name, `string`, a declared name, or `[` one of those `]`. A field's only attribute is `required`, and a declaration's only attribute is `authoring`. Refused: `include`, `union`, `rpc_service`, `file_extension`, any other attribute (`deprecated`, `id:`, `key`, `nested_flatbuffer`, `force_align`, `bit_flags`, …), fixed arrays `[T:n]`, a second namespace, and a type name that resolves to no declaration. `CSharpStaticGateTests.SchemaTypesIn`, `BundleElementIdTests` (`:209-215`, whose line regex misses `(required)` and shared-line fields), `WorkshopFormatTests` (`:20-38`) and S1 read only its model; no other schema reader remains.

- **S9** (`:145`): closed by the parser. Append: "The walk follows the parser's resolved field types (a vector's element type included) from each runtime root. An unresolved name cannot occur (the parser refuses it). Its stated limit is the opaque byte vectors."
- **S10** (`:146`): Core's compile is clean of generators, but Simulation and Animation.Worker use `[JSImport]`/`[JSExport]` (`CuriousContraptions.Simulation/Program.cs`, `Program.Schedule.cs`, `CuriousContraptions.Animation.Worker/Program.cs`). Their partial members get their bodies from a source generator, so a semantic model built from the command line alone reports CS8795-class errors. The contract does not say whether errors are refused (the guard then fails on real code) or ignored (binding can then miss silently). Repair (append to `:146`):

  > The replay parses each `csc` command line with Roslyn's `CSharpCommandLineParser`, not a hand tokenizer. It runs the generators from the command line's analyzer references through `CSharpGeneratorDriver`, and refuses (naming project, file and diagnostic) any replayed compile whose model then reports an error, so a binding gap cannot hide a reference. It inspects every type symbol bound in every syntax tree, generator output included: type syntax, member access, object creation, `typeof`, `nameof`, attribute arguments and generic arguments. XML doc `cref`s are not inspected (a stated limit).

- **S11** (`:147`): "no hand field-name or slot list", "no vtable arithmetic" and "exactly one TypeScript frame check" are open-ended. Repair (replace S11's expected-output cell with a closed list):

  > Outside the allow-list:
  > (1) none of these names: `SNAPSHOT_SCHEMA`, `TheVersionlessRecordsDeclareExactlyTheirFields`, `InstanceRecordFields`, `SavedConstructionSlot`, `ConstructionInstancesSlot`, `HoldsFieldBeyondTheInstanceTable`, `ValidateBindingShapes`, `TableTarget`, `COMMAND_IDENTIFIER`, `RESPONSE_IDENTIFIER`, `SAVE_IDENTIFIER`, `CommandIdentifier`, `ResponseIdentifier`, `ObservationIdentifier`, and `enum Field` in `WorkshopSaveRejectionTests.cs`;
  > (2) no `\b(CWCM|CWRM|CCWO|CCWS)\b`, in code or comments;
  > (3) C#, via Roslyn syntax: no integer literal as the argument of `StartTable`, as the slot argument of a `FlatBufferBuilder.Add*` call, or as the argument of `__offset`; and no binary expression `4 + 2 * x` or `x * 2 + 4`;
  > (4) TypeScript, via regex: no integer literal as the first argument of `startObject` or `addField*`, or the second of `__offset`;
  > (5) the only hand TypeScript file that calls `__has_identifier` or reads `SIZE_PREFIX_LENGTH` is `tools/runtime/wire-frame.ts`.
  >
  > Markers: `// historical layout: begin` and `// historical layout: end` must pair, must not nest, and appear only in the files H-T3 and H-T4 name, with the number of regions pinned per file.
  >
  > Stated limit: a new restatement under a new name is review's to catch.

### A6-9 (low, record): the three wording points (closes A5-2's remainder)

(a) **Confirmed.** In `owner-decisions-2026-11-22.md`, `- (f) …` (`:77`) is followed directly by `38. …` (`:78`). Under CommonMark an ordered list can interrupt a paragraph only when it starts at 1, so `38.` to `43.` (`:78-83`) are lazy continuation lines of bullet (f)'s paragraph. Repair: insert one blank line between `:77` and `:78`. 38 to 43 then render as an ordered list starting at 38. (The same rule merges `24j.` to `24p.` (`:64-71`) and `24a.`/`24.` (`:40-41`) into single paragraphs. That predates 24p; blank lines between those entries fix it in the same edit.)

(b) **Confirmed.** 24k is an owner decision (`:65`); a coordinator ruling cannot supersede it. Replace `:76` with:

> - (e) Applying 24p, S5c delivers the unknown-field refusal derived from generated code. Owner decision 24k gave that refusal to `ENGINE-WIRE-ABI`; 24p, the later owner decision, governs, and S5c delivers it. `ENGINE-WIRE-ABI` keeps 24k's other boundary work. Coordinator decision 38's listing of "the field-count refusal" under `ENGINE-WIRE-ABI` is amended accordingly.

(c) **Confirmed.** No rule in this checkout records a planning cap (audit 5, A5-2). Replace `:459`'s second sentence with:

> The coordinator authorised a bounded fifth check, limited to the audit 4 repairs.

Replace `:464` with:

> These repairs are re-checked at the mandatory dispatch re-audit (coordinator ruling, 25 November 2026; `audit-s5c-6.md`). It runs against the main that contains S5b and is bounded to citations, S5b's code and these repairs. It is not a further open planning round.

The Code Map "Planning verdict" line (`:168`) is then updated by the coordinator (a record update, not a finding).

### A6-10 (low, record): contract budget

Measured with `wc -w` and per section: frontmatter 49; Intent 473; Boundaries 1,891; I/O matrix 1,033 (these three are the read-only intent contract, 3,397); Code Map 1,968; inventory 744; probe 111; Decisions 1,040; Protections 137; Concurrency 89; Tasks and ACs 535 (ACs 151); Spec Change Log 637; Design Notes 430; Verification 112; acceptance-to-test table 588. Total 9,841, against the 6,000 planning budget (`bmad-loop-protocol.md:88`).

- **Movable to the run log without cutting protected content** (estimate about 2,300 words):
  - Spec Change Log 637 → about 80: one line per audit, citing the audit file.
  - Decisions 1,040 → about 330: three lines each; the alternatives go to the run log.
  - Design Notes 430 → about 150.
  - The test-by-test dispositions in the Code Map (`:224-268`), about 700 → about 250, as one line per file.
  - Task 1's sub-gate prose → about 150.
  - Protections → about 80.
- **What remains.** The intent contract, KEEP constraints and acceptance rows (matrix, ACs, table) are about 4,140 words and are never cut. The irreducible owned-scope list (one line per file, the inventory row IDs and owners, the probe, concurrency and commands) is about 2,600 to 2,800. The contract therefore lands near 6,800 to 7,000. **The acceptance rows alone fit** (about 1,770), but intent plus acceptance plus owned scope does not.
- **Recommendation.** Split at dispatch along the contract's own boundaries (`:172`). Each part keeps its rows, and the parts run in this order in lane 2:
  - **S5c-1** (schema, generation move, C# refusal): tasks 1 to 3; rows S1, S2, S8, S9, S10, S13 and the C# halves of S3, S6, S7; A6-1 and A6-3.
  - **S5c-2** (conformance and root pin): task 4; rows S3 to S7 and S12.
  - **S5c-3** (hand restatements): task 5; rows S11 and S14, the H-T and H-S rows, and A6-7.

  The alternative is a recorded coordinator decision to carry the `oversized` warning (frontmatter `:16`) at about 6,900 words after the moves above.

### A6-11 (low, record): citation corrections

Measured against `39aaba70`; see the table below. The Code Map's blanket note (`:156-164`, "Re-read every citation at dispatch") is replaced by the corrected numbers. `:156` "every `path:line` below was re-read [at `51d46f7b`]" becomes "re-read at `39aaba70` by `audit-s5c-6.md`". `:158-162` (S5b "in flight") becomes past tense: "S5b published as `39aaba70`".

## Citation correction table

Files with no diff between `51d46f7b` and `39aaba70` (`git diff --stat`: every cited `engine/`, `schemas/*.fbs`, `CuriousContraptions.Core/`, `RuntimeCodecs.cs`, the H-W4 Abstractions files, `FlatBufferVerify.cs`, `FlatBufferBuilder.cs`, `THIRD-PARTY-NOTICES.md`, `tools/runtime/`, root `package.json`, `tools/e2e/run-reset-snapshot.ts`, `workshop-observation.ts` and the unlisted suites and tests) keep their audited lines. Each was spot-read and **holds**. Among them:
- `WorkshopWireFlatBuffers.cs:18-19`, `:25-56`, `:45-53`, `:58-91`, `:104-109`;
- `WorkshopAuthoringFlatBuffers.cs:34-74`, `:76-102`, `:98-99`, `:230-231`;
- `WorkshopSaveCodec.cs:13`, `:17`, `:29`, `:35`, `:44-47`, `:48-55`, `:52-54`, `:59-69`, `:61`;
- `CoreCommandCodec.cs:9`, `:15-16`, `:19`; `RuntimeCodecs.cs:9`, `:15-16`, `:19`;
- `FlatBufferVerify.cs:445-458`, `:462-466`, `:781`; `FlatBufferBuilder.cs:1016`;
- `workshop_wire.fbs:306`, `:348-351`, `:357-361`, `:378-379`, `:1122`; `workshop_authoring.fbs:1`, `:95`;
- `RuntimeTypeScript.targets:23`, `:29-33`; root `package.json:10`; `Directory.Build.targets:41`;
- `run-reset-snapshot.ts:5-6`, `:13-146`, `:150-153`, `:156-158`, `:167`, `:182-293`, `:321`, `:371`;
- the four H-W4 enums (`WorkshopCommandKind` has 14 members);
- `pages.yml:43`; `CuriousContraptions.web.csproj:47-52`; `RuntimeProviders.cs:13-19`; `CuriousContraptions.Core.csproj:20`; `docs/delivery-workflow.md:38-53`.

Moved or wrong citations:

| Contract cites | Actual at `39aaba70` |
|---|---|
| `tools/e2e/workshop-wire.ts:12734-12736` (identifier constants) | `:14-16` |
| `workshop-wire.ts:12740-12749` (`openFrame`) | `:20-29` |
| `workshop-wire.ts:12812-12871` (`dumpAuthored`, `authoredState`) | `:92-151` |
| `workshop-wire.ts:12874-13101` (mock encoders) | `:153-381` (file is 381 lines) |
| `workshop-wire.ts:12799` (`CCWS` message) | `:79` |
| `tools/e2e/workshop-driver.ts:145`, `:935` | `:139`, `:929` |
| `tools/e2e/unit/run-reset-snapshot.test.ts:17-18` | `:17-22` (S5b added `GENERATED_FOLDER`/`GENERATED_FILES` at `:19-22`) |
| `run-reset-snapshot.test.ts:83` (`'CCWS'`) | `:86` |
| `run-reset-snapshot.test.ts:106-184` | `:106-186` |
| `cat-020.test.ts:104-106`, `:302` | `:105-107`, `:303` |
| `cat-050-joined.test.ts:187-293` | `:188-294` |
| `DrivenSurfaceBindingTests.cs:512-552` | `:513-553` |
| `MechanicalCoordinateBindingsTests.cs:1011-1044` | `:1019-1052` |
| `WorkshopWireBuildTests.cs:31-32` (`SharedBuildFiles`) | `:37-38` |
| `WorkshopWireBuildTests.cs:61` and `:77` ("edit and touch") | Edits at `:68` (S5) and `:82` (S5b T2); touches at `:99` (S5) and `:123` (S5b T3); `:61` is the S5 test's `[Fact]` |
| S5b's T8 filed under `WorkshopWireBuildTests.cs` (`:260`) | `WorkshopWireTypeScriptTests.cs:487-557` |
| S5b's T1 | `WorkshopWireTypeScriptTests.cs:26-154` (holds by name) |
| `tools/E2eRunner/SourceIdentity.cs` "S5b's `:36-38`" | `:37-39` (`IsSuiteSource`) |
| `tools/E2eRunner/BrowserDeferral.cs` "S5b's `:18` and `:43-45`" | `:18-20` (phrase at `:19`) and `:44-45` |
| `tools/e2e/README.md` "S5b's two phrases" | `:68` and `:82` |
| `Toolchain.cs` "S5b's runtime check" | `RequireE2eRuntime` `:59-72`, `PinnedRuntime` `:110-130`; the schema-record and staleness check is `RequireGeneratedWireModule` `:74-100` (A6-1) |
| `TypeCheckBoundaryTests.TheE2eFlatBuffersRuntimeMustBeThePinnedVersionInstalledInToolsE2e` | `:31-93` (holds by name; its "copy above `tools/e2e`" control `:59-60` inverts to "a copy only in `tools/e2e/node_modules`") |
| `schemas/README.md:48-53` | `:48-52` |
| `schemas/README.md:55-78` (TypeScript section) | `:54-88` (S5b rewrote it: `-M` ordering, `schemas.txt`, Debug-only, `tools/e2e/package.json`) |
| `schemas/README.md:77-78` ("also update" reminder) | `:90-91` |
| `schemas/README.md:3-9`, `:41` | holds |
| `RuntimeTypeScript.targets:22-33` (D3) | `:23-33` |
| `RuntimeTypeScript.targets:23-40` (Code Map) | `:23-52` (`BuildRuntimeTypeScript` ends `:52`) |
| `CuriousContraptions.FlatBuffers.csproj:43`, `:57`, `:114`, `:23-41`, `:127-133`, `:128`, `:5-8`, `:55-56`, `:124-126` | holds |
| `CSharpStaticGateTests.cs:116`, `:1335-1364`, `:1368-1377`, `:506-512` | holds (`SchemaTypesIn` itself `:509-511`) |
| `VendoredFlatBuffersIdentityTests.cs:5-12`, `:23` | holds |
| `WorkshopSaveRejectionTests.cs:17-20` | `:17-21` (summary `:17`, enum `:18-21`) |
| `WorkshopFormatTests.cs:30-31`, `:96-123`, `:137`, `:160-181`, `:20-38` | holds |
| `DeclaredOrientationTests.cs:137-138`, `:140`, `:148-151`, `:154-156`, `:157-181`, `:168-180` | holds |
| `CompoundMassAndRollingTests.cs:880-896`, `:896`, `:898-900` | holds |
| `WorkshopSaveTests.cs:398-401`, `SaveFixture.cs:7`, `:22`, `F32SaveBoundaryTests.cs:12`, `:48`, `GoalSignalSelectorTests.cs:81`, `OldLayoutSaves.cs:45`, `ClutchGeneralityTests.cs:443`, `WorkshopWireFlatBuffersTests.cs:43`, `:46`, `BundleElementIdTests.cs:209-215`, `HalfPrecisionGuardTests.cs:53`, `TypedTransportCommandTests.cs:397`, `EventRingConsumerTests.cs:761-773`, `NodeHarnessTests.cs:12-29` | holds |
| `owner-decisions-2026-11-22.md:71-78` | holds (rulings `:72-77`; `38.` at `:78`) |
| `deferred-work.md:309-310` | holds (`:309` BH2, `:310` BH3-4; lane 3's entry `:311`) |

Dispatch measurements, hits the contract does not name:
- `rg -n workshop_authoring`: `WorkshopFormatTests.cs:100` (inside the deleted H-T1); `WorkshopWireBuildTests.cs:99`, `:123` (touches; generic rule); `:223-244` and `:346` (in deleted cases); `WorkshopAuthoringFlatBuffers.cs:76` (deleted range). The rest are named.
- `rg -n "CommandIdentifier|ResponseIdentifier|ObservationIdentifier|WorkshopSaveCodec.Identifier"`:
  - `CompiledSceneBoundaryTests.cs:199`, `:615`; `RunControlTableTests.cs:411`; `TypedTransportCommandTests.cs:360`; `WireFaultTests.cs:207`, `:214`; `WorkshopIntegrationHarnessTests.cs:143`; `WorkshopObservationTests.cs:400`, `:555`; `WorkshopWireTests.cs:760` (files named, lines not);
  - **`WorkshopFormatTests.cs:180`** (not named; A6-3 (k));
  - production uses `WorkshopWireFlatBuffers.cs:45`, `:53`, `:152`, `:158`, `:567`, `:582` and `WorkshopWireFlatBuffers.Observation.cs:25`, `:34` (covered by "callers take the member");
  - `WorkshopAuthoringFlatBuffers.cs:51` (deleted range).
- D5 `rg`: every hit is named (`CoreCommandCodec.cs:9`, `RuntimeCodecs.cs:9`, `DeclaredOrientationTests.cs:169-173`, `WorkshopSaveCodec.cs:52`, `WorkshopAuthoringFlatBuffers.cs:36`, `:77-98`, `WorkshopWireFlatBuffers.cs:53-82`).
- Root identifier literals (`rg "\b(CWCM|CWRM|CCWO|CCWS)\b"`): all named, plus `WorkshopFormatTests.cs:176` (`0x53574343u` and the comment "the retired "CCWS" magic word"), which lies inside the H-T3 historical region and is allow-listed.

## D3 against S5b's final code

| Premise | Result |
|---|---|
| `GenerateWireTypeScript` is still Debug-only | Yes: `Condition="'$(Configuration)' == 'Debug'"` (`CuriousContraptions.FlatBuffers.csproj:173`). |
| Where it lives | The vendored project, `:169-205`, with `RemoveWireTypeScriptStaging` `:207-209` and `OrderWireSchemas` `:211-275`. `BeforeTargets="BeforeCompile"`, `DependsOnTargets="ListWireSchemas"`. |
| Inputs and Outputs | Inputs: `@(WireSchema)`, `$(IntermediateOutputPath)WireSchemas.list`, `$(MSBuildProjectFullPath)`. Outputs: the entry module **and** `schemas.txt` (`:174`; A6-1). It stages in `tools/e2e/.wire/.staging/` (`:171`), not in an intermediate directory. |
| The move into the Abstractions chain | Holds. `RestoreRuntimeTypeScript` and `BuildRuntimeTypeScript` run only in Abstractions (`RuntimeTypeScript.targets:23`, `:29-30`, `BeforeTargets="BeforeBuild"`). `tsconfig.runtime.json` includes `tools/runtime/**/*.ts` and follows imports into `tools/e2e/.wire/` (`rootDir "."`). New harness files join `BuildRuntimeTypeScript`'s Outputs through the `RuntimeTypeScriptHarness` glob (`:7`, `:32`). One writer per graph locally rests on task 1(c), which is a hard gate (the web graph's `BuildWorkshopEditorMetadata` call passes `Configuration=Debug` and removes `RuntimeIdentifier;SelfContained`, so whether it reuses the outer instance must be measured, not assumed). CI's Release-then-Debug ordering holds by the argument at `:99` (one web project node, blocking `MSBuild` calls). |
| Stamp | S5b's pattern holds (`ListWireSchemas` `:120-122` with `WriteOnlyWhenDifferent`, and `FileWrites` at `:147`). The contract's separate stamp target in the Abstractions instance mirrors it. |
| Shared compiler targets imported once | Holds. `Directory.Build.targets:41` imports `RuntimeTypeScript.targets` unconditionally; the vendored directory has only a nested `Directory.Build.props` (`git ls-files`). One precision: define the schema path once, for example as a property in `FlatcCompiler.targets` read by both steps. A second `WireSchema` include in a file every project imports would double `@(WireSchema)` in the vendored project (`:43`), pass the file to `flatc` twice and trip S2. |

## Checks that hold

| Question | Result |
|---|---|
| Capability probe | Present items keep cited symbols and tests: S5's generation and `WorkshopWireBuildTests`; S5b's generation and `WorkshopWireTypeScriptTests`; the C# frame boundary (`WorkshopWireFlatBuffers.cs:25-56`, `WireFaultTests`); node harnesses (`NodeHarnessTests.cs:12-29`, `EventRingConsumerTests.cs:761-773`). The missing TypeScript verifier is owned (H-R1, `ENGINE-WIRE-ABI`). No required-now owner decision is open; A6-2 needs a coordinator scheduling choice, not an owner decision. |
| Object API under strict typing | The probe adds no `any`; `tsc` was not re-run (task 1(b) remains the gate). |
| Rulings (a) to (f) | Unaffected by S5b. Ruling (e)'s wording is A6-9 (b). |
| Forward-only, element-free, no-hash, no-skip, TypeScript rules | No conflict found in the planned design. A6-1 removes a dead check rather than keeping one. |
| `tools/e2e/tsconfig.json` | S5b already removed the `flatbuffers` path mapping and includes `.wire/**/*.ts`; S12's "no tsconfig maps `flatbuffers`" holds today. |
