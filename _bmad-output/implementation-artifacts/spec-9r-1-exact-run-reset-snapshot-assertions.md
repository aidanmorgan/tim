---
title: 'Story 9R.1: Exact Run/Reset Snapshot Assertions in Every Listed Suite'
type: 'bugfix'
created: '2026-10-18'
status: 'ready-for-dev'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/AGENTS.md'
  - '{project-root}/_bmad-output/planning-artifacts/epics.md#story-9r1-exact-runreset-snapshot-assertions-in-every-listed-suite'
  - '{project-root}/_bmad-output/planning-artifacts/retro-verification-2026-10-18.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** No suite compares the state after Reset with a pre-Run snapshot. The retro-verification found this as report #14 and in the last row of §D. Its rows for 6.6–6.11, 8.2, 9.1, 9.2 and 9.3 list the gaps:
- Every Reset in cat-048a/b, 049, 050, 051, 052, 002 and 020 is followed only by an `errors == 0` check, or by a range or run-to-run comparison (cat-030 `:334-336`).
- The pipe, gate, plate, clock and counter suites reuse benches built by earlier tests.
- The engine does not publish two of the values the ACs ask for: the Counter count and the System Clock phase.

**Approach:** Add one snapshot helper to the driver. It captures the committed construction and the committed physical read through the existing channels, compares them bit for bit, and names the first field that differs. Make the counter and clock memory committed, published state. Rewrite every listed suite so each test builds its own bench and asserts the snapshot after the first Reset and again after a second Run and Reset.

## Boundaries & Constraints

**Always:**
- Compare every f32 as its IEEE-754 bits through a `DataView` (`setFloat32`/`getUint32`). Never use `===` or a tolerance, so −0 and NaN differ.
- Build each test's bench through actual UI controls.
- Repairs the lifecycle claims of Stories 6.6, 6.7, 6.8, 6.9, 6.10, 6.11, 8.2, 9.1, 9.2 and 9.3.
- Counter count and Clock phase live in the committed discrete checkpoint. They are never mutable fields on `ActivationNetwork`.

**Never:**
- Setters, imported solutions or numeric placement.
- A tolerance or an exclusion list beyond the identity fields named in D2.
- A second read path that bypasses the committed read.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|---|---|---|---|
| Exact restore | Snapshot S0 before Run; Run, Reset, snapshot S1 | `assertSnapshotEqual(S0, S1)` passes | N/A |
| One bit perturbed | S1 with one f32 bit flipped (unit check) | Fails, naming the field path, e.g. `read.bodies[id=…].local.y 0x40400000≠0x40400001` | The assertion message carries the path and both hex values |
| Second cycle | Run, Reset again | Equal to S0 | Same |
| Identity fields | sequence, revision, publication, epoch, capture stamp, session, master generation | Excluded (they must advance) | Any other field differing fails |

### Decisions (coordinator delegation, playability and "aggressive correctness")

- **D1. The snapshot channel is the committed read plus the save slot.**
  - The physical state comes from the read carried by the CCGPU acknowledgement of the `save()` command.
    - Save and Reset are acknowledged through the general path in `Apply` (`CuriousContraptions.Simulation/Program.cs:266-267`), which encodes `Simulation.Committed`. That read is the Build-phase state Reset just re-admitted (`engine/gpu/WorkshopSimulation.cs:291-299`); Save only bumps the revision (`:188-195`).
    - The early returns at `Program.cs:171`, `:179`, `:183` and `:201` (stale generation, Cancel, Busy) also carry `Committed`, but they are rejections. The helper therefore requires `outcome == Applied` and `kind == Save` before using a read.
    - `engine/MachineWorld.Gpu.cs:197` logs every response.
  - The authored construction comes from the IndexedDB save slot: instances (cell, local and rotation bits, parameters, locked), connections, and the puzzle and goal.
  - Rationale: Build mode does not republish the pose ring (`workshop-driver.ts:780-782`), and the ring holds at most 16 bodies (`engine/gpu/WorkshopPoseRing.cs:26`). The acknowledgement is exact, read-only and in production.
