---
title: "9A.39 — Battery generic instance (CAT-005)"
status: review
baseline: f27cb71d89697c1ae46ca3a6653d3be8d638e89c
---

# Outcome and authority

Battery (CAT-005) becomes a `WorkshopGenericInstance`. Its finite store, its enable state and its run-time toggle come only from its package, through the generic compiler, the generic editor and `ConfigureElectrical`. `WorkshopBattery` and `ui/WorkshopBattery.cs` (156 lines), and every arm that names them, leave the engine. Roadmap position 34, after C-Gate.

**Authority:**
- epics 9A.39 ACs, including the 22 Oct pointer: the native guard before catalogue E2E is `LatchSupplyFixtureTests`;
- reconciliation item 4 (the `"battery"` arm and the Battery panel);
- CAT-005 §2, B2/B6/B8–B11 and Q4/Q5;
- EL-196 §2;
- spec-9-4c D3 (the one-tick lag must not move);
- the C-Bumper+Spring and C-Balls precedents.

Native real-simulation C# proof is PRIMARY. Browser proof is pending (21 Oct policy).

**Absent or drifted (re-derived on `f27cb71d` = C-Gate merged, 23 Oct 2026, implementation owner):**
- `BrowserWorkshopClient.Electrical.cs` does not exist.
- The guidance rule is now at `WorkshopGuidance.cs:311`.
- C-Gate D5 already removed the warm-up battery: `Program.WarmupSimulation` and `WorkshopPhysicsEngine.Warmup` name no battery (the 6 Warmup Known rows went with C-Gate). There is nothing left to delete there.
- C-Gate interactions: `PhysicsSceneDeclaration.PrismaticDrives`, `SupplyRoute`'s driven-consumer binding and the `ActuatorDriveRecord.moving_contact` wire field touch the battery only as the Source node of `SupplyTopology`. Neither is changed here. The gate's drive debits nothing from the source (decision 1).
- No admitted lesson places a battery. The 28 content templates carry `Capacity`, `MaximumPower` and `InitialFraction` scalars and `enabled`, all 0: 112 key removals. The `kind: battery` rows are Planned only.
- `SupplyTopology` is already declaration-driven.
- The `"battery"` `ElementLiteral`/`PackageIdentifier` rows: 67 Battery-owned Known rows at `f27cb71d` (unchanged from `a598a09e`). The expected change is Known 301 → 234.

# Finite design decisions

**D1 Declared store.** `SupplyCapabilityDescriptor` gains `Store: SupplySourceStoreDescriptor(Capacity, PowerLimit, InitialCharge, Enable)` parameter keys. It is required exactly when `Kind == Source`. The Battery package declares these parameters, keyed as in EL-196, with the owner defaults of 10 Oct:

| Key | Kind and unit | Range | Default | Editor |
|---|---|---|---|---|
| `capacity` | Real, Joule | 60–14400 | 3600 | Clamp |
| `power_limit` | Real, Watt | 10–480 | 120 | Clamp |
| `initial_charge` | Real, None | 0–1 | 1 | Clamp |
| `enabled` | Choice `[Disabled, Enabled]` | 0–1 | 1 | Reject |

- Clamp matches today's SpinBoxes.
- `enabled` stays a two-option typed Choice (CAT-005 B2), which satisfies the AC's "boolean". The `PackageRuntimeParityTests:330` pin is re-expressed against the declared default.
- Add `ParameterUnit.Joule` and `ParameterUnit.Watt`.
- Move the envelope (60/14400 J, 10/480 W) to `Abstractions.Electrical.ElectricalSourceEnvelope`, shared by `ElectricalSupply`, `ElectricalSourceDeclaration.Validate` and the validator.
- The validator returns a `BundleError`, never a throw, for:
  - a missing or extra store;
  - a key that is absent, duplicated, or of the wrong kind or unit;
  - a range outside the envelope;
  - an `enabled` that is not a two-option Choice.

**D2 Run-editable is declared.** Add `ParameterDescriptor.RunEdit: ParameterRunEdit { BuildOnly, RunControl }`, default `BuildOnly`, with schema and round trip.
- `RunControl` is valid only on a Source store's `Enable`.
- `ConfigureElectrical` admits an unlocked generic owner whose package declares a Source store. This replaces `is WorkshopBattery`.
- A toggle edits the runtime control only: no revision, no Undo. Reset restores the authored value, and Save during Run saves the authored value. This is today's behaviour.

