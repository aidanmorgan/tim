---
title: 'Elements: the remaining proud parts collide as drawn'
type: 'bugfix'
created: '2026-10-25'
status: 'done'
baseline: a8ba8944
review_loop_iteration: 0
followup_review_recommended: true
context:
  - '/Users/aidan/dev/personal/tim/.claude/worktrees/story-elements-visible-solid-sweep/AGENTS.md'
  - '/Users/aidan/dev/personal/tim/.claude/worktrees/story-elements-visible-solid-sweep/docs/testing-guidance.md'
  - '/Users/aidan/dev/personal/tim/.claude/worktrees/story-elements-visible-solid-sweep/docs/gpu-f32-physics.md'
warnings: []
deferred:
  - summary: >-
      engine-hollow-shapes: the tori stay Uncollided (the Basketball seam, the Bumper's upper ring, the Weight's bands and lifting eye, the pressure plate's ring) until that slice adds the torus kind.
    evidence: |-
      No torus collider kind exists; a filled disc would be a new approximation of a round hollow part and would hold a centred ball off the pressure plate's sensing face. VisibleSolidColliderTests lists each; CAT-001, CAT-015, CAT-052 and CAT-067 name the owner.
  - summary: >-
      engine-multi-collider-dynamic: several colliders on a moving body (mass and inertia from the declaration, colliders as shape only), a lower sphere radius minimum, and the pressure plate's five-collider limit.
    evidence: |-
      PhysicsDeclarations, PhysicsBodyReadSet and PhysicsGpuAbi admit one collider per dynamic body (the Domino's pips, the Weight's base); ColliderDeclaration.Validate's sphere radius minimum refuses the Clock's pivot cap, the plate's studs, the pips, the truth dots and the latch's beads; a sixth plate collider reaches its contact-load row's identity, which WorkshopPhysicsCompiler.AdmitColliderSlots now refuses.
  - summary: >-
      bumper-cap-solid: make the Bumper's cap solid and re-author bumper_sidekick, bumper_depth and wall_and_bumper.
    evidence: |-
      As a cylinder collider the cap met a ball 0.3 m off the axis at its rim (contact normal 8.5° from vertical against the head's 17.6°) and the three lessons' authored solutions stopped solving (LaunchLessonSolveTests, F32BehaviourIdentityTests); the collider was withdrawn.
  - summary: >-
      face-markings-flush: move the raised face markings flush (the logic parts' pictograms, path bars, marks and truth dots, the hold timer's track, the latch's set, reset and out markers), so they become printing.
    evidence: |-
      They float 1–2 cm off their face and stand 1.5–8.5 cm proud of it; VisibleSolidColliderTests lists them as Uncollided, owned by that slice.
---

<intent-contract>

## Intent

**Problem:** the owner's rule is "visible = solid" (25 Oct 2026). The collider-vs-art test (`VisibleSolidColliderTests`, which covers all 29 packages) lists nine elements whose visible parts stand proud of the body without a collider. They are marked `Uncollided`:
- Basketball: the seam stripe.
- Bumper: the cap and the upper ring.
- Domino: the pips.
- Weight: the base, the bands and the lifting eye.
- Pressure plate: the studs, sockets and ring.
- The five logic parts (Both gate and the electrical NAND, NOR, OR and XOR), Hold timer, Counter, Latch and Clock: their face panels, lamps or indicators, and sockets.

**Approach:** add colliders as data, using the existing Sphere, Box and Cylinder kinds and each part's drawn dimensions and bindings, exactly as `elements-visible-solid` did. Each part then moves from `Uncollided` to `Solid` in the test.
- **Dynamic bodies** (Basketball, Domino, Weight): the new parts join the body as compound colliders. Mass and inertia must stay as declared; colliders do not add mass.
- **Flush or near-flush printed detail** (pips, seam): if a part stands proud of the surface by less than a stated threshold, it is classified `Decorative`, with the threshold derived and documented. Example derivation: under 2 mm, below the contact slop of 0.2 mm × 10. Only parts at or above the threshold collide.
- When this slice is done, the `Uncollided` category is empty and is deleted from the test.

## Boundaries & Constraints

**Always:**
- Use the real production C# simulation with behavioural assertions, and keep Skipped at 0. No mutation testing.
- Keep each collider bound to its art.
- Keep every lesson solving (`LaunchLessonSolveTests`), the energy gates at their current bounds, and the `F32BehaviourIdentityTests` tolerances unchanged. If a lesson moves past them, report it as a finding.
- Update each element's doc to describe the colliders.
- Edit only inside the slice worktree, using Write/Edit with Anvil validation. Do not commit.

**Never:**
- A box-ring approximation.
- An engine or solver change.
- A mass change from a collider.
- Loosening an assertion.
- A fallback or second rule.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Ball dropped onto the bumper cap | A ball over the cap | Stops at the drawn cap surface | — |
| Ball rolled into a logic part's face panel or lamp | A ball rolled at the face | Stops at the drawn face | — |
| Ball onto the pressure-plate studs | A ball over a stud | Rests at the stud height, and plate activation is unchanged | — |
| Weight with its base, bands and eye | The existing Weight tests | All pass, with mass and inertia as declared | — |
| Collider vs art | All 29 packages | No `Uncollided` remain, and the category is deleted | — |

</intent-contract>

## Code Map

- `elements/{Ball,Bumper,Domino,Weight,PressurePlate,BothGate,ElectricalNand,ElectricalNor,ElectricalOr,ElectricalXor,HoldTimer,Counter,StateLatch,SystemClock}/*.tscn`: the `col_*` nodes.
- `CuriousContraptions.tests/VisibleSolidColliderTests.cs`: move each part from `Uncollided` to `Solid`, then delete `Uncollided`.
- `docs/planning/elements/CAT-*.md` for each element above.

## Verification

- `dotnet build CuriousContraptions.slnx` with 0 warnings.
- `dotnet test CuriousContraptions.slnx --no-build --no-restore` passes once at the end, with Skipped 0.
- `git diff --check` is clean, and Anvil validates every write.

## Spec Change Log

### 2026-10-25 — Coordinator scope amendment (after step-03)
- **Trigger:** the implementer showed that the remaining `Uncollided` parts cannot collide through data alone.
- **Amended scope:** this slice delivers the parts that can be made solid as data:
  - the face, lamps or indicators and sockets of the logic parts, hold timer, counter, latch and clock;
  - the pressure plate's sockets.
- `Uncollided` stays. Each remaining entry names its owner slice:
  - **Tori** (basketball seam, bumper upper ring, weight bands and eye, pressure-plate ring): owned by `engine-hollow-shapes`, which adds the torus kind.
  - **Compound moving bodies** (domino pips, weight base): owned by a new engine slice, `engine-multi-collider-dynamic`. A moving body takes several colliders, with mass from the declaration and colliders as shape only. The same slice relaxes the 1/16 m minimum sphere radius (the clock pivot cap, the plate studs and the pips are 0.045–0.055 m) and the pressure plate's five-collider limit, where the sixth collider's id collides with the weight-sensing row.
  - **Bumper cap:** a new slice, `bumper-cap-solid`, makes it solid and re-authors `bumper_sidekick`, `bumper_depth` and `wall_and_bumper` (game intent: visible = solid).
  - **Raised face markings** (pictograms, truth dots, path bars, the hold-timer track and the latch markers) sit 1–2 cm off the face. A new slice, `face-markings-flush`, moves the art flush, so they become true printed decoration.
- **KEEP:** every delivered collider, test and doc update in this slice.
- **Known-bad states avoided:** loosening the classification threshold, filling the ring as a disc, and a solid bumper cap without re-authored lessons.

## Review Triage Log

### 2026-10-25 — Review pass (5 layers)
- Forward-Only found no violations, and Verification Gap found no gaps.
- **Patched (medium):**
  - Owner slices are named on every `Uncollided` entry, and the bumper cap is recorded as decided.
  - The raised markings move to `Uncollided` (`face-markings-flush`).
  - A typed refusal stops collider ids from reaching a part's own row slots.
  - New behaviour tests: a rolled ball into a face, drops onto a lamp, indicator, face and socket, and the plate socket's restitution.
- **Patched (low):**
  - The clock recess collides.
  - The counter's ActivationOut collider is removed (it is inside the foot).
  - The self-comparing tests are deleted, with a note of what covers them.
  - The StateLatch tests compare all fields, the helper is shared, and the docs are fixed (CAT-037, 015, 052, the bench-edge notes).
  - The `LogicLamp` read window is bounded.
  - Collider growth is recorded for the performance gate.
- **Rejected:** placement overlapping a sphere is the existing admitted-overlap rule.

## Auto Run Result
- **Delivered:** the faces, lamps, indicators and sockets of the logic parts, hold timer, counter, latch and clock collide as drawn, and so do the pressure plate's sockets.
- **Still open, each with an owner slice:**
  - tori (basketball seam, bumper ring, weight bands and eye, plate ring);
  - compound moving bodies (domino pips, weight base);
  - spheres below the radius minimum;
  - the bumper cap;
  - the raised face markings.
- **followup_review_recommended: true.** This first pass patched ≥ 2 medium findings. Not yet reviewed: `AdmitColliderSlots` and the new behaviour tests.
- **Verification:**
  - Build: 0 warnings.
  - Full suite: CuriousContraptions.tests 3532/3532, E2eRunner.Tests 208/208, BundleElements.Tests 194/194, Coverage.Tests 77/77, WorkerAotGuard.Tests 17/17. Skipped 0 in every project.
