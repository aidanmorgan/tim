# EL-173 · Diverging lens — named-identity spec

**Current delivery (24 October 2026):** [EL-173 delivery slice](../invest/named-elements.md#element-173). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Brought to build-ready detail on 22 November 2026 against checkout `spec-drafts` (published main `8d1d68aa`). Legacy citations use `path@a6c914e:Lstart-Lend`; citations without `@` are `path:line` in the checkout. Lengths are metres. The lens faces look along local ±X. Optical rules are the [optical conventions](EL-176-red-laser-emitter.md#optical-conventions) and are not restated here.

## 1. Identity

| Field | Value |
| --- | --- |
| ID | EL-173 |
| Name | Diverging lens |
| Type | Optical |
| Anchor | [requirements.md#element-173](../requirements.md#element-173); named entry [named-elements.md#element-173](../invest/named-elements.md#element-173); source record [todo-305](../requirements.md#todo-305) |
| Owner | S519 (refinement S524 divergent-lens) |
| Disposition | To deliver as a **single element**. It refines no catalogue part. It shares the colour filters' frame ([EL-143](EL-143-red-optical-filter.md)) and differs in its declared surface response: Transmit with a spread instead of Transmit by channel. The converging lens is a separate thermal identity (TH-06). |
| Planned package | id `diverging_lens`, directory `elements/DivergingLens/`, class `DivergingLensElementDeclaration` |
| Roadmap story | [EL-173 delivery slice](../invest/named-elements.md#element-173), `named-el-173`; after ENGINE-OPTICAL, a laser and detectors |
| Status | not started; no package; **blocked on the engine programme (owner decision 3a, 22 November 2026)**, then on ENGINE-OPTICAL, which follows ENGINE-SPATIAL-QUERIES and takes every candidate from its segment query |

## 2. Declaration

Shape followed: the colour filter ([CAT-055, Declaration](CAT-055-red_filter.md#declaration), which EL-143 points to; coordinator decision 27): a static frame of boxes around one pane whose two faces carry a Transmit response.

| Item | Value |
| --- | --- |
| Interfaces | `ISceneGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`) plus the optical element interface **(optics)** |
| Body | One static scene body; material restitution 1, friction 0.3, rolling resistance 0, bounce threshold 0.1 m/s |
| Colliders | Two frame bars 0.18 × 0.18 × 1.65 m at (0, ±0.74, 0); two frame bars 0.18 × 1.3 × 0.18 m at (0, 0, ±0.74); base 0.9 × 0.2 × 1.65 m at (0, −0.93, 0); lens slab 0.12 × 1.3 × 1.3 m at the origin. Six boxes. |
| Optical response **(optics)** | The slab collider: Transmit τ = (0.95, 0.95, 0.95), from either side, spread half-angle 15°, applied once per crossing; the continuing segment starts at the exit point on the far face. The whole 1.3 × 1.3 m slab is the aperture. Bars and base: Absorb. |
| Ports, parameters, stores, bindings | None. The spread is fixed, to keep the teaching contrast with the converging lens (D19). |
| Footprint | `ElementFootprint(1, 0, 0, 0)`; domain: the optical flag **(optics, M6)** |
| Catalogue | Id `diverging_lens`, "Diverging lens", `ElementCategory.Optics` **(optics, M6)**, colour (0.40, 0.72, 0.79), palette order 626 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)). Description: "Spreads a beam into a wider cone. Several targets can share it, but each gets only part of the light." |

### Art and solidity

| Art node | Mesh and size | Class | Moved by |
| --- | --- | --- | --- |
| `Visual/Frame` (four bars) | boxes as the colliders, cream `#fff8e9` | solid (four bar boxes) | nothing |
| `Visual/Base` | box 0.9 × 0.2 × 1.65 m at y −0.93, navy `#293954` | solid (base box) | nothing |
| `Visual/Lens` | slab 0.12 × 1.3 × 1.3 m, cyan `#66b8c9` at alpha 0.3, flat faces, with a thin ochre `#e8b764` edge line | solid (lens slab) | nothing |
| Concave marks | two inward-curving cream strokes printed flat on each face | decorative: printed flat | nothing |

The lens is drawn as a flat slab so that its art equals its box collider; the concavity is shown by the two printed strokes, not by curved glass that no admitted collider could match. It differs from the colour filters by a pane four times thicker (0.12 m against 0.03 m) with curved strokes and no bar marks, and from the mirror by being see-through.

### Compiled counts

Bodies 1 (static). Colliders 6 (boxes), as the filter. Optical surfaces 1. Everything else 0.

## 3. Engine capabilities

Binding shard ([element-02.json](../../coverage/engine/element-02.json)): FiniteLedger, GeometryQuery, OpticalAbsorption, OpticalTransport, SensibleHeat, SignalPropagation, StateTransaction; navigation only (D14). Probe on `8d1d68aa`: 3 present, 0 untested, 1 narrower, 4 missing.

| # | Capability | Status | Symbol | Test opened |
| --- | --- | --- | --- | --- |
| R1 | Static scene body with colliders from `col_` nodes | present | `ISceneGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`) | `StateLatchTests.Compiler_DeclaresStaticLatchBodyWithCanonicalColliders` (`CuriousContraptions.tests/StateLatchTests.cs:503`) |
| R2 | Every visible solid has a collider | present | the package's row in `Specs` | `VisibleSolidColliderTests.EverySolidArtPartHasItsColliderAndNoColliderReachesBeyondTheArt` (`CuriousContraptions.tests/VisibleSolidColliderTests.cs:237`) |
| R12 | Run, Reset, save and load of every admitted declaration | present | loop over `TestBundle.Packages` | `CatalogueIntegrationTests.EveryAdmittedDeclarationRetainsAuthoredStateAcrossRunResetSaveLoad` (`CuriousContraptions.tests/CatalogueIntegrationTests.cs:273`) |
| R9 | Line-of-sight occlusion by solid colliders | narrower: airflow rows only, by a scan of every collider (`CuriousContraptions.Simulation/WorkshopPhysicsEngine.Airflow.cs:141`, `:149`); the broadphase has only a pair-overlap query (`CuriousContraptions.Simulation/WorkshopDynamicBvh.cs:145`); no crossing points | `AirflowLineClear` | `AirflowRuntimeTests.MovingSolidOcclusionIsResampledBeforeTransfer` (`CuriousContraptions.tests/AirflowGeneralityTests.cs:13`). Owner of the ordered-crossing segment cast: **ENGINE-SPATIAL-QUERIES** |
| M2 | The optical solve: once per tick after the last substep, including diverging segments lighting receiver samples by the distance law with conserved shares | **missing** | — | owner **ENGINE-OPTICAL** |
| M3 | Declared surface responses, here Transmit with a declared spread half-angle | **missing** | — | owner **ENGINE-OPTICAL** |
| M5 | Beam and cone read model, build-mode aim preview | **missing** | — | owner **ENGINE-OPTICAL** |
| M6 | `ElementCategory.Optics` (`CuriousContraptions.Abstractions/ElementCategory.cs:5`–`:12`) and the optical domain flag | **missing** | — | owner **ENGINE-OPTICAL** |

ENGINE-OPTICAL is [its story in `epics.md`](../../../_bmad-output/planning-artifacts/epics.md#story-engine-optical-beam-emission-transport-surface-interaction-and-reception). Its text has "divergence" as an emitter property only; a spread on a transmitting surface is the same quantity applied to a continuing segment, and the addition is in the [batch summary](readiness/batch-k.md). The finite-width coverage that [todo-309](../requirements.md#todo-309) requires before any lens is the diverging segment law of the conventions; it is not a capability of this part.

**Dependencies.** A laser (CAT-036 or [EL-176](EL-176-red-laser-emitter.md)), Battery, two receivers ([EL-146](EL-146-red-selective-receiver.md) or CAT-038), Wall.

## 4. Sources and legacy

**Sources.** Row [element-173](../requirements.md#element-173): finite-aperture refraction broadens illumination while conserving transmitted power; two receivers share the beam and neither obtains the unsplit full power. [todo-305](../requirements.md#todo-305): cover two low-threshold detectors with finite-width coverage and partial occlusion. Refinement S524 **divergent-lens** ([refinements](../invest/refinements.md)). [todo-309](../requirements.md#todo-309): splitting and focusing cannot create power; finite-width coverage first. Campaign: converging/diverging lens first use 51–60 ([campaign-element-coverage](../requirements.md#campaign-element-coverage)).

**Legacy** (no lens; mechanisms only):
1. Filter frame geometry and transparent pane collider — `parts/ColourFilterPart.cs@a6c914e:L20-L29`. Carry forward as the frame.
2. Two-sided apertures and continued range — `engine/OpticalNetwork.cs@a6c914e:L94-L98`, `engine/OpticalNetwork.cs@a6c914e:L113-L127`. Carry forward.
3. Mirror retention 0.95 and the 16-interaction budget — `engine/OpticalNetwork.cs@a6c914e:L31-L33`. Carry forward as the loss and budget.
4. Cone geometry (cosine 0.9659258) and the distance law — `parts/FlashlightPart.cs@a6c914e:L10-L13`, `engine/LightNetwork.cs@a6c914e:L12-L52`. Carry forward as the shape basis.

**Files harvested:** `parts/ColourFilterPart.cs`, `engine/OpticalNetwork.cs`, `parts/FlashlightPart.cs`, `engine/LightNetwork.cs`, `reference/cpu/MachinePart.cs`.

## 5. Acceptance

### Behaviour and numbers

Starting values (D1).

- **Law.** A collimated segment that meets the slab continues from the hit point along its own direction as a diverging segment with half-angle 15° and P′ = 0.95 × P. Transmitted power is conserved over the cone: it is spread, not multiplied. A 10 W red laser gives 9.5 W over Ω = 0.2141 sr, I = 9.5 ÷ 0.2141 = 44.4 W/sr. The 5 % is absorbed by the lens.
- **Two receivers share it.** Lens 2 m from the laser; two red receivers 4 m beyond the lens, 0.8 m either side of the axis, facing back along it. Each centre is √(16 + 0.64) = 4.08 m from the hit point and atan(0.8 ÷ 4) = 11.3° off the axis, inside the 15° cone; facing factor 4 ÷ 4.08 = 0.981. E = 44.4 × 0.981 ÷ 16.64 = 2.62 W/m²; each 0.950 m² disc receives 2.49 W. Together they receive 4.97 W of the 9.5 W transmitted, so the conservation rule (conventions, item 8) leaves both shares unscaled; neither can receive the unsplit 10 W. If receivers crowded into the cone would together take more than 9.5 W, every share is scaled by 9.5 ÷ their sum: four such discs side by side at 1.5 m (uncapped 0.950 × 44.4 ÷ 2.25 = 18.7 W each on the axis) can never total more than 9.5 W.
- **Cone size.** The footprint radius is d × tan 15° = 0.268 d: 1.07 m at 4 m. Without the lens the beam is a zero-width ray passing between the two receivers, whose nearest edges are 0.25 m from the axis.
- **Already diverging light.** A torch's cone crossing the slab is multiplied by 0.95 and not widened: a diverging segment is terminal and starts no further segment (decision K-3).
- **Range and limits.** The diverging segment keeps the remaining range: with the lens 2 m from the lens point, light reaches 14 m beyond it. The crossing uses one interaction.

### Rows

Planned class `DivergingLensTests`. Fixture as above: Battery, a pulsed red laser, the lens 2 m along its +X, two supplied red receivers with `threshold` 1.0 W. Tolerances are the one optical set (coordinator decision 21): power ±2 %, stall guard 1 s, segment end points ±0.02 m.

| # | Given / When / Then | Test | Control | Boundary and tolerance |
| --- | --- | --- | --- | --- |
| D1 | Given the fixture, then each receiver reads 2.49 W red, both levels are on, and the two readings sum to less than 9.5 W (ledger outcome: two receivers share the beam; neither obtains the unsplit power) | `DivergingLensTests.TwoReceiversShareTheSpreadBeam` | D2 | ±2 % each; each reading below half of 9.5 W |
| D2 | Given the same receivers with `threshold` 5 W, then both stay off (twice the reading). Given the lens removed, both read nothing; one receiver moved onto the axis then reads the whole 10 W and the other nothing | `DivergingLensTests.SpreadLightIsWeakerAndTheLensIsWhatSpreadsIt` | D1 | ±2 % on the 10 W direct reading |
| D3 | Given a box shadowing one receiver, then that receiver reads nothing and the other still reads 2.49 W | `DivergingLensTests.AShadowRemovesOnlyItsOwnShare` | D1 | ±2 % |
| D4 | Given receivers at 6 m and 8 m beyond the lens on the axis, then the reading falls as 1 ÷ d²: 0.950 × 44.4 ÷ 36 = 1.17 W and 0.950 × 44.4 ÷ 64 = 0.66 W | `DivergingLensTests.SpreadLightFallsWithDistanceSquared` | D1 | ±2 %; the ratio 64 ÷ 36 = 1.78 within 2 % |
| D5 | Given a ray 0.74 m above the slab centre, then it ends on the frame bar; given the lens turned 180°, D1 holds unchanged | `DivergingLensTests.TheFrameBlocksAndEitherFaceSpreads` | D1 | segment end within 0.02 m of the bar |
| D6 | Given a Tennis ball rolled at the slab, then it rebounds and never passes | `DivergingLensTests.TheLensIsSolidToBodies` | — | centre never crosses the slab |
| D7 | Given a torch 2 m behind the lens and a detector 3 m in front of it, then the detector's mean is 0.95 × its reading without the lens (0.950 × 420.4 ÷ 25 ÷ 3 = 5.33 W becomes 5.06 W) | `DivergingLensTests.ConeLightIsDimmedNotWidened` | no lens | ±2 %; the row asserts the ratio 0.95 within 2 % |
| D9 | Given one receiver on the axis 1.5 m beyond the lens, where its uncapped share would be 18.7 W, then it reads no more than the 9.5 W transmitted | `DivergingLensTests.ReceiversNeverTakeMoreThanTheLensTransmits` | D1 | reading ≤ 9.5 W + 2 % |
| D8 | Given a lit run, when Reset, then no segment or reading remains; a saved and reloaded construction behaves as D1 | `DivergingLensTests.ResetAndReloadRestoreTheDarkState`; authored state by R12 | D1 | — |

No port and no parameter, so no refusal rows. Chrome recipe (pending, post-catalogue): Battery, triggered laser, the lens 2 m along the beam, two receivers side by side 4 m beyond set to a low threshold, each with a lamp circuit; Run.

## 6. Open questions

None open.

1. **Finite-width model — decided** (K-1, K-3): the conventions' diverging segment law; the earlier "share of footprint area" is the same quantity (irradiance × receiver area).
2. **Spread and retention — decided** (D1, D19): 15° and 0.95, fixed.
3. **Torch cones — decided** (K-3): dimmed by 0.95, not widened.
4. **Roadmap — resolved**: its own slice; finite-width coverage is delivered by ENGINE-OPTICAL, not by this slice.

Needs the owner: none.

## 7. Integration

- **Collaborators.** Lasers, receivers, mirrors upstream, prism [EL-174](EL-174-prism.md) (a diverging segment reaching the prism's inlet is absorbed there: only collimated segments are relayed), light sensor [EL-153](EL-153-general-light-receiver.md) (undelivered); Wall, balls, Battery (delivered). No ports.
- **Campaign.** Converging and diverging lens first use 51–60.
- **Coverage binding (planned, not yet live).** A named package is bound by the binding section that main's `tools/Coverage/NamedPackageSources.cs` reads; **blocked on the coverage follow-on story S7** (decision S519 has no anchor in `docs/planning/invest/scope-corrections.md`). S7 also widens the reader to admit companion packages and non-EL identities (owner decision 15); the section is not written here.
- **Lesson fixtures.** Story [S8](../invest/delivery-batches.md#s8-lesson-fixtures-for-any-admitted-package) is defined and lands after the engine programme (owner decision 22).

## 8. Delivery

- **Slice shape.** Single element.
- **Prerequisites, in order.** The engine programme (owner decision 3a), including ENGINE-SPATIAL-QUERIES; ENGINE-OPTICAL as corrected in the batch summary (Transmit spread, conserved shares); a laser; two receivers; the Flashlight for row D7 (queued until delivered); S7 for the binding.
- **Files.** `elements/DivergingLens/DivergingLens.csproj`, `DivergingLensElementDeclaration.cs`, `diverging_lens.tscn`, `icon.svg`; one guarded `CuriousContraptions.slnx` line; `CuriousContraptions.tests/DivergingLensTests.cs`; a `VisibleSolidColliderTests` row; this spec's binding section.
- **Review tier.** Data-only if the probe finds every row present after ENGINE-OPTICAL.
- **Disqualifiers.** No spread on a Transmit response (the part cannot be declared; the story is amended first); a spread that multiplies power per receiver instead of dividing it; a different unit scale.

## 9. Engine conformance

Verdict: **Conforms.**

1. **Declaration data only.** Six colliders and one Transmit response with a declared transmission and spread.
2. **Generic laws.** Transmit with spread; the diverging segment law; contact for bodies. Refraction is declared as its result, not solved.
3. **Three layers.** Colliders: six boxes. Visuals: frame, base, flat slab, printed strokes. Animation: none.
4. **One-way flow and typing.** Segments in, one diverging segment out.
5. **Flat state.** Counts in section 2.
6. **Performance fitness.** Static; six primitives as the filter; one diverging segment per crossing beam, costing one shadow ray per receiver sample in its cone.
7. **Tests are scenarios.** Rows D1 to D8.
