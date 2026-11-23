# Planning audit 4: ELEMENTS-VISIBLE-SOLID-REMAINDER part B

Contract: `_bmad-output/implementation-artifacts/spec-elements-visible-solid-remainder-b.md` (5,334 words). Checkout `spec-drafts`, HEAD `25df3cd5`, product and test paths identical to `25df3cd5`. Binding decisions: `docs/planning/elements/readiness/owner-decisions-2026-11-22.md`. Phase: planning; read-only; nothing built or run. Audited independently and as one serial set with parts A, B2, C and D.

## Verdict: Scoped Pass

No finding is inside `<intent-contract>`; the intent can freeze. Ready for development once part A is published and the `VisibleSolidColliderTests.cs` anchors are re-read there (its own precondition, line 56). Apply N1–N3 before dispatch; no re-audit needed.

## Earlier blocking findings (audit 3), resolved in substance

- F1 ("belt links"): line 28 reads "drive relay and Mechanical links" (coordinator decision 36); line 78 says "a ball on the carrier".
- R1 (`LaunchLessonSolveTests`): named at line 78, in B12 and in the filter. `LaunchLessonSolveTests.cs:93` runs `wind_then_release` and `saved_for_later` (both `Playable`, both with a Wound spring).
- R2 (precondition): line 56.
- R3 (H3 body order): line 136 asserts the moving root at index b − 1 is body b's root (sliders first, then hinges, `SceneGeometryExtractor.cs:43–46`).
- R4 (stale lines): applied.

## What was re-verified at `25df3cd5`

- `conveyor.tscn`: `load_steps=15` with 15 sub-resources (the file counts resources without the +1; 16 after one added shape is consistent), `Mesh_PulleySpoke` 45 (0.33 × 0.045 × 0.035), `col_support_b` 84, `col_roller` 123, wheels 134 and 145 (r 0.22, h 0.1, axis Z, z 0.73–0.83), spokes 138 and 149 at z 0.06 (27.5 mm proud). `Visual/PulleyOut` turns by `LocalRotationZ` (`ConveyorElementDeclaration.cs:20`), the wheel's own axis; `PulleyIn` rides the roller body with no binding.
- `linear_pusher.tscn`: 15 sub-resources, `load_steps=16` (+4 shapes gives 20); `col_Foot` 50, `col_Barrel` 53 (visible), rod 55 at (0.725, 0, 0) spanning the barrel face to the head, ring 62 (0.235/0.255, axis X), `col_Head` 65, `col_Rod` 67 (head-local −0.125), indicator 71, travel marks 75–93 at z 0.333 (2–14 mm off the 0.325 face), sockets 97–121.
- `wound_spring.tscn`: 16 sub-resources, `load_steps=17` (+5 shapes gives 22); the six drawn proxies 85–124; `ChargeIndex` 115, `Shaft/Spoke` 128 (17.5 mm proud of the wheel face), marks 402–424 (`stroke`-bound `LocalY`, `WoundSpringElementDeclaration.cs:76–80`), socket 427.
- Exactly eight `col_` `MeshInstance3D` nodes in `elements/*/*.tscn` lack `visible = false` (scan of every scene), all eight in the two scenes this slice separates, so AC6 and the in-`Art` `Visible` assertion are achievable here and break no existing row.
- Readers of the renamed nodes: only `WoundSpringArtTests.cs:41` (`Visual/col_Sleeve`) and `:58` (index position `(.6f, -.1f, .14f)`), the two literals task 7 edits.
- Drops recomputed: B1 face 0.83 vs idle-roller end 0.65; B2 the same on the roller; B3 socket apex 0.47, barrel-edge stop 0.641, indicator 0.622 m away; B7 socket apex 0.615, rail stop 0.44 (0.515 lower), base edge 0.448 m and sleeve end 0.666 m from the resting centre. Restitution 0.05 (Conveyor) and 0 (pusher, spring) from `SceneBodies`.
- Matcher anchors: `ArtSpec` 54–55, `Solid` 61, conveyor row 88–91, theory 184–234, `:193`, `:207`, `:222–223`, `Art` 242–314, `:249`, `MatchesHollow` 348–366 and its tube arm `:359` (mouth bores only today), `Feature` 444–449, `Drop` 456–512, `AssertDropOnto` 647–678, landing rule `:658`.
- H3 is buildable from declared data: extrusion on head collider 1 (`LinearPusherElementDeclaration.cs:40–41`) and span `Visual/Head` → `Visual/Rod` (`:71`).
- Construction refusal applies to hollow colliders on any body except the partner's own and its children (`WorkshopPhysicsCompiler.cs:498–523`), so the head ring is checked against independently authored payloads, as the Footprint section says.
- Counts: AC1 +2 rows (conveyor replaced); AC4 +9 (2 rows, 4 low-restitution drops, 3 facts); AC2 per-package growth matches D8.
- Every cited capability test exists (`CompoundMassAndRollingTests` Torus and free-flight cases, `LinearPusherTests` two, `WoundSpringContactTests`, `FaceMarkingTests`, `ClippedExtrusionTests`).
- Decisions: Mechanical link is a schematic with no collider (owner 2, coordinators 25 and 36); no tether; before the programme (owner 22, 24h); no wire or ABI change (24j/24k).

