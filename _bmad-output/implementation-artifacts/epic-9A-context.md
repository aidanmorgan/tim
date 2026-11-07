# Epic 9A Context: Sole Architecture Conformance (Project-per-Element, Command/Query, Event Disruptor)

<!-- Generated from planning artifacts. Regenerate with compile-epic-context if planning docs change. -->

## Goal

Make isolated element packages and their compiled bundle the sole declaration authority, with typed commands/queries and committed event occurrences. Extend reusable capabilities so new elements need declarations, bindings and assets rather than element-specific engine changes, preserving existing gameplay. This bounded context covers the current declaration-only repair queue; it does not replace earlier Epic 9A stories.

## Stories

- Story 9A.46a: Route ropes around any declared guide
- Story 9A.46b: Use the same declared interactions in puzzles and Workshop
- Story 9A.46c: Start articulated parts in their declared joint frames
- Story 9A.46d: Add repeated typed port roles through declarations
- Story 9A.46e: Connect mechanical ports to declared coordinates
- Story 9A.46f: Drive each declared contact surface from its own coordinate
- Story 9A.46g: Compose existing signal primitives within one element
- Story 9A.46h: Reuse count and memory laws independently of product form
- Story 9A.46i: Route supplied work through declared capability nodes
- Story 9A.46j: Declare sensing and guiding independently
- Story 9A.46k: Choose visual responses through declaration data
- Story 9A.46l: Address each actuator's controls and observations
- Story 9A.46m: Enforce element-free core boundaries

## Requirements & Constraints

For the current supply-routing work, FR-01 preserves catalogue construction, parameter editing and typed connections; FR-03 preserves dedicated-worker C# WASM SIMD physics at 120 Hz with 480 Hz substeps; FR-07 requires all 72 catalogue elements as declarations without bespoke solver branches; FR-09 preserves exact canonical f32 coordinates and connection graphs through save/load. Preserve every existing element/variant, unlimited Workshop and the 150-level goal.

PERF-24 assigns physical laws, stores and transitions to generic typed capabilities; packages declare composition and presentation without callbacks, identity-selected behavior or arbitrary expressions. PERF-25 resolves typed handles, geometry, topology and dependencies into compact execution plans at compilation/revision boundaries, retaining f32 SIMD, canonical ordering, bounded numerical budgets, cache invalidation and atomic rollback. PERF-26 requires unfamiliar compositions and meaningful substitution/reordering, unsupported-model, conservation and applicable cross-domain checks without core edits for supported processes. Repository-wide audit, measured browser performance and device qualification remain at their named later gates.

Supply, switching, logic and consumers have independent capability identities and declared edges, permitting internal source/load composition. Contact-work charging follows shared reachability and allocation while conserving finite supplied energy. Opening a route stops charging; an open route or unavailable source disables a drive under its existing binary enablement contract. Debit of drive work remains the separately owned powered-part demand-drain follow-up. Preserve direct-source behavior, exhausted-source behavior, invalid-reference and ambiguous unsupported-topology refusals. Delete owner-kind exclusivity, drive/supply composition prohibition and the separate direct-source-only charging path; update callers, packages and serialized contracts together.

Every slice deletes its superseded path: no fallback, compatibility shim, old-name alias, migration, version branch or duplicate rule. Unsupported input receives a typed visible atomic refusal. Prove real production behavior with positive/control/boundary cases, relevant tick progression, Run/Reset/save/load, exact authored restoration and unfamiliar declarations. Assert decoded semantics and causal order using quantity-specific gameplay/interface tolerances and finite generous stall guards; no output hashes, whole-buffer identity, incidental timing goldens, skipped game tests, mutation runs or precision reruns without a concrete uncovered defect.

Use native BMAD planning/verification/triage records and resolved project customizations. Required-now gaps are Incomplete; confirmed defects, including pre-existing defects, require repair and independent re-review. The six independent Auto review layers include Forward-Only and Element-Free Engine auditors. Maintainability metrics and balanced coupling apply across production/tool code, excluding test projects; run the gate before authoritative normal builds/full tests because it injects analyzer configuration. One final full local solution round, matching warnings-as-errors builds and normal hooks remain mandatory. Publish approved work without waiting for CI; remote build/artifact/deployment qualification stays unverified unless independently established. Browser/E2E and mutation remain deferred to post-catalogue qualification.

## Technical Decisions

Keep package declarations, compiler admission, simulation laws and presentation ownership distinct. Declare exact ports, nodes, stores, coordinates and observation targets; resolve local typed identities into runtime handles. An owner identity cannot substitute for a capability identity. Validate enum values, units, finite/range limits, ownership, references, duplicates, cardinality, topology and conflicting writers before partial mutation. Runtime uses compiled state, not authoring traversal or per-part callbacks.

Use C# wherever possible, strict TypeScript for necessary glue/tooling, enums for closed sets and strongly typed extensible identities. Convert strings only at genuine external boundaries. Keep Machine, Physics and Rendering graphs separate; f32 physics feeds independent cosmetic animation and instanced rendering one-way. Preserve committed state/event ownership and authored Reset state. A new process may add a bounded reusable law in its consuming slice; do not build speculative universal frameworks.

Before readiness, settle the eight affected-design questions in native artifacts: outcome, reusable contract, declarations/references, execution path, state/lifecycle ownership, generality proof, deleted path and unresolved prerequisites. Apply the current primary project engineering contract, both element-free guides and loop protocol; this context is orientation, not planning or implementation approval.

## UX & Interaction Patterns

Preserve palette, readable mechanism/state cues and contextual construction controls. Players place/configure parts, connect typed ports, Run, Reset and adjust. Architecture repairs introduce no visual redesign. Actual Chrome proof remains pending at the post-catalogue gate.

## Cross-Story Dependencies

The repair queue runs 9A.46a through 9A.46m before Conveyor 11.1b and later catalogue additions. The current 9A.46i prerequisites are 9A.46d's package-local port identities with repeated roles and exact endpoint preservation, and 9A.46g's independently identified signal nodes/internal edges with existing cycle/delay laws. Neither capability may collapse multiple instances to one owner. Guard closure follows repairs.

The isolated implementation baseline is published commit a3b50d0d2f584ae8d1bee19bc2265692fea495dc. Preserve unrelated Conveyor and policy work; do not import it. Load only the selected contract and genuine prerequisites, not historical results. This file supplies bounded context only and grants no delivery or readiness verdict.
