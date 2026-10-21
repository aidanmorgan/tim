---
title: 'Story 9A.3: Transport Controls as Typed Commands and Queries'
type: 'refactor'
created: '2026-10-18'
status: 'review'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/AGENTS.md'
  - '{project-root}/_bmad-output/planning-artifacts/epics.md#story-9a3-transport-controls-as-typed-commands-and-queries'
  - '{project-root}/_bmad-output/planning-artifacts/architecture-conformance-audit-2026-10-18.md'
  - '{project-root}/_bmad-output/implementation-artifacts/spec-9a-2-single-flatbuffers-codec.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Two transport controls bypass the router. Line numbers are from the main tree with 9.4b uncommitted; the symbol is authoritative.

- **C2: the stall flag.**
  - `workshop-client.js` `updateReliableStall` (`:79-89`, called at `:373` and `:578`) measures the age of pending commands and untaken acknowledgements.
  - It posts the raw `{reliableStalled}` flag.
  - `worker.js:348` calls `Program.SetReliableStall` (`Program.cs:379`), which sets `_reliableStalled` (`:24`). That flag feeds `RunLoopHost.DebtExceeded` (`:307`) and `StopForDebt` (`:311-318`, `ReliableStalled`).
  - Nothing sequences the flag and nothing receipts it.
  - 9.4b found that C2 is not the cat-023b cause (spec-9-4b §1, AC5), so all of C2 remains here.
- **C3: memory observation on its own channel.**
  - The raw `memoryRequest`/`memoryResult` channel runs through:
    - `worker.js:354-367`;
    - `Program.ObserveMemory` (`:138-152`);
    - `BrowserWorkshopClient.cs:66-68`, `:74-76`, `:114` and `:322-348`;
    - `workshop-client.js:312-313` and `:480-548`;
    - the 64/128-byte `WorkshopMemoryWire`.
  - Its only entry point, `observeMemory` (`:492`), has **zero callers** in the repository.
- **Silent transport paths found during the census:**
  - `worker.js:224`: `try { host.FlushObservation(); } catch {}`;
  - `acknowledgeRead` (`:227-232`) silently drops a mismatched receipt.

**Approach:**
- The Simulation worker detects the stall in its own C# reliable-read accounting. This is the AC's second option.
- Memory becomes a typed query on the FlatBuffers command/response transport.
- Any untyped envelope is rejected, and the rejection names its keys.

## Boundaries & Constraints

**Always:**
- **Stall definition after this story:** the oldest published, unreceipted reliable read is older than 500 ms on the worker's master clock. This is the same bound the client used. When that holds, the run stops with `ReliableStalled` through the existing `StopForDebt`.
- **Receipts:**
  - Read receipts are matched in C# (`host.AcknowledgeRead`), never in JS.
  - A mismatched receipt is a typed rejection, never a silent return. Mismatched means a wrong session, or a publication that was never published or is older than the last retired one.
- **Receipts are cumulative.**
  - `worker.js` coalesces reads: a newer read overwrites `latestRead` while one is in flight, and an occurrence clears it (`worker.js` `publish`). Some publications are therefore never posted, and never receipted.
  - So a receipt for publication P retires every ledger entry with a sequence ≤ P. Publication sequences only increase (`Program.EmitRead`).
  - An exact-match ledger would false-stall every ordinary run, because publication is 120 Hz and receipts arrive at display rate.
- **Client UI state:** the client keeps computing Backpressure and TimedOut for the player from its pending ages, but it posts nothing.

**Never:**
- A `TransportStalled` command. It would need a capacity bypass: `send` refuses at 2 outstanding (`workshop-client.js:555`), and `Execute` returns `Busy` while a command is pending (`Program.cs:182-184`). A stall is exactly that state.
- PLAYTEST gating of the memory query (owner decision h: one production build).
- Old-name allow-lists.

## I/O & Edge-Case Matrix

