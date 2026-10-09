# TH-22 · Bimetal thermostat — named-identity readiness spec

Story 7.0 named-identity spec ([spec](../../../_bmad-output/implementation-artifacts/spec-7-0-element-implementation-readiness.md)). Citations are `path@a6c914e:Lstart-Lend`. No legacy holds bimetal values, so every design value without a cited source is marked "(proposed: …)" with a one-line justification; the owner may revise it.

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name | TH-22 · Bimetal thermostat |
| Type | Thermal · potential element (potential/conditional; source adoption retained) |
| Anchors | [requirements.md#thermal-22](../requirements.md#thermal-22) (sequence-task-456); [named-elements.md#thermal-22](../invest/named-elements.md#thermal-22) |
| Proof owner / research | S585 · [TH-S06 thermal expansion](../../thermal-component-research.md#th-s06) |
| Related identities | No CAT spec. Shares the expansion law with [TH-23 Expansion rod](TH-23-expansion-rod.md); electrical sibling of [TH-21 Temperature sensor](TH-21-temperature-sensor.md), but switches by physical contact, not by a threshold signal. Circuit parts: [CAT-005 Battery](CAT-005-battery.md), a supplied load such as [CAT-042 Motor](CAT-042-motor.md). |
| Campaign | Intro 83; practice 84; reuse 90, 145 ([thermal allocation](../requirements.md#thermal-campaign-allocation)) |
| Roadmap story | Unscheduled. No epic in `_bmad-output/planning-artifacts/epics.md` schedules a thermal element. |
| Status | Not started. No `WorkshopPartKind` member (`engine/gpu/WorkshopPartKind.cs@a6c914e:L3-L3`); no legacy part. |

## 2. Declaration

- **Batch N game scale** (proposed, identical in all 37 TH specs; chains derived against it are in [TH-17](TH-17-kettle.md#batch-n-chains); owner question in section 6). Ambient reservoir 288 K and 101.3 kPa. [Research table](../../thermal-component-research.md) rows: specific heat, latent heat and source power read as listed range × scale (64–4096 J/(kg·K), 8192–2,097,152 J/kg, 0.5–2048 W); temperature (128–2048 K), conductance (2⁻¹⁰–2⁸ W/K), fuel/supply (finite), ignition threshold (400–1200 K) and sensor thresholds read as listed, without scale; expansion coefficient reads 2⁻²⁰–2⁻¹⁴ 1/K after scale, which TH-22 and TH-23 exceed on purpose as a declared exaggeration. Condensed-phase specific heat SI × ¼ and latent heat SI × 1/16, so catalogue-scale bodies change temperature or phase within one Run. Gas R and Cv SI × 1, so γ and pressure stay physical; each condensed phase's enthalpy is referenced so vapour made at T_sat carries m·Cv·T_sat and the scaled latent heat is exactly the heat consumed; because water's scaled latent heat (141 kJ/kg) is below R·T_sat (172 kJ/kg), boiling follows a declared saturation curve, not Clausius–Clapeyron. Every fuel's reaction energy and O₂ demand per kilogram SI × 1/1024 (wax 41,016 J/kg and 3.4/1024 kg O₂/kg; wood and cellulose 15,625 J/kg and 1.2/1024 kg O₂/kg), so a 0.3 kg candle holds about 12 kJ, heat per kilogram of O₂ keeps Thornton's ≈ 13 MJ/kg, and fuel mass is consumed 1:1 so burn-down stays visible. Still-air surface exchange h = 2.5 W/(m²·K) (SI × ¼, so condensed-body time constants C/(hA) stay SI-like while gas-node ones run about 4× slower than SI); forced air h = 2.5 + u (u in m/s). Contact pairs conduct G = G_a·G_b/(G_a + G_b) from each part's face conductance: metal 64, ceramic, wood, ice or soft 16, probe tip 0.5 W/K. Every exchange is clamped per 480 Hz substep to the equalising amount ΔT·C_a·C_b/(C_a + C_b), so small nodes never overshoot. Reactive surfaces carry a thermal-only patch node outside rigid mass (fibre 10 mg, wood 0.5 g; area 0.0025 m²; patch-to-bulk 0.01 W/K; thermal nodes 10⁻⁶–1024 kg, not bound by the 1/1024 kg rigid minimum) that takes strikes, flames and light first. Radiant optical power: 1 legacy intensity unit = 1 W, so the flashlight emits 24 W. Canonical f32 ([numeric contract](../../gpu-f32-physics.md#game-grade-envelope)).
- **Variants.** None named in the row. The "uniform-material control" needs a strip whose layers match; proposed as an authored strip material enum `Bimetal` (default) or `Uniform` (control), each with its own proof (proposed: the control must keep identical geometry).
- **Bodies and shapes.** Static cream base box, half-extents 0.15 × 0.05 × 0.10 m (proposed: a small plinth). Strip cantilevered from the base: length 0.40 m, two bonded layers of 0.01 m each, width 0.08 m (proposed: long enough for a visible tip sweep). Fixed contact post with a navy gap of 0.15 m from the cold tip (proposed: closes at about 314 K, see below).
- **Mass and material.** Static rigid mass zero for the base (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L70-L76`). Strip node 0.05 kg, c = 95 J/(kg·K) (brass 380 × ¼, batch scale; 4.75 J/K). Strip face: metal 64 W/K (batch scale), pairing at 32 W/K with a heating plate; ambient exchange 2.5 × 0.064 m² ≈ 0.16 W/K. Layer expansion coefficients are a declared exaggeration of about 50× SI, above the research expansion row's scaled range of 2⁻²⁰–2⁻¹⁴ 1/K (the listed 2⁻¹⁰–2⁻⁴ times its 2⁻¹⁰ scale): α₂ = 2⁻¹⁰ ≈ 9.8 × 10⁻⁴ 1/K (cream layer, about 50 × brass's SI 1.9 × 10⁻⁵ and 16× the row's scaled maximum) and α₁ = 2⁻⁹ ≈ 1.95 × 10⁻³ 1/K (gold layer, twice α₂) (proposed: SI millimetre bending would be invisible at catalogue scale; the 2:1 ratio exaggerates the brass-on-steel differential so the sweep reads clearly).
- **Constraints.** The strip is a compliant cantilever on the base; its natural curvature κ = 1.5·(α₁ − α₂)·ΔT / t (equal-thickness, equal-modulus bimetal law, t = 0.02 m) feeds the constraint rows as stiffness-scaled displacement, never a direct transform write (research expansion coupling). Tip deflection ≈ κL²/2 ≈ 5.9 mm/K (derived), so the 0.15 m gap closes near ΔT = 26 K. Contact detent: separation needs 12 mm extra retreat (proposed: about 2 K of mechanical hysteresis at 5.9 mm/K, matching the research ≥ 2 K sensor hysteresis rule).
- **Typed ports.** Two electrical terminals: `PowerIn` (input) and `Supply` (switched output), `Electrical` domain (`engine/gpu/WorkshopConnections.cs@a6c914e:L8-L11`). They conduct only while the strip touches the post; no signal input.
- **Sensors and activation.** None: "temperature is not a magic switch instruction" (row).
- **Work and energy stores.** Strip node enthalpy; elastic energy in the bent strip; the switched circuit's energy belongs to its supply.
- **Parameters.**

  | Parameter | Type | Range | Default | Unit | Source |
  | --- | --- | --- | --- | --- | --- |
  | Strip material | enum `Bimetal`/`Uniform` | closed set | `Bimetal` | — | (proposed: above) |
  | α₁ / α₂ | f32 | fixed | 2⁻⁹ / 2⁻¹⁰ | 1/K | (proposed: about 50× SI exaggeration, above) |
  | Contact gap | f32 | fixed | 0.15 | m | (proposed: above) |
  | Detent retreat | f32 | fixed | 0.012 | m | (proposed: above) |

- **Cosmetic curves and UI bindings.** Strip bend ← committed strain ([research per-element table](../../thermal-component-research.md)); the navy gap is the contact state.
- **Art.** "Paired gold/cream curved strip with a visible navy contact gap" (row). Nearest [DESIGN.md](../../../DESIGN.md) source colours (proposed mapping): gold `#f7cb52` (`DESIGN.md@a6c914e:L154-L154`), cream `#ead39b` (`DESIGN.md@a6c914e:L146-L146`), navy `#293954` (`DESIGN.md@a6c914e:L147-L147`).
- **Catalogue and inventory entry.** New `WorkshopPartKind.BimetalThermostat`; id `bimetal_thermostat`, title "Bimetal thermostat", category Power (proposed: a switched electrical pass-through, beside the existing power parts).

## 3. Engine capabilities

Families (map row TH-22, [general-engine-element-map](../general-engine-element-map.md); binding [thermal-01.json](../../coverage/engine/thermal-01.json)): EnvironmentState, FiniteLedger, GeometryQuery, JointConstraint, RigidBodyDynamics, SensibleHeat, SignalPropagation, ThermalExpansion, ThermalStress, TopologyTransaction; JSON adds StateTransaction. Decision note S585-D: bonded differential expansion and contact hysteresis, not a temperature switch.

- **Exists now.** Static base, box colliders, contact queries (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L120`); typed electrical sockets (`engine/gpu/WorkshopConnections.cs@a6c914e:L8-L11`); Run/Reset and save (`engine/gpu/ActivationNetwork.cs@a6c914e:L67-L67`, `engine/gpu/WorkshopSaveCodec.cs@a6c914e:L6-L10`).
- **Missing.** Compliant cantilever joint: Story 6.4 soft constraints. ElectricalPower and contact-switched routing: Story 8.1. [S543](../invest/decisions.md#s543): expansion → S557, thermal-stress → S558. S585-D has no row in `decisions.md`. Unscheduled.
- **Dependencies.** A heat source; Battery and a supplied load (Epic 8).

## 4. Sources and legacy

- **Requirement row** [thermal-22](../requirements.md#thermal-22): bonded two-material strip bends by differential expansion and operates a real contact; no variants.
- **Named entry** [thermal-22](../invest/named-elements.md#thermal-22): owner S585; "a uniform-material control lacks the same curvature; verify contact opening/closing and hysteresis from declared geometry/material mechanics".
- **Research** [TH-S06](../../thermal-component-research.md#th-s06), expansion coupling row ("strain feeds a joint/contact row as stiffness-scaled displacement"), expansion coefficient row 2⁻¹⁰–2⁻⁴ with scale 2⁻¹⁰ (2⁻²⁰–2⁻¹⁴ 1/K), and per-element table ("differential expansion → contact"; bend coefficient; no battery needed to move).
- **Processes and scenarios.** [IX-31](../requirements.md#interaction-31), [IX-32](../requirements.md#interaction-32), [IX-06](../requirements.md#interaction-06); [TX-06](../requirements.md#thermal-scenario-06).
- **Legacy.** None found. Searched `parts/`, `engine/physics/`, `engine/*.cs`, `engine/bridge/`, `CuriousContraptions.tests/`, `content/puzzles.json`, `tools/Campaign` and `reference/` at a6c914e for bimetal, thermostat, expansion and strain terms. Consulted, no element knowledge: `reference/P0-022-before/docs/coverage/engine/task-002.json@a6c914e:L2783-L2787` (historical coverage-scope rows).

## 5. Acceptance outline

Point of truth: [thermal-22](../requirements.md#thermal-22), [TX-06](../requirements.md#thermal-scenario-06) and [ELEMENT acceptance](../requirements.md#accept-element).

- **Chrome recipe.** Through the real palette and sockets: Battery → thermostat `PowerIn`; thermostat `Supply` → Motor `PowerIn`; a supplied Heating plate (TH-04) under the strip.
- **Positive.** The 4.75 J/K strip warms through the 32 W/K pair, bends visibly, touches the post near 314 K and the motor runs; when heating stops it cools, retreats past the detent and the motor stops.
- **Negative or control.** `Uniform` strip in the same place: no curvature, no contact. Insufficient heating (plate off early): the gap stays open. Contact closed but no battery: the motor does not run.
- **Boundaries.** Deflection follows κ within the envelope; closing and opening temperatures differ by about 2 K; strain never writes the transform directly.
- **Run/Reset.** Restores straight strip, open contact, 288 K and the circuit exactly.
- **Save/Load.** Pose, material preset and connections round-trip; Run after reload reproduces the outcome.
- **Integrations.** TX-06; campaign 83, 84, 90, 145.

## 6. Open questions

1. **Batch N game scale.** Adopt or revise the one scale in section 2 (×¼ specific heat, ×1/16 latent heat, gas ×1, reaction energy and O₂ ×1/1024, h 2.5 W/(m²·K), per-pair series contact conductance from per-part face values, 1 intensity unit = 1 W, and which research-table rows the scale multiplies). It carries two consequences: boiling needs a declared saturation curve (S550) because scaled water latent heat is below R·T_sat, and gas-node time constants run about 4× slower than SI (TH-25). Owner decision.
2. **Control preset.** Is the `Uniform` strip a declared preset of this part or a separate control fixture? Owner decision.
3. **S585-D.** The map cites decision S585-D, which has no row in `decisions.md`. Owner decision.
4. **Hysteresis mechanism.** Detent versus snap-disc geometry. Owner decision (S557).
5. **Expansion exaggeration.** α₂ = 2⁻¹⁰ and α₁ = 2⁻⁹ 1/K are about 50× SI and 16–32× the research row's scaled maximum of 2⁻¹⁴ 1/K. Confirm the declared exaggeration, or widen the research row. Owner decision.
