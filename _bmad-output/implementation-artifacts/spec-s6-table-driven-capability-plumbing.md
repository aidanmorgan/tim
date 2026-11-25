---
title: 'Table-driven committed presentation query (S6, first slice)'
type: 'refactor'
created: '2026-11-21'
status: 'draft'
baseline_revision: 'captured at dispatch (the main that contains the planning work)'
review_loop_iteration: 0
followup_review_recommended: false
context:
  - '{project-root}/docs/testing-guidance.md'
  - '{project-root}/_bmad/custom/element-free-engine-design.md'
  - '{project-root}/_bmad/custom/element-free-engine-review.md'
  - '{project-root}/_bmad/custom/project-engineering-contract.md'
warnings: []
deferred: []
---

<intent-contract>

## Intent

**Problem:** `WorkshopDeclaredPresentation.Query` (`engine/gpu/WorkshopDeclaredPresentation.cs:373`) selects the committed quantity for a presentation binding with one `switch` case per `PresentationDriver`. It has cyclomatic complexity 22 against the gate's limit of 25, and every capability that adds a committed driver adds a case, so the fourth new driver fails the maintainability gate inside an unrelated slice. No test proves that every driver value has a route.

**Approach:** Replace the `switch` with one static table keyed by the closed `PresentationDriver` enum, one row per continuous committed driver. Each row is a static function of the track and the committed read: the existing query function, or for the gas radius the inline case moved unchanged into its own static function, or for the transmission closure a static lambda returning `QueryTransmission(track, read).Closure`. Behaviour is preserved exactly; the proof is the existing presentation tests, one per driver. Add one behavioural test that every continuous driver value has a route, so this chain meets the S6 story's closing condition (owner decision 24h, 23 November 2026).

## Boundaries & Constraints

**Always:**
- Every continuous committed driver presents the same value, from the same committed read set, with the same refusal type and message as before.
- A driver with no row, including an undefined enum value, is refused with the existing `ArgumentException("Unsupported committed presentation driver.")`. A table lookup that surfaces any other exception type for a missing row is a defect.
- The table is built once at type initialisation from static functions. The per-read path allocates nothing new and uses no reflection or runtime enum enumeration (the Animation worker is trimmed and ahead-of-time compiled).
- The table is keyed by the generic capability enum only. No element identity, package name or part kind appears in engine code.
- The route test runs a real compiled scene of a delivered part that declares each driver and the real presentation through their public constructors, takes its cases from the enum and the public `PresentationDriverRules`, finds the declaring part from the registered part definitions' presentation bindings (`PartDefinition.Presentation.Bindings`), and asserts behaviour, not the table's layout.

**Never:**
- A change to a query function's body, to `MapDiscreteState`, `StageRead`, `CommitRead`, the `PresentationDriver` enum, `PresentationDriverRules`, the bundle validator, the compiled scene, the wire schema or any element package.
- A table row for the event driver or for a discrete-state driver; a retained copy of the old `switch`; a second dispatch path; a default or fallback row; a test-only accessor on the production type; a test that reads the table or reflects into non-public members.
- Another per-capability chain (S6b–S6g) or the member-count limit (`ENGINE-CAPABILITY-INSTANCES`, owner decision 24h).
- A suppression, exclusion or threshold change in the maintainability gate. Mutants, browser runs, output hashes.

## I/O & Edge-Case Matrix

| Row | Input / State | Expected Output / Behavior | Error Handling |
|-----|--------------|---------------------------|----------------|
| M1 | A binding of each of the 19 continuous committed drivers and a committed read carrying its source quantity | The presented value follows that quantity under the binding's declared mapping and curve | No error expected |
| M2 | A wheel-angle binding whose source row is absent (existing test) | Construction is refused | `ArgumentException` with the existing absent-query message |
| M3 | Each `PresentationDriver` value except `Event` that `PresentationDriverRules.SupportsMapping(value, PresentationValueMapping.Identity)` admits, taken from the enum; for each, a delivered part whose declared presentation binds that driver, compiled into a real scene and given its committed read | The presentation constructs and presents a sample for that part's binding of that driver | A value that no delivered part declares fails the case; a missing route surfaces "Unsupported committed presentation driver." and fails |

