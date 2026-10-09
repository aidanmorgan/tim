# Current delivery

**Goal:** 150 progressively taught challenge levels, unlimited free-play Workshop, and the complete engine/catalogue requirements through their named release gates.

**Candidate:** this root workspace is authoritative. The forward Tri-Graph architecture is authoritative:
- **Machine graph:** the C# Logical Machine Graph runs on the main thread and compiles at Run to a flat SIMD-aligned Structure-of-Arrays (SoA) Physics Graph.
- **Physics graph:** it executes on a dedicated WebAssembly SIMD Web Worker (`wasm-simd128`). It publishes poses through a zero-copy, lock-free `SharedArrayBuffer` triple pose ring.
- **Rendering graph:** the universal instanced Rendering Graph draws with `gl.drawElementsInstanced` in WebGL 2 and `renderPass.drawIndexed` in WebGPU.
- **Dependencies:** physics has strictly zero external dependencies. There is no Box2D, Jolt or Rapier; all algorithms are custom in-engine C# and TypeScript code.
- **Practices:** Quad-BVH SIMD vectorization, Speculative Contacts for CCD, 4-point area-maximizing manifold reduction, unified compliant soft constraints and deterministic pair sorting.
- **Removed:** legacy compute shader pipelines, GPU readback stalls and CPU fallback paths are permanently removed.

**Current slice:** Story 7.0 element implementation readiness ([spec](_bmad-output/implementation-artifacts/spec-7-0-element-implementation-readiness.md)), owner decision of 9 Oct 2026. It produces, in `docs/planning/elements/`:
- full specs for all 72 catalogue elements;
- full specs for all 275 named puzzle-element identities;
- a disposition ledger for every legacy file Epic 7 deletes.

It runs as 15 batches, each with an independent reviewer. Owner decisions surfaced by the batches are collected in [story-7-0-owner-questions](_bmad-output/implementation-artifacts/story-7-0-owner-questions.md).

**Delivered:** every verified slice to date is recorded in [docs/planning/delivered-slices.md](docs/planning/delivered-slices.md). That covers ENGINE-CORE-1 through Story 6.1d velocity hardening; Epics 1–5 are complete and Epic 6 is in progress.

**Current result / blocker / owner:**
- **Story 6.1d velocity hardening:** terminal scoped Pass (Murdoch, two passes; record in the [spec 6.1d](_bmad-output/implementation-artifacts/spec-6-1d-velocity-hardening.md) Review Triage Log). Committed locally, not pushed. The folded e2e speed-up and the duplicate-test merge (54 → 45 tests) cut the cumulative serial Chrome run from 731.5 s to 368 s.
- **Story 7.0 batches:**
  - B and D are in independent review.
  - C failed review on two misread extents and missed details; it is back with its implementer.
  - A and E–O are still writing.
- Commits are cut after every story passes independent verification (owner rule, 9 Oct 2026).
- Deferred findings: [deferred-work](_bmad-output/implementation-artifacts/deferred-work.md).
- Research: [e2e speed](_bmad-output/implementation-artifacts/research-e2e-speed.md), [WASM load time](_bmad-output/implementation-artifacts/research-wasm-load-time.md), [Epic 7 inventory](_bmad-output/implementation-artifacts/research-epic-7-legacy-inventory.md).

**Next acceptance check:** each Story 7.0 batch passes its independent review. The checks are:
- six sections per spec;
- every requirements variant covered;
- sampled citations state their facts;
- the riskiest legacy files fully harvested;
- no unsourced value presented as sourced.

The closing pass then completes the ledger and the index.

**Next:** Story 7.0, then the legacy purge (Stories 7.1–7.4, LEGACY-0a..0d), then Story 6.2 (CAT-015a Bumper radial impulse) in the [roadmap](docs/planning/invest/vertical-delivery.md#rolling-playable-roadmap). This order is the owner re-ordering of 9 Oct 2026. Unrelated root changes and parked Pipe remain preserved; Pipe is unadmitted.

**Authority:** [AGENTS](AGENTS.md), [game-grade envelope](docs/gpu-f32-physics.md#game-grade-envelope), [requirements](docs/planning/requirements.md). Detailed performance, worker/fault/device matrices and global qualification remain at their named later gates.
