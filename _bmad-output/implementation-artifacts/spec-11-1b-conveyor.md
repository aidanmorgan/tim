---
title: '11.1b — Conveyor package (CAT-019)'
type: 'feature'
created: '2026-11-04'
baseline_revision: 513a3c5b55a47552ab8868c4dd121762b74b46ce
status: 'done'
review_loop_iteration: 1
followup_review_recommended: false
context:
  - '{project-root}/docs/testing-guidance.md'
  - '{project-root}/docs/planning/elements/CAT-019-conveyor.md'
  - '{project-root}/_bmad-output/implementation-artifacts/epic-11-context.md'
  - '{project-root}/_bmad-output/implementation-artifacts/spec-11-1a-motor.md'
warnings: []
deferred: []
---

<intent-contract>

## Intent

**Problem:** CAT-019 Conveyor Belt (Story 11.1b, the second child of Epic 11) does not exist. Contraptions need a mechanical conveyor actuator fixture where a continuous driven belt transports contacting payloads via realistic friction, energized only by mechanical shaft coupling (from CAT-042 Motor or chained drive) with strict conservation of momentum and energy, stopping when power is cut.

**Approach:** Motor exposes `Drive` output port (`PortSocket.Drive`, `SimulationDomain.Mechanical`, `PortDirection.Output`). Deliver the `elements/Conveyor/` package as declarative data, colliders, and scene geometry implementing `IDrivenSurfaceElement` over the generic driven-surface contact law and mechanical transmission solver. Ensure `BundleValidator` enforces run-control parameter isolation and all tests pass with zero skips.

## Boundaries & Constraints

**Always:**
- Execute physics using canonical IEEE-754 `f32` single precision at 120 Hz with 480 Hz substeps (4 substeps per tick).
- One generic driven-surface friction law in the solver: tangential friction accelerating contacting bodies exerts equal and opposite reaction torque on the roller hinge.
- Only the authored carrier top face (+Y normal) drives contacts; side and underside contacts remain ordinary rigid contacts.
- Unloaded belt transfers zero work ($W_{\text{transferred}} = 0$). Total kinetic energy plus work respects the energy envelope ($E_k(t) \le E_k(0) + W_{\text{motor}} + \text{slack}$).
- When motor power is cut, bearing friction on the motor and belt brings the roller and belt to rest without sudden kicks or artificial energy injection.
- Mechanical connections: `Drive` (Output) couples to `DriveIn` (Input). At most one mechanical link per `DriveIn` socket and per `Drive` socket: fan-in and fan-out are both refused, because the engine admits one link per output coordinate and one per input coordinate (`engine/gpu/WorkshopConnections.cs:182`–`:183`).
- Validator rule (carried from `engine-revolute-drive-target` review): any parameter with `RunEdit == ParameterRunEdit.RunControl` must feed only its bound lane; using it in any other binding (mass, geometry, port offset) is rejected.
- Parameters: `length` (m, [1.0, 10.0], default 3.0), `width` (m, [0.6, 4.0], default 1.2), `surface_per_radian` (m/rad, [0.1, 5.0], default 0.6666667). All are build-only parameters. Retired properties `powered`, `speed`, and `traction` are rejected.
- Exact Reset restores authored unrotated roller pose, zero velocity, initial payload positions, and deterministic replay.
- Save/Load roundtrip preserves parameters, poses, and typed mechanical connections.
- Strict type safety, forward-only deletions, and zero skipped tests (`Skipped: 0`). Browser E2E remains deferred per `BrowserDeferral.cs`.

