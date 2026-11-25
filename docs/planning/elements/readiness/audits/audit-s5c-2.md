# Planning audit: `spec-s5c-one-wire-schema-file.md` (S5c), second pass

Audited 25 November 2026, read-only, planning phase. Checkout `.claude/worktrees/plan-s5c-one-wire-schema` (branch `plan-s5c-one-wire-schema`, HEAD `51d46f7b`, uncommitted planning edits: the untracked contract and first audit, and eight edited planning documents). The S5b candidate in `.claude/worktrees/slice-s5b` was read for context only. Policy was read from this checkout: `AGENTS.md` (including the forward-only review gate), owner decision 24p and coordinator rulings (a) to (f) as now recorded at `owner-decisions-2026-11-22.md:71-77`, `_bmad/custom/element-free-engine-design.md`, `_bmad/custom/element-free-engine-review.md`, `docs/testing-guidance.md`.

Scope: the repaired contract (all sections), the repairs of audit 1 findings A1-1 to A1-11, and the planning-document diff (`git diff`: `TODO.md`, `sprint-status.yaml`, `epics.md` ENGINE-CAPABILITY-INSTANCES and ENGINE-WIRE-ABI, `completion-ledger.md` OS-01, `engine-stories-index.md` gate, row 8a and C36, `owner-decisions-2026-11-22.md` 24p, `delivery-batches.md` §0, S5b and S5c, `vertical-delivery.md`). Every `path:line` the repairs added or changed was re-read at `51d46f7b`. No probe was compiled in this pass; audit 1's compiler measurements were reused where the repaired text depends on them.

## Verdict: Fail

Eight of the eleven audit 1 findings are closed and three are partly closed. Three new medium findings remain open:
- A2-1: D7's interface change needs an Abstractions-to-FlatBuffers project reference. D3 rejects that reference, and `ENGINE-WIRE-ABI` holds the decision.
- A2-2: a test of the deleted hand walk is described as a historical layout and exempted from S11 for the whole file. A second test asserts the old verifier contract.
- A2-3: S10 cannot detect the exact reference D7 removes (`SavedConstructionVerify`).

Each has a small, concrete repair. The rest are low.

## Audit 1 dispositions

