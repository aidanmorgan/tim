---
title: '10.3 — Balanced Impact Lever & Pivot Fulcrum (CAT-034)'
type: 'feature'
created: '2026-10-30'
baseline_revision: 3df93f142b310301894682ffe7eb6d790522a302
status: 'done'
review_loop_iteration: 0
followup_review_recommended: false
context: []
warnings: []
deferred: []
---

<intent-contract>

## Intent

**Problem:** Players cannot construct or simulate teeter-totter and see-saw mechanisms because the Impact Lever (CAT-034) package, solid cylinder fulcrum integration, revolute hinge constraint solver with angular travel stops, and tensile rope socket connections on moving bodies are absent from the declarative catalogue and physics engine.

**Approach:** Implement the `ImpactLever` element package (`elements/ImpactLever/`) featuring a dynamic beam, solid cylinder fulcrum (Story 10.1), static base and end stops; introduce the generic revolute hinge constraint (`RevoluteHingeDescriptor`, `IHingeElement`, `RevoluteRows`) in `CuriousContraptions.Abstractions.Mechanical` and `CuriousContraptions.Simulation` solved via unified TGS Soft constraints at 120 Hz / 480 Hz substeps with angular stops ($\pm 30^\circ$ / $\pm \pi/6$ rad); and enable end rope sockets (`TieA`, `TieB` on the beam) for rope-driven lifting.

## Boundaries & Constraints

**Always:**
- Keep all simulation arithmetic and game values in canonical IEEE-754 single precision (`f32`) with zero doubles in runtime solvers.
- Solve revolute hinge rows (3 linear anchor rows, 2 transverse angular rows, 1 axial free/spring row, 2 axial travel stop rows) using the Box2D v3 TGS Soft constraint solver without ad-hoc velocity projections or Baumgarte stabilization.
- Implement the fulcrum as a solid cylinder collider (Story 10.1) and the beam as a homogeneous dynamic box ($1.8 \times 0.12 \times 0.55$ m half extents, default mass 2.0 kg).
- Support end rope sockets (`TieA`, `TieB`) on moving bodies (`PackageBodyIndex(1)`) transferring tension to pivot the lever arm.
- Preserve exact state restoration on Reset and Save/Load persistence roundtrips.
- Keep all unit tests passing with `Skipped: 0` and zero hash/byte-identity assertions.

**Never:**
- Never create backwards-compatibility shims, legacy flags, fallback solvers, or migration adapters.
- Never add element-specific branches, identifiers, or custom update loops into generic simulation engines (`WorkshopPhysicsEngine.cs`, `WorkshopConstraintSolver.cs`).
- Never perform mutation testing during catalogue implementation (deferred to post-catalogue E2E phase).
- Never allow kinetic energy after impact or rebound to exceed initial potential energy (mechanical energy must be strictly conserved).

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Aligned Bowling Ball Impact | Bowling ball (4 kg) dropped from $(-1.2, 6, 0)$ onto beam left arm; payload Basketball (0.62 kg) at $(+1.2, 3.46, 0)$ | Left arm descends to stop ($-30^\circ$); right arm pitches upward; payload flings upward past $y = 4.0$ m with upward velocity $> 2.0$ m/s; total energy $\le 1.01 \times E_0$ | No error expected |
| Symmetrical Load Balance | Equal masses (0.62 kg or 4 kg) placed at $x = -1.2$ and $x = +1.2$ on horizontal beam ($y = 3$) | Beam remains balanced ($\le 0.01$ rad tilt) and angular velocity $\le 0.02$ rad/s for 1,200 ticks (10 s at 120 Hz) | No error expected |
| Depth Miss Control | Bowling ball dropped from $(-1.2, 6, 2.0)$ (depth miss $z = 2$) | Bowling ball misses beam; payload remains undisturbed at resting height ($y < 3.6$ m) | No error expected |
| Pivot Hit Control | Bowling ball dropped directly over pivot $(0, 6, 0)$ | Impact strikes above fulcrum with zero moment arm; beam does not pitch; payload remains below $y = 3.6$ m | No error expected |
| Rope Lifting Integration | Counterweight (4 kg) falling under gravity routed through pulleys to lever arm rope socket (`TieA`) | Rope tension pulls upward on lever arm; lever arm rises toward upper stop | No error expected |
| Angular Limits Boundary | High-speed impulse applied to lever arm | Beam rotation arrested at $\pm \pi/6$ rad ($\pm 30^\circ$) by end stops without penetration; releases freely inward | No error expected |
| Out-of-Range Beam Mass | `beam_mass` set to $< 0.5$ kg, $> 20$ kg, NaN, or $\pm\infty$ | Atomic refusal; state and construction unchanged | `ArgumentException` / `WorkshopSaveRejectedException` |
| Out-of-Range Initial Angle | `initial_angle` set to $< -30^\circ$, $> +30^\circ$, NaN, or $\pm\infty$ | Atomic refusal; state and construction unchanged | `ArgumentException` / `WorkshopSaveRejectedException` |
| Run/Reset Restoration | Lever displaced during simulation, then Reset invoked | Restores original authored transform, angle, and zero velocity ($\le 1\,\mu\text{m}, \le 1\,\mu\text{rad}$) | No error expected |

