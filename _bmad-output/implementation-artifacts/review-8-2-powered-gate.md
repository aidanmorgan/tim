# Adversarial Review Report: Story 8.2 — Powered Gate Passageway Barrier (CAT-051)

**Reviewer:** Murdoch (`bmad_reviewer`)  
**Mandate:** Independent Adversarial Review (REQ-14)  
**Story Under Review:** Story 8.2 (`spec-8-2-powered-gate.md`)  
**Date:** 2026-10-17  

---

## 1. Executive Summary & Verdict

- **Verdict:** **Pass**
- **Scope:** Story 8.2: Powered Gate Passageway Barrier (CAT-051) — The first electrical consumer in Curious Contraptions.
- **Core Outcome:** The Powered Gate (`powered_gate`) has been implemented as a physical sliding gold blade barrier across a passageway tube that retracts along local $+Y$ (stroke 1.55 m) under electrical power and returns closed via spring when disconnected. The element adheres strictly to clean entity separation (presentation meshes vs invisible physics colliders), declarative scene definitions, typed connection domain validation (Electrical only, Activation rejected), exact Save/Load persistence roundtrip, and bit-for-bit Reset lifecycle invariance. All 906 unit tests pass, pre-commit static analysis reports zero warnings, and the dedicated E2E suite plus seven cumulative regression E2E suites pass 100% in real Google Chrome on port 8060.

---

## 2. Criterion & Invariant Matrix

