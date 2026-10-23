---
title: "9A.22 — Event ring producer"
status: review
baseline: ae3cacf98f6fe8f7d3fb94a9d52b557ca970ae8a
---

# Outcome and authority

When a tick commits, the Simulation worker publishes exactly one typed 64-byte record per discrete transition into a scene-sized `SharedArrayBuffer` ring. A discarded or failed tick publishes nothing. This slice is the producer only; the consumers belong to 9A.23 + 9A.25b.

Roadmap position 44, before C-Memory, which needs `ThresholdReached` and `MemoryChanged`. Start after C-Clock merges. Re-derive every symbol and count there, and cite symbols, not lines.

**Authority:**
- epics 9A.22, plus 9A.23, 9A.25b and 9A.41 as consumers;
- the isolation doc, invariants 4, 5 and 7, and §2.3;
- the `gpu-f32-physics.md` compilation model and its one-way flow;
- spec-9-2b §5;
- audit findings D1 and A16;
- the C-Lamp review F1, F2 and F5 (coordinator, 23 Oct).

Native real-simulation C# proof is PRIMARY. Browser proof is pending.

**Facts at 61ded7ad:**
- Production has no producer:
  - `ActivationNetwork.Consume` writes `eventBuffer` on the *candidate* (the Clock and Latch branches).
  - Both production callers (`AdvanceScheduled` and `ConsumeScheduled`) pass `default`.
- The timestamps hard-code `8_333_333` ns, which is valid at 120 Hz only.
- `EventRingBuffer.DefaultCapacity = 1024` breaks invariant 7.
- `EventDescriptor.PayloadLayout` is a free string.
- No JS ring code exists. `git grep WorkshopEventRing|readEventRing|createEventConsumer|writeEventRing` returns 0.
- No committed read carries a prismatic-limit state.
- `SimulationEpoch` advances at both Admitting and Resetting.

# Finite design decisions

- **D1 Kinds.** The closed enum `SimulationEventKind : uint`, where 0 = None and is refused. Each record has exactly **one** source; no other path emits that transition.

  | Kind | Domain | Sole source | Payload: Magnitude / Count / Level |
  |---|---|---|---|
  | Impact | Mech | `ContactWorkRead.OccurrenceCount` delta, one record per increment (each new `ContactWorkOccurrence` row, at its own time) | supplied (debit) J / occurrence ordinal / – |
  | Passage | Mech | each `PassageSensorTracker` fire, every pass, latched or not | speed m/s / **n-th pass of that sensor this run (F2)** / – |
  | Captured (new; 9A.23 needs it for Receiver capture) | Mech | `CaptureLatch` Clear→Latched | – / – / – |
  | ContactLoad | Mech | the plate's debounced committed level (`ContactLoadSource` rise, and release after its hold); revised from the raw row edge, which jitters | that tick's measured kg / – / 1 or 0 |
  | TriggerActivated | Signal | Physical-stage network occurrences `Contact`, `Orientation`, `OscillatorTick`, and `TimerElapsed` of a `Latch`-policy timer | approach speed or 0 / oscillator count / – |
  | ThresholdReached | Signal | the network's `ThresholdReached` occurrence (renamed from `CounterReached`) | – / target / – |
  | MemoryChanged | Signal | the latch's boundary resolution On↔Off | – / – / new state |
  | ElectricalPowered | Elec | `ElectricalSourceRead.Available` edge | remaining J / – / 1 or 0 |

  - The network's `Passage` occurrence produces **no** record (the aperture's tracker passes own Passage); the `ContactLoad` kind is the level node's rise/release, never the raw row. The mapping from emitting node kind to record kind is `EventRecordProducer.KindOf`.
  - A `Rearm` (hold) timer's expiry is a level change, a committed query, and produces no record.
  - Delete `CounterReached` and `LatchSet`, with no alias; the bundle refuses those names.
  - Delete `PrismaticLimit`, which has no producer. The first story with a gate-limit cue adds the kind and its producer.
  - The mapping is one exhaustive table. It has no `PartId` and no element branch.
