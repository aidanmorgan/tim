---
title: '9A.24 — typed read-only production observation'
type: 'refactor'
created: '2026-10-29'
status: 'done'
baseline_revision: '7e12c9452a165f42ab2ef9c8b80964f237a05b00'
review_loop_iteration: 1
followup_review_recommended: false
context:
  - '{project-root}/AGENTS.md'
  - '{project-root}/docs/testing-guidance.md'
warnings: [oversized]
deferred: []
---

<intent-contract>

## Intent

**Problem:** Production observation still depends on console records and mutable global hooks. Story9A.24/C4/C5 requires a single typed read-only port exposing command receipts, committed queries and independent event cursors.

**Approach:** Publish observations from the existing admitted C# command/read path and shared event reader. Replace production console/global publishers with typed observations, retaining their meaningful state, fault, clock, transfer and capture information.

## Boundaries & Constraints

**Always:** One production artifact/path; C# owns domain decoding, validation and DTO construction, strict TypeScript owns browser plumbing. Exact typed identities and deliberate timer contracts remain. Observations must neither issue commands nor consume UI/animation delivery, hold ACK leases, expose shared storage or affect a committed simulation. All collections start with sensible category defaults and grow geometrically at demand watermarks without arbitrary item caps. Preserve existing event retention/lapping semantics. Native/node behavior, local full suite, normal hooks and five Auto layers gate publication.

**Never:** Second FlatBuffers decoder in TS, JSON/ad-hoc worker messages, global event bus, mutable global hook, version field, fallback/compatibility publisher, solver change, new element, output identity oracle, precision framework, skipped game tests, mutants, CI monitoring, local Release worker build or web publish. The current stabilization row explicitly defers browser consumers: do not migrate/run browser suites as this slice's gate or claim repository-wide zero hits including those consumers and historical evidence.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|---|---|---|---|
| Receipts | Admitted terminal ACKs, including delayed/inapplicable-to-presentation replies | Session, command sequence, dispatched command kind and result; each accepted terminal receipt once | Invalid/foreign/duplicate input retains current refusal; never infer command kind from response kind |
| Committed query | Latest managed-admitted state; failed command; repeated query | Detached typed fields matching admitted read; rejected commands preserve state; no UI delivery consumed | Unknown query/invalid identity returns typed refusal |
| Read-only boundary | Nested mutation attempt, absent command method, retained prior DTO | Game state and later snapshots unchanged; frozen JS graph | Mutation rejected, no shared views or live mutable aliases escape |
| Event readers | UI plus two observers; burst/growth/Reset | Independent positions, same-epoch continuity and explicit skipped/lapped counts | Existing epoch/admission rules; closed/retired handle refuses |
| Lifetime/growth | Beyond initial receipt/cursor/diagnostic capacity; Reset; disposal/new session | Growth preserves observations; Reset preserves session receipt identity; close releases cursor | Retired session refuses live access but terminal explanation remains readable |
| Diagnostics | Fault/rejection, clock report, transfer before read coalescing, trace chunks | Typed site/category/values/detail and capture completeness retain current information | Observation failure cannot roll back or fault already admitted gameplay |

</intent-contract>

## Code Map

- Authority: `_bmad-output/planning-artifacts/epics.md` Story9A.24 and stabilization row105; `docs/architecture/element-package-isolation.md` invariants3,4,7–9,§2.2/2.3,C4/C5.9A.23 is delivered per current sprint status despite its older spec header. Browser migration/full Chrome acceptance remains post-catalogue.
- `engine/gpu/BrowserWorkshopClient.cs`: ExecuteCore validates ACKs through _cursor.PrepareAcknowledgement; ReceiveRead validates schedule/cursor. Attach receipts after validation, committed snapshot alongside applied _cursor.Commit. Never drain _history.TryTakeLatest or acquire gameplay leases for observers.
- `engine/gpu/IWorkshopClient.cs`: IWorkshopBrowserTransport.CreateClient already carries managed callbacks. `WorkshopMessages.cs` defines command/response identities; response Kind is ACK/Read, not command kind.
- `engine/gpu/WorkshopObservation.cs`: session owner and typed result/query/handle contracts. `WorkshopObservationProjection.cs`: deterministic response, diagnostic, capture and construction mappings through the shared external-object boundary. `BrowserWorkshopObservation.cs`: browser-only import/discovery and JSObject adapter for those mappings. Reuse existing domain reads/identity/capacity helpers.
- `CuriousContraptions.web/wwwroot/workshop-client.ts`: client lifecycle, admitted-state callback plumbing and current global publishers near597–607; replace globals with exported module discovery of a frozen facade in new `workshop-observation.ts`.
- `CuriousContraptions.web/wwwroot/event-ring-cursor.ts`, `CuriousContraptions.Abstractions/EventRingBuffer.cs` (EventRingDelivery): reuse one transport cursor and C# decoding. Existing replacement attachment reaches UI; new independent observer cursors must follow it without exposing the buffer.
- `schemas/workshop_wire.fbs`, existing generated bindings and `engine/gpu/WorkshopWireFlatBuffers.cs`: extend the existing typed worker transport for diagnostic/capture records. No alternative wire parser or arbitrary postMessage fields.
- `engine/MachineWorld.Gpu.cs`: delete LogWorkshop ACK publisher. `ui/WorkshopAnimation.cs`: delete solved-console-only epoch bookkeeping, preserve bindings.
- `engine/gpu/WorkshopDiagnostics.cs` and callers in `CuriousContraptions.Simulation/Program.cs`, `CuriousContraptions.Simulation/WorkshopTrace.cs`, `ui/Workshop.cs`, `ui/WorkshopUiFailure.cs`, `engine/MachineWorld.Gpu.cs`, `WorkshopSimulation.cs`, `BrowserWorkshopClient.cs`: thread explicit session-owned typed diagnostics. Preserve unexpected fault versus reported rejection and terminal detail.
- `engine/gpu/WorkshopClockMapping.cs`, `WorkshopClockDerivation.cs`, `AnimationControlDeferral.cs`: typed counters/recalibration/derivation/deferral observations, preserving existing timing/report cadence.
- `CuriousContraptions.Simulation/Program.cs`: debt/tick failure and retained balance-transfer evidence before read coalescing; `engine/gpu/WorkshopRead.cs` CarriesBalanceTransfer is currently console-selection-only—delete or give it only the new typed transfer responsibility. `WorkshopTrace.cs` retains metadata/chunks/completeness including PLAYTEST trace output boundary. `CuriousContraptions.Simulation/wwwroot/worker.ts` carries typed trace-flush faults.
- New `CuriousContraptions.tests/WorkshopObservationTests.cs` and `tools/runtime/workshop-observation.test.ts`; affected client, event, clock, diagnostic, trace and UI-failure tests exercise real production interfaces. Reuse the native Workshop integration harness/browser external transport.
- `tools/workshop-console.ts` and its test are browser-only consumers already explicitly deferred in `tools/E2eRunner/BrowserDeferral.cs:49–51`; retain as pending browser migration, not active runtime compatibility. Existing SelectionTests only verifies importer selection. No new deferral/skips are needed.

