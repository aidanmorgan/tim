# TH-07 · Solar absorber plate — named-identity readiness spec

Story 7.0 named-identity spec ([spec](../../../_bmad-output/implementation-artifacts/spec-7-0-element-implementation-readiness.md)). Citations are `path@a6c914e:Lstart-Lend`. No legacy holds absorber values, so every design value without a cited source is marked "(proposed: …)" with a one-line justification; the owner may revise it.

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name | TH-07 · Solar absorber plate |
| Type | Thermal · potential element (potential/conditional; source adoption retained) |
| Anchors | [requirements.md#thermal-07](../requirements.md#thermal-07) (sequence-task-441); [named-elements.md#thermal-07](../invest/named-elements.md#thermal-07) |
| Proof owner / research | S573 · [TH-S04 optical heating](../../thermal-component-research.md#th-s04) |
| Related identities | No CAT spec. Distinct from [CAT-059 Solar panel](CAT-059-solar_panel.md) / [EL-211](../invest/named-elements.md#element-211), which convert light to electrical work; this plate converts it to heat. Sources: [CAT-029](CAT-029-flashlight.md), [CAT-036](CAT-036-laser.md), [TH-06](TH-06-converging-lens.md); onward routing by [TH-08](TH-08-heat-conducting-bar.md); readout by [TH-21](TH-21-temperature-sensor.md). |
| Campaign | Intro 55; practice 56; reuse 69, 131 ([thermal allocation](../requirements.md#thermal-campaign-allocation)) |
| Roadmap story | Unscheduled. No epic in `_bmad-output/planning-artifacts/epics.md` schedules a thermal element. |
| Status | Not started. No `WorkshopPartKind` member (`engine/gpu/WorkshopPartKind.cs@a6c914e:L3-L3`); no legacy part. |

## 2. Declaration

- **Batch N game scale** (proposed, identical in all 37 TH specs; chains derived against it are in [TH-17](TH-17-kettle.md#batch-n-chains); owner question in section 6). Ambient reservoir 288 K and 101.3 kPa. [Research table](../../thermal-component-research.md) rows: specific heat, latent heat and source power read as listed range × scale (64–4096 J/(kg·K), 8192–2,097,152 J/kg, 0.5–2048 W); temperature (128–2048 K), conductance (2⁻¹⁰–2⁸ W/K), fuel/supply (finite), ignition threshold (400–1200 K) and sensor thresholds read as listed, without scale; expansion coefficient reads 2⁻²⁰–2⁻¹⁴ 1/K after scale, which TH-22 and TH-23 exceed on purpose as a declared exaggeration. Condensed-phase specific heat SI × ¼ and latent heat SI × 1/16, so catalogue-scale bodies change temperature or phase within one Run. Gas R and Cv SI × 1, so γ and pressure stay physical; each condensed phase's enthalpy is referenced so vapour made at T_sat carries m·Cv·T_sat and the scaled latent heat is exactly the heat consumed; because water's scaled latent heat (141 kJ/kg) is below R·T_sat (172 kJ/kg), boiling follows a declared saturation curve, not Clausius–Clapeyron. Every fuel's reaction energy and O₂ demand per kilogram SI × 1/1024 (wax 41,016 J/kg and 3.4/1024 kg O₂/kg; wood and cellulose 15,625 J/kg and 1.2/1024 kg O₂/kg), so a 0.3 kg candle holds about 12 kJ, heat per kilogram of O₂ keeps Thornton's ≈ 13 MJ/kg, and fuel mass is consumed 1:1 so burn-down stays visible. Still-air surface exchange h = 2.5 W/(m²·K) (SI × ¼, so condensed-body time constants C/(hA) stay SI-like while gas-node ones run about 4× slower than SI); forced air h = 2.5 + u (u in m/s). Contact pairs conduct G = G_a·G_b/(G_a + G_b) from each part's face conductance: metal 64, ceramic, wood, ice or soft 16, probe tip 0.5 W/K. Every exchange is clamped per 480 Hz substep to the equalising amount ΔT·C_a·C_b/(C_a + C_b), so small nodes never overshoot. Reactive surfaces carry a thermal-only patch node outside rigid mass (fibre 10 mg, wood 0.5 g; area 0.0025 m²; patch-to-bulk 0.01 W/K; thermal nodes 10⁻⁶–1024 kg, not bound by the 1/1024 kg rigid minimum) that takes strikes, flames and light first. Radiant optical power: 1 legacy intensity unit = 1 W, so the flashlight emits 24 W. Canonical f32 ([numeric contract](../../gpu-f32-physics.md#game-grade-envelope)).
- **Variants.** None named in the row. The "reflective comparison" is a control construction (see Acceptance), not a declared preset; see Open questions.
- **Bodies and shapes.** One static body: upright slab box, half-extents 0.04 × 0.15 × 0.15 m, absorbing face +X (proposed: a 0.3 m face intercepts only about 40 % of the 0.536 m wide 15° flashlight cone at 1 m, `parts/FlashlightPart.cs@a6c914e:L11-L13`, so concentrating the whole beam with TH-06 visibly pays off). Rear: a cream insulating slab with a gold contact pad as the thermal port.
- **Mass and material.** Static rigid mass zero (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L70-L76`). Thermal node 0.1 kg aluminium, c = 225 J/(kg·K) (aluminium 900 × ¼, batch scale; 22.5 J/K). Front absorptance 0.95 (proposed: matte black paint about 0.95); the 0.05 remainder is reflected. Front-face ambient exchange 2.5 × 0.09 m² = 0.225 W/K; the insulated rear adds none (proposed: the slab's purpose). Rear pad: metal face 64 W/K (batch scale).
- **Constraints.** None.
- **Typed ports.** None as sockets. The optical aperture is the spatial front face; the thermal port is the rear contact pad.
- **Sensors and activation.** None.
- **Work and energy stores.** Absorbed power = 0.95 × incident optical power over the illuminated area (IX-14); reflected power cannot also become heat. Shading removes input only; stored enthalpy decays by the 0.225 W/K loss (time constant 22.5/0.225 = 100 s).
- **Parameters.**

  | Parameter | Type | Range | Default | Unit | Source |
  | --- | --- | --- | --- | --- | --- |
  | Absorptance | f32 | fixed | 0.95 | fraction | (proposed: above) |
  | Face size | f32 | fixed | 0.30 × 0.30 | m | (proposed: above) |
  | Thermal mass | f32 | fixed | 0.1 | kg | (proposed: fast response at tens of watts) |

- **Cosmetic curves and UI bindings.** Plate tint ← temperature ([research per-element table](../../thermal-component-research.md)), always paired with the raised temperature marks (row) so colour is never the only cue.
- **Art.** "Matte navy inset on a cream slab, gold rim and raised temperature marks" (row). Nearest [DESIGN.md](../../../DESIGN.md) source colours (proposed mapping): navy `#293954` (`DESIGN.md@a6c914e:L147-L147`), cream `#ead39b` (`DESIGN.md@a6c914e:L146-L146`), gold `#f7cb52` (`DESIGN.md@a6c914e:L154-L154`).
- **Catalogue and inventory entry.** New `WorkshopPartKind.SolarAbsorber`; id `solar_absorber`, title "Solar absorber plate", category Heat (proposed: no current category covers thermal parts).

## 3. Engine capabilities

Families (map row TH-07, [general-engine-element-map](../general-engine-element-map.md); binding [thermal-01.json](../../coverage/engine/thermal-01.json)): FiniteLedger, GeometryQuery, OpticalAbsorption, OpticalTransport, SensibleHeat, ThermalConduction; JSON adds StateTransaction.

- **Exists now.** Static body and box collider (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L120`); Run/Reset and save (`engine/gpu/ActivationNetwork.cs@a6c914e:L67-L67`, `engine/gpu/WorkshopSaveCodec.cs@a6c914e:L6-L10`).
- **Missing.** OpticalTransport: Stories 13.1 and 13.5 ([S484](../invest/decisions.md#s484) → S485, S486, S488). OpticalAbsorption into heat → S484 absorption → S489. [S543](../invest/decisions.md#s543): conduction → S544, convection → S545, thermal-radiation → S546; SensibleHeat (IX-43) has no S543 row. Unscheduled.
- **Dependencies.** A finite optical source (CAT-029 or CAT-036, Story 13.1); optional TH-06 lens; TH-08 for onward routing.

## 4. Sources and legacy

- **Requirement row** [thermal-07](../requirements.md#thermal-07): absorbed flux becomes internal energy by absorptance and illuminated area; a thermal port carries it onward; no variants.
- **Named entry** [thermal-07](../invest/named-elements.md#thermal-07): owner S573; "a reflective comparison absorbs less; shading removes the input without immediately deleting stored heat".
- **Research** per-element table: "node with absorptance + optical receiver"; parameters absorptance, mass; "warms under a Flashlight/laser or lens spot"; tint ← T.
- **Processes and scenarios.** [IX-13](../requirements.md#interaction-13), [IX-14](../requirements.md#interaction-14), [IX-18](../requirements.md#interaction-18), [IX-20](../requirements.md#interaction-20); [TX-03](../requirements.md#thermal-scenario-03) source side.
- **Legacy (consulted, no absorber).** Legacy optics recorded "Absorb" as a reception of power for receivers, with no heat (`engine/OpticalNetwork.cs@a6c914e:L109-L111`). Carry forward only the fact that absorbed power ends the ray; do not carry forward the CPU trace. Legacy flashlight cone: range 8, 15° half-angle, intensity 24 (`parts/FlashlightPart.cs@a6c914e:L10-L13`), read as 24 W under the batch scale. Searched `parts/`, `engine/physics/`, `engine/*.cs`, `engine/bridge/`, `CuriousContraptions.tests/`, `content/puzzles.json`, `tools/Campaign` and `reference/` for absorber and heat terms.

## 5. Acceptance outline

Point of truth: [thermal-07](../requirements.md#thermal-07) and [ELEMENT acceptance](../requirements.md#accept-element). Numbers are derived in the [batch chains](TH-17-kettle.md#batch-n-chains).

- **Chrome recipe.** Through the real palette, gizmo and sockets: an activated Flashlight (CAT-029, 24 W) facing the absorber 1 m away; a Temperature sensor (TH-21, On set to 300 K) probe on the plate wired to a Signal lamp. Second run: a Converging lens (TH-06) 1.5 m from the flashlight with the absorber moved to the 3.0 m image behind it.
- **Positive (no lens).** The face intercepts 9.6 W and absorbs 9.1 W: the plate rises at 0.40 K/s toward 328 K (ΔT∞ 40.5 K) and the lamp lights after about 35 s.
- **Positive (with lens).** The whole beam reaches the face: 21.7 W absorbed, 0.96 K/s toward 384 K; the lamp lights after about 13 s. Absorbed heat = 0.95 × incident within the envelope in both runs.
- **Negative or control.** A Flat mirror (CAT-041) in the plate's place absorbs less and stays near ambient. A Wall moved into the beam mid-Run (shading): input stops and the plate cools only at its 100 s time constant.
- **Boundaries.** Partial illumination gives proportional input; no beam gives zero input; the plate never exceeds the temperature at which loss equals absorbed power.
- **Run/Reset.** Restores 288 K and all connected state exactly.
- **Save/Load.** Pose round-trips; Run after reload reproduces the outcome.
- **Integrations.** TH-06 focus onto the plate; TH-08 routing; campaign 55, 56, 69, 131.

## 6. Open questions

1. **Batch N game scale.** Adopt or revise the one scale in section 2 (×¼ specific heat, ×1/16 latent heat, gas ×1, reaction energy and O₂ ×1/1024, h 2.5 W/(m²·K), per-pair series contact conductance from per-part face values, 1 intensity unit = 1 W, and which research-table rows the scale multiplies). It carries two consequences: boiling needs a declared saturation curve (S550) because scaled water latent heat is below R·T_sat, and gas-node time constants run about 4× slower than SI (TH-25). Owner decision.
2. **Flashlight power.** Reading the legacy intensity 24 as 24 W radiant makes the flashlight about ten times a real torch; it is what lets light heat anything at catalogue scale. Owner decision (S489).
3. **Reflective comparison.** Is a reflective surface preset of this plate required (its own proof), or does a mirror or wall control satisfy the row? Owner decision.
