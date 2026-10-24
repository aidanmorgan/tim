# Review: Engine slice "Restitution energy correctness"

This is an independent adversarial review. The reviewer did not write the candidate and edited no source, test, tool, content or document file in it. The probes ran in throwaway worktrees, which were removed afterwards.

## Merged-candidate re-verification (24 Oct 2026): Pass, SnapshotApproval below

**Candidate.** HEAD `2c668ecd` (merge of main `f5ffdb4e` into WIP `e0d7b23d`) plus the uncommitted merge-round changes. Start and end identities are equal (`evidence-engine-restitution-review/merged/identity-start.txt`, `identity-end.txt`). `origin/main` is `f5ffdb4e`.

**Merge integrity.**
- **The WIP commit is the round-2 candidate.** `e0d7b23d` reproduces it byte for byte: `git diff cf35a064 e0d7b23d` over the round-2 tracked files hashes to `a97587a0…`, and with the restitution test file appended to `1694734e…`. Both equal the round-2 identity.
- **The merge contains only main's changes plus resolutions.**
  - The merge base is `cf35a064`.
  - Every file only main changed equals main.
  - Every file only the slice changed equals the WIP.
  - The overlapping files (tests csproj, `EventRingProducerTests`, `PressurePlateTests`) equal `git merge-tree` output.
  - The only differences from the automatic merge are the `sprint-status.yaml` conflict, resolved by keeping both sides' rows, and a `TODO.md` status line.
- **The C-Memory receipt is byte-identical to what was carried.**
  - `cmem-terminal-receipt.patch` applied to HEAD gives exactly the working files: the three review records and `sprint-status.yaml`.
  - `diff -r` of `carry/cmem-terminal-evidence` against `evidence-c-memory-review/terminal/` is identical (10 files).

**Conditions.**
- **R2-1:** met. The `deferred-work.md` entry has the reproducer, the numbers, the owner and the plank/crate/seesaw check, and the §Restitution limitation sentence is in the doc.
- **R2-2:** met. The energy row states the per-480 Hz-substep bound and the tall-stack residual.
- **R2-3:** met. `TheEnergyBoundKeepsABounceWithinTheRelaxedEnergyAndScalesOneBeyondIt` kills MB2, re-run by the reviewer on the merged engine (`merged/MB2-mutation.log`).
- **Also present:** the SIGSEGV deferral.

**Gates** (`evidence-engine-restitution-review/merged/`):

| Check | Result |
|---|---|
| Debug slnx build, `--no-incremental` | 0 W / 0 E |
| Full slnx `--no-build --no-restore`, run alone | tests 2928, E2eRunner 190, BundleElements 191, Coverage 77, WorkerAotGuard 17; 0 failed, 0 skipped |
| CI tools step, body unchanged | 77 / 191 / 17 / 174, exit 0 |
| Release `--no-incremental` builds, Simulation and Animation.Worker | 0 W / 0 E |
| `git diff --check` | clean |
| Lessons, restitution class and Lively | 89/89, including 12 authored lessons × 3 precisions |

**F1 probes on the merged engine** (`merged/f1-probes-merged.log`). The results are identical to round 2:

| Probe | Result |
|---|---|
| Reproducers | 0.051 / 0.000 mJ |
| Sweep at e = 0.9 / 0.95 / 1 | 0.241 / 0.314 / 0.255 mJ |
| Held-back classes | worst 1.575 of 1.962 mJ |
| Isolation, Reset replay and fresh engine | bit-identical |
| Tall stacks at catalogue and mixed e | within bound |

The tall e = 1 stacks show the documented residual (21.0 and 27.3 mJ rise; genuine gain 2.1 and 7.3 mJ). These are the probe's 2 strict-rise assertions, outside the qualified scope as the energy row states.

**Verdict: Pass**, scoped to native and publication.
- Browser qualification is pending (post-catalogue E2E).
- Remote and deployed identities are to be verified after publication.

### SnapshotApproval (independent reviewer, 24 Oct 2026)

