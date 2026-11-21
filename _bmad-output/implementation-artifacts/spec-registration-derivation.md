---
title: Derive new-package registration from declarations
type: refactor
created: 2026-11-21
status: done
review_loop_iteration: 0
followup_review_recommended: false
baseline_revision: 4f193bb234322eb0490000ff4806d41bf0be1d36
context:
  - docs/testing-guidance.md
  - docs/delivery-workflow.md
  - docs/architecture/element-package-isolation.md
warnings: [oversized]
deferred: []
---

<intent-contract>

## Intent

**Problem:** Adding an element package requires hand edits to duplicated registry expectations, bundle identity/port lists, catalogue/palette membership, workbench capacity, coverage membership and E2eRunner impact registration. Those tables repeatedly fail the first full round.

**Approach:** Derive delivered membership from existing package declarations and the generated bundle, preserving independent behavioral checks and required planned catalogue identities. Use the smallest necessary tooling/test closure, without another registry or generator.

## Boundaries & Constraints

**Always:** Preserve current runtime behavior, typed identities and enum boundaries, all required catalogue/mode/fixture identities, independent semantic assertions and existing positive/control/boundary interaction scenarios. Preserve authored Reset/save/load and typed refusal coverage. Follow REQ-12/REQ-14, checkout-local engineering policy, Anvil before writes, one retained implementer, six independent review lenses, matching compiler evidence and full local solution round with zero failures/skips plus normal hooks. Root alone owns publication authorization.

**Never:** Bellows/gas or another element; solver/ABI/wire/presentation expansion; unrelated dirty work; tautological expectations; output hashes/byte identity; fallback/alias/migration; mutants, browser runs, local Release/web publish or CI wait. No production catalogue keyed by known package names.

## I/O & Edge-Case Matrix

| Row | Input / State | Expected Output / Behavior | Error Handling |
|---|---|---|---|
| M1 | Discovered declarations, including an unfamiliar admitted identity and reordered palette data | Exact declared membership/order reaches registry and palette, unlimited allowances retained | Unknown identity refuses; bad reload preserves prior registry |
| M2 | Declarations with zero/multiple ports and footprint values | Decoded bundle/runtime preserves exact declared port semantics and footprint | Duplicate/undefined/missing port inputs retain typed refusals |
| M3 | Each delivered identity and generic authored construction | Save/worker admission and generic lifecycle retain every admitted identity | Unknown saved identities refuse atomically |
| M4 | Bundle scene directory differs from part spelling, including unfamiliar package | Impact classification and actual selected suites/build-reference closure use declared identity | Invalid or ambiguous mapping visibly refuses/selects full impact under existing contract; no guessed identity |
| M5 | Current planned catalogue and delivered declaration resources | Delivered identity/path membership derives automatically, planned 72 catalogue/104 mode/304 fixture identities remain independent | Missing, duplicate, uncatalogued or unmatched resource refuses |

</intent-contract>

## Code Map

Checkout / policy: /Users/aidan/orca/workspaces/tim/tim-registration-derivation; baseline above immutable, clean branch aidanmorgan/tim-registration-derivation; no carried delta. Implementation owner: /root/implementation_owner, fresh collaboration.spawn_agent fork_turns=none; retained for all repairs. Entry/setup and Windmill publication receipt: [run log entry 1–2](run-logs/spec-registration-derivation.md).

