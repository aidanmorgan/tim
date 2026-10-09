# TH-04 · Electrical heating plate — named-identity readiness spec

Story 7.0 named-identity spec ([spec](../../../_bmad-output/implementation-artifacts/spec-7-0-element-implementation-readiness.md)). Citations are `path@a6c914e:Lstart-Lend`. No legacy holds heating-plate values, so every design value without a cited source is marked "(proposed: …)" with a one-line justification; the owner may revise it.

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name | TH-04 · Electrical heating plate |
| Type | Thermal · potential element (potential/conditional; source adoption retained) |
| Anchors | [requirements.md#thermal-04](../requirements.md#thermal-04) (sequence-task-438); [named-elements.md#thermal-04](../invest/named-elements.md#thermal-04) |
| Proof owner / research | S567 · [TH-S01 heat transfer and finite stores](../../thermal-component-research.md#th-s01) |
| Related identities | Supplied by [CAT-005 Battery](CAT-005-battery.md) through electrical routing; first heat slice partners [TH-21 Temperature sensor](TH-21-temperature-sensor.md) and [CAT-035 Signal lamp](CAT-035-lamp.md); the heater inside [TH-34](TH-34-timed-toaster-ejector.md); heat source for [TH-17 Kettle](TH-17-kettle.md) in TX-01. |
| Campaign | Intro 14; practice 15; reuse 55, 70, 147 ([thermal allocation](../requirements.md#thermal-campaign-allocation)) |
| Roadmap story | Unscheduled. The research doc names TH-04 + TH-21 + Signal lamp as the first heat slice ([first heat slice](../../thermal-component-research.md)), but no epic in `_bmad-output/planning-artifacts/epics.md` schedules it. |
| Status | Not started. No `WorkshopPartKind` member (`engine/gpu/WorkshopPartKind.cs@a6c914e:L3-L3`); no legacy part. |

## 2. Declaration

- **Batch N game scale** (proposed, identical in all 37 TH specs; chains derived against it are in [TH-17](TH-17-kettle.md#batch-n-chains); owner question in section 6). Ambient reservoir 288 K and 101.3 kPa. [Research table](../../thermal-component-research.md) rows: specific heat, latent heat and source power read as listed range × scale (64–4096 J/(kg·K), 8192–2,097,152 J/kg, 0.5–2048 W); temperature (128–2048 K), conductance (2⁻¹⁰–2⁸ W/K), fuel/supply (finite), ignition threshold (400–1200 K) and sensor thresholds read as listed, without scale; expansion coefficient reads 2⁻²⁰–2⁻¹⁴ 1/K after scale, which TH-22 and TH-23 exceed on purpose as a declared exaggeration. Condensed-phase specific heat SI × ¼ and latent heat SI × 1/16, so catalogue-scale bodies change temperature or phase within one Run. Gas R and Cv SI × 1, so γ and pressure stay physical; each condensed phase's enthalpy is referenced so vapour made at T_sat carries m·Cv·T_sat and the scaled latent heat is exactly the heat consumed; because water's scaled latent heat (141 kJ/kg) is below R·T_sat (172 kJ/kg), boiling follows a declared saturation curve, not Clausius–Clapeyron. Every fuel's reaction energy and O₂ demand per kilogram SI × 1/1024 (wax 41,016 J/kg and 3.4/1024 kg O₂/kg; wood and cellulose 15,625 J/kg and 1.2/1024 kg O₂/kg), so a 0.3 kg candle holds about 12 kJ, heat per kilogram of O₂ keeps Thornton's ≈ 13 MJ/kg, and fuel mass is consumed 1:1 so burn-down stays visible. Still-air surface exchange h = 2.5 W/(m²·K) (SI × ¼, so condensed-body time constants C/(hA) stay SI-like while gas-node ones run about 4× slower than SI); forced air h = 2.5 + u (u in m/s). Contact pairs conduct G = G_a·G_b/(G_a + G_b) from each part's face conductance: metal 64, ceramic, wood, ice or soft 16, probe tip 0.5 W/K. Every exchange is clamped per 480 Hz substep to the equalising amount ΔT·C_a·C_b/(C_a + C_b), so small nodes never overshoot. Reactive surfaces carry a thermal-only patch node outside rigid mass (fibre 10 mg, wood 0.5 g; area 0.0025 m²; patch-to-bulk 0.01 W/K; thermal nodes 10⁻⁶–1024 kg, not bound by the 1/1024 kg rigid minimum) that takes strikes, flames and light first. Radiant optical power: 1 legacy intensity unit = 1 W, so the flashlight emits 24 W. Canonical f32 ([numeric contract](../../gpu-f32-physics.md#game-grade-envelope)).
- **Variants.** None named in the row.
- **Bodies and shapes.** One static body: plinth box, half-extents 0.30 × 0.10 × 0.30 m (proposed: a 0.6 m square top seats a 0.68 m Basketball, a 0.4 m block or the 0.2 m kettle). Contact face: the top face; bodies touching it receive heat.
- **Mass and material.** Static rigid mass zero (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L70-L76`). Inset disc thermal node 0.5 kg steel, c = 122 J/(kg·K) (steel 490 × ¼, batch scale; 61 J/K). Plinth is thermally inert (proposed: keeps all heat in the disc). Disc face: metal, 64 W/K (batch scale), so a metal-based kettle pairs at 32 W/K and a ceramic body at 12.8 W/K. Ambient exchange from the 0.36 m² top: 2.5 × 0.36 = 0.9 W/K (batch scale). Contact friction 0.6, restitution 0.05 (proposed: matte ceramic like the Domino, `engine/gpu/WorkshopDomino.cs@a6c914e:L9-L9`).
- **Constraints.** None.
- **Typed ports.** One electrical input: `WorkshopSocket.PowerIn` in the `Electrical` domain, direction `Input` (types exist: `engine/gpu/WorkshopConnections.cs@a6c914e:L8-L11`). No signal input: switching is done by the circuit (S257 typed power versus signal).
- **Sensors and activation.** None.
- **Work and energy stores.** Electrical load rating 512 W (proposed: a small real hotplate element). Delivered power = min(rating, power the supply allocates); all delivered work enters the disc (IX-39). The disc warms at 8.4 K/s unloaded. Unloaded it settles at 288 + 512/0.9 ≈ 857 K, hot enough to ignite combustibles left on it (physical, recorded as a hazard). With the TH-17 kettle on it at 373 K the disc settles at 386 K and delivers 32 × 13 ≈ 423 W into the kettle; plate and kettle together reach the first 2 g of vapour in about 20 s ([batch chains](TH-17-kettle.md#batch-n-chains)).
- **Parameters.**

  | Parameter | Type | Range | Default | Unit | Source |
  | --- | --- | --- | --- | --- | --- |
  | Load rating | f32 | fixed | 512 | W | (proposed: above) |
  | Disc thermal mass | f32 | fixed | 0.5 | kg | (proposed: warms visibly in seconds) |
  | Disc face conductance | f32 | fixed | 64 | W/K | (batch scale: metal face) |

- **Cosmetic curves and UI bindings.** Glow ← committed delivered power ([research per-element table](../../thermal-component-research.md)); navy witness marks give a shaped temperature reading (row). The supply socket follows the existing socket gizmo.
- **Art.** "Cream plinth, gold inset disc and navy heat witness marks" (row). Nearest [DESIGN.md](../../../DESIGN.md) source colours (proposed mapping): cream `#ead39b` (`DESIGN.md@a6c914e:L146-L146`), gold `#f7cb52` (`DESIGN.md@a6c914e:L154-L154`), navy `#293954` (`DESIGN.md@a6c914e:L147-L147`).
- **Catalogue and inventory entry.** New `WorkshopPartKind.HeatingPlate`; id `heating_plate`, title "Heating plate", category Heat (proposed: no current category covers thermal parts).

## 3. Engine capabilities

Families (map row TH-04, [general-engine-element-map](../general-engine-element-map.md); binding [thermal-01.json](../../coverage/engine/thermal-01.json)): ElectricalDissipation, ElectricalPower, FiniteLedger, GeometryQuery, SensibleHeat, ThermalConduction; JSON adds StateTransaction.

- **Exists now.** Static body and box collider (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L120`); typed `Electrical` domain and `PowerIn` socket enums (`engine/gpu/WorkshopConnections.cs@a6c914e:L8-L11`); Run/Reset and save (`engine/gpu/ActivationNetwork.cs@a6c914e:L67-L67`, `engine/gpu/WorkshopSaveCodec.cs@a6c914e:L6-L10`).
- **Missing.** ElectricalPower: Story 8.1 battery network ([S257](../invest/decisions.md#s257) electrical-port → S270). [S543](../invest/decisions.md#s543): electrical-heat → S564, conduction → S544; SensibleHeat (IX-43) has no S543 row. Thermal rows unscheduled.
- **Dependencies.** A supply (CAT-005 Battery, Story 8.1); a receiver (TH-21, TH-37, TH-17).

## 4. Sources and legacy

- **Requirement row** [thermal-04](../requirements.md#thermal-04): finite electrical load converts supplied work to enthalpy in its contacting surface; any thermal body may receive it; no variants.
- **Named entry** [thermal-04](../invest/named-elements.md#thermal-04): owner S567; "disconnected or exhausted supply cannot create new heat".
- **Research** per-element table ("electrical node + source", watt rating, glow ← power) and the [first heat slice](../../thermal-component-research.md) recipe: Battery → Switch → Heating plate under a Temperature sensor → Signal lamp.
- **Processes and scenarios.** [IX-06](../requirements.md#interaction-06), [IX-39](../requirements.md#interaction-39), [IX-18](../requirements.md#interaction-18), [IX-43](../requirements.md#interaction-43); [TX-01](../requirements.md#thermal-scenario-01).
- **Legacy.** None found. Searched `parts/`, `engine/physics/`, `engine/*.cs`, `engine/bridge/`, `CuriousContraptions.tests/`, `content/puzzles.json`, `tools/Campaign` and `reference/` at a6c914e for heater, resistive, dissipation and heat terms. Consulted, no element knowledge: `reference/P0-022-before/docs/coverage/engine/task-002.json@a6c914e:L2783-L2787` (historical coverage-scope rows).

## 5. Acceptance outline

Point of truth: [thermal-04](../requirements.md#thermal-04) and [ELEMENT acceptance](../requirements.md#accept-element).

- **Chrome recipe.** The research first-slice recipe through actual palette, gizmo and socket controls: Battery → Switch → Heating plate, a Temperature sensor (TH-21) probe on the plate, sensor output → Signal lamp.
- **Positive.** Run with the switch closed: the disc glows with delivered power and passes 330 K after about 5 s; the probe (pair 0.5 W/K, time constant about 3.8 s) follows and the lamp lights once the On threshold holds for its dwell, about 10 s after Run.
- **Negative or control.** Unsupplied plate: needle stays at 288 K, lamp dark. Exhausted battery: heating stops when supply ends. An Insulating panel (TH-09) between plate and probe delays or prevents the rise.
- **Boundaries.** Supply allocation below rating gives proportionally less heat; delivered heat equals battery debit; a switched-off plate cools toward 288 K (time constant 61/0.9 ≈ 68 s without contacts) and never re-lights the lamp.
- **Run/Reset.** Restores disc temperature 288 K, supply state and sensor state exactly.
- **Save/Load.** Pose and the `PowerIn` connection round-trip; Run after reload reproduces the outcome.
- **Integrations.** TX-01 heat source swap (kettle about 20 s); TH-34 heater; campaign 14, 15, 55, 70, 147.

## 6. Open questions

1. **Batch N game scale.** Adopt or revise the one scale in section 2 (×¼ specific heat, ×1/16 latent heat, gas ×1, reaction energy and O₂ ×1/1024, h 2.5 W/(m²·K), per-pair series contact conductance from per-part face values, 1 intensity unit = 1 W, and which research-table rows the scale multiplies). It carries two consequences: boiling needs a declared saturation curve (S550) because scaled water latent heat is below R·T_sat, and gas-node time constants run about 4× slower than SI (TH-25). Owner decision.
2. **Unloaded temperature.** At 0.9 W/K ambient loss an unloaded plate settles near 857 K and can ignite combustibles; accept, or add a thermal cut-out. Owner decision.
3. **Rating.** Fixed 512 W or player-selectable ratings. Owner decision.
4. **Signal input.** Should the plate carry its own on/off signal input, or is switching always a circuit element? Owner decision (S257).
