---
title: 'Engine: lying cylinder axial creep'
type: 'bugfix'
created: '2026-10-24'
status: 'done'
baseline: 9568500d
review_loop_iteration: 0
followup_review_recommended: false
context:
  - '/Users/aidan/dev/personal/tim/.claude/worktrees/story-9a-44/AGENTS.md'
  - '/Users/aidan/dev/personal/tim/.claude/worktrees/story-9a-44/docs/testing-guidance.md'
  - '/Users/aidan/dev/personal/tim/.claude/worktrees/story-9a-44/docs/gpu-f32-physics.md'
warnings:
  - oversized
deferred:
  - summary: >-
      A single-point contact on one curved cylinder side resists rolling only about its own axis, leaving pitch unresisted with no second normal row.
    evidence: |-
      Edge Case Hunter, 25 Oct; maybe-false. Settle it by measuring whether a tilted lying cylinder at single-point contact levels or rocks differently with both tangents resisted.
    location: >-
      CuriousContraptions.Simulation/WorkshopConstraintSolver.cs RollingAxis
    severity: medium (unverified)
---

<intent-contract>

## Intent

**Problem:** A solid cylinder lying on its curved side does not stay put on the f32 engine. When it settles slightly tilted, it creeps along its own axis indefinitely, and while rolling it steers like a cone.

The 10.2a Weight's flush-ramp roll test fails because of this once main is merged with 9A.44A: peak world |z| is 6.07 mm against a 0.5 mm bound, with 5.0e-5 m/s of creep at rest. That failure blocks publishing 9A.44A. The diagnosis (24 Oct, scratch copies merge-probe/main-probe) showed:
- the placed pose and compiled rows differ only by Half→f32 rounding;
- the failure has the known signature: a 1.47e-4 rad ledge tilt and a frozen committed velocity of (≈0, −1.1e-4, −1.05e-5).

**Approach:** Two general contact laws in the shared solver, with no element-specific branch:
- **M1, manifold-coherent soft band:** in a multi-point manifold where any point is soft (s = separation + slop ≤ 0), every other point in the touching band (separation ≤ 0 < s) is also soft in the solve pass, with bias = biasRate·s, and gets no speculative allowance in the relax pass. Points that are apart (separation > 0), and single-point manifolds, keep today's rule. `SolveNormalRow`, `SolveNormalBlock` and its per-row fallback all use one branch helper.
- **M2, rolling axes per manifold:** the rolling-resistance row resists relative angular velocity only about the rolling axes of the manifold's curved supports, projected into the tangent plane:
  - a sphere: both tangents;
  - one curved cylinder side: its own axis;
  - two parallel curved sides: their common axis;
  - crossed curved sides, or any sphere: both tangents;
  - no curved support: no row, as today.

Measured on f32 at 9568500d+main: M alone and M+M2 turn the Weight roll test green and keep every energy gate, law, restitution, identity and lesson-solve class green.

## Boundaries & Constraints

**Always:**
- Edit only inside `/Users/aidan/dev/personal/tim/.claude/worktrees/story-9a-44`, using Write/Edit with Anvil validation before each write. Do not commit: the coordinator reviews and commits. Never use bare `git stash`.
- Use the real production C# simulation in every test, with behavioural assertions only (no hashes or byte identity), and derive every bound independently. Skipped tests are 0 throughout.
- Keep f32 throughout, with one current path: no toggle, mode flag, fallback or compatibility branch in production.
- Leave the energy gates (`FreshSeedDropsGainNoMoreEnergyThanTheBoxControlAndTheSlop`, its Lively twin, `RandomDropsGainNoMoreEnergyThanABoxAndTheSlop`) at their current bounds and seeds.
- Leave the `F32BehaviourIdentityTests` tolerances (12 ticks, 5.1 mm) unchanged. If either law moves a lesson past them, report it as a finding; do not widen them.
- Write the deviation from Box2D v3's per-point branch rule and the rolling-axis law into `docs/gpu-f32-physics.md`.
- The slice starts from the unpublished 9568500d (9A.44A), not from published main. This is deliberate: the fix is a technical prerequisite for publishing 9A.44A, and both publish together after main is merged again.

