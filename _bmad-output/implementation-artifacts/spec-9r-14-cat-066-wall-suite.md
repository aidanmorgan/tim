---
title: 'Story 9R.14: Wall suite cat-066 (reopened Story 6.5d)'
type: 'feature'
created: '2026-10-18'
status: 'ready-for-dev'
route: ''
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/planning-artifacts/epics.md#story-9r14-wall-suite-cat-066-reopened-story-65d'
  - '{project-root}/docs/planning/elements/CAT-066-wall.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem (report #8):** `tools/e2e/cat-066.test.ts` never existed. The Wall's stop and rebound across a ball path, its three-axis resize limits, Cancel/Undo, Reset and Save/Load have no element suite. Today it is touched only by `engine-core-2a3`, `2a4`, `2b3` and `cat-015b`.

**Approach:** Add one suite following CAT-066 §5:
- a drawer Wall resized on its local axes through the real handles (the 9R.13 `resizeSelectedPart`) and rotated, placed across a ramp-fed rolling path;
- the matched no-wall control;
- the limits and the refusal of out-of-range values;
- resize rejected while Running;
- exact Reset and Save/Load.

## Boundaries & Constraints

**Always:**
- Common 9R rules:
  - Chrome proof uses actual UI controls through `workshop-driver.ts`.
  - Committed reads arrive only on command acks.
  - Exact Run/Reset uses the Story 9R.1 snapshot. Its contract: the tick-0 read from the last Construct or Load ack plus the authored bytes. Every body's cell, local, rotation, com, velocity and angularVelocity, plus the captures, activations, timers and electrical rows, are deep-equal after Reset (model: `cat-062b.test.ts:226-231`).
- Wall limits:
  - minimum 0.4 × 0.4 × 0.12, maximum 8 × 6 × 2, default 3 × 2 × 0.25;
  - pointer input clamps finite values and rejects non-finite ones (`WallDimensions.FromInput`, `engine/gpu/WorkshopWall.cs:6-28`; via `MachinePart.SetDimensions`, `engine/MachinePart.cs:46-49`).
- Each local-axis handle grows the wall symmetrically about its centre (`ui/RotationGizmo.cs:179-185`). One gesture is one Undo, and Escape cancels (`RotationGizmo.End`, `:221-227`).

**Never:**
- No setters, numeric dimension entry or imported solutions.
- No new wall physics unless a defect is found.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Block | ramp-fed ball rolling +x; Wall resized (e.g. width 4 × height 3 × thickness 0.4) across the path | ball centre never passes the face plane minus r (pose samples every publication); vx reverses or reaches 0 | N/A |
| No wall | identical build without the Wall | ball passes the wall plane x | N/A |
| Rotated | wall yawed 45° | rebound along the face normal (CAT-066 fact 4) | N/A |
| Limits | drag each axis to the min and max | saved dimensions equal the limits exactly | N/A |
| Over range | drag past a max or below a min | saved dimension equals the limit; nothing out of range is ever saved | pointer-boundary clamp |
| While running | Resize during Run | no resize handle or command; bytes unchanged; same edit applies after Reset | UI gating |
| Cancel and Undo | Escape mid-drag; one Undo | start dimensions; prior dimensions and the part kept | N/A |

</frozen-after-approval>

## Code Map

- `engine/gpu/WorkshopWall.cs:6-40` -- `WallDimensions` limits, `FromInput` and `WorkshopWall`.
- `engine/MachinePart.cs:40-62` -- resize axes All, `SetDimensions` and `ApplyWallDimensions`.
- `ui/RotationGizmo.cs:117-119`, `:175-186`, `:221-227`; `ui/WorkshopGuidance.cs:99-108`, `:275`, `:384-432` -- handles, drag, cancel, part tools hidden in Run, commit.
- `tools/e2e/workshop-driver.ts:633-663` -- `tiltSelectedWall` and the origin-only `resizeSelectedWall`.
- `tools/e2e/engine-core-2a3.test.ts`, `engine-core-2b3.test.ts` -- existing deflection and no-tunnelling recipes to reuse.

## Tasks & Acceptance

