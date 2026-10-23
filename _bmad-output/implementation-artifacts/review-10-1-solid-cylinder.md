# Review: Story 10.1 Solid Cylinder Collider & Inertia (ENGINE-CYLINDER)

This is an independent adversarial review, done before the merge. The reviewer did not write the candidate and edited no source, test, tool, content or document file in it. The probes ran in a throwaway worktree, which was removed afterwards.

- **Candidate:** worktree `story-10-1`, base `a598a09e`, uncommitted.
  - Diff sha256 `80941ef977de9f12e8a6f568cd8604cbb0de02271d9af37ae453da9405a4ba9b`.
  - `WorkshopPhysicsLawCylinderTests.cs` sha256 `8e027598df5344869405492f4852d9eb6c57f93e96c54f124da66b64602c03bb`.
  - The spec's sha256 is `58b9822bfed92abee46e6ac213884033fee64249dad51d7e24b89d320565ac2e`.
  - The diff was identical at the start and end of the review (`evidence-10-1-review/identity-at-start.txt`, `identity-at-end.txt`).
  - The implementer's `candidate-identity.txt` predates the `TODO.md` edit, so its diff sha is `d14d15…`. This review's own runs supersede it.
- **Verdict: Fail.** Two known violations are blocking:
  - **F1:** the cylinder–plane contact while standing on an end.
  - **F2:** rim-regime SAT gaps for cylinder–box and cylinder–cylinder.

  Both contradict the AC "contact follows the true curved side and flat end faces … rests on an end face without jitter" and the game-grade contact envelope. Everything else reviewed passes. Once F1 and F2 (and F3) are resolved and re-reviewed, the next verdict available is Pass-pending-merge, because main (C-Gate) must be merged before publication.
- **Browser qualification** (`engine-cylinder`) is pending until the post-catalogue E2E.

## Commands and results (`evidence-10-1-review/`)

| Check | Result | Log |
|---|---|---|
| `dotnet build CuriousContraptions.slnx -c Debug` | 0 W / 0 E | build-debug.log |
| `dotnet test CuriousContraptions.slnx -c Debug --no-build --no-restore` | tests 2412, E2eRunner 189, Coverage 77, BundleElements 163, WorkerAotGuard 17; 0 failed | test-slnx.log |
| CI "Test C# tools" step (`pages.yml`:51-64), run under bash. The body was diffed equal to the workflow first | 77 / 163 / 17 / 174, exit 0 | ci-tool-step.log |
| `dotnet build CuriousContraptions.Simulation -c Release` | 0 W / 0 E | worker-release.log |
| `git diff --check`, plus `--no-index --check` on both untracked files | clean | diff-check.log |
| Reviewer probes (throwaway worktree = base + candidate diff + probe files) | see below | probe-run1.log, probe-run2.log, probe-run4.log, `probes/*.cs.txt` |
| Failed probe attempts, kept unused | `probe-run3.log` / `probe-run3b.log` (L1, L2): the bodies fell flat or rolled away, so the leaning scenarios never formed. Their "separation" lines (for example 135 and 250 mm) are **inconclusive and not evidence**. The first L1 run was refused at admission: radius 0.06 < 1/16 | probe-run3.log, probe-run3b.log |

## Criterion and impact matrix