## Tasks & Acceptance

**Execution:**
- `engine/gpu/WorkshopObservation.cs`, `BrowserWorkshopObservation.cs`, `BrowserWorkshopClient.cs`, `IWorkshopClient.cs`, `CuriousContraptions.web/wwwroot/workshop-observation.ts` -- implement the read-only contract on the sole managed-admitted state.
- `CuriousContraptions.web/wwwroot/event-ring-cursor.ts`, `workshop-client.ts` and the observation owner -- fork private independent readers, release on close/session retirement and preserve existing growth/epoch semantics.
- `schemas/workshop_wire.fbs`, `WorkshopWireFlatBuffers.cs`, generated bindings and diagnostic/worker callers listed above -- replace console observation with typed transport and explicitly owned state; migrate existing native/node assertions to typed observations.
- `engine/MachineWorld.Gpu.cs`, `ui/WorkshopAnimation.cs`, `workshop-client.ts` -- delete superseded ACK/solved/global hooks and console-only bookkeeping in the same change.
- `CuriousContraptions.tests/WorkshopObservationTests.cs`, affected native tests, `tools/runtime/workshop-observation.test.ts`, runtime harnesses and `tools/runtime/RuntimeTypeScript.targets` -- include new strict TS module in pinned compilation and test the shipped output; prove every matrix case without browser gates.
- `TODO.md`, `sprint-status.yaml`, `epics.md`, affected architecture C4/C5 row and this spec -- record production deletion and explicit deferred browser consumer migration; preserve requirement IDs and later acceptance. Prior hygiene publication receipt is already recorded in its spec.

**Acceptance Criteria:**
- Given a real production simulation/client, when authoring/Run/Pause/Step/Reset/save/load and rejected commands are observed, then port receipts and committed queries agree with admitted state and preserve gameplay/lifecycle without observer authority.
- Given delayed ACK completion, when a consumer advances its observation cursor, then later completion of a lower command sequence remains observable; receipt publication order must be distinct from command sequence.
- Given two observers and the UI, when they read at different rates across buffer growth and Reset, then each has independent delivery and existing explicit lapping/epoch behavior.
- Given retained nested results, when consumers attempt mutation and later commands advance the game, then neither modifies the other; closed/foreign/retired queries refuse through typed results.
- Given all active production sources and callers, when searched after replacement, then CCGPU, CCGOAL_SOLVED and globalThis.Workshop observation publishers have zero production hits. Old-format test tools are only the explicitly deferred browser consumers; no cosmetic rename substitutes for typed preservation.
- Given affected fault/clock/transfer/trace observations, when ordinary diagnostics and real failure/refusal cases occur, then typed data preserves meaningful fields and completion status without changing gameplay outcomes.
- Given the frozen candidate, when required local tests/hooks and independent Auto review complete, then zero failures/skips and exact-SHA approval support publication; browser/release/deployment qualification remains pending.

## Spec Change Log

### 2026-10-29 — Review repair iteration 1
The intent remains unchanged. Review found implementation-level ambiguity about the outer observation surface and production wiring. Amendments below prevent hidden startup failures, apparently complete missing diagnostics, incomplete capture DTOs and untyped closed sets. Preserve the reviewed candidate in `.tmp/observation-review-20261029.diff` (full baseline-relative diff); original test logs remain evidence, not a passing final round.

**KEEP:** session ownership; delayed receipt publication ordinals; managed admission and detached queries; independent event-ring transport readers with existing retention/growth/epoch rules; concrete diagnostic records and explicit-kind/exact-one-payload wire validation; f32 game values and exact external identity strings; contained observation failures; removal of superseded console/global/raw hooks; no solver changes; supported Run/Pause/Step/Reset/save/load behavior; existing meaningful tests and deletion coverage; all five compiler logs and original failed/passing results. Retain the working implementation of unaffected parts, rather than redesigning them.

**Re-derivation procedure:** before changing code, revert this slice's code/test/build changes to baseline under mandatory Anvil validation, preserving this spec, current documentation and the saved patch. The saved candidate is available to recover the KEEP implementation into the re-derived change; do not blindly restore rejected parts. Reapply unchanged valid code and make the bounded repairs below. Do not delete original evidence or touch another worktree. Same implementation owner handles this repair loop. No full suite on the implementer side; coordinator runs it after the repaired closure is ready.

**Additional implementation requirements (outside immutable intent):**
1. Establish one session observation lifecycle before bundle/client startup can fail. Discovery must retain a readable typed terminal explanation for bundle-load, bootstrap, qualification and Initialize failures. Do not create a separate early-failure port, console fallback or compatibility channel. Keep ordinary successful startup and retirement semantics. Verify failures through the production lifecycle with genuine external-boundary doubles; Node verifies discovery lifecycle. Browser execution remains deferred.
2. Worker publication failure must become sticky observable incompleteness at the receiving port. Preserve its meaningful exception detail, including a dropped capture/transfer record, without faulting gameplay. Use the existing typed transport, no ad-hoc messages or alternate publisher; later successful publication must not appear complete after earlier loss. Verify the production producer/codec/receiver path with failure then recovery at the external publication boundary. Do not claim delivery over an entirely unavailable transport; that transport's retirement must remain explicit.
3. Validate capture semantics at the codec boundary: Complete cannot accompany a non-Complete End or a terminal total different from ExpectedCount. Respect chunk-local Count versus capture total. Add positive and malformed controls using the real codec.
4. Project complete capture construction, including puzzle identity/configuration and connection Domain. Preserve all meaningful fields of the current command capture at the browser DTO boundary. Verify the actual production projection shape, not manually supplied lookalike DTOs alone; do not add a parallel serializer or JSON route.
5. Closed sets in the strict TypeScript facade remain enums, including command kind/outcome/reason, simulation/capture/event phases, domains, fault sites and diagnostic subtypes. Numeric interop conversion stays at the external boundary with canonical value validation. Update compiled-output consumers/tests and meaningful mapping/refusal coverage; no duplicate domain decoder in TS.
6. Transfer-retention verification must execute the production publication behavior before coalescing and observe retained records after later reads, without constructing the expected WorkshopTransferDiagnostic in the test. Factor only the smallest shared production seam if necessary; worker and native harness use the same rule.
7. Exercise actual workshop-client exported open/read/close observation functions with two observers and the UI over nonempty events, growth and Reset. Assert independent delivery and close/retirement without reaching into private fields.
8. Remove the exception-name fallback; retain one canonical naming rule or explicit typed failure containment.

