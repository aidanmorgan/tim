# RAD-09 · Integrating dosimeter — element readiness spec

**Current delivery (24 October 2026):** [RAD-09 delivery slice](../invest/named-elements.md#radiation-09). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Story 7.0 Batch O named-identity spec, brought to build-ready detail on 22 November 2026 against checkout `spec-drafts` (published main `8d1d68aa`) and repaired the same day after the independent audit (`docs/planning/elements/readiness/audits/elements-batch-o.md`) and owner decisions 13 and 17 and coordinator decisions 16 and 21. Legacy citations are pinned to `a6c914e`; citations without `@` are `path:line` in the checkout. Requirement row: [radiation-09](../requirements.md#radiation-09) (P1 potential). Doses are joules of fictional toy radiation; never real-world dosimetry. Units, the reading law, the accumulator, the one dose scale and what "exposed" means are in the [radiation conventions](RAD-08-radiation-rate-meter.md#radiation-conventions).

## 1. Identity

| Item | Value |
| --- | --- |
| ID / name | RAD-09 · Integrating dosimeter |
| Type | Radiation (accumulating receiver with a supplied contact) |
| Anchor | [requirements.md#radiation-09](../requirements.md#radiation-09); [named-elements.md#radiation-09](../invest/named-elements.md#radiation-09) |
| Owner | S616 |
| Disposition | To deliver as a single element, after [RAD-08](RAD-08-radiation-rate-meter.md), whose body and aperture it shares. It is not a family member of the meter: it adds an accumulator, a second parameter and a level on the accumulator, and has no pulse output. It refines no CAT part; it must stay distinct from the Hold timer ([CAT-033](CAT-033-hold_timer.md)), which counts time. |
| Planned package | id `integrating_dosimeter`, directory `elements/IntegratingDosimeter/`, class `IntegratingDosimeterElementDeclaration` |
| Related identities | Burst source [RAD-02](RAD-02-powered-x-ray-emitter.md); passive cargo counterpart [RAD-10](RAD-10-exposure-sensitive-cargo-badge.md), on the same dose scale; feedback partner [RAD-06](RAD-06-powered-radiation-shutter.md); bursts timed by the Clock. |
| Status | not started; no package; blocked on the engine programme (owner decision 3a, 22 November 2026), ENGINE-OPTICAL and ENGINE-RADIATION |
| Run length needed | 30 s (the default); the longest row is D4 at about 12 s |

## 2. Declaration

Shape followed: [RAD-08](RAD-08-radiation-rate-meter.md#2-declaration) for the body, aperture, threshold node and supplied contact; the Battery for a store whose initial value is a parameter (`elements/Battery/BatteryElementDeclaration.cs:42`–`:47`, `:71`). Items marked **(field)** use contracts that do not exist yet.

| Item | Value |
| --- | --- |
| Interfaces | `ISupplyElement` (`CuriousContraptions.Abstractions.Electrical/ElectricalContracts.cs:11`), `ISignalElement` (`CuriousContraptions.Abstractions.Signal/SignalContracts.cs:8`), `ISceneGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`), plus the receiver and accumulator interfaces **(field)** |
| Body and colliders | RAD-08's static body: case box 0.30 × 0.60 × 0.50 m, base box 0.40 × 0.06 × 0.60 m at (0, −0.33, 0), two socket spheres r 0.075 m. Four colliders; none declares a radiation row, so all take the scene's default row. |
| Ports | `PortId.PowerIn`, Electrical, Input, role `PowerIn`, at (−0.15, 0, −0.15), normal −X. `PortId.Supply`, Electrical, Output, role `Supply`, at (−0.15, 0, 0.15), normal −X. |
| Receiver **(field)** | Frame body 0; aperture square 0.30 × 0.30 m on +X, centre (0.15, 0, 0), normal +X, the standard area 0.09 m² (radiation convention 9); nine samples of weight 1/9; Lambert facing. Channel weights: PhotonSoft, PhotonMedium, PhotonHard = 1; BetaMinus = 0.5; Alpha, NeutronFast, NeutronSlow = 0. No rate threshold: the receiver feeds the accumulator only. |
| Accumulator **(field)** | `dose`, joules: + the receiver's committed strength × dt each tick (dt the committed tick's duration, 1/120 s at the default cadence; radiation convention 9), never reduced within a Run; capacity 100 J (it clamps there); initial value = parameter `initial_dose`. It commits one scalar with the tick (radiation convention 9). |
| Signal graph | One threshold node `reached` over the committed `dose`: on at `dose` ≥ parameter `threshold`, off below 0.75 × `threshold` **(field, ENGINE-SIGNAL-NODES)**. The release never acts within a Run, because the dose never falls; it is declared because every threshold node declares one. No signal port. |
| Supply graph | One `SupplyContactDescriptor` `relay` with `SignalStateContactControl` naming `reached`; `PowerIn` → `relay` → `Supply`. One port binds one endpoint. No consumer: the accumulator is not supplied and integrates whether or not `PowerIn` is live. |
| Parameters | `threshold`: Real, Joule, default 1.4, minimum 0.1, maximum 100, editor policy Clamp, build only. `initial_dose`: Real, Joule, default 0, minimum 0, maximum 100, editor policy Clamp, build only. Both editable unless the level locks them (D11). |
| Presentation bindings | `Visual/Bar` scale Y from 0.001 to 1 from the committed `dose` ÷ `threshold`, clamped **(field)**; `Visual/Lamp` albedo slate (85, 101, 115) → gold (247, 203, 82) from the committed level of `reached`, `PresentationCurve.SmoothStep` over 0.1 s **(field)**. The lamp cannot use the delivered socket driver: `CommittedElectricalSocket` is refused on a port bound to a contact (`CuriousContraptions.Bundle/SupplyBundleValidation.cs:195`). |
| Footprint | `ElementFootprint(Bodies: 1, MovingBodies: 0, PrismaticConstraints: 0, RevoluteConstraints: 0)`; domains Electrical, Signal and the radiation flag **(field)** |
| Catalogue | id `integrating_dosimeter`, "Dosimeter", `ElementCategory.Radiation` **(field)**, palette order 709 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)). Description: "Adds up the radiation that has fallen on its face. Its contact passes electricity once the total reaches the mark. It remembers through darkness and power cuts." |

