# 9A.45 implementation handoff — 29 October 2026

Final local verification and independent review passed on 30 October 2026: 5,057 passed, zero failed/skipped; all seven accepted findings resolved. Normal hooks, exact committed-SHA approval and publication remain pending. No publication, browser qualification or local Release/publish claim is made.

## Changes and ownership

- Core owns generic runtime declarations, simulation/device/solver, activation/supply execution, runtime wire, diagnostics and declared presentation. RuntimeSources.props is the explicit compile inventory. Main retains authoring, package lookup, saves, compilers and external observation projection. Both workers reference Core and no longer link the engine wholesale, load packages or embed lessons/bundles.
- Main compiles one installation containing physics rows, activation graph/contact owners/substeps, presentation descriptors, editable source permissions and capture ownership. Runtime installation validates references, enums, finites and ownership atomically. Main-to-runtime commands and installation responses carry that compiled scene, never hidden authored bytes.
- Runtime and authoring FlatBuffers schemas/generated files/codecs have distinct owners. Generated C#/TypeScript uses flatc 25.2.10; PATH's 25.12.19 was not used. Schema consumers, SIMD AOT assembly ownership and compiler inventories follow Core.
- PartId is opaque. Uninitialized Value access explicitly refuses; absent optional authored inventory is represented as null at the external projection boundary. Straight Pipe uses the generic length parameter for art, geometry, mouths, placement and persistence. Named identities/factories, WorkshopPipe, scalar authored length and the known-violation ledger are deleted.
- The generated bundle is the sole registry/port/palette membership authority. UndeliveredParts and its future-name blacklist are deleted. Unknown packages still refuse; a valid newly declared identity requires no engine admission edit.
- Runtime captures carry session plus compiled-scene identity. Main retains original authored snapshots through later edits/cadence changes and releases them on observation retirement. Missing/foreign associations explicitly retire observation while gameplay remains usable.
- Production BundleElements runs the package-coupling scanner during normal builds. Tests exercise actual scanner controls and runtime/project ownership. The added CSharpStaticGateTests ownership guard checks real Csc sources/references and awaits the coordinator's matching five compiler logs.

## Verification and failures retained

All native TRXs below are in CuriousContraptions.tests/TestResults; counts are total/passed/failed/skipped.

| Artifact | Result | Meaning |
| --- | --- | --- |
| isolation-pipe.trx | 398/390/8/0 | Earlier input set; eight exact-message assertions for obsolete Counter saves. |
| isolation-compiled-01.trx | 420/401/19/0 | Native browser fixture omitted compiled installation (15), forged capture omitted required session (3), old whole-command equality (1). |
| isolation-compiled-02.trx | 429/429/0/0 | Compiler/codec/runtime, malformed compiled controls, capture association and native lifecycle. |
| isolation-final-focused-01.trx | 962/814/148/0 | Expanded affected callers exposed stale authoring-command fixtures and offsets, described below. |
| isolation-final-focused-02.trx | 531/531/0/0, 54s | Repaired groups, actual Core/worker closure, main unknown/overlap refusal, capture retirement, live presentation and architecture docs. |
| isolation-final-controls-03.trx | 9/9/0/0 | Removed timer-control refusal and latest architecture links/consistency. |

tools/E2eRunner.Tests/TestResults/isolation-tool-selection-01.trx: 72/72/0/0, 25s. Its build first found unescaped source-fixture strings from named-ID migration; corrected raw string fixtures preserve actual catalogue-change assertions.

Strict npm run build:runtime passed. Node 26.12.1 with --experimental-vm-modules passed 57 tests across workshop-animation-output, workshop-client-lifecycle and workshop-observation, then 11 tests in workshop-animation-clock (zero failures/skips). The first invocation omitted the required VM flag and failed; the corrected invocation exposed two stale 144-byte fixtures. Current fixtures use the 104-byte animation frame and property offset 96. These run compiled shipped output; deferred browser suites/toolchain tests were not run.

Both worker Debug builds passed with zero warnings/errors. Final Simulation output is /tmp/isolation-9a45-simulation-debug-final.log (1.88s; see gate incident below). Final Animation output is obj/isolation-animation-debug-final.log (1.73s). Final native test/main/Core compilation passed with zero warnings/errors (7.83s, execution output chunk 0c2516). obj/isolation-main-debug-final.log retains a failed MSB4166 attempt; it is not successful build proof. Parallel MSBuild child nodes subsequently exited in about one second without source diagnostics, and their reported temporary diagnostic directories were absent; serial -m:1 -nodeReuse:false builds passed. Selection-tool restore assets were initially absent and were provisioned by its normal build.