**Approved snapshot.**
- Base commit: HEAD `2c668ecd`.
- `origin/main` is `f5ffdb4e`, the merged parent.
- Everything was staged with `git add -A` (40 paths), before this section was appended.
- `git write-tree` = `e4bf4c7c45a8d65ab24cefd3b1c0b9a5f7df8c5e`.
- sha256 of `git diff --cached --binary 2c668ecd` = `7a98e4da867effe05fb708ce295c666aa27d05385df827e4abfef366d979269a`.

**Scope of the approval.** This approval adds only this section to this file, and the file is then re-staged. Any other change to relevant inputs invalidates the approval.

**Criteria verified before publication:**
- merge integrity;
- the C-Memory receipt bytes;
- conditions R2-1 to R2-3;
- build, full slnx, CI tools step, Release workers and diff-check;
- lessons at 3 precisions;
- F1 probes.

**Checks that depend on publication:**
- the pushed commit's tree matches this staged tree plus the commit's own approval text;
- the push and the Pages build/deploy jobs succeed for that commit;
- the deployed artifact identity.

The implementer may commit, push and deploy this snapshot. The reviewer then verifies the remote and deployed identities before the terminal scoped Pass. Browser qualification stays pending.

## Round 2 re-review (24 Oct 2026): Pass-pending-merge (superseded by the merged verification above)

**Candidate.** Worktree `story-engine-restitution` on base cf35a064, uncommitted.
- `git diff` sha256 `a97587a0…10f0f7`.
- Diff plus untracked non-`_bmad-output` files: sha256 `1694734e…9df4ff`.
- Start and end identities are equal (`evidence-engine-restitution-review/round2/identity-start.txt`, `identity-end.txt`).

**Verdict: Pass-pending-merge.**
- F1 (blocking), F2 and F3 are resolved.
- There are no unresolved unintended regressions in the required-now scope.
- Main has moved to f5ffdb4e (C-Memory). The implementer merges main next, then SnapshotApproval.
- **Two conditions for the merge round.** SnapshotApproval verifies both (R2-1 below):
  - a `deferred-work.md` entry;
  - one sentence in `docs/gpu-f32-physics.md` §Restitution.
- Browser qualification stays pending (post-catalogue E2E).

### Commands and results (`evidence-engine-restitution-review/round2/`)

| Check | Result | Log |
|---|---|---|
| `dotnet build CuriousContraptions.slnx -c Debug --no-incremental` | 0 W / 0 E | build.log |
| `dotnet test CuriousContraptions.slnx -c Debug --no-build --no-restore`, solo rerun | tests 2847, E2eRunner 189, BundleElements 189, Coverage 77, WorkerAotGuard 17; 0 failed, 0 skipped | test-full.log |
| Run 1 of the same | CuriousContraptions.tests crashed with SIGSEGV (exit 139) after 848 tests. It ran while the reviewer's probe builds ran in parallel. The solo rerun on the same build passed. Recorded, not chased | test-full-run1-crashed.log |
| CI "Test C# tools" step (`pages.yml`:51-64, body unchanged since round 1), run under `bash -e` | 77 / 189 / 17 / 174, exit 0 | ci-tools-step.log |
| Release `--no-incremental` builds: Simulation and Animation.Worker | 0 W / 0 E | worker-release.log |
| `git diff --check`, plus `--no-index --check` on the untracked files | clean | diff-check.log |
| `LaunchLessonSolveTests` and `WorkshopPhysicsLawRestitutionTests` | 87/87, including 12 authored lessons × 3 precisions | lessons-and-restitution.log |
| Reviewer probes: round-1 file plus round-2 file, candidate vs base | below | probes/ |
| Reviewer mutations (focused set, 250 tests): MX2, MX4, MB1, MB2, MB3 | 4 of 5 killed; MB2 survived (R2-3). MB1's first form did not compile (IDE0059); its compiling variant was killed | mutations/ |

**Probe instrumentation.** The candidate probes ran with review-only counters in the throwaway copy: `probes/ReviewCounters-instrumentation.cs.txt` and `probes/instrumentation-vs-candidate.diff.txt`. Those counters count energy-bound activations and allow the bound to be switched off. They do not change behaviour unless the probe switches the bound off.

