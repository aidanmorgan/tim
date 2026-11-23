# RAD-08 · Radiation rate meter — element readiness spec

**Current delivery (24 October 2026):** [RAD-08 delivery slice](../invest/named-elements.md#radiation-08). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Story 7.0 Batch O named-identity spec, brought to build-ready detail on 22 November 2026 against checkout `spec-drafts` (published main `8d1d68aa`) and repaired the same day after the independent audit (`docs/planning/elements/readiness/audits/elements-batch-o.md`) and owner decisions 12, 13 and 17 and coordinator decisions 16 and 21. Legacy citations are pinned to `a6c914e` and resolve with `git show a6c914e:<path> | sed -n 'start,endp'`; citations without `@` are `path:line` in the checkout. Requirement row: [radiation-08](../requirements.md#radiation-08) (P1 potential). Lengths are metres; local +X is the sensing direction. The instrument reads fictional toy sources; nothing here is real-world dosimetry guidance.

**This spec owns the radiation conventions of the whole catalogue**: channels and units, sources, the propagation and attenuation law, receivers, thresholds, dose, what "exposed" means, tick order and the work bound. They are stated once, in [section 10](#radiation-conventions). Every other radiation spec cites that section and states only its own declared values.

## 1. Identity

| Item | Value |
| --- | --- |
| ID / name | RAD-08 · Radiation rate meter |
| Type | Radiation (receiver with a supplied contact) |
| Anchor | [requirements.md#radiation-08](../requirements.md#radiation-08); [named-elements.md#radiation-08](../invest/named-elements.md#radiation-08) |
| Owner | S615 |
| Disposition | To deliver as a single element. It is the first radiation receiver and the pattern for the detector family ([EL-131](EL-131-fast-neutron-detector.md), [EL-132](EL-132-slow-neutron-detector.md), [EL-129](EL-129-sheet-thickness-transmission-gauge.md), [EL-130](EL-130-tank-level-transmission-gauge.md), [RAD-09](RAD-09-integrating-dosimeter.md)). It refines no CAT part and duplicates none. |
| Planned package | id `radiation_rate_meter`, directory `elements/RadiationRateMeter/`, class `RadiationRateMeterElementDeclaration` |
| Related identities | Sources [RAD-01](RAD-01-gamma-source-capsule.md), [RAD-02](RAD-02-powered-x-ray-emitter.md), [RAD-03](RAD-03-alpha-source-cartridge.md), [RAD-04](RAD-04-beta-minus-source-cartridge.md), [RAD-13](RAD-13-decay-clock-capsule.md), [RAD-22](RAD-22-sealed-tracer-capsule.md). Supply from the Battery; typical load the Powered gate. |
| Status | not started; no package; blocked on the engine programme (owner decision 3a, 22 November 2026), ENGINE-OPTICAL and ENGINE-RADIATION |
| Run length needed | 30 s (the default); no row is longer than 5 s |

## 2. Declaration

Shape followed: the Ball detector for a static sensing part with a pulse output (`elements/BallDetector/BallDetectorElementDeclaration.cs:36`–`:38`, `:72`–`:74`) and the State latch for a supplied contact closed by a signal state (`elements/StateLatch/StateLatchElementDeclaration.cs:67`). Items marked **(field)** use contracts that do not exist yet; the slice's capability probe restates them.

| Item | Value |
| --- | --- |
| Interfaces | `ISupplyElement` (`CuriousContraptions.Abstractions.Electrical/ElectricalContracts.cs:11`), `ISignalElement` (`CuriousContraptions.Abstractions.Signal/SignalContracts.cs:8`), `ISceneGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`), plus the receiver interface **(field)** |
| Body | One static scene body; restitution 1, friction 0.3, rolling resistance 0, bounce threshold 0.1 m/s (the static default, for example `elements/Battery/BatteryElementDeclaration.cs:61`–`:64`) |
| Colliders (hidden `col_` proxies) | Case box 0.30 × 0.60 × 0.50 m at the origin. Base box 0.40 × 0.06 × 0.60 m at (0, −0.33, 0), touching the case. Three socket spheres radius 0.075 m at the three ports. Five colliders; none declares a radiation row, so all take the scene's default row. |
| Ports | `PortId.PowerIn`, Electrical, Input, role `PowerIn`, at (−0.15, 0, −0.15), normal −X. `PortId.Supply`, Electrical, Output, role `Supply`, at (−0.15, 0, 0.15), normal −X. `PortId.ActivationOut`, Signal, Output, role `ActivationOut`, `PortSignalKind.Pulse`, at (0, 0.30, 0), normal +Y. No radiation port: radiation crosses space. |
| Receiver **(field)** | Frame body 0; aperture square 0.30 × 0.30 m on the +X face, centre (0.15, 0, 0), normal +X, area 0.09 m² (the standard area); nine sample points at offsets 0 and ±0.10 m, weight 1/9 each; Lambert facing; WeightedSum strength. The channel weights are values bound to the options of `sensitivity`: Photon (PhotonSoft, PhotonMedium, PhotonHard = 1), Alpha (Alpha = 1), Beta-minus (BetaMinus = 1); every other weight 0. It commits one strength in watts. |
| Signal graph | One threshold node `detect` over the receiver's committed strength: on at strength ≥ `threshold`, off below 0.75 × `threshold` **(field, ENGINE-SIGNAL-NODES)**. `ActivationOut` is the pulse on each rise of `detect` (the same story's edge row). |
| Supply graph | One `SupplyContactDescriptor` `relay` with `SignalStateContactControl` naming `detect`; `PowerIn` → `relay` → `Supply`. One port binds one endpoint. The meter creates no electricity and has no consumer. |
| Electrical nodes | `relay`: `ElectricalRole.Switch`, 12 V, resistance 0.05 Ω, capacitance 0, maximum 100 W, as the Impact switch's contact (`elements/ImpactSwitch/ImpactSwitchElementDeclaration.cs:76`). A part with supply ports must declare at least one electrical node, or the bundle refuses it (`CuriousContraptions.Bundle/BundleValidator.cs:867`) |
| Parameters | `sensitivity`: Choice, unit None, options Photon, Alpha, Beta-minus, default Photon, build only. `threshold`: Real, Watt, default 0.2, minimum 0.02, maximum 2.0, editor policy Clamp, build only. Both player-editable unless the level locks them (D11). |
| Stores | None. A rate has no memory; accumulated dose is [RAD-09](RAD-09-integrating-dosimeter.md). |
| Presentation bindings | `Visual/Needle` rotation −60° to +60° from the committed strength on a clamped scale 0 to 1 W **(field)**; `Visual/Lamp` albedo slate (85, 101, 115) → gold (247, 203, 82) from the committed level of `detect`, `PresentationCurve.SmoothStep` over 0.1 s; one click cue per `ActivationOut` pulse (an `Event` binding). |
| Footprint | `ElementFootprint(Bodies: 1, MovingBodies: 0, PrismaticConstraints: 0, RevoluteConstraints: 0)`; domains Electrical, Signal and the radiation flag **(field)** |
| Catalogue | Id `radiation_rate_meter`, "Rate meter", `ElementCategory.Radiation` **(field)**, palette order 702 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)). Description: "Reads radiation falling on its face now. Its contact passes electricity while the reading is at or above the mark." |

