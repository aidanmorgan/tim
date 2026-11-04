---
title: 'Story 9A.46c: Start articulated parts in their declared joint frames'
type: 'feature'
created: '2026-11-05'
baseline_revision: 53736a5998c47fa1197dabf7a0c81dbdd3ef636e
status: 'done'
review_loop_iteration: 0
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

**Problem:** Slider and hinge initial moving-body transforms currently ignore joint-frame rotations (`slider.BaseFrame.Rotation` and `slider.MovingFrame.Rotation`), hardcode `Vector3.UnitY * compression`, and omit inverse `MovingFrame.Rotation` on hinges. When mechanisms with non-identity joint frames or rotated owners are compiled and advanced, solver correction changes authored placement or causes artificial pose jumps.

**Approach:** Derive child moving-body initial transforms from both joint frames, inverse moving frame, and initial scalar coordinate (compression for sliders, angle for hinges) using generic frame algebra:
- For Sliders (Prismatic):
  $$R_B = R_A \cdot R_{baseFrame} \cdot R_{movingFrame}^{-1}$$
  $$axisWorld = Vector3.Transform(Vector3.UnitY, R_A \cdot R_{baseFrame})$$
  $$anchorA = P_A + Vector3.Transform(P_{baseFrame}, R_A)$$
  $$localAnchorBInWorld = Vector3.Transform(P_{movingFrame}, R_B)$$
  $$P_B = anchorA - axisWorld \cdot compression - localAnchorBInWorld$$
- For Hinges (Revolute):
  $$R_{jointA} = R_A \cdot R_{baseFrame}$$
  $$R_{angle} = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, \theta)$$
  $$R_B = R_{jointA} \cdot R_{angle} \cdot R_{movingFrame}^{-1}$$
  $$anchorA = P_A + Vector3.Transform(P_{baseFrame}, R_A)$$
  $$localAnchorBInWorld = Vector3.Transform(P_{movingFrame}, R_B)$$
  $$P_B = anchorA - localAnchorBInWorld$$
- Validate that joint frame orientations are normalized unit quaternions in `BundleValidator.cs`.
- Delete unrotated `UnitY * compression` slider placement and hinge placement omitting inverse `MovingFrame.Rotation` from `WorkshopPhysicsCompiler.cs`.

## Boundaries & Constraints

**Always:**
- Execute physics using canonical IEEE-754 `f32` single precision at 120 Hz with 480 Hz substeps.
- Compose child initial transforms from owner transform ($P_A, R_A$), $BaseFrame$, $MovingFrame$, and initial scalar coordinate without assuming identity rotation or unrotated axes.
- Support arbitrary valid 3D joint-frame rotations, non-zero offsets, and rotated owners.
- Enforce unit quaternion normalization checks on slider $BaseFrame.Rotation$ and $MovingFrame.Rotation$ in `BundleValidator.cs`.
- Initial constraint rows at simulation start must have zero residual error (or exact initial coordinate displacement), with zero initial pose jump on the first tick of an unforced mechanism.
- Zero skipped tests (`Skipped: 0`), no hash/byte assertions, and no mutation testing before the post-catalogue E2E phase.

**Never:**
- No hardcoded `UnitY` translation without joint frame rotation.
- No omitting inverse $MovingFrame.Rotation$ in hinge orientation.
- No element-specific joint frame or solver branches.
- No backwards-compatibility shims, legacy fallbacks, or deprecated properties.
- No browser E2E execution during this slice (remains deferred per `BrowserDeferral.cs`).

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Identity joint frames (regression) | Slider or hinge with identity $BaseFrame$ and $MovingFrame$ | Moving body transforms match existing behavior exactly | No error expected |
| Rotated slider base frame | Slider with $BaseFrame.Rotation$ rotated 90° about Z, compression 0.2m | Moving body translates along rotated axis without initial transverse error | No error expected |
| Rotated slider moving frame | Slider with nonidentity $MovingFrame.Rotation$ | Moving body orientation $R_B$ satisfies $R_B \cdot R_{moving} = R_A \cdot R_{base}$ | No error expected |
| Rotated hinge moving frame | Hinge with $MovingFrame.Rotation$ rotated 45° about Z, initial angle 30° | Moving body orientation $R_B = R_A \cdot R_{base} \cdot R(\theta) \cdot R_{moving}^{-1}$ | No error expected |
| Rotated owner body | Owner body rotated 45° about Y and 30° about X | Child body transforms rotate rigidly with owner | No error expected |
| Unnormalized joint quaternion | Package declares slider or hinge with non-unit quaternion ($L^2 \neq 1$) | Bundle validation rejects with `OutOfRange` | Typed refusal (`OutOfRange`) |
| Non-finite frame numbers | Package declares slider or hinge with NaN or Infinity | Bundle validation rejects with `NonFinite` | Typed refusal (`NonFinite`) |