**D3 Generic editor.**
- In Build, a `Choice` renders as an `OptionButton` of its options. This also applies to Receiver's `capture_participation`, which gets one native check and a browser re-run.
- In Running or Paused, each `RunControl` parameter gets one toggle that calls `World.ConfigureElectrical`. It keeps today's queued-intent semantics: epoch/tick, "(queued)", and a second paused click reverses.
- Visibility becomes: the selected part declares a `RunControl` parameter.
- The "Charge: X J" label is dropped. The declared four-mark `CommittedCharge` gauge (CAT-005 Q5) is the charge display.

**D4 Compile and presentation.** The compiler emits an `ElectricalSourceDeclaration` from the store parameters as f32. `capacity × initial_charge` is bit-equal to today. Presentation capacity reads the parameter. Delete:
- `ElectricalSourceSettings`, folding its checks into `ElectricalSourceDeclaration.Validate`, including the ABI call site;
- `IWorkshopElectricalSource`;
- `MachinePart.CanonicalSource` and `ApplySource`.

**D5 Wire and content.**
- Delete `capacity`, `maximum_power`, `initial_fraction` and `enabled` from `InstanceRecord`. Later slots shift by 4.
- Delete the three matching `AuthoredScalar` members and the template `enabled` field. That removes four zero-valued keys from each of the 28 templates in `content/puzzles.json` (112 lines).
- `ElectricalSourceRecord` and `ElectricalControl` are unchanged.
- Regenerate the wire with flatc 25.2.10. Update `run-reset-snapshot.ts:58`, `workshop-wire.ts`, and the `cat-005`/`cat-051-own-supply` accessors at type level.
- Every moved record (battery, domino, counter, and any generic carrying parameters) is refused with a typed reason. There is no migration. Re-derive the expected reasons in `PreConversionLaunchSaveTests` and `PayloadInstanceTests`.

**D6 Energy accounting is out of scope.** The gate drive debits no battery energy (spec-9-4c, line 202). A conversion preserves trajectories, so the zero debit is recorded only in the baseline (Open Question 1).

**Naming:** new identifiers use `SupplySource*`, `RunControl` and `ElectricalSource*`. The ratchet adds no rows.

# Ordered tasks

- [x] **Suite-first native baseline.** After C-Gate merges, use one Debug build of `main` and run `--no-build --no-restore`. Keep the raw receipts.
  - Suites: `LatchSupplyFixtureTests`, `SwitchElectrical`, `PoweredGate`, `BothGate`, NAND/NOR/OR/XOR, `ElectricalSupply`, `ContactWorkInstance` (recharge), `WorkshopSimulation` (ConfigureElectrical), `DeclaredPresentation` (marks), the latch, counter, plate and clock suites, `PackageRuntimeParity`, the save and wire suites, `ElementIsolation`.
  - Golden per-tick source traces (`Remaining`, `Debit`, `Enabled`, `Available`) and consumer trajectories.
  - The C-Balls 12×3 probe and the placed-solution probe.
- [x] **Capability (D1, D2):** contracts, schema, validator and round trip, with every refusal.
- [x] **Conversion (D3–D5):**
  - generic `_` arms in `CaptureInstance`, encode/decode and prevalidation;
  - the compiler loop, `ConfigureElectrical`, the editor, content and wire;
  - test callers move to `WorkshopInput.Generic(PartId.Battery, …)` (about 28 files);
  - re-audit unfiltered `OfType<WorkshopGenericInstance>()`.
- [x] **Deletions.** Each grep must return 0 production hits (`engine`, `ui`, `elements` and the non-test `CuriousContraptions.*` projects; `engine/PartId.cs` is excluded, a FinalIsolation row):
  - `rg -w "WorkshopBattery|AddBatteryConfiguration|ElectricalSourceSettings|IWorkshopElectricalSource|CanonicalSource|ApplySource|RefreshBatteryObservation|BatteryIntent|BatteryToggleText"` → 0;
  - `rg "_battery|WorkshopInput\.Battery\b|PartId\.Battery"` → 0;
  - `rg '"battery"' engine/gpu engine/MachineWorld*.cs` → 0;
  - `git ls-files ui/WorkshopBattery.cs engine/gpu/WorkshopBattery.cs` → empty.

  Also remove:
  - the csproj `Compile Include` and the `.editorconfig` CA1031 block;
  - the `CSharpStaticGateTests` pins at `:61-64`, `:169` and `:812`;
  - two from the `WorkshopHandlerBoundaryTests` floor;
  - `ImpactRules.cs:54`.
