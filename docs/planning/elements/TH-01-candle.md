# TH-01 · Candle — named-identity readiness spec

Story 7.0 named-identity spec ([spec](../../../_bmad-output/implementation-artifacts/spec-7-0-element-implementation-readiness.md)). Citations are `path@a6c914e:Lstart-Lend`. No legacy holds candle values, so every design value without a cited source is marked "(proposed: …)" with a one-line justification; the owner may revise it.

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name | TH-01 · Candle |
| Type | Thermal · potential element (potential/conditional; source adoption retained) |
| Anchors | [requirements.md#thermal-01](../requirements.md#thermal-01) (sequence-task-435); [named-elements.md#thermal-01](../invest/named-elements.md#thermal-01) |
| Proof owner / research | S598 · [TH-S03 combustion and suppression](../../thermal-component-research.md#th-s03) |
| Related identities | Historical record [todo-196](../requirements.md#todo-196) is shared with [EL-155](../invest/named-elements.md#element-155), [EL-210](../invest/named-elements.md#element-210), [TH-03](TH-03-combustible-block.md) and [TH-06](TH-06-converging-lens.md). No CAT spec: no catalogue candle exists. Ignition sources [TH-31](TH-31-flint-striker.md), [TH-32](TH-32-tinder-pad.md), [TH-33](TH-33-spring-mounted-match.md); receivers [TH-17](TH-17-kettle.md), [TH-37](TH-37-heat-sensitive-target.md). |
| Campaign | Intro 56; practice 57; reuse 83, 137 ([thermal allocation](../requirements.md#thermal-campaign-allocation)) |
| Roadmap story | Unscheduled. No epic in `_bmad-output/planning-artifacts/epics.md` schedules a thermal element. |
| Status | Not started. No `WorkshopPartKind` member (`engine/gpu/WorkshopPartKind.cs@a6c914e:L3-L3`); no legacy part. |

## 2. Declaration

- **Batch N game scale** (proposed, identical in all 37 TH specs; chains derived against it are in [TH-17](TH-17-kettle.md#batch-n-chains); owner question in section 6). Ambient reservoir 288 K and 101.3 kPa. [Research table](../../thermal-component-research.md) rows: specific heat, latent heat and source power read as listed range × scale (64–4096 J/(kg·K), 8192–2,097,152 J/kg, 0.5–2048 W); temperature (128–2048 K), conductance (2⁻¹⁰–2⁸ W/K), fuel/supply (finite), ignition threshold (400–1200 K) and sensor thresholds read as listed, without scale; expansion coefficient reads 2⁻²⁰–2⁻¹⁴ 1/K after scale, which TH-22 and TH-23 exceed on purpose as a declared exaggeration. Condensed-phase specific heat SI × ¼ and latent heat SI × 1/16, so catalogue-scale bodies change temperature or phase within one Run. Gas R and Cv SI × 1, so γ and pressure stay physical; each condensed phase's enthalpy is referenced so vapour made at T_sat carries m·Cv·T_sat and the scaled latent heat is exactly the heat consumed; because water's scaled latent heat (141 kJ/kg) is below R·T_sat (172 kJ/kg), boiling follows a declared saturation curve, not Clausius–Clapeyron. Every fuel's reaction energy and O₂ demand per kilogram SI × 1/1024 (wax 41,016 J/kg and 3.4/1024 kg O₂/kg; wood and cellulose 15,625 J/kg and 1.2/1024 kg O₂/kg), so a 0.3 kg candle holds about 12 kJ, heat per kilogram of O₂ keeps Thornton's ≈ 13 MJ/kg, and fuel mass is consumed 1:1 so burn-down stays visible. Still-air surface exchange h = 2.5 W/(m²·K) (SI × ¼, so condensed-body time constants C/(hA) stay SI-like while gas-node ones run about 4× slower than SI); forced air h = 2.5 + u (u in m/s). Contact pairs conduct G = G_a·G_b/(G_a + G_b) from each part's face conductance: metal 64, ceramic, wood, ice or soft 16, probe tip 0.5 W/K. Every exchange is clamped per 480 Hz substep to the equalising amount ΔT·C_a·C_b/(C_a + C_b), so small nodes never overshoot. Reactive surfaces carry a thermal-only patch node outside rigid mass (fibre 10 mg, wood 0.5 g; area 0.0025 m²; patch-to-bulk 0.01 W/K; thermal nodes 10⁻⁶–1024 kg, not bound by the 1/1024 kg rigid minimum) that takes strikes, flames and light first. Radiant optical power: 1 legacy intensity unit = 1 W, so the flashlight emits 24 W. Canonical f32 ([numeric contract](../../gpu-f32-physics.md#game-grade-envelope)).
- **Variants.** The requirement row names no variants or modes. One authored initial state enum, `Unlit` (default) or `Lit` (proposed: a level may start with a burning candle; both are separate proofs under "each supported mode receives its own proof").
- **Bodies and shapes.** One static body. Collider: box, half-extents 0.15 × 0.30 × 0.15 m for the wax column (proposed: no cylinder collider exists, `engine/gpu/PhysicsDeclarations.cs@a6c914e:L89-L120`; the 0.6 m column is about half the 1.1 m height of the Domino, whose half-extents are 0.125 × 0.55 × 0.325 m, `engine/gpu/WorkshopDomino.cs@a6c914e:L9-L9`), plus a holder plate box with half-extents 0.20 × 0.03 × 0.20 m (proposed: stable footprint). Plume region above the wick, full size 0.20 × 0.60 × 0.20 m (proposed: one flame-width wide, so a receiver must sit directly above; a body covering the whole cross-section captures the whole convective share).
- **Mass and material.** Static rigid bodies declare zero rigid mass (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L70-L76`), so thermal mass is a separate node. Wax node 0.3 kg, c = 625 J/(kg·K) (paraffin 2500 × ¼ under the batch scale). Wick tip: the batch fibre patch node (10 mg, c 425 J/(kg·K)). Contact face: wax, 16 W/K (batch scale). Contact friction 0.6, restitution 0.05 (proposed: same matte solid as the Domino, `engine/gpu/WorkshopDomino.cs@a6c914e:L9-L9`).
- **Constraints.** None.
- **Typed ports.** None. Heat leaves through the plume region and contacts; light leaves through a spatial optical aperture at the wick, which is not a socket ([optical connections](../requirements.md#sequence-task-281)).
- **Sensors and activation.** None. Ignition is physical: the wick patch reaches 500 K (proposed: piloted wax-vapour ignition is about 470–520 K) with oxidiser available; the patch needs 0.90 J from 288 K. Extinction when the wick falls below 450 K (proposed: 50 K hysteresis prevents flicker), fuel reaches zero, or local O₂ fraction falls below 0.15 (proposed: diffusion flames go out near 15 % O₂).
- **Work and energy stores.** Finite fuel 0.3 kg wax × 41,016 J/kg = 12,305 J (batch scale). Reaction power 80 W while burning (proposed: a household candle releases about 80 W, so the SI power is kept) → 154 s burn. Split 75 W convective to the plume, 4 W thermal radiation, 1 W radiant light (1 intensity unit) (proposed: heat dominates; light is small but accountable). Oxidiser 3.4/1024 kg O₂ per kg wax: 6.5 mg/s while burning.
- **Parameters.**

  | Parameter | Type | Range | Default | Unit | Source |
  | --- | --- | --- | --- | --- | --- |
  | Initial state | enum `Unlit`/`Lit` | closed set | `Unlit` | — | (proposed: see Variants) |
  | Fuel mass | f32 | 0.05–0.5 | 0.3 | kg | (proposed: burn time 26–256 s at 80 W) |
  | Reaction power | f32 | fixed | 80 | W | (proposed: above) |
  | Ignition / extinction temperature | f32 | fixed | 500 / 450 | K | (proposed: above) |

  Authored per level, not player-tuned (proposed: matches fixed-per-kind ball materials, `engine/gpu/WorkshopConstruction.cs@a6c914e:L45-L61`).
- **Cosmetic curves and UI bindings.** Flame scale ← committed reaction power; wax height ← remaining fuel ([research per-element table](../../thermal-component-research.md)). Engraved height marks show fuel without colour alone ([visual contract](../requirements.md#thermal-elements)).
- **Art.** "Cream wax cylinder with a recessed wick and restrained, state-driven flame" (row). Nearest [DESIGN.md](../../../DESIGN.md) source colours (proposed mapping): cream deck `#ead39b` (`DESIGN.md@a6c914e:L146-L146`), gold `#f7cb52` flame core (`DESIGN.md@a6c914e:L154-L154`).
- **Catalogue and inventory entry.** New `WorkshopPartKind.Candle`; catalogue id `candle`, title "Candle", category Heat (proposed: no current category covers thermal parts; existing ones are Control, Goals, Motion, Optics, Power, Ropes, Sound, Structure).

## 3. Engine capabilities

Families (map row TH-01 in [general-engine-element-map](../general-engine-element-map.md), binding [thermal-01.json](../../coverage/engine/thermal-01.json)): ChemicalReaction, Extinction, FiniteLedger, GeometryQuery, Ignition, SensibleHeat, TopologyTransaction; the JSON also lists StateTransaction.

- **Exists now.** Static body and box collider (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L120`); finite paid work in joules, partial FiniteLedger (`engine/gpu/ContactWorkDeclaration.cs@a6c914e:L5-L38`); Run/Reset checkpoint and save codec, StateTransaction (`engine/gpu/ActivationNetwork.cs@a6c914e:L67-L67`, `engine/gpu/WorkshopSaveCodec.cs@a6c914e:L6-L10`).
- **Missing.** Every thermal family. Decision owner [S543](../invest/decisions.md#s543): reaction → S554, ignition → S555, extinction → S556, phase-topology (mass/geometry consumption) → S565. SensibleHeat (IX-43) has no S543 row. No epic story builds any of them.
- **Composition gaps.** The map row omits ThermalConvection (plume, S545), ThermalRadiation (S546), OpticalTransport ([S484](../invest/decisions.md#s484), Story 13.1 builds emitters) and GasState/EnvironmentState for oxygen ([S470](../invest/decisions.md#s470) → S471; [S635](../invest/decisions.md#s635) environment). See Open questions.
- **Dependencies.** An ignition source (TH-31, TH-32, TH-33, or TH-06 with TH-32) and a heat receiver (TH-37 or TH-17).

## 4. Sources and legacy

- **Requirement row** [thermal-01](../requirements.md#thermal-01): finite wax inventory; shared combustion; accounted heat/light; heat and oxygen conditions; no variants listed.
- **Named entry** [thermal-01](../invest/named-elements.md#thermal-01): owner S598; exact outcome "an unlit or exhausted candle produces no sustained output".
- **Research** [TH-S03](../../thermal-component-research.md#th-s03) and the per-element table: "finite fuel source + reaction state + convective link"; parameters power, fuel, ignition T; flame scale and wax height bindings.
- **Processes and scenarios.** [IX-28](../requirements.md#interaction-28), [IX-29](../requirements.md#interaction-29), [IX-30](../requirements.md#interaction-30), [IX-19](../requirements.md#interaction-19); [TX-01](../requirements.md#thermal-scenario-01), [TX-12](../requirements.md#thermal-scenario-12), [TX-14](../requirements.md#thermal-scenario-14).
- **Legacy.** None found. Searched `parts/`, `engine/physics/`, `engine/*.cs`, `engine/bridge/`, `CuriousContraptions.tests/`, `content/puzzles.json`, `tools/Campaign` and `reference/` at a6c914e for candle, combustion, ignition, flame and heat terms. Consulted, no element knowledge: `reference/P0-022-before/docs/coverage/engine/task-002.json@a6c914e:L2783-L2787`, a historical coverage-scope row naming thermal-01, superseded by the current binding.

## 5. Acceptance outline

Point of truth: [thermal-01](../requirements.md#thermal-01) and [ELEMENT acceptance](../requirements.md#accept-element). Numbers are derived in the [batch chains](TH-17-kettle.md#batch-n-chains).

- **Chrome recipe.** Through the real palette and move gizmo, place a Candle (`Unlit`), a Heat-sensitive target (TH-37, threshold 330 K) covering the plume region above it, and a Flint striker (TH-31) at the wick with a Basketball released 1.0 m up a Ramp to push it.
- **Positive.** The ball pushes the striker arm across its sprung flint plate and the strike deposits about 2 J into the 0.00425 J/K wick patch (→ about 750 K ≥ 500 K); the candle lights. The target captures the 75 W plume, rises toward 343 K (loss 1.35 W/K), passes 330 K after about 44 s and holds it for its dwell, well inside the 154 s burn. Fuel debit equals released heat plus light within the envelope.
- **Negative or control.** No striker: the candle stays unlit and the target stays at 288 K. Exhausted candle (fuel 0.05 kg authored low, 26 s of burn): it goes out before the target reaches 330 K and the target cools without re-lighting. Enclosed candle (O₂ restricted, after TX-14 support): it goes out.
- **Boundaries.** Fuel exactly 0 cannot ignite; wick at 499 K does not ignite; the ledger never goes negative; a target outside the plume region receives only radiation.
- **Run/Reset.** Restores fuel, wick and wax temperatures, flame state and wax height exactly.
- **Save/Load.** Construction (pose, initial state) round-trips; Run after reload reproduces the outcome.
- **Integrations.** TX-01 (candle → kettle: first 2 g of vapour after about 67 s), TX-12, TX-14; campaign 56, 57, 83, 137.

## 6. Open questions

1. **Batch N game scale.** Adopt or revise the one scale in section 2 (×¼ specific heat, ×1/16 latent heat, gas ×1, reaction energy and O₂ ×1/1024, h 2.5 W/(m²·K), per-pair series contact conductance from per-part face values, 1 intensity unit = 1 W, and which research-table rows the scale multiplies). It carries two consequences: boiling needs a declared saturation curve (S550) because scaled water latent heat is below R·T_sat, and gas-node time constants run about 4× slower than SI (TH-25). Owner decision.
2. **Oxygen model.** The research table names a boolean "oxidiser availability flag"; the row and TX-14 require oxygen conditions from finite gas transport. At 6.5 mg/s a candle needs a very small enclosure (about 0.005 m³ to fall to 15 % O₂ in a minute) for TX-14. Owner decision (S554/S555).
3. **Map composition.** Add ThermalConvection, ThermalRadiation, OpticalTransport and GasState to the TH-01 row, or confirm they come through shared processes. Owner decision.
4. **Light output.** Should the candle's 1 W light drive optical receivers (EL-153) and solar panels? Owner decision.
5. **Collider.** A box stands in for the wax cylinder until a cylinder collider exists. Owner decision.