**Never:**
- A sleep, rest or velocity-snap threshold ("No rest threshold zeroes velocity").
- Double precision, or hidden uncommitted pose state.
- The compensated orientation remainder (draft mode C, M3) in this slice. On f32 it breaks `FreshSeedDropsGainNoMoreEnergyThanTheBoxControlAndTheSlop` (CylinderOntoBox Dead, worst 4.109 mJ against a bound of 2.043 mJ). If the PRIMARY tests below still fail with M1+M2 because of sub-ulp orientation levelling (M3), stop and report the residual numbers; do not add C or any other remainder.
- Changing the body record layout, the ABI or the save format.
- Loosening any existing assertion, including the Weight tests merged later.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Weight-geometry flush-ramp roll (entry check, red at baseline) | LawScene cylinder r 0.3517, half-width 0.2793, 4 kg, material e 0.08 / bounce 0.1 / μ 0.3 / Crr 0.03, on its side on the 10.2a flush ramp onto the bench, as in `WeightTests.OnItsSideItRollsDown…` | World \|z\| ≤ 0.5 mm throughout; ≤ 0.05 mm of movement after stopping; displacement along the body's own axis ≤ 0.5 mm | No error expected |
| Drop-ramp roll | The same body dropped 3 mm onto the 15° drop ramp | World \|z\| ≤ 0.5 mm throughout and ≤ 0.05 mm after stopping. If it fails, attribute the steady rolling yaw (draft D4) by each row's yaw torque per substep; fix it if it is an engine asymmetry, otherwise report with numbers | Report, never loosen |
| Lying drops | ≥ 200 seeded random lying drops: r 0.1–0.4, half-width 0.1–0.6, 0.5–5 kg, tilt ±0.05 rad, drop 0.5–150 mm, Crr {0, 0.03, 0.1}, μ {0.1, 0.3, 0.6}, e 0–0.3 | After a 4 s settle: mean axial speed < 1e-6 m/s over 6 s, tilt < 1e-5 rad, resting penetration ≤ 0.5 mm, speed < 2 mm/s | Failing case and its numbers in the message |
| Placed micro-tilt | A cylinder on its side tilted 2e-6, 1e-5 or 1e-4 rad about the transverse axis; a box standing on its end tilted about its large-rotation axis | Drift < 1e-6 m/s over 8 s; levels to tilt < 1e-5 rad | If it fails only through M3, report it (see Never) |
| Level set-down (control) | A cylinder set down with exactly zero tilt | Bit-still: drift 0 over 10 s | No error expected |
| Box face (control) | Box drop sweep, four-point faces | Unchanged: worst creep ≤ 1e-9 m/s | No error expected |
| Sphere rolling | Existing sphere rolling and rest set, including `ABallRollingAlongPlusZAboutXAlsoComesToRest` | Unchanged pass; a sphere's rolling row still resists both tangents | No error expected |
| Rocked lying cylinder | Pitch rocking on its side | Damped only by the normal rows; the rolling row resists the axis only | No error expected |

</intent-contract>

## Code Map

- `CuriousContraptions.Simulation/WorkshopConstraintSolver.cs`:
  - `SolveNormalRow` and `SolveNormalBlock`, with its per-row fallback when the block is singular or an accumulated λ would go negative: get one shared branch helper that takes a manifold-has-soft-point flag (M1).
  - `PrepareRolling`, `RollingArm`, `SolveRolling`: use per-manifold rolling directions in place of the point-0 T1/T2 (M2).
- `CuriousContraptions.Simulation/WorkshopCollisionKernels.cs`: `ContactManifold` gains the internal row directions (`Roll1`, and `Roll2` or none) and enough support information to classify curved sides and spheres. `CollideCylinderPlane` is unchanged.
- `CuriousContraptions.tests/WorkshopPhysicsLawCylinderTests.cs`: the rest and gate sections take the new PRIMARY tests (lying drops, micro-tilt, level control, rocked pitch, Weight-geometry flush and drop ramps as LawScene scenes).
- `docs/gpu-f32-physics.md`: §One Constraint Row for Every Pair (the band rule and its Box2D deviation; rolling axes per manifold), and §Game-Grade Envelope (a "resting drift" row).
- `_bmad-output/implementation-artifacts/deferred-work.md`: close "Engine: lying cylinder axial creep".
- **Reference only (scratch, do not copy toggles):**
  - `/private/tmp/claude-501/-Users-aidan-dev-personal-tim/7e56a042-6670-44d4-ae0f-4efd972de00e/scratchpad/merge-probe/CuriousContraptions.Simulation/WorkshopConstraintSolver.cs`: the toggled M and 8 code under `CREEP_FIX`. The original is `merge-solver.orig.cs` beside it.
  - `/private/tmp/claude-501/-Users-aidan-dev-personal-tim/7e56a042-6670-44d4-ae0f-4efd972de00e/scratchpad/spec-engine-cylinder-creep-draft.md`: the full Half-era diagnosis of M1–M3.

## Tasks & Acceptance

