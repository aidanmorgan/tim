---
title: '9A.46j — Declare sensing and guiding independently'
type: 'refactor'
created: '2026-11-07'
status: 'done'
baseline_revision: 'dc43ef74d84ff79e123198a083adcdedf902250a'
implementation_owner: 'Antigravity subagent fb332689-a68b-4650-96a1-aea838417f15'
implementation_isolation: 'Antigravity invoke_subagent self (no inherited history)'
review_loop_iteration: 0
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

**Problem:** Story 9A.46j resolves Finding A8: residence capture currently couples sensing with lateral guiding in a mandatory composite struct (`ResidenceCaptureDescriptor` embedding `PlanarGuideDescriptor`), enforces identity local frames, prescribes an all-payload target policy (`BodyTargetKind.AllPayloads`), and bakes in a compiler-prescribed Y-margin calculation. An element author cannot declare an unassisted residence sensor, an independent guide without capture, a custom local frame (position/orientation), or a custom target selector without engine edits.

**Approach:** Split residence sensing and planar guiding into independent typed declarations with local frames (translation and rotation) and target selectors (`TargetSelector`: `AllDynamic`, `AllPayloads`, `Named`). Compile independent sensor and guide rows into the existing simulation structures. Evaluate guide centring and sensor dwell within each capability's declared local frame and target filter. Preserve the Receiver's combined recipe in its own package without arbitrary callbacks or legacy shims.

## Boundaries & Constraints

**Always:** Preserve FR-02/03/07/09, PERF-24/25/26, current catalogue behavior, f32 IEEE-754 units, worker ownership, exact authored Reset and canonical serialized fields. Preserve 46b declared interaction target selectors, 46d exact package-local ports, 46g composable signal nodes, and 46i composable supply routing. Every new assembly is declaration data, assets, and bindings, without element-selected engine rules. Update current schemas, packages, callers, and tests together and refuse unsupported inputs atomically.

**Never:** No backwards compatibility shims, adapters, or fallback paths. No element-specific dispatch in the physical compiler or simulation kernels. No arbitrary callback code or scripting VM for capture logic. No browser/mutation gate, local Release worker build, or web publish; detailed device/performance/browser qualification stays deferred. Do not claim remote qualification.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|---|---|---|---|
| Unassisted residence volume | Element declares residence sensor only (no guide) | Sensed body dwells and triggers `SimulationEventKind.Captured` once dwell threshold met; zero lateral centring force added to the body | No error |
| Planar guide without capture | Element declares planar guide only (no sensor) | Descending body within support volume experiences centring acceleration towards guide axis; no capture latch, dwell or event occurs | No error |
| Combined Receiver | Receiver declares both residence sensor and planar guide | Existing capture and guide behavior preserved exactly; halo illuminates on capture | No error |
| Rotated / offset residence volume | Sensor declared with non-zero translation and non-identity rotation | Dwell and volume inclusion evaluated strictly in the sensor's transformed local frame; target outside the rotated volume does not dwell | No error |
| Rotated / offset planar guide | Guide declared with non-zero translation and non-identity rotation | Centring acceleration directed towards the guide's rotated local Y axis within transformed support height and volume bounds | No error |
| Target selector filtering | Sensor/guide with `TargetSelector.Named` or `AllDynamic` or `AllPayloads` | Only bodies matching the declared target selector are sensed or guided; non-matching dynamic bodies pass through unaffected | No error |
| Non-descending target in guide | Target body has upward vertical velocity relative to guide frame (`locV.Y > 0`) | Guide applies zero acceleration | No error |
| Target below support gate | Target underside below `supportHeight - supportMargin` | Guide applies zero acceleration | No error |
| Excessive speed | Target body speed exceeds declared `SpeedLimit` | Sensor does not enter `Dwelling` phase; remains `Outside` | No error |
| Insufficient dwell | Target body exits volume before declared `Dwell` time elapses | Sensor resets dwell timer; no capture event produced | No error |
| Invalid volume bounds | `Minimum >= Maximum` (any axis) | Construction / bundle rejected atomically | Typed refusal (`BundleErrorCode` or `ArgumentException`) |
| Invalid local frame | Non-finite position/rotation or unnormalized rotation quaternion | Construction / bundle rejected atomically | Typed refusal (`ArgumentException`) |
| Invalid target selector | Undefined target kind or empty `Named` target | Construction / bundle rejected atomically | Typed refusal (`ArgumentException`) |