**Never:**
- No element-specific solver branches, update loops, or element IDs in physics kernels.
- No self-powered belt (no motor or battery within the conveyor itself; no copied speed work source).
- No backwards-compatibility shims, legacy fallbacks, or deprecated properties.
- No mutation testing before post-catalogue E2E phase.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Powered transport happy path | Battery enabled -> Motor `Drive` -> Conveyor `DriveIn` -> Basketball lands on conveyor top surface | Tangential friction accelerates ball along +X into receiver | No error expected |
| Unpowered belt control | Motor unpowered or mechanical cable disconnected | Roller does not turn; ball resting on belt stays stationary, zero motion, zero work | No error expected |
| Mid-run power cut | Running conveyor transporting ball; battery disabled mid-run | Motor and roller decelerate to stop under bearing friction; ball coasts to rest without velocity kicks | No error expected |
| Reversed drive | Motor run-controlled speed reversed (negative) or reversed transmission ratio | Belt surface runs along -X; ball accelerates in reverse direction | No error expected |
| Unloaded belt energy conservation | Motor drives empty conveyor | No payload on belt; no reaction torque transferred; motor work bounds roller KE ($E_k \le W_{\text{motor}}$) | No error expected |
| Multiple concurrent loads | Two basketballs resting on conveyor | Both transported simultaneously; shared roller reaction torque scales with contact forces; KE $\le E_0 + W_{\text{motor}}$ | No error expected |
| Off-belt miss / Side contact control | Ball contacts side rail or underside of conveyor carrier box | Ordinary rigid contact without tangential belt drive | No error expected |
| Rotated conveyor | Conveyor placed yawed 90° or 180° | Surface transport aligns with rotated local +X axis | No error expected |
| Reset & Replay | Simulation run mid-transport then Reset | Restores exact initial ball pose, roller angle, and zero velocity; replay matches identically | No error expected |
| Save/Load roundtrip | Save with non-default `length`, `width`, `surface_per_radian` and mechanical connection to motor | Load restores exact parameters, geometry, and connections | No error expected |
| Invalid parameters | Non-positive length/width, zero ratio, NaN/infinity | Rejected atomically with typed refusal | Typed refusal |
| Invalid mechanical connections | Non-mechanical domain wire to `DriveIn`, input-to-input connection, or duplicate drive to one `DriveIn` | Rejected atomically | Typed refusal |
| RunControl parameter leakage | Parameter marked `RunControl` bound to non-lane property | Rejected by `BundleValidator` | Typed refusal |

</intent-contract>

## Code Map

- `elements/Motor/`: Update `MotorElementDeclaration.cs` and `motor.tscn` to expose `Drive` output port (`PortSocket.Drive`, `SimulationDomain.Mechanical`, `PortDirection.Output`).
- `elements/Conveyor/`:
  - `ConveyorElementDeclaration.cs`: Implements `IMechanicalElement`, `IHingeElement`, `IDrivenSurfaceElement`, `ISceneGeometryElement`.
  - `conveyor.tscn`: Carrier box, dynamic driven cylinder roller, static idle cylinder roller, visuals.
  - `icon.svg` & `icon.svg.import`.
  - `Conveyor.csproj` & `.uid`.
- `CuriousContraptions.slnx`: Register `elements/Conveyor/Conveyor.csproj`.
- `CuriousContraptions.tests/`:
  - `CSharpStaticGateTests.cs`: Add Conveyor project to gate scan.
  - `BundleElementIdTests.cs`: Update expected package count (34 -> 35).
  - `CurrentCatalogueTests.cs`: Update catalogue tests (34 -> 35, add conveyor assertion).
  - `BasketballResourceTests.cs`: Add Conveyor definition.
  - `BundlePortsTests.cs`: Add Mechanical helper and Conveyor entry.
  - `PartRegistryTests.cs`: Add Conveyor entry.
  - `CatalogueIntegrationTests.cs`: Add Conveyor to drive interactions.
  - `VisibleSolidColliderTests.cs`: Add Conveyor classification.
  - `WorkbenchCapacityTests.cs`: Add Conveyor allowance.
  - `WorkshopIntegrationHarnessTests.cs`: Add Conveyor to expected palette.
  - `ConveyorTests.cs`: Dedicated integration and unit tests covering the matrix.

## Tasks & Acceptance

**Execution:**
- [x] Engine generic driven surface and transmission solvers delivered in Stories 9A.46a-9A.46m.
- [x] `elements/Motor/MotorElementDeclaration.cs` & `motor.tscn`: Add `Drive` output port.
- [x] `elements/Conveyor/`: Implement Conveyor package (`ConveyorElementDeclaration.cs`, `conveyor.tscn`, `icon.svg`, `Conveyor.csproj`).
- [x] `CuriousContraptions.slnx`, `CSharpStaticGateTests.cs`, `BundleElementIdTests.cs`, `CurrentCatalogueTests.cs`: Update solution registration and test assertions.
- [x] `CuriousContraptions.tests/ConveyorTests.cs`: Implement full matrix test suite (positive transport, stopped/unpowered, power-cut deceleration, reversed, energy conservation, multiple loads, Reset/save/load, parameter & connection refusals).

