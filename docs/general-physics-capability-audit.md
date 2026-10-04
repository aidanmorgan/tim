# General physics capability audit and extension contract

**Current architecture:** [Canonical Half game values and WGSL f16 physics](gpu-f16-physics.md) define the numerical model. Current design/acceptance is self-contained; implementation and qualification status are in [TODO](../TODO.md).

## Required execution contract — 1 October 2026

The dated source observations below remain evidence of their inspected revision. The required target is a dedicated simulation worker with C# discrete host and WebGPU/WGSL f16 numerical authority at fixed 120 Hz (initially four outer substeps), a separate C# animation worker initially at 60 Hz, and a browser-main-thread Godot/WebGL presenter on its own display clock. Both worker runtimes are independent of the Godot scene host. Typed messages, bounded transferable-buffer ownership and explicit clock/generation mapping connect the contexts; logical separation or asynchronous calls on one thread are insufficient.

Complete all required engine capabilities, renderer/worker integration, current-consumer proof and measured performance through **P0-035** in the [authoritative TODO register](delivery-workflow.md#pipeline-priority) before product or catalogue expansion. The release target is sustained 60 FPS on declared baseline devices and 90 FPS on qualified displays/devices under the [numeric budgets](planning/requirements.md#worker-performance-budgets). These remain unproven targets. Later catalogue additions use qualified declarations and receive their own proof; a missing law reopens the engine gate before dependent delivery.

## Historical source observations

Use [current source acceptance](planning/requirements.md) and the [coverage working documents](coverage/README.md) to establish capability membership and qualification. Every physical law and consumer below remains required.

## Required element/process coverage artifact

Create a compiler-checked coverage manifest and human-readable report from it. Each current catalogue entry, fixture, individual element, named variant and required research candidate must resolve to a stable typed identity. Explicitly record duplicates and references rather than dropping records or inflating completion counts.

For each element/mode, record:

- Source specification and current implementation status.
- Required generic laws, sensors, controller/state-machine primitives and domain dependencies.
- Geometry/contact/interaction accuracy and material/constitutive models with units and parameter validity ranges.
- Conserved quantities, ports, source/sink assumptions, events and thresholds.
- Numerical error/conditioning bounds, supported workload envelope and dependency-driven scheduling.
- Engine implementation symbols and declaration path, including every callback that can influence state.
- Positive/negative/boundary/substitution/native proof and individual real-UI integration/Reset/save proof.
- Production bundle/revision, browser/device performance evidence and remaining failure links.

Use enums for closed process, state, capability, status and query choices; strongly typed IDs for extensible identities. Validate external serialization and reject unsupported/undefined values. No arbitrary script expressions, string selectors, part-name tags or hidden catalogue dispatch may serve as a declarative physics system. Existence of a manifest row or generic interface is not evidence of implementation.

## Engine/part boundary

The [general-engine execution design](general-engine-design.md) defines the concrete data/compiler/kernel/transaction/animation pipeline. Its [source composition map](planning/general-engine-element-map.md) enumerates all 365 CAT/EL/TH/RAD/GAP identities and binds exact existing fixture/mode ledgers. Existing capability membership is a starting route, not proof of model sufficiency; missing submodels stay with the named design owner.

Animation evaluation belongs to the separate C# animation worker: visual loops, motor spin, recoil envelopes and effect clocks are not fundamental physical processes. Include an animation classification in the element coverage audit so cosmetic needs cannot create artificial solver requirements. Physical shafts/contact motion and energy accounting remain in physics when functionally required; the animation service can optionally observe their state without owning it. See [independent animation contract](simulation-presentation-bridge.md#independent-animation-system), PERF-36–38, and verify identical authoritative results with animation disabled.

Use the [simulation–presentation bridge](simulation-presentation-bridge.md) to enforce the runtime boundary: typed intent commands and authoritative query services enter through the simulation owner; coherent committed snapshots/events/results leave through a renderer-independent read model. The browser-main-thread adapter owns Godot nodes, timestamp interpolation, asset bindings and final property application. It never waits synchronously for either worker, and reads only committed owned data. CQRS-style separation does not waive full gameplay rollback, reliable event delivery or lifecycle generation checks; the bridge remains open implementation work.

Generic physics code may depend on physical state, geometry, units, numeric tolerances, material coefficients and explicit supported constitutive models. Constants inherent in numerical methods and validated defaults are legitimate; their meaning and units must be named. The prohibition concerns hard-coded puzzle identities and bespoke outcomes, not the equations themselves.

Part declarations assemble bodies, constraints, ports, materials, sources, sensors and finite stores. They do not compute custom collision responses, inject another body's velocity, add free energy or maintain hidden authoritative state. Generic controllers express actuator targets, latch logic and event conditions with typed operations and world-owned state. UI events and artwork may observe those results; cosmetic animation never changes physics.

Enforce a dependency boundary: solver/law modules must not reference concrete part classes, catalogue/level/goal identifiers, UI/presentation nodes or renderer state. Audit scene adapters and part scripts as well as engine/physics. Require independently compiled simulation and animation layers with only approved math/runtime dependencies. Move scene capture and conversions into construction/presentation adapters. Remove superseded implementations and callers in the same forward refactor; do not keep compatibility shims, aliases, old-format readers, automatic migration or old/new execution paths.

Build declarations into compact runtime execution plans at load/topology change. Resolve capabilities, typed handles, topology order, sparse patterns and dependency closure once where valid; numerical data and dynamic eligibility still update when their dependencies change. Per-element physical equations, solver callbacks and animation evaluators are forbidden on every execution path, including cold paths; moving them into the worker or a registry does not make them generic. Avoid reflection, repeated graph discovery and giant universal matrices in inner loops. Optimize measured hot paths; cold authoring interfaces may produce typed declarations, never executable part-specific laws. Numerical physics uses shared WGSL f16 kernels; cosmetic evaluation uses shared C# animation definitions.

## Proof that new elements need no physics edits

For every supported process, build multiple differently named and shaped assemblies from declarations only, including an unfamiliar composition using existing laws. Within supported capabilities, a new element adds presentation assets, declarative content/bindings and tests; it must not alter solver/law/evaluator source or add physics/animation algorithms in a part class. A missing capability instead requires a reusable engine extension with typed parameters and independent proof in its consuming slice; a single initial consumer is allowed and does not justify a bespoke solver. Record the actual file diff.

Require material/shape substitution, reordered declarations/inputs, unsupported-model rejection and positive/negative controls. Test independent analytic or conservation expectations, with within-build exact replay and explicitly qualified cross-browser tolerances; do not assume cross-platform bitwise identity.

Use scaling laws where appropriate (mass, length, conductance, capacity, source strength), but specify the regime rather than assuming every process scales linearly. Cross-domain fixtures must verify that changes to one model do not create energy, lose matter or depend on enumeration order. Native generic fixtures supplement mandatory individual UI proofs.

## Performance practices to incorporate and measure

The [browser performance plan](browser-physics-performance.md), PERF-01–48 and the P0-035 engine gate define the performance contract; only the [TODO register](delivery-workflow.md#pipeline-priority) determines task order. Add the following decisions to its measured implementation record:

| Practice | Application and acceptance limit |
| --- | --- |
| Compile declarations and cache topology | Build validated runtime plans and sparse patterns outside hot loops; invalidate on topology/material/geometry changes as applicable. Distinguish unchanged structure from changed numeric coefficients. |
| Compact storage and reusable arenas | Evaluate contiguous arrays/structure-of-arrays or small vector batches for actual hot kernels, retaining typed handles at boundaries. Bound retained capacity and prevent scratch aliasing with snapshots. |
| Sparse local solves | Solve truly coupled groups, exploiting sparsity and persistent structure. Select dense versus sparse kernels from measured dimensions/conditioning under one documented algorithm; avoid building a world-wide dense solve for unrelated domains. |
| Persistent contacts and warm starts | Preserve stable identities and state-conditioned validity. Reject stale caches; keep convergence/error criteria and failure rollback. |
| Candidate pruning and query flags | Separate contact, sensor, transport and render participation; tight bounds and compound hierarchies must conservatively contain their domain geometry. Reuse temporal coherence without missing new blockers. |
| SIMD and independent batches | Profile independent geometry/constraint batches and graph-colouring overhead before adopting them. Qualify ordering/convergence in actual GPU shader execution under the accepted f16 envelope; host/compiler SIMD remains a separate measured option. Mandatory simulation/animation workers are distinct from optional intra-worker shared-memory parallelism; native SIMD timings do not establish browser gains. |
| Residual-driven iterations | Stop only when documented residual/error criteria hold, with bounded failure handling. Budget exhaustion never silently lowers physical fidelity or drops events. |
| Absent/inactive work | Dependency closure, event queues and qualified island sleep remove unneeded work; all required accumulated stores and wake sources remain live. |
| Representative end-to-end measurement | Pair kernel metrics with 60/90 Hz frame tails, two-tick bursts, combined domains, allocations, startup and sustained mobile thermals. Retain optimization rejections and avoid stacking unmeasured techniques. |

Primary references: [PhysX best practices](https://nvidia-omniverse.github.io/PhysX/physx/5.3.0/docs/BestPractices.html) documents scratch buffers, tighter bounds, separate query participation, release measurement and substep overload. [MuJoCo computation](https://mujoco.readthedocs.io/en/stable/computation/index.html) provides examples of structured/sparse constraint algebra; its particular contact models are not automatically appropriate here. [Box2D SIMD analysis](https://box2d.org/posts/2024/08/simd-matters/) motivates independent constraint batches; its native benchmarks are not browser predictions. [Box2D determinism](https://box2d.org/posts/2024/08/determinism/) explains why engine determinism alone does not guarantee application determinism.

Adopt techniques only when applicable to the pinned host and accepted correctness contracts. Do not copy advice to drop elapsed time, skip necessary prescribed pairs, loosen the approved f16 error contract or rely on unsupported threading. “All best practices” means a reviewed applicability/measurement register, not every possible optimization implemented regardless of benefit.

## Completion gate

The generalized-engine claim remains blocked by incomplete element/process mapping, missing domain implementations, residual part-authored physics, absent independent workers and unqualified performance. Every required law needs independently computed native controls and named generic fixtures constructed through actual UI controls before P0-035 can close; future catalogue parts cannot be an unstated prerequisite for engine proof.

P0-035 requires current-consumer evidence, deterministic authority under independent cadences, adversarial transport/lifecycle tests, production/device budgets and deletion of obsolete execution paths. The closure report maps every removed path to its replacement, affected callers/content/tools and fresh proof; unsupported inputs explicitly reject. Historical test artifacts remain untouched and cannot serve as current runtime input.

Only then deliver new elements through declarations, presentation assets and their own focused correctness/build/real-UI/Reset/save proof and individual commit/push. Final catalogue and campaign audits reconcile that expanded coverage without reopening a completed engine implicitly.
