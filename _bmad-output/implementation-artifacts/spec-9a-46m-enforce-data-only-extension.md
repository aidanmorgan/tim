---
title: '9A.46m — Enforce element-free core boundaries'
type: 'refactor'
created: '2026-11-08'
status: 'done'
baseline_revision: '4a9dc61ba5668871298314754ecd708ae4dab76e'
implementation_owner: 'Antigravity subagent'
implementation_isolation: 'Antigravity invoke_subagent self (no inherited history)'
review_loop_iteration: 1
followup_review_recommended: false
context:
  - '/Users/aidan/dev/personal/tim/AGENTS.md'
  - '/Users/aidan/dev/personal/tim/_bmad/custom/bmad-loop-protocol.md'
  - '/Users/aidan/dev/personal/tim/_bmad/custom/project-engineering-contract.md'
  - '/Users/aidan/dev/personal/tim/_bmad/custom/element-free-engine-design.md'
  - '/Users/aidan/dev/personal/tim/_bmad/custom/element-free-engine-review.md'
  - '/Users/aidan/.codex/worktrees/d1fb/tim/docs/testing-guidance.md'
warnings: []
deferred: []
---

<intent-contract>

## Intent

**Problem:** Story 9A.46m resolves Finding V1 and verifies complete closure of Findings A1–A12, E1–E8, and V1–V4:
1. Finding V1: `tools/BundleElements/ElementIsolation.cs` currently enforces only seven fixed name forms (`Workshop + name`, `name + Settings`, `name + Dimensions`, `name + Lessons`, `name + Simulation`, `Add + name + Configuration`, `Apply + name + Configuration`). Element-specific solvers (e.g. `BatterySolver`), element-specific reads/wire records (e.g. `PulleyWheelRead`), and element-specific event kinds/occurrences (e.g. `CounterReached`) escape detection.
2. Invariant 5 & Invariant 6 (`docs/architecture/element-package-isolation.md`): The final isolation gate must derive package identities dynamically from the generated bundle, distinguish generic capability names from package-specific dispatch, and operate with zero exception ledgers or whitelists.
3. Extension contract: Adding a new element package that uses supported physical or animation capabilities must require only declarations, bindings, assets, and tests in `elements/<Name>/`, with zero custom engine, core, or worker support.

**Approach:**
1. Strengthen `tools/BundleElements/ElementIsolation.cs`:
   - Expand package identifier detection beyond the seven fixed forms to cover:
     - Element-specific solvers and constraint handlers: `<Name>Solver`, `Workshop<Name>Solver`, `<Name>ConstraintSolver`, `<Name>TransmissionSolver`, `<Name>FrictionSolver`, `Solve<Name>`, `Step<Name>`.
     - Element-specific reads, observation records, and wire records: `<Name>Read`, `<Name>WheelRead`, `<Name>Observation`, `<Name>Record`, `<Name>TargetRead`, `<Name>StateRead`.
     - Element-specific event kinds and occurrences: `<Name>Reached`, `<Name>Set`, `<Name>Triggered`, `<Name>Fired`, `<Name>StateChanged`, `<Name>Changed`.
     - Element-specific lifecycle, logic, and dispatch: `<Name>Settings`, `<Name>Dimensions`, `<Name>Lessons`, `<Name>Simulation`, `<Name>Payload`, `<Name>Logic`, `<Name>Handler`, `<Name>Controller`, `<Name>Dispatch`, `Add<Name>Configuration`, `Apply<Name>Configuration`, `Compile<Name>`, `Simulate<Name>`, `Execute<Name>`, `Dispatch<Name>`, `Query<Name>`, `Read<Name>`, `On<Name>`.
     - Element-specific collection/plural fields: e.g. `Pulleys`, `Batteries`, `Counters`.
     - Prohibited package references: `using CuriousContraptions.Elements.<Name>` or references into `elements/` from shipping runtime assemblies.
   - Preserve legitimate generic capabilities and mathematical vocabulary:
     - Mathematical geometry terms like `CounterClockwise` in `RopeGeometry.cs` must not be flagged.
     - Cavity geometry like `SmoothClosestWall` in `HollowGeometry.cs` must not be flagged.
     - Timing queries like `DisplayWall` in `WorkshopPoseHistory.cs` and `WorkshopObservationProjection.cs` must not be flagged.
     - Admitted capability enum members `ActivationNodeKind.Counter` and `ActivationNodeKind.StateLatch` must remain valid capability selections.
     - Generic capability vocabulary like `Clock`, `Spring`, `CaptureLatch`, `ThresholdReached`, `MemoryChanged` must not be falsely flagged.
