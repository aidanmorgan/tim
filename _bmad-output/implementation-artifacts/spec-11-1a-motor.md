---
title: '11.1a — Motor package (CAT-042)'
type: 'feature'
created: '2026-11-03'
status: 'done'
baseline_revision: 61c32d993f46350ff157de2167b21639ee66749b
review_loop_iteration: 0
followup_review_recommended: true
context:
  - '{project-root}/docs/testing-guidance.md'
  - '{project-root}/docs/planning/elements/CAT-042-motor.md'
  - '{project-root}/_bmad-output/implementation-artifacts/epic-11-context.md'
  - '{project-root}/_bmad-output/implementation-artifacts/spec-engine-revolute-drive.md'
warnings: ['oversized']
deferred: []
---

<intent-contract>

## Intent

**Problem:** CAT-042 Motor (Story 11.1a, the first child of the split Epic 11 aggregate) does not exist. Its acceptance is that a supplied shaft delivers bounded torque and work, an unpowered shaft delivers none, and stalled demand never creates work.

**Approach:** Add the `elements/Motor/` package as declaration data, art and bindings only, on the published generic engine (`engine-revolute-drive`).
- **Body and hinge:** a dynamic rotor on a `Continuous` hinge.
- **Drive:** a supplied velocity drive, with `speed` and `torque` as placement parameters.
- **Bearing friction:** spins the rotor down when unpowered (owner, 3 November 2026).
- **Power:** a `PowerIn` consumer.
- **Supply lamp:** reads the drive's committed supplied state, through the existing actuator read generalised to every drive slot.

## Boundaries & Constraints

**Always:**
- **No new mechanism.** The package is declaration data only; there is no Motor-specific engine code.
- **Lamp read.** It generalises the existing `ActuatorDriveRead` / `CommittedActuatorDrive` path to every drive slot, keyed by the typed `SupplyConsumer`. It is one read with one `SupplyClosed` expression. There is no second parallel read.
- **Decisions by game intent** (record each in CAT-042 §6):
  - **Spin direction:** +`speed` spins counterclockwise about the rotor's +Z, the engine convention. The Drive socket sign is 11.1b's.
  - **Bearing friction:** τ_f = 0.05 N·m, so an unpowered rotor spins down from 6 rad/s in about 2.7 s. `torque` ranges over [0.1, 100] N·m (default 20). Its minimum stays above τ_f, so every Motor starts. This replaces the legacy open interval (0, 100] and harvest row 14's 0.01.
  - **Rotor contact material:** restitution 0, threshold 0.1, friction 0.3. This is the catalogue's rigid-part material; the legacy restitution 1 is replaced.
  - **Index bar:** flush on the rotor face and decorative, with no collider.
  - **Battery voltage:** stays inert data.
- **Process:** f32; forward-only; red-first behavioural tests on the real compiled package; game-grade derived tolerances; full local round `Skipped: 0`.

**Never:**
- the `Drive` port, `Mechanical` connection domain or `Drive` socket, and the "non-mechanical wire into Drive refused" negative (11.1b);
- run-time speed control (`engine-revolute-drive-target`);
- battery drain (deferred);
- `Turned` / `Powered` events and goal kinds, and level promotion (level work);
- new engine laws;
- browser E2E.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|---|---|---|---|
| Supplied spin-up | Battery enabled → Motor defaults (speed 6, torque 20) | Rotor reaches 6 rad/s within one tick (ramp I·ω/(τ−τ_f) ≈ 6.8 ms); lamp gold within 0.1 s; committed work grows | No error expected |
| Unpowered controls | No battery; disabled battery; activation wire only | No rotation, work 0, lamp slate | No error expected |
| Supply cut mid-run | Running at 6 rad/s, battery disabled | Lamp fades. Spins down at τ_f/I ≈ 2.2 rad/s² to rest within about 2.7 s plus a stated slack; no work added | No error expected |
| Low torque | torque 0.1 | First-tick speed gain (τ−τ_f)·(1/120)/I within stated slack | No error expected |
| Speed bounds | speed 0, supplied | Shaft held still, work 0 | No error expected |
| | speed 20, supplied | Reaches 20 | No error expected |
| Stall | Rotor rim tied taut by a tensile span to a static anchor (appended to the compiled scene) | Work stops growing once taut | No error expected |
| Overload | Rim crank load above torque | Back-driven; work never negative and never grows while back-driven | No error expected |
| Energy | Every driven case | Rotor KE ≤ committed work + derived slack | No error expected |
| Rotated mount | Motor rotated 90° about X | Spins about its rotated axis; same speed | No error expected |
| Run/Reset | After driven ticks | Rotor pose and speed, work and lamp return to authored state | No error expected |
| Save/Load | speed 12, torque 40, pose, Battery connection | Round-trips | No error expected |
| Compiler hookup (owed by the engine slice) | Motor compiled at speed 20, torque 100 | Compiled `Continuous` travel; drive (20, 100) evaluated from parameters; owner's Consumer node names `SupplyConsumer.Revolute(joint)`; Motor bundle without `PowerIn` refused by the validator | Typed refusal |
| Invalid parameters | speed outside [0, 20]; torque outside [0.1, 100]; NaN or ±∞ | Refused atomically | Typed refusal |