| Criterion | Reviewer evidence | Status |
|---|---|---|
| Inertia: axial m r²/2, transverse m(3r²+w²)/12, on Half lanes | Code read. The 1/3 constant rounds to 0.33325 (2.4e-4 rel), well inside the 6×2⁻¹¹ bound. The test is analytic over the three geometries | Pass |
| Closed enum end to end; atomic rejection | `Gpu.ColliderShapeKind`, `Simulation.ShapeKind` and `Abstractions` all have Cylinder = 3. `Validate` checks range and zero lanes; `Admit` rejects X≠Z. The ingest `InvalidDeclaration` writes only status and failure. Repo-wide grep (`.cs/.ts/.js/.mjs/.wgsl/.gd`): no other shape consumer, no TS mirror, and no test uses 3 as "unknown". `PassageSensorTracker`'s throwing default arm runs only on admitted declarations | Pass |
| Extractor: box or sphere with a missing key → `ArgumentException` | It used to throw `KeyNotFoundException`. `BundleGenerator.cs:28` catches both, so this is still a build-time typed refusal at the right boundary, with no silent change. It is covered by the `size → ""` row and the cylinder missing-key rows. No current content can rely on it, since the old path threw too. *Low:* Godot omits default-valued keys, so a default-sized proxy (CylinderShape3D r 0.5 / h 2) is refused loudly. That is the spec's choice | Pass |
| Ramp roll (2/3) g sinθ | The suite covers 120 Hz. Probe P4 adds 60 and 240 Hz: −0.04% and −0.03% | Pass |
| Rest on side / cap, no drift | P3: 2000 ticks at 60, 120 and 240 Hz, drift 0, late ω ≤ 8e-12 | Pass |
| Topple at atan(r/h) | Correct at azimuths 0° (the suite's case) and 22.5°. Near the 45° diagonal, a slender rod (h/r ≳ 3.5) tipped within the ~10.0–11.5° window does not right itself. At 45°/12° it settles at 1.59°, above the suite's own 1° threshold | **Fail (F1)** |
| Cap-regime contact on the plane / jitter / penetration | P1, P2, P6-cap, P8 and T1 | **Fail (F1)** |
| Cylinder–box and cylinder–cylinder gaps (true shape) | P6 / S1–S3 oracle sweeps and the D phantom-push engine probe | **Fail (F2)** |
| Cylinder–sphere (side, cap, rim, inside exit) | Code derivation matches the closest point on the solid. The suite reflection tests agree | Pass |
| 64 m/s tunnelling | P7: disc end-on and edge-on, disc spinning 100 rad/s, tumbling drum, r 1/16 rod end-on, box and ball into a static r 1/16 rod. No pass-through | Pass |
| Determinism / Reset / ABI and JSON round trip | Suite (60/120/240 Hz bit-identical, Reset re-admission). Workshop Save/Load is carried to 10.2 per the spec, because no element can place a cylinder yet | Pass (10.2 carry recorded) |
| Every-contact rolling resistance (owner, 9 Oct) | The curved side adds the row with arm r; caps add none. P9: a ball over a static r 1.5 drum gets arm max(r_ball, r_drum) and slows 0.27 m/s in 0.25 s, against 0.095 m/s over a box with the same Crr. This matches the sphere–sphere max precedent and the updated doc. *Low* | Pass (Low noted) |
| Tests discriminate | The implementer's 15 kills are recorded. Reviewer mutations A–E all **survive** (F3) | Incomplete (F3) |
| No regression | Full suite and CI step green. `compiled-geometry-9a16.json` is unchanged | Pass |

## Reviewer probes (analytic oracles, real `WorkshopPhysicsEngine` through admission)

- **Oracle.** The exact separation of two convex solids is the maximum over unit u of −s_B(−u) − s_A(u), using exact support functions (6000 Fibonacci directions, then local refinement).
- **Control.** Box–box, whose 15-axis SAT is complete, agrees within ±0.72 mm over 300 poses (S1). This validates the oracle.
- **P1/T1, diagonal tip.** Rod r 0.1, h 0.4: the true critical angle is atan(r/h) = 14.04°.
  - At azimuth 0 (onto a rim sample), every tilt from 8° to 13° rights itself, with penetration ≤ 2.1 mm.
  - At azimuth 45°, tilts of 10.5° and 11° **do not right themselves**.
    - T1 shows a 10 s limit cycle between 10.6° and 11.8°, ending at 11.2–11.6° with ω up to 0.06 rad/s.
    - The per-tick energy rises by up to 7.7 mJ.
    - The 4-point inscribed-square pivot predicts toppling past 10.0°. The 0.98 regime switch then injects energy at 11.48°.
- **P2, diagonal tilt release** (true rim depth below the bench, at azimuth 45° against azimuth 0):

  | Body | Azimuth 45° | Azimuth 0 |
  |---|---|---|
  | Pulley wheel r 0.4, w 0.17 | **22.0 mm** | 0.36 mm |
  | Fulcrum r 0.22, w 1.02 | **12.5 mm** | 3.1 mm |
  | Drum 0.25 / 0.5 | **14.0 mm** | 0.29 mm |

  The fulcrum at 20° and azimuth 45° shows a 14.6 mJ/tick energy rise. The envelope allows resting ≤ 0.5 mm and temporary ≤ 5% of the smaller body extent (8.5 mm for the Pulley's width).
- **P6-cap.** At azimuth 45° and tilts of 2° to 11.4°, `CollideCylinderPlane` returns **0 contacts** while the true gap is 1 mm, inside the 5 mm margin (a missed speculative contact). The fulcrum at 2° reports 3.25 mm against a true 1 mm.
- **P2b.** A tall drum knocked over diagonally at ω 6 rad/s: 10.5 mJ/tick energy rise and 8.8 mm penetration. At azimuth 0: 0 mJ and 1.8 mm.
- **P8.** Thirty random-orientation dead drops: the worst per-tick energy rise is **4.35 mJ** for the cylinder, against 0.018 mJ for a box of the same size.
- **S2/S3, rim-regime SAT** (300 random poses each, at a true separation of +3 mm):
  - Cylinder–box: 13/300 report a negative gap. 7 err by more than 5 mm; the worst is −11.7 mm.
  - Cylinder–cylinder: 37/300 report a negative gap. 24 err by more than 5 mm; the worst is −26 mm.
  - At +20 mm, cylinder–cylinder gives 4 false positives (gap −5.5 mm).
  - At −4 mm, the depth is overestimated by up to 26 mm.
  - The worst cases all carry feature Rim (kind 6), and their normals are 13–15° off the true separating direction.
- **D, phantom push.** A motionless, weightless dynamic partner placed ≥ 2 mm (true) from a static cylinder:
  - 32/200 cylinder partners and 11/200 box partners are pushed;
  - one moved 18.6 mm in 0.5 s.

## Mutations (throwaway worktree; each reverted, `cmp`-verified against the candidate, logs in `mutations/`)

| ID | Mutation | Result |
|---|---|---|
| A | `RollingArm` returns r for any cylinder manifold (cap contacts roll too) | **Survived** (50/50) |
| B | `FacePreference = 0` | **Survived** |
| C | `InsideTolerance = 0` | **Survived** |
| D | Every cylinder feature id is constant 0 (warm starts mismatched) | **Survived** |
| E | Cylinder `ColliderExtent` = 0 (no spin fattening) | **Survived** (P7 is unchanged too) |

## Findings

**F1 (blocking, High): the cap-regime plane manifold misses the true lowest rim point.**
- *Criteria:*
  - the AC "contact follows the true … flat end faces against … the workbench plane";
  - the AC "rests on an end face without jitter";
  - the envelope contact slop (resting ≤ 0.5 mm, temporary ≤ 5%);
  - "no free energy".
- *Problem.* For |a·n| ≥ 0.98, `CollideCylinderPlane` uses the four fixed rim samples (±U0, ±U2). The spec disclosed this as "pivots on the inscribed square". The probes show it is player-visible:
  - a diagonally tipped rod never rights itself and cycles at about 11° indefinitely;
  - the Pulley-size wheel sinks 22 mm;
  - contacts inside the margin are not generated;
  - energy is injected at the 11.48° regime switch.
- *Risk.* Any drum, wheel or post standing on its end, and bumped off-axis by a ball, sinks or rocks. That covers the 10.2 Counterweight if it is a cylinder, and the shaft wheels.
- *Why blocking.* These are known envelope and AC violations (AGENTS.md: a known violation is Fail).
- *Smallest resolving check:*
  - make the cap manifold contain the exact deepest rim point (radial direction of −n). For example, use the deepest point plus points at 90° from it, or add it to the samples and `ReduceManifold`, so it is continuous with the rim regime;
  - add native tests at a diagonal azimuth: a rod tipped 10.5° rights itself; true rim penetration ≤ 0.5 mm resting and ≤ 5% transient; no contact missing within the margin at tilts of 2–11.4°;
  - the reviewer re-runs P1, P2, P2b, P6-cap, P8 and T1.
- The 8-sample cap-on-box and disc-on-disc paths have the same structure. Their worst case, about r(1 − cos 22.5°)·sin 11.5° ≈ 0.015 r, is **analytic and was not probed**. Cover them with the same check.

**F2 (blocking, High): the rim-regime SAT directions are not the true separating directions for cylinder–box and cylinder–cylinder.**
- *Criterion:*
  - cylinder–box: the AC "contact follows the true curved side and flat end faces against spheres, boxes …";
  - cylinder–cylinder: the spec's PRIMARY "Pairs" list and its §Narrowphase, because the AC does not list it as a partner;
  - the contact envelope.
- *Problem.* The rim-to-edge, rim-to-side and rim-to-rim candidate axes are single refinement steps. When no candidate is near the true axis, the rim-regime contact takes `gap = best`. The result:
  - separations are underestimated by up to 11.7 mm (box) and 26 mm (cylinder);
  - 4–12% of near-touching random poses report penetration while 3 mm apart. These poses are drawn uniformly over rotation space, not weighted toward play, so the rate is not a play-frequency estimate;
  - the engine visibly pushes bodies that are not touching. The D probe ran without a same-engine box–box control; it corroborates S2 and S3 rather than standing alone.
- The blocking basis is the worst-case magnitude (cm-scale, against a 0.5 mm resting slop) together with the confirmed engine push, not the prevalence.
- The spec's "known limits" called these one-step refinements but did not quantify the error, and no test checks the gap against an exact oracle away from the axis-aligned cases.
- *Smallest resolving check:*
  - the check is oracle-based: a native sweep test (box–box as its control) asserting |gap − exact| ≤ 0.5 mm in separated poses within the margin, and no negative gap when separated;
  - the choice of method, and whether it complies with §Narrowphase ("closed-form … without iterative GJK/EPA loops"), stays with the implementer and the coordinator. The reviewer does not pre-approve a design;
  - the reviewer re-runs S2, S3 and D, and expects 0/200 pushed.

**F3 (Medium; required with the fix round): tests do not discriminate spec-claimed behaviours.**
- Mutations A (cap contacts add no rolling row, a coordinator decision) and D (feature ids fixed in the cylinder frame, so warm starts are stable) survive. Both are stated in the spec and in `gpu-f32-physics.md`.
- *Smallest check:*
  - **A (required):** one native test where cap-on-surface rocking or sliding with Crr > 0 differs from the curved-side case. "Caps add no rolling row" is a coordinator behaviour decision.
  - **D:** either a native test showing an observable consequence of feature identity (cap-stack convergence within N ticks, or impulses persisting across ticks), or soften the claim in the spec and in `gpu-f32-physics.md`.
- B, C and E (tolerances and spin fattening) are Low and advisory.

**N1 (Low).** The rolling arm takes the larger radius, so a ball over a large static drum decelerates in proportion to the drum radius. This is consistent with the sphere–sphere precedent and the updated Materials row. Owner awareness only.

**N2 (Low).** A Godot default-sized cylinder or box proxy with omitted keys is refused at build time. This is the intended explicit refusal; authors must state `radius`/`height`/`size`.

**N3 (process).** The tests were written after the code; the spec discloses this. Some reviewer probe files were created with shell redirection in the throwaway worktree. They are preserved as `probes/*.cs.txt` evidence, and no candidate file was touched.

## Re-review scope after the fixes

- The full matrix rows for F1, F2 and F3.
- The probes P1, P2, P2b, P6 (cap, S2, S3), P8, T1 and D, plus mutations A and D.
- One rebuilt Debug full solution, the CI step, the Release worker build and `git diff --check`.
- Then the merge of main (C-Gate) and the post-merge SnapshotApproval.

---

# Re-review 1 (23 October 2026): fix round 1

The same independent reviewer. No candidate file was edited. The probes ran in the throwaway worktree `probe2-10-1` (base plus the candidate diff plus probe files), which has since been removed. Evidence is in `evidence-10-1-review/round2/`.

- **Candidate:** the diff sha256 at the start was `79e8c8b9…66927f`, and the test file `f1c37634…a9fa6c`.
  - About 1 s after that snapshot, `docs/gpu-f32-physics.md` and the spec changed again (the coordinator's ratification wording). The diff at the end is `9959c2ab…2b7bbc`.
  - No `.cs`, test or tool file changed (`find -newer`), so the build, the tests and the probes all apply to the end state.
- **Verdict: Fail.**
  - F1, F2 and F3 are **resolved**.
  - New blocking finding **F4**: energy is gained in dynamic cylinder–cylinder and cylinder–box contacts, beyond the candidate's own bound.
  - Overlap depth (coordinator item) is a Medium finding, not independently blocking, and is folded into F4's fix.
- **Browser qualification** is still pending.

## Commands (one rebuild of the fix-round candidate)

| Check | Result | Log |
|---|---|---|
| `dotnet build CuriousContraptions.slnx -c Debug` | 0 W / 0 E | round2/build-debug.log |
| `dotnet test … --no-build --no-restore` | 2429 / 189 / 77 / 163 / 17, 0 failed | round2/test-slnx.log |
| CI tools step (body diffed equal to `pages.yml`:51-64), under bash | 77 / 163 / 17 / 174, exit 0 | round2/ci-tool-step.log |
| Release worker build | 0 W / 0 E | round2/worker-release.log |
| `git diff --check`, plus `--no-index` on the untracked test file | clean | round2/diff-check.log |
| Implementer's round-2 mutation logs | Every `.build.log` shows 0 errors, and every "killed" test log names real failing tests: n10 by the cap-roll test, n11 by the tilted-cap landing envelope. No stale-binary runs | (read in place) |

## Oracles

- **Separated gaps (dual-certified).**
  - The upper bound is |a − b| after alternating closed-form projections in double precision.
  - The lower bound is the exact support-function separation along the primal direction, refined locally; any direction gives a valid lower bound.
  - A pose counts as certified when the two bounds differ by less than 1e-5 m.
  - An uncertified pose is classified against the interval [lower, upper], not dropped.
- **Overlap depth (certified translation bound).** B is placed δ back from a position along `dir` that is certified as separated. The true minimum-translation depth is then at most δ + 0.2 mm.
- **C2 is retracted.** Its pattern-search depth bound was loose: it read tens of mm in poses that were actually touching. `r2-c2*.log` is invalid and is not cited.
- **On the implementer's caution.** Round 1's pattern search maximised the support separation, so it gave a *lower* bound on separation. A kernel gap below that lower bound was certainly an underestimate, so round-1 F2 stands. The ±0.72 mm box–box spread in round 1 was looseness in the oracle.

## Results

| Item | Evidence | Status |
|---|---|---|
| F1: diagonal tip, penetration, missing contacts | P1: at azimuths 0, 22.5 and 45, the rod rights itself from 8–13° (final tilt ≤ 0.03°) and topples at 15°; penetration ≤ 0.34 mm. P2 (Pulley, fulcrum, drum at azimuths 0 and 45): true rim penetration ≤ 0.41 mm. P6-cap: every case 2–11.6° has its 1 mm contact. P2b: penetration ≤ 0.58 mm, no energy rise | **Resolved** |
| F2: separated gaps | C1 (certified), at 1, 3 and 20 mm; cylinder–box 200, cylinder–cylinder 200, coin–coin 100, cylinder–coin 100, and a box–box control of 80. No error > 0.5 mm (worst +0.39 mm, cylinder–box at 3 mm), no negative gap, no contact beyond the margin, no missed contact. Uncertified poses classified: 0 fail, 0 negative, 8 indeterminate | **Resolved** |
| F2: engine push | C3: motionless, weightless partners certified ≥ 2 mm apart. Box–box control 0/93, cylinder–box 0/182 and cylinder–cylinder 0/190 moved. Positive control (2 mm overlap): 30/30 moved | **Resolved** |
| F3 | The cap test (`ACylinderOnItsCapIsUntouchedByRollingResistance…`) is bit-identical across Crr 0 and 0.1 for sliding and rocking, and the side roll is slowed; n10 kills it. The feature-id claim is softened in the spec and the doc, and n11 is killed by an observable consequence | **Resolved** |
| Random drops onto the plane | P8: cylinder 0.017 mJ, box 0.018 mJ | Pass |
| Plain drops onto static partners (C8, 40 each, with bench) | Box→box 0; box→lying cylinder 0.60; cylinder→box 0.05; **cylinder→lying cylinder 5.20 mJ/tick** (1 of 40 drops is above the 1.96 mJ bound). Sphere→sphere and sphere→lying cylinder (C9): 0 | **F4** |
| Box tumbling over a cap rim (C6, rotated so the box starts on top, normal gravity, 0.3 mm certified overlap) | Cylinder–box: 3 of 21 poses spike to **20.4, 22.8 and 3.2 mJ/tick**. Box–box control (20 poses): ≤ 0.154 mJ. Every traced spike tick (C7) carries the multi-point cap-on-box manifold (feature kinds 2, 3, 4) | **F4** |
| Overlap depth (C4, certified) | Box–box control 80: 0 over the bound. Cylinder–cylinder 200: 0. Cylinder–box 200: kernel deeper than the certified bound by > 0.5 mm in 2 / 3 / 5 poses at δ 0.3 / 2 / 5 mm (worst +0.79 / +3.03 / +7.47 mm), all on cap-on-box points (kind 4). Weightless engine at 0.3 mm: 1/200 cylinder–box partners drifts at 0.0286 m/s (pose 135, where the kernel depth was accurate: C5). Controls ≤ 0.0002 m/s | Medium (N5) |
| Doc ratification condition | §Narrowphase states `SegmentSteps` 24, `RimSamples` 16 and `RimSteps` 16, with per-pair costs (box 312 + 12 evaluations, cylinder 136 + 4) and "Ratified … 23 Oct 2026"; there is no "pending" mark. The counts match the code (`GoldenMinimum` makes 2 + steps evaluations) | Pass |
| Support rule (coordinator decision 2) | Code read; rolling about the axis is unchanged (u′ = u). The ramp at 60 and 240 Hz is within 0.04%. Attribution: with n9 (no tumble term), C8 has 21 of 40 cylinder→cylinder drops and 11 of 40 cylinder→box drops over the bound. The term removes most of the gain, but drop 17 is not explained by it | Pass (residual is F4) |
| Tunnelling, rest, determinism | P7, P3 and the suite are unchanged and clean | Pass |

## Findings

**F4 (blocking, High): energy is gained in dynamic cylinder–cylinder and cylinder–box contacts.**
- *Criterion:* the envelope's "no free energy", measured against the candidate's own bound (box baseline + m·g·ContactSlop ≈ 1.96 mJ per tick for 1 kg). `RandomDropsGainNoMoreEnergyThanABoxAndTheSlop` applies that bound to the plane only.
- *Evidence:*
  - A cylinder rolling over a static lying cylinder gains 1.5–5.2 mJ per tick for 9 ticks, about 30 mJ in total (C8 drop 17; parallel side contacts, kind 5, both on the axis support). The kernel gaps are accurate there (−0.13 to −0.19 mm against a certified −0.2 mm).
  - A box tumbling over a cap rim spikes to 20–23 mJ in a single tick.
  - The controls are about 0: sphere-on-sphere 0, sphere-on-lying-cylinder 0, box–box ≤ 0.15 mJ.
- *Scope:* ordinary drops of a box onto a lying cylinder (0.60 mJ) and of a cylinder onto a box (0.05 mJ) are within the bound.
- *Why blocking:* it is a known violation (AGENTS.md). Round 1 treated 4.35 mJ per tick as blocking for the same reason.
- *Smallest resolving check:*
  - native tests for the C8 cylinder-on-lying-cylinder set and the three C6 tumble poses (seed 402: poses 2, 4 and 135; the probe source is in `round2/probes`), each held to box + m·g·ContactSlop;
  - the reviewer then re-runs C8, C6 and C7.

**N5 (Medium; fold into F4's fix round): cylinder–box overlap depth is overestimated by the cap-on-box manifold.**
- About 1–2.5% of overlapping poses are affected. The worst is +7.47 mm at 5 mm. That is under the 5%-of-smaller-body limit (10 mm), so this is not independently blocking.
- Cylinder–cylinder has none.
- *Check:* a native overlap test using C4's certified-touch method.
- **m6** (radial-vertex SAT axes) is a live coverage gap, not an equivalent mutant. It can affect only the overlap regime, through max(SAT, feature), because the closest features re-decide the early-out. It does not cause the kind-4 overestimate. Rate it Low and cover it with the same test.

**N6 (Low): stale XML summaries.**
- `CollideCylinderPlane` still says "four rim points … at ±r along U0 and U2".
- `CollideCylinderBox` still lists the deleted rim-to-vertex and rim-to-edge axes.

**N7 (Low): observations.**
- In C5 pose 2 the normal alternates between the cap and side directions from tick to tick, at a speed of 0.0005 m/s.
- The tilted-landing rises (≤ 1.22 mJ, measured by the implementer) are under m·g·slop, but have no tilted-box baseline.

**N8 (process).**
- The reviewer's wait loops matched their own command line (`pgrep -f`) and stalled for about 40 minutes; no result is affected.
- One line was appended to `round2/identity-at-end.txt` with `echo`.
- The probe sources are kept as `round2/probes/*.cs.txt`.

## Re-review scope after F4

- C8, C6 and C7 (and C4 for N5), plus the new native tests.
- One rebuild, the full slnx, the CI step and the Release worker build.
- Then the merge of main (f27cb71d, or later if Battery lands) and SnapshotApproval.

---

# Re-review 2 (23 October 2026): fix round 2

The same independent reviewer. No candidate file was edited. The probes ran in throwaway worktrees `probe3-10-1` and `probe4-10-1`, both since removed. Evidence is in `evidence-10-1-review/round3/`; the probe sources are in `round3/probes/*.cs.txt`.

- **Candidate:** diff sha256 `2260fa35…70a6c0`, test file `5893cd8b…92ac91`. Both were unchanged from the start to the end of the review (`round3/identity-*.txt`).
- **Verdict: Fail.**
  - F4 is resolved for the original sets and for every fresh tumble pose.
  - Fresh-seed drops still exceed the candidate's own bound. This is the over-fit the coordinator asked about.
- **Browser qualification** is still pending.

## Commands (one rebuild)

| Check | Result | Log |
|---|---|---|
| Debug build | 0 W / 0 E | round3/build-debug.log |
| Full slnx `--no-build --no-restore` | 2434 / 189 / 77 / 163 / 17, 0 failed | round3/test-slnx.log |
| CI tools step under bash | 77 / 163 / 17 / 174, exit 0 | round3/ci-tool-step.log |
| Release worker build | 0 W / 0 E | round3/worker-release.log |
| `git diff --check` (tracked and untracked) | clean | round3/diff-check.log |

## Results (dual-certified separation; certified translation bound for overlap)

| Item | Evidence | Status |
|---|---|---|
| F1 still holds | P1 (azimuths 0, 22.5 and 45; 8–13° rights itself, 15° topples), P2 (≤ 0.41 mm), P2b, P6-cap (20/20 contacts present), P3, P4 (−0.04%), P7 and P8 (0.017 vs box 0.018 mJ) are all unchanged and clean | Pass |
| F2 still holds | C1: no error > 0.5 mm, no negative gap, nothing beyond the margin or missed; uncertified poses 0 fail. C3: 0/183 cylinder–box and 0/190 cylinder–cylinder partners moved (control 0/100; positive control 30/30) | Pass |
| F3 still holds | Suite cap test, n10 and n11 killed (implementer logs) | Pass |
| N5, overlap overestimate | C4: 0 of 200 cylinder–box and 0 of 200 cylinder–cylinder poses deeper than the certified bound at 0.3, 2 and 5 mm (worst +0.07 mm). Weightless engine at 0.3 mm: worst 0.37 mm and 0.0003 m/s, against controls of 0.29 mm and 0.0002 m/s | **Resolved** |
| F4, original sets | C8 (seed 77, 40 drops): cylinder→log 0.64, cylinder→box 1.15, box→log 0.60 mJ. C6 poses 2, 4 and 135: 0.00 mJ (they were 3.2, 22.8 and 20.4) | Resolved |
| F4, fresh tumble | D2 (seeds 1401–1404): box over cylinder rim (60) 0.92, cylinder over cylinder rim (60) 1.19, cylinder over box edge (30) 0.22 mJ. Box-over-box control (30): 0.23 mJ | Resolved |
| **F4, fresh drops** (D1: 3 seeds × 60, random spin ±3 rad/s and height 0.1–0.6 m) | Box→box ≤ 0.18 and box→log ≤ 0.60 mJ (0 drops over the bound). **Cylinder→box: seed 2002 drop 15 4.65 mJ, seed 77 drop 25 2.29 mJ. Cylinder→log: seed 1001 drops 6 and 25 at 2.06 and 3.59 mJ.** That is 4 of 360 cylinder drops over 1.96 mJ | **Open** |
| 2° parallel band | D3b and D4b, with the skewed log placed 0.3 mm below its certified touch: 0.5–12° skew, worst rise ≤ 0.003 mJ, settles to parallel. Sweeps through 2° (1–3 crossings, frictionless): ≤ 0.002 mJ, net energy falls. No discontinuity or jitter at the threshold. Roll-off-edge D10: 0° and 20° skew 0 mJ; 5° skew ≤ 1.57 mJ (sphere 0) | Pass |
| Shrunk-core bias (coordinator item 3) | D6: caps, sides, faces, parallel and crossed sides are exact at 0.2–2 mm. D5: no early contact (gap at touch ≤ 0.07 mm). Cylinder–box rims touch late by ≤ 0.375 mm. D7 resting interfaces sit 0.27–0.34 mm deep, matching boxes (three-stack 1.29 mm against 1.22 mm for boxes). No visible gap | Pass for cylinder–box; cylinder–cylinder: N9 |
| Retracted | D3 and D4: the skewed log started below its certified touch, so their rises (up to 71 mJ) were a setup artifact. Only D3b and D4b are cited | — |

## Findings

**F4 (blocking, residual): fresh-seed drops still gain energy beyond box + m·g·ContactSlop.**
- 4 of 360 cylinder drops exceed it (2.06–4.65 mJ per tick). The box→box control in the same design stays at or below 0.18 mJ.
- *Traces (D9, `round3/r3-d9.log`):*
  - **cylinder→box:** a two-point side-on-face manifold has one end lifting (gaps −0.19 and +0.07 → +0.23 mm) over about 9 ticks of rising energy. The single side point then jumps to −1.41 / −2.13 mm.
  - **cylinder→log:** a single side point (kind 5, |n·a| 0.04–0.13) starts 2.4–3.7 mm deep, beyond the 1 mm core cutoff, and gains energy every tick while it pushes out.
- *Smallest resolving check:*
  - native D1-style drop sets on new seeds, with spin and height variation, plus a cylinder rolling off a box edge skewed 0–20°, each held to the bound;
  - the reviewer then reruns D1 on a hold-out seed that is not shared.

**N9 (Medium; fold into the F4 round): the cylinder–cylinder core gives late, non-monotone contact at rims.**
- D5 (60 rim-aimed walks) shows a gap of +0.768 mm while 0.05 mm overlapping: the gap jumps 0.001 → 0.768 mm one step deeper. That exceeds the 0.5 mm resting slop.
- With f4 applied (no cylinder–cylinder core), the worst positive gap is 0.021 mm, the jump is 0.053 mm, and cylinder→log energy is unchanged (seed 1001: 2.85 mJ against 3.59).
- **f4 is not an equivalent mutant:** it changes observable gap behaviour, and no test covers it.
- *Check:* remove or halve the cylinder–cylinder core, and add a native test that the gap through touch is monotone and that any positive gap while overlapping is ≤ 0.5 mm.

**m6:** Low. Its overlap-depth exposure is now bounded by C4 (no overestimate), and it remains a coverage gap for underestimates only.

**N10 (Low):**
- The cylinder three-stack keeps a late band of 0.11 mm and spin of 0.02 rad/s; the box stack's are 0.
- After 4 s at 12° skew, the log still spins at 0.68 rad/s. Energy never rises.

**N11 (process):** a wait loop matched its own command line once more. It was killed, and no result depends on it.

## Re-review scope

D1 on the new and hold-out seeds, D9, D5 and D10, plus C1/C3/C4 and the full verification. Then merge main (now well past a598a09e: C-Gate, Battery and C-Lamp, with Domino pending) before SnapshotApproval.

---

# Re-review 3 (24 October 2026): round 4, the single emission fix

The same independent reviewer. No candidate file was edited. The probes ran in throwaway worktrees `probe5-10-1` (the candidate) and `base-10-1` (base a598a09e only), both since removed. Evidence is in `evidence-10-1-review/round4/`; the probe sources are in `round4/probes/*.cs.txt` (R3 plus a restitution probe), with R1 and R2 as before.

- **Candidate:** diff sha256 `223fa82c…c260f1`, test file `27418594…c5b0be`. Both were unchanged from the start to the end of the review.
- **Verdict: Pass-pending-merge**, judged on the non-restitution gates (Dead and Rolling), per the coordinator decision below. No SnapshotApproval yet: main is at ae3cacf9+, and the implementer must merge it before publication.
- **Browser qualification** (`engine-cylinder`) is still pending.

## Coordinator decision recorded (24 Oct 2026, under owner delegation)

- Restitution gains energy engine-wide, and the gain is pre-existing. It is a shared contact-law defect, not cylinder scope.
- 10.1 is judged on the Dead and Rolling gates.
- A dedicated engine slice, "restitution energy correctness", runs immediately after 10.1 publishes and before 10.2a. That slice re-gates cylinders with restitution.
- The explicit `[Fact(Explicit = true)]` Lively gate (`FreshSeedDropsWithRestitution…`) is currently red. It is that slice's entry check.

**Reviewer confirmation that the defect is pre-existing.** The probe ran on base a598a09e with no cylinder code: 60 random drops per row, with spin and height variation.

| Restitution | Lone box on bench | Box onto static box | Lone sphere on bench |
|---|---|---|---|
| e = 0 | 0.00 mJ, 0 drops over 1.96 mJ | 0.00, 0 | 0.00, 0 |
| e = 0.3 | 155 mJ, 10 drops over | 211 mJ, 12 | 556 mJ, 5 |
| e = 1 | 3868 mJ, 60 drops over | 8250 mJ, 60 | 7083 mJ, 21 |

The candidate gives bit-identical numbers for these cylinder-free scenes, so the slice did not introduce the gain. The sphere also gains energy when it is spinning; the implementer's 0.001 mJ figure is for a non-spinning case. Owner: the restitution slice.

**Required tracking (non-blocking for 10.1).** TODO.md records the unmet Lively gate, but neither TODO.md nor the roadmap yet names the restitution slice and its position before 10.2a. The coordinator should add it when 10.1 publishes.

## Commands (one rebuild)

| Check | Result | Log |
|---|---|---|
| Debug build | 0 W / 0 E | round4/build-debug.log |
| Full slnx `--no-build --no-restore` | 2443 / 189 / 77 / 163 / 17, 0 failed | round4/test-slnx.log |
| CI tools step under bash | 77 / 163 / 17 / 174, exit 0 | round4/ci-tool-step.log |
| Release worker build | 0 W / 0 E | round4/worker-release.log |
| `git diff --check` (tracked and untracked) | clean | round4/diff-check.log |
| Implementer's round-4 mutation logs | All 41 builds have 0 errors. 37 are killed, each by named failing tests, and the 4 survivors match the spec | (read in place) |

## Results

| Item | Evidence (the bound is the stricter 1.96 mJ, below the gate's own 2.19 / 2.20) | Status |
|---|---|---|
| **F4: held-back seeds 31337 and 4242, never shared** | D1 (60 drops each, spin and height varied), Dead and Rolling: cylinder→box ≤ 1.01, cylinder→log ≤ 1.41 mJ. 0 over | **Resolved** |
| F4: round-3 seeds 1001, 2002 and 77 (the 4 earlier exceedances) | Dead and Rolling: 0 over, worst 1.23 mJ (cylinder→log, seed 77 Dead was 1.04). Seed 2002 drop 15 (was 4.65 mJ): the class worst is now 0.59 | Resolved |
| F4: tumble | Seeds 1401–1404 plus held-back 31402/31403, Dead and Rolling (330 cylinder poses per material): worst 1.86 mJ (held-back box over rim, Dead), 0 over. Controls ≤ 0.23. C6 poses 2/4/135: worst 0.276 mJ | Resolved |
| C8 (seed 77, 40 each) | Cylinder→box 0.80, cylinder→log 0.51, box→log 0.60 mJ | Resolved |
| Roll off a box edge (D10) | 0°, 5° and 20° skew at 0.3–2 m/s: ≤ 0.03 mJ (5° was 1.57). Sphere 0 | Pass |
| 2° band | D3b: 0.5–12° skew ≤ 0.003 mJ. D4b: band crossings ≤ 0.002 mJ, net energy falls | Pass, no over-fit |
| Shrunk cylinder–box gap at touch | D5: gap at touch ≤ 0.063 mm. The worst positive gap while overlapping fell from 0.375 to 0.035 mm (core clamp). Cylinder–cylinder: 0.021 mm, no core, jump ≤ 0.053 mm (N9 resolved). D6: corner over rim at 0.2 mm reads 0.000 (was +0.131); faces exact | Pass |
| Rest and stacking (D7) | Interfaces 0.27–0.34 mm deep. Three-stack 1.287 mm against 1.222 mm for boxes. Late band 0.084 mm (was 0.11), late spin 0.007 rad/s | Pass (N10 Low remains) |
| F1 | P1 (every azimuth and tilt rights itself, topples at 15°), P2 ≤ 0.41 mm, P2b, P6-cap with no missed contact, P3, P4 at −0.04%, P7, P8 (0.017 vs 0.018 mJ) | Pass |
| F2 | C1: 15/15 rows with no error > 0.5 mm, negative gap, contact beyond the margin or missed contact; uncertified poses 0 fail. C3: 0/183 and 0/190 moved (control 0/100, positive control 30/30) | Pass |
| F3 / N5 | n10 and n11 killed. C4: 0 poses over the certified bound (worst +0.07 mm); engine ≤ 0.37 mm, 0.0003 m/s | Pass |

## Surviving mutants

- **g1** (re-orthogonalisation kept, threshold 1e-9): equivalent per analysis. The re-orthogonalisation carries the fix and the threshold has no observable effect. Low.
- **g2a** (no edge closest point) and **n7** (cap-on-box without the deepest sample): redundant on the exact and core paths through KeepClosest. They differ only in overlaps deeper than 1 mm, where C4 certifies overestimation only, not underestimation. A Low coverage gap.
- **m6:** Low, as before.

## Remaining Low notes

- **N10:** the three-stack late band is 0.084 mm.
- **D5:** at the 1 mm core cutoff the cylinder–box gap steps by 0.461 mm (−0.986 → −1.447 mm). It is monotone and well inside the 5% temporary-penetration envelope.
- **N7:** there is still no tilted-box baseline for the 1.22 mJ landing rise.

None of these blocks.

## Before publication

1. The implementer merges current main (ae3cacf9+).
2. The reviewer re-runs one build, the full slnx, the CI step and the Release build on the merged candidate.
3. Re-run the D1 held-back seeds and C1 on the merged kernel.
4. Then SnapshotApproval.
5. The restitution slice is recorded as next, before 10.2a.