- **D2. Identity fields are excluded, and only these:** `sequence`, `revision`, `publication`, `epoch`, `capture` (`ClockStamp`), `session`, `masterGeneration`. `tick` stays in, and must be 0 in both snapshots. Rationale: these fields are required to advance.
- **D3. Scope addition required by the AC (flag to the owner): Counter count and Oscillator phase join the committed checkpoint.**
  - `_counterCounts`, `_counterLastTimes`, `_counterEmitted`, `_oscillatorPhases` and `_oscillatorDueTicks` are mutable arrays on `ActivationNetwork` (`engine/gpu/ActivationNetwork.cs:88-113`). `Consume` mutates them during candidate evaluation (`:457-464`), before commit.
  - They appear in no read. `GetCounterCount` and `GetOscillatorPulseCount` have no production caller, and `UpdateCounterLamps` is called only with 0 (`engine/MachinePart.cs:220`).
  - Without this change, "cat-017 asserts clock phase 0; cat-020 asserts count 0" cannot be observed without a setter.
  - Story 9R.10 builds on the same published phase.

</frozen-after-approval>

## Code Map

Line numbers refreshed against `e36467b8` (design review item 7). The symbol is authoritative. The frozen D1/D3 text above keeps its original 18 Oct references; the current ones are: `Program.cs` Save `:227`, general acknowledgement `:270-271`, early returns `:175-176`, `:183-184`, `:187-188`, `:205-206`; `WorkshopSimulation.Save` `:210-217`; `WorkshopSimulation.Reset` `:303-335`; `workshop-driver.ts` "Build mode publishes nothing" `:894`; `WorkshopPoseRing.BodyCapacity` `:26`; `ActivationNetwork` arrays `:88-94`, counter memory writes in `Consume` `:457-470`.

- `tools/e2e/workshop-driver.ts`
  - `decodeAcknowledgement` (`:144-157`) keeps only the header. Retain the decoded `WorkshopResponse`.
  - `save` (`:799`) returns void, and `expectSavedSlot` (`:808-840`) checks only magic and version.
  - `run` (`:869`) and `reset` (`:880`).
  - `WorkshopLevel` (`:333`).
- `tools/e2e/workshop-wire.ts`
  - `decodeSavedConstruction` (`:2138`), `WorkshopResponse` (`:1604`), `BodyRead` (`:192`), `ActivationRecord` (`:487`), `TimerRecord` (`:576`).
- `engine/gpu/ActivationNetwork.cs`
  - The mutable memory arrays (`:88-94`, filled `:107-116`), `Clear` (`:158-169`), `GetCounterCount` (`:177`), the oscillator getters (`:216-245`), and the counter increment inside `Consume` (`:454-470`).
- Read plumbing:
  - `engine/gpu/WorkshopRead.cs`.
  - `engine/gpu/WorkshopWireFlatBuffers.cs` `EncodeResponse` (`:212`).
  - `schemas/workshop_wire.fbs`.
  - `engine/gpu/WorkshopWire.cs`: the legacy codec, still reached by the B1 fallback.
- Reset re-admits a fresh world: `engine/gpu/WorkshopSimulation.cs` `Reset` (`:303-335`). Admission recompiles the network (`CuriousContraptions.Simulation/WorkshopGpuDevice.cs:72`).
- Suites, with Reset lines at `e36467b8`:
  - `cat-048a` `:102,132,180,224`
  - `cat-048b`, `cat-049` and `cat-050` `:56,84,103,135`
  - `cat-030` `:325-336`
  - `cat-051` `:145,202,252,304`
  - `cat-052` `:183,222,272,320`
  - `cat-017` `:203,244,270,321` (the range-only blade check after Reset is `:274-279`)
  - `cat-002` `:89,128,167,206,216,265`
  - `cat-020` `:229,295,361,436`

## Tasks & Acceptance

**Execution** (test first):
- [ ] `tools/e2e/run-reset-snapshot.ts` -- new pure module.
  - `snapshotFrom(response, savedBytes)` flattens every field to a `path → value` list, writing f32 values as bit strings.
  - `assertSnapshotEqual(a, b)` throws on the first differing path.
  - Rationale: one comparison authority.
