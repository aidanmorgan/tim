# EL-042 · Air nozzle — element readiness spec

Story 7.0 Batch H named-identity spec. Legacy citations are pinned to `a6c914e` and resolve with `git show a6c914e:<path> | sed -n 'start,endp'`. Values marked *proposed* have no legacy or requirement source; each carries a one-line justification, stays inside the [game-grade envelope](../../gpu-f32-physics.md#game-grade-envelope) and may be revised by the owner. Box sizes are full extents. Shared sealed-gas facts P1–P20, the `Gas` domain and the "Air" material are in [EL-039](EL-039-air-reservoir.md#shared-sealed-gas-family-facts).

## 1. Identity

| Item | Value |
| --- | --- |
| Identity / name | EL-042 · Air nozzle |
| Type | Pneumatic |
| Anchor | [requirements.md#element-042](../requirements.md#element-042); scope index [todo-376](../requirements.md#todo-376); [named-elements entry](../invest/named-elements.md#element-042); owner S477 |
| Related | Refines no CAT spec. The sealed-gas counterpart of the open-airflow emitters [CAT-028 Fan](CAT-028-fan.md) and [CAT-010 Bellows](CAT-010-bellows.md) (whose spec notes EL-042 is distinct); fed by [EL-039](EL-039-air-reservoir.md) via [EL-040](EL-040-pneumatic-release-valve.md) and [EL-038](EL-038-pneumatic-hose.md); can drive [CAT-070 Windmill](../requirements.md#current-cat-070), [CAT-069 Wind chimes](CAT-069-wind_chimes.md) or [EL-045 Air whistle](EL-045-air-whistle.md). The same nozzle law serves the [TH-17 Kettle](TH-17-kettle.md) spout and the [EL-092 Toy rocket](EL-092-toy-rocket.md). |
| Roadmap story | unscheduled |
| Status | not started |

## 2. Declaration

| Item | Declaration (value · source) |
| --- | --- |
| Bodies and shapes | Static root body: collider box 0.6 × 0.35 × 0.35 m at the origin **proposed** (a small fitting sized like the Bellows' 0.45 × 0.19 × 0.24 m nozzle box, scaled up for picking); jet exit at local (0.38, 0, 0), direction local +X **proposed**. |
| Mass and material | Static; restitution 1, bounce threshold 0.1 m/s, friction 0.3 (`engine/gpu/WorkshopPhysicsCompiler.cs@a6c914e:L109-L110`). A static mount takes the jet reaction; no recoil. |
| Constraints | none. |
| Typed ports | `GasIn` (Gas, Input) at (−0.35, 0, 0) **proposed**. No electrical or activation port: the nozzle is passive; control is upstream (EL-040). |
| Sensors and activation | none. |
| Work and energy stores | No store. Converging nozzle (EL-039 P6, oracle P20): mass rate, exit pressure/temperature/speed and thrust T = momentum rate + pressure force from the upstream node's committed state; NoFlow when upstream ≤ ambient. The jet's kinetic energy never exceeds the discharged enthalpy (P7). The jet enters the open-airflow family as a finite emitter whose source debits the upstream inventory ([finite-gas-foundation](../../finite-gas-foundation.md#airflow-transfer) linear transfer); the force rating seen by a receiver at axial distance d from the exit is T·(1 − d/`reach`)² for d ≤ `reach` and 0 beyond **proposed** (the jet spreads, so its momentum per unit area falls with distance; the square reaches zero smoothly at `reach`). |
| Parameters | `throat_area`: f32, 1 × 10⁻⁵ – 2 × 10⁻⁴ m², default 5 × 10⁻⁵ m² **proposed** (a 400 kPa supply then gives 20.3 N peak thrust, the order of the Bellows' 18 N default, and empties a default tank in about a second; derivation below). `reach`: f32, 0.5–8 m, default 4 m **proposed** (inside the Fan/Bellows 0–12 m bound, `parts/FanPart.cs@a6c914e:L28-L29`; a jet is shorter than a fan field). `width`: f32 jet radius, 0.1–1 m, default 0.4 m **proposed** (narrower than the 0.85 m fan/bellows default, yet wide enough to cover a 0.34 m-radius Basketball). |
| Cosmetic curves and UI bindings | Exhaust puff ← committed regime and mass rate (gas-family binding "Exhaust puff ← committed regime"); puff opacity scales with mass rate, none at NoFlow **proposed**. A selected-part readout shows regime (NoFlow, Subsonic, Choked). |
| Art | Navy `#293954` mount block, gold `#f7cb52` converging cone with navy mouth ring (the Bellows' gold nozzle and navy mouth, [DESIGN.md Bellows](../../../DESIGN.md#bellows)), cream `#fff8e9` collar, gold port sphere r 0.075. Geometry **proposed**. |
| Catalogue / inventory | Id `air_nozzle`, Title "Air nozzle", Category `Air` **proposed**. Description **proposed**: "Turns stored air into a short directed jet. Weak pressure only reaches nearby objects." |

**Peak thrust derivation** (Air γ 1.40, R 287 J/(kg·K), Cp 1005 J/(kg·K), stagnation 293.15 K, back pressure 101 325 Pa, `throat_area` 5 × 10⁻⁵ m²; P6 law):

| Upstream | Regime | Exit state | Mass rate | Thrust T |
| --- | --- | --- | --- | --- |
| 400 kPa | Choked (critical upstream 191.8 kPa) | 211.3 kPa, 244.3 K, 313.3 m/s | 0.0472 kg/s | 14.79 N momentum + 5.50 N pressure = 20.3 N |
| 150 kPa | Subsonic | 101.3 kPa, 262.1 K, 249.9 m/s, 1.347 kg/m³ | 0.0168 kg/s | 4.21 N momentum + 0 pressure = 4.2 N |

Force at the ball with the distance factor (1 − d/4 m)², compared with the Basketball's rolling resistance C_rr·m·g = 0.035 × 1 kg × 9.81 m/s² = 0.34 N (`engine/gpu/WorkshopConstruction.cs@a6c914e:L50-L50`, gravity `engine/gpu/WorkshopConstruction.cs@a6c914e:L123-L123`):

| Ball distance d | Factor | 400 kPa | 150 kPa |
| --- | --- | --- | --- |
| 1 m | 0.5625 | 11.4 N — moves | 2.4 N — moves |
| 3 m | 0.0625 | 1.27 N — moves | 0.26 N — stays (< 0.34 N) |
| ≥ 4 m | 0 | 0 — stays | 0 — stays |

These are peak values at valve opening; thrust only falls as the tank drains (P17 d), so later forces are lower still.

**Variants.** The requirements row names no variant; the base declaration is the only required mode.

## 3. Engine capabilities

Families ([element map row](../general-engine-element-map.md); [binding](../../coverage/engine/element-01.json), proof owner S477): EnvironmentState, FiniteLedger, FluidAdvection, GasState, GeometryQuery, JointConstraint, PressureWork, RigidBodyDynamics, TopologyTransaction (+ StateTransaction).

**Exists now**
- Static box body/collider: `engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L58`, `engine/gpu/PhysicsDeclarations.cs@a6c914e:L89-L90`.
- Dynamic sphere targets with drag and rolling resistance (Basketball, Bowling ball): `engine/gpu/WorkshopConstruction.cs@a6c914e:L50-L51`.

**Missing**
- Gas domain, GasState, nozzle source — [EL-039 §3](EL-039-air-reservoir.md#3-engine-capabilities); owners [S470](../invest/decisions.md#s470) (S471 gas-state, S697 open versus sealed), [S416](../invest/decisions.md#s416).
- Open-airflow jet field, receivers, occlusion and finite source allowance (AerodynamicDrag force region) — Story 12.2 (CAT-028 Fan); bellows nozzle source Story 12.4.
- Coupling of a sealed-gas nozzle to the airflow emitter (thrust → force rating with the distance factor, inventory debit) — first pneumatic story, under S697.

**Dependencies.** Fan airflow capability (Story 12.2); EL-039/EL-040/EL-038 upstream; a Basketball or Tennis ball target.

## 4. Sources and legacy

- Requirement row [element-042](../requirements.md#element-042): "Expands supplied gas into a bounded directed jet"; outcome "Insufficient pressure cannot move a distant load". No variants listed.
- Named entry [element-042](../invest/named-elements.md#element-042), owner S477. Gas design: "Nozzle source | throat area, direction | Converts stored pressure into a jet that moves cargo | Exhaust puff ← committed regime"; nozzle law row "NoFlow, Subsonic and Choked regimes with mass continuity, stagnation enthalpy, momentum and pressure thrust".

| # | Fact | Source | Disposition |
| --- | --- | --- | --- |
| N1 | Converging nozzle law and regimes; forward flow only; thrust = momentum + pressure force. | `engine/physics/ConvergingGasNozzle.cs@a6c914e:L39-L87` | carry forward (EL-039 P6) |
| N2 | Airflow emitter = finite cylindrical field (position, direction, reach, width) with a conserved source and impedance material; sample weights must be positive and sum to one. | `engine/AirflowNetwork.cs@a6c914e:L11-L13`, `engine/AirflowNetwork.cs@a6c914e:L45-L46` | carry forward the emitter shape; do not carry forward the per-step CPU network loop |
| N3 | Reference flow speed 12 m/s; fan force 0–40 N, reach (0, 12] m, width (0, 4] m. | `engine/AirflowNetwork.cs@a6c914e:L24-L24`, `parts/FanPart.cs@a6c914e:L28-L29` | carry forward as scale bounds |
| N4 | Nozzle oracle (fixture gas R 200, Cv 400; source 120 kPa, 300 K; area 0.01 m²): back 30 kPa is choked with exit 61 440 Pa, 240 K and √72 000 ≈ 268.3 m/s; the same mass rate and exit speed at vacuum, with larger thrust; doubling the area doubles mass and enthalpy rates. Same row as [TH-17 Kettle](TH-17-kettle.md) harvest row 6. | `CuriousContraptions.tests/ConvergingGasNozzleTests.cs@a6c914e:L31-L47` | carry forward as an acceptance oracle for this nozzle |
| N5 | A closed area or balanced pressure gives NoFlow with zero mass rate, enthalpy rate and thrust. | `CuriousContraptions.tests/ConvergingGasNozzleTests.cs@a6c914e:L64-L74` | carry forward (closed valve / empty tank control) |

- **Files harvested:** `engine/AirflowNetwork.cs` (emitter shape and constants), `parts/FanPart.cs` (bounds), `CuriousContraptions.tests/ConvergingGasNozzleTests.cs` (N4–N5); gas files as listed in EL-039.

## 5. Acceptance outline

Acceptance authority: [element-042](../requirements.md#element-042), [pneumatics profile](../invest/profiles.md#pneumatics).

- **Construction (actual Chrome UI).** Charged Air tank → hose → Release valve → hose → Air nozzle aimed with the rotate gizmo at a Basketball on a ledge, Receiver below; a Switch triggers the valve. Ball positions are set with the move gizmo, never a numeric menu.
- **Positive.** Ball 1 m from the exit, tank at 400 kPa: the valve opens, the puff shows, the ball (about 11 N peak) leaves the ledge and reaches the Receiver.
- **Negative / control.** Ball 3 m from the exit, tank at 150 kPa: peak force 0.26 N, below the 0.34 N rolling resistance, so the ball does not move (outcome). Matched control at the same 3 m with a 400 kPa tank: 1.27 N, the ball moves — pressure, not distance alone, decides. An empty tank, a closed valve, the nozzle facing away, or a Wall between nozzle and ball: no movement.
- **Boundaries.** `throat_area`, `reach` and `width` limits; a ball at or beyond `reach` receives nothing.
- **Run/Reset.** Reset restores tank pressure, valve phase and ball pose exactly.
- **Save/Load.** Parameters, orientation and connections round-trip.
- **Integrations.** Pneumatic integration task [sequence-task-393](../requirements.md#sequence-task-393) (stored charge released through a valve/air-hose route) and the [todo-378](../requirements.md#todo-378) combinations; interaction processes [IX-08 fluid advection](../requirements.md#interaction-08), [IX-09 pressure work](../requirements.md#interaction-09), [IX-40 gas state evolution](../requirements.md#interaction-40) and [IX-11 aerodynamic drag](../requirements.md#interaction-11) for the jet on the ball. Campaign: the pneumatic row of [campaign-element-coverage](../requirements.md#campaign-element-coverage) reserves first use 71–80 (reuse 91–100, 136–150); the air nozzle is not named in that row, so its lesson slot is still unassigned.

## 6. Open questions

1. **Jet decay law.** The quadratic distance factor (1 − d/reach)² is a proposal; a uniform field (as the fan) or a linear factor would move the pressure/distance boundary: owner decision under S697.
2. **Mount recoil.** Whether a nozzle may be attached to a dynamic body (reaction thrust, rocket-like, see EL-092) or is static only: owner decision.
3. **Static hold.** The negative assumes a force below C_rr·m·g leaves a resting ball still; the solver's rolling-resistance behaviour at zero speed must confirm it: owner check in the first pneumatic story.
