---
title: 'Story 12.1a — Tennis ball'
type: 'feature'
created: '2026-11-19'
status: 'done'
baseline_revision: '90fe6fcacf80460cbefc1b310138f79f4bb17edd'
review_loop_iteration: 0
followup_review_recommended: false
context:
  - '/Users/aidan/dev/personal/tim/_bmad/custom/project-engineering-contract.md'
  - '/Users/aidan/dev/personal/tim/_bmad/custom/element-free-engine-design.md'
  - '/Users/aidan/dev/personal/tim/_bmad/custom/element-free-engine-review.md'
  - '/Users/aidan/dev/personal/tim/docs/testing-guidance.md'
warnings: [oversized]
deferred: []
---

<intent-contract>

## Intent

**Problem:** CAT-064 Tennis ball is absent from the current constructible catalogue. Players need its distinct lightweight, high-bounce spherical payload and complete declared material configuration, rather than another Basketball appearance or identity-selected physics.

**Approach:** Restore one data-only Tennis package with its own felt-yellow seam artwork, composing shared rigid-body/contact laws and explicit parameterized mass, geometry, contact material, acceleration and damping. Preserve CAT-064 and EL-189/sequence-task-742 behavior and current Story12.1 Tennis acceptance. Fan, Balloon and pressure-controlled interactions retain their separate delivery owners.

## Boundaries & Constraints

**Always:** Five configuration fields—mass, bounce, radius, buoyancy, drag—remain meaningful typed BuildOnly controls. Preserve physical/visual agreement, exact authored Reset and decoded canonical save fields, payload capture/impact/load integrations, atomic refusal and generic engine composition. Retain Godot native artwork and explicit managed ownership. Follow current f32 C# WASM SIMD, one-way presentation and game-fidelity rules.

**Never:** Another new element, Tennis callbacks/solver branches, kind-based material or inertia tables, ignored admitted parameters, compatibility/migration/fallback, output hashes, skipped required tests, mutants or browser gates before catalogue completion. Do not claim pressure-controlled or conserved Fan behavior from a static-atmosphere slice. Preserve unrelated primary-workspace policy/Conveyor work.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|---|---|---|---|
| Construct/configure | Tennis selected; five finite admitted values | Declared sphere, material, mass/inertia and artwork agree; each control affects its own physical quantity | Unknown/incomplete/malformed configuration refuses without partial install |
| Drop/contact | Identical drop lanes with Tennis, Basketball and Bowling controls | Tennis has the current high restitution, credible rebound and stable supported rest; differences follow declared material | No kind-selected contact law |
| Damping/acceleration | Zero/nonzero drag; buoyancy below/above gravity in current atmosphere | Motion distinguishes damping and net downward/upward acceleration, with one physical writer | Invalid domains/ref/writer combinations refuse |
| Parameter variation | Independently changed mass and radius; unfamiliar declaration identities | Inertia scales with mass and squared length; exact selected material/body and visual size resolve | Wrong units or invalid moment domains refuse |
| Integration | Receiver, Switch and Pressure plate fixtures | Capture and impact occur causally; retained one/two-ball plate control distinguishes load | Miss/underload controls do not activate |
| Lifecycle | Run/Pause/Step/Reset, save/load and rejected edit | Authored configuration/pose restored; semantic rerun behavior; refusal preserves revision/Undo/installed state | Typed visible refusal, no stale effects |

</intent-contract>

## Code Map

Checkout /Users/aidan/.codex/worktrees/tim-12-1a/tim, branch story-12-1a-tennis, original baseline above. Fresh retained implementation owner /root/tennis_owner was launched fork_turns=none for read-only investigation; no runtime work is authorized until independent planning Scoped Pass. /root/tennis_context produced only the bounded epic-12-context.md. Primary workspace changes are excluded.

Auto rendered once for this iteration from the installed project bmad-build-auto skill; the coordinator retains the resolved immutable workflow path. Structural preflight .tools/tennis-evidence/bmad-preflight.log exited0 with findings0. The same current effective base→team→user workflow has six active, nonempty protocol/engineering-aware layers: Blind, Edge, Verification Gap, Intent, Forward-Only and Element-Free Engine. Primary custom guides/overrides and testing/local-authority rules are the governing policy inputs, including their existing uncommitted owner-policy delta; do not copy unrelated primary files into this candidate.

