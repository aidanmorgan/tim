---
title: 'Table-driven signal network and goal routes (S6d)'
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
  - '{project-root}/_bmad-output/implementation-artifacts/spec-s6b-response-codec-read-family-table.md'
warnings: []
deferred: []
---

<intent-contract>

## Intent

**Problem:** A signal node kind is routed by hand through the bundle validator, the compiler, the network, the event producer and committed-memory validation. `ActivationNodeKind` is named 80 times on 64 lines of `engine/gpu/ActivationNetwork.cs`, in `== Kind` ladders inside members at the gate's limit (`RouteOccurrences` `:471`, 25; `CommitContactLoads` `:563`, 22; `Consume` `:276`, 21), and again in switches in `ActivationNetwork.cs:159`, `EventRecordProducer.cs:67`, `WorkshopActivationCompiler.cs:51` and `SignalBundleValidation.cs:112` (23). Goal kinds follow the same pattern across nine files. `ENGINE-SIGNAL-NODES` adds seven node rows and `ENGINE-GOALS` adds goal kinds; each would touch every site, and the network's members are already at the limit.

**Approach:** One static node-kind table keyed by `ActivationNodeKind`, whose row holds that kind's signal operation (`SignalNodeKind`), published event kind and timer completion-policy column, declaration-field rule, sensor-identity rule, duplicate-emission rule, memory and level-source flags, the edge ends it may take and its per-stage handlers; the network, compiler, producer and validators read rows instead of comparing kinds. Matching tables for `SignalNodeKind` (observation, emission and compile rules; each descriptor record declares its own kind, which replaces the descriptor-class switch), `ActivationOccurrenceKind`, `SimulationEventKind`, `SignalTerminal`, `PhysicalRequestTargetKind` and `WorkshopGoalKind` (its per-kind evaluation and field set). Behaviour is unchanged.

## Boundaries & Constraints

**Always:**
- Every value of `ActivationNodeKind` (11), `SignalNodeKind` (10), `ActivationOccurrenceKind` (7), `SimulationEventKind` (12), `SignalTerminal` (4), `PhysicalRequestTargetKind` (3) and `WorkshopGoalKind` (4) has exactly one row in its table. A defined value that is not a route has an explicit refusal row that reproduces today's exception type and message at each site: `SignalTerminal.Output` as an input terminal (`WorkshopActivationCompiler.Edges` "Unsupported input terminal."; the network's inverse "Unsupported terminal."), `PhysicalRequestTargetKind.None` ("Unsupported physical request target."), `SimulationEventKind.None` (`SimulationEvents.Domain`, `ArgumentOutOfRangeException` "No event record has this kind."), `WorkshopGoalKind.None` (valid only for a default goal, `WorkshopPuzzle.cs:19`). `ActivationOccurrenceKind.TimerElapsed` has its own row (today the `_ => ActivationNodeKind.Timer` arm of `ValidateOccurrence`, `ActivationNetwork.cs:759-768`).
- A lookup used as a predicate before admission (`ActivationMemory.Holds` at `ActivationNetwork.cs:63`, `ValidateContactBindings` at `:78`, which run before `Enum.IsDefined(node.Kind)` at `:96`) uses `TryGetValue` and answers false, as today; a lookup that today throws throws the same exception type and message (`Operation` and `KindOf` throw `ArgumentOutOfRangeException` with different messages; the constructor refuses with `ArgumentException("Invalid activation node declaration.")`). An undefined value is refused where it is today (`:96`, the `Defined` checks); the table's own miss branch is unreachable past those guards and is verified by reading the diff, as in the first S6 slice's AC3.
- Occurrence order, duplicate-emission refusals ("Duplicate source emission."), event record kinds, committed memory, timers, contacts and goal outcomes are unchanged for every delivered package and lesson.
- The node table's declaration-field rule reproduces `InvalidNodeState` (`ActivationNetwork.cs:150-156`): each kind requires its own fields (timer duration, counter target, oscillator interval, contact-load hold, request target) and refuses the others; its sensor-identity rule reproduces `InvalidSensorIdentity` (`:143-148`).
- `content/puzzles.json` keeps its text names for goal kinds, read at the one boundary (`AuthoredInstanceReader.Closed<WorkshopGoalKind>`); the goal row owns the per-kind field set, so `ParseGoal`'s `Require` lists come from it.
- Tables are built once from static data and functions; the per-tick path allocates nothing new, no stage-handler delegate closes over per-tick state, and nothing uses reflection or runtime enum enumeration.

