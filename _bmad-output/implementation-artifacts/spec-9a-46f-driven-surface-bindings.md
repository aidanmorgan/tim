---
title: '9A.46f — Drive each declared contact surface from its own coordinate'
type: 'refactor'
created: '2026-11-05'
status: 'in-progress'
baseline_revision: '648d067056293a4d045faf88e6937db38ad26dbf'
review_loop_iteration: 0
followup_review_recommended: false
context:
  - '/Users/aidan/dev/personal/tim/_bmad/custom/bmad-loop-protocol.md'
  - '/Users/aidan/dev/personal/tim/_bmad/custom/project-engineering-contract.md'
  - '/Users/aidan/dev/personal/tim/_bmad/custom/element-free-engine-design.md'
  - '/Users/aidan/dev/personal/tim/_bmad/custom/element-free-engine-review.md'
  - '/Users/aidan/dev/personal/tim/.anvil/worktrees/story-9a-46f/docs/testing-guidance.md'
warnings: [oversized]
deferred: []
---

<intent-contract>

## Intent

**Problem:** The preserved, unapproved Conveyor candidate infers a driven surface's root carrier and first hinge and selects contacts by body/normal. Authors cannot safely compose independent driven regions or rely on reciprocal work transfer when source and carrier bodies differ.

**Approach:** Incorporate only its necessary generic contact-law/declaration closure into published 9A.46e, replacing those assumptions with explicit carrier, collider-face and coordinate bindings and one reciprocal friction law. Prove unfamiliar compositions through the production native harness; leave the Conveyor package for 11.1b.

## Boundaries & Constraints

**Always:** FR-03/07, PERF-24/25/26 and applicable CAT-019 signed finite-inertia transport/energy behavior; D1–D7 and applicable E1–E9 below. Typed declarations, complete atomic admission, one current schema, f32 production laws, ordinary lifecycle, exact authored Reset and semantic save/load. Source rotation uses its actual joint frames. Run all required local tests with zero failures/skips and normal hooks. Confirmed review defects, including pre-existing defects, and every E9 violation require repair and independent re-review.

**Never:** Whole-candidate import, Conveyor package publication, speculative region/physics framework, arbitrary callbacks, root/first-joint/singleton defaults, unsupported-input fallbacks, migrations, aliases, copied-speed work, output hashes/byte equality, precision goldens, mutants or browser gates. Do not change palette/art. Powered-source actual-demand debit remains its named later owner; this slice cannot claim it. CI production build/artifact/deployment and browser qualification remain unverified.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
| --- | --- | --- | --- |
| Independent regions | Two carriers, explicitly selected nonfirst revolute sources | Each load follows its own signed driven face | No error |
| Selection boundary | Adjacent coplanar collider faces; rail, side, underside and spanning load | Only declared faces add tangential drive; ordinary contacts retain friction | No error |
| Passive coupling | Finite shaft inertia, cargo; stopped/unloaded/stalled/coasting | Work and reaction use actual shaft; no invented drive energy | No error |
| Two driven sides | Two admitted driven faces meet | Both source contributions enter one relative slip law | No error |
| Invalid binding | Unknown/out-of-range body, collider or coordinate; wrong coordinate kind; duplicate surface ID/face writer | No candidate partially installed | Typed bundle/edit/wire refusal; committed construction/revision/Undo preserved |
| Lifecycle | Run, Pause, Step, Resume, Reset, Save/Load | Correct committed progression and exact authored restoration; no stale coupling | Existing typed command/receipt semantics |

</intent-contract>

## Code Map

All relative deliverable/code paths below resolve under **/Users/aidan/dev/personal/tim/.anvil/worktrees/story-9a-46f**. This is the sole candidate checkout, branch `story-9a-46f-driven-surface-bindings`, originally clean at **648d067056293a4d045faf88e6937db38ad26dbf** (verified with git). Preserve this baseline through repairs. Published 9A.46e supplies `CoordinateReference`, explicit coordinate resolution, signed transmission and `ConstraintBodyImpulse`; it is the only technical story prerequisite. Planning owner: `/root/surface_next_draft`; retained implementation owner: `/root/surface_implementer`, launched fresh with `fork_turns=none` on 5 November 2026 after independent planning Scoped Pass. Return all repairs to this owner; planning author and implementation owner are not independent approvers.

Authoritative external policy inputs are the four absolute primary-checkout paths in frontmatter, read-only, plus primary `_bmad/custom/bmad-build-auto.toml` and `tools/anvil/VerifyPolicy.cs`. Primary HEAD observed **30eba561c09164a4bc8c0c8080b7b7fcbdc16760**; dirty Auto/tool deltas carry current loop hooks and E9 maintainability execution. These are policy inputs, not candidate changes to copy or publish. Relevant adopted policy: 5 November loop isolation/repair/final-scope rules and E9 whole-codebase CA1505 floor 20, effective for this slice. Recheck those inputs on resume; no hash manifest.