**Acceptance Criteria:**
- [x] Given a Conveyor coupled to a powered Motor, when a Basketball lands on the belt, then tangential friction accelerates the ball along local +X into a receiver.
- [x] Given an unpowered Motor or disconnected mechanical wire, when a Basketball rests on the belt, then it remains stationary with zero motion and zero work.
- [x] Given a running Conveyor transporting a load, when motor power is cut, then the roller and belt decelerate to rest under friction without velocity kicks.
- [x] Given a reversed Motor speed or reversed ratio, when running, then the belt transports payloads in the reverse direction (-X).
- [x] Given an empty Conveyor driven by a Motor, when running, then zero work is transferred and motor work bounds roller kinetic energy.
- [x] Given two simultaneous basketball payloads on the belt, when running, then both payloads advance concurrently without artificial energy gain.
- [x] Given a parameter marked `RunControl`, when it is also referenced by a non-lane property, then `BundleValidator` rejects the bundle.
- [x] Given any invalid parameters or malformed connections, then they are rejected atomically with typed refusals.
- [x] Given any simulated state, when Reset is executed, then exact initial poses, speeds, and connection graphs are restored.

## Spec Change Log

- 2026-11-08: Updated baseline revision to 513a3c5b55a47552ab8868c4dd121762b74b46ce post-remediation program completion. Updated DrivenSurfaces contract to modern multi-surface descriptor.
- 2026-11-17: Repaired confirmed review findings (`FrictionTorque: 0.02f`, `PulleyIn` under `Visual/Roller`, test coverage hardening) and recorded terminal review/verification receipts.

## Review Triage Log

- **Initial 6-layer Auto review (`513a3c5b` + candidate diff):**
  - `Element-Free Engine Auditor` (`ce4bfa2c`): Scoped Pass (0 violations, 0 gaps; D1–D7 and E1–E9 verified).
  - `Forward-Only Auditor` (`a54abc38`): Pass (0 findings).
  - `Intent Alignment` (`4d268730`): Descriptive report confirmed Reading 1 (declarative package over generic engine solvers).
  - `Edge Case Hunter` (`a9e545af`): Flagged `FrictionTorque = 0f` and signature/binding checks on current baseline.
  - `Verification Gap Reviewer` (`c3669a67`): Flagged missing chained-conveyor relay test, active-braking power-cut test, and `FrictionTorque = 0f`.
  - `Blind Hunter` (`fd631f0a`): Flagged duplicate diff blocks, `OffBeltMiss` at `z = 1.5` missing conveyor, `FrictionTorque = 0f` + active-braking power-cut test, static `PulleyIn`/`PulleyOut`, unbound `length`/`width`, synthetic double in `ReversedDrive`, missing negative/zero parameter bounds, missing zero-work assertion in `UnloadedBelt_ConservesEnergy`, missing input-to-input connection refusal, and trajectory tolerance / missing roller angle assertion in `ResetAndReplay`.
- **Repairs applied & verified (`2026-11-17`):**
  - `elements/Conveyor/ConveyorElementDeclaration.cs`: Changed `Hinges[0].FrictionTorque` from `0f` to `0.02f` so unpowered/cut-power rollers coast smoothly to rest under bearing friction.
  - `elements/Conveyor/conveyor.tscn` & `CuriousContraptions.tests/VisibleSolidColliderTests.cs`: Parented `PulleyIn` under `Visual/Roller` at `(0, 0, 0.78)` so the input drive pulley and spoke rotate visibly with Body 1; updated `VisibleSolidColliderTests` path to `Visual/Roller/PulleyIn/*`. (`PulleyOut` sits on the static idle end without a generic revolute-angle `PresentationDriver`; `CommittedWheelAngle` is rope-guide-only.)
  - `CuriousContraptions.tests/ConveyorTests.cs`:
    - `MidRunPowerCut_DeceleratesToStopUnderFriction`: Switched from motor `RevoluteDriveTarget` braking to battery `SourceEnable = ElectricalEnable.Disabled`, verifying passive spin-down (`<= 0.05 rad/s` and `<= 0.05 m/s` after 240 ticks).
    - `OffBeltMiss_SideContactRemainsOrdinaryRigid`: Changed drop offset from `z = 1.5f` to `z = 0.85f` and asserted outward `+Z` side deflection (`pos.Z > 0.85f`), downward gravity fall (`pos.Y < ConveyorY`), and zero `+X` belt drive (`vel.X` within `[-0.05f, 0.05f]`).
    - `ChainedConveyors_DriveRelaysRotationToDownstreamConveyor`: Added full multi-conveyor relay integration test (`motor -> conveyor1 -> conveyor2`).
    - `UnloadedBelt_ConservesEnergy`: Added unpowered battery control (verifying zero work and rest envelope `<= 1e-4f`), asserted conveyor hinge `Work == 0f`, and tightened energy bound slack to `1e-4f`.
    - `ResetAndReplay_RestoresExactAuthoredState`: Asserted both motor and roller revolute angles restore to `0` (`< 0.001f`) and replaced the `5 mm` trajectory window with exact deterministic equality for `Origin` and `Velocity`.
    - `InvalidParameters_RefusedAtomically` & `InvalidMechanicalConnections_RefusedAtomically`: Added `-1f`, `0f`, `float.NegativeInfinity` parameter cases and input-to-input direction mismatch case.
    - `ConveyorArtTests`: Asserted `Visual/Roller/PulleyIn/Spoke` rotates (`> 0.5f rad`) and restores to its authored scene pose on Reset.
  - **Refuted with code evidence (`length`/`width` parametric scaling):** `PortDescriptor.PortPositionBinding` supports at most one parameter per port whereas Conveyor ports depend on both `length` (`±(L/2 - 0.15)`) and `width` (`W/2 + 0.18`), and `RevoluteHingeDescriptor.BaseFrame` is constant; partial collider scaling would detach carrier colliders from hinge anchors and drive ports, and every conveyor level in `content/puzzles.json` uses default `3.0 m × 1.2 m` dimensions.
