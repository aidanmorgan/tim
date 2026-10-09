# TH-25 · Hot-air balloon — named-identity readiness spec

Story 7.0 named-identity spec ([spec](../../../_bmad-output/implementation-artifacts/spec-7-0-element-implementation-readiness.md)). Citations are `path@a6c914e:Lstart-Lend`. No legacy hot-air balloon exists; the legacy sealed-gas state supplies the gas law and the delivered catalogue balloon supplies scale (section 4). Every design value without a cited source is marked "(proposed: …)" with a one-line justification; the owner may revise it.

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name | TH-25 · Hot-air balloon |
| Type | Thermal · potential element (potential/conditional; source adoption retained) |
| Anchors | [requirements.md#thermal-25](../requirements.md#thermal-25) (sequence-task-459); [named-elements.md#thermal-25](../invest/named-elements.md#thermal-25) |
| Proof owner / research | S588 · [TH-S06 thermal expansion](../../thermal-component-research.md#th-s06) |
| Related identities | **Distinct neighbour of [CAT-003 Balloon](CAT-003-balloon.md)** (Batch D) and [EL-208 Balloon](../invest/named-elements.md#element-208): those are sealed buoyant cargo; this is an open envelope whose lift comes from heated air. Shares the [gas family](../../finite-gas-foundation.md) row with [TH-24](TH-24-gas-expansion-bladder.md). Heat from [TH-02 Fire bowl](TH-02-fire-bowl.md) or [TH-01 Candle](TH-01-candle.md) under the opening. |
| Campaign | Intro 80; practice 81; reuse 99, 149 ([thermal allocation](../requirements.md#thermal-campaign-allocation)) |
| Roadmap story | Unscheduled. Story 12.1 builds buoyancy for CAT-003 only. |
| Status | Not started. No `WorkshopPartKind` member (`engine/gpu/WorkshopPartKind.cs@a6c914e:L3-L3`); no legacy part. |

## 2. Declaration

- **Batch N game scale** (proposed, identical in all 37 TH specs; chains derived against it are in [TH-17](TH-17-kettle.md#batch-n-chains); owner question in section 6). Ambient reservoir 288 K and 101.3 kPa. [Research table](../../thermal-component-research.md) rows: specific heat, latent heat and source power read as listed range × scale (64–4096 J/(kg·K), 8192–2,097,152 J/kg, 0.5–2048 W); temperature (128–2048 K), conductance (2⁻¹⁰–2⁸ W/K), fuel/supply (finite), ignition threshold (400–1200 K) and sensor thresholds read as listed, without scale; expansion coefficient reads 2⁻²⁰–2⁻¹⁴ 1/K after scale, which TH-22 and TH-23 exceed on purpose as a declared exaggeration. Condensed-phase specific heat SI × ¼ and latent heat SI × 1/16, so catalogue-scale bodies change temperature or phase within one Run. Gas R and Cv SI × 1, so γ and pressure stay physical; each condensed phase's enthalpy is referenced so vapour made at T_sat carries m·Cv·T_sat and the scaled latent heat is exactly the heat consumed; because water's scaled latent heat (141 kJ/kg) is below R·T_sat (172 kJ/kg), boiling follows a declared saturation curve, not Clausius–Clapeyron. Every fuel's reaction energy and O₂ demand per kilogram SI × 1/1024 (wax 41,016 J/kg and 3.4/1024 kg O₂/kg; wood and cellulose 15,625 J/kg and 1.2/1024 kg O₂/kg), so a 0.3 kg candle holds about 12 kJ, heat per kilogram of O₂ keeps Thornton's ≈ 13 MJ/kg, and fuel mass is consumed 1:1 so burn-down stays visible. Still-air surface exchange h = 2.5 W/(m²·K) (SI × ¼, so condensed-body time constants C/(hA) stay SI-like while gas-node ones run about 4× slower than SI); forced air h = 2.5 + u (u in m/s). Contact pairs conduct G = G_a·G_b/(G_a + G_b) from each part's face conductance: metal 64, ceramic, wood, ice or soft 16, probe tip 0.5 W/K. Every exchange is clamped per 480 Hz substep to the equalising amount ΔT·C_a·C_b/(C_a + C_b), so small nodes never overshoot. Reactive surfaces carry a thermal-only patch node outside rigid mass (fibre 10 mg, wood 0.5 g; area 0.0025 m²; patch-to-bulk 0.01 W/K; thermal nodes 10⁻⁶–1024 kg, not bound by the 1/1024 kg rigid minimum) that takes strikes, flames and light first. Radiant optical power: 1 legacy intensity unit = 1 W, so the flashlight emits 24 W. Canonical f32 ([numeric contract](../../gpu-f32-physics.md#game-grade-envelope)).
- **Variants.** None named in the row. Initial envelope air temperature is an authored parameter (proposed: see Open question 2 on the power requirement).
- **Bodies and shapes.** One dynamic body: envelope sphere collider radius 0.60 m (proposed: within the 1/16–16 m sphere bound, `engine/gpu/PhysicsDeclarations.cs@a6c914e:L100-L104`; larger than the 0.36 m catalogue balloon because hot-air lift per volume is small) plus a cream cargo platform box, half-extents 0.15 × 0.02 × 0.15 m, 0.75 m below the centre. Opening at the envelope bottom, radius 0.15 m (0.071 m²).
- **Mass and material.** Envelope and platform 0.15 kg (proposed: light fabric, a third of the catalogue balloon's 0.5 kg, `parts/catalog/balloon.tres@a6c914e:L14-L14`). Envelope air: ambient density 1.225 kg/m³ (derived, p/(RT)), 1.11 kg in the 0.905 m³ envelope at 288 K, SI R and Cv (batch scale). Envelope surface loss 2.5 × 4.52 m² = 11.3 W/K (batch scale).
- **Constraints.** None; a rope tether may attach to the platform (rope family).
- **Typed ports.** None as sockets. The opening is a gas exchange aperture to ambient; heat enters through it from a plume region below.
- **Sensors and activation.** None.
- **Work and energy stores.** Envelope gas at ambient pressure: inside mass m = p·V/(R·T) changes by exchange through the opening as T changes (IX-40). Buoyancy = (ρ_amb − ρ_in)·V·g (IX-10), no fixed rise speed. Heat-up of the open envelope costs Q = c_p·p·V/R·ln(T₂/T₁): about 82 kJ to 372 K and about 96 kJ to 388 K (derived, SI c_p 1005 J/(kg·K)). The envelope air's passive time constant m·c_p/(h·A) ≈ 1.11 × 1005 / 11.3 ≈ 99 s is about 4× the SI value (about 25 s at the SI still-air h of 10 W/(m²·K)), because gas c_p stays SI while h is scaled ×¼ (derived).
- **Equilibrium with the batch sources (derived).** A fire bowl (TH-02) under the opening delivers 79 % of its 230 W plume (0.071 m² opening over the 0.09 m² plume) = 181 W; the envelope settles at 288 + 181/11.3 ≈ 304 K, where lift is 1.225 × (1 − 288/304) × 0.905 ≈ 0.06 kg, below the 0.15 kg structure: no ascent. Lifting the structure plus 0.1 kg of cargo (0.25 kg) needs ρ_in = 1.225 − 0.276, so T_in ≈ 372 K, and holding it there needs 11.3 × 84 ≈ 0.95 kW (about 3.8 kW at the SI still-air h of 10 W/(m²·K)), about four times the fire bowl. A candle (75 W plume, 0.2 m wide, fully captured) settles near 295 K.
- **Parameters.**

  | Parameter | Type | Range | Default | Unit | Source |
  | --- | --- | --- | --- | --- | --- |
  | Envelope radius | f32 | fixed | 0.60 | m | (proposed: above) |
  | Structure mass | f32 | fixed | 0.15 | kg | (proposed: above) |
  | Initial air temperature | f32 | 288–450 | 288 | K | (proposed: lets a lesson start with hot air, see Open question 2) |

- **Cosmetic curves and UI bindings.** Envelope ← committed volume ([research per-element table](../../thermal-component-research.md)); a shaped temperature band on the envelope.
- **Art.** "Faceted envelope in established balloon colour with a small cream cargo platform" (row). Balloon colour `#ed6378` (`DESIGN.md@a6c914e:L168-L168`); cream `#ead39b` platform (proposed mapping, `DESIGN.md@a6c914e:L146-L146`).
- **Catalogue and inventory entry.** New `WorkshopPartKind.HotAirBalloon`; id `hot_air_balloon`, title "Hot-air balloon", category Motion (proposed: beside the catalogue Balloon).

## 3. Engine capabilities

Families (map row TH-25, [general-engine-element-map](../general-engine-element-map.md); binding [thermal-01.json](../../coverage/engine/thermal-01.json)): Buoyancy, EnvironmentState, FiniteLedger, FluidAdvection, GasState, GeometryQuery, RigidBodyDynamics, SensibleHeat, ThermalConvection, TopologyTransaction; JSON adds StateTransaction.

- **Exists now.** Dynamic sphere and box bodies, per-body gravity (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L120`); Run/Reset and save (`engine/gpu/ActivationNetwork.cs@a6c914e:L67-L67`, `engine/gpu/WorkshopSaveCodec.cs@a6c914e:L6-L10`).
- **Missing.** Buoyancy and atmosphere: Story 12.1 (CAT-003), [S635](../invest/decisions.md#s635) environment. Open gas store with exchange: [S470](../invest/decisions.md#s470) gas-state and open-versus-sealed → S471, S697. [S543](../invest/decisions.md#s543) convection → S545. Unscheduled.
- **Dependencies.** A heat source under the opening (TH-02, or a source the owner adds per Open question 2); cargo; optional rope tether (Story 10.2).

## 4. Sources and legacy

- **Requirement row** [thermal-25](../requirements.md#thermal-25): envelope geometry, contained gas state and surrounding density produce buoyancy against envelope and cargo mass; exchange through the opening; no variants.
- **Named entry** [thermal-25](../invest/named-elements.md#thermal-25): owner S588; "excess load or cooling prevents ascent; no fixed rising velocity or balloon-specific levitation rule".
- **Research** per-element table ("sealed gas store + buoyancy region"; volume; lifts when its air is heated) and the gas family per-element row.
- **Processes.** [IX-10](../requirements.md#interaction-10), [IX-40](../requirements.md#interaction-40), [IX-19](../requirements.md#interaction-19).
- **Legacy.**

  | # | Fact | Citation | Disposition |
  | --- | --- | --- | --- |
  | 1 | Gas state T = U/(m·Cv), ρ = m/V, p = ρRT. | `engine/physics/SealedGasState.cs@a6c914e:L18-L41` | Carry forward for envelope density. |
  | 2 | Catalogue Balloon: radius 0.36 m, mass 0.5 kg, buoyancy 11.5 (game acceleration), drag 0.4. | `parts/catalog/balloon.tres@a6c914e:L14-L14` | Scale reference only; do not carry forward the fixed buoyancy acceleration (the row forbids a balloon-specific levitation rule). |
  | 3 | Legacy lift law applied buoyancy × pressure × mass every substep on the CPU. | `reference/cpu/MachineWorld.cs@a6c914e:L869-L883` | Do not carry forward: fixed-acceleration CPU loop. |

  Files harvested: `engine/physics/SealedGasState.cs`, `parts/catalog/balloon.tres`, `reference/cpu/MachineWorld.cs` (CAT-003 owns the full harvest of the last two).

## 5. Acceptance outline

Point of truth: [thermal-25](../requirements.md#thermal-25) and [ELEMENT acceptance](../requirements.md#accept-element). The positive case depends on Open question 2.

- **Chrome recipe.** Through the real palette: a heat source under the opening (per Open question 2: an authored hot start at 380 K, or a source of about 1 kW), a 0.1 kg cargo on the platform, and a ledge with a Receiver above.
- **Positive.** With the envelope at or above about 372 K, lift exceeds the 0.25 kg total weight and the balloon rises; no fixed speed; heat in = air enthalpy gain + surface loss.
- **Negative or control.** The fire bowl alone holds the envelope near 304 K (lift ≈ 0.06 kg): it does not rise. A Basketball (1 kg) as cargo: never rises. Heat removed: the envelope cools with its 99 s time constant and the balloon sinks. Without a supporting atmosphere (environment preset): no buoyancy.
- **Boundaries.** Lift = (ρ_amb − ρ_in)·V·g within the envelope; equal temperatures give zero net lift beyond the fabric's own weight.
- **Run/Reset.** Restores envelope air temperature and mass, pose and cargo exactly.
- **Save/Load.** Pose and initial temperature round-trip; Run after reload reproduces the outcome.
- **Integrations.** IX-10 buoyancy shared with CAT-003; campaign 80, 81, 99, 149.

## 6. Open questions

1. **Batch N game scale.** Adopt or revise the one scale in section 2 (×¼ specific heat, ×1/16 latent heat, gas ×1, reaction energy and O₂ ×1/1024, h 2.5 W/(m²·K), per-pair series contact conductance from per-part face values, 1 intensity unit = 1 W, and which research-table rows the scale multiplies). It carries two consequences: boiling needs a declared saturation curve (S550) because scaled water latent heat is below R·T_sat, and gas-node time constants run about 4× slower than SI (TH-25). Owner decision.
2. **Power requirement.** The envelope settles about 16 K above ambient under the 256 W fire bowl (181 W captured against 11.3 W/K of loss) and lifts about 0.06 kg against its 0.15 kg structure. Lifting 0.25 kg needs about 372 K and about 0.95 kW sustained (about 3.8 kW at SI still-air loss), plus about 82 kJ to heat the open envelope (96 kJ to 388 K). Choose one: more power (an integral burner within the research table's 2048 W), a game scale for open-envelope air, or an authored hot start. Owner decision.
3. **Map wording.** The research table says "sealed gas store"; the row requires exchange through an opening. Owner decision (S697).
4. **Gas time constant.** With h scaled ×¼ and gas c_p kept at SI, the envelope air's time constant is about 99 s, about 4× SI, so heating, cooling and the sinking control all run about four times slower than a real balloon. Accept, or give gas nodes their own exchange scale (h × 1 for gas surfaces). Owner decision.