| Requirement / Invariant | Status | Verification Evidence |
| :--- | :--- | :--- |
| **Catalog Resource Definition** | **PASS** | [`parts/catalog/powered_gate.tres`](file:///Users/aidan/dev/personal/tim/parts/catalog/powered_gate.tres): `Id = "powered_gate"`, `Title = "Powered gate"`, `Category = "Motion"`, `Color = Color(0.97, 0.796, 0.322, 1)` (#f7cb52), `Scene = ExtResource("2")`. Discovered dynamically by `PartRegistry.Discover()`. |
| **Clean Entity Separation** | **PASS** | [`parts/scenes/powered_gate.tscn`](file:///Users/aidan/dev/personal/tim/parts/scenes/powered_gate.tscn): Presentation geometry grouped under `Visual/` (`Visual_Sleeve`, `Visual_Collar_Start/End`, `Visual_Housing`, `Visual_Rail_Start/End`, `Blade/Visual_Blade`, `Visual_Socket`, `Visual_Indicator`). All 19 frame collision shapes (`col_box_0..15`, `col_housing`, `col_rail_start/end`) have `visible = false`. |
| **Port Locators & Snapping** | **PASS** | Port locators in scene: `Loc_Port_power_in` at $(0, 1.65, 0.92)$, `Loc_Port_mouth_start` at $(-0.49, 0, 0)$, `Loc_Port_mouth_end` at $(0.49, 0, 0)$. Ports extracted by `GltfPhysicsBodyExtractor` and matched by `WorkshopPorts.LocalPosition`. |
| **Prismatic Slider Joint & Blade Physics** | **PASS** | `WorkshopPhysicsCompiler`: Dynamic blade body `GateBlade(owner)` with mass 1 kg, material `(0.1, 0.1, 0.3)`. Prismatic joint along local $+Y$ with limits $[0, 1.55]\text{ m}$, stiffness 14 N/m, damping 4 N·s/m, `ConnectedCollision.Disabled`. |
| **Electrical Actuator Integration** | **PASS** | Gate declares single input port `WorkshopSocket.PowerIn` in `WorkshopConnectionDomain.Electrical`. Connecting to Battery `Supply` is accepted; connecting Activation sources (Switch, Domino, Delay, etc.) is rejected. Blade linear drag is set to 0.0625 when wired to an enabled battery, retracting the shutter. Gate is excluded from contact work stores. |
| **Capacity & Workbench Footprint** | **PASS** | `WorkbenchFootprint.Of(PartId.PoweredGate)` declares 2 bodies, 1 dynamic body, 20 colliders, 0 triggers, 0 contact works, 0 activation nodes, 0 electrical sources, 1 prismatic constraint. Accommodated within compiled physics scene limits. |
| **Zero Legacy Remnants** | **PASS** | Grep verification confirms 0 active code occurrences of `WorkshopPartKind`, 0 active occurrences of `PartArt`, 0 occurrences of obsolete WGSL shaders (`physics.wgsl` only in historical archives), 0 Baumgarte velocity clamps or shims. |
| **Save/Load & Reset Invariance** | **PASS** | IndexedDB FlatBuffers save/reload/load roundtrips bit-identically across Chrome sessions. Reset restores gate blade and rolling balls to exact baseline poses. |

---

## 3. Automated Test Verification Results

### 3.1 Static Analysis (`anvil check`)
- Command: `anvil check --changed`
- Output:
  ```
  Last run written to .anvil/last-check.txt
  Checked 10 changed file(s) (extensions: .ts, .tsx, .js, .jsx, .mjs, .cjs, .rs, .py, .html, .htm, .css, .scss, .less)
  ✓ No warnings found
  ```
- Result: **0 errors, 0 warnings**.

### 3.2 Native C# Unit Tests (`dotnet test CuriousContraptions.slnx`)
- Command: `dotnet test CuriousContraptions.slnx`
- Output:
  ```
  Test run for CuriousContraptions.tests.dll (.NETCoreApp,Version=v10.0)
  Passed!  - Failed: 0, Passed: 906, Skipped: 0, Total: 906, Duration: 2 s
  ```
- Result: **100% pass rate (906/906 tests passing)**.
- Includes all 6 test cases in [`CuriousContraptions.tests/PoweredGateTests.cs`](file:///Users/aidan/dev/personal/tim/CuriousContraptions.tests/PoweredGateTests.cs):
  1. `PartRegistry_DiscoversPoweredGateDefinitionAndConfiguresInstance`
  2. `PoweredGateScene_ExtractsExact19CollidersAndThreePortLocators`
  3. `CanonicalColliders_MatchSceneExtractedColliders`
  4. `Ports_DeclareSinglePowerInInput`
  5. `Connections_AcceptsElectricalAndRejectsActivation`
  6. `PhysicsCompiler_CompilesGateWithDynamicBladeAndPrismaticJoint`
  7. `PhysicsCompiler_WiredToBattery_SetsBladeLinearDragAndExcludesFromElectricalBindings`
  8. `WorkbenchCapacity_ValidatesPoweredGateFootprint`

---

## 4. Real Browser Playwright E2E Verification

All browser suites executed serially in real Google Chrome via the Playwright connector on port 8060 against live Godot WebAssembly build:

### 4.1 Dedicated Suite: CAT-051 Powered Gate (`tools/e2e/cat-051.test.ts`)
- Command: `npx tsx --test tools/e2e/cat-051.test.ts`
- Tests:
  1. `1. Free workshop placement: place powered gate, tilt -15 deg, and place basketball upstream` — **PASS**
  2. `2. Negative control: unpowered gate blocks rolling ball upstream, blade remains closed at y = 0` — **PASS**
  3. `3. Powered run: wire battery Supply to gate PowerIn; actuator retracts blade to 1.55 m, ball rolls through` — **PASS**
  4. `4. Disconnecting battery closes gate: blade returns closed via spring, blocking rolling ball again` — **PASS**
  5. `5. State persistence: Save / reload / Load preserves gate, battery and electrical link bit-identically` — **PASS**
- Suite Duration: 25.9 s
- Pass Rate: **5/5 (100%)**

### 4.2 Cumulative Regression Suites
Executed sequentially with zero concurrency to prevent WebGPU/SharedArrayBuffer harness collisions:

| Suite | File | Tests Passed / Total | Duration | Verdict |
| :--- | :--- | :---: | :---: | :---: |
| **Story 8.1** | `tools/e2e/cat-005.test.ts` (Battery DC Source & Network Graph) | 7 / 7 | 143.9 s | **PASS** |
| **Story 6.10** | `tools/e2e/cat-050.test.ts` (90° Pipe Bend & Level Quarter Bend) | 5 / 5 | 23.8 s | **PASS** |
| **Story 6.9** | `tools/e2e/cat-049.test.ts` (45° Pipe Bend & Level Gentle Bend) | 5 / 5 | 21.7 s | **PASS** |
| **Story 6.7** | `tools/e2e/cat-048b.test.ts` (Clear Pipe Level) | 5 / 5 | 22.0 s | **PASS** |
| **Story 6.6** | `tools/e2e/cat-048a.test.ts` (Straight Pipe Compound Cylinder) | 4 / 4 | 36.3 s | **PASS** |
| **Story 6.8** | `tools/e2e/cat-002.test.ts` (Ball Detector Passage Sensor) | 5 / 5 | 58.7 s | **PASS** |
| **Story 6.11**| `tools/e2e/cat-030.test.ts` (Funnel Hollow Frustum Inlet to Bore) | 7 / 7 | 95.4 s | **PASS** |

**Total E2E Verification Across All Suites:** **43 / 43 tests passing (100% pass rate, 0 failures, 0 console errors).**

---

## 5. Architectural Invariants & Non-Functional Audit

1. **Strictly Declarative Architecture:**
   - Powered Gate is declared as `res://parts/catalog/powered_gate.tres` and `res://parts/scenes/powered_gate.tscn`.
   - Root node is generic `MachinePart` without custom C# script attached to the node or scene.
   - Dynamic colliders and prismatic joint parameters are constructed deterministically by `WorkshopPhysicsCompiler`.

2. **Typed Connection Domains:**
   - `WorkshopPorts.For(PartId.PoweredGate)` exposes only `WorkshopSocket.PowerIn` with domain `WorkshopConnectionDomain.Electrical`.
   - Attempting to route activation pulses from switches or delays throws `ArgumentException` at validation boundary.
   - Saves store connections via FlatBuffers schema tables (`connections` array).

3. **Lifecycle & Determinism:**
   - Reset cleanly restores dynamic blade position to baseline ($y = 0$).
   - IndexedDB FlatBuffers payload preserves the exact construction and electrical topology across hard browser reloads.

---

## 6. Terminal Verdict

**Pass** — Story 8.2 (Powered Gate Passageway Barrier, CAT-051) fully satisfies all requirements, physics constraints, electrical network invariants, static analysis gates, unit tests, and real browser E2E regression suites.