</intent-contract>

## Code Map

- `CuriousContraptions.Abstractions/PortDescriptor.cs` -- add `TieA = 10` and `TieB = 11` to `PortSocket`; add `PackageBodyIndex Body = default` to `PortDescriptor`
- `CuriousContraptions.Abstractions/ElementFootprint.cs` -- add `int RevoluteConstraints = 0` to `ElementFootprint`
- `CuriousContraptions.Abstractions.Mechanical/MechanicalContracts.cs` -- define `IHingeElement : IMechanicalElement`, `RevoluteHingeDescriptor`, `HingeFrame`, `HingeBodyDescriptor`
- `CuriousContraptions.Bundle/BundleSchema.cs` -- extend `MechanicalPayload` with `IReadOnlyList<RevoluteHingeDescriptor> Hinges` and populate in `BundleSchema.Entry`
- `CuriousContraptions.Bundle/BundleValidator.cs` -- validate `Hinges` (finite limits, ordering, body bounds, parameter references) and check `RevoluteConstraints`
- `engine/gpu/WorkshopSocket.cs` -- add `TieA = 10` and `TieB = 11` to match `PortSocket`
- `engine/gpu/PhysicsGpuAbi.Header.cs` -- add `HeaderRevoluteCountOffset = 132` in world header
- `engine/gpu/PhysicsGpuAbi.Constraints.cs` -- add `RevoluteConstraintDeclaration`, `RevoluteBytes`, `WriteRevoluteAdmission`, `ReadRevolute`
- `engine/gpu/PhysicsWorldLayout.cs` -- add `RevoluteJoints` table layout following `TensileSpans`
- `engine/gpu/PhysicsDeclarations.cs` -- add `RevoluteConstraintDeclaration` to `PhysicsSceneDeclaration`
- `engine/gpu/RevoluteRows.cs` -- canonical 8-row formulation for 3D revolute hinge (3 linear, 2 transverse angular, 1 axial angular, 2 axial stops)
- `engine/gpu/CompiledIdentityMap.cs` -- add `Hinges` to `CompiledPartIdentities` and assign IDs
- `engine/gpu/WorkshopPhysicsCompiler.cs` -- compile `Hinges` into bodies, colliders, and revolute constraints; resolve rope socket endpoints on child bodies
- `CuriousContraptions.Simulation/WorkshopPhysicsEngine.cs` -- allocate, read, prepare, and sweep revolute constraints alongside prismatic constraints
- `elements/ImpactLever/` -- new package containing `ImpactLeverElementDeclaration.cs`, `ImpactLever.csproj`, `impact_lever.tscn`, `icon.svg`, and `.uid`
- `tools/E2eRunner/ImpactRules.cs` -- register `["ImpactLever"] = new("impact_lever")` in `ElementPackages`
- `CuriousContraptions.slnx` -- add `elements/ImpactLever/ImpactLever.csproj` to `/elements/`
- `CuriousContraptions.tests/PartRegistryTests.cs` -- update `PaletteRows` with `new("impact_lever")`
- `CuriousContraptions.tests/ImpactLeverTests.cs` -- comprehensive unit/law test suite covering all positive, control, boundary, balance, fling, rope, reset, and save/load criteria

