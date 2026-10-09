# TH-21 · Temperature sensor — named-identity readiness spec

Story 7.0 named-identity spec ([spec](../../../_bmad-output/implementation-artifacts/spec-7-0-element-implementation-readiness.md)). Citations are `path@a6c914e:Lstart-Lend`. No legacy holds temperature-sensor values, so every design value without a cited source is marked "(proposed: …)" with a one-line justification; the owner may revise it.

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name | TH-21 · Temperature sensor |
| Type | Thermal · potential element (potential/conditional; source adoption retained) |
| Anchors | [requirements.md#thermal-21](../requirements.md#thermal-21) (sequence-task-455); [named-elements.md#thermal-21](../invest/named-elements.md#thermal-21) |
| Proof owner / research | S568 · [TH-S01 heat transfer and finite stores](../../thermal-component-research.md#th-s01) |
| Related identities | No CAT spec. First heat slice with [TH-04](TH-04-electrical-heating-plate.md) and [CAT-035 Signal lamp](CAT-035-lamp.md). Sibling sensors: [TH-22 Bimetal thermostat](TH-22-bimetal-thermostat.md) (mechanical contact), [TH-37 Heat-sensitive target](TH-37-heat-sensitive-target.md) (goal). Threshold-with-hysteresis pattern shared with the gas family's pressure gauge [EL-043](../invest/named-elements.md#element-043). |
| Campaign | Intro 15; practice 16; reuse 70, 83, 145 ([thermal allocation](../requirements.md#thermal-campaign-allocation)) |
| Roadmap story | Unscheduled. The research doc names it in the first heat slice ([first heat slice](../../thermal-component-research.md)); no epic in `_bmad-output/planning-artifacts/epics.md` schedules it. |
| Status | Not started. No `WorkshopPartKind` member (`engine/gpu/WorkshopPartKind.cs@a6c914e:L3-L3`); no legacy part. |

## 2. Declaration

- **Batch N game scale** (proposed, identical in all 37 TH specs; chains derived against it are in [TH-17](TH-17-kettle.md#batch-n-chains); owner question in section 6). Ambient reservoir 288 K and 101.3 kPa. [Research table](../../thermal-component-research.md) rows: specific heat, latent heat and source power read as listed range × scale (64–4096 J/(kg·K), 8192–2,097,152 J/kg, 0.5–2048 W); temperature (128–2048 K), conductance (2⁻¹⁰–2⁸ W/K), fuel/supply (finite), ignition threshold (400–1200 K) and sensor thresholds read as listed, without scale; expansion coefficient reads 2⁻²⁰–2⁻¹⁴ 1/K after scale, which TH-22 and TH-23 exceed on purpose as a declared exaggeration. Condensed-phase specific heat SI × ¼ and latent heat SI × 1/16, so catalogue-scale bodies change temperature or phase within one Run. Gas R and Cv SI × 1, so γ and pressure stay physical; each condensed phase's enthalpy is referenced so vapour made at T_sat carries m·Cv·T_sat and the scaled latent heat is exactly the heat consumed; because water's scaled latent heat (141 kJ/kg) is below R·T_sat (172 kJ/kg), boiling follows a declared saturation curve, not Clausius–Clapeyron. Every fuel's reaction energy and O₂ demand per kilogram SI × 1/1024 (wax 41,016 J/kg and 3.4/1024 kg O₂/kg; wood and cellulose 15,625 J/kg and 1.2/1024 kg O₂/kg), so a 0.3 kg candle holds about 12 kJ, heat per kilogram of O₂ keeps Thornton's ≈ 13 MJ/kg, and fuel mass is consumed 1:1 so burn-down stays visible. Still-air surface exchange h = 2.5 W/(m²·K) (SI × ¼, so condensed-body time constants C/(hA) stay SI-like while gas-node ones run about 4× slower than SI); forced air h = 2.5 + u (u in m/s). Contact pairs conduct G = G_a·G_b/(G_a + G_b) from each part's face conductance: metal 64, ceramic, wood, ice or soft 16, probe tip 0.5 W/K. Every exchange is clamped per 480 Hz substep to the equalising amount ΔT·C_a·C_b/(C_a + C_b), so small nodes never overshoot. Reactive surfaces carry a thermal-only patch node outside rigid mass (fibre 10 mg, wood 0.5 g; area 0.0025 m²; patch-to-bulk 0.01 W/K; thermal nodes 10⁻⁶–1024 kg, not bound by the 1/1024 kg rigid minimum) that takes strikes, flames and light first. Radiant optical power: 1 legacy intensity unit = 1 W, so the flashlight emits 24 W. Canonical f32 ([numeric contract](../../gpu-f32-physics.md#game-grade-envelope)).
- **Variants.** None named in the row.
- **Bodies and shapes.** One static body: thermometer column box, half-extents 0.08 × 0.40 × 0.08 m (proposed: a slim 0.8 m column reads beside a 0.6 m plate). Probe tip: box with half-extents 0.04 × 0.04 × 0.04 m at the column foot, the only thermal contact (proposed: the probe touches the body it measures). Hysteresis rule "On > Off by ≥ 2 K, dwell in ticks" from the research table; physics ticks at 120 Hz (AGENTS.md rates).
- **Mass and material.** Static rigid mass zero (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L70-L76`). Probe node 0.02 kg brass, c = 95 J/(kg·K) (brass 380 × ¼, batch scale; 1.9 J/K). Probe tip face 0.5 W/K (batch scale), so it pairs at 0.496 W/K with a metal face and 0.485 W/K with a ceramic one: response time constant about 3.8 s. Unattached, the probe exchanges only with ambient air: 2.5 × 0.02 m² = 0.05 W/K.
- **Constraints.** None.
- **Typed ports.** One output `WorkshopSocket.ActivationOut`, `Activation` domain, direction `Output` (types exist: `engine/gpu/WorkshopConnections.cs@a6c914e:L8-L11`). The output is a signal, never power: a downstream load needs its own supply (S257).
- **Sensors and activation.** Reads the probe node temperature T_p at substep endpoints. Asserts when T_p ≥ On for the dwell; clears when T_p ≤ Off for the dwell. Defaults On 330 K, Off 325 K, dwell 30 ticks = 0.25 s (proposed: 5 K hysteresis exceeds the ≥ 2 K rule and stops chatter; 0.25 s filters single-tick spikes). New activation source kind (proposed: `ActivationNodeKind` has ContactSource, Latch, Timer, OrientationSource only, `engine/gpu/ActivationNetwork.cs@a6c914e:L8-L8`).
- **Work and energy stores.** The probe node only; sensing creates no heat or electrical work (IX-17).
- **Parameters.**

  | Parameter | Type | Range | Default | Unit | Source |
  | --- | --- | --- | --- | --- | --- |
  | On threshold | f32 | 300–600, step 5 | 330 | K | (proposed: player dial covers warm-to-ignition range) |
  | Off threshold | f32 | On − 5 | 325 | K | (proposed: fixed 5 K hysteresis) |
  | Dwell | u32 | fixed | 30 | ticks | (proposed: above) |
  | Probe tip face conductance | f32 | fixed | 0.5 | W/K | (batch scale: probe tip) |

- **Cosmetic curves and UI bindings.** Needle ← committed probe temperature ([research per-element table](../../thermal-component-research.md)); gold indicator ← committed output state. A small dial sets On (proposed: matches the "small dial" control rule for discrete modules).
- **Art.** "Cream engraved thermometer column with navy ticks and gold indicator" (row). Nearest [DESIGN.md](../../../DESIGN.md) source colours (proposed mapping): cream `#ead39b` (`DESIGN.md@a6c914e:L146-L146`), navy `#293954` (`DESIGN.md@a6c914e:L147-L147`), gold `#f7cb52` (`DESIGN.md@a6c914e:L154-L154`).
- **Catalogue and inventory entry.** New `WorkshopPartKind.TemperatureSensor`; id `temperature_sensor`, title "Temperature sensor", category Control (proposed: the existing category for sensors and logic).

## 3. Engine capabilities

Families (map row TH-21, [general-engine-element-map](../general-engine-element-map.md); binding [thermal-01.json](../../coverage/engine/thermal-01.json)): FiniteLedger, SensibleHeat, SignalPropagation, TemperatureSensing; JSON adds StateTransaction. Composition note: probe binding, response, hysteresis and supplied output are separate from thermal state observation.

- **Exists now.** Activation-domain signal network and typed ports (`engine/gpu/ActivationNetwork.cs@a6c914e:L7-L14`, `engine/gpu/WorkshopConnections.cs@a6c914e:L8-L11`); Signal lamp consumer (`engine/gpu/WorkshopPartKind.cs@a6c914e:L3-L3`); Run/Reset and save (`engine/gpu/ActivationNetwork.cs@a6c914e:L67-L67`, `engine/gpu/WorkshopSaveCodec.cs@a6c914e:L6-L10`).
- **Missing.** TemperatureSensing (IX-17) has no S543 row; this element is its first consumer. Level (assert/clear) activation sources: the current network latches occurrences. [S543](../invest/decisions.md#s543) conduction → S544 for probe contact. SensibleHeat (IX-43) has no S543 row. Unscheduled.
- **Dependencies.** A heat source (TH-04); a consumer (CAT-035 Signal lamp, logic gates of Epic 9).

## 4. Sources and legacy

- **Requirement row** [thermal-21](../requirements.md#thermal-21): read local state through a probe/contact; emit a supplied typed threshold signal with defined hysteresis and response; no variants.
- **Named entry** [thermal-21](../invest/named-elements.md#thermal-21): owner S568; "an unattached probe or unmet threshold does not assert the goal; sensing supplies no heat or electrical work".
- **Research** parameter table (sensor thresholds, dwell), per-element table ("thermal sensor + electrical output"; On/Off T; needle ← T) and the [first heat slice](../../thermal-component-research.md).
- **Processes.** [IX-17](../requirements.md#interaction-17), [IX-07](../requirements.md#interaction-07), [IX-18](../requirements.md#interaction-18).
- **Legacy.** None found. Searched `parts/`, `engine/physics/`, `engine/*.cs`, `engine/bridge/`, `CuriousContraptions.tests/`, `content/puzzles.json`, `tools/Campaign` and `reference/` at a6c914e for thermometer, temperature sensor and threshold terms. Consulted, no element knowledge: `reference/P0-022-before/docs/coverage/engine/task-002.json@a6c914e:L2783-L2787` (historical coverage-scope rows).

## 5. Acceptance outline

Point of truth: [thermal-21](../requirements.md#thermal-21) and [ELEMENT acceptance](../requirements.md#accept-element).

- **Chrome recipe.** The research first-slice recipe through actual palette, gizmo and socket controls: Battery → Switch → Heating plate; Temperature sensor probe on the plate; sensor `ActivationOut` → Signal lamp `ActivationIn`.
- **Positive.** The plate passes 330 K after about 5 s; the probe follows with its 3.8 s time constant; once T_p ≥ 330 K for 30 ticks (about 10 s after Run) the output asserts and the lamp lights.
- **Negative or control.** Probe not touching the plate: needle stays near 288 K, output stays clear. Unsupplied plate: no rise. Sensor output wired to an electrical `PowerIn`: refused at connection (domain mismatch).
- **Boundaries.** T_p of 329.9 K does not assert; a 29-tick excursion does not assert; after asserting, it clears only at ≤ 325 K for 30 ticks.
- **Run/Reset.** Restores probe at 288 K, output clear and connections exactly.
- **Save/Load.** Pose, On threshold and connections round-trip; Run after reload reproduces the outcome.
- **Integrations.** TH-09 control; TH-07 absorber readout (On 300 K); Epic 9 logic; campaign 15, 16, 70, 83, 145.

## 6. Open questions

1. **Batch N game scale.** Adopt or revise the one scale in section 2 (×¼ specific heat, ×1/16 latent heat, gas ×1, reaction energy and O₂ ×1/1024, h 2.5 W/(m²·K), per-pair series contact conductance from per-part face values, 1 intensity unit = 1 W, and which research-table rows the scale multiplies). It carries two consequences: boiling needs a declared saturation curve (S550) because scaled water latent heat is below R·T_sat, and gas-node time constants run about 4× slower than SI (TH-25). Owner decision.
2. **Latching lamp.** The research slice expects the lamp to go dark below Off, but CAT-035 latches until Reset ([CAT-035](../requirements.md#current-cat-035)). Decide whether the slice needs a non-latching consumer. Owner decision.
3. **"Supplied" output.** Does the sensor need its own electrical supply to emit, or is the activation signal unsupplied? Owner decision (S257).
4. **Level signals.** Add a level (assert/clear) activation source to the network, or emit edge occurrences only. Owner decision.