- **D2 Record (64 B, little-endian):**

  | Offset | Field |
  |---|---|
  | 0 | Sequence u64 (the seqlock word, written last) |
  | 8 | Tick u64 |
  | 16 | Kind u32 |
  | 20 | Domain u32 |
  | 24 | Source u64 (owner `GpuBodyId`) |
  | 32 | Other u64 (body, or 0) |
  | 40 | Ordinal u32 |
  | 44 | Phase f32 |
  | 48 | Magnitude f32 |
  | 52 | Count u32 |
  | 56 | Level u32 |
  | 60 | reserved, 0 |

  - The meaning of each field per kind is the D1 table, typed in Abstractions.
  - `EventDescriptor` becomes `(SimulationEventKind Kind)`; `PayloadLayout` and the `Impulse`/`Contact*` accessors are deleted.
  - There is no wall clock and no cadence constant.
- **D3 Commit point.**
  - `WorkshopGpuDevice.Stage` builds the candidate batch and the per-row Count counters, from the Physical stage only.
  - `Commit` adopts both and assigns contiguous sequences, starting at the committed WriteSequence + 1.
  - `Discard` and any throw drop both and consume no sequence.
  - `ActivationNetwork` returns its transitions as data; it writes no buffer.
- **D4 Order within a tick:** (Ordinal, Phase, Kind, Source, Other). Impact, Passage and Captured carry their own physical time from their rows; a row with no sub-tick time (a supply edge, a memory or contact-load level) uses the tick-boundary `ActivationTime`.
- **D5 Capacity and overflow (revised, coordinator decision 24 Oct 2026, review F1/F4).**
  - At admission, capacity = nextPow2(max(1, Σ over producers of `RecordsPerTick(kind)` × HorizonTicks)):
    - a producer is one (owner, kind): one per contact-work owner, passage aperture, capture frame, supply source and emitting activation node. It never counts (owner, body) pairs, so the ring is linear in the scene's producers;
    - `RecordsPerTick` is 1 for every kind (justified per kind on `EventRecordProducer.RecordsPerTick`); a burst above it still commits and the excess beyond the ring is counted in `Lapped`;
    - Horizon is 0.5 s at the admitted cadence (30/60/120 ticks), a time requirement.
  - Hard limit: `EventRingBuffer.MaximumCapacity` = 2^20 slots (64 MiB + 64 B, representable). It is checked at the fastest cadence, so Σ ≤ 8738 records per tick is admitted at every cadence and a cadence change never refuses an admitted construction; Σ ≥ 8739 is refused with `GpuAdmissionException` → `WorkshopRejection.GpuAdmission` (the session stays Building, no fault).
  - Publish never blocks, throws or uses `checked`. A batch larger than capacity still advances the sequence and adds the excess to the header's `Lapped`; the tick commits.
  - Consumer laps are detected from the sequences (9A.23).
- **D6 Header (64 B):** Capacity u32, SlotBytes u32, Epoch u64 (`SimulationEpoch`), WriteSequence u64 (atomic, stored after the slots), Lapped u64; the rest is 0.
- **D7 Transport.**
  - `Program` drains the device's committed outbox (`publishEvents`: the C#-composed header with epoch and capacity, then each sequenced record) before **every** read and **every** acknowledgement, so each committed record is published exactly once and in sequence order, including ticks committed by Step (acknowledged without a read) or by a superseded loop.
  - `worker.js` `writeEventRing` copies slot by slot under the seqlock, like `writePoseRing`, with no domain logic.
  - It allocates a fresh SAB when the epoch or capacity changes and posts `{ eventRing }` before that read (the `growPoseRing` precedent).
  - The main thread only attaches `client.eventRing`. There is no reader and no `globalThis` hook (9A.24 requires 0).
- **D8 Lifecycle.**
  - The ring is runtime-only and never saved. Pause produces no records.
  - Reset drops the batch, the counters and the sequence. The next Run gets a new epoch, with sequences from 1.

# Ordered tasks

