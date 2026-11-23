---
title: 'ELEMENTS-VISIBLE-SOLID-REMAINDER, part B: Conveyor, Linear pusher and Wound spring'
type: 'bugfix'
created: '2026-11-21'
status: 'draft'
baseline_revision: 'captured at dispatch (the main that contains the planning work)'
review_loop_iteration: 0
followup_review_recommended: false
context:
  - '{project-root}/docs/testing-guidance.md'
  - '{project-root}/docs/delivery-workflow.md'
  - '{project-root}/docs/engine-contracts.md'
  - '{project-root}/_bmad/custom/element-free-engine-design.md'
warnings: []
deferred: []
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

**Baseline.** Captured at dispatch (coordinator decision 38, 23 November 2026): `baseline_revision` is the published main that contains the planning work (and, by sequence, part A), and every scope guard and `git diff` in this contract compares against it. Drafted on `25df3cd5`, which is the drafting revision only, not a baseline. Precondition (coordinator decision 37, 23 November 2026): the planning work of 22–23 November 2026 (the `docs/planning/elements/readiness/` folder and the Repairs table of `delivery-batches.md` section 5) is published, and part A is published; the baseline is the main that contains both, captured by the coordinator before first implementation. Line numbers were measured on `8d1d68aa`, before part A's edits to `VisibleSolidColliderTests.cs`, and are re-read on that baseline; every file cited with a line number is unchanged between `8d1d68aa` and `25df3cd5` (re-checked 23 November 2026). Expected totals are baseline + delta against the current full local round.

Planning verdict: Scoped Pass (fourth audit, `docs/planning/elements/readiness/audits/audit-visible-solid-B-4.md`); fixes applied 23 November 2026. The fourth audit found the third audit's F1 and R1–R4 resolved and raised N1–N3 (frontmatter baseline, the floating-art reason, the art-solidity wording), all applied here, with the floating art settled by coordinator decision 39; they need no re-audit. Line anchors are re-read on part A's published main. Part D's fifth audit (`.tools/audit/audit-part-d-5.md`, F1) removed the Wound spring socket's drop case from this part: the total is baseline + 8 and B7 is proven by shape (see B7). Planning only; no test has run.

Split, and why. Part B as first scoped (Tennis ball, Linear pusher, Wound spring) plus the Conveyor is over one contract. This contract keeps the two packages whose proxies are separated, and the Conveyor, which needs no matcher change. Part B2 (`spec-elements-visible-solid-remainder-b2.md`) is the Tennis ball alone.

Owned scope:

