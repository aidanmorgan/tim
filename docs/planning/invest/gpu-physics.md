# Physics engine and instanced presentation criteria inside playable delivery

These 27 labels are supporting criteria under existing work IDs, staged inside [roadmap](vertical-delivery.md#rolling-playable-roadmap) slices; they are not 27 prerequisite projects. The system they serve is stated once in [engine slices](engine.md) and fixed by the [compilation model](../../gpu-f32-physics.md#compilation-model), [solver model](../../gpu-f32-physics.md#solver-model) and [game-grade envelope](../../gpu-f32-physics.md#game-grade-envelope): one typed scene record compiled at Run, one generic WebAssembly SIMD f32 solver (`wasm-simd128` with Box2D v3 TGS Soft solver and Dynamic AABB BVH) on a dedicated Web Worker at 120 Hz with 480 Hz substeps, a separate animation worker at 60 Hz, and a browser main-thread universal instanced renderer (`gl.drawElementsInstanced` in WebGL 2, `renderPass.drawIndexed` in WebGPU) reading committed poses via a lock-free zero-copy triple-buffered pose ring in `SharedArrayBuffer`. Residuals clamp or continue; no tick is rejected on a numerical residual.

**Proof rule & timing relaxation (stated once):** implement and independently review each criterion with the playable outcome that stages it. Required now: affected production build, actual Chrome/Playwright behavior through real controls, ordinary Run/Reset and supported Save/Load, a meaningful negative/control. Timing budgets, tick latencies (<0.5 ms), zero-clock-debt thresholds over 20 runs, and display-rate 60 FPS pacing are advisory telemetry and target recommendations, not hard gating constraints for development slices. A unit or benchmark check supports the outcome and never substitutes for Chrome proof. Release-only measurements (tick p95, FPS windows, memory, injected loss, device matrix, thermal sessions) belong to the [P0-034 stage gate](../../delivery-workflow.md#stage-gates). Fixed: one WASM SIMD physics authority, canonical f32, puzzle-scale envelope, exact discrete/Reset, typed ABI, supported-device release scope. Algorithms, layout and factoring are negotiable.

## ENGINE-CORE-1 · existing ball on the envelope

<a id="p007-first"></a>
### P0-007 · p007-first

**Outcome:** Basketball placed through the palette falls, contacts the workbench and bounces; Remove and empty-scene control; Reset exact. Already playable; ENGINE-CORE-1 re-proves it under slop/clamp/normalise policy with zero tick faults across 20 Runs.

<a id="p007-geometry"></a>
### P0-007 · p007-geometry

**Outcome:** sphere, box and plane shapes with radii/extents as authored data in the flat SIMD Structure-of-Arrays (SoA) table; no Basketball/workbench identity in solver dispatch. **Control:** invalid transform/typed identity rejects before mutation.

<a id="p007-constraint"></a>
### P0-007 · p007-constraint

**Outcome:** restitution, bounce threshold and Coulomb friction from the declared material pair law (product restitution, max threshold, geometric-mean friction) through the shared sequential-impulse / TGS Soft pass. **Acceptance:** a dropped ball's second bounce apex is lower than the first; a resting ball never starts moving; a grazing contact does not launch.

<a id="p008-admission"></a>
### P0-008 · p008-admission

**Outcome:** Workshop placement compiles the same canonical f32 values the solver consumes; unsupported parts/levels reject before mutation and a usable Free workshop entry always remains. **Control:** overlap, out-of-range and unknown-schema input are refused visibly.

<a id="p008-values"></a>
### P0-008 · p008-values

**Outcome:** the IEEE-754 f32 values displayed for a placed part are the values run and restored by Reset; adjacent/invalid encodings are refused at the input boundary.

<a id="p012-ledger"></a>
### P0-012 · p012-ledger

**Outcome:** no free energy, observable in Chrome: a resting ball stays at rest, each bounce is lower, a paid element (Bumper) never fires unpaid. Finite electrical/fluid/thermal stores arrive with the element that first needs them.

<a id="p014-commit"></a>
### P0-014 · p014-commit

**Outcome:** a discarded candidate (non-finite, failed step, stale epoch) never becomes a visible pose in the `SharedArrayBuffer` pose ring; the previous committed pose stays and the tick continues; Reset recovers.

<a id="p016-wire"></a>
### P0-016 · p016-wire

**Outcome:** construction/Run/pose/Reset messages carry packed f32 floats, integer cells/scales and typed IDs losslessly; unknown tags/versions reject. ENGINE-CORE-2 extends the same revision to the multi-body scene record.

<a id="p017-device"></a>
### P0-017 · p017-device

**Outcome:** the Workshop starts its sole simulation worker with WebAssembly SIMD (`wasm-simd128`) and `SharedArrayBuffer`; missing feature or asset is a visible unsupported state; dispose once; no CPU fallback physics path.

<a id="p019-ui"></a>
### P0-019 · p019-ui

**Outcome:** real Workshop controls (palette, placement, Run, Reset) drive the WASM SIMD authority; the universal instanced renderer displays committed poses. No setters or imported solutions.

<a id="p019-cutover"></a>
### P0-019 · p019-cutover

**Outcome:** one production numerical path per admitted slice; the retired synchronous route and legacy compute shaders are unreachable; each later slice removes the legacy it names. LEGACY-0 proves the tree holds only shipped code and closes the P0-030/P0-031 coverage and cleanup audits.

<a id="p009-shader-ownership"></a>
### P0-009 · p009-shader-ownership

**Outcome:** reviewer grep of the simulation worker code and compiler dispatch finds no element or puzzle identifier; buffers/dispatch/pose updates have one owner. Repeated each slice; closed at LEGACY-0.

<a id="p010-gpu-proof"></a>
### P0-010 · p010-gpu-proof

**Outcome:** each slice's Chrome evidence names the WASM SIMD module, build identity and actual UI actions; a wrong/missing binary or stale layout cannot inherit a host-test Pass.

<a id="p020-transition"></a>
### P0-020 · p020-transition

**Outcome:** Save/Load of the playable construction with exact f32 bits and atomic replacement; Save during Run, malformed/obsolete content and failed admission leave the old construction intact. Re-proved by every slice's Save/Load bullet.

<a id="p016-content"></a>
### P0-016 · p016-content

**Outcome:** the construction save codec round-trips every admitted instance exactly; obsolete formats reject, never migrate; unported level files report unsupported before mutation.

<a id="p021-loss"></a>
### P0-021 · p021-loss

**Outcome:** worker crash or context termination discards the candidate, enters a typed fault and leaves Reset usable; no fallback CPU continuation. Injected before/during/after-commit loss matrices are release checklist (P0-032/P0-034).

## ENGINE-CORE-2 · multi-body speculative-contact core (decomposed into <1d thin vertical slices)

Staged through four thin vertical slices (<1 day each) in the roadmap:
- **ENGINE-CORE-2a1:** Multi-body pose ring and dual-sphere simulation
- **ENGINE-CORE-2a2:** Stable resting contact & restitution via TGS Soft solver
- **ENGINE-CORE-2a3:** Multi-body Ramp and Wall contact on generic core
- **ENGINE-CORE-2a4:** Anti-tunneling high-speed wall impact
- **ENGINE-CORE-2b:** Solver streamlining; delete legacy certificates
- **ENGINE-CORE-2c:** Receiver capture with endpoint-sampled sensors

<a id="p011-sweep"></a>
### P0-011 · p011-sweep (delivered at ENGINE-CORE-2a4)

**Outcome:** speculative contacts with margin $|\mathbf{v}|\cdot\Delta t + \text{slop}$ from the Dynamic AABB BVH and the generic branchless SAT pair table; no body passes a wall $\ge 1$ cell thick at $\le 64$ m/s. **Acceptance:** 20 runs without tunnelling; two-ramp path still succeeds; misaligned ramp still misses. Timing latency is advisory telemetry.

## ELEMENT-n · staged with the element that first needs it

<a id="p012-joint"></a>
### P0-012 · p012-joint

**Outcome (CAT-039 Linear pusher):** typed slider joint with stroke/force limits in the shared Box2D v3 TGS Soft constraint pass; powered extend/retract, unpowered hold, blocked stroke and endpoint outputs observable in Chrome. Not on the current critical path.

<a id="p022-half"></a>
### P0-022 · p022-half

**Outcome (ANIM-1):** Receiver capture halo and every other cosmetic property come from the animation model's declared bindings with canonical f32 values; animation disabled leaves capture itself unchanged.

## Release checklist · P0-034 stage gate (measured follow-ups, never prerequisites)

<a id="p013-resident"></a>
### P0-013 · p013-resident

Keep unchanged state resident in worker memory/SAB ring without duplicated allocations; same Run/Reset outcome.

<a id="p011-active"></a>
### P0-011 · p011-active

Omit provably inactive pairs via BVH query pruning only after a profile of the ramp/Receiver scene shows material cost; no false negatives.

<a id="p013-islands"></a>
### P0-013 · p013-islands

Bound sweep and solver iteration counts per Disjoint Set Union (DSU) island after an observed iteration cost; best iterate on exhaustion, never a relaxed outcome.

<a id="p017-reuse"></a>
### P0-017 · p017-reuse

Reuse a measured worker/pipeline/SAB buffer resource with one lifetime owner across Reset/disposal.

<a id="p014-batch"></a>
### P0-014 · p014-batch

Batch pose ring reads without losing a reliable capture occurrence under slow rendering or display saturation.

<a id="p033-contention"></a>
### P0-033 · p033-contention

Measure and repair one worker/render synchronization contention cause on first_principles with the renderer active.

<a id="p033-budget"></a>
### P0-033 · p033-budget

Matched whole-tick/frame remediation of one accepted interaction; reject unmeasured or regressing changes.

<a id="p034-qualify"></a>
### P0-034 · p034-qualify

Aggregate device × workload qualification under the P0-003 workload set: 30–60 FPS windows, 2.5 ms tick p95, memory after 20 cycles, $\ge 3$ complete UI attempts, 5-minute thermal sessions, exact build/device identities. An unsupported required tier blocks release.
