---
title: "Engine: restitution energy correctness"
status: review
baseline: cf35a0645c719e2551fc7ffa2473416d404da8a0
---

# Outcome and authority

Restitution stops adding energy. Qualified (native, seeded): single spheres, boxes and cylinders on static supports in random poses, a box landing on a resting dynamic box, and the Story 10.1 cylinder classes, at every e ≤ 1; other multi-body configurations rely on the energy-bounded pass (D13). Three outcomes follow:
- a bounce returns at most e² of the approach energy;
- no committed tick gains more than m·g·`ContactSlop` (1.962 mJ at 1 kg);
- resting contacts and stacks never bounce.

The pass is the Box2D v3 restitution pass, mapped onto this engine's 480 Hz substep, and it is one shared typed law with no shape or element branch.

**Authority:**
- AGENTS.md: game-grade envelope; numerical residuals clamp or continue and never fault the tick; shared typed laws only; refactor forward with no fallback or shim; enums end to end.
- `docs/gpu-f32-physics.md`:
  - §3 TGS Soft: 4 solve and 4 relax iterations per substep at h = 1/480 s;
  - §Narrowphase: "dissipative restitution", product restitution, maximum bounce threshold;
  - §Speculative contacts;
  - §Game-Grade Envelope: Thermodynamic Energy "no free energy", Normal Velocity Residual, Maximum Velocities (clamped again after restitution).
- Box2D v3 `b2ApplyRestitution` semantics, as stated in the brief:
  - the pass runs after relax;
  - each manifold point uses its own relative normal velocity, stored at prepare;
  - a point bounces only when its approach exceeds the threshold and it carried normal impulse;
  - the target is −e·v_n, with the accumulated impulse clamped ≥ 0;
  - no soft-contact or penetration bias enters the target.
- Defect source: the 10.1 implementer's round-5 evidence. The Lively set (e = 0.3 against the bench) failed 1–4 of 200 cases per class against a box control of 283 mJ (bound 285.317 mJ, worst 514.917 mJ). See `.claude/worktrees/story-10-1/_bmad-output/implementation-artifacts/evidence-10-1/round5/full-gate.log` and `WorkshopPhysicsLawCylinderTests.FreshSeedDropsWithRestitutionGainNoMoreEnergyThanTheBoxControlAndTheSlop` (Explicit, red by design).

**Scope.** This is an engine slice. It ships no catalogue element, `PartId`, package or bundle id, and the isolation ratchet is unchanged. Production C# native tests on the real `WorkshopPhysicsEngine` are PRIMARY. Browser proof of changed trajectories joins the post-catalogue E2E queue.

**Baseline.** TBD-after-10.1, the merge that lands cylinders and the Lively gate. The diagnosis was made on main ae3cacf9. The candidate patch applies cleanly onto the 10.1 worktree.

**Legacy deleted:**
- the cross-substep approach memory: the `Approach` lane of `_contactCache`, `MathF.Min(vn, warm.Approach)` and the cache write `(c.Bounced || CurrentSeparation(c) <= 0f) ? 0f : c.RelVel`;
- `ContactPoint.Bounced`;
- the separation guard `CurrentSeparation(c) > 0f` in restitution and in contact-event emission;
- per-point-only restitution `ApplyRestitution(ContactPoint)`, replaced by the manifold pass.

# Root cause, with evidence

All measurements below use the scratch copy of main ae3cacf9, with 1 kg bodies, a 0.5 × 0.2 × 0.3 m box and a 0.1 m sphere. The per-tick rise is E(t) − E(t−1). The **gain** is E(t) minus the maximum of the three preceding ticks: an impact that removes energy in one tick and returns its bounce in the next does not count, so a gain is genuine.

1. **Primary: the bounce is deferred, and the deferred target is stale.**
   - A point approaching from the speculative gap is stopped by the relax pass's speculative row (s > 0, bias s/h). That stop removes the approach while the point is still apart.
   - `ApplyRestitution` then skips the point because `CurrentSeparation(c) > 0`. Instead the engine caches `approach = c.RelVel`, and the next substep bounces the point with `RelVel = min(vn, cachedApproach)`.
   - Evidence, a sphere at e = 0.3: at ss176 the point skipped with gap 7.498 mm, sep +0.002 mm, relVel −3.598, maxImp 3.50 and vn −0.097. At ss177 the cached −3.598 drove +1.080 m/s, adding 583 mJ. Tick 44 is −6476 mJ and tick 45 is +575 mJ.
   - Where the bounce falls across a tick boundary, the per-tick rise equals the whole bounce energy. That is the 10.1 gate's failure signature.
   - The cache is also sticky: `min` keeps the fastest approach a hovering speculative point ever had. Bodies whose pose changes between the stop and the touch (spinning boxes, edge landings) then bounce on a stale target. That is a genuine gain.

   | Case (main) | Worst per-tick rise | Worst genuine gain | Measurement |
   | --- | --- | --- | --- |
   | Sphere e = 0.3 / 0.55 / 0.6 | 575 / 2469 / 2992 mJ | 0 / 0 / 0 mJ | 40 random poses |
   | Box on bench, e = 0.3 / 0.55 / 0.6 / 1 | 286 / 1025 / 1226 / 4342 mJ | 283 / 149 / 128 / 3140 mJ | 40 random poses |
   | Box on box, e = 0.3 / 0.55 / 0.6 / 1 | 284 / 600 / 1969 / 7189 mJ | 282 / 9 / 54 / 1042 mJ | 40 random poses |