- [ ] `tools/e2e/unit/run-reset-snapshot.test.ts`: unit tests that need no Chrome. They live under `unit/` so the Story 9R.0a runner doesn't treat them as a Chrome suite (coordinator, 19 Oct 2026).
  - identical snapshots pass;
  - one flipped f32 bit fails and names that path;
  - −0 against +0 fails;
  - a differing connection or parameter fails.
  - Write these red first.
- [ ] `tools/e2e/workshop-driver.ts`:
  - Keep the last decoded acknowledgement response.
  - `save()` returns the slot bytes.
  - Add `captureSnapshot()`, which runs Save, takes the Save acknowledgement read and the slot bytes, and returns `snapshotFrom(...)`.
  - Add `expectResetMatches(s0)`, which runs `captureSnapshot` and `assertSnapshotEqual`.
- [ ] `engine/gpu/ActivationNetwork.cs` -- move counter memory and oscillator phase and due tick into committed checkpoint rows, so `Consume` reads committed values and returns the candidate's. Delete the five mutable arrays and the getters that read them.
- [ ] `engine/gpu/WorkshopRead.cs`, `schemas/workshop_wire.fbs`, `engine/gpu/WorkshopWireFlatBuffers.cs`, `engine/gpu/WorkshopWire.cs`, `tools/e2e/workshop-wire.ts` -- publish and decode those rows, keeping both codecs in step until Story 9A.2 deletes B1. The native round-trip test fails first.
- [ ] Rewrite the ten suites (`tools/e2e/cat-048a`, `048b`, `049`, `050`, `030`, `051`, `052`, `017`, `002`, `020`):
  - Each test reloads and builds its own bench.
  - Take `s0 = captureSnapshot()` before Run.
  - After Reset, and again after a second Run and Reset, call `expectResetMatches(s0)`.
  - Delete the range-only Reset checks and every dependency on test order.
- [ ] Suite-specific assertions:
  - `cat-051`: the blade body read equals its authored closed pose.
  - `cat-017`: the oscillator row has phase 0 and no due tick.
  - `cat-020`: the counter count is 0 and its activation is `Clear` (contact open).
- [ ] `CuriousContraptions.tests/CounterTests.cs`, `ClockTests.cs` -- native tests:
  - the count and phase are in the committed read;
  - a superseded candidate leaves the committed count unchanged;
  - Reset gives 0.

**Acceptance Criteria:**
- Given the shared `workshop-driver.ts` helper capturing the committed construction (every body pose as f32 bits, part parameters, connections and goal state) through the committed-read channel before Run, when a suite runs, resets and re-reads, then the helper asserts bit-equality with the pre-Run snapshot and reports the first differing field.
- Given a snapshot with one perturbed f32 bit, when the unit check runs, then it fails and names that field.
- Given each listed suite, when it resets, then it asserts the snapshot after Reset and again after a second Run and Reset.
- Given cat-048a, cat-048b, cat-049 and cat-050, when any one test runs alone, then it builds its own bench and passes.
- Given Reset:
  - cat-051 asserts the blade's closed pose;
  - cat-017 asserts clock phase 0;
  - cat-020 asserts count 0 and an open contact.
- Given the gates, when they run:
  - `dotnet build CuriousContraptions.slnx` reports 0 warnings (the Story 9R.0 gate);
  - `dotnet test CuriousContraptions.slnx` passes 100%;
  - every `tools/e2e/*.test.ts` suite passes 100% in one serial Chrome run against the production preview.

## Implementation Notes

