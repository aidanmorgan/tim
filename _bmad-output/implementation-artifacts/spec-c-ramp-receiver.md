---
title: "C-Ramp+Receiver — generic authoring and declared capture"
status: review
baseline: e036b7f9eb55afe55d818c18c10578f1b3b5a610
---

# Outcome

CAT-054 Ramp and CAT-004 Receiver now use generic typed f32 parameters. Package-owned capture/guide declarations replace receiver records, compiler branches and scalar wire fields. Roadmap position24 precedes NAND. Dependencies9A27/9A26/9.6 are verified. Exact acceptance: epics C-Ramp+Receiver,9A28/29,9R12/13 and CAT004/CAT054 current rows. Production C# UNIT tests PRIMARY; integration supplemental; browser qualification deferred until catalogue completion.

# Design and cleanup

Ramp uses declared collider bindings and generic X/Z resize (.125–4m); gestures clamp while active, Cancel restores and completed resize creates one Undo. Direct malformed vectors reject. Receiver declares fixed capture/guide regions and six typed parameters; shared compilation creates one sensor/guide per admitted ball with stable ordering and IDs. Capacity admission reads the capability, not a Receiver identity. The redundant warmup Ramp was removed: five warmup ticks never reached it and other fixtures already exercise static boxes; Initialize/Construct/Run5/Reset and borrowed ownership remain.

Generic defaults/save retain exact f32, including non-Half-representable inputs. Existing common collider/sensor/guide admission and authored AssistanceKnot values still use Half and remain9A44 work. The retained upper capture bound has its existing shared rounding point. No old authoring model, alias, compatibility path or extra editor fields were retained.

Immutable36lesson and9A16geometry fixtures are unchanged. Expected historical JSON is normalized into the current generic comparison shape; actual current parameters are not rounded to fit history. Actual compiled capture/guide bounds, speed/dwell/participation/support are separately compared with historical observations. New f32 default and exact-save assertions establish current authoring precision. All50 Planned content rows remain unchanged.

# Tasks

- [x] Capture existing behavior/compiled baseline before conversion:85/85 native2s,6fb3a2; immutable fixtures and independently verified prior CI2123/2123.
- [x] Shared typed capability/bundle admission, generic Ramp/Receiver parameters, authored content, current wire/save, UI and caller conversion.
- [x] Delete WorkshopRamp/RampDimensions/WorkshopInput.Ramp, WorkshopReceiver/ReceiverCaptureSettings/ReceiverForceRegion/WorkshopInput.Receiver, CaptureReceiver, Receiver accessor, CaptureInstance/compiler/whitelist/scalar paths and obsolete fixtures. Known ledger shrinks56 rows, adds none.
- [x] Align latest design: package identity/geometry/parameters, typed shared engines, exact f32 authoring, explicitly bounded remaining9A44 fields.
- [x] Primary real C# simulation proof: ramp length/runout vs side/depth misses; capture speed/fast-pass, participation/dwell/reentry, actual shifted receiver miss, rotated descending guide vs ascending/outside, bounded acceleration/fixed walls; named goal controls, authored solutions/assistance, save/Reset and capture presentation.
- [x] Native generic resize min/max/overshoot/Cancel/oneUndo, exact non-Half save vectors, parameter rejection and authored forgery controls.
- [x] Matching Debug/worker builds, bundle admission, schema/strictTS and isolation checks; no rebuild between unchanged filters.
- [ ] Independent exact SnapshotApproval, normal protected publication, exact remote/CI/artifact/deployment scoped Pass.

# Evidence and applicability

Original tool receipts, not reconstructed logs:

| Scope | Result / original receipt |
|---|---|
| Actual engine, JoinedPipe, Springboard, compiled geometry |84/84,4s,690677|
| Save/wire/parameter parity and receiver lessons |290/291,6s,01f886; sole failure historical JSON representation|
| Corrected historical36 observations, exact non-Half save and resize |5/5,954ms,ea23ac|
| Bundle invalid capture/guide/null/missing/unknown parameter controls |35/35,68ms,fdf67e; buildfb298a|
| Real ramp roll/runout/misses |1/1,146ms,7ab5c9|
| Real receiver guide/rotation/eligibility/bounds/dwell/participation |7/7,111ms,4f10d1|
| Exact isolation shrink, capacity and capture animation/Reset |89/89,13s,585436; matching build93322e clean6.31s|
| Speed/fast-pass and partial residence leave/reentry |4/4,71ms,a63e8d; matching test-only builda0a917 clean4.19s|
| Current wire schema snapshot |24/24,280ms,5c2301|
| Strict TypeScript6.0.3 |pass fa8027|
| Release worker |clean4.34s,193245|

Native filters use `dotnet test CuriousContraptions.tests/CuriousContraptions.tests.csproj --no-build --no-restore --filter <named affected classes/tests> -v:minimal`. Builds use `dotnet build <affected csproj> --no-restore` in Debug or worker Release. Schema command: installed tsx `--test tools/e2e/unit/run-reset-snapshot.test.ts`; strictTS: installed TypeScript6.0.3 `tsc -p tools/e2e/tsconfig.json --typeRoots <installed @types>`. Source commands and original outputs remain in the tool receipts.

Prior failures remain explicit: CA1861 allocation38a62a and unused overloadf31ad4 corrected; test caller compile errors10326d/d6a5a8/549a44 corrected to current fields/types; historical representation failure01f886 corrected on expected side only. Isolation6fa5c4 found stale deleted debt and redundant warmup identity, now89/89. TS1cf462 found five retired-field accesses, now current generic vector access. Initial worker no-restore002059 lacked assets; restored then clean193245. Tool invocation failures (tsc PATH/typeRoots/Node enum loader) were corrected, not product failures.

Native goal proof `NamedGoalUsesCommittedBodySensorAndEpochWithoutAnimationFeedback` explicitly rejects wrong body, foreign sensor, empty capture and wrong epoch. Ordinary assistance/lesson tests retain existing Half inputs where they test the still-current AssistanceKnot boundary. Production capacity change only replaces equivalent declaration recognition; reviewer establishes unchanged prior proof applicability. No Chrome qualification claimed. Moving-frame/nudging and broad numeric migration remain named later scope.

# Current state

Implementation and scoped checks complete; awaiting retained reviewer exact approval. No known unresolved product defect. Prior9.6 terminal receipt57b2a6 is carried in this normal publication. Next: exact frozen review, protected publication, then NAND only after terminal scoped Pass.
