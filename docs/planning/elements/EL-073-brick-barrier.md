# EL-073 · Brick barrier declaration readiness spec

**Current delivery (24 October 2026):** [EL-073 delivery slice](../invest/named-elements.md#element-073). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Story 7.0 named-identity spec. Baseline commit `a6c914e`. Legacy citations use `path@a6c914e:Lstart-Lend` and stay retrievable from git history after the Epic 7 purge. Values marked **proposed** are design values the owner may revise. Sections 1 to 3 and 5 to 8 were rewritten on 21 November 2026 against the current code (published main `8d1d68aa`); citations without `@a6c914e` are `path:line` in that checkout.

## 1. Identity

| Field | Value |
| --- | --- |
| ID | EL-073 |
| Name | Brick barrier |
| Type | Mechanical |
| Requirement anchor | [element-073](../requirements.md#element-073); scope index [todo-199](../requirements.md#todo-199) |
| Named entry | [named-elements.md#element-073](../invest/named-elements.md#element-073); proof owner S643. The entry also retains [CAT-066 acceptance](CAT-066-wall.md) |
| Disposition | Member of the Family slice **Barrier materials**, with [EL-074 Wood barrier](EL-074-wood-barrier.md). A variant of the delivered Wall, not a duplicate: its contact material, default size, art and (later) strength differ |
| Planned package | id `brick_barrier`, directory `elements/BrickBarrier/`, class `BrickBarrierElementDeclaration` |
| Coverage binding | Planned, not yet live (section 7); blocked on S7. No CAT number |
| delivery-batches class | [Section 1a](../invest/delivery-batches.md#1a-families-one-slice-each-if-the-probe-passes), family "Barrier materials". **Confirmed by code:** the contact behaviour is declaration data over the Wall shape, every capability present with a test. "Strength" has no engine capability today and is a named later-partner criterion; the identity stays open until it runs |
| CAT spec refined | [CAT-066 wall](CAT-066-wall.md), delivered as `elements/Wall/` |
| Related identities | EL-074 (sibling; must not share brick values); the fracture consumers of `ENGINE-FRACTURE` (EL-159 to EL-161) |
| Status | Not started; blocked on the engine programme (owner decision 3a), relying most on ENGINE-SPATIAL-QUERIES (contact candidates and the segment cast that airflow, light and sound use) and ENGINE-ISLANDS-SLEEP (cargo resting on or against it); then S7. Strength criterion R8: then ENGINE-FRACTURE alone. Occlusion row R10: then ENGINE-OPTICAL and Story 14.0 |

## 2. Declaration

The declaration is the Wall's, `elements/Wall/WallElementDeclaration.cs:8`, with different metadata, default dimensions, material values and art. The two family members and the Wall share every interface, collider, binding and parameter key; they differ only in declared values.

- **Interfaces.** `IMechanicalElement` (`CuriousContraptions.Abstractions.Mechanical/MechanicalContracts.cs:7`), `ISceneGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`), `IParameterizedGeometryElement` (`SceneGeometry.cs:29`).
- **Body and collider.** One static body, mass 0, footprint 1 body and 0 moving (`WallElementDeclaration.cs:33`, `:58`–`:68`). One Box collider whose half-extents are bound to the parameters: half-extent X = 0.5·`width`, Y = 0.5·`height`, Z = 0.5·`thickness` (`WallElementDeclaration.cs:40`–`:45`).
- **Material** (restitution, friction, rolling resistance, bounce threshold, `CuriousContraptions.Abstractions/ColliderDescriptor.cs:21`): `new(0.3f, 0.8f, 0f, 0.1f)`. The Wall is `new(1f, 0.3f, 0f, 0.1f)` (`WallElementDeclaration.cs:51`); wood is `new(0.6f, 0.4f, 0f, 0.1f)`.
- **Ports, stores, sensors, events.** None.

| Parameter | Kind | Unit | Default | Min | Max | Editor policy | Run edit |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `width` | Real | Metre | 2.0 | 0.4 | 8 | Reject | BuildOnly |
| `height` | Real | Metre | 1.5 | 0.4 | 6 | Reject | BuildOnly |
| `thickness` | Real | Metre | 0.3 | 0.12 | 2 | Reject | BuildOnly |

Bounds are the Wall's (`WallElementDeclaration.cs:28`–`:30`). Policy and run edit are the descriptor defaults the Wall uses.

- **Presentation.** As `WallElementDeclaration.cs:10`–`:15`: pick radius 1.5 m, all three resize axes; `Visual` ScaleX ← `width`, ScaleY ← `height`, ScaleZ ← `thickness`; resize handles bound to the three parameters, with default size `(2, 1.5, 0.3)`.
- **Metadata.** Id `brick_barrier`, display name "Brick wall", category `ElementCategory.Structure`, palette order 502 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)), colour coral `(0.85, 0.42, 0.35)` `#d96b58`, description "A rough, dead masonry panel. Balls stop against it and barely bounce; things grip its surface."

### Art and solidity

| Node | Mesh and dimensions (at unit scale; `Visual` scales them) | Class |
| --- | --- | --- |
| `Visual/Visual_Body` | Box 1 × 1 × 0.998 | Solid, collider `col_0_0` (Box) |
| `Visual/Visual_Mortar_H0` … `H2` | Boxes 1 × 0.02 × 1 at y −0.25, 0 and +0.25: three bed joints, four courses | Decorative: printed flat. They pass through the thickness and stand 0.001 × `thickness` proud of each face, at most 0.002 m, the fit the Wall's stripes already declare (`CuriousContraptions.tests/VisibleSolidColliderTests.cs:136`) |
| `Visual/Visual_Mortar_V0` … `V5` | Boxes 0.02 × 0.25 × 1: head joints at x ±0.25 in the first and third courses and at x 0 in the second and fourth | Decorative: printed flat, same bound |

Colours: body coral `#d96b58` (approved palette, `DESIGN.md:80`); mortar cream `#f9e8c9`, the Wall's stripe colour (`elements/Wall/wall.tscn:12`). The earlier draft's terracotta `#a96f5c` is a projection-outline colour (`DESIGN.md:213`), not a base material colour, so coral replaces it. Icon: a coral panel with the running-bond joint pattern. **At a glance:** the staggered head joints (running bond) are the brick pattern; the Wall has two vertical cream stripes and no horizontal line, and the wood barrier has horizontal seams with nail pairs and no staggered joint. Resizing stretches the courses, as it stretches the Wall's stripes.

### Behaviour and numbers

- **Law.** Static contact. Pair laws: product restitution, geometric-mean friction, maximum bounce threshold (`docs/gpu-f32-physics.md:147`).
- **Restitution 0.3** (starting value, D1). A Basketball (0.55, `elements/Ball/BallElementDeclaration.cs:38`) rebounds at 0.165 of its approach speed: dropped from 1 m it returns to about 0.03 m. The Wall returns it at 0.55 (0.30 m) and wood at 0.33 (0.11 m). Three surfaces a player can tell apart by eye.
- **Friction 0.8** (starting value; the earlier draft proposed 0.7). Pair friction with a 0.3 partner (every delivered ball and the Weight) is 0.49; wood gives 0.35 and the Wall 0.30. The values are spread so that one tilt angle separates them with margin: on a panel tilted 22° (tan 22° = 0.404) a Weight standing on its base holds on brick (0.49, 21% above) and slides on wood (0.35, 14% below).
- **Default size 2.0 × 1.5 × 0.3 m.** A squat, thick block that reads as masonry beside the Wall's 3 × 2 × 0.25 m.
- **No passage.** The game-grade envelope keeps a body from passing a barrier at least 1 cm thick at up to 64 m/s (`docs/gpu-f32-physics.md:375`); the thinnest brick is 0.12 m.
- **Strength (later partner).** Starting value 400 J of impact work (D1), above a 4 kg Bowling ball at 10 m/s (200 J), so brick is intact against every delivered projectile. The unit and the law (the story speaks of a sustained load limit) are decided once in the `ENGINE-FRACTURE` Code Map for all consumers; this spec does not fix them. The break needs ENGINE-FRACTURE alone: a body break does not need player-made joints first (`readiness/batch-l.md` section 4 item 8). Fracture re-solves the breaking substep with the declared fragments in place of the removed collider, so the breaking contact acts on the fragments and momentum is conserved (coordinator decision 16); the fragments are declared companion content of this package (owner decision 15), fixed in count at compile.
- **Mass and density.** A static body has no mass, so no density is declared now. The game-scale density question the earlier draft raised belongs to ENGINE-FRACTURE, which first gives a barrier dynamic fragments.
- **Tick order.** No signal, optical or acoustic read.

## 3. Engine capabilities (capability probe)

| Capability | Status | Symbol | Exercising test |
| --- | --- | --- | --- |
| Static box from scene geometry with parameter-bound extents | *present* | `ColliderParameterBinding` `SceneGeometry.cs:27`; `WallElementDeclaration.cs:40` | `CuriousContraptions.tests/WorkshopWallTests.cs:15` (default and both limits), `:161` |
| A rotated static box blocks a moving ball; absent, it does not | *present* | — | `WorkshopWallTests.cs:88` |
| Per-package static contact material: product restitution | *present* | `SceneBodyGeometry` `SceneGeometry.cs:8` | `CuriousContraptions.tests/VisibleSolidColliderTests.cs:860` (Wall, 1.0) and `:685` (statics at 0.055 to 0.15) |
| Per-collider friction under the geometric-mean law | *present* | `MaterialProperties` `ColliderDescriptor.cs:21` | `CuriousContraptions.tests/CompoundMassAndRollingTests.cs:83`, `:19` (rough and smooth supports) |
| Dimension limits, atomic refusal, exact f32 round trip | *present* | `ParameterDescriptor` `CuriousContraptions.Abstractions/ParameterDescriptor.cs:52` | `WorkshopWallTests.cs:55`, `:39` |
| Reset and save/load of a static box and its contact | *present* | — | `WorkshopWallTests.cs:121` |
| A part with no port refuses a foreign connection | *present* | — | `WorkshopWallTests.cs:141` |
| Printed-flat art within a declared fit | *present* | art classification table | `VisibleSolidColliderTests.cs:237`, entry `:136` |
| Declared load limit on a body with a one-way break | *missing* | none | `ENGINE-FRACTURE` (`_bmad-output/planning-artifacts/epics.md@25df3cd5:4467`: "a declared load limit on a connection or body, a one-way break … and declared fragments"), with the additions of `readiness/batch-l.md` section 4 item 8 (body break without player-made joints; breaking substep re-solved with declared fragments). The story text covers it |

The contact slice uses only the eight *present* rows. The earlier draft's "Missing: static parts with a non-default material" is delivered: every package declares its own static material.

**Element dependencies.** None for the intact barrier. Collaborators: the delivered balls and the Weight.

### Engine conformance

- **Declaration data only.** One static box, one material, three size parameters. It is the Wall's declaration with other values; no callback or engine branch. Strength, when it arrives, is a declared load limit read by a general fracture law, never brick logic.
- **Generic laws.** Blocking, rebound and grip are contact with product restitution and geometric-mean friction and this package's declared material.
- **Layers.** Colliders: 1 static box. Visuals: body box and printed mortar joints, none used as a collider. Animation: none; nothing moves any art node during a Run; art and collider follow the size parameters through Build-time bindings. Visible = solid: the body has its box collider; the mortar joints are printed flat within 0.002 m.
- **One-way flow and typing.** Nothing reads art. The three dimensions are typed f32 parameters in metres.
- **Flat state.** 1 static body, 1 box collider, 0 joints, 0 drives, 0 sensors, 0 domain nodes, 0 stores, 3 collider parameter bindings and 3 scale bindings.
- **Performance.** One primitive, static. The 64 m/s boundary is met by the engine's general substeps and speculative margins (`docs/gpu-f32-physics.md:375`), not a per-part check. No island sleeping exists today; a ball or Weight resting on it costs nothing only once ENGINE-ISLANDS-SLEEP (owner decision 3a) is delivered, ahead of this part.
- **Tests are scenarios.** Rows R1 to R10 run the production simulation with delivered parts.

## 4. Sources and legacy

- Requirements row: "No passage through intact geometry." No variants.

| # | Fact | Citation | Disposition | Reason |
| --- | --- | --- | --- | --- |
| 1 | Legacy static surfaces use restitution 1, bounce threshold 0.1, friction 0.3 (the source `MachinePart.InitialContactMaterial`). | `engine/gpu/WorkshopPhysicsCompiler.cs@a6c914e:L109-L110` | do not carry forward for brick | The row requires the barrier's own declared contact properties. |
| 2 | Wall: three resizable local dimensions; the common box declaration owns all contact physics. | `parts/WallPart.cs@a6c914e:L6-L12` | carry forward | Same resize model. |
| 3 | Wall catalogue dimensions stored as half-precision bits (`WidthBits = 16896` = 3 m, `HeightBits = 16384` = 2 m, `ThicknessBits = 13312` = 0.25 m). | `parts/catalog/wall.tres@a6c914e:L8-L11` | do not carry forward the bits | f32 contract: carry the decimal meaning only. |

**Files harvested:** `parts/WallPart.cs`, `parts/catalog/wall.tres` (both harvested in full by CAT-066), `engine/WallDimensionsResource.cs` (checked). Searched for brick, masonry: no hit.

## 5. Acceptance outline

Requirement row: [element-073](../requirements.md#element-073). The row has two parts: "declared contact and strength properties" and "no passage through intact geometry". Rows R1 to R7 are delivered by the family slice; R8 is the later-partner criterion and the identity is not closed until it runs.

- **Chrome UI recipe (pending, post-catalogue).** Place a Brick wall upright across a Ramp's run-out with a Receiver behind it and release a Bowling ball. Place a Basketball 1 m above a horizontal Brick wall, a Wooden wall and a Wall. Resize the brick by its handles. Verify placement and dimensions through real UI controls.

All tests are planned. Family rows live in `CuriousContraptions.tests/BarrierMaterialsTests.cs`, a theory over `brick_barrier` and `wood_barrier`; brick-only rows in `BrickBarrierTests.cs`. "S4 row n" is the planned conformance theory; until it is delivered the named package test is written.

| # | Given / when / then | Planned test | Control | Boundary | Tolerance and stall guard |
| --- | --- | --- | --- | --- | --- |
| R1 | **No passage.** Given an upright brick barrier and a Bowling ball launched at its face at 8 m/s, when Run advances 3 s, then the ball's centre never passes the near face | `BarrierMaterialsTests.AMovingBallNeverPassesTheIntactBarrier` (pattern `WorkshopWallTests.cs:88`) | The same launch with the barrier absent crosses the plane | 64 m/s with the barrier yawed 45° (the retained CAT-066 maximum-speed boundary, `CAT-066-wall.md:88`; the Wall theory's case, `CuriousContraptions.tests/WorkshopWallTests.cs:86`); `thickness` 0.12 m at 64 m/s (the minimum, inside the envelope of `docs/gpu-f32-physics.md:375`) | At 8 m/s the centre stays at least ball radius − 0.01 m outside the face plane: 1 cm is the envelope's barrier bound, and a ball beyond it has entered the wall. At 64 m/s the retained criterion is the Wall theory's: the centre never reaches the near face plane (`WorkshopWallTests.cs:116`) |
| R2 | **Declared restitution.** Given a Basketball dropped 0.5 m onto a horizontal brick barrier, then it rebounds at 0.165 of its approach speed | `BarrierMaterialsTests.ABallReboundsAtEachMaterialsOwnRestitution` | The Wall's 0.55 (delivered, `VisibleSolidColliderTests.cs:860`); wood 0.33 | — | Ratio within ±0.04; the nearest neighbour is 0.165 away, so a barrier using another material's value fails |
| R3 | **Declared friction.** Given a barrier widened to 8 m, laid flat and tilted 22°, and a default Weight set on it base down, when Run advances 3 s, then on brick the Weight moves less than 0.02 m | `BarrierMaterialsTests.AWeightHoldsOnTiltedBrickAndSlidesOnTiltedWood` | On wood it travels more than 0.3 m (expected about 2.4 m at 0.52 m/s²) | — | 0.02 m is under a tenth of the Weight's radius, so settling is not a slide; 0.3 m is an eighth of the expected slide, so a late start still passes |
| R4 | **Family shape.** The brick, wood and Wall entries have the same interfaces, collider kind, bindings and parameter keys and bounds, and three different compiled materials | `BarrierMaterialsTests.TheFamilySharesOneShapeAndEachMemberCompilesItsOwnMaterial` | — | — | Exact: decoded material fields and parameter descriptors |
| R5 | Each dimension is admitted at its default and both limits and round-trips; a value outside them or non-finite is refused atomically | S4 rows 1 and 2, else `BarrierMaterialsTests.DimensionsRoundTripAtTheLimitsAndInvalidValuesAreRefused` (pattern `WorkshopWallTests.cs:15`, `:55`) | — | The next f32 beyond each limit | Exact typed refusal; exact f32 round trip of authored values |
| R6 | Run, Reset and save/load restore the barrier, its dimensions and the ball's authored state | S4 row 3, else `BarrierMaterialsTests.ResetAndSaveLoadRestoreTheBarrierAndItsContact` (pattern `WorkshopWallTests.cs:121`) | — | — | Authored state exact |
| R7 | The package declares no port and a construction that connects one is refused; every art node is classified and the body has its collider at each dimension's limits | `BarrierMaterialsTests.TheBarrierHasNoPortsAndRefusesAForeignConnection` (pattern `WorkshopWallTests.cs:141`); one entry beside `VisibleSolidColliderTests.cs:136` | — | Dimension limits, taken by that theory at `:227` | Exact; art fit 0.002 m |
| R8 | **Strength (later partner, after `ENGINE-FRACTURE`).** Given a brick barrier with its declared limit, when it is loaded just below the limit it stays intact and still blocks; just above, it breaks (the breaking substep re-solved with its declared fragments), stays broken for the Run, and Reset restores it | `BrickBarrierTests.ItBreaksOnlyAboveItsDeclaredLimitAndResetRestoresIt` | Below the limit | The limit itself, from each side | Set by the engine story's fixtures; the value 400 J is the starting value |
| R9 | **Air occlusion (retained CAT-066 control; IX-12, IX-13).** Given a Fan blowing at a Tennis ball with an upright barrier between them, then the ball is not pushed | `BarrierMaterialsTests.AnUprightBarrierBlocksAFansStream` (pattern `CuriousContraptions.tests/FanGuidanceTests.cs:45`) | Barrier removed: the ball is pushed | — | Ball displacement under 0.02 m in 3 s against more than 0.3 m in the control |
| R10 | **Light and sound occlusion (later partner: light after `ENGINE-OPTICAL`, sound after Story 14.0; IX-12, IX-13).** An upright barrier blocks a beam and a tone that reach their receiver without it. Light follows [EL-176 section 10](EL-176-red-laser-emitter.md#10-optical-conventions-owned-here-every-optical-spec-cites-this-section): the barrier's box takes the default Absorb response (convention 6), so a receiver behind it reads zero; the tone uses the same segment cast (convention 5) | `BarrierMaterialsTests.AnUprightBarrierBlocksABeamAndATone` | Barrier removed: the receiver reads the beam's power | — | Light: the optical tolerances (coordinator decision 21), ±2 % of the emitted power in the control and zero here, after a 1 s guard. Sound: set by Story 14.0's fixtures. The identity stays open for this row until it runs |

The retained CAT-066 acceptance is rows R1, R5, R6 and R7 on this package, with its occlusion controls in R9 and R10.

## 6. Open questions

**Resolved**

1. Resizable or fixed blocks: **resizable, decided now.** The ledger entry retains CAT-066 acceptance, whose three dimensions are the size parameters, so D13's exception applies; the family definition also needs the same shape as the Wall.
2. Strength value and failure mode: **D1 and the `ENGINE-FRACTURE` Code Map.** 400 J is the starting value; unit and law are the story's shared-model decision.
3. Optical opacity: a retained CAT-066 control, kept as later-partner row R10. Every participating collider occludes with the default Absorb response ([EL-176 section 10](EL-176-red-laser-emitter.md#10-optical-conventions-owned-here-every-optical-spec-cites-this-section), convention 6; [ENGINE-OPTICAL](../../../_bmad-output/planning-artifacts/epics.md#story-engine-optical-beam-emission-transport-surface-interaction-and-reception)); a brick barrier is a solid and declares no other response.
4. Material density scale: **not declared now** (static body, no mass). The engine story that first needs a barrier's mass decides the scale once; for thermal consumers that is D5.
5. Contact values against the Wall and wood: **decided now**, restitution 0.3 and friction 0.8, chosen to be distinct from both in a way one test angle separates.
6. Contact is delivered first, by the family slice after the engine programme; strength follows `ENGINE-FRACTURE` as later-partner row R8, and the identity stays open until R8 (and R10) runs. **Coordinator decision under the standing delegation (22 November 2026)**, as `docs/planning/invest/delivery-batches.md:28` allows. No requirement criterion is dropped or weakened.

**Needs the owner**

None.

## 7. Integration

- **Collaborators.** Delivered: Basketball, Bowling ball, Tennis ball, Weight, Ramp, Receiver, Wall. Same slice: EL-074.
- **Campaign.** "Ramp/wall/material variants", first use levels 1–10, reuse 21–30, 41–50, 91–100 and 136–150 (`docs/planning/invest/campaign-reservations.md:20`). This slice authors no level; a level that places the barrier before it is playable needs the lesson-fixture story (S8).

### Coverage binding (planned, not yet live)

Published main binds a named-identity package through a final section of the identity's spec headed "Package source binding", read by `tools/Coverage/NamedPackageSources.cs` (on `origin/main`, `25df3cd5`; pattern: EL-239 Baseball). The section is not written here: the reader parses every EL spec that carries that heading and refuses one whose preconditions fail. **Blocked on the coverage follow-on story S7.**

| Field | Planned value |
| --- | --- |
| Package | `brick_barrier` (its fracture fragments, when R8 lands, are a declared companion package, owner decision 15) |
| Named identity | EL-073, `../invest/named-elements.md#element-073` |
| Acceptance | `todo-199` and `sequence-task-626` |
| Decision | S643; `scope-corrections.md` has no `s643` block |
| Mode records | `Configuration=Adjustable` (it declares parameters; confirmed at slice planning) |

What it lacks today: an `s643` anchor block in `docs/planning/invest/scope-corrections.md`, and its todo-199 anchor is a scope index, not immediately followed by its sequence-task-626 anchor in `requirements.md`. S7 widens the reader (owner decision 15), including for companion packages and TH, RAD and GAP identities.

## 8. Delivery

- **Slice shape.** Family slice "Barrier materials": EL-073 and EL-074, one candidate commit, each identity with its own ledger row, acceptance rows and tests. The strength criterion (R8) is a later small slice on this package after `ENGINE-FRACTURE`.
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), which includes ENGINE-SPATIAL-QUERIES and ENGINE-ISLANDS-SLEEP; then S7 (the coverage follow-on, which widens the reader, owner decision 15). For R8 only: then ENGINE-FRACTURE alone (a body break does not need player-made joints, `readiness/batch-l.md` section 4 item 8). For R10 only: then ENGINE-OPTICAL and Story 14.0.
- **Files added.** `elements/BrickBarrier/BrickBarrier.csproj`, `BrickBarrierElementDeclaration.cs`, `brick_barrier.tscn`, `icon.svg`; `CuriousContraptions.tests/BarrierMaterialsTests.cs` (shared with EL-074). `BrickBarrierTests.cs` arrives with R8.
- **Files edited.** One entry beside `VisibleSolidColliderTests.cs:136`; until S3, `CuriousContraptions.slnx:46`, the trim list at `CSharpStaticGateTests.cs:687`–`:703` and the parameter and placement switches at `PackageRuntimeParityTests.cs:483` and `:641` (generic list `:653`); the binding section of section 7; this spec and `TODO.md`.
- **Review tier.** Data-only for the family slice once S3 has published (every capability it uses is present with a test); before S3, engine tier for the placement switch alone (`spec-family-ball-presets.md:124`). The R8 slice is tiered by its own probe.
- **Disqualifiers (the slice's probe re-checks).** The Wall's declaration shape and bounds are unchanged; the product-restitution and geometric-mean laws still hold for statics; the requirement row is still read as allowing strength to follow its engine story (if the owner requires strength in the first slice, the family slice waits for `ENGINE-FRACTURE`).
