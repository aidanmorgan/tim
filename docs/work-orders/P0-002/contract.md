# P0-002 — Required engine capability inventory

Stage: Design with executable inventory tooling. Initial independent review failed. The revised implementation awaits independent re-review and publication; this document does not declare Pass.

Implementation owner: provider session `01a0f6f8-aa85-71c3-8b91-1225e841682d`, canonical agent `/root/p0_002`, parent session `01a0f5f1-ab9a-79a3-9181-0dbb87285ec5`. Prerequisite: [P0-001 terminal review](../../verification/P0-001/review.md), status revision d99b5dc88dda943513e8813b03307428f3b7bcfa.

## Deliverable and boundaries

[Inventory index](../../coverage/engine-capabilities.json) references 54 bounded strict-schema documents. They enumerate 71 enum-typed capabilities and 1,471 separately identified source bindings/consumers: 799 task specifications, 216 EL, 37 TH, 22 RAD, 18 GAP, 72 current catalogue entries, 302 authored fixtures and five research containers. There are 104 current catalogue configuration/mode records. Source presence never qualifies implementation or behavior.

Each capability declares model boundary, units, independent oracle, design owner, nonempty typed implementation-owner set and proof owner, actual current source symbols, dependencies and the exact reverse list of source obligations. Each source retains its own delivery/proof owner and content hash. A future product's delivery owner is not an engine prerequisite: the capability's separate owner must prove the shared law with generic fixtures before P0-035. No additional runtime architecture or product implementation is introduced here.

The model boundary uses authoritative rigid-body mass/inertia/integration, contact and finite work/material/enthalpy ledgers. Gravity/atmosphere are EnvironmentState, separate from electromagnetic FieldForce. TopologyTransaction explicitly covers atomic body/handle changes. PresentationHistory samples authoritative state; it does not inherently require an active cosmetic animation clock. AnimationEvaluation/AnimationLifecycle apply to time-varying cosmetic consumers; static material and physical pose/deformation application remain RenderApplication/ResourceLifetime obligations. Radiation exposure is an explicit game abstraction, not a clinical dose claim.

P0-003 owns workload/device/numerical-budget selection; P0-004 owns exclusive field/property writers and assembly boundaries; P0-005/006 own protocol and clock contracts. Their referenced capability owners are explicit prerequisites for implementation, not unresolved choices hidden in this inventory. PerformanceMeasurement instrumentation belongs to S062-D/S062-I, with integrated qualification P0-034. P0-029 remains deployment/startup work.

The [existing-owner reuse/state inventory](../../verification/P0-002/reuse-state.md) hash-binds SimulationTimers, AnimationBatch, BodyBoundsTree and SimulationTransaction, records current test evidence and assigns lifecycle/identity/rollback gaps to their existing owners. ECS adoption and experiments remain deferred, never prerequisites.

## Acceptance criteria

| ID | Required outcome and independent oracle |
| --- | --- |
| P0-002/A01 | Every current discovered source has one binding and one named consumer; separately recompute origin counts and duplicate/missing/orphan sets. |
| P0-002/A02 | All 71 capabilities have units, bounded model/oracle and valid semantically appropriate owners; owner mutation to an unrelated existing work order rejects. Implementation owners must match the exact required set and an affirmative executable register stage (Native/Integration/Worker/Optimize), never an aggregate Audit or Design row. |
| P0-002/A03 | Exact enum-typed source relations and every capability's BoundSources reverse ledger agree; deleting or fabricating a relation, capability or required dependency rejects. Research groups and grouped task scopes remain explicit, not representative passes. |
| P0-002/A04 | Current catalogue and authored fixture consumers retain geometry/contact, state/publication, rendering and applicable active animation relationships. Actual fixture kind controls inheritance. The four existing owners retain exact hashes, current evidence and explicit identity/generation/rollback gaps under the reuse/state contract. Review direct visual writers and physical/cosmetic distinctions in the derivation record. |
| P0-002/A05 | Actual supported catalogue modes are distinct records. Remove a tone/mode, change its owner, mutate a resource enum/default/assignment or canonical enum declaration/value and require rejection. Laser's current fixed spectrum is not a selectable RGB emitter. |
| P0-002/A06 | Strict typed JSON boundaries reject unknown enum members, duplicate identities, null collections/records, default IDs, invalid paths and dimension/choice mismatches. No aliases, format inference or compatibility fallback. |
| P0-002/A07 | Current source/artifact hashes and lexical type declarations match. Stale source produces Incomplete; invalid references reject. Lexical type checking is not compiler symbol resolution. |
| P0-002/A08 | Focused Release tooling build/tests pass; current inventory/element manifests refresh without manufacturing reviewed/proven status. Preserve historical manifests and failed attempts. |
| P0-002/A09 | Runtime and historical evidence remain unchanged; independently compare the 916-source baseline and explain direct/transitive impacts. No browser qualification or FPS claim follows from tooling checks. |
| P0-002/A10 | Different authenticated reviewer reproduces criteria and impact checks against exact publication file/hash list, records SnapshotApproval, then verifies matching remote commit/blob receipts before terminal Pass. |

## Publication

Only files enumerated in the verification snapshot may be staged. TODO.md, AGENTS.md, runtime migration files, authored game content and historical runtime evidence are excluded. The implementation owner performs any narrowly scoped status/handoff reconciliation and the independent reviewer checks it; the coordinator reconciles reports. The massive external rewrite cannot be staged with this row.

The working-source input manifest records exact current hashes and clean-HEAD availability. The integrated inventory depends on unpublished TODO, runtime and authored inputs outside this publication scope. A clean checkout of this tooling/design publication therefore remains Incomplete for integrated inventory reproduction; remote blob receipts do not establish audit currency or runtime qualification. Focused tooling tests are self-contained. Publishing unreviewed input files or adding a fallback input format is prohibited.

A10 is two-phase: exact-snapshot approval precedes commit/push; terminal review verifies remote commit, parent, scoped file set and blob hashes. The reviewer verifies both the published tooling/design snapshot and the explicit unresolved input prerequisite; it must not report a remote integrated audit pass. No new deploy/browser criterion is introduced because this row changes only independent tooling/design inputs, excluded from the game compilation. Runtime performance, per-part UI/mode proof, Run/Reset/save, deployment and physical device gates remain owned by their existing work orders and explicitly incomplete.