- [x] **Baseline.**
  - Run the native suites.
  - Enumerate each package's compiled source rows against D1. That gives the expected-kinds table; the old `Events` lists are not the source.
  - Pin the expected-change sites: `StateLatchTests` (the latch ring), `PackageRuntimeParityTests` (events) and `EventRingBufferTests`.
- [x] **Abstractions:**
  - the D1 enum, the D2 codec and accessors, and the D6 header;
  - a capacity-parameterised `EventRingBuffer.Append` and an epoch-aware `EventRingConsumer`, the test reference;
  - delete `DefaultCapacity`.
- [x] **Packages.** Make `Events` equal the baseline table. Expected: the Ball, Bowling ball, Lamp, Gate and Plate `LatchSet` entries go; the Bumper becomes Impact; the Receiver gains Captured; the Counter and Latch get the new kinds. Regenerate the bundle and its identity.
- [x] **Producer (D3–D5).** Delete `eventBuffer` and both publish branches.
- [x] **Transport (D7, D8).**
  - Pin the constants in `WorkerAbiTests`.
  - Add a node vm test modelled on `tools/workshop-pose-ring.test.mjs`.
- [x] **Docs.**
  - **(a)** In the epics 9R.9 AC, replace "produces exactly one more" with: "lights the wired counter(target 2) lamp target exactly once, or publishes exactly one more `Passage` record (Count 2); the first-input lamp stays at one". Put the same clause in CAT-002 §5 "Boundaries" and in §2 "Current tests".
  - Epics 9A.22:
    - the kind list (+Captured, −PrismaticLimit, with its owner);
    - the "payload layout declared by the element's `EventDescriptor`" clause becomes "the kind's engine-owned layout";
    - the Chrome reads go through the 9A.23 consumer or the 9A.24 port, post-catalogue.
  - Epics 9A.23: drop the stale "hook survives" clause.
  - Isolation doc: rows D1 and A16, and §2.3 (header and epoch).
- [x] **(c)** `cat-002.test.ts` `lightsOnceMore`: after reaching `before+1`, hold for the glow duration plus 500 ms, then re-read and assert that it is still `before+1`. Strict tsc only.
- [x] **Checks.**
  - One Debug slnx build with 0 warnings, then `dotnet test --no-build --no-restore`.
  - The node tests and strict tsc.
  - Release worker publish, `anvil_check` and `git diff --check`.
  - Mutations, each of which must be killed:
    - publish moved to Stage;
    - Discard keeps the batch or the counters;
    - the ordering key is dropped;
    - Lapped is not counted;
    - the epoch is not stamped;
    - Scheduled-stage emission;
    - Passage published only on the first latch;
    - the network's Passage also emits.
- [ ] **Independent review, then SnapshotApproval, then publication.** Commit, merge and push. Verify the exact-SHA CI and the deploy. Mark `9a-22` done; browser proof stays pending.

# PRIMARY native tests (production compile, worker engine, tick progression)

- **Exact records at exact ticks:**
  - Bumper hit: exactly 1 Impact, and no TriggerActivated.
  - Detector, two in-run passes after clearance: Passage Count 1 then 2, with the first-input lamp at 1.
  - Receiver capture: Captured.
  - Plate: ContactLoad 1 then 0.
  - Impact switch, domino, Delay and Clock: TriggerActivated. The clock emits at 120·k, at 60, 120 and 240 Hz.
  - Counter(3): ThresholdReached on the 3rd crossing only.
  - Latch: MemoryChanged 1 then 0.
  - Battery on and off: ElectricalPowered.
- **Negative and control cases. None of these publishes anything:**
  - a sub-threshold contact;
  - a reverse or glancing pass;
  - jitter;
  - a Pause;
  - a latch re-Set while On;
  - two crossings of Counter(3);
  - an unsupplied clock;
  - a hold-timer expiry.
  - A Delay with a before-networks destination gives exactly 1 record.
- **Commit:** a discarded or faulted candidate publishes nothing and consumes no sequence or count.
- **Order and determinism:**
  - Ticks follow the D4 order.
  - Sequences are contiguous from 1.
  - Two runs, and Run/Reset/Run, give byte-identical *records*, while the header epoch advances.