</intent-contract>

## Code Map

Baseline: captured at dispatch (coordinator decision 38, 23 November 2026): `baseline_revision` is the published main that contains the planning work, and every scope guard and `git diff` here compares against it. Precondition (coordinator decision 37, 23 November 2026): the planning work is published first and the baseline is the main that contains it (Concurrency); the coordinator captures it there before first implementation. Drafted on `25df3cd5`, which is the drafting revision only, not a baseline. Every engine and test file cited below is unchanged between `8d1d68aa`, where it was measured, and `25df3cd5` (`git diff --stat 8d1d68aa 25df3cd5` lists none of them; lines re-read 23 November 2026). Review tier: **engine** (engine source). Delivery shape: one structural slice, one chain.

**Status: draft; runs before the engine programme** (owner decision 22: S6 is one of the structural stories allowed before it; owner decision 24h: its follow-ons S6b–S6g run with it, before the programme). The drafting Scoped Pass (auditor `ad184c2ef59113b82`, 21 November 2026) has no report on disk (completion ledger DC-55) and is not carried over.

Planning verdict: Fail (fourth audit, `docs/planning/elements/readiness/audits/audit-s6-4.md`); fixes applied 23 November 2026; re-audit required. The fourth audit raised F1 (M3 could not reach `Query`: the compiled scene refuses a re-targeted track; the intent row is corrected now, before planning freezes it), F2 (the division of chains and the Slices list against the planning document) and F3 (frontmatter baseline), all applied here, and N3 (this slice's place in the order), applied; N1 (inventory completeness) and N2 (S6c's enum count) are referrals to S6b/S6f and S6c. The re-audit can be limited to the M3 text. The third audit (`docs/planning/elements/readiness/audits/audit-s6-3.md`, Incomplete) found B1–B3 and N1–N6; all are applied here. Its owner questions are answered by owner decision 24h: S6b–S6g run before the programme (O-1), and S6 is restated as the per-capability enum chains (O-2), which also brings this slice's route test (M3) into scope.

**The S6 story, restated (owner decision 24h, 23 November 2026).** S6 closes when every per-capability enum chain (a dispatch keyed by a closed capability enum that grows one path per capability) is a table with a test that every enum value has a route. The limit on member size ("no listed member above 15") is no longer S6's; it moves to `ENGINE-CAPABILITY-INSTANCES`. S6 is delivered as this slice (`Query`) and S6b–S6g, all before the engine programme, divided as in the Slices table of `docs/planning/invest/delivery-batches.md` section S6 (inventory in S6b's Code Map); the story closes when S6g, the last, publishes.

Owned scope:
- `engine/gpu/WorkshopDeclaredPresentation.cs:373-414` -- `Query`: the `switch` becomes a table lookup. `:402-405` -- the inline gas-radius case moves to its own static function with the same message. No other member changes.
- `CuriousContraptions.tests/ComposableAnimationMappingTests.cs` -- one new theory (M3), beside `WorkshopDeclaredPresentation_Query_ThrowsOnMissingSource` (`:605`). No existing test changes.
- `docs/planning/invest/delivery-batches.md` (section "S6. Table-driven capability plumbing"), `_bmad-output/implementation-artifacts/deferred-work.md` (the "S6:" item under "Structural stories queued — 21 November 2026"), `TODO.md`, this contract and its run log.

Direct consumers: `Query` has one caller, `StageRead` (`engine/gpu/WorkshopDeclaredPresentation.cs:298`), unchanged; it skips `Event` tracks and sends `StateValues` tracks to `MapDiscreteState` (`:294-298`). The file is a `CoreRuntimeSource` (`CuriousContraptions.Core/RuntimeSources.props:124`) created through `CorePresentationServices.Create` (`CuriousContraptions.Core/CorePresentationServices.cs:13`); no signature changes. No table, schema, fixture, TypeScript, hook, tool or coverage row names `Query`.

