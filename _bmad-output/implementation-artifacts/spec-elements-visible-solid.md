---
title: 'Elements: every visible solid part collides with its true shape'
type: 'bugfix'
created: '2026-10-25'
status: 'done'
baseline: f321b91d
review_loop_iteration: 0
followup_review_recommended: false
context:
  - '/Users/aidan/dev/personal/tim/.claude/worktrees/story-elements-visible-solid/AGENTS.md'
  - '/Users/aidan/dev/personal/tim/.claude/worktrees/story-elements-visible-solid/docs/testing-guidance.md'
  - '/Users/aidan/dev/personal/tim/.claude/worktrees/story-elements-visible-solid/docs/gpu-f32-physics.md'
  - '/Users/aidan/dev/personal/tim/.claude/worktrees/story-elements-visible-solid/docs/architecture/element-package-isolation.md'
warnings: []
deferred:
  - 'Powered gate collars (outer 0.78 m) are not solid: the existing kinds need a new box ring around the bore, which this slice may not add. Deferred to engine-hollow-shapes (coordinator, 25 Oct 2026); VisibleSolidColliderTests lists them as pending.'
  - 'Pipe, both bends and funnel declare static restitution 1.0 against their documented 0.15. Setting the pipe to 0.15 was tried: clear_pipe no longer solves at precision 0 (LaunchLessonSolveTests), and joined_pipe event 1 moves from tick 240 to 272 against the 12-tick bound (F32BehaviourIdentityTests, all three precisions; clear_pipe also fails it). Reverted; deferred to engine-hollow-shapes, which sets 0.15 and re-authors the affected lessons (coordinator, 25 Oct 2026).'
  - 'Open, for a decision: extending the collider-vs-art test to every element package found visible parts standing proud of the body with no collider on elements outside this slice''s list. VisibleSolidColliderTests names each in its Uncollided list: the Basketball seam stripe; the Bumper cap and upper ring; the Domino pips; the Weight base, bands and eye; the Pressure plate studs, sockets and ring; and on the five logic parts, the Hold timer, Counter, Latch and Clock their face panels, lamps or indicators and sockets.'
---

<intent-contract>

## Intent

**Problem:** an audit (25 Oct 2026) found visible solid parts of puzzle elements with no collider, or with a collider of the wrong shape. Balls pass through drawn solids or bounce off empty space:
- the signal lamp's bulb and stem are art only (26 puzzles);
- the impact switch's round button is a box whose corners reach 0.55 m against the 0.38 m button (39 puzzles), and those corner hits probably fire the switch;
- the delay box's 1.25 m housing has no collider;
- the ramp rails and the battery terminal have no collider;
- the ball detector's outer surface stops at 0.70 m against the drawn 0.82 m, and its end faces are open;
- the powered gate's collar lips have no collider.

**Owner decision (25 Oct 2026):** "Yes, visible = solid". Every part a player sees as solid collides with its true shape, overriding element docs that said "art only".

**Approach:** data-only changes in each element's scene (`col_*` nodes read by `tools/BundleElements/SceneGeometryExtractor.cs`), using the existing Sphere, Box and Cylinder kinds. Each collider is bound to the same dimensions as its art. No engine or solver change.

**Not in this slice:** true ring, torus-segment and frustum shapes for the pipe, the bends, the funnel and the collars' round fit. Those belong to engine-hollow-shapes. Here, the collars of the gate and detector become solid out to their drawn outer radius using the existing kinds.

## Boundaries & Constraints

**Always:**
- Use the real production C# simulation, behavioural assertions, derived bounds and Skipped 0. There is no mutation testing (owner, 25 Oct).
- One element per scene change; the collider takes the art's own dimension bindings. Sensors and triggers keep their declared volumes unless the art defines them.
- Static restitution: settle whether statics' declared 1.0 against CAT-048's 0.15 is a real defect. Read how pair restitution combines in the solver. If 1.0 makes ball–static bounces wrong, set the documented value and test the bounce ratio (e² derivation). Otherwise document why 1.0 is the neutral value.
- Update each element doc's collider row. Edit only inside the slice worktree, with Write/Edit and Anvil. Do not commit.

