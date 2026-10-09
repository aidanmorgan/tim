# TH-37 · Heat-sensitive target — named-identity readiness spec

Story 7.0 named-identity spec ([spec](../../../_bmad-output/implementation-artifacts/spec-7-0-element-implementation-readiness.md)). Citations are `path@a6c914e:Lstart-Lend`. No legacy holds heat-target values, so every design value without a cited source is marked "(proposed: …)" with a one-line justification; the owner may revise it.

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name | TH-37 · Heat-sensitive target |
| Type | Thermal · potential element (potential/conditional; source adoption retained) |
| Anchors | [requirements.md#thermal-37](../requirements.md#thermal-37) (sequence-task-471); [named-elements.md#thermal-37](../invest/named-elements.md#thermal-37) |
| Proof owner / research | S602 · [TH-S01 heat transfer and finite stores](../../thermal-component-research.md#th-s01) |
| Related identities | Historical record [todo-304](../requirements.md#todo-304) is shared with [TH-06 Converging lens](TH-06-converging-lens.md); the lens → heat-sensitive latch integration is [sequence-task-402](../requirements.md#sequence-task-402). Goal sibling of [CAT-004 Receiver](CAT-004-basket.md) (capture goal) and sensor sibling of [TH-21](TH-21-temperature-sensor.md). Receiver in the [TH-01](TH-01-candle.md), [TH-26](TH-26-thermal-storage-block.md), [TH-27](TH-27-phase-change-cartridge.md) and [TH-34](TH-34-timed-toaster-ejector.md) recipes. |
| Campaign | Intro 15; practice 16; reuse 59, 90, 145 ([thermal allocation](../requirements.md#thermal-campaign-allocation)); the optical chapter notes "heat target returns in 81–90". |
| Roadmap story | Unscheduled. No epic in `_bmad-output/planning-artifacts/epics.md` schedules a thermal element. |
| Status | Not started. No `WorkshopPartKind` member (`engine/gpu/WorkshopPartKind.cs@a6c914e:L3-L3`); no legacy part. |

## 2. Declaration

- **Batch N game scale** (proposed, identical in all 37 TH specs; chains derived against it are in [TH-17](TH-17-kettle.md#batch-n-chains); owner question in section 6). Ambient reservoir 288 K and 101.3 kPa. [Research table](../../thermal-component-research.md) rows: specific heat, latent heat and source power read as listed range × scale (64–4096 J/(kg·K), 8192–2,097,152 J/kg, 0.5–2048 W); temperature (128–2048 K), conductance (2⁻¹⁰–2⁸ W/K), fuel/supply (finite), ignition threshold (400–1200 K) and sensor thresholds read as listed, without scale; expansion coefficient reads 2⁻²⁰–2⁻¹⁴ 1/K after scale, which TH-22 and TH-23 exceed on purpose as a declared exaggeration. Condensed-phase specific heat SI × ¼ and latent heat SI × 1/16, so catalogue-scale bodies change temperature or phase within one Run. Gas R and Cv SI × 1, so γ and pressure stay physical; each condensed phase's enthalpy is referenced so vapour made at T_sat carries m·Cv·T_sat and the scaled latent heat is exactly the heat consumed; because water's scaled latent heat (141 kJ/kg) is below R·T_sat (172 kJ/kg), boiling follows a declared saturation curve, not Clausius–Clapeyron. Every fuel's reaction energy and O₂ demand per kilogram SI × 1/1024 (wax 41,016 J/kg and 3.4/1024 kg O₂/kg; wood and cellulose 15,625 J/kg and 1.2/1024 kg O₂/kg), so a 0.3 kg candle holds about 12 kJ, heat per kilogram of O₂ keeps Thornton's ≈ 13 MJ/kg, and fuel mass is consumed 1:1 so burn-down stays visible. Still-air surface exchange h = 2.5 W/(m²·K) (SI × ¼, so condensed-body time constants C/(hA) stay SI-like while gas-node ones run about 4× slower than SI); forced air h = 2.5 + u (u in m/s). Contact pairs conduct G = G_a·G_b/(G_a + G_b) from each part's face conductance: metal 64, ceramic, wood, ice or soft 16, probe tip 0.5 W/K. Every exchange is clamped per 480 Hz substep to the equalising amount ΔT·C_a·C_b/(C_a + C_b), so small nodes never overshoot. Reactive surfaces carry a thermal-only patch node outside rigid mass (fibre 10 mg, wood 0.5 g; area 0.0025 m²; patch-to-bulk 0.01 W/K; thermal nodes 10⁻⁶–1024 kg, not bound by the 1/1024 kg rigid minimum) that takes strikes, flames and light first. Radiant optical power: 1 legacy intensity unit = 1 W, so the flashlight emits 24 W. Canonical f32 ([numeric contract](../../gpu-f32-physics.md#game-grade-envelope)).
- **Variants.** None named in the row.
- **Bodies and shapes.** One static body: target disc modelled as a box, half-extents 0.02 × 0.24 × 0.24 m, placed upright or laid flat with the ordinary gizmo (proposed: a 0.48 m face covers a 0.2 m plume, a lens spot or a resting body; colliders are boxes, `engine/gpu/PhysicsDeclarations.cs@a6c914e:L89-L120`).
- **Mass and material.** Static rigid mass zero (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L70-L76`), so its thermal node is declared separately: 0.2 kg ceramic, c = 210 J/(kg·K) (ceramic 840 × ¼, batch scale; 42 J/K). Ceramic face 16 W/K (batch scale): pairs at 12.8 W/K with a metal body, 8 W/K with a ceramic or soft body. Ambient loss 2.5 × 0.54 m² ≈ 1.35 W/K (batch scale; passive time constant 31 s). Optical absorptance 0.9 (proposed: matte face receives contact, plume and light alike).
- **Constraints.** None.
- **Typed ports.** Optional `WorkshopSocket.ActivationOut`, `Activation` domain (`engine/gpu/WorkshopConnections.cs@a6c914e:L8-L11`), mirroring the goal state for downstream signals (proposed: SignalPropagation in the map row). No heating input: sensing is separate from the source (row).
- **Sensors and activation.** Goal observation: the target's own node temperature ≥ threshold continuously for the dwell, evaluated at substep endpoints. Default threshold 330 K, dwell 120 ticks = 1 s at 120 Hz (proposed: clearly above ambient; one second rejects transient spikes). The objective references this target by typed identity, so warming any other body never counts.
- **Work and energy stores.** The thermal node only; observation creates no heat (IX-17). Reference receipts ([batch chains](TH-17-kettle.md#batch-n-chains)): a candle's 75 W plume → about 343 K, passing 330 K after about 44 s; a 400 K TH-26 block through 8 W/K → peaks near 367 K about 15 s after contact, because the block itself keeps losing 3.2 W/K to the room; a 303 K TH-27 cartridge through 12.8 W/K → about 302 K (derived).
- **Parameters.**

  | Parameter | Type | Range | Default | Unit | Source |
  | --- | --- | --- | --- | --- | --- |
  | Target temperature | f32 | 300–600, step 5 | 330 | K | (proposed: level-authored; covers warm to near-ignition goals) |
  | Dwell | u32 | 30–600 | 120 | ticks | (proposed: 0.25–5 s) |
  | Thermal mass | f32 | fixed | 0.2 | kg | (proposed: above) |
  | Face conductance | f32 | fixed | 16 | W/K | (batch scale: ceramic face) |
  | Absorptance | f32 | fixed | 0.9 | fraction | (proposed: above) |

- **Cosmetic curves and UI bindings.** Tint ← temperature ([research per-element table](../../thermal-component-research.md)), always with the navy degree marks and the gold shaped state indicator (row); the goal UI uses the existing goal feedback (`engine/gpu/WorkshopUi.cs@a6c914e:L7-L13`).
- **Art.** "Cream target disc with navy degree marks and a gold shaped state indicator" (row). Nearest [DESIGN.md](../../../DESIGN.md) source colours (proposed mapping): cream `#ead39b` (`DESIGN.md@a6c914e:L146-L146`), navy `#293954` (`DESIGN.md@a6c914e:L147-L147`), gold `#f7cb52` (`DESIGN.md@a6c914e:L154-L154`).
- **Catalogue and inventory entry.** New `WorkshopPartKind.HeatTarget`; id `heat_target`, title "Heat-sensitive target", category Goals (proposed: beside the Receiver and Signal lamp goals).

## 3. Engine capabilities

Families (map row TH-37, [general-engine-element-map](../general-engine-element-map.md); binding [thermal-01.json](../../coverage/engine/thermal-01.json)): FiniteLedger, ObjectiveEvaluation, SensibleHeat, SignalPropagation, TemperatureSensing; the JSON adds StateTransaction, ReliablePublication and TypedContracts. Decision note LAW-GOALS-D: exact quantity, window, order and final predicate.

- **Exists now.** Goal evaluator (`engine/gpu/WorkshopGoalEvaluator.cs@a6c914e:L5-L8`); dwell-counted residence sensors as the closest dwell precedent (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L123-L141`); activation signals and ports (`engine/gpu/ActivationNetwork.cs@a6c914e:L7-L14`, `engine/gpu/WorkshopConnections.cs@a6c914e:L8-L11`); Run/Reset and save (`engine/gpu/ActivationNetwork.cs@a6c914e:L67-L67`, `engine/gpu/WorkshopSaveCodec.cs@a6c914e:L6-L10`).
- **Missing.** Temperature goal predicate: [S635](../invest/decisions.md#s635) goal-occurrences → LAW-GOALS-I. TemperatureSensing (IX-17) has no S543 row. [S543](../invest/decisions.md#s543) conduction → S544; absorption [S484](../invest/decisions.md#s484) → S489. Unscheduled.
- **Dependencies.** Any heat source (TH-01, TH-04, TH-06 with light, TH-26).

## 4. Sources and legacy

- **Requirement row** [thermal-37](../requirements.md#thermal-37): finite thermal body exposes a typed temperature/phase goal with explicit threshold and dwell; sensing separate from the source; no variants.
- **Named entry** [thermal-37](../invest/named-elements.md#thermal-37): owner S602; "warm another disconnected target or remain below the target threshold and the goal stays false".
- **Research** per-element table: "goal node with threshold"; target T, dwell; "solved when held above temperature for the dwell"; tint ← T.
- **Processes.** [IX-17](../requirements.md#interaction-17), [IX-18](../requirements.md#interaction-18), [IX-14](../requirements.md#interaction-14), [IX-43](../requirements.md#interaction-43).
- **Legacy.** None found. Searched `parts/`, `engine/physics/`, `engine/*.cs`, `engine/bridge/`, `CuriousContraptions.tests/`, `content/puzzles.json`, `tools/Campaign` and `reference/` at a6c914e for heat target, temperature goal and thermal terms. Consulted, no element knowledge: `reference/P0-022-before/docs/coverage/engine/task-002.json@a6c914e:L2783-L2787` (historical coverage-scope rows).

## 5. Acceptance outline

Point of truth: [thermal-37](../requirements.md#thermal-37), [sequence-task-402](../requirements.md#sequence-task-402) and [ELEMENT acceptance](../requirements.md#accept-element).

- **Chrome recipe.** Through the real palette and sockets: a level whose goal names this Heat-sensitive target; Battery → Switch → Heating plate (TH-04) with the target laid flat on it (12.8 W/K pair).
- **Positive.** Plate and target warm together at about 5 K/s with the target about 17 K behind the plate disc (the 210 W it absorbs crosses the 12.8 W/K pair); it passes 330 K after about 12 s, holds for 120 ticks and the goal shows Solved once.
- **Negative or control.** A second, disconnected target placed on the plate instead: the goal stays false. Plate switched off after 8 s: plate and target equalise and the target peaks near 325 K: false. A 100-tick excursion above 330 K: false.
- **Boundaries.** 329.9 K never solves; the dwell resets if the temperature drops below threshold; observation adds no heat.
- **Run/Reset.** Restores 288 K, dwell count 0 and goal Waiting exactly.
- **Save/Load.** Pose, threshold and dwell round-trip; Run after reload reproduces the outcome.
- **Integrations.** Lens latch integration (sequence-task-402); receiver in TX scenarios and the TH-01, TH-26, TH-27 and TH-34 recipes; campaign 15, 16, 59, 90, 145.

## 6. Open questions

1. **Batch N game scale.** Adopt or revise the one scale in section 2 (×¼ specific heat, ×1/16 latent heat, gas ×1, reaction energy and O₂ ×1/1024, h 2.5 W/(m²·K), per-pair series contact conductance from per-part face values, 1 intensity unit = 1 W, and which research-table rows the scale multiplies). It carries two consequences: boiling needs a declared saturation curve (S550) because scaled water latent heat is below R·T_sat, and gas-node time constants run about 4× slower than SI (TH-25). Owner decision.
2. **Phase goals.** The row allows a "temperature/phase goal"; should the target also support a phase predicate (for example melted fraction)? Owner decision (LAW-GOALS-I).
3. **Dwell semantics.** Continuous dwell (reset on dip) versus cumulative time above threshold. Owner decision.
4. **JSON extras.** The binding adds ReliablePublication and TypedContracts that the map row omits. Owner decision.
