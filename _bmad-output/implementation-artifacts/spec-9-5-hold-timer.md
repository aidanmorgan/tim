---
title: '9.5 Hold Timer Pulse Stretcher (CAT-033)'
status: review
baseline_commit: 8b93a3afd81a50bcff73175809868e13ceae3b44
context:
  - AGENTS.md
  - docs/planning/elements/CAT-033-hold_timer.md
  - docs/architecture/element-package-isolation.md
---
## Outcome and bounded decisions
A momentary physical trigger closes an independently powered contact for hold_seconds (0.1–12, default2), then opens and rearms. Busy triggers never extend the window; source loss never pauses time or creates energy. Owner transport_implementer; retained reviewer transport_reviewer. 9.5a terminal scoped Pass is verified: CI2042/2042 and deployment7233524992 exact8b93. Other actual dependencies8.1/8.2/9A.45a are delivered.

Exact authority: Story9.5, requirements#current-cat-033/#todo-261, CAT-033 H1–H23 and DESIGN Hold timer row. Resolve the stale open questions explicitly: output is PowerIn→Supply only, no ActivationOut; Reset clears timer/pending trigger/delivery state and never invents an expiry event; use9.5a f32 timer and9A.25a f32 presentation, with remaining common ABI9A.44. Preserve legacy harvest as historical facts.

Extend the shared declared timer with typed completion, boundary and elapsed-output policies: Rearm, BeforeNetworks, None for Hold; existing Delay retains its latch/output behavior. No Hold identifier branch, record, per-part loop or ratchet debt. Expiry occurs before the same-tick Delay delivery/electrical solve: trigger at expiry starts a new full window. Integer due/start times remain integer; duration uses checked MathF.Ceiling of rounded f32 seconds×cadence. Supply contact follows committed Counting state.

New package owns its box/foot geometry, cyan/navy/cream art, three typed sockets, icon/catalogue data and presentation bindings. Remaining bar drains leftward anchored at−0.5, hidden outside Counting, minimum scale0.001; slate/gold lamp tracks committed window independently of supply with0.1s easing. Use existing generic drawer for configuration, no separate inspector. Generic declaration/evaluator extension only where missing.

## Tasks and acceptance
- [x] Reuse identifiable9.5a timer/supply native proof; add missing rearming/boundary/no-output behavior controls before relying on the new package.
- [x] Implement package declarations/art/bindings and necessary shared typed policy/evaluator support; no runtime element recognition or new known violations.
- [x] Align latest package isolation/f32 design and delete any superseded shared path/callers/fixtures introduced by the extension. No pre-existing Hold record exists to retain or migrate; no compatibility bridge.
- [x] C# UNIT tests are PRIMARY proof: actual physical trigger and supplied consumer, exact window/not-before/expiry, repeated busy triggers, repeated windows, both entity orders, min/default/max/non-Half parameters, invalid finite/range/unknown policies, and expiry-tick retrigger after Delay completion.
- [x] Prove no supply and supply loss/restoration paired controls, source loss during the window and restoration after expiry without replay; retained9.5a no-capability controls supplement package integration.
- [x] Prove real Run/Reset and save/load parameters/typed links, pending delivery cancellation and fresh state; generic editor guards/atomic invalid submission, connection ambiguity/Cancel/Undo as applicable.
- [x] Execute production read-model/presentation bindings and assert bar extent/anchor/visibility and lamp transitions, failed/uncommitted state preservation and hidden-state catch-up. Removing bindings must make the positive assertions fail.
- [x] Build affected closure once per changed inputs/config, run matching outputs with --no-build --no-restore. Integration is supplemental. Check package membership/deletions/ratchet.
- [ ] Retain independent exact SnapshotApproval before normal protected publication, then verify remote/CI/deployment identity and terminal scoped Pass.

## Deferred qualification
No Chrome/E2E until ALL catalogue elements are implemented. Retain cat-033 UI construction, positive/control/boundary/Reset/save and presentation cases for that phase; no native result claims browser qualification. saved_for_later requires Motor/Wound spring and remains its named later integration, without making their implementation a prerequisite for this supplied-contact slice. Detailed allocation/performance remains its named gate.

## Current state and original evidence
Implementation is ready for retained independent review; publication and browser qualification are not claimed. Shared typed policies, one candidate checkpoint and one chronological router preserve the ordinary Delay path. Scheduled work runs under the device gate. New physical input drives the next physical integration; already-due Delay drives the current boundary, proven by gate blade displacement and a no-supply control.

