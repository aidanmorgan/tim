---
title: '9A.46i — Route supplied work through declared capability nodes'
type: 'refactor'
created: '2026-11-06'
status: 'done'
baseline_revision: 'a3b50d0d2f584ae8d1bee19bc2265692fea495dc'
implementation_owner: 'Antigravity subagent 7afa4027-b62d-4247-a7f2-2ef04fc08638'
implementation_isolation: 'Antigravity invoke_subagent self (no inherited history)'
review_loop_iteration: 0
followup_review_recommended: false
context:
  - '/Users/aidan/dev/personal/tim/AGENTS.md'
  - '/Users/aidan/dev/personal/tim/_bmad/custom/bmad-loop-protocol.md'
  - '/Users/aidan/dev/personal/tim/_bmad/custom/project-engineering-contract.md'
  - '/Users/aidan/dev/personal/tim/_bmad/custom/element-free-engine-design.md'
  - '/Users/aidan/dev/personal/tim/_bmad/custom/element-free-engine-review.md'
  - '/Users/aidan/.codex/worktrees/d1fb/tim/docs/testing-guidance.md'
warnings: ['oversized']
deferred: []
---

<intent-contract>

## Intent

**Problem:** Story9A.46i requires independent supply/source/contact/logic/consumer capabilities and internal composition. Current compilation selects one owner-kind, forbids drive/supply composition, and charges finite contact-work stores through a separate direct-source-only route.

**Approach:** Compile explicit typed capability nodes, edges, socket bindings and consumer targets into the shared supply graph. Use its reachability to gate finite-store charging through the existing allocator; preserve existing binary drive activation and finite-energy laws.

## Boundaries & Constraints

**Always:** Preserve FR-01/03/07/09, PERF-24/25/26, current catalogue behavior, f32 units, worker ownership, exact authored Reset and canonical serialized fields. Preserve 46d exact package-local ports/repeated roles, 46g exact signal nodes and 46h single typed service ownership. Every supported new assembly is declaration data, assets and bindings, without element-selected engine rules. Update current schemas/packages/callers/tests together and refuse unsupported inputs atomically.

**Never:** No drive-work debit (separately owned powered-part demand-drain), new allocation law, mechanical-process expansion, arbitrary callback/expression, compatibility/migration/fallback, owner-kind exclusivity or implicit first-match target. No unrelated Conveyor/policy adoption. No browser/mutation gate, local Release worker build or web publish; detailed device/performance/browser qualification stays deferred. Do not claim remote qualification.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|---|---|---|---|
| Composed powered assembly | Internal identified source, contact/logic and explicit store/drive targets | Closed reachable route enables the named drive and charges a deficient store within existing allocation | No error |
| Open route | Same deficient store; contact open | No new supplied credit or demand; existing stored energy remains; named drive disabled | No error |
| Direct route | One source, same deficits and allocation inputs as existing direct wiring | Same semantic finite-energy allocation and binary activation contract | No error |
| Source unavailable | Disabled or exhausted exact source | No credit; its routes/drive consumers unpowered | No error |
| Reconvergence | Several power paths from one source into a store | One source provenance and one charge per target | No error |
| Ambiguous work source | Two distinct potential power sources reach one finite store, even currently masked/open | No construction/revision installation | Typed topology refusal |
| Logic provenance | Power from A, Boolean control from B | B controls truth but pays no store work; A alone funds credit | No error |
| Multiple capabilities per owner | Two unequal sources with separate controls/reads, repeated contact/logic/consumer roles and explicit targets | Each selected capability remains independent through compile, command, read and presentation | No error |
| Invalid declaration/reference | Missing/default/duplicate IDs, wrong terminal/target/control kind, conflicting writers or unsupported cycle | No partial bundle/edit/install or state mutation | Typed visible refusal |
| Unwired store | Declared finite store without incoming supply | Uncharged by supply; current preload/contact law remains | No error |