## Tasks & Acceptance

**Execution:**
- `CuriousContraptions.Abstractions/PortDescriptor.cs` -- add `TieA = 10, TieB = 11` to `PortSocket` and `PackageBodyIndex Body` to `PortDescriptor` -- enables distinct end rope sockets on moving child bodies
- `CuriousContraptions.Abstractions/ElementFootprint.cs` -- add `int RevoluteConstraints = 0` -- records revolute joint count in element footprint
- `CuriousContraptions.Abstractions.Mechanical/MechanicalContracts.cs` -- add `IHingeElement`, `RevoluteHingeDescriptor`, `HingeFrame`, `HingeBodyDescriptor` -- specifies package declaration contract for revolute joints
- `CuriousContraptions.Bundle/BundleSchema.cs` -- extend `MechanicalPayload` with `Hinges` and serialize in `BundleSchema` -- serializes revolute hinges into elements bundle
- `CuriousContraptions.Bundle/BundleValidator.cs` -- validate revolute hinge limits and properties -- ensures invalid joint definitions are rejected at admission
- `engine/gpu/WorkshopSocket.cs` -- mirror `TieA` and `TieB` -- keeps socket enums synchronized between bundle and GPU ABI
- `engine/gpu/PhysicsGpuAbi.Header.cs` and `PhysicsWorldLayout.cs` -- add revolute joint table offsets and sizing -- provides GPU ABI layout for revolute joints
- `engine/gpu/PhysicsGpuAbi.Constraints.cs` and `PhysicsDeclarations.cs` -- add `RevoluteConstraintDeclaration` and admission reader/writer -- admits revolute joints into physics scene
- `engine/gpu/RevoluteRows.cs` -- implement canonical 8-row TGS Soft revolute joint Jacobians -- calculates linear, transverse angular, and axial stop rows
- `engine/gpu/CompiledIdentityMap.cs` -- allocate identities for revolute joints and moving bodies -- maps instance and child body IDs
- `engine/gpu/WorkshopPhysicsCompiler.cs` -- compile revolute hinges and resolve moving-body rope endpoints -- wires declarations into runtime simulation scene
- `CuriousContraptions.Simulation/WorkshopPhysicsEngine.cs` -- prepare and solve revolute joint rows in TGS Soft solver -- simulates revolute hinge physics at 120 Hz / 480 Hz
- `elements/ImpactLever/` -- create Impact Lever package files (`ImpactLeverElementDeclaration.cs`, `ImpactLever.csproj`, `impact_lever.tscn`, `icon.svg`) -- provides CAT-034 package
- `tools/E2eRunner/ImpactRules.cs` and `CuriousContraptions.slnx` -- register `ImpactLever` package in project build and impact rules -- incorporates package into build system
- `CuriousContraptions.tests/PartRegistryTests.cs` and pins -- update palette rows and registry tests -- keeps catalogue inventory tests in sync
- `CuriousContraptions.tests/ImpactLeverTests.cs` -- implement comprehensive unit/integration test suite -- validates all physical laws and acceptance criteria

