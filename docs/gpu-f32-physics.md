# High-Performance Generalised Data-Driven Physics Architecture

This document defines the numerical authority, compilation model, solver model, and game-grade tolerance envelope for the game engine. It supersedes earlier experimental WebGPU compute shader and Half-precision (`f16`) specifications with an architecture designed for **hundreds of concurrent puzzle elements**, **universal WebGL 2 and WebGPU compatibility**, **standard IEEE-754 `f32` precision with WebAssembly SIMD128 vectorization**, and a **lock-free zero-copy shared memory pipeline**.

The engine operates under the [general data-driven engine contract](engine-contracts.md) and maintains **strictly zero external dependencies for physics** (no Box2D, no Jolt, no Rapier, no PhysX, and zero npm/NuGet physics packages). All algorithms, vector mathematics, spatial hierarchies, and constraint solvers are custom, data-driven implementations authored directly within this repository. Typed element declarations select reusable physical capabilities, never element-specific solvers, bespoke equation tables, or per-part update loops. Missing behavior extends the general engine within its consuming slice. Domain-specialised capability kernels selected by declared data are valid; element-selected kernels are forbidden.

---

## Universal Web Platform Authority

### Dedicated Simulation Worker
Authoritative continuous mechanics, collision detection, constraint solving, and multi-domain physical fields execute exclusively on a dedicated simulation Web Worker. The target solver is high-performance C# compiled to WebAssembly with 128-bit SIMD (`wasm-simd128`) enabled, delivering deterministic, sub-millisecond execution times across hundreds of bodies while running natively across all modern browser engines on desktop and mobile. Today the solver is the JavaScript module `CuriousContraptions.Simulation/wwwroot/worker.js` running in that worker: it computes each tick in JavaScript doubles and commits velocities as f32 and pose as binary16 (see [f32 migration status](#f32-migration-status)); the WASM SIMD solver is remaining migration work.

### Universal WebGL 2 and WebGPU Support
By executing the core simulation in vectorized WASM on a dedicated worker, the physics engine is completely decoupled from the underlying graphics API:
- The game runs universally whether the browser renders via WebGL 2 or WebGPU.
- No dependency is placed on optional or fragmented GPU features such as `shader-f16` or compute shaders in the graphics pipeline.
- WebGL 2 is fully supported as a first-class production target alongside WebGPU.

### Zero-Copy Shared Memory Pipeline
The simulation worker and the browser main thread communicate via a pre-allocated `SharedArrayBuffer` under cross-origin isolation (`crossOriginIsolated`). The simulation worker writes committed body transforms and event states directly into a lock-free atomic multi-buffered ring buffer. The main thread renderer samples this shared ring directly in its animation loop and uploads transforms to GPU instance buffers with zero heap allocations, zero serialization overhead, zero garbage collection pauses, and zero GPU readback stalls (`mapAsync` is completely eliminated).

---

<a id="compilation-model"></a>
## Compilation Model and Tri-Graph Architecture

The game architecture strictly separates data and operations into three decoupled graphs optimized for their specific access patterns:

```
+-------------------------------------------------------------------------------+
|                       1. LOGICAL SCENE / MACHINE GRAPH                        |
|  - Owned by C# Main Thread (Authoring, Sockets, Parts, Serialization, UI)     |
|  - Relational graph: MachinePart nodes, SocketPin attachments, Wire edges     |
+---------------------------------------+---------------------------------------+
                                        | Compile-to-State at Run
                                        v
+-------------------------------------------------------------------------------+
|                       2. PHYSICS GRAPH (Execution Core)                       |
|  - Owned by WASM Worker (Flat SoA tables, SIMD arrays, Islands, Constraints)  |
|  - Topological: Bipartite graph (Bodies <-> Contacts/Joints)                  |
|  - Spatial: Dynamic AABB BVH with velocity fattening                          |
+---------------------------------------+---------------------------------------+
                                        | Lock-Free SharedArrayBuffer Ring
                                        v
+-------------------------------------------------------------------------------+
|                       3. RENDERING GRAPH (GPU Presentation)                   |
|  - Owned by Browser Main Thread (WebGL 2 / WebGPU Instanced Batches)          |
|  - Grouped by (MeshID, PipelineID); draws hundreds of instances in 1-2 calls  |
+-------------------------------------------------------------------------------+
```

### 1. Logical Scene Graph (Machine Graph)
The authoring representation manipulated by the player in build mode. It maintains high-level entity hierarchies, socket pin attachments, parameter configurations, electrical/signal wiring, and JSON save/load state. It is static during simulation runs and contains full object metadata.

