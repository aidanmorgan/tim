# Working documentation

Start with [TODO](../TODO.md) for the current outcome, blocker and next acceptance check, then [AGENTS](../AGENTS.md) for how to work. The four design authorities below, plus the ordered roadmap, describe the only system the game is built to. Every other current document is navigation, element behaviour, content or research that defers to them. Archives are provenance, never missing requirements or another execution queue.

## Design authorities

| Authority | What it fixes |
| --- | --- |
| [Physics Architecture](gpu-f16-physics.md) | Canonical IEEE-754 f32 game values, the Tri-Graph architecture, dedicated WASM SIMD128 physics solver, the generic capability inventory, the solver model and the game-grade envelope |
| [Engine contracts](engine-contracts.md) | Ownership of the physics worker, the animation worker and the main-thread renderer; identity, protocol, lifecycle and clock rules |
| [Delivery workflow](delivery-workflow.md) | Standing requirements REQ-01–14, playable-first precedence, paired independent review, stage gates and the release checklist |
| [Ordered roadmap](planning/invest/vertical-delivery.md#rolling-playable-roadmap) | The programme: ENGINE-CORE-1, ENGINE-CORE-2, one element per slice, ANIM-1, LEGACY-0, ELEMENT-n, CAMPAIGN |

## Supporting current documents

| Need | Document |
| --- | --- |
| Game loop, build/run commands and repository entry | [README](../README.md) |
| Approved palette, form, lighting, motion and mechanism presentation | [DESIGN](../DESIGN.md) |
| Physics → animation → renderer data flow: commands, committed read model, events, Reset/Save barriers | [Bridge](simulation-presentation-bridge.md) |
| The one declared binding for every presentation property | [Presentation bindings](presentation-bindings.md) |
| One master clock; 120 Hz physics, 60 Hz animation, display-paced rendering | [Shared clock cadence](shared-clock-cadence.md) |
| Controllers, sensors and presentation events (Delay, Hold timer, Clock, Counter, Latch, Basket, detectors) | [Simulation controls](simulation-controls.md) |
| Release performance checklist and measurement protocol (P0-034) | [Performance checklist](browser-physics-performance.md) |
| Forgiving/Balanced/Precise profiles and bounded author assistance | [Difficulty](difficulty.md) |
| Generic contact/constraint geometry | [Solver model](gpu-f16-physics.md#solver-model), [hollow geometry](hollow-geometry-design.md) |
| How the general engines execute each capability family | [Execution design](general-engine-design.md), [source composition map](planning/general-engine-element-map.md), [capability requirements](general-physics-capability-audit.md) |
| Every stable requirement, named element/mode and teaching outcome | [Requirements](planning/requirements.md) |
| One bounded executable scope and genuine prerequisites | [INVEST index](planning/invest-index.md), [named elements](planning/invest/named-elements.md), [roadmap](planning/invest/vertical-delivery.md) |

## Element models

Named element models state what a part does for the player as declaration data over the generic capability families: [springboard](springboard-elastic-contract.md), [bounded acceleration drives](bounded-acceleration-drives.md), [finite energy stores and wound spring](world-owned-energy-stores.md), [rotary transmission](rotary-transmission-parts.md), [rotor airflow](finite-gas-foundation.md#rotary-capture), [conserved airflow/transfer](finite-gas-foundation.md#airflow-transfer) and [finite gas/chamber/nozzle](finite-gas-foundation.md). Unresolved model decisions stay with their named owner before dependent implementation. No second physics path, old format or prototype is a supported alternative.

## Research

The [TIM research dossier](research.md) preserves reference observations and fidelity questions. [Component](component-research.md), [thermal](thermal-component-research.md), [radiation](radiation-component-research.md) and [gap research](physics-puzzle-gap-audit.md) retain candidate descriptions and source limitations. Adopted behaviour lives in current named requirements; research priority labels and example campaign slots are proposals, not execution order. Difficulty is Forgiving/Balanced/Precise and the target is 150 progressively taught levels plus unlimited free play.

## Tools and records

Tool READMEs document their actual commands and limitations: [Playtest](../tools/Playtest/README.md), [Performance](../tools/Performance/README.md), [Coverage](../tools/Coverage/README.md), [Ownership](../tools/Ownership/README.md), [WireContract](../tools/WireContract/README.md), [LifecycleContract](../tools/LifecycleContract/README.md), [TraceAllocations](../tools/TraceAllocations/README.md), [Anvil](../tools/anvil/README.md). Tools that depend on legacy CPU physics are deleted at LEGACY-0; a frozen historical fixture never qualifies the current GPU runtime.

[Coverage records](coverage/README.md) track source/consumer membership and proof state. Each slice keeps its exact failures, commands, identities and independent review in its own record under docs/verification/; TODO keeps only the working brief.

## Historical material

Historical verification captures, reference inputs, logs and measured artifacts retain their original scope and identities as evidence. They are not part of the current design reading path and are never supported current input. Git history is the archive of deleted documents and code; the working tree is not.
