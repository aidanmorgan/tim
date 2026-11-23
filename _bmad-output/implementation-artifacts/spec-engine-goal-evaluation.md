---
title: 'ENGINE-GOALS part 1: all-of goal composition over committed observations'
type: 'feature'
created: '2026-11-21'
status: 'blocked'
baseline_revision: 'captured at dispatch (the main that contains the planning work)'
review_loop_iteration: 0
followup_review_recommended: false
context:
  - '{project-root}/_bmad/custom/element-free-engine-design.md'
  - '{project-root}/_bmad/custom/element-free-engine-review.md'
  - '{project-root}/docs/testing-guidance.md'
  - '{project-root}/schemas/README.md'
warnings: ['oversized']
deferred: []
---

<intent-contract>

## Intent

**Problem:** A level has exactly one goal of one of three shapes, and an activation goal must name a predecessor node. Thirty-four planned levels built only from delivered parts need several goals at once, an activation with no predecessor, or a goal on shaft travel, so none of them can be authored.

**Approach:** Replace the single goal with one typed, declared list of conditions that must all hold. Each condition selects one committed observation the engine already publishes (capture latch, activation latch, integrity break, coordinate travel reached) and may require an earlier observation by a minimum game time. There is one evaluator, one content shape and one save shape; the single-goal path is deleted.

## Boundaries & Constraints

**Always:**
- Goal truth derives only from the committed read, on the main thread. Nothing flows back to the worker.
- A goal is solved in the first committed read in which every condition holds. Its occurrence is the latest condition occurrence. The declared order of conditions never changes phase or occurrence.
- A condition names typed owners and local identities. No part identity, kind or name appears in engine code. Closed sets stay enums end to end; durations are f32 game seconds.
- Unsupported, malformed, duplicate, unresolved, non-finite or out-of-range goal data is refused atomically with a typed reason at content load, edit validation and save load. The installed catalogue or construction is unchanged.
- Reset returns the goal and the Solved cue to Waiting with the authored state. Save and load preserve the decoded goal, and a reloaded solved construction solves again.
- The 15 Playable levels keep their outcome, their failing control, Reset and save/load behaviour.
- The player sees one Solved cue, as today, at the goal's occurrence.
- Forward-only: current callers, content, schema, tools, tests and active documents change together.

**Never:**
- No change to the worker ABI, `schemas/workshop_wire.fbs`, a solver law or `WorkshopRead`. The main-thread presentation sample gains the existing committed travel read.
- No electrical "supplied" occurrence and no `powered_after` (part 2). No per-condition progress or Solved result on the observation port or HUD (part 3).
- No any-of or count group, ordered sequence beyond one predecessor or forbidden event (owned by `engine-goal-ordered-events`); no count, time-window, hold or scalar-band goal (owned by `engine-goal-count-window-hold`). The quantity, rate-window and protected-state goals of EL-120, EL-121 and EL-123 are built on those two slices.
- No element package added or changed. No Planned level authored or made Playable.
- No fallback, alias, migration, retained goal field or second goal representation. No output hash, byte identity, fixed arrival tick, skipped test, mutant or browser run.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| M1 All-of | A goal of four conditions no catalogue level has: two captures on distinct sensors, one activation with no predecessor, one coordinate travel; the real simulation makes all four occur | Waiting until the last occurs; Solved in the first committed read where all hold; occurrence equals the latest condition occurrence | No error expected |
| M2 Reordered | The M1 conditions declared in every other order | Same phase at every committed read and the same occurrence as M1 | No error expected |
| M3 One unmet | M1 with the single cause of one condition removed, for each condition in turn | Waiting for the whole stall guard while the other three observations still occur | No error expected |
| M4 Predecessor | A condition requiring an earlier observation by minimum `d`: gap just below `d`, exactly `d`, above `d`; earlier absent; later before earlier | Holds only when the earlier occurred first and the gap is at least `d` | No error expected |
| M5 Travel | A travel condition on each of two declared observers with different thresholds | Each holds from the read in which its observer is Reached; Waiting while it accumulates | No error expected |
| M6 Content refusal | Authored `goals` that is absent, empty, not an array; a condition with an undefined kind, a missing or extra field, a field of another kind, an unknown slot, an undeclared node or observation, owners that declare no capture, a predecessor equal to its condition, a duplicate condition, a minimum below 0, above 120 or non-finite; the old `goal` key | `InvalidDataException` naming the puzzle and field | `PuzzleCatalogue.ReadDefinitions` throws; the installed catalogue still serves every prior level unchanged |
| M7 Edit and save refusal | A construction or save whose goal differs from the authored goal; a saved condition with an undefined kind, non-finite or out-of-range minimum, fields not of its kind, or a duplicate; a save in the old single-goal layout | `WorkshopEditRejection.PuzzleSettings`, or the matching `WorkshopSaveRejection` (`UndefinedEnum`, `NonFinite`, `OutOfRange`, `MissingField`, `VerifierRejected`) | Nothing is decoded into a construction or installed |
| M8 Reset | A solved multi-condition goal, then Reset, then Run | After Reset: Waiting, neutral cue, authored state. The second Run solves again | No error expected |
| M9 Save/load | A solved multi-condition construction saved and loaded | The decoded goal equals the saved goal; a fresh run of the loaded construction solves | No error expected |

</intent-contract>

## Code Map

Checkout facts measured at `8d1d68aa` (published main). `baseline_revision` is captured at dispatch (coordinator decision 38, 23 November 2026) from the published main that contains the planning work, the programme and S5, before first implementation; scope guards compare against it. `25df3cd5` is the drafting revision only, not a baseline. The level counts here (34 planned levels, 15 and 19 Playable, the 48 Planned analysis) predate owner decision 24d, which removed 29 authored levels, and the 45th package (the Baseball); they are recounted at replanning.

