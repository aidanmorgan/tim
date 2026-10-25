---
title: 'Engine: well-posed contact block solve for coplanar manifolds'
type: 'bugfix'
created: '2026-10-25'
status: 'blocked'
baseline: d87b6a1e
review_loop_iteration: 1
followup_review_recommended: false
context:
  - '/Users/aidan/dev/personal/tim/.claude/worktrees/story-engine-cap-block/AGENTS.md'
  - '/Users/aidan/dev/personal/tim/.claude/worktrees/story-engine-cap-block/docs/testing-guidance.md'
  - '/Users/aidan/dev/personal/tim/.claude/worktrees/story-engine-cap-block/docs/gpu-f32-physics.md'
warnings: []
deferred: []
---

<intent-contract>

## Intent

**Problem:** a cylinder cap or a box face resting on a plane gives four coplanar contact points. Their 4×4 normal coupling matrix has rank 3, so only the diagonal regularisation makes `SolveNormalBlock` invertible, and it returns a huge alternating null-space solution. On a 1 kg rod one sweep returned −33.8, +33.7, −33.7 and +33.7 N·s.

An accumulated impulse then goes negative, and 52–73 % of cap block solves drop to the sequential per-row recovery. Rim impacts keep more energy than a perfectly inelastic impact allows: 0.833–0.856 against 0.8307. The rod-righting oracle therefore carries a declared +0.01 retention margin.

The same simultaneous-then-per-row structure is in `ApplyRestitution`.