These are bounded completeness repairs to the current slice. They do not authorize a new telemetry platform, stricter physics assertions, browser gates, mutants, arbitrary capacities or new puzzle elements. Update affected docs and matrix coverage with honest surface limitations. Prior passing proof is reusable only for unchanged relevant inputs; rebuild/test the changed closure once and report exact commands/results.

## Review Triage Log

### 2026-10-29 — Review pass 1
- verdicts: 13 findings — high 1, medium 12, low 0, false 0, maybe-false 0
- findings:
  - `medium` `bad_spec` Blind: worker publication failure remains local — WorkshopDiagnostics.Record/Capture set Incomplete, but worker publishers encode only records and ReceiveObservation never sees that state. Require sticky completeness propagation through the existing typed channel.
  - `medium` `bad_spec` Blind: startup port is undiscoverable — BrowserWorkshopClient.Create installs only after Initialize; earlier failures dispose without discovery. Require observation ownership and discovery before startup failure points.
  - `medium` `bad_spec` Edge: worker publication failure remains local — same verified producer/receiver gap; same completeness amendment.
  - `medium` `bad_spec` Edge: startup diagnostics lose their surface — same late Install call; same startup lifecycle amendment.
  - `medium` `patch` Edge: contradictory complete capture metadata is admitted — ValidateDiagnostic checks ranges and payload lengths but not Complete versus End/count. Require semantic refusal and positive/control coverage; Chunk.Count is local, terminal Count is total.
  - `medium` `bad_spec` Verification: transfer test injects its expected diagnostic — test constructs the record instead of executing Program.EmitRead publication. Require production publisher verification; shared production implementation, no duplicated harness rule.
  - `medium` `bad_spec` Verification: event-reader independence stops below client wiring — direct fork tests cannot catch client registration of the UI cursor. Require two observers plus UI through shipped client exports over growth and Reset.
  - `high` `patch` Forward: exception-name fallback — FullName ?? Name selects two naming rules. Use one canonical naming rule or explicit refusal; no fallback retained.
  - `medium` `bad_spec` Intent: startup/bundle faults lose discovery — ui/Workshop.ReadyAsync can fail loading the bundle before client creation, so merely moving Install inside Create is insufficient. Require the same observation lifecycle to cover this earlier boundary.
  - `medium` `bad_spec` Intent: worker publication failure not exposed — same verified sticky-failure propagation amendment.
  - `medium` `bad_spec` Intent: capture construction drops puzzle and connection domain — BrowserWorkshopObservation.Construction omits WorkshopConstruction.Puzzle and WorkshopConnection.Domain while the former command capture carried them. Require complete consumer-facing projection.
  - `medium` `bad_spec` Intent: closed sets degrade to numbers — command kinds, outcomes, reasons, phases, domains and fault sites are number fields in workshop-observation.ts. Require enum-typed DTO fields and canonical mappings at the genuine interop boundary.
  - `medium` `bad_spec` Intent: transfer test supplies observations — same verification gap and production publisher amendment.

All five layers completed before triage. Repeated claims retain separate rows. Group roots: worker completeness, startup lifecycle, capture validation, transfer publication proof, client cursor proof, exception naming, capture projection, closed-set typing. Bad-spec loopback takes precedence; patch findings remain required in re-derivation. No findings deferred or rejected. Independent layer identities: observation_blind, observation_edge, observation_verification, observation_forward, observation_intent.

### 2026-10-29 — Review pass 2 (patched; independent re-review passed)
- verdicts: 7 findings — high 2, medium 5, low 0, false 0, maybe-false 0
- findings:
  - `high` `patch` Blind: startup import yields before scene initialization — ReadyAsync awaits Start before World/interface/guidance creation, while ProcessFrame dereferences those controls. Move scene construction before this await and handle failure/removal through the existing lifecycle.
  - `medium` `patch` Verification: managed event epoch/content is untested — native ReadObservationEvents returns records labelled with the requested epoch regardless of actual admission; Node supplies its own epoch. Add nonempty causal delivery through two managed cursors over Reset with faithful epoch admission.
  - `medium` `patch` Verification: response/diagnostic projection is bypassed — construction alone uses the native-tested external-object seam; native domain assertions and supplied Node DTOs cannot catch omitted captures or reversed interval fields. Reuse the existing projection boundary for those mappings and exercise representative state/identities/clock values.
  - `high` `patch` Edge: startup import exposes uninitialized scene to frames — same verified ReadyAsync/ProcessFrame ordering defect; same lifecycle correction.
  - `medium` `patch` Edge: local incompleteness retains cursors — Terminal derives from Diagnostics.Incomplete, but only received worker incompleteness calls Retire. CloseEvents then refuses while local readers stay retained. Route the first local transition through existing retirement and verify external reader release.
  - `medium` `patch` Edge: transport error loses stack/cause — fail passes String(error) to retiredCallback; the managed wrapper cannot recover the original stack or nested cause. Preserve available external fault detail on that same typed boundary.
  - `medium` `patch` Intent: trace sample invents response metadata — BrowserWorkshopObservation wraps WorkshopTraceSample in a default WorkshopResponse, while TS declares CommittedObservation samples. Narrow samples to actual epoch/tick/bodies/captures and remove fabricated response fields; verify the real projection.

All five layers completed before triage. Independent identities: observation_r1_blind, observation_r1_edge, observation_r1_verification, observation_r1_intent, observation_r1_forward. Forward-only Pass has zero findings; its initial partial-read limitation was resolved before acceptance. No findings rejected/deferred. Six root groups (startup duplicates retained separately) are direct corrections within existing interfaces/state; no new public observation API, speculative guard or spec-level redesign is required. Same implementer completed all six patch groups and affected checks in the patch-closure record below. All five original layers independently re-reviewed the repair delta and returned scoped Pass; the high startup risk is closed.

## Design Notes

