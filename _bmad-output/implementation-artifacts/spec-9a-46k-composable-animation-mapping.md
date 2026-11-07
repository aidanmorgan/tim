---
title: '9A.46k — Choose visual responses through declaration data'
type: 'refactor'
created: '2026-11-07'
status: 'done'
baseline_revision: '73cdbf4a80f852c592eec39ad6c6249c17148304'
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

**Problem:** Story 9A.46k resolves Findings A6, E7, and V3:
1. `PresentationDriverRules.Supports` currently enforces a driver-specific curve whitelist that hardcodes which curves each observation driver may use (e.g., only `CommittedOscillatorPhase` may use `ExponentialFollow`, only `CommittedContactLoadPhase` may use `CubicSmoothStep`, only specific drivers may use `SmoothStep`).
2. Ad-hoc fields and hidden bypasses proliferate across presentation compilation and evaluation:
   - `Intermediate` exists on `PresentationBinding` solely to supply the underweight colour for `PressurePlate`, accompanied by an ad-hoc `track.Rest` calculation in `WorkshopDeclaredPresentation.QueryContactLoad`.
   - `PresentationTimerPhase Phase` exists on `PresentationBinding` solely so `DelayBox` can declare 3 separate bindings writing to the same `(Node, Property)`, accompanied by a hidden sample suppression check in `WorkshopDeclaredPresentation.Advance` (`(d.Driver == PresentationDriver.CommittedTimerPhase && track.Input == 0) continue`) and a writer whitelist exception in `BundleValidator` (`(Node, Property, Phase)`).
   - `CommittedWheelAngle` has a special-case bypass in `WorkshopDeclaredPresentation.Advance` (`|| d.Driver == PresentationDriver.CommittedWheelAngle`) that bypasses `float.Lerp(From, To, amount)`, preventing declared wheel scale or gear ratio.
   - `PresentationValueMapping.ActuatorStateValues` is hardcoded strictly to `CommittedActuatorState`, preventing other 3-state observations from sharing a unified state mapping.

**Approach:**
1. Separate typed observation selection (`PresentationDriver`) from bounded typed transforms/state maps (`PresentationValueMapping`, `PresentationStateValues`), predicates, interpolation (`PresentationCurve`), and overlap (`PresentationOverlap`).
2. Generalize `PresentationActuatorStateValues` to `PresentationStateValues(float State0, float State1, float State2)` under `PresentationValueMapping.StateValues`. Map discrete states consistently:
   - `ActuatorState`: State 0 = Unpowered, State 1 = Powered, State 2 = Blocked.
   - `ContactLoadPhase`: State 0 = Unloaded (None), State 1 = Loaded, State 2 = Underweight.
   - `TimerPhase`: State 0 = Ready, State 1 = Counting, State 2 = Finished.
   Delete `Intermediate` and `track.Rest` from `PresentationBinding`, `CompiledPresentationBinding`, and `WorkshopDeclaredPresentation`.
3. Update `DelayBox` to declare a single binding per indicator colour using `StateValues`, delete `PresentationTimerPhase Phase` from `PresentationBinding` and `CompiledPresentationBinding`, delete the hidden `track.Input == 0` sample suppression bypass in `WorkshopDeclaredPresentation.Advance`, and enforce strict single-property ownership in `BundleValidator`: `(string Node, PresentationProperty Property)` uniqueness per element.
4. Delete the driver-specific curve whitelist in `PresentationDriverRules.Supports`. Replace it with mathematical curve and timing validation (e.g., `ExponentialFollow` requires `Response > 0` on continuous level drivers; `SmoothStep`/`CubicSmoothStep`/`Linear` work across level and event drivers; `LinearDecay`/`SineSquaredPulse` work on pulses/events with `Duration > 0`).
5. Delete the wheel-bypass in `WorkshopDeclaredPresentation.Advance`. Evaluate wheel rotation using standard `float.Lerp(From, To, amount)` so packages declare gear ratio, scale, or inversion via `From` and `To` (target scale/offset).
6. Expose raw phase/coordinate values; preserve Clock pendulum sine and midpoint in package data via `SineCyclesToUnit` with `From: -.42f, To: .42f`, where stopped/zero phase naturally maps to 0.5 (midpoint 0).
7. Update callers, packages (`PressurePlate`, `DelayBox`, `PoweredGate`, etc.), serializers, and tests together.

## Boundaries & Constraints

