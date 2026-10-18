# Specification: Story 9.2b Modular Element Package Isolation, Pure C# WASM Solver Consolidation & Multi-Consumer Event Ringbuffer (ENGINE-CORE-2e)

## 1. Overview & Context

- **Goal:** Execute comprehensive modular architectural refactoring to achieve strict compile-time project isolation for all puzzle elements, consolidate 100% of physical and sensor simulation into pure C# compiled to WebAssembly (`wasm-simd128`), eliminate custom JavaScript solver logic from `worker.js`, and introduce a lock-free multi-consumer Event Ringbuffer for discrete simulation occurrences.
- **Role in Tri-Graph Architecture:**
  1. **Logical Scene / Machine Graph (Main Thread):** Loads puzzle elements dynamically from a compiled bundle without compile-time knowledge of concrete element classes or IDs.
  2. **Physics Graph (WASM Worker):** Runs 100% in C# WASM SIMD at 120 Hz (480 Hz substeps). Dynamic AABB BVH with velocity fattening, analytical collision manifolds, TGS Soft solver, MNA electrical solver, and passage sensors execute purely in C#.
  3. **Rendering Graph (Animation Worker & Main Thread):** Driven by zero-copy pose ringbuffers and event ringbuffers with zero back-pressure or GPU-to-CPU stalls.
- **Compliance with Balanced Coupling (`coupling.dev`):**
  - High Distance (cross-project element isolation, cross-worker thread boundaries) strictly demands Low Integration Strength (Contract Coupling via typed Abstractions and Ringbuffer ABIs).
  - Eliminates all Intrusive and Functional coupling across boundaries.

---

## 2. Balanced Coupling Modularity Analysis (coupling.dev)

Following the Balanced Coupling model (`BALANCE = (STRENGTH XOR DISTANCE) OR NOT VOLATILITY`), the system boundaries are classified across three dimensions:

| Integration Boundary | Distance | Integration Strength | Volatility | Balance Assessment | Architectural Rule |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **Puzzle Elements $\leftrightarrow$ Core Engine** | **High** (Isolated projects under `elements/*`) | **Contract** (Lowest: DTOs, interfaces, metadata descriptors in `Abstractions`) | **Low** (Supporting Subdomain) | **Balanced** (High Distance $\oplus$ Low Strength) | Core Engine has **0 references** to Element projects. Elements reference only `Abstractions`. |
| **Simulation Worker $\leftrightarrow$ Main UI Thread** | **High** (Separate Web Worker threads) | **Contract** (Pose ringbuffer ABI, Command/Query DTOs, Event ringbuffer) | **High** (Core Subdomain) | **Balanced** (High Distance $\oplus$ Low Strength) | Communication **strictly restricted** to Ringbuffers and Command/Query messages. Zero shared heap. |
| **Simulation Worker $\leftrightarrow$ Animation Worker** | **High** (Independent worker threads) | **Contract** (Zero-copy Pose Ringbuffer & Event Ringbuffer in `SharedArrayBuffer`) | **High** (Core Subdomain) | **Balanced** (High Distance $\oplus$ Low Strength) | Zero-copy lock-free ringbuffers only. One-way physics $\to$ animation flow. |
| **Physics Solver $\leftrightarrow$ Numerical Kernels** | **Low** (Internal to `CuriousContraptions.Simulation`) | **Model / Functional** (SIMD SoA memory buffers, TGS solver equations) | **High** (Core Subdomain) | **Balanced** (Low Distance $\oplus$ High Strength = High Cohesion) | Co-located in C# WASM for maximum cache locality and SIMD execution speed. |
| **Host Worker Shell (`worker.js`) $\leftrightarrow$ C# WASM Runtime** | **Medium** (JS Host to Dotnet WASM runtime) | **Contract** (Minimal bootstrap shell: `postMessage` envelope, `SharedArrayBuffer` passing) | **Low** (Generic Subdomain) | **Balanced** (Low Volatility $\to$ Stable) | Stripped of all physics/solver logic (~50–80 lines total). No custom JS physics. |

