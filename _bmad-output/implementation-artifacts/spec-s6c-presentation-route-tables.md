---
title: 'Table-driven presentation routes: drivers, properties and curves (S6c)'
type: 'refactor'
created: '2026-11-23'
status: 'draft'
baseline_revision: 'captured at dispatch (the main that contains the planning work)'
review_loop_iteration: 0
followup_review_recommended: false
context:
  - '{project-root}/docs/testing-guidance.md'
  - '{project-root}/_bmad/custom/element-free-engine-design.md'
  - '{project-root}/_bmad/custom/element-free-engine-review.md'
  - '{project-root}/_bmad-output/implementation-artifacts/spec-s6-table-driven-capability-plumbing.md'
  - '{project-root}/_bmad-output/implementation-artifacts/spec-s6b-response-codec-read-family-table.md'
warnings: []
deferred: []
---

<intent-contract>

## Intent

**Problem:** A presentation capability is classified by hand in many places. `PresentationDriverRules` (`CuriousContraptions.Abstractions/ElementPresentation.cs:135-310`) answers "is this driver discrete, continuous, a level, a signal source, which curves and mappings does it take" through separate `is X or Y` ladders; the two admission sites repeat the same per-driver rules twice, as payload rules (`BundleValidator.PresentationBindingValues` `:205` [22], `WorkshopCompiledScene.InvalidBindingValues` `:341` [24]) and as mirrored source-reference ladders (the nine `PresentationBinding*` members of `BundleValidator.cs:192-359`, the nine `Invalid*Binding` members of `WorkshopCompiledScene.cs:231-339`: each says `driver == X` requires reference Y and that Y exists); `MapDiscreteState` (`engine/gpu/WorkshopDeclaredPresentation.cs:588`) switches on discrete drivers; and each presentation property and curve is translated by its own switch in four files. A new driver, property or curve touches all of them, and the two admission sites can drift apart.

**Approach:** Tables keyed by the closed presentation enums, each in the layer that reads it. Abstractions: a driver table gives each `PresentationDriver` exactly one route class (event, continuous, discrete) and its classification columns, payload kind and the source references it requires; both admission sites read it. The two sites keep their own existence checks (they read different types: the bundle payloads and the compiled scene) as one table per site keyed by the reference kind, so each site is one loop over the driver's references. Engine: the first S6 slice's continuous query table stays as it is, and `MapDiscreteState` becomes a second engine table keyed by the same driver enum. A property table gives each `PresentationProperty` its value domain and native visual property, and a curve table gives each `PresentationCurve` its animation curve or impulse curve; the evaluator's curve functions are rows keyed by `AnimationCurve` and `AnimationImpulseCurve`. Behaviour is unchanged.

## Boundaries & Constraints

**Always:**
- Every value of `PresentationDriver` (26), `PresentationProperty` (15), `PresentationCurve` (8), `PresentationValueMapping` (4), `AnimationProperty` (8), `AnimationCurve` (4), `AnimationImpulseCurve` (4) and `WorkshopVisualProperty` (15) has exactly one row in its table. A defined value that a rule does not route has an explicit row saying so (for example the five non-impulse presentation curves have no impulse curve, and `LocalX`, `LocalY`, `LocalZ` and `LocalRotationZ` are unconstrained properties).
- A value with no row (only an undefined value, since every defined value has a row) answers each rule as that rule answers it today: `SignalSource`, `ContinuousNormalized`, `Discrete3State`, `Discrete4State`, `ContinuousPhysical`, `LevelDriver`, `SupportsMapping` and `Supports` return false; `ImpulseCurve`, `ImpulseOverlap` and `Map` (and `PresentationStateValues.Map`) throw `ArgumentOutOfRangeException`; `AdmitsPropertyValue` returns false (today it returns true through its `_ => true` arm; unreachable, because both admission sites refuse an undefined property first, `BundleValidator.cs:196` and `WorkshopCompiledScene.cs:325`). Lookups use `TryGetValue`, never an indexer or a cast-indexed array, so no other exception type can escape.
- Bundle admission and compiled-scene admission accept and refuse exactly the bindings they do today, with the same `BundleError` names (`Presentation.StateValues`, `Presentation.Timing`, `Presentation.Value`, `Presentation.Socket`, `Presentation.GuidePort`, `Presentation.Airflow`, `Presentation.GasShape`, `Presentation.Integrity`, `Presentation.FiniteWork`, `Presentation.Store`, `Presentation.Retention`, `Presentation.Transmission`, `Presentation.Actuator`, `Presentation.DriverCurve` and the rest) and the same "Compiled presentation track is invalid." refusal, and they read the same driver rows. The one difference that stays as it is: the compiled site also requires a track's store when its finite work is selected (the store is derived at compile), which the bundle binding does not carry; it is that site's own existence function.
- Presented values over time are unchanged for every delivered package.
- Tables are built once from static data and functions; the per-frame path allocates nothing new and uses no reflection or runtime enum enumeration (trimmed, ahead-of-time Animation worker).

