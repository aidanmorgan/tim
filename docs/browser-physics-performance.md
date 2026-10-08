# Release performance checklist

This is the measurement companion to standing requirements [REQ-02, REQ-04, REQ-05 and REQ-10](delivery-workflow.md#standing-requirements) and the [game-grade envelope](gpu-f16-physics.md#game-grade-envelope). **Every number here is a release-checklist item enforced at the P0-034 stage gate** ([stage gates](delivery-workflow.md#stage-gates)) and at the LEGACY-0 and CAMPAIGN rows of the [ordered roadmap](planning/invest/vertical-delivery.md#rolling-playable-roadmap). None is a per-slice gate: under the owner's [playable-first decision](delivery-workflow.md#playable-first) a slice needs ordinary correctness, actual Chrome behaviour, the production build and obviously playable responsiveness. P0-032 owns fault/concurrency campaigns; P0-035 engine closure and the final S847 release repeat the full applicable set. This document establishes no speedup or supported tier; measurements belong in the owning slice record. Retained IDs: PERF-01–48, PERF-03a–03d, PERF-23–27, PERF-28–30, PERF-31–48, PERF-36–38, PERF-39–48, with their detail in the [requirements](planning/requirements.md#worker-performance-budgets).

## Architecture under measurement

Three systems, one production pipeline: the physics worker (C# WebAssembly host plus generic WASM SIMD128 f32 solver over the compiled initial state, fixed 120 Hz with 480 Hz substeps), the animation worker (own compiled model, own clock, currently 60 Hz, fed one way by committed physics) and the main-thread renderer (universal WebGL 2 and WebGPU instanced draw batches, display-paced at 30–60 FPS) reading both. Rates are ordered physics > animation ≥ renderer; rendering never influences either worker. The zero-copy `SharedArrayBuffer` triple pose ring eliminates GPU-to-CPU readback stalls (`mapAsync` stalls). Missing required browser features (`SharedArrayBuffer`, WebAssembly SIMD) is an explicit unsupported state, never a browser-thread fallback.

Complete-tick budgets include dispatch, synchronisation, transfer, networks, events, restoration preparation and publication; GPU budgets include instanced vertex transformation and fragment rendering. There is no solver-only substitute, no selectable old/new solver, no silent quality downgrade and no dropped simulation work. Never resolve an overrun by dropping contacts, enlarging the solver dt, loosening slop beyond the envelope, treating hollow geometry as solid, truncating accepted work or downsampling authoritative interactions because FPS fell.

## Release budgets (P0-034)

| Metric | Release-checklist limit |
| --- | --- |
| Frame pacing (REQ-02) | Renderer 30–60 FPS on supported devices: across each full normal active 30 s Run, no window longer than 1 s presents fewer than 30 distinct frames/s; ≤1% missed scheduled cadence slots at the device's 60 Hz target; at 60 Hz p95 frame interval ≤18 ms and p99 ≤25 ms. Count distinct presentations only; repeated frame IDs, extra callbacks, screenshots and synthetic cadence cannot inflate delivery. P0-003 freezes the slot schedule, distinct-presentation measurement, observability and capture boundaries for the target display. |
| Physics tick (REQ-04) | Complete gameplay tick p95 ≤2.5 ms at 120 Hz including all four substeps, networks, events and publication; also measure attributable service between presented frames during catch-up. The measured ≈3 ms whole-tick p95 is a retained Fail under P0-034. |
| Animation and bridge (REQ-04/05) | Animation evaluation p95 ≤2 ms; bridge CPU service p95 ≤1 ms per presented frame (command drain, publication, packing/unpacking, copies, final application). |
| Application CPU (REQ-04) | Aggregate p95 ≤10 ms per displayed frame at 60 Hz (planning allocation 5 ms host/orchestration, 2 ms presentation/UI, 3 ms render submission); GPU p95 ≤8 ms where asynchronous GPU timing is valid. Aggregate raw attributable samples; never add percentiles or overlapping spans. |
| Freshness and interaction (REQ-05) | Snapshot and animation sample age p95 ≤33.3 ms / p99 ≤50 ms; pointer/key-to-visible feedback and pending indication p95 ≤100 ms, timing actual canvas interactions. |
| Independent clocks (REQ-03) | Same-build replay agrees at equal applied tick/phase/order across animation 30/60/90/120 Hz and render 30/60/90/120/144 Hz, including pauses, stalls and overload; sustained simulated/wall-time ratio 0.99–1.01 without growing debt. |
| Memory (REQ-10) | Zero routine warmed simulation/animation scratch allocation at stable topology; after 20 Run/Reset + Save/Load cycles and 20 level transitions no growth in live worlds, workers, listeners or leased buffers; retained bytes within the predeclared tolerance. Record each runtime heap, Wasm capacity and GPU resources separately. The pinned 256 MB initial heap changes only with low-memory browser proof. |
| Startup | Usable workshop within 15 s cold / 5 s warm on the dev machine; ≤5 s on a documented 20 Mbps / 50 ms RTT profile. Measure compressed transfer, cold download, runtime startup and first usable construction frame separately. |
| Hitches | No unexplained application work >50 ms after warm-up; retain shader compilation, GC, memory growth and Reset spikes as separate categories. |
| Transport and faults (P0-032) | 10,000-message transport tests; delay, reorder, duplicate, saturate and restart producers; stall each producer independently while the others continue within bounds; physics paused with animation active and animation disabled with physics active; hidden-tab suspend/resume. Separately reported, never pooled into normal-load qualification. |

Thresholds change only by an explicit recorded owner decision before the accepting run; never lower one retrospectively, drop a supported device or workload, or relabel an inherited failure NotApplicable. A missing device or unavailable metric leaves that tier Incomplete, not Pass.

## Measurement protocol

Freeze reference devices, workload counts, metric definitions and sampling rules before accepting a run. Begin with a named integrated-GPU laptop and a named mid-range physical mobile device; record CPU/GPU, RAM, OS, browser/version, power mode, thermals, viewport, device pixel ratio and actual render scale. Chrome through Playwright is mandatory for UI proof; Firefox and Safari on real platforms qualify their own support tiers. Device emulation does not emulate mobile hardware. A 60 Hz display cannot show more than 60 distinct frames/s; measure the actual cadence of 90/120 Hz devices rather than assuming a requested rate.

Use a diagnostic Release bundle for stage timings and counters, then repeat timing on the diagnostics-disabled production bundle and measure instrumentation overhead. Record tick/substep/event counts, simulated versus wall seconds, candidate pairs, contact rows, solver sweeps, snapshot bytes, allocation bytes, GC pauses where observable, draw calls, triangles, material changes and mesh upload bytes. Stage/metric/scenario identifiers are enums and body/part/revision identifiers are typed; convert to validated serialized labels only at the diagnostic boundary. Correlate with [Chrome DevTools traces](https://developer.chrome.com/docs/devtools/performance); [Long Animation Frames](https://developer.mozilla.org/en-US/docs/Web/API/Performance_API/Long_animation_frame_timing) report frames over 50 ms but cannot prove a 16.67 ms target. Do not poll GPU state synchronously; report GPU time as unavailable, not zero, when no asynchronous timing exists.

For each scenario, following the [runnable measurement contract](planning/requirements.md#measurement-contract):

1. Build through actual palette, placement, rotation and connection controls; retain failed attempts. No injected solutions, setters or numeric placement menus. Diagnostic fault fixtures delay transport/scheduling only and are absent from production.
2. Warm for at least 10 wall-clock seconds through preceding complete UI attempts, then capture at least three matched complete fresh Runs. Timeout cases reach the 3600-tick / 30 s endpoint; early-goal cases keep their shorter window and predeclared minimum observations.
3. Five-minute thermal evidence is a continuous real-UI session of repeated attempts with every gap and lifecycle interval retained. Never stitch frames, pad stopped simulation or override timeouts/goals; separate screenshot/video runs from timing runs.
4. Retain distributions and raw records; compare matched devices, scenes, build settings and thermal conditions, alternating before/after. A claimed gain exceeds run-to-run variation and regresses no frame tail, startup or correctness.
5. Store revision/bundle hashes, commands, action recipes, hardware/browser metadata, stage timings, frame intervals, simulated/wall durations, allocation/resource counts, console failures, traces and captures; mark unavailable metrics and stale proofs explicitly.

## Scenario groups

Enum-identified scenarios in the C# Playtest tooling. Record actual placed elements, modes, geometry, connections and active contacts; part count alone does not describe cost.

| Scenario | Purpose and required control |
| --- | --- |
| Empty/idle workshop and paused construction | Baseline render/input overhead; unchanged geometry must not rebuild continually. |
| Sparse scene, then doubled population | Broadphase scaling; distant bodies create no narrow-phase work. |
| Dense stack/queue and one connected mechanism | Worst coupled contact solve and persistence (up to 16 dynamic bodies); include a disturbed-support control. A resting body never starts moving. |
| Fast launch, pure rotation and moving hollow compounds | Speculative-contact correctness at 64 m/s against 1-cell walls; clear-endpoint and intermediate-hit cases and misses. |
| Rope/joint/motor/spring/airflow assembly | Constraint, store and force-region coupling; stalled/released controls. |
| Multiple cones, optical paths, ropes and belts | Transparent overdraw, mesh updates and draw calls; unchanged versus moving blockers. |
| Repeated lifecycle and visibility transitions | Exact Run/Reset, Save/Load, level change, Pause/Resume, background/foreground and resource lifetime. |

<a id="multi-domain-architecture-and-6090-fps-qualification"></a>
Benchmark mixed chains across capability families (electricity → motor → contact → heat; light → heating → flow; pressure → actuator → moving occluder; radiation → exposure sensor → powered gate) and record domain sizes (bodies, constraints, edges, sources, samples, path branches, material intervals), not just part count. Establish a measured supported workload envelope per browser/device class (low/mid-range Android Chrome, iOS Safari, integrated-GPU desktop with Chrome/Edge, Firefox and Safari where applicable). If the supported baseline cannot meet the budgets at the required workload, optimisation or an explicit supported-envelope decision remains open; a desktop pass cannot close mobile qualification. Presentation-quality settings are enum-typed, explicit and cosmetic only; they never alter physics.

## Rendering cost on the main thread

Use reusable previous/current committed presentation snapshots and typed dirty revisions (state buffering, not another full-frame image buffer). Trial idle-frame suppression only when camera, UI, effects and all visible dependencies are settled; redraw for input, simulation, topology/resource change, resize/DPR and visibility/context restoration. Paused physics alone is not sufficient. Full 3D dirty rectangles stay conditional on depth, revealed background, shadow, transparency and camera invalidation.

Render geometry, contact geometry and interaction surfaces/volumes stay separate (PERF-03a–03d artwork/contact declarations, PERF-18 interaction geometry), sharing immutable geometry only where accuracy requirements agree; one authoritative committed pose drives all of them. Track typed geometry/pose/power revisions for optical, rope, belt and cone presentation; recompute rope sag or clipped cone meshes only when inputs change. Cosmetic sampling may be reduced only under a declared, visually qualified policy; authoritative light/airflow/sensor results keep their timing in the physics worker. Replace per-frame mesh-dimension edits with transforms of shared unit geometry where equivalent; share immutable meshes/materials for identical decorative parts without sharing mutable per-part material state.

PERF-28–30 require projected-size decorative mesh budgets, stable visual LOD, same-material merging of rigidly co-moving details and immutable art-resource reuse while preserving independent mechanisms, selection, culling bounds and physical geometry. The renderer employs GPU-instanced draw batches (`gl.drawElementsInstanced` in WebGL 2, `renderPass.drawIndexed` in WebGPU) for repeated elements (dominoes, balls, pins, guard details), grouped by mesh/material pipeline. Profile full-screen pixel cost: overlapping translucent cones, beams, guides and wavefronts can dominate; bound effect coverage while preserving see-through geometry and channel colours. Benchmark shadow distance/resolution and casters; keep decorative beams/waves shadow-free as designed. Record device pixel ratio and cap backing-buffer resolution only through an explicit enum-typed policy. Follow WebGL/WebGPU best practices. Preserve the [DESIGN.md](../DESIGN.md) palette, silhouettes, socket readability and interaction target size throughout.

## Solver and worker cost

The [solver model](gpu-f16-physics.md#solver-model) is fixed: Dynamic AABB BVH with speculative velocity fattening ($|\mathbf{v}|\Delta t + \text{slop}$) and incremental AVL rotations, branchless analytic narrowphase manifolds + hollow SDFs, Box2D v3 TGS Soft constraint solving with unified compliance/damping, Disjoint Set Union (DSU) island partitioning with sleeping state machine, WASM SIMD128 4-wide vectorization, per-substep quaternion normalisation and endpoint-sampled sensors. Optimisation happens inside that model and preserves same-build replay and the envelope: zero scratch allocation at stable topology, cached topology order refreshed at explicit revision changes, persistent contact identities with explicit invalidation, and worker scheduling that runs at most four whole ticks per turn, yields to its mailbox, stops with explicit Capacity failure above 100 ms of debt and pauses hidden tabs at a committed boundary without a catch-up burst ([engine contracts](engine-contracts.md#clocks-histories-and-performance)). Island sleep is a measured generic-solver option only if it preserves "a resting body never starts moving", wakes on contact, changed force/supply, motor commands, support removal, topology/material change and energy release, and keeps timers, sensor residence, triggers and stores running while mechanical state sleeps; prove equal results with sleep denied as a control.

Managed AOT and SIMD for the C# host are measured choices against the pinned toolchain ([AOT guidance](https://learn.microsoft.com/en-us/aspnet/core/blazor/webassembly-build-tools-and-aot?view=aspnetcore-10.0), [runtime performance](https://learn.microsoft.com/en-us/aspnet/core/blazor/performance/webassembly-runtime-performance?view=aspnetcore-10.0)); compare production CPU profiles, compressed bytes, load time, memory and Reset before adopting either, and never add scalar/SIMD compatibility branches. Verify negotiated Brotli/gzip, Wasm MIME type and versioned cache invalidation on the deployed host; measure engine, runtime and content downloads separately. Standard fixed-timestep analysis and cubic Hermite / shortest-arc Slerp pose interpolation govern display presentation between ticks.

## Open checklist items (P0-034 unless noted)

- [ ] Enum-typed performance instrumentation and the production browser/device baseline.
- [ ] Verified zero scratch allocation and stable live memory across the 20-cycle lifecycle matrix.
- [ ] Interpolation, lifecycle discontinuity and freshness qualification across the admitted cadences.
- [ ] Cached visuals, shared geometry and explicit instancing trial; transparency/shadow/resolution costs.
- [ ] Startup delivery and browser/device budgets on the production bundle.
- [ ] Fault, stall and transport campaigns (P0-032).
- [ ] Largest taught campaign machines in final qualification after CAMPAIGN content exists.

Completion also requires removal of superseded paths, enum/typed-identifier review through callers and tests, no stale affected-element proof and preserved DESIGN.md appearance.