| Finding | Disposition | Evidence |
|---|---|---|
| A1-1 (high) second TypeScript frame check | Closed | Always (frame check) and Code Map: one `tools/runtime/wire-frame.ts`, compiled by the runtime chain and imported by `wire-conformance.ts` and `workshop-wire.ts`. `:12734-12736` and `:12740-12749` are deleted (H-S3), and S11 asserts "Exactly one TypeScript frame check". The import is feasible: `tsconfig.runtime.json` has `rootDir: "."`, so the generated module under `tools/e2e/.wire/` compiles to `.runtime-build/tools/e2e/.wire/`. The e2e config (`noEmit`, no `rootDir`) admits an import from `../runtime/`. Residual: A2-10. |
| A1-2 (medium) reason match | Closed | Always: four distinct plain messages; the mapping to `WireFaultReason` lives in the C# test, with no TypeScript mirror. S6 matches each message. 24p (a) now carries the matching clause (`owner-decisions-2026-11-22.md:72`). |
| A1-3 (medium) number reporting, NaN | Closed | Code Map (`wire-conformance.ts`): every `number` is reported as its binary64 bit pattern and every `bigint` as decimal. D8 drops NaN and cites `WorkshopSavePrevalidation.cs:104`, `:169`, `:189-192`, which read as cited (`float.IsFinite` refusals). |
| A1-4 (medium) CI one-writer, Inputs and Outputs | Closed, with low residuals A2-4 and A2-5 | D3 states CI's possible two Abstractions instances (`pages.yml:43`; `CuriousContraptions.web.csproj:47-52`, which reads as cited), records the evidence limit, and gives task 1(c) the CI derivation. Always states per-instance staging, the entry module moved last, and Inputs and Outputs. |
| A1-5 (medium) build-test fixture | Partly closed (low residual A2-7) | Task 1(e) defines an offline Abstractions-copy fixture. The three named cases are disposed (merged or moved), and `AReleaseBuildWritesNoTypeScript` and the two S5 cases are deleted. Other S5b cases that read TypeScript from the vendored-project copy, and S5b's T8, have no disposition. |
| A1-6 (medium) S8 boundaries | Closed | S8 covers all four roots, `CompiledSceneRecord` rows, a save's `InstanceRecord` and the zero-entry boundary. The change of behaviour is stated in Always (`WorkshopAuthoringFlatBuffers.cs:98-99` reads as cited). `WireDecodeTests.cs` is in the owned scope and the filter, and the test is renamed. |
| A1-7 (medium) enum direction | Closed (low residual A2-9) | Ruling (f) is recorded (`:77`). C36 is amended (`engine-stories-index.md:196`), and so are `epics.md`'s ENGINE-CAPABILITY-INSTANCES sentence (`:4646`) and ENGINE-WIRE-ABI's Enums bullet. S14 is added. S4 names the enum it enumerates (`Enum.GetValues<WorkshopCommandKind>()`). The seven pairs match today: same member names and values for `CommandKind`/`WorkshopCommandKind` (0-10, 12, 13, 14), `SimulationPhase` (0-10), `CommandOutcome` (0-4), `ResponseKind`, `ElectricalEnable`, `RunControlLane` (1, 2) and `RunControlValueKind` (1, 2); only the underlying integer types differ, which S14 does not compare. |
| A1-8 (medium) unrecorded rulings | Closed | 24p records rulings (a) to (f), including (c) (four roots and the arm definition), (d) (pin reversal) and (e) (supersedes 24k and 38 for the derived refusal). OS-01's source column reads "owner 22, 24h, 24m and 24p". |
| A1-9 (medium) inventory and callers | Partly closed (A2-2, medium) | Added: `F32SaveBoundaryTests.cs:12` and `:48`, `GoalSignalSelectorTests.cs:81`, the twelve `WorkshopSaveCodec.Identifier` sites, `WorkshopFormatTests.cs:160-181`, `DeclaredOscillatorTests.cs:174-177`, S11's allow-list and D5's fixtures. All read as cited. Not closed: the `HoldsFieldBeyondTheInstanceTable` and slot-constant callers, and an assertion that inverts under the new rule (A2-2). `WorkshopFormatTests.cs:180` is not listed, but the dispatch `rg` catches it. |
| A1-10 (low) citations | Closed | `schemas/README.md:17-18`; `CuriousContraptions.FlatBuffers.csproj:127-133`; "five callers" removed. |
| A1-11 (low) design record and guards | Partly closed (A2-3, medium) | Design Notes now answer all eight questions, including atomic save refusal and what remains uncertain. The guards are named and live in one class with controls. `WireSchemaText` is the one schema reader, with its `Add*` control. The S9 limit is stated, with correct citations (`workshop_wire.fbs:306`, `:357`, `:359`, `:361`, `:378`, `:379`). The withdrawn intent is recorded. S10's repair introduced a new gap (A2-3). |

Citations checked in this pass and correct at `51d46f7b`:
- `WorkshopSaveCodec.cs`: `:17`, `:29`, `:48-55`, `:52-54`, `:53`, `:59-69`, `:61`.
- `WorkshopSavePrevalidation.cs`: `:104`, `:169`, `:189-192`.
- `F32SaveBoundaryTests.cs`: `:12`, `:48`.
- `GoalSignalSelectorTests.cs:81`.
- `WorkshopSaveTests.cs:398-401`.
- `WorkshopFormatTests.cs`: `:96-123`, `:137`, `:160-181`.
- `DeclaredOscillatorTests.cs:174-177`.
- `ClutchGeneralityTests.cs:443`.
- `WorkshopSaveRejectionTests.cs`: `:17-20`, `:42`.
- `CompoundMassAndRollingTests.cs`: `:880-896`, `:896`, `:900`.
- `WorkshopWireFlatBuffersTests.cs`: `:43`, `:46`.
- `SaveFixture.cs:22`.
- `CuriousContraptions.web/CuriousContraptions.web.csproj:47-52`.
- `.github/workflows/pages.yml:43`.
- `schemas/workshop_wire.fbs`: `:305-306`, `:348-351`, `:357-361`, `:378-379`, `:385`, `:1122`.
- `workshop_authoring.fbs`: `:4-92`, `:95`.
- `WorkshopWireFlatBuffers.cs`: `:18-19`, `:25-56`, `:45-53`, `:51`, `:58-91`, `:73`, `:78`, `:104-109`.
- `WorkshopAuthoringFlatBuffers.cs`: `:34-74`, `:76-102`.
- `CoreCommandCodec.cs`: `:9`, `:15-16`, `:19`.
- `RuntimeCodecs.cs`: `:9`, `:15-19`.
- `RuntimeTypeScript.targets`: `:23`, `:29-33`.
- `CuriousContraptions.FlatBuffers.csproj`: `:43`, `:57`, `:114`.

