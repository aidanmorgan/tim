---
title: 'engine-run-control-table — one typed run-control path; Battery enable becomes its first row'
type: 'refactor'
created: '2026-11-04'
status: 'done'
baseline_revision: 78ff003caeae80ff33f00b5e4ada4b0bf40e7316
review_loop_iteration: 0
followup_review_recommended: true
context:
  - '{project-root}/docs/architecture/element-package-isolation.md'
  - '{project-root}/docs/testing-guidance.md'
warnings: ['oversized']
deferred:
  - summary: >-
      The worker admits a compiled run-control row for a locked owner or for a lane the owner's package does not declare.
    evidence: |-
      WorkshopCompiledScene.Validate checks that each row resolves to a slot the owner owns, but the scene carries no lock or part data, so it cannot refuse a row the browser compiler would never emit. The same trust boundary existed at baseline: EditableSources was validated only for ownership of a source. Pre-existing, not caused by this slice.
    location: >-
      engine/gpu/WorkshopCompiledScene.cs:Validate
    severity: low
---

<intent-contract>

## Intent

**Problem:** The owner decided on 3 November 2026 that the Motor's `speed` is adjustable during Run (CAT-042 open question 5). The only in-run control today is Battery Enabled, and it is Battery-shaped at every layer:
- the validator admits `RunControl` only on a Source store's enable;
- the compiled scene has `EditableSources`;
- the command is `ConfigureElectrical` with `WorkshopElectricalControl`;
- the simulation keeps an owner → `ElectricalEnable` table;
- the UI pins `ApplyRunControl(GpuBodyId, ElectricalEnable)`.

Adding speed beside it would be a second path for the same behaviour, "a declared parameter edited during Run", which the forward-only gate forbids.

**Approach:** Replace that path with one generic, typed run-control path, with no behaviour change.
- **Declaration:** `ParameterRunEdit.RunControl` stays the only declaration. The validator admits it on a parameter bound to exactly one run-controllable lane.
- **Lane kinds:** a closed `RunControlLane` enum, whose only member in this slice is `SourceEnable`.
- **Compiled table:** Run compiles a run-control table of rows `(RunControlId, Owner, Lane, LaneIndex)`.
- **Command:** one `ConfigureRunControl` command carries `(Owner, RunControlId, typed value)`.
- **Committed read:** a generic per-row read is built from the committed lanes.

Battery Enabled becomes the first row of this table. The follow-up slice `engine-revolute-drive-target` adds the `RevoluteDriveTarget` lane, the Motor `speed` run control, and the numeric UI control.

## Boundaries & Constraints

