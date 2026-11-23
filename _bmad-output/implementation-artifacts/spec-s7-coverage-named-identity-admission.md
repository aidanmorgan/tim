---
title: 'S7: Coverage admits every named identity, and a companion package is declared not placeable with a seat'
type: 'feature'
created: '2026-11-21'
status: 'blocked'
baseline_revision: 'captured at dispatch (the main that contains the planning work)'
review_loop_iteration: 0
followup_review_recommended: false
context:
  - '{project-root}/docs/testing-guidance.md'
  - '{project-root}/docs/delivery-workflow.md'
  - '{project-root}/docs/planning/invest/named-elements.md'
  - '{project-root}/tools/Coverage/README.md'
  - '{project-root}/_bmad/custom/element-free-engine-design.md'
warnings: ['two-slices']
deferred: []
---

<intent-contract>

## Intent

**Problem:** Main's Coverage reader (`tools/Coverage/NamedPackageSources.cs`) admits a named package only from an `EL-*.md` spec (`:19`) whose binding names an `EL-NNN` identity, a `todo-N` acceptance co-anchored with a `sequence-task-N`, and a `**Decision:**` that matches one `Owner **SNNN**` in the ledger (`:55`–`:61`, `:93`–`:108`). TH, RAD and GAP identities (owner decision 15), identities whose acceptance is a sequence task alone (EL-240, EL-241) and slice-owned identities with no S owner (EL-240: "Owner: slice `named-el-240`") cannot bind. Separately, owner decisions 15 and 24b allow one identity to own declared companion packages (toast slice, sealed can, toy fish, glow fish, balloon cargo) that are not placeable: no palette entry, appearing only with their parent, which declares a seat pose for each (coordinator decisions 33, 34). No declaration, construction or save form expresses this today.

**Approach:** Two slices of one story, in order. **S7a** widens main's binding reader to every identity family and acceptance form under one rule, and makes every refusal name its document. **S7b** adds the general companion capability: a typed placement in element metadata, seat poses declared by the parent, construction rules for placing, moving, deleting and restoring a seated companion, the seat in the save record, and Coverage admission of a companion under its parent's binding. No CAT number, ledger identity or palette order is minted for a companion.

## Boundaries & Constraints

**Always:**
- One admission rule per slice: a package is admitted when exactly one catalogue part or one binding section holds it (S7a), or when exactly one admitted parent declares a seat for it (S7b, companions only).
- Every refusal of the binding reader is an `InvalidDataException` whose message starts with the binding document's repository path, then a fixed reason; a refusal about one package or identity names it. Every refusal of a construction edit is a `WorkshopEditRejectedException` with a typed `WorkshopEditRejection`.
- Identity families are the closed set `RequirementOrigin.Element`, `Thermal`, `Radiation`, `Gap` (`tools/Coverage/RequirementDiscovery.cs:50`–`:55`); the identity prefix (`EL`, `TH`, `RAD`, `GAP`) and the ledger anchor prefix (`element`, `thermal`, `radiation`, `gap`) map through that enum and must agree.
- A companion is a part like any other in Run, Reset, save and load; its seat is a Build placement relation, not a joint.
- Tests run production code through defined interfaces: `NamedPackageSources.Parse`/`Read`, `CurrentCatalogue.Read`/`ValidateResources`, `CapabilityInputs.Prepare`; `BundleValidator`, `PartRegistry.Read`, `WorkshopConstruction.WithInstance`/`WithoutInstance`, `WorkshopSaveCodec`, and `WorkshopIntegrationHarness` Run/Reset.

**Never:**
- No new CAT number, `current-cat-NNN` anchor, `current-consumers.md` heading or `CapabilityCatalogueRequirements` row for a named identity; no edit to `docs/planning/requirements.md`.
- No ledger identity, palette order or lesson obligation for a companion; no companion that is also placeable.
- No fallback, alias, sentinel value, optional parameter keeping an old call shape, or second admission rule chosen by family; the `EL`-only form is replaced, not kept beside the widened one.
- No fixture package added under `elements/`; fixtures are in-memory text, in-memory bundle entries derived from delivered entries, or files in a temporary repository copy.
- No hash or byte-identity assertion, no skipped test, no mutants, no browser run.