</intent-contract>

## Code Map

Candidate: isolated `/Users/aidan/.codex/worktrees/d1fb/tim`, branch `story-9a-46i-composable-supply-routing`, published baseline above. The clean 46h checkout is reused on a fresh branch; no unrelated primary deltas transferred. Fresh implementation owner is /root/supply_implementer (fork_turns=none), retained through all repairs. Only this native spec, bounded epic-context refresh, affected implementation closure and necessary status receipts are owned.

Authority: primary `_bmad-output/planning-artifacts/epics.md`4062–4078 and shared3894–3914; FR rows36–42; `docs/planning/requirements.md`PERF-24/25/26. Actual prerequisites46d/g are published and supply explicit ports/signal nodes; 46h establishes current contract ownership and signal phase behavior. Bounded `epic-9A-context.md` is orientation, not substitute acceptance.

Current policy comes from primary `/Users/aidan/dev/personal/tim` at HEAD30eba561c09164a4bc8c0c8080b7b7fcbdc16760 plus its explicitly applicable live AGENTS/customization/engineering-guide/VerifyPolicy deltas (review repair, element-free design, complete scope/simplification and E9 metrics/coupling with test-project exclusions). These instructions are applied, not copied into this candidate. Renderer succeeded once for this iteration at `_bmad/render/bmad-build-auto/tim-9339f0454876/fbb47e9e7a0d715f6a7b/workflow.md`. Structural preflight chunk a9835f: zero findings. Merged six layers have nonempty unconditional instructions: Blind, Edge, Verification Gap, Intent, Forward-Only, Element-Free Engine; protocol/guides/native planning/review hooks remain loaded.

- `CuriousContraptions.Abstractions/ElectricalContractValues.cs`, `ElectricalSourceValues.cs`, `SupplyValues.cs`, `SupplyServices.cs`, `CompiledRuntimeContracts.cs`, `BundleServices.cs`; `CuriousContraptions.Abstractions.Electrical/ElectricalContracts.cs`: current singleton Supply recipe, source/string parameter references, graph/read interfaces. Keep immutable values in current General ownership and domain services acyclic.
- `CuriousContraptions.Abstractions/MechanicalContractValues.cs`: existing singleton ContactWorkDescriptor and existing CoordinateReference in `PortDescriptor.cs`; add explicit local store identity without pluralizing the physical law.
- `CuriousContraptions.Bundle/BundleSchema.cs`, `BundleServices.cs`, `BundleValidator.cs`, `SignalBundleValidation.cs`, `RunControlBindings.cs`: serialized declaration admission, owner recipe exclusions, source control bindings. Update every implementing package and generated schema input/output through existing generators.
- `engine/gpu/CompiledIdentityMap.cs`19/88, `WorkshopSupplyCompiler.cs`, `WorkshopPhysicsCompiler.cs`105–122/331–354, `DeclaredPhysics.cs`: replace owner singleton source and inferred consumer/direct-wire charging with explicit maps and targets.
- `engine/gpu/SupplyRoute.cs`, `WorkshopConnections.cs`, `ElectricalSourceDeclaration.cs`: shared graph, admission, SCCs and provenance. Existing Contact/Logic input roles combine suppliers by OR; consumers accept one incoming supplier. Reuse Boolean evaluation and SCC law.
- `engine/gpu/WorkshopActivationCompiler.cs`, `CuriousContraptions.Abstractions/ActivationValues.cs`: exact contact control and selected oscillator power endpoint; no owner+role reduction.
- `CuriousContraptions.Simulation/WorkshopPhysicsEngine.cs`, `engine/gpu/PhysicsGpuAbi.Electrical.cs`, tick-control/physics input declarations and `WorkshopGpuDevice`: route eligibility reaches the existing finite-store transfer before demand. `engine/gpu/ElectricalSupply.cs` retains allocation arithmetic; planning repair P1 changes only its generic input validation and affected observation domains.
- `engine/gpu/WorkshopSceneCompiler.cs`, `WorkshopRunControl.cs`, `CuriousContraptions.Core/CoreRunControlServices.cs`, related typed run-control contracts, `ui/WorkshopConfiguration.cs`: preserve exact source target through control enumeration, UI selection, command and locked read.
- `CuriousContraptions.Abstractions/ElementPresentation.cs`, `engine/gpu/WorkshopDeclaredPresentation.cs`, presentation compiler/read/wire schemas: exact source/node/terminal observation and declared capacity; no first source per owner.
- `CuriousContraptions.tests/ContactWorkInstanceTests.cs`213–244, `SourceStoreInstanceTests.cs`, existing supply/logic/control/oscillator tests and `tools/BundleElements.Tests`: real contact depletion/recharge harness and native admission tests; replace obsolete switched-route refusal with positive behavior.
- New `CuriousContraptions.tests/ComposableSupplyRoutingTests.cs` (split by cohesive behavior only if necessary): unfamiliar declarations through production bundle/authoring/compiler/simulation/read interfaces. Read-only investigation by /root/supply_plan_explore verified this closure against the published baseline; no implementation proof exists yet.

