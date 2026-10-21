---
title: 'Story 9A.2: Single FlatBuffers Codec'
type: 'refactor'
created: '2026-10-18'
status: 'in-review'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/AGENTS.md'
  - '{project-root}/_bmad-output/planning-artifacts/epics.md#story-9a2-single-flatbuffers-codec'
  - '{project-root}/_bmad-output/planning-artifacts/architecture-conformance-audit-2026-10-18.md'
  - '{project-root}/_bmad-output/implementation-artifacts/spec-9-4b-cumulative-regression-repair.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** The legacy binary codec is not a fallback. It is the live read path.
- `Program.ReadOutput` is `new byte[WorkshopWire.ResponseBytes]` (`CuriousContraptions.Simulation/Program.cs:21`). Because of that, every `EmitRead` (`:357-376`) goes through `WorkshopWire.Write` (`EncodeResponse`, `:270-281`). Only command acknowledgements are FlatBuffers.
- Three readers decode those bytes:
  - `BrowserWorkshopClient.ReceiveRead` tries FlatBuffers, throws, and falls back to `WorkshopWire.DecodeResponse` (`engine/gpu/BrowserWorkshopClient.cs:263-277`).
  - `worker.js` `writePoseRing` parses the legacy layout: offset 72, byte 104, and `READ_BODIES_OFFSET` (`CuriousContraptions.Simulation/wwwroot/worker.js:40-82`).
  - Both `extractReadIdentity` copies take their `responseAbi` tail (`worker.js:174-211`, `CuriousContraptions.web/wwwroot/workshop-client.js:35-77`), and so does `sequence()` (`:12-33`).
- **The FlatBuffers response cannot replace it as it stands.** `WorkshopResponse` has no motion field. The decoder fabricates zero motion only at tick 0 (`engine/gpu/WorkshopWireFlatBuffers.cs:588-590`). Yet `WorkshopPoseHistory` needs `Motion` (`:293`).
- **Every Construct carries two encodings.** The command holds a FlatBuffers record and a legacy construction blob (`WorkshopWireFlatBuffers.cs:43-44`, schema `construction: [uint8]` at `schemas/workshop_wire.fbs:314`), and the decoder falls back to the blob (`:146-149`).
- **No decoder verifies its buffer.** `GetRootAs` is called with no `Verifier` (`:111`, `:425`; `WorkshopSaveCodec.cs:39`), so a truncated buffer is reinterpreted rather than rejected.

**Approach:** Make FlatBuffers the only codec.
- Add motion to the response, and give the Counter target a typed integer field.
- Give every root a `file_identifier`, size-prefix every buffer and verify it.
- Move reads, schedule endpoints, the trace and saves onto the schema.
- C# hands JS the read identity and the pose-slot bytes, so JS never parses a buffer.
- Delete `WorkshopWire` and every fallback.

## Boundaries & Constraints