### Round-1 findings

**F1: resolved.** Both reproducers now pass:

| Case | Candidate | Base |
| --- | --- | --- |
| e = 0.95 reproducer | 0.051 mJ | 910 mJ |
| e = 1 reproducer | 0.000 mJ | 3051 mJ |

- The 200-case sweep worst rise is 0.314 mJ at e = 0.95 and 0.255 mJ at e = 1 (bound 5.887). Results at e ≤ 0.9 are unchanged.
- Every round-1 held-back class still passes.
- The claims are narrowed in the spec and docs to the qualified classes, and the rest are described as relying on the bound.
- Routine tests now pin the reproducers and the dynamic-box sweep. Mutants M9 (bound off), MB1 (per-body instead of per-group scaling) and MB3 (scale linear only) are all killed.

**F2: resolved.** `APullingOrRankDeficientBlockFallsBack…` kills MX2. `ABallSetDownTwoMillimetresAboveTheBenchComesToRest` was added.

**F3: resolved.** `ABouncingSpinningBallLosesTheRollingResistanceOfTheWholeImpact` kills MX4.

### Coordinator questions

**Q1. Is the bound principled?** Yes. It is a game-grade passivity clamp, consistent with the AGENTS clamp/continue envelope: a group's kinetic energy after the pass may not exceed max(pre-solve, relaxed). It is targeted:
- **Lessons and element tests:** 0 activations in 3293 bouncing passes across the lessons, the plate tests and the event-ring tests (`probes/scaling-counts.txt`).
- **Tall stacks at catalogue e:** 2–4 activations in about 1300 passes, minimum scale 0.63–0.72.
- **A real gain removed:** in the e = 1 cradle pass it caught 0.843 J after the pass against a 0.707 J bound.
- **What it masks:** restitution is still solved per manifold, not simultaneously over an island. The bound neither causes nor fixes that (Q3 and R2-1).

**Q2. Tall stacks, chains, mixed e.**
- **3- and 4-box stacks with a box or ball dropped on top, 60 seeds each:**
  - at catalogue e and mixed e, the worst rise is 2.1–4.1 mJ against bounds of 10.0–13.7 mJ;
  - at e = 1, the rise is 21.0 and 27.3 mJ, above Σm·g·slop (10.8 and 13.7 mJ), but the genuine gain is 2.1 and 7.3 mJ, within bound;
  - base gains 840–3600 mJ.
  - e = 1 lies outside the qualified classes; see R2-2.
- **Cradle (5 balls, 1 mm gaps, no gravity):** KE never exceeds its initial value, and momentum is exact.
- **Rolling chain on the bench (e 0.55, μ 0.4):** the rise is 0.000 mJ, with a spread of 0.35–0.46 m/s against base 0.36–0.49.

**Q3. Artefacts from the group scaling.**
- **Isolation:** an unrelated ball is bit-identical with and without a scaling group in the scene.
- **Determinism:** a Reset replay and a fresh engine are bit-identical.
- **Group extent:** groups form only from manifolds that bounce in that substep, so a resting stack is never pulled into a group.
- **Dead impacts are not caused by the bound:**
  - the e = 0.55 cradle moves as one plastic block whether the bound is on or off;
  - the ball-onto-resting-box runs record 0 activations.

**Q4. Do F2 and F3 discriminate?** Yes, by MX2 and MX4.

### Remaining items

**R2-1. Medium, non-blocking. A merge-round condition.**

*The finding.* Impacts through a resting dynamic body lose bounce fidelity. When a ball lands on a box resting on the bench, the box–bench contact has about zero approach at substep start, so it absorbs the push from the bounce instead of returning it. Apex height over drop height (`probes/apex-*.log`):

| Case | Candidate | Base | Static-support limit |
|---|---|---|---|
| 0.55 materials, 2 kg box | 0.020 | 0.050 | 0.091 |
| 0.55 materials, 0.5 kg box | 0.006 | 0.011 | — |
| e = 1, 0.3 kg box | 0.45 | 0.19 | — |
| e = 1, 2 kg box | 0.44 | 0.57 | — |

