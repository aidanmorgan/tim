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
