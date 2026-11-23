---
title: 'ELEMENTS-VISIBLE-SOLID-REMAINDER, part B2: Tennis ball seam printed flush'
type: 'bugfix'
created: '2026-11-22'
status: 'draft'
baseline_revision: 'captured at dispatch (the main that contains the planning work)'
review_loop_iteration: 0
followup_review_recommended: false
context:
  - '{project-root}/docs/testing-guidance.md'
  - '{project-root}/docs/delivery-workflow.md'
  - '{project-root}/_bmad/custom/element-free-engine-design.md'
warnings: []
deferred: []
---

<intent-contract>

## Intent

**Problem:** The owner's rule is that what a player sees as solid is solid. The Tennis ball draws its seam as a torus that stands 3 mm proud of its felt with no collider, so a raised solid is drawn that does not collide, and the package is outside the visible-solid table.

**Approach:** Like the Pool ball's stripe, the seam is printed flush with no collider (owner decision 24i, 23 November 2026): redraw it as a thin open band lying on the felt within the delivered print tolerance, keep the ball's one sphere collider, and add the Tennis ball to the visible-solid table with the seam classified as printed flush.

## Boundaries & Constraints

**Always:** The seam keeps its node name `Visual/Seam`, its colour and its great circle (turned 45° about X); it is redrawn as an open band whose every vertex lies between the felt's radius and 0.5 mm beyond it at the default radius (the print bound of `FaceMarkingTests.cs:60`), and whose flat facets never dip inside the felt's sphere. Seam and felt stay under the one `Visual` node, scaled together by the radius parameter, so the seam stands proud by the same fraction of the radius at every size. The ball keeps exactly one collider, its sphere; its mass, inertia, bindings, airflow samples, buoyancy and drag are unchanged, so the compiled entry equals the baseline's. Reset, save and load restore the ball as before. Follow the handoff order, Anvil before each write, one retained implementer, the engine review tier and one full local round with zero failures and `Skipped: 0`.

**Never:** A collider for the seam. A radius-bound torus or any new collider binding. A change to the matcher's rules or tolerances. A change to the declaration, the radius range or any other parameter. A raised seam kept beside its print. An edit to any existing test's code, cases or assertions; the only test changes are the new `tennis` row of `VisibleSolidColliderTests.Specs` and the new fact in `TennisArtTests.cs`. Any other package. Hashes, exact arrival ticks, mutants, browser runs, skipped tests.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| T1 seam printed flush | The Tennis ball's scene at the default radius | `Visual/Seam` is an open band (no caps) on the seam's great circle; every vertex lies between 0 and 0.5 mm outside the felt's 0.25 m radius; its facets between vertices stay outside the felt's sphere; it is the cream seam colour | A seam more than 0.5 mm proud, or buried inside the felt, fails |
| T2 no collider | The compiled Tennis ball at the default radius and at 1/16 m and 16 m | Exactly one collider, a sphere of the ball's radius; the bundle entry equals the baseline's | A second collider fails |
| T3 rolls true | A Tennis ball rolling on the bench at radius 0.125 m and 0.5 m (existing contact-spin cases) | Its speed and contact spin are those of a solid sphere; the existing bounds pass unedited | Existing |
| T4 table coverage | The visible-solid test at the default radius and both radius bounds | The felt matches its sphere collider; the seam is classified as printed flush; no collider is without art | Existing failure messages |
| T5 retained behaviour | The named existing Tennis tests | Pass with no edit | Existing |

</intent-contract>

## Code Map

Delivery shape: one small product repair over one package, split from part B (`spec-elements-visible-solid-remainder-b.md`). The repair story runs in the order A, B, B2, C. Review tier: **engine**: every capability is present and the scene, table row and test are on the data-only allow-list, but the candidate commit carries `TODO.md`, which is outside it (`_bmad/custom/bmad-loop-protocol.md`, Review tiers; the Baseball repair is tiered the same way for the same reason). No new source file and no test-project edit: `CuriousContraptions.tests.csproj:77` compiles every top-level `*.cs` (S2, `25df3cd5`).