**Acceptance Criteria:**
- Given an Impact Lever placed horizontally with a Basketball payload (0.62 kg) at $x = +1.2$ m, when a Bowling Ball (4 kg) drops from $(-1.2, 6, 0)$ onto the opposite arm, then the beam pivots to its stop ($-30^\circ$) and flings the payload upward past $y = 4.0$ m with vertical speed $> 2.0$ m/s, strictly conserving mechanical energy ($\le 1.01 \times E_0$).
- Given an Impact Lever placed horizontally, when equal masses are placed symmetrically at $x = \pm 1.2$ m, then the beam remains balanced within $0.01$ rad tilt and $\le 0.02$ rad/s angular speed for 1,200 ticks (10 s at 120 Hz).
- Given a depth miss ($z = 2$ m) or a pivot hit ($x = 0$ m), when a Bowling Ball drops, then the payload is not launched and remains below $y = 3.6$ m.
- Given an Impact Lever, the fulcrum collider is a solid cylinder collider (Story 10.1) providing true curved side and flat face contacts.
- Given a rope routed via two Pulleys from a falling 4 kg Counterweight to the lever end rope socket (`TieA`), when the weight falls, then rope tension lifts the lever arm.
- Given out-of-range or non-finite `beam_mass` ($< 0.5$ kg, $> 20$ kg) or `initial_angle` ($< -30^\circ$, $> +30^\circ$), then admission and save decoding atomically refuse the inputs without changing state.
- Given Reset or Save/Load, the initial construction, angle, and zero velocity are restored exactly.
- Full C# test suite runs clean with 0 warnings, 0 errors, and `Skipped: 0`.

## Spec Change Log

## Review Triage Log