### Measurement (21 November 2026, `8d1d68aa`; engine code is unchanged at `25df3cd5`, but the Baseball package, `tools/Coverage/` and `tools/anvil/` changed, so task 1 re-measures at the baseline)

The gate prints only violations, so the solution was built once with the gate's own analyzer injection and CA1502 lowered to 14, into a scratch artifacts path (commands in Verification). Result: 0 members above 25; **81 production members at 22 or above** (14 at 25, 21 at 24, 22 at 23, 24 at 22); 120 at 20 or above; 284 at 15 or above. The story's recorded values are stale: `Query` is 22 (recorded 28), `BundleValidator.PresentationBindingCapabilities` is 20 (recorded 27), `BundleSchema.Entry` is below 15. The playtest variant, the web host and the two file-based tools were not measured.

`Query` is the member whose chain is keyed by a closed capability enum and grows by one path per capability; most other members at the limit are sequential validators and codecs whose repair is extraction, not a table, and their size is `ENGINE-CAPABILITY-INSTANCES`' (owner decision 24h). This slice: `Query` **22 before; expected about 2 after, accepted at 14 or below**; no member this slice adds reports at threshold 14; `WorkshopDeclaredPresentation..ctor` (19) and `AcceptEvents` (19) are untouched and stay as measured.

### Capability probe

All capabilities exist; nothing in a solver law, the worker ABI, the wire schema or a shared presentation contract changes. Each test below was opened in planning and asserts a presented value (a sample, or the native property the sample drives) produced by the real `WorkshopDeclaredPresentation`.

