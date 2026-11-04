---
title: 'Story 9A.46b: Use the same declared interactions in puzzles and Workshop'
type: 'feature'
created: '2026-11-05'
baseline_revision: 25b18d171ca1a4fe5c625e82692eac6761cd4d36
status: 'done'
review_loop_iteration: 0
followup_review_recommended: false
context:
  - '{project-root}/docs/testing-guidance.md'
  - '{project-root}/docs/architecture/element-package-isolation.md'
  - '{project-root}/_bmad/custom/element-free-engine-design.md'
  - '{project-root}/_bmad/custom/element-free-engine-review.md'
  - '{project-root}/_bmad-output/implementation-artifacts/review-engine-abstraction-2026-11-04.md'
warnings: ['oversized']
deferred: []
---

<intent-contract>

## Intent

**Problem:** Contact-trigger (`SignalContactDescriptor`) and contact-work (`ContactWorkDescriptor`) compilation currently employs an ad-hoc dual-dispatch mechanism based on `construction.Puzzle.Id.IsAuthored`: in Free Workshop it unconditionally targets `AllDynamic`, whereas in authored puzzles it searches for the first payload in instance order (`FirstPayload(construction)`) and targets that single body. This hardcoded branch causes instance reordering to arbitrarily alter which body activates a mechanism, and silently drops contact-trigger and contact-work rows entirely in authored scenes lacking a payload.

**Approach:** Introduce typed target selectors (`TargetSelector`) to the contact-trigger and contact-work declaration and authored boundary, allowing element declarations to specify default target policies (`AllDynamic`, `AllPayloads`, or `Named`) and authored puzzle content to explicitly declare intended interaction targets. Delete `FirstPayload` and all `IsAuthored` target-policy physical dispatch from `WorkshopPhysicsCompiler.cs`, compiling identical declared target semantics in all contexts.

## Boundaries & Constraints

**Always:**
- Execute physics using canonical IEEE-754 `f32` single precision at 120 Hz with 480 Hz substeps.
- Target selectors (`TargetSelector`) support `AllDynamic`, `AllPayloads`, and `Named(string)` / `Named(AuthoredSlot)`.
- Default target selector on `SignalContactDescriptor` and `ContactWorkDescriptor` is `TargetSelector.AllDynamic`, ensuring standard Workshop elements interact with all dynamic bodies without explicit authoring.
- Authored puzzle definitions specify intended named interaction targets explicitly in content (e.g. `interaction_target: "fixture_1"` on puzzle definitions or instances), resolving to exact `GpuBodyId` handles.
- Compiling in Workshop and authored puzzles follows only declared selectors; a supported capability is never silently dropped in scenes with zero payloads.
- Atomic validation rejects unknown named targets with a typed refusal (`WorkshopEditRejectedException` / `WorkshopEditRejection.PuzzleSettings` or `ArgumentException`).
- Exact Reset restores initial state and authored poses; Save/Load roundtrip preserves target selection.
- Zero skipped tests (`Skipped: 0`), no hash/byte assertions, and no mutation testing before the post-catalogue E2E phase.

