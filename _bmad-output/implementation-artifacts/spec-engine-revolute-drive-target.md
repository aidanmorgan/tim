---
title: 'engine-revolute-drive-target — a supplied hinge drive''s target speed becomes a run control (Motor speed during Run)'
type: 'feature'
created: '2026-11-04'
status: 'done'
baseline_revision: 015e902d09950d10a333c61c541c6b579993938b
review_loop_iteration: 0
followup_review_recommended: true
context:
  - '{project-root}/docs/architecture/element-package-isolation.md'
  - '{project-root}/docs/testing-guidance.md'
  - '{project-root}/docs/gpu-f32-physics.md'
warnings: ['oversized']
deferred: []
---

<intent-contract>

## Intent

**Problem:** The owner decided on 3 November 2026 that the Motor's `speed` is adjustable during Run (CAT-042 open question 5). The generic run-control path (engine-run-control-table, main `30fcfb56`) has one lane, `SourceEnable`. A hinge drive's target speed is compiled into the revolute record's immutable declaration bytes, so nothing can change it during a run.

**Approach:** Add a second run-control lane, `RevoluteDriveTarget`, through the same declaration → table → command → committed-read path.
- **Committed target:** the revolute record's target speed (byte 104) becomes committed state. Admission writes the authored value, each tick writes the authorised value, and the solver keeps reading that one field.
- **Declaration:** the Motor declares `speed` as `RunControl`. The resolver binds a parameter to `RevoluteDriveTarget` when a hinge drive's `TargetSpeed` is that parameter.
- **Bounds:** each row carries the declared parameter's range. The worker refuses an out-of-range value with a typed rejection.
- **UI:** a numeric run control is the first real use of `RunControlValueKind.Scalar`.

## Boundaries & Constraints

**Always:**
- **Data-driven and generic.** Nothing in the engine is named after an element. The `GenericIdentifiers` gate (`WorkshopPhysicsLawTests.cs:738-834`) forbids "motor" in scanned engine identifiers; use names like `DriveTarget`. Any part whose hinge drive targets a parameter gets the control from its declaration alone.
- **One field, one rule (forward-only).**
  - Byte 104 is the only target field: there is no second copy of the authored value. The solver, supply, presentation and every ABI read take the committed value. `ReadRevoluteDrive(...).TargetSpeed` means the committed target.
  - The candidate lock in `ValidateRevoluteCandidate` covers [0,104) and [108,112) instead of [0,112). The committed target is checked by the existing record rule (finite, |ω| ≤ `AngularVelocity.MaximumSpeed`, zero when the drive kind is None), not by a second equality check at the device.
- **One index space.** A `RevoluteDriveTarget` row's lane index is the position in `physics.RevoluteDrives`. The tick array, its `Validate`, the committed drive read, `Resolves`/`Slot` and the engine's revolute-to-drive mapping all use that index.
- **Applies from the next committed step, on every tick.** No electrical-phase gate. The tick controls start from the committed targets and apply held rows. The engine writes each driven revolute's target from the tick input before its substeps.
- **Committed-value checks in the simulation.**
  - `ValidateInitialRead`: each admitted committed target equals the compiled authored target.
  - `ValidateAdvancedRead`: on every advanced read, outside the electrical `hasPhase` block, each driven revolute's committed target equals the held value if one is held, and the previous committed target otherwise.
- **Bounds at the boundary.**
  - Rows carry `Minimum`/`Maximum`, compiled from the declared parameter. `WorkshopCompiledScene.Validate` requires them to be zero on an Enable-kind row, and finite, ordered and within ±`AngularVelocity.MaximumSpeed` on a `RevoluteDriveTarget` row.
  - `ConfigureRunControl` refuses a scalar outside [Minimum, Maximum] with a new `WorkshopRejection.RunControlOutOfRange = 20`. This check comes after kind mismatch.
  - The wire already refuses non-finite values (`WireFaultException`).
- **Declaration rules.**
  - `RunControlBindings.Lanes` adds `RevoluteDriveTarget` once for each hinge whose `Drive.TargetSpeed` is `FromParameter(key)`. A key bound by two hinges, or by both a store enable and a drive, therefore has several lanes, and the validator refuses it (`Metadata.Parameters[i].RunEdit`, `OutOfRange`).
  - A part with a `RevoluteDriveTarget` run control must declare exactly one driven hinge, so the owner's slot is unique. Otherwise the same `OutOfRange` refusal applies.
  - Units are already enforced: a drive target parameter must be rad/s, within ±128 (`BundleValidator.cs:667-691`).
- **Motor:** `speed` is `RunEdit = RunControl`. `torque` stays `BuildOnly`.
- **Lifecycle (unchanged rules, now also for speed):**
  - Paused hold, where a second change replaces the first; applied on Resume or Step.
  - A committed Reset clears held controls and re-admits the authored target. A cancelled Reset keeps them. A discarded tick keeps them.
  - Construct, Configure and Run from Building clear held controls.
  - A run control never edits `WorkshopConstruction`, so a save stores the authored speed.
