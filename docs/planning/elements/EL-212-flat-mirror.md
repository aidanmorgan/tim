# EL-212 · Flat mirror — named-identity spec

**Current delivery (24 October 2026):** [EL-212 delivery slice](../invest/named-elements.md#element-212). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Brought to build-ready detail on 22 November 2026 against checkout `spec-drafts` (published main `8d1d68aa`) and repaired the same day after its audit and the owner decisions of 22 November 2026. Legacy citations use `path@a6c914e:Lstart-Lend`; citations without `@` are `path:line` in the checkout. Lengths are metres. The silvered face looks along local −X. Optical rules are the [optical conventions](EL-176-red-laser-emitter.md#optical-conventions) and are not restated here.

## 1. Identity

| Field | Value |
| --- | --- |
| ID | EL-212 |
| Name | Flat mirror |
| Type | Optical |
| Anchor | [requirements.md#element-212](../requirements.md#element-212); named entry [named-elements.md#element-212](../invest/named-elements.md#element-212); scope index [todo-215](../requirements.md#todo-215) |
| Owner | S496 |
| Disposition | Candidate **Duplicate closure** of [CAT-041 mirror](CAT-041-mirror.md) (`mirror`, "Flat mirror"), closed inside that part's slice. No delta from CAT-041. |
| Acceptance authority | The CAT-041 spec's acceptance rows are the single authority (coordinator decision 16, 22 November 2026). This identity's source outcome, "rear incidence and occluded paths do not reflect through the housing", is CAT-041 rows **R3 and R4**; the ledger row cites the package and those tests. This spec carries no rows of its own. |
| Planned package | none of its own; CAT-041's `mirror` (`elements/Mirror/`). The CAT spec is the single declaration (coordinator decision 27); section 2 points to it. |
| Roadmap story | [Story 13.5](../../../_bmad-output/planning-artifacts/epics.md#story-135-planar-reflection-mirror-beam-splitter--beam-combiner-cat-041-cat-008-cat-006) |
| Status | not started; **blocked on the engine programme (owner decision 3a, 22 November 2026)**, then on ENGINE-OPTICAL, which follows ENGINE-SPATIAL-QUERIES and takes every candidate from its segment query |

## 2. Declaration

The `mirror` declaration, art, bindings and compiled counts are [CAT-041, Declaration](CAT-041-mirror.md#declaration); this identity adds none (coordinator decision 27, 23 November 2026). Palette: CAT-041's order 603 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)); no order of its own.

## 3. Engine capabilities

Binding shard ([element-03.json](../../coverage/engine/element-03.json)): FiniteLedger, GeometryQuery, OpticalAbsorption, OpticalTransport, SensibleHeat, SignalPropagation, StateTransaction; navigation only (D14). Probe on `8d1d68aa`: 3 present, 0 untested, 1 narrower, 4 missing. CAT-041's probe is the slice's.

| # | Capability | Status | Symbol | Test opened |
| --- | --- | --- | --- | --- |
| R1 | Static scene body with colliders from `col_` nodes | present | `ISceneGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`) | `StateLatchTests.Compiler_DeclaresStaticLatchBodyWithCanonicalColliders` (`CuriousContraptions.tests/StateLatchTests.cs:503`) |
| R2 | Every visible solid has a collider | present | the package's row in the test's `Specs` | `VisibleSolidColliderTests.EverySolidArtPartHasItsColliderAndNoColliderReachesBeyondTheArt` (`CuriousContraptions.tests/VisibleSolidColliderTests.cs:237`) |
| R12 | Run, Reset, save and load of every admitted declaration | present | loop over `TestBundle.Packages` | `CatalogueIntegrationTests.EveryAdmittedDeclarationRetainsAuthoredStateAcrossRunResetSaveLoad` (`CuriousContraptions.tests/CatalogueIntegrationTests.cs:273`) |
| R9 | A segment cast against solid colliders, with crossing point and normal | narrower: `AirflowLineClear` answers clear or blocked for airflow rows only, with no point or normal, by a scan of every collider (`CuriousContraptions.Simulation/WorkshopPhysicsEngine.Airflow.cs:141`, `:149`); the broadphase has only a pair-overlap query (`CuriousContraptions.Simulation/WorkshopDynamicBvh.cs:145`) | `AirflowLineClear` | `AirflowRuntimeTests.MovingSolidOcclusionIsResampledBeforeTransfer` (`CuriousContraptions.tests/AirflowGeneralityTests.cs:13`). Owner of the ordered-crossing cast: **ENGINE-SPATIAL-QUERIES** |
| O2 | The optical solve: once per tick after the last substep, on ordered crossings from the spatial query, with range and interaction limits | **missing** | — | owner **ENGINE-OPTICAL** |
| O3 | Declared surface responses on colliders (here Reflect on a disc aperture of one cap) | **missing** | — | owner **ENGINE-OPTICAL** |
| O5 | Beam read model and the build-mode aim preview, with a stated collider source in Build | **missing** | — | owner **ENGINE-OPTICAL** |
| O6 | `ElementCategory.Optics` (`CuriousContraptions.Abstractions/ElementCategory.cs:5`–`:12`) and the optical domain flag | **missing** | — | owner **ENGINE-OPTICAL** |

Rows O2 to O6 are the rows numbered M2 to M6 in the other optical specs; they are lettered O here so they are not confused with CAT-041's acceptance rows. ENGINE-OPTICAL is [its story in `epics.md`](../../../_bmad-output/planning-artifacts/epics.md#story-engine-optical-beam-emission-transport-surface-interaction-and-reception); ENGINE-SPATIAL-QUERIES is named by owner decision 3a and has its story there too.

**Dependencies.** A laser (CAT-036 or [EL-176](EL-176-red-laser-emitter.md) to EL-178), Battery, a detector (CAT-038 / [EL-214](EL-214-broadband-beam-detector.md)), Wall, a ball.

## 4. Sources and legacy

**Sources.** Row [element-212](../requirements.md#element-212): a finite front surface reflects supported optical power according to its normal and material model; the opaque frame and back remain physical; rear incidence and occluded paths do not reflect through the housing. Campaign: first use 51–60; practice 61–70; reuse 111–120 and 142 as separate objectives ([campaign-reservations](../invest/campaign-reservations.md)). CAT row [current-cat-041](../requirements.md#current-cat-041): 95% RGB per bounce, one range budget, up to 16 reflections, back/frame/mount/returning-ray occlusion, preview equals committed reflection.

**Legacy** (shared optical facts S1–S8 in [CAT-036-laser](CAT-036-laser.md#legacy-harvest); full mirror harvest in [CAT-041-mirror](CAT-041-mirror.md#legacy-harvest); key facts):
1. Aperture `Main` (−0.18, 0, 0), −X, radius 0.65, Mirror; preview composition Separate — `parts/MirrorPart.cs@a6c914e:L9-L11`. Carry forward.
2. Geometry and art (backing and base authored full sizes; `AddBox` stores halves) — `parts/MirrorPart.cs@a6c914e:L12-L29`; `reference/cpu/MachinePart.cs@a6c914e:L352-L356`. Carry forward.
3. Reflection law, retention 0.95, range continuation, depth increment; 16 interactions — `engine/OpticalNetwork.cs@a6c914e:L31-L33`, `engine/OpticalNetwork.cs@a6c914e:L112-L127`. Carry forward.
4. Front-face-only acceptance for non-split apertures — `engine/OpticalNetwork.cs@a6c914e:L94-L98`. Carry forward (rear incidence control).
5. Catalogue entry — `parts/catalog/mirror.tres@a6c914e:L6-L11`. Carry forward.
6. Acceptance: 45° mirror reflects 90° into a receiver 3 below with power × 0.95; reflection follows any 3D rotation; Reset empties the path — `CuriousContraptions.tests/MirrorTests.cs@a6c914e:L15-L54`; two mirrors × 0.95² share one range budget — `CuriousContraptions.tests/MirrorTests.cs@a6c914e:L55-L79`; wall on the reflected leg, reversed mirror and a ray 0.73 off-axis all fail to reflect — `CuriousContraptions.tests/MirrorTests.cs@a6c914e:L80-L109`; facing mirrors stop at 17 segments with falling power — `CuriousContraptions.tests/MirrorTests.cs@a6c914e:L126-L143`. Carry forward.
7. Acceptance: preview hidden for placement ghosts, matches the committed trace, hidden during Run — `CuriousContraptions.tests/MirrorTests.cs@a6c914e:L110-L125`, `CuriousContraptions.tests/OpticalPreviewPresentationTests.cs@a6c914e:L45-L82`. Carry forward.

**Files harvested:** `parts/MirrorPart.cs`, `parts/catalog/mirror.tres`, `engine/OpticalNetwork.cs`, `reference/cpu/MachinePart.cs`, `CuriousContraptions.tests/MirrorTests.cs`, `CuriousContraptions.tests/OpticalPreviewPresentationTests.cs`.

## 5. Acceptance

**Authority.** Acceptance is [CAT-041's rows](CAT-041-mirror.md#acceptance-outline) R1 to R11, with their fixtures, tolerances and stall guards. This identity's outcomes map to **R3** (rear incidence) and **R4** (occluded paths; nothing reflects through the housing or outside the face); the ledger row cites those tests. No row is stated here, so none can diverge.

**Behaviour and numbers** (the laws behind those rows).

- **Law.** Reflect: d′ = d − 2(d·n)n about the cap's world normal; P′ = 0.95 × P per channel. A 10 W red beam leaves as 9.5 W; after two mirrors 10 × 0.95² = 9.03 W; after sixteen 10 × 0.95¹⁶ = 4.40 W. The 5 % is absorbed by the mirror (ledgered; not heat until ENGINE-THERMAL, K-10). The reflected segment starts at the hit point with the remaining range: one 16 m budget for the whole path.
- **A 45° mirror.** The face normal at rest is (−1, 0, 0). Turned **+45° about Z** (right-handed) it is n = (−0.7071, −0.7071, 0), the value CAT-041 gives. For a beam d = (1, 0, 0): d·n = −0.7071, so d′ = (1, 0, 0) + 1.4142 × (−0.7071, −0.7071, 0) = (0, −1, 0): the beam leaves straight down. Turned −45° it would leave straight up.
- **Finite face.** A ray striking the cap more than 0.65 m from its centre, the cylinder's side, the backing or the base is absorbed. A ray reaching the back never meets the cap.
- **Limits.** Two facing mirrors stop after 16 reflections: 17 segments, each weaker than the last.
- **Bodies.** All three colliders are ordinary solids; a ball rebounds from the disc face.

Chrome recipe (pending, post-catalogue): CAT-041's.

## 6. Open questions

None open.

1. **"Material model" — decided** (D19): one declared reflectance of 0.95 for every channel; not authorable.
2. **Heat from the absorbed 5 % and back-face light — decided** (K-10): ledgered, not heat, until ENGINE-THERMAL.
3. **Observer order — resolved**: receivers (Story 13.2) precede Story 13.5.
4. **One catalogue part satisfies EL-212 — decided**: Duplicate closure with CAT-041's rows as the single authority (coordinator decision 16).

Needs the owner: none.

## 7. Integration

- **Collaborators.** Lasers, detectors, splitter (CAT-008), combiner [EL-213](EL-213-optical-combiner.md), filters, gates (undelivered); Wall, balls, Battery (delivered). A Flashlight's cone is not reflected: it is blocked by the mirror like any solid (conventions, item 3). The optical fibre [EL-175](EL-175-optical-fibre.md) must not erase the mirror lesson; its inventory is restricted in mirror levels. No ports.
- **Campaign.** First use 51–60; practice 61–70; reuse 111–120 and 142.
- **Coverage binding (planned, not yet live).** Catalogue key `mirror` (CAT-041). A closed duplicate has no binding section of its own: it is covered by the package of the part that satisfies it and by the closure record, which the coverage follow-on story S7 must admit (owner decision 15 widens the reader).
- **Lesson fixtures.** Story [S8](../invest/delivery-batches.md#s8-lesson-fixtures-for-any-admitted-package) is defined and lands after the engine programme (owner decision 22).

## 8. Delivery

- **Slice shape.** Closed inside CAT-041's slice.
- **Prerequisites, in order.** The engine programme (owner decision 3a), including ENGINE-SPATIAL-QUERIES; ENGINE-OPTICAL as corrected in the batch summary; a laser (CAT-036); a detector (CAT-038).
- **Files (CAT slice).** `elements/Mirror/` (project, declaration, scene, icon), one guarded `CuriousContraptions.slnx` line, `CuriousContraptions.tests/FlatMirrorTests.cs`, a `VisibleSolidColliderTests` row, the ledger row for EL-212.
- **Review tier.** Data-only if the probe finds every row present after the engine stories.
- **Disqualifiers.** A Reflect response that cannot be limited to a disc on a cylinder cap (then the reflecting face is redrawn as the collider's whole cap, radius 0.65 m); a segment cast without a normal; no Build preview from the same solve; a different interaction limit.

## 9. Engine conformance

Verdict: **Conforms.**

1. **Declaration data only.** Three colliders and one Reflect response with a declared reflectance and aperture.
2. **Generic laws.** The Reflect law; contact for bodies. "Mirror" appears nowhere in the engine.
3. **Three layers.** Colliders: two boxes and a cylinder. Visuals: backing, base, disc and flat prints; the collider's declared response reflects, not the art. Animation: none.
4. **One-way flow and typing.** Segments in, segments out; nothing returns from presentation, and the preview commits nothing.
5. **Flat state.** Counts in section 2.
6. **Performance fitness.** Static; three primitives; one extra segment per reflection, bounded by the interaction limit.
7. **Tests are scenarios.** CAT-041's rows.
