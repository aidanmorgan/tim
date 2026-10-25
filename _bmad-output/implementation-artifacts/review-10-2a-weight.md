# Review — Story 10.2a Weight (CAT-067)

Independent adversarial review, 24 October 2026. Reviewer did not implement the slice and changed no source, test, tool or content file.

**Candidate.** Worktree `story-10-2a`, HEAD `e52c5473c6ed` (tree `d3f919d978a6`), plus an uncommitted delta: one spec line, and the untracked `evidence-10-2a/merged-dc0356de/`.

**History.** Base `0cba945b` → WIP `59b37080` → merge `f03ee5bc` (main `8411ba3b`) → `d4036e78` (restitution receipt and behavioural conversions) → merge `e52c5473` (main `dc0356de`).

**Evidence.** All in `evidence-10-2a-review/`, run with .NET SDK 10.0.402.

## Terminal verdict: **Fail** (one blocking finding, R1). Return to the implementation owner.

SnapshotApproval was not performed, and the origin/main check was not needed.

## Findings

### R1 — Blocking (Medium): a lifted Weight is admitted inside a static sphere

The cause is the new cylinder arm in `PlacementPose`.

- `Pose` sets `Radius = 0` for every non-sphere shape.
- In `Overlaps`, the `box.Kind == Shape.Sphere` branch only redirects `Shape.Box` to the closest-point path.
- A cylinder therefore falls through to the sphere–sphere test with radius 0, so it is tested as a point.
- The new doc comment says the opposite: "against a sphere it is bounded by its (r, h, r) box".

**Reachable in the shipped catalogue.** The Bumper is a static sphere with r = 0.65.

**Reproduction** (`probes/ReviewerPlacementProbe.cs.txt`, `probes/probes-on-candidate-e52c5473.log`):
- **Setup:** a Weight dropped upright into a bench-standing plate, with a Bumper whose centre is 0.9 m from the Weight's axis at its mid-height.
- **Weight:** `Refusal None`. It is admitted with a ≈ 0.10 m interpenetration: the cylinder reaches 0.352 m from the axis and the sphere reaches 0.25 m.
- **Control:** a Basketball at the same spot is refused with `IntoPart`.

This violates the B1 contract that a lifted pose is clear of every Static part. No committed test covers a cylinder against a sphere.

**Required:**
- Fix the overlap: route a cylinder through the closest-point path against its (r, h, r) box, as the doc states, or use an exact cylinder test.
- Add a native `PlacementPoseTests` case: a Weight beside a Bumper is refused `IntoPart`, and a clear-gap control is admitted.

Then re-review the affected placement tests and run a fresh full slnx.

### Non-blocking findings

| # | Severity | Finding |
| --- | --- | --- |
| L1 | Low | **F3 bench-volume asymmetry.** The extent rule applies only to lifted poses. A grid (unlifted) placement still uses the point rule `Inside`, so a ball rested near x = ±7 is refused where an unlifted one is admitted. This is stricter, not looser, and consistent with the coordinator decision. Note it for the post-catalogue browser case. |
| L2 | Low | **Stack test drift bound.** The stack test's late-motion check is per tick (≤ 0.05 mm per tick), which would allow about 6 mm/s of steady drift. It satisfies D8 as worded. A cumulative last-second bound, like the roll test's post-stop check, would be tighter and could be added with the R1 fix. |
| L3 | Info | **Spec count.** The spec says the CI step deselects 13 E2eRunner tests. The workflow lists 15 names, and 190 − 174 = 16 test cases are deselected. |
| L4 | Info, pre-existing on main | **CI filter listing.** The CI "Test C# tools" filter is the browser-toolchain exception (AGENTS.md, `1e9769a4`). It is listed in one place (`pages.yml`, with the reason and the post-catalogue tier) but names no owner. Owner: the coordinator, on main. 10.2a does not change `.github/` (`git diff 0cba945b HEAD -- .github` is empty), so this does not block the slice. |