- **UI.**
  - A `Scalar` control renders as a label, a `LineEdit` (named after the parameter key) and an apply button. The label shows the parameter, its units (`ParameterUnits`), the committed value, and "(queued)" while a change is pending. The per-frame refresh updates the label and never overwrites the field.
  - Apply parses with `float.TryParse(..., InvariantCulture)`. Text or non-finite input is refused before sending, as the build editor does ("Enter valid parameter values."). Out-of-range input follows the parameter's `EditorPolicy`: `Reject` refuses with `WorkshopEditRejectedException(WorkshopEditRejection.OutOfRange)` through `WorkshopUiFailure.Report`, as `ApplyDeclaredParametersAsync` does; `Clamp` clamps. Nothing is sent in either refusal case.
  - The send goes through the existing `ApplyRunControl`, so no CA1031 pin changes. The status text depends on the value kind: the Battery's "Supply change …" text is unchanged, and a scalar says "<key> change accepted for the next physics step." / "<key> change was not applied.".
  - A locked part has no row. Its control shows the committed value, disabled, with no field and no action. `RunControlNext` stays Enable-only.
- f32; strict TypeScript mirrors. Regenerate the wire with the pinned flatc 25.2.10 per `schemas/README.md`. Full local round with `Skipped: 0`, including the node harnesses. Red first.

**Never:**
- a torque run control, negative or reversing speeds, battery drain, a slider, or a per-element handler;
- a second target field, or an authored copy kept beside the committed one;
- a clamp in the worker (the worker refuses; only the UI applies `Clamp`);
- changes to the Battery toggle's behaviour or text;
- weakened or deleted existing assertions (rename API calls only, and record each changed test);
- running browser E2E.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|---|---|---|---|
| Speed up during Run | Supplied Motor authored speed 6, spun up; `ConfigureRunControl(motor, its row, 12)` | From the next committed step the committed target reads 12. The shaft spin reaches 12 ± 2% within the derived ramp plus one tick, then stays there. | No error expected |
| Speed down to 0 | Spun up at 6; control 0 | Committed target 0. The shaft comes to rest and holds within the rest-spin floor (speed 0 is not covered by the 2% allowance). | No error expected |
| Set while unsupplied | Battery disabled; control 12; then enable the Battery | The committed target is 12 while the shaft stays at rest. Once supplied, the shaft spins up to 12. | No error expected |
| Two lanes in one tick | Battery and Motor both controlled before one step; then a later control on one only | Both apply on the same committed step. The later control leaves the other lane's committed value unchanged. | No error expected |
| Paused hold and replace | Paused; controls 10 then 14; Step | Held, the second replaces the first, and the committed target is 14 after one Step | No error expected |
| Reset and save | Speed changed mid-run; save during Run; committed Reset; cancelled Reset | The save holds speed 6. Reset re-admits target 6 and clears the table. A cancelled Reset keeps the held control. | No error expected |
| Discarded tick | Control held when a tick is discarded | Still held, and applied on the next committed step | No error expected |
| Bounds | 0 and 20 | Applied | No error expected |
| Out of range | −1 and 20.001 | Refused `RunControlOutOfRange`; nothing held; the target is unchanged | Typed rejection |
| Kind mismatch | An Enable value on the speed row | Refused `RunControlKindMismatch` | Typed rejection |
| Non-finite on the wire | NaN or ∞ scalar | `WireFaultException(OutOfRange)` at decode (already covered; keep) | Typed wire fault |
| Dropped change | A candidate read whose committed target ignores a held control | `ValidateAdvancedRead` faults the advance | Typed fault (`ArgumentException` path as today) |
| Record defects | Non-finite or over-envelope committed target; non-zero target on an undriven revolute | ABI read refuses | Existing typed refusal |
| Declaration | `speed` bound by two hinges; `torque` declared RunControl (no lane); a RunControl drive target on a part with two driven hinges | Bundle validation refuses (`Metadata.Parameters[i].RunEdit`, `OutOfRange`) | Typed validation error |
| UI | Running Motor: type 12 and apply; type −1, 20.001 or text; locked Motor | 12 sends `RunControlValue.Of(12f)` with no construction edit. The others are refused with no command sent: out of range as the typed `OutOfRange` edit rejection, text as "Enter valid parameter values.". A locked Motor is shown disabled with no action. | Typed UI refusal |
| Round trip | Command, table rows with bounds, committed drive read | Encode → decode preserves every field; TS mirror numbers updated | No error expected |

</intent-contract>

## Code Map

- **Lane, rows, read** — `engine/gpu/WorkshopRunControl.cs`
  - `RunControlRow` (:47) gains `Minimum`/`Maximum`.
  - `RunControlLanes.ValueKind` (:61), `Resolves` (:71), `Slot` (:83), `RunControl(read, owner, lane)` (:91) and `Find` get the new lane.
  - `RunControlLane` lives at `CuriousContraptions.Abstractions/ParameterDescriptor.cs:45` (update the doc comment at :42-45).
