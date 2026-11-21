---
title: 'EL-239 Baseball: measured material and mass puzzle distinction'
type: feature
created: '2026-11-21'
status: done
baseline_revision: 8d1d68aa262bb3f89ee9d073bbbaf3a83b9f9322
review_loop_iteration: 2
followup_review_recommended: false
context:
  - '{project-root}/AGENTS.md'
  - '{project-root}/docs/testing-guidance.md'
  - '{project-root}/docs/architecture/element-package-isolation.md'
  - '{project-root}/docs/gpu-f32-physics.md'
  - '{project-root}/DESIGN.md'
  - '{project-root}/_bmad/custom/project-engineering-contract.md'
  - '{project-root}/_bmad/custom/element-free-engine-design.md'
  - '{project-root}/_bmad/custom/element-free-engine-review.md'
warnings: [oversized]
deferred: []
---

<intent-contract>

## Intent

**Problem:** EL-239/todo-198 is conditional on a measured mass/bounce distinction that gives Baseball a useful puzzle role; S636 requires an identical construction comparison against every retained ball preset.
**Approach:** Prove a fixed medium-light, moderate-rebound sphere through native production simulation, then adopt one declarative Baseball package with recognisable seamed art only if the measured distinction passes. This is one element, not a family or a duplicate closure.

## Boundaries & Constraints

**Always:** Preserve todo-198, sequence-task-530, EL-239 and S636. Use real production f32 simulation, current package/bundle/wire/save interfaces, declared geometry/material/mass and typed identities. Set functional clearance and duration assertions independently before execution. Use mandatory Anvil validation before each write. Keep all existing ball identities and ordinary lifecycle. Native acceptance now; Chrome visual/interaction proof is pending post-catalogue.
**Never:** Add an engine law, solver branch, schema/ABI/shared presentation change, alias, fallback, compatibility path or runtime element callback. Do not implement EL-217/223/241, S2–S6, Bellows, campaign expansion or a shared test harness. No mutants, browser tests, local Release/publish, push or CI waiting. If a required capability is missing return to planning. If measured distinction fails, do not adopt an arbitrary cosmetic duplicate or fit test bounds to output.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|---|---|---|---|
| S636 drop comparison | Each retained preset and proposed Baseball in identical release construction | Measured first rebound/roll visibly differs from every retained preset at puzzle scale; record actual measurements and independently chosen clearance | Failed distinction returns to planning; not adoption |
| Mass puzzle | Matched-size, matched-arrival Baseball and controls transfer momentum to a payload | Mass causes an observable route/load decision; mass-controlled negative and miss controls exclude bounce/geometry as the sole cause | No impulse or scripted movement |
| Airflow receiver | Actual Baseball in enabled/disabled fan stream | Enabled flow produces paid lateral motion; disabled flow transfers no work or lateral velocity | Production receiver declaration, no forced motion |
| Passive/rest | Unpowered release, then settled body over a finite observation window | Plausible bounce, no free launch or sustained energy growth, stable resting body | No velocity snapping |
| Declaration/art | Authored sphere and seamed cream/red visual | Visible solid and contact sphere agree; sole committed-body pose binding, seams cosmetic | Malformed declaration receives typed refusal |
| Current boundaries | Fixed preset, unknown parameter/identity or nonfinite pose | Canonical fixed values and valid identity admitted; unsupported candidate refused atomically | Prior construction unchanged |
| Lifecycle | Run, Pause, Step, Resume, Reset, save/load and recreate | Real motion, pause stability, single Step, exact authored Reset, canonical decoded identity, repeatable gameplay outcome | No output hashes or byte identity |

</intent-contract>

## Code Map

Checkout: current isolated tim-cluster2-next. Original baseline and policy revision are the frontmatter SHA; no carried dirty work. Implementation owner: fresh Orca worker ctx_dc914d89faaf / term_53b363a1-bc17-483a-8147-c885602e8e2e, isolated task prompt without coordinator history; retained for fixes. Shape: single element EL-239; review tier: **engine**, conservatively because new project build configuration and TODO changes are outside the data-only allow-list. All six native layers on first pass.

### Capability probe

One read-only scout, Orca dispatch ctx_5743097ecf8c, receipt msg_1cbe564e3068; citations are source evidence, not an executed run. Run-log entry 1 records decisions and carry-forward.