2. **Secondary: each point is applied separately on a multi-point manifold.** Each point is driven to its own −e·v_n in sequence, so a symmetric landing leaves with spurious spin and the wrong ratio.
   - A flat box at e = 0.3 leaves spinning at 1.80 rad/s. Its apex ratio is 0.0555 against e² = 0.09, and v_out/v_in is 0.211.
   - At e = 0.6 the apex ratio is 0.2245 against 0.36. At e = 1 it is 0.579, the box never settles, and one tick gains 1574 mJ.
   - A flat box-on-box drop at e = 0.6 gains 45 mJ.
3. **Secondary: the approach includes the current substep's gravity.** `PrepareContact` runs after gravity, drag, guides and drives. A flat sphere drop at e = 1 gains 14.3 mJ in one tick, more than the 1.962 mJ slop.
4. **Exposed once the bounce happens in the same substep: the bounce gets almost no friction.** Box2D's pass applies no friction. Box2D solves friction over a step's four substeps before its one restitution pass; here each substep is a whole step, so friction gets a single relax pass.
   - When the stop comes in the relax pass, the bounce carries almost no tangential impulse. A spin-10 basketball strike gives Δv_t = 0.058 m/s at drag 0 and 0.272 m/s with drag, so the result depends on the substep phase.
   - At e = 1, random-pose boxes still gain 186 and 438 mJ (Kane-type).

**Hypotheses checked and rejected:**
- *Restitution should run once per tick, not every substep.* Every 480 Hz substep re-collides, prepares, runs 4 solve iterations, integrates and runs 4 relax iterations, so each substep is one complete Box2D step. One pass per substep is the faithful mapping (D1).
- *The target uses post-bias velocity.* No: the impulse acts on the post-relax velocity, and relax carries no soft bias. The defect is in the stored approach, not the bias.
- *There is no threshold.* There is one: the maximum of the pair's bounce thresholds.
- *Resting contacts bounce.* No. The resting approach is about g·h = 0.02 m/s, below the 0.1 m/s thresholds. Stacks show no restitution after about 0.03 s (see the stack evidence).

# Finite design decisions