Owned scope:
- CuriousContraptions.tests/PartRegistryTests.cs — declaration-derived expected palette, retain generic admission and resource lifecycle.
- CuriousContraptions.tests/BundleElementIdTests.cs — remove fixed delivered count/latest-name expectation, preserve decode semantics.
- CuriousContraptions.tests/BundlePortsTests.cs — reuse TestBundle.Packages/Entries; remove exhaustive Golden membership/port table, preserve independent admission/connection controls.
- CuriousContraptions.tests/WorkbenchCapacityTests.cs — remove registration-only Place switch and palette census, preserve specialized capacity geometry and assertions.
- CuriousContraptions.tests/CatalogueIntegrationTests.cs — replace duplicated interaction census with real generic admission/lifecycle coverage; preserve every independent interaction test.
- CuriousContraptions.tests/PalettePlacementTests.cs — actual palette expected order from declaration input.
- tools/Coverage.Tests/CurrentCatalogueTests.cs — derive admitted resource membership; retain planned identities/modes/fixtures and rejection controls.
- tools/E2eRunner/ImpactRules.cs, Selection.cs, BuildImpact.cs — one validated per-context directory→PartId projection of bundle scene paths, shared through selection/build closure; delete ElementPackages hand table.
- tools/E2eRunner.Tests/TimingDeclarationTests.cs, SelectionTests.cs, FastSelectionTests.cs — adapt callers and add independently expected unfamiliar/malformed/duplicate/build-reference cases.
- docs/delivery-workflow.md registration section — describe derived membership, retain scoped registration gate.
- TODO.md, _bmad-output/implementation-artifacts/deferred-work.md, this contract and run-logs/spec-registration-derivation.md — accurate outcome, terminal receipt and close note.

No planned production registry, bundle schema/generator, Coverage runtime, puzzle-content or physics changes. Add a necessary caller to this list before editing; gate findings expand only the justified repair closure.

### Capability probe

Fresh context-free scout /root/registration_scout (fork_turns=none) found no missing solver/ABI/wire/presentation capability.
- Present: PackageDiscovery.PackageNames/Discover/FromAssembly (tools/BundleElements/PackageDiscovery.cs:16,25,48), exercised by SolutionMembershipTests.EveryPackageIsDiscoveredByGlobWithNoHandList and BundleValidatorTests declaration-count refusal.
- Present: BundleGenerator.Generate plus TestBundle.Packages/Entries (BundlePortsTests.cs:19–25), exercised by BundleRoundTripTests.EveryPackageEqualsItsDeserialisedEntry.
- Present: registry order and unfamiliar identity admission via PartRegistryTests.Load_RegistersEveryBundleElementFromItsPackageInPaletteOrder/AValidNewPackageIdentityNeedsNoEngineAdmissionChange.
- Present: ElementPortCatalogue.FromBundle/Declare, exercised by BundlePortsTests.ThePortsTheRuntimeSeesAreTheBundleTextsPorts/ADuplicateSocketOrElementIsRefused/AnInvalidPortDeclarationIsRefusedWithItsTypedReason.
- Present: WorkshopInput.Generic and SaveFixture.Elements, exercised by BundleElementIdTests.EveryLoadedElementIsKnownToTheWorkerDecode and unknown-element controls.
- Present: CurrentCatalogue.Read/ValidateResources (tools/Coverage/CurrentCatalogue.cs:30–59,233–300), exercised by CatalogueResourceBindsByItsOwnIdNotItsFileNameAndAcceptsCrLf and resource rejection theory.
- Present: ImpactContext.BundleElements (Selection.cs:64–96), exercised through SelectionTests and TimingDeclarationTests.ElementPackagesMatchDirectoriesAndBundle.
- Missing, owned here: validated directory→PartId tooling projection from parsed ScenePath; small tooling capability, no engine prerequisite split.

Direct consumers are the owned list above; scout traced Resolve/PackagePart callers and BuildImpact reference comparisons. Default decisions: reuse existing declarations/bundle without generation/schema changes or global cached filesystem state; preserve planned identities separately from delivered membership. No open owner decisions. Prior Windmill close/merge note and registration deferred entry read (run log entry2); separate Bellows/gas capabilities excluded.

## Tasks & Acceptance

**Execution:**
1. Test files above — derive only registration membership from independent declaration inputs; preserve semantic expected values and real behavioral scenarios.
2. ImpactRules/Selection/BuildImpact and their tests — derive package mapping once per context, validate identity/path uniqueness and malformed inputs, thread through current callers; remove old table/API paths without shims.
3. CatalogueIntegrationTests — replace membership-only census with a direct per-declaration production admission/lifecycle assertion, preserving named scenarios.
4. Docs/status — record completed registration outcome, predecessor receipt, proof limits and unresolved close-note items.

