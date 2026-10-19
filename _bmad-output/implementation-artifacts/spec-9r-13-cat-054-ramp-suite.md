---
title: 'Story 9R.13: Ramp suite cat-054 (reopened Story 6.5d)'
type: 'feature'
created: '2026-10-18'
status: 'ready-for-dev'
route: ''
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/planning-artifacts/epics.md#story-9r13-ramp-suite-cat-054-reopened-story-65d'
  - '{project-root}/docs/planning/elements/CAT-054-ramp.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem (report #8):** `tools/e2e/cat-054.test.ts` never existed. Ramp resizing through the real handles has no Chrome proof at all: CAT-054 §3 says "Chrome proof of supported dimensions still required". The driver has only a wall-specific resize (`resizeSelectedWall`, `tools/e2e/workshop-driver.ts:658-663`).

**Approach:** Add a generic real-handle resize to the driver. Then add one suite following CAT-054 §5:
- resize the X (length) and Z (width) handles;
- a run-out comparison between a longer and a shorter ramp;
- a beside-the-ramp fall control;
- out-of-range refusal;
- the `first_principles` two-ramp delivery;
- exact Reset and Save/Load.

## Boundaries & Constraints

**Always:**
- Common 9R rules:
  - Chrome proof uses actual UI controls through `workshop-driver.ts`.
  - Committed reads arrive only on command acks.
  - Exact Run/Reset uses the Story 9R.1 snapshot. Its contract: the tick-0 read from the last Construct or Load ack plus the authored bytes. Every body's cell, local, rotation, com, velocity and angularVelocity, plus the captures, activations, timers and electrical rows, are deep-equal after Reset (model: `cat-062b.test.ts:226-231`).
  - Negatives in authored levels run at Precise.
- Ramp dimensions: length and width each 0.125–4 m (defaults 3 and 1.3); thickness 0.18 (`RampDimensions`, `engine/gpu/WorkshopInstances.cs:42-50`). Resize axes are X and Z (`MachinePart.ResizableAxes`, `engine/MachinePart.cs:40-45`).
- Out-of-range today: `SetDimensions` builds the `Half` value, and `ApplyRampDimensions` → `Validate` throws (`MachinePart.cs:50-54`, `:66-68`). The gizmo catch cancels the whole gesture and shows "Placement unchanged" (`ui/WorkshopGuidance.cs:416-426`). No Construct is issued.

**Never:**
- No setters, numeric dimension entry or imported solutions.
- Do not change the ramp's physics.
- No silent clamp is added for the ramp.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Run-out | the same tilted ramp at length 2 vs 4 (X handle), the same ball drop on the upper end | the longer ramp's leave-point and bench-landing x are further down-slope | N/A |
| Beside | ball dropped at the ramp x, offset in depth beyond width/2 + r | \|Δx\|, \|Δz\| ≤ 1e−3 m until bench contact | N/A |
| Max endpoint | X and Z Shift-dragged to 4.0 (0.1 snap grid) | saved length and width equal 4 exactly; ball contact agrees with the slab edge | N/A |
| Min side | Shift-drag to 0.1 (the snapped value below 0.125) | refused: no ack, bytes unchanged | gesture cancelled |
| Smallest reachable | Shift-drag to 0.2 | admitted; saved value 0.2 (binary16) | N/A |
| Over range | free drag to ≥ 4.1 or ≤ 0.05 | no ack; saved bytes unchanged; dimensions return to the gesture start | gesture cancelled |
| Cancel and Undo | Escape mid-drag; one Undo after a commit | start dimensions; one Undo restores the prior dimensions and keeps the part | N/A |
| first_principles | two drawer ramps at −20° (the engine-core-2c recipe) | 1 solve; with one ramp missing at Precise, 0 | N/A |

</frozen-after-approval>

## Code Map

- `engine/gpu/WorkshopInstances.cs:42-60` -- `RampDimensions` and `WorkshopRamp`.
- `engine/MachinePart.cs:40-75` -- axes, `SetDimensions`, `ApplyRampDimensions` (visual scale and pick radius).
- `ui/RotationGizmo.cs:117-119`, `:175-186`, `:221-227` -- enabled axes, handle position, resize drag, cancel.
- `ui/WorkshopGuidance.cs:99-108`, `:280-284`, `:384-432` -- the Resize mode button and the end, commit and catch logic.
- `tools/e2e/workshop-driver.ts:228-233`, `:586-592`, `:633-663` -- the `first_principles` dock (resize at 104, 275), `setPartMode('resize')` and the projection helpers.
- `tools/e2e/engine-core-2c.test.ts:46-54` -- the two-ramp recipe.

## Tasks & Acceptance

