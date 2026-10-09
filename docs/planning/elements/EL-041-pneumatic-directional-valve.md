# EL-041 · Pneumatic directional valve — element readiness spec

Story 7.0 Batch H named-identity spec. Legacy citations are pinned to `a6c914e` and resolve with `git show a6c914e:<path> | sed -n 'start,endp'`. Values marked *proposed* have no legacy or requirement source; each carries a one-line justification, stays inside the [game-grade envelope](../../gpu-f32-physics.md#game-grade-envelope) and may be revised by the owner. Box sizes are full extents. Shared sealed-gas facts P1–P20, the `Gas` domain and the "Air" material are in [EL-039](EL-039-air-reservoir.md#shared-sealed-gas-family-facts).

## 1. Identity

| Item | Value |
| --- | --- |
| Identity / name | EL-041 · Pneumatic directional valve |
| Type | Pneumatic |
| Anchor | [requirements.md#element-041](../requirements.md#element-041); scope index [todo-376](../requirements.md#todo-376); [named-elements entry](../invest/named-elements.md#element-041); owner S476 |
| Related | Refines no CAT spec. Drives the double-acting piston of [todo-427](../requirements.md#todo-427) (two typed chamber ports); fed by [EL-039](EL-039-air-reservoir.md) through [EL-038](EL-038-pneumatic-hose.md); commanded like the [CAT-037 Latch](../requirements.md#current-cat-037) Set/Reset pair. |
| Roadmap story | unscheduled |
| Status | not started |

## 2. Declaration

| Item | Declaration (value · source) |
| --- | --- |
| Bodies and shapes | Static root body, collider box 1.2 × 0.6 × 0.5 m at the origin **proposed** (one valve body wide enough for three gas sockets 0.4 m apart); navy foot 1.3 × 0.12 × 0.6 m at (0, −0.36, 0) **proposed**. |
| Mass and material | Static; restitution 1, bounce threshold 0.1 m/s, friction 0.3 (`engine/gpu/WorkshopPhysicsCompiler.cs@a6c914e:L109-L110`). |
| Constraints | none; the spool is cosmetic. |
| Typed ports | `GasIn` supply (Gas, Input) at (0, −0.15, 0.3) **proposed**; `PortA` (Gas, Output) at (−0.4, 0.15, 0.3) and `PortB` (Gas, Output) at (0.4, 0.15, 0.3) **proposed** (two typed actuator ports, as todo-427 requires "two typed chamber ports"); `SetIn` (Activation, Input, command Set) at (−0.45, 0.36, 0) and `ResetIn` (Activation, Input, command Reset) at (0.45, 0.36, 0) — the legacy latch socket layout (harvest D1). A navy exhaust mouth on the rear face, not a socket. |
| Sensors and activation | Committed position is the closed enum `SpoolPosition { RouteA, RouteB }`. `SetIn` selects RouteA, `ResetIn` selects RouteB from the next committed tick; simultaneous Set and Reset resolve to RouteB (reset dominance, harvest D2). |
| Work and energy stores | none of its own. RouteA connects `GasIn` → `PortA` and `PortB` → exhaust; RouteB connects `GasIn` → `PortB` and `PortA` → exhaust. Each open path uses the nozzle law (EL-039 P6) with area `opening`. The single supply node is shared, so the two actuator ports can never both receive full independent supply; there is no mid position in which both are fed **proposed** (instant switching at the tick boundary keeps the rule exact). |
| Parameters | `opening`: f32, 2 × 10⁻⁵ – 2 × 10⁻⁴ m², default 1 × 10⁻⁴ m² **proposed** (same orifice range as [EL-040](EL-040-pneumatic-release-valve.md), so one tank drives several strokes). `initial_position`: `SpoolPosition`, default RouteB **proposed** (a double-acting piston starts retracted). `exhaust`: closed enum `ExhaustBoundary { Vent, Blocked }`, default Vent (variants below). |
| Cosmetic curves and UI bindings | Spool slide ← committed position, ±0.3 m, 0.1 s eased presentation transition **proposed** (gas-family binding "Blade position ← committed state", [finite-gas-foundation](../../finite-gas-foundation.md#sealed-gas)); A/B arrows on the cream face light gold on the fed port. |
| Art | Cyan `#66b8c9` body, cream `#fff8e9` face with navy `#293954` A (one raised mark) and B (two raised marks) — the one/two-mark convention of the logic gates (`parts/ElectricalLogicPart.cs@a6c914e:L91-L93`); raised bar for Set and hollow ring for Reset as on the latch (harvest D3); gold `#f7cb52` port spheres r 0.075. Geometry **proposed**. |
| Catalogue / inventory | Id `directional_valve`, Title "Two-way valve", Category `Air` **proposed**. Description **proposed**: "Set sends air to the one-mark port, Reset to the two-mark port; the other port exhausts. Only one side is ever fed." |

**Variants** (row outcome: "Blocked exhaust changes motion"):

| Variant | `exhaust` | Behaviour |
| --- | --- | --- |
| Vented exhaust | Vent | The unfed port discharges to ambient (EL-039 P15); a double-acting piston moves freely toward the fed side. |
| Blocked exhaust | Blocked | The unfed port's gas is trapped and compresses as the piston moves (EL-039 P3, P8): motion slows and can stall; with equal pressures on both chambers the piston holds still (P10). |

## 3. Engine capabilities

Families ([element map row](../general-engine-element-map.md); [binding](../../coverage/engine/element-01.json), proof owner S476): EnvironmentState, FiniteLedger, FluidAdvection, GasState, GeometryQuery, JointConstraint, PressureWork, RigidBodyDynamics, TopologyTransaction (+ StateTransaction).

**Exists now**
- Activation input sockets and network (single emission per source per run, sticky latch targets): `engine/gpu/ActivationNetwork.cs@a6c914e:L69-L108`, `engine/gpu/ActivationNetwork.cs@a6c914e:L199-L229`.
- Static box body/collider: `engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L58`, `engine/gpu/PhysicsDeclarations.cs@a6c914e:L89-L90`.

**Missing**
- Gas domain, GasState, FluidAdvection with switched paths — [EL-039 §3](EL-039-air-reservoir.md#3-engine-capabilities); owners [S470](../invest/decisions.md#s470), [S416](../invest/decisions.md#s416), [S257](../invest/decisions.md#s257).
- `SetIn`/`ResetIn` sockets and a two-state memory node that can switch repeatedly within a run: the current network lets each source emit once ("Duplicate source emission", `engine/gpu/ActivationNetwork.cs@a6c914e:L206-L208`) and has no Reset-input node; Story 9.4 (CAT-037 latch) builds the Set/Reset node.
- Gas chambers bound to a slider (PressureWork, JointConstraint) for the double-acting piston — slider joint Story 6.4; chamber binding with the piston slice (todo-427).

**Dependencies.** EL-039, EL-038, a double-acting piston (todo-427), and two activation sources (Switch, Clock CAT-017 or a Latch).

## 4. Sources and legacy

- Requirement row [element-041](../requirements.md#element-041): "Routes supply and exhaust to selected actuator ports"; outcome "Blocked exhaust changes motion; opposite routes cannot both receive full independent supply". Integration task [todo-427](../requirements.md#todo-427): valve transitions, both chambers pressurised, leaks/depletion, blocked load, mid-stroke reversal.
- Named entry [element-041](../invest/named-elements.md#element-041), owner S476. Gas design: "Gas network node + controller input | selected branch | Routes one supply to one branch | Blade position ← committed state".

| # | Fact | Source | Disposition |
| --- | --- | --- | --- |
| D1 | Set/Reset sockets: `SetIn` at (−0.45, 0.72, 0) with command Set, `ResetIn` at (0.45, 0.72, 0) with command Reset; any other command rejects. | `parts/LatchPart.cs@a6c914e:L30-L31`, `parts/LatchPart.cs@a6c914e:L37-L44` | carry forward the typed command sockets (heights rescaled to this body) |
| D2 | The latch is reset-dominant and its commands settle at the next tick boundary, so every delivery in a tick participates. | `parts/LatchPart.cs@a6c914e:L8-L9` | carry forward |
| D3 | Raised bar and hollow ring distinguish Set and Reset without text or colour. | `parts/LatchPart.cs@a6c914e:L62-L62` | carry forward |
| D4 | Equal opposed chambers hold without spending either inventory; blocked gas compresses with work balanced. | `CuriousContraptions.tests/GasChamberWorldTests.cs@a6c914e:L116-L133`, `CuriousContraptions.tests/GasChamberWorldTests.cs@a6c914e:L67-L81` | carry forward (EL-039 P9, P10) |

- **Files harvested:** `parts/LatchPart.cs` (socket and dominance facts), `parts/ElectricalLogicPart.cs` (input-mark convention only); gas files as listed in EL-039.

## 5. Acceptance outline

Acceptance authority: [element-041](../requirements.md#element-041), [pneumatics profile](../invest/profiles.md#pneumatics), [todo-427](../requirements.md#todo-427).

- **Construction (actual Chrome UI).** Charged Air tank → hose → valve `GasIn`; hoses `PortA → ExtendIn` and `PortB → RetractIn` of a double-acting piston; Switch A → `SetIn`, Switch B → `ResetIn` through Connect and the socket-choice buttons.
- **Positive.** Switch A extends the piston; Switch B retracts it; the tank dial drops on each stroke.
- **Negative / control.** Blocked exhaust: the same command produces a visibly slower or stalled stroke (outcome); both switches in one tick → RouteB only; an empty tank moves nothing; a hose from `PortA` to `PortB` never feeds both sides.
- **Boundaries.** `opening` limits; `initial_position` RouteA and RouteB; mid-stroke reversal reverses without a jump.
- **Run/Reset.** Reset restores spool position, tank and piston exactly.
- **Save/Load.** `initial_position`, `exhaust`, `opening` and all five connections round-trip.
- **Integrations.** Double-acting piston row [todo-427](../requirements.md#todo-427) ("Clock/logic alternates a physical sorting pusher between two chutes"; both chambers pressurised, valve transitions, leaks/depletion, blocked load, mid-stroke reversal) under the pneumatic integration task [sequence-task-393](../requirements.md#sequence-task-393); interaction processes [IX-08 fluid advection](../requirements.md#interaction-08), [IX-09 pressure work](../requirements.md#interaction-09) ("opposed load or sealed return changes actual motion", the blocked-exhaust variant) and [IX-07 signal propagation](../requirements.md#interaction-07) for the Set/Reset commands. Campaign: "directional valve" is named in the pneumatic row of [campaign-element-coverage](../requirements.md#campaign-element-coverage), first use 71–80 (reuse 91–100, 136–150).

## 6. Open questions

1. **Control kind.** Activation Set/Reset (proposed) versus a supplied electrical solenoid with spring return: owner decision.
2. **Blocked exhaust form.** A configuration value (proposed) versus a physical cap part or a hose ending in a Sealed free end ([EL-038](EL-038-pneumatic-hose.md)): owner decision.
3. **Spool transition.** Instant switching (proposed) versus a finite closed-centre transition: owner decision.
