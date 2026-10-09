# TH-03 · Combustible block — named-identity readiness spec

Story 7.0 named-identity spec ([spec](../../../_bmad-output/implementation-artifacts/spec-7-0-element-implementation-readiness.md)). Citations are `path@a6c914e:Lstart-Lend`. No legacy holds combustible-block values, so every design value without a cited source is marked "(proposed: …)" with a one-line justification; the owner may revise it.

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name | TH-03 · Combustible block |
| Type | Thermal · potential element (potential/conditional; source adoption retained) |
| Anchors | [requirements.md#thermal-03](../requirements.md#thermal-03) (sequence-task-437); [named-elements.md#thermal-03](../invest/named-elements.md#thermal-03) |
| Proof owner / research | S596 · [TH-S03 combustion and suppression](../../thermal-component-research.md#th-s03) |
| Related identities | Historical record [todo-196](../requirements.md#todo-196) is shared with [EL-155](../invest/named-elements.md#element-155), [EL-210](../invest/named-elements.md#element-210), [TH-01](TH-01-candle.md) and [TH-06](TH-06-converging-lens.md). Shape and dynamics follow the delivered box body of [CAT-023 Domino](CAT-023-domino.md). Noncombustible control: [TH-26 Thermal storage block](TH-26-thermal-storage-block.md). |
| Campaign | Intro 58; practice 59; reuse 85, 149 ([thermal allocation](../requirements.md#thermal-campaign-allocation)) |
| Roadmap story | Unscheduled. No epic in `_bmad-output/planning-artifacts/epics.md` schedules a thermal element. |
| Status | Not started. No `WorkshopPartKind` member (`engine/gpu/WorkshopPartKind.cs@a6c914e:L3-L3`); no legacy part. |

## 2. Declaration

- **Batch N game scale** (proposed, identical in all 37 TH specs; chains derived against it are in [TH-17](TH-17-kettle.md#batch-n-chains); owner question in section 6). Ambient reservoir 288 K and 101.3 kPa. [Research table](../../thermal-component-research.md) rows: specific heat, latent heat and source power read as listed range × scale (64–4096 J/(kg·K), 8192–2,097,152 J/kg, 0.5–2048 W); temperature (128–2048 K), conductance (2⁻¹⁰–2⁸ W/K), fuel/supply (finite), ignition threshold (400–1200 K) and sensor thresholds read as listed, without scale; expansion coefficient reads 2⁻²⁰–2⁻¹⁴ 1/K after scale, which TH-22 and TH-23 exceed on purpose as a declared exaggeration. Condensed-phase specific heat SI × ¼ and latent heat SI × 1/16, so catalogue-scale bodies change temperature or phase within one Run. Gas R and Cv SI × 1, so γ and pressure stay physical; each condensed phase's enthalpy is referenced so vapour made at T_sat carries m·Cv·T_sat and the scaled latent heat is exactly the heat consumed; because water's scaled latent heat (141 kJ/kg) is below R·T_sat (172 kJ/kg), boiling follows a declared saturation curve, not Clausius–Clapeyron. Every fuel's reaction energy and O₂ demand per kilogram SI × 1/1024 (wax 41,016 J/kg and 3.4/1024 kg O₂/kg; wood and cellulose 15,625 J/kg and 1.2/1024 kg O₂/kg), so a 0.3 kg candle holds about 12 kJ, heat per kilogram of O₂ keeps Thornton's ≈ 13 MJ/kg, and fuel mass is consumed 1:1 so burn-down stays visible. Still-air surface exchange h = 2.5 W/(m²·K) (SI × ¼, so condensed-body time constants C/(hA) stay SI-like while gas-node ones run about 4× slower than SI); forced air h = 2.5 + u (u in m/s). Contact pairs conduct G = G_a·G_b/(G_a + G_b) from each part's face conductance: metal 64, ceramic, wood, ice or soft 16, probe tip 0.5 W/K. Every exchange is clamped per 480 Hz substep to the equalising amount ΔT·C_a·C_b/(C_a + C_b), so small nodes never overshoot. Reactive surfaces carry a thermal-only patch node outside rigid mass (fibre 10 mg, wood 0.5 g; area 0.0025 m²; patch-to-bulk 0.01 W/K; thermal nodes 10⁻⁶–1024 kg, not bound by the 1/1024 kg rigid minimum) that takes strikes, flames and light first. Radiant optical power: 1 legacy intensity unit = 1 W, so the flashlight emits 24 W. Canonical f32 ([numeric contract](../../gpu-f32-physics.md#game-grade-envelope)).
- **Variants.** None named in the row; one wood material.
- **Bodies and shapes.** One dynamic body with one box collider, half-extents 0.20 × 0.20 × 0.20 m (proposed: a 0.4 m chamfered cube stacks beside the 0.25 × 1.1 × 0.65 m Domino, `engine/gpu/WorkshopDomino.cs@a6c914e:L9-L9`). Consumption shrinks the half-extents by (remaining mass fraction)^⅓ (proposed: uniform shrink keeps the silhouette readable).
- **Mass and material.** 0.4 kg (proposed: the Domino's mass, so it can stand in for one), centre of mass at the box centre. Thermal: c = 425 J/(kg·K) (wood × ¼, batch scale), k = 0.12 W/(m·K), emissivity 0.9 (proposed: wood SI values). Wood face 16 W/K; surface patch: the batch wood patch (0.5 g). Ambient exchange 2.5 W/(m²·K) × 0.96 m² = 2.4 W/K. Contact friction 0.6, restitution 0.05 (proposed: the Domino's declared values, same source). Admitted rigid bounds: mass 1/1024–1024 kg, half-extents 1/1024–16 m (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L78-L110`).
- **Constraints.** None of its own; as a structural member it may be joined by future joints (JointConstraint in the map row).
- **Typed ports.** None.
- **Sensors and activation.** None. The 0.5 g patch ignites at 573 K (proposed: piloted wood ignition 570–620 K); it needs about 60 J, so a neighbouring flame (a candle plume delivers 75 W) lights it in under a second while a single ≈ 2 J strike does not. Extinction below 520 K, at zero fuel or below 0.15 O₂ fraction (proposed: hysteresis and oxygen limit, as TH-02).
- **Work and energy stores.** Reactant 0.4 kg × 15,625 J/kg = 6,250 J (batch scale). Reaction power 96 W (proposed: between a candle's 80 W and the fire bowl's 256 W for a single block) → 65 s burn. Oxidiser 1.2/1024 kg O₂ per kg (batch scale). Ash residue 10 % of mass, inert (proposed: "spent fuel cannot reignite indefinitely").
- **Strength.** Support capacity 196 N at 288 K unburnt (proposed: carries a 20 kg load, far above catalogue masses), scaled by (1 − burnt fraction) and falling linearly to 0 at 773 K (proposed: wood loses strength strongly above about 500 K). Overload fractures into at most 4 conserved fragments (proposed: bounded fragments under S563).
- **Parameters.**

  | Parameter | Type | Range | Default | Unit | Source |
  | --- | --- | --- | --- | --- | --- |
  | Mass | f32 | fixed | 0.4 | kg | (proposed: above) |
  | Reaction power | f32 | fixed | 96 | W | (proposed: above) |
  | Ignition / extinction | f32 | fixed | 573 / 520 | K | (proposed: above) |
  | Support capacity at 288 K | f32 | fixed | 196 | N | (proposed: above) |

- **Cosmetic curves and UI bindings.** Char fraction ← burnt fraction ([research per-element table](../../thermal-component-research.md)); engraved fuel symbol plus readable consumed volume (row).
- **Art.** "Warm-wood chamfered block, engraved fuel symbol and readable consumed volume" (row). Nearest [DESIGN.md](../../../DESIGN.md) source colour (proposed mapping): warm wood `#b77c42` (`DESIGN.md@a6c914e:L146-L146`), navy symbol `#293954` (`DESIGN.md@a6c914e:L147-L147`).
- **Catalogue and inventory entry.** New `WorkshopPartKind.CombustibleBlock`; id `combustible_block`, title "Combustible block", category Heat (proposed: no current category covers thermal parts).

## 3. Engine capabilities

Families (map row TH-03, [general-engine-element-map](../general-engine-element-map.md); binding [thermal-01.json](../../coverage/engine/thermal-01.json)): ChemicalReaction, EnvironmentState, Extinction, FiniteLedger, GeometryQuery, Ignition, JointConstraint, RigidBodyDynamics, SensibleHeat, StructuralFracture, TopologyTransaction; JSON adds StateTransaction.

- **Exists now.** Dynamic box body with derived inertia (Story 5.1; `engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L120`, `engine/gpu/RigidMassProperties.cs@a6c914e:L17-L17`); contact and friction; Run/Reset and save (`engine/gpu/ActivationNetwork.cs@a6c914e:L67-L67`, `engine/gpu/WorkshopSaveCodec.cs@a6c914e:L6-L10`).
- **Missing.** [S543](../invest/decisions.md#s543): reaction → S554, ignition → S555, extinction → S556, fracture → S563, phase-topology (shrinking collider, mass and inertia) → S565, temperature-strength → S566. JointConstraint: Story 6.4 slider and Story 10.3 pivot. EnvironmentState: [S635](../invest/decisions.md#s635) environment → LAW-ENVIRONMENT-I. All thermal rows unscheduled.
- **Composition gap.** The row text requires temperature-dependent strength but the map row omits TemperatureStrength. See Open questions.
- **Dependencies.** An ignition source and a heat path; TH-26 for the control.

## 4. Sources and legacy

- **Requirement row** [thermal-03](../requirements.md#thermal-03): finite reactants, chemical energy, temperature-dependent strength, generic mass and geometry consumption; no variants.
- **Named entry** [thermal-03](../invest/named-elements.md#thermal-03): owner S596; "a noncombustible control absorbs heat but does not burn; spent fuel cannot reignite indefinitely".
- **Research** per-element table: "node with ignition threshold + fuel"; parameters ignition T, burn rate; "ignites from a hot neighbour, burns and loses mass"; char fraction binding.
- **Processes and scenarios.** [IX-28](../requirements.md#interaction-28), [IX-29](../requirements.md#interaction-29), [IX-30](../requirements.md#interaction-30), [IX-37](../requirements.md#interaction-37), [IX-41](../requirements.md#interaction-41), [IX-42](../requirements.md#interaction-42); [TX-03](../requirements.md#thermal-scenario-03), [TX-12](../requirements.md#thermal-scenario-12).
- **Legacy.** None found. Searched `parts/`, `engine/physics/`, `engine/*.cs`, `engine/bridge/`, `CuriousContraptions.tests/`, `content/puzzles.json`, `tools/Campaign` and `reference/` at a6c914e for combustible, fuel, wood, burn and fracture terms. Consulted, no element knowledge: `reference/P0-022-before/docs/coverage/engine/task-002.json@a6c914e:L2783-L2787` (historical coverage-scope rows).

## 5. Acceptance outline

Point of truth: [thermal-03](../requirements.md#thermal-03) and [ELEMENT acceptance](../requirements.md#accept-element).

- **Chrome recipe.** Through the real palette, place a Combustible block covering the plume of a lit Candle (TH-01), with a 1 kg load resting on it; in a second run place a Thermal storage block (TH-26) of the same size in the same position.
- **Positive.** The 75 W plume heats the patch past 573 K in under a second; the block burns at 96 W for about 65 s, shrinks and loses mass; the load drops once capacity falls below its 9.8 N weight.
- **Negative or control.** TH-26 in the same place warms but never ignites. A block that has burned to ash, reheated, does not ignite again.
- **Boundaries.** Patch at 572 K no ignition; a ≈ 2 J strike alone no ignition; zero remaining reactant no flame; a 1.0 kg load on a cold block holds; mass never below the 10 % ash residue.
- **Run/Reset.** Restores full mass, collider size, temperature, reaction state and unfractured topology exactly.
- **Save/Load.** Pose round-trips; Run after reload reproduces the outcome.
- **Integrations.** TX-03 focused-light ignition; TX-12 cooling extinguishes; structural use under IX-42.

## 6. Open questions

1. **Batch N game scale.** Adopt or revise the one scale in section 2 (×¼ specific heat, ×1/16 latent heat, gas ×1, reaction energy and O₂ ×1/1024, h 2.5 W/(m²·K), per-pair series contact conductance from per-part face values, 1 intensity unit = 1 W, and which research-table rows the scale multiplies). It carries two consequences: boiling needs a declared saturation curve (S550) because scaled water latent heat is below R·T_sat, and gas-node time constants run about 4× slower than SI (TH-25). Owner decision.
2. **Map row.** Add TemperatureStrength to TH-03, or confirm StructuralFracture covers it. Owner decision.
3. **Burn rate model.** Fixed reaction power versus a rate that scales with exposed area and oxygen. Owner decision (S554).
4. **Fragments.** Fragment count and whether burning fragments keep reacting. Owner decision (S563).
5. **Oxygen model.** Boolean flag versus finite gas transport. Owner decision (S555).