**Never:**
- A change to a query function body, a committed read, the wire schema, an element package, a binding's declared values, or an enum's members.
- A second classification path beside a table; a default or fallback row; a test-only accessor on a production type.
- The per-driver value computations inside the continuous query bodies (`WorkshopDeclaredPresentation.cs:446-499`: the count, oscillator pulse, timer and supply branches), which the first S6 slice's Never freezes, and the `CoordinateKind` switch at `BundleValidator.cs:330` inside `PresentationBindingActuator`, which is S6g's.
- Suppressions, gate threshold changes, hashes, golden output pins, mutants, browser runs.

## I/O & Edge-Case Matrix

| Row | Input / State | Expected Output / Behavior | Error Handling |
|-----|--------------|---------------------------|----------------|
| P1 | For each `PresentationDriver` value from `Enum.GetValues`, the first registered part definition (`PartRegistry.Definitions`, ordered by `PartId`) whose declared presentation binds that driver (`Assert.NotNull`: all 26 are declared by at least one delivered part at the drafting revision); its `BundleEntry` through `BundleValidator.Validate` and a real compiled scene of that part through `RuntimeTestServices.Core.CreateScene` over its unmodified tracks | Admitted by both sites; the driver's route class (event, continuous or discrete) is the one the test writes out from the enum's definition: `Event`, the six drivers `SupportsMapping(value, StateValues)` admits, the other 19 continuous | No error expected |
| P2 | For each driver and each of the four payload members (`StateValues`, `FourStateValues`, `FiniteWorkValues`, `PrismaticValues`), the real binding with that one member's presence toggled (set where the driver takes none, cleared where it takes it); the compiled site is fed the same change on the track's declaration (`tracks[i] with { Declaration = declaration with { ... } }`, the pattern of `CompiledSceneBoundaryTests.RefusesImpulseDeclaration`, `:130`) | Refused by both sites | Bundle: the error named `Presentation.StateValues` (`Assert.Contains` on the error detail); compiled: `ArgumentException` "Compiled presentation track is invalid."; control: the unmodified binding and track (P1) |
| P3 | For each of `PresentationDriver`, `PresentationProperty`, `PresentationCurve`, `PresentationOverlap` and `PresentationValueMapping`, the real binding with the value one past the last defined; and the public rules called directly with an undefined value | Refused by both sites at their existing guards (`Defined(...)`, `Enum.IsDefined`, `SupportsMapping`); the public rules answer as the second Always bullet says | Bundle: the named error; compiled: the one message |
| P4 | For each `PresentationProperty`, one real compiled continuous track (the Pulley's wheel angle, `elements/Pulley/PulleyElementDeclaration.cs:15`: driver `CommittedWheelAngle`, curve Linear, duration 0) with `Property` re-targeted and `From` and `To` set to admitted values (0.5 and 1.5 for the four scale properties, 0 and 1 for the others, 0 and 1 for `Visible`), presented by the real `WorkshopDeclaredPresentation` | The sample's animation property is the one an independent table in the test names for that presentation property, and its value follows the committed wheel angle through the declared mapping; where the part's node has the native property (translation, rotation, scale) the node changes by the mapped kind and the node's other properties do not change; for colour, emission and visibility the case runs on the first registered part whose declared node supports the property, and `LocalZ`, which no delivered part declares, is asserted at the sample only and recorded | No error expected |
| P5 | For each `PresentationCurve` (8), a real compiled track whose driver admits it (`PresentationDriverRules.Supports`), with the curve re-targeted: level-driver track with duration 0.5 s (the Battery supply) for `Linear`, `SmoothStep`, `CubicSmoothStep` (no delivered part declares `CubicSmoothStep`); for the impulse curves the driver each admits: the clock's pulse binding (`CommittedOscillatorPulse`, which admits only `LinearDecay`) for `LinearDecay`, the Bumper's event binding (`Visual/BumperHead`, driver `Event`) for `SineSquaredPulse`, the Cannon's event binding (`Visual/Sleeve`, driver `Event`) for `SmoothRiseFall`; a continuous physical track (the Pulley's, with `Response` 1 for `ExponentialFollow` and 0 otherwise) for `ExponentialFollow` and `IntegratedRate`; `AnimationImpulseCurve.SmoothDecay`, which no presentation curve selects, keeps its row and its existing test (`PortableAnimationTests`) | At t = 0, 0.5 and 1 of the duration the sample equals the curve's defining value: `Linear` t; `SmoothStep` the quintic 6t^5 - 15t^4 + 10t^3 (`AnimationCurve.SmoothStep`, `AnimationEvaluation.cs:21-28`); `CubicSmoothStep` 3t^2 - 2t^3 (`AnimationCurve.CubicSmoothStep`); `LinearDecay` 1 - t; `SineSquaredPulse` sin^2(pi t); `SmoothRiseFall` the quintic rise to the declared peak and fall after it (`AnimationEvaluation.cs:36-43`); `ExponentialFollow` (no duration): after one response time the value has covered 1 - 1/e of a step; `IntegratedRate`: after t seconds at constant rate r the angle is r t | No error expected |
| P6 | A curve a driver does not take (for example `SineSquaredPulse` on the Pulley's continuous physical track, `CubicSmoothStep` on a discrete driver) | Refused | Bundle: `Presentation.DriverCurve`; compiled: "Compiled presentation track is invalid."; control: the same curve on an admitting driver is admitted (P5) |
| P7 | For each of the six discrete drivers (taken from the enum with `SupportsMapping(value, StateValues)`), the first delivered part declaring it, compiled and presented | The presented value for each committed state equals that state's declared value (`MapDiscreteState`'s row) | No error expected |