Native planning sources: primary rendered `/Users/aidan/dev/personal/tim/_bmad/render/bmad-build-auto/tim-9339f0454876/97d6bc6d80b8420b1d5c/step-02-plan.md`, `spec-template.md`, `workflow.md`. Deliverables remain in the isolated checkout, never the renderer's primary output destination. Coordinator owns effective-config preflight and independent planning audit before ready-for-dev.

Requirements: primary `_bmad-output/planning-artifacts/epics.md` lines 3896–3914 shared acceptance and 4006–4025 story; `docs/planning/requirements.md` PERF-24/25/26, CAT-019, anchors `todo-151`–`todo-153`. FR-03 is dedicated WASM SIMD physics; FR-07 is declaration-only catalogue parts. CAT-019 package visuals, chained catalogue delivery and teaching remain 11.1b obligations, not removed acceptance.

- `CuriousContraptions.Abstractions/PortDescriptor.cs`: published CoordinateReference/CoordinateKind; `ElementPresentation.cs`: PackageBodyIndex. Reuse explicit typed reference semantics.
- `CuriousContraptions.Abstractions.Mechanical/MechanicalContracts.cs`, `CuriousContraptions.Bundle/BundleSchema.cs`, `BundleValidator.cs`, `RunControlBindings.cs`: collection schema, exact bindings and complete validation. Contact-load RegionCollider is a precedent, not a root-only restriction to copy.
- `engine/gpu/CompiledIdentityMap.cs`: root/slider/hinge collider arrays and surface identities. `WorkshopPhysicsCompiler.cs`: ResolvePortBody numbering and coordinate resolution; compile explicit references without ordinal guesses.
- `engine/gpu/PhysicsDeclarations.cs`, `SupplyRoute.cs`, `WorkshopWireFlatBuffers.Compiled.cs`, `schemas/workshop_wire.fbs`, generated `engine/gpu/WorkshopWireGenerated.cs`: compiled rows, reference validation and transport. `WorkshopSimulation.cs`, `CuriousContraptions.Simulation/WorkshopGpuDevice.cs`: carry rows through construction/Reset.
- `CuriousContraptions.Simulation/WorkshopCollisionKernels.cs`: SimCollider.Id/Body and ContactPoint.ColA/ColB/Feature; feature numbers are narrowphase identities, not authored face IDs.
- `CuriousContraptions.Simulation/WorkshopFrictionSolver.cs`, new `SimDrivenSurface.cs`, `WorkshopPhysicsEngine.cs`: contact ownership, coupled wrench and resolved install. Existing `MechanicalTransmissionSolver.cs` aggregates contributions by actual body; `ConstraintBodyImpulse.cs` provides common application/velocity helpers.
- New `CuriousContraptions.tests/DrivenSurfaceBindingTests.cs` and necessary existing shared-native-harness/bundle/codec fixtures: unfamiliar production behavior and refusals. Do not import catalogue Conveyor tests as substitute evidence.
- Primary dirty files at the same paths are **read-only selective import sources**. Primary `elements/Conveyor/ConveyorElementDeclaration.cs` and `conveyor.tscn` show separate carrier/rail/idler/roller colliders; preserve them untouched. No Conveyor project registration, bundle entry or whole candidate copy.
- `ui/WorkshopConfiguration.cs`: format the new admitted metre-per-radian parameter unit as `m/rad`; native formatting controls retain source-speed/torque mappings and undefined-value refusal.
- Candidate `docs/engine-contracts.md`: current bounded surface contract; this spec records proof and status. The twelve additional E9 repair files and their exact symbol/coverage closure are recorded in Implementation Notes → Actual repair closure and behavioral mapping, under the same retained owner. `tools/e2e/workshop-wire.ts` and `run-reset-snapshot.ts` retain the current generated schema/listing without running deferred browser suites.

## Tasks & Acceptance

**Execution:**

1. `CuriousContraptions.Abstractions.Mechanical/MechanicalContracts.cs` and bundle files above — define collection rows and typed collider-face bindings; validate identities, ranges, ownership and conflicts before compilation. Avoid parameter reuse hazards: a run-controlled parameter cannot also feed compiled surface travel; refuse that use unless the current supported control contract explicitly updates it atomically.
2. `engine/gpu/CompiledIdentityMap.cs`, `WorkshopPhysicsCompiler.cs`, `PhysicsDeclarations.cs` — allocate demand-sized identities, resolve body-local collider/source references, validate compiled topology. Incorporate only missing generic hunks relative to 46e.
3. Wire/schema/device/simulation files above — carry the one current representation and reject malformed/incomplete rows atomically; regenerate wire output through the repository tool.
4. Simulation contact files above — classify face ownership before grouping; implement reciprocal per-body aggregated Jacobians for zero, one or two driven sides inside the existing friction law. Preserve ordinary friction and warm-start lifecycle.
5. `CuriousContraptions.tests/DrivenSurfaceBindingTests.cs` and affected boundary fixtures — implement the matrix and ACs through the production compiler/codecs/native harness; add only necessary mathematical helper tests.
6. `docs/engine-contracts.md` and this spec — update current contract, deletion/repair scope and actual evidence. Run E9 on the whole codebase, repair every listed type/member and add affected behavioral coverage before independent review. Implementation and matching proof are recorded below.

