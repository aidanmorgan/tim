---
title: 'Derive the remaining per-package test lists from declarations (S3)'
type: 'refactor'
created: '2026-11-21'
status: 'draft'
baseline_revision: 'captured at dispatch (the main that contains the planning work)'
review_loop_iteration: 0
followup_review_recommended: false
context:
  - '{project-root}/docs/testing-guidance.md'
  - '{project-root}/docs/delivery-workflow.md'
  - '{project-root}/docs/planning/invest/delivery-batches.md'
warnings: []
deferred: []
---

<intent-contract>

## Intent

**Problem:** After the registration-derivation slice, a new element package still has to be typed by hand into five test-side lists: the trim-analysis project list, two placement switches, three family name pins, the registry identity list and the native palette row list. Each is membership only, and each fails the first full round when forgotten.

**Approach:** Derive each of those lists from the discovered packages or the repository file list, keep every independent behavioural expectation, and prove with executed controls that an unfamiliar package is covered and an undeclared one is refused. The solution file keeps its one explicit line per package (this slice and after: owner decision O1).

## Boundaries & Constraints

**Always:** Tests keep running the real production compiler, registry and Workshop scene. Every assertion that compares a declared value with a compiled or displayed value stays. The static gate keeps requiring the trim analyzer in every trimmable project and keeps refusing an unpinned analyzer. The repository file list stays the one source for project discovery in the static gate; it is the source `TheSolutionListsEveryCSharpProject` already uses, so the slice adds no new difference between the working tree and the pre-commit index snapshot. `CuriousContraptions.slnx`, its two guards (`CSharpStaticGateTests.TheSolutionListsEveryCSharpProject`, `SolutionMembershipTests.TheSolutionContainsEveryPackageTheDomainAbstractionsAndTheGenerator`) and its consumer `tools/E2eRunner/BuildImpact.cs` are unchanged. Follow the handoff order, Anvil before each write, one retained implementer, the engine review tier, and one full local round with zero failures and `Skipped: 0`.

**Never:** A new registry, generator, schema field or declaration member. A placement declaration that no package needs. Removing element projects from the solution. Editing the per-package literal parameter table (`Parameters(id)`) or the Windmill inertia branch in `PackageRuntimeParityTests`. Any production, tool, hook or CI change. A test that only asserts a file contains a string. Output hashes, mutants, browser runs, skipped tests, a fallback placement for an unknown identity.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| M1 package project found by path | A repository file `elements/<Name>/<Any>.csproj`, named nowhere else | The static gate treats it as a trimmable project: the trim analyzer must be on in its compile and the ILLink analyzers are admitted for it | A compile of it without `EnableTrimAnalyzer = true` is reported, naming the project and `must be true here` |
| M2 a path that only resembles one | `elements/<Any>.csproj`, `elements/<Name>/<Sub>/<Any>.csproj`, `tools/elements/<Name>/<Any>.csproj`, `elements/<Name>/<Any>.cs` | Not a package project; no trim requirement follows from it | No error expected |
| M3 default placement | Any declared package, including an unfamiliar one installed through the scoped declaration seam | One generic instance at the shared pose with exactly its declared default parameters, which compiles | An undeclared identity is refused by the production declaration lookup; the tests supply no placement of their own |
| M4 slider placement with given values | A slider package and a value for each declared parameter | Every declared parameter of the placed instance holds its given value | A missing or out-of-range value is refused by production validation |
| M5 family parity loops | The payload, slider and hinge parity tests | Each iterates every discovered package of its family and compares at least one package | A family that matches no package fails the test |
| M6 registry and palette membership | The loaded registry and the native palette | Identities equal the discovered declarations; palette rows are the five category headings in display order, each followed by its packages in declared palette order | A missing, extra or misplaced row fails |
| M7 solution membership | A package project absent from `CuriousContraptions.slnx` | Unchanged: the existing guards fail | `TheSolutionListsEveryCSharpProject` and `TheSolutionContainsEveryPackageTheDomainAbstractionsAndTheGenerator` |

</intent-contract>

## Code Map