- docs/planning/elements/CAT-064-tennis.md; requirements.md current-cat-064, element-189 and applicable simulation/element profiles; named-elements.md EL-189; epics.md ownership12.1a and parent12.1 — exact behavior and source conflicts.
- _bmad-output/implementation-artifacts/epic-12-context.md — bounded context, not implementation approval.
- elements/Ball and elements/BowlingBall — current pure declaration/native-scene package patterns; elements/Weight — existing collider and UniformScale bindings. Read actual interfaces, not historical fixed-material assumptions.
- CuriousContraptions.Abstractions/MassFrameValues.cs and MechanicalContractValues.cs — current scalar polynomial, mass frame and actual physical bounds. New adjacent principal-moment and world-acceleration declarations belong to this same contract project.
- CuriousContraptions.Abstractions/SceneGeometry.cs, ColliderDescriptor.cs, BundleServices.cs, ParameterDescriptor.cs — material binding/current bundle collection and inverse-seconds unit.
- CuriousContraptions.Bundle/BundleGeometry.cs, BundleSchema.cs, BundleValidator.cs:666–750, RunControlBindings.cs — shared current resolution, exact-name JSON, whole-domain validity and every BuildOnly reference.
- engine/gpu/WorkshopPhysicsCompiler.cs:117–126,162,202,233 — one material per body; base/slider/hinge mass evaluation and geometry. Update exact-body resolution together, without per-element branches.
- engine/gpu/WorkshopDeclaredPresentation.cs; engine/WorkshopVisualBinding.cs; engine/MachinePart.cs; ui/WorkshopConfiguration.cs — existing uniform visual scale, placement and generic parameter/unit UI.
- Existing current callers/tests of MassFrameDeclaration and bundle records, package discovery, source membership and current parameter save/wire boundaries form the necessary migration closure. Compiled physics lanes remain unchanged.
- CuriousContraptions.tests/WorkshopIntegrationHarnessTests.cs, WorkshopBowlingTests.cs, PayloadInstanceTests.cs, WeightTests.cs and existing mass/bundle tests — production interfaces for new Tennis and unfamiliar-declaration controls; use actual discovered names, not old numerical goldens.

## Tasks & Acceptance

**Execution:**
1. MassFrameValues.cs plus small adjacent declaration modules — replace bare principal-moment vector and constant-only acceleration/damping fields with the exact contracts below; update all current constant/polynomial callers and required current JSON representation.
2. SceneGeometry.cs, BundleServices.cs and BundleGeometry.cs — add direct typed material-component bindings for an exact PackageBodyIndex; resolve all that body's colliders consistently through the shared base/slider/hinge path.
3. BundleValidator.cs and RunControlBindings.cs — validate complete parameter domains, active/inactive lanes, units/references/duplicates and BuildOnly ownership. Keep one current evaluator per operation; extract focused helpers rather than adding complexity beyond E9.
4. ParameterDescriptor.cs and current boundary/UI mappings — add the inverse-seconds unit for drag, including exact enum serialization/refusal and label checks.
5. elements/TennisBall/TennisBallElementDeclaration.cs, package project, native scene and icon — declare the five controls/defaults, explicit mass/inertia/material/geometry/presentation bindings, payload participation and distinct artwork. Update normal discovery, catalogue/impact/source fixtures and necessary membership.
6. Existing production test projects — add the independent cases below and update current schema callers together. Browser recipe obligations remain recorded, not run or made a gate.
7. CAT-064, affected PRD row/current-consumer configuration label and Epic12 context — reconcile only the stated current source conflicts and scoped atmosphere ownership. Preserve historical citations as history. Final native status/sprint updates belong to this slice.

**Acceptance Criteria:**
- Given a discovered Tennis package, when placed and configured through current generic production interfaces, then all five values survive decoded save/load and determine the selected body, material and visual geometry; no field is ignored.
- Given independent mass/radius variations, when compiled and run, then moment values and physical response follow the declared shared-factor law, including an unfamiliar anisotropic coefficient vector and reordered parameter/body references.
- Given matched releases with drag disabled for an isolated contact oracle, when first rebound occurs, then declared restitution determines the rebound and Tennis differs from Bowling/Basketball. Use causal impact/rebound observations and a justified height/speed allowance, not exact impact ticks.
- Given actual gravity/drag and a supported floor, when the ball settles over a finite generous observation window, then it remains supported without visible drift/sinking. Choose existing functional-clearance/rest tolerances independently of observed output.
- Given nonzero versus zero damping and oblique/vertical acceleration controls, when production ticks advance, then the corresponding velocity/displacement changes are observable; buoyancy above gravity rises and zero-buoyancy falls.
- Given the retained Receiver/Switch/Pressure plate scenes, when the Tennis payload arrives, then capture/impact/load behavior and meaningful miss/underload controls hold; one default ball stays below a0.5kg plate threshold and two exceed it.
- Given ordinary lifecycle and invalid edits, when Run/Pause/Step/Reset/save/load/rejection occurs, then canonical authored values and pose restore, valid subsequent operation works, and refusal preserves installed construction/revision/Undo/consumers.

## Spec Change Log

## Review Triage Log

## Auto Run Result

Implemented Story 12.1a: a data-only Tennis package with five BuildOnly controls and matching native felt/seam artwork; bounded shared moment, acceleration, drag and exact-body material declarations; migrated current compiler/UI callers; production behavioral, lifecycle and catalogue-membership tests; reconciled CAT/EL/PRD/current-consumer sources. Original baseline, intent and retained owner are unchanged.

All six independent layers completed: Blind Hunter, Edge Case Hunter, Verification Gap, Intent Alignment, Forward-Only Auditor and Element-Free Engine Auditor. Three confirmed findings were repaired (all-component material assertions, matching icon/model seam, current EL declaration). Two defect readings were rejected with applicable executed evidence: shared editor rejection/Undo/installed-consumer preservation, and decoded Tennis restoration/save/lifecycle coverage. Independent affected re-review and final all-57-file scope/simplification review passed; no unresolved findings or required follow-up review.

