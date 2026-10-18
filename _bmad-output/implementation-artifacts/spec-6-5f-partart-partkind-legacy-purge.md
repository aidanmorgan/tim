# Story 6.5f: Zero-Remnant Procedural Geometry, Per-Part Classes, and WorkshopPartKind Purge

## Status: Ready for Implementation

## Context & Purpose
Following the successful conversion of all 14 catalogue elements to declarative glTF 2.0 / Godot `.tscn` scenes rooted on generic `MachinePart` (Stories 6.5d and 6.5e) and dynamic asset discovery (Story 6.5b), the legacy procedural mesh building class `engine/PartArt.cs` and the closed enum `engine/gpu/WorkshopPartKind.cs` represent obsolete transitional remnants. 

In strict adherence to the project rules and the user's explicit directive:
- **Zero Backwards Compatibility:** No shims, no compatibility type aliases, no deprecated fallback wrappers.
- **Zero Remnants:** Complete eradication of `PartArt` and `WorkshopPartKind` across all active C# source code and tests.
- **Strict Entity Separation:** Any remaining editor UI gizmo/table mesh generation must live in dedicated UI helpers (`ui/WorkshopArtwork.cs`), completely decoupled from engine core and part definitions.

---

## Acceptance Criteria

1. **Permanent Deletion of `engine/PartArt.cs` and `engine/gpu/WorkshopPartKind.cs`:**
   - Both files are removed from the filesystem and project.
   - Zero backwards compatibility shims, typedefs, or wrapper classes are left behind.

2. **Purge of `WorkshopPartKind` Across All Engine, UI, and Test Code:**
   - Replace any remaining usages of `WorkshopPartKind` in tests (`WorkbenchCapacityTests.cs`, `BasketballResourceTests.cs`, `WorkshopSpringboardTests.cs`, `WorkshopWallTests.cs`, `DelayedSignalTests.cs`, `DominoEffectTests.cs`) with strongly typed `PartId`.
   - Update `WorkbenchFootprint`, `WorkbenchCapacity`, `PuzzleDefinition`, and `WorkshopPorts` to operate exclusively on `PartId`.
   - Purge any switch statements branching on part kinds; all part behaviors and capacities derive from data-driven `PartDefinition` / `PartId`.

3. **Purge of `PartArt` from Active Engine Code:**
   - `engine/PartArt.cs` is deleted.
   - Any UI-level visual helpers required strictly for UI gizmos (e.g. `RotationGizmo.cs`, `WorkshopConnections.cs`, workbench staging in `Workshop.cs`, selection halo in `MachinePart.cs`) are migrated to `ui/WorkshopArtwork.cs` or inlined where appropriate.
   - No puzzle element uses procedural mesh generation.

4. **Zero-Occurrence Codebase Verification:**
   - `git grep "PartArt"` across all `.cs` source and test files returns **0 results**.
   - `git grep "WorkshopPartKind"` across all `.cs` source and test files returns **0 results**.

5. **Static Analysis & Automated Testing Quality Gates:**
   - `anvil check` passes with **0 warnings**.
   - `dotnet test CuriousContraptions.slnx` passes with **100% pass rate** (826/826 tests or equivalent updated tests).
   - Chrome Playwright E2E suites pass 100% serially in real Chrome:
     - `SPRING_PREVIEW_URL=http://127.0.0.1:8060/ npx tsx --test tools/e2e/cat-062.test.ts`
     - `npx tsx --test tools/e2e/cat-015a.test.ts`
     - `npx tsx --test tools/e2e/cat-023a.test.ts`
     - `npx tsx --test tools/e2e/cat-014.test.ts`
     - `npx tsx --test tools/e2e/engine-core-2a1.test.ts`
     - `npx tsx --test tools/e2e/engine-core-2c.test.ts`
