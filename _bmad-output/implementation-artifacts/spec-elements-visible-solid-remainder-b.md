---
title: 'ELEMENTS-VISIBLE-SOLID-REMAINDER, part B: Conveyor, Linear pusher and Wound spring'
type: 'bugfix'
created: '2026-11-21'
status: 'done'
baseline_revision: '37fbd3a48710306347b1660039d4ed142f0eeb3d'
review_loop_iteration: 0
followup_review_recommended: false
context:
  - '{project-root}/docs/testing-guidance.md'
  - '{project-root}/docs/delivery-workflow.md'
  - '{project-root}/docs/engine-contracts.md'
  - '{project-root}/_bmad/custom/element-free-engine-design.md'
warnings: ['oversized']
deferred:
  - summary: >-
      Scene-extractor refusals lack per-branch tests and node names in their messages.
    evidence: |-
      Review pass 4 (BH4-3, BH4-5, VG4-1 to VG4-5; run-log E14); deferred under Re-review scope: outside the pass 3 repair, no current content affected, coordinator decision 3 limited tests to one generator case per code. deferred-work.md, part B carry-forward entry.
    location: >-
      tools/BundleElements/SceneGeometryExtractor.cs
    severity: low
  - summary: >-
      Delete the declaration Colliders member and its bundle copy (EFE7-1), lane 1's next slice before B2.
    evidence: |-
      Owner-ruled, handled messages 25 November (decision 6; run-log E22, E25); deferred-work.md, part B entry.
    severity: high
  - summary: >-
      Whether KHR and compile-time extraction stay planned collider sources (FO7-1 planning documents).
    evidence: |-
      Coordinator decision 7 (run-log E22); owner the ENGINE-CORE-2d replanning; deferred-work.md, part B entry.
    severity: low
---

<intent-contract>

## Intent

**Problem:** The owner's rule is that what a player sees as solid is solid. A ball passes through the Conveyor's two pulley wheels, the Linear pusher's indicator, five sockets and head ring, and the Wound spring's activation socket. All three parts also carry raised markings that read as solid. The Linear pusher and Wound spring are outside the visible-solid table because eight of their solids are drawn by the collider proxies themselves, against the scene contract that a proxy is a separate node that is not rendered.

**Approach:** Give each of those drawn solids a collider of exactly its drawn shape on the body that carries it, redraw the raised markings as flat printing, separate the eight drawn proxies into art and hidden proxies as the scene contract requires, and bring the Linear pusher and Wound spring into the table (two more of the 45 delivered packages) with the Conveyor's row corrected.

## Boundaries & Constraints

**Always:** A collider proxy is a separate node that is not rendered, of the same primitive, size and pose as the mesh it backs (`docs/engine-contracts.md:88`). A new proxy is appended after its body's existing proxies, and a separated proxy keeps the file order of the node it replaces, so no collider index moves. Each collider takes its body's existing material. Colliders stay scene data: no engine, compiler, solver, wire or declaration change. Art turned by a presentation binding is decorative under the rule stated once in part A (its D5). Every existing behaviour is preserved: conveyor transport, drive relay and Mechanical links; pusher travel, self-lock, endpoint outputs and rod; spring winding, retention, release and sleeve guidance. The matcher's tolerances do not change; its new rules apply to every package alike. Print lies between 0 and 0.5 mm in front of its face, the delivered print contract. Reset, save and load restore each part as before. Follow the handoff order, Anvil before each write, one retained implementer, the engine review tier and one full local round with zero failures and `Skipped: 0`.

**Never:** A visible collider proxy, or a matcher rule that treats one as art. A collider larger, smaller or elsewhere than the drawn mesh. A raised marking kept beside its printed replacement. A matcher rule that names a package. A "non-solid tether" or any new decorative reason. The Tennis ball (part B2), or the Cannon, Windmill and Balloon (part C). A new parameter, port, declared `ElementFootprint` or moved port. A play test that tells a 5 mm proud ring from its sphere. Hashes, exact arrival ticks, mutants, browser runs, skipped tests.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| B1 Conveyor output pulley | A Basketball dropped onto the wheel's face, the Conveyor on its back | It stops at the drawn face | No error expected |
| B2 Conveyor input pulley | The same on the input wheel, which rides the roller's body | It stops at the drawn face | No error expected |
| B3 Linear pusher socket | A Basketball dropped onto the `RetractedOut` socket of an upright pusher | It stops at the drawn sphere | No error expected |
| B4 Linear pusher indicator and other sockets | The compiled part | One sphere collider at each drawn sphere (proven by shape; one socket of the same primitive is dropped on in B3) | The table row fails for a missing one |
| B5 Linear pusher head ring | The compiled part | One full torus (centreline 0.245 m, tube 0.01 m, axis along travel) on the head's body after its sphere and rod | The table row fails |
| B6 Linear pusher rod | The part at its authored pose | The drawn rod and the head body's clipped rod collider are the same box in the same place | A rod collider with no drawn rod fails |
| B7 Wound spring socket | The compiled part | One sphere collider at the drawn activation socket (proven by shape, as B4; part D drops a Tennis ball on it at its seated centre) | The table row fails for a missing one |
| B8 separated proxies | The eight solids that were drawn by their proxies | Each is an art mesh with a separate hidden proxy; the compiled colliders, their order and their dimensions are unchanged (sleeve: outer 0.49 m, bore 0.41 m, half-length 0.9 m) | The table row fails for a missing or different one |
| B9 printed markings | Both Conveyor spokes; pusher travel marks 0–4; spring marks 0–4, charge index and shaft spoke | Each is a flat printed surface inside its face, between 0 and 0.5 mm in front of the face plane, with its position and colour; no raised volume remains | A marking outside that band fails |
| B11 table coverage | The visible-solid test for the three packages | Every solid or hollow mesh has one matching collider and no collider is without art | Existing failure messages |
| B12 retained behaviour | The named existing tests of each part | Pass unchanged, apart from the two named literals | Existing |

</intent-contract>

## Code Map

Delivery shape: one product repair slice over three packages (precedent: `spec-elements-visible-solid-sweep.md`). The repair story runs in the order A, B, B2 (Tennis ball), C. Review tier: **engine** (it changes the shared matcher in a test class). No new source file and no test-project edit: `CuriousContraptions.tests.csproj:77` compiles every top-level `*.cs` (S2, `25df3cd5`).

**When.** Before the engine programme: repair parts A, B, B2 and C and the Baseball repair are among the slices owner decision 22 (23 November 2026) allows before it. This part follows part A (`spec-elements-visible-solid-remainder.md`) and is replanned from the revision that publishes it; that is sequence, not an external blocker.

**Baseline.** `37fbd3a4` (coordinator decisions 37 and 38): the published main that contains the planning work and part A; every scope guard and `git diff` here compares against it. Drafted on `25df3cd5`, with line numbers first measured on `8d1d68aa` and re-read on the baseline (run-log E2). Expected totals are baseline + delta.