</intent-contract>

## Code Map

Baseline: captured at dispatch (coordinator decision 38, 23 November 2026): the published main that contains the planning work and the slices before this one; scope guards compare against it. Drafted on `25df3cd5`, the drafting revision only, not a baseline; every line cited here was read there on 24 November 2026. **Status: draft; runs before the engine programme** (owner decision 24h, 23 November 2026). Prerequisites: S6b published, and the first S6 slice (its continuous query table stays as it is here). Review tier: **engine**. Delivery shape: one structural slice, the presentation rows of the S6 follow-on inventory (`spec-s6b-response-codec-read-family-table.md`, "S6 follow-on inventory"); the fifth audit judged it large (about 15 production files, eight enums, two sub-systems) and left the split to the coordinator, who keeps it one slice; if the first review pass finds the diff too large to review as one candidate, the split is at the driver tables and admission sites (P1 to P3, P7) versus the property and curve tables (P4 to P6), with the same acceptance.

Planning verdict: re-audit required. The fifth audit (`docs/planning/elements/readiness/audits/audit-s6c-5.md`, Fail) raised B1 to B7 and N1 to N7; all are applied here on 24 November 2026: B1 (source-reference ladders now owned), B2 (P1 to P3 on real parts and the established pattern for the compiled site), B3 (two layers, not one table), B4 (miss behaviour per rule), B5 and B6 (P4 to P6 executable, with the evaluator's curve names read from the code: the audit's formulas for `SmoothStep` and `CubicSmoothStep` were swapped), B7 (concurrency).

