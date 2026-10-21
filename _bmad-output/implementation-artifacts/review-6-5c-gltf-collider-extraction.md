# Independent Adversarial Review: Story 6.5c: glTF Asset-Driven Geometry & Collider Extraction (ENGINE-CORE-2d)

**Reviewer:** Murdoch, Independent Adversarial Reviewer (Curious Contraptions)  
**Mandate:** Strict independent review policy (REQ-14: Self-review never qualifies)  
**Date:** 2026-10-13  
**Target:** Story 6.5c (`spec-6-5c-gltf-collider-extraction.md` / `epics.md` Story 6.5c)  
**Baseline Commit:** `9107148592a934ff980b8f6201318968b8df35ea`  

---

## 1. Executive Summary & Terminal Scoped Verdict

### **Verdict: PASS**

All acceptance criteria set forth in `_bmad-output/implementation-artifacts/spec-6-5c-gltf-collider-extraction.md`, Epic 6 Story 6.5c, and architectural decision AD-11 are fully satisfied with verifiable, reproducible evidence:
1. `GltfPhysicsBodyExtractor` dynamically traverses scene node hierarchies, extracting physical collider shapes (`col_box`, `col_sphere`, and `CollisionShape3D` nodes) and port locators (`Loc_Port_<Id>`) based solely on node transforms and geometry without element-specific branching.
2. **Strict Entity Separation Verified:** Visual mesh entities and collider entities are distinct, separate nodes. Visual mesh nodes are strictly presentation entities and are ignored during collision extraction. Collider mesh entities have `Visible = false` enforced upon extraction.
3. Universal transform hierarchy accumulation: Nested child nodes accurately accumulate 3D transforms relative to root parts.
4. Extracted colliders are admitted into `PartDefinition`, `MachinePart` (`GetPortTransform`), `WorkshopInstances` (`IWorkshopDeclaredColliders`), and `WorkshopPhysicsCompiler` generically with zero element-specific branching.
5. Static analysis via Anvil reports **0 warnings and 0 errors**.
6. The full native test suite passes at **100% (827/827 tests passed, 0 failures, 0 skipped)** via `dotnet test CuriousContraptions.slnx`.
7. Pure TypeScript Playwright browser E2E test suites pass at **100% serially in real Chrome** (`cat-014.test.ts`, `engine-core-2a1.test.ts`, `engine-core-2c.test.ts`), confirming exact authoring Reset restoration and Save/Load persistence roundtrip.

---

## 2. Adversarial Review Checklist