**Execution:**
- `CuriousContraptions.tests/WorkshopPhysicsLawCylinderTests.cs` -- write the PRIMARY tests from the matrix first and confirm the entry check and the lying-drop sweep are red at 9568500d, with the observed creep in the failure message -- proves the defect is reached.
- `CuriousContraptions.Simulation/WorkshopCollisionKernels.cs` and `WorkshopConstraintSolver.cs` -- M2 rolling axes, then the M1 shared branch helper -- the two general laws.
- `docs/gpu-f32-physics.md`, `deferred-work.md` -- the laws, the Box2D deviation and the envelope row; close the deferral -- documentation stays current.
- Mutation check (once, small) -- revert M1 alone and M2 alone; a PRIMARY test must go red for each -- proves both laws are necessary.

**Acceptance Criteria:**
- Given 9568500d with M1+M2, when the matrix tests run, then each passes with Skipped 0. The only exception is a micro-tilt or sweep residual that is attributable to M3, which is reported with numbers per Never.
- Given the full solution suite (`dotnet test CuriousContraptions.slnx --no-build --no-restore`, after one Debug build), then every project passes with Skipped 0, including the energy gates at their unchanged bounds, `F32BehaviourIdentityTests`, `LaunchLessonSolveTests`, the restitution laws, and `TwoRunsAndAResetReadmissionAreBitIdenticalForSixHundredTicks`.
- Given main 686bdfd0 merged afterwards with the saved resolution (`scratchpad/merge-686bdfd0-resolution`), when `WeightTests` runs, then it passes unchanged. The coordinator checks this at publication.

## Verification

- `dotnet build CuriousContraptions.slnx` → 0 warnings, 0 errors.
- `dotnet test CuriousContraptions.slnx --no-build --no-restore` → Skipped 0, Failed 0, run once at the end. Never use `--blame-crash` on the whole solution.
- `git diff --check` is clean, and Anvil validates every write.

## Spec Change Log

### 2026-10-25 — Scope amendment after step-03 (owner decision and a coordinator spec correction)
- **Trigger:** the implementer reported 10 red tests after M1+M2:
  - **Rod righting:** `ARodRightsItself…` at 12.5°, azimuths 22.5/45, regressed. It drops under 1° only at 4.09 s and 4.48 s, after the 4 s sample. This is caused by this spec's requirement that the per-row fallbacks also get the manifold flag. Without the flag there, the rod matches baseline exactly.
  - **M3 residuals** (sub-f32-step orientation levelling):
    - 180/200 lying drops fail the axial-drift clause at 1.0e-6 to 2.2e-5 m/s, with every other clause green;
    - micro-tilt 1e-4 fails for the cylinder (7.6e-6) and for the box on end (7.5e-6, red at baseline);
    - micro-tilt 1e-5 fails at 2.5e-6, exposed by M1.
  - **Drop ramp:** world z 1.25 mm against 0.5 mm, from a one-off landing yaw kick of 1.77e-4 rad. The kick comes from unequal friction at the two generator ends and depends on the friction-row solve order.
- **Amended:**
  1. **M1:** ~~the per-row fallbacks keep the per-point rule~~. Withdrawn the same day on the owner's challenge ("we shouldn't have per-point rules should we?"). There is one normal-branch rule, the manifold-coherent band, in `SolveNormalRow`, the block solve and both per-row fallbacks. The rod-righting regression is resolved under that single rule: either fix a defect in the shared rule, or derive the righting-time bound independently from the soft-band rocking damping. Never loosen it on observation, never use a second rule.
  2. **Scope (owner, 25 Oct: "Land M1+M2, follow-up"):** this slice commits only tests that pass.
     - The lying-drop sweep keeps its tilt, penetration and rest clauses, and drops the axial-drift clause.
     - Micro-tilt keeps 2e-6. The 1e-5 and 1e-4 cylinder cases, the box on end, and the drop-ramp world-z test move to the follow-up engine slice "orientation levelling and two-point friction". Their exact scenarios and measured residuals are recorded in that slice's draft (`scratchpad/spec-engine-orientation-levelling-draft.md`), never committed red.
  3. **Docs:** the "Resting Drift" envelope row states the measured residuals as an open defect owned by the follow-up slice, not as an accepted bound.
  4. **Rod-righting oracle (owner, 25 Oct: "Re-derive the test oracle"):** under the single rule, `ARodRightsItself…` was red only on timing, settling at 4.09 s (azimuth 22.5) and 4.48 s (azimuth 45) against an undocumented 4 s sample. The test is restated by its named behaviour:
     - **Below atan(r/h) = 14.04°:** peak tilt stays below the rim angle, so the rod never passes over the rim.
     - **Above it:** the rod topples, unchanged.
     - **Settling:** the rod is upright (tilt < 1°) by a time derived from the inelastic rigid rocking model: 0.8307 energy kept per impact, giving 4.34 s from 12.5°. The model also includes the solver's one-substep impact allowance (m·g·R·h) and a small declared margin. The margin covers the 3 of 41 measured impacts that exceed the model, by at most 0.008 of retention. The derivation and the declared margin are written in the test and in `gpu-f32-physics.md`.
     - **Pre-existing defect, routed to the follow-up slice:** the 4-point cap block is rank 3. That sends 60–70% of cap solves to the negative-λ per-row recovery, with alternating ±34 N·s impulses.