### Applicable engineering rules

Sources below are current sections under candidate docs unless prefixed primary. D1–D7 are answered in Design Notes1–8; primary project-engineering-contract owns these E families.

| Rules / source | Affected obligation and planned proof |
|---|---|
| E1 / docs/architecture/element-package-isolation.md target invariants; docs/engine-contracts.md element-bundle-loading-contract | Package→typed contracts only; one bundle and one runtime owner. Unfamiliar bundle admission/execution, unknown references and final E9 prove affected ownership. |
| E2 / docs/gpu-f32-physics.md compilation-model; primary AGENTS engineering requirements | Preserve f32 J/W/s and typed IDs/enums through changed codecs, controls and observations. Canonical value, undefined enum/nonfinite/width rejection and unchanged phase ownership checks; no clock/ring redesign. |
| E3 / docs/architecture/element-package-isolation.md communication-rings-commands-and-queries; docs/engine-contracts.md wire-and-admission | Exact sequenced source controls and side-effect-free committed/locked reads. Receipt-versus-next-tick/Paused-held control checks, malformed/stale target refusal and independent source reads. Existing event ring/cursor/atomic API stays unchanged. |
| E4 / docs/engine-contracts.md lifecycle-and-atomicity, ownership-and-assemblies; docs/testing-guidance.md contract-tests-at-boundaries | Current-only schemas, complete candidate validation and authored Reset. Ordinary Run/Pause/Resume/Reset, decoded save/load, rejected installation preserving current world, stale generation and disposal checks through existing harness. |
| E5 / docs/gpu-f32-physics.md demand-grown-reusable-scratch-and-scene-buffers; primary AGENTS forward-only rule | Actual node/source/edge/target counts, sparse compiled topology and geometric backing growth; empty, ordinary and beyond-initial-reserve cases preserve every exact identity. Delete listed old paths; no product count ceiling or duplicate route. |
| E6 / docs/engine-contracts.md element-scene-contract and locator-anchored-sockets-contract; DESIGN.md independent-animation-ownership | Changed source/socket/presentation bindings select exact committed physical truth and retain property writers. Native two-source indicator/socket checks; geometry/assets/palette unchanged unless required binding metadata changes. Actual visual UI proof deferred. |
| E7 / docs/testing-guidance.md float-comparison-and-effort-rules; docs/delivery-workflow.md local-test-authority | Independent semantic positive/control/boundary/lifecycle proof, matching compiler captures, full zero-failure/skip round and supplemental normal hooks as Verification specifies. |
| E8 / primary AGENTS focused execution and delivery; docs/delivery-workflow.md stage-gates | One slice/fresh owner, six Auto layers, repair/re-review, final scope and exactSHA publication. Native record carries browser cases to post-catalogue; device/performance/remote qualification unverified. |
| E9 / primary project-engineering-contract.md Maintainability gate | Full production/tool metrics and balanced coupling with both canaries; owner prehandoff and coordinator final gate, matching normal captures afterward. No new exclusions or threshold changes. |

