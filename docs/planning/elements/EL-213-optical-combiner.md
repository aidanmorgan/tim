# EL-213 · Optical combiner — named-identity spec

**Current delivery (24 October 2026):** [EL-213 delivery slice](../invest/named-elements.md#element-213). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Brought to build-ready detail on 22 November 2026 against checkout `spec-drafts` (published main `8d1d68aa`) and repaired the same day after its audit and the owner decisions of 22 November 2026. Legacy citations use `path@a6c914e:Lstart-Lend`; citations without `@` are `path:line` in the checkout. Lengths are metres. Local +X is the outlet direction. Optical rules are the [optical conventions](EL-176-red-laser-emitter.md#optical-conventions) and are not restated here.

## 1. Identity

| Field | Value |
| --- | --- |
| ID | EL-213 |
| Name | Optical combiner |
| Type | Optical |
| Anchor | [requirements.md#element-213](../requirements.md#element-213); named entry [named-elements.md#element-213](../invest/named-elements.md#element-213); source record [todo-299](../requirements.md#todo-299); scope index [todo-215](../requirements.md#todo-215) |
| Owner | S509 |
| Disposition | Candidate **Duplicate closure** of [CAT-006 beam_combiner](CAT-006-beam_combiner.md) (`beam_combiner`, "Beam combiner"), closed inside that part's slice. No delta from CAT-006. |
| Acceptance authority | The CAT-006 spec's acceptance rows are the single authority (coordinator decision 16, 22 November 2026). This identity's source outcome, "a missing channel is not synthesized, and crossing unrelated beams does not combine by object identity", is CAT-006 rows **R2 and R8**; the ledger row cites the package and those tests. This spec carries no rows of its own. |
| Planned package | none of its own; CAT-006's `beam_combiner` (`elements/BeamCombiner/`). The CAT spec is the single declaration (coordinator decision 27); section 2 points to it. |
| Roadmap story | [Story 13.5](../../../_bmad-output/planning-artifacts/epics.md#story-135-planar-reflection-mirror-beam-splitter--beam-combiner-cat-041-cat-008-cat-006) |
| Status | not started; **blocked on the engine programme (owner decision 3a, 22 November 2026)**, then on ENGINE-OPTICAL, which follows ENGINE-SPATIAL-QUERIES and takes every candidate from its segment query |

## 2. Declaration

The `beam_combiner` declaration, art, bindings and compiled counts are [CAT-006, Declaration](CAT-006-beam_combiner.md#declaration); this identity adds none (coordinator decision 27, 23 November 2026). Palette: CAT-006's order 616 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)); no order of its own.

## 3. Engine capabilities

Binding shard ([element-03.json](../../coverage/engine/element-03.json)): FiniteLedger, GeometryQuery, OpticalAbsorption, OpticalTransport, SensibleHeat, SignalPropagation, StateTransaction; navigation only (D14). Probe on `8d1d68aa`: 3 present, 0 untested, 2 narrower, 4 missing. CAT-006's probe is the slice's.

| # | Capability | Status | Symbol | Test opened |
| --- | --- | --- | --- | --- |
| R1 | Static scene body with colliders from `col_` nodes | present | `ISceneGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`) | `StateLatchTests.Compiler_DeclaresStaticLatchBodyWithCanonicalColliders` (`CuriousContraptions.tests/StateLatchTests.cs:503`) |
| R2 | Every visible solid has a collider | present | the package's row in the test's `Specs` | `VisibleSolidColliderTests.EverySolidArtPartHasItsColliderAndNoColliderReachesBeyondTheArt` (`CuriousContraptions.tests/VisibleSolidColliderTests.cs:186`) |
| R12 | Run, Reset, save and load of every admitted declaration | present | loop over `TestBundle.Packages` | `CatalogueIntegrationTests.EveryAdmittedDeclarationRetainsAuthoredStateAcrossRunResetSaveLoad` (`CuriousContraptions.tests/CatalogueIntegrationTests.cs:273`) |
| R9 | A segment cast against solid colliders | narrower: `AirflowLineClear` answers clear or blocked for airflow rows only, by a scan of every collider (`CuriousContraptions.Simulation/WorkshopPhysicsEngine.Airflow.cs:141`, `:149`); the broadphase has only a pair-overlap query (`CuriousContraptions.Simulation/WorkshopDynamicBvh.cs:145`) | `AirflowLineClear` | `AirflowGeneralityTests.MovingSolidOcclusionIsResampledBeforeTransfer` (`CuriousContraptions.tests/AirflowGeneralityTests.cs:13`). Owner of the ordered-crossing cast: **ENGINE-SPATIAL-QUERIES** |
| R11 | Presentation binding on a committed driver | narrower: no driver reads committed outlet power (`CuriousContraptions.Abstractions/ElementPresentation.cs:18`–`:37`) | `PresentationDriver` | `BothGateTests.ThreeNativeLampsFollowCommittedInputsAndSupplyThroughHideAndReset` (`CuriousContraptions.tests/BothGateTests.cs:259`). Owner: **ENGINE-OPTICAL** |
| O2 | The optical solve: once per tick after the last substep, on ordered crossings from the spatial query, with range and interaction limits | **missing** | — | owner **ENGINE-OPTICAL** |
| O3 | Declared surface responses, here Relay from an inlet aperture to a declared outlet with an internal path, refused at bundle time if its outputs can exceed its input | **missing** | — | owner **ENGINE-OPTICAL** |
| O5 | Committed outlet-power driver, beam read model with co-linear merging, build-mode preview | **missing** | — | owner **ENGINE-OPTICAL** |
| O6 | `ElementCategory.Optics` (`CuriousContraptions.Abstractions/ElementCategory.cs:5`–`:12`) and the optical domain flag | **missing** | — | owner **ENGINE-OPTICAL** |

Rows O2 to O6 are the rows numbered M2 to M6 in the other optical specs, lettered O here so they are not confused with CAT-006's acceptance rows. ENGINE-OPTICAL is [its story in `epics.md`](../../../_bmad-output/planning-artifacts/epics.md#story-engine-optical-beam-emission-transport-surface-interaction-and-reception); ENGINE-SPATIAL-QUERIES is named by owner decision 3a and has its story there too.

**Dependencies.** Lasers ([EL-176](EL-176-red-laser-emitter.md) to EL-178, or CAT-036 with filters), mixed receivers ([EL-149](EL-149-yellow-selective-receiver.md) to EL-152) or a broadband detector, Battery, Wall, mirrors for the loop row.

## 4. Sources and legacy

**Sources.** Row [element-213](../requirements.md#element-213): compatible incident channels leave an explicit output aperture with conserved combined power and declared coupling loss; a missing channel is not synthesised; crossing unrelated beams do not combine by object identity. Campaign: first use 51–60; practice 71–80; reuse 117 and 142 ([campaign-reservations](../invest/campaign-reservations.md)). [todo-299](../requirements.md#todo-299): three cream input rims, gold output rim, numbered marks; each ray keeps 90% power and its remaining range/interaction budget, counting internal travel. CAT row [current-cat-006](../requirements.md#current-cat-006).

**Legacy** (shared optical facts S1–S8 in [CAT-036-laser](CAT-036-laser.md#legacy-harvest); full harvest in [CAT-006-beam_combiner](CAT-006-beam_combiner.md#legacy-harvest); key facts):
1. Retention 0.9, outlet, three Route apertures — `parts/BeamCombinerPart.cs@a6c914e:L12-L20`. Carry forward.
2. Input power aggregate, output power from the outlet, Active only when output > 0; output lamp follow rate 12; preview MergeCollinear — `parts/BeamCombinerPart.cs@a6c914e:L21-L49`. Carry forward behaviour; not the per-part callbacks.
3. Art and port marks — `parts/BeamCombinerPart.cs@a6c914e:L50-L75`. Carry forward.
4. Route law and outlet accounting — `engine/OpticalNetwork.cs@a6c914e:L119-L125`, `engine/OpticalNetwork.cs@a6c914e:L171-L182`. Carry forward; not the CPU solve.
5. Catalogue entry — `parts/catalog/beam_combiner.tres@a6c914e:L6-L11`. Carry forward.
6. Acceptance: the outlet lamp shows only light that actually exits (range 2.1 short: input read, output 0) — `CuriousContraptions.tests/BeamCombinerTests.cs@a6c914e:L16-L33`; three filtered amber lasers give a white receiver laser power × 0.9 (±1e-5) with one outgoing segment, for rotated and reordered sets; Reset clears everything — `CuriousContraptions.tests/BeamCombinerTests.cs@a6c914e:L39-L90`; each input keeps its remaining range (total path 5 ± 0.001 including internal travel) and never creates channels — `CuriousContraptions.tests/BeamCombinerTests.cs@a6c914e:L91-L117`. Carry forward.
7. Acceptance: a three-mirror loop back into the combiner keeps one 16-interaction budget with strictly falling power — `CuriousContraptions.tests/BeamCombinerTests.cs@a6c914e:L138-L160`; artwork sums shared paths only over their overlap and keeps crossing beams separate — `CuriousContraptions.tests/BeamCombinerTests.cs@a6c914e:L161-L177`; back face and housing block, and a wall on the output still occludes — `CuriousContraptions.tests/BeamCombinerTests.cs@a6c914e:L178-L199`. Carry forward.
8. Acceptance: output channels publish atomically across rollback, pause, Reset and Save/Load; output = 0.9 × input (±1e-5) — `CuriousContraptions.tests/OpticalObservationTests.cs@a6c914e:L66-L117`. Carry forward.

**Files harvested:** `parts/BeamCombinerPart.cs`, `parts/catalog/beam_combiner.tres`, `engine/OpticalNetwork.cs`, `reference/cpu/MachinePart.cs`, `CuriousContraptions.tests/BeamCombinerTests.cs`, `CuriousContraptions.tests/OpticalObservationTests.cs`.

## 5. Acceptance

**Authority.** Acceptance is [CAT-006's rows](CAT-006-beam_combiner.md#acceptance-outline) R1 to R13, with their fixtures, tolerances and stall guards. This identity's outcomes map to **R2** (a missing channel is not synthesised) and **R8** (crossing beams do not combine); the ledger row cites those tests. No row is stated here, so none can diverge.

**Behaviour and numbers** (the laws behind those rows).

- **Law.** Each collimated segment that ends on an inlet starts its own segment at the outlet with P′ = 0.9 × P, its own remaining range less the inlet's internal path, and its own interaction count plus one. Segments leaving the one outlet are co-linear, so a receiver downstream sums them. Red, green and blue lasers of 10 W each give (9, 9, 9) W; three filtered amber beams give (9, 7.02, 2.88) W. Total out is 0.9 × total in: 30 W in, 27 W out, 3 W absorbed (ledgered, K-10).
- **No synthesis.** With the green laser off the outlet carries (9, 0, 9) W.
- **No combining by proximity.** Two beams that cross in the air pass through each other; a beam that hits the cube outside an inlet is absorbed. A Flashlight's cone on an inlet is absorbed: only collimated segments are relayed.
- **Range.** A beam that has travelled 14.8 m when it reaches the First inlet has 1.2 m left, less than the 1.40 m internal path: nothing leaves and the outlet lens stays dark. Entering after 10 m it leaves with 16 − 10 − 1.40 = 4.6 m.
- **Supply check.** Three colour lasers demand 36 W, three amber lasers 75 W, both under the Battery's 120 W.

Chrome recipe (pending, post-catalogue): CAT-006's.

## 6. Open questions

None open.

1. **"Merge orthogonal beams into one" — decided** (D15): each beam is relayed separately to the common outlet; the visible result is one mixed beam and each keeps its own budget.
2. **Coupling loss — decided** (D19): fixed at 0.1.
3. **One catalogue part satisfies EL-213 — decided**: Duplicate closure with CAT-006's rows as the single authority (coordinator decision 16).

Needs the owner: none.

## 7. Integration

- **Collaborators.** Colour lasers, filters, mixed receivers, mirrors, prism [EL-174](EL-174-prism.md) (its inverse: prism then combiner recombines at 0.9 × 0.9 = 0.81), gates (undelivered); Battery, Wall (delivered). No ports.
- **Campaign.** First use 51–60; practice 71–80; reuse 117 and 142.
- **Coverage binding (planned, not yet live).** Catalogue key `beam_combiner` (CAT-006). A closed duplicate has no binding section of its own: it is covered by the package of the part that satisfies it and by the closure record, which the coverage follow-on story S7 must admit (owner decision 15 widens the reader).
- **Lesson fixtures.** Story [S8](../invest/delivery-batches.md#s8-lesson-fixtures-for-any-admitted-package) is defined and lands after the engine programme (owner decision 22).

## 8. Delivery

- **Slice shape.** Closed inside CAT-006's slice.
- **Prerequisites, in order.** The engine programme (owner decision 3a), including ENGINE-SPATIAL-QUERIES; ENGINE-OPTICAL as corrected in the batch summary; lasers and a detector; the mirror for CAT-006's loop row.
- **Files (CAT slice).** `elements/BeamCombiner/` (project, declaration, scene, icon), one guarded `CuriousContraptions.slnx` line, `CuriousContraptions.tests/BeamCombinerTests.cs`, a `VisibleSolidColliderTests` row, the ledger row for EL-213.
- **Review tier.** Data-only if the probe finds every row present after the engine stories.
- **Disqualifiers.** A Relay that cannot share one outlet among several inlets or that has no declared internal path; no outlet-power driver; a different unit scale.

## 9. Engine conformance

Verdict: **Conforms.**

1. **Declaration data only.** Two colliders, three relay rows, one outlet, three bindings.
2. **Generic laws.** The Relay law; "combining" is the receiver's sum of co-linear segments, not a rule of this part.
3. **Three layers.** Colliders: two boxes. Visuals: flush prints. Animation: the outlet lens colour from committed outlet power.
4. **One-way flow and typing.** Segments in, segments out; channels are the closed set.
5. **Flat state.** Counts in section 2.
6. **Performance fitness.** Static; two primitives; at most one new segment per arriving segment, inside the per-emitter segment limit.
7. **Tests are scenarios.** CAT-006's rows.