**Acceptance Criteria:**

- Given two unfamiliar surfaces on separately identified carriers with nonfirst driving joints, when Run and then reverse one coordinate, then only its contacting load reverses; the other retains its intended direction. Given reordered unrelated declarations and appropriately maintained explicit references, when rerun, then the same semantic routing holds without a core edit.
- Given a child carrier, rotated collider and source parent distinct from the carrier, when driven, then transport follows the declared face/tangent and actual source axis, not the root or carrier frame.
- Given stopped and unloaded controls differing only in contact/drive cause, when advanced, then stopped surfaces add no drive and unloaded shafts have no cargo reaction. Given contact load or a stall, when advanced, then reaction changes the selected shaft, not an unrelated joint.
- Given adjacent coplanar colliders, rails, side/underside contacts and a load spanning a selected/ordinary face boundary, when Run, then only selected-face contacts receive drive and ordinary contacts remain physically supported.
- Given two contacting driven faces, when sources co-drive or oppose, then relative transport follows both contributions; reversing only one source changes that contribution. Given shared contact/carrier/source bodies, when coupled, then reaction remains reciprocal without double-counting inertia.
- Given an unpowered finite-inertia coasting shaft with known initial kinetic energy and cargo, when friction transfers work, then cargo accelerates while the actual shaft loses energy; total kinetic plus potential energy cannot gain unprovided work within an independently justified gameplay energy allowance. A contact-free control separates legitimate gravity/drag effects. Local arithmetic checks independently derive Jacobian cross terms and impulse-work reciprocity, rather than comparing internal row layout.
- Given empty and multiple surface collections and representable growth, when compiled/run, then no rows disappear and unrelated parts retain behavior. Given zero or nonfinite travel, undefined face/kind, out-of-range reference, unsupported shape/prismatic source, mismatched collider owner, duplicate identity or same-face writer, when admitted at each applicable bundle/edit/compiled-wire boundary, then typed refusal preserves the installed construction, revision and Undo state.
- Given an admitted construction, when Run/Pause/Step/Resume/Reset and Save/Load into a fresh harness, then tick progression remains meaningful, authored Reset fields are exact, canonical bindings decode correctly and intended transport recurs; rejected replacements and prior-generation work cannot alter the installed world.
- Given the final candidate, when full local checks, normal hooks, all six independent Auto layers, affected repair review and final scope/simplification check complete, then no confirmed required-now finding remains. Given either E9 execution lists a violation, when triaged, then every listed symbol is a high repair in this slice with behavioral coverage and independent re-review; no approval until a final passing run.

## Spec Change Log

Implementation added the explicitly declared surface collection and reciprocal contact law. The baseline E9 gate expanded repair scope to the twelve named extractions below. Independent implementation review has not started.

## Review Triage Log

Coordinator reports current primary VerifyPolicy Auto preflight passed with zero structural findings; all six rendered review prompts are nonempty and unconditional with role boundaries and current E9/engine/loop hooks. This configuration evidence is not independent planning or runtime approval. Candidate TODO.md is also owned documentation scope: publication identity, current planning owner and next check only; preserve order/deferred links.

Planning Scoped Pass — /root/surface_planning_audit, fresh fork_turns=none, 5 November 2026, on original baseline 648d067056293a4d045faf88e6937db38ad26dbf and this draft including provisioning additions. Auditor independently inspected the eight design answers, E1–E9/D1–D7, source acceptance and production interfaces; no blocking design finding or missing decision. Coordinator rechecked readiness and advanced through ready-for-dev to implementation. This approves planning only. Subsequent implementation tests/E9 are recorded below; independent implementation review and publication remain pending.

## Design Notes

### Eight affected-design answers