Use a session-owned observation model. Receipts retain their command identity but use a separate monotonically increasing publication ordinal for polling, so out-of-order completion cannot disappear. Journals/cursors grow with demand; no invented128-entry cap. Reset changes world epoch, not session receipt identity. Disposal retires live access and releases cursor resources while preserving terminal explanation until the session handle is replaced/released.

Project detached C# DTOs through a narrow genuine browser interop adapter (managed callbacks and JSObject object/array construction; no JSON wire). At this external boundary,64-bit IDs use decimal strings, closed enums validated integers and f32 values JS numbers. Freeze the complete returned graph. Domain logic stays in C#; TS neither decodes FlatBuffers nor derives game state. Expose existing pose/animation observation information as typed queries as needed to replace the removed hooks, without consuming live presentation output.

Diagnostic records are concrete sealed payloads for faults, clock producers, balance transfers and captures. The existing FlatBuffers schema uses an explicit kind plus exactly one matching concrete payload; the decoder rejects absent, mismatched or multiple payloads. An initial union exposed the pinned v25.2.10 verifier's VerifyUnion type-offset defect (failed observation-projection-affected-1.trx). The explicit-table design preserves the vendor contract without bypassing verification; abandoned union bindings were regenerated away. Recalibration replies retain their individual typed stamps, including reversed replies that caused recovery, rather than passing those diagnostic facts through the valid-reply encoder.

Browser-origin admitted clock samples, animation samples and applied presentation frames join the same managed session journal without a worker round trip. Scalar host-duration observations retain their existing f64 lane; game values remain f32, cells/counts stay integer and exact64/128-bit identifiers/stamps project as decimal strings. The browser facade freezes concrete readonly DTO graphs and exposes no decoder or command.

Keep diagnostics session-owned, not static global mutable state. Worker diagnostics/capture messages extend the existing schema/codec/transport. Current diagnostic cadence, trace capture limits and event0.5s retention are existing semantic contracts, not permission for arbitrary collection caps. Future browser consumers must migrate to this port; retaining their disabled source does not justify retaining production aliases.

## Verification

Implementer runs matching incremental builds and affected native/compiled-node checks. Coordinator owns the single final complete solution round after implementation returns; do not duplicate the full suite on both sides.

- Follow `docs/delivery-workflow.md#local-test-authority`: pinned tools, matching affected compiler capture, scoped native/compiled-node checks; reuse unchanged evidence.
- Final complete solution round with five matching compiler logs, generated browser-only deferral filter, TRX and TreatNoTestsAsError: all required tests pass, Skipped:0. No local Release worker build/web publish.
- `git diff --check`; scoped production deletion search, schema/caller/build inclusion and documentation consistency.
- Normal hooks plus five Auto layers, affected repair re-review and exact-SHA approval; push and verify remote main without waiting for CI.

## Implementation evidence — 29 October 2026

Implementation is frozen for coordinator verification; this is scoped native/node evidence, not independent approval or browser/release qualification. Receipts, committed reads, independent event cursors and concrete diagnostic/capture projections are implemented. Production CCGPU, CCGOAL_SOLVED and globalThis.Workshop publisher searches return zero hits. Temporary generated C# under .artifacts was removed; only canonical bindings remain. git diff --check passes.

Matching incremental builds used `dotnet build <project> -c Debug --nologo --no-restore -noAutoResponse -p:PlaytestDiagnostics=true -bl:<path> -v:q`, all with zero warnings/errors:
- Tests/main: /tmp/observation-final-repairs.binlog.
- Simulation: /tmp/observation-simulation-affected-2.binlog.
- Animation: /tmp/observation-animation-final.binlog.

Native commands used `dotnet test CuriousContraptions.tests/CuriousContraptions.tests.csproj -c Debug --no-build --no-restore --filter '<classes>' --nologo --logger 'trx;LogFileName=<file>'`. Original TRX files remain under CuriousContraptions.tests/TestResults:
- observation-explicit-payload-affected-1.trx: 165 passed, 0 failed, 0 skipped; typed transport and producer repair.
- observation-final-affected-1.trx: 197/0/0, including ElementIsolationTests after exact generic inventories were updated. Known element debt is unchanged.
- observation-final-closure.trx: 842/2/0 of 844. Classes: WorkshopObservationTests, WorkshopClockDerivationTests, WorkshopWireTests, WorkshopTraceTests, AnimationControlDeferralTests, WorkshopUiFailureTests, BasketballResourceTests, F32SaveBoundaryTests, WorkshopActivationAnimationTests, WorkshopSimulationTests, PalettePlacementTests, WorkerAbiTests, ElementIsolationTests, PoseRingEncapsulationTests, EventRingConsumerTests, EventRingProducerTests, WireFaultTests. Filter joins FullyQualifiedName~<class> with |.
- observation-final-repairs.trx: 48/0/0; WorkshopObservationTests and EventRingConsumerTests. Repairs use the real save codec/load Construct flow (Save acknowledges the external save) and update the exact permitted view inventory for owned diagnostic byte copies and removed legacy evidence buffers. No other source changed after the expanded closure.
- Earlier failed evidence retained: observation-typed-affected-1.trx (569/1/0), observation-projection-affected-1.trx (217/7/0), observation-isolation-2.trx (26/1/0). The projection run includes the vendor union defect, reversed recalibration diagnostic stamps and missing full compiler-proof configuration. Coordinator owns the final production GameFloatGuard with all five matching compiler logs.

Compiled Node checks use `node --experimental-vm-modules --test`:
- .runtime-build/tools/runtime/workshop-client-lifecycle.test.js and .runtime-build/tools/runtime/pose-ring.test.js: 47/0/0.
- .runtime-build/tools/runtime/workshop-observation.test.js: 12/0/0, including concrete deep-frozen diagnostic DTOs and exact large integer strings.
- Event-ring harness: 14/0/0 retained from the initial combined 74/0/0 affected run; event-ring sources did not change after that check.

Removed binary presentation-ring and raw clock evidence assertions are covered by actual managed RecordPresentation/clock observation tests; removed public pose hooks are covered through the real PoseReader plus client lifecycle, without a replacement test-only hook. Console diagnostic assertions now inspect typed session-owned records, preserving fault/rejection, derivation/recalibration, transfer-before-coalescing and capture completion behavior. Full solution, five compiler-log guard, independent Auto review and publication remain coordinator work. Deferred browser consumers retain their existing explicit deferral and must migrate at the post-catalogue gate.