</intent-contract>

## Code Map

The full planning map is in the coordinator's investigation summary (11.1a). It is distilled here.

- **Package `elements/Motor/`:** `MotorElementDeclaration : IHingeElement, IElectricalElement, ISceneGeometryElement`, combining `elements/ImpactLever/ImpactLeverElementDeclaration.cs:12-118` and `elements/PoweredGate/PoweredGateElementDeclaration.cs:14-91`.
  - **Footprint:** `(2, 1, 0, 1)`; `KinematicMode.Static`; `PaletteOrder` 36.
  - **Electrical node:** `new("motor", ElectricalRole.Consumer, …)`.
  - **Hinge:** `RevoluteTravel.Continuous`, limits [−π, π], stiffness 0, `FrictionTorque` 0.05, drive `RevoluteDriveDescriptor(FromParameter("speed"), FromParameter("torque"))`, `BodiesCollide:false`, identity frames. `BaseFrame` is at the rotor centre (0, 0, 0.54) and `InitialAngle` is `Fixed(0)`. The compiler ignores `MovingFrame` rotation (`engine/gpu/WorkshopPhysicsCompiler.cs:124-135`).
  - **Rotor mass:** 0.5 kg, `MassFrameDeclaration.Constant` with the principal frame Rx(90°) so the axial inertia 0.0225 lies on Z and the transverse inertia is 0.01195 (`CuriousContraptions.Simulation/WorkshopPhysicsEngine.cs:371-372`). This is the first non-identity principal frame in a package. The parity oracle `PackageRuntimeParityTests.cs:254-289` compares in collider axes; adapt it, or state the frame.
  - **`.tscn`:** `SceneBodies [".", "Visual/Rotor"]`. Collider shapes:
    - the housing boxes and a cylinder turned onto Z (quaternion (0.7071068, 0, 0, 0.7071068), as for the lever `col_fulcrum`, `elements/ImpactLever/impact_lever.tscn:47-49,64-67`);
    - the rotor cylinder (r 0.3, height 0.13) on Z.
    `Loc_Port_PowerIn` sits at (−0.55, 0, 0) as a direct child of the root. The lamp material comes from `powered_gate.tscn:23-24,63-66,121-123`.
  - **Copy from PoweredGate:** `.csproj` (`elements/PoweredGate/PoweredGate.csproj`) and the `icon.svg` form, with unique import uids.
  - **Contract:** dimensions, colours and catalogue entry follow `docs/planning/elements/CAT-042-motor.md` §2:
    - id `motor`, title "Electric motor", category Power, colour `#66b8c9`, pick radius 0.9;
    - lamp a sphere, r 0.075, at (0.32, 0.28, 0.45);
    - the index bar is decorative;
    - `Presentation.Bodies [new("Visual/Rotor", new PackageBodyIndex(1), Vector3.Zero)]`.
- **Units:** add `RadianPerSecond` and `NewtonMetre` to `CuriousContraptions.Abstractions/ParameterDescriptor.cs:15-28`. Update the UI label switch `ui/WorkshopConfiguration.cs:163-177` and the parity parameter table (`PackageRuntimeParityTests.cs:~511`). `DriveValue` (`CuriousContraptions.Bundle/BundleValidator.cs:668-680`) admits the signed target and requires a positive torque minimum.
- **Lamp (generalised actuator read):**
  - `ActuatorDriveRead` (`engine/gpu/PhysicsPresentationRead.cs:44-63,125-130,158-175`) gets one row per drive slot, prismatics then revolutes, keyed by `SupplyConsumer`. `SupplyClosed` is `!admission && inputs.SupplyClosed[slot]` for every slot. Revolute rows set `MovingContact` false; Coordinate is travel, Speed is relative ω, Lower and Upper are the limits.
  - `PresentationDriverRules` (`CuriousContraptions.Abstractions/ElementPresentation.cs:83-86`): allow `CommittedActuatorDrive` with SmoothStep and a duration above 0, as well as the existing Linear with duration 0. This is shared by the validator (`BundleValidator.cs:165`) and compiled admission (`engine/gpu/WorkshopCompiledScene.cs:91`).
  - Wire: `ActuatorDriveRecord` gains a kind (`schemas/workshop_wire.fbs:160-170`). Regenerate `engine/gpu/WorkshopWireGenerated.cs` and `tools/e2e/workshop-wire.ts` with pinned flatc **25.2.10** at `/private/tmp/claude-501/-Users-aidan-dev-personal-tim/5b49c1ca-9721-4a5d-8c03-bdf3a5db8b62/scratchpad/flatc-25.2.10/flatc`. Land the regenerated files with Edit or Write, never shell redirection.
  - Encode and decode: `engine/gpu/WorkshopWireFlatBuffers.cs:259-270,530-535`.
  - Observation: JSON in `engine/gpu/WorkshopObservationProjection.cs:197-212`; TypeScript in `CuriousContraptions.web/wwwroot/workshop-observation.ts:101`; `tools/e2e/run-reset-snapshot.ts:33`.
  - Update every test that builds or asserts actuators: the investigation lists `DeclaredPresentationTests`, `DeclaredPrismaticDriveTests`, `WorkshopIntegrationHarnessTests`, `CatalogueIntegrationTests`, the electrical-gate tests, and others. In particular check every `Actuators[0]` and `Single(` use, now that undriven hinges get rows.