### 2. Physics Graph (Simulation Core)
When the player presses **Run**, the logical scene graph is compiled into a flat, contiguous, SIMD-aligned **Physics Graph**:
- One homogeneous scene record comprising bodies, shapes, materials, constraints, force regions, energy stores, sensors, triggers, and network nodes across all physical domains (mechanical contact, electrical, optical, acoustic, pneumatic/fluid, thermal, radiation).
- **One generic solver advances this state.** No puzzle element has its own solver, kernel branch, equation table, or tick loop.
- Dynamic Bounding Volume Hierarchy (BVH) accelerates all spatial queries.
- Disjoint Set Union (Union-Find) partitions bodies and constraints into independent Simulation Islands.

### 3. Rendering Graph (Presentation Pipeline)
The visual representation on the browser main thread. It consumes read-only pose data from the shared memory ring buffer and translates them into GPU instanced draw batches grouped strictly by mesh and material pipeline.

### Independent Systems, One-Way Data Flow
- **Simulation Worker (Physics):** Runs at a fixed 120 Hz tick with 480 Hz sub-stepping. Owns physical truth.
- **Animation Worker (Cosmetics):** Compiles an independent animation model from the same authoring data; evaluates procedural curves, squash-and-stretch, and cosmetic states at 60 Hz. Fed one-way by committed physics results. Never feeds back into physical state.
- **Renderer (Main Thread):** Runs at display refresh cadence (30–144 FPS). Smoothly interpolates between committed physics pose brackets using shortest-arc Slerp. Physics always runs faster than rendering; rendering never influences physical results.

Adding a new puzzle element means adding declaration data (the capabilities it instantiates, its geometric parameters, art assets, and animation bindings). No new element requires solver modifications or bespoke simulation branches.

---

<a id="numeric-representation"></a>
## Numeric Representation and Precision

All authoritative continuous simulation state, velocity integration, spatial coordinates, and constraint solving use standard **IEEE-754 single-precision floating-point (`f32`)**.

### Precision Invariants
- **Single-Precision Float (`f32`):** 24-bit mantissa ($\approx 7.2$ decimal digits) and 8-bit exponent ($\pm 10^{38}$). This eliminates catastrophic cancellation in rotational torque arms, prevents exponent overflow in kinetic energy calculations, and provides native alignment for 128-bit SIMD registers (`wasm_v128`).
- **Quantized World Domain:** Simulation coordinates span a standard puzzle volume $[-64, 64]\text{ m}$ per axis with sub-millimeter precision. Coordinate snapping during construction quantizes to canonical grid units ($0.1\text{ m}$ or $1/16\text{ m}$), but internal solver arithmetic operates in full continuous `f32`.
- **WASM SIMD128 Vectorization:** 4-wide single-precision float operations (`f32x4.add`, `f32x4.mul`, `f32x4.min`, `f32x4.max`) execute simultaneously in hardware registers, vectorizing broadphase slab tests, impulse updates, and bounding box evaluations.
- **No Hidden Precision Channels:** No double-precision (`f64`) shadow state or narrow 16-bit float truncation is permitted in the simulation loop. Game saves serialize exact 32-bit float bit patterns alongside typed integer configuration states.

<a id="f32-migration-status"></a>
### f32 Migration Status

