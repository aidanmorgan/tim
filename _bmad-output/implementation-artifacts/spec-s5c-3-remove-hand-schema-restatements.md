---
title: 'One schema file is the ABI: no hand restatement of the schema remains (S5c-3)'
type: 'refactor'
created: '2026-11-26'
status: 'done'
baseline_revision: '1b9dffe1ffa2eb6ada259e1b44a7dd4f865ffb37'
review_loop_iteration: 0
followup_review_recommended: false
context:
  - '{project-root}/_bmad/custom/element-free-engine-design.md'
  - '{project-root}/_bmad/custom/element-free-engine-review.md'
  - '{project-root}/schemas/README.md'
  - '{project-root}/docs/testing-guidance.md'
  - '{project-root}/docs/delivery-workflow.md'
  - '{project-root}/_bmad-output/implementation-artifacts/spec-s5c-one-wire-schema-file.md'
  - '{project-root}/_bmad-output/implementation-artifacts/spec-s5c-2-wire-conformance-and-root-pin.md'
  - '{project-root}/docs/planning/elements/readiness/owner-decisions-2026-11-22.md'
warnings: [oversized]
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

**Approach:** every restatement in the inventory is deleted or derived from generated code or `WireSchemaLayout`; historical inputs stay fixed in the marked regions and one committed save fixture; a guard (S11) refuses a closed list of restatement forms; an interim test (S14) proves each C# mirror of a wire enum matches its generated enum until `ENGINE-WIRE-ABI` moves enum ownership.

## Boundaries & Constraints

**Always:**
- **No inventoried hand restatement remains** outside the allow-list: generated code, the vendored runtime, `WireSchemaLayout`, and the marked historical-fixture regions named in H-T3 and H-T4 (regions, not whole files). The Story 9R.7 retired versioned-save payload is kept as one committed input fixture.
- **Historical fixtures of refused input stay fixed.** Their slots are the fixture's own data, commented as historical. Each region is marked `// historical layout: begin` and `// historical layout: end`; the markers pair, do not nest, appear only in the files H-T3 and H-T4 name, and the number of regions per file is pinned.
- **No current root identifier literal**, in code or in a comment, in tracked C#, TypeScript or MSBuild sources outside the allow-list and the foreign test data S5c-1 names (`ClutchGeneralityTests.cs:443`; the `Foreign` constant in `WorkshopSaveTests`). S11 scans the tracked sources, excluding `_bmad-output/` and `docs/`, which hold historical evidence such as `_bmad-output/implementation-artifacts/evidence-9r-0c/`. The remaining TypeScript ones are reworded to name the enum member: `tools/e2e/workshop-driver.ts:139` and `:929`, `tools/e2e/run-reset-snapshot.ts:321`, and `tools/e2e/unit/run-reset-snapshot.test.ts:86`.
- **The snapshot is derived.** The Run/Reset snapshot flattens generated unpacked read and save objects to camelCase paths and decoded scalar values (`boolean`, `string`, `number`, `bigint`, `null`). Compare with `Object.is`, including signed zero and NaN equality, independently of encoding presence for decoded scalar defaults. Include vector lengths, use exact bigint `id` keys only where the first generated row field is `id` and its value is bigint, and indices for other vectors, and refuse duplicate paths. Preserve the existing top-level advancing-response exclusions, plus the explicit `read.motion` exclusion in D11. Load alone ignores the value at `saved.construction.revision`. The Applied and exact declared body-count checks, tick bound and physical-change witness stay in force. A declared zero is valid for the static capacity bench. An empty read for a positive-count bench still refuses.
- **The suites follow the derived form.** Their snapshot path strings follow the derived path spelling, and their asserted values follow the derived value form that `snapshotValue` returns (stated in `run-reset-snapshot.ts`), including booleans, exact bigint identities and decoded authored f32 metres. A Pipe's `length` is a declared parameter key, not an instance field. These suites stay unexecuted until the browser phase.
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
| S11 | The tree, code and comments | Outside the allow-list (generated code, the vendored runtime, `WireSchemaLayout`, and the marked historical-fixture regions of H-T3 and H-T4): (1) none of these names: `SNAPSHOT_SCHEMA`, `TheVersionlessRecordsDeclareExactlyTheirFields`, `InstanceRecordFields`, `SavedConstructionSlot`, `ConstructionInstancesSlot`, `HoldsFieldBeyondTheInstanceTable`, `ValidateBindingShapes`, `TableTarget`, `COMMAND_IDENTIFIER`, `RESPONSE_IDENTIFIER`, `SAVE_IDENTIFIER`, `CommandIdentifier`, `ResponseIdentifier`, `ObservationIdentifier`, and `enum Field` in `WorkshopSaveRejectionTests.cs`; (2) no `\b(CWCM|CWRM|CCWO|CCWS)\b`, in code or comments; (3) C#, via Roslyn syntax: no integer literal as the argument of `StartTable`, as the slot argument of a `FlatBufferBuilder.Add*` call, or as the argument of `__offset`, and no hard-coded vtable-slot arithmetic in the observed argument or address forms `4 + 2 * x`, `4 + x * 2`, `x * 2 + 4` and a base plus those terms (including the left-associated spelling); (4) TypeScript, via the pinned compiler's syntax tree: no integer literal as the first argument of `startObject` or `addField*`, or the second of `__offset`; (5) the only hand TypeScript file with normalized static references to `__has_identifier` or `SIZE_PREFIX_LENGTH` is `tools/runtime/wire-frame.ts`. Markers: `// historical layout: begin` and `// historical layout: end` pair, do not nest, and appear only in the files H-T3 and H-T4 name, with the number of regions pinned per file. Stated limit: a new restatement under a new name is review's to catch | The guard names file and line |
| S14 | The seven C# domain mirrors of wire enums (interim, until `ENGINE-WIRE-ABI`): `WorkshopCommandKind`, `WorkshopResponseKind`, `WorkshopSimulationPhase`, `WorkshopCommandOutcome`, `ElectricalEnable`, `RunControlLane`, `RunControlValueKind` | Each has the same complete name-to-numeric-value map as its generated `CuriousContraptions.Wire` enum, independent of declaration order or storage type | Names the pair, member and values |