- **Resolver and validator**
  - `CuriousContraptions.Bundle/RunControlBindings.cs:12-17`: today only `Store.Enable`. Add hinge drives from `entry.Mechanical?.Hinges[i].Drive?.TargetSpeed` (`MechanicalValue.Source == Parameter`, `.Parameter == key`; `MechanicalContracts.cs:132-178`).
  - `BundleValidator.cs:106-111`: the exactly-one-lane rule; add the exactly-one-driven-hinge rule beside it.
  - Drive value rules: `:667-691`.
  - Tests: `tools/BundleElements.Tests/BundleValidatorTests.cs:487-499` (several bindings goes here), `:502-516` (its name and "no parameter off a Source may be run-editable" case become inaccurate: rename and adjust to the new rule, recorded) and `:1209-1221`.
- **Declaration lookup:** `engine/gpu/DeclaredPhysics.cs:86-88` `RunControls`. Unchanged shape, new lane flows through.
- **Motor:** `elements/Motor/MotorElementDeclaration.cs:39` (`speed`, 0–20 rad/s, default 6) gets `RunEdit: ParameterRunEdit.RunControl`. `:108` binds `Drive.TargetSpeed = FromParameter("speed")`. Update the comment at `:10-13`.
- **Compiler and table**
  - `engine/gpu/WorkshopSceneCompiler.cs:36-40` builds rows (add bounds from the declared parameter).
  - `engine/gpu/WorkshopPhysicsCompiler.cs:146-152` builds `revoluteDrives` (sorted by joint id, `PhysicsDeclarations.cs:507-508`; owner = `RevoluteConstraintDeclaration.BodyA`).
  - Slot pattern: `CompiledSupplyGraph`, `SupplyRoute.cs:115-117`.
  - `engine/gpu/WorkshopCompiledScene.cs:61-73`: row validation.
- **ABI** — `engine/gpu/PhysicsGpuAbi.Constraints.cs`
  - The layout comment (:41-44) changes: the target at 104 is committed state.
  - `WriteRevoluteAdmission` :227; `ReadRevoluteDrive` :234; `RevoluteDriveOf` :256; `ReadRevolute` :262-283 (record checks).
  - `ValidateRevoluteCandidate` :286-315: split the lock.
  - Also grep `RevoluteDriveTargetSpeedOffset|RevoluteDriveOf|ReadRevoluteDrive|DriveTargetSpeed` and move every behavioural reader to the committed meaning.
- **Tick path**
  - `engine/gpu/SupplyRoute.cs:302-325`: `WorkshopTickControls(SourceEnable)` plus `For`/`Validate`. Add the drive target array.
  - `:336-351`: `PhysicsTickInputs.Compose`/`Validate` carry it.
  - `CuriousContraptions.Simulation/WorkshopGpuDevice.cs:131,154`.
  - Engine: `CuriousContraptions.Simulation/WorkshopPhysicsEngine.cs`. Cuts are applied at tick start (:223-240); the revolute load loop (:549-593) reads the target at :580. Write the tick target into byte 104 before the substeps; :1026-1030 is the write-back.
- **Read**
  - `WorkshopRead` (`engine/gpu/WorkshopSimulation.cs:17-19`) gains the committed drive read: per driven revolute, owner and committed target, in `RevoluteDrives` order, decoded from the world like `ReadElectrical` (`PhysicsGpuAbi.Electrical.cs:79`).
  - Wire: the read encoder/decoder in `engine/gpu/WorkshopWireFlatBuffers.cs` (electrical read pattern) and `schemas/workshop_wire.fbs`.
  - Production pass-through: `MachineWorld.WorkshopRead`.
- **Simulation**
  - `WorkshopSimulation.ConfigureRunControl` (:97-107): add the bounds check.
  - `WorkshopRejection` (:12-15): add 20.
  - `ValidateInitialRead` (:519-555) and `ValidateAdvancedRead` (:575-585).
  - `CandidateRead` builds the read.
- **Wire and mirrors**
  - `schemas/workshop_wire.fbs:56-57`: `RunControlLane`; `RunControlRecord` gets the bounds; plus the drive read record.
  - Generated `engine/gpu/WorkshopWireGenerated.cs` and `tools/e2e/workshop-wire.ts`.
  - Compiled-scene codec: `WorkshopWireFlatBuffers.Compiled.cs:820-930`.
  - `CuriousContraptions.web/wwwroot/workshop-observation.ts:44` (rejection 20) and its pin at `tools/runtime/workshop-observation.test.ts:629-640`.
  - Field lists: `tools/e2e/run-reset-snapshot.ts` `SNAPSHOT_SCHEMA` and `WorkshopFormatTests` field lists.
- **UI** — `ui/WorkshopConfiguration.cs`
  - Run-control code is at :75-169. `_runControlViews` (:81) is typed to `Button`; split it or generalise it for a label/field/button view. Also `RunControlIntent` (:87), `RunControlText` (:101, Scalar arm) and `ApplyRunControlAsync` (:140-169, status text).
  - Build editor pattern: :18-73 and `ApplyDeclaredParametersAsync` :196-227.
  - `ParameterUnits` :171-188.
  - Visibility is unchanged: `ui/WorkshopGuidance.cs:328-331`.
  - Pins: `.editorconfig:69-70`, `CSharpStaticGateTests.cs:34,59-62,163-164,865,957`. `ApplyRunControl` is in `EditValidationPaths`: a refusal there must be `WorkshopEditRejectedException`.
