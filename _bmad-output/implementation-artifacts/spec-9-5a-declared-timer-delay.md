---
title: '9.5a Declared Timer and Switched Contact; Generic Delay'
type: feature
created: '2026-10-22'
status: in-review
route: dispatch
baseline_commit: bd37b95294d18da2416d58a08fa0312403193232
context:
  - AGENTS.md
  - docs/architecture/element-package-isolation.md
  - docs/planning/elements/CAT-022-delay.md
---
## Outcome and authority
A Delay configured through the generic parameter editor holds an activation until its declared deadline, then lights the lamp once; repeated inputs while Counting or Finished are ignored until Reset. Packages declare direct or switched supply routing. Preserve all Story9.5a,9A.38,9R.17 and CAT-022 behavior. Owner transport_implementer, retained independent reviewer transport_reviewer. 9A.45a terminal publication Pass is verified; bounded design precheck passed with the retained reviewer.

## Finite design
Use existing typed generic instances, parameter editor, bundle declarations and shared activation engine. Delay duration is canonical f32 throughout authoring/parameters/wire/save and scheduling; occurrence phase becomes f32. Validate finite admitted ranges before scheduling. Duration ticks are checked integer conversion of MathF.Ceiling of the rounded f32 product seconds × cadence (60/120/240); integer started/due ticks remain integer. Thus0.1f→12 and0.2f→24 at120Hz, chained deadline36 independent of entity order. Adjacent representable inputs may share a rounded product; expected ticks follow that product, not strict monotonic increments. Update CAT-022 and simulation-controls current exact-ceiling wording to make this deliberate numeric restoration explicit.

Battery declares supply-source capability; State Latch, Pulse Counter and Pressure Plate declare PowerIn→Supply switched contact governed by committed state. Compile routes from these declarations, preserving canonical Direct/Switched rows. Identical sockets without a declared capability never supply. Existing Impact Switch fixture remains a record until C-Switch; its future electrical mode only adds declaration data. Plate/counter/latch/clock body emission conversions stay with their named families.

ActivationCause provenance uses its existing typed speed validation at the actual shared boundary. Trace necessary phase consumers, wire/read serializers and tests. Do not introduce a bridge to satisfy a textual scan or claim the shared speed/collider ABI is migrated; any retained common numeric lane remains9A.44. Timer-lane Phase/duration must actually be f32.

## Tasks
- [x] Capture a production C# native behavior guard on the current record path before conversion, using existing simulation/test hosts. Preserve original results; do not add a harness.
- [x] Declare timer parameters and direct/switched supply capabilities in the affected packages/schema; compile only declared capabilities with typed runtime identities and atomic rejection.
- [x] Convert Delay generic construction/editor/save/wire/CaptureInstance and necessary DelayedSignal callers; retain configured duration and exact connections.
- [x] Migrate timer duration and ActivationTime.Phase to f32 and all necessary callers; implement approved scheduling-boundary rounding and bit-exact duration serialization.
- [x] Delete WorkshopDelay/DelayDuration, WorkshopInput.Delay, Delay-specific panel/factory/whitelist/compile/Capture arms, obsolete callers/fixtures and record-type supply-route selection. Remove this story's ratchet entries; do not add violations or fallback paths.
- [x] Align current package-isolation/f32 architecture: one declaration-driven shared engine, typed IDs and closed sets, no element solver branch or compatibility bridge. Keep unchanged family emission loops and common ABI ownership explicit.
- [x] Prove real production C# UNIT behavior as PRIMARY evidence: deadline/not-before, busy/finished retrigger controls, chained36/order independence, invalid duration, bypass/missing-link, live input removal atomic refusal, controls/min/max/adjacent floats, Run/Reset/save/load and generic editor guards. Integration supplements unit proof.
- [x] Verify canonical supply-route parity and no-capability negative through the actual compiler/activation consumers; preserve Battery off and committed switched-state behavior.
- [ ] Build affected closure once per relevant input/config change, reuse outputs with --no-build --no-restore; run affected deletion/schema/ratchet checks and retained independent review. Checks passed; frozen independent review is next.
- [ ] Carry9A.45a terminal receipt and dated9R.17/6.5e addendum with normal status/publication; exact SnapshotApproval before protected commit/push, then reviewer remote/CI/artifact/deployment verification. Receipt is carried; publication remains pending.

## Verification and deferred scope
Apply the complete controller contract from requirements#current-cat-022/#todo-130 and simulation-controls: invalid identity, foreign snapshot, clock discontinuity and deadline overflow reject; larger advances preserve actual due ticks and deterministic identity order; borrowed outputs require copying; Delay completion precedes physics; Reset/Load constructs fresh state. Reuse existing applicable native checks or fill concrete gaps. Delay carries command activation only and supplies no electricity. These are ordinary named behavior boundaries, not a new fault campaign.

