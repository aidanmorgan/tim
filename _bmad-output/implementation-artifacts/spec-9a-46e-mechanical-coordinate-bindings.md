---
title: 'Story 9A.46e: Connect mechanical ports to declared coordinates'
type: 'feature'
created: '2026-11-05'
baseline_revision: c36abf99a3051811ac8de6ac75dfe9bf262d6a85
status: 'in-review'
review_loop_iteration: 0
followup_review_recommended: false
context:
  - '{project-root}/docs/testing-guidance.md'
  - '{project-root}/docs/architecture/element-package-isolation.md'
  - '/Users/aidan/dev/personal/tim/_bmad/custom/element-free-engine-design.md'
  - '/Users/aidan/dev/personal/tim/_bmad/custom/element-free-engine-review.md'
  - '/Users/aidan/dev/personal/tim/_bmad-output/implementation-artifacts/review-engine-abstraction-2026-11-04.md'
warnings:
  - 'Oversized spec: exceeds the 1600-token template threshold; retained original contract and required bounded recovery evidence.'
deferred: []
---

<intent-contract>

## Intent

**Problem:** Mechanical ports currently lack a typed coordinate binding independently of port body/locator. In the Conveyor candidate, mechanical ports inferred revolute coordinates from body index with root meaning first hinge (`sourcePortDesc.Body.Value > 0 ? sourceHinges[sourcePortDesc.Body.Value - 1 - identities.Sliders.Length].Joint : sourceHinges[0].Joint`). This arithmetic couples port locator body index to hinge array index, assumes hinges always follow sliders, conflates root locator with the first hinge, and crashes/corrupts when referencing an element with sliders or no hinges (Findings A10, E5).

**Approach:**
1. Starting from published main (`c36abf99`), incorporate only the necessary generic transmission declarations, solver, wire, and caller closure from the preserved uncommitted Conveyor candidate (`SimMechanicalTransmission`, `MechanicalTransmissionSolver`, `MechanicalTransmissionDeclaration`, `MechanicalTransmissionRecord` in FlatBuffers schema and wire); review this imported code as new code under this story with unfamiliar declaration fixtures and NO Conveyor package publication.
2. Introduce strongly typed coordinate references:
   - Introduce `CoordinateKind` enum (`Revolute = 1`, `Prismatic = 2`).
   - Introduce `CoordinateReference` struct (`CoordinateKind Kind`, `int Index`).
   - Add `CoordinateReference? Coordinate` to `PortDescriptor` in `CuriousContraptions.Abstractions`.
3. Separate port coordinate reference from locator body index:
   - Port `Body` remains the anchor frame for `LocalPosition` in scene rendering.
   - Port `Coordinate` explicitly identifies the physical joint coordinate independently of body or locator hierarchy.
4. Typed admission and validation:
   - `ElementPortCatalogue.Admit` admits `SimulationDomain.Mechanical`, ensuring `PortRole.Drive` (Output) and `PortRole.DriveIn` (Input) require a declared `CoordinateReference` and finite non-zero `CoordinatePerRadian`.
   - `BundleValidator` checks that mechanical ports declare a supported `CoordinateKind.Revolute` pointing to an existing hinge (`0 <= Index < entry.Mechanical.Hinges.Count`). Unsupported slider coordinates (`CoordinateKind.Prismatic`) or missing/out-of-range joints refuse atomically.
5. Connection validation & compilation:
   - `WorkshopConnections.Validate` checks mechanical domain rules (Output Drive -> Input DriveIn, no self-connections, no duplicate drives into the same input, valid coordinate references).
   - `WorkshopPhysicsCompiler` compiles mechanical connections into `MechanicalTransmissionDeclaration` by resolving exact joint identities directly from declared `CoordinateReference` indices without body arithmetic or first-hinge assumptions.
6. Execution:
   - `WorkshopPhysicsEngine` executes `MechanicalTransmissionSolver` in `Prepare`, `WarmStart`, and `Solve`, applying impulses and transmitting motion across joints according to the ratio.

## Boundaries & Constraints

**Always:**
- Strongly typed coordinate references via `CoordinateReference(CoordinateKind, int)`.
- Mechanical ports must declare a valid `CoordinateReference` and non-zero finite `CoordinatePerRadian`.
- Incompatible coordinate kinds (e.g. `CoordinateKind.Prismatic` / slider) refuse atomically during admission.
- Out-of-bounds joint indices or mechanical ports on elements without hinges refuse atomically during admission.
- Delete all body-index-to-hinge arithmetic and root-means-first-hinge conventions.
- No Conveyor package publication in this story (Story 11.1b remains the Conveyor publication owner).
- Zero skipped tests (`Skipped: 0`), no hash/byte assertions, and no mutation testing before the post-catalogue E2E phase.