## I/O & Edge-Case Matrix

**S7a — binding reader.** Spec files read: `docs/planning/elements/{EL,TH,RAD,GAP}-*.md` containing `## Package source binding`. The section keeps main's five lines; two change:
- **Named identity:** `[<P>-<N>](../invest/named-elements.md#<anchor>-<N>)` with `<P>` in `EL|TH|RAD|GAP`, `<N>` two or three digits as the ledger writes it, `<anchor>` the family's ledger prefix.
- **Acceptance:** either `[<lead>](../requirements.md#<lead>), [sequence-task-M](../requirements.md#sequence-task-M).` with `<lead>` a `todo-N` or the identity's own requirement anchor (`thermal-25`), co-anchored immediately before the task; or `[sequence-task-M](../requirements.md#sequence-task-M).` alone. The bound source is always the sequence task's record (as main).
- **Decision:** present exactly when the ledger entry names one `Owner **SNNN**`; absent when it names none.

| Row | Input / State | Expected | Error |
|---|---|---|---|
| A1 | TH, RAD or GAP spec, identity anchor lead, ledger S owner with a decision block | Admitted under that identity; source is the sequence task | none |
| A2 | Sequence-task-only acceptance; ledger entry's one requirements link is that task (EL-240 form) | Admitted | none |
| A3 | Ledger entry with no S owner and no `**Decision:**` line | Admitted with no decision reference | none |
| A4 | Identity prefix and ledger anchor prefix disagree (`TH-25` → `#element-25`) | Refused | `<path>: named identity and ledger anchor are different families.` |
| A5 | `**Decision:**` present, ledger names no S owner; or absent while it names one | Refused | `<path>: decision line differs from the ledger owner.` |
| A6 | Lone acceptance anchor that is not a sequence task | Refused | `<path>: acceptance without a lead must be a sequence task.` |
| A7 | Ledger entry's requirements link is neither the lead nor (with no lead) the task; or, with no link, the lead is not the identity's own anchor | Refused | `<path>: ledger acceptance differs from the binding.` |
| A8 | Each main refusal (17 rows of `NamedPackageRejectsMissingDuplicateAndUnknownSourceBindings`) | Same refusal | message now starts with the path; uniqueness refusals name both packages or identities |
| A9 | EL-239's real binding and every delivered CAT package | Admitted exactly as at baseline | none |
| A10 | A `*.md` outside the four families containing the marker | Not read | none |

**S7b — companions.** "Parent" is a package whose declaration lists companion seats; "seated" is a companion instance placed on a parent instance's seat.

| Row | Input / State | Expected | Error |
|---|---|---|---|
| C1 | Bundle: a `Companion`-placement entry named by exactly one parent seat | Admitted; no palette row; inventory and palette omit it | none |
| C2 | Companion named by no seat, by two seats, or a seat naming a `Palette` part or the parent itself | Bundle refused | typed `BundleErrorCode` naming both ids |
| C3 | Build: place a parent | Each declared companion placed seated at the parent pose composed with its seat pose, with declared default parameters | none |
| C4 | Move a seated companion | Refused, construction unchanged | `WorkshopEditRejection.SeatedCompanion` |
| C5 | Move the parent | Its seated companions keep their seat poses relative to it | none |
| C6 | Edit a seated companion's parameter | Admitted like any part | none |
| C7 | Delete a seated companion alone; then restore it from the parent | Delete admitted; restore re-places it seated with defaults | restore while present: `WorkshopEditRejection.CompanionPresent` |
| C8 | Delete the parent | Its seated companions are removed with it; other parts unchanged | none |
| C9 | A companion instance not seated on a parent instance (authored, loaded or proposed) | Refused | `WorkshopEditRejection.UnseatedCompanion` at construction, `WorkshopSaveRejectedException` at load |
| C10 | Authored inventory row naming a companion | Refused at catalogue load | typed refusal naming the level and part |
| C11 | Save and load: companion present with edited parameters; deleted; parent authored without it | Each restored exactly: presence, seat, parameters | none |
| C12 | Run moves a companion off its seat; Reset | Authored state restored: on the seat when present, absent when deleted | none |
| C13 | Coverage: companion package of an admitted parent | Admitted under the parent's binding; no source record of its own | an orphan companion package is refused naming its declaration path |

