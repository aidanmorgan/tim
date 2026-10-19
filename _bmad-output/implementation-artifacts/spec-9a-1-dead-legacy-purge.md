---
title: 'Story 9A.1: Dead Legacy Purge'
type: 'chore'
created: '2026-10-18'
status: 'ready-for-dev'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/AGENTS.md'
  - '{project-root}/_bmad-output/planning-artifacts/epics.md#story-9a1-dead-legacy-purge'
  - '{project-root}/_bmad-output/planning-artifacts/architecture-conformance-audit-2026-10-18.md'
  - '{project-root}/_bmad-output/implementation-artifacts/spec-9-4b-cumulative-regression-repair.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Unreachable code and files remain, and they invite copying.
- Story 9.4b (uncommitted, main tree) has already deleted part of the epic's list:
  - `tools/workshop-rigid-body.test.mjs` (`git status`: `D`);
  - the nine JS-boundary exports (`AllocateElectrical` … `AbiOffsets`; `rg` 0 hits);
  - their Node stubs;
  - the `EarlyLoss`/`navigator.gpu` control;
  - the README and `cat-014` references (spec-9-4b R7, D5, D6).
- What remains:
  - untracked `scratch/` (79 entries, 3.7 MB, including `.cs` copies and FlatBuffers patches);
  - tracked `scratch_test.cs.uid`;
  - 20 dead `worker.js` layout constants;
  - the dead `writeEventRing` export;
  - the pinning test and its `WORKER_ABI_CANDIDATE` branch, which served the deleted harness;
  - the `MachinePart.Build()` subclass hook;
  - the dead `PartRegistry` skips.

**Approach:** First verify that the 9.4b rows read 0. Then delete the remainder. Replace the `ProbePart` subclass with a composed, validated binding set that tests reach without inheritance. Seal `MachinePart` so the hook cannot return.

## Boundaries & Constraints

**Always:**
- Each deletion is proven unreferenced in the active tree: `rg` over code, csproj, `project.godot`, `export_presets.cfg`, `.tres`/`.tscn` and docs.
- Engine laws formerly exposed through exports stay covered by named C# tests.

**Never:**
- Delete the three live constants `READ_BODIES_OFFSET`, `BODY_WIRE_BYTES` and `BODY_WIRE_VELOCITY`. `writePoseRing` uses them (`worker.js:58-76`); Story 9A.2 removes them.
- Move cosmetic bindings into packages (that is Story 9A.25).
- Touch the `PartRegistry` clock exemption beyond the dead skip (the remaining A17 exemptions are Story 9A.14).

</frozen-after-approval>

## Code Map

- `scratch/` (untracked), `scratch_test.cs.uid` (tracked). Not compiled: every csproj sets `EnableDefaultCompileItems=false`, and no `Compile Include` names `scratch`.
- `CuriousContraptions.Simulation/wwwroot/worker.js:13-37`: `PRISMATIC_BYTES` … `MOTION_GRAVITY` (16 `const`) and `COLLIDERS_OFFSET`, `MATERIALS_OFFSET`, `TRIGGERS_OFFSET`, `CONTACT_WORKS_OFFSET` (4 `let`). Each name appears once in `worker.js` and otherwise only in `WorkerAbiTests`.
- `worker.js:84-108`: `export function writeEventRing`, which has 0 callers.
- `CuriousContraptions.tests/WorkerAbiTests.cs:11-61`:
  - `Owned` map (16 dead rows);
  - `WORKER_ABI_CANDIDATE`/`WORKER_SOURCE` branch (`:37-42`);
  - `WorkerDynamicOffsetFallbacksDeclareValidTableOffsets` (`:51-61`).
- `engine/MachinePart.cs:236` (`protected virtual void Build()`), called at `:528`.
- `CuriousContraptions.tests/BasketballResourceTests.cs:162-191` (`ProbePart` and `ArtworkBindingsRequireOneDeclaredCurveAndTimerPhasesOnlyForTimers`).
- `engine/PartRegistry.cs:29-31` (text scan and skip) and `:37-38` (definition skip). `parts/catalog/clock.tres:14` is the only resource with `Parameters`, and it is exempted, so both skips are dead.