Owned scope (sites from the inventory; measured complexity at threshold 12 on 23 November 2026 in brackets):
- `CuriousContraptions.Abstractions/ElementPresentation.cs` -- `PresentationDriverRules` (`:135`): `SignalSource` `:137`, `ContinuousNormalized` `:143`, `Discrete3State` `:147`, `Discrete4State` `:152`, `ContinuousPhysical` `:154`, `LevelDriver` `:164`, `SupportsMapping` `:174`, `Map` `:201-210`, `AdmitsPropertyValue` `:212` (11-arm switch), `IsImpulseCurve` `:251`, `ImpulseCurve` `:254`, `ImpulseOverlap` `:262`, `Supports` `:277` [17], `:300` curve switch -- become reads of the driver, property and curve tables. The driver table is a `FrozenDictionary<PresentationDriver, DriverRow>`; its columns, each read by a site above: route class, signal source (always, never, or by event kind for `Event`), normalized, physical, level, the mapping set, the payload kind (none, three-state, four-state, prismatic phases, finite-work states), the source references it requires (a `[Flags]` set naming the binding fields Socket, Guide, Airflow, GasShape, Integrity, FiniteWork, Store, Retention, Transmission, Actuator, SupplyNode, SignalNode, Source; the `Event` row carries the two event-selected entries as today: `IntegrityBroken` requires Integrity, `FiniteWorkReleased` requires FiniteWork) and the signal node kinds it observes. An implementer adds a column only with a site that reads it.
- `CuriousContraptions.Abstractions.Signal/SignalObservationRules.cs:8-19` `Supports` -- the 10-arm driver switch reads the driver row's observed-node-kinds column (this slice owns `Supports`; `Emits` `:21-30` is S6d's).
- `CuriousContraptions.Bundle/BundleValidator.cs:192-359` -- `PresentationBindingIdentity :192`, `PresentationBindingValues :205` [22], `PresentationBindingSocket :245`, `PresentationBindingCapabilities :258` [20], `PresentationBindingAirflow :282`, `PresentationBindingGas :292`, `PresentationBindingFiniteWork :303`, `PresentationBindingRetention :315`, `PresentationBindingActuator :323`, `PresentationBindingEvents :343` -- the reference ladders become one loop over the driver's references with a bundle-side existence table keyed by reference kind (each row: the error name and the payload-member check); the payload rules read the driver row. The `CoordinateKind` switch inside `PresentationBindingActuator` (`:330`) is S6g's and is kept as it is. `CuriousContraptions.Bundle/SupplyBundleValidation.cs:183-190` -- the supply drivers' payload checks read the driver row.
- `engine/gpu/WorkshopCompiledScene.cs:231-356` -- `InvalidSupplyBinding :231`, `InvalidPhysicalBinding :248`, `InvalidAirflowBinding :251`, `InvalidGasBinding :255`, `InvalidFiniteWorkBinding :267`, `InvalidActuatorBinding :279`, `InvalidMechanicalObservationBinding :299`, `InvalidSignalBinding :313`, `InvalidBindingEvaluation :331`, `InvalidBindingValues :341` [24] -- the same loop with a compiled-side existence table keyed by reference kind. `:334` (the `SupplyTerminal` test inside `InvalidBindingEvaluation`) and the `SupplyConsumerKind` use at `:279` are S6e's and are kept as they are.
- `engine/gpu/WorkshopDeclaredPresentation.cs` -- `:588-600` `MapDiscreteState` becomes an engine table keyed by `PresentationDriver` (rows for the six discrete drivers); `:130-140` curve switch and `:736-746` property switch read the tables; the first S6 slice's continuous query table is unchanged and not a column of the driver table (its rows are functions over a private `Track` and cannot live in Abstractions).
- `engine/MachinePart.cs:282-300` (property to visual property, 15 arms), `engine/WorkshopVisualBinding.cs:24-35` and `:60-70` (visual property to value and colour channel), `engine/gpu/WorkshopHint.cs:125-140` (animation property to value, 8 arms), `CuriousContraptions.Abstractions/AnimationValues.cs:239`, `CuriousContraptions.Abstractions/AnimationDefinitions.cs:130`, `:138`, `engine/presentation/AnimationEvaluation.cs:21-30`, `:34-42`, `engine/presentation/AnimationBatch.cs:87` (`RegisterOscillation`, two `AnimationProperty` arms) -- property and curve rows, each keyed in the layer that already reads the enum.
- `CuriousContraptions.tests/PresentationRouteTests.cs` -- new: P1 to P7.
- `docs/planning/invest/delivery-batches.md` (story S6), `deferred-work.md`, `TODO.md`, `sprint-status.yaml` (key `s6c-presentation-route-tables`), this contract and its run log. `_bmad-output/implementation-artifacts/spec-s6-table-driven-capability-plumbing.md` is not edited.