**Never:**
- No body-index-to-hinge arithmetic (`Body.Value - 1 - Sliders.Length`).
- No implicit first-hinge fallback for root-attached ports.
- No backwards-compatibility shims, legacy socket aliases, or automatic migration.
- No browser E2E execution during this slice (remains deferred per `BrowserDeferral.cs`).

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Second hinge endpoint on root locator | Mechanical port with `Body = default`, `Coordinate = (Revolute, 1)` on element with 1 slider and 2 hinges | Compiles transmission targeting the second hinge (`Hinges[1]`) correctly | Validated clean |
| Declaration reordering | Slider and hinge declarations reordered in package | Target hinge coordinate stays exact and unchanged | Validated clean |
| Unsupported slider coordinate | Mechanical port declared with `Coordinate = (Prismatic, 0)` | Refuses atomically at admission before Run | Throws `BundleValidationException` / `ElementPortRejectedException` |
| Missing joint reference | Mechanical port declared with `Coordinate = (Revolute, 5)` on element with 1 hinge | Refuses atomically at admission before Run | Throws `BundleValidationException` |
| Base body without hinge | Mechanical port declared on element with 0 hinges | Refuses atomically at admission before Run | Throws `BundleValidationException` |
| Duplicate drive inputs | Multiple connections targeting the same `DriveIn` port | Connection rejected before compilation | `WorkshopEditRejection.InvalidConnection` |
| Positive ratio transmission | Driving joint connected to driven joint with ratio 2.0 | Driving rotation produces proportional driven rotation | Verified via physics engine simulation ticks |
| Save/load roundtrip | Mechanical connection in construction | Save to JSON and reload preserves exact endpoints and transmission ratio | Roundtrips identically |

</intent-contract>

## Code Map

- `CuriousContraptions.Abstractions/PortDescriptor.cs`: Add `CoordinateKind` and `CoordinateReference`. Update `PortDescriptor` constructor.
- `CuriousContraptions.Bundle/BundleValidator.cs`: Validate mechanical ports, coordinate kinds, and joint existence.
- `CuriousContraptions.Simulation/MechanicalTransmissionSolver.cs`: Reusable constraint solver for mechanical transmissions.
- `CuriousContraptions.Simulation/WorkshopPhysicsEngine.cs`: Prepare, warmstart, and solve mechanical transmissions.
- `CuriousContraptions.Core/RuntimeSources.props`: Add `MechanicalTransmissionSolver.cs` to runtime sources.
- `engine/gpu/PhysicsDeclarations.cs`: Declare `MechanicalTransmissionDeclaration`, `MechanicalTransmissionId`, and add to `CompiledPhysicsScene`.
- `engine/gpu/ElementPortCatalogue.cs`: Support `SimulationDomain.Mechanical`, validate coordinate and role agreements.
- `engine/gpu/WorkshopConnections.cs`: Validate mechanical domain connections.
- `engine/gpu/WorkshopPhysicsCompiler.cs`: Compile mechanical connections into transmissions via declared coordinates.
- `engine/gpu/WorkshopWireFlatBuffers.Compiled.cs`: Wire FlatBuffers encoding/decoding of transmissions.
- `schemas/workshop_wire.fbs` & `engine/gpu/WorkshopWireGenerated.cs`: `MechanicalTransmissionRecord` FlatBuffers table.
- `CuriousContraptions.tests/`: Add tests in `MechanicalCoordinateBindingsTests.cs` verifying coordinate resolution, controls, and transmission physics.

## Tasks & Acceptance

**Execution:**
- Add `CoordinateKind` and `CoordinateReference` to `CuriousContraptions.Abstractions`.
- Update `PortDescriptor` to include `CoordinateReference? Coordinate`.
- Implement `MechanicalTransmissionSolver.cs` in `CuriousContraptions.Simulation/` and add to `RuntimeSources.props`.
- Update `PhysicsDeclarations.cs` to include `MechanicalTransmissionDeclaration` in `CompiledPhysicsScene`.
- Update `schemas/workshop_wire.fbs`, `WorkshopWireGenerated.cs`, and `WorkshopWireFlatBuffers.Compiled.cs` for `MechanicalTransmissionRecord`.
- Update `WorkshopPhysicsEngine.cs` to solve transmissions during simulation substeps.
- Update `ElementPortCatalogue.cs` and `BundleValidator.cs` to validate mechanical ports and coordinate bindings.
- Update `WorkshopConnections.cs` and `WorkshopPhysicsCompiler.cs` to compile mechanical connections directly from declared coordinates.
- Add unit and integration tests in `CuriousContraptions.tests/MechanicalCoordinateBindingsTests.cs`.
- Run full verification suite (`local-test-authority`).