**Always:** Preserve FR-01/03/06/07/09, PERF-24/25/26, 46a-j prerequisites, dedicated-worker C# WASM SIMD physics and animation pipeline, exact authored Reset and canonical serialized fields. One-way physics -> animation -> renderer. Refuse invalid inputs atomically with typed errors. Ensure 100% test pass rate with zero skips.

## Element-Free Engine Questions (Owner Contract, 4 November 2026)

1. **What is the generic capability?**
   Composable visual animation mapping: decoupling typed simulation observation (`PresentationDriver`) from bounded value transforms (`PresentationValueMapping`), interpolation curves (`PresentationCurve`), timing, and property targeting.

2. **Where is it declared?**
   In package declarations (`elements/<Name>/<Name>ElementDeclaration.cs`) via `PresentationBinding` records, serialized into the element bundle.

3. **How does the engine remain ignorant of specific elements?**
   The engine (`WorkshopDeclaredPresentation`, `WorkshopSceneCompiler`, `WorkshopWireFlatBuffers`) queries observations strictly by `PresentationDriver`, applies transforms via `PresentationDriverRules.Map` / `PresentationStateValues.Map`, evaluates curves uniformly across all tracks, and registers them with the animation batch. Zero element identities, names, or ad-hoc special cases exist in the engine.

4. **What data types represent the capability?**
   `PresentationDriver`, `PresentationValueMapping`, `PresentationStateValues`, `PresentationCurve`, `PresentationOverlap`, `PresentationProperty`.

5. **How is it scheduled and solved?**
   During scene compilation, presentation tracks are compiled into flat structs. At each commit read, observations are queried and mapped. During `Advance`, the animation worker evaluates the active curves/follow targets and produces samples for visual application.

6. **How is state saved and restored?**
   Initial values are computed from declaration data using `PresentationDriverRules.InitialValue`. Reset evaluates initial values and restores visual nodes to their rest states.

7. **What is deleted?**
   - Driver-specific curve whitelist switch in `PresentationDriverRules.Supports`.
   - Ad-hoc `Intermediate` property on `PresentationBinding`, `CompiledPresentationBinding`, and `CapabilityPresentationBindingRecord`.
   - Ad-hoc `Phase` property on `PresentationBinding`, `CompiledPresentationBinding`, and `CapabilityPresentationBindingRecord`.
   - `track.Rest` and special-case underweight calculation in `WorkshopDeclaredPresentation.QueryContactLoad` and `Advance`.
   - Hidden sample suppression bypass in `WorkshopDeclaredPresentation.Advance` (`(d.Driver == PresentationDriver.CommittedTimerPhase && track.Input == 0) continue`).
   - Wheel-bypass in `WorkshopDeclaredPresentation.Advance` (`|| d.Driver == PresentationDriver.CommittedWheelAngle`).
   - `PresentationTimerPhase` uniqueness hack in `BundleValidator` (`writers.Add((Node, Property, Phase))`).
   - `PresentationActuatorStateValues` (superseded by generic `PresentationStateValues`).

8. **What remains uncertain?**
   - None. All requirements, contracts, and affected files are identified and bounded.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|---|---|---|---|