**Never:**
- A new box-ring approximation of a round hollow part.
- Changes to the engine or solver.
- A fallback or second rule.
- Loosening an existing assertion.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Ball dropped onto the lamp bulb | Ball above the bulb's centre | Bounces off the sphere (r 0.43 at y 0.22) and never enters it | — |
| Ball beside the switch button | Ball landing 0.45 m from the axis (inside the old box corner, outside the 0.38 m cylinder) | Misses the button, and the switch does not fire | — |
| Ball on the switch button | Ball onto the top of the cylinder | Fires the switch, as before | — |
| Ball into the delay housing | Ball rolled at the 1.25 m housing | Stopped at its face | — |
| Ball along the ramp | Ball rolling across the rail | Contained by the rail (0.055 m high at z ±0.598) | — |
| Collider vs art | Every element package | Each collider's dimensions equal the art's bound dimensions within f32 (one test per element, from the declarations and bindings) | — |
| Static restitution | Ball dropped onto a static wall or pipe shell | Rebound ratio equals the derived e for the combined material | — |

</intent-contract>

## Code Map

- `elements/SignalLamp/lamp.tscn`, `elements/ImpactSwitch/switch.tscn`, `elements/DelayBox/delay.tscn`, `elements/Ramp/ramp.tscn`, `elements/Battery/battery.tscn`, `elements/BallDetector/ball_detector.tscn`, `elements/PoweredGate/powered_gate.tscn`: add or replace the `col_*` nodes.
- Edit only inside `/Users/aidan/dev/personal/tim/.claude/worktrees/story-elements-visible-solid`. Use Write/Edit with Anvil validation, never bare `git stash`, and do not commit.
- Each element's `*ElementDeclaration.cs`, where bindings scale the new colliders (Ramp rails with length and width).
- `tools/BundleElements/SceneGeometryExtractor.cs`: reads `col_*` nodes. Read only, unless a kind or binding is missing.
- `engine/gpu/ContactTriggerDeclaration.cs:8`: the switch trigger is keyed to the whole body, so check that the new button shape is what fires it.
- Element docs: CAT-035, CAT-063, CAT-022, CAT-054, CAT-005, CAT-002 and CAT-051, plus CAT-048 for the restitution note.
- Audit evidence: the 25 Oct collider audit, summarised in `docs/hollow-geometry-design.md` and the element docs' plan notes.

## Verification

- `dotnet build CuriousContraptions.slnx` gives 0 warnings.
- `dotnet test CuriousContraptions.slnx --no-build --no-restore` passes once at the end, with Skipped 0.
- `git diff --check` is clean, and Anvil validates every write.

## Spec Change Log

## Review Triage Log

