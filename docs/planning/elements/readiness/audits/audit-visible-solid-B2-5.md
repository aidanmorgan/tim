# Planning audit 5: ELEMENTS-VISIBLE-SOLID-REMAINDER part B2 (Tennis ball, flush print), dispatch re-audit

Contract: `_bmad-output/implementation-artifacts/spec-elements-visible-solid-remainder-b2.md` (2,962 words), uncommitted, with run log entries E2 and E3. Checkout `.claude/worktrees/slice-visible-solid-b2`, HEAD `466ff4ca360c6ce0879f053f6c646f244bfd7bd5` (the contract's baseline). Phase: planning, coordinator decision 38's dispatch re-audit; read-only; nothing built or run. Binding: owner decision 24i (`owner-decisions-2026-11-22.md:62`). Previous: audit 4, Scoped Pass at `25df3cd5`.

## Verdict: Scoped Pass

The design, the probe, the red-before-green step and the counts hold at `466ff4ca`. No finding is inside `<intent-contract>`. Dispatch waits on N1 and N2 (record and owned-scope amendments) and on the coordinator's routing in C1; N3 to N6 are corrections to apply at the same time. None needs a re-audit.

## Re-verified at `466ff4ca`

- Every contract citation holds: `SceneGeometryExtractor.cs:27–28`, `:59–64`, `:69–83`, `:113`, `:197–236`; `VisibleSolidColliderTests.cs:55–56`, `:78`, `:128`, `:245–246`, `:908`, `:935`; `TennisBallElementDeclaration.cs:18`, `:24`; `FaceMarkingTests.cs:60`; `TennisArtTests.cs:34`; `tennis.tscn` 16–19 and 31–33 (`load_steps=6`, five sub-resources); `CuriousContraptions.tests.csproj:73`; `.gitignore:17`; `bmad-loop-protocol.md:54–60`; the capability probe's tests all exist (`FaceMarkingTests.cs:33`, `LinearPusherArtTests.cs:91`, `ConveyorTests.cs:610`).
- E2's element-document corrections are all right, and E2 missed no citation in either document (every remaining `Decl:`, `Scene:` and test-file citation was checked, including `FanGuidanceTests.cs:132`, `CannonElementDeclaration.cs:31` and `TennisInteractionTests.cs:11`, `:30`, `:56`). The exception is N6.
- **(a)** No. `Specs` is read only at `:223` and `:239`. No list, exclusion or coverage guard names `tennis` against the table. `FaceMarkingTests.Parts` (`:9–13`) is a fixed list without it; `tools/e2e` names it only in browser-deferred `UNDELIVERED` lists; `PartRegistryTests.cs:516` scans `PartId.cs` only. No existing test needs editing.
- **(b)** No. `Bake` reads only `CollisionShape3D` and `col_` nodes (`:27–28`). `Hollow` is reached only from `BakeProxy`. `Mouths` and `Locators` read only `Loc_Port_` nodes. `RefuseInfixProxyNames` refuses only `-col_`. `Resources` refuses only a missing id or type or a duplicate id. `BundleGenerator.cs:24–28` and `PackageValidator.cs:65` are the only scene readers. In the table, `Art`/`Primitive` gives the band a Tube shape and checks a decorative entry for classification only. Presentation-only consumers (`PlacementShadows.cs:52`, AABB; `WorkshopGuidance.cs:241`, transparency) do not depend on the mesh type.
- **(c)** Yes. `Listed => new(Specs.Keys)` makes one case per key, and the parameter sets loop inside it (`:241`). This was executed: part A's round was 7309 = 7294 + 15, including five cases for five rows (part A run log, E9). The registration filter (`delivery-workflow.md:69`) selects neither class, so its delta is +0.
- **(d)** Yes.
  - On today's torus (centreline 0.249, tube 0.004), `GetFaces` has vertices at 0.253 (3 mm proud) and at 0.245 (buried). The first assertion fails on the defect.
  - `VerticesInRoot` composes the live node transforms up to the part root, and `Visual`'s scale is 1 at the default radius (`TennisArtTests.cs:36`).
  - Godot's `CylinderMesh` defaults to 64 radial segments and 4 rings. Every side vertex lies on the radius, at heights ±0.004, ±0.0024 and ±0.0008, so vertices are 0.2504 to 0.250432 m from the centre (0.40 to 0.43 mm proud). The nearest facet is at 0.2504 · cos(π/64) = 0.250098 m. 0.2503 would dip 1.5 µm inside (D2).
- **(e)** Yes. The bound reuses the delivered print stand-off ("0.5 mm anti-z-fighting separation, never a buried label", `FaceMarkingTests.cs:59–60`), chosen before observation. Beyond it the seam is a raised drawn solid with no collider. The facet check is an exact geometric contract.
- **(f)** The comparison is decoded (`==` on parsed JSON values) but has no literal program (N4).
- **(g)** Correct, on a changed reason. Audit 4's reason (`TODO.md`) no longer applies: owner decision 24n put `TODO.md` on the allow-list (`:60`). Every other owned path is on the list. The deletion slice's run log is not this slice's own record, so the slice is engine tier when in doubt. The tier is unchanged.
- **(h)** Every path is listed except those in N1 and N2.
- **(i)** All eight design answers are present, consistent with D1–D7, and each is three lines or fewer. No required-now owner decision is open. The contract is within budget.

## Findings

### N1. Owned scope too narrow for the delivered record (wrong specified behaviour; apply before dispatch)

- **Where:** Code Map owned scope and task 4. EL-189 is limited to "stale line citations only"; for CAT-064 only `:48` and "point 3" are named.
- **Defect:** delivery makes these lines false:
  - EL-189 `:16`, `:53` (torus 0.245/0.253, "3 mm proud today"), `:55`, `:124`, `:134` and `:150` (verdict "Conforms with fixes (the flush seam print …)");
  - CAT-064 `:3`, `:50` ("no row in the visible-solid table"), `:98` (residual 1) and `:108` (verdict).

  EL-189 `:147` ("two after the repair, the second because the seam is a drawn solid") is already false under 24i. That makes it a confirmed pre-existing defect under the repair policy. Part B's precedent (`dbaacad1`) also touched readiness rows and the completion ledger. B2's equivalents are `readiness/batch-d.md:17` ("part B2 after part B") and `completion-ledger.md:318` (XB-04 cites `VisibleSolidColliderTests.cs:145` and `:177`, which the new row moves).
- **Fix:** name these lines in owned scope and task 4, and add `batch-d.md` and `completion-ledger.md` (a citation refresh only).

### N2. Moved `VisibleSolidColliderTests.cs` citations (coordinator decision; apply before dispatch)

- **Where:** the row inserted after `:128`. The contract says nothing, and `deferred-work.md` is "Not changed".
- **Defect:** every later line moves. That affects about 40 citations under `docs/planning/elements/` (`:237` in 14 documents, `:806`, `:826`, `:860`, `:1199`), part C's contract (`:279–294`) and XB-04.
- **Fix (default):** add one line to the existing delete-declaration-colliders carry-forward (`deferred-work.md:376`, which already names this file; owner: each slice re-reads at dispatch) naming part B2's row, and put `deferred-work.md` in owned scope. The alternative is to refresh them here, as part A did.

### N3. Felt faceting figure (record correction; non-blocking)

- **Where:** Repair numbers: "its 32 rings alone sag 0.48 % of r".
- **Defect:** 0.48 % is 1 − cos(π/32). That would need a latitude step of 2π/32, but the rings span only π. The ring sag is about 0.12 % at most (1 − cos(π/64)), and the 64 radial segments give the same 0.12 %. The seam's 0.16–0.17 % is under the felt's own faceting only for the combined sag at a facet centre (about 0.23 %). Also, 0.250099 should read 0.250098. Audit 4's line 17 has the same error and is corrected here.
- **Fix:** restate the sentence with the combined figure. No acceptance depends on it.

### N4. AC2 program not literal (evidence; non-blocking)

- **Fix:** state it. The bundle is `{"Elements":[…]}`, keyed by `.Metadata.Id`:

  `jq -n --slurpfile a bundle-baseline.json --slurpfile b elements/elements.bundle.json '($a[0].Elements|map({(.Metadata.Id):.})|add) as $A | ($b[0].Elements|map({(.Metadata.Id):.})|add) as $B | {sameIds:(($A|keys)==($B|keys)), unequal:[$A|keys[]|select($A[.]!=$B[.])]}'`

  Expected: `{"sameIds":true,"unequal":[]}`. The `tennis` entry has one `Sphere`, three `ColliderBindings` and an empty `TubeMouths`.

### N5. Scoped run's "every test appears" (evidence; non-blocking)

- **Defect:** `--list-tests` undercounts results (deferred-work, 204 names).
- **Fix:** add `--logger "trx;LogFileName=b2-scoped.trx"` to the scoped command and check the names from it.

### N6. E2's CAT-064 `:47` correction (record; non-blocking)

- **Defect:** `Scene:7–10` → `Scene:3–5` drops the radius citation.
- **Fix:** cite `Scene:3`–`:5` and `:7`–`:10`.

## Decision for the coordinator

**C1. XB-04 routing.** `completion-ledger.md:318` gives the stale comments in `VisibleSolidColliderTests.cs` (`:87`, `:126`, `:145`, `:177`; E2 noted `:126`) to "the next slice that edits it". B2 edits the file, but its Never clause forbids editing existing test code. The recommended default is to rule that B2 is not that slice and route the comments to part C, which rewrites the matcher in the same file (contract C, tasks 2 and 5), and record this in XB-04. This lies outside the intent. Making B2 fix them instead would be an intent gap (the Never clause) and needs the owner.

## Evidence limits

Nothing was built or run, and planned tests are not proof. Godot's `CylinderMesh` and `SphereMesh` vertex placement is taken from its documented defaults and generator, not executed. T1 measures the band, and N3's exact ring count does not change its conclusion. The bundle's JSON shape was read from the deletion slice's evidence `candidate.bundle.json`, consistent with `BundleSchema.WriteEntries`.