</intent-contract>

## Code Map

**Checkout and policy:** `/Users/aidan/orca/workspaces/tim/slice-s5c-3`. The baseline is verified local HEAD, tracking ref and published main. Policy and effective Auto inputs are from that checkout, with no owned policy delta (E2). Root holds the publish token. Lane 2 stops after S5c-3.

**Planning verdict:** bounded dispatch re-audit complete (E2–E3). Independent planning Scoped Pass on the repaired contract, `ctx_dc2f443ae1c3` / `msg_cdd38c5d555b` (E5).

**Review tier:** engine (TypeScript tools, test helpers and many test files). **Shape:** one structural slice.

Owned scope:
- **`CuriousContraptions.tests/WireSchemaGuards.cs`**: adds S11 (`NoHandSchemaRestatementRemains`).
- **`CuriousContraptions.tests/WireEnumMirrorTests.cs`** (new, interim): S14. `ENGINE-WIRE-ABI` deletes it together with the mirrors.
- **`CuriousContraptions.tests/WireSchemaText.cs`**: shares the existing `WireSchemaLayout` parser and named table-slot overloads across the current fixtures. Its layout calculation is the S11 exception, scoped to `WireSchemaLayout` itself.
- **S11 parser repair (E16):** `tools/runtime/wire-source-guard.ts` uses the already emitted pinned compiler harness path to inspect only the existing TypeScript call forms. `WireConformanceTests.cs` supplies its existing batched Node JSON launch seam to both harnesses. The C# guard retains the C#/MSBuild, identifier/frame and historical-region policies. No build or dependency changes.
- **Retired save fixture repair (E16):** `WorkshopFormatTests.cs` reads `CuriousContraptions.tests/Fixtures/pre-versionless/retired-versioned-construction.save.base64`. The frozen E15 archived payload and current frame reach the typed C# verifier refusal. This file replaces that historical region in the C# source. Five remaining source regions stay pinned.
- **H-T2:** `WorkshopSaveRejectionTests.cs:17-21` (`enum Field`, the `InstanceRecord` fields in schema order) is derived through `WireSchemaLayout`.
- **H-T3:** the files with literal slots or vtable sizes take their slots from `WireSchemaLayout`, and struct byte offsets are rewritten through generated builders or accessors: `BodyOwnershipTests.cs`, `ClutchGeneralityTests.cs:378-462`, `CompiledSceneBoundaryTests.cs:89-761`, `CompoundMassAndRollingTests.cs:880-896`, `DrivenSurfaceBindingTests.cs:513-553`, `FanAdmissionTests.cs`, `IndependentSignalCompositionTests.cs:268-312`, `OwnedFiniteWorkTransactionTests.cs:196-198`, `PayloadInstanceTests.cs:268-380`, `RetentionBoundaryTests.cs:117-122`, `:372-397`, `RopeTests.cs`, `RunControlTableTests.cs:102-104`, `TrampolineTests.cs:571-574`, `WindmillAdmissionTests.cs:281-289`, `WorkshopWireTests.cs:918-922`, `MechanicalCoordinateBindingsTests.cs:1019-1052`, `MechanicalLinkMappingTests.cs:144-150`.
- **Historical-fixture regions** gain one paired region per file around the actual retired layout: `OldLayoutSaves.cs:11-80`, `DeclaredOscillatorTests.cs:174-178`, `DeclaredOrientationTests.cs:148-152`, `PayloadInstanceTests.cs:268-317` (pre-conversion records), and `ClutchGeneralityTests.cs:428-445` (retired command/response). Keep current-wrapper field references generated or derived as applicable, and leave `GenericSave`, the current unknown-field Clutch control, and current positive/refusal assertions outside the exceptions.
- **`tools/e2e/run-reset-snapshot.ts`** (H-S1): `:5-6`, `:13-146` (`SNAPSHOT_SCHEMA`), `:156-158`, `:167` and `:182-293` (the hand layout and decoder) are deleted; the snapshot is derived from the object API (Always). `:321` is reworded. `:371` (`MemoryKind`) is H-W6's.
- **`tools/e2e/unit/run-reset-snapshot.test.ts`** (H-S2): the schema-listing checks (`:17-22`, `:86`, `:106-186`) are deleted, because nothing is left to compare. Its other cases follow the derived paths. Its save fixture writes a generated puzzle string ID and required `PuzzleRecord.assistance` vector (BH3-4). It directly exercises authored projection revision equivalence, meaningful changed authored fields and absent mock motion through `motionArray() === null`. It stays deferred and is not run (A6-7).
- **`tools/e2e/workshop-wire.ts`**: `:82-136` (`authoredState`; H-S4) is the actual authored comparison surface. Use the generated save object, retaining `nextBodyId` and all decoded authored settings, atmosphere, instances, connections and puzzle fields. Exclude the page-local construction revision so existing reload comparisons still express authored restoration. `:138-364` (H-S5) uses generated object classes with named properties for nested values and generated root builders. Preserve response motion absence with the direct response builder, as in S5c-2 D11. Remove the unused retired `MockCommandParams.settings` field. `tools/e2e/cat-005.test.ts:396-459` supplies those settings to a scene-less cadence command, which current C# refuses. Retire that forged command scenario from the executable deferred file and carry its browser qualification with the post-catalogue owner. Cite existing native `WorkshopActivationAnimationTests.DeclaredCadenceBoundaryRestartsOrdinalsAndRejectsReplay` and `WorkshopWireFlatBuffersTests.Command_ConfigureCadence_RoundTrips` for the current cadence boundary. A live browser case requires the production compile-to-command path and belongs to the browser gate.
- **Affected repair consumers (E12):** direct H-S4 forwarding definitions in `tools/e2e/cat-052.test.ts`, `cat-062.test.ts` and `cat-062b.test.ts` join the already-owned `cat-005.test.ts` and `cat-051.test.ts`. Delete the forwarding names and call `authoredState` directly. The CAT-051-own-supply projection repair uses the existing pure helper closure for its deferred decoded controls. E14 consolidates the alternate IndexedDB readers in the nine already-owned CAT-005, CAT-017, CAT-020, CAT-051, CAT-051-own-supply, CAT-052, CAT-052-lamp, CAT-062 and CAT-062b suites through WorkshopDriver's existing capture.
- **The suites' snapshot strings** (H-S6): paths follow the derived spelling and values the derived value form (Always; A6-7): `capacity-9r5b.test.ts:109-111`, `cat-020.test.ts:97-107`, `:303` (require an actual Counter row because the length path exists at zero), `cat-017.test.ts:106-115`, `:224-357` (activation memory if needed by its decoded value contract), `cat-050-joined.test.ts:188-294`, `cat-051.test.ts:74`, `cat-051-own-supply.test.ts:250-252`, `cat-052-lamp.test.ts:108-110`, `pkg-3.test.ts:108-110`, `tube-mouths.ts:38-68`, `tube-snapping.test.ts:117`, `:178`. `pipeLength` and the direct Undo assertion read the selected instance's generated parameter by the Pipe declaration's `length` key, or select the same decoded parameter rows from the derived projection. Retain refusal when the key is missing. Activation memory preserves node, kind, count, ordinal, phase and due tick with generated values and any existing deliberate timer semantics.
- **`tools/e2e/workshop-driver.ts:139`, `:886`, `:929`**: comments reworded to name the enum member.
- **`schemas/README.md`**: the `SNAPSHOT_SCHEMA` reminder that remains of `:90-91` is deleted.
- **Current save projection (E19):** `workshop-wire.ts` shares `decodeSavedConstruction` before unpacking and requires the generated current settings field. The same committed retired fixture proves deferred refusal. No historical layout detection or schema verifier.
- **Direct pass-four consumers (E19):** the `RunResetSnapshot` type has one defining module, so the driver drops its re-export and the current direct importers take it from `run-reset-snapshot.ts`. In `tools/e2e/capacity-9r.test.ts`, use the shared exact-count snapshot with a declared zero body count, preserving the committed-read, population and reload checks. Reset includes the authored revision, and Load alone excludes it.
- **Pass-five direct saved-slot callers (E21):** `cat-052-two-plates.test.ts`, `cat-015a.test.ts`, `cat-015a-sidekick.test.ts`, `cat-015b.test.ts`, `cat-037.test.ts` and `palette-placement.test.ts` use the shared current save admission. Remove their parallel slot readers while preserving the test actions and decoded assertions.
- **Planning records:** this contract and its run log; `TODO.md`; `sprint-status.yaml` (key `s5c-3-remove-hand-schema-restatements`); `delivery-batches.md` (S5c); `deferred-work.md` (BH3-4 source repair, the stale CAT-005 cadence exercise and explicit post-catalogue re-check). Existing owner-story citations are the `ENGINE-WIRE-ABI` and `ENGINE-CAPABILITY-INSTANCES` story sections of `_bmad-output/planning-artifacts/epics.md:4630–4703`. The engine story, schema, production codecs, build files, conformance harness and browser deferral list are verification inputs.

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
| H-T3 | The files listed in the owned scope; the five historical regions and committed retired versioned-save input in the current Code Map | Field ordinals, vtable sizes and struct byte offsets | S5c-3: slots through `WireSchemaLayout`, struct offsets through generated builders or accessors; historical regions marked and allow-listed |
| H-T4 | `DeclaredOrientationTests.cs:137-138`, `:148-151`, `:157-181` | A slot comment, a historical layout and a vtable walk | S5c-1: comment and walk deleted; S5c-3: the region marked |
| H-S1 | `tools/e2e/run-reset-snapshot.ts:13-146`, `:156-167`, `:182-293` | Both schemas in full, scalar sizes, struct layout and a hand decoder | S5c-3: deleted; derived from the object API |
| H-S2 | `tools/e2e/unit/run-reset-snapshot.test.ts:17-22`, `:86`, `:106-186` | A schema-text parser, listing checks and `'CCWS'` | S5c-3: deleted |
| H-S3 | `tools/e2e/workshop-wire.ts:14-16`, `:20-29` | Identifier literals and a second frame check | S5c-2: deleted; `wire-frame.ts` |
| H-S4 | `tools/e2e/workshop-wire.ts:82-136` | Partial field lists of `InstanceRecord`, `ConnectionRecord` and `PuzzleRecord` | S5c-3: derive all their fields plus settings, atmosphere and next ID, omitting construction revision from authored comparisons |
| H-S5 | `tools/e2e/workshop-wire.ts:153-381` | Positional struct-creator argument order (for example `createBodyRead`, 22 arguments) | S5c-3: object API with named properties |
| H-S6 | The suites' snapshot strings (owned scope) | Field names as path text | S5c-3: assertions use decoded scalars and derived paths; Pipe length uses the declared parameter key |
| H-R1 | No TypeScript FlatBuffers verifier | A missing capability | **Owner `ENGINE-WIRE-ABI`.** Its first slice that adds a production TypeScript table decoder provides TypeScript verification without hand code; the candidate is a generic verifier driven by the compiler's binary schema (`flatc --binary --schema`), with accessors generated from upstream `reflection.fbs` vendored under an integrity pin (24p ruling (a)) |
| H-W4 | `CuriousContraptions.Abstractions/WorkshopMessageValues.cs:11-13`, `RuntimeServices.cs:10-11`, `ElectricalSourceValues.cs:36`, `ParameterDescriptor.cs:50`, `RunControlValues.cs:11`; numeric casts in `engine/gpu/WorkshopWireFlatBuffers.cs:137`, `:147`, `:166`, `:530-533`, `:586-589`, `:761` | Seven C# enums mirroring schema enums by value | **Owner `ENGINE-WIRE-ABI`** (ruling (f); C36 amended). S5c-3 adds the interim S14. Scalar-encoded closed sets (for example `WorkshopCommand.revision_kind: uint32` mapped to `ExpectedRevisionKind`) have no generated enum pair here and follow the same owner's W4 gate. |
| H-W6 | `CuriousContraptions.web/wwwroot/workshop-observation.ts:21`, `:39-40`, `:47` (wire enums); the rest of `:3-49` and `run-reset-snapshot.ts:371` (`MemoryKind`) | TypeScript enum mirrors | Wire enums: **`ENGINE-WIRE-ABI`**, from the generated module. Non-wire enums: **`ENGINE-CAPABILITY-INSTANCES`**, through its C#-to-TypeScript generator |
| H-W7 | `engine/gpu/WorkshopCadence.cs:61-93` and every non-FlatBuffers boundary layout | Hand layouts | **Owner `ENGINE-WIRE-ABI`**, declared in the one file (24p) |
| H-W3 | `schemas/workshop_wire.fbs:305-306` (`motion: [uint8]`); `engine/gpu/PhysicsMotionRead.cs:47-52` | A hand layout tunnelled through the schema | **Owner `ENGINE-WIRE-ABI`** |

