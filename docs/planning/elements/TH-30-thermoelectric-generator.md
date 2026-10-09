# TH-30 · Thermoelectric generator — named-identity readiness spec

Story 7.0 named-identity spec ([spec](../../../_bmad-output/implementation-artifacts/spec-7-0-element-implementation-readiness.md)). Citations are `path@a6c914e:Lstart-Lend`. No legacy holds thermoelectric values, so every design value without a cited source is marked "(proposed: …)" with a one-line justification; the owner may revise it.

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name | TH-30 · Thermoelectric generator |
| Type | Thermal · potential element (potential/conditional; source adoption retained) |
| Anchors | [requirements.md#thermal-30](../requirements.md#thermal-30) (sequence-task-464); [named-elements.md#thermal-30](../invest/named-elements.md#thermal-30) |
| Proof owner / research | S592 · [TH-S09 thermoelectric conversion](../../thermal-component-research.md#th-s09) |
| Related identities | No CAT spec. **The same converter is reused by [RAD-20 Radioisotope thermoelectric generator](../invest/named-elements.md#radiation-20)** (Batch O) with decay heat on the hot face (row: "RTG assemblies use this same converter"). Hot stores [TH-26](TH-26-thermal-storage-block.md); cold side [TH-10](TH-10-finned-heat-sink.md), [TH-29](TH-29-cold-pack.md); loads such as [CAT-042 Motor](CAT-042-motor.md). |
| Campaign | Intro 87; practice 88; reuse 131, 145 ([thermal allocation](../requirements.md#thermal-campaign-allocation)) |
| Roadmap story | Unscheduled. No epic in `_bmad-output/planning-artifacts/epics.md` schedules a thermal element. |
| Status | Not started. No `WorkshopPartKind` member (`engine/gpu/WorkshopPartKind.cs@a6c914e:L3-L3`); no legacy part. |

## 2. Declaration

- **Batch N game scale** (proposed, identical in all 37 TH specs; chains derived against it are in [TH-17](TH-17-kettle.md#batch-n-chains); owner question in section 6). Ambient reservoir 288 K and 101.3 kPa. [Research table](../../thermal-component-research.md) rows: specific heat, latent heat and source power read as listed range × scale (64–4096 J/(kg·K), 8192–2,097,152 J/kg, 0.5–2048 W); temperature (128–2048 K), conductance (2⁻¹⁰–2⁸ W/K), fuel/supply (finite), ignition threshold (400–1200 K) and sensor thresholds read as listed, without scale; expansion coefficient reads 2⁻²⁰–2⁻¹⁴ 1/K after scale, which TH-22 and TH-23 exceed on purpose as a declared exaggeration. Condensed-phase specific heat SI × ¼ and latent heat SI × 1/16, so catalogue-scale bodies change temperature or phase within one Run. Gas R and Cv SI × 1, so γ and pressure stay physical; each condensed phase's enthalpy is referenced so vapour made at T_sat carries m·Cv·T_sat and the scaled latent heat is exactly the heat consumed; because water's scaled latent heat (141 kJ/kg) is below R·T_sat (172 kJ/kg), boiling follows a declared saturation curve, not Clausius–Clapeyron. Every fuel's reaction energy and O₂ demand per kilogram SI × 1/1024 (wax 41,016 J/kg and 3.4/1024 kg O₂/kg; wood and cellulose 15,625 J/kg and 1.2/1024 kg O₂/kg), so a 0.3 kg candle holds about 12 kJ, heat per kilogram of O₂ keeps Thornton's ≈ 13 MJ/kg, and fuel mass is consumed 1:1 so burn-down stays visible. Still-air surface exchange h = 2.5 W/(m²·K) (SI × ¼, so condensed-body time constants C/(hA) stay SI-like while gas-node ones run about 4× slower than SI); forced air h = 2.5 + u (u in m/s). Contact pairs conduct G = G_a·G_b/(G_a + G_b) from each part's face conductance: metal 64, ceramic, wood, ice or soft 16, probe tip 0.5 W/K. Every exchange is clamped per 480 Hz substep to the equalising amount ΔT·C_a·C_b/(C_a + C_b), so small nodes never overshoot. Reactive surfaces carry a thermal-only patch node outside rigid mass (fibre 10 mg, wood 0.5 g; area 0.0025 m²; patch-to-bulk 0.01 W/K; thermal nodes 10⁻⁶–1024 kg, not bound by the 1/1024 kg rigid minimum) that takes strikes, flames and light first. Radiant optical power: 1 legacy intensity unit = 1 W, so the flashlight emits 24 W. Canonical f32 ([numeric contract](../../gpu-f32-physics.md#game-grade-envelope)).
- **Variants.** None named in the row. RTG use is the RAD-20 assembly reusing this converter, not a TH-30 mode.
- **Bodies and shapes.** One static body: layered box, half-extents 0.20 × 0.15 × 0.20 m (proposed: a 0.4 m block between a hot store and a cold sink). Hot face −Y (underside), cold face +Y with small built-in fins.
- **Mass and material.** Static rigid mass zero (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L70-L76`), so its two thermal nodes are declared separately: hot-face and cold-face plates, 0.2 kg aluminium each, c = 225 J/(kg·K) (aluminium × ¼, batch scale; 45 J/K each). Both faces metal 64 W/K (batch scale): the hot face pairs at 12.8 W/K with a ceramic TH-26 block; the cold face pairs at 32 W/K with a TH-10 foot. Built-in fins give the cold face 2.5 × 0.4 m² = 1 W/K to ambient (batch scale; finned area proposed).
- **Constraints.** None.
- **Typed ports.** One electrical output `WorkshopSocket.Supply`, `Electrical` domain, direction `Output` (`engine/gpu/WorkshopConnections.cs@a6c914e:L8-L11`). Thermal ports: hot and cold faces by contact.
- **Sensors and activation.** None.
- **Work and energy stores.** Internal conductance K = 0.5 W/K between the faces: Q_h = K·(T_h − T_c) (proposed: a module that passes about 50 W at a 100 K difference, so a hot store lasts a whole Run). Electrical power available P_max = 0.3 × (1 − T_c/T_h) × Q_h, capped at 16 W (proposed: 30 % of Carnot is a generous game-grade module; about 3.75 W at T_h 400 K, T_c 300 K, enough for a small load). Delivered P = min(P_max, load demand); Q_c = Q_h − P (IX-34). No load: P = 0 and all heat passes. Equal temperatures: nothing.
- **Parameters.**

  | Parameter | Type | Range | Default | Unit | Source |
  | --- | --- | --- | --- | --- | --- |
  | Internal conductance | f32 | fixed | 0.5 | W/K | (proposed: above) |
  | Fraction of Carnot | f32 | fixed | 0.3 | — | (proposed: above) |
  | Output cap | f32 | fixed | 16 | W | (proposed: above) |
  | Face conductance | f32 | fixed | 64 | W/K | (batch scale: metal face) |

- **Cosmetic curves and UI bindings.** Meter ← committed delivered electrical power ([research per-element table](../../thermal-component-research.md)).
- **Art.** "Layered cream/gold faces, a small finned cold face and shaped electrical terminals" (row). Nearest [DESIGN.md](../../../DESIGN.md) source colours (proposed mapping): cream `#ead39b` (`DESIGN.md@a6c914e:L146-L146`), gold `#f7cb52` (`DESIGN.md@a6c914e:L154-L154`).
- **Catalogue and inventory entry.** New `WorkshopPartKind.ThermoelectricGenerator`; id `thermoelectric_generator`, title "Thermoelectric generator", category Power (proposed: an electrical source beside the Battery).

## 3. Engine capabilities

Families (map row TH-30, [general-engine-element-map](../general-engine-element-map.md); binding [thermal-01.json](../../coverage/engine/thermal-01.json)): ElectricalPower, FiniteLedger, SensibleHeat, ThermoelectricConversion; JSON adds StateTransaction. Composition note: two thermal ports and finite rejected heat constrain ElectricalPower; equal temperature gives none.

- **Exists now.** Static body and box collider (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L120`); typed `Supply` socket (`engine/gpu/WorkshopConnections.cs@a6c914e:L8-L11`); Run/Reset and save (`engine/gpu/ActivationNetwork.cs@a6c914e:L67-L67`, `engine/gpu/WorkshopSaveCodec.cs@a6c914e:L6-L10`).
- **Missing.** ElectricalPower: Story 8.1 ([S257](../invest/decisions.md#s257) → S270). [S543](../invest/decisions.md#s543): thermoelectric → S560, conduction → S544. SensibleHeat (IX-43) has no S543 row. Unscheduled.
- **Dependencies.** A hot store (TH-26) and a cold sink (TH-10, TH-29); an electrical load (CAT-042 Motor, Story 11.1).

## 4. Sources and legacy

- **Requirement row** [thermal-30](../requirements.md#thermal-30): generic hot/cold converter produces bounded supplied electrical output from heat flow and temperature difference; RTG assemblies reuse it; no variants.
- **Named entry** [thermal-30](../invest/named-elements.md#thermal-30): owner S592; "equal temperatures produce no thermoelectric work; load and finite cold-side rejection affect output".
- **Research** [TH-S09](../../thermal-component-research.md#th-s09) and per-element table: "two thermal ports → electrical source"; parameter coefficient; meter ← power.
- **Processes and scenarios.** [IX-34](../requirements.md#interaction-34), [IX-06](../requirements.md#interaction-06), [IX-18](../requirements.md#interaction-18); [TX-11](../requirements.md#thermal-scenario-11).
- **Legacy.** None found. Searched `parts/`, `engine/physics/`, `engine/*.cs`, `engine/bridge/`, `CuriousContraptions.tests/`, `content/puzzles.json`, `tools/Campaign` and `reference/` at a6c914e for thermoelectric, Seebeck, RTG and generator terms. Consulted, no element knowledge: `reference/P0-022-before/docs/coverage/engine/task-002.json@a6c914e:L2783-L2787` (historical coverage-scope rows).

## 5. Acceptance outline

Point of truth: [thermal-30](../requirements.md#thermal-30), [TX-11](../requirements.md#thermal-scenario-11) and [ELEMENT acceptance](../requirements.md#accept-element).

- **Chrome recipe.** Through the real palette and sockets: a Thermal storage block (TH-26) authored at 450 K under the generator, a still-air Finned heat sink (TH-10, 5 W/K) on top, generator `Supply` → Motor (CAT-042) `PowerIn` with the motor authored at 1 N·m and 6 rad/s (6 W maximum power, `parts/MotorPart.cs@a6c914e:L91-L97`).
- **Positive.** Both module faces and the sink start at 288 K. The finite 450 K block first warms the 45 J/K hot face: available output rises from zero to about 5.7 W near 11 s (block about 423 K, hot face 417 K, cold face 291 K), then declines to about 3.8 W at 40 s and 2.8 W at 60 s. These are matched-demand estimates with delivered P = min(P_available, 6 W), not a claim that an unloaded motor continuously draws 6 W. Its actual demand falls after acceleration; use the demand-coupled law for the recipe's measured temperatures and power. The estimates use the coupled 420 J/K block, two 45 J/K faces and 225 J/K sink, including the block's 3.2 W/K ambient loss and cold-side rejection. The unloaded Motor can accelerate and fire turned, but the source cannot sustain its full 6 W rating; verify delivered power never exceeds the available power or demand. At every step Q_h = P + Q_c; store cooling supplies both output and rejected heat.
- **Negative or control.** Block at 288 K: no output and the motor stays still. Motor disconnected: no electrical work, all heat passes. Under the same matched-demand estimate and initially cold faces, with the external heat sink removed, available power peaks near 5.2 W around 9 s and falls to about 2.4 W at 40 s (cold face about 315 K), versus about 3.8 W with the sink: compare at the same physical time and load, not a held-temperature steady state. The Motor may still turn in this reduced-output control; the discriminating observation is the source power and cold-face temperature. A 300 K block gives only a small transient output which also decays.
- **Boundaries.** P never exceeds 16 W or the Carnot fraction; reversing hot and cold produces no output from this one-way module (proposed: see Open question 3).
- **Run/Reset.** Restores face temperatures, the store and the motor exactly.
- **Save/Load.** Pose and connections round-trip; Run after reload reproduces the outcome.
- **Integrations.** TX-11; RAD-20 reuse; campaign 87, 88, 131, 145.

## 6. Open questions

1. **Batch N game scale.** Adopt or revise the one scale in section 2 (×¼ specific heat, ×1/16 latent heat, gas ×1, reaction energy and O₂ ×1/1024, h 2.5 W/(m²·K), per-pair series contact conductance from per-part face values, 1 intensity unit = 1 W, and which research-table rows the scale multiplies). It carries two consequences: boiling needs a declared saturation curve (S550) because scaled water latent heat is below R·T_sat, and gas-node time constants run about 4× slower than SI (TH-25). Owner decision.
2. **Converter law.** Fraction-of-Carnot versus a Seebeck voltage/internal-resistance model. Owner decision (S560).
3. **Polarity.** Does a reversed temperature difference produce reversed output or none? Owner decision.
4. **RTG sharing.** Confirm RAD-20 declares only the decay source and reuses this converter's constitutive law; RAD-20 explicitly declares its compact embedded capacities, conductance, rejection path and output cap. Owner decision (with Batch O).
