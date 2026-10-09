# TH-18 · Condenser — named-identity readiness spec

Story 7.0 named-identity spec ([spec](../../../_bmad-output/implementation-artifacts/spec-7-0-element-implementation-readiness.md)). Citations are `path@a6c914e:Lstart-Lend`. No legacy holds condenser values, so every design value without a cited source is marked "(proposed: …)" with a one-line justification; the owner may revise it.

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name | TH-18 · Condenser |
| Type | Thermal · potential element (potential/conditional; source adoption retained) |
| Anchors | [requirements.md#thermal-18](../requirements.md#thermal-18) (sequence-task-452); [named-elements.md#thermal-18](../invest/named-elements.md#thermal-18) |
| Proof owner / research | S582 · [TH-S02 phase change](../../thermal-component-research.md#th-s02) |
| Related identities | No CAT spec. Vapour from [TH-17 Kettle](TH-17-kettle.md), [TH-35](TH-35-coffee-pot-steam-vessel.md), or the exhaust of [TH-19](TH-19-steam-piston.md)/[TH-20](TH-20-steam-turbine.md); sinks [TH-29 Cold pack](TH-29-cold-pack.md), [TH-10](TH-10-finned-heat-sink.md), [TH-12](TH-12-reversible-heat-pump.md); liquid to water-family catch basins (Batches F and G). |
| Campaign | Intro 74; practice 75; reuse 87, 137 ([thermal allocation](../requirements.md#thermal-campaign-allocation)) |
| Roadmap story | Unscheduled. No epic in `_bmad-output/planning-artifacts/epics.md` schedules a thermal element. |
| Status | Not started. No `WorkshopPartKind` member (`engine/gpu/WorkshopPartKind.cs@a6c914e:L3-L3`); no legacy part. |

## 2. Declaration

- **Batch N game scale** (proposed, identical in all 37 TH specs; chains derived against it are in [TH-17](TH-17-kettle.md#batch-n-chains); owner question in section 6). Ambient reservoir 288 K and 101.3 kPa. [Research table](../../thermal-component-research.md) rows: specific heat, latent heat and source power read as listed range × scale (64–4096 J/(kg·K), 8192–2,097,152 J/kg, 0.5–2048 W); temperature (128–2048 K), conductance (2⁻¹⁰–2⁸ W/K), fuel/supply (finite), ignition threshold (400–1200 K) and sensor thresholds read as listed, without scale; expansion coefficient reads 2⁻²⁰–2⁻¹⁴ 1/K after scale, which TH-22 and TH-23 exceed on purpose as a declared exaggeration. Condensed-phase specific heat SI × ¼ and latent heat SI × 1/16, so catalogue-scale bodies change temperature or phase within one Run. Gas R and Cv SI × 1, so γ and pressure stay physical; each condensed phase's enthalpy is referenced so vapour made at T_sat carries m·Cv·T_sat and the scaled latent heat is exactly the heat consumed; because water's scaled latent heat (141 kJ/kg) is below R·T_sat (172 kJ/kg), boiling follows a declared saturation curve, not Clausius–Clapeyron. Every fuel's reaction energy and O₂ demand per kilogram SI × 1/1024 (wax 41,016 J/kg and 3.4/1024 kg O₂/kg; wood and cellulose 15,625 J/kg and 1.2/1024 kg O₂/kg), so a 0.3 kg candle holds about 12 kJ, heat per kilogram of O₂ keeps Thornton's ≈ 13 MJ/kg, and fuel mass is consumed 1:1 so burn-down stays visible. Still-air surface exchange h = 2.5 W/(m²·K) (SI × ¼, so condensed-body time constants C/(hA) stay SI-like while gas-node ones run about 4× slower than SI); forced air h = 2.5 + u (u in m/s). Contact pairs conduct G = G_a·G_b/(G_a + G_b) from each part's face conductance: metal 64, ceramic, wood, ice or soft 16, probe tip 0.5 W/K. Every exchange is clamped per 480 Hz substep to the equalising amount ΔT·C_a·C_b/(C_a + C_b), so small nodes never overshoot. Reactive surfaces carry a thermal-only patch node outside rigid mass (fibre 10 mg, wood 0.5 g; area 0.0025 m²; patch-to-bulk 0.01 W/K; thermal nodes 10⁻⁶–1024 kg, not bound by the 1/1024 kg rigid minimum) that takes strikes, flames and light first. Radiant optical power: 1 legacy intensity unit = 1 W, so the flashlight emits 24 W. Canonical f32 ([numeric contract](../../gpu-f32-physics.md#game-grade-envelope)).
- **Variants.** None named in the row.
- **Bodies and shapes.** One static body: descending channel box, half-extents 0.60 × 0.10 × 0.10 m, tilted 15° down toward the drip outlet (proposed: a 1.2 m architectural channel that visibly drains by gravity). Inlet at the high end, drip outlet underneath the low end, vapour exit at the low end top.
- **Mass and material.** Static rigid mass zero (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L70-L76`). Channel wall node 1.0 kg aluminium, c = 225 J/(kg·K) (aluminium × ¼, batch scale; a finite internal sink that warms as it condenses). Underside: metal face 64 W/K (batch scale), pairing at 12.8 W/K with a cold pack and 32 W/K with a heat pump face. Ambient exchange 2.5 × 1.04 m² = 2.6 W/K. Water vaporisation 141,250 J/kg at 373 K; steam R = 461.5, Cv = 1410 J/(kg·K) SI (batch scale).
- **Constraints.** None.
- **Typed ports.** Gas inlet, gas exit (uncondensed remainder) and liquid drip outlet, each an enum-typed fluid port (proposed: the gas and water families' port types; none exists in `engine/gpu/WorkshopConnections.cs@a6c914e:L8-L11`).
- **Sensors and activation.** None.
- **Work and energy stores.** Vapour-to-wall conductance UA = 6 W/K (proposed: condensing film on a metal channel). Condensation rate ṁ = UA·(T_sat − T_wall)/L when T_wall < T_sat, else zero (IX-25); the latent heat enters the wall node. At a 288 K wall that is about 3.6 g/s, matching the plate-driven kettle's 2.7 g/s boil rate; the 225 J/K wall then warms at about 1.7 K/s and condensation stops near 373 K unless an external sink removes heat. Liquid leaves through the drip outlet with its enthalpy; vapour mass in = liquid out + vapour exit (conserved).
- **Parameters.**

  | Parameter | Type | Range | Default | Unit | Source |
  | --- | --- | --- | --- | --- | --- |
  | Vapour-to-wall UA | f32 | fixed | 6 | W/K | (proposed: above) |
  | Wall thermal mass | f32 | fixed | 1.0 | kg | (proposed: above) |
  | Underside face conductance | f32 | fixed | 64 | W/K | (batch scale: metal face) |
  | Channel tilt | f32 | fixed | 15 | degrees | (proposed: above) |

- **Cosmetic curves and UI bindings.** Drip rate ← committed condensation rate ([research per-element table](../../thermal-component-research.md)); the cyan state window shows liquid film, distinct from vapour.
- **Art.** "Descending cream architectural channel with cyan state window and separate drip outlet" (row). Nearest [DESIGN.md](../../../DESIGN.md) source colours (proposed mapping): cream `#ead39b` (`DESIGN.md@a6c914e:L146-L146`), cyan `#66b8c9` (`DESIGN.md@a6c914e:L172-L172`).
- **Catalogue and inventory entry.** New `WorkshopPartKind.Condenser`; id `condenser`, title "Condenser", category Heat (proposed: no current category covers thermal parts).

## 3. Engine capabilities

Families (map row TH-18, [general-engine-element-map](../general-engine-element-map.md); binding [thermal-01.json](../../coverage/engine/thermal-01.json)): EnvironmentState, FiniteLedger, FluidAdvection, GasState, GeometryQuery, PhaseTopology, SensibleHeat, ThermalConduction, TopologyTransaction, VaporCondensation; JSON adds StateTransaction.

- **Exists now.** Static body and box collider (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L120`); Run/Reset and save (`engine/gpu/ActivationNetwork.cs@a6c914e:L67-L67`, `engine/gpu/WorkshopSaveCodec.cs@a6c914e:L6-L10`).
- **Missing.** GasState and gas ports: [S470](../invest/decisions.md#s470) → S471. Liquid outflow: [S416](../invest/decisions.md#s416) → S418. [S543](../invest/decisions.md#s543): condensation → S551, conduction → S544, phase-topology → S565. All unscheduled.
- **Dependencies.** A vapour source (TH-17); a sink (TH-29, TH-10 or TH-12); a catch basin.

## 4. Sources and legacy

- **Requirement row** [thermal-18](../requirements.md#thermal-18): a cooled passage removes vapour enthalpy and collects conserved liquid; no variants.
- **Named entry** [thermal-18](../invest/named-elements.md#thermal-18): owner S582; "a warm or exhausted sink cannot condense an unlimited vapor stream".
- **Research** per-element table: "cold link + phase transition"; parameter conductance; "turns steam back into accounted water"; drip rate ← q.
- **Processes and scenarios.** [IX-25](../requirements.md#interaction-25), [IX-08](../requirements.md#interaction-08), [IX-18](../requirements.md#interaction-18); [TX-05](../requirements.md#thermal-scenario-05).
- **Legacy.** None found. Searched `parts/`, `engine/physics/`, `engine/*.cs`, `engine/bridge/`, `CuriousContraptions.tests/`, `content/puzzles.json`, `tools/Campaign` and `reference/` at a6c914e for condens, vapour and steam terms. The legacy gas material excluded phase transitions (`engine/physics/IdealGasMaterial.cs@a6c914e:L5-L6`); recorded only.

## 5. Acceptance outline

Point of truth: [thermal-18](../requirements.md#thermal-18), [TX-05](../requirements.md#thermal-scenario-05) and [ELEMENT acceptance](../requirements.md#accept-element).

- **Chrome recipe.** Through the real palette and fluid connections: a Kettle (TH-17) boiling on a supplied Heating plate, hosed to the condenser inlet, a Cold pack (TH-29) under the channel, the drip outlet over a catch basin.
- **Positive.** Liquid drips into the basin; basin mass + vented vapour = kettle liquid lost; the wall and pack warm by the released latent heat.
- **Negative or control.** No cold pack and a channel already at 373 K: nothing condenses; vapour passes through the exit. Without the pack, the wall alone warms to saturation within about a minute and condensation stops (finite sink).
- **Boundaries.** Wall at or above T_sat gives zero condensation; condensate never exceeds vapour supplied.
- **Run/Reset.** Restores 288 K wall, empty channel and zero condensate exactly.
- **Save/Load.** Pose and fluid connections round-trip; Run after reload reproduces the outcome.
- **Integrations.** TX-05 working-fluid recovery; TH-19/TH-20 exhaust return; campaign 74, 75, 87, 137.

## 6. Open questions

1. **Batch N game scale.** Adopt or revise the one scale in section 2 (×¼ specific heat, ×1/16 latent heat, gas ×1, reaction energy and O₂ ×1/1024, h 2.5 W/(m²·K), per-pair series contact conductance from per-part face values, 1 intensity unit = 1 W, and which research-table rows the scale multiplies). It carries two consequences: boiling needs a declared saturation curve (S550) because scaled water latent heat is below R·T_sat, and gas-node time constants run about 4× slower than SI (TH-25). Owner decision.
2. **Internal sink.** Is the wall's own 1.0 kg mass an acceptable finite sink, or must every condenser need an attached external sink? Owner decision.
3. **Saturation curve.** T_sat versus pressure for non-ambient inlets, using the same declared curve as TH-17. Owner decision (S551).
4. **Fluid ports.** Shared typed port enum with the water and gas families. Owner decision.