- **Test bases**
  - `MotorTests.cs`:
    - constants: `RotorInertia` :25, `FrictionTorque` :26, `RestSpin` :38, `SpeedAllowance` :41, `EnergySlack` :45;
    - `RampTicks` :49-50;
    - builders: `MotorPart` :57, `Supplied` :69;
    - harness: `Probe` :105-136 and `Start` :149;
    - checks: `AssertEnergy` :195-202, the speed-0 hold test, and the run-control uses at :339 and :372.
  - `RevoluteDriveTests.cs`: `Rig` :61-100 (passes `WorkshopTickControls` directly), record-defect tests :722-741 and :758ff.
  - `SourceStoreInstanceTests.cs:153-164` (add Motor as a positive run-control declaration) and :273-311 (save/Reset pattern).
  - `RunControlTableTests.cs`.
  - `WorkshopSimulationTests.cs` controlled device (record controls; serve reads).
  - `PalettePlacementTests.cs:1634-1690` (toggle test) and :1909-1957 (typing into a `LineEdit`); fake client :1963-2035 (`RunControls`, `Controls`, served reads must carry the drive read).
  - `WorkshopIntegrationHarness.RunControl(owner, lane, value)`.
- **Docs**
  - `docs/planning/elements/CAT-042-motor.md`: :38, :65, harvest row 19 (:125), open questions 1 and 5 (:179, :183), acceptance lines :172-174.
  - `docs/architecture/element-package-isolation.md:107`: name the second lane.
  - `docs/testing-guidance.md` if it lists run controls.
  - `TODO.md`.

## Tasks & Acceptance

**Execution:**
1. **Red first.** Write the matrix tests before the implementation and record the failing compile or run:
   - `MotorTests` (speed-up, speed-to-0, unsupplied, energy);
   - `RunControlTableTests` (bounds, kind mismatch, two lanes, round trip with bounds and drive read);
   - `SourceStoreInstanceTests` (Motor declares a run control);
   - a `WorkshopSimulationTests` dropped-change fault plus discarded tick and paused Step for speed;
   - `RevoluteDriveTests` (record defects, lock split);
   - `BundleValidatorTests` (several bindings, torque, two driven hinges);
   - the `PalettePlacementTests` numeric control;
   - the node test (rejection 20).
2. **Schema and generated code.** Update `schemas/workshop_wire.fbs` (the lane enum, row bounds and the committed drive read) and regenerate with pinned flatc into the scratchpad. Write the output into the tree with the Write tool only. Update the TS mirrors and field lists.
3. **Declaration path:**
   - `ParameterDescriptor.cs`: the lane enum;
   - `RunControlBindings.cs`: hinge drive targets;
   - `BundleValidator.cs`: the driven-hinge rule;
   - `MotorElementDeclaration.cs`: `speed` RunControl.
4. **Engine path:**
   - ABI (`PhysicsGpuAbi.Constraints.cs`): committed meaning and the lock split;
   - `SupplyRoute.cs`: tick controls and inputs;
   - `WorkshopGpuDevice.cs` and `WorkshopPhysicsEngine.cs`: write the target at tick start;
   - the committed drive read;
   - `WorkshopRunControl.cs`: lane cases and bounds;
   - `WorkshopSceneCompiler.cs` and `WorkshopCompiledScene.cs`: rows and validation;
   - `WorkshopSimulation.cs`: rejection 20, bounds, initial and advanced checks.
5. **UI** (`ui/WorkshopConfiguration.cs`): the scalar view, refusals and status text.
6. **Existing tests:** update fakes (the `PalettePlacementTests` client and the `WorkshopSimulationTests` device serve the drive read) and API calls only. Record each changed test.
7. **Docs** as in the Code Map. In `TODO.md`, after publication, the current slice becomes Story 11.1b.
8. **Verification:** the full local round and the unmodified hook. Stage, do not commit.

**Acceptance Criteria:**
- Given a running supplied Motor, when the player applies a new speed in the run panel, then from the next committed step the shaft drives toward the new speed, and the authored construction, save and Reset keep the authored speed.
- Given any declared package whose single driven hinge targets a RunControl parameter, when Run compiles, then the table has its `RevoluteDriveTarget` row with the parameter's bounds, with no element-specific engine code.
- Given every candidate the device returns, when a held speed control exists, then a candidate that does not commit the authorised target faults the advance.

## Spec Change Log

## Review Triage Log