| Capability | Classification and cited exercising proof |
|---|---|
| Sphere material, mass/inertia and restitution | Present: TennisBallElementDeclaration.SceneBodies/MaterialBindings/MassFrame; WorkshopPhysicsCompiler material/mass compilation; WorkshopConstraintSolver.RestitutionPass. TennisBallTests.IndependentMassAndRadiusCompileCorrectInertiaAndPersist, TennisReboundsAboveMatchedHeavierBallsAndSettlesOnTheBench, ConfiguredMassChangesMomentumTransferredToAnEqualRadiusPayload, ConfiguredRadiusChangesContactSpinAtFixedMass. |
| Fixed sphere and ambient loading | Present: elements/Ball/BallElementDeclaration.cs and elements/BowlingBall/BowlingBallElementDeclaration.cs. TennisBallTests drop/momentum cases exercise retained presets and production physics. |
| Evaluated committed-body pose binding | Present, untested in this probe: WorkshopPhysicalPresentation.Apply (engine/MachinePart.cs:316), PhysicalVisual (:196) and BallElementDeclaration.Presentation provide the binding, but the cited CanonicalBody tests do not exercise evaluated presentation. BaseballTests.CommittedPresentationFollowsMovingBodyAndReset must prove evaluated moving pose and authored Reset through the production presentation surface; engine tier retained. |
| Native lifecycle and canonical admission | Present: TennisBallTests.ConfiguredBuoyancyAndDragChangeMotionAndResetRestoresAuthoredState, EveryConfiguredFieldIsRequiredAtSaveAndWireDecode, InvalidConfigurationCannotEnterConstruction; CatalogueIntegrationTests.EveryAdmittedDeclarationRetainsAuthoredStateAcrossRunResetSaveLoad. |
| Derived package discovery/registry/palette/save/wire | Present: tools/BundleElements/PackageDiscovery.cs PackageNames/Discover; PartRegistryTests.Load_RegistersEveryBundleElementFromItsPackageInPaletteOrder, Create_InstantiatesAndConfiguresEachLoadedPart, AValidNewPackageIdentityNeedsNoEngineAdmissionChange; BundleElementIdTests.AnUnfamiliarDeclarationPassesSaveAndWorkerAdmission; PalettePlacementTests.EveryFreePalettePartCapturesSizesAndDecodesFromTheSaveAndTheWorkerWire. |

Direct consumers / owned files:
- elements/Baseball/BaseballElementDeclaration.cs, Baseball.csproj, baseball.tscn, icon.svg: package data, scene, art and Abstractions-only scaffold; generated UID/import files only if normal import needs them.
- CuriousContraptions.slnx: package entry; CuriousContraptions.tests/CuriousContraptions.tests.csproj: explicit test Compile.
- CuriousContraptions.tests/FanGuidanceTests.cs: VG1 extends the existing production airflow enabled/disabled matrix to actual Baseball, retaining its meaningful lateral-speed and paid-work contract.
- CuriousContraptions.tests/BaseballTests.cs: package-local native tests; no shared harness changes.
- CuriousContraptions.tests/CSharpStaticGateTests.cs: necessary existing trim project-list entry.
- CuriousContraptions.tests/WorkshopIntegrationHarnessTests.cs: NativePaletteGroupsDeclaredCategoriesAndPreservesTheirDisplayOrder adds Baseball to the retained Motion category expectation; the startup class is now explicitly selected in expanded registration.
- CuriousContraptions.tests/BasketballResourceTests.cs: retained numeric-material test's explicit identity membership row must include Baseball (registration check exposed this direct consumer).
- CuriousContraptions.tests/PackageRuntimeParityTests.cs: necessary generic Placed identity arm and EveryPayloadPackageCompilesItsDeclaredMaterial payload identity list (line149); no new parameter defaults or physics branches.
- elements/elements.bundle.json only if tracked normal generation changes it; do not edit generated data by hand.
- docs/planning/elements/EL-239-baseball.md and README.md: part contract/index; docs/planning/invest/named-elements.md EL-239 row and scope-corrections.md S636 outcome.
- _bmad-output/implementation-artifacts/sprint-status.yaml: owning row; TODO.md: current outcome/check.
- This spec and run-logs/spec-named-el-239.md; deferred-work.md: accurate carry-forward ownership and close note only.
- Registration predecessor run log: append exact published/approval receipt, without rewriting historical evidence.
- _bmad-output/implementation-artifacts/evidence-named-el-239/checks.txt: durable raw gate and test output.