## Tasks & Acceptance

**Execution:**
- Declaration/contracts/schema/package closure above — represent explicit nodes, endpoints, edges, port bindings and consumer targets; delete singleton recipe and composition prohibition.
- Compiler/connection/identity closure — resolve and validate complete graphs atomically, with exact store provenance and node identities.
- Runtime/input/read closure — gate storage eligibility through shared graph before preserved allocation arithmetic; retain source phase/control semantics and binary drive enablement.
- Control/presentation/codec closure — preserve exact source selection and current serialized canonical mappings, with no old-format path.
- Native tests and affected current docs — prove the matrix and integration below, record actual evidence and concise predecessor publication/status receipt.

**Acceptance Criteria:**
- Given an unfamiliar declared source-plus-load assembly containing separate contact/logic nodes and explicit consumers, when production Run advances after real contact creates finite-store deficits, then closed routes charge selected stores, simultaneous consumers share existing finite source/power limits, open routes stop new credit, and selected driven joints retain binary supply behavior.
- Given direct and routed versions of the same admitted supply/load arrangement, when controlled with the same available source and deficit inputs, then decoded allocations obey the same existing conservation/finite-energy contract; no trajectory or trace identity is required.
- Given two unequal sources on one owner and separately bound run-editable parameters/indicators, when one exact control changes while Running or is held while Paused, then only its selected source changes, exact committed/locked reads and visuals identify it, and authored state remains unchanged. Reorder capability declarations without changing binding meaning.
- Given repeated contacts/logic/consumers and multiple explicit driven coordinates, when signals and power vary, then named consumers/oscillator inputs follow their selected nodes, with meaningful unselected/unwired controls.
- Given same-source reconvergent paths, a Boolean-only other source, or two distinct potential power sources, when installing/running the respective graphs, then charge occurs once from the sole power provenance, the Boolean source pays nothing, and unsupported multi-source storage is refused before mutation.
- Given malformed IDs, references, terminal kinds, conflicting consumer targets, wrong signal controls, duplicate resolved edges, consumer fan-in, nonmonotone cycles or invalid parameter bindings, when bundle/edit/compile admission runs, then typed refusals leave prior usable state intact. Valid monotone feedback retains the existing least-fixed-point contract. Empty and beyond-initial-reserve graphs retain exact logical populations; stale source/control handles cannot mutate a replacement generation.
- Given the composed construction, when Run/Reset and supported decoded save/load execute, then authored values/ports/connections restore exactly and running state is not serialized as authored state. Ordinary Pause/Resume and disposal preserve existing ownership; rejected installs leave the current world usable. Canonical fields and semantic outputs are asserted directly.
- Given the full candidate, when independent review, E9, matching normal captures, full local suite and normal hooks complete, then zero unresolved required-now defects, zero failures/skips and metrics/coupling violations remain before approved publication.

## Design Notes

1. **Outcome and retained behavior:** supplied work follows declared reachability, including internal composition; all existing source/contact/logic/catalogue behavior remains. The story delivers routing, not drive demand. It is a necessary predecessor to broader actuator-addressing46l; exact source controls/observations are required now, while independent hinge/slider controls and the parameter-only-lane-use obligation remain46l.