### 2026-11-04 — Review pass
- verdicts: 18 findings — high 0, medium 6, low 6, false 6, maybe-false 0
- findings:
  - `[false]` `[reject]` (Intent Alignment) Paused hold/replace, the discarded tick, the cancelled Reset and the dropped change are proven on the controlled device, not the real engine — the shared harness's real device completes every tick at once, so a discarded tick or cancelled Reset can only be staged on the controlled device (the same split engine-run-control-table used). The real engine's committed target on the same Advance path is proven in `RunControlTableTests` and `MotorTests`, including committed Reset and save.
  - `[low]` `[reject]` (Intent Alignment) The UI test asserts status strings rather than the typed `OutOfRange` exception, and the run control's `Clamp` branch is never exercised — `ApplyRunControl` is in `EditValidationPaths`, whose static gate enforces the typed refusal. No shipped RunControl parameter uses `Clamp`, and the shared `EditorValue` helper's clamp is covered through the build editor. Adding a synthetic Clamp control is more than a direct fix for a path no part reaches.
  - `[low]` `[reject]` (Intent Alignment) The generic declaration claim is tested only through the Motor — the Motor is the only shipped part whose hinge drive targets a parameter. The validator tests use altered entries (doubled hinge, second drive), and the engine and compiler code is name-free (`DriveTarget`, `RevoluteDriveTarget`).
  - `[false]` `[reject]` (Intent Alignment) The observation windows differ from a literal "ramp plus one tick" — none pins an exact arrival tick. Each window is the derived ramp plus latency, or a deliberately loose bound, as the Design Notes allow.
  - `[false]` `[reject]` (Intent Alignment) The committed drive read round-trips only through the harness's `DecodeResponse` — that is the production response codec, so the round trip is real. The review fix adds a direct wire-equality case in `WorkshopReadTests`.
  - `[false]` `[reject]` (Intent Alignment) No new test for a non-zero target on an undriven revolute — the existing record rule (`PhysicsGpuAbi.Constraints.cs:288`) and the existing `RecordDefect.UndrivenWithDriveValues` case cover it unchanged.
  - `[false]` `[reject]` (Intent Alignment) The changed-test record is outside the reviewed patch — it is in the spec's Implementation Record, as required.
  - `[false]` `[reject]` (Forward-Only, borderline) The status-line subject is chosen by value kind ("Supply" for an enable, the parameter key for a scalar), which the auditor places on the "second rule" side — it is not a second rule for the same behaviour. Each value kind renders its own typed value, which the intent allows ("no per-lane UI branch beyond rendering the typed value"), and slice A's per-kind `RunControlText` arms already do the same.
  - `[low]` `[patch]` (Forward-Only, side note) The subject lookup by (owner, value kind) would name the wrong parameter on a part with two scalar controls — the subject is now looked up by the control's row id.
  - `[medium]` `[patch]` (Edge Case Hunter; grouped with the Blind Hunter row below) After an accepted Set, the panel rebuild seeded the field from the committed read, so it showed the old speed and a second Set silently resent it — the field is now seeded from `RunControlIntent` (queued value while pending). The UI test asserts the rebuilt field reads "12" and that a second Set sends 12.
  - `[low]` `[reject]` (Edge Case Hunter) A decoded drive read with a repeated owner would make the committed read take the first match — a repeated owner arises only for a part with two driven hinges, which the validator forbids from declaring a drive-target run control, so no lookup reads it. Advance and admission already refuse a drive-row population mismatch.
  - `[low]` `[reject]` (Edge Case Hunter) A RunControl drive-target parameter also used by another compiled field would keep its authored value there — the Motor's `speed` feeds only `Drive.TargetSpeed` (`MotorElementDeclaration.cs:41,111`). The case is hypothetical, and its fix is a new validator guard. The next slice to declare a run-controlled speed (11.1b Conveyor) must check this; it is noted in TODO.
  - `[medium]` `[patch]` (Verification Gap) No scene had an undriven revolute sorting before a driven one, so the engine's driven-rank → drive-index mapping was never exercised where rank ≠ revolute index — added `RunControlTableTests.TheEngineMapsEachDriveTargetByDriveRankPastAnUndrivenHinge`. An Impact Lever sorts first, then a supplied Motor set to 12 and a second Motor kept at 6. The test asserts drive index 0 ≠ revolute index 1, the per-tick committed targets, and the spin within 2% of 12.
  - `[medium]` `[patch]` (Verification Gap) Read equality compares drive targets but was untested — `WorkshopReadTests.MotionAndOccurrenceChangesAreNotDuplicateAcknowledgements` now checks: equal after a wire round trip; different for 12 vs 6 and for no drive row.
  - `[medium]` `[patch]` (Blind Hunter) The same rebuilt-field defect — grouped with the Edge Case Hunter row above; same fix.
  - `[medium]` `[patch]` (Blind Hunter) The tick-input target guard was untested — added `RevoluteDriveTests.TickInputsWithoutOneAdmittedTargetPerDriveAreRefusedAtTheEngineAndTheDevice` (missing, extra, NaN, ±∞, beyond ±128). Both boundaries throw with the target unchanged, and a valid advance succeeds after.
  - `[medium]` `[patch]` (Blind Hunter) The admission drive check and the owner/population branches of the per-tick check were untested — added `WorkshopSimulationTests.AnAdmissionReadWithoutTheDeclaredDriveTargetsIsRefused` and `AnAdvancedReadWithAForeignOrMissingDriveRowFaultsTheAdvance`; each is Faulted/InvalidRead with nothing committed.
  - `[low]` `[reject]` (Blind Hunter) The browser observation projects the Battery's committed enable but not the committed drive target — the observation serves the deferred browser E2E phase, and no current consumer reads it. Projecting a new TS observation type is more than a direct fix. The post-catalogue browser case for speed during Run (CAT-042) adds it with its first consumer.