**Execution:**
- [ ] `tools/e2e/workshop-driver.ts` -- reuse or land `resizeSelectedPart`, `expectNoCommand` and `undo()` from 9R.13. Retire `resizeSelectedWall` once its callers move to the generic helper, updating its callers together.
- [ ] `tools/e2e/cat-066.test.ts` (new) -- the matrix cases, each from a fresh page. Dimensions come from decoded saves, and blocking from every pose publication during the run.
- [ ] Defect fixes, only if a case finds one, with a native `WorkshopWallTests` case first.

**Acceptance Criteria:**
- Given a Wall placed, resized on its local axes and rotated through the real UI across a ball's path, when run, then the ball is stopped or rebounds and never passes through, in any pose sample.
- Given the same path with no Wall, then the ball continues past the wall plane (control).
- Given a drag beyond a limit, then no out-of-range value is admitted: the saved dimension equals the exact limit (negative).
- Given a Run in progress, then resize is unavailable and the bytes are unchanged. After Reset, the same resize applies.
- Given limits and gestures, then 0.4/0.4/0.12 and 8/6/2 save exactly, Escape restores the start dimensions, and one Undo restores the previous dimensions.
- Given any case after Run, when Reset, then the read equals the pre-Run snapshot. When Save, reload and Load, then dimensions and pose are byte-identical.
- `cat-066` passes 100% serially in Chrome.
- `dotnet build CuriousContraptions.slnx` reports 0 warnings, `dotnet test CuriousContraptions.slnx` passes 100%, and every `tools/e2e/*.test.ts` suite passes 100% in one serial Chrome run against the production preview.

**§5 map (CAT-066):**
- Construction: AC1.
- Positive: AC1 and the Rotated row.
- Negative/control: AC2, AC3, AC4, and Cancel/Undo in AC5.
- Boundaries: AC5. Maximum-speed tunnelling is covered by `engine-core-2b3`.
- Run/Reset and Save/Load: AC6.
- Integrations: Bumper and Receiver via `cat-015b` (`wall_and_bumper`). Occlusion is Incomplete (Epics 12–14). `wall_return` is not admitted.

## Implementation Notes

## Spec Change Log

## Review Triage Log

## Design Notes

**Decisions (owner playability delegation):**
- For the Wall, "an out-of-range resize is refused" is proven as "no out-of-range value is ever admitted": the committed dimension stops at the limit. Rationale: CAT-066 fact 2 carries the clamp forward, and `FromInput` clamps today. The ramp (9R.13) instead cancels the whole gesture, which makes its declared 0.125 m minimum unreachable. Clamp versus cancel is one owner question covering both stories, reported and not silently unified.
- Blocking is asserted on every pose publication, not just the final pose. Rationale: tunnelling can pass and return.

**§5 items not reachable or not in scope:**
- Maximum-speed no-tunnelling: `engine-core-2b3`.
- Light, air and sound occlusion: Epics 12–14 (CAT-066 §3).
- The `wall_return` level is not admitted. `wall_and_bumper` is covered by `cat-015b`.
- A NaN pointer input: native `WorkshopWallTests` (input clamp and non-finite rejection).

**Test-first plan:** the suite runs red against the current build. Any failure is a defect stop, fixed here.

**Mutation plan:** each change below must fail a case.
- Shrink the compiled half extents by 2× (Block: the ball passes).
- Remove the clamp, so the admitted value exceeds 8 (over-range row).
- Allow the resize gizmo in Run (while-running row).
- Skip dimensions in the save codec (Save/Load).

**Impacted suites:** `engine-core-2a3`, `2a4`, `2b3`, `cat-015b`, `WorkshopWallTests`, `WorkbenchCapacityTests`.

**Risks:**
- The rotated wall moves the handle projection, so verify every drag by its saved-bytes delta.
- Tall walls can occlude canvas picks of other parts, so place the ball and ramp first.

## Verification

**Commands:**
- `dotnet build CuriousContraptions.slnx` -- expected: 0 warnings.
- `dotnet test CuriousContraptions.slnx` -- expected: 100%.
- Serial Chrome run of `tools/e2e/cat-066.test.ts`, then all of `tools/e2e/*.test.ts` against the 8060 production preview -- expected: 100%.