- **Known-bad states avoided:**
  - a committed red test, or a loosened bound written to absorb the residual;
  - the fallback flag that delays rod righting;
  - adding the compensated remainder C, which breaks the Dead energy gate.
- **KEEP:**
  - M2 rolling axes as implemented: `Roll1`/`Roll2` and the shared `LineAlignment` constant;
  - the `NormalBranch` helper;
  - the origin-placed set-down cases with the exact level quaternion, and the 1e-9 m/s level-control bound;
  - the energy gates at their unchanged bounds and seeds;
  - the mutation evidence: no-M1 turns the flush ramp and micro-tilt 2e-6 red; no-M2 turns rocking and micro-tilt 2e-6 red;
  - the docs for the band rule, its Box2D deviation and the rolling axes.

## Review Triage Log

### 2026-10-25 — Review pass (layers: forward-only, intent-alignment, blind, edge-case, verification-gap)
- verdicts: 27 findings — high 0, medium 7, low 12, false 0, maybe-false 1, plus 7 carried or rejected rows.
- findings:
  - Forward-Only Auditor:
    - `reject` No violations. Three borderline items were judged allowed:
      - the per-row recoveries pass the manifold flag;
      - the band rule takes a geometric input, not a mode;
      - the one- or two-axis rolling row is chosen by geometry.
  - Intent Alignment:
    - `reject` (1) The entry check is a LawScene copy, not the real `WeightTests`. The spec's acceptance criteria assign the real test to the coordinator at publication, after main is merged again.
    - `[low]` `patch` (2) The open-defect (b) doc text says "a 0.17 m drop", but the matrix scenario is a 3 mm drop onto the 15° ramp. Fix: state the exact scenario.
    - `reject` (3) The axial clause is gone from the lying drops. The owner moved it to the follow-up slice.
    - `[medium]` `patch` (4) The micro-tilt "levels" assertion is vacuous: the start tilt 2e-6 is already below the 1e-5 bound, and the doc says tilts below 5e-6 are never levelled (M3). Grouped with Blind 1.
    - `reject` (5) The level-control bound is 1e-9 m/s rather than bit-still. Bit-still cannot be reached even at the origin (1.3e-11 m/s of baseline solver round-off), and this is a new test, not a loosened one.
    - `[medium]` `patch` (6) The "unchanged" sphere cases are covered only by existing tests, and the parallel, crossed and B-only M2 branches are untested. Grouped with Verification Gap 2.
    - `carried` (7) The rod oracle's declared margin is the owner's decision (3).
    - `[low]` `patch` (8) The doc says single-point and apart points "keep the per-point rule". Fix: describe one rule, under which those cases reduce naturally.
  - Blind Hunter:
    - `[medium]` `patch` (1) The micro-tilt test claims levelling, and the doc contradicts it. Fix: rename the test to what it proves (drift < 1e-6 m/s, and the tilt does not grow), and correct the "levels" sentence in the Resting Drift row.
    - `reject` (2) An axial-drift ceiling set at the measured residual would accept the residual as a bound, which the owner rejected (option 3).
    - `[medium]` `patch` (3) `RollingAxis` projects through `m.Points[0].Normal`, which can be a cap or rim normal. That can give NaN, and the comment's bound is wrong. Fix: use an axis-support point's normal, check the projection length, and correct the comment.
    - `[medium]` `patch` (4) The rolling-axis branches are untested. Grouped with Verification Gap 2.
    - `[medium]` `patch` (5) The manifold band has no direct test, and the fallback-false mutant survives. Grouped with Verification Gap 1.
    - `[low]` `patch` (6) The box-face comment and the doc say faces are "unchanged by this slice's laws", but M1 applies to four-point faces. Fix: say the test is M1's box regression check.
    - `[low]` `patch` (7) No retention from before the slice is recorded. Fix: record the baseline rim retention (about 0.76) beside the band's 0.833–0.856. The margin itself is the owner's decision (carried), and `SubstepSeconds` is the declared engine constant.
    - `[low]` `patch` (8) The rocking test uses exact float equality. Grouped with Edge Case 3. Fix: a derived tolerance well under the predicted 6 % effect.
    - `[low]` `patch` (9) The Weight constants are copied by hand. The Weight package exists only on main. Fix: cite the exact source (10.2a Weight size law at b88ca425).
    - `[low]` `patch` (10) The manifold pool reset does not clear `Roll1`/`Roll2`. Fix: reset them with the other rolling fields.
    - `[low]` `patch` (11) Bookkeeping: name the deleted legacy (the duplicated inline per-point branch in `SolveNormalRow` and `SolveNormalBlock`) in the doc. TODO.md and committing the spec happen at publication.
  - Edge Case Hunter:
    - `[medium]` `patch` (1) The parallel-sides test compares projected tangents, while the narrowphase compares the raw axes. Grouped with (5). Fix: one criterion, the raw axes' |dot| against `LineAlignment`, as the narrowphase does.
    - `[maybe-false]` `defer` (2) A single-point contact on one curved side resists only its own axis, so pitch is unresisted with no second normal row. If true, it would be medium (unverified). To settle it, measure whether a tilted lying cylinder at single-point contact levels or rocks differently with both tangents.
    - `[low]` `patch` (3) Exact float equality. Grouped with Blind 8.
    - `[low]` `patch` (4) `InelasticRockingSettleSeconds` hangs when θ ≥ α. Fix: throw `ArgumentOutOfRangeException`.
    - `[medium]` `patch` (5) The parallel-axis claim and the code disagree. Grouped with (1).
  - Verification Gap:
    - `[medium]` `patch` (1) The fallback-`false` mutant passes all 266 tests, so the owner's single rule is not pinned. Fix: a solver-level or scene case that fails under that mutant.
    - `[medium]` `patch` (2) The removed parallel-bisector branch and the removed B-curved branch both pass all tests. Fix: a parallel lying-on-lying cylinder with Crr > 0, whose pitch rocking does not depend on Crr, plus a crossed-pair control where Crr does change the outcome.
    - `[low]` `patch` (other) The box-face comment. Grouped with Blind 6.