- **Present:** `CommittedWheelAngle`, `CommittedSurfaceDeflection`, `CommittedOscillatorPhase` -- `ComposableAnimationMappingTests.WorkshopDeclaredPresentation_OneCurveWithDifferentSources_ExponentialFollow` (`:379`).
- **Present:** `CommittedSupply`, `CommittedCharge` -- `DeclaredPresentationTests.BatterySupplyAndEachChargeBoundaryUseActualCommittedRead` (`:490`; terminal and charge-mark colours).
- **Present:** `CommittedOscillatorPulse`, `CommittedCount` -- `DeclaredPresentationTests.ClockTicksDrivePendulumPulseAndCounterLamps` (`:237`; clock indicator and counter dot lamps).
- **Present:** `CommittedActivation` -- `DeclaredPresentationTests.CommittedLatchAndLampPreserveBothDirectionsAndDelayPhases` (`:415`; rocker scalar and lamp colours).
- **Present:** `CommittedTimerProgress` -- `ComposableSignalNodesTests.OnlyTheSelectedTimerChangesItsEventAndVisual` (`:325`; the selected binding equals 3 ticks of its duration, the other stays 0).
- **Present:** `CommittedTimerRemaining`, `CommittedTimerActive` -- `HoldTimerPresentationTests.ActualCommittedWindowDrivesAnchoredBarAndIndependentLampAndHiddenCatchUp` (`HoldTimerTests.cs:470`).
- **Present:** `CommittedElectricalSocket` -- `ElectricalNandPresentationTests.PictogramTruthDotsAndThreeLampsFollowCommittedRowsThroughReset` (`ElectricalNandTests.cs:345`), `BothGatePresentationTests.ThreeNativeLampsFollowCommittedInputsAndSupplyThroughHideAndReset` (`BothGateTests.cs:259`).
- **Present:** `CommittedActuatorDrive` -- `ClutchTests.ChildSupportedDriveAcceptsOwnedTargetChangesAndRefusesForeignRows` (`ClutchGeneralityTests.cs:511`; both powered samples equal 1).
- **Present:** `CommittedStoredFraction` -- `OwnedFiniteWorkTransactionTests.StoredFractionAndAvailabilityUsePreloadAndSameTimeCommittedState` (`:400-460`).
- **Present:** `CommittedRevoluteAngle` -- `ClutchTests.DifferentHingeSupportsPreserveProvenanceAndReadTheSelectedChildCoordinate` (`ClutchGeneralityTests.cs:214-215`; the sample equals the selected child coordinate and differs from the first) and `FanIntegrationTests.MotorLinkUsesDeclaredInputSenseAndActualShaftTravel` (`:575-577`).
- **Present:** `CommittedFlowSpeed` -- `FanIntegrationTests.CommittedFlowPhaseHoldsOnPauseStepsOnceAndResetsWithoutUnexposedDebit` (`:591-594`).
- **Present:** `CommittedGasRadius` -- `BalloonTests.RupturePrecedesTimedPartialLossAndPauseStepResumeResetOwnTheOpenStore` (`:221-224`).
- **Present:** `CommittedTransmissionClosure` -- `ClutchArtTests.ClutchScene_AnimatesPlatesLampAndShaftSpokesAcrossLifecycleAndResetsCleanly` (`ClutchTests.cs:1450`; plate positions across the lifecycle).
- **Present:** `CommittedRetentionReleased` -- `WoundSpringLifecycleTests.LatchTracksEightRadiansPerSimulationSecondAcrossSkippedFramesAndReversal` (`:56-88`).
- **Present:** missing-source refusal -- `ComposableAnimationMappingTests.WorkshopDeclaredPresentation_Query_ThrowsOnMissingSource` (`:605`).
- **Present:** the six discrete drivers through `MapDiscreteState` (`engine/gpu/WorkshopDeclaredPresentation.cs:588-600`) -- `WorkshopDeclaredPresentation_Discrete3State_AllThreeStatesAndTransitions`, `WorkshopDeclaredPresentation_UnifiedStateValues_AcrossActuatorContactAndTimer`. Unchanged by this slice.
- **Present:** compiling a scene of delivered parts and constructing the presentation from its committed read -- `ComposableAnimationMappingTests.WorkshopDeclaredPresentation_OneCurveWithDifferentSources_ExponentialFollow` (`:379-404`). Re-targeting a compiled track's driver is not a capability: the compiled scene refuses a track whose driver-specific source fields do not match its driver (`engine/gpu/WorkshopCompiledScene.cs:223-307`).
- The unsupported-driver refusal (`:412`) is unreachable through `WorkshopCompiledScene` for an undefined value (refused at `:326`) or a discrete driver with a non-`StateValues` mapping (`:335`), each as "Compiled presentation track is invalid."; that invariant is verified by AC3. M3 executes the other side: every admitted continuous value reaches a row.

### Decisions

- **One slice per chain (decided): this slice is the `Query` chain.** The other chains follow as S6b–S6g (owner decisions O1 and 24h).
- **Scope of the table (decided): the 19 continuous drivers only.** The event driver has no committed query and the discrete drivers are selected by `Mapping == StateValues` in `StageRead`, not by driver; a row for either would change which refusal an invalid combination produces.
- **Proof (decided): existing tests per driver, plus one behavioural route theory (M3).** A row wired to the wrong function fails that driver's cited test, because each asserts a value only its own source produces. The route theory compiles a delivered part that declares each driver; it neither reads the table (`docs/testing-guidance.md:69`) nor adds a test double (`:31`).
- **Table type (decided):** a frozen dictionary from `PresentationDriver` to `Func<Track, WorkshopRead, float>`, built from `static` lambdas or method groups, read with `TryGetValue`; the miss branch throws `ArgumentException("Unsupported committed presentation driver.")`. `Track` is a private sealed class, so static lambdas are cached and the read allocates nothing.

### Owner decisions

- **O1 (21 November 2026), the size of S6: all capability plumbing.** S6 is not narrowed; S6b (the wire response codec) is first of the follow-ons.
- **24h (23 November 2026).** S6b–S6g run with S6, before the engine programme. S6 closes when every per-capability enum chain is a table with a test that every enum value has a route; the member-count limit moves to `ENGINE-CAPABILITY-INSTANCES`.