- [x] **Known rows shrink only.** Remove all Battery-owned rows: 67, re-measured on `f27cb71d` (Known 301 → 234). Add or reassign none. The Warmup and FinalIsolation rows are unchanged, and `Owner.Battery` stays.
- [x] **Latest-design alignment.** One package authority, compiler and editor; no alias, bridge or fallback. Update:
  - CAT-005 §2/§3/§6 (Q4 is declared);
  - EL-196 §2;
  - CAT-051 Q5 (the owner note);
  - the isolation-doc status.
- [x] **PRIMARY native proof** (production compile, worker engine, tick progression):
  - **Supply:**
    - enabled → the gate opens on the first committed tick;
    - disabled → nothing, and the charge is unchanged;
    - no wire, consumer→battery, or activation alone (B6) → no supply;
    - the 9R.3 own-supply and two-battery checks pass;
    - the lag is unchanged.
  - **Store:**
    - recharge debits within `power_limit`;
    - `initial_charge` 0 delivers nothing;
    - delivered ≤ initial;
    - envelope boundaries hold;
    - a synthetic package compiles its own values;
    - baseline traces are byte-identical.
  - **Control:**
    - a Running toggle returns an Applied receipt and the gate responds next tick;
    - a Paused toggle is queued, reversed by a second click, and applied on Resume;
    - locked, Build-phase, non-source or unknown owners are Rejected;
    - no revision or Undo.
  - **Editor** (real Workshop, drawer placement):
    - defaults are 3600/120/1/Enabled;
    - a finite out-of-range value is clamped; NaN is refused atomically;
    - an OptionButton change is one Undo; Cancel changes nothing;
    - Running, Paused and locked guards hold;
    - the toggle appears only for `RunControl`;
    - the Receiver Choice works.
  - **Lifecycle:**
    - Reset is exact and clears the override;
    - save/load replays exactly, including a save made during Run;
    - forged typed fields and unknown, missing or duplicate parameters are refused.
  - **Old saves:** current-layout battery, domino and counter records and a parameterised generic are refused with typed reasons at both boundaries. Record which kinds still decode.
  - **Lessons:** probe hashes are byte-identical. `lessons-484ec38.json` and `compiled-geometry-9a16.json` are untouched.
  - **Mutations:** each is killed:
    - the store is ignored;
    - Clamp is applied to `enabled`;
    - `RunControl` is ignored;
    - Reset keeps the override.
- [x] **Checks.** *(The bundle build runs inside the slnx build; the Release web publish ran once on the frozen tree.)* The full slnx build with 0 warnings, then one `dotnet test` with `--no-build --no-restore`. Then:
  - the CI "Test C# tools" step under `bash`;
  - the Release worker;
  - the bundle build;
  - strict tsc and the snapshot unit;
  - `anvil_check` and `git diff --check`.
- [ ] **SnapshotApproval → publication.**
  - Commit, merge and push the protected candidate. Verify exact-SHA CI and deployment.
  - At publication, mark `9a-39-generic-instance-battery` done.
  - Append the Story 8.1 re-verification addendum: native only, browser pending.
  - `8-1` stays in-progress until `cat-005` passes in Chrome. This follows the 9r-8 and 9r-11 precedent; the `sprint-status.yaml:130` "closes via addendum" wording is satisfied at that point.
  - Terminal scoped Pass.

# Pending browser cases (post-catalogue)

- `cat-005`, `cat-005-n19`, `cat-051`, `cat-051-interrupt` and `cat-015b` (recharge).
- The receiver capture suite, for Choice rendering.
- Rework `cat-051-own-supply` and `cat-037`, which click the old run-toggle coordinates.
- The AC's single serial Chrome run is deferred.
- **Visible UI changes to verify in Chrome:**
  - the Running/Paused "Charge: X J" label is gone (coordinator decision 2); the four-mark `CommittedCharge` gauge is the charge display;
  - the Build panel is the generic "Configure" editor: one field per declared key (`capacity`, `power_limit`, `initial_charge`) plus an `enabled` option list, replacing the former "Battery settings" SpinBoxes. `cat-005`'s `configure()` clicks the old coordinates;
  - the Running/Paused supply toggle keeps its text ("Disable supply"/"Enable supply", " (queued)");
  - the Receiver's `capture_participation` is now an option list;
  - natively untested and browser-only:
    - the per-frame `_connectionChoices.Visible` rule in `WorkshopGuidance._Process`, which now reads the declared run control;
    - the toggle's per-frame text refresh from committed electrical reads (`RefreshRunControlObservation`).