- **Snapshot authority.** `tools/e2e/run-reset-snapshot.ts` lists every reachable schema field explicitly (`SNAPSHOT_SCHEMA`), computes FlatBuffers struct offsets from that listing, and flattens raw bits (`bb.readUint32` for every float). `tools/e2e/unit/run-reset-snapshot.test.ts` checks the listing against `schemas/workshop_wire.fbs` (names, types, order, reachability) and the computed offsets/slots against the generated TS accessors, so schema drift turns it red (evidence `unit-red-schema-drift.tap`, taken when `activation_memory` was added).
- **Bench body count.** The committed read carries the dynamic bodies only (static parts have none): `bodies` is that count (for example 1 for a ball with pipes, 2 for a ball and a gate blade).
- **Per-cycle proof.** `expectRunAdvanced` pauses through the Workshop menu and uses the Pause acknowledgement's committed read (tick > 0); Reset is then pressed from Paused.
- **D3 shape.** One `ActivationMemory` row per Counter or Clock node (`engine/gpu/ActivationMemory.cs`): kind, count (counter count / clock pulse count), last counted input time, `OscillatorPhase` (Stopped 0 / Running 1) and due tick (0 = none). `ActivationCheckpoint`, `WorkshopRead`, both codecs (B1 header byte 113 + a 320-byte section; FlatBuffers `activation_memory`) and the TS decoder carry it. `ActivationNetwork` keeps declarations only; the counter's single emission is its Latched activation, and the clock's pulse count also moved into the row, so the network has no mutable fields left.

## Spec Change Log

- **20 Oct 2026, design review (coordinator), amendments to D1/D2.** The frozen text is unchanged; these amendments govern where they differ:
  - **D2 amended per design review:** the excluded identity paths are matched by full top-level path, never by field name: `read.sequence`, `read.revision`, `read.publication`, `read.epoch`, `read.capture.*`, `read.session`, `read.master_generation`, `read.cadence`, `read.projection` (`++_nextProjection` advances on every install, Reset included). `read.occurrences[*].sequence` and `saved.construction.revision` stay in.
  - **D2 amended: `WorkshopRead.Motion` is unobservable** (it is not on the wire) and is not published by this story.
  - **D1 amended: binding.** S0/S1 come from the acknowledgement that the driver's `command('save', WorkshopCommand.Save)` consumes (Applied, exactly one arrival, newer sequence and revision than the last consumed one), not from "the last decoded acknowledgement". The acknowledgement carries no command kind, so the frozen "`kind == Save`" check is replaced by that binding.
  - **Goal state:** the authored `goal_*` fields of the saved puzzle compare equal, and no `CCGOAL_SOLVED` appears after the Reset. No new wire field.
  - **No vacuous pass:** each cycle shows a committed read (the Pause acknowledgement, tick > 0) that differs from S0 in at least one compared path before Reset; `snapshotFrom` rejects empty `bodies` and a body count other than the bench's; `assertSnapshotEqual` fails when the path sets differ; the flattener lists the schema fields explicitly and a unit test checks the listing against `schemas/workshop_wire.fbs`.
  - **f32 bits:** every float field (struct and table) is read as its raw u32 with `bb.readUint32` at the field's offset; table scalars also record presence. A unit case compares two different NaN payloads.
- **20 Oct 2026, implementation (Amelia-6).**
  - **D2 amended for the Load leg (coordinator decision on Murdoch-7 F3):** after a reload and Load, the page-local `saved.construction.revision` restarts (observed `21≠3` in the cat-049 spike); it is an identity counter like `projection`, so the Load comparison (`assertLoadRestores`) excludes exactly that one full path; every other path, the committed read included, is bit-equal. The Reset comparison keeps it.
  - **Bench body count** is the committed read's dynamic body count; static parts have no body read.

## Review Triage Log

- **Design review, 20 Oct 2026 (coordinator), 7 items, all accepted:**
  1. Exclusions add `cadence` and `projection`; matched by full top-level path. Disposition: amended D2 (Spec Change Log).
  2. No vacuous pass (tick > 0 differing read per cycle, non-empty and bench-sized bodies, path-set equality, explicit schema listing with a field-count unit test). Disposition: accepted; the per-cycle read is the Pause acknowledgement's committed read, because Save is rejected while Running and the Run and Reset acknowledgements both carry tick 0.
  3. Goal state: equal authored `goal_*` fields and no `CCGOAL_SOLVED` after Reset. Disposition: accepted, no wire change.
  4. Bind S0/S1 to the consumed Save acknowledgement. Disposition: accepted; `command()` returns the entry it consumed (raw bytes and decoded response).
  5. Raw u32 bits for every float, with a two-NaN-payload unit case. Disposition: accepted.
  6. `WorkshopRead.Motion` named unobservable in D2. Disposition: accepted.
  7. Stale line references refreshed against `e36467b8`. Disposition: done (Code Map).
