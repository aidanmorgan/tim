# Story 6.6: Straight Pipe Compound Cylindrical Collider (CAT-048a)

## Status: Ready for Implementation

## Context & Purpose
Following the completion of the zero-remnant purge of procedural geometry and legacy part classes (Story 6.5f), Curious Contraptions now resumes the rolling playable catalogue roadmap at **Story 6.6: Straight Pipe Compound Cylindrical Collider (CAT-048a)**.

The Straight Pipe is authored purely as declarative data assets (`parts/catalog/pipe.tres` and `parts/scenes/pipe.tscn`) rooted on generic `MachinePart`, strictly obeying the architectural invariants:
1. **Strict Visual/Collider Entity Separation:** Visual tube and collar meshes are placed under `Visual/` (`MeshInstance3D`) and never extracted as physics bodies. Physics colliders are dedicated sibling entities (`col_box_*` / `CollisionShape3D` with `Visible = false`).
2. **Zero Backwards Compatibility:** No per-part C# scripts (`PipePart.cs`), no legacy dimension wrapper resources (`PipeDimensionsResource.cs`), and no custom annular engine branches.
3. **Compound Cylindrical Hollow Collider:** Extracted generically via `GltfPhysicsBodyExtractor` into a ring of 16 tangent box collider segment proxies along the tube circumference, creating an unobstructed internal bore (diameter 1.3 m, bore radius 0.65 m, length 3.6 m) with end collar collars.

---

## Acceptance Criteria

1. **Declarative Catalog & Scene Definition:**
   - Authored `parts/catalog/pipe.tres` defining `PartDefinition` with `Id = "pipe"`, `Title = "Clear pipe"`, `Category = "Motion"`, pointing to `res://parts/scenes/pipe.tscn`.
   - Authored `parts/scenes/pipe.tscn` rooted on `MachinePart` (`res://engine/MachinePart.cs`):
     - `Visual/`: Presentation meshes (`Visual_Shell` transparent `#66b8c9` alpha 0.16, `Visual_Collar_Start`, `Visual_Collar_End` cream `#fff8e9`).
     - `col_*` (`visible = false`): 16 box collider proxies arranged around the cylinder circumference at radius 0.675 m, thickness 0.05 m, length 3.6 m along local X, forming a smooth hollow cylinder that admits balls with radius <= 0.65 m.
     - Mouth collars / rim collider proxies at ±1.8 m along X that block balls with radius > 0.65 m from entering through the outside face.
     - `Loc_Port_mouth_start` at `(-1.8, 0, 0)` and `Loc_Port_mouth_end` at `(1.8, 0, 0)` for pipe socket and snapping locators.

2. **Simulation & Ball Travel (No Snagging, No Clipping):**
   - An admitted ball (e.g. Bowling Ball radius 0.28 m or Basketball radius 0.34 m) entering a tilted pipe mouth rolls continuously through the bore under gravity and emerges at the opposite mouth without snagging or falling through walls.
   - An oversized ball (radius > 0.65 m) cannot enter the bore and stops at the entrance collar without clipping.

3. **Pure TypeScript Playwright E2E Suite (`tools/e2e/cat-048a.test.ts`):**
   - Authored `tools/e2e/cat-048a.test.ts` exercising:
     - 1. Straight pipe placed in Workshop, ball enters elevated mouth, traverses through hollow bore, and exits safely.
     - 2. Reset restores the ball and pipe to exact initial poses; second Run replays bit-for-bit.
     - 3. Save / reload / Load roundtrip via IndexedDB restores the pipe and ball; subsequent Run reproduces the traversal.
     - 4. Oversized ball / negative control test verifying mouth entrance containment.
   - Passes 100% in real Chrome on port 8060.

4. **Quality Gates:**
   - `anvil check` outputs 0 warnings.
   - `dotnet test CuriousContraptions.slnx` passes 100%.
   - Cumulative Playwright E2E suites pass 100% serially in real Chrome (`cat-048a`, `cat-062`, `cat-015a`, `cat-023a`, `cat-014`, `engine-core-2a1`, `engine-core-2c`).
