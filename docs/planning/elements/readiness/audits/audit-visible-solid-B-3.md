# Planning audit 3: ELEMENTS-VISIBLE-SOLID-REMAINDER part B

Contract: `_bmad-output/implementation-artifacts/spec-elements-visible-solid-remainder-b.md` (5,172 words). Checkout `spec-drafts` at published main `25df3cd5` plus uncommitted planning work. Binding decisions: `docs/planning/elements/readiness/owner-decisions-2026-11-22.md`. Phase: planning; read-only; nothing built or run. Audited independently and as one serial set with parts A, B2, C and the Baseball repair; earlier passes (`audit-visible-solid-B.md`, `-B-2.md`) were not relied on.

## Verdict: Fail

One defect sits inside `<intent-contract>` (F1: a name that coordinator decision 36 forbids). Everything else that the intent and the verification depend on holds at `25df3cd5`. After F1 and R1–R4 the contract is ready for development, subject to its own rule that line anchors in `VisibleSolidColliderTests.cs` are re-read on the main that publishes part A.

## What was verified at `25df3cd5`

- Scene citations: `conveyor.tscn` 1 (`load_steps=15`), 45, 84, 123, 134, 138, 145, 149; `linear_pusher.tscn` 1 (16), 32, 50, 53, 55, 62, 65, 67, 71, 75–93, 97–121; `wound_spring.tscn` 1 (17), 29, 33, 41, 85, 90, 95, 100, 110, 115, 124, 128, 402–424, 427. Sizes and poses in both repair tables match. The new `load_steps` (16, 20, 22) are the sub-resource counts after the stated edits.
- Exactly eight `col_` `MeshInstance3D` nodes in `elements/*/*.tscn` lack `visible = false`, all in the two named scenes, so AC6 is achievable by this slice alone (parts A and C add only hidden proxies).
- The extractor bakes only `CollisionShape3D` and `col_`-named nodes (`SceneGeometryExtractor.cs:23–25`), per body in file order (`:57–68`), so the renamed art (`Sleeve` keeps `metadata/wall_thickness`) is not baked; no other art node in the catalogue carries `wall_thickness`, so H2 changes no existing row.
- `WoundSpringArtTests.cs:41` (`Visual/col_Sleeve`) and `:58` (index literal) are the only readers of the eight names; `ConveyorTests.cs:642` reads the input spoke relative to its authored pose.
- `LinearPusherElementDeclaration.cs:40–41` extrusion (MaterialBody 1, Collider 1), its span (`Visual/Head` → `Visual/Rod`), `SceneGeometry.cs:16`, `BundleSchema.cs:41`, `ElementPresentation.cs:116`: H3 is buildable from declared data.
- Drops recomputed: B1 face 0.83, idle-roller end 0.65; B2 the same on the roller body; B3 socket apex 0.47, barrel edge 0.641, indicator 0.622 m away; B7 socket apex 0.615, base edge 0.448, sleeve end 0.666, rail top 0.1. Static restitution 0.05 (Conveyor) and 0 (pusher, spring) from the declarations.
- Expected deltas: AC1 +2 rows; AC4 +9 (2 rows, 4 low-restitution drops, 3 facts); AC2's per-package collider growth matches D8.
- Decisions: the Mechanical link is a schematic with no collider (owner 2, coordinators 25 and 36), stated at line 144; no tether; runs before the programme (owner 22).

## Blocking finding

### F1. "belt links" in the intent