## Criterion and impact matrix

| Criterion / impact | Expected (derived) | Actual | Result |
| --- | --- | --- | --- |
| `MechanicalPayload.MassParameter` is generic (no Weight branch) | Selected by interface type; the compiler evaluates it for any entry; no `weight` id in the engine or solver | `BundleSchema` uses `is IParameterizedMassElement`. The compiler change is a single generic line (`bound.Evaluate(parameters)`). `rg` finds no `weight` in production `engine`, `Simulation` or `Bundle` code outside `elements/Weight`; the solver scan over `CuriousContraptions.Simulation` and `engine/gpu` has 0 hits. The compiler is the only production `MassProperties.Mass` consumer. | Pass |
| Validator rules: Parameter source; one Real kg parameter; Dynamic owner; range in [1/1024, 1024]; fixed mass bit-equal to the default; cylinder X/Z bindings equal | Each rule is refused by its own case | `BundleValidatorTests.AMassBinding…` has 11 refusal cases (including one ulp off the default and a duplicate key), a whole-domain boundary that is admitted, and 5 X ≠ Z cases with a box control. Reviewer mutation MX1 (Dynamic check removed) turns the "static body" case red. The implementer's M4 and M6 cover the one-truth and X = Z rules. | Pass |
| Mass 0.25–8 kg: literal boundaries admitted; refusals | 0.25 and 8 admitted at every seam; 0.249, 8.01, 0, −1, NaN, ±∞, missing and unknown keys refused at the editor, admission, save and wire seams, with no change and no Undo | `WeightTests` boundaries (2), refusals (7), missing/unknown (1); `PalettePlacementTests.TheWeightMassEditor…` (clamp with one Undo each; NaN, ±∞ and text refused; the handler seam is atomic) | Pass |
| Size scales with mass; growth from a base origin | r, h and offset follow D2; strictly increasing; OffsetY = h | Compile theories at 0.25, 4 and 8 kg; the size law equals the legacy cube root at its ends; the 4 kg binding equals the scene collider bit for bit; strictly increasing over 0.25 < 1 < 4 < 8. The mass-edit test shows the base stays on the bench within 0.5 mm with no tick above slop. Implementer M3, M7 and M8 are red. | Pass |
| Falls, rolls and rests end-down within the envelope | ≤ 0.5 mm, rest tolerances, no drift, no energy gain above m·g·slop | The rest test at 3 masses; the flush-ramp roll test (ω = v/r within 2%, Crr stop, axial ≤ 0.5 mm, post-stop ≤ 0.05 mm) | Pass |
| Two Weights stack (D8 per contact, F2) | Each contact within 0.5 mm; no bounce; late motion ≤ 0.05 mm per tick | 4 on 4 and 1 on 8 pass. The per-contact wording follows the coordinator's F2 decision (see L2). | Pass |
| Topples a Domino, with control | Tilt > 45° when struck; a Weight resting 0.1 m away leaves the Domino upright (< 1°) for 5 s | The Domino test passes | Pass |
| Reset and save/load are behavioural | Poses within 1 µm and 1 µrad and at rest after Reset; replay within 0.05 mm; decoded values round-trip | `d4036e78` replaced `AssertSameState` and the byte equality of saves with derived-tolerance assertions. The `+` lines of the slice's C# diff contain no hash, digest or byte-equality assertion. | Pass |
| F3: generic bench-volume fix | Uses world extent with no part identity; the box arithmetic is unchanged | `Within(Posed[])` is generic. `Extent` for a box is the same sum of terms in the same order (bit-identical). `TheBenchVolumeBoundsTheRestedBodyNotItsOrigin` covers the Weight, a ball and a box. Implementer M12 and reviewer MX3 (lower bound back to 0) turn it red, along with 5 other placement tests. | Pass |
| The 8 m → 7.5 m control change | Must tighten, not loosen | Under the extent rule, the old 8 m control would rightly refuse a Domino whose top is at 9.18 m. The 8.9 m refusal row is unchanged, and the lower control height keeps the whole body under 9 m. Justified. | Pass |
| F1 is pre-existing and correctly deferred | A plain engine cylinder on base `0cba945b` creeps after a drop landing; flush does not | Reviewer probe on a throwaway worktree at `0cba945b` (LawScene cylinder, no package): the drop ramp drifts **0.212 mm** along the axis after stopping (final speed 1.09e-4 m/s, persistent, z wandering 6.26 mm). The flush ramp drifts **0.009 mm**. The candidate gives identical numbers, so the slice does not change the engine law. Deferring to the "Engine: lying cylinder axial creep" slice (`deferred-work.md`, TODO) is justified, and no red or skipped test is committed. | Pass |
| Solver-scan rename `weight` → `gravityLoad` | Behaviour-neutral; scan rows added; Known list and hash unchanged | One local renamed (`PrismaticConstraintSolver.cs:278`). Scan rows `weightRow` = true and `gravityLoad` = false. Implementer M5 is red. | Pass |
| Membership pins | Each pin moves by +1 for `weight` | slnx, tests csproj, `ImpactRules`, Coverage 29 plus the path, registry 29 / ids 31, `CSharpStaticGateTests`, parity (exclusion from the 9A.16 baseline plus the cylinder lanes), `BundlePortsTests`, `WorkbenchCapacityTests`. The full suite is green. | Pass |
| PlacementPose cylinder rule | Exact projection; safe against every static shape | Against a box: the SAT uses the exact cylinder projection. A subset of axes can only over-refuse, and reviewer MX2 (radius term dropped) turns the lying-Weight test red. **Against a sphere: R1.** | **Fail** |
| No hash or byte assertions | None added | None (see above) | Pass |
| Skipped 0, no exclusion | Every result reports Skipped 0 | Full slnx: 0 skipped. Nothing new is filtered. | Pass |
| One element; package is declaration, art and bindings; f32; no element-specific solver; 9A.45a no new entry | — | Only the `elements/Weight` package. The material and size come from data. The scan and isolation tests are green. | Pass |
| Carried restitution receipt is byte-intact | Matches `carry/restitution-terminal-receipt.patch` and `…-evidence` | `review-engine-restitution.md` blob `7e4477ac` equals the patch post-image index. The patch reverse-applies cleanly to the worktree, including the sprint-status hunk. `diff -r` of `evidence-engine-restitution-review/terminal` against the carry evidence shows no difference. | Pass |