Delivery shape: single structural slice (no element, family or closure). Review tier: **engine**, because the owned diff changes the C# static gate and shared test builders, which are not on the data-only allow-list; the capability probe is below. Baseline: captured by the coordinator at dispatch as the published main that contains the planning work (coordinator decision 38, 23 November 2026; Running beside other work); every scope guard and `git diff` here compares against it. Drafted and line-checked on `25df3cd5`, which is the drafting revision only, not a baseline. `25df3cd5` holds the registration-derivation slice (`06ac9aa2`), S2 (no test-project edit: `CuriousContraptions.tests.csproj:77` compiles every top-level `*.cs`) and the Baseball (45 element packages). Line numbers were measured on `8d1d68aa` and re-read on `25df3cd5` on 23 November 2026; the moved ones are corrected below.

**Status: draft; runs before the engine programme** (owner decision 22: S3 is one of the structural stories allowed before it), after the planning work is published (coordinator decision 37; Running beside other work). The drafting Scoped Pass (auditor `aa26de8e9f5eea107`, 21 November 2026) has no report on disk (completion ledger DC-55) and is not carried over. Owner decisions O1 and O2 were answered on 21 November 2026.

Planning verdict: Scoped Pass (ready-for-dev) per the fourth audit (`docs/planning/elements/readiness/audits/audit-s3-4.md`, Incomplete with one blocking text fix); fixes applied 23 November 2026. The fourth audit found the third audit's B1–B4 and N1–N6 resolved and raised B-1 (frontmatter baseline) and N2–N4; B-1, N2 and N3 are applied here, and N4 (E14 in the engine stories index and the engine design gap) is applied in the planning documents. No further re-audit is needed. The third audit (`docs/planning/elements/readiness/audits/audit-s3-3.md`, Incomplete) found B1–B4 and N1–N6; all are applied here.

Owned scope:

- `CuriousContraptions.tests/CSharpStaticGateTests.cs` -- `TrimAnalyzedProjects()` at 670–705 (45 element entries at 681–704, the Baseball's at 701); consumers `TrimLinkedCompiles()` 612–613, `PackageAnalyzers` 558–566, `RequiredAnalyzerSwitches` 660–668, `GeneratedConfigViolations` 806–828; file source `Files` 199 and `RepositoryFiles()` 201–206.
- `CuriousContraptions.tests/PackageRuntimeParityTests.cs` -- `Placed(PartId)` 641–662; `Placed(PartId, Dictionary<string, float>)` 665–673; name pins at 149, 177 and 223; slider count pin at 400.
- `CuriousContraptions.tests/F32BehaviourIdentityTests.cs` -- line 39, a loop whose only effect was to trip the placement switch's `throw`.
- `CuriousContraptions.tests/BasketballResourceTests.cs` -- lines 34–35, an assertion against a literal list of all 45 identities.
- `CuriousContraptions.tests/WorkshopIntegrationHarnessTests.cs` -- the literal at 691–698 (asserted at 700), 5 headings and 45 identities.
- `docs/delivery-workflow.md` -- section "New-package registration check", its first paragraph (`:64` at `25df3cd5`, as S2 rewrote it).
- `docs/planning/invest/delivery-batches.md` -- section "S3. Remaining per-package lists".
- `_bmad-output/implementation-artifacts/deferred-work.md` -- the "S3:" item under "Structural stories queued — 21 November 2026", and the residuals named below.
- `TODO.md`, this contract and `_bmad-output/implementation-artifacts/run-logs/spec-s3-remaining-per-package-lists.md`.

Read, not changed: `engine/gpu/WorkshopConstruction.cs:31–39` (`WorkshopInput.Generic`), `engine/gpu/InstanceParameters.cs:34` (`Defaults`), `engine/gpu/DeclaredPhysics.cs:41,146`, `engine/gpu/ElementPortCatalogue.cs:89,320`, `CuriousContraptions.tests/BundlePortsTests.cs:19–27` (`TestBundle`), `CuriousContraptions.tests/PartRegistryTests.cs:17` (`PaletteRows`), `.githooks/pre-commit:36–53`, `tools/BundleElements/BundleElements.csproj:26`, `tools/BundleElements.Tests/SolutionMembershipTests.cs:32–38`, `tools/E2eRunner/BuildImpact.cs:9–33`, `CuriousContraptions.tests/EventRingProducerTests.cs:780` and `CuriousContraptions.tests/F32BehaviourIdentityTests.cs:71` (callers of `Placed`, signature unchanged).

### Capability probe

Performed while drafting on `8d1d68aa` and re-checked on `25df3cd5` (no cited symbol moved). Six present, none untested, narrower or missing. No solver, ABI, wire or presentation capability is involved.

- **Present.** Default parameters for an instance that names none: `WorkshopInput.Generic` (`engine/gpu/WorkshopConstruction.cs:31–39`, `parameters ?? InstanceParameterCollection.Defaults(partId)`). Exercised by `CatalogueIntegrationTests.EveryAdmittedDeclarationRetainsAuthoredStateAcrossRunResetSaveLoad`.
- **Present.** Refusal of an undeclared identity: `DeclaredPhysics.Of` (`engine/gpu/DeclaredPhysics.cs:146`, `InvalidOperationException`). Exercised by `BundleElementIdTests.ASaveNamingAnUnknownElementIsRefusedNamingIt` and `PartRegistryTests.Create_ThrowsArgumentException_OnUnknownOrUnregisteredPartId`.
- **Present.** Scoped installation of an unfamiliar declaration: `DeclaredPhysics.Scoped` (`engine/gpu/DeclaredPhysics.cs:41`) with `DeclaredPorts.Scoped` and `ElementPortCatalogue.Extend` (`engine/gpu/ElementPortCatalogue.cs:320,89`). Exercised by `BundleElementIdTests.AnUnfamiliarDeclarationPassesSaveAndWorkerAdmission`.
- **Present.** Discovered packages and decoded entries: `TestBundle.Packages`, `Entries`, `Entry` (`CuriousContraptions.tests/BundlePortsTests.cs:19–27`). Exercised by `BundlePortsTests.EveryDeclaredPortAndFootprintSurvivesBundleAdmission`.
- **Present.** Repository file list for both a git checkout and the hook's index snapshot: `RepositoryFiles()` (`CuriousContraptions.tests/CSharpStaticGateTests.cs:201–206`). Exercised by `TheSolutionListsEveryCSharpProject` and `EveryRepositoryCSharpFileIsCompiledByAProject`, which the hook runs on the snapshot (`.githooks/pre-commit:53`).
- **Present.** The generated-config check that a required analyzer switch is on: `GeneratedConfigViolations` (806–828). Exercised on real compiler logs by `EveryLoggedCompileReceivedThePinnedCompilerInputs`.

### Direct consumers

Found on `8d1d68aa` by searching every tracked file outside `elements/<Name>/` for all package identities and package directory names (`git grep -l` per name, counted per file) and by the non-package files of the Windmill slice (`git show --stat 47e01bdb`); counts re-read on `25df3cd5` with the Baseball's 45th package.

| File | Measured | In this slice |
|---|---|---|
| `CuriousContraptions.tests/CSharpStaticGateTests.cs:681–704` | 45 of 45 directories | derived |
| `CuriousContraptions.tests/PackageRuntimeParityTests.cs:641–673, 149, 177, 223, 400` | 45 of 45 identities | derived |
| `CuriousContraptions.tests/BasketballResourceTests.cs:34` | 45 of 45 identities | deleted, covered elsewhere |
| `CuriousContraptions.tests/WorkshopIntegrationHarnessTests.cs:691–698` (asserted at 700) | 45 of 45 identities | derived |
| `CuriousContraptions.slnx`, folder `/elements/` (`:15`), 45 project lines | 45 of 45 directories | unchanged, decision D1 (this slice and after: owner decision O1) |
| `CuriousContraptions.tests/PackageRuntimeParityTests.cs:468–638` (`Parameters(id)`) | 26 packages with parameters | unchanged, residual R1 |
| `CuriousContraptions.tests/PackageRuntimeParityTests.cs:291` | one identity | unchanged, residual R2 |
| `CuriousContraptions.tests/PackageRuntimeParityTests.cs:383–399` | 4 slider rows | rows kept, count pin removed |
| `CuriousContraptions.tests/CuriousContraptions.tests.csproj` | none: `:77` compiles every top-level `*.cs` (S2, published) | no edit |
| `CuriousContraptions.tests/VisibleSolidColliderTests.cs:63–162` (`Specs`) | 33 of 45 | S4; the Baseball's row is the Baseball repair's |
| `CuriousContraptions.tests/WorkshopSaveRejectionTests.cs:125–149` | 22 of 45, not extended since | S4 |
| `tools/Coverage/Capability*Requirements*.cs` | keyed by the 72 planned identities | no per-package edit; unchanged by the Windmill slice |
| `elements/elements.bundle.json` | generated by every `BundleElements` build, git-ignored | not a hand edit |

No TypeScript caller, schema, generated file, hook or tool names a package for registration. `tools/anvil/VerifyPolicy.cs` and `.githooks/pre-commit` name `CSharpStaticGateTests` only as a class filter.

**Evidence: the Baseball slice, published after registration derivation, still edited every list this slice derives or deletes** (`docs/planning/elements/readiness/audits/reconcile-main-fa86969f.md`, "Per-package lists the Baseball slice edited by hand"; lines on `25df3cd5`):

| File | Hand edit |
|---|---|
| `CuriousContraptions.tests/CSharpStaticGateTests.cs:701` | project path added to the trim list |
| `CuriousContraptions.tests/PackageRuntimeParityTests.cs:149` | `baseball` added to the payload name pin |
| `CuriousContraptions.tests/PackageRuntimeParityTests.cs:653` | `baseball` added to the generic placement switch |
| `CuriousContraptions.tests/BasketballResourceTests.cs:34` | `baseball` added to the literal registry list |
| `CuriousContraptions.tests/WorkshopIntegrationHarnessTests.cs:694` | `baseball` added to the palette rows |
| `CuriousContraptions.slnx:20` | project line (kept by decision D1) |

### Decisions

- **D1. This slice does not touch the solution file.** The `.slnx` format has no wildcard: on SDK 10.0.403 a `<Project Path="elements/*/*.csproj" />` entry is listed as a literal path and the build fails with MSB4014 (checked in a scratch folder). Removing package projects from the solution instead would change accepted architecture criterion A14 (`docs/architecture/element-package-isolation.md:178`), the Story 9A.4 guard in `SolutionMembershipTests`, `TheSolutionListsEveryCSharpProject` and the solution diff that `tools/E2eRunner/BuildImpact.cs:17–33` narrows selection on. A missing line already fails two guards before handoff.

Decided, with reason:

- **D2. Package projects are found in the repository file list, not through `TestBundle`.** It is the source the solution guard already uses and it needs no Godot fixture in the static gate. Commit path: an untracked, not-ignored `elements/X/X.csproj` is seen in the working tree and not in the hook snapshot (`git checkout-index --all`, `.githooks/pre-commit:23`). An ignored one is seen by neither. An intent-to-add one is listed by `git ls-files --cached`. This is the same file source `TheSolutionListsEveryCSharpProject` already uses, so the slice adds no new divergence.
- **D3. No placement declaration is added.** Every arm of the first switch builds the same generic instance at the same pose with the declared defaults (`PlacedParts.Battery`, `Plate`, `Counter`, `Clock`, `Latch`, `Domino` and `Springboard` pass exactly the declared defaults), so no package needs special placement today. In the second switch the `powered_gate` arm discards the given values, which is harmless only because the Powered gate declares no parameters (`PoweredGateElementDeclaration.cs:32`); M4 is therefore no behaviour change for it.
- **D5. "Every trimmable project" in the intent means the gate's trim list:** the eleven fixed projects and every package project. The vendored `CuriousContraptions.FlatBuffers` (`IsTrimmable`, `CuriousContraptions.FlatBuffers.csproj:12`) stays outside it as today, as the vendored analyzer opt-out. The seven `PlacedParts` arms also hard-coded declared defaults (3600 J, 120 W, full and Enabled for the Battery; 0.5 kg for the Plate; 3 for the Counter; 1 s for the Clock). That pin survives in `Parameters(id)` through `EveryPackageDeclaresTheRuntimeParameterDefaultsAndBounds`.
- **D4. The four literal slider rows at 383–399 stay; the count pin at 400 goes.** The rows are authored expectations. The pin only forced a new slider to add a row to a shared file.

Owner decisions (21 November 2026):

- **O1.** Keep one guarded solution line per package. S3's outcome is amended to "Adding a package edits one guarded solution line and no other shared membership list". No second S3 slice follows.
- **O2.** The literal `Parameters(id)` table is deleted in a follow-up slice once the conformance suite (S4) is published. It is a queued item of its own; owner: the slice after S4. This slice does not touch it.

### What this removes, and what catches the failure afterwards

| Removed | It used to fail loudly when | Now caught by |
|---|---|---|
| 45 trim-list entries | a listed project compiled without the trim analyzer; an unlisted project carried the ILLink analyzers | The same two checks, for every package project found by path. Stronger: an unlisted package without `IsTrimmable` used to pass |
| `_ => throw` in both placement switches | a package had no placement arm | Every declared package is placed; an undeclared identity is refused by `DeclaredPhysics.Of` |
| Name pins at 149, 177, 223 | a package joined or left a family | Joint counts per package by `EveryPackageDeclaresTheCompiledFootprintCollidersAndMaterial`; payload membership: **residual** (R5 below); a non-empty check in each loop. The slider and hinge pins are safe to remove because `Footprint` is authored separately in each declaration: a package that silently stops implementing `ISliderElement` or `IHingeElement` compiles no joint and fails `EveryPackageDeclaresTheCompiledFootprintCollidersAndMaterial` (`PackageRuntimeParityTests.cs:84–100`). Residual: a package that changes its footprint and its family together passes, which is a deliberate change |
| Count pin at 400 | a new slider had no literal row here | **Residual:** nothing forces a literal joint row for a new slider. Its declared-against-compiled parity is still checked for every slider at 174–216, and its literals belong in its own tests |
| Literal identity list, `BasketballResourceTests.cs:34` | registry membership changed | `PartRegistryTests.Load_RegistersEveryBundleElementFromItsPackageInPaletteOrder` and `TheRegistryElementsAreTheLoadedBundlesDeliverableElements` |
| Literal palette rows | a row moved | The same assertion against rows derived from the declarations, with the five headings still literal. **Residual:** a deliberate change of a declared `PaletteOrder` or `Category` no longer needs a second edit here |
| Loop at `F32BehaviourIdentityTests.cs:39` | a package had no placement arm | Structurally true after M3; the theory at 66–78 runs every element |

Residuals left for later slices, recorded in `deferred-work.md` by task 7: **R1** `Parameters(id)` (owner decision O2: deleted by the queued follow-up after S4); **R2** the identity branch at `PackageRuntimeParityTests.cs:291`, an analytic inertia oracle needed only by a package whose declared mass model is not its first collider's primitive; **R3** a new slider package has no forced authored joint literals and authors them in its own tests (owner: S4); **R4** a deliberate change of a declared `PaletteOrder` or `Category` needs no second edit (accepted); **R5** a package whose declared `Payload` policy changes leaves the payload parity loop without failing it; `PartRegistryTests.cs:51–53` checks only that the declaration and its bundle entry agree. Its own capture tests and the visible declaration diff cover it (owner: S4, whose conformance suite runs per package).

## Tasks & Acceptance

**Execution:**

1. `CuriousContraptions.tests/CSharpStaticGateTests.cs` -- add `internal static bool IsElementProject(string path)` (true only for `elements/<one segment>/<one segment>.csproj`) and make the trim list `TrimAnalyzedProjects(IEnumerable<string> files)`: the eleven fixed projects at 672–680 followed by `files.Where(IsElementProject)` in ordinal order, the static uses passing `Files`; delete lines 681–704 -- M1, M2.
2. `CuriousContraptions.tests/CSharpStaticGateTests.cs` -- give `GeneratedConfigViolations` a parameter for the required-switch table, pass `RequiredAnalyzerSwitches` from its one caller at 792, and add the two tests named in the table -- an executed control on synthetic input.
3. `CuriousContraptions.tests/PackageRuntimeParityTests.cs` -- replace the body of `Placed(PartId)` with the single generic placement and the body of `Placed(PartId, Dictionary<string, float>)` with the single generic placement carrying every given value; delete both switches; add the two tests named in the table -- M3, M4.
4. `CuriousContraptions.tests/PackageRuntimeParityTests.cs` -- replace the three name pins with `Assert.NotEmpty` on the same sequence, and delete line 400 -- M5.
5. `CuriousContraptions.tests/F32BehaviourIdentityTests.cs` -- delete line 39 and rename the test `EveryCurrentLessonHasAnExecutedScenario` -- the loop pinned only the deleted switch.
6. `CuriousContraptions.tests/BasketballResourceTests.cs` -- delete only the assertion at 34–35; the registry declared at 32–33 stays because the same test creates parts from it at 60–61. `CuriousContraptions.tests/WorkshopIntegrationHarnessTests.cs` -- replace the literal at 691–698 (asserted at 700) with rows derived from `TestBundle.Packages`: for each `ElementCategory` that at least one discovered package declares, in ascending value, its literal heading (`Motion`, `Goals`, `Power`, `Control`, `Structure`), then that category's identities by `Metadata.PaletteOrder` -- M6. The five headings are string literals in the test, not `ToString()` of the enum. Production emits a heading only for a category that has parts (`ui/Workshop.cs:419`), and the Optics and Radiation categories may be added by their engine stories before their first package (coordinator decision 36). The derived rows equal today's literal (checked against `elements/elements.bundle.json` on `8d1d68aa`: 49 rows, equal; on `25df3cd5` the literal has 50 rows, the Baseball's included, and the implementer re-checks equality against the baseline's bundle).
7. `docs/delivery-workflow.md`, "New-package registration check", first paragraph -- merge into the paragraph S2 rewrote (it already says the test project needs no project edit and keeps the trim checks "where needed"): state what is now derived, that the solution line and its two guards remain, and that a new slider package authors its joint literals in its own tests. `docs/planning/invest/delivery-batches.md` S3 -- amend Outcome to "Adding a package edits one guarded solution line and no other shared membership list", amend Acceptance to name the lists this slice derives and the solution guards that remain, and mark S3 closed on publication. `_bmad-output/implementation-artifacts/deferred-work.md`, the "S3:" item under "Structural stories queued — 21 November 2026" -- mark S3 closed on publication and add, as its own queued item, "delete the literal `Parameters(id)` table and `EveryPackageDeclaresTheRuntimeParameterDefaultsAndBounds` after the conformance suite publishes" (owner: the slice after S4, evidence `PackageRuntimeParityTests.cs:105–127, 468–638`); add residuals R2 to R5, each with source, evidence and owner. `TODO.md` -- the slice's outcome.

**Acceptance Criteria:**

- AC1: Given the candidate, when `git grep -n -E '"elements/[A-Za-z0-9]+/[A-Za-z0-9]+\.csproj"' -- CuriousContraptions.tests/CSharpStaticGateTests.cs` runs, then the only matches are the synthetic paths of the two new tests.
- AC2: Given the five matching compiler logs of the candidate, when `CSharpStaticGateTests` runs in the working tree and when the unmodified pre-commit hook runs it on the index snapshot, then both pass, so all 45 package compiles satisfy the derived trim requirement on both file sources.
- AC3: Given the candidate, when the full local round runs, then every project reports zero failed and `Skipped: 0`, and the totals are the current full local round + 8, the current registration check + 8 and the hook's current selection + 6, each re-measured at the slice's baseline before the first edit (main's full round at drafting: 7,294). No test is deleted; one is renamed.
- AC4: Given the candidate diff, when its paths are listed, then no path lies outside `CuriousContraptions.tests/`, `docs/`, `_bmad-output/` and `TODO.md`.

## Spec Change Log

## Review Triage Log

## Design Notes

1. **Behaviour and scope.** Membership of five test-side lists follows discovery; expectations about values do not change. Source: `docs/planning/invest/delivery-batches.md` S3.
2. **Reusable primitive.** None new. Reuses `RepositoryFiles()`, `TestBundle.Packages` and `WorkshopInput.Generic`.
3. **Declared composition.** A package project is identified by its path under `elements/`, the same rule as the generator's glob (`tools/BundleElements/BundleElements.csproj:26`); placement reads only declared defaults.
4. **Execution and ownership.** Test and guard code only. The scoped declaration seam restores the previous catalogue on dispose; the tests that use it run in the non-parallel native collection.
5. **Generality.** The controls use an unfamiliar identity and a path that no list names, and near-miss paths, so a copied list cannot satisfy them.
6. **Forward-only.** Deletes the 45 trim entries, both placement switches, three name pins, the count pin, the identity list, the palette literal and one vacuous loop. No alias, default placement or second path replaces them.
7. **Prerequisites.** The published planning work (coordinator decision 37); no capability prerequisite; runs before the engine programme (owner decision 22). Engineering rules E1, E7, E8 and E9 apply; E2–E6 and D1–D7 are untouched because no production code changes.

## Verification

Executor for every command: the implementation owner. Raw output goes under `.tools/s3-evidence/`, compiler logs where the capture puts them; each result is one line here citing its run-log entry. A change to any test source, project file or the toolchain invalidates the logs and the test binaries.

Environment: the pinned toolchain first on `PATH`: `PATH=$HOME/.local/share/tim-toolchain/dotnet-10.0.403:$HOME/.local/share/tim-toolchain/node-v26.12.1-darwin-arm64/bin:$PATH` and `DOTNET_ROOT=$HOME/.local/share/tim-toolchain/dotnet-10.0.403` (on this host the default `PATH` has Homebrew node v26.13.1 and a dotnet without `wasm-tools`, so the capture's node check and the web build fail otherwise); record the session environment check in the run log.

### Acceptance-to-test table

| Rows | Named discriminating test | Control and boundary | Run |
|---|---|---|---|
| M1, M2 | `CSharpStaticGateTests.ElementPackageProjectsAreRecognisedByPathAlone` (new theory, 5 cases: `elements/Unfamiliar/Unfamiliar.csproj` true; `elements/Unfamiliar.csproj`, `elements/Unfamiliar/nested/Other.csproj`, `tools/elements/Unfamiliar/Unfamiliar.csproj`, `elements/Unfamiliar/Unfamiliar.cs` false) | The four near-miss paths are the controls; one segment more and one segment fewer are the boundaries | planned |
| M1 | `CSharpStaticGateTests.AnElementProjectCompiledWithoutTheTrimAnalyzerIsReported` (new fact): a generated config written to a temporary folder without the switch, checked for `elements/Unfamiliar/Unfamiliar.csproj` against the required-switch table built, by the same derivation the static uses take, from `TrimAnalyzedProjects(["elements/Unfamiliar/Unfamiliar.csproj", "tools/Preview/Preview.csproj"])`, so the one fact proves the derivation and the check together, reports `build_property.EnableTrimAnalyzer =  (must be true here)` | The same config with `build_property.EnableTrimAnalyzer = true` reports nothing; the config without it checked for `tools/Preview/Preview.csproj` reports nothing | planned |
| M1, AC2 | `CSharpStaticGateTests.EveryLoggedCompileReceivedThePinnedCompilerInputs` (existing), on the candidate's logs | Existing rule controls in that class; the hook run is the second file source | planned |
| M3 | `PackageRuntimeParityTests.AnUnfamiliarDeclaredPackageIsPlacedFromItsDeclarationAlone` (new fact): the `ramp` entry re-identified as `unfamiliar_placed` and installed through the scoped seams is placed with `length` 3 and `width` 1.3 and compiles one owner body with the entry's collider count | `Placed(new("unregistered_payload"))` throws `InvalidOperationException`; the zero-parameter boundary is every existing package loop. The cited capability test re-identifies the zero-parameter `ball`; this one re-identifies `ramp`, whose colliders are parameter-bound (`RampElementDeclaration.cs:42–49`). If the compiler refuses the re-identified `ramp` under the scoped seams, that is a capability discovery returned to planning, not an in-slice repair | planned |
| M4 | `PackageRuntimeParityTests.AnUnfamiliarSliderPackageIsPlacedWithEveryDeclaredParameterBound` (new fact): the `spring` entry re-identified and placed with each parameter at its declared maximum holds those three values | Controls: the same call for an identity without a declaration throws `InvalidOperationException` (`DeclaredPhysics.Of`, reached through `InstanceParameterCollection.Validate(PartId)`); the same call with one declared key omitted throws `ArgumentException` ("Missing or unknown declared parameters", `engine/gpu/InstanceParameters.cs:41`). The old Springboard arm threw `KeyNotFoundException` for an omitted key, so this pins a changed refusal. The same call with one value just above its declared maximum (`stiffness` 1200 → the next f32 above) throws `ArgumentException` ("Invalid declared parameter value.", `engine/gpu/InstanceParameters.cs:47`); it is an assertion inside the same fact (+0 to the counts). `EverySliderPackageDeclaresTheCompiledPrismaticJointAndMovingBody` (existing) is the bit-level check for all four sliders | planned |
| M5 | `EveryPayloadPackageCompilesItsDeclaredMaterial`, `EverySliderPackageDeclaresTheCompiledPrismaticJointAndMovingBody`, `EveryHingePackageDeclaresTheCompiledRevoluteJointAndCarriesItsMovingBody` (existing, each now with a non-empty check) | `EveryPackageDeclaresTheCompiledFootprintCollidersAndMaterial` (existing) fails on a joint count that differs from the declared footprint | planned |
| M6 | `WorkshopIntegrationHarnessTests.NativePaletteGroupsDeclaredCategoriesAndPreservesTheirDisplayOrder` (existing, derived expectation); `PartRegistryTests.Load_RegistersEveryBundleElementFromItsPackageInPaletteOrder` (existing) | `PartRegistryTests.PaletteOrderFollowsReorderedDeclarationData` and `AValidNewPackageIdentityNeedsNoEngineAdmissionChange` (existing) | planned |
| M7 | `CSharpStaticGateTests.TheSolutionListsEveryCSharpProject`; `SolutionMembershipTests.TheSolutionContainsEveryPackageTheDomainAbstractionsAndTheGenerator` (existing, unchanged) | Unchanged | planned |

No simulated float is compared: the two literals in M3 are declared defaults, exact by contract.

**Commands:**

- The compiler-proof capture in `docs/delivery-workflow.md#reproducible-local-verification`, up to and including the `verify-policy.binlog` build, with `CSHARP_STATIC_GATE_BINLOGS` exported -- expected: five logs, zero warnings and errors.
- `dotnet run --file tools/anvil/VerifyPolicy.cs -- --maintainability --project-root "$PWD"` -- expected: PASS, both canary `PASS` lines and the `Coupling dependencies:` summary, zero violations. Run once when the candidate compiles and again here.
- `dotnet run --file tools/anvil/VerifyPolicy.cs` with no arguments -- expected: its self-checks pass, unchanged (the slice touches neither tool).
- The registration check in `docs/delivery-workflow.md#new-package-registration-check` -- expected: the current registration check at the baseline + 8 passed, 0 failed, 0 skipped.
- `dotnet test CuriousContraptions.slnx -c Debug -f net10.0 --no-build --no-restore --nologo -noAutoResponse --filter "FullyQualifiedName~CSharpStaticGateTests|FullyQualifiedName~PackageRuntimeParityTests|FullyQualifiedName~F32BehaviourIdentityTests|FullyQualifiedName~BasketballResourceTests|FullyQualifiedName~WorkshopIntegrationHarnessTests|FullyQualifiedName~SolutionMembershipTests"` -- expected: 0 failed, 0 skipped; every test in the table appears in the result.
- The AC1 `git grep` -- expected: matches only inside the two new tests.
- `git add -N _bmad-output/implementation-artifacts/spec-s3-remaining-per-package-lists.md _bmad-output/implementation-artifacts/run-logs/spec-s3-remaining-per-package-lists.md`, then `git diff --check <baseline_revision>` -- expected: no output.
- One full local round under `caffeinate -i`, the last two lines of the capture block -- expected: the current full local round at the baseline + 8 passed in total (main's at drafting: 7,294), 0 failed, `Skipped: 0` for every project.
- At the candidate commit, the unmodified `.githooks/pre-commit` -- expected: its builds succeed and its selection reports its count at the baseline + 6 passed, 0 failed, 0 skipped.

**Manual checks (if no CLI):**

- Open `Placed(PartId)` and `Placed(PartId, Dictionary<string, float>)`: neither names a package identity.

### Running beside other work

Prerequisite (coordinator decision 37, 23 November 2026): the planning work of 22–23 November 2026 (`docs/planning/elements/readiness/`, including `owner-decisions-2026-11-22.md`; `delivery-batches.md` sections 0 and 5; `named-elements.md`, `requirements.md`, `vertical-delivery.md`, `sprint-status.yaml`, `deferred-work.md`) is published before this slice starts, and the coordinator re-captures the baseline as that revision. Task 7 then edits the S3 section as published there (it keeps the **When.** line).

Read-only drafting aside, this slice is serialised with any slice that touches the same files. An element slice in flight adds lines to every list this slice deletes; whichever publishes second merges main once, drops its additions to the deleted lists and repeats the full round. Before the engine programme the other slices that may run are visible-solid parts A, B, B2 and C, the Baseball repair, S5 with S5b straight after it, and S6 with S6b–S6g (owner decisions 22 and 24h); none adds a package. S3 and S5 publish one at a time (coordinator decision 37). Parts A to C and the Baseball repair edit `VisibleSolidColliderTests.cs`, `TODO.md`, the sprint status and `deferred-work.md`; the Baseball repair also edits `BaseballTests.cs` and records in `delivery-batches.md`, not the lists this slice derives. S2 is published, so its documents are cited here by section. `spec-family-ball-presets.md` adds packages after the programme, so it follows this slice. Shared files touched: `CuriousContraptions.tests/CSharpStaticGateTests.cs` (with S5, which edits its generated-source rule, vendored digest and exemptions; different members, same class), `docs/delivery-workflow.md` (with S5, which edits `:34`), `_bmad-output/implementation-artifacts/deferred-work.md`, `TODO.md` and `docs/planning/invest/delivery-batches.md` (with every slice allowed before the programme). One publisher at a time; the later publisher merges main once, re-reads every `CSharpStaticGateTests.cs` line cited here and repeats the full local round. Not touched: the test project file, `content/puzzles.json`, coverage and impact tables.