### Art and solidity

| Art node | Mesh and size | Class | Moved by |
| --- | --- | --- | --- |
| `Visual/Case` | box 0.30 × 0.60 × 0.50 m, cream `#fff8e9` | solid (case box) | nothing |
| `Visual/Base` | box 0.40 × 0.06 × 0.60 m at y −0.33, navy `#293954` | solid (base box) | nothing |
| `Visual/Face` | square 0.30 m printed flush on the +X face, navy outline | decorative: printed flat | nothing |
| `Visual/Dial` | disc r 0.12 m printed flush on the top half of the −X face, with two printed marks at the on and off values | decorative: printed flat | nothing |
| `Visual/Needle` | gold `#f7cb52` line printed in the dial plane | decorative: moved by a binding | presentation binding |
| `Visual/Lamp` | disc r 0.03 m flush under the dial | decorative: flush, recoloured by a binding | nothing (colour only) |
| `Visual/ChannelMark` | γ, α or β printed flat beside the dial, chosen by `sensitivity` | decorative: printed flat | nothing |
| `Visual/PowerStud`, `Visual/SupplyStud`, `Visual/TriggerStud` | spheres r 0.075 m, gold | solid (socket spheres) | nothing |

Icon: the case outline with a needle and a square face. It differs from the dosimeter by the round dial and needle (the dosimeter has an upright bar) and from the neutron detectors by the Greek channel letter (they print "n").

### Compiled counts

Bodies 1 (static). Colliders 5 (2 boxes, 3 spheres), the Ball detector's order of size. Joints 0. Drives 0. Sensors 0. Signal nodes 1 (threshold). Supply nodes 1. Electrical nodes 1. Receivers 1 (nine samples). Stores 0. Presentation bindings 5 (needle, lamp R, G, B, click).

## 3. Engine capabilities

Map row families (navigation, D14): FiniteLedger, GeometryQuery, IonizingTransport, SignalPropagation. This probe, made on `8d1d68aa`, is the authority until the slice re-runs it. 5 present, 0 untested, 3 narrower, 4 missing. ENGINE-SPATIAL-QUERIES, ENGINE-SIGNAL-NODES and ENGINE-PARAMETER-BOUND-FRAMES are stories of owner decision 3a.