## Raw commands and results

| Command | Result | Raw |
| --- | --- | --- |
| `dotnet build CuriousContraptions.slnx -c Debug` | 0 W / 0 E | `build-debug.log`, `head.txt` |
| `dotnet test CuriousContraptions.slnx -c Debug --no-build --no-restore` (no blame-crash) | native 2962/2962; BundleElements 194; E2eRunner 190; Coverage 77; WorkerAotGuard 17; **Skipped 0** in every row; 0 "duplicate" lines | `test-slnx.log` |
| CI "Test C# tools" step under bash (body identical to the HEAD `pages.yml`) | Coverage 77, BundleElements 194, AOT 17, E2eRunner 174; exit 0 | `ci-tool-step.log` |
| Release `CuriousContraptions.Simulation` and `CuriousContraptions.Animation.Worker` builds | 0 W / 0 E each | `worker-release.log`, `animation-worker-release.log` |
| `git diff --check` | The uncommitted delta is clean. Slice-wide findings are only trailing whitespace in raw logs, plus two carried restitution evidence files (`deployment-status.txt`, `identities.txt`). Those two are intentionally unfixed, to keep them byte-identical to the carry. | `diff-check.log` |
| Reviewer F1 probe at `0cba945b` and at the candidate | drop 0.212 mm, flush 0.009 mm post-stop (identical at both) | `probes/f1-creep-on-base-0cba945b.log`, `probes/probes-on-candidate-e52c5473.log` |
| Reviewer placement probe (R1) | Weight `None` with a 0.10 m overlap; Basketball `IntoPart` | `probes/probes-on-candidate-e52c5473.log` |