2. Provide literal source fixtures for positive controls in `CuriousContraptions.tests/ElementIsolationTests.cs`:
   - Prohibited fixtures: `BatterySolver`, `PulleyWheelRead`, `CounterReached`, `WorkshopPulley`, `ApplyBatteryConfiguration`, `Pulleys`.
   - Positive controls: Legitimate generic laws and capability structures pass cleanly.
   - Dynamic package derivation: Verifying that dynamically registering a new test element (e.g. `Gizmo`) automatically guards its solver, read, event, and dispatch forms without scanner code modifications.
   - Preprocessor trivia blanking: Verifying that conditional directives (`#if`, `#elif`, `#else`) cannot conceal prohibited coupling.
   - TypeScript/JavaScript token scanning: Verifying that template strings, escaped literals, and identifiers in `.ts` files are guarded.
3. Validate entire shipping codebase:
   - `Scanner().ScanTree()` runs over `engine/`, `ui/`, `CuriousContraptions.Core/`, `CuriousContraptions.Simulation/`, `CuriousContraptions.Animation.Worker/`, `CuriousContraptions.web/` and confirms zero hits across the entire tree.
   - Worker project closures contain only runtime capabilities.

## Boundaries & Constraints

**Always:** Preserve FR-01/02/03/06/07/09, PERF-24/25/26, 46a-l prerequisites, dedicated-worker C# WASM SIMD physics and animation pipeline, exact authored Reset and canonical serialized fields. Zero exception ledgers or whitelists. Refuse invalid inputs atomically with typed errors. Ensure 100% test pass rate with zero skips (`Skipped: 0`).

## Element-Free Engine Questions (Owner Contract, 4 November 2026)

1. **What behavior is delivered?**
   Strengthen the element-isolation build guard (`ElementIsolation.cs`) and architecture tests (`ElementIsolationTests.cs`) to strictly reject all element-specific solver types, observations/reads, wire records, event kinds/occurrences, bespoke dispatch, and lifecycle logic in engine/core/worker/ui code. Enforce data-only extension contracts so that adding a new element package requires only declarations, bindings, assets, and tests in `elements/<Name>/`.

2. **What is reusable?**
   `ElementIsolation` scanner operating over shipping C# and TypeScript sources. Dynamic package alias derivation from bundle declarations: newly added or renamed packages are automatically recognized and guarded. Closed capability taxonomies for solvers, reads, events, and nodes.

3. **What is declared?**
   Package declarations in `elements/<Name>/` declare metadata, parameters, colliders, frames, joints, ports, signal nodes, supply nodes, driven surfaces, and presentation bindings. The scanner discovers all elements from `BundleGenerator.Generate(services, root)` and checks all shipping source files.

4. **How does it execute?**
   - At build time, `RunElementBundleGenerator` in `BundleElements.csproj` invokes `BundleElements --root <dir> --out <file>`.
   - After generating and validating the bundle, it instantiates `new ElementIsolation(root, services.Read(generated.Json!).Elements)`.
   - `isolation.ScanTree()` walks all shipping C# and TypeScript source files.
   - In C#, `ScanCSharp` parses syntax with Roslyn (trivia-blanking conditional preprocessor directives), checking tokens against package identifier patterns, element literals, and type/member patterns.
   - In TypeScript, `ScanJavaScript` lexes tokens and template strings, checking against package identifier patterns and element literals.
   - If any violation is found, `BundleElements` exits with code 1, emitting MSBuild-formatted error lines (`file(line): error ISOLATION001: Rule: Token (Symbol)`).
   - In automated test suite, `ElementIsolationTests` verifies prohibited fixtures, positive controls, dynamic package derivation, and clean tree scan.