**Never:**
- No implicit instance-order payload discovery (delete `FirstPayload(construction)` from `WorkshopPhysicsCompiler.cs`).
- No physical dispatch based on `construction.Puzzle.Id.IsAuthored` (delete `IsAuthored` target branching and `Targets(construction)` in `WorkshopPhysicsCompiler.cs`).
- No silent dropping of contact-trigger or contact-work capabilities when no payload exists in a scene.
- No backwards-compatibility shims, legacy fallbacks, or deprecated properties.
- No browser E2E execution during this slice (remains deferred per `BrowserDeferral.cs`).

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Reordered instances with explicit target | Two payloads (A and B); target explicitly declares B; instances ordered [B, A] vs [A, B] | Target body in compiled `BodyTargetSet` is strictly B in both instance orderings | No error expected |
| No-payload mechanism in authored puzzle | Authored puzzle with Bumper and switch, but no payload bodies (e.g. static geometry / dominoes) | Contact work and trigger rows are compiled and present in scene | No error expected (rows never silently dropped) |
| Nonselected-body control | Target explicitly declares body B; body A impacts Bumper | Bumper does not trigger or impart work to body A; body B impacts and triggers work | No error expected |
| Zero matches | Scene has no dynamic bodies, target is `AllDynamic` | Triggers and contact works compile; 0 matches during simulation | No error expected |
| Unknown named target | Authored puzzle declares `interaction_target: "nonexistent_fixture"` | Atomic rejection at puzzle validation / compilation | Typed refusal (`PuzzleSettings`) |
| Free Workshop default | Standard Bumper placed in Workshop with default selector | Compiles with `BodyTargetKind.AllDynamic` | No error expected |
| Existing playable lessons | Lessons `bumper_sidekick`, `bumper_depth`, `wall_and_bumper`, `delayed_signal` | Lessons compile intended named target `fixture_1` explicitly and pass canonical solutions | No error expected |

</intent-contract>

## Design Record (Element-Free Engine Gate)

1. **What behavior is delivered?**
   Contact triggers and contact work capabilities target explicitly declared sets of bodies (`AllDynamic`, `AllPayloads`, or `NamedBody`) uniformly across both Free Workshop and authored puzzles, eliminating the implicit `FirstPayload` instance scan and `IsAuthored` dual-dispatch path.
2. **What is reusable?**
   `TargetSelector` in `CuriousContraptions.Abstractions` declaring target policy (`AllDynamic`, `AllPayloads`, `Named`), and its resolution to `BodyTargetSet(BodyTargetKind Kind, GpuBodyId Body)`.
3. **What is declared?**
   - `SignalContactDescriptor` and `ContactWorkDescriptor`: `TargetSelector Target = default` (defaults to `AllDynamic`).
   - Authored puzzle content (`content/puzzles.json`): each lesson's intended named target is declared explicitly (e.g. `interaction_target: "fixture_1"` on the puzzle definition).
4. **How does it execute?**
   Compiler reads `TargetSelector` from capability declaration and authored puzzle -> resolves named target to exact `GpuBodyId` -> emits `TriggerDeclaration` and `ContactWorkDeclaration` with exact target -> simulation engine checks `MatchesTarget(data, kind, named, body)` using standard shared logic.
5. **Who owns changes?**
   Element package owns default capability target policy. Puzzle content author owns authored lesson target selection. Compiler resolves targets to body IDs. Simulation core executes contact evaluation without knowing element or puzzle identity.
6. **What proves generality?**
   - Generality proof: Multiple eligible bodies (e.g. payload A and payload B), explicit target selection (targeting B), and reordering instance order in both Workshop and authored puzzle confirms target follows declaration, not instance position.
   - Nonselected-body control: contacting non-target body does not trigger.
   - No-payload mechanism: bumper/switch in a scene with 0 payloads compiles rows and executes without silent row omission.
   - Atomic refusal: unknown named target (e.g. "fixture_99") fails with a typed refusal (`WorkshopEditRejection.PuzzleSettings` / `ArgumentException`).
   - Intended lesson solutions: all existing puzzle lessons solve cleanly.
7. **What is deleted?**
   `FirstPayload` instance scan in `WorkshopPhysicsCompiler.cs`
   `Targets(WorkshopConstruction construction)` dispatch in `WorkshopPhysicsCompiler.cs`
   `(!construction.Puzzle.Id.IsAuthored || FirstPayload(construction) is not null)` presence check in `WorkshopPhysicsCompiler.cs`.
8. **What remains uncertain?**
   None. All required design decisions are settled.

## Code Map

