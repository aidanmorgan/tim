---
title: '9A.46l — Address each actuator controls and observations'
type: 'refactor'
created: '2026-11-08'
status: 'done'
baseline_revision: '9e442366092bb1fa9c9998988a2aa4119c3cd296'
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

**Problem:** Story 9A.46l resolves Findings A7, A11, and E6:
1. `WorkshopRunControl.Slot` and `WorkshopRead.RunControl` resolve `RunControlLane.RevoluteDriveTarget` using an owner-first-match heuristic (`DriveOwner(physics, i) == owner`). If an element has multiple revolute drives, all controls resolve to the first drive.
2. `BundleValidator` currently enforces an artificial restriction (`entry.Mechanical?.Hinges.Count(hinge => hinge.Drive is not null) != 1`) that forbids elements from declaring more than one driven hinge.
3. `RevoluteDriveTargetRead` and the FlatBuffers wire record `RevoluteDriveTargetRecord` identify targets solely by `Owner` (`GpuBodyId`), leaving committed reads unable to distinguish between multiple drives belonging to the same owner.
4. The carried parameter-only-lane-use obligation (from Conveyor / Story 11.1b review) requires validating that any parameter declared as `ParameterRunEdit.RunControl` binds strictly to its run-control lane and is not bound to non-lane properties.

**Approach:**
1. Declare typed local capability targets for run controls:
   - Extend `DeclaredRunControl` with `CoordinateReference? Coordinate = null` (or exact hinge index/coordinate).
   - In `RunControlBindings.Lanes`, inspect each hinge and slider in `BundleEntry.Mechanical` to determine exact lane and coordinate bindings.
   - Enforce the carried parameter-only-lane-use obligation in `BundleValidator`: verify `!RunControlBindings.IsBoundToNonLane(entry, parameter.Key)`.
2. Delete the exactly-one-driven-hinge restriction in `BundleValidator` (`mechanical?.Hinges.Count(...) != 1`). Allow elements to declare multiple driven hinges and sliders with independent parameters.
3. Resolve exact lane slots in `WorkshopSceneCompiler`:
   - Map `control.Coordinate` through `construction.Instances.Identities[owner.Id].Hinges[index].Joint`.
   - Update `WorkshopRunControl.Slot` to look up the exact drive slot matching `joint` (not first-match owner).
4. Update `RevoluteDriveTargetRead` and wire representations:
   - Include `RevoluteConstraintId Joint` on `RevoluteDriveTargetRead` so committed reads identify exact target joints.
   - Update `WorkshopRead.RunControl` to accept exact joint target or row index.
5. Presentation bindings already specify `CoordinateReference? Actuator = null` (referencing `CoordinateKind.Prismatic` or `CoordinateKind.Revolute` with index). Ensure `WorkshopSceneCompiler` and `WorkshopDeclaredPresentation` evaluate them independently across reordered declarations.
6. Verify through comprehensive unit tests covering:
   - Two driven hinges and a driven slider with differing states on an element.
   - Editing one run control changes only the targeted drive.
   - Presentation indicators report correct states regardless of declaration ordering.
   - Rejection of unknown/mismatched capabilities, duplicate writers, out-of-range targets, stale commands, and non-lane parameter bindings.
   - Exact simulation Reset and Save/Load restoration.

## Boundaries & Constraints

**Always:** Preserve FR-01/02/06/07/09, PERF-24/25/26, 46a-k prerequisites, dedicated-worker C# WASM SIMD physics and animation pipeline, exact authored Reset and canonical serialized fields. One-way physics -> animation -> renderer. Refuse invalid inputs atomically with typed errors. Ensure 100% test pass rate with zero skips.

## Element-Free Engine Questions (Owner Contract, 4 November 2026)

1. **What behavior is delivered?**
   Address each actuator's controls and observations by exact typed capability handles. Decouple run controls and presentation indicators from owner-first-match heuristics. Allow elements to declare multiple driven hinges (and sliders) with independent parameters, run controls, and presentation indicators. Enforce the carried parameter-only-lane-use validator rule.

