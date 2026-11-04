---
title: 'Story 9A.46a: Route ropes around any declared guide'
type: 'feature'
created: '2026-11-04'
baseline_revision: 30eba561c09164a4bc8c0c8080b7b7fcbdc16760
status: 'done'
review_loop_iteration: 1
followup_review_recommended: false
context:
  - '{project-root}/docs/testing-guidance.md'
  - '{project-root}/docs/architecture/element-package-isolation.md'
  - '{project-root}/_bmad/custom/element-free-engine-design.md'
  - '{project-root}/_bmad/custom/element-free-engine-review.md'
  - '{project-root}/_bmad-output/implementation-artifacts/review-engine-abstraction-2026-11-04.md'
warnings: ['oversized']
deferred: []
---

<intent-contract>

## Intent

**Problem:** Tensile rope routing currently hardcodes Pulley-specific dimensions in shared engine code (`RopeGeometry.PulleyRadius = 0.4f` and `PulleySocketDepth = 0.12f` in `RopeGeometry.cs`), preventing elements from declaring guide sheaves with different radii or offsets. In addition, physical observation discovery in `WorkshopGpuDevice.cs` / `PhysicsPresentationRead.cs` discovers pulley bodies by searching cosmetic presentation tracks (`PresentationDriver.CommittedWheelAngle`), violating the principle that physical state and observations must be published directly from physical declarations rather than discovered through art bindings.

**Approach:** Generalize rope route geometry and tensile route evaluation so that every guide sheave's local frame, radius ($r > 0$), and socket offset are declared by package data and compiled into shared route structures. Replace `PulleyWheelRead` and `inputs.Pulleys` with generic coordinate observations (`RopeGuideRead` / `GuideReads`) published directly from physical declarations, eliminating all hardcoded pulley constants and art-driven physical observation discovery.

## Boundaries & Constraints

**Always:**
- Execute physics using canonical IEEE-754 `f32` single precision at 120 Hz with 480 Hz substeps.
- Every guide sheave declares its own local frame and radius ($r > 0$) as physical declaration data on the package. The existing Pulley declares radius 0.4 m and socket depth 0.12 m without behavioral change.
- `RopeGeometry.EvaluateRoute` accepts per-guide frames and radii; tangent leg calculation, arc lengths, total route length, and angular rotation speed ($\omega = v / r$) strictly follow each guide's declared values.
- Physical guide observations (`RopeGuideRead`) publish travel and angle ($\theta = \text{travel} / r_{\text{guide}}$) for every declared guide in the compiled physical scene, independent of whether presentation tracks exist. Removing or altering cosmetic animation tracks leaves physical observations and simulation motion identical.
- Closed enums and strong typing end-to-end; atomic validation rejects invalid radius ($r \le 0$, non-finite), non-finite frames, or undefined winding.
- Exact Reset restores initial route state, zero travel, and authored poses; Save/Load roundtrip preserves guide connections.
- Zero skipped tests (`Skipped: 0`), no hash/byte assertions, and no mutation testing before the post-catalogue E2E phase.

**Never:**
- No hardcoded element constants (delete `PulleyRadius` and `PulleySocketDepth` from `RopeGeometry.cs`).
- No discovering physical observation targets from cosmetic presentation tracks or naming observations after specific puzzle elements (delete `PulleyWheelRead`, `inputs.Pulleys`, and `CommittedWheelAngle` filtering in simulation core).
- No backwards-compatibility shims, legacy fallbacks, or deprecated properties.
- No browser E2E execution during this slice (remains deferred per `BrowserDeferral.cs`).

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Unfamiliar guide A ($r=0.2$) | Tensile route with sheave $r=0.2$ m, offset 0.05 m | Route wraps around $r=0.2$ sheave; arc length is $0.2 \cdot \theta$; angle reads $\text{travel} / 0.2$ | No error expected |
| Unfamiliar guide B ($r=0.6$) | Tensile route with sheave $r=0.6$ m, offset 0.2 m | Route wraps around $r=0.6$ sheave; arc length is $0.6 \cdot \theta$; angle reads $\text{travel} / 0.6$ | No error expected |
| Existing Pulley ($r=0.4$) | Standard Pulley sheave | Behavior identical to baseline: tangent points at $r=0.4$ m, angle reads $\text{travel} / 0.4$ | No error expected |
| Multi-guide route | Route through both $r=0.2$ and $r=0.6$ guides | Total route length equals sum of straight segments plus exact individual arc lengths | No error expected |
| Cosmetic track removal | Guide element has no `CommittedWheelAngle` or presentation tracks | Physical `GuideReads` still published; tensile simulation motion identical | No error expected |
| Straight route (no guides) | Rope connecting two endpoints directly without guides | `guides.Length == 0`, straight line distance, empty arc array | No error expected |
| Slack rope | Endpoint distance below maximum length | Route state is Slack; physical length and guide angles reflect actual geometry | No error expected |
| Cut rope | Rope span marked cut | Tensile constraint deactivated; route marked open | No error expected |
| Winding persistence | Motion causes endpoints to oscillate | Authored winding direction does not flip discontinuously under small motions | No error expected |
| Reset & Replay | Run under motion, then Reset | Travel zeroes, guide angle zeroes, initial route length and poses restored | No error expected |
| Invalid radius | Sheave declared with $r \le 0$ or non-finite | Atomic validation refusal at bundle admission / compiler | Typed refusal |
| Missing guide body | Route references nonexistent body ID | Atomic validation refusal | Typed refusal |

