# Specification: Story 6.10 — 90-Degree Pipe Bend & Joined Pipe Route (CAT-050)

## 1. Context & Overview
Deliver CAT-050: 90-Degree Pipe Bend (`pipe_bend_90`) with continuous turned passage, mouth port locators, and level integration for `quarter_bend` ("Around the corner") and `joined_pipe` ("Meet in the middle").
Follows the established declarative glTF/Godot architecture, strict visual vs. collider entity separation, dynamic part discovery via `PartId.PipeBend90`, and pre-commit verification gates.

## 2. Declarative Asset Specifications
- **Catalog Resource:** `parts/catalog/pipe_bend_90.tres` (`id: "pipe_bend_90"`, title: "90° pipe bend", category: "Motion", referencing `res://parts/scenes/pipe_bend_90.tscn`).
- **Scene:** `parts/scenes/pipe_bend_90.tscn`:
  - Static hollow circular bend: centreline radius $R = 2.4\text{ m}$, sweep $90^\circ$ ($\pi/2$), bore radius $r_{in} = 0.65\text{ m}$, shell radius $r_{out} = 0.70\text{ m}$.
  - Geometric centre arc offset: $-2.4 \cdot (\sin(\pi/4), \cos(\pi/4), 0) \approx (-1.697056, -1.697056, 0)$.
  - Port locators:
    - `Loc_Port_mouth_start` at angle 0: $(-1.787056, 0.702944, 0)$
    - `Loc_Port_mouth_end` at angle 90°: $(0.702944, -1.787056, 0)$
  - Presentation geometry: `Visual/Visual_Shell` (curved cylinder segments) and `Visual/Visual_Collar_Start`, `Visual/Visual_Collar_End` (cream `#fff8e9` collar meshes).
  - Collision geometry: compound static boxes (`col_*`, `visible = false`) forming the curved guide channel and mouth rims.
- **Web Export:** Include `res://parts/catalog/pipe_bend_90.tres` in `export_presets.cfg`.

## 3. Engine & Authoring Bindings
- `engine/PartId.cs`: Add `PartId.PipeBend90 = new("pipe_bend_90")` with numeric ID 18.
- `engine/gpu/WorkshopCanonicalColliders.cs`: Add `PipeBend90Colliders` matching extracted scene shapes.
- `engine/gpu/WorkbenchCapacity.cs`: Footprint `"pipe_bend_90" => new(1, 0, colliders, 0, 0, 0)`.
- `engine/gpu/WorkshopConstruction.cs`: Factory method `WorkshopInput.PipeBend90`.
- `engine/gpu/WorkshopConnections.cs`: Register socket owners for `"pipe_bend_90"`.
- `engine/gpu/WorkshopInventory.cs`: Register `PartId.PipeBend90` in canonical order.
- `engine/MachinePart.cs`: Set `PickRadius = 2.2f`.
- `engine/gpu/QuarterBend.cs`: Authored lesson definition for `quarter_bend` ("Around the corner") matching `content/puzzles.json`.
  - Locked basketball at `(0.7029, 7.3870564, 0)`
  - Locked receiver at `(-5.5, 0.6, 0)`
  - Inventory: 1 `pipe_bend_90`
  - Solution: placed at `(0, 4, 0)` rotated -90°.
- `ui/Workshop.cs` & `ui/WorkshopPuzzle.cs`: Wire level picker and mode presentation.
- `tools/e2e/workshop-driver.ts`: Wire `quarter_bend` picker and `pipe_bend_90` palette coordinates.

## 4. Verification Requirements
- `anvil check --changed`: 0 warnings.
- `dotnet test CuriousContraptions.slnx`: 100% pass rate.
- Playwright E2E in real Chrome:
  - `tools/e2e/cat-050.test.ts`:
    1. Select level `quarter_bend`: initial layout verified (locked ball, locked receiver, 1 pipe_bend_90 in inventory).
    2. Negative control: without placing the bend, ball drops vertically and misses receiver (no solve).
    3. Solved run: place `pipe_bend_90` at `(0, 4, 0)` rotated -90°, ball enters mouth, traverses 90° curve, emerges horizontally, lands into receiver, emitting `CCGOAL_SOLVED`.
    4. Reset restoration: Reset restores initial state; re-run solves bit-for-bit.
    5. Save/Load persistence: Save, reload, Load restores construction, re-run solves.