- **Overflow:** with capacity 4 and a 6-record batch, the tick commits, the sequence advances by 6, Lapped is 2, and the reference consumer reports the lap.
- **Lifecycle:**
  - Reset clears.
  - Save/Load then Run gives identical records.
  - There are no ring bytes in the save.
- **Capacity:** it is scene-derived, and the `NoFixedCapacityTests` populations run.
- **Round trip:** C# batch → the real `writeEventRing` (vm) → reference consumer gives equal records and an equal header.
- **No main-thread recomputation:** `rg` finds no producer or `Append` call outside the engine producer and the Simulation.
- **Conformance:** each package's declared kinds equal the kinds produced by its compiled rows.

# Pending browser cases (post-catalogue)

- `cat-015a`: one Impact per Bumper hit.
- `cat-020`: one ThresholdReached per target.
- `cat-002`: the in-run second pass, through the counter(2) lamp or a Passage record, and the F5 re-read.
- These read through the 9A.23 consumer or the 9A.24 port.
- Re-run `cat-017`, `cat-037` and `cat-052*`.

# Open questions

None owner-only. Decided by game intent:
- Captured is added, for the 9A.23 Receiver one-shot.
- PrismaticLimit is deferred: it has no producer and no consumer before Epic 14.
- The horizon is 0.5 s.
- A hold-timer expiry is a level, so it produces no record.

# Code Map

- **Change:**
  - `CuriousContraptions.Abstractions/{EventDescriptor,EventRingBuffer}.cs` and `CuriousContraptions.Bundle/{BundleSchema,BundleValidator}.cs`;
  - the affected `elements/*/…Declaration.cs`;
  - `engine/gpu/ActivationNetwork.cs`, plus a new `engine/gpu/EventRecordProducer.cs`;
  - `CuriousContraptions.Simulation/{WorkshopGpuDevice,Program}.cs` and `wwwroot/worker.js`;
  - `CuriousContraptions.web/wwwroot/workshop-client.js` (attach only);
  - `tools/e2e/cat-002.test.ts` and the tests;
  - the epics, CAT-002, the isolation doc, `TODO.md` and `sprint-status.yaml`;
  - the architecture code map's Event Ring row (`_bmad-output/planning-artifacts/architecture.md`).
- **Unchanged:**
  - the solver, the sensor laws and the pose ring;
  - the `WorkshopRead` wire;
  - the occurrence reads and pumps (9A.23 deletes them).
