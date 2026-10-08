# Engine delivery slices

**System (stated once; every card below inherits it):** the player's construction is compiled at Run into one flat SIMD Structure-of-Arrays (SoA) physics record ([compilation model](../../gpu-f16-physics.md#compilation-model)); one generic WebAssembly SIMD f32 solver (`wasm-simd128` with Box2D v3 TGS Soft solver and Dynamic AABB BVH) advances it in the physics worker at a fixed 120 Hz tick with 480 Hz substeps ([solver model](../../gpu-f16-physics.md#solver-model)); a separate animation model compiled from the same element data runs in its own worker at its own rate (60 Hz), fed one-way by committed physics results; the universal instanced renderer on the browser main thread (`gl.drawElementsInstanced` in WebGL 2, `renderPass.drawIndexed` in WebGPU) reads committed poses from a lock-free zero-copy triple pose ring in `SharedArrayBuffer` and the latest animation sample and draws at 30–60 FPS, never influencing either. Numerical tolerances are the [game-grade envelope](../../gpu-f16-physics.md#game-grade-envelope): residuals clamp or continue and never fault a tick. Ownership, protocol, lifecycle and clocks follow the [engine contracts](../../engine-contracts.md). Delivery order is the [rolling playable roadmap](vertical-delivery.md#rolling-playable-roadmap); [TODO](../../../TODO.md) owns the live slice.

