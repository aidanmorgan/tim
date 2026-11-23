# RAD-12 · Scintillator tile — element readiness spec

**Current delivery (24 October 2026):** [RAD-12 delivery slice](../invest/named-elements.md#radiation-12). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Story 7.0 Batch O named-identity spec, brought to build-ready detail on 22 November 2026 against checkout `spec-drafts` (published main `8d1d68aa`) and repaired the same day after the independent audit (`docs/planning/elements/readiness/audits/elements-batch-o.md`) and coordinator decisions 16 and 21. Legacy citations are pinned to `a6c914e`; citations without `@` are `path:line` in the checkout. Requirement row: [radiation-12](../requirements.md#radiation-12) (P2 potential). Lengths are metres; radiation enters the −X face and light leaves the +X face. Radiation units and laws are in the [radiation conventions](RAD-08-radiation-rate-meter.md#radiation-conventions); light follows the [optical conventions](EL-176-red-laser-emitter.md#optical-conventions). All values are game fiction.

## 1. Identity

| Item | Value |
| --- | --- |
| ID / name | RAD-12 · Scintillator tile |
| Type | Radiation to optical converter |
| Anchor | [requirements.md#radiation-12](../requirements.md#radiation-12); [named-elements.md#radiation-12](../invest/named-elements.md#radiation-12) |
| Owner | S622 |
| Disposition | To deliver as a single element. It refines no CAT part and duplicates none: it is the only part that turns received radiation into a light beam. |
| Planned package | id `scintillator_tile`, directory `elements/ScintillatorTile/`, class `ScintillatorTileElementDeclaration` |
| Related identities | Light observer: the Blue receiver ([CAT-012](CAT-012-blue_receiver.md) / [EL-148](EL-148-blue-selective-receiver.md)); sources [RAD-01](RAD-01-gamma-source-capsule.md), [RAD-02](RAD-02-powered-x-ray-emitter.md); shield control [EL-128](EL-128-dense-shield.md) |
| Status | not started; no package; blocked on the engine programme (owner decision 3a, 22 November 2026), ENGINE-OPTICAL, ENGINE-RADIATION and the Blue receiver CAT-012 |
| Run length needed | 30 s (the default); no row is longer than 2 s |

## 2. Declaration

Shape followed: the Signal lamp for emission bindings on a glowing face (`elements/SignalLamp/SignalLampElementDeclaration.cs:16`–`:18`); the composable-routing fixture for a package-internal finite-store source → signal-state contact → consumer (`CuriousContraptions.tests/ComposableSupplyRoutingTests.cs:453`–`:466`). The Signal lamp is a signal load with one Level `ActivationIn` (`SignalLampElementDeclaration.cs:9`, `:35`–`:38`), not an `ISupplyElement`, so the electrical load in the rows is the Powered gate (`elements/PoweredGate/PoweredGateElementDeclaration.cs:14`, `PowerIn` at `:38`). The tile converts radiation to light by the one form of [radiation convention 10](RAD-08-radiation-rate-meter.md#radiation-conventions) (coordinator decision 21, 22 November 2026): a credit of a declared fraction of its receiver's absorbed energy into a finite store, from which an ordinary supply source pays a consumer whose target is the Blue emitter. Items marked **(field)** use contracts that do not exist yet.

| Item | Value |
| --- | --- |
| Interfaces | `ISupplyElement` (`CuriousContraptions.Abstractions.Electrical/ElectricalContracts.cs:11`), `ISignalElement` (`CuriousContraptions.Abstractions.Signal/SignalContracts.cs:8`), `ISceneGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`), plus the receiver and emitter interfaces **(field)** |
| Body | One static scene body; restitution 1, friction 0.3, rolling resistance 0, bounce threshold 0.1 m/s (the static default, `elements/Battery/BatteryElementDeclaration.cs:61`–`:64`) |
| Colliders (hidden `col_` proxies) | Tile box 0.10 × 0.60 × 0.60 m at the origin, **Scintillator** row (16, 6, 3, 400, 150, 0.2, 0.2 per metre, radiation convention 4) **(field)**. Base box 0.30 × 0.06 × 0.60 m at (0, −0.33, 0), touching the tile, no radiation row (the scene's default row). Two colliders. |
| Ports | None. Radiation and light both cross space; the supply graph is internal. |
| Radiation receiver **(field)** | Frame body 0; aperture square 0.60 × 0.60 m on the −X face, centre (−0.05, 0, 0), normal −X, area 0.36 m²; nine samples at offsets 0 and ±0.20 m, weight 1/9; Lambert facing; sensed channels PhotonSoft, PhotonMedium, PhotonHard, Alpha and BetaMinus, weight 1 each; NeutronFast and NeutronSlow weight 0 (not sensed). Its strength is the incident power on the sensed channels; its absorbed power is radiation convention 10's (per sample and channel, the share × (1 − exp(−μ × chord)) for the chord of that sample's ray through the frame body). No level of its own. |
| Credit and store **(field)** | Each tick 0.8 × the receiver's absorbed energy is credited to the finite store of source `store`: capacity 0.05 J, power limit 4 W, initial charge 0, enabled, all declared values. Credit that would exceed capacity is not taken. |
| Signal graph | One threshold node `charged` over the store's committed charge: on at ≥ 0.0005 J, off below 0.5 × 0.0005 J **(field, ENGINE-SIGNAL-NODES)**. No signal port. |
| Supply graph | Source `store` (`SupplySourceDescriptor`, `CuriousContraptions.Abstractions/SupplyContractValues.cs:39`) → contact `gate` (`SignalStateContactControl` naming `charged`) → consumer `glow`, demand 4 W, whose target is the Blue emitter **(field, ENGINE-OPTICAL)**. Three supply nodes, two internal edges, no port binding. |
| Optical emitter **(field)** | Frame body 0; origin (0.05, 0, 0); axis +X; half-angle 0 (collimated); range 16 m; radiant power (0, 0, 4) W, Blue only; electrical demand 4 W; internal occluder = the tile box. It emits radiant × energy granted to `glow` over the tick's substeps ÷ energy demanded (optical convention 2), so its light equals the energy drawn from the store. |
| Parameters | None (D13). |
| Presentation bindings | `Visual/Face` emission blue from the committed emission of the emitter, `PresentationCurve.ExponentialFollow` at 12 /s **(field)**. The beam is drawn by the shared beam renderer from the committed segment table. |
| Footprint | `ElementFootprint(Bodies: 1, MovingBodies: 0, PrismaticConstraints: 0, RevoluteConstraints: 0)`; domains Electrical, Signal and the optical and radiation flags **(field)** |
| Catalogue | Id `scintillator_tile`, "Scintillator", `ElementCategory.Radiation` **(field)**, palette order 714 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)). Description: "Radiation absorbed on its back becomes a thin blue beam from its front. A fifth of the absorbed energy is lost." |