### 2026-10-25 — Review pass (follow-up on 02593110)
- verdicts: 24 findings — high 3, medium 3, low 8, false 0, maybe-false 0, plus 10 carried or rejected rows.
- findings:
  - Forward-Only Auditor (each confirmed finding is high, per the owner gate):
    - `[high]` `patch` `AxisSupportNormal` ends with `return m.Points[0].Normal`, an unreachable fallback. Fix: `RollingArm` returns the axis-support point with the radius, `RollingAxis` takes its normal, and `AxisSupportNormal` is deleted.
    - `[high]` `patch` The `length >= .5f ? … : null` guard switches a short projection to the two-tangent rule, a second rule. Fix: delete it and normalise directly; the invariant gives a length ≥ 0.98. First verify the invariant holds at the narrowphase sites that set `ContactSupport.Axis` without the `SideAlignment` test (`WorkshopCollisionKernels.cs:937, 990, 1533, 1603`). If it does not, those sites are fixed so that every axis-support point satisfies it.
    - `[high]` `patch` `gpu-f32-physics.md:217` writes the fallback rule ("a shorter one … both tangents") into the contract. Fix: delete it and keep the 0.98 invariant.
  - Intent Alignment:
    - `carried` Earlier divergences: entry-check surface, level control, micro-tilt, lying drops, drop ramp, box and sphere "unchanged", rocked damping.
    - `[low]` `patch` The M1 unit test covers only the relax pass and the negative-λ recovery. Fix: add the solve-pass band bias (biasRate·s), and a mixed band/soft manifold whose block solve succeeds.
    - `carried` (owner decision 3) The rod oracle with its declared margin.
  - Blind Hunter:
    - `[low]` `patch` Wrong arithmetic in the band-test comment: λ_sep is −0.064, not −0.004.
    - `[low]` `patch` The band test is white-box, sits in the cylinder class, and does not exercise solve-pass or block success. The internal solver row is the law's defined interface, since a scene cannot force the recovery path deterministically. Grouped with the Intent M1 row. Fix: move it to `WorkshopPhysicsLawTests`, which owns contact-row laws.
    - `[medium]` `patch` The crossed-log control cannot catch a single-axis rule. Grouped with VG 3.
    - `[medium]` `patch` The antiparallel branch is untested. Grouped with VG 1.
    - `[low]` `patch` The cap-chord test assumes pair order. Fix: assert in the test that the manifold reaches the B-only-curved case (collider order), or build the pair so the order is explicit.
    - `[low]` `patch` The micro-tilt test checks only the final tilt. Fix: assert the peak tilt over the run, and correct the comment about windows.
    - `[low]` `patch` The Resting Drift headline overclaims "face or cap … does not walk", and "M1" is an undefined spec label. Fix: narrow the headline to what is tested and name the rule in words.
    - `reject` The lying-drops axial ceiling: owner decision (option 3 rejected).
    - `carried` (owner decision 3) Rod retention and margin.
    - `[low]` `patch` The Weight constants are copied. Fix: at merge, the coordinator binds the test to `WeightElementDeclaration`.
    - `[low]` `patch` `SolveNormalRow` takes adjacent `bool manifoldSoft, bool useBias` parameters. Fix: closed-set enums (the AGENTS enum rule) and `private`.
    - `reject` The "redundant" `Roll1`/`Roll2` reset was requested last pass. It is ordinary pool hygiene.
  - Edge Case Hunter:
    - `carried` (defer) The single-point curved-side pitch case.
    - `[low]` `patch` `deferred-work.md` is not updated. Fix: at merge, the coordinator closes "Engine: lying cylinder axial creep" and adds the follow-up "orientation levelling and two-point friction" entry.
  - Verification Gap:
    - `[medium]` `patch` (1) The antiparallel bisector is unverified; mutant M1 passes all tests. Fix: an antiparallel `[InlineData]` (`AboutX(-90)`) on the parallel-pitch test.
    - `[medium]` `patch` (2) Resistance about a parallel pair's common axis is never asserted; mutant M2 passes. Fix: a parallel log rolling about Z loses spin faster with Crr 0.1 than with Crr 0.
    - `[medium]` `patch` (3) The crossed test's assertion is too weak; mutant M7 passes. Fix: assert its own derived stop (resisted < 0.1 × free at 0.35 s).
    - `reject` (M8) The singular-matrix recovery passing `false`: with `BlockRegularization` it is unreachable for realistic inverse masses. It is covered by the shared helper's single signature.