**Never:**
- Renaming product-named kinds (`Counter`, `StateLatch`, `LevelIndicator`; audit E1): owned by `ENGINE-CAPABILITY-INSTANCES`, which then edits one row each.
- A new node behaviour (saturation, edges, thresholds): owned by `ENGINE-SIGNAL-NODES`. A new goal kind: owned by `ENGINE-GOALS`.
- A change to the wire schema, committed read shapes, element packages, or the event ring; to the terminal-to-role mappings at `WorkshopActivationCompiler.cs:36-42` and `ActivationNetwork.cs:131-137`, which S6e owns.
- A second dispatch path beside a table; a default row; a test-only accessor on a production type; suppressions; hashes; mutants; browser runs.

## I/O & Edge-Case Matrix

| Row | Input / State | Expected Output / Behavior | Error Handling |
|-----|--------------|---------------------------|----------------|
| N1 | For each `ActivationNodeKind` from `Enum.GetValues`, a real scene of the part named here for that kind (named, not discovered, so a later part that declares the kind does not move the case; the case fails if the named part stops declaring it), compiled with `CompiledSceneFixture.Compile` and observed with `WorkshopIntegrationHarness`: Contact `ImpactSwitch`, Orientation `Domino`, Passage `BallDetector`, ContactLoad `PressurePlate`, Counter `Counter`, StateLatch `StateLatch`, Timer `DelayBox`, Oscillator `SystemClock`, PhysicalRequest `Cannon`, and the two indicator kinds through `SignalLamp` wired from a level source (a state latch: `LevelIndicator`) and from a pulse source (the clock: `StickyFirstInput`), as `StateLatchTests` and `ComposableSignalNodesTests` build them | Admitted; the network constructs; the kind's first occurrence publishes its event kind (or none) as today; the event column is also asserted through the public `EventRecordProducer.KindOf(kind, completion)` over kind and policy against the independently written expectation list in the test | No error expected |
| N2 | Through the public `new ActivationNetwork(nodes, edges)` constructor (the admission, with no earlier validator in the way), for each kind, a hand-built `ActivationNodeDeclaration` with exactly another kind's field set (a counter target on a timer, a duration on a counter, an interval on a latch, hold ticks on an oscillator, a request target on a contact source, a sensor identity of another kind) | Refused | `ArgumentException("Invalid activation node declaration.")`; control: the declaration with its own fields admitted; boundary: counter target 1 admitted, 0 refused |
| N3 | The defined refusals and the undefined values: an undefined node kind (constructor), an undefined terminal (`Edges` and the network's edge validation), an undefined occurrence kind (`ActivationOccurrence.Validate`), an undefined request target (compiler); and the explicit rows: `SignalTerminal.Output` as an input, `PhysicalRequestTargetKind.None`, `SimulationEventKind.None`, `WorkshopGoalKind.None` with a non-default goal, `ActivationOccurrenceKind.TimerElapsed` routed to a timer | Each refused or routed where and as today | Today's exception type and message at each site (listed in the second Always bullet) |
| N4 | For each emitting kind (contact, orientation, passage, contact load, counter, oscillator, timer), two occurrences from one source in one `Consume` through the public `ActivationNetwork.Consume` and contact or sensor read inputs | Admitted for passage (`Passage` occurrence), a counter's `ThresholdReached` while latched, an oscillator tick (the three arms of `RouteOccurrences` `:481-491`, which become the row's admit-set of occurrence kinds) | `ArgumentException("Duplicate source emission.")` for contact, orientation, contact load and timer; control: a single emission per kind admitted |
| N5 | For each `WorkshopGoalKind` from `Enum.GetValues`: `WorkshopGoal.Validate()` on the kind's well-formed goal and on a goal with another kind's field set; `WorkshopGoalEvaluator.Evaluate` on a real scene of a delivered lesson that uses the kind (`GoalSignalSelectorTests`, `DominoEffectTests`, the lessons in `content/puzzles.json`); the content reader on `IntegrityBroken` with no observer and on an unknown kind text | Well-formed goal validated and evaluated to a non-`NotApplicable` phase; `None` evaluates to `NotApplicable`; the field-set mismatch refused | `ArgumentException("Unsupported named goal.")` (validate); the content reader's refusals (`PuzzleDefinition.cs:172-175`, `Closed<WorkshopGoalKind>`) |
| N6 | Every playable lesson | Goal outcome, event kinds and causal order unchanged | No error expected |