Checked and unchanged: `BundleElementIdTests.cs:213` names two fields in an assertion, not a layout; `tools/WireContract/` and `tools/LifecycleContract/` are separate JSON design fixtures.

### Capability probe

No solver law, worker message content, wire field or shared presentation contract changes.
- **Present:** `WireSchemaLayout.{FieldCount,Slot,VtableOffset}` (`WireSchemaText.cs:296–314`), proved by `WireSchemaTextTests.EveryTableFieldCountEqualsItsGeneratedAddMethods` and `WireConformanceTests.VerifierFailuresAreRefusedInCSharp`; generated C#/TypeScript builders, `unpack` and frame checks, proved by `WireConformanceTests` response/save round trips and shared frame refusals; compiler replay and file inventory from `CSharpStaticGateTests`, exercised by S10's real-source check and `TheWorkerReferenceRuleRefusesEachForbiddenBinding`.
- **Limited by design:** generated unpack applies canonical scalar defaults and represents absent vectors as empty. Direct generated root building preserves actual absence where required, as proved by the S5c-2 response tests. `WireSchemaLayout` derives table slots only. Struct fixture fields use generated struct construction or access.
- **Missing, added here:** the restatement guard (S11); the interim enum-mirror check (S14).
- **Missing, owned elsewhere:** enum ownership and the TypeScript verifier (`ENGINE-WIRE-ABI`). No required-now owner decision is open. The direct consumers and shape/tier are listed above. The read-only scout evidence is E3.

