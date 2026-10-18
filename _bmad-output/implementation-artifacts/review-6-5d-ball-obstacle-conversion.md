# Independent Adversarial Review: Story 6.5d: Existing Implementation Conversion — Core Ball & Obstacle Primitives (CAT-001, CAT-014, CAT-064, CAT-003, CAT-066, CAT-054, CAT-004)

**Reviewer:** Murdoch, Independent Adversarial Reviewer (Curious Contraptions)  
**Mandate:** Strict independent review policy (REQ-14: Self-review never qualifies)  
**Date:** 2026-10-13  
**Target:** Story 6.5d (`spec-6-5d-ball-obstacle-conversion.md` / `epics.md` Story 6.5d)  
**Baseline Commit:** `9107148592a934ff980b8f6201318968b8df35ea`  

---

## 1. Executive Summary & Terminal Scoped Verdict

### **Verdict: PASS**

All acceptance criteria set forth in `_bmad-output/implementation-artifacts/spec-6-5d-ball-obstacle-conversion.md`, Epic 6 Story 6.5d, and architectural decisions AD-05, AD-06, and AD-11 are fully satisfied with verifiable, reproducible evidence:
1. Complete deletion of all 7 obsolete per-part C# classes and dimension resources: `parts/BallPart.cs`, `parts/BasketPart.cs`, `parts/RampPart.cs`, `parts/WallPart.cs`, `engine/WallDimensionsResource.cs`, `engine/RampDimensionsResource.cs`, and `engine/gpu/ReceiverGeometry.cs`. Codebase grep confirms strictly zero active occurrences, zero fallback loaders, and zero compatibility bridge shims.
2. **Strict Entity Separation Verified:** In all 5 converted element scenes (`ball.tscn`, `bowling.tscn`, `wall.tscn`, `ramp.tscn`, `basket.tscn`), visual mesh entities (`Visual_*`) and dedicated collider proxy entities (`col_*`, with `visible = false`) are distinct, segregated nodes. Presentation nodes are never used for collision extraction.
3. Generic `MachinePart` and `PartDefinition`: Elements are instantiated via generic `MachinePart`. Dimensions (`ApplyWallDimensions`, `ApplyRampDimensions`) and receiver halo albedo bindings (`BindVisual`) operate generically through `MachinePart` and `PartDefinition` without bespoke C# element classes.
4. Static analysis via Anvil reports **0 warnings and 0 errors**.
5. The full native test suite passes at **100% (827/827 tests passed, 0 failures, 0 skipped)** via `dotnet test CuriousContraptions.slnx`.
6. Pure TypeScript Playwright browser E2E test suites pass at **100% serially in real Chrome** (`cat-014.test.ts`, `engine-core-2a1.test.ts`, `engine-core-2c.test.ts`), confirming exact authoring Reset restoration and Save/Load persistence roundtrip.

---

## 2. Adversarial Review Checklist