The f32 contract above is the target; the code does not meet it everywhere yet. The lanes below still store or compute game values as IEEE-754 binary16 (`System.Half` in C#, `getF16`/`setF16` in `CuriousContraptions.Simulation/wwwroot/worker.js`). Each is required forward refactoring, not a supported second format. Integers, enums and identity tags keep their types. "Remaining f32 migration" means unscheduled, enforced at the Epic 16 performance/qualification gate unless pulled earlier.

| Remaining binary16 lane | Where | Owning slice |
| --- | --- | --- |
| Committed pose: cell-local position remainders, orientation quaternion (`LocalPosition`, `CanonicalRotation`), motion-piece pose, gravity and acceleration lanes | `CanonicalBody.cs`, `WorkshopConstruction.cs`, `PhysicsMotionRead.cs`, `PhysicsBodyWire.cs`, worker.js | Remaining f32 migration |
| Physics declarations: mass, gravity, declared linear drag (body record +74, read with `getF16`), centre of mass, principal frame and inertia mantissa, collider frames/radii/half-extents, materials including rolling resistance (material record +14, `RollingResistance` is `Half`), triggers, orientation sensors, drives and motion windows | `PhysicsDeclarations.cs`, `RigidMassProperties.cs`, `OrientationSensorDeclaration.cs`, `PhysicsGpuAbi.cs`, `WorkshopPhysicsCompiler.cs`, worker.js | Remaining f32 migration |
| Construction values (`Metres`, `Kilograms`, ball materials) and the construction wire reused by the save codec | `WorkshopConstruction.cs`, `WorkshopWire.cs`, `WorkshopSaveCodec.cs` | Remaining f32 migration |
| Catalog resource `*Bits` fields (ball material, ramp/wall dimensions, bumper work, delay duration) | `engine/*Resource.cs`, `parts/catalog/*.tres` | Remaining f32 migration |
| Activation/timer phases, contact work `Joules`, puzzle precision and assistance windows | `ActivationTimers.cs`, `PhysicsGpuAbi.cs`, `ContactWorkDeclaration.cs`, `WorkshopPuzzle.cs`, `WorkshopPuzzleWire.cs` | Remaining f32 migration |
| Animation values, cosmetic durations and the `Half` lanes of the [animation channel ABI](presentation-bindings.md#declared-cosmetic-curves-anim-1b-and-ui-bindings-anim-1c) | `engine/presentation/AnimationValues.cs`, `WorkshopCosmetic.cs`, `WorkshopHint.cs` | Remaining f32 migration |
| Worker tick arithmetic runs in JavaScript doubles and rounds pose to binary16 (velocity to f32) at commit; the WASM SIMD f32 solver replaces it | worker.js | Remaining f32 migration |
| Retired binary16 WGSL kernels, following their obsolete `tools/GpuBodyFixture` consumer | Historical `engine/gpu/basketball.wgsl`, `engine/gpu/body-integration.wgsl` | Removed by Story 7.4; preserved in git history |

**Pipe owner decision (10 Oct 2026):** Story 7.4 removes the twelve parked straight-Pipe files, including their binary16 dimensions/profile/resource lanes. Story 6.6 rebuilds Pipe fresh as canonical f32 declaration data on the generic WASM SIMD physics engine; no legacy adapter, alias or migration is retained. Complete CAT-048 acceptance and Story 6.7 rim/collar controls remain required in roadmap order.

Until a lane migrates, envelope consequences that depend on its precision are stated against binary16.

**Migrated (Story 6.1c ENGINE-F32-VELOCITY, owner decision 9 Oct 2026):** committed linear velocity (m/s, unscaled) and angular velocity (rad/s) are IEEE-754 f32 end to end: body record +48–60 / +60–72, motion piece +64–76 / +76–88, the per-body response/trace record (`PhysicsBodyWire`, 64 bytes, +40–52 / +52–64), `LinearVelocity` / `AngularVelocity` (`float`) in C#, and `getFloat32`/`setFloat32` in the worker. The host rejects a non-finite value or a magnitude above 64 m/s or 128 rad/s. The 80-byte construction/save body record (`CanonicalBody`) carries f32 velocity at +64–76 that must be all-zero bits in a construction, so earlier saves (binary16 zero in the same bytes) load unchanged. The binary16 velocity lanes and their ×32 scale are deleted.

**Hardened (Story 6.1d):** the trace record schema is `WorkshopTraceVersion.CompleteBodySetF32Velocity` (3; the 56-byte binary16-velocity schema 2 is rejected like any unknown version) and the canonical body record schema is named `BodyRecordVersion.HalfPoseF32Velocity` (value 2 kept, because construction and save bytes are unchanged). The worker declares every body-record, motion-piece and response-body offset it uses as a named constant that `WorkerAbiTests` compares with its C# owner (`PhysicsGpuAbi`, `PhysicsMotionRead`, `PhysicsBodyWire`, `WorkshopWire`). Each motion piece carries the body's declared drag rate (+54, `PhysicsMotionRead.DragRateOffset`), so between-tick sampling follows the same exponential decay the solver applies.

---

<a id="capability-inventory"></a>
## Generic Capability Inventory

Every puzzle element is an instance of one or more shared physical capabilities compiled into flat data tables:

| Capability | Declared Data Content | Execution Behavior |
| :--- | :--- | :--- |
| **Rigid Body** | Mass, 3x3 inertia tensor, world pose (position vector, orientation quaternion), linear and angular velocities, linear drag coefficient, motion type (Dynamic, Static, Kinematic), island index. | Integrated via symplectic Euler at 480 Hz; declared linear drag decays the linear velocity exactly ($e^{-c\,\Delta t}$) after gravity each sub-step, and no angular drag exists; orientation quaternion normalized every sub-step. Scalable to hundreds of dynamic bodies. |
| **Colliders & Shapes** | Shape discriminant (Sphere, Box, Capsule, Plane, Hollow SDF), local frame transform, half-extents/radii, participation filter masks. | Tested in narrowphase via branchless analytic manifold generators and signed distance fields. |
| **Materials** | Restitution coefficient, bounce threshold velocity, static and dynamic friction coefficients, rolling-resistance coefficient $C_{rr}$ (balls declare it; boxes, planes and walls declare zero). | Pair laws: geometric-mean friction, product restitution, maximum bounce threshold; at every contact (static or dynamic partner) the larger $C_{rr}$ times the larger sphere radius bounds one rolling-resistance row. |
| **Constraints & Joints** | Connected body pair, local anchor offsets, constraint axes, linear/angular limits, Box2D v3 soft parameters (natural frequency $\omega$, damping ratio $\zeta$). | Solved via Temporal Gauss-Seidel Soft Step (TGS Soft) with 4-wide SIMD vectorization. Unifies hinges, sliders, distance links, and ropes. |
| **Force Regions** | Spatial bounds (box, cylinder, cone), bounded acceleration vector or field equation (gravity, directional guide, fan airflow, fluid buoyancy). | Applied during sub-step force accumulation; acceleration clamped to $\le 64\text{ m/s}^2$. |
| **Sensors** | Spatial trigger volume, target filter mask, sensor kind (Aperture Crossing, Convex Residence), required dwell duration, velocity threshold. | Evaluated continuously: aperture crossings use exact sub-tick planar root-finding; residence sensors accumulate dwell over sub-steps. |
| **Contact Triggers** | Target collision pair, approach velocity threshold, rearm condition. | First qualifying impact per tick generates a typed event occurrence; triggers mechanical or electrical transitions. |
| **Orientation Sensors** | Sensed dynamic body, admitted initial orientation, threshold as the cosine of half the angle (Domino: 45°). | Evaluated at every sub-step endpoint: $|\langle q, q_0\rangle| \le \cos(\theta/2)$ fires one typed occurrence; the fired state is sticky within the world and only a fresh admission (Reset) rearms it. |
| **Finite Work Stores** | Stored potential energy capacity, discharge rate limit, recharge coupling. | Thermodynamic power debiting: total power extracted by connected loads cannot exceed available stored power ($\Delta K \le E_{\text{store}}$). |
| **Network Nodes** | Terminal IDs, node potential, branch conductance, transmission latency, spectral optical band, acoustic frequency. | Evaluated in staged domain sub-passes (Modified Nodal Analysis for circuits, ray marching for optics, acoustic field culling). |

---

<a id="solver-model"></a>
## Solver Model and Scalable Mechanics

The solver advances physical time at a fixed **120 Hz tick with 480 Hz sub-stepping** (exactly 4 sub-steps per tick; each sub-step is rational $h = 1/480\text{ s} \approx 2.0833\text{ ms}$).

```
      Single Physics Tick Pipeline (120 Hz)
      =====================================
      [Phase 1: Electrical & Logic Network MNA Solve]
                         |
                         v
      [Phase 2: Environmental Fields: Optics, Acoustics, Airflow]
                         |
                         v
      [Phase 3: 4 x 480 Hz Mechanical Sub-Steps]
         For each sub-step:
           1. Dynamic BVH Broadphase (fat AABB queries)
           2. Analytic Narrowphase (4-point contact manifolds, SDFs)
           3. Island Graph Construction & Sleeping Assessment
           4. Force & Torque Integration (Symplectic Euler)
           5. Box2D v3 TGS Soft Constraint Solve (Impulses)
           6. Velocity Integration & Position Advancement
           7. Quaternion Normalization
                         |
                         v
      [Phase 4: Thermodynamic Energy Debiting & Power Scaling]
                         |
                         v
      [Phase 5: Continuous Sensor Evaluation & Event Dispatch]
                         |
                         v
      [Commit State to SharedArrayBuffer Triple-Buffer Ring]
```

### 1. Broadphase: Dynamic AABB BVH with Velocity Fattening
- All colliders are indexed in a dynamic binary bounding volume hierarchy.
- **Incremental Tree Rotations:** Insertion and updates use surface-area heuristic tree rotations to maintain balance in $O(\log N)$ time without full tree rebuilds.
- **Fat AABBs:** Bounding boxes are speculatively expanded by velocity and slop:
  $$\mathbf{AABB}_{\text{fat}} = \left[ \mathbf{p}_{\text{min}} - (|\mathbf{v}| h + \mathbf{s}_{\text{slop}}), \; \mathbf{p}_{\text{max}} + (|\mathbf{v}| h + \mathbf{s}_{\text{slop}}) \right]$$
  If an object remains inside its fat AABB, zero tree modifications occur, eliminating up to 95% of tree updates for resting or sliding objects.

### 2. Narrowphase: Analytic Manifolds & 4-Point Area Reduction
- **Branchless Analytic Primitives:** Sphere-Sphere, Sphere-Box, Box-Box (15-axis SAT with reference-face clipping), Sphere-Plane, Box-Plane (up to four deepest vertices) and Capsule-Plane are computed via closed-form geometry without iterative GJK/EPA loops.
- **One Constraint Row for Every Pair:** each manifold point becomes the same TGS Soft row (lever arms about the centre of mass, world inverse inertia from the compiled principal moments, two-axis Coulomb friction with a circular cone, warm start keyed by collider pair and feature, speculative margin, dissipative restitution). Pair laws are geometric-mean friction, product restitution and maximum bounce threshold from the declared materials; no shape or element owns a different response. Every manifold with a sphere adds one two-axis rolling-resistance row (Box2D v3 coefficient mixing, not warm-started: its impulse restarts from zero every sub-step), solved after the normal rows of each sweep and applied equal and opposite to both bodies whether the partner is static or dynamic: it drives the relative rolling about the tangent axes toward zero within the circular bound $C_{rr}\,r\sum\lambda_N$, so it never reverses a ball on a static support (against a dynamic anisotropic box the per-axis diagonal effective masses are an approximation); spin about the contact normal is not resisted. A struck body (for example a Domino a ball rolls over) receives the reaction, so its outcome includes the ball's resistance (owner decision 9 Oct 2026).
- **Hollow SDFs:** Pipes, chutes, and funnels evaluate signed distance fields with toroidal rim caps, eliminating snagging and internal face artifacts.
- **4-Point Manifold Reduction:** Contact polygons from face clipping are reduced to the 4 area-maximizing extreme points (deepest point, furthest point, and two points maximizing triangle/quad area).
- **Feature ID Warm-Starting:** Each contact point caches a 32-bit packed `FeatureId`. Matching features across sub-steps inherit accumulated normal and friction impulses, reducing solver convergence sweeps by over 50%.
- **Speculative Contacts:** Fast-moving objects within predicted closing distance $d \le |\mathbf{v}_{\text{rel}}| h + \mathbf{s}_{\text{slop}}$ allocate speculative constraints that remove excess closing velocity, preventing tunneling without continuous swept root-finding.

### 3. Constraint Solver: Box2D v3 "TGS Soft" Formulation
Every contact, limit, and joint is modeled as an implicit damped spring-damper constraint integrated directly into the effective constraint mass:
$$\beta = \frac{h \omega}{2 \zeta + h \omega}, \quad \gamma = \frac{1}{h^2 m_{\text{eff}} (2 \zeta \omega + h \omega^2)}$$
$$\mathbf{M}_{\text{eff}} = \left( J M^{-1} J^T + \gamma \right)^{-1}$$
$$\Delta \lambda = -\mathbf{M}_{\text{eff}} \left( J v + \frac{\beta}{h} C + \gamma \lambda_{\text{total}} \right)$$
- The compliance $\gamma$ and softness bias $\beta$ naturally absorb large penetration errors without injecting artificial kinetic energy.
- **Provably Dissipative:** Eliminates Baumgarte position projection explosions, ensuring smooth, rock-solid joint assemblies and stacks.
- **4-Wide SIMD Batching:** 4 independent constraints or manifold points are evaluated in parallel using WASM SIMD128 `f32x4` instructions.

### 4. Island Partitioning and Sleeping
- Active contacts and joints define graph edges connecting dynamic bodies.
- Disjoint Set Union (Union-Find) with path compression partitions the world into independent Simulation Islands.
- **Sleeping State Machine:** Islands where all bodies exhibit linear velocity $\|\mathbf{v}\| < 0.01\text{ m/s}$ and angular velocity $\|\boldsymbol{\omega}\| < 0.02\text{ rad/s}$ for $>0.5\text{ s}$ enter sleep mode. Sleeping islands bypass collision detection and constraint solving entirely (0 ms CPU cost).
- **Parallel Dispatch:** Large active islands are distributed across worker threads via atomic work-stealing queues over `SharedArrayBuffer`.

---

<a id="modern-engine-patterns"></a>
## Modern Engine Architectural Patterns (Zero-Dependency In-Tree Formulation)

To guarantee industry-grade stability, sub-millisecond multi-body throughput, and deterministic replays across desktop and mobile browsers, the engine incorporates ten core architectural best practices inspired by modern high-performance physics engines (such as Jolt Physics), implemented strictly as **100% custom, in-engine, data-driven C# and TypeScript code with zero external libraries or dependencies**:

### 1. 4-Wide SIMD Quad-AABB BVH (`wasm-simd128`)
In place of scalar binary trees, the broadphase spatial hierarchy is structured as a 4-child Bounding Volume Hierarchy (Quad-BVH) directly mapped to WebAssembly 128-bit SIMD vector registers:
- **Transposed Node Layout:** Each inner node packs the AABBs of its 4 children into transposed SIMD vectors:
  - $\mathbf{min}_x = [c_0.\min.x, \; c_1.\min.x, \; c_2.\min.x, \; c_3.\min.x]$
  - $\mathbf{max}_x = [c_0.\max.x, \; c_1.\max.x, \; c_2.\max.x, \; c_3.\max.x]$
  - $\mathbf{min}_y, \mathbf{max}_y, \mathbf{min}_z, \mathbf{max}_z$ arranged identically.
- **Single-Instruction 4-Way Overlap Test:** Testing a query volume (or fat AABB) against all 4 child bounds executes concurrently using `f32x4.min`, `f32x4.max`, `f32x4.le`, and `f32x4.ge`, producing a 4-bit hit mask in a single instruction sequence (`i32x4.bitmask`).
- **$\log_4$ Tree Depth:** The Quad-BVH halves the tree depth compared to binary trees ($O(\log_4 N)$), minimizing cache line misses during tree traversal and eliminating CPU branch mispredictions.

### 2. Dual-Tree Broadphase (Static vs. Dynamic Acceleration)
Inspired by Jolt's dual-tree partitioning, the world broadphase is bifurcated into two specialized spatial structures:
- **Static Quad-BVH:** Holds all non-moving level geometry (workbench plane, static walls, ramps, guides, fixed machines). Built once or updated lazily with Surface Area Heuristic (SAH) tree rotations. Zero per-tick updates or node refits occur for static colliders.
- **Dynamic Quad-BVH:** Holds dynamic rigid bodies (basketballs, bowling balls, dominos) with velocity-fattened bounding boxes:
  $$\mathbf{AABB}_{\text{fat}} = \left[ \mathbf{p}_{\text{min}} - (|\mathbf{v}| h + \mathbf{s}_{\text{slop}}), \; \mathbf{p}_{\text{max}} + (|\mathbf{v}| h + \mathbf{s}_{\text{slop}}) \right]$$
- **Pruned Query Matrix:** Dynamic-to-Static queries traverse the static tree without ever rebuilding it; Dynamic-to-Dynamic queries traverse the dynamic tree; Static-to-Static interactions are completely skipped (0 ms cost).

### 3. Speculative Contacts for Continuous Collision Detection (CCD)
High-speed rigid bodies (such as balls accelerated by gravity or bumpers hitting thin walls) can traverse more than their bounding extent in a single sub-step, causing discrete collision tunneling. Instead of computationally expensive swept root-finding or interval arithmetic:
- **Velocity-Expanded Speculative Distance:** For bodies with relative approach velocity $\mathbf{v}_{\text{rel}} \cdot \mathbf{n} > 0$, the contact generation envelope is expanded along the separating normal by the speculative margin:
  $$d_{\text{spec}} = |\mathbf{v}_{\text{rel}} \cdot \mathbf{n}| \, h + \mathbf{s}_{\text{slop}}$$
- **Early Constraint Allocation:** When the separation distance $g$ satisfies $0 < g \le d_{\text{spec}}$, a speculative contact constraint is allocated in the narrowphase manifold.
- **Target Velocity Absorption:** The constraint sets a target relative velocity:
  $$v_{\text{target}} = -\frac{g}{h}$$
- **Zero Penetration without Ghost Collisions:** The TGS Soft solver applies an impulse that reduces the approaching normal velocity to zero exactly when the surface is reached, preventing tunneling through walls of arbitrary thickness while ensuring that glancing bodies continue without artificial snagging.

### 4. Hierarchical `SubShapeID` & Feature Warm-Starting
- **Compound Part Identification:** Every collider shape packs a 32-bit hierarchical `SubShapeID` indicating its sub-component index (e.g. pipe inner cylinder vs torus rims, compound levers).
- **Persistent Contact Caching:** Pairs are indexed as $(\text{Body}_A, \text{SubShape}_A, \text{Body}_B, \text{SubShape}_B, \text{FeatureId})$.
- **Warm-Starting Impulses:** Matching contact features across sub-steps inherit accumulated normal impulse $\lambda_n$ and tangential friction impulse $\lambda_t$, reducing solver iteration counts to convergence by over 50%.

### 5. Deterministic 4-Point Area-Maximizing Manifold Reduction
Polyhedral contact generation (such as 3D Box-Box SAT clipping or domino-on-workbench contact) can generate complex polygonal intersection shapes with 6 to 8 vertices. To guarantee constant-sized solver batches and numeric determinism:
- **Extreme Point 1:** The point with the maximum penetration depth (deepest contact).
- **Extreme Point 2:** The point furthest from Point 1 in Euclidean distance.
- **Extreme Point 3:** The point that maximizes the cross product magnitude $|(\mathbf{p}_3 - \mathbf{p}_1) \times (\mathbf{p}_2 - \mathbf{p}_1)|$, maximizing the triangle support area.
- **Extreme Point 4:** The point on the opposite side of the line $\overline{\mathbf{p}_1 \mathbf{p}_2}$ that maximizes the quadrilateral area.
- **Constant 4-Point Manifold:** Every planar contact manifold is clamped to at most 4 stable support points. This enables fixed-size Structure-of-Arrays (SoA) SIMD batching in the TGS Soft solver with zero dynamic heap allocations.

### 6. Unified Compliant Soft Constraints (Spring-Damper Formulation)
All bilateral and unilateral constraints—rigid contact normals, Coulomb friction, 1D prismatic sliders (`CAT-062a` Springboard), revolute hinges, distance links, and gear/pulley ratios—are unified under an implicit compliant spring-damper formulation:
- **Kinematic Constraint & Jacobian:** For any constraint function $C(\mathbf{x}) = 0$, the kinematic velocity constraint is $J \mathbf{v} = 0$, where $J = \frac{\partial C}{\partial \mathbf{x}}$.
- **Compliance $\gamma$ and Softness $\beta$:** Parameterized by natural frequency $\omega$ (stiffness) and damping ratio $\zeta$:
  $$\beta = \frac{h \omega}{2 \zeta + h \omega}, \quad \gamma = \frac{1}{h^2 m_{\text{eff}} (2 \zeta \omega + h \omega^2)}$$
- **Unified Effective Mass:**
  $$\mathbf{M}_{\text{eff}} = \left( J M^{-1} J^T + \gamma \right)^{-1}$$
- **Unconditionally Stable:** Large penetrations or initial configuration errors are smoothly absorbed over sub-steps without injecting artificial energy, completely eliminating Baumgarte position projection and velocity clamps.

### 7. Island Partitioning and Deactivation (Sleeping System)
- **Disjoint Set Union (DSU):** Fast Union-Find with path compression partitions connected active bodies into independent simulation islands.
- **Sleeping State Machine:** Islands where all bodies exhibit linear velocity $\|\mathbf{v}\| < 0.01\text{ m/s}$ and angular velocity $\|\boldsymbol{\omega}\| < 0.02\text{ rad/s}$ for $>0.5\text{ s}$ enter sleep mode.
- **Zero-Cost Sleep:** Sleeping islands bypass broadphase, narrowphase, and constraint solving entirely (0 ms CPU cost).
- **Instant Wakeup:** Any external impulse, user manipulation, or contact from an active dynamic body immediately wakes the entire island.

### 8. Zero-Allocation `TempAllocator` & Fixed-Capacity Buffers
- **No Dynamic Heap Allocation:** The 120 Hz / 480 Hz simulation loop executes with zero dynamic allocations (`new` or GC garbage generation).
- **Pre-Allocated Memory Pools:** Contact candidate pairs, clipped manifold points, and island evaluation stacks are provisioned from a linear bump `TempAllocator` reset at the end of each tick, eliminating browser Web Worker GC stutter.

### 9. Symplectic Euler Split-Integration & Multi-Substepping
- **High-Frequency Substeps:** Stepping at 480 Hz (4 sub-steps of $h = 1/480\text{ s}$ per 120 Hz tick) stabilizes extreme mass ratios (e.g. 1:1000) and stiff contact limits with minimal solver iterations.
- **Symplectic Velocity-Position Staggering:**
  $$\mathbf{v}_{t+h} = \mathbf{v}_t + \mathbf{M}^{-1} \mathbf{F}_{\text{ext}} h + \mathbf{M}^{-1} \mathbf{J}^T \boldsymbol{\lambda}$$
  $$\mathbf{x}_{t+h} = \mathbf{x}_t + \mathbf{v}_{t+h} h$$
  $$\mathbf{q}_{t+h} = \text{Normalize}\left( \mathbf{q}_t + \frac{h}{2} \boldsymbol{\omega}_{t+h} \mathbf{q}_t \right)$$
  The symplectic phase-space property preserves total energy and prevents numerical artificial heating over extended runs.

### 10. Deterministic Pair Sorting & 64-Byte Cache-Line Aligned Memory
- **Canonical Pair Ordering:** Candidate pairs from broadphase and contact manifolds are sorted strictly by $(\min(\text{BodyA}, \text{BodyB}), \; \max(\text{BodyA}, \text{BodyB}), \; \text{FeatureId})$. This guarantees identical constraint evaluation order and bitwise-reproducible simulation results regardless of worker thread scheduling.
- **64-Byte Cache-Line Aligned SoA Tables:** Dynamic body states (positions, orientations, velocities, inertias) are packed into flat contiguous arrays aligned to 64-byte CPU cache lines, maximizing SIMD memory bandwidth on WASM workers.

---

<a id="game-grade-envelope"></a>
## Game-Grade Envelope

The engine enforces a strict clamp-or-continue policy. Numerical errors, extreme velocities, or anomalous user constructions are handled cleanly without crashing the browser or terminating the simulation tick. Development timing budgets (e.g., <0.5 ms tick time, 60 FPS pacing, zero clock-debt over 20 runs) are target recommendations and advisory telemetry during incremental development slices; they are not blocking pass/fail gates for functional slices. Formal multi-device and workload qualification is enforced at the P0-034 stage gate.

| Physical Quantity | Permissible Envelope | Runtime Recovery Behavior |
| :--- | :--- | :--- |
| **Contact Slop** | Resting penetration $\le 0.5\text{ mm}$; temporary visual penetration $\le 5\%$ of smaller body extent. | Smoothly resolved over sub-steps via soft compliance $\gamma$; never a tick fault. |
| **Normal Velocity Residual** | Normal approach velocity clamped to $\ge 0$ following impulse sweeps. | Clamped; velocities $< 0.05\text{ m/s}$ treated as resting contact (zero bounce). |
| **Maximum Velocities** | Linear speed $\le 64\text{ m/s}$; angular speed $\le 128\text{ rad/s}$. | Every substep clamps twice to $(1-2^{-20})$ of each bound: before position integration, and again after the relax sweeps and restitution pass, which can add speed after the first clamp (a 100 kg sphere at 64 m/s would send a resting 1 kg sphere off at about 127 m/s; it commits just under 64 m/s). The $2^{-20}$ headroom covers f32 rounding of the committed components (at most $2^{-24}$ relative), so the committed squared magnitude, summed in double as the host checks it, stays within $64^2$ and $128^2$ in all 16 tested directions; without the headroom 6 of the 16 commit above each bound. A finite velocity of any size clamps (an f64 length overflow is measured at reduced scale); only a non-finite component drops the motion while the finite pose stands. The host rejects non-finite or larger committed values. Prevents numerical divergence. |
| **Committed Velocity Resolution** | Each tick commits linear and angular velocity as f32 (about $2^{-24}$ of the value). A per-tick decrement below half that step would round back to the previous value; the declared decelerations are orders of magnitude above it (drag 0.04 1/s removes $1.7\times10^{-4}$ of the speed per 240 Hz tick). | Declared linear drag alone slows a free-flying ball on every committed tick from low speed to the envelope bound: the balls' 0.04 1/s follows $2e^{-0.04t}$ within 1% at 60, 120 and 240 Hz, and $63.9e^{-0.04t}$ within 0.1% on every 240 Hz tick from 63.9 m/s (its relative decrement does not depend on speed). Declared rolling resistance decelerates a rolling ball on every committed tick up to 5 m/s: a Basketball rolling at 5 m/s at 240 Hz decelerates at $(5/7)(C_{rr}g + c\,v)$ (0.367 m/s² measured near 4.5 m/s), its speed and spin fall on every tick, and it rests after about 16 s; one struck to $\le 1\text{ m/s}$ rests at the same time within 10% at all three cadences. Faster rolling still decelerates at that rate over a second (20 m/s within 10%, spin never rising), but on a fast-spinning sphere the contact rows act intermittently, so single ticks skid with only drag acting; this is a solver limit, not precision (an f32 step at 20 m/s is $2\times10^{-6}$ m/s), recorded in deferred-work. No rest threshold zeroes velocity. |
| **Orientation Quaternion** | Normalization error $\|\mathbf{q}\| - 1 \le 10^{-4}$. | Renormalized every sub-step via fast inverse square root. |
| **Non-Finite Quantities** | NaN or $\pm\infty$ detected in candidate state. | Candidate discarded; body retains previous committed pose; simulation continues uninterrupted. |
| **Tunneling Threshold** | Zero penetration through solid barriers $\ge 1\text{ cm}$ thick at speeds $\le 64\text{ m/s}$. | Prevented by 480 Hz sub-stepping combined with speculative contact margins. |
| **Thermodynamic Energy** | No free energy: $\Delta K_{\text{system}} \le E_{\text{store}}$. | Total power demands exceeding available source power are scaled down proportionally ($\alpha = P_{\text{avail}} / P_{\text{demand}}$). |
| **Sensor Crossing** | Planar crossing timestamp precision $\le 0.1\text{ ms}$. | Continuous linear sub-tick interpolation between starting and ending poses. |

---

## Forward-Only Refactoring and Deletion Mandate

This architecture operates on a strict **forward-only refactoring policy**:
- **Zero Backwards-Compatibility Shims:** No compatibility wrappers, transitional adaptors, emulation shims, or parallel CPU/GPU execution modes are permitted.
- **Code Removal:** Superseded systems—including old WebGPU WGSL compute shaders (`physics.wgsl`), Half-precision (`f16`) bitwise manipulation codecs, and Baumgarte position stabilization—are permanently removed from the active codebase upon completion of the forward implementation.
- **Preservation through Refactoring:** All 72 named puzzle elements, their catalog definitions, and the 150 progressively taught campaign levels remain fully preserved and are refactored directly to the new declarative capability formats.
