---
title: 'test-workshop-harness-a: one native production integration path'
type: 'refactor'
created: '2026-10-29'
status: 'done'
baseline_revision: '6e0e0ee1799d31211e1769b8883f6bedb3fae396'
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

**Problem:** Existing native integration tests assemble several partial runners and often bypass the production command/wire path. That permits worker command handling, startup, authoring and cross-subsystem lifecycle defects to coexist with passing component tests.

**Approach:** Deliver the authorized harness-A stabilization slice: one deterministic native Workshop harness using the real bundle/content, production command dispatch, wire codecs, device/solver, committed reads, event reader and declared presentation. Migrate existing duplicate integration orchestration to it and delete superseded paths. Keep the complete current-catalogue matrix in harness B.

## Boundaries & Constraints

**Always:** Preserve shipped command/lifecycle behavior, typed identities/refusals, atomic admission, supported authoring Undo/Cancel and Save/Load, current cadence profiles, f32 values/integer time, geometric demand growth and existing element/content behavior. Worker and native host share one command-to-simulation implementation. Authoring uses the production operations and startup executes the real resource-loading/refusal flow. Use explicit deterministic ticks and genuine external-boundary controls. Assertions protect goals, causal events, useful motion/presentation and restoration; timing is exact only where it defines a feature. Preserve existing source acceptance and mapped tests, including F.16 startup and relevant F.4 buffer ownership.

**Never:** No solver mock, copied command switch, fake success response, copied private UI controller, compatibility alias, old runner wrapper or second game path. No new Advance wire command. No output hashes/whole-buffer identity acceptance, private reflection, sleep-based orchestration, mutants, browser gate, new coverage platform, local Release worker build or web publish. Do not expand into harness B's exhaustive current lesson/element matrix, a new solution parser, remaining repository-wide TypeScript conversion or unrelated test hygiene.

## I/O & Edge-Case Matrix

| Scenario | Input/state | Expected behavior | Error handling |
|---|---|---|---|
| Startup | Real generated bundle/resource and native client factory; missing/invalid resource control | Actual Workshop startup reaches Building only with an admitted catalogue; failure visibly reports PARTS UNAVAILABLE and admits no simulation | Real resource/validation reason; no solver/client initialization after failure |
| Authoring | Production placement, parameter edit, wire/unwire, Undo and Cancel | Admitted construction/selection/status and Undo behavior match Workshop; rejected operations preserve prior state | Typed refusal and meaningful visible status; no consumed identity or lost Undo on refusal |
| Production interaction | Small real authored layout plus arrangement through production operations at60/120/240Hz; one-cause control | Encoded commands execute real mechanics, activation, events and declared presentation; intended goal/state/motion occurs within a generous finite window | No fabricated solver/read/event result |
| Lifecycle | Run→Pause→Step→Resume→Reset; supported save/load into fresh harness | Paused world does not advance, Step advances once, resumed interaction continues, Reset restores authored fields and retires old events/effects; semantic saved replay | Existing command admission/ownership rules |
| Wire and authority refusal | Invalid command kind/enum/connection, stale world/revision/cadence/projection | Defined decode/execution refusal; no admitted construction/read/revision/Undo change | Existing typed wire fault or WorkshopRejection at its actual boundary |
| Pending operation | Real installation boundary held by an explicitly completed task | Busy, targeted Cancel, wrong-target AlreadyCommitted and Dispose ownership retain production behavior | Cancellation is atomic; no sleeps or direct private-state fabrication |
| Memory query | Stopped/quiet versus Running or outstanding work | Admit only allowed observations; admitted measurement is real and validated | Existing phase/idle rejection |
| Read/event ownership | Successive real ticks, retained decoded observations, reset/new world | Reused transport storage does not alter retained observations; events carry correct owners/order and do not leak across Reset | Existing epoch/ownership rejection |
| Migration | Existing integration callers moved from duplicate runners | Their behavioral assertions still execute over shared production path; legitimate low-level fixtures remain | No skipped/excluded tests or old-name forwarding wrappers |

</intent-contract>

## Code Map