| # | Inspection Area | Requirement | Result | Evidence |
|---|---|---|---|---|
| 1 | **Complete File Deletions** | `BallPart.cs`, `BasketPart.cs`, `RampPart.cs`, `WallPart.cs`, `WallDimensionsResource.cs`, `RampDimensionsResource.cs`, `ReceiverGeometry.cs` permanently deleted | **PASS** | Confirmed non-existent on disk. Zero active references across `engine/`, `parts/`, `ui/`, and `CuriousContraptions.tests/`. |
| 2 | **Zero Shims & Adapters** | No backward compatibility adapters, bridge classes, or fallback wrappers | **PASS** | All references replaced with generic `MachinePart`, `WallDimensions`, `RampDimensions`, and scene-declared colliders. |
| 3 | **Strict Entity Separation** | Visual meshes and collider proxies are distinct separate nodes in every converted scene | **PASS** | Verified in `ball.tscn`, `bowling.tscn`, `wall.tscn`, `ramp.tscn`, and `basket.tscn`. Visual meshes under `Visual/`; colliders named `col_*` with `visible = false`. |
| 4 | **Generic MachinePart Architecture** | Visual setup, dimension scaling, and cosmetic bindings handled generically | **PASS** | `BuildDeclarative()` configures ball radii, wall/ramp dimensions, and receiver halo albedo curve bindings directly on `MachinePart`. |
| 5 | **Declarative Catalog Definitions** | `.tres` resources point to declarative scenes and canonical definitions | **PASS** | Obsolete `Ramp`, `Wall`, and legacy `Parameters` subresources removed from `ramp.tres`, `wall.tres`, `balloon.tres`, `tennis.tres`, and `basket.tres`. |
| 6 | **Static Analysis** | `anvil check` outputs 0 warnings on all changed files | **PASS** | 0 warnings, 0 errors across all modified source and test files. |
| 7 | **Automated Unit Tests** | `dotnet test CuriousContraptions.slnx` 100% pass | **PASS** | 827/827 passed, 0 failed, 0 skipped in 1s. |
| 8 | **Browser E2E Execution** | Pure TypeScript Playwright E2E suites pass serially in real Chrome | **PASS** | `cat-014.test.ts` (4/4 passed), `engine-core-2a1.test.ts` (5/5 passed), `engine-core-2c.test.ts` (2/2 passed). Total: 11/11 passed (100%). |
| 9 | **Lifecycle Invariants** | Reset restoration and Save/Load persistence roundtrip verified in Chrome | **PASS** | Verified in `cat-014.test.ts` (case 4) and `engine-core-2a1.test.ts` (cases 2, 3, 5). |

---

## 3. Detailed Acceptance Criteria Matrix

### Criteria from `spec-6-5d-ball-obstacle-conversion.md` and `epics.md`:

| Criterion | Expected Behavior | Observed Behavior | Status |
|---|---|---|---|
| **AC-1: Core Element Conversion** | Basketball, Bowling Ball, Tennis Ball, Balloon, Wall, Ramp, Receiver converted to declarative assets. | All 7 primitives converted to declarative scenes and canonical `.tres` catalog definitions. `bowling.tscn` created; `tennis.tres` and `balloon.tres` parameter dictionaries cleaned. | **MET** |
| **AC-2: Strict Entity Separation** | Visual mesh and collision geometry authored as separate entities; colliders have `visible = false`. | Confirmed across all `.tscn` files. Visual meshes under `Visual/`; collider shapes under root with `visible = false`. Zero colliders extracted from visual nodes. | **MET** |
| **AC-3: Generic Instantiation** | Each element defined by `.tres` in `parts/catalog/` and instantiated via generic `MachinePart`. | Scenes bind `res://engine/MachinePart.cs`. `PartRegistry.Create(PartId)` instantiates and configures via generic `MachinePart`. | **MET** |
| **AC-4: Complete Class Purge** | Bespoke C# classes (`BallPart`, `BasketPart`, `RampPart`, `WallPart`) and dimension/geometry files deleted. | All 7 target files deleted. 0 references remaining in active code. Zero compatibility shims. | **MET** |
| **AC-5: Bit-for-Bit Physical Equivalence** | Elements simulate and collide with identical geometry and restitution. | Wall, ramp, and receiver box dimensions preserve exact previous bounds; ball material definitions preserve exact radii, mass, bounce, friction. | **MET** |
| **AC-6: Static Analysis Clean** | `anvil check --changed` passes with 0 warnings. | 0 warnings, 0 errors. | **MET** |
| **AC-7: Unit Test Suite Pass** | Full solution test pass (`dotnet test CuriousContraptions.slnx`). | 827/827 tests passed (100%), 0 failures. | **MET** |
| **AC-8: Chrome Playwright E2E Pass** | Cumulative Chrome Playwright E2E suites pass 100% serially in real Chrome. | `cat-014` (4/4), `engine-core-2a1` (5/5), `engine-core-2c` (2/2) passed 100% serially in Chrome. | **MET** |

