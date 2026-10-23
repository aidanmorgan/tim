---
title: "Review — C-Gate (9A.19 + 9A.21), independent adversarial review"
candidate: "uncommitted worktree story-c-gate on base a598a09e; tracked diff + untracked sources sha256 97f1bea25afff1fefae24f3a6c54fe74f94e2f8ba717a24a08d67d97e18621a5 (unchanged at start and end of review)"
verdict: "Pass-pending-merge (implementation scope, native proof; browser qualification pending post-catalogue E2E)"
date: 2026-10-22
---

# Verdict

**Pass-pending-merge.** I found no blocking findings in the native, required-now scope. There are four non-blocking findings (F1–F4) for the coordinator.

- This is not SnapshotApproval and not publication approval.
- Before SnapshotApproval, the branch must merge main `8bfddc91` (9.8b XOR, the 9.4c receipts and others). The merged candidate then needs re-review of the merge-affected checks: the full suite on one matching build, the Known/ratchet totals, and the wire and schema parity if XOR touched them.
- Browser qualification is pending post-catalogue E2E. This scoped Pass is not a release or runtime qualification.
- **Expect 9A.19's "cat-051 passes" browser item to fail on the current product once its direction is corrected (F1).** cat-051 test 3 currently passes only because it treats the upstream ejection as "downstream". The coordinator should decide F1 before post-catalogue E2E. It does not block the native verdict.

Evidence is in `evidence-c-gate-review/`. Raw logs are cited by file name.

# Checks run (one matching Debug build; tests `--no-build --no-restore`)

| Check | Result | Receipt |
|---|---|---|
| `dotnet build CuriousContraptions.slnx -c Debug` | 0 warnings, 0 errors | `build.log` |
| Full `dotnet test CuriousContraptions.slnx` | 2820/2820 | `full-test.log` |
| CI "Test C# tools" step under bash, extracted verbatim from `pages.yml` | 77 / 159 / 17 / 174, rc 0 | `ci-tools.log` |
| Release worker: `dotnet publish CuriousContraptions.web -c Release -p:PlaytestDiagnostics=false`, run in a throwaway copy | rc 0, 0 warnings, all 5 static-deployment files present | `web-publish.log` |
| Strict tsc (pinned typescript 6.0.3, @types/node 26.0.1) | rc 0 | `tsc.log` |
| Snapshot schema unit test (tsx 4.23.15) | 24/24 | `ts-snapshot-unit.log` |
| `git diff --check` | clean | `diff-check.txt` |
| Wire regeneration with pinned flatc 25.2.10 (`f004709c…`) | base reproduces committed C# byte for byte; candidate C# and the generated TS part are identical to my regeneration | see Rulings, item 1 |
| Deletion greps (production scope, with a base positive control) | 0 hits for every pattern | `deletion-greps.txt` |
| Known rows and ratchet | see criterion matrix | `known-and-ratchet.diff` |
| Gate trajectories: 13 fixtures, base vs candidate, Release | 13/13 bit-identical; also identical to the implementer's `capture-final` | `gate-traces.tgz` |
| Lesson and route traces (the 12 playable lesson benches + 12 launch routes, fresh device each). The other 50 `content/puzzles.json` entries are planned content: `PuzzleCatalogue.Definition` rejects them as "Unknown or unavailable puzzle" | see Rulings, item 2 | `lessons/` |
| Per-phase timing, n=5 fresh processes per variant | see Rulings, item 3 | `timing/` |
| My 15 mutants | 14 killed, 1 survived (F4) | `mutation/summary.txt` |

# Criterion and impact matrix