## Reviewer mutations

Each mutation ran on a throwaway worktree of `e52c5473`, which has since been removed. Each turned at least one test red. Round 1 combined three mutants in one build; each red is attributed below to the mutant it covers.

| Mutant | Red test(s) | Raw |
| --- | --- | --- |
| MX1 validator Dynamic-owner check removed | `BundleValidatorTests.AMassBinding…` ("static body") | `mutations/mut-r1*.txt` |
| MX2 cylinder `Extent` radius term dropped | `PlacementPoseTests.ADroppedWeightRests…` | `mutations/mut-r1*.txt` |
| MX4 Weight restitution 0.08 → 0.3 | `WeightTests.TheCompiled…` ×3 | `mutations/mut-r1*.txt` |
| MX3 bench-volume lower bound reset to 0 (F3 partly undone) | `TheBenchVolumeBoundsTheRestedBodyNotItsOrigin`, plus 5 plate-rest and Domino-plate tests | `mutations/mut-r2*.txt` |
| MX5 `PickRadius` 0.7 → 0.6 | `WeightTests.TheWeightIsAMassBound…` | `mutations/mut-r2*.txt` |

## Browser and publication

Browser qualification remains **pending** until post-catalogue E2E. `cat-067` includes the engine-cylinder dynamic case from 10.1, and a Weight placed beside a Bumper should be added to it.

Publication is not reached. After the R1 fix, the same implementation owner returns the candidate for independent re-review of the affected placement closure and a fresh full slnx. SnapshotApproval follows only after that.

# Re-review, round 2: fixes for R1 and coordinator additions (a)–(c), 24 October 2026

**Candidate.** HEAD `d29cb06e` = fix `710a084e` merged with main `54e19147` (which changes only AGENTS.md), plus the uncommitted gate logs in `evidence-10-2a/r1-merged-54e19147/`. The tree identity is in `evidence-10-2a-review/r1/head.txt`.

## Round-2 verdict: **Incomplete** (one blocking gap, R2). SnapshotApproval not performed.

origin/main has also moved to `7143a874`. That merge (`87d27e12`) changes CI and the test-deferral list, so a merge and a gate rerun are needed before SnapshotApproval in any case.

### R1: resolved (Pass)

**The fix.** In `Overlaps`, a static sphere now hands any non-sphere to the closest-point path. `ClosestOnCylinder` clamps the axial coordinate to ±h and the radial part to r; this is the exact solid-cylinder closest point. The doc comment now matches the code.

**Probe** (`r1/probes/placement-overlap-probe-d29cb06e.log`): the Bumper reproducer and every exact-geometry case agree with the analytic answer.

| Case | Result | Exact answer |
| --- | --- | --- |
| Bumper at 0.9 m | Weight and ball both refused, IntoPart | IntoPart |
| Bumper at 1.0 m | Weight IntoPart; ball None | IntoPart (0.0017 m) for the Weight; None for the ball |
| Bumper at 1.05 m | both None | None |

The same probe also matches the exact answer for:
- a Weight facing a box face, with gaps −0.027, −0.007, +0.013 and +0.043 m;
- a lying Weight beside a box end;
- cylinder against cylinder: side by side, crossed on top, and rim to rim. The result is the same when the two arguments are swapped.

**Committed test.** `ALiftedWeightBesideABumperIsRefusedAndAClearGapIsAdmittedLikeABall`. Reviewer mutation MX6 (the radial clamp disabled) turns it red, in addition to the implementer's M13.

### R2: blocking (Incomplete). Addition (a) is untested on the snap path

