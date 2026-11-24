# EL-074 · Wood barrier declaration readiness spec

**Current delivery (24 October 2026):** [EL-074 delivery slice](../invest/named-elements.md#element-074). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Story 7.0 named-identity spec. Baseline commit `a6c914e`. Legacy citations use `path@a6c914e:Lstart-Lend` and stay retrievable from git history after the Epic 7 purge. Values marked **proposed** are design values the owner may revise. Sections 1 to 3 and 5 to 8 were rewritten on 21 November 2026 against the current code (published main `8d1d68aa`); citations without `@a6c914e` are `path:line` in that checkout.

## 1. Identity

| Field | Value |
| --- | --- |
| ID | EL-074 |
| Name | Wood barrier |
| Type | Mechanical |
| Requirement anchor | [element-074](../requirements.md#element-074); scope index [todo-199](../requirements.md#todo-199) |
| Named entry | [named-elements.md#element-074](../invest/named-elements.md#element-074); proof owner S644. The entry also retains [CAT-066 acceptance](CAT-066-wall.md) |
| Disposition | Member of the Family slice **Barrier materials**, with [EL-073 Brick barrier](EL-073-brick-barrier.md). A variant of the delivered Wall, not a duplicate: its contact material, default size, art and (later) combustibility differ |
| Planned package | id `wood_barrier`, directory `elements/WoodBarrier/`, class `WoodBarrierElementDeclaration` |
| Coverage binding | Planned, not yet live (section 7); blocked on S7. No CAT number |
| delivery-batches class | [Section 1a](../invest/delivery-batches.md#1a-families-one-slice-each-if-the-probe-passes), family "Barrier materials". **Confirmed by code:** the contact behaviour is declaration data over the Wall shape, every capability present with a test. "Combustible" has no engine capability today and is a named later-partner criterion; the identity stays open until it runs |
| CAT spec refined | [CAT-066 wall](CAT-066-wall.md), delivered as `elements/Wall/`. The legacy "wooden wall" of level `solar_shadow` was the generic Wall kind (section 4) |
| Related identities | EL-073 (sibling; independent material); the burning parts of `ENGINE-IGNITION-BLAST` and the TH identities |
| Status | Not started; blocked on the engine programme (owner decision 3a), relying most on ENGINE-SPATIAL-QUERIES (contact candidates and the segment cast that airflow, light and sound use) and ENGINE-ISLANDS-SLEEP (cargo resting on or against it); then S7. Combustible criterion W8: then ENGINE-GAS-NETWORK, ENGINE-LIQUID and ENGINE-THERMAL (owner decision 18), then ENGINE-IGNITION-BLAST. Occlusion row W10: then ENGINE-OPTICAL and Story 14.0 |

## 2. Declaration

The declaration is the Wall's, `elements/Wall/WallElementDeclaration.cs:8`, with different metadata, default dimensions, material values and art. It shares every interface, collider, binding and parameter key with the Wall and with EL-073; the three differ only in declared values.

- **Interfaces.** `IMechanicalElement` (`CuriousContraptions.Abstractions.Mechanical/MechanicalContracts.cs:7`), `ISceneGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`), `IParameterizedGeometryElement` (`SceneGeometry.cs:29`).
- **Body and collider.** One static body, mass 0, footprint 1 body and 0 moving. One Box collider with half-extents 0.5·`width`, 0.5·`height`, 0.5·`thickness` (`WallElementDeclaration.cs:40`–`:45`).
- **Material** (restitution, friction, rolling resistance, bounce threshold, `CuriousContraptions.Abstractions/ColliderDescriptor.cs:21`): `new(0.6f, 0.4f, 0f, 0.1f)`. Brick is `new(0.3f, 0.8f, 0f, 0.1f)`; the Wall is `new(1f, 0.3f, 0f, 0.1f)` (`WallElementDeclaration.cs:51`). No two of the three share a restitution or a friction value.
- **Ports, sensors, events.** None. Ignition is a material response to thermal state, not a port.
- **Stores.** None today. The finite fuel store arrives with the combustible criterion.

| Parameter | Kind | Unit | Default | Min | Max | Editor policy | Run edit |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `width` | Real | Metre | 2.0 | 0.4 | 8 | Reject | BuildOnly |
| `height` | Real | Metre | 1.5 | 0.4 | 6 | Reject | BuildOnly |
| `thickness` | Real | Metre | 0.2 | 0.12 | 2 | Reject | BuildOnly |

Bounds are the Wall's (`WallElementDeclaration.cs:28`–`:30`).

- **Presentation.** As `WallElementDeclaration.cs:10`–`:15`: pick radius 1.5 m, all three resize axes; `Visual` ScaleX ← `width`, ScaleY ← `height`, ScaleZ ← `thickness`; default size `(2, 1.5, 0.2)`. Scorch and burn presentation is part of the combustible criterion.
- **Metadata.** Id `wood_barrier`, display name "Wooden wall", category `ElementCategory.Structure`, palette order 503 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)), colour ochre `(0.85, 0.62, 0.20)` `#d99e32`, description "A plank panel. Livelier than brick and slicker: balls bounce a little and loads slide on it."

### Art and solidity

| Node | Mesh and dimensions (at unit scale; `Visual` scales them) | Class |
| --- | --- | --- |
| `Visual/Visual_Body` | Box 1 × 1 × 0.998 | Solid, collider `col_0_0` (Box) |
| `Visual/Visual_Seam_0` … `2` | Boxes 1 × 0.012 × 1 at y −0.25, 0 and +0.25: four horizontal planks | Decorative: printed flat. They pass through the thickness and stand at most 0.002 m proud of each face, the fit the Wall's stripes declare (`CuriousContraptions.tests/VisibleSolidColliderTests.cs:118`) |
| `Visual/Visual_Nail_0` … `7` | Boxes 0.03 × 0.03 × 1, one at x −0.42 and one at x +0.42 on each plank's centre line | Decorative: printed flat, same bound |

Colours: body ochre `#d99e32` (approved palette, `DESIGN.md:80`); seams and nails the workbench stud brown `#9c743f` (`DESIGN.md:200`). The earlier draft used the Wall's own `#c28f52`, which would leave only the seams to tell the two apart. Icon: an ochre panel with three seams and the nail pairs. **At a glance:** horizontal plank seams with a nail pair at each plank end; brick has staggered head joints and no nails; the Wall has two vertical cream stripes.

### Behaviour and numbers

- **Law.** Static contact; product restitution, geometric-mean friction (`docs/gpu-f32-physics.md:147`).
- **Restitution 0.6** (starting value, D1). A Basketball (0.55) rebounds at 0.33 of its approach speed, about 0.11 m from a 1 m drop, between brick (0.165, 0.03 m) and the Wall (0.55, 0.30 m).
- **Friction 0.4** (starting value; the earlier draft proposed 0.5). Pair friction with a 0.3 partner is 0.35, against brick's 0.49 and the Wall's 0.30. On a panel tilted 22° (tan 22° = 0.404) a Weight slides on wood and holds on brick: this is the direct proof that mechanical contact does not use brick parameters.
- **Default size 2.0 × 1.5 × 0.2 m.** Plank thickness, thinner than brick's 0.3 m.
- **Combustible (later partner).** The thermal values follow the thermal conventions owned by [TH-04 section 11](TH-04-electrical-heating-plate.md#11-thermal-conventions-owned-here-every-thermal-spec-cites-this-section), which this spec cites and does not restate: units and ambient (T1), the barrier's thermal node and its face conductance (T2, T3, T11), wood's ignition temperature and game-scale fuel energy (T11), the chemical source that pays heat from the fuel store (T4) and the hand-off to ENGINE-IGNITION-BLAST at the ignition threshold (T5). The fuel mass and the burn rate are declared when `ENGINE-IGNITION-BLAST` defines the fuel store ("a declared ignition threshold, a finite fuel store consumed at a declared rate, extinction", `_bmad-output/planning-artifacts/epics.md@25df3cd5:4525`). Oxygen is read from a finite gas inventory (D6). The earlier "heat of combustion 15 MJ per kilogram" was an SI value and is withdrawn in favour of T11's game scale.
- **Strength.** The EL-074 row names no strength property, so none is declared (D19). The earlier draft's 150 J is withdrawn.
- **Mass and density.** A static body has no mass. The fuel mass is a declared store value, not a density times a volume, until an engine story says otherwise.
- **Tick order.** No signal, optical or acoustic read.

## 3. Engine capabilities (capability probe)

| Capability | Status | Symbol | Exercising test |
| --- | --- | --- | --- |
| Static box from scene geometry with parameter-bound extents | *present* | `ColliderParameterBinding` `SceneGeometry.cs:27`; `WallElementDeclaration.cs:40` | `CuriousContraptions.tests/WorkshopWallTests.cs:15`, `:161` |
| A rotated static box blocks a moving ball; absent, it does not | *present* | — | `WorkshopWallTests.cs:88` |
| Per-package static contact material: product restitution | *present* | `SceneBodyGeometry` `SceneGeometry.cs:8` | `CuriousContraptions.tests/VisibleSolidColliderTests.cs:684`, `:590` |
| Per-collider friction under the geometric-mean law | *present* | `MaterialProperties` `ColliderDescriptor.cs:21` | `CuriousContraptions.tests/CompoundMassAndRollingTests.cs:83`, `:19` |
| Dimension limits, atomic refusal, exact f32 round trip | *present* | `ParameterDescriptor` `CuriousContraptions.Abstractions/ParameterDescriptor.cs:52` | `WorkshopWallTests.cs:55`, `:39` |
| Reset and save/load of a static box and its contact | *present* | — | `WorkshopWallTests.cs:121` |
| A part with no port refuses a foreign connection | *present* | — | `WorkshopWallTests.cs:141` |
| Printed-flat art within a declared fit | *present* | art classification table | `VisibleSolidColliderTests.cs:206`, entry `:118` |
| Ignition threshold, finite fuel store, burn and extinction | *missing* | none | [`ENGINE-IGNITION-BLAST`](../../../_bmad-output/planning-artifacts/epics.md#story-engine-ignition-blast-ignition-burn-and-blast-impulse), which runs after [`ENGINE-THERMAL`](../../../_bmad-output/planning-artifacts/epics.md#story-engine-thermal-heat-storage-transfer-and-phase-change); the thermal engine comes early, after the engine programme, ENGINE-GAS-NETWORK and ENGINE-LIQUID (owner decision 18). The story text covers threshold, fuel and extinction. It does not say that a burnt-out solid stops being a barrier; that sentence is in the batch summary |

The contact slice uses only the eight *present* rows. One owner for the missing capability: `ENGINE-IGNITION-BLAST`. `ENGINE-THERMAL` is its prerequisite (it supplies the temperature that reaches the threshold), not a second owner.

**Element dependencies.** None for the intact barrier.

### Engine conformance

- **Declaration data only.** One static box, one material, three size parameters. It is the Wall's declaration with other values; no callback or engine branch. Burning, when it arrives, is a declared ignition threshold and fuel store read by a general ignition law, never wood logic.
- **Generic laws.** Blocking, rebound and sliding are contact with product restitution and geometric-mean friction and this package's own declared material, which is how "cannot silently use brick parameters" is met.
- **Layers.** Colliders: 1 static box. Visuals: body box, printed seams and nails, none used as a collider. Animation: none; nothing moves any art node during a Run; art and collider follow the size parameters through Build-time bindings. Visible = solid: the body has its box collider; seams and nails are printed flat within 0.002 m.
- **One-way flow and typing.** Nothing reads art. The three dimensions are typed f32 parameters in metres.
- **Flat state.** 1 static body, 1 box collider, 0 joints, 0 drives, 0 sensors, 0 domain nodes, 0 stores, 3 collider parameter bindings and 3 scale bindings.
- **Performance.** One primitive, static. The 64 m/s boundary is met by the engine's general substeps and speculative margins (`docs/gpu-f32-physics.md:375`). No island sleeping exists today; a ball or Weight resting on it costs nothing only once ENGINE-ISLANDS-SLEEP (owner decision 3a) is delivered, ahead of this part.
- **Tests are scenarios.** Rows W1 to W10 run the production simulation with delivered parts.

## 4. Sources and legacy

- Requirements row: "Mechanical contact cannot silently use brick parameters." No variants.

| # | Fact | Citation | Disposition | Reason |
| --- | --- | --- | --- | --- |
| 1 | Level `solar_shadow` ("Out of the shade") describes "The wooden wall blocks the flashlight", but places the generic `wall` kind (`shade`). | `content/puzzles.json@a6c914e:L6227-L6230`, `content/puzzles.json@a6c914e:L6426-L6427`; `tools/Campaign/Program.cs@a6c914e:L418-L420` | carry forward as context; do not carry forward "wooden" as a material | Legacy wood was a name in text only; no wood material existed. The level stays a CAT-066 Wall lesson unless the owner reassigns it. |
| 2 | Legacy static surfaces use (1, 0.1, 0.3). | `engine/gpu/WorkshopPhysicsCompiler.cs@a6c914e:L109-L110` | do not carry forward for wood | The row requires independent declared properties. |
| 3 | Wall body colour (0.76, 0.56, 0.32) and cream stripes `#f9e8c9`. | `parts/catalog/wall.tres@a6c914e:L20-L20`; `parts/WallPart.cs@a6c914e:L15-L17` | carry forward (palette) | Warm-wood tone already in the palette. |

**Files harvested:** `content/puzzles.json` (level `solar_shadow`), `tools/Campaign/Program.cs`, `parts/WallPart.cs`, `parts/catalog/wall.tres`. Searched for wood, timber, plank: only the level text above.

## 5. Acceptance outline

Requirement row: [element-074](../requirements.md#element-074). The row has two parts: "independently declared contact and combustible properties" and "mechanical contact cannot silently use brick parameters". Rows W1 to W7 are delivered by the family slice; W8 is the later-partner criterion and the identity is not closed until it runs. Fact 3's palette is superseded for the body colour (section 2).

- **Chrome UI recipe (pending, post-catalogue).** Place a Wooden wall, a Brick wall and a Wall side by side, horizontal, with a Basketball 1 m above each. Tilt a Wooden wall and a Brick wall to the same angle and set a Weight on each. Place a Wooden wall upright across a Ramp run-out. Verify placement and dimensions through real UI controls.

All tests are planned. Family rows live in `CuriousContraptions.tests/BarrierMaterialsTests.cs`, a theory over `brick_barrier` and `wood_barrier`; the wood-only row in `WoodBarrierTests.cs`. "S4 row n" is the planned conformance theory; until it is delivered the named package test is written.

| # | Given / when / then | Planned test | Control | Boundary | Tolerance and stall guard |
| --- | --- | --- | --- | --- | --- |
| W1 | **Not brick's restitution.** Given a Basketball dropped 0.5 m onto a horizontal wooden barrier, then it rebounds at 0.33 of its approach speed | `BarrierMaterialsTests.ABallReboundsAtEachMaterialsOwnRestitution` | Brick 0.165; the Wall 0.55 (delivered, `VisibleSolidColliderTests.cs:684`) | — | Ratio within ±0.04; brick is 0.165 away, so wood compiled with brick's value fails |
| W2 | **Not brick's friction.** Given a barrier widened to 8 m, laid flat and tilted 22°, and a default Weight set on it base down, when Run advances 3 s, then on wood the Weight travels more than 0.3 m | `BarrierMaterialsTests.AWeightHoldsOnTiltedBrickAndSlidesOnTiltedWood` | On brick it moves less than 0.02 m | — | Expected slide about 2.4 m at 0.52 m/s²; 0.3 m allows a late start. A wood barrier with brick friction (0.49 against tan 22° = 0.404) does not move at all |
| W3 | **Independent declaration.** The wood, brick and Wall entries share interfaces, collider kind, bindings and parameter keys and bounds, and compile three different materials | `BarrierMaterialsTests.TheFamilySharesOneShapeAndEachMemberCompilesItsOwnMaterial` | — | — | Exact decoded material fields |
| W4 | **No passage.** Given an upright wooden barrier and a Bowling ball launched at 8 m/s, the centre never passes the near face | `BarrierMaterialsTests.AMovingBallNeverPassesTheIntactBarrier` | Barrier absent: the ball crosses | 64 m/s with the barrier yawed 45° (the retained CAT-066 maximum-speed boundary, `CAT-066-wall.md:88`; `CuriousContraptions.tests/WorkshopWallTests.cs:86`); `thickness` 0.12 m at 64 m/s | At 8 m/s the centre stays at least ball radius − 0.01 m outside the face (`docs/gpu-f32-physics.md:375`). At 64 m/s the retained criterion is the Wall theory's: the centre never reaches the near face plane (`WorkshopWallTests.cs:116`) |
| W5 | Dimensions are admitted at the default and both limits and round-trip; a value outside them or non-finite is refused atomically | S4 rows 1 and 2, else `BarrierMaterialsTests.DimensionsRoundTripAtTheLimitsAndInvalidValuesAreRefused` | — | The next f32 beyond each limit | Exact |
| W6 | Run, Reset and save/load restore the barrier, its dimensions and the ball's authored state | S4 row 3, else `BarrierMaterialsTests.ResetAndSaveLoadRestoreTheBarrierAndItsContact` | — | — | Authored state exact |
| W7 | No port, and a connection to one is refused; every art node is classified and the body has its collider at each dimension's limits | `BarrierMaterialsTests.TheBarrierHasNoPortsAndRefusesAForeignConnection`; one entry beside `VisibleSolidColliderTests.cs:118` | — | Dimension limits | Exact; art fit 0.002 m |
| W8 | **Combustible (later partner, after `ENGINE-IGNITION-BLAST`).** Given a wooden barrier heated to its ignition threshold with oxygen present, then it ignites, consumes its finite fuel and goes out; a brick barrier heated the same way does not ignite; Reset restores the unburnt panel | `WoodBarrierTests.ItIgnitesAtItsThresholdBurnsItsFiniteFuelAndBrickDoesNot` | Brick; wood held just below the threshold; wood with no oxygen | The threshold from each side; fuel exhausted | Temperature, energy and time tolerances and the stall guard are TH-04 section 11's (T12); the values are T11's. If heating and burn-out take longer than 30 s the row states its Run length (ENGINE-RUN-LENGTH, owner decision 13), at most 300 s (owner decision 17), and a burn longer than 240 s is asserted by direction and order, not completion (T12) |
| W9 | **Air occlusion (retained CAT-066 control; IX-12, IX-13).** Given a Fan blowing at a Tennis ball with an upright barrier between them, then the ball is not pushed | `BarrierMaterialsTests.AnUprightBarrierBlocksAFansStream` (pattern `CuriousContraptions.tests/FanGuidanceTests.cs:45`) | Barrier removed: the ball is pushed | — | Ball displacement under 0.02 m in 3 s against more than 0.3 m in the control |
| W10 | **Light and sound occlusion (later partner: light after `ENGINE-OPTICAL`, sound after Story 14.0; IX-12, IX-13).** An upright barrier blocks a beam and a tone that reach their receiver without it. Light follows [EL-176 section 10](EL-176-red-laser-emitter.md#10-optical-conventions-owned-here-every-optical-spec-cites-this-section): the barrier's box takes the default Absorb response (convention 6), so a receiver behind it reads zero; the tone uses the same segment cast (convention 5) | `BarrierMaterialsTests.AnUprightBarrierBlocksABeamAndATone` | Barrier removed: the receiver reads the beam's power | — | Light: the optical tolerances (coordinator decision 21), ±2 % of the emitted power in the control and zero here, after a 1 s guard. Sound: set by Story 14.0's fixtures. The identity stays open for this row until it runs |

The retained CAT-066 acceptance is rows W4 to W7 on this package, with its occlusion controls in W9 and W10.

## 6. Open questions

**Resolved**

1. Should level `solar_shadow` switch to the Wooden wall: **no, decided now.** The level text is legacy wording, not a requirement (D15); it stays a Wall lesson. A wood lesson is authored when the campaign's material-variant levels are.
2. The capability map row lacks ChemicalReaction and ReactionIgnition: **D14.** The map is navigation; the combustible slice's probe names the families.
3. Floating wood: **not applicable now.** The barrier is static. Whether wooden fragments float is decided by the slice that first makes one dynamic (D19).
4. Material density scale: **not declared now.** Fuel mass is a declared store value fixed with `ENGINE-IGNITION-BLAST`; the thermal scale is D5.
5. Strength for wood: **D19, none.** The row does not name it.
6. Friction and colour changed from the earlier draft: **decided now**, friction 0.4 so one tilt separates wood from brick with margin, and an ochre body so wood is not the Wall's colour.
7. Contact is delivered first, by the family slice after the engine programme; "combustible" follows `ENGINE-IGNITION-BLAST` as later-partner row W8, and the identity stays open until W8 (and W10) runs. **Coordinator decision under the standing delegation (22 November 2026)**, as `docs/planning/invest/delivery-batches.md:28` allows. No requirement criterion is dropped or weakened.

**Needs the owner**

None.

## 7. Integration

- **Collaborators.** Delivered: Basketball, Bowling ball, Tennis ball, Weight, Ramp, Receiver, Wall. Same slice: EL-073. Later: the heat sources of the TH family (candle, hot plate) for W8.
- **Campaign.** "Ramp/wall/material variants", first use levels 1–10, reuse 21–30, 41–50, 91–100 and 136–150 (`docs/planning/invest/campaign-reservations.md:20`). This slice authors no level; a level that places the barrier before it is playable needs the lesson-fixture story (S8).

### Coverage binding (planned, not yet live)

Published main binds a named-identity package through a final section of the identity's spec headed "Package source binding", read by `tools/Coverage/NamedPackageSources.cs` (on `origin/main`, `25df3cd5`; pattern: EL-239 Baseball). The section is not written here: the reader parses every EL spec that carries that heading and refuses one whose preconditions fail. **Blocked on the coverage follow-on story S7.**

| Field | Planned value |
| --- | --- |
| Package | `wood_barrier` |
| Named identity | EL-074, `../invest/named-elements.md#element-074` |
| Acceptance | `todo-199` and `sequence-task-627` |
| Decision | S644; `scope-corrections.md` has no `s644` block |
| Mode records | `Configuration=Adjustable` (it declares parameters; confirmed at slice planning) |

What it lacks today: an `s644` anchor block in `docs/planning/invest/scope-corrections.md`, and its todo-199 anchor is a scope index, not immediately followed by its sequence-task-627 anchor in `requirements.md`. S7 widens the reader (owner decision 15), including for companion packages and TH, RAD and GAP identities.

## 8. Delivery

- **Slice shape.** Family slice "Barrier materials" with EL-073: one candidate commit, own ledger row, acceptance rows and tests per identity. The combustible criterion (W8) is a later small slice on this package.
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), which includes ENGINE-SPATIAL-QUERIES and ENGINE-ISLANDS-SLEEP; then S7 (the coverage follow-on, which widens the reader, owner decision 15). For W8 only: then ENGINE-GAS-NETWORK, ENGINE-LIQUID and ENGINE-THERMAL (owner decision 18), then ENGINE-IGNITION-BLAST, and one delivered heat source. For W10 only: then ENGINE-OPTICAL and Story 14.0.
- **Files added.** `elements/WoodBarrier/WoodBarrier.csproj`, `WoodBarrierElementDeclaration.cs`, `wood_barrier.tscn`, `icon.svg`; `CuriousContraptions.tests/BarrierMaterialsTests.cs` (shared with EL-073). `WoodBarrierTests.cs` arrives with W8.
- **Files edited.** One entry beside `VisibleSolidColliderTests.cs:118`; until S3, `CuriousContraptions.slnx`, the trim list at `CSharpStaticGateTests.cs:687`–`:703` and the parameter and placement switches at `PackageRuntimeParityTests.cs:483` and `:641` (generic list `:653`); the binding section of section 7; this spec and `TODO.md`.
- **Review tier.** Data-only for the family slice once S3 has published; before S3, engine tier for the placement switch alone (`spec-family-ball-presets.md:124`). The W8 slice is tiered by its own probe.
- **Disqualifiers (the slice's probe re-checks).** The Wall's declaration shape and bounds are unchanged; the pair laws still hold for statics; the Weight's friction is still 0.3 (`elements/Weight/WeightElementDeclaration.cs:52`), which the 22° test depends on; the requirement row is still read as allowing "combustible" to follow its engine story.
