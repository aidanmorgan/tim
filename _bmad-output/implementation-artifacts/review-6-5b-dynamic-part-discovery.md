# Independent Adversarial Review: Story 6.5b: Dynamic Puzzle Part Discovery & Extensible PartId Architecture

**Reviewer:** Murdoch, Independent Adversarial Reviewer (Curious Contraptions)  
**Mandate:** Strict independent review policy (REQ-14: Self-review never qualifies)  
**Date:** 2026-10-13  
**Target:** Story 6.5b (`spec-6-5b-dynamic-part-discovery.md` / `epics.md` Story 6.5b)  
**Baseline Commit:** `9107148592a934ff980b8f6201318968b8df35ea`  

---

## 1. Executive Summary & Terminal Scoped Verdict

### **Verdict: PASS**

All acceptance criteria set forth in `_bmad-output/implementation-artifacts/spec-6-5b-dynamic-part-discovery.md` and Epic 6 Story 6.5b are fully satisfied with verifiable, reproducible evidence:
1. Dynamic catalog discovery via `DirAccess.GetFilesAt("res://parts/catalog")` completely supersedes hardcoded file path arrays with zero fallback loaders or legacy shims.
2. Extensible `PartId` record struct wraps canonical identities and decouples part definition, registry, creation, and Free inventory from closed enum definitions.
3. Zero backwards compatibility shims, dual registration paths, or fallback adapters were introduced.
4. Static analysis via Anvil reports **0 warnings and 0 errors**.
5. The full native test suite passes at **100% (801/801 tests passed, 0 failures, 0 skipped)** via `dotnet test CuriousContraptions.slnx`.
6. Playwright browser E2E test suites pass at **100% serially in real Chrome** (`cat-014.test.ts`, `engine-core-2a1.test.ts`, `engine-core-2c.test.ts`), confirming exact authoring Reset restoration and Save/Load persistence roundtrip.

---

## 2. Adversarial Review Checklist

| # | Inspection Area | Requirement | Result | Evidence |
|---|---|---|---|---|
| 1 | **Dynamic Discovery** | `PartRegistry.Discover()` scans `res://parts/catalog/*.tres` dynamically via `DirAccess.GetFilesAt` | **PASS** | Evaluates directory entries; sorts ordinally; discovers 12 canonical parts; excludes unmigrated legacy resources (`balloon`, `tennis`). |
| 2 | **Extensible Identity** | `PartId` strongly typed record struct wrapping canonical strings | **PASS** | `engine/PartId.cs` implements `PartId` with non-whitespace validation, `IComparable<PartId>`, and canonical constants. |
| 3 | **Decoupled Creation** | `PartRegistry.Create(PartId)` instantiates from `definition.Scene` without switch statements | **PASS** | Lookups resolve directly against `_definitions[id].Scene.Instantiate<MachinePart>()` and invoke `part.Configure(definition)`. |
| 4 | **Dynamic Free Inventory** | `WorkshopInventoryPolicy.Free` populates dynamically from discovered definitions | **PASS** | `GenerateFree` keys `PartInventory` by `PartId` with `PartAllowance.Unlimited`. Empty registry throws `ArgumentException`. |
| 5 | **Zero Legacy Remnants & Shims** | Zero backwards compatibility shims, zero fallback loaders, zero hardcoded path tables | **PASS** | 60 lines of hardcoded path loaders removed from `PartRegistry.cs`. No fallback paths or dual registration mechanisms. |
| 6 | **Static Analysis** | `anvil check` outputs 0 warnings on changed files | **PASS** | 0 warnings, 0 errors across all 16 changed source and test files. |
| 7 | **Automated Unit Tests** | `dotnet test CuriousContraptions.slnx` 100% pass | **PASS** | 801/801 passed, 0 failed, 0 skipped in 1s. |
| 8 | **Browser E2E Execution** | Pure TypeScript Playwright E2E suites pass serially in real Chrome | **PASS** | `cat-014.test.ts` (4/4 passed), `engine-core-2a1.test.ts` (5/5 passed), `engine-core-2c.test.ts` (2/2 passed). Total: 11/11 passed (100%). |
| 9 | **Lifecycle Invariants** | Reset restoration and Save/Load roundtrip verified in Chrome | **PASS** | Verified in `cat-014.test.ts` (case 4) and `engine-core-2a1.test.ts` (cases 2, 3, 5). |

---

## 3. Detailed Acceptance Criteria Matrix

### Criteria from `spec-6-5b-dynamic-part-discovery.md` and `epics.md`:

| Criterion | Expected Behavior | Observed Behavior | Status |
|---|---|---|---|
| **AC-1: Dynamic Discovery** | `PartRegistry.Discover()` dynamically inspects `res://parts/catalog/` via `DirAccess.GetFilesAt` and registers all valid `.tres` files without hardcoded file lists. | `DirAccess.GetFilesAt("res://parts/catalog")` scans files; `.remap` suffixes stripped; non-`.tres` ignored; legacy parameter entries ignored; loads all 12 active canonical parts dynamically. | **MET** |
| **AC-2: Extensible PartId** | Parts identified by strongly typed extensible `PartId` record struct wrapping canonical strings. | `engine/PartId.cs` introduced; wraps `string Value`; provides typed comparisons, validation, and equality. | **MET** |
| **AC-3: Closed Enum Elimination in Active Paths** | Closed enum `WorkshopPartKind` eliminated from active part registration, creation, inventory, and machine parts. | `PartRegistry.Definitions` keyed by `PartId`; `Create(PartId)` accepts `PartId`; `PartInventory` keyed by `PartId`; `MachinePart.PartId` added. | **MET** |
| **AC-4: Extensible Factory** | `PartRegistry.Create(PartId)` instantiates from `definition.Scene` and calls `Configure(definition)` without switch statements. | `definition.Scene.Instantiate<MachinePart>()` followed by `part.Configure(definition)`. Switch statement eliminated. | **MET** |
| **AC-5: Dynamic Free Inventory** | Free Workshop palette populates dynamically from discovered `PartRegistry.Definitions` with unlimited allowances. | `WorkshopInventoryPolicy.GenerateFree` dynamically builds `PartInventory` from registry keys with `PartAllowance.Unlimited`. | **MET** |
| **AC-6: Native Solution Tests** | Full native test suite passes with zero warnings (`dotnet test CuriousContraptions.slnx`). | 801/801 tests passed, 0 failed, 0 skipped. | **MET** |
| **AC-7: Chrome Playwright E2E** | Existing cumulative Chrome Playwright E2E suites pass 100% serially in real Chrome. | All specified suites (`cat-014`, `engine-core-2a1`, `engine-core-2c`) executed serially against live Chrome on port 8060 and passed 100%. | **MET** |

---

## 4. Verification Evidence & Raw Command Outputs

### 4.1 Static Analysis: Anvil Anti-Pattern & Policy Scan
```sh
$ anvil check engine/PartId.cs engine/PartDefinition.cs engine/PartRegistry.cs engine/MachinePart.cs engine/MachineWorld.Gpu.cs engine/gpu/WorkshopInventory.cs ui/Workshop.cs ui/WorkshopGuidance.cs CuriousContraptions.tests/PartRegistryTests.cs CuriousContraptions.tests/BasketballResourceTests.cs CuriousContraptions.tests/WorkbenchCapacityTests.cs CuriousContraptions.tests/WorkshopBowlingTests.cs CuriousContraptions.tests/WorkshopBumperTests.cs CuriousContraptions.tests/WorkshopDominoTests.cs CuriousContraptions.tests/WorkshopSpringboardTests.cs CuriousContraptions.tests/WorkshopWallTests.cs
Last run written to .anvil/last-check.txt

  ✓ No warnings found
```
**Result:** 0 warnings, 0 errors.

### 4.2 Unit Tests: .NET Solution Suite
```sh
$ dotnet test CuriousContraptions.slnx
Test run for /Users/aidan/dev/personal/tim/CuriousContraptions.tests/bin/Debug/net10.0/CuriousContraptions.tests.dll (.NETCoreApp,Version=v10.0)
A total of 1 test files matched the specified pattern.

Passed!  - Failed:     0, Passed:   801, Skipped:     0, Total:   801, Duration: 1 s - CuriousContraptions.tests.dll (net10.0)
```
**Result:** 801/801 passed (100%), 0 failures.

### 4.3 Browser E2E: Real Chrome Verification via Playwright