2. **What is reusable?**
   `DeclaredRunControl` with exact coordinate target (`CoordinateReference? Coordinate`), `RevoluteDriveTargetRead` with exact joint identity (`RevoluteConstraintId Joint`), `RunControlBindings.Lanes` resolving exact targets, `WorkshopRunControl.Slot` resolving exact joint targets, and `BundleValidator` single-lane-only parameter rules.

3. **What is declared?**
   Hinges declare `RevoluteDriveDescriptor(TargetSpeed: MechanicalValue.FromParameter("param"), MaxTorque)`; parameters declare `RunEdit: ParameterRunEdit.RunControl`. Presentation bindings declare `Actuator: new CoordinateReference(CoordinateKind.Revolute, index)` or `CoordinateReference(CoordinateKind.Prismatic, index)`.

4. **How does it execute?**
   - Package discovery: bundle reads parameters and mechanical descriptors.
   - Bundle validator: verifies each RunControl parameter binds to exactly one lane and is not used for non-lane properties (`IsBoundToNonLane`). Removes the `count == 1` driven hinge restriction.
   - Scene compilation: `DeclaredPhysics.RunControls` extracts `(Parameter, Lane, SourceNode, Coordinate)`. `WorkshopSceneCompiler` resolves `Coordinate` to `ids.Hinges[index].Joint` and passes it to `core.RunControls.Slot(physics, lane, owner, source, joint)`.
   - Physics solver & publication: `RevoluteDriveTargetRead` carries `(Joint, Owner, TargetSpeed)`.
   - Presentation: `QueryActuatorState` and `QueryActuator` match `actuator.Drive == track.Actuator`.

5. **Who owns changes?**
   Run controls mutate simulation inputs via `WorkshopCommand.RunControl`. Authoring and Reset restore authored initial values. Bundle validation rejects ambiguous or multiple-owner lane bindings atomically.

6. **What proves generality?**
   Unit tests with unfamiliar test elements:
   - An element declaring two driven hinges (`Hinge_0` with parameter `speed_0`, `Hinge_1` with parameter `speed_1`) and a driven slider (`Slider_0`), with differing speeds/states.
   - Reordering presentation declarations (e.g. `Hinge_1` indicator declared before `Hinge_0` indicator, or slider indicator first): indicators accurately read their bound joint.
   - Editing run control for `Hinge_1` changes only `Hinge_1`, while `Hinge_0` and `Slider_0` remain unaffected.
   - Out-of-range target, mismatched capability kind, duplicate writer, stale command controls, and invalid non-lane parameter use.
   - Simulation Reset and Save/Load restoration.

7. **What is deleted?**
   - First-match owner drive lookup in `WorkshopRunControl.Slot` and `read.RunControl`.
   - Exactly-one-driven-hinge restriction in `BundleValidator.cs:133` (`mechanical?.Hinges.Count(hinge => hinge.Drive is not null) != 1`).
   - Ambiguous owner-only indexing in `RevoluteDriveTargetRead` (now carries exact `Joint`).

8. **What remains uncertain?**
   None. All requirements, contracts, and affected files are identified and bounded.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|---|---|---|---|
| Multiple driven hinges on one element | Element with Hinge 0 (speed_0) and Hinge 1 (speed_1) | Compiles 2 separate RunControl rows with unique lane slots; both hinges independently controllable | No error |
| Reordered presentation bindings | Presentation binding for Hinge 1 declared before Hinge 0 | Each binding tracks its declared `Actuator` coordinate regardless of declaration ordering | No error |
| Edit one run control among multiple actuators | Edit speed_1 on Hinge 1 | Only Hinge 1 drive changes speed; Hinge 0 and Slider 0 maintain their states | No error |
| Non-lane parameter with RunControl | Parameter declared as RunControl is also used for slider stiffness | Bundle validation failure: parameter-only-lane violation | Rejected with `BundleErrorCode.OutOfRange` |
| Multiple lane bindings for one parameter | Parameter bound to both SourceEnable and RevoluteDriveTarget | Bundle validation failure: parameter binds multiple lanes | Rejected with `BundleErrorCode.OutOfRange` |
| Unknown/mismatched capability target | Run control targeting non-existent hinge or passive hinge | Compilation/validation failure: unresolved actuator target | Rejected with typed error |
| Stale run control command | Run control command targeting non-existent control ID or stale revision | Engine rejects command | Rejected with `WorkshopEditRejectedException` |
| Authoring Reset | Player resets simulation after editing run control | Drive targets restore exact authored parameter values | No error |
| Save and load round-trip | Save construction with multiple driven actuators and reload | Exact drive targets, parameters, and bindings preserved | Exact match |