### Concurrency

Prerequisite (coordinator decision 37, 23 November 2026): the planning work of 22–23 November 2026 (the `docs/planning/elements/readiness/` folder, `docs/planning/invest/delivery-batches.md` section 0 and its S6 **When** line, and the sprint-status note on owner decision 22) is published before this slice starts. The coordinator confirms it is on the baseline and re-reads task 5's anchors there. Runs after S5b and before S6b (`delivery-batches.md` section 0).

Conflicts with any in-flight slice that adds a `PresentationDriver` (it edits the same `switch`); that slice publishes first and this one is replanned from the published revision with its driver added to M1. No slice allowed before the engine programme adds one (owner decisions 22 and 24h: visible-solid parts A, B, B2 and C, the Baseball repair, S3, S5 and S5b; under owner decision 24m, the lane 3 lettered level ids slice, which edits only `tools/Ownership`, `tools/Coverage`, `tools/Coverage.Tests` and documents, and the lane 4 policy-tool fixes, which edit only `tools/anvil/` and documents). The lane 3 content slice has no contract yet; its planning confirms whether it adds one, and if it does the rule above applies. S6b–S6g follow this slice, each from the published main of the one before. Shares no edited file with them except `deferred-work.md`, `TODO.md` and `delivery-batches.md`; one publisher at a time, and the later publisher merges main once and repeats the full local round. Part C cites this file's lines as evidence (`spec-elements-visible-solid-remainder-c.md`, capability probe: `:89-155`, `:402-405`, `:637-649`); whichever of S6 and part C publishes second re-reads those citations on the merged main. `docs/planning/elements/readiness/batch-o.md:46` cites `:492-502` and is re-read the same way. Does not touch the test project file, `content/puzzles.json`, coverage or impact tables.

## Tasks & Acceptance