#### Suite 1: `cat-014.test.ts` (Bowling Ball dynamic sphere by material declaration)
```sh
$ npx tsx --test tools/e2e/cat-014.test.ts
▶ CAT-014: Bowling ball dynamic sphere by material declaration
  ✔ 1. Two lanes in one Run: the Bowling ball topples its Domino past 60 deg, the identically released Basketball leaves its Domino below 20 deg, and both balls come to rest (10852.850375ms)
  ✔ 2. Dropped from the placement plane the Bowling ball barely rebounds and rests at its 0.28 m radius; the Basketball rebounds high; Reset replays both worlds bit-for-bit (8515.858167ms)
  ✔ 3. A Bowling ball dropped into a placed Receiver is captured and the declared capture halo animates (4149.855125ms)
  ✔ 4. Save / reload / Load restores both kinds; after Reset the second Run topples the Bowling lane again and leaves the Basketball lane standing (17614.993875ms)
✔ CAT-014: Bowling ball dynamic sphere by material declaration (45618.318417ms)
ℹ tests 4
ℹ suites 1
ℹ pass 4
ℹ fail 0
ℹ cancelled 0
ℹ skipped 0
ℹ todo 0
ℹ duration_ms 45883.403875
```
**Result:** 4/4 passed (100%). Exact Reset and Save/Reload/Load persistence confirmed.

#### Suite 2: `engine-core-2a1.test.ts` (Dual-Sphere Simulation)
```sh
$ npx tsx --test tools/e2e/engine-core-2a1.test.ts
▶ ENGINE-CORE-2a1: Dual-Sphere Simulation and Pure TypeScript Playwright E2E
  ✔ 1. Clean boot: cross-origin isolation and WorkshopPoseRing initialized (3.413959ms)
  ✔ 2. Empty-construction control: Run advances sequence with 0 bodies, Reset restores Build Mode (1163.370625ms)
  ✔ 3. Single-ball control: Place 1 Basketball via UI, Run observes gravitational motion, Reset restores elevation (2632.219542ms)
  ✔ 4. Dual-sphere multi-body simulation: Place 2 Basketballs, Run observes independent trajectories (2799.836125ms)
  ✔ 5. Save & Load persistence roundtrip: Save via driver, reload page, Load restores both balls (4116.767375ms)
✔ ENGINE-CORE-2a1: Dual-Sphere Simulation and Pure TypeScript Playwright E2E (15646.000083ms)
ℹ tests 5
ℹ suites 1
ℹ pass 5
ℹ fail 0
ℹ cancelled 0
ℹ skipped 0
ℹ todo 0
ℹ duration_ms 15930.01475
```
**Result:** 5/5 passed (100%). Empty construction, placement, trajectory observation, and Save/Load persistence confirmed.

#### Suite 3: `engine-core-2c.test.ts` (Endpoint-Sampled Sensors & Dwell Tick Counter)
```sh
$ npx tsx --test tools/e2e/engine-core-2c.test.ts
▶ ENGINE-CORE-2c: Endpoint-Sampled Sensors & Dwell Tick Counter (Decoupled Sensor Evaluation)
  ✔ 1. Qualified Receiver Capture with Dwell at <= 1.5 m/s: ball settles in Receiver and triggers capture with visual halo (5037.531833ms)
  ✔ 2. Fast Through-Pass Rejection (> 1.5 m/s): high-speed transit through receiver region without dwell does NOT trigger capture (7261.697292ms)
✔ ENGINE-CORE-2c: Endpoint-Sampled Sensors & Dwell Tick Counter (Decoupled Sensor Evaluation) (16440.292709ms)
ℹ tests 2
ℹ suites 1
ℹ pass 2
ℹ fail 0
ℹ cancelled 0
ℹ skipped 0
ℹ todo 0
ℹ duration_ms 16726.232958
```
**Result:** 2/2 passed (100%). Qualified receiver capture and high-speed transit rejection verified.

---

## 5. Architectural Health & Legacy Remnant Analysis

1. **No Fallback Loaders:** The dynamic scanner in `PartRegistry.Discover()` strictly loads valid `.tres` files and throws `ArgumentException` on invalid or malformed resources. If no valid parts are discovered, it immediately raises `ArgumentException("No valid catalogue parts were discovered.")`.
2. **No Dual Registration Paths:** All parts are registered exclusively through `_definitions.Add(id, definition)` keyed by `PartId`. The former `WorkshopPartKind`-keyed dictionary is completely eliminated.
3. **No Switch Statements in Creation:** `PartRegistry.Create(PartId id)` looks up `_definitions[id]`, invokes `definition.Scene.Instantiate<MachinePart>()`, and runs `part.Configure(definition)` generically.
4. **Clean Boundary Between Slices:** Story 6.5b decouples part discovery and inventory from the closed enum. The full deprecation of `WorkshopPartKind` across remaining legacy physics compilations is scoped to Stories 6.5c-6.5f, preserving stable operation while fulfilling all Story 6.5b requirements without introducing any backwards compatibility shims.

---

## 6. Conclusion

Story 6.5b meets every acceptance criterion without defects, regressions, or legacy remnants. Terminal Scoped Verdict: **PASS**.