**Acceptance Criteria:**
- Given a mechanical endpoint on the second hinge, a root locator and a mixed slider/hinge package, when reorder unrelated declarations and compile connections, then the selected coordinate stays correct; unsupported slider/no-joint endpoints refuse before Run mutation.
- And prove positive ratio/sign transmission, wrong kind, missing joint and base-body-without-hinge controls.
- And body-index-to-hinge arithmetic and root-means-first-hinge convention in the Conveyor candidate are deleted, with current callers, packages and serialized contracts updated together. Preserve existing intended gameplay and authored Reset state; do not close the story on renaming alone.

## Implementation Notes

- Replacement owner inspected the full carried diff before runtime changes. Removed dead warmstart/accumulator state, combined shared-body Jacobians, corrected ratio to source/target and moved topology out of per-tick controls into atomic transport admission/commit. Generated C# regeneration removed six spurious response builders; matching TS generation exposed stale `interaction_target` authoring output and corrected it without a schema change.
- Early scoped integration setup failed first on construction revision, then on copied Motor lamp bindings attached to an unpowered unfamiliar follower. Reassessment: these are fixture contract defects before physics, not numeric solver failures. Correct fixture revision and remove the borrowed supplied-drive lamp binding from the passive fixture; keep the production presentation harness active. No gameplay bound or solver complexity changes are justified by these failures.

## Spec Change Log

## Review Triage Log

### 2026-11-05 — Auto implementation review, candidate 251ce8692443529546cd5d8963b7963b5eaec0b6

All six layers ran with `fork_turns=none`: `/root/coordinate_blind_review`, `/root/coordinate_edge_review`, `/root/coordinate_verification_review`, `/root/coordinate_intent_review`, `/root/coordinate_forward_review`, `/root/coordinate_engineering_review`. Product/full inputs are retained under `.anvil/review-251ce869/`. Blind Hunter and Edge Case Hunter returned no findings; Forward-Only found no violation. Engineering acceptance is Fail until the confirmed storage defect is repaired and independently re-reviewed. No publication approval.

- verdicts: 8 reported findings/descriptive divergences — high 1, medium 1, low 0, false 6, maybe-false 0.
- `[medium] [patch]` Verification Gap: populated mechanical coordinates bypass the bundle JSON boundary in current tests. Existing discovered packages have null coordinates; scoped fixtures install entries directly. Add unfamiliar mixed-package `BundleSchema.WriteEntries` → `PartRegistry.Read` proof, preserving nonzero joint index, kind and signed scale and compiling decoded entries to the expected coordinate/ratio. Return to retained owner.
- `[false] [reject]` Intent cardinality observation: both endpoint-role limits are required by the source mechanical bilateral 1-to-1 contract (`docs/engine-contracts.md`, Multi-Domain Port Compatibility); distinct aliases cannot evade that law. Input-only wording in the initial matrix did not waive the source contract.
- `[false] [reject]` Intent hinge-order observation: source Story 9A.46e requires reordering unrelated declarations; local `(Kind, Index)` explicitly selects the current hinge table, as the independently approved design states. The new harness changes slider count and port/instance ordering while preserving selected-hinge behavior. No promise of persistent identity across an unchanged index into a permuted hinge table is made.
- `[false] [reject]` Intent WarmStart observation: the source story requires proportional bilateral motion, not a named no-op optimization stage. The approved recovery explicitly removes dead warmstart/accumulator state; Prepare/Solve implement the sole law and lifecycle proof remains present.
- `[false] [reject]` Intent JSON-save observation: the existing production save is FlatBuffers; owner one-current-schema/forward-only authority controls the initial matrix's inaccurate JSON label. Save acceptance means canonical decoded construction fields and replay using the existing `WorkshopSaveCodec`, not adding another format. Bundle JSON is a separate real boundary covered by the confirmed verification repair above.
- `[false] [reject]` Intent ratio-2 observation: source acceptance requires positive ratio/sign transmission, not an exclusive numeric value. Package-to-simulation tests prove 0.5 and -1; the production law fixture proves 2 and other signs with independently derived expectations. Both required surfaces are exercised.
- `[false] [reject]` Intent generated-TS observation: `schemas/workshop_authoring.fbs:68` already declares `interaction_target`; pinned regeneration repairs stale generated output without adding a schema or product behavior. Search found no external `createPuzzleRecord` caller requiring repair. Keeping the stale output would violate the single current schema contract.
- `[high] [patch]` Engineering E5/D5/PERF-25: `WorkshopPhysicsEngine.Advance` clears and recreates each transmission object and both scratch arrays every tick, and resolves unchanged endpoint identities repeatedly. Retain admitted transmission rows/scratch and resolve topology once for its epoch; preserve atomic candidate/committed ownership and Reset/replacement behavior. Source inspection plus affected production motion/lifecycle checks discriminate this repair; no profiling/stress programme is added. Return to retained owner, then independent affected re-review.