| Scenario | Input | Expected |
|---|---|---|
| Main thread stops receipting reads | no `readAcknowledged` for > 500 ms while Running | `StopForDebt`: Faulted/`ReliableStalled` read; player sees the stall fault |
| Receipts flowing, slow main thread | each receipt < 500 ms | no stall (control) |
| Coalesced reads | P1, P2 and P3 published; only P3 posted and receipted | P1 to P3 retired; no stall (control) |
| Exactly 500 ms | oldest age = 500 ms | no stall; stall only when the age is > 500 ms (boundary) |
| Memory query while stopped and idle | `ObserveMemory` | Acknowledgement with `MemoryObservation` (Simulation context) plus the Browser capture; `Accepted` |
| Memory query while Running | `ObserveMemory` | Rejected `WrongPhase`; no capture |
| Old envelope | `{reliableStalled:true}` or `{memoryRequest:…}` | worker posts `failure` "Unsupported worker envelope: reliableStalled"; the client logs `CCGPU_TRANSPORT_FAILURE` with that detail |

</frozen-after-approval>

## Code Map

- `CuriousContraptions.Simulation/Program.cs`:
  - delete `_reliableStalled`, `SetReliableStall` and `ObserveMemory`;
  - add `ReliableReadLedger` (new file `CuriousContraptions.Simulation/ReliableReadLedger.cs`), which records each published reliable read (session, publication sequence and master-clock time) and retires cumulatively on receipt;
  - `[JSExport] AcknowledgeRead(byte[] receipt)` returns accepted or throws a typed rejection;
  - `DebtExceeded` reads `ledger.OldestAge(MasterNow()) > 500 ms`.
- `engine/gpu/WorkshopSimulation.cs:9`: `WorkshopRejection.ReliableStalled` stays.
- `engine/gpu/WorkshopWire.cs` / `WorkshopMessages.cs` (after 9A.2): add `WorkshopCommandKind.ObserveMemory`, then `schemas/workshop_wire.fbs` `CommandKind.ObserveMemory` and the response field `memory: MemoryObservationRecord`.
- `engine/gpu/WorkshopMemoryWire.cs`: keep the records and `Capture`; delete the fixed 64/128-byte encode and decode.
- `engine/gpu/BrowserWorkshopClient.cs`: a public `ObserveMemory()` is built on `Execute`. Delete `BeginMemory`, `ReceiveMemory`, `ReleaseMemory`, `SendMemoryRequest`, `CompleteMemoryObservation` and the three `create` callbacks.
- `worker.js`:
  - delete the `reliableStalled` and `memoryRequest` branches;
  - forward the receipt to `host.AcknowledgeRead`;
  - an envelope with an unknown key posts a named failure;
  - remove the swallow at `:224`.
- `workshop-client.js`: delete the memory machinery (`:480-548`, `:312-313`) and the `postMessage` in `updateReliableStall`.
- Tests:
  - `tools/workshop-observation.test.mjs:98,164-165,201-204`;
  - `tools/workshop-client-lifecycle.test.mjs:280` (`stallStates`);
  - `CuriousContraptions.tests/WorkshopWireTests.cs:1297-1337` (the memory wire tests).

## Tasks & Acceptance

**Execution.** Depends on 9A.2. [R:9.4b] means that file is rewritten by the clock master; [R:9R.0] means `exports.CuriousContraptions.Simulation.Program`.

1. [ ] **Tests first (red):**
   - `CuriousContraptions.tests/ReliableReadLedgerTests.cs` (new; add a `Compile Include` and link `../CuriousContraptions.Simulation/ReliableReadLedger.cs`). It covers:
     - age > 500 stalls, and = 500 does not;
     - a receipt clears;
     - the coalesced case: P1, P2 and P3 with only P3 receipted gives no stall;
     - a mismatched receipt (wrong session, or a publication never published or below the last retired) is rejected;
     - Reset clears.
     
     In `workshop-observation.test.mjs`, add a slow-receipt case: reads coalesce for 400 ms, and then one receipt arrives with no stall.
   - `ObserveMemoryQueryTests` (in the same file or `WorkshopWireTests`): the query round-trips through the router with a sequence and a receipt; Running gives `WrongPhase`.
   - `tools/workshop-observation.test.mjs`: (a) the host clock advances 501 ms with no receipt, so the stop read carries `ReliableStalled`; (b) a typed `ObserveMemory` gives an acknowledgement with memory; (c) `{reliableStalled:true}` and `{memoryRequest}` each give a named failure.
