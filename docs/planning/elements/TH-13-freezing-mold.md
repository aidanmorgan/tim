# TH-13 · Freezing mold — named-identity readiness spec

Story 7.0 named-identity spec ([spec](../../../_bmad-output/implementation-artifacts/spec-7-0-element-implementation-readiness.md)). Citations are `path@a6c914e:Lstart-Lend`. No legacy holds freezing-mold values, so every design value without a cited source is marked "(proposed: …)" with a one-line justification; the owner may revise it.

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name | TH-13 · Freezing mold |
| Type | Thermal · potential element (potential/conditional; source adoption retained) |
| Anchors | [requirements.md#thermal-13](../requirements.md#thermal-13) (sequence-task-447); [named-elements.md#thermal-13](../invest/named-elements.md#thermal-13) |
| Proof owner / research | S577 · [TH-S02 phase change](../../thermal-component-research.md#th-s02) |
| Related identities | No CAT spec. Cold sources [TH-12 Reversible heat pump](TH-12-reversible-heat-pump.md) and [TH-29 Cold pack](TH-29-cold-pack.md) (TX-02); the solid it forms is the same water material as [TH-14 Ice block](TH-14-ice-block.md). Water supply from the water family (Batches F and G). |
| Campaign | Intro 79; practice 80; reuse 89, 149 ([thermal allocation](../requirements.md#thermal-campaign-allocation)) |
| Roadmap story | Unscheduled. No epic in `_bmad-output/planning-artifacts/epics.md` schedules a thermal element. |
| Status | Not started. No `WorkshopPartKind` member (`engine/gpu/WorkshopPartKind.cs@a6c914e:L3-L3`); no legacy part. |

## 2. Declaration

- **Batch N game scale** (proposed, identical in all 37 TH specs; chains derived against it are in [TH-17](TH-17-kettle.md#batch-n-chains); owner question in section 6). Ambient reservoir 288 K and 101.3 kPa. [Research table](../../thermal-component-research.md) rows: specific heat, latent heat and source power read as listed range × scale (64–4096 J/(kg·K), 8192–2,097,152 J/kg, 0.5–2048 W); temperature (128–2048 K), conductance (2⁻¹⁰–2⁸ W/K), fuel/supply (finite), ignition threshold (400–1200 K) and sensor thresholds read as listed, without scale; expansion coefficient reads 2⁻²⁰–2⁻¹⁴ 1/K after scale, which TH-22 and TH-23 exceed on purpose as a declared exaggeration. Condensed-phase specific heat SI × ¼ and latent heat SI × 1/16, so catalogue-scale bodies change temperature or phase within one Run. Gas R and Cv SI × 1, so γ and pressure stay physical; each condensed phase's enthalpy is referenced so vapour made at T_sat carries m·Cv·T_sat and the scaled latent heat is exactly the heat consumed; because water's scaled latent heat (141 kJ/kg) is below R·T_sat (172 kJ/kg), boiling follows a declared saturation curve, not Clausius–Clapeyron. Every fuel's reaction energy and O₂ demand per kilogram SI × 1/1024 (wax 41,016 J/kg and 3.4/1024 kg O₂/kg; wood and cellulose 15,625 J/kg and 1.2/1024 kg O₂/kg), so a 0.3 kg candle holds about 12 kJ, heat per kilogram of O₂ keeps Thornton's ≈ 13 MJ/kg, and fuel mass is consumed 1:1 so burn-down stays visible. Still-air surface exchange h = 2.5 W/(m²·K) (SI × ¼, so condensed-body time constants C/(hA) stay SI-like while gas-node ones run about 4× slower than SI); forced air h = 2.5 + u (u in m/s). Contact pairs conduct G = G_a·G_b/(G_a + G_b) from each part's face conductance: metal 64, ceramic, wood, ice or soft 16, probe tip 0.5 W/K. Every exchange is clamped per 480 Hz substep to the equalising amount ΔT·C_a·C_b/(C_a + C_b), so small nodes never overshoot. Reactive surfaces carry a thermal-only patch node outside rigid mass (fibre 10 mg, wood 0.5 g; area 0.0025 m²; patch-to-bulk 0.01 W/K; thermal nodes 10⁻⁶–1024 kg, not bound by the 1/1024 kg rigid minimum) that takes strikes, flames and light first. Radiant optical power: 1 legacy intensity unit = 1 W, so the flashlight emits 24 W. Canonical f32 ([numeric contract](../../gpu-f32-physics.md#game-grade-envelope)).
- **Variants.** None named in the row.
- **Bodies and shapes.** One static body: open tray made of a base box, half-extents 0.22 × 0.02 × 0.22 m, and four wall boxes, each half-extents 0.22 × 0.08 × 0.02 m (proposed: a 0.44 m tray with a 0.40 × 0.14 × 0.40 m cavity; colliders are boxes only, `engine/gpu/PhysicsDeclarations.cs@a6c914e:L89-L120`). The cavity is the declared geometry of the solid that forms.
- **Mass and material.** Static rigid mass zero (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L70-L76`). Tray thermal node 0.3 kg aluminium, c = 225 J/(kg·K) (aluminium × ¼, batch scale; 67.5 J/K). Water under the batch scale: liquid c = 1046, ice c = 525 J/(kg·K), fusion 20,875 J/kg at 273 K. Water charge: authored fill 0–0.05 kg, default 0.05 kg (proposed). Base: metal face 64 W/K (batch scale), pairing at 32 W/K with a heat pump face and 12.8 W/K with a cold pack. Tray-to-water internal conductance 16 W/K (proposed: the charge wets the whole flat tray). Ambient exchange 2.5 × 0.30 m² = 0.75 W/K.
- **Constraints.** None.
- **Typed ports.** None as sockets. Thermal port: the tray base underside (contact) and walls; liquid enters through the open top from any water source.
- **Sensors and activation.** None. No hidden cold source: freezing happens only when an external colder body removes heat.
- **Work and energy stores.** Contents node holds mass, enthalpy and solid fraction f ∈ [0, 1]. Freezing the default charge from 288 K needs 0.05 × 1046 × 15 = 784 J sensible + 0.05 × 20,875 = 1,044 J latent, plus 67.5 × 15 ≈ 1,013 J to cool the tray: about 2.8 kJ. At 273 K extra heat removal increases f; the solid occupies the cavity bottom-up in proportion to f.
- **Parameters.**

  | Parameter | Type | Range | Default | Unit | Source |
  | --- | --- | --- | --- | --- | --- |
  | Water fill | f32 | 0–0.05 | 0.05 | kg | (proposed: freezes in under a minute on the 24 W heat pump) |
  | Base face conductance | f32 | fixed | 64 | W/K | (batch scale: metal face) |
  | Tray-to-water conductance | f32 | fixed | 16 | W/K | (proposed: above) |

- **Cosmetic curves and UI bindings.** Ice fraction ← committed solid fraction f ([research per-element table](../../thermal-component-research.md)); the cyan phase window shows a readable solidification front (row), not colour alone.
- **Art.** "Open cream geometric tray with a cyan phase window and readable solidification front" (row). Nearest [DESIGN.md](../../../DESIGN.md) source colours (proposed mapping): cream `#ead39b` (`DESIGN.md@a6c914e:L146-L146`), cyan `#66b8c9` (`DESIGN.md@a6c914e:L172-L172`).
- **Catalogue and inventory entry.** New `WorkshopPartKind.FreezingMold`; id `freezing_mold`, title "Freezing mold", category Heat (proposed: no current category covers thermal parts).

## 3. Engine capabilities

Families (map row TH-13, [general-engine-element-map](../general-engine-element-map.md); binding [thermal-01.json](../../coverage/engine/thermal-01.json)): EnvironmentState, FiniteLedger, GasState, GeometryQuery, LiquidFreezing, PhaseTopology, SensibleHeat, ThermalConduction, TopologyTransaction; JSON adds StateTransaction.

- **Exists now.** Static bodies and box colliders (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L120`); Run/Reset and save (`engine/gpu/ActivationNetwork.cs@a6c914e:L67-L67`, `engine/gpu/WorkshopSaveCodec.cs@a6c914e:L6-L10`).
- **Missing.** [S543](../invest/decisions.md#s543): freezing → S548, phase-topology → S565, conduction → S544. Liquid water inventory: [S416](../invest/decisions.md#s416) advection → S418. GasState/EnvironmentState: [S470](../invest/decisions.md#s470) → S471, [S635](../invest/decisions.md#s635). All unscheduled.
- **Dependencies.** A colder sink (TH-12, TH-29); water (authored fill or the water family).

## 4. Sources and legacy

- **Requirement row** [thermal-13](../requirements.md#thermal-13): container defines the solid's geometry and provides heat-transfer surfaces; no hidden cold source; no variants.
- **Named entry** [thermal-13](../invest/named-elements.md#thermal-13): owner S577; "insufficient energy removal leaves a measured liquid/solid mixture; equal-temperature surroundings cannot freeze it".
- **Research** per-element table: "node + phase transition"; parameter volume; "water freezes into a solid cargo when cooled"; ice fraction ← f.
- **Processes and scenarios.** [IX-22](../requirements.md#interaction-22), [IX-41](../requirements.md#interaction-41), [IX-18](../requirements.md#interaction-18); [TX-02](../requirements.md#thermal-scenario-02).
- **Legacy.** None found. Searched `parts/`, `engine/physics/`, `engine/*.cs`, `engine/bridge/`, `CuriousContraptions.tests/`, `content/puzzles.json`, `tools/Campaign` and `reference/` at a6c914e for freez, mold, ice and phase terms. The legacy gas material explicitly excluded phase transitions (`engine/physics/IdealGasMaterial.cs@a6c914e:L5-L6`); recorded only.

## 5. Acceptance outline

Point of truth: [thermal-13](../requirements.md#thermal-13), [TX-02](../requirements.md#thermal-scenario-02) and [ELEMENT acceptance](../requirements.md#accept-element). Numbers are derived in the [batch chains](TH-17-kettle.md#batch-n-chains).

- **Chrome recipe.** Through the real palette and sockets: a filled Freezing mold on face A of a Forward Heat pump (TH-12, 24 W), Battery → Switch → pump, a still-air heat sink (TH-10) on face B.
- **Positive.** The pump removes about 73 W at first, less as its cold face drops below the freezing water, while the mold gains about 10 W from the room; the water cools to 273 K, then the front rises until f = 1 after about 50 s; removed heat equals sensible + latent change plus room gain; mass is conserved. With a Cold pack (TH-29, 255 K) instead of the pump the same charge freezes after about 26 s, leaving the pack near 264.7 K. Including the tray's further cooling and both ambient gains, the pack absorbs about 4.23 kJ; 2.84 kJ is the tray/water energy needed to reach fully frozen at 273 K.
- **Negative or control.** Pump unsupplied: no freezing. Pump switched off at f ≈ 0.5: a measured liquid/solid mixture remains. Mold in 288 K surroundings with no sink: stays liquid.
- **Boundaries.** f stays in [0, 1]; no freezing above 273 K; an empty mold forms nothing.
- **Run/Reset.** Restores liquid fill, 288 K and f = 0 exactly.
- **Save/Load.** Pose and fill round-trip; Run after reload reproduces the outcome.
- **Integrations.** TX-02 with TH-12 and with a TH-29 cold pack; campaign 79, 80, 89, 149.

## 6. Open questions

1. **Batch N game scale.** Adopt or revise the one scale in section 2 (×¼ specific heat, ×1/16 latent heat, gas ×1, reaction energy and O₂ ×1/1024, h 2.5 W/(m²·K), per-pair series contact conductance from per-part face values, 1 intensity unit = 1 W, and which research-table rows the scale multiplies). It carries two consequences: boiling needs a declared saturation curve (S550) because scaled water latent heat is below R·T_sat, and gas-node time constants run about 4× slower than SI (TH-25). Owner decision.
2. **Release.** Does a fully frozen charge become a separate dynamic ice body (TH-14 material) that can leave the mold? Owner decision (S565).
3. **Fill source.** Authored fill, poured water from the water family, or both. Owner decision.
4. **GasState in the row.** The map row lists GasState; freezing needs no gas. Owner decision.
