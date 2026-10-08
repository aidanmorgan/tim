# Current delivery

**Goal:** 150 progressively taught challenge levels, unlimited free-play Workshop, and the complete engine/catalogue requirements through their named release gates.

**Candidate:** this root workspace is authoritative. The forward Tri-Graph architecture is authoritative: C# Logical Machine Graph (main thread) compiles at Run to flat SIMD-aligned Structure-of-Arrays (SoA) Physics Graph executed on a dedicated WebAssembly SIMD Web Worker (`wasm-simd128`), publishing poses via a zero-copy lock-free `SharedArrayBuffer` triple pose ring to the universal instanced Rendering Graph (`gl.drawElementsInstanced` in WebGL 2, `renderPass.drawIndexed` in WebGPU). Strictly zero external dependencies for physics (no Box2D, no Jolt, no Rapier, all algorithms custom in-engine C# and TypeScript code) incorporating modern best practices (Quad-BVH SIMD vectorization, Speculative Contacts for CCD, 4-point area-maximizing manifold reduction, unified compliant soft constraints, deterministic pair sorting). Legacy compute shader pipelines, GPU readback stalls, and CPU fallback paths are permanently removed.

**Current slice:** CAT-023a: Dynamic box rigid body & upright stability (Story 5.1) — next ready outcome after the Epic 4 commit.

**Delivered:** every verified slice to date (ENGINE-CORE-1 through ANIM-1c; Epics 1–4 complete) is recorded in [docs/planning/delivered-slices.md](docs/planning/delivered-slices.md).

**Current result / blocker / owner:** ANIM-1c terminal scoped Pass verified by the independent reviewer (Murdoch, 9 Oct 2026, two passes; record in [spec 4.3](_bmad-output/implementation-artifacts/spec-4-3-legacy-presentation-code-retirement.md) Review Triage Log): Receiver halo on the declared Capture cosmetic path, goal/hint as declared UI bindings queued behind the single animation lease, 21 uncompiled legacy evaluator/visual files deleted; folded in by owner decision: native clock admission no longer pins a browser version (witness-only), and Free workshop offers every playable part with unlimited inventory bounded only by the typed physics table capacities (a second Receiver is rejected while a ball is present because each Receiver guides every ball; Wait for it now admits only its goal lamp). Evidence: `anvil check --changed` 0; `dotnet test CuriousContraptions.slnx` 591/591; `dotnet publish CuriousContraptions.web` exit 0; `tools/e2e/anim-1c.test.ts` 4/4; cumulative 12 suites / 42 tests pass in one serial Chrome run. Epic 4 commit cut locally (not pushed). Deferred findings: [deferred-work](_bmad-output/implementation-artifacts/deferred-work.md); follow-up research: [e2e speed](_bmad-output/implementation-artifacts/research-e2e-speed.md), [WASM load time](_bmad-output/implementation-artifacts/research-wasm-load-time.md).

**Deferred follow-up:** e2e wall-clock reduction options are recorded in [research-e2e-speed](_bmad-output/implementation-artifacts/research-e2e-speed.md); not scheduled.

**Next acceptance check:** CAT-023a (Story 5.1) — Domino placed upright on the workbench stands stably under gravity and topples realistically when struck (dynamic box rigid body, 3x3 inertia, 4-point manifold); exact Reset and Save/Load; `tools/e2e/cat-023a.test.ts` plus cumulative suites 100% serially in Chrome.

**Next:** CAT-023a–b (Domino dynamic box & orientation sensor, Epic 5); then Epic 6 in the [roadmap](docs/planning/invest/vertical-delivery.md#rolling-playable-roadmap). Unrelated root changes and parked Pipe remain preserved; Pipe is unadmitted.

**Authority:** [AGENTS](AGENTS.md), [game-grade envelope](docs/gpu-f16-physics.md#game-grade-envelope), [requirements](docs/planning/requirements.md). Detailed performance, worker/fault/device matrices and global qualification remain at their named later gates.