- Authority: [stabilization rows A/B](../planning-artifacts/epics.md#stabilization-contracts), [integration contract](../../docs/testing-guidance.md#integration-tests-the-system-working-as-a-whole-without-a-browser). F.16 current-stage ownership near epics.md:1978 requires actual Workshop startup refusal in A. F.4 near4718 assigns buffer reuse/pair-order to A/B and moving-body Reset to B; retain current buffer-reuse coverage. Shared clock lifecycle/installation authority: [shared-clock-cadence.md](../../docs/shared-clock-cadence.md), especially world projection and reliable schedule control.
- `CuriousContraptions.Simulation/Program.cs:155–260`: Execute/Apply own stale admission, pending identity, Cancel, Busy, ObserveMemory, simulation creation, cadence allocation and the command switch; EncodeResponse owns ACK metadata. Extract their shared command behavior to one instance `engine/gpu/WorkshopCommandExecutor.cs`, alongside `WorkshopCommandRouter.cs`. Both worker and harness dispatch bytes through that router and executor.
- `Program.Schedule.cs`: keep JS pose preparation, recipient receipts, commit/abort/deadlines and publication transport in the worker host. Typed host seams supply current identity/installation status, quiescence, post-command installation, run-loop ownership and real memory capture. Preserve worker trace/GC boundaries. A host callback must not independently execute commands.
- `WorkshopSimulation`, `WorkshopGpuDevice`, `BrowserGpuTransport`, `WorkshopWireFlatBuffers`, `WorkshopMessages`, `WorkshopCommandRouter`: reuse real contracts. Explicit tick advance calls Simulation.Advance and encodes/decodes its real response with current metadata. There is no wire Advance command. Factor response construction once if needed; do not fabricate transport identities in tests.
- `CuriousContraptions.tests/WorkshopIntegrationHarness.cs` and `WorkshopIntegrationHarnessTests.cs`: one owned in-process client/transport over that shared executor, deterministic cadence-aware clock, real device and defined observations. Existing integration StepClock advances on every capture by a fixed8,333,333ns; the new harness advances integer time explicitly with ticks, not incidental read calls.
- `engine/PartRegistry.cs:47–58`, `NativeSceneFixture`, `scenes/workshop.tscn`: load the actual bundle through PartRegistry and real Godot resources. `ui/Workshop.cs:ReadyAsync` catches resource/validation failure and shows PARTS UNAVAILABLE before initialization. Supply a genuine bundle resource path and client factory before scene attachment, with production defaults, and await actual startup completion. Keep _Ready as the catching boundary over ReadyAsync. Native tests traverse actual labels/buttons and inspect public World phase; no private reflection or fabricated startup-failed callback.
- `engine/MachineWorld.Gpu.cs:61`: existing InitializeWorkshop(Func<Task<IWorkshopClient>>) admits a real in-process transport; use it for native scene startup. `CaptureInstance` and `ReplaceConstruction` retain actual construction/revision transaction behavior.
- `engine/PlacementPose.cs:Compose`, `ui/WorkshopBenchPlacement.cs:Propose`, `ui/Workshop.cs:ClickAsync/UndoAsync`, `ui/WorkshopConfiguration.cs:ApplyDeclaredParametersAsync`, `ui/WorkshopConnections.cs:ChangeConnectionAsync`, `ui/WorkshopGuidance.cs:CancelTool`, `ui/WorkshopGpu.cs:SubmitConstruction`: retain Workshop as editor owner. Expose existing awaited operations internally and extract pointer→typed-placement separation once; both pointer and native entry call the same placement transaction. Centralize existing editor normalization (including Clock Clamp) so typed parameter calls cannot bypass it. Preserve identity allocation, selection, Undo rollback and palette/status updates. Do not reuse reflection-based PalettePlacementTests.BenchScene/ClickBench/InvokeHandler.
- `PuzzleCatalogue.Create`/`PuzzleDefinitionReader`: production authored fixed layout/inventory/goal. Existing `LaunchLessonSolveTests.AuthoredSolution` parses solution JSON separately; it is not a production solution reader. A introduces no parser and leaves that existing duplication explicitly for B.
- `EventRingPublication.Memory`→`EventRingConsumer.TryRead`→`EventRingDelivery`→`WorkshopDeclaredPresentation.AcceptEvents/Accept/Advance`: real event and declared presentation chain. Drain publication after real commands/ticks as the worker does; producer batch decoding alone is not the integration read surface. Use the current schedule-to-world projection seam, retaining independent autonomous time.
- Migration roots: `WorkshopPhysicsEngineTests.Run/RunWithLedgers/RunPhases`, `NoFixedCapacityTests.RunAndReset`, private runners in `SourceStoreInstanceTests` and `ClockGuardTests`, and `EventRingProducerTests.Ring`. Update their actual callers directly (including affected element/lesson/presentation/consumer tests) and remove the old orchestration methods/classes. Keep valid PlacedParts/LawScene/SwitchMachine builders, low-level solver/ABI/codec/transport fixtures and genuine external-boundary unit doubles.
- Preserve `DemandSizingLifecycleTests.PhysicalMemoryExposesOnlyLogicalRowsAndReusesReservedBuffersAcrossTicks` as defined transport ownership proof. Existing contact-accounting diagnostics may remain explicitly diagnostic observations; do not make private solver tables general harness acceptance.
- `CuriousContraptions.tests/CuriousContraptions.tests.csproj` explicitly lists tests; register every new source. `GameFloatGuardTests` and current project source globs must cover new production executor/host code.
- Coordinator owns TODO, sprint/current execution order and the prior f32 publication receipt. Baseline is published6e0e0ee1799d31211e1769b8883f6bedb3fae396; do not amend the previous slice.

## Tasks & Acceptance

**Execution:**
1. `engine/gpu/WorkshopCommandExecutor.cs`, `WorkshopCommandRouter.cs`, `CuriousContraptions.Simulation/Program*.cs` — extract the sole command implementation and typed host responsibilities; update current worker caller and remove replaced Program logic.
2. `ui/Workshop*.cs`, `engine/MachineWorld.Gpu.cs`, `engine/PartRegistry.cs` — expose the real resource/client/startup and awaited editor operations necessary for native use, with one transaction path and existing defaults.
3. `CuriousContraptions.tests/WorkshopIntegrationHarness.cs`, `WorkshopIntegrationHarnessTests.cs` — compose actual content, command/wire/device/event/presentation interfaces with explicit integer tick time; cover every matrix row.
4. `WorkshopPhysicsEngineTests.cs`, `NoFixedCapacityTests.cs`, `SourceStoreInstanceTests.cs`, `ClockGuardTests.cs`, `EventRingProducerTests.cs` and their integration callers under `CuriousContraptions.tests/` — migrate orchestration, retain behavioral assertions and remove old runner paths; record a concise old→new coverage map.
5. `CuriousContraptions.tests/CuriousContraptions.tests.csproj`, `docs/testing-guidance.md` — register sources and document the actual shared mechanism and A/B boundary. Do not claim B's complete matrix or browser qualification.

**Acceptance Criteria:**
1. Given native and shipped worker hosts, when a supported encoded command executes, then both use the same production command mapping/admission/ACK implementation; no copied switch or compatibility path remains.
2. Given actual Workshop startup and authoring operations, when native tests supply real resource inputs and the in-process production client, then visible success/refusal, placed configuration, Undo/Cancel and atomicity are proven without private reflection.
3. Given a small real interaction and control at each supported cadence, when the full composed path advances bounded ticks and lifecycle commands, then mechanics, events, declared cues, goals and semantic restoration satisfy the matrix. Timing bounds reflect intended gameplay, not historical arrival ticks.
4. Given every migrated test and its original asserted behavior, when registered native tests execute, then coverage remains, duplicate orchestration is deleted and retained low-level fixtures are identified; B's new complete-catalogue matrix is not claimed.
5. Given the frozen candidate, when affected tests, the final local solution round, five Auto layers and normal hooks run, then all required tests pass with zero skips and no unresolved regressions before exact-SHA production publication.

## Spec Change Log

## Review Triage Log

### 2026-10-29 — Review pass
- Reviewers: `/root/harness_a_gap`, `/root/harness_a_blind`, `/root/harness_a_edge`, `/root/harness_a_intent`, `/root/harness_a_forward`; reviewed candidate `native-harness-a-review-candidate-v4.diff`. Forward-only reported no violations; its native host/client and observation options are genuine test boundaries, not compatibility paths.
- verdicts: 9 findings — high0, medium6, low0, false3, maybe-false0.
- findings, in reporting order:
  - `[medium]` `[patch]` Cadence command coverage missing — accepted the verification-gap evidence: existing codec/direct-simulation checks bypass executor revision allocation. Added two successive encoded ConfigureCadence operations, fresh authority checks and real advancement.
  - `[medium]` `[patch]` Retained replay precision in HollowElement/F32BehaviourIdentity — exact passage/goal ticks, timed event records and physical endpoints were still asserted in migrated callers. Replaced them with required passage/goal, semantic event order and existing geometric/energy outcomes; exact authored Reset and decoded saves remain.
  - `[medium]` `[patch]` Retransmission rewinds harness sequence — actual Dispatch assigned an older request sequence and accepted its cached observation. The production client cursor now controls response applicability, sequence allocation stays monotonic, and a retransmit-then-Resume control passes.
  - `[medium]` `[patch]` Undrained advance leaves presentation stale — Publish delivered events without accepting the final read. The harness now admits that actual read through the cursor; the real bumper pulse test exercises undrained advancement.
  - `[medium]` `[patch]` Authored Run continuity missing — the original editor test stopped in Building. It now runs the actually authored unwired/wired gate through the native client at60/120/240Hz, preserving the one-cause control.
  - `[false]` `[reject]` Save/Load scene-handler surface — the intent explicitly requires semantic saved replay into a fresh harness; the implemented command admission plus canonical codec and fresh simulation exercise that surface. File-dialog/scene Save/Load handlers are not claimed by this native scenario.
  - `[false]` `[reject]` Worker host-state production — genuine external-boundary controls are expressly part of the intent. Held installation and ReadsIdle exercise the shared executor's admission; they do not claim worker read-ledger/receipt qualification.
  - `[false]` `[reject]` Full display presentation — the intent names declared presentation and defers browser qualification. The committed-pose/native evaluator boundary is explicitly documented; no renderer/UI-animation equivalence is claimed.
  - `[medium]` `[patch]` Intent auditor's exact replay observation — same demonstrated root cause and repair as the Blind Hunter precision finding above; retained as its own verdict row.
- Five patch groups (all medium); no deferrals. Follow-up review is required for response/read applicability, authored Run continuity and the corrected replay assertions. No production solver change or new compatibility path.

## Design Notes

This is one production path with different genuine host boundaries. The worker still owns browser scheduling/receipts; the native host supplies deterministic installation/clock/transport behavior around the same executor and solver. A held installation task is a deterministic external boundary, not a mock of command results. Avoid a new general command framework.

Use real authored fixed layouts or current construction builders plus production authoring operations. Keep B responsible for complete current-catalogue enumeration and resolving its authored-solution parser duplication. Migration preserves current integration cases but is not permission to add an exhaustive catalogue sweep to A.

## Verification

Implementer: restore the fresh worktree's required dependencies, incremental Debug build of the affected native and Simulation/worker closure with -noAutoResponse, then scoped native checks with --no-build --no-restore -c Debug -f net10.0 and TreatNoTestsAsError=true. Run relevant emitted-output node harnesses only if their boundary changes. Preserve failed attempts and report exact commands/results plus migrated coverage; no full solution test round by the implementer.

Coordinator: matching compiler captures and one final full local round per [local-test-authority](../../docs/delivery-workflow.md#local-test-authority), actual matrix membership audit, five Auto layers, same-owner fixes and affected independent re-review, normal hooks, exact committed-SHA approval, production artifact/deployment verification. No local Release build/web publish, browser test or mutant.


## Implementation coverage map — 29 October 2026

The production worker and native host now share `WorkshopCommandExecutor` (admission, sole command switch, cancellation ownership, cadence allocation and response metadata). Worker-only scheduling receipts, loop ownership, tracing and collection remain in `Program.CommandHost`. Native commands traverse `WorkshopCommandRouter` and the real wire codec; ticks invoke the production simulation and encode/decode its committed response. The harness reads the actual built bundle with `PartRegistry.Read`, advances integer master time explicitly, consumes the real ring through `EventRingConsumer`/`EventRingDelivery`, and retains/rebinds declared presentation across same-world transitions. Manual Step changes world time without inventing elapsed master time.

| Deleted orchestration | Current owner | Preserved coverage |
| --- | --- | --- |
| `WorkshopPhysicsEngineTests.Run/RunWithLedgers/RunPhases` | `WorkshopIntegrationHarness.Observe/ObserveAccounting/ObservePhases` | Existing element mechanics, electrical controls, phase sequences, explicit accounting diagnostics and their callers |
| `NoFixedCapacityTests.RunAndReset`, duplicate `PressurePlateTests.RunAndReset` | `ObserveReset` | Demand growth, authored restoration, saved replay and all existing callers |
| `WeightTests.RunAndReset` | `ObserveReset` with explicit cadence/include-built observations | Existing three-cadence weight cases and lifecycle observations |
| `SourceStoreInstanceTests.Harness`, `ClockGuardTests.Harness` | Owned `Building/Running` harness plus encoded commands | Source enable controls, intentional oscillator timing, Pause/Resume, Reset and saved construction |
| `EventRingProducerTests.Ring` | Owned shared harness | Producer/consumer events, typed owners, ordering, late publication and transport-level failure controls |
| Prismatic replay helper accepting `WorkshopSimulation` | Shared harness parameter and encoded lifecycle/control operations | Closed-gate hold, enabled downstream passage, authored Reset, saved replay |

The prismatic replay's historical endpoint ±1e-5 m assertion was removed: both original and replay still assert downstream exit (>0.49 m), and the authored Reset pose is still checked. No solver change or substitute tighter tolerance was introduced. Invalid hollow authoring now asserts rejection at the real encoder boundary before any request is consumed; overlap admission still asserts its production typed command result.
Coordinator follow-up removed retained output-byte pins in five files in the migrated closure: `MemoryGuardTests` now compares decoded saved construction/allocator after the run; `EventRingProducerTests` compares typed ring headers and write-sequence count alongside existing semantic event records; `SliderInstanceTests`, `SwitchElectricalTests` and `WorkshopWallTests` retain decoded saved equality and actual simulation replay/Reset assertions instead of redundant re-encoded bytes. No cases were deleted. These bounded assertion edits await the coordinator's matching final full-suite build/run; the implementer did not launch another build or test.

Retained low-level fixtures are intentional: `Step/StepWorld` and ABI/solver law cases, defined-interface simulation/device ownership tests, external-boundary installation/transport controls, consumer cursor/evaluator contract fixtures, and explicit contact-work ledger diagnostics. `StepClock/NoInstallation` remain for those direct component tests and are not used by the shared integration harness. `DemandSizingLifecycleTests.PhysicalMemoryExposesOnlyLogicalRowsAndReusesReservedBuffersAcrossTicks` remains unchanged and passed in the affected run. This does not claim B's complete named-catalogue matrix, broadphase pair-order closure, or browser qualification.

The new 13-case native matrix covers real startup success and missing/invalid-resource refusal; production placement/parameter/wiring/Undo/Cancel and rejected-edit atomicity; supplied gate and authored receiver-goal controls at60/120/240Hz; Run/Pause/Step/Resume/Reset and saved replay; stale epoch/revision/cadence/projection and malformed command/enum refusals; held-installation Busy/targeted Cancel/Dispose ownership; stopped/quiet memory observation; retained decoded reads; real event ownership and visible cue retention/restoration. Startup uses `WorkshopIntegrationClient` over the same harness; no private reflection or copied editor controller. Native display transport does not claim browser or autonomous UI-animation-worker qualification.

### Implementer verification attempts

- Fresh native `--no-restore` build first refused absent `project.assets.json`; `dotnet restore CuriousContraptions.tests/CuriousContraptions.tests.csproj -noAutoResponse -v:q` succeeded.
- Incremental native builds exposed and resolved executor interface/disposal typing, moved UI fault-boundary annotations, typed placement arguments and remaining migrated type/call references. No compiler warning was suppressed or gate weakened; the existing broad-catch boundary moved from `ClickAsync` to its extracted `PlaceAsync`, with the static guard updated.
- Worker restore's parallel MSBuild child nodes exited prematurely. The same restore with `-m:1` succeeded.
- `dotnet build CuriousContraptions.tests/CuriousContraptions.tests.csproj -c Debug -noAutoResponse --no-restore -v:q`: successful matching native build, zero warnings/errors.
- `dotnet build CuriousContraptions.Simulation/CuriousContraptions.Simulation.csproj -noAutoResponse --no-restore -c Debug -m:1 -v:q`: successful worker Debug build, zero warnings/errors. No Release build or web publish.
- Initial scoped clock/source/event producer run:114 passed,0 failed,0 skipped (40s). Its command used `dotnet test CuriousContraptions.tests/CuriousContraptions.tests.csproj -noAutoResponse --no-build --no-restore -c Debug -f net10.0 -p:TreatNoTestsAsError=true --filter 'FullyQualifiedName~ClockGuardTests|FullyQualifiedName~SourceStoreInstanceTests|FullyQualifiedName~EventRingProducerTests' -v:q`.
- Initial new matrix:5 passed/4 failed/0 skipped. Test setup incorrectly attempted Save while Running and cancellation of a noncancellable Run transition. Corrected to supported Save after Reset and a held Construct candidate. Next run:6 passed/3 failed/0 skipped; saved allocator incorrectly reused ID1 after owners1/2, corrected to next ID3. No numerical or solver fix. Then all13 new cases passed (4.4s), before strengthening active-cue and explicit held-completion assertions.
- Broad affected run:1,357 executed,1,346 passed,11 failed,0 skipped (8m35s); `build/native-harness-a/native-harness-a-affected.trx` retains the failures. Two prismatic cases and eight placement cases still bypassed the command owner; migrated those remaining calls. The finite parameter editor case expected rejection instead of the documented editor Clamp policy; updated its finite clamp and malformed-refusal assertions. No solver change.
- Targeted fixes:21 executed,20 passed,1 failed,0 skipped; `build/native-harness-a/native-harness-a-fixes.trx` retains that result. All11 original failures passed. The strengthened held-installation test incorrectly expected Cancelled/Superseded for interrupted installation receipts. Shipped `AbortInstallation` faults receipts with `WorkshopInstallationException`, and `InstallationRejected` returns Rejected/Transport. The test now asserts that typed result, successful targeted Cancel, preserved construction, successful subsequent installation and Disposed ownership.
- Final matching native build:0 warnings/errors (6.03s). Final held-installation check:1 passed,0 failed,0 skipped (151ms), recorded in `build/native-harness-a/native-harness-a-held.trx`. `git diff --check` passes. Reused the unaffected broad and targeted passes; no unresolved observed failure remains. Full local solution, compiler-log static gates, independent Auto layers and publication remain coordinator-owned.

Exact affected and repair commands (run from this worktree):

```sh
dotnet test CuriousContraptions.tests/CuriousContraptions.tests.csproj -noAutoResponse --no-build --no-restore -c Debug -f net10.0 -p:TreatNoTestsAsError=true --filter 'FullyQualifiedName~AssignedCompoundFeatureTests|FullyQualifiedName~BothGatePresentationTests|FullyQualifiedName~BothGateTests|FullyQualifiedName~ClockGuardTests|FullyQualifiedName~CompoundCaptureOwnershipTests|FullyQualifiedName~CompoundIdentityRefusalTests|FullyQualifiedName~CompoundMassAndRollingTests|FullyQualifiedName~CompoundPassageBoundaryTests|FullyQualifiedName~CompoundPlateMembershipTests|FullyQualifiedName~ContactActivationTests|FullyQualifiedName~ContactWorkInstanceTests|FullyQualifiedName~CurrentPuzzleShapeTests|FullyQualifiedName~DeclaredIndicatorPassageTests|FullyQualifiedName~DeclaredPresentationTests|FullyQualifiedName~DeclaredPrismaticDriveTests|FullyQualifiedName~DemandSizingLifecycleTests|FullyQualifiedName~DominoGuardTests|FullyQualifiedName~ElectricalNandPresentationTests|FullyQualifiedName~ElectricalNandTests|FullyQualifiedName~ElectricalNorPresentationTests|FullyQualifiedName~ElectricalNorTests|FullyQualifiedName~ElectricalOrPresentationTests|FullyQualifiedName~ElectricalOrTests|FullyQualifiedName~ElectricalXorPresentationTests|FullyQualifiedName~ElectricalXorTests|FullyQualifiedName~EventRingConsumerTests|FullyQualifiedName~EventRingProducerTests|FullyQualifiedName~F32BehaviourIdentityTests|FullyQualifiedName~HoldTimerPresentationTests|FullyQualifiedName~HoldTimerTests|FullyQualifiedName~HollowElementTests|FullyQualifiedName~JoinedPipeTests|FullyQualifiedName~LampDetectorGuardTests|FullyQualifiedName~LaunchLessonSolveTests|FullyQualifiedName~MemoryGuardTests|FullyQualifiedName~MultiBodyPresentationTests|FullyQualifiedName~NoFixedCapacityTests|FullyQualifiedName~PackageRuntimeParityTests|FullyQualifiedName~PalettePlacementTests|FullyQualifiedName~PayloadInstanceTests|FullyQualifiedName~PlacementPoseTests|FullyQualifiedName~PlateGuardTests|FullyQualifiedName~PresentationBindingGoldenTests|FullyQualifiedName~PressurePlateTests|FullyQualifiedName~SliderInstanceTests|FullyQualifiedName~SourceStoreInstanceTests|FullyQualifiedName~SwitchElectricalTests|FullyQualifiedName~TubeMouthSnapTests|FullyQualifiedName~TypedTransportCommandTests|FullyQualifiedName~VisibleSolidColliderTests|FullyQualifiedName~WeightTests|FullyQualifiedName~WorkbenchCapacityTests|FullyQualifiedName~WorkshopDelayTests|FullyQualifiedName~WorkshopIntegrationHarnessTests|FullyQualifiedName~WorkshopIntegrationStartupTests|FullyQualifiedName~WorkshopPhysicsEngineTests|FullyQualifiedName~WorkshopWallResourceTests|FullyQualifiedName~WorkshopWallTests|FullyQualifiedName~WorkshopWireTests' --logger 'trx;LogFileName=native-harness-a-affected.trx' --results-directory build/native-harness-a --logger 'console;verbosity=normal'
dotnet test CuriousContraptions.tests/CuriousContraptions.tests.csproj -noAutoResponse --no-build --no-restore -c Debug -f net10.0 -p:TreatNoTestsAsError=true --filter 'FullyQualifiedName~ABallRestingAgainstTheClosedTiltedBladePassesDownstreamWhenEnabledDuringRun|FullyQualifiedName~HollowWallAdmissionAgreesBetweenPlacementAndConstruct|FullyQualifiedName~TheWeightMassEditorClampsFiniteEntriesAndRefusesMalformedParameters|FullyQualifiedName~HeldInstallationOwnsBusyCancelAndDisposeWithoutWallClockWaiting|FullyQualifiedName~StaleAuthorityRefusesWithoutChangingCommittedStateAndMemoryRequiresQuietStoppedWorld|FullyQualifiedName~RealStartupAndEditorPlaceConfigureWireUndoAndCancelThroughProductionCommands|FullyQualifiedName~AConnectedHollowPassageRestoresConstructionAndRepeatsAfterResetAndSaveLoad|FullyQualifiedName~ABallDroppedIntoTheFunnelsInletLeavesThroughItsThroatInsideTheWall' --logger 'trx;LogFileName=native-harness-a-fixes.trx' --results-directory build/native-harness-a --logger 'console;verbosity=normal'
dotnet test CuriousContraptions.tests/CuriousContraptions.tests.csproj -noAutoResponse --no-build --no-restore -c Debug -f net10.0 -p:TreatNoTestsAsError=true --filter 'FullyQualifiedName~HeldInstallationOwnsBusyCancelAndDisposeWithoutWallClockWaiting' --logger 'trx;LogFileName=native-harness-a-held.trx' --results-directory build/native-harness-a -v:q
```

### Coordinator full-run findings and scoped guard repairs

The coordinator's full round2 recorded main tests4,403 passed/3 failed/0 skipped (20m51s) and tool tests491 passed/0 failed/0 skipped. Raw main result: `CuriousContraptions.tests/TestResults/native-harness-a-full-round2.trx`. All13 new matrix cases and the existing buffer-reuse contract passed. The three findings map as follows:

- `WorkshopHandlerBoundaryTests`: the source guard recognized only direct awaits. It now also recognizes the exact `_Ready` startup-task assignment and await inside its catching boundary. Positive and negative controls require the actual startup method/task, reject assignment outside the catch, and reject extending the exception to another handler. Production startup is unchanged.
- `ElementIsolationTests`: removed stale `Program.Apply` semantic contexts and recorded the nine observed generic clock/duration contexts at the extracted host/interface/executor owners. The known-violation list and shrinking ratchet are unchanged.
- `PortableAnimationTests.WarmedFollowImpulseAndOscillationControlsAllocateNoScratch`: full run observed4,936 allocated bytes; the coordinator's isolated unchanged-test check passed1/0/0 (27ms), retained in `CuriousContraptions.tests/TestResults/native-harness-a-allocation-check.trx`. No production allocation defect was established; no animation source, threshold or warmup changed. Runtime/test-isolation cause remains unproven.

Matching incremental native Debug build passed with0 warnings/errors (9.26s). The two affected guard classes passed29/0/0 (720ms); raw result `build/native-harness-a/native-harness-a-guards.trx`. Exact scoped command:

```sh
dotnet test CuriousContraptions.tests/CuriousContraptions.tests.csproj -noAutoResponse --no-build --no-restore -c Debug -f net10.0 -p:TreatNoTestsAsError=true --filter 'FullyQualifiedName~WorkshopHandlerBoundaryTests|FullyQualifiedName~ElementIsolationTests' --logger 'trx;LogFileName=native-harness-a-guards.trx' --results-directory build/native-harness-a -v:q
```

No additional full suite or animation run was launched by the implementer. Final verification and independent review remain coordinator-owned.

### Pre-review local verification and matrix audit

Round3 completed successfully: **4,897 passed, 0 failed, 0 skipped** (main4,406; tools491). Main duration20m55s. Raw results are each project's `TestResults/native-harness-a-full-round3.trx`. The unchanged animation allocation test passed in this full run; the earlier isolated failure's cause remains unproven, and no animation threshold or warmup was changed.

Matching playtest/solution compiler captures passed with zero warnings/errors; the unchanged web/file-tool captures were reused. Compiler logs: the verification session's `$local_gate_logs` directory (`tim-harness-a-gate.VDq1l3`). The full-run compiler-input/static gates passed on SDK10.0.403 and Node26.12.1. Round1 was cancelled, not a Pass. Round2 failures remain recorded above.

The final command was:
```sh
export CSHARP_STATIC_GATE_BINLOGS="$local_gate_logs"
local_deferred_filter=$(dotnet run --project tools/E2eRunner --no-build -c Debug -- deferred-filter)
dotnet test CuriousContraptions.slnx -c Debug -f net10.0 --no-build --no-restore --nologo -noAutoResponse --filter "$local_deferred_filter" --logger 'trx;LogFileName=native-harness-a-full-round3.trx' -- RunConfiguration.TreatNoTestsAsError=true
```

Every matrix row has executed passing coverage in round3:
- Startup and authoring: `RealStartupRefusesMissingOrInvalidResourceBeforeClientCreation` (both resource controls), `RealStartupAndEditorPlaceConfigureWireUndoAndCancelThroughProductionCommands`.
- Production interaction and lifecycle: `AuthoredReceiverRouteSolvesAndMissingReceiverControlCannot` and `WiredGateOpensWhileControlStaysClosedAndLifecycleRestoresAuthoredState`, each at60/120/240Hz.
- Wire/authority refusal and memory query: `UndefinedWireKindAndRevisionEnumNeverReachTheAdmittedWorld`, `StaleAuthorityRefusesWithoutChangingCommittedStateAndMemoryRequiresQuietStoppedWorld`.
- Pending operation: `HeldInstallationOwnsBusyCancelAndDisposeWithoutWallClockWaiting`.
- Read/event ownership: retained-read assertions in the gate lifecycle test, `RealImpactDeliverySurvivesPauseAndResetRetiresItsWorld`, and unchanged `PhysicalMemoryExposesOnlyLogicalRowsAndReusesReservedBuffersAcrossTicks`.
- Migration: the mapped caller classes above all executed in the full main suite; all13 new matrix cases passed. No missing, unregistered or skipped matrix case.

Guidance checks passed: five custom TOML overrides parse; active planning authorities inherit the game-fidelity rules;1,036 local file links resolve. `git diff --check` passed. Independent Auto review, normal hooks and publication remain pending.

### Review repair verification

The original implementer repaired four harness/test files. Root inspected the complete repair diff. Matching incremental native build passed0 warnings/errors (8.87s); one earlier compile attempt required converting a ReadOnlySpan to an array for Assert.Single. The six affected classes passed **183/0/0** in3.29min, including all17 integration/startup cases. Raw result: `build/native-harness-a/native-harness-a-review-fixes.trx`.

```sh
dotnet test CuriousContraptions.tests/CuriousContraptions.tests.csproj -noAutoResponse --no-build --no-restore -c Debug -f net10.0 -p:TreatNoTestsAsError=true --filter 'FullyQualifiedName~WorkshopIntegrationHarnessTests|FullyQualifiedName~WorkshopIntegrationStartupTests|FullyQualifiedName~HollowElementTests|FullyQualifiedName~F32BehaviourIdentityTests|FullyQualifiedName~EventRingProducerTests|FullyQualifiedName~EventRingConsumerTests' --logger 'trx;LogFileName=native-harness-a-review-fixes.trx' --results-directory build/native-harness-a --logger 'console;verbosity=normal'
```

Final full round4 is running after refreshed matching playtest/solution captures; unchanged web/file-tool captures are reused. It uses the same full-run command above with `native-harness-a-full-round4.trx`. The earlier4,897/0/0 result belongs to the pre-repair candidate; terminal approval awaits round4 and affected independent re-review.

### 2026-10-29 — Affected independent re-review

All five original reviewers independently re-read v5's affected fixes and direct impacts, reusing their unchanged v4 context:
- Verification Gap closed cadence command-path coverage.
- Blind Hunter closed unjustified replay precision.
- Edge Case Hunter closed sequence/observation rewind and stale undrained presentation.
- Intent Alignment closed authored-to-Run continuity and behavioral replay divergence.
- Forward-Only found no compatibility, fallback or second simulation/admission rule.

No new findings or patches: high0, medium0, low0, false0, maybe-false0. The five original patch groups are closed. Follow-up recommendation is now false; no unverified review risk remains. These are scoped source-review closures, not terminal full-suite/publication approval. Reviewers ran no additional tests or mutants. Final round4 remains pending; normal hooks, exact-commit approval and production publication follow only after its passing result.

## Auto Run Result

Implemented one native production integration path through the real bundle, shared command executor, wire codecs, solver, decoded observations, event consumer and declared presentation. Actual startup/authoring and lifecycle controls execute this path; duplicate integration runners are deleted. Browser qualification remains pending at the post-catalogue gate.

Files changed:
- `engine/gpu/WorkshopCommandExecutor.cs` and worker `Program*.cs`: sole shared command admission/dispatch/ACK implementation with genuine host boundaries.
- `WorkshopIntegrationHarness.cs`, `WorkshopIntegrationClient.cs`, `WorkshopIntegrationHarnessTests.cs` and test registration: deterministic native transport, real startup/editor operations,17 matrix cases and production cursor ownership.
- Migrated test callers listed in the coverage map: shared execution, behavioral replay and deletion of old runner methods/classes.
- `ui/Workshop*.cs`: actual startup completion/resource input and shared awaited authoring operations.
- `.editorconfig`, static/UI handler guards and the element-isolation fixture: maintain exact ownership of moved code.
- Agent/testing/physics/delivery/planning guidance and five custom workflow overrides: game-grade comparisons, adjustable stall guards and bounded verification effort.
- TODO, sprint/epic tracking and prior f32 receipt: current delivery state and original publication evidence.

Review:9 reported observations became five medium patch groups and three rejected surface-expansion claims; the duplicate precision observation retains its own row. All fixes returned to the original implementer. All five original independent reviewers closed the affected fixes with no new findings. No deferred findings. Follow-up review recommendation:false after that re-review; no remaining review risk.

Final local round4: **4,901 passed,0 failed,0 skipped** (main4,410; tools491), main20m52s. All17 matrix cases executed and passed. Raw results: each project's `TestResults/native-harness-a-full-round4.trx`. Matching playtest/solution captures passed0 warnings/errors (23.47s/16.30s); unchanged web/file-tool captures were reused and verified by the static gates. Affected repair checks183/0/0 and earlier failed/cancelled attempts remain above. No additional full run is needed for these unchanged inputs.

Residual boundaries: harness B owns the complete current-catalogue matrix and production authored-solution parsing. Native host controls do not claim worker scheduling/read-ledger, browser UI-animation or release/device qualification. Normal commit hooks, exact-commit SnapshotApproval and production artifact/deployment checks remain publication-dependent work; this Auto implementation result is not terminal publication Pass.