| # | Capability | Status | Symbol | Test opened |
| --- | --- | --- | --- | --- |
| R1 | Static scene body with colliders from `col_` nodes | present | `ISceneGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`) | `StateLatchTests.Compiler_DeclaresStaticLatchBodyWithCanonicalColliders` (`CuriousContraptions.tests/StateLatchTests.cs:503`) |
| R2 | Every visible solid has a collider | present | per-package row of the test's `Specs` | `VisibleSolidColliderTests.EverySolidArtPartHasItsColliderAndNoColliderReachesBeyondTheArt` (`CuriousContraptions.tests/VisibleSolidColliderTests.cs:186`) |
| R3 | Typed ports and wrong-kind refusal | present | `PortDescriptor` (`CuriousContraptions.Abstractions/PortDescriptor.cs:55`) | `BundlePortsTests.AWrongDomainOrOutputToOutputLinkIsStillRefused` (`CuriousContraptions.tests/BundlePortsTests.cs:262`) |
| R5 | Supply contact closed by a signal state, between a source and a consumer | present | `SignalStateContactControl` (`CuriousContraptions.Abstractions/SignalContactControl.cs:12`), declared at `elements/StateLatch/StateLatchElementDeclaration.cs:67` | `ComposableSupplyRoutingTests.SimulationAdvancesPowerAcrossPackageInternalEdgesToRechargeStore` (`CuriousContraptions.tests/ComposableSupplyRoutingTests.cs:530`, fixture at `:453`–`:466`) |
| R6 | Run, Reset, save and load of every admitted declaration | present | loop over `TestBundle.Packages` | `CatalogueIntegrationTests.EveryAdmittedDeclarationRetainsAuthoredStateAcrossRunResetSaveLoad` (`CuriousContraptions.tests/CatalogueIntegrationTests.cs:273`) |
| N1 | Line of sight through solids | narrower: `AirflowLineClear` (`CuriousContraptions.Simulation/WorkshopPhysicsEngine.Airflow.cs:141`) answers clear or blocked for airflow rows only and loops over every collider (`:149`); the broadphase offers only `QueryOverlaps` (`CuriousContraptions.Simulation/WorkshopDynamicBvh.cs:145`) | — | `AirflowGeneralityTests.MovingSolidOcclusionIsResampledBeforeTransfer` (`CuriousContraptions.tests/AirflowGeneralityTests.cs:13`). Owner of the segment cast: **ENGINE-SPATIAL-QUERIES** |
| N2 | A declared value bound to the options of a Choice | narrower: `ChoicePredicate` (`CuriousContraptions.Abstractions/AirflowValues.cs:23`) selects airflow input rows only | — | `FanIntegrationTests.ElectricalModePaysOnlyExposedWorkAndNeverUsesSelfContainedEnergy` (`CuriousContraptions.tests/FanIntegrationTests.cs:507`). Owner: **ENGINE-PARAMETER-BOUND-FRAMES** (choice-bound values, coordinator decision 16) |
| N3 | Presentation binding on a committed driver | narrower: no strength member (`CuriousContraptions.Abstractions/ElementPresentation.cs:18`–`:37`) | `PresentationDriver` | `BothGateTests.ThreeNativeLampsFollowCommittedInputsAndSupplyThroughHideAndReset` (`CuriousContraptions.tests/BothGateTests.cs:259`). Owner: **ENGINE-OPTICAL** |
| M1 | Declared receivers with weighted samples and a committed reading and strength | **missing** | — | owner **ENGINE-OPTICAL** |
| M2 | Radiation channels, attenuation rows and chords | **missing** | — | owner **ENGINE-RADIATION** |
| M3 | A threshold node with a release ratio over a committed scalar, the contact state it presents to `SignalStateContactControl`, the committed level it presents to a presentation binding (the lamp), and a pulse on its rise | **missing** (a contact today names `SignalContactState.Latched` or `TimerCounting`, `CuriousContraptions.Abstractions/SignalContactControl.cs:5`) | — | owner **ENGINE-SIGNAL-NODES** |
| M4 | `ElementCategory.Radiation` (`CuriousContraptions.Abstractions/ElementCategory.cs:5`–`:12` has five members) and the radiation `SimulationDomain` flag (`CuriousContraptions.Abstractions/SimulationDomain.cs:6`) | **missing** | — | owner **ENGINE-RADIATION** |