1. **Behavior:** independent region-local load transport and reciprocal shaft work under the requirements above; retain current interactions and lifecycle.
2. **Reusable law:** revolute-driven uniform planar contact velocity, coupled through existing friction. Angular coordinate rad, angular velocity rad/s, signed travel m/rad, surface velocity m/s and contact impulse N·s. Valid travel is finite and nonzero; negative values reverse physical direction. Reject arithmetic-unrepresentable derived state atomically, not with product-specific bounds.
3. **Declared data:** typed local surface ID; PackageBodyIndex carrier; typed collider index local to that body's collider list; explicit box-face enum (six faces, canonical normal transformed by collider rotation); CoordinateReference of kind Revolute; carrier-local unit tangent; MechanicalValue travel. Geometry supplies finite footprint; face and tangent must be perpendicular. One writer per resolved collider face. Separate coplanar regions use separate colliders. Other shapes/coordinate kinds are typed unsupported input, not guessed approximations.
4. **Execution:** declaration → bundle/parameter validation → resolved carrier/collider/revolute rows → compiled wire validation → atomic runtime install → face-classified contacts → common coupled friction → committed physical reads. Physics remains sole authority; existing presentation can read committed poses. No new cosmetic evaluator or feedback path.
5. **Ownership:** simulation owns velocity, impulse and state; source joint frames own the coordinate axis; package owns geometry/direction/coefficients. Contact cache is rebuilt/invalidated by the existing construction/Reset lifecycle. Save/load uses existing authored canonical construction and current declarations; no parallel saved solver model.
6. **Generality:** unfamiliar multi-carrier/multi-source fixtures and every planned AC above. Expected signs follow declared displacement, reciprocal work follows the Jacobian below, and tolerances follow clearance/work consequences. Published 46e supplies reusable implementation, not executed proof of new surface behavior.
7. **Deleted:** never import singleton schema/identity, root carrier inference, first hinge selection, body-plus-normal first-match selection, carrier-derived source axis, source-B-only effective mass or silent unresolved-row omission from the preserved candidate. Replace all necessary imported callers at once; no old-format reader. Conveyor package stays preserved and unapproved outside this checkout.
8. **Uncertainty/prerequisites:** technical prerequisite 46e is now satisfied by the verified baseline. Design choices passed independent planning review. The retained implementation owner has implemented them and repaired all twelve baseline E9 violations; current verification and independent acceptance status are recorded below. No other story/framework prerequisite is introduced.

### Reciprocal contact contract

Let `a` be the source axis transformed by source joint A's rotation and local frame, `qdot = a·(omegaSourceB - omegaSourceA)`, `d` the carrier-transformed tangent, and `g = TravelPerRadian`. Physical added surface velocity is **g*qdot*d**, with no hidden minus sign. The unapproved Conveyor candidate's opposite convention must not be carried into this contract; its later package reconciliation must declare the appropriate sign.

For contact A/B tangent row t, each driven side contributes `s*g*(t·d)*qdot` to relative slip, with s=+1 on contact A and -1 on B. Ordinary row contributions: contact A linear t/angular rA×t, contact B linear -t/angular -rB×t. Source A adds angular `-s*g*(t·d)*a`, source B adds the opposite. Include both driven sides when present.

Aggregate contributions by actual body identity **before** mass, velocity and impulse evaluation:
`Kij = sum(invMass*Li·Lj + Ai·inverseInertia*Aj)`.
Use those same aggregated rows for slip and impulse application. This includes both source endpoints and shared-body cross terms. Existing torsional patch wrench remains; uniform translation adds no source term to that row. At most two contact bodies plus four source endpoints is a mathematical per-contact arity, not a scene population cap.

Reuse 46e's body aggregation and common impulse helpers. Do not blindly copy transmission normalization: friction bounds and warm-start impulses have physical units. Any normalization must transform the complete friction problem consistently, including capacity and cached impulses; ordinary physical-wrench units are the simplest implementation.

Classify contacts by exact collider identity and selected planar face using existing geometric contact tolerances, with explicit wrong-face/boundary controls. Do not serialize narrowphase Feature as authored identity. Group only contacts with the same driven-side ownership; a spanning load can participate in multiple patches. No first-match winner or tolerance-based duplicate writer admission.

### Engineering applicability

Sources for E rules are the explicit primary engineering-contract sections in frontmatter; linked architecture/engine/testing contracts retain authority.

| Rule | Applicability and planned proof |
| --- | --- |
| E1 | Applies: package isolation, typed declaration-only variation; unfamiliar fixture plus dependency inspection (PERF-24/26). |
| E2 | Applies: C# f32 contact/wire law, enum/ID validation, dedicated production cadence; matching compiler logs and native behavior (FR-03, PERF-25). No new clock owner. |
| E3 | Applies at compiled command/read boundary: preserve receipts/generation and atomic install; malformed/stale lifecycle controls. No ring redesign or new event consumer. |
| E4 | Applies: one schema, complete refusal and exact authored Reset; decoded codec and fresh-harness save/load tests. |
| E5 | Applies: demand-sized rows and forward deletion; empty/multiple/growth controls and no obsolete path. |
| E6 | Applies to selected collider geometry/physical truth; rotated/child/rail-face native controls. No art or palette change; UI proof deferred. |
| E7 | Applies: defined-interface positive/control/boundary/lifecycle proof; quantity-specific tolerances and one full local round. |
| E8 | Applies: one bounded slice, isolated candidate, fresh owner, independent six-layer Auto review, mandatory fixes/final simplification and publication without CI waiting. |
| E9 | Applies immediately: whole-codebase CA1505 floor 20, two required passing gate checkpoints and repair of every listed symbol; no suppression/exclusion/threshold weakening. |

