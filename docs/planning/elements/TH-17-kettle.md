# TH-17 · Kettle — named-identity readiness spec

Story 7.0 named-identity spec ([spec](../../../_bmad-output/implementation-artifacts/spec-7-0-element-implementation-readiness.md)). Citations are `path@a6c914e:Lstart-Lend`. No legacy kettle exists; the legacy sealed-gas foundation supplies laws and test oracles (section 4). Every design value without a cited source is marked "(proposed: …)" with a one-line justification; the owner may revise it. This spec also holds the [Batch N chain derivations](#batch-n-chains) for the shared game scale.

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name | TH-17 · Kettle |
| Type | Thermal · potential element (potential/conditional; source adoption retained) |
| Anchors | [requirements.md#thermal-17](../requirements.md#thermal-17) (sequence-task-451); [named-elements.md#thermal-17](../invest/named-elements.md#thermal-17) |
| Proof owner / research | S581 · [TH-S02 phase change](../../thermal-component-research.md#th-s02), [TH-S07](../../thermal-component-research.md#th-s07) |
| Related identities | No CAT spec. Sibling vessel [TH-35 Coffee-pot steam vessel](TH-35-coffee-pot-steam-vessel.md) (distinct geometry). Heat from [TH-04](TH-04-electrical-heating-plate.md), [TH-01](TH-01-candle.md), [TH-02](TH-02-fire-bowl.md), [TH-26](TH-26-thermal-storage-block.md) (TX-01 swap). Vapour consumers [TH-19](TH-19-steam-piston.md), [TH-20](TH-20-steam-turbine.md), [TH-18](TH-18-condenser.md). |
| Campaign | Intro 70; practice 71; reuse 86, 140 ([thermal allocation](../requirements.md#thermal-campaign-allocation)) |
| Roadmap story | Unscheduled. No epic in `_bmad-output/planning-artifacts/epics.md` schedules a thermal element or the sealed gas store. |
| Status | Not started. No `WorkshopPartKind` member (`engine/gpu/WorkshopPartKind.cs@a6c914e:L3-L3`); no legacy part. |

## 2. Declaration

- **Batch N game scale** (proposed, identical in all 37 TH specs; chains derived against it are in [TH-17](TH-17-kettle.md#batch-n-chains); owner question in section 6). Ambient reservoir 288 K and 101.3 kPa. [Research table](../../thermal-component-research.md) rows: specific heat, latent heat and source power read as listed range × scale (64–4096 J/(kg·K), 8192–2,097,152 J/kg, 0.5–2048 W); temperature (128–2048 K), conductance (2⁻¹⁰–2⁸ W/K), fuel/supply (finite), ignition threshold (400–1200 K) and sensor thresholds read as listed, without scale; expansion coefficient reads 2⁻²⁰–2⁻¹⁴ 1/K after scale, which TH-22 and TH-23 exceed on purpose as a declared exaggeration. Condensed-phase specific heat SI × ¼ and latent heat SI × 1/16, so catalogue-scale bodies change temperature or phase within one Run. Gas R and Cv SI × 1, so γ and pressure stay physical; each condensed phase's enthalpy is referenced so vapour made at T_sat carries m·Cv·T_sat and the scaled latent heat is exactly the heat consumed; because water's scaled latent heat (141 kJ/kg) is below R·T_sat (172 kJ/kg), boiling follows a declared saturation curve, not Clausius–Clapeyron. Every fuel's reaction energy and O₂ demand per kilogram SI × 1/1024 (wax 41,016 J/kg and 3.4/1024 kg O₂/kg; wood and cellulose 15,625 J/kg and 1.2/1024 kg O₂/kg), so a 0.3 kg candle holds about 12 kJ, heat per kilogram of O₂ keeps Thornton's ≈ 13 MJ/kg, and fuel mass is consumed 1:1 so burn-down stays visible. Still-air surface exchange h = 2.5 W/(m²·K) (SI × ¼, so condensed-body time constants C/(hA) stay SI-like while gas-node ones run about 4× slower than SI); forced air h = 2.5 + u (u in m/s). Contact pairs conduct G = G_a·G_b/(G_a + G_b) from each part's face conductance: metal 64, ceramic, wood, ice or soft 16, probe tip 0.5 W/K. Every exchange is clamped per 480 Hz substep to the equalising amount ΔT·C_a·C_b/(C_a + C_b), so small nodes never overshoot. Reactive surfaces carry a thermal-only patch node outside rigid mass (fibre 10 mg, wood 0.5 g; area 0.0025 m²; patch-to-bulk 0.01 W/K; thermal nodes 10⁻⁶–1024 kg, not bound by the 1/1024 kg rigid minimum) that takes strikes, flames and light first. Radiant optical power: 1 legacy intensity unit = 1 W, so the flashlight emits 24 W. Canonical f32 ([numeric contract](../../gpu-f32-physics.md#game-grade-envelope)).
- **Variants.** None named in the row.
- **Bodies and shapes.** One dynamic body: box collider, half-extents 0.10 × 0.10 × 0.10 m (proposed: a 0.2 m toy kettle that fits inside a candle's 0.2 m plume and sits on the 0.6 m plate). Gold spout outlet at +X, 0.15 m above the base.
- **Mass and material.** Empty vessel 0.2 kg (proposed: a light toy vessel); rigid mass adds the water charge. Wall node 0.1 kg steel, c = 122 J/(kg·K) (batch scale; 12.2 J/K). Water under the batch scale: liquid c = 1046 J/(kg·K), vaporisation 141,250 J/kg at 373 K and 101.3 kPa. Base: metal face 64 W/K (batch scale), so the plate pair is 32 W/K and a ceramic block pair 12.8 W/K. Ambient exchange 2.5 × 0.2 m² = 0.5 W/K (batch scale). Contact friction 0.5, restitution 0.05 (proposed: matte ceramic-steel).
- **Constraints.** None.
- **Typed ports.** One gas outlet port at the spout (proposed: the gas family's typed outlet, connectable to a hose, [EL-038](../invest/named-elements.md#element-038)); unconnected, it vents to ambient. Heat enters through the base contact or a plume.
- **Sensors and activation.** None.
- **Work and energy stores.** Liquid charge 0–0.04 kg, default 0.02 kg (proposed: the cheapest useful boil fits every TX-01 source's budget). Head space: sealed gas store of 0.002 m³ (proposed: small, so heating its air costs little), initially air at 288 K and 101.3 kPa: 2.45 g, 1.76 J/K at SI Cv (derived). Specified vapour output: 2 g delivered through the spout (proposed: enough to move a receiver or one piston stroke). Boiling at 373 K at 101.3 kPa, rising with head pressure along the declared saturation curve (S550). Spout: converging nozzle, throat 1 × 10⁻⁴ m² (proposed: a visible jet; its choked capacity far exceeds the boil rate, so head pressure stays near ambient while venting). Relief: above 400 kPa absolute the lid lifts and vents (proposed: below the legacy 10 MPa fixture limit and a safe toy pressure). Budget: total heat capacity 34.9 J/K needs 2,966 J to reach 373 K; 2 g of vapour then costs 283 J; full boil-off of 0.02 kg costs 2,825 J.
- **Parameters.**

  | Parameter | Type | Range | Default | Unit | Source |
  | --- | --- | --- | --- | --- | --- |
  | Water charge | f32 | 0–0.04 | 0.02 | kg | (proposed: above) |
  | Head-space volume | f32 | fixed | 0.002 | m³ | (proposed: above) |
  | Spout throat area | f32 | fixed | 1 × 10⁻⁴ | m² | (proposed: above) |
  | Relief pressure | f32 | fixed | 400 | kPa | (proposed: above) |

- **Cosmetic curves and UI bindings.** Lid rattle ← committed head pressure ([research per-element table](../../thermal-component-research.md)); cyan water gauge ← committed liquid mass (row). Water vapour is never shown as a white condensed plume ([visual contract](../requirements.md#thermal-elements)).
- **Art.** "Rounded cream vessel, cyan water gauge and clearly visible gold outlet" (row). Nearest [DESIGN.md](../../../DESIGN.md) source colours (proposed mapping): cream `#ead39b` (`DESIGN.md@a6c914e:L146-L146`), cyan `#66b8c9` (`DESIGN.md@a6c914e:L172-L172`), gold `#f7cb52` (`DESIGN.md@a6c914e:L154-L154`).
- **Catalogue and inventory entry.** New `WorkshopPartKind.Kettle`; id `kettle`, title "Kettle", category Heat (proposed: no current category covers thermal parts).

<a id="batch-n-chains"></a>
### Batch N chains (derived against the game scale)

All values follow from the batch scale and the proposed part values in each linked spec.

| Chain | Derivation | Result |
| --- | --- | --- |
| Striker → tinder ([TH-31](TH-31-flint-striker.md), [TH-32](TH-32-tinder-pad.md)) | Basketball (1 kg, radius 0.34 m, solid-sphere inertia, `engine/gpu/RigidMassProperties.cs@a6c914e:L34-L37`) released 1.0 m up a Ramp arrives with about 9 J (about 6.5 J translational). The first impact passes at most about 1.1 J to the arm (product restitution 0.55 × 0.1, arm I ≈ 0.0167 kg·m²), so the striker relies on sustained contact: the ball keeps pushing the arm through the 20° engagement arc, 0.175 m of tip travel against the sprung flint plate (N = 28 N, μ = 0.8). Friction work μ·N·s ≈ 3.9 J, plus about 0.6 J to lift the arm and wind its spring until the tip rises above the ball's 0.68 m top at about 71°, all drawn from the ball's 9 J. Deposit 0.5 × 3.9 ≈ 2.0 J (cap 4 J) into the 0.00425 J/K fibre patch; 450 K needs 0.69 J. | Patch ≈ 750 K; ignites (2.8× margin). A 0.1 m release (about 0.9 J) stalls the arm inside the arc: deposit ≤ 0.45 J, patch ≤ about 395 K, no ignition. |
| Striker → candle wick ([TH-01](TH-01-candle.md)) | Same 2.0 J deposit into the wick patch; 500 K needs 0.90 J. | Lights directly (2.2× margin). |
| Tinder → fire-bowl bed ([TH-02](TH-02-fire-bowl.md)) | Wood patch 0.5 g × 425 = 0.21 J/K; 573 K needs 60 J. A 2 J strike adds 9 K. Burning tinder's 16 W flame on the bed patch. | Strike alone fails; tinder lights the bed in about 4 s (tinder burns 49 s). |
| Match ([TH-33](TH-33-spring-mounted-match.md)) → tinder | Spring 400 N/m × 0.15 m preload stores 4.5 J; the 15° pad takes about 7 % as impact, leaving about 4.19 J. The sprung strike pad (N = 100 N, μ = 0.8) decelerates the shaft axially at N·(sin 15° + μ·cos 15°) ≈ 103 N, stopping it within about 0.04 m; friction takes μ·cos 15°/(sin 15° + μ·cos 15°) ≈ 0.75 of that energy (about 3.1 J) and the pad spring is pushed back by the other 0.25 (about 1.0 J, stored elastically). Deposit 0.5 × 3.1 ≈ 1.6 J into the 0.00425 J/K tip patch. Tip 0.5 g × 15,625 = 7.8 J flare, shaft 0.01 kg × 15,625 = 156 J at 8 W. | Tip ≈ 660 K ≥ 450 K (2.3× margin on the 0.69 J needed); its flame lights tinder in about 0.2 s. |
| Candle → kettle (TX-01) | Candle 0.3 kg × 41,016 = 12,305 J at 80 W (154 s); plume 75 W; the 0.2 m kettle covers the 0.2 m plume, so 75 W in. Kettle 34.9 J/K, loss 0.5 W/K: ΔT∞ 150 K, time constant 70 s; 373 K after 70 × ln(150/65) ≈ 59 s; then 75 − 42.5 = 32.5 W net. | First 2 g of vapour after ≈ 67 s; uses 5.4 kJ of the 12.3 kJ fuel. |
| Fire bowl → kettle (TX-01) | Bed 1.5 kg × 15,625 = 23,438 J at 256 W (92 s); plume 230 W over 0.09 m²; kettle covers 0.04 m² (44 %), so 102 W in; ΔT∞ 204 K; 373 K after ≈ 38 s; then 59.5 W net. | First 2 g after ≈ 43 s, inside the 92 s burn. |
| Heating plate → kettle (TX-01) | Plate 512 W, disc 61 J/K, ambient 0.9 W/K; pair 32 W/K. With the kettle at 373 K the disc settles at 386 K and delivers 32 × 13 ≈ 423 W. Plate warm-up (61 × 98 ≈ 6.0 kJ) + kettle (2,966 J) ≈ 9.0 kJ at about 450 W net. | 373 K after ≈ 20 s; 2 g after 0.7 s more; full 20 g boil-off in 7.4 s. |
| Stored-hot block → kettle (TX-01) | TH-26 block 2 kg × 210 = 420 J/K authored at 400 K; pair 12.8 W/K; the block also loses 3.2 W/K to the room. Kettle reaches 373 K after ≈ 5 s with the block near 389 K; boil at 12.8 × 16 − 42.5 ≈ 157 W net, falling as the block cools at about 1.2 K/s. | First 2 g after ≈ 7 s; boiling stops after about 6 g, when the block nears 376 K. |
| Solar → absorber ([TH-07](TH-07-solar-absorber-plate.md)) | Flashlight 24 W, 15° cone, 0.536 m wide at 1 m. The 0.30 m face intercepts 40 %: 9.6 W, absorbed 9.1 W; plate 22.5 J/K, loss 0.225 W/K. | 0.40 K/s toward 328 K; 300 K after ≈ 35 s. |
| Solar → lens → absorber ([TH-06](TH-06-converging-lens.md)) | Lens 1.5 m from the source collects the whole cone (0.40 m radius < 0.65 m aperture); image at 3.0 m; 22.8 W transmitted, 21.7 W absorbed. | 0.96 K/s toward 384 K; 300 K after ≈ 13 s. |
| Lens → tinder | 22.8 W in the 0.13 m spot ≈ 1,700 W/m²; patch absorbs 0.9 × 1,700 × 0.0025 ≈ 3.9 W against 0.01 W/K. 1.0 m off focus: about 400 W/m², patch ≈ 0.9 W. | In focus: ignites in under a second; off focus: peaks near 378 K, no ignition. |
| Heat pump → freezing mold (TX-02, [TH-12](TH-12-reversible-heat-pump.md), [TH-13](TH-13-freezing-mold.md)) | Mold charge 784 J sensible + 1,044 J latent + 1,013 J tray ≈ 2.8 kJ; pump 24 W, COP ≈ 3 at 268/312 K, Q_c ≈ 73 W at first, falling as the cold face drops below the freezing water, while the mold gains about 10 W from the room; Q_h ≈ 100 W into a 5 W/K sink. | Frozen after ≈ 50 s. A 255 K Cold pack freezes about one charge: the first in about 26 s, leaving the pack near 265 K; immediately replacing the mold with a fresh tray and water charge at 288 K brings the water to 273 K with the pack near 270 K, but the solid fraction peaks near 0.13 and melts again. |
| Stability | Largest exchange G·dt/C in the batch: TH-22 strip on the plate 32/480/4.75 ≈ 0.014; fibre patch 0.01/480/0.00425 ≈ 0.005; plate–kettle 32/480/34.9 ≈ 0.002; probe 0.5/480/1.9 ≈ 0.0005. | All far below 1, and the equalising clamp bounds any exchange regardless. |

## 3. Engine capabilities

Families (map row TH-17, [general-engine-element-map](../general-engine-element-map.md); binding [thermal-01.json](../../coverage/engine/thermal-01.json)): EnvironmentState, FiniteLedger, GasState, GeometryQuery, JointConstraint, LiquidBoiling, PhaseTopology, PressureWork, RigidBodyDynamics, SensibleHeat, TopologyTransaction; JSON adds StateTransaction.

- **Exists now.** Dynamic box body (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L120`); Run/Reset and save (`engine/gpu/ActivationNetwork.cs@a6c914e:L67-L67`, `engine/gpu/WorkshopSaveCodec.cs@a6c914e:L6-L10`).
- **Missing.** Sealed gas store and nozzle: [S470](../invest/decisions.md#s470) gas-state → S471 (no epic story; Epic 12 builds airflow only). Liquid inventory: [S416](../invest/decisions.md#s416) → S418. [S543](../invest/decisions.md#s543): boiling → S550, conduction → S544, phase-topology → S565. PressureWork → S419/S471. All unscheduled.
- **Dependencies.** A heat source (TH-04, TH-01, TH-02, TH-26); a vapour consumer for TX-01/TX-04.

## 4. Sources and legacy

- **Requirement row** [thermal-17](../requirements.md#thermal-17): finite fluid vessel accepts generic heat; explicit vapour outlet and pressure boundaries; vapour from enthalpy and phase state; no variants.
- **Named entry** [thermal-17](../invest/named-elements.md#thermal-17): owner S581; "an empty vessel emits no water vapor; warm water requires further sensible/latent input before the specified vapor output".
- **Research** per-element table: "node + sealed gas store + nozzle"; parameter capacity; boils to steam pressure; lid rattle ← pressure. [Gas family](../../finite-gas-foundation.md) sealed-store and nozzle laws.
- **Processes and scenarios.** [IX-24](../requirements.md#interaction-24), [IX-40](../requirements.md#interaction-40), [IX-08](../requirements.md#interaction-08), [IX-41](../requirements.md#interaction-41); [TX-01](../requirements.md#thermal-scenario-01), [TX-04](../requirements.md#thermal-scenario-04).
- **Legacy harvest** (sealed-gas foundation; no part consumed it).

  | # | Fact | Citation | Disposition |
  | --- | --- | --- | --- |
  | 1 | Gas state derives T = U/(m·Cv), ρ = m/V, p = ρRT; never independently writable. | `engine/physics/SealedGasState.cs@a6c914e:L18-L41` | Carry forward as the head-space law; f32, not f64. |
  | 2 | Oracle: R 200, Cv 400, m 0.02 kg, V 0.01 m³, U 2400 J → 300 K, 2 kg/m³, 120 kPa, γ 1.5. | `CuriousContraptions.tests/SealedGasStateTests.cs@a6c914e:L7-L20` | Carry forward as an analytic test oracle (synthetic material). |
  | 3 | Material is single-species, calorically perfect; no phase transition. | `engine/physics/IdealGasMaterial.cs@a6c914e:L5-L38` | Carry forward fields; boiling is new (S550). |
  | 4 | Out-of-envelope states (U 799 or 8001 J in the oracle) threw exceptions. | `CuriousContraptions.tests/SealedGasStateTests.cs@a6c914e:L97-L112` | Do not carry forward a runtime throw (game-grade clamp-or-continue); keep as compile-time admission. |
  | 5 | Converging nozzle: NoFlow, Subsonic, Choked; critical ratio (2/(γ+1))^(γ/(γ−1)); forward flow only. | `engine/physics/ConvergingGasNozzle.cs@a6c914e:L39-L87` | Carry forward the law for the spout. |
  | 6 | Oracle: back 30 kPa → choked, exit 61,440 Pa, 240 K, √72000 m/s; rate independent of lower back pressure; double area doubles rate. | `CuriousContraptions.tests/ConvergingGasNozzleTests.cs@a6c914e:L31-L47` | Carry forward as oracle. |
  | 7 | Closed area or balanced pressure gives no flux. | `CuriousContraptions.tests/ConvergingGasNozzleTests.cs@a6c914e:L64-L74` | Carry forward: blocked spout emits nothing. |
  | 8 | Adiabatic discharge U₂ = U₁(m₂/m₁)^γ; outlet enthalpy = ΔU; no heat input included. | `engine/physics/AdiabaticGasDischarge.cs@a6c914e:L21-L45` | Carry forward for venting; heat input is new. |
  | 9 | Transit quadrature with non-rigorous adaptive Simpson estimates and evaluation budgets. | `engine/physics/GasNozzleTransit.cs@a6c914e:L5-L6` | Do not carry forward: proof-grade CPU integration. |

  Files harvested: `engine/physics/SealedGasState.cs`, `engine/physics/IdealGasMaterial.cs`, `engine/physics/ConvergingGasNozzle.cs`, `engine/physics/AdiabaticGasDischarge.cs`, `engine/physics/GasNozzleTransit.cs`, `CuriousContraptions.tests/SealedGasStateTests.cs`, `CuriousContraptions.tests/ConvergingGasNozzleTests.cs`.

## 5. Acceptance outline

Point of truth: [thermal-17](../requirements.md#thermal-17), [TX-01](../requirements.md#thermal-scenario-01) and [ELEMENT acceptance](../requirements.md#accept-element).

- **Chrome recipe.** Through the real palette and sockets: Battery → Switch → Heating plate (TH-04), a filled Kettle (0.02 kg) on it, spout aimed at a Heat-sensitive target (TH-37) or hosed to a Steam piston (TH-19).
- **Positive.** Water reaches 373 K after about 20 s, then boils; the first 2 g leave the spout about a second later; liquid lost = vapour emitted; plate work = sensible + latent + vented enthalpy + losses. The candle, fire-bowl and stored-hot-block swaps follow the [chains](#batch-n-chains) (about 67 s, 43 s and 7 s) with unchanged water logic.
- **Negative or control.** Empty kettle: no vapour. Warm water (authored 350 K) still needs sensible then latent input before output. Spout blocked: no flow; pressure and boiling point rise until relief at 400 kPa.
- **Boundaries.** Liquid mass never below 0; no vapour below the local boiling point; relief opens only above 400 kPa.
- **Run/Reset.** Restores liquid charge, 288 K, head-space air inventory and lid pose exactly.
- **Save/Load.** Pose, charge and hose connection round-trip; Run after reload reproduces the outcome.
- **Integrations.** TX-01 source swap (candle, fire bowl, plate, stored-hot block); TX-04; campaign 70, 71, 86, 140.

## 6. Open questions

1. **Batch N game scale.** Adopt or revise the one scale in section 2 (×¼ specific heat, ×1/16 latent heat, gas ×1, reaction energy and O₂ ×1/1024, h 2.5 W/(m²·K), per-pair series contact conductance from per-part face values, 1 intensity unit = 1 W, and which research-table rows the scale multiplies). It carries two consequences: boiling needs a declared saturation curve (S550) because scaled water latent heat is below R·T_sat, and gas-node time constants run about 4× slower than SI (TH-25). Owner decision.
2. **Pacing.** Under the scale the candle needs about 67 s to deliver the first 2 g of vapour, the plate about 20 s. Accept the slow-candle contrast, or tune powers. Owner decision.
3. **Gas store schedule.** No epic schedules S471; the kettle cannot start before it. Owner decision.
4. **Enthalpy reference.** The scale fixes the condensed-phase enthalpy offset so boiling conserves energy with SI gas Cv; confirm the reference with S550. Owner decision.
5. **Mixture.** Air plus steam in the head space requires a two-species gas store; the legacy store is single-species. Owner decision (S471).
6. **Saturation curve.** Water's scaled latent heat (141,250 J/kg) is below R·T_sat (461.5 × 373 ≈ 172 kJ/kg). Clausius–Clapeyron with the scaled L (L/R ≈ 306 K) gives no finite boiling point above about 230 kPa, below the 400 kPa relief, so S550 needs a declared saturation curve, for example the SI steam curve, instead. Owner decision (S550).
