---
title: 'Story 9R.7: joined_pipe level is playable (reopened Story 6.10)'
type: 'feature'
created: '2026-10-18'
status: 'ready-for-dev'
route: ''
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/planning-artifacts/epics.md#story-9r7-joined_pipe-level-is-playable-reopened-story-610'
  - '{project-root}/_bmad-output/planning-artifacts/retro-verification-2026-10-18.md'
  - '{project-root}/docs/planning/elements/CAT-048-pipe.md'
  - '{project-root}/docs/planning/elements/CAT-050-pipe_bend_90.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** `joined_pipe` ("Meet in the middle", `content/puzzles.json:7930`) cannot be played: `WorkshopPuzzleId` (`engine/gpu/WorkshopPuzzle.cs:5`) has no member, the picker (`ui/Workshop.cs:40-51`, `ui/WorkshopPuzzle.cs:12-31`) has no item, and the driver `WorkshopLevel` union (`tools/e2e/workshop-driver.ts:310`) has no level (report #4). The level is also unsolvable with today's pipe: its solution is a pipe with `length: 2`, but the Pipe has no length parameter (`WorkshopGenericInstance`, `engine/gpu/WorkshopInstances.cs:27-40`). Its colliders are fixed at a 1.8 m half length (`WorkshopCanonicalColliders.CreatePipeColliders`, `:92-97`), and only Wall and Ramp can be resized (`MachinePart.ResizableAxes`, `engine/MachinePart.cs:40-45`).

**Approach:** Add the `JoinedPipe` lesson as a compiled authored construction, using the `QuarterBend` pattern. Give the Pipe its declared `length` parameter (CAT-048 §2: 1–8 m, default 3.6) with one X resize handle. The player then shortens the granted pipe and snaps it (Story 9R.6) to the locked bend.

## Boundaries & Constraints

**Always:**
- Common 9R rules:
  - Chrome proof uses actual UI controls through `workshop-driver.ts`. Read-only observations are the CCGPU acks (`MachineWorld.LogWorkshop`, `engine/MachineWorld.Gpu.cs:363`), the pose ring, animation samples and the IndexedDB slot.
  - Committed reads arrive only on command acks, so exact state is sampled at Construct, Load, Pause, Step or Reset acks.
  - Exact Run/Reset uses the Story 9R.1 snapshot. Its contract: take the tick-0 committed read from the last Construct or Load ack plus the authored saved bytes. After Reset, deep-equal every body's cell, local, rotation, com, velocity and angularVelocity, plus the captures, activations, timers and electrical rows (model: `cat-062b.test.ts:226-231`).
  - Negatives in authored levels run at Precise.
- Fixtures equal the content exactly:
  - ball locked at (−1.3081, 8.402188, 0);
  - `pipe_bend_90` locked at (1, 3.5, 0), rotated −45° about Z;
  - Receiver locked at (−0.2, 0.6, 0);
  - goal Captured(receiver, ball);
  - inventory `{pipe: 1}`;
  - the three precision knots.
- `length` is a typed f32 parameter. Pointer input clamps finite values to 1–8 and rejects non-finite values (CAT-048 §2). A save that lacks it is rejected explicitly and is never defaulted.

**Never:**
- No new heuristic, shim, alias or migration.
- The lesson is not loaded from `puzzles.json`; Story 9A.26 converts lessons to content data.
- No numeric placement and no imported solution.
- Do not edit the CAT-050 `quarter_bend` behaviour.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Solve | pipe resized to 2, tilted downhill, lower mouth snapped to bend `mouth_start` | ball travels pipe → bend → Receiver; one `CCGOAL_SOLVED` | N/A |
| Unsnapped | same pipe left ≥0.72 m off the mouth (snap refused) | ball leaves the route between the mouths; 0 solves within 1,200 ticks | snap refusal leaves no ack |
| Default length | pipe 3.6 snapped | ball lands on the outer shell (drop line x −1.308 crosses the bore axis at y ≈ 6.80) | negative evidence |
| Over-inventory | second pipe placement | no Applied Construct; saved bytes unchanged | Validate "inventory exceeded" |
| Bad length | resize drag past 8 or below 1 | committed length is exactly 8 or 1; no out-of-range value is saved | clamp at the pointer boundary |

</frozen-after-approval>

## Code Map