Affected re-review — frozen repair delta from 251ce869: `/root/coordinate_verification_review` confirms the bundle JSON gap resolved through source/assertion inspection, no new supported verification gap. `/root/coordinate_engineering_review` returns **Affected Scoped Pass (source/targeted-proof phase)**: retained rows/scratch, once-per-epoch resolution, live pool state refresh, Reset and atomic replacement ownership satisfy the confirmed E5/D5/PERF-25 repair. Engineering inspected matching 42/0/0 and zero-warning/error build evidence. Neither verdict substitutes for the final compiler/full-round, normal hooks or exact committed-SHA approval. Coordinator released the single final full round after these checks.

Repair verdict: two confirmed repairs, no deferred defect. Original baseline and implementation owner remain unchanged. All unchanged review evidence remains reusable only after affected re-review verifies applicability.

- 2026-11-05: Planning Scoped Pass from independent `/root/coordinate_planning_audit` (`fork_turns=none`) after both delimiter and cycle-admission findings were corrected and independently re-reviewed. Applies to recovered design only; runtime acceptance pending. Coordinator verified ready-for-development criteria before transitioning to implementation. Fresh replacement owner will be recorded on launch; original baseline remains unchanged. The replacement implementer is designated executor of affected checks and the one final full local round, with matching raw compiler/test logs; coordinator reuses those results rather than repeating the round. Normal hooks remain separately required.

Planning audit returned Incomplete with two findings. Both are corrected in this draft for the same independent auditor's re-review: (1) the recovered closing intent delimiter now follows the I/O matrix, protecting the original intent, boundaries and matrix without changing their words; (2) removed undefined inconsistent-cycle admission/refusal and replaced it with the same bilateral law for chains/shared carriers/closed compositions, explicit existing typed refusal categories and bounded redundancy/locking controls. Rest always satisfies the homogeneous travel relation; physical locking is not an invalid topology. No cycle classifier or cycle ban is required. Coordinator verified Story 9A.46d's published c36abf99 receipt/status as prerequisite evidence only. Status remains draft; no planning Pass is claimed.

## Verification

### Implementation ownership

Replacement implementation owner: `/root/coordinate_implementer`, launched without inherited history after planning Scoped Pass. Original baseline remains `c36abf99a3051811ac8de6ac75dfe9bf262d6a85`. This owner executes affected incremental Debug builds/checks and the single final full local round; normal hooks remain separate. Raw logs will accompany executed commands below. Runtime/schema/generated-input or dependency/configuration changes invalidate the matching affected build/test evidence; policy-only receipt changes do not.

Initial full owned diff inspection found the planned repair closure: per-tick transmission topology, silent unresolved-joint omission, dead warmstart state, inverted scale quotient, missing endpoint cardinality, generated schema drift and duplicated integration orchestration. These are being repaired within this candidate; no inherited runtime proof is claimed.

### Current verification execution

Executor for every command below: `/root/coordinate_implementer`. Matching configuration is Debug/net10.0, .NET SDK 10.0.403 selected by unchanged global.json, exact Node v26.12.1 supplied externally. Incremental affected builds succeeded with zero warnings/errors. Last focused run: `dotnet test CuriousContraptions.tests/CuriousContraptions.tests.csproj -c Debug -f net10.0 --no-build --no-restore --nologo --filter FullyQualifiedName~MechanicalCoordinateBindingsTests`: **41 passed, 0 failed, Skipped: 0**, about one second (tool session 96961; build session 9505). Earlier fixture failures are described above; none changed numerical acceptance.

Full owned baseline-to-worktree scope/simplification inspection completed before the full round: kept generic declaration/admission/compiler/wire/solver/transport closure and required tests/schema companions; removed dead solver identity/warmstart state, obsolete per-tick topology fields and formatting-only simulation changes. Shared LawScene and FlatBuffers boundary helpers are reused; the duplicate Rig is deleted. No Conveyor package or primary-workspace runtime input is imported. This is implementation self-inspection, not independent approval.

Required full compiler/test capture completed at `/private/tmp/tim-verify-9a46e-owned`; the five compiler logs and raw command stdout there are the review evidence. Source/schema/generated inputs, dependencies, compiler environment or relevant configuration changes invalidate matching evidence. Browser, mutation, Release worker publish and remote qualification remain unexecuted.

### Review repair execution — 5 November 2026

Same owner `/root/coordinate_implementer`, same baseline. Both confirmed findings repaired in the current delta from 251ce869: bundle JSON case uses `BundleSchema.WriteEntries` → `PartRegistry.Read`, asserts decoded revolute kind/index 1 and signed scale -2, then compiles decoded mixed packages to the exact selected joint and ratio -0.5. First execution exposed fixture-only duplicate palette order and missing declared presentation bodies; corrected those inputs without changing product behavior or acceptance.

