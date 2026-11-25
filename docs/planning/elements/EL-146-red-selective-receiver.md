# EL-146 · Red selective receiver — named-identity spec

**Current delivery (24 October 2026):** [EL-146 delivery slice](../invest/named-elements.md#element-146). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Brought to build-ready detail on 22 November 2026 against checkout `spec-drafts` (published main `8d1d68aa`) and repaired the same day after its audit and the owner decisions of 22 November 2026. Legacy citations use `path@a6c914e:Lstart-Lend`; citations without `@` are `path:line` in the checkout. Lengths are metres. The sensitive face looks along local −X. Optical rules are the [optical conventions](EL-176-red-laser-emitter.md#optical-conventions) and are not restated here. This spec is the lead of the seven selective receivers (EL-146 to EL-152) and of the broadband detector [EL-214](EL-214-broadband-beam-detector.md), for the probe, sources and conformance; they share one body and differ only in the required channel set, strength rule, purity, bar marks, id, title, description and icon, and each one's declaration is its CAT spec's (coordinator decision 27).

## 1. Identity

| Field | Value |
| --- | --- |
| ID | EL-146 |
| Name | Red selective receiver |
| Type | Optical |
| Anchor | [requirements.md#element-146](../requirements.md#element-146); named entry [named-elements.md#element-146](../invest/named-elements.md#element-146); source record [todo-300](../requirements.md#todo-300) |
| Owner | S501 |
| Disposition | Candidate **Duplicate closure** of [CAT-056 red_receiver](CAT-056-red_receiver.md) (`red_receiver`, "Red receiver"), closed inside that part's slice (the Colour receivers Family slice of [delivery-batches §1a](../invest/delivery-batches.md#1a-families-one-slice-each-if-the-probe-passes)). No delta from CAT-056. |
| Acceptance authority | The CAT-056 spec's acceptance rows are the single authority (coordinator decision 16, 22 November 2026). This identity's source outcome, "missing required channel, wrong channel and absent supply remain independent negative controls", is CAT-056 row **R2** (no red in the beam), rows **R3 and R5** (red present, other channels too strong) and the **control of R1** (absent supply); the ledger row cites the package and those tests. This spec carries no rows of its own. |
| Planned package | none of its own; CAT-056's `red_receiver` (`elements/RedReceiver/`). The CAT spec is the single declaration (coordinator decision 27, 23 November 2026). |
| Roadmap story | [Story 13.2](../../../_bmad-output/planning-artifacts/epics.md#story-132-broadband--pure-channel-optical-receivers-cat-038-cat-056-cat-032-cat-012) |
| Status | not started; **blocked on the engine programme (owner decision 3a, 22 November 2026)**, then on ENGINE-OPTICAL (which follows ENGINE-SPATIAL-QUERIES and takes every candidate from its segment query) and ENGINE-SIGNAL-NODES |

## 2. Declaration

The `red_receiver` declaration, art and bindings are [CAT-056's declaration](CAT-056-red_receiver.md#declaration); this identity adds none (coordinator decision 27, 23 November 2026).

## 3. Engine capabilities

Binding shard ([element-02.json](../../coverage/engine/element-02.json)): FiniteLedger, GeometryQuery, OpticalAbsorption, OpticalTransport, SensibleHeat, SignalPropagation, StateTransaction; navigation only (D14). Probe on `8d1d68aa`: 6 present, 1 present-untested, 2 narrower, 5 missing. CAT-056's probe is the slice's.

| # | Capability | Status | Symbol | Test opened |
| --- | --- | --- | --- | --- |
| R1 | Static scene body with colliders from `col_` nodes | present | `ISceneGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`) | `StateLatchTests.Compiler_DeclaresStaticLatchBodyWithCanonicalColliders` (`CuriousContraptions.tests/StateLatchTests.cs:503`) |
| R2 | Every visible solid has a collider | present | the package's row in the test's `Specs` | `VisibleSolidColliderTests.EverySolidArtPartHasItsColliderAndNoColliderReachesBeyondTheArt` (`CuriousContraptions.tests/VisibleSolidColliderTests.cs:237`) |
| R3 | Typed electrical ports and wrong-kind connection refusal | present | `PortDescriptor` (`CuriousContraptions.Abstractions/PortDescriptor.cs:55`) | `BundlePortsTests.AWrongDomainOrOutputToOutputLinkIsStillRefused` (`CuriousContraptions.tests/BundlePortsTests.cs:262`) |
| R4 | A supply contact closed by a committed signal state, one tick after the state | present | `SignalStateContactControl` (`SignalContactControl.cs:12`) | `StateLatchTests.Network_ContactClosedFollowsCommittedLatchMemoryAndCounterReach` (`StateLatchTests.cs:473`) |
| R12 | Run, Reset, save and load of every admitted declaration | present | loop over `TestBundle.Packages` | `CatalogueIntegrationTests.EveryAdmittedDeclarationRetainsAuthoredStateAcrossRunResetSaveLoad` (`CuriousContraptions.tests/CatalogueIntegrationTests.cs:273`) |
| R13 | Typed parameter with bounds refused atomically | present | `ParameterDescriptor` (`CuriousContraptions.Abstractions/ParameterDescriptor.cs:52`) | pattern `FanAdmissionTests.PackageScalarEndpointsAndClosedModeAreAdmittedAtomically` (`CuriousContraptions.tests/FanAdmissionTests.cs:267`); the slice adds this package's rows |
| R11 | A colour binding driven by a committed signal level | present, untested here: `PresentationDriver.CommittedActivation` (`CuriousContraptions.Abstractions/ElementPresentation.cs:22`) drives the Signal lamp's bulb (`elements/SignalLamp/SignalLampElementDeclaration.cs:13`); no test body was opened for it | `PresentationDriver` | none opened |
| R9 | A segment cast against solid colliders | narrower: `AirflowLineClear` answers clear or blocked for airflow rows only, by a scan of every collider (`CuriousContraptions.Simulation/WorkshopPhysicsEngine.Airflow.cs:141`, `:149`); the broadphase has only a pair-overlap query (`CuriousContraptions.Simulation/WorkshopDynamicBvh.cs:145`) | `AirflowLineClear` | `AirflowRuntimeTests.MovingSolidOcclusionIsResampledBeforeTransfer` (`CuriousContraptions.tests/AirflowGeneralityTests.cs:13`). Owner of the ordered-crossing cast: **ENGINE-SPATIAL-QUERIES** |
| R10 | Weighted sample points on a receiver | narrower: airflow receivers only (`AirflowSample`, `CuriousContraptions.Abstractions/AirflowValues.cs:42`; validation `:128`) | `AirflowSampleValidation` | `AirflowRuntimeTests.OneOfFourOccludedSamplesRemovesItsOwnQuarterOfDemand` (`AirflowGeneralityTests.cs:138`). Owner: **ENGINE-OPTICAL** |
| MS | A signal node "threshold over a committed scalar, with a release ratio" | **missing**: `SignalNodeKind` has no such node (`CuriousContraptions.Abstractions/SignalContractValues.cs:13`) | — | owner **ENGINE-SIGNAL-NODES** |
| M2 | The optical solve: once per tick after the last substep, on ordered crossings from the spatial query | **missing** | — | owner **ENGINE-OPTICAL** |
| M4 | Declared receivers: reading, sensed and required sets, strength rule, purity; committed reading and strength; conserved shares of diverging light | **missing** | — | owner **ENGINE-OPTICAL** |
| M5 | Beam read model | **missing** | — | owner **ENGINE-OPTICAL** |
| M6 | `ElementCategory.Optics` (`CuriousContraptions.Abstractions/ElementCategory.cs:5`–`:12`) and the optical domain flag | **missing** | — | owner **ENGINE-OPTICAL** |

No new contact control is needed: the receiver couples through observation → signal node → signal-state contact (coordinator decision 16). ENGINE-OPTICAL, ENGINE-SPATIAL-QUERIES and ENGINE-SIGNAL-NODES are stories of [the engine-first section of `epics.md`](../../../_bmad-output/planning-artifacts/epics.md#engine-first-capability-stories-21-november-2026).

**Dependencies.** A red-carrying source (red laser [EL-176](EL-176-red-laser-emitter.md), or the amber laser through the red filter [EL-143](EL-143-red-optical-filter.md)), Battery, a load (Powered gate or Motor, delivered), Wall.

## 4. Sources and legacy

**Sources.** Row [element-146](../requirements.md#element-146): separately supplied output requires red under the declared thresholds and channel-purity contract; missing required channel, wrong channel and absent supply are independent negative controls. [todo-300](../requirements.md#todo-300): selected channel at threshold, default 0.25, and at least 90% of total RGB; switches independent supplied power. CAT row [current-cat-056](../requirements.md#current-cat-056): below threshold, backside/blocked and no-supply controls; coherent threshold indication. Refinement **optical-pairings** (S694). Campaign: each receiver channel first use 51–60 ([campaign-element-coverage](../requirements.md#campaign-element-coverage)).

**Legacy** (shared optical facts S1–S8 in [CAT-036-laser](CAT-036-laser.md#legacy-harvest)):
1. Aperture `Main` (−0.18, 0, 0), −X, radius 0.55, Absorb — `parts/LightReceiverPart.cs@a6c914e:L21-L21`. Carry forward.
2. Threshold binding, validation 0.05–2 finite, undefined colour rejected — `parts/LightReceiverPart.cs@a6c914e:L11-L14`, `parts/LightReceiverPart.cs@a6c914e:L40-L46`; enum `ReceiverParameter { Threshold }` — `engine/MachineData.cs@a6c914e:L80-L80`. Carry forward.
3. Receive: store received RGB and match; Active = matched — `parts/LightReceiverPart.cs@a6c914e:L35-L39`. Carry forward behaviour; not the per-part callback.
4. Ports and contact route `PowerIn` → `Supply` gated by the match — `parts/LightReceiverPart.cs@a6c914e:L28-L34`. Carry forward.
5. Indicator 0.125 s smoothstep slate → gold, endpoint drive; art — `parts/LightReceiverPart.cs@a6c914e:L22-L27`, `parts/LightReceiverPart.cs@a6c914e:L47-L59`. Carry forward behaviour; not Godot materials.
6. Acceptance rule: red strength = R; accept iff strength ≥ threshold and power·mask ≥ 0.9 × total — `engine/OpticalColour.cs@a6c914e:L10-L10`, `engine/OpticalColour.cs@a6c914e:L35-L48`. Carry forward.
7. Catalogue entry and scene (Colour = 1) — `parts/catalog/red_receiver.tres@a6c914e:L6-L12`, `parts/scenes/red_receiver.tscn@a6c914e:L1-L5`. Carry forward the entry.
8. Acceptance: (1, 1, 1) rejected (purity); R 0.249 rejected; R 0.25 accepted; R 0.9 with 0.051 on each other channel rejected; R 0.91 with 0.045 accepted; a laser through a red filter lights the receiver, but the downstream powered gate gets no supply until the receiver's `PowerIn` is wired to a battery; wiring is refused during Run; Reset restores colour, inactive and zero reading — `CuriousContraptions.tests/ColourOpticsTests.cs@a6c914e:L128-L168`. Carry forward.
9. Acceptance: the contact reads an owned, checkpointed acceptance result, independent of the presentation `Active` flag, and rolls back with the transaction — `CuriousContraptions.tests/ElectricalContactBindingTests.cs@a6c914e:L85-L111`. Carry forward.
10. Acceptance: indicator follows committed feedback across failure, pause and Reset (half-blend at 0.0625 s; returns to slate after supply loss); supply disable at the battery opens the downstream gate — `CuriousContraptions.tests/ReceiverAnimationTests.cs@a6c914e:L44-L102`. Carry forward.
11. Acceptance: a failed tick restores every reading and the retry commits — `CuriousContraptions.tests/OpticalRuntimeCheckpointTests.cs@a6c914e:L30-L93`. Carry forward at the later fault gate.

**Files harvested:** `parts/LightReceiverPart.cs`, `parts/catalog/red_receiver.tres`, `parts/scenes/red_receiver.tscn`, `engine/OpticalColour.cs`, `engine/MachineData.cs`, `engine/OpticalNetwork.cs`, `reference/cpu/MachinePart.cs`, `CuriousContraptions.tests/ColourOpticsTests.cs`, `CuriousContraptions.tests/ElectricalContactBindingTests.cs`, `CuriousContraptions.tests/ReceiverAnimationTests.cs`, `CuriousContraptions.tests/OpticalRuntimeCheckpointTests.cs`.

## 5. Acceptance

**Authority.** Acceptance is [CAT-056's rows](CAT-056-red_receiver.md#acceptance-outline) R1 to R12, with their fixtures, tolerances and stall guards. This identity's outcomes map to **R2** (missing required channel), **R3 and R5** (wrong channel) and the **control of R1** (absent supply); the ledger row cites those tests. No row is stated here, so none can diverge, and no sibling inherits one.

**Behaviour and numbers** (the laws behind those rows).

- **Strength and match.** Strength is the red reading when red is at least 90 % of the total, otherwise zero. `matched` turns on at strength ≥ `threshold` and off below 0.9 × `threshold` (K-4). A red laser gives (10, 0, 0) W: on. An unfiltered amber laser gives (10, 7.8, 3.2) W: purity 10 ÷ 21 = 47.6 %, off. A green laser gives no red: off.
- **Any light, one law** (owner decision 9). A white Flashlight 3 m away gives 0.950 m² × 46.7 W/m² = 44.4 W, 14.8 W per channel: 33 % red, off. The same torch through a red filter gives (14.8, 0, 0) W: on.
- **Purity is taken over all light on the disc.** A 9 W red beam with 2.25 W of another channel is 9 ÷ 11.25 = 80 % red (off); with 0.5625 W of another channel, 94.1 % (on). The contaminating channel must not be the required one: for this receiver it is green or blue.
- **Contact and tick order.** The strength commits with the tick in which the light arrives; `matched` takes it at the start of the next tick and the contact closes `PowerIn` → `Supply` in that tick. With `PowerIn` unwired the node and the disc still turn on and nothing is supplied.

Chrome recipe (pending, post-catalogue): CAT-056's.

## 6. Open questions

None open.

1. **"Declared thresholds" — decided** (K-6): one authorable `threshold`; purity 0.9 is a declared constant.
2. **Hysteresis — decided** (K-4): release ratio 0.9, as the gates' controls.
3. **Heat from absorbed light — decided** (K-10): ledgered, not heat, until ENGINE-THERMAL.
4. **One catalogue part satisfies EL-146 — decided**: Duplicate closure with CAT-056's rows as the single authority (coordinator decision 16).
5. **Coupling — decided** (coordinator decision 16): observation → signal node → signal-state contact.

Needs the owner: none.

## 7. Integration

- **Collaborators.** Red laser, amber laser, red filter, combiner, mirror, Beam shutter, Flashlight (undelivered); Battery, Powered gate, Motor, Wall (delivered). Port domains agree: `PowerIn` Electrical Input, `Supply` Electrical Output. A detector behind an unpowered Beam shutter is dark in any mounting (owner decision 6).
- **Campaign.** Each receiver channel first use 51–60.
- **Coverage binding (planned, not yet live).** Catalogue key `red_receiver` (CAT-056). A closed duplicate has no binding section of its own: it is covered by the package of the part that satisfies it and by the closure record, which the coverage follow-on story S7 must admit (owner decision 15 widens the reader).
- **Lesson fixtures.** Story [S8](../invest/delivery-batches.md#s8-lesson-fixtures-for-any-admitted-package) is defined and lands after the engine programme (owner decision 22).

## 8. Delivery

- **Slice shape.** Closed inside the Colour receivers Family slice (CAT-056, CAT-032, CAT-012, CAT-072, CAT-021, CAT-040, CAT-068, CAT-038).
- **Prerequisites, in order.** The engine programme (owner decision 3a), including ENGINE-SPATIAL-QUERIES; ENGINE-SIGNAL-NODES (threshold node); ENGINE-OPTICAL as corrected in the batch summary; a laser (CAT-036).
- **Files (CAT slice).** `elements/RedReceiver/` (project, declaration, scene, icon), one guarded `CuriousContraptions.slnx` line, `CuriousContraptions.tests/SelectiveReceiverTests.cs`, a `VisibleSolidColliderTests` row, the ledger row for EL-146.
- **Review tier.** Data-only if the probe finds every row present after the engine stories.
- **Disqualifiers.** A receiver without purity; no threshold node with a release ratio; a different unit scale (re-derive the parameter range).

## 9. Engine conformance

Verdict: **Conforms.**

1. **Declaration data only.** Four colliders, two ports, one receiver row, one threshold node, one supply contact, one parameter, three bindings.
2. **Generic laws.** The receiver law with declared sets, rule and purity; the signal network's threshold rule; the delivered signal-state contact. "Red only" is data.
3. **Three layers.** Colliders: two boxes and two spheres. Visuals: flush disc and prints. Animation: the disc colour from the committed signal level.
4. **One-way flow and typing.** Segments → reading → strength → next tick's signal node → contact; the channel sets are closed enums; watts are f32.
5. **Flat state.** Counts in [CAT-056's declaration](CAT-056-red_receiver.md#declaration).
6. **Performance fitness.** Static; four primitives; one sample point, so one shadow cast per diverging segment.
7. **Tests are scenarios.** CAT-056's rows.