| # | Inspection Area | Requirement | Result | Evidence |
|---|---|---|---|---|
| 1 | **Generic Collider Extraction** | Traverse scene graph and extract `ColliderDeclaration` records generically based solely on node transforms and geometry | **PASS** | `GltfPhysicsBodyExtractor.cs` parses `BoxShape3D`, `SphereShape3D`, and proxy nodes (`col_box`, `col_sphere`) with f32/Half quantization and local pose computation. Zero element-specific branching. |
| 2 | **Strict Entity Separation** | Visual mesh entities and collider mesh entities must be distinct separate nodes; visual meshes ignored for collision; collider entities have `Visible = false` | **PASS** | Visual meshes (`Visual*`, `vis_*`, or un-prefixed meshes) are ignored. Extracted collider nodes have `node3D.Visible = false` set. Verified by unit tests `VisualMeshEntityIgnoredAndSeparateFromColliderEntity`, `CollisionShapeColliderVisibilitySuppressedAlongsideVisualMesh`, and `SceneWithOnlyVisualMeshExtractsZeroColliders`. |
| 3 | **Port Locator Resolution** | 3D socket anchors resolved dynamically from named empty locator nodes (`Loc_Port_<Id>`) matching declared port IDs | **PASS** | `Loc_Port_<Id>` empty locators extracted into `ExtractedPortLocator` with local positions and normalized quaternions. Exposed via `PartDefinition.PortLocators` and `MachinePart.GetPortTransform(portId)`. |
| 4 | **Transform Accumulation** | Secondary child branches share articulated transforms automatically | **PASS** | `GltfPhysicsBodyExtractor.Traverse` accumulates parent transforms (`parentAccumulatedTransform * node3D.Transform`); verified by `ComputesAccumulatedTransformsInDeepHierarchy`. |
| 5 | **Generic Physics Compilation** | Compiler admits asset-driven colliders generically without element switch statements | **PASS** | `WorkshopPhysicsCompiler.cs` compiles `IWorkshopDeclaredColliders` using declared colliders with zero element-specific switch statements. |
| 6 | **Zero Legacy Remnants & Shims** | Zero backwards compatibility shims, zero fallback loaders, zero element-specific branching in generic extraction | **PASS** | Strict rejection of unsupported proxy types (`col_cylinder`, `col_capsule`, `col_hull`, `-convcol`) with `NotSupportedException`. No fallback shims or legacy adapters. |
| 7 | **Static Analysis** | `anvil check` outputs 0 warnings on all changed files | **PASS** | 0 warnings, 0 errors across all 8 changed source and test files. |
| 8 | **Automated Unit Tests** | `dotnet test CuriousContraptions.slnx` 100% pass | **PASS** | 827/827 passed, 0 failed, 0 skipped in 2s. |
| 9 | **Browser E2E Execution** | Pure TypeScript Playwright E2E suites pass serially in real Chrome | **PASS** | `cat-014.test.ts` (4/4 passed), `engine-core-2a1.test.ts` (5/5 passed), `engine-core-2c.test.ts` (2/2 passed). Total: 11/11 passed (100%). |
| 10 | **Lifecycle Invariants** | Reset restoration and Save/Load persistence roundtrip verified in Chrome | **PASS** | Verified in `cat-014.test.ts` (case 4) and `engine-core-2a1.test.ts` (cases 2, 3, 5). |

---

## 3. Detailed Acceptance Criteria Matrix

### Criteria from `spec-6-5c-gltf-collider-extraction.md` and `epics.md`:

| Criterion | Expected Behavior | Observed Behavior | Status |
|---|---|---|---|
| **AC-1: Scene Graph Traversal & Extraction** | `WorkshopPhysicsCompiler` / `GltfPhysicsBodyExtractor` traverses scene graph and extracts `ColliderDeclaration` records generically based solely on node transforms and geometry without element-specific branching. | Extractor recursively evaluates Node3D transforms, detects box/sphere collision shapes and proxies, computes `RigidLocalPose` and half-extents/radii. Compiler emits colliders directly from `IWorkshopDeclaredColliders`. Zero switch statements on part kind. | **MET** |
| **AC-2: Port Locator Extraction** | 3D socket anchor points resolved dynamically from named locator nodes (`Loc_Port_<Id>`) matching declared port IDs. | Locators extracted into dictionary keyed by PortId with local Vector3 position and normalized Quaternion rotation. Available on `PartDefinition` and `MachinePart`. | **MET** |
| **AC-3: Articulated Hierarchy Transforms** | Secondary child branches in scene share articulated transforms automatically. | `parentAccumulatedTransform * node3D.Transform` computes true local pose relative to root part across arbitrary nesting depth. | **MET** |
| **AC-4: Strict Entity Separation** | Visual mesh and collider mesh entities must be distinct, separate nodes; visual meshes ignored for collision; collider entities have `Visible = false`. | Visual nodes (`Visual*`, `vis_*`, un-prefixed `MeshInstance3D`) explicitly ignored. Identified collider nodes have `node3D.Visible = false` set. Verified by unit tests. | **MET** |
| **AC-5: Generic Compilation & Stability** | Placed elements with asset-driven colliders interact, collide, and rest stably without hardcoded C# collider tables; zero element identifiers in compiler dispatch. | Generic compilation implemented in `WorkshopPhysicsCompiler` via `IWorkshopDeclaredColliders`. Tested via `WorkshopPhysicsCompilerCompilesGenericInstanceWithExtractedColliders`. | **MET** |
| **AC-6: Static Analysis Clean** | `anvil check --changed` passes with 0 warnings. | 0 warnings, 0 errors. | **MET** |
| **AC-7: Unit Test Suite Pass** | Full solution test pass (`dotnet test CuriousContraptions.slnx`). | 827/827 tests passed (100%), 0 failures. | **MET** |
| **AC-8: Chrome Playwright E2E Pass** | Cumulative Chrome Playwright E2E suites pass 100% serially; exact authoring Reset and Save/Load persistence roundtrip in Chrome. | `cat-014` (4/4), `engine-core-2a1` (5/5), `engine-core-2c` (2/2) passed 100% serially in Chrome. | **MET** |