5. **Who owns changes?**
   `BundleElements` owns build-time isolation enforcement. Element packages in `elements/` own element definitions. Engine and Core own generic reusable physical and animation capabilities. Zero exception ledgers or whitelists exist.

6. **What proves generality?**
   Literal source fixtures:
   - `BatterySolver`: prohibited solver naming / package solver coupling.
   - `PulleyWheelRead`: prohibited read naming / package observation coupling.
   - `CounterReached`: prohibited event kind / occurrence naming.
   - Dynamic package addition / renaming: adding a new test bundle entry `Gizmo` (`gizmo`) immediately guards `GizmoSolver`, `GizmoWheelRead`, `GizmoReached`, etc., without modifying scanner code.
   - False-positive controls: `RevoluteConstraintSolver`, `TensileConstraintSolver`, `RopeGuideRead`, `RevoluteDriveTargetRead`, `ActivationNodeKind.Counter`, `ActivationNodeKind.StateLatch`, `RopeWinding.CounterClockwise`, `SmoothClosestWall`, `DisplayWall`, `ClockProbe`, `ThresholdReached`, `MemoryChanged`.

7. **What is deleted?**
   Seven-form-only narrow matching in `ElementIsolation.cs:47-56`.

8. **What remains uncertain?**
   None. All requirements, contracts, and affected files are identified and bounded.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|---|---|---|---|
| Prohibited solver fixture | C# source with `class BatterySolver` or `void SolveBattery()` | Scanner detects `PackageIdentifier` hit at exact line and symbol | Emits `error ISOLATION001` |
| Prohibited read fixture | C# source with `struct PulleyWheelRead` or `PulleyRead` | Scanner detects `PackageIdentifier` hit at exact line and symbol | Emits `error ISOLATION001` |
| Prohibited event fixture | C# source with `enum CounterReached` or `SimulationEventKind.CounterReached` | Scanner detects `PackageIdentifier` hit at exact line and symbol | Emits `error ISOLATION001` |
| Prohibited collection fixture | C# source with `List<Pulley> Pulleys` | Scanner detects `PackageIdentifier` hit at exact line and symbol | Emits `error ISOLATION001` |
| Prohibited TypeScript fixture | TS source with `function compileBattery()` or `${PartId.Battery}` | Scanner detects `PackageIdentifier` or `NamedPart` hit | Emits `error ISOLATION001` |
| Prohibited using directive | C# source with `using CuriousContraptions.Elements.Battery;` | Scanner detects package reference violation | Emits `error ISOLATION001` |
| Conditional source evasion | Prohibited identifier inside `#if DEBUG` or `#elif OTHER` | Directive trivia blanking preserves token; scanner detects hit | Emits `error ISOLATION001` |
| Renamed or new package | Dynamically added package `gizmo` (`Gizmo`) | Scanner automatically rejects `GizmoSolver`, `GizmoWheelRead`, `GizmoReached` | Emits `error ISOLATION001` |
| Legitimate geometry law | C# source using `RopeWinding.CounterClockwise` | Suffix `Clockwise` is legitimate; scanner reports 0 hits | Passes cleanly |
| Legitimate cavity law | C# source using `SmoothClosestWall` | Prefix `SmoothClosest` is legitimate; scanner reports 0 hits | Passes cleanly |
| Legitimate timing query | C# source using `DisplayWall` | Prefix `Display` is legitimate; scanner reports 0 hits | Passes cleanly |
| Legitimate capability enum | C# source using `ActivationNodeKind.Counter` or `ActivationNodeKind.StateLatch` | Admitted capability enum; scanner reports 0 hits | Passes cleanly |
| Legitimate generic solver | C# source using `RevoluteConstraintSolver` or `TensileConstraintSolver` | Generic physical law; scanner reports 0 hits | Passes cleanly |
| Legitimate generic read | C# source using `RopeGuideRead` or `RevoluteDriveTargetRead` | Generic physical read; scanner reports 0 hits | Passes cleanly |
| Legitimate generic event | C# source using `SimulationEventKind.ThresholdReached` or `MemoryChanged` | Generic capability event; scanner reports 0 hits | Passes cleanly |
| Full shipping tree scan | `isolation.ScanTree()` over all shipping projects | 0 hits across all shipping C# and TypeScript files | Returns empty list |