Direct consumers: `PresentationDriverRules` is public in Abstractions and called from `CuriousContraptions.Bundle` (validator, supply validation), `engine/gpu` (compiled scene, presentation) and `CuriousContraptions.Abstractions.Signal`; its public members keep their signatures, so element packages (which only declare values) need no edit. `rg -n "PresentationDriverRules\." --type cs -g '!CuriousContraptions.tests/**'` lists every caller. No TypeScript, schema, fixture or tool names these members (`CuriousContraptions.web/wwwroot/workshop-observation.ts` mirrors other enums by value). Existing tests that cover the admission boundaries and stay unchanged: `PresentationBindingGoldenTests.UnsupportedPropertyCurveAndMissingNodeAreRejectedAtTheirAdmissionBoundary` (`:106`), `ComposableAnimationMappingTests.BundleValidator_ValidatesStateValues` (`:269`) and `BundleValidator_RejectsDriverCurveMismatch` (`:300`), `BundleValidatorTests` (in `tools/BundleElements.Tests`, a second test project in the solution that the filter reaches) and `CompiledSceneBoundaryTests`.

### Capability probe

All routes exist; nothing in a solver law, the worker ABI, the wire schema or a shared presentation contract changes.

- **Present:** each continuous driver's presented value -- the 14 tests cited in the first S6 contract's probe (for example `ComposableAnimationMappingTests.WorkshopDeclaredPresentation_OneCurveWithDifferentSources_ExponentialFollow`, `DeclaredPresentationTests.BatterySupplyAndEachChargeBoundaryUseActualCommittedRead`).
- **Present:** the six discrete drivers -- `ComposableAnimationMappingTests.WorkshopDeclaredPresentation_Discrete3State_AllThreeStatesAndTransitions`, `WorkshopDeclaredPresentation_UnifiedStateValues_AcrossActuatorContactAndTimer`.
- **Present:** event cues -- `WorkshopActivationAnimationTests`, `DeclaredPresentationTests.ClockTicksDrivePendulumPulseAndCounterLamps`; a timed level curve -- `DeclaredPresentationTests.CommittedLatchAndLampPreserveBothDirectionsAndDelayPhases` (`:415`), the pattern P5 follows for the level curves.
- **Present:** admission refusals -- the tests listed under Direct consumers.
- **Present, untested:** that every enum value of the eight enums has exactly one row, that both admission sites agree for every driver and payload combination, and the defining values of every curve. This slice adds P1 to P7.

### Decisions

- **One driver row feeds both admission sites (decided; the two rule sets are near-identical today).** The two sites keep their own error vocabularies (bundle errors versus the compiled-scene refusal) and their own existence functions, but read one rule, so they cannot drift.
- **Two layers, two tables per enum where two layers read it (decided).** `CuriousContraptions.Bundle` references only Abstractions projects, and the query table's rows are functions over `Track`, a private class of an engine file, so one table cannot hold both. Abstractions keeps the rules; engine keeps the query and discrete-state tables.
- **Evaluator curve functions are rows (decided).** `AnimationCurve` and `AnimationImpulseCurve` are animation primitives, a capability kind under the element-free design guide's Terms; their evaluators are the chain's last link. `AnimationImpulseCurve.SmoothDecay` has no presentation curve and keeps its row.
- **The (driver, signal node kind) matrix is this slice's (decided).** `SignalObservationRules.Supports(kind, elapsed, driver, occurrence)` is keyed by `PresentationDriver`; S6d reads the driver row's column and owns `Emits`.
- **Overlap and timing enums stay as they are (decided).** They are options of one primitive and are excluded in the inventory.

### Concurrency

After S6b; before S6d to S6g. S6d also reads `SignalObservationRules.cs` (its `Emits`, `:21-30`; this slice owns `:8-19`); S6e edits `WorkshopCompiledScene.cs:117`, `:197`, `:334` and `BundleValidator.cs:39`, `:373-381`, `:520`; S6g edits `BundleValidator.cs:330`; each publishes after this slice, one at a time, and re-reads its lines on the merged main. This slice leaves `:334`'s `SupplyTerminal` test and `:330`'s `CoordinateKind` switch to them. The new test file is staged with `git add` before the candidate commit.