Read-only/retest consumers: PackageDiscovery and BundleElements wildcard references already derive membership; PartRegistryTests, BundleElementIdTests, PalettePlacementTests, CatalogueIntegrationTests, WorkbenchCapacityTests and BundlePortsTests. E2eRunner impact derives from scene paths. Do not refactor S2 compile globs, S3 remaining lists or S4–S6.

### Approved coverage amendment

Independent planning Scoped Pass msg_4d53fb69bf0c and coordinator msg_ae284a67dca9 authorize this amendment. Registration proved the coverage reader narrower than the probe implied: CurrentCatalogue.Read/ValidateResources admits resources only against the 72 CAT contracts. This is a missing direct tooling consumer, not a solver/ABI/wire/presentation prerequisite. Engine review tier and original baseline/owner remain. sequence-task-530 has no exact proof-owner role: retain its PlannedMode and unresolved issue without manufacturing a ModeContract; exercise explicit resolved and unresolved controls.

**Canonical binding.** Keep EL-239 as the named identity, todo-198/sequence-task-530 as its existing requirement acceptance and S636 as its conditional decision. Add a small explicit package-binding section to EL-239-baseball.md: package identity baseball, named-ledger link element-239, exact requirements links todo-198 and sequence-task-530, decision link scope-corrections.md#s636, and the existing Mode records syntax Configuration=Fixed. This is source provenance, not a new CAT identity, delivery-status claim or second package registry. The generated bundle continues to own delivered package membership.

**One admission rule.** Parse those opt-in binding sections from existing EL specification files into typed named-package source records (RequirementId package, WorkOrderId named identity, SourceKey existing Task, SourcePath/anchor references, PlannedMode values). Validate each reference exactly once against the current documents: matching EL filename/title/ledger identity; ledger's todo link; co-anchored todo and sequence requirement; nonempty unique decision anchor; defined modes with one Configuration entry. Reject malformed, missing, duplicate or unknown sources, duplicate named identities/package keys and a key claimed by both CAT and named contracts. A named spec without a binding remains planning prose; an admitted bundle package without exactly one validated source binding still fails. No inference from display names, absent metadata, file order or obsolete format is allowed.

**Consumers and evidence.** CurrentCatalogue retains its 72 CAT Elements and unchanged fixture contracts, adds the typed named bindings, and feeds both source sets to the same resource validation. RequirementDiscovery enriches the already discovered Task source with its validated named-spec/ledger/decision provenance; it neither invents another obligation nor aliases EL to CAT. CapabilityInputs attaches the declared package/art/runtime closure and fixed configuration mode to that existing task source using its existing proof-owner resolution. Missing/ambiguous owners remain explicit unresolved issues and never become implementation eligibility or completion; semantic capability obligations remain unchanged. Source edits invalidate only the affected source's evidence, observed through the audit's stale/current result. The existing CAT acceptance, resource, mode, fixture and unknown-source refusals remain enforced.

Repair pass1 (VG1/VG2/EC1): retain the approved capability probe and baseline; no new engine capability. NamedPackageSources validates the ledger's single explicit S decision owner against the bound decision. Its production uniqueness validation is exercised with independently valid parsed sources that collide on exactly one key (including distinct task sources for the identity collision). CurrentCatalogueTests adds wrong-existing/missing/ambiguous decision-owner refusals and current-document S636 freshness controls. Repair evidence uses repair1-* filenames and appends to the existing durable checks file; original review-product.diff/review-full.diff remain untouched.

**Actual additional closure:** tools/Coverage/NamedPackageSources.cs (bounded typed source reader and shared anchor validation, automatically compiled by existing project glob); tools/Coverage/CurrentCatalogue.cs (compose source bindings before the existing admission check); tools/Coverage/RequirementDiscovery.cs (enrich the existing task provenance); tools/Coverage/CapabilityInputs.cs (attach named resource closure/mode without changing owner or capability policies); tools/Coverage.Tests/CurrentCatalogueTests.cs and CurrentCapabilityInputsTests.cs (positive/refusal/staleness tests and actual-tree fixture copies). Existing owned EL-239-baseball.md holds the binding; named-elements.md and scope-corrections.md remain canonical referenced sources, not duplicated acceptance. BaseballElementDeclaration.cs uses named metadata arguments required by the existing Id/ScenePath source readers. README corrects the statement that all 25 new identities lack specs; delivery-batches.md changes only Baseball's now-stale no-spec navigation phrase. No requirements.md/current-consumers.md acceptance rewrite, CAT alias, S2–S6 overhaul, generator, cache, policy weakening or new runtime path.

