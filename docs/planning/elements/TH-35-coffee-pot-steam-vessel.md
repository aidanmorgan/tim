# TH-35 · Coffee-pot steam vessel — named-identity readiness spec

Story 7.0 named-identity spec ([spec](../../../_bmad-output/implementation-artifacts/spec-7-0-element-implementation-readiness.md)). Citations are `path@a6c914e:Lstart-Lend`. No legacy coffee pot exists; the legacy sealed-gas foundation supplies laws and test oracles (section 4). Every design value without a cited source is marked "(proposed: …)" with a one-line justification; the owner may revise it.

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name | TH-35 · Coffee-pot steam vessel |
| Type | Thermal · potential element (potential/conditional; source adoption retained) |
| Anchors | [requirements.md#thermal-35](../requirements.md#thermal-35) (sequence-task-469); [named-elements.md#thermal-35](../invest/named-elements.md#thermal-35) |
| Proof owner / research | S600 · [TH-S02 phase change](../../thermal-component-research.md#th-s02), [TH-S07](../../thermal-component-research.md#th-s07) |
| Related identities | No CAT spec. A distinct vessel geometry beside [TH-17 Kettle](TH-17-kettle.md); the same phase and flow laws, no coffee-pot event. Heat from [TH-04](TH-04-electrical-heating-plate.md) or a stored-hot [TH-26](TH-26-thermal-storage-block.md); vapour to [TH-19](TH-19-steam-piston.md), [TH-18](TH-18-condenser.md) or a receiver. |
| Campaign | Intro 86; practice 87; reuse 100, 140 ([thermal allocation](../requirements.md#thermal-campaign-allocation)) |
| Roadmap story | Unscheduled. No epic in `_bmad-output/planning-artifacts/epics.md` schedules a thermal element or the sealed gas store. |
| Status | Not started. No `WorkshopPartKind` member (`engine/gpu/WorkshopPartKind.cs@a6c914e:L3-L3`); no legacy part. |

## 2. Declaration

- **Batch N game scale** (proposed, identical in all 37 TH specs; chains derived against it are in [TH-17](TH-17-kettle.md#batch-n-chains); owner question in section 6). Ambient reservoir 288 K and 101.3 kPa. [Research table](../../thermal-component-research.md) rows: specific heat, latent heat and source power read as listed range × scale (64–4096 J/(kg·K), 8192–2,097,152 J/kg, 0.5–2048 W); temperature (128–2048 K), conductance (2⁻¹⁰–2⁸ W/K), fuel/supply (finite), ignition threshold (400–1200 K) and sensor thresholds read as listed, without scale; expansion coefficient reads 2⁻²⁰–2⁻¹⁴ 1/K after scale, which TH-22 and TH-23 exceed on purpose as a declared exaggeration. Condensed-phase specific heat SI × ¼ and latent heat SI × 1/16, so catalogue-scale bodies change temperature or phase within one Run. Gas R and Cv SI × 1, so γ and pressure stay physical; each condensed phase's enthalpy is referenced so vapour made at T_sat carries m·Cv·T_sat and the scaled latent heat is exactly the heat consumed; because water's scaled latent heat (141 kJ/kg) is below R·T_sat (172 kJ/kg), boiling follows a declared saturation curve, not Clausius–Clapeyron. Every fuel's reaction energy and O₂ demand per kilogram SI × 1/1024 (wax 41,016 J/kg and 3.4/1024 kg O₂/kg; wood and cellulose 15,625 J/kg and 1.2/1024 kg O₂/kg), so a 0.3 kg candle holds about 12 kJ, heat per kilogram of O₂ keeps Thornton's ≈ 13 MJ/kg, and fuel mass is consumed 1:1 so burn-down stays visible. Still-air surface exchange h = 2.5 W/(m²·K) (SI × ¼, so condensed-body time constants C/(hA) stay SI-like while gas-node ones run about 4× slower than SI); forced air h = 2.5 + u (u in m/s). Contact pairs conduct G = G_a·G_b/(G_a + G_b) from each part's face conductance: metal 64, ceramic, wood, ice or soft 16, probe tip 0.5 W/K. Every exchange is clamped per 480 Hz substep to the equalising amount ΔT·C_a·C_b/(C_a + C_b), so small nodes never overshoot. Reactive surfaces carry a thermal-only patch node outside rigid mass (fibre 10 mg, wood 0.5 g; area 0.0025 m²; patch-to-bulk 0.01 W/K; thermal nodes 10⁻⁶–1024 kg, not bound by the 1/1024 kg rigid minimum) that takes strikes, flames and light first. Radiant optical power: 1 legacy intensity unit = 1 W, so the flashlight emits 24 W. Canonical f32 ([numeric contract](../../gpu-f32-physics.md#game-grade-envelope)).
- **Variants.** None named in the row.
- **Bodies and shapes.** One dynamic body: pot box, half-extents 0.08 × 0.14 × 0.08 m, plus a gold handle box with half-extents 0.02 × 0.08 × 0.03 m on −X and a narrow spout on +X 0.22 m above the base (proposed: taller and narrower than the 0.2 m kettle so the silhouettes differ; colliders are boxes, `engine/gpu/PhysicsDeclarations.cs@a6c914e:L89-L120`).
- **Mass and material.** Empty pot 0.3 kg (proposed: a light toy vessel); rigid mass adds the water charge. Wall node 0.15 kg steel, c = 122 J/(kg·K) (batch scale; 18.3 J/K). Water under the batch scale: liquid c = 1046 J/(kg·K), vaporisation 141,250 J/kg at 373 K; steam R = 461.5, Cv = 1410 J/(kg·K) SI. Base: metal face 64 W/K (batch scale), so the plate pair is 32 W/K and a ceramic block pair 12.8 W/K. Ambient exchange 2.5 × 0.23 m² ≈ 0.58 W/K. Contact friction 0.5, restitution 0.05 (proposed: matte ceramic-steel).
- **Constraints.** None.
- **Typed ports.** Gas outlet port at the spout (proposed: gas family typed outlet); unconnected it vents to ambient. Heat enters through the base.
- **Sensors and activation.** None: steam output follows shared phase and flow laws, never a "coffee-pot event" (row).
- **Work and energy stores.** Liquid charge 0–0.05 kg, default 0.02 kg (proposed: the kettle's charge, so the two compare directly). Head space: sealed gas store of 0.0015 m³ (proposed: smaller than the kettle's 0.002 m³, so pressure builds faster), initially air at 288 K and 101.3 kPa: 1.84 g, 1.32 J/K at SI Cv. Budget: total 40.5 J/K needs about 3.4 kJ to reach 373 K; 2 g of vapour then costs 283 J (derived). Constrained spout: converging nozzle throat 5 × 10⁻⁵ m² (proposed: half the kettle's spout, giving the distinct constrained jet the row asks for). At the plate's boil rate (about 3 g/s) the jet leaves at about 100 m/s with the head about 3 kPa above ambient, against the kettle's 46 m/s (derived from the subsonic nozzle law). Lid relief above 300 kPa absolute (proposed: a perking lid, below the kettle's 400 kPa).
- **Parameters.**

  | Parameter | Type | Range | Default | Unit | Source |
  | --- | --- | --- | --- | --- | --- |
  | Water charge | f32 | 0–0.05 | 0.02 | kg | (proposed: above) |
  | Head-space volume | f32 | fixed | 0.0015 | m³ | (proposed: above) |
  | Spout throat area | f32 | fixed | 5 × 10⁻⁵ | m² | (proposed: above) |
  | Lid relief pressure | f32 | fixed | 300 | kPa | (proposed: above) |

- **Cosmetic curves and UI bindings.** Lid ← committed head pressure ([research per-element table](../../thermal-component-research.md)); the small cyan fill window ← committed liquid mass (row). No white plume stands in for vapour.
- **Art.** "Cream rounded pot with a gold handle and small cyan fill window" (row). Nearest [DESIGN.md](../../../DESIGN.md) source colours (proposed mapping): cream `#ead39b` (`DESIGN.md@a6c914e:L146-L146`), gold `#f7cb52` (`DESIGN.md@a6c914e:L154-L154`), cyan `#66b8c9` (`DESIGN.md@a6c914e:L172-L172`).
- **Catalogue and inventory entry.** New `WorkshopPartKind.CoffeePot`; id `coffee_pot`, title "Coffee-pot steam vessel", category Heat (proposed: no current category covers thermal parts).

## 3. Engine capabilities

Families (map row TH-35, [general-engine-element-map](../general-engine-element-map.md); binding [thermal-01.json](../../coverage/engine/thermal-01.json)): EnvironmentState, FiniteLedger, GasState, GeometryQuery, JointConstraint, LiquidBoiling, PhaseTopology, PressureWork, RigidBodyDynamics, SensibleHeat, TopologyTransaction; JSON adds StateTransaction.

- **Exists now.** Dynamic box bodies (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L120`); Run/Reset and save (`engine/gpu/ActivationNetwork.cs@a6c914e:L67-L67`, `engine/gpu/WorkshopSaveCodec.cs@a6c914e:L6-L10`).
- **Missing.** Sealed gas store and nozzle: [S470](../invest/decisions.md#s470) → S471. Liquid inventory: [S416](../invest/decisions.md#s416) → S418. [S543](../invest/decisions.md#s543): boiling → S550, conduction → S544, phase-topology → S565. All unscheduled.
- **Dependencies.** A heat source (TH-04, TH-26); a vapour consumer or receiver.

## 4. Sources and legacy

- **Requirement row** [thermal-35](../requirements.md#thermal-35): distinct vessel geometry with finite liquid, thermal contacts and a constrained spout; shared phase/flow laws produce steam; no variants.
- **Named entry** [thermal-35](../invest/named-elements.md#thermal-35): owner S600; "an empty or insufficiently heated pot does not emit vapor; a blocked spout uses actual pressure boundaries".
- **Research** per-element table: "node + sealed gas + nozzle"; capacity; "perks and vents steam to a receiver"; lid ← pressure.
- **Processes.** [IX-24](../requirements.md#interaction-24), [IX-40](../requirements.md#interaction-40), [IX-08](../requirements.md#interaction-08).
- **Legacy harvest** (sealed-gas foundation; no part consumed it; the full table is in [TH-17](TH-17-kettle.md)).

  | # | Fact | Citation | Disposition |
  | --- | --- | --- | --- |
  | 1 | Head-space state T = U/(m·Cv), p = ρRT, derived only. | `engine/physics/SealedGasState.cs@a6c914e:L18-L41` | Carry forward. |
  | 2 | Nozzle regimes NoFlow, Subsonic, Choked; forward flow only. | `engine/physics/ConvergingGasNozzle.cs@a6c914e:L39-L87` | Carry forward for the spout. |
  | 3 | Closed area or balanced pressure gives no flux. | `CuriousContraptions.tests/ConvergingGasNozzleTests.cs@a6c914e:L64-L74` | Carry forward: a blocked spout emits nothing. |
  | 4 | Doubling throat area doubles choked mass rate. | `CuriousContraptions.tests/ConvergingGasNozzleTests.cs@a6c914e:L44-L46` | Carry forward: the half-area spout gives half the kettle's choked rate. |
  | 5 | Out-of-envelope states threw. | `engine/physics/SealedGasState.cs@a6c914e:L29-L33` | Do not carry forward: clamp-or-continue at runtime. |
  | 6 | No phase transition in the gas material. | `engine/physics/IdealGasMaterial.cs@a6c914e:L5-L6` | Recorded: boiling is new. |

  Files harvested: `engine/physics/SealedGasState.cs`, `engine/physics/ConvergingGasNozzle.cs`, `engine/physics/IdealGasMaterial.cs`, `CuriousContraptions.tests/ConvergingGasNozzleTests.cs`.

## 5. Acceptance outline

Point of truth: [thermal-35](../requirements.md#thermal-35) and [ELEMENT acceptance](../requirements.md#accept-element). Plate figures follow the [batch chains](TH-17-kettle.md#batch-n-chains).

- **Chrome recipe.** Through the real palette and sockets: Battery → Switch → Heating plate (TH-04), a filled Coffee-pot (0.02 kg) on it, spout aimed at a Heat-sensitive target (TH-37).
- **Positive.** Plate warm-up (about 6 kJ) plus the pot's 3.4 kJ at about 450 W: the water reaches 373 K after about 21 s and boils at about 3 g/s; the first 2 g leave the spout under a second later as a narrower, faster jet than the kettle's; liquid lost = vapour emitted; the target warms.
- **Negative or control.** Empty pot: no vapour. Insufficient heating (plate switched off after 10 s): plate and pot settle near 337 K, below boiling, and no vapour leaves. Blocked spout: pressure and boiling point rise along the declared saturation curve until the lid perks at 300 kPa.
- **Boundaries.** No vapour below the local boiling point; choked spout rate is half the kettle's at the same head state.
- **Run/Reset.** Restores charge, 288 K, head-space air and lid exactly.
- **Save/Load.** Pose and charge round-trip; Run after reload reproduces the outcome.
- **Integrations.** Lesson 86 vessel variant; campaign 86, 87, 100, 140.

## 6. Open questions

1. **Batch N game scale.** Adopt or revise the one scale in section 2 (×¼ specific heat, ×1/16 latent heat, gas ×1, reaction energy and O₂ ×1/1024, h 2.5 W/(m²·K), per-pair series contact conductance from per-part face values, 1 intensity unit = 1 W, and which research-table rows the scale multiplies). It carries two consequences: boiling needs a declared saturation curve (S550) because scaled water latent heat is below R·T_sat, and gas-node time constants run about 4× slower than SI (TH-25). Owner decision.
2. **Distinctness.** Confirm the pot differs from TH-17 only by geometry and declared values, with no separate law. Owner decision.
3. **Perk behaviour.** Real percolators recirculate liquid; the row asks only for steam output. Owner decision.
4. **Gas store schedule.** S471 is unscheduled. Owner decision.