**Execution:**
- [ ] `tools/e2e/workshop-driver.ts` -- add `resizeSelectedPart(axis: 'x' | 'y' | 'z', worldDelta: number, { shift?: boolean })`. Shift is held through the drag so the gizmo snaps to 0.1 (`ui/RotationGizmo.cs:183`). It enters Resize mode through the real button, projects the square handle from the gizmo radius and the camera like `tiltSelectedWall`, and drags it with the real pointer. Also add `expectNoCommand`, and `undo()` through the real "↺ Undo" button. Shared with 9R.14 and 9R.7.
- [ ] `tools/e2e/cat-054.test.ts` (new) -- the matrix cases, each from a fresh page. Dimensions come from decoded saves, and motion from pose samples.
- [ ] Defect fixes, only if a case finds one, with a native test first (`WorkshopWallTests` covers the ramp ring today).

**Acceptance Criteria:**
- Given a Ramp placed and resized through the real X/Z handles, when a ball is released on it, then it rolls down. Given the same drop on a longer ramp, then the run-out is longer.
- Given a ball dropped beside the ramp, then it falls straight (control).
- Given a resize beyond 4 m or below 0.125 m, then no Construct is issued, the authored bytes are unchanged and the dimensions revert (negative).
- Given a Shift-snapped drag to 4.0 on both axes, then the saved dimensions equal 4 and the ball's contact agrees with the slab.
- Given a Shift-snapped drag to 0.1, then it is refused. A drag to 0.2, the smallest snapped valid value, is admitted. The declared minimum 0.125 is recorded as not reachable through the handle.
- Escape cancels, and one Undo restores the prior dimensions.
- Given `first_principles` with two drawer ramps, then exactly one solve is printed. Missing one ramp at Precise gives none.
- Given any case after Run, when Reset, then the read equals the pre-Run snapshot. When Save, reload and Load, then length and width are byte-identical.
- `cat-054` passes 100% serially in Chrome.
- `dotnet build CuriousContraptions.slnx` reports 0 warnings, `dotnet test CuriousContraptions.slnx` passes 100%, and every `tools/e2e/*.test.ts` suite passes 100% in one serial Chrome run against the production preview.

**§5 map (CAT-054):**
- Construction: AC6.
- Positive: AC1 and AC6.
- Negative/control: AC2, AC3, and the missing ramp in AC6. The misaligned ramp is the AC6 Precise variant. The valid Precise alternative is `engine-core-2b4`.
- Boundaries: AC4 and AC5. 0.125 is unreachable (owner question).
- Run/Reset and Save/Load: AC7.
- Integrations: Receiver in `first_principles` (AC6). Ball kinds: Basketball here, Bowling in `cat-014`. The sequence-task-285 audit is Incomplete (Story 9R.18).
- Nudging: Incomplete (unscheduled).

## Implementation Notes

## Spec Change Log

## Review Triage Log

## Design Notes

**Decisions (owner playability delegation):**
- "Refused" for the ramp is today's whole-gesture cancel with no command. Rationale: it is atomic and never saves an out-of-range value.
- The over-range drag targets ≥ 4.1 m. Rationale: binary16 spacing at 4 is about 0.0039, so 4.001 rounds into range and would not be refused.
- The run-out pair is 2 m vs 4 m at an identical pose and drop. Rationale: a matched control isolates length.
- Exactly 0.125 m cannot be reached through the handle. The ramp has no clamp (an over-range step cancels the gesture), Shift-snap lands on 0.1 (refused) or 0.2, and a free drag lands near 0.125 but not on it. This is the same owner question as the 9R.14 Wall clamp: should resize clamp at the limit (Wall) or cancel the gesture (Ramp)? Under the current cancel rule, the declared ramp minimum is unreachable. The suite proves today's behaviour and does not unify the two.

**§5 items not reachable or not in scope:**
- Physical placement nudging (facts 1–4) is unscheduled (CAT-054 §5). Stays Incomplete.
- Dashed projection agreement is pixel evidence. Capture a screenshot of each endpoint with the evidence, and compare the ball contact edge against the slab half length.
- The valid Precise alternative solution: `engine-core-2b4`.

**Test-first plan:** the driver helper goes first, verified by the saved-dimension change of one drag. Then the suite runs red against the current build. Failing behaviour is a defect stop.

**Mutation plan:** each change below must fail a case.
- Ignore the X dimension in the compiled box (run-out pair).
- Clamp instead of throw in `SetDimensions` (over-range: an ack would arrive).
- Skip dimensions in the save codec (Save/Load).

**Impacted suites:** `engine-core-2a3`, `2b1`, `2b2`, `2b3`, `2b4`, `2c`, `anim-1a`, `anim-1c`, `WorkshopWallTests`, `WorkshopSaveTests`.

**Risks:**
- Handle projection depends on the camera. Free workshop and `first_principles` use different views, so derive the handle position per view and verify it with one saved-bytes delta.
- The gizmo radius clamp (0.85–2.5) changes the handle position with ramp size.

## Verification

**Commands:**
- `dotnet build CuriousContraptions.slnx` -- expected: 0 warnings.
- `dotnet test CuriousContraptions.slnx` -- expected: 100%.
- Serial Chrome run of `tools/e2e/cat-054.test.ts`, then all of `tools/e2e/*.test.ts` against the 8060 production preview -- expected: 100%.