### Decisions

- **D11, decoded snapshot and motion.** Compare canonical scalar values with `Object.is`. Omit opaque `read.motion` from flattening, since comparing its number vector compares encoded render bytes. S5c-2's `CSharpDecodesEveryResponseTypeScriptEncoded` proves motion presence, piece metadata and sampled poses through `PhysicsMotionRead`; H-W3 still owns the ABI. See E3.
- **D13, authored state and non-vacuous Run.** Keep full authored values except page-local revision. Compare snapshot path sets and values in both directions. Exclude the exact top-level response metadata subtrees `kind`, `outcome`, `reason`, `tick`, `phase`, `physicalCommit`, `memory` and `scene` from the Run-change witness. A handshake, diagnostic or signed-zero-only change fails even with a positive tick. A numeric leaf can witness Run only when both values are finite. A meaningfully moved body or changed committed activation memory passes. Reset and Load keep decoded `Object.is` comparisons, including signed zero and the exact typed admission physical commit identity (E12–E15). Reorder ID-led vectors and preserve indexed vectors, including `RunControlRecord` whose `source` precedes `id`; duplicate identity paths must fail.
- **D12, S11's accepted forms are a closed list** (A6-8). Parse the three named TypeScript call forms with the pinned compiler. Match the named call through an identifier, dot access or a literal string or template bracket key. Unwrap grouping, assertions, non-null, satisfies and type arguments for the callee, key and slot. A numeric literal represents an integer slot when its parsed value is a finite whole number. The same AST scan checks normalized static references to both frame names in identifiers, literal element keys, quoted binding and property assignment keys, literal computed keys, and quoted import and export specifiers. The adapter exception applies only to those references. Slot-call checks still run inside the adapter. See E16, E19, E21, E23 and E25.

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
- AC5: Given a current generated frame, when the tooling projects, compares or encodes it, then decoded fields/identities obey D11 and D13, comparable authored settings and parameters survive Save/Load, and a mock response without motion carries no motion vector. Native conformance, strict helper compilation and the deferral check run now. Actual Chrome/Playwright and the existing snapshot unit suite qualify at the post-catalogue E2E gate.
- AC6: Given the baseline, when this candidate is inspected, then the schema, production codecs, runtime generator and browser deferral list match the baseline by tracked source scope. Whole-output byte equality is not a test.

## Spec Change Log

- 26 November 2026: created by the S5c split (coordinator ruling; audit 6, A6-10), from the S5c contract and audits 1 to 6. Carries A6-7, A6-8's S11 bullet and A6-11's citations (run log E1).