</intent-contract>

## Design Record (Element-Free Engine Gate)

1. **What behavior is delivered?**
   Articulated child moving bodies (revolute hinges and prismatic sliders) derive their initial world poses from the owner's pose, the declared joint $BaseFrame$, $MovingFrame$, and the initial scalar coordinate (compression / angle), ensuring zero constraint error and zero artificial pose jump on simulation startup.
2. **What is reusable?**
   Generic frame composition algebra in `WorkshopPhysicsCompiler.cs` using standard `Vector3` and `Quaternion` math, universally applicable to any mechanical part.
3. **What is declared?**
   $BaseFrame$ (Position, Rotation) and $MovingFrame$ (Position, Rotation) on `PrismaticSliderDescriptor` and `RevoluteHingeDescriptor`.
4. **How does it execute?**
   Compiler transforms owner cell/local position and rotation by the declared joint frames and initial coordinate, producing the moving body's initial `CellOrigin`, `LocalPosition`, and `CanonicalRotation`. The constraint solver evaluates initial rows with zero residual error.
5. **Who owns changes?**
   Compiler owns generic joint transform algebra. Bundle validator owns joint frame validation. Element packages own joint parameters. Simulation solver solves constraints without element awareness.
6. **What proves generality?**
   Tests exercising nonidentity base and moving rotations, compound 3D orientations, rotated owners, non-zero offsets, and initial coordinate displacements verify that initial joint anchors match and unforced simulation advances without pose jumps.
7. **What is deleted?**
   Unrotated `UnitY * compression` slider placement and hinge placement omitting `Quaternion.Conjugate(hinge.MovingFrame.Rotation)`.
8. **What remains uncertain?**
   None.

## Code Map

- `CuriousContraptions.Bundle/BundleValidator.cs`: Add quaternion normalization validation for `PrismaticSliderDescriptor.BaseFrame.Rotation` and `MovingFrame.Rotation`.
- `engine/gpu/WorkshopPhysicsCompiler.cs`: Update slider and hinge moving-body initial transform derivation to compose both joint frames, inverse moving frame, and initial coordinate.
- `CuriousContraptions.tests/`: Add unit and integration tests in `CuriousContraptions.tests/JointFrameTests.cs` verifying initial anchor alignment, zero pose jumps, rotated frames, and bundle validation refusals.

## Tasks & Acceptance

**Execution:**
- `CuriousContraptions.Bundle/BundleValidator.cs`: Validate unit quaternion for slider $BaseFrame.Rotation$ and $MovingFrame.Rotation$.
- `engine/gpu/WorkshopPhysicsCompiler.cs`:
  - Sliders: compute moving body rotation as $R_B = R_A \cdot R_{base} \cdot R_{moving}^{-1}$ and position taking into account rotated slider axis $axisWorld = Vector3.Transform(Vector3.UnitY, R_A \cdot R_{base})$ and moving frame offset.
  - Hinges: compute moving body rotation including $R_{moving}^{-1}$ and position from matching anchor positions.
- `CuriousContraptions.tests/JointFrameTests.cs`: Add tests proving identity-frame regression, rotated frames, rotated owners, zero pose jumps, and unnormalized frame refusals.
- Verify all existing solution tests and lesson tests pass cleanly.

**Acceptance Criteria:**
- Given nonidentity base and moving frames, nonzero compression/angle and a rotated owner, when compile and advance an unforced mechanism, then initial anchors and coordinate agree with the declaration and no artificial pose jump comes from frame miscompilation.
- And prove identity-frame regression, differently oriented axes, nonzero offsets and invalid-frame refusal; independently derived transform expectations.
- And unrotated UnitY slider placement and hinge placement omitting inverse MovingFrame.Rotation are deleted, with current callers, packages and serialized contracts updated together.