# Code Map

*Re-derived on `f27cb71d` (23 Oct 2026, implementation owner).*

- **Change:**
  - `CuriousContraptions.Abstractions{,.Electrical}`;
  - `CuriousContraptions.Bundle/{BundleSchema,BundleValidator}.cs`;
  - `elements/Battery/BatteryElementDeclaration.cs`;
  - `engine/gpu/{WorkshopPhysicsCompiler,WorkshopSimulation,WorkshopInstances,WorkshopConstruction,WorkshopWireFlatBuffers,WorkshopSavePrevalidation,AuthoredInstanceTemplate,ElectricalSourceDeclaration,ElectricalSupply,PhysicsGpuAbi.Electrical,WorkshopDeclaredPresentation}.cs`:
    - `WorkshopInstances.cs`: the `IWorkshopElectricalSource` interface and the `Validate` whitelist;
    - `ElectricalSupply.cs`: the envelope literals in `Allocate`;
    - `WorkshopWireFlatBuffers.cs`: `MapAuthoredInstance`, encode, `MapInstance` and `CarriesTypedField`;
  - `engine/{MachineWorld.Gpu,MachinePart}.cs`;
  - `ui/{WorkshopConfiguration,WorkshopGuidance,Workshop,WorkshopAnimation,WorkshopConnections}.cs`:
    - `Workshop.cs:764` (`RefreshBatteryObservation`);
    - `WorkshopAnimation.cs:65` (`_batteryQueued`);
    - `WorkshopConnections.cs:51` (`_batteryCharge`/`_batteryToggle`);
  - `CuriousContraptions.csproj:52` (the `ui/WorkshopBattery.cs` include), `.editorconfig` (CA1031 block), `tools/E2eRunner/ImpactRules.cs:54`;
  - the schema and generated wire, `content/puzzles.json`;
  - `tools/e2e`:
    - `run-reset-snapshot.ts:58`;
    - the hand-written mapper in `workshop-wire.ts` (about line 2401);
    - `cat-005.test.ts` (lines 181–184, 328–329, 345);
    - `cat-051-own-supply.test.ts:131`;
  - tests and fixtures;
  - docs, `TODO.md`, `sprint-status.yaml`.
- **Delete:** `engine/gpu/WorkshopBattery.cs`, `ui/WorkshopBattery.cs`.
- **Reuse:** `SupplyTopology`, `CompiledSupplyGraph`, `ElectricalSupply.Allocate`, `contactWorkOwners`, `WorkshopElectricalControl`, `InstanceParameterCollection`, `AddDeclaredConfiguration`, the C-Balls/C-Bumper probes.
- **Do not change:**
  - the route law and lag, and the allocation law;
  - `ElectricalSourceRecord` and `ElectricalControl`;
  - `PartId.cs` and `DriverVocabulary.cs`;
  - `ElectricalNodes` (its `MaxPower` 100 is unread; owner 9A.45);
  - Half lanes;
  - Planned content rows.

# Coordinator decisions (22 October 2026, under owner delegation)

1. **Gate energy debit:** deferred to Story 11.1 (Motor), the first declared power demand. 11.1 owns EL-196's per-load demand law, and it will settle whether the Powered Gate drive draws battery energy. 9A.39 preserves today's zero debit exactly. Rationale: a conversion must not change trajectories, and the demand law belongs with the first real consumer.
2. **"Charge: X J" panel label:** dropped. CAT-005 Q5 makes the declared four-mark committed-charge gauge the charge display. Record this as a visible UI change in the spec's pending browser cases.

# Former open questions (resolved above)

1. **Gate energy debit (CAT-051 Q5, owner-only).** The gate draws no battery energy, and 9A.39 preserves that. Recommendation: resolve it with EL-196's per-load demand law at Story 11.1 (Motor, the first declared power demand). CAT-005's "powered-work verification outstanding" stays owned there.