Native primary proof uses existing WorkshopDelayTests/ActivationTimerTests/DelayedSignalTests, actual simulation and affected supply/compiler/editor/save tests. Choose exact commands after dependency tracing; no automatic repeated full campaign. Browser criteria remain preserved but deferred until ALL named catalogue elements are implemented: cat-022,anim-1b,anim-1c,cat-062,cat-005,cat-037,cat-051,cat-052,cat-052-lamp,cat-020 and affected UI construction/presentation controls. Native proof does not claim browser qualification. Remaining delayed_solar/other companion-part integrations retain their named future owners.

## Current state
Production cutover and affected native verification are complete; independent frozen review is next. Canonical external parameter is delay_seconds per CAT-022/requirements; the stale package duration_seconds spelling is deleted and explicitly rejected, with no alias.

## Reused pre-conversion native baseline
The matching-input9A.45a solution run passed2452/2452 (main2035/2035, terminaltool1f22f8); runtime and these tests are unchanged at basebd37b952. Read-only mapping (tools32d414,f973bf) identifies existing production-network checks: ActivationTimerTests.FractionalStartCeilingAndExactDeadlineEmitDistinctTimerOccurrence; BusyAndFinishedInputsDoNotRestartOrReemit; EarlierPhysicalBypassWinsOverDueAtSamePublicationBoundary; SignedPhaseCeilingExactAndOverflowBoundaries; CompleteWorkerResponseRoundTripsCountingAndFinishedTimerSections; MalformedTimerReadRejectsWithoutChangingCommittedState. WorkshopDelayTests covers canonical duration, save/geometry, malformed duration, direction and no-supply boundaries. WorkshopSimulationTests.TimerBearingStepInstallationRejectsAtomicallyAndResetCancelsCountdown supplies production coordinator transaction/Reset proof with a controlled device; it does not prove physical contact generation. These are reused observations, not newly executed tests or a claim that every CAT-022 criterion is already covered. Missing f32 adjacent-value/chained36/generic-editor/declared-supply controls belong to the conversion proof; no redundant baseline run.



## Changed-input verification and findings
C# unit tests are primary; integration is supplemental. Original tool receipts are retained without repackaging. Debug solution build b88715: zero warnings/errors,13.46s; Release simulation worker build d8b54f: zero warnings/errors,12.97s. Strict TypeScript a9efd5 passed using pinned cached TypeScript with tools/e2e/tsconfig.json. Schema C#/TS outputs were regenerated with existing flatc25.2.10 (e094f0), including removal of InstanceRecord.duration and its hand-written consumer. Deletion/whitespace scans548f71 found no retired Delay names in production or Half in ActivationTimers.

Matching-output tests use --no-build --no-restore. Original affected run f6b0ea passed205/206: generic Delay malformed NaN was classified OutOfRange instead of NonFinite. Finite prevalidation now preserves that typed atomic refusal. New behavior run597352 passed115/116 in8.36s: all physical/f32/supply/save controls passed; sole failure was the editor fixture incorrectly expecting empty Undo after initial placement. Corrected to baseline count and exactly one committed edit. Final editor plus complete isolation ratchet7e90d4 passed22/22 in719ms. Final affected wire/decode/fault/coordinator run a9705d passed212/212 in1s. New bundle capability control7dd0d6 passed1/1; complete affected bundle validator/roundtrip92605d passed67/67 in273ms.

Behavior mapping: WorkshopDelayTests executes real contact→two timers→lamp at exactly start+36, reversed entity ordering, missing-link control, non-Half duration save/load and exact Run/Reset; fixed adjacent-float expected ticks do not repeat production arithmetic. ActivationTimerTests exercises non-Half phases through actual timer input/cause and complete response ActivationLatch roundtrip plus invalid phase rejection. DelayedSignalTests covers large integer origin/tiny phase and fractional borrow. StateLatchTests pairs identical actual routing inputs with copied declarations lacking supply capability; existing compiler/on-off and committed switched-state tests remain covered by the original affected run. PalettePlacementTests exercises seconds fields, invalid submit atomicity, Cancel/one Undo, and Running/Paused/locked guards. Existing controller transaction, malformed read, retrigger, bypass, deadline overflow and deterministic output tests remain in the applicable affected proof.

Reviewer accepted exact DeclaredCapability context BundleValidator.EntryCheck.Electrical/Switch; Known violations only shrink. SourceBody names the declared source accurately. One pre-write gate ordering lapse occurred while adding the supply negative test: malformed patch validation was followed by application before the decision was inspected. Exact full current content subsequently validated allow; subsequent writes await inspected decisions. This content validation does not erase the procedural lapse.

### 9R.17 / 6.5e native functional addendum (22 October 2026)
Delay now uses the sole canonical delay_seconds parameter, declared timer capability and generic editor/save path. Native functional criteria above are implemented; browser construction/presentation qualification remains explicitly deferred until catalogue completion. Timer duration/phase are f32; common contact/capture/provenance speed and minimum goal-duration lanes remain9A.44. No global f32 or browser qualification is claimed.