- **D1. One restitution pass per substep, after relax and before `SettleVelocity`** (the second velocity clamp), unchanged in position. It answers the brief's per-tick hypothesis: the substep is the step.
- **D2. Eligibility, as in Box2D.** A point bounces iff restitution ≠ 0, `RelVel < −Threshold`, and the point carried normal impulse this substep (`MaxImpulse > 0`), whether touching or speculative.
  - There is no separation guard and no cross-substep approach memory. The contact cache keeps (λN, λT1, λT2) only.
  - The bounce therefore happens in the same substep as the stop, so a single impact cannot straddle a tick. (A body pushed into a support by another body's bounce meets that support in a later substep as a new impact; D13 bounds what the pass may return.)
- **D3. The approach comes from substep-start velocities.** Each dynamic body snapshots v and ω at the top of the substep (`SimBody.V0/W0`), before guides, supplied drives, gravity and drag. `PrepareContact` stores `RelVel = n·(v_A(r_A) − v_B(r_B))` from those velocities, with the prepare-time anchors. This is Box2D's "prepare before integrate velocities".
  - *Alternative rejected:* subtracting g·h. It measured identically for these scenes, but it misses guides, drives and drag.
- **D4. Restitution is solved per manifold.** All bouncing points of a manifold are solved simultaneously:
  - K·Δλ = −(v_n + e·RelVel), using `CouplingMass` with the same trace-scaled `BlockRegularization` as `SolveNormalBlock`.
  - If K is singular, or any accumulated λN + Δλ < 0, the points are applied one at a time instead (`ApplyRestitutionRow`, accumulated impulse clamped ≥ 0).
  - A single eligible point uses the row directly.
  - This deviates from Box2D's per-point loop, consistent with the engine's existing block normal solver. It is justified by flat landings: the apex ratio goes from 0.0555 to 0.0858 at e = 0.3.
  - Measured use over 40 random drops per material (instrumented copy, D1–D4 without D5): 97–127 block solves, 0–14 per-point fallbacks, 125–935 single points. No case's worst gain comes from a fallback.
- **D5. After a manifold bounces, friction and rolling are solved once more.** Each point gets one `SolveFriction` and the manifold one `SolveRolling`; their cones (μλN, C_rr·r·ΣλN) now include the restitution impulse.
  - Without the bounce impulse, an impact would carry less than its Coulomb share of tangential impulse.
  - This is a stated deviation from Box2D's pass (see root cause 4).
  - **Regime check, done:** Basketball r = 0.34 m and I = 0.4·m·r² (compiled solid sphere), so m_t = m/7. The pre-impact slip is ω·r = 3.40 m/s, so m_t·slip₀ = 0.486 N·s. The normal impulse is Λ = (m/2)(1+e)v = 1.3025 N·s, so μΛ = 0.391 < 0.486: the impact slides throughout.
    - Post-impact slip is +0.644 m/s on the candidate and +1.29 on base: same sign, no reversal. D5 does not over-apply friction.
    - Base under-applies friction: its cone omits the restitution impulse.
  - **Evidence:**
    - strike Δv_t(B) = 0.3907 m/s = μ(1+e)·v/2 = 0.3 × 1.3023 × 2/2 at drag 0, and 0.3754 m/s with drag; base gives 0.300;
    - the e = 1 random-pose gains drop from 186 and 438 mJ to 0.
- **D6. Contact events (triggers and contact work).** A point that carried normal impulse this substep is a contact, touching or speculative (Box2D hit-event semantics).
  - Its approach is −`RelVel` (D3), the same quantity restitution uses, so there is one definition.
  - An impact is reported in the substep that resolves it, at most one substep earlier than on main. The warm-up rider fires at step 46 instead of 47, with approach 1.412 against 1.432.
- **D7. Contact-load sensors (shared `ContactLoadSensorTracker` law).** Loads are measured after the restitution pass instead of before prepare.
  - A record qualifies when gap ≤ `MaximumGap` **or** the point carried normal impulse this substep. Alignment, footprint and mass checks are unchanged, and the per-tick maximum over substeps stays.
  - Without this rule, a ball that bounces from the speculative gap never presses the plate it hits.
  - Implement it as an explicit record lane (for example `bool Pressed`), not the scratch's `min(gap, 0)` substitution. CAT-052's sensor wording gains "or carried contact impulse".
  - Effect: the plate's first qualifying tick is the impact tick (77 against base 78), and the control still sees eight non-qualifying in-flight ticks (78–85).
- **D7a. Sequencing with in-flight slices.** D6 changes the contact-event emission seam (gate, approach source and timing), and 9A.22's Impact ring records compile from that seam. D7 moves the measurement point that C-Plate's declared contact-load rows read.
  - Expected order: 10.1 → this slice → rebase 9A.22 and C-Plate, if either is still unmerged. Alternatively, this slice rebases onto them if they merge first.
  - Either way, the later slice re-runs its event-ring and contact-load guards. A changed Impact ordinal or approach is expected, at most one substep earlier.
- **D8. Envelope criteria (game-grade, from the slop, not tuned).**
  - **Per tick:** no rise above m·g·`ContactSlop` for random poses at e ∈ {0, 0.3, 0.55, 0.6, 1} on sphere, box-on-bench, box-on-static-box and cylinder; and no rise above Σ m·g·`ContactSlop` (5.887 mJ) for a 1 kg box dropped on a resting 2 kg box, 200 cases at e ∈ {0.3, 0.6, 0.9, 0.95, 1} plus the reviewer's two reproducers. No e = 1 exemption.
- **D13. Energy-bounded restitution pass (review F1 root cause, 24 Oct 2026).**
  - *Diagnosis* (phase trace of the reviewer's e = 0.95 reproducer, substep 324): the lower box bounces on the bench (approach 0.833 m/s, its own manifold) and the upper box bounces on it (approach 1.929 m/s) in the same substep. Each manifold drives its own points to −e·(their approach) in turn, so the second impact re-drives the lower box after the first: kinetic energy goes 6816.65 (substep start) → 6557.55 (relaxed) → 6850.05 mJ (restituted), 33 mJ more than the substep began with. At e = 1 the same coupling returns up to 186–292 mJ per substep. Of the coordinator's hypotheses this is (c), coupling across manifolds through a shared body; (a) and (b) do not hold (the bench manifold bounced in the same substep, on its own substep-start approach).
  - *Fix.* `WorkshopConstraintSolver.RestitutionPass`: union-find groups the dynamic bodies joined by bouncing manifolds; a group's kinetic energy after the pass may not exceed max(its kinetic energy before the contact solve, after gravity and drag; its kinetic energy after the relax sweeps). A group over the bound has its whole restitution change (bounce, friction and rolling re-solve) scaled back along the line from its relaxed velocities to the largest fraction the bound allows (the quadratic E(α) = c + bα + aα² solved for α ∈ [0, 1]). All in f32; single-manifold bounces at e ≤ 1 never reach the bound, so every single-body result above is unchanged (Lively, flat-drop and reflection values identical).
  - *Result* (reviewer's probe file run unchanged on the final candidate, `evidence-engine-restitution/review-fix/reviewer-probes-on-candidate.log`): reproducers e = 0.95 worst rise 0.051 mJ, e = 1 0.000 mJ (bound 5.887). Reviewer sweep (seed 883000 + 100·e): e = 0.95 0.314 mJ, e = 1 0.255 mJ; e ≤ 0.9 identical to before the bound (0.241–0.328 mJ). Every held-back class within its bound (worst: cylinder 1.575 of 1.962 mJ).
  - **Rebound speed** (sampled at 120 Hz): e − g·T·(1+e)/v_in ≤ v_out/v_in ≤ e + 2⁻¹⁰.
  - **Apex ratio:** within [(e − g·T·(1+e)/v_in)², e²]. With T = 1/120 s and v_in ≈ 3.13 m/s, g·T/v_in = 0.026.
  - The 10.1 Lively gate keeps its own bound (box control + slop) and must pass on all classes.
- **D9. Curved-feature bounce normal (resolved under coordinator decision 1: the fix had a defect, the test stays).**
  - *Derivation.* `ABallOffTheRimReflectsAlongTheRimNormal` uses `LawMaterial(1, .1, 0, 0)` for the ball and the cylinder: μ = 0 on both sides, so the friction re-solve (D5) adds no tangential impulse and the analytic outgoing velocity is the frictionless e = 1 mirror v − 2(v·n)n about the rim normal at touch, n = (0.05, 0, √(0.1² − 0.05²))/0.1. The 2° oracle is the correct expectation; the candidate's 3.90° was a defect of the fix (the bounce used the normal read across the speculative gap), so the fix was corrected, not the test.
  - *Correction.* A sphere's speculative contact that closes within two substeps takes its normal at the predicted touch (`WorkshopCollisionKernels.AdvanceSpeculativeSphereContact`): conservative advancement of the sphere's centre along the pair's relative linear velocity, at most four probes of the same pair kernel; the gap becomes the distance still to close along the touch normal; the point stays on the moving sphere's centre line, so its normal impulse has no torque. Two moving spheres keep their generated contact (no single point keeps both arms on the normal; their normal is read across the speculative gap, so its angular error scales with v·h/(r_a + r_b), growing with closing speed and shrinking with the pair's radii).
  - *Result.* Rim and static sphere: ≤ 0.02° at 2, 3, 6 and 12 m/s with |v| ratio 1.0000 (`evidence-engine-restitution/dev/reflect-candidate.txt`; v = 1 m/s never reaches the target in 1 s) (`ABallReflectsOffCurvedFeaturesAboutTheTouchNormalAtEverySpeed`, which base fails at 6 and 12 m/s with 64° and 62°). The 10.1 rim test passes unchanged.
  - *Pipe bend (same rule).* joined_pipe's unsolved layout drops the ball at 11.6 m/s onto the fixed bend's inclined box face (n ≈ (−0.877, 0.481, 0), e = 0.55·0.12 = 0.066, μ = 0.3). Sliding throughout (μ(1+e)|v_n| = 1.79 < |v_t|/3.5 = 2.91), so v' = e|v_n| n + (v_t + μ(1+e)|v_n|) t̂ = (−4.362, −7.177) m/s. Candidate (−4.369, −7.191), within 0.2 %; base (−3.659, −7.356), 5° off (its bounce came a substep later on a cone without the bounce impulse). Pinned in isolation by `ABallOnAnInclinedFaceLeavesWithTheCoulombSlidingReflection` (friction on: the Coulomb sliding reflection within 1 % and 1°; frictionless control within 1°).
- **D9 (draft, superseded): known geometric cost of bouncing from the speculative gap.** A curved feature reflects about the normal at the speculative closest point, not at true touch.
  - The angular error grows roughly as v·h/R, where R is the radius of the normal's sweep: the ball radius 0.1 m for a rim, r_a + r_b for sphere–sphere.
  - Measured on the 10.1 copy as elastic, frictionless reflection error at e = 1 (`ReflectProbe`):

    | v (m/s) | Rim, base | Rim, candidate | Sphere (R = 0.4 m), base | Sphere, candidate |
    | --- | --- | --- | --- | --- |
    | 2 | 1.45° | 2.83° | 0.60° | 1.09° |
    | 3 | 2.00° | 3.90° | 0.75° | 1.38° |
    | 6 | **64.45°**, \|v\| ratio 0.45 | 6.91° | 1.18° | 2.23° |
    | 12 | 6.92° | 6.92° | **62.24°**, \|v\| ratio 0.47 | 3.86° |

  - The candidate preserves |v| exactly (ratio 1.0000) at every speed and stays within 2·asin(min(1, v·h/R)). Base is smaller at low speed but fails grossly at 6 and 12 m/s (the stale-approach mechanism).
  - Consequence: the 10.1 test `ABallOffTheRimReflectsAlongTheRimNormal` (3 m/s, 2° tolerance) fails on the candidate (3.90°). The other 10.1 tests pass.
  - **Decision (coordinator, by game intent):** either (a) replace the rim and side reflection tolerances with the derived 2·asin(v·h/R) bound plus |v| within 2 %, and add the 6 and 12 m/s rows that base fails; or (b) keep 2° and add a touch-time normal refresh for sphere-feature pairs as a follow-up story.
  - Draft recommendation: (a). The bound is derived, not tuned, and the candidate is strictly better on speed preservation and at high speed; the refresh would be a second normal model.
  - Caveat: at the typical lesson speeds of 2–3 m/s the candidate's error is about twice base's, and the effect reaches pipe bends (`joined_pipe`, see the lesson check).
  - **This is a blocking coordinator decision. The slice is not green until it is signed off**, because the fix changes another story's test tolerance.
- **D10. Out of scope: a pre-existing stack limit cycle.** Some three-box stacks keep a 0.01–0.04 mm per-tick vertical oscillation for over 10 s:
  - base: 4 of 12 restitution variants;
  - candidate: 5 of 12;
  - e = 0: identical and settled on both.

  Every body's point speed after 1 s is ≤ 8.4 × 10⁻³ m/s, below every bounce threshold (0.1 m/s), so restitution cannot fire late. The cycle belongs to soft contact and relax. It is recorded as a separate defect for the engine owner. Here the criterion is: no restitution after settling, and late per-tick motion ≤ 0.05 mm (10 % of the 0.5 mm resting envelope).
- **D11. Test corrections. These fix confounds and keep their tolerances; they do not loosen anything.**
  - *`DeclaredPrismaticDriveTests.TheWarmupAdmitsAndStepsItsSceneWithTheDriveSuppliedThenUnsupplied`.* The trigger record is per tick, so "count > 0 after the last step" depended on which tick the strike landed. The test now asserts that the trigger fired on some step with a recorded approach ≥ its 0.5 m/s threshold. `WarmupScene.Steps` is unchanged.
  - *`WorkshopPhysicsLawTests.AFreePartnerBallReceivesTheEqualAndOppositeRollingImpulse`.* `resistanceFree` removed drag and C_rr together, so the control varied two variables. The runs now differ only in C_rr, with drag kept. The 0.05 equal-and-opposite tolerance and dB > 0.15 are kept. With equal drag, dA = −0.333 and dB = +0.333.
- **D12. Catalogue reach.** The dynamic materials are Ball 0.55, Bowling ball 0.14, Domino 0.05, plate 0.1, blade 0.1 and Springboard 0. Every 1.0 material is static, so catalogue pair restitution is ≤ 0.55. The gate still covers e up to 1 for future elements.

# Ordered tasks

- [ ] **Suite first, at the baseline (after 10.1 merges).** Run one matching Debug build, then `dotnet test CuriousContraptions.slnx --no-build --no-restore`. Add the 10.1 explicit gates, `--filter Category=CylinderEnergyGate -- xUnit.Explicit=on`, and record Lively red, Dead and Rolling green. Keep the raw receipts and the identity.
- [ ] **Red tests first** (below). Add a `WorkshopPhysicsLawRestitutionTests.cs` class and promote the Lively gate from Explicit into the normal run.
- [ ] **Implement D1–D7** in `WorkshopConstraintSolver`, `WorkshopPhysicsEngine`, `WorkshopCollisionKernels` (the `SimBody.V0/W0` and `ContactPoint` lanes) and `ContactLoadSensorTracker` (the explicit pressed lane). Route the JIT warm-up through the manifold pass, including the box manifold. Delete every legacy item listed above.
- [ ] **Apply the D11 test corrections**, each with a comment naming the confound.
- [ ] **Native tests** (PRIMARY), green on one build.
- [ ] **Lesson identity check.**
  - Regenerate trace hashes for every playable lesson at precisions 0, 0.45 and 1 (720 ticks), on the baseline and on the candidate.
  - Record which traces change and why. Scratch result: all 36 unsolved traces change, because every impact resolves one substep earlier and uses the pre-gravity approach. Final rest poses are identical except two:
    - **`ready_to_rebound` (0.65 m):** the 8 m/s bench landing at t141 rebounds at 4.396 m/s against 4.407 on base. The difference, ≈ e·g·h, is D3.
    - **`joined_pipe` (3.41 m):** the ball falls vertically at 11.6 m/s onto the fixed bend at t146. Base leaves at 26° from vertical with |v| 8.22; the candidate leaves at 36° with |v| 7.78, and the no-D5 variant also leaves at 36°. So this is the D2 bounce from the speculative gap on a curved hollow surface (the D9 class), not D5. **D9 therefore reaches a core puzzle element (the pipe bend), not only the 10.1 rim test.**
  - All goal ticks were −1 on both sides because these are unsolved layouts. "Goal outcomes unchanged" carries no weight from this probe. **Authored-solution traces were not diffed in the scratch run; only solve pass/fail was checked (suite).** The slice must diff every authored-solution trace (the `ReviewSolvedWorldProbe` pattern) and record the first-diverging contact for each.
  - Record the solve outcome of every route.
- [ ] **Authored-solution rechecks.** `LaunchLessonSolveTests` (every route, including WallPaid), Clear pipe, gentle/quarter bends, joined pipe, delayed signal, domino effect, bumper routes and pressure-plate routes all solve, or stay unsolved, as before. Re-run the 9A.32 domino_effect corrected solution at every precision.
- [ ] **Docs.**
  - `docs/gpu-f32-physics.md`:
    - §Narrowphase "dissipative restitution": state the D1–D5 pass, D3 approach and D5 friction re-solve;
    - §Speculative contacts: speculative points that carry impulse bounce, and events and loads treat them as contacts;
    - §Envelope: Normal Velocity Residual should name the material bounce threshold, not "0.05 m/s"; Thermodynamic Energy should cite the per-tick slop bound.
  - CAT-052 sensor wording (D7); D9 and D10 decisions; `TODO.md` and `sprint-status.yaml`.
- [ ] **Full gate.**
  - One matching 0-warning Debug build, then the full `dotnet test CuriousContraptions.slnx --no-build --no-restore` once on the frozen candidate, run in a real worktree so the `.git`-dependent isolation and ring-producer tests run.
  - CI tools step and Release worker build clean.
  - The 1000-case explicit gate for Dead, Rolling and Lively.
- [ ] **Review and publication.** Independent adversarial review (a different subagent) → SnapshotApproval → commit, push and deploy → reviewer verifies remote and deployed identities → terminal scoped Pass. Browser stays pending: changed bounce trajectories join the post-catalogue E2E queue.

# PRIMARY native tests (real admission → engine, tick progression)

- **Flat drops** (sphere, box on bench, box on box; e ∈ {0, 0.3, 0.6, 1}; 0.5 m):
  - the D8 rebound-speed and apex bounds hold, with zero spin after a flat landing (|ω| ≤ 10⁻³ rad/s);
  - no per-tick rise above slop;
  - e = 0 never leaves the surface (the control).

  Candidate scratch values (apex, v_out/v_in) at e = 0.3 / 0.6 / 1:
  - sphere: 0.0861 / 0.3501 / 0.9771 and 0.2803 / 0.5804 / 0.9803;
  - box: 0.0858 / 0.3491 / 0.9743 and 0.2799 / 0.5795 / 0.9788.

  Main fails the box rows (0.0555, 0.2114).
- **Random-pose energy:**
  - 40 seeded poses (spin ±3 rad/s, drop 0.45–0.95 m) per shape × e ∈ {0, 0.3, 0.55, 0.6, 1}: worst per-tick rise ≤ m·g·`ContactSlop`. Candidate: ≤ 0.001 mJ everywhere. Main: up to 8776 mJ.
  - Held-back seed kept by the reviewer.
- **10.1 gates (after merge):**
  - Lively at 200 cases: every class 0/200 over the control + slop bound. Scratch: control ≈ 0.010 mJ (bound 1.972 mJ); worst 1.471 mJ CylinderOntoPlane, 1.004 CylinderOntoBox, 0.046 BoxOntoCylinder, 1.466 CylinderOntoLyingCylinder, 1.327 CylinderOntoStandingCylinder, 0.000 SkewedRollOffEdge. Base failed with a 285.317 mJ bound and a 514.917 mJ worst.
  - Dead and Rolling at 200 and 1000 cases: 0 failures. The 1000-case worsts are bit-identical to round-5 base (for example CylinderOntoPlane Dead 1.813 mJ and Rolling CylinderOntoBox 1.987 mJ), as expected with e = 0. Every other `WorkshopPhysicsLawCylinderTests` test passes except the D9 rim test.
- **Frictional impact (D5):** the test first asserts the regime: sliding when μΛ < m_t·slip₀. The expected Δv_t is μ(1+e)·v/2 in the sliding regime and m_t·slip₀/m in the sticking regime, and post-impact slip never reverses sign. A spin-10 basketball strike (sliding) gives Δv_t(B) = μ(1+e)·v/2 within 2 % at drag 0 and with drag. The control is spin 0.3, which sticks: relative rolling is zeroed and the result is unchanged.
- **Event and load (D6/D7):**
  - a ball dropped on a plate qualifies on the impact tick, with in-flight non-qualifying ticks and the lamp lit through the bounce;
  - an impact switch or trigger records the approach −RelVel at the impact substep;
  - a resting body reports an approach below its threshold.
- **Resting and stacks (D10):**
  - three-box stacks over gaps {0.5, 1, 2} mm × offsets {0, 10} mm × e ∈ {0, 0.3, 0.6}: no restitution after 0.5 s (the max point speed is below the threshold and the direct count is zero), late per-tick motion ≤ 0.05 mm, top y error within the 0.5 mm envelope;
  - a ball set down 2 mm above the bench rests.
- **Determinism and Reset:** two runs and a re-admission are bit-identical for 600 ticks at 60, 120 and 240 Hz with bouncing boxes, spheres and cylinders. (The engine's warm-start cache is private and never saved, so a reloaded mid-bounce world is not claimed bit-identical; Workshop save/load is covered by the existing reload tests.)
- **Boundaries:**
  - e = 0 (no pass);
  - approach exactly at the threshold (no bounce) and one f32 step above (bounce);
  - a singular K (coincident points) uses the per-point fallback with no NaN;
  - the 64 m/s clamp after restitution still holds (the existing envelope test);
  - no tick fault anywhere.
- **Reflection (D9):** sphere–sphere and rim reflection errors stay within the decided bound at 2, 3, 6 and 12 m/s, and |v| is within 2 % at e = 1.

# Code Map

- **Change:**
  - `CuriousContraptions.Simulation/WorkshopConstraintSolver.cs`: `PrepareContact` (D3 approach, the narrowed cache tuple, `Bounced` deleted), a new `Restitute(ContactManifold)`, `ApplyRestitution(List<ContactPoint>)` (D4) and `ApplyRestitutionRow`, with `CouplingMass`, `SolveLinear`, `SolveFriction` and `SolveRolling` reused.
  - `CuriousContraptions.Simulation/WorkshopPhysicsEngine.cs`:
    - the substep-start snapshot;
    - the per-manifold restitution call;
    - contact-load measurement moved after restitution (D7);
    - the narrowed cache;
    - event emission on `MaxImpulse > 0` with −`RelVel` (D6);
    - JIT warm-up through the manifold pass.
  - `CuriousContraptions.Simulation/WorkshopCollisionKernels.cs`: `SimBody.V0/W0`; `ContactPoint.Bounced` deleted.
  - `engine/gpu/ContactLoadSensorDeclaration.cs` (`ContactLoadSensorTracker` record and `Measure`): the explicit pressed lane (D7).
  - **Tests:**
    - a new `WorkshopPhysicsLawRestitutionTests.cs`;
    - `WorkshopPhysicsLawCylinderTests.cs` (promote Lively; D9 rim tolerance per decision);
    - `DeclaredPrismaticDriveTests.cs` and `WorkshopPhysicsLawTests.cs` (D11);
    - `PressurePlateTests.cs` (impact-tick qualification).
  - **Docs:** `docs/gpu-f32-physics.md`, `docs/planning/elements/CAT-052-pressure_plate.md`, `TODO.md`.
- **Reuse:** `LawScene`, `LawWorld`, the 10.1 gate harness (`GateCase`, `Gate`, `AssertGate`), `SolveNormalBlock`'s regularization, `PackageRuntimeParityTests`.
- **Do not change:**
  - the TGS Soft row model, slop, speculative margin, iteration counts and `ContactHertz`;
  - material pair laws (product e, max threshold, geometric-mean μ);
  - element packages and `PartId`;
  - the ABI and wire layouts (the contact cache is engine-private);
  - `compiled-geometry-9a16.json`.

# Evidence (scratch, not committed)

All files are under `/private/tmp/claude-501/-Users-aidan-dev-personal-tim/7e56a042-6670-44d4-ae0f-4efd972de00e/scratchpad/restitution/`:
- `repo/`: the instrumented main copy, with A/B modes 0–6, `RestitutionProbeTests.cs` and the `probe-run1.txt` traces;
- `base/` and `clean2/`: the pristine main and the candidate. `candidate2-sim.patch` and `candidate2-tests.patch` are the candidate diff;
- `test-base.log` and `test-clean2b.log`: the full `CuriousContraptions.tests` runs. Both have 3 failures, all `.git`-dependent: `ElementIsolationTests` ×2 and `EventRingProducerTests.OnlyTheEngineProducerAndTheWorkerWriteTheRing`. The run without D7 and D11 (`test-clean.log`, `test-clean2.log`) showed the regressions those decisions fix;
- `lessons-base.txt` and `lessons-clean2.txt`: lesson identity;
- `wt101/`: 10.1 with the candidate applied (`wt101-gates.log`); `wt101base/`: 10.1 unpatched (the ReflectProbe baseline).

# Coordinator decisions (24 October 2026, under owner delegation)

1. **Rim reflection test (3.90° vs 2°): no tolerance change on sign-off alone.** The implementer derives the analytic expected reflection direction for that pose and material, including friction during the bounce: re-running friction after the bounce adds a tangential impulse.
   - If the old 2° oracle assumed a frictionless reflection that the material does not have, correct the oracle to the derived expectation, with a frictionless-material control that still reflects within 2°.
   - If the derived expectation still says ≤ 2°, the fix has a defect: correct the fix, not the test.
   - The derivation goes into the spec. The same rule applies to the pipe-bend deflection changes.
2. **Lesson impact:** every authored lesson solution must still solve natively at all 3 precisions. Extend the existing `TheAuthoredSolutionSolvesAtEveryPrecision` guard to all Playable lessons whose solutions exist.
   - Any lesson that stops solving is investigated. If the physics is now correct and the authored pose relied on the energy gain, correct the authored data (pure content fix, proven at 3 precisions). Otherwise reopen the fix.
   - Unsolved-lesson trace changes are expected; record their count and nature, not a byte comparison.
3. **Stacks that never fully settle** (also on base): a separate, pre-existing defect. Record it as a deferred follow-up engine story ("Engine: resting stack settling") with evidence. It is not a prerequisite of this slice.
4. **Event and plate-load timing one substep earlier:** accepted as a consequence of same-substep bounce. Pin it with native tests, and check the 9A.22 ring stamps still meet their tick contract.
5. **Lively gate entry check:** restore verbatim from story-10-1's `evidence-10-1/lively-gate-source.txt` as an ordinary routine `[Fact]` (200 per class, bound unchanged). It must be red on the pre-fix base and green after.

# Current state (24 October 2026, implementation owner): native candidate in review, uncommitted

Worktree `story-engine-restitution`, base cf35a064 (10.1 merged). Evidence: `_bmad-output/implementation-artifacts/evidence-engine-restitution/` (`baseline/`, `entry-red/`, `dev/`, `mutations/`, `stacks/`, `lessons/`, `final/`). Browser qualification pending (post-catalogue E2E).

- **Suite-first baseline** (pristine cf35a064, one Debug slnx build): CuriousContraptions.tests 2776/2776, E2eRunner 189, BundleElements 189, Coverage 77, WorkerAotGuard 17; 0 skipped, 0 duplicate IDs (`baseline/`).
- **Entry check red on base** (`entry-red/`): the restored Lively gate fails exactly as 10.1 round 5 recorded (bound 285.317 mJ; plane 1/200 344.658, box 1/200 347.448, box→cylinder 2/200 331.347, lying 4/200 514.917, standing 3/200 443.959, roll-off 1/200 415.090). The new restitution class fails 31 of 52 on base. The extended authored-solution guard passes on base for all 12 Playable lessons (harness extended: ramp, delay, basket, typed pipe, both bends; content wiring may name a unique fixture by its part).
- **Implementation** (D1–D7 as specified, plus the D9 correction): `WorkshopConstraintSolver` (`StartVelocity`, `Bounces`, `Restitute`, block `ApplyRestitution`, `ApplyRestitutionRow`), `WorkshopPhysicsEngine` (substep-start snapshot, per-manifold pass, loads after restitution, narrowed cache, events on carried impulse, warm-up through the manifold pass, the touch probe call), `WorkshopCollisionKernels` (`SimBody.V0/W0`, `AdvanceSpeculativeSphereContact`, `ContactPoint.Bounced` deleted), `ContactLoadSensorTracker` (`ContactPointRecord.Pressed`, no default). Threshold is strict (`RelVel < −Threshold`; the brief's "exceeds"; base and Box2D bounce at equality).
- **Legacy deleted:** the `Approach` cache lane and its `min`, the deferred-approach cache write, `ContactPoint.Bounced`, the separation guards in restitution and event emission, per-point-only `ApplyRestitution(ContactPoint)`.
- **Tests:** new `WorkshopPhysicsLawRestitutionTests` (52 cases: flat drops ×4 bodies ×4 e, random poses ×4 bodies ×5 e including e = 1 and cylinders, Coulomb strike sliding/sticking, inclined face with frictionless control, strict threshold, touch-normal reflection 2–12 m/s, stacks, bit-identical runs and Reset replay at 60/120/240 Hz); the Lively gate as a routine `[Fact]` (green: 0/200 every class); the authored-solution guard over every Playable lesson (12, all green at 3 precisions; no content change needed); timing pins (decision 4): the plate qualifies on the impact tick (`ABasketballBouncingOnThePlateKeepsItsLampSteadilyLit`), each bumper Impact's ring ordinal lies in its own tick's four substeps and the first hit is stamped on the tick the ball is struck back (`EachBumperHitPublishesExactlyOneImpactAtItsOwnTimeAndNoTrigger`; the 9A.22 tick contract holds); the pressed-lane law cases in `ContactLoadSensorTracker_CountsOnlyRestingContactsOnTheFace`; D11 corrections (warm-up trigger on any step with approach ≥ 0.5 m/s; partner-ball rolling with drag kept).
- **Mutations** (scratch mirror, non-incremental builds, focused set; `mutations/`): all 8 killed. M1 separation guard back (8 tests), M2 gravity in the approach (4), M3 per-point only (10), M4 no friction re-solve (5), M5 no touch normal (5), M6 load ignores pressed (2), M7 events touching only (2), M8 inclusive threshold (1).
- **Lessons (decision 2):** all 12 authored solutions solve at 3 precisions. Unsolved-layout traces: 36 of 36 change (every impact resolves one substep earlier on the substep-start approach); final rest poses identical except `ready_to_rebound` (0.65 m: an 8 m/s bench landing rebounds at e·v without this substep's gravity) and `joined_pipe` (1.87 m: the 11.6 m/s bend impact now leaves with the friction-inclusive analytic velocity, see D9). `lessons/summary.txt`.
- **Stacks (decision 3):** recorded as deferred story `engine-resting-stack-settling` (sprint-status backlog, `deferred-work.md`); base 5 of 18 stack variants still oscillate after 8 s, candidate 6 of 18, all ≤ 0.036 mm per tick with point speeds ≤ 8.4 × 10⁻³ m/s (`stacks/`).
- **Final gate** (`final/`, one Debug slnx build, 0 warnings): `dotnet test CuriousContraptions.slnx --no-build --no-restore` CuriousContraptions.tests 2837/2837, E2eRunner 189/189, BundleElements 189/189, Coverage 77/77, WorkerAotGuard 17/17; 0 skipped, 0 duplicate IDs (461 s). CI tools step under bash exit 0 (E2eRunner 174 without the toolchain 15). Release `CuriousContraptions.Simulation` and `CuriousContraptions.Animation.Worker` 0 warnings. `git diff --check` clean.
- **Review round 1 (review-engine-restitution.md: Fail on F1, Incomplete on F2/F3) — fixes, same implementation owner:**
  - *F1* took the root-cause path (one attempt converged): D13, the energy-bounded restitution pass. Routine tests added: the reviewer's two reproducers and 200 seeded box-on-resting-dynamic-box drops at e ∈ {0.3, 0.6, 0.9, 0.95, 1}, bound Σ m·g·slop = 5.887 mJ unchanged. Doc and spec claims narrowed to the qualified classes (outcome, D2, D8, `docs/gpu-f32-physics.md` §Restitution and the Thermodynamic Energy row).
  - *F2*: `APullingOrRankDeficientBlockFallsBackWithoutNaNAndEachPointBouncesToItsTarget` (a simultaneous block at its targets within the regularisation; a pulling block falls back, the pulled corner clamped at zero and the other exactly at target; coincident points share equally, no NaN) and `ABallSetDownTwoMillimetresAboveTheBenchComesToRest`.
  - *F3*: kept the rolling half and added `ABouncingSpinningBallLosesTheRollingResistanceOfTheWholeImpact` (frictionless spinning ball: Δω = C_rr·r·Λ/I with Λ = m(Δv_y + g·T), within 3 %; without the rolling pass after the bounce only ≈ Λ/(1+e) would act).
  - *Wording*: the moving-sphere-pair normal error is stated as scaling with v·h/(r_a + r_b).
  - *Mutations, round 2* (`mutations-round2/`, non-incremental, 73-test focused set): 11 of 11 killed, including the reviewer's surviving MX2 (block accepts a pull: 2 tests) and MX4 (no rolling re-solve: 1), and M9 (no energy bound: the two reproducers and the e = 1 sweep).
  - *Final gate, round 2* (`final-round2/`, one Debug slnx build, 0 warnings): CuriousContraptions.tests 2847/2847 (Lively and every 10.1 routine gate included), E2eRunner 189, BundleElements 189, Coverage 77, WorkerAotGuard 17; 0 skipped, 0 duplicate IDs (499 s). CI tools step under bash exit 0. Release Simulation and Animation.Worker 0 warnings. `git diff --check` clean.
- **Next:** independent re-review of F1–F3 (the same reviewer), then SnapshotApproval and publication; main has moved (f5ffdb4e), so the verdict available is Pass-pending-merge. Rebase order with 9A.22 and C-Plate per D7a.
