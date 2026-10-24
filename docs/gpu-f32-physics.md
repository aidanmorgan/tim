# High-Performance Generalised Data-Driven Physics Architecture

This document defines the numerical authority, compilation model, solver model, and game-grade tolerance envelope for the game engine. It supersedes earlier experimental WebGPU compute shader and half-precision (`f16`) specifications with an architecture designed for **hundreds of concurrent puzzle elements**, **universal WebGL 2 and WebGPU compatibility**, **standard IEEE-754 `f32` precision with WebAssembly SIMD128 vectorization**, and a **lock-free zero-copy shared memory pipeline**.

The engine operates under the [general data-driven engine contract](engine-contracts.md) and maintains **strictly zero external dependencies for physics** (no Box2D, no Jolt, no Rapier, no PhysX, and zero npm/NuGet physics packages). All algorithms, vector mathematics, spatial hierarchies, and constraint solvers are custom, data-driven implementations authored directly within this repository. Typed element declarations select reusable physical capabilities, never element-specific solvers, bespoke equation tables, or per-part update loops. Each puzzle element is declared once in its own `elements/<Name>/` package; `tools/BundleElements` writes the packages into one element bundle at build time, and the runtime loads only that bundle ([bundle loading](engine-contracts.md#element-bundle-loading-contract)), identifying elements by the typed id the bundle issues. New elements declare their capabilities, artwork, and bindings without engine enum edits or switch statements, and the Free Workshop palette lists every bundle element. The [sole design authority](architecture/element-package-isolation.md) governs this design; its Current status lists where the tree falls short. Missing behavior extends the general engine within its consuming slice. Domain-specialised capability kernels selected by declared data are valid; element-selected kernels are forbidden.

---

## Universal Web Platform Authority

### Dedicated Simulation Worker
Authoritative continuous mechanics, collision detection, constraint solving, and multi-domain physical fields execute exclusively on a dedicated simulation Web Worker. The target solver is high-performance C# compiled to WebAssembly with 128-bit SIMD (`wasm-simd128`) enabled, delivering deterministic, sub-millisecond execution times across hundreds of bodies while running natively across all modern browser engines on desktop and mobile. All solver arithmetic runs in the C# WASM worker; `CuriousContraptions.Simulation/wwwroot/worker.js` is a host that forwards typed envelopes and copies committed bytes into the shared rings, with no physics. Position, rotation and velocity are committed as f32 through one canonical pose path. No half-precision lane remains ([precision status](#precision-status)); this is not a claim of complete SIMD or whole-engine f32 qualification.

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
The authoring representation manipulated by the player in build mode. It maintains high-level entity hierarchies, socket pin attachments, parameter configurations, electrical/signal wiring, and JSON save/load state. It is static during simulation runs and contains full object metadata. Puzzle elements come from the element bundle and are identified by the typed id it issues; the Free Workshop palette lists every bundle element. Connection ports (exposed/output and accepted/input) and their connection domains (`Electrical`, `Activation`) are declared in each element's package, referencing `Loc_Port_<Id>` locator nodes in its `.tscn` scene to establish 3D socket anchors without hardcoded coordinate tables.

### 2. Physics Graph (Simulation Core)
When the player presses **Run**, the logical scene graph is compiled into a flat, contiguous, SIMD-aligned **Physics Graph**:
- One homogeneous scene record comprising bodies, shapes, materials, constraints, force regions, energy stores, sensors, triggers, and network nodes across all physical domains (mechanical contact and electrical supply routes today; optical ray marching, acoustic/pneumatic fields, thermal and radiation as their stories land).
- Colliders originate from the element's own `.tscn` scene: its collision proxy nodes (`col_box`, `col_sphere`, `col_capsule`, `col_cylinder`, `col_hull`) are extracted without element branching (at build time into the bundle, under Story 9A.16) and mapped to flat SIMD collider tables. Box, sphere and (Story 10.1) cylinder proxies are admitted: a `CylinderShape3D`, or a `CylinderMesh` whose top and bottom radii are equal, with explicit `radius` and `height`, axis local +Y and equal radial scale, bakes as the solid Cylinder $(r, w/2, r)$; capsule and hull proxies are rejected until their stories admit them. No procedural geometry or hardcoded shape tables are permitted in C#.
- The WASM SIMD physics worker consumes flat generic capability and collider tables compiled from the bundle's declarations, sized from the compiled scene, with strictly zero catalogue-specific kernel branches, solvers, or update loops.
- **One unified generalised data-driven physics engine advances this state.** Strictly zero element-specific solvers, zero per-part update loops, and zero element-selected kernel branches.
- Multi-domain solver couplings (compiled electrical supply routes over the reachability graph, discrete activation events) compile from declared ports rather than hardcoded C# tables or element-specific branches. Modified Nodal Analysis is a named later capability, added by the first element story that needs it.
- Flat contiguous SIMD-aligned Structure-of-Arrays (SoA) memory tables with 64-byte cache line alignment.
- Dynamic Bounding Volume Hierarchy (BVH) with speculative velocity fattening accelerates all broadphase spatial queries.
- Disjoint Set Union (DSU / Union-Find) partitions bodies and constraints into independent Simulation Islands.
- Generic Box2D v3 Temporal Gauss-Seidel (TGS Soft) solver unifies non-penetration, friction, and bilateral joints into effective constraint mass.
- Narrowphase collision detection uses generic analytic manifolds (Sphere, Box, Plane, solid Cylinder; Capsule later) and signed distance fields (SDF).
- Multi-domain physical fields (electrical supply, finite energy stores; optical rays and acoustics as their stories land) execute through shared capability kernels.

### 3. Rendering Graph (Presentation Pipeline)
The visual representation on the browser main thread. It consumes read-only pose data from the shared memory ring buffer and translates them into GPU instanced draw batches grouped strictly by mesh and material pipeline, using visual PBR meshes from the element's `.tscn` scene.

### Independent Systems, One-Way Data Flow
- **Simulation Worker (Physics):** Runs at a fixed 120 Hz tick with 480 Hz sub-stepping on a dedicated WebAssembly SIMD worker. Owns physical truth. The physics worker consumes flat generic capability and collider tables compiled from the bundle's declarations, advancing state through the unified, generalised, data-driven engine with strictly zero catalogue-specific kernel branches, solvers, or update loops.
- **Animation Worker (Cosmetics):** All animation executes on an independent Animation Web Worker at 60 Hz, completely decoupled from physical truth. Compiles an independent animation model from the same authoring data; evaluates procedural curves, squash-and-stretch, motor spin, indicator/UI easing, and particle effects driven by declarative bindings to committed physical poses, committed level states and one-shot event-ring occurrences. Strictly zero per-element animation scripts or callbacks in the animation host. Fed one-way by committed physics results. Never feeds back into physical state.
- **Renderer (Main Thread):** Runs at display refresh cadence (30–144 FPS). Smoothly interpolates between committed physics pose brackets sampled from the zero-copy `SharedArrayBuffer` ring using shortest-arc Slerp.
Data flows strictly one-way: physics → animation → renderer and physics → renderer; nothing flows back into physics. Physics always runs faster than rendering; rendering never influences physical results.

Adding a new puzzle element means adding declaration data (the capabilities it instantiates, its geometric parameters, exposed/accepted ports with named mesh node socket anchors, art assets, and animation bindings) to its own `elements/<Name>/` package. No new element requires solver modifications, engine enum additions, hardcoded connection tables, or bespoke simulation branches.

<a id="zero-element-assumptions-physics"></a>

### Zero Element Assumptions & Generic Combination Invariant

The physics simulation core and compilation pipeline enforce the non-negotiable architectural invariant of **zero element assumptions and generic combination support**:
- **Zero Element Assumptions:** The physics engine makes strictly zero assumptions about which puzzle elements exist or have been loaded. No part identity, kind tag, or catalog class exists in the solver.
- **Universal Arbitrary Combinations:** The engine supports ANY arbitrary combination of puzzle elements without special cases, element checks, or hardcoded couplings. If hundreds of diverse parts collide, mesh, or wire together, all interactions evolve through generic mathematical models.
- **Purely Homogeneous Tables & Generic Collider Extraction:** All rigid body integration (symplectic Euler), narrowphase manifold generation (Sphere, Box, Plane, solid Cylinder, Capsule later, SDF), constraint resolution (Box2D v3 TGS Soft), and multi-domain network passes (electrical supply routes now; optical ray marching and acoustic fields later) operate strictly on flat homogeneous capability tables. Collision shapes are extracted generically from the scene's `col_*` proxy nodes based solely on geometric properties (half-extents, radii, lengths) without element-specific branching or hardcoded shape tables.
- **Strictly Zero Identity Branching:** No physics compiler pass, solver kernel, or integration loop may branch, switch, or execute conditionals based on a part's identity, name, or purpose. When two colliders touch, their contact dynamics are governed 100% by universal geometric manifolds, Coulomb friction cones, and material restitution laws; the solver neither knows nor cares whether the collider belongs to a Tennis Ball, a Domino, or an authored custom fixture.

<a id="conversion-existing-elements-physics"></a>

### Conversion of Existing Elements & Zero Backwards Compatibility Authority

Every existing puzzle element is converted forward, one family per story in the [21 Oct 2026 delivery order](../_bmad-output/planning-artifacts/epics.md#delivery-order-coordinator-21-oct-2026-owner-authorised-re-examination):
- **Package and scene:** the declaration moves into the element's `elements/<Name>/` package and its art into its own `.tscn` scene with `col_*` proxies and `Loc_Port_<Id>` locators.
- **Generic instance:** per-element records and factories are replaced by the generic declared instance compiled from bundle declarations.
- **Delete in the same story:** each conversion deletes the records, tables and branches it supersedes; strictly zero compatibility shims, adapters, fallback loaders or migrations. Git history serves as the permanent archive.
- **Status:** the [isolation authority's Current status](architecture/element-package-isolation.md#current-status) lists each remaining violation with its owner.

---

<a id="numeric-representation"></a>
## Numeric Representation and Precision

All authoritative continuous simulation state, velocity integration, spatial coordinates, and constraint solving use standard **IEEE-754 single-precision floating-point (`f32`)**.

### Precision Invariants
- **Single-Precision Float (`f32`):** 24-bit mantissa ($\approx 7.2$ decimal digits) and 8-bit exponent ($\pm 10^{38}$). This eliminates catastrophic cancellation in rotational torque arms, prevents exponent overflow in kinetic energy calculations, and provides native alignment for 128-bit SIMD registers (`wasm_v128`).
- **Quantized World Domain:** Simulation coordinates span a standard puzzle volume $[-64, 64]\text{ m}$ per axis with sub-millimeter precision. Coordinate snapping during construction quantizes to canonical grid units ($0.1\text{ m}$ or $1/16\text{ m}$), but internal solver arithmetic operates in full continuous `f32`.
- **WASM SIMD128 Vectorization:** 4-wide single-precision float operations (`f32x4.add`, `f32x4.mul`, `f32x4.min`, `f32x4.max`) execute simultaneously in hardware registers, vectorizing broadphase slab tests, impulse updates, and bounding box evaluations.
- **No Hidden Precision Channels:** No double-precision (`f64`) shadow state or narrow 16-bit float truncation is permitted in the simulation loop. Game saves serialize exact 32-bit float bit patterns alongside typed integer configuration states.

<a id="precision-status"></a>
### Precision Status

No half-precision lane remains (Story 9A.44 part A, 24 Oct 2026). Declarations, construction values, compiled body, collider, material, sensor, guide, trigger, orientation and work-occurrence lanes, the physics ABI (layout tag `CTF4`), the WASM worker, motion pieces, the response/trace body wire, the FlatBuffers save (nine f32 `AssistanceKnotRecord` structs per puzzle) and the client's Animation channel read all carry IEEE-754 f32 through named offsets, with no narrowing. `HalfPrecisionGuardTests` refuses any half-precision type, codec, lane or hand-written decoder in code, tools, tests, schemas and shaders; `WorkshopAbiLiteralScanTests` refuses a literal field offset in every world record. A save written before Story 9A.44 with a puzzle record keeps its old knots only in the deprecated `raw_assistance` slot, so its decode is refused as `MissingField` with the explanation that a lesson saved before f32 assistance cannot be loaded, before anything is submitted: the live simulation keeps its committed state and goes on advancing; Free saves were already f32 and load unchanged. Integers, enums and identity tags keep their types. Remaining forward work: `double` game-value arithmetic in presentation sampling, pose history and clock derivation, the integer-tick clock and a `double` guard are Story 9A.44 part B, not yet delivered.

<a id="exact-f32-consequences"></a>
**Exact-f32 envelope consequences (Story 9A.44).** Values that half precision used to round are now exactly their f32 values: gravity 9.81 m/s² (was 9.8125), ball radius 0.34 m (was 0.340088), declared linear drag 0.04 1/s (was 0.040009), Balanced precision 0.45 (was 0.449951) and receiver capture speed 2.325 m/s (was 2.32421875); the Balanced maximum position corrections are the authored 0.12 and 0.08 m (the difficulty blocks had written them as 0.120000005 and 0.080000006). Principal inertia is its f32 closed form within $6\cdot2^{-24}$ relative (was $6\cdot2^{-11}$, Story 10.1), admitted in $[2^{-32}, 2^{19})$ kg·m². Physical event phases resolve $2^{-24}$ relative within the kept range $[-2048, 2048)$. A receiver's upper capture bound is its Y plus margin with a single f32 rounding. The orientation threshold is an explicit 4°–179° degree range (Domino 45°), no longer a rounding artefact. The bundle gate compares each package lane with its runtime lane for exact equality (9A.4). Against the half-precision baseline (f1ad3bd1), every authored lesson at precisions 0, 0.45 and 1 keeps its goal result and event order, with every event and goal within 12 ticks (0.1 s) of the baseline (observed −4…+9), and every element placed alone keeps its event order and ends within 5.1 mm of its baseline pose (observed 1.12 mm) (`F32BehaviourIdentityTests`). The pose bound is derived: over 240 ticks a fall moves by at most ½·|Δg|·t² = 5.0 mm, plus the 0.09 mm radius change. The timing bound is declared, not derived: to first order the deltas (all ≤ 3.4e-4 relative) shift a gravity-driven arrival by at most ½·3.4e-4·T, 0.2 tick over a 1200-tick lesson run, but contact sequences amplify that by an amount no input delta bounds.

**Pipe owner decision (10 Oct 2026):** Story 7.4 removes the twelve parked straight-Pipe files, including their half-precision dimensions/profile/resource lanes. Story 6.6 rebuilds Pipe fresh as canonical f32 declaration data on the generic WASM SIMD physics engine; no legacy adapter, alias or migration is retained. Complete CAT-048 acceptance and Story 6.7 rim/collar controls remain required in roadmap order.

**Current pose/velocity format:** `LocalPosition` and `CanonicalRotation` now store IEEE-754 f32 through construction, compilation, worker state, response, motion and presentation. Normalize authored/computed quaternions at the canonical boundary; external decode validates finite unit rotations and preserves their bits. Collider, sensor, guide and principal-frame rotations use that same type. No second pose reader remains. Mass, material, dimensions, frame translations and motion acceleration lanes are f32 on the same path (Story 9A.44).

Current record widths/offsets (layout tag `CTF4`): physics body160 (local32, rotation48, velocity64, angular76, mass88, drag92, gravity96, centre of mass108, principal frame120, principal inertia136); response/trace body96 (local20, rotation32, centre of mass48, velocity60, angular72); motion piece160 (three f32 phases16–28, u32 frequency28, cell40, remainder52, drag64, rotation68, velocity84, angular96); collider96, material32, guide80, work occurrence40 with f32 lanes throughout. Canonical construction body80 stores local56 and zero construction velocity68; instance160 stores quaternion96 and moves kind-specific fields to112. Committed speed limits remain64m/s and128rad/s. The canonical shared f32 pose ring retains its existing64-byte record; only its input decoding changes.

Formats carry no version numbers (owner, 21 Oct 2026): input that fails structural validation rejects atomically, with no automatic migration or compatibility reader. Since Story 9A.1 none remain: the canonical body record, the physics state header, the trace record and the clock, memory, schedule and animation channel headers keep their layouts, and the bytes where a version stood are zero padding that rejects when set. Sensor128 has immutable prefix84 and f32 residence state84–108; Guide80 stores its pose16–44, bounds44–68, maximum acceleration68 and support72–80. Named worker offsets and changed padding/mapping rejection controls must agree with the C# ABI owners. The trace/canonical formats supersede the earlier Story6.1c/6.1d byte layouts; those delivery receipts remain historical evidence.

---

<a id="capability-inventory"></a>
## Generic Capability Inventory

Every puzzle element is an instance of one or more shared physical capabilities declared in its element package and compiled into flat data tables. The simulation worker and compilation pipelines process these tables homogeneously with zero catalogue-specific kernel branches:

| Capability | Declared Data Content | Execution Behavior |
| :--- | :--- | :--- |
| **Rigid Body** | Mass, 3x3 inertia tensor, world pose (position vector, orientation quaternion), linear and angular velocities, linear drag coefficient, motion type (Dynamic, Static, Kinematic), island index. | Integrated via symplectic Euler at 480 Hz; declared linear drag decays the linear velocity exactly ($e^{-c\,\Delta t}$) after gravity each sub-step, and no angular drag exists; orientation quaternion normalized every sub-step. Scalable to hundreds of dynamic bodies. |
| **Colliders & Shapes** | Shape discriminant, a closed enum (Sphere, Box, Plane, Cylinder now; Capsule and Hollow SDF later), local frame transform, half-extents/radii (a cylinder: radius lane $r$, half-width $h = w/2$ in the Y lane, X and Z canonical zero), participation filter masks. Ingest refuses an undefined discriminant as a typed `InvalidDeclaration` before any state is stepped. | Tested in narrowphase via branchless analytic manifold generators and signed distance fields. |
| **Materials** | Restitution coefficient, bounce threshold velocity, static and dynamic friction coefficients, rolling-resistance coefficient $C_{rr}$ (balls declare it; boxes, planes and walls declare zero). | Pair laws: geometric-mean friction, product restitution, maximum bounce threshold; at every contact (static or dynamic partner) the larger $C_{rr}$ times the larger rolling radius (a sphere's radius, or a cylinder's radius when the manifold rolls on its curved side) bounds one rolling-resistance row. |
| **Constraints & Joints** | Connected body pair, local anchor offsets, constraint axes, linear/angular limits, Box2D v3 soft parameters (natural frequency $\omega$, damping ratio $\zeta$). | Solved via Temporal Gauss-Seidel Soft Step (TGS Soft) with 4-wide SIMD vectorization. Unifies hinges, sliders, distance links, and ropes. |
| **Force Regions** | Spatial bounds (box, cylinder, cone), bounded acceleration vector or field equation (gravity, directional guide, fan airflow, fluid buoyancy). | Applied during sub-step force accumulation; acceleration clamped to $\le 64\text{ m/s}^2$. |
| **Sensors** | Spatial trigger volume, target filter mask, sensor kind (Aperture Crossing, Convex Residence), required dwell duration, velocity threshold. | Evaluated continuously: aperture crossings use exact sub-tick planar root-finding; residence sensors accumulate dwell over sub-steps. |
| **Contact Triggers** | Target collision pair, approach velocity threshold, rearm condition. | First qualifying impact per tick generates a typed event occurrence; triggers mechanical or electrical transitions. |
| **Orientation Sensors** | Sensed dynamic body, admitted initial orientation, threshold as the cosine of half the angle (Domino: 45°). | Evaluated at every sub-step endpoint: $|\langle q, q_0\rangle| \le \cos(\theta/2)$ fires one typed occurrence; the fired state is sticky within the world and only a fresh admission (Reset) rearms it. |
| **Finite Work Stores** | Stored potential energy capacity, discharge rate limit, recharge coupling. | Thermodynamic power debiting: total power extracted by connected loads cannot exceed available stored power ($\Delta K \le E_{\text{store}}$). |
| **Network Nodes** | Terminal IDs, node potential, branch conductance, transmission latency, spectral optical band, acoustic frequency. | Evaluated in staged domain sub-passes: compiled supply routes over the reachability graph for electrical supply today; Modified Nodal Analysis, ray marching for optics and acoustic field culling are named later capabilities added by the first element story that needs each. Compiled from declared port connections without hardcoded wiring tables. |

---

<a id="solver-model"></a>
## Solver Model and Scalable Mechanics

The solver advances physical time at a fixed **120 Hz tick with 480 Hz sub-stepping** (exactly 4 sub-steps per tick; each sub-step is rational $h = 1/480\text{ s} \approx 2.0833\text{ ms}$).

```
      Single Physics Tick Pipeline (120 Hz)
      =====================================
      [Phase 1: Electrical Supply Routes & Logic Network]
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
- **Branchless Analytic Primitives:** Sphere-Sphere, Sphere-Box, Box-Box (15-axis SAT with reference-face clipping), Sphere-Plane, Box-Plane (up to four deepest vertices) and the solid Cylinder pairs below are computed via closed-form geometry without iterative GJK/EPA/MPR loops. The cylinder's curved rims add fixed-count refinements of closed-form signed distances, described below. Capsule follows in its own story.
- **Solid Cylinder (Story 10.1):** axis local +Y, radius $r$, half-width $h$; its exact support projection on a unit $u$ is $h|u\cdot a| + r\sqrt{1-(u\cdot a)^2}$, which also gives its exact rotated AABB (fattened for spin by $\sqrt{r^2+h^2}$). Inertia compiles on the canonical path: axial $m r^2/2$, transverse $m(3r^2+w^2)/12$. A contact normal within the cap tolerance of the axis ($|n\cdot a| \ge 0.98$) meets a cap; within the same angle of the axis-perpendicular plane ($|n\cdot a| \le \sqrt{1-0.98^2}$) it meets the curved side along a generator line; otherwise one rim point.
  - *Cylinder–Plane:* four points of the facing cap starting at its true deepest rim point (the radial direction of $-n$) and at 90° steps from it (upright, from U0), else the deepest rim point of each cap within the margin (two along the side, one tilted). The cap manifold therefore always contains the true lowest point and continues into the rim regime.
  - *Cylinder–Sphere:* the exact closest point of the solid (axial and radial coordinates clamped); a centre inside exits by the shallower feature, side or cap. One point.
  - *Cylinder–Box and Cylinder–Cylinder — gap and normal:* a SAT over the face normals and axes, axis × edge or axis, the closest-point direction of the two axis segments and the radial directions from an axis to box vertices or cap centres (exact support projections) only proves separation beyond the margin early. The gap and normal come from the **exact closest features**:
    - a box face, or a cap, facing the partner whose support point lies over it;
    - the two sides through the axes' common perpendicular (or the middle of a parallel overlap);
    - the minimum of the cylinder's closed-form signed distance along every box edge (convex along a segment: 24 golden-section steps) and along every rim of one cylinder against the other solid (16 fixed angular samples, then 16 golden-section steps in the best bracket).

    Every count is fixed: there is no simplex, convergence test or data-dependent iteration, so this is not GJK/EPA/MPR. Each golden-section search makes 2 initial evaluations plus 1 per step, so the per-pair cost is a bounded constant:
    - **cylinder–box:** 12 edges × 26 = 312 closed-form signed-distance evaluations, plus 12 closest-point evaluations and up to 6 face checks;
    - **cylinder–cylinder:** 4 rims × 34 (16 samples + 2 + 16 steps) = 136 signed-distance evaluations, plus 4 closest-point evaluations, up to 4 cap checks and 1 side–side check.

    The counts are fixed constants (`SegmentSteps` 24, `RimSamples` 16, `RimSteps` 16) with no convergence test. Ratified by the coordinator, 23 Oct 2026, as compatible with the "no iterative GJK/EPA" rule. Separated within the margin, the gap is the exact distance (proved against an independent closest-point oracle to 0.5 mm).
    - **Shallow cylinder–box overlap (Story 10.1 review F4, up to 1 mm; cylinder–cylinder has no core, review N9):** the same measures run between the cylinder's core and the box, the core shrunk by a margin of 1 mm on every face (at most half the radius or half-width). The gap is the core distance less the margins, never positive: the exact shapes overlap, so the core's rim rounding (at most 0.41 of the margin, along the rim's diagonal) cannot report them apart. The normal is the cores' closest-feature direction. This is exact on caps, sides and faces. The exact shapes' deepest point would exit by the cap or the side, flipping between them round a rim tick by tick; that flipping lost warm starts and pumped energy into a box resting on a rim. Each such evaluation doubles the measure cost above.
    - **Deeper overlap:** the shallower of the SAT depth and the deepest closest-feature measure.
    - **Depth clamp:** no manifold point claims more depth than its pair (N5).
  - *Cylinder–Box and Cylinder–Cylinder — manifolds by $n\cdot a$:* a cap meets a box face or a cap as their common region (eight fixed rim samples plus the rim point deepest toward the partner, partner vertices inside, rim crossings, reduced to four); a side meets an aligned face as the clipped generator.
    - **Side emission rule (Story 10.1 review F4, round 4):** a box edge within the cap tolerance of parallel emits the edge's closest point to the axis and both ends of their common stretch; two crossed or skewed cylinder sides emit their closest point and each facing-generator end that lies within the speculative margin against the other side. Every such point carries its own radial signed gap and its own radial normal, so a skewed side's far end is a speculative row before it lands. Parallel cylinder sides (within 2°) keep the two stretch ends, each at its own side distance.
    - **Reduction to four:** a side manifold with more than four points keeps the deepest point (first on a tie), the two ends of the farthest-apart pair, then the deepest of the rest, in emitted order.
    - **Closest point kept:** on the exact path every cylinder–box and cylinder–cylinder manifold holds the pair's closest point; when no point lies within 0.1 mm of the pair's gap, the closest features' midpoint joins at that gap (and the reduction applies).
    - **Radial directions:** re-orthogonalised once against the axis; a radial part shorter than 1e-3 of its vector takes the reference direction. An integrated orientation is not exactly unit, and on a turned-over body the axis's length error leaves an axial residue that rivals a micro-radian radial part (a cap tilted 2e-6 rad once read a −24.8 mm bench gap). Another side counts only when its axis is parallel within 2°. Each end of a stretch is measured by its own distance to the partner's curved side. Skewed further, two sides touch at one point: a two-point stretch there put the rows at a rolling cylinder's rim ends and pumped energy into it (review F4). A crossing edge or side meets at one point; a cap on a side gives the generator's chord across the disc; a tilted rim gives one point midway between the exact closest points. A clip that yields nothing falls back to one point between the closest features, so a speculative contact always exists within the margin.
  - Feature ids name the cylinder feature a point came from (cap, side, rim or a numbered sample), so a warm start carries over while that feature persists. Warm-start stability while rolling is not separately qualified.
- **One Constraint Row for Every Pair:** each manifold point becomes the same TGS Soft row (lever arms about the centre of mass, world inverse inertia from the compiled principal moments, two-axis Coulomb friction with a circular cone, warm start keyed by collider pair and feature, speculative margin, dissipative restitution). Pair laws are geometric-mean friction, product restitution and maximum bounce threshold from the declared materials; no shape or element owns a different response. A rolling support is no material point: a sphere's contact stays under its centre, and a curved-side cylinder contact is the generator facing the partner at the anchor's axial coordinate, so within a substep its support moves with that material axis point plus $r$ times the turn of the facing radial direction about the moved axis. Rolling about the axis leaves that direction unchanged; tumbling turns it, so no energy is gained when a lying cylinder tumbles end over end. Cap, rim and every other contact follow the material anchor. Every manifold with a sphere, and every manifold with a curved-side cylinder contact (arm $r$, coordinator decision under owner delegation, Story 10.1, extending the every-contact rule), adds one two-axis rolling-resistance row (Box2D v3 coefficient mixing, not warm-started: its impulse restarts from zero every sub-step), solved after the normal rows of each sweep and applied equal and opposite to both bodies whether the partner is static or dynamic: it drives the relative rolling about the tangent axes toward zero within the circular bound $C_{rr}\,r\sum\lambda_N$, so it never reverses a ball on a static support (against a dynamic anisotropic box the per-axis diagonal effective masses are an approximation); spin about the contact normal is not resisted. A struck body (for example a Domino a ball rolls over) receives the reaction, so its outcome includes the ball's resistance (owner decision 9 Oct 2026).
- **Hollow SDFs:** Pipes, chutes, and funnels evaluate signed distance fields with toroidal rim caps, eliminating snagging and internal face artifacts.
- **4-Point Manifold Reduction:** Contact polygons from face clipping are reduced to the 4 area-maximizing extreme points (deepest point, furthest point, and two points maximizing triangle/quad area).
- **Feature ID Warm-Starting:** Each contact point caches a 32-bit packed `FeatureId`. Matching features across sub-steps inherit accumulated normal and friction impulses, reducing solver convergence sweeps by over 50%.
- **Speculative Contacts:** Fast-moving objects within predicted closing distance $d \le |\mathbf{v}_{\text{rel}}| h + \mathbf{s}_{\text{slop}}$ allocate speculative constraints that remove excess closing velocity, preventing tunneling without continuous swept root-finding. A sphere's speculative contact that will close within two substeps (the solve stops what this substep reaches, the relax pass what the next would) takes its normal at the predicted touch: its centre is advanced along the pair's relative linear velocity by conservative advancement (at most four probes of the same pair kernel), the contact keeps the distance still to close along that normal as its gap, and its point stays on the advanced sphere's centre line. Two moving spheres keep their generated contact. A speculative point that carries normal impulse in a substep is a contact for restitution, contact events and contact loads.
- **Restitution (engine restitution slice, 24 Oct 2026):** one pass per 480 Hz substep, after the relax sweeps and before the second velocity clamp; each substep is a complete Box2D v3 step (collide, prepare, solve, integrate, relax), so this is the faithful mapping of Box2D's once-per-step pass. A point bounces when it carried normal impulse in that substep, touching or speculative, and its approach exceeds the pair's bounce threshold strictly. The approach is the relative normal velocity at the point from the bodies' substep-start velocities (before guides, supplied drives, gravity and drag; Box2D prepares before integrating velocities). A manifold's bouncing points are driven together to $-e\,v_n$ of their own approaches in one regularised block solve (the normal block's coupling and regularisation), or one at a time with each accumulated impulse clamped $\ge 0$ when that solve is singular or would pull; no soft-contact or penetration bias enters the target. Each manifold that bounced is then given one more friction and rolling pass whose cones include the restitution impulse, so an impact keeps its Coulomb share of tangential impulse (a deviation from Box2D's pass, which runs after several frictional substeps). There is no cross-substep approach memory: an impact is resolved in the substep that stops it. The pass is bounded by energy: the dynamic bodies joined by that substep's bouncing manifolds form groups, and a group's kinetic energy after the pass may not exceed the larger of its kinetic energy before the contact solve (after gravity and drag) and after the relax sweeps; a group over the bound has its restitution change scaled back along the line from its relaxed velocities. Independent Newton targets on a body shared by several impacts (a box landing on a box that bounces on the bench in the same substep) would otherwise return more than the impacts removed (reviewer F1, e ≥ 0.95). Known limitation: restitution is solved per manifold, not simultaneously over an island, so an impact through a resting dynamic body (a ball onto a box resting on the bench) loses bounce, because the support contact absorbs the push without returning it (deferred: "Engine: simultaneous/supported-impact restitution", to be checked before any dynamic plank, crate or seesaw element). Contact events report that approach and contact loads count that point, in the substep the impact is resolved.

### 3. Constraint Solver: Box2D v3 "TGS Soft" Formulation
Every contact, limit, and joint is modeled as an implicit damped spring-damper constraint integrated directly into the effective constraint mass:
$$\beta = \frac{h \omega}{2 \zeta + h \omega}, \quad \gamma = \frac{1}{h m_{\text{eff}} (2 \zeta \omega + h \omega^2)}$$
$$\mathbf{M}_{\text{eff}} = \left( J M^{-1} J^T + \gamma \right)^{-1}$$
$$\Delta \lambda = -\mathbf{M}_{\text{eff}} \left( J v + \frac{\beta}{h} C + \gamma \lambda_{\text{total}} \right)$$
- Compliance $\gamma$ and softness bias $\beta$ regularize constraint corrections; they do not by themselves guarantee energy conservation or dissipation.
- **Numerical envelope:** Verify contacts, limits and joints against the game-grade error and energy envelope, including external work. Residuals clamp or continue; actual behavior, not the softness formula alone, establishes acceptance.
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
  $$\beta = \frac{h \omega}{2 \zeta + h \omega}, \quad \gamma = \frac{1}{h m_{\text{eff}} (2 \zeta \omega + h \omega^2)}$$
- **Unified Effective Mass:**
  $$\mathbf{M}_{\text{eff}} = \left( J M^{-1} J^T + \gamma \right)^{-1}$$
- **Bounded numerical behavior:** Substeps and compliance improve constraint conditioning but do not guarantee stability or zero energy gain for every coupled scene. Verify physical time scale, damping, contacts and stops within the game-grade envelope; retain the declared velocity bounds and residual handling.
- **Declared supplied drive (C-Gate, 22 Oct 2026):** a slider may declare a `PrismaticDriveDescriptor` (target, maximum speed, stopping acceleration, maximum effort, maximum power), enabled while its element's compiled PowerIn route is closed. It compiles to a separate `PrismaticDriveDeclaration` row (the prismatic record's immutable `DriveKind` and drive lanes). On every substep the one generic law reads the joint's own axial row and applies, along it, the force $F = m\,\Delta v/h + k\,x - m\,g_{\text{axis}}$, where $\Delta v$ moves toward $\operatorname{sign}(d)\min(v_{\max}, \sqrt{2 a |d|}, |d|/h)$ by at most $a h$ (the $|d|/h$ cap, Story 9A.44, is the speed that closes the remaining distance in one substep, so the hold at the target does not chatter). $F$ is clamped to $\pm F_{\max}$, then to $|F v| \le P_{\max}$ at the current axial speed. A route may drive only a slider that declares a drive. Each committed tick also writes the prismatic's moving-contact lane: whether its moving body took a positive normal contact impulse from a body other than its base.

### 7. Island Partitioning and Deactivation (Sleeping System)
- **Disjoint Set Union (DSU):** Fast Union-Find with path compression partitions connected active bodies into independent simulation islands.
- **Sleeping State Machine:** Islands where all bodies exhibit linear velocity $\|\mathbf{v}\| < 0.01\text{ m/s}$ and angular velocity $\|\boldsymbol{\omega}\| < 0.02\text{ rad/s}$ for $>0.5\text{ s}$ enter sleep mode.
- **Zero-Cost Sleep:** Sleeping islands bypass broadphase, narrowphase, and constraint solving entirely (0 ms CPU cost).
- **Instant Wakeup:** Any external impulse, user manipulation, or contact from an active dynamic body immediately wakes the entire island.

### 8. Zero-Allocation `TempAllocator` & Scene-Sized Buffers
- **No Fixed Capacities:** Every physics, worker and wire table is sized from the compiled scene at Run; memory is the only bound (owner, 20 Oct 2026; Story 9R.5b).
- **No Dynamic Heap Allocation:** The 120 Hz / 480 Hz simulation loop executes with zero dynamic allocations (`new` or GC garbage generation) once its tables are sized at Run.
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
| **Normal Velocity Residual** | Normal approach velocity clamped to $\ge 0$ following impulse sweeps. | Clamped; an approach at or below the pair's bounce threshold (the larger declared threshold) is resting contact (zero bounce). |
| **Maximum Velocities** | Linear speed $\le 64\text{ m/s}$; angular speed $\le 128\text{ rad/s}$. | Every substep clamps twice to $(1-2^{-20})$ of each bound: before position integration, and again after the relax sweeps and restitution pass, which can add speed after the first clamp (a 100 kg sphere at 64 m/s would send a resting 1 kg sphere off at about 127 m/s; it commits just under 64 m/s). The $2^{-20}$ headroom covers f32 rounding of the committed components (at most $2^{-24}$ relative), so the committed squared magnitude, summed in double as the host checks it, stays within $64^2$ and $128^2$ in all 16 tested directions; without the headroom 6 of the 16 commit above each bound. A finite velocity of any size clamps (an f64 length overflow is measured at reduced scale); only a non-finite component drops the motion while the finite pose stands. The host rejects non-finite or larger committed values. Prevents numerical divergence. |
| **Committed Velocity Resolution** | Each tick commits linear and angular velocity as f32 (about $2^{-24}$ of the value). A per-tick decrement below half that step would round back to the previous value; the declared decelerations are orders of magnitude above it (drag 0.04 1/s removes $1.7\times10^{-4}$ of the speed per 240 Hz tick). | Declared linear drag alone slows a free-flying ball on every committed tick from low speed to the envelope bound: the balls' 0.04 1/s follows $2e^{-0.04t}$ within 1% at 60, 120 and 240 Hz, and $63.9e^{-0.04t}$ within 0.1% on every 240 Hz tick from 63.9 m/s (its relative decrement does not depend on speed). Declared rolling resistance decelerates a rolling ball on every committed tick up to 5 m/s: a Basketball rolling at 5 m/s at 240 Hz decelerates at $(5/7)(C_{rr}g + c\,v)$ (0.367 m/s² measured near 4.5 m/s), its speed and spin fall on every tick, and it rests after about 16 s; one struck to $\le 1\text{ m/s}$ rests at the same time within 10% at all three cadences. Faster rolling still decelerates at that rate over a second (20 m/s within 10%, spin never rising), but on a fast-spinning sphere the contact rows act intermittently, so single ticks skid with only drag acting; this is a solver limit, not precision (an f32 step at 20 m/s is $2\times10^{-6}$ m/s), recorded in deferred-work. No rest threshold zeroes velocity. |
| **Orientation Quaternion** | Normalization error $\|\mathbf{q}\| - 1 \le 10^{-4}$. | Renormalized every sub-step via fast inverse square root. |
| **Non-Finite Quantities** | NaN or $\pm\infty$ detected in candidate state. | Candidate discarded; body retains previous committed pose; simulation continues uninterrupted. |
| **Tunneling Threshold** | Zero penetration through solid barriers $\ge 1\text{ cm}$ thick at speeds $\le 64\text{ m/s}$. | Prevented by 480 Hz sub-stepping combined with speculative contact margins. |
| **Thermodynamic Energy** | No free energy: $\Delta K_{\text{system}} \le E_{\text{store}}$. The restitution pass's energy bound holds per 480 Hz substep (a group of bodies returns at most the kinetic energy its contacts removed in that substep). Per committed tick, contacts and restitution add at most $\sum m\,g\,s_{\text{slop}}$ over the bodies in contact (1.96 mJ per kg), qualified at every restitution $e \le 1$ for single bodies (sphere, box, cylinder) on static supports, a box landing on a resting dynamic box, and the Story 10.1 cylinder classes (`WorkshopPhysicsLawRestitutionTests`, `WorkshopPhysicsLawCylinderTests`); other multi-body configurations rely on the per-substep bound (measured: 3- and 4-box stacks hit from above at e = 1 rise 21–27 mJ in a tick against Σm·g·s_slop of 10.8–13.7 mJ, with genuine gain over the preceding ticks 2.1–7.3 mJ, within bound). | Total power demands exceeding available source power are scaled down proportionally ($\alpha = P_{\text{avail}} / P_{\text{demand}}$). |
| **Sensor Crossing** | Planar crossing timestamp precision $\le 0.1\text{ ms}$. | Continuous linear sub-tick interpolation between starting and ending poses. |

---

## Forward-Only Refactoring and Deletion Mandate

This architecture operates on a strict **forward-only refactoring policy**:
- **Zero Backwards-Compatibility Shims:** No compatibility wrappers, transitional adaptors, emulation shims, or parallel CPU/GPU execution modes are permitted.
- **Code Removal:** Superseded systems—including old WebGPU WGSL compute shaders (`physics.wgsl`), half-precision (`f16`) bitwise manipulation codecs, and Baumgarte position stabilization—are permanently removed from the active codebase upon completion of the forward implementation.
- **Preservation through Refactoring:** All 72 named puzzle elements, their catalog definitions, and the 150 progressively taught campaign levels remain fully preserved and are refactored directly to the new declarative capability formats.