</intent-contract>

## Code Map

**State: blocked.** Baseline: captured at dispatch (coordinator decision 38, 23 November 2026): `baseline_revision` is the published main that contains the planning work, and every scope guard and `git diff` here compares against it; `25df3cd5` is the drafting revision only, not a baseline. Owner: the coordinator. Reason: S7 is tooling but not in owner decision 22's list, so it waits for the engine programme (orders 1–10); S7b also changes the abstractions, bundle and authoring schema, which that programme reshapes (`ENGINE-CAPABILITY-INSTANCES`). Retry condition: `ENGINE-SOA-SIMD` publishes. On that check S7a resumes at `draft`; the planning gate re-verifies every citation below against the published main and records the baseline then ([loop protocol, blocked recovery](../../_bmad/custom/bmad-loop-protocol.md#blocked-recovery-and-stagnation)). S7b resumes only after S7a publishes. `baseline_revision` above is the drafting revision (`25df3cd5`); every `path:line` is on it. S5 (generated wire accessors) lands before the programme, so S7b edits `schemas/workshop_authoring.fbs` only.

**Shape: one story, two slices (S7a then S7b).** S7b changes element metadata, the bundle schema and the authoring save schema, which the capability-probe split rule makes its own engine slice; S7a changes no shared contract and must not wait for it. Owner decision 24b and coordinator decision 33 define S7b; the old Never clause "no identity field in a package declaration" still holds (the placement is not an identity).

### What main does today

- `CurrentCatalogue.Read` (`tools/Coverage/CurrentCatalogue.cs:29`) reads `NamedPackageSources.Read` (`:42`); `ValidateResources` (`:232`) admits a declaration whose `Id` is a catalogue key or a named package (`:237`–`:239`) and otherwise refuses "has no current catalogue declaration" (`:251`–`:252`).
- `NamedPackageSources` reads only `EL-*.md` (`:19`), parses the binding by one expression requiring `EL-([0-9]{3})`, `element-\3`, `todo-N` and `**Decision:**` (`:55`–`:61`), checks the ledger heading `### element-NNN · EL-NNN · ` and exactly one `todo` link (`:93`–`:99`), one S owner (`:101`–`:108`), co-anchoring (`:110`–`:119`), and uniqueness of package, identity and task (`:41`–`:49`). Its messages name nothing.
- `CapabilityInputs.AttachNamedPackages` (`tools/Coverage/CapabilityInputs.cs:137`) attaches the declaration and resource closure to the task source; `RequirementDiscovery.Discover` (`tools/Coverage/RequirementDiscovery.cs:24`) substitutes the named source.
- Palette: `ElementMetadata.PaletteOrder` (`CuriousContraptions.Abstractions/ElementMetadata.cs:16`), unique and positive (`CuriousContraptions.Bundle/BundleValidator.cs:25`, `:473`); `PartRegistry.Read` orders by it (`engine/PartRegistry.cs:95`) and `WorkshopInventoryPolicy.Free` lists every part (`engine/PartRegistry.cs:67`; `engine/gpu/WorkshopInventory.cs:49`).
- Build edits: place `ui/WorkshopBenchPlacement.cs:29`, move `ui/WorkshopGpu.cs:100`, parameters `ui/WorkshopConfiguration.cs:307`, delete `ui/Workshop.cs:708`; all go through `WorkshopConstruction.WithInstance`/`WithoutInstance` (`engine/gpu/WorkshopConstruction.cs:11`, `:14`) and `PuzzleCatalogue.WithInstance` (`engine/gpu/PuzzleCatalogue.cs:55`). Typed edit refusals: `WorkshopEditRejection` (`engine/gpu/WorkshopEditRejectedException.cs:6`).
- Save: `WorkshopSaveCodec.Encode`/`Decode` (`engine/gpu/WorkshopSaveCodec.cs:19`, `:48`); `InstanceRecord` (`schemas/workshop_authoring.fbs:44`) has no seat.

