# EL-039 · Air reservoir — element readiness spec

Story 7.0 Batch H named-identity spec. Legacy citations are pinned to `a6c914e` and resolve with `git show a6c914e:<path> | sed -n 'start,endp'`. Values marked *proposed* have no legacy or requirement source; each carries a one-line justification, stays inside the [game-grade envelope](../../gpu-f32-physics.md#game-grade-envelope) and may be revised by the owner. Box sizes are full extents. This spec also holds the **shared sealed-gas family facts (P1–P20)** that the other pneumatic identities ([EL-037](EL-037-air-compressor.md), [EL-038](EL-038-pneumatic-hose.md), [EL-040](EL-040-pneumatic-release-valve.md), [EL-041](EL-041-pneumatic-directional-valve.md), [EL-042](EL-042-air-nozzle.md), [EL-043](EL-043-pneumatic-pressure-gauge.md)) and the gas-fed [EL-045](EL-045-air-whistle.md) point to instead of repeating.

## 1. Identity

| Item | Value |
| --- | --- |
| Identity / name | EL-039 · Air reservoir |
| Type | Pneumatic |
| Anchor | [requirements.md#element-039](../requirements.md#element-039); scope index [todo-376](../requirements.md#todo-376); [named-elements entry](../invest/named-elements.md#element-039); owner S474 |
| Related | Refines no CAT spec. Related: [CAT-010 Bellows](CAT-010-bellows.md) (open airflow, not a sealed store), [CAT-028 Fan](CAT-028-fan.md) (open airflow). Pneumatic integration task [todo-426](../requirements.md#todo-426) and [todo-427](../requirements.md#todo-427) (spring-return and double-acting pistons, not EL identities). |
| Roadmap story | unscheduled (Epic 12 delivers only open airflow and the bellows) |
| Status | not started |

## 2. Declaration

| Item | Declaration (value · source) |
| --- | --- |
| Bodies and shapes | One static root body. Collider: box 1.1 × 0.7 × 0.7 m centred at (0, 0.2, 0) **proposed** (current collider kinds are sphere, box and plane only, `engine/gpu/PhysicsDeclarations.cs@a6c914e:L7-L7`; a box bounds the cylindrical tank art at Battery/Counter scale). Navy foot box 1.2 × 0.16 × 0.8 m centred at (0, −0.25, 0) **proposed** (same foot proportion as Counter and logic gates). |
| Mass and material | Static. Contact material restitution 1, bounce threshold 0.1 m/s, friction 0.3, the declared static default (`engine/gpu/WorkshopPhysicsCompiler.cs@a6c914e:L109-L110`), through `ContactMaterialDeclaration` (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L42-L45`). Gas material "Air": see P14. |
| Constraints | none — a fixed static body. |
| Typed ports | `GasIn` (Gas, Input) at (−0.62, 0.2, 0) and `GasOut` (Gas, Output) at (0.62, 0.2, 0) **proposed**: both bind the same single gas node, so flow direction follows pressure (P13); the in/out names only fix authoring direction (supply side → consumer side). Domain `Gas` is a new `WorkshopConnectionDomain` value **proposed** (current enum holds Activation and Electrical only, `engine/gpu/WorkshopConnections.cs@a6c914e:L8-L8`). |
| Sensors and activation | none. The reservoir exposes its committed pressure as a read for the dial and for [EL-043](EL-043-pneumatic-pressure-gauge.md). |
| Work and energy stores | One sealed gas store: mass m (kg), volume V (m³), internal energy U (J); T, ρ and p derived, never written (P1). Usable energy leaves only as mass discharge through `GasOut` (P5) or as chamber work (P8). |
| Parameters | `volume`: f32, 0.002–0.05 m³, default 0.01 m³ — the default is the legacy fixture volume (P2); range **proposed** (one fifth to five times the fixture, keeping charge times within a lesson). `initial_pressure`: f32 absolute, 101 325–600 000 Pa, default 400 000 Pa **proposed** (about 4 bar gauge, a familiar shop-air charge; the lower bound equals ambient, i.e. empty). Initial temperature is ambient 293.15 K (P15). `rated_pressure`: fixed 600 000 Pa **proposed** (charging stops at the rating; no relief valve is implied). Parameter keys are a closed enum `AirReservoirParameter { Volume, InitialPressure }`. |
| Cosmetic curves and UI bindings | Front dial needle ← committed absolute pressure, mapped linearly from ambient (needle at rest) to the 600 kPa rating (full scale) **proposed** (matches the gas-family binding "Dial ← committed pressure", [finite-gas-foundation](../../finite-gas-foundation.md#sealed-gas)). The needle eases on the 60 Hz animation worker and cannot change gas state. Selected-part readout shows gauge pressure in kPa. |
| Art | Cylindrical tank r 0.35 m, length 1.1 m, axis X, in cyan `#66b8c9` (air-part catalogue colour shared with Fan and Bellows, [DESIGN.md colour system](../../../DESIGN.md#colour-system)); two cream `#fff8e9` end bands; cream dial face r 0.18 m with navy `#293954` ticks and a gold `#e8b764` needle on the front at z 0.36; navy `#293954` cradle foot; gold `#f7cb52` port spheres r 0.075 at both sockets (port-sphere convention of every supplied part). All geometry **proposed** within the palette. |
| Catalogue / inventory | Id `air_reservoir`, Title "Air tank", Category `Air` **proposed** (a new category that keeps sealed-gas parts apart from the open-airflow Fan and Bellows in Power), Description **proposed**: "Stores a finite charge of compressed air. Each stroke it feeds lowers its pressure; it never refills by itself." No level grants it yet. |

**Variants.** The requirements row names no variant. The base declaration above is the only required mode; `initial_pressure` = ambient (empty) and charged starts are values of one parameter, not separate modes.

### Shared sealed-gas family facts

| # | Fact | Source | Disposition |
| --- | --- | --- | --- |
| P1 | A homogeneous gas inventory is mass (kg), volume (m³) and internal energy (J); temperature T = U/(m·Cv), density ρ = m/V and absolute pressure p = ρ·R·T are derived, never independently writable. | `engine/physics/SealedGasState.cs@a6c914e:L5-L41` | carry forward as f32 declaration and solver state |
| P2 | Legacy fixture: material R 200, Cv 400 J/(kg·K) (γ 1.5); state 0.02 kg, 0.01 m³, 2400 J gives 300 K, 2 kg/m³, 120 000 Pa and p·V = m·R·T. | `CuriousContraptions.tests/SealedGasStateTests.cs@a6c914e:L7-L20` | carry forward as an acceptance identity; the fixture material is a test value, not air (P14) |
| P3 | Reversible adiabatic volume change: U₂ = U₁·(V₁/V₂)^(R/Cv), mass unchanged; work on gas = U₂ − U₁ (positive on compression, negative on expansion); an unrepresentable change rejects. | `engine/physics/SealedGasState.cs@a6c914e:L43-L59` | carry forward the law; replace the reject with clamp-or-continue |
| P4 | Table: from (0.01 m³, 2400 J) to 0.0025 m³ → 4800 J, 600 K, 960 kPa, work 2400 J; to 0.04 m³ → 1200 J, 150 K, 15 kPa, work −1200 J; equal volume → zero work. | `CuriousContraptions.tests/SealedGasStateTests.cs@a6c914e:L22-L41` | carry forward (acceptance table, fixture material) |
| P5 | Finite adiabatic discharge: remaining mass must be positive and no larger than the inventory (discharge cannot increase it); U falls as U·(m₂/m₁)^γ; outlet enthalpy = U lost. | `engine/physics/AdiabaticGasDischarge.cs@a6c914e:L5-L45` | carry forward |
| P6 | Converging nozzle regimes NoFlow, Subsonic, Choked; forward flow only (back pressure ≤ source pressure); zero area or equal pressure gives NoFlow; choked below the critical ratio (2/(γ+1))^(γ/(γ−1)); mass rate = A·ρ·u; thrust = momentum rate + A·(p_exit − p_back). | `engine/physics/ConvergingGasNozzle.cs@a6c914e:L5-L5` and `engine/physics/ConvergingGasNozzle.cs@a6c914e:L39-L87` | carry forward the law; the regime is a typed enum |
| P7 | Finite transit: the jet's kinetic energy can never exceed the discharged outlet enthalpy. | `engine/physics/GasNozzleTransit.cs@a6c914e:L28-L51` | carry forward (no free energy) |
| P8 | Gas bound to a slider chamber drives shared motion: at every step gas U + body kinetic energy stays constant (240 J case); Reset restores gas and motion exactly and replays identically. | `CuriousContraptions.tests/GasChamberWorldTests.cs@a6c914e:L32-L65` | carry forward (piston consumers) |
| P9 | A collision during compression cuts the compression work; both domains restore on Reset. | `CuriousContraptions.tests/GasChamberWorldTests.cs@a6c914e:L67-L81` | carry forward |
| P10 | Equal opposed chambers hold the body still and spend neither inventory. | `CuriousContraptions.tests/GasChamberWorldTests.cs@a6c914e:L116-L133` | carry forward (directional-valve control) |
| P11 | An inventory with no mechanical binding supplies no motion. | `CuriousContraptions.tests/GasChamberWorldTests.cs@a6c914e:L135-L144` | carry forward |
| P12 | Duplicate inventory, unknown node or joint, and mismatched geometry reject atomically; one inventory cannot drive two independent chamber coordinates; bindings cannot change after the run starts. | `CuriousContraptions.tests/GasChamberWorldTests.cs@a6c914e:L83-L101` and `engine/physics/PhysicsWorld.cs@a6c914e:L321-L324` | carry forward as compile-time rejection |
| P13 | A gas node is owned by a body; distinct chambers on one body keep distinct typed identities. | `engine/physics/PhysicsGasNode.cs@a6c914e:L15-L29` | carry forward |
| P14 | Material: calorically perfect single-species ideal gas with explicit temperature, density and pressure limits; γ = 1 + R/Cv must exceed 1; no ambient source implied. | `engine/physics/IdealGasMaterial.cs@a6c914e:L5-L38` | carry forward; "Air" R 287, Cv 718 J/(kg·K) (γ 1.40) **proposed** (standard dry-air constants); limits 200–600 K, ≤ 10 kg/m³, ≤ 800 kPa **proposed** (covers a 600 kPa charge plus adiabatic heating to about 490 K) |
| P15 | Ambient boundary 101 325 Pa, 293.15 K **proposed** (standard atmosphere at room temperature); vents and "empty" readings use it; the legacy declared no ambient sink (P5, P7). | — | proposed |
| P16 | Legacy gas used f64 SI values, exact rejection on unrepresentable changes and a CPU world step. | `engine/physics/SealedGasState.cs@a6c914e:L11-L16`, `engine/physics/SealedGasState.cs@a6c914e:L55-L57` | do not carry forward (canonical f32; clamp-or-continue; worker solver) |
| P17 | Discharge checks on the fixture tank (R 200, Cv 400, 0.01 m³, 0.02 kg, 2400 J): (a) quarter inventory 0.02 → 0.005 kg leaves U = 300 J, T = 150 K, p = 15 kPa at unchanged volume, outlet enthalpy 2100 J, and that enthalpy exceeds discharged mass × Cv × final T; (b) discharge to 0.008 kg matches an independent 10 000-step integration of outlet enthalpy ∫(Cv + R)·T dm within 2 × 10⁻⁶ J for (R, Cv) = (200, 400) and (300, 750), and two steps via 0.014 kg compose to the same result; (c) an unchanged inventory discharges exactly zero mass and zero enthalpy; (d) a 10⁻⁵ m² nozzle's initial enthalpy rate per unit mass flow matches the reservoir's differential debit (10⁻⁸ kg removed) within 0.03 J/kg, and both the mass rate and the enthalpy rate fall once the tank has lost that mass. | `CuriousContraptions.tests/AdiabaticGasDischargeTests.cs@a6c914e:L10-L80` | carry forward as an acceptance check |
| P18 | Chamber geometry V(q) = V₀ + A_signed·(q − q₀) with positive reference volume and non-zero signed area; chamber effort A·p at rest and a finite-interval effort from the adiabatic energy difference; a chamber binds only to its owned slider with an inventory owned by a slider participant. Harvested in detail by [TH-19 Steam piston](TH-19-steam-piston.md) (rows 1–4). | `engine/physics/AxialGasGeometry.cs@a6c914e:L5-L33`, `engine/physics/AxialGasPotential.cs@a6c914e:L36-L73`, `engine/physics/AxialGasLoad.cs@a6c914e:L27-L45` | carry forward (piston consumers of this tank) |
| P19 | Interval work-error acceptance by quadrature for predicted chamber motion. Harvested by [TH-19](TH-19-steam-piston.md) (row 9). | `engine/physics/GasPredictionWork.cs@a6c914e:L7-L54` | do not carry forward (CPU prediction certificate) |
| P20 | Nozzle oracle on the fixture source (120 kPa, 300 K), area 0.01 m², back pressure 30 kPa: choked, exit 61 440 Pa, 240 K, √72 000 ≈ 268.3 m/s; the rate is unchanged at lower back pressure; doubling the area doubles mass and enthalpy rates ([TH-17 Kettle](TH-17-kettle.md) harvest row 6). A zero inventory change has zero transit, thrust and kinetic energy ([EL-092 Toy rocket](EL-092-toy-rocket.md) row L3). | `CuriousContraptions.tests/ConvergingGasNozzleTests.cs@a6c914e:L31-L47`, `CuriousContraptions.tests/GasNozzleTransitTests.cs@a6c914e:L89-L101` | carry forward as acceptance oracles |

## 3. Engine capabilities

Families ([element map row](../general-engine-element-map.md); [binding](../../coverage/engine/element-01.json), proof owner S474): EnvironmentState, FiniteLedger, FluidAdvection, GasState, GeometryQuery, JointConstraint, PressureWork, RigidBodyDynamics, TopologyTransaction (+ StateTransaction in the binding).

**Exists now**
- Static box body, collider and material: `engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L58` (`RigidBodyDeclaration`), `engine/gpu/PhysicsDeclarations.cs@a6c914e:L89-L90` (`ColliderDeclaration`), compiled by `engine/gpu/WorkshopPhysicsCompiler.cs`.
- Typed port/connection storage (activation only admitted): `engine/gpu/WorkshopConnections.cs@a6c914e:L8-L13`, validation `engine/gpu/WorkshopConnections.cs@a6c914e:L83-L99`.

**Missing**
- GasState sealed store, FiniteLedger gas inventory, gas network node — family GasState; decision owner [S470](../invest/decisions.md#s470) gas-state row → S471; open-versus-sealed declarations → S697.
- FluidAdvection between gas nodes (shared inventory, pressure-driven direction) — [S416/S470](../invest/decisions.md#s416) liquid/gas boundary.
- `Gas` connection domain, sockets and contextual wiring UI — [S257](../invest/decisions.md#s257) typed ports.
- Pressure dial cosmetic source (committed scalar) — animation feedback sources are Activation, Timer, ContactWork and Capture only (`engine/gpu/WorkshopCosmetic.cs@a6c914e:L7-L7`).

**Dependencies.** A charge source ([EL-037](EL-037-air-compressor.md), or an authored `initial_pressure`); a consumer to observe depletion: a valve ([EL-040](EL-040-pneumatic-release-valve.md)/[EL-041](EL-041-pneumatic-directional-valve.md)) feeding a piston ([todo-426](../requirements.md#todo-426)) or [EL-042 Air nozzle](EL-042-air-nozzle.md); [EL-038 hose](EL-038-pneumatic-hose.md) to connect them.

## 4. Sources and legacy

- Requirement row [element-039](../requirements.md#element-039): "Stores finite gas mass and internal energy in a declared volume"; outcome "Repeated strokes deplete pressure". No variants listed. Scope index [todo-376](../requirements.md#todo-376) and integration task (bellows charge → reservoir → valve → piston). Campaign coverage: first use 71–80, reuse 91–100 and 136–150 ([campaign-element-coverage](../requirements.md#campaign-element-coverage)).
- Named entry: [element-039](../invest/named-elements.md#element-039), owner S474.
- Gas family design: [finite-gas-foundation](../../finite-gas-foundation.md#sealed-gas) lists the reservoir as "Sealed gas store | capacity, maximum pressure | Stores a bounded charge; gauge shows it | Dial ← committed pressure".
- Profile: [pneumatics](../invest/profiles.md#pneumatics) — depleted pressure moves nothing; closed valve holds; Reset/save exact.
- Legacy: no reservoir part, catalogue entry, level or campaign lesson exists. The only element knowledge is the generic gas law harvested in P1–P20. `parts/`, `content/puzzles.json`, `tools/Campaign` and `reference/` were searched for "pneumat", "compressor", "reservoir" and gas types; `reference/` matches are copies of current declarations or coverage JSON only.
- **Files harvested:** `engine/physics/SealedGasState.cs`, `engine/physics/AdiabaticGasDischarge.cs`, `engine/physics/ConvergingGasNozzle.cs`, `engine/physics/GasNozzleTransit.cs`, `engine/physics/PhysicsGasNode.cs`, `engine/physics/IdealGasMaterial.cs`, `engine/physics/PhysicsWorld.cs` (gas binding lines only), `engine/physics/AxialGasGeometry.cs`, `engine/physics/AxialGasLoad.cs`, `engine/physics/AxialGasPotential.cs`, `engine/physics/GasPredictionWork.cs` (P18–P19; full harvest in [TH-19](TH-19-steam-piston.md)), `CuriousContraptions.tests/SealedGasStateTests.cs`, `CuriousContraptions.tests/GasChamberWorldTests.cs`, `CuriousContraptions.tests/AdiabaticGasDischargeTests.cs` (P17), `CuriousContraptions.tests/ConvergingGasNozzleTests.cs` (P20; full harvest in [TH-17](TH-17-kettle.md) row 6), `CuriousContraptions.tests/GasNozzleTransitTests.cs` (P20; full harvest in [EL-092](EL-092-toy-rocket.md)).

## 5. Acceptance outline

Acceptance authority: [element-039](../requirements.md#element-039) and the [pneumatics profile](../invest/profiles.md#pneumatics).

- **Construction (actual Chrome UI).** Place Air tank from the Air drawer; set `initial_pressure` through its contextual configuration control; place a release valve and a spring-return piston; select the tank, Connect `GasOut`, click the valve and choose `GasOut → GasIn`; connect valve → piston the same way. No numeric placement menu, setter or imported solution.
- **Positive.** Each valve opening extends the piston once; the dial falls after every stroke; a charged tank drives at least two strokes.
- **Negative / control.** Repeated strokes deplete the tank until the piston no longer reaches the load (outcome sentence); an ambient-pressure tank moves nothing; a tank with no hose moves nothing (P11).
- **Boundaries.** `initial_pressure` at ambient and at 600 kPa; `volume` at 0.002 and 0.05 m³ admit; values outside reject atomically at the configuration boundary; a non-finite or out-of-limit state is clamp-or-continue in the solver, never a tick fault. Discharge accounting follows the P17 checks (mass and energy debited only by what leaves; zero change discharges nothing).
- **Run/Reset.** Reset restores the authored charge, temperature and piston pose exactly; replay is identical (P8).
- **Save/Load.** `volume`, `initial_pressure` and both gas connections round-trip with exact socket names; runtime pressure is not persisted.
- **Integrations.** Bellows/compressor charging (todo-376 integration), gauge reading ([EL-043](EL-043-pneumatic-pressure-gauge.md)).

## 6. Open questions

1. **Game volume versus art volume.** The default 0.01 m³ is the legacy fixture volume, far smaller than the 0.42 m³ tank art. Whether the declared volume should follow the art or stay a tuned game value: unspecified — owner decision.
2. **Gas connection direction.** The current port model has only Input and Output; a reservoir both fills and drains. Two same-node sockets (proposed) versus a bidirectional gas port: owner decision under S257/S697.
3. **Thermal coupling.** Adiabatic compression heats the charge (about 490 K at 600 kPa); whether the tank cools toward ambient (needs S543 conduction) or stays adiabatic: owner decision.
4. **Category.** A new `Air` drawer category versus reusing Power: owner decision.