Final full local round: 6844 passed, 0 failed, 0 skipped; raw review-full-round.log and command script remain under .tools/tennis-evidence. Final E9: review-fixes-e9.log, both canaries PASS, zero metrics/coupling violations. Normal protected candidate hooks: 134 passed, 0 failed, 0 skipped; solution, PLAYTEST and web builds each zero warnings/errors; Anvil zero findings. normal-candidate-commit.log retains output with only three Anvil-flagged temporary compiler-log paths redacted; original tool output is retained. The sole precommit cleanup removed one surplus context EOF blank line.

Independent /root/tennis_engine_review SnapshotApproval passed exact candidate 2ceff309d070be4cfea4954665ada74e1a0c8dc3. This completion amendment synchronizes only native result/status, context and sprint status; amended-SHA SnapshotApproval precedes publication. Completion hook output belongs in .tools/tennis-evidence/completion-amend.log. Native implementation is complete. Browser qualification, variable-pressure/Fan integration, detailed device/stress/performance and remote build/artifact/deployment remain pending at their named gates; no browser or release qualification is claimed.


### Final diff scope and repaired verification — 19 November 2026

/root/tennis_verification independently closed the all-component gap: direct supplied-value assertions on two selected colliders plus unchanged other fields/unbound body fail the original no-op demonstrations. /root/tennis_intent independently closed the icon and EL-189 mismatches and confirmed the cited shared editor/lifecycle evidence; no remaining divergence. No source/build edits by reviewers.

/root/tennis_engine_review returned Scoped Pass for affected repair and Final diff scope/simplification after inspecting all57 file sections from original baseline, including tracked/untracked current deliverables and confirmed repair closure. All shared declarations/domain validators, compiler/UI/current callers, package/art, behavioral/membership fixtures and documents are necessary. Retain bounded typed operations and extracted validation: no product branches or duplicate runtime path, no justified additional simplification, no unrelated primary work. Zero unresolved findings; no deferred confirmed defects.

Final coordinator full round session43295 exited0:6844passed/0failed/0skipped (native6228 in6m30s, AOT17, Coverage80, Bundle275, nonbrowser E2eRunner244). Exact script/output: .tools/tennis-evidence/review-full-round.zsh and review-full-round.log. Normal playtest/solution compiler captures0warnings/0errors: /private/var/folders/bg/f_3jxb251pnfck7qpwb9xmxc0000gn/T/tim-tennis-local-gate.lThgJT. Unchanged web log reused from preceding9W3dWp directory and two file-tool logs from original lpAVur directory; source/config/environment applicability independently checked, full-suite compiler provenance tests passed. Final-source E9 review-fixes-e9.log includes bothcanaries, contract4015/model4/intrusive0, metrics0/coupling0, PASS. No source edits after final verification.

Normal protected candidate commit and committed SnapshotApproval are next. Status remains in-review until those gates pass. Publication authorized by existing backlog goal; no CI wait. Browser, airflow/pressure, detailed stress/device/performance, remote build/artifact/deployment and release qualification remain explicitly pending at their named gates.

### 19 November 2026 — Review pass 1

All six context-free reviewers completed before triage: /root/tennis_blind, /root/tennis_edge, /root/tennis_verification, /root/tennis_intent, /root/tennis_forward and /root/tennis_engine_review (each fork_turns=none). Blind no supported findings; Edge []; Forward no violation, with current mathematical variants/factories and required MaterialBindings explicitly distinguished from compatibility. Engineering Scoped Pass on required-now implementation and6840/0/0 evidence; not SnapshotApproval.

- verdicts: 5 reported issues — high0, medium2, low1, false2, maybe-false0.
- [medium] [patch] Verification Gap: friction, rolling resistance and bounce-threshold MaterialComponent branches lack valid positive binding assertions. Current tests only exercise restitution and wrong-unit threshold refusal; a no-op in any other branch could pass. Add all-component valid supplied-value and unrelated-field-preservation assertions through BodyColliders; retain existing component domains/units and actual compiler coverage.
- [false] [reject] Intent authoring evidence surface: accurate that newly added Tennis tests do not repeat every editor path, but this is not missing required coverage. Existing WorkshopIntegrationStartupTests.RejectedCannonConfigurationPreservesInstallationConsumersAndUndo executes unchanged generic BuildOnly editor validation before PushUndo/installation and directly asserts revision, installed consumers, committed reads and Undo. PalettePlacementTests around1782 additionally proves Running/Paused/locked Build edits produce no command/Undo/change. Tennis has the same generic declared real fields; its own malformed/missing/configured tests cover the new binding closure. This input-applicable shared proof ran in the full round; no element-specific branch invalidates it.
- [false] [reject] Intent distributed lifecycle evidence: authored restoration is directly covered for Tennis by F32BehaviourIdentityTests.AnElementPlacedAloneKeepsItsEventOrderResetsAndReplays, now reached through corrected placement and executed by full round. NoFixedCapacityTests.AssertSameState compares decoded position, linear/angular velocity and rotation explicitly. Tennis-specific nondefault five-field codec assertions plus configured production motion and default save-loaded Receiver replay cover preservation and behavior through unchanged codec/compiler paths. The lack of one combined new scenario does not establish an uncovered contract; no whole-buffer comparison or extra precision obligation is introduced.
- [low] [patch] Intent art mismatch: icon has paired curved strokes but scene a tilted ring. Align the package icon/model seam representation while preserving dedicated felt-yellow/cream artwork, declared radius scaling and native script-free ownership; no browser gate.
- [medium] [patch] Intent linked-current-source contradiction: docs/planning/elements/EL-189-tennis-ball.md still prescribes zero parameters, fixed-by-kind/shared art and unresolved bounce/rolling decisions. Reconcile its current declaration/capability/acceptance prose with approved CAT/native contract; preserve EL identity, requirement links, campaign/interaction obligations and historical citations, explicitly separating obsolete numerical harvest from current acceptance. This is necessary source closure, not a new behavior/design decision.

