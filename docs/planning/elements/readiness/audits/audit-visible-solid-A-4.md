# Planning audit 4: ELEMENTS-VISIBLE-SOLID-REMAINDER part A

Contract: `_bmad-output/implementation-artifacts/spec-elements-visible-solid-remainder.md` (4,944 words). Checkout `spec-drafts`, HEAD `25df3cd5`; every product, test, tool, hook and content path is identical to `25df3cd5` (`git diff --quiet 25df3cd5 -- elements CuriousContraptions.tests engine tools CuriousContraptions.Simulation CuriousContraptions.Abstractions CuriousContraptions.Bundle content .githooks`). Binding decisions: `docs/planning/elements/readiness/owner-decisions-2026-11-22.md` (owner 0–24k, coordinator 16, 21, 25–37, 24l). Phase: planning; read-only; nothing built or run. Audited independently and as one serial set with parts B, B2, C and D; earlier audits were read only to check that their findings are resolved. Citation strings being re-pointed from `.tools/audit/` to `docs/planning/elements/readiness/audits/` by another agent were ignored.

## Verdict: Scoped Pass

Ready for development once its own precondition holds (the planning work of 22–23 November published on main, baseline captured there). No finding is inside `<intent-contract>`; the intent can freeze as written. Apply N1–N3 before dispatch; they need no re-audit.

## Earlier blocking findings (audit 3), resolved in substance

- F1 (package count in the Problem): line 21 now reads "At `25df3cd5` twelve delivered packages are outside the visible-solid table (the Baseball repair adds `baseball` separately)". Correct: 45 packages (`BasketballResourceTests.cs:34`), 33 `Specs` rows (`VisibleSolidColliderTests.cs:63–162`).
- R1 (planned pulley lessons): lines 224 and task 7 record `counterweight` and `pulley_depth` with the `pulley-lesson-weight-start` entry. Re-checked in `content/puzzles.json`: solution weights (4 kg) at (2, 5, 0.12) under the pulley at (2, 6, 0) and (2, 5, 2.12) under (2, 6, 2), both `Planned`.
- R2 (hollow kinds): "Footprint, capture and occlusion" added; `HollowSolid.IsHollow` includes `Torus` (`engine/gpu/HollowGeometry.cs:33–34`), construction refusal `engine/gpu/WorkshopPhysicsCompiler.cs:498–523`, placement `engine/PlacementPose.cs:147`, `:113–116`.
- R3 (Windmill consumers): five classes added to A9 and to the filter.
- R4 (controls for A3, A4): present at lines 191–192.
- R5 (baseline precondition): present at line 54.
- R6 (stale lines): `FanArtworkTests` at `FanIntegrationTests.cs:610`, arrow span 0.61 m, AC3 closed allowance replaced by the searched statement.

## What was re-verified at `25df3cd5`