Coordinator verification found a missed explicit source-link consumer before the full suite started: tools/LifecycleContract linked only the clock mapping/native-clock files. It now references the production game assembly, so its controls exercise the real diagnostic dependency closure without duplicate linked types. Other explicit engine source-link projects were inspected; the worker hosts already include the full gpu source set. Matching `dotnet build tools/LifecycleContract/LifecycleContract.csproj -c Debug --nologo -noAutoResponse -p:PlaytestDiagnostics=true -bl:/tmp/observation-lifecycle-repair.binlog -v:q` passed with zero warnings/errors. `dotnet run --project tools/LifecycleContract/LifecycleContract.csproj -c Debug --no-build --no-restore` passed all 510 controls, failures 0; original output is retained in CuriousContraptions.tests/TestResults/observation-lifecycle-repair.log. The coordinator's original failed build remains /private/tmp/tim-observation-final.8rtj9K/playtest.log; no full tests ran in that attempt. Extra EOF whitespace in WorkshopObservation.cs was removed; `git diff --check HEAD` passes. Implementation refrozen for the resumed coordinator round.

The coordinator's next compiler captures passed in /private/tmp/tim-observation-final.NWhKTT; its GameFloatGuard preflight then failed 10/1/0 before the full suite started. The guard now recognizes the final SimpleName of a direct member-access argument as the same float/integer external conversion, without walking through arithmetic. Five permanent semantic controls admit float/uint member projection and f32 arithmetic, and reject double casts/arithmetic. Exact parameter classifications cover only WorkshopTrace.Record.milliseconds and WorkshopCaptureDiagnostic's constructor Milliseconds parameter; no method/class exemption was added for either. Production numeric types and calculations are unchanged.

Matching test build /tmp/observation-guard-repair-2.binlog passed with zero warnings/errors. With CSHARP_STATIC_GATE_BINLOGS=/private/tmp/tim-observation-final.NWhKTT, the scoped GameFloatGuardTests run passed 16/0/0 (observation-guard-repair-2.trx). The first repair build's xUnit cancellation-token analyzer failure and accidentally premature old-DLL guard run remain in /tmp/observation-guard-repair.binlog and observation-guard-repair.trx; they are not current proof. Stale stdout/stderr descriptions and test names were updated to typed diagnostics. The main-source comment edit in ui/WorkshopBenchPlacement.cs requires refreshed solution/playtest/web source captures for exact-input evidence; CheckStagedTests and VerifyPolicy inputs remain unchanged. Coordinator owns those refreshes and the full round. git diff --check HEAD passes.

Coordinator full round /private/tmp/tim-observation-full.UvKlSt completed with 5,020 passed, 2 failed, 0 skipped (main project 4,528/2/0; other projects 492/0/0). Original full TRX files remain intact. Both failures were exact guard bookkeeping: the first-party BuildRuntimeTypeScript target pin now covers the observation module's output/copy/cleanup additions, and WorkshopFormatTests permits the existing .NET Versioning namespace only in BrowserWorkshopObservation.cs, matching the worker attribute entries. Neither guard logic nor production source changed.

Matching incremental test build /tmp/observation-full-guard-repairs.binlog passed with zero warnings/errors. With CSHARP_STATIC_GATE_BINLOGS=/private/tmp/tim-observation-full.UvKlSt, the scoped filter FullyQualifiedName~RepositoryMsBuildLogicIsOnlyThePinnedTargetsAndImports|FullyQualifiedName~NoProductionSourceNamesAFormatVersion passed 2/0/0; evidence: CuriousContraptions.tests/TestResults/observation-full-guard-repairs.trx. Original production compiler captures remain applicable. git diff --check HEAD passes. Implementation refrozen for independent review; coordinator retains the final required full round before publication.

### Review repair iteration 1 — implementation closure

The required re-derivation was executed: all 69 changed code/test/build files were returned to baseline under Anvil validation, preserving current documentation, original evidence and .tmp/observation-review-20261029.diff. Unaffected KEEP behavior was recovered from that patch; the rejected surfaces were re-derived as follows. This is implementer evidence pending independent review, not a final full-suite or publication Pass.

| Finding group | Repair and actual production-path proof |
| --- | --- |
| Startup discovery (Blind/Edge/Intent) | One WorkshopObservation owner begins before Workshop bundle loading and is registered before client startup. The same owner binds event transport only after CreateClient returns; early event access refuses NotCommitted. Bundle errors and bootstrap/qualification/Initialize failures retain terminal detail on that owner. Real Workshop scene missing/invalid bundle tests and BrowserWorkshopClient external transport failures cover the paths; compiled Node covers retained discovery. |
| Sticky worker incompleteness (Blind/Edge/Intent) | WorkshopDiagnostics.Publish sends the first loss detail in the existing typed observation batch on subsequent successful publication. Record keeps the first failure. ReceiveObservation marks incompleteness and retires the same port, releasing event cursors. Real capture and transfer publication failures followed by recovery exercise producer, codec and receiver; gameplay Save/Pause remains available. Entirely unavailable transport cannot deliver diagnostics and still uses explicit transport retirement. |
| Capture semantic validation (Edge) | Complete requires Complete end reason, and End.Count equals ExpectedCount. Chunk.Count remains local. Actual codec positive and malformed encode/decode tests cover contradictory metadata and the chunk distinction. |
| Capture projection (Intent) | Shared C# construction projection now includes command identity, puzzle identity/precision/inventory/goal and all authored assistance knots, plus connection Domain. Browser JSObject construction uses that same typed external-object boundary; native tests inspect the actual resulting shape through a genuine external-object double, not a lookalike DTO. |
| Closed sets (Intent) | Strict TS DTO fields use canonical managed enum counterparts. C# validates enum values at projection; compiled Node checks canonical numeric mappings and unused-value holes. Clear activations/empty timer occurrences have no kind, represented by an absent optional enum field. Event/cause phases and presentation time phases remain fractional time quantities, not closed state enums. |
| Transfer production proof (Verification/Intent) | Worker EmitRead and native advancement both invoke WorkshopDiagnostics.RetainTransfer before ordinary read coalescing. The retained-transfer test no longer constructs a WorkshopTransferDiagnostic. The failure/recovery test exercises the same publisher with an external sink interruption. |
| Client cursor proof (Verification) | Compiled tests call exported workshop-client openObservationEvents/readObservationEvents/closeObservationEvents and UI readEvents over nonempty events, growth and Reset, with two readers; close/disposal refuse later access. No private cursor access. |
| Exception naming (Forward) | One canonical Type.ToString rule replaces FullName ?? Name. No exception-name fallback remains. |

