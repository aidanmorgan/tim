---
title: 'Story 6.1d: Velocity hardening (follow-up to Story 6.1c review)'
type: 'refactor'
created: '2026-10-09'
status: 'done'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: '33f44a8'
context:
  - '{project-root}/_bmad-output/implementation-artifacts/spec-6-1c-f32-committed-velocity-precision.md'
  - '{project-root}/AGENTS.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** The Story 6.1c review (Pass) left non-blocking gaps: the clamp test cannot catch commits the host rejects, interpolated poses ignore drag because the motion-piece drag lane is never written, the trace format changed without a version bump, the worker hard-codes layouts the C# ABI owns, and several new paths (second clamp, COM at +82, commit-path acceptance, huge-finite guard) are unpinned.

**Approach:** Close each gap with the smallest change: exact host-bound tests, write (or delete) the motion-piece drag lane so between-tick poses match the committed physics, bump/rename the trace and body-record versions, derive or check worker offsets against the C# ABI, and add the missing boundary/positive facts. No behaviour change beyond drag in interpolated poses.

## Boundaries & Constraints

**Always:**
- Clamp fact asserts the exact f64 sum of squares of committed f32 components ≤ 64² and ≤ 128² over several directions (must fail with the headroom removed).
- 5 m/s roll fact uses the same −1e-6 never-reverses tolerance as the 0.7 m/s fact and also checks spin decelerates.
- Motion piece +54 drag rate: either the worker writes the declared drag so `PhysicsMotionRead.Position`/`TrySample` interpolate with it (preferred; add a fact that a sampled mid-tick pose matches the drag-decayed trajectory), or the lane and its reader are deleted as legacy — pick one, no dual path.
- Bump `WorkshopTraceVersion` (body payload now 64 B) and rename `BodyRecordVersion.CanonicalHalf` to reflect f32 velocity; unknown versions rejected explicitly; no shim.
- Worker body/response offsets checked against C# (`PhysicsGpuAbi`, `PhysicsBodyWire`, `ResponseAbi`) by a test that fails on drift (or passed through ResponseAbi).
- Facts/tests: second clamp (construct a scenario that reaches it, or document it as defensive and pin it with a direct unit-level worker call), COM read at +82 with a non-zero offset, positive `ValidateCandidate` with in-envelope f32 velocity, body-wire bounds just above 64 / 128, guard that a finite huge velocity is clamped (not zeroed) — fix the guard if needed.
- Docs: "Maximum Velocities" row describes both clamps and the 2^-20 headroom; envelope "every speed" claim backed by boundary facts or narrowed.

- Owner decision 2026-10-09: fold the first e2e speed-up slice from `research-e2e-speed.md` into this story — replace fixed `waitForTimeout`/settle sleeps in `tools/e2e/workshop-driver.ts` with condition waits on existing signals (rAF frame, pose-ring sequence/body-set change, IndexedDB save slot, `CCGOAL_SOLVED` console line); remove the redundant first-test `reload()` after launch and `selectLevel('free_workshop')` after a reload; make `selectLevel` wait for the new construction to be installed (fixes the race where readiness is already true). Every assertion, negative control, Save/Load reload and real-time physics window is kept; target ≈ 731 s → ≈ 520 s serial.

**Never:**
- Change committed layouts, units, bounds, or gameplay behaviour other than interpolated-pose drag; no new solver features.
- Delete e2e coverage, weaken assertions, fast-forward physics, use setters or mock the worker. Exception (owner, 9 Oct 2026): duplicate recipes may be merged into one shared test that keeps the union of assertions and carries every original requirement ID.

</frozen-after-approval>

## Code Map