# Current state

*Implementation owner, 23 Oct 2026. Uncommitted candidate in worktree `story-9a-39` on base `f27cb71d`. Awaiting independent review. Evidence is in `evidence-9a-39/`.*

**What changed**
- **Contracts.**
  - `ParameterUnit.Joule` and `ParameterUnit.Watt` are added.
  - `ParameterRunEdit { BuildOnly, RunControl }` is added, with `ParameterDescriptor.RunEdit` defaulting to BuildOnly.
  - `SupplySourceStoreDescriptor(Capacity, PowerLimit, InitialCharge, Enable)` is added, as is `SupplyCapabilityDescriptor.Store`.
  - `ElectricalSourceEnvelope` (60/14400 J, 10/480 W) now lives in Abstractions.Electrical. `ElectricalSupply`, `ElectricalSourceDeclaration.ValidateStore` (which absorbs `ElectricalSourceSettings`, including the ABI read) and the validator all share it.
- **Validator.** It returns a `BundleError` and never throws. It refuses:
  - a store on a non-Source, or a Source without a store;
  - a key that is absent (BUNDLE007), duplicated, or shared by two roles;
  - a wrong kind or unit, or a range outside the envelope;
  - an `enabled` that is not exactly `[Disabled, Enabled]`;
  - `RunControl` on anything but a Source store's enable;
  - Clamp on any Choice.

  `ExactEnumConverter<ParameterRunEdit>` is registered, and the round trip compares `Supply` including `Store`.
- **Package.** It declares `capacity`/`power_limit`/`initial_charge` (Real, Clamp) and `enabled` (Choice, Reject, RunControl), plus the store.
- **Engine.**
  - `WorkshopPhysicsCompiler.ElectricalSourceFor` (f32; enable via an explicit 0/1 switch) replaces the `OfType<WorkshopBattery>` loop.
  - `ConfigureElectrical` admits an unlocked generic owner for which `DeclaredPhysics.HasRunControlledSource` holds.
  - The presentation capacity reads the store's capacity parameter.
  - The Battery is generic in `CaptureInstance`, `MapInstance`, encode and prevalidation. Every arm is deleted.
- **UI.**
  - In Build, a Choice renders as an `OptionButton`. This includes the Receiver's `capture_participation`.
  - In Running or Paused, a part with a RunControl store shows one toggle (`ApplyRunControl`, in `WorkshopConfiguration.cs`). The text and queued semantics are unchanged.
  - Visibility depends on the declaration.
  - The "Charge: X J" label is dropped.
- **Wire and content.**
  - Four `InstanceRecord` fields are removed, and the C#/TS code is regenerated with flatc 25.2.10. The base reproduction is byte-identical, except that the README's printf adds one blank line, which is dropped (`candidate/wire-regeneration.txt`).
  - Three `AuthoredScalar` members and the template `enabled` field are removed: 112 content lines.
  - `run-reset-snapshot.ts`, the `workshop-wire.ts` mapper, and `cat-005`/`cat-051-own-supply` are updated at type level only.
- **Deleted:**
  - `engine/gpu/WorkshopBattery.cs`, `ui/WorkshopBattery.cs` and their `.uid` files;
  - `ElectricalSourceSettings`, `IWorkshopElectricalSource`, `WorkshopInput.Battery`, `MachinePart.CanonicalSource`/`ApplySource`;
  - `ElectricalSupply.MaximumCapacity`;
  - the csproj include, the `.editorconfig` block, `ImpactRules.cs:54`, the static-gate pins, and the handler floor (17 → 16, net −1: two handlers removed, `ApplyRunControl` added).