**Planned discriminating proof:** CurrentCatalogueTests.NamedPackageUsesItsExistingTaskAcceptanceWithoutCatalogueAlias (Baseball plus an unfamiliar EL fixture; preserves all CAT IDs/modes/fixtures); NamedPackageRejectsMissingDuplicateAndUnknownSourceBindings (typed scenario enum; missing/empty/duplicate anchors/documents, mismatched todo/task/EL links, unknown package/source, duplicate binding/CAT collision, malformed modes); NamedAcceptanceChangesInvalidateOnlyItsTaskProof (alter task, named declaration, ledger or decision independently, with unrelated CAT/task controls); CurrentCapabilityInputsTests.NamedPackageClosureIsRequiredWithoutGrantingCompletion (package/scene removal, source edits, copied-tree fixture and unresolved-owner control). Existing resource/bundle tests continue proving no orphan or duplicate declaration. The full registration command adds CurrentCapabilityInputsTests and WorkshopIntegrationStartupTests to its affected selection, then the unchanged full local-round requirement applies.

### Design answers and decisions

1. Outcome: one distinct Baseball material/puzzle choice satisfying todo-198/S636. Source requirements.md todo-198 and scope-corrections.md S636; native-first policy defers actual Chrome proof.
2. Reusable capability: existing dynamic sphere/contact/material/mass/ambient-load laws; fixed kg/metre/restitution inputs, simulation-owned pose/velocity output. No new capability.
3. Declaration: proposed mass 0.65 kg, radius 0.25 m, restitution 0.35, friction 0.3, drag 0.04, zero buoyancy; exact sphere inertia 2/5 mr². These are design defaults, not measured claims.
4. Execution: package→build bundle→current admission/compiler→solver→committed body pose→existing presentation binding. No element identity selects a physical law.
5. Ownership: simulation writes motion; package binds visible body; existing commands own Run/Reset and codecs own canonical construction save/load and atomic refusal.
6. Generality: retained ball comparisons, same-radius mass control, absent/off-axis impact, invalid admission and lifecycle use existing production harness. New tests prove new acceptance; existing tests establish reusable capabilities.
7. Deletion: no old Baseball runtime exists; remove the unimplemented EL-239 disposition only after proof. No superseded engine path and no legacy compatibility retained.
8. Uncertainty: measured adoption remains a testable implementation condition, not a new engine prerequisite. Failure returns to planning; a duplicate closure would require its own full behavioral proof.

Default reason: a 0.65 kg, moderate-bounce Baseball is visibly and physically between existing presets, with Tennis-sized geometry isolating material/mass comparisons. Cream/red seamed art follows DESIGN. These defaults do not waive proof; implementer freezes test clearances before observing results. No required-now owner decision remains open.

Other priority probes preserve full sources (requirements.md todo-398, todo-412 and sequence-task-351). EL-223 existing unpowered hinges, compound contacts and physical binding are present; same-ball cup retention/release is **present capabilities, untested composition**, not a missing law. EL-241 existing sliders/end contacts/memory may compose an escapement, but held/empty/blocked queue proof is untested. EL-217 residence does not expose a live chamber-empty electrical output and cannon does not expose physical returned interlock; a declaration-only alternative remains unproven. Next cluster-2 planning owner must test concrete compositions first; only a demonstrated missing solver/ABI/wire/presentation contract moves the identity to the relevant later cluster (assembly/interlock cluster 6), with a bounded engine owner and REQ-12 amendment. Do not implement that prerequisite here or claim these identities proven blocked.

Engineering applicability: E1/E2/E4/E6–E9 and D1–D7 apply to declaration, boundaries, visual/physical truth and evidence. E3 transport ownership and E5 storage unchanged; preserve existing contracts and forward-only rule without transport/storage work. E9 executor implementer at handoff, coordinator on final candidate.