- `engine/gpu/WorkshopPuzzle.cs:5`, `:66-90` -- the `WorkshopPuzzleId` enum and its Validate dispatch.
- `engine/gpu/QuarterBend.cs:40-83` -- the template for Create, WithPrecision and Validate. Its Validate rejects a locked bend at `:76-77`.
- `engine/gpu/WorkshopInventory.cs:90-97` -- `Authored(puzzle)`, a single-part allowance.
- `ui/Workshop.cs:40-51`, `:55+`; `ui/WorkshopPuzzle.cs:12-31`, `:96`, `:103-145`, `:150-175` -- picker indices, `_nextId`, PresentMode text and ChangePrecision.
- `engine/gpu/WorkshopInstances.cs:27-40` -- the parameterless `WorkshopGenericInstance`.
- `engine/gpu/WorkshopCanonicalColliders.cs:20-33`, `:92-120` -- the fixed 24-box pipe table.
- `engine/MachinePart.cs:40-55`; `ui/RotationGizmo.cs:175-186`; `ui/WorkshopGuidance.cs:99-108`, `:416-426` -- resize axes, drag and cancel.
- `engine/MachineWorld.Gpu.cs:119`; `engine/gpu/WorkshopWireFlatBuffers.cs:644`, `:809`, `:895-896`; `engine/gpu/WorkshopWire.cs:428` -- generic capture and codec.
- `parts/scenes/pipe.tscn:176-180` -- mouths at ±1.8 (L/2). CAT-048 §2 says ±(L/2+0.09).
- `parts/scenes/pipe_bend_90.tscn:446-450` -- `mouth_start` at (−1.787, 0.703), facing −X.
- `tools/e2e/workshop-driver.ts:200-212`, `:310`, `:498-552`, `:658-663` -- picker anchors, level union, tool gating and the wall-only resize.

## Tasks & Acceptance

**Execution:**
- [ ] `CuriousContraptions.tests/WorkshopPipeLengthTests.cs` (new) -- write these tests first, red:
  - `length` bounds 1/8, the clamp, NaN rejection, and a missing field rejected;
  - colliders, mouths and pick radius scale with length;
  - a codec round trip.
- [ ] `engine/gpu/WorkshopInstances.cs`, `WorkshopCanonicalColliders.cs`, `WorkbenchCapacity.cs` -- add the typed pipe `length` parameter. Colliders are generated per length at compile time, and the collider count is unchanged (24).
- [ ] `engine/MachinePart.cs`, `parts/scenes/pipe.tscn` art binding -- make the pipe resizable on X. Shell scale, collars and mouths move to ±length/2 (keep consistent with the 9R.6 mouth sockets).
- [ ] `engine/gpu/WorkshopWireFlatBuffers.cs`, `WorkshopWire.cs`, save codec -- encode `length`. Bump the save version per codec convention, update the driver's version check (`workshop-driver.ts:726`), and reject old saves explicitly.
- [ ] `CuriousContraptions.tests/JoinedPipeTests.cs` (new) -- red first:
  - fixtures and knots equal the content;
  - a locked bend is required;
  - an unlocked bend, a second pipe or any foreign part rejects.
- [ ] `engine/gpu/JoinedPipe.cs` (new), `WorkshopPuzzle.cs` -- add `WorkshopPuzzleId.JoinedPipe` and Create/WithPrecision/Validate.
- [ ] `ui/Workshop.cs`, `ui/WorkshopPuzzle.cs` -- picker item "Meet in the middle", PresentMode title, task and hint text from the content, precision and `_nextId`.
- [ ] `tools/e2e/workshop-driver.ts` -- add:
  - the `joined_pipe` level, picker anchor and dock;
  - `pipe` gating for `joined_pipe`;
  - a generic `resizeSelectedPart(axis, delta)` that drags the real square handle, projected like `tiltSelectedWall`;
  - an `expectNoCommand(action)` helper that asserts no ack arrived and the authored bytes are unchanged.
- [ ] `tools/e2e/cat-050-joined.test.ts` (new) -- the Chrome suite below.