Pinned flatc25.2.10 regenerated the extended batch bindings; temporary generated C# was removed. Exact generic isolation rows describe new clock/capture/declared-capability enum names; Known element debt is unchanged. No solver changes or precision obligations were introduced.

Matching incremental builds all passed with zero warnings/errors:
- /tmp/observation-review1-build-5.binlog — test/main closure (same Debug/no-restore/noAutoResponse/PlaytestDiagnostics build command recorded above).
- /tmp/observation-review1-simulation.binlog — CuriousContraptions.Simulation/CuriousContraptions.Simulation.csproj.
- /tmp/observation-review1-animation.binlog — CuriousContraptions.Animation.Worker/CuriousContraptions.Animation.Worker.csproj.

Native commands use the recorded dotnet test --no-build --no-restore pattern, with TRX under CuriousContraptions.tests/TestResults:
- observation-review1-closure.trx: **361/0/0**. Filter joins FullyQualifiedName~ with | for WorkshopObservationTests, WorkshopIntegrationStartupTests, WorkshopTraceTests, WorkshopActivationAnimationTests, WorkshopWireTests, ElementIsolationTests, WorkshopFormatTests, WorkshopUiFailureTests, WorkshopClockDerivationTests, AnimationControlDeferralTests, EventRingConsumerTests and RepositoryMsBuildLogicIsOnlyThePinnedTargetsAndImports.
- observation-review1-publication.trx: **45/0/0**, WorkshopObservationTests and WorkshopActivationAnimationTests, after adding the explicit lost-transfer/recovery case. Other relevant inputs from the 361-case run are unchanged.
- Original failures retained: observation-review1-affected-1.trx **190/4/0** (new malformed fixtures omitted required empty vectors, immutable-array identity comparison, and bundle-message path assumption); observation-review1-affected-2.trx **62/1/0** (exact generic enum inventory). /tmp/observation-review1-build-1.binlog retains the nullable callback compiler failure. Later build-2, build-3 and build-4 logs remain intact.

Compiled Node command remains node --experimental-vm-modules --test:
- Initial affected workshop-observation, workshop-client-lifecycle and pose-ring harness run: **61/0/0**.
- Final .runtime-build/tools/runtime/workshop-observation.test.js: **15/0/0**, including added canonical enum mapping coverage. The unchanged lifecycle/pose checks remain **47/0/0** from the combined run.

Scoped searches for CCGPU, CCGOAL_SOLVED, globalThis.Workshop and the removed FullName ?? naming fallback have zero hits; git diff --check HEAD passes. Coordinator must refresh solution/playtest/web compiler captures because production source/schema and source membership changed; CheckStagedTests/VerifyPolicy compiler inputs remain unchanged. New WorkshopObservationProjection.cs and its Godot UID are unstaged for coordinator candidate assembly. Independent repair review, final required full local round, normal hooks and approval remain pending; browser-consumer migration and browser/release qualification remain deferred. No full suite, staging or commit was performed in this repair iteration.

### Review pass 2 — patch closure

All six requested repair roots are implemented, pending independent re-review:
- Workshop creates its frame-dependent world, UI and guidance before awaiting discovery, still before bundle loading. The same external-startup seam proves delayed success, import failure and scene removal; failure/removal retains terminal detail and prevents client startup.
- The first local journal incomplete transition invokes the owner's existing Retire path immediately. Owned event transport cursors are released, first detail retained, and gameplay Save remains Applied.
- The existing TypeScript transport failure boundary retains Error name/message/stack and nested cause (including circular-cause termination). Shipped client lifecycle coverage injects the external isolation failure through the actual retirement path.
- Native managed observation cursors now use the real shared event memory and EventRingConsumer with requested-epoch admission. Two cursors observe actual bumper Impact owners independently, drain independently, reject stale-run events after Reset and observe the next run's events. This assertion fails if Commit does not update the observer epoch.
- Response and all diagnostic deterministic mappings moved into the existing IWorkshopObservationObject boundary. Browser glue calls those same mappings. Native proof covers bodies, capture state/ordinal, actual response phase, exact identities beyond 2^53 and signed clock intervals. External duration output has a narrowly named SetMilliseconds member; the numeric guard inventory follows that exact boundary, with no class exemption or matcher change.
- Capture samples project only their actual epoch/tick/bodies/captures. The TypeScript sample DTO is narrowed accordingly; production projection tests assert the exact four keys, excluding fabricated response metadata.

Matching incremental test/main build: /tmp/observation-review2-build-4.binlog and .log, zero warnings/errors, using the recorded Debug/no-restore/noAutoResponse/PlaytestDiagnostics command. Earlier build-1 (unused moved helpers) and build-2 (test analyzer) failures remain intact; build-3 passed. No worker caller source changed in this patch pass.

Affected native command used the existing no-build/no-restore pattern:
- observation-review2-affected-1.trx: **61/0/0**, filter WorkshopObservationTests | WorkshopIntegrationStartupTests | WorkshopActivationAnimationTests | GameFloatGuardTests.ExternalOutputAdmitsMemberConversionsButRejectsWiderArgumentCalculations. Raw /tmp/observation-review2-affected-1.log.
- observation-review2-startup-success.trx: **1/0/0**, the changed NativePaletteGroupsDeclaredCategoriesAndPreservesTheirDisplayOrder test now also proves delayed discovery success. Raw /tmp/observation-review2-startup-success.log. The other 60 inputs/assertions remain unchanged.

Compiled Node command: node --experimental-vm-modules --test .runtime-build/tools/runtime/workshop-client-lifecycle.test.js. Final **41/0/0**, /tmp/observation-review2-node-2.log; npm run build:runtime passed (/tmp/observation-review2-ts.log). The combined original run retained the unchanged observation harness **15/0/0**, but the new lifecycle test timed out because it awaited unresolved creation instead of the harness readiness step (55 passed, 1 cancelled, 0 skipped); /tmp/observation-review2-node.log remains intact. The corrected test drives readiness and completes with the real retirement detail.

git diff --check HEAD passes. No full verification, staging or commit. Coordinator owns final compiler captures, production numeric guard, independent review and full round. Solution/playtest/web captures need refresh for changed production source; CheckStagedTests/VerifyPolicy inputs remain unchanged. Browser qualification remains deferred.

### Independent repair acceptance — 29 October 2026