## Tasks & Acceptance

**Execution:**
- Create package and tests at the owned paths; establish measured S636 and mass-role proof before recording adoption.
- Update only necessary registration closure list entries and current EL-239 records; retain S2–S6.
- Record exact commands/results, acceptance mapping, close note and explicit browser/remote limits in the run log.

**Acceptance Criteria:**
- Given all retained presets in identical construction, when dropped using production simulation, then Baseball has a measured visible bounce/roll distinction from each preset, with an independently specified meaningful clearance.
- Given matched geometry and arrival, when mass changes, then a payload route/load result demonstrates Baseball's mass role; no-impact and wrong-side/miss controls do not counterfeit it.
- Given the admitted Baseball, when exercised through native lifecycle and malformed-candidate boundaries, then authored state is restored exactly and invalid input leaves the installed construction unchanged.
- Given package artwork and collision data, when extracted and bound, then the visible sphere agrees with the physical sphere and seam art does not alter collisions.
- Given completed checks and independent review, when finalizing, then all required local tests/hook checks pass with zero failed/skipped, with exact-SHA approval before root publication.

## Implementation Notes

Review repair1: VG1/VG2/EC1 are implemented without changing the approved gameplay intent. Changed code is limited to FanGuidanceTests.cs, CurrentCatalogueTests.cs and NamedPackageSources.cs. Full owned scope and repair diff inspected before the post-review full round: no engine, ABI, package-data, source-acceptance or compatibility change; no speculative abstraction. repair1 compiler captures/E9/registration798/0/0/affected28/0/0 and the required final full round7294/0/0 pass. See run-log entries7–8 and the appended durable evidence for the changed-path list and raw results; independent pass2 cleared all findings; final scope/simplification PASS and coordinator E9 are recorded in run-log entry9. Original review-product.diff and review-full.diff remain untouched.

Planning Scoped Pass msg_d6572c2f246b authorizes implementation. Package, native tests, necessary registration-list edits and the approved named-source coverage consumer are implemented. Full owned diff inspected against the original baseline, including new files: declaration-only physics, one shared resource-admission rule and source provenance, no engine branch, compatibility path, speculative subsystem or unrelated change. The pre-review captures, E9, registration791/0/0 and full7285/0/0 are preserved in run-log entry6; repair1 supersedes affected proof with registration798/0/0 and full7294/0/0. Durable raw results and close notes are in run-log entries6–8; independent review and normal hooks remain coordinator-owned. Capability recheck found the declared sphere/material/mass/ambient-load, derived discovery, current admission and committed-root-pose paths present; no engine capability is added. DESIGN's older GLB wording yields to E6 and the sole .tscn authority; browser deferral remains explicit. Root serializes publication.

## Verification

Implementer owns matching compiler captures, E9, new-package registration checks, affected tests and one full local round at round end, in protocol order. Read docs/delivery-workflow.md local-test-authority and new-package-registration-check for exact commands; no excluded native tests. Node v26.12.1, SDK 10.0.403, wasm-tools 10.0.114/10.0.100 at /private/tmp/tim-dotnet-10.0.403 and /private/tmp/node-v26.12.1-darwin-arm64/bin are verified; restore and Auto configuration preflight passed at baseline.
Command executor: retained repair owner ctx_e5b79c398ee1 / term_3e13f954fc4f57e1e23c3d33d5fc3509 owns Debug/net10.0 compiler captures, E9, registration, affected tests and the handoff full round; coordinator owns final-candidate E9 and any required post-review round. Compiler logs resolve from the session TMPDIR as tim-local-gate.zBDYUZ. Commands/results are in run-log entries 2, 4, 5 and 6; [durable raw evidence](evidence-named-el-239/checks.txt) preserves the final checks and first-round failure. Repair1 captures and E9 pass; registration798/0/0 and full7294/0/0 cover the final repair candidate. Source/test/generated/dependency/configuration/compiler changes invalidate affected captures and tests; unchanged web/tool logs remain reusable after test-only repairs.

Store transient raw output in .tools/baseball-evidence/ and durable cited evidence under implementation-artifacts; compiler logs follow local verification contract. Changed source/generated/dependency/configuration inputs invalidate matching evidence. Normal hooks are separate. Coordinator runs final E9; a second full round only if candidate changes after handoff.