**Acceptance Criteria:**
- Given `joined_pipe` chosen from the real picker, when the player places the granted pipe through the drawer, resizes it to 2 m, tilts it downhill, snaps its lower mouth to the fixed bend's upper mouth (9R.6) and presses Run, then the ball travels the joined route and exactly one solve is reported.
- Given the same pipe left unsnapped (snap refused at 0.72 m or 40°), when run at Precise, then the goal is unsolved. Pose samples show the ball outside the bore between the two mouths.
- Given the level loaded, when the construction is read, then the locked fixtures, knots and inventory equal `content/puzzles.json:7930-8203`, and a second pipe placement issues no Applied Construct.
- Given a solved construction, when Reset, then the committed read equals the pre-Run snapshot exactly, and a second Run solves again. When Save, reload and Load, then the pipe's pose, length and snap are byte-identical and the level solves.
- Given CAT-048 §5 boundaries, when the pipe is resized to 1 and to 8 through the handle, then the saved length is exactly the limit. Escape cancels the gesture, and one Undo restores the prior length.
- `tools/e2e/cat-050-joined.test.ts` passes 100% serially in Chrome.
- `dotnet build CuriousContraptions.slnx` reports 0 warnings, `dotnet test CuriousContraptions.slnx` passes 100%, and every `tools/e2e/*.test.ts` suite passes 100% in one serial Chrome run against the production preview.

**§5 map (CAT-050 and CAT-048, for this level):**
- Construction: AC1.
- Positive: AC1.
- Negative/control: AC2 and AC3.
- Boundaries: AC5. Rotated, oversize and rim controls are owned by `cat-050` and 9R.6.
- Run/Reset and Save/Load: AC4.
- Integrations: pipe→bend joint (here). Funnel and detector joints belong to 9R.6.
- Depth-error nudging: Incomplete (unscheduled).

## Implementation Notes

Amelia-13, 21 Oct 2026, on 63ba232c (Story 9R.6 rebased on 9R.5b).

- **Pipe type.** The pipe is a typed `WorkshopPipe` with an f32 `PipeLength` (1–8 m, default 3.6; `engine/gpu/WorkshopPipe.cs`), never a
  `WorkshopGenericInstance` (the generic instance now rejects `PartId.Pipe`). Its 24 boxes come from `WorkshopCanonicalColliders.Pipe(length)`
  at compile time: 16 shell boxes of half length `length/2` and 8 rim boxes at ±`length/2`. Record equality is on the length, so equal
  pipes compare equal (Undo, fixture checks, "bytes unchanged").
- **One collider path.** Before this story a placed pipe compiled the scene-extracted boxes (`def.Colliders`, whose shell boxes declare the
  opposite rotation sign about X) and a loaded pipe the canonical ones. Every pipe now compiles the generated boxes. The default length has
  the scene tube's box centres and extents (`TheDefaultLengthHasTheSceneTubeShape`).
- **Mouths.** 9R.6 moved the pipe mouths to the collar faces, so the length math is ±(length/2 + 0.09) (CAT-048 §2), not ±length/2. The
  scene still declares the default pipe's locators at ±1.89; `TubeMouthSnap.LocalMouth` moves a pipe's mouths to its length. A pipe mover
  must declare its length (`TubeMouthMover.Length`), and no other part may.
- **Commit paths.** `CaptureInstance` takes the presented length (`MachinePart.PipeLength`), else the committed pipe's own length, else
  (a new pipe) the default. Rotate, move, snap-on-release, placement and every `CommitSelected` keep a resized length.
- **Codecs.** The command wire writes the f32 length at slot bytes 112–115 (116+ must be zero); FlatBuffers uses the existing `length`
  field, whose schema default is 0, so a record without it fails validation. Wire version 21 → 22 (slot layout), save version 13 → 14
  (`workshop-driver.ts`, `workshop-wire.ts`, `run-reset-snapshot.ts`, `cat-062b.test.ts`). Older saves are refused as `UnsupportedSave`.
- **Art.** `MachinePart.ApplyPipeLength` stretches `Visual_Shell` along its long axis, moves the collars to ±length/2 and sets the pick radius
  √((length/2 + 0.09)² + 0.78²) (2.05 → 2.045 at the default). `pipe.tscn` is unchanged; `pipe.tres` declares no parameter.
- **Lesson.** `JoinedPipe` (ids ball, bend, receiver = first, +1, +2; `_nextId` advances by 3). Validate requires exactly one locked
  `pipe_bend_90` equal to the authored fixture (pose, lock and colliders), else `FixedPart`; an extra bend, a locked pipe or any other part is
  `InventoryUnsupported`; a second pipe is `InventoryExceeded`.
