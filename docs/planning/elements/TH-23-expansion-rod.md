# TH-23 · Expansion rod — named-identity readiness spec

Story 7.0 named-identity spec ([spec](../../../_bmad-output/implementation-artifacts/spec-7-0-element-implementation-readiness.md)). Citations are `path@a6c914e:Lstart-Lend`. No legacy holds expansion-rod values, so every design value without a cited source is marked "(proposed: …)" with a one-line justification; the owner may revise it.

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name | TH-23 · Expansion rod |
| Type | Thermal · potential element (potential/conditional; source adoption retained) |
| Anchors | [requirements.md#thermal-23](../requirements.md#thermal-23) (sequence-task-457); [named-elements.md#thermal-23](../invest/named-elements.md#thermal-23) |
| Proof owner / research | S586 · [TH-S06 thermal expansion](../../thermal-component-research.md#th-s06) |
| Related identities | No CAT spec. Shares the expansion law with [TH-22 Bimetal thermostat](TH-22-bimetal-thermostat.md); pushes [CAT-034 Impact lever](CAT-034-impact_lever.md) or a [CAT-063 Switch](CAT-063-switch.md); slider guides as in Story 6.4. |
| Campaign | Intro 82; practice 83; reuse 90, 146 ([thermal allocation](../requirements.md#thermal-campaign-allocation)) |
| Roadmap story | Unscheduled. No epic in `_bmad-output/planning-artifacts/epics.md` schedules a thermal element. |
| Status | Not started. No `WorkshopPartKind` member (`engine/gpu/WorkshopPartKind.cs@a6c914e:L3-L3`); no legacy part. |

## 2. Declaration

- **Batch N game scale** (proposed, identical in all 37 TH specs; chains derived against it are in [TH-17](TH-17-kettle.md#batch-n-chains); owner question in section 6). Ambient reservoir 288 K and 101.3 kPa. [Research table](../../thermal-component-research.md) rows: specific heat, latent heat and source power read as listed range × scale (64–4096 J/(kg·K), 8192–2,097,152 J/kg, 0.5–2048 W); temperature (128–2048 K), conductance (2⁻¹⁰–2⁸ W/K), fuel/supply (finite), ignition threshold (400–1200 K) and sensor thresholds read as listed, without scale; expansion coefficient reads 2⁻²⁰–2⁻¹⁴ 1/K after scale, which TH-22 and TH-23 exceed on purpose as a declared exaggeration. Condensed-phase specific heat SI × ¼ and latent heat SI × 1/16, so catalogue-scale bodies change temperature or phase within one Run. Gas R and Cv SI × 1, so γ and pressure stay physical; each condensed phase's enthalpy is referenced so vapour made at T_sat carries m·Cv·T_sat and the scaled latent heat is exactly the heat consumed; because water's scaled latent heat (141 kJ/kg) is below R·T_sat (172 kJ/kg), boiling follows a declared saturation curve, not Clausius–Clapeyron. Every fuel's reaction energy and O₂ demand per kilogram SI × 1/1024 (wax 41,016 J/kg and 3.4/1024 kg O₂/kg; wood and cellulose 15,625 J/kg and 1.2/1024 kg O₂/kg), so a 0.3 kg candle holds about 12 kJ, heat per kilogram of O₂ keeps Thornton's ≈ 13 MJ/kg, and fuel mass is consumed 1:1 so burn-down stays visible. Still-air surface exchange h = 2.5 W/(m²·K) (SI × ¼, so condensed-body time constants C/(hA) stay SI-like while gas-node ones run about 4× slower than SI); forced air h = 2.5 + u (u in m/s). Contact pairs conduct G = G_a·G_b/(G_a + G_b) from each part's face conductance: metal 64, ceramic, wood, ice or soft 16, probe tip 0.5 W/K. Every exchange is clamped per 480 Hz substep to the equalising amount ΔT·C_a·C_b/(C_a + C_b), so small nodes never overshoot. Reactive surfaces carry a thermal-only patch node outside rigid mass (fibre 10 mg, wood 0.5 g; area 0.0025 m²; patch-to-bulk 0.01 W/K; thermal nodes 10⁻⁶–1024 kg, not bound by the 1/1024 kg rigid minimum) that takes strikes, flames and light first. Radiant optical power: 1 legacy intensity unit = 1 W, so the flashlight emits 24 W. Canonical f32 ([numeric contract](../../gpu-f32-physics.md#game-grade-envelope)).
- **Variants.** None named in the row.
- **Bodies and shapes.** Static cream base with two guide boxes, each half-extents 0.06 × 0.06 × 0.06 m, 0.8 m apart (proposed: guides read clearly along a 1 m rod). Dynamic gold rod: box half-extents 0.50 × 0.03 × 0.03 m at 288 K (proposed: a slender 1 m rod). One end anchored to the base; the free end carries a small push face.
- **Mass and material.** Rod 0.5 kg (proposed: comparable to a Domino); thermal node the same 0.5 kg, c = 95 J/(kg·K) (brass 380 × ¼, batch scale; 47.5 J/K). Rod face: metal 64 W/K (batch scale), pairing at 32 W/K with a heating plate; ambient exchange 2.5 × 0.24 m² = 0.6 W/K. Expansion coefficient α = 2⁻¹⁰ ≈ 9.8 × 10⁻⁴ 1/K, a declared exaggeration of about 50× SI brass (1.9 × 10⁻⁵ 1/K) and 16× the research expansion row's scaled maximum of 2⁻¹⁴ 1/K (the listed 2⁻¹⁰–2⁻⁴ times its 2⁻¹⁰ scale) (proposed: 1 m then lengthens about 0.1 m over 100 K, visible at catalogue scale). Contact friction in the guides 0.1 (proposed: smooth bearings).
- **Constraints.** Slider joint along the rod axis through the guides; anchored end fixed. Natural length L(T) = L₀·(1 + α·(T − 288 K)) feeds an axial compliance row with stiffness k = 20,000 N/m (proposed: stiff enough that a blocked rod builds hundreds of newtons yet stays soft-solvable), never a direct transform write (research expansion coupling).
- **Typed ports.** None as sockets; work leaves through the push face contact.
- **Sensors and activation.** None.
- **Work and energy stores.** Rod node enthalpy; elastic strain energy ½·k·(ΔL_blocked)² when the free end is restrained (IX-32). Work F·Δx done on a load and any strain energy are accounted through the thermal-expansion coupling (S557); without a temperature change there is no work.
- **Parameters.**

  | Parameter | Type | Range | Default | Unit | Source |
  | --- | --- | --- | --- | --- | --- |
  | Length at 288 K | f32 | fixed | 1.0 | m | (proposed: above) |
  | Expansion coefficient | f32 | fixed | 2⁻¹⁰ | 1/K | (proposed: about 50× SI exaggeration, above) |
  | Axial stiffness | f32 | fixed | 20,000 | N/m | (proposed: above) |
  | Thermal mass | f32 | fixed | 0.5 | kg | (proposed: above) |

- **Cosmetic curves and UI bindings.** Rod length ← committed strain ([research per-element table](../../thermal-component-research.md)); navy reference marks on the guides make the stroke readable.
- **Art.** "Slender gold rod with cream guides and navy reference marks" (row). Nearest [DESIGN.md](../../../DESIGN.md) source colours (proposed mapping): gold `#f7cb52` (`DESIGN.md@a6c914e:L154-L154`), cream `#ead39b` (`DESIGN.md@a6c914e:L146-L146`), navy `#293954` (`DESIGN.md@a6c914e:L147-L147`).
- **Catalogue and inventory entry.** New `WorkshopPartKind.ExpansionRod`; id `expansion_rod`, title "Expansion rod", category Heat (proposed: no current category covers thermal parts).

## 3. Engine capabilities

Families (map row TH-23, [general-engine-element-map](../general-engine-element-map.md); binding [thermal-01.json](../../coverage/engine/thermal-01.json)): EnvironmentState, FiniteLedger, GeometryQuery, JointConstraint, RigidBodyDynamics, SensibleHeat, ThermalExpansion, TopologyTransaction; JSON adds StateTransaction.

- **Exists now.** Rigid bodies and box colliders (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L120`); Run/Reset and save (`engine/gpu/ActivationNetwork.cs@a6c914e:L67-L67`, `engine/gpu/WorkshopSaveCodec.cs@a6c914e:L6-L10`).
- **Missing.** Slider joint and soft compliance: Story 6.4. [S543](../invest/decisions.md#s543): expansion → S557, thermal-stress → S558 (constrained case, not in the map row), conduction → S544. Collider length changing with temperature: phase-topology → S565. Unscheduled.
- **Dependencies.** A heat source (TH-04, TH-01); a load (CAT-034 Impact lever, CAT-063 Switch).

## 4. Sources and legacy

- **Requirement row** [thermal-23](../requirements.md#thermal-23): thermal strain changes natural length; stiffness, constraints and load determine actual displacement and work; no variants.
- **Named entry** [thermal-23](../invest/named-elements.md#thermal-23): owner S586; "insufficient temperature change cannot bridge the target gap; constrained heating produces accounted stress".
- **Research** per-element table: "expansion → slider constraint"; parameters coefficient, length; "pushes a lever as it warms"; rod length ← strain. Expansion coefficient row 2⁻¹⁰–2⁻⁴ with scale 2⁻¹⁰ (2⁻²⁰–2⁻¹⁴ 1/K).
- **Processes.** [IX-31](../requirements.md#interaction-31), [IX-32](../requirements.md#interaction-32), [IX-03](../requirements.md#interaction-03).
- **Legacy.** None found. Searched `parts/`, `engine/physics/`, `engine/*.cs`, `engine/bridge/`, `CuriousContraptions.tests/`, `content/puzzles.json`, `tools/Campaign` and `reference/` at a6c914e for expansion, strain and rod terms. Consulted, no element knowledge: `reference/P0-022-before/docs/coverage/engine/task-002.json@a6c914e:L2783-L2787` (historical coverage-scope rows).

## 5. Acceptance outline

Point of truth: [thermal-23](../requirements.md#thermal-23) and [ELEMENT acceptance](../requirements.md#accept-element).

- **Chrome recipe.** Through the real palette and gizmo: an Expansion rod on a supplied Heating plate (TH-04), its push face 0.05 m from an Impact switch (CAT-063) wired to a Signal lamp.
- **Positive.** The rod warms by about 51 K through the 32 W/K pair, lengthens 0.05 m, presses the switch and the lamp lights.
- **Negative or control.** Plate switched off after a 30 K rise: the rod lengthens about 0.03 m and never reaches the switch. Push face against a Wall: no motion; accounted stress k·αL₀ΔT appears instead.
- **Boundaries.** Length follows L(T) within the envelope; free expansion never exceeds αL₀ΔT; cooling returns the natural length.
- **Run/Reset.** Restores 1.0 m length, 288 K and rod pose exactly.
- **Save/Load.** Pose round-trips; Run after reload reproduces the outcome.
- **Integrations.** Lever and switch parts; campaign 82, 83, 90, 146.

## 6. Open questions

1. **Batch N game scale.** Adopt or revise the one scale in section 2 (×¼ specific heat, ×1/16 latent heat, gas ×1, reaction energy and O₂ ×1/1024, h 2.5 W/(m²·K), per-pair series contact conductance from per-part face values, 1 intensity unit = 1 W, and which research-table rows the scale multiplies). It carries two consequences: boiling needs a declared saturation curve (S550) because scaled water latent heat is below R·T_sat, and gas-node time constants run about 4× slower than SI (TH-25). Owner decision.
2. **Stress in the map.** Constrained heating needs ThermalStress, which the TH-23 map row omits. Owner decision.
3. **Stiffness.** Fixed 20,000 N/m versus a material modulus × area / length. Owner decision (S557).
4. **Failure.** Can a constrained rod fracture (S558/S563), or only push? Owner decision.
5. **Expansion exaggeration.** α = 2⁻¹⁰ 1/K is about 50× SI brass and 16× the research row's scaled maximum of 2⁻¹⁴ 1/K. Confirm the declared exaggeration (shared with TH-22), or widen the research row. Owner decision.