Nothing radiation-specific exists in `CuriousContraptions.Abstractions*`, `CuriousContraptions.Simulation`, `CuriousContraptions.Bundle`, `engine/` or `elements/` (searched 22 November 2026 for radiat, dose and decay symbols). ENGINE-RADIATION ([Story ENGINE-RADIATION](../../../_bmad-output/planning-artifacts/epics.md#story-engine-radiation-emission-attenuation-decay-and-exposure), sprint key `engine-radiation`) is the one owner of the radiation-specific rows.

**Dependencies.** Battery and Powered gate (delivered); any radiation source, first [RAD-01](RAD-01-gamma-source-capsule.md) (undelivered, planned before this slice).

## 4. Sources and legacy

- [radiation-08](../requirements.md#radiation-08): "correct exposure switches the output; wrong channel, blocked path and missing supply do not. Muting clicks leaves results identical."
- [Named entry](../invest/named-elements.md#radiation-08), owner S615; map composition note: "separately supplied meter/output state and response thresholds; numerical rate has no accumulated-dose memory substitution"; binding `radiation-01.json` (proof S615).
- [S605](../invest/decisions.md#s605) photon/alpha rows; research per-element row (needle ← committed rate; lessons 76, 83, 90, 91 → 101, 108, 115, 116) and [first-slice acceptance](../../radiation-component-research.md#chrome-observable-acceptance-for-the-first-radiation-slice).
- **Legacy.** No rate meter in `parts/` (the legacy `sound_meter` is acoustic), engine, tests, levels, Campaign or `reference/`. Hits are coverage snapshots only: the older binding copy `reference/P0-022-before/docs/coverage/engine/task-017.json@a6c914e:L1869-L1899`, identical to current, and aggregate relation lists that name this identity — no element knowledge; do not carry forward.

**Files harvested** (all "no element knowledge"):
- `reference/P0-022-before/docs/coverage/engine/task-017.json`
- `reference/P0-022-before/docs/coverage/engine/task-002.json`
- `reference/P0-022-before/docs/coverage/engine/task-003.json`
- `reference/P0-022-before/docs/coverage/engine/task-013.json`
- `reference/P0-022-before/docs/coverage/engine/capabilities-02.json`

## 5. Acceptance

### Behaviour and numbers

All values are starting values (D1) under the [radiation conventions](#radiation-conventions).

- **Reading law.** Strength = Σ over channels of weight × received power. For a diverging source of intensity I the received power is A × the mean over the nine samples of the sample term, times the transmission of every crossing on the way. Beyond 1 m the sample term is I × facing ÷ d²; inside 1 m it is I ÷ 1 m² for a source in front of the face. For the Gamma capsule (10 W/sr) square on the axis: R ≈ 0.09 × 10 ÷ d² = 0.9 ÷ d² W beyond 1 m.
- **Nine-sample value at 1 m.** Centre sample 10 W/m². Edge samples: d² = 1.01, facing 1 ÷ √1.01 = 0.995, E = 10 × 0.995 ÷ 1.01 = 9.85. Corner samples: d² = 1.02, facing 0.990, E = 9.71. Mean (10 + 4 × 9.85 + 4 × 9.71) ÷ 9 = 9.80 W/m². R = 0.09 × 9.80 = 0.882 W, 2 % under the axis formula.
- **Inside 1 m.** Every sample term is 10 W/m², so R = 0.09 × 10 = 0.900 W whether the capsule is at 0.9 m or touching the face. The reading never rises as a source moves away.
- **Switching distances.** On at 0.2 W: d = √(0.9 ÷ 0.2) = 2.12 m. Off at 0.75 × 0.2 = 0.15 W: d = √(0.9 ÷ 0.15) = 2.45 m. The 0.33 m band between them stops a contact chattering as a source rolls past. A response region of about 2 m suits the 16.8 m bench.
- **Shielding.** One Dense slab (0.05 m, 20 /m): 0.882 × e^−1 = 0.882 × 0.368 = 0.324 W, still on. Two: 0.882 × e^−2 = 0.882 × 0.135 = 0.119 W, off. So one slab is too little and two are enough. A Wall across the path (0.25 m, default row 0.15 /m) gives 0.882 × e^−0.0375 = 0.849 W: an ordinary part is not a shield.
- **Two sources.** Each at 2.6 m gives 0.9 ÷ 6.76 = 0.133 W, below the mark; together 0.266 W, above it. Readings are sums over sources.
- **Shares.** Six meters boxed around a capsule inside 1 m each ask 0.900 W, 5.4 W of the capsule's 125.66 W, so no meter layout is ever scaled. The scaling rule is proven on a law scene: one receiver of area 20 m² with one sample, 0.5 m from an isotropic 1 W source (0.0796 W/sr), asks 20 × 0.0796 = 1.59 W and commits 1.00 W.
- **Charged beams.** With `sensitivity` Alpha, an Alpha cartridge's 1 W beam ending on the face reads 1 W inside its 2 m range and nothing beyond.
- **Back face.** A source behind the meter has facing 0 and reads nothing.
- **Tick order.** The field solve of tick n runs after the last substep, on that tick's final poses, and commits the strength with the tick. The signal network of tick n + 1 turns `detect` on, emits the pulse and closes `relay`, so the load is supplied one tick after the radiation arrives. Supply lost in a tick opens the route in that tick; the strength and `detect` are unaffected.
- **Energy.** The meter absorbs what its case attenuates; it adds none. Its contact passes only what `PowerIn` receives.

### Rows

Planned class `RadiationRateMeterTests`. Fixture: Battery `Supply` → meter `PowerIn`; meter `Supply` → Powered gate `PowerIn`; a Gamma capsule on the meter's +X axis. Readings ±3 % (the nine-sample mean and the axis formula differ by 2 %; the nearest decision margin is 20 %). Stall guard 2 s.

| # | Given / When / Then | Test | Control | Boundary and tolerance |
| --- | --- | --- | --- | --- |
| A1 | Given the capsule at 1 m, when Run starts, then the committed strength is 0.882 W, `detect` is on and the gate is supplied (row: correct exposure switches the output) | `RadiationRateMeterTests.ASourceInRangeClosesTheSuppliedContact` | A2 | ±3 % |
| A2 | Given the capsule at 1.90 m `detect` is on (0.248 W: nine samples, centre 10 ÷ 3.61 = 2.770, edge 10 × 0.9986 ÷ 3.62 = 2.759, corner 10 × 0.9972 ÷ 3.63 = 2.747, mean 2.755 W/m² × 0.09); at 2.75 m it is off (0.09 × 10 ÷ 7.5625 × 0.999 = 0.119 W) and the gate stays unsupplied for 240 ticks | `RadiationRateMeterTests.TheLevelFollowsDistance` | A1 | 24 % above the 0.2 W mark and 21 % below the 0.15 W release; ±3 % |
| A3 | Given a law-scene emitter moving away so the strength falls from 0.3 W to 0.1 W, then `detect` stays on at 0.17 W and turns off below 0.15 W, once | `RadiationRateMeterTests.ReleaseIsBelowTheOnMark` | A2 | one off transition |
| A4 | Given `sensitivity` Alpha with the Gamma capsule at 1 m, or `sensitivity` Photon with an Alpha cartridge at 0.5 m, then the strength is 0 and `detect` stays off (row: wrong channel) | `RadiationRateMeterTests.AWrongChannelNeverSwitches` (theory) | A1 | strength below 0.001 W |
| A5 | Given two Dense slabs on the path `detect` is off (0.119 W); one slab leaves it on (0.324 W); the two slabs beside the path leave 0.882 W; a Wall across the path leaves 0.849 W (row: blocked path) | `RadiationRateMeterTests.ShieldingOnThePathLowersTheReading` | A1 | ±3 % |
| A6 | Given no wire to `PowerIn`, then `detect` is on and `Supply` is never live (row: missing supply) | `RadiationRateMeterTests.AnUnsuppliedMeterSwitchesNothing` | A1 | gate unsupplied for 240 ticks |
| A7 | Given two capsules each at 2.6 m `detect` is on; either alone leaves it off | `RadiationRateMeterTests.SourcesAdd` | one capsule | 0.266 W ±3 % |
| A8 | Given the same Run with and without a presentation consumer, then the committed strengths, levels and the tick of the first supply are the same (row: muting clicks leaves results identical) | `RadiationRateMeterTests.PresentationNeverChangesTheReading` | A1 | equal decoded levels and counts |
| A9 | Given a capsule held in range for 600 ticks, then `ActivationOut` pulses once; a Counter wired to it reads 1 | `RadiationRateMeterTests.AStationarySourceCountsOnce` | source moved out and back gives 2 | exact count |
| A10 | Given `threshold` 0.02 and 2.0 they are admitted; outside values and a non-finite value are refused with nothing changed; at 1.0 W the capsule at 1 m leaves `detect` off | `RadiationRateMeterTests.TheThresholdIsBoundedAndApplied` | A1 | typed refusal |
| A11 | Given the capsule behind the meter at 1 m, then the strength is 0 | `RadiationRateMeterTests.TheBackFaceReadsNothing` | A1 | below 0.001 W |
| A12 | Given an electrical output wired to `ActivationOut`, or `Supply` wired to a signal input, then the edit is refused | generic: `BundlePortsTests.AWrongDomainOrOutputToOutputLinkIsStillRefused` (R3) | — | typed refusal |
| A13 | Given a lit Run, when Reset, then no strength or level remains and the needle and lamp are at rest; a saved and reloaded construction behaves as A1 with its `sensitivity` and `threshold` | `RadiationRateMeterTests.ResetAndReloadRestoreTheAuthoredState`; authored state by R6 | A1 | strength 0 |
| A14 | Given `sensitivity` Alpha and an Alpha cartridge aimed at the face from 1 m, `detect` is on; from 2.2 m it is off. The same with Beta-minus at 1 m and 3.2 m | `RadiationRateMeterTests.EachChargedSensitivityReadsItsOwnBeam` (theory; queued until RAD-03 and RAD-04 are delivered) | A4 | 0.2 m beyond each range |
| A15 | Given the capsule at 0.9 m, at 0.5 m and touching the face, then the strength is 0.900 W each time and never above it | `RadiationRateMeterTests.TheReadingIsFlatInsideOneMetre` | A1 | ±3 % |
| A16 | Given a law-scene receiver of 20 m² with one sample, 0.5 m from an isotropic 1 W source, then its strength is 1.00 W, not the 1.59 W the unscaled share asks; six meters around a capsule each read 0.900 W | `RadiationRateMeterTests.ReceivedNeverExceedsEmitted` | A15 | ±3 %; never above 1 W |

Chrome recipe (pending, post-catalogue): Battery, Gamma capsule 1 m from the meter, Powered gate holding a ball over a Receiver; wire and Run; then move the capsule to 3 m.

## 6. Open questions

None open.

1. **Rate law — decided** (D16, decision O-1 of the [batch summary](readiness/batch-o.md); coordinator decision 21): the optical distance law and samples, in watts, flat inside 1 m; section 10.
2. **Player-editable thresholds — decided** (D11): `threshold` is editable unless the level locks it. One editable mark with a fixed release ratio gives the "separate on/off thresholds" of the row; two free values could be set in the wrong order, which a parameter bound cannot refuse.
3. **One body or three items — decided** (D8): one catalogue entry with a `sensitivity` setting; neutron detection is EL-131 and EL-132.
4. **Energy-band sensitivity — decided** (D19): none. No requirement row or lesson uses a band-selective meter; the band matters through shielding.
5. **Instrument states — decided now**: no state machine. The strength is always committed; an unsupplied meter passes nothing because its contact has nothing to pass.
6. **Coupling — decided** (coordinator decision 16): strength → threshold node → signal-state contact. No memory node is set by a receiver.

Needs the owner: none.

## 7. Integration

- **Collaborators.** Battery, Powered gate, Counter (delivered). Sources RAD-01, RAD-02, RAD-03, RAD-04, RAD-13, RAD-22; shields EL-126 to EL-128; shutter RAD-06; collimator RAD-07 (all undelivered). Port domains agree: `PowerIn` Electrical Input against the Battery's `Supply`; `Supply` Electrical Output against a load's `PowerIn`; `ActivationOut` Signal Pulse Output against the Counter's `ActivationIn`.
- **Cross-element tasks.** Receiver in [sequence-task-413](../requirements.md#sequence-task-413) and the photon-only control in [sequence-task-427](../requirements.md#sequence-task-427); processes [IX-15](../requirements.md#interaction-15), [IX-06](../requirements.md#interaction-06), [IX-07](../requirements.md#interaction-07).
- **Campaign.** Chapter 11, first use 101, then 108, 115, 116; reuse 111–120, 124–130, 136–150. No level in `content/puzzles.json` places it.
- **Coverage binding (planned, not yet live).** Blocked on the coverage follow-on story S7. Main's reader lists only `EL-*.md` specs and admits only an `EL-NNN` identity with an `element-NNN` anchor (`tools/Coverage/NamedPackageSources.cs:19`, `:57` on `origin/main`), so a `RAD-NN` identity cannot be bound until S7 widens it (owner decision 15). The slice adds the binding once S7 admits it.
- **Lesson fixtures.** Story [S8](../invest/delivery-batches.md#s8-lesson-fixtures-for-any-admitted-package) lands after the engine programme (owner decision 22); a level that places the meter locked needs what S8 delivers.

## 8. Delivery

- **Slice shape.** Single element.
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), which includes ENGINE-SPATIAL-QUERIES, ENGINE-SIGNAL-NODES and ENGINE-PARAMETER-BOUND-FRAMES; ENGINE-OPTICAL; ENGINE-RADIATION as amended; RAD-01; S7 for the binding. No dependency on ENGINE-THERMAL ([ENGINE-RADIATION](../../../_bmad-output/planning-artifacts/epics.md#story-engine-radiation-emission-attenuation-decay-and-exposure)'s *Depends on* line names no thermal story).
- **Files.** `elements/RadiationRateMeter/RadiationRateMeter.csproj`, `RadiationRateMeterElementDeclaration.cs`, `radiation_rate_meter.tscn`, `icon.svg`; one guarded line in `CuriousContraptions.slnx`; `CuriousContraptions.tests/RadiationRateMeterTests.cs`; a row in `VisibleSolidColliderTests`. Registration, bundle, palette and coverage membership are derived (`spec-registration-derivation.md`).
- **Review tier.** Data-only if the probe finds every row present after the engine stories; otherwise the engine tier.
- **Disqualifiers (the slice's probe re-checks).** A threshold node that cannot be named by `SignalStateContactControl` (then the contact needs a new control kind, an engine slice); a different unit scale or distance law (re-derive section 5); no choice-bound receiver weights (the slice stops and the binding story is amended; no second design is kept here); no strength driver for the needle.

## 9. Engine conformance

Verdict: **Conforms** (every missing capability has exactly one owning element-free story).

1. **Declaration data only.** A static body, five colliders, three ports, one receiver, one threshold node, one contact with its electrical node, two parameters and five bindings. No code path names the meter.
2. **Generic laws.** The line-of-sight field law of section 10; a threshold over a committed scalar; supply reservation through a contact. "Radiation closes the switch" is a strength crossing a threshold node that a contact reads.
3. **Three layers.** Colliders: five primitives on one static body. Visuals: the art table; face, dial, lamp and marks are flush or printed. Animation: needle, lamp and click bindings read committed state and write nothing back.
4. **One-way flow and typing.** Field solve → strength → signal node → next tick's supply; presentation reads committed state only. Channels, roles and options are enums; powers are f32.
5. **Flat state.** The counts in section 2 are fixed at compile time.
6. **Performance fitness.** Static body; five primitives; nine shadow casts per diverging source in range, each through the spatial query's segment cast; no sensor volume; no per-tick work proportional to the scene.
7. **Tests are scenarios.** Rows A1 to A16 run the production simulation through placement, wiring, Run and the committed read.

<a id="radiation-conventions"></a>
## 10. Radiation conventions (owned here; every radiation spec cites this section)

Radiation is a line-of-sight field like light. It is solved by the one solve of the [optical conventions](EL-176-red-laser-emitter.md#optical-conventions), whose rules are written over a declared channel set; there is no second ray engine. This section cites those conventions by item ("optical 4") and states only what radiation adds or changes. Each item settled by a decision is marked with it: **(coordinator 16)**, **(coordinator 21)** and **(coordinator 26)** are coordinator decisions of 22 November 2026, and **(owner, 21 November)** is the owner decision of 21 November 2026 on steady sources. Repaired on 23 November 2026 against the conventions re-audit (`docs/planning/elements/readiness/audits/conventions-reaudit.md`) under coordinator decision 26: items 3, 4, 7, 9, 10, 11 and 12. It is the family's input to ENGINE-RADIATION; if that slice changes a value, this section is edited and every citing spec follows. Nothing below exists in code today.

1. **Channels and units.** Seven channels, a closed set beside the three of light: PhotonSoft, PhotonMedium, PhotonHard, Alpha, BetaMinus, NeutronFast, NeutronSlow. "Gamma" and "X-ray" name sources, not channels: penetration depends on the band. Units are those of optical 1, f32: source strength is radiant power in watts per channel, or intensity I = P ÷ Ω in W/sr; irradiance is W/m². **Dose rate** is the power received at the standard aperture area, in watts. **Dose** is its time integral, in joules. All values are game fiction.
2. **Sources (owner, 21 November; coordinator 21).** A source is the emitter of optical 2 with radiation channels. It is paid either by a supply consumer (optical 2: radiant × energy granted over the tick's substeps ÷ energy demanded) or by a declared **finite inventory** in joules, and never emits unpaid power. A decaying inventory with half-life T½ emits remaining × (1 − 2^(−dt ÷ T½)) each tick, so its power is P₀ · 2^(−t ÷ T½) and its total can never exceed the inventory P₀ · T½ ÷ ln 2. A steady inventory (half-life none) emits constant power P₀ and holds P₀ × 300 s, the maximum Run length of ENGINE-RUN-LENGTH, so no admissible Run spends it and its strength is fixed within a Run (owner decision of 21 November, RAD-01, and coordinator decision 21 of 22 November). Reset restores the inventory. [IX-16](../requirements.md#interaction-16) holds as written for every source.
3. **Propagation (coordinator 21, coordinator 26).** The one near-field rule of optical 1, unchanged: beyond 1 m the sample term is I × facing ÷ d²; inside 1 m it is I ÷ 1 m² for a source on the front side of a Lambert aperture, so a reading is flat inside 1 m and never rises with distance. Requirement [radiation-01](../requirements.md#radiation-01)'s "moving the capsule farther away lowers the meter reading" therefore holds for moves that end beyond 1 m. Photon and neutron sources diverge (optical 3: terminal, reaching receiver samples directly). Alpha and BetaMinus sources are collimated: one zero-width ray carrying its whole power to its declared range. Air does not attenuate.
4. **Crossings and attenuation (coordinator 21, coordinator 26).** Every candidate comes from the segment cast of ENGINE-SPATIAL-QUERIES returning ordered crossings (entry, exit, collider) at this tick's final poses (optical 5 and 6). Per channel the transmittance of a crossing is surface τ × exp(−μ × chord). Three rules complete the shared cast for radiation:
   - **Surface.** For radiation channels every collider's surface τ is 1 unless declared; attenuation is the row's coefficient × chord only. An optical response (Absorb, Reflect, Transmit) names light channels and does not act on radiation; nothing reflects or refracts radiation.
   - **Overlap.** Where chords of several colliders of one body overlap, the overlapped length is counted once, with the largest coefficient per channel.
   - **Own body.** Optical 5's one own-body rule, for light and radiation alike (coordinator 26): a receiver's shadow cast from a diverging source ends at its aperture face, or at the sample point itself for a Uniform receiver (item 7), and every crossing before that end attenuates by its row, the receiver's own colliders included. So a source behind a Lambert receiver has facing 0, and a Uniform receiver reads a source on the far side of its own body through the chord of that body to each sample. Every collimated segment continues past the apertures it crosses and is attenuated by every body on its path, the receiver's included. A source skips its declared internal occluders (optical 6).

   μ comes from the collider's declared row of seven coefficients in 1/m. A collider that declares no row takes the scene's one **default row**, a declared value listed and tested by ENGINE-RADIATION; a declared row that omits a channel is refused at bundle time, so nothing falls back silently. No buildup factor and no scattered component: a channel's power only falls along a path, except by a declared conversion. A liquid store's committed filled region is crossed as a volume with the liquid's row. Art never attenuates.

   | Row | Soft | Medium | Hard | Alpha | BetaMinus | NeutronFast | NeutronSlow |
   | --- | --- | --- | --- | --- | --- | --- | --- |
   | Default | 0.4 | 0.15 | 0.075 | 400 | 150 | 0.2 | 0.2 |
   | Thin screen | 2 | 0.5 | 0.25 | 400 | 20 | 1 | 1 |
   | Polymer | 8 | 3 | 1.5 | 400 | 300 | 2 | 2 |
   | Dense | 64 | 20 | 10 | 2000 | 2000 | 1.6 | 2.8 |
   | Absorber | 6 | 2 | 1 | 400 | 150 | 2 | 60 |
   | Window | 2 | 0.5 | 0.25 | 5 | 5 | 1 | 1 |
   | Scintillator | 16 | 6 | 3 | 400 | 150 | 0.2 | 0.2 |
   | Liquid | 3 | 1.2 | 0.6 | 400 | 150 | 2.4 | 0.3 |

   The Liquid row also declares the conversion NeutronFast → NeutronSlow, m = 0.8. Rows are data carried by packages (the Liquid row by the scene's one liquid); the engine knows no row by name. The default row's photon coefficients are a tenth of an ordinary solid's, so the shielding lesson needs shields: for PhotonMedium a Wall (collider 3 × 2 × 0.25 m, `elements/Wall/wall.tscn:19`) passes e^(−0.15 × 0.25) = 0.963 across and e^(−0.15 × 3) = 0.638 end-on, a 0.56 m ball chord e^−0.084 = 0.919, against one Dense slab's e^−1 = 0.368 and four slabs' e^−4 = 0.018.
5. **Conversion.** A row may declare one conversion (from channel a, to channel b, fraction m ≤ 1). Per crossing of length L: out_a = in_a · e^(−μ_a·L); out_b = in_b · e^(−μ_b·L) + m · in_a · (1 − e^(−μ_a·L)). Converted power is not attenuated again inside the crossing that produced it. out_a + out_b ≤ in_a + in_b. Conversion creates no segment.
6. **Relay.** The Relay of optical 7 may list collimated radiation channels in its outlet masks; its enable names a signal level. Channels outside every mask, and every channel while it is disabled, cross by item 4. A curved path is declared as its result, never solved.
7. **Receivers.** The receiver of optical 8 with a WeightedSum strength over radiation channels and a declared facing rule, a closed set: Lambert (facing = max(0, −d̂·n)) or Uniform (facing 1 for every incident direction; the declared area is an effective cross-section; sample points are points on the frame body; a collimated segment is credited when it crosses the aperture rectangle from either side). Shares of one diverging segment are conserved as in optical 8: if the receivers' uncapped shares A × Σ wᵢEᵢTᵢ sum to more than the segment's power, every share is scaled by power ÷ sum. Standard detector: aperture 0.30 × 0.30 m, the **standard area** 0.09 m², nine samples. A collimated segment is credited with the power it carries where it crosses the aperture rectangle, from the front side for Lambert and from either side for Uniform, with no facing factor, and continues by item 4. A reading observes the power crossing the aperture, a flux and not energy absorbed (coordinator 26): several apertures on one collimated path each read what crosses them, no reading credits anything, and power leaves the field only by attenuation along the path (item 10), so absorbed energy never exceeds emitted.
8. **Coupling (coordinator 16).** As optical 9: a receiver commits a continuous reading and strength; its level is a threshold node over that committed scalar with a declared release ratio (0.75 for meters, 0.97 for gauges), owned by ENGINE-SIGNAL-NODES. Supply contacts use the delivered signal-state control; a relay or emitter enable names a signal level; logic is a boolean signal node. Nothing reads a receiver directly. Standard threshold 0.2 W.
9. **Dose and "exposed" (coordinator 21, coordinator 26).** An **accumulator** integrates one receiver's strength into a committed scalar in joules: + strength × dt each tick (dt the committed tick's duration, optical 4), never reduced within a Run, with a declared capacity and an authored initial value. Every accumulating receiver declares the standard area, so one dose scale serves every part: 1 m from a 10 W/sr source is 0.882 J/s on a nine-sample Lambert face and 0.900 J/s on a one-sample Uniform receiver. A threshold node over the accumulator gives its level. A body, or a goal's subject, is **exposed** on a tick when a receiver on it has committed strength of at least 0.001 W; its exposure is the accumulator's committed value. Reset restores the authored initial value; saves hold authored values only.
10. **Energy (coordinator 21, coordinator 26).** Power removed along a crossing is booked once, on the absorbing collider's record (optical 7), and becomes heat only as thermal T3 moves it ([thermal conventions](TH-04-electrical-heating-plate.md#thermal-conventions)). A receiver's absorbed power is, per sample and channel, its share × (1 − exp(−μ × chord)) for the chord of that sample's ray continued through the receiver's frame body; it is booked on the frame body's collider like any absorbed power. Absorbed radiation becomes supply by one form only, optical 9's: where the part declares a credit, the declared fraction of the energy booked on that collider moves from the record to a finite store that an ordinary supply source draws from, at the end of the solve, and the remainder stays on the record for thermal T3. The receiver credit is ENGINE-OPTICAL's receiver-transfer row, written over channels, so it serves radiation unchanged; the credited store is one row of ENGINE-THERMAL (optical 9; coordinator decision 26, 22 November 2026; Batch O G20); a thermal converter's credit is ENGINE-THERMAL's (thermal T4). A reading is not absorbed energy: readings along one collimated path observe the same power and are not a transfer, so they may sum to more (item 7), and no reading credits a store or a node. Absorbed energy never exceeds emitted energy, and the shares of one diverging segment are scaled so their sum never exceeds its power (item 7). No row, relay or conversion creates power.
11. **Tick order (coordinator 16, coordinator 26).** The [one combined tick order](EL-176-red-laser-emitter.md#combined-tick-order) of optical 4, which this item does not restate: the one solve runs once per tick of duration dt, after the last substep and before the thermal step, reading this tick's final collider poses and liquid levels, the energy granted over this tick's substeps and the signal levels resolved at tick start. Readings, strengths and accumulators commit with the tick and act from the next tick.
12. **Work per tick (coordinator 26).** As optical 11: at most C × L segment casts for C collimated sources and L the optical segment limit (optical 10), plus, for each diverging source in a tick, one shadow cast per sample point of each candidate receiver that its cone query returns through ENGINE-SPATIAL-QUERIES; no other receiver is tested. A pass-through collimated ray is one cast returning every crossing. Each cast costs what the spatial query's cast costs; none scans every collider. A cast returns every crossing on its segment, bounded only by the colliders on that segment: there is no crossing cap and no typed refusal for a scene capacity (design rule D5). The per-declaration bounds are optical 10's interaction and segment limits. ENGINE-RADIATION states a measured before and after on a fixed reference scene.
13. **Presentation.** Radiation is not drawn during Run except by parts that declare a display. Needles, lamps and bars are bindings on committed strengths, levels and accumulator fractions. The Build preview and the shared segment renderer are ENGINE-OPTICAL's (optical 12).
14. **Family values.** Gamma capsule 125.66 W isotropic PhotonMedium (10 W/sr), range 16 m, inventory 37,699 J. X-ray emitter 2.141 W over 15° (0.2141 sr, 10 W/sr) in one chosen band, demand 12 W. Alpha cartridge 1 W collimated, range 2 m, inventory 300 J. Beta-minus cartridge 1 W collimated, range 3 m, inventory 300 J. Decay capsule 251.33 W (20 W/sr), half-life 4, 8 or 16 s, inventory P₀ · T½ ÷ ln 2 (1,450, 2,901 or 5,801 J). Neutron module 125.66 W NeutronFast, inventory 37,699 J. Tracer capsule 62.83 W (5 W/sr), inventory 18,850 J. The standard detector 1 m from a 10 W/sr source reads 0.882 W, and 0.900 W anywhere inside 1 m.
