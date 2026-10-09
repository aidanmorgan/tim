---
title: 'Story 6.1c: ENGINE-F32-VELOCITY — f32 committed velocity and angular velocity'
type: 'refactor'
created: '2026-10-09'
status: 'done'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: '520f778'
context:
  - '{project-root}/_bmad-output/implementation-artifacts/spec-6-1b-engine-drag-application.md'
  - '{project-root}/AGENTS.md'
  - '{project-root}/docs/gpu-f32-physics.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Committed body velocity and angular velocity are stored as binary16 between ticks, so per-tick decrements smaller than half a binary16 step are erased: declared ball drag 0.04 does nothing in flight at 120/240 Hz, and rolling resistance stalls for balls at ≥ ~4.5 m/s at 240 Hz. The owner chose (9 Oct 2026) to fix this now with IEEE-754 f32 storage, per AGENTS.md's f32 numeric authority.

**Approach:** Store committed linear velocity (m/s, unscaled) and angular velocity (rad/s) as f32 in the body record, the motion piece and the per-body response record, and type them as `float` in C#, repacking within the existing 128-byte body record and motion piece. Delete the binary16 velocity lanes, their ×32 scale and their Half bounds. Pose (position remainder, quaternion) and declaration coefficients stay binary16 (later migration rows).

## Boundaries & Constraints