### 2026-10-25 — Review pass (follow-up 2 on 60d6a14c)
- verdicts: 22 findings — high 0, medium 3, low 7, false 3, maybe-false 2, plus 7 carried or rejected rows.
- findings:
  - Forward-Only Auditor:
    - `reject` No violations. The borderline items were checked and allowed.
    - `[low]` `defer` The prismatic `SweepConstraints(bool useBias)` is pre-existing and falls under the enum rule (also flagged by Blind Hunter).
  - Intent Alignment: `carried` Every divergence repeats an earlier row: level control, micro-tilt, box, lying drops, drop ramp, rod model, and the Weight surface (now bound to the declaration in merge 54622267).
  - Verification Gap:
    - `[medium]` `patch` (1) Deleting the sphere guard passes every test, because every ball–cylinder scene has Crr 0. Fix: a ball rolling along a static lying log with Crr 0.1, slowed against Crr 0.
    - `[medium]` `patch` (2) The `LineAlignment` 2° boundary is untested; replacing it with 26° passes. Fix: skew cases inside and outside 2° on the parallel-pitch test.
    - `[medium]` `patch` (3) The tangent-plane projection is untested; removing it passes. Fix: a row-level test with n·U1 ≈ 0.15. After the rolling row, spin about n is unchanged within f32 rounding and spin about the projected axis is reduced.
  - Blind Hunter:
    - `[false]` `reject` "The onlyA branch is never run." VG mutant m-line-273 fails 6 tests.
    - `[low]` `patch` The 2° boundary is untested. Grouped with VG 2.
    - `[medium]` `patch` The sphere shortcut is untested. Grouped with VG 1.
    - `[false]` `reject` "Dropping `separation <= 0` passes every test." The apart-point mutant fails 9 energy tests.
    - `[low]` `patch` The projection bound: with the parallel bisector, |n·b| can reach ≈ 0.216, giving ≈ 0.976, not 0.98. Fix: correct the comment and the doc. Pin the invariant with a test that every axis-support point the narrowphase emits satisfies |n·U1| ≤ `SideAlignment`, on the current sites and a seeded pose sweep. Do not add a guard: a guard would be a second rule (forward-only gate).
    - `[maybe-false]` `reject` The normal frame mismatch (`Points[0]` versus the side point). VG shows every manifold the narrowphase builds gives all its points one support kind, so the mutant behaves identically.
    - `defer` The prismatic bool. Grouped with the Forward-Only row.
    - `reject` The lying-drops axial ceiling: owner decision. `[low]` `patch` The lying-drops resting gap has no lower bound. Fix: assert the body rests in contact (gap ≤ slop).
    - `[maybe-false]` `patch` The box face bound may sit below one f32 step if a box rests away from the origin. Fix: assert each box rests within the radius where one f32 step is far below the bound, or scale the bound by the f32 step at the rest position.
    - `carried` (owner decision 3) The rod margin.
    - `reject` The normal-branch expectations use the declared soft coefficients and slop. Those are the law's parameters, not its logic.
    - `[false]` `reject` "The Weight constants are copied." Merge 54622267 reads them from `WeightElementDeclaration`.
    - `reject` Ceiling tests for the open defects: owner decision (option 3 rejected).
    - `[low]` `patch` The pre-9A.44 save contract was deleted from `gpu-f32-physics.md`, which no longer describes the behaviour. Fix: state it. Such a save is refused as `VerifierRejected` at both boundaries and never migrated. Grouped with Edge 2.
    - `reject` The parallel-spin 0.3 rad/s threshold is declared. It is mutation-proven against parallel mass 0, and the roll-over kinematics are not modelled.
    - `[low]` `patch` The Resting Drift row cites only the cylinder class. Fix: cite the band unit tests in `WorkshopPhysicsLawTests` too.
  - Edge Case Hunter:
    - `[low]` `patch` The unguarded projection. Grouped with Blind's bound row: pin the invariant with a test, with no guard.
    - `[low]` `patch` The deleted save contract. Grouped with Blind.