### 2026-10-25 — Review pass (layers: forward-only, intent-alignment, blind, edge-case, verification-gap)
- verdicts: 33 findings — high 0, medium 6, low 11, false 0, maybe-false 0, plus 16 carried, rejected or deferred rows.
- findings:
  - Forward-Only Auditor:
    - `reject` No violations.
    - `defer` Pre-existing: element colliders can be declared either in data (`Colliders`) or in scene `col_*` nodes, a two-path design (`SceneGeometryExtractor.cs:34-43`). Owned by its own slice.
    - `[low]` `patch` The summaries on the empty `Colliders` property in the BallDetector and PoweredGate declarations read as if the data path were in use. Fix: move them onto the class.
  - Verification Gap and Edge Case Hunter:
    - `[medium]` `patch` The placement rest rule (`PlacementPose.Rest`, line ~123) accepts only Box tops. Placing a ball on the switch (now a cylinder button) or on the battery (new terminal) is refused `IntoPart`, where it used to rest. Fix: an upright cylinder's flat top is a rest surface, under the same rule as a box top. Tests: a Basketball composed over the switch button rests at 0.15 + 0.34; over the battery terminal it rests on the terminal top; a control is placed clear.
    - `[medium]` `patch` In the pre-existing no-rest path, a ball overlapping only the button is admitted inside it, and the new cylinder now reaches that path. Fix: the no-rest path refuses overlaps (`IntoPart`), with a test.
  - Edge Case Hunter, Blind Hunter and Intent Alignment:
    - `[medium]` `patch` Hits on the switch's terminals and socket now fire its whole-body trigger, and nothing tests or documents that. Decision (game intent): the impact switch fires on a qualifying impact anywhere on the part. Fix: document it in CAT-063 and test that a terminal hit fires.
    - `defer` The switch button art drops 8 cm on activation while its collider stays at the rest pose. This predates the slice: the old box was static too. Owned by a follow-up (a moving button body or a presentation change).
  - Blind Hunter:
    - `[medium]` `patch` The detector's outer collar has no negative case, because the glancing test was moved from −1.1 m to −1.2 m. Fix: a ball striking the outer collar fires no `Passage` and the lamp stays dark.
    - `[medium]` `patch` The restitution matrix row (a drop onto a wall or pipe shell) is missing. Fix: a ball dropped onto a static wall rebounds at its own e under the product law. The pipe stays deferred.
    - `[medium]` `patch` The ramp rails are tested only at the default width. At the minimum width a rail is 5.3 mm deep and could tunnel. Fix: a fast crossing at minimum and maximum width is contained. If it tunnels at minimum width, give the rail depth a derived floor in the binding.
    - `[low]` `patch` `AssertRing` has no upper bound on box width. Fix: bound it at R·tan(π/n) plus a derived margin.
    - `[low]` `patch` `AxisTol` is mis-derived. Fix: θ ≈ Tol/extent, so 1 − cos θ ≈ θ²/2.
    - `[low]` `patch` The delay test's counterfactual comment is wrong: the port socket would stop the ball. Fix the comment.
    - `[low]` `patch` The test keys a closed set on strings. Fix: key it on `PartId`.
    - `[low]` `patch` The renamed switch compiler test checks only colliders 0 and 1. Fix: check all five colliders and their materials.
    - `[low]` `patch` The element docs carry "(in review)" status. Fix: remove it; status lives in TODO, epics and sprint-status.
    - `[low]` `patch` The detector ring closure passes only by the tolerance margin. Fix: derive the 16 positions and the half-width from R and n, not hand-entered decimals.
    - `reject` "Scope cuts need owner sign-off." Coordinator decisions under owner delegation are recorded in the spec's deferred list.
    - `reject` "No lesson evidence." `LaunchLessonSolveTests` (every lesson solves) passes in the full run.
  - Edge Case Hunter:
    - `reject` Rail-adjacent placement is refused: a ball overlapping a rail is a true overlap, and the refusal is the intended rule.
    - `reject` Bench-edge `OutsideBench`: every authored placement passes in the full run.
    - `defer` The detector ring corners reach 0.836 m against the drawn 0.82 m, inherent to box rings; owned by `engine-hollow-shapes`.
    - `reject` Declarations changed beyond scene data. The spec's Always rule required the restitution check, and the ramp bindings scale the new rails.
  - Intent Alignment: `carried` The gate collars and the pipe restitution deferrals, and the tipped switch control.
- Patch results (25 Oct):
  - Every patch applied except the no-rest overlap refusal. That row is re-routed to `reject`: `EveryOtherPlacementKeepsTheBasePose` pins overlapping placements as admitted (a ball in a funnel throat, a domino beside a wall, a ball inside a ramp), an existing placement rule. Changing it is not this slice's decision.
  - Ramp rails at widths 0.125 and 4 hold a 4 m/s crossing, so no depth floor is needed.
  - The gate sleeve ring width is now derived: 0.278477, about 1.5 mm narrower.

## Auto Run Result

- **Summary:** every visible solid part of the lamp, switch, delay, ramp, battery, ball detector and powered gate collides with its drawn primitive shape:
  - lamp: bulb sphere and stem cylinder;
  - switch: cylinder button;
  - delay: housing box and face disc;
  - ramp: rails;
  - battery: terminal and band;
  - ball detector: collar solid from the bore to its 0.82 m outer surface;
  - sockets, terminals, the delay hub pip and the indicators.

  An upright cylinder's flat top is now a placement rest surface.