| Criterion | Evidence | Result |
|---|---|---|
| 9A.19: the gate package declares 1.55 m, sign(d)·min(2.8, √(2·14·\|d\|)), 60 N, 120 W, spring 14 / damper 4 | Package diff; generated bundle carries the drive; `TheGatePackageDeclares…` parity test | Pass |
| Generic law replaces `ActuatorSliderDrive` (axial row, effort clamp then power cap, stopping profile) | Code read; mutants R3b (power cap off), R9b (weight term dropped), R10b (holding term dropped), R8 (target range) all killed | Pass |
| 120 W enforcement is new; unloaded peak 107.5 W; trajectories unchanged | 13/13 bit-identical (my reproduction) | Pass |
| Routes and the D3 one-tick lag unchanged; 9R.3 own-supply and two-battery checks | Full suite; golden supply and prismatic rows inside the 13 identical traces | Pass |
| Negative control: a box with the gate's dimensions and no drive never actuates | `ASuppliedBoxWithTheGatesBladeAndLimits…`; R7b (undriven consumer admitted) killed | Pass |
| Validator rejects: no PowerIn, two driven sliders, non-finite or negative values, target outside the limits, speed above 64 | `BundleValidatorTests` (11 cases); R6 killed | Pass |
| Return spring is the fail-closed law (scheduling item h) | Unsupplied drive applies no force; spring return asserted in `LatchSupplyFixtureTests` | Pass |
| Blocked (Q3): resting on the ball → Blocked; free travel, spring return, apex, both stops, closed-face contact, inverted and 135° mounts → not Blocked | LatchSupply and DeclaredPrismaticDrive tests; R2, R11 and R5 killed | Pass (see F4) |
| Indicator ochre through the wire | `TheGateCueIsOchreWhileABallHoldsTheBladeAtRest` | Pass |
| 9A.21: a synthetic, declaration-free scene (sphere, static box, prismatic); no element id or value | `WarmupScene`; deletion greps; `TheWarmupSceneCarriesNoDeclared…` | Pass |
| 9A.21: time to first tick not regressed beyond noise (advisory) | 0.20 → 1.42 ms native | **Not met (advisory)**; see item 3 and F2 |
| Deletions: `ActuatorSliderDrive`, `SwitchedSupplyClosed`, `GateBlade`, `fSpring`, `PartId.PoweredGate`, the element warm-up, the dummy joint | 0 production hits; the base control finds each | Pass |
| Known list only shrinks | Known: exactly 9 rows removed, 0 added. Generic contexts: 1 row removed (`WorkshopPhysicsEngine.Warmup` FromSpring), 1 added (`PrismaticConstraintSolver.ApplySuppliedDrives` Spring, SoftSpringLaw), so the total stays the same. That is a generic classification row, not a Known violation | Pass |
| Solver ratchet empty | `KnownViolations = []`, both maximums 0, SHA-256 of the empty list | Pass |
| D6: a resting ball passes when the gate is enabled during Run | Native D6 test, both cases. The base trace (`probe-m1-de`) is identical | Pass; the reinterpretation is confirmed (F1) |
| Lifecycle: Run/Reset and save/load | Full suite (gate fixtures, `WorkshopSaveRejectionTests`) | Pass |
| Closed sets are enums; no fallback or compat path | `PrismaticDriveKind` enum validated by `Enum.IsDefined`. A None record must have all-zero drive bytes. No alias or shim | Pass |

# Rulings on the implementer's three items

## 1. Wire change (`ActuatorDriveRecord.moving_contact`): necessary and correct

**Necessity.** No committed read carries body-body contact for two dynamic bodies:
- contact events fire only for dynamic-versus-static pairs;
- manifolds and the warm-start cache are engine-internal;
- stop-row impulses describe the joint's own limits.

An inverted mount resting at 0.70 m is kinematically identical to a blade resting on a ball. Coordinator decision 2 therefore requires a contact signal.

**Form.** It is a typed u32 lane in the world record (values 0 or 1, rejected otherwise, zero at admission) and a typed bool in the struct. The struct stays 40 bytes; the field occupies the former padding at byte 17. It is not a fallback.

**Regeneration.**
- The pinned flatc reproduces base byte for byte, and my regeneration equals the candidate's C# and generated TS.
- `SNAPSHOT_SCHEMA` was updated and its test passes 24/24.
- `SavedConstruction` and the save codec are unchanged.

**Contact lane accumulation.** The lane accumulates across substeps, as documented. The flag resets at load.

## 2. Static-body velocity fix: root cause confirmed, general, discriminating

**Root cause.**
- `ConstraintVelocity` and `PointVelocity` read velocities with no motion guard.
- Impulse and mass paths are guarded by `Motion == 1`.
- A pooled `SimBody` reused as a static body kept its old velocities.

**Generality.**
- The zeroing sits in the only static-load branch, which every `Advance` runs. That covers user worlds, Reset re-admission and the warm-up.
- The other stale pooled fields (InvMass, InvI, Q0/Cm0, SupportExtent) are all read behind motion guards.
- `Reset()` clears `_contactCache`, the BVH, pairs, contacts and manifolds. No other cross-world leak found.

**Discriminating.** Reverting the fix (R1) fails `AWarmedEngineStepsAWorldExactlyLikeAFreshOne` and `SliderInstanceTests.AMissedBallGetsNothingFromAChargedBoard`.

**Published behaviour does change, as an intended bug fix.**
- 10 of 24 lesson and route traces differ from base. Four move by 0.14–0.31 m: `ready_to_rebound` and both Ready routes from tick 1, and SpringZero from tick 188. The other six move by at most 6 mm.
- Every solve and unsolved outcome is unchanged.
- Base with only the one-line fix applied equals the candidate on all 24 traces. So every difference is the fix, and none comes from the drive law or the warm-up.
- On base, worker behaviour depended on run history.
- The gate fixtures are untouched.
- Chrome proofs of these lessons recorded on base are stale in trajectory detail. They are queued for post-catalogue E2E.

