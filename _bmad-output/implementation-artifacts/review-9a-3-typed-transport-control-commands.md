# Story 9A.3 independent review

Reviewer: Codex transport_reviewer (/root/transport_reviewer). Implementer: recovered Amelia-24 candidate, fixes owned by /root/transport_implementer.
Candidate inspected: 54502ddd23b86912ac2a5befa07acda5f8777eaf; parent 379cc24a. Current integration target c4f322bb.
Review started 21 October 2026. This is scoped review, not global qualification.

## Round 1: Incomplete; SnapshotApproval withheld

| Criterion / impact | Independently inspected evidence | Result |
| --- | --- | --- |
| AC1 worker stall, boundary, cumulative receipts | Actual Program / worker / ledger diff; native tests; M1, M2, M3b, M4 mutations | Production wiring is consistent. Never-published gap receipt accepted by ledger (Publish 5, receipt 3); implementer fixing. |
| Reset lifecycle / I2 | Reset keeps JS read queue and monotonic publication; ledger native test only comments Reset | Preserve pending receipt identity is sound. Actual Reset-with-pending-receipt proof missing. |
| AC2 typed memory query | Program Execute, router tests, FlatBuffers schema/codec, Browser QueryMemory; M5b/M6 | Typed request/ACK and phase/carriage controls present. Real production Program route not exercised by native fake router; affected browser proof under coordination. |
| AC3 old envelopes visibly rejected / I1 | worker RejectUnknownEnvelope; browser failure-or-rejected same handler; typed diagnostics | I1 equivalent observable behavior accepted; M8/M9/M10/M11/M12/M13/M14/M15 kills reviewed. |
| AC4 Node behavior | Independent command below, production scripts loaded in VM | 45/45 affected probes passed. Node host is fake managed runtime, not proof of C# host dispatch. |
| AC5 cat-023b three serial | Full Clean SerialAlone 4/4; serial-2 TAP says Interrupted | Incomplete, two clean runs still required on applicable candidate. |
| AC6 builds, tests, cumulative Chrome | Existing Tier1 logs and Full summary source/bundle identities inspected | Full 46 entries, 255/255; clock-master and cat-014 initial Contended entries have subsequent Clean reruns inside the same run; smoke pair also passes. Reviewer Tier1 pending stable corrected boundary. |
| Deletion and typed controls | Actual diff, deletion proof, re-add mutations | Named legacy channels/codec removed. Preserve old evidence as history. |
| Integration/publication | Candidate predates c4f322bb (9A.17 ports/footprints); no SnapshotApproval found | Merge relevant changes, inspect conflicts and selected impact checks; approval before further commit/push. Remote/deployed identity and required production-origin behavior pending. |

## Existing evidence applicability

Original Full: evidence-9a-3/e2e-full-54502ddd/summary.json.
Source 54502ddd23b86912ac2a5befa07acda5f8777eaf; suite digest a8fa589a43dc5061383c3048f25f7c8759e9e943c70dc8100ff225168ca7eb2b.
Bundle dc97ff8a48538cea8f278a1abd8f3606872d25f4f39b06099fcae3a778ba0198 unchanged start/end; served 303 files verified at start/end.
Chrome 155.0.8059.86, node v26.12.0, playwright 1.64.0-alpha-1790635538000.
All 46 result entries Pass; 44 unique TAP suites correspond to 46 executions including two reruns. The initial Contended clock-master and cat-014 results are followed by Clean reruns inside the same summary; no further repeat is needed.
Current owner verification-tiers policy supersedes older serial-everything AC wording. Full must not be repeated.
Existing original .anvil/e2e-runs/20261021T102500Z-91939 directory remains present (artifact subdirectories); committed/copied raw logs retained under evidence.
Candidate product changes from review fixes require affected proof refresh, not automatic invalidation of unrelated Full suites.

Existing Tier1 claims: 0-warning Debug/Release/PLAYTEST/web builds, 2283/2283 tests; Node every harness exit 0; Lifecycle 510/0, Wire 108/0; Ownership 22/22 rejected attacks; Anvil zero warnings.
Original tier1-node.sh last conditional returned false on success; corrected aggregate exit preserves underlying successful per-file logs. Review distinguishes shell bookkeeping from test failure.
Mutant log records invalid compile-only M3/M5 first attempts, then valid M3b/M5b behavioral kills; those first attempts do not count as kills.