### Capability probe (drafting, `25df3cd5`)

| # | Capability | Mark | Symbol | Exercising test |
|---|---|---|---|---|
| 1 | Named binding for `EL` identities with todo + task acceptance and an S decision | present | `NamedPackageSources.cs:51` | `CurrentCatalogueTests.NamedPackageUsesItsExistingTaskAcceptanceWithoutCatalogueAlias` |
| 2 | Refusal of each malformed or inconsistent binding | present (type only, no message) | `NamedPackageSources.cs:62`–`:130` | `CurrentCatalogueTests.NamedPackageRejectsMissingDuplicateAndUnknownSourceBindings` |
| 3 | Named closure attached to the task source | present | `CapabilityInputs.cs:137` | `CurrentCapabilityInputsTests.NamedPackageClosureIsRequiredWithoutGrantingCompletion` |
| 4 | TH, RAD, GAP identities; sequence-task-only acceptance; no S owner | **missing, S7a** | `NamedPackageSources.cs:19`, `:57`–`:60`, `:103` | none |
| 5 | Typed placement (palette or companion) | **missing, S7b** | `ElementMetadata.cs:16` | none |
| 6 | Parent-declared seat poses in the bundle | **missing, S7b** | `BundleServices.cs:16` | none |
| 7 | Seated relation in construction and save | **missing, S7b** | `WorkshopConstruction.cs:8`; `workshop_authoring.fbs:44` | none |
| 8 | Typed edit refusal | present | `WorkshopEditRejection` | `ClearPipeLevelTests` (`FixedPart` via `EditRefusal.Is`) |
| 9 | In-memory bundle for fixtures | present | `PartRegistry.Read(IBundleServices, string)` (`engine/PartRegistry.cs:81`); `BundleEntry` records | `TestBundle.Entries` (`CuriousContraptions.tests/BundlePortsTests.cs:23`) |
| 10 | Native Run/Reset and save round-trip harness | present | `WorkshopIntegrationHarness` (`CuriousContraptions.tests/WorkshopIntegrationHarness.cs:9`) | `BaseballTests` (Reset `:179`, save `:214`) |

Capability 7 changes the authoring save schema and capability 5 a shared declaration contract: split rule, so they are S7b, published after S7a. Capability 4 is a tool change inside S7a.

### Review tier

Both slices **engine tier** (all six layers first pass): S7a changes the `tools/Coverage` project and its tests; S7b changes abstractions, bundle, engine, schema, UI and tests, and has missing capabilities.

### Decisions (standing delegation; recorded once)

1. **One acceptance rule, three spellings (DC-66, DC-67).** The bound source is the sequence task; the lead, when present, is what the ledger links or the identity's own anchor. Reason: the ledger already uses all three forms (EL-239 todo, TH-25 own anchor `thermal-25` + `sequence-task-459`, EL-240 task only); one rule reads them.
2. **Decision line follows the ledger owner (DC-67).** Present iff the ledger names one S owner. Reason: a slice-owned identity has no decision to cite; requiring one would be invented text.
3. **EL-223's `**Ownership:**` draft (DC-67).** Not admitted; EL-223 is paused (owner decision 24g) and replans to main's `**Decision:**` form.
4. **Refusals name the document (DC-80).** Main's fixed reasons stay; each gains the binding path prefix, and uniqueness refusals name both holders. Reason: with up to 240 bindings a nameless refusal cannot be acted on.
5. **Placement is typed metadata (DC-69).** `ElementMetadata.PaletteOrder` is replaced by `ElementPlacement Placement`: `ElementPlacement.Palette(int order)` or `ElementPlacement.Companion` (kind enum `PlacementKind { Palette, Companion }`); every delivered declaration converts to `Palette(<its order>)` in S7b. Reason: a companion has no palette order (coordinator decision 36) and an unused int would be a sentinel.
6. **Seats belong to the parent (coordinator 33).** A parent implements `ICompanionSeatElement` with `CompanionSeats: IReadOnlyList<CompanionSeatDescriptor(string Companion, Vector3 Position, Quaternion Rotation)>`, carried on `BundleEntry`; the companion declares only `Placement.Companion`. Reason: one seat declaration per relation, cross-checked by the validator (C2).
7. **A level may place a parent without its companion: S7b, not S8 (DC-77).** It is the same construction state a player reaches by deleting the companion, so the construction and save rules that admit one admit the other; S8 owns only locking fixtures.
8. **No lesson obligation of its own (DC-79, coordinator reading of owner decision 24c).** A non-placeable companion is taught by its parent's lesson; Coverage makes no source record for it.
9. **45° bend (DC-70).** Placeable, so its own identity (coordinator decision 27) with its own binding when delivered; S7 adds nothing for it.
10. **In Run a companion is an ordinary body.** The seat sets its Build pose only; any physical attachment is the parent package's own declared joints. Reason: the toast slice and cargo must leave their seats in Run.