**Proof rule & timing relaxation (stated once):** each card is a supporting criterion inside the roadmap slice that stages it, with the same implementation/reviewer pair. Required now at that slice: affected production build, actual Chrome/Playwright behavior through real controls, ordinary Run/Pause/Reset and supported Save/Load correctness, and a meaningful negative/control. Timing budgets, tick latencies (<0.5 ms), zero-clock-debt thresholds over 20 runs, and display-rate 60 FPS pacing are advisory telemetry and target recommendations, not hard gating constraints for development slices. Release-only numbers (30–60 FPS windows, 2.5 ms tick p95, memory after 20 cycles, fault injection, device matrix, 10,000-message transport runs, 5-minute thermal sessions) are the release checklist at the [P0-034 stage gate](../../delivery-workflow.md#stage-gates), never per-slice gates. Fixed: one WASM SIMD physics authority, canonical f32, puzzle-scale envelope, exact discrete/Reset, typed ABI. Algorithms, layout and factoring are negotiable.

These labels sit under their existing canonical work IDs ([requirements](../requirements.md)); they are not new work orders. Algorithm, array layout and helper factoring remain negotiable inside the fixed contracts. A card is not a runtime Pass.

## Disposition of every P0 ID

| ID | Staged at | Current disposition |
| --- | --- | --- |
| P0-001–006 | Settled baseline/design records (P0-004 ownership, P0-005 wire, P0-006 lifecycle) | Reopen only an affected contract question; otherwise use the latest verdict. |
| P0-007 | ENGINE-CORE-1 (ball on the envelope), ENGINE-CORE-2 (multi-body) | Contact behavior becomes slop/clamp/normalise policy; moving-support and compound cases close on the generic core. |
| P0-008 | ENGINE-CORE-1 | Compile-to-state is the current path; the slice deletes the puzzle-keyed guide branch so no element identifier remains in dispatch. |
| P0-009 / P0-010 | Each slice's reviewer grep; closure at LEGACY-0 | Solver code ownership/proof binding is the per-slice `grep` for element identifiers in the physics solver plus actual Chrome proof; CHECK-AGGREGATE enforcement of the evidence schema is release checklist. |
| P0-011 | ENGINE-CORE-2 (broadphase/sweeps); ELEMENT-n optics (ordered intervals) | Conservative AABB broadphase and speculative margin replace the bounds-tree reference; ordered material intervals arrive with the first optical element. |
| P0-012 | ENGINE-CORE-2 | Retained numerical defects are re-judged against the envelope on the generic core; a residual that clamps is closed, not a defect. |
| P0-013 | Release checklist (P0-034) | Scratch, checkpoint, predictor, sleep and island work (OPT-SCRATCH, OPT-CHECKPOINT, OPT-PREDICTOR, OPT-SLEEP) are measured optimisations after a reproduced cost, never prerequisites. |
| P0-014 / P0-015 | ENGINE-CORE-1 | Committed publication and timed command admission are the current path; each slice re-proves Run/Reset through them. |
| P0-016 | ENGINE-CORE-2 | One forward ABI revision carries the multi-body scene record; prior layouts reject. |
| P0-017 | ENGINE-CORE-1 | Standalone simulation worker/device startup is the current path; no CPU physics path exists. |
| P0-018 | Release checklist (P0-034/P0-032) | Reliable sequencing is current behavior; 10,000-message, saturation and transfer campaigns are release-only. |
| P0-019 | ENGINE-CORE-1 | The simulation worker is the only stepping owner; the old synchronous Start/Step/Restore route is already unreachable for the playable target. |
| P0-020 | ENGINE-CORE-1 and every later slice | Run/Pause/Step/Reset/Load/Save/navigation acknowledgements are re-proved by each slice's "Done when". |
| P0-021 | Fault semantics now; injected campaigns at release | Device loss/worker crash leave a typed fault and usable Reset; injection matrices are release-only. |
| P0-022 / P0-023 / P0-024 | ANIM-1 | Separate animation worker with declared bindings for every presentation property; per-consumer feedback cards become declarations on their element slice. |
| P0-025 / P0-026 | ENGINE-CORE-1 (renderer reads committed poses); ANIM-1 (animation sample) | Main-thread renderer interpolates committed physics and animation samples; nothing flows back. |
| P0-027 / P0-028 | Release checklist (P0-034) | Resource sharing, dirty tracking, idle wake and visibility culling are measured render-cost work. |
| P0-029 | Production build every slice; startup at release | Clean Release export of both worker artifacts is required each slice; startup 15 s cold / 5 s warm is release-only. |
| P0-030 / P0-031 | LEGACY-0 | Consumer coverage and cleanup audits close when the tree holds only shipped code. |
| P0-032 / P0-033 / P0-034 | Release checklist | Concurrency attacks, bottleneck remediation and device/workload qualification are enforced at LEGACY-0 and CAMPAIGN. |
| P0-035 | CAMPAIGN | Engine closure is the programme's definition of done, not a gate before element or level work. |

## ENGINE-CORE-1 · the existing ball on the game-grade envelope

<a id="p007-contact"></a>
### P0-007 · p007-contact

**Outcome:** ball/workbench/ramp/wall contact behaves as the player expects with no tick fault: slop ≤ 1/64 m corrected over substeps, normal velocity clamped ≥ 0, acceleration clamped, quaternion normalised each substep. **Acceptance:** first_principles and delayed_signal solvable, Bumper demo pays out as before, 20 Runs without a fault, a resting ball never starts moving. Moving-support/compound cases close under ENGINE-CORE-2.

<a id="p007-affine"></a>
### P0-007 · p007-affine

**Outcome:** rotated Ramp and resized Wall keep full authored affine geometry as declared shape data in the compiled scene record; identity survives storage reorder. **Acceptance:** two-ramp path and local-axis Wall resize work through the UI; reflected/singular transforms reject before mutation.

<a id="p008-reject"></a>
### P0-008 · p008-reject

**Outcome:** an invalid construction is rejected at compile with nothing installed. **Acceptance:** malformed topology, duplicate identity, out-of-envelope admission bounds (speed, gravity, mass, extent) fail visibly and leave the old world, counters and queues unchanged.

<a id="p008-compile"></a>
### P0-008 · p008-compile

**Outcome:** the same construction in any declaration order compiles to the same typed scene record with stable IDs; no element or puzzle identifier reaches solver dispatch. **Acceptance:** `grep` of physics solver source and compiler dispatch finds nothing element-keyed; compiled candidate runs end to end in Chrome.

<a id="p008-install"></a>
### P0-008 · p008-install

**Outcome:** one complete scene record installs or is discarded atomically. **Acceptance:** each injected install failure leaves the previous world usable and identical; a stale candidate cannot install twice.

<a id="p014-state"></a>
### P0-014 · p014-state

**Outcome:** only a complete committed physical state is published to the renderer and animation worker. **Acceptance:** a discarded non-finite candidate keeps the previous pose; readers never mutate producer storage.

<a id="p014-events"></a>
### P0-014 · p014-events

**Outcome:** committed occurrences (impact, capture, activation) are delivered once with their state. **Acceptance:** the lamp lights once per switch impact; a retried message cannot add a second activation.

<a id="p015-admit"></a>
### P0-015 · p015-admit

**Outcome:** a command is admitted or rejected deterministically without claiming application. **Acceptance:** stale generation, duplicate ID, invalid enum, late tick and full queue reject explicitly.

<a id="p015-apply"></a>
### P0-015 · p015-apply

**Outcome:** admitted commands apply at their declared tick/phase/order. **Acceptance:** same timed log yields the same outcome regardless of producer cadence.

<a id="p016-codec"></a>
### P0-016 · p016-codec

**Outcome:** the C#/browser/worker boundary carries canonical IEEE-754 f32 bits, typed cells/scales and integer identities losslessly. **Staged at ENGINE-CORE-2:** the multi-body scene record is one forward revision; earlier layouts reject.

<a id="p017-bootstrap"></a>
### P0-017 · p017-bootstrap

**Outcome:** the simulation worker reaches verified readiness (WebAssembly SIMD128 + SharedArrayBuffer) or an explicit unsupported state; there is no CPU fallback physics path.

<a id="p019-step"></a>
### P0-019 · p019-step

**Outcome:** the simulation worker is the only stepping authority at 120 Hz; a browser stall never stops physics. **Acceptance:** a 250 ms main-thread stall leaves the committed tick advancing.

<a id="p019-debt"></a>
### P0-019 · p019-debt

**Outcome:** overload is bounded and visible; physics time is never silently stretched. Exact debt counters are release-checklist measurements.

<a id="p020-run"></a>
### P0-020 · p020-run

**Outcome:** Run/Pause/Step complete only after worker acknowledgement; queued Run is not Running.

<a id="p020-reset"></a>
### P0-020 · p020-reset

**Outcome:** Reset restores the exact canonical construction across worker, animation and renderer state. **Acceptance (every slice):** Reset after motion equals the pre-Run construction in the UI.

<a id="p020-load"></a>
### P0-020 · p020-load

**Outcome:** Load atomically replaces the construction or leaves it usable; unsupported saves reject, never migrate.

<a id="p020-save"></a>
### P0-020 · p020-save

**Outcome:** Save stores the construction only, after the required barrier; Save during Run is rejected. **Acceptance (every slice):** Save/Load round-trips the construction after every Run.

<a id="p020-navigation"></a>
### P0-020 · p020-navigation

**Outcome:** level navigation cancels or completes pending work without cross-level mutation; the new puzzle is usable.

<a id="p021-outcome"></a>
### P0-021 · p021-outcome

**Outcome:** a worker crash or device loss reports recoverable or indeterminate honestly, leaves a typed fault and a usable Reset, and never runs physics elsewhere. Injected before/after-commit matrices are release checklist.

<a id="p021-restart"></a>
### P0-021 · p021-restart

**Outcome:** recovery starts exactly one replacement worker from a known-good construction; at most one attempt.

<a id="p021-dispose"></a>
### P0-021 · p021-dispose

**Outcome:** disposal releases workers, buffers, leases and listeners exactly once. The 20-cycle retention proof is release checklist.

<a id="p025-map"></a>
### P0-025 · p025-map

**Outcome:** the main-thread renderer maps the worker clocks to one display time and presents committed poses with the fixed display delay; no backward display time. The [shared-clock contract](../../shared-clock-cadence.md) fixes 120 Hz physics / 60 Hz animation / display-rate presentation with no player frequency controls.

<a id="p025-history"></a>
### P0-025 · p025-history

**Outcome:** bounded read-only histories give coherent physical parent and cosmetic child samples; a missing bracket holds the last coherent view.

<a id="p025-discontinuity"></a>
### P0-025 · p025-discontinuity

**Outcome:** Reset/Load never interpolate across generations; no visual sweep through an invalid state.

<a id="p026-apply"></a>
### P0-026 · p026-apply

**Outcome:** the renderer applies committed transforms and properties once per frame without waiting on either worker; a stalled producer keeps the last coherent view and input stays responsive.

<a id="p029-assets"></a>
### P0-029 · p029-assets

**Outcome:** a clean Release export serves both worker artifacts with exact build identities. Required each slice.

## ENGINE-CORE-2 · multi-body speculative-contact core (decomposed into <1d thin vertical slices)

Delivered through thin vertical slices (<1 day developer effort each), followed by streamlining, sensor sampling, and animation pipeline:
- **ENGINE-CORE-2a1 (Pose Ring & Dual Spheres):** Expand SAB triple pose ring to 16 body slots with atomic sequence protocol; WASM SIMD dual-sphere stepping with basic ground/workbench plane impulse; instanced renderer reads multi-body poses directly from SAB; test by dropping 2 Basketballs in Workshop.
- **ENGINE-CORE-2a2 (TGS Soft Solver Formulation):** Box2D v3 TGS Soft constraint step (compliance $\gamma$, softness $\beta$, effective mass) with FeatureId warm-starting in WASM SIMD for spheres and static planes; stable resting contact on workbench without bounce jitter; Bumper demo payout exact.
- **ENGINE-CORE-2a3 (Box SAT Manifolds & Dynamic AABB BVH):** Dynamic AABB BVH with velocity fattening; branchless SAT narrowphase for Box-Sphere and Box-Box (Wall and Ramp colliders); first_principles 2-ramp solve in Chrome.
- **ENGINE-CORE-2a4 (Speculative Contacts & High-Speed Wall Impact):** Speculative contact margin ($|v| \cdot dt + \text{slop}$) in narrowphase; ball at maximum UI launch speed never passes a Wall of any admitted thickness in 20 Runs.
- **ENGINE-CORE-2b1 (Host Validation & Error Lane Removal):** Remove per-tick byte-exact host validation and motion-piece error lanes from BrowserWorkshopClient and PhysicsMotionRead; simulation worker publishes committed poses directly to SAB ring without intermediate host shadow checking.
- **ENGINE-CORE-2b2 (Certificate & Directed Rounding Removal):** Permanently delete clearance/closing certificates and directed rounding math (LipschitzBoundarySweep, JointBoundarySweep, directed rounding math); rely exclusively on TGS Soft constraint limits.
- **ENGINE-CORE-2b3 (Analytic CCD & Interval Library Removal):** Remove analytic CCD continuous collision sweep roots, interval-arithmetic range library, and delete engine/gpu/physics.wgsl compute shaders; collisions rely exclusively on Dynamic AABB BVH with speculative contact margins.
- **ENGINE-CORE-2b4 (Guide Horizon & Departure Ownership Removal):** Remove guide horizon calculation, departure ownership, and trajectory prediction branches from Receiver physics, converting assist curves to declarative force region fields (PlanarGuideBoundaryPath).
- **ENGINE-CORE-2c (Endpoint-Sampled Sensors):** Sensors and triggers sampled at substep endpoints, dwell counted in ticks; legacy sensor root-finding and sub-phase residence intervals deleted.
- **ANIM-1a (Dedicated Animation Worker Pipeline):** Dedicated 60 Hz WebAssembly animation worker and SAB event channel established before element additions; declared animation bindings for Receiver halo and Signal lamp glow.
- **CAT-023a (Dynamic Box Rigid Body & Upright Stability):** Dynamic box rigid body physics in WASM SIMD (box 3x3 inertia tensor, 4-point SAT contact manifold with workbench plane, friction, upright resting stability).
- **CAT-023b (Orientation Sensor & Domino Cascade):** Declarative orientation-threshold sensor (angle from initial pose, one-shot trigger, rearm on Reset); domino_effect level solve.

<a id="p011-bounds"></a>
### P0-011 · p011-bounds (delivered at ENGINE-CORE-2a3 and 2a4)

**Outcome:** the AABB broadphase with speculative margin |v|·dt + slop cannot miss a declared interaction. **Acceptance:** a 64 m/s ball never tunnels a 1-cell wall in 20 runs; three stacked boxes settle within 60 frames. Timing latency and clock debt are advisory telemetry.

<a id="p011-intervals"></a>
### P0-011 · p011-intervals

**Outcome:** ordered surface/material intervals for light, sound and radiation queries. **Staged at** the first optical ELEMENT-n slice as a generic capability, not before.

<a id="p012-defect"></a>
### P0-012 · p012-defect (delivered at ENGINE-CORE-2a2)

**Outcome:** each retained numerical defect is re-judged on the generic core against the envelope with a player-observable check; those that clamp or continue are closed.

## ANIM-1 · separate animation engine (decomposed into <1d thin vertical slices)

Delivered through three focused vertical slices:
- **ANIM-1a (Dedicated Animation Worker Pipeline & Core Feedback):** Dedicated 60 Hz WebAssembly animation worker and SAB event/feedback channel; declared animation bindings for Receiver halo and Signal lamp glow. Halo pulses and lamp glows via animation worker; physics paused leaves animation active; Reset exact.
- **ANIM-1b (Mechanical Cosmetic Bindings):** Declared procedural curves and squash/stretch bindings for Bumper compression, Impact switch depression, and Delay progress fill.
- **ANIM-1c (Legacy Presentation Code Retirement):** Delete retired per-element presentation evaluators in `engine/presentation/*.cs` and `ui/*.cs` not listed in project file. Reviewer grep confirms zero part-keyed presentation update loops.

<a id="p022-evaluate"></a>
### P0-022 · p022-evaluate (delivered at ANIM-1a)

**Outcome:** the animation model is compiled from element data (curves, tracks, transitions, bindings) and evaluated in its own worker at its own clock (60 Hz advisory target). **Acceptance:** physics paused with animation active still animates; disabling animation leaves physical outcomes unchanged.

<a id="p022-writer"></a>
### P0-022 · p022-writer

**Outcome:** every presentation property has one declared writer for its lifetime; a conflicting writer is rejected.

<a id="p023-feedback"></a>
### P0-023 · p023-feedback

**Outcome:** committed events drive declared cosmetic bindings; no element has its own update loop. The five consumer cards below are declaration specifications delivered with their element's slice.

<a id="p023-lifecycle"></a>
### P0-023 · p023-lifecycle

**Outcome:** animation registrations follow generation boundaries; an old event cannot rewrite a new part after Reset.

<a id="p024-worker"></a>
### P0-024 · p024-worker (delivered at ANIM-1a)

**Outcome:** a distinct second worker owns all animation evaluation at 60 Hz (advisory pacing); the main thread only applies samples. Cross-rate replay equality is release checklist.

<a id="p023-motor"></a>
### P0-023 · motor feedback (delivered with CAT-042)

Shaft and contact geometry show committed physical state; decorative spin is a declared animation binding that may run while physics is paused. **Control:** disabling animation leaves torque/work and replay equal; no cosmetic angle feeds contact.

<a id="p023-indicator"></a>
### P0-023 · indicator feedback (delivered with lamp/receiver slices)

Indicators read committed sensor/control state; glow/pulse is a binding. **Control:** a fading glow cannot latch a phantom true signal across rapid events or Reset.

<a id="p023-recoil"></a>
### P0-023 · recoil feedback (delivered with CAT-016)

One committed launch event binds one recoil clip; impulse stays physics-owned. **Control:** a blocked or rejected shot gives no recoil; duplicates add neither recoil nor impulse.

<a id="p023-ui"></a>
### P0-023 · ui feedback (delivered with ANIM-1)

Transition/pending/success clips follow acknowledged state and never complete Run or navigation themselves. **Control:** a delayed acknowledgement stays pending regardless of clip completion.

<a id="p023-acoustic"></a>
### P0-023 · acoustic feedback (delivered with CAT-009/CAT-061)

A committed acoustic occurrence drives declared sound/visual feedback; propagation stays physics-owned. **Control:** silence, no power or wrong tone fabricates nothing; disabled audio cannot change the receiver.

## Release checklist · P0-034 stage gate

<a id="p013-scratch"></a>
### P0-013 · p013-scratch
Remove one measured scratch allocation after a reproduced cost; results unchanged.

<a id="p013-checkpoint"></a>
### P0-013 · p013-checkpoint
Reuse one checkpoint across transaction lifetimes without narrowing rollback.

<a id="p013-predictor"></a>
### P0-013 · p013-predictor
Avoid one repeated computation with complete invalidation keys.

<a id="p013-sleep"></a>
### P0-013 · p013-sleep
Sleep a quiescent island and wake it on force/command/contact/topology change; visibility never influences authority.

<a id="p018-reliable"></a>
### P0-018 · p018-reliable
10,000 actual messages with delay/reorder/duplicate/reconnect: no loss or double application.

<a id="p018-pressure"></a>
### P0-018 · p018-pressure
Saturation cannot starve Reset/stop or fabricate successful commands.

<a id="p018-transfer"></a>
### P0-018 · p018-transfer
Each recipient owns bounded publication storage; no aliasing of transferred buffers.

<a id="p027-resource"></a>
### P0-027 · p027-resource
Identical artwork shares immutable GPU resources; per-instance parameters stay isolated; palette preserved.

<a id="p027-dirty"></a>
### P0-027 · p027-dirty
Only changed properties produce updates; invalidation remains complete.

<a id="p028-idle"></a>
### P0-028 · p028-idle
Idle rendering wakes for every visible change; frame caps cannot stall workers.

<a id="p028-visible"></a>
### P0-028 · p028-visible
Cosmetic-only visibility culling; physical replay identical offscreen.

<a id="p029-startup"></a>
### P0-029 · p029-startup
Startup bounds all three contexts and heaps: 15 s cold / 5 s warm on the dev machine.

<a id="p032-schedule"></a>
### P0-032 · p032-schedule
Declared concurrency schedules and injected delays/saturation produce the same timed replay.

<a id="p032-lifetime"></a>
### P0-032 · p032-lifetime
Failed mutations, compaction and recycled handles cannot resurrect stale state across 20 cycles/20 transitions.

<a id="p033-remediate"></a>
### P0-033 · p033-remediate
Remove one measured full-system bottleneck with matched before/after evidence.

<a id="p034-device"></a>
### P0-034 · p034-device
One declared device/workload meets every frozen budget: 30–60 FPS windows, 2.5 ms tick p95, memory after 20 cycles, ≥3 attempts, 5-minute thermal session. Missing hardware stays Incomplete.

<a id="wasm-experiment"></a>
### S044 · wasm-experiment
Optional measured compilation-option experiment (PERF-12); adopt only on attributable net benefit, otherwise record rejection. Never a dual shipped format.

## Consumer scope

Read [physics engine criteria](gpu-physics.md) for the numerical criteria staged inside the same slices and [vertical delivery](vertical-delivery.md) for the actual consumer. Ball-drop owns minimal Run/Reset/commit/device/pose support, basket-capture adds residence/event/halo, construction-save adds Save/Load, switch-lamp adds a typed activation edge, first-principles adds rotated Ramp contact and Captured success; each roadmap row after them adds one generic capability or one element by declaration and deletes the legacy it names.