- 28 November 2026: planning repair (E4). Keep existing Reset/Load identities and test witness semantics explicit while refining the decoded Run witness.
- 28 November 2026: dispatch reconciliation on published S5c-2 (E2–E3). Keep exact semantic authored restoration, established refusal behavior, no schema changes, no encoded output equality, and current browser deferral. The stale bit-pattern example, orphaned dump symbol and obsolete fragment of the cadence mock are superseded.

- 28 November 2026: bounded S11 implementation-plan amendment after the third related argument-match finding. The pinned compiler parser implements the same closed forms. The fixed Story 9R.7 retired payload moves into committed data per the contract-test rule. Keep the C# slot and frame checks, no schema change, sealed H-R1 deferral and decoded Run/Reset semantics. Independent bounded planning Scoped Pass `ctx_b60a6bfe2d04` / `msg_1f4ce3d8b074` (E17).

## Review Triage Log

- Auto first review uses the 47 owned paths at E10, engine tier. E11 records the six effective layers and the open S11 check for reordered named arguments.

- Planning audit `ctx_c64a446d6cac` / `msg_6ce0741412d4` found an overly broad Run witness and underspecified deferred cases. Plan repaired before implementation (E4), with the independent follow-up verdict required.


### 28 November 2026 — Implementation review pass 1

- Verdicts: 17 findings. High 6, medium 8, false 3. E12 records the six independent receipts and sources. Related findings share a retained-owner repair.
- BH1 [medium] [patch] Run witness accepts comparable diagnostic/envelope leaf changes. Exclude the nonphysical read metadata for this witness alone and retain physical activation memory.
- BH2 [false] [reject] Canonical `Object.is` treats decoded NaNs equally by D11. The comparator does not perform producer or admission validation.
- BH3 [medium] [patch] The late-path control mutates the generated last slot but hard-codes atmosphere in its diagnostic. Derive the diagnostic from the selected path.
- BH4 [medium] [patch] The historical settings struct placement follows the live schema. The retired fixture's own slot remains fixed.
- BH5 [false] [reject] The CAT-005 retirement is mapped by deferred-work and both cited native cadence tests passed in E8. The blind product diff excluded that receipt.
- ECH1 [high] [patch] Reordered named slot arguments escape S11 or report a non-slot literal. Select the actual parameter and test safe arguments.
- ECH2 [medium] [patch] Parenthesized TypeScript literal slots escape the already listed call forms.
- VG1 [false] [reject] The retired TypeScript arithmetic is gone. S11 explicitly limits TypeScript slot detection to literal arguments at the listed call positions. Its arithmetic rule applies to C#.
- VG2 [medium] [patch] Restore isolated decoded contact occurrence count and residual Reset tick comparisons in the deferred fixture.
- VG3 [medium] [patch] Same fixed historical settings-slot defect as BH4.
- IA1 [medium] [patch] Same fixed historical settings-slot divergence as BH4.
- IA2 [high] [patch] Same named C# slot divergence as ECH1.
- IA3 [medium] [patch] Valid binary TypeScript integer literals are the same listed form and escape S11. Cover valid integer spellings in those calls.
- FO1 [high] [patch] Remove the retained `authored` forwarding name from the five direct consumers. Call the current `authoredState` directly.
- EF1 [high] [patch] Same S11 actual-parameter defect as ECH1.
- EF2 [high] [patch] CAT-051 compares a decoded bigint owner to a string, preventing a valid projected row from supplying its enable. Select the exact typed owner and discriminate two owners and missing ownership.
- EF3 [high] [patch] The edited response mock exposes raw electrical enable values despite the generated enum. Use the enum at the mock boundary and in affected semantic fixtures, preserving false/zero and nullish controls.

- Retained owner repaired the routed findings in `ctx_0266100f1250` (E13). The rejected claims in E12 required no change. Independent repair review is pending.


### 28 November 2026 — Implementation review pass 2

- Verdicts: 12 findings. High 5, medium 7. E14 records the five affected receipts and the proof behind each repair.
- BH1 [medium] [patch] S11 can misclassify a nested argument comma as the TypeScript offset slot separator.
- BH2 [medium] [patch] The inner historical ConstructionRecord follows the live schema. Freeze its retired table and settings payload.
- BH3 [medium] [patch] Exercise the new committed-enable reader's invalid decoded byte refusal.
- ECH1 [medium] [patch] Same nested argument boundary as BH1.
- ECH2 [medium] [patch] Trivia around a literal can hide the same S11-listed TypeScript argument.
- ECH3 [high] [patch] Owner-only source selection silently chooses a row on a multi-source owner. Require unique selection for this fixture query.
- ECH4 [medium] [patch] Signed zero alone satisfies the Run witness without physical change. Preserve the exact signed-zero restoration comparison.
- VG1 [high] [patch] The S11 nested-comma miss and false positive need paired structural controls.
- VG2 [medium] [patch] Same untested invalid enum boundary as BH3.
- EF1 [high] [patch] Same D1 source ambiguity as ECH3, proved by a production dual-source owner.
- EF2 [high] [patch] Same S11 nested-comma defect as BH1.
- FO1 [high] [patch] Direct affected callers use a second IndexedDB reader and can repeat Save's slot read. Use the existing driver reader or the slot from the current Save. E14 names the verified same-pattern closure of nine suites.

- Pass 2 repairs returned from the retained owner in `ctx_70fa7a83611e` (E15). Independent repair review is pending.


### 28 November 2026 — Implementation review pass 3