### Excluded, each with an owner (DC-83, DC-21)

- Identities delivered with no package (EL-120 to EL-125 goals; EL-038, EL-048, EL-197 connection kinds; closed duplicates such as EL-165): no binding section; owners `ENGINE-GOALS`, `ENGINE-CONNECTION-MATERIALS` and the closure reviews.
- A second identity on another's package (TH-05 on the delivered brake): no binding; the CAT spec is the single declaration (coordinator decisions 16, 27); owner TH-05's slice.
- Ledger S owners with no `scope-corrections.md` block (S326–S331, S588, S610–S612, S618, S619, S629, S630, S672, S673, S680–S682, Batch I's 22), todos that are scope indexes, entries with no or several requirement links, co-anchor and owner-anchor gaps (s402–s404, s422–s465, s472–s478, s531–s541), and the ball presets' missing s647/s661 blocks and un-co-anchored todo rows (DC-21): source-document repairs the binding identity's own slice makes before binding; the reader refuses them by name (A5, A7, main's anchor refusals). Owner: each identity's slice; the coordinator lists them at that slice's planning.
- One sequence task shared by two identities (EL-185, EL-186 on `todo-321`): the uniqueness refusal stays (one obligation, one holder); EL-185 is a connection, so only EL-186 binds. Owner: EL-186's slice.
- Two mode records (EL-022, EL-029): present (`CurrentCatalogue.ParseModes` takes a list of distinct dimensions).
- Fracture fragments (GAP-09 and others): appear only from their parent's break; their declaration and Coverage admission are `ENGINE-FRACTURE`'s.
- Locked lesson fixtures of any admitted package: S8 (`docs/planning/invest/delivery-batches.md` §5).

### Owned files

**S7a:**
- `tools/Coverage/NamedPackageSources.cs` — file set `:19`; identity, acceptance and decision grammar `:55`–`:61`; family mapping; ledger check `:93`–`:99` generalised to `### <anchor>-<N> · <P>-<N> · `; decision presence `:101`–`:108`; path-prefixed messages throughout.
- `tools/Coverage/CurrentCatalogue.cs:239`, `:251`–`:252` — messages name the colliding package and declaration.
- `tools/Coverage.Tests/CurrentCatalogueTests.cs` — fixtures `:225`–`:259`; new theory; added rows and message assertions in `:298`–`:345`; `NamedPackageUniquenessIsDiscriminatingForEachKey` (`:351`) asserts named messages.
- `tools/Coverage.Tests/CurrentCapabilityInputsTests.cs` — one new test beside `:233`.
- `tools/Coverage/README.md` ("Current input boundary", `:50`), `docs/planning/elements/README.md` (the binding section and its three acceptance spellings), `docs/delivery-workflow.md:67` (add `CurrentCapabilityInputsTests` to the registration filter, DC-88).
- `TODO.md`, `_bmad-output/implementation-artifacts/deferred-work.md` (only if a finding is deferred), this contract, `run-logs/spec-s7-coverage-named-identity-admission.md`.

