---
title: "C-Bumper+Spring — Pinball Bumper and Springboard generic instances"
status: review
baseline: 3fce7a034b328be0a79f834a2a70a4d049372a4b
---

# Outcome and authority

CAT-015 Pinball Bumper and CAT-062 Springboard become `WorkshopGenericInstance`s. Their typed f32 parameters, finite contact-work store and slider come only from their packages. `WorkshopBumper`/`WorkshopSpringboard` and every arm that names them leave the engine.

- **Exact source:** epics C-Bumper+Spring, 9A.33, 9A.34, 9R.8 and reconciliation item 4 (this story owns both panels and both `CaptureInstance` arms); CAT-015 §2/§5; CAT-062 §2/§5; spec 6.5 l.19/33/102.
- **Position:** 28. Start only after C-Balls (26) and 9.7b NOR (27) each have a terminal scoped Pass. Merge main first.
- **Proof policy:** production C# unit tests on the real simulation are PRIMARY. The 9R.8 Chrome suite-first proof is deferred to post-catalogue E2E (21 Oct policy).

# Finite design decisions

- **Springboard compile is already generic.** The slider loop evaluates stiffness, damping and initial_compression from `DeclaredPhysics.Parameters`. 9A.34's `OfType<WorkshopSpringboard>` no longer exists. Delete the spring arm of `DeclaredPhysics.Parameters`.
- **Bumper declares one capability, following the C-Switch `SignalContactDescriptor` precedent.**
  - Add `ContactWorkDescriptor(StrengthParameter, ReferenceMass, Threshold, CooldownPhysicalSteps)` with `IContactWorkElement` in Abstractions.Mechanical, plus the bundle payload, validator and round trip.
  - Bumper declares `("strength", 1 kg, 0.05 m/s, 72 steps)`.
  - Validator: the parameter exists, is Real and MetrePerSecond, and its range sits within 0–20, so preload stays at or below 200 J.
  - The shared compiler emits `ContactWorkDeclaration` for each generic instance with the capability. It keeps the payload gate and `Targets`.
  - Preload is `(.5f*m)*s*s` in f32, bit-equal to today's value.
  - Battery→PowerIn recharge stays generic via `contactWorkOwners`.
  - Game intent: one strength knob teaches "harder kick, bigger store". Mass and preload are not player choices.
- **Editor policy preserves today's player behaviour.**
  - Bumper `strength` is `Clamp`: the current SpinBox clamps to 0–20.
  - Springboard parameters stay `Reject`: the current fields refuse out-of-range values visibly (CAT-062 fact 7).
- **Generic slider presentation.**
  - `WorkshopPhysicsCompiler.SliderBody(owner, i)`, the compiler's moving-body identity, replaces `SpringboardPlate`. `IsSecondaryBodyIdentity` tests `SliderBody(owner, 0)`.
  - `RestoreConstructionPresentation` installs it for every declaration with a slider. It displaces the secondary visual's scene rest pose by the declared compression along local Y.
  - Equivalence holds because the `Visual/Plate` scene pose is identity at (0, 0.14, 0), so the result equals today's 0.14 − q. PoweredGate's `Fixed(0)` compression is a no-op.
  - This subsumes the PoweredGate identity arm. Delete that arm and its 2 GateDrive rows as necessary closure (BumperAdvanced precedent). `GateBlade` and the supply arm stay with C-Gate.
- **Wire and content.**
  - Delete `stiffness`, `damping`, `initial_compression`, `strength`, `reference_mass` and `preload` from `InstanceRecord` and `AuthoredScalar`.
  - Later slots renumber: Battery's four fields shift by 3. `half_extents`, `friction`, `target_count` and `parameters` shift by 6.
  - Every old record whose meaning moves is refused with a typed reason. There is no migration and no deprecated slot.
  - ready_to_rebound's locked spring moves to direct `parameters` (400/0.2/0.2). Its 3 `Scalar` bindings are the only ones in content, so delete `AuthoredParameterSource`, `AuthoredParameterBinding` and `parameter_bindings`.
- **Spring Forward controls (decided in spec 6.5, not an owner question).**
  - The authored passive route still solves at preload 0.
  - A distinct charged construction solves with preload set through the generic editor.
  - The same construction at preload 0 fails.
  - A misplaced or 180°-rotated board fails.