- **Re-derived at ae3cacf9 (C-Plate and C-Clock merged):** `engine/gpu/{ActivationTimers,PassageSensorDeclaration,WorkshopMessages}.cs` also change (the occurrence kind rename and the tracker's `Fired` list); `CuriousContraptions.Simulation/WorkshopGpuDevice.cs` passes the admitted scene to Stage; the oscillator is the generic C-Clock instance and the plate's level is the generic `ContactLoadSource` node.

# Current state

Uncommitted native candidate on ae3cacf9 (worktree `story-9a-22`), tracked diff sha256 `65a38b17…` excluding evidence and this spec, plus the new files' hashes (`evidence-9a-22/candidate/identity.txt`). Next: independent review. Browser cases pending.

**Decisions confirmed:** records only at tick commit (staged in `WorkshopGpuDevice.Stage`, sequenced by `EventRingOutbox.Commit` in `Commit`; Discard/failure drop them); one source per kind; scene-sized capacity (revised per D5 after review F1/F4: producers × 1 record per tick × 0.5 s of ticks, next power of two, at least 1; above 2^20 slots refused typed at admission); counted typed overflow (`Lapped`); header epoch = the admitted `SimulationEpoch`; `Captured` added; `PrismaticLimit` deleted (no producer).

**Decisions revised (implementer, game intent):**
1. *ContactLoad source:* the plate's debounced committed level (`ContactLoadSource` node rise/release, Magnitude = that tick's measured kg), not the raw row phase. The raw row jitters on rebounds (measured: several raw edges while the level rises once), and the level is what the plate's supply and lamp use; one source still.
2. *Sub-tick times:* Impact and Captured carry their own occurrence time (`ContactWorkOccurrence` and `CaptureLatch` hold it); only supply edges and memory/load levels use the tick boundary.
3. *Kinds via node table:* the network returns `ActivationTransition`s (`ActivationCheckpoint.Transitions`, Physical stage only); `EventRecordProducer.KindOf(node kind, timer completion)` is the one exhaustive table (a passage node, an indicator and a rearming timer map to None).
4. *Passage rows:* every pass from the tracker's new `Fired` list (the network's per-sensor span can drop passes); Count = committed per-body `PassedCount` sums + in-tick time order. The sensor law is unchanged.
5. *Transport:* `Program` drains the outbox before every read and every acknowledgement (`publishEvents`), because Step commits a tick acknowledged without a read and a superseded loop commits without `EmitRead`; each record is published once, in sequence order. An empty drain (no admitted world) is a no-op.
6. *Element-named occurrence kind:* `ActivationOccurrenceKind.CounterReached` renamed `ThresholdReached` (value 5, numeric on the wire), so all 9 Known rows owned by `Events` are deleted (Known 136 → 127); 4 Generic contexts added for `EventRecordProducer` (`KindOf`: Latch, Counter, StateLatch as DeclaredCapability; `Stage`: CaptureLatchPhase ×2) and 2 Consume counts adjusted.

**Expected-kinds table (compiled rows, `EveryPackageDeclaresExactlyTheKindsItsCompiledRowsProduce`):** bumper Impact; ball_detector Passage; basket Captured; pressure_plate ContactLoad; switch, domino, delay, clock TriggerActivated; counter ThresholdReached; latch MemoryChanged; battery ElectricalPowered; every other package none (ball, bowling, lamp, powered_gate, hold_timer, the four logic gates, funnel, pipes, ramp, spring, wall).

# Evidence (`evidence-9a-22/`)