**S7b:**
- `CuriousContraptions.Abstractions/ElementMetadata.cs` (placement), new `CuriousContraptions.Abstractions/CompanionSeats.cs` (`ElementPlacement`, `PlacementKind`, `ICompanionSeatElement`, `CompanionSeatDescriptor`).
- `CuriousContraptions.Abstractions/BundleServices.cs:16` (`BundleEntry.CompanionSeats`), `:92` (new `BundleErrorCode` values); `CuriousContraptions.Bundle/BundleValidator.cs:25`, `:473` and a companion check; the bundle generator's declaration read under `tools/BundleElements/`.
- Every `elements/*/*ElementDeclaration.cs` — `PaletteOrder` argument becomes `ElementPlacement.Palette(<same order>)`; no other change.
- `engine/PartRegistry.cs:67`, `:95` (palette lists `Palette` parts only); `engine/gpu/PuzzleCatalogue.cs:55` (place seats, refuse companion inventory rows); `engine/gpu/WorkshopConstruction.cs:11`, `:14` (move refusal, cascade delete, `WithCompanionRestored(GpuBodyId parent, int seat)`); `engine/gpu/WorkshopInstances.cs:62`; `engine/gpu/WorkshopEditRejectedException.cs:6` (`SeatedCompanion`, `CompanionPresent`, `UnseatedCompanion`).
- `schemas/workshop_authoring.fbs:44` (optional `seat: SeatRecord { parent: uint64; index: uint32; }`), `engine/gpu/WorkshopAuthoringFlatBuffers.cs:329`, `engine/gpu/WorkshopSaveCodec.cs:19`, `:48`. The generated accessors (`engine/gpu/WorkshopAuthoringGenerated.cs` today) are re-derived at planning after S5, which runs before the programme and makes them build output.
- `ui/WorkshopConfiguration.cs` — a "Restore <companion>" control on a selected parent with a missing companion, calling `WithCompanionRestored`; its browser proof is queued for the post-catalogue phase.
- `tools/Coverage/CurrentCatalogue.cs:203`, `:232` (read placement and seats from the bundle; admit companions under the parent); `tools/Coverage/CapabilityInputs.cs:137` (attach the companion's closure to the parent's source).
- Tests: new `CuriousContraptions.tests/CompanionSeatTests.cs`; `tools/BundleElements.Tests/BundleValidatorTests.cs`; `tools/Coverage.Tests/CurrentCatalogueTests.cs`.
- `TODO.md`, this contract, its run log.

No test-project edit: `CuriousContraptions.tests/CuriousContraptions.tests.csproj:77` compiles every top-level `*.cs` (S2, `25df3cd5`); `tools/Coverage.Tests` uses default globs.

### Carry-forward and concurrency