**When.** Before the engine programme: repair parts A, B, B2 and C and the Baseball repair are among the slices owner decision 22 (23 November 2026) allows before it. This part follows part B and is replanned from the revision that publishes it; that is sequence, not an external blocker.

**Baseline.** Captured at dispatch (coordinator decision 38, 23 November 2026): `baseline_revision` is the published main that contains the planning work (and, by sequence, part B), and every scope guard and `git diff` in this contract compares against it. Drafted on `25df3cd5`, which is the drafting revision only, not a baseline. Precondition (coordinator decision 37, 23 November 2026): the planning work of 22–23 November 2026 (the `docs/planning/elements/readiness/` folder with owner decision 24i and the Repairs table of `delivery-batches.md` section 5) is published, and part B is published; the baseline is the main that contains both, captured by the coordinator before first implementation, and the line anchors in `VisibleSolidColliderTests.cs` are re-read there. Every file cited with a line number was read at `25df3cd5` on 23 November 2026. Expected totals are baseline + delta against the current full local round.

Planning verdict: Scoped Pass (fourth audit, `docs/planning/elements/readiness/audits/audit-visible-solid-B2-4.md`); fixes applied 23 November 2026. The fourth audit judged the flush-print design of owner decision 24i and raised N1 (frontmatter baseline), applied here; no re-audit needed. The third audit (`docs/planning/elements/readiness/audits/audit-visible-solid-B2-3.md`) audited the earlier torus design; owner decision 24i replaced that design, which voids its F1, F2, R1 and R4 and the matcher changes it checked. Its R2 (baseline precondition, without the withdrawn airflow entry) and R3 (stale record lines) are applied. Planning only; no test has run.

Owned scope:

- `elements/TennisBall/tennis.tscn` -- the `Seam` sub-resource at 16–19 (today a `TorusMesh`, inner 0.245, outer 0.253) becomes the band below; `Visual/Seam` at 31–33 keeps its name, quaternion and mesh reference. `load_steps` stays 6 (five sub-resources before and after).
- `CuriousContraptions.tests/VisibleSolidColliderTests.cs` -- one `Specs` row, after the `bowling` row (`:110` at `25df3cd5`).
- `CuriousContraptions.tests/TennisArtTests.cs` -- one new fact (T1, T2).
- `docs/planning/elements/CAT-064-tennis.md` -- the seam's art row and the conformance point 3 (delivered flush); `docs/planning/invest/delivery-batches.md` section 5 (Repairs, part B2's row); `TODO.md`; this contract and its run log.

Not changed: `elements/TennisBall/TennisBallElementDeclaration.cs`, every existing test, `content/puzzles.json`, `deferred-work.md`. `elements/elements.bundle.json` is build output, not committed (`.gitignore:17`); task 0 captures a baseline copy.

### Capability probe

Four present; none untested, narrower or missing.