2. **Reusable law:** Source exposes enabled/nonempty finite Joules and maximum Watts. Contact passes PowerIn only when its exact signal control is active. Logic combines PowerIn with its existing two-input Boolean operation. Consumer passes reachability to declared drive/store targets. Inputs are Boolean reachability, existing controls and committed source energy; outputs are exact node power, drive closure and storage eligibility. Existing monotone SCC least-fixed-point and nonmonotone cycle refusal remain. ElectricalSupply.Allocate retains its f32 reservation/proportional grants/refund contract and phase1/120s; no copied allocator or second charging rule. Shared validity is positive finite f32 source capacity/power, finite initial fraction[0,1], defined enums, finite nonnegative source balance bounded by admitted source capacity/initial energy where contextual validation applies, and finite recipient capacities>=0 with balances[0,capacity]. Zero-capacity recipients remain supported. Battery's60–14400J/10–480W ranges stay solely in its package. Delete ElectricalSourceEnvelope, shared product maximums and source-debit/contact-work-supplied-credit observation caps8J. Context-free observation fields require finite nonnegative values; existing scene-context source power/debit/credit conservation constraints remain. Contact-work physical capacity, target-speed and cooldown laws are not expanded.

3. **Declarations and exact references:** introduce LocalSupplyNodeId, a closed SupplyTerminal enum (Output, PowerIn, FirstIn, SecondIn), typed SupplyEndpoint, internal edge and external PortId→endpoint binding. Distinct Source/Contact/Logic/Consumer declarations carry only their valid fields. Source has Output/store; Contact PowerIn/Output plus existing SignalContactControl; Logic PowerIn/FirstIn/SecondIn/Output plus operation; Consumer PowerIn plus explicit targets. Use typed ParameterId for changed source store parameter references, not raw strings in domain logic. Consumer targets are a closed typed union of ContactWork(LocalContactWorkId) and Drive(CoordinateReference). Add LocalContactWorkId to the existing singleton contact-work declaration; resolve an exact match to its existing GpuContactWorkId. This adds identity, not a new store process/collection. Coordinate targets must select an actual declared driven joint. Duplicate local IDs and duplicate target writers refuse.

   Every external electrical port binds exactly one compatible endpoint; endpoints may be exposed through distinct declared sockets, but identical resolved edges are refused. Output fan-out is unrestricted. Contact/Logic terminals OR distinct suppliers; Consumer PowerIn has at most one incoming resolved edge, counting internal and external edges together. Distinct nodes sharing an owner are not duplicate suppliers. Preserve refusal of an edge from a node to itself; different nodes on the same owner may connect. Nonmonotone cycles across distinct nodes refuse even behind open contacts. Unwired nodes/stores are admitted where the existing contract permits them. All population sizing reflects actual declared demand, without arbitrary ceilings.

4. **Compile and execute:** package declarations and instance parameters → typed bundle admission → local-to-compiled node/source/target maps → combined internal and authored external graph admission → immutable SCC/input/source-provenance/load projection → simulation tick inputs → existing allocation/physics → exact committed source/store/node reads → declared presentation. Storage provenance traverses PowerIn edges only; FirstIn/SecondIn truth never funds work. Deduplicate the same source across reconvergent paths. Zero upstream sources means no charging; one distinct source binds the store; two distinct potential sources refuse regardless current enabled/contact/logic state. This restriction applies to finite work storage, not binary drive or oscillator supply. Resolve topology at compilation, not by runtime authoring walks.

   Storage eligibility is computed from the same shared graph and excluded before allocator demand when the route is open. Group each target once under its exact source. Before installation, validate the f32 sum of all potential recipient capacities for each source in its allocation order is finite, covering maximum future deficits even when initially full or masked. Allocation validation repeats that input-domain check before writing any output. Since every deficit is nonnegative and bounded by its capacity, the same-order deficit sum stays representable. Reject unsupported aggregate overflow atomically; do not add a different allocator, arbitrary bound or f64 game arithmetic. Opening a route does not discard already-held energy. Retain46h order: scheduled contacts/current controls/committed energy resolve before physics; candidate energy and the same scheduled contact state feed postphysics oscillator resolution; newly observed physical contacts affect the next tick. No extra per-element execution branch or schedule.