- Findings and evidence: `spec-6-1c-f32-committed-velocity-precision.md` Review Triage Log #1–#9; reviewer scratch logs under `/private/tmp/claude-501/-Users-aidan-dev-personal-tim/537235c4-e933-45de-99f1-70c27b375e0c/scratchpad/{vg61c,murdoch14}/`.
- `tools/workshop-rigid-body.test.mjs:319-325` (clamp fact), `:717` (5 m/s fact), fixture COM write; `CuriousContraptions.Simulation/wwwroot/worker.js:213-214` (clamps), `:1352-1356` (second clamp + guard), `:1383-1415` (motion piece write; +54 unwritten), `:1568-1584` (pose-ring stride/offsets); `engine/gpu/PhysicsMotionRead.cs` (`Position` reads `H(p,54)`), `engine/gpu/WorkshopTraceRecord.cs` (`WorkshopTraceVersion`), `engine/gpu/CanonicalBody.cs` (`BodyRecordVersion.CanonicalHalf`), `engine/gpu/PhysicsGpuAbi.cs`, `engine/gpu/PhysicsBodyWire.cs`; tests `WorkshopWireTests.cs:540-544,824-848`; docs `docs/gpu-f32-physics.md` Maximum Velocities and envelope rows.

## Tasks & Acceptance

**Execution:**
- [x] Harness: exact clamp bound; 5 m/s tolerance + spin; COM +82; second-clamp/huge-finite guard facts (mutation-sensitive)
- [x] Motion-piece drag lane written (or deleted) + interpolation fact
- [x] Version bump/rename with rejection tests
- [x] Worker offset drift check against C# ABI
- [x] C#: positive candidate commit test; body-wire bound edges
- [x] Docs, deferred-work, TODO, sprint status

**Acceptance Criteria:**
- Each new fact fails under its matching mutant (no-headroom, no-drag-lane, old-offset, zero-on-huge) and passes on the final worker.
- All prior suites pass serially in Chrome; harness, Node and C# green; anvil 0.

## Implementation Notes

