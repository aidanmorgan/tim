# Adversarial Review Report: Story 8.1 — Battery DC Power Source & Network Graph (CAT-005) & Schema-Driven Binary Wire Transfer (FlatBuffers)

**Reviewer:** Murdoch (`bmad_reviewer`)  
**Mandate:** Independent Adversarial Review (REQ-14)  
**Story Under Review:** Story 8.1 (`spec-8-1-battery.md`)  
**Date:** 2026-10-16  

---

## 1. Executive Summary & Verdict

- **Verdict:** **Pass**
- **Scope:** Story 8.1: Battery DC Power Source & Network Graph (CAT-005) & Schema-Driven Binary Wire Transfer (FlatBuffers).
- **Core Outcome:** All manual byte offsets and byte-counting guesswork across wire response structures (`26384`, `50192`, `3584`, `4864`, etc.) have been completely eradicated and replaced by Google FlatBuffers (`schemas/workshop_wire.fbs`). The battery element exhibits strict entity separation between visual components and invisible colliders, exact finite supply economics, and bit-exact Save/Load and Reset reproduction. 100% of unit tests pass, pre-commit checks report zero warnings, and dedicated plus cumulative Playwright browser test suites pass serially in real Chrome with zero failures.

---

## 2. Invariant & Requirement Verification

### 2.1 Schema-Driven Binary Wire Transfer (FlatBuffers)
- **Schema Authority:** Authored at `schemas/workshop_wire.fbs` defining all wire records: `SimulationPhase`, `CommandKind`, `CommandOutcome`, `ResponseKind`, `ElectricalEnable`, `WorkEffect`, `Vec3f`, `Vec3i`, `Quat4f`, `BodyRead`, `CaptureLatchRead`, `ElectricalSourceRecord`, `ContactWorkStoreRecord`, `ContactWorkOccurrenceRecord`, `ActivationRecord`, `TimerRecord`, `ClockStamp`, `ElectricalControl`, `WorkshopResponse`, `WorkshopCommand`.
- **C# Implementation:** `engine/gpu/WorkshopWireGenerated.cs` and `engine/gpu/WorkshopWireFlatBuffers.cs` encode and decode FlatBuffers binary packets.
- **Diagnostics Wire Protocol:** Both `engine/MachineWorld.Gpu.cs` (`CCGPU <base64>`) and `CuriousContraptions.Simulation/Program.cs` (`CCGPU_RETAINED <base64>`) emit schema-encoded buffers.
- **TypeScript Consumer:** `tools/e2e/workshop-wire.ts` decodes responses via `decodeWorkshopResponse` and provides mock helpers via `encodeWorkshopResponse`.
- **Zero Manual Byte Offsets:** Grep verification across `tools/e2e/cat-005.test.ts` and `tools/e2e/workshop-driver.ts` confirms complete elimination of magic wire offsets (`26384`, `50192`, `3584`, `4864`). `decodeAcknowledgement` and test assert methods access strongly-typed generated accessors.

### 2.2 Finite DC Energy & Network Graph Model
- **Catalog Resource:** `parts/catalog/battery.tres` defines standard battery identity `battery`, category `Power`, color `#de7059`.
- **Energy Budget:** Authoritative default capacity 3,600 J, maximum power output 120 W, initial fraction 1.0, typed `ElectricalEnable` enum.
- **Finite Discharge & Recharge:** Verified finite 60 J exhaustion, unspent energy retention across pause/resume and step, and funded repeated heavy impacts across single and multiple consumers (fanout to multiple Bumper contact work stores).
- **Control Invariants:** Running and paused enable/disable toggle commands commit without corrupting unspent store balances.

### 2.3 Strict Entity Separation
- **Scene Definition:** `parts/scenes/battery.tscn` separates presentation from physics:
  - Invisible collision shape `col_box` (`BoxShape3D`, `visible = false`).
  - Output port locator `Loc_Port_out` positioned at $(0, 0.62, 0)$ representing `WorkshopSocket.Supply` in `WorkshopConnectionDomain.Electrical`.
  - Presentation geometry cleanly organized under `Visual/` (`Visual_Body`, `Visual_Stripe`, `Terminal`, `Mark_0..3`, `Visual_PlusH/V`).

### 2.4 Zero Legacy Remnants
- **Search for Superseded Assets:**
  - `PartArt.cs` search across code trees: 0 active occurrences (completely purged).
  - `WorkshopPartKind` search across code trees: 0 active occurrences (completely purged).
  - Legacy dimension resources (`ContactWorkResource`, `DelayDurationResource`, `ElectricalSourceResource`, `RampDimensionsResource`, `SpringboardResource`, `WallDimensionsResource`, `ReceiverGeometry`): Deleted.
  - Zero backwards-compatibility shims or fallback loaders introduced.

---

## 3. Empirical Test & Verification Results

### 3.1 Pre-Commit Static Analysis (`anvil check`)
- Command: `anvil check --changed`
- Target files: `tools/e2e/cat-005.test.ts`, `tools/e2e/workshop-driver.ts`, `tools/e2e/workshop-wire.ts`
- Result: **0 errors, 0 warnings** (checked 4 changed files).

### 3.2 Automated Unit & Physics Compiler Tests
- Command: `dotnet test CuriousContraptions.slnx`
- Result: **Passed: 888, Failed: 0, Skipped: 0, Total: 888** (100% pass rate).

### 3.3 Chrome Playwright E2E Verification
Executed serially in real Google Chrome against preview server `http://127.0.0.1:8060/`:

1. **Dedicated Story Suite (`tools/e2e/cat-005.test.ts`):**
   - `Battery recharge, disable, Save/Load and Reset`: **PASS** (25,238 ms)
   - `Battery finite60J source exhausts and retains final credit`: **PASS** (18,150 ms)
   - `Battery fans one finite source out to two existing Bumper stores`: **PASS** (19,313 ms)
   - `Battery disconnected authored settings and rendered charge thresholds restore exactly`: **PASS** (51,295 ms)
   - `Battery paused intent reverses and Running controls preserve stores across continued ticks`: **PASS** (12,765 ms)
   - `Battery electrical animation survives same-world cadence downshift through existing protocol boundary`: **PASS** (6,184 ms)
   - `Battery accounting deduplicates exact commits and rejects mutated or unfunded credits`: **PASS** (1.8 ms)
   - **Total:** **7/7 passed** (0 failed, duration: 133,178 ms).

2. **Cumulative Vertical Slice Regression Suites:**
   - `cat-030.test.ts` (Funnel Hollow Frustum Inlet to Bore): **7/7 passed** (100%, 83,538 ms).
   - `cat-050.test.ts` (Pipe Bend 90 Torus & Level Quarter Bend): **5/5 passed** (100%, 22,045 ms).
   - `cat-049.test.ts` (Pipe Bend 45 Torus & Level Gentle Bend): **5/5 passed** (100%, 20,501 ms).
   - `cat-048b.test.ts` (Clear Pipe Level): **5/5 passed** (100%, 20,781 ms).
   - `cat-048a.test.ts` (Straight Pipe Compound Cylinder): **4/4 passed** (100%, 33,799 ms).
   - `cat-002.test.ts` (Ball Detector Passage Sensor): **5/5 passed** (100%, 51,105 ms).

---

## 4. Final Verdict

**Pass** — Story 8.1 satisfies all functional, architectural, wire protocol, and regression requirements.