## Design Notes

**Why M1.** The normal row's branch is per point: s > 0 is rigid, with an s/h speculative allowance, and s ≤ 0 is soft. A loaded soft row sags about 70–100 µm, while a rigid row does not sag. So a two-point line contact can rest with one end on the rigid ledge and the other in the soft sag. That leaves a tilt of about sag/length, which the friction rows (r below the centre of mass) turn into axial walking. The rigid end is offered its allowance again every substep. Making the whole touching band soft whenever a sibling point is soft removes the ledge only where it coexists with a sag.

**Why M2.** On a line contact the second tangent is pitch, which the two normal rows already carry. Resisting it holds a tilt against the levelling torque. The excess leaks through the axial friction rows.

**Why not C here.** Exact orientation integration (the compensated remainder) fixes sub-ulp levelling (M3), but on f32 it overruns the CylinderOntoBox Dead energy gate. M3 is a separate problem: it may need its own design (and an owner decision on the committed pose) if the PRIMARY tests show a residual.

## Auto Run Result

- **Summary:** two general contact laws in the shared solver stop a lying cylinder from creeping along its axis on the f32 engine. This unblocks publishing 9A.44A: the 10.2a Weight flush ramp creeps under f32.
  - **M1:** a manifold-coherent soft band, as one normal-branch rule (`NormalBranch`). It applies in the row solve, the block solve and both per-row recoveries.
  - **M2:** rolling axes per manifold. A sphere or crossed sides resist both tangents; one curved side resists its own axis; parallel sides resist their common axis (the raw-axis test, the same as the narrowphase).
- **Legacy deleted:** the per-point branch that `SolveNormalRow` and `SolveNormalBlock` each duplicated inline.
- **Owner decisions (25 Oct):**
  - "Land M1+M2, follow-up": the M3 orientation-levelling residual and the drop-ramp landing yaw kick move to the follow-up slice, drafted in `scratchpad/spec-engine-orientation-levelling-draft.md`.
  - "we shouldn't have per-point rules": one rule on every solve path.
  - "Re-derive the test oracle": the rod's peak stays below the 14.04° rim, and it settles by the inelastic rocking model's T (5.09 s from 10.5°, 5.51 s from 12.5°) with a declared +0.01 retention margin.
- **Files:**
  - `WorkshopConstraintSolver.cs`: `NormalBranch`, `RollingAxis`, `PrepareRolling`.
  - `WorkshopCollisionKernels.cs`: `ContactManifold` gains `Roll1`/`Roll2`, and `LineAlignment` is now shared.
  - `WorkshopPhysicsEngine.cs`: the pool reset covers the new fields.
  - `WorkshopPhysicsLawCylinderTests.cs`: the PRIMARY tests, the restated rod oracle, the solver-level band test, and the parallel, cap-chord and crossed rolling-axis tests.
  - `docs/gpu-f32-physics.md`: the laws, the Box2D deviation, the rocking derivation, and the Resting Drift open-defect row.