- Verdicts: 11 findings. High 5, medium 5, false 1. E16 records the confirmed scanner, legacy-fixture and Run witness repairs. The Forward-Only auditor recorded Scoped Pass.
- BH1 [high] [patch] The S11 TypeScript matcher misses the actual offset slot after a generic row expression. Replace the repeatedly failing lexical argument matcher with the pinned parser.
- BH2 [medium] [patch] The retired versioned payload still originates from executable fixture assembly. Commit the E15 current-framed retired input as data.
- BH3 [medium] [patch] Non-finite changes alone can witness Run. Require finite changed numeric values for this witness.
- ECH1 [high] [patch] Same TypeScript actual-offset defect as BH1.
- ECH2 [medium] [patch] TypeScript line-comment endings, including Unicode separators, can hide the slot. Recognize the compiler's parsed call.
- ECH3 [medium] [patch] Same non-finite Run witness as BH3.
- ECH4 [false] [reject] The frame reader has the unchanged H-R1 boundary. TypeScript checks frame, identifier and a construction accessor. C# owns structural verification and typed Load refusal. No caller passes a retired payload to the storage observer as an admitted current construction.
- VG1 [high] [patch] Same TypeScript actual-offset gap as BH1. Exercise the generic row against both literal and derived slots.
- VG2 [medium] [patch] The repaired argument collector lacks a discriminating quoted-punctuation control. Keep a quoted first-argument case in the parser controls.
- VG3 [high] [patch] Contract-test guidance requires the affected legacy input in a committed fixture file. Preserve the precise verifier-rejection boundary.
- EF1 [high] [patch] Same S11/AC1 actual-argument gap as BH1, independently confirmed by the pinned parser.


### 28 November 2026 — Implementation review pass 4

- Verdicts: 7 findings. High 5, medium 2. E19 records the narrow syntax/aggregation repair and direct-caller closure.
- BH1 [high] [patch] Constant string bracket access calls the same named S11 methods but escapes the AST guard.
- BH2 [medium] [patch] Decimal/exponent numeric literals that resolve to whole slots are missed. Use the parsed numeric value for the literal rule.
- ECH1 [high] [patch] Same literal bracket-call boundary as BH1.
- VG1 [medium] [patch] The direct parser controls do not exercise the repository guard's batching and diagnostic aggregation. Share the path and prove native guard failure with a synthetic TS source.
- FO1 [high] [patch] Use the one current TypeScript save admission for projected saves and require the generated cadence settings field the C# construction contract requires. Keep general structural verification with H-R1.
- FO2 [high] [patch] Remove the driver re-export of the rewritten RunResetSnapshot type and update its direct imports.
- EF1 [high] [patch] The direct capacity fixture compares full stored slots after Reset. Keep its zero-body committed-read proof and compare decoded authored state, preserving exact Reset revision separately.


### 28 November 2026 — Implementation review pass 5

- Verdicts: 12 findings. High 4, medium 7, low 1. E21 records the fixes and why the zero-body test keeps its existing static composition.
- BH1 [medium] [patch] Parenthesized known call targets escape the parsed slot guard.
- BH2 [medium] [patch] A substitution-free template key is a literal call name and escapes.
- ECH1 [medium] [patch] Same literal template-key defect as BH2.
- ECH2 [medium] [patch] Same grouped call-target defect as BH1.
- ECH3 [medium] [patch] Transparent TypeScript type wrappers hide a direct numeric slot.
- VG1 [medium] [patch] Pair an admitted handwritten E2E path and excluded generated path in the same mixed batch.
- VG2 [medium] [patch] Same grouped and literal-key defects as BH1/BH2.
- FO1 [high] [patch] Root approved one shared exact-count snapshot. Explicit zero on the static fixture uses the existing capture and comparators. An empty read for a positive-count bench still refuses.
- FO2 [high] [patch] Six more direct users of the repaired save admission retain the alternate IndexedDB read. Use the existing driver Save result or scoped read-only reader.
- EF1 [high] [patch] Same grouped TypeScript call-target defect as BH1.
- EF2 [high] [patch] A bracket identifier-check call bypasses the single frame adapter rule. Use the same parsed call detection outside its one permitted module.
- EF3 [low] [patch] Refresh the stale TODO handoff status to name the actual review check.


### 28 November 2026 — Implementation review pass 6

- Verdicts: 6 findings. High 2, medium 4. Both informed auditors recorded Scoped Pass for their review scope. E23 records the confirmed frame ownership and control repairs.
- BH1 [medium] [patch] The static identifier check is missed when directly referenced for Function.call.
- BH2 [medium] [patch] A direct size-prefix read with escaped identifier spelling bypasses source text matching.
- BH3 [medium] [patch] The current adapter control does not show that literal slot checks still apply there.
- ECH1 [high] [patch] Same direct identifier-access rule as BH1.
- ECH2 [high] [patch] Same normalized size-prefix access as BH2.
- VG1 [medium] [patch] Same adapter frame-only exception proof as BH3.


### 28 November 2026 — Implementation review pass 7

- Verdicts: 4 findings, all medium. Both informed auditors recorded Scoped Pass. E25 records the confirmed coverage limits in the replaced frame check.
- BH1 [medium] [patch] Direct quoted/literal-computed destructuring keys bypass normalized frame authority.
- ECH1 [medium] [patch] Same static binding-key defect as BH1.
- ECH2 [medium] [patch] Quoted import or re-export names can select the same frame APIs without a diagnostic.
- VG1 [medium] [patch] Cover the binding-key case through the same native guard while keeping admitted plain strings and the adapter slot check.


### 28 November 2026 — Implementation review pass 8

- Verdicts: two findings. Medium one, false one. Both informed auditors recorded Scoped Pass and Blind Hunter found no defect.
- VG1 [medium] [patch] Add the binding-element default string to the same native admitted batch. It selects no frame member and distinguishes it from a quoted binding key.
- ECH1 [false] [reject] A namespace export called `"SIZE_PREFIX_LENGTH"` names the namespace itself. It does not read or call a FlatBuffers frame member. E27 traces the distinction from a quoted source-member import or re-export.


