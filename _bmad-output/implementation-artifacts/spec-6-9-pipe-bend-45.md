# Story 6.9: 45-Degree Pipe Bend Hollow Torus Segment & Level Gentle Bend (CAT-049)

## Status: Ready for Implementation

## Context & Purpose
Following the delivery of the Straight Pipe (Story 6.6), Clear Pipe level (Story 6.7), and Ball Detector (Story 6.8), Story 6.9 delivers **CAT-049: 45° Pipe Bend** and campaign level `gentle_bend` ("A gentle turn", puzzle 26 from `content/puzzles.json`).

The 45° pipe bend is a static hollow torus segment (centreline radius $R = 2.4\text{ m}$, sweep angle $45^\circ$, inner bore radius $0.65\text{ m}$, outer shell radius $0.70\text{ m}$) that turns falling balls smoothly through a gentle curve under gravity without added speed or snagging. In level `gentle_bend`, the player places the 45° bend at `(0, 4, 0)` rotated `-90°` about Z between a high-falling basketball at `(0.1827, 6.60844, 0)` and a target receiver at `(-2.0, 0.6, 0)`.

Strictly adhering to project rules:
1. **Strict Visual/Collider Entity Separation:** Visual meshes (`Visual_Shell` transparent `#66b8c9` alpha 0.16, `Visual_Collar_Start`, `Visual_Collar_End` cream `#fff8e9`) are isolated under `Visual/`. Physics colliders are dedicated hidden proxies (`col_*` with `visible = false`).
2. **Zero Backwards Compatibility:** No per-part C# scripts (`PipeBendPart.cs`), no legacy enums (`TubeBendAngle`), and no bespoke curved solver branches. All collisions resolve through compound colliders and the unified TGS soft solver.
3. **Exact Lifecycle & Chrome E2E Verification:** Proven with exact Reset restoration, Save/Load persistence roundtrip, and real Chrome Playwright execution.

---

## Acceptance Criteria

1. **Declarative Catalog & Scene Definition:**
   - Authored `parts/catalog/pipe_bend_45.tres` defining `PartDefinition` with `Id = "pipe_bend_45"`, `Title = "45° pipe bend"`, `Category = "Motion"`, pointing to `res://parts/scenes/pipe_bend_45.tscn`.
   - Authored `parts/scenes/pipe_bend_45.tscn` rooted on `MachinePart` (`res://engine/MachinePart.cs`):
     - `Visual/`: Presentation meshes (`Visual_Shell` transparent cyan `#66b8c9` alpha 0.16, `Visual_Collar_Start`, `Visual_Collar_End` cream `#fff8e9`).
     - `col_*` (`visible = false`): Compound hollow torus segment colliders modeling the $45^\circ$ curved channel with bore radius 0.65 m (diameter 1.3 m), plus end rim colliders.
     - Port locators:
       - `Loc_Port_mouth_start` at the Start mouth.
       - `Loc_Port_mouth_end` at the End mouth.

2. **Level `gentle_bend` Integration:**
   - Authored `engine/gpu/GentleBend.cs` (or extension in `WorkshopPuzzle.cs`):
     - `WorkshopPuzzleId.GentleBend`
     - Basketball at `(0.1827, 6.60844, 0)` (`Locked = true`).
     - Receiver at `(-2.0, 0.6, 0)` with `ReceiverAssistance` evaluated at `precision` (`Locked = true`).
     - Inventory: `PartId.PipeBend45: 1`.
     - Goal: `WorkshopGoalKind.Captured` (target: receiver, body: ball).
   - UI level picker integration in `ui/Workshop.cs` and `ui/WorkshopPuzzle.cs`:
     - Registered `"A gentle turn"` (`GentleBendIndex = 10`) in `_picker`.
     - Display level title `"A gentle turn"` and task `"Guide the falling ball toward the basket with a 45-degree bend."`.

3. **Workshop Driver E2E Support & Playwright Suite (`tools/e2e/cat-049.test.ts`):**
   - Updated `tools/e2e/workshop-driver.ts`:
     - Added `'gentle_bend'` to `WorkshopLevel` union.
     - Added `UI_ANCHORS.picker.gentle_bend` coordinates.
     - Added `UI_ANCHORS.dock.gentle_bend` coordinates.
     - Added `'pipe_bend_45'` tool selection in `selectTool`.
   - Authored `tools/e2e/cat-049.test.ts` verifying:
     - 1. Level selection: `selectLevel('gentle_bend')` loads level with locked ball, locked receiver, and 1 `pipe_bend_45` in inventory.
     - 2. Negative control: without placing the bend, Run drops ball straight down, missing receiver (`CCGOAL_SOLVED` is not emitted).
     - 3. Solved run: Place 45° bend at `(0, 4, 0)` rotated $-90^\circ$, ball enters mouth, negotiates $45^\circ$ curve smoothly under gravity, emerges, lands into receiver basket, achieving `CCGOAL_SOLVED`.
     - 4. Reset restoration: Reset restores exact starting poses; second Run replays bit-for-bit and solves again.
     - 5. Save & Load persistence roundtrip: Save, reload, Load restores construction, Run solves again.
   - Passes 100% in real Chrome on port 8060.

4. **Quality Gates:**
   - `anvil check` outputs 0 warnings.
   - `dotnet test CuriousContraptions.slnx` passes 100%.
   - Cumulative Playwright E2E suites pass 100% serially in real Chrome (`cat-049`, `cat-002`, `cat-048b`, `cat-048a`, `cat-062`, `cat-015a`, `cat-023a`, `cat-014`, `engine-core-2a1`, `engine-core-2c`).
