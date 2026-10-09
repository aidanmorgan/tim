# EL-037 · Air compressor — element readiness spec

Story 7.0 Batch H named-identity spec. Legacy citations are pinned to `a6c914e` and resolve with `git show a6c914e:<path> | sed -n 'start,endp'`. Values marked *proposed* have no legacy or requirement source; each carries a one-line justification, stays inside the [game-grade envelope](../../gpu-f32-physics.md#game-grade-envelope) and may be revised by the owner. Box sizes are full extents. Shared sealed-gas facts P1–P20, the `Gas` domain and the "Air" material are in [EL-039](EL-039-air-reservoir.md#shared-sealed-gas-family-facts).

## 1. Identity

| Item | Value |
| --- | --- |
| Identity / name | EL-037 · Air compressor |
| Type | Pneumatic |
| Anchor | [requirements.md#element-037](../requirements.md#element-037); scope index [todo-376](../requirements.md#todo-376); [named-elements entry](../invest/named-elements.md#element-037); owner S472 |
| Related | Refines no CAT spec. Electrical supply from [CAT-005 Battery](CAT-005-battery.md) / [EL-196](EL-196-battery.md) through [EL-197 wire](EL-197-electrical-wire.md); shaft supply from [CAT-042 Motor](CAT-042-motor.md); charges [EL-039 Air reservoir](EL-039-air-reservoir.md) through [EL-038 hose](EL-038-pneumatic-hose.md). |
| Roadmap story | unscheduled |
| Status | not started |

## 2. Declaration

| Item | Declaration (value · source) |
| --- | --- |
| Bodies and shapes | Static root body, collider box 1.2 × 0.8 × 0.7 m centred at (0, 0.15, 0) **proposed** (Counter-scale housing that a 0.34 m Basketball cannot pass through); navy foot box 1.3 × 0.16 × 0.85 m at (0, −0.33, 0) **proposed** (foot convention of the supplied parts). |
| Mass and material | Static; contact material restitution 1, bounce threshold 0.1 m/s, friction 0.3 (`engine/gpu/WorkshopPhysicsCompiler.cs@a6c914e:L109-L110`). Working gas "Air" (EL-039 P14). |
| Constraints | none. The cosmetic piston is art, not a body. |
| Typed ports | Variant-specific supply port (below) plus `GasOut` (Gas, Output) at (0.66, 0.15, 0) **proposed**. The outlet is non-return: gas never flows back through the compressor into ambient **proposed** (a reciprocating compressor's delivery valve; otherwise an unpowered compressor would vent the tank). Intake is the ambient boundary (EL-039 P15) through a visible grille, not a connectable port. |
| Sensors and activation | none. No activation input: an activation command cannot compress air. |
| Work and energy stores | No internal store. Each tick the compressor converts supplied work W = P·dt into ideal adiabatic compression of ambient air into the outlet node: specific work w = Cp·T_amb·[(p_out/p_amb)^((γ−1)/γ) − 1], delivered mass dm = η·W/w, delivered enthalpy dm·Cp·T_out (EL-039 P3, P5 law family). Isentropic efficiency η = 0.7 **proposed** (typical small reciprocating compressor; the remaining 30% is dissipated heat, so supplied work ≥ stored gas energy gain). When p_out ≥ `rated_pressure` dm = 0 and no work is drawn. |
| Parameters | `power_rating`: f32, 10–240 W, default 60 W **proposed** (half the default Motor's 20 N·m × 6 rad/s = 120 W, `parts/catalog/motor.tres@a6c914e:L14-L14`, so a compressor alone uses half of a default battery's 120 W `power_limit`, [EL-196](EL-196-battery.md); sharing one default battery with a default Motor demands 60 + 120 = 180 W, so both are scaled by α = 120/180 ≈ 0.67 under the proportional rule — a 240 W battery runs both at full power). `rated_pressure`: f32 absolute, 150–600 kPa, default 600 kPa **proposed** (equals the Air tank rating so the compressor stalls before any limit). Supply kind is the closed enum `CompressorSupply { Electrical, Shaft }`, fixed per catalogue variant. |
| Cosmetic curves and UI bindings | Piston stroke ← committed delivered mass rate (gas-family binding "Piston stroke ← committed charge rate", [finite-gas-foundation](../../finite-gas-foundation.md#sealed-gas)); stroke amplitude 0.12 m **proposed**, cadence proportional to mass rate, stopped at zero. Supply indicator lamp slate `#556573` → gold `#f7cb52` follows committed supply availability (lamp convention of supplied parts). |
| Art | Cyan `#66b8c9` housing, cream `#fff8e9` cylinder head with navy `#293954` cooling fins, navy foot, navy intake grille, gold `#f7cb52` port spheres r 0.075 at each socket ([DESIGN.md colour system](../../../DESIGN.md#colour-system)). Geometry **proposed**. |
| Catalogue / inventory | Electrical variant: Id `air_compressor`, Title "Air compressor"; Shaft variant: Id `shaft_compressor`, Title "Belt compressor" **proposed** (one catalogue entry per variant, as for the electrical gates). Category `Air` (as EL-039). Description **proposed**: "Uses supplied work to pump outside air into a connected tank. Without supply it stops; it never adds air on its own." |

**Variants** (the requirements row names none; the [element map](../general-engine-element-map.md) composition requires "declared electrical/shaft supply"):

| Variant | Supply port | Work source | Specific declaration |
| --- | --- | --- | --- |
| Electrical | `PowerIn` (Electrical, Input) at (−0.66, −0.1, 0) **proposed** | finite electrical store through the supplied network; demand `power_rating` | Unpowered or depleted supply → dm = 0 |
| Shaft | `DriveIn` (Mechanical, Input) at (−0.66, 0.15, 0) **proposed** | upstream shaft; absorbed torque = min(`power_rating`/ω, available) | Stationary shaft → dm = 0; the compressor loads the shaft (it slows) |

## 3. Engine capabilities

Families ([element map row](../general-engine-element-map.md); [binding](../../coverage/engine/element-01.json), proof owner S472): EnvironmentState, FiniteLedger, FluidAdvection, GasState, GeometryQuery, JointConstraint, PressureWork, RigidBodyDynamics, TopologyTransaction (+ StateTransaction); the map adds FiniteWorkActuation and electrical/shaft supply.

**Exists now**
- Static box body/collider/material: `engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L58`, `engine/gpu/PhysicsDeclarations.cs@a6c914e:L89-L90`.
- `WorkshopSocket.PowerIn` and `WorkshopConnectionDomain.Electrical` enum values (not admitted): `engine/gpu/WorkshopConnections.cs@a6c914e:L8-L9`, rejected at `engine/gpu/WorkshopConnections.cs@a6c914e:L89-L92`.

**Missing**
- GasState, FluidAdvection, gas domain — see [EL-039](EL-039-air-reservoir.md#3-engine-capabilities); owners [S470](../invest/decisions.md#s470) (S471, S697), [S416](../invest/decisions.md#s416).
- ElectricalPower with finite demand — Story 8.1 builds binary supply; finite power/energy accounting is [EL-196](EL-196-battery.md) (owner decision); typed power role [S257](../invest/decisions.md#s257).
- FiniteWorkActuation (supplied work → gas enthalpy with efficiency loss) and the envelope's proportional demand scaling ([thermodynamic row](../../gpu-f32-physics.md#game-grade-envelope)).
- Shaft variant: ShaftTorque and mechanical ports — Epic 11 (Story 11.1 Motor), [S257](../invest/decisions.md#s257) mechanical-port row.

**Dependencies.** Battery (CAT-005/EL-196) or Motor (CAT-042) shaft; Air reservoir (EL-039) and hose (EL-038) to store the charge; gauge (EL-043) to observe it.

## 4. Sources and legacy

- Requirement row [element-037](../requirements.md#element-037): "External work raises pressure in a connected finite gas volume"; outcome "Unpowered compressor cannot increase stored gas energy". No variants listed.
- Named entry [element-037](../invest/named-elements.md#element-037), owner S472.
- Element map composition: "Add FiniteWorkActuation and declared electrical/shaft supply to compression/PressureWork; power loss cannot raise stored gas energy."
- Gas family design: "Air compressor (EL-037) | Electrical node + nozzle source into a sealed store | watt rating, stroke | Charges a connected reservoir while supplied | Piston stroke ← committed charge rate" ([finite-gas-foundation](../../finite-gas-foundation.md#sealed-gas)).
- Decision owners: [S470](../invest/decisions.md#s470) gas-state and open-versus-sealed; [S257](../invest/decisions.md#s257) electrical-port and mechanical-port.
- Legacy: no compressor part, level or lesson. Applicable facts: compression work law (EL-039 P3–P4), finite discharge (P5), a supplied motor's work allowance is torque × target speed per tick (`parts/MotorPart.cs@a6c914e:L93-L97`) — carry forward as the shaft-variant power reference; do not carry forward the per-part `PreparePhysics` update loop.
- **Files harvested:** `parts/MotorPart.cs` (power reference only), `parts/catalog/motor.tres`; gas files as listed in EL-039.

## 5. Acceptance outline

Acceptance authority: [element-037](../requirements.md#element-037), [pneumatics profile](../invest/profiles.md#pneumatics).

- **Construction (actual Chrome UI).** Place Battery, Air compressor and Air tank (`initial_pressure` = ambient); select the battery, Connect `Supply`, click the compressor, choose `Supply → PowerIn`; select the compressor, Connect `GasOut`, click the tank, choose `GasOut → GasIn`.
- **Positive.** Run: the piston cycles, the tank dial rises and stops at the rating; the battery ledger falls by at least the gas energy gained.
- **Negative / control.** No battery wire, disabled or depleted battery: the dial never rises and stored gas energy never increases (outcome); activation wired at the compressor is refused (no activation port); cutting supply mid-charge stops the rise and the tank keeps its pressure (non-return outlet).
- **Boundaries.** `power_rating` 10 and 240 W; `rated_pressure` reached exactly then dm = 0; compressor into a disconnected `GasOut` does no work; compressor and Motor on one default 120 W battery both run at about two thirds of their demand.
- **Shaft variant.** Motor-driven shaft charges the tank; a stalled or unconnected shaft charges nothing; the motor visibly slows under compression load.
- **Run/Reset.** Reset restores tank pressure, battery ledger and piston phase exactly.
- **Save/Load.** Parameters, variant id and both connections round-trip.
- **Integrations.** Pneumatic integration task [sequence-task-393](../requirements.md#sequence-task-393) (finite charge stored in a reservoir, valve/air-hose route, piston), with the piston rows [todo-426](../requirements.md#todo-426) and [todo-427](../requirements.md#todo-427) and the [todo-378](../requirements.md#todo-378) combination "One Breath"; interaction processes [IX-06 electrical power transfer](../requirements.md#interaction-06), [IX-08 fluid advection](../requirements.md#interaction-08), [IX-09 pressure work](../requirements.md#interaction-09) and [IX-40 gas state evolution](../requirements.md#interaction-40). Campaign: the pneumatic row of [campaign-element-coverage](../requirements.md#campaign-element-coverage) reserves first use 71–80 (reuse 91–100, 136–150); the compressor is not named in that row, so its lesson slot inside that reservation is still unassigned.

## 6. Open questions

1. **Shaft variant scope.** The element map names electrical/shaft supply; the requirement row and gas design name only electrical. Whether the shaft variant is a required separate catalogue entry: owner decision.
2. **Efficiency and heat.** η = 0.7 dissipates 30% of input; whether that heat is tracked (S543) or simply lost: owner decision.
3. **Intake model.** Whether intake needs a connectable gas inlet (e.g. drawing from another tank) or is always ambient: owner decision (S697).
4. **Acoustic emission.** Whether the running compressor emits sound events (S528): owner decision.