### 28 November 2026 — Implementation review pass 9

- Verdicts: zero findings. Verification Gap and both auditors independently accepted the admitted binding-default control (E29). The scanner behavior is unchanged from pass 8, with no unresolved Blind Hunter or Edge Case Hunter finding after triage. Intent Alignment ran on the first pass. Final diff scope follows.

- Final diff scope on the E29 candidate failed on one concrete inefficiency. S11 parses the default C# source tree twice. Return that bounded simplification to the retained owner and recheck the repaired candidate (E30).

- Final-scope repair re-review has zero findings. Verification Gap and both auditors confirmed equivalent default-tree reuse. Independent sequential scope recheck follows (E32).

- Final diff scope Pass, `ctx_aa5171af51ec` / `msg_0958c518de7c`, all 68 paths and 248 hunks after the duplicate default parse was removed. Matching compiler and maintainability evidence (E31, E33).

## Implementation Notes

- Retained implementation owner `ctx_dd594489caec` / `task_0977d053b63d`, a fresh Orca Codex session in the physical checkout. E6 records the handoff.

- Engineering families E2, E4, E5 and E7–E9 apply to the test and tool paths. The existing element-free compiler, worker ABI, solver and presentation contracts supply the unchanged runtime behavior (E7–E8).
- The implementation owner inspected the full owned diff before the full round. The inherited retention refusal fixture now edits the generated retention identity and preserves its typed discriminator (E8).
- The owner inspected the E19 repair diff and checked that 19 of the 20 direct snapshot type importers have import-only changes. The capacity suite keeps its explicit population, zero-body read and individual reload checks. The E21 repair uses the shared exact-count capture and comparison path for the static capacity bench. Each Reset compares authored fields and exact construction revision. Load ignores the restarted construction revision value. The six confirmed storage caller changes preserve each Save or read-only observation. The owner also inspected the E16 repair diff, including the fixed fixture and emitted tool. S11 has one TypeScript syntax path through the shared Node launcher. Five historical source regions keep their original refusal cases. The retired versioned construction is a fixed input fixture. Current table fixtures use shared layout and generated struct builders. TypeScript projects decoded scalar values and excludes opaque motion from comparison.

## Design Notes

1. **Delivered:** owner decision 24p requires one FlatBuffers schema authority. REQ-07/09 protect typed fields and exact authored restoration. REQ-11/13 preserve the typed browser boundary and deferred UI qualification. REQ-12/14 govern order and independent proof.
2. **Reusable:** `WireSchemaLayout` for test slots; the object API for snapshot, dump and mocks.
3. **Declared:** the schema file only.
4. **Execution:** test builders address generated fields or slots derived from the schema. Existing codecs admit and execute commands; decoded tool assertions use their generated accessors.
5. **Ownership:** generated types own layout. Existing production codecs own admission and atomic installation. Reset/Load observers own only their projections and revision exclusions.
6. **Generality:** decoded projections admit current schema fields automatically. Identity reorder, authored changes and scoped exclusions discriminate against field loss and positional copies.
7. **Deleted:** `SNAPSHOT_SCHEMA` and its hand decoder; the schema-listing checks; the partial field lists; positional struct creators; the literal slots outside historical fixtures.
8. **Uncertain:** browser-phase re-check of the snapshot test; enum ownership (`ENGINE-WIRE-ABI`). Nothing required now is open.

**Architecture guards.** S11 reads source; it lives in `WireSchemaGuards` with positive controls.

## Verification

Executor: retained implementation owner for incremental work, matching captures, maintainability, registration, affected checks and handoff. Coordinator owns the final merged round after Final diff scope Pass, normal hooks and independent SnapshotApproval. See E2 for the published 7767/0/0 baseline.

Environment: `PATH=~/.local/share/tim-toolchain/dotnet-10.0.403:~/.local/share/tim-toolchain/node-v26.12.1-darwin-arm64/bin:$PATH` and `DOTNET_ROOT=~/.local/share/tim-toolchain/dotnet-10.0.403`. Record the session environment check in the run log.

