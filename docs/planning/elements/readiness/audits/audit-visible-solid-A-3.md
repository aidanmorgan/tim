# Planning audit 3: ELEMENTS-VISIBLE-SOLID-REMAINDER part A

Contract: `_bmad-output/implementation-artifacts/spec-elements-visible-solid-remainder.md` (4,534 words). Checkout `spec-drafts` at published main `25df3cd5` plus uncommitted planning work. Binding decisions: `docs/planning/elements/readiness/owner-decisions-2026-11-22.md`. Phase: planning; read-only; nothing built or run. Audited independently and as one serial set with parts B, B2, C and the Baseball repair. Earlier passes (`audit-visible-solid-A.md`, `-A-2.md`) were not relied on.

## Verdict: Fail

One defect sits inside `<intent-contract>` (F1), which becomes read-only after planning. It is a one-line factual fix. The design, the capability probe, every drop line and every expected delta hold at `25df3cd5`. After F1 and the required fixes R1–R6 are applied, the contract is ready for development; a re-audit can be limited to the diff of those fixes.

## What was verified at `25df3cd5`

- Every scene citation: `rope_anchor.tscn` 28–30, 32–34; `pulley.tscn` 58–64, 75–77, 85–87; `fan.tscn` 18, 65, 80, 84–90, 123–131; `impact_lever.tscn` 108, 112–126; `FanElementDeclaration.cs:52`. Mesh sizes and poses in the repairs table match the scenes.
- Capability symbols: `SceneGeometryExtractor.cs:104` (BakeProxy), `:191–230` (Hollow, torus branch), proxies selected by `CollisionShape3D` or a `col_` name (`:23–25`), so art keeps no collider; `AirflowValues.cs:45–48`, `AirflowSceneValidation.cs:38–41`, `WorkshopPhysicsEngine.Airflow.cs:141–162`; the streamline starts at the sample projected onto the source plane (`:121–122`), so it starts inside the new post and ring, and the occluder list has no count limit (`PhysicsGpuAbi.Airflow.cs:63`). Every cited test exists.
- `VisibleSolidColliderTests.cs`: `Specs` 63–162 (33 rows), `Feature` 444–449, `Drop` 456–512, theory 524–558, apex list at 558. The mesh-error bound (`:206`, 5 mm) holds for the new hollow art: Fan ring 4.38 mm (32 rings, 16 segments), Pulley rim 4.76 mm (32, 8), anchor eye 0.3 mm.
- Drops, recomputed: A1 tube top 0.2075, mount top −0.04; A2 `Front` maps local z to −y: post face 0.19, wheel face 0.085, mount corner 0.398 m from the resting centre; A3 rim top 0.135, wheel face 0.085; A4 ring top 0.63, outside the cone. Static restitution 1 for `rope_anchor`, `pulley`, `fan` base (declarations).
- A5 placements: ring case samples 0.53–0.59 m off axis inside the 0.49–0.63 tube shadow; post case samples at y −0.27 to −0.33 inside the post (y −0.55 to −0.05); the default Wall control covers z 0.435–0.685.
- A7b arrow: with `HeadA` −45° and `HeadB` +45° both strokes end at (0.30, 0); ink covers (0.29, 0) and (0.25, 0.04), not (0.15, 0.04); the head corners reach x 0.311, inside the base's 0.325.
- Delta +15 (5 rows, 4 drops, 3 stream cases, 3 facts) and AC1's 33 + 5 are correct; AC2's three differences are correct.
- Decisions: Fan blades decorative (coordinator 25), rope solid but out of scope (owner 1; CAT-058 section 6 item 3), runs before the programme (owner 22). No conflict.

## Blocking finding

### F1. The Problem's package count is wrong at the stated baseline