---

## 4. Verification Evidence & Raw Command Outputs

### 4.1 Static Analysis: Anvil Anti-Pattern & Policy Scan
```sh
$ anvil check engine/gpu/GltfPhysicsBodyExtractor.cs engine/PartDefinition.cs engine/MachinePart.cs engine/gpu/WorkshopInstances.cs engine/gpu/WorkshopConstruction.cs engine/gpu/WorkbenchCapacity.cs engine/gpu/WorkshopPhysicsCompiler.cs CuriousContraptions.tests/GltfColliderExtractionTests.cs
Last run written to .anvil/last-check.txt

  ✓ No warnings found
```
**Result:** 0 warnings, 0 errors.

### 4.2 Unit Tests: .NET Solution Suite
```sh
$ dotnet test CuriousContraptions.slnx
Test run for /Users/aidan/dev/personal/tim/CuriousContraptions.tests/bin/Debug/net10.0/CuriousContraptions.tests.dll (.NETCoreApp,Version=v10.0)
A total of 1 test files matched the specified pattern.

Passed!  - Failed:     0, Passed:   827, Skipped:     0, Total:   827, Duration: 2 s - CuriousContraptions.tests.dll (net10.0)
```
**Result:** 827/827 passed (100%), 0 failures, 0 skipped.

### 4.3 Browser E2E: Real Chrome Verification via Playwright

#### Suite 1: `cat-014.test.ts` (Bowling Ball dynamic sphere by material declaration)
```sh
$ npx tsx --test tools/e2e/cat-014.test.ts
▶ CAT-014: Bowling ball dynamic sphere by material declaration
  ✔ 1. Two lanes in one Run: the Bowling ball topples its Domino past 60 deg, the identically released Basketball leaves its Domino below 20 deg, and both balls come to rest (10867.949ms)
  ✔ 2. Dropped from the placement plane the Bowling ball barely rebounds and rests at its 0.28 m radius; the Basketball rebounds high; Reset replays both worlds bit-for-bit (8532.556375ms)
  ✔ 3. A Bowling ball dropped into a placed Receiver is captured and the declared capture halo animates (4166.810083ms)
  ✔ 4. Save / reload / Load restores both kinds; after Reset the second Run topples the Bowling lane again and leaves the Basketball lane standing (17598.61775ms)
✔ CAT-014: Bowling ball dynamic sphere by material declaration (45846.717458ms)
ℹ tests 4
ℹ suites 1
ℹ pass 4
ℹ fail 0
ℹ cancelled 0
ℹ skipped 0
ℹ todo 0
ℹ duration_ms 46102.174459
```
**Result:** 4/4 passed (100%).

#### Suite 2: `engine-core-2a1.test.ts` (Dual-Sphere Simulation)
```sh
$ npx tsx --test tools/e2e/engine-core-2a1.test.ts
▶ ENGINE-CORE-2a1: Dual-Sphere Simulation and Pure TypeScript Playwright E2E
  ✔ 1. Clean boot: cross-origin isolation and WorkshopPoseRing initialized (4.045041ms)
  ✔ 2. Empty-construction control: Run advances sequence with 0 bodies, Reset restores Build Mode (1162.678833ms)
  ✔ 3. Single-ball control: Place 1 Basketball via UI, Run observes gravitational motion, Reset restores elevation (2717.168417ms)
  ✔ 4. Dual-sphere multi-body simulation: Place 2 Basketballs, Run observes independent trajectories (2799.131375ms)
  ✔ 5. Save & Load persistence roundtrip: Save via driver, reload page, Load restores both balls (4100.128458ms)
✔ ENGINE-CORE-2a1: Dual-Sphere Simulation and Pure TypeScript Playwright E2E (15225.639ms)
ℹ tests 5
ℹ suites 1
ℹ pass 5
ℹ fail 0
ℹ cancelled 0
ℹ skipped 0
ℹ todo 0
ℹ duration_ms 15503.994625
```
**Result:** 5/5 passed (100%).

