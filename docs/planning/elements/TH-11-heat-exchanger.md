# TH-11 · Heat exchanger — named-identity readiness spec

Story 7.0 named-identity spec ([spec](../../../_bmad-output/implementation-artifacts/spec-7-0-element-implementation-readiness.md)). Citations are `path@a6c914e:Lstart-Lend`. No legacy holds heat-exchanger values, so every design value without a cited source is marked "(proposed: …)" with a one-line justification; the owner may revise it.

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name | TH-11 · Heat exchanger |
| Type | Thermal · potential element (potential/conditional; source adoption retained) |
| Anchors | [requirements.md#thermal-11](../requirements.md#thermal-11) (sequence-task-445); [named-elements.md#thermal-11](../invest/named-elements.md#thermal-11) |
| Proof owner / research | S575 · [TH-S01 heat transfer and finite stores](../../thermal-component-research.md#th-s01) |
| Related identities | No CAT spec. Fluid routing comes from the water family (Batches F and G, for example [EL-001 Finite reservoir](../invest/named-elements.md#element-001)) and the gas family (Epic 12 airflow, pneumatic hoses such as [EL-038](../invest/named-elements.md#element-038)). Thermal partners [TH-17](TH-17-kettle.md), [TH-18](TH-18-condenser.md). |
| Campaign | Intro 69; practice 70; reuse 87, 137 ([thermal allocation](../requirements.md#thermal-campaign-allocation)) |
| Roadmap story | Unscheduled. No epic in `_bmad-output/planning-artifacts/epics.md` schedules a thermal element or liquid routing. |
| Status | Not started. No `WorkshopPartKind` member (`engine/gpu/WorkshopPartKind.cs@a6c914e:L3-L3`); no legacy part. |

## 2. Declaration

- **Batch N game scale** (proposed, identical in all 37 TH specs; chains derived against it are in [TH-17](TH-17-kettle.md#batch-n-chains); owner question in section 6). Ambient reservoir 288 K and 101.3 kPa. [Research table](../../thermal-component-research.md) rows: specific heat, latent heat and source power read as listed range × scale (64–4096 J/(kg·K), 8192–2,097,152 J/kg, 0.5–2048 W); temperature (128–2048 K), conductance (2⁻¹⁰–2⁸ W/K), fuel/supply (finite), ignition threshold (400–1200 K) and sensor thresholds read as listed, without scale; expansion coefficient reads 2⁻²⁰–2⁻¹⁴ 1/K after scale, which TH-22 and TH-23 exceed on purpose as a declared exaggeration. Condensed-phase specific heat SI × ¼ and latent heat SI × 1/16, so catalogue-scale bodies change temperature or phase within one Run. Gas R and Cv SI × 1, so γ and pressure stay physical; each condensed phase's enthalpy is referenced so vapour made at T_sat carries m·Cv·T_sat and the scaled latent heat is exactly the heat consumed; because water's scaled latent heat (141 kJ/kg) is below R·T_sat (172 kJ/kg), boiling follows a declared saturation curve, not Clausius–Clapeyron. Every fuel's reaction energy and O₂ demand per kilogram SI × 1/1024 (wax 41,016 J/kg and 3.4/1024 kg O₂/kg; wood and cellulose 15,625 J/kg and 1.2/1024 kg O₂/kg), so a 0.3 kg candle holds about 12 kJ, heat per kilogram of O₂ keeps Thornton's ≈ 13 MJ/kg, and fuel mass is consumed 1:1 so burn-down stays visible. Still-air surface exchange h = 2.5 W/(m²·K) (SI × ¼, so condensed-body time constants C/(hA) stay SI-like while gas-node ones run about 4× slower than SI); forced air h = 2.5 + u (u in m/s). Contact pairs conduct G = G_a·G_b/(G_a + G_b) from each part's face conductance: metal 64, ceramic, wood, ice or soft 16, probe tip 0.5 W/K. Every exchange is clamped per 480 Hz substep to the equalising amount ΔT·C_a·C_b/(C_a + C_b), so small nodes never overshoot. Reactive surfaces carry a thermal-only patch node outside rigid mass (fibre 10 mg, wood 0.5 g; area 0.0025 m²; patch-to-bulk 0.01 W/K; thermal nodes 10⁻⁶–1024 kg, not bound by the 1/1024 kg rigid minimum) that takes strikes, flames and light first. Radiant optical power: 1 legacy intensity unit = 1 W, so the flashlight emits 24 W. Canonical f32 ([numeric contract](../../gpu-f32-physics.md#game-grade-envelope)).
- **Variants.** None named in the row. Each passage accepts any supported fluid (liquid water or gas); fluid choice is not a variant of the part.
- **Bodies and shapes.** One static body: block box, half-extents 0.40 × 0.20 × 0.20 m (proposed: 0.8 m long so two windowed passages read side by side). Two internal passages A and B, each a straight channel along X of 0.10 × 0.10 m cross-section (proposed: matches a pipe bore scale), separated by a partition.
- **Mass and material.** Static rigid mass zero (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L70-L76`). Partition thermal node 0.3 kg copper, c = 96 J/(kg·K) (copper 385 × ¼, batch scale; 28.8 J/K). Passage capacity 0.05 kg liquid water (c = 1046 J/(kg·K), batch scale) or the same volume of gas at the gas family's SI R and Cv (proposed: small inventories exchange within seconds). Outer block exchange 2.5 × 0.96 m² = 2.4 W/K, minus the insulated passages (proposed: the cream block insulates; only the partition couples the streams).
- **Constraints.** None.
- **Typed ports.** Four fluid ports: A-inlet, A-outlet, B-inlet, B-outlet, each an enum-typed fluid port (proposed: the water and gas families' port types; no fluid socket exists in `engine/gpu/WorkshopConnections.cs@a6c914e:L8-L11`). Material never crosses from A to B.
- **Sensors and activation.** None.
- **Work and energy stores.** Partition conductance UA = 8 W/K between each passage's fluid and the partition (proposed: thin metal wall with flowing liquid). Exchange q = UA·(T_A − T_B) through the partition node, clamped per substep to the equalising amount; zero at equal temperatures. Each passage's inventory, enthalpy and species stay its own (IX-08 advection carries them).
- **Parameters.**

  | Parameter | Type | Range | Default | Unit | Source |
  | --- | --- | --- | --- | --- | --- |
  | Partition UA | f32 | fixed | 8 | W/K | (proposed: above) |
  | Passage capacity | f32 | fixed | 0.05 | kg (liquid) | (proposed: above) |
  | Partition thermal mass | f32 | fixed | 0.3 | kg | (proposed: above) |

- **Cosmetic curves and UI bindings.** Flow arrows ← committed exchange q ([research per-element table](../../thermal-component-research.md)); embossed route symbols distinguish A and B.
- **Art.** "Twin cyan windowed passages in a cream block with distinct embossed route symbols" (row). Nearest [DESIGN.md](../../../DESIGN.md) source colours (proposed mapping): cyan `#66b8c9` (`DESIGN.md@a6c914e:L172-L172`), cream `#ead39b` (`DESIGN.md@a6c914e:L146-L146`).
- **Catalogue and inventory entry.** New `WorkshopPartKind.HeatExchanger`; id `heat_exchanger`, title "Heat exchanger", category Heat (proposed: no current category covers thermal parts).

## 3. Engine capabilities

Families (map row TH-11, [general-engine-element-map](../general-engine-element-map.md); binding [thermal-01.json](../../coverage/engine/thermal-01.json)): FiniteLedger, FluidAdvection, GeometryQuery, SensibleHeat, ThermalConduction, ThermalConvection, TopologyTransaction; JSON adds StateTransaction.

- **Exists now.** Static body and box collider (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L120`); Run/Reset and save (`engine/gpu/ActivationNetwork.cs@a6c914e:L67-L67`, `engine/gpu/WorkshopSaveCodec.cs@a6c914e:L6-L10`).
- **Missing.** FluidAdvection: [S416](../invest/decisions.md#s416) advection → S418 (liquids) and [S470](../invest/decisions.md#s470) gas-state → S471; typed fluid ports. [S543](../invest/decisions.md#s543): convection → S545, conduction → S544. SensibleHeat (IX-43) has no S543 row. All unscheduled.
- **Dependencies.** Two fluid sources and sinks (water family reservoirs or gas reservoirs); a temperature difference between them.

## 4. Sources and legacy

- **Requirement row** [thermal-11](../requirements.md#thermal-11): two separated passages exchange through a conductive partition; material stays in its own passage; no variants.
- **Named entry** [thermal-11](../invest/named-elements.md#thermal-11): owner S575; "equal-temperature streams give no net exchange; blocked flow limits delivery and cannot cross-contaminate channels".
- **Research** per-element table: "two-stream link"; parameter conductance; flow arrows ← q.
- **Processes.** [IX-08](../requirements.md#interaction-08), [IX-18](../requirements.md#interaction-18), [IX-19](../requirements.md#interaction-19).
- **Legacy.** None found. Searched `parts/`, `engine/physics/`, `engine/*.cs`, `engine/bridge/`, `CuriousContraptions.tests/`, `content/puzzles.json`, `tools/Campaign` and `reference/` at a6c914e for exchanger, passage and heat terms. The legacy gas files hold single sealed inventories with no heat exchange (`engine/physics/IdealGasMaterial.cs@a6c914e:L5-L6`: "No ambient source, phase transition or variable heat capacity is implied"); no exchanger knowledge.

## 5. Acceptance outline

Point of truth: [thermal-11](../requirements.md#thermal-11) and [ELEMENT acceptance](../requirements.md#accept-element).

- **Chrome recipe.** Through the real palette and fluid connections: a hot water reservoir (heated by TH-04) routed through passage A to a catch basin; a cold reservoir routed through passage B to a second basin.
- **Positive.** Outlet A leaves cooler and outlet B warmer; heat lost by A equals heat gained by B plus partition storage; each basin collects only its own water.
- **Negative or control.** Equal inlet temperatures: no net exchange. Passage B blocked: B's resident inventory saturates at A's temperature and delivery stops; nothing crosses into A.
- **Boundaries.** Exchange never exceeds the equalising amount; mass in each loop is conserved exactly.
- **Run/Reset.** Restores both passage inventories, temperatures and the partition node exactly.
- **Save/Load.** Pose and fluid connections round-trip; Run after reload reproduces the outcome.
- **Integrations.** TX-05 condenser loops; campaign 69, 70, 87, 137.

## 6. Open questions

1. **Batch N game scale.** Adopt or revise the one scale in section 2 (×¼ specific heat, ×1/16 latent heat, gas ×1, reaction energy and O₂ ×1/1024, h 2.5 W/(m²·K), per-pair series contact conductance from per-part face values, 1 intensity unit = 1 W, and which research-table rows the scale multiplies). It carries two consequences: boiling needs a declared saturation curve (S550) because scaled water latent heat is below R·T_sat, and gas-node time constants run about 4× slower than SI (TH-25). Owner decision.
2. **Fluid ports.** Which typed port enum (shared water/gas, or one per domain) the exchanger uses. Owner decision (S416/S470).
3. **Liquid density.** Game-scale liquid volume per kilogram comes from the water family; reconcile passage capacity with Batches F and G. Owner decision.
4. **Mixed fluids.** May passage A carry gas while B carries liquid? Owner decision.