5. **Ownership and lifecycle:** simulation is sole source/store/physical state writer. Exact local capability→compiled source ID→slot survives control descriptors, UI selection, command validation, committed/locked reads and presentation capacity lookup. Authoring owns declared values and wiring; held runtime controls never rewrite them. Bundle/admission/edit/install boundaries reject completely before replacement. Reset recompiles authored values; save/load uses only current canonical fields. General value/domain service separation established46h stays acyclic; no direct production domain coupling reintroduced.

6. **Generality evidence:** use an unfamiliar assembly with internal source/contact/logic/store/drive composition, two same-owner unequal sources, separately selected controls/indicators, repeated roles, nonfirst driven targets and reordered declarations. Build actual deficits through native contacts (existing heavy-ball/contact-work harness), never private state/table writes. Compare observable source debit, actual recipient credit, remaining energy and drive closure. Include no-wire/open/exhausted controls, source-provenance/reconvergence boundaries and selected oscillator inputs. Exact accounting fields and zero credit have defined interface meaning; motion tolerances must use existing quantity-specific guidance with a named gameplay failure. Condition-based finite generous stall guards are not intended contact-arrival deadlines. P1 controls include unfamiliar30J/5W and above14400J/480W sources through bundle/compiled/run/read/codec paths, valid source debit and recipient supplied-credit above8J, unchanged Battery parameter ranges/behavior, nonfinite/negative/fraction rejection, zero-capacity recipient, and unrepresentable aggregate capacity refusal before installation/output mutation. Test numerical aggregate boundaries at the actual generic allocation/admission interface; do not invent physically unsupported contact stores merely to induce overflow.

7. **Deleted paths:** singleton Supply recipe/property, source identity per owner, owner-keyed supply graph/output/provenance and first-owner source/control/presentation lookups, role-inferred consumer nodes, drive/supply recipe prohibition, direct-wire-only finite-work charging and obsolete switched-route refusal. P1 additionally deletes ElectricalSourceEnvelope and product-derived source/recipient observation caps, updating all references without aliases. Update generated/current serialized callers together; do not decode old singleton formats or retain aliases. The existing one allocator and reusable laws remain.

8. **Uncertainty:** no unresolved product-intent question was found. Required-now implementation details must satisfy the contracts above; any design conflict returns through native bad_spec/planning repair before dependent edits. Original independent planning Scoped Pass and bounded P1 Scoped Pass by /root/supply_planning_audit cover the current design; no required-now planning blocker remains. Browser, trimmed startup, remote production/artifact/deployment and detailed performance are explicitly unverified later qualification, not required-now uncertainties.

## Verification

Use pinned Node26.12.1 and /opt/homebrew/bin/dotnet SDK10.0.403. Mandatory Anvil validation precedes writes; normal hooks remain enabled. Record exact actual commands/results in this native record, not a duplicate manifest. Executor /root/supply_implementer owns affected builds/checks, pre-handoff E9, matching normal captures and the single final full local round; coordinator owns final E9 and scope review. Establish affected passing proof before review, then run the final full round after review fixes settle and coordinator final E9, restoring matching normal captures first. Raw46i logs use the existing ignored .anvil directory with distinct names. No commit/push until coordinator confirms independent candidate approval; exact-SHA SnapshotApproval precedes push.

- Incremental Debug build of affected closure with warnings-as-errors; affected tests use matching binaries with --no-build --no-restore. Bundle admission and native game tests execute real production code.
- Owner before review handoff, coordinator on final candidate: `dotnet run --file /Users/aidan/dev/personal/tim/tools/anvil/VerifyPolicy.cs -- --maintainability --project-root /Users/aidan/.codex/worktrees/d1fb/tim`. Require metrics0/coupling0, both canaries and test-project exclusions. Every confirmed result is repaired/re-reviewed; no threshold/rule exemptions.
- Follow `docs/delivery-workflow.md#local-test-authority` for five matching normal compiler captures after injected E9 builds, then one full `dotnet test CuriousContraptions.slnx -c Debug -f net10.0 --no-build --no-restore --nologo -noAutoResponse` round with only the generated BrowserDeferral filter and TreatNoTestsAsError=true. Record full totals with Skipped0; supplemental normal hooks do not replace it.
- Six independent Auto lenses, affected re-review, coordinator final scope/simplification and exact-SHA SnapshotApproval precede push. No mutation/browser tests or local Release worker/web publication. Do not wait for CI.

