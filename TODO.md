# Current delivery

**Goal:** 150 progressively taught challenge levels, unlimited free-play Workshop, and the complete engine/catalogue requirements through their named release gates.

**Candidate:** this root workspace is authoritative. The forward Tri-Graph architecture is authoritative: C# Logical Machine Graph (main thread) compiles at Run to flat SIMD-aligned Structure-of-Arrays (SoA) Physics Graph executed on a dedicated WebAssembly SIMD Web Worker (`wasm-simd128`), publishing poses via a zero-copy lock-free `SharedArrayBuffer` triple pose ring to the universal instanced Rendering Graph (`gl.drawElementsInstanced` in WebGL 2, `renderPass.drawIndexed` in WebGPU). Strictly zero external dependencies for physics (no Box2D, no Jolt, no Rapier, all algorithms custom in-engine C# and TypeScript code) incorporating modern best practices (Quad-BVH SIMD vectorization, Speculative Contacts for CCD, 4-point area-maximizing manifold reduction, unified compliant soft constraints, deterministic pair sorting). Legacy compute shader pipelines, GPU readback stalls, and CPU fallback paths are permanently removed.

**Current slice:** ANIM-1c: Legacy presentation code retirement (Story 4.3) — next ready outcome after the ANIM-1b checkpoint.

**Delivered:** every verified slice to date (ENGINE-CORE-1 through ANIM-1b) is recorded in [docs/planning/delivered-slices.md](docs/planning/delivered-slices.md).

**Current result / blocker / owner:** ANIM-1b terminal scoped Pass verified by the independent reviewer (Murdoch, 9 Oct 2026; record in [spec 4.2](_bmad-output/implementation-artifacts/spec-4-2-mechanical-cosmetic-bindings.md) Review Triage Log): anvil 0 warnings; 562/562 `dotnet test CuriousContraptions.slnx`; `dotnet publish CuriousContraptions.web` exit 0; `tools/e2e/anim-1b.test.ts` 3/3; cumulative 11 suites / 38 tests pass in one serial Chrome run. The physics worker's static-sphere contact fix (collider radius/material) was accepted as a necessary behaviour-preserving correction. Checkpoint commit cut locally (not pushed) covering Epics 1–3, Story 4.1 and Story 4.2. Pending outside the slice: the worker-trimming publish change (three csproj files, [research](_bmad-output/implementation-artifacts/research-wasm-load-time.md)) awaits its own independent review before commit; deferred findings are listed in [deferred-work](_bmad-output/implementation-artifacts/deferred-work.md).

**Next acceptance check:** ANIM-1c (Story 4.3) — all playable parts animate through declared bindings only; retired presentation classes under `engine/presentation/*.cs` and `ui/*.cs` deleted; reviewer grep confirms zero element-keyed presentation loops; `tools/e2e/anim-1c.test.ts` plus cumulative suites 100% serially in Chrome.

**Next:** ANIM-1c (Legacy presentation code retirement, closes Epic 4 → epic commit); CAT-023a–b (Domino dynamic box & orientation sensor)... in the [roadmap](docs/planning/invest/vertical-delivery.md#rolling-playable-roadmap). Unrelated root changes and parked Pipe remain preserved; Pipe is unadmitted.

**Authority:** [AGENTS](AGENTS.md), [game-grade envelope](docs/gpu-f16-physics.md#game-grade-envelope), [requirements](docs/planning/requirements.md). Detailed performance, worker/fault/device matrices and global qualification remain at their named later gates.