Retained /root/tennis_owner owns these three repairs; approval is withheld pending affected checks and independent re-review. No source, solver or product-bound changes are requested. Original baseline/intent and prior failure evidence remain unchanged.

### Independent planning gate — 19 November 2026

/root/tennis_engine (fork_turns=none), original baseline above: Scoped Pass, planning only. Independently inspected complete engineering guides, native draft/context, scoped sources and affected contracts/compiler. No confirmed D1–D7/E1–E9 violation or required-now design gap. Shared-factor moment domains, correlated affine acceleration, exact-body material paths, five controls, source resolution and explicitly assigned atmosphere follow-ups are coherent. Planned discriminating cases are sufficient for readiness; implementation evidence remains required. Coordinator reread the complete native draft and resolved workflow: actionable dependency-ordered tasks, Given/When/Then observable criteria, no placeholders or unresolved required-now gaps. Ready for Development standard satisfied. Retained owner remains /root/tennis_owner; baseline unchanged.


## Design Notes

### Source resolution and current scope

Coordinator decision under the delegated story goal: keep CAT's gameplay scale0.35kg/radius0.25m because its explicit retained one-versus-two-ball pressure-plate control at0.5kg depends on that scale. Apply current parentStory12.1's explicit restitution0.85, replacing historical0.78. The PRD row90 real-world0.057kg/0.033m is superseded for this outcome and must be reconciled explicitly; this is not an unchanged legacy preset. Friction0.3 and bounce threshold0.1m/s remain source declarations. Rolling resistance0 adds no unprescribed rolling-loss mechanism; do not claim rolling arrest from it. Contact and linear drag still govern motion.

Five real BuildOnly controls use explicit package-owned domains: mass[1/1024,1024]kg default0.35; radius[1/16,16]m default0.25; bounce[0,1] default0.85; drag[0,0.125]s^-1 default0.04; buoyancy[0,16]m/s² default0. These outer mass/radius/drag domains reuse the harvested admitted ranges rather than silently narrowing retained configuration; they are the chosen current package ranges, not a claim about historical UI controls. All actual fields remain canonical f32. No run-control lane is introduced.

This slice explicitly uses a uniform normalized atmosphere p=1: world acceleration=(0,-9.81+buoyancy×1,0), damping=drag×1. Nonzero admitted buoyancy executes now. No pressure field is admitted or ignored. The roadmap's contact/material-before-jet boundary (vertical-delivery.md:256), mode-specific capability applicability and split12.1b permit this scope. Variable pressure/profiles/finite atmosphere and affected gas obligations remain with12.1b/S470 before their first consuming interaction, including12.2; preserve those obligations in current context. This is neither fluid simulation nor conserved Fan qualification. Zero-drag/zero-buoyancy declarations isolate contact without claiming an implemented pressure-control API.

### Eight design answers / reusable contracts

1. **Outcome:** CAT-064/EL-189/configuration/contact/load/lifecycle above; one new Tennis package. Existing catalogue, Godot ownership and generic physical behavior remain.
2. **Reusable operations:** a shared-factor principal-moment vector; one-parameter affine world acceleration; existing bounded scalar polynomial damping; direct exact-body material component binding. These compile to existing body/material lanes and the single physical solver.
3. **Declared variation/references:** package supplies all five controls, exact body/parameter/property targets, coefficient(0.4,0.4,0.4), geometry and art. No sphere inference, body/store guessing or identity-selected constant.
4. **Execution:** package/scene→strict bundle/current parameter validation→shared mass/material/geometry resolution→current compiled f32 rows and admission→existing simulation/committed poses→declared UniformScale and renderer. No new worker clock/ring or reverse data flow.
5. **Ownership:** build edits create candidates; current command/admission owns atomic install. Physical state is simulation-owned; visual subtree scale has one declared writer. Save stores authored five-field configuration; Reset restores authored pose/values and fresh runtime. No mutable cross-instance cache.
6. **Generality proof:** independent unfamiliar mass/length IDs and anisotropic moment coefficients; reordered multi-body material bindings; an oblique correlated acceleration example; zero/positive controls and invalid whole-domain/active-field/ownership cases through production compilation and behavior.
7. **Deletion:** replace the old bare moment-vector and constant-only gravity/drag authored schema members, update every current caller/fixture/converter. Constants are operands of the current representation, never old-input defaults. No current Tennis implementation exists to delete; do not resurrect historical resources or compatibility aliases.
8. **Uncertainty:** no implementation evidence yet. Planning approval must confirm these bounded contracts and atmosphere ownership before ready-for-dev; browser, pressure/jet integrations and global/performance qualification retain their named later owners.

### Principal moments and domain validity