### Acceptance-to-test table

| Row | Named planned test | Control/boundary | Executed evidence |
|---|---|---|---|
| S636 | BaseballTests.IdenticalDropDistinguishesEveryRetainedPreset | Every retained default, identical construction, first rebound and resting route | Repair1 affected: 28 passed / 0 failed / 0 skipped, including all 7 Baseball tests; repair1-affected.txt |
| Mass role | BaseballTests.MatchedSizeImpactChangesPayloadRouteByMass | Same-radius mass control, no-impact, off-axis miss; independently specified route clearance | Repair1 affected: 28 passed / 0 failed / 0 skipped, including all 7 Baseball tests; repair1-affected.txt |
| Passive/rest | BaseballTests.UnpoweredDropSettlesWithoutFreeRebound | Resting no-input and release boundary; finite stable observation | Repair1 affected: 28 passed / 0 failed / 0 skipped, including all 7 Baseball tests; repair1-affected.txt |
| Art/solid | BaseballTests.SceneAndIconExposeSeamedSphereMatchingContactGeometry | No seam colliders, physical radius and pose binding | Repair1 affected: 28 passed / 0 failed / 0 skipped, including all 7 Baseball tests; repair1-affected.txt |
| Pose binding | BaseballTests.CommittedPresentationFollowsMovingBodyAndReset | Moving committed body versus paused body, then exact authored Reset of evaluated pose | Repair1 affected: 28 passed / 0 failed / 0 skipped, including all 7 Baseball tests; repair1-affected.txt |
| Admission | BaseballTests.FixedPresetCompilesCanonicalMassAndMaterialAndRejectsForeignParameters | Unknown parameter/identity, nonfinite pose and atomic preservation | Repair1 affected: 28 passed / 0 failed / 0 skipped, including all 7 Baseball tests; repair1-affected.txt |
| Lifecycle | BaseballTests.RunPauseStepResetSaveLoadRestoresAuthoredBaseball | Pause/Step/Resume, repeat Reset, decoded save/load, recreate/replay outcome | Repair1 affected: 28 passed / 0 failed / 0 skipped, including all 7 Baseball tests; repair1-affected.txt |
| VG1 airflow | FanGuidanceTests.ExistingPayloadsReceivePaidLateralGuidanceOnlyWithEnabledFlow (Baseball true/false) | Existing 0.1 m/s guidance threshold and positive paid work; disabled exact no-flow zero | repair1-affected.txt: 28 passed / 0 failed / 0 skipped |
| VG2 independent uniqueness | CurrentCatalogueTests.NamedPackageUniquenessIsDiscriminatingForEachKey | Independently valid sources; none/package/identity/task collisions isolated; Parse integration controls | repair1-registration.txt: Coverage83 passed / 0 failed / 0 skipped |
| EC1 decision association | CurrentCatalogueTests.NamedDecisionMustMatchExactlyOneLedgerOwner; CurrentNamedDecisionCannotDetachItsAcceptanceAndFreshness | Existing S638 mismatch, missing/ambiguous/repeated owner, actual S636 freshness and unrelated S638 control | repair1-registration.txt: Coverage83 passed / 0 failed / 0 skipped |
| Registration | Existing registration-check named suites plus package parity | All bundle members, unknown identity refusal | Repair1 expanded registration: 798 passed / 0 failed / 0 skipped |
| Native palette order | WorkshopIntegrationStartupTests.NativePaletteGroupsDeclaredCategoriesAndPreservesTheirDisplayOrder | Full category/order expectation includes Baseball; existing labels/icon controls retained | Repair1 expanded registration: game409 passed / 0 failed / 0 skipped |
| Named source binding | CurrentCatalogueTests.NamedPackageUsesItsExistingTaskAcceptanceWithoutCatalogueAlias | Baseball and unfamiliar EL; all CAT identities/modes/fixtures retained | Repair1 expanded registration: Coverage 83 passed / 0 failed / 0 skipped |
| Named source refusals | CurrentCatalogueTests.NamedPackageRejectsMissingDuplicateAndUnknownSourceBindings | Missing/duplicate/unknown source, mismatched links, CAT collision, malformed mode | Repair1 expanded registration: Coverage 83 passed / 0 failed / 0 skipped |
| Source freshness | CurrentCatalogueTests.NamedAcceptanceChangesInvalidateOnlyItsTaskProof | Task/spec/ledger/decision change with unrelated-source controls | Repair1 expanded registration: Coverage 83 passed / 0 failed / 0 skipped |
| Coverage closure | CurrentCapabilityInputsTests.NamedPackageClosureIsRequiredWithoutGrantingCompletion | Missing package/scene, copied tree, unresolved owner | Repair1 expanded registration: Coverage 83 passed / 0 failed / 0 skipped |