- **Driver.** `joined_pipe` level, picker anchor (600, 413, measured), dock anchors. The clear_pipe dock anchors were stale and unused (y 285);
  a selected pipe now shows Resize, so both pipe lessons use the measured row (y 244: move 60, rotate 104, resize 148, delete 192).
  New helpers: `resizeSelectedPart` (the real square handle with Shift), `resizeSelectedPartCancelled` (Escape before release) and
  `setPrecision` (the menu slider). `expectNoCommand` already existed (9R.6).

## Spec Change Log

- 21 Oct 2026 (Amelia-13, stale references refreshed; frozen intent unchanged):
  - The snap capture is 0.45 m and 20° (9R.6), not 0.72 m and 40°. "≥0.72 m off" stays the negative's distance, beyond the capture.
  - The bend's mouth is at local (−1.787, 0.703); the content's −45° orientation is column-major (its first column is local +X's image).
  - Geometry check: with collar-face mouths, the snapped 2 m pipe's centre is the content solution (−0.5373, 6.031442) within 2e-4 m, and
    its upper mouth is at x −1.308, on the drop line. The Design Notes' (−1.18, 6.67) used ±length/2 mouths.
  - Unsnapped pose: a refused snap keeps the 0.1 grid. The negative uses (−1.3, 6), whose lower mouth is 0.76 m left of the bend's upper
    mouth. Poses about 0.72 m up, down or up the axis from the joint still deliver the ball (`evidence-9r-7/native-pose-probe.txt`), so
    the lesson tolerates a loose joint in those directions; the left pose demonstrably does not (the ball lands on the outer shell).
  - The positive row runs at the lesson's default Balanced precision; the negative rows run at Precise.

## Review Triage Log

## Design Notes

**Decisions (owner playability delegation):**
- The AC's "place its granted pipe and bend" reads as "place the granted pipe and snap it to the fixed bend". Content grants only the pipe and locks the bend, and Story 6.10's AC (`epics.md:649`) says "a resized straight pipe snapped to the fixed bend". Rationale: the content and the original AC agree.
- The pipe `length` is built here, not deferred. Rationale: the level cannot be solved without it (I/O row "Default length"). Story 9A.9 then carries `length` through its package parity guard.
- Old saves are rejected rather than defaulted. Rationale: AGENTS no-migration rule.

**Geometry check:** the bend's `mouth_start` lands at world ≈ (0.234, 5.261), facing (−0.707, 0.707).
- With L = 2, the pipe's upper mouth is at ≈ (−1.18, 6.67), 0.13 m from the drop line, inside the 0.65 bore.
- With L = 3.6, the upper mouth sits about 1 m left of the drop line, and the ball strikes the shell's outside.

**Test-first plan:** native tests go red before engine edits. The Chrome suite is written against the new driver helpers before the UI lands. Every placement, resize, rotation and snap is a real pointer gesture, and the pose comes from the saved bytes.

**Mutation plan:** each change below must fail a named test.
- Drop the locked-bend check in `JoinedPipe.Validate` (JoinedPipeTests).
- Fix the length at 3.6 (Default-length row: no solve).
- Default a missing length to 3.6 (the codec rejection test).
- Skip the snap (unsnapped row).
- Allow a second pipe (over-inventory row).

**Impacted suites:** `cat-048a`, `cat-048b`, `cat-049`, `cat-050`, `cat-030`, `tube-snapping` (9R.6), `WorkshopSaveTests`, `PipeBend90Tests`, and every suite that saves (the version bump).

**Risks:**
- Mouth offset: the scene uses ±1.8, while the spec says L/2+0.09. If 9R.6 adopts the collar face, the length math must follow, and the 1e−4 coincidence check depends on it.
- An unsnapped pose might still deliver the ball. Choose one that demonstrably does not. If every refused pose still solves, stop and report a level-design defect.
- The save version bump invalidates players' existing slots.

## Verification

**Commands:**
- `dotnet build CuriousContraptions.slnx` -- expected: 0 warnings.
- `dotnet test CuriousContraptions.slnx` -- expected: 100% pass, including the new tests.
- `node --import <tsx loader> --test --test-concurrency=1 --test-timeout=150000 tools/e2e/cat-050-joined.test.ts`, then all of `tools/e2e/*.test.ts` serially against the 8060 production preview -- expected: 100%.
