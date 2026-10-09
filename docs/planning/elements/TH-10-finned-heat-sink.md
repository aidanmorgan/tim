# TH-10 · Finned heat sink — named-identity readiness spec

Story 7.0 named-identity spec ([spec](../../../_bmad-output/implementation-artifacts/spec-7-0-element-implementation-readiness.md)). Citations are `path@a6c914e:Lstart-Lend`. No legacy holds heat-sink values, so every design value without a cited source is marked "(proposed: …)" with a one-line justification; the owner may revise it.

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name | TH-10 · Finned heat sink |
| Type | Thermal · potential element (potential/conditional; source adoption retained) |
| Anchors | [requirements.md#thermal-10](../requirements.md#thermal-10) (sequence-task-444); [named-elements.md#thermal-10](../invest/named-elements.md#thermal-10) |
| Proof owner / research | S572 · [TH-S01 heat transfer and finite stores](../../thermal-component-research.md#th-s01) |
| Related identities | No CAT spec. Airflow from [CAT-028 Fan](CAT-028-fan.md) (TX-08); cold-side rejection for [TH-12](TH-12-reversible-heat-pump.md) and [TH-30](TH-30-thermoelectric-generator.md). |
| Campaign | Intro 40; practice 41; reuse 78, 87, 145 ([thermal allocation](../requirements.md#thermal-campaign-allocation)) |
| Roadmap story | Unscheduled. No epic in `_bmad-output/planning-artifacts/epics.md` schedules a thermal element. |
| Status | Not started. No `WorkshopPartKind` member (`engine/gpu/WorkshopPartKind.cs@a6c914e:L3-L3`); no legacy part. |

## 2. Declaration

- **Batch N game scale** (proposed, identical in all 37 TH specs; chains derived against it are in [TH-17](TH-17-kettle.md#batch-n-chains); owner question in section 6). Ambient reservoir 288 K and 101.3 kPa. [Research table](../../thermal-component-research.md) rows: specific heat, latent heat and source power read as listed range × scale (64–4096 J/(kg·K), 8192–2,097,152 J/kg, 0.5–2048 W); temperature (128–2048 K), conductance (2⁻¹⁰–2⁸ W/K), fuel/supply (finite), ignition threshold (400–1200 K) and sensor thresholds read as listed, without scale; expansion coefficient reads 2⁻²⁰–2⁻¹⁴ 1/K after scale, which TH-22 and TH-23 exceed on purpose as a declared exaggeration. Condensed-phase specific heat SI × ¼ and latent heat SI × 1/16, so catalogue-scale bodies change temperature or phase within one Run. Gas R and Cv SI × 1, so γ and pressure stay physical; each condensed phase's enthalpy is referenced so vapour made at T_sat carries m·Cv·T_sat and the scaled latent heat is exactly the heat consumed; because water's scaled latent heat (141 kJ/kg) is below R·T_sat (172 kJ/kg), boiling follows a declared saturation curve, not Clausius–Clapeyron. Every fuel's reaction energy and O₂ demand per kilogram SI × 1/1024 (wax 41,016 J/kg and 3.4/1024 kg O₂/kg; wood and cellulose 15,625 J/kg and 1.2/1024 kg O₂/kg), so a 0.3 kg candle holds about 12 kJ, heat per kilogram of O₂ keeps Thornton's ≈ 13 MJ/kg, and fuel mass is consumed 1:1 so burn-down stays visible. Still-air surface exchange h = 2.5 W/(m²·K) (SI × ¼, so condensed-body time constants C/(hA) stay SI-like while gas-node ones run about 4× slower than SI); forced air h = 2.5 + u (u in m/s). Contact pairs conduct G = G_a·G_b/(G_a + G_b) from each part's face conductance: metal 64, ceramic, wood, ice or soft 16, probe tip 0.5 W/K. Every exchange is clamped per 480 Hz substep to the equalising amount ΔT·C_a·C_b/(C_a + C_b), so small nodes never overshoot. Reactive surfaces carry a thermal-only patch node outside rigid mass (fibre 10 mg, wood 0.5 g; area 0.0025 m²; patch-to-bulk 0.01 W/K; thermal nodes 10⁻⁶–1024 kg, not bound by the 1/1024 kg rigid minimum) that takes strikes, flames and light first. Radiant optical power: 1 legacy intensity unit = 1 W, so the flashlight emits 24 W. Canonical f32 ([numeric contract](../../gpu-f32-physics.md#game-grade-envelope)).
- **Variants.** None named in the row.
- **Bodies and shapes.** One static body: contact foot box, half-extents 0.25 × 0.03 × 0.25 m, under a fin block box with half-extents 0.25 × 0.25 × 0.25 m (proposed: a 0.5 m footprint sits on a Heating plate or a TH-30 cold face; colliders are boxes only, `engine/gpu/PhysicsDeclarations.cs@a6c914e:L89-L120`). Declared wetted fin area 2.0 m² (proposed: about eight times the footprint, typical of a finned extrusion).
- **Mass and material.** Static rigid mass zero (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L70-L76`). Thermal node 1.0 kg aluminium, c = 225 J/(kg·K) (aluminium 900 × ¼, batch scale). Foot: metal face 64 W/K (batch scale), so it pairs at 32 W/K with a metal face.
- **Constraints.** None.
- **Typed ports.** None as sockets. Thermal port: the foot contact face. Airflow receiver: the fin volume, sampled like other airflow receivers ([gas family](../../finite-gas-foundation.md)).
- **Sensors and activation.** None.
- **Work and energy stores.** Convective link to the explicit ambient reservoir uses the batch law: G = (2.5 + u)·A·w for relative air speed u at the fins and exposure weight w ∈ [0, 1]. Still air gives 2.5 × 2.0 = 5 W/K; a 12 m/s Fan jet gives 14.5 × 2.0 = 29 W/K. Flow q = G·(T − 288 K): zero at equal temperature and never below ambient (IX-19).
- **Parameters.**

  | Parameter | Type | Range | Default | Unit | Source |
  | --- | --- | --- | --- | --- | --- |
  | Fin area | f32 | fixed | 2.0 | m² | (proposed: above) |
  | Convection law | f32 | fixed | 2.5 + u | W/(m²·K) | (batch scale) |
  | Foot face conductance | f32 | fixed | 64 | W/K | (batch scale: metal face) |

  Fan flow speed 12 m/s is sourced from the [gas family parameters](../../finite-gas-foundation.md).
- **Cosmetic curves and UI bindings.** None ([research per-element table](../../thermal-component-research.md)).
- **Art.** "Ordered cream architectural fins and a gold contact foot; no industrial clutter" (row). Nearest [DESIGN.md](../../../DESIGN.md) source colours (proposed mapping): cream `#ead39b` (`DESIGN.md@a6c914e:L146-L146`), gold `#f7cb52` (`DESIGN.md@a6c914e:L154-L154`).
- **Catalogue and inventory entry.** New `WorkshopPartKind.HeatSink`; id `heat_sink`, title "Finned heat sink", category Heat (proposed: no current category covers thermal parts).

## 3. Engine capabilities

Families (map row TH-10, [general-engine-element-map](../general-engine-element-map.md); binding [thermal-01.json](../../coverage/engine/thermal-01.json)): FiniteLedger, FluidAdvection, GeometryQuery, SensibleHeat, ThermalConduction, ThermalConvection, TopologyTransaction; JSON adds StateTransaction.

- **Exists now.** Static body and box colliders (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L120`); Run/Reset and save (`engine/gpu/ActivationNetwork.cs@a6c914e:L67-L67`, `engine/gpu/WorkshopSaveCodec.cs@a6c914e:L6-L10`).
- **Missing.** Airflow receiver sampling: Story 12.2 (CAT-028 Fan). Gas-side transfer model: [S416/S470](../invest/decisions.md#s470) (map note "liquid/gas boundary and coupled transfer model"). [S543](../invest/decisions.md#s543): convection → S545, conduction → S544; SensibleHeat (IX-43) has no S543 row. Unscheduled.
- **Dependencies.** A hot body (TH-26, TH-12 hot face); CAT-028 Fan for the forced case.

## 4. Sources and legacy

- **Requirement row** [thermal-10](../requirements.md#thermal-10): exposed area and surface/environment exchange reject heat to an explicitly modelled ambient reservoir; no variants.
- **Named entry** [thermal-10](../invest/named-elements.md#thermal-10): owner S572; "an equal-temperature environment gives no net cooling; passive convection cannot cool below ambient".
- **Research** per-element table: "high-conductance link to environment + airflow scaling"; parameters conductance, exposure; "cools faster under a Fan".
- **Processes and scenarios.** [IX-19](../requirements.md#interaction-19), [IX-18](../requirements.md#interaction-18), [IX-08](../requirements.md#interaction-08); [TX-08](../requirements.md#thermal-scenario-08).
- **Legacy.** None found. Searched `parts/`, `engine/physics/`, `engine/*.cs`, `engine/bridge/`, `CuriousContraptions.tests/`, `content/puzzles.json`, `tools/Campaign` and `reference/` at a6c914e for sink, fin, convect and heat terms. Consulted, no element knowledge: `reference/P0-022-before/docs/coverage/engine/task-002.json@a6c914e:L2783-L2787` (historical coverage-scope rows).

## 5. Acceptance outline

Point of truth: [thermal-10](../requirements.md#thermal-10), [TX-08](../requirements.md#thermal-scenario-08) and [ELEMENT acceptance](../requirements.md#accept-element).

- **Chrome recipe.** Through the real palette and sockets: a Thermal storage block (TH-26) authored at 400 K, the heat sink placed on it (ceramic-on-metal pair 12.8 W/K), a Temperature sensor (TH-21) on the block; in a second run, a supplied Fan (CAT-028) aimed at the fins.
- **Positive.** The block (420 J/K, own loss 3.2 W/K) cools faster with the sink than without, and faster again under the Fan (sink path 29 W/K instead of 5 W/K, limited by the 12.8 W/K pair); heat removed equals ambient reservoir gain.
- **Negative or control.** Block at 288 K: no net flow. A Wall between Fan and fins: still-air rate. The block never cools below 288 K by convection alone.
- **Boundaries.** Exposure 0 gives the still-air rate only; every exchange is clamped to the equalising amount.
- **Run/Reset.** Restores sink and block temperatures and the Fan state exactly.
- **Save/Load.** Pose round-trips; Run after reload reproduces the outcome.
- **Integrations.** TX-08; TH-12 hot side; TH-30 cold side; campaign 40, 41, 78, 87, 145.

## 6. Open questions

1. **Batch N game scale.** Adopt or revise the one scale in section 2 (×¼ specific heat, ×1/16 latent heat, gas ×1, reaction energy and O₂ ×1/1024, h 2.5 W/(m²·K), per-pair series contact conductance from per-part face values, 1 intensity unit = 1 W, and which research-table rows the scale multiplies). It carries two consequences: boiling needs a declared saturation curve (S550) because scaled water latent heat is below R·T_sat, and gas-node time constants run about 4× slower than SI (TH-25). Owner decision.
2. **Convection law.** The linear 2.5 + u law is a placeholder for the S545 decision. Owner decision.
3. **Ambient reservoir.** Is the ambient reservoir a single world node (EnvironmentState) or per-region? Owner decision (S635).
4. **TopologyTransaction.** The map row lists it, but nothing in the sink changes topology. Owner decision.