- `CuriousContraptions.Abstractions.Signal/SignalContracts.cs`: Add `TargetSelector Target = default` to `SignalContactDescriptor`.
- `CuriousContraptions.Abstractions.Mechanical/MechanicalContracts.cs`: Add `TargetSelector Target = default` to `ContactWorkDescriptor`.
- `CuriousContraptions.Abstractions/TargetSelector.cs`: Define `TargetSelector(TargetSelectorKind Kind, string? NamedTarget = null)`.
- `CuriousContraptions.Bundle/BundleValidator.cs`: Validate `TargetSelector` on contact and contact-work payloads.
- `engine/gpu/PuzzleDefinition.cs` & `engine/gpu/PuzzleContent.cs`: Parse `interaction_target` in authored puzzles; validate slot exists.
- `engine/gpu/WorkshopPuzzle.cs`: Carry `GpuBodyId InteractionTarget` on `WorkshopPuzzle`.
- `engine/gpu/WorkshopPhysicsCompiler.cs`: Delete `FirstPayload`, delete `Targets(construction)` and `IsAuthored` branching; resolve targets via declared `TargetSelector` and `InteractionTarget`.
- `content/puzzles.json`: Add explicit `interaction_target: "fixture_1"` to `bumper_sidekick`, `bumper_depth`, `wall_and_bumper`, and `delayed_signal`.
- `CuriousContraptions.tests/`: Add tests in `CuriousContraptions.tests/InteractionTargetTests.cs` verifying multi-payload reordering, nonselected-body control, no-payload presence, and unknown-target refusal. Update `PayloadInstanceTests.cs` to test explicit declared target resolution.

## Tasks & Acceptance

**Execution:**
- `CuriousContraptions.Abstractions/TargetSelector.cs`: Add `TargetSelector` struct and `TargetSelectorKind` enum.
- `CuriousContraptions.Abstractions.Signal/SignalContracts.cs`: Add `TargetSelector Target = default` to `SignalContactDescriptor`.
- `CuriousContraptions.Abstractions.Mechanical/MechanicalContracts.cs`: Add `TargetSelector Target = default` to `ContactWorkDescriptor`.
- `CuriousContraptions.Bundle/BundleValidator.cs`: Add validation for `TargetSelector`.
- `engine/gpu/PuzzleDefinition.cs`: Add `AuthoredSlot? InteractionTarget` to `PuzzleDefinition` and parse `interaction_target` in `PuzzleDefinitionReader`.
- `engine/gpu/WorkshopPuzzle.cs`: Add `GpuBodyId InteractionTarget` to `WorkshopPuzzle`.
- `engine/gpu/WorkshopPhysicsCompiler.cs`: Delete `FirstPayload`, delete `Targets(construction)`, delete `!IsAuthored || FirstPayload != null` presence gate. Plumb declared target resolution.
- `content/puzzles.json`: Explicitly declare `"interaction_target": "fixture_1"` in `bumper_sidekick`, `bumper_depth`, `wall_and_bumper`, and `delayed_signal`.
- `CuriousContraptions.tests/InteractionTargetTests.cs`: Add tests verifying multi-body reordering, nonselected control, zero matches, no-payload mechanisms, and unknown-target refusal.
- `CuriousContraptions.tests/PayloadInstanceTests.cs`: Update tests to verify declared target behavior.

**Acceptance Criteria:**
- Given multiple eligible bodies, an explicit target and a no-payload mechanism, when compile reordered instances in Workshop and an authored puzzle, then target membership follows only the declaration; a supported capability is never silently dropped.
- Given a body not selected by the target selector, when it collides with a bumper or switch, then no work or trigger activation occurs (nonselected-body control).
- Given an authored puzzle declaring an unknown named target, when parsed and validated, then atomic validation refuses it with a typed refusal.
- Given existing playable puzzle lessons (`bumper_sidekick`, `bumper_depth`, `wall_and_bumper`, `delayed_signal`), when simulated with authored solutions, then all pass cleanly.
- Given code inspection of `WorkshopPhysicsCompiler.cs`, `FirstPayload` and all `IsAuthored` target-policy physical dispatch are completely deleted.