Dispatch record (24 November 2026): checkout `.claude/worktrees/slice-visible-solid-b` (branch `slice-visible-solid-b`, lane 1, owner decision 24m), baseline `37fbd3a48710306347b1660039d4ed142f0eeb3d`, policy revision the same; baseline round 7309/0/0 (owner's figure). Anchors re-read and the moved lines of `VisibleSolidColliderTests.cs` corrected (run-log E2, E3). The owner ran the full round on the first compiling candidate, before review (part A close note).

Planning verdict: Scoped Pass (fourth audit, `docs/planning/elements/readiness/audits/audit-visible-solid-B-4.md`; N1–N3 applied; floating art settled by coordinator decision 39). Part D's fifth audit (`docs/planning/elements/readiness/audits/audit-part-d-5.md`, F1) removed the Wound spring socket's drop case: B7 is proven by shape.

Split: the Tennis ball is part B2 (`spec-elements-visible-solid-remainder-b2.md`); this contract keeps the two packages whose proxies are separated, and the Conveyor.

Owned scope:

- `elements/Conveyor/conveyor.tscn` -- `load_steps` 1, `Mesh_PulleySpoke` 45, `col_support_b` 84, `col_roller` 123, `PulleyIn/Wheel` 134, `PulleyIn/Spoke` 138, `PulleyOut/Wheel` 145, `PulleyOut/Spoke` 149.
- `elements/LinearPusher/linear_pusher.tscn` -- `load_steps` 1, `Mark` 32, `col_Foot` 50, `col_Barrel` 53, `Rod` 55, `Head/Ring` 62, `col_Head` 65, `col_Rod` 67, `Indicator` 71, `TravelMark0`–`4` 75–93, the five `Socket_…` 97–121.
- `elements/WoundSpring/wound_spring.tscn` -- `load_steps` 1, `Mark` 29, `Index` 33, `Spoke` 41, the six drawn proxies 85, 90, 95, 100, 110 and 124, `Head/ChargeIndex` 115, `Shaft/Spoke` 128, `Mark0`–`4` 402–424, `ActivationSocket` 427.
- `CuriousContraptions.tests/VisibleSolidColliderTests.cs` -- `ArtSpec` 54–55, `Solid` 61, `Specs` 63–182 (`conveyor` 88–91), the theory 204–254, `Art` 262–334, `MatchesHollow` 368–386, `Feature` 464–469, `Drop` 479–548, `AssertDropOnto` 768–800, and the class, `ArtSpec` and `Art` summaries (lines on the baseline, after part A; run-log E2).
- `CuriousContraptions.tests/WoundSpringArtTests.cs:41` (reads `Visual/col_Sleeve`) and `:58` (the index position literal), plus one new fact each in `WoundSpringArtTests.cs`, `LinearPusherArtTests.cs` and `ConveyorTests.cs`.
- `CuriousContraptions.tests/FanIntegrationTests.cs:666`, the one other caller of `VisibleSolidColliderTests.AssertFacesUp`, which the print facts generalise to `AssertFaces(triangles, facing, name)` (added by the implementation owner, 24 November 2026).
- Review pass 1 repair closure (Spec Change Log, 25 November 2026): `CuriousContraptions.tests/ReverseTransmissionArtTests.cs` (the output wheel held on its collider under its binding, VG1); `docs/hollow-geometry-design.md:43`, `:59–62` (dynamic hollows delivered, F2); and the AC5 exception paths: `tools/BundleElements/SceneGeometryExtractor.cs:23–25` and a refusal test in `tools/BundleElements.Tests/SceneGeometryExtractorTests.cs` (FO1); `DESIGN.md:85–88`; `docs/general-engine-design.md:98–101` (the infix is on all four analytic lines); `_bmad-output/implementation-artifacts/spec-engine-art-solidity.md` task 6, with its lines 28 and 78, which adopt the same infix; `docs/hollow-geometry-design.md:88` (the same "both static" claim as `:43`); comment-only lines `CuriousContraptions.Simulation/WorkshopCollisionKernels.cs:1996–1997` and `CuriousContraptions.Abstractions/PhysicsDeclarationValues.cs:10` (F2b).
- Review pass 2 repair closure (coordinator decision 3 and the pass 2 patches, Spec Change Log, 25 November 2026): `tools/BundleElements/SceneGeometryExtractor.cs` (every refusal typed), `tools/BundleElements/BundleGenerator.cs`, `CuriousContraptions.Abstractions/BundleServices.cs` (`BundleErrorCode` 16–19), `tools/BundleElements.Tests/SceneGeometryExtractorTests.cs` and `Fixtures.cs`; `spec-engine-art-solidity.md` lines 60, 130 and 172–177 (its planned codes move to 20–23); the alias tokens at `DESIGN.md:91`, `docs/planning/invest/engine.md:179`, `:202`, `docs/planning/invest/decisions.md:140`, `_bmad-output/planning-artifacts/prd.md:191`, `:195`, `_bmad-output/planning-artifacts/epics.md:940` (coordinator edit, run-log E11); and the element documents under `docs/planning/elements/` whose citations of lines this slice moved are refreshed (run-log E10).
- Decision 4 repair closure (run-log E17, E18): `tools/BundleElements/PackageValidator.cs` (the shared tube-mouth rule); `CuriousContraptions.tests/BundlePortsTests.cs` (`LocatorNamesAreMatchedOrdinally` reads both tool files); `tools/BundleElements.Tests/PortLocatorTests.cs` and `tools/BundleElements.Tests/BundleValidatorTests.cs` (E19, E23); `DESIGN.md:92`, `:105` (decision 7); citations moved by the repair in `EL-001`, `EL-107`, `CAT-005`, `spec-engine-art-solidity.md`, `docs/planning/elements/readiness/engine-design-gap.md` and `epics.md` lines 4635 and 5095.
- `docs/planning/elements/CAT-019-conveyor.md`, `CAT-039-linear_pusher.md`, `CAT-071-wound_spring.md`; `_bmad-output/implementation-artifacts/deferred-work.md` (the per-instance collider-growth entry whose source is `spec-elements-visible-solid-sweep.md`); `docs/planning/invest/delivery-batches.md` section 5 (Repairs, part B's row); `TODO.md`; this contract and its run log.

`elements/elements.bundle.json` is build output, not committed (`.gitignore:17`); task 0 captures a baseline copy.

Not consumers, checked (read-only; detail in run-log E18): no declaration changes (the Conveyor's driven surface stays carrier collider 0, the pusher's rod head collider 1); the eight proxy names are read only by `WoundSpringArtTests.cs:41`; placement extent follows the colliders (`engine/PlacementPose.cs:113–116`); the two playable Wound spring lessons run through Run, Reset and save/load (`LaunchLessonSolveTests.cs:93`), and no test runs an authored Conveyor puzzle.

### Capability probe

Eight present, one narrower and owned here (test harness only); none missing.

- **Present.** Box, sphere and cylinder colliders baked from `CollisionShape3D` proxies: `SceneGeometryExtractor.BakeProxy` (`tools/BundleElements/SceneGeometryExtractor.cs:104`). Tests: `SceneGeometryExtractorTests`; in play, the `LatchSocket` case of `VisibleSolidColliderTests.ABallDroppedOntoEachSolidFeatureStopsAtItsDrawnSurfaceAndReboundsAtTheProductRestitution`.
- **Present.** A full torus and a tube baked from hidden mesh proxies, the tube with `metadata/wall_thickness`: `SceneGeometryExtractor.Hollow` (`:191–221`). Tests: `VisibleSolidColliderTests.ABallDroppedOntoTheBumpersUpperRingStopsAtItsDrawnTube`; the `powered_gate` row (hidden sleeve proxy).
- **Present.** Proxies are baked in file order for the body whose root they sit under: `SceneGeometryExtractor.BakeBodies` (`:57–68`); the pusher's head already carries two. Test: `LinearPusherTests.RotatedSphericalHeadPushesCargoAndLateralClearanceMisses`.
- **Present.** A torus child on a moving body in contact. A slider's or hinge's moving body compiles as a dynamic body (`engine/gpu/WorkshopPhysicsCompiler.cs:173`, `:214`), and a hollow collider meets a sphere, box or cylinder by one rule whatever body carries it (`CuriousContraptions.Simulation/WorkshopCollisionKernels.cs:1999–2008`). Test: `CompoundMassAndRollingTests.HollowChildLocalPoseAndDeclaredCentreGiveRealContactAndEmptySpace` (Torus case, a law scene); through the package compiler, the Basketball's torus seam on a dynamic body (`BasketballResourceTests.cs:48–52`). No existing test has a hollow collider on a slider or hinge body meet cargo; B5 proves the ring by shape and D6 records why it has no play test (run-log E3).
- **Present.** Child colliders add no mass: every moving body here declares a constant mass frame (`ConveyorElementDeclaration.cs`, hinge `MovingBody`; `LinearPusherElementDeclaration.cs:33`; `WoundSpringElementDeclaration.cs:35–36`), and the compiler takes mass and moments from it (`WorkshopPhysicsCompiler.cs:170–175`, `:211–216`). Tests: `CompoundMassAndRollingTests.CollisionChildrenDoNotChangeDeclaredFreeFlight`; through the package compiler, `PackageRuntimeParityTests.cs:205–213` (moving-body mass and colliders against the declaration).
- **Present.** A clipped rod collider on the head body: `ClippedExtrusionDescriptor` (`CuriousContraptions.Abstractions/SceneGeometry.cs:16`). Tests: `ClippedExtrusionTests`, `LinearPusherTests.PackageInstallationRoundTripsItsDriveExtrusionAndPhaseBinding`. H3's inputs: the pusher's extrusion (`LinearPusherElementDeclaration.cs:40`) and its one span, `Visual/Head` to `Visual/Rod` (`:71`).
- **Present.** The sleeve's bore guides the head: `WoundSpringContactTests.RealHeadContactLaunchesLoadsAndGuideRimStopsOversizeEntry` (symbol: the tube proxy above).
- **Present.** Flat printing as a `QuadMesh` on a face: `Mesh_Mark` in `elements/BothGate/both_gate.tscn:35–37`. Test: `FaceMarkingTests.EveryPrintedSurfaceIsPresentFrontFacingAndAttachedToTheRotatingFace`.
- **Narrower, owned here.** The matcher, `VisibleSolidColliderTests.EverySolidArtPartHasItsColliderAndNoColliderReachesBeyondTheArt`. It matches hollow art only against the base body (`:227`), accepts a tube's bore only from a tube mouth (`:379`; neither package has mouths) and has no class for art drawn by a span binding. H1–H3 below are test-harness code; none touches a solver law, the worker ABI, the wire or a presentation contract, so no engine slice precedes this one.

### Repairs, mesh by mesh

Measured in each scene; poses are part-local. Every `col_` node below is a `CollisionShape3D` with a shape sub-resource of the mesh's size, unless it says hidden mesh.

Separated proxies. Each drawn proxy keeps its place in the file as art under a new name, and a proxy of the old name follows it directly with the same transform:

| Package | Was (drawn proxy) | Art name | Proxy |
|---|---|---|---|
| `linear_pusher` | `Visual/col_Foot` (box 1.6 × 0.2 × 1), `Visual/col_Barrel` (box 1.2 × 0.65 × 0.65) | `Foot`, `Barrel` | `col_Foot`, `col_Barrel`: base colliders 0 and 1 |
| `wound_spring` | `Visual/col_Base` (box 1.55 × 0.2 × 1.35), `col_LeftRail`, `col_RightRail` (box 0.13 × 1.8 × 0.2) | `Base`, `LeftRail`, `RightRail` | `col_Base`, `col_LeftRail`, `col_RightRail`: base 0–2 |
| `wound_spring` | `Visual/col_Sleeve` (open cylinder r 0.49, length 1.8, wall 0.08) | `Sleeve`, keeping its `metadata/wall_thickness` | Hidden `MeshInstance3D` `col_visual_sleeve`, the same mesh resource, `metadata/wall_thickness = 0.08`: base 3 |
| `wound_spring` | `Visual/Head/col_Head` (sphere r 0.32) | `Head/Sphere` | `col_Head`: head collider 0 |
| `wound_spring` | `Visual/Shaft/col_Wheel` (cylinder r 0.26, length 0.1, axis Z) | `Shaft/Wheel` | `col_Wheel`, the same rotation: shaft collider 0 |

New colliders and print:

| Package | Mesh | Repair |
|---|---|---|
| `conveyor` | `Visual/PulleyOut/Wheel`: cylinder r 0.22, length 0.1, axis Z, centre (1.35, −0.07, 0.78); beyond the rail (z ≤ 0.64) and roller end (z ≤ 0.65) | `col_pulley_out` under `.`, r 0.22 h 0.1 at that position, quaternion (0.7071068, 0, 0, 0.7071068), after `col_support_b`: base collider 6. A binding turns the node about the wheel's own axis, which a static cylinder follows exactly (part A, D4) |
| `conveyor` | `Visual/Roller/PulleyIn/Wheel`: the same cylinder at roller-local (0, 0, 0.78); rides the roller's hinge body, no binding | `col_pulley_in` under `Visual/Roller`, same shape resource, position (0, 0, 0.78), same quaternion, after `col_roller`: roller collider 1 |
| `conveyor` | `PulleyIn/Spoke` and `PulleyOut/Spoke`: boxes 0.33 × 0.045 × 0.035 at z 0.06, 27.5 mm proud of the wheel face (z 0.05) | Both a `QuadMesh` 0.33 × 0.045 (`Mesh_PulleySpokePrint`) at z 0.0502, same names and material; `Mesh_PulleySpoke` is deleted. `load_steps` 16 |
| `linear_pusher` | `Visual/Indicator`: sphere r 0.065 at (−0.4, 0.2, 0.34) | `col_Indicator` under `Visual` |
| `linear_pusher` | `Socket_PowerIn` (−0.45, −0.4, 0.5), `Socket_ExtendIn` (0.15, −0.4, 0.5), `Socket_RetractIn` (0.55, −0.4, 0.5), `Socket_RetractedOut` (−0.45, 0.4, 0.45), `Socket_ExtendedOut` (0.45, 0.4, 0.45): spheres r 0.07 | `col_Socket_PowerIn` … `col_Socket_ExtendedOut` under `Visual`, one shared shape. With `col_Indicator` first, in this order, after `Socket_ExtendedOut`: base colliders 2–7. `load_steps` 20 |
| `linear_pusher` | `Visual/Head/Ring`: torus inner 0.235, outer 0.255 (centreline 0.245, tube 0.01), axis X, at the head's centre; 5 mm proud of the 0.25 m head | Hidden `MeshInstance3D` `col_visual_ring` under `Visual/Head`, the same mesh resource and transform, after `col_Rod`: head collider 2 |
| `linear_pusher` | `TravelMark0`–`4`: boxes 0.022 × 0.1 × 0.012 at z 0.333, standing 2–14 mm off the barrel face (z 0.325) | Each a `QuadMesh` 0.022 × 0.1 at z 0.3252, same x, name and material |
| `wound_spring` | `Visual/ActivationSocket`: sphere r 0.065 at (0.62, −0.55, 0.55) | `col_ActivationSocket` under `Visual`, after `ActivationSocket`: base collider 4. `load_steps` 22 |
| `wound_spring` | `Head/ChargeIndex`: box 0.19 × 0.045 × 0.035 at head-local (0.6, −0.1, 0.14), 22.5–57.5 mm off the right rail's face (z 0.1) and wider than the 0.13 m rail | `QuadMesh` 0.12 × 0.045 at head-local (0.6, −0.1, 0.1004), same name and material |
| `wound_spring` | `Mark0`–`4`: boxes 0.18 × 0.018 × 0.025 at z 0.12, 7.5–32.5 mm off the same face, wider than the rail | Each a `QuadMesh` 0.1 × 0.018 at z 0.1002; x 0.6 and the `stroke` height bindings unchanged |
| `wound_spring` | `Shaft/Spoke`: box 0.42 × 0.035 × 0.025 at z 0.055 on the shaft's hinge body, 17.5 mm proud of the wheel face (z 0.05), no binding | `QuadMesh` 0.42 × 0.035 at z 0.0502, same name and material |

Print layers are part A's: 0.2 mm, and 0.4 mm for the index because it slides over the marks. Every mark stays inside its face over its whole travel: rail x 0.535–0.665 and y −0.95–0.85; marks and index at y 0–0.8.

`Specs` rows (solid; hollow; arc; decorative; moving roots; spanned):

- `conveyor` (replaces 88–91): the seven present entries plus `"Visual/Roller/PulleyIn/Wheel"` and `"Visual/PulleyOut/Wheel"`; none; none; `["Visual/Roller/PulleyIn/Spoke", "Visual/PulleyOut/Spoke"]` (printed flat; the output one also turned by the binding); `["Visual/Roller"]`.
- `linear_pusher`: `["Visual/Foot", "Visual/Barrel", "Visual/Head/Sphere", "Visual/Indicator", "Visual/Socket_PowerIn", "Visual/Socket_ExtendIn", "Visual/Socket_RetractIn", "Visual/Socket_RetractedOut", "Visual/Socket_ExtendedOut"]`; `["Visual/Head/Ring"]`; none; `["Visual/TravelMark0"`–`"4"]` (printed flat); `["Visual/Head"]`; spanned `["Visual/Rod"]`.
- `wound_spring`: `["Visual/Base", "Visual/LeftRail", "Visual/RightRail", "Visual/Head/Sphere", "Visual/Shaft/Wheel", "Visual/ActivationSocket"]`; `["Visual/Sleeve"]`; none; `["Visual/Head/ChargeIndex", "Visual/Shaft/Spoke", "Visual/Mark0"`–`"4"` (printed flat), `"Visual/Latch/Lever"` (turned by the retention binding), `"Visual/Coil/*"` (drawn by compression)`]`; `["Visual/Head", "Visual/Shaft"]`.

### Matcher changes (test harness)

- **H1, hollow art on a moving body** (theory, 223–230). A hollow entry under a moving root is matched in the pool of that moving body, by the rule solids use. Each moving body has its own pool, keyed by its root; the theory asserts that the row's moving roots are the scene ownership's moving bodies in order (sliders first, then hinges), so art is never matched against another moving body's colliders (review pass 1, BH1, F4). No existing row has hollow art under a moving root, so it can only add a failure.
- **H2, a declared bore** (`Solid` 61, `Art`, `MatchesHollow` 379). `Solid` gains `float? Inner`. For an open `CylinderMesh` of equal radii whose own art node carries `wall_thickness` metadata, `Inner` is (radius − wall) × scale. The Tube arm requires `Same(h.X, Inner)` when `Inner` is present, and in addition a mouth bore whenever the entry has mouths. The art node and the hidden proxy are two nodes, each stating the wall, so the row fails if they disagree. This is a declared-bore consistency check, like the mouth rule, not proof of a drawn bore: the delivered convention (pipe, powered gate, ball detector) draws a tube as one open surface with its bore declared in data. The bore's behavioural proof is `WoundSpringContactTests.RealHeadContactLaunchesLoadsAndGuideRimStopsOversizeEntry` (review pass 1, BH2, F1).
- **H3, spanned art** (`ArtSpec` 54, theory). `ArtSpec` gains `string[]? Spanned`, counted in the classification at 213. `Art` also returns each moving root's scene transform, with its presentation parameter bindings applied. For each spanned path: the entry's `Presentation.Spans` has exactly one span with that `SpanNode`; the entry has exactly one clipped extrusion (`CuriousContraptions.Bundle/BundleSchema.cs:41`) and its `MaterialBody` b satisfies `spec.MovingRoots[b − 1] == span.MovingNode`, and the moving root at index b − 1 is the root of body b in the entry's scene ownership (sliders first, then hinges, `SceneGeometryExtractor.cs:43–46`); H3 asserts that, so a reordered row fails instead of selecting another body; the collider is that body's collider at the extrusion's `Collider` index; posed by the transform of `span.MovingNode`, it must satisfy `Matches` against the drawn mesh at `Tol + Fit`; it is then removed from that body's pool, and a second spanned path using the same extrusion is refused, so each extrusion's collider is removed at most once (review pass 1, ECH1). It checks the authored pose only; extension is `ClippedExtrusionTests`' subject.

What these remove, and what catches a failure. No protection is deleted. The matcher still skips every `col_` mesh (269) and no scene has a visible one after this slice; `docs/engine-contracts.md:88`, `DESIGN.md:83` and `docs/hollow-geometry-design.md:32` stay true without edit. A proxy left visible is caught by eye and, from `ENGINE-ART-SOLIDITY`, by the build. H1–H3 add checks and exempt nothing: without H3 the rod collider fails as "reaches beyond the art"; H3 passes only when a declared span draws that art, a declared extrusion names that collider and the two coincide.

### Footprint, capture and occlusion

- No declared `ElementFootprint` (bodies and joints), pick radius or port moves.
- Placement extent follows the colliders: the Conveyor's +z reach grows from 0.64 to 0.83 m and the pusher's from 0.5 to 0.57 m; every authored Conveyor stays inside the bench (run-log E18).
- The Conveyor's input wheel rides the roller's dynamic body, so a ball resting on it loads the roller; base and moving bodies do not collide and the Mechanical link has no collider, so the drive relay is untouched.
- Construction refuses a dynamic body that starts inside a hollow wall (`engine/gpu/WorkshopPhysicsCompiler.cs:498–523`): cargo authored within 5 mm of the pusher head's equator, where the new ring lies, is now refused.
- No part here is an airflow source or receiver; the new colliders occlude a Fan's stream like any solid.
- Dispatch re-check (planning scout, run-log E3): every test that places these parts with a dynamic body is clear of the new colliders; authored Conveyor puzzles are `Planned` and never constructed; each dynamic body's first collider stays first; new ids avoid `HalfPrecisionGuardTests`' guarded words.

### Decisions

Owner decisions (21 November 2026): sockets, indicators and wheels get sphere or cylinder colliders; rings get torus colliders; raised markings and the charge index are redrawn flush.

Coordinator decisions (22 November 2026, under the owner's standing delegation), each with its reason:

- **K1. Drawn proxies are separated, not accepted.** The three active documents say a proxy is a separate node that is not rendered; a repair may not approve a violation as the solution, and this removes a matcher rule instead of adding one.
- **K2. Both Conveyor spokes become print.** One part's two pulleys should not differ; the output spoke stays decorative as printed flat and turned by its binding.
- **K3. Print uses part A's 0.2 mm and 0.4 mm layers under the 0.5 mm bound** (`FaceMarkingTests.cs:60`): one quantity, one tolerance across the story.
- **K4. The Conveyor joins part B**; art turned by a presentation binding is decorative (part A, D5).

Decided here, with reason:

- **D1. Split.** Tennis ball to part B2 (above).
- **D2. New proxies sit beside their art;** the indicator's three colour bindings and every art path other than the eight renamed nodes are untouched.
- **D3. The spring's stroke marks are redrawn flush.** They are positioned by a parameter, not turned at Run, so they are static and proud; part A's D5 makes them solid or flush. `spec-engine-art-solidity.md` lists them as "moved by bindings" and is corrected at its replanning. Its task 6 line "visible proxy meshes as art" is withdrawn by K1 and is corrected at the same replanning.
- **D4. The spring's shaft spoke is redrawn flush.** It rides a hinge body with no binding, the same case as the Conveyor's input spoke.
- **D5. Marks narrower than drawn.** The spring's marks (0.18 m) and index (0.19 m) overhang the 0.13 m rail; printing cannot overhang a face, so they become 0.1 m and 0.12 m.
- **D6. The head ring has no play test.** It is 5 mm proud, under the class's 17 mm stop allowance and under the owner's accepted 1 cm band; a test that told it from the sphere would assert precision with no gameplay meaning. Proof: the compiled shape (B5) and the capability test.
- **D7. One socket is dropped on (B3); the indicator and the other four are proven by shape (B4),** being the same primitive on the same body with the same material.
- **D8. Collider growth is accepted and recorded:** conveyor base 6 to 7 and roller 1 to 2; pusher base 2 to 8 and head 2 to 3; spring base 4 to 5.

Floating art is seated in repair part D (`spec-elements-visible-solid-remainder-d.md`; coordinator decision 39, 23 November 2026, consistent with coordinator decision 33 for the Counter): the pusher's two upper sockets stand 7.6 cm clear of the barrel; the spring's socket, shaft wheel and `Latch/Lever` stand 0.35 to 0.39 m in front of its rails. The ports are declared at those points, so seating them moves ports, as part D does with each node's proxy, locator and port together. This part adds the colliders at the drawn positions and moves no node or port; part D moves them after the engine programme.

## Tasks & Acceptance

**Execution:**

0. Before any edit -- build at the baseline and copy `elements/elements.bundle.json` to `.tools/visible-solid-remainder-b-evidence/bundle-baseline.json` -- AC2's baseline.
1. `elements/LinearPusher/linear_pusher.tscn`, `elements/WoundSpring/wound_spring.tscn` -- separate the eight drawn proxies as tabled -- B8.
2. `CuriousContraptions.tests/VisibleSolidColliderTests.cs` -- make H1–H3; where `Art` skips a `col_` mesh (269), assert that the skipped mesh's own `Visible` is false; bring the class, `ArtSpec` and `Art` summaries up to date -- prerequisite of the rows; AC6 on every run.
3. `elements/Conveyor/conveyor.tscn` -- add `col_pulley_out` and `col_pulley_in` with one `CylinderShape3D`; replace `Mesh_PulleySpoke` with `Mesh_PulleySpokePrint` on both spokes; `load_steps` 16 -- B1, B2, B9.
4. `elements/LinearPusher/linear_pusher.tscn` -- add the six sphere proxies and `col_visual_ring`; make `Mark` a `QuadMesh` and move the five marks to z 0.3252 -- B3–B5, B9.
5. `elements/WoundSpring/wound_spring.tscn` -- add `col_ActivationSocket`; make `Mark`, `Index` and `Spoke` quads at the sizes and positions above, written as the f32 expansions the file uses -- B7, B9.
6. `CuriousContraptions.tests/VisibleSolidColliderTests.cs` -- replace the `conveyor` row, add the two rows; add `Feature` members `ConveyorOutputPulley`, `ConveyorInputPulley` and `PusherSocket` with their `Drop` arms (no `WoundSpringSocket`: B7 is proven by shape and by part D's Tennis ball); add the theory `ABallDroppedOntoEachLowRestitutionFeatureStopsAtItsDrawnSurface` and its helper -- B1–B3, B7, B11.
7. `CuriousContraptions.tests/ConveyorTests.cs`, `LinearPusherArtTests.cs`, `WoundSpringArtTests.cs` -- add the three printed-marking facts; at `WoundSpringArtTests.cs:41` read `Visual/Sleeve`; change the literal at `:58` to `(.6f, -.1f, .1004f)` -- B9, B12.
8. The three element documents, `deferred-work.md`, `delivery-batches.md`, `TODO.md` -- record each collider with size and pose, the separated proxies, the redraws, the counts of D8, the floating art as owned by repair part D (coordinator decision 39; no deferred-work entry), and parts B2 and C as queued.

**Acceptance Criteria:**

- AC1: Given the three packages, when the visible-solid theory runs, then each row passes and the theory has two more cases than at the baseline.
- AC2: Given the baseline copy and the candidate's bundle, when each entry's decoded colliders are compared, then the only differences are `conveyor` (one more base and one more roller collider), `linear_pusher` (six more base, one more head) and `wound_spring` (one more base); every collider present at the baseline is equal, in the same order, and every other entry is equal.
- AC3: Given the named existing tests (verification list), when they run, then all pass with no edit other than the two in task 7.
- AC4: Given the candidate, when the full local round runs, then every project reports zero failed and `Skipped: 0`, and the total is the baseline total + 44 = 7353: the slice's own two theory cases, three drops and three facts; two `-col_` refusal cases (decision 2); eight generator refusal cases and an ownership fact, later a two-case theory (decisions 3 and 4); eight parser cases (passes 3 and 4); a separator case and two mouth-rule cases (E17); seven pass 6 cases (E19: three ownership, two tube-mouth letter case, before the first section, `mouth_start`); and six pass 7 cases (E23: two `position`-posed locators, a lower-case prefix, a root named as a locator, a continued value, an unclosed header). Decision 5 deleted the continuation and escaped-quote tests.
- AC5: Given the candidate diff, when its paths are listed, then none is under `engine/`, `CuriousContraptions.Simulation/`, `CuriousContraptions.Bundle/`, `schemas/`, `tools/`, `DESIGN.md`, `docs/engine-contracts.md` or any declaration file, except the bounded exceptions of coordinator decisions 2 to 4 (25 November 2026, Spec Change Log): `tools/BundleElements/SceneGeometryExtractor.cs`, `tools/BundleElements/BundleGenerator.cs`, `tools/BundleElements/PackageValidator.cs` (the tube-mouth rule, the shared scene reader, the port-id refusal and the deleted `Validate`), `CuriousContraptions.Abstractions/BundleServices.cs`, the `tools/BundleElements.Tests/` test files and fixtures they touch, `DESIGN.md`, and comment-only lines in `CuriousContraptions.Simulation/WorkshopCollisionKernels.cs` and `CuriousContraptions.Abstractions/PhysicsDeclarationValues.cs`.
- AC6: Given every `elements/*/*.tscn`, when its `col_` mesh nodes are listed, then each has `visible = false`.

Counts in AC1 and AC4 are baseline + delta, counted on the slice's baseline at start, so the order in which parts B2, C and the Baseball repair publish does not change them.

## Implementation Notes

- Owners, each fresh (`general-purpose`, no inherited history, inherited model) on the same uncommitted candidate over `37fbd3a4`: the implementation, agent `ac27809550dff4831` (run-log E5; lost with the coordinator's stalled session, E6); pass 1 and decisions 1 and 2, `a81d5a807038b45ae` (E8); pass 2, decision 3 and passes 3 and 4, `a5ba9c1812207a6a6` (E10, E12, E15); decision 4 and pass 5, `ae92c761ef02a7737` (E17).
- Pass 7 repair with decisions 5 and 7: a fresh owner (E23).
- Capability probe re-checked before the first edit: nothing returned to planning. Self-check of the owned diff: no needless branch, duplicate rule or unrelated churn; one collider source, one tube-mouth name rule (`PackageValidator.TubeMouth`), one scene-text value rule. Deviations and rules in detail: run-log E18.

## Spec Change Log

All entries 25 November 2026, from the lane coordinator (run-log, handled messages). Intent unchanged; baseline `37fbd3a4`, the KEEP constraints (every collider index, the specified repairs, the drop and print bounds) and known-good work preserved.

- **Decision 1 (a), pass 1: the static output-wheel collider is accepted as specified** (BH4 and duplicates): belt transport comes from the carrier, as with part A's Pulley rim; added the `ReverseTransmissionArtTests` centre-and-axis assertion and a `deferred-work.md` entry (surface velocity; a ball touching the spinning rim gets no tangential push).
- **Decision 2 (a), pass 1: bounded AC5 exception** (FO1, F2b): the `-col_` infix rule deleted and refused, with a test; the infix removed from `DESIGN.md`, `docs/general-engine-design.md` and `spec-engine-art-solidity.md`; two stale "static only" comments corrected.
- **Decision 3 (b), pass 2: every extractor refusal typed** (EFE-A, FO2-3): one typed exception carrying a `BundleErrorCode`, grouped by kind, reported by `BundleGenerator`; generator-level tests per code, not per call site. Ratified (E11): the internal `Generate` overload; art-solidity's planned codes moved to 20–23.
- **Decision 4 (a), pass 5: one collider source** (FO4-1, FO5-2): scene ownership required, declaration-data colliders refused at bake, the data-only branch deleted, fixtures scene-owned, one tube-mouth name rule; pass 6 added one scene reader and deleted `PackageValidator.Validate`.
- **Decisions 5 to 8, pass 7 (owner-ruled on this lane's owner channel; run-log E22, E25):** 5, the parser refuses any non-header, non-blank, non-comment line without ` = ` as `MalformedScene`, and the continuation and escape machinery (`Scan`) is deleted with its tests; 6, the declaration `Colliders` member (EFE7-1) is deleted by lane 1's next slice, before B2 (`deferred-work.md`); 7, `DESIGN.md:92` says KHR is planned, not admitted, and the planned-scope question goes to the `ENGINE-CORE-2d` replanning; 8, after this repair, a finding about malformed scene text that no shipped scene contains and that breaks no hard rule goes to `ENGINE-ART-SOLIDITY` by name. Cross-lane checks: run-log, handled messages.

## Review Triage Log

- Pass 1, 25 November 2026 (all six layers; run-log E6): 25 findings, high 1, medium 8, low 12, false 4. Patched (E8): per-body moving pools, one use per extrusion, no binding on a `col_` node, the output wheel held under its binding, print colours, test and document wording. Rejected with evidence: BH6, IA-e, IA-d, FO2. Decisions 1 and 2 (Spec Change Log): the output pulley's surface motion; the `-col_` infix rule (FO1, high) and two stale comments.
- Pass 2 (five layers; run-log E9): 12 findings; patched (E10): aliases, citations, a control case, records; three rejected; decision 3.
- Pass 3 (five layers; E13): 14 findings; patched (E12): typed exception, code assertions, parser tests, `/` in names; one rejected.
- Pass 4 (five layers; E14): 22 findings; patched (E15): summaries, wording, one case; seven rejected; per-branch refusal tests and node names deferred under Re-review scope (`deferred:`); BH4-6 and FO4-1, first deferred, repaired after pass 5.
- Pass 5 (Verification Gap and both auditors; run-log E16): FO4-1 confirmed, so its deferral is withdrawn (decision 4, with FO5-2); patched with it (E17): the separator rule (EFE5-2), records and a test summary.
- Pass 6 (Edge Case Hunter, Verification Gap, both auditors; run-log E20): 21 findings. Patched (E19): one scene reader, `mouth_` port ids refused, `PackageValidator.Validate` deleted, parser fixes, tests, records. Rejected: FO6-5. FO6-4 and FO6-6, first deferred, were reopened at pass 7.
- Pass 7 (the same four layers; run-log E21): Element-Free Fail on EFE7-1 (the declaration `Colliders` member), contested by Forward-Only; decisions 5 to 8. Patched (E23): the parser simplified, locator tests and messages, a root named as a locator refused, summaries, citations, `DESIGN.md:92`. Rejected: FO7 borderlines 1 and 2.
- Pass 8 (the same four layers; run-log E25): every pass 7 patch confirmed; Forward-Only no Fail. EFE8-1 owner-ruled (decision 6, a sequenced repair before B2); eight decision 8 findings recorded for `ENGINE-ART-SOLIDITY`; records corrected. No product finding open.

## Design Notes

1. **Behaviour delivered.** Nine drawn solids and one ring become solid; fourteen raised markings become printing; two packages join the visible-solid proof. Source: owner rule "visible = solid" (25 October 2026); owner decisions of 21 November and coordinator decisions of 22 November 2026.
2. **Reusable.** Nothing new in the game runtime. The matcher gains three package-neutral rules. The bundle tool's extractor refuses with one typed `SceneGeometryRefusalException` carrying a `BundleErrorCode` (four new members, 16–19), which the generator reports (decision 3); one tube-mouth name rule, `PackageValidator.TubeMouth`, and one scene-text parser, read by both the extractor and the locator check (decision 4).
3. **Declared.** Proxy nodes in each scene. No declaration and no engine identity changes.
4. **Execution.** Scene → `SceneGeometryExtractor.Bake` → bundle entry → the unchanged compiler and solver.
5. **Ownership.** Colliders on existing bodies; no new state. Reset, save and load are unaffected: colliders compile from the bundle.
6. **Generality.** H1–H3 read only scene ownership, node metadata, declared spans and declared extrusions.
7. **Deleted.** Fourteen raised boxes with `Mesh_PulleySpoke` and the `Mark`, `Index` and `Spoke` box resources; the eight drawn proxies as a pattern; the `-col_` infix proxy-name rule (decision 2); the generator's catch-all that reported every extractor refusal as `BUNDLE004` (decision 3); the data-only bake (`ownership is null`) and its refusals, the extractor's own mouth switch, the validator's `TubeMouths` set and regex scene reader, `PackageValidator.Validate` (decision 4, passes 5 and 6); the parser's continuation and escape state, `Scan` (decision 5). No raised copy, visible proxy, second matcher path, second proxy-name rule, second collider source or second scene reader remains.
8. **Uncertain.** None left from planning: the slider body's 0.01 m torus is admitted by the bundle and by every pusher construction in the full round, and B2's hinge holds the axial load inside the unchanged bounds (run-log E5). Rules E1, E2 (typed codes end to end), E4 (a refused scene writes no bundle), E6, E7 and E8 apply; E9 passed.

## Verification

Executor for every command: the implementation owner. Raw output goes under `.tools/visible-solid-remainder-b-evidence/`.

Environment: the pinned toolchain first on `PATH`: `PATH=$HOME/.local/share/tim-toolchain/dotnet-10.0.403:$HOME/.local/share/tim-toolchain/node-v26.12.1-darwin-arm64/bin:$PATH` and `DOTNET_ROOT=$HOME/.local/share/tim-toolchain/dotnet-10.0.403` (on this host the default `PATH` has Homebrew node v26.13.1 and a dotnet without `wasm-tools`, so the capture's node check and the web build fail otherwise); record the session environment check in the run log. Work only in `/Users/aidan/dev/personal/tim/.claude/worktrees/slice-visible-solid-b`: start every shell command with `cd` there, give Anvil that `workspaceRoot`, and never touch another checkout. Compiler logs go to `CSHARP_STATIC_GATE_BINLOGS=$HOME/.local/share/tim-toolchain/logs/slice-visible-solid-b`, raw round output beside them (other lanes run rounds on this host). The implementation owner does not commit.

### Acceptance-to-test table

The low-restitution theory asserts the stop only (the product restitution, at most 0.0275, is below what the rebound bracket can tell from zero): the same bench, 0.5 m fall and 180 ticks; the first landed read lies in [rest − 17 mm, rest + 5 mm], the class's own two bounds. Derivation in run-log E20. `Back` puts a part-local point (x, y, z) at (x, z, −y).

| Rows | Named discriminating test | Control and boundary | Run |
|---|---|---|---|
| B1 | Low-restitution theory, `Feature.ConveyorOutputPulley` = (`conveyor`, `Back`, top 0.78 + 0.05, X 1.35, Z 0.07) | Without the collider the ball stops on the idle roller's end, 0.18 m lower. Boundary: a flat face on a static body | passed: scoped run and full round (run-log E5); again in `scoped-2` and `full-round-2` (E8) |
| B2 | Same theory, `Feature.ConveyorInputPulley` = (`conveyor`, `Back`, 0.83, X −1.35, Z 0.07) | Without it the ball stops on the roller's end, 0.18 m lower. Boundary: a face on a hinged body, struck on its axis | passed, inside the unchanged bounds (no hinge-support finding) (E5; again E8) |
| B3 | Same theory, `Feature.PusherSocket` = (`linear_pusher`, identity, 0.4 + 0.07, X −0.45, Z 0.45) | Without it the ball stops on the barrel's edge at 0.325 + √(0.34² − 0.125²), 0.169 m lower; the indicator is 0.62 m from the resting centre. Boundary: a sphere apex, first stop only | passed (E5; again E8) |
| B7 | `VisibleSolidColliderTests.EverySolidArtPartHasItsColliderAndNoColliderReachesBeyondTheArt`, case `wound_spring` (its `Visual/ActivationSocket` entry) | No drop case: once part D seats the socket on the rail's face (the spring on its back), the sleeve's end ring is 0.15 m behind the ball's centre plane and a Basketball's stop (0.505 m) is only 7.2 mm above the ring's (0.4978 m), so no bound could tell the socket from the ring; part D drops a Tennis ball on the seated socket. Boundary: a sphere on a static body, as B4 | passed (E5; again E8) |
| B4, B5, B6, B8, B11, AC1 | `VisibleSolidColliderTests.EverySolidArtPartHasItsColliderAndNoColliderReachesBeyondTheArt`, cases `conveyor`, `linear_pusher`, `wound_spring` | Each row fails with "solid art without a matching collider" if a proxy is absent, and with "reach beyond the art" if H3 or a proxy's art is absent. Each moving body is matched against its own pool, its root asserted against the scene ownership order (BH1), and a second spanned use of one extrusion is refused (ECH1). Boundary: torus and rod on the slider body; a tube with a declared bore (both nodes' wall agree) | passed; the theory has the two new cases (E5); all 40 cases with the per-body pools (E8) |
| B8, AC2, AC6 | The AC2 comparison (command below); for AC6 the assertion in `Art` that every skipped `col_` mesh is hidden and that no presentation binding (bindings, bodies, spans, parameters) targets a `col_` node (ECH3), run by every theory case, and the `git grep` listing for the packages outside the table | The baseline's colliders are the control: equal and in order | AC2: exactly the three planned differences, every baseline collider equal and in order; AC6: all 48 `col_` meshes hidden (E5; again `ac2-2`, `ac6-2`, E8) |
| B9 | `ConveyorTests.BothPulleySpokesArePrintedFlatOnTheirWheels`, `LinearPusherArtTests.TheTravelMarksArePrintedFlatOnTheBarrelFace`, `WoundSpringArtTests.TheMarksIndexAndSpokeArePrintedFlatOnTheirFaces` (new facts): each named node is a `QuadMesh` facing +Z, its plane between 0 and 0.5 mm in front of its face, its extent inside the face, at the authored size, in the colour the scene authored for the raised box (gold, or navy for the spring's marks; drawn material albedo, IA-c); the index lies in front of the marks | 0.5 mm is the bound `FaceMarkingTests.cs:60` uses for print (delivered stand-off 0.25 mm, `both_gate.tscn:100`–`:101`); the thinnest old box (12 mm) fails; a print in another colour fails | passed (E5); with the colour assertion (E8) |
| Review repair VG1 (decision 1) | `ReverseTransmissionArtTests.NativeSceneAppliesWheelPosesConveyorWitnessAndEvaluatedLinkArtwork` (both cases): after Run, `Visual/PulleyOut/Wheel`'s global centre within 5 mm and its axis within 5 mm / 0.22 m of their authored values, while the spoke's axis has moved further | The 5 mm drop bound of the visible-solid class. Control: the spoke has turned by more than the axis bound. Boundary: the bound is a rim point's travel at the wheel's 0.22 m radius | passed (`scoped-2`, `full-round-2`, E8) |
| Review repair FO1 (decision 2, AC5 exception) | `SceneGeometryExtractorTests.ANodeNamedWithAColInfixInsteadOfThePrefixIsRefused` (`Box-col_box`, `Box-COL_box`): refused with the reason naming `Visual/<name>` | Control: the same box mesh named `col_box` bakes one collider. Boundary: either case | passed (`scoped-2`, `full-round-2`, E8; `full-round-5`, E15; `scoped-6`, `full-round-6`, E17) |
| Review repair EFE-B | The same theory: a prefixed name that also carries the infix, `col_box-col_b`, bakes one collider, and the refusal carries `ProxyNameInfix` | Deleting the prefix guard refuses `col_box-col_b`. Boundary: the infix inside a prefixed name | passed (`full-round-3`, E10) |
| Review repair EFE-A, FO2-3 (decision 3) | `SceneGeometryExtractorTests.AnExtractorRefusalReachesTheBuildUnderTheCodeOfItsKind` (eight cases through `BundleGenerator.Generate` over one fixture package): `Box-col_box` yields exactly `ProxyNameInfix` (`BUNDLE016`) and no bundle; a body without a collider still yields `BUNDLE004`; one case each for `DuplicateId`, `NonFinite`, `OutOfRange`, `UnresolvedReference`, `MalformedScene` and `UnsupportedColliderProxy`. `SceneColliderOwnership` at generator level: `SceneGeometryExtractorTests.APackageWhoseCollidersAreNotItsScenesIsRefusedAndWritesNoBundle` (the decision 4 row), which replaced `SceneProxiesBesideDataCollidersReachTheBuildAsAnOwnershipRefusal` (E17). `MissingAndUnknownMouthsAreRejected` asserts `UndeclaredPortLocator` at extractor level for an unknown and a duplicate mouth (the generator's locator check refuses that locator first, under the same code). Pass 3 repairs (E12): `SceneGeometryExtractorTests.UnreadableSceneTextIsRefusedUnderItsCode` (eleven cases: a value or section attribute stated twice, a reused sub-resource id, a sub-resource without its id or its type (pass 4, E15), a nameless node, a node name containing `/`, a line without the ` = ` separator (EFE5-2, E17), a value continued onto a later line and an unclosed section header (decision 5, E23), and a value line before the first section (E19)); the reference `SubResource(")` is a `MalformedGeometryIsRejected` case, `UnsupportedColliderProxy` (E23); and every existing extractor refusal assertion names its expected code | Control: the unedited scene generates a bundle with no error. Each generator case asserts the one error's exact code, a detail starting `Fixture: ` (the validator's own `BUNDLE004` reads `Fixture.Colliders: …`) and, where the refusal has one, the refused node or value; the exception derives from `Exception`, so an untyped `ArgumentException` fails every refusal assertion | passed (`scoped-3`, `full-round-3`, E10; `scoped-4`, E12; `scoped-5`, `full-round-5`, E15; `scoped-6`, `full-round-6`, E17; `scoped-7`, `full-round-7`, E19; `scoped-9`, `full-round-9`, E23) |
| Review repair decision 4 (FO4-1, FO5-2), EFE5-2 and the pass 6 repairs | One collider source: `SceneGeometryExtractorTests.APackageWhoseCollidersAreNotItsScenesIsRefusedAndWritesNoBundle` (two cases through `BundleGenerator.Generate`): a scene-owned declaration that also states a data collider, and a declaration that is not `ISceneGeometryElement`, each yield exactly one `SceneColliderOwnership` error with detail `Fixture: …` and no bundle (Json null); `DeclarationDataCollidersAreRefusedOnEveryBodyWhateverTheScene` (extractor): data colliders on the base with a proxy scene and with an art-only scene (which the deleted `ownership is null` branch baked from the data), and on a slider's and a hinge's moving body (E19), are refused as `SceneColliderOwnership`; `AnOwnershipThatDoesNotNameTheBaseAndEachMovingBodyOnceIsRefused` (E19, three cases): an ownership of the wrong count, a first root other than ".", and a moving root nested under another, each `SceneColliderOwnership`. One mouth rule: `ADeclaredPortNamedLikeATubeMouthIsTreatedAlikeByTheLocatorCheckAndTheExtractor` (`mouth_x`, `mouth_start`, `socket_x`): a declared port with the `mouth_` prefix is refused on its id (`OutOfRange`, E19); the locator `mouth_x` is refused by `PackageValidator.Locators` and by the extractor, both as `UndeclaredPortLocator`; the locator `mouth_start` is a tube mouth to both and bakes. `ATubeMouthNameIsMatchedWithItsCase` (E19): `Loc_Port_Mouth_end` is refused by the locator check as `UndeclaredPortLocator` and bakes no mouth. One scene reader (E19): `PortLocatorTests`, including `EveryRealPackageSceneAgreesWithItsDeclaredPorts`, now read locators through the extractor's parser; an unreadable locator pose is `MalformedScene`, naming the locator (`ANestedOrMalformedLocatorFailsTheBuild`, E23). Pass 7 (E23): `ALocatorPosedByPositionIsReadAtThatPoint` (admitted at its `position`, `PortLocatorDisagrees` elsewhere), `ALowerCaseLocatorPrefixIsNoLocator` (`MissingPortLocator`), `ASceneRootNamedAsALocatorFailsTheBuild` (`PortLocatorDisagrees`, naming it). Parser: the separator, continued-value and before-the-first-section cases of `UnreadableSceneTextIsRefusedUnderItsCode` (`MalformedScene`); decision 5 deleted the two continuation facts (E23) | Controls: the scene-owned fixture without a data collider generates, in each case; the box scene without data colliders bakes its proxy, and the art-only scene without them is refused as `BodyWithoutCollider`, not as ownership; `MovingSubtreeBelongsOnlyToItsDeclaredBody` bakes a valid ownership; `socket_x` is accepted by both checks and bakes no mouth; `Loc_Port_mouth_end` is accepted and bakes the End mouth; a child locator at the declared anchor or the origin is admitted. Boundaries: data colliders on a moving body only; a declaration refused by type before any bake; `mouth_start` declared as a port (a valid mouth name, refused as a socket) | passed (`scoped-6`, `full-round-6`, E17; `scoped-7`, `full-round-7`, E19; `scoped-9`, `full-round-9`, E23) |
| B12 | Existing. Conveyor: `ConveyorTests` (16, including `PoweredTransport_AcceleratesBallAlongPositiveX`, `OffBeltMiss_SideContactRemainsOrdinaryRigid`, `ChainedConveyors_DriveRelaysRotationToDownstreamConveyor`, `TheRollerArtFollowsTheCommittedRollerPoseAndResetsToItsScenePose`), `ReverseTransmissionTests`, `ClutchTests`, `ClutchGeneralityTests`, `WindmillIntegrationTests.PaidShaftTransportsRemoteCargoAndReverseStagesControlDirection`. Pusher: `LinearPusherTests` (13, including `LosingPowerLocksTheIntermediatePositionAndRetractResumesFromIt`, `ObstructionStopsTheHeadAndReportsBlockedWhileAnAdjacentWallDoesNot`), `LinearPusherIntegrationTests`, `LinearPusherArtTests`, `ClippedExtrusionTests`. Spring: `WoundSpringControlTests`, `WoundSpringContactTests`, `WoundSpringIntegrationTests`, `WoundSpringLifecycleTests`, `WoundSpringArtTests`, `RetentionBoundaryTests`, `RetentionRequestTests`, `LaunchLessonSolveTests` (the two playable Wound spring lessons). All: `F32BehaviourIdentityTests`, `PackageRuntimeParityTests`, `BundleRoundTripTests`, `PalettePlacementTests`, `WorkshopIntegrationHarnessTests` | Existing | passed, with only the two task-7 literal edits (E5; the roller-art test is in `ConveyorArtTests`, added to the scoped filter); again E8 |

**Commands:**

- The compiler-proof capture in `docs/delivery-workflow.md#reproducible-local-verification`, up to and including the `verify-policy.binlog` build -- expected: five logs, zero warnings and errors; the bundle regenerates.
- `dotnet run --file tools/anvil/VerifyPolicy.cs -- --maintainability --project-root "$PWD"` -- expected: PASS with both canary `PASS` lines and the `Coupling dependencies:` summary.
- The registration check in `docs/delivery-workflow.md#new-package-registration-check` -- expected: 0 failed, 0 skipped.
- The AC2 comparison of `bundle-baseline.json` with the candidate's bundle, by decoded entry -- expected: exactly the three differences listed.
- `git grep -n -A6 -E 'node name="col_[^"]*" type="MeshInstance3D"' -- 'elements/*/*.tscn'` -- expected: every match is followed by `visible = false` before the next node (AC6).
- `dotnet test CuriousContraptions.slnx -c Debug -f net10.0 --no-build --no-restore --nologo -noAutoResponse --filter "FullyQualifiedName~VisibleSolidColliderTests|FullyQualifiedName~ConveyorTests|FullyQualifiedName~LinearPusher|FullyQualifiedName~WoundSpring|FullyQualifiedName~ClippedExtrusionTests|FullyQualifiedName~ReverseTransmission|FullyQualifiedName~Clutch|FullyQualifiedName~Windmill|FullyQualifiedName~Retention|FullyQualifiedName~PalettePlacementTests|FullyQualifiedName~WorkshopIntegrationHarnessTests|FullyQualifiedName~F32BehaviourIdentityTests|FullyQualifiedName~PackageRuntimeParityTests|FullyQualifiedName~BundleRoundTripTests|FullyQualifiedName~FaceMarkingTests|FullyQualifiedName~LaunchLessonSolveTests"` -- expected: 0 failed, 0 skipped; every test in the table appears.
- `git add -N` this contract and its run log if untracked, then `git diff --check <baseline_revision>` -- expected: no output.
- One full local round under `caffeinate -i` -- expected: the AC4 total, 0 failed, `Skipped: 0` for every project.

Results: each candidate's checks are in its run-log entry (E5, E8, E10, E12, E15, E17, E19, E23); the final candidate's in the Auto Run Result. Inputs that invalidate reuse: any scene, test or production source change.
- At the candidate commit, the unmodified `.githooks/pre-commit` -- expected: pass.

**Manual checks (if no CLI):**

- Open the three scenes: every proxy follows its body's earlier proxies in file order, and no `col_` mesh is visible.

### Running beside other work

It runs after part A and before parts B2 and C; part C reuses `Solid.Inner` (H2) and the low-restitution theory. It is serialised with parts A, B2 and C and the Baseball repair, which all edit `VisibleSolidColliderTests.cs`: one publisher at a time; the later merges main once and repeats the full local round on the merged candidate. It shares `deferred-work.md`, `TODO.md` and `docs/planning/invest/delivery-batches.md` with those slices and with S3, S5, S5b and S6–S6g, the other slices allowed before the engine programme (owner decisions 22 and 24h). It does not touch `content/puzzles.json`, the test project file or any registration table.

## Auto Run Result

Status: as the frontmatter states (25 November 2026), after the repair of decisions 5 to 8 and review pass 7 (Spec Change Log). Final-tree evidence (run-log E23, raw `*-9`): capture five binlogs, 0 warnings, 0 errors; maintainability gate PASS (0 metrics, 0 coupling, both canary lines, `Coupling dependencies: contract=4670, model=4, intrusive=0; violations=0`); registration 767/0/0; scoped 1057/0/0; AC2 the three planned differences, every baseline collider equal and in order; AC6 48 of 48 hidden; `git diff --check` clean; full local round 7353 passed, 0 failed, `Skipped: 0` (WorkerAotGuard 17, BundleElements 323, Coverage 115, E2eRunner 256, game tests 6642), HEAD and HEAD-AFTER `37fbd3a4` with an identical status. Review passes 1 to 8 complete: Element-Free Scoped Pass, Forward-Only no Fail, no product finding open. Final diff scope: Pass (informed existing reviewer `a1cf1da5c37194151`, run-log E26): all 153 paths serve the intent, decisions 1 to 8 or confirmed repairs; nothing removed; no simplification justified. The candidate commit `dbaacad1` passed the normal pre-commit hook; with the publish token it is merged once with `origin/main` `51d46f7b` (run-log E27), and that merged commit is the publication candidate.

Remote build, artifact and deployment, browser, performance and release qualification are unverified (owner instruction of 29 October 2026).