| Unfamiliar source-curve: Activation with ExponentialFollow | `CommittedActivation`, `ExponentialFollow`, `Response = 20`, `From = 0`, `To = 1` | Value smoothly exponential-follows 0 -> 1 when activated; smoothly decays 1 -> 0 when deactivated | No error |
| Unfamiliar source-curve: Activation with CubicSmoothStep | `CommittedActivation`, `CubicSmoothStep`, `Duration = 0.25`, `From = 0`, `To = 1` | Value smoothly cubic-interpolates between 0 and 1 over 0.25 seconds | No error |
| Unfamiliar curve-source: WheelAngle with ExponentialFollow | `CommittedWheelAngle`, `ExponentialFollow`, `Response = 15`, `From = 0`, `To = 1` | Wheel rotation smoothly tracks guide angle via exponential follow rather than jumping | No error |
| Target scale and offset: Inverted Wheel | `CommittedWheelAngle`, `Linear`, `From = 0`, `To = -1` | Wheel rotates in opposite direction with exact 1:1 scale (`-angle`) | No error |
| Target scale and offset: 2:1 Wheel Ratio | `CommittedWheelAngle`, `Linear`, `From = 0`, `To = 2` | Wheel rotates at twice the guide angle (`2 * angle`) | No error |
| Unified StateValues: PoweredGate | `CommittedActuatorState`, `StateValues(85/255, 247/255, 232/255)` | Unpowered = 85/255, Powered = 247/255, Blocked = 232/255 | No error |
| Unified StateValues: PressurePlate | `CommittedContactLoadPhase`, `StateValues(85/255, 247/255, 232/255)` | None = 85/255, Loaded = 247/255, Underweight = 232/255 | No error |
| Unified StateValues: DelayBox | `CommittedTimerPhase`, `StateValues(85/255, 232/255, 247/255)` | Ready = 85/255, Counting = 232/255, Finished = 247/255; single binding per property | No error |
| Clock stopped pendulum | `CommittedOscillatorPhase`, `SineCyclesToUnit`, `From = -.42, To = .42` | Phase 0 when stopped maps to 0.5 via sine; Lerp(-.42, .42, 0.5) = 0 (straight down) | No error |
| Conflicting property writers | Two bindings targeting same `(Node, Property)` on one element | Bundle validation failure: duplicate property writer | Rejected with `BundleErrorCode.UnresolvedReference` |
| Invalid transform: Missing StateValues | `Mapping = StateValues` but `StateValues = null` | Bundle validation failure: missing state values | Rejected with `BundleErrorCode.OutOfRange` |
| Invalid transform: Non-finite StateValues | `Mapping = StateValues` with `State0 = NaN` | Bundle validation failure: nonfinite state values | Rejected with `BundleErrorCode.OutOfRange` |
| Invalid timing: ExponentialFollow with Response <= 0 | `Curve = ExponentialFollow`, `Response = 0` | Bundle validation failure: invalid response timing | Rejected with `BundleErrorCode.OutOfRange` |
| Event vs Level control | `Driver = Event` with level signal, or `Driver = Committed*` with event | Validation failure: driver-source mismatch | Rejected with `BundleErrorCode.OutOfRange` |
| Authoring Reset | Player resets simulation after run | All visual properties restored to exact authored initial values | No error |

</intent-contract>

## Code Map

Candidate: isolated `/Users/aidan/.codex/worktrees/d1fb/tim`, branch `story-9a-46k-composable-animation-mapping`, published baseline `73cdbf4a80f852c592eec39ad6c6249c17148304`. Clean checkout on a fresh branch. Fresh implementation owner Antigravity subagent (no inherited history), retained through all repairs.

Authority: primary `_bmad-output/planning-artifacts/epics.md`; Findings A6, E7, V3; requirements FR-06/07, PERF-24/25/26. Prerequisites 46a-j published at baseline.

## Verification & Terminal Review

- **Maintainability Gate E9:**
  Command: `dotnet run --file /Users/aidan/dev/personal/tim/tools/anvil/VerifyPolicy.cs -- --maintainability --project-root /Users/aidan/.codex/worktrees/d1fb/tim`
  Result: `Maintainability gate (CA1505 index >= 20, CA1502 complexity <= 25, balanced coupling; test projects excluded): violations=0 (metrics=0, coupling=0). PASS`
  Coupling dependencies: `contract=2987, model=4, intrusive=0; violations=0`.
- **Compiler Proof Captures:**
  All 5 binlogs captured cleanly with 0 warnings and 0 errors: `playtest.binlog`, `solution.binlog`, `web.binlog`, `check-staged-tests.binlog`, `verify-policy.binlog`.
- **Full Solution Test Round:**
  - `WorkerAotGuard.Tests.dll`: 17 passed, 0 failed, 0 skipped
  - `Coverage.Tests.dll`: 77 passed, 0 failed, 0 skipped
  - `BundleElements.Tests.dll`: 238 passed, 0 failed, 0 skipped
  - `E2eRunner.Tests.dll`: 192 passed, 0 failed, 0 skipped
  - `CuriousContraptions.tests.dll`: 5,377 passed, 0 failed, 0 skipped
  - **Total**: 5,901 tests passed, 0 failed, 0 skipped.
- **Independent Adversarial Review:**
  6 Auto review layers executed; all 7 confirmed findings repaired by the implementation owner and re-verified:
  1. PressurePlate discrete 3-state evaluation cleaned; special cases eliminated in engine.
  2. Chronological domain order for `ActivationTimerPhase` aligned (`Ready => State0, Counting => State1, Finished => State2`).
  3. Binary fallback for `CommittedContactLoadPhase` deleted; discrete 3-state drivers disallow `Identity`.
  4. Initial value calculation and Reset in `PresentationDriverRules` and `MachinePart` properly restore State0.
  5. `StateValues` components validated against `AdmitsValue` in `BundleValidator`.
  6. Curve support rules cleanly separated for continuous vs level drivers.
  7. Test suite `ComposableAnimationMappingTests.cs` (18 tests) added and passing.