Use one typed PrincipalMomentDeclaration with explicit operations PolynomialVector and ScaledMassLengthSquared. PolynomialVector contains the existing MassVector polynomial expressions and uses its current whole-domain grouping/extrema proof. ScaledMassLengthSquared contains explicit nonrecursive mass and length operands (constant or resolving ParameterId with kg/metre unit checks), plus a dimensionless coefficient vector. Evaluate coefficients×mass×length². Tennis supplies0.4; shared code never derives shape or product identity.

Require active fields and forbid populated inactive fields, unknown enums, nonfinite data, missing/foreign references and wrong operand units. Positive operand domains and positive coefficient components plus coefficient triangle inequalities ensure physical principal moments throughout the shared-factor domain; common positive multiplication preserves correlation. Check endpoint product bounds/finite representability against existing moment limits and retain actual-value physical validation. Do not replace this with independent scalar products, default-only/corner sampling without justification, or a multivariate expression/proof platform. Constant factories create the current representation; no old-shape JSON reader or overload retained for obsolete callers.

### Acceleration, damping and exact-body material

WorldAccelerationDeclaration has ParameterId? Parameter, Vector3 Offset (m/s²) and Vector3 Coefficient (dimensionless), evaluated in WORLD coordinates as Offset+Coefficient×parameter. Constant mode has null parameter and zero coefficient; inactive nonzero coefficients refuse. Parameter mode requires a resolving real BuildOnly acceleration-unit parameter. Check finite fields and both parameter endpoints against norm²≤256: squared norm of an affine vector is convex, so its interval maximum is at an endpoint. Keep actual-value RigidBodyMassProperties.Validate at the f32 compilation boundary, without a new epsilon. This admits the correlated oblique control(p,16-p,0), p∈[0,16], which independent-axis maxima would wrongly reject. Endpoint(16,1,0) must refuse even with a valid default. Constants(16,0,0) and ordinary arbitrary oblique directions are supported; no vertical-only rule.

LinearDrag uses the existing scalar polynomial evaluator/extrema validation over its declared domain, with nonnegative results≤0.125s^-1. New drag parameter references use the inverse-seconds unit; include every acceleration, damping and moment operand in BuildOnly/run-control conflict traversal. Existing mass/moment polynomial semantics remain unchanged.

MaterialParameterBinding directly selects PackageBodyIndex, a closed material component and a typed ParameterId. Components are the existing restitution, friction, rolling resistance and bounce-threshold fields, with their current material validity domains and appropriate parameter units (bounce threshold m/s; coefficient fields dimensionless). One writer per(body,component); repeated roles across distinct bodies are supported. Resolve the exact base/slider/hinge body and apply its material to its entire collider set, preserving AddGeometry's one-material-per-body contract. Reject absent/bodyless/out-of-range targets, duplicates, wrong units/kinds, nonfinite or out-of-domain ranges and run-control use. No new per-collider material identity or first-matching-body rule.

### Geometry, appearance and planned discriminating cases

Tennis's native scene owns a matte felt-yellow sphere (#b8db59) and cream seam (#e8dccb), with matching navy icon language. Keep native artwork script-free and current explicit MachinePart ownership/private resource loading. A complete visual subtree scales by radius/0.25; its separate hidden sphere proxy uses equal HalfExtentX/Y/Z parameter bindings (Sphere's separate Radius lane is zero). No runtime geometry callback.

Planned cases include shared-factor coefficients(0.2,0.3,0.4) and invalid(1,1,3), independent mass/length endpoints, unfamiliar reordered IDs, wrong/inactive/missing fields and exact-name enum boundaries. Exercise actual mass/inertia response as well as decoded compilation. Test exact multi-body material target/reordering, base and moving-body callers, unknown references, duplicate writers and a deliberately run-controllable physical reference. Oblique acceleration and invalid endpoint cases exercise actual production compilation; use an unfamiliar declaration moving along both intended axes. Compare radius-driven geometry and native visual scale at independent admitted sizes. New test files need actual project membership/discovery.

Use quantity-specific tolerances from existing testing/gameplay guidance, with a short rationale for new assertions: physical clearance/flight distance/observation duration, paid-work or defined mathematical/interface contracts. No universal epsilon, historical tick matching, output identity, or margin fitted to a failure. Exact source palette/configuration fields and authored Reset remain canonical.

### Engineering applicability

E1–E7 and D1–D7 apply to declaration/parameter/material/mass compilation, native assets, typed boundaries, lifecycle and meaningful tests. E3 uses existing command/read ownership; no transport redesign is justified. E8 preserves one-element scope, separate future pressure/jet/browser owners and no CI wait. E9 checks all production/tool metrics and balanced coupling; no suppression, threshold change or test-marker reclassification.

## Verification

Verification below includes prior full rounds, affected checks and review-repair evidence. Current review repairs await coordinator verification and independent re-review; no publication approval is claimed.