</intent-contract>

## Code Map

Candidate: isolated `/Users/aidan/.codex/worktrees/d1fb/tim`, branch `story-9a-46l-exact-capability-controls`, published baseline `9e442366092bb1fa9c9998988a2aa4119c3cd296`.
Fresh implementation owner Antigravity subagent (no inherited history), retained through all repairs.

Authority: primary `_bmad-output/planning-artifacts/epics.md`; Findings A7, A11, E6; requirements FR-01/02/06/07/09, PERF-24/25/26. Prerequisites 46a-k published at baseline.

## Verification & Terminal Review

- **Maintainability Gate E9:**
  Command: `dotnet run --file /Users/aidan/dev/personal/tim/tools/anvil/VerifyPolicy.cs -- --maintainability --project-root /Users/aidan/.codex/worktrees/d1fb/tim`
  Result: `Maintainability gate (CA1505 index >= 20, CA1502 complexity <= 25, balanced coupling; test projects excluded): violations=0 (metrics=0, coupling=0). PASS`
  Coupling dependencies: violations=0.
- **Compiler Proof Captures:**
  All 5 binlogs captured cleanly with 0 warnings and 0 errors: `playtest.binlog`, `solution.binlog`, `web.binlog`, `check-staged-tests.binlog`, `verify-policy.binlog`.
- **Static Gate & Float Guards:**
  Command: `dotnet test CuriousContraptions.tests/CuriousContraptions.tests.csproj --filter "FullyQualifiedName~CSharpStaticGateTests|FullyQualifiedName~GameFloatGuardTests"`
  Result: 109 passed, 0 failed, 0 skipped.
- **Full Solution Test Round:**
  Command: `dotnet test CuriousContraptions.slnx --no-build`
  Result: 5,913 passed, 0 failed, 0 skipped (`Skipped: 0`).
- **Independent Adversarial Review:**
  6 Auto review layers executed; all confirmed findings repaired by the implementation owner and re-verified:
  1. Forward-Only Auditor: Eliminated fallback branches in `WorkshopRunControl.cs` and `RunControlLanes.Find` where `joint == default` or `laneIndex < 0`; non-exact lookups throw `ArgumentException`.
  2. Edge Case Hunter: Extended `IsBoundToNonLane` to validate `MassFrameDeclaration` mass scalars (`Mass`, `CentreOfMass.X/Y/Z`, `PrincipalMoments.X/Y/Z`) across base bodies, hinges, and sliders; fixed harness unmapped joint bug.
  3. Intent Alignment Auditor: Added duplicate writer tests, stale command controls tests, differing states tests, and verified UI positional indexing.
  4. Blind Hunter: Added explicit joint verification to `WorkshopSimulation.ValidateAdmissionRead` and `ValidateAppliedControls`; verified owner checking in `read.RunControl(row)`.
  5. Verification Gap Reviewer: Implemented true save/load codec roundtrip verification via `WorkshopSaveCodec.Encode` and `WorkshopSaveCodec.Decode`; verified slider invariance; verified multi-lane binding and coordinate exception tests.
  6. Element-Free Engine Auditor: Engine has zero element names or heuristics (SCOPED PASS).
  7. Maintainability: Refactored `RunControlBindings.cs` to break cyclomatic complexity 65 into modular helper methods with CC <= 10.
  8. Test suite `ExactCapabilityControlsTests.cs` (12 tests) added and passing.
