# Epic 4 Context: Dedicated WebAssembly Animation Worker

<!-- Compiled from planning artifacts. Edit freely. Regenerate with compile-epic-context if planning docs change. -->

## Goal
Establish an independent 60 Hz WebAssembly animation worker that consumes committed physics state over a one-way event channel and drives every cosmetic secondary animation (lamp glow, receiver halo pulses, bumper squash/stretch, switch depression, delay progress fill). This removes cosmetic timing from both the main thread and the simulation worker, so presentation feedback stays fluid regardless of physics sub-stepping or display pacing, and the renderer ends up consuming only two inputs: the pose ring and the animation worker channel. The epic completes the Tri-Graph architecture's third leg and retires all legacy per-element presentation loops before new catalogue elements land.

## Stories
- Story 4.1: Dedicated 60 Hz Animation Worker Pipeline & Core Feedback (ANIM-1a)
- Story 4.2: Mechanical Cosmetic Bindings & Procedural Curves (ANIM-1b)
- Story 4.3: Legacy Presentation Code Retirement (ANIM-1c)

## Requirements & Constraints
- Decoupled animation pipeline: cosmetic animations evaluate at 60 Hz on a dedicated animation worker, fed one-way by physics events. Animation must continue whether physics is paused or running.
- Rendering never synchronously blocks waiting on the animation worker; no reverse data flow or synchronous waits anywhere in physics -> animation -> renderer.
- Visual elements animate according to declared curve bindings on the element, not element-specific evaluators. Adding an element must touch only declaration data and art; the animation worker core stays generic.
- Forward-only refactoring: each story names and deletes the legacy it supersedes (main-thread halo/lamp timer loops, element-specific cosmetic evaluators, retired presentation classes and ad-hoc update hooks). No shims, flags, dual paths or compatibility bridges; an active-tree grep must show zero element-keyed presentation update loops by the end of the epic.
- Exact lifecycle: Reset restores the authoring scene bit-for-bit (no cosmetic state, residual glow or partial fill may persist); Save/Load roundtrip preserves exact f32 values and configurations; saving during Run is rejected.
- Strict typing: closed sets (curve kinds, binding targets, event types) are enums end-to-end; extensible identities use strongly typed IDs; string conversion only at JSON/UI/worker-message boundaries with unknown values rejected.
- Universal presentation: cosmetic transforms must feed instanced draw batches on both WebGL 2 and WebGPU; no GPU readback.
- Game-grade envelope applies to cosmetic math: clamp or continue, never fault a tick; non-finite values keep the last committed state.
- Definition of done per story: `anvil check --changed` with 0 warnings; `dotnet test CuriousContraptions.slnx` 100%; dedicated suite `tools/e2e/anim-1a|1b|1c.test.ts` plus all cumulative `tools/e2e/*.test.ts` suites passing 100% serially in actual Chrome (`node --test --test-concurrency=1 --test-timeout=150000`); Reset and Save/Load verified in Chrome; legacy-remnant grep clean; terminal scoped Pass from an independent adversarial reviewer. Self-review never qualifies.
- Playable-first policy: deliver working interactions now; detailed profiling and global performance qualification remain at their later named gates, but ordinary lifecycle/resource ownership and production builds are required.

## Technical Decisions
- Tri-Graph, one-way flow: Machine Graph (main thread, C#) -> Physics Graph (WASM SIMD worker, 120 Hz / 480 Hz substeps) -> lock-free SharedArrayBuffer triple pose ring -> Rendering Graph (main thread, 30-60 FPS); and Physics -> Animation worker (60 Hz) -> Renderer as cosmetic transforms. Neither animation nor rendering may mutate physical state. Rate ordering physics > animation >= renderer is an invariant.
- The animation worker is a dedicated WebAssembly Web Worker (`CuriousContraptions.Animation.Worker/`) hosting the cosmetic models and curve evaluators (`CuriousContraptions.Animation/`). It evaluates procedural curves, squash/stretch and light transitions from physics events and committed state only.
- Worker interop uses pre-allocated SharedArrayBuffer rings with atomic sequence counters (`Atomics.load`/`Atomics.store`); no per-frame allocation, no `mapAsync`, no message-passing of per-frame pose data.
- Declarative element model: an element's presentation is defined by its visual mesh plus animation curve bindings in its declaration record alongside shape, mass, materials, constraints, sensors and energy stores. The worker contains zero element `switch` statements or identifiers; greps for element or level IDs in the animation core must return 0 matches.
- Numeric authority is canonical IEEE-754 f32 across authoring, solver, animation and presentation; 4-wide `Vector128<float>` where vectorised. Integers and declared external adapters keep their own types.
- Rendering samples committed poses with shortest-arc quaternion slerp and issues instanced draws grouped by (MeshID, PipelineID) via `gl.drawElementsInstanced` / `renderPass.drawIndexed`; cosmetic transforms layer onto those instances rather than driving separate per-element draws.
- Stack: .NET 10 / C# 13 (prefer C# wherever possible), Godot 4.7.2 mono as rendering host and UI, TypeScript for worker bridge and Playwright E2E, Anvil for static checks. Zero external physics or animation libraries.
- Deterministic pair ordering and zero-allocation tick memory are physics-side invariants the animation worker must not disturb (it reads, never writes, physics tables).
- Workflow: paired subagents (dedicated implementer, distinct adversarial reviewer), test-first; the reviewer independently runs the diff inspection, static checks, unit tests, Chrome E2E suites, Reset/Save-Load proof and legacy grep before issuing Pass/Fail/Incomplete. Anvil pre-write validation runs before each file write.

## UX & Interaction Patterns
- Cosmetic feedback must read as immediate and continuous: lit lamps glow and captured receivers pulse smoothly; bumpers squash/stretch on impact, switches depress on activation, delays show a progress fill. Motion follows the project's DESIGN.md guidance on form, lighting and motion, and preserves the approved palette.
- Proof uses actual UI controls in Chrome (placement, wiring, Run, Reset, Save/Load), never setters or numeric placement menus; each story keeps a meaningful negative/control case (e.g. unlit lamp does not glow, un-impacted bumper stays at rest).

## Cross-Story Dependencies
- Story 4.1 (done) established the worker, event channel and lamp/halo bindings; 4.2 extends the same binding schema to mechanical curves and must not introduce a second evaluation path.
- Story 4.3 depends on 4.1 and 4.2 having migrated every currently playable part, since it deletes the remaining presentation classes under `engine/presentation/*.cs` and `ui/*.cs` and requires a clean element-keyed-loop grep across the tree.
- Downstream element epics (5 onward) assume elements ship as declaration data plus art with animation bindings only, so the binding schema finalized here is the contract they build on.