Addition (a) claims the extent rule applies to "every placement path (snap, grid and rest)". Reviewer mutation MX7 makes the snap path return `None` instead of calling `Bounded`. It **survives** every `PlacementPoseTests`, `PalettePlacementTests` and `TubeMouth*` test (103 passed; `r1/mutations/mx6-mx7.*`).

**Required:** a native test in which a tube-mouth snap puts the body across the bench edge and is refused `OutsideBench`, plus a control that is admitted. The only moving input here is the claimed snap behaviour, so a re-review of that test and its mutation is enough, followed by the gate on the merged candidate.

### Addition (a): grid bounding, and the Wall test edit

**The rule is generic.** It uses each part's declared colliders at default parameters. That is correct, because `Compose` is only called for new parts (`WorkshopGuidance.PlacementPose`, `_nextId`); moving a part uses `MoveSnap` only.

**The Wall edit in `EveryOtherPlacementKeepsTheBasePose` is a correction, not a loosening:**
- Centred on the bench plate, the old 2 m Wall reached 0.78 m below the bench surface, so under the new rule it is correctly refused.
- The moved case still overlaps the plate face, so it still exercises "a Static part keeps its base pose".
- Nothing about the refusal was weakened. A static part sunk into the bench is refused by the same generic `Within` that `AGridPlacementIsBoundedByTheBodyNotItsPoint` tests, but only for dynamic parts (L5).

**Regression scope (deliberate rule change, not an accident).** `r1/probes/grid-admitted-range-per-part-d29cb06e.log` sweeps every registered part on an empty bench at its default rotation. The old point rule admitted every point in the work-point volume; the new rule admits less:

| Part | Lowest admitted grid y | Range at y = 4 |
| --- | --- | --- |
| Funnel | 1.2 m | — |
| Pipe bend 90 | 1.4 m | — |
| Wall | 0.6 m | x ∈ [−5.5, 5.5] |
| Pipe | — | x ∈ [−5.1, 5.1] |

Every Playable lesson's solution position stays admissible at default parameters, though the Bumper at z = 3.3 has only 0.05 m to spare. The 12 Playable lessons and the full native suite are green.

Browser recipes that click near the volume's edges (for example `cat-015b`'s Wall) must be re-checked in the post-catalogue E2E phase. Record them as accumulated browser cases.

### New non-blocking findings

| # | Severity | Finding |
| --- | --- | --- |
| L5 | Low | **Wrong message for grid refusals.** `WorkshopBenchPlacement.Refuse` still says "resting there would lift the part above the bench" for every `OutsideBench`, but grid and snap refusals (for example a Wall clicked at y = 0.3, or a part crossing x = ±7) are not rests. The `BenchPlacementRejection` doc ("a rested part outside the bench volume") is stale too. No test covers a static part sunk into the bench being refused. |
| L6 | Low (over-refusal) | **Conservative cylinder test near corners.** Cylinder–box and cylinder–cylinder use a 15-axis SAT without the cylinder's rim axes, so it can refuse placements that are actually clear, but it never admits an overlap. Measured: a Weight diagonally 0.018–0.098 m clear of a Wall corner is refused, as is an upright cylinder 0.10 m clear on the diagonal. Only cylinder–box cases reach the catalogue today; static cylinders arrive with 10.2c. |
| L1 | — | Superseded by addition (a). |
| L2 | — | Resolved by (b): the cumulative path bound is ≤ 0.5 mm in the last second. |
| L3 | — | Resolved by (c). |

### Gates on `d29cb06e` (one rebuild)

| Command | Result | Raw |
| --- | --- | --- |
| `dotnet build CuriousContraptions.slnx -c Debug` | 0 W / 0 E | `r1/build-debug.log` |
| `dotnet test CuriousContraptions.slnx -c Debug --no-build --no-restore` | native **2964/2964**, BundleElements 194, E2eRunner 190, Coverage 77, WorkerAotGuard 17; **Skipped 0**; 0 duplicates | `r1/test-slnx.log` |
| CI "Test C# tools" step (body identical to HEAD `pages.yml`) | 77 / 194 / 17 / E2eRunner 174; exit 0 | `r1/ci-tool-step.log` |
| Release Simulation / Animation.Worker | 0 W / 0 E each | `r1/worker-release.log`, `r1/animation-worker-release.log` |

