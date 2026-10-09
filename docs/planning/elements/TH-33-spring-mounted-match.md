# TH-33 · Spring-mounted match — named-identity readiness spec

Story 7.0 named-identity spec ([spec](../../../_bmad-output/implementation-artifacts/spec-7-0-element-implementation-readiness.md)). Citations are `path@a6c914e:Lstart-Lend`. No legacy holds match values, so every design value without a cited source is marked "(proposed: …)" with a one-line justification; the owner may revise it.

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name | TH-33 · Spring-mounted match |
| Type | Thermal · potential element (potential/conditional; source adoption retained) |
| Anchors | [requirements.md#thermal-33](../requirements.md#thermal-33) (sequence-task-467); [named-elements.md#thermal-33](../invest/named-elements.md#thermal-33) |
| Proof owner / research | S595 · [TH-S03 combustion and suppression](../../thermal-component-research.md#th-s03) |
| Related identities | No CAT spec. Spring and release follow [CAT-071 Wound spring](CAT-071-wound_spring.md) and the Story 6.4–6.5 preload store; striking law shared with [TH-31 Flint striker](TH-31-flint-striker.md); lights [TH-32](TH-32-tinder-pad.md), [TH-01](TH-01-candle.md). |
| Campaign | Intro 85; practice 86; reuse 99, 149 ([thermal allocation](../requirements.md#thermal-campaign-allocation)) |
| Roadmap story | Unscheduled. No epic in `_bmad-output/planning-artifacts/epics.md` schedules a thermal element. |
| Status | Not started. No `WorkshopPartKind` member (`engine/gpu/WorkshopPartKind.cs@a6c914e:L3-L3`); no legacy part. |

## 2. Declaration

- **Batch N game scale** (proposed, identical in all 37 TH specs; chains derived against it are in [TH-17](TH-17-kettle.md#batch-n-chains); owner question in section 6). Ambient reservoir 288 K and 101.3 kPa. [Research table](../../thermal-component-research.md) rows: specific heat, latent heat and source power read as listed range × scale (64–4096 J/(kg·K), 8192–2,097,152 J/kg, 0.5–2048 W); temperature (128–2048 K), conductance (2⁻¹⁰–2⁸ W/K), fuel/supply (finite), ignition threshold (400–1200 K) and sensor thresholds read as listed, without scale; expansion coefficient reads 2⁻²⁰–2⁻¹⁴ 1/K after scale, which TH-22 and TH-23 exceed on purpose as a declared exaggeration. Condensed-phase specific heat SI × ¼ and latent heat SI × 1/16, so catalogue-scale bodies change temperature or phase within one Run. Gas R and Cv SI × 1, so γ and pressure stay physical; each condensed phase's enthalpy is referenced so vapour made at T_sat carries m·Cv·T_sat and the scaled latent heat is exactly the heat consumed; because water's scaled latent heat (141 kJ/kg) is below R·T_sat (172 kJ/kg), boiling follows a declared saturation curve, not Clausius–Clapeyron. Every fuel's reaction energy and O₂ demand per kilogram SI × 1/1024 (wax 41,016 J/kg and 3.4/1024 kg O₂/kg; wood and cellulose 15,625 J/kg and 1.2/1024 kg O₂/kg), so a 0.3 kg candle holds about 12 kJ, heat per kilogram of O₂ keeps Thornton's ≈ 13 MJ/kg, and fuel mass is consumed 1:1 so burn-down stays visible. Still-air surface exchange h = 2.5 W/(m²·K) (SI × ¼, so condensed-body time constants C/(hA) stay SI-like while gas-node ones run about 4× slower than SI); forced air h = 2.5 + u (u in m/s). Contact pairs conduct G = G_a·G_b/(G_a + G_b) from each part's face conductance: metal 64, ceramic, wood, ice or soft 16, probe tip 0.5 W/K. Every exchange is clamped per 480 Hz substep to the equalising amount ΔT·C_a·C_b/(C_a + C_b), so small nodes never overshoot. Reactive surfaces carry a thermal-only patch node outside rigid mass (fibre 10 mg, wood 0.5 g; area 0.0025 m²; patch-to-bulk 0.01 W/K; thermal nodes 10⁻⁶–1024 kg, not bound by the 1/1024 kg rigid minimum) that takes strikes, flames and light first. Radiant optical power: 1 legacy intensity unit = 1 W, so the flashlight emits 24 W. Canonical f32 ([numeric contract](../../gpu-f32-physics.md#game-grade-envelope)).
- **Variants.** None named in the row.
- **Bodies and shapes.** Static cream guide: box half-extents 0.30 × 0.05 × 0.05 m with a rough strike pad at its far end, half-extents 0.02 × 0.06 × 0.06 m, inclined 15° to the travel and sprung against the arriving tip with normal load N = 100 N (proposed: the tip wedges and slides to rest along the pad instead of rebounding). Dynamic warm-wood match shaft: box half-extents 0.15 × 0.015 × 0.015 m with the tip at the front (proposed: a 0.3 m match travelling 0.2 m along a 0.6 m guide reads clearly). Release pin: small box, half-extents 0.02 × 0.04 × 0.02 m, that a ball or lever can knock out.
- **Mass and material.** Shaft 0.02 kg wood, c = 425 J/(kg·K) (wood 1700 × ¼, batch scale), above the 1/1024 kg rigid minimum. Tip head 0.5 g, a thermal-only node outside rigid mass, carrying the batch fibre patch (10 mg, 0.00425 J/K). Tip/pad friction μ = 0.8 (proposed: rough strike surface).
- **Constraints.** Slider joint for the shaft along the guide; gold compression spring k = 400 N/m preloaded 0.15 m behind the shaft, storing ½·400·0.15² = 4.5 J (proposed: enough that a single release clears the 1.4 J friction work the tip needs with a 2.9× margin, visible as a quick snap); the release pin holds the 60 N preload until knocked free (shared joint lifecycle).
- **Typed ports.** None as sockets; release is mechanical contact on the pin (proposed: avoids a signal-driven flame).
- **Sensors and activation.** None. The tip ignites when its patch reaches 450 K with O₂ fraction ≥ 0.15 (proposed: strike-anywhere heads light around 400–470 K); 450 K needs 0.69 J.
- **Work and energy stores.** Spring elastic store 4.5 J. The 15° pad takes about 7 % of the arrival energy as normal impact; the remaining kinetic energy, about 4.2 J, is spent against the pad's sliding friction μ·N = 0.8 × 100 N = 80 N, so the shaft stops within about 0.05 m and about 4 J becomes friction work (μ·N·s, bounded by the shaft's energy). Strike deposit = 0.5 × friction work, capped at 4 J (the TH-31 law), so about 2 J (≤ 2.25 J) enters the tip patch, lifting it to about 750 K ([batch chains](TH-17-kettle.md#batch-n-chains)). Tip reaction 0.5 g × 15,625 J/kg = 7.8 J at 8 W (batch scale: wood; about a 1 s flare), then the front half of the shaft burns, 0.01 kg × 15,625 J/kg = 156 J at 8 W (about 20 s, long enough to light tinder in about 0.2 s). Winding the spring alone deposits nothing.
- **Parameters.**

  | Parameter | Type | Range | Default | Unit | Source |
  | --- | --- | --- | --- | --- | --- |
  | Spring stiffness / preload | f32 | fixed | 400 / 0.15 | N/m / m | (proposed: above) |
  | Strike pad normal load | f32 | fixed | 100 | N | (proposed: above) |
  | Tip ignition | f32 | fixed | 450 | K | (proposed: above) |
  | Reaction energy | f32 | fixed | 15,625 | J/kg | (batch scale: wood) |
  | Flame power | f32 | fixed | 8 | W | (proposed: a small match flame) |
  | Pad incline | f32 | fixed | 15 | ° | (proposed: above) |

- **Cosmetic curves and UI bindings.** Match pose ← committed slider position ([research per-element table](../../thermal-component-research.md) "match pose ← hinge"); visible spring compression ← committed slider; tip flame ← committed reaction power.
- **Art.** "Cream guide, gold visible spring and warm-wood shaft with a small original tip mark" (row). Nearest [DESIGN.md](../../../DESIGN.md) source colours (proposed mapping): cream `#ead39b` (`DESIGN.md@a6c914e:L146-L146`), gold `#f7cb52` (`DESIGN.md@a6c914e:L154-L154`), warm wood `#b77c42` (`DESIGN.md@a6c914e:L146-L146`).
- **Catalogue and inventory entry.** New `WorkshopPartKind.SpringMatch`; id `spring_match`, title "Spring-mounted match", category Heat (proposed: no current category covers thermal parts).

## 3. Engine capabilities

Families (map row TH-33, [general-engine-element-map](../general-engine-element-map.md); binding [thermal-01.json](../../coverage/engine/thermal-01.json)): ChemicalReaction, ElasticStorage, EnvironmentState, FiniteLedger, GeometryQuery, Ignition, JointConstraint, RigidBodyDynamics, SensibleHeat, TopologyTransaction; JSON adds StateTransaction.

- **Exists now.** Rigid bodies, contact, friction (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L42-L120`); Run/Reset and save (`engine/gpu/ActivationNetwork.cs@a6c914e:L67-L67`, `engine/gpu/WorkshopSaveCodec.cs@a6c914e:L6-L10`).
- **Missing.** Slider, spring and preload store: Stories 6.4–6.5. Sprung contact normal load, friction-work deposit and ignition: [S543](../invest/decisions.md#s543) ignition → S555, reaction → S554, phase-topology for the burning shaft → S565. SlidingFriction work debit (IX-02) is needed but absent from the map row. Thermal-only patch nodes (batch scale). Unscheduled.
- **Dependencies.** A release trigger (ball, CAT-034 lever); a fuel to light (TH-32, TH-01).

## 4. Sources and legacy

- **Requirement row** [thermal-33](../requirements.md#thermal-33): finite reactive tip on a generic spring/contact assembly receives ignition energy from modelled striking work; position and stored spring energy stay physical; no variants.
- **Named entry** [thermal-33](../invest/named-elements.md#thermal-33): owner S595; "no contact or spent reactive tip cannot ignite; winding the spring alone supplies no flame".
- **Research** per-element table: "spring + friction strike + ignition"; stiffness; "releases, strikes, lights".
- **Processes.** [IX-02](../requirements.md#interaction-02), [IX-29](../requirements.md#interaction-29), [IX-28](../requirements.md#interaction-28), [IX-03](../requirements.md#interaction-03).
- **Legacy.** None found. Searched `parts/`, `engine/physics/`, `engine/*.cs`, `engine/bridge/`, `CuriousContraptions.tests/`, `content/puzzles.json`, `tools/Campaign` and `reference/` at a6c914e for match, spring strike and ignition terms. Consulted, no element knowledge: `reference/P0-022-before/docs/coverage/engine/task-002.json@a6c914e:L2783-L2787` (historical coverage-scope rows).

## 5. Acceptance outline

Point of truth: [thermal-33](../requirements.md#thermal-33) and [ELEMENT acceptance](../requirements.md#accept-element). Numbers follow the [batch chains](TH-17-kettle.md#batch-n-chains).

- **Chrome recipe.** Through the real palette and gizmo: a Spring-mounted match with a Basketball on a Ramp aimed at its release pin; a Tinder pad (TH-32) beside the strike pad.
- **Positive.** The ball knocks the pin, the spring drives the match along the guide, the tip slides along the sprung pad and stops within about 0.05 m, its patch reaches about 750 K and ignites, and its flame lights the tinder; spring energy = kinetic + impact + friction work, and the deposit is half the friction work.
- **Negative or control.** No release (ball misses): the spring stays wound and nothing lights. Strike pad removed (no contact): the match slides off the guide end without igniting. A spent tip pushed back against the pad: no new flame.
- **Boundaries.** Deposit ≤ 0.5 × friction work and ≤ 4 J; friction work ≤ μ·N·s and ≤ the shaft's energy; tip at 449 K does not ignite; spent tip cannot reignite.
- **Run/Reset.** Restores wound spring, pin, unburnt tip and shaft pose exactly.
- **Save/Load.** Pose round-trips; Run after reload reproduces the outcome.
- **Integrations.** Lesson 85 bounded ignition; campaign 85, 86, 99, 149.

## 6. Open questions

1. **Batch N game scale.** Adopt or revise the one scale in section 2 (×¼ specific heat, ×1/16 latent heat, gas ×1, reaction energy and O₂ ×1/1024, h 2.5 W/(m²·K), per-pair series contact conductance from per-part face values, 1 intensity unit = 1 W, and which research-table rows the scale multiplies). It carries two consequences: boiling needs a declared saturation curve (S550) because scaled water latent heat is below R·T_sat, and gas-node time constants run about 4× slower than SI (TH-25). Owner decision.
2. **Release method.** Mechanical pin only, or also an activation-latch release. Owner decision.
3. **Map row.** Add SlidingFriction for the strike work. Owner decision.
4. **Re-arming.** Can the player rewind the spring during a Run? Owner decision.