**Assumptions, deviations and decisions (reviewer to confirm)**
1. **ConfigureElectrical admission and the editor toggle.** Both require the store's enable state to be RunControl (`HasRunControlledSource`), not merely a Source store, so that RunControl means something in the engine. A BuildOnly enable is admitted by the validator and offers no control.
2. **Clamp on a Choice is refused for every element.** This is game intent: an option list has no value to clamp. It also makes the "Clamp on enabled" mutant discriminating.
3. **"Undefined enable" refusals are now OutOfRange, at the save and at decode.** At base they were UndefinedEnum. The enable is now a declared Choice index, and an index outside its options is out of range. A NaN parameter is NonFinite at the save and OutOfRange at command decode.
4. **Charge display.** The gauge's full scale is the instance's capacity.
5. **Chrome was not run** (21 Oct policy). The Release web publish ran once on the frozen tree (`candidate/web-publish.txt`: rc 0, the five static-deployment files present), and `AppBundle` was then moved out of the tree. AppBundle was absent for every test and hook run.
6. **Handler floor 17 → 16.** Measured with the test's own matcher: 17 handlers on base, 16 on the candidate. `ApplyBatterySettings` and `ApplyBatteryEnable` were removed and `ApplyRunControl` added, so the floor equals the count. A plain `grep "async void"` gives 20 and 19 because it also counts non-handler lines. A trial floor of 19 failed (`superseded-second-freeze/`). The first freeze is superseded too, because the Battery editor's Running/Paused/locked guards were added after it.
7. **`OfType<WorkshopGenericInstance>` / `IWorkshopDeclaredColliders` re-audit.** Ran `rg` over `engine` and `ui`. Every site that now sees the Battery, and its effect:
   - **Sites with no effect:**
     - the compiler's capture, contact-trigger and contact-work loops, `ActivationNetwork` contact/timer and `WorkbenchCapacity` capture all filter on a declared capability the Battery lacks;
     - the compiler's body loop and `RestoreConstructionPresentation` (`ApplyPresentationParameters`) now receive the four declared parameters instead of the bundle defaults. The Battery's colliders and presentation bind none of them, and the admission sha in all 21 traces is identical.
   - **Intended sites:**
     - the source-store loop, `ConfigureElectrical`, the presentation capacity and the generic editor are tested in `SourceStoreInstanceTests`, `DeclaredPresentationTests` and `PalettePlacementTests`;
     - `CaptureParameters` keeps a placed Battery's parameters and gives a new one the package defaults (the editor test checks the defaults);
     - wire encode and decode and prevalidation (the generic parameter path and the typed-field refusal) are covered by `PreConversionLaunchSaveTests.ACurrentBatteryRecord…` and the save round trips;
     - `WorkshopInstances.Validate` now validates the Battery's parameters against its package.
   - No `DeclaredColliders` consumer exists outside its declaring types.
8. **Process disclosures:**
   - Anvil `anvil_check` ran after the edits (0 warnings, 37 files), not before each write.
   - The generated wire files were written from the shell:
     - `sed '10d' new.cs > engine/gpu/WorkshopWireGenerated.cs`;
     - `cat new.ts` plus the base hand-written tail, then `cp` into `tools/e2e/workshop-wire.ts` (`wiregen.sh.txt`).
   - Test callers were rewritten with an `ast-grep` codemod (53 sites).
   - The spec status line was changed with `sed -i`.
   - A `python3` heredoc wrote `candidate/known-rows.txt`.
   - The probe harness files were moved to the scratchpad with `mv`, and `git rm` staged the four deletions.
   - Every other source edit used the Edit and Write tools.

**Old saves.** Records from the base production encoder (`baseline/capture/old-saves/`, sha256 pinned) were checked at both boundaries (`candidate/capture-final/old-save-outcomes.txt`).
- **Refused with a typed reason:**
  - battery default, custom and lesson-shaped: VerifierRejected;
  - battery disabled and disabled-zero: OutOfRange. Command decode is now typed too; C-Bumper L1's untyped `ArgumentException` is gone;
  - domino and counter: MissingField;
  - switch, ramp, bumper, receiver, springboard, wall and hold_timer: OutOfRange.
- **Still decode unchanged:** lamp, latch, ball_detector, pipe, pressure_plate, clock, basketball and powered_gate (slots ≤ 9, or no parameters).
- 13 of these are hand-encoded in `OldLayoutSaves.PreSourceStoreRecords` and pinned to the base sha256 in `PreConversionLaunchSaveTests`. The C-Bumper-era `bumper strength 8` record re-derives to OutOfRange (from VerifierRejected).

**Evidence** (`_bmad-output/implementation-artifacts/evidence-9a-39/`)

