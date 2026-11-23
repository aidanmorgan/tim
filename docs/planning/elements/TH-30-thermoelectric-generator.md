# TH-30 · Thermoelectric generator — named-identity readiness spec

**Current delivery (24 October 2026):** [TH-30 delivery slice](../invest/named-elements.md#thermal-30). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Rewritten on 22 November 2026 as one current text against checkout `spec-drafts` (published main `8d1d68aa`), and repaired the same day against the independent audit `docs/planning/elements/readiness/audits/elements-batch-n.md` (section 3, owner item 6) and owner decisions 13 and 17 and coordinator decision 21 of 22 November 2026; repaired again on 23 November 2026 against the second audit `docs/planning/elements/readiness/audits/reaudit-group-3.md` (A1, A2, A5 and the charge binding) and coordinator decision 27. Code citations are `path:line` in that checkout; `path@a6c914e` citations appear only in section 4. Nothing was built or run: every number is a hand calculation shown here, the transient figures of section 5 are iterated tick by tick at 120 Hz with the exact step of T9 (source credits before links; links in the order contact, converter, ambient; the supply grant of a tick from the charge committed by the tick before), and each is a starting value under decision D1 of [delivery batches](../invest/delivery-batches.md#3-family-level-decisions). Thermal units, laws, scale, tick phase and tolerances are the [thermal conventions](TH-04-electrical-heating-plate.md#thermal-conventions) (T1 to T14); shared probe rows are cited as "TH-04 R*n*" and the converter rows as "[TH-12](TH-12-reversible-heat-pump.md#capability-probe) K*n*".

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name / type | TH-30 · Thermoelectric generator · Thermal |
| Anchors | [requirements.md#thermal-30](../requirements.md#thermal-30) (task `sequence-task-464`); [named-elements.md#thermal-30](../invest/named-elements.md#thermal-30) |
| Owner key / research | S592 · [TH-S09 thermoelectric conversion](../../thermal-component-research.md#th-s09) |
| Related identities | [TH-12](TH-12-reversible-heat-pump.md) (lead of the converters); hot sources [TH-04](TH-04-electrical-heating-plate.md) and [TH-26](TH-26-thermal-storage-block.md); cold side [TH-10](TH-10-finned-heat-sink.md) or a delivered Fan; loads the delivered Fan (Electrical mode) and [CAT-042 Motor](CAT-042-motor.md) at a low setting; [RAD-20](RAD-20-radioisotope-thermoelectric-generator.md) reuses the same converter row with a decay-heat source. No CAT refines it. |
| Campaign | Intro 87; practice 88; reuse 131, 145 ([thermal allocation](../requirements.md#thermal-campaign-allocation)) |
| Status | Not started. Blocked on the engine programme (owner decision 3a, 22 November 2026), then on ENGINE-THERMAL (converter row and the credit into a supply store, T4), ENGINE-SIGNAL-NODES (threshold node), ENGINE-SUPPLY-DEMAND (the Motor's drive demand) and ENGINE-RUN-LENGTH. |

### Disposition

**To deliver as a single element after TH-12.** Planned package id `thermoelectric_generator`, directory `elements/ThermoelectricGenerator/`, display name "Thermoelectric generator". RTG use is RAD-20's package declaring the same converter row; it is not a mode of this part.

## 2. Declaration

Follows the delivered Battery for the supply side (one source node with a finite store bound to a `Supply` output, `elements/Battery/BatteryElementDeclaration.cs:70-74`), the package-internal source → signal-state contact path of `ComposableSupplyRoutingTests` (`CuriousContraptions.tests/ComposableSupplyRoutingTests.cs:457-466`), and TH-12 for the two-plate static body.

- **Interfaces.** `ISupplyElement` (`CuriousContraptions.Abstractions.Electrical/ElectricalContracts.cs:11`), `ISignalElement` (`CuriousContraptions.Abstractions.Signal/SignalContracts.cs:8`), `ISceneGeometryElement`, and ENGINE-THERMAL's node and converter contracts. `KinematicMode.Static`.
- **Body.** One static body; origin at the centre of its underside. The hot plate is the bottom, the cold plate the top.
- **Colliders (metres).**

  | Collider | Shape | Centre | Half extents | Purpose |
  | --- | --- | --- | --- | --- |
  | `col_hot` | box | (0, 0.04, 0) | (0.20, 0.04, 0.20) | hot plate; thermal face of `hot` |
  | `col_stack` | box | (0, 0.15, 0) | (0.18, 0.07, 0.18) | layered module; no thermal face |
  | `col_cold` | box | (0, 0.26, 0) | (0.32, 0.04, 0.20) | cold plate with two wings; thermal face of `cold` |

- **Material.** Friction 0.6, restitution 0.05, rolling resistance 0.
- **Thermal nodes** (T2). `hot`: 45 J/K, ambient conductance 0.1 W/K, face `col_hot`, 64 W/K. `cold`: 45 J/K, ambient conductance 1.0 W/K (the wings: 2.5 × 0.4 m²), face `col_cold`, 64 W/K; one forced-air sample point at (0, 0.31, 0), excluding this body's colliders (T3).
- **Converter** (T4, the one form of coordinator decision 21). One generator row from `hot` to `cold` crediting the store of `source`: heat through the module Q_h = K·(T_h − T_c), K = 0.5 W/K; electrical energy converted in a tick E = min(0.3·(1 − T_c ÷ T_h)·Q_h·dt, the store's free capacity) while T_h > T_c, otherwise 0; the hot node is debited Q_h·dt, the cold node credited Q_h·dt − E, and E is credited to the store. Nothing else fills the store.
- **Supply.** Source node `source`: an ordinary finite store, capacity 2 J, power limit 16 W, initial charge 0, always enabled (declared constants of the credit form, ENGINE-THERMAL; 2 J holds far more than one tick's credit at the slowest 60 Hz cadence, as coordinator decision 32 requires: the largest converted power in any row is at A5's 500 K boundary, about 0.3 × (1 − 356 ÷ 493) × 68 W = 5.7 W at the start, 0.095 J per 1/60 s tick (A1's 2.18 W is 0.036 J); today a store's four values are parameter-bound, `CuriousContraptions.Abstractions/ElectricalContractValues.cs:31`). Contact node `outlet`: `SupplyContactDescriptor` with `SignalStateContactControl` on the signal node `available` (`CuriousContraptions.Abstractions/SignalContactControl.cs:12`); the control reads the state `Asserted`, the one state a threshold node presents (TH-04 R14, ENGINE-SIGNAL-NODES "Contacts"): the contact is closed while `available` is asserted and open once it has released (today `SignalContactState` has only `Latched` and `TimerCounting`, `SignalContactControl.cs:5`). Edges `source` Output → `outlet` PowerIn; `outlet` Output → `Supply` port. Electrical nodes: `source` `ElectricalRole.Source`, 12 V, maximum 16 W; `outlet` `ElectricalRole.Switch`.
- **Signal node** `available`: a threshold node over the committed stored fraction of `source` (ENGINE-SIGNAL-NODES, TH-04 R14): on at 0.25 (0.5 J), release ratio 0.2 (off below 0.05, 0.1 J). It makes the load run in short bursts rather than flicker on every tick's credit; the energy delivered is the energy credited either way.
- **Port.** `PortId.Supply` (Electrical, Output, `PortRole.Supply`) at (0.18, 0.15, 0) facing +X.
- **Parameters.** None (D13).
- **Presentation bindings** (the Battery's forms, `elements/Battery/BatteryElementDeclaration.cs:10-20`). `Visual/Lamp` albedo red, green and blue, slate `#556573` → gold `#f7cb52`, ← `PresentationDriver.CommittedSupply` with `SupplyNode: source` (1 while the source is available, else 0). `Visual/Charge_0`, `Visual/Charge_1`, `Visual/Charge_2` albedo red, green and blue, slate → gold, ← `PresentationDriver.CommittedCharge` with `SupplyNode: source` and `Threshold` 0.25, 0.5 and 1.0 (each mark lights while the stored charge is at least that fraction of the 2 J capacity). Both drivers read a supply source and return 0 or 1 (`engine/gpu/WorkshopDeclaredPresentation.cs:492-502`); the bundle admits them on a supply source node (`CuriousContraptions.Bundle/SupplyBundleValidation.cs:183-185`). `CommittedStoredFraction` is not used: the bundle admits it only on a mechanical `EnergyStore` (`CuriousContraptions.Bundle/BundleValidator.cs:310-312`), which this part does not declare.
- **Palette.** Category `ElementCategory.Power`. Order: 339 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)).

### Art and solidity

| Art node | Mesh | Class | Moved by |
| --- | --- | --- | --- |
| `Visual/Hot` gold `#f7cb52` | box 0.40 × 0.08 × 0.40 m | solid, `col_hot` | nothing |
| `Visual/Stack` alternating cream `#fff8e9` and gold layers, printed | box 0.36 × 0.14 × 0.36 m | solid, `col_stack` | nothing |
| `Visual/Cold` cream, fin ribs printed flush on the two wings | box 0.64 × 0.08 × 0.40 m | solid, `col_cold` | nothing |
| `Visual/Lamp` slate `#556573`, gold when supplying | a disc printed flush on the stack's front | decorative: printed flat, recoloured by a binding | presentation binding |
| `Visual/Charge_0` to `Visual/Charge_2` slate, gold when lit | three marks printed flush in a row beside the lamp | decorative: printed flat, recoloured by bindings | presentation bindings |
| `Supply` socket | shared socket gizmo on the stack's +X face | decorative: shared socket art | nothing |

The fins are printed ribs on a solid wing, so they need no colliders of their own. Icon: a layered block under a wide ribbed plate with a lightning shape. It differs from the heat pump by the wide winged top plate and the layered stack.

### Compiled counts

Bodies 1 (static); colliders 3 (box); joints 0; sensors 0; thermal nodes 2; forced-air samples 1; converter rows 1; supply nodes 2 (one source with a 2 J store, one contact); signal nodes 1; electrical nodes 2; ports 1; parameters 0; presentation bindings 12 (the lamp and three marks, each by red, green and blue albedo, as the Battery).

## 3. Engine capabilities

### Capability probe

Uses TH-04 R1 to R5, R9 to R13 and R14; TH-12 K1 (the converter family's row). Its own rows:

| # | Capability | State | Symbol | Exercising test |
| --- | --- | --- | --- | --- |
| N1 | A generator row: conduction between two nodes of which a bounded fraction is credited to a finite supply store, the flows summing to zero each tick and the credit stopping when the store is full | **missing** | none | none; owner ENGINE-THERMAL |
| N2 | A supply source whose finite store has declared constant capacity and power limit, starts empty and is filled only by a credit | **narrower**: a source's store exists but its four values are parameter-bound and only its initial charge fills it (`SupplySourceStoreDescriptor`, `CuriousContraptions.Abstractions/ElectricalContractValues.cs:31`) | `SupplySourceDescriptor`, `CuriousContraptions.Abstractions/SupplyContractValues.cs:39` | `ElectricalSupplyTests.UnavailableSourcePreservesBothStores` (TH-04 R4); owner ENGINE-THERMAL (the credited store, coordinator decision 26) |
| N3 | A package-internal source → signal-state contact → output path | present | `SignalStateContactControl`, `CuriousContraptions.Abstractions/SignalContactControl.cs:12` | `ComposableSupplyRoutingTests.SimulationAdvancesPowerAcrossPackageInternalEdgesToRechargeStore` (`CuriousContraptions.tests/ComposableSupplyRoutingTests.cs:530`, body read: once the contact's signal latches, power crosses the package-internal source → contact → consumer path and the target store is recharged); topology in `PackageInternalSupplyEdgesAdmittedByValidationAndCompileToTopology` (`:499`, read); fixture at `:457-466` |
| N4 | A source's committed availability and stored-charge marks as presentation drivers | present, test not opened | `PresentationDriver.CommittedSupply`, `CommittedCharge` (`CuriousContraptions.Abstractions/ElementPresentation.cs:23`), evaluated by `QuerySource` (`engine/gpu/WorkshopDeclaredPresentation.cs:492-502`: availability 1 or 0; charge 1 while the remaining charge is at least the declared threshold × capacity, and a refusal if the capacity is not resolved, `:500`, which N2's constant capacity supplies), used by the Battery (`elements/Battery/BatteryElementDeclaration.cs:10-20`) | not opened |

Counts: present 6 (TH-04 R1 to R5; N3), untested 1 (N4), narrower 2 (TH-04 R14; N2), missing 7 (TH-04 R9 to R13; K1 with N1 counted once as this part's converter row; the threshold over a stored fraction is R14's). Owners, one each: N1, N2 and the TH-04 thermal rows **ENGINE-THERMAL** (T4's one form; the earlier "store-less source of ENGINE-OPTICAL" is withdrawn, coordinator decision 21); R14 **ENGINE-SIGNAL-NODES**; the Motor's demand law **ENGINE-SUPPLY-DEMAND**. Nothing here needs code that names this part.

## 4. Sources and legacy

- **Requirement row** [thermal-30](../requirements.md#thermal-30): a generic hot/cold converter produces bounded supplied electrical output from heat flow and temperature difference; RTG assemblies use the same converter with decay-generated heat; no variants.
- **Named entry** [thermal-30](../invest/named-elements.md#thermal-30): owner S592; "Equal temperatures produce no thermoelectric work; load and finite cold-side rejection affect output".
- **Research** [TH-S09](../../thermal-component-research.md#th-s09) and per-element table: "two thermal ports → electrical source"; parameter coefficient; meter ← power.
- **Element map** row TH-30: ElectricalPower, FiniteLedger, SensibleHeat, ThermoelectricConversion; composition note: two thermal ports and finite rejected heat constrain ElectricalPower. Navigation only (D14).
- **Processes and scenarios.** [IX-34](../requirements.md#interaction-34), [IX-06](../requirements.md#interaction-06), [IX-18](../requirements.md#interaction-18); [TX-11](../requirements.md#thermal-scenario-11).
- **Legacy.** None found. Searched `parts/`, `engine/physics/`, `engine/*.cs`, `engine/bridge/`, `CuriousContraptions.tests/`, `content/puzzles.json`, `tools/Campaign` and `reference/` at a6c914e for thermoelectric, Seebeck, RTG and generator terms.

## 5. Acceptance

Point of truth: [thermal-30](../requirements.md#thermal-30), [TX-11](../requirements.md#thermal-scenario-11) and [ELEMENT acceptance](../requirements.md#accept-element). Chrome proof is pending the post-catalogue phase.

### Behaviour and numbers

- **Law and bound.** E ≤ 0.3 × the Carnot fraction × Q_h·dt, so the output is always less than a third of the heat passing and the cold node's credit is never negative. Delivered electrical energy ≤ energy credited to the store ≤ that bound; the difference is what the store holds (at most 2 J).
- **On a Heating plate** (hot plate on the disc, 32 W/K; Battery `capacity` 14 400 J), steady state if the plate ran long enough: treating disc and hot plate as one node (32 W/K pair) with 0.6 W/K to the room, and the cold plate rejecting only to the room, T_c − 288 = 0.5·(T_h − T_c), so T_c − 288 = (T_h − 288) ÷ 3 and Q_h = (T_h − 288) ÷ 3; 120 = 0.6·x + x ÷ 3 = 0.933·x gives x = 128.6 K: T_h = 416.6 K, T_c = 330.9 K, Q_h = 42.9 W, P = 0.3 × (1 − 330.9 ÷ 416.6) × 42.9 = 0.3 × 0.2057 × 42.9 = 2.65 W, 2.2% of the heater's input: it cannot feed itself.
- **At 120 s** (the 14 400 J Battery is then empty), evaluated tick by tick with T9: T_h 391.9 K, T_c 316.4 K, Q_h = 0.5 × 75.5 = 37.8 W, P = 0.3 × (1 − 316.4 ÷ 391.9) × 37.8 = 0.3 × 0.1927 × 37.8 = 2.18 W; energy converted by 120 s **154 J**; delivered power averaged over 110 to 120 s 2.13 W (the store cycles between the threshold node's 0.1 J and 0.5 J, so the delivered average is the converted power less the change in what the store holds). The store first reaches 0.5 J at 10.2 s. Hand check: disc and hot plate warm as one 60.25 J/K node losing 0.6 W/K to the room and about x ÷ 3 through the module, a time constant of 60.25 ÷ 0.933 = 64.6 s, so T_h ≈ 288 + 128.6 × (1 − e^(−120 ÷ 64.6)) = 396.6 K at 120 s, against 391.9 K by tick. The earlier lumped estimate (64 J, one 105 J/K node with the cold plate riding at a third of the rise) understated the energy: the cold plate has its own 45 ÷ 1.5 = 30 s time constant and lags the hot one, so the difference across the module, and with it Q_h and the Carnot fraction, is larger through the first minute than the steady third (at 60 s the tick gives T_h 362.1 K against T_c 303.0 K).
- **Cold-side rejection.** A delivered Fan blowing across the cold plate (12 m/s, `elements/Fan/FanElementDeclaration.cs:48`) multiplies its ambient conductance by (2.5 + 12) ÷ 2.5 = 5.8 (T3). Tick by tick at 120 s: T_h 386.3 K, T_c 295.1 K, Q_h 45.6 W, P = 0.3 × (1 − 295.1 ÷ 386.3) × 45.6 = 3.22 W; energy converted by 120 s 210 J, 1.36 times still air. The energy ratio is below the 1.48 ratio of the powers at 120 s because in still air the lagging cold plate already sits near ambient through the first minute, when much of the energy is made.
- **Stored heat (TX-11).** A TH-26 block authored with `initial_temperature` 400 K (TH-26 admits 288 to 500 K; its default 288 K delivers nothing) under the hot plate (12.8 W/K pair). Quasi-steady at the start, with x = T_h − 288 and the block 112 K above the room: cold plate y = x ÷ 3, Q_h = x ÷ 3; hot balance 12.8 × (112 − x) = 0.1·x + x ÷ 3, so x = 1433.6 ÷ 13.233 = 108.3 K: T_h = 396.3 K, T_c = 324.1 K, Q_h = 36.1 W, P = 0.3 × (1 − 324.1 ÷ 396.3) × 36.1 = 1.97 W. **Fade:** the block also loses 3.2 W/K × 112 K = 358 W to the room (TH-26's own ambient conductance), so it falls at (358 + 47) ÷ 420 = 0.96 K/s with time constant 420 ÷ (3.2 + 0.42) = 116 s. Output scales with the square of the block's excess: P ≈ 1.97 × e^(−2t ÷ 116) W, so 1.26 W at 30 s, 0.30 W at 120 s, 0.03 W at 240 s, and ∫ = 1.97 × 58 × (1 − e^(−4.14)) = 112 J by 240 s. Evaluated tick by tick with T9 (block 420 J/K, 3.2 W/K to the room, the 12.8 W/K pair): the store first reaches 0.5 J at 1.7 s; at 30 s T_h 366.8 K, T_c 304.1 K, block 367.7 K; at 120 s T_h 327.3 K, T_c 303.9 K; energy converted 61.7 J by 30 s, 122 J by 120 s and **136 J by 240 s**, the delivered power averaged over 220 to 240 s 0.050 W. The quasi-steady estimate is low for the same reason as on the plate: the lagging cold plate widens the difference early. A block at 500 K (TH-26's maximum) converts 423 J by 240 s; a block at 288 K converts nothing. With a TH-10 sink on the cold plate (5.5 W/K to the room behind a 32 W/K pair: 4.69 W/K, plus the plate's own 1.0 W/K): y = 0.5·x ÷ 6.19 = 0.0808·x, Q_h = 0.4596·x, 1433.6 = 13.36·x, x = 107.3 K: T_h = 395.3 K, T_c = 296.7 K, Q_h = 49.3 W, P = 0.3 × 0.2494 × 49.3 = 3.69 W at the start. The 16 W power limit never binds.
- **Loads.** Delivered power cannot exceed what the store holds, so a heavy load (the Fan in Electrical mode, consumer rating 480 W, `elements/Fan/FanElementDeclaration.cs:76`) takes each 0.4 J burst within a few ticks (16 W limit, 0.133 J per tick, coordinator decision 27) and then waits: the Fan barely moves, but the energy it receives is measurable and equals the energy converted less what the store holds, because its demand always exceeds the store. That makes the Fan the measuring load of A1, A2 and A5. A light load does not empty the store. The Motor at a low setting, `torque` 0.2 N·m (minimum 0.1) and `speed` 4 rad/s (`elements/Motor/MotorElementDeclaration.cs:41-44`), has a rotor of 0.5 kg and radius 0.3 m, 0.5 × 0.3² ÷ 2 = 0.0225 kg·m² about its axis (`:18-19`, `:105-110`), and 0.05 N·m bearing friction (`:100`). Under the drive-demand law of ENGINE-SUPPLY-DEMAND and decision 27's per-tick allowance it asks at most 0.2 × 4 = 0.8 W while spinning up (4 × 0.0225 ÷ 0.15 = 0.6 s) and 0.05 × 4 = 0.2 W at speed. On the 400 K block the store reaches 0.5 J at 1.7 s, the contact closes, and it stays closed while the output exceeds 0.2 W: by hand the Motor turns (60 − 1.7 − 0.3) × 4 ÷ 2π = 36.9 revolutions by 60 s, and tick by tick 36.9. The store then fills to 2 J and the credit stops (A3's law), so the energy converted with the Motor (38.9 J by 240 s, 38.6 J delivered) is set by the load, not by the heat: that is why the energy row uses the Fan and the rotation row the Motor. Decided now (audit owner item 6, standing delegation): rows assert delivered power and energy with the Fan; only A9 asserts rotation, with that low setting. The converter's conductance is not raised: 3.7 W on the best stored-heat layout is already a bounded, readable output, and a larger K would make the part a heater bypass.
- **With nothing connected** the store fills to 2 J and the credit stops; all of Q_h then reaches the cold plate.
- **Reversed difference.** T_c above T_h gives no output (decided, section 6).
- **Tick order.** The thermal step of tick N (T8) passes Q_h and credits E to the store; the store's charge and the stored fraction commit with tick N; the threshold node over that fraction enters the signal network with tick N's observations and switches `outlet` from tick N + 1 (D7); the supply solve of tick N + 1 grants from the store's committed charge, inside the substeps. Nothing is granted that the store does not hold, so the ledger closes without a same-tick correction.
- **Run length these rows need: 240 s** (A5 observes energy to 240 s; A1 to A4 run 120 s; A9 runs 60 s; ENGINE-RUN-LENGTH, owner decisions 13 and 17; default 30 s, maximum 300 s).
- **Tolerances** (T12): power and energy 25% on transient values (the tick-by-tick values above; the band covers the link order and grant timing that the slice's compiled order fixes, not a fit to observed output), 2 K; ledger 2% of the transfer or 5 J; the bound itself to 0.02 W.

### Rows

Planned class `ThermoelectricGeneratorTests` in `CuriousContraptions.tests`.

| # | Given / When / Then | Planned test | Control | Boundary | Tolerance |
| --- | --- | --- | --- | --- | --- |
| A1 | Given the generator on a wired Heating plate (Battery `capacity` 14 400 J) with `Supply` → a Fan in Electrical mode, when 120 s pass, then the energy converted by 120 s (delivered to the Fan plus what the store holds) is 154 J, and the delivered power averaged over 110 to 120 s is 2.13 W | `AHeatFlowAcrossThePlatesPowersALoad` | no heat source: both plates at 288 K, delivered energy 0 | at every committed tick the energy credited is at most 0.3 × (1 − T_c ÷ T_h) × 0.5 × (T_h − T_c) × dt from the committed temperatures, and the delivered total never exceeds the credited total | energy 116 to 193 J; power 1.6 to 2.7 W; bound within 0.02 W |
| A2 | Given A1 with a second, self-contained Fan blowing across the cold plate, then the energy converted by 120 s is at least 1.25 times A1's | `ABetterCooledColdPlateDeliversMore` | A1 | none | ratio bound (tick ratio 1.36) |
| A3 | Given A1 with nothing on `Supply`, then nothing is delivered, the store holds 2 J and, once it is full, nothing more is credited | `AnUnloadedGeneratorStopsCreditingWhenItsStoreIsFull` | A1 | the store never exceeds 2 J | 0.02 J |
| A4 | Given A1, then the plate Battery's debit equals the enthalpy gained by disc and both plates plus the ambient account plus the electrical energy credited (delivered plus held) | `ElectricalOutputIsTakenFromTheHeatFlow` | none | none | 2% of the debit |
| A5 | Given a TH-26 block authored at `initial_temperature` 400 K under the hot plate and a Fan in Electrical mode on `Supply`, when 240 s pass, then the energy converted by 240 s (delivered plus held) is 136 J and the delivered power averaged over 220 to 240 s is under 0.1 W | `StoredHeatPowersALoadUntilTheStoreIsSpent` (TX-11; run in the TH-26 slice) | a block at 288 K: nothing delivered | `initial_temperature` 500 K (TH-26's maximum): more energy than at 400 K (tick 423 J) | energy 102 to 170 J; late power one-sided (tick 0.050 W) |
| A6 | Given A1 then Reset, then both plates are at 288 K, the store is empty and no power is delivered | `ResetRestoresBothPlatesAndAnEmptyStore` | none | none | 0.01 K; store exactly 0 |
| A7 | Given a placed, wired generator, when saved and loaded, then its pose and connection are kept and no run temperature or store charge is saved | `SaveAndLoadKeepTheConnection` | none | none | exact |
| A8 | `Supply` wired to another supply output, or to an activation input, is refused | generic: `BundlePortsTests.AWrongDomainOrOutputToOutputLinkIsStillRefused` (TH-04 R5) | — | — | typed refusal |
| A9 | Given A5's block and generator with the Motor at `torque` 0.2 N·m, `speed` 4 rad/s on `Supply` instead of the Fan, when 60 s pass, then the Motor has turned at least 10 revolutions, and at every committed tick the electrical energy delivered never exceeds the energy credited | `StoredHeatTurnsALightLoad` (TX-11; run in the TH-26 slice) | a block at 288 K: the Motor never turns; Motor disconnected: no electrical energy leaves | none | revolutions one-sided (hand and tick 36.9); ledger 0.02 J |

No parameters, so no parameter-refusal row. Requirement clauses: equal temperatures (A1's control); load (A3 against A1, A5, A9); finite cold-side rejection (A2); bounded output (A1's boundary); the RTG's reuse is RAD-20's rows. The A9 revolution count rests on the Motor's drive-demand law of ENGINE-SUPPLY-DEMAND (disqualifier below). Each control can fail: A1's and A5's on a converter that credits without a difference, A2's on forced air that does not reach the cold plate's ambient term (ratio 1), A9's on a Motor run from a store that was never credited.

## 6. Open questions

Resolved:

1. **Game scale.** D5 as restated in the thermal conventions.
2. **Converter law.** A fraction (0.3) of Carnot on the conducted flow, three declared numbers (K, fraction, store), no voltage or internal-resistance model, which the supply network does not use (D16).
3. **How heat becomes supply.** Only by T4's one form (coordinator decision 21): a credit into a finite 2 J store that an ordinary source draws from, availability by a threshold node over its stored fraction. The earlier store-less source is withdrawn. Why 2 J: it holds about one second of the best output, so a load sees bursts of at most 0.4 J and nothing large is banked.
4. **Polarity.** A reversed difference gives no output (D19): no lesson uses reverse generation, and a signed source has no meaning on the supply network.
5. **RTG sharing.** RAD-20 declares its own nodes and its own values for the same converter row, with its decay heat as the source; its cold side is a contact face (RAD-20 states "no thermal wire and no thermal port"). This spec's `Supply` port id is the one RAD-20 uses.
6. **Layout.** Hot plate at the bottom, cold plate on top with printed-fin wings, decided now: it stands on a source and takes a static sink on top (a static-on-static pair in T3's list).
7. **Load (audit owner item 6).** Decided under the standing delegation (reason in section 5, "Loads"): energy and power rows with a Fan as a measuring load, and the low-setting Motor for the one rotation row.

Needs the owner: none.

## 7. Integration

- **Works with.** Heating plate, TH-26 (hot side); Fan (delivered), TH-10 (cold side); Fan, Motor (loads, delivered); RAD-20.
- **Campaign.** Introduction 87, practice 88; reuse 131, 145. The levels declare a Run length of at least 120 s (240 s for the stored-heat lesson) and set the Motor to its low setting. A fixed placement needs the lesson-fixture story ([S8](../invest/delivery-batches.md#s8-lesson-fixtures-for-any-admitted-package)), which lands after the engine programme (owner decision 22).
- **Coverage.** Section 10.

## 8. Delivery

- **Slice shape.** Single element, after TH-12.
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), which comes first: the programme (ENGINE-SPATIAL-QUERIES and ENGINE-CONTACT-OBSERVATIONS for the touch pairs; ENGINE-SUPPLY-DEMAND for the drive demand); ENGINE-SIGNAL-NODES (threshold node); ENGINE-GAS-NETWORK, ENGINE-LIQUID, then ENGINE-THERMAL as amended (owner decision 18; N1, N2, K1); ENGINE-RUN-LENGTH; TH-04; TH-12; S7; S3 preferred. A5 waits for TH-26.
- **Files.** `elements/ThermoelectricGenerator/ThermoelectricGenerator.csproj`, `ThermoelectricGeneratorElementDeclaration.cs`, `thermoelectric_generator.tscn`, `icon.svg`; one guarded line in `CuriousContraptions.slnx`; `CuriousContraptions.tests/ThermoelectricGeneratorTests.cs`; a row in `VisibleSolidColliderTests`; this spec's binding section once S7 allows it. Registration is derived.
- **Review tier.** Data-only if N1, N2 and R14 are present and tested when the slice starts; otherwise the engine tier.
- **Disqualifiers (the slice's probe re-checks).** A converter that credits supply in any other form than a finite store; a generator law fixed in code; a store whose capacity must be a player parameter (then three build-only parameters with single admitted values are needed, or ENGINE-THERMAL's credit form is incomplete); forced-air exchange not delivered (A2 then uses TH-10); a Motor whose demand is its torque × speed limit rather than its committed drive work (A9 then needs a lighter load).

## 9. Engine conformance (22 November 2026)

1. **Declaration data only.** Three colliders, two nodes, one generator row, one finite source store, one contact, one threshold node, one port, twelve bindings (a lamp and three charge marks by red, green and blue).
2. **Generic laws.** Q̇ = G·ΔT; a bounded fraction of a conducted flow credited to a finite store; an ordinary source; a threshold node; proportional grant to consumers. "No difference, no power" is the law at ΔT = 0.
3. **Three layers.** Colliders: two plates and the stack. Visuals: their art, printed ribs, a printed lamp and three printed charge marks. Animation: the lamp and the marks from the committed availability and charge of the source. Nothing is written back.
4. **Flow and typing.** One Electrical output; f32 values; enum roles; no parameters.
5. **Flat state.** The counts in section 2.
6. **Performance.** Static; three primitive colliders; per tick two nodes, one row, one store and the links at the two plates (T10).
7. **Tests are scenarios.** A1 to A7 and A9 run the production simulation.

**Verdict: Conforms; blocked on ENGINE-THERMAL (converter row and store credit) and ENGINE-SIGNAL-NODES, general capabilities stated element-free, and on the engine programme (owner decision 3a).**

## 10. Coverage binding (planned, not yet live)

Published main binds a named-identity package through a final section of its spec that `tools/Coverage/NamedPackageSources.cs` reads (pattern: EL-239 Baseball). That section is not written here. **Blocked on the coverage follow-on story S7:** main's reader lists only `EL-*.md` files and admits only an `EL-NNN` identity with an `element-NNN` anchor, so no thermal identity can be bound until S7 widens it.

| Field | Planned value |
| --- | --- |
| Package | `thermoelectric_generator` |
| Named identity | TH-30, `../invest/named-elements.md#thermal-30` |
| Acceptance | none resolvable today; the row's own task is `sequence-task-464` and the ledger entry has no todo link |
| Decision | S592; `scope-corrections.md` on published main has no `s592` block |
| Mode records | `Configuration=Fixed` |