- Where: contract line 28 (Always), "conveyor transport, drive relay and belt links".
- Defect: coordinator decision 36 (23 November 2026): the delivered rigid mechanical connection is named "Mechanical link" everywhere, and "belt" means only the new Drive belt material (EL-200). The Conveyor's drive connection is a Mechanical link.
- Consequence: the read-only intent names a material the delivered parts do not have.
- Fix: replace "drive relay and belt links" with "drive relay and Mechanical links". Outside the intent, line 78 "a ball on the belt touches a wheel" may stay (it is the Conveyor's carrier surface), but "a ball on the carrier" is clearer.
- Classification: wrong specified behaviour (intent text).

## Required fixes (outside the intent)

### R1. Missed consumer: the playable Wound spring lessons are also run by `LaunchLessonSolveTests`

- Where: line 78 ("run by `WoundSpringIntegrationTests`"), row B12 (line 229) and the scoped filter (line 238).
- Defect: `LaunchLessonSolveTests.cs:93` runs `wind_then_release` and `saved_for_later` (both with a Wound spring, both `Playable`) through Run, Reset and save/load. The spring gains `col_ActivationSocket` and its eight proxies are rebuilt.
- Fix: line 78, "run by `WoundSpringIntegrationTests`, `WoundSpringLifecycleTests` and `LaunchLessonSolveTests`"; add `LaunchLessonSolveTests` to B12's Spring list and `|FullyQualifiedName~LaunchLessonSolveTests` to the filter.
- Classification: test.

### R2. Baseline precondition for the planning text

- Where: lines 54–56, 69, 184 cite `delivery-batches.md` section 5 "Repairs" (absent at `25df3cd5`) and decisions from `readiness/owner-decisions-2026-11-22.md` (absent at `25df3cd5`).
- Fix: add to **Baseline**: "Precondition: the planning commit that adds `docs/planning/elements/readiness/` and the Repairs table of `delivery-batches.md` section 5 is on main, and part A is published; the coordinator captures the baseline at or after both."
- Classification: wrong specified behaviour (Code Map).

### R3. H3 ties the moving body to the row's root order

- Where: line 136, "its `MaterialBody` b satisfies `spec.MovingRoots[b − 1] == span.MovingNode`".
- Defect: `MovingRoots` is a test-authored list; its order is not tied to the declaration's body order (slider bodies then hinge bodies, `SceneGeometryExtractor.cs:43–46`). A row listing roots in another order would compare the wrong body.
- Fix: append "and the moving root at index b − 1 is the root of body b in the entry's scene ownership (sliders first, then hinges); H3 asserts that, so a reordered row fails instead of selecting another body."
- Classification: test.

### R4. Stale record lines

- Line 58: replace with "Planning audit 3 (`.tools/audit/audit-visible-solid-B-3.md`, 23 November 2026): Fail on F1; ready after F1 and R1–R4, line anchors re-read on part A's published main."
- Line 56: "every product and test file cited here is unchanged" is false for `PackageRuntimeParityTests.cs` and `WorkshopIntegrationHarnessTests.cs` (Baseball membership at `fa86969f`, named in B12); say "every file cited with a line number".

## Serialisation

After part A, before parts B2 and C; one publisher at a time on `VisibleSolidColliderTests.cs`; consistent with parts A, B2, C, the Baseball repair and the Repairs table. Rows 38 → 40 (the `conveyor` row is replaced). Part C depends on this part's `Solid.Inner` and on the low-restitution theory by name (`ABallDroppedOntoEachLowRestitutionFeatureStopsAtItsDrawnSurface`); the names here and in part C agree. No overlap with S3, S5, S6 beyond `TODO.md`, `deferred-work.md` and `delivery-batches.md`. Chained-Conveyor and Windmill-Conveyor placements in the named tests leave at least 0.86 m between new wheels and other parts' bodies.

## Decisions for the owner

None that cut scope or waive a gate. The floating art (pusher sockets 7.6 cm off the barrel; spring socket, shaft wheel and latch lever 0.35–0.39 m in front of the rails) is correctly left to the owner as non-blocking, default "leave as drawn".

## Evidence limits

- Nothing was built or run. The hinge's axial support under a ball resting on the input wheel's face (B2) and admission of the 0.01 m torus tube on the slider body are unexecuted, as the contract says.
- The AC6 `git grep -A6` listing is read by eye; the in-test `Visible` assertion covers only packages in the table.
- `content/puzzles.json` Conveyors (all `Planned`) were checked for bench extent only, as the contract states.
