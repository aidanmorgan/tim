# RAD-20 · Radioisotope thermoelectric generator — element readiness spec

**Current delivery (24 October 2026):** [RAD-20 delivery slice](../invest/named-elements.md#radiation-20). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Story 7.0 Batch O named-identity spec, brought to build-ready detail on 22 November 2026 against checkout `spec-drafts` (published main `8d1d68aa`) and repaired the same day after the independent audit (`docs/planning/elements/readiness/audits/elements-batch-o.md`, findings H-2, H-3, C-9, X-2, X-4) and owner decisions 13, 17 and 18 and coordinator decision 21. Legacy citations are pinned to `a6c914e`; citations without `@` are `path:line` in the checkout. Requirement row: [radiation-20](../requirements.md#radiation-20) (P3 potential). Lengths are metres, temperatures kelvin. The module is fully contained: it emits no radiation, so the [radiation conventions](RAD-08-radiation-rate-meter.md#radiation-conventions) apply only through item 2 (a steady source debits a finite inventory sized for the 300 s maximum Run) and item 10 (energy accounting). Heat follows the [thermal conventions](TH-04-electrical-heating-plate.md#thermal-conventions) (T1 to T14, cited by item), and the converter is the generator row of [TH-30](TH-30-thermoelectric-generator.md) with this part's values. All values are game fiction.

## 1. Identity

| Item | Value |
| --- | --- |
| ID / name | RAD-20 · Radioisotope thermoelectric generator |
| Type | Thermal + Electrical (a steady inventory-paid heat source with a thermoelectric converter) |
| Anchor | [requirements.md#radiation-20](../requirements.md#radiation-20); [named-elements.md#radiation-20](../invest/named-elements.md#radiation-20) |
| Owner | S631 |
| Disposition | To deliver as a single element. It is not a duplicate of the Thermoelectric generator (TH-30): it adds an internal steady heat source and has its own declared capacities, conductances and store. It declares TH-30's generator row with different values, never a second law. It is not a Battery: its electrical store is a 2 J buffer filled only by the converter, so cooling, not a charge, decides the output. |
| Planned package | id `radioisotope_thermoelectric_generator`, directory `elements/RadioisotopeThermoelectricGenerator/`, class `RadioisotopeThermoelectricGeneratorElementDeclaration` |
| Related identities | Converter row [TH-30](TH-30-thermoelectric-generator.md) (lead [TH-12](TH-12-reversible-heat-pump.md)); cooling [TH-10](TH-10-finned-heat-sink.md) and a self-contained Fan; loads a Fan in Electrical mode and the Motor (the Signal lamp has no electrical input: one Signal `ActivationIn`, `elements/SignalLamp/SignalLampElementDeclaration.cs:35`–`:38`) |
| Status | not started; no package; blocked on the engine programme (owner decision 3a, 22 November 2026), ENGINE-SIGNAL-NODES, ENGINE-RUN-LENGTH and ENGINE-THERMAL |
| Run length needed | 60 s (ENGINE-RUN-LENGTH, owner decisions 13 and 17): rows G1, G3, G4 and G7 observe to 55 to 60 s |

## 2. Declaration

Shape followed: TH-30 for the converter, the credited supply store and the source → signal-state contact → output path (`elements/Battery/BatteryElementDeclaration.cs:70`–`:74` for the store-backed source; `CuriousContraptions.tests/ComposableSupplyRoutingTests.cs:453`–`:466` for the internal contact path). Items marked **(thermal)** or **(signal)** use contracts that do not exist yet.

| Item | Value |
| --- | --- |
| Interfaces | `ISupplyElement` (`CuriousContraptions.Abstractions.Electrical/ElectricalContracts.cs:11`), `ISignalElement` (`CuriousContraptions.Abstractions.Signal/SignalContracts.cs:8`), `ISceneGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`), plus ENGINE-THERMAL's node, source and converter contracts **(thermal)**. `KinematicMode.Static`. |
| Body | One static scene body; restitution 1, friction 0.3, rolling resistance 0, bounce threshold 0.1 m/s (the static default, `BatteryElementDeclaration.cs:61`–`:64`) |
| Colliders (hidden `col_` proxies) | `col_casing` box 0.80 × 0.90 × 0.80 m at the origin. `col_cold` box 0.60 × 0.04 × 0.60 m at (0, 0.47, 0) on the casing's top (y 0.45 to 0.49); its +Y face is the contact face. One socket sphere radius 0.075 m at the port. Three colliders. No radiation row is declared and no emitter exists: nothing leaves the casing. |
| Ports | `PortId.Supply`, Electrical, Output, role `Supply`, at (−0.40, 0, 0), normal −X |
| Heat source **(thermal)** | `decay`: a steady finite inventory of 24 000 J (80 W × 300 s, the maximum Run of ENGINE-RUN-LENGTH; radiation convention 2) credited to `core` at a constant 80 W and debited by the same joules each tick (T4). No admissible Run spends it; Reset restores it. |
| Thermal nodes **(thermal)** | `core`: heat capacity 3 J/K, ambient conductance 0.2 W/K (the insulated casing), no face. `cold`: heat capacity 0.25 J/K, ambient conductance 0, face `col_cold`, 64 W/K (metal, T11). Both start at the 288 K ambient. With TH-10's 64 W/K foot standing on it, the static-on-static contact link pairs at 32 W/K (T3); the plate has no other path out. |
| Converter **(thermal)** | TH-30's generator row (T4, the one form of coordinator decision 21) from `core` to `cold`, crediting the store of `source`: Q_h = K·(T_h − T_c) with K = 0.4 W/K; electrical energy converted in a tick E = min(0.3·(1 − T_c ÷ T_h)·Q_h·dt, the store's free capacity) while T_h > T_c, otherwise 0; `core` is debited Q_h·dt, `cold` credited Q_h·dt − E, and E is credited to the store. Nothing else fills the store. |
| Supply graph | Source node `source`: an ordinary finite store, capacity 2 J, power limit 16 W, initial charge 0, always enabled (declared constants of the credit form, as TH-30). 2 J holds more than one tick's credit at the slowest 60 Hz cadence, as coordinator decision 32 requires: the converter credits at most 0.3 × the heat through the module, which, as the core warms from ambient toward its steady state, stays below the source's 80 W, so at most 24 W, 0.40 J per 1/60 s tick (4.74 W, 0.079 J, at G1's working point). Contact `outlet`: `SupplyContactDescriptor` with `SignalStateContactControl` (`CuriousContraptions.Abstractions/SignalContactControl.cs:12`) on `available`. Edges `source` Output → `outlet` PowerIn; `outlet` Output → `Supply`. Electrical nodes: `source` `ElectricalRole.Source`, 12 V, maximum 16 W; `outlet` `ElectricalRole.Switch`. |
| Signal graph **(signal, ENGINE-SIGNAL-NODES)** | `available`: a threshold node over the committed stored fraction of `source`: on at 0.25 (0.5 J), release ratio 0.2 (off below 0.05, 0.1 J), as TH-30. |
| Parameters | None (D13). |
| Stores | The heat inventory (24 000 J) and the electrical buffer (2 J). Neither is a charge the player spends: the buffer fills only from the converter. |
| Presentation bindings | `Visual/Lamp` albedo red, green and blue, slate `#556573` → gold `#f7cb52`, ← `PresentationDriver.CommittedSupply` with `SupplyNode: source` (the Battery's form, `elements/Battery/BatteryElementDeclaration.cs:10-20`): 1 while the source is available, else 0 (`engine/gpu/WorkshopDeclaredPresentation.cs:492-502`); the driver gives availability, not delivered power. `CommittedStoredFraction` is not used: the bundle admits it only on a mechanical `EnergyStore` (`CuriousContraptions.Bundle/BundleValidator.cs:310-312`). `Visual/CoreBand` albedo terracotta → red from the committed `core` temperature over 288 to 688 K **(thermal)** |
| Footprint | `ElementFootprint(Bodies: 1, MovingBodies: 0, PrismaticConstraints: 0, RevoluteConstraints: 0)`; domains Electrical, Signal and the thermal flag **(thermal)** |
| Catalogue | Id `radioisotope_thermoelectric_generator`, "RTG", `ElementCategory.Power` (`CuriousContraptions.Abstractions/ElementCategory.cs:9`), palette order 345 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)). Description: "Always warm inside. It makes electricity only while the plate on its top is kept cooler than its core." |

