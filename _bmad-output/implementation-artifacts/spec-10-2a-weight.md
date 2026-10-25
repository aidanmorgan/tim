---
title: "10.2a — Weight (CAT-067)"
status: review
baseline: 0cba945b1d3765a4bb95bd2bbd56dfe58f497e43
---

# Outcome and authority

The player can place a Weight. It is a dynamic blue solid cylinder whose `mass` parameter (0.25–8 kg, default 4) sets both its compiled mass and its size. It falls, rolls, rests end-down, stacks and topples a Domino. It has no rope until 10.2b.

**Authority:**
- Epics Story 10.2 and owner decision (f): the Weight ships with 10.2.
- 10.1 coordinator decision 1: the Weight is the UI's dynamic cylinder. It also inherits 10.1's carried native Workshop Reset/Save-Load proof of a placed cylinder, which moves from the Pulley to the Weight.
- CAT-067 W1–W6 and W20, and its §5 boundaries.
- DESIGN :241 and :341.

**Ordering:** after "Engine: restitution energy correctness" merges, and before 10.2b. Re-derive every pin on that merge.

**Generic capability:** one only, a body mass bound to a declared parameter.

**Deletes:**
- the CAT-067 §2/§3 lines "Add `WorkshopPartKind.Weight`" and "inventory row", the spherical-envelope text, and "10.4 (or 10.2…)";
- the solver local `weight` (T2).

There is no fallback, alias or Known entry, and the 9A.45a ratchet gains no entry.

# Finite design decisions

- **D1 Body.** One dynamic solid Cylinder, axis +Y, whose collider follows the art (CAT-067 Q1).
  - `weight.tscn` has `col_cylinder` (explicit radius and height at 4 kg) and a Visual node.
  - **Body origin at the base centre.** The collider's OffsetY equals h, so a mass edit grows the Weight upward, not into the bench.
    - The compiled centre of mass is (0, h, 0). The ingest already carries a body centre of mass (`WorkshopPhysicsEngine.cs:509`).
    - The Visual pivot is at the base.
    - **For 10.2b:** the draft's "Tie at the centre of mass" now needs a size binding.
  - Material (e, μ, Crr, threshold) is (0.08, 0.3, 0.03, 0.1), with drag 0, answering Q2. Damping (Q5) and length (Q6) stay with 10.4.
  - `PickRadius` is 0.7, which covers 8 kg: √(0.544² + 0.432²) = 0.695.
- **D2 Size from mass (Q3, decided by game intent).** Four linear `ColliderParameterBinding`s on collider 0, exact at 0.25 and 8 kg against the legacy r = 0.85·0.32∛m and h = 0.675·0.32∛m. X and Z use identical coefficients, because admission rejects X ≠ Z.

  | Binding | Coefficients |
  | --- | --- |
  | X, Z | 0.04808397·m + 0.15932827 |
  | Y and OffsetY | 0.03818433·m + 0.12652539 |

  | m | r | h |
  | --- | --- | --- |
  | 0.25 | 0.17135 | 0.13607 |
  | 4 | 0.35166 | 0.27926 |
  | 8 | 0.544 | 0.432 |

  - At 4 kg the size is about 19% under the cube-root law, but it is monotone and visibly larger, so no non-linear law is needed.
  - The art has one `UniformScale` binding, 0.136734·m + 0.453070. Two cream bands are fixed, because ⌈mass⌉ bands cannot be expressed linearly; record this in CAT-067 §2.
- **D3 Mass binding.** This verifies and simplifies the draft's D2: mass equals the parameter, so `MechanicalValue` is reused rather than adding a new Multiplier/Offset type.
  - `IParameterizedMassElement : IMechanicalElement { MechanicalValue Mass }` is carried as `MechanicalPayload.MassParameter`, an optional field appended like `Capture` and `ContactWork`.
  - Validator rules:
    - the source is a Parameter of Kind Real and Unit Kilogram;
    - the body is Dynamic;
    - [Min, Max] lies within [1/1024, 1024];
    - **one truth:** the fixed `MassProperties.Mass` must equal the binding at the parameter's Default (4). `PackageRuntimeParityTests` compares declared and compiled mass at defaults.
  - `WorkshopPhysicsCompiler` (:52/:65, the only mass consumer) evaluates the binding.
  - The ABI mass is binary16 (`(Half)m`); save/load keeps the f32.
  - Inertia is compiled from the bound collider. `InertiaTensor` is unread; it is declared with the 4 kg analytic value (0.22765, 0.24733, 0.22765).
