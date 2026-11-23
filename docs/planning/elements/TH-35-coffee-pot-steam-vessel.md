# TH-35 · Coffee-pot steam vessel — named-identity readiness spec

**Current delivery (24 October 2026):** [TH-35 delivery slice](../invest/named-elements.md#thermal-35). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Rewritten on 22 November 2026 as one current text against checkout `spec-drafts` (published main `8d1d68aa`), and repaired the same day against the audit `docs/planning/elements/readiness/audits/elements-batch-n.md` (E1, C2, N6, TH-35 status and family) and owner decisions 17 to 20 and coordinator decisions 16 and 21, and on 23 November 2026 against the second audit `docs/planning/elements/readiness/audits/reaudit-group-3.md` (relief throat, F4 vent, condense row). Code citations are `path:line` in that checkout; `path@a6c914e` citations appear only in section 4. Nothing was built or run: every number is a hand calculation shown here and a starting value under decision D1 of [delivery batches](../invest/delivery-batches.md#3-family-level-decisions). Thermal conventions T1 to T14 are owned by [TH-04](TH-04-electrical-heating-plate.md#thermal-conventions); the water-and-steam probe rows W1 to W11, the saturation table and the boil row are owned by the family lead [TH-17](TH-17-kettle.md).

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name / type | TH-35 · Coffee-pot steam vessel · Thermal |
| Anchors | [requirements.md#thermal-35](../requirements.md#thermal-35) (task `sequence-task-469`); [named-elements.md#thermal-35](../invest/named-elements.md#thermal-35) |
| Owner key / research | S600 · [TH-S02 phase change](../../thermal-component-research.md#th-s02), [TH-S07](../../thermal-component-research.md#th-s07) |
| Related identities | Family lead [TH-17 Kettle](TH-17-kettle.md). Heat from [TH-04](TH-04-electrical-heating-plate.md) or a stored-hot [TH-26](TH-26-thermal-storage-block.md); vapour to [TH-19](TH-19-steam-piston.md), [TH-18](TH-18-condenser.md) or a receiver. No CAT refines it. |
| Campaign | Intro 86; practice 87; reuse 100, 140 ([thermal allocation](../requirements.md#thermal-campaign-allocation)) |
| Status | Not started. Blocked on the engine programme (owner decision 3a, 22 November 2026), then as TH-17: ENGINE-SPATIAL-QUERIES, ENGINE-CONTACT-OBSERVATIONS and ENGINE-SUPPLY-DEMAND within that programme; ENGINE-GAS-NETWORK, ENGINE-LIQUID and ENGINE-THERMAL (owner decision 18 order); ENGINE-RUN-LENGTH (rows need 150 s). |

### Disposition

**Member of the Family slice "Steam vessels"** with TH-17. Checked against the Kettle item by item: the same interfaces, stores, nodes, wetted links, boil row, gas paths, port and parameters, and no capability, port, mode or parameter differs. It differs in declared values (wall mass, head volume, liquid capacity, throat area, relief pressure, ambient conductance), one additional collider (the handle) and art. So it is a family member by declared values, one additional collider and art; it is not a value-only variant, and it is not a duplicate (every value below differs and it has its own catalogue entry, D8). The requirement's "constrained spout" is a smaller declared throat, not a new capability. Planned package id `coffee_pot`, directory `elements/CoffeePot/`, display name "Coffee pot".

## 2. Declaration

As [TH-17 section 2](TH-17-kettle.md), with these values.

- **Body.** One dynamic body; empty mass 0.3 kg; contents add 16 kg/m³ × volume (L12): 0.32 kg at the default fill. Inertia from the colliders.
- **Colliders (metres).**

  | Collider | Shape | Centre | Size | Purpose |
  | --- | --- | --- | --- | --- |
  | `col_body` | cylinder, axis Y | (0, 0.14, 0) | radius 0.08, height 0.28 | the pot; the thermal face |
  | `col_handle` | box | (−0.10, 0.16, 0) | half extents (0.02, 0.08, 0.03) | the gold handle |
  | `col_spout` | box | (0.10, 0.22, 0) | half extents (0.03, 0.015, 0.015) | the narrow spout |

- **Material (f32).** Friction 0.5, restitution 0.05, rolling resistance 0.
- **Liquid store** `water` (L4): capacity 0.003125 m³, plan area 0.0201 m² (π × 0.08²), interior height 0.155 m, floor datum (0, 0.01, 0). It is also a thermal node (T2, owner decision 20): mass × 1046 J/(kg·K), 20.9 J/K at the default 0.02 kg, authored start 288 K (declared, D13).
- **Gas store** `head`: rigid, 0.0015 m³; carrier air at ambient (101 325 × 0.0015 ÷ (287 × 288) = 1.84 g, 1.32 J/K) plus a steam component (T11).
- **Thermal node** `wall`: 18.3 J/K (0.15 kg of steel); ambient conductance 0.35 W/K; face `col_body`, 64 W/K. Wetted links `wall`↔`water` declared 64 W/K × the fill fraction (25.6 W/K at the default fill 0.00125 ÷ 0.003125 = 0.4), `wall`↔`head` 2 W/K. Whole vessel with the default water: 18.3 + 1.32 + 20.9 = 40.5 J/K.
- **Phase rows** (T5): the boil row on `water` as TH-17 declares it (on the game-steam saturation table of T11; boiling moves m·Cp_v·T_sat into the vapour component, latent L_v = 107.2 × T_sat J/kg, 40 000 J/kg at 373 K, T11) and T5's one wall-limited condense row in its words, with G the declared conductance of this pot's `wall`↔`head` wetted link (2 W/K). They are the only meeting of gas and liquid in the part (coordinator decision 16).
- **Gas paths.** `head` ↔ `GasOut` through a throat of 5.0 × 10⁻⁵ m² (half the kettle's), flow following pressure in either direction as TH-17's (F3); `GasOut` is declared Vent when unconnected (gas convention F4, `EL-039-air-reservoir.md:89`); closed by `spout`. One-way relief `head` → ambient through a throat of 2.0 × 10⁻⁵ m² at 150 000 Pa (section 5 shows what it passes).
- **Port.** `GasOut`, Fluid, Output, at (0.13, 0.22, 0) facing +X (gas convention F3).
- **Parameters.**

  | Name | Kind | Unit | Default | Min | Max | Editor policy |
  | --- | --- | --- | --- | --- | --- | --- |
  | `water` | Real | m³ | 0.00125 | 0 | 0.003125 | Clamp; build-only |
  | `spout` | Choice | none | Open | Open | Capped | build-only |

- **Presentation bindings.** `Visual/Window` scale Y ← committed fraction of `water`; `Visual/Lid` local Y ← committed `head` pressure (101 325 to 150 000 Pa mapped to 0 to 0.01 m); `Visual/Cap` shown while Capped.
- **Palette.** `ElementCategory.Motion`. Order: 165 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)).

### Art and solidity

| Art node | Mesh | Class | Moved by |
| --- | --- | --- | --- |
| `Visual/Body` cream `#fff8e9` | cylinder radius 0.08, height 0.28, shoulders rounded inside the collider | solid, `col_body` | body pose |
| `Visual/Handle` gold `#f7cb52` | box 0.04 × 0.16 × 0.06 | solid, `col_handle` | body pose |
| `Visual/Spout` gold | box 0.06 × 0.03 × 0.03 | solid, `col_spout` | body pose |
| `Visual/Lid` cream disc | disc radius 0.06 on the top face | decorative: moved by a binding | body pose; presentation binding |
| `Visual/Window` cyan `#66b8c9` | small flat bar printed flush | decorative: printed flat, scaled by a binding | body pose; presentation binding |
| `Visual/Cap` navy `#293954` | plug inside the spout mouth, flush | decorative: flush, shown by a binding | body pose; presentation binding |

Icon: a tall pot with a handle on one side and a thin spout on the other. It differs from the Kettle by the handle and the height (0.28 m against 0.20 m).

### Compiled counts

Bodies 1 (dynamic); colliders 3 (1 cylinder, 2 boxes; one more than the Kettle for the handle, which is a proud visible solid); joints 0; thermal nodes 2 (`wall` and the liquid store); faces 1; wetted links 2; phase rows 2 (boil, condense); liquid stores 1; gas stores 1; gas paths 2 (spout, relief); ports 1; presentation bindings 3.

## 3. Engine capabilities

### Capability probe

| # | Capability | State | Owner or evidence |
| --- | --- | --- | --- |
| TH-04 R1, R5, R6 | Colliders from art; typed ports; parameter bounds | present | as TH-04 |
| TH-17 W3 | Binding gated by a Choice | present, untested here | as TH-17 (test not opened) |
| TH-17 W1, W2 | Gas store on a body; one fluid port contract | narrower | as TH-17; owner ENGINE-GAS-NETWORK |
| TH-17 W4, W5, W6, W10 | Rigid store with a venting, closable output; carrier plus vapour; cracking relief; pressure driver | **missing** | ENGINE-GAS-NETWORK |
| TH-17 W7, W11 | Liquid store adding mass to its body and carrying a temperature; fraction driver | **missing** | ENGINE-LIQUID |
| TH-17 W8, W9 | Node, touch-based contact link, wetted link scaled by fill, and the boil row | **missing** | ENGINE-THERMAL (link table on ENGINE-SPATIAL-QUERIES and ENGINE-CONTACT-OBSERVATIONS) |

Counts: present 3, untested 1, narrower 2, missing 8, identical to TH-17. No row is specific to this vessel, which is what makes it a family member.

## 4. Sources and legacy

- **Requirement row** [thermal-35](../requirements.md#thermal-35): distinct vessel geometry with finite liquid, thermal contacts and a constrained spout; shared phase/flow laws produce steam; no variants.
- **Named entry** [thermal-35](../invest/named-elements.md#thermal-35): owner S600; "an empty or insufficiently heated pot does not emit vapor; a blocked spout uses actual pressure boundaries".
- **Research** per-element table: "node + sealed gas + nozzle"; capacity; "perks and vents steam to a receiver"; lid ← pressure.
- **Element map** row TH-35: EnvironmentState, FiniteLedger, GasState, GeometryQuery, JointConstraint, LiquidBoiling, PhaseTopology, PressureWork, RigidBodyDynamics, SensibleHeat, TopologyTransaction (D14).
- **Processes.** [IX-24](../requirements.md#interaction-24), [IX-40](../requirements.md#interaction-40), [IX-08](../requirements.md#interaction-08).
- **Legacy harvest** (sealed-gas foundation; no part consumed it; the full table is in [TH-17](TH-17-kettle.md)).

  | # | Fact | Citation | Disposition |
  | --- | --- | --- | --- |
  | 1 | Head-space state T = U/(m·Cv), p = ρRT, derived only. | `engine/physics/SealedGasState.cs@a6c914e:L18-L41` | Carry forward. |
  | 2 | Nozzle regimes NoFlow, Subsonic, Choked; forward flow only. | `engine/physics/ConvergingGasNozzle.cs@a6c914e:L39-L87` | Carry forward the regimes for the spout; not "forward flow only" (F3, as TH-17). |
  | 3 | Closed area or balanced pressure gives no flux. | `CuriousContraptions.tests/ConvergingGasNozzleTests.cs@a6c914e:L64-L74` | Carry forward: a capped spout emits nothing. |
  | 4 | Doubling throat area doubles choked mass rate. | `CuriousContraptions.tests/ConvergingGasNozzleTests.cs@a6c914e:L44-L46` | Carry forward: at equal head and back pressures the rate scales with area, subsonic or choked. |
  | 5 | Out-of-envelope states threw. | `engine/physics/SealedGasState.cs@a6c914e:L29-L33` | Do not carry forward: clamp-or-continue at runtime. |
  | 6 | No phase transition in the gas material. | `engine/physics/IdealGasMaterial.cs@a6c914e:L5-L6` | Recorded: boiling is new. |

  Files harvested: `engine/physics/SealedGasState.cs`, `engine/physics/ConvergingGasNozzle.cs`, `engine/physics/IdealGasMaterial.cs`, `CuriousContraptions.tests/ConvergingGasNozzleTests.cs`.

## 5. Acceptance

Point of truth: [thermal-35](../requirements.md#thermal-35) and [ELEMENT acceptance](../requirements.md#accept-element). Chrome proof is pending the post-catalogue phase.

### Behaviour and numbers

- **Laws.** As TH-17: conduction (T3, T9), the boil row (T5: m·Cp_v·T_sat into the vapour, 40 000 J/kg latent at 373 K), the gas law and throat flow.
- **Energy budget.** Boiling needs more than a default Battery's 3600 J (`elements/Battery/BatteryElementDeclaration.cs:42`; 120 W limit at `:44`). Every row names its `capacity`; boiling rows use the 14 400 J maximum, which lasts 120 s.
- **On the plate** (TH-04: 120 W, disc 15.25 J/K, 0.5 W/K; pair 32 W/K). Lumped: C = 15.25 + 40.5 = 55.75 J/K; loss 0.5 + 0.35 = 0.85 W/K; final 288 + 120 ÷ 0.85 = 429.2 K; time constant 55.75 ÷ 0.85 = 65.6 s. At 373 K the mean rises 0.85 K/s, 64 W enters the pot and the disc runs 2.0 K ahead, so the mean must rise 85.6 K: t = −65.6 × ln(1 − 85.6 ÷ 141.2) = 65.6 × 0.932 = 61.1 s (7.3 kJ). Confirmed.
- **Boiling.** With the wall at 373 + x the plate gives 76.3 − 0.49x W; the pot loses 0.35·(85 + x) to the room, 2x to the head and 25.6·x to the water: x = 46.56 ÷ 28.44 = 1.64 K, so the water gets 41.9 W: 41.9 ÷ 40 000 = 1.05 g/s at the default fill, slowing as it empties. The head holds 0.0015 ÷ 1.699 = 0.88 g of steam at 373 K, so 2 g out needs 2.88 g made: 115 J ÷ 41.9 W = 2.75 s, at 63.9 s and 7.7 kJ. 1 g left (fill 0.02): t = (0.05 × 40 000 ÷ 46.56) × [0.38 + 0.0444 × ln 20] = 22.0 s, plus about 3 s while wall and disc warm: about 86 s and 10.3 kJ.
- **Insufficient heating.** A default Battery (3600 J) is empty at 30 s with the lumped mean at 288 + 141.2 × (1 − e^(−30/65.6)) = 288 + 141.2 × 0.367 = 339.8 K; the water peaks near 339 K: no vapour.
- **Constrained spout.** Flow through the throat is subsonic at every reachable head (choking for γ = 1.667 needs p_in ÷ p_out above 1 ÷ 0.487 = 2.05, beyond the 150 000 Pa relief), and Δp = (ṁ ÷ A)² ÷ (2ρ) with steam at 0.589 kg/m³. Boiling on its plate the pot vents 1.05 g/s through 5.0 × 10⁻⁵ m²: (21.0)² ÷ 1.178 = 374 Pa; the kettle vents 1.17 g/s through 1.0 × 10⁻⁴ m²: 11.7² ÷ 1.178 = 116 Pa. Per unit rate squared the pot needs 374 ÷ 1.10 = 339 against the kettle's 116 ÷ 1.37 = 85: four times, as half the area requires. The jet is narrower and faster.
- **Capped spout.** As TH-17: the head air alone reaches 133 900 Pa where boiling begins at 380.7 K (mean rise 93.2 K, t = −65.6 × ln(1 − 93.2 ÷ 141.2) = 70.8 s). The relief cracks at 150 000 Pa, T_sat 384.5 K: air 101 325 × 384.5 ÷ 288 = 135 270 Pa, steam 14 730 Pa = 14 730 × 0.0015 ÷ (461.5 × 384.5) = 0.12 g, returning 354.3 × 384.5 × 0.000125 = 17 J to the vessel (TH-17 item 9). Heat 40.5 × 96.5 − 17 = 3895 J; mean rise (15.25 × 98.4 + 3895) ÷ 55.75 = 96.7 K: t = −65.6 × ln(1 − 96.7 ÷ 141.2) = 75.8 s (9.1 kJ). The earlier 300 000 Pa relief could not be reached from the plate and is withdrawn.
- **Relief throat.** As TH-17, the relief's capacity is its mixture flow × R_mix ÷ R_steam. At the crack the head holds 1.84 g of air and 0.12 g of steam: R_mix = (1.84 × 287 + 0.12 × 461.5) ÷ 1.96 = 297.7, Cv_mix = (1.84 × 718 + 0.12 × 691.7) ÷ 1.96 = 716.3, γ = 1.416, critical ratio 0.526 below the back ratio r = 101 325 ÷ 150 000 = 0.6755, so the flow is subsonic: flux = p × √(2γ ÷ ((γ − 1)·R_mix·T)) × √(r^(2/γ) − r^((γ+1)/γ)) = 150 000 × 7.715 × 10⁻³ × √(0.5745 − 0.5120) = 150 000 × 7.715 × 10⁻³ × 0.2500 = 289 kg/(s·m²) at 384.5 K. Through 2.0 × 10⁻⁵ m² that is 5.8 g/s of mixture, worth 5.8 × 297.7 ÷ 461.5 = 3.7 g/s of steam, which carries 3.7 × 10⁻³ × L_v(384.5) = 3.7 × 10⁻³ × 41 218 = 154 W of latent heat; as the air purges it rises toward the pure-steam 150 000 × 5.308 × 10⁻³ × 0.3012 = 239.8 kg/(s·m²), 4.8 g/s. Boil rates at the relief state: the plate at most 120 ÷ 41 218 = 2.9 g/s; the Fire bowl, whose box the pot's 0.16 × 0.16 m bounds cover by 0.16 (G 0.64 W/K), gives 228 + 0.64 × 96.5 = 0.895·x, x = 323.8 K of rise, 0.64 × (323.8 − 96.5) = 145 W into the pot, of which the pot loses 0.35 × 96.5 = 34 W to the room: 111 W, 2.7 g/s; the Candle under 70 W, 1.7 g/s; a Thermal storage block at its 400 K reference start reaches the relief near 391 K (3.9 kJ taken from 420 J/K is 9.3 K), passing 8.53 W/K (12.8 in series with 25.6) × 6.2 K = 53 W, 1.3 g/s. For each the relief holds the head at its crack. One tick of relief flow is 5.8 ÷ 120 = 0.048 g of the 1.96 g in the head, 2.5% of its gas; venting adiabatically drops the pressure about 1.416 × 2.5% = 3.5% (about 5200 Pa), which shuts the relief again, as A3's boundary requires. Between openings the head rises by at most one tick of boiling, 2.9 ÷ 120 = 0.024 g of steam, 0.024 × 10⁻³ × 461.5 × 384.5 ÷ 0.0015 = 2800 Pa, inside A3's 5000 Pa tolerance. A block hotter than about 399 K when the relief opens (384.5 + 154 ÷ 10.67 at fill 1; 402 K at the default fill 0.4, 384.5 + 154 ÷ 8.53) boils faster than the relief passes and the head rises above 150 000 Pa until the relief's flow matches the boil: the limit TH-17 item 6 accepts.
- **Condense row.** As TH-17: it moves nothing in these rows. Before the boil there is no vapour; boiling with the spout open the wall runs 1.64 K above 373 K, above any dew point the head can have; capped, the vapour's 14 730 Pa partial pressure is below the table's first point, so T_dew = 333.2 K against a wall at or above 384.5 K. It can act only after the Battery is empty, where no row asserts.
- **Tick order.** As TH-17 (T8).
- **Tolerances.** T12; liquid and vapour as mass on the stores' ledgers, 0.2 g (not L17 volume).
- **Run length these rows need: 150 s** (A5's stall guard is 146 s; ENGINE-RUN-LENGTH, owner decisions 13 and 17; default 30 s, maximum 300 s).

### Rows

Planned class `CoffeePotTests`. Heat source: a TH-04 plate wired to a Battery of the stated `capacity`, the pot standing on the disc.

| # | Given / When / Then | Planned test | Control | Boundary | Tolerance |
| --- | --- | --- | --- | --- | --- |
| A1 | Given a default pot on the plate, Battery `capacity` 14 400 J, then no vapour leaves `GasOut` before 52 s (61.1 s − 15%) and 2 g has left by the stall guard (hand 63.9 s) | `AHeatedPotEmitsVapourOnlyAfterItBoils` | an empty pot (`water` 0), same Battery: no vapour in 150 s | none | 0.2 g; stall guard 124 s |
| A2 | Given a default Battery (`capacity` 3600 J), when it is empty, then the water peaks near 339 K and the boil row never moves mass | `AnInsufficientlyHeatedPotEmitsNothing` | A1 | none | ± 2.6 K (T12: 5% of the 51 K rise); vapour mass 0 (no transfer occurs) |
| A3 | Given `spout` Capped and Battery `capacity` 14 400 J, then no vapour leaves `GasOut`; the head is above ambient before the boil; the relief opens (hand 75.8 s) and the head never exceeds 150 000 Pa | `ACappedSpoutHoldsPressureUpToTheRelief` | A1: an open spout keeps the head within 2000 Pa of ambient (hand 374 Pa) | the relief passes nothing while the head is below 145 000 Pa | 5000 Pa; stall guard 136 s |
| A4 | Given a pot and a kettle (TH-17) each on its own plate with Battery `capacity` 14 400 J, when each has boiled for 5 s, then the pot's head overpressure ÷ (its vent rate)² is four times the kettle's | `TheNarrowSpoutNeedsFourTimesTheDropForTheSameRate` | the kettle | none | 15% of the ratio |
| A5 | Given A1, when at most 1 g of water is left (hand 86 s), then liquid lost = vapour in `head` + vapour out | `LiquidLostEqualsVapourMade` | none | none | 0.2 g; stall guard 146 s |
| A6 | Reset restores `water`, 288 K and ambient air; a second Run repeats A1 | `ResetRestoresWaterAirAndTemperature` | none | none | as A1 |
| A7 | Save and load keep pose, `water` and `spout`; no run state | `SaveAndLoadKeepTheChargeAndNoRunState` | none | none | canonical field values |
| A8 | Out-of-range `water` is clamped; a non-Fluid link to `GasOut` is refused | generic: TH-04 R6, R5 | — | 0 and 0.003125 admitted | typed refusal |

## 6. Open questions

Resolved:

1. **Distinctness.** A family member of the Kettle by declared values, one additional collider and art (decided now from the item-by-item check in section 1; audit section 5).
2. **Perk behaviour.** No liquid recirculation: the row asks only for steam output (D19).
3. **Relief.** 150 000 Pa, decided now so that the plate reaches it in 76 s within a 14 400 J Battery.
4. **Game scale, mixture, saturation, capped spout, closed-head boiling.** As TH-17 items 1, 3, 5, 7 and 9.
5. **Spout comparison.** A4 compares overpressure per rate squared between two boiling vessels, decided now: a "same head state" for both cannot be authored, while two plates give it directly.

Needs the owner: none.

## 7. Integration

- **Works with.** TH-04, TH-26 (heat); TH-18, TH-19 and a receiver (vapour), through the gas family's hose, which is a solid strand and blocked on ENGINE-CONNECTION-MATERIALS (solid strand; owner decision 1); rows A1 to A8 use none.
- **Campaign.** Introduction 86, practice 87; reuse 100, 140; each such level declares a Run length of at least 150 s and a 14 400 J Battery. A fixed pot needs the lesson-fixture story ([S8](../invest/delivery-batches.md#s8-lesson-fixtures-for-any-admitted-package)), which lands after the engine programme (owner decision 22).
- **Coverage.** Section 10.

## 8. Delivery

- **Slice shape.** Family slice "Steam vessels" with TH-17. If delivered after TH-17, its own slice: declaration data, one additional collider and art, no capability.
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), which comes first: the programme; ENGINE-GAS-NETWORK, ENGINE-LIQUID, ENGINE-THERMAL as amended (TH-17 section 8, including the ENGINE-LIQUID zero-flow note); ENGINE-RUN-LENGTH; TH-04; TH-17 (same slice or earlier); S7.
- **Files.** `elements/CoffeePot/CoffeePot.csproj`, `CoffeePotElementDeclaration.cs`, `coffee_pot.tscn`, `icon.svg`; one guarded line in `CuriousContraptions.slnx`; `CuriousContraptions.tests/CoffeePotTests.cs`; a row in `VisibleSolidColliderTests`; the binding section once S7 allows it.
- **Review tier.** Data-only once TH-17 has proved the rows (no capability is added; the extra collider is checked by `VisibleSolidColliderTests`).
- **Disqualifiers.** Any capability the pot needs that the Kettle did not (then it is not a family member); a throat law in which, at equal pressures, rate does not scale with area.

## 9. Engine conformance (22 November 2026)

1. **Declaration data only.** Three colliders, one liquid store (also a node), one gas store, one node, two wetted links, two phase rows (boil; wall-limited condense), two gas paths (spout, relief), one port, two parameters, three bindings.
2. **Generic laws.** The Kettle's; no "coffee-pot event".
3. **Three layers.** Colliders: body, handle, spout. Visuals: their art, plus decorative lid, window and cap. Animation: three bindings from committed state.
4. **Flow and typing.** One Fluid output; f32; a Choice.
5. **Flat state.** The counts in section 2.
6. **Performance.** Three primitive colliders (the handle is a proud visible solid); no sensor; bounded thermal work (T10). No island sleeping is assumed; none exists in the engine today.
7. **Tests are scenarios.** A1 to A7.

**Verdict: Conforms; blocked on ENGINE-GAS-NETWORK, ENGINE-LIQUID, ENGINE-THERMAL and ENGINE-RUN-LENGTH and on the engine programme (owner decision 3a).**

## 10. Coverage binding (planned, not yet live)

Not written as main's binding section. **Blocked on the coverage follow-on story S7:** main's reader (`tools/Coverage/NamedPackageSources.cs`) lists only `EL-*.md` files and admits only an `EL-NNN` identity.

| Field | Planned value |
| --- | --- |
| Package | `coffee_pot` |
| Named identity | TH-35, `../invest/named-elements.md#thermal-35` |
| Acceptance | none resolvable today; the row's own task is `sequence-task-469` and the ledger entry has no todo link |
| Decision | S600; `scope-corrections.md` on published main has no `s600` block |
| Mode records | `Spout=Open`, `Spout=Capped` |