## Tasks & Acceptance

**Execution** ([R:9.4b] re-run the census after 9.4b commits; [R:9R.0] the analyzers may flag the new type):

1. [ ] **Census, recorded as evidence.** Run the Appendix commands before any edit and save the output as `evidence-9a-1/census-before.txt`.
   - **Hold point:** ask the coordinator to confirm that no in-flight agent writes `scratch/`. Its files date from 17–18 Oct, and an untracked deletion cannot be recovered from git.
2. [ ] **Tests first.** In `CuriousContraptions.tests/WorkerAbiTests.cs`:
   - reduce `Owned` to the three live constants;
   - delete the candidate branch and the second fact;
   - add `WorkerDeclaresNoUnownedLayoutConstant`, which fails when `worker.js` declares any `^(const|let) [A-Z_]+ = \d+;` outside `Owned`. It is red until Task 3.
3. [ ] `CuriousContraptions.Simulation/wwwroot/worker.js`: delete the 20 constants and `writeEventRing`. [R:9.4b] The clock-master rewrite moves the lines.
4. [ ] `engine/MachinePart.cs`:
   - extract `CosmeticBindingSet`, an internal sealed class with `Add(declaration, target, property, neutral, active, phase)` holding today's `Bind` rules at `:146-155`;
   - `MachinePart` composes it;
   - delete `Build()` and its call;
   - mark the class `partial` and `sealed`.
5. [ ] `CuriousContraptions.tests/BasketballResourceTests.cs`:
   - delete `ProbePart`;
   - prove the three rejections and the acceptance directly on `CosmeticBindingSet`;
   - add one positive check through a real declared part: `registry.Create(PartId.Delay)` with `EnsureConstructed` gives `HasCosmeticBindings` and `Cosmetic == CosmeticCurves.Delay`.
6. [ ] `engine/PartRegistry.cs`: delete the `content` read and both skips. Every `.tres` loads or rejects explicitly.
7. [ ] Delete `scratch/` and `git rm scratch_test.cs.uid`.

**Acceptance Criteria (1:1 with epics 9A.1):**
- **AC1.** Given the audit's §7 evidence, when the items are deleted, then an active-tree `rg` for each deleted symbol or path returns 0. That covers the 9.4b rows, verified rather than re-deleted.
- **AC2.** Given the deleted exports, then their laws stay covered by named, passing C# tests: the spec-9-4b §5 coverage map (`WorkshopPhysicsEngineTests`, `PrismaticConstraintTests`, `SoftConstraintTests`, `WorkshopSpringboardTests`, `ElectricalSupplyTests`, `WorkshopBumperTests`), listed in the review record.
- **AC3.** `node --test tools/workshop-observation.test.mjs tools/workshop-pose-ring.test.mjs` passes.
- **AC4.** `ProbePart` behaviour is proven through declared bindings (Tasks 4 and 5).
- **AC5.** `dotnet build CuriousContraptions.slnx` reports 0 warnings, `dotnet test` passes 100%, and every `tools/e2e/*.test.ts` suite passes 100% in one serial Chrome run against the production preview (the 9.4b baseline).

## Design Notes

**Decisions (owner playability delegation):**
- **D1. Seal `MachinePart` and compose `CosmeticBindingSet`.** Rationale: CLAUDE.md prefers composition over inheritance, and sealing is a compile-time forcing function against reviving the hook. Story 9A.25 later feeds the set from package declarations without changing the rules.
- **D2. Keep the three live layout constants.** Rationale: deleting them breaks pose presentation, and Story 9A.2 replaces the pose copy.

**Negative controls and mutations:**
- Re-adding one dead constant makes `WorkerDeclaresNoUnownedLayoutConstant` fail.
- Restoring a `virtual` member on `MachinePart` makes CS0549 fail the build.
- Removing the one-curve rule in `CosmeticBindingSet` makes the two-curve rejection test fail.
- Adding a `Parameters` entry to a fixture `.tres` makes `PartRegistryTests` show it loads, not skips. That test lives in `PartRegistryTests.cs`, which already discovers the catalogue.