`PuzzleRecord.assistance` carries `(required)` (`workshop_authoring.fbs:74`, field index 11). So the deleted lower bound in `CurrentPuzzleTable` (`WorkshopAuthoringFlatBuffers.cs:47`, at least 10 fields) is covered by the generated verifier, as the contract claims.

## New findings

### A2-1 (medium) D7 changes an Abstractions interface to generated types, which needs the reference D3 rejects and `ENGINE-WIRE-ABI` holds

Evidence:
- The Code Map says that in `CuriousContraptions.Abstractions/RuntimeCodecs.cs:9` and `:15-19` "the identifier properties become `WireRootIdentifier` members, and the save-frame check takes the member and a verify function from the main assembly (D7)".
- `ICommandCodec` is declared in Abstractions (`RuntimeCodecs.cs:7-21`), and Abstractions has no project reference (`CuriousContraptions.Abstractions.csproj`).
- `WireRootIdentifier` and the verify delegate type (`VerifyTableAction`) live in `CuriousContraptions.FlatBuffers`. Typing the interface with them needs an Abstractions-to-FlatBuffers reference, and that contradicts the contract twice:
  - D3 rejects "a build-order reference from Abstractions to FlatBuffers" and rests on "FlatBuffers and Abstractions have no build-order relation".
  - Ruling (f), as recorded in ENGINE-WIRE-ABI's Enums bullet (`epics.md`), reserves to that story's planning the choice of "Abstractions referencing the dependency-free `CuriousContraptions.FlatBuffers` assembly".
- The main assembly cannot call Core's frame boundary directly instead: `WorkshopWireFlatBuffers.OpenFrame` is `internal` (`WorkshopWireFlatBuffers.cs:25`), and Core grants internals only to the tests (`CuriousContraptions.Core.csproj:20`).
- No consumer reads `CommandIdentifier`, `ResponseIdentifier` or `ObservationIdentifier` through the interface (`rg`). `ValidateSaveFrame` has one caller (`WorkshopAuthoringFlatBuffers.cs:36`).

As written, the plan either adds the reserved reference, which changes D3's build graph and pre-empts a ruling, or cannot compile.

Repair:
- Delete the three unused identifier properties and `ValidateSaveFrame` from `ICommandCodec`.
- Give Core one public frame entry, the existing boundary made public, taking `(bytes, WireRootIdentifier, VerifyTableAction)`, together with the one public member-to-identifier conversion. The main assembly's save codec (`WorkshopSaveCodec.cs:29`, `:61`) calls both directly.
- State that Abstractions gains no project reference, and keep D3's premise.

D7 is otherwise unchanged: Core names no authoring declaration, and there is still one frame check.

### A2-2 (medium) A test of the deleted hand walk is described as a historical layout and exempted from S11; another test asserts the old verifier contract

Evidence:
- H-T4 lists `DeclaredOrientationTests.cs:169-179` as "a historical layout ... handled as in H-T3, and the file is on the allow-list". These lines are not a historical fixture. They are the control `CurrentRecordsHoldNothingBeyondTheTableAndTheWalkReachesTheGeneratedInstances`, which calls the deleted `HoldsFieldBeyondTheInstanceTable` (`:169`) and walks vtables with the deleted `SavedConstructionSlot` and `ConstructionInstancesSlot` (`:172-173`, `__offset`, `__vector`, `__indirect`). It cannot compile after the deletion.
- The contract gives that test no disposition, while AC2 requires every deleted test to be named with the test that covers it. The `rg` measurement at dispatch (Code Map) searches only identifier names, so it does not find these callers.
- The allow-list is per file. It exempts all of `DeclaredOrientationTests.cs` and `WorkshopFormatTests.cs` from S11, not only their historical fixture lines (`:148`, `:160-181`), so any hand slot or vtable code left in them passes the guard. That weakens the "nothing restates it by hand" reading of 24p.
- `CompoundMassAndRollingTests.cs:898-900` states "The vendored verifier deliberately permits unknown fields" and asserts `Assert.True(new Verifier(...).VerifyBuffer(...))` on a puzzle table with an appended field. Under the derived rule the assertion becomes false. The contract lists `:896` and `:900` only as identifier callers, and D5's list measured at dispatch names only `OutOfRange` expectations and fixtures.

