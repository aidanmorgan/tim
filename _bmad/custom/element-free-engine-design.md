# BMAD design requirements: reusable physics and animation

Owner instruction, 4 November 2026. This guide translates the [sole design authority](../../docs/architecture/element-package-isolation.md), [engine contract](../../docs/engine-contracts.md#general-data-driven-engines) and [general engine design](../../docs/general-engine-design.md) into design obligations. It owns the design requirements below; the [review policy](element-free-engine-review.md) owns scope, evidence and approval decisions. Workflow overrides load these documents rather than define separate versions of their rules.

## Terms

- **Engine:** shared authoring/UI routing, bundle admission, compilation, simulation/domain laws, worker transport/read models, animation and presentation infrastructure.
- **Capability:** a reusable mathematical law, sensor/controller operation or animation primitive with a defined input/state/output contract. Its kind is a closed enum; each declared instance has a typed identity.
- **Supported capability:** an implemented capability admitted by the current schema and validated for the required use. A missing physical process is a new capability, not an excuse for element-specific support.
- **Element-specific assumption:** shared executable behavior that depends on a product's identity or recipe instead of an explicit capability contract. This includes implicit dimensions, composition, target choice, arbitrary product bounds or visual mappings, even when the code has a generic name.
- **Data-only extension:** another element using supported capabilities adds declarations, bindings, assets and tests, without engine changes or executable element callbacks. A genuinely new process may extend a reusable capability in its consuming slice.

Opaque element IDs may identify authoring/save records. They must not select physical rules or animation behavior. Mathematical constants and independently justified numerical tolerances are valid engine data; product constants belong to packages.

## 1. Ownership and dependency requirements

| Owner | Required responsibility | Forbidden responsibility |
| --- | --- | --- |
| Element package | Typed parameters/ports, bodies, geometry/materials, capability instances/composition, assets and visual bindings | Runtime physical equations, custom callbacks, engine/sibling dependencies |
| Authoring/compiler | Bundle discovery, typed edits, validation and compilation into resolved runtime rows | Catalogue-aware defaults or target selection, product recipe enforcement, physical simulation |
| Simulation Core/worker | Shared laws and typed operations; authoritative physical motion, state and events | Catalogue/assets, element-selected kernels, cosmetic ownership of physical observations |
| Animation worker | Generic mappings/tracks driven by committed values, events and shared time | Physics mutation/work, package scripts, one-shots inferred from read differences |
| Renderer/UI | Apply compiled bindings; expose declared controls and explicit authored selections | Select rules by element identity, implement element-specific controls or cosmetic schedulers |

Data flows physics → animation → renderer and physics → renderer, never back into physics. Contact-bearing motion remains simulation-owned. Preserve typed commands/queries, event-ring consumer ownership and exact authored Reset state. Do not rebuild unaffected worker/ring infrastructure.

## 2. Declaration and composition requirements

**D1 — Explicit instances and references.** Declare the exact body, collider/region, joint/coordinate, node, port, store, observation and property target needed by each binding. Resolve local identities to compiled handles. Owner identity is insufficient when several capabilities share that owner. Do not infer a hinge from a body ordinal or choose the first matching row.

**D2 — Composable contracts.** Separate port identity from domain/direction/role; support repeated roles where the declared cardinality permits them. Independently identify sensing, guiding, signal operations, supply nodes and driven surfaces. A product's combined recipe belongs in its package. Do not require one node per element, a static body for otherwise body-independent logic, or an electrical contact for a purely signal operation.

**D3 — Data owns variation.** Package data supplies geometry, frames, materials, product parameter ranges, target sets and visual recipes. Shared admission may impose only the reusable contract's justified validity limits. Do not hide a missing contract in tags, reflection dispatch, property bags, serialized scripts or runtime callbacks. Bounded typed mappings must have explicit supported operations and validation.

## 3. Compilation and admission requirements

**D4 — Complete, atomic compilation.** Specify package values/instance overrides → validation → resolved runtime rows, including units. At the appropriate bundle/edit/install boundaries, validate enum values, finite/range limits, reference kinds, ownership, duplicate IDs, cardinality, topology and conflicting writers. Unsupported input receives a typed visible refusal without partial mutation. Never silently omit a capability, substitute a default product or admit a reference that later indexes an unrelated table.

Joint child poses must use both local frames and the declared initial coordinate. Authored geometry and solver frames must describe the same mechanism. Workshop and lessons compile the same declared target semantics; content IDs may select authored content, not physical rules.

**D5 — One current representation.** Keep typed capability IDs and enum operations through compiler, wire, runtime, reads and tests. Update current callers, schemas and content together; delete superseded paths without aliases, adapters, migrations or fallback. Logical counts reflect actual demand; backing storage grows geometrically and interaction state stays sparse. Do not add arbitrary population ceilings or owner×target reservations.

## 4. Physical law and multi-domain requirements

**D6 — Shared laws.** Define the primitive's inputs, state, outputs, units, valid domain, ownership and causal order before implementation. Physical execution remains C# WASM SIMD f32 at the established rates. Use shared shape/material/contact/constraint laws and explicit generalized-coordinate relations; package identity must not select equations.

For a new process, identify the bounded reusable capability, its consuming story and discriminating controls. Do not promise support for all future physics, build a universal framework in advance, or approximate a missing law with an element handler.

Preserve documented limitations and their owners. For example, binary drive enablement does not prove debit of drive work; that separately owned demand law must not be claimed or silently added to a routing slice.

## 5. Animation and observation requirements

**D7 — Independent source, mapping and evaluator.** Declare separately:

1. Exact committed capability/field or typed event/filter.
2. Supported typed value/state mapping, including units, scale/offset or threshold.
3. Generic curve, duration/response and overlap semantics.
4. Target property, sole writer and any visibility predicate.

Raw phase must not conceal a product's sine recipe; rotation must name a coordinate rather than a Pulley. Blocked-state colors are declared mappings, not shared magic values. Restrict source/curve combinations only for a documented type or mathematical incompatibility.

Physical capability declarations own physical state, events and observation availability. Removing cosmetics must not change them. Select each actuator explicitly; use a declared aggregation when several observations intentionally drive one property. Preserve event-versus-level semantics and single-property ownership.

## 6. Required design record in existing BMAD artifacts

Use the existing spec Constraints/Capabilities and companions, architecture decisions, story scope/AC or Auto Code Map/verification fields. Do not create another tracking platform or copy these guides into each story.

Before implementation readiness, answer these eight questions for the affected behavior. Reference existing contracts where unchanged; state why any item is inapplicable.

| Question | Required answer |
| --- | --- |
| What behavior is delivered? | Observable outcome, requirement IDs and retained behavior. |
| What is reusable? | Existing or bounded new primitive; kind, inputs/state/outputs, units and valid domain. |
| What is declared? | Package values/composition and exact typed references; identities distinguished from roles. |
| How does it execute? | Declaration → admission → rows/handles → execution → committed state/events → animation/binding; affected code paths. |
| Who owns changes? | State/property writers, Run/Reset/save/load and atomic rejection boundaries. |
| What proves generality? | Planned unfamiliar declaration/composition, relevant variation/reordering, positive/control/boundary cases and independently derived expectations. Identify reusable existing evidence separately. |
| What is deleted? | Superseded symbols/schema/callers/content, or an explicit statement that none is superseded. |
| What remains uncertain? | Missing capability/decision and prerequisite owner; required-now decisions must be settled before readiness. |

Planning specifies the tests; it does not require the planned implementation or its tests to exist already. Implementation acceptance supplies actual results under the review policy. Apply [testing guidance](../../docs/testing-guidance.md#float-comparison-and-effort-rules): semantic assertions, quantity-specific gameplay/interface tolerances and generous finite stall guards; no output hashes, incidental tick/trajectory goldens, skipped required tests or mutation runs.

## 7. Integration with native BMAD steps

Use the [single native workflow map](project-engineering-contract.md#native-workflow-bindings) and [loop protocol](bmad-loop-protocol.md). They bind these requirements to supported customizations, native artifacts and explicit transitions. The [review policy](element-free-engine-review.md) owns scope, evidence and blocking verdicts. Do not duplicate the dispatch map here.