*Why it is not blocking.* This is a fidelity change, not a regression against a stated criterion:
- base is not a valid reference (it is far from ideal and comes from the stale-approach mechanism);
- the direction differs from case to case;
- no criterion covers dynamic supports;
- current catalogue reach is negligible (ball onto domino gives e² ≈ 0.0008, and balls cannot rest on balls).

*The two conditions.*
- A `deferred-work.md` entry, "Engine: simultaneous/supported-impact restitution". Include this reproducer, the numbers above and the owner, marked as a check before any dynamic plank, crate or seesaw element.
- One sentence in §Restitution of `docs/gpu-f32-physics.md` naming the limitation.

**R2-2. Low.** The Thermodynamic Energy row should state that the bound is per substep. Record the measured residual for tall e = 1 stacks: a per-tick rise of 21–27 mJ with genuine gain within bound. This is a wording request; the row is not false.

**R2-3. Low.** The `max(…, relaxed)` branch of the bound is unpinned: mutant MB2, which bounds by pre-solve energy only, survived. The cradle log shows where it matters. When the relaxed energy exceeds the pre-solve energy (budget −14 mJ, scale 0), a small bounce is dropped.

## Round 1 (superseded by round 2 above)

- **Candidate:** worktree `story-engine-restitution`, branch `story-engine-restitution`, base `cf35a064`, uncommitted.
  - `git diff` sha256 `9d7056bb…2861629`.
  - Diff plus untracked non-`_bmad-output` files: sha256 `9eea9ca8…3f9cf1`.
  - Spec sha256 `f1ed5b5b…dae1`.
  - `WorkshopPhysicsLawRestitutionTests.cs` sha256 `9af1d08d…54ed`.
  - Identical at the start and end of the review (`evidence-engine-restitution-review/identity.txt`).
- **Verdict: Fail.** One known violation is blocking (F1). Two coverage gaps make the slice Incomplete (F2, F3).
  - Every other criterion passes.
  - The candidate removes base's energy gains of hundreds to thousands of mJ in every class probed.
  - After F1–F3 are resolved and re-reviewed, the next verdict available is Pass-pending-merge, because main has moved to f5ffdb4e (C-Memory).
- **Browser qualification is pending** (the post-catalogue E2E). Changed bounce trajectories join that queue.

## Commands and results (`evidence-engine-restitution-review/`)

| Check | Result | Log |
|---|---|---|
| `dotnet build CuriousContraptions.slnx -c Debug` | 0 W / 0 E (incremental) | build.log |
| `dotnet test CuriousContraptions.slnx -c Debug --no-build --no-restore` | tests 2837, E2eRunner 189, BundleElements 189, Coverage 77, WorkerAotGuard 17; 0 failed, 0 skipped, no duplicate-ID message | test-full.log |
| CI "Test C# tools" step (`pages.yml`:51-64), body extracted verbatim and run under `bash -e` | 77 / 189 / 17 / 174, exit 0 | ci-tools-step.log |
| Release `--no-incremental` build of `CuriousContraptions.Simulation` and `CuriousContraptions.Animation.Worker` | 0 W / 0 E each | worker-release.log |
| `git diff --check`, plus `--no-index --check` on the two untracked files | clean (no whitespace output) | diff-check.log |
| `LaunchLessonSolveTests`, re-run | 25/25: 12 Playable authored solutions at 3 precisions, every launch route, plus the coverage fact | lessons-guard.log |
| Candidate tests against the base engine (independent red-on-base) | 33 of 70 red: Lively gate, plate impact-tick pin, 31 restitution-class cases. The ring-ordinal guard and the D11 corrections pass on both | red-on-base.log |
| Reviewer probes, deterministic seeds, candidate vs base | see F1 and the matrix | probes/probes-candidate.log, probes/probes-base.log, probes/ReviewRestitutionProbeTests.cs.txt |
| Standalone reproducers (explicit pose, height, offset, spin) | candidate and base | probes/reproducer-candidate.log, probes/reproducer-base.log, probes/trace-e095-case58-candidate.log |
| Reviewer mutations MX1–MX6 (focused set of 240 tests, non-incremental per mutant) | 3 killed, 3 survived | mutations/ |