## Tasks & Acceptance

**Execution:**
1. `.tools/s6c-evidence/` -- the S6 measurement on the baseline (configuration `CA1502: 12`, judged against 14) -- before; the hot-path measurement of the S6c reference scene on the baseline.
2. `CuriousContraptions.Abstractions/ElementPresentation.cs` -- the driver, property and curve tables; `PresentationDriverRules` members read them -- P1 to P6.
3. `CuriousContraptions.Abstractions.Signal/SignalObservationRules.cs`, `CuriousContraptions.Bundle/BundleValidator.cs`, `CuriousContraptions.Bundle/SupplyBundleValidation.cs`, `engine/gpu/WorkshopCompiledScene.cs` -- read the driver rows; the two reference loops -- P1 to P3.
4. `engine/gpu/WorkshopDeclaredPresentation.cs`, `engine/MachinePart.cs`, `engine/WorkshopVisualBinding.cs`, `engine/gpu/WorkshopHint.cs`, `CuriousContraptions.Abstractions/AnimationValues.cs`, `CuriousContraptions.Abstractions/AnimationDefinitions.cs`, `engine/presentation/AnimationEvaluation.cs`, `engine/presentation/AnimationBatch.cs` -- the discrete-state, property and curve rows -- P4 to P7. A new file under `engine/gpu/` or `engine/presentation/` is added to `CuriousContraptions.Core/RuntimeSources.props` (its compile list is explicit) in the same candidate.
5. `CuriousContraptions.tests/PresentationRouteTests.cs` -- the tests in the table.
6. `.tools/s6c-evidence/`, planning records -- after-measurements; result; S6 open until S6g publishes.

**Acceptance Criteria:**
- AC1: Given the measurement on the candidate, when it runs, then no changed or added member reports CA1502 above its baseline value, and `PresentationBindingValues`, `InvalidBindingValues` and `Supports` report none at threshold 14.
- AC2: Given the maintainability gate, when it runs, then both canaries pass and violations are 0.
- AC3: Given the full local round, when it completes, then failures are 0 and skipped are 0 in every project, and the total is the baseline round plus 166 (P1 26, P2 104, P3 5 plus 1 for the public rules, P4 15, P5 8, P6 1, P7 6), each case named in the run log.
- AC4: Given the hot-path rule of the inventory (S6b), when the S6c reference scene is measured on the baseline and the candidate, then the candidate's median is not above the slowest baseline run. Reference scene: the presentation of every continuous and discrete driver (the P1 scenes of the six discrete and nineteen continuous drivers composed in one scene), 10,000 `Advance` frames with the committed read changing each frame; the cold admission paths record nothing.

## Spec Change Log

## Review Triage Log

## Design Notes

1. Behaviour and scope: no behaviour change; presentation classification becomes data (story S6, owner decision 24h).
2. Reusable primitive: tables; a new driver adds a driver row, its references and its query or discrete row; a new property or curve adds one row each.
3. Declared composition: packages still declare driver, property, curve and mapping; tables key generic enums only.
4. Execution and ownership: same admission sites, same Animation worker path, immutable static tables; the per-frame lookups are frozen-dictionary `TryGetValue` (no allocation, no indexer).
5. Generality: P1 to P7 are theories over every enum value on real delivered parts, so an added value without a row, or a declaration no delivered part makes, fails a test naming it.
6. Forward-only, deleted: every `is X or Y` ladder, per-enum switch and per-driver reference ladder listed in owned scope; nothing kept beside the tables.
7. Prerequisites: S6b and the first S6 slice. E1, E2, E5, E7, E8, E9 and D3, D5, D7 apply; E3, E4, E6 and D1, D2, D4, D6 unaffected.

## Verification

Executor: implementation owner. Environment: `PATH=~/.local/share/tim-toolchain/dotnet-10.0.403:~/.local/share/tim-toolchain/node-v26.12.1-darwin-arm64/bin:$PATH`, `DOTNET_ROOT=~/.local/share/tim-toolchain/dotnet-10.0.403`. The measurement runs before the compiler-log captures.