- **D4 Editor.** `mass`: Kilogram, Real, 0.25–8, default 4, `EditorPolicy: Clamp`, RunEdit BuildOnly. Admission, save and wire still reject out-of-range values.
- **D5 Ports `[]`.** Tie, Rope and `Load` belong to 10.2b; declaring them here would be a second capability.
- **D6 Catalogue and art.**
  - Id `weight`, "Weight", Category Motion ("Ropes" is not an `ElementCategory`; this follows BowlingBall), colour (0.27, 0.39, 0.61, 1), `PaletteOrder: 31`.
  - Description: "A heavy cylindrical load. Larger weights are heavier."
  - Art: blue body, cream base and two cream bands, and a gold eye ring on top.
  - The icon is from `ui/WorkshopIcons.cs@a6c914e:L95`.
- **D7 Supported-impact restitution (deferred entry checked): not relevant.** The worst catalogue impact onto a resting Weight is a Ball at e = 0.55 × 0.08 ≈ 0.044. The defect loses bounce rather than gaining energy, and the Weight is not a plank, crate or seesaw.
- **D8 Stacks.** `engine-resting-stack-settling` is deferred, so stacks use the restitution D10 criterion: no restitution after settling, late per-tick motion ≤ 0.05 mm, and each resting contact within the game-grade 0.5 mm envelope (the lower base on the bench, the upper base on the lower Weight's top face). Corrected wording (coordinator decision on F2, 24 Oct 2026): the envelope is per contact, so a stack's top may sit up to 0.5 mm per contact below the analytic height.

# Ordered tasks

- [x] **T1 Baseline and tests.** Run one Debug slnx build and the full test suite on the post-restitution main, and keep the receipts. Write the tests red first.
- [x] **T2 Solver scan.**
  - Rename `PrismaticConstraintSolver.cs:278-279` `weight` → `gravityLoad`, with no behaviour change.
  - `rg -nP '(?<![a-z])[Ww]eight(?![a-z])'` over the five scanned files must give 0 hits.
  - Add the rows `("weightRow", true)` and `("gravityLoad", false)`. The Known list and its hash are unchanged.
- [x] **T3 D3.** Add the interface, the payload field, the validator rules and the compiler evaluation. Regenerate `elements.bundle.json`, and list the bundle golden and identity pins that move.
- [x] **T4 Package.** `elements/Weight/`: the declaration with named arguments, `weight.tscn`, `icon.svg` with its import, the csproj and the `.uid`. No `PartId` branch.
- [x] **T5 Membership** (re-derive from the merged bundle):
  - the slnx and the tests csproj;
  - `ImpactRules.ElementPackages["Weight"] = new("weight")`;
  - Coverage: admitted +1 and a `weight` path. The planned example stays `pulley`, which is still absent.
  - Pins:
    - `PartRegistryTests` 28→29, ids 30→31;
    - SaveFixture;
    - `BasketballResourceTests`;
    - `BundlePortsTests`;
    - `CSharpStaticGateTests`;
    - `PackageRuntimeParityTests`;
    - `WorkbenchCapacityTests`;
    - `PalettePlacementTests`;
    - `PaletteOrder` uniqueness.
- [x] **T6 PRIMARY** (stacks pass D8 as corrected: the per-contact envelope; F2) native tests** (`WeightTests`; real Workshop construction → compiler → `WorkshopPhysicsEngine`):
  - **Boundaries.** The literals 0.25 and 8 are admitted. These reject:
    - 0.249, 8.01, 0, −1, NaN and ±∞;
    - a missing `mass` (the count check: no default fill and no migration);
    - an unknown key.

    Each rejection is tested at the editor seam, at save pre-validation and at wire decode, and leaves the construction and save byte-identical with no Undo push.
  - **Validator.** These are refused:
    - a fixed mass ≠ the binding at Default;
    - a non-Kilogram or Integer parameter;
    - a parameter outside the mass bound;
    - a binding on a Static body;
    - X ≠ Z coefficients.
  - **Compile** at 0.25, 4 and 8 kg:
    - mass = `(Half)m`;
    - r, h and the offset match D2;
    - the centre of mass is (0, h, 0);
    - inertia is m r²/2 and m(r²/4 + h²/3) within 10.1's Half bound;
    - the binding at 4 kg equals the extracted scene collider;
    - scale ÷ r is constant, within an explicit f32 tolerance of 1e-5 relative.
  - **Size** strictly increases over 0.25 < 1 < 4 < 8.
  - **Rest end-down** at all three masses: penetration ≤ 0.5 mm, |ω| below the rest tolerance, no drift over 10 s, and no tick above m·g·ContactSlop.
  - **Mass edit:** take a bench-resting 0.25 kg Weight, edit it to 8 kg, then Run. The base stays on the bench (≤ 0.5 mm) and no tick exceeds slop.
  - **Rolling:** on its side down a Ramp, ω ≈ v/r within 2%. On the bench it stops under Crr without axial creep.
  - **Stacking:** 4 on 4 and 1 on 8, both end-down, hold under D8.
  - **Domino:** a Weight rolling off a Ramp topples a Domino (tilt > 45°). Control: a Weight resting 0.1 m away leaves it upright for 5 s.
  - **Reset:** bit-identical over 600 ticks at 60, 120 and 240 Hz, including after a mass edit.
  - **Save/load:** pose, rotation and `mass` round-trip exactly. `OldLayoutSaves` and `SaveFixture` load unchanged. No wire or schema change.
- [x] **T7 Gates still green:**
  - the 10.1 routine subset (`WorkshopPhysicsLawCylinderTests`, including `FreshSeedDropsGainNoMoreEnergyThanTheBoxControlAndTheSlop`, the walk tests and the feature ids);
  - `WorkshopPhysicsLawRestitutionTests` and Lively at 0/200 in every class;
  - the 12 Playable lessons at 3 precisions.
- [x] **T8 Mutations:** each must turn a test red.
  - the fixed mass is used;
  - X ≠ Z;
  - OffsetY is dropped (the mass-edit test);
  - the one-truth rule is removed;
  - the rename is reverted.
- [x] **T9 Docs:**
  - CAT-067 §1/§2/§3, and §6 Q1–Q4 answered;
  - the 10.1 carry note → the Weight;
  - epics and vertical-delivery: 10.4 is pendulum qualification only;
  - the 10.2b draft's Tie note;
  - `TODO.md`;
  - `sprint-status`, with 10-2 split into a/b/c.
- [x] **T10 Full gate on the frozen candidate:**
  - one 0-warning Debug slnx build;
  - `dotnet test CuriousContraptions.slnx --no-build --no-restore` (no `--blame-crash` on the full slnx);
  - the CI "Test C# tools" step under bash;
  - the Release Simulation and Animation.Worker builds;
  - `git diff --check`;
  - `anvil check`;
  - the 9A.45a ratchet with no new entry.
- [ ] **T11 Publication.**
  - Independent review, then SnapshotApproval, then protected commit and push. Verify the remote, CI and Pages, and write the terminal `review-10-2a-weight.md`.
  - Browser cases stay pending until post-catalogue E2E. `cat-067` covers: place, edit mass with clamp, growth, fall, roll and rest end-down, a stack, the Domino and its control, Run/Reset and Save/Load. It includes the **engine-cylinder dynamic case** from 10.1.

# Code Map

- **New:** `elements/Weight/*` and `CuriousContraptions.tests/WeightTests.cs`.
- **Change:**
  - `Abstractions.Mechanical/MechanicalContracts.cs`;
  - `CuriousContraptions.Bundle/BundleSchema.cs` and `BundleValidator.cs`;
  - `engine/gpu/WorkshopPhysicsCompiler.cs`;
  - `engine/PlacementPose.cs` (**scope addition**: palette rest threw "no rule for a Cylinder collider" for any dynamic cylinder; it now poses a cylinder with its exact projection, h·|a·n| + r·√(1 − (a·n)²); box arithmetic is unchanged bit for bit);
  - `PrismaticConstraintSolver.cs` (the rename);
  - `WorkshopPhysicsLawTests.cs`;
  - `BundleValidatorTests`;
  - the T5 pins and the T9 docs.
- **Reuse:**
  - `MechanicalValue`, `ColliderParameterBinding` and `PresentationParameterBinding`;
  - the 9A.27 editor;
  - the BowlingBall and Domino templates;
  - the 10.1 and restitution harnesses.
- **Do not change:**
  - `workshop_wire.fbs` and `ConnectionRecord`;
  - `PortDescriptor` and `WorkshopConnections` (10.2b);
  - the collider record and Half lanes (9A.44);
  - `IsPayload`;
  - the narrowphase and solver laws;
  - `PartId`.

# Current state

24 Oct 2026, implementation owner. Native candidate on base 0cba945b in worktree `story-10-2a`, **uncommitted**. Next: independent review, then SnapshotApproval and publication (T11). Evidence: `evidence-10-2a/` (baseline, dev, probe, mutations, final).

**Delivered.** `IParameterizedMassElement` → `MechanicalPayload.MassParameter` (optional, appended); validator: Parameter source, one Real kg parameter, range in [1/1024, 1024], Dynamic owner, fixed mass bit-equal to the default; a cylinder's X/Z bindings equal or both absent (`ColliderBindings[i].CylinderRadius`); the compiler evaluates the binding (the only mass consumer). Package `elements/Weight` per D1–D6 (scene collider at 4 kg = the binding bit for bit: r 0.35166413, h 0.27926272). T2 rename `weight` → `gravityLoad`; scan rows added; Known list/hash unchanged; 0 hits in the five solver files. Placement rest supports cylinders (scope addition above).

**Pins moved (T3/T5).** Bundle 28 → 29 entries; every mechanical entry gains `"MassParameter": null` (the bundle is generated and gitignored; no bundle hash pin exists); the 9A.16 compiled-geometry golden excludes `weight` (like the gates); `BundlePortsTests` row, registry 29 / ids 31, SaveFixture 29, `BasketballResourceTests`, `CSharpStaticGateTests`, `PackageRuntimeParityTests` (exclusion, `Placed` arm, `mass` row, cylinder lanes in `AssertColliders`), `WorkbenchCapacityTests`, `CurrentCatalogueTests` 29 + `weight` path (planned example stays `pulley`), `ImpactRules["Weight"]`, `BundleRoundTripTests` (`MassParameter` pair). `PaletteOrder` 31 is unique (the generator's BUNDLE008 check passed).

**Tests.** `WeightTests` (28), `PlacementPoseTests.ADroppedWeightRests…`, `PalettePlacementTests.TheWeightMassEditor…` (D4: finite entries clamp, one Undo each; NaN/±∞/text refused; the handler seam refuses 0.249, 8.01, 0, −1, NaN, ±∞, missing and unknown keys with no change, command or Undo), `BundleValidatorTests.AMassBinding…`.

| Gate (frozen candidate) | Result |
| --- | --- |
| Baseline (0cba945b) | build 0 W; native 2928, BundleElements 191, E2eRunner 190, Coverage 77, AOT 17 (`baseline/`) |
| Debug slnx build | 0 W / 0 E (`final/build-debug.log`) |
| Full `dotnet test CuriousContraptions.slnx -c Debug --no-build --no-restore` | native **2961/2961**, BundleElements 194, E2eRunner 190, Coverage 77, WorkerAotGuard 17; **0 skipped** (`final/test-slnx.log`); includes the 10.1 cylinder subset, the restitution suite and the lessons |
| CI "Test C# tools" step under bash | Coverage 77, BundleElements 194, AOT 17, E2eRunner 174; exit 0 (`ci-tool-step.sh`, `final/ci-tool-step.log`). The step (unchanged on main 8411ba3b) deselects 16 E2eRunner toolchain test cases (15 names; 190 − 174 = 16) by filter; this is now handled by the CI slice's explicit deferral list (story-ci-no-skip). All 190 run unfiltered in the slnx row |
| Duplicate test IDs | 0 "duplicate" lines in `final/test-slnx.log` |
| Release Simulation / Animation.Worker | 0 W / 0 E each |
| `git diff --check`; `anvil_check` on 22 changed C# files; solver-file `weight` scan | tracked files clean; with new files, only trailing whitespace inside raw build logs (as in committed 9.8b logs); 0 findings; 0 hits |
| After the gate | M1 rerun and M11 (each reverted), `epic-10` left at backlog; one Debug rebuild of the restored inputs, `WeightTests` 28/28 |
| Merged candidate e52c5473 (WIP 59b37080; merge f03ee5bc of main 8411ba3b; d4036e78 restitution receipt + behavioural assertions; merge e52c5473 of main dc0356de; includes the F3 fix) | Debug 0 W; full slnx: native **2962/2962**, BundleElements 194, E2eRunner 190, Coverage 77, WorkerAotGuard 17, **0 skipped**, 0 duplicate IDs; CI tools step exit 0; Release workers 0 W; `git diff --check` clean (`merged-dc0356de/`) |

| Mutation (T8) | Red |
| --- | --- |
| M1 compiler uses the fixed mass | 5 (compile 0.25/8, boundaries 0.25/8, editor; `m1-fixed-mass-used-rerun.log`). Failed attempt kept: `m1-fixed-mass-used.log` also showed the roll test red from its earlier 0.01 m/s stop threshold, unrelated to M1 |
| M2 declaration Z ≠ X | build fails BUNDLE006 `CylinderRadius` |
| M3 OffsetY binding dropped | 7 incl. mass edit, rest, stack |
| M4 one-truth rule removed | `BundleValidatorTests.AMassBinding…` |
| M5 rename reverted | solver scan + isolation ratchet |
| M6 X = Z rule removed | `BundleValidatorTests.AMassBinding…` |
| M7 size not scaling (X/Y/Z bindings dropped) | 7 |
| M8 centre origin (OffsetY ≡ 0) | 13 incl. placement |
| M9 placement Cylinder arm removed | `PlacementPoseTests` + editor |
| M10 drop ramp restored (creep discrimination) | roll test (axial envelope) |
| M11 drop ramp with the axial-envelope assertion removed | roll test (post-stop check: moved 0.21 mm after stopping) |
| M12 placement volume back to the origin point (F3 undone) | `PlacementPoseTests.TheBenchVolumeBoundsTheRestedBodyNotItsOrigin` |

**Findings and coordinator decisions (24 Oct 2026, under owner delegation).**
- **F1 Drop-landing axial creep (engine; deferred).** A cylinder that drops onto its side (even a plain LawScene cylinder, no package) can settle with its axis tilted ~1.5e-4 rad and then creeps along its axis at ~0.05 mm/s indefinitely (0.5 mm over 14 s). A bench roll without a drop, and a Weight set down on its side, do not creep. Reproducer: `probe/` (source and log), M10/M11. Decision: a shared 10.1 defect, not the Weight's; its own engine slice "Engine: lying cylinder axial creep" follows, with the drop-ramp case as its entry check (`deferred-work.md`, TODO "Next"). 10.2a keeps the flush-ramp roll test; no red or skipped test is committed.
- **F2 D8 (accepted).** Measured as an absolute top error the stack top sits 0.68 mm (4 on 4) and 0.55 mm (1 on 8) low: each resting contact sinks 0.28–0.34 mm and two contacts add. Decision: the game-grade envelope is per contact; D8 is reworded accordingly and the per-contact bound stays at 0.5 mm.
- **F3 Placement volume (fixed here).** The B1 volume checked the lifted origin, so a Weight rested on a bench-standing plate (base −0.16 m) was refused and one rested at 8.9 m admitted with its top above 9 m. Decision: placement bounds use the placed body's world-space extent. `PlacementPose` now validates the lifted pose's posed colliders: x ∈ [−7, 7], z ∈ [−4, 4], y from the bench surface (less the 10⁻⁴ m contact slop) to 9 m; the work-point gate `Inside` is unchanged. Tests: `TheBenchVolumeBoundsTheRestedBodyNotItsOrigin` (Weight, Basketball and Domino on the bench plate admitted; on a plate faced at 8.9 m all refused, the Weight's origin being inside the old point volume). `ARestedPoseAboveTheBenchIsRefused`'s control moves from 8 m to 7.5 m, because the old control admitted a Domino whose top was at 9.18 m.
- **Deviation.** Tests were written alongside the package rather than strictly red-first; the mutation table is the red evidence. During debugging `WeightTests.cs` was edited once with `sed -i` and once with a `python3` heredoc, and a few edits (the CA2208 fix, test probes) went in without a prior Anvil validation; `anvil_check` on every changed file is clean.

**Review round 1 (Fail R1, [review](review-10-2a-weight.md)) — fixes.** R1: `PlacementPose.Overlaps` tested a cylinder against a static sphere as a point (radius 0); a sphere now meets a box or a cylinder at its exact closest point (`ClosestOnCylinder`: axial coordinate clamped to ±h, radial part to r), and the doc comment is corrected. Test `ALiftedWeightBesideABumperIsRefusedAndAClearGapIsAdmittedLikeABall` (Bumper 0.9 m away refused IntoPart, 1.05 m admitted, a Basketball the same each time). Coordinator additions: (a) the world-extent bench volume now bounds every placement path (snap, grid and rest; `Bounded`), test `AGridPlacementIsBoundedByTheBodyNotItsPoint`; `EveryOtherPlacementKeepsTheBasePose`'s Wall case moves 1 m up because centred on the bench plate its 2 m body reached below the bench; (b) the stack test adds a last-second path-length bound ≤ 0.5 mm; (c) the CI deselection count is corrected to 16 cases. Mutations M13 (point cylinder) and M14 (grid unbounded) turn the new tests red (`mutations/m13-r1-point-cylinder-and-m14-grid-unbounded.log`).

**Review round 2 (Incomplete R2, L5) — fixes.** R2: `ATubeMouthSnapAcrossTheBenchEdgeIsRefusedAndOneInsideIsAdmitted` joins a Pipe at a placed Pipe's end mouth (centre 2 × mouth offset beyond it): next to x = 3 its body would reach x ≈ 8.6 and is refused `SnapOutsideBench`; next to x = −3 it is admitted. L5: `PlacementRefusal` now types each path (`OutsideBench` grid, `SnapOutsideBench`, `RestOutsideBench`, `IntoPart`), each with its own status text (`WorkshopBenchPlacement.Refuse`; doc corrected); `EachPlacementRefusalHasItsOwnStatusText` asserts every reason's typed rejection, unchanged construction and text, and the rest/grid tests assert their path's reason. Mutations: M15 (snap path unbounded, reviewer MX7) and M16 (grid path given the rest reason) each turn a new test red (`mutations/m15-snap-unbounded-m16-grid-reason-wrong.log`). Checked against `docs/testing-guidance.md`: defined interface (`PlacementPose.Compose`, `WorkshopBenchPlacement.Refuse`), expectations derived from declared geometry, positive/control pairs, mutation-proven, deterministic, no hashes.

**Behavioural assertions (owner rule, main dc0356de).** 10.2a adds no hash or byte-identity assertion: the refused-mass test checks the admitted and re-loaded mass; Reset checks every body back at its built pose (≤ 1 µm, ≤ 1 µrad) and at rest; replay and the reloaded run check poses and motion within the 0.05 mm late-motion bound; the save round trip checks decoded values (pose, rotation, mass bits), not re-encoded bytes.

**Pending (post-catalogue E2E).** `cat-067`: place, edit mass with clamp, growth, fall, roll and rest end-down, a stack, the Domino and its control, Run/Reset and Save/Load, the art's uniform scale, palette rest of a cylinder, and the **engine-cylinder** dynamic case from 10.1. 10.2b: the Tie (needs a size binding; CAT-067 §2).
