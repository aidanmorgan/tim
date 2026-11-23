# TH-17 · Kettle — named-identity readiness spec

**Current delivery (24 October 2026):** [TH-17 delivery slice](../invest/named-elements.md#thermal-17). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Rewritten on 22 November 2026 as one current text against checkout `spec-drafts` (published main `8d1d68aa`), and repaired the same day against the audit `docs/planning/elements/readiness/audits/elements-batch-n.md` (E1, C2, N1, N6) and owner decisions 17 to 20 and coordinator decisions 16 and 21, and on 23 November 2026 against the second audit `docs/planning/elements/readiness/audits/reaudit-group-3.md` (relief throat, condense rate, F4 vent). Code citations are `path:line` in that checkout; `path@a6c914e` citations appear only in section 4. Nothing was built or run: every number is a hand calculation shown here and a starting value under decision D1 of [delivery batches](../invest/delivery-batches.md#3-family-level-decisions). Thermal conventions T1 to T14 and probe rows R1 to R14 are owned by [TH-04](TH-04-electrical-heating-plate.md#thermal-conventions); liquid conventions L1 to L18 by [EL-001](EL-001-finite-reservoir.md) (listed in [Batch F](readiness/batch-f.md)); gas conventions by [EL-039](EL-039-air-reservoir.md) and [Batch H](readiness/batch-h.md). This spec leads the "Water and steam" specs (TH-35, TH-18, TH-19, TH-20, TH-28), owns the saturation table and keeps the [batch chains](#batch-n-chains).

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name / type | TH-17 · Kettle · Thermal |
| Anchors | [requirements.md#thermal-17](../requirements.md#thermal-17) (task `sequence-task-451`); [named-elements.md#thermal-17](../invest/named-elements.md#thermal-17) |
| Owner key / research | S581 · [TH-S02 phase change](../../thermal-component-research.md#th-s02), [TH-S07](../../thermal-component-research.md#th-s07) |
| Related identities | Family sibling [TH-35 Coffee-pot steam vessel](TH-35-coffee-pot-steam-vessel.md). Heat from [TH-04](TH-04-electrical-heating-plate.md), [TH-01](TH-01-candle.md), [TH-02](TH-02-fire-bowl.md), [TH-26](TH-26-thermal-storage-block.md) (TX-01 swap). Vapour consumers [TH-19](TH-19-steam-piston.md), [TH-20](TH-20-steam-turbine.md), [TH-18](TH-18-condenser.md). No CAT refines it. |
| Campaign | Intro 69c, a lettered level before the chapter 7 finale (coordinator decision 35); practice 71; reuse 86, 140 ([thermal allocation](../requirements.md#thermal-campaign-allocation)) |
| Status | Not started. Blocked on the engine programme (owner decision 3a, 22 November 2026), in particular ENGINE-SPATIAL-QUERIES and ENGINE-CONTACT-OBSERVATIONS (the kettle on the plate is a face pair with a moving member, T3) and ENGINE-SUPPLY-DEMAND (the plate's debit); then ENGINE-GAS-NETWORK, ENGINE-LIQUID and ENGINE-THERMAL in the order of owner decision 18; and ENGINE-RUN-LENGTH (rows need 140 s). |

### Disposition

**Lead of the Family slice "Steam vessels"** (TH-17, TH-35): no capability, port, mode or parameter differs between the two; declared values, one collider and art do (audit section 5). Planned package id `kettle`, directory `elements/Kettle/`, display name "Kettle". Not a duplicate of anything delivered.

## 2. Declaration

Follows the delivered Balloon for "a dynamic body that owns a gas store" (`elements/Balloon/BalloonElementDeclaration.cs`, `IGasStoreElement`, `CuriousContraptions.Abstractions.Mechanical/MechanicalContracts.cs:109`).

- **Interfaces.** `ISceneGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`), `IMechanicalElement` (`MechanicalContracts.cs:7`), and the store, port, node and phase-row contracts the three engine stories add (section 3).
- **Body.** One dynamic body, origin at the centre of its base. Empty mass 0.2 kg; contents add 16 kg/m³ × the store's volume (L12): 0.22 kg at the default fill. Inertia from the colliders (the delivered rule for a dynamic package body).
- **Colliders (metres).**

  | Collider | Shape | Centre | Size | Purpose |
  | --- | --- | --- | --- | --- |
  | `col_body` | cylinder, axis Y | (0, 0.10, 0) | radius 0.10, height 0.20 | the vessel; the thermal face |
  | `col_spout` | box | (0.13, 0.15, 0) | half extents (0.04, 0.02, 0.02) | the gold spout |

- **Material (f32).** Friction 0.5, restitution 0.05, rolling resistance 0.
- **Liquid store** `water` (L4): capacity 0.0025 m³, plan area 0.0314 m², interior height 0.08 m (capacity ÷ plan area), floor datum (0, 0.01, 0); no liquid mouth; the authored volume comes from `water`. The store is also a thermal node (T2, owner decision 20): capacity mass × 1046 J/(kg·K) (20.9 J/K at the default 0.02 kg), authored starting temperature 288 K (a declared value, not a player parameter, D13), no ambient link of its own.
- **Gas store** `head`: rigid, 0.002 m³; starts as carrier air at 288 K and 101 325 Pa (101 325 × 0.002 ÷ (287 × 288) = 2.45 g, 2.45 g × 718 = 1.76 J/K); it also holds a vapour component (game steam, T11: R 461.5, Cv 691.7, Cp_v 1153.2 J/(kg·K)).
- **Thermal node** `wall` (T2): 12.2 J/K (0.1 kg of steel at 122 J/(kg·K)); ambient conductance 0.3 W/K; face `col_body`, 64 W/K. Wetted links (T3): `wall`↔`water` declared 64 W/K, acting as 64 W/K × the fill fraction (32 W/K at the default fill of 0.5, falling to zero as the charge boils away); `wall`↔`head` 2 W/K (a gas store is always full, fraction 1). With the head air the vessel is the 14 J/K of T14; with the default water (20.9 J/K) the whole is 34.9 J/K.
- **Boil row** (T5) on `water`: boils at T_sat of the head store's total pressure, from the saturation table below. Boiling a mass m moves m·Cp_v·T_sat out of the `water` node into the vapour component of `head`; the liquid gives up m·c·T_sat of it and the heat source pays the rest, the latent heat L_v(T_sat) = 107.2 × T_sat J/kg (40 000 J/kg at 373 K, T11).
- **Condense row** (T5's one condense form, the wall-limited row, in T5's words as TH-18 also declares it): while the `wall` node is below the dew point T_dew of the `head` store's vapour partial pressure (T_sat of that pressure from the game-steam saturation table, T11, clamped at its ends), vapour moves from `head` to `water` at ṁ = G × (T_dew − T_wall) ÷ L_v(T_dew), never more than the vapour present, where G is the declared conductance of the `wall`↔`head` wetted link (here 2 W/K); the row takes m·Cp_v·T_dew out of `head`, of which m·c·T_dew enters `water` with the liquid and the latent m·L_v(T_dew) = m × 107.2 × T_dew enters `wall` (T5, T11).
- Both rows move mass between `water` and `head` without a flow edge; they are the only place gas meets liquid in this part (coordinator decision 16).
- **Gas paths.** `head` ↔ port `GasOut` through a throat of 1.0 × 10⁻⁴ m², flow following pressure in either direction (gas convention F3: "flow direction follows pressure, not socket direction"), so gas pushed into a joined line returns to the head when the head cools; the Choice `spout` closes it. `GasOut` is declared Vent when unconnected (gas convention F4, `EL-039-air-reservoir.md:89`). A one-way relief path `head` → ambient through a throat of 2.0 × 10⁻⁵ m² cracks at 200 000 Pa; section 5 shows what it passes.
- **Port.** `GasOut`, `SimulationDomain.Fluid`, `PortDirection.Output`, at (0.17, 0.15, 0) facing +X (gas convention F3, `EL-039-air-reservoir.md:88`).
- **Parameters.**

  | Name | Kind | Unit | Default | Min | Max | Editor policy |
  | --- | --- | --- | --- | --- | --- | --- |
  | `water` | Real | m³ (a unit member the gas and liquid stories add) | 0.00125 | 0 | 0.0025 | Clamp; build-only |
  | `spout` | Choice | none | Open | Open | Capped | build-only |

- **Presentation bindings.** `Visual/Gauge` scale Y ← committed fraction of `water`; `Visual/Lid` local Y ← committed pressure of `head` (101 325 to 200 000 Pa mapped to 0 to 0.01 m); `Visual/Cap` shown while `spout` is Capped. Vapour is not drawn as a white plume ([visual contract](../requirements.md#thermal-elements)).
- **Palette.** Category `ElementCategory.Motion` (a movable vessel; TH-04 section 6 item 5). Order: 155 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)).

**Saturation table.** The game-steam material row's (TH-04 section 11, T11), shared material data that no element owns: linear between its points from (20 000 Pa, 333.2 K) to (800 000 Pa, 443.6 K), clamped below 20 000 Pa, so the dew point is never below 333.2 K and a wall colder than that condenses any vapour present (TH-18 uses this). The rows here read it between 101 325 Pa (373.0 K) and the relief's 200 000 Pa (393.4 K).

### Art and solidity

| Art node | Mesh | Class | Moved by |
| --- | --- | --- | --- |
| `Visual/Body` cream `#fff8e9` | cylinder radius 0.10, height 0.20, top edge rounded inside the collider | solid, `col_body` | body pose |
| `Visual/Spout` gold `#f7cb52` | box 0.08 × 0.04 × 0.04 | solid, `col_spout` | body pose |
| `Visual/Lid` cream disc with a gold knob | disc radius 0.08 on the top face | decorative: moved by a binding | body pose; presentation binding |
| `Visual/Gauge` cyan `#66b8c9` | flat bar printed flush on the side | decorative: printed flat, scaled by a binding | body pose; presentation binding |
| `Visual/Cap` navy `#293954` | plug drawn inside the spout mouth, flush | decorative: flush, shown by a binding | body pose; presentation binding |

Icon: a round vessel with a short spout and a level bar. It differs from the Coffee pot by silhouette: squat with a spout and no handle, against tall with a handle.

### Compiled counts

Bodies 1 (dynamic); colliders 2 (1 cylinder, 1 box); joints 0; drives 0; sensors 0; thermal nodes 2 (`wall` and the liquid store `water`; the gas store carries its own energy, T2); faces 1; wetted links 2; phase rows 2 (boil, condense); liquid stores 1; gas stores 1; gas paths 2; ports 1; presentation bindings 3.

## 3. Engine capabilities

### Capability probe

Rows W1 to W11 are the shared water-and-steam probe; TH-35, TH-18, TH-19, TH-20 and TH-28 cite them.

| # | Capability | State | Symbol | Exercising test |
| --- | --- | --- | --- | --- |
| TH-04 R1, R5, R6 | Colliders from art; typed ports; parameter bounds | present | as TH-04 | as TH-04 (bodies read) |
| W1 | A dynamic body that owns a gas store | narrower: one isobaric store per body, not rigid, not joined (TH-04 R7) | `GasStoreDescriptor`, `CuriousContraptions.Abstractions/GasStoreDescriptor.cs:9` | `GasCapabilityTests.PreparedSealedInventoryConservesMassAndAdiabaticEnergy` (body read); owner ENGINE-GAS-NETWORK |
| W2 | One fluid port contract for gas and liquid (coordinator decision 16) | narrower: declared and validated, no solver or element consumes it | `FluidPortDescriptor`, `CuriousContraptions.Abstractions/FluidContractValues.cs:12` | none for a network; owner ENGINE-GAS-NETWORK (one fluid port contract, coordinator decision 16) |
| W3 | A presentation binding gated by a Choice option | present, untested here | `Selection:` on a binding, `elements/Fan/FanElementDeclaration.cs:77-78` | `FanIntegrationTests.SelectedModeCompilesOneWorkInputAndOneBladeWriterWithoutHiddenStores` (not opened) |
| W4 | A rigid fixed-volume gas store, lines, and an output that vents through a declared throat when unconnected and that a Choice can close; flow through the throat in either direction, following pressure (F3) | **missing** | none | ENGINE-GAS-NETWORK (Batch H's additions name each; reverse flow through a directed port is unverified, audit section 9) |
| W5 | A store holding a carrier gas and one vapour component, well mixed | **missing** | `GasMaterial` is single-species (`CuriousContraptions.Abstractions/GasMaterial.cs:6`) | ENGINE-GAS-NETWORK (the carrier-and-vapour mixture by Dalton's law, pneumatic P1 and the story's Stores row; coordinator decision 26) |
| W6 | A one-way gas path with a declared cracking pressure | **missing** | none | ENGINE-GAS-NETWORK (text gap) |
| W7 | A liquid store with an authored volume whose contents add mass to a dynamic body and carry a temperature (T2, owner decision 20) | **missing** | none | ENGINE-LIQUID (L4, L12) |
| W8 | Thermal node, face, touch-based contact link, wetted link scaled by fill, ambient exchange (TH-04 R9 to R11, T3, T9) | **missing** | none | ENGINE-THERMAL, with the link table on ENGINE-SPATIAL-QUERIES and ENGINE-CONTACT-OBSERVATIONS |
| W9 | A boil row on a saturation table that moves conserved mass and m·Cp_v·T_sat from a liquid store to a gas store's vapour component, and the wall-limited condense row (section 2) that moves vapour back at G × (T_dew − T_wall) ÷ L_v(T_dew) (T5) | **missing** | none | ENGINE-THERMAL (phase rows, T5: the boil row's enthalpy rule and the one wall-limited condense row, both in the story's phase-row text) |
| W10 | Committed gas pressure as a presentation driver | **missing** | not in `PresentationDriver` (`CuriousContraptions.Abstractions/ElementPresentation.cs:18-37`) | ENGINE-GAS-NETWORK |
| W11 | Committed liquid fraction as a presentation driver | **missing** | not in `PresentationDriver` | ENGINE-LIQUID (L13) |

Counts: present 3, untested 1, narrower 2, missing 8. Every missing row has one owner. Nothing here needs code that names this part.

## 4. Sources and legacy

- **Requirement row** [thermal-17](../requirements.md#thermal-17): finite fluid vessel accepts generic heat; explicit vapour outlet and pressure boundaries; vapour from enthalpy and phase state; no variants.
- **Named entry** [thermal-17](../invest/named-elements.md#thermal-17): owner S581; "an empty vessel emits no water vapor; warm water requires further sensible/latent input before the specified vapor output".
- **Research** per-element table: "node + sealed gas store + nozzle"; parameter capacity; boils to steam pressure; lid rattle ← pressure. [Gas family](../../finite-gas-foundation.md) sealed-store and nozzle laws.
- **Element map** row TH-17: EnvironmentState, FiniteLedger, GasState, GeometryQuery, JointConstraint, LiquidBoiling, PhaseTopology, PressureWork, RigidBodyDynamics, SensibleHeat, TopologyTransaction. Navigation only (D14).
- **Processes and scenarios.** [IX-24](../requirements.md#interaction-24), [IX-40](../requirements.md#interaction-40), [IX-08](../requirements.md#interaction-08), [IX-41](../requirements.md#interaction-41); [TX-01](../requirements.md#thermal-scenario-01), [TX-04](../requirements.md#thermal-scenario-04).
- **Legacy harvest** (sealed-gas foundation; no part consumed it).

  | # | Fact | Citation | Disposition |
  | --- | --- | --- | --- |
  | 1 | Gas state derives T = U/(m·Cv), ρ = m/V, p = ρRT; never independently writable. | `engine/physics/SealedGasState.cs@a6c914e:L18-L41` | Carry forward as the head-space law; f32, not f64. |
  | 2 | Oracle: R 200, Cv 400, m 0.02 kg, V 0.01 m³, U 2400 J → 300 K, 2 kg/m³, 120 000 Pa, γ 1.5. | `CuriousContraptions.tests/SealedGasStateTests.cs@a6c914e:L7-L20` | Carry forward as an analytic test oracle (synthetic material). |
  | 3 | Material is single-species, calorically perfect; no phase transition. | `engine/physics/IdealGasMaterial.cs@a6c914e:L5-L38` | Carry forward fields; boiling and the vapour component are new. |
  | 4 | Out-of-envelope states (U 799 or 8001 J in the oracle) threw exceptions. | `CuriousContraptions.tests/SealedGasStateTests.cs@a6c914e:L97-L112` | Do not carry forward a runtime throw (game-grade clamp-or-continue); keep as compile-time admission. |
  | 5 | Converging nozzle: NoFlow, Subsonic, Choked; critical ratio (2/(γ+1))^(γ/(γ−1)); forward flow only. | `engine/physics/ConvergingGasNozzle.cs@a6c914e:L39-L87` | Carry forward the regimes for the spout; do not carry forward "forward flow only": the throat passes flow toward the lower pressure either way (F3). |
  | 6 | Oracle: back 30 000 Pa → choked, exit 61,440 Pa, 240 K, √72000 m/s; rate independent of lower back pressure; double area doubles rate. | `CuriousContraptions.tests/ConvergingGasNozzleTests.cs@a6c914e:L31-L47` | Carry forward as oracle. |
  | 7 | Closed area or balanced pressure gives no flux. | `CuriousContraptions.tests/ConvergingGasNozzleTests.cs@a6c914e:L64-L74` | Carry forward: a capped spout emits nothing. |
  | 8 | Adiabatic discharge U₂ = U₁(m₂/m₁)^γ; outlet enthalpy = ΔU; no heat input included. | `engine/physics/AdiabaticGasDischarge.cs@a6c914e:L21-L45` | Carry forward for venting; heat input is new. |
  | 9 | Transit quadrature with non-rigorous adaptive Simpson estimates and evaluation budgets. | `engine/physics/GasNozzleTransit.cs@a6c914e:L5-L6` | Do not carry forward: proof-grade CPU integration. |

  Files harvested: `engine/physics/SealedGasState.cs`, `engine/physics/IdealGasMaterial.cs`, `engine/physics/ConvergingGasNozzle.cs`, `engine/physics/AdiabaticGasDischarge.cs`, `engine/physics/GasNozzleTransit.cs`, `CuriousContraptions.tests/SealedGasStateTests.cs`, `CuriousContraptions.tests/ConvergingGasNozzleTests.cs`.

## 5. Acceptance

Point of truth: [thermal-17](../requirements.md#thermal-17), [TX-01](../requirements.md#thermal-scenario-01) and [ELEMENT acceptance](../requirements.md#accept-element). Chrome proof through real UI controls is pending the post-catalogue phase.

### Behaviour and numbers

- **Laws.** Q̇ = G·ΔT on the contact, wetted and ambient links (T3), each link stepped by T9's exact two-node exchange. Boil row (T5, owner decision 19): a mass m leaves `water` carrying m·Cp_v·T_sat = m × 1153.2 × 373 = m × 430 144 J/kg at 373 K into `head`, which is exactly the enthalpy that vapour carries under the gas law; the liquid supplies m × 1046 × 373 = m × 390 158 J/kg of it and the heat source the latent m × 39 986 J/kg (40 000 J/kg, T11). Energy and mass are conserved by construction. The earlier rule (m·(c·T_sat + L) with L = 141 062 J/kg and a steam Cv of 1410) is withdrawn. The gas law and throat flow are ENGINE-GAS-NETWORK's.
- **Vapour volume.** At 373 K and 101 325 Pa game steam occupies R·T ÷ p = 461.5 × 373 ÷ 101 325 = 1.699 m³/kg; the full head (0.002 m³) holds 0.002 ÷ 1.699 = 1.18 g of steam.
- **Heat to boiling.** 34.9 J/K × (373 − 288) = 2966 J. **First 2 g of vapour made:** 0.002 × 40 000 = 80 J. **Whole default charge:** 0.02 × 40 000 = 800 J of latent heat.
- **On the plate** (chain 1): 373 K after 53 s (6.4 kJ); the boil starts at 1.17 g/s and slows as the charge empties; 2 g has left `GasOut` at about 56 s (6.7 kJ); 1 g is left at about 75 s (9.0 kJ). 1.27 g/s (T11) is the bound for ample wetting; 3.0 g/s is the ceiling for any 120 W source.
- **Energy budget.** 6.4 kJ to the boil is more than a default Battery's 3600 J (`elements/Battery/BatteryElementDeclaration.cs:42`, at the 120 W limit of `:44` it is empty at 30 s). Every row below names its Battery `capacity`; boiling rows use the 14 400 J maximum (`:42`), which lasts 120 s.
- **Default Battery.** Empty at 30 s: lumped mean 288 + 150 × (1 − e^(−30 ÷ 62.7)) = 345.0 K; the water lags the mean by about 1 K (31 W through the 32 W/K wetted link) and then gains about 0.5 K from the cooling disc: it peaks near 344 K, 29 K short of the boil. No vapour.
- **Capped spout.** Nothing leaves `GasOut`. The head air alone heats with the vessel: p = 101 325 × T ÷ 288, 131 230 Pa at 373 K. Boiling reads total pressure, so it starts where T = T_sat(101 325 × T ÷ 288): T = 373 + (351.82·T − 101 325) × 2.3626 × 10⁻⁴ gives T = 380.7 K at 133 900 Pa (lumped time −62.7 × ln(1 − 93.3 ÷ 150) = 61 s). The relief cracks at 200 000 Pa, T_sat 393.4 K: air 101 325 × 393.4 ÷ 288 = 138 410 Pa, steam 61 590 Pa, which is 61 590 × 0.002 ÷ (461.5 × 393.4) = 0.68 g. Vaporising into a rigid head stores only Cv·T per kilogram, so the vessel gets back (Cp_v − Cv)·T − L_v = (c − Cv)·T = 354.3 × 393.4 J/kg: 94.6 J for 0.68 g. Heat needed 34.9 × 105.4 − 94.6 = 3584 J; with the disc 1.75 K ahead the lumped mean must rise (15.25 × 107.15 + 3584) ÷ 50.15 = 104.0 K: t = −62.7 × ln(1 − 104.0 ÷ 150) = 74 s, 8.9 kJ. Self-limiting, not a source: each gram raises T_sat by 461.5 × 393 ÷ 0.002 × 1.78 × 10⁻⁴ = 16 K but returns only 139 J, 4.0 K of the vessel's 34.9 J/K.
- **Relief throat.** A rigid head holds its pressure while the moles leaving match the steam made, so the relief's capacity is its mixture flow × R_mix ÷ R_steam. At the crack the head holds 2.45 g of air and 0.68 g of steam: R_mix = (2.45 × 287 + 0.68 × 461.5) ÷ 3.13 = 324.9, Cv_mix = (2.45 × 718 + 0.68 × 691.7) ÷ 3.13 = 712.3, γ = 1037.2 ÷ 712.3 = 1.456, whose critical ratio (2 ÷ 2.456)^(1.456 ÷ 0.456) = 0.519 is above the back ratio 101 325 ÷ 200 000 = 0.507, so the flow is just choked: flux = p × √(γ ÷ (R_mix·T)) × (2 ÷ (γ + 1))^((γ + 1) ÷ (2(γ − 1))) = 200 000 × 3.375 × 10⁻³ × 0.575 = 388 kg/(s·m²) at 393.4 K. Through 2.0 × 10⁻⁵ m² that is 7.8 g/s of mixture, worth 7.8 × 324.9 ÷ 461.5 = 5.5 g/s of steam, which carries 5.5 × 10⁻³ × L_v(393.4) = 5.5 × 10⁻³ × 42 172 = 230 W of latent heat. As the air purges the capacity rises toward the pure-steam figure, 200 000 × 5.247 × 10⁻³ × 0.3246 = 340.6 kg/(s·m²) (subsonic for γ = 1.667, TH-20 section 5), 6.8 g/s. Boil rates at the relief state: the plate at most 120 ÷ 42 172 = 2.8 g/s; the Fire bowl at most about 160 W into a kettle at 393 K (TH-02's flame balance 1.255·x = 228 + 105.4, x = 265.7 K of rise, 265.7 − 105.4 = 160 W), 3.8 g/s; the Candle under 70 W, 1.7 g/s; a Thermal storage block at its 400 K reference start never brings a capped kettle to the relief (the pair settles at (420 × 400 + 34.9 × 288) ÷ 454.9 = 391.4 K). For each of these the relief holds the head at its crack. One tick of relief flow is 7.8 ÷ 120 = 0.065 g of the 3.13 g in the head, 2.1% of its gas; venting adiabatically drops the pressure about 1.456 × 2.1% = 3% (about 6000 Pa), which shuts the relief again, as A5's boundary requires. Between openings the head rises by one tick of boiling, at most 2.8 ÷ 120 = 0.023 g of steam, 0.023 × 10⁻³ × 461.5 × 393.4 ÷ 0.002 = 2100 Pa, so it never exceeds the crack by more than that, inside A5's 5000 Pa tolerance. A block hotter than about 415 K when the relief opens (its 12.8 W/K pair in series with the 64 W/K × fill wetted link is 9.1 W/K at fill 0.5 and 10.7 W/K at fill 1; 393.4 + 230 ÷ 9.1 = 419 K, 393.4 + 230 ÷ 10.7 = 415 K) boils faster than the relief passes, and the head rises above 200 000 Pa until the relief's flow matches the boil (item 6).
- **The condense row in these rows.** It acts only while `wall` is below T_dew of the head's vapour partial pressure. Before the boil the head holds no vapour, so it moves nothing. Boiling with the spout open, the head's vapour is at most the 101 325 Pa total (T_dew at most 373 K) while the wall runs x = 50.8 ÷ (2.79 + 64f) K above 373 K (chain 1), so nothing condenses. Capped, the vapour's partial pressure at the relief is 61 590 Pa, T_dew = 354.5 + (61 590 − 50 000) ÷ 51 325 × 18.5 = 358.7 K, against a wall at or above 393.4 K. It can act only after the Battery is empty at 120 s, as the vessel cools; no row asserts anything in that window, and A3's ledger holds either way because the row conserves mass and enthalpy. Declaring the rate therefore changes no row here; TH-18's rows and TH-19's return row exercise it.
- **Warm water** control: any state below T_sat produces no vapour; the row holds the node at T_sat only while latent heat is supplied.
- **Tick order.** T8: the boil row runs in the thermal step of tick N; the vapour is in `head` at the tick boundary and flows from the substeps of tick N + 1.
- **Tolerances.** T12. Liquid and vapour moved by the phase rows are checked as mass on the stores' ledgers, in grams (0.2 g, 10% of the 2 g output), not against L17's 0.004 m³ volume tolerance, which is 3.2 times the whole charge (audit C2).
- **Run length these rows need: 140 s** (A5's stall guard is 134 s and A3 observes at 120 s; ENGINE-RUN-LENGTH, owner decisions 13 and 17; default 30 s, maximum 300 s).

### Rows

Planned class `KettleTests`. Heat source: a TH-04 plate wired to a Battery of the stated `capacity`, the kettle standing on the disc.

| # | Given / When / Then | Planned test | Control | Boundary | Tolerance |
| --- | --- | --- | --- | --- | --- |
| A1 | Given a default kettle on the plate, Battery `capacity` 14 400 J, then no vapour leaves `GasOut` before 45 s (53 s − 15%) and 2 g has left by the stall guard (hand 55.7 s) | `AHeatedKettleEmitsVapourOnlyAfterItBoils` | an empty kettle (`water` 0), same Battery: no vapour in 140 s | Battery `capacity` 5400 J (empty at 45 s; lumped mean 288 + 150 × (1 − e^(−45/62.7)) = 364.8 K): the water peaks near 364 K and no vapour is made | 0.2 g; ± 3.8 K (T12: 5% of the 76 K rise), so the band stays 5 K short of the boil; stall guard 116 s (min of 3 × 55.7 and 55.7 + 60) |
| A2 | Given A1, when at most 1 g of water is left (hand 75 s), then liquid lost = vapour in `head` + vapour out, by the stores' ledgers | `LiquidLostEqualsVapourMade` | none | none | 0.2 g; stall guard 135 s |
| A3 | Given A1, when the Battery is empty at 120 s, then its 14 400 J debit = the rise of node and store enthalpy + the ambient account + the enthalpy vented (T13) | `PlateWorkBalancesSensibleLatentAndLosses` | none | none | 288 J (2% of the debit) |
| A4 | Given a default Battery (`capacity` 3600 J), when it is empty, then the water peaks near 344 K and the boil row never moves mass | `WarmWaterNeedsMoreHeatBeforeAnyVapour` | A1 | none | ± 2.8 K (T12: 5% of the 56 K rise); vapour mass 0 (no transfer occurs) |
| A5 | Given `spout` Capped and Battery `capacity` 14 400 J, then no vapour leaves `GasOut`; the head is above ambient before the boil (air alone, about 131 000 Pa at 373 K); the relief opens (hand 74 s) and the head never exceeds 200 000 Pa | `ACappedSpoutBuildsPressureUntilTheReliefOpens` | A1: an open spout keeps the head within 500 Pa of ambient (hand (ṁ ÷ A)² ÷ (2ρ) = 12.7² ÷ (2 × 0.589) = 137 Pa at 1.27 g/s) | the relief passes nothing while the head is below 195 000 Pa | 5000 Pa; stall guard 134 s |
| A6 | Given A1 then Reset, then `water` is 0.00125 m³ at 288 K and `head` holds only ambient air; a second Run repeats A1 | `ResetRestoresWaterAirAndTemperature` | none | none | as A1 |
| A7 | Save and load keep pose, `water`, `spout` and any hose; no run state is saved | `SaveAndLoadKeepTheChargeAndNoRunState` | none | none | canonical field values |
| A8 | `water` of −1 or 0.003 is clamped; an electrical or activation link to `GasOut` is refused | generic: TH-04 R6 and R5 once Fluid links are validated by ENGINE-GAS-NETWORK | — | 0 and 0.0025 admitted | typed refusal |

The TX-01 source swap (candle, fire bowl, stored-hot block) is one row in each source's slice, using chains 2 and 3.

<a id="batch-n-chains"></a>
### Batch N chains (water, steam, ice and the plate)

Plate values are TH-04's (disc 15.25 J/K, 0.5 W/K, 120 W). Steam values are T11's (L_v = 107.2 × T J/kg). Chains for the striker, the match, the lens, the absorber and the heat pump are in TH-31, TH-33, TH-06, TH-07 and TH-12.

| # | Chain | Derivation | Result |
| --- | --- | --- | --- |
| 1 | Plate → kettle | Pair 64·64 ÷ 128 = 32 W/K. Lumped: C = 15.25 + 34.9 = 50.15 J/K, loss 0.5 + 0.3 = 0.8 W/K, final 288 + 150 = 438 K, time constant 62.7 s. At 373 K the mean is rising 1.03 K/s, so 61 W enters the kettle and the disc runs 1.9 K ahead: the mean must rise 85.6 K, t = −62.7 × ln(1 − 85.6 ÷ 150) = 53.0 s. Boiling: 120 = 0.5·(T_d − 288) + 32·(T_d − T_w); with the wall at 373 + x the plate gives 76.3 − 0.49x W and the wall loses 0.3·(85 + x) to the room, 2x to the head (carried out with the vented steam) and 64·f·x to the water, f the fill: x = 50.8 ÷ (2.79 + 64f). At f = 0.5 the water gets 46.7 W: 1.17 g/s. Mass from f = 0.5 to f = 0.025 (1 g left), with dm = 0.04·df and 40 000 J/kg: t = (1600 ÷ 50.8) × [0.475 + 0.0436 × ln 20] = 19.1 s, plus about 3 s while wall and disc warm 10 K (275 J) | 373 K at 53 s (6.4 kJ); 2 g made at 54.7 s; 2 g out at 55.7 s (3.18 g made, the head holding 1.18 g; 6.7 kJ); 1 g left at about 75 s (9.0 kJ). The level needs `capacity` ≥ 9.6 kJ; lessons use 14 400 J |
| 2 | Flame → kettle | Power P into the kettle is the region-link result owned by TH-01 and TH-02. Kettle alone: final 288 + P ÷ 0.3, time constant 116.3 s, t = −116.3 × ln(1 − 25.5 ÷ P); net boil power P − 25.5 W | No boil below 25.5 W. At 60 W: 373 K at 64.4 s, 2 g made 80 ÷ 34.5 = 2.3 s later. With TH-01's repaired Candle (68.0 W into a 288 K kettle, 60.8 W at 373 K; mean 64.4 W): −116.3 × ln(1 − 25.5 ÷ 64.4) = 58.6 s, then 80 ÷ (60.8 − 25.5) = 2.3 s; at most (60.8 − 25.5) ÷ 40 000 = 0.88 g/s. With TH-02's repaired Fire bowl (182 W and 164 W; mean 173 W): −116.3 × ln(1 − 25.5 ÷ 173) = 18.6 s, then 80 ÷ (164 − 25.5) = 0.58 s; at most 3.5 g/s |
| 3 | Stored-hot block → kettle | TH-26 block 420 J/K authored at `initial_temperature` 400 K (T14: a level setting), ceramic face: pair 64·16 ÷ 80 = 12.8 W/K. Two-node time constant (420 × 34.9 ÷ 454.9) ÷ 12.8 = 2.5 s toward 391.4 K: 373 K when e^(−t/2.5) = 18.4 ÷ 103.4. Block then at 392.9 K: 12.8 × 19.9 − 25.5 = 229 W net | 373 K at 4.4 s; 2 g made 0.35 s later; the block holds (392.9 − 375) × 420 = 7.5 kJ above 375 K against the charge's 800 J, so the whole charge boils (about 4 s, the last grams slower as the wetted link thins). The block's own 3.2 W/K loss (TH-26) costs under 0.5 kJ in that time |
| 4 | Plate → coffee pot | [TH-35](TH-35-coffee-pot-steam-vessel.md) | 373 K at 61.1 s (7.3 kJ); 2 g out at about 64 s (7.7 kJ) |
| 5 | Kettle → condenser | [TH-18](TH-18-condenser.md): the vapour arrives at 373 K carrying 1153.2 × 373 − 1046 × T_dew J/kg more than the condensate keeps (81.6 J/g at the 333.2 K table floor) | the 7.5 J/K wall alone condenses about 11 g and passes about 4.7 g by the time 1 g is left in the kettle; a Cold pack on the shelf condenses all of it |
| 6 | Kettle → piston, turbine | [TH-19](TH-19-steam-piston.md), [TH-20](TH-20-steam-turbine.md) | warm air moves the rod about 0.14 m before the boil; full stroke about 3 s after it. Turbine throat 3.0 × 10⁻⁶ m²: head about 150 000 Pa, available power about 46 W, best shaft about 11.6 W |
| 7 | Plate → ice block, ice plug | [TH-14](TH-14-ice-block.md), [TH-15](TH-15-ice-plug.md) section 5 | owned there; not restated here |
| 8 | Sink → freezing mold | [TH-13](TH-13-freezing-mold.md) section 5 | owned there; not restated here |
| 9 | Stability | T9's exact two-node exchange is stable for any G and C. The kettle's stiffest pair, `wall` (12.2 J/K) to `water` through 32 W/K at fill 0.5, has G·dt ÷ C_r = 32 ÷ 120 ÷ 7.7 = 0.035; as the charge empties both G and the store's capacity go to zero together, and a store of zero capacity is inactive | no clamp and no refusal needed |

## 6. Open questions

Resolved:

1. **Game scale.** D5 and owner decision 19, stated once in TH-04 T11.
2. **Pacing.** The plate needs about 56 s and 6.7 kJ for the first 2 g out. Decided now: accepted; boiling lessons give the Battery its 14 400 J maximum.
3. **Mixture.** One store holds carrier air and a steam component (W5). Decided now: a single-species store would either start as cold "steam" that condenses on any cool wall or lose its air; the two-component store is the general form and TH-28's humidity uses the same idea.
4. **Enthalpy reference.** Both phases are referenced to 0 K (liquid c·T, vapour Cp_v·T), so the row moves m·Cp_v·T_sat and no separate reference is needed (T5).
5. **Saturation curve.** The game-steam table of T11 (section 2), clamped below 20 000 Pa. Boiling reads the store's total pressure against the water; condensing reads the vapour's partial pressure against the wall (the condense row, section 2).
6. **Relief.** 200 000 Pa through a 2.0 × 10⁻⁵ m² throat, decided now: the earlier 400 000 Pa could not be reached from a 120 W plate within a 14 400 J Battery, and the throat passes more than the plate, the Candle or the Fire bowl can boil at the crack (section 5). A Thermal storage block authored above about 415 K (its range reaches 500 K) can boil faster; the head then rises above the crack until the relief's flow matches, bounded and conserving, on the saturation table up to its 800 000 Pa top, which is the gas limit (T11, pneumatic P14). Accepted as a limit, decided now: no lesson caps a kettle on a block that hot and no row asserts it; a larger throat would make each opening tick drop the head further below the crack.
7. **Blocking the spout.** A Choice `spout` with a drawn cap (D19: the requirement's sibling row names a blocked spout; a body pressed on the mouth is not a gas boundary).
8. **Refill.** None; the charge is authored (D13).
9. **Closed-head boiling returns heat.** With T11's values the vapour's internal energy (Cv·T) is below the liquid's (c·T), so vapour made into a rigid head hands 354.3 × T J/kg back to the vessel (section 5). Decided now: accepted as the consequence of decision 19's values; it is self-limiting (T_sat rises four times faster than the vessel warms), the ledger closes, and A5 covers it. Recorded for the Batch N summary as a note for T11.

Needs the owner: none.

## 7. Integration

- **Works with.** TH-04 (planned first); TH-01, TH-02, TH-26 (source swap rows in their slices); TH-18, TH-19, TH-20 through a hose, which is the gas family's Fluid connection kind. A drawn hose is a thick strand and therefore solid (owner decision 1, 22 November 2026): hose rows are blocked on ENGINE-CONNECTION-MATERIALS (solid strand); rows A1 to A8 use no hose.
- **Campaign.** "Full Steam Ahead": introduction 69c (a lettered level before the chapter 7 finale 70, coordinator decision 35), practice 71; reuse 86, 140. Each of these levels declares a Run length of at least 140 s and a Battery of 14 400 J. A level that fixes the kettle needs the lesson-fixture story ([S8](../invest/delivery-batches.md#s8-lesson-fixtures-for-any-admitted-package)), which lands after the engine programme (owner decision 22).
- **Coverage.** Section 10.

## 8. Delivery

- **Slice shape.** Family slice "Steam vessels" (TH-17, TH-35), engine tier on first use of the boil row.
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), which comes first: the programme (ENGINE-SPATIAL-QUERIES, ENGINE-CONTACT-OBSERVATIONS, ENGINE-SUPPLY-DEMAND among it); ENGINE-GAS-NETWORK as amended (W2, W4 to W6, W10; owner of the one fluid port contract, coordinator decision 16); ENGINE-LIQUID as amended (W7, W11; text gap: a phase-row source or sink, and a stream fed by one, is not subject to L17's zero-flow threshold, and the ledger resolves 10⁻⁶ m³); ENGINE-THERMAL as amended (W8, W9); ENGINE-RUN-LENGTH; TH-04; S7; S3 preferred.
- **Files.** `elements/Kettle/Kettle.csproj`, `KettleElementDeclaration.cs`, `kettle.tscn`, `icon.svg`; one guarded line in `CuriousContraptions.slnx` (`spec-s3-remaining-per-package-lists.md`, decision O1); `CuriousContraptions.tests/KettleTests.cs`; a row in `VisibleSolidColliderTests`; the binding section once S7 allows it. Registration is derived (`spec-registration-derivation.md`).
- **Review tier.** Engine tier unless the probe finds W4 to W11 present and tested.
- **Disqualifiers.** A gas network with single-species stores only (re-decide item 3); a forward-only throat (TH-19's return row needs gas to flow back into the head); phase rows that cannot write a gas store, or that move other than m·Cp_v·T_sat on boiling and m·Cp_v·T_dew at the wall-limited rate on condensing; a wetted link not scaled by fill; a store on a dynamic body that does not add mass; a different saturation rule; the thermal step in another phase; a venting step that leaves the open head below ambient (the boil row reads committed head pressure, and a head left below the vent's outer pressure would flash the charge: A5's control catches it).

## 9. Engine conformance (22 November 2026)

1. **Declaration data only.** Two colliders, one liquid store (also a node), one gas store, one node, two wetted links, two phase rows (boil; wall-limited condense), two gas paths (spout, relief), one port, two parameters, three bindings. No update loop, handler or engine branch on this part.
2. **Generic laws.** Conduction (T3, T9), the boil and condense rows (T5), the gas law and throat flow. "Boils and whistles" is a consequence of heat reaching a store with a boil row and a throat.
3. **Three layers.** Colliders: `col_body`, `col_spout`. Visuals: body and spout art with their colliders; lid, gauge and cap decorative. Animation: gauge, lid and cap from committed state; nothing is written back.
4. **Flow and typing.** One Fluid output; f32 values; `spout` is a Choice; no string in domain logic.
5. **Flat state.** The counts in section 2.
6. **Performance.** Two primitive colliders; no sensor; thermal work is two nodes, two wetted links, two rows and one link-table entry per touching face (T10). This spec does not assume island sleeping, which does not exist in the engine today (ENGINE-ISLANDS-SLEEP is planned); T3 requires the link table to persist through it once it does.
7. **Tests are scenarios.** A1 to A7 run the production simulation.

**Verdict: Conforms; blocked on ENGINE-GAS-NETWORK, ENGINE-LIQUID and ENGINE-THERMAL, each a general capability stated element-free, on ENGINE-RUN-LENGTH, and on the engine programme (owner decision 3a).**

## 10. Coverage binding (planned, not yet live)

Published main binds a named-identity package through a final section of its spec that `tools/Coverage/NamedPackageSources.cs` reads (pattern: EL-239 Baseball). That section is not written here. **Blocked on the coverage follow-on story S7:** main's reader lists only `EL-*.md` files and admits only an `EL-NNN` identity with an `element-NNN` anchor.

| Field | Planned value |
| --- | --- |
| Package | `kettle` |
| Named identity | TH-17, `../invest/named-elements.md#thermal-17` |
| Acceptance | none resolvable today; the row's own task is `sequence-task-451` and the ledger entry has no todo link |
| Decision | S581; `scope-corrections.md` on published main has no `s581` block |
| Mode records | `Spout=Open`, `Spout=Capped` |