**Planning state: blocked — waits for the engine programme (owner decision 22) and for prerequisite S5 published; replanned from the main that contains both.** Owner: coordinator. Retry condition: `ENGINE-SOA-SIMD` (the programme's last story) and S5 both published. Owner decision 22 (23 November 2026): the goal slices wait for the owner decision 3a engine programme (`doc-engine-status` to `engine-soa-simd`); only the visible-solid repairs, the Baseball repair and structural stories S3, S5 and S6 run before it. When S5 has published, the schema accessors are generated in the build, so the regeneration step and the committed generated files in this contract's scope are re-derived at replanning; the programme also moves citations (`engine-capability-instances` renames activation kinds, `engine-sensor-volumes` adds residence phases), so every `path:line` is re-verified then. Everything else in this contract is settled.

**Planning verdict.** Scoped Pass of the `8d1d68aa` draft (planning auditor `aa7a9cb1524b7322c`, second pass, 21 November 2026); the replanned contract passes the planning gate again.

**Delivery shape and tier.** Single engine slice (not a Family slice or closure). **Engine tier**: it changes engine, schema, generated and TypeScript files, and its probe has *narrower* and *missing* entries. All six layers on the first pass. The capability probe is the subsection below.

**Split decision (owner story [ENGINE-GOALS](../planning-artifacts/epics.md#story-engine-goals-declared-goal-kinds-and-ordered-objectives); owner decision 12, 22 November 2026).** The story is five engine slices, because the parts change different shared contracts:

1. **This contract** (`engine-goal-evaluation`): composition and the observation kinds whose committed reads exist. It changes the save schema, the content shape and the observation projection. It changes no worker ABI and no HUD contract.
2. `engine-goal-supplied-occurrence`: a committed first-supplied occurrence for an electrical node (worker read ABI and `workshop_wire.fbs`), then a `Supplied` observation kind. Owns `powered_after` (levels 15, 49, and 24 once its parts exist).
3. `engine-goal-progress-observation`: per-condition progress and the Solved result on the typed observation port and HUD (shared presentation contract).
4. `engine-goal-ordered-events`: ordered conditions with a minimum gap (equal occurrence times satisfy a zero minimum), a forbidden event with a failed-until-Reset phase, and the any-of form as a count group "at least k of n" whose occurrence is the k-th earliest, one subject counted once per group (owner decision 12: the any-of/count group form ships here). It carries the story's own acceptance verbatim: "Given an ordered-events goal, when its events occur in the required order, then the level is Solved; the same events out of order do not solve it, and a forbidden event fails the attempt until Reset."
5. `engine-goal-count-window-hold` (owner decision 12): count, time-window and hold goals. A half-open game-time window from Run start on a condition or group, refused when it ends after the level's declared Run length (`engine-run-length`); a condition may require a named committed scalar of its subject to lie in an authored closed band at its occurrence; a held level is a committed level with its became-true occurrence (coordinator decision 16; the residence phase comes from `engine-sensor-volumes`), and the goal holds when the read's time is the declared hold past the latest became-true occurrence; the evaluator stays stateless and schedules one time wake; a hold not shorter than the Run length is refused.

ENGINE-GOALS stays open until all five slices publish.

After this slice, 31 planned levels become authorable with content only: the 30 multi-goal levels without `powered_after`, and 02 `power_trip`. Level 14 `battery_motor` additionally needs a travel observer declared on the Motor package, in its own level slice.

### Owned scope

Engine and schema:
- `engine/gpu/WorkshopPuzzle.cs:9-31,74-77` -- goal model: delete `WorkshopGoalKind`, `IntegrityGoalSelector` and the single-shape `WorkshopGoal`; add the condition list.
- `engine/gpu/PuzzleDefinition.cs:24-32,43-59,84-96,130-131,164-185,212-254,273,283` -- `AuthoredGoal`, `CreateGoal`, `ValidateGoalSignals`, `ParseGoal`, `ParseIntegrityGoal`, solution goal-target check, required field lists (`:107-108`, `:113-114`).
- `engine/gpu/PuzzleCatalogue.cs:32-33,64-67,74-76,97-107,120-126,129-132` -- goal construction, goal-target rebinding, identity anchor.
- `engine/gpu/WorkshopConstruction.cs:17-25` -- split `Validate` into an internal `ValidateStructure(supply, bundle)` (revision, settings, atmosphere, instances, connections) followed by the puzzle check.
- `engine/gpu/WorkshopGoalEvaluator.cs:10-56` and `engine/gpu/WorkshopGoalWake.cs:28-49` -- evaluation and wake.
- `engine/gpu/CompiledIdentityMap.cs:74` -- `TryIntegrity` signature; add the travel lookup beside it (`:36`).
- `schemas/workshop_authoring.fbs:57-77`, `engine/gpu/WorkshopAuthoringGenerated.cs` (regenerated), `engine/gpu/WorkshopAuthoringFlatBuffers.cs:105-137,279-294`, `engine/gpu/WorkshopSavePrevalidation.cs:161-223`.
- `engine/gpu/WorkshopObservationProjection.cs:89-92`, `CuriousContraptions.web/wwwroot/workshop-observation.ts:43,147`.
- Callers that only pass the goal through: `engine/gpu/BrowserWorkshopClient.Ui.cs:86,90,141`, `engine/MachineWorld.Gpu.cs:43-44`.
- Presentation sample travel lane: `CuriousContraptions.Abstractions/PresentationSampleValues.cs:9-10`, `engine/gpu/WorkshopPoseHistory.cs:212,248`, `engine/gpu/BrowserWorkshopClient.Ui.cs:135-143`.

Content, tools and documents:
- `content/puzzles.json` -- the `goal` object of all 15 Playable entries becomes `goals`. Planned entries are untouched.
- `tools/e2e/workshop-wire.ts` (generated part), `tools/e2e/run-reset-snapshot.ts:141-142` (`SNAPSHOT_SCHEMA`), `tools/e2e/unit/run-reset-snapshot.test.ts:66-73`.
- Hand-written TypeScript consumers: `tools/e2e/workshop-wire.ts:12853-12858` (the dump below the helpers marker at `:12732`), `tools/e2e/cat-062b.test.ts:252,262,263`, `tools/e2e/unit/run-reset-snapshot.test.ts:40,229`, `tools/e2e/run-reset-snapshot.ts:141`.
- `docs/engine-contracts.md:257`, `docs/presentation-bindings.md:273`, `schemas/README.md` if it names a goal field, `docs/planning/invest/delivery-batches.md` section 4, the ENGINE-GOALS story in `_bmad-output/planning-artifacts/epics.md` (its first-slice text), the `engine-goal-evaluation` key in `_bmad-output/implementation-artifacts/sprint-status.yaml`, `TODO.md`, `_bmad-output/implementation-artifacts/deferred-work.md`, this contract and its run log.

Tests (32 files in `CuriousContraptions.tests` name a goal; `rg -l "Goal|goal" CuriousContraptions.tests -g '*.cs'`):
- New: `GoalCompositionTests.cs`, `GoalAdmissionTests.cs`, `Fixtures/pre-goal-list/` (three old-layout saves and a provenance note). No test-project edit: `CuriousContraptions.tests.csproj:77` compiles every top-level `*.cs` (S2, `25df3cd5`).
- Adapted to the new model, keeping every assertion's meaning: `SaveFixture.cs:100-137`, `CompoundMassAndRollingTests.cs:665-678,884-890`, `EventRingConsumerTests.cs:554-650`, `GoalSignalSelectorTests.cs`, `DelayedSignalTests.cs:92-142`, `DominoEffectTests.cs:28,75,84-98`, `LaunchLessonSolveTests.cs:79-80,152-166,191-216`, `F32BehaviourIdentityTests.cs`, `WorkshopSaveTests.cs`, `WorkshopSaveRejectionTests.cs`, `WorkshopObservationTests.cs:572-582`, `WorkshopWireFlatBuffersTests.cs`, `WorkshopHintTests.cs`, `FanGuidanceTests.cs:149`, and each remaining file the compiler reports. Name each in this list before editing it.
- Also adapted: `F32SaveBoundaryTests.cs:53-54` (its older-layout refusal stays).

Not owned: `schemas/workshop_wire.fbs`, `engine/gpu/WorkshopWireGenerated.cs`, any `CuriousContraptions.Simulation` or `elements/` file, `ui/`.

### Capability probe

Performed while drafting; the planning scout re-verifies each citation.

| # | Capability | State | Evidence |
|---|---|---|---|
| 1 | Committed capture latch with occurrence | present | `engine/gpu/WorkshopGoalEvaluator.cs:30-36`, `engine/gpu/CompiledIdentityMap.cs:81`; `LaunchLessonSolveTests.AuthoredLessonSolvesAndOneMissingCausePreventsTheGoal` |
| 2 | Committed activation latch with occurrence time | present | `WorkshopGoalEvaluator.cs:41-54`; `DominoEffectTests.GoalSolvesOnlyWhenTheEndDominoAndTheLampAreBothLatched` |
| 3 | Minimum elapsed game time between two occurrences | present | `WorkshopGoalEvaluator.cs:57-72`; `DelayedSignalTests.DyadicMinimumDoesNotRoundElapsedBeforeComparison` |
| 4 | Committed integrity break with occurrence | present | `WorkshopGoalEvaluator.cs:37-40`, `CompiledIdentityMap.cs:74`; `GoalSignalSelectorTests.AuthoredIntegrityGoalBindsTheDeclaredObservationAndCurrentSaveField` |
| 5 | Committed coordinate travel with a reached state and tick ordinal; it has no phase within the tick | present | `CuriousContraptions.Abstractions/CoordinateTravelValues.cs:26-27`, `CuriousContraptions.Abstractions/WorkshopRead.cs:10`, `CuriousContraptions.Simulation/WorkshopPhysicsEngine.CoordinateTravel.cs:8-25`; its event record requires phase 0 (`CuriousContraptions.Abstractions/EventRingBuffer.cs:30`); `CoordinateTravelGeneralityTests.DistinctObserversFollowExactMovingCoordinatesAndReset` |
| 6 | Travel as a goal observation | missing, owned here | The evaluator reads no travel observer. Adding it reads an existing committed read; no ABI change |
| 7 | Activation with no predecessor | narrower, owned here | `WorkshopPuzzle.cs:22-24` and `PuzzleDefinition.cs:229-235` require a distinct source node. A model and validator change only |
| 8 | Several goals per level | missing, owned here | `WorkshopPuzzle.cs:75`, `PuzzleDefinition.cs:30`, `schemas/workshop_authoring.fbs:68-76`. Save schema change: this is the engine slice that owns it |
| 9 | Goal wake from the event ring | present for one goal; composite narrower, owned here | `WorkshopGoalWake.cs:34-49`; `EventRingConsumerTests.TheGoalWakeIgnoresOtherOwnersAndALapMakesItDueAtOnceAndAfterTheNewestRecord` |
| 10 | Save encode, decode and pre-validation of a goal | present for one goal | `WorkshopAuthoringFlatBuffers.cs:105-137,279-283`, `WorkshopSavePrevalidation.cs:206-223`; `GoalSignalSelectorTests.SaveRefusesMissingSelectorAndPreservesTheAuthoredGoal` |
| 11 | Typed refusal of a save whose layout was superseded | present | `CuriousContraptions.tests/PreConversionLaunchSaveTests.cs:94`, `CuriousContraptions.tests/Fixtures/README-pre-conversion.md` |
| 12 | Goal on the observation projection | present for one goal | `WorkshopObservationProjection.cs:89-92`; `WorkshopObservationTests.ProductionConstructionProjectionIncludesPuzzleConfigurationAndConnectionDomains`. The TypeScript enum at `workshop-observation.ts:43` already lacks `IntegrityBroken` (pre-existing defect, repaired here) |
| 13 | One Solved cue at the goal occurrence | narrower, owned here: the presentation sample has no travel lane | `BrowserWorkshopClient.Ui.cs:82-97,135-143`, `engine/gpu/WorkshopUi.cs:16`; `WorkshopHintTests.CommittedBuildingClearsSolvedWithEitherAcknowledgementOrder`. The cue is applied only when the goal re-evaluates against the presentation sample (`:135-143`), and `PresentationSampleValues.cs:9-10` carries captures, activations and integrity only |
| 14 | Authored identity range independent of the goal | narrower, owned here | `PuzzleCatalogue.cs:129-132` anchors on the single goal's source body, except for `IntegrityBroken` |
| 15 | A goal owner the player places (`goal_target`) | present for one goal | `PuzzleCatalogue.cs:64-67,120-126`; `LaunchLessonSolveTests.EachLaunchRouteSolvesOrStaysUnsolvedAndResetRestoresTheBench` (`ReadyReceiver`) |
| 16 | Electrical supplied occurrence | **missing, not built here** | `CuriousContraptions.Abstractions/PhysicsPresentationRead.cs:27` publishes a `Supplied` level with no occurrence time. Needs the worker ABI and wire: slice `engine-goal-supplied-occurrence` |
| 17 | Per-condition progress on the observation port and HUD | **missing, not built here** | `WorkshopObservationProjection.cs:84-93` projects no phase. Shared presentation contract: slice `engine-goal-progress-observation` |
| 18 | Regenerating schema accessors | present | `schemas/README.md` ("Regenerating the code"), `tools/WorkshopWireGeneration/Program.cs`; `WorkshopWireGenerationTests.GeneratedAccessorsHaveOneSupportedSurface` |

Counts: present 7; present for one goal and extended here 3; narrower 4 (activation predecessor, composite wake, identity anchor, presentation-sample travel lane); missing and owned here 2; missing and split out 2, plus the ordered-events and count-window-hold parts, which no row here needs. No capability this slice builds changes a solver law, the worker ABI, `workshop_wire.fbs` or `WorkshopRead`.

### What the planned levels need (measured)

`python3` over `content/puzzles.json` and the `Metadata.Id` of the 44 packages under `elements/`: 48 Planned levels; 45 use only delivered parts; 13 of those have one goal and 32 have two to five (14, 13, 4, 1). Their 101 goals are 51 `activated`, 46 `captured`, 2 `turned`, 2 `powered_after`. Every target is a fixed part: a Receiver (capture), a Signal lamp, Domino or Fan (one signal node), or a Motor (`turned`, `powered_after`). `activated` never names a predecessor, so today's `ActivatedAfter` cannot express level 02 `power_trip`, whose only fixed parts are a ball and the lamp.

### Decisions made (defensible from the game's intent)

- **All-of only.** Every planned multi-goal level is a conjunction. Any-of and count groups, ordered sequences and forbidden events are owned by `engine-goal-ordered-events`; count, window, hold and scalar-band goals by `engine-goal-count-window-hold`; EL-120, EL-121 and EL-123 consume them.
- **A predecessor is one optional earlier observation of any kind.** This keeps today's activated-after behaviour, and part 2 adds supplied-after without a new rule.
- **Composite occurrence is the latest condition occurrence.** Each observation is a first-occurrence latch until Reset, so the conjunction never un-solves in a run.
- **No condition-count ceiling.** At least one condition for an authored level; none for the Free workshop.
- **The identity anchor is the lowest locked instance identity for every goal.** This is today's `IntegrityBroken` rule (`PuzzleCatalogue.cs:131`); a player cannot place a locked part (`:137`). Content load refuses an unlocked placement, which no current entry has.
- **A saved goal must equal the authored goal in authored order.** Order-independence is a property of evaluation, not a licence to alter a save.
- **`turned` is not a goal kind.** It is a travel condition on an observer the package declares, so the Motor gains that declaration in a level slice.
- **A travel condition whose observer publishes no reached event is refused at admission**, because nothing would wake its evaluation.
- **A travel occurrence is `ActivationTime(Ordinal, 0f)`**, the value its event record publishes. A travel and an activation in the same tick compare by phase.
- **The Motor's travel observer is owned by the Early level proof slice that authors level 14 `battery_motor`.** It is package declaration data.
- **Catalogue operations become definition-scoped.** The bodies of `PuzzleCatalogue.Create`, `Validate`, `WithInstance` and `WithPrecision` move to internal `PuzzleDefinition` instance methods and the statics become one-line delegations. It stays one path, and tests call the methods on `ReadDefinitions(unfamiliar json)[id]`. There is no test-only static install. No definition-scoped method resolves `PuzzleCatalogue.Definition`.
- **The role strings `"puzzle_target"` and `"interaction_target"` (`CompiledIdentityMap.cs:105-107`, `WorkshopPhysicsCompiler.cs:668`) are outside the affected scope.** They select which bodies a declared sensor admits; this slice calls `TryCapture` exactly as today and changes neither site, and no delivered package declares either role. Typing them changes the shared `TargetSelector` contract and `BundleValidator`. This slice records the closed-set defect in `deferred-work.md` with both sites; destination: the first slice that changes `TargetSelector` or residence target selection (unassigned until then).

No decision above cuts scope or waives a gate. The split into five slices follows the protocol's split rule and owner decision 12.

### Owner decisions (21 November 2026)

The owner answered both questions directly.

- **Compiler provisioning: wait for S5.** `flatc --version` here reports 25.12.19 and the schema needs 25.2.10. This slice waits for `spec-s5-generate-wire-accessors-in-build` to publish; there is no one-off download. No open decision remains.
- **The four-part split is accepted.** ENGINE-GOALS stays open until all four slices publish. *Superseded by owner decision 12 (22 November 2026): five slices, the fifth being `engine-goal-count-window-hold`; ENGINE-GOALS stays open until all five publish.*

### At replanning (once S5 is on main)

- **Scope lines.** `engine/gpu/WorkshopAuthoringGenerated.cs` leaves the owned list, and `engine/gpu/WorkshopWireGenerated.cs` leaves the not-owned list: neither file exists. `tools/e2e/workshop-wire.ts` stays, because S5 leaves the generated TypeScript by hand.
- **Task 6.** The C# regeneration step drops; the build generates the accessors from the edited schema. The TypeScript part is still regenerated, with the cached pinned compiler that `schemas/README.md` then names.
- **Probe row 18.** `tools/WorkshopWireGeneration` and `WorkshopWireGenerationTests` are deleted by S5. Re-derive the row from S5's build generation and the test that checks the generated set against the schemas, which must admit `GoalObservationRecord` and `GoalConditionRecord`.
- **Verification.** Drop `WorkshopWireGenerationTests` from the affected filter. The session check reads the cached pinned compiler, not `flatc` on `PATH`.
- **AC6 search.** The terms do not change. The generated C# is no longer tracked, so the search covers hand-written code and the schema; add an exclusion for the intermediate output directory if `rg` reaches it.
- **Citations.** Re-verify every `path:line` in the owned scope and the probe against the new baseline, and re-read `CSharpStaticGateTests`, which S5 edits.

### Running beside other work

Not parallel-safe with an Early level proof slice: both edit `content/puzzles.json` and `LaunchLessonSolveTests.cs`. Publication is serialised. If `spec-early-level-proof-batch-1` publishes first, this slice merges main once and converts every Playable entry then on main (recounted at replanning). If this slice publishes first, that batch authors `goals` in the new shape. S5 is a prerequisite, not a neighbour: this slice starts from the main that contains it.

### Carry-forward

`deferred-work.md` has no open entry naming goal evaluation (`rg -n -i "goal" _bmad-output/implementation-artifacts/deferred-work.md`). Prerequisite: S5 published, which provisions the pinned `flatc` 25.2.10 in the build.

## Tasks & Acceptance

**Execution:**
1. `CuriousContraptions.tests/Fixtures/pre-goal-list/` -- before any schema edit, capture one save of the authored solution construction for each former kind: `first_principles.save.bin`, `delayed_signal.save.bin` and `burst_of_motion.save.bin`, each the raw size-prefixed `CCWS` bytes of `WorkshopSaveCodec.Encode` over `PuzzleCatalogue.CreateSolution` at precision 0.45. Capture them with a scratch test run in a detached worktree of the baseline commit, keep its source as evidence under `_bmad-output/implementation-artifacts/`, and write `README.md` beside the bytes in the form of `Fixtures/f1ad3bd1-provenance.md` -- the old layout cannot be produced afterwards.
2. `engine/gpu/WorkshopPuzzle.cs` -- replace the goal model with: a closed observation kind (`Captured`, `Activated`, `IntegrityBroken`, `TravelReached`); an observation selector (kind, owner, and only the fields of its kind: captured subject, signal node, or local observation identity); a condition (observation, optional earlier observation with minimum duration); and the goal as an immutable, value-equal, ordered condition list. Validation refuses unused fields, duplicates, a predecessor equal to its condition and a minimum outside 0 to 120 -- one typed model.
3. `engine/gpu/WorkshopGoalEvaluator.cs`, `engine/gpu/CompiledIdentityMap.cs` -- one occurrence function per observation kind over the committed read, one conjunction, the existing `ElapsedAtLeast`; keep `Evaluate` and both `Occurrence` entry points for their callers -- the single rule.
   - Add the committed travel read to `WorkshopPresentationSample` and fill it at both `WorkshopPoseHistory` sites, so the evaluator's presentation-sample entry point (`BrowserWorkshopClient.Ui.cs:141`) sees every observation kind the committed-read entry point sees. The presentation-sample entry point takes the `WorkshopPresentationSample` itself (`Occurrence(goal, sample, identities)`), with no optional lane parameter, so `Ui.cs:141` cannot omit an observation kind.
4. `engine/gpu/WorkshopGoalWake.cs` -- the wake is the union of each condition's and predecessor's wake sources -- no polling.
5. `engine/gpu/PuzzleDefinition.cs`, `engine/gpu/PuzzleCatalogue.cs` -- read `goals` with an exact field set per kind; resolve slots to identities by one slot map for fixtures and the single `goal_target` inventory row; validate each condition against the compiled solution (capture declared, node declared, observation declared, reached event declared); apply the anchor decision -- admission.
   - Move the bodies of `Create`, `Validate`, `WithInstance` and `WithPrecision` (`PuzzleCatalogue.cs:25-127`) to internal `PuzzleDefinition` instance methods; each static delegates in one line. The definition-scoped `Create` and `WithPrecision` end with `construction.ValidateStructure(supply, bundle)` and then this definition's own `Validate(bundle, puzzle, construction)`; they never call `WorkshopConstruction.Validate`, which re-enters the installed catalogue (`WorkshopConstruction.cs:24`, `WorkshopPuzzle.cs:90`). `WorkshopConstruction.Validate` is `ValidateStructure` plus `Puzzle.Validate`, so the installed path runs the same two steps in the same order: one path.
6. `schemas/workshop_authoring.fbs` -- delete `goal_kind`, `goal_body`, `goal_target`, `goal_delay`, `goal_source_node`, `goal_target_node`, `goal_integrity` and `IntegrityGoalRecord`; add the condition records and a required `goals` vector in the slot `goal_kind` held. Regenerate `engine/gpu/WorkshopAuthoringGenerated.cs` and the generated part of `tools/e2e/workshop-wire.ts` by the `schemas/README.md` commands -- one save shape.
7. `engine/gpu/WorkshopAuthoringFlatBuffers.cs`, `engine/gpu/WorkshopSavePrevalidation.cs` -- encode, decode and pre-validate the list with the existing rejection reasons; keep the rule that a goal identity need not be a saved part (`WorkshopSavePrevalidation.cs:221-222`).
8. `engine/gpu/WorkshopObservationProjection.cs`, `CuriousContraptions.web/wwwroot/workshop-observation.ts` -- project `goals` as an array of typed conditions; replace the TypeScript enum and type -- one projection.
9. `content/puzzles.json` -- rewrite each Playable `goal` as `goals` with the same meaning (Design Notes) -- current content.
10. `tools/e2e/run-reset-snapshot.ts`, `tools/e2e/unit/run-reset-snapshot.test.ts` -- match the schema, as `schemas/README.md` requires.
   - Also the hand-written dump at `tools/e2e/workshop-wire.ts:12853-12858` and the goal assertions at `tools/e2e/cat-062b.test.ts:252,262,263`.
11. `CuriousContraptions.tests/GoalCompositionTests.cs`, `CuriousContraptions.tests/GoalAdmissionTests.cs` -- write the tests in the Acceptance-to-test table.
12. Existing tests in the owned list -- adapt to the new model without weakening an assertion; update any `PuzzleRecord` field list `WorkshopFormatTests.cs` holds.
13. Documents in the owned list -- state the delivered behaviour, the four follow-up slices and the level counts.
   - ENGINE-GOALS stays open in `epics.md` with its five slices listed. `sprint-status.yaml` already lists all five keys as `backlog` (23 November 2026); this slice changes only `engine-goal-evaluation`'s status. `deferred-work.md` gains the role-string entry and this one: "first end-to-end Solved cue with a `TravelReached` condition: add a `TryUiFrame` assertion to the level-14 slice; a failure there is an engine defect and returns to planning."

**Acceptance Criteria:**
- AC1: Given the 15 Playable levels with converted content, when their existing solve, control, cadence, precision, Reset and save/load theories run, then every one passes unchanged in meaning.
- AC2: Given a goal with no activation condition, when a record arrives from an owner no condition names, then the wake is not due; when a record arrives from any condition's owner, or a lap is reported, then the goal is evaluated against the covering committed read.
- AC3: Given a goal with a capture and a `TravelReached` condition, when only one holds in the presentation sample, then the goal has no occurrence there and the Solved cue stays neutral; when both hold, then the sample's occurrence equals the committed occurrence, which is the condition under which `TryUiFrame` returns the goal opacity; Reset returns the cue to neutral.
- AC4: Given a multi-condition construction, when it is projected to the observation port, then `goals` lists each condition's kind, owners, node, local identity and predecessor with the declared values.
- AC5: Given an authored level whose conditions are reordered, when it is created at another first identity, validated and re-created at another precision, then fixture identities and validation are the same as before the reorder.
- AC6: Given the finished tree, when searched, then `WorkshopGoalKind`, `IntegrityGoalSelector`, `IntegrityGoalRecord`, `goal_kind`, the old goal accessor names in `tools/` and `CuriousContraptions.web/`, and a `"goal"` key in `content/puzzles.json` have zero hits outside history, and no engine file names a part.
- AC7: Given a level whose `goal_target` inventory part is a condition owner, when the player places that part, then the condition binds to the placed identity; before placement the goal is Waiting.

## Spec Change Log

## Review Triage Log

## Design Notes

The eight design answers (E1, E2, E4, E5, E7, E8, E9 and D1 to D5 apply; E3 applies to the wake only; E6, D6 and D7 are unchanged: no law, asset or animation mapping changes).

1. **Behaviour.** A level is solved when all its declared conditions hold; ENGINE-GOALS part 1. Retained: capture, activated-after and integrity outcomes of 15 levels.
2. **Reusable.** One primitive: a condition over a committed first-occurrence observation, with an optional earlier observation. Inputs: the committed read and compiled identities. Output: phase and occurrence. Unit: physical tick ordinal and phase (`ActivationTime`).
3. **Declared.** Level data declares conditions by slot and local identity; packages already declare the sensors, nodes and observers. Identity (slot, local id) is separate from role (observation, predecessor).
4. **Execution.** `goals` JSON → reader → slot map → compiled-identity validation → `WorkshopPuzzle` → evaluator on each woken committed read → one occurrence → existing cue request.
5. **Ownership.** The worker owns observations; the main thread owns evaluation and writes nothing physical. Reset clears observations at the worker, so the goal follows. Content load, edit validation and save load are the three atomic refusal boundaries.
   Writers: the cue is requested and retired only by `BrowserWorkshopClient.Ui.cs:72-73,93-97`, and the saved goal is written only by `WorkshopAuthoringFlatBuffers.EncodePuzzleRecord`. Phase and occurrence are derived on each evaluation, never stored or saved.
6. **Generality.** M1 to M5: a composition no level has, every reordering, one-cause controls, two travel observers with different thresholds, predecessor boundaries, and a goal on a player-placed owner.
7. **Deleted.** `WorkshopGoalKind`, `IntegrityGoalSelector`, the single `WorkshopGoal` shape, `AuthoredGoal`, `ParseIntegrityGoal`, the goal-kind branches at `PuzzleCatalogue.cs:65-67,129-132`, seven `PuzzleRecord` fields, `IntegrityGoalRecord`, the content key `goal`, the TypeScript `WorkshopGoalKind` and `goal` type.
8. **Uncertain.** None required now. Supplied occurrence, progress observation, ordered events (with the any-of/count group) and count, window and hold goals are owned by the four named slices.

**What the deleted checks protected, and what replaces them.** The exact goal field set (`PuzzleDefinition.cs:220-221,243`) becomes an exact field set per kind (M6). `Goal.Validate()` and `Enum.IsDefined` on the saved kind (`WorkshopSavePrevalidation.cs:167`) become per-condition validation (M7). "Authored goal changed" (`PuzzleCatalogue.cs:106`) is kept for the list (M7). Removing seven fields renumbers `assistance` and `interaction_target`; the three committed old-layout saves prove such a save is refused, never misread (M7). If any of them decodes, stop and return the contract to planning.

**Why the three old-layout fixtures discriminate.** In an old save, vtable slot 5 of `PuzzleRecord` holds the inline scalar `goal_kind` (1, 2 or 3). The new reader takes slot 5 as the required offset to the `goals` vector, so the verifier, whose alignment checking is on, refuses the buffer. The expected reason for all three is `WorkshopSaveRejection.VerifierRejected`.

Save records, in slot order:

```
table GoalObservationRecord { kind: uint32; owner: uint64; subject: uint64; node: string; local: uint32; }
table GoalConditionRecord { observation: GoalObservationRecord (required); after: GoalObservationRecord; after_minimum: float; }
PuzzleRecord: 0 id, 1 placement, 2 precision, 3 inventory_part, 4 inventory_count, 5 goals: [GoalConditionRecord] (required), 6 assistance (required), 7 interaction_target
```

Content shape, one example of each kind:

```json
"goals": [
  {"kind": "Captured", "owner": "fixture_2", "subject": "fixture_1"},
  {"kind": "Activated", "owner": "fixture_3", "node": "signal",
   "after": {"kind": "Activated", "owner": "fixture_2", "node": "signal", "minimum_seconds": 1}},
  {"kind": "IntegrityBroken", "owner": "fixture_1", "observation": 1},
  {"kind": "TravelReached", "owner": "rotor", "observation": 1}
]
```

Today's `Captured` goal with `source` S and `target` T becomes owner T, subject S. Today's `ActivatedAfter` becomes an `Activated` condition on the target with `after` naming the source.

## Verification

Executors: the implementation owner runs every command below; the coordinator inspects results; a designated executor repeats the full round only if the candidate changes after handoff. Evidence is invalidated by a change to source, schema, generated files, content, dependencies or build configuration.

**Commands:**
- Session check: `test "$(node --version)" = v26.12.1`, `dotnet --version`, `dotnet workload list` -- expected: v26.12.1, the `global.json` SDK, wasm tools present. The pinned compiler chosen by the owner decision reports 25.2.10 (under S5, the cached binary, not `flatc` on `PATH`).
- The five compiler-log builds of `docs/delivery-workflow.md` "Reproducible local verification" (playtest, solution, web, `CheckStagedTests.cs`, `VerifyPolicy.cs`) -- expected: all succeed with warnings as errors.
- `dotnet run --file tools/anvil/VerifyPolicy.cs -- --maintainability --project-root "$PWD"` -- expected: both canary `PASS` lines, the `Coupling dependencies:` summary, zero `FAIL MI`, `FAIL CC`, `FAIL COUPLING`. Run when the candidate first compiles and again here.
- The registration filter of `docs/delivery-workflow.md` "New-package registration check" -- expected: zero failed, `Skipped: 0` (the slice adds test files and a save field).
- `dotnet test CuriousContraptions.slnx -c Debug -f net10.0 --no-build --no-restore --nologo -noAutoResponse --filter "FullyQualifiedName~GoalCompositionTests|FullyQualifiedName~GoalAdmissionTests|FullyQualifiedName~GoalSignalSelectorTests|FullyQualifiedName~LaunchLessonSolveTests|FullyQualifiedName~F32BehaviourIdentityTests|FullyQualifiedName~WorkshopSaveTests|FullyQualifiedName~WorkshopSaveRejectionTests|FullyQualifiedName~PreConversionLaunchSaveTests|FullyQualifiedName~EventRingConsumerTests|FullyQualifiedName~WorkshopObservationTests|FullyQualifiedName~WorkshopHintTests|FullyQualifiedName~DominoEffectTests|FullyQualifiedName~DelayedSignalTests|FullyQualifiedName~WorkshopFormatTests|FullyQualifiedName~WorkshopWireGenerationTests"` -- expected: zero failed, `Skipped: 0`, every table row's test listed as run.
- `rg -n "WorkshopGoalKind|IntegrityGoalSelector|IntegrityGoalRecord|goal_kind|goal_integrity" -g '!_bmad-output/**' -g '!docs/**' -g '!**/Fixtures/**' .` -- expected: no hits.
- `rg -n "goalKind|goalBody|goalDelay|goalSourceNode|goalTargetNode|goalIntegrity|addGoal" tools CuriousContraptions.web -g '!**/node_modules/**'` -- expected: no hits.
- `rg -n '"goal":' content/puzzles.json` -- expected: no hits.
- `git add -N` the new files, then `git diff --check <baseline_revision>` -- expected: no output.
- One full local round under `caffeinate -i`: the `local_deferred_filter` and `dotnet test CuriousContraptions.slnx ...` lines of the same section -- expected: zero failed, `Skipped: 0` in every project. The total rises by the new tests only; record the delta and its reason at handoff.

### Acceptance-to-test table

Tolerances chosen now. Stall guard for every simulated wait: 10 game seconds converted by the selected cadence (the existing lesson guard; the longest causal chain here, a fan spinning a rotor through its threshold, takes about 3 s). Occurrence comparisons are exact because an occurrence is an integer ordinal and an authored phase of one committed read. Elapsed-minimum boundaries are exact by the timer contract. No position or speed is asserted.

**Where a multi-condition level is exercised.** No installed level has more than one condition, and none may be authored here. M1 to M5 and M8 run a Free-workshop construction through the real simulation and evaluate a test-declared goal. M6, AC5 and AC7 use `ReadDefinitions(unfamiliar json)[id]` and its definition-scoped methods. For a multi-condition goal, the malformed-record cases of M7 and the three old-layout fixtures run the full `WorkshopSaveCodec.Decode`, because they are refused before the catalogue is consulted; only the changed-goal cases of M7 (dropped, added, reordered condition) and the M9 round trip use `OpenFrame` (the verifier), pre-validation, `MapConstructionRecord` and the definition-scoped `Validate`, because `Decode`'s last step validates against the installed catalogue. Full decode and replay are proven on the installed levels (AC1). AC3 is proven at the evaluator's presentation-sample entry point; `TryUiFrame` itself runs on installed levels.

| Rows | Named discriminating test | Control | Boundary | Run |
|---|---|---|---|---|
| M1 | `GoalCompositionTests.EveryConditionMustHoldAndTheGoalOccursAtTheLatest` | Phase is Waiting in the read before the last condition holds | A goal of exactly one condition solves on that condition | planned |
| M2 | `GoalCompositionTests.DeclaredOrderDoesNotChangePhaseOrOccurrence` (theory over the orders) | A goal with one condition replaced gives a different occurrence | Two conditions occurring in the same tick | planned |
| M3 | `GoalCompositionTests.OneUnmetConditionKeepsTheGoalWaiting` (theory per condition) | M1 solves with the cause restored | The unmet condition is the last declared and the first declared | planned |
| M4 | `GoalCompositionTests.APredecessorBoundaryIsExactOnConstructedOccurrences` (constructed dyadic `ActivationTime` values at the evaluator); existing `DelayedSignalTests.DyadicMinimumDoesNotRoundElapsedBeforeComparison` | Earlier observation absent; order reversed | Gap one phase step below, exactly at and above the minimum; minimum 0 and 120 | planned |
| M4 | `GoalCompositionTests.APredecessorMustOccurFirstByItsMinimum` (real simulation) | Earlier observation absent; order reversed | A minimum several ticks shorter and several ticks longer than the simulated gap; no exact boundary | planned |
| M4 | `GoalCompositionTests.ATravelAndAnActivationInOneTickCompareByPhase` (constructed read) | With the activation as predecessor of the travel, the condition does not hold | Travel at phase 0 precedes an activation later in the same tick | planned |
| M4 | `GoalCompositionTests.AnActivationWithoutAPredecessorHoldsOnItsNodeAlone` | The node is clear: Waiting | A second node of the same owner latched instead: Waiting | planned |
| M5 | `GoalCompositionTests.ATravelConditionHoldsWhenItsDeclaredObserverIsReached` (two observers, different thresholds) | The driving cause removed: Waiting | The read before Reached is Waiting; the smaller threshold holds first | planned |
| M6 | `GoalAdmissionTests.MalformedAuthoredGoalsAreRefusedAndTheCatalogueIsUnchanged` (theory per defect) | The same content without the defect loads | Minimum 0 and 120 admitted; the next f32 below and above refused | planned |
| M7 | `GoalAdmissionTests.AChangedOrMalformedSavedGoalIsRefusedWithItsTypedReason` (theory per defect); `GoalAdmissionTests.AnOldSingleGoalSaveIsRefusedAndNeverDecoded` (three fixtures) | The unmodified save decodes equal | A dropped condition, an added condition, a reordered list | planned |
| M8 | `GoalCompositionTests.ResetReturnsEveryConditionToWaitingAndTheGoalSolvesAgain` | Without Reset the goal stays Solved | Reset in the tick after the goal occurrence | planned |
| M9 | `GoalAdmissionTests.AMultiConditionGoalRoundTripsThroughSaveAndSolvesAfterLoad` | A forged save with one condition altered is refused | A goal with a predecessor on every condition | planned |
| AC1 | Existing `LaunchLessonSolveTests.AuthoredLessonSolvesAndOneMissingCausePreventsTheGoal`, `F32BehaviourIdentityTests.ALessonPreservesGoalsAndCausalEventOrderAndReplaysAfterSaveLoad`, `WorkshopSaveTests.EveryLessonAdmitsItsAuthoredPlacementsInventoryGoalAndKnotsAsF32` | Their existing one-missing-cause controls | Three cadences and three precisions | planned |
| AC2 | `GoalCompositionTests.TheWakeIsTheUnionOfItsConditionsOwners`; existing `EventRingConsumerTests.EveryAuthoredGoalSolvesOnItsWakeExactlyWhenPollingWould` | A record from an unrelated owner is not due | A reported lap makes it due at once | planned |
| AC3 | `GoalCompositionTests.ThePresentationSampleCarriesTravelAndGatesTheCueOnEveryCondition` (capture plus `TravelReached`, samples from the real pose history); for `TryUiFrame` on installed levels, the existing `LaunchLessonSolveTests` goal-cue assertions and `WorkshopHintTests.CommittedBuildingClearsSolvedWithEitherAcknowledgementOrder` | One condition held: no occurrence in the sample | The sample's occurrence equals the committed occurrence; after Reset there is none | planned |
| AC4 | `WorkshopObservationTests.ProductionConstructionProjectionIncludesPuzzleConfigurationAndConnectionDomains`, extended to the list | A one-condition goal projects one entry | A condition with and without a predecessor | planned |
| AC5 | `GoalAdmissionTests.FixtureIdentitiesDoNotDependOnTheGoal` | An unlocked placement is refused at content load; the same reordered definition under an id the installed catalogue does not hold creates and validates | First identity 1 and 11; precision 0, 0.45 and 1 | planned |
| AC6 | The `rg` command above; the Element-Free Engine Auditor reads the diff for part identity | not a test | not a test | planned |
| AC7 | `GoalAdmissionTests.AConditionOnAPlayerPlacedOwnerBindsWhenItIsPlaced`; existing `ReadyReceiver` and `ReadyMissingReceiver` routes | Owner not placed: Waiting, cue absent | Two conditions sharing the placed owner | planned |

Proof limits: native C# simulation and compiled TypeScript only. Browser, performance, remote build, artifact and deployment qualification are unverified and stay at their named gates. The `tools/e2e` edits are unverified by any required run until the browser phase (`tools/E2eRunner/BrowserDeferral.cs:27-28,42`). `TryUiFrame` with a travel condition is first executed end to end by the level slice that installs one.