**Execution:**
1. `.tools/s6-evidence/` -- run the measurement in Verification on the baseline and record `Query`'s value and the file's other members in the run log -- the "before" number. If the measurement shows a per-capability enum chain that the S6 Inventory (S6b's Code Map) neither assigns nor excludes, record it in the run log for the coordinator; do not edit the inventory or the Slices table.
2. `engine/gpu/WorkshopDeclaredPresentation.cs` -- replace the `switch` in `Query` with the table of the decision above and its lookup; move the gas-radius case into its own static function; the transmission-closure row is a static lambda in the table initialiser -- the refactor.
3. `CuriousContraptions.tests/ComposableAnimationMappingTests.cs` -- add the M3 theory -- the route test.
4. `.tools/s6-evidence/` -- repeat the measurement and record the "after" numbers.
5. `docs/planning/invest/delivery-batches.md` S6 -- the planning work restates Outcome and Acceptance per owner decision 24h (every per-capability enum chain becomes a table with a test that every enum value has a route; no "member above 15", no "same totals"); read them on the baseline and edit only if they are not so restated. Keep the existing Closure, Inventory and Slices table unchanged, unless task 1 recorded an unassigned chain; in that case the coordinator amends the S6b inventory before this slice's review. `deferred-work.md` -- under the "S6:" item, one line: the first slice's chain is delivered by this candidate; S6b–S6g remain, per the Slices table. `TODO.md` -- what was verified before the commit, the remaining follow-ons, and nothing about this commit's approval or publication -- current documents.

**Acceptance Criteria:**
- AC1: Given the candidate, when the measurement in Verification runs, then `Query` and every member this slice adds report no CA1502 warning at threshold 14.
- AC2: Given the candidate, when the maintainability gate runs, then both canaries pass and metric and coupling violations are 0.
- AC3: Given the candidate diff of `engine/gpu/WorkshopDeclaredPresentation.cs`, when it is read, then the only changed members are `Query`, the new table and the extracted gas-radius function (the transmission-closure row is a static lambda inside the table initialiser, not a new member), and each continuous driver appears in exactly one table row, and the lookup's miss branch throws `ArgumentException("Unsupported committed presentation driver.")`; no indexer or cast-indexed array is used, so no other exception type can escape for a missing or undefined driver.
- AC4: Given the full local round, when it completes, then failures are 0 and skipped is 0 in every project, and the total equals the current full local round at the baseline + 19 (the M3 theory's cases: the 25 committed values less the six discrete ones; re-measured at start; main's at drafting: 7,294, so 7,313 if unchanged).

## Spec Change Log

## Review Triage Log

## Design Notes

1. Behaviour and scope: no behaviour change; the dispatch from committed driver to committed quantity becomes data, with a route test (story S6 as restated by owner decision 24h; E9 maintainability).
2. Reusable primitive: one table of existing query functions; a new continuous committed driver adds one row and one function, and enters the route theory without a test edit.
3. Declared composition: bindings still declare their driver; the table maps the generic enum, never a package (element-free design guide).
4. Execution and ownership: same worker, same staged-read path, no new state; the table is immutable static data.
5. Generality: a continuous driver without a row fails M3 with the unsupported-driver message and its own cited presentation test.
6. Forward-only, deleted: the `switch` in `Query` and its inline gas-radius case. Nothing is kept beside the table.
7. Prerequisites: the published planning work (coordinator decision 37); runs before the engine programme (owner decisions 22 and 24h). Uncertain: whether every declaring part compiles alone (M3 falls back to the driver's cited M1 scene). E2, E5, E7, E8, E9 and D1 to D5 apply; D7 applies and is preserved (source, mapping and evaluator stay separately declared); E1, E3, E4, E6 and D6 are unaffected.

## Verification

Executor: implementation owner, pinned environment of `docs/delivery-workflow.md#reproducible-local-verification`. The measurement rebuilds the root game project in place, so run it before the compiler-log captures, never between a capture and a test run that counts as evidence.

Environment: the pinned toolchain first on `PATH`: `PATH=$HOME/.local/share/tim-toolchain/dotnet-10.0.403:$HOME/.local/share/tim-toolchain/node-v26.12.1-darwin-arm64/bin:$PATH` and `DOTNET_ROOT=$HOME/.local/share/tim-toolchain/dotnet-10.0.403` (on this host the default `PATH` has Homebrew node v26.13.1 and a dotnet without `wasm-tools`, so the capture's node check and the web build fail otherwise); record the session environment check in the run log.

**Commands:**
- Measurement. Write three files under `.tools/s6-evidence/`: `metrics.globalconfig` (`is_global = true`, `dotnet_diagnostic.CA1502.severity = warning`, `dotnet_diagnostic.CA1505.severity = warning`), `CodeMetricsConfig.txt` (`CA1505: 20`, `CA1502: 14`) and `metrics.targets` (adds `CA1502;CA1505` to `WarningsNotAsErrors` and, where `IsTestProject` is not `true`, the first file as `EditorConfigFiles` and the second as `AdditionalFiles`). Then `dotnet build CuriousContraptions.slnx -noAutoResponse --artifacts-path <temporary folder> -nologo -v q --no-incremental -p:CustomAfterMicrosoftCommonTargets=<absolute path of metrics.targets> > .tools/s6-evidence/before.log 2>&1` (and `after.log` for task 4), then `rg "WorkshopDeclaredPresentation.cs.*CA1502" .tools/s6-evidence/before.log` -- expected before: `'Query' has a cyclomatic complexity of '22'`; after: no `Query` line and no line for a member this slice added. The file is compiled into three projects, so the same member can appear up to three times.
- The five compiler-log capture commands from `docs/delivery-workflow.md` -- expected: every build succeeds.
- `dotnet run --file tools/anvil/VerifyPolicy.cs -- --maintainability --project-root .` -- expected: both canary `PASS` lines, the `Coupling dependencies:` summary, `violations=0`.
- `dotnet test CuriousContraptions.slnx -c Debug -f net10.0 --no-build --no-restore --nologo -noAutoResponse --filter "FullyQualifiedName~ComposableAnimationMappingTests|FullyQualifiedName~DeclaredPresentationTests|FullyQualifiedName~ComposableSignalNodesTests|FullyQualifiedName~HoldTimerPresentationTests|FullyQualifiedName~ElectricalNandPresentationTests|FullyQualifiedName~BothGatePresentationTests|FullyQualifiedName~OwnedFiniteWorkTransactionTests|FullyQualifiedName~BalloonTests|FullyQualifiedName~ClutchTests|FullyQualifiedName~ClutchArtTests|FullyQualifiedName~FanIntegrationTests|FullyQualifiedName~WoundSpringLifecycleTests"` -- expected: 0 failed, 0 skipped on the candidate, with the baseline's total + 19. `ClutchTests` is the partial class that `ClutchGeneralityTests.cs` extends.
- `git add -N _bmad-output/implementation-artifacts/spec-s6-table-driven-capability-plumbing.md _bmad-output/implementation-artifacts/run-logs/spec-s6-table-driven-capability-plumbing.md`, then `git diff --check <baseline_revision>` -- expected: clean. The registration check is not required: no package, port, parameter, wire field, test file or tool is added or renamed (the theory goes into an existing test file).
- One full local round -- expected: AC4.

**Manual checks (if no CLI):**
- AC3: read the owned diff of the engine file against the baseline, including the lookup's miss branch.

### Acceptance-to-test table

No tolerance is chosen by this slice: M1 relies on the cited tests with their own tolerances; M3 asserts an exception type and message.

| Rows | Named discriminating test | Control and boundary | Execution |
|---|---|---|---|
| M1 | Per driver, the existing test cited in the capability probe | Each cited test's own control and boundary, unchanged | planned |
| M2 | `ComposableAnimationMappingTests.WorkshopDeclaredPresentation_Query_ThrowsOnMissingSource` (existing, wheel angle) | Control: the same scene with its guide row present constructs (`WorkshopDeclaredPresentation_OneCurveWithDifferentSources_ExponentialFollow`) | planned |
| M3 | `ComposableAnimationMappingTests.EveryContinuousCommittedDriverHasACommittedQueryRoute` (new theory). Cases: `Enum.GetValues<PresentationDriver>()` minus `Event`, filtered by `PresentationDriverRules.SupportsMapping(value, PresentationValueMapping.Identity)` (19 at the baseline). Per case: the first registered part definition (`PartRegistry.Definitions`, ordered by `PartId`) with a presentation binding of that driver (at the baseline one of Cannon, Signal lamp or State latch, Counter, System clock, Delay box, Hold timer, an electrical gate, Battery, Clutch or Windmill, Motor, Pulley, Fan, Balloon, Trampoline, Wound spring); `Assert.NotNull` on it, so a driver no part declares fails; one instance placed with `WorkshopInput.Generic`, compiled with `CompiledSceneFixture.Compile`, one read from `WorkshopIntegrationHarness.Observe`; `new WorkshopDeclaredPresentation(...)` constructs without throwing (construction stages the read through `Query`, `WorkshopDeclaredPresentation.cs:154`, `:282-298`) and `Advance` returns a sample for that part's binding of that driver. If a declaring part cannot compile alone, its case uses the scene of that driver's cited M1 test; no case is dropped and no track field is edited. | Control by reading: a driver without a row throws 'Unsupported committed presentation driver.' at construction and fails its case. Boundary: continuous routes only; the six `StateValues` drivers and `Event` are S6c's, owned by S6c with its every-value test (`delivery-batches.md` S6 Slices table). If a case fails on the baseline for any reason other than a missing route, return to planning with the driver, the declaring part and the exception; neither the assertion nor a track field is changed to make it pass | planned |
| AC1, AC2 | The measurement and the maintainability gate | Before and after values in the run log | planned |
| AC3 | Manual reading of the owned diff | The undefined-value refusal has no executed test (unreachable through real admission) | planned |
| AC4 | The filtered run and the full local round | Totals equal the baseline round + 19 | planned |
