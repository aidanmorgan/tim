---
title: 'Table-driven mechanical kind routes and S6 closure (S6g)'
type: 'refactor'
created: '2026-11-24'
status: 'draft'
baseline_revision: 'captured at dispatch (the main that contains the planning work)'
review_loop_iteration: 0
followup_review_recommended: false
context:
  - '{project-root}/docs/testing-guidance.md'
  - '{project-root}/docs/gpu-f32-physics.md'
  - '{project-root}/_bmad/custom/element-free-engine-design.md'
  - '{project-root}/_bmad-output/implementation-artifacts/spec-s6b-response-codec-read-family-table.md'
warnings: []
deferred: []
---

<intent-contract>

## Intent

**Problem:** Joint coordinate kinds, continuous-work input kinds, body-target and target-selector kinds and mechanical value sources are switched by hand in up to nine places each: bundle validators, the wire codec, the physics compiler, the airflow compiler and validation, the interaction ABI, the identity map and, for the continuous-work input, the per-tick payment in the simulation (`WorkshopPhysicsEngine.ContinuousCommit.cs:51`). `ENGINE-JOINT-KINDS`, `ENGINE-SUPPLY-DEMAND` and the fluid engines add values to these enums. This slice is also the last of S6: it closes the story (owner decision 24h, coordinator decision 42).

