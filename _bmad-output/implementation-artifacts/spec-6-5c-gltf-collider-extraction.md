---
title: 'glTF Asset-Driven Geometry & Collider Extraction (ENGINE-CORE-2d)'
type: 'feature'
created: '2026-10-13'
status: 'done'
route: 'dispatch'
baseline_commit: '9107148592a934ff980b8f6201318968b8df35ea'
review_loop_iteration: 0
context:
  - '{project-root}/AGENTS.md'
  - '{project-root}/_bmad-output/planning-artifacts/architecture.md'
  - '{project-root}/_bmad-output/planning-artifacts/epics.md'
  - '{project-root}/docs/gpu-f32-physics.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Physical collider shapes, dimensions, and connection socket coordinates are currently hardcoded in C# classes (`WorkshopPhysicsCompiler.cs`, `ReceiverGeometry.cs`, `WallDimensionsResource.cs`, `RampDimensionsResource.cs`, etc.). Adding or modifying any puzzle element requires maintaining duplicated geometric values in C# code rather than extracting them authoritatively from 3D artist `.glb` containers.

**Approach:** Implement `GltfPhysicsBodyExtractor` to traverse imported scene / `.glb` node hierarchies and dynamically extract:
1. Physical collider proxies using standard Godot / glTF naming conventions (`col_box`, `col_sphere`, `col_capsule`, `col_cylinder`, `col_hull` / `-convcol`) or `CollisionShape3D` nodes into generic `ExtractedCollider` definitions.
2. Connection socket anchors from named empty locator nodes (`Loc_Port_<Id>`) into `ExtractedPortLocator` definitions.
3. Integrate collider extraction into `PartDefinition` and `WorkshopPhysicsCompiler` so any element with scene-defined colliders compiles into `ColliderDeclaration` records generically with zero element-specific branching.
4. Enforce strict entity separation: Visual mesh entities and collider mesh entities are distinct, separate nodes. Visual mesh nodes are exclusively for presentation; collider nodes are exclusively for physics simulation.

## Boundaries & Constraints

**Always:**
- Universal glTF 2.0 authority: Extract geometry solely from node transforms, meshes, and shapes.
- **Strict Entity Separation:** The visual mesh and collider mesh must be separate entities. Visual mesh nodes (`Visual_*` or standard `MeshInstance3D`) are strictly presentation entities; collider nodes are dedicated physics proxies (`col_*`, `-convcol`, `CollisionShape3D`) whose visual visibility is suppressed (`Visible = false`). Physics colliders are never extracted directly from visual meshes.
- Clean proxy naming: Detect `col_box`, `col_sphere`, `col_capsule`, `col_cylinder`, `col_hull`, `-convcol`, or `CollisionShape3D`.
- Dynamic port resolution: Detect `Loc_Port_<PortId>` locator nodes to locate connection ports in local coordinates.
- Maintain full IEEE-754 f32 precision and half-precision (`Half`) quantization where expected by `ColliderDeclaration`.

**Never:**
- Do not extract colliders from visual mesh entities.
- Do not add element-specific switch statements (e.g. `switch (kind)`) in the extractor.
- No backwards-compatibility shims, fallback loaders, or legacy flags.
- Do not break existing hardcoded paths while existing 14 elements await Stories 6.5d-e conversion.

## I/O & Edge-Case Matrix

| Scenario | Input Node | Extracted Output | Error / Fallback |
|----------|------------|------------------|------------------|
| Box Collider | Node named `col_box*` or `CollisionShape3D` with `BoxShape3D` | `ColliderShapeKind.Box`, local pose, half-extents from shape/mesh size | Throws `ArgumentException` if extents are zero/negative |
| Sphere Collider | Node named `col_sphere*` or `CollisionShape3D` with `SphereShape3D` | `ColliderShapeKind.Sphere`, local pose, radius from shape/mesh | Throws `ArgumentException` if radius is zero/negative |
| Port Locator | Empty `Node3D` named `Loc_Port_<Id>` | `ExtractedPortLocator` with ID `<Id>` and local transform | Ignored if node is not Node3D |
| Deep Hierarchy | Nested child nodes under visual parents | Transformed pose relative to root `MachinePart` | Accumulated local transforms |
| Visual Mesh Separation | Visual node (e.g. `Visual_*` or `MeshInstance3D` without `col_` prefix) | Ignored by `GltfPhysicsBodyExtractor`; reserved solely for presentation graph | Zero collision generation from visual meshes |

</frozen-after-approval>

## Code Map

- `engine/gpu/GltfPhysicsBodyExtractor.cs` -- Extractor service traversing Node3D hierarchies and emitting colliders and port locators.
- `engine/PartDefinition.cs` -- Cache extracted colliders and port locators upon resource load or configuration.
- `engine/MachinePart.cs` -- Expose port locators via `GetPortTransform(string portId)`.
- `engine/gpu/WorkshopPhysicsCompiler.cs` -- Admit asset-driven collider extraction for generic elements alongside existing legacy tables during 6.5d-e transition.
- `CuriousContraptions.tests/GltfColliderExtractionTests.cs` -- Comprehensive unit test suite for proxy detection, transform hierarchy accumulation, and port locator parsing.

## Tasks & Acceptance

**Execution:**
- [ ] `engine/gpu/GltfPhysicsBodyExtractor.cs` -- Implement `GltfPhysicsBodyExtractor` -- Generic node traversal and shape extraction.
- [ ] `engine/PartDefinition.cs` -- Expose extracted colliders and port locators on `PartDefinition`.
- [ ] `engine/MachinePart.cs` -- Expose port anchor querying by name.
- [ ] `engine/gpu/WorkshopPhysicsCompiler.cs` -- Support generic extracted colliders in compilation.
- [ ] `CuriousContraptions.tests/GltfColliderExtractionTests.cs` -- Author unit tests for all shapes and port locators.
- [ ] `anvil check --changed` -- 0 warnings.
- [ ] `dotnet test CuriousContraptions.slnx` -- 100% pass.
- [ ] Real Chrome Playwright E2E suites -- 100% pass.

## Verification

**Commands:**
- `dotnet test CuriousContraptions.slnx` -- 100% pass rate.
- `anvil check --changed` -- 0 warnings.
- `npx tsx --test tools/e2e/cat-014.test.ts` -- 100% pass in Chrome.