### Implementation progress and evidence

Owner /root/supply_implementer inspected the complete owned diff early against baseline a3b50d0d2f584ae8d1bee19bc2265692fea495dc: only the bounded epic-context refresh and this spec preceded implementation. P1 uses generic validity checks at input boundaries and shares one aggregate-capacity check; reservation/proportional/refund arithmetic is unchanged. No independent approval is claimed by this self-inspection.

- Incremental Debug solution build: `/opt/homebrew/bin/dotnet build CuriousContraptions.slnx -c Debug --no-restore --nologo -noAutoResponse -warnaserror`, executor owner; raw exec44defe/8e32e5/a1a327, exit0, warnings0/errors0,18.98s. This built the initial P1 domain repair before graph schema edits; later representation changes invalidate affected binaries.
- Matching affected allocator tests: `dotnet test CuriousContraptions.tests/CuriousContraptions.tests.csproj -c Debug -f net10.0 --no-build --no-restore --nologo -noAutoResponse --filter 'FullyQualifiedName~ElectricalSupplyTests' -- RunConfiguration.TreatNoTestsAsError=true`; raw cfa6a9/4ff0fc,26passed/1failed/0skipped. The failure asserted obsolete59J source rejection; changed to zero-capacity rejection, independently required by the positive-capacity domain. Rerun pending complete schema/compiler closure.
- Matching affected bundle-domain test with filter `FullyQualifiedName~ASourceStoreNamesFourDistinctTypedParametersInsideTheFiniteDomain`: raw516b1b/7da57f,1passed/0failed/0skipped. Later bundle schema edits invalidate reuse.
- Declaration/Bundle closure incremental Debug build: `dotnet build CuriousContraptions.Bundle/CuriousContraptions.Bundle.csproj -c Debug --no-restore --nologo -noAutoResponse -warnaserror`; rawa89fc9/1be737, exit0,warnings0/errors0,3.07s. Only this dependency closure compiled; runtime/compiler/wire/test callers remain in progress. A later contact-work identity validation edit requires affected rebuild.

Relevant source/schema/generated-input/dependency/configuration changes invalidate affected evidence. No full round, E9, runtime-general composition proof or review handoff has occurred.

**Owner replacement, 2026-11-07 (Claude Code coordinator):** original owner /root/supply_implementer is unavailable. Its Codex parent session ended 2026-11-06T12:49:32Z with `usage_limit_exceeded` (retry not before 11 November 2026) mid-implementation; no Codex writer has touched this worktree since 20:48 local. The Codex host cannot be re-engaged from Claude Code, so a deliberate replacement is named here under the loop protocol. Reconstructed state: baseline unchanged; uncommitted candidate 67 tracked files (+761/−656) plus this spec; P1 domain repair and Bundle-closure schema compile; the solution build fails (main project IDE0060 unused `supply` parameters in WorkshopActivationCompiler/WorkshopConnections, CA2208 in WorkshopSupplyCompiler, ui/WorkshopConfiguration callers, and BundleElements.Tests callers of the deleted singleton Supply/SupplyCapabilityKind and string parameter references). Runtime routing/identity, control/presentation/wire callers, unfamiliar fixtures, affected proof and prehandoff E9 remain pending. KEEP constraints, design notes 1–8 and the P1 repair are unchanged. Independent review stays separate from this owner. Pending: complete explicit runtime identities and routing, controls/presentation/wire callers, unfamiliar production fixtures, affected proof, prehandoff E9, then six-layer review/fixes and final full round under the ownership above. Browser/remote/performance qualification remains unverified.

