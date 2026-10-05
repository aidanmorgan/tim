# Canonical f16 game values and authoritative WebGPU physics

Owner decision: GPU-F16-DESIGN-2026-10-02, 2 October 2026, amended 5 October 2026 with the compilation model and game-grade envelope below. This is the current numerical and execution design. This is a video game: the owner explicitly accepts coarse, puzzle-scale physical precision as the end state and chooses one canonical f16 game-value model across authoring, content, saves, messages, presentation models and physics. Implementation and qualification status are tracked in [TODO](../TODO.md). This design asserts no measured speedup.

This document defines canonical Half game values, sole WGSL f16 numerical authority, the compilation model and the game-grade tolerance envelope. The [engine contracts](engine-contracts.md) define ownership, protocol, lifecycle and clock rules. Physical laws, puzzle coverage, typed identity, forward-only changes, exact construction Reset/save, independent review and browser proof remain required.

Physics follows the [general data-driven engine contract](engine-contracts.md#general-data-driven-engines): typed element declarations select reusable physical capabilities, never element-specific solvers or catalogue-keyed equations. Missing behavior extends the general engine within its consuming slice. Domain-specialised capability kernels selected by declared data are valid; element-selected kernels are not.

## One authority

The independent simulation worker owns a WebGPU adapter/device, compute pipelines, buffers and queue. WGSL compute implements authoritative continuous mechanics, constraints, collision/query geometry and numerical electrical, optical, acoustic, fluid, thermal and radiation models. It also computes numeric sensor predicates. C# remains the host/compiler/tooling language where viable: validated construction, typed command admission, integer ticks/IDs, discrete controllers/events, transactions and goal transitions consuming committed GPU results. A CPU calculation must not override a physical result or serve an unported numerical domain.

The animation worker remains separate. Main-thread 2dog/Godot WebGL rendering consumes committed read-only presentation data. WebGL is the renderer, not the physics compute API. There is no CPU solver, optional production backend switch, dual ownership, automatic format migration or legacy protocol interpretation. Missing WebGPU or the separate required `shader-f16` feature is explicit unsupported capability; every already-required device/browser tier remains a qualification requirement. Unsupported required tiers block release rather than disappearing from the support matrix.

A CPU editor preview is non-authoritative. Placement admission, overlap, sweeps, material boundaries and optical/acoustic/detector queries use the same quantized GPU geometry and typed identity contract as Run. External numeric text/API values are parsed and quantized once on entry to the canonical game model; the UI displays the admitted canonical value. They cannot supply a second production spatial oracle.

<a id="compilation-model"></a>
## Compilation model

The player's construction (placed elements, their configuration and typed connections) is **compiled at Run into a transformed initial state**: one typed scene record of bodies, shapes, materials, constraints, force regions, stores, sources, sensors and network nodes across every physical domain the game uses. **One generic WGSL f16 solver advances that state.** No puzzle element has its own solver, kernel branch, equation table or update loop; `engine/gpu/*.wgsl` and the solver dispatch in WorkshopPhysicsCompiler.cs contain no element or puzzle identifier.

The domains are capability families the single solver must cover: gravity/mechanical contact, electrical, light/optics, heat, acoustics, fluid/gas and radiation. Each later domain is added as a generic capability inside the slice that first needs it, never as an element-specific path.

A **separate animation model** is compiled from the same element data/configuration and the player's initial state and is driven one-way by committed physics results; it owns every cosmetic property. Physics and animation are two independent systems: two compiled models, two solvers/evaluators, two workers with independent clocks and tick rates ([REQ-01 and REQ-03](delivery-workflow.md#standing-requirements)), one-way data flow from committed physics to animation inputs, never shared mutable state and never one combined kernel or loop.

A third system, the **renderer**, runs on the browser main thread and draws the simulation as it runs. It consumes read-only inputs only: the latest committed physics pose/state directly and the latest animation sample, interpolating and presenting them. Data flows physics → animation → renderer and physics → renderer; nothing flows back. Rates are ordered physics (fastest, fixed tick) > animation ≥ renderer, where the renderer targets 30–60 FPS on supported devices ([REQ-02](delivery-workflow.md#standing-requirements)); physics always updates faster than rendering and rendering never influences physical results.

Adding a new puzzle element means adding its declaration data (the physics capabilities it instantiates, parameters, art and animation bindings) and nothing in the solver or the animation engine: new puzzle elements can be added in the future without changes to the physics or animation systems; an element is defined by its visual, physics and animation behaviours (declaration data) that the solvers use when the simulation runs.

## Numeric representation and admission

All authoritative continuous floating state, iterative solver arithmetic, dot products, reductions and law evaluations use concrete WGSL f16. No f32/f64 force accumulator, constraint solver or hidden CPU correction is permitted.

The following explicit exceptions do not create a second solver:

| Value | Representation and boundary |
| --- | --- |
| Identity, topology, material/mode tags, counts, iteration/tick/epoch/sequence | Typed integer values; closed sets are C# enums and validated generated numeric shader ABI discriminants. |
| Spatial coarse origin | Signed integer cell coordinates, cell width exactly 1/16 metre. This is an explicit physical representation component, not an ID. Local primary position is f16 in [-1/2, 1/2) cell units; its physical unit is 1/16 metre. Crossing a cell renormalizes the origin and local offset together. |
| Units/scales | Typed dimension and signed integer power-of-two scale exponent per compiled quantity group. Scaling metadata is immutable during an admitted transaction; a changed scale is an explicit recompilation/revalidation. |
| Canonical game values and durable saves | C# Half wrapped in dimension-specific value types through editor controls, construction, configuration, content, animation/read models, fixtures and tools; WGSL f16 for physical arithmetic. Current resources and construction saves serialize canonical half bits plus typed integer scales/cells losslessly through explicit codecs; do not assume Godot Export/Variant supports Half directly. Quantize at the explicit input boundary, not again differently at Run. Save is construction-only; Reset restores its exact canonical bits. |
| Scheduling and platform boundaries | IDs, ticks, counts, palette byte identities and indices keep their correct integer types. Host/browser clocks and required Godot/API arguments may be wider at explicit boundaries. Authored animation curves/state, geometry/material values, machine settings and difficulty/nudge/goal thresholds remain typed Half; CPU cosmetic operations explicitly round their declared results back to Half. Kernel dt is f16 under its declared scale. Widening f16 to render f32 is exact but cannot recover precision already lost. No wide stored game model or physical feedback. |
| Reference testing | Higher-precision analytical/offline oracles and read-only performance clocks/statistics are permitted outside game authority. Immutable host copies of committed raw f16/integer state are checkpoints, not CPU simulations. |

No previous floating format is accepted as a compatibility input. Forward-convert current authored resources, editor/property controls, previews, serialization, fixtures and tests together in their owned slices; reject obsolete schemas explicitly.

**Simple integration representation:** one integer cell origin plus one f16 local-position lane; no persistent compensation lane, exact-product machinery or higher-precision correction. During a tick, use a bounded f16 displacement relative to that tick's starting local frame for substep evaluation, then compose/quantize the committed local pose once. Carry k=floor(p+1/2) advances the integer origin and sets p=f16(p-k), with +1/2 advancing and -1/2 staying. All affected query kernels consume the same current candidate pose. WGSL-permitted rounding, contraction/reassociation, promoted implementation and subnormal flushing are accepted; we do not demand a strict compiler rounding trace or add dispatches merely to materialize arithmetic.

Cell-local coordinates reduce absolute-position quantization; they do not solve every small increment, long lever arm or cancellation problem. Relative frames are chosen before narrowing, using integer cell differences and bounded f16 local arithmetic. Do not convert a 64 m absolute position to f16 and then subtract it to recover a small gap.

Operand guards precede division, reciprocal, square root and accumulation. Normalized nonzero stored operands should lie within [2^-10,2^8] where the declared unit permits it. A zero denominator, impossible range or non-finite candidate discards that candidate for the affected body, keeps its previous committed pose and continues the tick; Reset always recovers. Admission bounds (below) are checked once at compile; the solver never rejects a tick on a numerical residual.

<a id="capability-inventory"></a>
## Generic capability inventory

Every element is composed from these shared capabilities; each entry is a typed declaration record, not a code path per element.

| Capability | Declared data |
| --- | --- |
| Rigid body | Mass, inertia tensor (sphere/box closed forms derived from shape and mass), local centre-of-mass offset, canonical pose, initial motion, dynamic/static flag; up to 16 dynamic bodies per scene in the first multi-body core. |
| Shapes | Sphere, box, plane; hollow/compound colliders as signed-distance fields in the same pair table. Radii, extents and local poses are authored data. |
| Materials | Restitution, bounce threshold, friction. Pair law: product restitution, maximum threshold, geometric-mean friction. |
| Constraints/joints | Typed hinge, slider, distance/rope and spring records with limits, stiffness and damping, solved by the shared sequential-impulse pass. |
| Force regions | Static-frame regions applying a declared bounded acceleration law (gravity, guide, fan/jet, buoyancy) to admitted targets; A≤16 m/s². |
| Sensors | Residence (open local bounds, speed limit, dwell in ticks), aperture (directional crossing with rearm) and orientation threshold (angle from initial pose, emits once, rearms on Reset), sampled at substep endpoints. |
| Contact triggers | Static owner, dynamic target, Half normal-approach-speed threshold; first qualifying impact per tick emits a typed occurrence. |
| Finite stores and sources | Declared capacity, charge/discharge law and depletion; no free energy. |
| Network nodes | Typed node and edge records for the electrical, optical, acoustic, thermal, fluid/gas and radiation domains, solved by generic per-domain capability passes in the same solver; each domain is added as a generic capability in the slice that first needs it, never as an element-specific path. This table grows with those slices. |

<a id="solver-model"></a>
## Solver model

Fixed physics tick 120 Hz with 480 Hz substeps (2/4/8 per commit admitted, see [shared-clock cadence](shared-clock-cadence.md)). Each substep: integrate velocities under declared forces/regions, build speculative contacts with margin |v|·dt + slop from an AABB broadphase and the generic shape-pair table (1–4 point manifolds), run sequential impulses (restitution, Coulomb friction, joints) for a bounded sweep count, apply slop-aware position correction, normalise every orientation quaternion, then sample sensors at the substep endpoint. Budgets (impulse sweeps, manifold points, contacts and events per substep) take the best iterate on exhaustion and continue. Contact order is stable body/collider/feature identity so same-build replay is exact.

<a id="game-grade-envelope"></a>
## Game-grade envelope

Units: 1 cell = 1/16 m. Values are clamp-or-continue policy; none rejects a tick.

| Quantity | Envelope | Behaviour |
| --- | --- | --- |
| Contact slop | Resting penetration ≤ 1/4 cell = 1/64 m (≈15.6 mm); visible temporary interpenetration ≤ 10% of the smaller body's span | Correct over substeps; never a fault |
| Contact velocity residual | Normal velocity clamped to ≥ 0 after the sweeps | Clamp; diagnostic log above 0.05 m/s |
| Effective acceleration | ≤ 64 m/s² linear, ≤ 1024 rad/s² angular | Clamp |
| Orientation | Quaternion normalised every substep; drift ≤ 5° over a 30 s Run | Normalise; drift is reference-test guidance |
| Solver budgets | Impulse sweeps, manifold points, contacts and events per substep | Best iterate, continue |
| Non-finite candidate | NaN/Inf in a body's candidate | Discard candidate, keep previous committed pose, continue; Reset recovers |
| Sensors/dwell | Sampled at substep endpoints, dwell counted in ticks | A through-pass shorter than the dwell never captures |
| Tunnelling | No body passes a wall ≥ 1 cell thick at ≤ 64 m/s | Speculative margin |v|·dt + slop |
| Trajectory | Outcome-based: same construction + inputs on the same machine → same outcome; 5% trajectory / 10% penetration / 5° orientation | Reference-test tool guidance, not slice gates |
| Energy | No free energy: a resting body never starts moving; second bounce apex lower than the first; a paid element (Bumper) never fires unpaid | Observable in Chrome; slice gate |

**Admission-only bounds** (checked once at compile, never per tick): speed cap 64 m/s, gravity ≤ 16 m/s², drag k ≤ 1/8 s⁻¹, positions within [-64,64] m per axis, collider extent ≤ 16 m, mass 1/1024–1024 kg. Out-of-range authored content rejects visibly before mutation and remains its owner's obligation.

**Startup** (not a per-slice gate): loading state shown; usable workshop within 15 s cold on the dev machine, 5 s warm. **Release checklist only (P0-030/031/032/034/035 stage gates, not per slice):** renderer 30–60 FPS, 2.5 ms tick p95, memory growth after 20 cycles, fault-injection matrices, device matrix, full fixture audit and 10,000-message transport tests, under the [stage gates](delivery-workflow.md#stage-gates).

Exact integer identity, events, causal order, transaction commit, canonical construction Save and Reset remain exact. Cross-device qualification compares intended outcomes inside this envelope; it does not promise bitwise physical equality. Element geometry, material values, thresholds and colours live in catalogue declarations, not in this document.

## Transaction, protocol and resource lifecycle

One forward protocol revision carries explicit packed f16 bits, typed scale/cell data and integer metadata. Specify byte layout, endianness, finite-domain validation and enum mappings; unknown tags/versions are rejected. Keep 64-bit identities/ticks exact. WGSL lacks C# enum syntax: generate validated numeric ABI definitions from the canonical typed declaration rather than string selectors. Exact byte counts are implementation detail recorded in code and slice evidence, not here.

Simulation owns candidate and committed buffers. Commands are acknowledged as applied only after dispatch and result transfer complete and the whole epoch/tick commits. CPU discrete results and GPU numerical state publish together; queued Run is not a running state. Bound in-flight work and queues; the worker remains responsive while awaiting GPU completion. Reject late generation/epoch results after Reset/Load/disposal.

Device loss discards the candidate and enters a typed fault state; a retained immutable last-committed host checkpoint may restore only the matching state on a newly qualified device. Otherwise require Reset/requalification; never silently continue on CPU. Run/Pause/Step, Reset, Save/Load, worker crash, GPU loss, cancellation and disposal each retain their own acknowledgement and rejection controls.

## Delivery order and cutover

The [ordered roadmap](planning/invest/vertical-delivery.md#rolling-playable-roadmap) names the slices; [TODO](../TODO.md) supplies the live outcome and blocker. ENGINE-CORE-1 brings the existing ball onto this envelope; ENGINE-CORE-2 replaces the analytic CCD core with the solver model above; every later slice adds one generic capability or one element by declaration and deletes the legacy it supersedes. No CPU/GPU selector, mixed numerical domain or delayed all-domain switch is permitted. Historical CPU convergence work and precision approvals are archived in git history, not current input. Primary API constraints: [WGSL floating point accuracy](https://www.w3.org/TR/WGSL/#floating-point-accuracy) and [WebGPU](https://www.w3.org/TR/webgpu/); numerical limits above are owner-authorized project policy, not guarantees made by those specifications.