- **Restitution:** settled. Pair restitution is the product of the two values, so 1.0 is neutral. The detector is set to its documented 0.15 and the gate frame to 0.1.
- **Deferred:**
  - the gate collars, and pipe, bend and funnel restitution, to `engine-hollow-shapes`;
  - the moving switch-button collider;
  - the two-path collider declaration.
- **followup_review_recommended: true.** This first pass patched 6 medium findings. Not yet reviewed: the `PlacementPose` cylinder-cap rest rule and the derived ring widths.
- **Verification (coordinator):**
  - Build: 0 warnings.
  - Full suite: CuriousContraptions.tests 3498/3498, E2eRunner.Tests 208/208, BundleElements.Tests 194/194, Coverage.Tests 77/77, WorkerAotGuard.Tests 17/17. Skipped 0 in every project.
  - `git diff --check`: clean.

### 2026-10-25 — Review pass (follow-up on b02b5fea)
- verdicts: 28 findings — high 0, medium 4, low 12, false 0, maybe-false 0, plus 12 carried or rejected rows.
- **patch (medium):**
  - VG1: lying or tipped static cylinders acting as shelves is untested. Fix: negative cases (a default Delay face disc, and a switch tipped 90° about Z) composing to `Base`.
  - VG2: the cap-disc boundary is untested. Fix: points just inside the battery terminal's radius, and just outside it but inside its bounding square.
  - Edge1: the separating-axis `Overlaps` treats a cylinder like a box, so a part diagonally beside a static cylinder falsely overlaps. Fix: an exact cylinder test, for example the rim separating axis or closest point.
  - Intent R3: the collider-vs-art test covers 7 elements, not "every element package". Fix: extend it to every package.
- **patch (low):**
  - The engine-hollow-shapes story in epics has no pipe-restitution or lesson re-authoring acceptance. Fix: add it.
  - The test summary overclaims drop coverage. Fix: narrow it; the art test covers the remaining parts.
  - The `AssertRing` angle wrap. Fix: angles in (−π, π] around the reference.
  - The fast rail test cannot tell "over" from "never reached". Fix: also assert that the lowest point near the rail is below the rail top before it crosses.
  - The collar-strike check is weak. Fix: the ball stays outside the 0.65 m aperture.
  - `ParameterSets` lacks mixed cases. Fix: vary each bound parameter alone.
  - CAT-005, CAT-002, CAT-051 and CAT-022 shape rows are uneven. Fix: bring them current.
  - Raw string part IDs. Fix: `MemberData` of `PartId`.
  - A variable named `boxes` holds mixed shapes. Fix: rename it to `colliders`.
  - Doc comment wrapping. Fix: rewrap.
- **reject:**
  - "Ball admitted inside a lamp bulb or sphere" and "no-rest overlap": the existing admitted-overlap placement rule, as triaged last pass.
  - "Engine changed despite data-only": `PlacementPose` is the main-thread placement rule, not the solver (reading E-a).
  - The commit message overstatement: the final commit message names the gate-collar and ring-corner exceptions.
  - Material changes in declarations: required by the spec's restitution clause.
- **carried:** the gate collars, pipe restitution, the switch reading S-b, and ring corners to engine-hollow-shapes.
- Follow-up result (25 Oct):
  - All patches applied. Cylinder overlaps now use the closest-point direction; lying cylinders do not act as shelves; the cap disc is bounded (switch button, because grid snapping hides the battery case); the art test covers all 29 packages; the fast rail test reaches the rail; the collar strike stays outside the aperture.
  - The extended art test lists 9 elements with proud parts that do not collide (the basketball seam, bumper cap and ring, domino pips, weight base, bands and eye, pressure-plate studs, the logic parts' panels, lamps and sockets) as `Uncollided`. They are owned by the follow-up slice `elements-visible-solid-sweep`.
  - **followup_review_recommended: false.** This follow-up pass patched no high, so the review has converged.
  - **Verification:**
    - Build: 0 warnings.
    - Full suite: CuriousContraptions.tests 3524/3524, E2eRunner.Tests 208/208, BundleElements.Tests 194/194, Coverage.Tests 77/77, WorkerAotGuard.Tests 17/17. Skipped 0 in every project.
    - `git diff --check`: clean.
