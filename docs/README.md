# Working documentation

Start with [TODO](../TODO.md) for the current outcome, blocker and next acceptance check, then [AGENTS](../AGENTS.md) for how to work. Current documents below contain the intended game and complete current task criteria. Archives are optional provenance, never missing requirements or another execution queue.

| Need | Current document |
| --- | --- |
| Game loop, build/run commands and repository entry | [README](../README.md) |
| Approved visual, interaction, palette and mechanism presentation | [DESIGN](../DESIGN.md) |
| Canonical Half game values and WGSL f16 numerical ownership | [GPU physics](gpu-f16-physics.md) |
| Identity, authoring, protocol, ownership, lifecycle and clocks | [Engine contracts](engine-contracts.md) |
| Generic contact/constraint world and physical geometry | [Collision architecture](collision-architecture-plan.md), [hollow geometry](hollow-geometry-design.md) |
| Independent simulation/animation and browser rendering | [Bridge](simulation-presentation-bridge.md), [presentation bindings](presentation-bindings.md), [performance requirements](browser-physics-performance.md) |
| How the general physics and animation engines execute every planned family | [Execution design](general-engine-design.md), [complete source composition map](planning/general-engine-element-map.md) |
| Supported laws, consumers and completeness | [Capability requirements](general-physics-capability-audit.md) |
| Difficulty and bounded author assistance | [Difficulty](difficulty.md) |
| Every stable requirement, named element/mode and teaching outcome | [Requirements](planning/requirements.md) |
| Standing REQ-01–14, delivery stages, paired review and publication | [Delivery workflow](delivery-workflow.md) |
| One bounded executable scope and genuine prerequisites | [INVEST index](planning/invest-index.md), then the exact [register](planning/work-register.md) row |

## Current physical and controller models

Use the named model with the affected requirement, not an old implementation checkpoint: [springboard](springboard-elastic-contract.md), [bounded acceleration drives](bounded-acceleration-drives.md), [finite energy stores and wound spring](world-owned-energy-stores.md), [rotary transmission](rotary-transmission-parts.md), [rotor airflow](rotor-airflow-contract.md), [conserved airflow/transfer](linear-airflow-cutover.md), [finite gas/chamber/nozzle](finite-gas-foundation.md), and [controls, sensors and events](simulation-controls.md). Unresolved current model or ABI decisions stay with their named design owner before dependent implementation. No CPU fallback, old format or prototype is a supported alternative.

## Planning and source research

The planning directory contains requirements, the register and INVEST navigation: engine/vertical scopes, named elements, current consumers, family profiles, bounded decisions, refinements, campaign reservations/levels, source index/scopes and the obligation map. Cards summarize; complete current acceptance and genuine technical prerequisites remain binding. The map is traceability, not a new task platform. Historical completion IDs do not create fresh implementation chores.

The [TIM research dossier](research.md) preserves reference observations and fidelity questions. [Component](component-research.md), [thermal](thermal-component-research.md), [radiation](radiation-component-research.md) and [gap research](physics-puzzle-gap-audit.md) retain unique candidate descriptions and original source limitations. Adopted behavior lives in current named requirements; research priority labels and example campaign slots are proposals, not execution order. Current difficulty is Forgiving/Balanced/Precise, the campaign target is 150, and parts declare generic capabilities rather than implementing private numerical laws.

## Tools and current records

Tool READMEs document their actual commands and limitations: [Playtest](../tools/Playtest/README.md), [Performance](../tools/Performance/README.md), [Coverage](../tools/Coverage/README.md), [Ownership](../tools/Ownership/README.md), [WireContract](../tools/WireContract/README.md), [LifecycleContract](../tools/LifecycleContract/README.md), [TraceAllocations](../tools/TraceAllocations/README.md), [Anvil](../tools/anvil/README.md). Frozen contract fixtures check their named historical design; they are not current GPU qualification or prerequisites for implementing an unadmitted ABI.

[Coverage records](coverage/README.md) track source/consumer membership and proof state. Source hashes changing with documentation do not promote runtime proof. Each live slice keeps exact failures, commands, identities and independent review in its own record; TODO keeps only the working brief.

## Historical material

The [history index](history/README.md) contains retired approaches, dated implementation/progress reports and the original-path lookup. The 26 work-order Markdown records, verification captures, reference inputs, native logs and measured JSON artifacts retain their original scope and identities. They are not part of the current design reading path. Existing tools may still exercise a frozen historical fixture; that does not make the old runtime/schema current.