---

## 4. Verification Evidence & Raw Command Outputs

### 4.1 Verification of File Deletions & Zero Remnants
```sh
$ ls -la parts/BallPart.cs parts/BasketPart.cs parts/RampPart.cs parts/WallPart.cs engine/WallDimensionsResource.cs engine/RampDimensionsResource.cs engine/gpu/ReceiverGeometry.cs
ls: engine/gpu/ReceiverGeometry.cs: No such file or directory
ls: engine/RampDimensionsResource.cs: No such file or directory
ls: engine/WallDimensionsResource.cs: No such file or directory
ls: parts/BallPart.cs: No such file or directory
ls: parts/BasketPart.cs: No such file or directory
ls: parts/RampPart.cs: No such file or directory
ls: parts/WallPart.cs: No such file or directory
```
Active code search: 0 matches in `.cs` source files.

### 4.2 Static Analysis: Anvil Anti-Pattern & Policy Scan
```sh
$ anvil check engine/MachinePart.cs engine/MachineWorld.Gpu.cs engine/PartDefinition.cs engine/PartRegistry.cs engine/gpu/WorkbenchCapacity.cs engine/gpu/WorkshopConstruction.cs engine/gpu/WorkshopInstances.cs engine/gpu/WorkshopInventory.cs engine/gpu/WorkshopPhysicsCompiler.cs ui/Workshop.cs ui/WorkshopGpu.cs ui/WorkshopGuidance.cs CuriousContraptions.tests/BasketballResourceTests.cs CuriousContraptions.tests/WorkbenchCapacityTests.cs CuriousContraptions.tests/WorkshopBowlingTests.cs CuriousContraptions.tests/WorkshopBumperTests.cs CuriousContraptions.tests/WorkshopDominoTests.cs CuriousContraptions.tests/WorkshopSpringboardTests.cs CuriousContraptions.tests/WorkshopWallTests.cs CuriousContraptions.tests/GltfColliderExtractionTests.cs CuriousContraptions.tests/PartRegistryTests.cs engine/PartId.cs engine/gpu/GltfPhysicsBodyExtractor.cs
Last run written to .anvil/last-check.txt

  ✓ No warnings found
```
**Result:** 0 warnings, 0 errors.

### 4.3 Unit Tests: .NET Solution Suite
```sh
$ dotnet test CuriousContraptions.slnx
Test run for /Users/aidan/dev/personal/tim/CuriousContraptions.tests/bin/Debug/net10.0/CuriousContraptions.tests.dll (.NETCoreApp,Version=v10.0)
A total of 1 test files matched the specified pattern.

Passed!  - Failed:     0, Passed:   827, Skipped:     0, Total:   827, Duration: 1 s - CuriousContraptions.tests.dll (net10.0)
```
**Result:** 827/827 passed (100%), 0 failures, 0 skipped.

### 4.4 Browser E2E: Real Chrome Verification via Playwright

#### Suite 1: `cat-014.test.ts` (Bowling Ball dynamic sphere by material declaration)
```sh
$ npx tsx --test tools/e2e/cat-014.test.ts
▶ CAT-014: Bowling ball dynamic sphere by material declaration
  ✔ 1. Two lanes in one Run: the Bowling ball topples its Domino past 60 deg, the identically released Basketball leaves its Domino below 20 deg, and both balls come to rest (10886.286792ms)
  ✔ 2. Dropped from the placement plane the Bowling ball barely rebounds and rests at its 0.28 m radius; the Basketball rebounds high; Reset replays both worlds bit-for-bit (8764.156125ms)
  ✔ 3. A Bowling ball dropped into a placed Receiver is captured and the declared capture halo animates (4148.267042ms)
  ✔ 4. Save / reload / Load restores both kinds; after Reset the second Run topples the Bowling lane again and leaves the Basketball lane standing (17517.506791ms)
✔ CAT-014: Bowling ball dynamic sphere by material declaration (45478.626167ms)
ℹ tests 4
ℹ suites 1
ℹ pass 4
ℹ fail 0
ℹ cancelled 0
ℹ skipped 0
ℹ todo 0
ℹ duration_ms 45731.596542
```
**Result:** 4/4 passed (100%).