- Where: contract line 21, "Eleven delivered packages are outside the visible-solid table".
- Defect: at `25df3cd5` there are 45 packages (`BasketballResourceTests.cs:34`) and 33 table rows, so twelve are outside (the eleven of `8d1d68aa` plus `baseball`). Line 23 already says "33 of the 45". The count also depends on publication order: if the Baseball repair publishes first it becomes eleven again.
- Consequence: the read-only intent states a false fact at its own baseline.
- Fix: replace "Eleven delivered packages are outside the visible-solid table, and in five of them built from primitive art" with "At `25df3cd5` twelve delivered packages are outside the visible-solid table (the Baseball repair adds `baseball` separately); in five of them, built from primitive art,".
- Classification: wrong specified behaviour (intent text).

## Required fixes (outside the intent)

### R1. Missed consumer: two planned pulley lessons start with the weight's eye through the drawn rim

- Where: Code Map and "Running beside other work" (line 214), which checks only the Fan puzzles.
- Defect: `content/puzzles.json` lessons `counterweight` (solution weight, 4 kg, at (2, 5, 0.12) under the pulley at (2, 6, 0)) and `pulley_depth` (weight at (2, 5, 2.12) under the pulley at (2, 6, 2)), both `Planned`. The weight's eye torus (centreline 0.14 m, tube 0.0175 m, centre 0.5771 m above the weight's origin, axis Z, `weight.tscn` eye node) lies 0.423 m below the rim's centre in a plane 0.02 m from the rim's (rim centreline 0.4, tube 0.035, plane z 0.1). The two centreline circles intersect in projection, so the tubes overlap by about 3 cm at the authored pose. Today the rim has no collider; after this slice the authored solution starts with a dynamic torus inside a static torus: either refused by `ValidateHollowPlacement` (`engine/gpu/WorkshopPhysicsCompiler.cs:498–523`) or pushed apart at Run start. No test runs these lessons (not playable), so the full round will not show it.
- Fix: add to "Running beside other work": "Two planned lessons, `counterweight` and `pulley_depth`, place the 4 kg Weight with its eye through the Pulley's drawn rim (eye 0.423 m below the rim's centre, planes 2 cm apart). With the rim solid that start is an initial overlap. This slice does not edit `content/puzzles.json`; the entry `pulley-lesson-weight-start` in `deferred-work.md` records it for the level-proof slice of those lessons, which lowers each solution weight by at least 0.1 m." Add that entry to task 7.
- Classification: product defect (authored content), recorded.

### R2. The new tori are hollow kinds: say what construction and placement now refuse

- Where: Code Map (no Footprint section; part B states the equivalent at its line 145).
- Defect: `HollowSolid.IsHollow` includes `Torus` (`engine/gpu/HollowGeometry.cs:33–34`), so the eye, rim and ring tori make construction refuse a dynamic body that starts inside them (`WorkshopPhysicsCompiler.cs:498–523`) and make placement refuse such a pose (`engine/PlacementPose.cs:147`, `:253–254`). Placement extent also follows the base colliders (`PlacementPose.cs:113–116`): the Fan now reaches 0.63 m in ±z (was 0.5) and 0.63 m above its origin; the anchor 0.2075 m in +z; the pulley 0.435 m radially (was 0.4).
- Fix: add a "Footprint, capture and occlusion" paragraph with those three statements and "No test or authored playable lesson places a dynamic body inside the new tori (checked: `content/puzzles.json`, the Fan, Pulley and Rope anchor placements of the tests named in the verification list); the two planned pulley lessons are R1."
- Classification: test (consumer coverage).

### R3. Windmill tests depend on the Fan-post exemption and are not named

- Where: verification row A9 (line 191) and the scoped filter (line 202).
- Defect: `WindmillIntegrationTests` places Fans at (0, 5, 0) and (6, 5, 0) and the Windmill at (3, 5, 0) (`:213`, `:271`, `:419`, `:453–455`). The capture samples at (−0.12, ±0.42, 0) project onto the Fan's source plane at Fan-local y −0.42, inside the new post (y −0.55 to −0.05, |x| ≤ 0.065, |z| ≤ 0.075). These tests therefore exercise the A5 exemption for rotary captures; without the post in the occluder list they would lose a sample. Also placing Fans: `WindmillAdmissionTests`, `BodyOwnershipTests` (`:30`), `CoordinateTravelGeneralityTests`, `WindmillArtworkTests`.
- Fix: add those five classes to the A9 "Classes that place these parts" list, and append `|FullyQualifiedName~Windmill|FullyQualifiedName~BodyOwnershipTests|FullyQualifiedName~CoordinateTravelGeneralityTests` to the filter.
- Classification: test.