- Baseline (`baseline/`): 0-warning Debug build; 17/77/167/189/2628, rc 0.
- Candidate (`candidate/`): one 0-warning Debug slnx build; full `dotnet test --no-build --no-restore --blame-crash` WorkerAotGuard 17, BundleElements 174, Coverage 77, E2eRunner 189, CuriousContraptions.tests 2686; 0 failed, 0 skipped, 0 duplicate-ID notices, rc 0 (`full-test.log`; the E2eRunner crash dumps are its own hang-kill fixtures, as at baseline). CI "Test C# tools" step under bash 77/174/17/174, rc 0 (`ci-tools.log`). Release Simulation and Animation.Worker `--no-incremental` 0 warnings (`worker-release.txt`). `node --experimental-vm-modules --test tools/*.test.mjs` 101/101 (`node-tests.log`). Strict tsc 6.0.3 rc 0; tsx snapshot unit 24/24 (`ts-checks.txt`). `git diff --check` clean (`diff-check.txt`). `anvil_check` 0 findings on every changed source, test, script and doc.
- New native proof (`EventRingProducerTests`, production compile + worker engine + `WorkshopSimulation`): Impact per contact-work increment and no trigger; Passage Count 1 then 2 with the first-input lamp held; glancing/reverse none; Captured / miss none; plate ContactLoad edges = committed level edges, 2 m jitter → one record; switch trigger / sub-threshold (64 m/s) none; domino 30° one / 10° none; switch → Delay → hold timer exactly two triggers, hold expiry none; oscillator at 60/120/240 Hz (Count k, ordinal = tick × substeps), unsupplied none; Counter(3) only at the third input, ThresholdReached before TriggerActivated at the same time (D4); latch MemoryChanged 1,0,1,0,1,0,1 at 120…600; a Set-only latch: one MemoryChanged at 120, none for the Sets at 240…600 while On; supply off/on edges; Pause, a discarded candidate and a failed Step publish nothing and consume no sequence or count (the next pulse is sequence 1, Count 1); two runs and Run/Reset/Run byte-identical records under an advancing epoch (consumer restarts); Save/Load identical, save bytes unchanged; capacity 128/256/512 by cadence, empty bench 1; overflow 6 into 4 (sequence 6, Lapped 2, consumer lap 2); the shipped `worker.js` `writeEventRing` (node vm) writes the same ring bytes as the C# reference for a real run, a Reset and an overflow; package conformance; over this checkout's `git ls-files` sources only the device and producer touch the outbox, 0 hits of `readEventRing|createEventConsumer|WorkshopEventRing|eventBuffer`. Plus `EventRingBufferTests` (layout, refusals, seqlock skip, epoch restart), `ActivationTimerTests` (scheduled stage emits nothing), `BallDetectorTests` (detector jitter: `Fired` empty through twenty oscillations, exactly one pass before and after clearance), `StateLatchTests` (LevelChanged), bundle refusal of `CounterReached`/`LatchSet`/`PrismaticLimit`/`PayloadLayout`/None/duplicates, `WorkerAbiTests` (6 event constants pinned), `tools/workshop-event-ring.test.mjs`.
- Mutations (`candidate/mutations.log`), each applied alone and reverted: M1 publish moved to Stage — killed; M2 a discarded tick writes its records — killed; M3 ordering key dropped — killed; M4 Lapped not counted — killed; M5 epoch not stamped — killed; M6 scheduled stage also records — survived the producer suite (equivalent at the device, which reads only the Physical checkpoint), then killed by the added network fact; M7 Passage only on the first pass — killed; M8 network Passage also publishes — killed; M9 wrong tick stamp — killed (15); M10 overflow faults — killed; M11 a second ContactLoad producer (raw row) — killed; M12 worker.js keeps the ring across an epoch — killed (C# round trip and node test).
- Bundle identity (`candidate/bundle-identity.txt`): base `5bf3d63a…` (regenerated at ae3cacf9 in a throwaway worktree, since removed) → candidate `25069901…`; only `Events` changed (per-element diff recorded: `PayloadLayout` gone, kinds as in the table above).
- Publication never validates or throws: `SimulationEventRecord.Write` only encodes; the producer validates every record in `Stage`, before the commit point.
- **Process note (pre-write validation):** `anvil_validate_write`/`anvil_apply_patch` ran before writing for: `EventDescriptor.cs`, `EventRecordProducer.cs` (create), the 15 element declarations, `BundleValidator.cs`, the `ActivationNetwork.cs` checkpoint/consume edits, `ActivationTimers.cs`, `PassageSensorDeclaration.cs`, `WorkshopGpuDevice.cs` (outbox fields and commit), `Program.cs`, `worker.js` (writer), the first `StateLatchTests.cs` change, `EventRingProducerTests.cs` (create) and `workshop-event-ring.test.mjs` (create). They ran only **after** writing for: `EventRingBuffer.cs`, `EventRingBufferTests.cs`, the `workshop-client.js` branch and the two `CommitContactLoads` lines. They did **not** run before writing for every later edit: `PackageRuntimeParityTests`, `WorkerAbiTests`, `ActivationTimerTests`, `BallDetectorTests`, `CounterTests`, `element-isolation-known.json`, the csproj, both BundleElements test files, `cat-002.test.ts`, the later `WorkshopGpuDevice`/`worker.js`/producer/`EventRingProducerTests`/mjs edits (scene parameter, accessor, dispose and `publishEvents` lines, `capture` rename, `Math.Min`, `Write` without validation, git-scoped scan, added facts), `WorkshopMessages.cs`, the docs, epics, architecture, CAT-002, TODO, sprint-status and this spec. No hook ran it automatically. Remedy: every changed file's final content was then validated (`anvil_validate_write` on all 49 paths with their sha256: 49 allow) and scanned in full (`anvil check` CLI over all 49 files, `candidate/anvil-check-cli.txt`: no warnings; `anvil_check`: 0 findings).

# Review forward fix (F1 blocking, with F3, F4, F6; F2 handed off), 24 Oct 2026

**Sizing (coordinator decision, 24 Oct 2026):** capacity = nextPow2(max(1, P × 1 × H)), where P = the number of event producers (one per contact-work owner, passage aperture, capture frame, supply source and emitting activation node; never per (owner, body) pair), 1 = `RecordsPerTick` for every kind (justified per kind on `EventRecordProducer.RecordsPerTick`), and H = 0.5 s of ticks at the admitted cadence (30/60/120). **Limit:** 2^20 slots (64 MiB + 64 B, a representable byte length), checked at the fastest cadence: P ≤ 8738 is admitted at every cadence, P ≥ 8739 is refused by `EventRecordProducer.RingCapacity` with `GpuAdmissionException` → `WorkshopRejection.GpuAdmission` (Building kept, `Fault` None). Examples: 200 balls + 200 bumpers = 200 producers → 16,384 slots (1 MiB) at 120 Hz, against about 4 Mi slots (256 MiB) under the old pairwise rule.

**New proof (`EventRingProducerTests`):** `NBallsAndNBumpersSizeTheRingLinearlyInProducers` (N = 1/10/40 → 64/1024/4096, N Impact producers); `TheLimitAdmits…AndRefusesOneMoreTyped` (8738 → 2^18/2^19/2^20 at 60/120/240 Hz, 8739 typed refusal at every cadence, `ByteLength(2^20)` computable, `Capacity(2^20 + 1)` refused); `AConstructionBeyondTheLimitIsRefusedTypedAndTheSessionStaysBuilding` (the production admission path with 8738 and 8739 batteries: admitted at 2^19, or Rejected/GpuAdmission with Building kept, no fault, then a small construction admitted); `ABurstLargerThanTheRingCommitsEveryTickAndCountsTheExcessInLapped` (3588 ticks published at once: every tick commits, WriteSequence = all 300 records, the newest 256 written, Lapped 44, the consumer reads the newest 256 and reports the lap, then the sequence continues). F3: the conformance test now also requires the wired lamp, compiled both as a level indicator (latch, plate) and as a first-input indicator, to produce nothing. Mutants: M13 (the limit admits one more) killed (4 failures); reviewer R5 (level indicator → MemoryChanged) killed; M14 (pairwise Impact producers) killed.

**F6:** D1 (ContactLoad source, Impact time, ThresholdReached name), D4 (sub-tick times), D5 (sizing, limit) and D7 (drain before every read and acknowledgement) are amended in place; CAT-002 §2 now uses the coordinator's clause word for word.

**Handoff to 9A.23 (review F2):**
- Impact records are built from the device-side contact-work occurrence rows (`PhysicsContactWorkRead` occurrences: target, ordinal, phase, debit, sequence). 9A.23's delete list ("the occurrence arrays in `PhysicsContactWorkRead`") must keep those rows available to `EventRecordProducer.Stage`, or re-source Impact first.
- The JS reader must use `Atomics.load` for the slot sequence word before and after the copy and for `WriteSequence` (the C# `EventRingConsumer` is a single-threaded reference with `Volatile` only), and it needs a concurrent stress test; a Reset after a superseded commit drops undrained records under the new epoch.

**Checks (`evidence-9a-22/fix-f1/`):** one 0-warning Debug slnx build; full `dotnet test --no-build --no-restore --blame-crash` 17/77/174/189/2695, 0 failed, 0 skipped, 0 duplicate notices, rc 0; node harnesses 101/101; Release workers 0 warnings; `git diff --check` clean; `anvil check` no warnings. Process note: these fix edits were again anvil-validated after writing (sha256-bound `anvil_validate_write`, all allow), not before.

# Pending browser cases (unchanged)

`cat-015a`, `cat-020`, `cat-002` (in-run second pass via the counter(2) lamp or a Passage record, and the F5 re-read now in `lightsOnceMore`), re-runs of `cat-017`, `cat-037`, `cat-052*`; reads through the 9A.23 consumer or the 9A.24 port.