## Independent raw command/result

Command (21 October 2026):
`node --experimental-vm-modules --test tools/workshop-observation.test.mjs tools/workshop-client-lifecycle.test.mjs tools/workshop-pose-ring.test.mjs`

Result: exit 0, tests 45, pass 45, fail 0, cancelled/skipped/todo 0, duration 406.03575 ms.
Observation harness controls in production and diagnostic modes passed: preparationClockOverlap, noEarlyWorld, pending, receiptsToLedger, queuedTerminal, activeCommand, matchingReceipt, outputFailureLogged, ordinaryReadNoExport, acknowledgementOnlyReset, retiredEnvelopesRejected, dispose.
Additional harness results: refusedReceiptIsAFailure, memoryQueryOnCommandTransport, startupControl 1/2, commandsReachTheManagedDecoder, schedulerTurnFailureEnvelope, disposalCancelsSchedulerTurns.
Pose-ring probes passed first-secondary/full-width identities, in-progress slot rejection, invalid whole-slot shape, grow capacity and wrap/shrink newest-frame behavior.
Full raw tool output is retained in the coordinator thread tool record (chunk a7a1ea).

No Chrome run was started by reviewer; implementer owns exclusive Chrome line.

## First-candidate lens triage

Each claim below was checked against actual source and the frozen intent, with current verification policy limiting blockers to story acceptance and introduced player-visible regressions. Implementer owns all fixes and spec changes.

| Lens item | Verdict | Reason / next action |
| --- | --- | --- |
| Blind B1: never-published gap receipt | Confirmed, blocking, fix inspected | Publish allows increasing gaps; original Receipt accepted them. New explicit membership guard rejects before retiring; regression test added. Independent green pending stable boundary. |
| B2 / Edge E2: receipt of generated but unposted read | Confirmed hardening opportunity, deferred | C# accepts generated publication and JS clears readInFlight; spoofing queued identities can retire undelivered data. Frozen mismatch definition names wrong session, never-published, retired; it does not require posted identity. Legitimate browser gets identities only from validated posted reads. No reachable legitimate-client violation established. File typed posted-identity hardening separately. |
| B3: mixed envelope bypasses unknown rejection | Confirmed, blocking | Worker matches known property first and ignores all extras, so bytes plus unknown channel key dispatches instead of rejecting atomically. Validate full envelope shape before effects, naming unsupported keys without old-name allowlist. |
| B4 / Edge E1: memory capture races Run | Confirmed, blocking | QueryMemory awaits Execute while a second Execute(Run) is permitted; pending limit is two, no observation reservation. Simulation can answer query then start Run before Browser continuation captures GC. Preserve stopped idle ownership until paired capture finishes. |
| B5: stale memory reply captured | Confirmed, blocking | Execute returns WorkshopDelivery with Applicable=false, but QueryMemory discards that flag and calls Complete/Capture anyway. Superseded/disposed replies must capture nothing. |
| B6: deleted late bound / I5 | Documentation correction; runtime strengthening deferred | Frozen AC has no 1000ms completion deadline. TimedOut is an indication, not cancellation or bound on pending promise. Correct I5 wording; bounded query completion belongs to separate hardening. |
| B7 / Edge E3 / Verification V1: manufactured memory execution proof | Confirmed, blocking proof gap | Native router callback manufactures ACK and Node managed host is fake. Removing Program Execute ObserveMemory branch leaves these tests green. Exercise actual managed executor with real transport; no new diagnostic UI/platform prerequisite. |
| B8: unbounded ledger while stopped | Deferred resource hardening | Running stops after 500ms; stopped mode has no continuous publication loop. Repeated adversarial unreceipted Step commands can grow ledger. Ordinary client receipts each read; no demonstrated ordinary lifecycle leak. Detailed resource stress stays at named later gate. |
| B9: spec filter selects absent test classes | Confirmed trivial docs fix | Actual class is TypedTransportCommandTests, not ReliableReadLedgerTests or ObserveMemoryQueryTests. Correct executable verification command. |
| B10: claims unknown kind precedes shape validation | Confirmed wording correction | DecodeCommand validates frame/session/revision before kind. No story AC requires a particular order for multiple malformed fields; describe actual order. |
| Edge E4: Reset clears task conflict | Accepted design correction, boundary proof pending | Task text is outside frozen intent; queues and publication identity survive Reset, so clearing falsely rejects legitimate in-flight receipt. New shell test covers delayed receipt over Reset dispatch; actual browser Reset case awaits run. |
| Verification V2: production stall wiring | Confirmed proof gap, fix pending execution | Fake LedgerHost reproduces condition rather than invoking Program host. New Chrome case withholds one receipt from real worker and asserts reason=ReliableStalled; passing this closes gap. Story-specific fault control is required now even though global injected-fault qualification is later. |