**Approach:** one exact rule for every multi-point normal block, and for the restitution block. It is the linear-complementarity solution found by fixed-order active-set enumeration, extending Box2D v2's two-point enumeration to n ≤ 4:
- Try the active sets in a fixed, documented order: all points, then each subset of size n−1, n−2 and so on down to single points, in a deterministic index order.
- Accept the first set whose reduced system has a well-conditioned solution with every impulse ≥ 0, and whose inactive points do not approach (relative normal velocity after the solve ≥ the rule's target).
- Skip a rank-deficient set (relative pivot below a stated tolerance); it is simply not a valid candidate.
- With exact enumeration nothing remains to recover from, so the per-row recovery paths in `SolveNormalBlock` and `ApplyRestitution` are deleted. Their branch logic (the coherent slop band, `NormalBranch`) stays the rule that defines each row's target.

## Boundaries & Constraints

**Always:**
- Use the real production C# simulation and behavioural assertions, with derived bounds and Skipped 0.
- Keep f32 throughout, with one current path. The coherent slop band (`NormalBranch`, `ManifoldSoftness`, `ContactPass`) and the rolling axes per manifold are unchanged.
- Keep the energy gates at their current bounds and seeds, and the `F32BehaviourIdentityTests` tolerances (12 ticks, 5.1 mm) unchanged. If a lesson moves past them, report it as a finding.
- Write the enumeration rule, its order and its tolerance into `docs/gpu-f32-physics.md`. Delete the doc text about the per-row recovery.
- Edit only inside `/Users/aidan/dev/personal/tim/.claude/worktrees/story-engine-cap-block`, using Write/Edit with Anvil validation. Do not commit.
- No mutation testing (owner, 25 Oct). Tests assert the changed result directly.

**Never:**
- A fallback to per-row solving, a second rule for some manifold sizes, or a mode or flag.
- A sleep or velocity threshold, or double precision.
- Loosening an existing assertion.
- Changing the ABI or save format.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Cap resting flat | Cylinder r 0.1 and r 0.4 standing on a plane, at rest | Every block solve accepts a set on the first well-conditioned candidate. No impulse is negative or larger than the body's weight impulse for the substep (m·g·h) by more than f32 rounding. Resting penetration is ≤ 0.5 mm. | — |
| Rod rocking on its rim | The predecessor's rod (r 0.1, h 0.4, 1 kg, Dead) released at 10.5° and 12.5°, azimuths 0, 22.5 and 45 | Every impact's kept energy is ≤ the one-substep inelastic model (no declared margin). It never passes over the rim (peak < 14.04°). It is upright (tilt < 1°) by the model's settle time T with the +0.01 margin removed from `InelasticRockingSettleSeconds`. | If the retention still exceeds the model, report the numbers and do not re-add a margin |
| Box face drops | The existing 60-box face sweep | Drift ≤ 1e-9 m/s, unchanged | — |
| Two-point line contact | Lying cylinder; the coherent slop band cases in `WorkshopPhysicsLawTests` | Unchanged results | — |
| Restitution on a flat face | A box or cap landing flat with e > 0 | Leaves flat: the per-point bounce velocities agree within f32 rounding, and no point is pulled | — |
| Rank-deficient full set | Four coplanar points | The all-points set is skipped and a well-conditioned subset is accepted. A test asserts the accepted set's impulses are all ≥ 0 and its inactive points do not approach. | — |

</intent-contract>

## Code Map

- `CuriousContraptions.Simulation/WorkshopConstraintSolver.cs`:
  - `SolveNormalBlock` (~453): the block build, `BlockRegularization`, `SolveLinear`, and the two per-row recovery paths (~489–501).
  - `ApplyRestitution` (~645–690): the simultaneous restitution with its per-point recovery (`ApplyRestitutionRow`).
  - `CouplingMass`, `SolveLinear`: reuse them for the reduced systems.
  - Delete the recovery paths. Remove `BlockRegularization` if the enumeration no longer needs it, or keep it only if the doc derives why.
- `CuriousContraptions.tests/WorkshopPhysicsLawCylinderTests.cs`:
  - `InelasticRockingSettleSeconds`: remove the +0.01 declared margin.
  - `ARodRightsItself…`.
  - New: cap-at-rest and per-impact retention tests.
- `CuriousContraptions.tests/WorkshopPhysicsLawTests.cs`: the band row tests now go through the enumerated block (they currently name the fallback). Restate them on the accepted set, and add the rank-deficient case.
- `CuriousContraptions.tests/WorkshopPhysicsLawRestitutionTests.cs`: the flat-landing cases.
- `docs/gpu-f32-physics.md`:
  - §One Constraint Row for Every Pair (the block rule);
  - "Rocking on a cap rim" (drop the margin);
  - Resting Drift (close defect (c));
  - the Restitution section.
- `_bmad-output/implementation-artifacts/spec-engine-orientation-levelling-draft.md` §(c): the diagnosis, the numbers and the candidates (this spec picks candidate 2).

## Tasks & Acceptance

**Execution:**
- Tests first: write the matrix tests and confirm that the rod-retention-without-margin case and the cap-rest impulse bound are red at d87b6a1e, with the observed numbers in the failure message.
- `WorkshopConstraintSolver.cs`: the enumerated block for the normal rows and for restitution; delete both recovery paths.
- Docs and tests: as mapped above.

**Acceptance Criteria:**
- Given d87b6a1e plus this change, when the matrix tests run, then each passes with Skipped 0.
- Given the full solution suite (one Debug build, then `dotnet test CuriousContraptions.slnx --no-build --no-restore`), then every project passes with Skipped 0. That includes the energy gates, `F32BehaviourIdentityTests`, `LaunchLessonSolveTests`, `WeightTests`, the restitution laws and the Reset bit-identity test.

## Verification

- `dotnet build CuriousContraptions.slnx` gives 0 warnings and 0 errors.
- `dotnet test CuriousContraptions.slnx --no-build --no-restore` passes once at the end, with Skipped 0 and no `--blame-crash`.
- `git diff --check` is clean, and Anvil validates every write.

## Spec Change Log

Implementation, 25 Oct 2026:
- **The empty set is the last candidate.** It is Box2D v2's "both inactive" case. Without it, a block whose points all separate, or whose band points' allowance covers their approach, accepts no set.
- **One rule for every manifold size, including one point.** `SolveNormalRow` and `ApplyRestitutionRow` are deleted. For one point the sets {0} then {} are the clamped row.
- **Removed as dead.**
  - `BlockRegularization`.
  - `SoftCoefficients.ImpulseScale`: it equals 1 − MassScale, so the soft row is MassScale times the rigid total.
  - `ContactPoint.NormalMass`: it is no longer read.
- **Coincident restitution pair.** The rank-deficient pair is skipped and the first point alone is accepted with the whole impulse; both points leave at the target. The old "equal shares" assertion described the deleted regularised solve. It is replaced, not loosened.
- **Red at d87b6a1e** (`scratchpad/cap-baseline-red.log`):
  - Rod retention without the margin: red at 3 of 6 releases, worst 0.8561 against 0.8483.
  - Cap at rest: red through the regularisation's residual approach after one block solve, −6.0e-6 and −2.4e-5 m/s against 1.25e-6. The impulse bound alone, every λ in [0, m g h], was not violated: each λ was 5.1e-3 against 2.04e-2 N·s.
  - The rank-deficient case, flat-landing restitution and the exact two-point band block were also red.
- **"Accepts on the first well-conditioned candidate"** holds for a symmetric resting cap. A micro-rocking cap (q differing by 25–65 ulps) would pull its unloaded corner, so the next three-point set is accepted: 2–7 % of a standing cylinder's solves. No block in the diagnosis runs accepted no set.
- **Full suite: 2 failures, blocking (owner decision needed).** The run was 3485 tests in `CuriousContraptions.tests`, 2 failed and 0 skipped; every other project passed.
  - `BoxesDroppedOntoAFaceStayPutAtRest`: 25 of 60 boxes drift above 1e-9 m/s (worst 2.05e-8 m/s).
    - Mechanism: a resting face has a one-parameter family of complementarity solutions, and the enumeration takes a vertex of it (one diagonal pair loaded).
    - Micro-rocking of 25–65 ulps flips the accepted three-point set: 268–375 flips over 6 s on the failing boxes #5, #17 and #39, and 1 on the passing box #2.
    - Each flip moves the per-point friction cones, and that walks the box.
  - `FreshSeedDropsGainNoMoreEnergyThanTheBoxControlAndTheSlop`: CylinderOntoBox (Dead), 1 of 200 cases (case 179) gains 2.447 mJ in a tick against the 1.999 mJ bound. Before the slice the worst was 1.657 mJ against 2.045 mJ.
    - The gain tick (97, a rim on the box's edge at 48° tilt) has single-point blocks only. Their solve is the former clamped row up to rounding.
    - The trajectory diverged earlier, at multi-point cap contacts. So this is a pre-existing single-point susceptibility, newly reached, not the block rule acting in that tick.
    - The gate's box control also moved: its bound fell from 2.045 to 1.999 mJ.
- **Attempt 1, reverted:** try the set of points carrying accumulated impulse first, then the fixed order.
  - It cut the switches (29–145) but did not clear the drift: 2.1e-9 to 9.0e-9 m/s, and passing box #2 regressed to 2.1e-9.
  - It left gate case 179 unchanged at 2.447 mJ.
  - The tree is back on the spec's literal order.
- **Not tried, by direction:** a larger complementarity tolerance. It only moves the flip threshold, and it would break the rounding-level derivation.
- **Likely resolution, for a decision:** the per-point friction interaction belongs to the follow-up's draft B.2 (central friction anchor and twist row). Alternatively, use candidate 1's minimum-norm split for the rank-deficient family.

### 2026-10-25 — Coordinator amendment (bad_spec, iteration 1): minimum-norm least-squares block
- **Trigger:** fixed-order enumeration returns a vertex solution (two diagonal corners carry a resting face). Tiny rocking flips the chosen vertex 268–375 times in 6 s, which moves the per-point friction limits and walks the box. `BoxesDroppedOntoAFaceStayPutAtRest` fails: 25 of 60 boxes, worst 2.05e-8 m/s. The energy gate also regressed on Dead case 179: 2.447 mJ against 1.999 mJ.
- **Amended (design, outside the intent contract):** the accepted solution for an active set is the **minimum-norm least-squares** solution λ = K⁺·b, not a vertex.
  - K⁺ is the pseudo-inverse with a structural rank cutoff: drop eigen- or pivot directions below 1e-3 of the largest, Box2D v2's conditioning limit.
  - For coplanar points the rank is fixed by geometry (3 for a face, 2 for a line), so the cutoff sits in a wide spectral gap. A resting face's load spreads evenly and changes continuously with pose.
  - An inconsistent right-hand side is projected out instead of amplified. That inconsistency is the source of the old ±34 N·s null-space impulses.
  - Non-negativity: if any λ < 0, that set is rejected and the fixed-order active-set search continues. The order, the inactive-point no-approach test and the empty set as the last candidate are unchanged. Restitution uses the same solve.
- **Known-bad states avoided:**
  - a vertex (corner) solution that flips under micro-rocking;
  - regularised inversion of an inconsistent b, which gave the huge alternating impulses;
  - any per-row recovery or second rule.
- **KEEP:**
  - one function for every block size, including single points (`SolveNormalRow` and `ApplyRestitutionRow` deleted);
  - deleting both recovery paths, `BlockRegularization`, `SoftCoefficients.ImpulseScale` and `ContactPoint.NormalMass`;
  - the empty set as the last candidate;
  - the 2⁻¹⁶ velocity-scale acceptance tolerance;
  - all new tests: rod retention with no margin, standing-cap stop and rest, the coplanar complementarity case, flat landing;
  - the restated band and restitution tests;
  - the margin removed from `InelasticRockingSettleSeconds`, with T recomputed from the model;
  - the docs rule text, with the solution choice updated.
- **Acceptance unchanged:** every matrix row, the box face drift ≤ 1e-9 m/s and the energy gates at their bounds. If case 179 still exceeds its bound under the minimum-norm solve, report the tick's contacts and numbers; do not change the gate.
- **Iteration 1 result (implementer, 25 Oct 2026): stopped, still red; no bound changed and no full-suite run, per the instruction.**
  - **Implemented.**
    - `SolveMinimumNorm`: pivoted Cholesky to the structural rank, with `RankCutoff` 1e-3, then K⁺ = P L (LᵀL)⁻² Lᵀ Pᵀ.
    - It replaces `SolveLinear`, and `PivotTolerance` becomes `RankCutoff`. No set is skipped for rank.
    - Everything under KEEP is kept.
    - The coincident restitution pair's assertion returns to equal shares, since that is the minimum-norm split.
    - The comments in the coplanar, standing-cap and flat-landing tests are restated.
  - **Cylinder, law and restitution classes:** 262 tests, 259 passed, 3 failed, 0 skipped (`scratchpad/cap-minnorm-classes.log`). The three failures:
    - `BoxesDroppedOntoAFaceStayPutAtRest`: 14 of 60 boxes over 1e-9 m/s, worst 2.08e-9 (the vertex version gave 25 of 60, worst 2.05e-8).
    - `FreshSeedDropsGainNoMoreEnergyThanTheBoxControlAndTheSlop`: CylinderOntoBox (Dead) has 1 of 200 cases over the bound, worst 2.297 mJ against 2.023 mJ (vertex version 2.447 against 1.999). Every other class is within its bound.
    - `ACylinderSetDownExactlyLevelOnItsSideDoesNotDrift`: 2.15e-9 m/s against 1e-9. This is new under the minimum-norm solve; the vertex version passed it.
      - It is a two-point line contact, which is full rank. The likely cause is the normal-equation rounding of (LᵀL)⁻², which squares the conditioning.
      - A Householder QR of L, or Cholesky triangular solves where the set has full rank, would give the exact solve back. That has not been tried, per the stop instruction.
  - **Still passing:** rod retention, with no impact over the model (closest 0.8465 against 0.8484), and every other matrix test.
- **Iteration 2 (the coordinator's last bounded attempt on this design, 25 Oct 2026): stopped, still red.** No bound was changed and the full suite was not run, per the instruction.
  - **Implemented.**
    - `SolveMinimumNorm` is now one rank-revealing method for every set: a cyclic Jacobi eigen-decomposition (`JacobiSweeps` 6, fixed) with x = Σ vᵢ(vᵢ·b)/eᵢ over eigenvalues above `RankCutoff` 1e-3 of the largest. The pivoted Cholesky is deleted.
    - New test `OnAFullRankLyingSideTheBlockGivesTheDirectSolvesImpulses`: a lying cylinder's full-rank two-point block matches a direct double-precision 2 × 2 solve within 64·2⁻²⁴·cond. It passes.
    - Docs: the Jacobi method and the spectral-gap numbers. The drift gate's thinnest face keeps its smallest eigenvalue at 0.053 of the largest, 53 times the cutoff.
  - **Cylinder, law and restitution classes:** 263 tests, 262 passed, 1 failed, 0 skipped (`scratchpad/cap-jacobi-classes.log`).
    - `ACylinderSetDownExactlyLevelOnItsSideDoesNotDrift` passes again.
    - `BoxesDroppedOntoAFaceStayPutAtRest` still fails: 4 of 60 boxes over 1e-9 m/s, worst 2.26e-9 m/s.
  - **Energy gate: it passes, but only because its bound moved.** The box-onto-box control that sets the bound now gains far more energy:
    - Dead: bound 3.156 mJ (was 1.999–2.045), so the control gains 1.19 mJ over the slop. CylinderOntoBox worst 2.447 mJ.
    - Rolling: bound 6.077 mJ (was 2.052–2.079), so the control gains 4.12 mJ over the slop. CylinderOntoBox worst 2.421 mJ (was 1.488).
    - Lively: bound 1.962 mJ, unchanged in kind; all classes within it.
    - So case 179 still gains 2.447 mJ, above the earlier bound. The gate is green only through a box-onto-box energy regression in the control, a finding against this design.
  - **Case 179's energy-jump tick** (tick 97, rise 2.447 mJ, tilt 48.15°, centre 0.173 m above the box top) has **only single-point blocks**:
    - One cylinder–box point, feature 0x4042, at about (0.297, 0.137, −0.336) m, with normal about (0.72–0.74, 0.67–0.69, −0.085 to −0.097).
    - It sits 6.5–7.0 mm inside the box.
    - Per substep, the solve-pass impulses climb 0.034 → 0.043 N·s and the relax-pass impulses fall 0.0087 → 0.0001 N·s (q −0.12 to −0.15 m/s in solve).
    - The gain is the soft push-out of a 7 mm-deep single-point rim-on-edge penetration, the same in every form of the block. This trajectory reaches that penetration.

## Review Triage Log

## Design Notes

**Why enumeration.** With four coplanar points the true solution set is a 1-dimensional family: the null space. Regularised inversion picks an arbitrary huge member of it, so impulses alternate and a negative λ forces the recovery path.

Enumerating active sets in a fixed order returns the first physically valid, well-conditioned solution: impulses ≥ 0 and no inactive point approaching. It is deterministic and bounded. For n ≤ 4 there are at most 15 subsets, each a 1–4 dimensional solve. It needs no convergence loop and no second rule.
