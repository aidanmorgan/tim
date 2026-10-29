---
title: 'test-workshop-harness-b: complete current-catalogue behavioral integration'
type: 'refactor'
created: '2026-10-29'
status: 'done'
baseline_revision: 'db90de9b231e218025ef72a1007f953e214baee6'
review_loop_iteration: 0
followup_review_recommended: false
context:
  - '{project-root}/AGENTS.md'
  - '{project-root}/docs/testing-guidance.md'
warnings: [oversized]
deferred: []
---

<intent-contract>

## Intent

**Problem:** The shared native production harness is delivered, but coverage does not yet prove every current lesson and admitted element interaction at each supported cadence. Authored solutions still use a duplicate test-only parser.

**Approach:** Complete harness B's current-catalogue positive/control/lifecycle matrix through harness A. Read current authored solutions through the production canonical instance reader, deleting the duplicate format/parser and migrating its actual callers.

## Boundaries & Constraints

**Always:** Cover actual playable-lesson and admitted-registry membership, not a count or representative per family. Preserve poses, parameters, assistance, inventory, goals, authored Reset, f32/integer-time semantics and demand growth. Assert meaningful outcomes, causal owners/kinds/order and useful declared presentation; use generous finite game-second guards. Keep precision0/.45/1 evidence without multiplying every cadence/control/lifecycle case by every precision. Reuse existing adequate scenarios and A's production path. Local tests, normal hooks and independent Auto review remain required.

**Never:** No solver mock, copied runner/parser, compatibility format, old-name wrapper, output hash/byte identity, historical trajectory/tick pin, mutant, browser gate, new coverage platform or unrelated cleanup. Do not add future elements or a player-facing load-solution feature. Do not wait for or investigate CI/deployment before continuing (owner29October); report remote qualification as unverified. No local Release worker build or web publish.

## I/O & Edge-Case Matrix

| Scenario | Input/state | Expected behavior | Error handling |
|---|---|---|---|
| Lesson membership | Actual PuzzleCatalogue.Playable | Every current lesson has an explicit executed positive/control scenario | Missing/extra identity fails; count alone insufficient |
| Lesson gameplay | Production-read solution at60/120/240Hz | Goal, required causal events and useful presentation occur; preserved precision evidence | Finite generous stall guard, not historical arrival time |
| Lesson control | Remove one necessary placed part and its incident wires, or disconnect one necessary connection; other admitted input unchanged | Goal remains unsolved throughout meaningful window | Control remains a valid construction; no all-parts removal shortcut |
| Element membership/interactions | Every admitted registry part in its required cross-element use at all three cadences | Actual mechanical/routing/electrical/logic/time consequence and one-cause control | Mere placement/presence is not coverage |
| Lifecycle | Demonstrated motion/state change before Pause/Step/Reset and Save/fresh Load where supported | Correct phase progression, exact authored restoration, retired old effects, semantic replay | Existing typed admission and ownership rules |
| Canonical content | Explicit fixture/solution slots, current canonical instances and typed links | Production reader builds the intended solution with preserved authored values/assistance | Unknown/duplicate slots, parts, nonfinite parameters, invalid inventory/counts/endpoints/domains and old solution shape refuse atomically |
| Palette | Actual bundle/resources and native palette controls | F.16 grouping/display order and switch title/description/colour/icon match independent intended values | No self-comparison to the same declaration |
| Pair order/Reset | Reordered equivalent physical interaction; moving body before Reset | Same meaningful interaction result and exact authored restoration | No private BVH order or identical-trajectory requirement |

</intent-contract>

## Code Map