### 2026-11-04 — Follow-up review pass (fix delta)
- verdicts: 2 findings — high 0, medium 0, low 2, false 0, maybe-false 0 (all six first-pass fixes judged Pass)
- findings:
  - `[low]` `[patch]` The engine-side "nothing committed" assertion in `TickInputsWithoutOneAdmittedTargetPerDriveAreRefusedAtTheEngineAndTheDevice` re-read the read-only source and could never fail — deleted; the comment now claims only the typed refusal at the engine, and the device-side unchanged-target check stays.
  - `[low]` `[patch]` Nothing proved a tick target of exactly ±`AngularVelocity.MaximumSpeed` is accepted, so a `>=` slip in `ValidateDriveTargets` would pass — the same test now advances ±128 at the engine and advances, commits and reads back ±128 at the device. `Rig.AdvanceWith` commits on success, like `Rig.Advance`.

## Design Notes

- **Why byte 104 becomes committed rather than gaining a copy.** The electrical source keeps a declared enable beside its committed enable. For the drive, a kept authored copy would have no behavioural reader: Reset re-admits from the compiled scene, and `ValidateInitialRead` compares against the compiled declaration. So one field with a narrower lock is the forward-only shape.
- **Ramp bound.** The change applies from the next committed step: allow one tick of latency, plus `RampTicks` for Δω = |12 − 6| (I·Δω/(τ − τ_f), as `MotorTests.RampTicks`). Slowing down is helped by friction, so the same bound is a deliberately conservative upper limit for 6 → 0.
- **Energy across a change.** Bound the drive's work by τ·max(|ω_target|)·t across the run, and keep kinetic ≤ work + `EnergySlack`. This is looser than the exact bound but correct and game-grade.
- **Index example.** `physics.RevoluteDrives` is sorted by joint id. A row for owner X uses i where `Revolutes[j].BodyA == X` and `RevoluteDrives[i].Joint == Revolutes[j].Id`. The engine's revolute loop finds its drive index the same way, never through `layout.Prismatics + index`, which is the supply index.

## Verification

**Commands:**
- Prepend pinned Node v26.12.1 (`/private/tmp/claude-501/-Users-aidan-dev-personal-tim/5b49c1ca-9721-4a5d-8c03-bdf3a5db8b62/scratchpad/node-26.12.1-dl/node-v26.12.1-darwin-arm64/bin`) to `PATH`. Run the full block in `docs/delivery-workflow.md#local-test-authority`. Expected: 0 warnings, 0 errors, every project `Skipped: 0`, 0 failed, node harnesses included.
- Run `.githooks/pre-commit` unmodified on the staged candidate. Expected: exit 0, Anvil clean.
- `rg -n -i 'motor' CuriousContraptions.Simulation engine/gpu --type cs` finds no new engine identifier.

## Implementation Record — 4 November 2026

Candidate staged on `engine-revolute-drive-target` over baseline `015e902d`; not committed. Independent step-04 review is pending.

