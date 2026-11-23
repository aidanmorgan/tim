# TH-19 · Steam piston — named-identity readiness spec

**Current delivery (24 October 2026):** [TH-19 delivery slice](../invest/named-elements.md#thermal-19). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Rewritten on 22 November 2026 as one current text against checkout `spec-drafts` (published main `8d1d68aa`), and repaired the same day against the audit `docs/planning/elements/readiness/audits/elements-batch-n.md` (E1, N6, the blocked-exhaust band) and owner decisions 17 to 20 and coordinator decisions 16 and 21. Code citations are `path:line` in that checkout; `path@a6c914e` citations appear only in section 4. Nothing was built or run: every number is a hand calculation shown here and a starting value under decision D1 of [delivery batches](../invest/delivery-batches.md#3-family-level-decisions). Thermal conventions T1 to T14 are owned by [TH-04](TH-04-electrical-heating-plate.md#thermal-conventions); steam values and probe rows W1 to W11 by [TH-17](TH-17-kettle.md); gas conventions by [EL-039](EL-039-air-reservoir.md) and [Batch H](readiness/batch-h.md).

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name / type | TH-19 · Steam piston · Thermal |
| Anchors | [requirements.md#thermal-19](../requirements.md#thermal-19) (task `sequence-task-453`); [named-elements.md#thermal-19](../invest/named-elements.md#thermal-19) |
| Owner key / research | S583 · [TH-S07 heat to mechanical work](../../thermal-component-research.md#th-s07) |
| Related identities | The same chamber-on-a-slider capability as [EL-230](EL-230-spring-return-pneumatic-piston.md) and [EL-231](EL-231-double-acting-pneumatic-piston.md). Vapour from [TH-17](TH-17-kettle.md) or [TH-35](TH-35-coffee-pot-steam-vessel.md); exhaust to [TH-18](TH-18-condenser.md). No CAT refines it. |
| Campaign | Intro 75; practice 76; reuse 86, 143 ([thermal allocation](../requirements.md#thermal-campaign-allocation)) |
| Status | Not started. Blocked on the engine programme (owner decision 3a, 22 November 2026); ENGINE-GAS-NETWORK; ENGINE-CONNECTION-MATERIALS (solid strand: every row uses a hose, owner decision 1); ENGINE-RUN-LENGTH (rows need 240 s); and, for the rig only, ENGINE-LIQUID and ENGINE-THERMAL with TH-04 and TH-17. |

### Disposition

**To deliver as a single element, after EL-230.** It configures the general pressure actuator and adds no law. It is not a duplicate of, or a family member with, either pneumatic piston. Exact delta: it has the spring of EL-230 **and** the two chambers of EL-231; its ports are `GasIn` (Input end) into the cap chamber and `GasOut` (Output end, declared Vent when unconnected, F4) from the rod chamber, both fluid ports of the one record with role Bidirectional (EL-039 F3); its chamber area is 2.0 × 10⁻³ m² (five times theirs) so that a kettle's low pressure moves it; and its chambers admit a store with a vapour component. Planned package id `steam_piston`, directory `elements/SteamPiston/`, display name "Steam piston".

## 2. Declaration

Follows the delivered Powered gate's sprung slider (`elements/PoweredGate/PoweredGateElementDeclaration.cs:76`), as EL-230 does.

- **Interfaces.** `ISliderElement` (`CuriousContraptions.Abstractions.Mechanical/MechanicalContracts.cs:15`), `ISceneGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`), and the chamber and port contracts of ENGINE-GAS-NETWORK.
- **Barrel (body 0, static).** `col_barrel`: cylinder, axis X, centre (0, 0.20, 0), radius 0.12, length 0.80. `col_foot_a`, `col_foot_b`: boxes, centres (±0.30, 0.04, 0), half extents (0.05, 0.04, 0.12).
- **Rod and head (body 1, dynamic, 0.3 kg).** `col_rod`: cylinder radius 0.03, length 0.70; `col_head`: cylinder radius 0.10, thickness 0.06, at the rod's outer end. Material friction 0.3, restitution 0.1.
- **Slider.** Along +X; limits 0 to 0.40 m; spring 100 N/m toward 0; damping 8 N·s/m; no drive.
- **Chambers.** `cap` behind the piston: volume 1.0 × 10⁻⁴ + 0.002·q m³. `rod` in front of it: 1.0 × 10⁻⁴ + 0.002·(0.40 − q) m³. Both act on the slider with area 0.002 m² in opposite senses; both start as ambient air and admit vapour (W5). Net force on the rod = (p_cap − p_rod) × 0.002 − 100·q.
- **Ports.**

  | Port | Domain | Direction | Position (m) | Opens into |
  | --- | --- | --- | --- | --- |
  | `GasIn` | Fluid | Input | (−0.40, 0.20, 0), facing −X | `cap` |
  | `GasOut` | Fluid | Output; vents when unconnected | (0.30, 0.34, 0), facing +Y | `rod` |
- **Thermal.** No node: the barrel stores and passes no heat (T2), so steam does not condense in it; its chambers are gas stores whose energy is their own (T2).
- **Parameters.** None (D13).
- **Presentation bindings.** The rod follows body 1's pose. `Visual/Mark_In` and `Visual/Mark_Out` albedo ← committed pressure of `cap` and of `rod` (101 325 to 200 000 Pa mapped navy to cyan).
- **Palette.** `ElementCategory.Motion`, as the pneumatic pistons. Order: 158 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)).

### Art and solidity

| Art node | Mesh | Class | Moved by |
| --- | --- | --- | --- |
| `Visual/Barrel`, `Visual/Foot_A`, `Visual/Foot_B` cream `#fff8e9` | cylinder and boxes as the colliders | solid, `col_barrel`, `col_foot_*` | nothing |
| `Visual/Rod`, `Visual/Head` gold `#f7cb52` | cylinders as the colliders | solid, `col_rod`, `col_head` | body 1 pose |
| `Visual/Mark_In` (one chevron), `Visual/Mark_Out` (two chevrons) navy `#293954` | embossed look, printed flush at each port; ports are scene locators | decorative: printed flat, recoloured by a binding | presentation binding |

Icon: a cylinder with a rod and two chevron marks. It differs from EL-230 (a visible coil) and EL-231 (two ring marks, no chevrons) by the chevrons; its return spring is inside the barrel and not drawn.

### Compiled counts

Bodies 2 (1 static, 1 dynamic); colliders 5 (3 cylinders, 2 boxes, as EL-230); prismatic joints 1; drives 0; sensors 0; gas chambers 2; ports 2; thermal nodes 0; presentation bindings 2.

## 3. Engine capabilities

### Capability probe

| # | Capability | State | Symbol or owner | Test |
| --- | --- | --- | --- | --- |
| TH-04 R1, R5 | Colliders from art; typed ports | present | as TH-04 | as TH-04 (bodies read) |
| P1 | A sprung passive slider that returns when unloaded | present, untested here | `PrismaticSliderDescriptor`, `CuriousContraptions.Abstractions/MechanicalContractValues.cs:104` | `DeclaredPrismaticDriveTests.ThePoweredGateOpensToItsTargetWithinItsDeclaredSpeedEffortAndPowerOnEverySubstep` (`CuriousContraptions.tests/DeclaredPrismaticDriveTests.cs:92`; opening lines read, assertions not) |
| TH-17 W2 | One fluid port contract | narrower | as TH-17 | none for a network; owner ENGINE-GAS-NETWORK |
| P2 | Two gas chambers on one prismatic coordinate doing pressure work paid from their gas; lines; a venting output | **missing** | ENGINE-GAS-NETWORK ("a declared chamber drives a declared prismatic or revolute coordinate through pressure work paid from the store") | — |
| TH-17 W5 | A chamber holding carrier gas and vapour | **missing** | ENGINE-GAS-NETWORK (the mixture of pneumatic P1) | — |
| TH-17 W10 | Committed pressure as a presentation driver | **missing** | ENGINE-GAS-NETWORK | — |
| TH-18 C2 | A colliding hose | **missing** | ENGINE-CONNECTION-MATERIALS | — |

Counts: present 2 (R1, R5), untested 1 (P1's assertions not read), narrower 1, missing 4. It needs nothing from ENGINE-THERMAL of its own; its sources do. Nothing here needs code that names this part.

## 4. Sources and legacy

- **Requirement row** [thermal-19](../requirements.md#thermal-19): catalogue assembly of the generic pressure actuator with vapour-capable seals, inlet and exhaust; no steam-specific force callback; no variants.
- **Named entry** [thermal-19](../invest/named-elements.md#thermal-19): owner S583; "no pressure difference, blocked exhaust or excessive load prevents the predicted stroke".
- **Research** per-element table: "chamber + slider body"; parameters stroke, load; rod ← slider.
- **Element map** row TH-19: EnvironmentState, FiniteLedger, GasState, JointConstraint, PressureWork, RigidBodyDynamics (D14).
- **Processes and scenarios.** [IX-09](../requirements.md#interaction-09), [IX-40](../requirements.md#interaction-40), [IX-08](../requirements.md#interaction-08); [TX-04](../requirements.md#thermal-scenario-04).
- **Legacy harvest** (sealed-gas chamber foundation; no part consumed it).

  | # | Fact | Citation | Disposition |
  | --- | --- | --- | --- |
  | 1 | Chamber geometry V(q) = V₀ + A_signed·(q − q₀); volume must stay positive. | `engine/physics/AxialGasGeometry.cs@a6c914e:L5-L33` | Carry forward. |
  | 2 | Chamber effort at rest = A·p; finite-interval effort from the adiabatic energy difference; both orientations share the law. | `engine/physics/AxialGasPotential.cs@a6c914e:L36-L73` | Carry forward the law; f32. |
  | 3 | Oracle: A = 0.1 m², gas 120 000 Pa; travel 0.3 m to V = 0.04 m³ gives U 2400 → 1200 J and effort × travel = 1200 J. | `CuriousContraptions.tests/AxialGasPotentialTests.cs@a6c914e:L7-L28` | Carry forward as test oracle. |
  | 4 | A chamber binds only to an owned slider; inventory owned by a slider participant. | `engine/physics/AxialGasLoad.cs@a6c914e:L27-L45` | Carry forward as declaration validation. |
  | 5 | Gas drives shared motion: U + kinetic energy conserved (240 J), motion sign follows signed area, exact replay after restore. | `CuriousContraptions.tests/GasChamberWorldTests.cs@a6c914e:L32-L65` | Carry forward as acceptance facts. |
  | 6 | A collision cuts compression work and both domains restore exactly. | `CuriousContraptions.tests/GasChamberWorldTests.cs@a6c914e:L67-L81` | Carry forward: blocked load stops the stroke. |
  | 7 | Equal opposed chambers hold without spending inventory; inventory without a mechanical binding supplies no motion. | `CuriousContraptions.tests/GasChamberWorldTests.cs@a6c914e:L116-L144` | Carry forward: no pressure difference, no stroke. |
  | 8 | Unsupported motion rolled back the whole step with an exception. | `CuriousContraptions.tests/GasChamberWorldTests.cs@a6c914e:L103-L114` | Do not carry forward: game-grade clamp-or-continue. |
  | 9 | Interval work-error acceptance by quadrature. | `engine/physics/GasPredictionWork.cs@a6c914e:L7-L54` | Do not carry forward: CPU prediction certificate. |

  Files harvested: `engine/physics/AxialGasGeometry.cs`, `engine/physics/AxialGasPotential.cs`, `engine/physics/AxialGasLoad.cs`, `engine/physics/GasPredictionWork.cs`, `CuriousContraptions.tests/AxialGasPotentialTests.cs`, `CuriousContraptions.tests/GasChamberWorldTests.cs`. Consulted for state laws: `engine/physics/SealedGasState.cs@a6c914e:L43-L59` (adiabatic volume work).

## 5. Acceptance

Point of truth: [thermal-19](../requirements.md#thermal-19), [TX-04](../requirements.md#thermal-scenario-04) and [ELEMENT acceptance](../requirements.md#accept-element). Chrome proof is pending the post-catalogue phase.

### Behaviour and numbers

- **Law.** Pressure work on a declared coordinate: force = (p_cap − p_rod) × A − k·q; the work done is taken from the chamber gas (legacy facts 2, 5).
- **The rig is a closed gas system.** Kettle head (0.002 m³), hose (4.0 × 10⁻⁴ m³, gas convention F10) and `cap` (1.0 × 10⁻⁴ + 0.002·q) share one gas: 2.45 + 0.49 + 0.12 = 3.07 g of air at the start, m·R = 3.07 × 10⁻³ × 287 = 0.880 J/K. Nothing leaves it except through the kettle's relief. Only `rod` vents, through `GasOut`.
- **Pressure for a full stroke.** Spring at 0.40 m: 40 N; 40 ÷ 0.002 = 20 000 Pa over ambient, 121 325 Pa absolute (T_sat 377.7 K). A kettle can hold up to its 200 000 Pa relief, which is (200 000 − 101 325) × 0.002 = 197 N.
- **Warm air moves the rod before the boil.** Balance p = 101 325 + 50 000·q with p·ΣV/T = 0.880. With the kettle head at 373 K and the hose and `cap` still at 288 K: (101 325 + 50 000q)(7.10 + 6.94q) × 10⁻⁶ = 0.880 gives q = 0.145 m; with all the gas at 373 K, q = 0.214 m. At a default Battery's 344 K the two bounds are 0.103 and 0.144 m. So the rod creeps 0.10 to 0.21 m on warm air alone and never reaches 0.38 m without steam.
- **Vapour for a full stroke.** At q = 0.40 the system holds 0.0033 m³ at 121 325 Pa: Σm·R = 121 325 × 0.0033 ÷ T. At 378 K that is 1.059 J/K, so steam adds (1.059 − 0.880) ÷ 461.5 = 0.39 g; with the hose and `cap` gas at 330 K it is 0.52 g. The kettle boils from about 375 K (T_sat at the 109 000 to 112 000 Pa the warm air already holds, hand 54.7 s) and must warm 34.9 × 2.7 = 94 J more to 377.7 K, plus about 20 J of latent heat (0.5 g × 40.4 J/g): 114 J at the 46.7 W the water takes (TH-17 chain 1) is 2.5 s. Full stroke at about 57 s (6.9 kJ).
- **Energy.** Work at full stroke: spring ½ × 100 × 0.40² = 8 J plus 101 325 × 0.002 × 0.40 = 81 J pushing the atmosphere out of `rod`: 89 J, against about 0.45 g × 1153.2 × 378 = 196 J of vapour enthalpy plus the warm air's share. Work never exceeds the energy the gas gave up (A5).
- **Blocked exhaust.** With `GasOut` on a hose whose free end is Sealed, `rod` (9.0 × 10⁻⁴ m³ at rest) and that hose (4.0 × 10⁻⁴ m³) together hold 1.3 × 10⁻³ m³ of ambient air, compressed as the piston advances: V = 13 − 20·q in 10⁻⁴ m³. With `cap` at the kettle's 200 000 Pa the forces balance where (200 000 − p_rod) × 0.002 = 100·q. Isothermal, p_rod = 101 325 × 13 ÷ (13 − 20q): q = 0.29 m (p_rod 185 000 Pa). Adiabatic (air, γ 1.4, as the barrel has no node), p_rod = 101 325 × (13 ÷ (13 − 20q))^1.4: q = 0.23 m (p_rod 189 000 Pa); with `cap` 5 000 Pa short of the relief, 0.225 m. The audit's "0.19 to 0.33 m" is the adiabatic and isothermal answers widened by 0.04 m each (the 0.02 m travel tolerance and the cap pressure's 5 000 Pa band), and is adopted. The earlier "0.14 to 0.26 m" ignored the hose. The cap reaches the relief after the kettle has warmed to 393.4 K and made about 1.35 g of steam: hand about 75 s, as TH-17's capped spout.
- **Excessive load.** A Wall 0.20 m in front: the head stops at contact; pressure rises to the kettle's relief; the Wall sees at most 197 − 100 × 0.20 = 177 N.
- **No pressure difference.** A kettle on a disabled Battery leaves both chambers at ambient: zero force, no motion.
- **Return.** With Battery `capacity` 9000 J the plate stops at 75 s with the kettle near 392 K and about 19 g of water left. The kettle and disc then cool with time constant 62.7 s (TH-17 chain 1): 288 + 104 × e^(−t ÷ 62.7). The gas in `cap` and the hose flows back into the kettle head through the kettle's two-way throat (TH-17 section 2, F3), and its vapour returns to the water only by the kettle's condense row, T5's one wall-limited row on the `wall`↔`head` link (G = 2 W/K): while the wall is below the dew point of the head's vapour partial pressure, ṁ = 2 × (T_dew − T_wall) ÷ L_v(T_dew). At the stop the system holds about 193 000 Pa, of which the air is at most 0.880 × 392 ÷ 0.0033 = 104 500 Pa, so the vapour's partial pressure is at least 88 500 Pa and T_dew = 354.5 + (88 500 − 50 000) ÷ 51 325 × 18.5 = 368.4 K, below the 392 K wall: nothing condenses until the wall (held near the water by the 61 W/K wetted link at 19 g) has cooled past it, 62.7 × ln(104 ÷ 80.4) = 16 s after the stop. From then the row keeps the vapour at the saturation pressure of the wall within about 0.5 K: at a 343 K wall the vapour (about 0.70 g at 33 700 Pa in 0.0033 m³) must shrink by about 0.026 g/s as the wall cools 0.875 K/s, which the row moves at T_dew − T_wall = 0.026 × 10⁻³ × 107.2 × 343 ÷ 2 = 0.47 K. Once the wall passes the table's 333.2 K floor, 62.7 × ln(104 ÷ 45.2) = 52 s after the stop, the dew point stays at 333.2 K and the last vapour (about 0.43 g at 20 000 Pa) condenses within about 5 s: the wall falls 0.72 K/s, so ṁ ≈ 2 × 0.72·t ÷ 35 720 kg/s and 2.0 × 10⁻⁵·t² kg reaches 0.43 g at t = 4.6 s. The air alone then holds the rod at p·ΣV = 0.880 × T: q = 0.12 m at 333 K and 0.02 m at 296 K (165 s after the plate stops, at 240 s). With 14 400 J instead, the kettle boils dry through its relief by about 106 s; the vapour left has no liquid to return to and the rod stays out. That is why the return row names 9000 J.
- **Energy budget.** Every row names its Battery `capacity` (`elements/Battery/BatteryElementDeclaration.cs:42`, 120 W limit at `:44`): a default 3600 J empties at 30 s with the water near 344 K, before any steam.
- **Tick order.** Gas transfer and chamber work run in the substeps (the delivered gas advance site, `CuriousContraptions.Simulation/WorkshopPhysicsEngine.cs:994`); the source's boil row adds vapour at the tick boundary (T8).
- **Tolerances.** Travel 0.02 m (5% of stroke); pressure 5000 Pa; energy 2% or 5 J; vapour as mass on the ledger, 0.2 g. A process whose hand time exceeds 240 s is asserted by direction (T12); the return completes inside 240 s by hand but is asserted by direction because its end state depends on the adiabatic hose gas.
- **Run length these rows need: 240 s** (A6 observes at 240 s; A1's stall guard 117 s, A3's 135 s; ENGINE-RUN-LENGTH, owner decisions 13 and 17; default 30 s, maximum 300 s).

### Rows

Planned class `SteamPistonTests`. Rig: a TH-17 kettle on a TH-04 plate wired to a Battery of the stated `capacity`, a hose from the kettle's `GasOut` to `GasIn`.

| # | Given / When / Then | Planned test | Control | Boundary | Tolerance |
| --- | --- | --- | --- | --- | --- |
| A1 | Given the rig with Battery `capacity` 14 400 J, then the rod reaches at least 0.38 m by the stall guard (hand 57 s), and only after the kettle's boil row has first moved mass | `SteamExtendsTheRodOnlyAfterTheKettleBoils` | a disabled Battery (`enabled` Disabled): no pressure difference, travel under 0.01 m | a default Battery (`capacity` 3600 J): warm air moves the rod (hand 0.10 to 0.14 m) but it stays below 0.20 m | 0.02 m; stall guard 117 s |
| A2 | Given A1 with a Bowling ball resting against the head, then the ball is pushed at least 0.3 m | `TheRodPushesALoad` | A1's control: ball unmoved | none | 0.05 m; stall guard 117 s |
| A3 | Given `GasOut` on a Sealed hose and Battery `capacity` 14 400 J, when the kettle's relief has opened (hand 75 s), then the rod has stalled between 0.19 and 0.33 m | `ABlockedExhaustStallsTheStroke` | A1 (full stroke) | none | the stated band; stall guard 135 s |
| A4 | Given a Wall 0.20 m ahead and Battery `capacity` 14 400 J, then the head stops at the Wall and never passes it; `cap` pressure stays at or below 200 000 Pa | `AnImmovableLoadStopsTheRod` | A1 | none | 0.01 m; 5000 Pa |
| A5 | Given A1 at full stroke, then the gas energy given up equals spring energy + work on the atmosphere and load + kinetic energy, and vapour made = vapour held + vapour out through the relief | `ChamberWorkIsPaidFromTheGas` | none | none | 2% or 5 J; 0.2 g |
| A6 | Given the rig with Battery `capacity` 9000 J (empty at 75 s, the kettle still wet), then after the Battery is empty the rod never extends more than 0.02 m further, and by 240 s it has moved back below 0.20 m (hand 0.02 m) | `TheRodReturnsWhenTheSourceCools` | A1 with 14 400 J (dry kettle): the rod stays above 0.30 m | none | direction only (T12); 0.02 m |
| A7 | Reset restores the rod at 0, ambient air in both chambers and the hoses; a second Run repeats A1 | `ResetRestoresRodAndChambers` | none | none | as A1 |
| A8 | Save and load keep pose and hoses; no run state | `SaveAndLoadKeepConnections` | none | none | canonical field values |
| A9 | A non-Fluid link to `GasIn` or an output-to-output link is refused | generic: TH-04 R5 once Fluid links are validated | — | — | typed refusal |

No parameters, so no parameter-refusal row. The requirement's three controls are A1's control (no pressure difference), A3 (blocked exhaust) and A4 (excessive load).

## 6. Open questions

Resolved:

1. **Valve gear.** None. Decided now: an exhaust chamber on the rod side replaces the earlier position-uncovered port. It needs no seat driven by a coordinate, it makes a blocked exhaust stall the stroke as the requirement says, and the rod returns by its spring when the source pressure falls.
2. **Condensation in the chamber.** None: the barrel declares no thermal node (D19). Vapour returns to liquid only in the source's own store, by its condense row.
3. **Port ids.** `GasIn` (Input end) and `GasOut` (Output end), the gas family's names for one inlet and one outlet (convention F3); like every gas port both have role Bidirectional, and flow follows pressure.
4. **Gas chamber schedule.** ENGINE-GAS-NETWORK, then EL-230, then this part (D16).
5. **Game scale.** D5 and owner decision 19, TH-04 section 11.
6. **Warm-air creep.** Accepted, decided now: the closed hose makes heated air push the rod 0.10 to 0.21 m before any steam. It is the gas law, not a defect, and A1 asserts the order (full stroke only after boiling) instead of "no motion before the boil".

Needs the owner: none.

## 7. Integration

- **Works with.** TH-17, TH-35 (vapour); TH-18 (exhaust); Bowling ball, Wall, Receiver (delivered loads). Every gas connection is a hose: a solid strand (owner decision 1).
- **Campaign.** "Full Steam Ahead": introduction 75, practice 76; reuse 86, 143; each such level declares a Run length that covers its rows (240 s for the return lesson) and a Battery of 9000 or 14 400 J. A fixed piston needs the lesson-fixture story ([S8](../invest/delivery-batches.md#s8-lesson-fixtures-for-any-admitted-package)), which lands after the engine programme (owner decision 22).
- **Coverage.** Section 10.

## 8. Delivery

- **Slice shape.** Single element.
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), which comes first: the programme; ENGINE-GAS-NETWORK as amended (owner of the one fluid port contract, coordinator decision 16); EL-230; ENGINE-LIQUID and ENGINE-THERMAL with TH-04 and TH-17 (for the rig, not for this package); ENGINE-CONNECTION-MATERIALS; ENGINE-RUN-LENGTH; S7.
- **Files.** `elements/SteamPiston/SteamPiston.csproj`, `SteamPistonElementDeclaration.cs`, `steam_piston.tscn`, `icon.svg`; one guarded line in `CuriousContraptions.slnx`; `CuriousContraptions.tests/SteamPistonTests.cs`; a row in `VisibleSolidColliderTests`; the binding section once S7 allows it.
- **Review tier.** Data-only if EL-230 and EL-231 proved one and two chambers on a slider; otherwise engine.
- **Disqualifiers.** A gas network that allows one chamber per coordinate; no venting output; chambers that refuse a vapour component; a hose without the F10 line volume (re-derive the blocked band); a forward-only source throat (A6's return needs gas to flow back into the kettle head, TH-17 W4).

## 9. Engine conformance (22 November 2026)

1. **Declaration data only.** Five colliders on two bodies, one sprung slider, two chambers, two ports, two bindings. No force callback.
2. **Generic laws.** Pressure work on a declared coordinate; the spring law; conserved gas transfer. The fluid being steam changes no rule.
3. **Three layers.** Colliders: barrel, feet, rod, head. Visuals: their art; printed port marks. Animation: mark colour from committed pressure; the rod is a body.
4. **Flow and typing.** Two Fluid ports of the one record (role Bidirectional; an Input and an Output authoring end); f32; enum roles.
5. **Flat state.** The counts in section 2.
6. **Performance.** The Powered gate's primitive count; no sensor; no scene-proportional work. No island sleeping is assumed; none exists in the engine today.
7. **Tests are scenarios.** A1 to A8.

**Verdict: Conforms; blocked on ENGINE-GAS-NETWORK, ENGINE-CONNECTION-MATERIALS and ENGINE-RUN-LENGTH, general capabilities stated element-free, and on the engine programme (owner decision 3a).**

## 10. Coverage binding (planned, not yet live)

Not written as main's binding section. **Blocked on the coverage follow-on story S7:** main's reader (`tools/Coverage/NamedPackageSources.cs`) lists only `EL-*.md` files and admits only an `EL-NNN` identity.

| Field | Planned value |
| --- | --- |
| Package | `steam_piston` |
| Named identity | TH-19, `../invest/named-elements.md#thermal-19` |
| Acceptance | none resolvable today; the row's own task is `sequence-task-453` and the ledger entry has no todo link |
| Decision | S583; `scope-corrections.md` on published main has no `s583` block |
| Mode records | `Configuration=Fixed` |