**Discarded runs.** Probe runs 1–5 are kept in `probes/discarded/` and are **inconclusive, not evidence**:
- they seeded by `string.GetHashCode()`, which .NET randomises per process;
- runs 4 and 5 also ran a stale DLL after a failed build.

Only the deterministic run (`probes-*.log`) and the reproducers are cited.

**Probe source identity.** `probes/ReviewRestitutionProbeTests.cs.txt` is the final source. `probes-*.log` came from the same file before three edits:
- the trace constants changed (`TraceCase` −1 → 58, with an e = 0.95 trace gate);
- `PrintSweepCase` was added;
- `DynamicStackReproducer` was added.

None of these changes the cited assertions or the seeds.

## Findings

### F1. Blocking (Fail): a body dropped onto a resting dynamic body still gains energy at high e; the docs and spec claim it never does

**Measured.** Both cases are reproducible from explicit parameters (`DynamicStackReproducer`):
- **Setup:** a 1 kg box (half 0.25 × 0.1 × 0.15) falls onto a 2 kg box (half 0.3 × 0.1 × 0.3) that rests on the bench. Material is (e, threshold 0.1, μ 0.5), and the bench is e = 1.
- **The bound:** Σm·g·slop = 5.887 mJ.

| Case | Candidate | Base |
| --- | --- | --- |
| e = 0.95 (q = (0.1504, −0.1278, 0.7330, 0.6510), h 0.7498, x 0.0610, spin (−3.456, 0.949, 0.311)) | rise 31.5 mJ; **genuine gain 26.5 mJ at t81** (E(t) minus the maximum of the 3 preceding ticks) | rise 910 mJ |
| e = 1 (q = (0.5394, 0.2810, −0.5952, 0.5251), h 0.5262, x 0.0609, spin (1.523, −0.742, −3.308)) | **rise 455.8 mJ at t47**: energy is removed in one tick and returned in the next | rise 3051 mJ; genuine gain 63 mJ |

The failures come from the 200-case sweep (`BoxOnDynBoxSweep`, seed base 883000 + 100·e). It is clean at e ≤ 0.9 (worst rise 0.33 mJ). It fails at e = 0.95 (rise 128 mJ, genuine 26.5 mJ) and at e = 1 (rise 455.8 mJ). The two reproducers above pin both failures.

The 30-case held-back classes (`HeldBackRandomEnergy`, seed base 880000) all stay within Σm·g·slop at every e ∈ {0, 0.3, 0.55, 1}. That includes the same box-onto-dynamic-box class, which gives 0.19 mJ at e = 1. So a 30-case run can come back green: this residual needs the wider sweep or the reproducers to show. The held-back classes are:
- a 1 kg sphere (r 0.1, spin ±10) onto a resting 3 kg sphere (r 0.2);
- the box onto a dynamic box;
- the static-partner classes: a spinning sphere (±20 rad/s), a box, a cylinder, a cylinder onto a lying cylinder, a sphere onto a tilted box edge, and a rolling sphere with C_rr 0.05.

**Why it blocks.** The slice delivers these statements as true:
- the spec outcome, "Restitution stops adding energy, on every shape and in every pose";
- D8, "no e = 1 exemption";
- D2, "An impact cannot straddle a tick";
- the new `docs/gpu-f32-physics.md` text: Envelope, "Contacts and restitution add at most m·g·s_slop per committed tick … at every restitution e ≤ 1"; and §Restitution, "energy never returns a substep (or a tick) after it was removed".

Each one is falsified for chains through a dynamic body at e ≥ 0.95. No delivered test covers an impact onto a resting dynamic body:
- `BoxOnBox` uses a static box;
- the stack test starts at 0.5–2 mm gaps and checks only resting;
- the ball strike is a head-on hit with no gravity.