D1–D6 govern exact references, composition, declaration variation, complete admission, one representation and reciprocal shared law. D7 is preserved: no new animation primitive/property writer; removing cosmetics cannot change motion/work. Any necessary affected observation changes must remain explicit committed bindings.

## Verification

**Verified provisioning (5 November 2026):** Node executable `/Users/aidan/dev/personal/tim/.anvil/toolchain/node-v26.12.1-darwin-arm64/bin/node` reports v26.12.1; prepend its containing bin directory to PATH for builds/tests/hooks. `/opt/homebrew/bin/dotnet` reports SDK 10.0.403 from this checkout; existing global.json selects 10.0.100 with latestFeature roll-forward. Workload list reports workload version 10.0.401 and wasm-tools manifest 10.0.112/10.0.100 (SDK 10.0.400 installation source). `/Users/aidan/dev/personal/tim/.anvil/toolchain/flatc-25.2.10/flatc --version` reports 25.2.10. The suggested executable `/opt/homebrew/bin/dotnet10.0.403` does not exist; use the verified `/opt/homebrew/bin/dotnet` path. These are provisioning observations, not compiler/static-gate or runtime proof; normal capture checks must establish compiler-log applicability.

**Publication sequence:** independent planning passed and this spec is in progress with retained owner `/root/surface_implementer`. The retained implementer is authorized to prepare the bounded local candidate commit through normal hooks after full-round success and retain its exact SHA for independent SnapshotApproval. The coordinator owns publishing only the independently approved candidate after required local gates and affected repair review. No push is authorized before that approval. Do not wait for CI; remote production build/artifact/deployment remain unverified unless independently established.

**Executor:** retained implementation owner `/root/surface_implementer` owns affected checks, both required E9 checkpoints, normal compiler captures and the single full solution round per verification round. Reviewers inspect matching evidence and run unresolved discriminating checks. Retained implementer `/root/surface_implementer` owns normal hooks and the local review-candidate commit; coordinator `/root` owns publication and final scope recording. Actual raw affected/E9 logs live in candidate `.anvil/`; the separately validated normal compiler/full-round evidence directory is `/Users/aidan/dev/personal/tim/.anvil/9a46f-normal-verification`, outside the isolated checkout. The earlier `/private/tmp` validation refusal remains recorded as historical provisioning evidence.

**Maintainability command (external primary tool, candidate target):**

`dotnet run --file /Users/aidan/dev/personal/tim/tools/anvil/VerifyPolicy.cs -- --maintainability --project-root /Users/aidan/dev/personal/tim/.anvil/worktrees/story-9a-46f`

Run before review handoff, and again on the actual final candidate after relevant repairs before final scope approval. The primary tool is read-only input because the published candidate tool may lack this new operation. Record source revision/delta applicability, candidate identity, violation count and raw output for each run. Its analyzer-injected builds are **not** normal verification builds. Run each gate before authoritative normal captures/full round, or recapture/rebuild affected normal outputs afterward. Gate FAIL is Fail; missing/refused/non-compiling execution is Incomplete. Every listed symbol is mandatory high repair with the same owner and independent re-review.

**Normal commands:** from the isolated checkout, use the exact commands in `docs/delivery-workflow.md#local-test-authority`: verify pinned SDK/workload and Node 26.12.1, produce five normal warnings-as-errors compiler logs, set CSHARP_STATIC_GATE_BINLOGS, derive only the explicit browser-deferral filter and run `dotnet test CuriousContraptions.slnx -c Debug -f net10.0 --no-build --no-restore` with that documented filter/configuration. Zero failures and Skipped: 0 in every required project. Fast affected tests reuse matching builds; normal pre-commit protections run separately and their totals are recorded separately. No local Release worker/web publish.

Choose scene assertions before observing results: direction/clearance, selected load/shaft, stable contact and energy transfer over a generous finite game-time window. Use existing quantity-specific allowances justified by those consequences, never a universal epsilon or incidental arrival tick. Exact IDs, enum values, canonical fields and authored Reset stay exact. No hashes, buffer identity, mutants, skips or precision reruns without a concrete uncovered failure.

Source/generated/dependency/configuration/compiler changes invalidate affected evidence; reuse unchanged applicable proof. After all confirmed repairs and independent affected re-review, record full-baseline final scope and simplification analysis here. Pending browser recipes cover selection/transport/Reset/save UI at the post-catalogue phase. Remote build/artifact/deployment and browser/release qualification remain unverified.


## Implementation Notes — retained owner /root/surface_implementer