## 3. Warm-up timing: advisory AC not met; non-blocking; coordinator to accept or re-target

Per-phase native Release timing (median of 5, ms):

| Variant | Construct | Run | First Advance |
|---|---|---|---|
| Base, element warm-up | 6.3 | 0 | 0.20 |
| Base, empty warm-up | 26.2 | 0 | 1.40 |
| Candidate (empty) | 23.7 | 0 | 1.42 |
| Candidate engine + element-scene warm-up (diagnostic) | 5.4 | 0 | 0.20 |
| Candidate + `WarmupScene` driven through the generic ABI reads (diagnostic) | — | — | 1.16 |

**Cause.** The whole regression comes from replacing the Program-level element scene with the empty bench. `WarmupScene` is not the cause, and the engine-level warm-up got richer (prepare 28 → 44 ms, off the critical path).

**Where the time went.**
- The cold cost moved into the first Construct (+18 ms, which happens at a Build edit, not at Run) and the first tick (+1.2 ms).
- Startup-to-first-tick in total is about the same.
- Warming the generic read path recovers only about 0.25 ms. The rest is per-element compile and activation first use, which 9A.21's own "no element id" clause forbids warming.

**Ruling.** No cheap fix exists within the AC. 1.4 ms is well under one frame, and the production worker is AOT, so the JIT proxy overstates the cost. This is not blocking under the AC's own "advisory; Epic 16" wording and the playable-first policy. The wording is **not met** on the native proxy, and the spec records it as such. The Chrome/WASM first-Run measurement decides.

# Findings (none blocking)

**F1. D6 direction: the reinterpretation is correct, but the real ejection is reachable and the pending suite pins the wrong direction.** Owner: coordinator; then the C-Gate implementation owner or the post-catalogue E2E owner.
- **Physics.** Gravity along the bore axis is +2.54 m/s² toward local +x, so downstream is +x. The decoded base and candidate traces agree: held at −0.40 along the axis, then +2.55.
- **The real ejection.** It is the enabled-from-start case. The ball starts 0.3 m inside the closed blade and is thrown out of the entry mouth to world x −2.75, where it falls.
- **Build admits that placement.** `tools/e2e/cat-051.test.ts` builds it through the UI (ball at −0.08). Its test 3 ("powered run", battery enabled from the start) asserts `px < -0.6 && vx < -0.2` as "downstream". That pins the ejection as success, and the 9R.3 review table has the same inverted labels.
- **Risk.** The canonical cat-051 bench shows a player-visible upstream ejection, and the pending browser suite would pass it.
- **Not blocking because** the 9A.19 AC's resting, enabled-during-Run case is met natively, and the behaviour predates this slice (bit-identical to base).
- **Smallest resolving check.** The coordinator decides whether the deep-overlap launch is fixed in C-Gate (contact push-out or Build overlap) or filed. Then re-pin cat-051 test 3 and the 9A.19 Chrome check to local +x (ends beyond +0.49, never below the held position − 0.02).

**F2. 9A.21 timing wording not met on the native proxy (+1.2 ms first tick, +18 ms first Construct).** Owner: coordinator (acceptance) and Epic 16.
- Record it as advisory-not-met.
- Measure the first Run and the first Construct in Chrome/WASM at post-catalogue E2E.

**F3. Lesson trajectories changed by the engine fix.** Owner: C-Gate implementation owner (record) and post-catalogue E2E.
- Add a line to the spec and the 8.2 addendum: "lesson trajectories changed by the static-velocity fix; outcomes unchanged; base browser proofs stale".

**F4. Mutation survivor R4: the moving-contact lane counted only on the last substep.** Owner: C-Gate implementation owner (optional).
- No test distinguishes "any substep" from "last substep". A persistent resting contact makes them equivalent for the Blocked use.
- This is a coverage note only.

# Process notes

- **Implementer gaps.** The `python3` heredoc edit of `SpringboardElementDeclaration.cs` and the after-the-fact Anvil run are disclosed in the spec. The content is correct (`Drive: null`). Non-blocking. The pre-commit hook applies at commit.
- **Reviewer disclosure.** I edited only throwaway files: the probe and test registrations in two scratch worktrees, using `sed` and `perl`, and mutants applied with `perl`. I also created files by shell: `mutate2.sh` through a `cat >>` heredoc, and an `echo >>` append of the closing re-hash line to `evidence-c-gate-review/candidate-identity.txt`. Both worktrees were removed after the review. I made no change to candidate source, tests, tools or content. The candidate hash is unchanged.
- **Pre-existing doc inaccuracy.** The `schemas/README.md` C# recipe's `printf … ''` emits one extra blank line, so its "byte for byte" claim is false. The implementer worked around it. Fix it in the next wire-touching slice.