Implementation notified before further runtime proof. Reviewer has not modified deliverables or started Chrome.

## Integrated correction boundary: reviewer Tier 1

Integrated current main c4f322bb without a new commit. Runtime/test scoped digest:
`0b4f69691d5012d824b1094d87262be59ec275f6d6ead7530c7808dc04214a48`.
Recipe: git diff c4f322bb --name-only in git ordering; exclude _bmad-output/ and docs/; hash each path + NUL + SHA256(file bytes) + newline, using deleted for removed paths; SHA256 concatenated records.
This is a scoped candidate identity, not an approval of the entire historical backlog.

Fix inspection:
- B1 membership check precedes any retirement, preserves oldest age after rejected gaps; new test exercises gap before and after earlier retirement.
- B3 generic supported envelope shapes reject extra keys before any bootstrap, port, schedule, receipt or command effect. No old-name allowlist.
- B4/B5 Browser Execute calls memory owner admission; QueryMemory uses ExecuteCore under exclusive owner through synchronous capture. Pending commands reject query Busy; Run/Reset blocked while owned; Dispose permitted but current/applicable guard prevents stale capture. finally releases ownership on all outcomes. Async TaskCompletionSource tests exercise interleaving and capture callback exclusion.
- V1 query tests execute actual worker onmessage -> Program.Dispatch with typed bytes, inspect actual acknowledgement and collector measurement; Running expects WrongPhase/no memory. This fails if the Program query branch disappears. Source review confirms latest CCGPU is command ACK only (MachineWorld.Gpu.cs:211), and driver shutdown closes browser without issuing duplicate sequence.
- Reset test now waits for and retains the exact intercepted receipt identity before initiating Reset; browser execution remains pending.
- Real stall case asserts debt reason and retains a player-state screenshot for independent inspection.

Independent commands/results:
| Command | Result / original tool evidence |
| --- | --- |
| dotnet build CuriousContraptions.slnx -nologo && dotnet test CuriousContraptions.slnx -nologo --no-build | Exit 0. Tests: main 1960, E2eRunner 166, BundleElements 110, Coverage 77, WorkerAotGuard 17; total 2330, no failures. Raw chunks 917582, d1e903, b31dc5. Existing duplicate xUnit IDs in unchanged setting rows printed; test runner skipped duplicate identities, not test failures. |
| zsh evidence-9a-3/tier1-node.sh | Exit 0; 11 files, 115 tests, all pass. Raw chunks 2b88f6 and f86156. |
| dotnet build CuriousContraptions.slnx -nologo -c Release -v:q | Exit 0; 0 warnings/errors, 8.58s; chunk 844b76. |
| dotnet run --project tools/LifecycleContract --no-build | Exit 0, Count510 Failures0; chunk25716c. |
| dotnet run --project tools/WireContract --no-build | Exit0, Count108 Failures0; chunk25716c. |
| dotnet run --project tools/Ownership -c Release -- --oracles . | Exit0; original run chunk4c569c. |
| anvil check --changed --format plain | Exit0; 3 changed non-C# files, no warnings; chunke65b79. |
| git diff c4f322bb --check | Exit0; chunka13fc2. |

Ownership first attempt failed with CS1061 on PortDescriptor.Socket: its SourceInventory explicitly rebuilds Release with BuildProjectReferences=false, but only Debug dependencies had been refreshed after integrating 9A.17. Release solution rebuild restored matching references; Ownership then passed without product edits. This was a tool prerequisite failure, preserved here rather than treated as a passing first attempt.
Lifecycle/Wire output initially exceeded tool output limit; concise repeat recovered exact aggregate results. No repeated browser run was requested.

