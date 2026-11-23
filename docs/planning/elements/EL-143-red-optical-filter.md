# EL-143 · Red optical filter — named-identity spec

**Current delivery (24 October 2026):** [EL-143 delivery slice](../invest/named-elements.md#element-143). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Brought to build-ready detail on 22 November 2026 against checkout `spec-drafts` (published main `8d1d68aa`) and repaired the same day after its audit and the owner decisions of 22 November 2026. Legacy citations use `path@a6c914e:Lstart-Lend`; citations without `@` are `path:line` in the checkout. Lengths are metres. The pane's faces look along local ±X. Optical rules are the [optical conventions](EL-176-red-laser-emitter.md#optical-conventions) and are not restated here. This spec is the lead of the three colour filters (EL-143 to EL-145) for the probe, sources and conformance; the filters differ only in the transmitted channel, pane colour, bar count, id, title, description and icon, and their declarations are CAT-055, CAT-031 and CAT-011's (coordinator decision 27).

## 1. Identity

| Field | Value |
| --- | --- |
| ID | EL-143 |
| Name | Red optical filter |
| Type | Optical |
| Anchor | [requirements.md#element-143](../requirements.md#element-143); named entry [named-elements.md#element-143](../invest/named-elements.md#element-143); source record [todo-297](../requirements.md#todo-297) |
| Owner | S498 |
| Disposition | Candidate **Duplicate closure** of [CAT-055 red_filter](CAT-055-red_filter.md) (`red_filter`, "Red filter"), closed inside that part's slice (the Colour filters Family slice of [delivery-batches §1a](../invest/delivery-batches.md#1a-families-one-slice-each-if-the-probe-passes)). No delta from CAT-055. |
| Acceptance authority | The CAT-055 spec's acceptance rows are the single authority (coordinator decision 16, 22 November 2026). This identity's source outcome, "absent red input cannot be recoloured into red; opaque frame blocks rays", is CAT-055 rows **F4 and F5**; the ledger row cites the package and those tests. This spec carries no rows of its own. |
| Planned package | none of its own; CAT-055's `red_filter` (`elements/RedFilter/`). The CAT spec is the single declaration (coordinator decision 27, 23 November 2026). |
| Roadmap story | [Story 13.4](../../../_bmad-output/planning-artifacts/epics.md#story-134-spectral-color-bandpass-filters-red-green-blue-cat-055-cat-031-cat-011) |
| Status | not started; **blocked on the engine programme (owner decision 3a, 22 November 2026)**, then on ENGINE-OPTICAL, which follows ENGINE-SPATIAL-QUERIES and takes every candidate from its segment query |

## 2. Declaration

The `red_filter` declaration, art and bindings are [CAT-055's declaration](CAT-055-red_filter.md#declaration); this identity adds none (coordinator decision 27, 23 November 2026).

## 3. Engine capabilities

Binding shard ([element-02.json](../../coverage/engine/element-02.json)): FiniteLedger, GeometryQuery, OpticalAbsorption, OpticalTransport, SensibleHeat, SignalPropagation, StateTransaction; navigation only (D14). Probe on `8d1d68aa`: 3 present, 0 untested, 1 narrower, 4 missing. CAT-055's probe is the slice's.

| # | Capability | Status | Symbol | Test opened |
| --- | --- | --- | --- | --- |
| R1 | Static scene body with colliders from `col_` nodes | present | `ISceneGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`) | `StateLatchTests.Compiler_DeclaresStaticLatchBodyWithCanonicalColliders` (`CuriousContraptions.tests/StateLatchTests.cs:503`) |
| R2 | Every visible solid has a collider | present | the package's row in the test's `Specs` | `VisibleSolidColliderTests.EverySolidArtPartHasItsColliderAndNoColliderReachesBeyondTheArt` (`CuriousContraptions.tests/VisibleSolidColliderTests.cs:186`) |
| R12 | Run, Reset, save and load of every admitted declaration | present | loop over `TestBundle.Packages` | `CatalogueIntegrationTests.EveryAdmittedDeclarationRetainsAuthoredStateAcrossRunResetSaveLoad` (`CuriousContraptions.tests/CatalogueIntegrationTests.cs:273`) |
| R9 | A segment cast against solid colliders, with entry and exit of each crossing | narrower: `AirflowLineClear` answers clear or blocked for airflow rows only, with no crossing points and no per-collider transmission, by a scan of every collider (`CuriousContraptions.Simulation/WorkshopPhysicsEngine.Airflow.cs:141`, `:149`); the broadphase has only a pair-overlap query (`CuriousContraptions.Simulation/WorkshopDynamicBvh.cs:145`) | `AirflowLineClear` | `AirflowGeneralityTests.MovingSolidOcclusionIsResampledBeforeTransfer` (`CuriousContraptions.tests/AirflowGeneralityTests.cs:13`). Owner of the ordered-crossing cast: **ENGINE-SPATIAL-QUERIES** |
| M2 | The optical solve: once per tick after the last substep, on ordered crossings from the spatial query | **missing** | — | owner **ENGINE-OPTICAL** |
| M3 | Declared surface responses on colliders, here Transmit by channel on a collider that is solid to bodies, applied once per crossing | **missing** | — | owner **ENGINE-OPTICAL** |
| M5 | Beam read model and build-mode aim preview | **missing** | — | owner **ENGINE-OPTICAL** |
| M6 | `ElementCategory.Optics` (`CuriousContraptions.Abstractions/ElementCategory.cs:5`–`:12`) and the optical domain flag | **missing** | — | owner **ENGINE-OPTICAL** |

[ENGINE-OPTICAL](../../../_bmad-output/planning-artifacts/epics.md#story-engine-optical-beam-emission-transport-surface-interaction-and-reception)'s acceptance already says "a channel-selective surface passes only its declared channels". ENGINE-SPATIAL-QUERIES is a story of the same engine-first section of `epics.md`.

**Dependencies.** A multi-channel source (amber laser CAT-036, or the combiner [EL-213](EL-213-optical-combiner.md)), Battery, an observer (CAT-056 / [EL-146](EL-146-red-selective-receiver.md) or CAT-038), Wall, a ball.

## 4. Sources and legacy

**Sources.** Row [element-143](../requirements.md#element-143): passes only the existing red channel through its finite aperture with declared loss; absent red input cannot be recoloured; opaque frame blocks rays. [todo-297](../requirements.md#todo-297): finite two-sided apertures, transparent solid panes, opaque frames; unlike stacked filters extinguish. CAT row [current-cat-055](../requirements.md#current-cat-055): front, back and rotated paths; red artwork does not recolour numerical power. [todo-309](../requirements.md#todo-309): passive optics cannot amplify. Refinement **optical-pairings** (S694, [refinements](../invest/refinements.md)). Campaign: filters first use 51–60 ([campaign-element-coverage](../requirements.md#campaign-element-coverage)).

**Legacy** (shared optical facts S1–S8 in [CAT-036-laser](CAT-036-laser.md#legacy-harvest)):
1. Aperture `Main` at origin, normal −X, radius 0.65, Filter, transmission = channel mask — `parts/ColourFilterPart.cs@a6c914e:L10-L12`; red mask (1, 0, 0) — `engine/OpticalColour.cs@a6c914e:L11-L16`. Carry forward.
2. Only Red, Green or Blue is a valid filter channel; others reject — `parts/ColourFilterPart.cs@a6c914e:L15-L19`. Carry forward.
3. Frame, base and non-opaque pane collider; pick radius 1.2 — `parts/ColourFilterPart.cs@a6c914e:L20-L29`; `AddBox` stores half the authored size — `reference/cpu/MachinePart.cs@a6c914e:L352-L356`. Carry forward.
4. Pane art, marks and preview — `parts/ColourFilterPart.cs@a6c914e:L30-L42`; inks and mark counts (Red = 1) — `engine/OpticalColour.cs@a6c914e:L23-L34`, `engine/OpticalColour.cs@a6c914e:L61-L76`. Carry forward behaviour; not Godot materials.
5. Filter apertures accept either face; the branch continues straight with power × transmission and the remaining range — `engine/OpticalNetwork.cs@a6c914e:L94-L98`, `engine/OpticalNetwork.cs@a6c914e:L113-L126`. Carry forward.
6. Catalogue entry and scene (Colour = 1) — `parts/catalog/red_filter.tres@a6c914e:L6-L11`, `parts/scenes/red_filter.tscn@a6c914e:L1-L5`. Carry forward the entry.
7. Acceptance: laser → filter → red receiver, front and back (180°): two segments, one reception equal to source × mask; the receiver activates but does not supply without its own supply; a source lacking red gives no reception and one segment; an arbitrary rotation (23°, 37°, 11°) keeps the same reception — `CuriousContraptions.tests/ColourOpticsTests.cs@a6c914e:L69-L103`. Carry forward.
8. Acceptance: red then blue filters extinguish (two segments, no reception); two red filters give three segments and do not amplify (reception = first filtered power) — `CuriousContraptions.tests/ColourOpticsTests.cs@a6c914e:L104-L127`. Carry forward.
9. Acceptance: a ray 0.74 above centre hits the opaque frame and stops before the filter; a ball at 4 m/s bounces off the pane (impact recorded, ball returns), and Reset replays identically — `CuriousContraptions.tests/ColourOpticsTests.cs@a6c914e:L169-L212`. Carry forward.
10. Acceptance: red beam ink equals `#de7058`, and a 0.32-power beam renders with alpha 0.223–0.225 — `CuriousContraptions.tests/ColourOpticsTests.cs@a6c914e:L213-L230`. Carry forward as presentation.
11. Integration: red filter in front of an amber laser feeds the combiner's first input — `CuriousContraptions.tests/BeamCombinerTests.cs@a6c914e:L39-L90`. Carry forward.

**Files harvested:** `parts/ColourFilterPart.cs`, `parts/catalog/red_filter.tres`, `parts/scenes/red_filter.tscn`, `engine/OpticalColour.cs`, `engine/OpticalNetwork.cs`, `reference/cpu/MachinePart.cs`, `CuriousContraptions.tests/ColourOpticsTests.cs`, `CuriousContraptions.tests/BeamCombinerTests.cs`.

## 5. Acceptance

**Authority.** Acceptance is [CAT-055's rows](CAT-055-red_filter.md#acceptance-outline) F1 to F9, with their fixtures, tolerances and stall guards. This identity's outcomes map to **F4** (a beam without red leaves no red) and **F5** (the opaque frame blocks a ray; the pane is solid to bodies); the ledger row cites those tests. No row is stated here, so none can diverge.

**Behaviour and numbers** (the laws behind those rows).

- **Law.** Transmit by channel: P′ = τ ∘ P with τ = (1, 0, 0). An amber beam (10, 7.8, 3.2) W leaves as (10, 0, 0) W; the 7.8 + 3.2 = 11 W of green and blue is absorbed by the pane. That absorbed power is the declared loss (decision K-5); the passed channel is not attenuated. The segment continues straight from the pane's far face with its remaining range and one interaction used.
- **No creation.** A beam without red, such as a green laser's (0, 10, 0) W, ends at the pane. Two red filters pass 10 W, not more. A red then a blue filter pass nothing.
- **Cone light.** A Flashlight's diverging light is multiplied by the pane on its way to any sample behind it: (30, 30, 30) W white arrives red only. Redirecting responses do not act on a cone; a Transmit surface does (conventions, item 3).
- **Bodies.** The pane is an ordinary collider with restitution 1; a ball rebounds by the pair's product restitution.

Chrome recipe (pending, post-catalogue): CAT-055's.

## 6. Open questions

None open.

1. **"Declared loss" — decided** (K-5, D15, D19): the passed channel is transmitted at 1.0; the absorbed channels are the loss.
2. **White test source — decided** (D18, K-9): the amber laser stands in until the Flashlight, which is white, exists.
3. **Heat from absorbed light — decided** (K-10): ledgered, not heat, until ENGINE-THERMAL.
4. **One catalogue part satisfies EL-143 — decided**: Duplicate closure with CAT-055's rows as the single authority (coordinator decision 16).

Needs the owner: none.

## 7. Integration

- **Collaborators.** Amber laser, colour lasers, combiner, receivers, gates, Flashlight (all undelivered); Battery, Wall and balls (delivered). No ports.
- **Campaign.** Filters first use 51–60; reuse with the combiner in 71–80.
- **Coverage binding (planned, not yet live).** Catalogue key `red_filter` (CAT-055). A closed duplicate has no binding section of its own: it is covered by the package of the part that satisfies it and by the closure record, which the coverage follow-on story S7 must admit (owner decision 15 widens the reader).
- **Lesson fixtures.** Story [S8](../invest/delivery-batches.md#s8-lesson-fixtures-for-any-admitted-package) is defined and lands after the engine programme (owner decision 22).

## 8. Delivery

- **Slice shape.** Closed inside the Colour filters Family slice (CAT-055, CAT-031, CAT-011).
- **Prerequisites, in order.** The engine programme (owner decision 3a), including ENGINE-SPATIAL-QUERIES; ENGINE-OPTICAL as corrected in the batch summary; a laser (CAT-036) and a detector (CAT-038).
- **Files (CAT slice).** `elements/RedFilter/` (project, declaration, scene, icon), one guarded `CuriousContraptions.slnx` line, `CuriousContraptions.tests/ColourFilterTests.cs`, a `VisibleSolidColliderTests` row, the ledger row for EL-143.
- **Review tier.** Data-only if the probe finds every row present after the engine stories.
- **Disqualifiers.** A Transmit response that cannot sit on a collider solid to bodies; a segment cast without exit points (a 0.03 m pane then cannot be crossed once); a different unit scale.

## 9. Engine conformance

Verdict: **Conforms.**

1. **Declaration data only.** Six colliders and one surface response with a declared transmission vector.
2. **Generic laws.** Transmit by channel, once per crossing; contact and restitution for bodies. Nothing reads "filter".
3. **Three layers.** Colliders: six boxes. Visuals: frame, base, pane and flat marks; the pane's art is not what filters, its collider's declared response is. Animation: none.
4. **One-way flow and typing.** Segments in, segments out; channels are the closed set.
5. **Flat state.** Counts in [CAT-055's declaration](CAT-055-red_filter.md#declaration).
6. **Performance fitness.** Static; six primitives, reason given; one extra segment per crossing beam.
7. **Tests are scenarios.** CAT-055's rows.