**Acceptance Criteria:**
- AC1: Given an unfamiliar valid declared package, when normal bundle/registry/palette/save admission and registration checks execute, then membership follows its declaration without hand-editing any listed membership table.
- AC2: Given package directory and ID spellings that differ, when package source/project or solution references change, then selector classification and selected suites use the bundle identity, including connected suites; malformed/ambiguous input remains visibly conservative/refused.
- AC3: Given existing planned catalogue and semantic interaction tests, when membership derivation replaces the duplicated tables, then independent identity requirements, behavior, controls, boundaries and lifecycle assertions remain effective.

## Design Notes

Eight affected-design answers:
1. Outcome: registration-first tooling slice under REQ-12 and REQ-14; all existing gameplay preserved.
2. Reusable: existing discovery, bundle reader and per-context selector; new tooling projection accepts parsed bundle identities/scene paths and returns typed directory mapping or visible invalidity.
3. Declared: package Metadata.Id, ScenePath, PaletteOrder, ports and footprint remain source; no inferred name aliases.
4. Flow: package discovery→generated bundle→runtime admission; tests compare original declarations with decoded results; selector bundle parse→validated map→classification/build closure→selected suites.
5. Ownership: no physical state/property writer changes; each ImpactContext owns mapping, existing registry atomic rejection/lifecycle retained.
6. Generality: unfamiliar identity/directory mismatch, palette reordering, empty/multiple ports, malformed/duplicate mapping, unknown identity, changed references; literal expected selected suites and unchanged behavioral controls prevent copied oracles.
7. Delete: exhaustive registration-only arrays/Golden membership/counts, registration-only placement switch, static ImpactRules.ElementPackages and superseded no-context resolution calls. Keep independent physical/signal/geometry expected semantics and required catalogue identities.
8. Uncertainty: no required owner decision; implementation must return a discovered solver/ABI/wire/presentation gap to planning.

Engineering applicability: E1/E2/E4/E5/E7/E8/E9 apply to discovery/typed tool boundaries, refusal/lifecycle, table deletion and proof; E3/E6 unchanged (no worker communication or asset changes). D1–D5 protect explicit identities, declaration variation, validation and single representation; D6/D7 unchanged (no physical law or animation). Sources: checkout-local engineering contract and both engine guides, package-isolation target invariants1–2/7–10, testing guidance and delivery-workflow local-test-authority/new-package-registration-check. Browser/performance/release and remote production/artifact/deployment remain unverified.

## Implementation Notes

Planning readiness Scoped Pass by /root/planning_auditor (fork_turns=none), no findings; all READY FOR DEVELOPMENT criteria checked. Ready-for-dev accepted and native step-03 entered. Implementation owner /root/implementation_owner owns affected checks, E9, compiler captures, registration and full-round execution. No coordinator implementation. Capability probe confirmed before edits; no missing engine capability. Full owned diff inspected for needless branches, duplicate rules and churn: single context-owned mapping replaces the table, rules cached once, no schema/runtime behavior changes. E9's CC26 rule-construction finding repaired by extracting the package-source predicate; second gate passed. See run log entry5.

## Verification

Use pinned environment and exact reproducible commands in docs/delivery-workflow.md. Implementation executor runs E9 with both canaries and Coupling dependencies summary before normal five compiler captures; registration gate; affected tests/table; whitespace and simplification; one full round under caffeinate -i. Zero failures and Skipped:0 per project. Relevant source/generated inputs/dependency/configuration changes invalidate affected evidence; do not duplicate full round without candidate change. Normal hooks separately at the single candidate commit. Coordinator final E9/scope check and independent exact-SHA SnapshotApproval required; no push.

