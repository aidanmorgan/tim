# TH-08 · Heat-conducting bar — named-identity readiness spec

Story 7.0 named-identity spec ([spec](../../../_bmad-output/implementation-artifacts/spec-7-0-element-implementation-readiness.md)). Citations are `path@a6c914e:Lstart-Lend`. No legacy holds conductor values, so every design value without a cited source is marked "(proposed: …)" with a one-line justification; the owner may revise it.

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name | TH-08 · Heat-conducting bar |
| Type | Thermal · potential element (potential/conditional; source adoption retained) |
| Anchors | [requirements.md#thermal-08](../requirements.md#thermal-08) (sequence-task-442); [named-elements.md#thermal-08](../invest/named-elements.md#thermal-08) |
| Proof owner / research | S570 · [TH-S01 heat transfer and finite stores](../../thermal-component-research.md#th-s01) |
| Related identities | No CAT spec. Low-conductivity comparison: [TH-09 Insulating panel](TH-09-insulating-panel.md). Typical endpoints: [TH-04](TH-04-electrical-heating-plate.md), [TH-07](TH-07-solar-absorber-plate.md), [TH-26](TH-26-thermal-storage-block.md), [TH-37](TH-37-heat-sensitive-target.md). |
| Campaign | Intro 16; practice 17; reuse 55, 82, 145 ([thermal allocation](../requirements.md#thermal-campaign-allocation)) |
| Roadmap story | Unscheduled. No epic in `_bmad-output/planning-artifacts/epics.md` schedules a thermal element. |
| Status | Not started. No `WorkshopPartKind` member (`engine/gpu/WorkshopPartKind.cs@a6c914e:L3-L3`); no legacy part. |

## 2. Declaration

- **Batch N game scale** (proposed, identical in all 37 TH specs; chains derived against it are in [TH-17](TH-17-kettle.md#batch-n-chains); owner question in section 6). Ambient reservoir 288 K and 101.3 kPa. [Research table](../../thermal-component-research.md) rows: specific heat, latent heat and source power read as listed range × scale (64–4096 J/(kg·K), 8192–2,097,152 J/kg, 0.5–2048 W); temperature (128–2048 K), conductance (2⁻¹⁰–2⁸ W/K), fuel/supply (finite), ignition threshold (400–1200 K) and sensor thresholds read as listed, without scale; expansion coefficient reads 2⁻²⁰–2⁻¹⁴ 1/K after scale, which TH-22 and TH-23 exceed on purpose as a declared exaggeration. Condensed-phase specific heat SI × ¼ and latent heat SI × 1/16, so catalogue-scale bodies change temperature or phase within one Run. Gas R and Cv SI × 1, so γ and pressure stay physical; each condensed phase's enthalpy is referenced so vapour made at T_sat carries m·Cv·T_sat and the scaled latent heat is exactly the heat consumed; because water's scaled latent heat (141 kJ/kg) is below R·T_sat (172 kJ/kg), boiling follows a declared saturation curve, not Clausius–Clapeyron. Every fuel's reaction energy and O₂ demand per kilogram SI × 1/1024 (wax 41,016 J/kg and 3.4/1024 kg O₂/kg; wood and cellulose 15,625 J/kg and 1.2/1024 kg O₂/kg), so a 0.3 kg candle holds about 12 kJ, heat per kilogram of O₂ keeps Thornton's ≈ 13 MJ/kg, and fuel mass is consumed 1:1 so burn-down stays visible. Still-air surface exchange h = 2.5 W/(m²·K) (SI × ¼, so condensed-body time constants C/(hA) stay SI-like while gas-node ones run about 4× slower than SI); forced air h = 2.5 + u (u in m/s). Contact pairs conduct G = G_a·G_b/(G_a + G_b) from each part's face conductance: metal 64, ceramic, wood, ice or soft 16, probe tip 0.5 W/K. Every exchange is clamped per 480 Hz substep to the equalising amount ΔT·C_a·C_b/(C_a + C_b), so small nodes never overshoot. Reactive surfaces carry a thermal-only patch node outside rigid mass (fibre 10 mg, wood 0.5 g; area 0.0025 m²; patch-to-bulk 0.01 W/K; thermal nodes 10⁻⁶–1024 kg, not bound by the 1/1024 kg rigid minimum) that takes strikes, flames and light first. Radiant optical power: 1 legacy intensity unit = 1 W, so the flashlight emits 24 W. Canonical f32 ([numeric contract](../../gpu-f32-physics.md#game-grade-envelope)).
- **Variants.** None named in the row.
- **Bodies and shapes.** One static body placed like a Ramp: box, half-extents 0.60 × 0.04 × 0.10 m (proposed: a 1.2 m bridge spans two parts sitting a Basketball-width apart; "broad" reads as 0.2 m wide). Two contact ends, the ±X end faces and the last 0.1 m of the underside at each end.
- **Mass and material.** Static rigid mass zero (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L70-L76`). Copper: thermal mass 1.0 kg in two end nodes of 0.5 kg each, c = 96 J/(kg·K) (copper 385 × ¼, batch scale), k = 400 W/(m·K) (SI). End faces: metal, 64 W/K (batch scale), so an end pairs at 32 W/K with a metal plate and 12.8 W/K with a ceramic target. Contact friction 0.3, restitution 0.1 (proposed: smooth metal).
- **Constraints.** None.
- **Typed ports.** None as sockets: "neither endpoint names a permitted counterpart" (row). Heat crosses only real contacts.
- **Sensors and activation.** None.
- **Work and energy stores.** Bar conductance G = k·A/L = 400 × (0.2 × 0.08) / 1.2 ≈ 5.3 W/K (derived from the proposed geometry; inside the 2⁻¹⁰–2⁸ range). Insulated grips cover the middle; the exposed 0.1 m² of metal ends exchange 2.5 × 0.1 = 0.25 W/K with ambient (batch scale). Heat flow q = G·(T₁ − T₂), equal and opposite (IX-18).
- **Parameters.**

  | Parameter | Type | Range | Default | Unit | Source |
  | --- | --- | --- | --- | --- | --- |
  | Length | f32 | fixed | 1.2 | m | (proposed: above) |
  | Cross-section | f32 | fixed | 0.2 × 0.08 | m | (proposed: above) |
  | Conductivity | f32 | fixed | 400 | W/(m·K) | (proposed: copper SI) |
  | End face conductance | f32 | fixed | 64 | W/K | (batch scale: metal face) |

- **Cosmetic curves and UI bindings.** None ([research per-element table](../../thermal-component-research.md)). Engraved contact ends mark where heat crosses.
- **Art.** "Broad gold bridge with cream insulated grips and engraved contact ends" (row). Nearest [DESIGN.md](../../../DESIGN.md) source colours (proposed mapping): gold `#f7cb52` (`DESIGN.md@a6c914e:L154-L154`), cream `#ead39b` (`DESIGN.md@a6c914e:L146-L146`).
- **Catalogue and inventory entry.** New `WorkshopPartKind.HeatConductingBar`; id `heat_bar`, title "Heat-conducting bar", category Heat (proposed: no current category covers thermal parts).

## 3. Engine capabilities

Families (map row TH-08, [general-engine-element-map](../general-engine-element-map.md); binding [thermal-01.json](../../coverage/engine/thermal-01.json)): FiniteLedger, GeometryQuery, SensibleHeat, ThermalConduction; JSON adds StateTransaction.

- **Exists now.** Static body, box collider and contact queries (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L120`); Run/Reset and save (`engine/gpu/ActivationNetwork.cs@a6c914e:L67-L67`, `engine/gpu/WorkshopSaveCodec.cs@a6c914e:L6-L10`).
- **Missing.** Conduction across real contacts → [S543](../invest/decisions.md#s543) conduction → S544. SensibleHeat (IX-43) has no S543 row. Unscheduled.
- **Dependencies.** A hot and a cold body to bridge (TH-04, TH-07, TH-26, TH-37).

## 4. Sources and legacy

- **Requirement row** [thermal-08](../requirements.md#thermal-08): conductivity, length, cross-section and actual contacts; no endpoint names a counterpart; no variants.
- **Named entry** [thermal-08](../invest/named-elements.md#thermal-08): owner S570; "an air gap removes solid contact conduction; a low-conductivity comparison transfers less under the same conditions".
- **Research** per-element table: "conduction link"; parameter conductance; "carries heat between touching nodes".
- **Processes.** [IX-18](../requirements.md#interaction-18), [IX-43](../requirements.md#interaction-43); conductor routing is IX-18's second objective (lesson 16).
- **Legacy.** None found. Searched `parts/`, `engine/physics/`, `engine/*.cs`, `engine/bridge/`, `CuriousContraptions.tests/`, `content/puzzles.json`, `tools/Campaign` and `reference/` at a6c914e for conduct, conductance and heat terms. Consulted, no element knowledge: `reference/P0-022-before/docs/coverage/engine/task-002.json@a6c914e:L2783-L2787` (historical coverage-scope rows).

## 5. Acceptance outline

Point of truth: [thermal-08](../requirements.md#thermal-08) and [ELEMENT acceptance](../requirements.md#accept-element).

- **Chrome recipe.** Through the real palette and gizmo: a supplied Heating plate (TH-04), a Heat-sensitive target (TH-37, threshold 330 K) 1 m away, and the bar placed with one end on each.
- **Positive.** Plate → bar → target conduct in series at 1/(1/32 + 1/5.3 + 1/12.8) ≈ 3.35 W/K; the target heads for about 484 K (plate about 563 K) and passes 330 K well inside the Run. The plate's loss equals the bar's and target's gain plus declared ambient loss.
- **Negative or control.** Bar lifted 0.05 m off the target (air gap): no conduction at that end; the target stays near ambient. Insulating panel (TH-09, 0.4 W/K through) in the bar's place: the target warms far more slowly under the same supply.
- **Boundaries.** Equal end temperatures give zero flow; reversing hot and cold reverses the flow; every exchange is clamped to the equalising amount.
- **Run/Reset.** Restores both end nodes and all connected bodies to 288 K exactly.
- **Save/Load.** Pose round-trips; Run after reload reproduces the outcome.
- **Integrations.** TH-07 → bar routing; TH-05 brake heat onward; campaign 16, 17, 55, 82, 145.

## 6. Open questions

1. **Batch N game scale.** Adopt or revise the one scale in section 2 (×¼ specific heat, ×1/16 latent heat, gas ×1, reaction energy and O₂ ×1/1024, h 2.5 W/(m²·K), per-pair series contact conductance from per-part face values, 1 intensity unit = 1 W, and which research-table rows the scale multiplies). It carries two consequences: boiling needs a declared saturation curve (S550) because scaled water latent heat is below R·T_sat, and gas-node time constants run about 4× slower than SI (TH-25). Owner decision.
2. **Comparison material.** Is a same-shape low-conductivity bar preset (for example steel, k = 50 W/(m·K)) required, or does TH-09 satisfy the comparison? Owner decision.
3. **Dynamic option.** Should the bar be a dynamic carried body instead of a static bridge? Owner decision.
