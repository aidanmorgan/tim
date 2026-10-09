# TH-19 · Steam piston — named-identity readiness spec

Story 7.0 named-identity spec ([spec](../../../_bmad-output/implementation-artifacts/spec-7-0-element-implementation-readiness.md)). Citations are `path@a6c914e:Lstart-Lend`. No legacy steam piston exists; the legacy sealed-gas chamber supplies the actuator law and test oracles (section 4). Every design value without a cited source is marked "(proposed: …)" with a one-line justification; the owner may revise it.

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name | TH-19 · Steam piston |
| Type | Thermal · potential element (potential/conditional; source adoption retained) |
| Anchors | [requirements.md#thermal-19](../requirements.md#thermal-19) (sequence-task-453); [named-elements.md#thermal-19](../invest/named-elements.md#thermal-19) |
| Proof owner / research | S583 · [TH-S07 heat to mechanical work](../../thermal-component-research.md#th-s07) |
| Related identities | No CAT spec. Configures the same generic pressure actuator as the gas family's pneumatic piston ([gas family per-element table](../../finite-gas-foundation.md)) and the hydraulic piston of [S416](../invest/decisions.md#s416) pressure-work. Vapour from [TH-17](TH-17-kettle.md) or [TH-35](TH-35-coffee-pot-steam-vessel.md); exhaust to [TH-18](TH-18-condenser.md). Rod geometry echoes [CAT-039 Linear pusher](CAT-039-linear_pusher.md). |
| Campaign | Intro 75; practice 76; reuse 86, 143 ([thermal allocation](../requirements.md#thermal-campaign-allocation)) |
| Roadmap story | Unscheduled. No epic in `_bmad-output/planning-artifacts/epics.md` schedules a thermal element or the sealed gas chamber. |
| Status | Not started. No `WorkshopPartKind` member (`engine/gpu/WorkshopPartKind.cs@a6c914e:L3-L3`); no legacy part. |

## 2. Declaration

- **Batch N game scale** (proposed, identical in all 37 TH specs; chains derived against it are in [TH-17](TH-17-kettle.md#batch-n-chains); owner question in section 6). Ambient reservoir 288 K and 101.3 kPa. [Research table](../../thermal-component-research.md) rows: specific heat, latent heat and source power read as listed range × scale (64–4096 J/(kg·K), 8192–2,097,152 J/kg, 0.5–2048 W); temperature (128–2048 K), conductance (2⁻¹⁰–2⁸ W/K), fuel/supply (finite), ignition threshold (400–1200 K) and sensor thresholds read as listed, without scale; expansion coefficient reads 2⁻²⁰–2⁻¹⁴ 1/K after scale, which TH-22 and TH-23 exceed on purpose as a declared exaggeration. Condensed-phase specific heat SI × ¼ and latent heat SI × 1/16, so catalogue-scale bodies change temperature or phase within one Run. Gas R and Cv SI × 1, so γ and pressure stay physical; each condensed phase's enthalpy is referenced so vapour made at T_sat carries m·Cv·T_sat and the scaled latent heat is exactly the heat consumed; because water's scaled latent heat (141 kJ/kg) is below R·T_sat (172 kJ/kg), boiling follows a declared saturation curve, not Clausius–Clapeyron. Every fuel's reaction energy and O₂ demand per kilogram SI × 1/1024 (wax 41,016 J/kg and 3.4/1024 kg O₂/kg; wood and cellulose 15,625 J/kg and 1.2/1024 kg O₂/kg), so a 0.3 kg candle holds about 12 kJ, heat per kilogram of O₂ keeps Thornton's ≈ 13 MJ/kg, and fuel mass is consumed 1:1 so burn-down stays visible. Still-air surface exchange h = 2.5 W/(m²·K) (SI × ¼, so condensed-body time constants C/(hA) stay SI-like while gas-node ones run about 4× slower than SI); forced air h = 2.5 + u (u in m/s). Contact pairs conduct G = G_a·G_b/(G_a + G_b) from each part's face conductance: metal 64, ceramic, wood, ice or soft 16, probe tip 0.5 W/K. Every exchange is clamped per 480 Hz substep to the equalising amount ΔT·C_a·C_b/(C_a + C_b), so small nodes never overshoot. Reactive surfaces carry a thermal-only patch node outside rigid mass (fibre 10 mg, wood 0.5 g; area 0.0025 m²; patch-to-bulk 0.01 W/K; thermal nodes 10⁻⁶–1024 kg, not bound by the 1/1024 kg rigid minimum) that takes strikes, flames and light first. Radiant optical power: 1 legacy intensity unit = 1 W, so the flashlight emits 24 W. Canonical f32 ([numeric contract](../../gpu-f32-physics.md#game-grade-envelope)).
- **Variants.** None named in the row.
- **Bodies and shapes.** Static cream cylinder body: box, half-extents 0.12 × 0.12 × 0.40 m (proposed: a 0.8 m barrel, the Linear pusher's scale). Dynamic gold rod-and-head body: rod box with half-extents 0.03 × 0.03 × 0.35 m, head box with half-extents 0.10 × 0.10 × 0.03 m (proposed: a readable rod that pushes a ball-sized load). Stroke 0.40 m (proposed: half a metre-scale move is visible on the bench).
- **Mass and material.** Rod-and-head 0.3 kg (proposed: light enough that the gas does the work, heavier than a Domino's tip impulse). Head contact friction 0.3, restitution 0.1 (proposed: smooth metal). Steam R = 461.5, Cv = 1410 J/(kg·K) SI (batch scale).
- **Constraints.** One slider joint (rod on barrel axis) with end stops at 0 and 0.40 m; return spring k = 100 N/m (proposed: returns the unloaded rod against residual pressure, as the gas family's pneumatic piston "spring return").
- **Typed ports.** Gas inlet and gas exhaust ports, enum-typed (proposed: gas family port types; vapour-capable seals). Mechanical output: the head's contact face.
- **Sensors and activation.** None. No steam-specific force callback: force = (p_chamber − p_ambient)·A.
- **Work and energy stores.** Chamber: sealed gas store bound to the slider, V(q) = V₀ + A·q with bore area A = 0.002 m² (proposed: 50 kPa gauge gives 100 N, enough to push the 4 kg Bowling ball) and dead volume V₀ = 0.0004 m³ (proposed: 20 cm of bore at rest). Exhaust port uncovered at q ≥ 0.38 m, venting to the exhaust port (proposed: a uniflow-style port position, geometric, not timed). Work on the load = ∫(p − p_amb)·A dq, debited from the chamber's internal energy. A full stroke at 50 kPa gauge holds about 1.05 g of steam (0.0012 m³ at 151 kPa, 373 K), so the TH-17 kettle's 20 g charge gives about 19 strokes, one every 0.4 s at the plate-driven 2.7 g/s boil rate (derived).
- **Parameters.**

  | Parameter | Type | Range | Default | Unit | Source |
  | --- | --- | --- | --- | --- | --- |
  | Bore area | f32 | fixed | 0.002 | m² | (proposed: above) |
  | Stroke | f32 | fixed | 0.40 | m | (proposed: above) |
  | Dead volume | f32 | fixed | 0.0004 | m³ | (proposed: above) |
  | Return stiffness | f32 | fixed | 100 | N/m | (proposed: above) |
  | Exhaust uncover position | f32 | fixed | 0.38 | m | (proposed: above) |

- **Cosmetic curves and UI bindings.** Rod ← committed slider position ([research per-element table](../../thermal-component-research.md)); no cosmetic stroke animation.
- **Art.** "Cream cylinder, gold rod and separate embossed inlet/exhaust ports" (row). Nearest [DESIGN.md](../../../DESIGN.md) source colours (proposed mapping): cream `#ead39b` (`DESIGN.md@a6c914e:L146-L146`), gold `#f7cb52` (`DESIGN.md@a6c914e:L154-L154`).
- **Catalogue and inventory entry.** New `WorkshopPartKind.SteamPiston`; id `steam_piston`, title "Steam piston", category Heat (proposed: no current category covers thermal parts; Motion is the alternative).

## 3. Engine capabilities

Families (map row TH-19, [general-engine-element-map](../general-engine-element-map.md); binding [thermal-01.json](../../coverage/engine/thermal-01.json)): EnvironmentState, FiniteLedger, GasState, JointConstraint, PressureWork, RigidBodyDynamics; JSON adds StateTransaction. Composition note: inlet/exhaust finite mass and enthalpy transport, chamber work and load coupling; GasState alone is not a piston cycle.

- **Exists now.** Rigid bodies and box colliders (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L120`); Run/Reset and save (`engine/gpu/ActivationNetwork.cs@a6c914e:L67-L67`, `engine/gpu/WorkshopSaveCodec.cs@a6c914e:L6-L10`).
- **Missing.** Slider joint and spring: Story 6.4 (prismatic slider, TGS soft spring). Sealed gas chamber: [S470](../invest/decisions.md#s470) gas-state → S471. PressureWork: [S416](../invest/decisions.md#s416) pressure-work → S419. Gas ports and advection: S471/S697. All gas and thermal rows unscheduled.
- **Dependencies.** A vapour source (TH-17); a load (CAT-067 Weight, a ball).

## 4. Sources and legacy

- **Requirement row** [thermal-19](../requirements.md#thermal-19): catalogue assembly of the generic pressure actuator with vapour-capable seals, inlet and exhaust; no steam-specific force callback; no variants.
- **Named entry** [thermal-19](../invest/named-elements.md#thermal-19): owner S583; "no pressure difference, blocked exhaust or excessive load prevents the predicted stroke".
- **Research** per-element table: "chamber + slider body"; parameters stroke, load; rod ← slider.
- **Processes and scenarios.** [IX-09](../requirements.md#interaction-09), [IX-40](../requirements.md#interaction-40), [IX-08](../requirements.md#interaction-08); [TX-04](../requirements.md#thermal-scenario-04).
- **Legacy harvest** (sealed-gas chamber foundation; no part consumed it).

  | # | Fact | Citation | Disposition |
  | --- | --- | --- | --- |
  | 1 | Chamber geometry V(q) = V₀ + A_signed·(q − q₀); volume must stay positive. | `engine/physics/AxialGasGeometry.cs@a6c914e:L5-L33` | Carry forward. |
  | 2 | Chamber effort at rest = A·p; finite-interval effort from the adiabatic energy difference; both orientations share the law. | `engine/physics/AxialGasPotential.cs@a6c914e:L36-L73` | Carry forward the law; f32. |
  | 3 | Oracle: A = 0.1 m², gas 120 kPa; travel 0.3 m to V = 0.04 m³ gives U 2400 → 1200 J and effort × travel = 1200 J. | `CuriousContraptions.tests/AxialGasPotentialTests.cs@a6c914e:L7-L28` | Carry forward as test oracle. |
  | 4 | A chamber binds only to an owned slider; inventory owned by a slider participant. | `engine/physics/AxialGasLoad.cs@a6c914e:L27-L45` | Carry forward as declaration validation. |
  | 5 | Gas drives shared motion: U + kinetic energy conserved (240 J), motion sign follows signed area, exact replay after restore. | `CuriousContraptions.tests/GasChamberWorldTests.cs@a6c914e:L32-L65` | Carry forward as acceptance facts. |
  | 6 | A collision cuts compression work and both domains restore exactly. | `CuriousContraptions.tests/GasChamberWorldTests.cs@a6c914e:L67-L81` | Carry forward: blocked load stops the stroke. |
  | 7 | Equal opposed chambers hold without spending inventory; inventory without a mechanical binding supplies no motion. | `CuriousContraptions.tests/GasChamberWorldTests.cs@a6c914e:L116-L144` | Carry forward: no pressure difference, no stroke. |
  | 8 | Unsupported motion rolled back the whole step with an exception. | `CuriousContraptions.tests/GasChamberWorldTests.cs@a6c914e:L103-L114` | Do not carry forward: game-grade clamp-or-continue. |
  | 9 | Interval work-error acceptance by quadrature. | `engine/physics/GasPredictionWork.cs@a6c914e:L7-L54` | Do not carry forward: CPU prediction certificate. |

  Files harvested: `engine/physics/AxialGasGeometry.cs`, `engine/physics/AxialGasPotential.cs`, `engine/physics/AxialGasLoad.cs`, `engine/physics/GasPredictionWork.cs`, `CuriousContraptions.tests/AxialGasPotentialTests.cs`, `CuriousContraptions.tests/GasChamberWorldTests.cs`. Consulted for state laws: `engine/physics/SealedGasState.cs@a6c914e:L43-L59` (adiabatic volume work).

## 5. Acceptance outline

Point of truth: [thermal-19](../requirements.md#thermal-19), [TX-04](../requirements.md#thermal-scenario-04) and [ELEMENT acceptance](../requirements.md#accept-element).

- **Chrome recipe.** Through the real palette and fluid connections: a Kettle (TH-17) boiling on a supplied Heating plate, hosed to the piston inlet, exhaust hosed to a Condenser (TH-18), a Basketball in front of the head, a Receiver beyond.
- **Positive.** Chamber pressure rises, the rod extends and pushes the ball to the Receiver; at 0.38 m the exhaust uncovers and the spring returns the rod; mass in = mass out; work on the ball ≤ enthalpy supplied.
- **Negative or control.** Cold kettle (no pressure difference): no stroke. Exhaust blocked: the rod stalls at a partial stroke where pressure balances spring and load. Excessive load (a Wall in front): no predicted stroke; the head stops at contact.
- **Boundaries.** Rod never passes the end stops; force never exceeds (p − p_amb)·A; equal pressures give zero force.
- **Run/Reset.** Restores rod pose, chamber inventory (ambient air), spring state and connections exactly.
- **Save/Load.** Pose and hoses round-trip; Run after reload reproduces the outcome.
- **Integrations.** TX-04; TX-05 with TH-18; campaign 75, 76, 86, 143.

## 6. Open questions

1. **Batch N game scale.** Adopt or revise the one scale in section 2 (×¼ specific heat, ×1/16 latent heat, gas ×1, reaction energy and O₂ ×1/1024, h 2.5 W/(m²·K), per-pair series contact conductance from per-part face values, 1 intensity unit = 1 W, and which research-table rows the scale multiplies). It carries two consequences: boiling needs a declared saturation curve (S550) because scaled water latent heat is below R·T_sat, and gas-node time constants run about 4× slower than SI (TH-25). Owner decision.
2. **Gas chamber schedule.** No epic schedules S471 or a pneumatic piston story; both are prerequisites. Owner decision.
3. **Valve gear.** Position-uncovered exhaust versus an explicit valve or a double-acting cylinder. Owner decision.
4. **Condensation in the chamber.** Does steam condense on the cold barrel during the stroke? Owner decision (S551).