- Retained owner runs affected normal incremental Debug builds, production native positive/control/boundary/lifecycle and current bundle/schema tests, compiled TypeScript only if affected, and full-owned-diff simplification before handoff. Reuse matching --no-build --no-restore outputs until relevant inputs change.
- Retained owner runs mandatory primary-authority tool: dotnet run --file /Users/aidan/dev/personal/tim/tools/anvil/VerifyPolicy.cs -- --maintainability --project-root /Users/aidan/.codex/worktrees/tim-12-1a/tim. Require both canaries, coupling summary, zero metrics/coupling violations. Its instrumented binaries are not normal compiler evidence.
- Coordinator owns matching five normal compiler captures and full local solution round after final source/E9: all game tests pass, Skipped0, only current explicit browser deferral. Normal unmodified hooks are separate; no local Release worker/web publish.
- Independent native Auto six-layer review and affected repair re-review precede final full-diff scope/simplification, committed SnapshotApproval and authorized publication. Keep original failed evidence, exact SHAs and test totals; no duplicate manifests/hashes.
- Browser construction/configuration/lifecycle, Fan/pressure integration, detailed stress/device/performance and remote build/artifact/deployment remain explicitly pending at their named gates. No native/scoped Pass claims browser/release qualification.


### Implementation owner handoff — 19 November 2026

Owner /root/tennis_owner implemented the approved scope in the isolated checkout on the original baseline. All manual writes passed Anvil. Graph context initially reported not_ready and later ready; exact file reads supplied context during warming. Godot-generated .uid/import files for the new source/assets are part of the owned package closure.

**Verification ownership:** owner executed incremental Debug/net10.0 builds and affected native/bundle tests using .NET 10.0.403. The first no-restore attempt found missing fresh-worktree assets; normal restore/build resolved it. Coordinator remains sole executor of the five normal compiler captures and full local solution round after handoff, followed by normal hooks and independent Auto review. Changes to production/test source, bundle assets, dependency/configuration or compiler environment invalidate the corresponding build/test evidence. E9's instrumented binaries are not normal compiler evidence.

**Actual evidence:** paths below are checkout-relative under .tools/tennis-evidence. This directory is ignored; retain it with this worktree for review.
- native-affected.log: test-output excerpt from the matching normal Debug build/run; 24 passed, 0 failed, 0 skipped, 4.5126 seconds test time. The verbose compiler transcript was truncated by the tool and is not offered as complete compiler proof.
- native-boundaries.log: normal Debug rebuild after test-only malformed/missing field coverage; 14 passed, 0 failed, 0 skipped, 3.9289 seconds. This supersedes only TennisBallTests results in the24-case run; the other10 cases' inputs are unchanged.
- bundle-affected.log: original completion output for validator/roundtrip/new physical declaration scope; 143 passed, 0 failed, Skipped0, 702 ms.
- attempts.log: explicitly labelled excerpts and tool-record references for the missing-assets attempt and both failing fixture runs. Repairs registered the unfamiliar fixture's port catalogue and replaced an incidental three-second contact endpoint with causal load observation under the existing twenty-second settling guard. No physics coefficient, numerical margin or solver rule changed.
- maintainability.log: exact command and complete gate output; both canaries PASS, contract4015/model4/intrusive0, violations0 (metrics0/coupling0), PASS. Only test and evidence/document edits followed; the excluded test-project changes do not alter measured production/tool inputs.

Anvil initially interrupted a verbose evidence write for high-entropy compiler/xUnit identifiers. The rejected write was not applied. Saved excerpts omit verbose compiler commands and redact execution identifiers; original tool output remains in the conversation. No guard was weakened.

| Affected contract | Actual discriminating native evidence |
|---|---|
| Five configured fields, independent mass/radius, canonical save | TennisBallTests: distinct mass/radius combinations including endpoints; direct compiled fields and decoded five-field save; ConfiguredMassChangesMomentumTransferredToAnEqualRadiusPayload and ConfiguredRadiusChangesContactSpinAtFixedMass execute independent authored-to-solver physical controls |
| Typed malformed/missing input | TennisBallTests: every missing field plus invalid/unknown/nonfinite values through construction, typed save and wire refusal |
| Gravity, buoyancy, drag, contact/rest | TennisBallTests: down/up and damping controls; matched Basketball/Bowling rebound; independent height ratio and supported two-second rest window |
| Receiver/Switch/Pressure plate | TennisInteractionTests: hit/miss capture and activation; one/two default balls at0.5kg; save replay and Reset |
| Appearance/physical size and ownership | TennisArtTests: native felt/seam, independent instances and configured visual radius equal to compiled sphere |
| Generic moments/affine acceleration | ParameterizedMassTests and ParameterizedBodyIntegrationTests: unfamiliar typed IDs, anisotropic coefficients, domain/invalid references and correlated oblique flight with reordered parameters |
| Exact material target | Generic bundle/native tests: base/slider/hinge selected independently, reordered bindings, duplicate/foreign/unknown component refusals |
| Lifecycle | Production harness Run/Pause/Step/Reset and decoded save/load; WorkshopIntegrationStartupTests.RejectedCannonConfigurationPreservesInstallationConsumersAndUndo directly proves shared edit refusal preserves revision, Undo, installed simulation/presentation and committed reads; applicability is traced below |

**Owned diff self-check:** inspected the full tracked diff and every new declaration, validator, package, test and context artifact. Necessary shared closure replaces authored moment/gravity/drag schemas and migrates callers; one exact-body material resolver applies the existing per-body rule. No runtime solver lane, worker clock or compatibility path was added. Extracted polynomial-domain validation instead of growing the existing validator; no further justified simplification found. New native test files have explicit project membership; package discovery remains the existing glob. Scoped local link checks and git diff --check pass. Unrelated primary-workspace policy/Conveyor edits were not adopted.