**Current verdict: Incomplete (code findings fixed; required browser/mutation/publication evidence pending).**
Pre-publication remaining: production publish and corrected real-worker query/stall/Reset tests, cat-023b three clean serial results with applicability established, selected integration/smoke proofs, corrective mutation kills, exact frozen candidate/bundle identities, current spec/status and deferred findings consistency.
Publication-dependent remaining: remote commit/tree and deployment identity verification, required-now production-origin behavior. SnapshotApproval has NOT been granted.

## SnapshotApproval — 21 October 2026

**SnapshotApproval: APPROVED for the exact integrated candidate below.** All required pre-publication criteria for Story 9A.3 pass within the justified transport/query and merged dependency scope. This is not terminal publication Pass or global qualification.

Exact candidate:
- Parent branch commit 54502ddd23b86912ac2a5befa07acda5f8777eaf with uncommitted integration of main c4f322bb and reviewed corrections.
- Tracked diff versus c4f322bb: 36 paths, identity `12fa9b774307787247b876b7f243324a601a473ce2ef30f6437b0042e7547071`. Recipe: git diff c4f322bb --name-only, for each path concatenate path + NUL + SHA256(current bytes) + newline, deleted marker for removals; hash concatenation. This includes current spec, epics and sprint followups.
- Runtime/test subset (exclude _bmad-output/ and docs/): `7627508906c0d8bdcd2235d89d0e3949f6dcedf6a2f7efe6eb7c2edb3645b0e6`.
- Current suite digest independently recomputed using E2eRunner SourceIdentity recipe: `b4df407d100259af59aa281f62329dd8a95ce4950e816a54f89b7ef00b3739ca`.
- Current AppBundle independently recomputed using BundleManifest recipe: `f382ae395445e3fc0331eb7d8c1582caeaa58a789f0c3aaf867511d74678e88c`, 336 files. This equals recovery-chrome-3 start/end identity.
- Original evidence directory snapshot: 217 files, identity `018e8ae8cf4d5061b47fd447d34124ae72ba1dd3cf6fd230c13ad7aa860dc7f3`; sorted evidence-relative path + tab + file SHA256 + newline, hash concatenation. Review record itself is outside that directory and is not self-hashed.

Pre-publication closure:
1. B1/B3/B4/B5 source fixes independently inspected and covered by reviewer Tier1. Five corrected decision mutations produce intended assertion failures, not compile-only kills. The first invalid current-ownership mutant is retained and superseded by a valid assertion-killing variant.
2. Independently compared restored ledger, memory owner and worker file hashes with recovery-mutants.txt: all three match; native 33/33 and Node restored green retained.
3. recovery-chrome-1 preserves the invalid combined 400ms/Reset test failure. Program.Schedule has a separate 250ms drain bound; correction separates a 400ms healthy coalescing control from a receipt held across actual Reset and released 50ms later. No product change or relaxed timeout. This is a test error, not a retried product failure.
4. recovery-chrome-2 has all 13 clock-master tests plus CAT-023b and smoke pair Clean, 25/25. Actual shipped Program.Dispatch observes memory while Building and rejects Running without capture. Actual worker unreceipted ledger stops with ReliableStalled. Exact pre-Reset held receipt is released after Reset begins, and another Run succeeds.
5. Reviewer visually inspected recovery-chrome-2/clock-master.simulation-unresponsive.reliable-stall.png: stopped ball, clock 0.55s, footer “The GPU simulation stopped. Reset to retry.” This closes player-visible AC1 evidence together with actual typed debt log.
6. CAT-023b passes 4/4 Clean SerialAlone in recovery runs 1, 2 and 3 consecutively. Run3 also passes smoke pair (12/12 total). Product inputs remained unchanged; only the clock-master suite correction changed between runs1/2. Each republish has a distinct retained bundle identity and matching start/end served checks: run1 bca7e42d…, run2 bec47d75…, run3 f382ae39…. No claim of byte-identical cross-publish bundles.
7. Original Full on 54502ddd remains reusable for unchanged element behavior: transport corrections reject malformed gaps/envelopes and protect the previously unused memory query; merged 9A.17 package/port inputs have their independently reviewed Full plus refreshed pkg-3 and smoke integration in recovery1. Real normal transport/lifecycle and changed edge cases are refreshed by clock-master/CAT/smoke; no physics/solver change requires replacing unrelated Full evidence. Original Full 44 unique suites includes its two successful clean reruns. No new Full was run or required.
8. All 16 lens rows retained in spec and reviewer matrix. F.20/F.21/F.22 have concrete acceptance in epics, matching backlog keys in sprint status, and correct relative review links. These capture deferred posted-identity, bounded-query completion and adversarial stopped-resource issues without weakening current acceptance. Unknown measurements stay unknown. Diff whitespace check passes.