All five original Auto layers independently reviewed .tmp/observation-review2-repairs.diff against current source and reused their unchanged full-candidate proof. observation_r1_blind, observation_r1_edge, observation_r1_verification, observation_r1_intent and observation_r1_forward returned scoped Pass with zero remaining findings. The initial partial forward review was completed before acceptance. Reviewers performed no edits, builds, tests or mutants. Coordinator read the full repair delta, verified native TRX totals 61/0/0 and changed-case 1/0/0 plus Node lifecycle 41/0/0, and traced each repair against the finding. Actual browser execution remains deferred.

Follow-up pass findings: 0; patched entries in this follow-up: high 0, medium 0, low 0. No further follow-up review is recommended. Final full local verification, normal hooks and exact committed-SHA approval remain pending; these review verdicts alone are not publication approval.

Coordinator preflight /private/tmp/tim-observation-reviewed.HpzovS/preflight.log retained **42/1/0**: the isolation inventory still named the former browser owner for 14 generic clock/wall contexts. Only those Generic File/Symbol entries now name WorkshopObservationProjection; tokens, counts, meanings and all Known debt are unchanged. The guard reads this fixture directly from the repository, so no rebuild was needed. With CSHARP_STATIC_GATE_BINLOGS=/private/tmp/tim-observation-reviewed.HpzovS, dotnet test CuriousContraptions.tests/CuriousContraptions.tests.csproj -c Debug --no-build --no-restore --filter 'FullyQualifiedName~ElementIsolationTests.ProductionKnowledgeIsExactlyTheOwnedShrinkingList' --nologo --logger 'trx;LogFileName=observation-reviewed-inventory-repair.trx' passed **1/0/0**. Raw output: /tmp/observation-reviewed-inventory-repair.log; original failed observation-reviewed-preflight.trx remains intact. All five compiler captures remain applicable because production inputs are unchanged. Refrozen; no full run, staging or commit.

### Cancelled full attempt and final guard repairs

The coordinator deliberately cancelled the known-failed full attempt in /private/tmp/tim-observation-reviewed.HpzovS (session 67902, exit 130). Its tests.log remains original evidence; this is neither a full Pass nor a complete test total. The two observed failures were source-guard bookkeeping: the new canonical TypeScript command enum's emitted reverse mapping, and the additional reviewed discovery-import catch in Workshop.ReadyAsync.

Only tests changed. The raw-channel guard now scans TypeScript source, recognizes its declared typed enum boundary, and permits only the exact canonical ObserveMemory reverse-mapping statement at workshop-observation.js while scanning surrounding code. Nine permanent controls admit the canonical declarations/typed use and reject wrong paths, enum/value changes, raw channel use and a raw channel beside the allowed declaration. ReadyAsync's exact catch count is now 2; no catch policy was broadened.

Matching incremental test build /tmp/observation-final-guard-repairs.binlog passed with zero warnings/errors (raw /tmp/observation-final-guard-repairs-build.log). With CSHARP_STATIC_GATE_BINLOGS=/private/tmp/tim-observation-reviewed.HpzovS, the recorded dotnet test --no-build --no-restore command filtered CSharpStaticGateTests | TypedTransportCommandTests | ElementIsolationTests | GameFloatGuardTests and passed **177/0/0** in 27 seconds. TRX: CuriousContraptions.tests/TestResults/observation-final-guard-repairs.trx; raw /tmp/observation-final-guard-repairs.log. Production is unchanged and all five compiler captures remain applicable. git diff --check HEAD passes. Refrozen for coordinator verification; no full run, staging or commit.

## Auto Run Result

**Implementation: done. Publication: pending.** One session-owned, typed read-only observation port now exposes admitted command receipts, detached committed state, concrete diagnostics/captures and independent epoch-admitted event cursors. Existing production console/global publishers and raw observation exports are removed. Startup discovery shares the same owner before bundle loading; import failure, scene removal, transport retirement and incomplete diagnostics retain terminal detail and release cursors without changing gameplay authority.

### Files changed

The existing Code Map above is the dependency map; these descriptions record the final implementation rather than the original plan. Related files share a row where they make the same change.