- **Present.** Art that is not a collider: the extractor bakes only `CollisionShape3D` and `col_` nodes (`tools/BundleElements/SceneGeometryExtractor.cs:23–25`), so `Visual/Seam` yields no collider whatever its mesh. Test: `TennisArtTests.NativeFeltAndSeamScaleTogetherWithTheConfiguredPhysicalSphere` (`:34`, one collider for the ball's body).
- **Present.** Art classified as print in the visible-solid table: the `Decorative` list of `ArtSpec` ("printing flat on a face", `VisibleSolidColliderTests.cs:50–51`), checked for classification only. Test: `VisibleSolidColliderTests.EverySolidArtPartHasItsColliderAndNoColliderReachesBeyondTheArt` (the `delay` and `reverse_transmission` rows' decorative entries).
- **Present.** Seam and felt scaled together by one presentation binding on `Visual` (`TennisBallElementDeclaration.cs:24`, UniformScale 4 · radius). Test: `TennisArtTests.NativeFeltAndSeamScaleTogetherWithTheConfiguredPhysicalSphere`.
- **Present.** The delivered print bound, 0 to 0.5 mm in front of the surface, never buried: `FaceMarkingTests.cs:60`. Test: `FaceMarkingTests.EveryPrintedSurfaceIsPresentFrontFacingAndAttachedToTheRotatingFace`.

### Repair

| Mesh | Today | Repair |
|---|---|---|
| `Seam` sub-resource, drawn by `Visual/Seam` (quaternion (0.38268343, 0, 0, 0.9238795), 45° about X) | `TorusMesh`, inner 0.245, outer 0.253: 3 mm proud of the 0.25 m felt, no collider | `CylinderMesh`, material `Cream`, `top_radius = 0.2504`, `bottom_radius = 0.2504`, `height = 0.008`, `cap_top = false`, `cap_bottom = false`, default 64 radial segments: an 8 mm open band on the same great circle (the cylinder's axis is local Y, as the torus's was) |

Numbers, at the default radius (Visual scale 1). A band vertex at height y lies √(0.2504² + y²) from the centre, y within ±0.004: 0.2504 to 0.250432 m, so 0.40 to 0.43 mm outside the felt, inside the 0.5 mm bound. Between vertices a facet comes nearest the centre at 0.2504 · cos(π/64) = 0.250099 m, so no facet dips inside the felt's sphere, and none inside the felt mesh, which is inscribed in it. At radius r the whole `Visual` node is scaled by 4r, so the seam stands proud by 0.16 to 0.17 % of r at every size (0.1 mm at 1/16 m, 27.6 mm at 16 m), under the felt mesh's own faceting at the same size (its 32 rings alone sag 0.48 % of r). The 0.5 mm bound is the bound at the drawn default size, the one size at which the print contract's metre-scale bound applies; the scale relation is proven by the existing scale test.

`Specs` row (solid; hollow; arc; decorative; moving roots): `tennis`: `["Visual/Felt"]`; none; none; `["Visual/Seam"]`, its comment citing owner decision 24i ("printed flush, like the Pool ball's stripe; no collider"); none. At the 16 m bound the felt compares exactly (0.25 × 64 and 16 × 1 are exact in f32), so the row needs no `Fit` and no matcher change.

### Decisions

Owner decision 24i (23 November 2026): the Tennis ball's seam is printed flush with no collider, like the Pool ball's stripe (owner decision 11); part B2 is a flush-print repair, with no radius-bound torus, no matcher change and no fan-shadowing consequence.

Decided here, with reason:

- **D1. An open band, not a thinner torus.** Every vertex of a band lies within the print bound, as `FaceMarkingTests` requires of print; a torus always has vertices deep inside the felt.
- **D2. 0.4 mm stand-off.** The least radius whose facets stay outside the felt's sphere with 64 segments (0.2503 would dip 1.5 µm inside); its edges stay under 0.5 mm.
- **D3. The seam keeps its width (8 mm) and great circle,** so the ball reads as before at a glance, now flush.

No owner decision is open.

## Tasks & Acceptance

**Execution:**

0. Before any edit -- build at the baseline and copy `elements/elements.bundle.json` to `.tools/visible-solid-remainder-b2-evidence/bundle-baseline.json` -- AC2's baseline.
1. `CuriousContraptions.tests/TennisArtTests.cs` -- add the fact of T1 and T2 and run it on the baseline: it fails on the 3 mm torus -- red before green.
2. `elements/TennisBall/tennis.tscn` -- replace the `Seam` sub-resource with the band of the repair table -- T1.
3. `CuriousContraptions.tests/VisibleSolidColliderTests.cs` -- add the `tennis` row -- T4.
4. `CAT-064-tennis.md`, `delivery-batches.md`, `TODO.md` -- record the seam as delivered flush (radius 0.2504, height 0.008, 0.40–0.43 mm proud at the default size, the same fraction of the radius at every size), no collider, part B2's row delivered and part C queued.

**Acceptance Criteria:**

- AC1: Given the Tennis ball, when the visible-solid theory runs, then its row passes at the default radius and at both radius bounds, and the theory has one more case than at the baseline.
- AC2: Given the baseline copy and the candidate's bundle, when each entry is compared by decoded value, then every entry, `tennis` included, is equal: one sphere collider and three collider bindings.
- AC3: Given the named existing tests (verification list), when they run, then all pass with no edit.
- AC4: Given the candidate, when the full local round runs, then every project reports zero failed and `Skipped: 0`, and the total is the baseline total + 2 (one theory case, one fact). The registration check's total is the baseline's + 0 (its filter selects neither class).
- AC5: Given the candidate diff, when its paths are listed, then none is under `engine/`, `CuriousContraptions.Simulation/`, `CuriousContraptions.Bundle/`, `CuriousContraptions.Abstractions/`, `schemas/`, `tools/`, and `elements/TennisBall/TennisBallElementDeclaration.cs` is unchanged.

Counts in AC1 and AC4 are baseline + delta, counted on the slice's baseline at start, so the order in which this part, part C and the Baseball repair publish does not change them.

## Spec Change Log

## Review Triage Log

## Design Notes

1. **Behaviour delivered.** The Tennis ball's seam is printed flush; its only solid is its sphere; the package joins the visible-solid proof. Source: owner rule "visible = solid" (25 October 2026) and owner decision 24i (23 November 2026).
2. **Reusable.** Nothing new: the decorative classification and the print bound exist.
3. **Declared.** One mesh resource in the scene. No declaration or engine identity changes.
4. **Execution.** Scene → `SceneGeometryExtractor.Bake` → the unchanged bundle entry → the unchanged compiler and solver.
5. **Ownership.** No new collider, body or state. Reset, save and load are unaffected.
6. **Generality.** Not an engine change.
7. **Deleted.** The proud torus seam. No raised copy remains.
8. **Uncertain.** None beyond the unexecuted tests. Rules E1, E6, E7 and E8 apply; E9 is run as always.

## Verification

Executor for every command: the implementation owner. Raw output goes under `.tools/visible-solid-remainder-b2-evidence/`.

Environment: the pinned toolchain first on `PATH`: `PATH=$HOME/.local/share/tim-toolchain/dotnet-10.0.403:$HOME/.local/share/tim-toolchain/node-v26.12.1-darwin-arm64/bin:$PATH` and `DOTNET_ROOT=$HOME/.local/share/tim-toolchain/dotnet-10.0.403` (on this host the default `PATH` has Homebrew node v26.13.1 and a dotnet without `wasm-tools`, so the capture's node check and the web build fail otherwise); record the session environment check in the run log.

### Acceptance-to-test table

| Rows | Named discriminating test | Control and boundary | Run |
|---|---|---|---|
| T1, T2 | `TennisArtTests.TheSeamIsPrintedFlushOnTheFeltAndTheBallHasOnlyItsSphereCollider` (new fact). From the part made by `PartRegistry`: `Visual/Seam` is a `CylinderMesh` with both caps off and the scene's `Cream` seam material; every vertex of `Mesh.GetFaces()`, placed by the seam's transform in the `Visual` frame, lies at a distance from the felt's centre in [0.25, 0.25 + 0.0005] m; `TopRadius · cos(π / RadialSegments)` is at least the felt's radius (no facet inside the felt). The `tennis` bundle entry has exactly one collider, a sphere, at the default parameters and at both radius bounds | Controls by reading: today's torus fails both (outer 0.253 is 3 mm proud; its inner vertices are buried); a band of radius 0.25 fails the facet check. Boundary: the band's edge vertices, 0.43 mm out, nearest the bound. 0.5 mm is the print bound `FaceMarkingTests.cs:60` uses for the same quantity | planned |
| T4, AC1 | `VisibleSolidColliderTests.EverySolidArtPartHasItsColliderAndNoColliderReachesBeyondTheArt`, case `tennis` (default, 1/16 m and 16 m) | Without the row the package has no case; with the seam left unclassified it fails "art is not classified once"; a seam collider would fail "collider(s) reach beyond the art" | planned |
| T2, AC2 | The AC2 comparison (command below) | The baseline entry is the control: equal | planned |
| T3 | `TennisBallTests.ConfiguredRadiusChangesContactSpinAtFixedMass` (existing, both cases unedited): speed and rim speed in [0.68, 0.75] m/s, a solid sphere's sticking roll. The 0.125 m case rolls about 79° in its 30 ticks, so a proud seam collider would meet the bench inside it | Existing bounds | planned |
| T5, AC3 | Existing, unedited: `TennisArtTests.NativeFeltAndSeamScaleTogetherWithTheConfiguredPhysicalSphere`, `TennisBallTests` (all), `TennisInteractionTests` (3), `FanIntegrationTests`, `FanAdmissionTests`, `FanGuidanceTests`, `WindmillAdmissionTests`, `PackageRuntimeParityTests`, `F32BehaviourIdentityTests`, `BundleRoundTripTests` | Existing | planned |

**Commands:**

- The compiler-proof capture in `docs/delivery-workflow.md#reproducible-local-verification`, up to and including the `verify-policy.binlog` build -- expected: five logs, zero warnings and errors; the bundle regenerates.
- `dotnet run --file tools/anvil/VerifyPolicy.cs -- --maintainability --project-root "$PWD"` -- expected: PASS with both canary `PASS` lines and the `Coupling dependencies:` summary.
- The registration check in `docs/delivery-workflow.md#new-package-registration-check` -- expected: the baseline's total + 0, 0 failed, 0 skipped.
- The AC2 comparison of `bundle-baseline.json` with the candidate's bundle, by decoded entry -- expected: every entry equal.
- `dotnet test CuriousContraptions.slnx -c Debug -f net10.0 --no-build --no-restore --nologo -noAutoResponse --filter "FullyQualifiedName~VisibleSolidColliderTests|FullyQualifiedName~Tennis|FullyQualifiedName~Fan|FullyQualifiedName~WindmillAdmissionTests|FullyQualifiedName~PackageRuntimeParityTests|FullyQualifiedName~F32BehaviourIdentityTests|FullyQualifiedName~BundleRoundTripTests"` -- expected: 0 failed, 0 skipped; every test in the table appears.
- `git add -N` this contract and its run log if untracked, then `git diff --check <baseline_revision>` -- expected: no output.
- One full local round under `caffeinate -i` -- expected: baseline total + 2, 0 failed, `Skipped: 0` for every project.
- At the candidate commit, the unmodified `.githooks/pre-commit` -- expected: pass.

**Manual checks (if no CLI):**

- Open the scene: `Visual/Seam` draws the band; no `col_` node is added.

### Running beside other work

It runs after parts A and B and before part C, and is serialised with them on `VisibleSolidColliderTests.Specs`: one publisher at a time; the later merges main once and repeats the full local round on the merged candidate. It is also serialised with the Baseball repair, which adds the `baseball` row beside this one: one publisher at a time, in either order. No content depends on the other: the Tennis ball keeps its one sphere collider, so the Baseball repair's Tennis comparisons (R3 to R5) and part A's Tennis probes (the anchor-eye control, the re-authored hub test and the Fan stream theory) see the same ball as on main, and no `BaseballTests` edit follows from this part. It shares `TODO.md` and `docs/planning/invest/delivery-batches.md` with those slices and with S3, S5, S5b and S6–S6g, the other slices allowed before the engine programme (owner decisions 22 and 24h). It does not touch `content/puzzles.json`, the test project file, `deferred-work.md` or any registration table.