Transmission rows now retain their two scratch arrays and references to stable revolute pool objects for the installed epoch. Identity mapping is built only on the first execution of that epoch and then released; per-tick joint/body state still refreshes in those objects. Reset clears the rows and resolution flag alongside the pools. Candidate installation/replacement retains existing isolated engine ownership; no alternate solver path or cache platform is added.

Early full owned-diff and repair-delta inspection: production repair is confined to WorkshopPhysicsEngine; MechanicalTransmissionSolver requires no change because its retained rows already overwrite active scratch on Prepare. New test stays in the existing fixture class. No numerical bounds changed. Matching incremental Debug solution build: zero warnings/errors (`.anvil/repair-build-2.txt`); affected test command uses `--no-build --no-restore -c Debug -f net10.0 --filter FullyQualifiedName~MechanicalCoordinateBindingsTests`: **42 passed, 0 failed, Skipped: 0** (`.anvil/repair-focused-2.txt`). Initial fixture failure retained in `.anvil/repair-focused.txt`. All paths are relative to the isolated checkout.

Runtime/test source changes invalidated affected original compiler/full-round proof. Fresh five matching captures completed with zero warnings/errors in `/private/tmp/tim-isolated-9a46e/.anvil/repair-verification`: playtest.binlog, solution.binlog, web.binlog, check-staged-tests.binlog and verify-policy.binlog, plus corresponding stdout. Same Debug/net10.0 compiler block and environment as the original receipt; exact executables, SDK and workload output are retained in executables.txt, sdk.txt and workloads.txt.

After both affected reviewers returned their scoped verdicts, the designated owner executed the authoritative full solution command once for this repair round (`--no-build --no-restore`, existing browser-only deferred-filter, TreatNoTestsAsError=true; session 30337). **5658 passed, 0 failed, Skipped: 0**: main game 5136 (22m16s), WorkerAotGuard 17, Coverage 77, BundleElements 236, E2eRunner 192. Raw result: `.anvil/repair-verification/full-tests.txt`. Original full/compiler/hook evidence remains separate and unchanged.

Relevant runtime/test inputs are exactly the frozen repair delta independently inspected by /root/coordinate_verification_review and /root/coordinate_engineering_review; no code, generated input, dependencies or configuration changed during or after this round. Only native triage, verification and TODO/sprint status receipts were updated. Normal hooks will run through the local repair commit with distinct `.anvil/repair-commit-hooks-9a46e.txt` stdout; coordinator records the resulting SHA and hook outcome at final acceptance. Status remains in-review, not done; no push or publication approval. Browser, remote and performance qualification remain unverified.

### Full-round receipt — 5 November 2026

Executor: `/root/coordinate_implementer`; checkout `/private/tmp/tim-isolated-9a46e`; original baseline retained. Five matching compiler captures completed with zero warnings/errors: `playtest.binlog`, `solution.binlog`, `web.binlog`, `check-staged-tests.binlog`, `verify-policy.binlog`, each with corresponding .txt stdout under `/private/tmp/tim-verify-9a46e-owned`. SDK/workload evidence: `sdk.txt`, `workloads.txt`; selected dotnet `/opt/homebrew/bin/dotnet` 10.0.403, workload set 10.0.401 / wasm-tools 10.0.112/10.0.100. Exact Node is `/Users/aidan/dev/personal/tim/.anvil/toolchain/node-v26.12.1-darwin-arm64/bin/node` v26.12.1. Pinned flatc 25.2.10 regenerated schema companions.

Executed the authoritative local verification block: restore; Debug solution compiler capture with PlaytestDiagnostics=true; normal Debug solution capture; Debug web capture with WasmBuildOnlyAfterPublish=true/TwoDogAutoImport=false; CheckStagedTests and VerifyPolicy compiler captures; then `dotnet test CuriousContraptions.slnx -c Debug -f net10.0 --no-build --no-restore --nologo -noAutoResponse --filter "$local_deferred_filter" -- RunConfiguration.TreatNoTestsAsError=true`. Filter came from the existing E2eRunner `deferred-filter` command and applies only the explicit browser deferral.

Raw full result: `/private/tmp/tim-verify-9a46e-owned/full-tests.txt`. Main game **5135 passed, 0 failed, Skipped: 0**, 22m33s; WorkerAotGuard 17, Coverage 77, BundleElements 236, E2eRunner 192; **total 5657 passed, 0 failed, Skipped: 0**. Full round ran once. An initial shell-selection attempt selected SDK9 and stopped before restore/build/tests; corrected shell selected the pinned environment. No tests repeated for that setup failure. One coordinator-authorized read-only process sample during quiet output (`main-process-sample.txt`) had unsymbolicated managed frames; it established no defect and triggered no changes.