| Files | Final change |
| --- | --- |
| engine/gpu/WorkshopObservation.cs and .uid | Own receipts, committed state, cursor lifetime, typed refusals and sticky retirement. |
| engine/gpu/WorkshopObservationProjection.cs and .uid | Map real responses, diagnostics, construction and truthful trace samples through one testable external-object boundary. |
| engine/gpu/BrowserWorkshopObservation.cs and .uid | Install browser discovery and adapt the shared mappings to JSObject. |
| engine/gpu/BrowserWorkshopClient.cs, BrowserWorkshopClient.Schedule.cs, IWorkshopClient.cs | Publish only admitted observations and bind independent event transport to the existing client lifecycle. |
| engine/gpu/WorkshopDiagnostics.cs | Retain concrete typed diagnostic records and first publication-loss detail in the owned journal. |
| engine/gpu/WorkshopClockMapping.cs, WorkshopClockDerivation.cs, AnimationControlDeferral.cs | Replace clock/deferral console reporting with typed observations while preserving timing behavior. |
| engine/gpu/WorkshopWireFlatBuffers.Observation.cs and .uid, WorkshopWireFlatBuffers.cs, WorkshopWireGenerated.cs, schemas/workshop_wire.fbs | Encode and validate concrete diagnostic/capture payloads on the existing schema and transport. |
| engine/gpu/WorkshopClockWire.cs, WorkshopPresentationWire.cs, WorkshopTraceRecord.cs | Retain meaningful typed clock, presentation and capture data while removing superseded raw observation paths. |
| engine/gpu/WorkshopCommandExecutor.cs, WorkshopRead.cs, WorkshopSimulation.cs, engine/MachineWorld.Gpu.cs | Route faults/transfer observations through the journal and delete console ACK publishing. |
| CuriousContraptions.Simulation/Program.cs, Program.Schedule.cs, WorkshopTrace.cs, wwwroot/worker.ts | Publish typed clock/debt/tick/transfer/capture diagnostics, including completeness, through the existing worker channel. |
| CuriousContraptions.Animation.Worker/Program.cs, wwwroot/worker.ts | Carry animation/clock faults and diagnostics on the same typed observation transport. |
| CuriousContraptions.web/wwwroot/workshop-observation.ts | Expose concrete readonly enum-typed DTOs and deeply frozen discovery results without command capability. |
| CuriousContraptions.web/wwwroot/workshop-client.ts, event-ring-cursor.ts | Carry observations, retain complete failure details and own independent event readers across replacement/Reset/disposal. |
| ui/Workshop.cs, WorkshopGuidance.cs | Initialize frame-dependent state before discovery awaits and retire the same owner on startup failure/removal. |
| ui/WorkshopAnimation.cs, WorkshopBenchPlacement.cs, WorkshopConfiguration.cs, WorkshopConnections.cs, WorkshopGpu.cs, WorkshopPuzzle.cs, WorkshopUiFailure.cs | Migrate diagnostics and remove solved-console bookkeeping while retaining existing UI behavior. |
| CuriousContraptions.tests/WorkshopObservationTests.cs, WorkshopActivationAnimationTests.cs, WorkshopIntegrationHarnessTests.cs | Prove production client observations, causal event ownership/epochs, projections and delayed/failing startup through native boundaries. |
| CuriousContraptions.tests/AnimationControlDeferralTests.cs, BasketballResourceTests.cs, F32SaveBoundaryTests.cs, PalettePlacementTests.cs, WorkshopClockDerivationTests.cs, WorkshopSimulationTests.cs, WorkshopTraceTests.cs, WorkshopUiFailureTests.cs, WorkshopWireTests.cs | Replace console/raw diagnostic expectations with semantic typed behavior and codec/capture coverage. |
| CuriousContraptions.tests/EventRingConsumerTests.cs, WorkerAbiTests.cs | Cover the changed event transport and worker observation boundary. |
| CuriousContraptions.tests/CSharpStaticGateTests.cs, GameFloatGuardTests.cs, TypedTransportCommandTests.cs, WorkshopFormatTests.cs, Fixtures/element-isolation-known.json | Update precise build/adapter/declaration inventories and guard controls; retain numeric arithmetic enforcement and Known element debt. |
| CuriousContraptions.tests/CuriousContraptions.tests.csproj | Compile the new observation tests in the normal native suite. |
| tools/runtime/workshop-observation.test.ts, workshop-client-lifecycle.test.ts, event-ring.test.ts, pose-ring.test.ts, pose-client-harness.ts, worker-contracts.ts | Exercise shipped TypeScript output, frozen DTOs, actual client cursors and typed diagnostic/failure transport. |
| tools/runtime/RuntimeTypeScript.targets, .gitignore | Build/copy the pinned TypeScript observation module and keep generated JS out of source control. |
| tools/LifecycleContract/LifecycleContract.csproj, RuntimeClockControls.cs | Consume the real production diagnostic/clock dependency closure and retain its lifecycle controls. |
| TODO.md, sprint-status.yaml, epic-9A-context.md, epics.md, docs/architecture/element-package-isolation.md, this spec | Record 9A.24 implementation/review/full-test completion and remaining publication/browser gates. |
| _bmad-output/implementation-artifacts/spec-test-behavioral-hygiene.md | Preserve the previously recorded hygiene publication receipt; no new 9A.24 behavior or acceptance depends on rewriting that evidence. |
| New .uid files listed with their C# owners | Register those source resources with Godot; no runtime behavior. |

### Review disposition

All original **13 findings** (high 1, medium 12) are resolved through the recorded re-derivation and repairs; every original row remains in the triage log. Review pass 2's **7 findings** (high 2, medium 5; **6 roots**) are resolved by the recorded patches, including both reports of the startup defect. **Deferred findings: 0. Rejected findings: 0. Unresolved findings: 0.** Existing post-catalogue browser scope is an enforcing-gate deferral, not a newly deferred review finding.

Blind Hunter, Edge Case Hunter, Verification Gap, Intent Alignment and Forward-Only all returned Pass on the affected repair re-review. Subsequent exact guard repairs also passed independent review. The final follow-up patched-entry counts are **high 0, medium 0, low 0**; **followup_review_recommended: false**. All five re-reviews cover the affected changes; no unverified implementation risk remains from their findings.

### Final verification

Coordinator session **41005** completed with **exit 0**. The independently parsed five final TRXs report **5,047 passed, 0 failed, 0 skipped**: native **4,555/0/0**, tools **17 + 206 + 77 + 192 = 492/0/0**. Native duration: **21m05s**. The final native result includes all 35 observation/startup rows, including causal impact, projections, failures, delayed startup, growth and refusals.

Exact final invocation (only the existing explicit browser deferral filter):
```sh
export CSHARP_STATIC_GATE_BINLOGS=/private/tmp/tim-observation-reviewed.HpzovS
observation_filter=$(dotnet run --project tools/E2eRunner --no-build -c Debug -- deferred-filter)
dotnet test CuriousContraptions.slnx -c Debug -f net10.0 --no-build --no-restore --nologo -noAutoResponse --filter "$observation_filter" --logger trx --results-directory /private/tmp/tim-observation-reviewed.HpzovS/results-final -- RunConfiguration.TreatNoTestsAsError=true > /private/tmp/tim-observation-reviewed.HpzovS/tests-final.log 2>&1
```

Environment: SDK 10.0.403; workload 10.0.401; wasm-tools 10.0.112/10.0.100; Node 26.12.1. Independent verification confirmed all five TRXs and the affected repair rows passed.

- Full verification log: /private/tmp/tim-observation-reviewed.HpzovS/tests-final.log.
- All five original final TRXs: /private/tmp/tim-observation-reviewed.HpzovS/results-final/ (filenames beginning _aidan-runlevelfive_2026-10-29_15_55_46_net10.0, including [1], [2], [3], plus _aidan-runlevelfive_2026-10-29_15_55_47_net10.0.trx).
- Five matching production compiler captures in /private/tmp/tim-observation-reviewed.HpzovS passed; production GameFloatGuard passed. The test-only repair closure passed **177/0/0** in observation-final-guard-repairs.trx with those captures.
- Compiled Node lifecycle **41/0/0** and observation **15/0/0**, with original commands/logs recorded above. Matching incremental builds had zero warnings/errors.
- Scoped deletion/consistency checks and git diff --check HEAD passed. Earlier failed builds, failed TRXs and the deliberately cancelled exit-130 full attempt remain preserved and are not counted as passing verification.

### Remaining gates and risks

Normal hooks, exact committed-SHA approval, commit/push and remote-main verification are **pending**, owned by the coordinator. This done status records implementation completion, not publication. No staging or commit was performed by the implementer during finalization.

Browser-consumer migration and actual browser qualification remain pending at the named post-catalogue gate; native/Node proof does not claim Chrome qualification. CI production build, artifact and deployment qualification remain **unverified**; the owner's nonblocking-CI policy applies. Detailed performance/resource qualification remains at its existing named later gates. No new fallback, compatibility path, schema platform, solver change or element debt was introduced.