- 2026-10-09 planning: scope is exactly the 6.1c triage items #1–#9; Checkpoint 1 self-approved under the owner's standing autonomous instruction.
- 2026-10-09 implementation (Amelia; the e2e driver speed-up bullet is owned by a separate agent and not covered here). **Decisions.** (1) Drag lane **written**, not deleted: the worker copies the body record's drag bits (+74) unchanged into motion piece +54 (`PhysicsMotionRead.DragRateOffset`, new constant, now used by `Validate` and `TrySample`); a raw bit copy avoids the worker's truncating `setF16` flushing a subnormal declared drag. One substep of drag moves a body by at most ~1.7e-5 m, below the Half position resolution, so the interpolation fact is a C# fact on an anchored free piece (t = 58/480 s, v0 8 m/s, k 0.125, g −9.81): `TrySample` matches the exact decayed solution within 1e-4 m and the drag-free piece overshoots by > 5e-3 m; the harness fact pins that the worker writes each body's declared bits (0.04, 0.125, 0). (2) Versions: `WorkshopTraceVersion.CompleteBodySet = 2` → `CompleteBodySetF32Velocity = 3` (2, 0, 4 rejected). `BodyRecordVersion.CanonicalHalf` → `HalfPoseF32Velocity`, **value 2 kept**: it is persisted in construction/save bytes that 6.1c kept identical, so a value change would reject every save (Never: gameplay change); 0, 1, 3 are rejected. (3) Offsets: the worker declares 18 named layout constants (body record base/stride/v/ω/mass/drag/gravity/COM, motion base/header/piece/drag/v/ω/gravity, response body base/stride/velocity) and uses them for every read, commit, motion and pose-ring access of those lanes; new `WorkerAbiTests` regex-reads them from worker.js (or `WORKER_SOURCE`) and compares with the compiled C# constants. The other hard-coded literals (cell/local/rotation, principal frame, other tables) are unchanged — out of the body-velocity scope. (4) Guard: the post-relax guard became `export function settleVelocity(b)` (per-component `Number.isFinite`, then clamp); `clampVelocity` now uses `envelopeScale`, which measures an f64-overflowing finite vector at 2^-512 scale. The previous sum guard zeroed v = (1e308, 1e308, −1e308), and `Math.hypot` of (1.5e308, −1.5e308, 1e308) overflowed to a zero scale. In-range arithmetic is unchanged (same `limit / length`); a non-finite component still yields k = 0 → NaN → the existing pose guard. The export is the "direct unit-level worker call" the spec allows; the second clamp is also reached by a real scenario (below). (5) Envelope claim **narrowed**: flight drag is per-tick at the 63.9 m/s boundary; rolling is per-tick up to 5 m/s only (see Surprises), with a 20 m/s rate fact.
- **Facts added** (`tools/workshop-rigid-body.test.mjs`, 37 → 42 tests): clamp fact rewritten over 16 Fibonacci-sphere directions with gravity 0, asserting the exact double sum of squares of the committed f32 components ≤ 64² / 128² and > 63.99² / 127.99²; 5 m/s fact uses the −1e-6 tolerance on v and ω_z and asserts spin strictly falls every tick until rest; COM +82 (local COM 0.1 m, spin 2 rad/s, zero gravity: origin within 3e-3 m of 0.1 m from the centre for 2 s and reaching > 0.19 m from its start); heavy (100 kg, 64 m/s) strikes light (1 kg, restitution 1) in the last substep of tick 1 → light commits 63.9999 m/s within the exact bound; `settleVelocity` unit fact (huge finite clamps keeping direction, 100 m/s / 200 rad/s clamp, in-envelope untouched, NaN in vx or wz zeroes all); motion-piece drag bits; flight at 63.9 m/s at 240 Hz decays every tick within 0.1% of 63.9e^-0.04t; rolling at 20 m/s at 240 Hz: rate within 10% over 1 s, spin never rises, never reverses, ends rolling. C#: `WorkerAbiTests` (1), `WorkshopTraceTests.RecordsOfAnyOtherTraceVersionReject` (3), `CanonicalBodyTests.OnlyTheHalfPoseF32VelocitySchemaDecodes` (3), `WorkshopWireTests.BodyWireAdmitsEachVelocityBoundAndRejectsTheNextF32Above` (2: each axis at −bound reads, `BitIncrement` rejects; the largest equal component pair inside the vector bound reads, the next f32 pair rejects), `WorkshopReadTests.FreeMotionSampledBetweenTicksFollowsTheDragDecayedTrajectory` (1); `GenericCandidateBindsImmutableDeclarationsAndVectorDomain` now also commits the in-envelope (45, 45, 0) / (0, 0, −128) candidate and the exact (0, 0, −64) / (128, 0, 0) edge through `ValidateCandidate` and checks the bound bits.
- **Mutants** (scratch copies of the final worker under `scratchpad/sd61d/`, one change each, via `WORKER_SOURCE`): no-headroom (clamps at exactly 64/128) → only the clamp fact fails (direction 1 commits |v|² 4096.0000287; 6 of 16 directions exceed each bound); no-drag-lane (piece +54 write removed) → only the drag-bits fact fails (0 vs 10527); old-offset (`BODY_CENTRE_OF_MASS = 80`) → only the COM fact fails (deviation 0.141 m) and `WorkerAbiTests` fails; old-wire-stride (`BODY_WIRE_BYTES = 56`) → `WorkerAbiTests` fails; zero-on-huge (old sum guard + plain hypot clamp) → only the `settleVelocity` fact fails (|v|² 0); no-second-clamp → the strike fact (126.73 m/s committed) and the `settleVelocity` fact fail. The final worker passes 42/42. Red baseline: on the unchanged worker the clamp (gravity-reduced speed, then fixed by gravity 0), COM (tolerance, then fixed), `settleVelocity` (no export), drag-bits and the first 40 m/s rolling fact failed.
- **Surprises.** (a) Rolling above ~5 m/s is not decelerated on every tick: spin holds for runs of ticks while only drag slows the speed, then friction re-couples (10 m/s: 6 stalled ticks/s; 20: 28; 30: 65; 40 m/s: no rolling resistance for ~100 ticks); average deceleration stays within 10% up to 30 m/s. Not precision; a solver limit (likely the relax pass reading the rotated anchor of a fast-spinning sphere as separation). Out of scope (Never: solver features); recorded in deferred-work and the docs row narrowed. My first 40 m/s per-tick fact was replaced by the 20 m/s rate fact. (b) Pre-existing motion-piece issues found while adding the drag lane, recorded in deferred-work and not changed: pieces are written from the end-of-substep pose but labelled with the start ordinal (samples lead by one substep), and the origin is written where the host reads the COM cell (equal only while local COM is zero). (c) The tests project lists compile items explicitly; `WorkerAbiTests.cs` had to be added to the csproj before it was discovered. (d) Process slip: I briefly copied a scratch probe into `tools/.probe-tmp.mjs` with `cp`; it was deleted immediately (anvil-validated delete) and never ran from there.
- **Files touched.** `CuriousContraptions.Simulation/wwwroot/worker.js` (sha256 da42e6d7…), `CuriousContraptions.Simulation/WorkshopTrace.cs`, `engine/gpu/{CanonicalBody,PhysicsMotionRead,WorkshopTraceRecord}.cs`, `tools/GpuBodyFixture/Program.cs` (rename only; stale tool outside the solution), tests `CuriousContraptions.tests/{WorkerAbiTests (new),CanonicalBodyTests,WorkshopReadTests,WorkshopTraceTests,WorkshopWireTests}.cs` and `CuriousContraptions.tests.csproj`, harness `tools/workshop-rigid-body.test.mjs` (sha256 4df467e6…), docs `docs/gpu-f32-physics.md` (Hardened paragraph, Maximum Velocities and Committed Velocity Resolution rows), `_bmad-output/implementation-artifacts/deferred-work.md` (second clamp resolved; two new entries), `TODO.md`. `sprint-status.yaml` stays `in-progress` until review. Legacy deleted: the sum-based non-finite guard, the hard-coded body/motion/response velocity offsets in the worker, `CompleteBodySet` (trace schema 2) and the `CanonicalHalf` name.
- **Evidence (implementer, not a review verdict).** `node --experimental-vm-modules --test tools/workshop-rigid-body.test.mjs` 42/42; `node --experimental-vm-modules --test tools/*.test.mjs` 83/83; `dotnet test CuriousContraptions.slnx` 642/642 (632 + 10; only the 5 pre-existing xUnit2013 warnings in `WorkshopActivationAnimationTests.cs`); `anvil check --changed` 0 warnings and `anvil_check` 0 over all 16 changed files; `dotnet publish CuriousContraptions.web` exit 0, and the worker served on :8060 (plain, br, gzip) hashes da42e6d7…, identical to the source. **Not run:** the cumulative Chrome e2e suite — held at the coordinator's instruction until the e2e driver changes land. Not committed or pushed.
- 2026-10-09 e2e speed-up slice (separate implementation agent; `tools/e2e/workshop-driver.ts` and `tools/e2e/*.test.ts` only, no game-side change or new global). **Signals.** (a) `CCGPU <base64>` console line, which `MachineWorld.LogWorkshop` already prints for every UI command acknowledgement (Construct, Run, Reset, Save, Pause, Resume). The driver decodes bytes 8–11 (kind, outcome, reason, phase) into value tables, rejecting unknown values, and each command-issuing action waits for exactly that acknowledgement: Applied and in the expected phase, with a 15 s bound and a message naming the action, outcome and reason. (b) Rendered frames (`requestAnimationFrame` through `page.evaluate`, 10 s bound) for UI-only clicks, Escape and readiness. (c) Pose-ring sequence advance after Run (the ring is written only while running, so Build-mode edits and level changes never change it). (d) A read-only IndexedDB check of the save slot after the Save acknowledgement. (e) `CCGOAL_SOLVED` resolving `waitForGoalSolved(timeout)` from the console listener. **Driver.** `toggleRun(waitMs)` is replaced by `run(windowMs)` (asserts Running, waits for a new publication, then holds `windowMs` from the key press as the physics window) and `reset(holdMs)` (asserts Building; `holdMs` is used only for the three deliberate 900 ms drain windows). `clickAt` holds the button for one frame and settles three frames instead of 60 ms + 200–800 ms. Placement, gizmo drags, wiring choice, Fine-rotate tilt, Load and level selection wait for their Construct acknowledgement. Save, Pause and Resume wait for their own acknowledgements. `waitForReady` waits five frames instead of 400 ms. **selectLevel race fixed.** A level change submits a Construct to the existing worker; the worker is not recreated, and the old comment saying it was is corrected. The readiness predicate was therefore already true, and only the fixed 400 ms covered the change. selectLevel now waits for the Construct acknowledgement. The level picker is a Godot PopupMenu that discards clicks arriving too soon after it opens, with no observable end to that guard, so the item click keeps a 250 ms hold from the frame the picker opened. **Tests.** Removed the first-test `reload()` in the 14 suites that had one, and the 13 `selectLevel('free_workshop')` calls that followed a reload. Replaced 17 goal-polling loops and the 300 ms post-Resume sleep. Removed explicit `any` from 2a1, 2b1–2b4 and 2c. Every assertion, negative control, mid-test Save/reload/Load, sampling window and absence window is unchanged; 0 assertion lines were removed. **Process slip.** A module-import parse probe imported the test files and launched part of the Chrome suite before the coordinator had freed the machine (anim-1a/1b/1c test 1 and cat-014 test 1). It was stopped within about two minutes. All three anim tests failed after the 15 s acknowledgement bound in `selectLevel`, which exposed the picker click guard; cat-014 test 1 passed. **Chrome results** (served `/simulation/worker.js` sha256 da42e6d7…; serial run `node --test --test-concurrency=1 --test-timeout=150000 tools/e2e/*.test.ts`; logs in `scratchpad/e2e-speed/run{1..4}.log`).
  - **Final run:** 53/54 pass in 478 s (baseline 731 s). That includes about 15 s spent waiting out the one failing acknowledgement.
  - **Hidden no-ops exposed.** The strict acknowledgement waits revealed three gizmo drags that had silently done nothing. The baseline runs passed without them.
    - The second ramp tilt in every two-ramp recipe was a no-op: `RotationGizmo._angles` keeps the Z handle where the previous drag left it, about 3.45 rad, while the driver always started at 3.8 rad. A HEAD-driver probe showed no Construct and the ramp flat (screenshot `old-2-tilt2.png`).
    - The wall tilt in 2a4 test 2 and 2b3 test 1 was also a no-op. The wall inherited Move mode from the ball lift, and the front-view ring maths does not fit the 3D view.
  - **Fixes.**
    - The driver tracks the Z handle angle per page and resets it on reload.
    - New `tiltSelectedWall(degrees)` selects Rotate mode, then drags the projected Z handle. The ring radius of 1.97 m was measured from a screenshot.
    - The two wall-tilt tests call it; screenshots show both ramps and the wall actually tilted.
    - With both ramps really at −20°, the two-ramp solves still capture the ball, and the tilted-wall CCD iterations still rebound with min py > 3.8.
    - The Space/Escape hold is back to 70 ms. Zero-length and two-frame holds were dropped in probes; this did not fix the failure below.
  - **Open, game-side, not fixed:** anim-1b test 1's final `reset()` gets no acknowledgement.
    - Cause: after a level is chosen from the picker, Godot keyboard focus stays on the OptionButton, and a Space pressed while running is consumed there (`ui_accept`), so `ToggleRun` never fires.
    - Reproduced on first_principles too, and with the HEAD driver on the same bundle: its unchecked `toggleRun(500)` also produced no acknowledgement, so the baseline left that machine running.
    - Focus moved off the picker (palette click, then Escape), or a page where the picker was never used: Reset is acknowledged.
    - Not worked around in the driver, because that would mask a real input defect.