2. [ ] `CuriousContraptions.Simulation/ReliableReadLedger.cs` (new) and `Program.cs`. [R:9.4b] [R:9R.0]
3. [ ] Schema, `WorkshopWireFlatBuffers.cs` and `WorkshopMemoryWire.cs`: `ObserveMemory` and `MemoryObservationRecord`. Regenerate `WorkshopWireGenerated.cs` and `tools/e2e/workshop-wire.ts`.
4. [ ] `engine/gpu/BrowserWorkshopClient.cs`: `ObserveMemory()` over `Execute`, and delete the callback chain.
5. [ ] `CuriousContraptions.Simulation/wwwroot/worker.js` and `CuriousContraptions.web/wwwroot/workshop-client.js`. [R:9.4b]
6. [ ] `tools/workshop-client-lifecycle.test.mjs`: assert that no `reliableStalled` message is ever posted, and keep the Backpressure/TimedOut state assertions. Migrate the `WorkshopWireTests` memory facts to the typed record.

**Acceptance Criteria (1:1 with epics 9A.3):**
- **AC1.** Given a reliable-read stall, when it is detected in the worker's own reliable-read accounting, then the run stops with `ReliableStalled` and the player sees the existing stall state.
- **AC2.** Memory observation is a typed query with a receipt on the command/query transport.
- **AC3.** An untyped message on an old channel name is rejected, and the rejection is visible in the worker fault log (`CCGPU_TRANSPORT_FAILURE … Unsupported worker envelope: <key>`). This is the negative control.
- **AC4.** `tools/workshop-observation.test.mjs` exercises the typed stall stop, the memory query and the rejection.
- **AC5.** `cat-023b` passes in three consecutive serial runs.
- **AC6.** `dotnet build CuriousContraptions.slnx` reports 0 warnings, `dotnet test` passes 100%, and every `tools/e2e/*.test.ts` suite passes 100% in one serial Chrome run against the production preview.

## Design Notes

**Decisions (owner playability delegation, 18 Oct 2026):**
- **D1. Worker-side detection rather than a typed command.**
  - Why: the command path is saturated exactly when a stall happens. The worker already owns read publication, and read receipts come back on the main thread synchronously after C# validation (`workshop-client.js:316-318`), so a stalled main thread stops receipts.
  - Consequence: the old "untaken acknowledgement" criterion folds into this one. An acknowledgement the main thread fails to take means the main thread is stalled, and so are its read receipts.
  - Rationale: a stalled connection stops the run predictably, with one source of truth.
- **D2. Keep memory observation, as a typed query.**
  - It has no consumer today, but REQ-10 and P0-034/PERF-46 need post-GC observations at the release gate (`docs/delivery-workflow.md:44`).
  - The AC says "is a typed query", not "delete".
  - Its in-repo consumers become the native and Node tests; 9A.24's observation port exposes it to Chrome.
- **D3. Generic unknown-envelope rejection naming the keys,** with no list of old names. Rationale: no legacy knowledge survives in the shell.

**Mutation plan:**
- Make the ledger's age check `>=`: the 500 ms boundary test fails.
- Make retirement exact-match: the coalesced-read tests fail.
- Restore the `acknowledgeRead` silent `return`: the mismatched-receipt test fails.
- Re-add the `reliableStalled` branch: the rejection harness fails.
- Remove the `WrongPhase` guard: the Running memory test fails.

**Impacted suites:**
- Node `workshop-observation`, `workshop-client-lifecycle` and `workshop-pose-ring`;
- native `WorkshopWireTests`, `WorkshopSimulationTests` and `WorkshopTickLoopTests` ([R:9.4b] new);
- Chrome: all suites, with cat-023b ×3.

**Risks:**
- cat-023b depends on 9.4b's clock-master fix. If 9.4b's AC4 (five runs) is still unmet, this story's AC5 is blocked by 9.4b, not by this change.
- A slow but healthy main thread (> 500 ms GC) now faults the run. That is the same bound as today, but it is now enforced worker-side.

