# General data-driven engine design

Required architecture for the whole game, not a claim that every model is implemented. It applies the [general-engine requirement](engine-contracts.md#general-data-driven-engines) under the [compilation model](gpu-f32-physics.md#compilation-model), [capability inventory](gpu-f32-physics.md#capability-inventory), [solver model](gpu-f32-physics.md#solver-model) and [game-grade envelope](gpu-f32-physics.md#game-grade-envelope). The [element map](planning/general-engine-element-map.md) keeps all 72 CAT, 216 EL, 37 TH, 22 RAD and 18 GAP identities and the 302 authored fixtures mapped to candidate capability compositions and owners; a mapping is traceability, not qualification.

Three states stay visible for every capability: **architectural route** (the records below can express it), **frozen model** (law, parameters, discrete semantics and Chrome-observable acceptance passed their design stage) and **qualified support** (implemented, proven in Chrome through real controls with Reset and Save/Load, legacy deleted). A table row establishes neither of the latter two. An unresolved route or model rejects admission at compile; it never authorises element-owned solver code.

## Tri-Graph Architecture: Three Systems, One Direction of Data

The engine is strictly partitioned into three independent graphs, each optimized for its execution domain:

1. **Logical Scene / Machine Graph (Main Thread Authoring):**
   The authoring model on the browser main thread. Represents placed puzzle elements, their hierarchical components, typed sockets, and physical/logical connections (wires, pipes, belts, chains, axles). Handles user interactions (drag, drop, rotate, connect), undo/redo, UI selection highlights, and serialization. It contains no solver state or GPU vertex layouts.
2. **Physics Graph (Dedicated WASM SIMD Worker):**
   Compiled from the Scene Graph at Run into flat, contiguous Structure-of-Arrays (SoA) SIMD-aligned memory tables. Represents dynamic rigid bodies (mass, inertia tensors, linear/angular velocities, transforms), colliders, Box2D v3 Temporal Gauss-Seidel (TGS) Soft constraints, force regions, finite energy stores, continuous sensors, and multi-domain network nodes (compiled electrical supply routes today; optical rays and acoustic/pneumatic fields as their stories land). All physics is simulated through a unified, generalised, data-driven engine on a dedicated WASM SIMD Web Worker advancing at a fixed 120 Hz tick with 480 Hz substeps using 128-bit WebAssembly SIMD (`wasm-simd128`). Strictly zero element-specific solvers, zero per-part update loops, and zero element-selected kernel branches exist in the physics engine. Broadphase collision detection uses a Dynamic AABB BVH with velocity fattening; island partitioning uses Disjoint Set Union (DSU); narrowphase collision detection uses generic analytic manifolds (Sphere, Box, Plane, Capsule, Compound Cylinder) and signed distance fields (SDF); and multi-domain physical fields execute through shared capability kernels. Scaled to simulate hundreds of concurrent puzzle elements with sub-millisecond execution times. Committed poses are written directly into a lock-free zero-copy triple-buffered ring in `SharedArrayBuffer` using atomic sequence counters, completely bypassing GPU readback stalls (`mapAsync`).
3. **Rendering Graph (Browser Main Thread Renderer):**
   Paced by the browser display refresh (30–60 FPS, with high-refresh display support up to 144 Hz). Assembles GPU-instanced draw batches (`gl.drawElementsInstanced` in WebGL 2, `renderPass.drawIndexed` in WebGPU) grouped by shared mesh geometry and material pipelines. Reads the latest committed transforms directly from the zero-copy `SharedArrayBuffer` pose ring, evaluates Slerp interpolation between bracketing substep snapshots, and composes cosmetic secondary animation offsets evaluated at 60 Hz by an independent Animation worker.

**One-Way Data Flow and Decoupled Animation:**
Data flows strictly one-way: physics → animation → renderer and physics → renderer; nothing flows back into physics. Rates are ordered: physics (120 Hz with 480 Hz substeps) > animation (60 Hz) ≥ renderer (30–60 FPS). Physics always updates faster than rendering, and rendering never influences physical results. All animation executes on an independent Animation Web Worker at 60 Hz, completely decoupled from physical truth. Animation is entirely data-driven and event-driven: cosmetic curves, squash-and-stretch, motor spin, indicator/UI easing, and particle effects evaluate declarative bindings to committed physical poses, committed level states and one-shot event-ring occurrences. Strictly zero per-element animation scripts or callbacks exist in the animation host.

<a id="zero-element-assumptions"></a>

## Zero Element Assumptions & Generic Combination Support

A foundational, non-negotiable architectural invariant of the engine is that it operates with **zero element assumptions and generic combination support**:

- **Zero Part Identity Assumptions:** The engine runtime makes strictly zero assumptions about which puzzle elements exist or have been loaded. There are no built-in "core parts" versus "extension parts", no assumptions that certain parts must co-exist, and zero hardcoded special cases.
- **Arbitrary Combination Support:** The engine must support ANY arbitrary combination of puzzle elements without special cases, element checks, or hardcoded couplings. Whether a player places fifty motors linked in series, chains ten fans directed at suspended bowling balls, or connects arbitrary electrical networks to sensors, all interactions are evaluated uniformly.
- **Homogeneous Mathematical Abstractions:** All physics simulation, multi-domain fields (supply routes, activation DAGs, joints), animation evaluation, and rendering batches operate strictly on generic mathematical abstractions and homogeneous capability tables.
- **Strictly Forbidden Identity Branching:** No subsystem—including the physics compiler, solver kernels, animation worker, or UI router—may branch, switch, or assume anything about a part's identity, class, or purpose. If two parts collide or connect, their interaction is governed 100% by universal physics manifolds, contact materials, and typed port contracts. The solver evaluates contact geometry and constraint rows without knowledge of part names or catalog kinds.

<a id="element-packages-and-bundle"></a>

## Element Packages and the Element Bundle

Every puzzle element is declared once, in its own `elements/<Name>/` package, under the [sole design authority](architecture/element-package-isolation.md):

- **One declaration source:** `tools/BundleElements` discovers the packages at build time and writes one bundle; the runtime loads only that bundle ([bundle loading](engine-contracts.md#element-bundle-loading-contract)). There is no second catalogue, no per-element C# table and no named element constant; the isolation authority's Current status lists what the tree still has to delete.
- **Typed schema without versions:** each entry carries typed metadata, an enum category, a typed parameter schema (key, unit, enum kind, f32 range and default or enum options), f32 vectors `[x, y, z]` and quaternions `[x, y, z, w]`, ports, colliders, and mechanical, electrical and signal payloads ([bundle schema](engine-contracts.md#element-bundle-schema-contract)). The bundle has one schema and no version field; structural validation rejects bad input.
- **Extensible typed identity:** elements are identified by the typed id the bundle issues, never by a closed enum. Saves and messages carry that id.
- **Zero switch statements for instantiation and configuration:** generic `MachinePart` entities and the generic declared instance are built from bundle entries; parameters are edited by the generic editor from the declared parameter schema.
- **Free palette:** the Free Workshop palette lists every bundle element, grouped by declared category, without a static inventory list.

<a id="declarative-ports-and-mesh-anchors"></a>

### Declarative Port Definitions and Mesh-Anchored Behavior Bindings

Connection sockets and ports are fully decoupled from hardcoded C# coordinate lookup tables and closed enum switches:

- **Declarative ports in the package:** Each element package declares the complete collection of ports it provides and consumes:
  - **Exposed (Output) Ports:** Ports through which the part emits signals, power, or mechanical/fluid flux (e.g. electrical supply output, impact trigger pulse).
  - **Accepted (Input) Ports:** Ports through which the part receives signals or energy (e.g. electrical power input, activation trigger).
  - **Domain / Type:** Strongly-typed connection domain (e.g. `Electrical`, `Activation`, `Mechanical`, `Pneumatic`, `Optical`).
- **Mesh-Anchored Port Attachment via Named Nodes (`Loc_Port_<Id>`):** Rather than relying on hardcoded C# local-coordinate vectors (`Vector3`), port anchors in 3D space are referenced by name to target locator nodes in the part's rendering scene hierarchy (convention `Loc_Port_<Id>`). At runtime, the connection UI and visual wiring systems resolve the 3D attachment point directly by querying the node transform in the scene graph (e.g. `Visual.FindChild(port.TargetNode)`). Moving or adjusting the 3D mesh automatically updates wire endpoints without code edits or hardcoded coordinate updates.
- **Port Behavior Binding System:** Parts bind gameplay and presentation behaviors directly to these declared ports:
  - **Visual & Cosmetic Bindings:** Dynamic visual responses driven by port state (e.g. signal lamp bulb albedo and emission when its accepted `ActivationIn` port triggers; switch button depression when activated; indicator lamp transitions).
  - **Physical & Multi-Domain Field Bindings:** Solver-level coupling derived from declared ports at compile-to-state time (e.g. compiling a supply route when an accepted power port is wired to an exposed battery port; scheduling an activation event in the discrete event queue; triggering contact impulse drives).

<a id="declarative-part-definition-system"></a>

### Unified Declarative Part Definition System

Every contraption element in the game is defined purely through the declaration in its `elements/<Name>/` package, written into the element bundle, drawing from battle-tested industry architectures (Kerbal Space Program `Part` / `PartModule`, Scrap Mechanic shape/joint data, Besiege block configurations, and Unreal Engine DataAssets / Gameplay Ability System):

- **Zero Per-Part C# Classes or Update Loops:** A puzzle element is never a unique C# class inheriting from a base class with virtual `Update()` methods. An element is a data declaration specifying the reusable capabilities it instantiates, its geometric colliders, connection ports, behavior event-action bindings, art assets, and cosmetic curves.
- **Declaration contents:** The package declaration supplies everything the [bundle schema contract](engine-contracts.md#element-bundle-schema-contract) lists: metadata and enum category; the typed parameter schema; colliders (shape kind, f32 offset `[x, y, z]`, rotation `[x, y, z, w]`, dimensions, material); typed ports with their `Loc_Port_<Id>` locators; the generic capabilities it instantiates (rigid body, constraint, force region, sensor, contact trigger, orientation sensor, finite work store, network node) with their mechanical, electrical and signal payloads; the capability-named event kinds it can produce; and its presentation bindings (target node, property, curve and driving source: a committed level state or a one-shot ring occurrence).
- **No executable data:** declarations carry values and references only, never callbacks, expressions or an identity that selects an equation.

<a id="locator-anchored-ports"></a>

### Locator-Anchored Port Pattern (`Loc_Port_<Id>`)

Connection sockets and wiring pins have one source, the element's declared ports (Story 9A.17):
- **Declared ports:** each package declares its `PortDescriptor` list: the typed socket (`PortSocket`), domain, direction, signal kind and f32 anchor position in the element's local frame. The bundle carries the list, and `ElementPortCatalogue` admits it at load (the Workshop after `PartRegistry.Load`, the Simulation worker at bootstrap from the bundle it is built with). It refuses an unknown socket name, an unsupported domain or direction, a socket declared with another socket's domain or direction, a misplaced signal kind or a non-finite anchor, each with a typed `ElementPortRejection`. Connection validation, the activation compiler, the connection drawer (in declaration order) and the wire artwork's endpoints read only these ports; no engine or UI table restates them.
- **Artist-authored locators:** an element's `.tscn` scene may keep an empty `Loc_Port_<PortSocket>` node at each socket, named by the typed socket exactly (`Loc_Port_PowerIn`, `Loc_Port_ActivationOut`), as a child of the scene root. The bundle build fails when a locator names no declared socket, when a declared port has no locator, or when a locator is not at its declared anchor (`BUNDLE011`–`BUNDLE013`). The only other locators are the tube mouths (`Loc_Port_mouth_start`, `Loc_Port_mouth_end`, Story 9R.6). Nothing at runtime looks a socket up in the scene.
- **Articulated sockets:** a socket that moves with a secondary body (a spinning shaft, a pivoting arm) is not declared yet; the first element story that needs one declares it relative to its moving body.

<a id="event-to-action-behavior-pipeline"></a>

### Event-to-Action Behavior Binding Pipeline

Parts handle physical interactions and state transitions without per-element C# scripts or update callbacks:
- **Physical Trigger Dispatch:** Continuous physics sensors, contact triggers, orientation threshold checks ($|\langle q, q_0\rangle| \le \cos(\theta/2)$), and timer deadlines generate typed discrete occurrences on the simulation worker.
- **Declarative Action Execution:** The runtime behavior engine evaluates the part's `BehaviorBindingDefinition` entries against these occurrences. Qualifying triggers dispatch port signals to connected wires, toggle drive states, latch contact switches, or debit finite energy stores without custom per-part logic.

<a id="element-scene-pipeline"></a>

### Element Scene & Declarative Part Pipeline

All 3D puzzle elements, fixtures, and tools are authored as the element's own Godot `.tscn` scene inside its package (owner decision, 18 Oct 2026). Procedural mesh generation and hardcoded collider dimension tables in code are forbidden.

1. **Standard Node Structure of an Element Scene:**
   Every puzzle element scene follows a standardized declarative node hierarchy:
   - `Visual/` (or visual mesh nodes): Contains the renderable geometry with standard PBR metallic-roughness materials adhering to the project palette and sculptural style defined in [DESIGN.md](../DESIGN.md).
   - `Collision/` (or collision nodes): Contains non-rendered analytic proxy primitives and convex hulls used for physics:
     - `col_box` (or `*-col_box`, `-col` box): Analytic box half-extents.
     - `col_sphere` (or `*-col_sphere`): Analytic sphere radius.
     - `col_capsule` (or `*-col_capsule`): Analytic capsule radius and half-height.
     - `col_cylinder` (or `*-col_cylinder`): Analytic cylinder radius and height.
     - `col_hull`: Convex polyhedral hull meshes.
   - `Locators/` (or locator nodes): Empty nodes named `Loc_Port_<Id>` specifying 3D socket attachment points for electrical, activation, and mechanical connections.
   - **Secondary Body Sub-Trees:** For composite or articulated parts (e.g. rotating motor shafts, springboard moving plates, lever arms), moving sub-components are organized under dedicated child node branches. Each secondary body branch contains its respective visual geometry, collision nodes (`col_*`), and locators, sharing local transforms dynamically during simulation.
2. **Package Binding to the Scene:**
   The package declaration names its scene path. The declaration owns generic physical capabilities, port contracts and bindings; the scene owns the visual and collision geometry.
3. **Generic Collider Extraction without Identity Branching:**
   Collider extraction parses the dedicated collision proxy nodes (`col_*`), computes their local transforms relative to the parent body origin, and produces generic `ColliderDeclaration` records without branching on element kind. Under Story 9A.16 extraction runs at build time and its output is baked into the bundle, which is then the compiler's only collider source.

<a id="optimization-options"></a>

## Preserve optimization options without tuning ahead

Implement the current interaction straightforwardly in the intended C#/WASM workers, SIMD128 physics and Chrome presentation. Keep these inexpensive boundaries in the affected code; they require no new infrastructure or engine-wide redesign:

- Semantic IDs, canonical data and observable ordering stay separate from private indices, buffer packing and dispatch layout. Shared capabilities are selected by typed declaration, never by catalogue identity. Public contracts must not require one allocation, dispatch, readback or message per element.
- Workers retain exclusive ownership and bounded asynchronous queues/buffers. Physical time follows the physics clock, independent of rendering cadence; animation cannot become a physics scheduler. Do not bake the development device's speed, workgroup choice or display rate into gameplay or admission semantics.
- Reuse compiled static topology while its declared revision and inputs remain valid; invalidate on relevant changes. Resource creation, reuse and disposal have explicit owners; no externally retained view outlives its storage.
- Separate correctness contracts (laws, canonical values, ordering, atomicity, observable results) from execution strategy (packing, batching, scheduling). Future tuning must pass those contracts; preserving the option does not justify implementing an optimization now.

Release-only numbers (renderer FPS, tick p95, memory after cycles, fault injection, device matrix) belong to the release checklist at the [stage gates](delivery-workflow.md#stage-gates) (P0-034), not to per-slice gates.

## Grow capabilities through roadmap slices

The [rolling playable roadmap](planning/invest/vertical-delivery.md#rolling-playable-roadmap) is the order: ENGINE-CORE-1, ENGINE-CORE-2, CAT-023 Domino, CAT-014, CAT-015, CAT-062, CAT-002, CAT-048, ANIM-1, LEGACY-0, ELEMENT-n, CAMPAIGN. Each slice is one player-visible outcome, adds at most one generic capability or one element by declaration, names the legacy it deletes and is done when a player can observe the acceptance in Chrome through Playwright with exact Reset and Save/Load. [TODO](../TODO.md) holds the live slice and blocker.

A catalogue composition describes possible needs; the admitted implementation and its Chrome proof determine what can run. Add a missing shared capability only with its consuming outcome, including animation/presentation bindings and restoration, never as a physics-only feature with feedback postponed. For example, detector passage (CAT-002) adds the generic aperture sensor with directional rearm; Delay adds a shared deadline state machine; `delayed_signal` adds a generic occurrence-time goal. Continuous physical predicates are WASM SIMD; integer scheduling and Boolean/event transitions are C#. A new timer cannot become a part-local update loop and an animated countdown cannot become time authority. Do not first build a universal graph editor, expression language or all-domain kernel library; unsupported compositions reject atomically. All refactoring is forward-only: zero backwards compatibility shims, helpers, or bridges are allowed.

## Declaration data per element

An element is defined only by its visual, physics and animation behaviours: which generic capabilities it instantiates, their parameters, its art and its animation bindings. Catalogue assemblies contain values and references; they cannot carry executable expressions, delegates, per-part callbacks or an identity that selects an equation. Closed alternatives are enums; extensible definitions and instances use typed stable IDs, specifically the typed element id the element bundle issues. The compiled plan contains capability/shape/material kinds and instance handles, not a switch over Basketball, Receiver or any other catalogue name. P0-004/P0-008/P0-016 own the exact admitted fields, offsets, enum mappings and quotas.

| Record family | Declared contents | Execution owner |
| --- | --- | --- |
| Assembly definition and construction instance | Definition identity, stable member keys, initial transforms, canonical IEEE-754 f32 parameters with units/scales, component membership, exposed typed ports, inventory and supported modes | C# construction compiler (Scene Graph); no physical evolution |
| Body, collider and material | Motion kind, mass/inertia tensor, pose/velocity, sphere/box/plane/hollow SDF geometry with feature identities, material coefficients, participation flags and bounds | Shared WASM SIMD body SoA, Dynamic AABB BVH, and shape-pair contact table |
| Constraint, drive and transfer port | Body/frame endpoints, allowed degrees of freedom, axes/limits, stiffness/damping, bounded effort, source/store IDs and work-conjugate couplings | Shared Box2D v3 TGS Soft constraint rows and domain networks |
| Force region | Static-frame region, declared bounded acceleration law (gravity, guide, fan/jet, buoyancy), admitted targets | Shared WASM SIMD force-region evaluator |
| Material inventory and field | Species/mass, energy/enthalpy, phase/constitutive data, thermal/fluid/electrical/optical/acoustic/radiation ports, geometry and explicit environment boundaries | Shared multi-domain network solvers |
| Sensor and contact trigger | Typed observable kind (residence, aperture, impact, orientation threshold from the admitted pose), target bodies/regions/frames, channel filters, threshold/hysteresis, dwell in ticks, capacity | WASM predicates sampled at substep endpoints; committed typed outputs |
| Controller definition | Finite enum states, typed Boolean/enum/event tests, integer tick/deadline operations, transition priority and typed actuator intents | Reusable C# discrete state machine; no physical formula |
| Goal definition | Typed quantity/rate/window/order/protected-state predicate and target identities; exact occurrence and completion rules | WASM observations plus reusable C# goal orchestration |
| Animation definition | Shared evaluator kind, typed curve/track parameters, start/end/duration/repeat, clock projection, feedback/event bindings, overlap/visibility/removal policy | Separate C# animation worker |
| Presentation binding | Typed target/property, immutable descriptor, baseline, physical pose or animation instance IDs, declarative mapping and art identities | Browser renderer (Render Graph); instanced draw batches |

All floating game data is canonical IEEE-754 f32 (single precision); IDs, enum tags, counts and ticks keep their integer types. A source's authored formula (for example a speed gain) is expressed through a supported reusable law and typed coefficients. Bodies, inventories, sensors, controller state and animation instances each have one owner. A physical shaft is a body/constraint; decorative spin is a separate animation track. The distinction depends on functional geometry/work, not the appearance or name of the part.

## Compile at Run

1. Decode the complete current construction, definitions, mode values, ports and resource references. Validate enum variants, finite canonical values, units, admission-only bounds ([envelope](gpu-f32-physics.md#game-grade-envelope)), identities and required fields before any mutation.
2. Expand assembly members deterministically and allocate typed identities. Resolve geometry, materials, joints, stores, sensor regions, controller inputs and animation targets. Reject dangling/foreign references, incompatible ports, duplicate ownership or conflicting property writers.
3. Build the transitive capability closure, including declared possible transitions (a heater may require heat, phase and changed contact; an actuator may require supply, work accounting, constraints and contact).
4. Pack the admitted capabilities into typed GPU buffers for immutable definitions/topology, committed state, candidate state and results, and C# discrete state separately. Instance-ID-to-storage mappings are stable; packing order cannot alter physical ordering.
5. Build the dispatch plan: each dispatch selects a reusable kernel by typed capability/shape/material kind and an input range. Static specialisation may remove unused work; it must not generate part-specific equations.
6. Size every table (bodies, shapes, contacts/constraints, network nodes, cells/samples, event paths, outputs) from the compiled scene; there are no fixed capacities and memory is the only bound. An unsupported capability or coupling fails admission for that construction atomically with a typed reason and preserves the previous construction; nothing partially installs.

The compiler decides structure and admission only. It never computes a physical trajectory, constitutive result or numerical correction in C#. GPU shape/query admission is authoritative; the CPU editor preview is non-authoritative.

<a id="generic-compilation-model"></a>

### Generic Homogeneous Compilation in WorkshopPhysicsCompiler

The compilation of a user contraption from the Logical Scene Graph into the flat WASM SIMD Physics Graph is strictly homogeneous and data-driven:
- **Homogeneous Part Iteration:** `WorkshopPhysicsCompiler` iterates over placed declared instances. It does NOT contain `switch (part.Kind)` or type-testing branches (`if (part is Motor)`); the remaining type-pattern dispatch is listed in the isolation authority's Current status.
- **Component Packing:**
  - For each instance, the compiler takes its element's declared `ColliderDeclaration` records from the bundle into the Dynamic AABB BVH and narrowphase collider tables, without element identity branching.
  - The compiler unpacks the declared capabilities directly into the flat SIMD-aligned Structure-of-Arrays (SoA) tables: bodies into body SoA arrays, constraint rows into Box2D v3 TGS Soft constraint tables, force regions into force SoA tables, and network nodes into domain tables. Every table is sized from the compiled scene.
  - Active wire connections between compatible declared sockets are compiled into multi-domain graph entries: supply routes over the reachability graph for `Electrical` domains (Modified Nodal Analysis is a named later capability, added by the first element story that needs it), discrete event routing tables for `Activation` domains, and kinematic ratio constraints for `Mechanical` domains.
- **Bitwise Determinism & Zero GC:** The compiler builds flat, cache-aligned (64-byte) tables with zero dynamic heap allocation in the simulation loop, guaranteeing deterministic execution across all browsers and platforms.

## Advance Physical Time: Staged Multi-Domain Solver Pipeline

Each 120 Hz tick advances through four 480 Hz substeps under the [solver model](gpu-f32-physics.md#solver-model). The physics engine executes the following staged pipeline on the dedicated WASM SIMD Web Worker (with strictly zero element-specific solvers, zero per-part update loops, and zero element-selected kernel branches):

1. **Multi-Domain Network Execution:**
   - **Electrical Domain:** Closes each compiled supply route whose source is enabled and, for a switched route, whose committed switch contact is closed; the shared allocation debits finite supply. Modified Nodal Analysis (node potentials, branch currents) is a named later capability, added by the first element story that needs it.
   - **Optical Domain:** Marches discrete spectral rays across mirrors, lenses, filters, and splitters, evaluating reflections, refractions, and power deposition.
   - **Acoustic and Pneumatic Fields:** Evaluates pressure distributions, duct flow continuity, acoustic cone projections, and nozzle momentum transfer.
   - **Thermodynamic Accounting:** Proportional power allocation debiting finite energy stores with strict conservation: kinetic energy gain cannot exceed stored potential ($\Delta K \le E_{\text{store}}$). No free energy.
2. **External Forces and Velocity Integration:**
   - Integrates gravitational acceleration and declared force regions (conveyor drag, airflow jets, buoyancy) into linear velocity using 128-bit SIMD vector instructions (`wasm_f32x4`); angular velocity changes only through contact impulses. Rotation uses the compiled principal inertia of each body record (sphere and box alike); the solver carries no shape-specific inertia constants. Right after gravity each sub-step applies the body's declared `LinearDrag` $c$ (body record +74) as the exact decay $\mathbf{v} \leftarrow \mathbf{v}\,e^{-c\,\Delta t}$; there is no angular drag, so free-flight spin is never damped.
3. **Broadphase Collision Detection:**
   - Queries the Dynamic AABB Bounding Volume Hierarchy (BVH). Leaf nodes use speculative velocity fattening ($|\mathbf{v}|\Delta t + \text{slop}$) and surface-area-heuristic incremental tree rotations.
   - Eliminates 85–95% of tree updates during steady motion, pruning non-colliding pairs in $O(N \log N)$ time.
4. **Narrowphase Manifold Generation:**
   - Evaluates active pairs using specialized analytic geometric solvers: Sphere-Sphere, Sphere-Box, Box-Box Separating Axis Theorem (SAT), Sphere-Plane, Box-Plane, Capsule, and Compound Cylinder.
   - Evaluates hollow/concave geometry (pipes, funnels, chutes) using hollow signed distance fields (SDF) with normal vector gradients.
   - Emits persistent 1-to-4 point contact manifolds with feature IDs for warm starting.
5. **Island Partitioning and Deactivation:**
   - Partitions contacting bodies and joint constraints into independent kinematic islands using Disjoint Set Union (DSU / Union-Find with path compression).
   - Inactive or settled islands are put to sleep, reducing active solver workload to 0 ms for at-rest assemblies.
6. **Box2D v3 Temporal Gauss-Seidel (TGS) Soft Constraint Solve:**
   - Solves contact normal non-penetration, Coulomb friction and bilateral joint constraints across sub-steps with warm starting, then a rolling-resistance row that is not warm-started (its impulse restarts from zero every sub-step). Rolling resistance is one angular row per manifold at every sphere contact (static or dynamic partner, equal and opposite on both), solved after the normal rows of each sweep: it opposes the relative rolling with at most $C_{rr}\,\lambda_N\,r$ per sub-step, where $C_{rr}$ is the larger declared material coefficient (material record +14; balls declare it, boxes, planes and walls declare zero) and $r$ the larger sphere radius, and it targets zero rolling so it stops a ball on a static support without reversing it (against a dynamic anisotropic box the per-axis diagonal effective masses are an approximation). A rolling solid sphere therefore decelerates at $\tfrac{5}{7}(C_{rr}\,g + c\,v)$. A struck body such as a Domino receives the reaction, so its outcome includes the ball's resistance (owner decision 9 Oct 2026).
   - Erin Catto's TGS Soft formulation mathematically unifies spring compliance, damping, restitution, and penetration slop directly into effective constraint mass:
     $\Delta \lambda = -M_{\text{eff}} \left( J \mathbf{v} + \frac{\beta}{h} C + \frac{\gamma}{h^2} \lambda \right)$.
   - Eliminates separate Baumgarte position projection and constraint explosion bugs.
7. **Position Integration and Quaternion Normalisation:**
   - Advances positions and orientations under the solved velocities. Normalises quaternions with SIMD fast inverse square root.
8. **Endpoint Sensor Sampling and Event Latching:**
   - Evaluates continuous residence, aperture crossings, and impact sensors at substep endpoints. Latches state transitions into discrete event queues.
9. **Zero-Copy Publication to SharedArrayBuffer Ring:**
   - Commits substep poses directly into the lock-free triple-buffered ring in `SharedArrayBuffer` using atomic sequence counters, guaranteeing zero-latency access for the main thread renderer without `mapAsync` stalls.

Dependencies between domains execute in declared order within the substep. Where domains feed back on each other (electrical → heat → material strength → contact → mechanical work), the owning slice freezes a staged order and a bounded iteration budget that takes the best iterate; it never lags a feedback edge by a tick without declaring it, iterates unbounded or hides a CPU solve.

Cross-domain exchange has one semantic record: source and sink store IDs, quantity/unit, proposed signed amount, corresponding work/impulse/enthalpy, material/species identity, interval and declaration identity. Kernels produce proposals; shared allocation considers every consumer of a finite source together so two receivers cannot spend the same charge, heat or fluid. Mechanical/electrical/fluid/thermal/radiative conversions declare losses and reactions (motor electrical debit and shaft work, pressure work and chamber internal energy, absorbed optical power and heat, frictional loss and its sink). A signal or animation event carries information, not free power: no free energy is a slice gate.

Within a tick, C# discrete controllers and goals consume committed WASM observations at tick boundaries and publish together with the physical state. Dynamic geometry, phase/fracture/spawn changes install as one bounded topology transaction. Reset restores exact construction bits; reversing physics is never restoration.

## Animation and rendering

All animation executes on an independent Animation Web Worker at 60 Hz, completely decoupled from physical truth. Data flows strictly one-way: physics → animation → renderer and physics → renderer; nothing ever flows back into physics. Animation is entirely data-driven and event-driven: cosmetic curves, squash-and-stretch, motor spin, indicator/UI easing, and particle effects evaluate declarative bindings to committed physical poses, committed level states and one-shot event-ring occurrences. Strictly zero per-element animation scripts or callbacks exist in the animation host (ANIM-1 removes the remaining legacy evaluators).

The animation worker evaluates shared clip/easing/follow/impulse/oscillation and occurrence-lifecycle evaluators over active instances compiled from declaration data. Each binding declares its input channels, direction/range, clock projection, target property and lifecycle. A new effect needs a reusable evaluator extension when existing kinds cannot express it; a per-element callback inside the animation host is forbidden. Autonomous UI tracks use session time and run without physics; world feedback uses identified committed observations/events. Present-once effects retain their instance until a visible receipt or Reset. Disabled/reduced-motion effects cannot change physical results.

The renderer samples one coherent display time, reads bracketing substep snapshots directly from the `SharedArrayBuffer` triple-buffered pose ring with atomic sequence verification, performs Slerp interpolation of orientations and linear interpolation of positions, applies animation child outputs, and executes instanced GPU draw calls (`gl.drawElementsInstanced` in WebGL 2, `renderPass.drawIndexed` in WebGPU). One writer per target property. Physics never reads the drawn transform back. Descriptors choose element-specific mesh, colour, sound or curve data; they cannot choose physical equations. Run, Pause, Step, Resume, Completed, Reset, Load and disposal keep their acknowledged semantics; the durable format is construction-only.

## Capability families and concrete composition

The capability IDs in the element map index required responsibilities, not working solvers. Each family becomes record types plus shared kernels inside the slice that first needs it; [bounded model decisions](planning/invest/decisions.md) retain unresolved equations.

| Family and consumers | Declarative composition and shared execution | Required distinction / model owner |
| --- | --- | --- |
| Rigid cargo, walls, ramps, hollow pipes, cams and gates | Body/collider/material records → broadphase, speculative shape-pair contact, sequential impulses, joint rows, finite-work loads → committed pose | Generic shape-pair table including [hollow SDF](hollow-geometry-design.md) and moving references; ENGINE-CORE-2, CAT-048, P0-007/P0-031 and each CAT-D |
| Springs, wound stores, ropes, pulleys, shafts, gears and conveyors | Spring/stop constraints, tension-only rope rows, finite inertia, ratio/engagement and drive rows in the same impulse pass | Slack cannot push; obstruction loads the source; no launch or copy-speed shortcut. [Springboard](springboard-elastic-contract.md), [stores](world-owned-energy-stores.md), [drives](bounded-acceleration-drives.md), [transmission](rotary-transmission-parts.md); S019/S020/S109/S149 design owners |
| Liquid transport, reservoirs, moving buckets, siphons, pumps and buoyant cargo | Finite material stores, port openings, pressure/head relations, transport transfers, displacement and capillary primitives; carried mass/inertia and spilled material updated consistently | A moving container is not a stationary counter: free surface, spill, siphon continuity and pressure limit need their own adopted model. S416 → S418–S421 and named source D owners |
| Gas, chambers, nozzles, airflow, balloons and turbines | Gas state, finite inventory/enthalpy, chamber volume, supply/exhaust/nozzle ports, paired momentum/work and shared linear/rotary capture | Open flow differs from sealed pressure; unloaded emission cannot be inferred from a hit. [Gas](finite-gas-foundation.md), [airflow](finite-gas-foundation.md#airflow-transfer), [rotor](finite-gas-foundation.md#rotary-capture); S470/S471 and consumer owners |
| Electrical supply/storage/loads and logic | Typed power terminals plus finite ledger and network solve; separately typed signal edges, Boolean/enum operations, delay/latch/counter and actuator-intent bindings | Signal true creates no energy; fan-out shares supply. Supported cycles and finite losses are frozen by S021/S022/S257 |
| Light, colour, mirrors, lenses, filters and receivers | Finite spectral bands, aperture/geometry/material transport, reflection/refraction/filter/conversion parameters, sensor channels and committed beam artwork | Finite width/occlusion/dispersion and power allocation preserve the source budget; eight colours and each logic variant stay explicit. S484 → S485/S486/S488/S489 |
| Acoustic sources, ducts, resonators and receivers | Finite pulse/continuous supply, band/path/arrival data, propagation/loss/resonance model, typed sensor state and shared occurrence animation | Strongest-arrival versus additive intensity, route loops and band response are unresolved until S528/S529/S530; pulse and continuous speakers stay distinct |
| Heat, reactions, phase and material topology | Enthalpy/species stores, thermal ports, sensible/latent relationships, conduction/convection/radiation, reaction rates, phase fractions, strain/stress/strength and transactional geometry | No temperature-setter shortcut. Partial melt retains partial support; broken material does not reattach on cooling. S543 → S544–S566 and individual TH-D owners |
| Ionizing sources, shields, meters, badges, converters and tracks | Typed particle/energy groups, finite source populations, material intervals, field response, rate and path-integrated exposure, bounded conversion and detector sensitivity | Photon paths, oppositely charged trajectories and fast/slow neutron moderation need distinct models; rate, accumulated dose and residence differ. S604/S605 → S606/S607; LAW-FIELD-D |
| Grains, coatings, cutting and fracture | Finite material batches, contact geometry, deposited surface state, declared strength/work and bounded topology proposals | Aperture sorts real size, cutting consumes work, fragments conserve material; no target-name recognition. LAW-GRANULAR-D, S562-D/S563-D and each source D owner |
| Characters, programmable toys and goal controls | Typed sensor/perception queries → reusable finite-state policy → actuator intent; shared locomotion/contact laws produce actual motion; cosmetic pose uses animation bindings | Occluded lures and unreachable targets cannot trigger scripted travel; LAW-CONTROLLER-D plus each character's source D decision |
| Environment, placement, editor, blueprint/save and diagnostics | Typed environment and construction/connection records, validated spatial admission, deterministic ID remapping, committed observation views and shared UI tracks | UI workflows are not physical laws. No executable imported scripts or source-matched solution behaviour. LAW-ENVIRONMENT-D/LAW-GOALS-D and individual workflow owners |

## Worked assemblies and discriminating controls

**Basketball and Receiver:** the ball declares sphere geometry/material, mass, gravity and drag; the Receiver declares its box bodies, a residence sensor (open local bounds, speed limit, dwell in ticks) and, after ENGINE-CORE-1, its guide as a declared force region. The same shared contact kernels handle ball/workbench and ball/Receiver; WASM SIMD samples residence at substep endpoints; a reusable discrete latch emits one occurrence that drives a shared halo binding. Preserve default margin 0.02, speed 1.5, dwell 0.35 and zero guide. Edge/fast-through, duplicate event, changed declaration instance and exact Reset distinguish the general path from an identity-based capture script. CAT-004-D owns the model freeze.

**Motor, conveyor and trapped cargo:** electrical debit feeds a bounded drive row on the shaft hinge; the conveyor's surface-per-radian binding, contact/friction and load reaction consume the same committed shaft state. An obstruction cannot leave cargo moving because artwork spins; decorative cues are animation followers. The [transmission](rotary-transmission-parts.md) and [drive](bounded-acceleration-drives.md) declarations supply the data; disconnected/exhausted supply and blocked cargo are required controls.

**Fan or bellows driving a rotor:** source and receiver declare the same work-conjugate ports, finite supply and transfer coefficients. Shared allocation divides one source allowance across linear and rotary branches with paired reactions and source debit. The fan's declared flow and the bellows' slider-derived flow are coefficients, not catalogue branches. Disconnected, blocked, unloaded emission, multiple receivers and exhausted supply distinguish supported transfer from hidden free thrust ([rotor](finite-gas-foundation.md#rotary-capture), [transfer](finite-gas-foundation.md#airflow-transfer), [nozzle](finite-gas-foundation.md)).

**Heated water driving a piston:** electrical dissipation adds enthalpy to a finite vessel; the phase model converts liquid to vapour, gas/nozzle transfers carry mass/enthalpy, and the generic pressure actuator exchanges work with a loaded body and exhaust. TH-17/19/35 differ in geometry/ports, not in boiling or piston code. Empty vessel, inadequate latent energy, blocked exhaust and excessive load must fail differently. Phase/transport coupling remains S543/S550/S565 and S471/S419 design work.

**Lens, combustible target and fusible support:** optical transport allocates absorbed power to the target's thermal store; shared reaction/material laws determine ignition or strength, then a topology transaction releases a loaded joint. Defocus, non-reactive material, missing oxidizer and sub-threshold heat are controls. A TH-22 bimetal thermostat needs two-material differential strain and contact hysteresis, not a temperature switch. S554/S555/S557/S558/S566 and the named source owners hold these decisions.

**Radiation instrument chain:** a capsule's committed trajectory feeds typed group/material transport. RAD-09 accumulates exposure across bursts and retains it on supply loss; RAD-08 measures rate. RAD-14 bends Alpha and BetaMinus by their distinct signed models; Photon is unaffected. RAD-17 moves fast to slow neutron population with declared energy accounting; absorption is a separate sink. Scintillation and thermoelectric output consume deposited energy through shared conversion ports. S606/S607 and the RAD owners freeze the grouped transport models and their Chrome-observable oracles before implementation.

**Lured character and cut rope:** a controller queries occlusion/range and chooses a typed locomotion intent; physics resolves contact and actuation. It cannot write an authored destination pose. Scissors declare contact geometry, cutting work and material failure, updating a rope's topology only after the shared fracture criterion. EL-082/083/086/087 and EL-066/068 need LAW-CONTROLLER/S563 models, not cosmetic walking or counterpart-type checks.

## Remaining design decisions

The [element map](planning/general-engine-element-map.md) gives every source its owner. These gaps refine those owners inside the slice that first needs them; they create no replacement IDs or global prerequisite project.

| Gap route | Decision before the affected slice | Owner |
| --- | --- | --- |
| General continuous sensors | Moving-frame containment, threshold intervals, residence versus cumulative exposure, identity/disable/reset semantics | CAT-004-D with P0-004 and affected sensor source D |
| Fluid and moving contents | Free-surface/pressure representation, conservative open/closed transport, spill and dynamic mass/inertia coupling; siphon priming/column break | S416, S418-D/S419-D/S420-D/S421-D; each fluid source D |
| Coupled domains | Per admitted feedback group, the staged order, bounded iteration budget (best iterate) and Chrome-observable oracle | P0-004/P0-006 and participating law D owners |
| Heat/phase/topology | Material tables, finite phase/reaction rates, partial geometry/support, strain/stress/fracture and replacement ordering | S543 and S544-D–S566-D |
| Acoustic interpretation | Arrival combination, finite resonance and channel/route/overlap models | S528, S529-D/S530-D |
| Radiation and fields | Grouped transport/stopping/moderation, charged path integration, response/dose, decay envelope and finite field supply | S604/S605, S606-D/S607-D, LAW-FIELD-D |
| Characters and programmable policies | Perception/preset operations, energy source, locomotion/contact actuation, pursuit/avoidance/obstacle transitions | LAW-CONTROLLER-D and each EL source D |
| Discrete materials | Grain distribution/capacity, cutting/fracture models, deposited coating state and bounded topology | LAW-GRANULAR-D, S562-D/S563-D and named source D |
| Animation expressibility | Each needed shared evaluator/feedback kind with f32 ranges, clock projection, occurrence lifecycle and visual oracle; no unknown evaluator becomes a callback | P0-004/P0-022/P0-023/P0-024 and named consumer |

## Conversion of existing elements to packages and the element bundle

Every existing puzzle element is converted forward, one family per story in the [21 Oct 2026 delivery order](../_bmad-output/planning-artifacts/epics.md#delivery-order-coordinator-21-oct-2026-owner-authorised-re-examination), without backwards compatibility:
- **Package and scene:** the element's declaration moves into its `elements/<Name>/` package and its art into its own `.tscn` scene with `col_*` proxies and `Loc_Port_<Id>` locators.
- **Generic instance:** its per-element record, factory and panel are replaced by the generic declared instance and the generic parameter editor.
- **Delete in the same story:** each conversion deletes everything it supersedes (records, switch tables, collider tables, panels, curves); every deleted symbol returns 0 production hits. Strictly zero compatibility shims, adapters, fallback loaders or migrations are permitted. Git history is the permanent archive.
- **Status:** the [isolation authority's Current status](architecture/element-package-isolation.md#current-status) lists each remaining violation with its owning story.

A broad capability such as GeometryQuery, FiniteLedger or ProgrammableController appearing in a source's row does not make that source covered; the source's exact behaviour wins, and the missing reusable primitive is identified in the same design stage. Acceptance of this document proves design coverage and ownership only. Runtime acceptance for each slice is its Chrome/Playwright proof through real controls with the negative control, exact Reset and Save/Load, production build and the grep that finds no element identifier in the physics solver or the evaluator selection; release numbers and P0-035 engine closure are release-checklist gates enforced at LEGACY-0 and CAMPAIGN through P0-034, never reasons to postpone a playable slice.