Repair:
- Correct H-T4: `:137-138` is a slot comment and is deleted. `:148-151` is the historical layout and stays allow-listed. `:169-180` is deleted, covered by S8's control (current records admitted) and S4 and S5 (generated accessors reach every instance). Record that in the run log.
- Make S11's allow-list line ranges or marked regions in the named files, not whole files.
- Add `CompoundMassAndRollingTests.cs:898-900` to D5's list as an assertion that inverts: the verifier itself now refuses the buffer, and the test keeps the codec refusal.
- Extend the dispatch `rg` to `HoldsFieldBeyondTheInstanceTable|SavedConstructionSlot|ConstructionInstancesSlot|InstanceRecordFields|ValidateBindingShapes|TableTarget`.

### A2-3 (medium) S10 cannot see the generated companions of marked declarations, so the D7 regression passes it

Evidence:
- S10 refuses type references that bind "to a declaration marked `authoring`", and its control declares a variable of type `InstanceRecord`.
- The reference D7 removes from Core is `CuriousContraptions.Wire.SavedConstructionVerify.Verify` (`CoreCommandCodec.cs:9`). `SavedConstructionVerify` is a generated class, not one of the nine marked declarations, so a Core or Simulation source that names it again passes S10. That is the plausible defect the guard exists to catch (testing guidance: a plausible defect must fail the test).
- S10 also says "binds" but specifies "Roslyn syntax ... not text tokens". Syntax cannot bind, so an alias or a `var` declaration inferred from a generated accessor goes unseen.

Repair:
- Define the refused set as each marked declaration and its generated companions (`<Name>Verify`, and any other type the pinned compiler emits for it, measured in task 1(a)).
- Add a control in which a Core source names `SavedConstructionVerify`.
- Either build a semantic model from the binlog's replayed compile (references included), or state the syntax-only limit (aliases, `var`) as an evidence limit beside S9's.

### A2-4 (low) The generation target's stamp is listed as both Input and Output

Always: "Its Inputs are the schema file, the shared compiler targets file and an intermediate stamp ... Its Outputs are the entry module and the stamp."

MSBuild compares the newest input with the oldest output. If the stamp is written after the entry module is moved, it is newer than that output, and the target reruns on every incremental build. The moved test `AnUnchangedSchemaDoesNotRegenerateTypeScript` then fails.

Repair: follow S5b's pattern. A separate target writes an intermediate list or stamp with `WriteOnlyWhenDifferent` and lists it in `FileWrites`, so Clean deletes it. That file is an Input only, and the entry module is the only Output.

### A2-5 (low) "A concurrent run never leaves a partial set" is not what the inherited mechanism gives

S5b's step deletes the previous output before it moves the new set into place (`slice-s5b` `CuriousContraptions.FlatBuffers.csproj:194-198`), and S5c must keep a deletion step for `ARemovedSchemaTypeLeavesNoTypeScript`. Two overlapping writers would expose an incomplete set. In CI's graph the Release and Debug Abstractions instances are reached from different targets of the one web project (reference resolution, then `BuildWorkshopEditorMetadata`), so they run one after another.

Repair: rest D3's one-writer claim on that ordering, recorded by task 1(c), and drop the concurrency wording. Optionally move over the existing files first and delete strays afterwards, so the set is never absent.

### A2-6 (low) The static gate's build-logic pins are not in the owned scope

Every non-vendored target and import is pinned in `CuriousContraptions.tests/CSharpStaticGateTests.cs`:
- `PinnedTargets` (`:1336-1364`, including `RuntimeTypeScript.targets:RestoreRuntimeTypeScript` and `:BuildRuntimeTypeScript`);
- `PinnedImports` (`:1368-1377`).

S5c changes `BuildRuntimeTypeScript`, adds a generation target, and adds a shared provisioning targets file (with `DownloadFile` and `Exec`) imported from two places. The Code Map lists only the vendored project's pin (`:116`).

Repair: add the `PinnedTargets` and `PinnedImports` entries to the owned scope. State that the vendored project's new import of a file outside the vendored directory is covered by `PinnedTargets` on that file, because the vendored files are excluded from the import check.

### A2-7 (low) S5b build and TypeScript tests without a disposition

- S5b's T8 `TheTypeScriptStepRunsExactlyOncePerBuildGraph` counts `Exec` inside the vendored `GenerateWireTypeScript`. The target moves, so T8 finds 0 and duplicates S2.
- `ACheckoutPathWithASpaceBuildsACompleteModuleSet` asserts TypeScript from the vendored copy, which will write none. It guarded the include ordering that S5c deletes.
- `AnUnchangedSchemaDoesNotRegenerateTypeScript` uses `tools/e2e/.wire/.staging`, which S5c moves into the intermediate directory.
- The "refused build wrote TypeScript" assertions in `APinThatTheCachedCompilerDoesNotMatchRefusesTheBuild` and the span-define cases become vacuous.
- `AReleaseBuildWritesNoTypeScript` is deleted because its behaviour is removed, not because S2 covers it: S2 reads only Debug logs.