**Always:**
- Body record stays 128 B: v f32 48..60, ω f32 60..72; repack mass/drag/gravity/COM into 72..88 (principal frame, inertia, collider slot keep 88/96/104/112/120); nothing after the body table moves (colliders 4352 etc. unchanged). Motion piece stays 128 B with v/ω f32 and the remaining lanes repacked. `PhysicsBodyWire` grows to carry f32 v/ω (computed response offsets cascade in C#; the worker's pose-ring writer uses the new stride).
- Units: velocity stored in m/s (×32 scale deleted), bounds |v| ≤ 64 m/s, |ω| ≤ 128 rad/s checked on f32 values; worker clamp headroom reduced to what f32 needs.
- `CanonicalBody` (construction/save wire) keeps its 80-byte size; velocity lanes become f32 and must be all-zero at admission, so existing construction and save bytes stay valid (zero is all-zero bits in both formats).
- Every layout change updates C# ABI, validation (padding masks, immutable ranges), worker offsets, the Node harness fixture and all pinning tests together; reject malformed f32 (non-finite, out of bounds) explicitly.
- Proofs: harness facts that declared drag 0.04 slows a flying ball at 60/120/240 Hz (every tick decreases), a 5 m/s rolling ball decelerates every tick at 240 Hz and rests, each mutation-sensitive; all prior suites pass serially in Chrome; update `docs/gpu-f32-physics.md` migration-status (remove the Story 6.1c row, fix the line-12 present-tense WASM claim) and the "Committed Velocity Resolution" envelope row; close the deferred-work entries for drag survival.

**Never:**
- Change pose, declaration coefficient formats, table capacities, the SharedArrayBuffer pose ring layout (already f32), or save semantics; no dual-format reader, compatibility shim or version branch for binary16 velocity.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Drag in flight | Ball 2 m/s, drag 0.04, gravity 0, 60/120/240 Hz | v(t) ≈ 2·e^(−0.04t) within 1%; every tick decreases | N/A |
| Fast roll | Ball 5 m/s rolling, 240 Hz | Decelerates every tick ≈ (5/7)·Crr·g and rests | N/A |
| Bounds | Committed |v| > 64 or |ω| > 128 or NaN | Candidate rejected by host | Typed rejection |
| Save/Load | Pre-6.1c saves | Load unchanged | N/A |
| Regression | All prior suites | Pass serially in Chrome | N/A |

</frozen-after-approval>

## Code Map

- Worker `CuriousContraptions.Simulation/wwwroot/worker.js`: clamps `:210-214` (SPEED/SPIN_CLAMP 1/512 headroom), hard-coded offsets `:926-927,938,940,1060,1423`, body read `:978-1003` (v `getF16(+48..52)*32`, ω `+56..60`, mass +64, drag +66, gravity +68..72, COM +80), drag `:1263-1271`, motion-piece write `:1383-1415` (v/32 +64..68, ω +72..76, gravity +80..84), commit `:1530-1537`, `writePoseRing :1542-1610` (stride `128 + i*56`, v Half at +26..30 ×32).
- `engine/gpu/PhysicsGpuAbi.cs:20-44` constants, admission `:64-82`, `ReadDynamicBodies :207-215`, `ValidateCandidate :332-333` (immutable `[..16]`/`[64..]`, padding 28-32, 38-40, 54-56, 62-64), `ReadMassProperties :423-425` (COM +80). Free bytes today: 74-80, 124-128.
- `engine/gpu/CanonicalBody.cs:13,36-54,82-91` (`CellVelocity(Half)` unit 32 m/s, 80-byte encode with v at +64..70), `BodyRecordVersion.CanonicalHalf=2`.
- `engine/gpu/PhysicsDeclarations.cs:22,57-75` (`AngularVelocity(Half)`, 128/16384 bounds, static IsPositiveZero); `engine/gpu/WorkshopConstruction.cs:246` (HalfBits.IsPositiveZero(CellVelocity)); `engine/gpu/PhysicsBodyReadSet.cs:9-31,88-101`; `engine/gpu/PhysicsBodyWire.cs` (56 B, v +26..30, ω +40..44) used by `WorkshopWire.cs:32,162,201,206`, `WorkshopTraceRecord.cs:16,30`; `engine/gpu/PhysicsMotionRead.cs:81-99,130-132,167,176` (padding masks, Half `Norm`); `engine/gpu/WorkshopPoseRing.cs:81-83` (ring already f32; only conversion changes); `WorkshopWire.cs:367-369` (rejects non-zero velocity in construction).
- Tests pinning layouts: `WorkshopWireTests.cs:308-325,698,805-813`, `WorkshopReadTests.cs:70`, `WorkshopSimulationTests.cs:301,374,393-394`, `RigidMassPropertiesTests.cs:111-125`, `WorkshopPoseRingTests.cs:46,52`, `WorkshopDominoTests.cs:23-24`, `WorkshopBowlingTests.cs:134` (drag +66 moves), `CanonicalBodyTests.cs:48-50`, `OrientationSensorTests.cs:40-48`, `ActivationTimerTests.cs:112`; harness `tools/workshop-rigid-body.test.mjs:12-14,121-124,161-162,377-379`; mocks `tools/workshop-observation.test.mjs:45,166,199` (`ResponseAbi`).
- Out of scope but stale on the old layout (already listed for deletion): `tools/GpuBodyFixture`, `engine/gpu/body-integration.wgsl`, `basketball.wgsl`.

## Tasks & Acceptance

**Execution:**
- [x] Harness facts first (drag-in-flight per cadence, 5 m/s roll at 240 Hz) — failing on the current worker
- [x] `PhysicsGpuAbi.cs`, `PhysicsMotionRead.cs`, `PhysicsBodyWire.cs`, `CanonicalBody.cs`, `PhysicsDeclarations.cs`, `PhysicsBodyReadSet.cs`, `WorkshopConstruction.cs`, `WorkshopPoseRing.cs`, `WorkshopWire.cs`/`WorkshopTraceRecord.cs` — f32 v/ω types, layouts, bounds, padding masks
- [x] `worker.js` — f32 read/commit/motion piece/response stride; clamps; offsets
- [x] Tests and harness fixture updated; mocks updated
- [x] Docs: migration-status row removed, envelope row, line-12 claim, engine-contracts; deferred-work closed; TODO; sprint status

**Acceptance Criteria:**
- Given declared drag 0.04 at 60/120/240 Hz, a flying ball slows every tick as e^(−0.04t).
- Given a 5 m/s rolling ball at 240 Hz, it decelerates every tick and rests.
- Given malformed committed velocity, the host rejects the candidate.
- All prior suites pass serially in Chrome; harness and C# green; no binary16 velocity lane remains (grep `CellVelocity(Half`, `getF16(view, b + 48` style reads).

## Implementation Notes

- 2026-10-09 planning: Checkpoint 1 self-approved under the owner's standing autonomous instruction (the owner chose this slice). Decisions: store m/s unscaled; repack within 128-byte records; keep drag/Crr coefficients binary16 (remaining migration row).
- 2026-10-09 implementation (Amelia). **Decisions.** (1) C# types: `CellVelocity(Half)` (unit 32 m/s) is deleted and replaced by `LinearVelocity(float X, Y, Z)` in m/s (`CanonicalBody.cs`); `AngularVelocity` becomes `float` rad/s. Each owns its bound (`MaximumSpeed` 64 / 128), `Validate()` (finite components and vector magnitude, wide arithmetic, via the new `PhysicsDeclarationBounds.Magnitude`), `IsPositiveZero` and `HasSameBits` (new `F32Bits` helper next to `HalfBits`; `HalfBits.IsPositiveZero(CellVelocity)` is deleted). `CanonicalBody.Validate` now checks the velocity vector (the per-axis Half ±2-cell check is gone), so the separate vector check in `PhysicsGpuAbi.ReadDynamicBodies` and the velocity/angular squared checks in `RigidBodyDeclaration.Validate` were deleted as duplicates; `PhysicsBodyRead.Validate` calls `AngularVelocity.Validate()`. Static declarations and constructions require all-zero velocity bits (−0 rejected). (2) `PhysicsGpuAbi` gains named body offsets (`BodyVelocityOffset` 48, `BodyAngularVelocityOffset` 60, `BodyMassOffset` 72, `BodyDragOffset` 74, `BodyGravityOffset` 76, `BodyCentreOfMassOffset` 82); `ValidateCandidate` compares `[..16]` and `[72..]` and requires zero padding 28..32 and 38..40 (the old 54..56 / 62..64 masks vanish with the Half lanes). `PhysicsMotionRead` gains `VelocityOffset`/`AngularVelocityOffset`/`GravityOffset`/`SupportedAccelerationOffset`/`AngularAccelerationOffset`; f32 vectors are validated by `F32Norm` (finite, ≤ 64 m/s / 128 rad/s; all-zero skipped). (3) Worker: `getFloat32`/`setFloat32` for body read, commit and motion piece; pose-ring writer reads the 64-byte wire stride (rotation +26, velocity f32 +40, no ×32); clamp headroom `1 − 2^-20` (f32 rounding moves a magnitude by ≤ 2^-24 relative); the clamp is factored into `clampVelocity` and also applied after the relax sweeps and restitution pass, because the reduced headroom no longer absorbs a post-clamp overshoot (game-grade clamp-or-continue rather than a host rejection; pinned only by inspection, recorded in deferred-work). (4) The unread, unvalidated motion-piece bytes 104..110 (a legacy "residual" hole the worker never wrote and C# never read; only the test `ForceDrivenMotionSamplesQuadraticAndAcceptsGameGradeResiduals` wrote into it) now hold the validated angular acceleration; that test is renamed `...AndBoundsItsAccelerations` and checks the out-of-domain and free-motion rejections instead. (5) `BodyRecordVersion.CanonicalHalf` (= 2) is unchanged: pose stays Half and construction/save bytes are identical, so no version bump.
- **Final byte layouts.** Body record (128 B): id 0..8 (u64), motion 8..12 (u32), 12..16 zero, cell 16..28 (i32×3), 28..32 zero, local remainder 32..38 (Half×3), 38..40 zero, rotation 40..48 (Half×4), **linear velocity m/s 48..60 (f32×3), angular velocity rad/s 60..72 (f32×3)** [mutable 16..72], mass 72, linear drag 74, gravity 76..82, local COM 82..88 (Half), principal frame 88..96 (Half×4), principal inertia 96/104/112 (Half mantissa, 2 zero, i32 exponent), collider slot 120 (u32), 124..128 zero. Motion piece (128 B): kind 0, start/end/anchor ordinals 4/8/12 (u32), phases 16/18/20 and rate 22 (Half), body id 24..32, COM cell 32..44 (i32×3), 44..48 zero, COM remainder 48..54, drag rate 54..56, rotation 56..64 (Half), **linear velocity m/s 64..76 (f32×3), angular velocity rad/s 76..88 (f32×3)**, gravity 88..94, 94..96 zero, supported acceleration 96..102, 102..104 zero, angular acceleration 104..110, 110..112 zero, bounded lane 112..118 (Half, zero unless flag), 118..120 zero, flag 120 (u32 ≤ 1), 124..128 zero. Per-body response/trace record `PhysicsBodyWire` (64 B, was 56): id 0..8, cell 8..20, local 20..26, rotation 26..34, local COM 34..40 (Half), **linear velocity 40..52 (f32×3), angular velocity 52..64 (f32×3)**; no reserved bytes. Response: bodies at 128 + i·64, `ReadCapturesOffset` 1152 (was 1024), `ResponseBytes` 24080 (was 23952); trace record 48 + 64·bodies + 24·captures. `CanonicalBody` (80 B): velocity f32 at 64..76, zero 44..48, 62..64, 76..80.
- **Files touched.** `CuriousContraptions.Simulation/wwwroot/worker.js` (sha256 8c56cc4d1b877e16…), `engine/gpu/{CanonicalBody,PhysicsDeclarations,PhysicsBodyReadSet,PhysicsBodyWire,PhysicsGpuAbi,PhysicsMotionRead,WorkshopConstruction,WorkshopPoseRing,WorkshopWire}.cs`; tests `CuriousContraptions.tests/{CanonicalBodyTests,RigidMassPropertiesTests,WorkshopWireTests,WorkshopReadTests,WorkshopSimulationTests,WorkshopPoseRingTests,WorkshopDominoTests,WorkshopBowlingTests,WorkshopTraceTests}.cs`; harness/mocks `tools/workshop-rigid-body.test.mjs` (sha256 a15effd4ae32c0f1…), `tools/workshop-observation.test.mjs`, `tools/workshop-client-lifecycle.test.mjs`; docs `docs/gpu-f32-physics.md`, `docs/general-engine-design.md`, `docs/planning/invest/vertical-delivery.md`, `README.md`, `_bmad-output/implementation-artifacts/{deferred-work.md,sprint-status.yaml}`, `TODO.md`. `WorkshopTraceRecord.cs` needed no change (it sizes by `PhysicsBodyWire.ByteLength`); `docs/engine-contracts.md` has no layout or precision statement to change (its body row says "pose/velocity" generically). Legacy deleted: binary16 velocity lanes in body record, motion piece and response wire; the ×32 velocity scale (worker, `PhysicsMotionRead`, `WorkshopPoseRing`, pose-ring writer); `CellVelocity`; `HalfBits.IsPositiveZero(CellVelocity)`; the Half velocity bounds and the duplicate vector checks; the 1/512 Half clamp headroom; the motion-piece residual hole.
- **Harness.** Fixture writes f32 v/ω at +48/+60, mass +72, drag +74, gravity y +78; `readBody` reads f32; `ResponseAbi` mocks 24080. New facts: (a) declared drag 0.04, 2 m/s, zero gravity, 60/120/240 Hz: every committed tick strictly below the previous and within 1% of 2e^-0.04t; spin exactly 5 rad/s (measured after 2 s: 1.846201 vs 1.846233, −1.7e-5 relative at all three cadences); (b) Basketball rolling at 5 m/s at 240 Hz from x = −30: strictly decreasing committed v_x on every tick until rest, deceleration over 1–3 s 0.3670 m/s² vs (5/7)(Crr·g + drag·v) 0.3667 (10% bound), rests at 15.98 s (closed form 16.07 s; bound 17.5 s), never reverses, rests at radius height (x ≈ 7.1). Test-first: both failed on the unchanged worker (60 Hz tick 59 1.94238 vs 1.92286 m/s; the 5 m/s ball never rested). Mutants (scratch copies of the final worker via `WORKER_SOURCE`): velocity and spin committed on the binary16 grid → both new facts and the 0.125 drag fact fail (3 fail); drag removed → both new facts plus both 0.125 drag facts fail (4); rolling row removed → the 5 m/s fact plus the 7 earlier rolling facts and the two-lane fact fail (9); the real worker passes 36/36. The 0.125 drag fact is tightened from 3% to 1%.
- **Surprises.** (a) `orientation sensor: a tile placed 10 deg off upright` sampled "settles upright (< 2°)" at 2 s while the tile is still rocking ±3°: binary16 1.97°, f32 2.03° at tick 240, the same trajectory to 3 significant figures (traces compared with the baseline worker); the run is now 4 s (0.27° final), still asserting it never fires. (b) The rolling non-reversal fact asserted `max ω_z <= 0` exactly; f32 keeps solver residue of 8.8e-25 rad/s at rest that binary16 flushed to zero, so the bound is now 1e-6 (the bang-bang mutant reverses by 3.3e-4). (c) Response byte length is pinned in three JS mocks (`23952`), not computed; updated to 24080. (d) Process slip: while comparing workers I ran `git stash -- worker.js`, which stashed my worker edits; popped immediately and verified by diff and hash (no content lost). `TODO.md` and `research-e2e-speed.md` were also being changed by another party during the session; I did not edit `research-e2e-speed.md`.
- **Evidence (implementer, not a review verdict).** `node --experimental-vm-modules --test tools/workshop-rigid-body.test.mjs tools/workshop-observation.test.mjs tools/workshop-pose-ring.test.mjs` 38/38; other Node suites (`workshop-client-lifecycle`, `workshop-save-storage`, `workshop-isolation`, and `workshop-animation-output` with `--experimental-vm-modules`) all pass; `anvil check --changed` 0 warnings (4 script files) and `anvil_check` 0 over all changed source/test/script/doc files; `dotnet test CuriousContraptions.slnx` 632/632 (622 + 10 new; the 5 pre-existing xUnit2013 warnings in untouched `WorkshopActivationAnimationTests.cs`); `dotnet publish CuriousContraptions.web` exit 0 and the worker served on :8060 is byte-identical to the source (sha256 8c56cc4d…, `else clampVelocity(b)` present in the compressed response); cumulative `node --test --test-concurrency=1 --test-timeout=150000 tools/e2e/*.test.ts` 15 suites / 54 tests, 54 pass, 0 fail, 0 cancelled, one serial Chrome run (736 s) against that worker, including `cat-014` (both balls rest, Save/reload/Load), CAT-023a/b Domino suites, ENGINE-CORE-2a3 ramps and 2c Receiver capture. An earlier cumulative run against the pre-`clampVelocity` worker was stopped before completion and is not evidence. No Chrome negative control (old worker in the bundle) was run. `grep CellVelocity` finds only historical `docs/work-orders/P0-004` JSON; no `getF16` read of body +48..60 remains. Not committed or pushed.

## Spec Change Log

## Review Triage Log

Pass 1 (2026-10-09). Murdoch: Pass (scoped); SnapshotApproval for a local commit granted (diff 5569dcda…, worker 8c56cc4d…). Harness 38/38 (drag-in-flight and 5 m/s roll facts fail under the binary16-commit and drag-removed mutants); other Node suites 39/39; 632/632; anvil 0; publish exit 0; cumulative 15 suites / 54 tests first attempt. Adjusted pre-existing facts judged legitimate (10° tile phase luck at 2 s; 8.79e-25 rest residue). Probes: improved precision, no regressions; CAT-014 lane outcomes identical in 40/40 lanes.

Non-blocking findings routed to follow-up Story 6.1d (velocity hardening), so this verified snapshot can be committed:

| # | Finding (layer) | Verdict | Route |
|---|---|---|---|
| 1 | Clamp fact tolerance 64.01/128.01 cannot catch commits the host rejects; no-headroom mutant commits >64 in ~half of probes yet passes (VG) | medium | 6.1d (exact f64 sum-of-squares bound) |
| 2 | 5 m/s fact asserts exact never-reverses (flaky against f32 rest residue) and checks v[0] only (BH, EC) | low | 6.1d |
| 3 | Motion-piece drag rate lane (+54) never written by the worker, so interpolated poses ignore drag (BH) | medium | 6.1d (write or delete as legacy) |
| 4 | `WorkshopTraceVersion` unchanged though body payload 56→64 B; `BodyRecordVersion.CanonicalHalf` name wrong (BH, M-I2) | low | 6.1d |
| 5 | Second clamp untested and undocumented; envelope "every speed" claim demonstrated only at 0.7/1/5 m/s (BH, VG, M-L1/L2) | low | 6.1d (docs + boundary facts) |
| 6 | COM read at +82 unpinned (no collider offset yet) (VG) | low | 6.1d |
| 7 | Worker hard-codes body/response offsets instead of deriving from ResponseAbi/PhysicsGpuAbi (BH, EC) | low | 6.1d (offset check test) |
| 8 | Missing positive commit-path test for in-envelope f32 velocity; host-bound edge tests for body wire (EC, BH) | low | 6.1d |
| 9 | Non-finite guard sum overflow zeroes a finite huge velocity instead of clamping (EC) | low | 6.1d |
| 10 | research-e2e-speed.md rewrite in the review diff (BH) | — | committed separately |

## Verification

**Commands:**
- `node --experimental-vm-modules --test tools/workshop-rigid-body.test.mjs tools/workshop-observation.test.mjs tools/workshop-pose-ring.test.mjs` -- expected: all pass
- `anvil check --changed` -- expected: 0 warnings
- `dotnet test CuriousContraptions.slnx` -- expected: 100%
- `dotnet publish CuriousContraptions.web` -- expected: exit 0
- `node --test --test-concurrency=1 --test-timeout=150000 tools/e2e/*.test.ts` -- expected: all suites 100%
