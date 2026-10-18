# Adversarial Review Report: Story 9.2 — Pulse Counter Event Accumulator (CAT-020)

**Reviewer:** Murdoch (`bmad_reviewer`)  
**Mandate:** Independent Adversarial Review (REQ-14)  
**Story Under Review:** Story 9.2 ([`spec-9-2-pulse-counter.md`](file:///Users/aidan/dev/personal/tim/_bmad-output/implementation-artifacts/spec-9-2-pulse-counter.md))  
**Date:** 2026-10-17  

---

## 1. Executive Summary & Verdict

- **Verdict:** **Pass**
- **Scope:** Story 9.2: Pulse Counter Event Accumulator (CAT-020) — Trigger event accumulator, single activation emitter, and series electrical contact latch.
- **Core Outcome:** The Pulse Counter (`counter`) has been implemented under the modern declarative Tri-Graph architecture. It accepts activation pulses on `ActivationIn`, accumulates distinct physical trigger events up to an authored target ($N \in [1, 9]$, default 3), and upon reaching $N$, emits a single activation pulse through `ActivationOut` while latching an internal series electrical contact between `PowerIn` and `Supply`. Subsequent pulses saturate the counter without duplicate emissions. Negative controls confirm that sub-target triggers leave the circuit open and downstream devices inert. During review, adversarial inspection uncovered missing compilation of `CounterTests.cs` in `CuriousContraptions.tests.csproj` (with obsolete signatures) and explicit `any` types in `cat-020.test.ts`; both were corrected, bringing unit tests to 932/932 passing (100%) and static analysis to 0 warnings. Dedicated slice E2E tests and cumulative regression suites pass 100% in real Google Chrome on port 8060.

---

## 2. Criterion & Invariant Matrix

| Requirement / Invariant | Status | Verification Evidence |
| :--- | :--- | :--- |
| **Catalog Resource Definition** | **PASS** | [`parts/catalog/counter.tres`](file:///Users/aidan/dev/personal/tim/parts/catalog/counter.tres): `Id = "counter"`, `Title = "Counter"`, `Category = "Control"`, `Color = Color(0.91, 0.72, 0.39, 1)` (#e8b764), `Scene = ExtResource("2")`. Discovered dynamically by `PartRegistry.Discover()`. |
| **Declarative Part Instance & Boundaries** | **PASS** | [`engine/gpu/WorkshopCounter.cs`](file:///Users/aidan/dev/personal/tim/engine/gpu/WorkshopCounter.cs) defines `WorkshopCounter` with `TargetCount` in $[1, 9]$ (default 3), rejecting values $\le 0$ or $> 9$. `PartId.Counter` (`"counter"`, numeric 22) in [`engine/PartId.cs`](file:///Users/aidan/dev/personal/tim/engine/PartId.cs). |
| **Clean Entity Separation & Scene Geometry** | **PASS** | [`parts/scenes/counter.tscn`](file:///Users/aidan/dev/personal/tim/parts/scenes/counter.tscn): Presentation meshes grouped under `Visual/` (`Housing`, `Foot`, `Face`, 9 `DotLamp_0..8` indicators, 4 socket spheres). Exactly 2 static collision shapes (`CollisionShape_Housing` $1.65 \times 1.25 \times 0.65\text{ m}$ at $(0, 0, 0)$ and `CollisionShape_Foot` $1.8 \times 0.16 \times 0.85\text{ m}$ at $(0, -0.72, 0)$), both set to `visible = false`. Extracted cleanly by `GltfPhysicsBodyExtractor`. |
| **Port Locators & Connection Domains** | **PASS** | Scene defines 4 port locators: `Loc_Port_activation_in` at $(0, 0.72, 0.1)$, `Loc_Port_activation_out` at $(0, -0.72, 0.1)$, `Loc_Port_power_in` at $(-0.93, 0, 0)$, `Loc_Port_supply` at $(0.93, 0, 0)$. [`engine/gpu/WorkshopConnections.cs`](file:///Users/aidan/dev/personal/tim/engine/gpu/WorkshopConnections.cs) validates exact socket domains: `ActivationIn` and `ActivationOut` strictly in `Activation` domain; `PowerIn` and `Supply` strictly in `Electrical` domain. |
| **Activation Network & Event Accumulation** | **PASS** | [`engine/gpu/ActivationNetwork.cs`](file:///Users/aidan/dev/personal/tim/engine/gpu/ActivationNetwork.cs): `ActivationNodeKind.Counter` node accumulates distinct incoming event timestamps, filtering duplicate/held events. Reaching target $N$ transitions to `Reached`, produces `ActivationOccurrenceKind.CounterReached` activation pulse through `ActivationOut`. Extra pulses saturate without re-emitting. |
| **Series Electrical Latch & Actuation** | **PASS** | [`engine/gpu/WorkshopPhysicsCompiler.cs`](file:///Users/aidan/dev/personal/tim/engine/gpu/WorkshopPhysicsCompiler.cs) and [`CuriousContraptions.Simulation/wwwroot/worker.js`](file:///Users/aidan/dev/personal/tim/CuriousContraptions.Simulation/wwwroot/worker.js): When wired to Battery `Supply` $\to$ Counter `PowerIn` and Counter `Supply` $\to$ Powered Gate `PowerIn`, gate blade remains closed during sub-target counting ($N=2 < 3$, blade $y < 3.2$). Upon 3rd event, circuit closes, gate blade retracts open ($y > 4.2$), allowing upstream basketball to pass through. Saturation (4th ball) maintains open gate without glitch. |
| **Visual Dot Indicators & Reset Invariance** | **PASS** | [`engine/MachinePart.cs`](file:///Users/aidan/dev/personal/tim/engine/MachinePart.cs): `UpdateCounterLamps(count)` dynamically updates lamps $0 \le i < \text{count}$ to Gold (`#f7cb52`) and remaining to Slate (`#556573`). `ResetPhysicalVisuals()` restores all lamps to Slate on Reset. |
| **Persistence Roundtrip** | **PASS** | [`engine/gpu/WorkshopWire.cs`](file:///Users/aidan/dev/personal/tim/engine/gpu/WorkshopWire.cs), [`engine/gpu/WorkshopWireFlatBuffers.cs`](file:///Users/aidan/dev/personal/tim/engine/gpu/WorkshopWireFlatBuffers.cs), and IndexedDB bridge preserve `target_count` and 4-way wiring topology bit-identically across hard browser reloads. |
| **Capacity & Workbench Footprint** | **PASS** | [`engine/gpu/WorkbenchCapacity.cs`](file:///Users/aidan/dev/personal/tim/engine/gpu/WorkbenchCapacity.cs): `WorkbenchFootprint.Of(PartId.Counter)` declares 1 static body, 0 dynamic bodies, 2 colliders, 0 triggers, 0 contact works, 1 activation node, 0 electrical sources, 0 constraints. |
| **Zero Legacy Remnants** | **PASS** | Grep over active codebase confirms 0 occurrences of `CounterPart`, 0 occurrences of `WorkshopPartKind`, 0 occurrences of `PartArt`, 0 active WGSL compute shaders, and 0 Baumgarte velocity clamps or shims. |

---

## 3. Automated Test Verification Results

### 3.1 Static Analysis (`anvil check`)
- Command: `anvil check` on all Story 9.2 modified and created files:
  - `engine/gpu/WorkshopCounter.cs`
  - `CuriousContraptions.tests/CounterTests.cs`
  - `CuriousContraptions.tests/CuriousContraptions.tests.csproj`
  - `tools/e2e/cat-020.test.ts`
  - `parts/catalog/counter.tres`
  - `parts/scenes/counter.tscn`
  - `engine/gpu/ActivationNetwork.cs`
  - `engine/gpu/WorkshopPhysicsCompiler.cs`
  - `engine/gpu/WorkshopConnections.cs`
  - `engine/gpu/WorkbenchCapacity.cs`
  - `engine/MachinePart.cs`
  - `engine/PartId.cs`
  - `CuriousContraptions.Simulation/wwwroot/worker.js`
- Diagnostic Remediation: Adversarial check flagged two `AP-003: Explicit any type usage` warnings in `tools/e2e/cat-020.test.ts` at line 89. Replaced with explicit `Worker` and `ConsoleMessage` types from `playwright`.
- Result:
  ```json
  {"warnings":[], "summary":{"total":0,"errors":0,"warnings":0,"info":0,"suppressed":0}}
  ```
- Verdict: **0 errors, 0 warnings**.

### 3.2 Native C# Unit Tests (`dotnet test CuriousContraptions.slnx`)
- Diagnostic Remediation: Adversarial inspection revealed `CounterTests.cs` was omitted from `CuriousContraptions.tests.csproj` compilation items and contained outdated signatures. Added `<Compile Include="CounterTests.cs" />` and updated tests to match modern Tri-Graph APIs (`WorkshopInput.Generic`, `WorkshopCadenceSettings`, `WorkshopWire.DecodeCommand`, `WorkbenchCapacity`).
- Command: `dotnet test CuriousContraptions.slnx`
- Output:
  ```
  Test run for CuriousContraptions.tests.dll (.NETCoreApp,Version=v10.0)
  Passed!  - Failed: 0, Passed: 932, Skipped: 0, Total: 932, Duration: 2 s
  ```
- Result: **100% pass rate (932/932 tests passing, +11 tests from Story 9.1's 921)**.
- Specific unit test suite [`CuriousContraptions.tests/CounterTests.cs`](file:///Users/aidan/dev/personal/tim/CuriousContraptions.tests/CounterTests.cs) covers:
  1. `PartRegistry_DiscoversCounterDefinitionAndConfiguresInstance`
  2. `CounterScene_ExtractsExactTwoCollidersAndPortLocators`
  3. `CanonicalColliders_MatchSceneExtractedColliders`
  4. `Ports_DeclareExactSocketsAndDirections`
  5. `WorkshopCounter_Validation` (admits [1, 9], rejects $\le 0$, $\ge 10$)
  6. `Connections_AcceptsValidActivationAndElectricalLinks` (accepts valid, rejects cross-domain)
  7. `PhysicsCompiler_CompilesCollidersAndCounterPower` (verifies blade drag unpowered vs powered)
  8. `ActivationNetwork_CountingProgression_SingleEmission_AndSaturation` (3-step progression, single emission, latching, 20 extra saturated steps)
  9. `WorkshopWire_SerializesAndDeserializesCounter`
  10. `WorkshopWireFlatBuffers_SerializesAndDeserializesCounter`
  11. `WorkbenchCapacity_ValidatesCounterFootprint`

---

## 4. Real Browser Playwright E2E Verification

All browser suites executed serially in real Google Chrome via the Playwright connector on port 8060 against the live Godot WebAssembly build:

### 4.1 Dedicated Suite: CAT-020 Pulse Counter (`tools/e2e/cat-020.test.ts`)
- Command: `npx tsx --test tools/e2e/cat-020.test.ts`
- Tests:
  1. `1. Placement and wiring validation: Counter (target 3), Powered Gate (tilt -15 deg), Ball Detector, Upstream Basketball inside mouth, Battery, Lamp` — **PASS**
  2. `2. Negative control: 2 ball crossings do not emit, gate closed, lamp unlit, upstream ball stopped` — **PASS**
  3. `3. Positive trigger: 3rd ball crossing reaches target count 3, illuminates lamp, retracts gate blade, and ball traverses downstream` — **PASS**
  4. `4. Saturation: 4th ball crossing does not glitch or re-trigger, saturated state maintains gate open` — **PASS**
  5. `5. State persistence & reset: Save / reload / Load preserves counter, gate, detector, lamp, and wiring; simulation replay triggers traversal` — **PASS**
- Suite Duration: 54.1 s
- Pass Rate: **5/5 (100%)**

### 4.2 Cumulative Regression Suites
Executed sequentially with zero concurrency to prevent WebGPU/SharedArrayBuffer harness collisions:

| Suite | File | Tests Passed / Total | Duration | Verdict |
| :--- | :--- | :---: | :---: | :---: |
| **Story 9.1** | `tools/e2e/cat-052.test.ts` (Pressure Plate Surface Contact Load Sensor) | 5 / 5 | 29.1 s | **PASS** |
| **Story 8.2** | `tools/e2e/cat-051.test.ts` (Powered Gate Passageway Barrier) | 5 / 5 | 26.3 s | **PASS** |
| **Story 6.8** | `tools/e2e/cat-002.test.ts` (Ball Detector Passage Sensor) | 5 / 5 | 59.4 s | **PASS** |

**Total Cumulative E2E Pass Rate:** **20 / 20 tests passing (100% pass rate, 0 failures, 0 console errors).**

---

## 5. Architectural Invariants & Non-Functional Audit

1. **Pure Declarative Tri-Graph Architecture:**
   - Catalog resource: `res://parts/catalog/counter.tres`.
   - Structural scene: `res://parts/scenes/counter.tscn`.
   - Root node uses generic `MachinePart.cs` without ad-hoc C# part scripts attached to the node or scene.
   - Closed enums throughout: `PartId.Counter` (numeric 22), `ActivationNodeKind.Counter`, `ActivationOccurrenceKind.CounterReached`.

2. **Dual-Domain Separation & Validation:**
   - Clearly separates Activation domain (`ActivationIn`, `ActivationOut`) from Electrical domain (`PowerIn`, `Supply`).
   - Domain validation in [`WorkshopConnections.cs`](file:///Users/aidan/dev/personal/tim/engine/gpu/WorkshopConnections.cs) strictly forbids cross-domain connections (e.g. wiring activation source into `PowerIn` throws `ArgumentException`).

3. **Event Filtering & Saturation Invariance:**
   - Timestamp-based edge detection prevents continuous contact from repeatedly incrementing the counter.
   - Saturated state prevents multiple activations from firing while holding the electrical contact latched.

4. **Lifecycle & Determinism:**
   - Simulation Reset clears counter accumulated events to 0, resets latches, and restores visual dot indicators to Slate.
   - Save/Load roundtrip preserves target count and complete circuit topology across IndexedDB FlatBuffers serialization.

---

## 6. Terminal Verdict

**Pass** — Story 9.2 (Pulse Counter Event Accumulator, CAT-020) satisfies all acceptance criteria, architectural invariants, static analysis rules, unit tests, and real browser E2E regression suites.
