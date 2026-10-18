# Story 6.7: Hollow Signed-Distance Torus Rim Cap Pipe Collider & Level Clear Pipe (CAT-048b)

## Status: Ready for Implementation

## Context & Purpose
Following the delivery of the Straight Pipe compound cylindrical collider (Story 6.6), Story 6.7 brings level `clear_pipe` ("Through the looking tube", puzzle 25 from `content/puzzles.json`) to full playable completion in Chrome.

In this slice, the player solves `clear_pipe` by placing a clear pipe at `(0, 3, 0)` tilted at `-45°` about Z between a high-falling basketball at `(-1.4, 6.0, 0)` and a target receiver at `(2.0, 0.6, 0)`. The ball strikes the pipe entrance at high speed (~6 m/s) and a steep edge angle, smoothly enters the mouth, traverses the tilted bore without snagging or clipping, emerges from the exit mouth, and lands securely in the receiver to trigger goal capture.

Strictly adhering to project rules:
1. **Zero Backwards Compatibility:** No legacy annular identifiers, no `AnnularFeature` enums, and no bespoke physics kernels. All collisions resolve through generic compound colliders and the unified TGS soft solver.
2. **Strict Visual/Collider Entity Separation:** All presentation meshes stay under `Visual/` and are decoupled from physics colliders.
3. **Full Lifecycle & Chrome E2E Verification:** Proven with exact Reset restoration, Save/Load persistence roundtrip, and real Chrome Playwright execution.

---

## Acceptance Criteria

1. **Puzzle Level Integration (`clear_pipe`):**
   - Authored `engine/gpu/ClearPipe.cs` (or extension in `WorkshopPuzzle.cs`) declaring:
     - `WorkshopPuzzleId.ClearPipe`
     - Ball: Basketball at `(-1.4, 6.0, 0)` (`Locked = true`).
     - Receiver: Basket at `(2.0, 0.6, 0)` with `ReceiverAssistance` evaluated at `precision` (`Locked = true`).
     - Inventory: `PartId.Pipe`, count 1.
     - Goal: `WorkshopGoalKind.Captured` with target receiver and subject ball.
   - Integrated into `ui/Workshop.cs` and `ui/WorkshopPuzzle.cs`:
     - Registered in `_picker` as `"Through the looking tube"` (`ClearPipeIndex`).
     - Display title `"Through the looking tube"` and description `"Catch the falling ball inside the clear pipe and guide it down to the basket."`.
     - Inventory policy respects `PartId.Pipe: 1`.

2. **High-Speed & Edge-Angle Pipe Entry:**
   - When a ball falls from $Y = 6.0$ m and strikes the upper mouth of the $-45^\circ$ tilted pipe, the entrance rim colliders guide the ball into the bore without clipping through the wall or bouncing erratically.
   - The ball rolls down the 3.6 m tube under gravity, exits at the lower mouth, and lands in the receiver basket at $(2.0, 0.6, 0)$ within 1200 ticks.

3. **Workshop Driver & Playwright Suite (`tools/e2e/cat-048b.test.ts`):**
   - Updated `tools/e2e/workshop-driver.ts`:
     - Added `'clear_pipe'` to `WorkshopLevel` union.
     - Added `UI_ANCHORS.picker.clear_pipe` coordinates.
     - Added `UI_ANCHORS.dock.clear_pipe` coordinates for placing and editing parts in level mode.
   - Authored `tools/e2e/cat-048b.test.ts` verifying:
     - 1. Level selection: `selectLevel('clear_pipe')` loads level with locked ball and receiver.
     - 2. Negative control: without placing the pipe, Run drops the ball straight down to the bench, missing the receiver (goal never solved).
     - 3. Solved run: Pipe placed at `(0, 3, 0)` rotated $-45^\circ$, Run drops the ball through the pipe, ball lands in receiver, and `waitForGoalSolved()` succeeds.
     - 4. Reset restoration: Reset restores exact placed poses; second Run replays bit-for-bit and solves the level again.
     - 5. Save & Load roundtrip: Save, page reload, Load restores level construction, and Run solves the level.
   - Passes 100% in real Chrome on port 8060.

4. **Quality Gates:**
   - `anvil check` outputs 0 warnings.
   - `dotnet test CuriousContraptions.slnx` passes 100%.
   - Cumulative Playwright E2E suites pass 100% serially in real Chrome (`cat-048b`, `cat-048a`, `cat-062`, `cat-015a`, `cat-023a`, `cat-014`, `engine-core-2a1`, `engine-core-2c`).