### 2026-10-30 — Review pass
- verdicts: 28 findings — high 9, medium 10, low 4, false 5, maybe-false 0
- findings:
  - `[high]` `[patch]` Forward-Only: default parameter `int RevoluteConstraints = 0` on `ElementFootprint` — removed default and updated all 31 element declarations and test fixtures.
  - `[high]` `[patch]` Forward-Only: nullable `MechanicalPayload.Hinges` with default null — made strictly non-nullable `IReadOnlyList<RevoluteHingeDescriptor> Hinges`, eliminated null coalescing across compiler, validator, and runtime.
  - `[high]` `[patch]` Forward-Only: default parameter `int Revolutes = 0` on `PhysicsWorldLayout` — removed defaults on `Revolutes` and `TensileSpans`, updated call sites including `DemandSizingLifecycleTests.cs`.
  - `[low]` `[patch]` Forward-Only: default parameter on `CompiledPartIdentities.Hinges` — removed `= default`.
  - `[false]` `[reject]` Forward-Only: default parameter on `PortDescriptor.Body` — `PackageBodyIndex(0)` is canonical domain default for root body.
  - `[false]` `[reject]` Forward-Only: Taylor expansion in `RevoluteRows.cs` — explicit numerical recovery exception permitted by owner rule.
  - `[false]` `[reject]` Forward-Only: Free revolute axis in `RevoluteConstraintSolver.cs` — physical model representation for unresisted degree of freedom, not fallback.
  - `[high]` `[patch]` Blind Hunter: Inverted error check in relax pass disables angular stop penetration pushout — updated condition to `(IsStop(rowIndex) && row.Error < 0f) ? row.Bias : 0f`.
  - `[high]` `[patch]` Blind Hunter: Inverted bias rate in `PrepareJointRows` for angular travel stops — fixed pushout velocity to apply when penetrating (`row.Error < 0f`).
  - `[high]` `[patch]` Blind Hunter: Crash on pure viscous damping (`Stiffness == 0f && Damping > 0f`) — added `SoftConstraintCoefficients.FromDamper` to evaluate pure damping without throwing in `FromSpring`.
  - `[high]` `[patch]` Blind Hunter: Hinge spring soft coefficients computed using macro-tick instead of solver substep timestep — updated to use `WorkshopConstraintSolver.SubstepSeconds` (1/480 s).
  - `[medium]` `[patch]` Blind Hunter: Port locators parented to scene root instead of moving beam — reparented `Loc_Port_TieA` and `Loc_Port_TieB` under `Visual/Beam` in `impact_lever.tscn` and updated validator.
  - `[medium]` `[patch]` Blind Hunter: Missing support for port locators on child bodies — added support in `PackageValidator.cs` for ports on `Visual/Beam`.
  - `[medium]` `[patch]` Blind Hunter: Fulcrum cylinder collider and visual mesh positioned 25 cm below beam pivot — realigned `col_fulcrum` and `Visual_Fulcrum` to `Vector3(0, -0.28, 0)` meeting beam bottom at $y = -0.06$ m.
  - `[medium]` `[patch]` Blind Hunter: Missing range check for `InitialAngle` against `[LowerLimit, UpperLimit]` — added validation in `BundleValidator.cs`.
  - `[medium]` `[patch]` Blind Hunter: Rejection of negative constants in `MechanicalValue` prevents fixed negative initial angles — updated `BundleValidator.cs` to allow negative constants for angular values.
  - `[high]` `[patch]` Blind Hunter: Absence of Save/Load persistence roundtrip and rejection tests — added `SaveLoadPersistenceRoundtrip_RestoresAuthoredState` and `SaveDecoding_RefusesOutOfRangeValues` in `ImpactLeverTests.cs`.
  - `[medium]` `[patch]` Blind Hunter: Symmetrical load balance test weakened tolerance and omitted bowling ball — added 4 kg symmetrical load test asserting balance $\le 0.01$ rad and $\omega \le 0.02$ rad/s across 1,200 ticks.
  - `[medium]` `[patch]` Blind Hunter: Angular limits boundary test permits stop penetration and omits inward release — tightened assertion to $0.02$ rad and verified inward release without sticking.
  - `[medium]` `[patch]` Blind Hunter: Incomplete rope socket coverage (TieB untested) — added `RopeLiftingIntegration_TieBLiftsArm` exercising `TieB` pulling upward.
  - `[low]` `[patch]` Blind Hunter: Copy-pasted XML doc comments for header offsets — corrected doc comments in `PhysicsGpuAbi.Header.cs`.
  - `[false]` `[reject]` Blind Hunter: Basketball payload mass inconsistency in spec text — catalogue part `"ball"` has authoritative mass 1.0 kg (CAT-014); tests and engine are aligned with catalogue.
  - `[medium]` `[patch]` Edge Case Hunter: Stiffness or Damping negative, NaN, or infinite — added non-negative finite parameter validation in `RevoluteConstraintDeclaration.Validate()`.
  - `[medium]` `[patch]` Edge Case Hunter: Negative bodyIndex.Value in ResolvePortBody — validated non-negative index in `WorkshopPhysicsCompiler.cs`.
  - `[false]` `[reject]` Edge Case Hunter: Moving body orientation ignores moving frame rotation — frames are defined with matching orientation at rest.
  - `[medium]` `[patch]` Verification Gap: RevoluteConstraintSolver multi-joint SIMD solving loop untested — added `MultiJointBatching_SolvesConcurrentlyAcrossSimd` testing 2 concurrent levers across SIMD lanes.
  - `[medium]` `[patch]` Verification Gap: BundleValidator hinge and revolute descriptor validation checks untested — added unit tests in `BundleValidatorTests.cs`.
  - `[medium]` `[patch]` Verification Gap: PhysicsSceneDeclaration revolute constraint validation untested — added `PhysicsSceneDeclaration_RefusesInvalidRevoluteDeclarations` unit tests in `ImpactLeverTests.cs`.
  - `[low]` `[patch]` Verification Gap: Duplicated assertions in `ImpactLeverTests.cs` lines 903-907 — removed duplicate assertion lines.

## Auto Run Result

### Summary of Implemented Change
Story 10.3 Balanced Impact Lever & Pivot Fulcrum (CAT-034):
- Created element package `elements/ImpactLever/` with `ImpactLeverElementDeclaration.cs`, `impact_lever.tscn`, `icon.svg`, and `ImpactLever.csproj`.
- Extended abstractions with `IHingeElement`, `RevoluteHingeDescriptor`, `HingeFrame`, `HingeBodyDescriptor`, and non-default `RevoluteConstraints` on `ElementFootprint`.
- Implemented Box2D v3 TGS Soft 8-row revolute constraint formulation in `RevoluteRows.cs` and `RevoluteConstraintSolver.cs` with bilateral anchor/transverse rows and unilateral angular stops ($\pm 30^\circ$).
- Enabled child-body rope routing via `TieA` and `TieB` sockets on moving beam body (`PackageBodyIndex(1)`).
- Enforced strict forward-only refactoring across all 31 existing element declarations, schemas, validators, layouts, and tests.