Successful unchanged behavior from earlier scoped runs was reused; the full solution suite was not repeated by the implementer. Final static/compiler-policy checks remain pending the coordinator's logs. git diff --check passed.

## Coverage migration

The 148 expanded-run failures were 132 poisoned-knot rows, three knot-count rows, one off-precision knot, one historical-save/current-frame verifier control, three unknown-element command cases, two hollow-overlap cases, two dependency guards, one animation layout case, one schema-ownership inventory and two command wire assertions.

Authored-knot tests now exercise WorkshopAuthoringFlatBuffers with a current save frame and actual generated verifier; all lanes/counts/precision controls remain. Corresponding save admission and real compiled simulation/Run/Reset tests remain. Unknown-element tests now call the main compiler/client and assert typed UnsupportedPart before dispatch; malformed compiled-reference/enum/nonfinite tests exercise the production runtime router and unchanged usable world. Hollow cases assert typed InitialOverlap at main compilation and unchanged committed state; a real client control proves subsequent Run/Reset succeeds. Command identity checks assert identity and compiled body values, while malformed Cancel mutates its actual FlatBuffers target slot. Dependency tests distinguish worker source ownership from generic Animation-owned source and check the real runtime assembly; actual Csc membership is additionally enforced by the new log-based guard.

Two exclusive dead TimerObservation tests were deleted: TimerSegmentsSeedTheirCommittedEndpointAndRejectInconsistentIntervals and TimerObservationUsesItsCommittedIntervalAndTerminalLevels. Their segment/control implementation is deleted. Live declared timer presentation, activation timers, playback/cadence and channel padding/unknown-kind controls remain; WorkshopActivationAnimationTests and DeclaredPresentationTests passed in the expanded/current focused rounds.

## Process exceptions

Two orchestration mistakes were reported immediately to the coordinator and are not represented as prospective validation:

1. Three tiny edits were applied after malformed unified-diff previews were rejected. Original operation IDs: op_01M63F9YWJ8TN75QME1BYXYEG4 (Core project references), op_01M63F9YWWQBRBF1BH6WND6YMM (WorkshopObservation ordinal removal), op_01M63F9YX79ZK9XRVBXE8JHGRS (WorkshopRuntimeValues ordinal placement). Subsequent full-content validations allowed all three; those validations were retrospective. Original failed-preview/tool records remain in the implementation conversation.
2. /tmp worker-log previews were rejected as workspace escapes: op_01M63HXJFDY1336G9CAS6PVSXK and op_01M63HXJFJDYP50XWNM8ZX3N2F. A faulty orchestration call nevertheless launched the Simulation build redirected to /tmp/isolation-9a45-simulation-debug-final.log. No source write occurred in this incident; the Animation redirect was not launched. Subsequent gates and writes were separated into distinct calls, with explicit allowed results inspected before writing. The Animation log used an admitted workspace-local path.

A briefly spawned timer helper was stopped after the owner clarified serial implementation; it confirmed no writes or tests. All TimerObservation changes were completed serially by this implementer.

Remaining risks are pending independent review, complete local compiler/test enforcement, normal hooks and production CI/AOT/deployment. Browser/E2E and detailed performance/device qualification retain their named later gates.

## Coordinator failure repair and second freeze

The coordinator's .tmp/isolation-coordinator-20261029/full-local.log is an interrupted failed full run: 16 emitted failures before SIGINT, exit 1, no complete totals. It is retained unchanged. Six valid large-scene cases failed verification, five XOR feedback cases still expected runtime admission of invalid authored graphs, four shape cases targeted removed authored command/response envelopes, and the strict membership guard found the scanner's redundant package literal.

The bounded vendored FlatBuffers repair keeps absolute vtable positions in checked int arithmetic and reads format-defined vtable lengths/field offsets as UInt16 in both verifier and generated-accessor support. Bounds/alignment checks remain; arithmetic failures are no longer converted to absent fields. Table.cs and FlatBufferVerify.cs now have honest exact integrity pins and documented local provenance; the other seven vendor sources and license remain unchanged. No capacity cap, compatibility wrapper or verifier bypass was introduced. Regression controls exercise positions beyond 32 KiB, unsigned field offsets on both sides of the signed boundary, generated getters and malformed offsets through the defined interfaces. The existing real large-scene gameplay scenarios passed after the verifier correction.

