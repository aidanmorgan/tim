# TH-36 · Lamp-trigger apparatus — named-identity readiness spec

Story 7.0 named-identity spec ([spec](../../../_bmad-output/implementation-artifacts/spec-7-0-element-implementation-readiness.md)). Citations are `path@a6c914e:Lstart-Lend`. No legacy holds this apparatus; the delivered Signal lamp art supplies scale (section 4). Every design value without a cited source is marked "(proposed: …)" with a one-line justification; the owner may revise it.

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name | TH-36 · Lamp-trigger apparatus |
| Type | Thermal · potential element (potential/conditional; source adoption retained) |
| Anchors | [requirements.md#thermal-36](../requirements.md#thermal-36) (sequence-task-470); [named-elements.md#thermal-36](../invest/named-elements.md#thermal-36) |
| Proof owner / research | S601 · [TH-S04 optical heating](../../thermal-component-research.md#th-s04) |
| Related identities | **Refines the historical lamp** whose trigger must be resolved first (decision S601-D). Distinct from [CAT-035 Signal lamp](CAT-035-lamp.md) (Batch A; activation-latched, not a light/heat source) and [EL-155 Rope-operated light](EL-155-rope-operated-light.md) (Batch K; rope displacement actuating a supplied light), whose actuator pattern and emitter are the closest sibling. Optical neighbour [EL-210 Flashlight](EL-210-flashlight.md). Receivers [TH-07](TH-07-solar-absorber-plate.md), [TH-37](TH-37-heat-sensitive-target.md). |
| Campaign | Intro 86; practice 87; reuse 100, 142 ([thermal allocation](../requirements.md#thermal-campaign-allocation)) |
| Roadmap story | Unscheduled, and blocked on S601-D. |
| Status | Not started. No `WorkshopPartKind` member (`engine/gpu/WorkshopPartKind.cs@a6c914e:L3-L3`); no legacy part. |

## 2. Declaration

- **Batch N game scale** (proposed, identical in all 37 TH specs; chains derived against it are in [TH-17](TH-17-kettle.md#batch-n-chains); owner question in section 6). Ambient reservoir 288 K and 101.3 kPa. [Research table](../../thermal-component-research.md) rows: specific heat, latent heat and source power read as listed range × scale (64–4096 J/(kg·K), 8192–2,097,152 J/kg, 0.5–2048 W); temperature (128–2048 K), conductance (2⁻¹⁰–2⁸ W/K), fuel/supply (finite), ignition threshold (400–1200 K) and sensor thresholds read as listed, without scale; expansion coefficient reads 2⁻²⁰–2⁻¹⁴ 1/K after scale, which TH-22 and TH-23 exceed on purpose as a declared exaggeration. Condensed-phase specific heat SI × ¼ and latent heat SI × 1/16, so catalogue-scale bodies change temperature or phase within one Run. Gas R and Cv SI × 1, so γ and pressure stay physical; each condensed phase's enthalpy is referenced so vapour made at T_sat carries m·Cv·T_sat and the scaled latent heat is exactly the heat consumed; because water's scaled latent heat (141 kJ/kg) is below R·T_sat (172 kJ/kg), boiling follows a declared saturation curve, not Clausius–Clapeyron. Every fuel's reaction energy and O₂ demand per kilogram SI × 1/1024 (wax 41,016 J/kg and 3.4/1024 kg O₂/kg; wood and cellulose 15,625 J/kg and 1.2/1024 kg O₂/kg), so a 0.3 kg candle holds about 12 kJ, heat per kilogram of O₂ keeps Thornton's ≈ 13 MJ/kg, and fuel mass is consumed 1:1 so burn-down stays visible. Still-air surface exchange h = 2.5 W/(m²·K) (SI × ¼, so condensed-body time constants C/(hA) stay SI-like while gas-node ones run about 4× slower than SI); forced air h = 2.5 + u (u in m/s). Contact pairs conduct G = G_a·G_b/(G_a + G_b) from each part's face conductance: metal 64, ceramic, wood, ice or soft 16, probe tip 0.5 W/K. Every exchange is clamped per 480 Hz substep to the equalising amount ΔT·C_a·C_b/(C_a + C_b), so small nodes never overshoot. Reactive surfaces carry a thermal-only patch node outside rigid mass (fibre 10 mg, wood 0.5 g; area 0.0025 m²; patch-to-bulk 0.01 W/K; thermal nodes 10⁻⁶–1024 kg, not bound by the 1/1024 kg rigid minimum) that takes strikes, flames and light first. Radiant optical power: 1 legacy intensity unit = 1 W, so the flashlight emits 24 W. Canonical f32 ([numeric contract](../../gpu-f32-physics.md#game-grade-envelope)).
- **Variants (trigger candidates, pending S601-D), specified separately.** The row requires one selected original design with "a generic contact or displacement actuator"; both candidates are declared so S601-D can choose:
  - **Contact trigger.** A gold push lever on the lamp body; a qualifying impact (approach ≥ 0.3 m/s, proposed: a rolling ball's tap) closes the lamp's internal supply contact. Uses the existing contact-trigger pattern (`engine/gpu/ContactTriggerDeclaration.cs@a6c914e:L8-L8`).
  - **Displacement trigger.** A gold lever on a hinge that must be displaced ≥ 20° (proposed: a deliberate pull, like EL-155's rope) to close the contact; returns by a 0.2 N·m spring (proposed).
  No historical-name selector chooses either effect.
- **Bodies and shapes.** Static cream lamp body at the delivered Signal lamp's scale (section 4): base box half-extents 0.425 × 0.10 × 0.425 m (legacy full size 0.85 × 0.2 × 0.85 m); column box half-extents 0.18 × 0.15 × 0.18 m around the legacy 0.18 m-radius, 0.3 m stem; luminous aperture sphere radius 0.43 m on top (legacy bulb radius). Lever box half-extents 0.20 × 0.02 × 0.03 m (proposed). The emitter sits at the aperture front, +X.
- **Mass and material.** Static rigid mass zero (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L70-L76`), so its thermal node is declared separately: a lamp-head collar node of 0.1 kg brass, c = 95 J/(kg·K) (brass 380 × ¼, batch scale; 9.5 J/K), metal face 64 W/K, ambient 2.5 × 0.4 m² = 1 W/K (proposed collar area). Lever 0.05 kg (proposed).
- **Constraints.** Lever hinge (both candidates).
- **Typed ports.** Electrical `PowerIn` (`engine/gpu/WorkshopConnections.cs@a6c914e:L8-L11`): the source is finite and supplied. The actuator is mechanical; no activation input selects the effect.
- **Sensors and activation.** None beyond the actuator contact.
- **Work and energy stores.** Supplied lamp rating 40 W while the contact is closed and supply exists (proposed: a small incandescent lamp). Radiant optical emission 24 W through the EL-155 emitter (cone half-angle 15°, range 8; 24 intensity units = 24 W under the batch scale), so every light receiver, solar threshold and the TH-07 chain respond exactly as to the flashlight (proposed: one optical unit across EL-155, EL-210 and TH-36). The remaining 16 W heats the collar node, which settles about 16 K above ambient (about 304 K) with a 9.5 s time constant (derived). No supply or no actuation: no new output.
- **Parameters.**

  | Parameter | Type | Range | Default | Unit | Source |
  | --- | --- | --- | --- | --- | --- |
  | Trigger design | enum `Contact`/`Displacement` | closed set | pending S601-D | — | (proposed: above) |
  | Lamp rating | f32 | fixed | 40 | W | (proposed: above) |
  | Radiant optical power | f32 | fixed | 24 | W | (proposed: EL-155 intensity 24 under the batch scale) |
  | Cone half-angle / range | f32 | fixed | 15 / 8 | ° / m | (proposed: the EL-155 emitter, `parts/FlashlightPart.cs@a6c914e:L10-L13`) |

- **Cosmetic curves and UI bindings.** Aperture glow ← committed delivered optical power; lever pose ← committed hinge angle. The research table's "lamp ← output" binding applies to the glow.
- **Art.** "Cream sculptural lamp, gold lever and restrained luminous aperture with original icon" (row). Nearest [DESIGN.md](../../../DESIGN.md) source colours (proposed mapping): cream `#ead39b` (`DESIGN.md@a6c914e:L146-L146`), gold `#f7cb52` (`DESIGN.md@a6c914e:L154-L154`); neutral `#556573` and lit `#fff0a5` aperture colours from the Signal lamp art (section 4).
- **Catalogue and inventory entry.** New `WorkshopPartKind.TriggerLamp`; id `trigger_lamp`, title "Lamp-trigger apparatus", category Optics (proposed: a light source beside the flashlight).

## 3. Engine capabilities

Families (map row TH-36, [general-engine-element-map](../general-engine-element-map.md); binding [thermal-01.json](../../coverage/engine/thermal-01.json)): FiniteLedger, GeometryQuery, OpticalAbsorption, OpticalTransport, SensibleHeat, SignalPropagation, TemperatureSensing; JSON adds StateTransaction. Composition note: S601-D must resolve the historical trigger first; bind a generic displacement/contact actuator and a finite supplied optical/heat source.

- **Exists now.** Static body, box and sphere colliders (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L120`); contact triggers (`engine/gpu/ContactTriggerDeclaration.cs@a6c914e:L8-L8`); typed `PowerIn` (`engine/gpu/WorkshopConnections.cs@a6c914e:L8-L11`); Run/Reset and save (`engine/gpu/ActivationNetwork.cs@a6c914e:L67-L67`, `engine/gpu/WorkshopSaveCodec.cs@a6c914e:L6-L10`).
- **Missing.** S601-D decision (no row in `decisions.md`). ElectricalPower: Story 8.1. OpticalTransport emitter: Story 13.1 ([S484](../invest/decisions.md#s484)). Hinge: Story 10.3. Electrical heat: [S543](../invest/decisions.md#s543) electrical-heat → S564. The map lists TemperatureSensing, which the row does not need (see Open questions).
- **Dependencies.** A supply (CAT-005); an actuator source (ball, rope); a receiver (TH-07, TH-37, a light receiver).

## 4. Sources and legacy

- **Requirement row** [thermal-36](../requirements.md#thermal-36): resolve the historical lamp's trigger first; represent the selected design as a finite supplied light/heat source plus a generic contact or displacement actuator; no historical-name selector.
- **Named entry** [thermal-36](../invest/named-elements.md#thermal-36): owner S601; "an unactuated or unsupplied source produces no new output; demonstrate trigger displacement and optical/thermal energy independently".
- **Research** per-element table: "heat sensor ↔ lamp activation"; On/Off T; "lights a lamp when warm" — this conflicts with the row (see Open questions).
- **Processes.** [IX-13](../requirements.md#interaction-13), [IX-39](../requirements.md#interaction-39), [IX-01](../requirements.md#interaction-01), [IX-03](../requirements.md#interaction-03).
- **Legacy (scale and emitter reference only).**

  | # | Fact | Citation | Disposition |
  | --- | --- | --- | --- |
  | 1 | Signal lamp art: base box 0.85 × 0.2 × 0.85 m (full size; `PartArt.Box` takes a size), stem cylinder radius 0.18 m and height 0.3 m, bulb sphere radius 0.43 m; neutral `#556573`, lit `#fff0a5`, emission `#e9b24c`. | `parts/LampPart.cs@a6c914e:L11-L24`; `engine/PartArt.cs@a6c914e:L16-L21` | Carry forward as scale and colour reference only; CAT-035 owns it and it carries no trigger or light/heat source. |
  | 2 | Torch emitter: range 8, cone cosine 0.9659258 (15°), intensity 24. | `parts/FlashlightPart.cs@a6c914e:L10-L13` | Carry forward as the emitter, as EL-155 does. |

  Searched `parts/`, `engine/physics/`, `engine/*.cs`, `engine/bridge/`, `CuriousContraptions.tests/`, `content/puzzles.json`, `tools/Campaign` and `reference/` at a6c914e for lamp, trigger, bulb and light terms; `reference/switch-lamp/` is a CAT-035 contact probe with no light/heat source.

## 5. Acceptance outline

Point of truth: [thermal-36](../requirements.md#thermal-36) and [ELEMENT acceptance](../requirements.md#accept-element). Written for either S601-D outcome; optical numbers follow the [batch chains](TH-17-kettle.md#batch-n-chains).

- **Chrome recipe.** Through the real palette and sockets: Battery → lamp `PowerIn`; a Basketball on a Ramp aimed at the lever (Contact) or a rope pull on the lever (Displacement); a Solar absorber plate (TH-07) 1 m away in the cone; a Temperature sensor (TH-21) probe on the collar.
- **Positive.** Actuation closes the contact. Optical: the absorber intercepts 9.6 W and warms at about 0.40 K/s toward 328 K, passing 300 K after about 35 s. Thermal: the collar probe rises toward about 304 K. Trigger displacement, optical output and thermal output are each observed separately.
- **Negative or control.** No actuation: no output. Actuated but unsupplied: no output. Lever displaced below 20° (Displacement) or a slow tap (Contact): no closure. An occluder in the cone: the absorber stays at 288 K while the collar still warms.
- **Boundaries.** Optical + thermal output = supplied power (24 + 16 = 40 W); output stops when supply ends.
- **Run/Reset.** Restores lever pose, open contact, collar 288 K and supply exactly.
- **Save/Load.** Pose, trigger design and connections round-trip; Run after reload reproduces the outcome.
- **Integrations.** Lesson 86–87 vessel-and-lamp chapter; campaign 86, 87, 100, 142.

## 6. Open questions

1. **Batch N game scale.** Adopt or revise the one scale in section 2 (×¼ specific heat, ×1/16 latent heat, gas ×1, reaction energy and O₂ ×1/1024, h 2.5 W/(m²·K), per-pair series contact conductance from per-part face values, 1 intensity unit = 1 W, and which research-table rows the scale multiplies). It carries two consequences: boiling needs a declared saturation curve (S550) because scaled water latent heat is below R·T_sat, and gas-node time constants run about 4× slower than SI (TH-25). Owner decision.
2. **S601-D.** Which original toy trigger the historical lamp used (contact or displacement); S601-D has no row in `decisions.md`. Owner decision.
3. **One optical unit and lamp scale.** This spec adopts EL-155's 24-unit, 15° emitter (24 W) and the Signal lamp's 0.43 m bulb, replacing the earlier 2 W, 30° and 0.25 m proposals. Confirm the shared emitter and scale, or give the lamp its own silhouette and cone. Owner decision (with Batches A and K).
4. **Research conflict.** The research table describes a heat-sensor-driven lamp ("lights a lamp when warm"); the row describes an actuated light/heat source. Owner decision.
5. **Map row.** TemperatureSensing in the family list fits the research reading, not the row. Owner decision.