The baseline E9 inventory ran before runtime edits, against 648d067056293a4d045faf88e6937db38ad26dbf plus owned planning documents. Command executor: /root/surface_implementer. Command: the external-primary maintainability command in Verification, with pinned Node PATH. Result: FAIL, 12 CA1505 violations; raw output: `.anvil/maintainability-baseline.log` in this candidate checkout. Planned /private/tmp logging was refused by Anvil as a workspace escape; the workspace-local output path was separately validated. No full local round has run. Analyzer-injected outputs are not normal compiler proof.

Mandatory high repair closure, preserving behavior and ownership:
- WorkshopPhysicsEngine.Advance (MI 0): extract existing phase operations; coverage: shared native simulation/integration suite and new driven-surface fixtures.
- PhysicsSceneDeclaration constructor (MI 4): extract admission families; coverage: declaration and compiled-wire boundary suites.
- ActivationNetwork.Consume (MI 6): extract event operations; coverage: existing signal/activation integration suites.
- BundleValidator.Signal (MI 7): extract capability validators; coverage: BundleElements.Tests.
- WorkshopPhysicsCompiler.Compile (MI 9): extract declaration compilation phases; coverage: production native compilation/integration.
- RevisionTwoOracles.Run (MI 12): extract independent wire scenarios; coverage: existing WireContract invocation.
- CapabilityInventory.Analyze (MI 14): extract inventory operations; coverage: existing coverage tooling tests.
- WorkshopWireFlatBuffers.EncodeResponse (MI 16): extract response encoding groups; coverage: codec and WireContract suites.
- WorkshopCollisionKernels.CollideCylinderCylinder (MI 17): extract geometric cases; coverage: existing cylinder collision/physics behavioral tests.
- WorkshopObservationProjection.Diagnostic (MI 17): extract diagnostic projections; coverage: existing observation/diagnostic tests.
- RuntimeClockControls.Run (MI 17): extract lifecycle scenarios; coverage: existing LifecycleContract invocation.
- SourceInventory.Inspect (MI 19): extract ownership inspection operations; coverage: existing ownership tooling tests.

All twelve repairs are implemented through behavior-preserving phase/scenario/validator extractions. The intermediate `.anvil/maintainability-repair.log` and pre-handoff `.anvil/maintainability-handoff.log` gates both report zero violations and PASS, including the below-threshold canary. Independent repair review remains pending. The final post-review gate remains mandatory on the actual final candidate. Analyzer-injected outputs are not normal proof.

### Full owned-diff self-check before the full local round

Executor `/root/surface_implementer` inspected every owned tracked diff and new file against original baseline `648d067056293a4d045faf88e6937db38ad26dbf`, including the twelve E9 closures, generated C#/TypeScript callers, schema, tests and documents. At this pre-commit inspection, no carried candidate commit existed. The primary policy-input HEAD remains `30eba561c09164a4bc8c0c8080b7b7fcbdc16760`; primary source/Conveyor work remains untouched.

The implementation uses one contact-wrench path for empty, one-sided and two-sided surfaces. The six-body scratch is mathematical contact arity, not population capacity. Construction-local compiler/tool builders only retain the locals required by their extracted phases; no service/framework or second behavior path was introduced. Removed the superseded ordinary-only wrench calculation, rather than retaining a mode branch. Exact source/collider resolution replaces all imported root/first-hinge assumptions. The self-check corrected frame-cache invalidation, required the current-schema collection instead of silently interpreting its omission as empty, and updated the existing generated TypeScript/schema listing. It also cleaned indentation on newly extracted bodies. No unrelated catalogue/art/palette changes or unnecessary abstraction remains identified. This is a self-check, not independent approval or the required post-review final scope verdict.

### Actual repair closure and behavioral mapping