### Art and solidity

| Art node | Mesh and size | Class | Moved by |
| --- | --- | --- | --- |
| `Visual/Case`, `Visual/Base` | RAD-08's boxes, cream `#fff8e9` and navy `#293954` | solid (case and base boxes) | nothing |
| `Visual/Face` | square 0.30 m printed flush on the +X face | decorative: printed flat | nothing |
| `Visual/Window` | upright rectangle 0.08 × 0.30 m printed flush on the −X face with a printed line at the mark and a printed Σ | decorative: printed flat | nothing |
| `Visual/Bar` | gold `#f7cb52` rectangle in the window's plane, anchored at its foot | decorative: moved by a binding | presentation binding |
| `Visual/Lamp` | disc r 0.03 m flush under the window | decorative: flush, recoloured by a binding | nothing (colour only) |
| `Visual/PowerStud`, `Visual/SupplyStud` | spheres r 0.075 m, gold | solid (socket spheres) | nothing |

Icon: the case with an upright bar and a Σ. It differs from the rate meter by the upright bar in place of a round dial and needle.

### Compiled counts

Bodies 1 (static). Colliders 4 (2 boxes, 2 spheres). Signal nodes 1 (threshold). Supply nodes 1. Receivers 1 (nine samples). Stores 1 (accumulator). Presentation bindings 4 (bar, lamp R, G, B). Joints, drives, sensors 0.

## 3. Engine capabilities

Map row families (navigation, D14): FiniteLedger, GeometryQuery, IonizingTransport, SignalPropagation. Probe made on `8d1d68aa`: **5 present, 0 untested, 4 narrower, 5 missing**. ENGINE-SPATIAL-QUERIES, ENGINE-SIGNAL-NODES and ENGINE-PARAMETER-BOUND-FRAMES are stories of owner decision 3a.