---

## 3. Package & Project Architecture

### 3.1 Abstractions Package Hierarchy
The core engine and elements interact exclusively through typed abstractions:
1. `CuriousContraptions.Abstractions`:
   - `IPuzzleElementDeclaration`: Base interface for element registration.
   - `ElementMetadata`: Id, Category, DisplayNameKey, IconPath, ScenePath, Parameters.
   - `PortDescriptor`: PortId, Domain (Mechanical, Electrical, Fluid, Signal), Direction (Input, Output, Bidirectional), LocalPosition, Normal.
   - `ColliderDescriptor`: ShapeKind (Box, Sphere, Cylinder, Capsule, Mesh), Dimensions, Offset, Rotation, MaterialProperties.
   - `EventDescriptor`: EventType, PayloadLayout.
2. `CuriousContraptions.Abstractions.Mechanical`:
   - Rigid body mass properties, kinematic modes, anchor points, friction, restitution.
3. `CuriousContraptions.Abstractions.Electrical`:
   - Electrical source, switch, consumer node descriptors, rated voltage, resistance, capacitance.
4. `CuriousContraptions.Abstractions.Fluid`:
   - Fluid intake, discharge, buoyancy, pressure, volume bounds.
5. `CuriousContraptions.Abstractions.Signal`:
   - Activation trigger, pulse duration, bistable latch, counter threshold contracts.

### 3.2 Element Project Isolation (`elements/*`)
Every puzzle element is isolated in its own independent project:
- `elements/Ball/Ball.csproj`
- `elements/Bumper/Bumper.csproj`
- `elements/Domino/Domino.csproj`
- `elements/Ramp/Ramp.csproj`
- `elements/Wall/Wall.csproj`
- `elements/Battery/Battery.csproj`
- `elements/PoweredGate/PoweredGate.csproj`
- `elements/PressurePlate/PressurePlate.csproj`
- `elements/Counter/Counter.csproj`
- `elements/SignalLamp/SignalLamp.csproj`
- `elements/BallDetector/BallDetector.csproj`

Each element project references **only** `CuriousContraptions.Abstractions` and its specific domain abstractions (e.g. `.Mechanical`, `.Electrical`). They contain zero project references to `CuriousContraptions`, `CuriousContraptions.Simulation`, or sibling elements.