The timer validation cache now keys the successfully committed compiled scene identity. Reset then cadence change retains authored revision but receives a new identity and new timer duration. The regression runs until Counting with a generous ten-second stall guard, checks the feature-defined duration and next supply-evaluation ordering, then expiry and Reset. Failed/cancelled installations do not replace the committed scene or its cache identity.

XOR zero-delay feedback refusal now asserts typed NonmonotoneFeedback at the main compiler before dispatch, with unchanged scene/save and valid installation controls retained. CurrentPuzzleShapeTests retains its two real authored-save shape rows; four obsolete command/response envelope rows were removed because runtime commands now contain compiled scenes. Current compiled-boundary malformed/root controls cover that interface. The redundant scanner switch-string exclusion was deleted; the strict membership guard was not weakened. Obsolete runtime-authoring codec comments and the unused command construction slot constant were removed.

Additional removed-schema coverage mapping: DeclaredOscillatorTests.ANegativeZeroLengthIsATypedFieldOnAGenericRecord (two cases) and PayloadInstanceTests.AGenericRecordCarryingAnyTypedFieldIsRefused (twelve theory rows, including its EveryTypedFieldOnGenericRecords data and TypedField enum) described the deleted scalar typed-field slots. Generic parameter admission/refusal, live oscillator behavior and historical unsupported-format save controls remain. These deletions are distinct from the two dead TimerObservation tests above.

| Artifact | Result | Meaning |
| --- | --- | --- |
| isolation-verifier-cadence-before-cache-fix.trx | 5/0/5/0 | Two reproduced cadence faults; three new wire fixtures initially omitted required Exact revision kind. |
| isolation-coordinator-fixes-01.trx | 487/484/3/0, 15s | Original large-scene/refusal/shape failures repaired. Two new cadence assertions observed supply one evaluation too early; one boundary exposed signed generated-accessor offsets. |
| isolation-coordinator-fixes-02.trx | 308/308/0/0, 6s | Final vendor/generated-accessor, compiled/wire malformed controls, save rejection, cadence and architecture-doc checks. |

The three TRXs are in CuriousContraptions.tests/TestResults. tools/BundleElements.Tests/TestResults/isolation-membership-fix-01.trx passed 3/3/0/0 (34 ms), after its matching build (4.49 s); scanner and membership inputs have not changed since. Final incremental native test/main/Core Debug build passed with zero warnings/errors (9.19 s, execution chunk 81855e), using -m:1 -nodeReuse:false. An earlier build session 30731 was unavailable after context recovery; no result is claimed for it. The initial boundary fixture used a nonexistent builder helper and failed compilation before correction to the generated vector constructor.

The 484 passing first-repair checks remain applicable except the shared Table accessor dependency, for which the final 308 checks exercise current codec/save/compiled frames, malformed inputs and both unsigned offset boundaries. Large-scene gameplay and repaired XOR/current-shape/scanner behavior were not needlessly rerun. Earlier worker host compilation remains evidence only for unchanged host sources/interfaces; the coordinator's refreshed compiler capture/full required run establishes the final complete candidate. No full solution repeat, browser tests, compiler-log capture or Release/publish was performed by the implementer in this repair round. Final diff whitespace check passed. Complete compiler/static enforcement, Auto review, hooks and production CI remain pending the coordinator.

## Auto findings and complete round-two failure repair

The coordinator's complete round-two run remains unchanged at .tmp/isolation-coordinator-round2/full-local.log and .tmp/isolation-coordinator-round2/results: 5042 total / 4998 passed / 44 failed / 0 skipped, terminal exit 1 (native: 4550 / 4506 / 44 / 0). The earlier interrupted run is separate evidence. The following fixes address the five accepted Auto findings and all 44 failures; independent re-review and complete verification remain the coordinator's responsibility.

