# TH-24 · Gas expansion bladder — named-identity readiness spec

Story 7.0 named-identity spec ([spec](../../../_bmad-output/implementation-artifacts/spec-7-0-element-implementation-readiness.md)). Citations are `path@a6c914e:Lstart-Lend`. No legacy bladder exists; the legacy sealed-gas chamber supplies the equation of state, volume work and test oracles (section 4). Every design value without a cited source is marked "(proposed: …)" with a one-line justification; the owner may revise it.

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name | TH-24 · Gas expansion bladder |
| Type | Thermal · potential element (potential/conditional; source adoption retained) |
| Anchors | [requirements.md#thermal-24](../requirements.md#thermal-24) (sequence-task-458); [named-elements.md#thermal-24](../invest/named-elements.md#thermal-24) |
| Proof owner / research | S587 · [TH-S06 thermal expansion](../../thermal-component-research.md#th-s06) |
| Related identities | No CAT spec. The [gas family](../../finite-gas-foundation.md) lists it with [TH-25 Hot-air balloon](TH-25-hot-air-balloon.md) as "sealed gas store + heat family". Bellows form echoes [CAT-010 Bellows](CAT-010-bellows.md). Heat from [TH-04](TH-04-electrical-heating-plate.md). |
| Campaign | Intro 77; practice 78; reuse 90, 143 ([thermal allocation](../requirements.md#thermal-campaign-allocation)) |
| Roadmap story | Unscheduled. No epic in `_bmad-output/planning-artifacts/epics.md` schedules a thermal element or the sealed gas store. |
| Status | Not started. No `WorkshopPartKind` member (`engine/gpu/WorkshopPartKind.cs@a6c914e:L3-L3`); no legacy part. |

## 2. Declaration

- **Batch N game scale** (proposed, identical in all 37 TH specs; chains derived against it are in [TH-17](TH-17-kettle.md#batch-n-chains); owner question in section 6). Ambient reservoir 288 K and 101.3 kPa. [Research table](../../thermal-component-research.md) rows: specific heat, latent heat and source power read as listed range × scale (64–4096 J/(kg·K), 8192–2,097,152 J/kg, 0.5–2048 W); temperature (128–2048 K), conductance (2⁻¹⁰–2⁸ W/K), fuel/supply (finite), ignition threshold (400–1200 K) and sensor thresholds read as listed, without scale; expansion coefficient reads 2⁻²⁰–2⁻¹⁴ 1/K after scale, which TH-22 and TH-23 exceed on purpose as a declared exaggeration. Condensed-phase specific heat SI × ¼ and latent heat SI × 1/16, so catalogue-scale bodies change temperature or phase within one Run. Gas R and Cv SI × 1, so γ and pressure stay physical; each condensed phase's enthalpy is referenced so vapour made at T_sat carries m·Cv·T_sat and the scaled latent heat is exactly the heat consumed; because water's scaled latent heat (141 kJ/kg) is below R·T_sat (172 kJ/kg), boiling follows a declared saturation curve, not Clausius–Clapeyron. Every fuel's reaction energy and O₂ demand per kilogram SI × 1/1024 (wax 41,016 J/kg and 3.4/1024 kg O₂/kg; wood and cellulose 15,625 J/kg and 1.2/1024 kg O₂/kg), so a 0.3 kg candle holds about 12 kJ, heat per kilogram of O₂ keeps Thornton's ≈ 13 MJ/kg, and fuel mass is consumed 1:1 so burn-down stays visible. Still-air surface exchange h = 2.5 W/(m²·K) (SI × ¼, so condensed-body time constants C/(hA) stay SI-like while gas-node ones run about 4× slower than SI); forced air h = 2.5 + u (u in m/s). Contact pairs conduct G = G_a·G_b/(G_a + G_b) from each part's face conductance: metal 64, ceramic, wood, ice or soft 16, probe tip 0.5 W/K. Every exchange is clamped per 480 Hz substep to the equalising amount ΔT·C_a·C_b/(C_a + C_b), so small nodes never overshoot. Reactive surfaces carry a thermal-only patch node outside rigid mass (fibre 10 mg, wood 0.5 g; area 0.0025 m²; patch-to-bulk 0.01 W/K; thermal nodes 10⁻⁶–1024 kg, not bound by the 1/1024 kg rigid minimum) that takes strikes, flames and light first. Radiant optical power: 1 legacy intensity unit = 1 W, so the flashlight emits 24 W. Canonical f32 ([numeric contract](../../gpu-f32-physics.md#game-grade-envelope)).
- **Variants.** None named in the row. The "vented control" is an authored vent enum `Sealed` (default) or `Vented` (proposed: the same geometry with an open vent, each proved separately).
- **Bodies and shapes.** Static cream neck base: box half-extents 0.15 × 0.05 × 0.15 m. Dynamic top plate on a slider: box with half-extents 0.05 × 0.02 × 0.05 m, area 0.01 m² (proposed: a 0.1 m plate gives readable lift without huge forces). Stroke 0–0.20 m above the rest height. The bellows dome between them is visual; the gas volume is V(q) = V₀ + A·q.
- **Mass and material.** Static base rigid mass zero (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L70-L76`); top plate 0.1 kg (proposed: light). Skin thermal node 0.05 kg, c = 500 J/(kg·K) (rubber 2000 × ¼, batch scale; 25 J/K). Gas: air at SI R and Cv (batch scale), V₀ = 0.004 m³ at 288 K and 101.3 kPa, mass 0.0049 kg, 3.5 J/K (derived) (proposed volume: heats within seconds). Base: soft face 16 W/K (batch scale), pairing at 12.8 W/K with a heating plate.
- **Constraints.** Slider joint for the top plate with end stops at 0 and 0.20 m; skin elastic return k = 200 N/m (proposed: the compliant boundary stores elastic energy and returns the plate when cooled).
- **Typed ports.** None as sockets. Heat enters through the base contact. `Vented` declares an open vent to ambient.
- **Sensors and activation.** None.
- **Work and energy stores.** Sealed gas store (m, V, U). Heating raises U; pressure p = ρRT pushes the plate: force (p − p_amb)·A against load, gravity and skin spring. Isochoric heating by 100 K raises p by about 35 % (derived), about 350 N on the 0.01 m² plate. Raising skin and gas by 100 K costs about 2.85 kJ, about 10 s through the plate pair. Work on the load debits the gas's internal energy (legacy law, section 4). `Vented`: pressure stays at ambient and the plate does not rise.
- **Parameters.**

  | Parameter | Type | Range | Default | Unit | Source |
  | --- | --- | --- | --- | --- | --- |
  | Vent | enum `Sealed`/`Vented` | closed set | `Sealed` | — | (proposed: above) |
  | Gas volume at rest | f32 | fixed | 0.004 | m³ | (proposed: above) |
  | Plate area | f32 | fixed | 0.01 | m² | (proposed: above) |
  | Skin stiffness | f32 | fixed | 200 | N/m | (proposed: above) |
  | Stroke | f32 | fixed | 0.20 | m | (proposed: above) |

- **Cosmetic curves and UI bindings.** Skin ← committed volume ([research per-element table](../../thermal-component-research.md); [gas family](../../finite-gas-foundation.md) "skin scale ← committed volume"); gold volume marks on the neck.
- **Art.** "Cyan geometric bellows dome, cream neck and gold volume marks" (row). Nearest [DESIGN.md](../../../DESIGN.md) source colours (proposed mapping): cyan `#66b8c9` (`DESIGN.md@a6c914e:L172-L172`), cream `#ead39b` (`DESIGN.md@a6c914e:L146-L146`), gold `#f7cb52` (`DESIGN.md@a6c914e:L154-L154`).
- **Catalogue and inventory entry.** New `WorkshopPartKind.GasBladder`; id `gas_bladder`, title "Gas expansion bladder", category Heat (proposed: no current category covers thermal parts).

## 3. Engine capabilities

Families (map row TH-24, [general-engine-element-map](../general-engine-element-map.md); binding [thermal-01.json](../../coverage/engine/thermal-01.json)): ElasticStorage, EnvironmentState, FiniteLedger, GasState, GeometryQuery, JointConstraint, RigidBodyDynamics, SensibleHeat, ThermalExpansion, TopologyTransaction; JSON adds StateTransaction. Composition note: PressureWork and deformable volume/elastic boundary coupling required in addition to gas and temperature data.

- **Exists now.** Rigid bodies and box colliders (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L120`); Run/Reset and save (`engine/gpu/ActivationNetwork.cs@a6c914e:L67-L67`, `engine/gpu/WorkshopSaveCodec.cs@a6c914e:L6-L10`).
- **Missing.** Slider and elastic store: Stories 6.4–6.5. Sealed gas store: [S470](../invest/decisions.md#s470) gas-state → S471. PressureWork: [S416](../invest/decisions.md#s416) → S419. Heat into gas: [S543](../invest/decisions.md#s543) expansion → S557, conduction → S544. Unscheduled.
- **Dependencies.** A heat source (TH-04); a load (a ball or CAT-067 Weight).

## 4. Sources and legacy

- **Requirement row** [thermal-24](../requirements.md#thermal-24): compliant sealed boundary; equation of state couples heat, pressure, volume and work; no variants.
- **Named entry** [thermal-24](../invest/named-elements.md#thermal-24): owner S587; "a vented control cannot retain the same pressure; blocked expansion changes pressure rather than prescribing motion".
- **Research** per-element table ("sealed gas store + node"; material; inflates when heated; skin ← volume) and the gas family per-element row.
- **Processes.** [IX-40](../requirements.md#interaction-40), [IX-09](../requirements.md#interaction-09), [IX-31](../requirements.md#interaction-31).
- **Legacy harvest** (sealed-gas foundation; no part consumed it).

  | # | Fact | Citation | Disposition |
  | --- | --- | --- | --- |
  | 1 | State T = U/(m·Cv), p = ρRT, derived only. | `engine/physics/SealedGasState.cs@a6c914e:L18-L41` | Carry forward. |
  | 2 | Adiabatic volume change U₂ = U₁(V₁/V₂)^(R/Cv); work on gas = ΔU. Oracle: V 0.01 → 0.0025 m³ gives U 4800 J, 600 K, 960 kPa, work +2400 J; → 0.04 m³ gives 1200 J, 150 K, 15 kPa, −1200 J. | `engine/physics/SealedGasState.cs@a6c914e:L43-L59`, `CuriousContraptions.tests/SealedGasStateTests.cs@a6c914e:L22-L41` | Carry forward law and oracle. |
  | 3 | Chamber V(q) = V₀ + A·(q − q₀) bound to an owned slider. | `engine/physics/AxialGasGeometry.cs@a6c914e:L25-L33`, `engine/physics/AxialGasLoad.cs@a6c914e:L27-L45` | Carry forward. |
  | 4 | A collision cuts compression work and both domains restore exactly. | `CuriousContraptions.tests/GasChamberWorldTests.cs@a6c914e:L67-L81` | Carry forward: blocked expansion changes pressure. |
  | 5 | Legacy gas had no heat input; the material excluded an ambient source. | `engine/physics/IdealGasMaterial.cs@a6c914e:L5-L6` | Recorded: heat input is new. |
  | 6 | Out-of-envelope states threw. | `engine/physics/SealedGasState.cs@a6c914e:L29-L33` | Do not carry forward: clamp-or-continue at runtime. |

  Files harvested: `engine/physics/SealedGasState.cs`, `engine/physics/AxialGasGeometry.cs`, `engine/physics/AxialGasLoad.cs`, `engine/physics/IdealGasMaterial.cs`, `CuriousContraptions.tests/SealedGasStateTests.cs`, `CuriousContraptions.tests/GasChamberWorldTests.cs`.

## 5. Acceptance outline

Point of truth: [thermal-24](../requirements.md#thermal-24) and [ELEMENT acceptance](../requirements.md#accept-element).

- **Chrome recipe.** Through the real palette and sockets: a `Sealed` bladder on a supplied Heating plate (TH-04), a Basketball resting on the top plate, a Receiver on a ledge 0.15 m higher.
- **Positive.** The gas warms; the plate rises and lifts the ball within about 10 s; gas internal-energy change = heat in − work on load − skin energy within the envelope.
- **Negative or control.** `Vented` bladder: pressure stays ambient, no lift. Plate blocked by a Wall: no motion; pressure rises instead. Unsupplied plate: nothing moves.
- **Boundaries.** Plate stops at 0.20 m; force never exceeds (p − p_amb)·A; cooling returns the plate under the skin spring.
- **Run/Reset.** Restores gas inventory, 288 K, plate pose and vent state exactly.
- **Save/Load.** Pose and vent round-trip; Run after reload reproduces the outcome.
- **Integrations.** Gas family pneumatics; campaign 77, 78, 90, 143.

## 6. Open questions

1. **Batch N game scale.** Adopt or revise the one scale in section 2 (×¼ specific heat, ×1/16 latent heat, gas ×1, reaction energy and O₂ ×1/1024, h 2.5 W/(m²·K), per-pair series contact conductance from per-part face values, 1 intensity unit = 1 W, and which research-table rows the scale multiplies). It carries two consequences: boiling needs a declared saturation curve (S550) because scaled water latent heat is below R·T_sat, and gas-node time constants run about 4× slower than SI (TH-25). Owner decision.
2. **Gas store schedule.** S471 is unscheduled. Owner decision.
3. **Vent preset.** Is `Vented` a declared preset or a separate valve part? Owner decision.
4. **Map row.** The composition note requires PressureWork, absent from the family list. Owner decision.