- Scenes: `rope_anchor.tscn` 28–30 (eye torus 0.1725/0.2275, axis Z, z 0.18), 32–34; `pulley.tscn` 58–64, 75–77 (post 0.12 × 0.55 × 0.12 at (0, 0.3, −0.13)), 85–87 (rim 0.365/0.435, 32 × 8, z 0.1); `fan.tscn` 18, 65, 80, 84–86 (post 0.13 × 0.5 × 0.15 at (0, −0.3, 0)), 88–90 (ring 0.49/0.63, 32 × 16, axis X), 123–131 (arrow x 0.6–1.15, tip 1.09–1.21); `impact_lever.tscn` 108 (inset top 8 mm proud), 112–126 (marks 10 mm proud). Every `MeshInstance3D` of the five scenes is classified exactly once by the five new rows.
- `FanElementDeclaration.cs:52` (two occluders today) and the new four-entry list; the Pulley's `Visual/Wheel` binding turns about Z, the rim's own axis (`PulleyElementDeclaration.cs:13–17`), so D4 holds.
- Matcher: hollow art is matched in the base pool (`VisibleSolidColliderTests.cs:203–209`), which is where all three new tori sit; mesh-error bound `:206` holds (anchor eye 0.4 mm, Fan ring 4.38 mm, Pulley rim 4.76 mm). Apex list `:558`, theory inline data `:524–549`, `Drop` `:456–512`, `AssertDropOnto` `:647–678`.
- Drops recomputed: A1 tube top 0.2075, mount top −0.04 (control lands at −0.04 + 0.1); A2 `Front` maps local z to −y: post face 0.19, mount edge 0.398 m from the resting centre; A3 rim top 0.135, wheel edge contact 0.425 (50 mm lower); A4 ring top 0.63, outside the cone. Restitution 1 for all three (`SceneBodies` material).
- A5: the streamline starts at the sample projected onto the source plane (`CuriousContraptions.Simulation/WorkshopPhysicsEngine.Airflow.cs:118–122`, `:141–162`); cone radius 0.85 × x / 5 gives 0.714 at 4.2 and 0.425 at 2.5; the default Wall control spans z 0.435–0.685. The post exemption also serves every axial Basketball payload (samples at y ±0.17 project inside the post), e.g. `FanGuidanceTests.cs:27`.
- A7b arrow arithmetic: both head strokes end at (0.30, 0); ink covers (0.29, 0) and (0.25, 0.04) at 7.1 mm from each stroke's centreline (half-width 15 mm), not (0.15, 0.04); corners reach x 0.311 inside the base's 0.325; clear of the post (|x| ≤ 0.065) and ring tube (|x| ≤ 0.07).
- A9 hub re-authoring: a 0.1 m Tennis ball at Fan-local (0, 0.30, 0.1) clears the ring opening (0.416 < 0.49) and the hub (0.316 > 0.24) at rest and meets the hub off-centre at y 0.218. The miss case at (0, 1, 0.8) clears ring (0.7 > 0.63) and base (0.7 > 0.5).
- A6's parenthetical (a 0.34 m ball cannot reach the post) holds: a front-face contact needs y ≥ −0.13 to clear the base and y ≤ −0.247 to clear the hub; side faces lie inside the ring's tube shadow.
- Counts: AC1 33 + 5; AC4 +15 (5 rows, 4 drops, 3 stream cases, 3 facts); AC2's three differences.
- Capability probe: seven present; symbols (`SceneGeometryExtractor.cs:15`, `:23–25`, `:57–68`, `:104`, `:191–230`; `AirflowValues.cs:45–48`; `AirflowSceneValidation.cs:38–41`) and every cited test exist.
- Commit path: no new file; `CuriousContraptions.tests.csproj:77` (`<Compile Include="*.cs" />`) at `25df3cd5`.
- Decisions: Fan blades decorative (coordinator 25), rope solid but owned by `ENGINE-CONNECTION-MATERIALS` (owner 1), runs before the programme (owner 22, 24h), no wire or ABI change (owner 24j/24k). Part B2 is now a flush print (owner 24i), so the Tennis probes of A1's control, A5 and A9 see the same sphere as on main.

## Non-blocking corrections (apply before dispatch, no re-audit)

### N1. Frontmatter baseline contradicts the Baseline paragraph (all five contracts)

- Where: line 6, `baseline_revision: '25df3cd5…'`, against line 54 ("the baseline is the main that contains [the planning work]; the coordinator captures it there, before first implementation").
- Consequence: the protocol captures `baseline_revision` once and never overwrites it (`_bmad/custom/bmad-loop-protocol.md`, Entry and candidate identity). A coordinator that treats the pre-filled value as captured roots the isolated checkout, task 1's bundle baseline and `git diff --check <baseline_revision>` at `25df3cd5`, before the planning commit.
- Fix: delete the `baseline_revision` line (the drafting brief says not to set it; line 54 already records the drafting revision).
- Classification: wrong specified behaviour (frontmatter).

### N2. Fan puzzle count

- Where: line 222, "in all 21 authored Fan puzzles".
- Defect: `content/puzzles.json` has 22 puzzles with a Fan (23 Fan records). The clearance claim holds for all of them (every Tennis ball starts at |Fan-local x| = 1 m or more).
- Fix: "in all 22 authored Fan puzzles (23 Fans)".
- Classification: test (evidence text).

### N3. Baseball repair's Fan test if it publishes first

- Where: A9 list (line 199) and filter (line 210).
- Defect: the Baseball repair adds `BaseballTests.TheFanMovesTheBaseballMoreThanTheTennisBallAndTheBasketball` (its R4: three balls on a default Fan's axis); the Basketball's samples project inside the new post, so it consumes the A5 exemption.
- Fix: add "`BaseballTests.TheFanMovesTheBaseballMoreThanTheTennisBallAndTheBasketball`, if the Baseball repair has published" to A9 and `|FullyQualifiedName~BaseballTests` to the filter.
- Classification: test.

Also update line 56 to cite this audit.

## Serialisation

Order A, B, B2, C; the Baseball repair before or after B2; one publisher at a time on `VisibleSolidColliderTests.cs`; the later publisher merges main once and repeats the full local round. Shared with S3, S5, S5b, S6–S6f only through `TODO.md`, `deferred-work.md` and `delivery-batches.md`. Part D (after the programme) edits other rows of the same file.

## Decisions for the owner

None. D1–D6 follow from the owner's rule.

## Evidence limits

Nothing was built or run. The hub speed (about 2.4 rad/s against the 0.1 rad/s bound) is a rigid-impulse estimate. Whether `HollowContactQuery.Overlaps` refuses or separates the eye-in-rim start of the two planned lessons was read, not run; either outcome is recorded.