**Commands:**
- The S6 measurement with `CA1502: 12` in its configuration, judged against 14 -- expected before: `BundleValidator.PresentationBindingValues` 22, `WorkshopCompiledScene.InvalidBindingValues` 24, `PresentationDriverRules.Supports` 17, `BundleValidator.PresentationBindingCapabilities` 20 (23 November 2026, `25df3cd5`); after: AC1.
- The five compiler-log capture commands -- expected: every build succeeds.
- `dotnet run --file tools/anvil/VerifyPolicy.cs -- --maintainability --project-root .` -- expected: AC2.
- The registration check filter (a test file is added) -- expected: 0 failed, 0 skipped.
- `dotnet test CuriousContraptions.slnx -c Debug -f net10.0 --no-build --no-restore --nologo -noAutoResponse --filter "FullyQualifiedName~PresentationRouteTests|FullyQualifiedName~ComposableAnimationMappingTests|FullyQualifiedName~DeclaredPresentationTests|FullyQualifiedName~WorkshopActivationAnimationTests|FullyQualifiedName~BundleValidatorTests|FullyQualifiedName~CompiledSceneBoundaryTests|FullyQualifiedName~WorkshopHintTests|FullyQualifiedName~WorkshopAnimationTracksTests|FullyQualifiedName~PresentationBindingGoldenTests|FullyQualifiedName~MechanicalLinkPresentationTests"` -- expected: 0 failed, 0 skipped.
- `git add -N` on new files, then `git diff --check <baseline_revision>` -- expected: clean.
- One full local round -- expected: AC3.

**Manual checks (if no CLI):**
- Read the owned diff: each listed ladder and switch is gone and each table has one row per enum value; the table's miss branch for an undefined value is unreachable past the existing guards and is verified by reading, as AC3 of the first S6 slice states.

### Acceptance-to-test table

Tolerances: P5 compares curve samples to their defining formulas within 1e-5, a local mathematical tolerance for f32 evaluation of polynomials and sines on [0, 1] (testing guidance, local mathematical contracts); `ExponentialFollow` is compared within 1e-3 of the step and `IntegratedRate` within 1e-4 rad (a wheel turning at the committed speed; a wrong rate or curve misses by far more); P4 asserts the changed native property and that no other changes, exactly.

| Rows | Named discriminating test | Control and boundary | Execution |
|---|---|---|---|
| P1 | `PresentationRouteTests.EveryDriverHasItsRouteAndBothAdmissionSitesAdmitIt` (theory over `Enum.GetValues<PresentationDriver>()`) plus each driver's existing presentation test | Control: the event driver routes to cues, not a query; a driver no part declares fails `Assert.NotNull` naming it. Boundary: the last enum value `CommittedFlowSpeed` | planned |
| P2 | `PresentationRouteTests.BothAdmissionSitesRefuseEachPayloadMismatchForEveryDriver` | Control: the unmodified binding and track admitted (P1). Boundary: `CommittedTransmissionPhase`, the one four-state driver | planned |
| P3 | `PresentationRouteTests.AnUndefinedPresentationValueIsRefusedByBothSites` (five enums) and `PresentationRouteTests.PublicRulesAnswerAnUndefinedValueAsTheyDidBefore` | Boundary: one past the last defined value; the table's own miss branch is verified by reading | planned |
| P4 | `PresentationRouteTests.EveryPropertyWritesExactlyItsNativeProperty` (theory over `PresentationProperty`) | Control: an unbound sibling node is unchanged. Boundary: `Visible` (a blend, not a scalar) | planned |
| P5 | `PresentationRouteTests.EveryCurveSamplesItsDefinedValues` (theory over `PresentationCurve`, with the per-curve input table above checked to cover all eight) | Control: a zero-duration Linear binding jumps to its target. Boundary: the endpoint sample equals the curve's end value | planned |
| P6 | `PresentationRouteTests.ACurveTheDriverDoesNotTakeIsRefused` | Control: the same curve on an admitting driver passes | planned |
| P7 | `PresentationRouteTests.EveryDiscreteDriverPresentsItsDeclaredStateValues` | Control: the existing discrete tests; boundary: `CommittedTransmissionPhase` (four states) | planned |
| AC1 to AC4 | Measurement, gate, full round, hot-path comparison | Before and after values and the five runs in the run log | planned |