- **Red first.** The node test failed to compile on `WorkshopRejection.RunControlOutOfRange`; then the C# tests failed to compile (104 errors) on the missing lane, row bounds, committed drive read and tick-input target array.
- **Decisions the spec left implicit.**
  - The drive-target array is an init property `DriveTarget` on `WorkshopTickControls` and `PhysicsTickInputs` (the `Cuts`/`Pulleys` pattern). Both `Validate` overloads require exactly one finite, in-envelope target per compiled drive; an empty array is valid only with no drives. `WorkshopTickControls.For` takes the committed `WorkshopRead`.
  - The committed read is `WorkshopRead.DriveTargets` (`PhysicsDriveTargetRead` of `RevoluteDriveTargetRead(Owner, TargetSpeed)`), decoded from the world by `PhysicsGpuAbi.ReadDriveTargets` and carried on the wire as `WorkshopResponse.drive_targets` (field 30, appended; the runtime decoder's response field count is now 31).
  - `RunControlRecord` appends `minimum`/`maximum` after `lane`, so the lane byte stays at struct offset 20.
  - The engine writes byte 104 for each driven revolute right after validating the inputs, by the revolute's rank among driven revolutes (both tables are in joint identity order).
  - The UI clamp shares one `EditorValue` helper with the build editor.
- **Generated code.** flatc 25.2.10 regenerated into the scratchpad; the unchanged schema first reproduced the committed files byte for byte. The schema diff's hunks were applied with the edit tool, and the tree files were then compared byte for byte with the scratchpad output (C# and the generated TS part identical; the authoring output unchanged).
- **New tests.**
  - MotorTests: speed up, speed to 0 with a control, set while unsupplied, save during Run and Reset.
  - RunControlTableTests: drive-target row and bounds, row refusals, round trip, bounds and kind mismatch, two lanes in one step (120 Hz), and a drive target applied on a 240 Hz tick without an electrical phase while the enable waits for the next phase.
  - WorkshopSimulationTests: a dropped change faults, a change without a held control faults, discarded tick, cancelled and committed Reset, paused replace and Step.
  - RevoluteDriveTests: the lock split, the committed drive read.
  - BundleValidatorTests: one lane on exactly one driven hinge.
  - PalettePlacementTests: the numeric run control.
  - Node: rejection 20.
- **Changed existing tests** (API, fixture or name only; no assertion weakened):
  - MotorTests: `Tick` gains the committed `Target`.
  - SourceStoreInstanceTests: renamed `ARunTimeControlExistsOnlyWhereTheStoreDeclaresItsEnableStateRunEditable` → `ARunTimeControlExistsOnlyWhereThePackageDeclaresABoundParameterRunEditable`, with the Motor assertion added.
  - BundleValidatorTests: renamed `ARunControlIsDeclaredOnlyOnASourceStoreEnableAndAChoiceIsNeverClamped` → `ARunControlIsDeclaredOnlyOnABoundParameterAndAChoiceIsNeverClamped`, comments only.
  - Fakes and helpers that serve or step reads:
    - the WorkshopSimulationTests `ControlledDevice` serves the drive read and commits the controls' targets;
    - the PalettePlacementTests client serves the drive read;
    - the RevoluteDriveTests `Rig` and engine-direct input carry `DriveTarget`, as do `WorkshopPhysicsLawTests`, `WorkshopPhysicsEngineTests.StepWorld` and `WorkshopPhysicsLawCylinderTests`.
  - API renames:
    - StateLatchTests: `Validate(..., 0)`;
    - EventRingProducerTests and WorkshopGpuDeviceTests: `For(read, [])`;
    - DeclaredPrismaticDriveTests: explicit enables.
  - WorkshopFormatTests: the WorkshopResponse field list adds `drive_targets`.
- **Verification.** Full local round per `docs/delivery-workflow.md#local-test-authority` (SDK 10.0.403 selected locally, Node v26.12.1): 0 warnings, 0 errors in all five compiler-log builds. Tests 5,563 passed, 0 failed, `Skipped: 0`:
  - CuriousContraptions.tests 5,041;
  - BundleElements.Tests 236;
  - E2eRunner.Tests 192;
  - Coverage.Tests 77;
  - WorkerAotGuard.Tests 17.

  The unmodified `.githooks/pre-commit` on the staged candidate exited 0: 127/0/0, Anvil with no blocking warnings. The `tools/e2e` TypeScript also type-checks clean with `tsc --noEmit`.
- **Known gaps.**
  - No shipped RunControl parameter uses `EditorPolicy.Clamp`, so the run-control clamp branch is covered only through the shared helper's build-editor use.
  - Browser qualification is pending until post-catalogue E2E.

### Review fixes — 4 November 2026

- **UI.** A scalar field is seeded from `RunControlIntent` (the queued value while pending), so a rebuilt panel no longer shows the old committed speed and a second Set resends the queued value. The status subject is found through the control's compiled row id, not (owner, value kind).
- **Changed test:** `TheNumericRunControlSendsATypedScalarAndRefusesInvalidInputBeforeSending` now asserts the rebuilt field reads 12 and a second Set sends 12 again.
- **New tests**
  - RunControlTableTests `TheEngineMapsEachDriveTargetByDriveRankPastAnUndrivenHinge`: an undriven Impact Lever hinge sorts first, so drive rank ≠ revolute index.
  - WorkshopReadTests `MotionAndOccurrenceChangesAreNotDuplicateAcknowledgements` gains drive-target read equality: 6 equals itself through the wire; 12 and no row differ.
  - RevoluteDriveTests `TickInputsWithoutOneAdmittedTargetPerDriveAreRefusedAtTheEngineAndTheDevice` (7 cases), plus the Rig helper `AdvanceWith`.
  - WorkshopSimulationTests `AnAdmissionReadWithoutTheDeclaredDriveTargetsIsRefused` (4 cases) and `AnAdvancedReadWithAForeignOrMissingDriveRowFaultsTheAdvance` (3 cases).
  - `ControlledDevice.CompleteAdmission` takes an optional read transform.
- **Verification:** the covering classes only (17 new or changed cases pass; 162 across the affected classes). The full round is the coordinator's.
- **Second review fix.** `TickInputsWithoutOneAdmittedTargetPerDriveAreRefusedAtTheEngineAndTheDevice` drops the engine-side unchanged-target assertion. It re-read the engine's read-only source, so it could never fail; the comment now claims only the typed refusal at the engine. The test adds boundary controls: exactly ±128 rad/s advance at the engine, and at the device each is committed. `Rig.AdvanceWith` now commits like `Advance`. RevoluteDriveTests 75/0/0.

## Auto Run Result

Status: done (4 November 2026). No blocking condition.

**Summary.** A supplied hinge drive's target speed is now a run control, so the Motor's `speed` can be changed during Run (owner, 3 November 2026; CAT-042 open question 5).
- **Lane and declaration:** the generic run-control path gains its second lane, `RevoluteDriveTarget`. The resolver binds it from any hinge drive whose target speed is a parameter. The validator requires exactly one lane and exactly one driven hinge, and the Motor declares `speed` RunControl.
- **Engine:** the revolute record's byte 104 is the single committed target. The candidate lock is split around it, admission writes the authored value, and every tick writes the authorised value from a `DriveTarget` tick input in drive-table order.
- **Rows and refusals:** rows carry the declared parameter's bounds; an out-of-range value is refused `RunControlOutOfRange` (20).
- **Committed-value checks:** `WorkshopRead` carries the committed drive read. Admission and every advance check the committed target.
- **UI:** a numeric run control (label, field, Set) refuses invalid input before sending and leaves the Battery toggle unchanged.
- **Saves and Reset:** both keep the authored speed.

**Files changed.** These are listed in the Implementation Record above:
- declaration (`ParameterDescriptor`, `RunControlBindings`, `BundleValidator`, `MotorElementDeclaration`);
- engine (`PhysicsGpuAbi.Constraints`, `SupplyRoute`, `WorkshopPhysicsEngine`, `WorkshopGpuDevice`, `RevoluteConstraint` drive read, `WorkshopRunControl`, `WorkshopSceneCompiler`, `WorkshopCompiledScene`, `WorkshopSimulation`, `WorkshopRead`);
- wire (`workshop_wire.fbs`, regenerated C# and TS, codecs, TS mirrors and field lists);
- UI (`WorkshopConfiguration`);
- tests, and docs (CAT-042, isolation, TODO).

**Review findings.**
- **First pass (18):**
  - Patched 6 medium and 1 low:
    - the rebuilt field reseeding the old speed (two layers reported it);
    - an undriven-hinge-first engine mapping test;
    - read equality;
    - tick-input refusals;
    - the admission and per-tick drive checks;
    - the status subject looked up by row id.
  - Rejected 5 low and 6 false; every reason is in the Review Triage Log.
- **Follow-up pass on the fix delta (2):** all six fixes judged Pass. Patched 2 low test findings (a vacuous assertion, and a ±128 boundary acceptance).
- **Nothing deferred.**

**Follow-up review.** Recommended `true`: the first pass patched six medium entries. The named risk was that the fixes might reintroduce a stale value in the UI or mis-map drive indices. An independent re-review of the fix delta ran and passed all six, and its two low items were patched. Patched counts by verdict, first pass: high 0, medium 6, low 1.

**Verification.**
- **Final full local round** on the reviewed staged tree (`/private/tmp/claude-501/-Users-aidan-dev-personal-tim/5b49c1ca-9721-4a5d-8c03-bdf3a5db8b62/scratchpad/coord-round-drivetarget-3.log`):
  - builds: 0 warnings, 0 errors in all three;
  - tests: WorkerAotGuard 17, Coverage 77, BundleElements 236, E2eRunner 192 (node harnesses included), CuriousContraptions.tests 5,056;
  - total **5,578 passed, 0 failed, 0 skipped**.
  - This supersedes the implementer's pre-review 5,563 evidence above.
- **Pre-commit hook** (unmodified, on the staged candidate; `/private/tmp/claude-501/-Users-aidan-dev-personal-tim/5b49c1ca-9721-4a5d-8c03-bdf3a5db8b62/scratchpad/drivetarget-hook-final.log`): exit 0, 127/0/0, Anvil clean.
- **Engine names and matrix:** no new engine identifier contains "motor", and every matrix row has a covering test that ran and passed.
- **Not run:** browser E2E (deferred).

**Residual risks.**
- Browser qualification of the speed control is pending until the post-catalogue phase. That phase also projects the committed drive target into the browser observation.
- No shipped run control uses the `Clamp` policy.
- A future part that binds a run-controlled parameter to a second compiled field would leave that field at its authored value. 11.1b Conveyor must check this before declaring a run-controlled speed.
- Process disclosures: the implementer recorded that some first-round writes were validated by preview or partial patch, and one was written before its check. The final staged snapshot's hook Anvil scan is clean.

## Publication receipt — 4 November 2026

- **Commit:** `66da1ae76afceb74089b36f054c175dafe7fcaea`, pushed as a fast-forward of `origin/main` from `015e902d09950d10a333c61c541c6b579993938b`. The coordinator confirmed afterwards that remote `main` equals that SHA.
- **Approval:** the five-layer Auto review plus one independent re-review of the fix delta, Pass; the advisor's pre-commit audit items (the Forward-Only triage row split, the staged-path audit) were done before commit.
- **Tests:** the full local round passed 5,578/0/0 (`Skipped: 0`) on the committed code and tests; only spec record text changed after it. The commit's normal hook passed 127/0/0, Anvil clean.
- **Not verified:** CI build, Pages artifact and deployment were not monitored, per the owner instruction of 29 October 2026. Browser qualification is pending until the post-catalogue phase; the [run-control observation](deferred-work.md#run-control-observation) is queued for it.