- 2026-10-09 level-picker focus fix (Amelia; coordinator scope addition (3)). **Fix.** `ui/Workshop.cs` creates the level picker with `FocusMode = Control.FocusModeEnum.None`. It still opens and selects with the pointer, but it never takes keyboard focus, so Space reaches `Workshop._UnhandledInput` (Run/Reset). This also covers opening the list and dismissing it without choosing, which releasing focus after `ItemSelected` would not. No driver change. **Native test (test-first).** `WorkshopHintTests.ClickingTheLevelPickerLeavesKeyboardFocusWithTheWorkshopShortcuts` pushes a real left press and release through the root viewport at the picker's centre, asserts the list opened, closes it and asserts the picker is not the viewport's focus owner. Before the fix it failed on that last assertion (the picker kept focus). Surprise: the headless window is stretched onto the 1440×900 viewport, so pointer events must be in window pixels (`viewport.GetFinalTransform() * point`); viewport coordinates missed every control. **Evidence (implementer).** `dotnet test CuriousContraptions.slnx` 643/643; `anvil check --changed` 0 (18 script files) and `anvil_check` 0 on `ui/Workshop.cs` and `WorkshopHintTests.cs`; `dotnet publish CuriousContraptions.web` exit 0. Served `/simulation/worker.js` sha256 da42e6d7… (plain and br, unchanged by this fix). `node --test --test-concurrency=1 --test-timeout=150000 tools/e2e/anim-1b.test.ts` 3/3, 28 s, including test 1's final Reset acknowledgement. Cumulative `node --test --test-concurrency=1 --test-timeout=150000 tools/e2e/*.test.ts`: 15 suites, 54 tests, 54 pass, 0 fail, 0 cancelled, exit 0, 463 s wall (duration_ms 463082), one serial run on the first attempt; log `scratchpad/sd61d/cumulative.log` (sha256 05882603…). Not committed or pushed.
- 2026-10-09 e2e review fix round (e2e implementation agent; review items 1–8).
  - (1) `waitForGoalSolved` counts solves after a mark taken at call time. After the `CCGOAL_SOLVED` line it settles three frames (the halo sample lands about 3 ms after the line). It returns the number of new solves. Seven "run until solved" steps now assert that result is above 0 (anim-1a test 3, 2a3, 2b1–2b4, 2c); the old loops did not check it.
  - (2) Acknowledgements:
    - The driver now also decodes the command sequence (u64 @0) and authority revision (u64 @32).
    - Each acknowledgement must be newer than the last consumed one on both counts.
    - The wire carries no command kind, so the command's kind (`WorkshopCommand` table) is checked through the phase it implies.
    - Exactly one acknowledgement must arrive per input.
    - A decode error now belongs to its own line only.
  - (3) The final Reset before every `errors.length === 0` assertion holds its old window again: 500 ms in anim-1b, anim-1c, cat-014, cat-023a and cat-023b; 300 ms in 2b1, 2b2, 2b4 and 2c.
  - (4) Retired-world drains are restored: cat-023b test 4 has `reset(800)` before the lamp sample comparison, and the CCD iterations have `reset(200)`. Pose reads are additionally tied to the new run (5).
  - (5) `waitForPublicationAfter` requires both a sequence above the pre-Run maximum and a capture timestamp later than the pre-Run slot's.
  - (6) The tilt outcome is asserted: 2a4 test 2 asserts its existing `horizontalMotion` per iteration, and 2b3 test 1 tracks and asserts the same.
  - (7) Robustness:
    - `selectLevel` rejects re-selecting the current level and any non-Build phase.
    - The tracked handle angle is updated in `finally`.
    - `expectSavedSlot` requires the 'CCWS' magic and a declared and actual length of 5720 bytes (`WorkshopSaveCodec.ByteLength`).
  - (8) Worker ABI:
    - `worker.js` declares `ORIENTATION_SENSORS_OFFSET = 19744`, and the former inline literal now uses it.
    - `WorkerAbiTests` maps it to `PhysicsGpuAbi.OrientationSensorsOffset`.
    - `WORKER_SOURCE` is honoured only with `WORKER_ABI_CANDIDATE=1`, and the checked path is written to the test output.
  - **Evidence:**
    - `anvil check --changed` 0.
    - `dotnet test CuriousContraptions.slnx` 643/643 (only the 5 pre-existing xUnit2013 warnings).
    - `dotnet publish CuriousContraptions.web` exit 0; served `/simulation/worker.js` sha256 4116abb0d8fb6e02…, identical to the source. The rebuilt bundle also carries Amelia's picker focus fix.
    - anim-1a alone ×5: 3/3 each, 24, 23, 24, 24 and 23 s.
    - Cumulative serial ×2 (`run5.log`, `run6.log`): 54/54 each, 476 s and 476 s.