- **Registration:**
  - `CuriousContraptions.slnx:48`; tests `.csproj`;
  - `BundleElementIdTests.cs:90` (33 → 34);
  - `PartRegistryTests.cs:23`;
  - `WorkbenchCapacityTests.cs:130,288`;
  - `PackageRuntimeParityTests.cs:213` (the hinge list), `:511-516` and `:533`;
  - `CSharpStaticGateTests.cs:694`;
  - `CatalogueIntegrationTests.cs:276-292`;
  - `BundlePortsTests.cs:~132`;
  - `BasketballResourceTests.cs:34`;
  - `WorkshopIntegrationHarnessTests.cs:543`;
  - `tools/Coverage.Tests/CurrentCatalogueTests.cs:198-199`;
  - `tools/E2eRunner/ImpactRules.cs:27`;
  - `VisibleSolidColliderTests.cs` (a Motor `ArtSpec` with moving root `Visual/Rotor`; the index bar is decorative).
- **Tests:** new `CuriousContraptions.tests/MotorTests.cs`.
  - **Battery wiring:** `new WorkshopConnection(battery.Id, WorkshopSocket.Supply, motor.Id, WorkshopSocket.PowerIn, WorkshopConnectionDomain.Electrical)`. `PlacedParts.Battery` is at `CuriousContraptions.tests/PlacedParts.cs:16-24`.
  - **Mid-run cut:** `sim.Require(WorkshopCommandKind.ConfigureElectrical, electrical: new(battery.Id, ElectricalEnable.Disabled))`. Precedent: `DeclaredPrismaticDriveTests.cs:412-475`; harness `WorkshopIntegrationHarness.cs:126,176-181,229`.
  - **Committed state:** `PhysicsGpuAbi.ReadRevoluteState(harness.Device.CommittedWorld.ToArray(), i)`.
  - **Load fixture:** append a tensile span from a rotor rim point to a static anchor (stall) or a hanging box (overload) to the compiled scene, following `CompiledSceneBoundaryTests.cs:337-353`. Engine-direct precedents are at `RevoluteDriveTests.cs:293-353`.
  - **Lamp:** `harness.Presentation` is live (`DeclaredPresentationTests.cs:24-60`). Advance the clock at least 0.1 s.
- **Compiler hookup** (deferred from the engine slice):
  - Parity at non-default parameters (speed 20, torque 100), plus the consumer-node assertion (pattern `DeclaredPrismaticDriveTests.cs:765-767`).
  - The missing-PowerIn refusal is at the validator: `WorkshopPhysicsCompiler.cs:45` is unreachable through `DeclaredPhysics.Install`, so test the refusal on the Motor entry with `PowerIn` removed (generic precedent `tools/BundleElements.Tests/BundleValidatorTests.cs:1119`).
- **Docs:**
  - CAT-042 §2 and §6: decisions recorded; open questions 1, 3 and 10 answered by game intent; Q5 → the target slice; Q7 → level work; the Drive port → 11.1b.
  - `deferred-work.md`: none new, unless found.

## Tasks & Acceptance

**Execution:**
1. **Predict and red-first.** Add the matrix tests on the real package and record their failures (the Motor does not exist yet). Write each expected outcome in the implementer record first. Investigate mismatches; never tune.
2. **Package.** Add the package, units and registration, green.
3. **Lamp.** Add the generalised actuator read and the lamp binding, green, with the Powered Gate unchanged.
4. **Tests and docs.**
5. **One full local round, then the unmodified pre-commit hook.**

**Process rules for the implementer:**
- Write only inside the worktree and `$TMPDIR`. Logs stay there too; never write to the coordinator scratchpad.
- Use Edit or Write for every repository file. No heredoc, `cp` or redirect into repository files.
- Run Anvil before each write.
- Put a timeout on every build and test command.
- Stop and report rather than tune, or add a mechanism.
- Stage the result; do not commit.

**Acceptance Criteria:**
- Given the shipped Motor wired to a Battery, when Run proceeds, then the matrix outcomes hold through the real compiled package.
- Given every existing Powered Gate and actuator test, then its behaviour is unchanged.
- Given CAT-042, then each decision and the 11.1b, target-slice and level moves are recorded.

## Spec Change Log