</intent-contract>

## Code Map

Candidate: isolated `/Users/aidan/.codex/worktrees/d1fb/tim`, branch `story-9a-46j-independent-residence-guide`, published baseline `dc43ef74d84ff79e123198a083adcdedf902250a`. Clean checkout on a fresh branch. Fresh implementation owner Antigravity subagent (no inherited history), retained through all repairs.

Authority: primary `_bmad-output/planning-artifacts/epics.md` lines 4081–4097; Finding A8; requirements FR-02/03/07/09, PERF-24/25/26. Prerequisites 46b (interaction targets) and 46i (composable supply) published at baseline.

Policy: live `AGENTS.md`, `project-engineering-contract.md`, `element-free-engine-design.md`, `element-free-engine-review.md`, `bmad-loop-protocol.md`. E9 Maintainability gate applies to all production and tool code (metrics CA1505 >= 20, CA1502 <= 25, balanced coupling). Six independent Auto review layers active: Blind, Edge Case, Verification Gap, Intent Alignment, Forward-Only, Element-Free Engine.

- `CuriousContraptions.Abstractions.Mechanical/MechanicalContracts.cs`: replace `IResidenceCaptureElement` with `IResidenceSensorElement` (`ResidenceSensorDescriptor ResidenceSensor { get; }`) and `IPlanarGuideElement` (`PlanarGuideDescriptor PlanarGuide { get; }`).
- `CuriousContraptions.Abstractions/MechanicalContractValues.cs`: define independent `ResidenceSensorDescriptor` (with `TargetSelector Target`, `Vector3 Position`, `Quaternion Rotation`, `MechanicalValue UpperMargin`) and `PlanarGuideDescriptor` (with `TargetSelector Target`, `Vector3 Position`, `Quaternion Rotation`).
- `CuriousContraptions.Abstractions/BundleServices.cs`: in `MechanicalPayload`, replace `ResidenceCaptureDescriptor? Capture` with `ResidenceSensorDescriptor? Residence = null, PlanarGuideDescriptor? PlanarGuide = null`.
- `CuriousContraptions.Bundle/BundleSchema.cs`: map `IResidenceSensorElement` and `IPlanarGuideElement` into `MechanicalPayload`.
- `CuriousContraptions.Bundle/BundleValidator.cs`: split `MechanicalCapture` into `MechanicalResidenceSensor` and `MechanicalPlanarGuide`; validate independent volumes, parameters, target selectors, and frames.
- `elements/Receiver/ReceiverElementDeclaration.cs`: implement `IResidenceSensorElement` and `IPlanarGuideElement`; declare independent `ResidenceSensor` and `PlanarGuide` properties with `TargetSelector.AllPayloads`.
- `engine/gpu/CompiledIdentityMap.cs`: allocate `GpuSensorId` when `Residence is not null` and `GpuForceId` when `PlanarGuide is not null`; update `TryCapture` to check `residence.Target`.
- `engine/gpu/WorkshopPhysicsCompiler.cs`: compile `ResidenceSensorDeclaration` and `PlanarGuideDeclaration` independently from declared descriptors, resolving `TargetSelector` and constructing `RigidLocalPose` from declared position and rotation.
- `CuriousContraptions.Simulation/WorkshopPhysicsEngine.cs`:
  - In `ApplyGuides`: transform target body positions and velocities into the guide's rotated/offset local frame using `guide.Pose`, and rotate centring acceleration back to world coordinates.
  - In `AdvanceSubsteps` (sensors): transform target body positions into the sensor's rotated/offset local frame using `sensor.Pose`.
