# Current delivery

**Goal:** 150 progressively taught challenge levels, unlimited free-play Workshop, and the complete engine/catalogue requirements through their named release gates.

**Candidate:** this root workspace is authoritative. The forward Tri-Graph architecture is authoritative: C# Logical Machine Graph (main thread) compiles at Run to flat SIMD-aligned Structure-of-Arrays (SoA) Physics Graph executed on a dedicated WebAssembly SIMD Web Worker (`wasm-simd128`), publishing poses via a zero-copy lock-free `SharedArrayBuffer` triple pose ring to the universal instanced Rendering Graph (`gl.drawElementsInstanced` in WebGL 2, `renderPass.drawIndexed` in WebGPU). Strictly zero external dependencies for physics (no Box2D, no Jolt, no Rapier, all algorithms custom in-engine C# and TypeScript code) incorporating modern best practices (Quad-BVH SIMD vectorization, Speculative Contacts for CCD, 4-point area-maximizing manifold reduction, unified compliant soft constraints, deterministic pair sorting). Legacy compute shader pipelines, GPU readback stalls, and CPU fallback paths are permanently removed.

**Current slice:** ENGINE-F32-VELOCITY (Story 6.1c): committed body velocity and angular velocity stored as f32 instead of binary16 so declared drag (0.04) and rolling-resistance decrements survive at 60/120/240 Hz and at all speeds.

**Delivered:** every verified slice to date (ENGINE-CORE-1 through ENGINE-DRAG; Epics 1–5 complete, Epic 6 in progress) is recorded in [docs/planning/delivered-slices.md](docs/planning/delivered-slices.md).

**Current result / blocker / owner:** ENGINE-DRAG (Story 6.1b) terminal scoped Pass (Murdoch, three passes; record in [spec 6.1b](_bmad-output/implementation-artifacts/spec-6-1b-engine-drag-application.md) Review Triage Log): harness 36/36, 622/622, `cat-014` 4/4 with a Chrome negative control, cumulative 15 suites / 54 tests. CAT-014 (Story 6.1) holds its terminal scoped Pass for the original 0.38 m Bowling ball (Murdoch, record in [spec 6.1](_bmad-output/implementation-artifacts/spec-6-1-bowling-ball-dynamic-sphere.md)); the 0.28 m re-tune is covered by the 6.1b review; its "both balls rest" row is now asserted by 6.1b. Not committed: commits are cut per epic (Epic 6 commit after its last story). Owner decisions of 9 Oct 2026 applied in 6.1b: rolling resistance at every sphere contact and a 0.28 m / 4 kg Bowling ball; f32 committed velocity moves to Story 6.1c. Earlier slice summaries: [delivered-slices](docs/planning/delivered-slices.md). Deferred findings: [deferred-work](_bmad-output/implementation-artifacts/deferred-work.md); follow-up research: [e2e speed](_bmad-output/implementation-artifacts/research-e2e-speed.md), [WASM load time](_bmad-output/implementation-artifacts/research-wasm-load-time.md).

**Deferred follow-up:** e2e wall-clock reduction options are recorded in [research-e2e-speed](_bmad-output/implementation-artifacts/research-e2e-speed.md); not scheduled.

**Next acceptance check:** Story 6.1c — declared drag 0.04 slows a flying ball at 60/120/240 Hz; a 5 m/s rolling ball decelerates every tick at 240 Hz and rests; host validation and pose ring read f32 velocity; all prior suites pass serially in Chrome.

**Next:** Story 6.1c (f32 committed velocity), then Story 6.2 (CAT-015a Bumper radial impulse) in the [roadmap](docs/planning/invest/vertical-delivery.md#rolling-playable-roadmap). Unrelated root changes and parked Pipe remain preserved; Pipe is unadmitted.

**Authority:** [AGENTS](AGENTS.md), [game-grade envelope](docs/gpu-f32-physics.md#game-grade-envelope), [requirements](docs/planning/requirements.md). Detailed performance, worker/fault/device matrices and global qualification remain at their named later gates.