#### Suite 3: `engine-core-2c.test.ts` (Endpoint-Sampled Sensors & Dwell Tick Counter)
```sh
$ npx tsx --test tools/e2e/engine-core-2c.test.ts
▶ ENGINE-CORE-2c: Endpoint-Sampled Sensors & Dwell Tick Counter (Decoupled Sensor Evaluation)
  ✔ 1. Qualified Receiver Capture with Dwell at <= 1.5 m/s: ball settles in Receiver and triggers capture with visual halo (5117.2065ms)
  ✔ 2. Fast Through-Pass Rejection (> 1.5 m/s): high-speed transit through receiver region without dwell does NOT trigger capture (7278.282583ms)
✔ ENGINE-CORE-2c: Endpoint-Sampled Sensors & Dwell Tick Counter (Decoupled Sensor Evaluation) (16470.929542ms)
ℹ tests 2
ℹ suites 1
ℹ pass 2
ℹ fail 0
ℹ cancelled 0
ℹ skipped 0
ℹ todo 0
ℹ duration_ms 16753.624125
```
**Result:** 2/2 passed (100%).

---

## 5. Architectural Health & Legacy Remnant Analysis

1. **No Fallback Loaders:** `GltfPhysicsBodyExtractor` raises explicit `ArgumentException` on non-positive or non-finite extents/radii and `NotSupportedException` on unsupported collider types (`col_cylinder`, `col_capsule`, `col_hull`, `-convcol`). No fallback guessing or silent truncation is permitted.
2. **Strict Entity Separation Maintained:** The presentation graph and the physics collision graph are completely segregated. Visual meshes remain visible and generate zero colliders. Collider proxies have presentation visibility suppressed (`Visible = false`) and generate physical colliders.
3. **Zero Element-Specific Branching:** Both `GltfPhysicsBodyExtractor` and `WorkshopPhysicsCompiler` handle extracted colliders generically without testing for specific part kinds or enum values.
4. **Transition Health for Stories 6.5d-e:** Existing legacy collider compilation paths remain functioning in parallel during the staged conversion, ensuring full backward test stability across all 827 unit tests and all browser E2E suites.

---

## 6. Conclusion

Story 6.5c meets every acceptance criterion, enforces strict visual/collider entity separation, and maintains 100% automated test pass rates without defects or regressions. Terminal Scoped Verdict: **PASS**.

---

## Addendum 21 Oct 2026, retro-verification (Story 9A.17)