Owned scope expansion: refreshed isolated TODO.md and sprint-status.yaml to the actual 46d publication, current 46e review and owner-prioritized 46f then g–m before Conveyor. Preserved Conveyor's run-control obligation and existing backlog/deferred gates. These status-only changes do not invalidate runtime/compiler evidence. Normal hooks will execute through the local candidate commit; six-layer independent acceptance and publication remain pending. Browser, device/performance and remote qualification remain unverified. No known runtime failure remains; independent review owns the blocking verdict.

**Commands:**
- `dotnet build CuriousContraptions.slnx -c Debug --nologo` -- expected: 0 warnings, 0 errors.
- `dotnet test CuriousContraptions.tests/CuriousContraptions.tests.csproj -c Debug --nologo --filter "FullyQualifiedName~MechanicalCoordinateBindingsTests"` -- expected: all pass, 0 failed, Skipped: 0.
- Run full verification block from `docs/delivery-workflow.md#local-test-authority` -- expected: all 5 test projects pass with Skipped: 0.
- Run `.githooks/pre-commit` on staged candidate -- expected: exit 0, Anvil clean.

## Draft recovery — 5 November 2026 (historical planning reconstruction)

The draft/pending wording below records the recovery checkpoint. It is superseded by the planning Scoped Pass in Review Triage Log and current Implementation ownership above; `/root/coordinate_implementer` is the designated full-round executor.

This is reconstructed planning, not independent approval or executed verification. Status is draft pending a bounded independent engineering planning audit and validation of the settled design and prerequisite evidence below. Preserve the immutable intent above; the original missing closing delimiter is repaired after the I/O matrix, immediately before Code Map, preserving Intent, Boundaries & Constraints and the matrix inside the protected contract.

### Candidate, ownership and KEEP constraints

Original baseline and isolated HEAD are `c36abf99a3051811ac8de6ac75dfe9bf262d6a85`; checkout is `/private/tmp/tim-isolated-9a46e`. The candidate is the complete baseline-to-worktree delta, including index and working tree, not the last repair. At recovery it contains 22 staged files: the existing Code Map closure plus BundleSchema.cs, RevoluteConstraintSolver.cs, WorkshopGpuDevice.cs, SupplyRoute.cs, WorkshopSimulation.cs, the test project include, and the two generated/schema companions tools/e2e/workshop-wire.ts and tools/e2e/run-reset-snapshot.ts. No candidate commits beyond baseline or untracked story artifacts were found. Primary-workspace dirty work, including Conveyor, is excluded.

The prior spec records no implementation-owner agent ID or raw execution evidence; its Verification section lists expectations only. The coordinator reports no live prior local implementation process. Deliberate replacement is authorized only after this reconstruction and planning approval; the coordinator must record the fresh replacement agent ID before implementation and retain it for repairs. Reconstruction owner: /root/coordinate_plan_recovery; this role is not the implementation owner or independent auditor.

KEEP: exact original baseline; explicit coordinate independent of locator Body; supported revolute relation and typed rejection of unsupported kinds; no Conveyor publication or new transmission law; current schema/caller consistency; existing intended gameplay and exact authored Reset; semantic saves/wire; no compatibility path, browser gate, mutants, hashes or skipped native tests. Preserve useful candidate work, but none of its runtime results is approved or grandfathered.

Applicable current policy is loaded from the primary checkout, because the isolated baseline does not contain all current guides:
- /Users/aidan/dev/personal/tim/AGENTS.md
- /Users/aidan/dev/personal/tim/_bmad/custom/bmad-loop-protocol.md
- /Users/aidan/dev/personal/tim/_bmad/custom/project-engineering-contract.md
- /Users/aidan/dev/personal/tim/_bmad/custom/element-free-engine-design.md
- /Users/aidan/dev/personal/tim/_bmad/custom/element-free-engine-review.md
- /Users/aidan/dev/personal/tim/docs/testing-guidance.md

These are current working-tree policy inputs, not an assertion that baseline contains them. Policy checkout HEAD is 30eba561c09164a4bc8c0c8080b7b7fcbdc16760. Read-only current policy inputs include the uncommitted AGENTS.md and docs/delivery-workflow.md changes, _bmad/custom/bmad-loop-protocol.md, project-engineering-contract.md, element-free-engine-design.md, element-free-engine-review.md, and bmad-build-auto.toml/config.toml overrides. These policy inputs are not owned runtime candidate or publication changes; the effective Auto configuration receipt is recorded below. Story authority is /Users/aidan/dev/personal/tim/_bmad-output/planning-artifacts/epics.md, lines 3988–4007. Story 9A.46d is its explicit prerequisite: the coordinator verified its published completion receipt/status at c36abf99. This satisfies prerequisite continuity and is not reused as 9A.46e implementation proof. No sibling loading is authorized merely by adjacency.