Repair: dispose each case in the Code Map (delete T8, covered by S2; delete or move the space-path case with its reason; rewrite the staging steps; remove the vacuous assertions) and correct the Release rationale.

### A2-8 (low) S11 does not say whether comments count; one theory mixes types

- Comments hold current identifiers outside the allow-list: `SaveFixture.cs:7`, `WorkshopSaveCodec.cs:13`, `tools/e2e/workshop-driver.ts:145`, `:935`. State whether S11 scans comments, and dispose these lines.
- `WorkshopSaveTests.cs:398-401` is one theory with a `string identifier` parameter. Row `:398` keeps `"XXXX"`, while `:399-401` are to take the enum member, which cannot be passed as a `string`. Say how: split the theory, or use a `uint` parameter with `(uint)WireRootIdentifier.SavedConstruction` and the foreign word as a `uint` constant.

### A2-9 (low) Residual planning-document inconsistencies

- `delivery-batches.md` S5c Acceptance says "rows S1 to S13", but the contract has S14.
- The same file's S5b "Remains after it" still says the production-script enum mirrors (W6) "are replaced by `ENGINE-CAPABILITY-INSTANCES`' generator". Under ruling (f), the wire enums among them go to `ENGINE-WIRE-ABI`.
- C36 keeps its original governing sentence ("the C# enum is the single source ... deletes the seven") followed by the amendment. Rewrite the governing sentence so the current text states one rule.

### A2-10 (low) Smaller completeness points

- The e2e source identity (S5b `tools/E2eRunner/SourceIdentity.cs:36-38`) digests `tools/e2e/**`, `tools/*.ts` and `.wire`, not `tools/runtime/wire-frame.ts`, which the suites now import. Add it to the deferred-runner scope in the Code Map.
- The `WireSchemaText` control has no named test or class, and it is not in the scoped filter. The filter also omits `TypeCheckBoundaryTests`, which S5c changes. The full round covers both.
- D8's rationale should name the actual reference-equality member: `WorkshopCompiledScene` is a class without `Equals`, whereas `WorkshopRead` defines its own `Equals`, and existing tests already compare scene-free commands, responses and saves with `Assert.Equal`. `ImmutableArray` is not the reason. The choice of `Assert.Equivalent` stays sound: xUnit 3.2.2 handles by-ref-like properties, and the scene graph exposes `ReadOnlySpan` properties.

## Checks that hold

| Question | Result |
|---|---|
| Rulings (a) to (f) | The contract complies with each: C# keeps the verifier and TypeScript checks the frame only (a); identifiers are generated data (b); four roots, 14 + 2 kinds and 11 arms (c); root pin (d); derived refusal in S5c (e); S14 is interim, with ownership left to `ENGINE-WIRE-ABI` (f). Apart from A2-1, which would pre-empt (f)'s reserved reference. |
| Forward-only | No format version, alias, fallback decoder, second schema, second frame check or retained deprecated item. The domain enum mirrors are kept under ruling (f) with a named owner and an interim test. Historical fixtures remain refused input. |
| Testing guidance and hashes | Semantic field assertions with exact equality, justified as a defined-interface invariant. No hashes, mutants, skips or browser gating. `BrowserDeferral` entries are unchanged. Guards are named, each with a positive control. |
| Element-free design | No element identity, capability kind or table name selects behaviour. All eight design-record questions are answered. |
| Derived verifier rule | Sound as stated. Vtable deduplication cannot share a vtable longer than a table's own highest present field, and the C# and TypeScript builders trim trailing zero entries, so no valid buffer is refused. |
| Runtime script resolution | The compiled `.runtime-build/tools/runtime/wire-conformance.js` reaches `../e2e/.wire/curious-contraptions/wire.js` and resolves `flatbuffers` from the root `node_modules` under the root `"type": "module"` (audit 1's CommonJS-interop measurement). |
| Planning edits | TODO lane 2, sprint key, `delivery-batches.md` §0 and S5c, the engine stories gate and row 8a, `epics.md` (ENGINE-WIRE-ABI Contract, Enums and the one-file acceptance; ENGINE-CAPABILITY-INSTANCES), `vertical-delivery.md`, and OS-01 are consistent with the contract, apart from A2-9. |
