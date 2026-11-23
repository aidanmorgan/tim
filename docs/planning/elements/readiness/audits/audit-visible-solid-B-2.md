# Planning audit, second pass: ELEMENTS-VISIBLE-SOLID-REMAINDER, part B

Contract: `_bmad-output/implementation-artifacts/spec-elements-visible-solid-remainder-b.md` (4,961 words, read in full as repaired). Checkout `spec-drafts` at `8d1d68aa`. Auditor `a5dde8af9f33ad854`, 22 November 2026. Phase: planning. Nothing was built or run; every test named is planned.

## Verdict: Scoped Pass (planning)

No blocking finding remains. The pass covers the contract's readiness for development only; it is not executed proof of any row.

## First-pass findings, each checked against the repaired text

| Finding | Repair | Result |
|---|---|---|
| F1 visible proxies approved as art | Lines 21–29, 93–101, 149 (K1): the eight drawn proxies become art with separate proxies; the matcher rule, row B10 and its fact are gone; "A visible collider proxy" is in Never; AC5 keeps `DESIGN.md` and `docs/engine-contracts.md` out of the diff; AC6 added | Closed |
| F2 1 mm print bound | Lines 27, 43, 109–119, 151 (K3), 178, 221: layers 0.2 mm and 0.4 mm under the delivered 0.5 mm bound; literal `(.6f, -.1f, .1004f)` | Closed |
| F3 placement consumer | Lines 29, 72, 137–138 | Closed |
| F4 row B4's parenthesis | Lines 38, 162 | Closed |
| F5 AC2 baseline and command | Lines 66, 171, 184, 229 | Closed |
| N1 bore also needs a mouth bore | Line 130 | Applied |
| N2 spanned-art pose source | Line 131 | Applied |
| N3 first-landed-read reasoning and hinge rule | Line 211 | Applied |
| N4 Conveyor puzzles and filter | Lines 73, 222, 231 | Applied |
| N5 citations, resource name, `load_steps` | Lines 70, 83, 109, 221 | Applied |
| Owner item: Conveyor output spoke | Line 109, K2: both spokes are print | Applied |
| Owner item: floating `Latch/Lever` | Line 165 | Applied |

## New text, checked against the code

- **Collider order and equality after separation (B8, AC2).** The extractor takes proxies in file order per body root (`SceneGeometryExtractor.cs:57–68`) and bakes a `BoxShape3D`, `SphereShape3D` or `CylinderShape3D` to the same kind and half-extents as the `BoxMesh`, `SphereMesh` or `CylinderMesh` it replaces (`:122–135`, `:234–246`). With the same transform text, every baseline collider is equal and in place. The sleeve stays a mesh proxy with its wall metadata (`:209`, `:222–228`).
- **Renamed paths.** No declaration binding names a `col_` node (bindings name `Visual/Head`, `Visual/Shaft`, `Visual/Latch`, `Visual/Coil`, the marks and the indicator). No C# under `ui/` or `engine/` reads a `col_` name. The one test reader is `WoundSpringArtTests.cs:41`, which task 7 edits.
- **`load_steps`.** Conveyor 16, pusher 20, spring 22: each agrees with a count of sub-resources after the edits.
- **Drawn bore (H2).** `metadata/wall_thickness` is on hidden `col_` nodes only in every other scene (19 nodes checked), so `Inner` is absent for all existing rows and they are unchanged. For the spring, the art node and the hidden proxy now state the wall separately, so the check is independent.
- **AC6.** At the baseline the command finds 51 `col_` meshes, 43 hidden and the eight drawn ones visible. After the slice the two packages keep two `col_` meshes, both hidden.
- **Counts.** Fourteen markings (two spokes, five travel marks, five stroke marks, index, shaft spoke); total + 9 (two rows, four drops, three facts); 40 rows after part A.
- **Tier, shape, budget.** Engine tier; one repair slice; 11 matrix rows, 6 criteria; within 6,000 words.

## Non-blocking

1. **AC6 is a text listing, not an executed test** (lines 133, 188, 230). It is adequate for a scene-authoring invariant, and nothing loud is lost: no such check exists today. A stronger form costs one line and is package-neutral: in `Art`, where a `col_` mesh is skipped (`VisibleSolidColliderTests.cs:249`), assert that the skipped mesh's own `Visible` is false. It would cover the 40 tabled packages on every run. Optional; if taken, name it in task 2 and in row B8's test cell. The total does not change.
2. **Line 53** should carry this verdict and identifier.

## Decisions for the owner

None open. K1–K4 are defensible from the documents and the game's intent; none cuts scope or waives a gate. The floating-art note (line 165) is correctly recorded as not blocking.

## Evidence limits

- No build, test or bundle generation was run.
- Unexecuted, as the contract's Design Note 8 states: admission of the 0.01 m torus tube on the slider body, and the hinge's axial support in B2. The contract routes a B2 failure back to planning.
- The ten Conveyor puzzles were checked by geometry, not simulation.
- Part B2 (`spec-elements-visible-solid-remainder-b2.md`) was not audited here.