`deferred-work.md` has no open entry under `tools/Coverage` or the bundle placement path at drafting. Shared files: S4 (`docs/delivery-workflow.md:67`, `CurrentCatalogueTests`), S8 (Coverage fixture table), every element slice touching `elements/*/*ElementDeclaration.cs` (S7b's metadata conversion). Publication is serialised; the later publisher merges main once and repeats the full round.

## Tasks & Acceptance

**S7a execution:**
1. `CurrentCatalogueTests.cs` — write the S7a tests of the table first (red on baseline for A1–A8).
2. `NamedPackageSources.cs` — read the four family file patterns; map prefixes through `RequirementOrigin`; parse the two acceptance spellings; check the ledger heading for the family; require the decision line iff one S owner; prefix every message with the path and name the holders in `ValidateUnique`. Keep each member within E9 limits.
3. `CurrentCatalogue.cs` — name the colliding package and declaration at `:239`, `:251`–`:252`.
4. `CurrentCapabilityInputsTests.cs` — add A-T4.
5. READMEs and `docs/delivery-workflow.md:67` as listed.

**S7b execution:**
1. `CompanionSeatTests.cs`, `BundleValidatorTests.cs`, Coverage tests — write first.
2. Abstractions and bundle: placement, seats, validator codes; convert every delivered declaration's order.
3. Registry and inventory: palette from `Palette` parts; companion inventory rows refused.
4. Construction: seat placement on parent placement, move refusal, parent move carries seats, cascade delete, restore, unseated refusal.
5. Save schema and codec: optional seat record, round trip and refusal of an unseated companion.
6. UI restore control.
7. Coverage: companion admission under the parent; closure attached to the parent's source.

**Acceptance Criteria:**
- AC-a1: Given a TH, RAD or GAP binding, a sequence-task-only binding and a binding with no S owner, when `NamedPackageSources.Parse` runs, then each is admitted under its identity with the sequence task as source.
- AC-a2: Given each malformed or inconsistent binding (A4–A8), when parsed, then it is refused with a message that starts with the document path and names the package or identity involved.
- AC-a3: Given the real repository, when `CurrentCatalogue.Read` and `CapabilityInputs.Prepare` run, then EL-239 and every CAT package are admitted as at baseline and the pinned catalogue, mode, fixture and source counts are unchanged.
- AC-a4: Given a repository copy with a TH package, its binding and its decision block, when `Read` and `Prepare` run, then it is admitted with its closure on its task source; without the decision block it is refused naming the path.
- AC-b1: Given two in-memory parents (one seat; two seats at different poses), when each is placed through `WorkshopConstruction.WithInstance`, then every companion appears seated at the composed pose with default parameters, and none is in the palette.
- AC-b2: Given C4–C10, when each edit or load is proposed, then the matrix outcome holds and a refused edit leaves the construction unchanged.
- AC-b3: Given C11 and C12, when saved, loaded, Run and Reset through the production codec and `WorkshopIntegrationHarness`, then presence, seat and parameters are restored exactly.
- AC-b4: Given C13, when Coverage reads a bundle with a parent and its companion, then the companion is admitted under the parent and an orphan is refused by path.
- AC-n: Given each candidate, when the full local round runs, then zero failed, `Skipped: 0`, and the total is the current full local round + 12 (S7a) or + 22 (S7b).

## Spec Change Log

- 23 November 2026: re-based to main `25df3cd5`: the ledger `**Package:**` design is withdrawn for main's spec-section binding; scope widened by owner decisions 15, 24b and coordinator 33–34; split into S7a and S7b; tasks adding S7/S8 to `delivery-batches.md` deleted (they exist at §5); status follows owner decision 22.

## Review Triage Log

## Design Notes

1. **Element-free.** Neither slice names an identity, family member or package in production code; families are the closed `RequirementOrigin` set, companions a typed placement.
2. **Forward-only.** Deleted: the `EL`-only file pattern and grammar, nameless refusal messages, `ElementMetadata.PaletteOrder` as a bare int. Nothing is kept beside them.
3. **Planning audit.** `audit-s7-2.md` passed the withdrawn ledger-line design; a fresh planning audit of each slice is required at its planning gate (DC-84).

## Verification

Executor: the implementation owner of each slice runs every command; the coordinator reads results. Environment and compiler-log builds: `docs/delivery-workflow.md#reproducible-local-verification`.

**Commands (each slice):**
- The workflow's compiler-log builds — zero warnings and errors.
- `dotnet run --file tools/anvil/VerifyPolicy.cs -- --maintainability --project-root "$PWD"` — both canary `PASS` lines, `Coupling dependencies:` summary, zero violations.
- The registration check (`docs/delivery-workflow.md#new-package-registration-check`), with `CurrentCapabilityInputsTests` added by S7a — zero failed, `Skipped: 0`.
- `dotnet test CuriousContraptions.slnx -c Debug -f net10.0 --no-build --no-restore --nologo -noAutoResponse --filter "FullyQualifiedName~CurrentCatalogueTests|FullyQualifiedName~CurrentCapabilityInputsTests"` (S7a; S7b adds `|FullyQualifiedName~CompanionSeatTests|FullyQualifiedName~BundleValidatorTests`) — the focused count at the slice's baseline + its delta, zero failed.
- `git add -N` on new files, then `git diff --check <baseline_revision>` — no output; new test files staged with `git add` before the hook.
- One full local round under `caffeinate -i` — current full local round + delta, zero failed, `Skipped: 0`.

### Acceptance-to-test table

Coverage expectations are exact identities, counts and message fragments. Construction poses compare by the shared end-point tolerance ±0.02 m and 0.5° (a seat off by more is visibly off its parent); parameters are authored values, compared exactly.

| Row | Test | Control | Boundary | Run |
|---|---|---|---|---|
| A1–A3, AC-a1 | `CurrentCatalogueTests.NamedPackageAdmitsEveryIdentityFamilyAndAcceptanceForm` (theory: `Thermal`, `Radiation`, `Gap`, `SequenceTaskOnly`, `NoDecisionOwner`) | the existing EL-239 fact | two-digit identities (`TH-05`, `GAP-09`); last entry at end of file | planned |
| A4–A7, AC-a2 | `NamedPackageRejectsMissingDuplicateAndUnknownSourceBindings` new rows `FamilyMismatch`, `DecisionWithoutOwner`, `OwnerWithoutDecision`, `LoneLeadNotTask`, `LedgerLinkNotLead`, `OwnAnchorNotIdentity` | A1 fixtures accepted | `OwnAnchorNotIdentity` binds `thermal-26` for `TH-25` | planned |
| A8 | the same theory's 17 existing rows and `NamedPackageUniquenessIsDiscriminatingForEachKey`, asserting path and named holders | `None` row admitted | — | planned |
| A9, A10, AC-a3 | `ActualCurrentContractRetainsAllIdentitiesModesAndFixtures`, `CurrentNamedDecisionCannotDetachItsAcceptanceAndFreshness`, `CurrentPreparationPreservesObligationsAndReportsAllUnresolvedRoles` (existing pins) | the real tree admits EL-239 | a marker in a non-family file is ignored (in-memory case inside the new theory's fixture set) | planned |
| AC-a4 | A-T4 `CurrentCapabilityInputsTests.ThermalNamedPackageIsAdmittedWithItsClosureInACopiedRepository` | the same copy without its `s588` block is refused naming the path | uses real `thermal-25`/`sequence-task-459` anchors | planned |
| C1, C2 | `BundleValidatorTests.CompanionPlacementIsAdmittedOnlyWithOneParentSeat` (theory: `OneSeat`, `NoSeat`, `TwoSeats`, `SeatOnPalettePart`, `SeatOnSelf`, `OrderOnCompanion`) | `OneSeat` admitted | `OrderOnCompanion` | planned |
| C3, AC-b1 | `CompanionSeatTests.PlacingAParentSeatsEachCompanion` (theory: one seat; two seats at different poses) | an unrelated part placed alone adds nothing | seat at the parent origin and a rotated seat | planned |
| C4–C6 | `CompanionSeatTests.SeatedCompanionEdits` (theory: `MoveRefused`, `ParentMoveCarries`, `ParameterEdit`) | `ParameterEdit` admitted | a move of zero distance is not a move | planned |
| C7, C8 | `CompanionSeatTests.DeleteRestoreAndCascade` | deleting an unrelated part leaves companions | restore while present refused `CompanionPresent` | planned |
| C9, C10 | `CompanionSeatTests.UnseatedCompanionAndCompanionInventoryAreRefused` (theory: construction, load, inventory row) | the seated form admitted | — | planned |
| C11, AC-b3 | `CompanionSeatTests.SaveLoadKeepsPresenceSeatAndParameters` (theory: `Present`, `Deleted`, `ParentWithoutCompanion`) | — | edited parameter at its declared maximum | planned |
| C12, AC-b3 | `CompanionSeatTests.ResetRestoresAuthoredCompanionState` (theory: `Present`, `Deleted`) via `WorkshopIntegrationHarness` | the companion leaves its seat during Run | — | planned |
| C13, AC-b4 | `CurrentCatalogueTests.CompanionPackageIsAdmittedUnderItsParent` (theory: `Seated`, `Orphan`) | `Seated` admitted | — | planned |

New cases: S7a 5 + 6 + 1 = 12 (existing rows strengthened, none removed); S7b 6 + 2 + 3 + 1 + 3 + 3 + 2 + 2 = 22.
