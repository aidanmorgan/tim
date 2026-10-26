# Behavioral test hygiene — digest child

Scope: [test-semantic-digests](spec-test-semantic-digests.md), baseline `8ac611ffe81a74af7d36328bf69cc6403065b6d9`. This is a bounded replacement inventory, not global compliance. All 12 affected methods and 57 literal cases remain distinct: three replay cadences, 50 refused fixtures, four accepted fixtures. The affected classes' other controls and lifecycle tests remain unchanged.

| Removed verification | Replacement / retained observable assertions | Literal cases |
| --- | --- | ---: |
| Restitution replay world SHA list | `BouncingRunsAndAReusedEngineReplayPreserveBodyMotion`: 600 ticks on two fresh engines and the first reused; copied states keyed by body ID; exact position, quaternion, linear/angular velocity; fall then rebound, changing box orientation, finite/envelope checks | 3 |
| Contact-load save/command pins | `ARecordPathPlateSaveIsRefusedTypedAtBothBoundaries`: typed verifier refusal and messages at both codecs | 4 |
| Memory counter pins | `ARecordPathCounterSaveIsRefusedTypedAtBothBoundaries`: ten targets/poses, retaining counter 4/8 empty-vector distinction and messages | 10 |
| Memory generic lamp pins | `ARecordPathParameterlessGenericSaveIsRefusedForItsFieldBeyondTheTable`: typed beyond-table refusal/messages | 1 |
| Memory latch pins | `ARecordPathLatchSaveLoadsAsTheGenericLatchWithItsPoseAtBothBoundaries`: identity, pose and lock at both boundaries | 2 |
| Orientation domino/plate/clock pins | Existing both-boundary typed refusals; no rows merged | 3 |
| Orientation pipe pins | Existing save OutOfRange/message and command MissingField / “Instance 1 has no length.” distinction | 1 |
| Oscillator pins | Eight save/command typed refusal/message pairs; parameter boundary controls retained | 8 |
| Indicator wired pin | `TheWiredRecordPathSaveIsRefusedAfterTheSlotRenumbering`: typed save refusal/message and counter mapping refusal | 1 |
| Indicator lamp/detector pins | `ARecordPathSaveLoadsAsTheGenericInstancesAtBothBoundaries`: expected construction and each mapped instance, including detector pose/lock | 2 |
| Pre-launch generated save pins | `APreConversionRecordIsRefusedWithATypedReasonAtBothBoundaries`: nine fixed save/command pairs, same typed refusals | 9 |
| Pre-source-store generated save pins | `APreSourceStoreRecordIsRefusedWithATypedReasonAtBothBoundaries`: thirteen fixed save/command pairs, same typed refusals | 13 |

The fixture cutover captures 44 buffers from the existing native test assembly's historical builders. One-off preservation verified all 22 save inputs against their original pins; all 44 files were then re-read and compared against original builder output, including all 22 commands. [Fixture provenance](../../CuriousContraptions.tests/Fixtures/README-pre-conversion.md). No permanent output hash/byte assertions or generation path remains. Removed newly unused `Records`, `PreSourceStoreRecords`, their layout constants, unused defaults, byte/vector slot branches and declared-default helper. Retained pre-Domino inputs, malformed-table builders, and historical material constants consumed by existing controls.