- **Stall and loaded speed-0 rows blocked by an engine defect (3 November 2026, decision needed).** The spec's rope fixtures reveal that a tensile span cannot hold a supplied revolute drive: a rim-tied rotor creeps at about 0.0082 rad/s per N·m of drive torque, and a supplied speed-0 Motor slowly lifts a rope-hung 1 kg Weight (+0.0075 rad/s). Both tests stay as written and fail. Controls on the same fixture path pass (a rope from the static housing holds a hanging Weight; an unloaded tied rotor at speed 0 is still; the overload case passes), and a Limited hinge appended between the rotor and a static Battery locks the supplied rotor exactly with zero work. Recorded in [deferred work](deferred-work.md#tensile-revolute-drive-creep) and CAT-042 §6 item 12.
- **Owner decision, 4 November 2026: land 11.1a; the stall and speed-0 rows are proved against a locked hinge and an unloaded shaft.** Stall: `ALockedMotorShaftDoesNoWork` (a Limited hinge appended between the rotor and the static Battery locks the supplied rotor; travel within 0.01 rad, work within the slack). Speed 0: `ASuppliedMotorAtSpeedZeroStaysAtRestWithNoWork` (supplied at every tick, spin below the rest envelope, work exactly 0). The two rope tests (`AStalledMotorDoesNoWorkOnceItsShaftIsHeld`, `ASuppliedMotorAtSpeedZeroHoldsItsLoadedShaftStill`) were removed from `MotorTests`; their exact scenario, assertions, measured creep and controls move to the [deferred-work entry](deferred-work.md#tensile-revolute-drive-creep) as the acceptance of the next engine slice, `engine-tensile-drive-coupling` (TODO order 2, ahead of `engine-revolute-drive-target`).
- **Unit labels:** `PalettePlacementTests.TheMotorEditorLabelsSpeedInRadiansPerSecondAndTorqueInNewtonMetres` places a Motor on the bench, opens the generic editor and reads "speed (0–20 rad/s)" and "torque (0.1–100 N·m)".
- **Choices beyond the spec, by game intent:** the housing takes the rotor's rigid-part material (restitution 0); the gold PowerIn stud (r 0.1 m) and the lamp are drawn, so they carry housing colliders (visible = solid); the index bar is printed flush (4 mm, not the legacy 4 cm) so it can be decorative; the electrical node's inert data is 12 V, 2000 W (100 N·m × 20 rad/s), 0.072 Ω.
- **`ActuatorDriveRead.Joint` is replaced** by `Drive` (`SupplyConsumer`) with a `Slot` order (kind, then joint identity). No old member is kept. The wire record gains `kind` (uint32, the `SupplyConsumerKind`), decoded through the one existing consumer mapping (`SupplyConsumerOf`).

## Implementer record

**Predictions, written before the first run of `MotorTests`.** Every value derives from the declared rotor (0.5 kg, r 0.3 m: axial inertia I = m·r²/2 = 0.0225 kg·m²), the bearing friction τ_f = 0.05 N·m, the 120 Hz tick and the 480 Hz substep h.

| Matrix row | Predicted outcome |
|---|---|
| Supplied spin-up (6, 20) | Ramp I·ω/(τ−τ_f) = 6.8 ms, so ω_t within 2 % from tick 2 (`RampTicks` = ⌈0.81⌉ + 1) and held for 1 s. Lamp strictly between slate and gold mid-clip, gold 0.1 s after supply. From 0.5 s to 1 s the work grows by the friction loss τ_f·ω_t·0.5 s = 0.15 J (within 10 %). Travel after 1 s ≈ ω_t·(t − T/2) = 5.98 rad (within 2 %). |
| Unpowered controls | No battery, disabled battery: drive lane inert, work exactly 0, supplied false, rotor below one substep's spin, lamp slate. Activation wire: refused at the connection seam (`InvalidConnection`); the Motor has no activation socket. |
| Supply cut at 0.5 s | Work frozen at its cut value; decelerates at τ_f/I = 2.22 rad/s² (3.78 rad/s 1 s after the cut, within 5 %); at rest (≤ 0.02 rad/s) after 6/2.22 = 2.7 s ± 10 %, then stays at rest; lamp back to slate. |
| Low torque 0.1 | Speed after n ticks (τ−τ_f)·n/(120·I): 0.0185 rad/s at tick 1, 2.22 rad/s at 1 s, each within one substep of friction (τ_f·h/I = 0.0046 rad/s) plus 2 %. |
| Speed 20 | Ramp 22.6 ms: 20 rad/s within 2 % from tick 4, held. KE there 4.5 J. |
| Speed 0, supplied | A 1 kg Weight hung from the rim (2.94 N·m < τ_d): held within 0.01 rad, work ≤ 0.01 J. Unsupplied control: turns past −0.5 rad. |
| Stall | Rim tied to the housing by a 1.001 m span: after 0.25 s the rotor moves less than 0.01 rad in 1 s and the work grows by at most τ_d·|Δtravel| + slack (a free drive would deliver 120 J). |
| Overload | Torque 1 N·m against the 2.94 N·m rim load: back-driven past −0.5 rad; work never decreases and stays ≤ 0.01 J through the descent; system energy gain ≤ work + slack. |
| Energy | KE ≤ work + 3 % of ½·I·ω_t² at every tick; work ≤ τ_d·|ω_t|·t + slack. Ledger over-count ½·τ_d·h·ω_t = 0.125 J at the defaults (only over-counts). |
| Rotated mount (90° about X) | Spins about R·Z = −Y at 6 rad/s within 2 %, off-axis spin below 2 %. |
| Run/Reset, Save/Load | Authored pose and zero speed, travel, work, supplied; lamp slate. Save round-trips speed 12, torque 40, pose and the typed connection; the loaded motor reaches 12 rad/s. |
| Compiler hookup | Continuous travel, drive (20, 100), consumer node naming `SupplyConsumer.Revolute(joint)`; no PowerIn refused by the validator. |
| Invalid parameters | Refused at the collection, placement, construction and save seams. |

**Red run.** On the base (no package; `MotorTests.cs` only), the 30 matrix cases compiled and all 30 failed: 19 with `No baked physics declaration exists for 'motor'`, 10 parameter refusals with that `InvalidOperationException` instead of the expected `ArgumentException`, and the validator case with no `motor` bundle entry.

**Observed after the package.** 27 of 30 passed on the first green run; no constant or bound was tuned.
- `ResetRestores…`: a test defect. Reset installs a new world epoch, which the body read carries; the test now compares every other field of the authored rotor read.
- `AStalledMotor…` and `ASuppliedMotorAtSpeedZero…(supplied: True)`: an engine defect, see the Spec Change Log. Measured series (3 s, travel and work): τ_d 0.5 N·m → 0.0038 rad/s; 2 N·m → 0.0164 rad/s; 20 N·m → 0.168 rad/s with work growing 2.8 J/s; speed 0 with the 1 kg rim load → +0.0075 rad/s, passing 0.01 rad after about 0.6 s. Diagnostics were temporary tests, removed.

The generalised-read tests (`EachDriveSlot…`, `TheDriveReadRefuses…`, `AResponseWithAnUndefinedDriveKind…`) and `ALockedMotorShaftDoesNoWork` were written after the read changed and passed on their first run.

**Verification (implementer).**
- Full local round per `docs/delivery-workflow.md#local-test-authority` with pinned Node 26.12.1 (log `$TMPDIR/full-round-1.log`): five compiler logs captured, 0 warnings, 0 errors. Every project reported `Skipped: 0`. Exit 1, because of the two blocked tests only:

  | Project | Passed | Failed |
  |---|---|---|
  | CuriousContraptions.tests | 4,989 | 2 (`AStalledMotor…`, `ASuppliedMotorAtSpeedZero…(supplied: True)`) |
  | BundleElements.Tests | 231 | 0 |
  | E2eRunner.Tests | 192 | 0 |
  | Coverage.Tests | 77 | 0 |
  | WorkerAotGuard.Tests | 17 | 0 |

- Unmodified `.githooks/pre-commit` on the staged candidate (38 paths, log `$TMPDIR/hook-1.log`): exit 0; Anvil scanned 27 snapshots with total 0, errors 0, `hasBlockingWarnings: false`; scoped tests 127/0/0, Skipped 0. Its scoped selection does not include `MotorTests`, so the hook passing does not cover the two blocked rows.

- After hook run 1: `BundleValidatorTests.ACommittedDriveBindingIsInstantLinearOrATimedSmoothStep` was added (the widened driver rule's admitted and refused cases; scoped `BundleValidatorTests` 65/0/0, Skipped 0), CAT-042 §6 item 9 and the deferred-work destination wording were corrected, and this record changed. Hook run 2 on the restaged candidate (39 paths, log `$TMPDIR/hook-2.log`): exit 0; Anvil scanned 28 snapshots with total 0, errors 0, `hasBlockingWarnings: false`; scoped tests 127/0/0, Skipped 0. After it, only this record line changed and was restaged. The coordinator's final full round covers the added test.

- After hook run 2 (owner decision, 4 November 2026): the two rope tests were removed and their recipe moved to deferred work; `ASuppliedMotorAtSpeedZeroStaysAtRestWithNoWork` and the unit-label test were added; TODO and CAT-042 §6 item 12 were updated. Scoped `MotorTests` and `PalettePlacementTests`: 110/0/0, Skipped 0. The full round above predates this change (the coordinator runs the final round). Hook run 3 on the restaged candidate (41 paths, log `$TMPDIR/hook-3.log`): exit 0; Anvil scanned 29 snapshots with total 0, errors 0, `hasBlockingWarnings: false`; scoped tests 127/0/0, Skipped 0. After it, only this record line changed and was restaged.

**Review loop 1 fixes** (the `[patch]` rows of the 2026-11-04 triage).
- Forward-Only 1 / Intent 8: CAT-042 harvest facts 10 and 13 are marked superseded for their coasting claims (owner Q9) and fact 14 superseded (decision 1, the 0.1 N·m floor); the acceptance outline names spin-down and the 0.1 N·m boundary; §6 item 13 records the flush index bar; §2 records the as-shipped description and domains.
- Intent 10 / Blind 8: the description is "Electricity turns its shaft." and `SupportedDomains` is Electrical only until 11.1b.
- Edge 1 / Blind 6: a committed-drive binding reads only the rows its owner's compiled consumer node names (`WorkshopDeclaredPresentation` track `Drives`). New `InAMixedSceneEachLampReadsItsOwnDrivenSlotAndAnUndrivenHingeHasItsOwnRow` (Impact Lever + Motor + Powered Gate). No shipped package has both a driven and an undriven slot on one owner, so this scene cannot fail under the old first-row lookup; it pins row population, order and each lamp's own slot.
- Edge 2 / Edge 3: the validator refuses `CommittedActuatorState` on an owner with no driven slider, and requires `RadianPerSecond` and `NewtonMetre` for parameter-bound hinge drive values (prismatic drive values unchanged). Tests: `ACommittedActuatorStateBindingNeedsADrivenSlider`; unit refusals in the hinge-travel validator test.
- Verification 1 / Blind 11: the projection test injects a prismatic and a revolute row with exact identities and asserts each row's kind, joint, owner and supply. No TS/C# enum parity test pattern exists in the repository, so the TypeScript `SupplyConsumerKind` values are not cross-checked by a test.
- Blind 1 / Intent 3: overload asserts `Supplied` at every tick and adds an unwired control that passes −0.5 rad sooner; the deferred entry records that overload depends on the same rope coupling.
- Edge 4 / Blind 2: speed 0 bounds work by the energy slack's 0.01 J floor. Blind 3: low torque samples n = 10 and 120 only. Blind 4: the lamp blend allows a one-tick start slack and the comment says 5 ticks (0.042 s). Intent 5: the supply-cut fade is sampled mid-blend.
- Blind 7: new `MotorRotorArtTests.TheRotorArtFollowsTheCommittedRotorPoseAndResetsToItsScenePose` (speed 6 and 0) applies the committed rotor pose through `WorkshopPhysicalPresentation` and checks `Visual/Rotor` within 0.1 mm and 1 mrad, turned while spinning, at its scene pose at speed 0 and after Reset.
- Blind 10: new `EnablingTheBatteryDuringRunSpinsTheMotorUpFromRestAndLightsItsLamp` and `AMotorSuppliedThroughAnEitherGateRunsOnlyWhileTheGateIsOn` (input enabled or disabled).
- Blind 5: a revolute row's coordinate is documented as unwrapped signed travel (C# doc, schema comment, TS doc); the schema comment does not change generated output (pinned flatc regenerated byte-identical files).
- Blind 9: the "120 J" rationale is corrected to the power bound in the locked-shaft comment and the deferred recipe.
- Scoped checks: the Motor, rotor-art, observation, declared-presentation, clock-guard, parity and bundle-port classes 201/0/0, and `BundleValidatorTests` 66/0/0, all Skipped 0. Every new or changed test passed on its first run.
- Hook run 4 on the restaged candidate (44 paths, log `$TMPDIR/hook-4.log`): exit 0; Anvil scanned 32 snapshots with total 0, errors 0, `hasBlockingWarnings: false`; scoped tests 127/0/0, Skipped 0. After it, only this record line changed and was restaged.

**Review loop 2 fixes.**
- CAT-042 harvest rows 5 (torque range → decision 1), 7, 16 and 17 (no separate work budget; an unsupplied drive is clamped to [0, 0] and only bearing friction slows the rotor; open question 6) are marked superseded, and the Work-and-energy line is rewritten as legacy-only plus the shipped rule.
- The mixed-scene test now appends an undriven slider whose base is the Motor's housing (moving body a Weight). Its row sorts before the Motor's driven revolute row, so a first-owner-row lookup would leave the lamp slate; the test asserts gold. `Rebuilt` appends prismatic constraints too.
- The validator refuses a `CommittedActuatorDrive` binding on an owner with no driven slider or hinge (`Presentation.ActuatorDrive`), tested in `ACommittedDriveBindingIsInstantLinearOrATimedSmoothStep`.
- `DriveValue` takes a required unit on every call: prismatic target `Metre`, maximum speed `MetrePerSecond`, stopping acceleration `MetrePerSecondSquared`, maximum effort the new `ParameterUnit.Newton` (UI label "N"; no package parameter uses it, so there is no parity row), maximum power `Watt`; hinge target `RadianPerSecond`, torque `NewtonMetre`. Test: `AParameterBoundSliderDriveValueMustBeInItsOwnUnit`.
- The projection test asserts the literal wire kinds 0 and 1 the TypeScript enum declares.
- Lamp blend: gold from `Skip(12)` again; the mid-blend helper's doc covers its index-4 uses. Rotor art at speed 0: bounded by the rest envelope over the elapsed 0.25 s (0.005 rad). Mid-run enable: work exactly 0 before the enable; after it only KE ≤ work + slack.
- `SupportedDomains` is Mechanical | Electrical again (the description change stays); CAT-042 §2 says so.
- Scoped checks: `MotorTests` and `MotorRotorArtTests` 39/0/0; the observation, palette, declared-presentation, parity and bundle-port classes passed in the same build before the mixed-scene fixture's spring fix; `BundleElements.Tests` 234/0/0. All Skipped 0.
- Hook run 5 on the restaged candidate (44 paths, log `$TMPDIR/hook-5.log`): exit 0; Anvil scanned 32 snapshots with total 0, errors 0, `hasBlockingWarnings: false`; scoped tests 127/0/0, Skipped 0. After it, only this record line changed and was restaged.

**Status: implementation complete for review** (owner decision, 4 November 2026, on the stall and speed-0 rows). Browser qualification is pending until the post-catalogue phase.

**Process disclosures.**
- Anvil: production files and new package files were validated before writing (full content or patch for small files; a preview only for `MotorTests.cs`, `MotorElementDeclaration.cs` and `motor.tscn`). Several small test and registration edits were written without a prior Anvil call; the hook's Anvil scan covers the staged files.
- One rename of two call-site arguments in `MotorTests.cs` used `sed -i` instead of Edit.
- One stray placeholder file was written to the coordinator scratchpad and deleted at once; logs are in `$TMPDIR`.
- The regenerated wire hunks were landed with Edit; the files then compared byte-identical with pinned flatc 25.2.10 output (the pipeline first reproduced both committed files from the base schema).

## Review Triage Log

### 2026-11-04 — Review pass
- verdicts: 31 findings — high 1, medium 10, low 13, false 7, maybe-false 0.
- findings:
  - `[false]` `[reject]` (Intent 1) Stall proved on a locked hinge rather than a rim rope: owner decision of 4 November 2026; the rope variant is the next engine slice's acceptance.
  - `[false]` `[reject]` (Intent 2) Speed 0 proved unloaded: same owner decision.
  - `[medium]` `[patch]` (Intent 3) Overload hangs its load on a rope (the deferred interaction). Same root as Blind 1. → patch with Blind 1, and record the rope dependency in the deferred entry.
  - `[false]` `[reject]` (Intent 4) An activation wire into PowerIn is refused at connection validation rather than observed at run: refusing the invalid connection is the stronger outcome, so the row's "no rotation" holds.
  - `[low]` `[patch]` (Intent 5) The lamp's fade on supply cut is not observed (only its end state). → patch: sample mid-fade.
  - `[low]` `[reject]` (Intent 6) No energy check in the Save/Load, Reset and slot tests: none of them is a driving-behaviour case; every driven case carries the oracle.
  - `[false]` `[reject]` (Intent 7) The Motor's electrical node carries inert voltage data, the same schema as every powered package.
  - `[low]` `[patch]` (Intent 8) The index-bar decision is recorded in CAT-042 §2, not §6. → patch.
  - `[false]` `[reject]` (Intent 9) The housing shares the rotor's rigid material; consistent with the catalogue.
  - `[medium]` `[patch]` (Intent 10) The description promises a drive socket and `SupportedDomains` includes Mechanical before 11.1b. Same as Blind 8. → patch: remove both until 11.1b.
  - `[false]` `[reject]` (Intent 11) Generic changes outside the package (SmoothStep rule, wire `kind`, units) are allowed by the lamp-read clause and contain no Motor-specific engine code.
  - `[false]` `[reject]` (Intent 12) Differences that follow the owner's test policy (no exact arrival tick).
  - `[medium]` `[patch]` (Verification 1) The projected actuator `kind` and `joint` are untested in the observation projection. Same as Blind 11. → patch: extend the projection test with a prismatic and a revolute row; check the TypeScript and C# enums agree.
  - `[high]` `[patch]` (Forward-Only 1) CAT-042 harvest facts 10, 13 and 14 still read "carry forward", and outline lines 171–172 still name coasting and the 0.01 floor. Each documents a second rule. → patch: mark them superseded (owner Q9, and decision 1 at 0.1 N·m); rewrite the outline.
  - `[medium]` `[patch]` (Edge 1) The lamp binding takes the first actuator row with its owner. Now that every hinge emits a row, an owner with several slots could read an undriven one. → patch: the committed-drive lookup selects the row named by the owner's compiled consumer, with a mixed-slot test.
  - `[low]` `[patch]` (Edge 2) `CommittedActuatorState` can never report a revolute stall. → patch: the validator refuses it on an owner with no prismatic drive.
  - `[low]` `[patch]` (Edge 3) Drive values bound to parameters are not unit-checked. → patch: `DriveValue` requires `RadianPerSecond` for a hinge target and `NewtonMetre` for a hinge torque (prismatic units as already declared), with a refusal test.
  - `[low]` `[patch]` (Edge 4) Supplied speed 0 asserts `Work == 0f` exactly while the row solves. Same as Blind 2. → patch: a derived slack.
  - `[low]` `[reject]` (Edge 5) The spec's stall row describes a rope: the change log records the owner decision; the fix would edit this spec.
  - `[low]` `[reject]` (Edge 6) The spec's speed-0 row: same.
  - `[medium]` `[patch]` (Blind 1) The overload test passes even if the drive is unsupplied. → patch: assert `Supplied` on every tick, and an unpowered control that descends faster.
  - `[low]` `[patch]` (Blind 2) Same as Edge 4.
  - `[low]` `[patch]` (Blind 3) The low-torque n = 1 sample depends on when Coulomb friction engages. → patch: drop it; n = 10 and n = 120 carry the contract.
  - `[low]` `[patch]` (Blind 4) The lamp blend has no slack for its start tick, and a comment misstates 0.05 s. → patch: allow a one-tick start slack; fix the comment.
  - `[low]` `[patch]` (Blind 5) Revolute actuator rows report ±π limits while their coordinate is unbounded travel; the docs say "between its limits". → patch: document that a revolute row's coordinate is signed travel, and drop the claim for Continuous rows.
  - `[medium]` `[patch]` (Blind 6) Rows for undriven hinges and mixed scenes are untested. Same root as Edge 1. → patch: assert an undriven hinge's row and the owner lookup in a scene mixing a lever with a Motor or gate.
  - `[medium]` `[patch]` (Blind 7) Nothing proves the rotor art follows the shaft (harvest row 22). → patch: sample the `Visual/Rotor` presentation pose against the committed rotor body while spinning, at speed 0, and after Reset.
  - `[medium]` `[patch]` (Blind 8) Same as Intent 10.
  - `[low]` `[patch]` (Blind 9) The "120 J" rationale (in the locked-shaft comment and the deferred recipe) is the upper bound, not the delivered work (under 1 J). → patch: correct both.
  - `[medium]` `[patch]` (Blind 10) Supply is never switched on mid-run, nor routed through a logic element. → patch: enable the Battery mid-run (spin-up from rest, lamp slate→gold), and add one supply case routed through a switched element.
  - `[medium]` `[patch]` (Blind 11) Same as Verification 1.

## Design Notes

**Owner decisions (3 November 2026):**
- spins down under bearing friction;
- supply is on/off, with drain deferred;
- speed adjustable during Run is its own slice.

**Game-intent decisions** are listed under Always. The torque floor is 0.1 N·m, 2× τ_f, so the weakest Motor still visibly accelerates: at 0.1 N·m the gain is 0.0185 rad/s per tick.

## Verification

**Commands:**
- Prepend pinned Node v26.12.1 (`/private/tmp/claude-501/-Users-aidan-dev-personal-tim/5b49c1ca-9721-4a5d-8c03-bdf3a5db8b62/scratchpad/node-26.12.1-dl/node-v26.12.1-darwin-arm64/bin`) to `PATH`. Run the full block in `docs/delivery-workflow.md#local-test-authority` from the worktree root. Expected: 0 warnings, 0 errors, every test project `Skipped: 0`, 0 failed.
- Run `.githooks/pre-commit` unmodified on the staged candidate. Expected: exit 0, Anvil clean.

## Auto Run Result

Status: done (4 November 2026).

**Implemented:** the `elements/Motor/` package (CAT-042) as declaration data on the published generic engine.
- **Rotor:** a 0.5 kg rotor (axial inertia 0.0225 on Z through an Rx(90°) principal frame) on a `Continuous` hinge.
- **Drive:** a supplied velocity drive from the `speed` [0, 20] rad/s and `torque` [0.1, 100] N·m parameters.
- **Bearing friction:** 0.05 N·m, so the rotor spins down when unpowered (owner, 3 November).
- **Power:** a `PowerIn` consumer.
- **Supply lamp:** reads the generalised actuator read. That read now has one row per drive slot, keyed by `SupplyConsumer`, and the lookup selects the row named by the owner's compiled consumer.
- **Validator:**
  - every `DriveValue` requires a unit (slider and hinge);
  - `CommittedActuatorDrive` requires a driven joint;
  - `CommittedActuatorState` requires a driven slider.
- **Units added:** `RadianPerSecond`, `NewtonMetre` and `Newton`.
- **Game-intent decisions,** recorded in CAT-042 §6: spin direction, bearing friction and torque floor, rigid rotor material, decorative index bar.
- **Superseded rows:** CAT-042 legacy rows 5, 7, 10, 13, 14, 16 and 17 are marked superseded.

**Owner decisions:**
- **3 November:** spins down; on/off supply with drain deferred; run-time speed in its own slice.
- **4 November:** rope-coupled stall and speed-0 cases move to the next engine slice, `engine-tensile-drive-coupling` (deferred-work `#tensile-revolute-drive-creep`). In 11.1a, stall is proved on a locked hinge and speed 0 on an unloaded shaft.

**Review:**
- **Five-layer pass:** 31 findings: high 1 (Forward-Only doc rows), medium 10, low 13, false 7. Patched: 1 high, 10 medium, 10 low. Rejected: 7 false and 3 low, with reasons in the triage log.
- **Follow-up re-review:**
  - Round 1 found 2 blocking items (more superseded doc rows; a non-discriminating lookup test) and 6 lower ones. All were fixed.
  - Round 2: all confirmed correct, no new defect. Pass.

**Verification:** final coordinator full round per `docs/delivery-workflow.md#local-test-authority` with pinned Node 26.12.1. Exit 0; 5,516/0/0, `Skipped: 0` in every project:

| Project | Passed |
|---|---|
| CuriousContraptions.tests | 4,996 |
| BundleElements.Tests | 234 |
| Coverage.Tests | 77 |
| E2eRunner.Tests | 192 |
| WorkerAotGuard.Tests | 17 |

Implementer hook: exit 0, Anvil clean, 127/0/0. Red-first: all 30 Motor tests failed on the base before the package existed.

**Matrix audit:** every row is covered by a `MotorTests` / `MotorRotorArtTests` / validator / parity / observation test that ran and passed. The stall and speed-0 rows are covered by their owner-approved non-rope variants.

**Residual risks:**
- **Rope coupling:** a rope cannot hold a supplied drive (next engine slice). The overload case also hangs its load on a rope.
- **No TS/C# enum-parity pattern:** the wire values are pinned instead.
- **Browser:** qualification is pending until the post-catalogue phase.

**Process disclosures:**
- In loop 1 the implementer edited one file with `sed -i`, validated three new files' Anvil content only by preview, and briefly wrote a stray placeholder file to the coordinator scratchpad before deleting it.
- The coordinator skipped the advisor checkpoint before finalising this spec plan; it built on the reviewed engine slice.
