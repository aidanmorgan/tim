---
title: 'Existing Implementation Conversion — Interactive Mechanisms & Dynamic Fixtures (CAT-015, CAT-023, CAT-062, CAT-063, CAT-035, CAT-022, CAT-005)'
type: 'refactor'
created: '2026-10-13'
status: 'in-progress'
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

**Problem:** The remaining 7 interactive fixtures and mechanisms (Pinball Bumper CAT-015, Domino CAT-023, Springboard CAT-062, Impact Switch CAT-063, Signal Lamp CAT-035, Delay Box CAT-022, Battery CAT-005) still rely on bespoke per-part C# scripts (`BumperPart.cs`, `DominoPart.cs`, `SpringboardPart.cs`, `SwitchPart.cs`, `LampPart.cs`, `DelayPart.cs`, `BatteryPart.cs`) and procedural mesh generation in `PartArt.cs`.

**Approach:** Convert all 7 interactive fixtures and mechanisms forward to declarative scenes rooted on generic `MachinePart` using `GltfPhysicsBodyExtractor`:
1. Each element scene defines separate visual mesh entities (`Visual_*`) and dedicated collider proxy entities (`col_*`, `visible = false`).
2. Sockets are authored as empty locator nodes (`Loc_Port_<Id>`) matching declared connection ports.
3. Moving sub-assemblies (e.g. Springboard plate, Switch push button) are authored as child branches in the scene.
4. Delete bespoke scripts (`BumperPart.cs`, etc.) and hardcoded parameter resources with strictly zero backwards compatibility shims.

## Boundaries & Constraints

**Always:**
- Strict Entity Separation: Visual meshes and collider meshes must be separate entities in each element scene.
- Universal glTF / Scene extraction: Colliders and port locators extracted via `GltfPhysicsBodyExtractor`.
- Bit-for-bit physical equivalence: Retain identical restitution, impulses, stiffness, damping, delays, and thresholds.
- Complete deletion of replaced classes: No backwards compatibility shims, adapters, or fallback loaders.

**Never:**
- Do not extract colliders from visual mesh nodes.
- Do not create compatibility adapters or retain old procedural builders.

</frozen-after-approval>

## Tasks & Acceptance

**Execution:**
- [ ] Refactor Pinball Bumper (`bumper.tres`, `bumper.tscn`) to declarative scene with separate visual and `col_sphere` entities.
- [ ] Refactor Domino (`domino.tres`, `domino.tscn`) to declarative scene with separate visual and `col_box` entities.
- [ ] Refactor Springboard (`springboard.tres`, `springboard.tscn`) to declarative scene with separate visual, `col_box_base`, and articulated `col_box_plate` entities.
- [ ] Refactor Impact Switch (`switch.tres`, `switch.tscn`) to declarative scene with separate visual and `col_box` entities and `Loc_Port_out`.
- [ ] Refactor Signal Lamp (`lamp.tres`, `lamp.tscn`) to declarative scene with separate visual and `col_box` entities and `Loc_Port_in`.
- [ ] Refactor Delay Box (`delay.tres`, `delay.tscn`) to declarative scene with separate visual and `col_box` entities and `Loc_Port_in`/`Loc_Port_out`.
- [ ] Refactor Battery (`battery.tres`, `battery.tscn`) to declarative scene with separate visual and `col_box` entities and `Loc_Port_out`.
- [ ] Delete bespoke per-part scripts (`BumperPart.cs`, `DominoPart.cs`, `SpringboardPart.cs`, `SwitchPart.cs`, `LampPart.cs`, `DelayPart.cs`, `BatteryPart.cs`).
- [ ] `anvil check --changed` -> 0 warnings.
- [ ] `dotnet test CuriousContraptions.slnx` -> 100% pass.
- [ ] Playwright E2E suites (`cat-014`, `cat-015a`, `cat-023a`, `cat-062`, `engine-core-2a1`, `engine-core-2c`) -> 100% pass in Chrome.

## Verification

**Commands:**
- `dotnet test CuriousContraptions.slnx`
- `anvil check --changed`
- `npx tsx --test tools/e2e/cat-014.test.ts`
- `npx tsx --test tools/e2e/cat-015a.test.ts`
- `npx tsx --test tools/e2e/cat-062.test.ts`