**Evidence:** retained owner executed both solution Debug compile variants, the Debug web-host compile, the two file-based compiles, maintainability, registration, the scoped native checks, and strict no-emit. Raw paths and invalidating inputs are in [E8–E9](run-logs/spec-s5c-3-remove-hand-schema-restatements.md#e8-implementation-and-early-evidence-28-november-2026). The retention fixture correction refreshed the two relevant solution logs. The retained owner ran the full local round on those matching inputs (E10). The first two repair passes refreshed the changed solution captures and scoped checks (E13, E15). The E16 repair changed the shared runtime TypeScript build inputs and C# test sources, so the owner refreshed both solution captures and the Debug web capture and reused the two matching file-based tool captures. The E19 repair refreshed the same three captures for the changed guard and shared harness and reused the two unchanged file-tool inputs. The E21 repair refreshed the three affected captures for the changed guard, bridge and shared harness. The E23 repair refreshed the three affected captures for the changed guard and shared harness. The E25 repair refreshed the three affected captures. The E27 control repair refreshed both solution captures and reused the matching web and file-tool captures. The E30 parse reuse repair refreshed both solution captures and reused the matching web and file-tool captures. E18, E20, E22, E24, E26, E28 and E31 record the matching commands, invalidating inputs and raw results. Normal hooks and independent qualification are the coordinator's publication checks.

**Commands:**
- The five compiler-log capture commands (`docs/delivery-workflow.md:38-53`). Expected: every build succeeds.
- `dotnet run --file tools/anvil/VerifyPolicy.cs -- --maintainability --project-root .` Expected: AC4.
- The registration check filter. Expected: 0 failed, 0 skipped (a test file and an emitted test tool are added).
- The scoped run of the guard, the mirror test and the edited test classes (named at dispatch from the H-T2 and H-T3 list). Expected: 0 failed, 0 skipped.
- `git add -N` on the new files, then `git diff --check <baseline_revision>`. Expected: clean.
- Pinned root `tsc` strict no-emit on the changed browser-independent TS helpers and H-S2 fixtures using generated accessors, without loading the browser compiler configuration, Playwright or `tsx`. Only the two pure helpers, tube geometry helper and unit fixture closure enter that command.
- One full local round at handoff and one after Final diff scope Pass only if required inputs changed. Execute under `caffeinate -i`; AC2 and normal hooks are separate. Root owns fast-forward publication; remote build, Pages artifact, deployment, browser, performance and release qualification remain unverified.

### Acceptance-to-test table

Use the quantity-specific [float comparison and effort rules](../../docs/testing-guidance.md#float-comparison-and-effort-rules). Exact checks protect IDs, explicit authored f32 values and canonical interfaces. Fixtures use meaningfully unequal decoded values; no archived drift, ULP, NaN payload, encoded motion bytes or incidental trajectory check. Reuse passing evidence unless inputs or a concrete coverage gap change.

| Rows | Named discriminating test | Control and boundary | Execution |
|---|---|---|---|
| S11 | `WireSchemaGuards.NoHandSchemaRestatementRemains` and `SchemaRestatementGuardRefusesTheClosedFormsAndChecksHistoricalRegions` | Control: synthetic sources holding `StartTable(12)`, `"CWCM"`, a `SNAPSHOT_SCHEMA` declaration, a `4 + 2 * slot` expression or a second TypeScript frame check, outside the allow-list, are each refused, naming file and line. Boundary: a marked historical-fixture region is admitted, and a restatement outside its markers in the same file is refused; an unpaired or nested marker is refused | Both passed (E31). The C# guard reuses the default root and retains the Debug and Playtest parses. The mixed batch distinguishes the admitted binding default string from the forbidden quoted binding key. It includes quoted and literal-computed binding and assignment keys and quoted import and export specifiers. It retains admitted computed names and standalone strings and the frame-only adapter exception with a refused literal slot. |
| S14 | `WireEnumMirrorTests.EachDomainMirrorOfAWireEnumMatchesItsGeneratedDeclaration` and `EnumComparisonReportsWrongValuesAndNamesAcrossStorageTypes` | Control: a synthetic pair that differs in one member's value, and one that differs in one name, are each refused | Both passed (E8). |
| AC1 | S11 controls and real tree | All closed forms, table-slot argument arity, one-argument scalar additions, marker count/pairing/file boundaries and nearby ordinary sources | Both S11 checks passed on the E30 repair (E31). `WorkshopFormatTests.ASaveInTheRetiredVersionedLayoutIsTheTypedSaveRefusal` passed in E18. The committed fixture carries the retired table and seven-word settings in a current frame and receives `VerifierRejected`. |
| AC2 | Full native solution round and normal hook receipt | Check all five per-project totals and explicit browser deferral against E2 | Final matching full local 7771/0/0 in 655.48 s, including the +4 native delta from the published baseline (E34). Normal hooks execute on the staged candidate. |
| AC3 | S11 plus independent inventory and owner-story review | Current schemas & helpers vs historical regions, production mapping and deferred ABI | S11 passed on the repair (E31). Deferred row owners confirmed in E9. Independent affected re-review and final scope passed (E32–E33). |
| AC4 | Maintainability and coupling canaries | Injected canary fixture only, zero candidate findings | Both canaries passed. Metrics and coupling violations 0 on the repair (E31). |
| AC5 | `CSharpDecodesEveryResponseTypeScriptEncoded`, `CSharpDecodesTheSaveTypeScriptEncoded`, strict helper compilation and deferral | Missing/present motion, sampled pose, authored versus empty construction. H-S2 directly covers authoredState revision-only equivalence and authored-field change, mock `motionArray() === null`, real puzzle ID/assistance, ID reorder/duplicate and indexed rows, exact bigint, value/path, signed zero, Apply/body-count, Load-only revision and identity-only Run refusal with moved-body control. CAT-020 checks a decoded Counter row | Native conformance passed on the E23 repair (E24). Strict no-emit and the browser suite source checks remain evidenced in E22. The deferral source matches the baseline and its executed check remains in E8. Deferred controls preserve exact Reset/Load and cover finite Run progress, nonfinite-only changes in either direction, contact counts, owner uniqueness and invalid enable bytes. The driver and all 20 direct consumers passed syntax and import checks (E20). Deferred controls cover current-settings admission, shared zero-count capture, positive-bench empty-read refusal, and exact Reset and Load-only revision rules. Seven edited browser suites passed source syntax checks (E22). Browser execution remains deferred. |
| AC6 | Independent scope check against 1b9dffe1 | Verify tracked input scope and no generated artifacts or production changes | Tracked-source scope comparison passed on the repair (E31). Independent Final diff scope Pass (E33). |

## Auto Run Result

- S5c-3 derives the affected test layouts, generated snapshots and authored save comparison, centralizes the repaired save/slot callers, and adds S11 and the seven-pair interim S14. The schema and production codecs remain at the published baseline.
- Independent affected review finished with zero unresolved findings (E32). Final diff scope Pass covers 68 paths and 248 hunks (E33). The final matching solution round passed 7771/0/0 in 655.48 s (E34). S11 and conformance passed affected checks, registration is 828/0/0, and both maintainability canaries passed with zero violations. Five matching Debug compiler captures and original receipts are named in E31–E34.
- The normal hooks check the staged candidate. Independent SnapshotApproval uses the committed SHA. Root owns publication. The post-catalogue Chrome and snapshot-toolchain tests, remote production build, Pages artifact, deployment, performance and release qualification remain unverified.