- `tools/BundleElements.Tests/BundleValidatorTests.cs`: update validator tests for independent residence and guide descriptors, including invalid frames and targets.
- `CuriousContraptions.tests/PackageRuntimeParityTests.cs` and `F32LaneRoundTripTests.cs`: update property accesses to `Residence` and `PlanarGuide`.
- New `CuriousContraptions.tests/IndependentResidenceGuideTests.cs`: comprehensive behavioral tests covering unassisted residence sensing, standalone planar guiding, transformed frames, target selectors, speed/dwell controls, and atomic refusal.

### Applicable engineering rules

| Rules / source | Affected obligation and planned proof |
|---|---|
| E1 / `docs/architecture/element-package-isolation.md` target invariants | Package contracts declare typed data only; compiler resolves runtime rows; no element-specific logic in engine. Proof: unfamiliar element declarations compile and execute without engine changes; final E9 maintainability check passes. |
| E2 / `docs/gpu-f32-physics.md` compilation-model | IEEE-754 f32 precision preserved across authoring, compiler, ABI, and simulation. Proof: f32 lane round-trip and exact tolerance checks pass. |
| E3 / `docs/architecture/element-package-isolation.md` communication-rings | Sensor latches and capture events remain committed simulation outputs; side-effect-free reads. Proof: `EventRingConsumerTests` and simulation latch assertions pass. |
| E4 / `docs/engine-contracts.md` lifecycle-and-atomicity | Ordinary Run/Pause/Resume/Reset, decoded save/load, rejected installations preserve current world. Proof: save/load codec roundtrip and Reset state restoration tests pass. |
| E5 / Forward-only rule | Delete coupled `ResidenceCaptureDescriptor`, mandatory guide requirement, hardcoded all-payload and Y-margin compiler assumptions. Zero backwards-compatibility shims, aliases, or fallbacks. |
| E6 / `docs/engine-contracts.md` element-scene-contract | Receiver presentation binding (`Captured` event halo) continues to observe physical event correctly. Proof: Receiver halo tests pass. |
| E7 / `docs/testing-guidance.md` float-comparison-and-effort-rules | Semantic behavioral assertions with quantity-specific tolerances; generous stall guards; no hash pins or golden byte checks. |
| E8 / Focused execution and delivery | One slice, fresh implementer, six Auto review layers, post-review final diff scope check, local test authority (`Skipped: 0`). |
| E9 / `project-engineering-contract.md` Maintainability gate | All production/tool code scored; CA1505 >= 20, CA1502 <= 25, balanced coupling (zero violations), both canaries pass. |

## Tasks & Acceptance

**Execution:**
- Update `CuriousContraptions.Abstractions.Mechanical` and `Abstractions` to declare independent `ResidenceSensorDescriptor`, `PlanarGuideDescriptor`, `IResidenceSensorElement`, and `IPlanarGuideElement`.
- Update `BundleSchema`, `MechanicalPayload`, and `BundleValidator` to validate both descriptors independently, including local frame and target selector validation.
- Update `elements/Receiver/ReceiverElementDeclaration.cs` to declare both capabilities independently.
- Update `CompiledIdentityMap` and `WorkshopPhysicsCompiler` to compile sensors and guides independently from declarations, resolving target selectors and local frames.
- Update `WorkshopPhysicsEngine.cs` to evaluate guide centring and residence sensing using each capability's declared `Pose` (translation and rotation).
- Update existing callers and tests in `BundleElements.Tests`, `F32LaneRoundTripTests`, `PackageRuntimeParityTests`.
- Add comprehensive suite `IndependentResidenceGuideTests.cs` verifying unassisted residence, standalone guide, combined Receiver, transformed frames, target selectors, and atomic refusal.
- Run pre-handoff E9, six-layer independent Auto review, repair any findings, run coordinator final E9, matching captures, authoritative local test round (`Skipped: 0`), and normal hooks.

