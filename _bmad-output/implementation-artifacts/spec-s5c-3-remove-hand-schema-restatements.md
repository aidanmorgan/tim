---
title: 'One schema file is the ABI: no hand restatement of the schema remains (S5c-3)'
type: 'refactor'
created: '2026-11-26'
status: 'draft'
baseline_revision: 'captured at dispatch (the main that contains S5c-2)'
review_loop_iteration: 0
followup_review_recommended: false
context:
  - '{project-root}/schemas/README.md'
  - '{project-root}/docs/testing-guidance.md'
  - '{project-root}/docs/delivery-workflow.md'
  - '{project-root}/_bmad-output/implementation-artifacts/spec-s5c-one-wire-schema-file.md'
  - '{project-root}/_bmad-output/implementation-artifacts/spec-s5c-2-wire-conformance-and-root-pin.md'
  - '{project-root}/docs/planning/elements/readiness/owner-decisions-2026-11-22.md'
warnings: []
deferred: []
---

<intent-contract>

## Intent

**Problem:** Owner decision 24p (25 November 2026) requires that nothing restates the one schema file by hand. After S5c-1 ([contract](spec-s5c-one-wire-schema-file.md)) and S5c-2 ([contract](spec-s5c-2-wire-conformance-and-root-pin.md)), the schema is one file, both bindings are generated from it and a required test proves they agree. These hand restatements remain:
- **`SNAPSHOT_SCHEMA`** is a full hand copy of the schema (`tools/e2e/run-reset-snapshot.ts:13-146`), with a hand decoder at `:182-293`. It is stale: the deferred `tools/e2e/unit/run-reset-snapshot.test.ts` fails 23 of its 24 cases (S5b review finding BH3-4).
- **The authored-state dump** lists a partial set of fields by hand (`tools/e2e/workshop-wire.ts:92-151`), and the mock encoders use positional struct creators (`:153-381`).
- **Literal slot numbers** build or read tables in about fifteen C# test files, and `WorkshopSaveRejectionTests.cs:18-21` lists `InstanceRecord`'s fields in schema order.
- **TypeScript comments and messages** still name current root identifiers.
- **Seven C# domain enums** mirror wire enums by value with no test that they agree.

This is the last of the three S5c slices (coordinator ruling, 26 November 2026).

**Approach:** every restatement in the inventory is deleted or derived from generated code or `WireSchemaLayout`; the historical fixtures of refused input stay as marked regions; a guard (S11) refuses a closed list of restatement forms; an interim test (S14) proves each C# mirror of a wire enum matches its generated enum until `ENGINE-WIRE-ABI` moves enum ownership.

## Boundaries & Constraints

**Always:**
- **No inventoried hand restatement remains** outside the allow-list: generated code, the vendored runtime, `WireSchemaLayout`, and the marked historical-fixture regions named in H-T3 and H-T4 (regions, not whole files).
- **Historical fixtures of refused input stay fixed.** Their slots are the fixture's own data, commented as historical. Each region is marked `// historical layout: begin` and `// historical layout: end`; the markers pair, do not nest, appear only in the files H-T3 and H-T4 name, and the number of regions per file is pinned.
- **No current root identifier literal**, in code or in a comment, in tracked C#, TypeScript or MSBuild sources outside the allow-list and the foreign test data S5c-1 names (`ClutchGeneralityTests.cs:443`; the `Foreign` constant in `WorkshopSaveTests`). S11 scans the tracked sources, excluding `_bmad-output/` and `docs/`, which hold historical evidence such as `_bmad-output/implementation-artifacts/evidence-9r-0c/`. The remaining TypeScript ones are reworded to name the enum member: `tools/e2e/workshop-driver.ts:139` and `:929`, `tools/e2e/run-reset-snapshot.ts:321`, and `tools/e2e/unit/run-reset-snapshot.test.ts:86`.
- **The snapshot is derived.** The Run/Reset snapshot flattens the generated object API's unpacked read and save into `path -> value`. Values are compared with `Object.is`, so -0 still differs from 0 and NaN equals NaN; the raw-bit NaN-payload distinction is dropped, because Run/Reset restoration is judged on decoded values (AGENTS.md game fidelity) and no gameplay outcome depends on a NaN payload. `EXCLUDED_PATHS` (`:150-153`) keeps its intent in the derived path spelling.
- **The suites follow the derived form.** Their snapshot path strings follow the derived path spelling, and their asserted values follow the derived value form that `snapshotValue` returns (stated in `run-reset-snapshot.ts`), for example `cat-050-joined.test.ts:294`'s f32 bit pattern (A6-7). They stay unexecuted until the browser phase.
- **Wire enums keep one source in the schema.** Owner decision 24p, ruling (f), makes the schema enum the one source for closed sets that cross the wire. S5c-3 does not move enum ownership; `ENGINE-WIRE-ABI` does. Until then, the interim test S14 proves that each of the seven C# domain mirrors has the same member names and values as its generated enum.
- **Forward-only.** No alias, re-export or second representation of any field list; derivation replaces each restatement.