- 2026-10-09 duplicate e2e recipes merged (owner decision, 9 Oct 2026; e2e implementation agent). Each merged test carries every original ID in its name and suite header, so a grep for each ID still finds its proof. Each asserts the union of its copies' checks. The copies differed only in message text, except two cases. 2c #3 also asserted `readCaptured() === 0` after the reload, and that assertion is now in 2a3 #5. 2b3 #1 also asserted zero console errors, and that check stays in the merged test. No suite file was removed.

  | Old test (suite: name) | New test |
  |---|---|
  | ENGINE-CORE-2a3: 4. First principles 2-ramp solve | engine-core-2a3: 4. [ENGINE-CORE-2a3, 2b1, 2b2, 2b3] First principles 2-ramp solve … |
  | ENGINE-CORE-2b1: 2. Multi-body Ramp and Wall interaction (First principles 2-ramp solve) with direct pose ring publishing | same (engine-core-2a3 #4) |
  | ENGINE-CORE-2b2: 2. Multi-body Ramp and Wall interaction (First principles 2-ramp solve) under pure TGS Soft compliance | same (engine-core-2a3 #4) |
  | ENGINE-CORE-2b3: 2. Multi-body ramp and wall solve under speculative contacts without analytic interval sweeps | same (engine-core-2a3 #4) |
  | ENGINE-CORE-2a3: 5. Exact Reset and Save/Load restoration | engine-core-2a3: 5. [ENGINE-CORE-2a3, 2b1, 2b2, 2b3, 2b4, 2c] Exact Reset and Save/Load persistence roundtrip in Chrome |
  | ENGINE-CORE-2b1 / 2b2 / 2b3 / 2b4 / 2c: 3. Exact Reset and Save/Load persistence roundtrip in Chrome | same (engine-core-2a3 #5) |
  | ENGINE-CORE-2a4: 2. High-speed impact across 20 iterations never tunnels through thin Wall | engine-core-2b3: 1. [ENGINE-CORE-2a4, ENGINE-CORE-2b3] High-speed ball impact … 20 iterations … |
  | ENGINE-CORE-2b3: 1. High-speed ball impact against thin wall … across 20 iterations | same (engine-core-2b3 #1) |
  | ENGINE-CORE-2a4: 3. Exact Reset and Save/Load persistence restoration | engine-core-2a4: 2. (renumbered only; a different recipe) |

  - **Proof:** 54 tests became 45 (9 copies folded).
    - anim-1a alone ×5: 3/3 each, 25, 26, 26, 23 and 24 s.
    - Cumulative serial ×2 (`run7.log`, `run8.log`): 45/45 each, 371 s and 377 s.
    - The same suite before the merge took 476 s (54 tests); the baseline was 731 s.
    - Served worker sha256 4116abb0….
  - Not committed.

## Spec Change Log

- 2026-10-09 (coordinator, scope addition during implementation): the folded e2e speed-up's stricter acknowledgement waits exposed three latent defects the old fixed sleeps hid — (1) the second ramp tilt in every two-ramp recipe never hit its handle (ramp 2 stayed flat; driver fixed to track the handle angle), (2) the free-workshop wall tilt never happened in 2a4 test 2 and 2b3 test 1 (driver gains a rotate-mode `tiltSelectedWall`), (3) a game input defect: the level picker keeps keyboard focus after a selection and swallows Space while running, so Reset never fires. (3) is fixed in the game within this story. KEEP: no assertion, negative/absence window or test removed; Save/Load reloads kept; real UI only.
- 2026-10-09 (owner decisions via coordinator): (a) level picker stays pointer-only (`FocusMode.None`); trade-off recorded in DESIGN.md known limitations — closes the deferred picker-reachability finding. (b) Duplicate e2e recipes (Reset + Save/Load ×6, two-ramp solve ×4, 20-impact ×2) are merged into shared tests keeping the union of assertions and every requirement ID; mapping recorded in Implementation Notes. (c) Parallel e2e declined for now; stay serial. (d) Epic 7 legacy purge runs after 6.1d and before Story 6.2.

## Review Triage Log

Pass 1 (2026-10-09). Murdoch: Fail — intermittent anim-1a test 3 halo assertion introduced by `waitForGoalSolved` resolving on the solve line (~3 ms before the first halo sample); all 6.1d hardening items verified (mutants, versions, ABI drift test, real tilts, ~460 s vs measured 731.5 s). Routed to the e2e implementer (patch): (1) settle frames + mark-based goal wait and asserted results; (2) ack identity/sequence matching, scoped decode errors (BH, EC); (3) restore error-check holds before final error asserts (BH); (4) retired-world drains before sample/pose counts (BH); (5) publication wait tied to the new run (BH, EC); (6) assert wall tilt outcome (VG, BH); (7) selectLevel guard, handle-angle update in finally, save-slot comparison (EC); (8) orientation-sensor offset into WorkerAbiTests, restrict WORKER_SOURCE override (VG, BH). Deferred: harness not in CI/pre-commit (VG); picker keyboard reachability vs DESIGN.md:312 (M, EC, BH) — owner decision; earlier 2a3/2b1/2b2/2b4/2c/anim passes ran with ramp 2 or the wall untilted, re-proven by this story's runs (BH) — record in delivered-slices at close.

**Pass 2 (2026-10-09). Murdoch: terminal scoped Pass.** SnapshotApproval is granted for a local commit of the 6.1d files only. Epic 7 planning edits are excluded.

**Runs:**

| Check | Result |
|---|---|
| `tools/*.test.mjs` | 83/83 |
| anvil | 0 warnings, 18 files |
| dotnet | 643/643 |
| publish | exit 0 |
| served `worker.js` | `4116abb0…`, plain and br, matching source |
| anim-1a ×3 | 3/3 each time |
| cumulative serial | 15 suites / 45 tests, 45 pass, 368.0 s |

HEAD had measured 731.5 s on the cumulative serial run.

**Merge preservation:** a machine comparison of assertion predicates found that each merged test holds the union of its HEAD copies. All requirement IDs are in the titles, and the mapping table matches the code.

**Pass-1 items:** all eight fixed.
- Halo-race probe: 8/8 present, against 2/8 missing in pass 1.
- Driver negative control: still fails loudly.
- Hardening mutants rebuilt from the current worker: each kills only its own fact.

**Non-blocking findings, all Low, deferred:**
1. Run's one-acknowledgement check has no settle frames (`workshop-driver.ts:441`).
2. The driver hard-codes `SAVE_BYTES = 5720` instead of deriving it from the C# codec.

**Accepted limitation:** Construct and Save both end in Build mode.

**Bundle:**

| Artifact | Hash |
|---|---|
| `worker.js` | `4116abb0…` |
| `godot.pck` | `30a01345…` |
| `CuriousContraptions.wasm` | `05ea17c5…` |
| AppBundle manifest | `3f0c6c57…` |

Evidence: `scratchpad/murdoch16/`.

## Verification

**Commands:**
- `node --experimental-vm-modules --test tools/*.test.mjs` -- expected: all pass
- `anvil check --changed` -- expected: 0 warnings
- `dotnet test CuriousContraptions.slnx` -- expected: 100%
- `dotnet publish CuriousContraptions.web` -- expected: exit 0
- `node --test --test-concurrency=1 --test-timeout=150000 tools/e2e/*.test.ts` -- expected: all suites 100%