C# UNIT tests are primary; integration is supplemental. Original tool receipts (no duplicated manifests):
- Debug build45542c: zero warnings/errors,11.07s. Native53/53 df2bbc,3.67s covers Hold windows, min/default/max/non-Half values, Reset/save, supply controls, editor, presentation, forged-read ownership and exact isolation ratchet.
- Final new-test build bcb42f: zero warnings/errors,4.63s; bundle build d5a470: zero warnings/errors,2.47s. Bundle137/1378a5e99,281ms includes unknown/invalid timer policies, silent timer port admission and package validation.
- Shared affected closure cdd020:549/551 passed in8s (timer/Delay, memory/latch/counter/clock/plate, device, supply, presentation, wire/save, ports). Two failures were fixture maintenance: missing new Hold golden row and a new physical-bypass fixture whose contact landed exactly at deadline, not earlier. Port correction passed37158f. Diagnostic37158f preserved ordinal464 equality; lower second-drop height plus an explicit strict-earlier prerequisite closes the intended control. Final bypass bcd86f1/1,170ms; matching build7ffed3 zero warnings/errors4.28s. No product change after the549 shared passes.
- Release simulation worker build6e718d: zero warnings/errors,12.35s. No schema wire fields changed; bundle schema policy roundtrip/validation is covered by the137 native tool tests.
- Prior attempts preserved: first package transform/palette compile errors c769f8/8859e9; stale ContactClosed test callers805199; new-test inclusion discovery b5f397; five fixture revision/allocator failures6baaf6; native scene scalar syntax failure6b6170 corrected without numeric geometry change and presentation79460b passed. No failure is represented as a pass.

Criterion mapping: HoldTimerTests executes actual production device/solver for physical windows, independent supplied gate motion, power loss/restoration, two windows, due-boundary Delay, Reset/save and actual earlier-contact arbitration. ScheduledExpiryRetriggerAndPhysicalBypass covers expiry/retrigger and Reset-dominates-Set; its Delay-before-Hold identities complement the real Hold-before-Delay construction. ActualCoordinatorRejectsForged and QueuedDiscardedAndFailed exercise production read admission and device ownership, including backdating/input-start mismatch, busy restart, foreign identity, discard/retry and queued advancement. HoldTimerPresentationTests applies real bindings to native scene nodes and checks omitted binding, hidden catch-up and stale read controls. GenericTimerEditor runs both Delay/Hold through real native drawer guards and atomic submit/Cancel/Undo.

Deletion/design closure: no prior Hold implementation existed. Removed the unconditional Finished/elapsed completion path and unconditional pre-solve use of prior contact state; the shared typed-policy implementation replaces them. Removed the intermediate request-mask carry design after chronological stage filtering made it unnecessary. Existing callers use declared timer state; no old API bridge, element record, identifier branch or Known-violation addition. Exact Generic classifier rows only describe timer completion policy and capture protocol. Geometry/art/ports/bindings live in the package; common physical Half lanes remain9A.44. The brief earlier Anvil ordering lapse was disclosed to the coordinator and exact current content subsequently validated; subsequent edits awaited explicit decisions.

## Forward correction after publication CI
CI39654813219 on f1fe8596 failed25 tests,2037 passed,2062 total; deployment was skipped (c48863 original log). Local red806714 reproduced both real Faulted cases: scheduled routing incorrectly required orientation reads before physics. The completeness check now applies only to Physical; Scheduled accepts no physical read, while Physical still rejects a missing sensor read. No timer ordering or physical admission was relaxed.

The other failures exposed catalogue fixture omissions: new Hold palette/icon counts, placement/default/material/geometry/capacity coverage, and solver census parsing only named Id arguments. Census now reads generated bundle IDs and matches camel/Pascal words, including holdWindow while excluding unrelated threshold. Original solver pins remain unchanged. Historical9A.16 geometry golden is unchanged and explicitly excludes the later Hold addition; Hold housing/foot/material rows have independent fixed assertions. SavePrevalidation's puzzle-goal local was misleadingly named delay and owned by done9.5a: renamed minimumGoalSeconds with identical Half validation and removed exactly that obsolete Known entry; remaining goal ABI stays9A.44.

Matching build8a7ec2 passed zero warnings/errors12.81s. Targeted closure7a2fe0 passed340/340 in21s: all original failing classes, actual mixed/orientation capacity simulation and Reset/save, missing-physical negative, new Hold geometry, registry/material/parity/capacity, solver/isolation guards and save refusal. Release worker77d91d passed zero warnings/errors11.57s. Earlier unaffected results remain evidence only subject to retained reviewer applicability. Forward exact SnapshotApproval/publication/CI/deployment are pending.