</intent-contract>

## Code Map

Baseline: captured at dispatch (coordinator decision 38, 23 November 2026): the published main that contains the planning work and the slices before this one; scope guards compare against it. Drafted on `25df3cd5`, the drafting revision only, not a baseline; every line cited here was read there on 24 November 2026. **Status: draft; runs before the engine programme** (owner decision 24h, 23 November 2026). Prerequisites: S6c published (S6c owns `SignalObservationRules.cs:8-19`; this slice owns `:21-30`). Review tier: **engine**. Delivery shape: one structural slice, the signal and goal rows of the S6 follow-on inventory (`spec-s6b-response-codec-read-family-table.md`); the fifth audit judged it large (about 25 production files, two tables of different layers and the goal chain) and left the split to the coordinator, who keeps it one slice; if the first review pass finds it too large, the split is the node-kind chain (N1, N2, N4) versus the goal chain and the remaining signal tables (N3, N5), with the same acceptance.

Planning verdict: replanned at dispatch from the main that contains the previous S6 slice (coordinator decision 43); sixth-audit findings in `docs/planning/elements/readiness/audits/audit-s6d-6.md` carried into that replanning.

Owned scope (complexity at threshold 12, 23 November 2026, in brackets):
- `engine/gpu/ActivationNetwork.cs` -- `:63` and `:78-82` `ValidateContactBindings`, `:86-100` `ValidateNodeDeclarations`, `:115-132` `ValidateEdges` (which kinds may be sources and targets; level sources; the terminal switch at `:131-137` is S6e's), `:143-148` `InvalidSensorIdentity`, `:150-156` `InvalidNodeState` [14] and `:159-172` `Operation` (11-arm switch) become node-row reads; the kind ladders in `Consume` `:276` [21], `QueueContacts` `:333` [15], `QueueOrientations` `:361` [20], `QueuePassages` `:400` [19], `RouteOccurrences` `:471` [25], `RouteInput` `:517-531` (the per-target-kind input dispatch), `IsLevelSource` `:556`, `CommitContactLoads` `:563` [22], `ValidatePhysicalActivation` `:670` [17], `ValidateLatchedActivation` `:686` [16], `ValidateOccurrence` `:754` [15] (its 6-arm `ActivationOccurrenceKind` switch at `:759`) and the four `*Cause` members `:787-825` read rows.
- `engine/gpu/EventRecordProducer.cs:67-80` `KindOf` (11 arms, with the nested `TimerCompletionPolicy` switch) -- the node row's event column and completion-policy column; `Stage` `:86` [22] reads it.
- `engine/gpu/WorkshopActivationCompiler.cs:51-70` (descriptor to node, with the `PhysicalRequestTargetKind` switch at `:63`) -- per-kind compile rows; `:36-42` is S6e's.
- `engine/gpu/WorkshopSimulation.cs:66` and `CuriousContraptions.Abstractions/ActivationMemory.cs:22-23` `Holds`, `:27` `Validate` [25] -- their kind comparisons read rows.
- `CuriousContraptions.Abstractions/SignalContractValues.cs:33-44` (the ten-arm descriptor-class-to-`SignalNodeKind` switch: each descriptor record declares its kind) and `CuriousContraptions.Abstractions.Signal/SignalContracts.cs:17` (`SignalTerminals.Kind`, terminal and descriptor to `PortSignalKind`); `CuriousContraptions.Bundle/SignalBundleValidation.cs:112` `ValidateNode` [23] (descriptor type switch with `PhysicalRequestTargetKind` at `:114`, `:140`) -- one validation row per node kind; `CuriousContraptions.Bundle/RunControlBindings.cs:158` `IsBoundToSignal` (which descriptor binds a parameter) -- the node row's parameter-binding column.
- `CuriousContraptions.Abstractions.Signal/SignalObservationRules.cs:21-30` (`Emits`), `CuriousContraptions.Abstractions/EventDescriptor.cs:47` `SignalSource`, `:51` `Domain` (11 arms), `CuriousContraptions.Abstractions/EventRingBuffer.cs:21` `Validate` (the three kind-specific record rules), `CuriousContraptions.Abstractions/ActivationTimerValues.cs:62` `ActivationOccurrence.Validate` (the per-occurrence time rules), `CuriousContraptions.Abstractions/PhysicalRequestValues.cs:14`, `:27`, `CuriousContraptions.Simulation/WorkshopPhysicsEngine.Requests.cs:21`, `engine/gpu/WorkshopMessages.cs:32` -- rows (the event-kind row holds the domain, the signal-source flag, the node kinds that emit it and its record rule).
- Goals: `engine/gpu/WorkshopGoalEvaluator.cs:13`, `:20`, `:27`, `:37`, `:41` (the evaluation of each goal kind), `engine/gpu/WorkshopPuzzle.cs:17` (`Validate` `:15` [25]), `engine/gpu/WorkshopGoalWake.cs:34`, `engine/gpu/WorkshopSavePrevalidation.cs:211` (`CheckGoal` `:206` [24]), `engine/gpu/PuzzleDefinition.cs:48`, `:87`, `:167-175` (per-kind owner checks), `:212-234` (`ParseGoal`: the per-kind field set read from `content/puzzles.json` text and the per-kind validity test), `:250-253`, `engine/gpu/PuzzleCatalogue.cs:65`, `:130` -- the goal-kind table.
- `CuriousContraptions.tests/SignalRouteTests.cs` -- new: N1 to N5.
- `docs/planning/invest/delivery-batches.md` (story S6), `deferred-work.md`, `TODO.md`, `sprint-status.yaml` (key `s6d-signal-node-route-tables`), this contract and its run log.

**Row shape (decided).** Layers follow the readers. Abstractions: `ActivationNodeKindRow` in a `FrozenDictionary<ActivationNodeKind, ...>` with columns signal operation, published event kind, timer completion policy, memory holder, level source, may-source and may-target, required declaration fields, sensor-identity field, parameter-binding field, read by `ActivationMemory`, `SignalTopology`, `SignalObservationRules`, `EventRecordProducer.KindOf` and the bundle and engine tables; a `SignalNodeKind` row (observation, emission, event kinds) read the same way; the event-kind, occurrence-kind and terminal rows likewise. Bundle: a table keyed by `SignalNodeKind` whose row function validates one descriptor (`SignalBundleValidation.ValidateNode`'s arms). Engine: a table keyed by `SignalNodeKind` whose row function compiles one descriptor (`WorkshopActivationCompiler`), and the stage dispatch of `ActivationNetwork` as a `FrozenDictionary<ActivationNodeKind, StageHandlers>` of custom delegate types with `Span` and `ref` parameters (`Func<>` cannot carry them), static, built once; the handlers stay private members of the network. The bundle and Abstractions projects cannot see engine types, so no row holds an engine function. This shape is the contract's choice; the implementation does not substitute another (rows as data only with stage branches reduced by extraction is not an option here), and if the diff proves too large the split named in the Code Map applies.

Direct consumers: every member above keeps its signature; element packages only declare descriptors. `spec-engine-goal-evaluation.md` (drafted, after the programme) cites `WorkshopPuzzle.cs:9`, `:75` and the goal sites; it is replanned from the published revision. `spec-closure-review-electrical-gates.md` and the `ENGINE-SIGNAL-NODES` self-edge slice text (`epics.md`) cite network lines; both are after the programme and re-read them. `CuriousContraptions.web/wwwroot/workshop-observation.ts:6`, `:7`, `:31` mirror `ActivationNodeKind`, `ActivationOccurrenceKind` and `SimulationEventKind` by value; unchanged (the members do not change; `ENGINE-CAPABILITY-INSTANCES` generates the mirrors, owner decision 38). `content/puzzles.json` carries goal kind text, unchanged. A new file under `engine/gpu/` or `CuriousContraptions.Simulation/` is added to `CuriousContraptions.Core/RuntimeSources.props` (its compile list is explicit) in the same candidate.

### Capability probe

All routes exist; nothing in a solver law, the worker ABI, the wire schema or a shared presentation contract changes.

- **Present:** contact source -- `ContactActivationTests`; orientation -- `OrientationSensorTests`, `DeclaredOrientationTests`; passage -- `BallDetectorTests`; contact load -- `ComposedPhysicalSignalTests`; counter -- `CounterTests.ActivationNetwork_CountingProgression_SingleEmission_AndSaturation`, `MemoryGuardTests.ThreeBallsThroughTheDetectorEmitOnceOnTheTargetCrossingOnly`; state latch -- `StateLatchTests`; oscillator -- `ClockTests`; timer -- `ActivationTimerTests`, `DelayedSignalTests`; indicators and sticky first input -- `StateLatchTests`, `ComposableSignalNodesTests.OnlyTheSelectedTimerChangesItsEventAndVisual`; physical request -- `RetentionRequestTests`, `PrismaticRetentionTests`; event record kinds -- `EventRingProducerTests`; goals -- `GoalSignalSelectorTests`, `DominoEffectTests` and each playable lesson's test.
- **Present, untested:** that every value of the seven enums has exactly one row, the per-kind field rule as a theory, the explicit refusal rows, and the per-kind goal field sets. This slice adds N1 to N5.

### Decisions

- **Goal kinds belong here (decided).** Owner decision 24h covers every per-capability chain; goals are a capability family with nine routing files, evaluated from committed signal state.
- **Rows carry handlers where the layer allows (decided).** Where a kind's stage behaviour differs (queueing, duplicate rule, commit), the engine row holds a static function, so a new kind adds one row instead of a branch per stage; the Abstractions and Bundle rows hold data and their own layer's functions.
- **Terminals belong to S6e (decided).** The terminal-to-role switch of the compiler and its inverse in the network are one `PortRole` mapping owned by S6e; this slice keeps `:51-70` of the compiler and no `:36`, `:134`.
- **Single-kind selections are not dispatch (decided).** A comparison that selects one kind to find a node stays: `WorkshopCompiledScene.cs:92`, `:120`, `PhysicalRequestComposition.cs:16`, `:39`, `engine/gpu/ActivationMemory.cs:11`, `WorkshopSimulation.cs:67`; the run log lists each with file:line.

### Concurrency

After S6c; before S6e, S6f and S6g. S6e edits `ActivationNetwork.cs:131-137` and `WorkshopActivationCompiler.cs:36-42` (the `PortRole` mapping) and shares `WorkshopActivationCompiler.cs` and `ActivationNetwork.cs` with this slice; S6c shares `SignalObservationRules.cs`; S6g edits `BundleValidator.cs` and `WorkshopCompiledScene.cs` coordinate sites. Whichever publishes second re-reads its lines on the merged main. The new test file is staged with `git add` before the candidate commit.

## Tasks & Acceptance

**Execution:**
1. `.tools/s6d-evidence/` -- the S6 measurement on the baseline (configuration `CA1502: 12`, judged against 14) -- before; the hot-path measurement of the S6d reference scene on the baseline.
2. `engine/gpu/ActivationNetwork.cs`, `engine/gpu/EventRecordProducer.cs`, `engine/gpu/WorkshopActivationCompiler.cs`, `engine/gpu/WorkshopSimulation.cs`, `CuriousContraptions.Abstractions/ActivationMemory.cs`, `ActivationTimerValues.cs` -- the node-kind table and its readers -- N1, N2, N4.
3. `CuriousContraptions.Bundle/SignalBundleValidation.cs`, `RunControlBindings.cs`, `CuriousContraptions.Abstractions.Signal/SignalObservationRules.cs`, `SignalContracts.cs`, `CuriousContraptions.Abstractions/SignalContractValues.cs`, `EventDescriptor.cs`, `EventRingBuffer.cs`, `PhysicalRequestValues.cs`, `CuriousContraptions.Simulation/WorkshopPhysicsEngine.Requests.cs`, `engine/gpu/WorkshopMessages.cs` -- the remaining signal tables -- N1 to N3.
4. `engine/gpu/WorkshopGoalEvaluator.cs`, `WorkshopPuzzle.cs`, `WorkshopGoalWake.cs`, `WorkshopSavePrevalidation.cs`, `PuzzleDefinition.cs`, `PuzzleCatalogue.cs` -- the goal table -- N5.
5. `CuriousContraptions.tests/SignalRouteTests.cs` -- the tests in the table.
6. `.tools/s6d-evidence/`, planning records -- after-measurements; result.

**Acceptance Criteria:**
- AC1: Given the measurement, when it runs on the candidate, then no changed or added member reports CA1502 above its baseline value, and `RouteOccurrences`, `CommitContactLoads`, `SignalBundleValidation.ValidateNode`, `WorkshopPuzzle.Validate` and `WorkshopSavePrevalidation.CheckGoal` each report at least 3 below their baseline value, or no line (at most 12, the measurement's configured threshold).
- AC2: Given the maintainability gate, when it runs, then both canaries pass and violations are 0.
- AC3: Given the full local round, when it completes, then failures are 0 and skipped are 0 in every project, and the total is the baseline round plus 44 (N1 11, N2 11, N3 9, N4 7, N5 6), each case named in the run log.
- AC4: Given the hot-path rule of the inventory (S6b), when the S6d reference scene is measured on the baseline and the candidate, then the candidate's median is not above the slowest baseline run. Reference scene: the clock, delay, counter, ball detector and state latch lesson scenes composed in one scene (the N1 scenes), 10,000 ticks with the committed contacts changing each tick, through `ActivationNetwork.Consume`.

## Spec Change Log

## Review Triage Log

## Design Notes

1. Behaviour and scope: no behaviour change; signal and goal routing becomes data (story S6, owner decision 24h).
2. Reusable primitive: one row per node kind; `ENGINE-SIGNAL-NODES` adds rows, not branches.
3. Declared composition: packages declare descriptors; tables key generic enums only (element-free design D2, D5).
4. Execution and ownership: same network stages and order; immutable static tables; stage handlers are static.
5. Generality: N1 to N3 are theories over every enum value on real delivered parts; an added kind without a row, or without a declaring part, fails.
6. Forward-only, deleted: the kind ladders, descriptor-class switch and switches listed in owned scope; nothing kept beside the tables.
7. Prerequisites: S6c. E1, E2, E3, E5, E7, E8, E9 and D1, D2, D5 apply; E4, E6 and D6, D7 unaffected.

## Verification

Executor: implementation owner. Environment: `PATH=~/.local/share/tim-toolchain/dotnet-10.0.403:~/.local/share/tim-toolchain/node-v26.12.1-darwin-arm64/bin:$PATH`, `DOTNET_ROOT=~/.local/share/tim-toolchain/dotnet-10.0.403`. The measurement runs before the compiler-log captures.

**Commands:**
- The S6 measurement with `CA1502: 12` in its configuration, judged against 14 -- expected before: the bracketed values above; after: AC1.
- The five compiler-log capture commands -- expected: every build succeeds.
- `dotnet run --file tools/anvil/VerifyPolicy.cs -- --maintainability --project-root .` -- expected: AC2.
- The registration check filter (a test file is added) -- expected: 0 failed, 0 skipped.
- `dotnet test CuriousContraptions.slnx -c Debug -f net10.0 --no-build --no-restore --nologo -noAutoResponse --filter "FullyQualifiedName~SignalRouteTests|FullyQualifiedName~ContactActivationTests|FullyQualifiedName~OrientationSensorTests|FullyQualifiedName~BallDetectorTests|FullyQualifiedName~ComposedPhysicalSignalTests|FullyQualifiedName~CounterTests|FullyQualifiedName~MemoryGuardTests|FullyQualifiedName~StateLatchTests|FullyQualifiedName~ClockTests|FullyQualifiedName~ActivationTimerTests|FullyQualifiedName~DelayedSignalTests|FullyQualifiedName~ComposableSignalNodesTests|FullyQualifiedName~RetentionRequestTests|FullyQualifiedName~EventRingProducerTests|FullyQualifiedName~GoalSignalSelectorTests|FullyQualifiedName~DominoEffectTests"` -- expected: 0 failed, 0 skipped.
- `git add -N` on new files, then `git diff --check <baseline_revision>` -- expected: clean.
- One full local round -- expected: AC3 (N6 is every lesson test in it).

**Manual checks (if no CLI):**
- Read the owned diff: in the owned files, no comparison of a node kind (`== ActivationNodeKind.X`, `is ActivationNodeKind.X or ...`) naming two or more kinds remains outside the tables' rows; the single-kind selections listed under Decisions are not dispatch and stay, each recorded in the run log.

### Acceptance-to-test table

No tolerance is chosen: assertions are kinds, counts, orders and refusals; lesson goals keep their own stall guards.

| Rows | Named discriminating test | Control and boundary | Execution |
|---|---|---|---|
| N1 | `SignalRouteTests.EveryNodeKindCompilesRunsAndPublishesItsEventKind` (theory over `ActivationNodeKind`, the part per kind named in the matrix) plus each kind's existing test | Control: a passage node publishes no record of its own. Boundary: the timer's two completion policies (Latch publishes, Rearm does not) | planned |
| N2 | `SignalRouteTests.EveryNodeKindRefusesAFieldOfAnotherKind` | Control: the kind's own fields admitted (N1). Boundary: a counter target of 1 admitted, 0 refused | planned |
| N3 | `SignalRouteTests.UndefinedAndExplicitlyRefusedSignalValuesAreRefusedAsToday` (node kind, occurrence kind, terminal, request target, and the four explicit rows) | Boundary: one past each last defined value; `TimerElapsed` routed to a timer | planned |
| N4 | `SignalRouteTests.OnlyTheAdmittedKindsMayEmitTwiceInOneTick` | Control: a contact source's second emission is refused; a single emission per kind admitted | planned |
| N5 | `SignalRouteTests.EveryGoalKindValidatesAndEvaluates` (theory over `WorkshopGoalKind`) and `SignalRouteTests.TheContentReaderRefusesAnIntegrityGoalWithoutAnObserverAndAnUnknownKind` | Control: `None` evaluates to `NotApplicable`. Boundary: `IntegrityBroken` with no integrity observer refused at content read | planned |
| N6, AC1 to AC4 | Full local round, measurement, gate, hot-path comparison | Baseline round, values and the five runs in the run log | planned |