**Approach:** Per enum, one static table keyed by it, in the layer that reads it: `CoordinateKind` (the reference's validity, its prismatic or revolute joint identity and its binding), `ContinuousWorkInputKind` (the input's validity, its compile and its per-tick payment), `BodyTargetKind` and `TargetSelectorKind` (the selected bodies and the selector's resolution), `MechanicalValueSource` (its evaluation and its range, where each validator reads the same row). Contact results, motion and refusals are unchanged. Closure: the S6 inventory commands run on the candidate and print only table rows, kept single-member comparisons and the excluded enums, and every in-scope enum has a test that every value has a route.

## Boundaries & Constraints

**Always:**
- Every value of `CoordinateKind` (2), `ContinuousWorkInputKind` (3), `BodyTargetKind` (3), `TargetSelectorKind` (3: `AllDynamic`, `AllPayloads`, `Named`) and `MechanicalValueSource` (4: `Constant`, `Parameter`, `NegatedParameter`, `Product`) has exactly one row in its table. An undefined value answers each site as that site answers it today; the miss branch stays at the site and no lookup helper unifies the refusals. The run log lists, from the baseline, each site that refuses an undefined value with its exception type and message prefix (for example `ConstraintCoordinateReference.Validate`'s `_ => false`, `MechanicalValue.Evaluate`'s "Invalid or unresolved mechanical value.", `MechanicalOperand.Evaluate`'s "Invalid mechanical product operand.", the body-target set's "Undefined target policy." and "Invalid declared body target set.", `ContinuousInput.Validate`'s "Invalid exact continuous-work input.").
- Coordinates, airflow payment, contact-work targets and mechanical values are unchanged for every delivered package and lesson; the schema's field set is unchanged.
- The per-tick payment (`CommitContinuousWork`) and the coordinate lookups on the physics engine's per-tick path allocate nothing new and use no reflection (trimmed, ahead-of-time simulation worker).
- Tables are built once from static data and functions; no runtime enum enumeration in production. A table that is read by the Bundle project lives in Abstractions or Bundle; engine and Simulation rows live there.

**Never:**
- A new joint kind, input kind or value source: owned by `ENGINE-JOINT-KINDS` and `ENGINE-SUPPLY-DEMAND`. Merging enum declarations; a change to the wire schema, element packages or any contact, support or geometry function body.
- A second dispatch path; a default row; a test-only accessor; suppressions; hashes; mutants; browser runs.
- Adding any enum to S6 beyond the inventory: an enum the closing run prints that is in neither list is recorded and returned to the coordinator, not tabled here.

## I/O & Edge-Case Matrix

| Row | Input / State | Expected Output / Behavior | Error Handling |
|-----|--------------|---------------------------|----------------|
| G1 | For each `CoordinateKind`, a real scene of the part named for it (prismatic: `LinearPusher`, whose slider coordinate `(Prismatic, 0)` is a supply drive target, `LinearPusherElementDeclaration.cs:54`; revolute: `Motor`), encoded and decoded through the response and command codecs; the revolute coordinate is also bound by `Clutch`'s transmission (`ClutchElementDeclaration.cs:148`; no delivered transmission binds a prismatic coordinate, so the prismatic binding is read through the bundle validators and `MechanicalCoordinateBindingsTests`) | Round-trips; the coordinate drives its binding as today; the bundle validators (`MechanicalTransmissionValidation`, `PresentationBindingActuator`) read the same row | Control: a coordinate naming a missing joint refused as today |
| G2 | For each `ContinuousWorkInputKind`, a real scene of `Fan` with that input selected (`Fan` declares a store, an electrical and a coordinate input, `FanElementDeclaration.cs:53-55`) | The input pays its source as today (a store debit, an electrical debit, a coordinate sense) and delivers flow | `ArgumentException` "Invalid exact continuous-work input." for a malformed input; control: the input absent, no flow |
| G3 | For each `TargetSelectorKind`, the selector through the public `WorkshopPhysicsCompiler.ResolveTarget` (`InteractionTargetTests.cs:30`, `:59`, `:66`, `:74-78`): `AllPayloads` as the Receiver declares it (`ReceiverElementDeclaration.cs:16`), `AllDynamic` (the selector's default) and `Named` as `InteractionTargetTests` builds them with a level puzzle (no delivered part declares either of the last two); for each `BodyTargetKind` (`NamedBody`, `AllDynamic`, `AllPayloads`) the resolved target's `Contains` | Selects the same bodies as today; `AllPayloads` with no payload body selects none | Today's refusals ("Undefined target policy.", "Invalid declared body target set.") |
| G4 | For each `MechanicalValueSource`, a real declared mechanical value of a delivered part (`Fan` and `LinearPusher` declare `Constant`, `Parameter`, `NegatedParameter` and `Product`; `WoundSpring` declares `NegatedParameter`), through `MechanicalValue.Evaluate`, `MechanicalValueDomain.TryRange` and the bundle validators that read the source (`Value`, `DriveValue`, `ValidRatio`, `ValidCloseSeconds`, `ValidTravel`, `RunControlBindings.Matches`) | Resolves to the same value and range as today | `ArgumentException` "Invalid or unresolved mechanical value." and today's `BundleError`; boundary: `NegatedParameter` at the parameter's minimum (`WoundSpringContractTests.cs:66-75`) |
| G5 | Undefined values of each enum at each site the run log lists | Refused at the site as before | The type and message the list names |
| G6 | The S6 inventory commands (S6b Code Map) on the candidate | Only table rows, the tables' own lookups, kept single-member comparisons (listed with file:line) and the excluded enums print; the run log lists each in-scope enum with its route test and the unnamed enums with a member of three or more distinct members (none beyond the excluded list) | A printed enum in neither list is returned to the coordinator before review |

</intent-contract>

## Code Map

Baseline: captured at dispatch (coordinator decision 38, 23 November 2026): the published main that contains the planning work and the slices before this one; scope guards compare against it. Drafted on `25df3cd5` (the drafting revision only, not a baseline); every line cited here was read there on 24 November 2026. **Status: draft; runs before the engine programme** (owner decision 24h, 23 November 2026); its publication closes story S6 (coordinator decision 42, 24 November 2026, which split S6f into S6f and this slice). Prerequisites: S6f published (the chain S6c, S6d, S6e, S6f, S6g is strictly ordered; both S6e and this slice edit `WorkshopWireFlatBuffers.Compiled.cs` and `BundleValidator.cs`). Review tier: **engine**. Delivery shape: one structural slice, the mechanical rows of the S6 follow-on inventory (`spec-s6b-response-codec-read-family-table.md`) and its closing run.

Planning verdict: replanned at dispatch from the main that contains the previous S6 slice (coordinator decision 43); sixth-audit findings in `docs/planning/elements/readiness/audits/audit-s6g-6.md` carried into that replanning.

Owned scope (complexity at threshold 12, 23 November 2026, in brackets):
- Coordinates (`CoordinateKind`): `CuriousContraptions.Abstractions/ConstraintCoordinateReference.cs:32`; `CuriousContraptions.Bundle/BundleValidator.cs:330` (the switch inside `PresentationBindingActuator`; the member's driver rules were S6c's and this switch is the one S6c left); `CuriousContraptions.Bundle/MechanicalTransmissionValidation.cs:49`; `CuriousContraptions.Simulation/WorkshopPhysicsEngine.cs:740`; `engine/gpu/WorkshopPhysicsCompiler.cs:341`; `engine/gpu/WorkshopSceneCompiler.cs:19`; `engine/gpu/PhysicsGpuAbi.Constraints.cs:623`; `engine/gpu/WorkshopSupplyCompiler.cs:94-97` (the `Coordinate.Kind: CoordinateKind.Prismatic` and `Revolute` property patterns inside the `:92` descriptor-type switch, which stays); `engine/gpu/WorkshopWireFlatBuffers.Compiled.cs:65-70`, `:72-77` (the identity and decode switches).
- Continuous-work inputs (`ContinuousWorkInputKind`): `CuriousContraptions.Abstractions/AirflowValues.cs:59` (the validity switch); `engine/gpu/WorkshopPhysicsCompiler.Airflow.cs:68` and `CuriousContraptions.Bundle/AirflowDomain.cs:131` (descriptor-type dispatch over `StoredAirflowInput`, `ElectricalAirflowInput`, `CoordinateAirflowInput`); `engine/gpu/AirflowSceneValidation.cs:112`; `CuriousContraptions.Simulation/WorkshopPhysicsEngine.ContinuousCommit.cs:51-72` `CommitContinuousWork` (the per-tick payment; a hot path, see AC5). Kept as single-member comparisons, recorded with file:line: `engine/gpu/PhysicsGpuAbi.Airflow.cs:93`, `CuriousContraptions.Simulation/WorkshopPhysicsEngine.ContinuousPrepare.cs:54`, `ContinuousSolve.cs:107` (hot path, unchanged).
- Targets (`BodyTargetKind`, `TargetSelectorKind`): `CuriousContraptions.Abstractions/ContactWorkDeclarationValues.cs:39`; `engine/gpu/WorkshopPhysicsCompiler.cs:646`; `engine/gpu/PhysicsGpuAbi.Interactions.cs:23`; `engine/gpu/CompiledIdentityMap.cs:101`.
- Value sources (`MechanicalValueSource`): `CuriousContraptions.Abstractions/MechanicalProduct.cs:14` (`MechanicalOperand.Evaluate`); `CuriousContraptions.Abstractions/MechanicalContractValues.cs:175` (`MechanicalValue.Evaluate`); `CuriousContraptions.Bundle/MechanicalValueDomain.cs:16` (`TryRange`), `:26` (`ProductRange`), `:42` (`ScalarRange`), `:62` (`SliderTravel`); `CuriousContraptions.Bundle/BundleValidator.cs:832` (`DriveValue`), `:845` (`Value`); `CuriousContraptions.Bundle/MechanicalTransmissionValidation.cs:60` (`ValidRatio`), `:75` (`ValidCloseSeconds`); `CuriousContraptions.Bundle/DrivenSurfaceValidation.cs:47` (`ValidTravel`); `CuriousContraptions.Bundle/RunControlBindings.cs:26` (`Matches`). Kept as single-member comparisons, recorded with file:line: `CuriousContraptions.Bundle/BundleValidator.cs:736`, `engine/gpu/DeclaredPhysics.cs:132`.
- `CuriousContraptions.tests/MechanicalKindRouteTests.cs` -- new: G1 to G5.
- `docs/planning/invest/delivery-batches.md` (story S6: S6 closed), `deferred-work.md` (the S6 item closed), `TODO.md`, `sprint-status.yaml` (key `s6g-mechanical-kind-tables`), this contract and its run log. A new file under `engine/gpu/` or `CuriousContraptions.Simulation/` is added to `CuriousContraptions.Core/RuntimeSources.props` (its compile list is explicit) in the same candidate.

Direct consumers: every member keeps its signature. `BundleValidatorTests` (in `tools/BundleElements.Tests`, which the filter reaches), `MechanicalCoordinateBindingsTests`, `CoordinateTravelGeneralityTests` and `ClutchTests` cover the coordinate sites; `AirflowRuntimeTests` (the `AirflowGeneralityTests` and `AirflowSupplyIntegrationTests` files are partial-class files of it) and `RotaryAirflowRuntimeTests` the inputs; `ContactWorkInstanceTests`, `InteractionTargetTests` the targets; `WoundSpringContractTests` the value sources. `docs/planning/elements/readiness/engine-design-gap.md` and the `ENGINE-JOINT-KINDS` and `ENGINE-SUPPLY-DEMAND` story text cite these lines and re-read them.

### Capability probe

All routes exist; nothing in a solver law, the worker ABI, the wire schema or a shared presentation contract changes.

- **Present:** coordinates -- `MechanicalCoordinateBindingsTests`, `CoordinateTravelGeneralityTests`, `ClutchTests` (the file `ClutchGeneralityTests.cs` extends); continuous-work inputs -- `AirflowRuntimeTests` (files `AirflowGeneralityTests.cs` and `AirflowSupplyIntegrationTests.cs`), `RotaryAirflowRuntimeTests`, `FanIntegrationTests`; targets -- `ContactWorkInstanceTests`, `InteractionTargetTests`; value sources -- `BundleValidatorTests`, `WoundSpringContractTests` (`NegatedParameter`, `:66-75`), `MechanicalProductAdmissionTests`.
- **Present, untested:** that every value of the five enums has exactly one row. This slice adds G1 to G5, and G6 for the whole of S6.

### Decisions

- **Mechanical kinds stay in S6 (decided).** Owner decision 24h names every per-capability chain.
- **Each layer keys its own enum (decided).** A Bundle member cannot read an engine table; the value-source rows live in Abstractions and Bundle, the payment and compile rows in engine and Simulation.
- **The closing run is S6's acceptance (decided).** AC4 and G6 close the story: the inventory in S6b defines chain, commands and rule, so "dispatched only inside its table" is decidable.
- **Performance (owner decision 3a and coordinator decision 42).** `CommitContinuousWork` and the coordinate lookup on the physics engine's per-tick path are hot; the hot-path rule of the inventory (S6b) applies (AC5).

### Concurrency

After S6f; last of S6. S6c edited `BundleValidator.cs` and `WorkshopCompiledScene.cs`, and S6e edited `BundleValidator.cs` and `WorkshopWireFlatBuffers.Compiled.cs`, before this slice; this slice re-reads its lines (`BundleValidator.cs:330`, `Compiled.cs:65-77`) on the merged main. Repair parts A to C edit package scenes only. The new test file is staged with `git add` before the candidate commit.

## Tasks & Acceptance

**Execution:**
1. `.tools/s6g-evidence/` -- the S6 measurement on the baseline (configuration `CA1502: 12`, judged against 14) -- before; the hot-path measurement of the S6g reference scene on the baseline; the undefined-value site list written from the baseline.
2. Coordinate, input, target and value-source files -- their tables -- G1 to G4.
3. `CuriousContraptions.tests/MechanicalKindRouteTests.cs` -- the tests in the table; G5.
4. The S6 inventory commands (S6b Code Map) on the candidate -- G6; the run log lists every kept single-member comparison with file:line and each in-scope enum with its route test.
5. `.tools/s6g-evidence/`, planning records -- after-measurements; S6 closed with every chain listed and its test named.

**Acceptance Criteria:**
- AC1: Given the measurement, when it runs on the candidate, then no changed or added member reports CA1502 above its baseline value, and the measurement's before and after values for every member this slice changes are in the run log.
- AC2: Given the maintainability gate, when it runs, then both canaries pass and violations are 0.
- AC3: Given the full local round, when it completes, then failures are 0 and skipped are 0 in every project, and the total is the baseline round plus the cases `MechanicalKindRouteTests` adds: G1 2, G2 3, G3 6, G4 32 (4 sources times the 8 readers `MechanicalValue.Evaluate`, `TryRange`, `Value`, `DriveValue`, `ValidRatio`, `ValidCloseSeconds`, `ValidTravel` and `Matches`; a source a reader refuses by design asserts that refusal), and G5 one per site that refuses an undefined value. G5 cannot be counted yet: whether a site refuses is a property of its code that task 1 reads from the baseline (at most the 31 owned sites), and the run log states the exact total then.
- AC4: Given the S6 inventory commands and the member scan defined in S6b's Code Map, when they run on the candidate, then the only members they list are table rows and the tables' own lookups, plus the single-member comparisons the run log lists with file:line; the run log lists each in-scope enum of S6c to S6g with its route test; no enum prints that is in neither list of the inventory.
- AC5: Given the hot-path rule of the inventory (S6b), when the S6g reference scene is measured on the baseline and the candidate, then the candidate's median is not above the slowest baseline run. Reference scene: 32 Fans (each with a store, an electrical and a coordinate input in turn) powered for 1,000 ticks at 120 Hz through `WorkshopPhysicsEngine.Advance`, which runs `CommitContinuousWork` and the coordinate lookups each tick.

## Spec Change Log

## Review Triage Log

## Design Notes

1. Behaviour and scope: no behaviour change; mechanical routing becomes data and S6 closes (owner decision 24h; coordinator decision 42).
2. Reusable primitive: one row per kind; `ENGINE-JOINT-KINDS` and `ENGINE-SUPPLY-DEMAND` add rows, not branches.
3. Declared composition: packages declare coordinates, inputs, targets and values; tables key generic enums only.
4. Execution and ownership: same physics order; immutable static tables; the payment row is a static function.
5. Generality: G1 to G4 are theories over every value on real delivered parts; an added value without a row fails.
6. Forward-only, deleted: every listed switch and ladder; nothing kept beside the tables.
7. Prerequisites: S6f. E1, E2, E5, E6, E7, E8, E9 and D4, D5 apply; E3, E4, D6, D7 unaffected.

## Verification

Executor: implementation owner. Environment: `PATH=~/.local/share/tim-toolchain/dotnet-10.0.403:~/.local/share/tim-toolchain/node-v26.12.1-darwin-arm64/bin:$PATH`, `DOTNET_ROOT=~/.local/share/tim-toolchain/dotnet-10.0.403`. The measurement runs before the compiler-log captures.

**Commands:**
- The S6 measurement with `CA1502: 12` in its configuration, judged against 14 -- expected before: the bracketed values; after: AC1.
- The S6 inventory commands (S6b Code Map) -- expected: AC4.
- The five compiler-log capture commands -- expected: every build succeeds. Trimmed and ahead-of-time safety is by design and review; CI publish is the first compile of the trimmed worker, recorded unverified locally.
- `dotnet run --file tools/anvil/VerifyPolicy.cs -- --maintainability --project-root .` -- expected: AC2.
- The registration check filter (a test file is added) -- expected: 0 failed, 0 skipped.
- `dotnet test CuriousContraptions.slnx -c Debug -f net10.0 --no-build --no-restore --nologo -noAutoResponse --filter "FullyQualifiedName~MechanicalKindRouteTests|FullyQualifiedName~MechanicalCoordinateBindingsTests|FullyQualifiedName~CoordinateTravelGeneralityTests|FullyQualifiedName~ClutchTests|FullyQualifiedName~Airflow|FullyQualifiedName~FanIntegrationTests|FullyQualifiedName~ContactWorkInstanceTests|FullyQualifiedName~InteractionTargetTests|FullyQualifiedName~BundleValidatorTests|FullyQualifiedName~WoundSpringContractTests|FullyQualifiedName~MechanicalProductAdmissionTests"` -- expected: 0 failed, 0 skipped.
- `git add -N` on new files, then `git diff --check <baseline_revision>` -- expected: clean.
- One full local round -- expected: AC3.

**Manual checks (if no CLI):**
- Read the owned diff: every contact and geometry function body is unchanged (moves only); the kept single-member comparisons are the ones listed.

### Acceptance-to-test table

No tolerance is chosen: assertions are kinds, counts, selected bodies, decoded fields and refusals. Mechanical value ranges are compared exactly because they are computed from declared f32 constants and parameter bounds (canonical declared values).

| Rows | Named discriminating test | Control and boundary | Execution |
|---|---|---|---|
| G1 | `MechanicalKindRouteTests.EveryCoordinateKindRoundTripsAndBinds` | Control: a coordinate naming a missing joint refused | planned |
| G2 | `MechanicalKindRouteTests.EveryContinuousWorkInputPaysItsSource` | Control: the input absent, no flow | planned |
| G3 | `MechanicalKindRouteTests.EveryBodyTargetAndSelectorSelectsItsBodies` | Boundary: `AllPayloads` with no payload body selects none | planned |
| G4 | `MechanicalKindRouteTests.EveryValueSourceResolvesAndRanges` | Boundary: `NegatedParameter` at the parameter's minimum | planned |
| G5 | `MechanicalKindRouteTests.UndefinedValuesAreRefusedAtEverySite` (theory over the site list written from the baseline) | Boundary: one past each last defined value | planned |
| G6, AC1 to AC5 | The inventory commands, measurement, gate, full round, hot-path comparison | Baseline values and the five runs in the run log | planned |