</intent-contract>

## Design Record (Element-Free Engine Gate)

1. **What behavior is delivered?**
   Tensile rope routes wrap around sheaves of arbitrary declared radius and frame. Generic physical guide observations publish travel and rotation angle directly from declared physical guides without inspecting presentation tracks or hardcoding element names.
2. **What is reusable?**
   `RopeGuideDescriptor` / `RopeGuideDeclaration` defining local sheave center, normal/axis, and radius. Reusable `RopeGeometry.EvaluateRoute` taking per-guide geometry. Generic `RopeGuideRead` carrying `(GpuBodyId Guide, float Travel, float Angle)`.
3. **What is declared?**
   Package ports with `RopeAttachmentRole.Guide` declare guide sheave geometry: sheave radius and socket offset. Pulley package declares $r=0.4$ m, depth $0.12$ m.
4. **How does it execute?**
   Package declaration -> validation ($r > 0$, finite) -> compilation into route records -> `TensileConstraintSolver` evaluates route using declared per-guide geometry -> `WorkshopRead.Guides` publishes generic observations -> animation consumes committed guide state.
5. **Who owns changes?**
   Package owns sheave dimensions. Compiler translates to runtime structures. Simulation core advances route lengths and angular positions.
6. **What proves generality?**
   Two unfamiliar guide fixtures ($r=0.2$ m and $r=0.6$ m) with distinct radii and offsets evaluated alongside the 0.4 m Pulley. Exact tangent positions, arc lengths, and rotation speeds verify scaling with declared radius. Cosmetic track removal verifies observation independence.
7. **What is deleted?**
   `RopeGeometry.PulleyRadius`, `RopeGeometry.PulleySocketDepth`, `PulleyWheelRead`, `inputs.Pulleys`, and `PresentationDriver.CommittedWheelAngle` filtering in `WorkshopGpuDevice.cs`.
8. **What remains uncertain?**
   None. All required design decisions are settled.

## Code Map

- `CuriousContraptions.Abstractions/PortDescriptor.cs`: Add guide sheave geometry (e.g. `float GuideRadius` and `Vector3 GuideOffset`) to `PortDescriptor` or `RopeAttachmentRole`.
- `engine/gpu/RopeGeometry.cs`: Remove `PulleyRadius` and `PulleySocketDepth`. Update `EvaluateRoute` to take per-guide radius and frame.
- `CuriousContraptions.Simulation/TensileConstraintSolver.cs`: Supply per-guide declared geometry to `EvaluateRoute`.
- `engine/gpu/PhysicsPresentationRead.cs`: Replace `PulleyWheelRead` and `pulleys` mapping with `RopeGuideRead` and `guides` mapping.
- `CuriousContraptions.Simulation/WorkshopGpuDevice.cs`: Replace cosmetic track inspection `pulleys` with physical guide bodies compiled from scene.
- `engine/gpu/WorkshopPhysicsCompiler.cs`: Compile guide geometry from element port descriptors.
- `elements/Pulley/PulleyElementDeclaration.cs`: Declare $r=0.4$ m and depth $0.12$ m explicitly in port descriptor.
- `CuriousContraptions.tests/`: Add tests with unfamiliar guide geometries ($r=0.2$ m and $r=0.6$ m) verifying route length, tangent points, and rotation angle scaling.

## Tasks & Acceptance

**Execution:**
- `CuriousContraptions.Abstractions/PortDescriptor.cs`: Add `GuideRadius` and `GuideOffset` to guide port descriptors.
- `engine/gpu/RopeGeometry.cs`: Delete `PulleyRadius` and `PulleySocketDepth`. Refactor `EvaluateRoute` to consume declared per-guide radius and frame.
- `CuriousContraptions.Simulation/TensileConstraintSolver.cs`: Plumb declared guide radius and frame into route solver.
- `engine/gpu/PhysicsPresentationRead.cs` & `WorkshopGpuDevice.cs`: Delete `PulleyWheelRead` and presentation-driven pulley discovery; introduce generic `RopeGuideRead`.
- `elements/Pulley/PulleyElementDeclaration.cs`: Update Pulley declaration to declare its sheave radius and offset.
- `CuriousContraptions.tests/RopeGuideTests.cs`: Add tests verifying unfamiliar guide geometries ($r=0.2$, $r=0.6$) alongside Pulley ($r=0.4$), straight route, cut route, slack route, and presentation decoupling.