**Impacted suites:** `WorkerAbiTests`, `BasketballResourceTests`, `PartRegistryTests`, `workshop-pose-ring`, `workshop-observation`, and every Chrome suite (the pose copy is untouched).

**Risks:**
- `scratch/` is held by another agent (the Task 1 hold point).
- Sealing could break a hidden subclass. Census: `rg ": MachinePart\b"` finds only `ProbePart`.
- Sealing could also break Godot's C# source generators for a `partial` `Node3D` script. Prove it with the Godot build, `dotnet publish CuriousContraptions.web`. If the generators reject `sealed`, drop D1's seal and keep the composed set; the deleted hook is the AC.

## Verification

- For each `$s` in the 20 constants plus `writeEventRing` and `ProbePart`, run `rg -n "\b$s\b" --glob '!_bmad-output/**'`. Expected: 0.
- `rg -n "protected virtual void Build|Unmigrated legacy" engine`. Expected: 0.
- `git ls-files scratch scratch_test.cs.uid` and `ls scratch`. Expected: empty, and no such directory.

## Legacy Deleted

- `scratch/` (untracked) and `scratch_test.cs.uid`.
- `CuriousContraptions.Simulation/wwwroot/worker.js:13-37` (20 constants) and `:84-108` (`writeEventRing`).
- `CuriousContraptions.tests/WorkerAbiTests.cs:37-42,51-61` and the 16 dead `Owned` rows.
- `engine/MachinePart.cs:236,528`.
- `CuriousContraptions.tests/BasketballResourceTests.cs:162-172` (`ProbePart`).
- `engine/PartRegistry.cs:29-31,37-38`.
- Verified gone by 9.4b: `tools/workshop-rigid-body.test.mjs`, the nine exports and their stubs, and the `workshop-observation` `EarlyLoss` case.

## Implementation Notes

## Spec Change Log

## Review Triage Log

## Appendix: reference census (main tree, 18 Oct 2026)

| Item | Live references | Status |
|---|---|---|
| `tools/workshop-rigid-body.test.mjs` | `git status` D. `rg rigid-body` finds only `WorkerAbiTests.cs:37` (the comment deleted by Task 2) and `WorkshopPhysicsEngineTests.cs:9,85,281` (history comments; keep or reword) | 9.4b, verify |
| Nine exports (`AllocateElectrical`, `PaidContactImpulse`, `PrismaticRowKinds`, `SpringCoefficients`, `PrismaticJacobians`, `ConstraintRows`, `OperationAbi`, `StateBytes`, `AbiOffsets`) | `rg` 0 in the main tree. At HEAD `8f682f8` the `tools/workshop-pose-ring.test.mjs:67` stub named `OperationAbi`/`StateBytes`/`PrismaticRowKinds`; 9.4b removed it. [R:9.4b] recheck after commit | 9.4b, verify |
| `EarlyLoss`, `navigator.gpu`, `imports.initialize` | `rg` 0 | 9.4b, verify |
| `writeEventRing` | only its declaration at `worker.js:84` | delete |
| 20 constants | each 1× in `worker.js`, 1× in `WorkerAbiTests.cs` | delete |
| `READ_BODIES_OFFSET`, `BODY_WIRE_BYTES`, `BODY_WIRE_VELOCITY` | `worker.js` 3, 3 and 4 uses (`writePoseRing`) | keep (9A.2) |
| `MachinePart.Build` | `MachinePart.cs:236,528`; override `BasketballResourceTests.cs:165` | delete |
| `PartRegistry` skips | only `clock.tres` has `Parameters`, and it is exempt | delete |
| `scratch/`, `scratch_test.cs.uid` | 0 csproj, slnx, `project.godot` or `export_presets.cfg` references; `scratch` not in `.gitignore` | delete |

Commands: `rg -n -w <symbol> --glob '!scratch/**' --glob '!_bmad-output/**' .` and `rg -n scratch -g '*.csproj' -g '*.props' -g '*.slnx' -g project.godot -g export_presets.cfg -g .gitignore .`