| Check | Receipt | Result |
|---|---|---|
| Suite-first guards on base `f27cb71d` (28 classes; filter in `baseline/guards-filter.txt`) | `baseline/guards.log`, `baseline/build.log` | 831/831; build 0 warnings |
| Base full suite | `baseline/full-test.log` | 17/161/77/189/2419 = 2863 |
| Traces, lessons and placed solutions: one harness, adapter-only difference (`probe/`, `adapter.diff`) | `baseline/capture/`, `candidate/capture-final/`, `candidate/capture-final-compare.txt` | 23/23 byte-identical. 21 battery fixtures, each with compiled source rows, admission sha, per-tick source/store/actuator/body bits, Reset and replay: latch ×7, gate toggle and paused reverse/apply, zero charge, custom, recharge, depletion at 60 J, two sources. Lesson world 12×3 and placed solutions 36 |
| Deletion greps, with a base positive control | `candidate/deletion-greps.txt` (`.sh.txt`) | 0/0/0, no files; base 15/6/3/2 |
| Known list | `candidate/known-rows.txt` | 301 → 234: 67 Battery rows removed, 0 added, Generic section unchanged |
| Frozen Debug build (`--no-incremental`) | `candidate/freeze-build.txt` | 0 warnings, 0 errors |
| Full `dotnet test CuriousContraptions.slnx -c Debug --no-build --no-restore` | `candidate/freeze-test.txt` | WorkerAotGuard 17, Coverage 77, BundleElements 163, E2eRunner 189, CuriousContraptions.tests 2441 = 2887/2887 |
| CI "Test C# tools" step under bash | `candidate/ci-tools.txt` | 77/163/17/174, rc 0 |
| Release worker; Release web publish | `candidate/worker-release.txt`; `candidate/web-publish.txt` | 0/0; rc 0, the static-deployment files present, `AppBundle` moved out of the tree |
| Strict tsc (6.0.3) and snapshot schema unit | `candidate/ts-checks.txt` | rc 0; 24/24 |
| `git diff --check` | `candidate/diff-check.txt` | clean |
| Mutations (`mutation/`; control M0 204/204; restored, `restored-check.txt`) | `M1`–`M7` logs | See the list below this table |

Mutation results:
- **M1** (store ignored): killed by 16 tests.
- **M2** (enable inverted): killed.
- **M3** (RunEdit ignored in `HasRunControlledSource`): killed.
- **M3b** (`ConfigureElectrical` admits any generic): killed.
- **M4** (Reset keeps the control): killed by 2 tests.
- **M5** (Clamp on enable): killed at the build (BUNDLE006) and by the validator unit test. Its test section ran stale binaries; this is annotated in the log.
- **M6** (gauge uses the 3600 J default): killed by the 3 non-default rows. The first attempt failed IDE0059 and was rerun.
- **M7 survives** (the UI call site ignores RunEdit): this is equivalent under the shipped bundle. The shared predicate is killed by M3 and M3b.

**New and changed tests**
- `SourceStoreInstanceTests` (14 cases), all on the production simulation:
  - supply from the first committed tick, and the disabled control, with the zero debit preserved;
  - no wire, a reversed wire and activation links all refused;
  - the store pays within its power limit and never more than its initial charge, at five value sets, including envelope ends and depletion;
  - a synthetic store compiles its own keys;
  - the declared run control;
  - a Running control applies on the next tick with no construction change;
  - a Paused control is reversed by a second one, which is bit-identical to no control, and is applied on Resume;
  - refusals for WrongPhase, locked, gate, ball and unknown owners;
  - an exact Reset that clears the control, a save during Run that keeps the authored value, and a bit-exact replay of the loaded save.
- `PalettePlacementTests`:
  - the generic Battery editor: defaults, clamps, NaN refused atomically, the option list, Cancel and one Undo, the compiled store, and the Running/Paused/locked build-edit refusals;
  - the run toggle: typed controls with queued and reversed states, no edit and no Undo, Reset clearing the queue, the locked state disabled, and no toggle on the Bumper;
  - the Receiver Choice.
- `BundleValidatorTests`: store refusals (19 cases) and RunEdit/Choice-Clamp refusals.
- `PreConversionLaunchSaveTests`: 13 pre-9A.39 records and the current Battery record's forgery refusals.
- `DeclaredPresentationTests`: three non-default capacity rows.
- About 30 callers move to `PlacedParts.Battery`. The `PackageRuntimeParityTests` Battery row is re-expressed against the four declared parameters.

**Risks and next check.** Browser qualification is pending: the cases listed above and the visible UI changes. Next: independent adversarial review of this exact uncommitted candidate, then SnapshotApproval and publication, then the Story 8.1 addendum (native only).