- **Review findings:**
  - Patched: 18 entries, 0 high, 7 medium, 11 low.
  - Deferred: 1, a maybe-false single-point side-contact pitch case.
  - Rejected: 6:
    - the entry-check surface (the real `WeightTests` run is the coordinator's at publication);
    - the axial clause and the axial-drift ceiling, both owner decisions;
    - the bit-still level control, which round-off makes unreachable;
    - the Forward-Only borderline items, all allowed.
  - Carried: the owner's rod margin.
- **followup_review_recommended: true.** The first pass patched 7 medium findings. Not yet reviewed: the `RollingAxis` rewrite (axis-support normal, NaN guard, raw-axis parallel test) and the new solver-level and rolling-axis tests.
- **Verification (coordinator):**
  - `dotnet build CuriousContraptions.slnx`: 0 warnings, 0 errors.
  - `dotnet test CuriousContraptions.slnx --no-build --no-restore`: CuriousContraptions.tests 3424/3424, E2eRunner.Tests 208/208, BundleElements.Tests 191/191, Coverage.Tests 77/77, WorkerAotGuard.Tests 17/17. Skipped 0 in every project.
  - Anvil: 0 findings. `git diff --check`: clean.
  - Mutations (implementer logs in `scratchpad/creep-slice/`): no-M1, no-M2, fallback-`false`, no-parallel-bisector, no-B-curved and rim-0.85·r each turn a committed test red.
- **Residual risks:**
  - The real `WeightTests` runs only after main is merged again (at publication).
  - The M3 and drop-ramp residuals are an open defect owned by the follow-up slice.
  - The rod settle clause has not been shown to catch a plausible defect; only its rim-angle clause has a mutant.
  - Browser qualification is pending post-catalogue.

### Follow-up pass 1 (25 Oct, on 02593110)
- **Patched:** 3 high (forward-only), 3 medium, 8 low.
  - The fallback in `AxisSupportNormal` is gone: `RollingArm` returns the axis-support point.
  - The short-projection guard is deleted. The 0.98 invariant was verified at every `ContactSupport.Axis` site, and none needed fixing.
  - The doc clause stating the fallback is gone.
  - New tests: antiparallel pitch, a parallel roll about the common axis, and the crossed log stopping at its own derived time.
  - The band tests moved to `WorkshopPhysicsLawTests`. They cover the relax pass, the solve pass, and a block success, and the comment arithmetic is fixed.
  - The cap chord asserts its pair order, and the micro-tilt test asserts the peak tilt.
  - `SolveNormalRow` is private and takes the `ManifoldSoftness` and `ContactPass` enums.
  - The docs are narrowed and name the "coherent slop band" in words.
- **Mutants now caught:** fallback-`false`, no-parallel, no-B, plain-sum bisector, parallel mass 0, and no `LineAlignment` gate (implementer logs `scratchpad/creep-slice/review2-*`).
- **Rejected:** 3 (the axial ceiling, the pool reset, M8). **Carried:** the remaining rows.
- **followup_review_recommended: true.** This follow-up pass patched a high. Not yet reviewed: the `RollingArm`/`RollingAxis` signature change, the enum plumbing through six engine call sites, and the new band tests.
- **Verification (coordinator):**
  - Build: 0 warnings.
  - Full suite: CuriousContraptions.tests 3429/3429, E2eRunner.Tests 208/208, BundleElements.Tests 191/191, Coverage.Tests 77/77, WorkerAotGuard.Tests 17/17. Skipped 0 in every project.
  - Anvil: 0. `git diff --check`: clean.

### Follow-up pass 2 (25 Oct, on 60d6a14c)
- **Patched:** 0 high, 3 medium, 7 low.
  - New tests: a ball rolling on a lying log, skew cases inside and outside 2°, a row-level tangent-plane projection test, and an invariant test (2,547 points: every projected axis-support point satisfies |n·U1| ≤ `SideAlignment`, with no guard in the code).
  - Lying drops assert contact at rest. Box drops assert the rest radius within which one f32 step stays below the bound.
  - The bound is corrected (0.98 for one side, 0.976 for the parallel bisector).
  - The pre-9A.44 save contract is restated in the docs.
- **Rejected:** 3 false, 1 maybe-false and 5 others. **Deferred:** the prismatic bool (enum rule).
- **Mutation testing:** deferred to the post-catalogue E2E phase (owner, 25 Oct, main 9853aea8). The implementer's last mutant checks ran before that decision arrived.
- **followup_review_recommended: false.** This follow-up pass patched no high, so the review has converged.
- **Verification (coordinator, after merge 54622267):**
  - Build: 0 warnings.
  - Full suite: CuriousContraptions.tests 3472/3472, E2eRunner.Tests 208/208, BundleElements.Tests 194/194, Coverage.Tests 77/77, WorkerAotGuard.Tests 17/17. Skipped 0 in every project.
  - `git diff --check`: clean.