- `elements/Conveyor/conveyor.tscn` -- `load_steps` 1, `Mesh_PulleySpoke` 45, `col_support_b` 84, `col_roller` 123, `PulleyIn/Wheel` 134, `PulleyIn/Spoke` 138, `PulleyOut/Wheel` 145, `PulleyOut/Spoke` 149.
- `elements/LinearPusher/linear_pusher.tscn` -- `load_steps` 1, `Mark` 32, `col_Foot` 50, `col_Barrel` 53, `Rod` 55, `Head/Ring` 62, `col_Head` 65, `col_Rod` 67, `Indicator` 71, `TravelMark0`–`4` 75–93, the five `Socket_…` 97–121.
- `elements/WoundSpring/wound_spring.tscn` -- `load_steps` 1, `Mark` 29, `Index` 33, `Spoke` 41, the six drawn proxies 85, 90, 95, 100, 110 and 124, `Head/ChargeIndex` 115, `Shaft/Spoke` 128, `Mark0`–`4` 402–424, `ActivationSocket` 427.
- `CuriousContraptions.tests/VisibleSolidColliderTests.cs` -- `ArtSpec` 54–55, `Solid` 61, `Specs` 63–162 (`conveyor` 88–91), the theory 184–234, `Art` 242–314, `MatchesHollow` 348–366, `Feature` 444–449, `Drop` 456–512, `AssertDropOnto` 647–678, and the class, `ArtSpec` and `Art` summaries.
- `CuriousContraptions.tests/WoundSpringArtTests.cs:41` (reads `Visual/col_Sleeve`) and `:58` (the index position literal), plus one new fact each in `WoundSpringArtTests.cs`, `LinearPusherArtTests.cs` and `ConveyorTests.cs`.
- `docs/planning/elements/CAT-019-conveyor.md`, `CAT-039-linear_pusher.md`, `CAT-071-wound_spring.md`; `_bmad-output/implementation-artifacts/deferred-work.md` (the per-instance collider-growth entry whose source is `spec-elements-visible-solid-sweep.md`); `docs/planning/invest/delivery-batches.md` section 5 (Repairs, part B's row); `TODO.md`; this contract and its run log.

`elements/elements.bundle.json` is build output, not committed (`.gitignore:17`); task 0 captures a baseline copy.

Not consumers, checked (read-only):

- No declaration changes. `ConveyorElementDeclaration.cs` names carrier collider 0 for its driven surface and `LinearPusherElementDeclaration.cs:41` names head collider 1 for its clipped rod; both keep their place.
- The eight proxy names are read by one test only, `WoundSpringArtTests.cs:41` (`rg` over the repository, scenes excluded). No test reads `ActivationSocket`, `Socket_…`, `TravelMark…` or `Head/Ring`. `ConveyorTests.cs:642` reads the input spoke's transform relative to its authored value and is unaffected.
- `engine/PlacementPose.cs`: placement extent follows the colliders (`:46–60`, `:113–116`), so it changes with them (Footprint section).
- `content/puzzles.json`: a Wound spring in two puzzles (solution 58, placement 59), run by `WoundSpringIntegrationTests`, `WoundSpringLifecycleTests` and `LaunchLessonSolveTests` (`LaunchLessonSolveTests.cs:93` runs `wind_then_release` and `saved_for_later`, both `Playable`, through Run, Reset and save/load); eleven Conveyors in ten puzzles (`conveyor_courier`, `belt_relay`, `reverse_belt`, `air_and_belt`, `mixed_signals`, `chain_mail`, `double_cold_start`, `depth_delivery`, `double_bridge`, `grand_contraption`), which no test runs. By geometry a ball on the carrier touches a wheel only with its centre beyond z 0.59 of the 0.6 m half-width.

### Capability probe

Eight present, one narrower and owned here (test harness only); none missing.

- **Present.** Box, sphere and cylinder colliders baked from `CollisionShape3D` proxies: `SceneGeometryExtractor.BakeProxy` (`tools/BundleElements/SceneGeometryExtractor.cs:104`). Tests: `SceneGeometryExtractorTests`; in play, the `LatchSocket` case of `VisibleSolidColliderTests.ABallDroppedOntoEachSolidFeatureStopsAtItsDrawnSurfaceAndReboundsAtTheProductRestitution`.
- **Present.** A full torus and a tube baked from hidden mesh proxies, the tube with `metadata/wall_thickness`: `SceneGeometryExtractor.Hollow` (`:191–221`). Tests: `VisibleSolidColliderTests.ABallDroppedOntoTheBumpersUpperRingStopsAtItsDrawnTube`; the `powered_gate` row (hidden sleeve proxy).
- **Present.** Proxies are baked in file order for the body whose root they sit under: `SceneGeometryExtractor.BakeBodies` (`:57–68`); the pusher's head already carries two. Test: `LinearPusherTests.RotatedSphericalHeadPushesCargoAndLateralClearanceMisses`.
- **Present.** A torus child on a moving body in contact. A slider's or hinge's moving body compiles as a dynamic body (`engine/gpu/WorkshopPhysicsCompiler.cs:173`, `:214`), and a hollow collider meets a sphere, box or cylinder by one rule whatever body carries it (`CuriousContraptions.Simulation/WorkshopCollisionKernels.cs:1999–2008`). Test: `CompoundMassAndRollingTests.HollowChildLocalPoseAndDeclaredCentreGiveRealContactAndEmptySpace` (Torus case).
- **Present.** Child colliders add no mass: every moving body here declares a constant mass frame (`ConveyorElementDeclaration.cs`, hinge `MovingBody`; `LinearPusherElementDeclaration.cs:33`; `WoundSpringElementDeclaration.cs:36`), and the compiler takes mass and moments from it (`WorkshopPhysicsCompiler.cs:170–175`, `:211–216`). Test: `CompoundMassAndRollingTests.CollisionChildrenDoNotChangeDeclaredFreeFlight`.
- **Present.** A clipped rod collider on the head body: `ClippedExtrusionDescriptor` (`CuriousContraptions.Abstractions/SceneGeometry.cs:16`). Tests: `ClippedExtrusionTests`, `LinearPusherTests.PackageInstallationRoundTripsItsDriveExtrusionAndPhaseBinding`.
- **Present.** The sleeve's bore guides the head: `WoundSpringContactTests.RealHeadContactLaunchesLoadsAndGuideRimStopsOversizeEntry` (symbol: the tube proxy above).
- **Present.** Flat printing as a `QuadMesh` on a face: `Mesh_Mark` in `elements/BothGate/both_gate.tscn:35–37`. Test: `FaceMarkingTests.EveryPrintedSurfaceIsPresentFrontFacingAndAttachedToTheRotatingFace`.
- **Narrower, owned here.** The matcher, `VisibleSolidColliderTests.EverySolidArtPartHasItsColliderAndNoColliderReachesBeyondTheArt`. It matches hollow art only against the base body (`:207`), accepts a tube's bore only from a tube mouth (`:359`; neither package has mouths) and has no class for art drawn by a span binding. H1–H3 below are test-harness code; none touches a solver law, the worker ABI, the wire or a presentation contract, so no engine slice precedes this one.

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

- **H1, hollow art on a moving body** (theory, 203–210). A hollow entry under a moving root is matched in the moving pool, by the rule solids already use at 222–223. No existing row has hollow art under a moving root, so it can only add a failure.
- **H2, a drawn bore** (`Solid` 61, `Art`, `MatchesHollow` 359). `Solid` gains `float? Inner`. For an open `CylinderMesh` of equal radii whose own node carries `wall_thickness` metadata, `Inner` is (radius − wall) × scale. The Tube arm requires `Same(h.X, Inner)` when `Inner` is present, and in addition a mouth bore whenever the entry has mouths. The art node and the hidden proxy are two nodes, each stating the wall, so the row fails if they disagree.
- **H3, spanned art** (`ArtSpec` 54, theory). `ArtSpec` gains `string[]? Spanned`, counted in the classification at 193. `Art` also returns each moving root's scene transform, with its presentation parameter bindings applied. For each spanned path: the entry's `Presentation.Spans` has exactly one span with that `SpanNode`; the entry has exactly one clipped extrusion (`CuriousContraptions.Bundle/BundleSchema.cs:41`) and its `MaterialBody` b satisfies `spec.MovingRoots[b − 1] == span.MovingNode`, and the moving root at index b − 1 is the root of body b in the entry's scene ownership (sliders first, then hinges, `SceneGeometryExtractor.cs:43–46`); H3 asserts that, so a reordered row fails instead of selecting another body; the collider is that body's collider at the extrusion's `Collider` index; posed by the transform of `span.MovingNode`, it must satisfy `Matches` against the drawn mesh at `Tol + Fit`; it is then removed from the moving pool. It checks the authored pose only; extension is `ClippedExtrusionTests`' subject.

What these remove, and what catches a failure. No protection is deleted. The matcher still skips every `col_` mesh (249) and no scene has a visible one after this slice; `docs/engine-contracts.md:88`, `DESIGN.md:83` and `docs/hollow-geometry-design.md:32` stay true without edit. A proxy left visible is caught by eye and, from `ENGINE-ART-SOLIDITY`, by the build. H1–H3 add checks and exempt nothing: without H3 the rod collider fails as "reaches beyond the art"; H3 passes only when a declared span draws that art, a declared extrusion names that collider and the two coincide.

### Footprint, capture and occlusion

- No declared `ElementFootprint` (bodies and joints), pick radius or port moves.
- Placement extent follows the colliders (`engine/PlacementPose.cs:113`): the Conveyor's reach in +z grows from 0.64 to 0.83 m and the pusher's from 0.5 to 0.57 m, so each is refused 0.19 m and 0.07 m sooner at the bench's +z edge. All eleven authored Conveyors (z from −3 to 3) stay inside. The spring's socket reaches z 0.615, inside its base's 0.675.
- The Conveyor's input wheel is on the roller's dynamic body: a ball resting on it now loads the roller. Base and moving bodies do not collide (`BodiesCollide: false` in all three), and the delivered rigid drive connection, the Mechanical link, is drawn as a schematic and has no collider (owner decision 2, coordinator decision 25), so Mechanical links and the drive relay are untouched.
- Construction refuses only a dynamic body that starts inside a hollow wall (`engine/gpu/WorkshopPhysicsCompiler.cs:498–523`). The head ring is the one new hollow collider; cargo authored within 5 mm of the head's equator is now refused. The ring lies in the head's centre plane and does not lead the sphere; the pusher's cargo tests place cargo on the head's axis.
- No part here is an airflow source or receiver; the new colliders occlude a Fan's stream like any solid.

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
2. `CuriousContraptions.tests/VisibleSolidColliderTests.cs` -- make H1–H3; where `Art` skips a `col_` mesh (249), assert that the skipped mesh's own `Visible` is false; bring the class, `ArtSpec` and `Art` summaries up to date -- prerequisite of the rows; AC6 on every run.
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
- AC4: Given the candidate, when the full local round runs, then every project reports zero failed and `Skipped: 0`, and the total is the baseline total + 8 (two theory cases, three drop cases, three facts).
- AC5: Given the candidate diff, when its paths are listed, then none is under `engine/`, `CuriousContraptions.Simulation/`, `CuriousContraptions.Bundle/`, `schemas/`, `tools/`, `DESIGN.md`, `docs/engine-contracts.md` or any declaration file.
- AC6: Given every `elements/*/*.tscn`, when its `col_` mesh nodes are listed, then each has `visible = false`.

Counts in AC1 and AC4 are baseline + delta, counted on the slice's baseline at start, so the order in which parts B2, C and the Baseball repair publish does not change them.

## Spec Change Log

## Review Triage Log

## Design Notes

1. **Behaviour delivered.** Nine drawn solids and one ring become solid; fourteen raised markings become printing; two packages join the visible-solid proof. Source: owner rule "visible = solid" (25 October 2026); owner decisions of 21 November and coordinator decisions of 22 November 2026.
2. **Reusable.** Nothing new in the product. The matcher gains three package-neutral rules.
3. **Declared.** Proxy nodes in each scene. No declaration and no engine identity changes.
4. **Execution.** Scene → `SceneGeometryExtractor.Bake` → bundle entry → the unchanged compiler and solver.
5. **Ownership.** Colliders on existing bodies; no new state. Reset, save and load are unaffected: colliders compile from the bundle.
6. **Generality.** H1–H3 read only scene ownership, node metadata, declared spans and declared extrusions.
7. **Deleted.** Fourteen raised boxes with `Mesh_PulleySpoke` and the `Mark`, `Index` and `Spoke` box resources; the eight drawn proxies as a pattern. No raised copy, visible proxy or second matcher path remains.
8. **Uncertain.** Admission of the 0.01 m torus tube on the slider body and the hinge's axial support in B2 are unexecuted. Rules E1, E6, E7 and E8 apply; E9 is run as always.

## Verification

Executor for every command: the implementation owner. Raw output goes under `.tools/visible-solid-remainder-b-evidence/`.

Environment: the pinned toolchain first on `PATH`: `PATH=$HOME/.local/share/tim-toolchain/dotnet-10.0.403:$HOME/.local/share/tim-toolchain/node-v26.12.1-darwin-arm64/bin:$PATH` and `DOTNET_ROOT=$HOME/.local/share/tim-toolchain/dotnet-10.0.403` (on this host the default `PATH` has Homebrew node v26.13.1 and a dotnet without `wasm-tools`, so the capture's node check and the web build fail otherwise); record the session environment check in the run log.

### Acceptance-to-test table

The three parts' static restitution is 0.05 (Conveyor) or 0 (the others), so the product with the ball's 0.55 is at most 0.0275. The existing drop theory's rebound bracket has a slack of g·T·(1 + e)/v_in = 0.027 at its 3.13 m/s arrival and cannot tell that from zero; restitution is a body material (`SceneBodyGeometry.Material`), so a new collider cannot carry a different one. The new theory therefore asserts the stop only: the same bench, 0.5 m fall and 180 ticks; landing detected as at `AssertDropOnto:658`; the height of the first landed read lies in [rest − 17 mm, rest + 5 mm], the class's own two bounds. They fit the first landed read because after the stop the ball rises at most e·v_in·T = 0.7 mm. If B2's first landed read is below rest − 17 mm, that is a hinge-support finding: return to planning; do not widen. Each drop was checked by hand and by the auditor against the baked colliders; the implementer recomputes it and returns to planning if a clearance is under 5 mm. `Back` puts a part-local point (x, y, z) at (x, z, −y).

| Rows | Named discriminating test | Control and boundary | Run |
|---|---|---|---|
| B1 | Low-restitution theory, `Feature.ConveyorOutputPulley` = (`conveyor`, `Back`, top 0.78 + 0.05, X 1.35, Z 0.07) | Without the collider the ball stops on the idle roller's end, 0.18 m lower. Boundary: a flat face on a static body | planned |
| B2 | Same theory, `Feature.ConveyorInputPulley` = (`conveyor`, `Back`, 0.83, X −1.35, Z 0.07) | Without it the ball stops on the roller's end, 0.18 m lower. Boundary: a face on a hinged body, struck on its axis | planned |
| B3 | Same theory, `Feature.PusherSocket` = (`linear_pusher`, identity, 0.4 + 0.07, X −0.45, Z 0.45) | Without it the ball stops on the barrel's edge at 0.325 + √(0.34² − 0.125²), 0.169 m lower; the indicator is 0.62 m from the resting centre. Boundary: a sphere apex, first stop only | planned |
| B7 | `VisibleSolidColliderTests.EverySolidArtPartHasItsColliderAndNoColliderReachesBeyondTheArt`, case `wound_spring` (its `Visual/ActivationSocket` entry) | No drop case: once part D seats the socket on the rail's face (the spring on its back), the sleeve's end ring is 0.15 m behind the ball's centre plane and a Basketball's stop (0.505 m) is only 7.2 mm above the ring's (0.4978 m), so no bound could tell the socket from the ring; part D drops a Tennis ball on the seated socket. Boundary: a sphere on a static body, as B4 | planned |
| B4, B5, B6, B8, B11, AC1 | `VisibleSolidColliderTests.EverySolidArtPartHasItsColliderAndNoColliderReachesBeyondTheArt`, cases `conveyor`, `linear_pusher`, `wound_spring` | Each row fails with "solid art without a matching collider" if a proxy is absent, and with "reach beyond the art" if H3 or a proxy's art is absent. Boundary: torus and rod on the slider body; tube with a drawn bore | planned |
| B8, AC2, AC6 | The AC2 comparison (command below); for AC6 the assertion in `Art` that every skipped `col_` mesh is hidden, run by every theory case, and the `git grep` listing for the packages outside the table | The baseline's colliders are the control: equal and in order | planned |
| B9 | `ConveyorTests.BothPulleySpokesArePrintedFlatOnTheirWheels`, `LinearPusherArtTests.TheTravelMarksArePrintedFlatOnTheBarrelFace`, `WoundSpringArtTests.TheMarksIndexAndSpokeArePrintedFlatOnTheirFaces` (new facts): each named node is a `QuadMesh` facing +Z, its plane between 0 and 0.5 mm in front of its face, its extent inside the face, at the authored size; the index lies in front of the marks | 0.5 mm is the bound `FaceMarkingTests.cs:60` uses for print (delivered stand-off 0.25 mm, `both_gate.tscn:100`–`:101`); the thinnest old box (12 mm) fails | planned |
| B12 | Existing. Conveyor: `ConveyorTests` (16, including `PoweredTransport_AcceleratesBallAlongPositiveX`, `OffBeltMiss_SideContactRemainsOrdinaryRigid`, `ChainedConveyors_DriveRelaysRotationToDownstreamConveyor`, `TheRollerArtFollowsTheCommittedRollerPoseAndResetsToItsScenePose`), `ReverseTransmissionTests`, `ClutchTests`, `ClutchGeneralityTests`, `WindmillIntegrationTests.PaidShaftTransportsRemoteCargoAndReverseStagesControlDirection`. Pusher: `LinearPusherTests` (13, including `LosingPowerLocksTheIntermediatePositionAndRetractResumesFromIt`, `ObstructionStopsTheHeadAndReportsBlockedWhileAnAdjacentWallDoesNot`), `LinearPusherIntegrationTests`, `LinearPusherArtTests`, `ClippedExtrusionTests`. Spring: `WoundSpringControlTests`, `WoundSpringContactTests`, `WoundSpringIntegrationTests`, `WoundSpringLifecycleTests`, `WoundSpringArtTests`, `RetentionBoundaryTests`, `RetentionRequestTests`, `LaunchLessonSolveTests` (the two playable Wound spring lessons). All: `F32BehaviourIdentityTests`, `PackageRuntimeParityTests`, `BundleRoundTripTests`, `PalettePlacementTests`, `WorkshopIntegrationHarnessTests` | Existing | planned |

**Commands:**

- The compiler-proof capture in `docs/delivery-workflow.md#reproducible-local-verification`, up to and including the `verify-policy.binlog` build -- expected: five logs, zero warnings and errors; the bundle regenerates.
- `dotnet run --file tools/anvil/VerifyPolicy.cs -- --maintainability --project-root "$PWD"` -- expected: PASS with both canary `PASS` lines and the `Coupling dependencies:` summary.
- The registration check in `docs/delivery-workflow.md#new-package-registration-check` -- expected: 0 failed, 0 skipped.
- The AC2 comparison of `bundle-baseline.json` with the candidate's bundle, by decoded entry -- expected: exactly the three differences listed.
- `git grep -n -A6 -E 'node name="col_[^"]*" type="MeshInstance3D"' -- 'elements/*/*.tscn'` -- expected: every match is followed by `visible = false` before the next node (AC6).
- `dotnet test CuriousContraptions.slnx -c Debug -f net10.0 --no-build --no-restore --nologo -noAutoResponse --filter "FullyQualifiedName~VisibleSolidColliderTests|FullyQualifiedName~ConveyorTests|FullyQualifiedName~LinearPusher|FullyQualifiedName~WoundSpring|FullyQualifiedName~ClippedExtrusionTests|FullyQualifiedName~ReverseTransmission|FullyQualifiedName~Clutch|FullyQualifiedName~Windmill|FullyQualifiedName~Retention|FullyQualifiedName~PalettePlacementTests|FullyQualifiedName~WorkshopIntegrationHarnessTests|FullyQualifiedName~F32BehaviourIdentityTests|FullyQualifiedName~PackageRuntimeParityTests|FullyQualifiedName~BundleRoundTripTests|FullyQualifiedName~FaceMarkingTests|FullyQualifiedName~LaunchLessonSolveTests"` -- expected: 0 failed, 0 skipped; every test in the table appears.
- `git add -N` this contract and its run log if untracked, then `git diff --check <baseline_revision>` -- expected: no output.
- One full local round under `caffeinate -i` -- expected: baseline total + 8, 0 failed, `Skipped: 0` for every project.
- At the candidate commit, the unmodified `.githooks/pre-commit` -- expected: pass.

**Manual checks (if no CLI):**

- Open the three scenes: every proxy follows its body's earlier proxies in file order, and no `col_` mesh is visible.

### Running beside other work

It runs after part A and before parts B2 and C; part C reuses `Solid.Inner` (H2) and the low-restitution theory. It is serialised with parts A, B2 and C and the Baseball repair, which all edit `VisibleSolidColliderTests.cs`: one publisher at a time; the later merges main once and repeats the full local round on the merged candidate. It shares `deferred-work.md`, `TODO.md` and `docs/planning/invest/delivery-batches.md` with those slices and with S3, S5, S5b and S6–S6g, the other slices allowed before the engine programme (owner decisions 22 and 24h). It does not touch `content/puzzles.json`, the test project file or any registration table.
