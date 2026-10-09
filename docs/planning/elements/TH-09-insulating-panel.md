# TH-09 · Insulating panel — named-identity readiness spec

Story 7.0 named-identity spec ([spec](../../../_bmad-output/implementation-artifacts/spec-7-0-element-implementation-readiness.md)). Citations are `path@a6c914e:Lstart-Lend`. No legacy holds insulation values, so every design value without a cited source is marked "(proposed: …)" with a one-line justification; the owner may revise it.

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name | TH-09 · Insulating panel |
| Type | Thermal · potential element (potential/conditional; source adoption retained) |
| Anchors | [requirements.md#thermal-09](../requirements.md#thermal-09) (sequence-task-443); [named-elements.md#thermal-09](../invest/named-elements.md#thermal-09) |
| Proof owner / research | S571 · [TH-S01 heat transfer and finite stores](../../thermal-component-research.md#th-s01) |
| Related identities | Configurable-dimension precedent: [CAT-066 Wall](CAT-066-wall.md). Used as the control in the first heat slice with [TH-04](TH-04-electrical-heating-plate.md) and [TH-21](TH-21-temperature-sensor.md); the low-conductivity comparison for [TH-08](TH-08-heat-conducting-bar.md). |
| Campaign | Intro 19; practice 20; reuse 67, 84, 139 ([thermal allocation](../requirements.md#thermal-campaign-allocation)) |
| Roadmap story | Unscheduled. No epic in `_bmad-output/planning-artifacts/epics.md` schedules a thermal element. |
| Status | Not started. No `WorkshopPartKind` member (`engine/gpu/WorkshopPartKind.cs@a6c914e:L3-L3`); no legacy part. |

## 2. Declaration

- **Batch N game scale** (proposed, identical in all 37 TH specs; chains derived against it are in [TH-17](TH-17-kettle.md#batch-n-chains); owner question in section 6). Ambient reservoir 288 K and 101.3 kPa. [Research table](../../thermal-component-research.md) rows: specific heat, latent heat and source power read as listed range × scale (64–4096 J/(kg·K), 8192–2,097,152 J/kg, 0.5–2048 W); temperature (128–2048 K), conductance (2⁻¹⁰–2⁸ W/K), fuel/supply (finite), ignition threshold (400–1200 K) and sensor thresholds read as listed, without scale; expansion coefficient reads 2⁻²⁰–2⁻¹⁴ 1/K after scale, which TH-22 and TH-23 exceed on purpose as a declared exaggeration. Condensed-phase specific heat SI × ¼ and latent heat SI × 1/16, so catalogue-scale bodies change temperature or phase within one Run. Gas R and Cv SI × 1, so γ and pressure stay physical; each condensed phase's enthalpy is referenced so vapour made at T_sat carries m·Cv·T_sat and the scaled latent heat is exactly the heat consumed; because water's scaled latent heat (141 kJ/kg) is below R·T_sat (172 kJ/kg), boiling follows a declared saturation curve, not Clausius–Clapeyron. Every fuel's reaction energy and O₂ demand per kilogram SI × 1/1024 (wax 41,016 J/kg and 3.4/1024 kg O₂/kg; wood and cellulose 15,625 J/kg and 1.2/1024 kg O₂/kg), so a 0.3 kg candle holds about 12 kJ, heat per kilogram of O₂ keeps Thornton's ≈ 13 MJ/kg, and fuel mass is consumed 1:1 so burn-down stays visible. Still-air surface exchange h = 2.5 W/(m²·K) (SI × ¼, so condensed-body time constants C/(hA) stay SI-like while gas-node ones run about 4× slower than SI); forced air h = 2.5 + u (u in m/s). Contact pairs conduct G = G_a·G_b/(G_a + G_b) from each part's face conductance: metal 64, ceramic, wood, ice or soft 16, probe tip 0.5 W/K. Every exchange is clamped per 480 Hz substep to the equalising amount ΔT·C_a·C_b/(C_a + C_b), so small nodes never overshoot. Reactive surfaces carry a thermal-only patch node outside rigid mass (fibre 10 mg, wood 0.5 g; area 0.0025 m²; patch-to-bulk 0.01 W/K; thermal nodes 10⁻⁶–1024 kg, not bound by the 1/1024 kg rigid minimum) that takes strikes, flames and light first. Radiant optical power: 1 legacy intensity unit = 1 W, so the flashlight emits 24 W. Canonical f32 ([numeric contract](../../gpu-f32-physics.md#game-grade-envelope)).
- **Variants.** None named in the row. Thickness is a configuration parameter, so the "thinner comparison" is the same part at a smaller thickness.
- **Bodies and shapes.** One static body: upright slab box, half-extents (t/2) × 0.40 × 0.50 m for thickness t (proposed: a 1.0 × 0.8 m face shields a Basketball-sized target). Opaque to light and gas as an ordinary solid.
- **Mass and material.** Static rigid mass zero (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L70-L76`). Two face nodes of 0.25 kg each, c = 210 J/(kg·K) (ceramic fibre 840 × ¼, batch scale). Conductivity k = 0.05 W/(m·K) (proposed: ceramic-fibre board 0.04–0.08). Faces: ceramic, 16 W/K (batch scale), so a probe tip pairs at 0.485 W/K and the plate at 12.8 W/K. Contact friction 0.6, restitution 0.05 (proposed: matte ceramic like the Domino, `engine/gpu/WorkshopDomino.cs@a6c914e:L9-L9`).
- **Constraints.** None.
- **Typed ports.** None.
- **Sensors and activation.** None.
- **Work and energy stores.** Through-conductance G = k·A/t = 0.05 × 0.8 / t; at the default 0.10 m G = 0.4 W/K, at 0.05 m G = 0.8 W/K (derived). Each 0.8 m² face keeps its explicit still-air exchange, 2.5 × 0.8 = 2 W/K (batch scale, radiation included); insulation never deletes heat (map composition note).
- **Parameters.**

  | Parameter | Type | Range | Default | Unit | Source |
  | --- | --- | --- | --- | --- | --- |
  | Thickness t | f32 | 0.05–0.20 | 0.10 | m | (proposed: a 4:1 range shows the conductance trade; set through the gizmo like Wall dimensions, `engine/gpu/WorkshopWall.cs@a6c914e:L8-L10`) |
  | Conductivity | f32 | fixed | 0.05 | W/(m·K) | (proposed: above) |
  | Face conductance | f32 | fixed | 16 | W/K | (batch scale: ceramic face) |

- **Cosmetic curves and UI bindings.** None ([research per-element table](../../thermal-component-research.md)). The navy section pattern shows thickness.
- **Art.** "Layered cream ceramic slab with a visible navy section pattern" (row). Nearest [DESIGN.md](../../../DESIGN.md) source colours (proposed mapping): cream `#ead39b` (`DESIGN.md@a6c914e:L146-L146`), navy `#293954` (`DESIGN.md@a6c914e:L147-L147`).
- **Catalogue and inventory entry.** New `WorkshopPartKind.InsulatingPanel`; id `insulating_panel`, title "Insulating panel", category Heat (proposed: no current category covers thermal parts).

## 3. Engine capabilities

Families (map row TH-09, [general-engine-element-map](../general-engine-element-map.md); binding [thermal-01.json](../../coverage/engine/thermal-01.json)): FiniteLedger, GeometryQuery, SensibleHeat, ThermalConduction, ThermalRadiation; JSON adds StateTransaction. Composition note: keep explicitly supported convection and radiation paths; insulation never deletes heat.

- **Exists now.** Static body and box collider (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L120`); configurable box dimensions precedent (`engine/gpu/WorkshopWall.cs@a6c914e:L8-L10`); Run/Reset and save (`engine/gpu/ActivationNetwork.cs@a6c914e:L67-L67`, `engine/gpu/WorkshopSaveCodec.cs@a6c914e:L6-L10`).
- **Missing.** [S543](../invest/decisions.md#s543): conduction → S544, thermal-radiation → S546; face convection → S545 (not in the map row). SensibleHeat (IX-43) has no S543 row. Unscheduled.
- **Dependencies.** A heat source and a receiver on opposite sides (TH-04, TH-21, TH-37).

## 4. Sources and legacy

- **Requirement row** [thermal-09](../requirements.md#thermal-09): thickness and material resist transfer while retaining supported radiative/convective paths; no variants.
- **Named entry** [thermal-09](../invest/named-elements.md#thermal-09): owner S571; "a thinner/lower-resistance comparison leaks heat faster; insulation is not a universal heat deletion field".
- **Research** per-element table: "low-conductance link"; parameter conductance; "slows heat loss; protects cargo". The [first heat slice](../../thermal-component-research.md) uses this panel as a control.
- **Processes.** [IX-18](../requirements.md#interaction-18), [IX-19](../requirements.md#interaction-19), [IX-20](../requirements.md#interaction-20).
- **Legacy.** None found. Searched `parts/`, `engine/physics/`, `engine/*.cs`, `engine/bridge/`, `CuriousContraptions.tests/`, `content/puzzles.json`, `tools/Campaign` and `reference/` at a6c914e for insulat, conductance and heat terms. Consulted, no element knowledge: `reference/P0-022-before/docs/coverage/engine/task-002.json@a6c914e:L2783-L2787` (historical coverage-scope rows).

## 5. Acceptance outline

Point of truth: [thermal-09](../requirements.md#thermal-09) and [ELEMENT acceptance](../requirements.md#accept-element).

- **Chrome recipe.** The first-slice recipe (Battery → Switch → Heating plate → Temperature sensor → Signal lamp) with an Insulating panel placed through the palette between plate and probe; thickness set with the gizmo.
- **Positive.** With the 0.10 m panel the probe side face rises slowly (0.4 W/K through, 2 W/K lost from that face), so it stays far below the plate and the lamp lights much later than without the panel, or not at all within the Run; heat crossing the panel equals G·ΔT.
- **Negative or control.** The same construction at 0.05 m leaks twice as fast (the needle rises sooner). A hot body wrapped by panels still cools through the declared face paths; total heat is conserved.
- **Boundaries.** Thickness outside 0.05–0.20 m is clamped at the gizmo; equal face temperatures give zero flow.
- **Run/Reset.** Restores face temperatures and thickness exactly.
- **Save/Load.** Pose and thickness round-trip; Run after reload reproduces the outcome.
- **Integrations.** TH-14 Ice block protection (lesson 67); campaign 19, 20, 67, 84, 139.

## 6. Open questions

1. **Batch N game scale.** Adopt or revise the one scale in section 2 (×¼ specific heat, ×1/16 latent heat, gas ×1, reaction energy and O₂ ×1/1024, h 2.5 W/(m²·K), per-pair series contact conductance from per-part face values, 1 intensity unit = 1 W, and which research-table rows the scale multiplies). It carries two consequences: boiling needs a declared saturation curve (S550) because scaled water latent heat is below R·T_sat, and gas-node time constants run about 4× slower than SI (TH-25). Owner decision.
2. **Thickness control.** Player-configurable thickness or fixed thickness presets. Owner decision.
3. **Face convection.** The map row omits ThermalConvection although the row keeps convective paths. Owner decision.
