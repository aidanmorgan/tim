# Browser physics and rendering performance plan

**Current architecture:** [Canonical Half game values and WGSL f16 physics](gpu-f16-physics.md) define the numerical model. Current design/acceptance is self-contained; implementation and qualification status are in [TODO](../TODO.md).

Generalization requirements and current gaps: [general physics capability audit and extension contract](general-physics-capability-audit.md). PERF-23–27 require exhaustive element/process coverage, declaration-only parts, compiled execution plans and evidence that new assemblies reuse existing laws without solver edits. The audit records the newer body-index work; historical implementation observations below remain dated snapshots.

Status: **binding target design and acceptance contract; implementation and qualification remain open**. Design reconciled 2026-10-01; the inspected research baseline is dated 2026-09-29. No speedup or supported performance tier is established by this document. This is the performance acceptance companion to the [collision architecture plan](collision-architecture-plan.md), [authoritative TODO](delivery-workflow.md#pipeline-priority) and [visual contract](../DESIGN.md).

## Objective and scope

Required graphics/physics boundary: [simulation–presentation bridge](simulation-presentation-bridge.md), tracked by PERF-31–48. The 2026-10-01 [worker target](planning/requirements.md#worker-loop-target) requires a C# host/WebGPU f16 simulation worker and separate C# animation worker and browser-thread Godot rendering. Existing in-process contracts are intermediate work; typed boundaries alone do not establish concurrent execution. Apply the [declared worker budgets](planning/requirements.md#worker-performance-budgets) to all new measurements.

Make the complete replacement physics engine practical in a browser while preserving physical correctness, continuous collision, shared-world ownership, same-build replay and exact construction Run/Reset/save restoration. The physics engine, rendering engine and separate simulation and animation workers are the highest priority: complete and qualify them through **P0-035** in the [authoritative execution register](delivery-workflow.md#pipeline-priority) before product, component or campaign work. Optimize simulation, animation, presentation, rendering and startup together. Derive required generic capabilities from the full planned scope and prove them using the current catalogue or typed, UI-placeable generic qualification fixtures; future catalogue elements cannot block the engine gate. Proof-blocking UI and lifecycle infrastructure are engine prerequisites. Later component integration and final-catalogue qualification retain their full scope.

Implement one production pipeline. Refactor current implementations, callers, authored content, tooling and tests forward together, removing superseded algorithms and schedulers. Do not retain selectable old/new solvers, compatibility shims, old-name aliases, legacy modes, automatic format/schema migration or fallback implementations. Reject unsupported inputs explicitly; never infer obsolete fields, silently downgrade quality or drop simulation work. Preserve historical evidence unchanged without treating it as supported current input. Focused per-part and generic-fixture Playwright proof remains mandatory; aggregate benchmarks cannot certify individual parts or required capabilities.

The GPU budgets include compute plus rendering and complete-tick budgets include dispatch, synchronization and transfer; there is no solver-only substitute. The numeric budgets below and the [worker-loop gates](planning/requirements.md#worker-performance-budgets) are **binding acceptance limits for declared workloads/support tiers**, not achieved measurements or a claim that arbitrary hardware is supported. A truthful baseline or scoped optimization follows TODO's [stage budget policy](delivery-workflow.md#stage-gates): identity-bound existing failures remain open; at the applicable qualification/optimization stage, new/worsened failures block that scope and uncertainty stays Incomplete; and all applicable absolute limits must pass at P0-034/P0-035 before product work. Match topology, algorithms and measurement conditions before attributing a gain; the authorized backend/precision redesign is a combined change and cannot claim an isolated precision speedup. worker separation does not establish a storage optimization. Freeze real reference devices, workload counts, metric definitions and sampling rules before accepting a run. A missing device or failed metric leaves that tier incomplete. Changes to thresholds require an explicit recorded decision before the accepting run; never lower a threshold retrospectively. Conditional optimization techniques remain evidence-driven choices, while independent execution, correctness and performance qualification are mandatory.

**Permanent requirement:** The [standing requirements](delivery-workflow.md#standing-requirements) continue after P0-035 for every implementation, content and tooling change: a simulation worker with C# discrete control and WebGPU/WGSL f16 physics, plus an independent C# animation worker, independently display-paced rendering on the browser main thread, sustained 60 FPS within supported envelopes and 90 FPS where qualified, exact authority/lifecycle guarantees and forward-only cleanup. Later work must preserve these invariants and refresh affected evidence. No compatibility shim, old-name alias, schema migration, fallback or silent accuracy reduction is an acceptable performance solution.

## Baseline evidence

Use [TODO](../TODO.md) for current implementation status. The budgets and qualification criteria below define required behavior; measurements belong in the owning slice record.

## Budgets and measurement contract

**Enforcing phase:** The owner's [4 October playable-first decision](delivery-workflow.md#playable-first) supersedes earlier per-increment measurement/qualification requirements here for pre-P0-035 playable development slices and their publication. Record available failures honestly; do not require new matched baselines, detailed profiling, device/fault campaigns or retention/global-audit qualification before the next interaction. P0-034 retains the unchanged numerical budgets and measured failures; P0-032/035 retain their full safety/engine qualification. Ordinary-use correctness and visibly playable behavior still block. The measurement protocol below applies when performing that later qualification or claiming a performance improvement.

Require 60 Hz presentation across the declared supported baseline and 90 Hz where display/headroom qualify, beginning with 60 Hz qualification on a named integrated-GPU laptop and a named mid-range physical mobile device. Record CPU/GPU, RAM, OS, browser/version, power mode, thermals, viewport, device pixel ratio and actual render scale. Use Chrome through Playwright for mandatory project UI proof, with Firefox and Safari on applicable real platforms for their additional support-tier qualification; Playwright WebKit is supplemental, not physical iOS Safari qualification. Refresh-rate and device-emulation settings do not emulate mobile hardware. The multi-domain section below adds 90 Hz budgets and the broader device/workload matrix.

| Metric | Binding acceptance limit / interpretation |
| --- | --- |
| End-to-end pacing at a controlled 60 Hz display | Sustained distinct-frame rate ≥59.4 FPS and missed scheduled cadence slots ≤1% over each full normal active capture; p95 frame interval ≤18 ms; p99 ≤25 ms; report fraction exceeding 20 ms and 33.3 ms, plus maximum. |
| CPU work per displayed frame | p95 ≤10 ms total: initially reserve 5 ms for CPU host/orchestration service across all physics/gameplay ticks (WGSL physical computation is accounted as GPU work, never moved to a CPU solver), 2 ms for presentation/UI and 3 ms for render submission/other app work. |
| Physics cost at 120 Hz | p95 ≤2.5 ms per complete gameplay tick, including all four outer steps, networks, events, rollback preparation and publication; also measure actual attributable service between presented frames during catch-up. |
| GPU time | p95 ≤8 ms where asynchronous GPU timing is supported and valid. CPU and GPU may overlap; do not add these percentiles to claim frame time. |
| Interaction | p95 pointer/key-to-visible-response ≤100 ms during construction and Run/Reset; time actual canvas interactions, not DOM event delivery alone. |
| Allocation and memory | Zero routine simulation/animation scratch allocation after warm-up for stable topology; enumerate unavoidable allocations/copies and predeclare heap/retained-byte caps. No increasing live state across 20 identical Run/Reset and Save/Load cycles plus 20 level transitions; record each runtime heap, Wasm capacity and GPU resources separately. |
| Startup | Measure compressed transfer bytes, cold download, Wasm/runtime startup and first usable construction frame separately. Required ≤5 s on a documented 20 Mbps / 50 ms RTT profile; retain cache state and payload size. |
| Hitches | No unexplained application work >50 ms after warm-up; retain shader compilation, GC, memory growth and reset spikes as separate categories. |

For every full normal active capture, require sustained distinct-frame rate ≥59.4 FPS for the 60 FPS tier and ≥89.1 FPS for the 90 FPS tier: an explicit 1% pacing tolerance around nominal cadence. Missed scheduled cadence slots must be ≤1%. P0-003 must predeclare the slot schedule, distinct-presentation measurement, observability and capture boundaries for the target display. Retain every normal active stall in the denominator and raw evidence; duplicate frames, repeated callbacks, selected windows or discarded stalls cannot inflate the result. Missing presentation/slot observability leaves the tier incomplete. These rate/slot gates supplement every existing p95/p99, CPU/GPU, freshness and throughput limit.

CPU/frame budgets account for raw attributable service from simulation, animation, transport and browser presentation. The 5/2/3 ms allocation above remains a planning allocation within the 10 ms total, not permission to sum independent percentiles or omit worker costs. Report concurrent elapsed critical-path time separately. Apply the worker-specific 2 ms animation and 1 ms bridge limits and freshness/debt gates from TODO as well.

Use a diagnostic Release bundle for stage timings and counters, then repeat timing on a diagnostics-disabled production Release bundle. Measure instrumentation overhead. Native microbenchmarks help isolate algorithms but never replace browser results.

Record tick/substep/event counts, simulated seconds versus wall seconds, candidate counts, contact rows, predictor/solver iterations, largest coupled group, snapshot bytes, allocation bytes, GC pauses where observable, draw calls, triangles, material changes and mesh upload bytes. Use enum-typed stage/metric/scenario identifiers and typed body/part/revision identifiers through collection and reporting APIs. Convert to validated serialized labels only at the diagnostic boundary; unknown values must fail.

Use [Chrome DevTools traces](https://developer.chrome.com/docs/devtools/performance) to correlate CPU work, frame pacing and memory. [Long Animation Frames](https://developer.mozilla.org/en-US/docs/Web/API/Performance_API/Long_animation_frame_timing) report frames exceeding 50 ms; their absence cannot prove a 16.67 ms target, and API availability varies. Capture frame intervals and engine stage timers independently. Do not poll GPU state synchronously to instrument it.

## Multi-domain architecture and 60–90 FPS qualification

The standing requirement is sustained **60 FPS across a declared broad browser/device matrix, with 90 FPS where display cadence and measured headroom permit**. These binding targets are not a claim that either rate is currently qualified. A 60 Hz display cannot show 90 distinct frames/s; browser callbacks normally follow display refresh and may be throttled. Measure actual cadence, including 90/120 Hz devices, instead of assuming a requested rate is available. See [requestAnimationFrame timing](https://developer.mozilla.org/en-US/docs/Web/API/Window/requestAnimationFrame).

At 60 FPS the nominal frame interval is 16.67 ms; at 90 FPS it is 11.11 ms. Retain the binding 60 Hz budgets. The binding 90 Hz limits are: sustained distinct-frame rate ≥89.1 FPS and missed scheduled cadence slots ≤1% over each full normal active capture, p95 frame interval ≤12 ms, p99 ≤16.7 ms, p95 total app CPU ≤7 ms and GPU ≤6 ms where measurable. These are required acceptance thresholds, not demonstrated guarantees. Report missed-refresh proportions, worst frames and sustained simulated-time/wall-time ratio. CPU/GPU work overlaps; separate stage percentiles do not prove frame pacing. At 120 Hz simulation and 90 Hz rendering, two ticks may complete between successive presented frames: measure the actual CPU/publication burst, not only the 1.33-tick average. Those ticks execute in the simulation worker, never inside the render callback. Do not change simulation results according to monitor refresh.

### Shared infrastructure, specific physical models

Extend the existing [generic interaction and subsystem requirements](planning/requirements.md#subsystem-dependency-closure), not a parallel physics authority. Parts declare typed capabilities, material laws, body-local geometry, ports, sources, receivers and finite stores. Shared services own identities, committed transforms, spatial queries, scheduling, conservation and rollback. Each domain evaluates its own laws. The following is a coverage checklist, not a replacement for individual element specifications.

| Domain | Required model and main performance strategy |
| --- | --- |
| Rigid, jointed, elastic, rope, granular | Shared continuous contact/constraint pipeline, spatial hierarchy, local coupled groups and qualified sleep. Granules remain physical conserved material where required; cosmetic particles cannot replace functional grains. |
| Electrical, logic, timers and controls | Cache typed connection graphs; process changed inputs and scheduled events, while updating finite stores and continuous consumers over elapsed simulated time. Feedback requires an explicit validated solution, not arbitrary traversal order. |
| Water, hydraulic and open-flow transport | Follow the authored finite-volume/network or free-flow contract; conserve mass, momentum/energy where modeled, species and enthalpy. Solve active connected regions. Do not force every droplet into a rigid body or replace specified flow/jamming behaviour with an animation. |
| Gas, pneumatics and wind | Finite pressure/storage and flow networks where specified; bounded jets, influence regions and exposed receiver samples for wind. Re-evaluate moving blockage and coupled pressure/work feedback. Rendering ribbons are cosmetic. |
| Thermal, phase and chemical | Track finite enthalpy/species/reactants, conductance graphs, losses and threshold events. Use validated integration and exact/event-aware boundaries for melting, ignition, depletion and topology changes. |
| Visible light and optics | Typed surfaces/channels, ordered intersections and qualified reflection/transmission/splitting models. Index emitters, receivers and blockers; reuse unchanged paths without missing newly entering blockers. |
| Sound | Scheduled pulses and the specified propagation, obstruction and material response. Do not implement full wave simulation unless a required element needs it; never substitute an unqualified binary model for required transmission/diffraction. |
| Ionizing radiation | Separate photon, charged-particle and neutron models from the research contract. Material intervals, energy groups, exposure integration and curved interception where specified; no visual photon-per-particle simulation requirement. |
| Magnetic/electric fields and other forces | Typed source/receiver capabilities, conservative spatial bounds or explicitly bounded approximation error, force integration and coupled energy accounting. An inverse-distance field has no exact finite cutoff unless the authored model declares one. |

Document every required element's domain dependencies, geometry/query needs, supported abstraction and expected maximum workload before declaring engine coverage complete. Full computational fluid dynamics or particle transport is not a blanket requirement, but no required behaviour may be silently removed to save time.

### Geometry and query contracts across domains

Maintain separate render geometry, contact geometry and interaction surfaces/volumes, sharing immutable geometry only where their accuracy requirements agree. One authoritative body pose drives all of them. A transparent solid, optical coating or radiation shield can have different interaction properties from its contact material. Define domain participation independently from mechanical collision participation.

Generalize spatial indexing to include source influence regions and non-solid receivers, with enum-typed capability filters and typed geometry/material IDs. Internal aggregate bounds must enclose every relevant indexed representation; a smaller contact-only bound cannot prune a larger interaction volume. Bounds select candidates only. Nearest-hit queries serve true opaque blockers; ordered surface hits and material entry/exit intervals serve transmission. Union overlapping pieces of the same material volume before accumulating path length, while preserving genuinely separate layers and hollow spaces. Handle inside origins, touching boundaries, grazing paths and moving shutters explicitly.

A nearest-obstruction-distance query alone cannot implement the required radiation/volumetric transport contract. Preserve exact geometry queries while sharing traversal/scratch buffers; eliminate unnecessary temporary body/trajectory allocations from point-ray queries only after profiling and equivalence tests. [Light transmittance](https://pbr-book.org/4ed/Volume_Scattering/Transmittance) and [NIST photon attenuation](https://physics.nist.gov/PhysRefData/XrayMassCoef/chap2.html) support path-dependent attenuation; they do not justify one common response formula for every domain.

### Scheduling and conservation

Compute conservative subsystem dependency closure from the existing TODO contract, including inventory, environment, sensors, reachable phase/reaction/spawn products and edits. A subsystem absent from the closure needs no stepping/storage. Mechanical sleep does not suspend source emission, timer events, heat exchange or accumulated exposure.

Use a deterministic shared simulated-time schedule. Different domain update intervals are allowed only with stated error bounds, preserved accumulated quantities and event handling for brief pulses or threshold crossings. Never downsample authoritative interactions simply because FPS fell. Continuous forces and rapidly coupled pressure/contact processes need evaluation at the relevant predictor stages; slow independent stores may permit larger certified integration intervals.

Read a consistent state, propose bounded transfers, resolve shared supply limits, then commit atomically. Strongly coupled feedback needs a qualified coupled solve or controlled substeps; a single sequential pass can invent energy or introduce one-tick delays. Source-to-many-consumer allocation must be deterministic and conservation-tested. Save/Reset/failed-step restoration includes pending events, accumulated quantities and all domain state.

### Capacity and browser coverage

Benchmark single-domain scenes and mixed chains: electricity → motor → contact/friction → heat; light → heating → phase/reaction → flow; pressure → actuator → moving occluder; radiation → exposure sensor → powered gate. Include a source with many receivers, many overlapping sources, long optical paths, connected fluid networks and dense contact groups. Record domain-specific sizes (bodies/children, constraints, edges, sources, samples, path branches, material intervals and active reactions), not just part count.

Establish a measured supported workload envelope for each real browser/device class, including low/mid-range Android Chrome, iOS Safari and integrated-GPU Windows/macOS/Linux combinations with Chrome/Edge, Firefox and Safari where applicable. Record exact versions and capability requirements. Use sustained sessions of at least five minutes with repeatable UI Run/Reset cycles to expose thermal degradation; separate active windows and lifecycle costs. A stress scenario is not per-element proof.

Use explicit presentation-quality settings only for cosmetic cost. Document overloaded/unsupported conditions and retain failures; never silently remove a physics subsystem, shorten a field or reduce interaction accuracy. If the broad supported-device baseline cannot meet 60 FPS at the required workload, optimization or an explicit supported-envelope decision remains open. A successful desktop 90 FPS run cannot close mobile/browser qualification.

## Prioritized engine changes

### 1. Complete and qualify the world-level conservative broad phase

The [body-index acceptance](planning/requirements.md#sequence-task-070) retains evidence of the body hierarchy added after the historical inspection. Continue from that implementation: qualify integration, all required domain queries, comparative browser performance and current affected-part behavior. Do not recreate a replaced all-pairs path merely to follow the old research order.

Apply PERF-03a–03d's separate artwork/contact declarations and PERF-18's interaction geometry. A conservative enclosing sphere is an optional early rejection stage within spatial traversal, never a replacement collider for non-spherical parts. Overlap only admits a candidate; the real convex pieces establish contact. Certify bounds across the full motion horizon, preserve hollows and thickness, and measure sphere overhead versus tighter AABBs for long thin shapes. Do not add an all-pairs sphere loop.

Complete and qualify the persistent 3D body AABB hierarchy above the compound-child tree, including static geometry and moving proxies. Finalize all required domain geometry/query contracts before closing the index. Compare a dynamic tree with sweep-and-prune only as a measured implementation experiment; ship one qualified production path, with no runtime selector or retained all-pairs fallback. The TODO register controls when these bounded changes execute.

[Box2D's collision documentation](https://box2d.org/documentation/md_collision.html) describes hierarchical AABB pruning and reuse for spatial queries. The applicable principle is eliminating distant candidates before expensive geometry work, not adopting its 2D collision rules or assuming all angular CCD is solved.

For this engine, bounds must enclose the complete captured trajectory over the queried horizon, including acceleration, angular reach, offset children and numerical reserve. Endpoint boxes alone are insufficient. Refit after impulses, position projection, prescribed motion, geometry/participation updates and Restore. A fat proxy is safe only while it contains the newly certified swept bound. Include prescribed/prescribed feasibility checks and spatial sensors/traces where their query contracts require them.

Emit canonical typed body/child pairs in deterministic order. Preserve active-contact release processing even when a pair leaves the near set. Filter by typed participation and joint policy before narrow phase. Treat tree nodes as derived state: snapshot them or rebuild deterministically after rollback.

Acceptance: sparse size-doubling scenes must show materially fewer body-pair visits than all-pairs; also measure dense scenes where quadratic overlap may be unavoidable. Compare against exhaustive candidate enumeration in tests only, including pure rotation, fast opposing bodies, moving gates, hollow compounds, disable/re-enable, resize and Reset. Never prune by camera visibility.

### 2. Remove repeated allocation and redundant reconstruction

Introduce world-owned reusable scratch buffers and dense typed-index storage for candidate lists, bound caches, force vectors, constraint rows and predictor stages. Keep immutable declarations shared. Cache body lookup/topology order at explicit revision changes instead of rebuilding dictionaries or sorting identical sets inside every event. Prioritize measured hot paths in CandidatePairs, CurrentPairs, AccelerationSolver.Predict, Capture and ReplaceLoads; do not indiscriminately rewrite cold authoring code.

Retain immutable public results and explicit buffer lifetimes. A ReadOnlySpan must not expose scratch that a later operation mutates while a consumer still relies on it. Separate step rollback storage, externally retained save/Reset snapshots and presentation snapshots. Bounded reusable rollback storage or a transactional journal may reduce copying, but must restore every owned ledger, contact cache, event, clock and effect after failure. Measure pool high-water marks; clear retained references and reject unsafe reuse/reentrancy.

Reuse trajectory/constraint work only with keys covering pose, velocity, geometry, topology, loads and prediction horizon. A changing force law invalidates prediction even if the body's current pose is unchanged. Preserve canonical ordering and the approved f16 error contract; do not silently widen the solver or relax its new acceptance limits.

Acceptance: allocation profiles before/after, stable live memory across lifecycle cycles, exact same-build replay, forced-failure atomic restoration and invalidation controls. A full-world snapshot may still be correct and cheaper than complex journaling for small scenes: select from evidence.

### 3. Localize coupled work and introduce qualified island sleep

The engine already has local mass/constraint grouping work; audit those paths before adding another graph. Extend reuse toward complete independent simulation islands only when contacts, joints, ropes, transmissions, compliant loads, airflow coupling and shared energy constraints are represented correctly. A static floor must not connect all resting bodies into one dynamic solve. A supposedly local solve must not omit a nonlocal dependency.

[Box2D's island analysis](https://box2d.org/posts/2023/10/simulation-islands/) explains island-wide sleep, immediate wake propagation and why nondeterministic ordering can destabilize warm starts. Apply these principles to this engine's richer dependencies. Sleeping by individual velocity alone is unsafe for loaded mechanisms.

Require persistent equilibrium and bounded residuals before sleeping an entire eligible island. Wake for contacts, changed force/supply, motor/servo commands, prescribed motion, support removal, topology/material changes and energy releases. Timers, sensor residence, trigger delivery and scheduled gameplay continue even when mechanical state sleeps. Do not sleep a pressurized, creeping or spring-loaded mechanism unless its equilibrium and all wake sources are qualified.

Keep a shared event clock initially. Local caching may avoid rebuilding unrelated predictors, but independent time advancement needs separate proof against cross-island collision and earliest-event ordering.

Acceptance: resting stacks, taut/slack ropes, powered stalls, spring release, conveyor cargo, sensor dwell and wake propagation; same results with sleep eligibility denied as a test control. Record active versus sleeping work and the largest island; no promised benefit for one dense connected machine.

### 4. Reduce solver work without weakening certificates

Retain persistent contact identities and warm starts with explicit invalidation. Cache symbolic sparsity and reusable workspaces; numeric factorization reuse requires unchanged effective coefficients, not merely unchanged topology. Rotation, lever arms, contact normals, active unilateral rows and mass/inertia changes can invalidate it.

Profile grazing contact, slip transitions, stiff springs and nonlinear support separately. Improve conservative interval bounds and reuse valid support information before changing event budgets. Box2D's [simulation discussion](https://box2d.org/documentation/md_simulation.html) is useful background on persistent contacts and substeps, but its recommended rates and CCD policies do not qualify this engine.

Preserve 120 Hz and four outer substeps for the worker cutover. Retain outer-step consolidation as a separate conditional experiment after removing redundant allocations/presentation: determine whether the continuous solver can own the necessary subdivisions without four unconditional scene-level preparations. Preserve tick-timed logic, force integration, motor/work budgets and earliest-event semantics. A lower tick rate or changed step policy requires fresh correctness and affected campaign evidence, not an FPS-only decision.

Never resolve overruns by dropping contacts, skipping rotational CCD, loosening penetration/energy tolerances, treating hollow geometry as solid, truncating accepted work or swallowing convergence errors. Explicit failures and retained evidence remain required.

## Scheduling and presentation

**Animation is independent of physics.** Follow the [animation ownership contract](simulation-presentation-bridge.md#independent-animation-system) and PERF-36–38. Cosmetic motor spin, easing, recoil and UI loops need no physical body, solver integration or angular-travel state merely to render. The separate C# animation worker owns their phase/clocks and evaluates on its independent clock, initially 60 Hz, using optional coarse state/events. Only final scene-property application is paced by rendered frames; neither rendering nor physics calls drive animation evaluation. Keep actual contact/interaction poses authoritative, preserve real mechanical work where required, and prove physics results unchanged with cosmetics disabled. Count animation CPU/dirty updates inside presentation budgets; active animation still requires frames while physics is idle.

The [bridge contract](simulation-presentation-bridge.md) is normative for command order, coherent generation/revision-stamped reads, reliable transient events, buffer leases, delta-gap recovery, bounded backpressure and Reset/Load/save barriers. Establish full gameplay rollback before publishing atomic snapshots. Presentation may skip obsolete pose samples but not required events/results; authoritative queries and saves never read interpolated state.

The simulation, animation and render clocks must be independent: fixed 120 Hz simulation, initially 60 Hz animation evaluation and display-paced 60/90 Hz presentation. Worker timer callbacks wake bounded schedulers; monotonic elapsed time and integer simulation ticks define progress. Map clock origins explicitly and stamp generations; presentation samples worker histories instead of Godot's physics interpolation fraction. [Fixed-timestep analysis](https://gafferongames.com/post/fix_your_timestep/) explains accumulated time and the catch-up spiral; [Godot's interpolation introduction](https://docs.godotengine.org/en/stable/tutorials/physics/interpolation/physics_interpolation_introduction.html) explains presentation between physics ticks.

Publish changed body poses and visual telemetry once per completed gameplay tick, then apply visible transforms at most once per rendered frame. Audit all ObservePhysics, effect and network consumers before moving Present: consumers needing current physical transforms must read authoritative shared-world state, never an interpolated node. Godot documents that many [C# node property accesses cross native interop](https://docs.godotengine.org/en/stable/tutorials/scripting/c_sharp/c_sharp_basics.html); batch writes and cache repeated reads at the presentation boundary.

Interpolate presentation from bounded timestamped committed histories at one selected display time, using position and appropriate orientation interpolation. Do not interpolate back into collisions, ray queries, sensors, force inputs, save state or construction coordinates. Remove competing engine-node interpolation. Predeclare the interpolation delay and measure total snapshot/sample age, including transport; do not assume it is exactly one tick. Reset, Load, spawn, teleport and rollback must reseed both displayed states; no sweep from an old location.

Use a bounded simulation-worker catch-up policy that yields to its command/lifecycle mailbox while browser input/rendering proceeds independently. Preserve unprocessed simulated-time debt and report sustained overload; never advance the game clock without solving it. If overload cannot be recovered, pause explicitly at a committed boundary with actionable status instead of silently slowing physics or accumulating unbounded debt.

Define hidden-tab behavior explicitly: pause at a committed boundary, discard only the wall-time interval intentionally spent suspended, and resume without a catch-up burst. [requestAnimationFrame documentation](https://developer.mozilla.org/en-US/docs/Web/API/Window/requestAnimationFrame) notes background throttling and different display rates. Test hidden/visible transitions and 30/60/120 Hz presentation without changing the simulation contract.

## Rendering changes for the existing visual style

### Remove unchanged geometry work first

Use reusable previous/current committed presentation snapshots and typed dirty revisions. This is state buffering, not another full-frame image buffer. Trial idle-frame suppression only when camera/UI/effects and all visible dependencies are settled; request a final settled frame and redraw for input, simulation, topology/resource changes, resize/DPR and visibility/context restoration. Paused physics is not sufficient. Full 3D dirty rectangles remain conditional: depth, revealed background, shadows, transparency and camera changes invalidate more than the moving object's current rectangle.

Add typed geometry/pose/power revision tracking to optical, rope, belt and cone presentation. Recompute rope sag or clipped cone meshes when their inputs change; animate only the moving marker/material data when geometry is stable. Optical visibility depends on source and blocker changes, not only electrical power. Cosmetic sampling may be reduced only under a declared, visually qualified policy; authoritative light/airflow/sensor calculations retain their required timing.

Replace per-frame CylinderMesh.Height edits with transforms of shared unit geometry where equivalent. Use shared immutable meshes/materials for identical decorative parts; update instance data for colour/state, without accidentally sharing mutable per-part material state. For deforming surfaces, retain topology and update vertex regions through a supported Godot API after verifying the pinned browser implementation. Measure upload bytes and interop calls before choosing mesh buffers over simple nodes.

### Batch repeated artwork and preserve culling

PERF-28–30 also require projected-size decorative mesh budgets, stable visual LOD, same-material merging of rigidly co-moving details and immutable art-resource reuse. Preserve independent moving mechanisms, selection ownership, culling bounds and physical/interaction geometry. Suspend offscreen cosmetic work only when shadows/reflections/UI have no visible dependency; restore current appearance without replaying missed cosmetic frames. Physics, emission and accumulated stores continue offscreen.

Qualify first-use material/effect warm-up in Compatibility by rendering representative required combinations during loading; merely instantiating hidden assets is not proof. Forward+/Mobile pipeline precompilation and shader baking do not establish support on this WebGL renderer. Record cold startup, first-use hitches, retained resources and warm-up cost; avoid unbounded preload. See [Godot shader-compilation guidance](https://docs.godotengine.org/en/stable/tutorials/performance/pipeline_compilations.html).

The current renderer is Compatibility. Godot's [3D optimization guidance](https://docs.godotengine.org/en/stable/tutorials/performance/optimizing_3d_performance.html) limits automatic instancing to Forward+; simply reusing a mesh does not establish automatic batching here. Trial explicit MultiMesh for repeated rope segments, pins, guard details and other identical artwork.

[MultiMesh guidance](https://docs.godotengine.org/en/stable/tutorials/performance/using_multimesh.html) describes bulk instance rendering and the lack of individual-instance visibility culling. Group by mesh/material and sensible spatial bounds, not one world-sized batch. Retain typed part-to-instance mapping for selection and updates; physics bodies remain independent. Compare draw-call savings against buffer upload, culling and interaction cost.

### Control fill rate, transparency and shadows

Profile full-screen pixel cost as well as object count. Overlapping translucent light cones, optical beams, guides and sound waves can dominate small dioramas. Reduce redundant transparent layers and bound effect coverage while preserving required see-through geometry, channel colours and feedback. Keep decorative beams/waves shadow-free as designed. Do not replace open tubes or readable guides with opaque art merely to lower overdraw.

Godot's [GPU optimization guidance](https://docs.godotengine.org/en/stable/tutorials/performance/gpu_optimization.html) motivates testing resolution, lighting and shader costs separately. Benchmark shadow distance/resolution and affected casters; use static lighting only for genuinely static scenery. Moving parts still need the visual grounding required by DESIGN.md. Avoid adding expensive occlusion structures before confirming that this mostly open diorama has meaningful hidden geometry.

Record device pixel ratio and cap backing-buffer resolution through an explicit rendering policy if high-DPI fill rate dominates. Trial render scale and MSAA independently. Presentation quality settings must be enums, selected explicitly and visually qualified; they must never alter physics or silently downgrade behaviour. Preserve the palette, silhouette, socket readability and interaction target size.

Follow [MDN's WebGL guidance](https://developer.mozilla.org/en-US/docs/Web/API/WebGL_API/WebGL_best_practices): batch work and avoid synchronous GPU readback/state queries in hot loops. Use engine-supported asynchronous timing if available; otherwise report GPU time as unavailable, not zero. Do not modify Godot's WebGL state externally. Separate screenshot/video evidence runs from timing runs so captures do not inflate the benchmark.

## Wasm deployment and conditional experiments

1. **Replace the synchronous baseline with independent worker runtimes.** PERF-39–48 require a dedicated simulation worker with C# discrete host and WebGPU/WGSL f16 numerical authority at 120 Hz, a separate C# animation worker initially at 60 Hz, and independently display-paced browser-main Godot presentation. Owned transferable buffers, bounded transport and asynchronous lifecycle acknowledgements must be implemented and measured. Preserve the current severe stalls as baseline evidence. Shared-memory threading, Task.Run and OffscreenCanvas are not prerequisites of this accepted main-thread renderer architecture. Missing worker capabilities or startup failures produce explicit unsupported/error states, never a synchronous fallback.
2. **Evaluate managed AOT only after checking the pinned toolchain.** Microsoft's [.NET WebAssembly AOT guidance](https://learn.microsoft.com/en-us/aspnet/core/blazor/webassembly-build-tools-and-aot?view=aspnetcore-10.0) documents CPU-throughput versus payload/startup tradeoffs for Blazor. This is a plain 2dog host: first confirm the relevant targets execute and inspect output, then compare production CPU profiles, compressed bytes, load time, memory, reflection/serialization and Reset. Do not paste a Blazor property and label AOT complete.
3. **Measure vectorization rather than enable an existing flag.** [.NET SIMD guidance](https://learn.microsoft.com/en-us/aspnet/core/blazor/performance/webassembly-runtime-performance?view=aspnetcore-10.0) provides runtime context. CPU host/compiler batches may benefit from vector-friendly layouts. Physical kernels use the required f16 GPU path and deterministic staged reductions; verify generated shaders and complete-system improvement, including readback and WebGL contention. Avoid scalar/SIMD runtime compatibility branches.
4. **Treat startup and memory as separate performance work.** Verify negotiated Brotli/gzip responses, correct Wasm MIME type and versioned cache invalidation on the deployed host. Precompressed files alone do not prove compressed delivery. Measure engine/runtime/content downloads separately. Preserve required reflection roots when trimming. Remove non-game exports only after inspecting the pack.
5. **Size memory from evidence.** Record linear-memory growth and live managed/native/GPU resources through load, construction, Run, Reset and level switching. Increasing the initial heap does not repair allocation churn or leaks and can exclude mobile devices. Changing the pinned 256 MB default requires low-memory browser proof.

GPU physics with WebGPU/f16 is the selected architecture, not a conditional experiment. Shared-memory threading, a WebGPU renderer replacement and optional host AOT/SIMD remain separately measured choices. Continue only the C# discrete host/compiler and canonical Half model extraction needed by the GPU path; do not extend CPU numerical authority.

## Reproducible verification and delivery

Establish the following named scenario groups with enum identifiers in the C# Playtest tooling. Record actual placed parts, modes, geometry, connections and active contacts; part count alone does not describe solver cost.

| Scenario | Purpose and required control |
| --- | --- |
| Empty/idle workshop and paused construction | Baseline render/input overhead; unchanged geometry should not rebuild continually. |
| Sparse catalogue scene, then doubled population | World-level broad-phase scaling; distant bodies must not create narrow-phase work. |
| Dense stack/queue and one connected mechanism | Worst coupled solve, contact persistence and sleep/wake; include disturbed-support control. |
| Fast launch, pure rotation and moving hollow compounds | CCD/candidate correctness under load; include clear-endpoint/intermediate-hit cases and misses. |
| Rope/gear/motor/servo/compliant/airflow assembly | Nonlocal coupling, stiff prediction, work budgets and stalled/released controls. |
| Multiple cones, optical paths, ropes and belts | Transparent overdraw, mesh updates and draw calls; compare unchanged versus moving blockers. |
| Repeated lifecycle and visibility transitions | Exact Run/Reset, Save/Load, level change, pause/resume, background/foreground and resource lifetime. |

For each applicable scenario:

1. Build through actual palette, placement, rotation and connection controls. Retain failed attempts. Diagnostic observation is read-only: no injected solutions, game-state setters or numeric placement menus. Diagnostic fault fixtures may delay transport/scheduling only, never create gameplay success, and must be absent from production.
2. Follow TODO's [runnable measurement contract](planning/requirements.md#measurement-contract): warm for at least 10 wall-clock seconds through preceding complete UI attempts, then capture at least three matched complete fresh Runs. Timeout cases reach the unchanged 3600-tick / 30-simulated-second endpoint; early-goal cases retain their full shorter window and predeclared minimum observations and cannot satisfy that timeout case. Report simulated and wall durations separately. Five-minute thermal evidence is a continuous real-UI session timeline of repeated attempts with every gap and lifecycle interval retained, per-attempt active budgets and separately measured lifecycle costs. Never stitch frames, pad stopped simulation or override timeout/goals to claim sustained active FPS. Long standalone harness captures remain separately labelled and cannot qualify UI, display FPS or production-origin behavior.
3. Retain distributions and raw records for each run; compare matched devices, scenes, build settings and thermal conditions. Alternate before/after runs when practical. A stage improvement must exceed observed run-to-run variation and must not regress frame tails, startup or correctness.
4. Capture separate screenshots and continuous motion evidence for interpolation, contacts and visual readability. Record positive, negative/control and integration assertions for every affected part/mode, typed connection checks and exact construction restoration. An aggregate stress scene supplements those proofs.
5. Test candidate completeness and rollback natively, compile affected callers, publish production Release and exercise that actual bundle. Verify unsupported protocol/configuration values are rejected at changed boundaries.
6. Store revision/diff hashes, commands, action recipes, hardware/browser metadata, stage timings, frame intervals, simulated/wall durations, allocation/resource counts, console failures, traces and captures. Mark unavailable metrics and missing/stale proofs explicitly.

### Implementation queue and completion gate

The [authoritative execution register](delivery-workflow.md#pipeline-priority) supplies the order and prerequisites. Its P0-035 gate completes physics, rendering and both workers before product, component or campaign work. The retained PERF-01–48 tasks and PERF-03a–03d preserve generalized-process coverage, render assets, the command/read bridge, worker transport and independent animation requirements. Engine obligations qualify against current parts and generic UI-placeable fixtures; later product-integration audits do not create a dependency on unimplemented catalogue elements. The grouped items below summarize the preserved scope and do not provide a competing schedule.

All entries below remain open; documentation research does not implement them.

- [ ] Add enum-typed performance instrumentation and capture the production browser/device baseline.
- [ ] Complete integration and qualification of replacement conservative spatial indexing; remove any residual all-pairs world traversal.
- [ ] Reduce hot-path allocation and predictor/snapshot reconstruction while preserving ownership and atomic failure.
- [ ] Separate committed simulation publication from per-frame presentation; qualify interpolation and lifecycle discontinuities.
- [ ] Cache unchanged visuals, share appropriate geometry and trial explicit instancing; qualify transparency/shadow/resolution costs.
- [ ] Extend measured coupled-work reuse and qualify sleep/wake without losing sensor, energy or event semantics.
- [ ] Evaluate outer-step consolidation, numeric reuse and managed AOT only against the above correctness/performance gates.
- [ ] Verify startup delivery, stable memory and browser/device budgets on the production bundle.
- [ ] Re-run every affected part's real-UI proof; publish verified increments and keep unresolved work in TODO.

Finish engine instrumentation, shared improvements and mandatory qualification before closing P0-035 and beginning product/component/campaign work. Keep current-part and generic-fixture correctness, production builds and focused UI proof within that engine gate. Afterward, every implementation, content and tooling change must preserve the [standing requirements](delivery-workflow.md#standing-requirements), meet the same applicable ownership/performance/proof contracts and refresh affected evidence. Defer exhaustive campaign/difficulty sweeps until component coverage, then include their largest taught machines in final performance qualification. Preserve all campaign and later integration scope governed by the current user instructions and TODO.

Apply common/profile proof according to the [task stage gates](delivery-workflow.md#stage-gates). Element I and CLEAN rows close their scoped implementation, build, boundary, cleanup and applicable existing-behavior regression checks; they do not require the later U row's new-element UI proof or the retained closure row's publication. U owns its complete named positive/control/integration/Reset/save/motion proof; the retained closure owns final evidence reconciliation and publication. An intermediate stage pass never closes the element or waives any later gate, and P0-035 still requires all scheduled engine qualification.

Incremental optimization rows close only their declared scoped improvement/no-regression checks, preserving any unresolved release-budget failure for its named qualification gate. P0-035 and supported releases require the full applicable browser budgets and current proof; native benchmarks alone cannot close them. Completion also requires removal of superseded paths, enum/typed-identifier review through callers/tests, no stale affected-part proof and preserved DESIGN.md appearance.