**Never:**
- Running or gating the browser toolchain (`tsx`, Playwright) in a required run, or the deferred `tools/e2e/unit/run-reset-snapshot.test.ts`. The `BrowserDeferral` list is unchanged.
- Any schema change, wire field move or codec behaviour change.
- Hashes or byte comparisons of encoded output as acceptance; mutants; a local Release worker build or web publish.
- Moving enum ownership or the thread-boundary layouts here (`ENGINE-WIRE-ABI`).

## I/O & Edge-Case Matrix

| Row | Input / State | Expected Output / Behavior | Error Handling |
|-----|--------------|---------------------------|----------------|
| S11 | The tree, code and comments | Outside the allow-list (generated code, the vendored runtime, `WireSchemaLayout`, and the marked historical-fixture regions of H-T3 and H-T4): (1) none of these names: `SNAPSHOT_SCHEMA`, `TheVersionlessRecordsDeclareExactlyTheirFields`, `InstanceRecordFields`, `SavedConstructionSlot`, `ConstructionInstancesSlot`, `HoldsFieldBeyondTheInstanceTable`, `ValidateBindingShapes`, `TableTarget`, `COMMAND_IDENTIFIER`, `RESPONSE_IDENTIFIER`, `SAVE_IDENTIFIER`, `CommandIdentifier`, `ResponseIdentifier`, `ObservationIdentifier`, and `enum Field` in `WorkshopSaveRejectionTests.cs`; (2) no `\b(CWCM|CWRM|CCWO|CCWS)\b`, in code or comments; (3) C#, via Roslyn syntax: no integer literal as the argument of `StartTable`, as the slot argument of a `FlatBufferBuilder.Add*` call, or as the argument of `__offset`, and no binary expression `4 + 2 * x` or `x * 2 + 4`; (4) TypeScript, via regex: no integer literal as the first argument of `startObject` or `addField*`, or the second of `__offset`; (5) the only hand TypeScript file that calls `__has_identifier` or reads `SIZE_PREFIX_LENGTH` is `tools/runtime/wire-frame.ts`. Markers: `// historical layout: begin` and `// historical layout: end` pair, do not nest, and appear only in the files H-T3 and H-T4 name, with the number of regions pinned per file. Stated limit: a new restatement under a new name is review's to catch | The guard names file and line |
| S14 | The seven C# domain mirrors of wire enums (interim, until `ENGINE-WIRE-ABI`): `WorkshopCommandKind`, `WorkshopResponseKind`, `WorkshopSimulationPhase`, `WorkshopCommandOutcome`, `ElectricalEnable`, `RunControlLane`, `RunControlValueKind` | Each has the same member names and numeric values as its generated `CuriousContraptions.Wire` enum | Names the pair, member and values |

</intent-contract>

## Code Map

Baseline: captured at dispatch, the main that contains S5c-2. Citations below were read at `39aaba70` (`audit-s5c-6.md`, A6-11 applied); S5c-1 and S5c-2 move some, so the dispatch re-audit re-reads each one.

**Status:** draft. Runs in lane 2 after S5c-2 publishes and before S6 (coordinator ruling, 26 November 2026).