### R4. Rows A3 and A4 have no control

- Where: verification lines 183–184.
- Fix: A3, add "Control: without the rim collider the ball first meets the wheel's edge at 0.085 + 0.34 above the origin, 50 mm lower, outside the 17 mm penetration band." A4, add "Control: without the ring collider the ball falls 0.49 m further, to the hub's top (r 0.14)."
- Classification: test.

### R5. Baseline precondition for the planning text

- Where: lines 50, 54, 76, 145 cite `docs/planning/invest/delivery-batches.md` section 5 "Repairs", the owner-decisions file and CAT-058 sections that exist only in the uncommitted planning work; at `25df3cd5` section 5 has no Repairs table and `readiness/owner-decisions-2026-11-22.md` does not exist.
- Fix: add to **Baseline**: "Precondition: the planning commit that adds `docs/planning/elements/readiness/`, the Repairs table of `delivery-batches.md` section 5 and the element-spec revisions cited here is on main; the coordinator captures the baseline at or after it."
- Classification: wrong specified behaviour (Code Map).

### R6. Stale record lines

- Line 56: replace the planning-verdict sentence with "Planning audit 3 (`.tools/audit/audit-visible-solid-A-3.md`, 23 November 2026): Fail on F1; ready after F1 and R1–R6."
- Line 54: "every product and test file cited here is unchanged" is false for `FanGuidanceTests.cs`, `PackageRuntimeParityTests.cs` and `WorkshopIntegrationHarnessTests.cs` (Baseball membership rows added at `fa86969f`); say "every file cited with a line number".
- Line 74: `FanArtworkTests` is at `FanIntegrationTests.cs:610`, not 609. Line 103: the old arrow and tip span x 0.6 to 1.21, 0.61 m, not 0.67 m.
- AC3 (line 151): replace "and any other assertion that counted a part's colliders or internal occluders, corrected ... and named in the run log" with "no other test counts these parts' colliders or occluders (searched: `rg -n 'Colliders|InternalOccluders'` over the A9 classes; `PackageRuntimeParityTests` derives its expectation from the bundle)". An open-ended allowance is not acceptance.

## Serialisation

Order A, B, B2, C; the Baseball repair before or after B2; one publisher at a time; each later slice merges main once and repeats the full round. All five contracts and the Repairs table agree. Table rows: 33 at `25df3cd5`, 38 after A. Part A runs first and has no prerequisite. Shared files with S3, S5, S6: `TODO.md`, `deferred-work.md`, `delivery-batches.md` only (S3 reads `VisibleSolidColliderTests.Specs` and does not edit it). Part B2's seam later changes the Tennis probes of A1's control, A5 and A9; the margins hold (seam outer 0.103 m at radius 0.1 against the 0.1725 m opening; the hub contact direction is about 70° from the seam's plane).

## Decisions for the owner

None. D1–D6 are defensible from the owner's rule; D6 (hub reachable only through the ring) follows from it.

## Evidence limits

- Nothing was built or run. Whether `HollowContactQuery.Overlaps` reports the eye-in-rim overlap of R1 as a refusal (it counts only distance-only contacts for a hollow partner, `HollowContactQuery.cs:58–66`) was not executed; either outcome is a defect for those lessons.
- The hub estimate (about 2.4 rad/s against 0.1) is a rigid-impulse calculation.
- Rope, weight and lever tests were checked by placement only; weights lifted toward a pulley meet the rim 35 mm before the wheel, which no named test appears to reach.
