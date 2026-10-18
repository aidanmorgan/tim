# Specification: FlatBuffers Simulation Compiler Pre-Calculated Fixed-Offsets & Universal Wire

## 1. Overview & Context

- **Goal:** Unify inter-system communication and memory operations under the FlatBuffers architecture:
  1. Use lock-free SharedArrayBuffer ring buffers (`WorkshopPoseRing`) between simulation, animation, and renderer for zero-copy 60Hz/120Hz poses.
  2. Use FlatBuffers for all encoding and decoding of commands, acknowledgements, physical reads, and saved constructions (`WorkshopWireFlatBuffers`, `schemas/workshop_wire.fbs`).
  3. Pre-calculate all fixed-offset byte addresses and slot indices in the simulation compiler (`WorkshopPhysicsCompiler`), eliminating runtime manual offset calculations and $O(N)$ linear index searches (`BodySlot`, `MaterialSlot`) during admission and simulation memory operations.
  4. Ensure zero legacy shims, 0 Anvil warnings, 100% unit test pass rate (906+ tests), and 100% Playwright Chrome E2E pass rate.

## 2. Technical Architecture

### 2.1 Pre-Calculated Memory Layout (`CompiledMemoryLayout`)
- Currently, `PhysicsGpuAbi.Admission` calculates offsets dynamically with arithmetic like `BodiesOffset + i * BodyBytes` and calls `BodySlot(scene, id)` which linearly iterates through `scene.Bodies` for every collider, sensor, guide, trigger, contact work, and orientation sensor.
- We introduce `CompiledMemoryLayout` in `engine/gpu/CompiledMemoryLayout.cs`:
  - Contains immutable mappings for `BodySlots`, `BodyOffsets`, `ColliderSlots`, `ColliderOffsets`, `MaterialSlots`, `MaterialOffsets`, `SensorSlots`, `SensorOffsets`, `GuideSlots`, `GuideOffsets`, `TriggerSlots`, `TriggerOffsets`, `ContactWorkSlots`, `ContactWorkOffsets`, `OrientationSensorSlots`, `OrientationSensorOffsets`, `ElectricalSourceSlots`, `ElectricalSourceOffsets`, `PrismaticSlots`, `PrismaticOffsets`.
  - Built at compile time by `PhysicsSceneDeclaration` / `WorkshopPhysicsCompiler`.
  - Exposed via `PhysicsSceneDeclaration.Layout`.
  - `PhysicsGpuAbi.Admission` refactored to read directly from `scene.Layout` for all slots and fixed byte offsets, achieving $O(1)$ lookups and zero allocation during admission.

### 2.2 Universal FlatBuffers Wire
- `CuriousContraptions.Simulation/Program.cs`:
  - `ReadOutput` buffer allocation replaced with direct FlatBuffers encoding: `WorkshopWireFlatBuffers.EncodeResponse(readResponse)`.
  - Both commands, acknowledgements, and physical reads flow end-to-end as FlatBuffers.
- `engine/gpu/BrowserWorkshopClient.cs`:
  - `ReceiveRead()` decodes incoming bytes with `WorkshopWireFlatBuffers.DecodeResponse(bytes)` with backward-compatible fallback to `WorkshopWire.DecodeResponse(bytes)`.
- `CuriousContraptions.web/wwwroot/workshop-client.js` & `worker.js`:
  - Introduce `extractReadIdentity(bytes, responseAbi)` to parse `Session` (16 bytes) and `Publication` (8 bytes) directly from FlatBuffers root/vtable structure or legacy buffer fallback.

### 2.3 Pose Ring Zero-Copy
- Zero-copy triple-buffered SharedArrayBuffer pose ring (`WorkshopPoseRing`) remains the high-throughput conduit for 120Hz physical poses to the 60Hz animation worker and display-paced renderer.

## 3. Verification Plan
1. `CompiledMemoryLayoutTests.cs`:
   - Verify all body, collider, sensor, trigger, contact work, and electrical source offsets match canonical `PhysicsGpuAbi` table layouts.
   - Verify $O(1)$ slot lookups match array indices.
2. `WorkshopWireFlatBuffersTests.cs`:
   - Verify physical read responses roundtrip bit-exact with all body reads, captures, contact works, occurrences, and electrical records.
3. Unit Tests:
   - `dotnet test CuriousContraptions.slnx` -> 100% pass (906+ tests).
4. Chrome Playwright E2E:
   - All active test suites run against local dev server on port 8060.
5. Anvil Static Analysis:
   - `anvil check --changed` -> 0 warnings.