**Planning verdict:** not yet audited as its own contract. Its content comes from the S5c contract and audits 1 to 6 (run log E1). A bounded re-audit at its own dispatch, from the main that contains S5c-2, is mandatory before the build.

**Review tier:** engine (TypeScript tools, test helpers and many test files). **Shape:** one structural slice.

Owned scope:
- **`CuriousContraptions.tests/WireSchemaGuards.cs`**: adds S11 (`NoHandSchemaRestatementRemains`).
- **`CuriousContraptions.tests/WireEnumMirrorTests.cs`** (new, interim): S14. `ENGINE-WIRE-ABI` deletes it together with the mirrors.
- **H-T2:** `WorkshopSaveRejectionTests.cs:17-21` (`enum Field`, the `InstanceRecord` fields in schema order) is derived through `WireSchemaLayout`.
- **H-T3:** the files with literal slots or vtable sizes take their slots from `WireSchemaLayout`, and struct byte offsets are rewritten through generated builders or accessors: `BodyOwnershipTests.cs`, `ClutchGeneralityTests.cs:378-462`, `CompiledSceneBoundaryTests.cs:89-761`, `CompoundMassAndRollingTests.cs:880-896`, `DrivenSurfaceBindingTests.cs:513-553`, `FanAdmissionTests.cs`, `IndependentSignalCompositionTests.cs:268-312`, `OwnedFiniteWorkTransactionTests.cs:196-198`, `PayloadInstanceTests.cs:270-358`, `RetentionBoundaryTests.cs:117-122`, `:372-397`, `RopeTests.cs`, `RunControlTableTests.cs:102-104`, `TrampolineTests.cs:571-574`, `WindmillAdmissionTests.cs:281-289`, `WorkshopWireTests.cs:918-922`, `MechanicalCoordinateBindingsTests.cs:1019-1052`, `MechanicalLinkMappingTests.cs:144-150`.
- **Historical-fixture regions** gain their markers: `OldLayoutSaves.cs:11-80`, `WorkshopFormatTests.cs:160-181` (the retired versioned save, including `:176`'s `0x53574343u` and its comment), `DeclaredOscillatorTests.cs:174-177`, `DeclaredOrientationTests.cs:148-151`.
- **`tools/e2e/run-reset-snapshot.ts`** (H-S1): `:5-6`, `:13-146` (`SNAPSHOT_SCHEMA`), `:156-158`, `:167` and `:182-293` (the hand layout and decoder) are deleted; the snapshot is derived from the object API (Always). `:321` is reworded. `:371` (`MemoryKind`) is H-W6's.
- **`tools/e2e/unit/run-reset-snapshot.test.ts`** (H-S2): the schema-listing checks (`:17-22`, `:86`, `:106-186`) are deleted, because nothing is left to compare. Its other cases follow the derived paths. Its fixture sets the required `PuzzleRecord.assistance` (BH3-4's second cause). It stays deferred and is not run (A6-7).
- **`tools/e2e/workshop-wire.ts`**: `:92-151` (`dumpAuthored`, `authoredState`; H-S4) is derived from the generated object API and flattens every field; `:153-381` (the mock encoders; H-S5) build through object-API classes with named properties, not positional struct creators.
- **The suites' snapshot strings** (H-S6): paths follow the derived spelling and values the derived value form (Always; A6-7): `capacity-9r5b.test.ts:109-111`, `cat-020.test.ts:105-107`, `:303`, `cat-050-joined.test.ts:188-294`, `cat-051.test.ts:74`, `cat-051-own-supply.test.ts:250-252`, `cat-052-lamp.test.ts:108-110`, `pkg-3.test.ts:108-110`, `tube-mouths.ts:38-68`, `tube-snapping.test.ts:117`, `:178`.
- **`tools/e2e/workshop-driver.ts:139`, `:929`**: comments reworded to name the enum member.
- **`schemas/README.md`**: the `SNAPSHOT_SCHEMA` reminder that remains of `:90-91` is deleted.
- **Planning records:** this contract and its run log; `TODO.md`; `sprint-status.yaml` (key `s5c-3-remove-hand-schema-restatements`); `delivery-batches.md` (S5c); `deferred-work.md` (closes S5b's carried BH3-4 in the record; its re-check stays with the browser phase).

### Hand-restatement inventory (owner decision 24p; all S5c rows)

Measured at `51d46f7b` with `rg` over C#, TypeScript, MSBuild, tools and tests, excluding `_bmad-output/` and `docs/`; audit 1 extended it and audit 6 re-read it at `39aaba70`. Each row is deleted or derived by the slice named, unless the row names another owner.

| Row | Where | What it restates | Slice and action |
|---|---|---|---|
| H-C1 | `engine/gpu/WorkshopWireFlatBuffers.cs:45-53`, `:58-91` | Command and response field counts, scene ordinals, track and binding counts | S5c-1: deleted; verifier rule |
| H-C2 | `engine/gpu/WorkshopAuthoringFlatBuffers.cs:34-102` | Puzzle vtable bounds, saved-construction and instances slots, `InstanceRecordFields = 7` | S5c-1: deleted; verifier rule plus `(required)` |
| H-C3 | `WorkshopWireFlatBuffers.cs:18-19`, `WorkshopWireFlatBuffers.Observation.cs:11`, `WorkshopSaveCodec.cs:17`, `CoreCommandCodec.cs:9`; `F32SaveBoundaryTests.cs:12` | Root identifiers | S5c-1: derived from `WireRootIdentifier` |
| H-C4 | `engine/gpu/WorkshopSaveCodec.cs:52-54` | The instance-record field bound | S5c-1: deleted |
| H-T1 | `WorkshopFormatTests.cs:96-123` | Field lists of eight records | S5c-1: deleted |
| H-T2 | `WorkshopSaveRejectionTests.cs:17-21` | `InstanceRecord` field order | S5c-3: derived through `WireSchemaLayout` |
| H-T3 | The files listed in the owned scope; historical regions `OldLayoutSaves.cs:11-80`, `WorkshopFormatTests.cs:160-181`, `DeclaredOscillatorTests.cs:174-177`, `DeclaredOrientationTests.cs:148-151` | Field ordinals, vtable sizes and struct byte offsets | S5c-3: slots through `WireSchemaLayout`, struct offsets through generated builders or accessors; historical regions marked and allow-listed |
| H-T4 | `DeclaredOrientationTests.cs:137-138`, `:148-151`, `:157-181` | A slot comment, a historical layout and a vtable walk | S5c-1: comment and walk deleted; S5c-3: the region marked |
| H-S1 | `tools/e2e/run-reset-snapshot.ts:13-146`, `:156-167`, `:182-293` | Both schemas in full, scalar sizes, struct layout and a hand decoder | S5c-3: deleted; derived from the object API |
| H-S2 | `tools/e2e/unit/run-reset-snapshot.test.ts:17-22`, `:86`, `:106-186` | A schema-text parser, listing checks and `'CCWS'` | S5c-3: deleted |
| H-S3 | `tools/e2e/workshop-wire.ts:14-16`, `:20-29` | Identifier literals and a second frame check | S5c-2: deleted; `wire-frame.ts` |
| H-S4 | `tools/e2e/workshop-wire.ts:92-151` | Partial field lists of `InstanceRecord`, `ConnectionRecord` and `PuzzleRecord` | S5c-3: derived from the object API, every field |
| H-S5 | `tools/e2e/workshop-wire.ts:153-381` | Positional struct-creator argument order (for example `createBodyRead`, 22 arguments) | S5c-3: object API with named properties |
| H-S6 | The suites' snapshot strings (owned scope) | Field names as path text | S5c-3: kept as assertions, respelled |
| H-R1 | No TypeScript FlatBuffers verifier | A missing capability | **Owner `ENGINE-WIRE-ABI`.** Its first slice that adds a production TypeScript table decoder provides TypeScript verification without hand code; the candidate is a generic verifier driven by the compiler's binary schema (`flatc --binary --schema`), with accessors generated from upstream `reflection.fbs` vendored under an integrity pin (24p ruling (a)) |
| H-W4 | `CuriousContraptions.Abstractions/WorkshopMessageValues.cs:11-13`, `RuntimeServices.cs:10-11`, `ElectricalSourceValues.cs:36`, `ParameterDescriptor.cs:50`, `RunControlValues.cs:11`; numeric casts in `engine/gpu/WorkshopWireFlatBuffers.cs:137`, `:147`, `:166`, `:530-533`, `:586-589`, `:761` | Seven C# enums mirroring schema enums by value | **Owner `ENGINE-WIRE-ABI`** (ruling (f); C36 amended). S5c-3 adds the interim S14 |
| H-W6 | `CuriousContraptions.web/wwwroot/workshop-observation.ts:21`, `:39-40`, `:47` (wire enums); the rest of `:3-49` and `run-reset-snapshot.ts:371` (`MemoryKind`) | TypeScript enum mirrors | Wire enums: **`ENGINE-WIRE-ABI`**, from the generated module. Non-wire enums: **`ENGINE-CAPABILITY-INSTANCES`**, through its C#-to-TypeScript generator |
| H-W7 | `engine/gpu/WorkshopCadence.cs:61-93` and every non-FlatBuffers boundary layout | Hand layouts | **Owner `ENGINE-WIRE-ABI`**, declared in the one file (24p) |
| H-W3 | `schemas/workshop_wire.fbs:305-306` (`motion: [uint8]`); `engine/gpu/PhysicsMotionRead.cs:47-52` | A hand layout tunnelled through the schema | **Owner `ENGINE-WIRE-ABI`** |

Checked and unchanged: `BundleElementIdTests.cs:213` names two fields in an assertion, not a layout; `tools/WireContract/` and `tools/LifecycleContract/` are separate JSON design fixtures.

### Capability probe

No solver law, worker message content, wire field or shared presentation contract changes.
- **Present:** `WireSchemaText` and `WireSchemaLayout` (S5c-1; `WireSchemaTextTests`); the generated object API (S5c-2; `WireConformanceTests`); the `csc` replay and Roslyn syntax (`CSharpStaticGateTests`).
- **Missing, added here:** the restatement guard (S11); the interim enum-mirror check (S14).
- **Missing, owned elsewhere:** enum ownership and the TypeScript verifier (`ENGINE-WIRE-ABI`). No required-now owner decision is open.

### Decisions

- **D11, the snapshot compares decoded values.** `Object.is` over the object API's values; the NaN-payload distinction is dropped because Run/Reset restoration is judged on decoded values (AGENTS.md game fidelity). Reasoning: run log E1.
- **D12, S11's accepted forms are a closed list** (A6-8): each form is named, and a new restatement under a new name is review's to catch, so the scanner does not widen with each repair.

### Protections this change removes, and what replaces them

- **`SNAPSHOT_SCHEMA`'s listing test**, deferred and stale, is replaced by derivation, which leaves nothing to list.
- **Literal slots in tests** are replaced by `WireSchemaLayout`, which `WireSchemaTextTests` checks against the generated `Add*` counts.
- **Residuals:** the deferred unit test and suites stay unexecuted until the browser phase; S11 sees only its named forms.

### Concurrency

- Starts from the main that contains S5c-2 (coordinator ruling, 26 November 2026). It edits no file under `tools/E2eRunner/` or `tools/E2eRunner.Tests/`, so lane 3's runner slice is not affected.
- The H-T3 test files overlap lane 1's collider repairs only if those repairs edit the same tests (for example `RopeTests.cs`, `FanAdmissionTests.cs`); checked at dispatch, and an overlap serialises the two slices.
- Shared documents (owner decision 24m): `TODO.md`, `deferred-work.md`, `delivery-batches.md`, `completion-ledger.md` and `sprint-status.yaml`; line-local edits only.
- New files are staged with `git add` before the candidate commit.

## Tasks & Acceptance

**Execution:**
1. **Gate:** re-measure the inventory rows assigned here with `rg` at the baseline and record any new hit in the run log before editing.
2. **C# tests:** H-T2, H-T3 and the historical-region markers.
3. **TypeScript tools:** H-S1, H-S2, H-S4 to H-S6 and the comments.
4. **Guards:** S11 and S14.
5. **Records:** the README line and the planning records; close S5c with its numbers.

**Acceptance Criteria:**
- AC1: Given the candidate, when S11 runs, then no listed form remains outside the allow-list, and its controls refuse each form.
- AC2: Given the full local round, when it completes, then failures are 0 and skipped are 0 in every project, the total is the baseline plus the new tests minus the deleted ones (delta measured at dispatch), and the run log names each added and deleted test with its cover.
- AC3: Given the inventory, when the slice closes, then every row marked deleted or derived is gone from the tree (S11), and every row owned elsewhere is cited from its owner story: `ENGINE-WIRE-ABI` for H-R1, H-W3, H-W4, H-W6 (wire enums) and H-W7; `ENGINE-CAPABILITY-INSTANCES` for H-W6 (non-wire enums).
- AC4: Given the maintainability gate, when it runs, then both canaries pass and violations are 0.

## Spec Change Log

- 26 November 2026: created by the S5c split (coordinator ruling; audit 6, A6-10), from the S5c contract and audits 1 to 6. Carries A6-7, A6-8's S11 bullet and A6-11's citations (run log E1).

## Review Triage Log

## Design Notes

1. **Delivered:** no hand restatement of the schema remains outside the marked historical fixtures. No gameplay, solver or presentation change.
2. **Reusable:** `WireSchemaLayout` for test slots; the object API for snapshot, dump and mocks.
3. **Declared:** the schema file only.
4. **Execution:** tests and tools read the generated code or `WireSchemaLayout`; S11 refuses a closed list of forms.
5. **Ownership:** test helpers own slots; the generated object API owns field lists.
6. **Generality:** the snapshot and dump flatten every field without naming one.
7. **Deleted:** `SNAPSHOT_SCHEMA` and its hand decoder; the schema-listing checks; the partial field lists; positional struct creators; the literal slots outside historical fixtures.
8. **Uncertain:** browser-phase re-check of the snapshot test; enum ownership (`ENGINE-WIRE-ABI`). Nothing required now is open.

**Architecture guards.** S11 reads source; it lives in `WireSchemaGuards` with positive controls.

## Verification

Executor: the implementation owner.

Environment: `PATH=~/.local/share/tim-toolchain/dotnet-10.0.403:~/.local/share/tim-toolchain/node-v26.12.1-darwin-arm64/bin:$PATH` and `DOTNET_ROOT=~/.local/share/tim-toolchain/dotnet-10.0.403`. Record the session environment check in the run log.

**Commands:**
- The five compiler-log capture commands (`docs/delivery-workflow.md:38-53`). Expected: every build succeeds.
- `dotnet run --file tools/anvil/VerifyPolicy.cs -- --maintainability --project-root .` Expected: AC4.
- The registration check filter. Expected: 0 failed, 0 skipped (a test file is added).
- The scoped run of the guard, the mirror test and the edited test classes (named at dispatch from the H-T2 and H-T3 list). Expected: 0 failed, 0 skipped.
- `git add -N` on the new files, then `git diff --check <baseline_revision>`. Expected: clean.
- One full local round. Expected: AC2.

### Acceptance-to-test table

No tolerance applies: every assertion is a name, a value, a count or a refusal.

| Rows | Named discriminating test | Control and boundary | Execution |
|---|---|---|---|
| S11 | `WireSchemaGuards.NoHandSchemaRestatementRemains` | Control: synthetic sources holding `StartTable(12)`, `"CWCM"`, a `SNAPSHOT_SCHEMA` declaration, a `4 + 2 * slot` expression or a second TypeScript frame check, outside the allow-list, are each refused, naming file and line. Boundary: a marked historical-fixture region is admitted, and a restatement outside its markers in the same file is refused; an unpaired or nested marker is refused | planned |
| S14 | `WireEnumMirrorTests.EachDomainMirrorOfAWireEnumMatchesItsGeneratedDeclaration` (interim) | Control: a synthetic pair that differs in one member's value, and one that differs in one name, are each refused | planned |
| AC1 to AC4 | S11, the full round, the maintainability gate | The baseline round is recorded at the start | planned |