This is not a regression: base is far worse. It is also outside the current catalogue's reach (D12: dynamic–dynamic e ≤ 0.30, dynamic–static e ≤ 0.55). But it is a known violation of the slice's own stated scope, and the authority doc would carry a false invariant for future elements.

**Smallest resolving check (either one).**
- (a) A fix, plus a routine native test: a box dropped onto a dynamic box resting on the bench, seeded poses at e ∈ {0.3, 0.55, 0.95, 1}, with per-tick rise ≤ Σm·g·slop. Include both reproducers above.
- (b) A coordinator-signed scope narrowing:
  - correct both doc sentences and the spec outcome, D2 and D8 to the verified scope: a single dynamic body against static partners at every e ≤ 1, and dynamic partners up to e = 0.9;
  - add that routine test at e ∈ {0.3, 0.55} (catalogue reach) plus e = 0.9;
  - record a deferred item ("restitution through dynamic chains at e ≥ 0.95") with the two reproducers.

**Attribution hint for the implementer.** Unconfirmed: re-run case 58 with D5 disabled and with the manifold order reversed. Check whether the lower-box-to-bench link, whose substep-start approach is about 0, bounces a substep after the top impact.

### F2. Incomplete: two of the spec's PRIMARY boundary tests are absent

**The two missing tests.** Neither is delivered by the slice, and no pre-existing equivalent was found:
- "a singular K (coincident points) uses the per-point fallback with no NaN". A suite-wide grep for `singular` and `coincident` in `CuriousContraptions.tests/*.cs` came back empty.
- "a ball set down 2 mm above the bench rests". The grep covered `.002f`, "2 mm" and "set down" across the tests. The only hits are pose tolerances, and a box set 2 mm above another box. The box stacks (0.5–2 mm gaps) cover boxes, not a ball.

**The pull fallback.** MX2 (the block solve accepting a pull) is killed only through the energy tests (Lively, BoxOnBox e = 1). So the fallback has indirect coverage, but the singular branch has none.

**Demoted to a note.** The spec item "a resting body reports an approach below its threshold" (D6) is covered by the pre-existing `WorkshopPhysicsLawSensorWorkTests.RestingSeparatingBelowThresholdAndGrazingGapContactsRecordNoOccurrence`, which is green under the new emission gate in the full run.

**Smallest resolving check:** add the two native cases, or have the coordinator remove them from the spec.

### F3. Incomplete: the D5 rolling re-solve is unpinned (mutant MX4 survived)

D5 claims "friction **and rolling** are solved once more". Deleting `SolveRolling(m)` from `Restitute` passes all 240 focused tests (`mutations/MX4-no-rolling-resolve.test.log`).

**Smallest resolving check:** either a native test whose expectation needs the bounce impulse in the rolling cone, or delete the rolling re-solve (the simplest complete design) and correct D5 and the doc.

### Non-blocking (record only)

- **MX5 survived** (approach from current ω instead of W0). It is equivalent in every tested scene, because gravity and drag do not change ω. Only angular guides or drives would distinguish it. Optional: a test with a driven angular velocity.
- **MX6 survived** (touch point without the half-gap offset). It is near-equivalent, because the point stays on the sphere's centre line either way. Low.
- **Moving sphere pair (the D9 exception).** For r = 0.1 spheres in an oblique elastic hit, the deflection error is 1.11° at 3 m/s (base 0.55°), 1.93° at 6 m/s (base 1.72°) and 3.44° at 12 m/s (base 3.44°). The candidate keeps KE at 1.000000; base loses about 40 % at 6 and 12 m/s.
  - This is acceptable for the game.
  - Request: the spec's "≈ 0.5°" is specific to the Basketball. State the v·h/(r_a + r_b) scaling instead.
