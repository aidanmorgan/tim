# TH-29 · Cold pack — named-identity readiness spec

Story 7.0 named-identity spec ([spec](../../../_bmad-output/implementation-artifacts/spec-7-0-element-implementation-readiness.md)). Citations are `path@a6c914e:Lstart-Lend`. No legacy holds cold-pack values, so every design value without a cited source is marked "(proposed: …)" with a one-line justification; the owner may revise it.

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name | TH-29 · Cold pack |
| Type | Thermal · potential element (potential/conditional; source adoption retained) |
| Anchors | [requirements.md#thermal-29](../requirements.md#thermal-29) (sequence-task-463); [named-elements.md#thermal-29](../invest/named-elements.md#thermal-29) |
| Proof owner / research | S591 · [TH-S02 phase change](../../thermal-component-research.md#th-s02) (row), [TH-S05](../../thermal-component-research.md#th-s05) ("a cold pack is a finite store, not a pump") |
| Related identities | No CAT spec. Warm sibling [TH-26 Thermal storage block](TH-26-thermal-storage-block.md); contrast [TH-12 Reversible heat pump](TH-12-reversible-heat-pump.md) (active). Cold sink for [TH-13 Freezing mold](TH-13-freezing-mold.md) (TX-02) and [TH-18 Condenser](TH-18-condenser.md). |
| Campaign | Intro 66; practice 67; reuse 79, 139 ([thermal allocation](../requirements.md#thermal-campaign-allocation)) |
| Roadmap story | Unscheduled. No epic in `_bmad-output/planning-artifacts/epics.md` schedules a thermal element. |
| Status | Not started. No `WorkshopPartKind` member (`engine/gpu/WorkshopPartKind.cs@a6c914e:L3-L3`); no legacy part. |

## 2. Declaration

- **Batch N game scale** (proposed, identical in all 37 TH specs; chains derived against it are in [TH-17](TH-17-kettle.md#batch-n-chains); owner question in section 6). Ambient reservoir 288 K and 101.3 kPa. [Research table](../../thermal-component-research.md) rows: specific heat, latent heat and source power read as listed range × scale (64–4096 J/(kg·K), 8192–2,097,152 J/kg, 0.5–2048 W); temperature (128–2048 K), conductance (2⁻¹⁰–2⁸ W/K), fuel/supply (finite), ignition threshold (400–1200 K) and sensor thresholds read as listed, without scale; expansion coefficient reads 2⁻²⁰–2⁻¹⁴ 1/K after scale, which TH-22 and TH-23 exceed on purpose as a declared exaggeration. Condensed-phase specific heat SI × ¼ and latent heat SI × 1/16, so catalogue-scale bodies change temperature or phase within one Run. Gas R and Cv SI × 1, so γ and pressure stay physical; each condensed phase's enthalpy is referenced so vapour made at T_sat carries m·Cv·T_sat and the scaled latent heat is exactly the heat consumed; because water's scaled latent heat (141 kJ/kg) is below R·T_sat (172 kJ/kg), boiling follows a declared saturation curve, not Clausius–Clapeyron. Every fuel's reaction energy and O₂ demand per kilogram SI × 1/1024 (wax 41,016 J/kg and 3.4/1024 kg O₂/kg; wood and cellulose 15,625 J/kg and 1.2/1024 kg O₂/kg), so a 0.3 kg candle holds about 12 kJ, heat per kilogram of O₂ keeps Thornton's ≈ 13 MJ/kg, and fuel mass is consumed 1:1 so burn-down stays visible. Still-air surface exchange h = 2.5 W/(m²·K) (SI × ¼, so condensed-body time constants C/(hA) stay SI-like while gas-node ones run about 4× slower than SI); forced air h = 2.5 + u (u in m/s). Contact pairs conduct G = G_a·G_b/(G_a + G_b) from each part's face conductance: metal 64, ceramic, wood, ice or soft 16, probe tip 0.5 W/K. Every exchange is clamped per 480 Hz substep to the equalising amount ΔT·C_a·C_b/(C_a + C_b), so small nodes never overshoot. Reactive surfaces carry a thermal-only patch node outside rigid mass (fibre 10 mg, wood 0.5 g; area 0.0025 m²; patch-to-bulk 0.01 W/K; thermal nodes 10⁻⁶–1024 kg, not bound by the 1/1024 kg rigid minimum) that takes strikes, flames and light first. Radiant optical power: 1 legacy intensity unit = 1 W, so the flashlight emits 24 W. Canonical f32 ([numeric contract](../../gpu-f32-physics.md#game-grade-envelope)).
- **Variants.** None named in the row. Initial temperature is authored (proposed: the row's "authored initially cold" mass).
- **Bodies and shapes.** One dynamic body, box collider, half-extents 0.20 × 0.08 × 0.15 m (proposed: a flat 0.4 m tile that rests under a mold or against a channel).
- **Mass and material.** 0.5 kg (proposed: a hand-sized pack, between Domino and Basketball). Water-based gel c = 875 J/(kg·K) (gel 3500 × ¼, batch scale); 437.5 J/K total. Pack face: soft 16 W/K (batch scale), pairing at 12.8 W/K with a metal base such as the TH-13 tray or TH-18 channel, and 8 W/K with a ceramic TH-26 block. Ambient gain over the 0.46 m² surface: 2.5 × 0.46 ≈ 1.2 W/K (batch scale; about 40 W at 255 K, so an idle pack warms at about 0.09 K/s at first). Contact friction 0.5, restitution 0.05 (proposed: soft tile).
- **Constraints.** None.
- **Typed ports.** None as sockets; heat crosses contact faces.
- **Sensors and activation.** None.
- **Work and energy stores.** Sensible store only: cooling capacity H = m·c·(288 K − T). At the default 255 K it can absorb 437.5 × 33 ≈ 14.4 kJ before reaching ambient (derived), but it can freeze water only while it is below 273 K: that share is 437.5 × 18 ≈ 7.9 kJ. It freezes one default TH-13 charge; this capacity alone does not guarantee a second charge because ambient gain and the declining temperature difference limit removal. Immediately replacing the first mold with a fresh tray and water at 288 K produces a maximum solid fraction of about 0.13, followed by melting. No reaction, no subambient boundary, no supply: once warmed it has no capacity left (map composition note).
- **Parameters.**

  | Parameter | Type | Range | Default | Unit | Source |
  | --- | --- | --- | --- | --- | --- |
  | Initial temperature | f32 | 233–288 | 255 | K | (proposed: domestic freezer ≈ 255 K; 233 K floor keeps within the heat-pump range) |
  | Mass | f32 | fixed | 0.5 | kg | (proposed: above) |
  | Specific heat | f32 | fixed | 875 | J/(kg·K) | (batch scale: water-based gel) |
  | Face conductance | f32 | fixed | 16 | W/K | (batch scale: soft face) |

- **Cosmetic curves and UI bindings.** Frost ← remaining capacity ([research per-element table](../../thermal-component-research.md)); the gold capacity gauge shows the same committed value as a shape (row).
- **Art.** "Frosted cyan tiled block with cream grip and a gold capacity gauge" (row). Nearest [DESIGN.md](../../../DESIGN.md) source colours (proposed mapping): cyan `#66b8c9` (`DESIGN.md@a6c914e:L172-L172`), cream `#ead39b` (`DESIGN.md@a6c914e:L146-L146`), gold `#f7cb52` (`DESIGN.md@a6c914e:L154-L154`).
- **Catalogue and inventory entry.** New `WorkshopPartKind.ColdPack`; id `cold_pack`, title "Cold pack", category Heat (proposed: no current category covers thermal parts).

## 3. Engine capabilities

Families (map row TH-29, [general-engine-element-map](../general-engine-element-map.md); binding [thermal-01.json](../../coverage/engine/thermal-01.json)): ChemicalReaction, FiniteLedger, GeometryQuery, SensibleHeat, TopologyTransaction; JSON adds StateTransaction. Composition note: use an initially cold finite SensibleHeat store and shared ThermalConduction; inherited ChemicalReaction is not applicable.

- **Exists now.** Dynamic box body (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L120`); Run/Reset and save (`engine/gpu/ActivationNetwork.cs@a6c914e:L67-L67`, `engine/gpu/WorkshopSaveCodec.cs@a6c914e:L6-L10`).
- **Missing.** [S543](../invest/decisions.md#s543) conduction → S544 (ThermalConduction is required by the composition note but absent from the family list). SensibleHeat (IX-43) has no S543 row. Unscheduled.
- **Dependencies.** A warmer body to cool (TH-13 contents, TH-18 wall, TH-26, TH-37).

## 4. Sources and legacy

- **Requirement row** [thermal-29](../requirements.md#thermal-29): authored initially cold finite thermal mass absorbs heat and warms; a store, not an active refrigerator; no variants.
- **Named entry** [thermal-29](../invest/named-elements.md#thermal-29): owner S591; "a warmed pack has depleted cooling capacity; no endless subambient boundary is attached secretly".
- **Research** [TH-S05](../../thermal-component-research.md#th-s05) and per-element table: "finite cold store"; parameter capacity; "absorbs heat until spent"; frost ← remaining.
- **Processes and scenarios.** [IX-43](../requirements.md#interaction-43), [IX-18](../requirements.md#interaction-18); [TX-02](../requirements.md#thermal-scenario-02) ("a sufficiently cold finite pack uses the same water phase model").
- **Legacy.** None found. Searched `parts/`, `engine/physics/`, `engine/*.cs`, `engine/bridge/`, `CuriousContraptions.tests/`, `content/puzzles.json`, `tools/Campaign` and `reference/` at a6c914e for cold, pack, gel and freezer terms. Consulted, no element knowledge: `reference/P0-022-before/docs/coverage/engine/task-002.json@a6c914e:L2783-L2787` (historical coverage-scope rows).

## 5. Acceptance outline

Point of truth: [thermal-29](../requirements.md#thermal-29) and [ELEMENT acceptance](../requirements.md#accept-element). Numbers follow the [batch chains](TH-17-kettle.md#batch-n-chains).

- **Chrome recipe.** Through the real palette: a Cold pack (255 K) under a filled Freezing mold (TH-13); in a second run, the same pack first pressed against a Thermal storage block authored at 400 K (TH-26) with a Temperature sensor (TH-21) on the pack, then moved under the mold.
- **Positive.** The coupled pack, tray and water nodes use 12.8 W/K pack-to-tray and 16 W/K tray-to-water conductance, with ambient gains 1.2 and 0.75 W/K respectively. The 0.05 kg charge reaches f = 1 after about 26 s; the pack reaches about 264.7 K and the tray about 269.6 K. The pack absorbs about 4.23 kJ: 2.84 kJ to bring tray and water to frozen at 273 K, 0.23 kJ further tray cooling, 0.32 kJ tray ambient gain and 0.83 kJ pack ambient gain. The gauge loses about 29% of its initial 14.4 kJ cooling capacity.
- **Negative or control.** A pack authored at 288 K cools nothing. In the second run the 8 W/K pair brings pack and block toward about 326 K within about 80 s (time constant 27 s); the warmed pack has no capacity left and the mold stays liquid.
- **Boundaries.** The pack never cools anything below its own temperature; it freezes nothing once it is at or above 273 K; capacity reaches zero at 288 K and stays there; heat absorbed = pack enthalpy rise.
- **Run/Reset.** Restores the authored temperature and pose exactly.
- **Save/Load.** Pose and initial temperature round-trip; Run after reload reproduces the outcome.
- **Integrations.** TX-02 passive variant; TH-18 cold wall; lesson 66 before melting and exchange; campaign 66, 67, 79, 139.

## 6. Open questions

1. **Batch N game scale.** Adopt or revise the one scale in section 2 (×¼ specific heat, ×1/16 latent heat, gas ×1, reaction energy and O₂ ×1/1024, h 2.5 W/(m²·K), per-pair series contact conductance from per-part face values, 1 intensity unit = 1 W, and which research-table rows the scale multiplies). It carries two consequences: boiling needs a declared saturation curve (S550) because scaled water latent heat is below R·T_sat, and gas-node time constants run about 4× slower than SI (TH-25). Owner decision.
2. **Latent gel.** Real packs often hold frozen gel (latent store). Keep sensible-only or add a melting fill under S547. Owner decision.
3. **Map row.** Remove ChemicalReaction and add ThermalConduction per the composition note. Owner decision.
4. **Initial temperature authoring.** Level-authored only, or player-selectable? Owner decision.