- Observation associations store an explicit compiled identity and scene-free command metadata, so historical authored metadata cannot retain compiled physics/activation/presentation graphs. Pending candidates remain available until their terminal completion; active installations remain available; completed rejected/cancelled/superseded unused candidates and superseded uncaptured edits are released. Successfully Run scenes retain authored metadata because the production worker starts captures on Run and their diagnostics may arrive later. Retirement clears all association and retention state. Removal does not allocate an intermediate key collection. Owner-level rejection/cancellation/supersession tests are explicitly scoped to that observation owner; the actual-client repeated-edit test proves current Run/Reset remains usable after obsolete association removal. Late-capture projection now establishes a successful Run before changing cadence/editing, and identities come from the actual installed scene rather than an incidental command-sequence allocation.
- Command and response decode controls mutate compiled presentation From to NaN, remove the required electrical socket by selecting that driver without a socket, and introduce incompatible driver/curve combinations. Each asserts typed OutOfRange, unchanged installed world and continuing Run/Advance/Reset. The valid control is a real compiled counter binding; its nonzero scalar driver field makes the hostile wire mutation explicit.
- The production scanner blanks preprocessor directive trivia while preserving branch text and original character positions, then scans every conditional branch. DEBUG, PLAYTEST, combined conditions, elif/else and nested alternatives cover literals and named/aliased/qualified PartId members with useful source locations and generic controls. No exception ledger was introduced.
- Compiled admission rejects a missing CommittedElectricalSocket socket atomically. Supported driver/curve combinations now have one shared enum-based contract in Abstractions, used by bundle and compiled admission. Endpoint curve mapping is exhaustive and refuses incompatible bindings; the silent Linear default is deleted. Existing supported presentation and bundle behavior passed.

All 44 original failures have passing affected coverage. Their exact assumption-to-interface mapping is:

| Original failures | Repair |
| --- | --- |
| DeclaredOscillator 8; DeclaredContactLoad 4 | Historical scalar records lack current declared parameters and refuse at authored admission with OutOfRange; historical authored command envelopes refuse current runtime root shape with OutOfRange. Tests retain every fixture and require typed visible refusal, without obsolete incidental error-text pins. |
| DeclaredOrientation 2; PreConversionLaunchSave historical 2 | Current runtime root shape rejects old authored envelopes before their former nested-field decoder. Pipe save refusal follows missing declared parameters, not the deleted scalar-length slot. Historical save controls remain. |
| PreConversionLaunchSave current equality 1; WorkshopBumper equality 2 | Decode creates a new compiled scene object. Tests compare command scalar fields and semantic scene identity, bodies and owners instead of reference equality. |
| ElectricalNor 6; Nand 5; Or 5 | Nonmonotone authored feedback is rejected by the real main compiler before runtime dispatch. Existing typed refusal, unchanged scene and valid construction controls remain. |
| PlacementPose hollow admission 5 | Placement refusal agrees with typed InitialOverlap at main compilation. Unchanged installed state and subsequent Run/Advance/Reset are checked; non-overlap controls still install through the production route. |
| WireFault codec guard 1 | Declared-parameter exception translation belongs to the authoring codec; the architecture guard now inspects that actual owner while retaining its no-alternate-decoder checks. |
| PhysicsLaw family scan 2 | Removed two hardcoded future-family rows absent from the generated catalogue. Every actual catalogue family remains derived and checked; no deleted admission blacklist was restored. |
| DeclaredPrismaticDrive empty bench 1 | Native device and actual compiler fixture now use the same document identity; empty-world Run/Advance/Reset and no synthetic bodies/events remain asserted. |

Affected proof (counts total/passed/failed/skipped):

| Artifact | Result |
| --- | --- |
| CuriousContraptions.tests/TestResults/isolation-review-fixes-01.trx | 475/462/13/0, 16s |
| CuriousContraptions.tests/TestResults/isolation-review-fixes-02.trx | 100/96/4/0, 916ms |
| CuriousContraptions.tests/TestResults/isolation-review-fixes-03.trx | 71/71/0/0, 1s |
| tools/BundleElements.Tests/TestResults/isolation-review-bundle-01.trx | 61/61/0/0, 451ms |

First affected-run failures were eight still-obsolete authored-save expectations, three new observation fixtures accidentally reusing a harness command identity, and two new mutation fixtures selecting the default Event driver whose scalar wire slot is omitted. The second run's four failures were the replacement fixture looking for CommittedActivation in an event-only scene; the corrected fixture compiles an actual counter and selects its CommittedCount binding. No production gate was relaxed to fix these fixtures. Passing unchanged groups from the first and second runs were reused; only affected test files/consumers were selected afterward.

Matching final native test/main/Core Debug build passed with zero warnings/errors (8.19s, execution chunk 1db67b); affected bundle-tool Debug build passed with zero warnings/errors (5.09s, chunk 56e450). Incremental builds used -m:1 -nodeReuse:false and tests used --no-build --no-restore. Intermediate compile failures from a misplaced appended declaration, escaped scanner character literals, a missed projection constructor call, a constant-array analyzer finding and a misplaced fixture property were corrected; their execution outputs remain in the conversation. All writes followed an explicit allowed gate in a separate call; no rejected gate was bypassed in this round.