</intent-contract>

## Code Map

Candidate: isolated `/Users/aidan/.codex/worktrees/d1fb/tim`, branch `story-9a-46m-enforce-data-only-extension`, published baseline `4a9dc61ba5668871298314754ecd708ae4dab76e`.
Fresh implementation owner Antigravity subagent (no inherited history), retained through all repairs.

Authority: primary `_bmad-output/planning-artifacts/epics.md`; Finding V1, verifies closure of A1–A12, E1–E8, V1–V4; requirements PERF-24/26, FR-07, NFR-03/05/06. Prerequisites 46a-l published at baseline.

## Verification & Terminal Review

- **Maintainability Gate E9:**
  Command: `dotnet run --file /Users/aidan/dev/personal/tim/tools/anvil/VerifyPolicy.cs -- --maintainability --project-root /Users/aidan/.codex/worktrees/d1fb/tim`
  Result: `Maintainability gate (CA1505 index >= 20, CA1502 complexity <= 25, balanced coupling; test projects excluded): violations=0 (metrics=0, coupling=0). PASS`
  Coupling dependencies: `contract=2988, model=4, intrusive=0; violations=0`.
- **Compiler Proof Captures:**
  All 5 binlogs captured cleanly with 0 warnings and 0 errors: `playtest.binlog`, `solution.binlog`, `web.binlog`, `check-staged-tests.binlog`, `verify-policy.binlog`.
- **Static Gate & Float Guards:**
  109 passed, 0 failed, 0 skipped.
- **Normal Pre-Commit Hook:**
  `.githooks/pre-commit` passed with 134 passed, 0 failed, 0 skipped.
- **Full Solution Test Round:**
  - `WorkerAotGuard.Tests.dll`: 17 passed, 0 failed, 0 skipped
  - `Coverage.Tests.dll`: 77 passed, 0 failed, 0 skipped
  - `BundleElements.Tests.dll`: 238 passed, 0 failed, 0 skipped
  - `E2eRunner.Tests.dll`: 208 passed, 0 failed, 0 skipped
  - `CuriousContraptions.tests.dll`: 5,434 passed, 0 failed, 0 skipped
  - **Total**: 5,974 passed, 0 failed, 0 skipped (`Skipped: 0`).
- **Independent Adversarial Review:**
  6 Auto review layers executed; all confirmed defects and edge cases repaired by the implementation owner and re-verified:
  1. Forward-Only Auditor: Confirmed zero fallbacks, backwards compatibility shims, or legacy exception ledgers.
  2. Element-Free Engine Auditor: Confirmed data-only extension and zero element-specific assumptions in shipping code (SCOPED PASS).
  3. Verification Gap Reviewer: Confirmed complete literal fixture coverage, negative controls, and dynamic derivation testing (SCOPED PASS).
  4. Intent Alignment Auditor: Confirmed full alignment with Story 9A.46m goals and closure of Findings A1–A12, E1–E8, and V1–V4 (SCOPED PASS).
  5. Blind Hunter & Edge Case Hunter: Repaired 7 confirmed findings:
     - Deleted line 99 hardcoded identifier whitelist; derived canonical package names from directory paths (`SystemClock`, `StateLatch`), cleanly disambiguating generic timing (`Clocks`, `ClockObservation`) and capture domain (`latches`).
     - Added camelCase and interface prefix checking in `MatchesSuffix` and `MatchesPlural`.
     - Purged active shipping residual coupling in `workshop-observation.ts`: replaced `pulleys: readonly ObservedPulleyWheel[]` with `guides: readonly ObservedGuideRead[]` matching `WorkshopObservationProjection.cs`.
     - Added `-es` plural checking for `-o` words (e.g. `Dominoes`).
     - Handled expression-context member accesses and `global::` namespace aliases in Roslyn C# scanning.
     - Added element package import path validation in `ScanJavaScript`.
     - Added 24 new assertions in `ElementIsolationTests.cs` (80 tests total in suite), 100% passing.