#### Suite 2: `engine-core-2a1.test.ts` (Dual-Sphere Simulation)
```sh
$ npx tsx --test tools/e2e/engine-core-2a1.test.ts
▶ ENGINE-CORE-2a1: Dual-Sphere Simulation and Pure TypeScript Playwright E2E
  ✔ 1. Clean boot: cross-origin isolation and WorkshopPoseRing initialized (3.836917ms)
  ✔ 2. Empty-construction control: Run advances sequence with 0 bodies, Reset restores Build Mode (1164.374208ms)
  ✔ 3. Single-ball control: Place 1 Basketball via UI, Run observes gravitational motion, Reset restores elevation (2647.733125ms)
  ✔ 4. Dual-sphere multi-body simulation: Place 2 Basketballs, Run observes independent trajectories (2800.200084ms)
  ✔ 5. Save & Load persistence roundtrip: Save via driver, reload page, Load restores both balls (4066.079042ms)
✔ ENGINE-CORE-2a1: Dual-Sphere Simulation and Pure TypeScript Playwright E2E (15155.220042ms)
ℹ tests 5
ℹ suites 1
ℹ pass 5
ℹ fail 0
ℹ cancelled 0
ℹ skipped 0
ℹ todo 0
ℹ duration_ms 15431.031542
```
**Result:** 5/5 passed (100%).

#### Suite 3: `engine-core-2c.test.ts` (Endpoint-Sampled Sensors & Dwell Tick Counter)
```sh
$ npx tsx --test tools/e2e/engine-core-2c.test.ts
▶ ENGINE-CORE-2c: Endpoint-Sampled Sensors & Dwell Tick Counter (Decoupled Sensor Evaluation)
  ✔ 1. Qualified Receiver Capture with Dwell at <= 1.5 m/s: ball settles in Receiver and triggers capture with visual halo (5134.945791ms)
  ✔ 2. Fast Through-Pass Rejection (> 1.5 m/s): high-speed transit through receiver region without dwell does NOT trigger capture (7347.184458ms)
✔ ENGINE-CORE-2c: Endpoint-Sampled Sensors & Dwell Tick Counter (Decoupled Sensor Evaluation) (16559.170708ms)
ℹ tests 2
ℹ suites 1
ℹ pass 2
ℹ fail 0
ℹ cancelled 0
ℹ skipped 0
ℹ todo 0
ℹ duration_ms 16836.257334
```
**Result:** 2/2 passed (100%).

---

## 5. Architectural Health & Zero Legacy Remnants Analysis

1. **Zero Backward Compatibility Shims:** No legacy classes or wrapper methods were preserved. All deleted types were completely expunged from the project file (`CuriousContraptions.csproj`) and the tree.
2. **Strict Entity Separation Maintained:** In all 5 converted scenes (`ball.tscn`, `bowling.tscn`, `wall.tscn`, `ramp.tscn`, `basket.tscn`), visual meshes and collider meshes are distinct sibling/child branches, and colliders have `visible = false`.
3. **Generic MachinePart Consolidation:** Bespoke logic has been fully eliminated in favor of declarative properties and generic scaling/binding routines on `MachinePart`.
4. **Clean Staging for Story 6.5e:** With core primitives converted, the codebase is cleanly prepared for Story 6.5e (converting interactive mechanisms: Bumper, Domino, Springboard, Switch, Lamp, Delay, Battery).

---

## 6. Conclusion

Story 6.5d meets every acceptance criterion, successfully deletes all target legacy classes with zero backwards-compatibility shims, enforces strict entity separation across all converted primitive scenes, and maintains 100% automated test pass rates without regressions. Terminal Scoped Verdict: **PASS**.