### Eight design answers

1. **Behavior:** authors connect an explicitly selected revolute joint even when its locator is root-mounted and its package mixes sliders and hinges. Retain FR-01/03/07/09, CAT-019/042, PERF-24/25/26 and story shared acceptance; the coordinator/auditor must resolve the affected source clauses before approval.
2. **Reusable primitive:** a bilateral revolute transmission between exact compiled joint IDs, with signed finite nonzero dimensionless velocity ratio; joint coordinates are relative angular coordinates in radians and speeds rad/s. No element identity selects the law. Define endpoint travel q = c × (theta - theta_initial), where c is signed endpoint-coordinate units/radian and both endpoints use the same coordinate unit. Equality of endpoint travel gives c_source × omega_source = c_target × omega_target and Ratio = c_source/c_target. Preserve authored initial phase; do not snap initial poses. Bilateral reaction conserves ideal transmission work; no assigned-velocity work source. Relative joint speeds use each hinge's own declared axis/frame.
3. **Declarations:** package-local typed CoordinateReference distinguishes kind and local hinge index from PortId, role, direction and locator Body. Local indices resolve only in their declared coordinate table. Reordering unrelated sliders/ports/instances must not change the selected hinge; reordering the selected hinge table requires updating its explicit reference, not preserving an accidental ordinal identity. Bundle values and compiled IDs remain distinct contracts.
4. **Execution:** package/bundle admission → catalogue and authoring connection validation → WorkshopSceneCompiler/WorkshopPhysicsCompiler → current compiled-scene codec → worker installation → shared C# constraint execution → committed body motion. Inspect every boundary, not only the compiler. No new animation law or event is required; existing committed poses remain the observation route.
5. **Ownership:** main owns declarations and atomic edits; worker owns installed transmission topology and physical state. Run installs validated rows; Pause/Step/Resume retains them; Reset restores exact authored state; Save/Load retains endpoints and reconstructs the same declared law. Invalid kind/index/ratio/direction/cardinality or wire input must refuse with the boundary's typed reason before changing construction, revision, Undo or installed world. Per-tick mutable replacement of scene topology requires repair or explicit justification against this ownership contract.
6. **Generality:** use an unfamiliar mixed package with independently observable second-hinge motion, an unrelated first hinge, root and moving locators, changed slider count/order and port/instance order. Exercise positive and negative ratio, disconnected control, shared-body/moving-base composition where supported, and literal admission boundaries through production interfaces. Current candidate tests are source material, not executed proof.
7. **Deletion:** omit the preserved Conveyor candidate's Body-to-hinge arithmetic and root-first-hinge convention from the imported closure. Baseline lacks that imported path; do not edit unrelated primary Conveyor work to manufacture a deletion. No old-name alias, migration, alternate solver path or duplicated orchestration. Regenerate wire companions consistently; remove generated methods not represented by the schema.
8. **Uncertainty:** the original spec omitted law, topology and validation details now settled below. Prerequisite completion and applicable policy/source evidence must still be checked by the coordinator/auditor; no prior executed proof or owner identity is recovered. Planning approval remains pending. Browser/device/performance/release qualification stays at its named later gates.

E1–E5 and E7–E8 apply to declaration separation, typed f32 laws, command/worker transport, lifecycle, storage, forward-only repair and evidence. E6 applies to retaining locator/body geometry semantics; no art change is planned. D1–D6 apply directly; D7 requires preserving existing committed-pose observation ownership, with no new animation mapping.

### Required planning resolutions and candidate concerns