### Art and solidity

| Art node | Mesh and size | Class | Moved by |
| --- | --- | --- | --- |
| `Visual/Tile` | box 0.10 × 0.60 × 0.60 m, cream `#fff8e9` | solid (tile box) | nothing |
| `Visual/Base` | box 0.30 × 0.06 × 0.60 m at y −0.33, navy `#293954` | solid (base box) | nothing |
| `Visual/Face` | cyan `#66b8c9` square printed flush on the +X face | decorative: flush, lit by a binding | nothing (emission only) |
| `Visual/Outlet` | gold `#f7cb52` ring r 0.05 m printed flat at the centre of the +X face | decorative: printed flat | nothing |
| `Visual/Arrow` | arrow printed flat on the top edge, from −X to +X | decorative: printed flat | nothing |

Icon: a slab with a wavy line entering one side and a straight line leaving the other. It differs from the shields by the arrow and outlet ring and by having no grooves.

### Compiled counts

Bodies 1 (static). Colliders 2 boxes. Joints 0. Drives 0. Sensors 0. Signal nodes 1 (threshold). Supply nodes 3 (source, contact, consumer). Receivers 1 (nine samples). Optical emitters 1. Stores 1 (the source's finite store). Presentation bindings 3 (emission R, G, B).

## 3. Engine capabilities

Map row families (navigation, D14): FiniteLedger, GeometryQuery, IonizingTransport, OpticalTransport. Probe made on `8d1d68aa`: **4 present, 0 untested, 3 narrower, 5 missing**. ENGINE-SPATIAL-QUERIES and ENGINE-SIGNAL-NODES are stories of owner decision 3a.

| # | Capability | Status | Symbol | Test opened |
| --- | --- | --- | --- | --- |
| R1 | Static scene body with colliders from `col_` nodes | present | `ISceneGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`) | `StateLatchTests.Compiler_DeclaresStaticLatchBodyWithCanonicalColliders` (`CuriousContraptions.tests/StateLatchTests.cs:503`) |
| R2 | Every visible solid has a collider | present | per-package row | `VisibleSolidColliderTests.EverySolidArtPartHasItsColliderAndNoColliderReachesBeyondTheArt` (`CuriousContraptions.tests/VisibleSolidColliderTests.cs:186`) |
| R3 | Run, Reset, save and load of every admitted declaration | present | loop over `TestBundle.Packages` | `CatalogueIntegrationTests.EveryAdmittedDeclarationRetainsAuthoredStateAcrossRunResetSaveLoad` (`CuriousContraptions.tests/CatalogueIntegrationTests.cs:273`) |
| R4 | A package-internal finite-store source → signal-state contact → consumer route | present | `SupplySourceDescriptor` (`CuriousContraptions.Abstractions/SupplyContractValues.cs:39`), `SignalStateContactControl` (`CuriousContraptions.Abstractions/SignalContactControl.cs:12`) | `ComposableSupplyRoutingTests.SimulationAdvancesPowerAcrossPackageInternalEdgesToRechargeStore` (`CuriousContraptions.tests/ComposableSupplyRoutingTests.cs:530`, fixture at `:453`–`:466`) |
| N1 | Line of sight through solids | narrower: `AirflowLineClear` (`CuriousContraptions.Simulation/WorkshopPhysicsEngine.Airflow.cs:141`) serves airflow rows only and loops over every collider (`:149`) | — | `AirflowGeneralityTests.MovingSolidOcclusionIsResampledBeforeTransfer` (`CuriousContraptions.tests/AirflowGeneralityTests.cs:13`). Owner of the segment cast: **ENGINE-SPATIAL-QUERIES** |
| N2 | Presentation binding on a committed driver | narrower: no emission member (`CuriousContraptions.Abstractions/ElementPresentation.cs:18`–`:37`) | `PresentationDriver` | `BothGateTests.ThreeNativeLampsFollowCommittedInputsAndSupplyThroughHideAndReset` (`CuriousContraptions.tests/BothGateTests.cs:259`). Owner: **ENGINE-OPTICAL** |
| M1 | Declared emitters as supply consumer targets, the segment solve, receivers with weighted samples, the beam read model | **missing** | — | owner **ENGINE-OPTICAL** |
| M2 | Radiation channels, per-collider rows crossed by chord, a receiver's absorbed power per radiation convention 10 | **missing** | — | owner **ENGINE-RADIATION** |
| N3 | A supply source whose finite store has declared constant capacity, power limit, initial charge and enable, starts empty and is filled only by a credit | narrower: the delivered store binds all four to parameters and only its initial charge fills it (`SupplySourceStoreDescriptor`, `CuriousContraptions.Abstractions/ElectricalContractValues.cs:31`) | `SupplySourceDescriptor` (`CuriousContraptions.Abstractions/SupplyContractValues.cs:39`) | owner **ENGINE-THERMAL**, the one row of [TH-30](TH-30-thermoelectric-generator.md) N2 that RAD-20 also cites; it lands before ENGINE-OPTICAL (owner decision 18) |
| M3 | A credit of a declared fraction (≤ 1) of a receiver's absorbed energy into that store | **missing** | — | owner **ENGINE-OPTICAL** (the receiver-transfer row, written over channels; coordinator decision 21) |
| M4 | `ElementCategory.Radiation` and the radiation domain flag | **missing** (`CuriousContraptions.Abstractions/ElementCategory.cs:5`–`:12`) | — | owner **ENGINE-RADIATION** |
| M5 | A threshold node with a release ratio over a committed scalar (here a store's charge) and the contact state it presents to `SignalStateContactControl` | **missing** (a contact today names `SignalContactState.Latched` or `TimerCounting`, `CuriousContraptions.Abstractions/SignalContactControl.cs:5`) | — | owner **ENGINE-SIGNAL-NODES** |

**Dependencies.** A radiation source (RAD-01, RAD-02) and a Blue receiver (CAT-012), all undelivered; Battery and Powered gate (delivered) for the receiver's load.

## 4. Sources and legacy

- [radiation-12](../requirements.md#radiation-12): "Incident radiation produces observable light and a receiver response; shielding prevents both. Conversion loss is explicit and output cannot exceed absorbed energy."
- [Named entry](../invest/named-elements.md#radiation-12), owner S622; map row (S484, S605); binding `radiation-01.json`.
- [S605](../invest/decisions.md#s605) photon row; research "Conversion" law ("absorption resolves once so serial tiles cannot each claim the original energy") and slots 92 "Borrowed Light", 97.
- **Legacy.** No scintillator part or test. Hits are coverage snapshots only: older binding copy `reference/P0-022-before/docs/coverage/engine/task-017.json@a6c914e:L2009-L2039`, identical to current, and aggregate relation lists — no element knowledge; do not carry forward.

**Files harvested** (all "no element knowledge"):
- `reference/P0-022-before/docs/coverage/engine/task-017.json`
- `reference/P0-022-before/docs/coverage/engine/task-002.json`
- `reference/P0-022-before/docs/coverage/engine/task-003.json`
- `reference/P0-022-before/docs/coverage/engine/task-013.json`
- `reference/P0-022-before/docs/coverage/engine/capabilities-02.json`

## 5. Acceptance

### Behaviour and numbers

All values are starting values (D1); the efficiency 0.8 is coordinator decision 21. Each sample's share is 0.36 ÷ 9 = 0.04 m² × its irradiance. A sample's ray crosses the 0.10 m tile with chord 0.10 × √(1 + r²) for a sample r metres off axis per metre of distance.

- **Gamma capsule at 1 m, square to the back face.** Irradiance: centre 10 W/m²; edge samples (0.20 m off axis) d² = 1.04, facing 0.981, E = 10 × 0.981 ÷ 1.04 = 9.43; corner samples d² = 1.08, facing 0.962, E = 8.91. Incident 0.04 × (10 + 4 × 9.43 + 4 × 8.91) = 0.04 × 83.36 = 3.33 W. Absorbed fractions for PhotonMedium (6 /m): centre chord 0.100 m, 1 − e^−0.600 = 0.451; edge chord 0.102 m, 1 − e^−0.612 = 0.458; corner chord 0.104 m, 1 − e^−0.624 = 0.464. Absorbed 0.04 × (10 × 0.451 + 4 × 9.43 × 0.458 + 4 × 8.91 × 0.464) = 0.04 × (4.51 + 17.28 + 16.54) = 1.53 W. Credit and light 0.8 × 1.53 = 1.23 W, all Blue. The declared loss is the other 0.31 W.
- **Receiver response.** The beam is collimated, so a Blue receiver on the axis takes the whole 1.23 W. The rows set its threshold to 0.6 W (CAT-012 admits 0.5 to 20 W; its default is 2.5 W): 1.23 W is 104 % above it. The light falls to 0.6 W with the capsule at about √(1.23 ÷ 0.6) = 1.43 m. The tile cannot light a broadband detector: 1.23 W in one channel is a three-channel mean of 0.41 W, under the family's 0.5 W floor.
- **Shielding.** Two Dense slabs before the tile (PhotonMedium 20 /m × 0.05 m each, e^−2 = 0.135 head-on, less on oblique rays): incident at most 3.33 × 0.135 = 0.45 W, absorbed at most 1.53 × 0.135 = 0.207 W, light at most 0.8 × 0.207 = 0.166 W, 72 % below the 0.6 W mark. The glow and the response both go.
- **Band matters.** The X-ray emitter (10 W/sr, half-angle 15°) at 1 m lights the centre and edge samples (its cone is tan 15° = 0.268 m wide there; the corners at 0.283 m are outside): incident 0.04 × (10 + 4 × 9.43) = 1.91 W. Soft (16 /m): fractions 1 − e^−1.600 = 0.798 and 1 − e^−1.632 = 0.804; absorbed 0.04 × (10 × 0.798 + 4 × 9.43 × 0.804) = 1.53 W; light 1.23 W, a response. Hard (3 /m): fractions 1 − e^−0.300 = 0.259 and 1 − e^−0.306 = 0.264; absorbed 0.04 × (10 × 0.259 + 4 × 9.43 × 0.264) = 0.502 W; light 0.401 W, 33 % below the mark, no response.
- **Serial tiles.** A second tile behind the first receives e^−0.6 = 0.549 of the PhotonMedium flux head-on, because the first tile's box attenuates the path. Absorption resolves once: two tiles never claim the same energy.
- **Store and availability.** At every source of the family the credit stays under the 4 W demand (the largest, a Decay capsule inside 1 m, gives 0.36 × 20 = 7.2 W incident, about 3.3 W absorbed and 2.6 W of credit), so `glow` draws the whole store each tick and the store holds at most one tick's credit: 1.23 W × dt = 0.0102 J at 1 m at the default 1/120 s tick (0.0051 J at 1/240 s), above the 0.0005 J mark, so `charged` stays on. The 0.05 J capacity holds one tick of the largest credit at the slowest 60 Hz cadence (2.6 W × 1/60 s = 0.043 J), as coordinator decision 32 requires. Below a credit of 0.06 W (0.0005 J per default tick) the store fills to the mark in a few ticks and empties in one; such a pulse carries under 0.001 J, at most 0.12 W of light for one tick, below every optical threshold.
- **Energy.** Over any Run: light emitted ≤ energy credited ≤ 0.8 × energy absorbed by the receiver ≤ 0.8 × energy incident on the aperture. Radiation entering another face, and the neutron channels, are never credited.
- **Tick order.** Tick n's field solve, after the last substep on that tick's final poses, ends by crediting the store from the tile's absorbed record ([combined tick order](EL-176-red-laser-emitter.md#combined-tick-order) step 3), before tick n's thermal step; the store commits with tick n. The signal network of tick n + 1 turns `charged` on and closes `gate`; tick n + 1's substeps grant `glow` the stored energy; tick n + 1's solve emits the Blue segment from that grant, and the Blue receiver's reading commits with tick n + 1. The signal network of tick n + 2 closes that receiver's contact. Light follows radiation by one tick and a load by two. When the radiation stops at tick n, tick n still spends the credit of tick n − 1 and tick n + 1 has nothing to draw: the light stops one tick after the radiation.

### Rows

Planned class `ScintillatorTileTests`. Fixture: Gamma capsule 1 m behind the tile on its axis; a Blue receiver with threshold 0.6 W, 2 m in front, its `PowerIn` fed by a Battery and its `Supply` → Powered gate `PowerIn`. Powers ±3 % (the nearest decision margin is 33 %); stall guard 2 s.

| # | Given / When / Then | Test | Control | Boundary and tolerance |
| --- | --- | --- | --- | --- |
| T1 | Given the fixture, when Run starts, then the tile's committed absorbed power is 1.53 W, one Blue segment of 1.23 W leaves the +X face, the receiver's level turns on and the gate is supplied (row: incident radiation produces light and a receiver response) | `ScintillatorTileTests.AbsorbedRadiationBecomesABlueBeam` | T2 | ±3 %; Red and Green under 0.001 W |
| T2 | Given two Dense slabs between capsule and tile, then the emission is at most 0.166 W, the face binding is near rest and the receiver stays off for 240 ticks (row: shielding prevents both) | `ScintillatorTileTests.ShieldingRemovesTheLightAndTheResponse` | T1 | at most 0.166 W + 3 % |
| T3 | Given T1 over 240 ticks, then the light energy emitted ≤ the energy credited ≤ 0.8 × the receiver's absorbed energy ≤ 0.8 × the energy incident on the aperture (row: output cannot exceed absorbed energy; loss is explicit) | `ScintillatorTileTests.LightNeverExceedsTheCreditedShareOfAbsorbedEnergy` | T2 | each inequality with 1 % slack |
| T4 | Given a second tile 0.5 m behind the first, then its strength is 0.549 times what it reads alone at that place and the two credits sum to less than 0.8 × the first tile's incident power | `ScintillatorTileTests.SerialTilesDoNotClaimTheSameEnergy` | second tile alone | ±3 % |
| T5 | Given the capsule in front of the tile (on +X), then the receiver's strength is 0, nothing is credited and no light is emitted; given the Blue receiver 0.5 m off the beam axis, it stays off | `ScintillatorTileTests.ConversionHasADirection` | T1 | under 0.001 W |
| T6 | Given the X-ray emitter at 1 m with `band` Soft the emission is 1.23 W and the receiver responds; with Hard it is 0.401 W and the receiver does not (queued until RAD-02 exists) | `ScintillatorTileTests.TheBandSetsTheLight` (theory) | T1 | ±3 % |
| T7 | Given a lit Run, when the capsule's path is blocked and then Reset, then light stops within two ticks of the block, and after Reset the store is empty and no segment remains; a saved and reloaded construction behaves as T1 | `ScintillatorTileTests.LightStopsWithTheRadiationAndResetClears`; authored state by R3 | T1 | emission 0 |

No port and no parameter, so no connection or parameter refusal row. Chrome recipe (pending, post-catalogue): capsule, tile, Blue receiver set to 0.6 W, Powered gate; then slide two Dense shields in.

## 6. Open questions

None open.

1. **Exchange and efficiency — decided** (D1, D16; coordinator decision 21, 22 November 2026): radiation and light share watts, so there is no exchange constant; efficiency 0.8.
2. **How radiation pays for light — decided** (coordinator decision 21): the one form of radiation convention 10, a credit into a finite store drawn by an ordinary source, with availability by a threshold node over the store.
3. **Broadband or one channel — decided now**: one Blue channel, collimated. A white beam would put a third of the light in each channel, under any optical receiver's 0.5 W floor; one channel gives 1.23 W. The cyan face and the Blue receiver make the pairing readable.
4. **Tile size — decided now**: the face is 0.60 × 0.60 m, so the light at 1 m (1.23 W) is about twice the 0.6 W mark the lessons use.

Needs the owner: none.

## 7. Integration

- **Collaborators.** RAD-01, RAD-02, EL-128, the Blue receiver CAT-012 (all undelivered); Battery and Powered gate (delivered). The tile has no ports; the Blue receiver's `PowerIn` and `Supply` carry the electrical side.
- **Processes.** [IX-15](../requirements.md#interaction-15) and [IX-13](../requirements.md#interaction-13): deposition is resolved before optical emission.
- **Campaign.** Chapter 12, levels 117 and 122; reuse 121–125 and 136–150, including the radiation to light to sound capstone. No level in `content/puzzles.json` places it.
- **Coverage binding (planned, not yet live).** Blocked on the coverage follow-on story S7. Main's reader lists only `EL-*.md` specs and admits only an `EL-NNN` identity with an `element-NNN` anchor (`tools/Coverage/NamedPackageSources.cs:19`, `:57` on `origin/main`), so a `RAD-NN` identity cannot be bound until S7 widens it (owner decision 15).
- **Lesson fixtures.** Story [S8](../invest/delivery-batches.md#s8-lesson-fixtures-for-any-admitted-package) lands after the engine programme (owner decision 22). A level must place the Blue receiver with its threshold at 0.6 W, locked or taught.

## 8. Delivery

- **Slice shape.** Single element.
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), which includes ENGINE-SPATIAL-QUERIES and ENGINE-SIGNAL-NODES; ENGINE-THERMAL for the declared-constant store row only (TH-30 N2; it precedes ENGINE-OPTICAL in the roadmap, owner decision 18); ENGINE-OPTICAL, including its receiver-transfer row; ENGINE-RADIATION as amended; RAD-01; the Blue receiver CAT-012; S7 for the binding. No heat law is used.
- **Files.** `elements/ScintillatorTile/ScintillatorTile.csproj`, `ScintillatorTileElementDeclaration.cs`, `scintillator_tile.tscn`, `icon.svg`; one guarded line in `CuriousContraptions.slnx`; `CuriousContraptions.tests/ScintillatorTileTests.cs`; a row in `VisibleSolidColliderTests`. Registration is derived (`spec-registration-derivation.md`).
- **Review tier.** Data-only if the probe finds every row present after the engine stories; otherwise the engine tier.
- **Disqualifiers (the slice's probe re-checks).** The optical receivers' admissible threshold floor rising above 0.6 W; a changed radiation scale or Scintillator row (re-derive section 5); no credit of absorbed energy into a store with declared values; an optical receiver that needs a diverging source; a threshold node that `SignalStateContactControl` cannot name. In each case the slice stops and the owning story is amended; no second design is kept here.

## 9. Engine conformance

Verdict: **Conforms** (every missing capability has exactly one owning element-free story).

1. **Declaration data only.** One static body, two colliders, one receiver, one credited store with its source, one threshold node, one contact, one consumer, one emitter, three bindings. No code names the tile.
2. **Generic laws.** Chord attenuation; the receiver's absorbed power; the one credit of absorbed energy into a finite store; ordinary supply routing; the optical segment law. "Radiation becomes light" is a store credited by absorption and drained by a consumer whose target is an emitter.
3. **Three layers.** Colliders: two boxes. Visuals: the art table; face, ring and arrow are flush or printed. Animation: the face's emission binding; the beam is drawn from committed segments.
4. **One-way flow and typing.** Field solve → absorbed power → store credit → next tick's supply grant → emitter → segments → readings; nothing flows back. Channels are enums; powers and energies are f32.
5. **Flat state.** The counts in section 2 are fixed at compile time.
6. **Performance fitness.** Static body; two primitives; nine shadow casts per diverging source in range and one segment cast for the beam, each through the spatial query's segment cast.
7. **Tests are scenarios.** Rows T1 to T7 run the production simulation through placement, wiring of the receiver's load, Run and committed reads.