## Verification

- `rg -n "reliableStalled|SetReliableStall|memoryRequest|memoryResult|sendMemoryRequest|completeMemoryObservation|catch \{\}" CuriousContraptions.*/wwwroot engine CuriousContraptions.Simulation`: expected 0.
- `node --test tools/workshop-observation.test.mjs tools/workshop-client-lifecycle.test.mjs`: expected all pass.
- `dotnet test CuriousContraptions.slnx --filter "ReliableReadLedgerTests|ObserveMemoryQueryTests"`: expected all pass.

## Legacy Deleted

- **Stall flag path:** `Program._reliableStalled` and `Program.SetReliableStall`; `worker.js` `reliableStalled` branch; `workshop-client.js` `postMessage({ reliableStalled })`.
- **Raw memory channel:**
  - `Program.ObserveMemory`; `worker.js` `memoryRequest` branch;
  - `workshop-client.js` `observeMemory`, `settleMemory`, `sendMemoryRequest`, `receiveMemoryResult` and `completeMemoryObservation`, and the `memoryResult` branch;
  - `BrowserWorkshopClient` `BeginMemory`, `ReceiveMemory`, `ReleaseMemory`, `SendMemoryRequest`, `CompleteMemoryObservation` and the three `create` callback parameters;
  - the fixed 64/128-byte `WorkshopMemoryWire` encode and decode.
- **Swallowing paths:** `worker.js` `catch {}` around `FlushObservation`, and the silent `return` in `acknowledgeRead`.

## Implementation Notes

Amelia-24, 21 Oct 2026, on main `379cc24a`. Story 9A.17 (in review) is not merged: it touches ports and footprints, not the
schema, the codec or the transport, so this story does not depend on it.

**What landed (code map as built):**
- `engine/gpu/WorkshopReliableReadLedger.cs` (new, compiled natively so it is unit-tested): the worker's reliable-read accounting,
  `ReadReceiptRejection` (Malformed, WrongSession, Unpublished, Retired) and `ReadReceiptRejectedException`. `Program.EmitRead`
  publishes into it; `Program.AcknowledgeRead` (JSExport) matches every receipt; `RunLoopHost.DebtExceeded` and `StopForDebt` read
  `Stalled` and `DebtReason` on the master clock.
- `engine/gpu/WorkshopMemoryObservation.cs` (renamed from `WorkshopMemoryWire.cs`; it keeps `WorkshopCaptureMode` and
  `WorkshopBuild`): `WorkshopMemoryMeasurement`, `WorkshopMemoryObservation`, `WorkshopMemoryReply` and `WorkshopMemoryQuery`
  (`Admit`, `ValidateCarriage`, `Complete`).
- `WorkshopCommandKind.ObserveMemory` (12), schema `CommandKind.ObserveMemory`, `struct MemoryObservationRecord` and
  `WorkshopResponse.memory`; `WorkshopResponse.Memory`. Generated code from flatc 25.2.10, byte-identical to its output (C# whole
  file; TS generated part).
- `WireFaultReason.UnknownCommand`: `DecodeCommand` refuses an undefined kind before any other check of its shape.
- `engine/gpu/WorkerEnvelopeRejection.cs` (new): `WorkerEnvelopeRejection` (UnknownEnvelope, CommandTransportSaturated) and
  `WorkerEnvelopeRejectedException`; `Program.RejectUnknownEnvelope` and `RejectSaturatedCommand` record it in the worker fault log
  (`CCGPU_WORKER_ENVELOPE_EXCEPTION`, new `FaultSite.WorkerEnvelope`) and return the detail the worker posts.
- `BrowserWorkshopClient.QueryMemory()` over `Execute`; every acknowledgement passes `WorkshopMemoryQuery.ValidateCarriage`.

**Decisions (owner delegation: decide small gaps, record each):**
- **I1. A refused envelope is the existing `rejected` envelope,** not `failure`. The browser treats both alike
  (`CCGPU_TRANSPORT_FAILURE` with the detail), so the matrix's observable outcome holds; the typed reason is managed.