Authorized next action: implementer may commit this candidate, merge its exact reviewed contents into main while preserving unrelated local edits/history, and push/deploy the approved snapshot. Repository protections remain required. A changed relevant source/contract input invalidates affected approval; review-only records and publication receipts do not require recursive review.

Publication-dependent checks required before terminal scoped Pass:
- Verify pushed remote commit and tree contain the approved source/contract identities; verify integration introduced no additional relevant changes.
- Verify deployed production-origin bundle identity, worker/bootstrap resources, isolation and actual Chrome execution correspond to the approved candidate (or a proven source-equivalent reproducible publication identity, recorded explicitly).
- On production origin, run affected actual worker memory-query/stall/Reset behavior and ordinary Run/Reset smoke; inspect player-visible stall state and unexpected console/resource/lifecycle failures. Reuse unchanged already-proven element behavior; do not repeat Full.
- Record deployment URL, exact served identity, original raw commands/results and final independent verdict. No completion/dependent-slice terminal claim until those checks pass.

## Terminal publication review — scoped Pass, 21 October 2026

**Terminal verdict: PASS for Story 9A.3, including required publication.** No unresolved unintended regression remains within the justified transport/query and integrated dependency scope. This does not claim global performance, supported-device or later resource/fault qualification.

Independent publication verification:
- Remote origin/main is `28e2bb9c77dd16026d460a4c23f1f915dd59792c`; approved candidate `52f855738f4d5109d8c0ce3754c0cf98109979a7` and published merge have identical tree `d5dcb82147ad96fe92d9120c3e56f6ca6a51c158`. Direct git diff exits0.
- [CI39595227818](https://github.com/aidanmorgan/tim/actions/runs/39595227818) completed build and deployment successfully. GitHub deployment7225578315 names the exact merge SHA, production environment and success at12:06:35UTC.
- CI artifact12343584517 is the published github-pages artifact. Independently downloaded and computed manifest identity `17dc85677719e924cbdd515c2e9e11031243d2507d616d6af5f90e3649dfeb31`:337files. All333served files fetched from [production](https://aidanmorgan.github.io/tim/) match artifact bytes;four dotfiles correctly NotServed. CI-specific bundle differs from local build as expected; provenance links it to the identical reviewed tree and production tests verify it directly.
- Chrome through available Playwright connector was checked directly, then unchanged connector-backed suites ran serially against the explicit production URL. No stale/local preview was used.
- Actual production worker controls pass4/4:400mscoalescing control,exact in-flight receipt across Reset,650msreceipt interruption producing ReliableStalled,Building memory capture and Running WrongPhase/no capture (memory cases are separate tests). Zero failures/skips.
- Reviewer inspected production screenshot: stopped basketball at0.54s and footer “The GPU simulation stopped. Reset to retry.” Production debt log names ReliableStalled.
- Both production smoke suites pass8/8:cross-origin isolation,pose ring,empty/single/dual-body UI construction,ordinary Run/Reset,Save/Load and invalid-save/unknown-element negative controls,with clean shutdown checks.

Original evidence: [production commands and results](evidence-9a-3/production/commands.log), [clock probes](evidence-9a-3/production/clock-master.tap), [smoke](evidence-9a-3/production/smoke.tap), [served identities](evidence-9a-3/production/deployed-identity.json), [player-visible stall](evidence-9a-3/production/clock-master.reliable-stall.png).
Runtime browser time:26.677s transport probes plus36.404s smoke. Unknown wider delivery/blocked-time measurements remain unknown.

The coordinator may mark this story done and continue the next authorized backlog outcome. F.20/F.21/F.22 remain named later qualification work, not unresolved current acceptance. Review-only evidence publication does not require recursive approval or another browser run when relevant runtime inputs remain unchanged.





