---
title: 'ELEMENTS-VISIBLE-SOLID-REMAINDER, part B2: Tennis ball seam printed flush'
type: 'bugfix'
created: '2026-11-22'
status: 'done'
baseline_revision: '466ff4ca360c6ce0879f053f6c646f244bfd7bd5'
review_loop_iteration: 0
followup_review_recommended: false
context:
  - '{project-root}/docs/testing-guidance.md'
  - '{project-root}/docs/delivery-workflow.md'
  - '{project-root}/_bmad/custom/element-free-engine-design.md'
warnings: []
deferred:
  - summary: >-
      spec-engine-art-solidity.md cites VisibleSolidColliderTests.cs lines (:249, :241, :194) that already named unrelated code at the baseline.
    evidence: |-
      Review pass 3, Element-Free Engine auditor observation 3; pre-existing, outside the repair diff, breaks no hard rule (Re-review scope). Already owned by the part A carry-forward, deferred-work.md:364 (each slice re-reads its cited lines at dispatch).
    location: >-
      _bmad-output/implementation-artifacts/spec-engine-art-solidity.md
    severity: low
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

Delivery shape: one small product repair over one package (a single element repair; not a Family slice or closure review), split from part B (`spec-elements-visible-solid-remainder-b.md`). The repair story runs in the order A, B, B2, C. Review tier: **engine** (coordinator, at dispatch): every capability is present and the scene, table row, tests, element documents and `TODO.md` are on the data-only allow-list (`_bmad/custom/bmad-loop-protocol.md:54–60`), but the candidate also carries the declaration-`Colliders` deletion slice's run log (its publication receipt), which is not this slice's own record, and in doubt a slice is engine tier (protocol, Review tiers). No new source file and no test-project edit: `CuriousContraptions.tests.csproj:73` compiles every top-level `*.cs` (S2).

**When.** Before the engine programme: repair parts A, B, B2 and C and the Baseball repair are among the slices owner decision 22 (23 November 2026) allows before it. This part follows part B (`c3456907`) and the declaration-`Colliders` deletion (`466ff4ca`), both published, and is replanned from `466ff4ca`.

**Baseline.** `466ff4ca360c6ce0879f053f6c646f244bfd7bd5` (frontmatter), the published main containing the planning work of 22–23 November 2026, parts A and B and the declaration-`Colliders` deletion, captured at dispatch (coordinator decision 38); every scope guard and `git diff` in this contract compares against it. Drafted on `25df3cd5`; every line cited here was re-read at the baseline by the planning scout (run log E2) and the dispatch re-audit. Baseline totals: full local round 7415/0/0, registration 829 (`TODO.md`); expected totals are baseline + delta.

Planning verdict: Scoped Pass, the dispatch re-audit at the baseline (`docs/planning/elements/readiness/audits/audit-visible-solid-B2-5.md`); its N1–N6 applied and C1 ruled, no re-audit needed (run log E4). Earlier: Scoped Pass by the fourth audit at `25df3cd5` (`audit-visible-solid-B2-4.md`); the third audit judged the torus design that owner decision 24i withdrew. Planning audits ran no tests; implementation results are in run log E5 onward.

Implementation owner: agent `a53535a1780135c53` (fresh, no inherited history; checkout `.claude/worktrees/slice-visible-solid-b2`; run log E5).

Owned scope:

- `elements/TennisBall/tennis.tscn` -- the `Seam` sub-resource at 16–19 (today a `TorusMesh`, inner 0.245, outer 0.253) becomes the band below; `Visual/Seam` at 31–33 keeps its name, quaternion and mesh reference. `load_steps` stays 6 (five sub-resources before and after).
- `CuriousContraptions.tests/VisibleSolidColliderTests.cs` -- one `Specs` row, after the `bowling` row (`:128`).
- `CuriousContraptions.tests/TennisArtTests.cs` -- one new fact (T1, T2).
- Records: `docs/planning/elements/CAT-064-tennis.md` (`:3`, `:48`, `:50`, `:98`, `:108`, conformance point 3, and the stale citations of run log E2 and E4); `docs/planning/elements/EL-189-tennis-ball.md` (`:16`, `:53`, `:55`, `:124`, `:134`, `:147`, `:150`, and the stale citations of E2); `docs/planning/elements/EL-092-toy-rocket.md:36` (the Tennis ball's collider count, false since owner decision 24i; run log E6); `docs/planning/elements/readiness/batch-m.md:40` (the EL-189 row) and `_bmad-output/implementation-artifacts/spec-engine-art-solidity.md:92` (its `tennis` survey row), both stale once the seam is flush (review pass 1); `docs/planning/elements/readiness/batch-d.md:17` (the CAT-064 row); `docs/planning/elements/readiness/completion-ledger.md:318` (XB-04: its `VisibleSolidColliderTests.cs` citations moved by the new row, and the ruling of E4); `_bmad-output/implementation-artifacts/deferred-work.md` (one line, `:377`, after the delete-declaration-colliders carry-forward entry `:376`, naming the citations the new row moves; E4); `docs/planning/invest/delivery-batches.md` section 5 (`:248`, part B2's row); `TODO.md`; `docs/planning/elements/readiness/audits/audit-visible-solid-B2-5.md`; this contract and its run log; `_bmad-output/implementation-artifacts/run-logs/spec-delete-declaration-colliders.md` (E30, that slice's publication receipt, written by the coordinator).

Not changed: `elements/TennisBall/TennisBallElementDeclaration.cs`, every existing test and shared test helper (the stale comments XB-04 names stay, routed to part C, E4), `content/puzzles.json`. `elements/elements.bundle.json` is build output, not committed (`.gitignore:17`); task 0 captures a baseline copy.

### Capability probe

Five present; none untested, narrower or missing (re-checked at the baseline, run log E2).

- **Present.** Art that is not a collider: the extractor bakes only `CollisionShape3D` and `col_` nodes (`tools/BundleElements/SceneGeometryExtractor.cs:27–28`; `Hollow`, the only reader of `cap_top`, `cap_bottom` and `wall_thickness`, runs only on those proxies, `:59–64`, `:113`, `:197–236`), and tube mouths come only from `Loc_Port_` locators (`Mouths`, `:69–83`), so `Visual/Seam` yields no collider, mouth or other bundle data whatever its mesh. Test: `TennisArtTests.NativeFeltAndSeamScaleTogetherWithTheConfiguredPhysicalSphere` (`:34`, one collider for the ball's body).
- **Present.** Art classified as print in the visible-solid table: the `Decorative` list of `ArtSpec` ("printing flat on a face", `VisibleSolidColliderTests.cs:55–56`), checked for classification only (`:245–246`). Test: `VisibleSolidColliderTests.EverySolidArtPartHasItsColliderAndNoColliderReachesBeyondTheArt` (the `delay` and `reverse_transmission` rows' decorative entries).
- **Present.** Seam and felt scaled together by one presentation binding on `Visual` (`TennisBallElementDeclaration.cs:24`, UniformScale 4 · radius). Test: `TennisArtTests.NativeFeltAndSeamScaleTogetherWithTheConfiguredPhysicalSphere`.
- **Present.** The delivered print bound, 0 to 0.5 mm in front of the surface, never buried: `FaceMarkingTests.cs:60`. Test: `FaceMarkingTests.EveryPrintedSurfaceIsPresentFrontFacingAndAttachedToTheRotatingFace`.
- **Present.** A scene node's mesh vertices in the scene frame: `VisibleSolidColliderTests.VerticesInRoot` (`:908`, `internal static`; composes node transforms to the scene root and applies no presentation binding, so it is exact at the default radius, where `Visual` is unscaled). Tests: `LinearPusherArtTests.cs:91` and `ConveyorTests.cs:610`, through `AssertPrintedFlat` (`:935`). Used unchanged.

### Repair

| Mesh | Today | Repair |
|---|---|---|
| `Seam` sub-resource, drawn by `Visual/Seam` (quaternion (0.38268343, 0, 0, 0.9238795), 45° about X) | `TorusMesh`, inner 0.245, outer 0.253: 3 mm proud of the 0.25 m felt, no collider | `CylinderMesh`, material `Cream`, `top_radius = 0.2504`, `bottom_radius = 0.2504`, `height = 0.008`, `cap_top = false`, `cap_bottom = false`, default 64 radial segments: an 8 mm open band on the same great circle (the cylinder's axis is local Y, as the torus's was) |

Numbers, at the default radius (Visual scale 1). A band vertex at height y lies √(0.2504² + y²) from the centre, y within ±0.004: 0.2504 to 0.250432 m, so 0.40 to 0.43 mm outside the felt, inside the 0.5 mm bound. Between vertices a facet comes nearest the centre at 0.2504 · cos(π/64) = 0.250098 m, so no facet dips inside the felt's sphere, and none inside the felt mesh, which is inscribed in it. At radius r the whole `Visual` node is scaled by 4r, so the seam stands proud by 0.16 to 0.17 % of r at every size (0.1 mm at 1/16 m, 27.6 mm at 16 m), under the felt mesh's own faceting at the same size (its combined sag at a facet centre is about 0.23 % of r; audit 5, N3). The 0.5 mm bound is the bound at the drawn default size, the one size at which the print contract's metre-scale bound applies; the scale relation is proven by the existing scale test.

`Specs` row (solid; hollow; arc; decorative; moving roots): `tennis`: `["Visual/Felt"]`; none; none; `["Visual/Seam"]`, its comment citing owner decision 24i and, for the planned Pool ball's stripe, owner decision 11 ("printed flush with no collider"); none. At the 16 m bound the felt compares exactly (0.25 × 64 and 16 × 1 are exact in f32), so the row needs no `Fit` and no matcher change.

### Decisions

Owner decision 24i (23 November 2026): the Tennis ball's seam is printed flush with no collider, like the Pool ball's stripe (owner decision 11); part B2 is a flush-print repair, with no radius-bound torus, no matcher change and no fan-shadowing consequence.

Decided here, with reason:

- **D1. An open band, not a thinner torus.** Every vertex of a band lies within the print bound, as `FaceMarkingTests` requires of print; a torus always has vertices deep inside the felt.
- **D2. 0.4 mm stand-off.** The least radius whose facets stay outside the felt's sphere with 64 segments (0.2503 would dip 1.5 µm inside); its edges stay under 0.5 mm.
- **D3. The seam keeps its width (8 mm) and great circle,** so the ball reads as before at a glance, now flush.

No owner decision is open.

## Tasks & Acceptance

**Execution:**

0. Before any edit -- build `tools/BundleElements/BundleElements.csproj -c Debug` at the baseline and copy the generated `elements/elements.bundle.json` (ignored build output) to `.tools/visible-solid-remainder-b2-evidence/bundle-baseline.json` -- AC2's baseline.
1. `CuriousContraptions.tests/TennisArtTests.cs` -- add the fact of T1 and T2, its vertex-distance assertion first (acceptance-to-test table), and run it on the baseline: it fails on that assertion (the torus's outer vertices 3 mm proud, its inner ones buried), not on the mesh type -- red before green.
2. `elements/TennisBall/tennis.tscn` -- replace the `Seam` sub-resource with the band of the repair table -- T1.
3. `CuriousContraptions.tests/VisibleSolidColliderTests.cs` -- add the `tennis` row -- T4.
4. `CAT-064-tennis.md`, `EL-189-tennis-ball.md`, `delivery-batches.md`, `TODO.md` -- refresh the two element documents' stale citations listed in run log E2 (with E4's correction), each checked at its baseline line, and record the seam as delivered flush (radius 0.2504, height 0.008, 0.40–0.43 mm proud at the default size, the same fraction of the radius at every size), no collider, part B2's row delivered and part C queued, on every record line the Code Map names (`batch-d.md:17` included); in `completion-ledger.md:318` (XB-04) refresh the `VisibleSolidColliderTests.cs` lines the new row moves and record the E4 ruling (those comments go to part C); in `deferred-work.md`, after the carry-forward entry `:376`, add one line (`:377`) naming the `VisibleSolidColliderTests.cs` citations after `:128` that the new row moves (owner: each slice at dispatch).

**Acceptance Criteria:**

- AC1: Given the Tennis ball, when the visible-solid theory runs, then its row passes at the default radius and at both radius bounds, and the theory has one more case than at the baseline.
- AC2: Given the baseline copy and the candidate's bundle, when each entry is compared by decoded value, then every entry, `tennis` included, is equal: one sphere collider and three collider bindings.
- AC3: Given the named existing tests (verification list), when they run, then all pass with no edit.
- AC4: Given the candidate, when the full local round runs, then every project reports zero failed and `Skipped: 0`, and the total is the baseline total + 2 (one theory case, one fact). The registration check's total is the baseline's + 0 (its filter selects neither class).
- AC5: Given the candidate diff, when its paths are listed, then none is under `engine/`, `CuriousContraptions.Simulation/`, `CuriousContraptions.Bundle/`, `CuriousContraptions.Abstractions/`, `schemas/`, `tools/`, and `elements/TennisBall/TennisBallElementDeclaration.cs` is unchanged.

Counts in AC1 and AC4 are baseline + delta, counted on the slice's baseline at start, so the order in which this part, part C and the Baseball repair publish does not change them.

## Spec Change Log

## Review Triage Log

### 2026-11-25 — Review pass 1 (engine tier, all six layers; candidate uncommitted on `466ff4ca`; run log E7)
- verdicts: 23 findings — high 0, medium 0, low 15, false 8, maybe-false 0
- findings:
  - `low` `patch` BH1: the new comments at `TennisArtTests.cs:47` and `VisibleSolidColliderTests.cs:129` cite "the Pool ball's stripe" as if delivered; the only delivered ball stripe (`ball`, Hollow) is the opposite — reword to cite owner decisions 24i and 11 (the planned Pool ball, EL-098). Its second part, the pre-existing `ball` row comment `:126`, is existing test code (Never clause) already routed to part C by XB-04 (ruling 2): routed, not patched here.
  - `low` `patch` ECH1: a zero-height or vanishing band passes every assertion of the new fact (an invisible seam) — assert the band's drawn width is the seam's authored 8 mm (D3).
  - `low` `patch` VG-O1: same root as BH1 (grouped).
  - `low` `reject` VG-O2: the fact's collider loop overlaps the scale test — T2 requires one sphere at the default and both bounds and the kind check is new; removing it would drop a contract row.
  - `low` `patch` IA1 (with EFE evidence limit): the fact does not check the T1 row's "on the seam's great circle" — assert every band vertex lies within half the band height of the 45°-about-X great-circle plane.
  - `false` `reject` IA2: no "printed flush" table category — the `Decorative` list is defined as including "printing flat on a face" (`VisibleSolidColliderTests.cs:55–56`), as the contract specifies.
  - `false` `reject` IA3: flushness measured at the default radius only — the intent's Always clause sets the 0.5 mm bound at the default size and proportion elsewhere, proven by the existing scale test.
  - `false` `reject` IA4: whole-entry equality not in the fact — AC2's decoded comparison ran (`{"sameIds":true,"unequal":[]}`, run log E5).
  - `false` `reject` IA5: a straight band, not a spherical zone — the intent's open band, inside its stated bounds.
  - `low` `patch` IA6: Pool-ball precedent — grouped with BH1.
  - `false` `reject` IA7: T3/T5 execution not visible — they ran in the full round 7417/0/0 and the scoped trx (run log E5; confirmed by the Element-Free auditor).
  - `false` `reject` IA8: print relief and facing not applied — the intent cites only the 0.5 mm bound and adds the facet rule.
  - `false` `reject` IA9: exact equalities — the palette colour and the ×1 radius pass-through are exact by contract.
  - `low` `patch` FO-a: `batch-m.md:40` (EL-189) still says B2 "may run before it" and "Conforms with fixes" — restate as delivered (path added to owned scope).
  - `low` `patch` FO-b: `spec-engine-art-solidity.md:92` still surveys the seam as "a torus 3 mm proud" — mark the row repaired by part B2 (path added to owned scope).
  - `low` `patch` EFE-R1: `CAT-064-tennis.md:5` and `EL-189-tennis-ball.md:7` anchor citations to checkout `8d1d68aa`, where the refreshed lines are false — re-anchor to part B2's commit.
  - `low` `patch` EFE-R2a: `batch-d.md:17` Prerequisites still says "part B2 after part B".
  - `false` `reject` EFE-R2b: `delivery-batches.md:246–247` say B2 stays queued — those are parts A and B's delivery records; B2's current state is its own row `:248` and `TODO.md`.
  - `low` `patch` EFE-R3: `completion-ledger.md:227` names audit B2-2 as current — name audit 5.
  - `low` `patch` EFE-R4: `deferred-work.md:377` count (137 in 100) has no reproducible command, and "the Tennis ball's two element documents … were refreshed" has nothing to refresh — state the command and its count; drop the claim.
  - `low` `patch` EFE-R5: contract `:51` "no test has run" and Design Note 8 "unexecuted tests" are stale (coordinator corrected both); run log E5 says E8 not triggered — owner appends the correction.
  - `low` `patch` EFE-R6: records cite `.tools/` files (ignored) as evidence — state the results inline as the record and label `.tools/` files as transient review inputs (`role-cards.md:15`); no further `cp`.
  - `low` `patch` EFE evidence limit (orientation): grouped with IA1.

### 2026-11-25 — Review pass 2 (re-review of repair 1: Blind Hunter, Edge Case Hunter, Verification Gap, Forward-Only, Element-Free Engine; run log E9)
- verdicts: 8 findings — high 0, medium 0, low 7, false 1, maybe-false 0
- findings:
  - `low` `patch` ECH2-1: the new 1 µm tolerance on the great-circle plane check (`TennisArtTests.cs:77–83`) is justified by f32 spacing on transformed vertices, so a sub-micrometre re-authoring no player can see fails — bound the plane and width checks by the 0.5 mm print bound the test already uses.
  - `low` `patch` VG2-O1: same root as ECH2-1 (grouped).
  - `low` `patch` BH2-1: same root as ECH2-1 (grouped).
  - `low` `patch` BH2-2: the tolerance comment names "a band turned 1° off its great circle", but a tilted band through the centre is still on a great circle; the check excludes a seam drawn off its authored 45° great circle — correct the comment.
  - `false` `reject` FO2-O1: `CAT-064-tennis.md:50` and `EL-189-tennis-ball.md:55` say "like the Pool ball's stripe" — they restate owner decision 24i's own words, and owner decision 11 does rule that stripe printed flush (`owner-decisions-2026-11-22.md:23`, `:62`); true as a decision record.
  - `low` `patch` EFE2-F1: `completion-ledger.md:227` status cell still says "`draft`; Scoped Pass after repair (:47)" — state the contract's status at the candidate commit (`done`, set at Finalize) and point at the planning verdict `:51`.
  - `low` `patch` EFE2-F2: `deferred-work.md:377` counts only the `VisibleSolidColliderTests.cs:N` form — say so and name the continuation (`, :N`) and range-end forms that also move.
  - `low` `patch` EFE2-F3: contract Code Map `:82` still quotes the pre-repair comment, and the Code Map and task 4 point at `deferred-work.md:376` for the line at `:377` — corrected by the coordinator in this pass.

### 2026-11-25 — Review pass 3 (re-review of repair 2: Blind Hunter, Edge Case Hunter, Verification Gap, Forward-Only, Element-Free Engine; Re-review scope applies; run log E11)
- verdicts: 4 findings — high 0, medium 0, low 2, false 2, maybe-false 0
- findings:
  - `false` `reject` BH3-1: the width and great-circle checks reuse the 0.5 mm print stand-off, not a lateral bound — both check authored scene values (D3's 8 mm width, the intent's 45°-about-X great circle), which the testing guidance allows exactly; the 0.5 mm window is looser than exact, still fails the named defects, and has 0.5 mm margin on the authored scene (Verification Gap and Element-Free auditor agree).
  - `false` `reject` EFE3-O1: `completion-ledger.md:227` says `done` while the contract is `in-review` — Finalize sets `done` in the same candidate commit.
  - `low` `patch` EFE3-O2: the Verification executor line names only the implementation owner, but the coordinator ran the final capture, gate and round — line corrected by the coordinator.
  - `low` `defer` EFE3-O3: `spec-engine-art-solidity.md` cites `VisibleSolidColliderTests.cs:249`, `:241`, `:194`, already stale at the baseline — outside the repair, no rule broken (Re-review scope); owned by the part A carry-forward (`deferred-work.md:364`); in `deferred:`.

## Design Notes

1. **Behaviour delivered.** The Tennis ball's seam is printed flush; its only solid is its sphere; the package joins the visible-solid proof. Source: owner rule "visible = solid" (25 October 2026) and owner decision 24i (23 November 2026).
2. **Reusable.** Nothing new: the decorative classification and the print bound exist.
3. **Declared.** One mesh resource in the scene. No declaration or engine identity changes.
4. **Execution.** Scene → `SceneGeometryExtractor.Bake` → the unchanged bundle entry → the unchanged compiler and solver.
5. **Ownership.** No new collider, body or state. Reset, save and load are unaffected.
6. **Generality.** Not an engine change.
7. **Deleted.** The proud torus seam. No raised copy remains.
8. **Uncertain.** None; the planned tests ran (run log E5). Rules E1, E6, E7, E8 (scope, review and deferred browser qualification) and E9 apply; E2–E5 and D1–D7 are not engaged (no engine, schema, lifecycle or storage change).

## Verification

Executor for every command: the implementation owner, except the final candidate's capture builds, maintainability gate and full local round, which the coordinator ran as designated executor after review (run log E11). Raw logs go under `~/.local/share/tim-toolchain/logs/slice-visible-solid-b2/`; `.tools/visible-solid-remainder-b2-evidence/` holds transient review inputs (ignored, gone with the worktree; `role-cards.md:15`), and the run log states each result inline as the record.

Environment: the pinned toolchain first on `PATH`: `PATH=$HOME/.local/share/tim-toolchain/dotnet-10.0.403:$HOME/.local/share/tim-toolchain/node-v26.12.1-darwin-arm64/bin:$PATH` and `DOTNET_ROOT=$HOME/.local/share/tim-toolchain/dotnet-10.0.403` (on this host the default `PATH` has Homebrew node v26.13.1 and a dotnet without `wasm-tools`, so the capture's node check and the web build fail otherwise); record the session environment check in the run log.

### Acceptance-to-test table

| Rows | Named discriminating test | Control and boundary | Run |
|---|---|---|---|
| T1, T2 | `TennisArtTests.TheSeamIsPrintedFlushOnTheFeltAndTheBallHasOnlyItsSphereCollider` (new fact). From the part made by `PartRegistry`, at the default radius and in this order: first, every vertex of the seam's `Mesh.GetFaces()`, placed in the scene frame by `VisibleSolidColliderTests.VerticesInRoot`, lies at a distance from the felt's centre in [r, r + 0.0005] m, r the felt's radius read from its `SphereMesh` (the red on the baseline fails here); then `Visual/Seam` is a `CylinderMesh` with both caps off and the scene's `Cream` seam material; then `min(TopRadius, BottomRadius) · cos(π / RadialSegments)`, scaled to the part root, is at least r (no facet inside the felt); then the band's drawn width (`Height` × its scale in the part frame) is the authored 8 mm within the 0.5 mm print bound, and every vertex lies within half that width plus the print bound of the great-circle plane through the felt's centre whose normal is Y turned 45° about X, derived from the angle. The `tennis` ball compiled by `WorkshopPhysicsCompiler` has exactly one collider on its body, a sphere of the ball's radius, at the default radius and at both radius bounds | Controls by reading: today's torus fails both (outer 0.253 is 3 mm proud; its inner vertices are buried); a band of radius 0.25 fails the facet check. Boundary: the band's edge vertices, 0.43 mm out, nearest the bound. 0.5 mm is the print bound `FaceMarkingTests.cs:60` uses for the same quantity | Red on the baseline scene at the vertex assertion; passed in the scoped run and the full round (run log E5); with the width and great-circle assertions, passed in the repair's scoped run (run log E8), and again with both bounded by the print bound (run log E10) |
| T4, AC1 | `VisibleSolidColliderTests.EverySolidArtPartHasItsColliderAndNoColliderReachesBeyondTheArt`, case `tennis` (default, 1/16 m and 16 m) | Without the row the package has no case; with the seam left unclassified it fails "art is not classified once"; a seam collider would fail "collider(s) reach beyond the art" | Passed in the scoped run and the full round, which has one more case (run log E5); passed again after the comment rewording (run log E8) |
| T2, AC2 | The AC2 comparison (command below) | The baseline entry is the control: equal | `{"sameIds":true,"unequal":[]}`, stated inline as the record (run log E5) |
| T3 | `TennisBallTests.ConfiguredRadiusChangesContactSpinAtFixedMass` (existing, both cases unedited): speed and rim speed in [0.68, 0.75] m/s, a solid sphere's sticking roll. The 0.125 m case rolls about 79° in its 30 ticks, so a proud seam collider would meet the bench inside it | Existing bounds | Both cases passed in the scoped run and the full round (run log E5) |
| T5, AC3 | Existing, unedited: `TennisArtTests.NativeFeltAndSeamScaleTogetherWithTheConfiguredPhysicalSphere`, `TennisBallTests` (all), `TennisInteractionTests` (3), `FanIntegrationTests`, `FanAdmissionTests`, `FanGuidanceTests`, `WindmillAdmissionTests`, `PackageRuntimeParityTests`, `F32BehaviourIdentityTests`, `BundleRoundTripTests` | Existing | All passed in the scoped run (`BundleRoundTripTests` in its own trx) and the full round 7417/0/0 (run log E5) |
| AC4 | The full local round and the registration check | Baseline totals 7415 and 829 | 7417 = 7415 + 2, 0 failed, `Skipped: 0` in every project; registration 829 = baseline + 0 (run log E5) |
| AC5 | `git diff --name-only 466ff4ca` filtered for `engine/`, `CuriousContraptions.{Simulation,Bundle,Abstractions}/`, `schemas/`, `tools/`; `git diff --quiet 466ff4ca -- elements/TennisBall/TennisBallElementDeclaration.cs` | Any listed path or a declaration diff fails | No path listed; declaration unchanged (run log E5) |

**Commands:**

- The compiler-proof capture in `docs/delivery-workflow.md#reproducible-local-verification`, up to and including the `verify-policy.binlog` build -- expected: five logs, zero warnings and errors; the bundle regenerates.
- `dotnet run --file tools/anvil/VerifyPolicy.cs -- --maintainability --project-root "$PWD"` -- expected: PASS with both canary `PASS` lines and the `Coupling dependencies:` summary.
- The registration check in `docs/delivery-workflow.md#new-package-registration-check` -- expected: the baseline's total + 0, 0 failed, 0 skipped.
- The AC2 comparison, decoded per entry (`==` on parsed JSON values, not bytes), from the checkout root: `jq -n --slurpfile a .tools/visible-solid-remainder-b2-evidence/bundle-baseline.json --slurpfile b elements/elements.bundle.json '($a[0].Elements|map({(.Metadata.Id):.})|add) as $A | ($b[0].Elements|map({(.Metadata.Id):.})|add) as $B | {sameIds:(($A|keys)==($B|keys)), unequal:[$A|keys[]|select($A[.]!=$B[.])]}'`, output kept under `.tools/visible-solid-remainder-b2-evidence/` -- expected: `{"sameIds":true,"unequal":[]}`.
- `dotnet test CuriousContraptions.slnx -c Debug -f net10.0 --no-build --no-restore --nologo -noAutoResponse --logger "trx;LogFileName=b2-scoped.trx" --filter "FullyQualifiedName~VisibleSolidColliderTests|FullyQualifiedName~Tennis|FullyQualifiedName~Fan|FullyQualifiedName~WindmillAdmissionTests|FullyQualifiedName~PackageRuntimeParityTests|FullyQualifiedName~F32BehaviourIdentityTests|FullyQualifiedName~BundleRoundTripTests"` -- expected: 0 failed, 0 skipped; every test in the table appears.
- `git add -N` this contract and its run log if untracked, then `git diff --check <baseline_revision>` -- expected: no output.
- One full local round under `caffeinate -i` -- expected: baseline total + 2, 0 failed, `Skipped: 0` for every project.
- At the candidate commit, the unmodified `.githooks/pre-commit` -- expected: pass.

**Manual checks (if no CLI):**

- Open the scene: `Visual/Seam` draws the band; no `col_` node is added.

### Running beside other work

It runs after parts A and B and before part C, and is serialised with them on `VisibleSolidColliderTests.Specs`: one publisher at a time; the later merges main once and repeats the full local round on the merged candidate. It is also serialised with the Baseball repair, which adds the `baseball` row beside this one: one publisher at a time, in either order. No content depends on the other: the Tennis ball keeps its one sphere collider, so the Baseball repair's Tennis comparisons (R3 to R5) and part A's Tennis probes (the anchor-eye control, the re-authored hub test and the Fan stream theory) see the same ball as on main, and no `BaseballTests` edit follows from this part. It shares `TODO.md` and `docs/planning/invest/delivery-batches.md` with those slices and with S3, S5, S5b and S6–S6g, the other slices allowed before the engine programme (owner decisions 22 and 24h). It does not touch `content/puzzles.json`, the test project file or any registration table; in `deferred-work.md` it adds one carry-forward line (run log E4, ruling 1).

## Auto Run Result

- **Summary.** The Tennis ball's seam is an open `CylinderMesh` band (radius 0.2504, height 0.008, caps off) printed flush on the felt, 0.40–0.43 mm proud at the default size, with no collider; the ball keeps its one sphere collider and joins the visible-solid table as felt solid, seam decorative (owner decision 24i). Run log E5, E8, E10.
- **Files.** 17 paths, all on the owned scope (Final diff scope, E11): `tennis.tscn` (the band), `TennisArtTests.cs` (one fact), `VisibleSolidColliderTests.cs` (the `tennis` row); records `CAT-064-tennis.md`, `EL-189-tennis-ball.md`, `EL-092-toy-rocket.md`, `batch-d.md`, `batch-m.md`, `completion-ledger.md`, `delivery-batches.md`, `spec-engine-art-solidity.md`, `deferred-work.md`, `TODO.md`, audit 5, the deletion slice's run log (E30 receipt), this contract and its run log.
- **Review.** Engine tier, three passes (E7, E9, E11): pass 1 23 rows (14 low patched, 1 low rejected, 8 false); pass 2 8 rows (7 low patched, 1 false); pass 3 4 rows (1 low patched, 1 low deferred under Re-review scope, 2 false). Patched by verdict: low 22, medium 0, high 0. Forward-Only Pass on every pass; Element-Free Engine Scoped Pass on the final repair. Final diff scope Pass (E11). `followup_review_recommended: false` (no high or medium patched).
- **Verification.** Final candidate, coordinator as executor (E11): capture builds 0 warnings, 0 errors; maintainability gate PASS (both canaries, coupling violations 0); full local round 7417/0/0, `Skipped: 0` (baseline 7415 + 2); registration 829 (E5, + 0); AC2 decoded comparison `{"sameIds":true,"unequal":[]}` (E5); AC5 no forbidden path, declaration unchanged (E5).
- **Residual risks.** Browser qualification pending to the post-catalogue E2E phase; remote build, artifact and deployment not verified. Remaining for the approver: the pre-commit hook at the commit and SnapshotApproval of the committed SHA.