## Non-blocking corrections (apply before dispatch, no re-audit)

### N1. Frontmatter baseline

- Where: line 6 `baseline_revision: '25df3cd5…'` against line 56 (baseline is the main containing the planning work and part A).
- Consequence: an isolated checkout or task 0 bundle baseline rooted at `25df3cd5` lacks part A, so AC2's "only differences" would also show `rope_anchor`, `pulley` and `fan`.
- Fix: delete the line; the coordinator captures it on part A's published main.
- Classification: wrong specified behaviour (frontmatter).

### N2. Floating-art reason contradicts coordinator decision 33

- Where: line 170, "closing the gaps moves ports and is not a visible-solid repair".
- Defect: coordinator decision 33 put the Counter's floating sockets, foot and face (CAT-020 repair 3, which moves four declared ports) into visible-solid repair part D, under the rule "A drawn solid may not hang clear of the part it belongs to" (`CAT-020-counter.md:111`, `readiness/batch-d.md:53`).
- Fix: replace the sentence with "The ports are declared at those points; seating them moves ports, as CAT-020 repair 3 does in part D (coordinator decision 33). Recommended default for the owner: add them to part D." Keep the item non-blocking for this slice (see the owner decision below).
- Classification: wrong specified behaviour (Code Map decision text).

### N3. Art-solidity wording this slice contradicts

- Where: D3 (line 163) records only the "moved by bindings" correction for `spec-engine-art-solidity.md`.
- Defect: `spec-engine-art-solidity.md:200` also carries "visible proxy meshes as art" as a matcher extension, which K1 and this slice's Never clause remove.
- Fix: append to D3: "Its task 6 line 'visible proxy meshes as art' is withdrawn by K1 and is corrected at the same replanning."
- Classification: wrong specified behaviour (downstream record).

Also update line 58 to cite this audit. Readability only: matrix row "B2" (input pulley) shares its name with part B2; line 210's "in B2" means the row.

## Serialisation

After part A, before parts B2 and C; part C consumes `Solid.Inner` and `ABallDroppedOntoEachLowRestitutionFeatureStopsAtItsDrawnSurface` by these names. One publisher at a time on `VisibleSolidColliderTests.cs`; shares `TODO.md`, `deferred-work.md`, `delivery-batches.md` with S3, S5, S5b, S6–S6f. No content dependency on the Baseball repair.

## Decisions for the owner

- Floating art across the remainder (not blocking B): the pusher's two upper sockets (7.6 cm off the barrel), the spring's socket, shaft wheel and latch lever (0.35–0.39 m in front of the rails), and part C's Cannon sockets and track and Windmill wheel. Recommended default: add them to repair part D, as coordinator decision 33 did for the Counter, so the "hang clear" rule is applied once; the alternative is the contracts' "leave as drawn".

## Evidence limits

Nothing was built or run. The hinge's axial support under a ball on the input wheel's face (row B2) and admission of the 0.01 m torus tube on the slider body are unexecuted. The `git grep -A6` listing for AC6 is read by eye; the in-test assertion covers table packages only, which after part C is all 45.
