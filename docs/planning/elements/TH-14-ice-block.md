# TH-14 · Ice block — named-identity readiness spec

Story 7.0 named-identity spec ([spec](../../../_bmad-output/implementation-artifacts/spec-7-0-element-implementation-readiness.md)). Citations are `path@a6c914e:Lstart-Lend`. No legacy holds ice values, so every design value without a cited source is marked "(proposed: …)" with a one-line justification; the owner may revise it.

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name | TH-14 · Ice block |
| Type | Thermal · potential element (potential/conditional; source adoption retained) |
| Anchors | [requirements.md#thermal-14](../requirements.md#thermal-14) (sequence-task-448); [named-elements.md#thermal-14](../invest/named-elements.md#thermal-14) |
| Proof owner / research | S578 · [TH-S02 phase change](../../thermal-component-research.md#th-s02) |
| Related identities | No CAT spec. Same water material as [TH-13 Freezing mold](TH-13-freezing-mold.md) contents and [TH-15 Ice plug](TH-15-ice-plug.md); the row separates the plug's placement role from this cargo role. Box body like [CAT-023 Domino](CAT-023-domino.md). Meltwater joins the water family (Batches F and G). |
| Campaign | Intro 67; practice 68; reuse 89, 139 ([thermal allocation](../requirements.md#thermal-campaign-allocation)) |
| Roadmap story | Unscheduled. No epic in `_bmad-output/planning-artifacts/epics.md` schedules a thermal element. |
| Status | Not started. No `WorkshopPartKind` member (`engine/gpu/WorkshopPartKind.cs@a6c914e:L3-L3`); no legacy part. |

## 2. Declaration

- **Batch N game scale** (proposed, identical in all 37 TH specs; chains derived against it are in [TH-17](TH-17-kettle.md#batch-n-chains); owner question in section 6). Ambient reservoir 288 K and 101.3 kPa. [Research table](../../thermal-component-research.md) rows: specific heat, latent heat and source power read as listed range × scale (64–4096 J/(kg·K), 8192–2,097,152 J/kg, 0.5–2048 W); temperature (128–2048 K), conductance (2⁻¹⁰–2⁸ W/K), fuel/supply (finite), ignition threshold (400–1200 K) and sensor thresholds read as listed, without scale; expansion coefficient reads 2⁻²⁰–2⁻¹⁴ 1/K after scale, which TH-22 and TH-23 exceed on purpose as a declared exaggeration. Condensed-phase specific heat SI × ¼ and latent heat SI × 1/16, so catalogue-scale bodies change temperature or phase within one Run. Gas R and Cv SI × 1, so γ and pressure stay physical; each condensed phase's enthalpy is referenced so vapour made at T_sat carries m·Cv·T_sat and the scaled latent heat is exactly the heat consumed; because water's scaled latent heat (141 kJ/kg) is below R·T_sat (172 kJ/kg), boiling follows a declared saturation curve, not Clausius–Clapeyron. Every fuel's reaction energy and O₂ demand per kilogram SI × 1/1024 (wax 41,016 J/kg and 3.4/1024 kg O₂/kg; wood and cellulose 15,625 J/kg and 1.2/1024 kg O₂/kg), so a 0.3 kg candle holds about 12 kJ, heat per kilogram of O₂ keeps Thornton's ≈ 13 MJ/kg, and fuel mass is consumed 1:1 so burn-down stays visible. Still-air surface exchange h = 2.5 W/(m²·K) (SI × ¼, so condensed-body time constants C/(hA) stay SI-like while gas-node ones run about 4× slower than SI); forced air h = 2.5 + u (u in m/s). Contact pairs conduct G = G_a·G_b/(G_a + G_b) from each part's face conductance: metal 64, ceramic, wood, ice or soft 16, probe tip 0.5 W/K. Every exchange is clamped per 480 Hz substep to the equalising amount ΔT·C_a·C_b/(C_a + C_b), so small nodes never overshoot. Reactive surfaces carry a thermal-only patch node outside rigid mass (fibre 10 mg, wood 0.5 g; area 0.0025 m²; patch-to-bulk 0.01 W/K; thermal nodes 10⁻⁶–1024 kg, not bound by the 1/1024 kg rigid minimum) that takes strikes, flames and light first. Radiant optical power: 1 legacy intensity unit = 1 W, so the flashlight emits 24 W. Canonical f32 ([numeric contract](../../gpu-f32-physics.md#game-grade-envelope)).
- **Variants.** None named in the row.
- **Bodies and shapes.** One dynamic body, box collider, half-extents 0.20 × 0.20 × 0.20 m at full solid (proposed: a 0.4 m cube can support a Basketball or Domino as cargo). As solid fraction f falls, the half-extents scale by f^⅓ about the box's bottom face, so supported cargo descends (proposed: uniform shrink keeps contact geometry simple and never leaves full-block support on a partial block).
- **Mass and material.** Total water mass 0.25 kg (proposed: smaller than the 0.4 kg Domino, melts in seconds on a plate); rigid mass = solid mass f·0.25 kg, with inertia rederived from the shrunk box. Water under the batch scale: ice c = 525, liquid c = 1046 J/(kg·K), fusion 20,875 J/kg at 273 K. Initial temperature 253 K (proposed: a domestic freezer, cold enough that ambient air alone cannot start melting it within the first 50 s of a Run). Ice face 16 W/K (batch scale), pairing at 12.8 W/K with a metal plate. Ambient gain 2.5 × 0.8 m² = 2 W/K. Contact friction 0.05, restitution 0.1 (proposed: wet ice is very slippery).
- **Constraints.** None.
- **Typed ports.** None. Heat arrives through contacts, radiation or convection.
- **Sensors and activation.** None.
- **Work and energy stores.** One water node: mass, enthalpy, solid fraction f. Warming 253 → 273 K takes 0.25 × 525 × 20 = 2,625 J; melting takes 0.25 × 20,875 = 5,219 J: about 7.8 kJ in all (derived). Melted mass leaves as liquid water at the block's base into the shared water inventory (IX-41), never deleted.
- **Parameters.**

  | Parameter | Type | Range | Default | Unit | Source |
  | --- | --- | --- | --- | --- | --- |
  | Water mass | f32 | fixed | 0.25 | kg | (proposed: above) |
  | Initial temperature | f32 | fixed | 253 | K | (proposed: above) |
  | Contact friction / restitution | f32 | fixed | 0.05 / 0.1 | — | (proposed: above) |

- **Cosmetic curves and UI bindings.** Scale ← committed solid fraction ([research per-element table](../../thermal-component-research.md)); a clear melt edge and bounded droplets follow the same committed f (row).
- **Art.** "Frosted cyan faceted cuboid with a clear melt edge and bounded droplets" (row). Nearest [DESIGN.md](../../../DESIGN.md) source colour (proposed mapping): cyan `#66b8c9` (`DESIGN.md@a6c914e:L172-L172`).
- **Catalogue and inventory entry.** New `WorkshopPartKind.IceBlock`; id `ice_block`, title "Ice block", category Heat (proposed: no current category covers thermal parts).

## 3. Engine capabilities

Families (map row TH-14, [general-engine-element-map](../general-engine-element-map.md); binding [thermal-01.json](../../coverage/engine/thermal-01.json)): ContactImpulse, EnvironmentState, FiniteLedger, GasState, GeometryQuery, PhaseTopology, RigidBodyDynamics, SensibleHeat, SolidMelting, TopologyTransaction; JSON adds StateTransaction.

- **Exists now.** Dynamic box body, contact, friction and derived inertia (Story 5.1; `engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L120`, `engine/gpu/RigidMassProperties.cs@a6c914e:L17-L17`); Run/Reset and save (`engine/gpu/ActivationNetwork.cs@a6c914e:L67-L67`, `engine/gpu/WorkshopSaveCodec.cs@a6c914e:L6-L10`).
- **Missing.** [S543](../invest/decisions.md#s543): melting → S547, phase-topology (shrinking collider, changing mass and inertia mid-Run) → S565, conduction → S544. Meltwater inventory: [S416](../invest/decisions.md#s416) advection → S418. GasState/EnvironmentState: [S470](../invest/decisions.md#s470), [S635](../invest/decisions.md#s635). All unscheduled.
- **Dependencies.** A heat source (TH-04, TH-01); a catch basin from the water family to collect meltwater (TX-07).

## 4. Sources and legacy

- **Requirement row** [thermal-14](../requirements.md#thermal-14): finite mass and enthalpy; phase evolution alters support and contact geometry and releases the same material as liquid; no variants.
- **Named entry** [thermal-14](../invest/named-elements.md#thermal-14): owner S578; "a cold control retains support; partially melted material cannot disappear or keep impossible full-block support".
- **Research** per-element table: "solid-phase node + body"; parameter mass; "melts into accounted water, shrinks"; scale ← solid fraction.
- **Processes and scenarios.** [IX-21](../requirements.md#interaction-21), [IX-41](../requirements.md#interaction-41); [TX-07](../requirements.md#thermal-scenario-07).
- **Legacy.** None found. Searched `parts/`, `engine/physics/`, `engine/*.cs`, `engine/bridge/`, `CuriousContraptions.tests/`, `content/puzzles.json`, `tools/Campaign` and `reference/` at a6c914e for ice, melt, phase and water terms. Consulted, no element knowledge: `reference/P0-022-before/docs/coverage/engine/task-002.json@a6c914e:L2783-L2787` (historical coverage-scope rows).

## 5. Acceptance outline

Point of truth: [thermal-14](../requirements.md#thermal-14), [TX-07](../requirements.md#thermal-scenario-07) and [ELEMENT acceptance](../requirements.md#accept-element).

- **Chrome recipe.** Through the real palette and sockets: an Ice block on a supplied Heating plate (TH-04), a Basketball resting on the block, a Receiver below the plate edge, and a water catch basin.
- **Positive.** Through the 12.8 W/K pair the plate settles near 311 K and delivers about 490 W; the block reaches 273 K after about 5 s and melts over about 11 s more, shrinking so the ball descends and rolls off to the Receiver; collected water mass + remaining ice = 0.25 kg.
- **Negative or control.** Unsupplied plate (cold control): ambient gain of about 2 W/K warms the block toward 273 K but starts no melting within the first 50 s, so full support is retained for the lesson. Heating a disconnected body nearby: no melting.
- **Boundaries.** At f = 0.5 the box is 0.79 of full size and support follows that geometry; f never below 0; mass never created or deleted.
- **Run/Reset.** Restores 0.25 kg solid at 253 K, full collider, zero meltwater and cargo pose exactly.
- **Save/Load.** Pose round-trips; Run after reload reproduces the outcome.
- **Integrations.** TX-07; TH-09 insulation protects it (lesson 67); campaign 67, 68, 89, 139.

## 6. Open questions

1. **Batch N game scale.** Adopt or revise the one scale in section 2 (×¼ specific heat, ×1/16 latent heat, gas ×1, reaction energy and O₂ ×1/1024, h 2.5 W/(m²·K), per-pair series contact conductance from per-part face values, 1 intensity unit = 1 W, and which research-table rows the scale multiplies). It carries two consequences: boiling needs a declared saturation curve (S550) because scaled water latent heat is below R·T_sat, and gas-node time constants run about 4× slower than SI (TH-25). Owner decision.
2. **Shrink model.** Uniform f^⅓ shrink versus melting from the heated face. Owner decision (S547/S565).
3. **Meltwater.** Liquid water representation (particles, volume inventory) belongs to the water family; reconcile with Batches F and G. Owner decision.
4. **Ambient melting.** The cold control slowly melts in room air after about 50 s; acceptable, or should cold-control levels use a cold environment preset? Owner decision.
