---
title: 'Story 9R.5b: Every Physics Table Sized at Run (No Fixed Capacities)'
type: 'bugfix'
created: '2026-10-20'
status: 'review'
route: 'dispatch'
context:
  - '{project-root}/AGENTS.md'
  - '{project-root}/_bmad-output/planning-artifacts/epics.md#story-9r5b-every-physics-table-sized-at-run-no-fixed-capacities'
  - '{project-root}/_bmad-output/implementation-artifacts/spec-9r-2-collider-table-sized-at-run.md'
---

## Story text (verbatim, coordinator's epics.md, 20 Oct 2026)

> *Status: Backlog. Runs after Story 9R.5. Coordinator, 20 Oct 2026, under the owner's rule that there are no arbitrary engine caps. Amelia-9 found that the Impact Switch is limited to 8 by the fixed physics trigger table.*
>
> **Acceptance Criteria:**
> - **Given** a machine with any number of trigger-bearing parts up to the ordinary Free-palette maxima (Impact Switches and every other trigger user), **When** Run compiles it, **Then** the physics trigger table is sized from the compiled scene. The 9R.2 collider table and the 9R.4 activation slots are the pattern.
> - **And** Build never refuses a machine for its trigger count.
> - **And, by owner decision of 20 Oct 2026, the physics body table has no limit.** In the owner's words: "the physics body table needs to be infinite why would you restrict it".
>   - The body table, and every other fixed-capacity physics or worker table, is sized from the compiled scene at Run. That covers bodies, joints, prismatics, sensors, contact-work rows, electrical rows, motion and occurrence buffers.
>   - Memory is the only bound.
>   - The story begins with an audit that lists every fixed capacity constant in `engine/gpu`, the Simulation and Animation workers, and the wire. Each one is either removed or justified by a written requirement. "Historical" doesn't count as a justification.
> - **And** the per-part Build limit (32 instances) is kept only if a requirement states it. The goal is an "unlimited Free Workshop" (TODO.md). Otherwise it is removed, and the e2e and native benches prove a machine beyond it compiles, steps, Resets exactly and Saves/Loads.
> - **And** the fixed cap constant and its wording are deleted.
> - **Proof:**
>   - red today when a 9th Impact Switch is placed;
>   - a bench at the palette maximum of Impact Switches, where the last switch fires its own lamp, Reset is exact and Save/Load works;
>   - a mutant that restores a fixed cap fails.
> - **And** Tier 1 passes, plus the runner's Tier 2 selection.

Coordinator brief additions (20 Oct 2026): the fixed-size worker response buffer (wire sections capped at 32) is in scope; allocate per Run, not per tick, where possible; one Tier 3 full run (material engine change); new e2e suites are SerialAlone.

## Audit (every fixed capacity constant, base `c5336092`)

Population tables scale with the machine and are **removed**. Structural, temporal or protocol bounds are **kept** only with the written source named.