- **Strict threshold (`<` versus Box2D `≤`).** It differs by one f32 step at exactly the threshold, with no game impact. It is pinned both ways (MX8 in the implementer's set).
- **Stacks, 5/18 → 6/18 still oscillating.** Bounded at ≤ 0.036 mm per tick and below the 0.1 m/s threshold, so restitution cannot fire. The cause is pre-existing soft contact and relax. Deferring it (`engine-resting-stack-settling`) is accepted, and it is not a regression that needs a fix here.
- **Spec staleness.** "Ordered tasks / Lesson identity check" still describes the superseded D9 draft (joined_pipe 3.41 m, 36°). The current state says 1.87 m. The "diff every authored-solution trace" task is unaddressed; coordinator decision 2 replaced it with solve-at-3-precisions, which passes. Please align the text.

## Criterion and impact matrix

| # | Criterion | Expected (derived) | Observed | Verdict |
|---|---|---|---|---|
| 1 | Same-substep bounce from the substep-start approach; no double or missed bounce | v_out = e·v at every phase, with no tunnelling | Phase sweep of 64 offsets across two substeps of travel at v ∈ {1, 4, 20, 60} m/s and e ∈ {0.5, 1}: ratio exactly 0.50000 / 1.00000, never tunnelled. Base gives the same in this frictionless head-on case | Pass |
| 2 | Block solve; fallback when singular or pulling; determinism; flat landing without spin | ω ≤ 10⁻³; bit-identical runs | Flat-drop tests green, bit-identical at 60/120/240 Hz plus Reset. Pull fallback covered via MX2; the singular branch is untested (F2) | Pass / F2 |
| 3 | Friction and rolling re-solve: no gain, within the Coulomb bound | Δv_t = μ(1+e)v/2 when sliding; no reversal | Strike and incline tests green and red on base. The rolling half is unpinned (F3) | Pass / F3 |
| 4 | Speculative touch normal | ≤ 2° reflection, \|v\| within 1 % | Green at 2–12 m/s (base fails at 6 and 12). Spheres only: the narrowphase is already shape-dispatched, and flat features have a pose-invariant normal. MX3 (horizon h) killed | Pass |
| 5 | Strict threshold | | No game impact | Pass (note) |
| 6 | Plate and events on carried impulse | The plate qualifies on the impact tick; lit through the bounce; 12-tick hold unchanged | The pin is red on base and green on the candidate. Law cases cover pressed plus alignment/footprint rejections. The ring ordinal lies within its tick. The C-Plate hold tests are green | Pass |
| 7 | Energy: e² ratio; per-tick ≤ m·g·slop, including e = 1 | | Static partners pass for all shapes and every e. Dynamic chains fail at e ≥ 0.95 (F1) | **Fail (F1)** |
| 8 | Lessons | 12 solve at 3 precisions; trace deltas explained | 12/12 green on re-run. ready_to_rebound: the rebound is 4.396 m/s on the candidate against 4.407 on base. The 0.011 m/s difference equals e·g·h = 0.55 × 9.81 / 480, which is D3 dropping this substep's gravity from the approach, and it amplifies in an unsolved layout. joined_pipe: matches the analytic Coulomb sliding reflection within 0.2 %, pinned in isolation | Pass |
| 9 | Corrected tests are not loosened | | D11 tolerances unchanged and green on both. EventRing, plate and lesson edits only add assertions. The Lively gate matches `lively-gate-source.txt` (only the summary line changed, `[Fact]` without Explicit) | Pass |
| 10 | Reviewer mutations | | MX1, MX2 and MX3 killed; MX4 survived (F3); MX5 and MX6 survived (equivalent or near-equivalent) | F3 |
| 11 | Docs consistency | | CAT-052, the narrowphase text and the threshold row are consistent. The energy-row and "never returns a tick" claims are false for dynamic chains (F1). Spec text is stale (note) | **Fail (F1)** |
| — | Build, full test, CI tools, Release, diff-check | | All green | Pass |

## Return

Return to the same implementation owner. Re-review covers:
- F1, by fix or by signed scope, plus the new dynamic-partner test;
- F2 and F3;
- the spec and doc text.

Then a fresh full slnx run on the frozen candidate. After that, Pass-pending-merge: merge main f5ffdb4e, then SnapshotApproval.

## Terminal publication verification — 24 October 2026

Raw outputs are in `evidence-engine-restitution-review/terminal/`. The system and GitHub timestamps read 23 Oct 2026 22:29–22:32Z (local 24 Oct).

**Identities**
- **Slice commit:** `adcffdb2e2be3b0ac7d1dd412c7e92e22c7b4b10`, tree `786e1735518ada7e9c2797b9f041de71a73f089c`, parent `2c668ecd`.
- **Published merge** (`--no-ff`): `0cba945b1d3765a4bb95bd2bbd56dfe58f497e43`, parents `f5ffdb4e` and `adcffdb2`, the same tree. `git diff adcffdb2 0cba945b` is empty.
- **Remote:** `origin/main` and `git ls-remote origin refs/heads/main` both return `0cba945b…`.

| # | Publication-dependent check | Result | Raw |
|---|---|---|---|
| 1 | The commit tree equals the approved snapshot | **Pass.** `adcffdb2^{tree}` is `786e1735`, the final staged tree reported with the SnapshotApproval. `git diff --binary 2c668ecd adcffdb2` sha256 is `02d33da5…98da46`, equal to the post-append staged-diff hash (40 files) | `identities.txt` |
| 2 | Hooks | **Pass.**<br>• **Implementer's commit log:** 3 gate builds, 0 warnings and 0 errors; 124/124; commit `adcffdb2` created.<br>• **My re-run** on the exact published tree: a throwaway worktree with HEAD `2c668ecd` and index `786e1735` (40 staged paths, 0 unstaged, 0 untracked), through `.git/hooks/pre-commit` → `.githooks/pre-commit`, 22:30:49Z–22:31:48Z. Result: 3 gate builds, 0 warnings and 0 errors; 124/124; hook rc 0. The worktree was removed | `commit-hook-implementer.log`, `hook-rerun.log` |
| 3 | The merge tree equals the branch tree, and remote main is the published merge | **Pass** | `identities.txt` |
| 4 | Pages workflow on the exact SHA | **Pass.**<br>• Run 40031774258, "Build and deploy game", push, main, headSha `0cba945b`, completed/success. Checkout fetched `0cba945b…`.<br>• Every build step succeeded: static gate (3 builds, 0 warnings, 0 errors), Test C# game, Test C# tools, publish, static-deployment check and Pages upload. The deploy job succeeded.<br>• **Totals:** game **2928**; tools **77 / 191 / 17 / 174**; Failed 0 and Skipped 0 everywhere. The job log never mentions `blame` | `run.json`, `build-job.log` |
| 5 | Pages artifact ownership and deployment | **Pass.**<br>• Artifact `github-pages` id 12536451244 (digest `sha256:790638bb…f58900`, 80,275,862 bytes, not expired) belongs to run 40031774258 at headSha `0cba945b` on main.<br>• The deploy job logged "Created deployment for 0cba945b…" and "Reported success!".<br>• Deployment 7290143602 (sha `0cba945b`, ref main) is the newest `github-pages` deployment; the previous one is `f5ffdb4e`. Its status went waiting → queued → in_progress → success (22:29:30Z), environment `https://aidanmorgan.github.io/tim/` | `artifact-deployment.txt`, `deploy-job.log` |
| 6 | Serving | **Pass.** `/`, `/index.html`, `/404.html`, `/godot.wasm`, `/godot.pck` and `/_framework/dotnet.js` each return 200 with `last-modified` 22:29:26 GMT, this deployment's own time (checked 22:32:16Z) | `deployment-status.txt` |
| 7 | Status | `engine-restitution-energy-correctness` → `done # scoped native/publication Pass0cba94; browser pending` | `sprint-status.yaml` |
| 8 | Browser cases | Pending post-catalogue E2E: the changed bounce trajectories (same-substep restitution, touch-normal reflections, the plate impact tick) | — |

**Terminal verdict: scoped native and publication Pass at `0cba945b1d37`.**
- Browser qualification is pending post-catalogue E2E.
- The deferred items remain open with their owners:
  - `engine-resting-stack-settling`;
  - "Engine: simultaneous/supported-impact restitution", to be checked before any dynamic plank, crate or seesaw element;
  - "Test-host SIGSEGV under concurrent load".
- This is not a release, global-audit or runtime qualification.
