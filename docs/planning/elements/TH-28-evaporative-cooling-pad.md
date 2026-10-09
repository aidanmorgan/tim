# TH-28 · Evaporative cooling pad — named-identity readiness spec

Story 7.0 named-identity spec ([spec](../../../_bmad-output/implementation-artifacts/spec-7-0-element-implementation-readiness.md)). Citations are `path@a6c914e:Lstart-Lend`. No legacy holds evaporative-pad values, so every design value without a cited source is marked "(proposed: …)" with a one-line justification; the owner may revise it.

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name | TH-28 · Evaporative cooling pad |
| Type | Thermal · potential element (potential/conditional; source adoption retained) |
| Anchors | [requirements.md#thermal-28](../requirements.md#thermal-28) (sequence-task-462); [named-elements.md#thermal-28](../invest/named-elements.md#thermal-28) |
| Proof owner / research | S590 · [TH-S08 evaporative cooling](../../thermal-component-research.md#th-s08) |
| Related identities | No CAT spec. Airflow from [CAT-028 Fan](CAT-028-fan.md); capillary feed shares the water family's wick law (Batches F and G; [S416](../invest/decisions.md#s416) capillary). Read by a [TH-21 Temperature sensor](TH-21-temperature-sensor.md); contrast [TH-10](TH-10-finned-heat-sink.md), which cannot go below ambient (TX-08). |
| Campaign | Intro 73; practice 74; reuse 88, 145 ([thermal allocation](../requirements.md#thermal-campaign-allocation)) |
| Roadmap story | Unscheduled. No epic in `_bmad-output/planning-artifacts/epics.md` schedules a thermal element. |
| Status | Not started. No `WorkshopPartKind` member (`engine/gpu/WorkshopPartKind.cs@a6c914e:L3-L3`); no legacy part. |

## 2. Declaration

- **Batch N game scale** (proposed, identical in all 37 TH specs; chains derived against it are in [TH-17](TH-17-kettle.md#batch-n-chains); owner question in section 6). Ambient reservoir 288 K and 101.3 kPa. [Research table](../../thermal-component-research.md) rows: specific heat, latent heat and source power read as listed range × scale (64–4096 J/(kg·K), 8192–2,097,152 J/kg, 0.5–2048 W); temperature (128–2048 K), conductance (2⁻¹⁰–2⁸ W/K), fuel/supply (finite), ignition threshold (400–1200 K) and sensor thresholds read as listed, without scale; expansion coefficient reads 2⁻²⁰–2⁻¹⁴ 1/K after scale, which TH-22 and TH-23 exceed on purpose as a declared exaggeration. Condensed-phase specific heat SI × ¼ and latent heat SI × 1/16, so catalogue-scale bodies change temperature or phase within one Run. Gas R and Cv SI × 1, so γ and pressure stay physical; each condensed phase's enthalpy is referenced so vapour made at T_sat carries m·Cv·T_sat and the scaled latent heat is exactly the heat consumed; because water's scaled latent heat (141 kJ/kg) is below R·T_sat (172 kJ/kg), boiling follows a declared saturation curve, not Clausius–Clapeyron. Every fuel's reaction energy and O₂ demand per kilogram SI × 1/1024 (wax 41,016 J/kg and 3.4/1024 kg O₂/kg; wood and cellulose 15,625 J/kg and 1.2/1024 kg O₂/kg), so a 0.3 kg candle holds about 12 kJ, heat per kilogram of O₂ keeps Thornton's ≈ 13 MJ/kg, and fuel mass is consumed 1:1 so burn-down stays visible. Still-air surface exchange h = 2.5 W/(m²·K) (SI × ¼, so condensed-body time constants C/(hA) stay SI-like while gas-node ones run about 4× slower than SI); forced air h = 2.5 + u (u in m/s). Contact pairs conduct G = G_a·G_b/(G_a + G_b) from each part's face conductance: metal 64, ceramic, wood, ice or soft 16, probe tip 0.5 W/K. Every exchange is clamped per 480 Hz substep to the equalising amount ΔT·C_a·C_b/(C_a + C_b), so small nodes never overshoot. Reactive surfaces carry a thermal-only patch node outside rigid mass (fibre 10 mg, wood 0.5 g; area 0.0025 m²; patch-to-bulk 0.01 W/K; thermal nodes 10⁻⁶–1024 kg, not bound by the 1/1024 kg rigid minimum) that takes strikes, flames and light first. Radiant optical power: 1 legacy intensity unit = 1 W, so the flashlight emits 24 W. Canonical f32 ([numeric contract](../../gpu-f32-physics.md#game-grade-envelope)).
- **Atmosphere.** Ambient relative humidity RH 0.5 by default; a level may author 0–1 (proposed: a temperate room; RH 1 is the saturated control).
- **Variants.** None named in the row.
- **Bodies and shapes.** One static body: upright ribbed wick box, half-extents 0.25 × 0.25 × 0.05 m, above a reservoir box with half-extents 0.25 × 0.08 × 0.10 m (proposed: a 0.5 m pad face sized to a Fan's jet).
- **Mass and material.** Static rigid mass zero (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L70-L76`). Pad thermal node 0.1 kg cellulose, c = 425 J/(kg·K) (cellulose 1700 × ¼, batch scale; 42.5 J/K), holding up to 0.01 kg of absorbed water (proposed), so about 53 J/K when wet. Reservoir water 0–0.05 kg, default 0.05 kg (proposed: lasts minutes under a Fan). Water vaporisation 141,250 J/kg (batch scale). Pad surface: both 0.25 m² faces, A = 0.5 m², exchanging h·A = 1.25 W/K in still air and (2.5 + u)·A under airflow (batch scale); pad face soft 16 W/K.
- **Constraints.** None.
- **Typed ports.** None as sockets. Airflow receiver: the pad face, sampled like other airflow receivers ([gas family](../../finite-gas-foundation.md)). Optional liquid inlet to refill the reservoir (proposed: water-family port type).
- **Sensors and activation.** None.
- **Work and energy stores.** Capillary feed reservoir → pad up to 1 × 10⁻³ kg/s while the reservoir has water (proposed: above the evaporation rate, so feed limits only when dry). Evaporation ṁ = β·A·(1 − RH)·(1 + 0.4·u) with β = 2 × 10⁻⁴ kg/(m²·s) (proposed: the airflow factor 1 + 0.4u equals (2.5 + u)/2.5, so mass transfer and surface exchange scale together, as the heat–mass analogy requires). At RH 0.5 that is 5 × 10⁻⁵ kg/s and 7.1 W in still air, and 2.9 × 10⁻⁴ kg/s and 41 W in a 12 m/s Fan jet, so the 0.05 kg reservoir lasts about 170 s under the Fan (derived). Latent heat L·ṁ leaves the pad node; liquid consumed = vapour produced, and the vapour joins the ambient reservoir. The pad's steady depression is L·β·(1 − RH)/2.5 ≈ 5.7 K at RH 0.5, so it settles near 282 K, the real wet-bulb at 288 K and RH 0.5; airflow changes the rate (wet time constant 42 s still, 7.3 s in the Fan jet), not the depth (derived). Saturated air (RH = 1) or a dry pad: zero evaporation.
- **Parameters.**

  | Parameter | Type | Range | Default | Unit | Source |
  | --- | --- | --- | --- | --- | --- |
  | Reservoir water | f32 | 0–0.05 | 0.05 | kg | (proposed: above) |
  | Evaporation coefficient β | f32 | fixed | 2 × 10⁻⁴ | kg/(m²·s) | (proposed: above) |
  | Airflow factor | f32 | fixed | 1 + 0.4u | — | (proposed: above) |
  | Capillary feed limit | f32 | fixed | 1 × 10⁻³ | kg/s | (proposed: above) |
  | Ambient RH | f32 | 0–1 | 0.5 | — | (proposed: above) |

  Fan flow speed 12 m/s is sourced from the [gas family parameters](../../finite-gas-foundation.md).
- **Cosmetic curves and UI bindings.** Damp fraction ← committed pad water ([research per-element table](../../thermal-component-research.md)); the visible wet/dry pattern follows it (row). Vapour is invisible; droplets and vapour stay distinct ([visual contract](../requirements.md#thermal-elements)).
- **Art.** "Cream ribbed wick above a cyan reservoir with a visible wet/dry pattern" (row). Nearest [DESIGN.md](../../../DESIGN.md) source colours (proposed mapping): cream `#ead39b` (`DESIGN.md@a6c914e:L146-L146`), cyan `#66b8c9` (`DESIGN.md@a6c914e:L172-L172`).
- **Catalogue and inventory entry.** New `WorkshopPartKind.EvaporativePad`; id `evaporative_pad`, title "Evaporative cooling pad", category Heat (proposed: no current category covers thermal parts).

## 3. Engine capabilities

Families (map row TH-28, [general-engine-element-map](../general-engine-element-map.md); binding [thermal-01.json](../../coverage/engine/thermal-01.json)): CapillaryTransport, EnvironmentState, FiniteLedger, FluidAdvection, GasState, GeometryQuery, LiquidEvaporation, PhaseTopology, SensibleHeat, ThermalConvection, TopologyTransaction; JSON adds StateTransaction.

- **Exists now.** Static body and box colliders (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L120`); Run/Reset and save (`engine/gpu/ActivationNetwork.cs@a6c914e:L67-L67`, `engine/gpu/WorkshopSaveCodec.cs@a6c914e:L6-L10`).
- **Missing.** Airflow receiver: Story 12.2. Capillary wick: [S416](../invest/decisions.md#s416) capillary → S421. Humidity and atmosphere: [S635](../invest/decisions.md#s635) environment, [S470](../invest/decisions.md#s470) gas-state → S471. [S543](../invest/decisions.md#s543): evaporation → S549, convection → S545. All thermal rows unscheduled.
- **Dependencies.** CAT-028 Fan (Story 12.2); water fill; TH-21 to read the pad.

## 4. Sources and legacy

- **Requirement row** [thermal-28](../requirements.md#thermal-28): wet porous body loses finite liquid by modelled surface mass transfer carrying latent energy; airflow and humidity affect the rate; no variants.
- **Named entry** [thermal-28](../invest/named-elements.md#thermal-28): owner S590; "a dry pad or saturated surrounding gas prevents equivalent cooling; liquid consumption balances vapor output".
- **Research** [TH-S08](../../thermal-component-research.md#th-s08) and per-element table: "wet node + water store"; parameter wetting; "cools airflow while wet"; damp fraction ← water.
- **Processes and scenarios.** [IX-23](../requirements.md#interaction-23), [IX-38](../requirements.md#interaction-38), [IX-19](../requirements.md#interaction-19); [TX-09](../requirements.md#thermal-scenario-09) ("evaporation cools a wet surface").
- **Legacy.** None found. Searched `parts/`, `engine/physics/`, `engine/*.cs`, `engine/bridge/`, `CuriousContraptions.tests/`, `content/puzzles.json`, `tools/Campaign` and `reference/` at a6c914e for evaporat, wick, humidity and pad terms. Consulted, no element knowledge: `reference/P0-022-before/docs/coverage/engine/task-002.json@a6c914e:L2783-L2787` (historical coverage-scope rows).

## 5. Acceptance outline

Point of truth: [thermal-28](../requirements.md#thermal-28), [TX-09](../requirements.md#thermal-scenario-09) and [ELEMENT acceptance](../requirements.md#accept-element).

- **Chrome recipe.** Through the real palette and sockets: a supplied Fan (CAT-028) blowing through a filled pad, a Temperature sensor (TH-21) probe on the pad face (pair 0.485 W/K).
- **Positive.** The pad wets and the probe falls below ambient, reaching about 283 K within about 20 s under the Fan; the reservoir loses about 0.29 g/s and reservoir loss = vapour produced. Without the Fan the pad reaches the same depth in about 100 s.
- **Negative or control.** Empty reservoir (dry pad) under the Fan: the probe stays at 288 K (TX-08: airflow alone never cools below ambient). Saturated atmosphere (RH 1 preset): stays at 288 K. Feed exhausted mid-Run: the pad dries and returns to 288 K.
- **Boundaries.** Evaporation never exceeds feed plus pad water; the pad never falls below T_amb − L·β·(1 − RH)/2.5; zero evaporation when RH = 1.
- **Run/Reset.** Restores reservoir fill, dry pad, temperatures and Fan exactly.
- **Save/Load.** Pose, fill and atmosphere round-trip; Run after reload reproduces the outcome.
- **Integrations.** TX-09; TX-08 comparison with TH-10; campaign 73, 74, 88, 145.

## 6. Open questions

1. **Batch N game scale.** Adopt or revise the one scale in section 2 (×¼ specific heat, ×1/16 latent heat, gas ×1, reaction energy and O₂ ×1/1024, h 2.5 W/(m²·K), per-pair series contact conductance from per-part face values, 1 intensity unit = 1 W, and which research-table rows the scale multiplies). It carries two consequences: boiling needs a declared saturation curve (S550) because scaled water latent heat is below R·T_sat, and gas-node time constants run about 4× slower than SI (TH-25). Owner decision.
2. **Humidity model.** Single ambient RH versus a tracked vapour species in the air. Owner decision (S549/S471).
3. **Mass-transfer law.** Linear β·(1 − RH), whose depth is fixed by β, versus a vapour-pressure difference law that rises with pad temperature. Owner decision (S549).
4. **Cooling another body.** Under the scale, latent heat ×1/16 against gas Cv ×1 cools the 3.6 kg/s Fan stream through the pad by only about 0.01 K, so the pad cools itself and bodies touching it, not the air. Accept, or require a measurable air-stream drop. Owner decision.