Executor /root/implementation_owner: E9, five Debug compiler captures, registration, affected checks and one full local round. Normal binary logs: /private/tmp/tim-registration-gate.a59eJa; raw command outputs under .tools/registration-derivation-evidence, with durable receipt text in run log entry5. Source/tests/generated inputs/dependencies/build configuration/compiler environment invalidate affected captures and test reuse; documentation receipts alone do not. E9 repair run passed both canaries, metrics0/coupling0 (contract4633/model4/intrusive0). Final repaired E9 and all five normal compiler captures passed (run log entry6–7). Registration/affected check passed730/0/0; full local round passed7246/0/0 (run log entry9).

### Acceptance-to-test table

Planned names may be adapted to existing precise test names with the same direct assertions; record executed names/run evidence before handoff.

| Rows | Named discriminating test | Control and boundary | Execution |
|---|---|---|---|
| M1, AC1 | Load_RegistersEveryBundleElementFromItsPackageInPaletteOrder; AValidNewPackageIdentityNeedsNoEngineAdmissionChange; PaletteOrderFollowsReorderedDeclarationData; FreeInventoryListsEveryPlayableKindUnlimitedInPaletteRowOrder | Unknown ID, reordered declarations, invalid atomic reload, empty bundle | Executed: registration730/0/0, run log entry7 |
| M2, AC1 | EveryDeclaredPortAndFootprintSurvivesBundleAdmission | Existing moved/reordered/removed-port checks; zero ports; duplicate/undefined typed refusals | Executed: registration730/0/0, run log entry7 |
| M3, AC1, AC3 | EveryLoadedElementIsKnownToTheWorkerDecode; AnUnfamiliarDeclarationPassesSaveAndWorkerAdmission; EveryAdmittedDeclarationRetainsAuthoredStateAcrossRunResetSaveLoad (new in CatalogueIntegrationTests) | Unknown ID refusal; empty construction; real run/reset and fresh load for each declaration | Executed: registration730/0/0, run log entry7 |
| M4, AC2 | PackageIdentityComesFromBundleSceneDirectory; AnUnfamiliarDeclaredPackageNeedsNoImpactTableEdit; MalformedPackagePathsSelectFullImpact; DuplicatePackageIdentitiesOrDirectoriesSelectFullImpact; PackageBuildReferencesUseDerivedIdentity | Directory/ID mismatch; unknown/unbundled directory; malformed/duplicate paths; added/removed references | Executed: registration730/0/0, run log entry7 |
| M5, AC3 | ActualPartIdentityTableDeclaresEveryCatalogueResource; ActualCurrentContractRetainsAllIdentitiesModesAndFixtures | CatalogueResourceRejectionNamesTheResourceAndIdentity; missing/duplicate/uncatalogued resource; 72/104/304 independent counts | Executed: registration730/0/0, run log entry7 |
| AC3 | Existing CatalogueIntegrationTests semantic scenarios and PalettePlacementTests; full solution7246/0/0 | Existing disconnected/wrong-domain/limits and ordinary lifecycle controls unchanged | Executed: registration730/0/0, run log entry7 |

## Spec Change Log

## Review Triage Log

### 2026-11-21 — Review pass1
- verdicts: 1 finding — high0, medium0, low1, false0, maybe-false0; six fresh independent lenses completed (run log entry10).
- [low] [patch] EA1: stale TODO verification status corrected by retained owner and independently closed; engineering re-review and coordinator final E9/scope Scoped Pass (run log entry11).
- Intent Alignment descriptive proof limits assessed without new acceptance obligations; independent behavioral scenarios/requirements retained (run log entry10).

## Auto Run Result

Done: implementation, registration730/0/0, full7246/0/0, six independent review lenses and EA1 re-review complete. Coordinator final E9 passed both canaries with zero metric/coupling violations; final scope/simplification Scoped Pass. See run log entry11–12 for precommit completion and close note. Exact-SHA approval uses the resulting commit and normal-hook evidence; no publication or remote qualification claim.
