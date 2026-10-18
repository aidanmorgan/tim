---
title: 'Existing Implementation Conversion — Core Ball & Obstacle Primitives (CAT-001, CAT-014, CAT-064, CAT-003, CAT-066, CAT-054, CAT-004)'
type: 'refactor'
created: '2026-10-13'
status: 'done'
route: 'dispatch'
baseline_commit: '9107148592a934ff980b8f6201318968b8df35ea'
review_loop_iteration: 0
context:
  - '{project-root}/AGENTS.md'
  - '{project-root}/_bmad-output/planning-artifacts/architecture.md'
  - '{project-root}/_bmad-output/planning-artifacts/epics.md'
  - '{project-root}/DESIGN.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Core physical simulation elements (Basketball CAT-001, Bowling Ball CAT-014, Tennis Ball CAT-064, Balloon CAT-003, Wall CAT-066, Ramp CAT-054, Receiver CAT-004) rely on bespoke per-part C# scripts (`BallPart.cs`, `WallPart.cs`, `RampPart.cs`, `BasketPart.cs`), hardcoded dimension resources (`WallDimensionsResource.cs`, `RampDimensionsResource.cs`, `ReceiverGeometry.cs`), and procedural mesh generation in `PartArt.cs`. This violates the declarative data-driven engine architecture.

**Approach:** Convert all 7 ball and obstacle primitives forward to declarative scene / `.glb` assets using `GltfPhysicsBodyExtractor`:
1. Each element defines separate visual mesh entities (presentation graph) and dedicated collider proxy entities (`col_sphere`, `col_box`, with `Visible = false`).
2. Catalog resources in `parts/catalog/*.tres` configure material declarations and reference generic `MachinePart` scenes.
3. Completely delete bespoke per-part scripts (`BallPart.cs`, `WallPart.cs`, `RampPart.cs`, `BasketPart.cs`) and dimension classes with strictly zero backwards compatibility shims.

## Boundaries & Constraints

**Always:**
- Strict Entity Separation: Visual meshes and collider meshes must be separate entities in each element scene.
- Universal glTF / Scene extraction: Colliders and port locators extracted via `GltfPhysicsBodyExtractor`.
- Bit-for-bit physical equivalence: Balls and obstacles retain their exact physics dimensions, materials, and restitution.
- Complete deletion of replaced classes: No backwards compatibility shims, adapters, or fallback loaders.

**Never:**
- Do not extract colliders from visual mesh nodes.
- Do not create compatibility adapters or retain old procedural builders.

</frozen-after-approval>

## Tasks & Acceptance

**Execution:**
- [x] Refactor Basketball (`ball.tres`) and Bowling Ball (`bowling.tres`) to declarative scenes with separate visual and `col_sphere` entities.
- [x] Migrate Tennis Ball (`tennis.tres`) and Balloon (`balloon.tres`) to canonical `PartDefinition` without legacy parameters.
- [x] Refactor Wall (`wall.tres`) and Ramp (`ramp.tres`) to declarative scenes with separate visual and `col_box` entities.
- [x] Refactor Receiver (`basket.tres`) to declarative scene with separate visual and `col_box` walls.
- [x] Delete bespoke per-part scripts (`BallPart.cs`, `WallPart.cs`, `RampPart.cs`, `BasketPart.cs`) and obsolete dimension resources.
- [x] `anvil check --changed` -> 0 warnings.
- [x] `dotnet test CuriousContraptions.slnx` -> 100% pass.
- [x] Playwright E2E suites (`cat-014`, `engine-core-2a1`, `engine-core-2c`) -> 100% pass in Chrome.

## Verification

**Commands:**
- `dotnet test CuriousContraptions.slnx`
- `anvil check --changed`
- `npx tsx --test tools/e2e/cat-014.test.ts`
- `npx tsx --test tools/e2e/engine-core-2c.test.ts`