**Always:**
- One decode attempt per buffer. A failure raises a typed `WireFault` (implemented as `WireFaultException`, the analyzers' CA1710 name) carrying a `WireFaultReason` enum: `Truncated`, `UnknownIdentifier`, `TrailingBytes`, `VerifierRejected`, `MissingField` or `OutOfRange`. *(Version items dropped: owner, 21 Oct 2026; Stories 9R.7 and 9A.1 removed every format version.)*
- Lanes this story rewrites are f32 or integer, never `Half`.
- No format versions (owner, 21 Oct 2026). A save that fails structural pre-validation is a typed `WorkshopSaveRejectedException` carrying a closed `WorkshopSaveRejection` reason; the player sees the refusal and the bench is kept.

**Never:**
- A second codec, a `catch`-and-retry, or `GetRootAs` without a verify.
- JS that parses FlatBuffers vtables.
- A migration of old saves.
- Touching the per-id decode arms or `WorkshopCanonicalColliders.For` in `DecodeConstructionRecord`; Stories 9A.16 and 9A.27–9A.43 own those.

## I/O & Edge-Case Matrix

| Scenario | Input | Expected |
|---|---|---|
| Truncated command, response or save | last byte cut | `WireFault(Truncated)`; no state change |
| Wrong root identifier | a save handed to `DecodeCommand` | `WireFault(UnknownIdentifier)` |
| Trailing bytes | valid buffer plus 1 byte | `WireFault(TrailingBytes)` |
| Save that fails pre-validation | any structural violation (a pre-9A.2 save among them) | the UI shows "This save cannot be loaded: <reason>"; the bench is unchanged |
| Counter target missing, 0 or 10 | `target_count` absent or out of range | `WireFault(MissingField)` or `(OutOfRange)` |
| Read at tick > 0 | a committed read | motion round-trips bit-exactly |

</frozen-after-approval>

## Code Map

- `schemas/workshop_wire.fbs`. Add: a `file_identifier` per root (command `CCWC`, response `CCWR`, save `CCWS`, schedule endpoint via the response); `motion: MotionRead`; `InstanceRecord.target_count: uint32 = null`. Delete: `WorkshopCommand.construction`.
- `engine/gpu/WorkshopWireGenerated.cs` (regenerated), `engine/gpu/WorkshopWireFlatBuffers.cs`, `engine/gpu/WorkshopSaveCodec.cs`.
- `engine/gpu/WorkshopWire.cs`. Deleted. Its records and validators move: `WorkshopCommand`, `WorkshopResponse` and `WorkshopCommandIdentity` (`:10-13`), `Matches`, `ValidateCapture`, `ValidateScheduleIdentity` and `ValidatePhysicalRead` move to `engine/gpu/WorkshopMessages.cs`. `WriteSession` and `ReadSession` become `RuntimeSessionId.WriteTo` and `RuntimeSessionId.Read`.
- Producers: `CuriousContraptions.Simulation/Program.cs` (`ReadOutput`, `EncodeResponse`, `EmitRead`, `ResponseAbi`, `Bootstrap`); `CuriousContraptions.Simulation/Program.Schedule.cs:41,65-78`; `CuriousContraptions.Simulation/WorkshopTrace.cs:41`.
- Consumers:
  - `engine/gpu/WorkshopCommandRouter.cs:15-25,42`;
  - `engine/gpu/BrowserWorkshopClient.cs:105,113,263-277`;
  - `engine/gpu/BrowserWorkshopClient.Schedule.cs:58`;
  - `CuriousContraptions.Animation.Worker/Program.cs:100,311`;
  - `engine/gpu/WorkshopSchedule.cs:101,122,142`;
  - `engine/gpu/WorkshopPoseHistory.cs:24`;
  - `engine/gpu/WorkshopClockWire.cs:25,41,142`;
  - `engine/gpu/WorkshopMemoryWire.cs:52,71`;
  - `engine/gpu/WorkshopHint.cs:30,142`.
- JS: `worker.js` (`writePoseRing`, `extractReadIdentity`, `flushRead`, `acknowledgeRead`, `READ_BODIES_OFFSET`/`BODY_WIRE_*`, `responseAbi`); `workshop-client.js` (`sequence`, `extractReadIdentity`, `create(..., responseAbi, ...)`, the `read` branch at `:314-318`).
- `tools/e2e/workshop-wire.ts`. Regenerate it, and move the hand-written helpers at `:2128-2478` into a new `tools/e2e/workshop-wire-helpers.ts`. That covers `decodeSavedConstruction` (version 13 at `:2143`), `authoredState` and the mock encoders.
- `ui/Workshop.cs:809-840` (`LoadSave` message), `tools/Coverage/CapabilityImplementationRequirements.cs:13` (path string `engine/gpu/WorkshopWire.cs`).

## Tasks & Acceptance

**Execution** ([R:9.4b] = re-read after 9.4b lands; [R:9R.0] = after 9R.0 lands, because namespaced `Program` changes `exports.CuriousContraptions.Simulation.Program` and 9R.0 retyped the router and client catches):
1. [ ] **Tests first (red).** Create `CuriousContraptions.tests/WireFaultTests.cs` and add its `Compile Include`. It covers command, response, save and schedule-endpoint fixtures, each with truncated, wrong-identifier, trailing-byte, corrupt-root and counter-target cases (version cases dropped: owner, 21 Oct 2026); motion round trip at tick 0 and at tick > 0; and `RejectedInputIsNeverRetried` (a spy counts decode attempts and expects 1).
2. [ ] `schemas/workshop_wire.fbs`, then `engine/gpu/WorkshopWireGenerated.cs` and `tools/e2e/workshop-wire.ts`. Regenerate with flatc 25.2.10 to match the vendored runtime ([R:9.4b] `CuriousContraptions.FlatBuffers/`). Record the exact command in `schemas/README.md`.
3. [ ] `engine/gpu/WorkshopWireFlatBuffers.cs`. Change all of these:
   - `FinishSizePrefixed` with the identifier;
   - the decoders call `*Verify` and check `size prefix + 4 == length`;
   - encode and decode motion;
   - `target_count`: delete `WholeCount` ([R:9.4b] `:786`) and the `(float)counter.TargetCount` threshold (`:737`);
   - delete the legacy blob path (`:43-44`, `:146-149`) and the `is { Length: 16 } ? … : default` session defaults (`:129`, `:450`), which become `MissingField`.
4. [ ] `engine/gpu/WorkshopMessages.cs` (new) and `engine/gpu/RuntimeSessionId` (its existing declaration file). Move the records and validators, then retarget the clock, memory, hint and schedule callers. [R:9R.0]
5. [ ] `CuriousContraptions.Simulation/Program.cs`, `Program.Schedule.cs` and `WorkshopTrace.cs`. Every response becomes FlatBuffers. Delete `ReadOutput`, `ResponseAbi` and the `output.Length` branch.
   - Change `Publish` to `Publish(byte[] read, byte[] readIdentity24, byte[] poseSlot, …)`. The pose slot is composed by the existing `WorkshopPoseRing` (`ComposeSlot`), as wide as the read's bodies (Story 9R.5b: no 1040-byte fixed slot). No trace version. [R:9.4b] [R:9R.0]
6. [ ] `engine/gpu/WorkshopSchedule.cs`. A Prepare control carries a length-prefixed FlatBuffers endpoint. Delete `PrepareBytes = 80 + ResponseBytes`. `WorkshopPoseHistory.ReservedHistoryBytes` gets its own declared bound.
7. [ ] `engine/gpu/WorkshopCommandRouter.cs`, `BrowserWorkshopClient.cs`, `BrowserWorkshopClient.Schedule.cs` and `CuriousContraptions.Animation.Worker/Program.cs`. Decode once and drop the `isFlatBuffer` reserve. `send(id, bytes, sequence)` passes the sequence explicitly. [R:9R.0]
8. [ ] `worker.js` and `workshop-client.js`. Delete both `extractReadIdentity` copies, `sequence()`'s vtable parse, the `responseAbi` plumbing and the three `READ_BODIES_OFFSET`/`BODY_WIRE_*` constants. `writePoseRing` copies `poseSlot` verbatim. [R:9.4b] (both files are rewritten by the clock master)
9. [ ] `engine/gpu/WorkshopSaveCodec.cs` and `ui/Workshop.cs`. Structural pre-validation with the typed `WorkshopSaveRejectedException`; delete the catch-alls (`IsInvalidStoredValue`, `IsMalformedRecord`) and the save magic (the `CCWS` identifier replaces it). No version.
10. [ ] Before deleting, run `rg -n "^public |^internal " engine/gpu/WorkshopWire.cs` and move every top-level type to `WorkshopMessages.cs` (at least `WorkshopWireVersion` at `:8`, the records at `:10-13`, and any kind enums declared there). Then delete `engine/gpu/WorkshopWire.cs` and its `.uid`.
    - Migrate `CuriousContraptions.tests/WorkshopWireTests.cs` (43 refs), `WorkshopReadTests.cs` (20), `WorkshopSaveTests.cs`, `WorkshopTraceTests.cs` and the minor callers (Appendix).
    - In `WorkerAbiTests.cs`, delete the three `Owned` rows but keep 9A.1's `WorkerDeclaresNoUnownedLayoutConstant` with an empty `Owned` set, so that `worker.js` may declare no layout constant. Update `tools/Coverage/CapabilityImplementationRequirements.cs:13` to `WorkshopWireFlatBuffers.cs`.
11. [ ] Fix `tools/e2e/workshop-driver.ts:66,118`: the stale "WorkshopWire.Write" layout comment.

**Acceptance Criteria (1:1 with epics 9A.2):**
- **AC1.** Given the remaining `WorkshopWire` callers, when this story lands, then each of them encodes and decodes through `WorkshopWireFlatBuffers` and the schemas only. They include `Program.Schedule.cs`, `BrowserWorkshopClient.Schedule.cs`, the Animation `Program.cs`, `WorkshopTrace.cs`, `WorkshopSaveCodec.cs`, the read publication, the pose slot and the read identity.
- **AC2.** Given `tools/e2e/workshop-wire.ts`, when the suites run, then it decodes the FlatBuffers format only.
- **AC3.** Given a truncated, unknown-tag or trailing-byte command, response or save, when it is decoded, then a `WireFault` names the reason and nothing retries it through another codec (unit negative controls).
- **AC4.** Given a save that fails structural pre-validation, when the player loads it, then a typed `WorkshopSaveRejectedException` names the reason, a visible message shows and the current bench is kept (no migration, no version check).
- **AC5.** `rg "WorkshopWire\."` returns 0 hits outside the FlatBuffers codec.
- **AC6.** `dotnet build CuriousContraptions.slnx` reports 0 warnings, `dotnet test` passes 100%, and every `tools/e2e/*.test.ts` suite passes 100% in one serial Chrome run against the production preview, including every Save/Load suite.

## Design Notes

**Decisions (owner playability delegation, 18 Oct 2026):**
- **D1. Identifier, size prefix and verifier together.** Only all three detect all three faults: the identifier catches a wrong root, the verifier catches truncation, and the size prefix catches trailing bytes. Rationale: a corrupt machine must never load as a different machine.
- **D2. C# composes the pose slot.** JS keeps only a verbatim copy. Rationale: a thin worker, and pose layout owned by C#.
- **D3. The old-save message names the cause, not a code.** Rationale: the player knows to rebuild.
- **D4. Fixed-ABI wires that are not `WorkshopWire` stay out of scope** and keep their own validation: the clock ring, the schedule control header, the 144-byte animation output and presentation observations. Rationale: SAB Atomics records need fixed slots. Story 9A.3 moves memory onto the command transport.

**Test-first and negative controls:** Task 1 is red before Task 3. Controls: a valid buffer with one flipped vtable byte gives `VerifierRejected`; a valid v14 save loads (positive).

**Mutation plan:**
- Remove the verify call: Truncated tests fail.
- Drop the size-prefix check: TrailingBytes fails.
- Restore the router `catch`: `RejectedInputIsNeverRetried` fails.
- Skip motion encode: the tick > 0 round trip and `cat-014` interpolation fail.

**Impacted suites:**
- **Native:** `WorkshopWire*`, `WorkshopRead*`, `WorkshopSave*`, `WorkshopTrace*`, `ClockTests`, `StateLatchTests`, `PressurePlateTests`, `CounterTests`, `DelayedSignalTests`, `ActivationTimerTests`, `WorkshopBumperTests`, `WorkshopWallTests`, `ElectricalSupplyTests`.
- **Node:** `workshop-pose-ring`, `workshop-observation`, `workshop-client-lifecycle`, `workshop-save-storage`, `workshop-console`.
- **Chrome:** all, especially cat-005, cat-015*, cat-037, cat-051, cat-062b and the Save/Load suites.

**Risks:**
- Variable-size reads change allocation: one buffer per publish replaces the reused `ReadOutput`. Measure it, but only record it (P0-034 owns the budget).
- `cat-005` mock encoders must be regenerated.
- 9.4b's clock-ring rewrite of `worker.js` and `workshop-client.js` collides with Tasks 5 and 8; start only after it lands.

## Verification

- `rg -n "WorkshopWire\." --glob '!engine/gpu/WorkshopWireFlatBuffers.cs' --glob '!engine/gpu/WorkshopWireGenerated.cs'`: expected 0.
- `rg -n "getUint16\(vtablePos|responseAbi|READ_BODIES_OFFSET" CuriousContraptions.*/wwwroot`: expected 0.
- `dotnet test CuriousContraptions.slnx --filter WireFaultTests`: expected all pass; with Task 3 reverted, the red set fails.

## Legacy Deleted

- `engine/gpu/WorkshopWire.cs` (and `.uid`).
- The three `Owned` rows in `CuriousContraptions.tests/WorkerAbiTests.cs`. The no-unowned-constant fact stays.
- The router `catch` and `isFlatBuffer` reserve (`WorkshopCommandRouter.cs:15-25,42`).
- The `ReceiveRead` `catch` (`BrowserWorkshopClient.cs:270-276`).
- `extractReadIdentity` (both copies), the `sequence()` vtable/fixed-offset parse, `responseAbi`, and the 3 `worker.js` layout constants.
- `Program.ReadOutput`, `Program.ResponseAbi` and the `EncodeResponse` legacy branch.
- `WorkshopSchedule.PrepareBytes`.
- `WorkshopSaveCodec.ByteLength` and its catch-all.
- Schema field `WorkshopCommand.construction`.
- `WholeCount` and the counter's float threshold.
- The session `: default` decode fallbacks.

## Implementation Notes

Amelia-21, 21 Oct 2026. Evidence: `evidence-9a-2/`.

- **One frame for every root.** `WorkshopWireFlatBuffers.OpenFrame` checks, in order: the 4-byte size prefix against the length (`Truncated`, `TrailingBytes`), the root identifier at bytes 8..12 (`UnknownIdentifier`), then the vendored verifier with that identifier (`VerifierRejected`). The vendored verifier throws `OverflowException` on some hostile signed offsets (`Convert.ToUInt32` in `FlatBufferVerify.cs:277`); that one call is translated to `VerifierRejected`, and no other catch exists on any decode path. A frame fault on a save becomes the same-named `WorkshopSaveRejection`.
- **Names.** The spec's `WireFault` is `WireFaultException` (analyzer CA1710). It is not an `ArgumentException`.
- **Motion** is `motion: [uint8]`: the physics ABI's own `PhysicsMotionRead` record, validated by `PhysicsMotionRead.Decode` against the read's bodies and tick. Its Half lanes stay for Story 9A.44. A committed read past tick 0 without motion is `MissingField`; admission's absent motion stays absent (no fabrication). `PhysicsMotionRead.Validate` no longer overflows on a hostile tick (the fuzz corpus found `checked(tick * steps)`).
- **Counter** target is `target_count: uint32`: 0 is `MissingField`, beyond Int32 is `OutOfRange`; `WholeCount` and the f32 threshold are deleted.
- **Session** is required on commands and responses (`MissingField`), never defaulted.
- **Save pre-validation** (`WorkshopSavePrevalidation.Check`) runs on the verified record before any domain value is built: required records and fields, defined enums, finite values, every per-kind range after the decoder's binary16 narrowing, declared materials, unique nonzero identities, resolving connection references, the connection rules (the domain's own `WorkshopConnections.Validate`, now over a part lookup, whose typed refusal becomes `InvalidConnection`), the named-goal rule and the allocator. `WorkshopSaveRejectionTests.PreValidationAdmitsExactlyWhatTheDomainAdmits` pins parity lane by lane (every f32 lane of every kind, specials, sweeps and every domain boundary found by bisection), so a range change on either side fails. Domain construction (`WorkshopSaveCodec.Construct`) catches nothing; `WorkshopSaveRejectedException` joins `WorkshopUiFailure.IsRejection`. Residual (typed refusals, not faults): an authored lesson's own settings rules (`PuzzleSettings`, Story 9A.26 deletes the lessons) and the workbench guide rule (`WorkbenchFullException`); both filed as F17-2. Deliberately stricter than the domain, because no encoder writes them: a Free save that carries a puzzle record, a precision outside [0, 1] for any lesson, and an inventory part that names no delivered element. `EveryAuthoredLessonSaveLoadsIncludingAMissingGoalPart` shows none of them refuses a real save. A Captured goal's parts are *not* required to be on the bench (the first candidate required it and refused CAT-062b's MissingReceiver save; the test now pins that).
- **Reads and acknowledgements.** The Simulation publishes `Publish(read, readIdentity24, poseSlot, …)` and acknowledges `Acknowledge(response, sequence8, …)`; the browser sends `send(id, command, sequence8)`. `WorkshopPoseRing.ComposeSlot` builds the slot (header with the capture time, then one record per body), which `worker.js` copies under its sequence lock. `ResponseAbi`, `CommandAbi`, `extractReadIdentity` and the `sequence()` vtable parse are deleted. One `FlatBufferBuilder` is reused for every published read, and `CCGPU_RETAINED` prints the already encoded bytes.
- **Not split:** `tools/e2e/workshop-wire.ts` keeps its hand-written helpers below a marker (splitting would change 25 importers for no behaviour); `schemas/README.md` records the exact regeneration commands (flatc 25.2.10, verified to reproduce the committed C# byte for byte).
- **Left to Story 9A.15:** `PartId.ToNumericId`/`FromNumericId` no longer carry any wire or save identity, but `tools/Coverage` reads that table as its declared-identity list.

## Spec Change Log

- 21 Oct 2026 (coordinator, owner rule): every version item dropped (the `UnsupportedVersion` reason, the v13 row, the version bumps of Tasks 5 and 9, AC4's trace bump). AC4 is now the typed pre-validation refusal.

## Review Triage Log

## Appendix: reference census (main tree, 18 Oct 2026; symbol authoritative)

- **`WorkshopWire` references** (rg count): tests `WorkshopWireTests` 43, `WorkshopReadTests` 20, `WorkshopSaveTests` 7, `WorkshopTraceTests` 6, `ClockTests` 4, `WorkshopBumperTests` 3, `StateLatchTests` 3, and 2 each in `WorkshopWallTests`, `PressurePlateTests`, `DelayedSignalTests`, `CounterTests`, `ActivationTimerTests`, `ElectricalSupplyTests` and `WorkerAbiTests`.
- **Production:** `WorkshopWireFlatBuffers` 13, `Program.cs` 6, `WorkshopSchedule` 3, `WorkshopClockWire` 3, `BrowserWorkshopClient` 3, `WorkshopMemoryWire` 2, `WorkshopHint` 2, `WorkshopCommandRouter` 2, Animation `Program` 2, and 1 each in `WorkshopSaveCodec`, `WorkshopPoseHistory`, `BrowserWorkshopClient.Schedule`, `WorkshopTrace` and `Program.Schedule`.
- **Tools and docs:** `tools/Coverage/CapabilityImplementationRequirements.cs:13` (path string); `tools/e2e/workshop-driver.ts:66,118` (comments); `docs/gpu-f32-physics.md`, `docs/coverage/engine/capabilities-01.json` (2) and `docs/work-orders/P0-004/ownership/part-023.json` (historical, unchanged).
- **No legacy layout use** in `tools/WireContract`, `tools/TraceAllocations` or `tools/LifecycleContract`.
- **No references** in `.csproj` globs (the workers compile `../engine/gpu/*.cs`, so the deletion is automatic), `export_presets.cfg`, `.tres` or `.tscn`.
- **`WorkshopSaveCodec.ByteLength`:** 0 consumers.
- **`?? 0` on `Read.Motion`** (`BrowserWorkshopClient.Activation.cs:39`, `.Electrical.cs:38`). This becomes non-null after motion is carried; replace it with a required value.