**Other checks:**
- **No hashes or skips:** the `+` lines of the `e52c5473..d29cb06e` C# diff add no hash, digest or byte-identity assertion, and no `Skip` or `Explicit`.
- **Throwaway worktree:** `wt-r1` has been removed.

**Next:** the same implementation owner adds the R2 test and merges origin/main `7143a874`. Then the reviewer re-checks R2 (test plus MX7 red) and reruns the gate on the merged tree. SnapshotApproval follows only after that.

# Re-review, round 3: fixes for R2 and L5, 24 October 2026

**Candidate.** HEAD `6387b5892aac` (tree `722b201ef4a5`). It is fix `16b13bb0` merged with origin/main `f9ce61ef`, which brought the testing guidance and the CI no-skip deferral list. The merge touches no `engine/`, `elements/`, `ui/`, Bundle or `CuriousContraptions.tests` file.

Uncommitted: `evidence-10-2a/ci-tool-step-f9ce61ef.sh` and `evidence-10-2a/r2-merged-f9ce61ef/`. Evidence is in `evidence-10-2a-review/r2/`.

## Round-3 verdict: **Pass-pending-main-green** (scoped native)

There are no blocking findings, and every test I re-checked or added complies with `docs/testing-guidance.md`.

SnapshotApproval waits until both of these have happened:
1. Main's CI is green again. The red node harnesses on Linux at `7143a874`+ belong to a separate fix slice, not 10.2a.
2. The branch has re-merged origin/main `e344b19c`, which adds the TypeScript rule to `AGENTS.md` and nothing else: `git diff --stat f9ce61ef e344b19c` shows only `AGENTS.md`. 10.2a adds no TS, JS or runtime tool source. Its only scripts are bash copies of CI-step bodies kept as evidence.

Publication must not happen before then.

### R2: resolved

New test: `ATubeMouthSnapAcrossTheBenchEdgeIsRefusedAndOneInsideIsAdmitted`.
- **Positive case:** a new 3.6 m Pipe joined to a Pipe at x = 3 would reach about 8.6 m. It is refused with `SnapOutsideBench`.
- **Control:** the same join at x = −3 reaches about 2.6 m and is admitted. It differs from the positive case only in the placed Pipe's x.
- **Expected pose:** derived from the declared mouth (`2 × mouth.X`), not copied from output.
- **Mutations:** the composed pose is the snap's own. Reviewer mutation MX7 (snap returns `None`) is now **red**, and so is the implementer's M15.

Guidance check:

| Checklist item | Result |
| --- | --- |
| Defined interface | `PlacementPose` |
| Derived expectation | Yes |
| Positive and control cases | Yes |
| Mutation-proven | Yes |
| Deterministic | Yes |
| No hash, no skip | Yes |

The bench-volume boundary itself is the shared `Within` rule. Its literal-edge cases are in the grid test (6.9 / 6.6 m) and the rest tests.

### L5: resolved. The expectation update is a correction

`PlacementRefusal` is now typed per path: `OutsideBench` for grid, plus `SnapOutsideBench`, `RestOutsideBench` and `IntoPart`. The new values are appended after the existing ones, and nothing serialises the enum.

**Message text.** Each reason has its own text. `EachPlacementRefusalHasItsOwnStatusText` checks them through `WorkshopBenchPlacement.Refuse` (the UI-logic seam), along with:
- the unchanged construction;
- the `UnplaceablePose` kind;
- that `None` throws.