## Spec Change Log

## Review Triage Log

- **Iteration 0 (Step-04 Review):**
  - Finding 1: Container override defect in `WorkshopPhysicsCompiler.ResolveTarget` forcing `AllDynamic`/`AllPayloads` to `NamedBody(puzzle.InteractionTarget)` in authored lessons.
    - Class: Defect
    - Severity: High
    - Disposition: Patch
  - Finding 2: Retention of `puzzle.Id.IsAuthored` and `PuzzleCatalogue` query in `ResolveNamedTarget` with placement index arithmetic.
    - Class: Element-Free Engine Gate / Forward-Only Violation
    - Severity: High
    - Disposition: Patch
  - Finding 3: Missing FlatBuffers serialization of `interaction_target` in `PuzzleRecord` and wire decode side-channel reconstruction bridge.
    - Class: Schema / Wire Defect
    - Severity: High
    - Disposition: Patch
  - Finding 4: Authored puzzle reordering test lacks multiple competing payloads to verify elimination of `FirstPayload`.
    - Class: Broken-Verification Gap
    - Severity: High
    - Disposition: Patch
  - Finding 5: `TargetSelectorKind.AllPayloads` resolution branch unverified.
    - Class: Regression Gap
    - Severity: High
    - Disposition: Patch
  - Finding 6: `PuzzleCatalogue.Validate` rejection of modified `InteractionTarget` unverified.
    - Class: Regression Gap
    - Severity: High
    - Disposition: Patch
  - Finding 7: Hand-mutated internal records in nonselected-body control tests instead of public interface.
    - Class: Defined-Interface Defect
    - Severity: Medium
    - Disposition: Patch

## Verification

**Commands:**
- `test "$(node --version)" = v26.12.1`
- `dotnet build CuriousContraptions.slnx -c Debug --nologo` -- expected: 0 warnings, 0 errors.
- `dotnet test CuriousContraptions.tests/CuriousContraptions.tests.csproj -c Debug --nologo --filter "FullyQualifiedName~InteractionTargetTests"` -- expected: all pass, 0 failed, Skipped: 0.
- `dotnet test CuriousContraptions.tests/CuriousContraptions.tests.csproj -c Debug --nologo --filter "FullyQualifiedName~PayloadInstanceTests"` -- expected: all pass, 0 failed, Skipped: 0.
- Run full verification block from `docs/delivery-workflow.md#local-test-authority` -- expected: all 5 test projects pass with Skipped: 0.
- Run `.githooks/pre-commit` on staged candidate -- expected: exit 0, Anvil clean.

### Receipts:
- Node check: `v26.12.1` verified.
- Solution Build: `dotnet build CuriousContraptions.slnx -c Debug --nologo` -> 0 Warning(s), 0 Error(s).
- Pre-commit: `.githooks/pre-commit` -> PASSED (Anvil clean, 0 warnings/errors, 127/127 static gate & UI tests passed).
- Scoped Tests:
  - `InteractionTargetTests`: 12 passed, 0 failed, 0 skipped.
  - `PayloadInstanceTests`: 10 passed, 0 failed, 0 skipped.
  - `LaunchLessonSolveTests`: 49 passed, 0 failed, 0 skipped.
- Full Local Solution Test Round (`CuriousContraptions.slnx`):
  - `WorkerAotGuard.Tests`: 17 passed, 0 failed, 0 skipped.
  - `Coverage.Tests`: 77 passed, 0 failed, 0 skipped.
  - `BundleElements.Tests`: 236 passed, 0 failed, 0 skipped.
  - `E2eRunner.Tests`: 192 passed, 0 failed, 0 skipped.
  - `CuriousContraptions.tests`: 5,076 passed, 0 failed, 0 skipped.
  - **Total Solution Tests: 5,598 passed, 0 failed, 0 skipped.**