- **Implementation, 20 Oct 2026 (Amelia-6):** the Load-leg comparison (`assertLoadRestores`) ignores `saved.construction.revision`, which a reload restarts (observed `21≠3`). **Resolved by the coordinator (Murdoch-7 F3):** accepted as a D2 amendment for the Load leg only, by full path, because Load legitimately advances it: it is the page-local construction revision (an identity counter like `projection`), assigned by the reloaded page's own Construct sequence, not authored content. The Reset comparison keeps the path.
- **Filed, not in this story's scope:** `tools/e2e/unit/*` runs under no gate (Tier 1 and the runner skip it); M1 (Reset keeps velocities) is caught first by the existing main-thread `ValidateWorkshopRead` guard, so the snapshot proof used a guard-bypassed variant M1b; N1 (held input recounted) survives because the detector latch drops held passages first; the `tools/*.test.mjs` fake hosts still quote the old 26640-byte response ABI (cosmetic); D3 (counter and clock memory in the committed checkpoint) is the owner-flagged scope addition, accepted as in scope by the coordinator (Murdoch-7 F4: AC5 needs it). Murdoch-7 F1, F5, F7 and F8–F12 are filed Lows.

## Design Notes

**Test-first plan.**
1. The unit tests in `run-reset-snapshot.test.ts` fail first.
2. Native tests for committed counter and clock memory fail first.
3. Chrome proof for each suite, through actual UI: build, `s0`, Run, Reset, compare, Run, Reset, compare.
4. Negative controls:
   - the perturbed-bit unit check;
   - a deliberate engine mutation (below) caught in Chrome.
5. Boundaries:
   - −0 against +0;
   - a counter at count = target − 1 before Reset;
   - the clock mid-interval.
6. Save/Load: every suite that already has a Load leg re-snapshots after Load and compares with `s0`.

**Mutation plan.** Each break must turn at least one suite red:
1. `WorkshopSimulation.Reset` keeps the previous `Committed.Bodies` velocities, so every suite fails on `bodies[*].velocity`.
2. Counter memory survives Reset (the committed row is copied, not cleared), so cat-020 fails.
3. The oscillator due tick survives Reset, so cat-017 fails.
4. The gate blade is re-admitted at its last pose, so cat-051 fails.
5. `assertSnapshotEqual` skips the last path, so the unit test fails.
6. Restoring a shared bench in cat-049 test 2 makes it fail when run alone.

**Impacted suites to re-run:**
- Native: `CounterTests`, `ClockTests`, `StateLatchTests`, `ActivationTimerTests`, `ContactActivationTests`, `OrientationSensorTests`, `WorkshopWireFlatBuffersTests`, `WorkshopWireTests`.
- Chrome: all ten listed suites, then the full serial run.

**Risks.**
- The `CCGPU` channel is debt that Story 9A.24 replaces, so keep it behind `captureSnapshot` only.
- Read-size growth touches both codecs, which is the B1 dual path.
- If the Save acknowledgement read ever differs from the Construct acknowledgement read (both are the Build-phase `Committed`; `WorkshopSimulation.Save` at `:188-195` bumps only the revision), D1 still holds because the revision is excluded.

**Refresh after Story 9.4b lands.**
- 9.4b edits `Program.cs` (`CCGPU_RETAINED`), the FlatBuffers vendoring and `WorkshopWireFlatBuffers.cs`, and adds the clock transport (`clock-master.js`, `clock-ring.js`).
- Re-confirm the acknowledgement line and read schema before implementing.
- The all-suites Chrome gate depends on 9.4b AC4: cat-023b's clock-recovery fault is NOT MET and needs an owner decision.