**The `PalettePlacementTests` expectation change.** "resting there would lift the part above the bench" becomes "…outside the bench" for the same rest refusal. It is a correction:
- A rest refusal can now come from x/z as well as from height, so "above" was inaccurate.
- The test still asserts the exact string and the unchanged construction, commands and Undo.
- It is the same rest-path case: under reviewer mutation MX8 (rest returns the grid reason), the 8.9 m palette case goes red, along with three `ARestedPoseAboveTheBenchIsRefused` cases and `TheBenchVolumeBoundsTheRestedBodyNotItsOrigin`.

**The `BenchPlacementRejection` doc** now describes the grid, snap and rest paths.

### Gates on `6387b589` (one rebuild)

| Command | Result | Raw |
| --- | --- | --- |
| `dotnet build CuriousContraptions.slnx -c Debug` | 0 W / 0 E | `r2/build-debug.log`, `r2/head.txt` |
| `dotnet test CuriousContraptions.slnx -c Debug --no-build --no-restore` | native **2966/2966**, BundleElements 194, E2eRunner **207**, Coverage 77, WorkerAotGuard 17; **Skipped 0**; 0 duplicates. Node harnesses run inside E2eRunner.Tests (`NodeHarnessTests`) and passed locally. | `r2/test-slnx.log` |
| CI "Test C# tools" step (body identical to the HEAD `pages.yml`; the filter is generated from `BrowserDeferral.cs`) | 77 / 194 / 17 / E2eRunner 191 (16 deferred browser-toolchain cases, explicitly listed); exit 0 | `r2/ci-tool-step.log` |
| Release Simulation / Animation.Worker | 0 W / 0 E each | `r2/worker-release.log`, `r2/animation-worker-release.log` |
| `git diff --check HEAD` | clean | — |

**Reviewer mutations.** MX7 (snap unbounded) and MX8 (rest gives the grid reason) were applied together on a throwaway worktree of `6387b589`, now removed. Six tests went red; each mutant has its own reds, attributed above (`r2/mutations/mx7-mx8.*`).

**Other checks:**
- The `+` lines of the `d29cb06e..16b13bb0` C# diff add no hash, digest, byte-identity, `Skip` or `Explicit`.
- A bare `node --test` run without `--experimental-vm-modules` is not the supported invocation; it failed on `vm.SyntheticModule`. That log was discarded, and the supported run is the slnx row above.

**Still open, non-blocking:**
- L6: conservative cylinder–box overlap near corners (over-refusal only).
- Browser cases accumulated for the post-catalogue E2E phase: `cat-067`, the Weight beside a Bumper, edge placements under the grid rule, and `cat-015b`'s Wall click.

# Round 4: merge of green main f1ad3bd1, and SnapshotApproval (24 October 2026)

**Candidate.** HEAD `56f3e327` = merge of slice `6387b589` (first parent) and origin/main `f1ad3bd1` (second parent). The tree identity is in `evidence-10-2a-review/r3/head.txt`.

## Verdict: **Pre-publication Pass** (scoped native). SnapshotApproval is below.

### Merge integrity

`git merge-tree --write-tree 6387b589 f1ad3bd1` gives auto tree `86029d46`. HEAD differs from that tree only in:
- **`TODO.md`.** Main's version was kept: `git diff f1ad3bd1 HEAD -- TODO.md` is empty. Main's TODO already lists 10.2a and the "Engine: lying cylinder axial creep" next slice.
- **`deferred-work.md`.** Both sides were kept: the 10.2a F1 entry and main's harness-determinism entry each appear once.
- **Records added in the merge commit.** These are review and evidence files: my `evidence-10-2a-review/r2/` and the round-3 text of this record, both byte-identical to what I wrote, plus the implementer's `evidence-10-2a/` gate logs.

Six code files were auto-merged on both sides (`BundleSchema`, `BundleValidator`, the tests csproj, `BasketballResourceTests`, `PalettePlacementTests`, `WorkbenchCapacityTests`). Each is main's text plus the slice's own hunk only.