Required-now verification: initial five-layer Auto review is complete. Seven grouped replay repairs are applied: canonical Cell/Local comparisons; actual committed IDs/count/ticks; static bench preservation; cylinder descent; sign-independent physical rotation; a derived bench-contact rebound bound; run/tick/body mismatch diagnostics. Follow-up review is complete with only these documentation corrections. The repaired Debug build with -m:1 passed (0 warnings/errors, 10.07 s); all three repaired replay cadences passed (0 failed/skipped, 1 s). Earlier scoped native classes passed 187 tests, helper consumers 46 and typed fixture paths 26, all zero failed/skipped. Pre-repair full native session 9021 passed 4,228 tests with zero failures/skips (main 3,720; Coverage 77; E2eRunner tooling 208; WorkerAotGuard 17; Bundle 206). E2eRunner tooling ran as part of the solution and is not browser qualification. Final full native session 59612 passed 4,228 tests with zero failures/skips (main 3,720 / 8 m 59 s; Coverage 77 / 657 ms; E2eRunner tooling 208 / 23 s; WorkerAotGuard 17 / 45 ms; Bundle 206 / 347 ms). Both independent Auto review rounds passed after the seven repair groups and documentation corrections; no unresolved findings remain. Next: normal-hook commit, then exact-SHA CI, Pages artifact and deployment; publication remains pending. Native replay proves engine reset/re-admission, not UI Reset qualification; browser/global qualification remain pending.

Remaining groups owned by `test-behavioral-hygiene`: raw physics replay/Reset (prismatic, cylinder refusal, receiver frame, sensor, device and simulation Reset); event-ring/motion semantics; codec saves; element lifecycle saves; repository-owned source guard pins; compiled-geometry golden review. Preserve vendored integrity checks. Node save-storage ownership changes require strict TypeScript and remain outside this child. The sibling friction candidate replaces its own cylinder replay; reconcile only when that candidate is ready to merge. Its deep single-point push-out defect remains owned by the separate penetration prerequisite.

## Synthetic startup removal — 26 October 2026

Scope: [engine-remove-core-warmup](spec-engine-remove-core-warmup.md). The unpublished warmup-preservation intent is superseded; only its typed 120-tick observations are adapted for ordinary engine reuse.

| Removed test/assertion | Removal or replacement coverage |
| --- | --- |
| `TheWarmupSceneCarriesNoDeclaredSliderOrDriveValue` | Sole synthetic subject deleted; no runtime warmup scene remains |
| `TheWarmupAdmitsAndStepsItsSceneWithTheDriveSuppliedThenUnsupplied` | Sole synthetic subject deleted. Ordinary declared drive/contact remains in `ThePoweredGateOpensToItsTargetWithinItsDeclaredSpeedEffortAndPowerOnEverySubstep`, `ASuppliedBoxWithTheGatesBladeAndLimitsButNoDeclaredDriveNeverActuates`, `TheMovingContactLaneRecordsAContactInAnySubstepOfTheTick`, and `ABallRestingAgainstTheClosedTiltedBladePassesDownstreamWhenEnabledDuringRun`. Sensor/work behavior remains in `AllSixRadialDirectionsQualifyAndArePaidIndependentOfTheOwnerRotation`, `RestingSeparatingBelowThresholdAndGrazingGapContactsRecordNoOccurrence`, and `EachBumperHitPublishesExactlyOneImpactAtItsOwnTimeAndNoTrigger` |
| `AWarmedEngineStepsAWorldExactlyLikeAFreshOne` output bytes | `AReusedEnginePreservesEveryBodyTrajectoryAndStaticSupportInTheNextScene`: one real tick of two separated translating/spinning spheres, then 120 fresh/reused ticks with exact typed body and motion observations, static supports and passive-joint state |
| `AfterTheWarmupTheFirstRunIsAppliedAndEveryCommandOfTheEmptyBenchLifecycleIsApplied` | `AColdDeviceStartsTheFirstRealBallAndResetReplaysItsFirstTick` (explicit and implicit Prepare), plus `AnEmptyBenchAppliesItsOrdinaryLifecycleWithoutProducingDynamicBodiesOrEvents` |
| Borrowed warmup ownership/private semaphore assertion | `ABorrowedSimulationLeavesTheDeviceUsableUntilItsOwnerDisposesIt`: ordinary borrower lifecycle, subsequent owner initialization, eventual single disposal |

Required-now native verification and independent Auto review are tracked in the removal spec; browser qualification remains pending. No private pool assertions, output bytes or hashes are introduced.
