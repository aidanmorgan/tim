# TH-32 · Tinder pad — named-identity readiness spec

Story 7.0 named-identity spec ([spec](../../../_bmad-output/implementation-artifacts/spec-7-0-element-implementation-readiness.md)). Citations are `path@a6c914e:Lstart-Lend`. No legacy holds tinder values, so every design value without a cited source is marked "(proposed: …)" with a one-line justification; the owner may revise it.

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name | TH-32 · Tinder pad |
| Type | Thermal · potential element (potential/conditional; source adoption retained) |
| Anchors | [requirements.md#thermal-32](../requirements.md#thermal-32) (sequence-task-466); [named-elements.md#thermal-32](../invest/named-elements.md#thermal-32) |
| Proof owner / research | S594 · [TH-S03 combustion and suppression](../../thermal-component-research.md#th-s03) |
| Related identities | No CAT spec. Lit by [TH-31 Flint striker](TH-31-flint-striker.md), [TH-33 Spring-mounted match](TH-33-spring-mounted-match.md) or focused light through [TH-06 Converging lens](TH-06-converging-lens.md) (TX-03); lights the next fuel such as [TH-01](TH-01-candle.md), [TH-02](TH-02-fire-bowl.md), [TH-03](TH-03-combustible-block.md). |
| Campaign | Intro 58; practice 59; reuse 85, 149 ([thermal allocation](../requirements.md#thermal-campaign-allocation)) |
| Roadmap story | Unscheduled. No epic in `_bmad-output/planning-artifacts/epics.md` schedules a thermal element. |
| Status | Not started. No `WorkshopPartKind` member (`engine/gpu/WorkshopPartKind.cs@a6c914e:L3-L3`); no legacy part. |

## 2. Declaration

- **Batch N game scale** (proposed, identical in all 37 TH specs; chains derived against it are in [TH-17](TH-17-kettle.md#batch-n-chains); owner question in section 6). Ambient reservoir 288 K and 101.3 kPa. [Research table](../../thermal-component-research.md) rows: specific heat, latent heat and source power read as listed range × scale (64–4096 J/(kg·K), 8192–2,097,152 J/kg, 0.5–2048 W); temperature (128–2048 K), conductance (2⁻¹⁰–2⁸ W/K), fuel/supply (finite), ignition threshold (400–1200 K) and sensor thresholds read as listed, without scale; expansion coefficient reads 2⁻²⁰–2⁻¹⁴ 1/K after scale, which TH-22 and TH-23 exceed on purpose as a declared exaggeration. Condensed-phase specific heat SI × ¼ and latent heat SI × 1/16, so catalogue-scale bodies change temperature or phase within one Run. Gas R and Cv SI × 1, so γ and pressure stay physical; each condensed phase's enthalpy is referenced so vapour made at T_sat carries m·Cv·T_sat and the scaled latent heat is exactly the heat consumed; because water's scaled latent heat (141 kJ/kg) is below R·T_sat (172 kJ/kg), boiling follows a declared saturation curve, not Clausius–Clapeyron. Every fuel's reaction energy and O₂ demand per kilogram SI × 1/1024 (wax 41,016 J/kg and 3.4/1024 kg O₂/kg; wood and cellulose 15,625 J/kg and 1.2/1024 kg O₂/kg), so a 0.3 kg candle holds about 12 kJ, heat per kilogram of O₂ keeps Thornton's ≈ 13 MJ/kg, and fuel mass is consumed 1:1 so burn-down stays visible. Still-air surface exchange h = 2.5 W/(m²·K) (SI × ¼, so condensed-body time constants C/(hA) stay SI-like while gas-node ones run about 4× slower than SI); forced air h = 2.5 + u (u in m/s). Contact pairs conduct G = G_a·G_b/(G_a + G_b) from each part's face conductance: metal 64, ceramic, wood, ice or soft 16, probe tip 0.5 W/K. Every exchange is clamped per 480 Hz substep to the equalising amount ΔT·C_a·C_b/(C_a + C_b), so small nodes never overshoot. Reactive surfaces carry a thermal-only patch node outside rigid mass (fibre 10 mg, wood 0.5 g; area 0.0025 m²; patch-to-bulk 0.01 W/K; thermal nodes 10⁻⁶–1024 kg, not bound by the 1/1024 kg rigid minimum) that takes strikes, flames and light first. Radiant optical power: 1 legacy intensity unit = 1 W, so the flashlight emits 24 W. Canonical f32 ([numeric contract](../../gpu-f32-physics.md#game-grade-envelope)).
- **Variants.** None named in the row.
- **Bodies and shapes.** One dynamic body: cream tray box, half-extents 0.18 × 0.02 × 0.18 m, carrying the wafer region with half-extents 0.15 × 0.01 × 0.15 m on top (proposed: a 0.36 m tray fits a spark region or a lens spot).
- **Mass and material.** Tray 0.1 kg (proposed: light, carried by hand or conveyor); rigid mass 0.15 kg with the wafer. Wafer fuel 0.05 kg cellulose, c = 425 J/(kg·K) (cellulose 1700 × ¼, batch scale; 21 J/K). Surface: the batch fibre patch node (10 mg, 0.00425 J/K, 0.0025 m², 0.01 W/K to the wafer). Optical absorptance 0.9 (proposed: dark charred fibres). Contact friction 0.6 (proposed: matte tray).
- **Constraints.** None.
- **Typed ports.** None as sockets; ignition energy arrives by contact, strike deposit, flame or absorbed light, all through the shared interface. No special lens interaction (row).
- **Sensors and activation.** None. Ignition when the patch reaches 450 K with O₂ fraction ≥ 0.15 (proposed: dry tinder lights around 420–470 K, the lowest of the fuels); a 450 K patch needs 0.69 J from 288 K. Extinction below 420 K, at zero fuel or below 0.15 O₂ (proposed: hysteresis, as TH-01).
- **Work and energy stores.** Fuel 0.05 kg × 15,625 J/kg = 781 J (batch scale: cellulose). Reaction power 16 W (proposed: a small, short flame), so it burns about 49 s and consumes fuel at about 1.0 g/s with O₂ at about 1.2 mg/s (derived from the batch ratio). Its flame lights a touching candle wick (0.90 J) within a fraction of a second and a fire-bowl wood patch (60 J) in about 4 s ([batch chains](TH-17-kettle.md#batch-n-chains)). Fuel area shrinks with remaining fuel; reacted material becomes inert ash and cannot be reused (IX-28).
- **Parameters.**

  | Parameter | Type | Range | Default | Unit | Source |
  | --- | --- | --- | --- | --- | --- |
  | Fuel mass | f32 | fixed | 0.05 | kg | (proposed: above) |
  | Reaction energy | f32 | fixed | 15,625 | J/kg | (batch scale: cellulose) |
  | Ignition / extinction | f32 | fixed | 450 / 420 | K | (proposed: above) |
  | Reaction power | f32 | fixed | 16 | W | (proposed: above) |
  | Absorptance | f32 | fixed | 0.9 | fraction | (proposed: above) |

- **Cosmetic curves and UI bindings.** Ember ← committed reaction state ([research per-element table](../../thermal-component-research.md)); the visibly shrinking fuel area follows remaining fuel (row).
- **Art.** "Warm-wood patterned wafer on a cream tray with a visibly shrinking fuel area" (row). Nearest [DESIGN.md](../../../DESIGN.md) source colours (proposed mapping): warm wood `#b77c42` (`DESIGN.md@a6c914e:L146-L146`), cream `#ead39b` (`DESIGN.md@a6c914e:L146-L146`).
- **Catalogue and inventory entry.** New `WorkshopPartKind.TinderPad`; id `tinder_pad`, title "Tinder pad", category Heat (proposed: no current category covers thermal parts).

## 3. Engine capabilities

Families (map row TH-32, [general-engine-element-map](../general-engine-element-map.md); binding [thermal-01.json](../../coverage/engine/thermal-01.json)): ChemicalReaction, Extinction, FiniteLedger, GeometryQuery, Ignition, SensibleHeat, TopologyTransaction; JSON adds StateTransaction.

- **Exists now.** Dynamic box body (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L120`); Run/Reset and save (`engine/gpu/ActivationNetwork.cs@a6c914e:L67-L67`, `engine/gpu/WorkshopSaveCodec.cs@a6c914e:L6-L10`).
- **Missing.** [S543](../invest/decisions.md#s543): reaction → S554, ignition → S555, extinction → S556, phase-topology for shrinking fuel → S565. Light absorption into heat: [S484](../invest/decisions.md#s484) absorption → S489. Unscheduled.
- **Dependencies.** An ignition source (TH-31, TH-33, TH-06 with a light source); a next fuel to light.

## 4. Sources and legacy

- **Requirement row** [thermal-32](../requirements.md#thermal-32): finite porous combustible configures surface area, thermal mass and reaction properties; no special lens interaction; no variants.
- **Named entry** [thermal-32](../invest/named-elements.md#thermal-32): owner S594; "defocused/insufficient heating fails; reacted material cannot be reused as fresh fuel".
- **Research** per-element table: "low-threshold combustible"; ignition T; "catches from a striker or lens, lights the next fuel"; ember ← state.
- **Processes and scenarios.** [IX-28](../requirements.md#interaction-28), [IX-29](../requirements.md#interaction-29), [IX-30](../requirements.md#interaction-30), [IX-14](../requirements.md#interaction-14); [TX-03](../requirements.md#thermal-scenario-03).
- **Legacy.** None found. Searched `parts/`, `engine/physics/`, `engine/*.cs`, `engine/bridge/`, `CuriousContraptions.tests/`, `content/puzzles.json`, `tools/Campaign` and `reference/` at a6c914e for tinder, kindling, ember and ignition terms. Consulted, no element knowledge: `reference/P0-022-before/docs/coverage/engine/task-002.json@a6c914e:L2783-L2787` (historical coverage-scope rows).

## 5. Acceptance outline

Point of truth: [thermal-32](../requirements.md#thermal-32), [TX-03](../requirements.md#thermal-scenario-03) and [ELEMENT acceptance](../requirements.md#accept-element). Numbers follow the [batch chains](TH-17-kettle.md#batch-n-chains).

- **Chrome recipe.** Through the real palette and gizmo: an activated Flashlight (CAT-029, 24 W), a Converging lens (TH-06) 1.5 m from it focusing at 3.0 m on a Tinder pad, the pad touching a Candle (TH-01) wick.
- **Positive.** The 0.13 m spot (about 1,700 W/m²) puts about 3.9 W into the patch, which passes 450 K in under a second; the pad burns for about 49 s, shrinks and lights the candle; fuel debit = released heat.
- **Negative or control.** Lens moved 1.0 m off focus: the patch receives about 0.9 W and peaks near 378 K, no ignition. Without the lens the patch receives about 0.05 % of the beam and stays near ambient. A spent pad re-heated does not ignite again. A Thermal storage block (TH-26) at the same focus warms but does not burn.
- **Boundaries.** 449 K does not ignite; zero fuel cannot burn; ash mass remains.
- **Run/Reset.** Restores full fuel, 288 K, unlit state and pose exactly.
- **Save/Load.** Pose round-trips; Run after reload reproduces the outcome.
- **Integrations.** TX-03; TH-31 and TH-33 ignition; TH-02 bed lighting; campaign 58, 59, 85, 149.

## 6. Open questions

1. **Batch N game scale.** Adopt or revise the one scale in section 2 (×¼ specific heat, ×1/16 latent heat, gas ×1, reaction energy and O₂ ×1/1024, h 2.5 W/(m²·K), per-pair series contact conductance from per-part face values, 1 intensity unit = 1 W, and which research-table rows the scale multiplies). It carries two consequences: boiling needs a declared saturation curve (S550) because scaled water latent heat is below R·T_sat, and gas-node time constants run about 4× slower than SI (TH-25). Owner decision.
2. **Burn law.** Fixed reaction power versus area- and oxygen-dependent rate. Owner decision (S554).
3. **Oxygen model.** Threshold fraction versus finite gas transport. Owner decision (S555).