| # | Capability | Status | Symbol | Test opened |
| --- | --- | --- | --- | --- |
| R1, R2, R3, R5, R6 | Static body from `col_` nodes; every visible solid has a collider; typed ports and wrong-kind refusal (`PortDescriptor`, `CuriousContraptions.Abstractions/PortDescriptor.cs:55`); supply contact closed by a signal state between a source and a consumer; Run, Reset, save and load | present | as [RAD-08 section 3](RAD-08-radiation-rate-meter.md#3-engine-capabilities) | as RAD-08: `StateLatchTests.cs:503`, `VisibleSolidColliderTests.cs:206`, `BundlePortsTests.cs:262`, `ComposableSupplyRoutingTests.cs:530`, `CatalogueIntegrationTests.cs:273` |
| N1 | Line of sight through solids | narrower, as RAD-08 N1 | `AirflowLineClear` | owner of the segment cast **ENGINE-SPATIAL-QUERIES** |
| N3 | A lamp driven by a threshold node's committed level | narrower; the delivered socket driver is refused on a port bound to a contact (`CuriousContraptions.Bundle/SupplyBundleValidation.cs:195`) | `PresentationDriver` | `BothGateTests.ThreeNativeLampsFollowCommittedInputsAndSupplyThroughHideAndReset` (`CuriousContraptions.tests/BothGateTests.cs:259`, socket lamps on logic-node ports). Owner **ENGINE-SIGNAL-NODES** (the committed level a threshold node presents to a presentation binding; one owner with RAD-08 M3, RAD-14 and RAD-15) |
| N4 | A bar driven by an accumulator's committed value ÷ a declared mark | narrower: `PresentationDriver.CommittedStoredFraction` (`CuriousContraptions.Abstractions/ElementPresentation.cs:33`) reads only a mechanical energy store's fraction of capacity (the store must be one of the package's `MechanicalPayload.EnergyStores`, `CuriousContraptions.Bundle/BundleValidator.cs:310`–`:312`), not an accumulator's fraction of a mark | `PresentationDriver` | `OwnedFiniteWorkTransactionTests.StoredFractionAndAvailabilityUsePreloadAndSameTimeCommittedState` (`CuriousContraptions.tests/OwnedFiniteWorkTransactionTests.cs:403`). Owner **ENGINE-RADIATION** |
| N5 | An accumulator's initial value bound to a parameter (the mark of `reached` is bound to `threshold` as RAD-08's `detect` is, inside M5) | narrower: a parameter binds store values only on the supply source's store (`SupplySourceStoreDescriptor`, `CuriousContraptions.Abstractions/ElectricalContractValues.cs:31`; the Battery binds `initial_charge`, `elements/Battery/BatteryElementDeclaration.cs:46`, `:71`) | `SupplySourceStoreDescriptor` | `ComposableSupplyRoutingTests.SimulationAdvancesPowerAcrossPackageInternalEdgesToRechargeStore` (`CuriousContraptions.tests/ComposableSupplyRoutingTests.cs:530`, store fields bound to parameters at `:457`). Owner **ENGINE-PARAMETER-BOUND-FRAMES** (parameter-bound values, coordinator decision 16) |
| M1 | Declared receivers with weighted samples and a committed reading and strength | **missing** | — | owner **ENGINE-OPTICAL** |
| M2 | Radiation channels, attenuation rows and chords | **missing** | — | owner **ENGINE-RADIATION** |
| M3 | An accumulator: a declared store credited by a receiver's committed strength each tick, with a capacity and an authored initial value, committed as a scalar; restored on Reset; authored values only in saves | **missing** (the delivered store is a supply source, `CuriousContraptions.Abstractions/SupplyContractValues.cs:39`, debited by loads, never credited by a reading) | — | owner **ENGINE-RADIATION** |
| M4 | `ElementCategory.Radiation` and the radiation domain flag | **missing** | — | owner **ENGINE-RADIATION** |
| M5 | A threshold node with a release ratio and a parameter-bound mark over a committed scalar, and the contact state it presents to `SignalStateContactControl` (RAD-08 M3) | **missing** (a contact today names `SignalContactState.Latched` or `TimerCounting`, `CuriousContraptions.Abstractions/SignalContactControl.cs:5`) | — | owner **ENGINE-SIGNAL-NODES** |

**Dependencies.** Battery and Powered gate (delivered); RAD-08 (shape); RAD-01 (steady source); RAD-02 for bursts, or a law-scene source with an enable until it is delivered.

## 4. Sources and legacy

- [radiation-09](../requirements.md#radiation-09): "Two separated exposures add to the same total as one equivalent exposure; darkness does not erase the total. Run/Reset clears it to the authored initial value."
- [Named entry](../invest/named-elements.md#radiation-09), owner S616; map notes "Source D: accumulated dose across bursts and retained value on supply loss, not residence reset" and "residence reset is forbidden"; binding `radiation-01.json`.
- [S605](../invest/decisions.md#s605) photon row; research row (bar ← accumulated D; slots 84 "A Little at a Time", 85 "Close at the Mark").
- **Legacy.** No dosimeter part, test or level. Hits are coverage snapshots only: older binding copy `reference/P0-022-before/docs/coverage/engine/task-017.json@a6c914e:L1900-L1930`, identical to current, and aggregate relation lists — no element knowledge; do not carry forward.

**Files harvested** (all "no element knowledge"):
- `reference/P0-022-before/docs/coverage/engine/task-017.json`
- `reference/P0-022-before/docs/coverage/engine/task-002.json`
- `reference/P0-022-before/docs/coverage/engine/task-003.json`
- `reference/P0-022-before/docs/coverage/engine/task-013.json`
- `reference/P0-022-before/docs/coverage/engine/capabilities-02.json`

## 5. Acceptance

### Behaviour and numbers

Starting values (D1) under the [radiation conventions](RAD-08-radiation-rate-meter.md#radiation-conventions). Reference source: a 10 W/sr PhotonMedium source 1 m from the face on its axis. On the standard 0.09 m² nine-sample Lambert face (one dose scale, convention 9): centre sample 10 W/m², four edge samples 10 × 0.995 ÷ 1.01 = 9.85 W/m², four corner samples 10 × 0.990 ÷ 1.02 = 9.71 W/m²; mean (10 + 4 × 9.85 + 4 × 9.71) ÷ 9 = 9.80 W/m²; strength 0.09 × 9.80 = 0.882 W, so the dose rate is 0.882 J/s.

- **Accumulation.** Each tick adds strength × dt joules: 0.882 × 1/120 = 0.00735 J at the default tick (0.0147 J at 1/60 s, 0.00368 J at 1/240 s). One second of exposure adds 0.882 J at every cadence.
- **Two bursts equal one.** Two 1 s bursts separated by any dark time: 0.882 + 0.882 = 1.764 J. One 2 s burst: 1.764 J. The same 240 lit ticks either way.
- **The default mark.** `threshold` 1.4 J lies between one burst (0.882 J, 37 % below the mark: 1 − 0.882 ÷ 1.4 = 0.37) and two (1.764 J, 26 % above it: 1.764 ÷ 1.4 = 1.26). It is reached after 1.4 ÷ 0.882 = 1.59 s of exposure, 0.59 s into the second burst.
- **Darkness.** With strength 0 a tick adds 0. Ten dark seconds change nothing. This is the difference from a Hold timer: the dosimeter never counts time, only received energy.
- **Power loss.** The accumulator has no supply input. Unsupplied, `Supply` is dead and the dose keeps its value and keeps rising if radiation falls on it. When supply returns, `Supply` is live at once if `reached` is on.
- **Weighting.** A Beta-minus cartridge's 1 W beam on the face has strength 0.5 × 1 = 0.5 W: the mark takes 1.4 ÷ 0.5 = 2.8 s. An Alpha beam and neutron flux add nothing.
- **Shielding.** Behind two Dense slabs (PhotonMedium 20 /m × 0.05 m each) the strength is 0.882 × e^−2 = 0.882 × 0.135 = 0.119 W: the mark takes 1.4 ÷ 0.119 = 11.7 s.
- **Initial value.** With `initial_dose` 1.0 J the mark is reached after (1.4 − 1.0) ÷ 0.882 = 0.45 s. With `initial_dose` at or above `threshold`, `reached` is on from the first tick.
- **Capacity.** The dose clamps at 100 J; nothing wraps. From 0 the reference source would need 100 ÷ 0.882 = 113 s, longer than the default Run, so row D12 starts from `initial_dose` 95 J: (100 − 95) ÷ 0.882 = 5.67 s to the clamp.
- **Tick order.** The field solve of tick n runs after the last substep on that tick's final poses, credits the accumulator and commits it with the tick; the signal network of tick n + 1 turns `reached` on and closes `relay` (radiation convention 11).
- **Energy.** The dose is a record of received energy; it is not a supply store and nothing can draw from it. Received never exceeds emitted.

### Rows

Planned class `IntegratingDosimeterTests`. Fixture: Battery `Supply` → dosimeter `PowerIn`; dosimeter `Supply` → Powered gate `PowerIn`; a law-scene 10 W/sr PhotonMedium source with an enable, 1 m from the face (the X-ray emitter with Set and Reset pulses once RAD-02 is delivered). Doses ±3 % plus one tick's credit (0.0074 J); the nearest decision margin is 26 %. Rows that wait for a level change or the clamp stop after three times the hand-calculated time, at least 2 s; timed rows run their stated duration.

| # | Given / When / Then | Test | Control | Boundary and tolerance |
| --- | --- | --- | --- | --- |
| D1 | Given two 1 s bursts 3 s apart, the dose is 1.764 J; given one 2 s burst, the dose is 1.764 J; the two totals agree (row: two separated exposures add to the same total as one equivalent exposure) | `IntegratingDosimeterTests.SeparatedBurstsAddToTheSameTotalAsOneExposure` | D2 | each ±3 %; difference within one tick's credit |
| D2 | Given one 1 s burst, the dose is 0.882 J, `reached` is off and the gate is unsupplied for the 5 s that follow | `IntegratingDosimeterTests.OneBurstIsBelowTheMark` | D1 | ±3 % |
| D3 | Given D1, `reached` turns on during the second burst, 1.59 s of exposure in, and `Supply` is live from the next tick and stays live after the source is off | `IntegratingDosimeterTests.TheContactClosesAtTheMarkAndStaysClosed` | D2 | exposure time ±5 % |
| D4 | Given one burst then 10 s of darkness, the dose after the darkness equals the dose before it (row: darkness does not erase the total; not a timer) | `IntegratingDosimeterTests.DarknessNeitherErasesNorAdds` | D1 | equal within one tick's credit |
| D5 | Given the Battery disabled by its run control (`enabled`, `ParameterRunEdit.RunControl`, `elements/Battery/BatteryElementDeclaration.cs:48`–`:49`) during the second burst and enabled after it, `Supply` is dead while unsupplied, the dose still reaches 1.764 J, and `Supply` is live as soon as supply returns (row: power loss disables the contact but retains the measurement) | `IntegratingDosimeterTests.PowerLossOpensTheContactAndKeepsTheTotal` | D3 | ±3 % |
| D6 | Given a Run that reached the mark, when Reset, the dose is `initial_dose`, `reached` is off and the bar and lamp are at rest; with `initial_dose` 1.0 J the next Run reaches the mark after 0.45 s of exposure (row: Reset clears it to the authored initial value) | `IntegratingDosimeterTests.ResetRestoresTheAuthoredInitialDose` | D1 | value exact after Reset; time ±5 % |
| D7 | Given a Beta-minus beam of 1 W on the face, the dose after 2 s is 0.5 × 2 = 1.0 J; an Alpha beam or a fast-neutron source adds nothing | `IntegratingDosimeterTests.ChannelsAreWeighted` (theory; the beta and alpha cases use law-scene collimated sources until RAD-03 and RAD-04) | D1 | ±3 %; below 0.001 J |
| D8 | Given two Dense slabs on the path, the dose after 2 s is 0.119 × 2 = 0.238 J and `reached` is off | `IntegratingDosimeterTests.ShieldingSlowsAccumulation` | D1 | ±3 % |
| D9 | Given `threshold` 0.1 and 100 and `initial_dose` 0 and 100 they are admitted; outside and non-finite values are refused with nothing changed | `IntegratingDosimeterTests.ParametersAreBounded` | — | typed refusal |
| D10 | Given a saved and reloaded construction, `threshold` and `initial_dose` are kept and no Run-time dose is saved | `IntegratingDosimeterTests.SavesHoldAuthoredValuesOnly`; authored state by `CatalogueIntegrationTests.EveryAdmittedDeclarationRetainsAuthoredStateAcrossRunResetSaveLoad` (`CuriousContraptions.tests/CatalogueIntegrationTests.cs:273`) | — | exact values |
| D11 | Given a signal output wired to `PowerIn`, the edit is refused | generic: `BundlePortsTests.AWrongDomainOrOutputToOutputLinkIsStillRefused` (`CuriousContraptions.tests/BundlePortsTests.cs:262`) | — | typed refusal |
| D12 | Given `initial_dose` 95 J and the source on for 10 s, the dose reaches 100 J after 5.67 s and stays there (95 + 8.82 = 103.8 J unclamped) | `IntegratingDosimeterTests.TheDoseClampsAtCapacity` | the same Run with `initial_dose` 0 reads 8.82 J | ±1 J; never above 100 J |

Chrome recipe (pending, post-catalogue): Battery, Clock, X-ray emitter 1 m from the dosimeter, Powered gate; two clocked bursts.

## 6. Open questions

None open.

1. **Integrate while unpowered, or only retain — decided now**: it integrates. A receiver has no supply input in the conventions, so one receiver law serves every part; the row asks only that the contact is disabled and the total kept, and both hold.
2. **Weights and default mark — decided** (D1, D16; coordinator decision 21 for the dose scale): photons 1, beta-minus 0.5, others 0; the standard 0.09 m² area; mark 1.4 J, chosen so one burst and two bursts each sit at least 20 % from it.
3. **Authored initial value — decided** (D11): a parameter, `initial_dose`, editable unless the level locks it, as the Battery's initial charge is.
4. **How bursts are made — resolved**: RAD-02 takes Set and Reset pulses ([RAD-02](RAD-02-powered-x-ray-emitter.md)); a Clock or Delay box supplies them.
5. **Coupling — decided** (coordinator decision 16): accumulator → threshold node → signal-state contact. No memory node is set by an accumulator.

Needs the owner: none.

## 7. Integration

- **Collaborators.** Battery, Powered gate, Clock, Delay box (delivered); RAD-01, RAD-02, RAD-06, EL-128 (undelivered). Port domains agree: `PowerIn` Electrical Input, `Supply` Electrical Output. Feedback with the shutter: the dosimeter's `Supply` powers nothing in an open-when-supplied shutter until the mark is reached; the lesson wiring is decided with RAD-06.
- **Cross-element tasks.** None names the dosimeter. Processes [IX-15](../requirements.md#interaction-15), [IX-06](../requirements.md#interaction-06), [IX-07](../requirements.md#interaction-07).
- **Campaign.** Chapter 11, first use 101–110 (lessons 109, 110); reuse 111–120, 124–130, 136–150. No level in `content/puzzles.json` places it.
- **Coverage binding (planned, not yet live).** Blocked on the coverage follow-on story S7. Main's reader lists only `EL-*.md` specs and admits only an `EL-NNN` identity with an `element-NNN` anchor (`tools/Coverage/NamedPackageSources.cs:19`, `:57` on `origin/main`), so a `RAD-NN` identity cannot be bound until S7 widens it (owner decision 15).
- **Lesson fixtures.** Story [S8](../invest/delivery-batches.md#s8-lesson-fixtures-for-any-admitted-package) lands after the engine programme (owner decision 22).

## 8. Delivery

- **Slice shape.** Single element.
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), which includes ENGINE-SPATIAL-QUERIES, ENGINE-SIGNAL-NODES and ENGINE-PARAMETER-BOUND-FRAMES; ENGINE-OPTICAL; ENGINE-RADIATION as amended, including the accumulator; RAD-01 and RAD-08; S7 for the binding. No dependency on ENGINE-THERMAL ([ENGINE-RADIATION](../../../_bmad-output/planning-artifacts/epics.md#story-engine-radiation-emission-attenuation-decay-and-exposure)'s *Depends on* line names no thermal story). No ENGINE-RUN-LENGTH: every row fits the default 30 s.
- **Files.** `elements/IntegratingDosimeter/IntegratingDosimeter.csproj`, `IntegratingDosimeterElementDeclaration.cs`, `integrating_dosimeter.tscn`, `icon.svg`; one guarded line in `CuriousContraptions.slnx`; `CuriousContraptions.tests/IntegratingDosimeterTests.cs`; a row in `VisibleSolidColliderTests`. Registration is derived (`spec-registration-derivation.md`).
- **Review tier.** Data-only if the probe finds every row present after the engine stories; otherwise the engine tier.
- **Disqualifiers (the slice's probe re-checks).** An accumulator whose initial value, or a threshold node whose mark, cannot be bound to a parameter; an accumulator gated by supply; no accumulator-over-mark driver for the bar; a threshold node that `SignalStateContactControl` cannot name; a different dose scale (re-derive section 5). In each case the slice stops and the owning story is amended; no second design is kept here.

## 9. Engine conformance

Verdict: **Conforms** (every missing capability has exactly one owning element-free story).

1. **Declaration data only.** A static body, four colliders, two ports, one receiver, one accumulator, one threshold node, one contact, two parameters and four bindings. No code path names the dosimeter.
2. **Generic laws.** The field law; an accumulator credited by a committed strength; a threshold node over a committed scalar; supply through a signal-state contact. "Remembers through darkness" is a store that is only ever credited.
3. **Three layers.** Colliders: four primitives on one static body. Visuals: flush and printed art. Animation: bar and lamp read committed state and write nothing back.
4. **One-way flow and typing.** Field solve → strength → accumulator → threshold node → next tick's supply. Channels, ports and units are enums; doses are f32.
5. **Flat state.** Counts in section 2, fixed at compile time; the accumulator is one row of a store table.
6. **Performance fitness.** Static body; four primitives; nine shadow casts per diverging source in range, each through the spatial query's segment cast; one add per tick; no sensor volume.
7. **Tests are scenarios.** Rows D1 to D12 run the production simulation through placement, wiring, Run and committed reads.