- **I2. The ledger is never cleared, Reset included** (Task 1 said "Reset clears"). Publication sequences never reset and the
  worker's read queues survive Reset, so a receipt in flight across a Reset must still retire its read; clearing would turn it into a
  `Retired` rejection and retire the session. A run started after a genuine stall stops again at once, which is correct.
- **I3. The memory query is answered in `Execute`,** after the stale-generation and Busy guards and without taking
  `_pendingCommand`, so it never sets a schedule transition. `Busy` covers a pending GPU operation or any unreceipted read (this
  replaces the old script-computed `transportIdle`). The old `Stale` status is the command path's `StaleGeneration`.
- **I4. The `Unsupported` memory status is deleted with no replacement rejection.** The GC APIs it guarded run on browser-wasm; a
  failure would be a fault, never a fallback result.
- **I5. The worker's per-request 1000 ms lateness checks (`CCGPU_MEMORY_LATE`) are deleted with the channel.** The query is bounded
  like every command by the browser's TimedOut state (2 s).
- **I6. The dead `candidateTimedOut` envelope is deleted.** Nothing posts it; the browser now refuses it as an unknown envelope.
- **I7. `_memoryRequested` (State Latch feedback controls, unrelated to this channel) is renamed `_memoryControls`,** so the AC's
  `rg memoryRequest` is literally empty.
- **I8. The typed command and query definitions for the AC's `rg`** are the `WorkshopCommandKind` enum (`engine/gpu/WorkshopMessages.cs`),
  the schema and the generated schema code. Everywhere else the query is named only as `WorkshopCommandKind.ObserveMemory` (or the
  generated `CommandKind.ObserveMemory`). `TypedTransportCommandTests.TheRawStallFlagAndMemoryChannelAreGoneFromProduction` enforces
  exactly that, and it is the killer of every re-add mutant.
- **I9. The `FlushObservation` swallow is contained and logged** (`CCGPU_TRACE_FLUSH_EXCEPTION`), never silent; the trace is
  diagnostic, so its failure does not retire the session.
- **I10. `CCGPU_DEBT_FAULT` now reads `reason=<WorkshopRejection> oldestUnreceipted=<publication> ageMs=<age>`,** so a stall fault
  names the read that stalled.
- **I11. The Node harness cannot encode FlatBuffers** (the `flatbuffers` module exists only for the e2e suites), and the worker parses
  no buffer, so the typed query's encoding, routing and acknowledgement are proven natively; the harness proves the worker carries it
  on the command transport, forwards every receipt to the ledger and refuses each retired envelope by name.
- **I12. `QueryMemory` has no production caller,** as before (`observeMemory` had none). Story 9A.24's observation port exposes it.

## Spec Change Log

- 21 Oct 2026 (Amelia-24): Task 1's "Reset clears" is replaced by I2 (the ledger is never cleared). The matrix row "Old envelope:
  worker posts `failure`" is met through I1 (`rejected`, same browser outcome).

## Review Triage Log

## Appendix: reference census (main tree, 18 Oct 2026)

- **`reliableStalled` / `SetReliableStall` / `_reliableStalled`:**
  - `Program.cs:24,239,307,313,314,379`;
  - `worker.js:348`;
  - `workshop-client.js:79-89,373,578`;
  - `workshop-observation.test.mjs:98,164,165,201,204`;
  - `workshop-client-lifecycle.test.mjs:280`;
  - `tools/e2e/workshop-driver.ts:77` (the `ReliableStalled: 13` enum mirror; it stays).
- **Memory channel:**
  - `WorkshopMemoryWire.cs` (whole);
  - `Program.cs:19,138-152`;
  - `BrowserWorkshopClient.cs:66-68,74-76,114,322-348`;
  - `worker.js:354-367`;
  - `workshop-client.js:143,175,312-313,480-548,555`;
  - `WorkshopWireTests.cs:1297,1314,1337`;
  - `docs/work-orders/P0-004/ownership/part-023.json` (historical).
  - `rg observeMemory` across the whole tree (hidden files included) finds only its declaration.
- **Not referenced** from csproj, `export_presets.cfg`, `.tres`/`.tscn` or e2e suites.