- Baseline published main: `db90de9b231e218025ef72a1007f953e214baee6`. Work in the `story-native-harness-b` worktree; do not edit the stale root checkout. A's local4901/0/0, hooks126/0/0 and exact-SHA approval passed. Remote CI/deployment is not a progression gate by the latest owner instruction.
- Authority: [stabilization contracts](../planning-artifacts/epics.md#stabilization-contracts), [integration guidance](../../docs/testing-guidance.md#integration-tests-the-system-working-as-a-whole-without-a-browser). Read affected source acceptance, including F.16 ownership near epics.md:1978 and F.4 near4718, rather than the complete historical requirements file.
- `engine/gpu/PuzzleDefinition.cs:56` validates fields but does not parse solution contents. `AuthoredInstanceTemplate.cs:84` already parses canonical part/cell/local/quaternion/locked/scalars/parameters/assistance/bindings. Reuse it for solution `slot/instance` rows. Keep explicit typed fixture/solution slot references; delete implicit unique-part aliases.
- `PuzzleCatalogue.cs:19` constructs fixed layouts; expose production-read typed solution placements/connections or construction through the existing catalogue. Validate the complete solution against inventory, goal and connection rules before admission. No second element-kind switch.
- `content/puzzles.json`: convert every current solution from kind/position/3x3-orientation/properties/difficulty to the existing canonical representation; preserve authored meaning and update current connections to explicit slots. Old content is refused, never interpreted by a compatibility path.
- `CuriousContraptions.tests/LaunchLessonSolveTests.cs:61` owns the duplicate parser; delete it and move its callers, including `EventRingConsumerTests`, `F32BehaviourIdentityTests` and A's receiver scenario, directly to the production reader. Existing lesson precision and semantic replay coverage should be consolidated, not duplicated.
- Source-content consumers: `JoinedPipeTests`, `DelayedSignalTests`, `PipeBend45Tests`, `PipeBend90Tests`, `WorkshopSaveTests`, `WorkshopSpringboardTests`, `ClearPipeLevelTests`, `WorkshopBumperTests`; update current solution fields while preserving their actual obligations.
- Reuse `WorkshopIntegrationHarness`, `WorkshopIntegrationClient` and actual Workshop awaited operations. A already covers startup/refusal, cancellation, sequence/cadence authority, memory and retained buffers; do not recreate that matrix.
- Mechanical/routing test owners under `CuriousContraptions.tests/`: `PayloadInstanceTests.cs`, `WeightTests.cs`, `WorkshopBumperTests.cs`, `WorkshopSpringboardTests.cs`, `WorkshopWallTests.cs`, `DominoGuardTests.cs`, `HollowElementTests.cs` and `LaunchLessonSolveTests.cs` (ramp/receiver routes).
- Supply/logic owners there: `SourceStoreInstanceTests.cs`, `SwitchElectricalTests.cs`, `LatchSupplyFixtureTests.cs`, `PressurePlateTests.cs`, `BothGateTests.cs`, `ElectricalNandTests.cs`, `ElectricalNorTests.cs`, `ElectricalOrTests.cs`, `ElectricalXorTests.cs`, `LampDetectorGuardTests.cs`, `DeclaredPrismaticDriveTests.cs`.
- Time/count owners there: `ClockGuardTests.cs`, `HoldTimerTests.cs`, `WorkshopDelayTests.cs`, `CounterTests.cs`. Inspect their actual required cross-element acceptance before extending missing rows.
- Extend existing element integration owners for missing named interactions/cadences/controls/lifecycle. `PartRegistryTests.cs` and native palette tests own F.16; `NoFixedCapacityTests.cs` and affected collision tests own moving Reset/pair-order behavior. Register new files in the explicit test csproj.
- Current lessons: first_principles, spring_forward, domino_effect, bumper_sidekick, bumper_depth, wall_and_bumper, delayed_signal, clear_pipe, gentle_bend, quarter_bend, joined_pipe, ready_to_rebound.
- Current admitted parts: ball, ball_detector, basket, battery, both_gate, bowling, bumper, clock, counter, delay, domino, electrical_nand, electrical_nor, electrical_or, electrical_xor, funnel, hold_timer, lamp, latch, pipe, pipe_bend_45, pipe_bend_90, powered_gate, pressure_plate, ramp, spring, switch, wall, weight. Use typed PartId/PuzzleId identity sets against actual catalogue membership, not these counts as acceptance.
- Coordinator owns delivery-policy clarification, TODO/sprint/epic updates and A's publication receipt. Keep these separate from implementation edits.

## Tasks & Acceptance

**Execution:**
1. `PuzzleDefinition.cs`, `PuzzleCatalogue.cs`, `content/puzzles.json` — adopt the canonical solution representation and typed references; reject malformed/old inputs.
2. `LaunchLessonSolveTests.cs` and listed consumers — remove duplicate parsing and prove every lesson's production solution/control at each cadence, retaining existing difficulty evidence.
3. `CuriousContraptions.tests/` mechanical, supply/logic and time/count files named in Code Map — close actual membership gaps using A; assert the named part's consequence with matched controls and lifecycle, including downstream supplied logic and intentional timer/count boundaries.
4. `PartRegistryTests.cs`, native palette and `NoFixedCapacityTests.cs`/collision owners — close the named F.16/F.4 behaviors without browser or private-state substitutes.
5. Test csproj and this slice record — register cases; record a short identity→scenario/positive/control/lifecycle map and any deleted assertion→replacement mapping. No new manifest/generator/framework.

**Acceptance Criteria:**
- Given every current playable lesson and admitted part, when the registered native scenarios execute at60/120/240Hz, then each identity has direct meaningful positive/control coverage through the shared production path.
- Given saved/reset scenarios after demonstrated change, when reset or loaded into a fresh harness, then authored fields restore exactly and required gameplay outcomes recur without historical trajectory pins.
- Given canonical solution content and invalid controls, when production readers and command admission execute, then valid content preserves authored intent and malformed/unsupported content is rejected before partial admission.
- Given the frozen candidate, when affected checks, final full local round, five Auto layers and normal hooks complete, then all required tests pass with zero skips and no unresolved required-now findings. Publish the approved commit and continue without waiting for CI.

## Spec Change Log

## Review Triage Log

### 29 October 2026 — Review pass
- verdicts: 7 findings — high0, medium4, low0, false3, maybe-false0
- findings:
  - `[medium]` `[patch]` Blind Hunter: hold-window continuity is not asserted — the test checks initial supply and post-expiry opening but permits early opening after blade movement. Original implementer will assert the continuous authored window at each cadence.
  - `[medium]` `[patch]` Edge Case Hunter: joined-pipe source pose pins impose unjustified precision — the .0002 position and .004 angle checks do not derive from functional clearance. Original implementer will replace them with geometry-based mouth/drop-line checks while preserving passage and unsnapped controls.
  - `[medium]` `[patch]` Verification Gap: inventory configuration mismatch has no canonical-reader refusal case — the existing19 cases use an unconfigured inventory and cannot catch removal of the precision-knot equality rejection. Original implementer will add valid-but-mismatched ready_to_rebound content, including a non-default knot.
  - `[medium]` `[patch]` Intent Alignment: lesson outcome presentation is not evaluated — goal/event checks cannot catch missing declared goal cues. Original implementer will assert meaningful evaluated capture/activation presentation and neutral/control/Reset behavior in the existing lesson matrix.
  - `[false]` `[reject]` Intent Alignment: codec replay does not consume Save data or execute Load — this describes the supported native interface correctly, rather than a missing native operation. WorkshopSimulation.Save is an acknowledgement; UI persistence serializes World.Construction and load decodes then submits Construct. The matrix asserts bench equals harness.Construction after Reset, acknowledges Save, decodes that construction and installs it through fresh Construct. Browser storage/UI qualification is explicitly deferred.
  - `[false]` `[reject]` Intent Alignment: catalogue-wide stale-effect injection is absent — the intent explicitly permits reuse of adequate A scenarios. A's RealImpactDeliverySurvivesPauseAndResetRetiresItsWorld independently checks evaluated rest state, epoch replacement and old-event refusal through the same generic presentation path; B changes no retirement algorithm. Additional lesson outcome/Reset observations are included in the presentation patch.
  - `[false]` `[reject]` Intent Alignment: part membership is manually associated with scenarios — an executable coverage platform is neither required nor intended. The explicit identity-to-scenario map was checked against assertions and actual round1 TRX: all29 identities' named interactions and all three cadences executed. The manual membership list alone was never used as execution proof.
- Forward-Only Auditor reported no violations after checking current production readers, all12 playable declarations, migrated callers, palette and test fixtures.
- All five layers were launched before triage. Repairs were sent together to the original implementer, harness_b_dev; affected independent re-review follows. No mutants or browser checks were run.

## Design Notes

Keep the behavioural map explicit and small. Existing tests can satisfy a row when their actual assertions and executed cadence/control/lifecycle coverage establish it; add only the missing behavior. A part participating incidentally in another case does not prove its named interaction. Positive runs may stop at their condition; negative controls run the full intended guard. Exact feature timing is retained only for timers, holds, clocks and cadence.

## Verification

Implementer: incremental Debug builds of the affected native/production closure with `-noAutoResponse`, then affected native tests using `--no-build --no-restore -c Debug -f net10.0` and TreatNoTestsAsError. Keep raw results and failures. No full solution run by the implementer.

Coordinator: matching compiler captures, one final full local solution round per [local authority](../../docs/delivery-workflow.md#local-test-authority), actual matrix membership audit, five Auto layers and affected re-review, normal hooks and exact-commit approval. Reuse unchanged proof. Push approved work; do not monitor or wait for CI/deployment. Browser/device/release qualification remains unverified.

## Implementation map — 29 October 2026

The frozen intent contract is unchanged. Actual additional files are `CatalogueIntegrationTests.cs` (registered in the explicit test project), `WorkshopIntegrationHarnessTests.cs` (native palette controls), `ui/Workshop.cs` (category headings in the existing palette path), `WeightTests.cs`, `BothGateTests.cs` and `NoFixedCapacityTests.cs`.

All rows below execute at 60/120/240 Hz through harness A. The lesson identity set is checked against `PuzzleCatalogue.Playable`; the combined typed part identity set is checked against actual bundle membership.

| Identity | Executed positive / one-cause control | Lifecycle |
|---|---|---|
| first_principles; ramp | Authored two-ramp goal / remove first ramp | Pause/Step after demonstrated motion, exact Reset, Save refusal during Run, Save after Reset, fresh decoded construction solves |
| spring_forward; spring | Authored passive bounce goal / remove spring | Same lesson lifecycle |
| domino_effect; domino | Toppled end domino activates lamp / disconnect its lamp wire; source/target kind and shared physical cause asserted | Same lesson lifecycle |
| bumper_sidekick, bumper_depth; bumper | Paid routing to receiver / remove bumper | Same lesson lifecycle |
| wall_and_bumper; wall | Wall-assisted rebound goal / remove wall | Same lesson lifecycle |
| delayed_signal; delay | Physical switch then elapsed timer lights target / disconnect switch-to-delay; minimum duration and provenance asserted | Same lesson lifecycle |
| clear_pipe, gentle_bend, quarter_bend, joined_pipe; pipe, pipe_bend_45, pipe_bend_90 | Authored routes solve / remove the granted routing part | Same lesson lifecycle |
| ready_to_rebound; basket | Receiver captures / remove receiver | Same lesson lifecycle |
| ball, bowling, basket | Each payload captured / offset payload misses; both equivalent placement orders | Demonstrated velocity before Reset, exact restored fields; fresh Save/Load replay |
| funnel, ball_detector, lamp | Offset ball guided through detector lights lamp / remove funnel | Exact Reset and fresh Save/Load replay |
| battery, switch, both_gate, electrical_nand, electrical_nor, electrical_or, electrical_xor, powered_gate | Actual supplied logic opens blade; physical switch conditions where applicable / disable only bottom supply; output lamp visibly on/off | Exact Reset and fresh Save/Load replay |
| switch, latch, lamp, powered_gate | Physical impact sets supplied latch, blade opens and lamp lights / disconnect only SetIn | Exact Reset and fresh Save/Load replay |
| clock, counter | Half-second pulses count to two, then supplied counter opens blade / disconnect only clock supply | Intentional count/timing boundaries, exact Reset and fresh Save/Load replay |
| hold_timer, delay | Delayed physical impact holds supplied contact for half a second / remove only timer supply | Exact due-boundary expiry, no replay after expiry, exact Reset and fresh Save/Load replay |
| weight, pressure_plate | Weight directly loads plate and opens supplied gate / move Weight beside plate | Exact Reset and fresh Save/Load replay |
| weight, ramp, domino | Weight rolls down ramp and topples tile / move only Weight to resting pose | Existing owner extended to each cadence; exact Reset |
| clock, latch, both_gate | Clock Set/Reset and supply cut/restoration propagate through downstream Both gate | Existing owner extended to each cadence; defined one-commit route latency |

F.4: the beyond-instance-capacity construction now includes a visibly falling payload before exact Reset. Pair-order proof compares meaningful capture/miss outcomes for equivalent reordered physical constructions, not private BVH order or trajectories. F.16: the actual native palette has Motion, Goals, Power, Control and Structure headings, retaining declared display order inside each group; visible switch title, intended description/colour and package icon are independently pinned.

Canonical admission reuses `AuthoredInstanceReader` and the current wire instance constructor. Raw authored parameters validate before assistance bindings; each supported precision knot validates afterwards. This admits typed pipe scalars without a second part-kind switch. Explicit solution slots and typed domain/socket links validate inventory, target ownership, capture/node capabilities and connection admission before exposure. Malformed slots, part/parameter/field values, counts, goal capabilities, endpoints/domains and old solution shape refuse atomically.

Deleted assertion → replacement:
- Duplicate `LaunchLessonSolveTests.AuthoredSolution` parser and unique-part slot aliases → `PuzzleCatalogue.CreateSolution`; actual consumers call it directly.
- Duplicate source difficulty-profile helpers → current `AuthoredContentAssertions.Profile`, reading canonical assistance arrays.
- Launch's duplicate three-precision solve/replay test → existing `F32BehaviourIdentityTests.ALessonPreservesGoalsAndCausalEventOrderAndReplaysAfterSaveLoad` (all 36 identity/precision rows retained).
- Harness A's three receiver-only scenarios → the ready_to_rebound rows of the complete lesson cadence matrix.
- Weight control's sub-degree tilt assertion → recognisably upright (<15 degrees) versus toppled (>45 degrees); no historical resting-angle requirement. Its ramp remains present in the control, changing only the Weight pose.

Source-content preservation check: all puzzle identities/order, all planned rows, all non-solution playable fields and each solution's placement count match the baseline semantically. No browser, mutant, local Release worker build or web publish was run. Remote build/artifact/deployment and browser qualification remain unverified.

## Coordinator verification and matrix audit — 29 October 2026

The complete staged diff was inspected, including canonical content, production admission and palette changes. `git diff --cached --check` passed. SDK10.0.403 and Node26.12.1 were used. All five matching compiler captures completed successfully; solution, playtest and compile-only web builds reported zero warnings/errors. No Release worker build or web publish was run.

Full local round1 passed **4,980/0/0**: main4,489 (20m49s), WorkerAotGuard17, Coverage77, BundleElements206 and E2eRunner191. Raw results are each project's `TestResults/native-harness-b-full-round1.trx`; execution session81284 exited0. Compiler captures are retained in that session's `$local_gate_logs` directory.

```sh
export CSHARP_STATIC_GATE_BINLOGS="$local_gate_logs"
local_deferred_filter=$(dotnet run --project tools/E2eRunner --no-build -c Debug -- deferred-filter)
dotnet test CuriousContraptions.slnx -c Debug -f net10.0 --no-build --no-restore --nologo -noAutoResponse --filter "$local_deferred_filter" --logger 'trx;LogFileName=native-harness-b-full-round1.trx' -- RunConfiguration.TreatNoTestsAsError=true
```

The actual main TRX confirms every matrix row's covering tests ran and passed:
- All12 named lessons at60/120/240Hz:36 positive/control/lifecycle cases; explicit lesson membership passed. Existing36 precision cases also passed.
- All33 catalogue interaction/cadence cases passed (five supplied logic kinds and six other interactions at three cadences), plus explicit29-part membership. These execute the positive/control, meaningful presentation, Reset and fresh Save/Load assertions described in the implementation map.
- Weight/ramp/domino and clock/latch/Both cases each passed at all three cadences.
- All19 canonical malformed-solution cases passed; actual native palette grouping/display assertions passed.
- Pair-order and moving Reset passed in all three payload cases; all12 retired-capacity boundary cases passed, including the changed moving-instance Reset.

No required matrix case was missing, filtered or skipped. Independent Auto review, normal hooks and publication remain pending. CI is not monitored or awaited; remote and browser qualification remain unverified.

### Review repair verification

The original implementer changed only CatalogueIntegrationTests, WorkshopSaveTests, JoinedPipeTests and LaunchLessonSolveTests. The coordinator inspected the complete repair diff. Matching incremental Debug build passed with zero warnings/errors (9.42s); the affected hold, receiver-mismatch, joined-pipe and all36 lesson-cadence cases passed **53/0/0** in1m16s. Existing joined-pipe passage and unsnapped controls remain.

All four finding owners independently re-reviewed the actual repairs and found their findings resolved, with no concrete repair regressions: Blind Hunter (continuous supplied/control hold window), Edge Case Hunter (functional pipe mouth/drop-line clearance), Verification Gap (independently valid inventory mismatch including non-default precision), Intent Alignment (evaluated bright/neutral/control/Reset lesson cues). Intent Alignment also independently confirmed the native Save acknowledgement/decoded Construct seam and A's stale-event/evaluated Reset coverage.

Final full local round2 passed **4,982/0/0** (main4,491 in20m49s; tools491), execution session8247 exit0. Changed solution/playtest compiler captures were refreshed with zero warnings/errors; unchanged web/file-tool captures were reused. Raw results: each project's `TestResults/native-harness-b-full-round2.trx`. The actual TRX confirms all36 lesson/cadence cases,34 catalogue cases,36 precision cases, two new valid-mismatch cases, functional joined-pipe case, palette,12 capacity boundaries and both three-cadence owner scenarios passed. Normal hooks and exact-SHA publication approval remain pending. No CI monitoring, browser checks or mutants.

## Implementer verification — 29 October 2026

Baseline: `db90de9b231e218025ef72a1007f953e214baee6`; candidate is the uncommitted B worktree for independent Auto review. No full solution round or normal hooks were run by the implementer; those remain coordinator-owned. No known required-now implementation failure remains after the scoped repair run.

Matching final incremental build command:
```text
dotnet build CuriousContraptions.tests/CuriousContraptions.Tests.csproj -c Debug --no-restore --nologo -noAutoResponse -v:q
Build succeeded.
    0 Warning(s)
    0 Error(s)

Time Elapsed 00:00:06.10
```

Broad affected command (before the final motion-test correction and the two owner-test cadence extensions):
```text
dotnet test CuriousContraptions.tests/CuriousContraptions.Tests.csproj --no-build --no-restore -c Debug -f net10.0 -noAutoResponse --filter 'FullyQualifiedName~LaunchLessonSolveTests|FullyQualifiedName~F32BehaviourIdentityTests.ALesson|FullyQualifiedName~WorkshopSaveTests|FullyQualifiedName~CatalogueIntegrationTests|FullyQualifiedName~WorkshopIntegrationStartupTests|FullyQualifiedName~NoFixedCapacityTests.OnePast|FullyQualifiedName~EventRingConsumerTests.EveryAuthoredGoal|FullyQualifiedName~JoinedPipeTests|FullyQualifiedName~ClearPipeLevelTests|FullyQualifiedName~DelayedSignalTests|FullyQualifiedName~WorkshopBumperTests|FullyQualifiedName~WorkshopSpringboardTests|FullyQualifiedName~PipeBend45Tests|FullyQualifiedName~PipeBend90Tests' -- RunConfiguration.TreatNoTestsAsError=true
Failed!  - Failed:     3, Passed:   362, Skipped:     0, Total:   365, Duration: 3 m 10 s - CuriousContraptions.Tests.dll (net10.0)
```
All three failures were `AuthoredLessonSolvesAndOneMissingCausePreventsTheGoal`, ready_to_rebound at60/120/240Hz: `Assert.Contains() Failure: Filter not matched in collection` at the new first-tick downward-velocity assertion. The charged spring correctly moves upward immediately. The motion contract now checks nonzero velocity in any direction; goals, provenance, cadence and restoration are unchanged. This is a test-contract correction, not a solver change or a fitted numerical margin.

Final affected repair/extension command, after the matching build above:
```text
dotnet test CuriousContraptions.tests/CuriousContraptions.Tests.csproj --no-build --no-restore -c Debug -f net10.0 -noAutoResponse --filter 'FullyQualifiedName~LaunchLessonSolveTests.AuthoredLessonSolvesAndOneMissingCausePreventsTheGoal|FullyQualifiedName~WeightTests.AWeightRollingOffARamp|FullyQualifiedName~BothGateTests.CommittedLatchSetReset' -- RunConfiguration.TreatNoTestsAsError=true
Passed!  - Failed:     0, Passed:    42, Skipped:     0, Total:    42, Duration: 1 m 10 s - CuriousContraptions.Tests.dll (net10.0)
```
This re-executed all36 lesson cadence rows and the six Weight/ramp/domino and clock/latch/Both rows. The unaffected362 passing cases from the broad run are retained as scoped evidence (33 overlap the repaired method's rerun); there are371 distinct passing affected cases across those two runs. The coordinator still runs the required full local round, independent five-layer Auto review and normal hooks.

Earlier diagnostic results retained:
- Initial build: 0 warnings, 0 errors. A test invocation made before rebuilding a changed test was not accepted as evidence: `Failed:61, Passed:89, Skipped:0, Total:150`; startup reported `Puzzle 'clear_pipe.pipe_1', field 'instance': Missing or unknown declared parameters.` Fixed by using the current canonical instance constructor for typed scalar/generic parameter admission.
- Another invocation after a compiler failure was not accepted as evidence: `Failed:1, Passed:9, Skipped:0, Total:10`; `ContentLoadRejectsMalformedDeclarationsBeforeExposure(OutOfRange)` reported `Assert.Throws() Failure: No exception was thrown`. Fixed raw authored validation before assistance bindings; final broad content tests passed.
- Native palette/catalogue check: `Failed:1, Passed:60, Skipped:0, Total:61, Duration:21s`; palette title checked the empty button text instead of its visible child label. Corrected the observable; subsequent affected check passed `47/0/0, Duration:21s`.
- Earlier lesson/content check passed `197/0/0, Duration:2m46s`; applicable coverage was included again in the final broad selection after canonical admission changed.
- Compile diagnostics corrected before final checks: ambiguous target-typed connection constructor and CA1861 on a literal expected array argument. No suppressions or skipped tests were added.
- `git diff --check` passed. Semantic source-preservation check passed: unchanged puzzle identities/order, planned rows, non-solution playable fields and placement counts.

Remote build/artifact/deployment, browser/device and release qualification remain unverified. No browser or mutation run was performed.

## Auto Run Result

Implemented complete current-catalogue native behavior coverage and canonical production-read authored solutions. All12 lessons and29 admitted part identities have explicit executed positive/control coverage at60/120/240Hz; existing precision evidence is retained. Reset, semantic replay, causal ownership, useful goal cues, palette grouping and malformed canonical admission are covered.

Files changed:
- `engine/gpu/PuzzleDefinition.cs`: canonical solution slots/links and inventory/goal admission.
- `engine/gpu/PuzzleCatalogue.cs`: production CreateSolution entry point and typed refusal boundary.
- `engine/gpu/AuthoredInstanceTemplate.cs`: shared canonical instance validation before assistance application.
- `content/puzzles.json`: all current playable solutions use canonical declarations and explicit endpoints.
- `ui/Workshop.cs`: existing palette displays declared category headings.
- `CuriousContraptions.tests/CatalogueIntegrationTests.cs`: named cross-element positive/control, presentation, timing, Reset and replay cases.
- `CuriousContraptions.tests/LaunchLessonSolveTests.cs`: complete lesson/cadence matrix, useful goal cues and duplicate parser deletion.
- `CuriousContraptions.tests/WorkshopSaveTests.cs`: canonical content refusal and independently valid inventory mismatch cases.
- `CuriousContraptions.tests/JoinedPipeTests.cs`: canonical fields and functional mouth/drop-line clearance.
- `CuriousContraptions.tests/BothGateTests.cs`: clock/latch propagation at each cadence.
- `CuriousContraptions.tests/WeightTests.cs`: ramp/domino consequence and one-cause control at each cadence.
- `CuriousContraptions.tests/NoFixedCapacityTests.cs`: demonstrated movement before capacity-case Reset.
- `CuriousContraptions.tests/WorkshopIntegrationHarnessTests.cs`: native palette expectations and receiver-case consolidation.
- `CuriousContraptions.tests/CuriousContraptions.tests.csproj`: registers catalogue integration cases.
- `CuriousContraptions.tests/ClearPipeLevelTests.cs`: canonical solution fields/profile assertions.
- `CuriousContraptions.tests/DelayedSignalTests.cs`: canonical solution fields/profile assertions.
- `CuriousContraptions.tests/EventRingConsumerTests.cs`: direct production solution construction.
- `CuriousContraptions.tests/F32BehaviourIdentityTests.cs`: retained precision cases use the production solution reader.
- `CuriousContraptions.tests/PipeBend45Tests.cs`: canonical solution/profile assertions.
- `CuriousContraptions.tests/PipeBend90Tests.cs`: canonical solution/profile assertions.
- `CuriousContraptions.tests/WorkshopBumperTests.cs`: canonical solution/profile assertions.
- `CuriousContraptions.tests/WorkshopSpringboardTests.cs`: canonical solution/profile assertions.
- `AGENTS.md`: owner's nonblocking CI clarification.
- `docs/delivery-workflow.md`: local authority and nonblocking remote checks.
- `TODO.md`: current candidate, checks and next outcome.
- `_bmad-output/implementation-artifacts/spec-test-workshop-harness-a.md`: published A receipt, remote qualification unverified.
- `_bmad-output/implementation-artifacts/spec-test-workshop-harness-b.md`: intent, implementation map, verification and review record.
- `_bmad-output/implementation-artifacts/sprint-status.yaml`: A/B delivery state.
- `_bmad-output/planning-artifacts/epics.md`: A publication and current sequence.

Review: seven findings, four medium patches applied and independently re-reviewed; three false findings rejected with individual evidence in the triage log. No deferred findings. Forward-only audit passed. Follow-up review recommendation: false after affected independent re-review established all four repairs; no specific unverified repair risk remains. First-pass patched entry counts: high0, medium4, low0.

Verification: affected53/0/0 after repairs; final full4,982/0/0; matching compiler captures zero warnings/errors; actual matrix execution audit passed; `git diff --check` passed. Normal hooks and exact-SHA approval are the remaining publication checks. Browser/device/release and remote artifact/deployment qualification remain unverified, as required by the current stage and owner's nonblocking CI instruction.
