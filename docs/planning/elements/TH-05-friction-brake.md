# TH-05 · Friction brake — named-identity readiness spec

Story 7.0 named-identity spec ([spec](../../../_bmad-output/implementation-artifacts/spec-7-0-element-implementation-readiness.md)). Citations are `path@a6c914e:Lstart-Lend`. No legacy holds friction-brake values, so every design value without a cited source is marked "(proposed: …)" with a one-line justification; the owner may revise it.

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name | TH-05 · Friction brake |
| Type | Thermal · potential element (potential/conditional; source adoption retained) |
| Anchors | [requirements.md#thermal-05](../requirements.md#thermal-05) (sequence-task-439); [named-elements.md#thermal-05](../invest/named-elements.md#thermal-05) |
| Proof owner / research | S574 · [TH-S01 heat transfer and finite stores](../../thermal-component-research.md#th-s01) |
| Related identities | **Extends [EL-055 Mechanical brake](EL-055-mechanical-brake.md)** (Batch I, owner S341): "one brake identity with an added thermal proof". Bodies, modes, ports, capacity and the dissipation law are EL-055's and are adopted unchanged here; this spec adds only the thermal extension. Shaft supply [CAT-042 Motor](CAT-042-motor.md); receivers [TH-26](TH-26-thermal-storage-block.md), [TH-08](TH-08-heat-conducting-bar.md). No CAT spec. |
| Campaign | Intro 39; practice 40; reuse 81, 147 ([thermal allocation](../requirements.md#thermal-campaign-allocation)) |
| Roadmap story | Unscheduled (EL-055 is also unscheduled). |
| Status | Not started. No `WorkshopPartKind` member (`engine/gpu/WorkshopPartKind.cs@a6c914e:L3-L3`); no legacy part. |

## 2. Declaration

- **Batch N game scale** (proposed, identical in all 37 TH specs; chains derived against it are in [TH-17](TH-17-kettle.md#batch-n-chains); owner question in section 6). Ambient reservoir 288 K and 101.3 kPa. [Research table](../../thermal-component-research.md) rows: specific heat, latent heat and source power read as listed range × scale (64–4096 J/(kg·K), 8192–2,097,152 J/kg, 0.5–2048 W); temperature (128–2048 K), conductance (2⁻¹⁰–2⁸ W/K), fuel/supply (finite), ignition threshold (400–1200 K) and sensor thresholds read as listed, without scale; expansion coefficient reads 2⁻²⁰–2⁻¹⁴ 1/K after scale, which TH-22 and TH-23 exceed on purpose as a declared exaggeration. Condensed-phase specific heat SI × ¼ and latent heat SI × 1/16, so catalogue-scale bodies change temperature or phase within one Run. Gas R and Cv SI × 1, so γ and pressure stay physical; each condensed phase's enthalpy is referenced so vapour made at T_sat carries m·Cv·T_sat and the scaled latent heat is exactly the heat consumed; because water's scaled latent heat (141 kJ/kg) is below R·T_sat (172 kJ/kg), boiling follows a declared saturation curve, not Clausius–Clapeyron. Every fuel's reaction energy and O₂ demand per kilogram SI × 1/1024 (wax 41,016 J/kg and 3.4/1024 kg O₂/kg; wood and cellulose 15,625 J/kg and 1.2/1024 kg O₂/kg), so a 0.3 kg candle holds about 12 kJ, heat per kilogram of O₂ keeps Thornton's ≈ 13 MJ/kg, and fuel mass is consumed 1:1 so burn-down stays visible. Still-air surface exchange h = 2.5 W/(m²·K) (SI × ¼, so condensed-body time constants C/(hA) stay SI-like while gas-node ones run about 4× slower than SI); forced air h = 2.5 + u (u in m/s). Contact pairs conduct G = G_a·G_b/(G_a + G_b) from each part's face conductance: metal 64, ceramic, wood, ice or soft 16, probe tip 0.5 W/K. Every exchange is clamped per 480 Hz substep to the equalising amount ΔT·C_a·C_b/(C_a + C_b), so small nodes never overshoot. Reactive surfaces carry a thermal-only patch node outside rigid mass (fibre 10 mg, wood 0.5 g; area 0.0025 m²; patch-to-bulk 0.01 W/K; thermal nodes 10⁻⁶–1024 kg, not bound by the 1/1024 kg rigid minimum) that takes strikes, flames and light first. Radiant optical power: 1 legacy intensity unit = 1 W, so the flashlight emits 24 W. Canonical f32 ([numeric contract](../../gpu-f32-physics.md#game-grade-envelope)).
- **Variants.** None of its own. EL-055 specifies two modes (shaft and carrier); the thermal extension applies to both with the same law. Shaft mode is the TX-10 case and is specified here; carrier mode uses the same partition with force × slip speed.
- **Bodies and shapes (adopted from EL-055).** Static housing box 0.7 × 0.7 × 0.4 m; dynamic pass-through drum, radius 0.2 m, width 0.12 m, 0.25 kg, from the legacy winding shaft (`parts/WoundSpringPart.cs@a6c914e:L71-L71`). Carrier mode: static clamp box 0.6 × 0.3 × 0.4 m. EL-055 states no extent convention for these boxes; this spec reads them as full sizes (housing half-extents 0.35 × 0.35 × 0.20 m, clamp 0.30 × 0.15 × 0.20 m), matching the legacy `AddBox`, which takes full sizes and stores halves (`reference/cpu/MachinePart.cs@a6c914e:L352-L356`).
- **Mass and material.** Drum thermal node = the drum's 0.25 kg, steel c = 122 J/(kg·K) (batch scale; 30.5 J/K). Shoe thermal node 0.1 kg brake composite, c = 210 J/(kg·K) (proposed: ceramic-composite lining 840 × ¼; 21 J/K). Drum ambient exchange 2.5 × 0.40 m² = 1.0 W/K; shoe housing face 16 W/K (batch scale: composite face) for any body touching the housing.
- **Constraints.** EL-055's brake row on the shaft hinge: |torque| ≤ capacity while engaged, absent while released; it only removes relative motion.
- **Typed ports.** EL-055's `DriveIn`, `Drive` and `ActivationIn`. No new port: heat leaves by contact and convection.
- **Sensors and activation.** None added; EL-055's `ActivationIn` flips the brake state.
- **Work and energy stores.** Dissipated power is EL-055's capacity-limited row work: P = |τ_row| × |ω_slip| with |τ_row| ≤ torque capacity (default 30 N·m, range 1–100). The thermal extension routes P into heat: 50 % to the drum node, 50 % to the shoe node (proposed: equal split until S544 fixes a partition law). Shaft energy lost = drum + shoe heat + declared ambient loss within the envelope. Example: capacity 10 N·m against the default motor (20 N·m, 6 rad/s, [CAT-042](CAT-042-motor.md)) slips at 6 rad/s and dissipates 60 W; the drum gains about 1 K/s and settles about 30 K above ambient.
- **Parameters.**

  | Parameter | Type | Range | Default | Unit | Source |
  | --- | --- | --- | --- | --- | --- |
  | Torque capacity | f32 | 1–100 | 30 | N·m | EL-055 |
  | Heat partition to drum | f32 | fixed | 0.5 | fraction | (proposed: above) |
  | Shoe thermal mass | f32 | fixed | 0.1 | kg | (proposed: above) |

- **Cosmetic curves and UI bindings.** Pad glow ← committed dissipated power ([research per-element table](../../thermal-component-research.md)); a small shaped thermal indicator ← drum temperature (row). Drum pose follows the committed hinge angle; EL-055's shoe closure follows the committed brake state.
- **Art.** The row asks for "gold rotor, cream shoe and a small shaped thermal indicator"; EL-055 specifies slate shoes `#556573` turning gold `#f7cb52` when engaged on a pale grey drum `#ccd9df`. This conflicts; see Open questions. Shared palette sources: gold `#f7cb52` (`DESIGN.md@a6c914e:L154-L154`), cream `#ead39b` (`DESIGN.md@a6c914e:L146-L146`).
- **Catalogue and inventory entry.** The EL-055 catalogue entry (`mechanical_brake`); no separate inventory item (row: one identity).

## 3. Engine capabilities

Families (map row TH-05, [general-engine-element-map](../general-engine-element-map.md); binding [thermal-01.json](../../coverage/engine/thermal-01.json)): ContactImpulse, EnvironmentState, FiniteLedger, GeometryQuery, RigidBodyDynamics, SensibleHeat, SlidingFriction, ThermalConduction; JSON adds StateTransaction.

- **Exists now.** Rigid bodies, contact and per-contact friction material (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L42-L120`); paid contact work ledger in joules (`engine/gpu/ContactWorkDeclaration.cs@a6c914e:L5-L38`); Run/Reset and save (`engine/gpu/ActivationNetwork.cs@a6c914e:L67-L67`, `engine/gpu/WorkshopSaveCodec.cs@a6c914e:L6-L10`).
- **Missing.** EL-055's prerequisites (hinge Story 10.3, cylinder collider, capacity row, ShaftTorque Story 11.1). Row work debited into heat stores and conduction → [S543](../invest/decisions.md#s543) conduction → S544; SensibleHeat (IX-43) has no S543 row. EnvironmentState → [S635](../invest/decisions.md#s635). Thermal rows unscheduled.
- **Dependencies.** EL-055 Mechanical brake; a supplied shaft (CAT-042 Motor, Story 11.1); a heat receiver for TX-10.

## 4. Sources and legacy

- **Requirement row** [thermal-05](../requirements.md#thermal-05): extend the existing brake's resistive-contact/shaft model; removed work enters heat stores with a visible mechanical cost; no variants.
- **Named entry** [thermal-05](../invest/named-elements.md#thermal-05): owner S574; "a stationary brake makes no frictional heat; shaft energy loss balances thermal gain and declared losses".
- **EL-055 spec** ([EL-055](EL-055-mechanical-brake.md)): drum geometry, capacity parameters, modes and art adopted.
- **Research** per-element table: "contact friction work → heat source"; parameters friction, mass; pad glow ← power.
- **Processes and scenarios.** [IX-02](../requirements.md#interaction-02), [IX-05](../requirements.md#interaction-05), [IX-18](../requirements.md#interaction-18); [TX-10](../requirements.md#thermal-scenario-10).
- **Legacy.** No friction-brake part, law or level. The "brake" hits in the legacy tests are motor and pusher supply-hold braking, a different mechanism: `CuriousContraptions.tests/MotorPredictionTests.cs@a6c914e:L136-L136` ("EmptySupplyCannotStartMotionButCanBrake") and `CuriousContraptions.tests/LinearPusherTests.cs@a6c914e:L153-L153` ("InterruptedSupplyOrConflictBrakesAtExactMidStroke"). Do not carry forward as heat sources: they describe actuator holding, not dissipative contact heat; EL-055 harvests them for the mechanical law. Searched `parts/`, `engine/physics/`, `engine/*.cs`, `engine/bridge/`, `content/puzzles.json`, `tools/Campaign` and `reference/`.

## 5. Acceptance outline

Point of truth: [thermal-05](../requirements.md#thermal-05), the EL-055 row and [ELEMENT acceptance](../requirements.md#accept-element).

- **Chrome recipe.** Through the real palette and sockets: a supplied Motor (CAT-042, default 20 N·m and 6 rad/s) driving the brake shaft, brake engaged with capacity set to 10 N·m, a Thermal storage block (TH-26) touching the housing over the shoe.
- **Positive.** The shaft slips at 6 rad/s under a 10 N·m brake torque; 60 W is dissipated; the indicator rises about 1 K/s; the TH-26 block warms through the 8 W/K composite-on-ceramic pair. Shaft energy lost equals drum + shoe heat + declared ambient loss.
- **Negative or control.** Brake at the default 30 N·m stalls the 20 N·m motor: zero slip, zero frictional heat. Released brake: shaft runs, no heat.
- **Boundaries.** Zero slip gives zero heat; heat never exceeds removed shaft work; a fully stopped shaft stops heating in the same tick.
- **Run/Reset.** Restores drum and shoe temperatures, hinge state and the EL-055 state exactly.
- **Save/Load.** Construction, capacity and the shaft connection round-trip; Run after reload reproduces the outcome.
- **Integrations.** TX-10; conduction onward to any receiver; campaign 39, 40, 81, 147.

## 6. Open questions

1. **Batch N game scale.** Adopt or revise the one scale in section 2 (×¼ specific heat, ×1/16 latent heat, gas ×1, reaction energy and O₂ ×1/1024, h 2.5 W/(m²·K), per-pair series contact conductance from per-part face values, 1 intensity unit = 1 W, and which research-table rows the scale multiplies). It carries two consequences: boiling needs a declared saturation curve (S550) because scaled water latent heat is below R·T_sat, and gas-node time constants run about 4× slower than SI (TH-25). Owner decision.
2. **Art conflict.** The TH-05 row asks for a gold rotor and cream shoe; EL-055 specifies slate shoes on a pale grey drum. One brake identity needs one art. Owner decision.
3. **Heat partition.** Equal split versus an effusivity-based partition. Owner decision (S544).
4. **Indicator.** Which shaped indicator form (dial or witness strip) shows drum temperature. Owner decision.