| # | Constant (file) | Value | Disposition |
|---|---|---|---|
| 1 | `WorkshopInstances.Capacity` (WorkshopInstances.cs) | 32 | **Removed.** No requirement states it; TODO.md's goal is an unlimited Free Workshop. Deletes `WorkbenchTable.Instances` and its "part table" wording. |
| 2 | `PhysicsSceneDeclaration.BodyCapacity` / `MaterialCapacity` | 33 / 33 | **Removed.** Owner, 20 Oct: "the physics body table needs to be infinite". |
| 3 | `PhysicsBodyReadSet.Capacity` (dynamic bodies) | 16 | **Removed**, with every derived table (`PresentedBodySet`, `OrientationSensorCapacity`, motion, occurrences, pose history scratch). |
| 4 | `SensorCapacity` / `GuideCapacity` | 16 / 16 | **Removed.** The one-guide-per-dynamic-body rule (two Receivers with a ball) is a document invariant (`PhysicsSceneDeclaration` admits one guide per body), not a capacity: **kept**, unchanged wording. |
| 5 | `TriggerCapacity` | 8 | **Removed** (the story's trigger). |
| 6 | `ContactWorkCapacity` | 8 | **Removed.** |
| 7 | `PrismaticCapacity` | 16 | **Removed.** |
| 8 | `ContactLoadSensorCapacity` (= instances) | 32 | **Removed.** |
| 9 | `ElectricalSupplyPlan.SourceCapacity` | 8 | **Removed.** |
| 10 | `WorkshopConnections.Capacity`, `ElectricalSupply.LoadCapacity` | 8 | **Removed** (wire, binding and load tables). |
| 11 | `PhysicsContactWorkRead.OccurrenceCapacity`, `ContactWorkRead.MaximumOccurrences` | 128 / 1601×16 | **Removed.** The occurrence table is works × dynamic bodies of the compiled scene; the per-owner sequence is a `uint` counter. |
| 12 | `PhysicsMotionRead.Capacity` | 16×8 | **Removed.** Sized as dynamic bodies × 8. The 8 is the largest admitted substep count per tick (`PhysicalStepProfile` 480 Hz at the 60 Hz cadence, `docs/gpu-f32-physics.md` rates): structural. |
| 13 | `WorkshopWire` construction width (32 × 160 + 8 × 32) and `ResponseBytes` sections (16 bodies, 16 captures, 32 activation/timer/memory rows, 8 works, 128 occurrences, 8 sources) | fixed | **Removed.** Both are length-prefixed by their own u32 counts; the Simulation's response buffer is reallocated only when a world's counts change (per Run). Byte-wide response counts (a hidden 255 cap) widen to u32. |
| 14 | `WorkshopPoseRing.BodyCapacity` and JS `POSE_BODY_CAPACITY`/`POSE_RING_BYTES` | 16 / 3120 | **Removed.** The ring's body capacity is its own length; the worker grows it at Run and posts the new buffer. `SlotCount = 3` **kept**: AGENTS.md "triple-buffered pose ring". |
| 15 | `WorkshopAnimationWire.TargetCapacity` (and Animation Program fixed arrays) | derived | **Removed.** The track table grows when a new target arrives. |
| 16 | `PresentedBodySet`, `PhysicsCaptureRead` `[InlineArray]` storage and every `byte Count` population (body, capture, contact work, electrical, activation, timer, memory, presentation `PartCount`, `PositionWrites`) | 16 / 255 | **Removed** (exact arrays, `int` counts). |
| 17 | `PhysicsGpuAbi.ShaderPreamble` capacity lines | derived | **Removed.** No shader consumes the preamble (`BrowserGpuTransport.Initialize` ignores it, 9R.2 D3). |
| 18 | `PhysicsGpuAbi.PrismaticRowCount`, `PrismaticRows.Count` | 8 | **Kept.** Rows of one prismatic joint (2 transverse, 3 angular, axial spring, 2 stops): structural, `docs/gpu-f32-physics.md` constraint rows. |
| 19 | `WorkshopCollisionKernels` clip polygon `PolyVertex[16]` | 16 | **Kept.** Box–box clipping yields at most 8 vertices (geometry). |
| 20 | `CanonicalBody.MaximumCell` | 1024 | **Kept.** Spatial domain (game-grade envelope), not a population. |
| 21 | `WorkshopPoseHistory.Capacity` | 7 | **Kept.** Interpolation history depth in samples (time), not a population. |
| 22 | `WorkshopAnimationWire.ImpulseCapacity`, `AnimationImpulseDefinition.MaximumCapacity` | 64 / 65536 | **Kept.** Concurrent overlapping impulses on one target, bounded by its declared cooldown (time). |
| 23 | worker.js `occurrenceReadCapacity` | 64 | **Kept.** Back-pressure depth of the occurrence-read queue (pending messages, time), not a scene table. |
| 24 | `WorkshopTrace.Capacity`, `PoseByteCapacity` | 7201 | **Kept.** One record per tick for the 60 s run limit (`RunTickLimit`), time. |
| 25 | `EventRingBuffer.DefaultCapacity` | 1024 | **Kept.** Message queue depth, not a scene table. |
| 26 | Clock limits (`WorkshopClockMapping.ProbeCapacity`, `StartupAttemptLimit`, `CheckExchangeLimit`, ...) | various | **Kept.** Clock protocol (Story 9.4b), not a scene table. |
| 27 | `WorkshopCounter.PlacedTargetCount`, `WorkshopPresentationWire.Capacity` (retained diagnostic frames) | 3 / 32 | **Kept.** A Counter's default target value, and a diagnostic frame history (time). |

## Decisions (coordinator delegation)

- **D1. One per-world layout.** `PhysicsWorldLayout` derives every table offset and the exact world length from the world header's counts. The header grows from 128 to 144 bytes and carries the prismatic and contact-load counts (their in-band 16-byte table headers are deleted). With exact tables, the "unused slots stay zero" checks become empty and are deleted. `PhysicsStateVersion` 12 → 13.
- **D2. Derived tables.** Work occurrences = contact works × dynamic bodies; motion pieces = dynamic bodies × 8. Both come from the compiled scene at admission.
- **D3. Variable-length wire.** Construction = header + count × 160 + links × 32. Response header grows to 160 bytes with u32 counts; the sections follow back to back. `WorkshopWireVersion` 20 → 21. The Simulation keeps one response buffer, reallocated only when the read's counts change (once per Run).
- **D4. Pose ring sized at Run.** The ring's body capacity is `(length / 3 − 16) / 64`. When a read holds more dynamic bodies than the ring, the worker allocates a larger `SharedArrayBuffer` and posts it to the main thread before the read; postMessage ordering means the main thread holds the new ring before it receives that read. Nothing is truncated.
- **D5. Per-Run scratch.** Per-tick `stackalloc` sized by a removed capacity becomes an array owned by its engine/device/client and resized at admission (allocation per Run, not per tick).
- **D5a. Hidden width caps found during implementation, also removed.**
  - Work-occurrence work and collider slots were u16 (a 65,536-collider ceiling the old 32-part budget hid). They are u32 in the world record (40 bytes), the B1 wire and the FlatBuffers struct (still 40 bytes: the u16 pair plus its 4-byte pad become two u32s; the struct section of the generated C# and TS is flatc 25.12.19's output for the edited schema).
  - The worker refused commands over 65,536 bytes (`CommandAbi`); a construction command is as wide as its machine, so only the minimum stays.
  - The presentation diagnostic's part and position-write counts were bytes (`checked((byte)_parts.Count)` faulted the presentation at 256 parts): u32, record 352 → 360 bytes.
  - The Animation worker's latest-sample map refused new targets past `TargetCapacity`; the worker's track table and `AnimationBatch` now grow (`EnsureCapacity`).
  - `WorkshopConnections.Validate` refused a ninth wire (`WorkshopEditRejection.ConnectionCapacity`, deleted).
  - `ActivationNetwork.Consume` refused a tick whose fanout passed 4 × nodes ("Activation fanout capacity exceeded."); its queue grows.
  - The client's "Contact feedback history exhausted" bound (8 × 25,616 pulses) is deleted.
- **D6. Proof bench.** Free play is Unlimited, so "palette maximum" has no number: the bench is **40 Impact Switches + 1 Signal Lamp + 1 Basketball** (42 instances: beyond the old 32 instances and 8 triggers), with the last switch wired to the lamp and the ball dropped on it.

## Acceptance (observable)

1. Native, real compile + `WorkshopSimulation` Advance: one test per removed table that exceeds its old cap by one compiles and steps (red on base: Build or compile refuses).
2. Native: the 40-switch bench lights its lamp from the last switch only; Reset restores the Build read exactly; Save/Load round-trips and replays bit-equal.
3. Native: one combined bench exceeds every removed cap at once and runs, Resets and Save/Loads.
4. Wire: construction and response round-trip (binary and FlatBuffers) beyond the old caps; worker ABI pins updated.
5. Chrome `tools/e2e/capacity-9r5b.test.ts` (SerialAlone): the 40-switch bench is placed through the drawer, the last switch lights the lamp, Run/Reset use the 9R.1 snapshot, Save/Load restores it; no worker fault.
6. Mutants: restoring any one cap fails a named test.
7. Tier 1, Tier 2 selection, and one Tier 3 full run pass.