| Original failing symbol | Owned file | Behavioral proof |
| --- | --- | --- |
| WorkshopPhysicsEngine.Advance | CuriousContraptions.Simulation/WorkshopPhysicsEngine.cs | New driven-surface native scenes; full native physics, activation, rope, electrical and lifecycle suites |
| PhysicsSceneDeclaration constructor | engine/gpu/PhysicsDeclarations.cs | New malformed compiled command/response refusals; existing WorkshopWireTests and declaration suites |
| ActivationNetwork.Consume | engine/gpu/ActivationNetwork.cs | Existing contact/timer/clock/counter/latch/indicator native suites |
| BundleValidator.Signal | CuriousContraptions.Bundle/BundleValidator.cs | Existing BundleElements.Tests and signal declaration/refusal tests |
| WorkshopPhysicsCompiler.Compile | engine/gpu/WorkshopPhysicsCompiler.cs | Every production-native scene compiles through the extracted phases; new unfamiliar source/carrier compositions |
| RevisionTwoOracles.Run | tools/WireContract/RevisionTwoOracles.cs | Existing tool executable: 108 checks, 0 failures (`.anvil/wire-oracles.log`); design-contract proof only |
| CapabilityAudit.Analyze | tools/Coverage/CapabilityInventory.cs | Existing tools/Coverage.Tests/CapabilityTests.cs positive/refusal inventory cases in the full round |
| WorkshopWireFlatBuffers.EncodeResponse | engine/gpu/WorkshopWireFlatBuffers.cs | Every native harness response, new malformed response cases, existing codec/read suites |
| WorkshopCollisionKernels.CollideCylinderCylinder | CuriousContraptions.Simulation/WorkshopCollisionKernels.cs | Existing WorkshopPhysicsLawCylinderTests in the full round; same contact formulas and emission order |
| WorkshopObservationProjection.Diagnostic | engine/gpu/WorkshopObservationProjection.cs | Existing observation/diagnostic interface tests in the full round |
| RuntimeClockControls.Run | tools/LifecycleContract/RuntimeClockControls.cs | Existing tool executable: 510 checks, 0 failures (`.anvil/lifecycle-oracles.log`), including current production clock controls |
| SourceInventory.Inspect | tools/Ownership/SourceInventory.cs | Existing `--oracles`: positive, 22 refusal cases, 22 semantic flows, 5 placements, portable-assembly inspection/control cases (`.anvil/ownership-oracles.log`); no global current ownership qualification |

### Command ownership, matching configuration and evidence

All commands below are executed for this candidate by retained owner `/root/surface_implementer`. The same executor owns normal hooks and the authorized local review-candidate commit. Parent `/root` owns publication and coordinates independent review. Commands run from the isolated checkout with Node 26.12.1 on PATH and SDK 10.0.403, in normal Debug unless the explicitly identified E9 analyzer configuration applies.

- Fast build: `dotnet build CuriousContraptions.slnx -c Debug --no-restore --nologo -v q -noAutoResponse` completed with 0 warnings/errors, 8.34 s. This supplied the matching binaries for the following affected checks.
- Native surface check: `dotnet test CuriousContraptions.tests/CuriousContraptions.tests.csproj -c Debug -f net10.0 --no-build --no-restore --filter FullyQualifiedName~DrivenSurfaceBindingTests --nologo`: **46 passed, 0 failed, Skipped: 0**, 2 s, raw `.anvil/surface-native.log`.
- Existing affected tool checks: `dotnet run --project tools/WireContract -c Debug --no-build --no-restore`; the analogous LifecycleContract command; and `dotnet run --project tools/Ownership -c Debug --no-build --no-restore -- --oracles`. All exit 0; raw outputs and behavioral limits are in the repair table. These tools are not xUnit projects; their explicit checks supplement the required full round.
- Pre-handoff E9: the exact external-primary command in Verification, raw `.anvil/maintainability-handoff.log`, exit 0, **violations=0, PASS**. It followed all runtime/schema/test source edits and preceded normal captures. No threshold, exclusion, suppression or analyzer policy changed.
- Normal compiler proof: the five exact commands from `docs/delivery-workflow.md#local-test-authority`, with `CSHARP_STATIC_GATE_BINLOGS=/Users/aidan/dev/personal/tim/.anvil/9a46f-normal-verification`. This separately validated location is outside the isolated checkout and avoids the rejected /private/tmp escape. Raw output `compiler-captures.log`; named `playtest.binlog`, `solution.binlog`, `web.binlog`, `check-staged-tests.binlog`, `verify-policy.binlog`. All five capture commands exited successfully; compiler invocation/provenance acceptance is evaluated by CSharpStaticGateTests in the required full round.
- Required full round: same executor and matching normal captures, `dotnet test CuriousContraptions.slnx -c Debug -f net10.0 --no-build --no-restore --nologo -noAutoResponse --filter "$local_deferred_filter" -- RunConfiguration.TreatNoTestsAsError=true`, where the filter is produced only by the existing E2eRunner `deferred-filter` command. Raw output will be `/Users/aidan/dev/personal/tim/.anvil/9a46f-normal-verification/full-solution.log`. The first run was deliberately interrupted after a concrete missing UI unit mapping was found; its raw log is retained, not accepted. Four tool projects completed (17 + 77 + 236 + 192 passes, each Skipped: 0); the main project had no terminal total. After the caller repair, `full-solution-final.log` was also interrupted when coordinator inspection found missing populated bundle JSON coverage; its four tool projects passed with the same totals, but the main project had no terminal total. Neither interrupted run is accepted. The complete candidate's authoritative round writes `full-solution-authoritative.log` after the populated-bundle repair and matching compiler refresh.

Relevant source/test/generated/schema, dependencies, SDK/analyzer or configuration edits invalidate affected evidence; documentation-only status updates do not. Final E9 reruns inject analyzer configuration and therefore require restoring matching normal outputs before any affected `--no-build` reuse.

### Native acceptance and limits