- **Settled ratio correction:** use sourceScale/targetScale, derived above. CAT-019 requires signed shaft/surface conventions and no copied-speed work source; CAT-042 requires finite inertia and load-aware effort. These requirements justify bilateral reaction and frame-aware motion without importing Conveyor publication. Original immutable intent specifies proportional motion, not the candidate's inverted quotient; this is a bad_spec elaboration outside immutable intent, not an intent change.
- **Settled cardinality:** docs/engine-contracts.md#multi-domain-port-contract requires bilateral 1-to-1 mechanical pairs. Reject reuse of either output endpoint or input endpoint; aliases of the same resolved coordinate cannot evade same-side cardinality. A coordinate may expose distinct input and output roles for a chain; CAT-019 names chained transmission. Do not apply the signal DAG rule to mechanical constraints. Shared carriers are supported: combine each unique body's Jacobian contributions before computing effective mass, so common-body cross terms and reaction are correct. Use the same bilateral law for chains, shared carriers and closed compositions. Homogeneous travel relations always admit rest; a non-unit ratio loop may physically lock and is not invalid. No topology classifier or cycle ban is introduced. Typed refusals cover bad IDs/references, invalid ratio, direction mismatch and source-required cardinality violations. Bound redundancy/locking controls to the affected law and require no manufactured energy.
- **Settled numeric domain:** both endpoint scales and the derived f32 ratio must be finite and nonzero; reject underflow-to-zero or overflow quotients at typed admission. Normalize Jacobian coefficients by max(1, abs(ratio)) before effective-mass evaluation to avoid ratio-squared overflow without arbitrary product bounds. Zero effective inverse mass means no impulse; finite numerical residuals remain inside the one algorithm and never fault the tick.
- **Settled admission:** bundle validation checks kind and package-local index bounds. Catalogue checks shape/nonnegative index; authoring/compile resolves against the actual package and refuses typed missing/out-of-range references before indexing. Compiled-scene decode/install validates distinct joint IDs, existence, duplicate IDs, finite ratio, direction and source-required cardinality before any world mutation. No incidental indexing exception is an accepted refusal.
- Runtime currently silently omits unresolved joint references. Remove this silent omission: validate the entire candidate before install, then resolve every admitted row. Retain installed topology in worker scene state rather than accepting an unaudited changing topology each tick.
- Current generated WorkshopResponse contains transmission builder methods absent from its schema table; regenerate from the single current schema and inspect the complete generated diff.
- Prepare clears AccumulatedImpulse before WarmStart. Use the simplest coherent per-substep solve without dead warmstart/accumulator state unless the implementer demonstrates necessary retained state and defines Reset ownership. Shared-body Jacobians use the combined contributions specified above.
- Candidate tests duplicate a private Rig, manually inject transmissions each tick and bypass the required shared Workshop integration harness. Their second-hinge and reordering cases inspect compiler identities only; the purported reorder adds one slider. Replace orchestration with the shared mechanism and preserve useful boundary/codec assertions.
- Original matrix's “roundtrips identically” means decoded semantic fields and rerun behavior, never byte identity. Original generic build/test commands are illustrative only; matching compiler logs and authoritative local verification below govern.

### Planned acceptance and execution ownership

Replacement implementer owns affected incremental builds and native checks, recording exact commands, matching configuration/inputs and raw log paths here. Use the shared native Workshop integration harness, production scene compiler, command router/codec, simulation and committed reads.

Required cases:
- Mixed slider/two-hinge package: drive selected second hinge with root locator; observe driven motion and unchanged unrelated hinge. Repeat with genuinely reordered unrelated declarations and moving locator; negative control removes only the mechanical connection.
- Signed ratio with independently derived endpoint equation, differing scales and rotated/moving frames; load reaction demonstrates bilateral behavior. Cover supported shared-body and multi-link topology, and bounded redundancy/locking controls for closed compositions against the same bilateral law.
- Missing/null/unknown/prismatic coordinates; index -1, zero, last valid and first invalid; no-hinge base; non-mechanical coordinate; wrong role/direction/domain; zero, NaN, infinity and nonrepresentable derived ratios. Assert typed reason and unchanged construction/revision/Undo or installed world as appropriate.
- Duplicate/alias input conflicts and malformed compiled-wire rows: duplicate/zero IDs, missing joints, same joint, invalid ratio. Valid neighboring inputs must succeed.
- Run, Pause, Step, Resume, Reset and a second Run; exact authored Reset fields and no stale transmission state. Save/load into a fresh harness reproduces endpoints and causal signed motion; malformed replacement leaves the prior world intact.
- Current schema roundtrip preserves decoded joint IDs and canonical ratio; generated companions match schema. Empty transmission scenes retain existing behavior.

Choose quantity-specific speed/angle tolerances from the gameplay/defined-interface consequence and observation duration before seeing outputs. Use bounded game-time stall guards, not incidental 30-tick goldens. Current 1.90–2.10 and speed >4 assertions have no recorded independent justification and are not approved defaults.

One designated executor (coordinator to name) runs the full required local solution round once after implementation, using /Users/aidan/dev/personal/tim/docs/delivery-workflow.md#local-test-authority and the five matching compiler logs; zero failures and Skipped: 0. Normal hooks are additional. No tests/builds were run by this reconstruction. Independent planning audit precedes implementation dispatch; full independent Auto layers follow implemented evidence. No browser/mutation/local Release publish, and no CI wait; remote build/artifact/deployment and browser qualification remain unverified.

Entry evidence supplied by coordinator: remote main via git ls-remote matches original baseline; /private/tmp/tim-gate-9a46e is empty and prior /private/tmp/tim-{full,product,claims,intent}-9a46e.* inputs have no receipts, so neither is runtime proof. Effective Auto snapshot: /Users/aidan/dev/personal/tim/_bmad/render/bmad-build-auto/tim-9339f0454876/e4465dbab89be3738256/workflow.md. Coordinator reports VerifyPolicy Auto structural findings=0 and six required resolved layers always active/nonempty; this establishes entry wiring only, not planning approval.