- **Naming:** new identifiers use `Slider*`/`ContactWork*`, never "spring", "bumper" or "plate", so the ratchet adds no rows.
- **Scope:** Half compiled lanes and Bumper geometry stay with 9A.44. No new solver, framework or calibration.

# Ordered tasks

- [x] **Suite-first native baseline on the record path** (one Debug build):
  - classes: Bumper, Springboard, ElectricalSupply, ActivationAnimation, PhysicsEngine, LawPrismatic, PackageRuntimeParity, save/wire, NoFixedCapacity, PresentationBindingGolden and the lesson classes;
  - the C-Balls 12×3 admitted-world probe;
  - a **placed-solution probe**: each authored solution placed through the production path in spring_forward, bumper_sidekick, bumper_depth, wall_and_bumper and ready_to_rebound, hashing the compiled scene plus committed state after N ticks (the Bumper is inventory-only, so the admitted world never exercises it);
  - the four Spring Forward routes.
  Retain the raw receipts.
- [x] **Declared capability:** contract, bundle, validator and round trip. Include missing, wrong-unit, unknown-key and over-200 J refusals, and Bumper `Clamp`.
- [x] **Conversion:**
  - the compiler contact-work loop; the generic `_` arms of `CaptureInstance`, encode/decode and prevalidation; generic slider presentation; the generic editor replaces both panels;
  - content `scalars` drop the six keys; ready_to_rebound uses `parameters`;
  - regenerate the C# and TS wire with flatc 25.2.10; update `run-reset-snapshot.ts` and its unit test;
  - fix at type level only: `cat-015a`, `cat-015a-sidekick`, `cat-015b`, `cat-062` and `cat-062b`;
  - test callers move to `WorkshopInput.Generic(PartId.PinballBumper|Springboard, …)`;
  - re-audit unfiltered `OfType<WorkshopGenericInstance>()` selection.
- [x] **Delete** everything named in Decisions, plus:
  - `WorkshopBumper.cs` and `WorkshopSpringboard.cs`;
  - `BumperWork`, `ContactWorkCalibration`, `WorkMass` and `SpringboardSettings`;
  - `WorkshopInput.Bumper/Springboard`;
  - the whitelist entries;
  - `MachinePart.CanonicalSpring/ApplySpring/CanonicalWork/ApplyWork`;
  - both panels with their `Apply*` handlers and `AddConfigurationChoices` arms;
  - the prevalidation preload check.

  **AC:** `rg -w "WorkshopBumper|BumperWork|WorkshopSpringboard|SpringboardSettings|AddBumperConfiguration|AddSpringboardConfiguration|SpringboardPlate|ContactWorkCalibration|CanonicalSpring|CanonicalWork"` and `rg "WorkshopInput\.(Bumper|Springboard)\b"` return 0 production hits.
- [x] **Known rows only shrink.**
  - Remove exactly the Bumper 42 and Springboard 47 rows plus 2 GateDrive presentation rows. Re-measure on the real baseline.
  - Add or reassign none. FinalIsolation `PartId.*` rows are unchanged.
  - At publication, mark `9a-c-bumper-spring-conversion`, `9a-33` and `9a-34` done. `9r-8` stays in review with its Chrome cases queued.