**Always:**
- **Forward-only (isolation rule 9):** delete everything the new path supersedes in this slice: `ConfigureElectrical` (C# and wire), `WorkshopElectricalControl`, `ElectricalControl` (fbs), `EditableSources` / `editable_sources`, `DeclaredPhysics.HasRunControlledSource`, and the single-key rule at `BundleValidator.cs:106-110`. Replace the `ApplyRunControl` pins in `.editorconfig` and `CSharpStaticGateTests`. Replace the guard; do not drop it.
- **Closed sets are enums end-to-end:** `RunControlLane` on the wire, in C#, in TS mirrors and in tests. The value is typed: an enable is an `ElectricalEnable`, never an f32 0/1. The value's kind must match the row's lane, or the control is refused with a typed reason.
- **Typed refusals, in two layers:**
  - **Decode:** a typed `WireFaultException` for an undefined lane, value kind or enable value. This also replaces today's plain `ArgumentException` from `WorkshopElectricalControl.Validate`.
  - **Simulation:** a typed `WorkshopRejection` for an unknown control id, an owner that does not own that id, a value kind that does not match the lane, `Busy` and `WrongPhase`.
- **Lifecycle, copied from the electrical rules as they are today:**
  - a change applies on the next committed step;
  - while Paused it is held, and a second change replaces the first;
  - a committed Reset clears it, and a cancelled Reset keeps it;
  - Construct, Configure, and Run from Building clear it;
  - a discarded tick keeps the pending control (the controls table persists across a discard; `WorkshopSimulation.cs:88-90`);
  - the control never edits `WorkshopConstruction`, so a save stores the authored value.
- **Generic, data-driven code:** no element name in any engine identifier (the `GenericIdentifiers` gate, `WorkshopPhysicsLawTests.cs:738-748`). The UI finds its run-control rows from the declaration and compiled table, with no per-lane UI branch beyond rendering the typed value.
- f32; strict TypeScript for TS mirrors; regenerate the wire with the pinned flatc 25.2.10 per `schemas/README.md`; full local round `Skipped: 0`, including the node harness tests.

**Never:**
- add a speed, revolute or prismatic lane or bounds fields in this slice (that is `engine-revolute-drive-target`);
- change any player-visible behaviour, tick timing or physics;
- keep an alias, shim or old-name re-export of any deleted symbol;
- send an enum as a float;
- weaken or delete an existing run-control assertion (rename API calls only; record each changed test);
- run browser E2E.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|---|---|---|---|
| Enable during Run | Supplied Battery → Motor, Battery authored Disabled, Running; `ConfigureRunControl(battery, its id, Enabled)` | Next committed step: source enabled, Motor spins up, lamp lights, committed run-control read shows Enabled (`MotorTests.EnablingTheBatteryDuringRun…` unchanged) | No error expected |
| Paused hold and replace | Paused; two controls in turn | Held; the second replaces the first; applied on Resume or Step | No error expected |
| Reset and save | Control applied mid-run; committed Reset; save during Run | Authored value restored; table cleared; save holds the authored value; a cancelled Reset keeps the control | No error expected |
| Discarded tick | Control pending when a tick is discarded | Still pending; applied on the next committed step | No error expected |
| Phase | Building, or no Run | Refused `WrongPhase`; nothing changes | Typed rejection |
| Unknown or foreign id | Id not in the table, or owned by another instance; locked owner (no row) | Refused with a typed rejection; nothing changes | Typed rejection |
| Kind mismatch | Value kind does not match the row's lane | Refused with a typed rejection | Typed rejection |
| Undefined on the wire | Undefined lane, value kind or enable byte | `WireFaultException` with a typed reason; router state unchanged | Typed wire fault |
| Declaration | `RunControl` on a parameter with zero or several run-controllable bindings, or on a non-store-enable parameter | Bundle validation refuses (`OutOfRange`) | Typed validation error |
| Round trip | `ConfigureRunControl` command and compiled run-control table | Encode → decode preserves every field (TS mirror enum numbers updated) | No error expected |

</intent-contract>

## Code Map

The search map came from a read-only exploration of `78ff003c`.

- **Declaration**
  - `CuriousContraptions.Abstractions/ParameterDescriptor.cs:37-51`: `ParameterRunEdit { BuildOnly, RunControl }`. It stays as the only declaration.
  - `elements/Battery/BatteryElementDeclaration.cs:49,80-81`: `enabled` is `RunControl`, referenced by `Supply.Store.Enable`. No change.
- **Validator**
  - `CuriousContraptions.Bundle/BundleValidator.cs:106-110`: the single-key rule. Replace it with "bound to exactly one run-controllable lane", where the lane is resolved by a small generic resolver (today only a Source store's `Enable`).
  - `:327-330`: Choice and EditorPolicy checks. Keep.
  - Test: `tools/BundleElements.Tests/BundleValidatorTests.cs:426,484`.
- **Lookup:** `engine/gpu/DeclaredPhysics.cs:79-85`, `HasRunControlledSource`. Delete it, and replace it with a generic lookup of the part's run-control parameters and their lanes. Callers: `WorkshopSceneCompiler.cs:36`, `ui/WorkshopConfiguration.cs:21-24`, `ui/WorkshopGuidance.cs:330`.
- **Compiled scene**
  - `engine/gpu/WorkshopCompiledScene.cs:29,39,54-55`: `EditableSources`. Replace it with the run-control table, validated as follows: unique ids; each owner is an instance; each lane index resolves (for `SourceEnable`, the owner owns that electrical source).
  - Wire: `schemas/workshop_wire.fbs:725` (`editable_sources`) and `engine/gpu/WorkshopWireFlatBuffers.Compiled.cs:823,866,916`.
  - Test constructors: `MotorTests.cs:184`, `CompiledSceneBoundaryTests.cs:169,244,353`.
- **Command:**
  - `engine/gpu/WorkshopElectricalControl.cs`: delete it.
  - `engine/gpu/WorkshopMessages.cs:9,13`: `ConfigureElectrical = 11`, `WorkshopCommand.Electrical`.
  - `schemas/workshop_wire.fbs:29,48,190,265`.
  - Generated code: `engine/gpu/WorkshopWireGenerated.cs` and `tools/e2e/workshop-wire.ts`.
  - TS mirrors: `tools/e2e/run-reset-snapshot.ts:15,89,97`, `CuriousContraptions.web/wwwroot/workshop-observation.ts:37`, `tools/runtime/workshop-observation.test.ts:631` (asserts enum numbers).
  - Codec: `engine/gpu/WorkshopWireFlatBuffers.cs:101,125,155,168-172`.
- **Routing and client:**
  - `engine/gpu/WorkshopCommandRouter.cs:18-43`, `WireFaultException.cs:6`.
  - `engine/gpu/WorkshopCommandExecutor.cs:64`.
  - `CuriousContraptions.Simulation/Program.cs:169` (drains a pending GPU operation first; keep that for the new kind).
  - `engine/gpu/BrowserWorkshopClient.cs:156-185` (`ExpectedRevisionKind.Any`), `engine/gpu/IWorkshopClient.cs`, `engine/MachineWorld.Gpu.cs:136,162,174`.
- **Simulation:** `engine/gpu/WorkshopSimulation.cs`.
  - `:12-14`: rejections; add typed members as needed.
  - `:88-103`: the controls table and refusals.
  - `:221,223,269,302,375`: clears.
  - `:330`: the tick controls.
  - `:575-582`: `ValidateAdvancedRead`, which checks per row against the committed lanes.
  - `engine/gpu/SupplyRoute.cs:301-318`: `WorkshopTickControls.For`. It stays the place where a `SourceEnable` row's value reaches the engine.
- **Committed read:** today's UI reads `WorkshopRead.Electrical[i].Enabled` (`ui/Workshop.cs:804`). Add a generic per-row committed read, built from the committed lanes, that the UI and `ValidateAdvancedRead` use.
- **UI:**
  - `ui/WorkshopConfiguration.cs:21-24,75-160`: the toggle, queued state and `ApplyRunControlAsync`. Generalise it to run-control rows; it stays a toggle for `SourceEnable`.
  - `ui/WorkshopGuidance.cs:330`.
  - Static pins: `.editorconfig:70` and `CuriousContraptions.tests/CSharpStaticGateTests.cs:61-62,164,865`.
- **Other callers to update** (the grep list at the baseline): `CuriousContraptions.Core/RuntimeSources.props` and the test callers `BasketballResourceTests`, `DeclaredPrismaticDriveTests:417-476`, `ElectricalNand/Nor/Or/XorTests`, `EventRingProducerTests`, `LatchSupplyFixtureTests:72`, `MotorTests:361`, `PalettePlacementTests:1635`, `SourceStoreInstanceTests:153-255`, `WorkshopHintTests`, `WorkshopIntegrationClient`, `WorkshopIntegrationHarness:7,111,161,176`, `WorkshopPhysicsEngineTests:734`, `WorkshopSimulationTests:449` and `WorkshopWireFlatBuffersTests:104`.
- **Docs:** `docs/planning/elements/CAT-005-battery.md`, `docs/testing-guidance.md` (mentions of the old names), and `docs/architecture/element-package-isolation.md`, which gains one line naming the run-control table as the generic in-run parameter path.

## Tasks & Acceptance

**Execution:**
1. **Red first.** Add the new-path tests before the implementation:
   - the round trip of the new command and table;
   - decode refusals;
   - simulation refusals (unknown id, foreign owner, kind mismatch);
   - the validator's zero- or multi-binding refusal.
   Record their failures (compile failures count as red).
2. **Schema and generated code.** Update `schemas/workshop_wire.fbs` (`RunControlLane`, the typed value, `ConfigureRunControl`, the table record) and regenerate with the pinned flatc. Generate the output into `$TMPDIR`, then Write it into the tree with the Write tool: no `cp`, no redirects. Update the TS mirrors.
3. **C# path.** In declaration lookup, validator, compiled scene, codec, router, executor, client, simulation, committed read and UI: replace the old pieces and delete the superseded symbols. Replace the static pins.
4. **Existing tests.** Update every existing run-control test to the new API with its assertions unchanged, and list the tests touched in the spec record.
5. **Docs.** Update CAT-005, testing-guidance, the isolation doc line and TODO.md. In TODO.md, the current slice is this one, then `engine-revolute-drive-target`, then 11.1b.
6. **Verify.** One full local round, then the unmodified pre-commit hook. Stage; do not commit.

**Acceptance Criteria:**
- Given the matrix, when each row runs through the real encode → router → executor → simulation → engine path (`WorkshopIntegrationHarness`), then each outcome holds.
- Given a grep for `ConfigureElectrical`, `WorkshopElectricalControl`, `EditableSources`, `editable_sources`, `HasRunControlledSource` and `ElectricalControl` in source, schema, generated code, TS and tests, then nothing remains except historical records under `_bmad-output`.
- Given every existing run-control test, then each passes with its assertions unchanged.

**Process rules for the implementer:**
- Write only in the worktree and `$TMPDIR`.
- Use Edit or Write for every repository file, and run Anvil before each write.
- Put timeouts on builds and tests.
- Stop and report rather than redesign.
- Stage the result; do not commit.

## Spec Change Log

- **Split (coordinator with advisor, 4 November 2026).** The invocation intent, `engine-revolute-drive-target`, is split. A second command beside `ConfigureElectrical` would be a second path for the same behaviour, so it was rejected. Under rule 9, the single generic path would otherwise put the refactor and the new lane in one diff of about 35 files. This slice is the behaviour-preserving refactor, and the speed lane follows as its own slice. The branch was renamed from `engine-revolute-drive-target` to `engine-run-control-table` before any change; the worktree directory keeps its name.

- **Implementation decisions (implementer, 4 November 2026).** Recorded before the red tests; none changes behaviour.
  - **One resolver, one lane enum.** `RunControlLane { SourceEnable = 1 }` is in `CuriousContraptions.Abstractions` (beside `ParameterRunEdit`), so the validator, Core and main share it. `CuriousContraptions.Bundle/RunControlBindings.Lanes(entry, key)` is the one resolver; the validator refuses a RunControl parameter with `Lanes(...).Count != 1`, and `DeclaredPhysics.RunControls` and the compiler read the same resolver.
  - **Kind mismatch is reachable.** With one lane and one value kind the mismatch row could not be built, so `RunControlValueKind` is `{ Enable = 1, Scalar = 2 }` (an f32 scalar that no lane takes yet). It is a value kind, not a lane or bounds; the follow-up speed lane will take it. A value's unused field must be zero; the decoder refuses a non-canonical or non-finite value.
  - **Several bindings** cannot be constructed until a second lane exists (one store per part). The `Count != 1` rule covers it; its test arrives with the second lane.
  - **Wire layout.** flatc 25.2.10 refuses an enum-typed struct field whose enum has no zero member, and the repository's closed wire enums start at 1 (zero undefined). `RunControlRecord.lane` and `RunControlCommandRecord.kind` therefore carry their schema enums (`RunControlLane`, `RunControlValueKind`, both declared in the schema) as `uint8`, like `revision_kind` carries `ExpectedRevisionKind`; the codecs cast and refuse undefined values. `enable` is the schema's `ElectricalEnable`. `run_control` replaces `electrical` in the same `WorkshopCommand` slot and `run_controls` replaces `editable_sources` in the same `CompiledSceneRecord` slot.
  - **Command and rejection numbers.** `CommandKind` 11 (`ConfigureElectrical`) is retired and undefined (it refuses as `UnknownCommand`), following the precedent of rejection 6; `ConfigureRunControl = 14`. The C# and TS enums now have explicit values. New rejections: `UnknownRunControl = 17`, `ForeignRunControl = 18`, `RunControlKindMismatch = 19`. Refusal precedence stays Busy, then WrongPhase, then id, owner, kind.
  - **Row identity.** Rows are numbered from 1 in instance order, then declared parameter order. A row's lane index is its slot in the lane's compiled table (for `SourceEnable`, the owner's source in source order). The compiled table refuses id 0, duplicate ids, a foreign or absent owner, an undefined lane, two rows on one lane slot, and a slot that does not exist or that another owner owns.
  - **Committed read (revised after review, 4 November 2026).** `WorkshopRead.RunControl(owner, lane)` scans the lane's committed slots for the owner and refuses a read without one; there is no row-based read. `ValidateAdvancedRead` keeps only its every-source enable check (the per-row repeat was deleted). The UI has one display rule for every owner, including a locked Battery (which still compiles a source): the committed lane value read by (owner, lane), or a queued value of the owner's row. The UI finds rows through `IWorkshopClient.RunControls` by owner and lane; a part without a row (locked) gets a disabled button with no action. `DeclaredPhysics.Authored` and the compiler's shared enable helper are deleted. Test fixtures carry `(GpuBodyId Owner, RunControlValue Value)`; the harness helper is `RunControl(owner, lane, value)`.
  - **Discarded tick is proven on the controlled device.** The shared harness's real device completes each Advance synchronously, so it cannot interleave a superseded tick. The matrix row therefore runs in `WorkshopSimulationTests.ADiscardedTickKeepsTheHeldControlForTheNextCommittedStep` on the controlled device (the same seam as the existing cancelled-Reset test): the production `WorkshopSimulation` holds the control across the superseded tick and hands it to the next committed step. Every other matrix row runs through `WorkshopIntegrationHarness` or the one codec.
- **Tests touched (assertions unchanged unless stated).**
  - API renames only: `MotorTests` (2 controls and the scene rebuild), `DeclaredPrismaticDriveTests` (4 controls; the reference loop passes no held controls), `EventRingProducerTests` (2 controls; 2 device candidates pass no held controls, as before, since none was configured), `ElectricalNand/Nor/Or/XorTests` (row type becomes `(GpuBodyId, ElectricalEnable)` tuples; literals unchanged), `LatchSupplyFixtureTests`, `WorkshopIntegrationHarness` (`RunControl(owner, enable)` resolves the installed row), `WorkshopIntegrationClient`, `BasketballResourceTests` and `WorkshopHintTests` fakes, `WorkshopGpuDeviceTests`, `WorkshopWireFlatBuffersTests` (`Command_ConfigureRunControl_RoundTrips`; `RunControl` absent on Run), `WorkshopFormatTests` (field list), `CompiledSceneBoundaryTests` (3 constructors), `WorkshopPhysicsEngineTests` (comments), `CSharpStaticGateTests` and `.editorconfig` (the `ApplyRunControl(WorkshopRunControl)` pins).
  - `PalettePlacementTests.TheRunControlToggle…`: the fake client serves the shared compiler's table, and its reads now carry the admitted sources' committed enables (as every installed read does); the four recorded controls carry that row id and typed values.
  - `SourceStoreInstanceTests`: the declaration test asserts the declared control list (`[enabled → SourceEnable]` or empty) instead of a boolean; the post-Reset check asserts no held control (was the trivially true `ElectricalState(Source, Enabled) == Enabled`). **Changed expectation:** the foreign-owner test sends the battery's real row id with each foreign owner and expects the typed `ForeignRunControl`, where it expected the catch-all `InvalidConstruction`; the spec requires the typed reason.
  - `WorkshopSimulationTests.ACommittedResetClears…`: `HeldRunControl(row)` replaces `ElectricalState(owner, Disabled)`; the final check is "no control held".
  - `tools/runtime/workshop-observation.test.ts`: adds `ConfigureRunControl = 14`, `[11]` undefined and rejections 17-19.
  - New: `RunControlTableTests` (compiled table, table refusals, round trip, undefined lane, undefined bytes and kind 11 through the router, unknown id and kind mismatch, committed read), `WorkshopSimulationTests.ADiscardedTickKeepsTheHeldControlForTheNextCommittedStep`, `BundleValidatorTests.ARunControlParameterBindsExactlyOneRunControllableLane`.

## Review Triage Log

### 2026-11-04 — Review pass
- verdicts: 23 findings — high 2, medium 4, low 10, false 7, maybe-false 0
- findings:
  - `[medium]` `[patch]` (Verification Gap) `BrowserWorkshopClient.RunControls`, the table the production UI uses, had no native test — added `WorkshopActivationAnimationTests.TheClientRunControlTableIsTheInstalledTableAndItsRowDrivesTheNextCommittedEnable` through the real client (table equals the installed table; a control from its row is Applied and flips the next committed enable).
  - `[medium]` `[patch]` (Verification Gap; grouped with the Blind Hunter zero-id row) The decoder's zero-owner/zero-id refusal and the encoder's non-canonical refusal were untested — owner-0 and id-0 rows added to the router fault table (`OutOfRange`); `WorkshopWireFlatBuffersTests.Command_ConfigureRunControl_RefusesANonCanonicalControlAtTheEncoder` added.
  - `[low]` `[patch]` (Intent Alignment) The matrix's enable-during-Run row expects the committed run-control read to reach Enabled for an authored-Disabled Battery; only Enabled→Disabled was asserted — `TheCommittedRunControlReadFollowsTheCommittedLane` is now a theory covering both directions.
  - `[low]` `[patch]` (Intent Alignment) No test refused `ConfigureRunControl` with `Busy` — added `WorkshopSimulationTests.AControlWhileAnOperationIsPendingIsRefusedBusy` (Rejected/Busy, nothing held).
  - `[low]` `[patch]` (Intent Alignment; grouped with the Blind Hunter Step row) A paused control applied by Step was not asserted — added `SourceStoreInstanceTests.APausedControlIsAppliedByTheNextStep`.
  - `[false]` `[reject]` (Intent Alignment) Construct, Configure and Run from Building clearing held controls have no direct assertion — each runs only from Building (`WorkshopSimulation.cs:285` for Configure), where `ConfigureRunControl` is refused `WrongPhase` and a committed Reset has already cleared the table, so no control can be held there; the clear is unobservable.
  - `[low]` `[reject]` (Intent Alignment) Only Building is asserted for `WrongPhase`, not Completed — one phase rule (`Phase is not (Running or Paused)`); reaching Completed needs a goal capture, which is more than a direct addition for a negligible gap.
  - `[low]` `[patch]` (Intent Alignment) An `enabled` RunControl on a part with no store was refused only through the resolver — `ARunControlParameterBindsExactlyOneRunControllableLane` now asserts the `BundleValidator` `OutOfRange` error.
  - `[false]` `[reject]` (Intent Alignment) `RunControlLane` is absent from the observation TS mirror and the generated TS wire is not executed — the observation mirror carries only enums the observation reports, and no run-control lane is observed; `tools/e2e/workshop-wire.ts` is browser toolchain deferred until post-catalogue E2E and was regenerated byte-identical to pinned flatc.
  - `[false]` `[reject]` (Intent Alignment) The `PalettePlacementTests` fake now serves electrical reads because the new UI throws without a slot — the real worker's every read carries each compiled source (`PhysicsGpuAbi.ReadElectrical`), so the fake now matches the production read; the loud throw is on a path the UI never reaches (Run and Reset clear the buttons before their command; Construct is Building-only).
  - `[false]` `[reject]` (Intent Alignment) The UI branches on value kind — the intent allows "rendering the typed value"; there is no per-lane branch.
  - `[false]` `[reject]` (Intent Alignment) An existing assertion's expected reason changed from `InvalidConstruction` to `ForeignRunControl` — the intent's typed-refusal clause requires a typed reason for a foreign owner; the assertion is narrower, not weaker, and the change is recorded in the test and the Spec Change Log.
  - `[high]` `[patch]` (Forward-Only) The UI showed a locked part's authored value (`DeclaredPhysics.Authored`) while row-owning parts showed their committed lane — a second display rule chosen by a condition, plus a silent return in the click handler — now one rule: `WorkshopRead.RunControl(owner, lane)` reads the committed slot for every owner (a locked Battery still compiles a source); `Authored` deleted, `DeclaredEnable` re-inlined, the click action attached only when a row exists; the locked toggle stays shown, disabled, with its committed state.
  - `[high]` `[patch]` (Forward-Only) Test fixtures kept the deleted `WorkshopElectricalControl` shape as an `(Owner, ElectricalEnable)` tuple, adapted by a SourceEnable-only harness helper — fixtures now carry `(GpuBodyId Owner, RunControlValue Value)`; the helper is lane-generic `RunControl(owner, lane, value)` over `RunControlLanes.Find`; no assertion changed.
  - `[medium]` `[patch]` (Blind Hunter) The per-held-row loop in `ValidateAdvancedRead` repeated the enable check for the only lane — deleted.
  - `[medium]` `[patch]` (Blind Hunter) Zero owner/id decoder refusal untested — grouped with the Verification Gap row above; same fix.
  - `[low]` `[patch]` (Blind Hunter) Paused control applied by Step untested — grouped with the Intent Alignment row above; same fix.
  - `[low]` `[patch]` (Blind Hunter) The undefined-lane table fault was tested only at the codec — the patched Construct now goes through `Router.Dispatch` (typed `OutOfRange`, nothing installed) and the same valid Construct is then applied.
  - `[false]` `[reject]` (Edge Case Hunter) The UI's committed read throws every frame when the read lacks the row's slot — every read of an installation carries each compiled source; Run and Reset clear the buttons before their command and Construct is Building-only, so the throw is a loud failure on an unreachable path, not a defect.
  - `[false]` `[reject]` (Edge Case Hunter) `WorkshopTickControls.For` indexes a held row's slot unguarded — held rows come only from `Scene.TryRunControl`, whose table `WorkshopCompiledScene.Validate` admits only when each index resolves to a source its owner owns; the committed read's sources are in compiled source order.
  - `[low]` `[defer]` (Edge Case Hunter) The worker admits a compiled row for a locked owner or an undeclared lane — the scene carries no lock or part data; the same trust boundary existed with `EditableSources`; pre-existing, recorded in `deferred`.
  - `[low]` `[reject]` (Edge Case Hunter, claim) The spec's evidence says existing run-control assertions are unchanged, yet one expected reason changed — the fix is a spec edit; the change itself is recorded in the Spec Change Log.
  - `[low]` `[reject]` (Edge Case Hunter, claim) The several-bindings refusal (`Lanes(...).Count != 1`) has no test — not constructible while only `SourceEnable` exists; the `engine-revolute-drive-target` slice adds the second lane and carries this test in its plan.

### 2026-11-04 — Follow-up review pass (fix delta)
- verdicts: 4 findings — high 0, medium 0, low 3, false 1, maybe-false 0 (all ten first-pass fixes judged Pass)
- findings:
  - `[low]` `[patch]` Tests found a run-control row two ways (`Single(...ValueKind...)` in `ObservePhases` and `LatchSupplyFixtureTests`, the latter skipping `RunControlLanes.Find`) — one shared harness helper `WorkshopIntegrationHarness.RunControl(scene, owner, value)` picks the lane by value kind and resolves through `RunControlLanes.Find`; both call sites use it; no assertion changed.
  - `[low]` `[reject]` The Busy test's final `device.Controls[0]` assertion cannot catch a missing Busy guard (the fake records controls when Advance is called) — redundant, not a gap: `Assert.Null(HeldRunControl(row.Id))` fails without the guard.
  - `[low]` `[reject]` No test emits `Pressed` on the locked toggle and asserts nothing is sent — negligible: the button has no action attached (`WorkshopConfiguration.cs` row ternary) and is `Disabled`; asserting through Godot's `EmitSignal`, which ignores `Disabled`, adds a test of the button helper rather than the run-control rule.
  - `[false]` `[reject]` The click closure captures the row id from when the view was built — the panel rebuilds after every GPU operation and applied control, row ids are compiled 1..n per installation, and a stale id would receive a typed `UnknownRunControl` refusal, not a wrong control.

## Design Notes

The controls dictionary persists across a discarded tick and is cleared only at the listed lifecycle points. The new table keeps exactly that. A row's identity is a compiled `RunControlId`, so a part with several run-controllable parameters in later slices needs no string key on the wire. The UI maps rows to parameters through the declaration, using owner and lane.

**Planned for the follow-up slice, so this design must leave room:** the revolute target will get a committed lane in the revolute record's padding (bytes 164-175, outside the immutable [0, 112) guarded by `ValidateRevoluteCandidate`), set to the declared value at admission. Its rows will add min/max bounds. Do not add either here.

## Verification

**Commands:**
- Prepend pinned Node v26.12.1 (`/private/tmp/claude-501/-Users-aidan-dev-personal-tim/5b49c1ca-9721-4a5d-8c03-bdf3a5db8b62/scratchpad/node-26.12.1-dl/node-v26.12.1-darwin-arm64/bin`) to `PATH`. Run the full block in `docs/delivery-workflow.md#local-test-authority`. Expected: 0 warnings, 0 errors, every project `Skipped: 0`, 0 failed, node harness tests included.
- Run `.githooks/pre-commit` unmodified on the staged candidate. Expected: exit 0, Anvil clean.

**Implementation evidence (implementer, 4 November 2026; staged on `engine-run-control-table` over baseline `78ff003c`, not committed).** Logs are in `/private/tmp/claude-501/-Users-aidan-dev-personal-tim/5b49c1ca-9721-4a5d-8c03-bdf3a5db8b62/scratchpad/runctl/`.
- **Red first:** before any implementation, the new tests failed to compile: TS2339 for `WorkshopCommandKind.ConfigureRunControl` and rejections 17-19 (`red-tests.log`); CS0246/CS1061 for `WorkshopRunControl`, `RunControls`, `RunControlValue`, `ConfigureRunControl` and `HeldRunControl` (`red-tests-cs.log`); CS0103/CS0234 for `RunControlLane` and `RunControlBindings` (`red-bundle-cs.log`).
- **Generated code:** pinned flatc 25.2.10 per `schemas/README.md`. The pipeline first reproduced the baseline C# and TS output byte for byte; the regenerated scratch output then matched `engine/gpu/WorkshopWireGenerated.cs` and the generated part of `tools/e2e/workshop-wire.ts` exactly (`cmp`). The hand-written mock encoder, `SNAPSHOT_SCHEMA` and the `WorkshopFormatTests` field list were updated by hand.
- **Deleted symbols:** `rg 'ConfigureElectrical|WorkshopElectricalControl|EditableSources|editable_sources|editableSources|HasRunControlledSource|ElectricalControl\b|ElectricalState\('` outside `_bmad-output` returns nothing.
- **Full local round** on the staged candidate (`full-round-2.log`): three compiler logs 0 warnings, 0 errors; WorkerAotGuard 17, Coverage 77, BundleElements 235, E2eRunner 192 (includes the node harnesses through `NodeHarnessTests`), CuriousContraptions.tests 5,012 (includes `CSharpStaticGateTests` with the binlogs): **5,533 passed, 0 failed, 0 skipped**. The browser suites, including `tools/e2e/unit/*`, stay in the generated deferral filter and were not run.
- **Normal hook** (`hook-1.log`): `.githooks/pre-commit` unmodified, exit 0; scoped tests 127/0/0; Anvil secret and anti-pattern scan 0 findings.
- **Remaining for review:** step-04 review layers, including the Forward-Only Auditor. Browser qualification is pending until the post-catalogue E2E phase.
- **Superseded:** the round, hook and "remaining" lines above are the pre-review implementation evidence. The final candidate's evidence (5,538/0/0 on `coord-round-runcontrol-4.log`, hook `runcontrol-hook-final.log`, review complete) is in the Auto Run Result below.

## Auto Run Result

Status: done (4 November 2026). No blocking condition.

**Summary.** The Battery-only in-run control is replaced by one generic typed run-control path, with no player-visible change. `ParameterRunEdit.RunControl` remains the only declaration. The validator now admits it on a parameter bound to exactly one run-controllable lane, through one resolver, `RunControlBindings`. Run compiles a run-control table of rows `(RunControlId, Owner, RunControlLane, LaneIndex)`; Battery Enabled is its first row, on lane `SourceEnable`. One `ConfigureRunControl(Owner, RunControlId, RunControlValue)` command replaces `ConfigureElectrical`. The value is typed: an `ElectricalEnable`, or an f32 scalar that no lane takes yet. Every refusal has a named reason: `WireFaultException` at decode, and `UnknownRunControl`, `ForeignRunControl`, `RunControlKindMismatch`, `Busy` and `WrongPhase` in the simulation. The lifecycle is copied exactly from the old electrical path. The UI shows every declared control from the committed lane, read by owner and lane, and sends through the compiled row; a locked part has no row and shows a disabled control.

**Files changed.**
- **Declaration and validation:**
  - `ParameterDescriptor.cs`: `RunControlLane`.
  - `RunControlBindings.cs` (new): the one lane resolver.
  - `BundleValidator.cs`: the exactly-one-lane rule.
- **Engine:**
  - `WorkshopRunControl.cs` (new): id, value, row, command, lane rules and the committed read.
  - `DeclaredPhysics.cs`: `RunControls`.
  - `WorkshopSceneCompiler.cs`: compiles the table.
  - `WorkshopCompiledScene.cs`: the table and its validation.
  - `WorkshopSimulation.cs`: held controls by row, plus the new rejections.
  - `SupplyRoute.cs`: tick controls from held rows.
  - `WorkshopMessages.cs`, `WorkshopCommandExecutor.cs`, `IWorkshopClient.cs`, `BrowserWorkshopClient.cs`, `MachineWorld.Gpu.cs`, `Program.cs`: the command kind and the installed table.
  - Deleted: `WorkshopElectricalControl.cs`.
- **Wire:**
  - `workshop_wire.fbs`, `WorkshopWireGenerated.cs` and `tools/e2e/workshop-wire.ts`, all regenerated with pinned flatc 25.2.10.
  - `WorkshopWireFlatBuffers(.Compiled).cs`: codecs and typed faults.
  - `workshop-observation.ts` and `run-reset-snapshot.ts`: TS mirrors.
- **UI:**
  - `WorkshopConfiguration.cs`: run-control views from the declaration and table.
  - `WorkshopConnections.cs` and `Workshop.cs`: an inert button for a control with no row.
  - `WorkshopGuidance.cs`: visibility.
  - `.editorconfig` and `CSharpStaticGateTests.cs`: the `ApplyRunControl(WorkshopRunControl)` pins.
- **Tests:**
  - New: `RunControlTableTests.cs`, plus tests in `WorkshopSimulationTests`, `SourceStoreInstanceTests`, `WorkshopActivationAnimationTests`, `WorkshopWireFlatBuffersTests`, `BundleValidatorTests` and the node observation test.
  - The other changed tests changed only their API calls and fixture vocabulary. One expected reason became typed: `InvalidConstruction` → `ForeignRunControl`.
- **Docs:** CAT-005, `testing-guidance.md`, `element-package-isolation.md`, `TODO.md`.

**Review findings (one pass, 23 findings).**
- **Patched:** 2 high (both Forward-Only), 4 medium and 6 low:
  - high: the second display rule for a locked part, and the old-shape test fixtures;
  - medium: the repeated read check; the browser-client table test; the zero-id decoder and encoder refusals (two layers reported these);
  - low: the committed read in both directions; Busy; Step while Paused (two layers); the undefined lane through the router; the store-less validator case.
- **Deferred:** 1 low: compiled rows for locked owners rest on the same trust boundary as `EditableSources`.
- **Rejected:**
  - 7 false: Construct/Configure/Run-from-Building clears that can't be observed; the TS observation mirror; the fake electrical reads; the value-kind branch; the narrowed expected reason; the UI read throw; the tick-control index.
  - 3 low: the Completed phase (one phase rule; not a direct fix); the spec's evidence claim (its fix is a spec edit); several bindings (can't be built until the second lane exists; the `engine-revolute-drive-target` plan carries that test).
- Every row's reason is in the Review Triage Log.

**Follow-up review.** Recommended `true`: two high entries were patched on a first pass. The named risk was that the Forward-Only fixes might reintroduce a second rule or change what the player sees. An independent re-review of the fix delta ran and passed all ten fixes, with no blocking findings. Its one low item (a duplicated test row lookup) was patched into one shared harness helper. Patched counts by verdict: high 2, medium 4, low 6.

**Verification.**
- **Full local round on the final staged tree** (`/private/tmp/claude-501/-Users-aidan-dev-personal-tim/5b49c1ca-9721-4a5d-8c03-bdf3a5db8b62/scratchpad/coord-round-runcontrol-4.log`):
  - three builds: 0 warnings, 0 errors;
  - tests: WorkerAotGuard 17, Coverage 77, BundleElements 235, E2eRunner 192 (includes the node harnesses), CuriousContraptions.tests 5,017;
  - total **5,538 passed, 0 failed, 0 skipped**.
- **Pre-commit hook** (`.githooks/pre-commit`, unmodified, on the staged candidate; `/private/tmp/claude-501/-Users-aidan-dev-personal-tim/5b49c1ca-9721-4a5d-8c03-bdf3a5db8b62/scratchpad/runcontrol-hook-final.log`): exit 0, scoped tests 127/0/0, Anvil scan clean.
- **Deleted symbols:** a grep outside `_bmad-output` finds none.
- **Matrix audit:** every row has a covering test that ran and passed.
- **Not run:** browser E2E, deferred until the post-catalogue phase.

**Residual risks.**
- Browser qualification of the CAT-005 toggle is pending until the post-catalogue E2E phase.
- `RunControlValueKind.Scalar` exists before any lane takes it; the next slice's speed lane is its first user.
- `WorkshopRunControl.cs.uid` keeps the deleted file's Godot UID. Nothing references it.
- Coordinator disclosure: one triage-log line was written before its own Anvil call. It was validated straight after and its content is unchanged.

## Publication receipt — 4 November 2026

- **Commit:** `30fcfb56218342e4b94bea6cb86eff4c671dc6cb`, pushed as a fast-forward of `origin/main` from `78ff003caeae80ff33f00b5e4ada4b0bf40e7316`. The coordinator confirmed afterwards that remote `main` equals that SHA.
- **Approval:** the five-layer Auto review plus one independent re-review of the fix delta, Pass; the advisor's pre-commit audit items (follow-up triage entry, superseded evidence note) were fixed before commit.
- **Tests:** the full local round passed 5,538/0/0 (`Skipped: 0`) on the committed code and tests; only spec record text changed after it. The commit's normal hook passed 127/0/0, Anvil clean.
- **Not verified:** CI build, Pages artifact and deployment were not monitored, per the owner instruction of 29 October 2026. Browser qualification is pending until the post-catalogue phase.