## Spec Change Log

## Review Triage Log

- **Forward-Only Auditor:** PASS (0 findings). Hardcoded UnitY translation deleted; no fallbacks or legacy bridges.
- **Element-Free Engine Auditor:** PASS (0 findings). Generic geometric algebra functions `SliderMovingTransform` and `HingeMovingTransform`. No element coupling.
- **Intent Alignment Auditor:** PASS (0 findings). AC1, AC2, AC3 satisfied without scope creep or behavioral drift.
- **Verification Gap Reviewer:**
  - Finding 1: `RotatedOwnerBody_ChildRigidlyFollowsOwner` tests slider with rotated owner but omitted hinge with rotated owner through `WorkshopPhysicsCompiler.Compile`.
  - Disposition: Accept / Patch. Add test coverage for revolute hinge (`motor`) with rotated owner compiled through `WorkshopPhysicsCompiler.Compile`.
- **Edge Case Hunter:**
  - Finding 1: Unnormalized `rB` quaternion floating drift can cause `CanonicalRotation.ValidateCommitted` to throw `ArgumentException`.
  - Disposition: Accept / Patch. Normalize `rB` via `Quaternion.Normalize` in `SliderMovingTransform` and `HingeMovingTransform`, and `CanonicalRotation.Normalize` in `bodies.Add`.
  - Finding 2: `BundleValidator.cs` reports spurious `OutOfRange` alongside `NonFinite` on infinite quaternion components.
  - Disposition: Accept / Patch. Add `float.IsFinite(rot.LengthSquared())` guard before checking `MathF.Abs(rot.LengthSquared() - 1f) > 1e-4f`.
- **Blind Hunter:**
  - Finding 1: Unnormalized quaternion propagation (same as Edge Case Finding 1).
  - Disposition: Accept / Patch (addressed by Edge Case Finding 1 fix).
  - Finding 2: `localAnchorBInWorld` dilation if quaternion non-unit.
  - Disposition: Accept / Patch (addressed by normalizing `rB` before `Vector3.Transform`).
  - Finding 3: `JointFrameTests.cs:108` missing antipodal sign tolerance.
  - Disposition: Accept / Patch. Update assertion to `MathF.Abs(MathF.Abs(Quaternion.Dot(rotB, baseRot)) - 1f)`.
  - Finding 4: Self-referential test oracle in `JointFrameTests.cs:213-217`.
  - Disposition: Accept / Patch. Calculate expected pose independently via analytic vector and quaternion math.
  - Finding 5: `BundleValidator.cs` asymmetric non-finite error (same as Edge Case Finding 2).
  - Disposition: Accept / Patch (addressed by Edge Case Finding 2 fix).
  - Finding 6: Weak rotation assertion asserting only `W - 1f`.
  - Disposition: Accept / Patch. Assert `springMoving.Rotation == CanonicalRotation.Identity` and `motorMoving.Rotation == CanonicalRotation.Identity`.

## Verification

**Commands:**
- `dotnet build CuriousContraptions.slnx -c Debug --nologo` -- expected: 0 warnings, 0 errors.
- `dotnet test CuriousContraptions.tests/CuriousContraptions.tests.csproj -c Debug --nologo --filter "FullyQualifiedName~JointFrameTests"` -- expected: all pass, 0 failed, Skipped: 0.
- Full local solution test round per `docs/delivery-workflow.md#local-test-authority`:
  - `WorkerAotGuard.Tests.dll`: Passed: 17, Failed: 0, Skipped: 0
  - `Coverage.Tests.dll`: Passed: 77, Failed: 0, Skipped: 0
  - `BundleElements.Tests.dll`: Passed: 236, Failed: 0, Skipped: 0
  - `E2eRunner.Tests.dll`: Passed: 192, Failed: 0, Skipped: 0
  - `CuriousContraptions.tests.dll`: Passed: 5083, Failed: 0, Skipped: 0
  - **Total: 5,605 passed, 0 failed, 0 skipped.**
- `.githooks/pre-commit`:
  - Solution binlog: 0 warnings, 0 errors
  - Playtest binlog: 0 warnings, 0 errors
  - Web binlog: 0 warnings, 0 errors
  - Anvil validation: clean (0 errors, 0 warnings)
  - Static gate tests: Passed: 127, Failed: 0, Skipped: 0