- **Independent re-review (`2026-11-17` on `/tmp/bmad-11-1b/re-review-product.diff`):**
  - `Verification Gap Reviewer` (`c3669a67`): All 3 prior findings **CLOSED**; 0 new verification gaps.
  - `Blind Hunter` (`fd631f0a`): Findings 1–4 and 6 **RESOLVED**; Finding 5 (`length`/`width`) **REFUTED WITH CODE EVIDENCE**; 0 remaining/new findings (`Conveyor` tests 39/39 passed, `VisibleSolidColliderTests` 70/70 passed).
- **Maintainability Gate (`2026-11-17`):**
  - `dotnet run --file /Users/aidan/dev/personal/tim/tools/anvil/VerifyPolicy.cs -- --maintainability --project-root /Users/aidan/.codex/worktrees/d1fb/tim` -> `PASS` (`violations=0 (metrics=0, coupling=0)`, `contract=3038, model=4, intrusive=0`), both canaries `PASS`.
- **Final diff scope & simplification analysis:**
  - Candidate scope on baseline `513a3c5b55a47552ab8868c4dd121762b74b46ce`: 25 files (`CuriousContraptions.Abstractions/PortId.cs`, `elements/Motor/MotorElementDeclaration.cs`, `elements/Motor/motor.tscn`, `elements/Conveyor/*`, `CuriousContraptions.slnx`, `tools/Coverage.Tests/CurrentCatalogueTests.cs`, `tools/E2eRunner/ImpactRules.cs`, catalogue test registrations, `VisibleSolidColliderTests.cs`, `ConveyorTests.cs`, and workflow tracking files). Every hunk serves CAT-019 Conveyor delivery, Motor's `Drive` output port, catalogue registration, or confirmed review repairs. Zero engine solver edits; no speculative abstractions or legacy shims; simplification check found no justified further reduction while preserving data-driven package boundaries and explicit port declarations. **Final diff scope: Pass.**

## Auto Run Result

- **Status:** `done` (native catalogue implementation complete; Chrome/Playwright E2E deferred until post-catalogue closure; remote CI/deployment unverified per 29 October no-CI-wait rule).
- **Build (`dotnet build CuriousContraptions.slnx -c Debug --nologo`):** `0 Warning(s), 0 Error(s)`.
- **Affected Native & Fast Tool Suites:**
  - `CuriousContraptions.tests.dll` (affected Conveyor, VisualSolid, Bundle/Registry/Harness filter): **263 passed, 0 failed, 0 skipped**.
  - `WorkerAotGuard.Tests.dll`: **17 passed, 0 failed, 0 skipped**.
  - `Coverage.Tests.dll`: **77 passed, 0 failed, 0 skipped**.
  - `BundleElements.Tests.dll`: **240 passed, 0 failed, 0 skipped**.
  - Normal pre-commit hook (`.githooks/pre-commit`) verified on staged snapshot with 5 compiler logs + `CSharpStaticGateTests` + `WorkshopUiFailureTests` + `anvil hook pre-commit`.

## Verification

**Commands:**
- `test "$(node --version)" = v26.12.1`
- `dotnet build CuriousContraptions.slnx -c Debug --nologo` -- expected: 0 warnings, 0 errors.
- `dotnet test CuriousContraptions.tests/CuriousContraptions.tests.csproj -c Debug --nologo --filter "FullyQualifiedName~ConveyorTests"` -- expected: all pass, 0 failed, Skipped: 0.
- Run full verification block from `docs/delivery-workflow.md#local-test-authority` -- expected: all 5 test projects pass with Skipped: 0.
- Run `.githooks/pre-commit` on staged candidate -- expected: exit 0, Anvil clean.