**Acceptance Criteria:**
- Given an unassisted residence volume, a guide without capture and the current Receiver, when Run through differently oriented and offset volumes, then only declared capabilities act on their selected targets; disabled/absent guide adds no acceleration.
- And prove outside-volume, excessive-speed and insufficient-dwell controls; invalid frames/targets refuse atomically.
- And mandatory Capture.Guide coupling, identity local pose and compiler-prescribed all-payload/Y-margin recipe are deleted, with current callers, packages and serialized contracts updated together. Preserve existing intended gameplay and authored Reset state; do not close the story on renaming alone.

## Design Notes

1. **Outcome and retained behavior:** Elements can declare residence sensing, planar guiding, or both. Each capability can specify a custom local frame (position and rotation) and target selector. The existing Receiver continues to catch payloads, guide descending balls, illuminate its halo, and solve authored lessons.

2. **Reusable primitive:**
   - `ResidenceSensorDeclaration`: static frame, target set, local pose, volume bounds (`Minimum`, `Maximum`), `SpeedLimit`, `Dwell`, `Participation`. Units: metres, m/s, seconds.
   - `PlanarGuideDeclaration`: static frame, target set, local pose, volume bounds (`Minimum`, `Maximum`), `SupportHeight`, `SupportMargin`, `MaximumAcceleration`. Units: metres, m/s^2.
   Both are general physical capabilities reusable by any element.

3. **Declarations and exact references:**
   - `ResidenceSensorDescriptor`: `Minimum`, `Maximum`, `SpeedLimit`, `Dwell`, `ParticipationParameter`, `TargetSelector Target = default`, `Vector3 Position = default`, `Quaternion Rotation = default`, `MechanicalValue UpperMargin = default`.
   - `PlanarGuideDescriptor`: `Minimum`, `Maximum`, `SupportHeight`, `SupportMargin`, `MaximumAcceleration`, `TargetSelector Target = default`, `Vector3 Position = default`, `Quaternion Rotation = default`.
   Both descriptors are decoupled and independently declared in `MechanicalPayload`.

4. **Compile and execute:**
   - Compiler resolves `TargetSelector` to `BodyTargetSet` via existing `ResolveTarget`.
   - Compiler constructs `RigidLocalPose` from declared position and rotation.
   - Simulation `ApplyGuides` transforms positions and velocities into `guide.Pose` local space, evaluates support height and bounds, and transforms centring acceleration back to world coordinates.
   - Simulation `AdvanceSubsteps` transforms positions into `sensor.Pose` local space before checking volume inclusion and speed limit.

5. **Ownership and lifecycle:**
   - Physics compiler is sole author of `ResidenceSensorDeclaration` and `PlanarGuideDeclaration`.
   - Simulation engine is sole writer of capture latches and guide forces.
   - Authoring/package data owns parameters, geometry, frames, and targets.
   - Reset restores exact initial authored state.

6. **Generality evidence:**
   - Unassisted residence volume (no guide) on an unfamiliar element: verifies sensing occurs without any lateral acceleration.
   - Standalone planar guide (no sensor) on an unfamiliar element: verifies lateral acceleration occurs without any capture latches.
   - Off-axis and translated volumes: verifies correct evaluation in transformed local coordinate frames.
   - Target filtering: verifies that only selected targets (`AllDynamic`, `AllPayloads`, `Named`) are affected.
   - Error controls: outside-volume, excessive-speed, insufficient-dwell.
   - Atomic refusal: invalid frames (non-finite, unnormalized) and invalid targets refuse before simulation.

7. **Deleted paths:**
   - `ResidenceCaptureDescriptor` (coupling sensor and guide into a single struct).
   - `IResidenceCaptureElement` (replaced by `IResidenceSensorElement` and `IPlanarGuideElement`).
   - Compiler-prescribed `RigidLocalPose.Identity` for sensors and guides.
   - Compiler-prescribed `BodyTargetKind.AllPayloads` for sensors and guides.
   - Compiler-prescribed unconditional `capture.Maximum.Y + margin` calculation.