Closing record under the epics rule "Reopened-record closure" (Story 6.5c's closing stories are 9A.16 and 9A.17). Only the
port-locator clauses are in scope here; the collider clauses belong to 9A.16. The original text above is unchanged.

**Reviewer:** Murdoch-24, terminal reviewer of Story 9A.17. I am not its implementer (Amelia-23), and I did not write the
original review above (dated 2026-10-13, baseline `9107148`).

**Identity.**
- Reviewed tree: `46e734289726c169d2d4a395a54db29ebb57d0f7`, commit `2bdd7e27`.
- Final commit: `24179187`, tree `0b9a8182`. It differs from the reviewed tree only in `docs/architecture/element-package-isolation.md` and under `_bmad-output/`.
- Main: merged as `6ca5591e` (2026-10-21T18:27:53+08:00), whose tree is the same `0b9a8182`.

**Evidence** (paths relative to `_bmad-output/implementation-artifacts/`):
- The review record: `review-9a-17-bundle-ports-and-footprint.md`, with its raw logs in `evidence-9a-17-review/`:
  - mutants `R1.txt` to `R6.txt`;
  - Tier 1 logs `tier1-build-debug.txt`, `tier1-build-release.txt`, `tier1-test.txt` and `tier1-node.txt`, taken on a clean copy at `2bdd7e27`. These logs carry no stamp of their own; their identity is recorded in the review record.
- **Full Chrome run:** `evidence-9a-17/e2e-full-2bdd7e27/summary.txt` (SHA-256 `bf9b4529…`).
  - Run `20261021T092247Z-74820`, `sourceHead 2bdd7e27`, `bundleIdentity 570e3d4cb8b8…`.
  - Started 2026-10-21T09:22:47Z, finished 10:16:41Z.
  - 48 files, 284/284 tests.
- **Clean rerun** of the two Contended SerialAlone suites: `evidence-9a-17/e2e-contended-rerun-2bdd7e27/summary.txt` (SHA-256 `1889fde9…`).
  - Run `20261021T101828Z-84313`, same `sourceHead`, `bundleIdentity fdf5b9e38fed…`.
  - 10:18:28Z to 10:21:06Z, verdict Pass, all Clean.
- **Red bundle build** before the scene renames: `evidence-9a-17/red-locators-before-rename.txt`.

| Original clause | Retro-verification finding (18 Oct) | Verdict at `24179187` |
| --- | --- | --- |
| Checklist #3: "3D socket anchors resolved dynamically from named empty locator nodes (`Loc_Port_<Id>`) matching declared port IDs"; evidence "Exposed via `PartDefinition.PortLocators` and `MachinePart.GetPortTransform(portId)`" | C: the runtime ports came from `WorkshopPorts.For`/`LocalPosition`, the locators were presentation-only, and the names did not match | **Closed, Pass**, against the clause as amended by Story 9A.17 AC6 ("one source in the bundle"). **Single source:** each element's declared `PortDescriptor` list (typed `PortSocket`) is the only runtime port source, through `ElementPortCatalogue`, read on the main thread and in the Simulation worker. `WorkshopPorts`, `MachinePart.TryGetPortTransform`, `GetPortTransform` and its string alias chain are deleted, with 0 `rg` hits in code. **Locators:** every socket locator is named `Loc_Port_<PortSocket>` exactly (22 in 12 scenes). The bundle build fails on a misnamed or missing locator, or one away from its declared anchor (BUNDLE011–013; mutant R1 is killed by BUNDLE013). `BundlePortLocatorTests` compares all 22 ports with their instantiated-scene locators. The runtime no longer resolves a socket from the scene; it reads the declaration, which the locator must equal |
| AC-2: "Locators extracted into dictionary keyed by PortId … Available on `PartDefinition` and `MachinePart`" | C (as above) | **Closed, Pass**, as amended. `GltfPhysicsBodyExtractor` matches `Loc_Port_` names ordinally (no case folding), and `PartDefinition.PortLocators` remains for the tube mouths (`mouth_start`, `mouth_end`; Story 9R.6). `MachinePart` has no port lookup any more, by design |
| Locator orientation (implied by "normalized quaternions") | Not raised | **Open, Low.** A declared port normal is not compared with its locator's basis (filed F19-2 in the 9A.17 record; owner: coordinator to schedule) |

**Stays open in this record:**
- AC-1 and checklist #1/#5: collider extraction, the canonical collider tables and the compiler's per-type collider rows. Owner: Story 9A.16.
- AC-3: articulated transforms. The finding (U) concerns colliders and bodies; owner 9A.16. A port that moves with a secondary body is not declared yet; the first element story that needs one owns it.
- Retro rows "no element ids in dispatch" and "Reset, Save/Load in Chrome" for extraction-driven parts. Owner: 9A.16.

`sprint-status.yaml` should not mark 6.5c `done` until 9A.16's addendum records its terminal Pass.