- [x] **Latest design:** one package authority, one shared compiler and engine, committed presentation; no alias, bridge or fallback. Update CAT-015/CAT-062 §2, `docs/springboard-elastic-contract.md` and the EL-194/195 record citations.
- [x] **PRIMARY real-simulation proof** (production compile, worker engine, tick progression):
  - **Bumper:**
    - strength 8 launches the ball paid at more than 7.99 m/s and debits 32 J;
    - a later contact on an empty store spends nothing and bounces ordinarily;
    - partial affordable boost;
    - strength 0 is unpaid, and 20 holds 200 J;
    - preload bits at s = 0/8/20 and one non-Half value;
    - the 72-step per-target cooldown edge;
    - another depth plane never pays;
    - recharge works, a disconnected source gives no credit, and switched routing is refused.
  - **Springboard:**
    - a 0.2 m preload launches the ball, with E = ½kq² within 1e-5;
    - zero preload stays still, and a missed ball gets nothing;
    - stops hold at −0.25 m and 0;
    - the plate visual equals its body pose; the edit-mode rest pose is 0.14 − q; the gate pose matches its golden.
  - **Spring Forward:** the four routes, judged by `WorkshopGoalEvaluator`.
  - **Editor:**
    - Bumper clamps a finite out-of-range value; NaN refuses atomically.
    - Springboard refuses 119, 1201, −0.1, 8.1, −0.01, 0.21 and NaN atomically; endpoints are accepted.
    - Running, Paused and locked guards hold; Cancel leaves no change; one Apply creates one Undo.
  - **Lifecycle:** exact Run/Reset and save/load for both kinds. Forged typed fields and unknown, missing or duplicate parameters are refused.
  - **Old saves:** hand-encoded pre-conversion bumper, spring, battery, domino and counter records, plus one parameter-carrying generic (Switch/Ramp), are each refused with a typed reason through save prevalidation and command decode (`WireFaultReason`). Record which old kinds still decode unchanged.
  - **Lessons unchanged:**
    - the 12×3 admitted-world/tick hashes and the placed-solution hashes are byte-identical to the baseline;
    - five native solve-runs pass;
    - `compiled-geometry-9a16.json` and `lessons-484ec38.json` are untouched, with normalisation on the expected side only.
- [x] **Builds, once each, reused with `--no-build --no-restore`:**
  - Debug slnx, Release worker, bundle;
  - strict tsc and the wire-schema unit test; the isolation ratchet;
  - one full slnx test plus the CI "Test C# tools" step on the frozen candidate;
  - web publish, `anvil_check` and `git diff --check`.
- [ ] **Publication:** independent SnapshotApproval on the exact candidate, then protected commit/merge/push. Verify exact-SHA CI, artifact and deployment; terminal scoped Pass. Append the 6.5e 9R.8 native addendum without browser claims.

# Pending browser cases (post-catalogue)

- Author `tools/e2e/spring-forward.test.ts` (9R.8): drawer placement, preload through the generic editor, the four routes, Run/Reset and Save/Load. Add `spring_forward` to the driver's `WorkshopLevel` union.
- Re-run `cat-015a`, `cat-015a-sidekick`, `cat-015b`, `cat-062`, `cat-062b`, `engine-core-2a2`, `anim-1b` and `cat-005`. The UI field names become the descriptor keys.

# Code Map

- **Change:**
  - engine: `engine/gpu/{WorkshopBumper,WorkshopSpringboard}.cs` (delete), `{ContactWorkDeclaration,WorkshopConstruction,WorkshopInstances,WorkshopPhysicsCompiler,DeclaredPhysics,WorkshopWireFlatBuffers,WorkshopSavePrevalidation,AuthoredInstanceTemplate}.cs`, `engine/{MachineWorld.Gpu,MachinePart}.cs`, `ui/WorkshopConfiguration.cs`;
  - packages and bundle: `elements/Bumper/BumperElementDeclaration.cs`, Abstractions.Mechanical, `CuriousContraptions.Bundle/{BundleSchema,BundleValidator}.cs`;
  - wire and content: `schemas/workshop_wire.fbs` and its generated outputs, `tools/e2e/run-reset-snapshot.ts`, the five suites, `content/puzzles.json`, `element-isolation-known.json`;
  - about 19 test files; docs; `TODO.md`; `sprint-status.yaml`.
- **Reuse:** `WorkshopInput.Generic`, `InstanceParameterCollection`, `AddDeclaredConfiguration`, `CaptureParameters`, `ContactWorkDeclaration`/`ContactWorkStore`, the generic slider compile, `DeclaredPhysics.IsPayload`, the C-Balls lesson probe.
- **Do not change:** `PartId.cs`, the solver and worker kernels, `GateBlade` and the gate supply arm, compiled Half lanes, golden fixtures, Planned content rows.

# Open questions

None owner-only. 9R.8's zero-preload control applies to the matched charged construction, as spec 6.5 already decided.

# Evidence

Raw receipts: `evidence-c-bumper-spring/baseline/` (record path 3fce7a03, `baseline-head.txt`), `candidate/` (frozen candidate), `probe/` (probe and adapter sources), `mutations/`. Native filters: `dotnet test CuriousContraptions.tests/CuriousContraptions.tests.csproj -c Debug --no-build --no-restore --filter <classes>`. Strict TS: TypeScript 6.0.3 `tsc --noEmit --project tools/e2e/tsconfig.json --typeRoots <@types/node 26.0.1>`. Schema: tsx 4.23.15 `--test tools/e2e/unit/run-reset-snapshot.test.ts`.