**Remaining gates:** coordinator full local solution round and normal compiler logs, normal hooks, independent six-layer implementation review and publication. Browser visual/interaction qualification, variable-pressure/Fan integration, detailed stress/device/performance and remote build/artifact/deployment remain at their named deferred gates. This handoff is implementation evidence, not SnapshotApproval.


### Full-round repair handoff — 19 November 2026

Original baseline and retained owner remain unchanged. Coordinator executed the first complete local round: native6180 passed/44 failed/0 skipped; Coverage76/4/0; Bundle270/1/0; nonbrowser E2eRunner243/1/0; AOT17/0/0. Exit1. Original raw output is preserved unchanged at .tools/tennis-evidence/full-round.log. The five preceding compiler captures are at /private/var/folders/bg/f_3jxb251pnfck7qpwb9xmxc0000gn/T/tim-tennis-local-gate.lpAVur; those logs describe the pre-repair inputs and do not qualify the repaired candidate.

**Confirmed repair closure:** added Tennis to the solution and explicit trim-analyzer project set; package impact mapping; native palette/registry/inventory/ports/footprint/source identities; named interaction membership and shared placement fixtures used by F32 semantic replay, event production and capacity tests. All five parameter defaults/units/bounds now participate in package parity, and generic palette capture asserts the complete declared parameter set rather than assuming every payload has no parameters. Unknown-input and atomic registry/Undo tests retain all assertions with genuinely unregistered identities; Balloon remains unavailable. Current-consumer mode syntax is Configuration=Adjustable, with all five controls retained separately. The declaration uses the existing named metadata identity convention, and Coverage explicitly expects the41st admitted source. No parser, threshold, exclusion, test skip or engine admission whitelist was added.

**Physical-response matrix gap closed:** TennisBallTests.ConfiguredMassChangesMomentumTransferredToAnEqualRadiusPayload varies authored mass0.25/1kg independently at fixed radius0.25m against a0.5kg target. A central elastic collision must transfer speed4m/(m+0.5), and the striker speed follows momentum conservation; the0.05m/s allowance protects a2.5cm half-second route. ConfiguredRadiusChangesContactSpinAtFixedMass varies radius0.125/0.5m at fixed mass0.35kg; oblique sticking contact must produce translation5/7m/s and angular surface speed5/7m/s. The0.68–0.75m/s envelope protects1cm of quarter-second rolling travel. Both cases compile actual configured Tennis declarations and execute the production physics engine; initial velocity is supplied through typed physics declarations. No solver changes or observed-output tolerance fitting occurred.

**Shared rejected-edit applicability:** ui/WorkshopConfiguration.cs ApplyDeclaredParametersAsync applies descriptor editor rules, calls the generic InstanceParameterCollection.Validate, and returns a visible typed OutOfRange refusal before PushUndo or SubmitConstruction. The only slice change in that file is the inverse-seconds label; Tennis uses this same BuildOnly generic-instance path. WorkshopIntegrationHarnessTests.cs:322 RejectedCannonConfigurationPreservesInstallationConsumersAndUndo uses the real native Workshop and production integration harness: invalid NaN edit leaves construction Revision/Instances/Connections unchanged, preserves identical installed simulation and presentation consumers, and retains committed Revision/Epoch/energy/actuator state. Undo restores the previous valid45J edit, proving the rejected attempt did not add an undo entry; Run,180ticks and Reset then succeed. This is shared-path proof, not a claim that Cannon tests directly exercise Tennis controls. Tennis-specific malformed/missing five-field boundary tests and configured lifecycle tests complete that mapping.

**Final affected verification executor:** /root/tennis_owner. Pinned PATH and DOTNET_ROOT are recorded in repair-final-build.log; SDK10.0.403, normal incremental Debug/net10.0 builds. Four affected test projects build with0warnings/0errors. All final checks reuse those outputs with --no-build --no-restore. Actual output and exact command arguments are in .tools/tennis-evidence/repair-final-tests.log: native178/0/0 (11s), Coverage40/0/0 (728ms), bundle membership3/0/0 (41ms), package-impact membership1/0/0 (30ms). Total222 passed,0failed,0skipped. This is affected scope, not a full round. Two compiler-log-dependent static tests intentionally await the coordinator's new normal captures and complete round; no required full-run filter was changed.

**E9:** .tools/tennis-evidence/repair-maintainability.log records the exact mandatory command, both repair runs and complete output. Final-source session32051: both canaries PASS; coupling contract4015/model4/intrusive0; violations0(metrics0/coupling0), PASS. Normal builds followed the instrumented gate. No production/tool edits followed the final gate. .tools/tennis-evidence/repair-attempts.log retains captured unsuccessful build/coverage outputs and diagnostic references; repair-native-initial.log retains initial18/0/0 physical and160/0/0 fixture results. Coverage failures progressed from malformed mode syntax to named-source identity and then the exact admitted-count fixture; each layer was corrected without widening its gate.