### 3.3 Compile-Time Build Pipeline & Asset Bundling
- An MSBuild packaging task or source tool (`tools/BundleElements`) gathers:
  - Element metadata manifests (`element.json` or generated C# source).
  - Scenes (`.tscn`), resources (`.tres`), and icons.
  - Localization strings.
- Emits a unified distribution bundle into `AppBundle/elements.bundle.json` and asset paths.
- The Core Engine dynamically parses and loads this bundle without containing compile-time references to element assemblies.

---

## 4. Pure C# WASM Solver Consolidation

### 4.1 Elimination of Custom JS in `worker.js`
All physics and domain logic previously present in `worker.js` is removed:
- `DynamicBVH` $\to$ C# `WorkshopDynamicBvh` inside `CuriousContraptions.Simulation`.
- Analytical collision manifolds (Sphere-Box, Box-Box, Sphere-Sphere) $\to$ C# `WorkshopCollisionKernels`.
- TGS Soft constraint solver & numerical velocity integration $\to$ C# `WorkshopConstraintSolver` using `wasm-simd128` vectorization.
- Passage sensors and pressure plate load tracking $\to$ C# `PassageSensorTracker` and `ContactLoadSensor`.
- Gate actuation & prismatic joints $\to$ C# `PrismaticConstraintSolver`.

### 4.2 Minimal Worker Shell (`worker.js`)
`worker.js` is reduced to an un-opinionated host shell (~50–80 lines):
1. Boots `dotnet.create()`.
2. Receives initialization message with `SharedArrayBuffer` references.
3. Exposes minimal `workshopGpu` JS host functions: high-resolution clock (`now`), `postMessage` relay for commands/queries.
4. Relies on C# WASM runtime exports (`Program.Tick()`, `Program.Bootstrap()`, `Program.ExecuteCommand()`).

---

## 5. Multi-Consumer Event Ringbuffer

### 5.1 Architecture & Memory Layout
In addition to the continuous Pose Ringbuffer, simulation occurrences stream through a lock-free, zero-copy Event Ringbuffer in `SharedArrayBuffer`:
- **Ringbuffer Header (64 bytes):**
  - `Capacity` (uint32, power of 2, e.g. 1024 slots)
  - `SlotBytes` (uint32, e.g. 64 bytes per event record)
  - `WriteSequence` (atomic uint64: monotonic sequence written by producer)
  - `Reserved` (padding to 64-byte cache line)
- **Event Slot Layout (64 bytes):**
  - `Sequence` (uint64): Must match slot sequence to guarantee complete write.
  - `TimestampNs` (uint64): Simulation monotonic timestamp.
  - `Domain` (uint32): Domain enum bitmask (1 = Mechanical, 2 = Electrical, 4 = Signal, 8 = Fluid, 16 = Audio).
  - `EventKind` (uint32): Specific occurrence kind (Impact, Passage, CounterReached, LatchSet, etc.).
  - `SourceBodyId` (uint64): Origin body/element ID.
  - `TargetBodyId` (uint64): Interacting body/element ID.
  - `Payload` (8 x float32): Impulse, contact position, voltage, or event-specific data.

### 5.2 Multi-Consumer Filtering
- Each consumer (Main UI Thread, Animation Worker, Audio Worker) maintains its own private `ReadCursor` sequence.
- Consumers read available slots where `ReadCursor < WriteSequence`.
- Consumers apply a bitmask/predicate filter (e.g. Audio Worker filters `Domain & (Mechanical | Audio) != 0`, ignoring pure electrical events).
- Lock-free, non-blocking: slow consumers can detect overrun if `WriteSequence - ReadCursor > Capacity`.

---

## 6. Verification & Acceptance Criteria

- **AC 1: Project Isolation:** All puzzle elements are housed in dedicated `elements/*` projects referencing only `Abstractions` packages. The core engine (`CuriousContraptions`, `CuriousContraptions.Simulation`) contains zero references to element projects, zero hardcoded element types, and zero element-specific assets.
- **AC 2: Pure C# WASM Simulation:** 100% of simulation code (BVH, collisions, TGS solver, passage sensors, electrical MNA) runs in C# compiled to WASM. `worker.js` contains zero physics logic and is reduced to a minimal host shell (~50–80 lines).
- **AC 3: Inter-Thread Communication Constraints:** Communication between workers is strictly confined to Ringbuffers (continuous updates), Commands/Queries (discrete work requests), and the Multi-Consumer Event Ringbuffer.
- **AC 4: Multi-Consumer Event Ringbuffer:** Lock-free Event Ringbuffer in `SharedArrayBuffer` successfully publishes discrete events to multiple concurrent consumers with independent cursors and filtering.
- **AC 5: Build-time Packaging Pipeline:** Build process aggregates element metadata, assets, icons, and i18n into an `elements.bundle.json` package dynamically loaded at runtime.
- **AC 6: Unit Tests:** `dotnet test CuriousContraptions.slnx` passes 100% across all test suites.
- **AC 7: Chrome Playwright Verification:** All existing Playwright E2E suites (`cat-052`, `cat-051`, `cat-050`, `cat-049`, `cat-048b`, `cat-048a`, `cat-002`, `cat-030`, `cat-020`) pass 100% in Chrome serially without regression.
- **AC 8: Static Analysis & Review:** `anvil check` reports 0 errors and 0 warnings. Independent adversarial review by Murdoch yields a terminal scoped Pass.