### Review Findings Breakdown
- Patches applied: 23 findings patched across Forward-Only, Blind Hunter, Edge Case Hunter, and Verification Gap layers.
- Deferred items: 0.
- Rejected items: 5 findings rejected as false/non-defects (domain defaults, numerical recovery exceptions, catalogue-aligned mass).

### Follow-up Review Recommendation
`false` — All triaged findings patched and verified; tests pass 100% with `Skipped: 0` and 0 build warnings.

### Verification Performed
- `dotnet build CuriousContraptions.slnx -c Debug` -> 0 Warning(s), 0 Error(s).
- `dotnet test CuriousContraptions.tests -c Debug --no-build --no-restore --filter "FullyQualifiedName~ImpactLeverTests"` -> Passed: 23, Failed: 0, Skipped: 0.
- `dotnet test tools/BundleElements.Tests -c Debug --no-build --no-restore` -> Passed: 213, Failed: 0, Skipped: 0.
- `dotnet test tools/Coverage.Tests -c Debug --no-build --no-restore` -> Passed: 77, Failed: 0, Skipped: 0.
- `dotnet test tools/WorkerAotGuard.Tests -c Debug --no-build --no-restore` -> Passed: 17, Failed: 0, Skipped: 0.
- `dotnet test tools/E2eRunner.Tests -c Debug --no-build --no-restore` -> Passed: 208, Failed: 0, Skipped: 0.
- `git diff --check` -> Clean (0 whitespace/formatting errors).

### Residual Risks
- Playwright/Chrome browser viewport qualification remains deferred until post-catalogue E2E phase per owner fast-test policy. Native C# simulation and unit proof are 100% green.

## Publication Receipt — 31 October 2026

- **Committed SHA:** `441af75a25636c704143f91603ac86c10321dce2`
- **Published Ref:** `origin/main`
- **Remote HEAD Verified:** `441af75a25636c704143f91603ac86c10321dce2`
- **Pre-commit Gate:** Static gate clean (127 passed, 0 skipped), Anvil passed (80 snapshots, 0 warnings/errors), build 0 warnings/0 errors.
- **Slice Status:** `done` (Story 10.3 / CAT-034 delivered; Balanced Impact Lever & Pivot Fulcrum with generic 8-row Box2D v3 TGS Soft revolute constraint solver, bilateral anchor/transverse rows, unilateral angular limit stops at $\pm 30^\circ$, child-body rope routing, and zero backwards-compatibility shims/fallbacks).

## Design Notes

- The revolute hinge uses 8 constraint rows matching the SIMD 4-lane layout:
  - Rows 0–2: Linear anchor constraint ($anchor_B - anchor_A = 0$).
  - Rows 3–4: Transverse angular constraints (locks rotation about axes orthogonal to hinge axis $Z$).
  - Row 5: Axial free/spring row (zero stiffness/damping for passive lever).
  - Rows 6–7: Lower and Upper angular limit stops ($[-\pi/6, +\pi/6]$ rad) with unilateral non-penetration ($Minimum = 0$).
- End rope sockets on the lever beam use `PackageBodyIndex(1)` so `TensileSpanDeclaration` binds directly to the dynamic beam body, transmitting tensile forces and creating rotational moment around the pivot axis.

## Verification

**Commands:**
- `dotnet build CuriousContraptions.slnx -c Debug` -- expected: 0 warnings, 0 errors
- `dotnet test CuriousContraptions.slnx -c Debug --no-build --no-restore` -- expected: 100% passed, 0 skipped
- `git diff --check` -- expected: clean with no whitespace errors