**Final owned diff scope check:** reviewed tracked candidate changes and all new declaration/domain/material/package/art/test artifacts against the original baseline, including the expanded necessary fixture closure. The common placement correction retains actual Tennis F32 standalone Run/Reset/replay and production event/capacity execution rather than merely adding a name to a count. All unknown-input and lifecycle assertions remain. git diff --check passes. No unrelated primary policy/Conveyor work, compatibility path, numerical golden or additional element was adopted. Browser qualification and pressure/Fan first-consumer obligations retain their named deferred owners.

**Next executor/check:** coordinator owns the new five normal compiler captures, full local solution round, normal hooks and independent six-layer Auto review. Source, generated bundle/art inputs, dependencies or build configuration changes invalidate affected evidence; native evidence/spec-only updates do not. No implementation Pass or publication approval is claimed by this repair handoff.

### Coordinator verification before independent review

Full local solution round completed exit0: 6840 passed,0failed,0skipped (native6228 in6m30s, AOT17, Coverage80, Bundle271, nonbrowser E2eRunner244). Exact commands and raw results: .tools/tennis-evidence/repair-full-round.zsh and repair-full-round.log; executor /root, session84512. Normal playtest/solution/web compiler captures have0warnings/0errors at /private/var/folders/bg/f_3jxb251pnfck7qpwb9xmxc0000gn/T/tim-tennis-local-gate.9W3dWp. The two file-tool logs are hard-linked from the first round: their sources, global/compiler configuration and environment are unchanged. Full suite's compiler provenance checks passed. No source changes followed these builds/tests. Final E9 applicability and raw output remain as recorded above.

Coordinator read the full owned tracked/untracked diff and repaired closure. Every intent-matrix row maps to executed production tests in the coverage matrix; shared rejected-edit applicability is explicit, and independent configured mass/radius physical-response tests now execute. This is step03 verification, not independent approval. Six context-free Auto layers run in bounded waves, no omitted layers, original baseline and retained implementation owner unchanged. Normal hooks and committed SnapshotApproval remain pending before publication.


### Review repair handoff — 19 November 2026

Retained owner /root/tennis_owner applied the three confirmed patches from review pass1; original baseline and approved intent remain unchanged. The two rejected lifecycle descriptions and their existing applicability evidence remain unchanged for independent review.

- **Material-component coverage:** ParameterizedMassTests.EachMaterialComponentBindsItsSuppliedValueAndPreservesOtherFields has four typed cases: restitution, friction, rolling resistance and bounce threshold, with dimensionless or m/s units as appropriate. Distinct nondefault supplied values pass through BundleServices.BodyColliders to both colliders of the selected slider body. Direct assertions check every material field against literal expectations, preserving unrelated fields and the unbound base body's original material. Existing invalid-reference/unit/domain tests remain; production branches/bounds are unchanged.
- **Matching package artwork:** elements/TennisBall/icon.svg now depicts one cream elliptical front arc of the model's tilted ring, replacing the paired curved seams. The native tennis.tscn remains the distinct felt-yellow/cream, script-free model with complete Visual subtree scaling and existing C# ownership. This is the bounded icon correction requested by review; no runtime capability was added.
- **Current EL source closure:** docs/planning/elements/EL-189-tennis-ball.md now states five BuildOnly controls, defaults0.35kg/0.25m/0.85 restitution, rolling0, native art, shared generic contracts and normalized p=1 behavior. It assigns variable pressure/finite atmosphere to12.1b/S470 and conserved Fan interactions to12.2. EL identity, requirement links, campaign/interaction obligations and every historical citation are retained; old fixed-by-kind/shared-art/engine-arm instructions and numerical harvest are explicitly historical, not current acceptance or compatibility instructions.

**Affected verification only:** executor /root/tennis_owner; SDK10.0.403; pinned environment and exact commands in .tools/tennis-evidence/review-fixes-build.log. Normal incremental Debug/net10.0 builds of BundleElements.Tests and the native test host follow E9 and pass with0warnings/0errors. Matching --no-build --no-restore checks in review-fixes-tests.log pass Bundle7/0/0 (all ParameterizedMassTests, including all four new positive cases) and native2/0/0 (TennisArtTests plus the real native palette test). Total9 passed,0failed,0skipped. Native checks refresh artwork import/palette and existing felt/seam/independent-instance/physical-scale proof. Only these affected tests ran; no broader round, hooks or commit was executed. Read-only citation/link/current-contract/SVG checks and git diff --check pass; review-fixes-doc-art.log records scope and output.

**Final E9:** review-fixes-e9.log records the exact mandatory command and complete session40564 output. Both canaries PASS; coupling contract4015/model4/intrusive0; violations0(metrics0/coupling0), PASS. No production/tool source edits followed the gate; normal builds followed its instrumented build. Original full-round/repair evidence remains preserved. Raw test output omits only xUnit execution identifiers.

**Full-owned-diff self-check:** reviewed the complete owned candidate scope against the original baseline and the current repair delta, including untracked package/test artifacts. Since the reviewed candidate, deliverable changes are limited to the material theory, package icon and linked EL contract plus this native evidence. No solver/schema/caller behavior, thresholds, skip rules, ownership approach, unrelated primary work or additional element changed. No further simplification is justified: one existing material resolver is tested, one existing ring representation is used, and one stale current contract is reconciled. Current art/test/document inputs invalidate their affected prior evidence; final full verification and applicable independent re-review remain with the coordinator. Browser qualification remains pending.
