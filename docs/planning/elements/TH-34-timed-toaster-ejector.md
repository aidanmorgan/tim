# TH-34 · Timed toaster ejector — named-identity readiness spec

Story 7.0 named-identity spec ([spec](../../../_bmad-output/implementation-artifacts/spec-7-0-element-implementation-readiness.md)). Citations are `path@a6c914e:Lstart-Lend`. No legacy holds toaster values, so every design value without a cited source is marked "(proposed: …)" with a one-line justification; the owner may revise it.

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name | TH-34 · Timed toaster ejector |
| Type | Thermal · potential element (potential/conditional; source adoption retained) |
| Anchors | [requirements.md#thermal-34](../requirements.md#thermal-34) (sequence-task-468); [named-elements.md#thermal-34](../invest/named-elements.md#thermal-34) |
| Proof owner / research | S599 · [TH-S01 heat transfer and finite stores](../../thermal-component-research.md#th-s01) |
| Related identities | Historical record [todo-214](../requirements.md#todo-214) is shared with [EL-075 Toy pulse emitter](../invest/named-elements.md#element-075) (Batch L). Composes the heater law of [TH-04](TH-04-electrical-heating-plate.md), the timer of [CAT-022 Delay](CAT-022-delay.md) / [CAT-033 Hold timer](CAT-033-hold_timer.md), a latch like [CAT-037](CAT-037-latch.md) and a spring like [CAT-071](CAT-071-wound_spring.md). Temperature proof by [TH-21](TH-21-temperature-sensor.md) or [TH-37](TH-37-heat-sensitive-target.md). |
| Campaign | Intro 91; practice 92; reuse 99, 146 ([thermal allocation](../requirements.md#thermal-campaign-allocation)) |
| Roadmap story | Unscheduled. No epic in `_bmad-output/planning-artifacts/epics.md` schedules a thermal element. |
| Status | Not started. No `WorkshopPartKind` member (`engine/gpu/WorkshopPartKind.cs@a6c914e:L3-L3`); no legacy part. |

## 2. Declaration

- **Batch N game scale** (proposed, identical in all 37 TH specs; chains derived against it are in [TH-17](TH-17-kettle.md#batch-n-chains); owner question in section 6). Ambient reservoir 288 K and 101.3 kPa. [Research table](../../thermal-component-research.md) rows: specific heat, latent heat and source power read as listed range × scale (64–4096 J/(kg·K), 8192–2,097,152 J/kg, 0.5–2048 W); temperature (128–2048 K), conductance (2⁻¹⁰–2⁸ W/K), fuel/supply (finite), ignition threshold (400–1200 K) and sensor thresholds read as listed, without scale; expansion coefficient reads 2⁻²⁰–2⁻¹⁴ 1/K after scale, which TH-22 and TH-23 exceed on purpose as a declared exaggeration. Condensed-phase specific heat SI × ¼ and latent heat SI × 1/16, so catalogue-scale bodies change temperature or phase within one Run. Gas R and Cv SI × 1, so γ and pressure stay physical; each condensed phase's enthalpy is referenced so vapour made at T_sat carries m·Cv·T_sat and the scaled latent heat is exactly the heat consumed; because water's scaled latent heat (141 kJ/kg) is below R·T_sat (172 kJ/kg), boiling follows a declared saturation curve, not Clausius–Clapeyron. Every fuel's reaction energy and O₂ demand per kilogram SI × 1/1024 (wax 41,016 J/kg and 3.4/1024 kg O₂/kg; wood and cellulose 15,625 J/kg and 1.2/1024 kg O₂/kg), so a 0.3 kg candle holds about 12 kJ, heat per kilogram of O₂ keeps Thornton's ≈ 13 MJ/kg, and fuel mass is consumed 1:1 so burn-down stays visible. Still-air surface exchange h = 2.5 W/(m²·K) (SI × ¼, so condensed-body time constants C/(hA) stay SI-like while gas-node ones run about 4× slower than SI); forced air h = 2.5 + u (u in m/s). Contact pairs conduct G = G_a·G_b/(G_a + G_b) from each part's face conductance: metal 64, ceramic, wood, ice or soft 16, probe tip 0.5 W/K. Every exchange is clamped per 480 Hz substep to the equalising amount ΔT·C_a·C_b/(C_a + C_b), so small nodes never overshoot. Reactive surfaces carry a thermal-only patch node outside rigid mass (fibre 10 mg, wood 0.5 g; area 0.0025 m²; patch-to-bulk 0.01 W/K; thermal nodes 10⁻⁶–1024 kg, not bound by the 1/1024 kg rigid minimum) that takes strikes, flames and light first. Radiant optical power: 1 legacy intensity unit = 1 W, so the flashlight emits 24 W. Canonical f32 ([numeric contract](../../gpu-f32-physics.md#game-grade-envelope)).
- **Variants.** None named in the row.
- **Bodies and shapes.** Static cream housing: box half-extents 0.30 × 0.25 × 0.20 m with an open top slot (proposed: a rounded 0.6 m housing). Dynamic gold carriage tray on a vertical slider: box half-extents 0.25 × 0.02 × 0.08 m, travel 0.25 m. Cargo: one dynamic slice, box half-extents 0.12 × 0.10 × 0.02 m, resting on the carriage (proposed: a slice-shaped thermal cargo the carriage throws).
- **Mass and material.** Carriage 0.1 kg (proposed: light tray). Cargo 0.1 kg, c = 700 J/(kg·K) (bread 2800 × ¼, batch scale; 70 J/K); soft face 16 W/K; ambient 2.5 × 0.13 m² ≈ 0.33 W/K. Heater element node inside the housing: 0.05 kg steel, c = 122 J/(kg·K) (batch scale; 6.1 J/K). Element-to-slice slot coupling 8 W/K (proposed: two close radiant faces on both sides of the slice, declared as a radiant coupling rather than a contact pair).
- **Constraints.** Carriage slider with end stops at 0 and 0.25 m; ejection spring k = 300 N/m, preload 0.10 m (1.5 J) (proposed: lifts the slice clear of the slot); latch holds the carriage down while the timer counts.
- **Typed ports.** Electrical `PowerIn` (`engine/gpu/WorkshopConnections.cs@a6c914e:L8-L11`) for the heater; `ActivationIn` starts a cycle (proposed: a falling ball's switch can start it). Each coupling is typed: power never starts the timer, a signal never heats.
- **Sensors and activation.** Timer: default 2400 ticks = 20 s at 120 Hz (proposed: long enough to warm the slice measurably, short for a lesson), using the existing timer declaration (`engine/gpu/ActivationTimers.cs@a6c914e:L66-L74`). On expiry the latch releases and the spring ejects the carriage. Heating only while the timer counts and supply exists.
- **Work and energy stores.** Heater rating 256 W (proposed: half the TH-04 plate). Two-node balance over 20 s (derived): of the 5.1 kJ delivered, the element keeps about 0.56 kJ (it ends near 380 K, about 30 K above the slice; time constant 0.76 s), the slice loses about 0.2 kJ to the room through 0.33 W/K, and about 4.3 kJ stays in the slice, which ends near 350 K. Over a 5 s cycle the slice reaches only about 302 K. Spring store 1.5 J: after lifting 0.2 kg through the 0.25 m travel about 1.0 J remains, so the slice leaves at about 3 m/s and rises about 0.5 m above the slot (derived). Battery debit = heat delivered.
- **Parameters.**

  | Parameter | Type | Range | Default | Unit | Source |
  | --- | --- | --- | --- | --- | --- |
  | Timer duration | u32 | 120–3600 | 2400 | ticks | (proposed: 1–30 s dial; matches timer parts) |
  | Heater rating | f32 | fixed | 256 | W | (proposed: above) |
  | Slot coupling | f32 | fixed | 8 | W/K | (proposed: above) |
  | Spring stiffness / preload | f32 | fixed | 300 / 0.10 | N/m / m | (proposed: above) |
  | Cargo mass / c | f32 | fixed | 0.1 / 700 | kg / J/(kg·K) | (proposed mass; batch-scale c) |

- **Cosmetic curves and UI bindings.** Lever ← committed latch state ([research per-element table](../../thermal-component-research.md)); the cyan timed-state window shows the timer phase (row); the gold tray shows heater power. A small dial sets the duration.
- **Art.** "Rounded cream housing, gold warming tray and cyan timed-state window" (row). Nearest [DESIGN.md](../../../DESIGN.md) source colours (proposed mapping): cream `#ead39b` (`DESIGN.md@a6c914e:L146-L146`), gold `#f7cb52` (`DESIGN.md@a6c914e:L154-L154`), cyan `#66b8c9` (`DESIGN.md@a6c914e:L172-L172`).
- **Catalogue and inventory entry.** New `WorkshopPartKind.ToasterEjector`; id `toaster_ejector`, title "Timed toaster ejector", category Heat (proposed: no current category covers thermal parts).

## 3. Engine capabilities

Families (map row TH-34, [general-engine-element-map](../general-engine-element-map.md); binding [thermal-01.json](../../coverage/engine/thermal-01.json)): ElasticStorage, ElectricalDissipation, ElectricalPower, EnvironmentState, FiniteLedger, FiniteWorkActuation, JointConstraint, RigidBodyDynamics, SensibleHeat, SignalPropagation, TemperatureSensing; JSON adds StateTransaction. Composition note: add TimedCommand/latch ordering and a contact-obstructed spring carriage; elapsed time is not measured cargo temperature.

- **Exists now.** Activation network and timers (TimedCommand) (`engine/gpu/ActivationNetwork.cs@a6c914e:L7-L14`, `engine/gpu/ActivationTimers.cs@a6c914e:L66-L74`); typed sockets (`engine/gpu/WorkshopConnections.cs@a6c914e:L8-L11`); rigid bodies (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L120`); Run/Reset and save (`engine/gpu/ActivationNetwork.cs@a6c914e:L67-L67`, `engine/gpu/WorkshopSaveCodec.cs@a6c914e:L6-L10`).
- **Missing.** ElectricalPower: Story 8.1. Slider and spring preload: Stories 6.4–6.5. Latch: Story 9.4. [S543](../invest/decisions.md#s543): electrical-heat → S564, conduction → S544, thermal-radiation (the slot coupling) → S546. TimedCommand is not in the map family list despite the note. Unscheduled.
- **Dependencies.** Battery (CAT-005), a start signal (CAT-063 Switch), and a temperature proof (TH-21 or TH-37).

## 4. Sources and legacy

- **Requirement row** [thermal-34](../requirements.md#thermal-34): supplied heater, finite thermal cargo and generic timer/latch/spring perform warming and delayed ejection; each coupling typed and energy-accounted; no variants.
- **Named entry** [thermal-34](../invest/named-elements.md#thermal-34): owner S599; "no supply cannot warm; an obstructed carriage cannot teleport the payload; time alone is not proof of temperature".
- **Research** per-element table: "heating plate + timer + spring eject"; watt rating, time; "warms cargo then pops it out"; lever ← state. Historical reference: the [todo-214](../requirements.md#todo-214) Sierra article link.
- **Processes.** [IX-39](../requirements.md#interaction-39), [IX-18](../requirements.md#interaction-18), [IX-07](../requirements.md#interaction-07), [IX-17](../requirements.md#interaction-17).
- **Legacy.** None found. Searched `parts/`, `engine/physics/`, `engine/*.cs`, `engine/bridge/`, `CuriousContraptions.tests/`, `content/puzzles.json`, `tools/Campaign` and `reference/` at a6c914e for toaster, eject and toast terms. Consulted, no element knowledge: `reference/P0-022-before/docs/coverage/engine/task-002.json@a6c914e:L2783-L2787` (historical coverage-scope rows).

## 5. Acceptance outline

Point of truth: [thermal-34](../requirements.md#thermal-34) and [ELEMENT acceptance](../requirements.md#accept-element).

- **Chrome recipe.** Through the real palette and sockets: Battery → toaster `PowerIn`; an Impact switch (CAT-063) hit by a rolling ball → toaster `ActivationIn`; a Ramp beside the slot that guides the falling slice onto a Heat-sensitive target (TH-37) laid flat, threshold 315 K.
- **Positive.** The switch starts the 20 s cycle; the slice warms to about 350 K; the latch releases; the spring throws the slice, which lands on the target. Through the 8 W/K soft–ceramic pair the two settle near 327 K within about 10 s, and the target holds above 315 K for its 1 s dwell, so the goal solves.
- **Negative or control.** No battery: the timer runs and ejects a 288 K slice; the target stays at 288 K and false (time is not temperature). A 5 s cycle: the slice leaves at about 302 K, the target settles near 297 K and stays false. A Wall over the slot: the carriage stops at the obstruction and the slice stays inside.
- **Boundaries.** Heat delivered = battery debit; ejection never exceeds spring energy; durations outside 120–3600 ticks are refused by the dial range.
- **Run/Reset.** Restores latched carriage, 288 K slice and element, timer and spring exactly.
- **Save/Load.** Pose, duration and connections round-trip; Run after reload reproduces the outcome.
- **Integrations.** Lesson 91 timed ejector; campaign 91, 92, 99, 146.

## 6. Open questions

1. **Batch N game scale.** Adopt or revise the one scale in section 2 (×¼ specific heat, ×1/16 latent heat, gas ×1, reaction energy and O₂ ×1/1024, h 2.5 W/(m²·K), per-pair series contact conductance from per-part face values, 1 intensity unit = 1 W, and which research-table rows the scale multiplies). It carries two consequences: boiling needs a declared saturation curve (S550) because scaled water latent heat is below R·T_sat, and gas-node time constants run about 4× slower than SI (TH-25). Owner decision.
2. **Start trigger.** `ActivationIn` start versus a mechanical push-down lever. Owner decision.
3. **Cargo.** One built-in slice or any placed thermal cargo. Owner decision.
4. **Map row.** Add TimedCommand and the radiant slot coupling to the family list per the composition note. Owner decision.