The 46 physical/boundary/lifecycle cases plus four parameter-unit formatting controls cover positive/negative source speed and stopped sources; explicit nonfirst source and reordered declarations; child carrier with rotated box and source frame; independent source reversal; adjacent ordinary faces, equal-support spanning load, side/underside controls and a blocking rail; two driven faces whose contributions cancel or add; unloaded/stalled/coasting source reaction, passive kinetic-plus-potential energy and a shared-body impulse-work mathematical derivation; empty/single/growing collections; bundle reference/kind/shape/identity/tangent/coefficient refusals; build-only versus run-controlled travel; malformed current command and response ownership/data/overflow/missing-collection refusal; continued operation after refusal; Pause/Step/Resume, exact authored Reset fields and fresh-harness semantic save/load transport.

Numerical scene oracles use 10 cm meaningful transport, 5 cm region/rest clearance, 0.2 m/s visible transport and 0.05 J (less than lifting 0.5 kg by 1.1 cm) unprovided-work allowance. The narrow Jacobian formula check uses 1e-5 for its explicitly derived scalar arithmetic contract. No hashes, buffer identity, trajectory/tick golden, mutants or tighter precision reruns were used. Fixture development corrected a coordinate-unit oracle, the expected Reset generation, an upper carrier's mode/footprint, and the physical expectation for a half-spanning load held by ordinary static friction; no solver change was made to fit those outcomes.

Browser UI construction/placement/configuration, pending game interaction recipes, detailed performance/device/resource qualification, remote production build/artifact/deployment and release qualification remain deferred/unverified. The generated deferred-browser codec/listing was kept current, but no browser suite was run. Powered-source actual-demand debit stays with its named later owner. Independent six-layer review, affected repair re-review, normal hooks and the final coordinator scope verdict remain pending.

### Late caller repair before independent handoff

Following the complete enum-consumer check, a valid `MetrePerRadian` parameter was found to reach the undefined-unit branch in `Workshop.ParameterUnits`. The retained owner added the canonical `m/rad` mapping and four native formatter cases covering the new unit, existing source-speed/torque units and unknown-value refusal. This is a required caller closure, not a new UI feature. The formatter is internal for its narrow interface test; configuration uses the same method. The coordinator directed interruption of the unfinished earlier full round to avoid completing stale-candidate proof.

The caller-repaired candidate passed E9 with zero violations (`.anvil/maintainability-unit-repair.log`) and refreshed `playtest.binlog`, `solution.binlog`, `web.binlog` in the existing normal evidence directory; unchanged file-tool compiler captures remain applicable. Raw refresh output is `compiler-captures-unit-repair.log`. Executor remains `/root/surface_implementer`; runtime surface/oracle evidence remains applicable because those relevant inputs did not change. No accepted full-round or final handoff is claimed yet.

### Populated bundle boundary repair

Coordinator matrix inspection identified missing populated `BundleSchema.WriteEntries` → `PartRegistry.Read` proof. `PopulatedBundleRoundTripPreservesSignedBindingsAndRunsDecodedDeclarations` serializes two rows with nondefault local identities, distinct collider/source/carrier references, faces and signed coefficients. It asserts every decoded semantic field, then compiles and runs the decoded declarations and checks negative cargo transport and supported height. No byte-identity oracle is used. The matching affected Debug build and 51-case native check passed (0 failures, Skipped: 0); raw `.anvil/surface-bundle-boundary.log`. This includes the four formatter controls. Executor remains `/root/surface_implementer`.

The complete owned source diff was inspected before full-round handoff; the final boundary repair changes tests only. Final pre-handoff E9 passed with zero violations and a passing canary, raw `.anvil/maintainability-bundle-boundary.log`; the normal compiler refresh passed all three builds with zero warnings/errors and uses `compiler-captures-bundle-boundary.log` and the same three applicable binlog paths. The two unchanged file-tool captures remain applicable. The authoritative full round completed successfully: game 5,187, Coverage 77, BundleElements 236, WorkerAotGuard 17 and E2eRunner 192, total **5,709 passed, 0 failed, Skipped: 0**, exit 0. Main test duration was **22 m 32 s**. All five terminal results are in `full-solution-authoritative.log`; matching compiler provenance tests passed within that round. Relevant test source changes invalidate the earlier matching solution captures; unchanged runtime/oracle evidence remains applicable.

### Local review-candidate handoff

The full local round and final pre-handoff E9 are complete with no failures/skips or maintainability violations. The coordinator authorized the retained owner to stage only the isolated checkout's owned changes and prepare a local review-candidate commit through normal hooks. Raw hook/commit evidence: `.anvil/review-candidate-commit.log`. The commit's own canonical SHA is reported in the handoff rather than self-embedded. No push occurs before independent SnapshotApproval. All six implementation review layers, affected repair re-review if needed, post-review E9 and final scope verdict remain pending; no implementation approval is claimed.