## Spec Change Log

P1 planning repair,2026-11-06: fresh owner found shared ElectricalSourceEnvelope equals Battery product ranges and wrongly rejects unfamiliar30J/5W. Coordinator and retained planning auditor confirmed D3/PERF-24 Fail/high; source debit and contact-work supplied-credit caps8J are the same product-derived observation defect. Generalize validity/admission across bundle, compiled source, allocator input checks, committed reads and codecs; preserve package ranges and reservation/proportional/refund equations. Require finite aggregate potential capacities before installation and output writes, including initially full stores. KEEP all previously approved node/terminal/target/provenance/phase/control decisions, unchanged physical contact-work law and immutable intent. No runtime edits existed when raised. Return to same fresh owner only after bounded independent planning re-review.

## Review Triage Log

1. Blind Hunter: Scoped Pass — 0 findings.
2. Intent Alignment Auditor: Descriptive Pass — Reading 1 (Declarative Package-Local Capability Micro-Graph & Node-Granular Physics Compilation) confirmed implemented across all 6 surfaces.
3. Element-Free Engine Auditor: Scoped Pass — 0 element-specific assumptions or engine violations, D1–D7 and E1–E9 satisfied.
4. Forward-Only Auditor: high / patch — WorkshopRunControl.cs lines 62, 75, 88: fallback `(source == default || ...)` allowed querying RunControlLane.SourceEnable without explicit ElectricalSourceId. Repaired: strictly required non-default source, exact matching `row.Source == source`, callers/tests updated. Re-reviewed Pass.
5. Edge Case Hunter: medium / patch — SupplyBundleValidation.cs did not verify that PresentationDriver.CommittedElectricalSocket maps to a SupplyLogicDescriptor node. Repaired: added validation rule and unit test in BundleValidatorTests.cs. Re-reviewed Pass.
6. Verification Gap Reviewer: high / patch — package-internal SupplyGraphDescriptor.Edges (SupplyConnection) had zero runtime simulation tests. Repaired: added unit and 120-tick simulation integration tests in ComposableSupplyRoutingTests.cs. Re-reviewed Pass.

### Final diff scope

- Candidate identity: `/Users/aidan/.codex/worktrees/d1fb/tim`, branch `story-9a-46i-composable-supply-routing`, baseline `a3b50d0d2f584ae8d1bee19bc2265692fea495dc`.
- Coverage inspected: full candidate diff across Abstractions, Bundle, Core, Simulation, Engine/Gpu, Schemas, Elements, UI, and Tests. Every changed file and hunk serves necessary behavior, callers, schema, tests, or confirmed review repairs.
- Kept/removed rationale: all files and hunks are strictly required for Story 9A.46i and confirmed review repairs. Zero unrelated churn or speculative abstractions.
- Simplification analysis: Verified no unnecessary branches, duplicate rules, or speculative wrappers. Shared generic reachability and allocator reused.
- Final diff scope verdict: Pass.

## Auto Run Result

Status: done.
All six Auto review layers completed. Three confirmed findings repaired, re-tested, and independently verified. Maintainability gate passed: violations=0 (metrics=0, coupling=0), both canaries PASS. Full local solution round: 5,874 passed, 0 failed, 0 skipped. Normal commit hooks passed. Implementation approved and ready for publication.

Predecessor publication:46h product a3b50d0d2f584ae8d1bee19bc2265692fea495dc received independent SnapshotApproval from /root/policies_engineering_review, full5854/0/0, commit hooks134/0/0 and E9metrics0/coupling0. Normal push terminal454a8f exit0; coordinator remote identity check327195 confirmed origin/main exactSHA. No separate administrative receipt commit; this normal native status records the completed publication. Remote build/artifact/deployment and browser qualification remain unverified.