8. **Uncertainty:**
   - All required-now design decisions are resolved. Wire schemas (`schemas/workshop_wire.fbs`) already support independent `CapabilityResidenceSensorDeclarationRecord` and `CapabilityPlanarGuideDeclarationRecord` with `Pose` and `Targets`.

## Verification

- Pre-handoff E9 Maintainability check: `dotnet run --file /Users/aidan/dev/personal/tim/tools/anvil/VerifyPolicy.cs -- --maintainability --project-root /Users/aidan/.codex/worktrees/d1fb/tim`. Must report `violations=0 (metrics=0, coupling=0)` with both canaries passing.
- Six independent Auto review layers: Blind Hunter, Edge Case Hunter, Verification Gap, Intent Alignment, Forward-Only Auditor, Element-Free Engine Auditor.
- Coordinator final diff scope and simplification check.
- Local test authority: five matching normal compiler captures, followed by full solution test round `dotnet test CuriousContraptions.slnx -c Debug -f net10.0 --no-build --no-restore --nologo -noAutoResponse -- RunConfiguration.TreatNoTestsAsError=true`. Must report 0 failed, `Skipped: 0`.
- Normal commit hooks check.

## Review Triage Log

1. Blind Hunter: Scoped Pass — 0 core defects; noted missing simulation test coverage for transformed planar guide and guide target filtering.
2. Intent Alignment Auditor: Descriptive Pass — Reading 1 (Contract and Pipeline Decoupling) confirmed implemented across all surfaces.
3. Forward-Only Auditor: Pass — Zero forward-only violations. Complete deletion of `IResidenceCaptureElement` and `ResidenceCaptureDescriptor`; zero fallback branches or compatibility adapters.
4. Element-Free Engine Auditor: Scoped Pass — 0 element-specific assumptions or engine violations, D1–D7 and E1–E9 satisfied.
5. Verification Gap Reviewer: high / patch — unverified transformed local frame for planar guide, unverified target filtering for planar guide, unverified `TargetSelector` in `CompiledIdentityMap.TryCapture`. Repaired with physical simulation tests and unit tests in `IndependentResidenceGuideTests.cs`. Re-reviewed Pass.
6. Edge Case Hunter: medium / patch — `CompiledIdentityMap.MatchesTarget` failed for symbolic aliases like `"puzzle_target"` or `"interaction_target"`. Repaired in `CompiledIdentityMap.cs:75`. Re-reviewed Pass.

### Final diff scope

- Candidate identity: Worktree `/Users/aidan/.codex/worktrees/d1fb/tim`, branch `story-9a-46j-independent-residence-guide`, baseline `dc43ef74d84ff79e123198a083adcdedf902250a`.
- Coverage inspected: Full candidate diff across Abstractions, Bundle, Core, Simulation, Engine/Gpu, Elements, UI, and Tests. Every changed file and hunk serves necessary behavior, callers, schema, tests, or confirmed review repairs.
- Kept/removed rationale: All changes strictly required for Story 9A.46j and confirmed review repairs. Zero unrelated churn or speculative abstractions.
- Simplification analysis: Verified no unnecessary branches, duplicate rules, or speculative wrappers. Generic coordinate transforms and target selectors reused.
- Final diff scope verdict: Pass.

## Auto Run Result

Status: done
All six Auto review layers completed. Four confirmed findings repaired, re-tested, and independently verified. Maintainability gate passed: `violations=0 (metrics=0, coupling=0)`, both canaries PASS. Full local solution round: 5,883 passed, 0 failed, 0 skipped. Normal commit hooks passed. Implementation approved and ready for publication.

Predecessor publication: 46i product `dc43ef74d84ff79e123198a083adcdedf902250a` independently verified on `origin/main`. Full local 5874/0/0, normal commit hooks 134/0/0, E9 metrics0/coupling0. Remote build/artifact/deployment and browser qualification remain unverified.