### Art and solidity

| Art node | Mesh and size | Class | Moved by |
| --- | --- | --- | --- |
| `Visual/Casing` | box 0.80 × 0.90 × 0.80 m, cream `#fff8e9` | solid (`col_casing`) | nothing |
| `Visual/ColdPlate` | box 0.60 × 0.04 × 0.60 m on the top, navy `#293954` | solid (`col_cold`) | nothing |
| `Visual/CoreBand` | band printed flat around the casing, terracotta `#a96f5c` | decorative: printed flat, recoloured by a binding | nothing (colour only) |
| `Visual/Lamp` | disc r 0.06 m printed flush on the +X face, slate `#556573`, gold while supplying | decorative: printed flat, recoloured by a binding | presentation binding (colour only) |
| `Visual/SupplyStud` | sphere r 0.075 m on the −X face, gold | solid (socket sphere) | nothing |

No fins are drawn on the module: cooling is a separate part stood on the plate, which is the puzzle. TH-10's 0.50 × 0.50 m foot sits wholly on the 0.60 × 0.60 m plate. Icon: a box with a warm band and a dark plate on top. It differs from the Battery by the top plate and band and by having no charge bar, and from TH-30 by its tall casing with no layered stack.

### Compiled counts

Bodies 1 (static). Colliders 3 (2 boxes, 1 sphere). Joints 0. Drives 0. Sensors 0. Thermal nodes 2; ambient links 1; faces 1; heat sources 1; converter rows 1. Supply nodes 2 (one source with a 2 J store, one contact). Signal nodes 1. Electrical nodes 2. Stores 2 (heat inventory, electrical buffer). Presentation bindings 6 (lamp R, G, B; band R, G, B).

