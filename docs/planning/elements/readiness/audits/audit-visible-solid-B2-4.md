# Planning audit 4: ELEMENTS-VISIBLE-SOLID-REMAINDER part B2 (Tennis ball, flush print)

Contract: `_bmad-output/implementation-artifacts/spec-elements-visible-solid-remainder-b2.md` (2,726 words), rewritten as a flush-print repair under owner decision 24i. Checkout `spec-drafts`, HEAD `25df3cd5`, product and test paths identical to `25df3cd5`. Binding decisions: `docs/planning/elements/readiness/owner-decisions-2026-11-22.md`. Phase: planning; read-only; nothing built or run. Audited independently and as one serial set with parts A, B, C and D.

## Verdict: Scoped Pass

No finding is inside `<intent-contract>`; the intent can freeze. Ready for development once part B is published and the `Specs` anchor is re-read there (its own precondition, line 49). Apply N1 before dispatch; no re-audit needed.

## Earlier findings (audit 3) and owner decision 24i

Audit 3 judged the radius-bound torus design. Owner decision 24i ("the Tennis ball's seam is printed flush with no collider; repair part B2 becomes a flush-print repair (no radius-bound torus, no matcher changes, no fan-shadowing consequence)") replaced it, so F1 (Never clause against S1/S2), F2 (contact-spin edit decided by outcome), R1 (part A's Tennis probes) and R4 (airflow loss) have no subject: the contract has no matcher change, no collider and no test edit, and the Tennis ball keeps one sphere. R2 (precondition) is at line 49 without the withdrawn entry; `deferred-work.md` marks `airflow-receiver-self-shadowing` withdrawn (23 November 2026) and nothing cites it. R3 (stale lines) is applied. Conformance with 24i: the intent states print-flush, one collider, no radius-bound torus, no matcher change, and the Never clause forbids all three.

## What was re-verified at `25df3cd5`

- `tennis.tscn`: five sub-resources, `load_steps=6`; `Seam` 16–19 (`TorusMesh` 0.245/0.253, 3 mm proud); `Visual/Seam` 31–33 (45° about X); one `col_0_0` sphere. `TennisBallElementDeclaration.cs:18` (radius 1/16 to 16, default 0.25), `:24` (`Visual` uniform scale 4 · radius), three collider bindings.
- The extractor bakes only `CollisionShape3D` and `col_` nodes (`SceneGeometryExtractor.cs:23–25`), so the band yields no collider; AC2 (every entry equal) follows.
- Band geometry: a `CylinderMesh` of radius 0.2504, height 0.008, both caps off, default 64 radial segments, has every vertex at 0.2504 from the axis, so 0.2504 to 0.250432 m from the centre (0.40–0.43 mm proud, inside `FaceMarkingTests.cs:60`'s 0.5 mm); its nearest facet point is 0.2504 · cos(π/64) = 0.250099 m, outside the felt's sphere; 0.2503 would dip 1.5 µm inside (D2 checked). The felt's own faceting (32 rings) sags 0.48 % of r, so the seam reads flush at every size.
- Matcher: the felt compares exactly at both bounds (scale 0.25 and 64, sphere 0.0625 and 16, collider bound ×1); the seam is decorative, which the classification check (`VisibleSolidColliderTests.cs:193–195`) accepts without shape; no `Fit` and no matcher change needed. The row sits after `bowling` (`:110`); the Baseball repair's row goes after `ball` (`:109`).
- T3: `TennisBallTests.ConfiguredRadiusChangesContactSpinAtFixedMass` cases 0.125 and 0.5 (`:135–149`), bounds [0.68, 0.75], unaffected by a non-colliding seam.
- Readers of the seam: only `TennisArtTests.cs:25` (non-null mesh); `BaseballTests.cs:139` reads the Baseball's own seams.
- Registration filter (`docs/delivery-workflow.md`, new-package registration check) selects neither `VisibleSolidColliderTests` nor `TennisArtTests`, so AC4's "+0" there holds; AC4 full round +2 (one theory case, one fact).
- Capability probe: four present, each symbol and test exists (`TennisArtTests.NativeFeltAndSeamScaleTogetherWithTheConfiguredPhysicalSphere`, the `delay` and `reverse_transmission` decorative rows, `FaceMarkingTests.EveryPrintedSurfaceIsPresentFrontFacingAndAttachedToTheRotatingFace`).
- Tier: engine, because the candidate carries `TODO.md` (protocol, Review tiers), as the Baseball repair records.

## Non-blocking corrections (apply before dispatch, no re-audit)

### N1. Frontmatter baseline

- Where: line 6 `baseline_revision: '25df3cd5…'` against line 49 (baseline is the main containing the planning work and part B).
- Fix: delete the line; the coordinator captures it on part B's published main.
- Classification: wrong specified behaviour (frontmatter).

Also update line 51 to cite this audit.

## Serialisation

After parts A and B, before part C; with the Baseball repair one publisher at a time in either order, no content dependency (the Baseball repair's R3–R5 compare against the unchanged sphere; its contract says the same at its line 158). Part A's Tennis probes see the same ball. Shares `TODO.md` and `delivery-batches.md` with S3, S5, S5b, S6–S6f; does not touch `deferred-work.md`.

## Decisions for the owner

None. Audit 3's surfaced look-and-airflow question is settled by owner decision 24i.

## Evidence limits

Nothing was built or run. The band's vertex distances assume Godot's `CylinderMesh` places every side vertex on the stated radius (as its generator does); T1 measures it.