The R2/L5 inputs are unchanged by the merge (`git diff 6387b589 HEAD` is empty for each):
- `PlacementPose.cs`
- `WorkshopBenchPlacement.cs`
- `PlacementPoseTests.cs`
- `WeightTests.cs`
- `elements/Weight`
- `WorkshopPhysicsCompiler.cs`

So the round-3 R2/L5 checks and mutations (MX7, MX8) still apply.

### The restitution receipt is carried exactly once

- **Review record:** `review-engine-restitution.md` blob `7e4477ac` holds the terminal section once. Main's copy holds it zero times.
- **Sprint status:** `sprint-status.yaml` has one `engine-restitution-energy-correctness: done` row.
- **Evidence:** `evidence-engine-restitution-review/terminal/` is identical to the carry under `diff -r`.
- **Patch:** the carry patch reverse-applies cleanly.

### Gates on `56f3e327` (one rebuild)

| Command | Result | Raw |
| --- | --- | --- |
| `dotnet build CuriousContraptions.slnx -c Debug` | 0 W / 0 E | `r3/build-debug.log` |
| `dotnet test CuriousContraptions.slnx -c Debug --no-build --no-restore` (no blame) | native **2988/2988**, BundleElements 194, E2eRunner **208** (the node harnesses run inside `NodeHarnessTests`), Coverage 77, WorkerAotGuard 17; **Skipped 0** in every row; 0 duplicates | `r3/test-slnx.log` |
| CI "Test C# tools" step, run from the body of HEAD `pages.yml` | 77 / 194 / 17 / E2eRunner 192 (deferral filter generated from `BrowserDeferral.cs`); exit 0 | `r3/ci-tool-step-body.txt`, `r3/ci-tool-step.log` |
| Release Simulation / Animation.Worker | 0 W / 0 E each | `r3/worker-release.log`, `r3/animation-worker-release.log` |
| `git diff --check HEAD` | clean | — |
| origin/main at approval | `f1ad3bd19e27` (the same as the merge's second parent) | — |

## SnapshotApproval

**Approved candidate.** HEAD `56f3e327` plus the staged delta: this record and `evidence-10-2a-review/r3/`. The staged identities are recorded below.

Before committing, the implementer must:
- confirm the staged tree is unchanged;
- let the normal pre-commit protections run, with no `--no-verify`.

**Do not push until main's CI on `f1ad3bd1` (9A.23 + 9A.25b) is green.**

### Publication-dependent checks (the reviewer verifies them after the push)

| # | Check |
| --- | --- |
| 1 | The slice commit's tree equals the final staged tree reported with this approval. |
| 2 | Hooks: the implementer's commit log shows the gate builds at 0 W and passing tests, and a reviewer re-run on the exact published tree. |
| 3 | The published merge's tree equals the branch tree, and `origin/main` and `git ls-remote origin refs/heads/main` equal the published merge. |
| 4 | The Pages workflow on that exact SHA has completed with success: every step, including the C# game and tools tests, Skipped 0. |
| 5 | The Pages artifact belongs to that run and is deployed. |
| 6 | Serving: `/`, `/index.html`, `/404.html`, `/godot.wasm`, `/godot.pck` and `/_framework/dotnet.js` each return 200 from this deployment. |
| 7 | Status: `10-2a-weight` → done (scoped native/publication Pass; browser pending `cat-067`, including the engine-cylinder case), and TODO updated. |
| 8 | Browser cases remain pending for post-catalogue E2E (listed above). |

### Snapshot identities

These were taken after staging the candidate, before this section was appended:
- **Staged tree:** `a95056fafe10cfef4988a8939827f6162a2a4eea`.
- **SHA-256 of `git diff --cached --binary HEAD`:** `c53177ed64e1bba73d62ac2eb91102befba77dfe343adc47805a27881412b35f`.

This section was then appended and the record re-staged. The final staged tree is the one reported to the coordinator, and publication check 1 compares against it.