Numerical policy: name each quantity and gameplay failure, use existing quantity-specific tolerances where applicable, independently derive visible clearance before simulation, finite generous stall guards. No per-tick monotonicity, incidental arrival ticks, byte equality, hashes or tighter reruns without concrete defect.

## Spec Change Log

Draft amendment 1 (msg_70a2cbf0ea6c): bounded named-package coverage binding, exact consumers and refusal proof proposed in the Code Map; independent Scoped Pass msg_4d53fb69bf0c approved implementation via msg_ae284a67dca9. Preserve sequence-task-530's missing exact proof-owner role, retain PlannedMode without fabricating a ModeContract, and test resolved/unresolved controls. Observed failure and current passing evidence: run-log entry 3.

## Review Triage Log

Pass 2: VG1/VG2/EC1 independently cleared; Blind msg_c213b979554e, Edge msg_29faaea41092, Verification msg_0f2613f9e6d7, Forward msg_69aa9c5c8e72 and Engine msg_800626de9651 report no findings. Intent omitted under later-pass policy. Final scope/simplification PASS msg_49b1735e8a36 covers all32 owned paths; coordinator final E9 session4301 exit0 has both canaries PASS and metrics0/coupling0. The approver checks the resulting exact commit and normal-hook evidence; this record makes no SnapshotApproval or publication claim.

Pass 1 (engine tier, all six native layers): Blind msg_67d3b35d5b65, Edge msg_0bb4845ee923, Verification msg_1edcef531305 (duplicate delivery msg_f1e9d1d37395), Intent msg_62e7fbd30654, Forward msg_425ec7c46cee, Engine msg_1b35ffbd823e. No finding from Blind/Forward/Engine; Intent describes native-vs-browser and operational evidence limits, consistent with the declared enforcing stage. Three confirmed findings were routed as patches and independently cleared in pass2 below.

| Finding | Verdict | Route | Evidence / required repair |
|---|---|---|---|
| VG1 airflow coverage | medium | patch, test | Baseball declares airflow at BaseballElementDeclaration.cs:27 but tests exercise other payloads; add enabled/disabled production airflow proof for actual Baseball. |
| VG2 uniqueness guard masking | medium | patch, test | CurrentCatalogueTests.ParseDuplicateNamed reuses sequence-task-530 so either identity/package guard could disappear while tests still reject; isolate package, identity and task collisions with otherwise-valid sources. |
| EC1 unrelated decision accepted | medium | patch, product | NamedPackageSources.cs:62-69 validates chosen decision existence but not ledger association; an existing S638 binding bypasses Baseball S636 provenance. Validate exact declared decision association, with unrelated-existing/missing/ambiguous controls and correct-source freshness proof. |

Planning Scoped Pass msg_d6572c2f246b closes P1/P2; amendment Scoped Pass msg_4d53fb69bf0c approves named-source coverage. Repair1 implementation verification complete: final E9 metrics0/coupling0, registration798/0/0, affected28/0/0 and full7294/0/0; VG1/VG2/EC1 repairs independently cleared in pass2. Final full-diff scope and coordinator E9 pass (entry9); the approver verifies the exact candidate commit and its normal-hook evidence before publication.

## Auto Run Result

Status: done. Outcome: EL-239 implementation and independent native review complete; pass2 clears VG1/VG2/EC1, final scope/simplification PASS covers all32 owned paths, and coordinator final E9 passes. Matching full7294/0/0, registration798/0/0 and affected28/0/0 remain applicable; final changes are records only. The approver checks the resulting exact SHA and normal-hook evidence. Coverage's missing exact proof-owner role remains explicit and grants no mode proof or completion; browser, remote build/artifact/deployment, performance and release qualification remain unverified. No publication claim.
