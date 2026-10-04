# General collision and constraint architecture — execution plan

Required target architecture: [general physics capability audit](general-physics-capability-audit.md), [browser physics and rendering performance plan](browser-physics-performance.md), and [simulation–presentation bridge](simulation-presentation-bridge.md). Their requirements are tracked in [P0 TODO](planning/requirements.md#physics-performance); none is a claim of completed implementation or 60–90 FPS qualification.

The target is declaration-only parts built from generic physical laws, shared typed ownership/queries and conserved cross-domain state. Separate render, contact and interaction geometry; use conservative spatial bounds only for candidate rejection. Compile validated capabilities/topology into efficient execution plans and qualify each required process and individual part/mode. Solver code must not select physical outcomes by part, catalogue, level or render identity.

Use a dedicated simulation worker with C# discrete control and WebGPU/WGSL f16 numerical authority at fixed 120 Hz with four initial outer substeps, a separate C# animation worker initially at 60 Hz, and the browser-main-thread Godot/WebGL presenter on its own display clock. Typed commands enter the simulation owner; committed snapshots/events/results leave through bounded owned transport. Full gameplay rollback, deterministic application, lifecycle generations and buffer leases precede publication. Animation evaluation never accesses Godot objects; the browser presenter applies properties using worker timestamps without synchronous waits. Distinct execution contexts are mandatory.

Complete engine laws, both workers, renderer integration, current-consumer proof and sustained 60 FPS baseline/90 FPS qualified-device budgets before **P0-035** in the [authoritative TODO register](delivery-workflow.md#pipeline-priority) closes. Product/catalogue expansion follows that gate. This document defines architecture, not another execution queue.

Current implementation status, blocker and next check come from [TODO](../TODO.md). The architecture and acceptance below define the intended state.

## Required outcome

A puzzle part declares geometry, material, mass/inertia, constraints, typed capabilities/ports and finite stores. Generic world-owned laws/controllers produce forces and events; a part must not implement its own physics in a callback. The simulation worker owns spatial queries, continuous contact, constraints, numerical state, clocks, goals and all coupled domain transactions. Rendering and animation consume committed observations without feeding interpolated or cosmetic state back into those laws.

Preserve compound hollow passages, declared physical error bounds, current palette, authored assistance and exact construction restoration. Forward-refactor code, callers, content, tools and tests together. Remove superseded solvers, browser-thread simulation, per-part cosmetic scheduling, obsolete APIs and readers. No compatibility shim, old-name alias, automatic format migration, fallback routing or hidden old/new runtime selection is permitted.

## Engine acceptance before product work

The TODO register supplies the sole task sequence. The engine gate requires:

- Independently compiled scene-free C# host/discrete and animation assemblies plus typed WGSL f16 numerical kernels; compiler-checked command/event/state contracts and rejected undefined values. Canonical game values and explicit platform adapters follow the [numeric contract](gpu-f16-physics.md).
- Bounded-error compound geometry, conservative spatial queries and a shared continuous contact/constraint pipeline. Both bodies' translation/rotation, hollow seams, intermediate impacts, finite work and failed-step rollback need independent controls.
- Every required generic mechanical, electrical, fluid, gas, optical, acoustic, thermal, phase, chemical, radiation and material law, with declared units/model limits and analytic/conservation oracles.
- Named generic law fixtures built through real UI controls, then every affected current part/mode's positive/control/integration/Reset/save proof. Isolated probes and native fixtures supplement these checks.
- Real simulation/animation workers and independently paced presentation, truthful overload/backpressure, reliable events, stale-generation rejection and asynchronous lifecycle barriers.
- Production Release measurements and Chrome/Playwright traces proving distinct contexts, same-tick authority across cadence/delay attacks, bounded freshness and sustained device/workload budgets.
- An explicit removed-path ledger, clean current callers/content/tools, retained failure evidence and verified publication. Later additions must use already-qualified capabilities; missing capability work reopens the engine gate.

## Evidence and regression contract

Retain inspection, source identities and raw verification results in separate verification records. Intentional forward refactors require independently justified physical oracles; do not weaken tolerances or regenerate success expectations merely to obtain a pass. Record current source/artifact identities and every affected proof invalidation.

Benchmark the complete three-context production pipeline, including transport, animation, scene application, rendering, memory and input response. A native kernel result, startup probe or screenshot is insufficient. All 150 campaign levels, every required catalogue element, device support and difficulty obligations remain tracked in TODO after engine completion.

## Current model contracts

Complete current behavior and controls are in the [source requirements](planning/requirements.md#physics-target), [hollow geometry](hollow-geometry-design.md), [current models](README.md#current-physical-and-controller-models) and [engine contracts](engine-contracts.md).