**Acceptance Criteria:**
- Given two unfamiliar guide sheaves ($r=0.2$ m and $r=0.6$ m) and a Pulley ($r=0.4$ m), when a tensile route is compiled and simulated under motion, then route arc lengths and rotation angles strictly match each guide's declared radius.
- Given a guide element without any cosmetic presentation tracks, when simulated, then generic physical guide observations are still published and physical motion is identical.
- Given any sheave declared with non-positive or non-finite radius, when compiled, then validation rejects it atomically with a typed refusal.
- Given simulation run under motion, when Reset is executed, then initial guide travel, angles, and poses are restored exactly.

## Spec Change Log

## Review Triage Log

### 2026-11-04 — Review pass
- verdicts: 13 findings — high 3, medium 2, low 1, false 7, maybe-false 0
- findings:
  - `[high]` `[patch]` Guide sheave identity conflated with rigid body identity (violates D1/D2) — Introduce explicit RopeGuideId so multiple guide sheaves per body are independently identified.
  - `[high]` `[patch]` Candidate staging omitted core 9A.46a files (compiler, declarations, presentation, wire, tests) — Re-stage candidate to include the complete 9A.46a dependency closure.
  - `[high]` `[patch]` Candidate staging contaminated with Conveyor modifications (11.1b) — Isolate 9A.46a by excluding unowned Conveyor code from candidate diff.
  - `[medium]` `[patch]` Modulo-wrapped angle ratio assertion defect in dynamic simulation test — Remove guideA.Angle / guideB.Angle == 3.0 division and verify exact modular travel/r equality.
  - `[medium]` `[patch]` Incomplete dynamic execution in presentation decoupling test — Add simulation.Advance() loop to verify dynamic guide observations update without presentation tracks.
  - `[low]` `[patch]` Unjustified 1e-4 tolerance precision on analytic arc identities in RopeGuideTests.cs — Relax float comparison tolerances to 1e-3f per testing guidance.
  - `[false]` `[reject]` VG-1: Unverified mechanical domain port validation — Belongs to Story 11.1b Conveyor, not Story 9A.46a.
  - `[false]` `[reject]` VG-2: Unverified DrivenSurface validation — Belongs to Story 11.1b Conveyor, not Story 9A.46a.
  - `[false]` `[reject]` VG-3: Unverified RunControl non-lane binding validation — Belongs to Story 11.1b Conveyor, not Story 9A.46a.
  - `[false]` `[reject]` VG-4: Unverified mechanical transmission and driven surface physics — Belongs to Story 11.1b Conveyor, not Story 9A.46a.
  - `[false]` `[reject]` EC-1: Non-mechanical port declares non-zero CoordinatePerRadian — Belongs to Story 11.1b Conveyor, not Story 9A.46a.
  - `[false]` `[reject]` EF-2: Implicit joint inference in Conveyor compilation — Belongs to Story 11.1b Conveyor, not Story 9A.46a.
  - `[false]` `[reject]` BH-1/BH-2/BH-3/VG-Other: Candidate staging and Conveyor contamination — Merged into the root-cause candidate staging patch entries above.


## Verification

**Commands:**
- `test "$(node --version)" = v26.12.1`
- `dotnet build CuriousContraptions.slnx -c Debug --nologo` -- expected: 0 warnings, 0 errors.
- `dotnet test CuriousContraptions.tests/CuriousContraptions.tests.csproj -c Debug --nologo` -- expected: all pass, 0 failed, Skipped: 0.
- Run full verification block from `docs/delivery-workflow.md#local-test-authority` -- expected: all 5 test projects pass with Skipped: 0.
- Run `.githooks/pre-commit` on staged candidate -- expected: exit 0, Anvil clean.

## Final Diff Scope Analysis
- Total modified files: 31 files, +571 / -220 lines.
- Untracked files: `CuriousContraptions.tests/RopeGuideTests.cs`, `_bmad-output/implementation-artifacts/spec-9a-46a-declared-route-guides.md`.
- No Conveyor leaks, no mechanical transmission code, no driven surface code.
- All changes strictly implement declared route guides, generic coordinate observations, FlatBuffers wire protocol updates, caller route frame adaptations, and comprehensive behavioral tests.
- Final diff scope: Pass.

## Auto Run Result
- Full local solution test round: 5,586 tests passed across 5 test projects (`WorkerAotGuard.Tests`: 17, `Coverage.Tests`: 77, `BundleElements.Tests`: 236, `E2eRunner.Tests`: 192, `CuriousContraptions.tests`: 5,064).
- Zero failures, `Skipped: 0`.
- All 13 review findings resolved; zero unresolved defects.
- Element-Free Engine Gate: Pass (0 element-specific code in engine).
- Forward-Only: Pass (deleted legacy `PulleyRadius`, `PulleySocketDepth`, `PulleyWheelRead`, `inputs.Pulleys`; 0 backwards compatibility shims).