| Criterion | Command | Result |
| --- | --- | --- |
| Baseline, record path (3fce7a03) | Debug slnx build; full slnx test (`baseline-build.txt`, `baseline-full.txt`) | 0 warnings; 17/143/77/189/2245 = 2671/2671 |
| Suite-first classes on the record path, new suites included | 27-class filter (`baseline-classes-filter.txt`, `baseline-classes.txt`): Bumper, Springboard, ElectricalSupply, ActivationAnimation, PhysicsEngine, LawPrismatic, LawSensorWork, PackageRuntimeParity, Save, SaveRejection, WireFlatBuffers, Wire, NoFixedCapacity, PresentationBindingGolden, the lesson classes, SwitchElectrical, PalettePlacement, WorkbenchCapacity, and the new ContactWorkInstance, SliderInstance and LaunchLessonSolve | 791/791. The new suites (`baseline-new-suites.txt`) 29/29 through the record-path adapter |
| Spring Forward four routes and five native solve-runs on the record path | `LaunchLessonSolveTests` (`baseline-launch-routes.txt`). Poses and precision are the Chrome-proven ones: cat-062b Precise passive/charged/zero/away; sidekick (−3.3, 1, 0) and depth (0, 1, 3.3) at 0.45; wall_and_bumper with Wall (−1, 4, 0) .4×6×1.5; ready_to_rebound Receiver (1.8, 1.3, 0) | 12/12: passive, charged, sidekick, depth, wall and ready solve; zero, away, unpaid, depth-miss, wall-miss and missing-Receiver stay unsolved; Reset exact |
| Old-layout bytes = record-path encoder | `probe/OldLayoutEqualityProbe.cs.txt` on 3fce7a03 (`baseline/old-layout-equality.txt`) | 9/9 hand-encoded saves byte-equal the old production encoder and load (sha256 pinned in `PreConversionLaunchSaveTests`) |
| Lessons unchanged: 12×3 admitted world and placed solutions | `probe/LessonWorldProbe.cs.txt`, identical source on both sides except the `PlacedParts` adapter (`probe/adapter.diff`). Base `baseline/probe-base-*.txt`, candidate `candidate/probe-cand-*.txt` | Admitted world plus 480-tick reads: 36/36 byte-identical. 12 routes × 3 precisions via `WithPrecision`, hashing admission, the 1200-tick trace, every contact-work read, Reset and the goal verdict: 36/36 byte-identical (`cmp` clean) |
| Declared capability | `BundleValidatorTests.ContactWorkCapabilityRequiresOneTypedStrengthWithinTheTwoHundredJouleStore`; `BundleRoundTripTests` (Bumper package incl. `ContactWork`) | Pass: 200 J boundaries (1 kg at 20, 4 kg at 10), cooldown 9/14400 and threshold 0/64 accepted. Refused as a `BundleError`, never a throw: missing, unknown key, duplicate key, wrong unit, not Real, negative minimum, maximum above 20, over 200 J, mass 0/NaN/101, threshold −0.01/64.5/∞, cooldown 8/14401, dynamic owner, slider owner |
| Bumper on the real simulation | `ContactWorkInstanceTests` (14 cases), `WorkshopPhysicsEngineTests` (heavy-payload ×12, target-limited, strength-0, recharge phases), `WorkshopPhysicsLawSensorWorkTests` (72-step per-target cooldown edge, depth miss, > 7.99 m/s zero-gravity radial launch) | Pass. Preload bits at s = 0/8/20/3.3333333. Store 0 J and 200 J. A strength-8 1 kg ball leaves at the target within one tick of gravity and the ledger closes against 32 J. A 4 kg ball spends 32 J and stays under target, and later contacts are unpaid and still bounce. The depth plane never pays. A wired battery recharges, an unwired one credits nothing, switched routing is refused. Only the declaring part gets a store. A synthetic capability compiles its own values. Exact Run/Reset and Save/Load |
| Springboard on the real simulation and presentation | `SliderInstanceTests` (7), `WorkshopSpringboardTests` (converted), `WorkshopPhysicsLawPrismaticTests` | Pass. 0.2 m preload launches a resting ball over 0.2 m, and the uncharged board does not (< 0.05 m). A missed ball's path equals the no-board path (≤ 1e-4 m). Stops hold within 1 cm (−0.25, 0). E = ½kq² in [7.99999, 8.00001] J. Rest pose: 0.14f − q bit-exact over 0.2 → 0.1 → 0 → 0.1234567, equal to the compiled body. The gate blade sits exactly at its scene pose under `SliderBody(gate, 0)`. Exact Run/Reset and Save/Load |
| Editor (generic, real Workshop scene, drawer placement through `CaptureInstance`) | `PalettePlacementTests.GenericBumperEditorClampsAFiniteStrengthAndRefusesNaNAtomically`, `…GenericSpringboardEditorRefusesOutOfRangeValuesAtomicallyAndAcceptsEndpoints` | Pass. Placed defaults are 8 and 400/0.2/0. Bumper clamps −1 → 0 and 25 → 20, keeps 12.345678, and refuses NaN with no command and no Undo. Springboard refuses 119, 1201, −0.1, 8.1, −0.01, 0.21 and NaN atomically, and accepts every endpoint with one Undo each. Cancel, Running, Paused and locked all hold |
| Lifecycle refusals | `WorkshopBumperTests.ConstructionRoundTripPreservesTheStrengthAndRefusesForgedStores`, `WorkshopSpringboardTests` (invalid preload/settings saves), `PayloadInstanceTests` typed-field theory | Refused: every typed field (11) on a generic Bumper; an undeclared `preload` parameter; strength 20.5 or −0.01; missing or duplicate strength (OutOfRange); NaN (NonFinite) |
| Old saves | `PreConversionLaunchSaveTests` (receipt `candidate/old-layout-outcomes.txt`) | Each refused at both boundaries with a typed reason. Save: bumper 8 VerifierRejected, bumper 0 OutOfRange, spring ×2 OutOfRange, battery VerifierRejected, domino MissingField, counter MissingField, switch and ramp OutOfRange. Command decode: the same `WireFaultReason`s. Control: current-layout Bumper and Springboard load at both |
| Golden fixtures | `PackageRuntimeParityTests.CompiledGeometryMatchesPreCutoverGolden`, `WorkshopSaveTests.ContentPreservesPrechangeLessonGeometryInventoryAndProfiles` | `compiled-geometry-9a16.json` and `lessons-484ec38.json` untouched. Expected-side normalisation only: the historical spring `Settings` become its three `Parameters` by the same values; bodies and prismatics are still compared |
| Mutations (disposable copy, 7 affected classes, 193 tests; M0 control 193/193) | `mutations/*.diff`, `*.test.log` | All killed. M1 (threshold/cooldown hard-coded) and M2 (reference mass ignored): capability test. M3 (rest pose compounds) and M6 (compression not presented): plate rest-pose test. M4 (Clamp applied to every parameter): Springboard editor plus 4 other editor tests. M5 (Bumper declares Reject): Bumper editor. The first M5/M6 runs used a stale incremental build (restored file older than the mutant's outputs); they are kept in `mutations/invalid-stale-build/` and were rerun |
| Wire regeneration | flatc 25.2.10 per schemas/README (`candidate/wire-regeneration.txt`) | TS generated part = fresh output byte-for-byte; C# = fresh output except the existing header blank line (absent at base too) |
| Deletions | `candidate/deletion-greps.txt` | 0 production hits for both AC greps and for `WorkMass/ApplySpring/ApplyWork/AuthoredParameterSource/AuthoredParameterBinding/parameter_bindings`; 0 removed TS accessors in `tools/e2e`; tests: one adapter doc comment only |
| Isolation ratchet | `ElementIsolationTests`; `candidate/known-rows.txt` | Known list: 91 rows removed (Bumper 42, Springboard 47, GateDrive 2 presentation rows), 0 added; 401 → 310 rows; the stale set equals the removed set exactly |
| Strict TS / schema | `candidate/tsc.txt`, `candidate/ts-snapshot-unit.txt` | tsc exit 0 (`strict: true`); snapshot schema 24/24 |
| Frozen candidate | `dotnet build CuriousContraptions.slnx -c Debug` (`final-build.txt`); `dotnet test CuriousContraptions.slnx -c Debug --no-build --no-restore` (`final-test.txt`) | 0 warnings, 0 errors. WorkerAotGuard 17/17, BundleElements 144/144, Coverage 77/77, E2eRunner 189/189, CuriousContraptions.tests 2276/2276 = 2703/2703. The first freeze (2273, before the review-readiness additions below) is kept in `candidate/superseded-first-freeze/` |
| CI steps (bash) | "Test C# game" (`ci-game.txt`); "Test C# tools" verbatim body under bash (`ci-tools-step-source.txt`, `ci-tools.txt`) | 2276/2276; Coverage 77, BundleElements 144, WorkerAotGuard 17, E2eRunner 174 (15 toolchain excluded), rc 0 |
| Charged route through the generic editor | `PalettePlacementTests.TheChargedSpringForwardRouteSolvesWithItsPreloadSetThroughTheGenericEditor` | Pass. The real Workshop handler edits the zero-preload board at the charged pose (Precise) to 0.2 m through the lesson's placement configuration. Only the preload changes, with one Undo. The edited construction solves in the real simulation, the unedited one does not |
| Current-layout forged records, both kinds | `PreConversionLaunchSaveTests.ACurrentRecordCarryingATypedFieldOrAWrongParameterSetIsRefusedAtBothBoundaries` (bumper, spring) | Refused OutOfRange at save pre-validation and command-decode mapping: each of the 11 typed fields beside valid parameters, plus an undeclared `preload`, a missing parameter and a duplicate parameter. The exact declared set loads at both |
| Worker / web | `dotnet build CuriousContraptions.Simulation -c Release` (`worker-release.txt`); `dotnet publish CuriousContraptions.web -c Release --nologo -p:PlaytestDiagnostics=false` (`web-publish.txt`) | 0/0; publish rc 0, `index.html`, `404.html`, `godot.wasm` present; AppBundle (0 tracked files) moved to the scratchpad afterwards |
| Anti-pattern / whitespace | `anvil_check` over the 43 changed source, content, fixture and config files; `git diff --check` | 0 warnings; clean |

# Current state

- Candidate: uncommitted on `story-c-bumper-spring` at base 3fce7a03 (= main). Awaiting independent review, then SnapshotApproval and publication.
- Publication gate (blocks publication, not review): the spec requires 9.7b NOR (position 27) to hold a terminal Pass and main to be merged first. Neither holds yet; the coordinator started this slice on the C-Balls merge. Merge-main checklist when NOR lands:
  - grep all code, tests included, for `WorkshopInput.(Bumper|Springboard)`, `BumperWork`, `SpringboardSettings`, `SpringboardPlate`, `CanonicalSpring` and `ApplyWork`;
  - grep `content/` for the six scalar keys and `parameter_bindings` (`AuthoredInstanceReader.Require` refuses any template still carrying them);
  - re-audit NOR's tests for type-only generic selection;
  - on any schema or generated conflict, regenerate with flatc 25.2.10 and diff against fresh output;
  - re-measure the Known diff against the new main (91 removed, 0 added);
  - then one Debug slnx build, the full slnx test, the CI tools step under bash, the Release worker and the web publish, with no `AppBundle` present for the ratchet.
- Lesson poses (pre-existing content finding): the bumper solve-runs use the Chrome-proven poses (sidekick (−3.3, 1, 0), depth (0, 1, 3.3), both at Balanced). The content `solution` rows say y = 1.5 (and depth z = 3.2), and the C-Balls review probe showed sidekick's content pose latches nothing natively. The solution rows are documentation and no production path reads them, so this slice changes nothing there. Owner: lesson-content maintenance (the 150-level difficulty/nudging sweep).
- Pre-existing validator hazard (not this slice): `BundleValidator`'s capture check uses `SingleOrDefault` on the participation key, so a package declaring that key twice throws instead of returning an error. The new contact-work check refuses a duplicate strength key as an error. Owner: bundle-validator maintainers.
- Deleted symbols: `WorkshopBumper`, `BumperWork`, `WorkshopSpringboard`, `SpringboardSettings`, `ContactWorkCalibration`, `WorkMass`, `WorkshopInput.Bumper/Springboard`, `WorkshopPhysicsCompiler.SpringboardPlate`, `MachinePart.CanonicalSpring/ApplySpring/CanonicalWork/ApplyWork` and `_physicalReady`, `AddBumperConfiguration`/`ApplyBumperStrength(Async)`/`AddSpringboardConfiguration`/`ApplySpringboardSettings(Async)`, `AuthoredParameterSource`/`AuthoredParameterBinding`/`ParameterBindings`/`parameter_bindings`, the six `AuthoredScalar` members and schema fields, the `spring`/`bumper` arms of `CaptureInstance`, `MapInstance`, encode, prevalidation, `DeclaredPhysics.Parameters` and the whitelist, the PoweredGate presentation arm, and their `.editorconfig` and static-gate pins.
- Added: `ContactWorkDescriptor`/`IContactWorkElement` (Abstractions.Mechanical), `MechanicalPayload.ContactWork` and its validator, `WorkshopPhysicsCompiler.SliderBody`/`ContactWorkFor`, `MachinePart.InstallSliderPresentation` (scene rest pose kept per slot), the TS `parameter(instance, key)` helper.
- Deviations:
  - The "launches paid at > 7.99 m/s" check in the gravity simulation is bounded: the read closes up to one tick after the event, so the measured speed is at most 8.0001 and plus 9.81/120 above 7.99. The zero-gravity > 7.99 m/s radial launch stays CAT-015 fact 1 (law suite). The first attempt asserted > 7.99 raw and failed at 7.9788923 = 8 − one substep of gravity (`baseline/attempt1-instance-suites.txt`).
  - `run-reset-snapshot.test.ts` needed no edit (it derives the listing from the schema).
  - `PayloadInstanceTests.PreConversionRecordsAreRefusedWithTheTypedSaveRefusal`: the C-Balls-era Domino record is now MissingField (its half extents lie beyond the 18-field table), where it used to be VerifierRejected. It is still refused.
  - `WorkshopHandlerBoundaryTests` floor 19 → 17: two handlers deleted.
  - `BundleRoundTripTests.AssertSame` compares `uint` (the cooldown) as a scalar.
  - `SliderBody` accepts slider index 0–15 (the 256-identity stride) and fails fast beyond.
  - `9r-8` stays backlog, per the C-Balls `9r-11` precedent (L8): its Chrome ACs are the pending browser cases. Line 76's "stays in review" wording is superseded.
- Old saves:
  - Kinds that still decode unchanged (by layout, not by a test): records whose fields all sit at slot 9 or below, where the slots are identical. That covers lamp, latch, ball_detector, pipe (length), pressure_plate and clock (threshold), and the zero-field generics (balls, bends, funnel).
  - Every record whose meaning moved is refused with a typed reason (table above): spring, bumper, battery, domino, counter, and any generic with parameters.
- Payload gate (`!IsAuthored || FirstPayload`) is unchanged and unobservable for a validated lesson, which always holds its payload; no test discriminates its removal (C-Balls L5 precedent).
- Spring Forward at Forgiving/Balanced: the probe records that the matched zero-preload construction also captures (Receiver forgiveness). The 9R.8 controls are therefore asserted at Precise, as cat-062b proved; identical on both sides.
- First route run failed for every route on revision 1 (Construct is rejected on the initial revision); revision 2 is the harness convention. That log was overwritten by the corrected run; disclosed here.
- Process disclosure:
  - Anvil validation preceded most production writes, but not every test or doc Edit. Those were covered by `anvil_check` on the frozen files: 0 warnings.
  - One accidental `sed -i` replaced one Bumper caller line in `NoFixedCapacityTests.cs`. The content is as intended and visible in the diff.
  - Generated wire code followed schemas/README: flatc output was installed with `cp`, and the TS generated part was spliced by the README's documented procedure. The C# header blank line was then restored with Edit.
  - Probes ran as temporary test files and were removed. Their sources are in `probe/`.
- Risks:
  - Browser qualification is pending: 9R.8 `spring-forward.test.ts` is unauthored, and the five type-fixed suites are not run.
  - The Bumper editor's field label and name are now `strength`; the Chrome suites still click the former panel coordinates and need post-catalogue rework.
- Next check: independent adversarial review of this candidate, then SnapshotApproval and normal publication.