Third candidate freeze: final diff whitespace check passed. No processes remain and no further edits are planned. The implementer did not rerun the full suite, compiler captures, CI, browser/E2E or mutants. Complete verification, affected compiler capture, Auto re-review and publication remain pending the coordinator; the architecture current-status view should be reconciled with these now-repaired findings at that review boundary.

## Follow-up review: delayed Run acknowledgement and electrical socket admission

Two new findings blocked the third candidate before any new full suite. The coordinator completed three affected compiler captures with zero warnings/errors; unchanged tool logs were reused from round two. Those captures precede the following production fixes.

Run capture ownership is now recorded before transport send, keyed by the exact command sequence and the scene active at dispatch. Pending Run ownership protects that original authored metadata through Reset, cadence changes and later construction while the acknowledgement is held. An Applied acknowledgement promotes its own recorded scene to capture retention; it never reads the newer active scene to choose capture ownership. Completion removes pending Run ownership for every outcome, including rejection, cancellation and supersession. The pending map follows the client's bounded command queue; it does not retain unused candidates indefinitely, and retirement clears it.

The real-client late-capture test now runs with both ordinary and held Run acknowledgement delivery. In the held case the production Run executes, Reset and cadence change plus later Construct complete, then the original acknowledgement and capture arrive. The original scene still projects its authored puzzle metadata without retiring observation. Three explicitly scoped observation-owner controls prove rejected/cancelled/superseded Runs release their pending old-scene ownership while preserving the current installation association.

Compiled CommittedElectricalSocket admission now permits only FirstIn, SecondIn, PowerIn and Supply, matching the defined ElectricalLogicRead.At interface. A real compiled both-gate binding is the valid wire control; mutating its socket to the defined but non-electrical ActivationIn is refused with typed OutOfRange at both command and response decode boundaries. The existing installed world remains unchanged and continues Run/Advance/Reset.

| Artifact | Result (total/passed/failed/skipped) |
| --- | --- |
| CuriousContraptions.tests/TestResults/isolation-followup-before-01.trx | 7/5/2/0, 388ms |
| CuriousContraptions.tests/TestResults/isolation-followup-after-01.trx | 110/110/0/0, 10s |

The baseline failures reproduce original-association retirement after a delayed Run acknowledgement and admission of the invalid electrical socket. The latter unexpectedly dispatched the malformed command; fixture disposal subsequently reported a duplicate sequence, retained in the raw failure rather than hidden. The repaired run covers observation/client lifecycle, compiled boundaries and supported declared presentation only. Matching incremental native test/main/Core Debug build passed with zero warnings/errors (14.10s, execution chunk 52524f); tests used --no-build --no-restore. No broader suite, CI, browser or mutant run was performed.

Fourth candidate freeze: handoff updated, diff whitespace check passed, no running processes or planned edits. Affected independent re-review, refreshed coordinator compiler captures and complete verification remain pending.

## Final local and independent review result — 30 October 2026

Coordinator session 11797 completed exit 0. All five round-four TRXs independently reconcile to 5,057 total/executed/passed, zero failed and zero skipped: native 4,565; WorkerAotGuard 17; Coverage 77; E2eRunner non-browser 192; BundleElements 206. Original console and TRXs remain in .tmp/isolation-coordinator-round4/full-local.log and results/. Five compiler inputs match the final candidate; refreshed playtest/solution/web captures passed without warnings/errors, with unchanged tool captures linked from round two.

All five original Auto layers and affected repairs were independently re-reviewed. Seven accepted findings (one high, six medium) are resolved; two intent concerns were rejected with recorded evidence; no finding is deferred. Final follow-up recommendation is false because affected re-review converged, not because review was omitted. The spec's Auto Run Result records file groups, findings and evidence identities. Baseline is 15cf84969870cddefc7c91b6a539d7144dc47f76; final reviewed runtime diff is .tmp/isolation-coordinator-round4/candidate.diff.

Only final status documents changed after the successful frozen proof. The normal commit hook runs unchanged, with output to .tmp/isolation-coordinator-round4/commit-hooks.log. Its actual result and canonical SHA will be reported after completion. No push is authorized here; exact committed-SHA approval and coordinator authorization follow. CI/deployment/browser qualification remains explicitly pending.