## 3. Engine capabilities

Map row families (navigation, D14): ElectricalPower, FiniteLedger, RadioactiveDecay, SensibleHeat, ThermoelectricConversion. Probe made on `8d1d68aa`: **6 present, 1 untested, 2 narrower, 4 missing.** It uses the thermal rows of [TH-04 section 3](TH-04-electrical-heating-plate.md#3-engine-capabilities) and the generator row of TH-30 (N1 there); this table restates only what RAD-20 needs.

| # | Capability | Status | Symbol | Test opened |
| --- | --- | --- | --- | --- |
| R1 | Static scene body with colliders from `col_` nodes | present | `ISceneGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`) | `StateLatchTests.Compiler_DeclaresStaticLatchBodyWithCanonicalColliders` (`CuriousContraptions.tests/StateLatchTests.cs:503`) |
| R2 | Every visible solid has a collider | present | per-package row | `VisibleSolidColliderTests.EverySolidArtPartHasItsColliderAndNoColliderReachesBeyondTheArt` (`CuriousContraptions.tests/VisibleSolidColliderTests.cs:206`) |
| R3 | Typed ports and wrong-kind refusal | present | `PortDescriptor` (`CuriousContraptions.Abstractions/PortDescriptor.cs:55`) | `BundlePortsTests.AWrongDomainOrOutputToOutputLinkIsStillRefused` (`CuriousContraptions.tests/BundlePortsTests.cs:262`) |
| R4 | Proportional sharing of a source's per-tick allowance | present | `ElectricalSupply.Reserve` (`engine/gpu/ElectricalSupply.cs:11`) | `ElectricalSupplyTests.UnequalDemandSharesProportionallyAndFullLoadReceivesNothing` (`CuriousContraptions.tests/ElectricalSupplyTests.cs:207`) |
| R5 | Run, Reset, save and load of every admitted declaration | present | loop over `TestBundle.Packages` | `CatalogueIntegrationTests.EveryAdmittedDeclarationRetainsAuthoredStateAcrossRunResetSaveLoad` (`CuriousContraptions.tests/CatalogueIntegrationTests.cs:273`) |
| R6 | A package-internal source → signal-state contact → output path | present | `SignalStateContactControl` (`SignalContactControl.cs:12`) | `ComposableSupplyRoutingTests.PackageInternalSupplyEdgesAdmittedByValidationAndCompileToTopology` (`CuriousContraptions.tests/ComposableSupplyRoutingTests.cs:499`, fixture `:453`–`:466`) |
| U1 | A source's committed availability as a presentation driver | present, untested | `PresentationDriver.CommittedSupply` (`ElementPresentation.cs:23`), evaluated by `QuerySource` (`engine/gpu/WorkshopDeclaredPresentation.cs:492-502`: 1 while the source is available, else 0), used by the Battery (`BatteryElementDeclaration.cs:10-20`) | none opened (as TH-30 N4) |
| N1 | A supply source whose finite store has declared constant capacity and power limit, starts empty and is filled only by a credit | narrower: a store's four values are parameter-bound and only its initial charge fills it (`SupplySourceStoreDescriptor`, `CuriousContraptions.Abstractions/ElectricalContractValues.cs:31`; `SupplySourceDescriptor`, `SupplyContractValues.cs:39`) | — | `ElectricalSupplyTests.UnequalDemandSharesProportionallyAndFullLoadReceivesNothing` (R4). Owner **ENGINE-THERMAL** (T4's one form; TH-30 N2) |
| N2 | Presentation driver on a committed temperature | narrower: no temperature member (`ElementPresentation.cs:18`–`:37`) | `PresentationDriver` | `BothGateTests.ThreeNativeLampsFollowCommittedInputsAndSupplyThroughHideAndReset` (`CuriousContraptions.tests/BothGateTests.cs:259`). Owner **ENGINE-THERMAL** |
| M1 | Heat nodes with capacity and temperature, ambient links, and a contact link between declared faces of two static parts found once at Run through ENGINE-SPATIAL-QUERIES (pairs with a moving member through ENGINE-CONTACT-OBSERVATIONS) | **missing** (only a gas temperature exists: [Story ENGINE-THERMAL](../../../_bmad-output/planning-artifacts/epics.md#story-engine-thermal-heat-storage-transfer-and-phase-change), *Present today*) | — | owner **ENGINE-THERMAL** (T2, T3; TH-04 R9, R10) |
| M2 | A heat source paid from a declared finite inventory at a constant power | **missing**; covered by the story's "sources and sinks paid from a finite store" | — | owner **ENGINE-THERMAL** (T4) |
| M3 | The generator row: conduction between two nodes of which a bounded fraction is credited to a finite supply store, flows summing to zero each tick, the credit stopping when the store is full | **missing** | — | owner **ENGINE-THERMAL** (TH-30 N1, TH-12 K1) |
| M4 | A threshold node with a release ratio over a committed stored fraction, and the contact state it presents to `SignalStateContactControl` | **missing** (`SignalNodeKind`, `CuriousContraptions.Abstractions/SignalContractValues.cs:13`, has no threshold kind, and `SignalContactState` has only `Latched` and `TimerCounting`, `CuriousContraptions.Abstractions/SignalContactControl.cs:5`) | — | owner **ENGINE-SIGNAL-NODES** (TH-04 R14) |

ENGINE-RADIATION and ENGINE-OPTICAL are not prerequisites: the module has no emitter, receiver or row, and it is listed under Power. The cold plate is a contact face, not a port; there is no thermal port or connection domain.

**Dependencies.** TH-10 Finned heat sink and TH-30 (undelivered); Motor, Fan, Battery (delivered).

## 4. Sources and legacy

- [radiation-20](../requirements.md#radiation-20): "Cooling permits useful output; an equal-temperature control produces no thermoelectric output. Heat, electrical work, load and Reset state are accounted for." The requirement adds that it "must not be a battery with decorative fins".
- [Named entry](../invest/named-elements.md#radiation-20), owner S631; map composition "decay deposition feeds SensibleHeat and thermal ports before ThermoelectricConversion; cooling/rejection paths constrain output"; binding `radiation-01.json`.
- [S605](../invest/decisions.md#s605) decay row; [thermal research TH-S09](../../thermal-component-research.md#th-s09) ("a radioisotope generator is decay heat plus this same record"); research "Conversion" law (a cooling fan loop must start from a finite supply, never power itself); slots 106, 107.
- **Legacy.** None. Hits are coverage snapshots only, e.g. `reference/P0-022-before/docs/coverage/engine/capabilities-02.json@a6c914e:L15438-L15441`, plus the aggregate task lists — no element knowledge; do not carry forward.

**Files harvested** (all "no element knowledge"):
- `reference/P0-022-before/docs/coverage/engine/capabilities-02.json`
- `reference/P0-022-before/docs/coverage/engine/task-002.json`
- `reference/P0-022-before/docs/coverage/engine/task-003.json`
- `reference/P0-022-before/docs/coverage/engine/task-013.json`

## 5. Acceptance

### Behaviour and numbers

All values are starting values (D1, D5). x = T_h − 288 K, y = T_c − 288 K.

- **Why 80 W.** The output must be useful to a delivered load: the Motor at `torque` 1.0 N·m and `speed` 4 rad/s (both inside its ranges, `elements/Motor/MotorElementDeclaration.cs:41`–`:44`) asks at most 1.0 × 4 = 4 W while spinning up. With K = 0.4 W/K and 0.3 of the Carnot fraction, an 80 W source behind a TH-10 sink credits about 4.7 W (below), enough to run it continuously. The source is still small beside what the Motor can take (up to 2000 W, `MotorElementDeclaration.cs:126`).
- **Sink path.** TH-10 on the plate: 32 W/K contact pair, then TH-10's 5.5 W/K to ambient (`TH-10-finned-heat-sink.md` section 2): G_s = 1 ÷ (1/32 + 1/5.5) = 1 ÷ 0.2131 = 4.693 W/K.
- **Cooled steady state.** Core: 80 = 0.2x + Q_h. Converter: Q_h = 0.4(x − y). Cold: Q_h − E = 4.693y. With E = 4.74 W: x = 400 − 5Q_h and y = 0.2131Q_h − 1.010, so Q_h = 0.4(401.01 − 5.2131Q_h), giving 3.0852Q_h = 160.40 and **Q_h = 52.0 W**; x = 140.0, **T_h = 428.0 K**; y = 10.07, **T_c = 298.1 K**; Carnot fraction 1 − 298.1 ÷ 428.0 = 0.3035; **E = 0.3 × 0.3035 × 52.0 = 4.74 W**, which confirms the starting guess. The sink settles at 288 + 47.3 ÷ 5.5 = 296.6 K.
- **Balance when cooled.** 80 W in = 0.2 × 140.0 = 28.0 W casing loss + 4.74 W electrical + 47.3 W rejected through the sink (79.99 W).
- **Time.** Merging `cold` into the sink (they are joined at 32 W/K), the two slow nodes are `core` (3 J/K) and the sink (56.25 + 0.25 J/K). Their rate matrix [[−0.2, 0.1333], [0.00708, −0.1044]] has eigenvalues −0.095 and −0.209 per second, so the slow time constant is 10.5 s, set by the sink. E grows about as the square of the excess; it is within 3 % of steady once the slow mode has decayed to 1.4 %, at 4.25 × 10.5 = 45 s. Rows read the window 45 to 55 s; Run length 60 s.
- **Uncooled.** Nothing touches the plate, so `cold` follows `core` and Q_h tends to 0. The two nodes (3.25 J/K) warm toward 288 + 80 ÷ 0.2 = 688 K with time constant 3.25 ÷ 0.2 = 16.3 s. While warming, the plate lags because the converter must carry its warming flow: Q_h = 0.25 J/K × r, with r = 80 ÷ 3.25 × e^(−t/16.3) = 24.6 e^(−t/16.3) K/s, so Q_h ≤ 6.15 W, ΔT = Q_h ÷ 0.4 ≤ 15.4 K, and E = 0.3 × (ΔT ÷ T_h) × Q_h = 0.75 Q_h² ÷ T_h ≤ 0.75 × 37.8 ÷ 288 = 0.098 W, 2 % of the cooled output. Energy converted in the whole Run is at most 0.75 × 37.8 ÷ 288 × 16.3 ÷ 2 = 0.80 J, against 4.74 × 60 ≈ 280 J cooled. At most one 0.5 J burst ever reaches a load.
- **Equal temperatures.** At Run start every node is at 288 K: Q_h = 0 and E = 0.
- **Fan on the sink.** A delivered Fan's flow at TH-10's sample point multiplies its ambient conductance by (2.5 + u) ÷ 2.5 (T3); at u = 12 m/s that is 5.8 × 5.5 = 31.9 W/K, G_s = 1 ÷ (1/32 + 1/31.9) = 15.97 W/K. Then y = 0.0626Q_h − 0.311, 3.0251Q_h = 160.12, Q_h = 52.9 W, T_h = 423.3 K, T_c = 291.0 K, E = 0.3 × 0.3126 × 52.9 = 4.96 W, 4.6 % more. More cooling never lowers the output, and the gain is small because the converter, not the sink, limits the flow.
- **Load.** The store passes to `Supply` only what it holds, and the credit stops while the store is full, so the energy credited depends on the load. A delivered Fan in Electrical mode (consumer rating 480 W, `elements/Fan/FanElementDeclaration.cs:76`; as TH-30 row A1) takes each 0.4 J burst between the 0.5 J and 0.1 J levels of `available` within a few ticks (16 W limit, 0.13 J per tick), so the store never fills and the credit is the converter's full E: 4.74 W cooled. The energy and ledger rows use that Fan. The Motor set as above asks at most 4 W while spinning up and 0.05 × 4 = 0.2 W at speed against its bearing friction (`MotorElementDeclaration.cs:100`), under the drive-demand law of ENGINE-SUPPLY-DEMAND; 4.74 W covers both, so it reaches and keeps its set speed (row G10), after which the store fills and the credit falls to what the Motor takes; Q_h then sends the rest to the plate, raising T_c by under 0.1 K. With no load, the store fills to 2 J and the credit stops. Uncooled, at most 0.80 J is ever converted.
- **Steady source.** The 80 W source does not decay within a Run (owner decision of 21 November 2026, RAD-01; radiation convention 2); it debits its 24 000 J inventory by 80 J each second, so 4 800 J by 60 s and never more than 24 000 J in the 300 s maximum Run. Reset restores the inventory.
- **Tick order** (T8). The thermal step of tick n credits `core` from the inventory, moves Q_h and credits E to the store; temperatures, the store's charge and its fraction commit with tick n; `available` switches `outlet` in the signal network of tick n + 1, and the supply solve of tick n + 1 grants from the store's committed charge inside the substeps.

### Rows

Planned class `RadioisotopeGeneratorTests`. Fixture: the module with a TH-10 sink stood on its cold plate and a delivered Fan in Electrical mode on `Supply` (it takes every burst, so the credit is the converter's full E); row G10 puts the Motor in its place. Tolerances are the thermal conventions' T12: temperature 2 K or 5 % of the expected rise, whichever is larger; energy 2 % of the transfer or 5 J, whichever is larger; times 15 %; stall guard three times the hand time, at least 10 s.

| # | Given / When / Then | Test | Control | Boundary and tolerance |
| --- | --- | --- | --- | --- |
| G1 | Given the fixture, over the window 45 to 55 s the energy credited to the store is 47.4 J, the core is at 428 K and the plate at 298 K and the energy delivered to the Fan is that less the store's change (row: cooling permits useful output) | `RadioisotopeGeneratorTests.ACooledModuleDeliversPower` | G3 | energy ±5 J; core ±7 K (5 % of 140 K); plate ±2 K |
| G2 | Given the first tick of any Run, then the energy credited is 0 with every node at 288 K (row: equal temperatures give no output) | `RadioisotopeGeneratorTests.EqualTemperaturesGiveNoOutput` | G1 | under 0.001 J |
| G3 | Given no sink, then over 60 s the converted power never exceeds 0.15 W, the energy converted is under 2 J, and the core and plate are within 5 % of each other's rise at 60 s | `RadioisotopeGeneratorTests.AnUncooledModuleDeliversNothingUseful` | G1 | hand 0.098 W and 0.80 J |
| G4 | Given G1 over the window 45 to 55 s, then the inventory's debit (800 J) equals the casing loss + the energy credited to the store + the heat through the plate into the sink + the change of node enthalpy, and the store's credit equals the energy delivered to the Fan + the change of its charge; the ledger of T13 holds on every tick (row: heat, work and load are accounted for) | `RadioisotopeGeneratorTests.EnergyBalancesEveryTick` | — | 16 J (2 % of 800 J) |
| G5 | Given the Fan, it is granted on every tick no more than the store holds; given no load, nothing is delivered, the store holds 2 J and the credit stops, so the plate receives all of Q_h | `RadioisotopeGeneratorTests.TheLoadGetsOnlyWhatIsCredited` (theory) | G1 | store never above 2 J, ±0.02 J |
| G6 | Given a second, self-contained Fan blowing across the sink, then the energy credited over 45 to 55 s is not lower than G1's; at a committed flow of 12 m/s at the sink's sample point it is 49.6 J | `RadioisotopeGeneratorTests.MoreCoolingNeverLowersOutput` (the Fan paid from a Battery) | G1 | not lower; ±5 J |
| G7 | Given G1, then after 60 s the inventory holds 24 000 − 4 800 = 19 200 J and the credited power over 50 to 60 s is within T12 of that over 45 to 55 s (row context: not a battery; steady within a Run) | `RadioisotopeGeneratorTests.TheSourceDoesNotRunDown` | G3 | inventory ±5 J; energy ±5 J |
| G8 | Given `Supply` wired to another output or to a signal input, then the edit is refused | generic: `BundlePortsTests.AWrongDomainOrOutputToOutputLinkIsStillRefused` (R3) | — | typed refusal |
| G9 | Given a warm Run, when Reset, then every node is at 288 K, the inventory holds 24 000 J, the store is empty and the output is 0; a saved and reloaded construction behaves as G1 (row: Reset state) | `RadioisotopeGeneratorTests.ResetRestoresAuthoredState`; authored state by R5 | G1 | 2 K; exact authored store and inventory values |
| G10 | Given the Motor (`torque` 1.0 N·m, `speed` 4 rad/s) on `Supply` in place of the Fan, then it reaches its set speed and turns at 4 rad/s from 45 to 55 s; the store never exceeds 2 J and the energy delivered to the Motor never exceeds the energy credited (row: cooling permits useful output, a load the output runs continuously) | `RadioisotopeGeneratorTests.TheOutputRunsAMotorContinuously` | G3 (no sink: at most 0.80 J is ever converted) | speed ±10 % |

No parameter, so no parameter-refusal row. Chrome recipe (pending, post-catalogue): module, heat sink on its plate, Motor; Run and wait; then remove the sink.

## 6. Open questions

None open.

1. **Coefficients — decided** (D1, D5, D16; audit H-3, standing delegation): an 80 W steady source and the values above, so the output is useful to a delivered load (section 5, "Why 80 W"). The source is steady within a Run (owner decision of 21 November 2026) and paid from a finite inventory (coordinator decision 21 of 22 November 2026).
2. **One part or an assembly — decided** (D8): one part with its own catalogue entry. It declares TH-30's generator row with its own values; nothing is shared but the law.
3. **Thermal connection — decided** (coordinator decision 21; T3): a contact face on top of a static part, linked to a static sink stood on it, found once at Run by the spatial query; no thermal wire and no thermal port.
4. **Palette group — decided now**: Power. The module emits no radiation and its lesson is power from a temperature difference.
5. **Electrical output form — decided** (coordinator decision 21; T4): the converter credits a 2 J finite store that an ordinary source draws from, with availability by a threshold node. No store-less source.

Needs the owner: none.

## 7. Integration

- **Collaborators.** Motor, Fan, Battery (delivered); TH-10, TH-30 (undelivered). `Supply` is Electrical Output against a load's `PowerIn`. The cold plate's face (64 W/K) agrees with TH-10's foot face (64 W/K), pairing at 32 W/K; the pair is a static-on-static pair of the kind T3 lists ("sink foot on … a generator's cold plate").
- **Scenario and processes.** [thermal-scenario-11](../requirements.md#thermal-scenario-11); [IX-16](../requirements.md#interaction-16) (holds as written: the steady source is paid from its inventory), [IX-43](../requirements.md#interaction-43), [IX-34](../requirements.md#interaction-34), [IX-06](../requirements.md#interaction-06).
- **Campaign.** Chapter 14, levels 131 and 132, each declaring a 60 s Run length; reuse 136–150. No level in `content/puzzles.json` places it.
- **Coverage binding (planned, not yet live).** Blocked on the coverage follow-on story S7. Main's reader lists only `EL-*.md` specs and admits only an `EL-NNN` identity with an `element-NNN` anchor (`tools/Coverage/NamedPackageSources.cs:19`, `:57` on `origin/main`), so a `RAD-NN` identity cannot be bound until S7 widens it (owner decision 15).
- **Lesson fixtures.** Story [S8](../invest/delivery-batches.md#s8-lesson-fixtures-for-any-admitted-package) lands after the engine programme (owner decision 22).

## 8. Delivery

- **Slice shape.** Single element.
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), which includes ENGINE-SPATIAL-QUERIES, ENGINE-CONTACT-OBSERVATIONS, ENGINE-SUPPLY-DEMAND (the Motor's demand law) and ENGINE-SIGNAL-NODES; ENGINE-RUN-LENGTH (60 s); ENGINE-GAS-NETWORK, then ENGINE-LIQUID, then ENGINE-THERMAL as amended (owner decision 18); TH-10; TH-30 (it proves the generator row first); S7 for the binding. ENGINE-THERMAL is a real prerequisite here; ENGINE-RADIATION and ENGINE-OPTICAL are not.
- **Files.** `elements/RadioisotopeThermoelectricGenerator/RadioisotopeThermoelectricGenerator.csproj`, `RadioisotopeThermoelectricGeneratorElementDeclaration.cs`, `radioisotope_thermoelectric_generator.tscn`, `icon.svg`; one guarded line in `CuriousContraptions.slnx`; `CuriousContraptions.tests/RadioisotopeGeneratorTests.cs`; a row in `VisibleSolidColliderTests`. Registration is derived (`spec-registration-derivation.md`).
- **Review tier.** Data-only if the probe finds every row present after ENGINE-THERMAL and TH-30; otherwise the engine tier.
- **Disqualifiers (the slice's probe re-checks).** A generator row or thermal scale different from TH-30's as delivered (re-derive section 5 from it; never keep a second law); a heat source that cannot debit a declared inventory at constant power; a credited store whose values must be parameters (then they are declared parameters with locked defaults, as the delivered store requires); a static-on-static contact link not formed at Run (then the sink is not linked and the module is never cooled).

## 9. Engine conformance

Verdict: **Conforms** (every missing capability has exactly one owning element-free story).

1. **Declaration data only.** One static body, three colliders, one port, two heat nodes, one ambient link, one face, one heat source with its inventory, one converter row, one source with a 2 J store, one contact, one threshold node, six bindings. No code names the module.
2. **Generic laws.** Heat storage and exchange (T9); a store-paid heat source; TH-30's generator row; supply through a signal-state contact. "Cooling makes power" is a temperature difference across a declared converter.
3. **Three layers.** Colliders: three primitives. Visuals: the art table; band and lamp are printed. Animation: lamp and band colour bindings on committed state.
4. **One-way flow and typing.** Thermal step → committed temperatures and store charge → next tick's signal network and supply; presentation reads committed state. Values are f32.
5. **Flat state.** The counts in section 2 are fixed at compile time.
6. **Performance fitness.** Static body; three primitives; two heat nodes, one converter, one source and one link-table pair; constant work per tick; no query per tick and no sensor (the static pair is found once at Run).
7. **Tests are scenarios.** Rows G1 to G10 run the production simulation through placement, wiring, Run and committed reads.
